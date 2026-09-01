---
id: proof-watcher-coordination-contract
title: "Proof Pack - Loomkeeper Injected Coordination Contract (slice 2)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [loomkeeper, watcher, proof-pack, coordination, injected-contract, coord-core, phase-1]
links:
  - { to: design-watcher-coordination-contract, rel: tested-by }
  - { to: design-watcher-ingest-host, rel: depends-on }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Evidence that the Loomkeeper injected coordination contract meets its design: a non-AI-Forward session
  registers and heartbeats over the coord-core append log and appears identically in the fact store (one
  ledger, projected); the parser tolerantly reads the real writer shape (LOG-A leading newline, CRLF,
  blank/malformed skip, version pin, sort by at/seq); and the capability lives in the adapter, so a
  heartbeat for a session never registered here is dropped - proven by 16 tests incl. an end-to-end
  parse->adapter->real-registrar->liveness composition, with the version-pin oracle mutation-verified.
---

# Proof Pack: Loomkeeper Injected Coordination Contract (slice 2)

- **Component:** `src/AiDe.Core/Watcher/CoordinationContract.cs` (`CoordContract`, `CoordContractEvent`+3, `CoordContractParser`, `InjectedContractIngest`)
- **Tests:** `tests/AiDe.Core.Tests/Watcher/CoordinationContractTests.cs` — 16 tests, **Passed 16 / 16**; full `AiDe.Core.Tests` suite **770/0** (on retry — see residual DC-062); build clean (0 warnings, `TreatWarningsAsErrors`).
- **Spike:** `spikes/watcher-coord-contract/` (PASS) — established the real `coord-core` writer byte shape (sorted-key JSONL, open schema, `seq` auto-assign, LOG-A leading-newline guard, atomic `O_APPEND`) and the C# tolerant read that consumes it.

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| Register + heartbeat parse to ordered events, attrs merged | `Parse_RegisterAndHeartbeat_ReturnsBothSortedByAt` | `CoordContractParser.Parse` | 2 events, register attrs carry `service.name` | Seen green | Verified | Only string attr values read |
| A malformed line is skipped and counted; others survive | `Parse_MalformedLine_IsSkippedAndCounted_OthersSurvive` | parser try/catch `JsonException` | `{ not json` skipped, heartbeat still parsed, `Malformed=1` | Seen green | Verified | — |
| The LOG-A leading newline, a blank line, and CRLF are tolerated | `Parse_BlankAndLogALeadingNewlineAndCrlf_AreTolerated` | `line.Trim()` + skip-empty | 2 events, `Malformed=0` | Seen green | Verified | — |
| A wrong `contract` version is rejected and counted (A6) | `Parse_WrongContractVersion_IsRejectedAndCounted` | version guard | `loomkeeper/2` rejected, `loomkeeper/1` kept, `VersionRejected=1` | **Yes** — inverting the version guard reds 7 tests (behavioral) | Verified | — |
| Out-of-order lines are sorted `(at, session, seq)` | `Parse_OutOfOrderLines_AreSortedByAtThenSeq` | stable sort | at 1000 < 1005 < 1030 | Seen green | Verified | — |
| A valid line of an unhandled kind is silently skipped, not malformed | `Parse_UnknownKindWithValidVersion_IsSilentlySkipped_NotMalformed` | `ToEvent` `_ => null` | board `question` skipped, `Malformed=0` | Seen green | Verified | Shared-log kinds (board) land in later slices |
| The reader consumes the real coord-core writer bytes | `Parse_RealCoordCoreByteShape_GoldenFixture` (**D6**) | S4 golden fixture (sorted keys + LOG-A) | register+heartbeat parse, attrs recovered | Seen green | Verified | — |
| The contract version is pinned (A6) | `ContractVersion_IsPinned` | `CoordContract.Version` | `== "loomkeeper/1"` | Seen green | Verified | A bump is a deliberate, gated change |
| A register mints a session with Verified trust when it names its harness | `Apply_Register_MintsSessionInTheStore_WithVerifiedTrust` | adapter → `IngestHost.Register` → `MapRegistration` | store has session, `Trust=Verified` | Seen green | Verified | — |
| A register without a harness name is Asserted trust (ADR-0020) | `Apply_RegisterWithoutHarnessName_IsAssertedTrust` | `MapRegistration` trust rule | `Trust=Asserted` | Seen green | Verified | Asserted cannot satisfy a correctness floor |
| A duplicate register is ignored and counted (idempotent) | `Apply_DuplicateRegister_IsIgnoredAndCounted` | external-id map guard | `Registered=1`, `DuplicateRegister=1` | Seen green | Verified | First capability stands |
| A register with incomplete identity is quarantined; the stream survives | `Apply_RegisterMissingRequiredIdentity_IsQuarantined_AndTheStreamSurvives` | catch `LK-0004` | `Quarantined=1`, a later good register still lands | Seen green | Verified | Mirrors host US-11 |
| A heartbeat refreshes liveness back to Alive | `Apply_HeartbeatForRegisteredSession_RefreshesLivenessToAlive` | adapter → `IngestHost.Heartbeat` → liveness | register Alive → +31s Stale → heartbeat Alive | Seen green | Verified | — |
| A heartbeat for an unregistered session is dropped and counted | `Apply_HeartbeatForUnregisteredSession_IsDroppedAndCounted` | external-id map miss | `Unknown=1`, `Heartbeats=0` | Seen green | Verified | The file cannot present a capability (ADR-0020) |
| A session-end forgets the mapping (a later heartbeat is unknown) | `Apply_SessionEnd_ForgetsTheMapping_SoALaterHeartbeatIsUnknown` | `_byExternalId.Remove` | post-end heartbeat → `Unknown=1` | Seen green | Verified | Full episode lifecycle is slice 4 |
| End to end: JSONL → parse → adapter → real registrar/store → Alive → Stale | `EndToEnd_JsonlRegisterThenHeartbeat_SessionIsAlive_ThenGoesStale` (**E11**) | full composition + `LivenessProjection` | session stored, Alive, then Stale after 31s quiet | Seen green | Verified | — |

**Boundary set covered:** register (Verified / Asserted), heartbeat (registered / unregistered / after-end), duplicate register, incomplete-identity register, malformed line, blank + LOG-A leading newline + CRLF, wrong version, out-of-order, unhandled kind, real-writer golden bytes, end-to-end parse→liveness.

**Testing Strategy triggers applied:** **D1** (parser + adapter units), **D6** (golden fixture reproducing the real coord-core writer bytes — CI carries no Python dependency), **A6** (the `contract` version is pinned and a wrong version is rejected — a bump is a gated contract change), plus an **E11** composition test through the real registrar/store/liveness. No triggered directive dropped.

**Mutation sense:** the version-pin oracle is proven **behaviorally** — inverting `!= CoordContract.Version` to `==` reds 7 tests (the happy-path parse, the golden fixture, malformed-survives, unknown-kind) — then reverted. The adapter counters are **single-writer**, so flipping one to another counter fails the build under warnings-as-errors (compile-enforced, as in slice 1).

**Security note (STRIDE, carried from design):** the append log is a local, forgeable surface (ADR-0007), so the capability is **never** read from it — the adapter mints it at `register` and verifies every `heartbeat` against the held capability; a forged heartbeat for a session never registered here is dropped (`Unknown`). A forged register can only assert `Asserted` trust unless it names a real harness, and asserted identity cannot satisfy a correctness floor (ADR-0020). Defence in depth: the registrar's own `LK-0001` forgery check still guards heartbeat.

**Residual:**
- **DC-062 (registered):** `ShellBootstrapTests.ASecondShell_ReusesTheRunningDaemon...` flaked once in the full run and passed in isolation and on retry — a pre-existing real-process daemon-teardown timing flake, **not introduced by slice 2** (this slice's code is pure in-process logic and adds no daemon test). Registered as `uncontrolled` with the readiness-barrier control to build.
- **Session-side writer** (injecting a `.loomkeeper/contract` helper into a non-pack repo that emits these records) is external — slice 2 ships the **ingest** half; the writer is versioned by `contract`.
- A **file watcher** tailing `log/*.jsonl` and calling `ApplyAll` is a wiring concern (deferred); the adapter is pure and file-agnostic, so tests drive `Apply` directly.
