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

`simplify: one scope per (project, target framework); ceiling ~50k assertions per scope; upgrade trigger = P2-PERF p95
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

> **DECIDED 2026-08-28 (`cl-0021`) — the extractor does NOT use `MSBuildWorkspace`.**
> D3 measured that loading a repository through `MSBuildWorkspace` executes code the repository
> supplied ([D3 result](../../spikes/msbuild-task-execution/RESULT.md)). Of the two containments
> measured ([containment result](../../spikes/extraction-containment/RESULT.md)), **Strategy 1 is
> adopted: read the project file as data and compile with Roslyn directly, always.** Repository code
> never executes — the principle stays literally true rather than becoming conditional. Where package
> references cannot be resolved (a fresh clone has no `project.assets.json`, and producing one is
> itself MSBuild evaluation), the projection **discloses the omission** rather than answering
> silently — the same shape S1 chose for absent generated symbols.
>
> The low-integrity sandbox (**A2**) is built, measured and kept as an escape hatch. Adopting it is a
> deliberate decision that must first close its **unmeasured network-egress gap**, not an automatic
> fallback.
>
> **The contract below was rewritten 2026-08-28** against a measured prototype
> ([fidelity result](../../spikes/extraction-fidelity/RESULT.md)), not an inferred one.

### Contract

```csharp
public sealed class CSharpExtractor(string extractorVersion) : IExtractor
{
    public string ScopeKind => "csharp";
    public Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken ct);
}
```

Same `IExtractor` as Phase 1 — the substitution the design promised. Named `CSharpExtractor` rather
than `RoslynExtractor`: Roslyn is still the semantic engine, but the name previously implied the
Roslyn *workspace* layer, which is exactly the part that is not used.

**One scope per (project, target framework).** Not per project. `MultiTarget` in the fidelity spike
declares `net10.0;netstandard2.0` and its `#if`-gated types differ between them; a single scope per
project would have to pick one framework and be silently wrong about the others. `MSBuildWorkspace`
loaded one framework and saw one of the two conditional types — the grain is the finding.

### What it reads, and what it never does

**The project file is data.** Nothing below evaluates MSBuild or runs a target, so there is no path
by which a repository's build logic executes ([D3](../../spikes/msbuild-task-execution/RESULT.md)).

| Input | Source | Why it is safe |
|---|---|---|
| Sources | SDK default glob, minus `bin/`+`obj/`, honouring `Compile Remove`/`Include` | Directory enumeration |
| Target frameworks | `TargetFramework` / `TargetFrameworks` | XML attribute read |
| Preprocessor symbols | Framework symbols synthesised per TFM (`NET10_0`, `*_OR_GREATER`, `NETSTANDARD2_0`, `WINDOWS`) + literal `DefineConstants` | Documented SDK behaviour |
| Global usings | The documented `ImplicitUsings` set + explicit `<Using Include>` | **Reproduced, not read from `obj/`** |
| Framework references | `Microsoft.NETCore.App.Ref` / `NETStandard.Library.Ref`, plus `Microsoft.WindowsDesktop.App.Ref` **first** when `UseWPF`/`UseWindowsForms` | Reference assemblies on disk |
| Project references | `ProjectReference` resolved recursively and compiled from source, cycle-guarded, depth-capped | Same rules, one level down |
| Package references | `obj/project.assets.json` **when present** | A JSON file; reading it executes nothing |

`simplify: recursive ProjectReference compilation rather than a build-order graph; ceiling is a
depth of 8; upgrade trigger = a real repository exceeds it or the repeated sub-compilation shows up
in P2-PERF-01.`

### The three disclosures this design owes the user

Every one is an **omission state on the projection**, never a silent answer — the shape S1 set for
absent generated symbols.

| Disclosure | When | Why it cannot be fixed by trying harder |
|---|---|---|
| `packages-not-restored` | No `obj/project.assets.json` | Producing one requires `dotnet restore`, which **is** MSBuild evaluation — the thing this design refuses |
| `xaml-generated-members-not-analysed` | `UseWPF`, and a `.xaml` with a code-behind partial | `InitializeComponent` and friends are generated into `obj/*.g.cs`. Measured: costs **0 types and 0 edges** on `AiDe.App`, because the generated half is UI wiring |
| `generated-code-not-analysed` | Always, under the S2 control | Unchanged from the 2026-08-26 decision |

### Measured fidelity (the basis for all of the above)

Against `MSBuildWorkspace` on four project shapes — no-reference, `ProjectReference`+WPF,
`ProjectReference`, and multi-targeted:

| | Result |
|---|---|
| Dependency edges resolved | **100.0%** on all four (3138, 300, 11, 3 edges) |
| Types lost | **0** on all four |
| Speed | **46–74 ms** vs MSBuildWorkspace's **796–1963 ms** (~25×) |

**Read the history with the number.** The spike's *first* run reported 82–89% edge resolution, and
that was two defects in the harness — missing implicit usings, and a `WindowsBase` facade shadowing
the real assembly — not a limit of the approach. Had it stopped there, the recorded conclusion would
have been "Option B loses ~15% of edges" and the strategy would likely have been reversed.

### Patterns

| Pattern | Why |
|---|---|
| **Adapter** (Roslyn `Compilation` → `EvidenceAssertion`) | The store's grain is already right; this only translates. Note the source is a `Compilation`, not a `Workspace`. |
| **Snapshot Replacement** (unchanged) | Inherited from Phase 1; nothing about real symbols changes it. |
| **Circuit Breaker per scope** (unchanged) | A project that fails to compile quarantines itself; the other projects keep their evidence. |

Ladder: `Microsoft.CodeAnalysis.CSharp` is rung 5 (a dependency), justified because re-implementing
C# semantic analysis is not a candidate. **`Microsoft.CodeAnalysis.Workspaces.MSBuild` is not taken**
— the layer that would have supplied project loading is the layer that executes repository code, and
replacing it costs ~250 lines of project-file reading measured at full fidelity.

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

### Terminal renderer — the surface

**BUILT 2026-08-27.** `TerminalScreen` + `VtParser` in Core (no WPF), `TerminalView` +
`TerminalPalette` + `TerminalInput` in the app, joined by `TerminalSurface`. The Phase-1b
placeholder pane is replaced by a live session, and **`TerminalSurface` is the first thing in the
product that passes `Integration: PowerShell`** — until now nothing did.

**S3's constraint is now enforced rather than recorded.** The renderer draws one `GlyphRun` per
*run of identical style* per line, and `AFullScreenRedraw_StaysInsideTheFrameBudget` measures a
200×50 screen at **5.50 ms p95** against a 16.67 ms budget — consistent with S3's 6.64 ms for this
path and three orders away from the 142.80 ms per-cell path. A mutation that reverts the run loop to
one draw per cell **is caught by that test**, which is what turns a design decision into a control:
per-cell text is the natural implementation and nothing about it looks wrong until it is measured.

**The model holds no WPF.** Wrapping, scrolling, erase extents and cursor clamping are
data-structure rules; behind a rendering framework each would be verifiable only by drawing pixels
and reading them back.

**Two parsers over one stream, deliberately.** `OscParser` reads for authenticated state and is a
security control; `VtParser` reads for what to draw and *skips* OSC so a title or clipboard sequence
never appears as text. Merging them would put display concerns inside the control.

| Decision | Why |
|---|---|
| Runs, not whole lines | A line rarely has one style. Same shape as the measured path; what real terminals do. |
| Present on the rendering tick, gated on `IsDirty` | The 1 MiB/s budget is an *output* rate, not a *draw* rate. Redrawing per write spends the budget on states nobody observes; without the dirty flag a motionless terminal repaints forever. |
| Parse off the UI thread, draw on it | Marshalling every chunk to the dispatcher would put a megabyte a second of parse work on the thread that must stay responsive to typing. |
| Scrolling, no scrollback | History needs its own memory budget. Growing the viewport to fake it puts an unbounded allocation behind an innocuous property, sized by the child process. |
| Palette in `App.xaml` | Raw colour literals are legal in exactly one file (`TokenDisciplineTests`). The ANSI sixteen are a required vocabulary — what a theme may choose is the shade, never the meaning. |
| Keys mapped away from the control | Every entry has an exact right answer and a wrong one is a key that silently does nothing. Text arrives through `OnTextInput`, never mapped from key codes, or every non-US layout breaks. |

#### DC-014's condition was too strong, and it nearly cost the architecture

`AiDe.App` is a GUI application with **no console at all**. Read literally, DC-014 ("ConPTY attaches
a child only when the launching process owns a real console") says every terminal pane in the
product must be permanently empty — and **no test in the suite would have failed**, because none ran
in that configuration.

Two stand-ins gave two wrong answers before the real one. A probe calling `FreeConsole()` to
*simulate* a GUI host captured nothing. A genuine WinExe probe *still* captured nothing when started
by the test host, because with `UseShellExecute = false` the child inherits the runner's redirected
standard handles. Shell-executed, the same binary captured **291 characters**.

**The operative condition is which standard handles the host was given, not whether it owns a
console.** `tests/AiDe.App.TerminalProbe` is a WinExe whose `OutputType` *is* the thing under test,
and DC-014 now carries the correction. The corollary is general: **a stand-in for a configuration is
not evidence about that configuration.**

#### Measured in the real app, with one finding only running it could produce

The app was launched and a terminal pane rendered a live PowerShell prompt. It also showed:

> *PowerShell detected that you might be using a screen reader and has disabled PSReadLine for
> compatibility purposes.*

So on this machine **the shell integration correctly declines to install** — no line-accept hook, so
no `C` mark, so per the all-or-nothing rule it installs nothing and the session keeps the heuristic.
The control behaves exactly as designed; what is new is the *frequency*. The no-integration path is
not an exotic fallback, it is what happens on any machine where PowerShell detects a screen reader.

**Not mitigated, and deliberately so.** The obvious fix — `Import-Module PSReadLine` from our script
— overrides an accessibility accommodation the shell made on the user's behalf. ADR-0014 withdrew
the conformance *obligation*; it did not license undoing an accommodation to gain a nicer status
indicator. Carried as a known limitation: on such machines Ready/Busy comes from the coarse
heuristic.

### Shell integration — the half that makes the control operate

**BUILT 2026-08-27.** `ShellIntegration.PowerShellScript(nonce)` composes the shell-side script;
`TerminalSessionRequest.Integration` opts a session into it and the runtime decorates the command
line itself.

**The nonce forced the shape.** It is generated per session and lives only in memory, so no script a
user commits to a profile can carry it — the integration must be composed at session start, by us,
from that session's secret. That is also what makes it a control: the script is the only thing that
knows the nonce. Consequently the **nonce is generated in `StartAsync` before the process exists**,
not in the constructor, because it has to be inside the command line.

**All of the loop, or none of it — and this is a safety rule, not tidiness.** An authenticated claim
retires the output heuristic. An integration that marked the prompt (`D`/`A`/`B`) but not command
start (`C`) would therefore pin a session at `Ready` for the whole duration of every command: a
confident wrong answer, and strictly worse than the coarse signal it displaced. So the script checks
it can hook line-accept **before** overriding anything and returns without installing if it cannot.
A session with no integration is supported; a session with half of one is not.

**Measured 2026-08-27 through a real pseudo console** (`P2-TERM-08`, `mode: integration`): a real
`powershell.exe` reached `Ready` at its prompt, `Busy` while a 4-second command ran, and `Ready`
again afterwards. Two unknowns closed on the way — **PSReadLine does load under `-NoProfile` inside
ConPTY**, and `-EncodedCommand` survives the ConPTY launch path.

| Decision | Why |
|---|---|
| `-EncodedCommand` (UTF-16LE base64) | The script's quotes, `;` and `$` would otherwise cross two parsers — the Win32 command line and PowerShell's. Base64 has no metacharacters. |
| `-NoProfile` | A user profile can redefine `prompt` after us, print banners, or fail; none of that should change what the product reports. |
| Non-hex nonce **refused, not escaped** | Escaping is a standing claim about PowerShell quoting rules that must remain true forever. Every nonce we generate is hex. |
| Enter handler calls `AcceptLine()` **then** marks `C` | Ends PSReadLine's render first, so the write cannot land mid-repaint. The command does not start until the handler returns, so `C` still precedes its first output byte. |
| PowerShell only | `cmd.exe` has no line-accept hook and its prompt is a string, not a function, so it can only ever produce the half-loop the rule above forbids. A cmd session keeps the heuristic. |

**Known redundancy, stated so it is not mistaken for load-bearing:** for *state*, `D` and `A` both
mean `Ready`, and deleting either changes no reported activity. `D` is kept because it carries the
finished command's exit code — the only place command success is reported at all.

Eleven controls were mutation-tested one at a time; all eleven were caught. Two survived the first
run and both were faults in the mutations rather than the controls: one commented a line out with
`#`, leaving the asserted substring in the file, and one relied on a text-position assertion that
could not see a disabled guard. The second is why the bail-out now has a **behavioural** test that
runs the real script in a PowerShell whose module path is emptied.

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

### The named-pipe transport and the daemon process

**BUILT 2026-08-27.** `IpcFraming`, `IpcPipeName`, `WorkspaceLock`, `IpcPipeFactory`, `IpcServer`,
`IpcClient`, and `src/AiDe.Daemon` — a real second process. The decision layer landed transport-free
in `36f0c47` precisely so its security choices were testable without a socket; this is the plumbing
underneath it, and the split is now real rather than designed.

| Concern | Decision |
|---|---|
| Framing | 4-byte big-endian length + UTF-8, capped at 1 MiB. The prefix is attacker-chosen, so the cap is checked **before** any allocation. A short read is `null` ("peer hung up"), not an exception — only a negative or oversized length is a protocol violation. |
| Pipe name | `aide.` + half a SHA-256 of the lowercased path. Derived so both ends agree without talking, and hashed so the name — which any process can enumerate — does not disclose which repository a user has open. |
| ACL | Exactly one Allow rule, for the owner SID. Nothing for Everyone, Authenticated Users or Administrators. Read back by a test rather than trusted. |
| Client | `PipeOptions.CurrentUserOnly`, which defends the **opposite** direction: the ACL stops another user reaching our daemon, this stops us reaching theirs. |
| Peer identity | SID by impersonation, PID from `GetNamedPipeClientProcessId` — both from the kernel, never from the payload. Derived **after the first frame**, because Windows refuses to impersonate "until data has been read from that pipe"; no authorization decision is made before it exists. |
| Workspace lock | A `Local\` named mutex taken **first**, before a pipe exists. Kernel-released on death, so no staleness heuristic. |
| Daemon lifetime | Exits when nobody has needed it for the grace period. An orphan holds the workspace lock invisibly, making the workspace unopenable. |

**Scope stated rather than disguised:** the daemon serves `ping` and `epoch`. Moving `describe`,
`find`, `impact` and the dispatch surface behind the endpoint is the next piece of the process split,
and doing half of it here would leave a boundary partly crossed — worse than one honestly not yet.

#### The read surface now crosses the boundary

**BUILT 2026-08-27.** `WorkspaceOperations` registers `describe`, `impact`, `find` and `knowledge`
on the endpoint; `WorkspaceClient` is the typed proxy; the daemon opens a real `WorkspaceCore`.
Until this, the boundary existed and almost nothing crossed it.

**The property the tests assert is agreement**: each projection is run in process and across the
pipe against one store, and the whole results are compared. Serialisation is where agreement is
lost — a field that does not round-trip, an enum that renumbers, a bound nothing read — and
comparing a node id would catch none of them.

**Enums travel as strings.** By number, inserting a member renumbers every later one, and the
dual-major handshake exists so an old shell may meet a new daemon: that is a wire break with no
error and no symptom except wrong answers.

**The handshake now returns the epoch** (`IpcOpenResult`), which a protocol gap forced. Every
command states the epoch it was authored against and the daemon rejects a mismatch — so a freshly
connected shell could not ask for the epoch, because asking is itself a command subject to the
fence. Returning it from the handshake is the only ordering that terminates; exempting an `epoch`
operation would have put a hole in the fence to work around an ordering problem.

**Not moved:** dispatch. Writing to a terminal and staging a prompt carry ADR-0010's two-phase
receipt semantics, and half-crossing that boundary is worse than not yet crossing it.

#### Three controls that could not fire, and what replaced them

Mutation testing found the same shape three times, now registered as **DC-016**:

- **A per-connection in-flight semaphore** meant to refuse a command flood. The serve loop reads,
  answers, then reads again, so in-flight is one by construction and the refusal was unreachable.
  **Removed rather than made reachable** by adding concurrency the design does not want. What
  actually bounds a flood is serial service per connection, the frame cap, and the connection cap —
  a client that writes faster than we read blocks on its own write, which is backpressure applied by
  the kernel rather than memory spent by us.
- **The owner-SID check**, unreachable in a single-user environment because the ACL already admits
  only that user. Deleting it failed no test. `IpcServer` now accepts the **expected** owner SID, so
  a server told to expect a different one must refuse the peer it gets.
- **`WorkspaceLock`**, which used a Windows mutex alone. A mutex is owned by a *thread* and is
  re-entrant, so a second acquisition inside one process succeeded — and ADR-0009 keeps an
  in-process daemon as a supported hosting mode, which is exactly the case it most needed to cover.
  It now tracks in-process holders as well.

#### Two defects the tests found before a user could

- **A deaf client could hold a listener indefinitely.** A client that pipelines requests and never
  drains responses fills the pipe buffer; the daemon blocks writing, stops reading, and that
  listener is held for as long as the client likes. With a fixed pool, enough of them make the
  daemon unreachable. Found when a flood test deadlocked. Fixed with a response-write timeout that
  abandons the connection, and covered by `AClientThatNeverReads_IsDisconnected`.
- **The idle reaper sampled instead of remembering.** It polled `ActiveConnections` every 100 ms, so
  a client that connected and left between polls was never observed — the daemon then waited out the
  full 60-second *startup* grace instead of the short *idle* one. The reaper now decides from
  `ServedConnections` and a stamp written when a connection ends.

`P2-IPC-07` is therefore satisfied by **backpressure and caps, not by a refusal code** — recorded
here because the design's wording ("per-connection admission") implies a rejection that this shape
of server cannot produce without adding concurrency for its own sake.

Ten controls were mutation-tested one at a time; all ten were caught. Four needed a runtime-false
rather than `if (false)`, which trips CS0162 under `TreatWarningsAsErrors` and fails the build — an
empty result reading as "all passed" is the DC-012 shape, already recorded once on this boundary.

### The shell uses the daemon

**BUILT 2026-08-27.** `IWorkspaceQueries` (the seam), `LocalWorkspaceQueries` (in-process),
`WorkspaceClient` (remote), `ShellBootstrap` (connect-or-launch), and the app switched onto it. The
daemon ships in a `daemon/` folder beside the shell.

**Measured by running it:** the app starts, spawns exactly one daemon process, and renders its
evidence panes from answers that crossed the pipe. Before this the split existed in tests and
nowhere a user could reach.

| Decision | Why |
|---|---|
| Connect, then launch | One daemon per workspace is enforced by the lock, so launching first would mean the second shell starts a process whose only job is to discover it is redundant. Racing shells are safe *because* of the lock — adding our own would put a second mechanism in front of the one that already decides correctly. |
| No fallback to in-process | A shell that quietly ran the core itself would work, and would abandon the trust boundary, the workspace lock and the epoch fence at the moment they were most obviously needed. The failure is shown (**DC-011**). |
| The seam is async | The remote case is the real one. A synchronous seam blocks a UI thread for a pipe round trip; the local adapter completes immediately and pays nothing. |
| Window first, workspace second | Reaching a daemon can mean a cold process start. A window that appears only once another process has launched looks like a failure to launch, so the shell shows immediately and `AttachWorkspace` points it at the daemon when it resolves. |
| Daemon in its own folder | The two share assemblies. Merging the outputs is a clobber waiting for the first version where they differ — which is exactly what the dual-major handshake exists to survive. |
| Heading shows the folder name | The workspace id is a hash *so the path does not travel with it*, which makes it the wrong thing to show a user asking which workspace they are in. |

#### The defect 459 passing tests could not see

Moving the pane to async left `SurfaceContentFactory` binding `pane.Rows` and `pane.StatusMessage`
at construction — before the load ran. `Rows` is replaced by the load and is not observable, so both
evidence panes sat on *"Loading evidence…"* permanently. The pane view model was correct and had its
own test class; nothing asserted on what the **control** displayed. It was found by running the
application and looking at it.

Registered as **DC-017 — verified one layer below the one that actually fails**, with
`SurfaceContentTests` as the control: it builds the surface through the real factory, pumps the
dispatcher, and asserts on what the control ends up showing.

Mutation also found an **unreachable catch** in the fix itself: the pane already degrades internally
and only cancellation escapes it, so the general `catch` wrote a message that could never appear. It
was removed rather than kept (**DC-016**).

### The first write crosses: scope refresh

**BUILT 2026-08-27.** `ScopeRefreshService` on the daemon, `IWorkspaceCommands` as the write seam,
`WorkspaceClient.RefreshScopeAsync` on the shell, and a `workspace.refresh` palette command so it is
reachable from the keyboard.

**Started and polled, never awaited on the wire.** A scope has a 60-second budget and the lane
serves one request at a time per connection — a refresh that answered only on completion would hold
that connection for the whole budget, and the response-write timeout would abandon it first. **The
control lane carries commands; a command that starts long work returns once the work is started.**

**The command id is the idempotency key**, exactly as the architecture's command protocol says, and
this is the first place it matters across a process boundary: two extractions of one scope both bump
the generation and the loser's work is discarded after costing a full budget. Deduplication has two
guards — a fast path for the sequential retry, and `TryAdd` for the concurrent one — and only the
second is load-bearing.

**Job records are bounded**, because they are keyed by a caller-chosen id: an unbounded map is a
memory leak any client can drive. A *running* job is never evicted — its status is the only record
that the extraction is happening.

**An incomplete extraction is a failure, not a refresh of zero.** The previous snapshot keeps
rendering, and reporting success would present stale evidence as freshly confirmed.

**Reads and writes are separate seams.** `IWorkspaceQueries` and `IWorkspaceCommands`: a read repeats
freely, a write bumps a generation, carries an idempotency key and is judged against the epoch fence.
One interface would put a name on the seam that half its members contradict.

#### Two defects the mutation run found

- **Announcements were not marshalled.** A re-index reports its outcome from background work, and the
  live region is a WPF control — an unmarshalled call throws exactly when the product is trying to
  tell the user something. `WorkbenchAnnouncer` now marshals internally, because it owns the control
  and is the only thing that knows a dispatcher is involved. `RecordingAnnouncer` could not catch
  this (it has no dispatcher), so the test creates a real one on an STA thread.
- **`RecordingAnnouncer` was not thread-safe**, and now receives announcements from background work.

**The existing catalog conformance test earned its keep**: adding `workspace.refresh` without a
handler failed immediately with *"palette lists commands that do nothing"* — the SC 2.5.7 control
working exactly as intended.

#### The dispatch divergence, resolved: terminals stay in the shell

**DECIDED 2026-08-28 (D1, `cl-0011`).** `ADR-0010`'s two-phase receipts were recorded here as blocked
on a prior question: the failure table read as though terminals were the daemon's children, while
`TerminalSurface` creates its `ConPtyTerminalSession` in the shell.

**The divergence was in this document, not between the document and the code.** The *What moves*
table above already places terminals in Shell Bootstrap's Job Object — shell-side. Only the
failure row said otherwise, and it has been corrected.

**The mitigation was never missing.** `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` is implemented in
`ConPtyInterop`, and a Job Object is owned by the process that creates it. The control fires when
the *shell* dies, which is the case that actually strands a CLI. A daemon crash leaves terminals
running because they were never its children — correct behaviour, now recorded as such.

Terminals are **not** moved to the daemon. Terminal output is the highest-rate stream in the product
and its consumer — the WPF surface, measured at 5.50 ms p95 against a 16.67 ms budget — lives in the
shell. Crossing a request/response pipe would buy a second lane, framing cost and a fresh
backpressure design for a stream that would have to come straight back. It would also invert
[ADR-0003](../adr/0003-workspace-daemon-boundary.md), which scopes the daemon to evidence rather
than to UI.

**Prompt dispatch is therefore unblocked**, and `ADR-0010` is the remaining work rather than the
remaining question.

### Upgrade and rollback (P2-UPGRADE-01..03)

**BUILT 2026-08-27.** `MigrationJournal`, `StoreSnapshot`, `HealthGate`, `UpgradeCoordinator`,
`DaemonInstallation` — and the daemon runs `RecoverIfIncomplete` at startup, before it opens the
store, so the mechanism is used rather than merely present.

**The asymmetry that shapes all of it:** an upgrade that fails halfway is worse than one that never
started. A store migrated to a schema the running binary cannot read is a workspace nobody can open,
with the user's evidence inside it. So the ordering is: **snapshot → journal → migrate → gate →
commit**, and the point of no return (deleting the snapshot) is last.

| Piece | Decision |
|---|---|
| Journal | Latest state only, not a log — it answers one question and a history would make that a parse. Replaced by temp-then-rename. A torn journal reads as "nothing in flight" rather than throwing, because the recovery path must not be the thing that crashes after a crash. |
| Snapshot | Copied, never moved. Renaming would leave a window with no store at all, which is the one state a crash must never find. |
| Health gate | The **fast subset**, with the 60-second budget **enforced**. Full restore/replay equality stays asynchronous — P1-PERF measured a 50k-edge replay against a 15-minute RTO, and a gate that merely documented its budget would pass it. Stops at the first failure (later checks assume earlier ones held) and reports every check it ran, because a green gate is evidence the gate passed, not that its contents did. |
| Rollback | Undoing a migration that already happened, not declining to run one — which is why the snapshot exists at all. |
| Recovery | A separate startup entry point, because the case it handles is the one where nothing got to finish. |
| Side-by-side | Keeping the previous build is what makes rollback possible: restoring a store achieves nothing if the only binary on disk is the one that could not read it. Repointing is one atomic write and is the commit. Pruning protects the current build **explicitly** — after a rollback the current version is an *older* one, exactly when "keep the newest N" would delete what is running. |

#### Three defects the mutation run and its tests found

- **Rollback restored to the wrong path.** It derived the store's location from the snapshot's
  filename and passed only because the fixture put both in one directory. The store and the upgrade's
  scratch space are independent by design; a workspace with its store elsewhere would have had the
  snapshot restored to a path that is not the store — a rollback that silently does nothing.
- **The atomic replace was not atomic.** `File.ReadAllText` does not share delete, so on Windows a
  concurrent reader makes the *writer* throw `UnauthorizedAccessException` from `File.Move`. Readers
  now open with `FileShare.ReadWrite | FileShare.Delete`.
- **And it still needed a bounded retry.** With a reader holding the previous file, the renamed-over
  name is left *delete-pending*, and the next replace can fail with access-denied though nothing is
  wrong. Twenty attempts with a small backoff; the last is allowed to throw, because a journal that
  cannot be written is a real failure the caller must not proceed past.

**Two tests that proved nothing until mutation said so:** the enum-naming test asserted a
round-tripped value, which a *numeric* enum also satisfies — it now asserts on the wire text; and
nothing distinguished atomic replacement from an in-place write, which is now covered by a
concurrent reader that must never observe a torn journal.

Thirteen controls were mutation-tested one at a time; all thirteen were caught.

**Not built:** the health gate's *contents* for a real schema migration — the store has one schema
version and no migration chain yet, so the gate is exercised with checks supplied by its caller. The
mechanism ships before the first breaking change because a migration hook added afterwards is added
too late for every store already on disk, which is the same reason `LayoutMigrations` shipped empty.

## Failure-mode analysis

| Failure mode | From which choice | Disposition | How addressed | Test |
|---|---|---|---|---|
| **Shell** crashes, agent CLIs keep running headless | Terminal ownership (D1) | **prevent** | Terminals in the shell's Job Object with `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`; the kernel reaps them when the owning process dies | `P2-TERM-05` |
| Daemon crashes while terminals run | Process split | **tolerate** | Terminals are the shell's children and are unaffected; Bootstrap detects and raises `aide.core.restart`. This is correct behaviour, not an orphan | `P2-IPC-05` |
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

### Privacy, seeded and measured (P2-PRIV-01/02)

**BUILT 2026-08-27.** `TerminalPrivacyTests` (P2-PRIV-01, out of process against a real ConPTY child)
and `PrivacyMarkerTests` (P2-PRIV-02, the daemon's own spans).

**An absence is seeded, never reasoned about.** Reading the code and concluding nothing writes output
to the store is an inference; printing a unique string and then searching every span attribute and
every file the workspace wrote is a measurement.

**Two ways these tests could have lied, and both did before they were fixed:**

- **The seed never arriving** makes every absence hold trivially. The probe now requires the seed to
  reach the output channel before it asserts it is nowhere else.
- **A file that could not be read** is a file the scan did not cover. The first run reported *"could
  not read workspace.db"* — SQLite held it open, so **the store, the most important file in the
  check, was never scanned** while the probe reported success. The core is now closed before
  scanning, and an unreadable file fails the run rather than being noted.

**The command line is asserted as well as the output**, because the privacy analysis makes them
separate claims: a session's command line "may contain paths or arguments" and is excluded from
telemetry. Nothing tested that until a mutation added a command-line tag and no test failed; the seed
now travels in both.

#### The privacy net had a hole for four commits — DC-018

`TelemetryTests` enforces the floor over `ActivitySource`s named `aide.*`. Every source added with
the process split was named `AiDe.Core.*`. **The IPC boundary, the terminal runtime and the upgrade
coordinator were emitting spans no privacy assertion could see** — including spans on the first
cross-process trust boundary in the product.

The sources are renamed (`aide.ipc.command`, `aide.ipc.connection`, `aide.terminal.runtime`,
`aide.upgrade.gate`), and a control now fails when one is not. It scans source text rather than
reflecting, so an emitter no test exercises is still covered — and its own listener subscribes to
*every* source, because a listener scoped to the convention cannot see what broke it.

**That control was itself vacuous on the first attempt**: it matched `new ActivitySource("…")` while
every declaration is target-typed `= new("…")`, so it scanned zero sources and passed. It now asserts
a minimum match count.

Four controls were mutation-tested; all four were caught.

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

### `P2-PERF-01..03` — specified 2026-08-28

The suite was named here from the start and never given cases. Naming a gate is not specifying one,
and a `simplify:` ceiling whose upgrade trigger points at an unspecified suite has no trigger at all.

| Case | What it measures | Budget | Status |
|---|---|---|---|
| `P2-PERF-01` | **Scope settlement** — one Roslyn scope (one project) from load to committed snapshot. The first and only test of the scope-per-project decision. | p95 **< 10 s** on the approved corpus (from the `simplify:` marker above) | **BLOCKED** — needs Component 1, which D3 blocks. |
| `P2-PERF-02` | **The daemon boundary tax** — the same projection run in process and over a real pipe against **one** store, so the difference is serialisation plus transport and nothing else. | The Phase-1 user-facing budgets still hold end to end: `describe` p95 < 100 ms, `impact` p95 < 250 ms | ✅ **MEASURED 2026-08-28** |
| `P2-PERF-03` | **Terminal throughput** — output *held* at the architecture's 1 MiB/s case, not burst through it. | Rate actually sustained; chunk parse p95 **< 16.67 ms**; per-chunk cost must not grow across the run | ✅ **MEASURED 2026-08-28** |

**`P2-PERF-02`, measured** (`dotnet run --project bench/AiDe.Bench -c Release -- p2`, 50,000-edge
corpus, 30 warm samples, Release):

| Projection | In process p95 | Over the pipe p95 | Boundary tax | Headroom to budget |
|---|---|---|---|---|
| `describe` | 0.58 ms | **0.92 ms** | +0.34 ms (1.6×) | 99.08 ms |
| `impact` | 0.43 ms | **0.79 ms** | +0.36 ms (1.8×) | 249.21 ms |

**The multiplier is the wrong number to fear.** Crossing the boundary costs roughly **0.35 ms flat**
— framing, a pipe write, a read and deserialisation — which lands as 1.6–1.8× only because the
projections themselves are sub-millisecond. Against the budgets that actually gate the user's
experience, the boundary consumes **0.35% of `describe`'s 100 ms** and less of `impact`'s. The
process split is not a performance risk on the read path at this corpus size, and now that is
measured rather than assumed.

Cold first call over the pipe is 14.73 ms (describe) against 13.85 ms in process — the connect and
handshake are a one-off, not a per-call cost.

**What this does not cover:** one client, one connection, no contention, and a 50k-edge corpus. It
says nothing about concurrent shells, a saturated control lane, or the write path.

**`P2-PERF-03`, measured** (200×50 screen, 64 KiB chunks of plausible build output — text with SGR,
cursor and erase traffic — held at 1 MiB/s for 10 s):

| | Result |
|---|---|
| Sustained rate | **1.00 MiB/s over 10.0 s** (10.0 MiB, 160 chunks) |
| Chunk parse | p50 **1.05 ms**, p95 **1.80 ms**, p99 1.96 ms |
| Per-chunk drift | 0.897 ms (first quarter) → 1.191 ms (last quarter), **+0.293 ms** |
| Unthrottled ceiling | **77 MiB/s** — 77× the budget |

**The drift check is the reason to sustain rather than burst.** A burst measures the fast path; a
parser that allocates per escape sequence looks fine for a moment and degrades as the heap fills.
The observed +0.293 ms is well inside the doubling threshold that fails the gate, but it is *not*
zero, and it is now a tracked number rather than an assumption.

**A discrepancy worth recording rather than smoothing over.** S3 reported VT scanning at **2361×**
the budget; this measures **77×**. They are not the same quantity — S3 scanned, this one *scans and
applies to the screen model*, writing cells, moving the cursor and scrolling. Neither number is
wrong; the S3 figure must simply never be quoted as end-to-end terminal throughput, because it
excludes the work that dominates.

**What this does not cover:** the **draw** half. It needs a dispatcher and a real visual tree, so it
stays in `AFullScreenRedraw_StaysInsideTheFrameBudget` (5.50 ms p95, App tests). This number is a
parse-and-model cost and must not be quoted as a frame time.

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

**BUILT 2026-08-28 — complete, including the keyboard-trap test.** `CanvasFocusRouter` (Core, no
WPF) holds the policy; `CanvasFocusTarget` implements the Win32 half over the `HwndHost`;
`CanvasSurface` is the windowed WebView2 pane with the page's boundary handlers inlined so they
cannot be separated from the control that depends on them. `workbench.focusCanvas` (`Ctrl+K, G`)
routes through `WorkbenchController`.

| Test | State |
|---|---|
| `P2-FOCUS-01` | ✅ In-process seam tests **and** a real WebView2: focus lands, verified by reading `GetFocus` back |
| `P2-FOCUS-02` | ✅ Pre-entry focus recorded; `Esc` restores it; falls forward when that element is gone |
| `P2-FOCUS-03` | ✅ **Out of process** (`tests/AiDe.App.CanvasProbe`) — a real window, a real WebView2, real keys |
| `P2-FOCUS-04` | ✅ Refused **and announced** while the snapshot swap is showing |

**`P2-FOCUS-03` took three routes to get honest, and the first two failed silently-looking.** A
posted `WM_KEYDOWN` never reaches Chromium's key handling. `SendInput` delivers to the **foreground**
window, which neither a `dotnet test` host nor a shell-launched probe can hold. Both produced the
same symptom — *"the page never posted `focus.leave`"* — which reads exactly like a keyboard trap.
The difference was measured, not guessed: the page reported `activeElement="first"`, so focus **had**
landed, while `window.__tabsSeen` was **0**. A trap test that fails because the keys never arrived is
**DC-016** wearing the right label, and it would have been "fixed" by weakening the assertion. Keys
now enter through the browser's own input layer, and the probe reports 3 Tab keydowns seen before
`focus.leave` arrives.

**What that still does not cover:** the OS→browser hop. Injecting at the renderer's input layer
cannot catch a regression where the host swallows the key before the browser sees it.

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
- **RESOLVED — the `ITerminalSession` conformance claim.** `P2-CONFORM` now runs: `TerminalSessionConformanceTests` is executed against **both** `FixtureTerminalSession` and `ConPtyTerminalSession`, so the dispatch tests written against the fixture prove something about the seam. Originally flagged as:
  the fixture and the real runtime agree on the half that was specified, and nothing yet proves they
  agree on output, activity or exit.
- **ACCEPTED (D5, `cl-0015`) — cross-monitor DPI remains unverified** and is explicitly **non-blocking**. The owner works on a laptop with no second display; this is validated once multi-monitor hardware is available and the product is further along. The DPI arithmetic already has evidence — the snapshot swap measured aligned to within a pixel of rounding at 150% DPI — so what is missing is the monitor-*transition* case. Failure mode is visual misalignment on a multi-monitor drag: user-visible, not data-threatening. Originally flagged because
  Phase 2 adds floating terminal panes.
- **`MSBuildWorkspace` failure modes are environmental** — SDK version, NuGet restore state, missing
  targets — and are the least predictable dependency in the phase. **Narrowed by S2:** a patch-level
  SDK mismatch loads cleanly with zero diagnostics. A cross-major mismatch, and a repository pinning
  an SDK that is not installed, remain unmeasured.
- **DECIDED (D2, `cl-0012`) — the MSBuildWorkspace dependency chain carries ten high-severity advisories** (S2 finding 7). The shipped extractor **adopts the spike's posture** — every reference `ExcludeAssets="runtime"`, MSBuild loaded at runtime from the installed SDK via `MSBuildLocator` — and the claim is then **verified by inspecting the built output directory** for the flagged assemblies. If none ship, the exposure is a reference-assembly artifact and the residual is the user's own SDK, which they already execute to build their code. **Component 1 is not blocked on this.** Originally flagged
  no clean version identified. Unresolved for the shipped extractor.
- **RESOLVED (D3 → `cl-0021`) — loading a repository through `MSBuildWorkspace` executes repository-supplied code, so the extractor will not use it.** D3 measured all four vectors firing on `OpenProjectAsync` with **zero** `WorkspaceFailed` diagnostics; two need nothing but the checked-in `.csproj`. Both containments were then measured: a job object alone contains **nothing** (4/4 still land), low integrity + job blocks all four with extraction intact, and a no-MSBuild path recovers **159/159 types on `AiDe.Core` at 359 ms against 2210 ms**. **Strategy 1 adopted** — no MSBuild, ever; disclose unresolved references. Registered as **DC-019**. **Residual, live:** Option B's fidelity is proven on one project with no `ProjectReference`; multi-targeting, custom globs and `Directory.Build.props` are untested, and fidelity failures are silent.
- **ACCEPTED (D6, `cl-0016`) — the terminal has no scrollback, and this is a stated product limitation for Phase 2.** `TerminalScreen` is viewport-only: when the cursor passes the last row the grid shifts up and the top row is discarded. The engineering reason holds (history needs its own memory budget; growing the viewport would put an unbounded allocation behind an innocuous property, sized by an untrusted child process) and the upgrade path is designed — a bounded ring *beside* the screen, not inside it. **Backlogged, not forgotten.** It is named here as a product limitation rather than a technical note because Phase 2's human validation is *"launch `pwsh`, observe real session state"*, and a developer who runs a build and cannot scroll up to read the errors will read that as broken.
- **CLOSED (D7, `cl-0017`) — `P2-PERF-01..03` was named in the test plan but never specified.** Found 2026-08-28: the suite was cited three times with no cases and one budget, and that budget measured Roslyn extraction, which does not exist. Now specified below. **`P2-PERF-02` is measured**; `P2-PERF-01` is blocked behind Component 1 (D3).

## Status and next action

*Refreshed 2026-08-28. **Phase 2 is complete** — see the
[exit review](../reviews/phase-2-exit.md).*

| | |
|---|---|
| **Completed** | **All three components.** Component 1: `CSharpExtractor` reads the project file as data and never runs MSBuild, emitting disclosures as facts on the scope node; discovery yields one scope per (project, framework); a broken or slow project quarantines itself and raises a health incident (`P2-EXT-02/04`). Component 2: ConPTY runtime, OSC 133, shell integration, WPF renderer, `P2-CONFORM` against both implementations. Component 3: named-pipe transport, `AiDe.Daemon.exe`, the read surface, scope refresh, prompt dispatch (`ADR-0010`) across the boundary, `P2-UPGRADE-01..03`, `P2-PRIV-01/02`. The workbench now indexes (`Ctrl+K, I`), dispatches (`Ctrl+K, P`), focuses the canvas (`Ctrl+K, G`) and renders real graph data. **541 tests; four gates green; all three performance budgets met with an order of magnitude of headroom.** |
| **Remaining (carried to Phase 3)** | **The Option-B fidelity spike is the first owed item**, not a Phase-3 feature: 100% edge resolution is measured on four project shapes but not on shared projects, `Directory.Build.props` inheritance or `Compile Link`, and a fidelity failure in an extractor is *silent*. Built-but-unreachable: upgrade/rollback has no UI, MCP tools are not exposed, the canvas has no navigation or layout algorithm. `ADR-0010` stays `proposed` until dispatch is exercised against a real agent session. Accepted by decision: A2's network gap, cross-monitor DPI (D5), no scrollback (D6). |
| **Best next action** | **Extend the fidelity spike to the unmet project shapes.** It is the one open item that can be wrong without anyone noticing, and it gates indexing any repository other than this one with confidence. |

## Gate record

`GATE design · 2026-08-26 · Patterns Expert ⇄ Simplifier (no new fact table justified; scope-per-project chosen over streaming with a simplify: ceiling); Test Architect (D0,D1,D2,D3,D4,D5-provider,D6,D7 enumerated; the D7 conformance suite is newly owed and named); Security & Identity (the cross-process boundary Phase 1 deferred is restored; the Roslyn analyzer-execution threat is new and mitigated); Distributed Systems (dual-major handshake, capability revocation, post-rollback pairing as an explicit cell); Privacy (terminal output is the highest-volume work data in the product and stays ephemeral by construction); SRE (health gate keeps the fast subset only — the v1 contradiction must not return) · verdict: PASS-WITH-CONDITIONS · conditions: the four spikes run before implementation; S1 and S2 may change stated contracts; authors did not self-clear.`

---
**Handoff:** → spikes S1–S4, then `/implement`.
