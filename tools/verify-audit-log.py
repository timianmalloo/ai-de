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
  2. no id present in the committed version has DISAPPEARED
  3. ids parse as <prefix>-<number> or <prefix>-<ULID> (the pack rev-59 allocator)
  4. every line is valid JSON with an id at all

(1) is the class. (2) is the hole (1) left, and it cost a real entry: resolving a merge by unioning
keyed on id silently dropped one side, and THIS GATE STAYED GREEN — because uniqueness was satisfied
precisely by the removal. A control that only counts duplicates cannot see a deletion, and an
append-only log has no legitimate reason to shrink (DC-026). The rest are cheap neighbours worth
having while the file is open.

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
# Two id shapes are legitimate, and the second one arrived with pack revision 59.
#
#   al-0449                       the sequential allocator
#   al-01M1MYWGG050BEVR42EHRC7FBZ the pack's coord_ids.py ULID allocator
#
# The pack moved to ULIDs to stop two trees allocating the SAME id independently — which is not a
# hypothetical here: docs/adr currently carries four collisions (adr-0017..0020, each claimed by two
# unrelated decisions) from exactly that, and they fail this repository's own allocator gate on main.
# A sequential allocator cannot be made safe across concurrent worktrees; a ULID cannot collide.
#
# Both are accepted rather than switching, because the log is append-only: 435 existing entries carry
# the sequential form and rewriting them to satisfy a format rule would be editing history to please
# a checker.
ID = re.compile(r"^([a-z]+)-(\d+|[0-9A-HJKMNP-TV-Z]{26})$")

# Windows consoles default to cp1252 and cannot encode the glyphs below.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


def committed_ids(path: Path) -> set[str] | None:
    """The ids in HEAD's version of this file, or None when git cannot say."""
    import subprocess

    relative = path.relative_to(REPO).as_posix()

    try:
        result = subprocess.run(["git", "show", f"HEAD:{relative}"],
                                cwd=REPO, capture_output=True, timeout=30, check=False)
    except (OSError, subprocess.SubprocessError):
        return None

    if result.returncode != 0:
        return None                      # new file, or no git: nothing to compare against

    found: set[str] = set()
    for line in result.stdout.decode("utf-8", "replace").splitlines():
        if not line.strip():
            continue
        try:
            identifier = json.loads(line).get("id")
        except json.JSONDecodeError:
            continue
        if identifier:
            found.add(str(identifier))

    return found


def check_no_entry_vanished(path: Path, present: set[str]) -> list[str]:
    """
    An append-only log may grow. It may not shrink.

    Compared against HEAD rather than against a stored count, because the question is "did this
    working copy lose something that was committed", and HEAD is the only thing that knows.
    """
    was = committed_ids(path)
    if was is None:
        return []

    gone = sorted(was - present)
    if not gone:
        return []

    return [
        f"{path.name}: {len(gone)} id(s) present in HEAD are missing here: "
        + ", ".join(gone[:8]) + ("…" if len(gone) > 8 else "")
        + " — an append-only log does not shrink. A merge resolved by de-duplicating on id drops "
          "one side silently (DC-026); use tools/merge-append-only-log.py."
    ]


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
            findings.append(
                f"{path.name}:{number}: id '{identifier}' is neither <prefix>-<number> nor "
                "<prefix>-<ULID>")

        ids.append(str(identifier))

    duplicates = {i: n for i, n in Counter(ids).items() if n > 1}
    for identifier, count in sorted(duplicates.items()):
        findings.append(
            f"{path.name}: id '{identifier}' is claimed by {count} entries — two trees allocated it "
            f"independently. Renumber the later one; do not merge two records under one id.")

    print(f"{path.name:<24} {len(ids):>4} entries, {len(duplicates)} duplicate id(s)")
    findings.extend(check_no_entry_vanished(path, {
        str(json.loads(line).get("id"))
        for line in path.read_text(encoding="utf-8").splitlines()
        if line.strip() and _parses(line)
    }))

    return findings


def _parses(line: str) -> bool:
    try:
        json.loads(line)
        return True
    except json.JSONDecodeError:
        return False


def main(argv: list[str]) -> int:
    logs = [Path(a) for a in argv] if argv else DEFAULT_LOGS

    findings: list[str] = []
    counted = 0
    for path in logs:
        findings.extend(check(path))

        # PRINT THE CARDINALITY, NOT JUST THE VERDICT. "OK" and "OK — 347 entries" fail identically
        # and differ completely: only the second can be contradicted by a later run, and being
        # contradicted is how every blind spot found today surfaced. A gate that prints only OK
        # gives the next run nothing to disagree with.
        if path.exists():
            counted += sum(1 for line in path.read_text(encoding="utf-8", errors="replace").splitlines()
                           if line.strip())

    print()
    if findings:
        print("verify-audit-log: FAILED")
        for finding in findings:
            print(f"  - {finding}")
        return 1

    print(f"verify-audit-log: OK — {counted:,} entr(ies) across {len(logs)} log(s); every id is "
          "claimed by exactly one entry.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
