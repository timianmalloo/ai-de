#!/usr/bin/env python3
"""verify-defect-register.py — make a dangling defect-class citation fail loudly.

The widened control for defect class DC-001.

DC-001's original control caught a document that was *authored and never committed*. It could not
catch the narrower, quieter variant: a document that IS committed, and an entry inside it that was
never written. Three lessons in this repo were assigned IDs, cited as authoritative in four committed
artifacts, and had no register entry at all — `DC-010` was cited by `architecture.md` as a controlled
class while resolving to nothing. The register's own header claimed twelve classes over nine.

A prose citation cannot fail. This makes it fail. For the register it checks:

  1. every `DC-NNN` cited anywhere under docs/ resolves to a real entry heading
  2. the ID sequence has no gaps — a gap means an entry was dropped or an ID was burned silently
  3. every entry declares a `**Status:**` from the known vocabulary
  4. the header's status counts match the entries actually present

(1) is the one that catches this class. (4) is what makes the file self-describing rather than
self-congratulating: a count typed by hand drifts the moment an entry is added.

Usage
  python tools/verify-defect-register.py            check the register
  python tools/verify-defect-register.py --fix-counts   rewrite the header counts from the entries

Exit 0 clean, 1 on any finding.
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
REGISTER = REPO / "docs" / "lessons" / "defect-classes.md"
DOCS = REPO / "docs"

ENTRY = re.compile(r"^### (DC-(\d+)) — (.+)$", re.MULTILINE)
STATUS = re.compile(r"^- \*\*Status:\*\* `([a-z-]+)`", re.MULTILINE)
CITATION = re.compile(r"\bDC-(\d+)\b")
COUNT_LINE = re.compile(
    r"^\*\*Status counts:\*\* controlled (\d+) · partially-controlled (\d+) · uncontrolled (\d+)$",
    re.MULTILINE)

KNOWN_STATUSES = {"controlled", "partially-controlled", "uncontrolled"}

# Windows consoles default to cp1252 and cannot encode the glyphs below.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


def entries(text: str) -> list[tuple[str, int, str]]:
    """(id, number, title) for every register entry, in file order."""
    return [(m.group(1), int(m.group(2)), m.group(3)) for m in ENTRY.finditer(text)]


def statuses(text: str) -> list[str]:
    """The declared status of each entry, in file order.

    Sliced per entry rather than counted globally, so an entry that declares no status at all is
    visible as a gap instead of silently borrowing its neighbour's.
    """
    bounds = [m.start() for m in ENTRY.finditer(text)] + [len(text)]
    found = []
    for i in range(len(bounds) - 1):
        block = text[bounds[i]:bounds[i + 1]]
        match = STATUS.search(block)
        found.append(match.group(1) if match else "")

    return found


def citations() -> dict[int, list[str]]:
    """Every DC-NNN referenced under docs/, mapped to the files citing it."""
    found: dict[int, list[str]] = {}
    for path in sorted(DOCS.rglob("*.md")):
        if path == REGISTER:
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            continue
        for number in {int(m.group(1)) for m in CITATION.finditer(text)}:
            found.setdefault(number, []).append(str(path.relative_to(REPO)).replace("\\", "/"))

    return found


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fix-counts", action="store_true",
                        help="rewrite the header status counts from the entries actually present")
    args = parser.parse_args()

    if not REGISTER.exists():
        print(f"verify-defect-register: {REGISTER.relative_to(REPO)} does not exist")
        return 1

    text = REGISTER.read_text(encoding="utf-8")
    found = entries(text)
    declared = statuses(text)
    findings: list[str] = []

    if not found:
        print("verify-defect-register: the register contains no entries — refusing to pass over nothing")
        return 1

    numbers = [n for _, n, _ in found]
    present = set(numbers)

    # 1. Dangling citations. The class this gate exists for.
    for number, files in sorted(citations().items()):
        if number not in present:
            findings.append(
                f"DC-{number:03d} is cited by {', '.join(files)} but has no entry in the register — "
                f"a citation that resolves to nothing reads as authority and carries none")

    # 2. Gaps in the sequence.
    for missing in sorted(set(range(min(numbers), max(numbers) + 1)) - present):
        findings.append(
            f"DC-{missing:03d} is missing from the sequence (the register runs "
            f"{min(numbers):03d}–{max(numbers):03d}) — an ID was either dropped or burned silently")

    # 3. Duplicates and missing/unknown statuses.
    for number in sorted({n for n in numbers if numbers.count(n) > 1}):
        findings.append(f"DC-{number:03d} has more than one entry — one entry per class (CI1)")

    for (identifier, _, title), status in zip(found, declared):
        if not status:
            findings.append(
                f"{identifier} has no `**Status:** `backticked-value`` line ({title}) — "
                f"the value must be in backticks, e.g. \"- **Status:** `partially-controlled` — why\"")
        elif status not in KNOWN_STATUSES:
            findings.append(
                f"{identifier} declares status '{status}', which is not one of "
                f"{'/'.join(sorted(KNOWN_STATUSES))}")

    # 4. The header counts.
    actual = {name: declared.count(name) for name in KNOWN_STATUSES}
    header = COUNT_LINE.search(text)
    rendered = (f"**Status counts:** controlled {actual['controlled']} · "
                f"partially-controlled {actual['partially-controlled']} · "
                f"uncontrolled {actual['uncontrolled']}")

    if args.fix_counts:
        if header is None:
            print("verify-defect-register: no status-count line to rewrite")
            return 1
        REGISTER.write_text(text[:header.start()] + rendered + text[header.end():], encoding="utf-8")
        print(f"verify-defect-register: counts rewritten → {rendered}")
        return 0

    if header is None:
        findings.append("the register has no `**Status counts:**` line")
    else:
        stated = {"controlled": int(header.group(1)),
                  "partially-controlled": int(header.group(2)),
                  "uncontrolled": int(header.group(3))}
        if stated != actual:
            findings.append(
                f"the header claims controlled {stated['controlled']} · partially-controlled "
                f"{stated['partially-controlled']} · uncontrolled {stated['uncontrolled']}, but the "
                f"entries are controlled {actual['controlled']} · partially-controlled "
                f"{actual['partially-controlled']} · uncontrolled {actual['uncontrolled']} — "
                f"run --fix-counts")

    print(f"{len(found)} entr{'y' if len(found) == 1 else 'ies'}: "
          f"{', '.join(i for i, _, _ in found)}")
    print(rendered)
    print()

    if findings:
        print("verify-defect-register: FAILED")
        for finding in findings:
            print(f"  - {finding}")
        return 1

    print(f"verify-defect-register: OK — every cited class resolves, the sequence is unbroken, "
          f"and the header counts match.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
