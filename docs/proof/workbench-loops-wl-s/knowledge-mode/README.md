---
id: proof-workbench-loops-wl-s-knowledge-mode
title: "Evidence — WL-S T18: knowledge-inclusive canvas mode"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-S"
tags: [workbench-loops, evidence]
links:
  - { to: architecture, rel: implements }
  - { to: adr-0045-perspective-set-factory-review-understand, rel: implements }
  - { to: design-workbench-loops-wl-s, rel: implements }
review-by: "2026-11-18"
summary: >-
  Track T18 red and green evidence (nodes 1 and 2; checks 1-5): the canvas toolbar's Knowledge-inclusive toggle turns the ExcludeKnowledge seam off for that canvas only, starts OFF, and names itself through one const per string (Ruling 149 (2); Ruling 166).
---

# T18 `wls-knowledge-mode` — proof, nodes 1 and 2

Track T18 of the workbench-loops drive (slice WL-S; Ruling 149 (2), Ruling 166). Design: `docs/design/workbench-loops-wl-s.md` §8.
The knowledge-inclusive representation is a **mode of the one admitted `canvas` kind**. It is not a second surface or kind.

## What landed

- `src/AiDe.App/Workbench/CanvasPage.cs` — a `Knowledge-inclusive` toggle in the canvas toolbar, beside Group.
  It is a native `<button>` with `aria-pressed`, and its UIA name is `Knowledge-inclusive mode`. It posts
  `mode.knowledge`, then asks for its current top (overview or groups). Node 2: the text and the UIA name are one
  const each (`CanvasPage.KnowledgeModeText`, `CanvasPage.KnowledgeModeName`), interpolated into the page.
- `src/AiDe.App/Workbench/CanvasSurface.cs` — `KnowledgeInclusive` is held per canvas instance (default false).
  `ApplyKnowledgeMode(graph)` sets `graph.ExcludeKnowledge = !KnowledgeInclusive`. `SendKeyAsync` gains an optional
  `text`, so a test's Enter activates a focused button through CDP.
- `src/AiDe.App/Workbench/WorkbenchShell.cs` (the `ExcludeKnowledge` seam only; `BindCanvas`, about `:1398` on main) —
  `BindCanvas` no longer fixes `ExcludeKnowledge = true`. Its `GraphSource` applies the canvas's mode on every load.
  The code-only default is unchanged.

## Checks (red first, as assertion failures)

All tests are in `tests/AiDe.App.Tests/Workbench/Canvas/KnowledgeModeTests.cs`.

| Check | Node | Test | Red line | Red commit | Green commit | Proof |
|---|---|---|---|---|---|---|
| 1 selector offers the mode; shows then hides knowledge nodes | 1 | `TheSelector_OffersAKnowledgeInclusiveMode_ThatShowsThenHidesKnowledgeNodes_OnTheOneCanvas` | `the canvas offers no knowledge-inclusive mode control` | `e5599d04` | `0104ed64` | `check1-red.txt`, `check1-green.txt` |
| 2 seam off for this canvas only; default code-only | 1 | `TheMode_TurnsTheExcludeKnowledgeSeamOff_ForThisCanvasOnly_AndTheDefaultStaysCodeOnly` | `the mode did not turn the seam off` | `e5599d04` | `0104ed64` | `check2-red.txt`, `check2-green.txt` |
| 3 keyboard reach and UIA state, in the design's words | 1 | `TheMode_IsKeyboardReachable_AndItsStateIsExposedToUia_InTheDesignsWords` | `Expected: "BUTTON"` / `Actual: "none"` | `e5599d04` | `0104ed64` | `check3-red.txt`, `check3-green.txt` |
| 4 toggle default OFF; text and UIA name one const each (Ruling 166 (1)(2)) | 2 | `TheToggle_IsDefaultOff_AndItsTextAndUiaNameAreOneConstEach` | `Expected: "Knowledge-inclusive"` / `Actual: null` | `6beddd74` | `44f17286` | `check4-red.txt`, `check4-green.txt` |
| 5 every page read in the test fails on the `EvaluateAsync` sentinel | 2 | `EvaluateAsyncCallersGuardTests.EveryCallerChecksForTheFailureSentinel` (existing control) | `these files call EvaluateAsync and never check for its failure sentinel … KnowledgeModeTests.cs` | `626ccb36` | `0176c858` | `check5-red.txt`, `check5-green.txt` |

Whole suites (App and Core), after check 5: `suite-green.txt`.

- `tests/AiDe.Core.Tests`: **3019 passed, 0 failed, 1 skipped** (3020).
- `tests/AiDe.App.Tests`: **1126 passed, 2 failed, 1 skipped** (1129). The two failures are not T18 code:
  `ASendWhileATurnRunsOffersWaitOrParallelTests.AfterStop_TheTurnIsStopRequested_AndTheQueuedTurnStaysQueuedBehindIt`
  (`the composer editor could not start`) and `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`
  (`foreground not held; SendInput would prove another window`). Both passed in the suite run before check 5, and both
  pass when re-run alone: `suite-rerun.txt`, **2 passed, 0 failed**. They read as WebView2 start and foreground contention
  on this machine (Inferred, not proven).

### Check 5: a gap node 1 left

Node 1 named `suite-green.txt` in this README but never wrote it. The first whole App run in node 2 failed one test:
the existing guard `EvaluateAsyncCallersGuardTests` fired on `KnowledgeModeTests.cs`, which read the page without
checking `EvaluateAsync`'s `(evaluate failed` sentinel. Check 5's red is that run, saved unchanged. The fix routes every
page read in the test through one `Eval` helper that fails on the sentinel. The class already has a control (the guard
did its job), so no new defect class is recorded.

### What check 4's red proves, and what it does not

Check 4's red is the missing const: the page at HEAD inlined both strings. The default-OFF assertions in check 4 were
already true at HEAD (node 1 built the toggle OFF; check 3 also asserts `aria-pressed="false"` before any input).
So default OFF is **held** by check 4, not **made** by it. The red does not cover it.

## Seam requests

- `req-01M59VFTDSMA8A0ABAA6VWEEVE` (node 1, rule 14). There was no representation-selector control in code, and
  Ruling 53 had banned an in-surface toggle. Ruling 166 answered it: a per-instance toolbar toggle beside Group.
- `req-01M59X00WGJBF1EAEGDMDXDZF3` (node 2, rule 14). Ruling 166 (2) says the toggle "persists with the layout, like
  Group". On main, Group is **not** persisted. It is a page-local JS variable (`CanvasPage.cs`, `var grouped = false`).
  The layout stores only `Surface(SurfaceId, Kind, Title)` (`src/AiDe.Core/Workbench/LayoutModel.cs:11`; `SurfaceDto`,
  `LayoutStore.cs:35`). `SurfaceContentFactory.cs:142` builds `new CanvasSurface(s.SurfaceId, s.Title)` with no state.
  Persisting the toggle therefore needs a new per-surface field in Core's layout model and store (a schema change) and
  a factory change (T11's). Neither path is owned by T18. Recommendation and default: persistence matches Group
  (per instance, session only), unless the ruling adds the per-surface field.
  **Ruling 169 answered it with that option:** the toggle is per instance, default OFF and session only, like Group.
  There is no persistence and no schema change. Persisted view state for Group, 2D/3D and this toggle together is a
  future designed feature, outside T18.

## Per-instance non-leak (Ruling 169)

Check 2 already proves that one canvas's toggle never affects another. It builds two `CanvasSurface` instances
(`canvas-1`, `canvas-2`), turns the mode on for one, and asserts that the other's `ExcludeKnowledge` stays `true`
(`the mode leaked to another canvas instance`). Its red (`check2-red.txt`) failed at `the mode did not turn the seam off`,
the assertion just before the non-leak one, because at that commit there was no per-instance mode. No new test was added.

## Not done

- **Ruling 166 (2) persistence across close and reopen** is not built, by Ruling 169.
- **Ruling 166 (1) DESIGN.md entry** for the two strings is not written. `DESIGN.md` is not in T18's owned paths.

## Residual risks

- Check 1 reads the drawn nodes from the real page's DOM. Its graph source mirrors the seam with a real
  `CanvasGraphViewModel` but not the real daemon projection. The projection's own filter is proven by
  `GraphProjectionTests.ExcludeKnowledge_*` and `DaemonProcessTests`.
- Check 2 proves `ApplyKnowledgeMode` per instance. No test drives `WorkbenchShell.BindCanvas` itself.
  That one-line wiring is checked by reading it, not by a test.
- Check 3 proves the ARIA role, name and pressed state in the page, and Enter through CDP. It does not walk the
  OS UIA tree through WebView2. The design's §8 E2E check in the running app (a known `docs/knowledge/` node with its
  typed edges) is not run here; it is a later node's real-lane step.
- Check 4 reads the consts by reflection, so that a missing const fails as an assertion and not as a compile error.
- Node 1 committed its three reds together and its three greens together, not one commit per check.
