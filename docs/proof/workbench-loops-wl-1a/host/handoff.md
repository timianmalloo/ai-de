---
id: proof-workbench-loops-wl-1a-host-handoff
title: "WL-1a host (T5) — node 1 hand-off: gate, write-ahead, W1–W4 green; node 2 next"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [proof, handoff, attempt, gate, workbench-loops, wl-1a, t5]
links:
  - { to: adr-0041-execution-attempt-lifecycle, rel: implements }
  - { to: design-workbench-loops-wl-1a, rel: implements }
review-by: "2026-11-18"
summary: >-
  Track T5 node 1 (GO9 split) is done: design committed with both adversary verdicts, the host-interface
  commit, and the gate, dispatch_key idempotency, stale-prepare, W1–W4 crash-window and sole-host checks,
  each red then green. Node 2 (Stop, sweep triggers and emission, run log, redaction wiring, queue, NFR-11,
  the slice Proof Pack) starts from here in the same tree.
---

# WL-1a host — node 1 hand-off

Tree `C:\Projects\ai-de-wl1a-attempt-host`, branch `wl1a-attempt-host`, base `f1688dba`. Design:
`docs/design/workbench-loops-wl-1a.md` (read it first; its gate table and "Crash windows" are the contract).

## Done (node 1)

| Commit | What |
|---|---|
| `344f6420` | `docs: design WL-1a` — Simplifier PASS-WITH-CONDITIONS; Distributed Systems VETO, cleared on re-review (gate record in the design) |
| `66ae2be7` | host interface: `IAttemptHost`, `AttemptDispatch`, `AttemptStart`, `AttemptRun`; `IAttemptLedger` (`IWorkspaceDispatch.cs`); `AttemptKey`, `AttemptReceipt`, `AttemptErrorCodes` (`BoundaryDispatcher.cs`); IPC ops `attempt.begin/outcome/read/sweep`; `RunAsync(..., AttemptRun? attempt = null)` |
| `6aad0db4` | `test: red` — 17 tests, red evidence `*-red.txt` |
| _green_ | the gate (see below), `*-green.txt`, `DC-NEW-wl-t5-host` |
| _refactor_ | host types moved to `src/AiDe.App/Conductor/AttemptHost.cs` (seam request `req-01M599H0364THVM23WJJ68R11S` granted) |

Checks, red then green (test names exact):

| Check | Tests | Evidence |
|---|---|---|
| gate (G2/G3) | `AttemptGateTests.ASecondDispatchedRow_IsNotCommitted`, `.Dispatched_AfterATerminal_IsNotCommitted`, `.ASecondTerminal_IsNotCommitted_AndTheFirstStands` | `gate-red.txt`, `gate-green.txt` |
| dispatch_key idempotency (G0, L) | `AttemptGateTests.BeginTwice_WritesOneAttemptRow_AndTheSecondIsAlreadyAttempted`, `.BeginAgain_AfterTheEpochMoved_StillReturnsTheExistingReceipt`; `AttemptHostTests.SameDispatchKeyTwice_LaunchesOnce`, `.TwoConcurrentDispatchesOfOneKey_LaunchOnce` | `dispatch-key-idempotency-*.txt` |
| stale-prepare refusal (G1) | `AttemptHostTests.AStalePrepare_IsRejected_AndStartsNoProcess` | `stale-prepare-*.txt` |
| write-ahead crash windows W1–W4 + sweep predicate | `AttemptHostTests.ACrashInAWindow_SweepsToDeliveryUnknown_AndNeverResends(W1..W4)`; `AttemptGateTests.Sweep_ResolvesEachAbandonedAttempt_ToDeliveryUnknownWithItsCause`, `.Sweep_SkipsLiveAttempts`, `.Sweep_SkipsAQueuedSuccessorWhosePredecessorLacksResponseComplete`, `.ThePtySweep_NeverTouchesANativeAttempt` | `write-ahead-crash-windows-*.txt` |
| one host per workspace | `AttemptHostTests.ASecondHost_IsRefused` | `sole-host-*.txt` |

Core tests: `tests/AiDe.Core.Tests/Attempts/AttemptGateTests.cs`. App tests (product `RunAsync` spawning
`StubAcpAdapter`): `tests/AiDe.App.Tests/Conductor/Attempts/AttemptHostTests.cs`. Whole suites:
`suite-green.txt`.

## Red / not built (node 2 — none of these has a test yet)

- **Queue:** `AttemptHost.DispatchAsync` refuses `QueuedAfterDispatchKey` with `NotSupportedException`. Build: live
  from queue time (L at G0), launch after the predecessor's `ResponseComplete`, launcher re-reads and launches only
  a `Pending` attempt; `SendNowAsync` on `IAttemptHost` (design, "Queued launch" and the re-review conditions).
- **Stop:** `IAttemptHost.StopAsync`, `AcpPeer.NotifyAsync("session/cancel")`, `StopRequested{high-water seq}`,
  `PermissionCancelledByStop`, 10 s deadline ⇒ `DeliveryUnknown{stop_not_acknowledged}`; `cancelled` reply after the
  mark ⇒ `Interrupted` (today `AttemptTerminal.For` maps any `cancelled` to `DeliveryUnknown{relay_lost}` — the Stop
  path replaces that row). Token cancel alone ⇒ not `Interrupted`. One recorded real claude-code run under
  `real-lane-stop/`. `RunChannelSessionThread.Wait`/`Resume` must not move `StopRequested` to `Running` (red first).
- **Sweep triggers + emission:** startup sweep runs in `AttemptHost.OpenAsync` today; add engine-exit (`relay_lost`)
  and the 30 s backstop, and emit `attempt.sweep{count,last_sweep_at}` to the sidecar even at zero
  (`AttemptHost.LastSweep` holds the last result).
- **Run log + relay + subscribe:** `RunLogStore` (append+flush before `Events.PublishAsync`, line =
  `JsonSerializer.Serialize(RunEvent)`), `RunLogUnwritable` ⇒ `DeliveryUnknown{run_log_unwritable}`;
  `RunEventRelay.Subscribe()` fan-out; `IAttemptHost.Subscribe`. `AttemptHost.LaunchAsync` passes `sink: null` today.
- **Redaction wiring:** the mapper calls T7's `FrameRedaction`; the sentinel fixture end to end.
- **NFR-11 oracle:** terminal `detail` already carries `{stopReason, usage}` or `usage: "not recorded"`; add event
  count and bytes from the run log and the oracle test.
- **Proof Pack:** `docs/proof/workbench-loops-wl-1a.md`.

## Next step

Start node 2 with the queue (smallest, unblocks T8's queued turns), then run log + relay (unblocks T6's fold
input), then Stop on the stub, then the real-lane Stop run.
