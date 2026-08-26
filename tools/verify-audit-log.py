#!/usr/bin/env python3
"""verify-audit-log.py — catch two records that claim the same audit id.

The control for defect class DC-013.

The audit and change logs allocate ids by reading the highest one present and adding one. That is
correct in a single checkout and wrong the moment there are two, which is the normal state of this
repo: worktree discipline says a session that writes gets its own tree, so two trees routinely hold
the same `al-NNNN` as their highest entry and both hand it to the next writer. Neither notices. The
collision surfaces later as a merge conflict, or — worse — as a clean append-only merge in which two
unrelated records share an id and one silently wins the lookup.

It has happened twice in this repo. Both times it was resolved by discarding one entry and
re-logging it, which is the right fix and leaves no trace that would stop the third time.

This checks, for each log:

  1. no id appears twice
  2. ids parse as <prefix>-<number>
  3. every line is valid JSON with an id at all

(1) is the class. The rest are the cheap neighbours worth having while the file is open.

Usage
  python tools/verify-audit-log.py                     check the committed logs
  python tools/verify-audit-log.py <file> [<file> ...] check specific files (used to observe it red)

Exit 0 clean, 1 on any finding.
"""
from __future__ import annotations

import json
import re
import sys
from collections import Counter
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
DEFAULT_LOGS = [
    REPO / "docs" / "audit" / "audit-log.jsonl",
    REPO / "docs" / "audit" / "change-log.jsonl",
]
ID = re.compile(r"^([a-z]+)-(\d+)$")

# Windows consoles default to cp1252 and cannot encode the glyphs below.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


def check(path: Path) -> list[str]:
    if not path.exists():
        # A missing change log is normal early in a project; a missing audit log is not, but that is
        # the Audit Mandate's business rather than this gate's.
        print(f"{path.name:<24} (absent)")
        return []

    findings: list[str] = []
    ids: list[str] = []

    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        if not line.strip():
            continue
        try:
            entry = json.loads(line)
        except json.JSONDecodeError as error:
            findings.append(f"{path.name}:{number}: not valid JSON — {error.msg}")
            continue

        identifier = entry.get("id")
        if not identifier:
            findings.append(f"{path.name}:{number}: entry has no id")
            continue

        if not ID.match(str(identifier)):
            findings.append(f"{path.name}:{number}: id '{identifier}' is not <prefix>-<number>")

        ids.append(str(identifier))

    duplicates = {i: n for i, n in Counter(ids).items() if n > 1}
    for identifier, count in sorted(duplicates.items()):
        findings.append(
            f"{path.name}: id '{identifier}' is claimed by {count} entries — two trees allocated it "
            f"independently. Renumber the later one; do not merge two records under one id.")

    print(f"{path.name:<24} {len(ids):>4} entries, {len(duplicates)} duplicate id(s)")
    return findings


def main(argv: list[str]) -> int:
    logs = [Path(a) for a in argv] if argv else DEFAULT_LOGS

    findings: list[str] = []
    for path in logs:
        findings.extend(check(path))

    print()
    if findings:
        print("verify-audit-log: FAILED")
        for finding in findings:
            print(f"  - {finding}")
        return 1

    print("verify-audit-log: OK — every audit id is claimed by exactly one entry.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
