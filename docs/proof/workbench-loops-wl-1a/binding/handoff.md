---
id: proof-workbench-loops-wl-1a-binding-handoff
title: "Hand-off — WL-1a T8 node 3: the run is the host's, G1 through the gate, the shell opens the AttemptHost"
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
  Track T8 (wl-t8-bind) node 3: merged T5 node 2 and origin/main; rewrote the three adoption steps to the Ruling 156(3)
  refusal (seam grant); the notebook's Stop, queue and live lines now run on IAttemptHost; the G1 read goes through
  ComposerSendGate; the product shell opens the workspace's AttemptHost. Node 4 has the UIA names.
---

# WL-1a T8 node 3 — hand-off

Branch `wl1a-notebook-binding`. Merged `wl1a-attempt-host-n2` (53d529ce) at bf8f80e6. Merged `origin/main` at 9c182d49,
and again at c77362d7 because `coord claim` refused with COORD-CLAIM-STALE-CONTROLS after main moved (0be25c99, dafcbee8).

## Done — tests under `tests/AiDe.App.Tests/Workbench/Records/` unless named

| Check | Tests | Red | Green |
|---|---|---|---|
| Seam rewrite (Ruling 156(3)) | `AiDe.Core.Tests.SchemaV2.Ruling150StandingJourneyUseTests.ALaterAdoption_ReOwnsADraftOutOfTheStandingJourney_AndItsRevisionsStay` · `TypedStoreApiTests.ALegacyNotebook_IsAdoptedByANewVersionRow_ThenItsOwnerIsFixed` · `MigrationGoldenTests.AMigratedStore_AcceptsTheV2Api` | node 2's `suite-core.txt` | `adoption-seam-rewrite-green.txt` |
| A: the run is the host's | `TheAttemptHostBindingTests.APaneStop_ReachesTheHostsStopAsync_AndTheAttemptEndsByItsStopRule` · `.AQueuedTurn_IsTheHostsHeldSuccessor_AndEachAttemptsLiveLinesReachTheThread` · `.TheHostInterface_CarriesTheDesignedStopSendNowAndSubscribeMembers` (now checks `SubscribeAsync`) | `host-run-red.txt` | `host-run-green.txt` |
| B: no fourth `Projection.Project` | `Composer.TheSendGateSendsWhatProjectionProjectsTests.ProjectionProjectHasExactlyThreeNamedCallSitesByPath` | `projection-call-sites-red.txt` | `projection-call-sites-green.txt` |
| C: the shell opens the host | `TheShellOpensTheAttemptHostTests.ASentTurn_InTheShellsSessionDocument_RunsThroughTheHostTheShellOpened` (real `WorkspaceClient` over a pipe; `CompositionRootLedger.Roots == 1`; the shell's disposal frees the host lock) | `shell-opens-host-red.txt` | `shell-opens-host-green.txt` |

What changed:
- `Records/NotebookRuns.cs`, when bound to a host:
  - Stop → `StopAsync`, and the turn shows `StopRequested` until the host's terminal arrives.
  - A queued turn → `DispatchAsync(QueuedAfterDispatchKey = the predecessor's derived key)` at queue time.
  - Send now and the drain → `SendNowAsync`.
  - Live lines → `SubscribeAsync`, through the one sink.
- `ComposerSendGate.CurrentProjectionSha` reuses the send's own `Projection.Project` call.
- `ConductorEntry.OpenAttemptHostAsync` is the one opener. The lock is `<ws>/.aide/attempt-host.lock`.
- `WorkbenchShell` binds each notebook after the host opens. It announces when the host cannot be opened, and disposes
  the host after the notebooks.
- `CompositionRootLedger` is unchanged: the host launches through `GovernedRunHost.RunAsync`, so no new root exists.

## Suites (`suite-green.txt` = `suite-core.txt` + `suite-app.txt`)

- **Core:** 3013 passed, 1 skipped, 0 failed.
- **App:** 1070 passed, 1 skipped, 1 failed. The failure is `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`
  ("foreground not held"). Run alone, it **passed** (`solution-tree-chord-alone.txt`).

## Not done / open

- **Cancel on a turn the host holds is refused** (`Runs.Cancel` returns false, and the turn stays Queued with Send now).
  The cause: `IAttemptHost` cannot withdraw a held successor. Seam req-01M59ERYY7362ASCQ4E20XA4MA is open.
- `assume:` in `WorkbenchShell.BindAttemptHostAsync`: a session document's attempts carry `SessionGeneration` 1,
  because a native session has no PTY generation and the design does not name one.
- The `AttemptScope` epoch is the client's handshake epoch, fixed at bind time. If the daemon restarts, it is not re-read.
- A turn sent before the host finishes opening runs directly through `GovernedRunHost.RunAsync`.
- UIA names for the execution states: node 4.
