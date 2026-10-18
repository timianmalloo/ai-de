---
id: adr-0041-execution-attempt-lifecycle
title: "ADR-0041 — Execution attempt: deterministic key with insert-as-gate, host-asserted terminal outcomes, publish-side fan-out, and distinct queue / steer / stop / retry semantics"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [architecture, execution, attempt, run-event, idempotency, cancellation, queue, workbench-loops]
links:
  - { to: architecture, rel: implements }
  - { to: spec-workbench-loops-ux, rel: implements }
  - { to: adr-0039-workbench-records-durable-representation, rel: depends-on }
  - { to: adr-0010-two-phase-dispatch-receipt, rel: refines }
  - { to: adr-0027-acp-lane-separate-shape, rel: refines }
  - { to: adr-0034-envelope-event-store, rel: depends-on }
  - { to: architecture-agent-plane, rel: refines }
  - { to: adr-0042-native-harness-profile-binding, rel: relates-to }
review-by: 2027-04-18
summary: >-
  An execution attempt is ADR-0010's dispatch receipt applied to the native-harness run: a
  deterministic dispatch key, an insert that is the gate, a Dispatched row written before the
  consequential prompt write, terminal outcomes written by the host from its own correlated reply,
  non-terminal states folded from a durable per-attempt run log, and a sweep with a stated
  predicate. The relay fans out at the publish side. Queue drains only after ResponseComplete;
  Steer goes through Prepare; Stop is a bounded native notification whose only acknowledgment is
  the harness's cancelled reply; Retry is a new linked attempt the user chooses.
review-suggested:
  - { by: architecture, on: 2026-10-18, reason: "2026-10-18 workbench-loops architecture: §WL added, ADR-0039..0045 proposed, Perspective set re-rowed (ADR-0045 supersedes ADR-0017/0030 rows)" }
---

# ADR-0041: Execution attempt lifecycle, derived state and control semantics

- **Status:** Accepted — owner ruling 2026-10-18 (@timianmalloo); council record follows. Previously Proposed; council 2026-10-18 — Distributed Systems **VETO** on the first draft
  (non-deterministic key; second relay reader), both cleared in this revision together with its
  conditions 3–13; SRE and Simplifier conditions folded in. Re-review recorded in
  `docs/architecture.md` §WL.14.
- **Date:** 2026-10-18 (revised the same day after the gate)
- **Deciders (Peer Mode):** Distributed Systems Architect (author), SRE, Tech Lead
- **Context spec/architecture:** spec US-R01–R03, US-E04, AC-W02b, AC-A01b, NFR-03, NFR-11, §C2
  execution states; `architecture-agent-plane` §4, §7.2, §12; ADR-0010.

## Context (all Verified 2026-10-18 by the council's reads)

- `GovernedRunHost.RunAsync` (`:126`) is the one run root; `run_id` is minted inside it (`:148`)
  *after* any caller-side record; the engine process is `using`-scoped (`:171`) so a thrown
  exception kills it; `DrainAsync` calls the sink synchronously and does not swallow its exceptions.
- `RunEventRelay` is `SingleReader = true, SingleWriter = true` (`:31-35`) and its reader is handed
  to `SessionLane` (`SessionDocumentSurface.cs:561-562`); a second `ReadAllAsync` would **split**
  events, not duplicate them.
- `AcpPeer` exposes `RequestAsync` only (registers an id, times out); ACP `session/cancel` is a
  **notification** with no id and no reply; `AcpLaneClient` has `session/new`, `session/prompt`,
  `session/request_permission` (`:310-470`). The mapper tags every reply `acp.result`/`acp.error`
  without a request id (`AcpRunEventMapper.cs:113-120`); only the host's `await prompt` knows which
  reply ended the turn.
- Stop today cancels a `CancellationTokenSource` per turn (`SessionDocumentSurface.cs:835-837`) and
  shows `TurnState.Stopped` (`:653`) — a synthetic acknowledgment. The queue is one `_queued` slot
  (`:551`) drained only after `Completed`/`Answered` (Ruling 95, `:571-593`).
- `TurnState` (`SessionThread.cs:59`) is Core's one lifecycle encoding (DM7): Running, Waiting,
  Completed, Answered, Failed, Stopped, NotRecorded, Queued, Cancelled.
- The two-grain receipt exists only on the PTY path (`BoundaryDispatcher.Begin` returns
  `AlreadyAttempted` on an existing key, `:49-72`; `DispatchService.SweepPendingToUnknown` `:139`).
- `runs/<run-id>.jsonl` is reserved, not built (`SessionPaths.cs:28-32`); no file writer exists in
  `AgentPlane/` or `Conductor/`.
- `RunBudget` / `FanOutCap` are validated, never enforced (agent-plane §12).

LOA: P3 cognition/execution separated, P5 verification over plausibility, P8 idempotency at the
side-effect boundary. Patterns: Receipt Ledger (LOA 4.3), Hot Path Bypass (1.4).

## Decision

### 1. Identity and the gate (ADR-0010 re-applied)

`dispatch_key = sha256(envelope_id ‖ projection_sha ‖ queued_after_dispatch_key ‖ parent_dispatch_key)`
— deterministic from the prepared envelope, so a retried click or a lost IPC reply recomputes the
same key. `run_id := dispatch_key`. The attempt row (`dispatch_attempt_fact`, ADR-0039) is written
by `BoundaryDispatcher.Begin` semantics: **read-before-write inside one store transaction; an
existing key returns the existing receipt and launches nothing.** The host launches only when
*its own* insert of the `Dispatched` outcome row committed (partial unique index); the queue
drainer and the operator's Send-now/Retry both pass through this one gate, so no WPF-thread
ordering is load-bearing.

Order of writes: attempt row (`Pending`) → `AcpEngineProcess.Start` → worktree provision
(`WorktreeProvisioned` row) → `session/new` → **`Dispatched` row → `client.PromptAsync`** (the
consequential write) → terminal row. The host refuses to launch when the current envelope fold's
`projection_sha` ≠ the attempt's (`Rejected`, no process started; AC-E04b).

### 2. State: host-asserted terminals, folded non-terminals, one enum

`TurnState` is extended, not duplicated: `+ Preparing, StopRequested, DeliveryUnknown, Rejected,
Denied, OverLimit`; `Stopped` is renamed `Interrupted` with changed semantics (native
acknowledgment only) — the slice's red test asserts `TurnState` is never serialized (today only
in-memory sites exist, `RunChannelSessionThread.cs:131-331`), else `Stopped` stays as a legacy
read-only value. `HasEvidence` (`∃ evidence_link_fact` for the attempt) is a derived bool on
the view, **not** a state — an interrupted attempt can carry evidence.

| State | Source (one home) |
|---|---|
| Preparing / Queued / Rejected / StopRequested / DeliveryUnknown(cause) / ResponseComplete / Failed / Interrupted | **host-asserted `dispatch_outcome_fact` rows** — one vocabulary, the CHECK list in ADR-0039 §3 (which includes `PermissionCancelledByStop`); the UI/`TurnState` name for `DeliveryUnknown` is the same word. The terminals are written from the host's own `await prompt`, correlated by request id: stopReason `end_turn`/`max_tokens` → `ResponseComplete`; `cancelled` → `Interrupted`; an **`error` reply correlated to the prompt id** → `Failed`; prompt timeout → `DeliveryUnknown{prompt_timeout}`; `EngineStreamEnded` / `IOException` / pump cancelled → `DeliveryUnknown{relay_lost}` (a transport loss is never `Failed`, because its effects are unknown and Retry must not read as safe). **At most one terminal row per attempt**: partial UNIQUE `dispatch_outcome_fact (dispatch_key) WHERE state IN (terminals)`; the host deregisters the run as live only *after* its terminal write, and the process-exit trigger considers only runs no longer registered live; if two terminals ever exist the lowest `ingress_seq` wins and the second is reported as a defect row. **Outcome row first, then the envelope's `consumed` row** (`envelope_id`-keyed, idempotent) written by the same host path and by the sweep, with reasons `interrupted_native`, `delivery_unknown{cause}`, `rejected_stale` added beside the existing ones (`stopped_by_operator` keeps its token-kill meaning). |
| Running / Waiting (one state for input *or* permission, per spec §C2) / Denied (last permission decision on the attempt was reject, no later event) / OverLimit (a budget or context-ceiling event) | **fold over the durable run log** for `run_id`; an uncorrelated `acp.result`/`acp.error` never yields a terminal state |

The fold is pure, implemented once in Core, and reads Not recorded for any cited seq the log lacks.
Fold store-writes (none exist by design — terminals are the host's) never run inside the host's
drain sink; a persistence failure goes to the health-incident sidecar and never kills the engine.

### 3. Durable run log and publish-side fan-out

`RunLogStore` (WL-1a) appends and flushes one line per `RunEvent` to `runs/<dispatch_key>.jsonl`
**before** `Events.PublishAsync` in `AcpPeer.DispatchAsync`; frames are stored redacted at ingest
(ADR-0042 §6). `RunEventRelay.Publish` writes the same instance into **N per-subscriber unbounded
channels** (lane, fold, evaluation); no component ever takes a second reader on one channel — the
WL-4 census asserts one reader per channel. In-memory retention per attempt is capped (10 000
events; spill is the log; `relay.retained_events` / `relay.spilled` emitted).

### 4. Controls

- **Queue** appends a new attempt with `queued_after_dispatch_key`; the drainer dispatches it
  **only after the predecessor's `ResponseComplete`**; after `Failed`, `Interrupted` or
  `OutcomeUnknown` the successor stays `Queued` — "awaits you (predecessor <state>)" — and only an
  explicit Send-now moves it. At drain the queued revision is **recompiled** and its
  `projection_sha` compared; a mismatch caused by the predecessor's own worktree edits is reported
  as "context changed by attempt X — re-prepare, or send as prepared", both explicit user actions.
- **Steer** (WL-4, only where `capability:steer` is observed true, ADR-0042) is
  Prepare(amendment) → envelope → `session/prompt`: an attempt row with `amends_dispatch_key` and
  its own `projection_sha`, through `ComposerSendGate` on the WPF side, with the same stale-prepare
  refusal — never a second send path around the envelope.
- **Stop** = append `StopRequested` carrying the relay's high-water `seq` → `AcpPeer.NotifyAsync
  ("session/cancel")` (a notification serialised through the same write gate; added to `AcpPeer`,
  not `AcpLaneClient`) → answer every outstanding permission request on the attempt with outcome
  `cancelled`, recorded as its own outcome kind `PermissionCancelledByStop` (never a
  `permission_decision_fact`) → wait on the prompt Task with a **bounded deadline** (10 s, config)
  → on the native `cancelled` reply with `seq` greater than the high-water mark: `Interrupted`;
  on deadline: the terminal is `DeliveryUnknown{stop_not_acknowledged}` **unconditionally**, then
  the token is cancelled and whatever is already queued is drained for the log and the lane only
  (the pending entry is gone, so a late reply can no longer decide the state). Process exit caused
  by the host's own teardown **never** yields `Interrupted`. UI copy: "Stop requested. Waiting for the harness to acknowledge." Stop
  never implies rollback and never cancels queued successors; they are listed and cancelled by
  their own action. Fold precedence: `StopRequested` dominates `Waiting`.
- **Disconnect / crash — the sweep.** Predicate: *any attempt row with no terminal outcome row,
  no live host-owned run, **and no `queued_after_dispatch_key` whose predecessor lacks a
  `ResponseComplete` row*** → `DeliveryUnknown` with cause `never_dispatched` (no `Dispatched`
  row), `relay_lost`, or `host_restart`. A queued attempt whose predecessor is still running (or
  ended other than `ResponseComplete`) is never swept — after a restart it folds to "Queued —
  awaits you". The host registers the run as live **before** (or in the same transaction as) the
  `Pending` row, so a launch in progress is never swept. Triggers: engine process exit (for runs no
  longer registered live), host startup, and a 30 s backstop timer; every run emits
  `attempt.sweep{count, last_sweep_at}` to the sidecar even when zero, so "sweep did not run" is
  distinguishable from "nothing to sweep". A legitimately long `Waiting` attempt has a live
  host-owned run and is never swept. No resend or retry is triggered by inspection, reconnection
  or navigation (AC-R01b). The UI never offers Retry as *safe* from `DeliveryUnknown`.
- **Retry** is a user action after inspection that appends a **new** attempt with
  `parent_dispatch_key` = the **chain head** (the most recent attempt in the chain; Retry is
  offered only there), so a second retry never recomputes the first retry's key; the original row
  and its possible effects are retained (AC-R01c).
- **Run-log write failure.** A line that cannot be made durable is not published: the pump fails
  the prompt with `RunLogUnwritable`, the host writes `DeliveryUnknown{run_log_unwritable}` and a
  sidecar incident; the engine is torn down by the host, not by an unhandled exception.
- **Relay bound.** Per-subscriber channels are bounded with `DropOldest` counted as
  `relay.spilled`; a subscriber that fell behind catches up by `seq > last_seen` from
  `RunLogStore` (a bounded read, not a hot-path reader).

### 5. Permission binding

A `permission_decision_fact` names the exact request `(session_id, run_id, seq)` (unique) and
`arguments_sha = sha256(canonical {name, kind, rawInput, locations})`. A decision row is appended
only against a `permission.request (run_id, seq)` that already exists in the run log and stores
the sha the host computed from it; at answer time the host **recomputes** the sha from the live
request and refuses the row on mismatch; a row inserted before its request event is refused (no
pre-approval by future seq). The host answers request `(run_id, seq)` only with that row, consumed
once. Same action with
different `rawInput` → a new `Waiting`; an identical request issued twice → two decision rows; a
decision naming seq 5 offered for seq 9 → refused. `allow_always` is recorded as the native option
chosen but never short-circuits a later write/execute request on AI-DE's side (AC-A01b). A plan
acceptance or a model-seat ruling is a different fact and never answers a permission request
(AC-W02c).

### 6. Budget

`RunBudget` stays validated-not-enforced in WL-1 (agent-plane §12, recorded in §WL.13 as the
Unbudgeted-Loop residual). WL-4 enforces it at the attempt: the host consumes `usage` events and
writes `Failed{budget-exhausted}` / `OverLimit`; context ceiling and consumption budget are
distinct (NFR-11).

### 7. Observability (NFR-11, measured not modeled)

Every attempt has: `Dispatched.ts` and terminal `ts` (duration derived), event count, bytes out,
outcome, `usage` or Not recorded; `attempt.sweep`, `relay.retained_events`, `fold.duration_ms`
emitted to the rolling log; the health incident for `DeliveryUnknown` lists the first 20 attempt
ids plus a count (dedup key unchanged). The 3 a.m. walk is `dispatch_key` → outcome rows
(incl. worktree rows) → `runs/<dispatch_key>.jsonl` → permission decisions; the engine runs in a
kill-on-close job object, so after a host crash outstanding effects are bounded to pre-crash.
Diagnostics pass through the same redaction as native stderr before any retained sink.

## Alternatives considered

- **Per-click `attempt_id`:** rejected — a lost IPC reply or a race between the drainer and
  Send-now dispatches twice; ADR-0010's deterministic key with insert-as-gate already solves it.
- **Store `status` and update it:** rejected — Type-1 status is overwritten in reconnect races and
  cannot hold "stop requested *and* evidence arrived".
- **Fold terminals from `acp.result`:** rejected — replies are uncorrelated in the event stream; a
  late reply to another request would read as end-of-turn.
- **Second reader on the relay:** rejected by construction — `SingleReader = true` splits events.
- **Auto-drain the queue after any terminal:** rejected — sends a possibly-effective action onto a
  worktree whose state is unknown (NFR-03, Ruling 95).
- **Token cancel as Stop:** rejected — our own kill presented as the harness's acknowledgment
  (AC-R03c).

## Consequences

- **Positive:** at-most-once dispatch across crash, lost reply and race; every UI state has one
  named source; Stop is honest and bounded; the run survives restart.
- **Negative / accepted trade-offs:** `RunLogStore` is a second durable artifact with a stated
  commit order; per-family Stop acknowledgment stays Flagged until each qualification record; a
  kill during an in-flight tool call leaves effects unknown by construction.
- **Follow-ups / new risks:** `AcpPeer.NotifyAsync`; the `TurnState` rename of `Stopped` touches
  every consumer (red first); run-log growth/compaction (WL-4); per-route cancel for the non-ACP
  transports (ADR-0042).

## Evidence

- Code lines cited in Context — **Verified** 2026-10-18 (council reads).
- ADR-0010's crash-injection tests P1-DISPATCH-01..04 — the pattern reused — **Verified** (existing).
- Per-route Stop acknowledgment, `session/cancel` honoured by each adapter/CLI — **Flagged** per
  family until its qualification record.
