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
import os
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
        "signature": "shortname",
    },
    {
        "prefix": "cl",
        "path": "docs/audit/change-log.jsonl",
        "kind": "jsonl",
        "field": "id",
        "what": "change entries",
        "contiguous": False,
        "signature": "shortname",
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
# The lookbehind matters more than it looks. Without it a JSON-escaped newline before a
# MENTIONED id — "...\nDC-035 moves from..." inside a log summary — parses as a family
# called "nDC", and the gate reports an undeclared allocator that does not exist. This
# script had already cried wolf once (see the contiguity note in FAMILIES); a control that
# does it twice is one people switch off.
CANDIDATE = re.compile(r"(?<![\\A-Za-z0-9_])([A-Za-z]{2,5})-(\d{3,5})\b")

# Enough sightings to be a sequence rather than a version number or a date fragment.
CANDIDATE_THRESHOLD = 8

# Files that legitimately mention ids without allocating them.
NOT_ALLOCATORS = {
    "docs/audit/audit-data.js",      # derived view of the two logs
    "docs/docs-index.js",            # derived view of the docs graph

    # These are prose ABOUT ids. They allocate `al-`, `cl-` and `DC-`, which are declared families
    # and are checked directly; letting their narrative text nominate NEW families means every id
    # anybody has ever written a sentence about becomes a candidate allocator. A mention is not an
    # allocation — the same distinction that made the first draft read ADR ids out of
    # architecture.md.
    "docs/audit/audit-log.jsonl",
    "docs/audit/change-log.jsonl",
    "docs/lessons/defect-classes.md",
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


def check_family(
    root: Path, family: dict, inherited: set[str] | None = None,
) -> tuple[list[str], list[str]]:
    """Duplicates and holes in one tree. Returns (failures, notes).

    A duplicate the TRUNK already carries is a note, not a failure. It is real and it is reported
    in full every run — but no feature branch introduced it, and failing every branch's build for
    it teaches a whole team that this gate is somebody else's problem, which is how a control stops
    being read. The same scoping the cross-branch half uses: tell the person who can act. `main`'s
    own build still fails, which is where the defect lives.
    """
    problems: list[str] = []
    notes: list[str] = []
    found, error = ids_in_family(root, family)

    if error:
        return [error], notes

    if not found:
        return [f"{family['path']} allocates no {family['what']} — is the family still real?"], notes

    # 1. No id handed out twice. THE defect this file is named for.
    for identifier, count in sorted(Counter(found).items()):
        if count > 1:
            (notes if inherited and identifier in inherited else problems).append(
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

    return problems, notes


def duplicates_on(root: Path, family: dict, trunk: str) -> set[str]:
    """The ids the trunk itself already claims twice — a condition, not a regression."""
    resolved = resolve_trunk(root, trunk)

    if resolved is None:
        return set()

    seen = signatures_at(root, family, resolved)

    if seen is None:
        return set()

    # signatures_at() is a dict, so it cannot count duplicates. Ask the tree directly.
    if family["kind"] == "filename":
        listing = _git(root, ["ls-tree", "--name-only", f"{resolved}:{family['path']}"]) or ""
        numbers = [
            f"{family['prefix']}-{m.group(1)}"
            for m in (re.match(family["pattern"], n) for n in listing.splitlines()) if m]
    else:
        text = _git(root, ["show", f"{resolved}:{family['path']}"]) or ""

        if family["kind"] == "jsonl":
            numbers = []
            for line in text.splitlines():
                line = line.strip()
                if not line:
                    continue
                try:
                    entry = json.loads(line)
                except json.JSONDecodeError:
                    continue
                value = entry.get(family["field"])
                if isinstance(value, str) and value:
                    numbers.append(value)
        else:
            numbers = [m.group(1) for m in
                       (re.match(family["pattern"], line) for line in text.splitlines()) if m]

    return {identifier for identifier, count in Counter(numbers).items() if count > 1}


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


# --------------------------------------------------------------------------------------------
# The collision this file is named for happens BETWEEN trees, and the check above cannot see it.
#
# DC-013 recurred three more times in a single day: DC-054, DC-055 and DC-059 were each allocated
# twice, once in this session's worktree and once in the design session's. Every file involved was
# internally consistent, so `check_family` passed in both trees and went on passing until the two
# branches met. The gate was not wrong. It was looking at one tree.
#
# So compare the trees. For each ref, take the ids it ADDS relative to its own merge base with the
# trunk: an id added by two refs with different content was allocated twice, and an id added here
# that the trunk already spends is already gone. The merge base is what keeps this from crying
# wolf — a stale branch that merely still CONTAINS an old id adds nothing, and a branch already
# merged adds nothing at all. This script has cried wolf twice before (see the contiguity note in
# FAMILIES and the lookbehind on CANDIDATE), and a control people switch off is worse than none.
# --------------------------------------------------------------------------------------------

def _git(root: Path, args: list[str]) -> str | None:
    """Stdout, or None when the command failed — a missing path at a ref is normal, not an error.

    The encoding is stated, not inherited. `text=True` decodes with the locale codec, which on a
    Windows console is cp1252, and every file this reads is UTF-8. The first run of this function
    threw UnicodeDecodeError on a background reader thread for EVERY ref, returned empty output,
    compared nothing, and printed OK — a control that passes by looking at nothing (DC-016). The
    count in `assert_actually_compared` exists because that failure was silent and green.
    """
    out = subprocess.run(
        ["git", *args], capture_output=True, cwd=root,
        encoding="utf-8", errors="replace")

    return out.stdout if out.returncode == 0 else None


def signatures_at(
    root: Path, family: dict, ref: str, working_tree: bool = False,
) -> dict[str, str] | None:
    """id -> what that id SAYS, as of one ref. None when the ref does not carry the family."""
    found: dict[str, str] = {}

    if family["kind"] == "filename":
        if working_tree:
            directory = root / family["path"]
            listing = (chr(10).join(
                sorted(e.name for e in directory.iterdir() if e.is_file()))
                if directory.is_dir() else None)
        else:
            listing = _git(root, ["ls-tree", "--name-only", f"{ref}:{family['path']}"])

        if listing is None:
            return None

        for name in listing.splitlines():
            match = re.match(family["pattern"], name)
            if match:
                # The filename IS the signature. Two sessions numbering a new note 0021 write two
                # different filenames, and that difference is the whole detection.
                found[f"{family['prefix']}-{match.group(1)}"] = name

        return found

    if working_tree:
        # The branch you are ON is read from DISK, not from its last commit. An id you have just
        # written and not yet committed is exactly the one worth being told about — waiting for the
        # commit means the warning arrives after the entry has been written and cited.
        path = root / family["path"]
        text = path.read_text(encoding="utf-8", errors="replace") if path.exists() else None
    else:
        text = _git(root, ["show", f"{ref}:{family['path']}"])

    if text is None:
        return None

    if family["kind"] == "jsonl":
        for line in text.splitlines():
            line = line.strip()
            if not line:
                continue
            try:
                entry = json.loads(line)
            except json.JSONDecodeError:
                continue
            identifier = entry.get(family["field"])
            if isinstance(identifier, str) and identifier:
                found[identifier] = str(entry.get(family.get("signature", ""), ""))[:120]
    else:
        # The heading kind. The signature is the title AFTER the id: nobody names the same lesson
        # twice, so two independent `### DC-054` headings differ in every character but the number.
        #
        # Read line by line rather than with findall. The family patterns capture the id, and
        # findall returns the CAPTURE rather than the match — so the first version of this got
        # "DC-054" where it wanted the whole heading, failed to re-match it, and read zero ids out
        # of a file holding sixty. It reported OK. Only DC is a heading family, so the other four
        # kept working and the aggregate guard stayed quiet, which is why that guard is now
        # per-family.
        for line in text.splitlines():
            match = re.match(family["pattern"], line)
            if match:
                found[match.group(1)] = line[match.end():].strip()[:120]

    return found


def resolve_trunk(root: Path, trunk: str) -> str | None:
    """The trunk as a ref that exists here, or None.

    CI checks out a detached HEAD at the pushed commit, so there is no local `main` — only
    `origin/main`. This check is most valuable exactly there: the workflow runs on every branch
    push with full history, so every session's branch is present as a remote ref and a collision
    is reported to the branch that introduced it rather than to whoever merges next. Getting this
    fallback wrong would have made it a no-op in the one place it matters most.
    """
    present = [
        candidate for candidate in (trunk, f"origin/{trunk}")
        if _git(root, ["rev-parse", "--verify", "--quiet", f"{candidate}^{{commit}}"])]

    if not present:
        return None

    # Whichever is FURTHER AHEAD. A local `main` left behind while `origin/main` moved on makes
    # every id the remote has published look newly allocated by whoever rebased onto it — this
    # branch reported three of its own inherited entries as collisions the first time that
    # happened. The trunk is "what has been published", not "what this checkout last fetched".
    best = present[0]

    for candidate in present[1:]:
        if _git(root, ["merge-base", "--is-ancestor", best, candidate]) is not None:
            best = candidate

    return best


def interesting_refs(root: Path, trunk: str) -> list[str]:
    """Branches that could be allocating right now."""
    listing = _git(root, [
        "for-each-ref", "--format=%(refname:short)", "refs/heads", "refs/remotes"]) or ""

    # Both spellings of the trunk are excluded, not just the resolved one: comparing `main` against
    # `origin/main` produces a pair of refs that agree about everything and says nothing.
    trunk_name = trunk.split("/")[-1]

    keep = []

    for line in listing.splitlines():
        ref = line.strip()

        if not ref or ref == "origin" or ref.endswith("/HEAD"):
            continue

        if ref.split("/")[-1] == trunk_name:
            continue

        keep.append(ref)

    return keep


def current_branch(root: Path) -> set[str]:
    """Every name for the branch this checkout is on — local, remote, and CI's own.

    Two branches colliding with each other is not a defect in a THIRD branch, and failing that
    third build teaches everyone that this gate is noise somebody else has to fix. Each colliding
    branch fails its own build, where the person who can re-issue the id is looking.
    """
    names: set[str] = set()

    head = (_git(root, ["rev-parse", "--abbrev-ref", "HEAD"]) or "").strip()

    if head and head != "HEAD":
        names.add(head)

    # CI checks out a detached HEAD, so the branch name arrives in the environment instead.
    for variable in ("GITHUB_REF_NAME", "GITHUB_HEAD_REF"):
        value = os.environ.get(variable, "").strip()
        if value:
            names.add(value)

    if not names:
        listing = _git(root, ["for-each-ref", "--format=%(refname:short)", "--points-at", "HEAD"])
        for line in (listing or "").splitlines():
            ref = line.strip()
            if ref and not ref.endswith("/HEAD"):
                names.add(ref.split("/", 1)[-1] if ref.startswith("origin/") else ref)

    return names


def across_refs(
    root: Path, families: list[dict], trunk: str, mine: set[str],
) -> tuple[list[str], list[str]]:
    problems: list[str] = []
    notes: list[str] = []

    resolved = resolve_trunk(root, trunk)

    if resolved is None:
        # Said out loud, and not a failure. A clone with no trunk genuinely cannot do this, and
        # failing there would be this script's third false alarm. Printing it is what keeps the
        # skip from becoming the silent no-op the guard below exists to catch.
        return [], [
            f"neither '{trunk}' nor 'origin/{trunk}' is here — no cross-branch check ran"]

    trunk = resolved

    spent = {f["prefix"]: (signatures_at(root, f, trunk) or {}) for f in families}

    # Every family the trunk carries must yield ids here. A family that reads as empty is not a
    # family with no collisions, it is a reader that has stopped seeing — and it would report OK
    # forever (DC-016). Per family, not in aggregate: four working readers hid a fifth blind one.
    blind = [
        f["prefix"] for f in families
        if not spent[f["prefix"]] and signatures_at(root, f, trunk) is not None]

    if blind:
        return [
            f"no {prefix}- ids could be read from {trunk}, though the family is there — the "
            f"cross-branch check is looking at nothing for {prefix} and would report OK "
            "regardless (DC-016)"
            for prefix in blind], notes
    added: dict[str, dict[str, list[tuple[str, str]]]] = {f["prefix"]: {} for f in families}

    for ref in interesting_refs(root, trunk):
        base = (_git(root, ["merge-base", ref, trunk]) or "").strip()
        if not base:
            continue  # unrelated history; nothing to say about it

        for family in families:
            here = signatures_at(root, family, ref, working_tree=_is_mine(ref, mine))
            if here is None:
                continue

            inherited = signatures_at(root, family, base) or {}

            for identifier, signature in here.items():
                if identifier in inherited:
                    continue  # not new on this branch — it came with the merge base

                taken = spent[family["prefix"]].get(identifier)

                if taken is not None and taken != signature:
                    (problems if _is_mine(ref, mine) else notes).append(
                        f"{identifier} is allocated on {ref} as {signature!r}, but {trunk} already "
                        f"spends it on {taken!r} — re-issue the one on {ref} before it merges "
                        f"(DC-013)")
                    continue

                added[family["prefix"]].setdefault(identifier, []).append((ref, signature))

    for family in families:
        for identifier, raw in sorted(added[family["prefix"]].items()):
            # `session/x` and `origin/session/x` are one branch making one claim. Counting them
            # separately turns two colliding sessions into "3 branches", and a control that
            # cannot count is one people stop reading.
            claims = list({ref.split("/", 1)[-1] if ref.startswith("origin/") else ref: (ref, sig)
                           for ref, sig in raw}.values())

            if len(claims) > 1 and len({signature for _, signature in claims}) > 1:
                where = "; ".join(f"{ref} as {signature!r}" for ref, signature in claims)
                involves_me = any(_is_mine(ref, mine) for ref, _ in claims)
                (problems if involves_me else notes).append(
                    f"{identifier} is being allocated independently on {len(claims)} branches — "
                    f"{where}. Every one of those files is internally consistent and every one "
                    f"will merge cleanly; re-issue all but the one that reaches {trunk} first "
                    "(DC-013)")

    return problems, notes


def _is_mine(ref: str, mine: set[str]) -> bool:
    """Whether a ref names the branch this checkout is on, under any of its spellings."""
    bare = ref.split("/", 1)[-1] if ref.startswith("origin/") else ref
    return bare in mine or ref in mine


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--trunk", default="main",
        help="the branch an id is spent on once it lands there (default: main)")
    parser.add_argument(
        "--this-tree-only", action="store_true",
        help="skip the cross-branch check (a fresh CI clone has only one branch to compare)")
    parser.add_argument(
        "--self-test", action="store_true",
        help="prove the control fires: plant a duplicate and a hole in a copy, expect failure")
    args = parser.parse_args()

    root = repo_root()

    if args.self_test:
        return self_test(root)

    problems: list[str] = []

    notes: list[str] = []

    # On the trunk itself nothing is "inherited" — the buck stops here, and main's own build is
    # exactly where a duplicate already on main should be failing. Without this the downgrade would
    # apply everywhere and the defect would be a note nobody's build ever refuses.
    on_trunk = args.trunk.split("/")[-1] in current_branch(root)

    for family in FAMILIES:
        inherited = (set() if args.this_tree_only or on_trunk
                     else duplicates_on(root, family, args.trunk))
        failures, inherited_notes = check_family(root, family, inherited)
        problems.extend(failures)
        notes.extend(
            n + f" — already on {args.trunk}, so no branch introduced it; it is reported here "
                "and fails main's own build"
            for n in inherited_notes)

    problems.extend(undeclared_families(root, {f["prefix"].lower() for f in FAMILIES}))

    if not args.this_tree_only:
        found, cross_notes = across_refs(
            root, FAMILIES, args.trunk, current_branch(root))
        problems.extend(found)
        notes.extend(cross_notes)

    # Printed whether or not this run fails, and BEFORE the verdict. A collision between two other
    # branches is real, is not this branch's to fix, and must still be visible to whoever is
    # reading — silently dropping it would be the narrowness that started this file.
    for note in notes:
        print(f"  note: {note}")

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

    resolved = resolve_trunk(root, args.trunk)

    scope = ("this tree only" if args.this_tree_only or resolved is None
             else f"{len(interesting_refs(root, resolved))} branch(es) compared against {resolved}")

    print(
        "verify-id-allocators: OK — "
        f"{len(FAMILIES)} declared famil(ies) ({', '.join(counts)}), "
        f"no duplicate ids, no holes, no undeclared sequence, {scope}.")
    return 0


def self_test_across_refs(root: Path) -> int:
    """The cross-branch check, observed failing on the shape that actually happened.

    Built as a throwaway repository rather than by branching this one: the check reads refs, so a
    fixture that plants a branch HERE would leave a branch behind on a failure, and this session
    already lost work twice to commands that reached outside their tree.
    """
    import tempfile

    family = {
        "prefix": "DC",
        "path": "register.md",
        "kind": "heading",
        "pattern": r"^###\s+(DC-\d+)\b",
        "what": "defect classes",
        "contiguous": True,
    }

    with tempfile.TemporaryDirectory() as directory:
        place = Path(directory)
        register = place / "register.md"

        def commit(text: str, message: str) -> None:
            register.write_text(text, encoding="utf-8")
            subprocess.run(["git", "add", "-A"], cwd=place, capture_output=True, check=True)
            subprocess.run(
                ["git", "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-m", message],
                cwd=place, capture_output=True, check=True)

        subprocess.run(
            ["git", "init", "-q", "-b", "main"], cwd=place, capture_output=True, check=True)

        commit("### DC-001 - the one they already share\n", "base")

        # Two sessions, each in its own tree, each reaching for the next free number. Both files
        # are internally consistent, which is exactly why the single-tree check passes in both.
        subprocess.run(["git", "checkout", "-q", "-b", "session-a"], cwd=place, capture_output=True)
        commit("### DC-001 - the one they already share\n### DC-002 - a pane hides its stack\n", "a")

        subprocess.run(["git", "checkout", "-q", "main"], cwd=place, capture_output=True)
        subprocess.run(["git", "checkout", "-q", "-b", "session-b"], cwd=place, capture_output=True)
        commit("### DC-001 - the one they already share\n### DC-002 - a status line has no cap\n", "b")

        single_tree, _ = check_family(place, family)

        # On session-b's own checkout the collision is session-b's to fix, so it must FAIL.
        mine, _ = across_refs(place, [family], "main", {"session-b"})

        # On an unrelated branch the same collision is real and is somebody else's; it must be
        # reported and must NOT fail that build. Getting this backwards makes every session's
        # gate red until whichever other session happens to fix theirs.
        theirs_failed, theirs_noted = across_refs(place, [family], "main", {"session-c"})

    for problem in mine + theirs_noted:
        print(f"  planted -> {problem}")

    if theirs_failed:
        print("verify-id-allocators: SELF-TEST FAILED — a collision between two OTHER branches "
              "failed an unrelated build; that is the collateral this scoping exists to stop.")
        return 1

    if not any("DC-002" in n for n in theirs_noted):
        print("verify-id-allocators: SELF-TEST FAILED — a collision between two other branches "
              "was dropped instead of reported as a note.")
        return 1

    cross = mine

    if single_tree:
        print("verify-id-allocators: SELF-TEST FAILED — the single-tree check should NOT fire "
              "here; each branch's file is internally consistent, which is the whole point.")
        return 1

    if any("DC-002" in p and "independently on 2 branches" in p for p in cross):
        print("verify-id-allocators: cross-branch self-test OK — the control fires on the shape "
              "that produced DC-054, DC-055 and DC-059, and the single-tree check does not.")
        return 0

    print("verify-id-allocators: SELF-TEST FAILED — two branches allocated DC-002 independently "
          "and the cross-branch check did not fire.")
    return 1


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

        problems, _ = check_family(Path(directory), family)

    duplicate = any("claimed by 2" in p for p in problems)
    hole = any("hole" in p for p in problems)

    for problem in problems:
        print(f"  planted -> {problem}")

    if duplicate and hole:
        print("verify-id-allocators: self-test OK — the control fires on both shapes.")
        return self_test_across_refs(root)

    print("verify-id-allocators: SELF-TEST FAILED — the control did not fire.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
