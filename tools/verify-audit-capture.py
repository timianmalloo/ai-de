#!/usr/bin/env python3
"""A skill run must declare what it was for and what it verified. Existing debt is frozen.

WHY THIS EXISTS. Daydream, the leaderboard and the scorer all read Work Episodes, and an episode is
an audit entry carrying `goal`, `done_when` and `session` — that is not a convention, it is
`AuditLogEpisodeSource`'s actual rule, and this gate derives its own from the same three fields
rather than restating one (DC-021).

MEASURED 2026-09-03, and the first gap is the larger one:

    kind=skill entries                          292
      carrying goal + done_when                  33      <- the rest are NEVER episodes
    episode-shaped entries                      111
      carrying evidence (signals or proof)       14      <- the rest are never assessable

So 259 skill runs cannot be scored at all, and 97 of the ones that can carry nothing to assess.
Daydream's output over this repository's whole history is one observation, and with a recurrence
threshold of two that means zero candidates, forever, however good the engine is.

AL5b already requires the first half in prose — "a substantive turn records its goal-state in the
audit log" — and 259 entries say what prose is worth (CI6). The second half has had CLI flags the
whole time: `--signal-verification-path`, `--signal-acceptance-met` and friends, used once in 292
entries. This is a discipline gap with the affordance already built, which is the only kind a gate
can fix.

WHAT COUNTS AS EVIDENCE, INCLUDING THE HONEST ZERO:

    a docs/proof/ artifact          the strongest form
    a signals object                including --signal-verification-path false

The last one matters more than it looks. Many turns genuinely verify nothing, and forcing a Proof
Pack onto those would be a false positive on a push gate, which is how a gate gets switched off
(DC-104). "There was no verification path" is a true statement and recording it is capture — what is
refused is SILENCE, because an absent signal and an honest zero are the two states this whole
repository keeps learning not to render alike (DC-025).

WHY A RATCHET AND NOT A RULE. Rewriting 259 historical entries is impossible — the log is
append-only and the goals are gone. So everything at or before WATERMARK is frozen and the gate
fails only on new debt. Raising the watermark is how someone would hide debt; the run prints the
frozen count so a raise shows up in the diff, and there is no honest reason to move it.

Exit 0 when clean, 1 on new debt, 2 when the log cannot be read. Stdlib only.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LOG = ROOT / "docs" / "audit" / "audit-log.jsonl"

# Frozen at the tip on 2026-09-03, the commit that introduced this gate. Everything at or before is
# pre-existing debt: the log is append-only and those goals no longer exist to be recorded.
WATERMARK = 449

ID_RE = re.compile(r"^al-(\d+)$")

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


def is_frozen(entry: dict) -> bool:
    """Whether this entry predates the ratchet and is therefore exempt.

    THE ID SCHEME CHANGED UNDER THIS GATE. It was written when every id was `al-NNNN`, and the
    AI-Forward Pack r59 update switched new entries to a ULID — `al-01M1N21F1JKT7VFFBRQHER84T5`.
    The first version parsed `al-(\\d+)` and treated anything else as unrecognised, which it SKIPPED.
    So the gate reported "7 checked" while silently ignoring every entry written in the new scheme,
    including the one recording the change that broke it.

    That is DC-103 — a check whose scope silently stops covering new subjects — inside a control
    written to stop a different silence. The fix is not to add the second pattern; it is to make the
    unrecognised case FAIL SAFE. An id this function cannot place is treated as NEW and checked,
    because the cost of checking a frozen entry is a false alarm someone reads, and the cost of
    skipping a new one is the gate quietly doing nothing.
    """
    m = ID_RE.match(str(entry.get("id") or ""))
    return m is not None and int(m.group(1)) <= WATERMARK


def is_episode_shaped(entry: dict) -> bool:
    """The rule AuditLogEpisodeSource actually applies — goal, done_when and session, all present."""
    return all(str(entry.get(f) or "").strip() for f in ("goal", "done_when", "session"))


def has_evidence(entry: dict) -> bool:
    """A signals object, or an artifact under docs/proof/. An honest zero counts; silence does not."""
    if entry.get("signals"):
        return True
    return any("docs/proof" in str(a) for a in (entry.get("artifacts") or []))


def failures_for(entry: dict) -> list[str]:
    """What this entry is missing. Empty when it is compliant or out of scope."""
    if is_frozen(entry) or entry.get("kind") != "skill":
        return []

    problems = []
    if not is_episode_shaped(entry):
        missing = [f for f in ("goal", "done_when", "session")
                   if not str(entry.get(f) or "").strip()]
        problems.append(
            f"no {', '.join(missing)} — it will never become a Work Episode, so nothing can "
            f"score or observe it (AL5b). Pass --goal and --done-when.")
    elif not has_evidence(entry):
        # `elif`: an entry that is not an episode has a bigger problem than missing evidence, and
        # reporting both would bury the one that has to be fixed first.
        problems.append(
            "no evidence — no signals object and no docs/proof/ artifact, so it scores Not Scored "
            "for want of a verification path. If nothing was verified, say so: "
            "--signal-verification-path false is capture, silence is not.")

    return problems


def scan(lines: list[str]) -> tuple[list[tuple[str, str]], int, int]:
    """Returns (failures, entries checked, entries frozen)."""
    bad: list[tuple[str, str]] = []
    checked = frozen = 0

    for line in lines:
        line = line.strip()
        if not line:
            continue
        try:
            entry = json.loads(line)
        except json.JSONDecodeError:
            # Someone else's problem: the audit-log verifier owns malformed lines and reports them
            # as unreadable. Failing here too would report one defect twice under two names.
            continue

        if is_frozen(entry):
            frozen += 1
            continue
        if entry.get("kind") != "skill":
            continue

        checked += 1
        for problem in failures_for(entry):
            bad.append((str(entry.get("id")), problem))

    return bad, checked, frozen


def self_test() -> int:
    """Prove both refusals fire and that a compliant entry passes (DC-104)."""
    failures = []

    def check(label: str, condition: bool) -> None:
        print(f"  {'ok  ' if condition else 'FAIL'}  {label}")
        if not condition:
            failures.append(label)

    after = WATERMARK + 1
    base = {"id": f"al-{after:04d}", "kind": "skill", "session": "s"}

    check("a skill entry with no goal is refused",
          any("never become a Work Episode" in p for p in failures_for(dict(base))))

    check("an episode-shaped entry with no evidence is refused",
          any("no evidence" in p for p in
              failures_for(dict(base, goal="g", done_when="d"))))

    check("an honest zero counts as capture",
          failures_for(dict(base, goal="g", done_when="d",
                            signals={"verification_path": False})) == [])

    check("a proof-pack artifact counts as capture",
          failures_for(dict(base, goal="g", done_when="d",
                            artifacts=["docs/proof/pp-0001.md"])) == [])

    # Both exemptions, because a gate that fires on everything is as useless as one that never does.
    check("an entry at or before the watermark is frozen",
          failures_for(dict(base, id=f"al-{WATERMARK:04d}")) == [])
    check("a non-skill entry is out of scope",
          failures_for(dict(base, kind="prompt")) == [])

    # The regression that made this gate silently stop working: the pack changed audit ids from
    # al-NNNN to a ULID, and an unrecognised id used to be SKIPPED. Unrecognised now means NEW.
    check("a ULID id is checked, not skipped",
          any("never become a Work Episode" in p
              for p in failures_for(dict(base, id="al-01M1N21F1JKT7VFFBRQHER84T5"))))
    check("a ULID id with a goal and evidence passes",
          failures_for(dict(base, id="al-01M1N21F1JKT7VFFBRQHER84T5", goal="g", done_when="d",
                            signals={"verification_path": False})) == [])

    # And the scan wrapper, not just the predicate — the predicate being right is not the claim.
    bad, checked, frozen = scan([
        json.dumps(dict(base, goal="g", done_when="d", signals={"verification_path": True})),
        json.dumps(dict(base, id=f"al-{after + 1:04d}")),
    ])
    check("scan reports exactly the offending entry",
          len(bad) == 1 and bad[0][0] == f"al-{after + 1:04d}" and checked == 2 and frozen == 0)

    print()
    if failures:
        print(f"verify-audit-capture --self-test: {len(failures)} guard(s) did not fire.")
        return 1
    print("verify-audit-capture --self-test: every guard fires, and both exemptions hold.")
    return 0


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--self-test", action="store_true",
                    help="prove the refusals can fire; reads no log")
    args = ap.parse_args()

    if args.self_test:
        return self_test()

    try:
        lines = LOG.read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError as exc:
        print(f"verify-audit-capture: cannot read {LOG} — {exc}")
        return 2

    bad, checked, frozen = scan(lines)

    if bad:
        print("verify-audit-capture: FAILED — a skill run did not record what it was for.")
        for entry_id, problem in bad:
            print(f"  {entry_id}: {problem}")
        print()
        print("Daydream, the leaderboard and the scorer all read Work Episodes, and an entry without")
        print("a goal never becomes one. This is AL5b, which 259 entries show prose does not carry.")
        return 1

    print(f"verify-audit-capture: OK — {checked} skill entry(s) after the watermark carry a goal, a "
          f"done condition and evidence; {frozen} frozen as pre-existing debt.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
