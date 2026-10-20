"""Refuse a literal coordination seat id or Leader epoch in the templates seats are started from (DC-NEW-wl-leader5).

Leaders hand off mid-drive, so a Leader id written into a template goes stale at the next hand-off: brief-common.md
named wl-leader3-20261019 and epoch 3 for two epochs, and T32's brief sent its done mail to a session that was gone.
A template says how to resolve the seat at send time (`coord leader who`, `$AGENT_SESSION`) and never names one.

Checked: every `.agents/briefs/*/brief-common.md` and every `docs/coordination/*-kickoff.md`. A track's assembled
brief and a hand-off note are records of one moment, so they may name the seat of that moment.

  python tools/verify-no-literal-seat-ids.py [--root DIR]
  python tools/verify-no-literal-seat-ids.py --self-test    prove the gate can fail

Exit 0 clean, 1 a literal found, 2 no template found (the check did not run).
"""
import argparse
import re
import sys
import tempfile
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

TEMPLATES = (".agents/briefs/*/brief-common.md", "docs/coordination/*-kickoff.md")
LITERALS = (
    (re.compile(r"\b[a-z]+-leader\d*-\d{8}\b"), "a Leader session id"),
    (re.compile(r"\b[Ll]eader epoch\W{0,6}\d"), "a Leader epoch"),
)


def check(root):
    files = sorted(path for pattern in TEMPLATES for path in root.glob(pattern))
    if not files:
        print("SEAT-ID-NOT-CHECKED: no template found under " + str(root))
        return 2
    found = []
    for path in files:
        lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
        for number, line in enumerate(lines, 1):
            for pattern, what in LITERALS:
                for match in pattern.finditer(line):
                    found.append(f"{path.relative_to(root).as_posix()}:{number}: {what} '{match.group(0)}'")
    if found:
        print("SEAT-ID-LITERAL: resolve the seat at send time ('coord leader who', $AGENT_SESSION) instead of naming it:")
        for item in found:
            print("  " + item)
        return 1
    print(f"no literal seat id or Leader epoch in {len(files)} template(s)")
    return 0


def self_test():
    """A clean template, each literal shape, and an empty tree must each exit as declared."""
    cases = (
        ("clean", "The Leader is whoever `coord leader who` names when you send.\n", 0),
        ("id", "The Leader is `wl-leader3-20261019`.\n", 1),
        ("epoch", "Leader epoch: **3**.\n", 1),
        ("empty", None, 2),
    )
    failures = []
    for name, text, want in cases:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            if text is not None:
                brief = root / ".agents" / "briefs" / "drive" / "brief-common.md"
                brief.parent.mkdir(parents=True)
                brief.write_text(text, encoding="utf-8", newline="\n")
            code = check(root)
        if code != want:
            failures.append(f"{name}: exit {code}, expected {want}")
    if failures:
        print("self-test FAILED: " + "; ".join(failures))
        return 1
    print("self-test OK - a clean template, a literal id, a literal epoch and an empty tree each exit as declared.")
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument("--self-test", action="store_true", help="prove the gate can fail")
    args = parser.parse_args()
    if args.self_test:
        return self_test()
    return check(args.root)


if __name__ == "__main__":
    raise SystemExit(main())
