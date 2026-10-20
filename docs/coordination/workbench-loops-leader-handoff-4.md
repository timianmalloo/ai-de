---
id: coordination-workbench-loops-leader-handoff-4
title: "Leader hand-off 4 — workbench-loops drive (wl-leader4-20261020 → wl-leader5)"
type: plan
status: proposed
owner: "@timianmalloo"
tags: [coordination, handoff, workbench-loops, leader]
links:
  - { to: coordination-workbench-loops, rel: implements }
  - { to: coordination-workbench-loops-leader-kickoff, rel: depends-on }
  - { to: coordination-workbench-loops-leader-handoff-3, rel: depends-on }
review-by: "2026-11-18"
summary: >-
  Fourth Leader's hand-off. T30's second batch and T26 (the WL-1b checklist kind) joined, both on the Owner's direction
  before the WL-S walk-through. The WL-1b demo script is confirmed against the code. T32 (the copy-pin class control)
  runs at hand-off. WL-S and WL-1b await the user's walk-throughs; T31, then T25, wait for USER DEMO DONE.
---

# Leader hand-off 4 — workbench-loops drive

Written by `wl-leader4-20261020` (epoch 4) near its context ceiling, for `wl-leader5`. The Owner is `wl-owner-20261018`
(session `ai-de-c3`). Read the kickoff (`workbench-loops-leader-kickoff.md`, substituting your own session id), then
this note, then `.agents/briefs/workbench-loops/obligations.md` (tail, from "Owner (2026-10-20 ~01:10Z"). Hand-off 3
holds the controls before this epoch; this note supersedes its track state.

## On main (origin/main `afe52a97` at writing; every join through `conductor-join.py`, the ratchet and a step-8 record)

| Join | Step-8 record | What |
|---|---|---|
| T30 n4–n6 (second batch) | `1731a1b2` (merge `4e8ad0cc`, join `4caa88a9`) | the turn's outcome line counts every compile call; a prepare opens the fold, keeps focus, announces the count once, names each line `? suggested`; `tools/compile-eval/first-calls.py`; the sheet tests inject the gate; R21's real control (`ComposerSendGate.cs:148`, the composer's one model-call site; mutation red); the privacy gate's `Suggest` ban retired with its reason |
| T26 n1–n3b (WL-1b checklist kind) | `26ac6861` (merge `e69fdbeb`, join `e3943961`) | the checklist in the Right zone; currentness against the stored envelope sha; accept refused while blocked; saved review state is not acceptance; CHK1–CHK12 confirmed by the Owner (CHK3, CHK9 changed and pinned literally); M6's Invalidate removed, M9 red |

Both recounts failed ONE test, `AReopenedNotebookRendersItsRecordedAttemptsTests.ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation`
("Space on the restored row's fold did not open it"). It failed **alone** on each join head **and** its first parent in
clean `rt-` trees, so it was excused under Ruling 155 (3) (NOT CHECKED) and the join continued with `--continue
--docs-only`. It now fails unattended on every head tried: it is the third keyboard test owed to a desk run, with
CtrlDigit and SolutionTreeChord. Both ratchets PASS 0/49; Release builds 0 errors.

**Accepted:** WL-1a. **Review-ready:** WL-S (demo staged on `C:\Projects\ai-de-demo` at `80cd82fd`, not started).
**Joined, not accepted:** WL-1b. Owed: T25 (perf), the user's walk-through of the demo script now in
`docs/proof/workbench-loops-wl-1b.md` ("Human demo script", `afe52a97`, confirmed by reading the code at `a85ffa58`;
steps 3 and 6 spend real usage on the claude-code lane; four claims are listed as not confirmed in code).

## Track state at hand-off

| Track | Branch / tree | State | Next |
|---|---|---|---|
| **T32 `wl-copy-pin`** | `C:\Projects\ai-de-wl-copy-pin`, off `14a55db5`, Sonnet, dispatched 02:35:51Z | **RUNNING** (child of wl-leader4's window). (P1) `DesignCopyRowsArePinnedTests`: every DESIGN.md copy row with an id pinned to its rendered literal; unmatched rows and constants are named findings on a shrink-only list; mutation red. (P2) F1: one comment-only commit, the Owner's exact sentence at `TheTypedBoundaryIsInertButRealTests.cs:252-255`, citing H4 rows 11-14 | done mail to `coord leader who`; its finding list goes to the Owner; join (no App code change, but its recount runs) |
| **T31 `wl-loadflake`** | not created | the control for DC-NEW-wl-leader3-b (≥10 full Core+App runs on a clean origin/main, per-test failure rates) | **after USER DEMO DONE** (its desktop runs must not overlap the user) |
| **T25 `wl1b-perf`** | not created | design row T25: n1 append overhead, n2 evaluate p95 at 100× | after T31; T32 may run beside it |
| **T21 n9** | — | the layout-command class gate | when a slot frees |

## Rulings and Owner directions this epoch (no numbered ruling was made)

All in `obligations.md`: CHK1–CHK12 confirmed with CHK3/CHK9 changed; M6 settled by option (b) and M9; the T30 join
hold (R21 live since Ruling 180) and its lift; F1 folded into T32; a WL-2 obligation (re-read
`NothingSaysAnUnsentDraftIsSavedTests:83` when the draft store lands; every WL-2 brief carries it); the order
(WL-S record, T30 join, T26 join, T31, then T32 with T25), then amended by the Owner to join T30 and T26 before the
walk-through (done).

## Controls added this epoch

- **Brief rule 23** (`brief-common.md`): a gate's refusal is answered by reading what it guards, never by reshaping code
  to leave its view; the commit names the gate, its rule and the answer. Class **DC-NEW-wl-t30-gate-evasion** (instance
  0 `f2b382fa`, instance `0528992b`; sweep: 68 bans, 63 hold, 4 stale wording → T32, 0 now live).
- **DC-NEW-wl-t26-copy** (a copy oracle reading the constant it checks); its class control is T32.
- **Leader tools in the scratchpad** (re-create if gone): `leader-watch.py` (a 60 s Monitor: leader renew every 100 s,
  mail to the Leader and Owner posts, typed requests, posts starting `USER DEMO`; re-arm every 30 min),
  `join-generic.sh` (epoch 4: change `--epoch`/`--session` to yours), `solo-probe.sh <sha> <label> <filter>` (Ruling 155
  (3) in a clean `rt-` tree), `assemble_brief.py <specific> <out> TRACK TREE BRANCH PROOF MODEL` (specific brief +
  `brief-common.md` with placeholders filled; proved to rebuild the committed n2/n5 briefs byte for byte).
- **A sub-agent's report is not a verification:** the demo-script check (41 claims) was re-read at the key lines before
  the commit; T30's and T26's done mails were checked against `git diff --name-only <suite head> <tip>` (docs-only after
  the suite head) before each join.

## Owed

- The user's **WL-S walk-through** (staged), then record it in the WL-S Proof Pack from the Owner's relay.
- The user's **WL-1b walk-through** (script above), after T25.
- The **prefill demo** (T30 n4's script in the prefill README) on a fresh tree at or after `1731a1b2`.
- **Desk runs:** CtrlDigit, SolutionTreeChord, the restored-row fold. Count them owed, never failed.
- **Budget:** ten Opus nodes overran their hand-off lines (T26 n3 199k, T30 n6 200k this epoch); the call-budget hook is
  with the user via the Owner.
- RES-1, the IME check (WL-2), PD-1/PD-2: unchanged.

## Resume brief (wl-leader5's first lines)

You are wl-leader5. Read the kickoff (substitute your session id), this note, then `obligations.md` (tail). Pin
(`coord leader pin <you> --ttl 900`), arm the watch (`leader-watch.py` from this note, re-created in your scratchpad),
and post a report on the board and to ai-de-c3, opening with the request census. Then:
(1) Wait for T32's done mail (running in wl-leader4's window; do not re-dispatch). Kick it at 15 min from 02:35:51Z if
it is still running. Pass its finding list to the Owner; join it when green.
(2) On `USER DEMO DONE`, record the WL-S result, then dispatch T31; T25 after T31.
(3) WL-1b is accepted only by the user's walk-through of the confirmed script, after T25.
