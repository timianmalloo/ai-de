---
id: proof-workbench-loops-wl-1a
title: "Proof Pack: WL-1a — the revision ↔ attempt seam (walking skeleton)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [proof, workbench-loops, wl-1a, attempt, run-log, stop, sweep, redaction, nfr-11]
links:
  - { to: architecture, rel: implements }
  - { to: adr-0041-execution-attempt-lifecycle, rel: implements }
  - { to: design-workbench-loops-wl-1a, rel: implements }
review-by: "2026-11-18"
summary: >-
  The slice Proof Pack for WL-1a: one row per §WL.11 WL-1a check, each citing its evidence folder. The host
  checks (T5) are red-then-green with the recorded real claude-code Stop (Interrupted); the schema checks are
  T2's, the redaction function T7's, the folds T6's and the App binding T8's. The human demo script is left
  for the Leader after T8 joins.
---

# Proof Pack: WL-1a — the revision ↔ attempt seam

- **Change:** branches `wl1a-attempt-host` (T5, nodes 1–2), `wl1a-schema` (T2, on main), `wl1a-runevent` (T3, on
  main), `wl1a-redaction` (T7, on main at `7c54de9e`), `wl1a-folds` (T6), `wl1a-notebook-binding` (T8), joined by
  the Leader.
- **Spec / design:** `docs/architecture.md` §WL.11 (the WL-1a row) · ADR-0039, ADR-0041, ADR-0042 ·
  `docs/design/workbench-loops-wl-1a.md`.
- **Tier:** T2.
- **Author / date:** T5 (`wl-t5-host`), 2026-10-19. Each row's evidence is the cited file; the Leader re-runs at the join.

## Claims & evidence — one row per §WL.11 WL-1a check

| # | Check (§WL.11, verbatim) | Owner | Tests (exact) | Evidence (red → green) | Confidence |
|---|---|---|---|---|---|
| 1 | write-ahead crash injection at W1–W4 → `OutcomeUnknown{cause}`, never resend | T5 n1 | `AttemptHostTests.ACrashInAWindow_SweepsToDeliveryUnknown_AndNeverResends(W1..W4)`; `AttemptGateTests.Sweep_*` | `host/write-ahead-crash-windows-red.txt` → `-green.txt` | Verified |
| 2 | same `dispatch_key` twice → one launch | T5 n1 | `AttemptHostTests.SameDispatchKeyTwice_LaunchesOnce`, `.TwoConcurrentDispatchesOfOneKey_LaunchOnce`; `AttemptGateTests.BeginTwice_*`, `.BeginAgain_*` | `host/dispatch-key-idempotency-*.txt` | Verified |
| 3 | stale-prepare refusal | T5 n1 | `AttemptHostTests.AStalePrepare_IsRejected_AndStartsNoProcess` | `host/stale-prepare-*.txt` | Verified |
| 4 | Stop: `session/cancel` sent → `acp.result cancelled` observed → `Interrupted` | T5 n2 | `AttemptStopTests.Stop_SendsSessionCancel_AndTheEnginesCancelledReplyIsInterrupted`, `.Stop_ThatIsNeverAcknowledged_IsDeliveryUnknownStopNotAcknowledged_AtTheDeadline`, `.APermissionAskedDuringStop_IsAnsweredCancelled_AndRecordedAsPermissionCancelledByStop`; real lane: `AttemptRealLaneStopTests.Stop_OnTheRealClaudeCodeLane_EndsInTheAdrsTerminal_AndIsRecorded` | `host/node2-app-red.txt` → `host/node2-app-green.txt`; real run `host/real-lane-stop/` (Interrupted: mark 86, reply seq 143) | Verified |
| 5 | token cancel alone ⇒ not `Interrupted` | T5 n2 | `AttemptStopTests.ATokenCancelAlone_TheHostsOwnTeardown_IsNeverInterrupted`; `RunLogOrderTests.AStopRequestedTurn_StaysStopRequested_WhenAWaitOrAResumeArrives` (ADR-0041 §4 fold precedence) | `host/node2-app-*.txt`, `host/run-log-core-*.txt` | Verified |
| 6 | migration golden round-trip incl. WAL snapshot/restore and fresh-vs-migrated `sqlite_master` equality | T2 | see T2's pack | `workbench-loops-wl-1a/sp1/README.md` | Verified by T2 (cited, not re-run here) |
| 7 | the DM11 forbidden-update test | T2 | see T2's pack | `workbench-loops-wl-1a/sp1/README.md` | Verified by T2 (cited) |
| 8 | the constraint table's violation tests | T2 | see T2's pack | `workbench-loops-wl-1a/sp1/README.md` | Verified by T2 (cited) |
| 9 | **NFR-11 oracle**: `Dispatched.ts`, terminal `ts`, events, bytes, outcome, `usage` or Not recorded | T5 n2 | `AttemptObservabilityTests.EveryAttempt_HasDispatchedAndTerminalTs_EventCount_Bytes_Outcome_AndUsage`, `.AnAttemptWithNoUsage_ReadsNotRecorded_NeverZero` | `host/node2-app-*.txt`; real lane `host/real-lane-stop/measures.json` | Verified |
| 10 | `attempt.sweep{count,last_sweep_at}` emitted even at zero | T5 n2 | `AttemptQueueAndSweepTests.TheStartupSweep_EmitsAttemptSweep_EvenAtZero`, `.TheBackstopTimer_SweepsAndEmits_OnItsInterval`, `.AnEngineExit_SweepsAnAbandonedDispatchedAttempt_ToRelayLost` | `host/node2-app-*.txt` | Verified |
| 11 | redaction fixture: sentinel in a frame ⇒ run-log bytes clean + redaction row | T7 + T5 n2 | function: T7's `FrameRedactionTests`; end to end: `AttemptRedactionTests.ASentinelInAFrame_IsStoredRedacted_WithARedactionRow_AndNoSubscriberSeesIt` | `workbench-loops-wl-1a/redaction/` (T7); `host/redaction-wiring-red.txt` → `-green.txt` | Verified |

Supporting host checks (the seam the rows above run on), red then green in `host/node2-app-*.txt` and
`host/run-log-core-*.txt`: run log append-before-publish (`RunLogOrderTests.EveryEvent_IsInTheRunLog_BeforeThePeerPublishesIt`),
`RunLogUnwritable` (`RunLogOrderTests.ALineThatCannotBeMadeDurable_*`, `AttemptRunLogTests.AnUnwritableRunLogLine_*`),
relay fan-out (`AttemptRunLogTests.TheRelay_WritesTheSameInstanceToEverySubscriber_AndTracksTheHighWaterSeq`),
subscribe + catch-up (`AttemptRunLogTests.ARun_WritesItsRunLog_AndASubscriberSeesEveryEventOnceInOrder`), queue and
Send-now (`AttemptQueueAndSweepTests.AQueuedSuccessor_*`), `fold.duration_ms`
(`AttemptObservabilityTests.AFoldActivity_WritesFoldDurationMs_ToTheRollingLog`), G2/G3 uniqueness and the sole host
(node 1, `host/gate-*.txt`, `host/sole-host-*.txt`).

Slice deliverables owned by other tracks (cited; their folders join with them):

| Deliverable (§WL.11 "Builds") | Owner | Evidence |
|---|---|---|
| `WorkspaceSchema` v2, migration chain, WAL-safe snapshot, the dims and widened facts | T2 | `workbench-loops-wl-1a/sp1/README.md` |
| `RunEvent` envelope fields, `TurnState` +6 | T3 | `workbench-loops-wl-1a/sp2/` |
| folds `AttemptState` + `Currentness` | T6 | T6's evidence folder — not on main at `6c9782ca`; the Leader cites it at T6's join |
| the `session-document` kind re-bound to notebook + attempt; CTS/queue moved to the host | T8 | T8's evidence folder — pending T8's join |

## Oracles — why each is trustworthy

- **Far-end counts.** Prompts, cancels and frames are counted in the engine's own trace (the stub's
  `.aide-stub-<pid>.log`), not by the host that sends them, so a host that lies about what it sent fails.
- **Rows read back.** Every host assertion reads the receipt from the real SQLite store after the terminal commit.
- **Red first.** Every T5 test above failed for its stated reason before the code (the `*-red.txt` files), at commits
  `6aad0db4` (node 1), `1b560f5d` and `07922bfd` (node 2).
- **The real lane.** Check 4 ran once against `claude-agent-acp` 0.75.1 on the operator's Max subscription.

## Residual risk

- **Flagged — personal data in the run log.** The adapter's `_auth/status_update` puts the account email and
  organization in every run log; v1 redaction covers credentials only (`host/real-lane-stop/README.md`, "Finding").
- **Flagged — JSON-escaped secrets.** `FrameRedaction` v1 scans the raw line and does not decode JSON string escapes
  (its own remarks); a secret the adapter writes as `\uXXXX` escapes is not matched.
- **Inferred — one host per workspace** rests on the lock file; a stale lock after an OS handle leak is the residual.
- **Not built in WL-1a:** the design's `aide.attempt` spans (`attempt.begin` / `attempt.terminal`) — the rolling log
  carries `attempt.sweep` and `fold.duration_ms`; the relay retention cap, steer, retry UI and budget are WL-4.
- **One real Stop run** proves the adapter version it ran; a new adapter release re-opens check 4.

## Human demo script

*Left for the Leader, after T8 lands.*
