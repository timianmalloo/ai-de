# Brief — T11 `wls-kinds-migration` (slice WL-S) · Claude Code · Opus · node 12 (the last node before the T11 join)

**Goal (plan row):** split `session-document` into the `notebook` + `execution` kinds and migrate saved layouts with a
report. The WL-S Proof Pack is `docs/proof/workbench-loops-wl-s.md`.
**Node 11 is done** (a2e2e8b8): D2 green, D6 amended with the reflection test, and the DOCK4 plural pin **red only**
(`b62261e8`), because T21 held `LayoutPersistence.cs`.
**T21 is joined on main** (Owner order: T21 first, then T11 on top of it). The Leader merged origin/main into your tree
before dispatch, and resolved any `LayoutPersistence.cs` conflict in that merge by keeping both sides. Read the merge
commit before your first edit.

**This node (n12), in this order:**
- **(0)** The whole Core and App suites at the merge head, before any edit, saved as `{PROOF}/suite-node12-start.txt`.
  Any red the merge brought in (T21's partial-read sentence and the DOCK4 arm share `LayoutPersistence.cs`) is this
  node's first fix. Name its cause by file:line.
- **(A) The DOCK4 plural pin, green.** Your node-11 hand-off step 1: one `KeptPromptsSentence` const, and the :391 plural
  arm returns it with `{N}` replaced. Mutate the const once to prove the two-prompt test bites, then revert. Check the
  line numbers on the merged file first (rule 14).
- **(B) The WL-S demo script's attended list (Owner, Ruling 155 (3)).** `CtrlDigitSwitchesThePerspectiveFromInsideAPageTests`
  and `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` stay NOT CHECKED unattended. Add both to
  the user's **attended** list in `docs/proof/workbench-loops-wl-s.md`'s human demo script, with the exact commands.
  Mark the float step "pending: close condition 4, the live-notebook float after the T11 join" (T21's README names it).
  This is a docs check: no red/green, but the Leader reads the diff.
- **(C) The seam guard edit from node 10.** `EveryOpeningCommandPassesThroughTheSeamTests.cs` was edited outside the
  grant. The Leader confirmed it retroactively at node 10 (obligations.md). Make sure the merge did not reopen it: it
  must be green at the merge head. Record that in handoff.md.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16) · **kick** at 12 min by mail.
**Desktop hold:** a "desktop hold occupied" result is re-run after 2 min and logged in `{PROOF}/desktop-hold.txt`. It is
never counted as a pass or a fail. Rules 19, 20 and 21 apply.

## Owned authored paths
As in node 11, plus `src/AiDe.App/Workbench/LayoutPersistence.cs` (the DOCK4 arm and its const only; T21's sentences stay
as joined) and `docs/proof/workbench-loops-wl-s.md` (the demo script).

## Not in scope
Close condition 4, the live-notebook float and redock: it is owed after the T11 join, and the Leader dispatches it as a
T21 node.
