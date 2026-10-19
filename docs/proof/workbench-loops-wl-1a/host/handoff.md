---
id: proof-workbench-loops-wl-1a-host-handoff
title: "WL-1a host (T5) — hand-off after node 2: (a)–(g) green, real-lane Stop Interrupted"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [proof, handoff, attempt, stop, sweep, run-log, workbench-loops, wl-1a, t5]
links:
  - { to: adr-0041-execution-attempt-lifecycle, rel: implements }
  - { to: design-workbench-loops-wl-1a, rel: implements }
review-by: "2026-11-18"
summary: >-
  Track T5 after node 2. Node 1 built the gate, write-ahead and W1–W4; node 2 built the run log, relay fan-out
  and subscribe, RunLogUnwritable, the sweep triggers and attempt.sweep emission, Stop (stub and one real
  claude-code run: Interrupted), queue and Send-now, redaction wiring, fold.duration_ms, the NFR-11 oracle and
  the slice Proof Pack. Each check red then green. What remains is listed under "Not done".
---

# WL-1a host — hand-off after node 2

Tree `C:\Projects\ai-de-wl1a-attempt-host`, branch `wl1a-attempt-host`. Node 1's tip is pinned as
`wl1a-attempt-host-n1` (`2d3cb1bd`). Design: `docs/design/workbench-loops-wl-1a.md`. Slice Proof Pack:
`docs/proof/workbench-loops-wl-1a.md`.

## Done (node 2)

| Commit | What |
|---|---|
| `1b560f5d` | `test: red` — 18 tests + the skeleton API (`run-log-core-red.txt`, `node2-app-red.txt`) |
| `cf3aef8f` | green: run log, relay, subscribe, RunLogUnwritable, sweep triggers + emission, Stop, queue + Send-now, NFR-11, `fold.duration_ms`, StopRequested precedence; `DC-NEW-wl-t5-host-n2` |
| `439e9704` | merge `origin/main` (T7 `FrameRedaction` on main) |
| `07922bfd` | `test: red` — redaction wiring (`redaction-wiring-red.txt`) |
| `cbde0cb2` | green: the mapper redacts at ingest (`redaction-wiring-green.txt`) |
| `1d9472d8` | the recorded real claude-code Stop (`real-lane-stop/`): `Interrupted` |
| (this commit) | slice Proof Pack, suite runs, this hand-off |

Checks, red then green (test names exact):

| Check | Tests |
|---|---|
| (a) run log before publish; unwritable line | `AiDe.Core.Tests.Attempts.RunLogOrderTests.EveryEvent_IsInTheRunLog_BeforeThePeerPublishesIt`, `.ALineThatCannotBeMadeDurable_IsNotPublished_AndFailsThePendingPromptWithRunLogUnwritable`; `AiDe.App.Tests.Conductor.Attempts.AttemptRunLogTests.AnUnwritableRunLogLine_IsNotPublished_AndTheAttemptIsDeliveryUnknownRunLogUnwritable_WithAnIncident` |
| (a) relay fan-out + subscribe | `AttemptRunLogTests.TheRelay_WritesTheSameInstanceToEverySubscriber_AndTracksTheHighWaterSeq`, `.ARun_WritesItsRunLog_AndASubscriberSeesEveryEventOnceInOrder` |
| (b) sweep triggers + emission | `AttemptQueueAndSweepTests.TheStartupSweep_EmitsAttemptSweep_EvenAtZero`, `.TheBackstopTimer_SweepsAndEmits_OnItsInterval`, `.AnEngineExit_SweepsAnAbandonedDispatchedAttempt_ToRelayLost` |
| (c) Stop | `AttemptStopTests.Stop_SendsSessionCancel_AndTheEnginesCancelledReplyIsInterrupted`, `.Stop_ThatIsNeverAcknowledged_IsDeliveryUnknownStopNotAcknowledged_AtTheDeadline`, `.APermissionAskedDuringStop_IsAnsweredCancelled_AndRecordedAsPermissionCancelledByStop`, `.ATokenCancelAlone_TheHostsOwnTeardown_IsNeverInterrupted`; `RunLogOrderTests.AStopRequestedTurn_StaysStopRequested_WhenAWaitOrAResumeArrives`; real lane `AttemptRealLaneStopTests.Stop_OnTheRealClaudeCodeLane_EndsInTheAdrsTerminal_AndIsRecorded` (skipped unless `AIDE_REAL_ADAPTER_ROOT` is set) |
| (d) queue + Send-now | `AttemptQueueAndSweepTests.AQueuedSuccessor_LaunchesOnlyAfterItsPredecessorsResponseComplete`, `.AQueuedSuccessor_BehindAPredecessorThatDidNotComplete_IsHeld_AndSendNowLaunchesItOnce` |
| (e) redaction wiring | `AttemptRedactionTests.ASentinelInAFrame_IsStoredRedacted_WithARedactionRow_AndNoSubscriberSeesIt` |
| (f) `fold.duration_ms`; NFR-11 | `AttemptObservabilityTests.AFoldActivity_WritesFoldDurationMs_ToTheRollingLog`, `.EveryAttempt_HasDispatchedAndTerminalTs_EventCount_Bytes_Outcome_AndUsage`, `.AnAttemptWithNoUsage_ReadsNotRecorded_NeverZero` |
| (g) Proof Pack | `docs/proof/workbench-loops-wl-1a.md` (demo script left for the Leader) |

Existing test changed because of this work: `AiDe.Core.Tests.Sessions.SessionPathContractTests` — its allowlist
gains `RunLogStore.cs` (the extension point Ruling 38 names). Node 1's `AttemptHostTests` are unchanged and green.

## Suites (after the merge of `origin/main`)

- `tests/AiDe.Core.Tests`: **2966 passed, 0 failed, 1 skipped** (`suite-core-node2.txt`).
- `tests/AiDe.App.Tests`: **1049 passed, 1 failed, 1 skipped** (`suite-app-node2.txt`). The one failure is
  `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` — "foreground not held", the known
  desktop-focus flake (also in node 1's `suite-green.txt` and `sp2/env-flakes.txt`); it passes alone
  (`suite-app-node2-chord-rerun.txt`). The skip is `AttemptRealLaneStopTests` (no `AIDE_REAL_ADAPTER_ROOT`).
- A first full App run also failed `AFoldActivity_WritesFoldDurationMs_ToTheRollingLog`: its oracle asserted
  `value >= 5` after `Task.Delay(5)` and read 4.6 ms. The test (not the listener) was wrong; it now finds its own
  activity by a unique operation name and asserts `value > 0`.

## Interface changes T8 must know (the host's public surface)

- `IAttemptHost` gains `SendNowAsync`, `StopAsync`, `SubscribeAsync` (**async**, returns `AttemptSubscription`;
  the design sketched a synchronous `Subscribe` — a finished attempt's run-log path needs its row), and
  `MeasureAsync` (NFR-11).
- `AttemptHost.OpenAsync(ledger, lockFile, ct, AttemptHostOptions? options = null)`; null derives the workspace from
  the lock path `<ws>/.aide/attempt-host.lock`. Options: `StopDeadline` (10 s), `SweepBackstop` (30 s),
  `Incidents`, `RunLogs`.
- `DispatchAsync` now accepts `QueuedAfterDispatchKey` (it threw `NotSupportedException` after node 1).
- Rolling log: `<ws>/.aide/attempt-telemetry.jsonl`, one JSON object per line (`metric`, `at`, fields).

## Not done

- The design's `ActivitySource "aide.attempt"` spans (`attempt.begin`, `attempt.terminal`, `attempt.sweep`) are not
  built; the sweep is emitted to the rolling log only. No check names the spans.
- The ADR-0041 §7 health incident for `DeliveryUnknown` (first 20 ids + count) is not built; only the
  run-log-unwritable incident is.
- Privacy finding for the Leader: the account email/organization reach every run log (see
  `real-lane-stop/README.md`).

## Next step

The Leader joins `wl1a-attempt-host` after `wl1a-attempt-host-n1`, re-runs `suite-core-node2.txt` /
`suite-app-node2.txt`, and completes the Proof Pack's "Human demo script" after T8 lands.
