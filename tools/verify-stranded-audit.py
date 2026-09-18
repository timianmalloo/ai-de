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

WHAT IT REFUSES, AND WHAT IT ONLY REPORTS. Ruling 129 (c) — this EXTENDS the design above; it is
not a reading of it. A finding is a REFUSAL only where the session running the gate can act on it:

    the PRIMARY checkout                        -> FAIL   (nobody should be working there)
    a tree THIS session is working in           -> FAIL   (it can commit there, now)
    a tree another session is live in           -> quiet  (legitimately mid-write, as before)
    a STALE tree belonging to another session   -> REPORT, exit 0

Repo-wide and fail-closed, one session's uncommitted ledger in its own tree stopped EVERY other
session's join in the primary — two joins in one night. For a LINKED tree this gate is a DETECTOR,
not a preventer: the actual loss path — `git checkout --` on the file, or removing the tree — is
already refused by `coord worktree cleanup`, which will not remove a tree that is dirty. So the only
thing the refusal bought there was a stop sign in front of people who could not act on it, which is
exactly the muting pressure lines 26-28 warn about. The report keeps every bit of the detection:
the named tree, the session ids from its own `.agents/log/*.jsonl`, the last coord timestamp, and
the same remedy — it just does not fail a join that has nothing to do with it.

Pre-commit and on demand, NOT CI: a runner has one checkout, so it structurally cannot see this.

Exit 0 when clean or when only foreign stale trees were reported, 1 otherwise. Stdlib only.
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


def session_trees(primary: Path, here: Path) -> set[str]:
    """The trees the RUNNING session is in.

    Two sources, because a session may hold more than one tree (WT: more trees are fine when the
    work needs them): the tree this gate was started from, and every tree named by $AGENT_SESSION's
    own coord log in the primary. Staleness is NOT applied here — a long turn whose last coord
    record aged past the 8-hour window is still this session's tree, and it is still the session
    that can commit in it. That is the case the severity is kept for.
    """
    keys = {_key(here)}
    session = os.environ.get("AGENT_SESSION")

    if not session:
        return keys

    log = primary / ".agents" / "log" / f"{session}.jsonl"

    try:
        text = log.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return keys

    for line in text.splitlines():
        line = line.strip()

        if not line:
            continue

        try:
            record = json.loads(line)
        except json.JSONDecodeError:
            continue

        if not isinstance(record, dict):
            continue

        where = record.get("worktree") or record.get("cwd") or record.get("path")

        if where:
            keys.add(_key(Path(str(where))))

    return keys


def tree_sessions(tree: Path) -> list[tuple[str, float | None]]:
    """(session id, last coord timestamp) for every session named in THIS tree's own .agents/log.

    Read from the tree being reported, not from the primary: the question the reader has is "whose
    tree is this, and when did they last touch it", and the tree's own log is what answers it. In a
    linked worktree that log is the committed snapshot, which is exactly the provenance available —
    a reported tree with no log degrades to "not recorded", never to a guessed owner (IO12).
    """
    found: dict[str, float | None] = {}
    log = tree / ".agents" / "log"

    if not log.is_dir():
        return []

    for entry in sorted(log.glob("*.jsonl")):
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

            if not isinstance(record, dict):
                continue

            who = record.get("session") or entry.stem
            stamp = _seconds(record.get("at") or record.get("ts") or record.get("time"))
            previous = found.get(who)

            if who not in found or (stamp is not None and (previous is None or stamp > previous)):
                found[who] = stamp

    return sorted(found.items(), key=lambda item: (item[1] is None, -(item[1] or 0), item[0]))


def _stamp_text(stamp: float | None) -> str:
    """A timestamp an operator can compare against, or the words that say it is missing."""
    if stamp is None:
        return "not recorded"

    return time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime(stamp))


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


def check(root: Path, now: float | None = None,
          running: Path | None = None) -> tuple[list[str], list[str]]:
    """(refusals, reports). `running` is the tree the gate was started from — a parameter so the
    self-test can stand the gate in one tree and plant the dirt in another, which is the whole
    distinction Ruling 129 (c) turns on and therefore the one thing that must be provable."""
    now = time.time() if now is None else now
    trees = worktrees(root)
    # The session log is per REPOSITORY: coord-core.py writes it under the PRIMARY checkout, and a
    # linked worktree carries only the committed snapshot. Reading `root/.agents/log` from a linked
    # tree therefore saw no session as live and reported every other tree as stranded — first observed
    # 2026-09-15 with four registered sessions (Codex, `req-01M2JQY0G3…`), and by the conductor the
    # same hour. Resolve against the primary, as coord-core.py does.
    primary = trees[0][0] if trees else root
    live = live_trees(primary, now)
    ours = session_trees(primary, root if running is None else running)
    problems: list[str] = []
    reports: list[str] = []

    # The same three sentences end every finding, refused or reported: the detection did not change,
    # only who is being asked to act on it.
    remedy = (
        "      These logs are APPEND-ONLY, so those lines are probably the only copy of that\n"
        "      work and exist in no other tree. Commit them IN THAT TREE.\n"
        "      Do NOT run `git checkout --` on them to unblock a merge: that is how the entry\n"
        "      gets deleted, and nothing reports it.")

    for tree, is_primary in trees:
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

        # REFUSE ONLY WHERE THE RUNNING SESSION CAN ACT (Ruling 129 (c)). The primary is everyone's
        # to fix and nobody's to work in. This session's own tree it can commit in right now — and
        # it reaches here only when its own liveness has aged out, which is the case worth keeping
        # loud. Another session's stale tree it cannot commit in at all.
        if is_primary or _key(tree) in ours:
            who = ("the PRIMARY checkout" if is_primary
                   else "a worktree THIS session is working in")

            problems.append(
                f"{tree} — {who} has uncommitted changes to {', '.join(found)}.\n" + remedy)
            continue

        # A STALE FOREIGN TREE: report it with everything needed to find its owner, and exit 0.
        # Failing here stops joins that cannot fix it (two in one night), and a gate that stops
        # unrelated work is a gate that gets skipped — lines 26-28. Nothing is hidden: the tree, its
        # sessions and their last coord record are printed on the normal path, no flag (IO2).
        # NAMED, NOT DUMPED. The first run of this report printed all 144 sessions a long-lived
        # tree's committed .agents/log carried, which is the same as printing none: the reader
        # wants who to route this to, and that is the most recent few plus how many more there are.
        sessions = tree_sessions(tree)
        recent = sessions[:3]
        named = ", ".join(f"{who} ({_stamp_text(stamp)})" for who, stamp in recent)

        if len(sessions) > len(recent):
            named += f", and {len(sessions) - len(recent)} more"

        if not sessions:
            named = "not recorded — the tree carries no .agents/log/*.jsonl"

        reports.append(
            f"{tree} — a STALE worktree belonging to another session has uncommitted changes to "
            f"{', '.join(found)}.\n"
            f"      sessions in its .agents/log ({len(sessions)}), most recent first: {named}\n"
            f"      last coord record: {_stamp_text(sessions[0][1] if sessions else None)}\n"
            + remedy + "\n"
            "      REPORTED, NOT REFUSED (Ruling 129 (c)): this session cannot commit in another\n"
            "      session's tree, so refusing here would only stop joins that cannot fix it. For\n"
            "      a linked tree this gate is a DETECTOR, not a preventer — the loss path itself\n"
            "      (`git checkout --`, or removing the tree) is already refused by\n"
            "      `coord worktree cleanup`, which will not remove a tree that is dirty.")

    return problems, reports


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--self-test", action="store_true",
        help="prove the control fires: plant a dirty log in a primary checkout")
    args = parser.parse_args()

    root = Path(_git(["rev-parse", "--show-toplevel"]).strip() or ".")

    if args.self_test:
        return self_test()

    problems, reports = check(root)

    # Printed FIRST and on both paths: a report that only appears when the gate is otherwise green
    # is a report nobody reads on the day it matters.
    if reports:
        print(f"verify-stranded-audit: {len(reports)} REPORT(S) — a stale tree this session cannot "
              "commit in. Not a refusal; route it to the tree's owner.")
        for report in reports:
            print(f"  ! {report}")

    if problems:
        print("verify-stranded-audit: FAILED")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    print(f"verify-stranded-audit: OK — {len(worktrees(root))} worktree(s), {len(reports)} "
          "reported, no append-only log left uncommitted in a tree this session can commit in.")
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

        clean, clean_reports = check(place, running=place)

        # The stranding: one more entry, appended and never committed.
        log.write_text('{"id": "al-0001"}\n{"id": "al-0002"}\n', encoding="utf-8")

        stranded, _ = check(place, running=place)

        # The linked-worktree case: a session registered in the PRIMARY's log is live in its own
        # tree, and the gate must see that even when it runs FROM that linked tree (whose own
        # .agents/log is only the committed snapshot). Restore the primary first so it is clean.
        log.write_text('{"id": "al-0001"}\n', encoding="utf-8")
        linked = place.parent / (place.name + "-linked")
        subprocess.run(["git", "worktree", "add", "-q", "-b", "linked", str(linked)],
                       cwd=place, capture_output=True, check=True)
        (linked / "docs" / "audit" / "audit-log.jsonl").write_text(
            '{"id": "al-0001"}\n{"id": "al-0003"}\n', encoding="utf-8")

        unregistered, _ = check(linked, running=linked)

        # RULING 129 (c): the SAME dirty linked tree, seen from the PRIMARY by a different session.
        # Identical dirt, opposite severity — that is the whole change, so it is proven by moving
        # only where the gate is standing. Its own .agents/log is planted so the report can be held
        # to naming the session and its last coord record, not merely to staying quiet.
        (linked / ".agents" / "log").mkdir(parents=True)
        (linked / ".agents" / "log" / "codex-foreign.jsonl").write_text(json.dumps({
            "kind": "claim", "session": "codex-foreign", "at": time.time() - 30 * 3600,
            "worktree": str(linked)}) + "\n", encoding="utf-8")

        foreign_problems, foreign_reports = check(place, running=place)

        (place / ".agents" / "log").mkdir(parents=True)
        (place / ".agents" / "log" / "s.jsonl").write_text(json.dumps({
            "kind": "session-start", "session": "s", "at": time.time(), "worktree": str(linked)}) + "\n",
            encoding="utf-8")

        registered, registered_reports = check(linked, running=linked)

        subprocess.run(["git", "worktree", "remove", "--force", str(linked)],
                       cwd=place, capture_output=True, check=True)

    for problem in stranded:
        print(f"  planted -> {problem.splitlines()[0]}")

    if clean or clean_reports:
        print("verify-stranded-audit: SELF-TEST FAILED — fired on a clean primary checkout, which "
              "is how a gate gets muted.")
        return 1

    if not any("PRIMARY" in p for p in stranded):
        print("verify-stranded-audit: SELF-TEST FAILED — an uncommitted append-only log in the "
              "primary checkout was not reported.")
        return 1

    if not any("THIS session is working in" in p for p in unregistered):
        print("verify-stranded-audit: SELF-TEST FAILED — a dirty log in the running session's own "
              "linked worktree, with no registered session, was not refused.")
        return 1

    # The foreign-stale case, in three parts: it does not fail, it IS reported, and the report
    # carries the provenance a reader needs to route it. A silent exit 0 would pass the first check
    # and be a worse gate than the one it replaced.
    if foreign_problems:
        print("verify-stranded-audit: SELF-TEST FAILED — a STALE FOREIGN worktree refused the run. "
              "That is the blast radius Ruling 129 (c) removed:")
        for problem in foreign_problems:
            print(f"  {problem.splitlines()[0]}")
        return 1

    if not any("STALE worktree belonging to another session" in r for r in foreign_reports):
        print("verify-stranded-audit: SELF-TEST FAILED — a dirty append-only log in a stale foreign "
              "worktree was not reported at all. Demoting a finding to silence is not narrowing it.")
        return 1

    if not any("codex-foreign" in r and "last coord" in r for r in foreign_reports):
        print("verify-stranded-audit: SELF-TEST FAILED — the report named neither the session ids "
              "from the tree's own .agents/log nor a last coord record, so nobody can route it.")
        return 1

    if registered or registered_reports:
        print("verify-stranded-audit: SELF-TEST FAILED — a session registered in the PRIMARY's log "
              "was reported stranded when the gate ran from its own linked worktree:")
        for problem in registered + registered_reports:
            print(f"  {problem.splitlines()[0]}")
        return 1

    print("verify-stranded-audit: self-test OK — a stranded entry fails, a clean tree does not, a "
          "session registered in the primary's log is live from any worktree, and the SAME dirty "
          "linked tree seen from the primary by another session is reported with its sessions and "
          "last coord record, exit 0.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
