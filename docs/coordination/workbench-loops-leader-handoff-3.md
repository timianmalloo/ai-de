---
id: coordination-workbench-loops-leader-handoff-3
title: "Leader hand-off 3 — workbench-loops drive (wl-leader3-20261019 → wl-leader4)"
type: plan
status: proposed
owner: "@timianmalloo"
tags: [coordination, handoff, workbench-loops, leader]
links:
  - { to: coordination-workbench-loops, rel: implements }
  - { to: coordination-workbench-loops-leader-kickoff, rel: depends-on }
  - { to: coordination-workbench-loops-leader-handoff-2, rel: depends-on }
review-by: "2026-11-18"
summary: >-
  Third Leader's hand-off. WL-1a accepted by the user's demo; WL-S review-ready with its demo script staged; WL-1b Core,
  T28's compiled-field fix and T30's prefill joined. T30 n5 (the prefill is invisible: first) and T26 n2 (the checklist
  kind) run at hand-off. Controls added this epoch, and a resume brief.
---

# Leader hand-off 3 — workbench-loops drive

Written by `wl-leader3-20261019` (epoch 3) at its context ceiling, for `wl-leader4`. The Owner is `wl-owner-20261018`
(session `ai-de-c3`). Read the kickoff (`workbench-loops-leader-kickoff.md`), substituting your own session id. Read
hand-off 2 for the step-8 practice and the controls before this epoch. This note supersedes their track state.

## On main (origin/main `649b558d` at writing; every join ran through `conductor-join.py`, the ratchet and a step-8 record)

| Join | Step-8 record | What |
|---|---|---|
| T17e | `64f713a4` | a read-back pending at disposal ends quietly (WL-1a) |
| T21 n1–n6 | `8608d4eb` | float/redock real, Ruling 174 bounds, malformed layouts load with a report |
| T28 n1–n2 | `cf31f424` | the compiled field keeps the sent prompt after a submit (Ruling 181) |
| T28 n3 | `367f4de4` | queued-as heading; the gesture-oracle class gate |
| T11 | `d25d0311` | notebook + execution kinds, slot migration (re-join; the first was refused at the ratchet, kept at `backup/t11-join-070d2637`) |
| WL-1b Core (T23+T24+T27, one integration branch) | `69039edb` | folds, checklist service, one currentness definition (Rulings 176–179, 178) |
| Float fix (T21 n8 `36884736` + cite fix `2213b507`) | `dc03007c` | layout commands render; the palette's Float pane is safe again (first attempt refused, kept at `backup/float-fix-join-a7f17301`) |
| T21 n8 rest | `26e14f8b` | live-notebook float, redock, restart; close condition 4 proved by test |
| T28 n4 | `26ef801e` | the drain heading |
| T30 n1–n3b | `d986e4a4` | the model prefill default, the Compile mode control, kept lines, restored rows rebuilt and verified or "not recorded" (Rulings 180, 182) |

**Accepted:** WL-1a, by the user's demo (2026-10-19, 15:34–15:46Z; Proof Pack Acceptance section).
**Review-ready:** WL-S. The demo script is `docs/proof/workbench-loops-wl-s.md` (`3fd79cfa`). The Owner staged
`C:\Projects\ai-de-demo` at `80cd82fd` (pre-T30, on purpose) and will post `USER DEMO IN PROGRESS`.
**Not accepted:** WL-1b. Owed: T26 (the checklist kind), T25 (perf), the WL-1b demo script and the user's walk-through.

## Track state at hand-off

| Track | Branch / tree | State | Next |
|---|---|---|---|
| **T30 `wl-prefill`** | `C:\Projects\ai-de-wl-prefill`, n5 dispatched on `fb54c8fc` | **n5 RUNNING** (Opus, hand off at 120k). (A) first, in the Owner's exact shape: the suggested lines are invisible on main (a closed fold, `ComposerSurface.cs:1648-1652`, `:830-835`). (B) the first-50 reader `tools/compile-eval/first-calls.py` (seam granted). (C) the four sheet tests inject the gate (granted). Then rule 22's ratchet | join T30's second batch; then the prefill demo script (n4, in the prefill README) for the user, on a fresh tree at or after the join |
| **T26 `wl1b-checklist-kind`** | `C:\Projects\ai-de-wl1b-checklist-kind`, n2 dispatched on `9068cfac` | **n2 RUNNING** (Opus, hand off at 120k). (0) three literal entries (granted). The Owner's refusing test (a journey not matching the session's notebook). D3 with the own-journey restore red. (E) the one-live-sha ceiling. Then the ratchet | join; the checklist copy CHK1–CHK12 awaits the Owner's confirmation; then the WL-1b demo script |
| **T31 `wl-loadflake`** | not created | the control for DC-NEW-wl-leader3-b: run the full Core and App suites ≥10 times on a clean origin/main, quiet machine, per-test failure rates, then repro or NOT REPRODUCED with N | **after the user's WL-S walk-through**, ahead of T25 |
| **T25 `wl1b-perf`** | not created | design row T25: n1 append overhead (no dependency), n2 evaluate p95 at 100× (T24 is joined) | after T31 (needs a quiet machine) |
| **T21 n9** | — | the layout-command class gate (a view-state oracle must drive the command route; node 2's float test passed by rendering by hand) | when a slot frees |

**Agents in flight are children of wl-leader3's window.** Do not close it until T30 n5 and T26 n2 have each sent
their done mail. Their briefs tell them to mail whoever `coord leader who` names, so wl-leader4 receives them. Kick each
at 15 min from its start, or at its measured rate, by `coord-mail.py send --kind kick`. A SendMessage from wl-leader4
does not reach them.

## Rulings this epoch (addendum-c register; each committed alone)

176 checklist_id = journey_id, enforced · 177 no disposition covers a mandatory gap · 178 one currentness definition ·
179 the deliverable is the accepted intent revision · 180 the model prefill by default under gate 1 (amends Ruling 68's
default only) · 181 after a submit the field keeps the sent prompt, display-only, never resent · 182 Ruling 180 (3)
amended: survival across the next prepare now, restart survival with WL-2's draft store; the residual is stated to the user.

## Controls added this epoch (use them every time)

- **Brief rules 18–22** (`brief-common.md`): 18, a kick never skips the suite. 19, no App run of any kind while a join is
  in flight (strict, including scan-only). 20, no App run, UI probe or real-lane test while `USER DEMO IN PROGRESS`.
  21, every output stays in the track's own tree. 22, a node that hands off for a join runs the ratchet first; the
  derived-view gates (docs-graph index drift, verify-derived-views, verify-site-figures) are expected red on a branch head.
- **Joins:** `.agents/briefs/workbench-loops/` gains nothing new. The Leader's wrapper is in the session scratchpad,
  `join-generic.sh <branch> <shortname> <title> <summary> <goal> <artifact>`, which waits for no test host before the
  recount. Re-create it if the scratchpad is gone; its logic is 15 lines. A foreground-probe refusal: run the probe
  alone on the join head and the first parent in a clean `C:/Projects/rt-*` tree (Ruling 155 (3)). A load-only Core/App
  failure: ONE full re-run per join, at most (Owner); a second failure stops the join.
- **Never write into the primary while a join is between steps.** Hold briefs and register lines in the scratchpad and
  apply them after the push. An abandoned join: `git branch backup/<name> HEAD`, save the changed `.agents/**` files,
  `git reset --hard origin/main`, restore them (`--keep` refuses when a ledger is both committed and dirty).
- **Seam requests arrive in the requests ledger, not mail.** Run a Monitor that polls `coord request list` for typed
  requests addressed to you every 60 s, and re-arm it every 30 min (DC-NEW-wl-leader3, 4th instance).
- **Times and shas in messages come only from command output read in that turn.** Resolution strings carry
  `$(date -u +%H:%M:%SZ)`. A "nothing depends on X" claim is a claim about the code, so open the steps first
  (DC-NEW-wl-leader2: seven recurrences under wl-leader3, each corrected).
- **Defect classes this epoch:** DC-NEW-wl-leader3 (a recount shares the desktop with a track suite; 4 instances),
  DC-NEW-wl-leader3-b (tests that fail only under full-suite load; T31 measures), DC-NEW-wl-t28-compiled (an oracle that
  never drives the user's gesture; recurred in T21's float test and T30's fold), DC-NEW-wl-t21-float-d (a layout command
  that changes the model and never renders), DC-NEW-wl-t30-prefill (tests that read other threads' windows).

## Owed

- The user's **WL-S walk-through** (staged; `USER DEMO IN PROGRESS` / `DONE` on the board). Record the acceptance in
  the WL-S Proof Pack, from the Owner's relay.
- The **prefill demo** for the user, after T30's second join, on a fresh tree.
- **Desk runs:** every keyboard and foreground test fails unattended now (SolutionTreeChord, CtrlDigit,
  AReopenedNotebook's keyboard case); the Owner says count them owed, never failed. They passed at the T28 n4 and T30
  joins (Inferred: the desktop session changed).
- **The call-budget hook** (`docs/coordination/proposal-call-budget-hook.md`): the Owner is putting it to the user. Eight
  Opus nodes overran their hand-off lines this epoch (up to 376k against 150k).
- RES-1, the IME check (WL-2), PD-1/PD-2: unchanged from hand-off 2.

## Resume brief (wl-leader4's first lines)

You are wl-leader4. Read docs/coordination/workbench-loops-leader-kickoff.md (substitute your session id), then this
note, then `.agents/briefs/workbench-loops/obligations.md` (tail). Pin the leader ref (`coord leader pin <you> --ttl
900`) and start a renew loop and the seam-request Monitor. Post a report on the board and to ai-de-c3, opening with the
request census. Then:
(1) Wait for T30 n5's and T26 n2's done mail (running in wl-leader3's window). Do not re-dispatch them. Kick each at 15
min from its start if it is still running.
(2) Hold every join and App run while `USER DEMO IN PROGRESS` is open. On `USER DEMO DONE`, record the WL-S result,
then join T30 (second batch) and T26 when green, one at a time, then dispatch T31, then T25.
(3) After T26 joins, write the WL-1b demo script for the Owner. WL-1b is accepted only by the user's walk-through.
