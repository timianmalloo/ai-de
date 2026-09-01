---
id: design-watcher-coordination-contract
title: "Loomkeeper Injected Coordination Contract - Non-Pack Ingest Adapter"
type: design
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [loomkeeper, watcher, design, coordination, injected-contract, coord-core, phase-1]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: design-watcher-ingest-host, rel: refines }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: adr-0020-trusted-registrar-harness-model-identity, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Design for the Loomkeeper injected coordination contract (slice 2): a versioned, coord-core-append
  schema that lets a session from a repository WITHOUT the AI-Forward pack register and heartbeat over
  the same append-only ledger (one ledger, projected, not duplicated). A pure CoordContractParser reads
  the JSONL tolerantly (LOG-A leading newline, CRLF, blank/malformed skip, version pin, sort by at/seq),
  and an InjectedContractIngest adapter mints the capability at register, holds external-id->capability,
  and feeds the same TrustedRegistrar/IngestHost as the OTLP path. Contract established by spike S4.
---

# Design: Loomkeeper Injected Coordination Contract

- **Status:** Accepted · **Tier:** T2 · **Phase:** 1, slice 2 · **Refines:** [`design-watcher-ingest-host`](watcher-ingest-host.md) (feeds its `Register`/`Heartbeat`).
- **Established contract:** spike **S4** (`spikes/watcher-coord-contract/FINDINGS.md`, PASS) — the real `coord-core` writer's byte shape (sorted-key JSONL, open schema, `seq` auto-assign, LOG-A leading-newline guard, atomic `O_APPEND`) and the C# tolerant-read that consumes it.

## 1. Responsibility and boundary

One responsibility: **let a non-AI-Forward session register and heartbeat with Loomkeeper over the existing `coord-core` append log, without native OTLP** (spec US-5; architecture §6). It owns the **injected-contract schema** (versioned) and the **parse + map** from that log into the watcher domain. It borrows the registrar and the host.

**This is a file trust boundary, not a network one.** The append log is a local, forgeable surface (ADR-0007). So — symmetrically with the OTLP path where the capability rides an opaque token, never the wire — **the capability lives in the adapter, never in the file**: the adapter mints the per-session capability at `register` (via the `TrustedRegistrar`) and verifies each `heartbeat` against it. A heartbeat whose external session id was never registered here has no capability and is dropped.

**Decision (ladder):** realize the contract as **`coord-core`-shaped JSONL** parsed with **stdlib `System.Text.Json`** — no new ledger, no new dependency, no fork of `coord-core` (rung 2 reuse + rung 3 stdlib). "One ledger, projected, not duplicated": AI-Forward sessions already coordinate through `coord-core`; a non-pack session writes the same-shaped log with an added `contract`/`attrs` payload.

**Split:** `CoordContractParser` (pure, fully unit-testable — the tolerant read + version pin + ordering) + `InjectedContractIngest` (thin adapter — mints/holds the capability, feeds `IngestHost.Register`/`Heartbeat`).

## 2. Data model

No new persisted shape. The adapter is stateless w.r.t. the fact store; it holds an in-memory **external-session-id → `RegisteredSession`** map (which carries the internal `SessionId` + `SessionCapability`). It reuses the **same `OtelAttributes` keys** as the OTLP registration path, so `OtelSpanMapper.MapRegistration` serves both transports with no new mapping seam.

**Grain of the injected-contract event:** one row is exactly one contract event (`register` | `heartbeat` | `session-end`) emitted by one external session at one `at`, ordered within a session by `seq` (mirrors `coord-core` fold: sort `(at, session, seq)`, dedup `(session, seq)`).

**Change-surface:** contract JSONL line → `CoordContractParser.Parse` → `InjectedContractIngest.Apply` → `IngestHost.Register`/`Heartbeat` → (existing registrar + fact store + liveness projection). No new store column; liveness and session identity are the existing projections.

## 3. Contracts

### 3.1 The injected-contract schema (versioned - `loomkeeper/1`)

```json
{"kind":"register","contract":"loomkeeper/1","session":"<external-id>","at":<epoch>,"seq":<n>,
 "attrs":{"repo.canonical_path":"...","repo.display_name":"...","worktree.branch":"...",
          "worktree.path":"...","terminal.id":"...","agent.name":"...",
          "service.name":"<harness>","gen_ai.request.model":"<model>"}}
{"kind":"heartbeat","contract":"loomkeeper/1","session":"<external-id>","at":<epoch>,"seq":<n>}
{"kind":"session-end","contract":"loomkeeper/1","session":"<external-id>","at":<epoch>,"seq":<n>}
```

- `contract` is **pinned** (`CoordContract.Version = "loomkeeper/1"`). A record with a different or missing version is **rejected and counted** (A6 — a schema change is a contract change, not a silent re-parse).
- `attrs` reuses the OTLP attribute keys; `service.name` present ⇒ trust `Verified`, absent ⇒ `Asserted` (ADR-0020, inherited from `MapRegistration`).
- `session` is the **external** id; the registrar mints its own internal `SessionId`. The adapter owns the external→internal map.

### 3.2 Types

- `CoordContractEvent` (abstract) → `ContractRegister(ExternalSessionId, Attributes, At, Seq)`, `ContractHeartbeat(ExternalSessionId, At, Seq)`, `ContractSessionEnd(ExternalSessionId, At, Seq)`.
- `CoordContractParser.Parse(string jsonl) → IReadOnlyList<CoordContractEvent>` — pure; tolerant read; version pin; sorted `(at, externalId, seq)`. Exposes `Parse(string, out CoordContractParseStats)` so malformed/version-rejected counts are observable (IO1).
- `InjectedContractIngest(IngestHost host)` — `Apply(CoordContractEvent)`; `ApplyAll(IEnumerable<...>)`; `Stats` → `CoordContractStats(Registered, Heartbeats, Unknown, DuplicateRegister, Quarantined)`.
- `CoordContractStats` / `CoordContractParseStats` — counter snapshots (the operator questions: how many registered, heartbeated, dropped-unknown, duplicate, quarantined, malformed, version-rejected).

## 4. Failure-mode analysis (carried into implementation)

| # | Failure mode | Disposition |
|---|---|---|
| Inputs | malformed JSON line | skip + count `Malformed` (parser); the log survives one bad line |
| Inputs | blank line / LOG-A **leading** newline | tolerated (trim); never a record |
| Inputs | CRLF terminator | tolerated (trim) |
| Inputs | wrong/missing `contract` version | reject + count `VersionRejected` (A6) |
| Inputs | `register` missing a required identity attr | `MapRegistration` throws `LK-0004`; adapter **quarantines + counts**, loop survives (mirrors host US-11) |
| State | duplicate `register` for an external id already mapped | ignore the second + count `DuplicateRegister` (idempotent; the first capability stands) |
| State | `heartbeat` for an **unregistered** external id | drop + count `Unknown` (no capability minted here → cannot verify) |
| State | out-of-order `at`/`seq` across lines | sort `(at, externalId, seq)` before apply (deterministic replay) |
| Concurrency | interleaved writers | `coord-core` atomic `O_APPEND` guarantees whole lines (S4); parse is over a whole read |
| Time | `at` from the log | advisory only; liveness is stamped by the watcher's `TimeProvider` at ingest, never trusted from the record (clock-skew prevention, as OTLP) |

## 5. Security (STRIDE-lite, carried into implementation)

- **Spoofing / Tampering:** the file is forgeable, so the capability is **never** read from it — the adapter mints it at `register` and verifies every `heartbeat` against the held capability. A forged `heartbeat` line for a session the adapter never registered is dropped (`Unknown`); a forged `register` can only assert `Asserted` trust unless it also names a real harness, and asserted identity **cannot satisfy a correctness floor** (ADR-0020). Defence in depth: the registrar's own `LK-0001` forgery check still guards heartbeat.
- **DoS:** parse is bounded by the read; one malformed/oversize line is skipped, not fatal. (Whole-file size bounding is the reader's concern when wired to a real file watcher — noted residual, the in-process `Apply` path is not a network surface.)
- **Repudiation:** every disposition increments a visible counter (US-11 fail honestly).

## 6. Instrumentation (IO1)

The operator questions answerable without a debugger: how many external sessions **registered**, how many **heartbeats** landed, how many were dropped as **unknown**, **duplicate**, or **quarantined** (bad identity), and at the parse layer how many lines were **malformed** or **version-rejected**. Each is a single counter on the normal path; nothing degrades to a plausible wrong number (a drop is a counted drop).

## 7. Testing plan (Testing Strategy triggers)

- **D1** (units): parser — valid register (Verified via service.name), register without service.name (Asserted), heartbeat, session-end, malformed→skip+count, blank/LOG-A-leading-newline→tolerated, CRLF→tolerated, wrong-version→reject+count, out-of-order→sorted. Adapter — register maps+registers, duplicate register→ignored+counted, heartbeat-registered→liveness updated, heartbeat-unknown→dropped+counted, register-missing-attr→quarantined+counted, end-to-end through the **real registrar + store + LivenessProjection** (register→heartbeat→Alive; then no heartbeat→Stale).
- **D6** (golden payload): fixtures reproduce the exact `coord-core` writer byte shapes proven in S4 (sorted keys, leading newline, CRLF) — CI carries no Python dependency.
- **A6** (contract/version): a test pins `CoordContract.Version`; a wrong-version record is rejected, so a future bump is a deliberate, gated change.
- **Mutation:** one load-bearing oracle (version pin, or the unknown-heartbeat drop) red-then-revert; counters are single-writer (compile-enforced under warnings-as-errors, as in slice 1).

## 8. Ladder / simplicity

Reuse the registrar, the host, the mapper, and `coord-core`'s append log — **no new store, no new dependency, no second ledger**. The only new code is the tolerant parse and the thin external→internal capability-holding adapter. `session-end` handling is minimal (remove the mapping); full episode lifecycle is slice 4.

## 9. Residual (out of slice 2)

- The **session-side writer** — **now implemented** (slice-2 residual): `CoordContractWriter` writes `register`/`heartbeat`/`session-end` records to `<dir>/<session>.jsonl` with the same atomic-append + LOG-A discipline as the coord-core writer, and `CoordContractLog.ReadDirectory` + `CoordContractLogPump.PumpOnce` read a log directory and feed `InjectedContractIngest.ApplyAll` (idempotent re-read). Proven by `CoordinationContractLogTests` (7 D4 real-filesystem tests; LOG-A anti-fusion mutation-verified).
- A **live `FileSystemWatcher` tail** that calls `PumpOnce` on change is a thin wrapper over the tested pump, deferred to avoid a DC-061-style flaky FS-watcher surface; the pull-based pump is the tested core.
- **Board / goal-done** kinds over the same log — slices 6 / 4.
