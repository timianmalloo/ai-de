# Brief — T6 `wl1a-folds` (slice WL-1a) · Claude Code · Sonnet

**Goal (plan row):** Derive non-terminal attempt state and currentness as pure folds.
**Done when:** the pure folds (ADR-0041 §2: Running / Waiting / Denied / OverLimit from the run log;
`StopRequested` dominates `Waiting`; a cited seq absent ⇒ Not recorded); currentness (ADR-0040 §5); fixture
tests red then green; a `fold.duration_ms` emission test.
**Tier** T2 · **fan-out** 0 · **budget** 140 tool calls · 400k · 3 h · **deadline** 3 h.
**Convergence:** the fold fixtures are green. No UI, no host, no store writes.

## Binding lines (quoted, DC-189)
- ADR-0041 §2: "Running / Waiting (one state for input *or* permission, per spec §C2) / Denied (last permission
  decision on the attempt was reject, no later event) / OverLimit (a budget or context-ceiling event) | **fold over
  the durable run log** for `run_id`; an uncorrelated `acp.result`/`acp.error` never yields a terminal state" ·
  "The fold is pure, implemented once in Core, and reads Not recorded for any cited seq the log lacks. Fold
  store-writes (none exist by design — terminals are the host's) never run inside the host's drain sink" ·
  §4 "Fold precedence: `StopRequested` dominates `Waiting`." · terminal states come from the outcome rows (T2's
  `OutcomeRow`), never from events.
- ADR-0040 §5: "A journey's scope snapshot is compared to the live scope on every open/refresh; a changed HEAD, PR
  head, intent revision or dirty-content hash marks dependent preparations, assessments and obligations `stale` by
  a derived rule, retaining the historical rows (AC-P01c, AC-B02b)." For WL-1a the consumer is "edit and see the
  old result marked stale": an attempt bound to `(notebook_id, revision_no, projection_sha)` is stale when the
  notebook's current revision or the live envelope's `projection_sha` differs. Copy: "This output belongs to an
  earlier draft. Run again to refresh it." (spec §C3).
- ADR-0041 §7: "`fold.duration_ms` emitted to the rolling log" — find how other metrics are emitted to the rolling
  log/sidecar in Core and reuse that path; the test asserts one emission per fold evaluation.

## Inputs on `main` (read the code)
T2's record types (`src/AiDe.Core/Store/Records/`), T3's `RunEvent` and `TurnState`, and T5's design doc
`docs/design/workbench-loops-wl-1a.md` (the run-log line format and the fold's inputs). The fold's output is a
`TurnState` value plus `HasEvidence` (a derived bool, **not** a state; for WL-1a it reads `evidence_link_fact`
through T2's reader and is false when none exist).

## Owned authored paths
new `src/AiDe.Core/Workbench/Records/Folds/AttemptStateFold.cs`, `CurrentnessFold.cs` (+ small helper types in the
same folder), new `tests/AiDe.Core.Tests/Workbench/Folds/**`, `{PROOF}/**`.

## Not in scope
Writing any row; the host; the relay subscriber wiring (T5 wires the fold channel); UI (T8); acceptance/closure
folds (WL-1b); readiness (WL-3).
