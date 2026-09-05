# Spike S1 — Harness OTLP ingest shape (FINDINGS)

**Status:** complete · **Date:** 2026-08-30 · **Result:** PASS (`dotnet run` exit 0) · **Verdict:** the ingest wire's contract is established; the deterministic mapping is safe to build.

## Question

Before designing the daemon ingest wire (Loomkeeper Phase 1), what is the contract by which a harness's telemetry becomes a `SessionBinding` (registration) and `ObservedSpan`s (observation)? Which parts are stable, and which are preview and must be pinned?

## Method

Pure, no live harness and no network: the spike drives **`System.Diagnostics.Activity`** — the real OpenTelemetry span primitive that an OTLP exporter serialises — and maps a representative span and a representative registration event to our `ObservedSpan` / `SessionBinding`. It does **not** stand up an OTLP transport, because OTLP/HTTP is a stable, non-preview protocol adoptable without a spike; what needed proving was the **field mapping** and the **preview attribute vocabulary**.

## Findings

1. **Two event kinds, one dual path (Verified — KB `agentic-session-observability` S1/S2).** A harness supplies (a) **OTel spans** for operations, and (b) a **registration / session-start event** for durable identity. Native OTLP is the preferred path where a harness emits it (Claude Code does, via `OTEL_EXPORTER_OTLP_ENDPOINT`); an **injected coordination contract** is the fallback for harnesses that do not.

2. **The subprocess gap is real and load-bearing (Verified — S2).** Claude Code **does not pass `OTEL_*` into subprocesses** (Bash, hooks, MCP servers, language servers), so those operations emit no OTLP. The wire must render them **Not Recorded / Partially Observed**, never absent-as-healthy. This is the Blind Spot source the Observatory already renders.

3. **OTel span → `ObservedSpan` mapping (proven):**
   | ObservedSpan field | OTel source |
   |---|---|
   | `SessionId` | span attribute `session.id` |
   | `TraceId` | `Activity.TraceId` (32 hex) |
   | `SourceSpanId` | `Activity.SpanId` (16 hex) |
   | `OperationName` | `Activity.DisplayName` (span name) |
   | `SpanId` (ours) | content-addressed SHA256 of `(session, trace, source)` — computed, not from OTLP |

4. **Registration → `SessionBinding` mapping (proven):**
   | SessionBinding field | Source attribute |
   |---|---|
   | Repository | `repo.canonical_path`, `repo.display_name` (from the injected contract / environment) |
   | Worktree / Terminal / Agent | `worktree.*`, `terminal.id`, `agent.name` |
   | **Harness** | OTel resource attribute **`service.name`** (+ `service.version`) |
   | **Model** | GenAI attribute **`gen_ai.request.model`** (+ `gen_ai.model.version`) |
   | Trust | **Verified** when the harness names itself via `service.name`; **Asserted** when only environment-asserted |

5. **GenAI attributes are preview — pin them (Verified — S1).** `gen_ai.system`, `gen_ai.request.model` and the agent/tool span vocabulary are marked **Development** in the OpenTelemetry semantic conventions. The wire **pins this attribute snapshot** and treats a schema-version change as a contract change (a regression gate), rather than tracking upstream churn silently.

6. **Unknown harness/model degrade honestly (proven).** A harness that emits no `service.name` / `gen_ai.request.model` maps to `Harness = null` / `Model = null` (rendered **Not Recorded**) and `Trust = Asserted` — the session is still observable and scorable on available evidence (spec US-13).

7. **OTLP `session.id` is asserted, not authority (Verified by design — ADR-0020 trusted-registrar-harness-model-identity).** A span's `session.id` says which session a process *claims*; it is **not** proof. The wire must bind incoming spans to the **per-session capability** issued at registration (already built: `TrustedRegistrar` + `SpanIngest`), so a local process cannot forge spans into another session — the "local OTLP forgery" risk the KB open-questions raise.

## Consequences for the design

- The wire is a **dual-path adapter**: an OTLP receiver (spans) + a registration adapter (injected contract / session-start), both funnelling through `TrustedRegistrar.Register` and `SpanIngest.Ingest` — the capability check is the trust anchor.
- The deterministic **span/registration attribute mapping** is safe to implement now as `OtelSpanMapper` (pure, testable). The **OTLP transport hosting** (receiving real exports) and the **daemon integration** remain, and the **GenAI schema snapshot** is pinned with a regression gate.

## Confidence

| Claim | Evidence | Label |
|---|---|---|
| Activity is the OTLP span primitive and carries trace/span/name | Ran the spike; fields populated | Verified |
| The two mappings are correct and degrade honestly | Spike PASS (5 span + 4 reg + 3 opaque checks) | Verified |
| OTel GenAI vocabulary is Development-status | KB S1 (fetched OTel semconv) | Verified — pinned |
| Claude Code subprocess OTLP gap | KB S2 | Verified |
| Real OTLP transport hosting behaves as mapped | not run (transport out of spike scope) | Inferred — stable protocol, adopt without spike |
