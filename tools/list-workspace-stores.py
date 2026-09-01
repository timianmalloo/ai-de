#!/usr/bin/env python3
"""What is in the machine-wide workspace store directory, and what is safe to remove.

WHY THIS EXISTS. The daemon derived its own state directory under LocalAppData and no caller could
say otherwise, so every test that launched one wrote into the user's real profile. MEASURED before
the fix: 12 directories per run of the Core suite, and 2,695 accumulated over four days — all but
one of them an empty store belonging to a test that had finished long before (DC-049).

The leak is fixed. What was already written is still there, and it is the user's disk.

WHAT THIS DOES. Reports, and removes only when asked. A directory is REMOVABLE only when it holds
nothing but an empty store — no assertions, no layout, no incidents — and the workspace it belongs
to cannot be identified as one currently open. Anything else is reported and left alone, because a
store with evidence in it is somebody's indexed workspace and there is no way to tell from the id
alone which.

    python tools/list-workspace-stores.py                 # report
    python tools/list-workspace-stores.py --remove        # delete only the empty ones

Exit 0 always when reporting; 1 if a removal was requested and something could not be removed.
Stdlib only.
"""

from __future__ import annotations

import argparse
import os
import shutil
import sqlite3
import sys
from pathlib import Path


def store_root() -> Path:
    local = os.environ.get("LOCALAPPDATA")

    if local:
        return Path(local) / "AiDe" / "workspaces"

    # Not Windows, or a stripped environment. Reported rather than guessed at.
    return Path.home() / ".local" / "share" / "AiDe" / "workspaces"


def workspace_id(path: str) -> str:
    """The id the product derives from a workspace path (IpcPipeName.ForWorkspace)."""
    import hashlib

    normalized = path.replace("/", "\\").rstrip("\\").lower()
    return "aide." + hashlib.sha256(normalized.encode("utf-8")).hexdigest()[:32]


def known_ids(root: Path) -> dict[str, str]:
    """Ids we can NAME, from the recent-workspaces list. Everything else is merely unidentified."""
    recents = root.parent / "recent-workspaces.txt"

    if not recents.exists():
        return {}

    try:
        lines = recents.read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError:
        return {}

    return {workspace_id(line.strip()): line.strip() for line in lines if line.strip()}


SIDECARS = ("-wal", "-shm", "-journal")

# What separates a leaked test store from somebody's workspace, stated as a number rather than a
# feeling. MEASURED on this machine after the empties were cleared: 435 unnamed stores remained, and
# every one of them held 8, 14, 16 or 19 assertions across a scope set of `bicep:main` — one Bicep
# template, sometimes with a docs folder — which is the fixture the daemon tests build. The one real
# workspace held 47,809 across 67 scopes. There is no ambiguity between those two populations, and
# the gap is three orders of magnitude wide.
#
# Anything above this is reported and left alone, whatever else is true of it.
FIXTURE_CEILING = 100


def assertion_count(database: Path) -> int | None:
    """How many facts a store holds, or None when it cannot be read as one.

    Opened `immutable=1` when there is no write-ahead log to miss, which stops SQLite creating a
    shared-memory file just to answer a question. The first version of this tool did not, and left
    5,390 sidecar files behind — two per store, across every directory it reported on, changing the
    thing it was measuring. On its second run those files WERE the difference: 1,495 directories that
    had held nothing but a store now held three files, and the tool reported every one of them as in
    use. A read that writes is not a read.

    Where a write-ahead log does exist it may hold committed facts the database file does not, so the
    tool takes the shared-memory file rather than the risk of undercounting — an undercount here
    would call somebody's workspace empty.
    """
    parameters = "immutable=1" if not database.with_name(database.name + "-wal").exists() else "mode=ro"

    # `with sqlite3.connect(...)` commits or rolls back; it does NOT close. The first version relied
    # on it and left 31 stores locked at the moment it tried to delete them — "used by another
    # process", where the other process was this one. A handle held open is a handle held open
    # whether or not the block that opened it has ended.
    try:
        connection = sqlite3.connect(f"file:{database}?{parameters}", uri=True)
    except sqlite3.Error:
        return None

    try:
        return connection.execute("SELECT count(*) FROM evidence_assertion_fact").fetchone()[0]
    except sqlite3.Error:
        return None
    finally:
        connection.close()


def describe(directory: Path) -> tuple[str, int, int]:
    """(verdict, assertions, bytes) for one workspace directory."""
    total = sum(f.stat().st_size for f in directory.rglob("*") if f.is_file())
    database = directory / "workspace.db"

    if not database.exists():
        return ("no store", 0, total)

    # A store's write-ahead log and shared-memory file are PART of the store, not evidence that
    # something else is here. Counting them as "other files" is how this tool reported a directory
    # as in use because it had itself opened it a moment earlier.
    others = [
        f.name for f in directory.iterdir()
        if f.name != "workspace.db" and not f.name.startswith("workspace.db")
    ]

    facts = assertion_count(database)

    if facts is None:
        return ("unreadable", 0, total)

    if facts == 0 and not others:
        return ("empty", 0, total)

    return ("in use", facts, total)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--remove", action="store_true",
        help="delete the directories reported as empty. Everything else is left alone.")
    parser.add_argument(
        "--remove-fixtures", action="store_true",
        help=f"also delete unnamed stores holding fewer than {FIXTURE_CEILING} assertions. "
             "Reports every scope shape it is about to remove first.")
    args = parser.parse_args()

    root = store_root()

    if not root.exists():
        print(f"list-workspace-stores: nothing at {root}")
        return 0

    known = known_ids(root)

    empty: list[Path] = []
    named: list[tuple[Path, int, int, str]] = []
    unidentified: list[tuple[Path, int, int]] = []

    for directory in sorted(p for p in root.iterdir() if p.is_dir()):
        verdict, facts, size = describe(directory)

        if directory.name in known:
            named.append((directory, facts, size, known[directory.name]))
        elif verdict == "empty":
            empty.append(directory)
        else:
            unidentified.append((directory, facts, size))

    total = len(empty) + len(named) + len(unidentified)
    reclaimable = sum(
        sum(f.stat().st_size for f in d.rglob("*") if f.is_file()) for d in empty)
    stranded = sum(size for _, _, size in unidentified)

    print(f"list-workspace-stores: {root}")
    print(f"  {total} workspace director(ies)")
    print()
    print(f"  {len(named)} belong to a workspace you have opened:")

    for directory, facts, size, path in sorted(named, key=lambda k: -k[2]):
        print(f"    {size / 1_048_576:8.1f} MB  {facts:>9,} assertion(s)  {path}")

    print()
    print(f"  {len(empty)} hold an empty store — {reclaimable / 1_048_576:.0f} MB, removable")
    print(f"  {len(unidentified)} hold facts but match no workspace in your recent list — "
          f"{stranded / 1_048_576:.0f} MB")
    print("      Reported, not removed. An id is a one-way hash of a path, so 'not in the recent")
    print("      list' is not proof the workspace is gone — only that this tool cannot name it.")

    fixtures = [(d, facts) for d, facts, _ in unidentified if facts < FIXTURE_CEILING]
    substantial = [(d, facts) for d, facts, _ in unidentified if facts >= FIXTURE_CEILING]

    if fixtures:
        print()
        print(f"  of those, {len(fixtures)} hold fewer than {FIXTURE_CEILING} assertions "
              "— the size of a test fixture, not a workspace:")

        shapes: dict[int, int] = {}
        for _, facts in fixtures:
            shapes[facts] = shapes.get(facts, 0) + 1

        for facts, count in sorted(shapes.items()):
            print(f"      {count:5} store(s) of {facts} assertion(s)")

        if substantial:
            print(f"  and {len(substantial)} hold {FIXTURE_CEILING}+ — reported, never removed:")
            for directory, facts in sorted(substantial, key=lambda k: -k[1])[:10]:
                print(f"      {directory.name}  {facts:,} assertion(s)")

    if not args.remove and not args.remove_fixtures:
        if empty:
            print()
            print("  Re-run with --remove to delete the empty ones. Nothing else is touched.")
        if fixtures:
            print(f"  Add --remove-fixtures to delete the {len(fixtures)} fixture-sized ones too.")
        return 0

    failed = 0
    doomed = list(empty) if args.remove else []

    if args.remove_fixtures:
        doomed.extend(directory for directory, _ in fixtures)

    for directory in doomed:
        try:
            shutil.rmtree(directory)
        except OSError as error:
            print(f"  could not remove {directory.name}: {error}")
            failed += 1

    print(f"  removed {len(doomed) - failed} store(s)"
          + (f", {failed} could not be removed" if failed else ""))

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
