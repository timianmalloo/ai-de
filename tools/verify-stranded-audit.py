#!/usr/bin/env python3
"""An append-only log edited in a tree nobody is working in is the only copy of that work.

WHAT HAPPENED (§8.7). `prompt-log.py` was run as a session's first command, before its worktree
existed, so it appended to the PRIMARY checkout's `docs/audit/audit-log.jsonl`. That left one entry
as an uncommitted modification in a tree nobody was working in. It surfaced only because it blocked
a fast-forward, and the one-keystroke way to unblock a fast-forward — `git checkout --` on the
offending file — would have deleted it. The log is append-only, so a dirty line in it is almost
never a change to something; it is a record that exists nowhere else.

WHY THE OBVIOUS FIX WAS REJECTED. Both sessions first proposed patching `audit-log.py` to refuse a
target outside the caller's toplevel. `audit-log.py` is a LISTED pack artifact and `/updatepack`
replaces listed artifacts wholesale, so that patch is one pack update away from vanishing silently —
leaving a control everybody believes exists. Adding an unlisted file is safe; modifying a listed one
is not, and those are opposite rules rather than one. Hence a repo-owned gate here instead.

WHY THE EXISTING TOOLING CANNOT SEE IT. `coord-core.worktree_safety()` returns
`"primary checkout - the reference tree is never cleanup"` **before** any dirtiness test
(coord-core.py:910). The one tree whose dirty state it never examines is exactly where a stranded
write lands, because a session that has not created its worktree yet is standing in the primary.

WHAT THIS CHECKS.
  * The PRIMARY checkout, always. No session should be working there under WT discipline, so a
    dirty append-only log in it is the hazard by definition and needs no liveness signal.
  * Every OTHER worktree, only when nobody is live in it — read from `.agents/log/*.jsonl` with
    coord's own 8-hour window. Your own tree, mid-turn, legitimately has a dirty log; a gate that
    fires on that is muted within a week, which is the lesson `verify-id-allocators` already had to
    be taught.

Pre-commit and on demand, NOT CI: a runner has one checkout, so it structurally cannot see this.

Exit 0 when clean, 1 otherwise. Stdlib only.
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import time
from pathlib import Path

# The append-only logs. A dirty line in one of these is a record, not an edit.
APPEND_ONLY = ("docs/audit/audit-log.jsonl", "docs/audit/change-log.jsonl")

# coord-core.py's own window, so the two tools agree about what "live" means.
STALE_SECONDS = 8 * 3600


def _git(args: list[str], cwd: Path | None = None) -> str:
    out = subprocess.run(
        ["git", *args], capture_output=True, cwd=cwd, encoding="utf-8", errors="replace")

    return out.stdout if out.returncode == 0 else ""


def worktrees(root: Path) -> list[tuple[Path, bool]]:
    """Every worktree, and whether it is the primary. The primary is the first git lists."""
    trees: list[Path] = []

    for line in _git(["worktree", "list", "--porcelain"], root).splitlines():
        if line.startswith("worktree "):
            trees.append(Path(line[len("worktree "):].strip()))

    return [(t, i == 0) for i, t in enumerate(trees)]


def dirty_logs(tree: Path) -> list[str]:
    """Which append-only logs have uncommitted changes in this tree."""
    found = []

    for relative in APPEND_ONLY:
        if not (tree / relative).exists():
            continue

        # --porcelain prints nothing for a clean path, whatever its state otherwise.
        if _git(["status", "--porcelain", "--", relative], tree).strip():
            found.append(relative)

    return found


def live_trees(root: Path, now: float) -> set[str]:
    """Worktree paths a session has recently claimed, from coord's own log."""
    live: set[str] = set()
    log = root / ".agents" / "log"

    if not log.is_dir():
        return live

    for entry in log.glob("*.jsonl"):
        try:
            text = entry.read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue

        for line in text.splitlines():
            line = line.strip()
            if not line:
                continue

            try:
                record = json.loads(line)
            except json.JSONDecodeError:
                continue

            when = record.get("at") or record.get("ts") or record.get("time")
            where = record.get("worktree") or record.get("cwd") or record.get("path")

            if not where:
                continue

            stamp = _seconds(when)

            if stamp is not None and now - stamp < STALE_SECONDS:
                live.add(_key(Path(str(where))))

    return live


def _seconds(value) -> float | None:
    if isinstance(value, (int, float)):
        return float(value)

    if isinstance(value, str):
        try:
            from datetime import datetime

            return datetime.fromisoformat(value.replace("Z", "+00:00")).timestamp()
        except ValueError:
            return None

    return None


def _key(path: Path) -> str:
    try:
        return os.path.normcase(str(path.resolve()))
    except OSError:
        return os.path.normcase(str(path))


def check(root: Path, now: float | None = None) -> list[str]:
    now = time.time() if now is None else now
    live = live_trees(root, now)
    problems: list[str] = []

    for tree, is_primary in worktrees(root):
        if not tree.exists():
            continue

        found = dirty_logs(tree)

        if not found:
            continue

        # A live session's own tree is allowed to be mid-write. The primary is not: under worktree
        # discipline nobody works there, so a dirty append-only log in it was written by a session
        # standing somewhere it was not going to commit from.
        if not is_primary and _key(tree) in live:
            continue

        who = "the PRIMARY checkout" if is_primary else "a worktree nobody is live in"

        problems.append(
            f"{tree} — {who} has uncommitted changes to {', '.join(found)}.\n"
            f"      These logs are APPEND-ONLY, so those lines are probably the only copy of that\n"
            f"      work and exist in no other tree. Commit them IN THAT TREE.\n"
            f"      Do NOT run `git checkout --` on them to unblock a merge: that is how the entry\n"
            f"      gets deleted, and nothing reports it.")

    return problems


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--self-test", action="store_true",
        help="prove the control fires: plant a dirty log in a primary checkout")
    args = parser.parse_args()

    root = Path(_git(["rev-parse", "--show-toplevel"]).strip() or ".")

    if args.self_test:
        return self_test()

    problems = check(root)

    if problems:
        print("verify-stranded-audit: FAILED")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    print(f"verify-stranded-audit: OK — {len(worktrees(root))} worktree(s), no append-only log "
          "left uncommitted in a tree nobody is working in.")
    return 0


def self_test() -> int:
    """The control must be observed FAILING, or it is not a control (CI6)."""
    import tempfile

    with tempfile.TemporaryDirectory() as directory:
        place = Path(directory)
        audit = place / "docs" / "audit"
        audit.mkdir(parents=True)

        subprocess.run(["git", "init", "-q"], cwd=place, capture_output=True, check=True)

        log = audit / "audit-log.jsonl"
        log.write_text('{"id": "al-0001"}\n', encoding="utf-8")

        subprocess.run(["git", "add", "-A"], cwd=place, capture_output=True, check=True)
        subprocess.run(
            ["git", "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-m", "base"],
            cwd=place, capture_output=True, check=True)

        clean = check(place)

        # The stranding: one more entry, appended and never committed.
        log.write_text('{"id": "al-0001"}\n{"id": "al-0002"}\n', encoding="utf-8")

        stranded = check(place)

    for problem in stranded:
        print(f"  planted -> {problem.splitlines()[0]}")

    if clean:
        print("verify-stranded-audit: SELF-TEST FAILED — fired on a clean primary checkout, which "
              "is how a gate gets muted.")
        return 1

    if not any("PRIMARY" in p for p in stranded):
        print("verify-stranded-audit: SELF-TEST FAILED — an uncommitted append-only log in the "
              "primary checkout was not reported.")
        return 1

    print("verify-stranded-audit: self-test OK — a stranded entry fails, a clean tree does not.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
