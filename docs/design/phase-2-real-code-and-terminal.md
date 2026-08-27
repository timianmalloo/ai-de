---
id: design-phase-2-real-code-and-terminal
title: "Phase 2 — real code, terminal, and process split: detailed design"
type: design
status: in-review
owner: "@timianmalloo"
phase: "2"
tags: [design, phase-2, roslyn, conpty, process-split, ipc, upgrade]
links:
  - { to: architecture, rel: implements }
  - { to: spec-ai-native-ide, rel: implements }
  - { to: adr-0005-terminal-runtime-boundary, rel: depends-on }
  - { to: adr-0009-in-process-first-daemon, rel: depends-on }
  - { to: adr-0007-agent-session-adapter, rel: depends-on }
  - { to: threat-model-ai-native-ide, rel: depends-on }
  - { to: design-phase-1-walking-skeleton, rel: refines }
review-by: 2027-02-26
review-suggested: []
summary: >-
  The blueprint for Phase 2's three interlocking components — a real Roslyn extractor, a ConPTY
  terminal runtime, and the in-process-to-daemon split with its IPC auth and upgrade path. Surfaces
  two contract gaps in seams Phase 1 declared substitutable, and gates implementation behind four
  named spikes.
---

# Design: Phase 2 — real code, terminal, and process split

- **Status:** In review — **all four gating spikes resolved 2026-08-26**; one architectural decision they raised is settled in ADR-0015
- **Spec / architecture:** US-1, US-2, US-3, US-8 · [`docs/architecture.md`](../architecture.md) ·
  ADR-0005 · ADR-0007 · ADR-0009
- **Delivery phase:** **Phase 2.** **Real:** Roslyn semantic extractor, ConPTY runtime + Job Object,
  OSC parser, terminal renderer, separate daemon process, IPC auth protocol, Shell Bootstrap
  upgrade/rollback with dual-major handshake. **Still mocked:** Bicep/DDL extractors, audit reader,
  trace import (Phases 3–5).
- **Author(s) / date:** @timianmalloo · 2026-08-26

## Responsibility

Replace three Phase-1 substitutes with their real implementations, without changing what the layers
above them believe. Phase 1 deliberately shipped a fixture extractor, a fixture terminal session and
an in-process core precisely so this phase would be a **substitution**; whether that held is the
question this design has to answer honestly, and in two places it did not.

**Not** responsible for: Bicep/DDL extraction, the audit reader, runtime traces, or the graph canvas.

---

## Two contract gaps found while grounding

ADR-0009 promised the Phase-1→Phase-2 move would be "a deployment substitution, not a redesign".
Grounding against the real code shows two seams that were under-specified because the Phase-1 fake
never exercised them. **Both are recorded here rather than discovered during implementation.**

### Gap 1 — `ITerminalSession` has no output path

```csharp
public interface ITerminalSession
{
    string SessionId { get; }
    long Generation { get; }
    SessionProcessingClass ProcessingClass { get; }
    Task<PtyWriteResult> WriteAsync(long expectedGeneration, ReadOnlyMemory<byte> bytes, CancellationToken ct);
}
```

The seam is **write-only**. The Phase-1 fixture recorded bytes and returned; nothing ever needed to
read. A real terminal is a bidirectional stream whose *output* is the entire point — the renderer
subscribes to it, the OSC parser reads it, and the resource budget (4 MiB ring, 1 MiB/s truncation
trigger) is defined over it.

**This is a contract extension, not a redesign** — `WriteAsync` and the generation fence are
unchanged, so the write-ahead dispatch built on them is untouched. But it means the Phase-1
conformance claim ("substituting the real runtime is a swap") was true only of the half that was
specified.

**Design decision:** output is a *pull-based bounded stream*, not an event. An event would let a
fast-producing process drive unbounded work in whatever thread raised it, which is exactly the
"1 MiB/s sustained output" case the architecture budgets for. A bounded channel makes backpressure
representable and truncation a state rather than a crash.

```csharp
public interface ITerminalSession
{
    // …unchanged members…

    /// <summary>Bounded, ephemeral output. Terminal text never enters the fact store (spec privacy).</summary>
    ChannelReader<TerminalChunk> Output { get; }

    /// <summary>Advisory prompt/exit state parsed from OSC. Never agent acceptance (ADR-0007).</summary>
    SessionActivity Activity { get; }

    Task<SessionExit> WaitForExitAsync(CancellationToken ct);
    ValueTask ResizeAsync(int columns, int rows, CancellationToken ct);
}

public readonly record struct TerminalChunk(ReadOnlyMemory<byte> Bytes, bool Truncated);
public enum SessionActivity { Starting, Ready, Busy, Disconnected, Ended, OutputOverload }
public sealed record SessionExit(int? ExitCode, bool Killed, DateTimeOffset At);
```

### Gap 2 — `IExtractor` materialises the whole scope in memory

`ExtractAsync` returns `ExtractionResult` holding every assertion at once. That is fine for the
fixture (P1-PERF: 10,000 assertions committed in ~170 ms) and unproven for a real solution, where
symbol counts are an order of magnitude larger and Roslyn's own compilation load dominates the 60 s
per-scope budget.

**Design decision: keep the contract, change what a scope is.** Streaming the extractor would leak
partial results into a store whose whole invariant is "only a *complete* snapshot contributes
evidence", so the boundary stays all-or-nothing. Instead **a scope becomes one project, not one
solution** — a natural unit that is independently completable, independently stale-able, and sized
for the existing budget. A ten-project solution is ten scopes that settle independently, which is
also better behaviour: one unparseable project marks itself stale without blanking the other nine.

`simplify: one scope per project; ceiling ~50k assertions per project; upgrade trigger = P2-PERF p95
scope settlement > 10 s on the approved corpus.`

---

## Data model

**Phase 2 adds no new fact table.** Stating that explicitly because the temptation is real: sessions
have a lifecycle, and lifecycles look like facts.

| Candidate | Decision |
|---|---|
| Terminal session lifecycle (start/ready/exit) | **Not a fact.** Session state is ephemeral operational state, not evidence about a repository. The spec makes terminal output ephemeral and forbids it entering the graph; session *state* is the same category. It lives in memory and in `session_dim`'s Type-2 `generation`/`processing_class`, which already exist. |
| Terminal output | **Never persisted.** Spec privacy: "Display only in the live terminal… never automatically indexed, attached to prompts, or copied into audit/telemetry." |
| Roslyn symbols | **Existing `evidence_assertion_fact` grain, unchanged.** A C# symbol relation is exactly "one assertion about one normalized (subject, predicate, object) relation at one artifact revision". No new grain is needed, which is the point of having declared it carefully. |
| Process/daemon identity | **Not a fact.** `core_epoch` already exists in `core_state`. |

**What does change** is the *content* of existing dimensions:

| Dimension | Change | History rule |
|---|---|---|
| `node_dim.node_id` | Now a **Roslyn documentation-comment ID** (`T:Namespace.Type`, `M:…`) for C# symbols, per the architecture's extractor rule 4. | Unchanged — Type-2 on `node_kind`. |
| `node_dim.node_kind` | Gains `csharp.type`, `csharp.member`, `csharp.project`. | Type-2: a node changing kind changes what past assertions meant. |
| `session_dim` | Gains a real OS process identity alongside the existing generation. | `generation` stays Type-2; the process id is **Type-1** — it identifies a process that no longer exists once the generation advances, so retaining its history would preserve a fact about nothing. |

**Migration:** none. No schema change, so no expand-migrate-contract. Phase-1 fixture nodes and
Phase-2 Roslyn nodes coexist because they are different scopes with different `node_kind` values —
the store never had to know which extractor produced a node.

### Change surfaces (E7)

`store` (no change) → `domain model` (`ITerminalSession` extension, `SessionActivity`) → `service`
(`TerminalRuntime`, `RoslynExtractor`, `IpcServer`/`IpcClient`) → `projection/wire` (IPC envelope
serialization — **new wire format**) → `client type` (`ITerminalSession` proxy over IPC) → `UI`
(terminal surface, real session states) → `compute reader` (health view gains daemon liveness).

**The new surface is the IPC wire.** Everything else is a substitution behind an existing contract.

---

## Component 1 — Roslyn semantic extractor

### Contract

```csharp
public sealed class RoslynExtractor(string extractorVersion) : IExtractor
{
    public string ScopeKind => "csharp";
    public Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken ct);
}
```

Same `IExtractor` as Phase 1 — the substitution the design promised, with one scope per project.

### Patterns

| Pattern | Why |
|---|---|
| **Adapter** (Roslyn `Workspace` → `EvidenceAssertion`) | The store's grain is already right; this only translates. |
| **Snapshot Replacement** (unchanged) | Inherited from Phase 1; nothing about real symbols changes it. |
| **Circuit Breaker per scope** (unchanged) | A project that fails to compile quarantines itself; the other projects keep their evidence. |

Ladder: `Microsoft.CodeAnalysis.CSharp.Workspaces` is rung 5 (a dependency), justified because
re-implementing C# semantic analysis is not a candidate. **`MSBuildWorkspace` specifically** is the
part needing a spike — it shells out to MSBuild and its failure modes are environmental.

### Confidence rules (spec US-1/US-2 are explicit here)

| Relation | Status | Why |
|---|---|---|
| `TypeA depends_on TypeB` from a resolved symbol reference | `Verified` | The compiler resolved it. |
| `Controller handles Route` from an attribute | `Verified` | Declared in the artifact. |
| DI registration `AddScoped<IFoo, Foo>()` | **`Inferred`** | Static approximation; the runtime container may differ. Architecture rule 4 names this explicitly. |
| ORM entity → table from convention | **`Inferred`** | Convention-derived, not declared. |
| Anything from a **source generator** | **Absent — not labelled at all** (S2, 2026-08-26) | The security control strips analyzer references before compilation, so generated symbols never enter the model. Measured: 3 source-declared types instead of 4, zero generated documents. The honest consequence is **silence, not a weak label**. **Decided 2026-08-26: disclose the absence** — the scope records that analyzer references were stripped, and every projection over it carries a `generated-code-not-analysed` omission state. |

---

## Component 2 — ConPTY terminal runtime

### Contract and lifecycle

```csharp
public sealed class ConPtyTerminalRuntime : IAsyncDisposable
{
    Task<ITerminalSession> StartAsync(TerminalRequest request, CancellationToken ct);
    IReadOnlyList<ITerminalSession> Sessions { get; }
}

public sealed record TerminalRequest(
    string SessionId, string Executable, string WorkingDirectory,
    int Columns, int Rows, SessionProcessingClass DeclaredProcessingClass);
```

### Patterns

| Pattern | Where | Rejected alternative |
|---|---|---|
| **Process Supervisor + Windows Job Object** | Runtime owns child lifetime | Rejected: relying on process-tree kill. A crashed daemon must not leave agent CLIs running headless against dead pipes — the architecture names this as the failure the Job Object prevents. |
| **Separate reader/writer service loops** | I/O | Rejected: one loop. ConPTY deadlocks when a full output buffer blocks a write; the spike (`conpty-foundation`) records I/O separation as a documented requirement. |
| **Bounded channel + drop-oldest** | Output | Rejected: unbounded buffering. A 1 MiB/s producer would otherwise consume memory until the process dies. Truncation becomes `TerminalChunk.Truncated` and `SessionActivity.OutputOverload` — a *state*, not a crash. |

### OSC parsing — advisory only

OSC 133 (prompt markers) drives `SessionActivity.Ready`/`Busy`. Per ADR-0007 this is **never** agent
acceptance, and per the threat model the parser must:
- accept only an allowlisted display subset,
- **disable OSC 52 (clipboard) and OSC 8 (hyperlink) host actions entirely**,
- require a session nonce before honouring any state claim,
- treat every sequence as forgeable, because the child process is untrusted.

**BUILT 2026-08-27.** `OscParser` + `TerminalActivityState`, wired into the ConPTY read loop.

**The unknown this closed.** ConPTY is not a pipe — it is a terminal emulator that parses the
child's output and re-emits its own VT stream, so whether an OSC sequence *survives the round trip
at all* was never established. Had it not, every unit test would still have passed and shell
integration would have been silently inert in production. **Measured 2026-08-27 through a real
pseudo console:** an authenticated `OSC 133;D` written by a PowerShell child drove the session to
`Ready`, and an unauthenticated one left it at `Busy`. OSC passes through.

**Nonce mechanism.** The session generates a per-session 128-bit nonce
(`ConPtyTerminalSession.ShellIntegrationNonce`) and honours a state claim only when the sequence
carries it as a `nonce=` parameter — `OSC 133;D;0;nonce=<hex> ST`. Comparison is length-checked and
fixed-time. Generated by the session rather than accepted from the caller: a caller-chosen nonce is
one that can be reused across sessions, and a shared value authenticates the wrong child's claims.

**Which signal wins.** The pre-existing heuristic (*bytes arrived, so the process is working*) and
OSC conflict directly — a shell that finishes a command prints its prompt, which is output, which
the heuristic reads as `Busy`. So the **first authenticated claim makes OSC authoritative** for the
session and the heuristic retires; it exists for sessions with no shell integration. Overload
outranks both (only we know we are dropping bytes) and `Ended` outranks everything.

**`Ready` was previously unreachable.** It was a declared state nothing produced — the runtime only
ever moved `Starting → Busy → Ended` — so any consumer switching on it was switching on a value it
would never see. The parser is what makes it real.

**Not sanitised, absent.** OSC 52 and OSC 8 are refused regardless of the nonce, because sanitising
presumes we can separate a safe payload from a hostile one when the child chose all of it. There is
no clipboard or hyperlink code path to reach. OSC 633 (VS Code's parallel protocol, named in the
threat model beside 133) is not spoken and is never honoured.

Seven controls were mutation-tested one at a time; all seven were caught.

---

## Component 3 — The process split

This is the phase's real risk. It creates the **first cross-process trust boundary in the product.**

### What moves

| | Phase 1 | Phase 2 |
|---|---|---|
| Core | In-process module | Separate `AiDe.Daemon.exe`, one per workspace |
| `CallerPrincipal` | Shell identity directly | Derived from the authenticated pipe connection |
| Command transport | Method call | Named pipe, versioned envelope |
| Lifetime | Shell's | Owned by Shell Bootstrap; terminals in its Job Object |

### IPC contract

Named pipe restricted to the workspace-owner SID. On `OpenWorkspace`, the daemon issues a random
in-memory capability bound to `{connection, shell process, workspace, epoch}`, validated and revoked
per command. Envelope, `commandId` idempotency and the stable `CallerPrincipal` are **unchanged from
the architecture's command protocol** — that specification was written for this boundary and Phase 1
simply had a shorter path to it.

**Dual-major handshake:** the daemon publishes supported current and previous major IPC versions;
an unsupported version is rejected with a stable code, never negotiated down silently.

### Upgrade and rollback (Shell Bootstrap)

Side-by-side versioned daemon directories. On upgrade: preflight → forward migration → **health
gate** → repoint. On gate failure: restore the prior binary and the pre-migration snapshot, verify
the previous projection, then declare rollback success.

**The health gate is the fast subset only** — schema/version preflight, forward migration, store
integrity sample, IPC handshake, bounded projection comparison. Full restore/replay equality is
*asynchronous verification*, because P1-PERF measured a 50k-edge replay against a 15-minute RTO and
the gate has a 60-second budget. Putting the slow check inside the fast gate is the contradiction the
council review caught in the v1 architecture; it must not return here.

---

## Failure-mode analysis

| Failure mode | From which choice | Disposition | How addressed | Test |
|---|---|---|---|---|
| Daemon crashes, agent CLIs keep running headless | Process split | **prevent** | Terminals in a Job Object with kill-on-close; Bootstrap detects and raises `aide.core.restart` | `P2-TERM-05` |
| ConPTY deadlock on a full buffer | Single I/O loop | **prevent** | Separate reader/writer loops (spike-documented requirement) | `P2-TERM-02` |
| A process floods output and exhausts memory | Bounded channel choice | **mitigate + detect** | Drop-oldest with `Truncated`; `OutputOverload` state; `pty.dropped_bytes` metric | `P2-TERM-03` |
| Terminal text reaches the graph, audit or telemetry | New high-volume untrusted data | **prevent** | Output never crosses into the store; seeded-marker negative test asserts absence everywhere | `P2-PRIV-01` |
| Roslyn fails to load a project (missing SDK, broken build) | MSBuildWorkspace | **detect + mitigate** | Scope marked stale/failed with a diagnostic; other projects unaffected; last good snapshot renders | `P2-EXT-02` |
| Roslyn extraction exceeds the 60 s scope budget | Real solution size | **mitigate** | One scope per project; cooperative cancellation; per-scope quarantine after K timeouts | `P2-EXT-04` |
| Generated symbols **absent** from the model, so a query answers confidently about an incomplete picture | The security control that strips analyzer references | **detect + disclose** | S2 settled the mechanism; what remains is product-facing. The extractor **records that a scope had analyzer references stripped**, and any projection over such a scope carries a "generated code not analysed" omission state — the same shape as the existing bounded-result omission, never silent | `P2-EXT-06` |
| Shell and daemon versions mismatch after a rollback | Dual-major handshake | **prevent** | Handshake rejects unsupported versions with a stable code; the post-rollback pairing is an explicit test cell | `P2-UPGRADE-02` |
| Power loss mid-migration | Upgrade choreography | **recover** | Durable migration journal; incomplete migration on next start triggers automatic snapshot restore | `P2-UPGRADE-03` |
| Daemon orphaned when the shell dies | Split lifetime | **detect + recover** | Daemon exits when its owning connection closes and no other client attaches within a grace period | `P2-IPC-05` |
| Two shells open the same workspace | Split lifetime | **prevent** | The existing OS-level workspace ownership lock; the second gets a stable "already open" code | `P2-IPC-06` |
| A pipe client floods commands | New boundary | **mitigate** | Bounded control lane already specified; per-connection admission | `P2-IPC-07` |

## Adversarial analysis (STRIDE-lite)

**Phase 2 restores the boundary Phase 1 did not have.** ADR-0009 explicitly deferred these controls;
this is where the deferral is paid back.

| Boundary | Threat | Disposition | Control | Negative test |
|---|---|---|---|---|
| Shell → daemon pipe | **S**: another process impersonates the shell | mitigate | Pipe ACL limited to the workspace-owner SID; capability bound to `{connection, process, workspace, epoch}` | `P2-SEC-01` wrong SID → denied |
| Shell → daemon pipe | **S**: caller claims another workspace in the payload | mitigate | `CallerPrincipal` server-derived from the connection, never read from the payload | `P2-SEC-02` |
| Shell → daemon pipe | **R**: a command executes with no attributable record | mitigate | Command receipts keyed by stable principal (existing) | `P2-SEC-03` |
| Shell → daemon pipe | **E**: a revoked capability is replayed | mitigate | Capability validated *and revoked* per command; stale epoch rejected | `P2-SEC-04` replay → denied |
| Shell → daemon pipe | **D**: command flood | mitigate | Bounded control lane, per-connection admission | `P2-IPC-07` |
| Terminal process → runtime | **T**: forged OSC claims readiness | mitigate | Session nonce required; OSC advisory only; never agent acceptance | `P2-TERM-06` forged OSC 133 |
| Terminal process → host | **E**: OSC 52 clipboard write / OSC 8 hyperlink action | mitigate | Both disabled outright, not sanitised | `P2-TERM-07` |
| Terminal process → UI | **D**: ANSI flood | mitigate | Bounded ring + rate trigger + overload state | `P2-TERM-03` |
| Repository → Roslyn | **E**: analyzer or source generator executes during compilation | **mitigate** | `solution.WithProjectAnalyzerReferences(id, [])` before any compilation is requested — **not** MSBuild properties, which S2 measured as ineffective | `P2-SEC-08` hostile generator does not run |
| Daemon → disk | **E**: same-user process reads the store | **accept** | Unchanged desktop residual (threat model boundary 1). Residual risk: full workspace compromise; out of scope for a single-user local tool. | documented, not tested |

**The Roslyn row is the one that is new and easy to miss** — and **Spike S2 measured it on
2026-08-26, changing the mitigation** ([result](../../spikes/roslyn-msbuild-workspace/RESULT.md)).

The threat is confirmed real: the fixture generator executed **inside the extractor's own process**
(`Initialize`, `PostInitialization` and `SourceOutput` all fired), triggered by `GetCompilationAsync()`
— the ordinary call an extractor makes to get symbols, with no "just read the project" path that
avoids it. `OpenProjectAsync` alone is silent, so the trigger is compilation rather than load.

**The mitigation this design originally named does not work.** `RunAnalyzers=false`,
`RunAnalyzersDuringBuild=false` and `EnforceCodeStyleInBuild=false` left the analyzer references at
nine and the generator ran regardless. They govern the build, not what `MSBuildWorkspace` puts in
the project model — and they are in any case **the repository's own build configuration**, so a
control resting on them is one a hostile repository can influence.

The control that holds is **stripping `AnalyzerReferences` from the loaded solution** before
requesting a compilation: applied after load, in our process, depending on nothing in the repository
cooperating. Measured cost is exactly the generated symbols — 3 source-declared types instead of 4,
zero generated documents, and no compilation errors either way.

Two things worth carrying: **eight analyzer references arrive from the SDK itself** on any ordinary
.NET project, so the hostile case is the normal case with different intent; and **`RS1035`** ("do not
do file IO in analyzers") is a compile-time lint the *generator's own author* opts into, so it
constrains the well-behaved and nobody else. Neither is a control.

## Privacy analysis (LINDDUN-lite)

| Flow | Finding | Disposition | Control | Retention |
|---|---|---|---|---|
| Terminal output → renderer | **D**: the highest-volume personal/work data in the product — credentials, tokens, customer data all pass through a terminal | **mitigate** | Ephemeral by construction: bounded in-memory ring, never written to the store, logs, metrics or traces. Cleared on close. | Process lifetime |
| Terminal output → OSC parser | **D**: the parser reads every byte | mitigate | Parser extracts state only; it retains no text and emits no text into telemetry | None |
| C# symbol names → assertions | **I**: identifiers may embed names or ticket ids | mitigate | Existing allowlist: normalized relation metadata and source references only, never raw bodies | Rebuildable |
| Daemon logs | **D**: a new process with its own log stream | mitigate | Same prohibited-attribute list as the core; `P2-PRIV-02` seeds a secret and asserts absence across daemon logs, metrics and traces | Local, finite |
| Session process command line | **I**: may contain paths or arguments | **mitigate** | Stored as `session_dim` display metadata only; excluded from telemetry by the existing allowlist | Workspace lifetime |

## Telemetry

New spans: `aide.terminal.session` (extended), `aide.ipc.command`, `aide.daemon.lifecycle`,
`aide.upgrade.gate`. New metrics: `pty.output_bytes`, `pty.dropped_bytes`, `pty.active_sessions`,
`ipc.connections`, `ipc.rejected` by code, `daemon.restarts`, `upgrade.gate_duration`,
`extraction.project_duration`. Stable codes: `AIDE-IPC-VERSION-UNSUPPORTED`,
`AIDE-IPC-CAPABILITY-INVALID`, `AIDE-IPC-WORKSPACE-LOCKED`, `AIDE-TERM-OVERLOAD`,
`AIDE-TERM-START-FAILED`, `AIDE-EXTRACT-PROJECT-LOAD-FAILED`, `AIDE-UPGRADE-HEALTH-FAILED`.

**Prohibited, restated because the temptation is highest here:** no terminal text, no command lines,
no source text, no paths in any span attribute or metric label.

## Test plan

Triggered directives: **D0** + **D1** (OSC parsing, version negotiation logic) + **D2** (OSC byte
streams and IPC envelopes are wide, hostile-capable input domains) + **D3** (new projects and a new
process boundary) + **D4** (filesystem, pipes, real MSBuild) + **D5-provider** (the daemon exposes an
IPC API) + **D6** (the IPC envelope is a payload schema) + **D7** (`ITerminalSession` now has two
implementations — **the conformance suite ADR-0012 deferred is now owed**). **A-series does not
fire:** no model call in Phase 2.

**The D7 obligation is new and load-bearing.** Phase 1 had one `ITerminalSession` implementation, so
no conformance suite was owed. Phase 2 has two, and the fixture is what every dispatch test runs
against — if the two diverge, every one of those tests is proving something about a fake.

Named suites: `P2-EXT-01..06` (extraction), `P2-TERM-01..07` (terminal), `P2-IPC-01..07`,
`P2-SEC-01..08`, `P2-UPGRADE-01..03`, `P2-PRIV-01..02`, `P2-CONFORM-01..04` (the `ITerminalSession`
conformance suite run against **both** implementations), `P2-PERF-01..03`.

---

## Implementation is gated behind four spikes

Per the architecture's flagged risks. **None of this may be built before these run**, because each
determines a contract rather than an optimisation.

| Spike | Question | Why it gates | Status |
|---|---|---|---|
| **S1 — Roslyn source generators** | ~~Are generated symbols visible, and distinguishable from hand-written ones?~~ Re-scoped by S2 to: is the absence of generated symbols acceptable, and how is it disclosed? | A user asking "what implements `IFoo`" in a repository that *generates* implementations gets an answer correct about hand-written code and silent about the rest. | **DECIDED 2026-08-26 by the product owner — disclose the absence.** No spike needed: the question was a product call, and it has been made. See below. |
| **S2 — MSBuildWorkspace load** | Does a real solution load without the host SDK matching exactly, and can analyzers/generators be disabled? | The security control above depends on the answer. If they cannot be disabled, extraction executes repository code and the approach changes. | **CLEARED 2026-08-26** — [result](../../spikes/roslyn-msbuild-workspace/RESULT.md). Both yes, but **the mitigation changed**: MSBuild properties are ineffective; stripping `AnalyzerReferences` is the control. |
| **S3 — Terminal renderer** | ~~Which renderer meets the keyboard/screen-reader contract?~~ Re-weighted by [ADR-0014](../adr/0014-accessibility-posture.md) to throughput, fidelity, input, licence and integration cost. | ADR-0005 defers the choice. | **CLEARED 2026-08-26** — [result](../../spikes/terminal-renderer/RESULT.md). **Own a WPF renderer.** `GlyphRun` per line: p95 **6.64 ms** (151 fps ceiling). VT scanning at **2361×** the 1 MiB/s budget. |
| **S4 — WebView2 airspace** | Does WebView2 compose with WPF focus, DPI and the docking layout? | ADR-0008's recorded reversal trigger. | **RUN 2026-08-26 — TRIGGER MET** — [result](../../spikes/webview2-airspace/RESULT.md). Airspace is real, and the composition control is **not** the fix: it kills the process. A decision is owed. |

### S1 — decided rather than spiked: disclose the absence

The product owner decided on 2026-08-26 to **disclose absent generated code**. There is nothing left
to measure, so S1 does not run; what it produces instead is a contract:

- The extractor **records, per scope, that analyzer references were stripped** — a scope-level fact,
  not a per-symbol one, because the absence is a property of how the scope was extracted.
- Every projection over such a scope carries a **`generated-code-not-analysed` omission state**,
  reusing the existing bounded-result omission shape rather than inventing a second one. `describe`,
  `find`, `impact` and `knowledge` all already carry omission state, so this rides an established
  channel.
- The UI surfaces it wherever a result could be incomplete because of it — the same treatment a
  capped result gets. **Silence read as "nothing there" is the failure mode**, and the whole point of
  the decision is to prevent a confident, incomplete answer.
- `P2-EXT-06` becomes the test: a fixture project with a generator, extracted under the control,
  must yield the omission state, and a projection over it must carry it through to the wire.

### S3 — cleared, with a binding implementation constraint

Owning a WPF renderer is viable, and the margin is comfortable. But the spike measured a **21×
spread between draw paths**: `GlyphRun` per line at 6.64 ms p95 versus `FormattedText` per cell at
142.80 ms — 7 fps, four times over budget.

That matters because per-cell is the *natural* design. A terminal is conceptually a grid of
independently styled cells, and modelling it that way is what a competent implementer would write
first. So the draw path is recorded here as a **design decision, not an optimisation**: `GlyphRun`
per line with a cached `GlyphTypeface`. ADR-0005 is unchanged — nothing measured argues for letting
the renderer own session state.

### S4 — the trigger is met, and the obvious mitigation is worse than the problem

Airspace is real in the default control and not marginal: the WPF overlay's own region samples as web
content at a distance of 38 versus 219. Concretely, any popup, context menu, tooltip or drag adorner
over the graph canvas is invisible — **including AvalonDock's own drop-target indicators**, which is
a direct collision with US-9's drop-target preview.

`WebView2CompositionControl` fixes airspace *exactly* (distance 0). It also **terminates the process**
when AvalonDock floats its pane — an `ArgumentException` from `GraphicsItemD3DImage.UpdateSize`,
followed by an uncatchable `0xC0000005` in `Direct3D11CaptureFrame.Dispose()` — and never repaints
after a tab restore. US-9 requires floating panes, so this is not a trade that can be taken.

**A decision is owed before the graph canvas is built.** The options, with the spike deliberately not
choosing: keep the windowed control and forbid WPF chrome over the canvas; reverse ADR-0008 for the
canvas and render the graph in WPF; or accept the composition control with floating disabled for that
one pane. This is now the largest open architectural question in Phase 2.

**Focus does not cross the boundary in either hosting mode** — `Focus()` is refused and Tab traversal
never lands on the canvas. Under ADR-0014 that is no longer an accessibility veto, but it remains an
ordinary defect in a keyboard-first tool: routing focus into web content needs the
`MoveFocusRequested` / `CoreWebView2.MoveFocus` protocol, which is a design obligation rather than
something that works by construction.

**What S2 established beyond its own question.** A real solution (`AiDe.sln`, 5 projects) loaded in
1.4 s with **zero** `WorkspaceFailed` diagnostics against a deliberately *older* SDK (10.0.301 host,
10.0.303 build) — so the environmental fragility flagged below is real but narrower than feared, at
least across a patch-level mismatch. A cross-major mismatch remains unmeasured.

S2 also surfaced a **supply-chain finding that is not yet resolved**: ten high-severity advisories
are reachable from `Microsoft.CodeAnalysis.Workspaces.MSBuild` 4.14.0 — `GHSA-w3q9-fxm7-j8fq` against
the MSBuild packages and eight against `System.Security.Cryptography.Xml` 9.0.0. The spike suppresses
`NU1903` for itself on the narrow ground that its references are compile-time only and it never
ships. **The shipped extractor gets no such exemption**, and this is carried to the Security &
Identity Architect as Phase-2 input.

## Focus routing across the canvas boundary

The canvas is the one surface in the workbench that WPF's focus system cannot reach. Spike S4
measured `Focus()` refused and Tab traversal never landing on it, in **both** hosting modes — so this
is a property of hosting a browser, not of the hosting mode [ADR-0015](../adr/0015-canvas-hosting-and-overlay-strategy.md)
chose.

### The documented mechanism does not exist here

`CoreWebView2Controller.MoveFocus` is the documented way to hand focus to web content. **The WPF
`WebView2` control exposes no controller at all** — verified by enumerating its public declared
surface, which contains exactly two focus-related members, both `FocusVisualStyle`
([spike](../../spikes/webview2-snapshot-swap/RESULT.md) finding 6). Those API names *do* appear in
the assembly's string table, so this is a case where a grep would have confirmed a design that could
not be built.

What is available: `WebView2` derives from `HwndHost` and therefore owns a real window handle.
`SetFocus` on it puts focus on the browser's inner input window — **measured**, and read back with
`GetFocus()` rather than trusting `SetFocus`'s return value, which is the *previously* focused window
and whose null case is ambiguous between "failed" and "nothing had focus".

### The contract

Focus is **explicit in both directions**. Neither crossing happens by WPF traversal, because
traversal does not work here.

| Transition | Mechanism | Trigger |
|---|---|---|
| **WPF → canvas** | `SetFocus(webView.Handle)`, then read back `GetFocus()` and confirm it landed on the handle or a descendant | The `workbench.focusCanvas` command — command palette, and its bound chord. Also on a canvas click, which the browser handles itself. |
| **canvas → WPF** | The page traps `Tab` on its last focusable element and `Shift+Tab` on its first, and posts `{kind:"focus.leave", direction}` via `chrome.webview.postMessage`. The host moves WPF focus to the next/previous element. | User tabs off either end of the canvas. |
| **canvas → WPF (escape)** | The same channel, `{kind:"focus.leave", direction:"restore"}` | `Esc` in the canvas returns focus to whatever last held it before entry. |

Two properties fall out of this, and are stated because they are easy to lose later:

- **The host records the pre-entry focus target** before calling `SetFocus`, so `Esc` has somewhere
  to return to. Without it, leaving the canvas dumps the user at the start of the tab order.
- **The page's boundary handlers are the only way out.** A canvas page that forgets them is a
  keyboard trap — the user enters and cannot leave. That makes it a contract on the page rather than
  a nicety, and it is the first thing the tests below assert.

### Failure modes

| Mode | Disposition | Control |
|---|---|---|
| `SetFocus` does not land (handle not yet created, or the control is hidden behind the snapshot swap) | **detect + announce** | Read back `GetFocus()`. On failure the command reports *"The graph canvas is not ready"* rather than doing nothing — a silent refusal is indistinguishable from a broken key (**DC-011**). |
| Focus command issued while the snapshot swap is showing | **prevent** | The canvas is hidden and cannot take focus, so `workbench.focusCanvas` is disabled for the duration of a drag and refused with an announced reason. |
| The page never posts `focus.leave` | **detect** | `P2-FOCUS-03` fails. There is no runtime recovery — a keyboard trap is a defect to fix, not to work around. |
| A `focus.leave` message is produced by page content rather than the boundary handler | **mitigate** | The channel carries a fixed typed vocabulary and the canvas page is first-party; acting on `focus.leave` moves focus and grants nothing. Consistent with the repository-content-is-untrusted posture: the message has no privileged effect. |

### Tests

| ID | Asserts |
|---|---|
| `P2-FOCUS-01` | `workbench.focusCanvas` puts focus on the canvas handle or a descendant, verified via `GetFocus()` — not via the command's return value. |
| `P2-FOCUS-02` | The pre-entry focus target is recorded, and `Esc` returns focus to exactly that element. |
| `P2-FOCUS-03` | Tabbing off either end of the canvas returns focus to WPF in the correct direction. **This is the keyboard-trap test**, and the one that must never be allowed to rot. |
| `P2-FOCUS-04` | With the snapshot swap active, `workbench.focusCanvas` is refused **and announced**, never silently ignored. |

`P2-FOCUS-03` needs a real window and a real WebView2 runtime, so it belongs with the existing
WPF-hosted tests under `DisableTestParallelization` (**DC-008**), and its absence must fail the run
rather than be skipped (**DC-012**).

> **Scope note.** Under [ADR-0014](../adr/0014-accessibility-posture.md) none of this is accessibility
> work and none of it carries an accessibility veto. It is here because AI-DE is a keyboard-first
> developer tool and "focus the graph" is an ordinary product capability that does not work by
> default.

## Flagged risks

- **RESOLVED — ConPTY child attachment requires the host to own a real console.** The D7 case
  `Output_DeliversTheChildProcessesOwnOutput` failed for a reason that was never in the runtime.
  Measured 2026-08-26: identical code captures the child's stdout under `dotnet run` from a terminal
  (90 bytes, marker present, in both handle-closing orders) and captures nothing under a console-less
  host. A `dotnet test` host is always console-less because its stdio is redirected. The interop was
  correct throughout — `CreatePseudoConsole` HRESULT 0, attribute list 48 bytes, `STARTUPINFOEX` 112,
  `HPCON` passed by value (by-pointer yields no output at all).
  **Control:** the case now runs **out of process** via `tests/AiDe.Core.TerminalHost`, launched with
  `CREATE_NEW_CONSOLE`, driving the real `ConPtyTerminalSession` and reporting by exit code. Verified
  capturing 297 characters of child output. Registered as defect class **DC-014**.
- **The `ITerminalSession` extension weakens the Phase-1 conformance claim** until `P2-CONFORM` runs:
  the fixture and the real runtime agree on the half that was specified, and nothing yet proves they
  agree on output, activity or exit.
- **Cross-monitor DPI remains unverified** (needs a second display) and now matters more, because
  Phase 2 adds floating terminal panes.
- **`MSBuildWorkspace` failure modes are environmental** — SDK version, NuGet restore state, missing
  targets — and are the least predictable dependency in the phase. **Narrowed by S2:** a patch-level
  SDK mismatch loads cleanly with zero diagnostics. A cross-major mismatch, and a repository pinning
  an SDK that is not installed, remain unmeasured.
- **The MSBuildWorkspace dependency chain carries ten high-severity advisories** (S2 finding 7), with
  no clean version identified. Unresolved for the shipped extractor.
- **Repository-authored MSBuild *tasks* are an unprobed trust boundary.** S2 established that
  analyzers and generators can be prevented from executing, but `MSBuildWorkspace` still runs MSBuild
  *evaluation* to load projects, and whether that executes repository-supplied task assemblies was
  not tested.
- The scope-per-project decision is measured against nothing yet; `P2-PERF` is its first test.

## Status and next action

| | |
|---|---|
| **Completed** | Phase-2 design: two contract gaps closed on paper, data model (no new facts, and why), three component contracts, failure/STRIDE/LINDDUN analyses, telemetry, the triggered-directive test plan including the newly-owed conformance suite. **All four spikes resolved 2026-08-26.** S2 changed the analyzer-execution mitigation to stripping `AnalyzerReferences`. S3: own a WPF renderer, `GlyphRun` per line. S4 met ADR-0008's reversal trigger, **now resolved by [ADR-0015](../adr/0015-canvas-hosting-and-overlay-strategy.md)** — windowed control plus snapshot swap, gut-checked at 150% DPI. S1 decided: disclose the absence. **Focus routing designed** against a verified mechanism, after the documented one turned out not to exist on this control. |
| **Remaining** | The terminal renderer (S3's `GlyphRun`-per-line constraint), then the process split, then Roslyn. **The OSC parser landed 2026-08-27** and with it the measurement that OSC survives ConPTY. **Newly owed:** the shell-integration script that echoes `ShellIntegrationNonce` — the nonce is generated and checked, but nothing injects it yet, so a real session has no integration and falls back to the heuristic. Open findings carried forward: the MSBuildWorkspace dependency-chain advisories, the unprobed MSBuild-task trust boundary, and a possible colour flash at the snapshot swap that only a human observer can settle. |
| **Best next action (superseded 2026-08-27 — the runtime and the OSC parser are built)** | **Implement the ConPTY terminal runtime.** It is the only Phase-2 component with no open decision in front of it — S3 cleared its renderer and named the binding draw path, ADR-0005's boundary is unchanged, and it does not touch the canvas. It also forces the `ITerminalSession` output extension and the newly-owed D7 conformance suite, which every dispatch test currently written against the fixture depends on. |

## Gate record

`GATE design · 2026-08-26 · Patterns Expert ⇄ Simplifier (no new fact table justified; scope-per-project chosen over streaming with a simplify: ceiling); Test Architect (D0,D1,D2,D3,D4,D5-provider,D6,D7 enumerated; the D7 conformance suite is newly owed and named); Security & Identity (the cross-process boundary Phase 1 deferred is restored; the Roslyn analyzer-execution threat is new and mitigated); Distributed Systems (dual-major handshake, capability revocation, post-rollback pairing as an explicit cell); Privacy (terminal output is the highest-volume work data in the product and stays ephemeral by construction); SRE (health gate keeps the fast subset only — the v1 contradiction must not return) · verdict: PASS-WITH-CONDITIONS · conditions: the four spikes run before implementation; S1 and S2 may change stated contracts; authors did not self-clear.`

---
**Handoff:** → spikes S1–S4, then `/implement`.
