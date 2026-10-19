# Brief — T5 `wl1a-attempt-host` (slice WL-1a) · Claude Code · Opus · hard track

**Goal (plan row):** Make an execution attempt write-ahead, deterministic, durable and honestly stoppable on
the real claude-code lane.
**Done when:** the design doc committed first; crash injection at W1–W4 ⇒ `DeliveryUnknown{cause}`, never
resend; same `dispatch_key` twice ⇒ one launch; stale-prepare refusal; Stop on the **real** claude-code lane:
`session/cancel` sent ⇒ `acp.result cancelled` observed ⇒ `Interrupted`; token cancel alone ⇒ not
`Interrupted`; the NFR-11 oracle; `attempt.sweep{count,last_sweep_at}` at zero; the redaction fixture end to
end (sentinel ⇒ run-log bytes clean + redaction row); each red then green; the slice Proof Pack.
**Tier** T2 · **fan-out** 2 (only the `/design-slice` adversaries: Distributed Systems Architect and Simplifier,
Adversary Mode, read-only) · **budget** 400 tool calls · 400k (split at the ceiling into host-gate and
Stop+sweep halves via `{PROOF}/handoff.md`) · 8 h · **deadline** 8 h (design doc committed by +1.5 h; the
host-interface commit by +2.5 h).
**Convergence:** every check above red then green and the Proof Pack drafted. Nothing from WL-4 (steer,
budget enforcement, relay retention cap, per-family Stop) and nothing for the other five families.

## Stage 1 — `/design-slice` WL-1a (inside this track)
Follow `.claude/skills/design-slice/SKILL.md` (read its `reference/*.md` only at its stage). Write
`docs/design/workbench-loops-wl-1a.md` (V2 frontmatter: `id: design-workbench-loops-wl-1a`, `type: design`,
`owner: "@timianmalloo"`, links `implements` → `adr-0041-execution-attempt-lifecycle`, `depends-on` →
`adr-0039-workbench-records-durable-representation`, `review-by: "2026-11-18"`; **Markdown only — no HTML**).
It designs the whole WL-1a seam so tracks T6 (folds), T7 (redaction function) and T8 (App binding) build to
it: the host's shape (`GovernedRunHost` is a `static class` today, `:48`; its other caller is
`ConductorEntry.cs:80`, the comment at `:120-125` warns about it and `CompositionRootLedger` counts calls),
where the attempt row and outcome rows are written (App ↔ daemon IPC: today's dispatch IPC is
`src/AiDe.Core/Ipc/WorkspaceOperations.cs:71` `DispatchFinalizeRequest`, `WorkspaceClient.cs:101`,
`IWorkspaceDispatch.cs:29`; insert-as-gate must be **one store transaction**), the run-log line format
(T3's `RunEvent` serialization), the subscriber channels, and the **host interface T8 consumes** (dispatch a
prepared revision, queue, Stop, read an attempt and its events, subscribe). Name the patterns (Receipt Ledger
4.3, Hot Path Bypass 1.4). Run the adversarial review as two read-only sub-agents and record their verdicts.
Commit (`docs: design WL-1a`), then mail the Leader:
`coord mail send --to wl-leader2-20261019 --kind note --body "design committed <sha>"`.

## Stage 2 — implement. The lines this row builds (quoted, DC-189) — read `docs/adr/0041-execution-attempt-lifecycle.md` whole
- §1: "`dispatch_key = sha256(envelope_id ‖ projection_sha ‖ queued_after_dispatch_key ‖ parent_dispatch_key)`"
  · "`run_id := dispatch_key`" · "**read-before-write inside one store transaction; an existing key returns the
  existing receipt and launches nothing.** The host launches only when *its own* insert of the `Dispatched`
  outcome row committed (partial unique index)" · "Order of writes: attempt row (`Pending`) →
  `AcpEngineProcess.Start` → worktree provision (`WorktreeProvisioned` row) → `session/new` → **`Dispatched` row →
  `client.PromptAsync`** (the consequential write) → terminal row. The host refuses to launch when the current
  envelope fold's `projection_sha` ≠ the attempt's (`Rejected`, no process started; AC-E04b)."
- §2 terminals: "stopReason `end_turn`/`max_tokens` → `ResponseComplete`; `cancelled` → `Interrupted`; an
  **`error` reply correlated to the prompt id** → `Failed`; prompt timeout → `DeliveryUnknown{prompt_timeout}`;
  `EngineStreamEnded` / `IOException` / pump cancelled → `DeliveryUnknown{relay_lost}`" · "**At most one terminal
  row per attempt**" · "**Outcome row first, then the envelope's `consumed` row**".
- §3: "`RunLogStore` (WL-1a) appends and flushes one line per `RunEvent` to `runs/<dispatch_key>.jsonl`
  **before** `Events.PublishAsync` in `AcpPeer.DispatchAsync`; frames are stored redacted at ingest (ADR-0042 §6).
  `RunEventRelay.Publish` writes the same instance into **N per-subscriber unbounded channels** (lane, fold,
  evaluation); no component ever takes a second reader on one channel".
- §4 Stop: "append `StopRequested` carrying the relay's high-water `seq` → `AcpPeer.NotifyAsync
  ("session/cancel")` (a notification serialised through the same write gate; added to `AcpPeer`, not
  `AcpLaneClient`) → answer every outstanding permission request on the attempt with outcome `cancelled`,
  recorded as its own outcome kind `PermissionCancelledByStop` … → wait on the prompt Task with a **bounded
  deadline** (10 s, config) → on the native `cancelled` reply with `seq` greater than the high-water mark:
  `Interrupted`; on deadline: the terminal is `DeliveryUnknown{stop_not_acknowledged}` **unconditionally**" ·
  "Process exit caused by the host's own teardown **never** yields `Interrupted`."
- §4 sweep: "Predicate: *any attempt row with no terminal outcome row, no live host-owned run, **and no
  `queued_after_dispatch_key` whose predecessor lacks a `ResponseComplete` row*** → `DeliveryUnknown` with cause
  `never_dispatched` (no `Dispatched` row), `relay_lost`, or `host_restart`" · "Triggers: engine process exit …,
  host startup, and a 30 s backstop timer; every run emits `attempt.sweep{count, last_sweep_at}` to the sidecar even
  when zero" · "No resend or retry is triggered by inspection, reconnection or navigation".
- §4 run-log failure: "A line that cannot be made durable is not published: the pump fails the prompt with
  `RunLogUnwritable`, the host writes `DeliveryUnknown{run_log_unwritable}` and a sidecar incident".
- §7 NFR-11: "Every attempt has: `Dispatched.ts` and terminal `ts` (duration derived), event count, bytes out,
  outcome, `usage` or Not recorded".
- Queue (§4) is in scope only as far as the WL-1a demo needs (a queued successor is its own attempt with
  `queued_after_dispatch_key`, drained only after `ResponseComplete`); Steer and Retry UI are WL-4.
- DC-221 applies to every drain you write: every completion source the exit depends on must be able to wake the
  loop (`Task.WhenAny`); `TheDrainEndsWhenThePromptEndsTests` must stay green.
- Crash-injection pattern to reuse: ADR-0010's `P1-DISPATCH-01..04` tests (grep `P1-DISPATCH` in `tests/`).
- The real claude-code lane: find how `GovernedRunHost` launches it today (`AcpEngineProcess`, the adapter path in
  the engine catalog). The Stop check must run against the real lane once — a test fixture engine for the unit
  tests, plus one recorded real run whose run-log and outcome rows you save under `{PROOF}/real-lane-stop/`. If
  the adapter does not honour `session/cancel`, the honest observed result is `DeliveryUnknown{stop_not_acknowledged}`
  — record that; it is the ADR's terminal, not a failure of yours.

## Inputs already on `main` (read the code, do not assume)
T2's v2 schema and record API (`src/AiDe.Core/Store/Records/`, `StoreWriter`, `StoreReader`), T3's `RunEvent`
envelope fields and `TurnState` values. T7 delivers `src/AiDe.Core/AgentPlane/FrameRedaction.cs` (a pure
function); you wire the call inside the mapper. If T7 has not landed when you need it, wire against its
documented signature in your design and let the redaction fixture stay red until it joins — say so.

## Owned authored paths (WL-1a window)
`src/AiDe.App/Conductor/GovernedRunHost.cs`, `RunEventRelay.cs`, `ConductorEntry.cs`, `CompositionRootLedger.cs`,
`src/AiDe.Core/AgentPlane/AcpPeer.cs`, `AcpLaneClient.cs`, `AcpEngineProcess.cs`, `AcpRunEventMapper.cs`, new
`src/AiDe.Core/AgentPlane/RunLogStore.cs`, `src/AiDe.Core/Sessions/SessionPaths.cs`,
`src/AiDe.Core/Dispatch/BoundaryDispatcher.cs`, `DispatchService.cs`, `src/AiDe.Core/Store/StoreWriter.cs`,
`StoreReader.cs`, `src/AiDe.Core/Ipc/WorkspaceOperations.cs`, `WorkspaceClient.cs`, `IWorkspaceDispatch.cs`,
`src/AiDe.Daemon/Program.cs`, new `tests/AiDe.Core.Tests/Attempts/**`, `tests/AiDe.App.Tests/Conductor/Attempts/**`,
existing tests that break because of your changes (name each in your report), `docs/design/workbench-loops-wl-1a.md`,
`docs/proof/workbench-loops-wl-1a.md`, `docs/proof/workbench-loops-wl-1a/host/**`.
**Not yours:** `src/AiDe.App/Workbench/**` (T8), the folds (T6, `src/AiDe.Core/Workbench/Records/Folds/`),
`FrameRedaction.cs` (T7). T8 will send you seam requests for IPC ops or store reads; answer them.

## Proof Pack (yours for the whole slice)
`docs/proof/workbench-loops-wl-1a.md` from `docs/ai-forward-pack/templates/proof-pack.template.md` (V2 frontmatter
`id: proof-workbench-loops-wl-1a`, `type: proof-pack`, `phase: "workbench-loops WL-1a"`, `owner: "@timianmalloo"`,
`links: [{ to: architecture, rel: implements }]`, `review-by: "2026-11-18"`; Markdown only). One row per §WL.11 WL-1a
check: yours, plus T2's (`docs/proof/workbench-loops-wl-1a/sp1/README.md`), T6's, T7's and T8's evidence folders,
cited by path. Leave a "Human demo script" section the Leader completes after T8 lands.

## Not in scope
WL-4 controls (steer, retry UI, budget enforcement, relay retention cap, compaction), other families' transports,
permission UI, `HarnessProfileBinding`/route fingerprint beyond carrying the column (WL-3), any App/Workbench file.

## Hand-overs from the spine (Leader, 2026-10-18 — read before designing)
- **T3 interim behaviour you close:** `SessionDocumentSurface.cs:653` now calls `RunChannelSessionThread.RequestStop(ordinal)`
  after its own token cancel, so a stopped turn sits at `StopRequested` (in flight) until the host acknowledges. Your Stop
  path must conclude it (`Interrupted` on the native `cancelled` reply, `DeliveryUnknown{stop_not_acknowledged}` at the
  deadline). Also yours (added to your owned paths): `src/AiDe.Core/Presentation/Sessions/RunChannelSessionThread.cs` —
  `Wait` and `Resume` can move a `StopRequested` turn back to `Running`; ADR-0041 §4 "Fold precedence: `StopRequested`
  dominates `Waiting`" forbids that — red test first.
- **T2's departures from ADR-0039 (accepted by the Leader):** vocabulary/composite references are `BEFORE INSERT`
  triggers with stable codes (`AIDE-STORE-VOCABULARY-VIOLATION`, `AIDE-STORE-REFERENCE-VIOLATION`); dims have no
  `valid_to_seq` (latest = max `ingress_seq` per key); `WorkspaceStore.Open` returns a `StoreOpenReport`; a newer file opens
  read-only (`AIDE-STORE-NEWER-SCHEMA-READ-ONLY`). The PTY path binds attempts to revisions, so "no attempt ever bound a legacy
  row" is not asserted. Build on the code, not on the ADR's wording, where they differ.

## Split rule (GO9 — T4 ran 344/280 calls and 632k context as one node)
This track is sized as **two nodes**. Node 1 is the design doc + the host interface commit + the gate/write-ahead/crash-window
checks. Node 2 is Stop + sweep + run-log + redaction wiring + NFR-11 + the Proof Pack. At **200 tool calls or 300k context**,
whichever comes first, commit what is green, write `{PROOF}/handoff.md` (done / red / next, with exact test names), mail the
Leader `--kind note "handoff <sha>"` and stop. The Leader starts node 2 as a fresh session in the same tree.
