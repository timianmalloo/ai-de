---
id: proof-workbench-loops-wl-1a-binding-handoff
title: "Hand-off — WL-1a T8 node 2: run outlives its pane, IAttemptHost binding, restart read-back, adoption refusal"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, wl-1a, t8, evidence, handoff]
links:
  - { to: adr-0045-perspective-set-factory-review-understand, rel: implements }
  - { to: adr-0043-notebook-editor-codemirror-canonical-source, rel: implements }
  - { to: design-workbench-loops-wl-1a, rel: depends-on }
review-by: "2026-11-18"
summary: >-
  Track T8 (wl-t8-bind) node 2: runs move from the surface to the notebook model, the notebook binds to IAttemptHost
  (node-1 interface), restart reads attempts back, the product saves revisions over T14's ops, the store refuses
  adoption (Ruling 156), and T3's two follow-ups. Stopped past the Rule 16 cap; node 3's list is below.
---

# WL-1a T8 node 2 — hand-off

Branch `wl1a-notebook-binding`. Merged: `wl1a-attempt-host-n1` (2d3cb1bd), T14 at c1296bb7 then 69e75e4b,
`origin/main` (6c9782ca, per the Leader's stale-controls mail). **Not merged:** `wl1a-attempt-host-n2` (53d529ce) —
the Leader granted it after node 2 passed the Rule 16 cap. Node 3 merges it first.

## Done (red, then green) — tests under `tests/AiDe.App.Tests/Workbench/Records/` unless named

| Check | Tests | Red | Green |
|---|---|---|---|
| Adoption refused (Ruling 156(3)) | `AiDe.Core.Tests.Notebooks.AdoptionIsNotOfferedTests.AdoptingALegacyNotebook_IsRefused_AndWritesNoVersionRow` · `.ReOwningATerminalDraft_OutOfTheStandingJourney_IsRefused` · `.CreatingANotebook_AtVersionOne_StillPasses` | `adoption-refused-red.txt` | `adoption-refused-green.txt` |
| Product revisions over T14's ops | `TheProductRecordsRevisionsTests.TheShellsNotebook_SavesRevisionsOverTheDaemon_AndAnEditMarksTheResultStale` | `product-revisions-red.txt` | `product-revisions-green.txt` |
| T14b open refusals | `TheProductRecordsRevisionsTests.ARefusedOpen_SavesNoRevision_AndTheModelNamesTheStableCode` (JOURNEY-ID-COLLISION, RESERVED-JOURNEY) | `open-refused-red.txt` | `open-refused-green.txt` |
| Run outlives its pane | `TheRunOutlivesItsPaneTests.ClosingThePaneMidRun_LeavesTheRunRunning_AndTheReopenedSurfaceSeesItEnd` | `run-outlives-pane-red.txt` | `run-outlives-pane-green.txt` |
| IAttemptHost binding + restart read-back | `TheAttemptHostBindingTests.ASentTurn_IsAnAttemptOfItsRevision_AndARestartReadsTheAttemptAndItsEventsBack` (real `AttemptHost` + store + stub adapter) | `attempt-binding-restart-red.txt` (compile red: no binding) | `attempt-binding-restart-green.txt` |
| T3 follow-ups | `TheLiveStatesReadAsLiveTests.TheComposersOffer_ForAStopRequestedTurn_UsesTheStopCopy_NeverRunning` · `.TheConsole_FollowsEveryLiveState` (Preparing, StopRequested, Running) | `t3-follow-ups-red.txt` | `t3-follow-ups-green.txt` |

What changed: `Records/NotebookRuns.cs` (new — per-run CTS, the queue, the drain, the host dispatch, `AttemptOf`,
`ReadBackAsync`), `Records/DaemonNotebookRevisions.cs` (new), `NotebookModel` owns `Runs`; the surface is an
`INotebookRunObserver` (lane + consumed row) and announces "Pane hidden. The run continues." on close mid-run;
`WorkbenchShell` passes `DaemonNotebookRevisions` when its queries are the `WorkspaceClient`; Core:
`StoreErrorCodes.AdoptionNotOffered` and the refusal in `StoreWriter.AppendNotebook`.

## Suites (`suite-green.txt` = `suite-core.txt` + `suite-app.txt`, after the origin/main merge)

- **Core:** 2976 passed, 1 skipped, **3 failed** — all three assert adoption succeeds and fail by Ruling 156(3):
  `SchemaV2.Ruling150StandingJourneyUseTests.ALaterAdoption_ReOwnsADraftOutOfTheStandingJourney_AndItsRevisionsStay`,
  `SchemaV2.TypedStoreApiTests.ALegacyNotebook_IsAdoptedByANewVersionRow_ThenItsOwnerIsFixed`,
  `SchemaV2.MigrationGoldenTests.AMigratedStore_AcceptsTheV2Api`. Seam requests req-01M59CCA8RBZWRMNPSFMFQ4PZF and
  req-01M59DPPWWNRG5ZZWC8VNQG3RH (not T8's paths).
- **App:** 1049 passed, **3 failed**:
  - `TheAttemptHostBindingTests.TheHostInterface_CarriesTheDesignedStopSendNowAndSubscribeMembers` — red by ruling
    against node-1's interface; n2 has `StopAsync`/`SendNowAsync`/`SubscribeAsync` (async — rule 14: the test must
    check `SubscribeAsync`, not the design's `Subscribe`).
  - `Composer.TheSendGateSendsWhatProjectionProjectsTests.ProjectionProjectHasExactlyThreeNamedCallSitesByPath` —
    **a defect node 2 introduced**: `SessionDocumentSurface.CurrentProjectionOf` (the G1 read) adds a fourth
    `Projection.Project` call site. Fix in node 3: read the projection through `ComposerSendGate` (T8-owned), not a
    new call site.
  - `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` — "foreground not held"; re-run alone:
    **passed** (`solution-tree-chord-alone.txt`). Environmental.

## Not done / open

- Node-1-interface limits until n2 is merged: a host-run turn cannot be stopped; a queued turn is drained by the
  notebook, not by the host's queue; no live lines reach the thread from a host run (`assume:` in `NotebookRuns.Start`).
- **The production App does not bind a host:** no `AttemptHost` is opened in the shell, so the product still runs
  `GovernedRunHost.RunAsync` directly (now model-owned, so it outlives the pane). Opening the host over the
  `WorkspaceClient` ledger needs a composition-root seam (T5 / Leader).
- UIA names for the execution states: not started.
- Coordination: `coord claim` refused all paths with COORD-CLAIM-STALE-CONTROLS until the origin/main merge (request
  req-01M59CEEA37ER449Q3W3JGSGP2). The Ruling 156 edit to `StoreWriter.Workbench.cs` / `WorkspaceStore.cs` and the
  node-2 edits to owned paths were made **without a lease**.
- v3 `adopted_by` migration: not T8's (Ruling 156(1)).

## Next (node 3), in order

1. `git merge wl1a-attempt-host-n2` (53d529ce only). Rebind `NotebookRuns`: Stop → `IAttemptHost.StopAsync`; queue →
   `DispatchAsync` with `QueuedAfterDispatchKey` + `SendNowAsync`; live lines → `SubscribeAsync`. Fix the designed-members
   test to the code's names; turn it green against the real host.
2. Fix the fourth `Projection.Project` call site (G1 through `ComposerSendGate`).
3. A composition-root seam to open the workspace's `AttemptHost` in the shell, then `BindHost` at the construction site.
4. UIA names; then the suites and the join.
