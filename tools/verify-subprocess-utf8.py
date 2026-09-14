#!/usr/bin/env python3
"""verify-subprocess-utf8.py — a subprocess read in a tool states its encoding; the locale never decides.

THE CLASS (DC-211, the F5 oracle on 2026-09-14; DC-016's mechanism recurring). `subprocess.run(...,
text=True)` with no `encoding=` decodes the child's bytes with the interpreter's locale codec — cp1252
on this machine — while every file this repository writes is UTF-8. A `git show` of a UTF-8 file then
reads `§` as two characters, and an oracle comparing that against `Path.read_text(encoding="utf-8")`
reports its own committed bytes as CHANGED. The nine-clause front-door oracle did exactly that, and
could not be repaired in place: a repair post-dates the run its clause 0 guards. DC-016 was the same
decode failing the other way — reads that threw and a gate that printed OK over nothing.

WHAT IT CHECKS. Every `.py` under `tools/` and `spikes/` (the repository's own scripts; the pack's
scripts under docs/ai-forward-pack/ are the pack's to keep, and a finding there goes to ai-forward):
a code line that starts a child process in text mode (`subprocess.run(`, `check_output(`, `Popen(`, or
a continuation line carrying `capture_output=True`) and says `text=True` or `universal_newlines=True`
must also say `encoding=` on the same statement (this scan reads the statement's lines up to the
closing paren). Prose in docstrings and comments is not a call and is ignored.

ALLOWLIST, with the reason each entry is there:
  tools/verify-front-door-exit-evidence.py  frozen at commit 1374401d by its own clause 0; its
                                            caller (verify-front-door-exit-attended.py) runs it under
                                            PYTHONUTF8=1 instead.

Exit 0 when every statement states its encoding; 1 with the offending file:line list. `--self-test`
plants the shape in a temporary file and observes the finding. Stdlib only.
"""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
import tempfile
from pathlib import Path

ROOTS = ("tools", "spikes")
ALLOW = {
    "tools/verify-front-door-exit-evidence.py": "frozen oracle (clause 0); run under PYTHONUTF8=1 by its wrapper",
}
CALL = re.compile(r"\b(subprocess\.(run|check_output|check_call|call|Popen)|Popen)\s*\(")
TEXT = re.compile(r"\b(text|universal_newlines)\s*=\s*True\b")
ENCODING = re.compile(r"\bencoding\s*=")


def repo_root() -> Path:
    return Path(subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True,
                               encoding="utf-8", errors="replace", check=True).stdout.strip())


def statements(lines: list[str]):
    """Yield (start line number, statement text) for every call statement, joined to its closing paren."""
    i = 0
    n = len(lines)
    while i < n:
        line = lines[i]
        stripped = line.lstrip()
        if stripped.startswith("#") or not CALL.search(line):
            i += 1
            continue
        depth = 0
        text = []
        j = i
        while j < n:
            text.append(lines[j])
            depth += lines[j].count("(") - lines[j].count(")")
            if depth <= 0 and j >= i:
                break
            j += 1
        yield i + 1, "\n".join(text)
        i = j + 1


def scan_file(path: Path) -> list[tuple[int, str]]:
    findings = []
    try:
        lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError:
        return findings
    for lineno, statement in statements(lines):
        if TEXT.search(statement) and not ENCODING.search(statement):
            findings.append((lineno, statement.splitlines()[0].strip()[:100]))
    return findings


def scan(root: Path, roots=ROOTS) -> list[str]:
    out = []
    for base in roots:
        for path in sorted((root / base).rglob("*.py")):
            rel = path.relative_to(root).as_posix()
            if "node_modules" in rel or rel in ALLOW:
                continue
            for lineno, head in scan_file(path):
                out.append(f"{rel}:{lineno}: a text-mode subprocess with no encoding= — {head}")
    return out


def self_test(root: Path) -> int:
    with tempfile.TemporaryDirectory() as tmp:
        fake_root = Path(tmp)
        (fake_root / "tools").mkdir()
        (fake_root / "spikes").mkdir()
        bad = fake_root / "tools" / "probe.py"
        bad.write_text(
            'import subprocess\n'
            '# a comment saying text=True is not a call\n'
            'out = subprocess.run(["git", "show", "HEAD:x"],\n'
            '                     capture_output=True, text=True)\n'
            'ok = subprocess.run(["git", "x"], capture_output=True, text=True, encoding="utf-8")\n',
            encoding="utf-8")
        findings = scan(fake_root)
        if len(findings) != 1 or "probe.py:3" not in findings[0]:
            print(f"self-test: expected one finding at probe.py:3, got {findings}")
            return 1
    print("verify-subprocess-utf8: self-test OK — a text-mode call with no encoding is a finding; the stated one and the comment are not.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    root = repo_root()
    if args.self_test:
        return self_test(root)
    findings = scan(root)
    for f in findings:
        print(f)
    if findings:
        print(f"\nverify-subprocess-utf8: {len(findings)} finding(s) — add encoding=\"utf-8\" (errors=\"replace\" where the child may emit anything) to each.")
        return 1
    print("verify-subprocess-utf8: OK — every text-mode subprocess under tools/ and spikes/ states its encoding"
          f" ({len(ALLOW)} allowlisted with a reason).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
