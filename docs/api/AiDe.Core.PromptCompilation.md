---
id: api-aide-core-promptcompilation
title: "API: AiDe.Core.PromptCompilation"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.PromptCompilation: 46 types, 164 members, 81% carrying a summary doc comment.
---

# API: `AiDe.Core.PromptCompilation`

**46 public types · 164 public members · 81% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `CompileContract`

*class* — `CompileContract.cs`

The versioned pair the agentic rung calls (ADR-0033 rule 1): `compile-prompt/1` in,
`compile-output/1` out — the contract's constants, its allow-list and the
two host-embedded texts. **Shipped inert in this slice**: no model call exists here (the
agentic rung is CV-3's, behind PD-5 and the eval gate); the validator and the assembler are
tested red-first over authored fixtures so the rung has a typed boundary to call.

**Remarks.** **The header and the template are host-compiled bytes** — never read from the workspace
or `.claude/` (§A8.3; P-D3's falsifier: a workspace file named like the template changes no
byte). They are string constants — the ladder's lower rung (host-compiled bytes need no resource
pipeline) and the shape a `const` keeps deterministic; ADR-0033's "embedded resources" named
the property (host-embedded, never read from disk), which a constant satisfies. `simplify:`
ceiling — a template over ~200 lines, or a second family's profile shipped as text, moves the
texts to `Compilation/Resources/` under an `EmbeddedResource` glob;
`PromptSha` is unchanged as long as the bytes are.





**X-3 (the Shell-lane seam slice): the glob landed, the move did not.** The csproj now
carries `<EmbeddedResource Include="Compilation\Resources\*" />`, so a future profile
dropped there needs no build-file edit. `HostHeader` and `Template` are
~15 lines together today — under this remark's own ceiling and under the item's 20-line floor —
and no file exists yet under `Compilation/Resources/`, so moving them now would be motion
with no ceiling crossed. Left as this constant pair; move when either trigger fires.

| Member | Summary |
|---|---|
| `string Version = "compile-prompt/1"` | The prompt contract's version — `called.contract_version`; names the pair. |
| `string OutputContract = "compile-output/1"` | The output contract the model must answer with. |
| `int MaxValueChars = 2000` | A proposal's value bound (§A8.3): ≤ 2000 chars, no control characters. |
| `int MaxNotesChars = 500` | The `notes` bound: ≤ 500 chars, shown as provenance, never applied. |
| `IReadOnlyList<string> AllowList = DecorationNames.StructureLines` | The allow-list (v1): the three structure lines, and only the ones the prompt named as open. Case-sensitive. |
| `string HostHeader =` | The fixed host header — **the prompt's first bytes, always**: a prompt whose first bytes are `/…` is executed by the CLI as a local command (adapter 0.75.1 `acp-agent.js:6035`), so the compile prompt never begins with… |
| `string Template =` | The `compile-prompt/1` template — the blocks in order, each named; `{{…}}` slots the assembler fills. |
| `ReadOnlySpan<byte> TemplateBytes` | The header's and the template's bytes, as hashed and as sent. |

## `CompileLine`

*class* — `CompileLine.cs`

The compile line's strings (spec Addendum D §A10.2's table, §A11, E4/E5): one per
`called.outcome`, the tenth for a reuse, and the skipped call's — a function of the fold,
read by Prepare and the STA state test alike, stored nowhere.

**Remarks.** **Absent under `mechanical-only` with nothing supplied** (E5): `mode: mechanical-only`
is provenance on `opened`, never a compile-line string; the line exists only when a call
was made, reused, or skipped because the structure was already supplied.

| Member | Summary |
|---|---|
| `string? For(Envelope envelope, string structureSource = "", int historyTurns = 0, string tierRationale = "")` | The line for a fold: null when absent. |
| `string ForOutcome(string outcome, string? reason, string modelObserved, int toolCalls, int permissionRequests, string family, int historyTurns, string tierRationale)` | The string for one outcome — the table, parameterised on `reason` (§A11's STA test walks it). |
| `IReadOnlyList<string> Outcomes =` | Every outcome the table names, in §A10.2 order — the STA test's rows. |

### `string? For(Envelope envelope, string structureSource = "", int historyTurns = 0, string tierRationale = "")`

The line for a fold: null when absent.

- **`envelope`** — The fold.
- **`structureSource`** — Who filled the structure when the call was skipped: `operator` · `template`; empty when no line is supplied.
- **`historyTurns`** — How many prior turns the compile read (the history window's ids).
- **`tierRationale`** — The projection's rationale for a succeeded line.

## `DerivedDecoration`

*record* — `CompileOutputValidator.cs`

One derived decoration that passed the typed boundary (§A8.3's invariant, all five clauses).

## `ValidationResult`

*record* — `CompileOutputValidator.cs`

What the boundary made of one model output: the decorations that passed, what it dropped by
reason, the `notes` if they passed the same scan, and the `called.outcome` the
output earns (`succeeded` · `succeeded_no_structure` · `malformed`).

## `CompileOutputValidator`

*class* — `CompileOutputValidator.cs`

The non-determinism boundary (§A8.4; LOA 3.1 Deterministic Verifier): raw text → JSON → schema →
allow-list and open lines → type → mention scan (values and `notes`) → span resolution →
typed `DerivedDecoration`[]. Nothing downstream sees raw text.

**Remarks.** **C-Lease** (§A13.3 c): a value or `notes` carrying a mention token is refused —
not stripped — with `HasMention`, the same regex `Patterns`
matches, so the scan and the derivation cannot drift. A refused proposal is never stored, so
*restore* cannot resurface it.





**Every drop is counted on the reason it fell to**, in pipeline order — the first
failing clause names it — and the counts land on the `called` row (US-D5).

| Member | Summary |
|---|---|
| `ValidationResult Validate(string rawText, string sourceText, IReadOnlyList<string> openLines)` | Validates one raw model output against the open lines and the source text it must cite. |

### `ValidationResult Validate(string rawText, string sourceText, IReadOnlyList<string> openLines)`

Validates one raw model output against the open lines and the source text it must cite.

- **`rawText`** — The model's text, verbatim.
- **`sourceText`** — The `opened.source_text` spans resolve into (UTF-16 indices).
- **`openLines`** — The structure lines the prompt named as open — a proposal for any other line is dropped.

## `InstalledPinTriple`

*record* — `CompilePin.cs`

The pin triple as **installed** on this machine (ADR-0035 rule 2): the adapter's version
and `dist/acp-agent.js` sha, the SDK's version, and the sha of the CLI binary the SDK
vendors — the binary that enforces the pin. Every sha is over raw bytes; an element that cannot
be read is `NotRecorded`, never a plausible value.

## `RecordedPinTriple`

*record* — `CompilePin.cs`

What the recorded artifact says the triple was when the spike ran.

## `FrameRecount`

*record* — `CompilePin.cs`

A recount of `tool_call` / `tool_call_update` frames over a frame log — computed, never read from the artifact.

## `CompilePinArtifact`

*record* — `CompilePin.cs`

The gate-1 artifact as read: `compile-pin-spike.json`'s recorded triple and the frame log
it names (ADR-0036 Gate 1).

| Member | Summary |
|---|---|
| `string FileName = "compile-pin-spike.json"` | The artifact's file name — the same name the Proof Pack's citation copy carries under `docs/proof/`. |
| `string FrameLogFileName = "compile-pin-spike.frames.jsonl"` | The frame log's file name, beside the artifact (ADR-0036: machine-level, never a checkout's). |
| `string DefaultDirectory` | Where the product reads the gate artifacts: `~/.aide/proof/`, beside `~/.aide/providers.json` — the pin is about *this machine's* installed adapter, SDK and CLI, not about which workspace is open (ADR-0036's path-reso… |
| `CompilePinArtifact? Read(string path, out string? problem)` | Reads the artifact, or null when the file is absent. A malformed file reads as absent with its reason in . |

## `CompilePin`

*class* — `CompilePin.cs`

The pin as a check: the installed triple from the adapter install root, the recount over a
frame log, and the comparison with the recorded artifact — one reader for the settings model
(Gate 1) and the compile host (verified per call, ADR-0035 rule 1).

| Member | Summary |
|---|---|
| `string AdapterPackage = "@agentclientprotocol/claude-agent-acp"` | The adapter package whose entry module the catalog names. |
| `string SdkPackage = "@anthropic-ai/claude-agent-sdk"` | The SDK package the adapter runs on. |
| `string AdapterAgentFile = "acp-agent.js"` | The adapter file whose sha is pinned — the file that forwards `tools` and launches the CLI. |
| `string CliPlatformPackage { get; } = "claude-agent-sdk-" + Platform() + "-" + Arch()` | The platform package the SDK vendors its CLI in — `@anthropic-ai/claude-agent-sdk-<platform>-<arch>` (PD-5's second finding: not the machine's global `claude`). |
| `string CliFileName { get; } = OperatingSystem.IsWindows() ? "claude.exe" : "claude"` | The CLI binary's file name on this platform. |
| `InstalledPinTriple Installed(string adapterInstallRoot)` | Reads the installed triple from the adapter install root's own bytes. |
| `string? Mismatch(InstalledPinTriple installed, RecordedPinTriple recorded)` | Compares the installed triple with the recorded one: null when every element matches, else the elements that differ, named — the reason the settings model and the compile host show. |
| `FrameRecount Recount(ReadOnlySpan<byte> frameLog)` | Recounts tool-call frames over a frame log's raw bytes (LLM-free; ADR-0036 Gate 1). |
| `string Sha256(string path)` | sha256 over the file's raw bytes, lowercase hex; `NotRecorded` when the file cannot be read. |

## `ConstitutionRef`

*record* — `CompilePromptAssembler.cs`

One constitution document, by identity and hash only — never its body (§A12.3).

## `CompilePromptInputs`

*record* — `CompilePromptAssembler.cs`

What the compile prompt is assembled from (§A8.3): the source text, the mechanical facts the
model may not contradict (as display), the open lines, the profile body, the history window's
admissible classes and the constitution by reference. Never an account label, never an
attachment body, never lane output.

## `CompilePromptAssembler`

*class* — `CompilePromptAssembler.cs`

Assembles the `compile-prompt/1` text from host-embedded bytes and typed inputs, and
computes the two hashes the `called` row records: `PromptSha` over the
template's bytes and `InputsSha` over the canonical inputs (§A8.4).

| Member | Summary |
|---|---|
| `string PromptSha { get; } = EnvelopeHash.Sha256Hex(CompileContract.TemplateBytes)` | sha256 over the host header and template bytes — a wording edit to the prompt changes it, a workspace file never does. |
| `string Assemble(CompilePromptInputs inputs)` | The prompt, first bytes the host header. |
| `string InputsSha(` | `sha256(canonical(source_text ‖ the mechanical facts sorted by name ‖ open lines ‖ profile {family, version, sha} ‖ history ids ‖ contract_version ‖ prompt_sha))` — the same inputs yield the same sha, so a re-prepare … |

## `EnvelopeIds`

*class* — `CompileSignal.cs`

Mints envelope ids: sortable (a UTC timestamp) and collision-resistant (CSPRNG entropy), the session id's own shape.

| Member | Summary |
|---|---|
| `string New(DateTimeOffset? now = null)` | A new id, `yyyyMMddTHHmmssfffZ-xxxxxxxx`. |

## `CompileEventKinds`

*class* — `CompileSignal.cs`

The `compile.*` run-event vocabulary (spec Addendum D §A10.3; ADR-0033 rule 6): kinds added
to the open vocabulary `Kind` accepts, emitted to the Console stream and the
profiler — an emission, never a second store. This slice emits `compile.degraded`; the
stage rows arrive with the agentic rung.

| Member | Summary |
|---|---|
| `string Degraded = "compile.degraded"` | `compile.degraded{reason, error_code}`. |
| `string Stage = "compile.stage"` | `compile.stage{stage, duration_ms, outcome}` — one per stage (§A10.3). |
| `string Origin = "compile"` | The `Ext.origin` value every compile event carries — origin is never inferred from `RunId` / `AgentId` (a type pun). |

## `CompileSignal`

*class* — `CompileSignal.cs`

The compile step's instrumentation on the normal path (IO1–IO12): every fact is an attribute on
an `aide.compile` activity (OpenTelemetry is the data model). The Console-stream translator
(`compile.stage` rows with `Ext.origin = "compile"`) is the agentic rung's, written
with its first emitter (CV-3) — the mechanical rung records no stage.

| Member | Summary |
|---|---|
| `string SourceName = "aide.compile"` | The activity source every compile-step span is published on. Observed by `CompileSignalTests`. |
| `void Stage(string stage, long durationMs, string outcome)` | Records one stage's timing and outcome: `compile.stage{stage, duration_ms, outcome}`; a stage that did not run is never recorded as 0. |
| `void Degraded(string reason, string errorCode)` | Records a degraded state: `compile.degraded{reason, error_code}`. |
| `bool IsCompileEvent(RunEvent evt)` | Whether a Console-stream event came from the compile step — by `Ext.origin`, never by its ids. |

## `Envelope`

*class* — `Envelope.cs`

The **Envelope** — the aggregate root of Prompt Compilation: the fold of its events (§A6).
Opened by the Send gesture, closed by `submitted` + `consumed`; an envelope with no
accepted `submitted` is abandoned.

**Remarks.** **One invariant:** it only grows — every event carries a `seq` strictly greater
than every event before it, no event is mutated or removed, and no `decorated` follows an
accepted `submitted`. The writer enforces it (`Append`); the fold
reads the rows in `seq` order and never re-sorts them by source.





**Derive, don't store (DM7).** The shape, the tier and its rationale, the effective
fan-out, the CT19 block, the lease and every count are `Project`'s over
this fold; nothing here is a stored copy of one. `Current`, `Confirmed`
and `EffectiveMode` have one definition each, read by Prepare, Submit and the eval
alike (DM11 b).

| Member | Summary |
|---|---|
| `string NotRecorded = "not recorded"` | The word for an absent measurement or identity — never a plausible substitute (IO12). |
| `string EnvelopeId { get; }` | The envelope's id. |
| `IReadOnlyList<EnvelopeEvent> Events` | Every event, in `seq` order. |
| `Opened? Opened` | The `opened` row, or null when the fold holds none (an incomplete envelope — `Project` refuses it). |
| `IEnumerable<Decorated> Decorations` | Every decoration, in `seq` order. |
| `Called? LastCall` | The last `called` row, or null when no call was made. |
| `Submitted? Submitted` | The one accepted `submitted`, or null. |
| `Consumed? Consumed` | The `consumed` row, or null. |
| `bool IsAbandoned` | Abandoned: no accepted `submitted`. A stable read, because no writer holds the file while a reader folds it. |
| `string? Outcome` | The run's outcome: the `consumed` row's word; `NotRecorded` for an accepted `submitted` with no `consumed`; null for an envelope that was never submitted. |
| `Decorated? Current(string name)` | `Current(name)`: the `decorated` event with the highest `seq` for that name (§A6). |
| `Decorated? Confirmed(string name)` | `Confirmed(name)`: `Current` ignoring `derived` rows when `opened.compile_mode = agentic-advisory` — the one definition the shape, the tier rule's P and the CT19 projection all read, so an unkept derived line is blank… |
| `string EffectiveMode` | `EffectiveMode`: `agentic` iff `opened.compile_mode ∈ {agentic-advisory, agentic}` and the last `called.outcome ∈ {succeeded, succeeded_no_structure, suspect}`; else `mechanical`. A function of the fold, never a store… |
| `IReadOnlyList<Envelope> Fold(IEnumerable<EnvelopeEvent> events)` | Folds events into envelopes, grouped by id, each in `seq` order. Every event must carry a positive `seq` — the writer's, or `Pending`'s for a live pre-compile. |
| `Envelope Pending(IReadOnlyList<EnvelopeEvent> events)` | The live, in-memory envelope the pre-compile builds and persists nothing of (§A10.1): the events numbered 1..n in order, folded. The same `Fold` the store's rows take, so what Prepare renders before Send and what the … |

## `EffectiveModes`

*class* — `Envelope.cs`

The effective compile mode after degradation — a projection's vocabulary, never a stored decoration.

| Member | Summary |
|---|---|
| `string Mechanical = "mechanical"` | **(gap)** |
| `string Agentic = "agentic"` | **(gap)** |

## `EnvelopeFold`

*record* — `Envelope.cs`

What a reader folded from one file: the envelopes it could read, the rows it skipped and
counted, and where the chain broke (DM11 g).

| Member | Summary |
|---|---|
| `string Report` | The reader's report, in one sentence — *record broken at line N* when it is. |
| `DateTimeOffset? NewestAt` | The newest `at` across every readable row, or null when none. |
| `Envelope? Find(string envelopeId)` | One envelope by id, or null. |

## `EnvelopeEvent`

*record* — `EnvelopeEvents.cs`

One event on one envelope — the stored fact of the Prompt Compilation bounded context
(spec Addendum D §A6; ADR-0034). Five kinds: `opened · decorated · called · submitted · consumed`.

**Remarks.** **The grain (DM8):** one row in `envelope-events.jsonl` is exactly one event on one
envelope, identified by `(envelope_id, seq)`, recorded when the event occurs. `Seq`
and `At` are assigned by the writer at `Append` (`EnvelopeStore`), or
by `Pending` for the live, in-memory pre-compile that persists nothing; a
caller that supplies a `Seq` of its own is checked against the envelope's last, never
trusted.





**Immutable by construction.** A record is never mutated or removed; a later event
supersedes an earlier one by `seq` (the fold's `Current`). The pattern
is *Event Store* (the envelope is the fold), named as ADR-0034's LOA mapping names it.

| Member | Summary |
|---|---|
| `string Kind { get; }` | The event's kind — one of `EnvelopeEventKinds`. |
| `int Seq { get; init; }` | Per-envelope ordinal, strictly increasing; 0 until the writer assigns it. |
| `DateTimeOffset? At { get; init; }` | When the event was recorded — the append is the moment; null until then. |

## `EnvelopeEventKinds`

*class* — `EnvelopeEvents.cs`

The five kinds, as the row's `kind` member spells them.

| Member | Summary |
|---|---|
| `string Opened = "opened"` | **(gap)** |
| `string Decorated = "decorated"` | **(gap)** |
| `string Called = "called"` | **(gap)** |
| `string Submitted = "submitted"` | **(gap)** |
| `string Consumed = "consumed"` | **(gap)** |

## `CompileConstants`

*record* — `EnvelopeEvents.cs`

The rule's constants an `opened` row pins so a change never rewrites history (§A12.4, IO7).

## `Opened`

*record* — `EnvelopeEvents.cs`

The envelope opened — by the Send gesture, on a draft with no fresh envelope (§A10.1).

| Member | Summary |
|---|---|
| `string Kind` | **(gap)** |

## `DecorationInput`

*record* — `EnvelopeEvents.cs`

Who wrote a mechanical decoration's value — named on the row, so no compile-time default hides behind it (F-6).

## `GroundedSpan`

*record* — `EnvelopeEvents.cs`

A span a derived decoration cites — offsets are UTF-16 indices into the raw `opened.source_text` (§A8.3 (iii)).

## `Decorated`

*record* — `EnvelopeEvents.cs`

One immutable, named, sourced claim about this turn (§A6): `{name, value, source}`.

| Member | Summary |
|---|---|
| `string Kind` | **(gap)** |
| `IReadOnlyList<DecorationInput>? Inputs { get; init; }` | The writer(s) of a mechanical snapshot or a template-supplied line; null otherwise. |
| `int? CallSeq { get; init; }` | The `called` row a derived decoration came from; null unless `Source` is `derived`. |
| `double? Confidence { get; init; }` | The model's self-reported confidence in [0, 1]; derived only. |
| `IReadOnlyList<GroundedSpan>? GroundedIn { get; init; }` | The spans a derived decoration cites; derived only. |
| `int? Bytes { get; init; }` | The history window's byte size; `history_window` only. |
| `string? ValueAsString` | The value as a string, or null when it is absent or not a string (a blank reads as absent, as `SpawnContract.Validate` reads a field). |

## `DecorationSources`

*class* — `EnvelopeEvents.cs`

The provenance of a decoration — `source` is provenance, never precedence (§A6 Supersession).

| Member | Summary |
|---|---|
| `string Mechanical = "mechanical"` | The pre-compile wrote it, deterministically. |
| `string Derived = "derived"` | The bound model proposed it, through the typed boundary (CV-3's rung). |
| `string Operator = Watcher.TaskClasses.Sources.Operator` | The operator wrote or overrode it in Prepare, or typed it as a structure line — one spelling with the cohort column's (`Sources`). |
| `string SessionDefault = Watcher.TaskClasses.Sources.SessionDefault` | The session's default, snapshotted for this prompt (the `task_class` row, Ruling 70) — one spelling with the cohort column's. |

## `DecorationNames`

*class* — `EnvelopeEvents.cs`

The decoration names this slice writes or reads — one spelling each (DM7).

| Member | Summary |
|---|---|
| `string Goal = GoalBlockFields.GoalKey` | **(gap)** |
| `string DoneWhen = GoalBlockFields.DoneWhenKey` | **(gap)** |
| `string NotInScope = GoalBlockFields.NotInScopeKey` | **(gap)** |
| `string Tier = GoalBlockFields.TierKey` | **(gap)** |
| `string TaskClass = "task_class"` | **(gap)** |
| `string Ceilings = "ceilings"` | **(gap)** |
| `string FamilyProfile = "family_profile"` | **(gap)** |
| `string TemplateApplied = "template_applied"` | **(gap)** |
| `string Attachments = "attachments"` | **(gap)** |
| `string HistoryWindow = "history_window"` | **(gap)** |
| `string Constitution = "constitution"` | **(gap)** |
| `IReadOnlyList<string> StructureLines = [Goal, DoneWhen, NotInScope]` | The three structure lines, in §14.3 order. |
| `IReadOnlyList<string> NeverOperator =` | Names an `operator` row may never carry (US-D1): the settings have one home, and a projection is never stored. Checked at `Append`. |

## `DroppedCounts`

*record* — `EnvelopeEvents.cs`

What the typed boundary dropped from one model output, by reason (§A8.3).

| Member | Summary |
|---|---|
| `DroppedCounts None = new(0, 0, 0, 0, 0)` | **(gap)** |
| `int Total` | **(gap)** |

## `Called`

*record* — `EnvelopeEvents.cs`

One invocation of the bound model by the compile stage — the grain at which cost exists (§A6).
Written by the agentic rung (`ComposerSendGate.PrepareAsync` from a `CompileResult`).

| Member | Summary |
|---|---|
| `string Kind` | **(gap)** |

## `CallOutcomes`

*class* — `EnvelopeEvents.cs`

The `called.outcome` vocabulary (§A10.2).

| Member | Summary |
|---|---|
| `string Succeeded = "succeeded"` | **(gap)** |
| `string SucceededNoStructure = "succeeded_no_structure"` | **(gap)** |
| `string Suspect = "suspect"` | **(gap)** |
| `string Unavailable = "unavailable"` | **(gap)** |
| `string Refused = "refused"` | **(gap)** |
| `string TimedOut = "timed_out"` | **(gap)** |
| `string Malformed = "malformed"` | **(gap)** |
| `string Cancelled = "cancelled"` | **(gap)** |
| `string Reused = "reused"` | A re-prepare with an unchanged `inputs_sha` after a success reused the stored derived decorations and made no request — a `called` row with a known-zero cost and `reason` naming `reused_from` (ADR-0035 rule 1: C3/C10'… |
| `IReadOnlyList<string> Agentic = [Succeeded, SucceededNoStructure, Suspect, Reused]` | The outcomes `EffectiveMode` counts as agentic. |

## `Submitted`

*record* — `EnvelopeEvents.cs`

The Send gesture's confirmation: accepted, or refused with a code — at most one **accepted**
`submitted` per envelope (US-D6/US-D7).

| Member | Summary |
|---|---|
| `string Kind` | **(gap)** |

## `Consumed`

*record* — `EnvelopeEvents.cs`

The run consumed the envelope: the pair `(outcome, reason)` (E6), by identity only.

| Member | Summary |
|---|---|
| `string Kind` | **(gap)** |

## `ConsumedReasons`

*class* — `EnvelopeEvents.cs`

The `consumed.reason` codes (E6).

| Member | Summary |
|---|---|
| `string Completed = "completed"` | **(gap)** |
| `string StoppedByOperator = "stopped_by_operator"` | **(gap)** |
| `string DocumentClosed = "document_closed"` | **(gap)** |
| `string LaneExited(int? code)` | `lane_exited{code}`, rendered with the exit code — or `lane_exited` alone when none was recorded. |

## `PurgePlan`

*record* — `EnvelopePurge.cs`

What `aide session purge <id>` will do, resolved and read before anything is touched
— the confirmation names an identity, never a count alone (DC-120; US-D13).

| Member | Summary |
|---|---|
| `bool HasHistory` | Whether there is any history to purge: a file with at least one line (an empty file is the store's own handle, not history). |
| `string Describe()` | The confirmation, one line per fact. |

## `EnvelopePurge`

*class* — `EnvelopePurge.cs`

Deletes a session's `envelope-events.jsonl` and nothing else (ADR-0034 rule 6; §A13.5 rule 1).
`session.json`, `session-events.jsonl`, the layout files and `runs/` belong to
the Session aggregate and its own command.

| Member | Summary |
|---|---|
| `PurgePlan Resolve(string workspaceRoot, string sessionId)` | Resolves the plan: validates the id as one segment, resolves it under `<workspace>/.aide/sessions/` without following a junction or symlink, and reads the file — refused visibly while a writer holds it. |
| `void Execute(PurgePlan plan)` | Removes the one file the plan names. A file already absent is not an error. |

### `PurgePlan Resolve(string workspaceRoot, string sessionId)`

Resolves the plan: validates the id as one segment, resolves it under
`<workspace>/.aide/sessions/` without following a junction or symlink, and reads
the file — refused visibly while a writer holds it.

**Throws `EnvelopeStoreException`.** `PurgeRefused` before any file is touched; `HeldByAnotherWriter` while a composer holds the file.

### `void Execute(PurgePlan plan)`

Removes the one file the plan names. A file already absent is not an error.

**Throws `EnvelopeStoreException`.** `HeldByAnotherWriter` when a composer opened it since the plan was read.

## `EnvelopeHash`

*class* — `EnvelopeRowCodec.cs`

The one hash the store, the projection and the rebuild share: sha256 as lowercase hex over UTF-8.

| Member | Summary |
|---|---|
| `string Sha256Hex(string text)` | sha256 of 's UTF-8 bytes, lowercase hex. |
| `string Sha256Hex(ReadOnlySpan<byte> bytes)` | sha256 of raw bytes, lowercase hex. |

## `EnvelopeStoreOpenReport`

*record* — `EnvelopeStore.cs`

What one open observed — emitted as `envelope-store.open` on the normal path (ADR-0034 rule 3; IO2).

## `EnvelopeStore`

*class* — `EnvelopeStore.cs`

The compiled-envelope store: one append-only, exclusively written, sha-chained JSONL sidecar per
session — `<workspace>/.aide/sessions/<id>/envelope-events.jsonl` (ADR-0034).

**Remarks.** **Append and a reader, nothing else.** No update, delete or rewrite member exists, and a
reflection test asserts the surface by name. The file is opened `FileShare.None` for the
life of this object: a second writer, and a reader while a writer holds it, are refused visibly
(DM11 h) — which is also what makes *no `submitted` = abandoned* a stable read.





**The walk at open is schema-agnostic and the fold is not.** Every line's
`(envelope_id, seq)` and raw bytes are read regardless of `schema` or `kind`, so a
rolled-back `/1` binary opening a file with `/2` rows still mints a unique, higher
`seq` and an unbroken chain; only the fold skips what it cannot read, and counts it. A chain
broken at line N refuses `Append` for the life of the open and leaves the bytes for
inspection — refuse, not rotate: a rotated file would be a second history nothing reads.





**Patterns:** Event Store (the envelope is the fold), Hash Chain for corruption detection
— not tamper evidence (Security T1/T2), Projection (`Read`, the eval).

| Member | Summary |
|---|---|
| `string Schema = "compiled-envelope/1"` | The row schema this writer writes and this reader folds. |
| `string FileName = "envelope-events.jsonl"` | The sidecar's file name, a sibling of `session.json`. |
| `string Path { get; }` | The file this store holds exclusively. |
| `string SessionId { get; }` | The session id — the directory segment, which every `opened` row must match. |
| `int? BrokenAt { get; }` | The line the chain broke at, or null; when set, `Append` is refused for the life of this open. |
| `EnvelopeStoreOpenReport OpenReport { get; }` | What the open measured (bytes, rows, envelopes, broken_at, walk_ms). |
| `EnvelopeStore Open(string sessionDirectory, TimeProvider? time = null)` | Opens the session's store exclusively, walking the whole chain first (off the caller's UI thread is the caller's job; the walk is measured either way). |
| `EnvelopeFold ReadFile(string path)` | The reader for purge, export and the eval: folds a file that no writer holds — refused visibly otherwise, never a partial fold. |
| `EnvelopeEvent Append(EnvelopeEvent evt)` | Appends one event: assigns `seq` (one greater than that envelope's last) and `prev_sha` (over the raw previous line, any schema), stamps `at`, and refuses anything that would break the aggregate's one invariant. |
| `EnvelopeFold Read()` | The fold over every row this open has read or written (DM11 g's defensive fold). |
| `void Dispose()` | Releases the exclusive handle. A second open succeeds afterwards. |

### `EnvelopeStore Open(string sessionDirectory, TimeProvider? time = null)`

Opens the session's store exclusively, walking the whole chain first (off the caller's UI
thread is the caller's job; the walk is measured either way).

- **`sessionDirectory`** — The session's directory; its last segment is the session id. Never created here.
- **`time`** — The clock rows are stamped from.

**Throws `EnvelopeStoreException`.** `NoSessionDirectory`; `HeldByAnotherWriter`.

### `EnvelopeEvent Append(EnvelopeEvent evt)`

Appends one event: assigns `seq` (one greater than that envelope's last) and
`prev_sha` (over the raw previous line, any schema), stamps `at`, and refuses
anything that would break the aggregate's one invariant.

**Returns.** The event as written — with its `seq` and `at`.

**Throws `EnvelopeStoreException`.** One of the `EnvelopeStoreErrorCodes`; the file is unchanged on every refusal.

## `EnvelopeStoreErrorCodes`

*class* — `EnvelopeStoreException.cs`

Stable error codes for the envelope store and the projections over it (the `CE-` family; O-standard: every failure carries one).

| Member | Summary |
|---|---|
| `string NoSessionDirectory = "CE-0001"` | The session directory does not exist — the store never creates the Session aggregate's directory. |
| `string HeldByAnotherWriter = "CE-0002"` | Another writer holds the file (`FileShare.None`): a second AI-DE, or a composer while a reader asks. |
| `string StoreBroken = "CE-0003"` | The chain is broken at line N; `Append` is refused for the life of this open. |
| `string SessionIdMismatch = "CE-0004"` | An `opened` row's `session_id` differs from the directory segment. |
| `string SeqNotIncreasing = "CE-0005"` | A supplied `seq` is not the envelope's next. |
| `string DecoratedAfterSubmitted = "CE-0006"` | A `decorated` after an accepted `submitted`. |
| `string SubmittedTwice = "CE-0007"` | A second accepted `submitted` on one envelope. |
| `string ConsumedTwice = "CE-0008"` | A second `consumed` on one envelope. |
| `string AttachmentBodyRefused = "CE-0009"` | An attachment value carrying a `body` member — bodies are never persisted (§A13.5). |
| `string DecorationNameRefused = "CE-0010"` | A decoration named `lease` (DM-A), an `operator` row named a setting, or an unknown source. |
| `string TierRefused = "CE-0011"` | A `tier` row outside {T0, T1, T2} (§A9 R4's falsifier). |
| `string NotOpened = "CE-0012"` | An event on an envelope with no `opened` row, or a second `opened`. |
| `string AppendFailed = "CE-0013"` | The append's write failed after a good open (disk full, an IO error) — the send proceeds degraded. |
| `string PurgeRefused = "CE-0014"` | A purge was refused before any file was touched: a traversal, a non-segment id, a junction. |
| `string ProjectionIncomplete = "CE-0015"` | A fold is incomplete for projection: no `opened` row, or no well-formed `ceilings` row. |
| `string PinArtifactMissing = "CE-0016"` | Gate 1: the compile-pin-spike artifact is absent (or unreadable) — no agentic rung is selectable (ADR-0036; US-D11 b1). |
| `string PinTripleMismatch = "CE-0017"` | Gate 1: the artifact's recorded adapter/SDK/CLI triple is not the installed one — an adapter bump under a stale artifact. |
| `string PinFrameLogUnverifiable = "CE-0018"` | Gate 1: the artifact names no frame log, or the frame log is missing or does not hash to the recorded sha — the count cannot be recounted. |
| `string PinRecountNotZero = "CE-0019"` | Gate 1: the recount over the frame log is not zero — the pin did not hold on the recorded run. |
| `string AdmissionReportOutstanding = "CE-0020"` | Gate 2: no admission report has been read — `agentic` is refused by name until CV-4's reader admits it. |
| `string CompileModeUnknown = "CE-0021"` | A `compile_mode` word outside the three the ladder names. |
| `string PinRunNotEnded = "CE-0022"` | Gate 1: the artifact records a run that did not end (`mode` not `full`, or a prompt with no result) — an aborted or timed-out spike admits nothing. |
| `string PinIdentityMismatch = "CE-0023"` | Gate 1: the artifact's `sent_meta_triple` is not the pin this build sends — a pin change re-runs PD-5 by construction. |

## `EnvelopeStoreException`

*class* — `EnvelopeStoreException.cs`

A refusal by the envelope store, with its stable code (`EnvelopeStoreErrorCodes`).

| Member | Summary |
|---|---|
| `EnvelopeStoreException(string code, string message, Exception? inner = null)` | **(gap)** |
| `string Code { get; }` | The stable code. |

## `PreCompileInput`

*record* — `PreCompile.cs`

Everything the mechanical pre-compile reads — the draft, the binding and the session's settings, with no live setting read later (ADR-0033 rule 3).

## `PreCompile`

*class* — `PreCompile.cs`

The mechanical rung of the compile step (§A8.1, T0): a pure, total, deterministic function of
the draft and the settings that yields the envelope's mechanical rows — never a model call,
never a clock in a value, never a file read (US-D2).

**Remarks.** **Runs in memory, persists nothing** (§A10.1): the live pre-compile on the debounced
draft yields `Live`, a pending envelope Prepare renders from; the Send gesture yields
`Open`, the same rows for the store to append. One function, two callers, so what
Prepare showed and what the store holds cannot differ.





**What it writes, and what it never writes.** The `ceilings` snapshot with its
writer named on `inputs`; the `task_class` row (`session-default` or
`operator`); the family-profile selection (`none` until the pack ships one — ADR-0037's
dimension is read, not populated, here); the template applied; the attachments by reference;
the operator's structure lines as `operator` rows; the operator's tier override. It never
writes a lease, a shape, a rule-computed tier, an effective fan-out or a count — those are
`Project`'s (DM7; US-D1).

| Member | Summary |
|---|---|
| `string ProjectorVersion = "1"` | The projector version `submitted.projector_version` records and `ConstantsFor` is keyed by. |
| `string CeilingsWriter = "session.config"` | The writer the `ceilings` snapshot names: the session's settings (S2's home, Ruling 56). |
| `string TemplateWriter = "template"` | The writer a template-supplied structure line names. |
| `string LiveEnvelopeId = "live"` | The envelope id the live, in-memory pre-compile carries — never persisted. |
| `CompileConstants ConstantsFor(string projectorVersion)` | The rule's constants for a projector version — a host-compiled table, never read from the workspace (§A12.4: K and the byte bound are initial values labelled Inferred; a change is a new version, so history never rewri… |
| `Envelope Live(PreCompileInput input)` | The live envelope Prepare renders from — the same rows `Open` yields, numbered, persisted nowhere. |
| `IReadOnlyList<EnvelopeEvent> Open(PreCompileInput input, string envelopeId, string? supersedes = null)` | The rows the Send gesture appends: `opened`, then the mechanical decorations, then the operator's rows. |

### `IReadOnlyList<EnvelopeEvent> Open(PreCompileInput input, string envelopeId, string? supersedes = null)`

The rows the Send gesture appends: `opened`, then the mechanical decorations, then the operator's rows.

- **`input`** — The inputs.
- **`envelopeId`** — The envelope being opened.
- **`supersedes`** — The abandoned envelope this one re-prepares, or null.

## `CompiledProjection`

*record* — `Projection.cs`

`Project(Fold(events))`: the shape, the tier with its rationale, the effective fan-out, the
CT19 block, the lease, the prompt, the task class and the rebuild's witness — computed at every
render and at Submit, stored nowhere (§A6; §A12.2).

| Member | Summary |
|---|---|
| `bool IsReadOnly` | Ruling 73's access projection: read-only when no lease was derived. |
| `string? Prompt` | The sent bytes, when rendered. |

## `Projection`

*class* — `Projection.cs`

The one projection of the Prompt Compilation context — **the sole function that assembles what
`ComposerSendGate.Send` puts into `GovernedRunRequest`**, serving every render and
the eval's `aide compile fold` alike (ADR-0033 rule 2; DM11 b).

**Remarks.** **Exactly three call sites by path**, asserted by a census: the composer's render
(`Presentation/Composer/ComposerCompiler.cs`), the send gate
(`Workbench/Composer/ComposerSendGate.cs`) and the eval's verb (`Cli/CompileFold.cs`).
A fourth producer of the sent bytes is the failure this shape exists to make visible.





**The lease is derived here, from `opened.source_text`, and nowhere else in the
product** — the one `LeaseDerivation.Derive` call (Ruling 66; §A13.3 d′). A stored lease
would be one quantity with two homes (DM-A).





**It never reads a live setting** (ADR-0033 rule 3): the ceilings come from the
`ceilings` row the pre-compile snapshotted, so the rebuild is possible offline with no
session open. A fold with no `opened` row or no well-formed `ceilings` row is
*incomplete* and refused with `ProjectionIncomplete`.





Patterns: **Projection / CQRS read model**; the mapping onto `GovernedRunRequest` in
the send gate is an **Anti-Corruption Layer** to the agent plane's Published Language.

| Member | Summary |
|---|---|
| `string Version = PreCompile.ProjectorVersion` | The projector's version — `submitted.projector_version`. |
| `CompiledProjection Project(Envelope envelope, ComposerDraft? draft = null, PromptTemplate? template = null)` | Projects one envelope. With a  the prompt is rendered from the held bodies; without one (the offline rebuild) the prompt is null and only the rebuildable domain is computed. |

### `CompiledProjection Project(Envelope envelope, ComposerDraft? draft = null, PromptTemplate? template = null)`

Projects one envelope. With a  the prompt is rendered from the held
bodies; without one (the offline rebuild) the prompt is null and only the rebuildable
domain is computed.

**Throws `EnvelopeStoreException`.** `ProjectionIncomplete`.

## `StructureSources`

*class* — `Projection.cs`

Who filled the structure — the rationale's `structure_source` (§A9).

| Member | Summary |
|---|---|
| `string Operator = "operator"` | **(gap)** |
| `string Derived = "derived"` | **(gap)** |
| `string Template = "template"` | **(gap)** |
