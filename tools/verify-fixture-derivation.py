#!/usr/bin/env python3
"""Fail when a test restates a list the product already declares (DC-021).

The class: a test needs "the set of things this release ships" — surfaces, kinds, commands — and
writes the list out by hand. It is correct the day it is typed. The next release adds a member and
tests *about something else* go red, pointing away from the change that caused them. The repair is
trivial, which is exactly what stops anyone asking why it happened a third time.

It happened three times before it was registered. The register entry recorded the residual risk
honestly: derivation was a convention, and nothing failed when the next fixture typed the list out
again. This is that missing control.

**What it looks for.** The product's own vocabulary — the surface ids in `Layout.Default()` and the
kinds in `SurfaceContentFactory.KnownKinds` — appearing as three or more quoted literals inside one
collection literal in a test file. Three is the threshold because one or two identifiers is a test
naming the specific things it is about, which is exactly what a test should do; three or more in a
collection is someone enumerating the set.

**The escape hatch is a stated reason**, not a flag: put `fixture-derivation: ok — <why>` in a
comment on the line above. A test that deliberately pins an exact historical set has a real reason,
and the reason belongs next to it.

Usage:
    python tools/verify-fixture-derivation.py          # fail on findings
    python tools/verify-fixture-derivation.py --list   # print the vocabulary it derived
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

# Windows consoles default to cp1252 and cannot encode the glyphs below.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parent.parent
TEST_ROOT = ROOT / "tests"

LAYOUT_MODEL = ROOT / "src" / "AiDe.Core" / "Workbench" / "LayoutModel.cs"
SURFACE_FACTORY = ROOT / "src" / "AiDe.App" / "Workbench" / "SurfaceContentFactory.cs"

SURFACE_ID = re.compile(r'new Surface\("([^"]+)"')
KNOWN_KINDS = re.compile(r"KnownKinds\s*\{\s*get;\s*\}\s*=\s*\[([^\]]*)\]", re.S)
QUOTED = re.compile(r'"([^"\\]*)"')
ALLOW = re.compile(r"fixture-derivation:\s*ok", re.I)

# A collection literal on one line: [ ... ] or { ... }. Deliberately line-scoped — a multi-line
# literal is rare in these fixtures and a cross-line matcher produces false positives on ordinary
# code, which is how a lint gets switched off.
LITERAL = re.compile(r"[\[{]([^\[\]{}]*)[\]}]")

THRESHOLD = 3


def vocabulary() -> set[str]:
    """The identifiers the product declares. Read from the product, never listed here."""
    words: set[str] = set()

    if LAYOUT_MODEL.exists():
        words.update(SURFACE_ID.findall(LAYOUT_MODEL.read_text(encoding="utf-8")))

    if SURFACE_FACTORY.exists():
        match = KNOWN_KINDS.search(SURFACE_FACTORY.read_text(encoding="utf-8"))
        if match:
            words.update(QUOTED.findall(match.group(1)))

    return words


def findings(words: set[str]) -> list[tuple[Path, int, str, list[str]]]:
    found: list[tuple[Path, int, str, list[str]]] = []

    for path in sorted(TEST_ROOT.rglob("*.cs")):
        lines = path.read_text(encoding="utf-8", errors="replace").splitlines()

        for number, line in enumerate(lines, start=1):
            stripped = line.strip()
            if stripped.startswith("//"):
                continue

            # The reason may sit on this line or the one above it.
            above = lines[number - 2] if number >= 2 else ""
            if ALLOW.search(line) or ALLOW.search(above):
                continue

            for literal in LITERAL.findall(line):
                hits = [q for q in QUOTED.findall(literal) if q in words]
                if len(set(hits)) >= THRESHOLD:
                    found.append((path, number, stripped, sorted(set(hits))))

    return found


def main() -> int:
    words = vocabulary()

    if not words:
        # Failing closed. An empty vocabulary would make this gate pass over everything, which is a
        # control that cannot fire — the exact shape it exists to prevent.
        print("verify-fixture-derivation: FAILED — derived no vocabulary from the product source.")
        print("  looked in:", LAYOUT_MODEL.relative_to(ROOT), "and", SURFACE_FACTORY.relative_to(ROOT))
        return 1

    if "--list" in sys.argv:
        print("verify-fixture-derivation: vocabulary derived from the product:")
        for word in sorted(words):
            print(" ", word)
        return 0

    found = findings(words)

    if not found:
        print(f"verify-fixture-derivation: OK — no test restates the product's own list "
              f"({len(words)} identifier(s) watched).")
        return 0

    print("verify-fixture-derivation: FAILED — a test enumerates what the product declares (DC-021).")
    print()

    for path, number, line, hits in found:
        print(f"  {path.relative_to(ROOT)}:{number}")
        print(f"    {line[:160]}")
        print(f"    names: {', '.join(hits)}")
        print()

    print("  Derive the set from the product instead — Layout.Default(), KnownKinds, the command")
    print("  catalog — so the fixture cannot disagree with what ships. If the exact set is the point")
    print("  of the test, say why in a comment containing: fixture-derivation: ok — <reason>")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
