#!/usr/bin/env python3
"""verify-no-new-console-launches.py — no code under src/ or tests/ launches a child with CREATE_NEW_CONSOLE.

The control for defect class DC-170 (INV-0011 §7, measured 2026-09-12): on a machine where Windows
Terminal is the default terminal application, a child started with `CREATE_NEW_CONSOLE` is handed
to Windows Terminal as a tab, and Windows Terminal's agent host attaches an agent session (a
`copilot.exe --acp` child and its MCP servers, one `node.exe` each) to that tab and keeps it for
Windows Terminal's lifetime after the tab closes. Nine Core test classes launched the terminal-host
helper that way: two tests → two `node.exe` born, with or without WT_SESSION in the host; 25 born
during one whole-suite recount; 257 accumulated over two days. The launcher now uses
`CREATE_NO_WINDOW` (a headless console, never a tab): the same twenty-one tests, zero born.

WHAT THIS READS. Every *.cs under src/ and tests/, recursively; the token `CREATE_NEW_CONSOLE` on a
line that is CODE — a line whose first non-blank characters are not `//` (doc comments may name the
flag while explaining why it is not used). Allowlist: none. A `const` declaration counts: it exists
to be used.

Usage
  python tools/verify-no-new-console-launches.py              scan the repository
  python tools/verify-no-new-console-launches.py --self-test  prove the gate can fail (DC-104)

Exit 0 when clean, 1 on a finding, 2 on a usage error. Stdlib only.
"""
from __future__ import annotations

import argparse
import re
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ROOTS = ("src", "tests")
TOKEN = re.compile(r"CREATE_NEW_CONSOLE")
COMMENT = re.compile(r"^\s*//")


def findings(root: Path) -> list[str]:
    out: list[str] = []
    for sub in ROOTS:
        base = root / sub
        if not base.is_dir():
            continue
        for path in sorted(base.rglob("*.cs")):
            if any(part in ("bin", "obj") for part in path.parts):
                continue
            try:
                lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
            except OSError as error:
                out.append(f"{path.relative_to(root)}: unreadable ({error})")
                continue
            for number, line in enumerate(lines, 1):
                if TOKEN.search(line) and not COMMENT.match(line):
                    out.append(f"{path.relative_to(root).as_posix()}:{number}: {line.strip()[:100]}")
    return out


def self_test() -> int:
    with tempfile.TemporaryDirectory() as tmp:
        fake = Path(tmp)
        (fake / "tests").mkdir()
        (fake / "tests" / "Launch.cs").write_text(
            "/// <summary>Explains CREATE_NEW_CONSOLE in prose.</summary>\n"
            "const uint CREATE_NEW_CONSOLE = 0x00000010;\n", encoding="utf-8")
        red = findings(fake)
        (fake / "tests" / "Launch.cs").write_text(
            "/// <summary>Explains CREATE_NEW_CONSOLE in prose.</summary>\n"
            "const uint CREATE_NO_WINDOW = 0x08000000;\n", encoding="utf-8")
        green = findings(fake)
    if len(red) != 1 or green:
        print(f"verify-no-new-console-launches --self-test: FAILED (red={red}, green={green})")
        return 1
    print("verify-no-new-console-launches --self-test: OK — a code-line launch is a finding, a comment is not")
    return 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args(argv)
    if args.self_test:
        return self_test()
    found = findings(ROOT)
    if found:
        print("verify-no-new-console-launches: FAILED — a child is launched with CREATE_NEW_CONSOLE (DC-170):")
        for f in found:
            print(f"  - {f}")
        print("  remedy: CREATE_NO_WINDOW (a headless console) — see tests/AiDe.Core.Tests/TerminalHostLauncher.cs")
        return 1
    print("verify-no-new-console-launches: OK — no CREATE_NEW_CONSOLE launch under src/ or tests/.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
