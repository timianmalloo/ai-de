#!/usr/bin/env python3
"""Every monotonic id family in this repository is guarded, and none has handed an id out twice.

DC-013 — a monotonically allocated id is handed out twice because two trees allocate
independently — has now recurred FOUR times: al-0012, al-0028, al-0071, and DC-032. The first
three were audit ids and were caught by verify-audit-log.py. The fourth was a defect-class id, in
a file that merged CLEANLY because the two entries were hundreds of lines apart, and it was caught
only because verify-defect-register.py happens to enforce one-entry-per-class for its own reasons.

The control was too narrow, not ignored. Two allocators were guarded by two scripts written for
those two allocators, and the register was a third allocator that nobody had classified as one.
This script exists so that the FIFTH allocator is guarded on the day it is invented rather than on
the day it collides:

  * every declared family is checked for duplicate ids and for a broken sequence;
  * every UNDECLARED family that looks like one is reported, so a new sequence cannot quietly
    accumulate a hundred entries and its first collision.

Why not elect a single allocator between sessions instead. Because the sessions work in separate
worktrees on purpose, and an election needs a rendezvous they do not have: a session an hour into
its work has not fetched, so "ask the allocator" is either stale or a blocking round trip through
`main`. Election also makes one session wait on another to record a lesson, which is a worse
failure than a rename. The cheaper answer is the one already proven for the JSONL logs — union at
merge time and re-issue the loser — plus detection wide enough that no family is missed.

Exit 0 when clean, 1 otherwise. Stdlib only.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from collections import Counter, defaultdict
from pathlib import Path

# --------------------------------------------------------------------------------------------
# The families this repository allocates. Adding one is a line here, not a new script — which is
# the whole point: the previous shape was one bespoke checker per allocator, and the allocator
# without a checker is the one that collided.
# --------------------------------------------------------------------------------------------
# `contiguous` says whether a HOLE in the sequence is a defect. It is not always: the append-only
# logs resolve a collision by RE-ISSUING the loser, which is the documented protocol and which
# leaves the contested number permanently unused. This checker's first run against the real
# repository reported eighteen such holes as failures — flagging the fix as the defect, which is
# how a control teaches people to ignore it. The holes were verified against `git log -S`: none of
# the missing ids has ever existed in history, so nothing was lost, they were never written.
FAMILIES = [
    {
        "prefix": "al",
        "path": "docs/audit/audit-log.jsonl",
        "kind": "jsonl",
        "field": "id",
        "what": "audit entries",
        "contiguous": False,  # re-issue on collision leaves gaps by design
    },
    {
        "prefix": "cl",
        "path": "docs/audit/change-log.jsonl",
        "kind": "jsonl",
        "field": "id",
        "what": "change entries",
        "contiguous": False,
    },
    {
        "prefix": "DC",
        "path": "docs/lessons/defect-classes.md",
        "kind": "heading",
        "pattern": r"^###\s+(DC-\d+)\b",
        "what": "defect classes",
        # A hole here IS a defect: a class does not get re-issued, it gets renumbered in place, so
        # a missing number means a lesson was deleted rather than superseded.
        "contiguous": True,
    },
    {
        # FOUND BY THIS SCRIPT'S OWN UNDECLARED-FAMILY CHECK, on its first run — the fifth allocator
        # the header predicts, guarded on the day it was noticed rather than the day it collides.
        #
        # And it is allocated by FILENAME, which is a kind the first draft of this script did not
        # have. That matters more than the one family: `docs/adr/`, `docs/notes/` and
        # `docs/investigations/` all number their files, so a checker that could only read ids out
        # of a single file would have missed every one of them. The first draft also tried to read
        # ADR ids out of `architecture.md`, which merely CITES them — an allocator is where an id is
        # created, never where it is mentioned, and confusing the two makes every citation look like
        # a duplicate allocation.
        "prefix": "adr",
        "path": "docs/adr",
        "kind": "filename",
        "pattern": r"^(\d+)-",
        "what": "architecture decisions",
        "contiguous": True,
    },
    {
        # Below the undeclared-family threshold today (two entries), so the scan would not have
        # flagged it until the eighth. Declared on sight instead: the cost of a line here is
        # nothing against the cost of the collision, and "it is too small to collide yet" is a
        # statement with an expiry date.
        "prefix": "INV",
        "path": "docs/investigations",
        "kind": "filename",
        "pattern": r"^INV-(\d+)",
        "what": "investigations",
        "contiguous": True,
    },
]

# A token that looks like a monotonic id: a short prefix, a dash, a zero-padded number.
CANDIDATE = re.compile(r"\b([A-Za-z]{2,5})-(\d{3,5})\b")

# Enough sightings to be a sequence rather than a version number or a date fragment.
CANDIDATE_THRESHOLD = 8

# Files that legitimately mention ids without allocating them.
NOT_ALLOCATORS = {
    "docs/audit/audit-data.js",      # derived view of the two logs
    "docs/docs-index.js",            # derived view of the docs graph
}

# Prefixes that are references to somebody else's sequence, not ours.
FOREIGN_PREFIXES = {
    "CVE", "RFC", "WCAG", "ISO", "SC", "UTF", "ECMA", "IEC",
    # FR-nnn is the AI-Forward Pack's own requirement numbering, referenced by these docs and
    # allocated in the pack rather than here — verified: no file in this repository DEFINES one,
    # they only cite them.
    "FR",
}


def repo_root() -> Path:
    out = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True)
    return Path(out.stdout.strip())


def tracked_text_files(root: Path) -> list[str]:
    out = subprocess.run(
        ["git", "ls-files"], capture_output=True, text=True, check=True, cwd=root)

    keep = (".md", ".jsonl", ".json", ".py", ".cs", ".txt", ".yml", ".yaml")
    return [p for p in out.stdout.splitlines() if p.endswith(keep)]


def ids_in_family(root: Path, family: dict) -> tuple[list[str], str | None]:
    """Every id the family's own file allocates, in file order."""
    path = root / family["path"]
    found: list[str] = []

    if not path.exists():
        return [], f"{family['path']} does not exist"

    if family["kind"] == "filename":
        directory = root / family["path"]

        if not directory.is_dir():
            return [], f"{family['path']} is not a directory"

        for entry in sorted(directory.iterdir()):
            if not entry.is_file():
                continue

            match = re.match(family["pattern"], entry.name)
            if match:
                found.append(f"{family['prefix']}-{match.group(1)}")

        return found, None

    text = path.read_text(encoding="utf-8", errors="replace")

    if family["kind"] == "jsonl":
        for line_number, line in enumerate(text.splitlines(), start=1):
            line = line.strip()
            if not line:
                continue
            try:
                entry = json.loads(line)
            except json.JSONDecodeError as error:
                return found, f"{family['path']}:{line_number} is not JSON — {error}"

            value = entry.get(family["field"])
            if isinstance(value, str) and value:
                found.append(value)
    else:
        found = re.findall(family["pattern"], text, flags=re.MULTILINE)

    return found, None


def number(identifier: str) -> int | None:
    match = re.search(r"(\d+)$", identifier)
    return int(match.group(1)) if match else None


def check_family(root: Path, family: dict) -> list[str]:
    problems: list[str] = []
    found, error = ids_in_family(root, family)

    if error:
        return [error]

    if not found:
        return [f"{family['path']} allocates no {family['what']} — is the family still real?"]

    # 1. No id handed out twice. THE defect this file is named for.
    for identifier, count in sorted(Counter(found).items()):
        if count > 1:
            problems.append(
                f"{family['prefix']}: {identifier} is claimed by {count} {family['what']} "
                f"in {family['path']} — two trees allocated it independently (DC-013)")

    # 2. No hole, WHERE a hole would mean something. See FAMILIES: for the append-only logs it
    #    means the merge protocol did its job, and reporting that as a failure is how a control
    #    gets ignored.
    numbers = sorted({n for n in (number(i) for i in found) if n is not None})

    if numbers and family.get("contiguous", False):
        expected = set(range(numbers[0], numbers[-1] + 1))
        missing = sorted(expected - set(numbers))

        if missing:
            shown = ", ".join(f"{family['prefix']}-{n:04d}" for n in missing[:8])
            more = "" if len(missing) <= 8 else f" (+{len(missing) - 8} more)"
            problems.append(
                f"{family['prefix']}: the sequence has {len(missing)} hole(s) in "
                f"{family['path']} — {shown}{more}")

    return problems


def undeclared_families(root: Path, declared: set[str]) -> list[str]:
    """Sequences that are being allocated somewhere nobody has told this script about."""
    sightings: dict[str, set[str]] = defaultdict(set)
    homes: dict[str, Counter] = defaultdict(Counter)

    for relative in tracked_text_files(root):
        if relative in NOT_ALLOCATORS:
            continue

        try:
            text = (root / relative).read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue

        for prefix, digits in CANDIDATE.findall(text):
            if prefix in FOREIGN_PREFIXES or prefix.lower() in declared:
                continue

            sightings[prefix].add(f"{prefix}-{digits}")
            homes[prefix][relative] += 1

    problems = []

    for prefix, ids in sorted(sightings.items()):
        if len(ids) < CANDIDATE_THRESHOLD:
            continue

        where = homes[prefix].most_common(1)[0][0]
        problems.append(
            f"'{prefix}-' looks like a monotonic id family ({len(ids)} distinct ids, mostly in "
            f"{where}) and no allocator is declared for it. Two sessions will hand out the same "
            f"one and the file will merge cleanly. Declare it in FAMILIES, or rename it if it is "
            f"not a sequence.")

    return problems


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--self-test", action="store_true",
        help="prove the control fires: plant a duplicate and a hole in a copy, expect failure")
    args = parser.parse_args()

    root = repo_root()

    if args.self_test:
        return self_test(root)

    problems: list[str] = []

    for family in FAMILIES:
        problems.extend(check_family(root, family))

    problems.extend(undeclared_families(root, {f["prefix"].lower() for f in FAMILIES}))

    if problems:
        print("verify-id-allocators: FAILED")
        for problem in problems:
            print(f"  - {problem}")
        print()
        print("  Resolve a duplicate the way the session contract prescribes: keep the id already")
        print("  published on main, re-issue the other, regenerate any derived view.")
        return 1

    counts = []
    for family in FAMILIES:
        found, _ = ids_in_family(root, family)
        counts.append(f"{family['prefix']} {len(found)}")

    print(
        "verify-id-allocators: OK — "
        f"{len(FAMILIES)} declared famil(ies) ({', '.join(counts)}), "
        "no duplicate ids, no holes, no undeclared sequence.")
    return 0


def self_test(root: Path) -> int:
    """The control must be observed FAILING, or it is not a control (CI6)."""
    import tempfile

    family = {
        "prefix": "zz",
        "path": "planted.jsonl",
        "kind": "jsonl",
        "field": "id",
        "what": "planted entries",
        "contiguous": True,
    }

    with tempfile.TemporaryDirectory() as directory:
        planted = Path(directory) / "planted.jsonl"
        planted.write_text(
            '{"id": "zz-0001"}\n'
            '{"id": "zz-0002"}\n'
            '{"id": "zz-0002"}\n'   # the duplicate two trees would produce
            '{"id": "zz-0005"}\n',  # the hole a lost merge would leave
            encoding="utf-8")

        problems = check_family(Path(directory), family)

    duplicate = any("claimed by 2" in p for p in problems)
    hole = any("hole" in p for p in problems)

    for problem in problems:
        print(f"  planted -> {problem}")

    if duplicate and hole:
        print("verify-id-allocators: self-test OK — the control fires on both shapes.")
        return 0

    print("verify-id-allocators: SELF-TEST FAILED — the control did not fire.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
