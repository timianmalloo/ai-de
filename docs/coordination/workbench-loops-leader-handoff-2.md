---
id: coordination-workbench-loops-leader-handoff-2
title: "Leader hand-off 2 — workbench-loops drive (wl-leader2-20261019 → wl-leader3)"
type: plan
status: proposed
owner: "@timianmalloo"
tags: [coordination, handoff, workbench-loops, leader]
links:
  - { to: coordination-workbench-loops, rel: implements }
  - { to: coordination-workbench-loops-leader-kickoff, rel: depends-on }
  - { to: coordination-workbench-loops-leader-handoff-1, rel: depends-on }
review-by: "2026-11-18"
summary: >-
  Second Leader's hand-off. WL-1a was accepted provisionally, then reopened (Ruling 172) and fixed by T17d. WL-S is
  near close: T11, T18, T20 and T21. The next Leader starts WL-1b on branches only. Includes the controls added this
  epoch and a resume brief.
---

# Leader hand-off 2 — workbench-loops drive

Written by `wl-leader2-20261019` (epoch 2) at release, for `wl-leader3`. The Owner is `wl-owner-20261018` (session
`ai-de-c3`). The kickoff is `docs/coordination/workbench-loops-leader-kickoff.md`; substitute your own session id for
the Leader's everywhere. Hand-off 1 holds the earlier state and the step-8 practice. This note supersedes its track state.

## Why this hand-off

The Owner agreed to it (13:10Z). This Leader had been compacted once, and its slips were piling up: kick times, a claimed
spot-read, header times. WL-1b's design is the heaviest-context work ahead, so it starts in a fresh Leader.

## On main (origin/main; every join through `conductor-join.py`, ratchet, step-8 record)

- WL-1a joins: T19 `30b2eb8c` + `4ca7a73e` ("stopped by you", Ruling 167). T17d `e1280798` + step-8 `a0e33443`
  (the read-back on host bind, Ruling 172; a Send while restoring keeps the words, Ruling 175).
- WL-S joins: T18 `f6fd0841` + `9c62f453` (knowledge-inclusive canvas mode; the chore commit was re-created without a
  swept Ruling 171). T20 `ab3fa990` + `4748cdb2` (the float guard, Ruling 171 (1); T21 removes it on its branch).
- Leader controls: `leader_prejoin.py` and the `leader_precommit.py` register check (`a05b0b2d`, `eda6aeff`).

## Track state at hand-off

| Track | Branch / tree | State | Next node (first check first) |
|---|---|---|---|
| T21 `wls-float` | `wls-float`, C:\Projects\ai-de-wls-float, tip `61788b3e` | n1 render/move/close; n1b read-back + null fix; n2 T20's guard and DOCK2/DOCK3 removed; n3 Ruling 174 bounds; all red→green | **n4 RUNNING at hand-off** (dispatched 14:42Z, kick due ~14:54Z; done mail to `coord leader who`): (0) revert `60779f03`'s generalisation and replace it with the Owner's allow-list (the frozen prefix plus exactly the four bounds parameters by name and type; any other added parameter fails "added to schema 1 without a ruling"; red by planting a fifth optional double?); then the Owner's four close conditions in order: (1) the two saved-file shapes that crash the reader (no "zones", no "surfaces") each load with a report line; (2) floating-window ids unique across a restore; (3) floatPane floats the focused tab, not the first (ZoneBackedLayoutService.cs:584); (4) the live-notebook float → redock (after T11 lands). Also: the LayoutRect comment (DropTargetResolver.cs:3); pin the off-screen sentence to the older path's source; residual: a move followed by a crash before the next layout pass is lost (Inferred; WorkbenchShell.cs needs a grant) |
| T11 `wls-kinds-migration` | C:\Projects\ai-de-wls-kinds-migration, tip `7dbb0099` | n1–n10 done (notebook + execution kinds, D2 Landing flip, Ruling 168/170 measure and MinWidth 840, Open-log opener, Ctrl+1/2/3 in WebView2, hide with DOCK1 once, slot migration, prompt kept, §7 rename/fold, DOCK4 row, both guard reds fixed) | **n11 RUNNING at hand-off** (dispatched 14:42Z on fc56ee37, main with T17d already merged in; kick due ~14:54Z; done mail to `coord leader who`): (1) D2 green (execution at revive; the shell part is granted at WorkbenchShell.cs ReviveRestoredSessionDocuments :3506 → OpenExecution(…, activate:false)); (2) the Owner's D6 amendment (a switch STATEMENT lists Derived/Verb/TurnAction with no default; a switch EXPRESSION's discard arm throws UnreachableException naming the type; one reflection test over every SurfaceEntry subtype per consumer); (3) pin DOCK4's PLURAL sentence (LayoutPersistence.cs:391 is a plain string no test reaches). At the T11 join, re-run CtrlDigitSwitches… and SolutionTreeChordTests WITH the foreground (last green: node 5) |
| T17d `wl1a-readback` + T17e `wl1a-dispose-race` | T17d joined (a0e33443). T17e: C:\Projects\ai-de-wl1a-dispose-race, branch wl1a-dispose-race, tip 4f9c691d, off a0e33443 | T17e DONE, not joined: a read-back pending at disposal is reproduced by a gate and fixed in NotebookRuns.cs (red 31b61650 -> green d7e0ac1b; unit path and WorkbenchShell.Dispose path); Core 3024/0, App 1147/1 (SolutionTreeChord, foreground, Ruling 155 (3)) | **Join T17e first** (WL-1a; pre-join check, start and end posts, ratchet, step-8 record). Still open: T17d's one-off teardown NRE is a different race on the store's WRITE connection, not reproduced (a stub run's final write vs fixture disposal; fixing it means the test or fixture) |
| WL-1b | — | not started | wl-leader3: plan refresh from actuals (GO19 step 11), /design-slice, tracks red-first on branches ONLY; NO WL-1b join until the user's WL-1a demo passes (coordination plan :191) |

## Binding rulings this epoch (addendum-c register; the next number follows its last heading)

- 166 knowledge-inclusive toggle beside Group · 167 "stopped by you" · 168 thread measure held both ways in the Center ·
  169 the toggle is session-only per instance · 170 MinWidth 840 with a derived test · 171 float guarded now, made real in
  WL-S, and WL-S not accepted until a live notebook floats and redocks · 172 WL-1a reopened, T17d · 173 prompt kept, and §4
  retirement needs all six capabilities · 174 floating bounds, four optional fields · 175 Send while restoring keeps the
  words. Each was committed alone.
- Unnumbered Owner decisions: SurfaceEntry.TurnAction accepted (D6); D6 amended for CS8509; DOCK2/DOCK3/DOCK4 copy
  confirmed; T17d n3 and n4 accepted; T21 n2 accepted; the 60779f03 revert and allow-list; T21's four findings are its own
  close conditions.

## Controls added this epoch (use them every time)

- **Before any join:** run `python -I .agents/briefs/workbench-loops/leader_prejoin.py .` and require exit 0. Then post
  `join in flight: <name>` on the board. At the end, post `join done: <name> <sha> pushed` or `join abandoned`.
- **Before any push:** run `python -I .agents/briefs/workbench-loops/leader_precommit.py . HEAD origin/main` and require
  exit 0. It refuses a range with a join that has no step-8 record, and a join commit that changes the rulings register.
- **The ratchet:** `sh .agents/briefs/workbench-loops/run_ratchet.sh <parent> <head> <label>`, which runs `gate_ratchet.py <parent tree> <head tree> <out>` on
  two clean detached worktrees of the merge's first parent and the join commit. Use short paths (`C:/Projects/rt-*`):
  long paths fail to delete.
- **Rulings:** commit each one alone, never inside or stacked on an unpushed join. If the Owner rules while a join is
  between steps 2 and 7, save the register edit as a patch (`git diff > patch`), restore the file, and re-apply it
  after the push (Rulings 172/173 were saved this way). The Owner holds rulings while a "join in flight" post is open.
- **Decision requests:** use deadlines of at least 2400 s. The evidence names who observed each fact. Never write "the
  Leader verified" without the read in that turn (DC-NEW-wl-leader2, 14th instance).
- **Kicks (rule 16):** kick at 12 min. The kick NEVER skips the whole-suite run (15th instance: skipping it let T11
  nodes 4–8 close with two unseen guard reds). Grace is given for a suite already running. Brief rule 16 says a node
  never stops on its own wall-clock estimate.
- **Mail:** ack after reading, through `coord-mail.py ack` (`python -I .agents/briefs/workbench-loops/ack_all.py . <session>`). The bounce is 50 unacked
  (`coord-mail.py:312-313`). The lines advisory is PD-5 and never bounces.
- **Main into a track tree:** a claim refused with COORD-CLAIM-STALE-CONTROLS means the Leader merges origin/main
  into the track's tree (tracks may not merge). Discard the derived `docs/audit/audit-data.js` first.

## Agents in flight at hand-off

**Hard instruction: do not close wl-leader2's window until T21 n4 and T11 n11 have each posted done (T17e already has).** When both have, wl-leader2 posts one board line, "wl-leader2 children done; window may close", and only then may the window close. (The work itself, meaning trees, branches and the ledger, survives a hand-off. In-process sub-agents are children of the window and stop if it closes.)

T21 n4 and T11 n11 run as sub-agents inside wl-leader2's process. That window stays open, released and idle, until each one has reported. Their done mail goes to whoever `coord leader who` names, so wl-leader3 receives it. Kick each one at its time above, with the whole suite never skipped (grace for a suite already running), by `coord-mail.py send --kind kick` to its session. A kick by mail reaches them; a SendMessage from wl-leader3 does not reach wl-leader2's sub-agents.

## Owed

- The user's WL-1a walk-through (the Human demo script in `docs/proof/workbench-loops-wl-1a.md`; step 18 is the
  restart step T17d fixes). WL-1a is accepted only when it passes.
- The WL-S demo script (draft in T11's `docs/proof/workbench-loops-wl-s.md`). "hide/float/redock a live notebook"
  stays "pending T21" until T21 joins. WL-S is not accepted until T21 lands (Ruling 171 (3)).
- RES-1 (CI steps), the IME check (WL-2), and the PreToolUse call-budget hook proposal
  (`docs/coordination/proposal-call-budget-hook.md`, unwired; the user decides).

## Resume brief (wl-leader3's first lines)

You are wl-leader3. Read docs/coordination/workbench-loops-leader-kickoff.md (substitute your session id), then this
note, then `.agents/briefs/workbench-loops/obligations.md` (tail). Pin the leader ref (`coord leader`), then post a
report on the board and to ai-de-c3 opening with the request census. Then, in order:
(1) T21 n4 and T11 n11 are running in wl-leader2's window; do not re-dispatch; join T17e first. Read their done mail
when it arrives. Their next nodes are Opus, with the kick at 12 minutes and the whole suite never skipped.
(2) Start WL-1b on branches only: plan refresh, /design-slice, red-first tracks. No WL-1b join until the user's WL-1a demo
passes.
(3) Join T11 and T21 when green, with the pre-join check and the start and end posts, and keep WL-S's demo script ready
with float marked "pending T21" until T21 lands.
Tools: .agents/briefs/workbench-loops/{leader_prejoin.py, leader_precommit.py, gate_ratchet.py, run_ratchet.sh, ack_all.py}.
