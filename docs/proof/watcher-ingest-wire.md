---
id: proof-watcher-ingest-wire
title: "Proof Pack - Loomkeeper Ingest Wire (OtelSpanMapper)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [loomkeeper, watcher, proof-pack, ingest, otlp, phase-1]
links:
  - { to: design-watcher-ingest-wire, rel: tested-by }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Evidence that the Loomkeeper ingest wire's deterministic core (OtelSpanMapper) meets the contract
  spike S1 established: OTel span and registration events map to ObservedSpan/SessionBinding, unknown
  harness/model degrade to Not Recorded, malformed events raise LK-0004, and the Development-status
  GenAI schema is pinned behind a mutation-verified regression gate.
---

# Proof Pack: Loomkeeper Ingest Wire (OtelSpanMapper)

- **Component:** `src/AiDe.Core/Watcher/OtelSpanMapper.cs` (the wire's pure, deterministic core)
- **Contract source:** spike **S1** — `spikes/watcher-otlp-ingest/` (`dotnet run` **PASS**), establishing the OTel-span and registration mappings against the real `Activity` primitive.
- **Tests:** `tests/AiDe.Core.Tests/Watcher/OtelSpanMapperTests.cs` — 11 tests, **Passed 11 / 11**; full `AiDe.Core.Tests` suite **732/0**; build clean (0 warnings under `TreatWarningsAsErrors`).

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| An OTel span maps to an ObservedSpan | `MapSpan_ValidSpan_MapsAllFields` | `OtelSpanMapper.MapSpan` | Wrong field extraction fails the field asserts | Seen green; verified against `Activity` in spike S1 | Verified | — |
| A span with no session.id is rejected | `MapSpan_NoSessionId_ThrowsMalformed` (LK-0004) | `OtelSpanMapper.MapSpan` | Must throw; would fail if it defaulted | Seen green | Verified | — |
| A registration maps to a full SessionBinding | `MapRegistration_Full_MapsHarnessModelAndVerifiedTrust` | `OtelSpanMapper.MapRegistration` | Harness=service.name, Model=gen_ai.request.model, Verified trust | Seen green | Verified | — |
| Unknown harness/model degrade to Not Recorded | `MapRegistration_NoHarnessOrModel_IsNotRecordedAndAsserted` | `MapRegistration` | Null harness/model + Asserted trust (US-13) | Seen green | Verified | — |
| A missing/blank required identity attribute is rejected | `MapRegistration_MissingRequiredAttribute` (Theory ×4); `_BlankRequiredAttribute` | `MapRegistration` | Each required key absent → LK-0004 | Seen green | Verified | — |
| The Development-status GenAI schema is pinned | `OtelAttributes_PinnedSchemaSnapshot_IsUnchanged` (A6) | `OtelAttributes` | A silent upstream rename must fail the gate | **Yes** — mutation (`gen_ai.request.model`→`gen_ai.model`) turned the gate red, then reverted | Verified | Pin tracks one schema snapshot; update deliberately |
| The mapping composes with the built core | `Mapper_ComposesThroughRegistrarAndIngest` | mapper + `TrustedRegistrar` + `SpanIngest` + store | Register→map→ingest must yield Accepted and persist | Seen green | Verified | — |

**Boundary set covered:** valid span, span without session.id, full registration, opaque (no harness/model), each required attribute missing, blank required attribute, schema rename, end-to-end composition.

**Mutation sense:** the novel A6 pinned-schema gate was confirmed to fail on a renamed constant, then reverted.

**Not built this slice (residual):** the OTLP transport receiver (accepting real exports), the daemon host that owns the **bounded ingest queue** (the DoS control), and the WPF Sessions-treegrid row — the remaining Phase-1 tasks. The mapper is transport-neutral, so those are additive.
