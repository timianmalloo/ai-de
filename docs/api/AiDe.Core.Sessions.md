---
id: api-aide-core-sessions
title: "API: AiDe.Core.Sessions"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Sessions: 23 types, 60 members, 88% carrying a summary doc comment.
---

# API: `AiDe.Core.Sessions`

**23 public types · 60 public members · 88% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `SessionConfig`

*record* — `SessionConfig.cs`

The user-facing container Addendum A3 defines: named, workspace-bound, and carrying
session-scoped config (Phase 1: which agent backends are enabled). R14 b2: "session" in this
namespace names only this container — never a Watcher/Dispatch/Terminal-internal concept.

| Member | Summary |
|---|---|
| `bool AttachEnabled { get; init; }` | Whether this session may attach files to a composed prompt (Security/Privacy **C21**). **Off by default**, confirmed by the human on 2026-09-10. |

### `bool AttachEnabled { get; init; }`

Whether this session may attach files to a composed prompt (Security/Privacy **C21**).
**Off by default**, confirmed by the human on 2026-09-10.

**Remarks.** **It gates the attach path — not egress, not send, not paste.** Gating all model
egress or the composer's send would disable the product's core function and be dishonest in a
specific way: the agent CLI is a separate process the operator launches from a terminal
anyway, so switching off this app's send does not stop the egress, it routes around it. Attach
is the honest line because it is the only path that puts bytes into the prompt the operator
did not type.





**An init-only property rather than a positional parameter** so a
`session.json` written before this field existed still reads, and reads false — which is
the safe state, not merely the convenient one.





**"Off" is distinguishable from "never asked" by the EXISTING event log, and no
provenance field is added here.** False with no `session.config` event naming it is the
shipped default; with one, an operator decided. The append-only log is already the record, and
two definitions of one fact is a defect signature — so there is deliberately no
`source` or `decidedAt` member on this type.





**Honest limit, stated here rather than discovered later.** This record is
per-session and operator-writable, so this is **a default with a safe initial state, not an
enforceable policy**. A deployment that must *prevent* attach needs a
non-session-overridable layer, which is Phase 2 — named here as the upgrade trigger. Nothing
may describe this field as restricting or preventing attach for a deployment.

## `SessionEventKinds`

*class* — `SessionConfig.cs`

The `kind` strings this slice adds to the open, unenumerated vocabulary
`Kind` already accepts (clause 4). Defined here, in the
container's own namespace, rather than in `AgentPlane` — these are session-level events, not
run events, and F0 does not touch `AgentPlane.RunEvent` at all: proving it needs no change
IS the clause.

| Member | Summary |
|---|---|
| `string Open = "session.open"` | A session was created (`Create`). |
| `string Config = "session.config"` | A session's config changed — currently only `EnabledBackends` toggles. |

## `SessionEvent`

*record* — `SessionConfig.cs`

One line of a session's append-only `session-events.jsonl` (clause 3's "emit a session
event"). Deliberately its own small type rather than `RunEvent`: a session
event has no run id and no agent id, so forcing it into that shape would mean populating fields
that do not apply. Clause 4's obligation — that the future run-event stream accepts
`SessionEventKinds` with no schema change — is proven directly against
`RunEvent` in `SessionEventEnvelopeTests`, not by round-tripping this
type through it.

## `SessionOrigins`

*class* — `SessionConfig.cs`

How a session came to exist, as the `session.open` body records it (F5 clause 1).

**Remarks.** **On the event, never on `SessionConfig`.** Origin is a fact about one
moment — the creation — not a property of the container, and the append-only log is already the
record of what happened. Putting it on the record too would be two definitions of one fact, which
is the defect signature DM's "derive don't store" names.





**`Direct` is the default because the claim is about the OTHER value.**
"Started from File → New Session" is only checkable if something that did not start there reads
differently. A field that is always `MainMenuNewSession` would satisfy the sentence
and prove nothing — the exact "asserted-about" shape N7 was blocked for. So
`Create` defaults to `Direct` and exactly one caller
in `src/` passes `MainMenuNewSession`.

| Member | Summary |
|---|---|
| `string MainMenuNewSession = "main-menu.new-session"` | The `File → New Session` command path — `Ctrl+N` and the `MainMenuBuilder` entry, both of which resolve to `WorkbenchCommandCatalog`'s `session.new`. |
| `string Direct = "direct"` | Anything that created a session without going through that command. |

### `string MainMenuNewSession = "main-menu.new-session"`

The `File → New Session` command path — `Ctrl+N` and the `MainMenuBuilder`
entry, both of which resolve to `WorkbenchCommandCatalog`'s `session.new`.

**Remarks.** **Passed at exactly one site in `src/`**, `NewSessionSheetViewModel.Create` —
the sheet that command opens, and the only production caller of
`Create`. `TheSessionOriginIsSetOnlyOnTheCommandPathTests`
is the scan that holds that to one site.

## `SessionConfigStore`

*class* — `SessionConfigStore.cs`

Reads and writes one session's `session.json` and `session-events.jsonl` (clauses 1-3).

**Remarks.** **Toggles apply to new runs only (clause 3).** `SessionConfig` is an
immutable record; `SetEnabledBackends` never mutates an existing instance, it writes a
new one. A caller that already captured a `SessionConfig` (modelling a run reading its
config at start) is holding a value a later toggle cannot reach — proven in
`SessionConfigStoreTests.SetEnabledBackends_NeverMutatesAConfigARunAlreadyCaptured`. The
append-only event log gives the same guarantee one layer down, at the persisted bytes: earlier
lines are never rewritten (`SessionEventsFile_EarlierEventsSurviveByteForByteAfterALaterToggle`).






Idiom matches `Health.HealthIncidentSidecar`: a single lock around read-modify-write,
plain `System.Text.Json`, tolerant JSONL reads.

| Member | Summary |
|---|---|
| `SessionConfigStore(string workspaceRoot, string sessionId)` | **(gap)** |
| `string WorkspaceRoot { get; }` | **(gap)** |
| `string SessionId { get; }` | **(gap)** |
| `SessionConfig Create(` | Creates the session: writes `session.json` and emits `session.open`. |
| `SessionConfig Load()` | The current, live config — what a NEW run would pick up. |
| `SessionConfig SetEnabledBackends(IReadOnlyList<string> enabledBackends, DateTimeOffset now)` | Applies a backend toggle for new runs and emits `session.config`. Never mutates a `SessionConfig` a caller already holds — see the remarks on this type. |
| `SessionConfig SetAttachEnabled(bool attachEnabled, DateTimeOffset now)` | Applies the attach toggle for new runs and emits `session.config` (C21). |
| `IReadOnlyList<SessionEvent> ReadEvents()` | Every event this session has ever emitted, in append order. |

### `SessionConfig Create(`

Creates the session: writes `session.json` and emits `session.open`.

- **`name`** — The operator-facing name.
- **`workspaceId`** — The workspace this session is bound to.
- **`enabledBackends`** — The agent backends enabled for it.
- **`now`** — Stamps the config and the event.
- **`origin`** — How this session came to exist — see `SessionOrigins`. **Defaulted to `Direct` deliberately:** F5 clause 1's claim is that the front door is distinguishable from every other way of reaching this method, and that is only checkable if something which did not come through it reads differently. The default is what makes the other value evidence.

### `SessionConfig SetAttachEnabled(bool attachEnabled, DateTimeOffset now)`

Applies the attach toggle for new runs and emits `session.config` (C21).

**Remarks.** **Through the store, exactly like the backend toggle.** C21 needs no Settings surface
and invents no config concept: it is a new field on an existing record, written with an
existing event kind, by the same read-modify-write under the same lock.





**Host-owned and unreachable from the page (C21(e)).** No page-to-host kind reads or
writes it and none may be added — the page may be *told* the state so it can render a
disabled affordance; it may never *report* it. The asymmetry is deliberate: the composer
may not carry a dial that loosens governance, and this one only restricts.

## `SessionId`

*class* — `SessionId.cs`

The session id: a filesystem-safe, sortable, collision-resistant token that names a session's
directory under `.aide/sessions/<session-id>/` (Addendum A3).

**Remarks.** **Why not `AgentWorktree.ShortId`.** `ShortId`
was read before writing this. It solves a different problem: deriving an eight-character
correlation TAG from an EXISTING, possibly hostile, external id (a harness session id feeding a
branch/folder name) by sanitizing and truncating. Truncation is the right trade there because the
tag is a display convenience alongside the real id.





Here there is no existing id to derive from — this method MINTS the primary identity of a
brand-new object, and it is the only thing distinguishing two sessions on disk. An
eight-character truncation of hostile/arbitrary input collides readily (that is *why*
`ShortId` is only ever used as a secondary tag, never as the sole key); a primary key cannot
accept that trade. So this generates its own value instead of sanitizing one: a UTC timestamp
(sortable — an operator scanning `.aide/sessions/` sees creation order for free) plus 32 bits
of CSPRNG entropy (collision-resistant independent of anything the caller supplies).





**The safe character set is reused.** The format below (`yyyyMMddTHHmmssZ-hexhexhex`)
is built entirely from `AgentWorktree`'s lesson: no `.`, no path separator, and nothing
outside `[A-Za-z0-9-]` — the same "small safe set" argument, applied to a value this type
controls completely rather than to hostile input.

| Member | Summary |
|---|---|
| `string New(DateTimeOffset? now = null)` | Mints a new session id. Deterministic in its timestamp half for testability. |
| `bool IsValid(string? candidate)` | True when  is this type's own shape — filesystem-safe by construction, since the character set never leaves `[0-9A-Za-z-]`. |

## `SessionPaths`

*class* — `SessionPaths.cs`

The on-disk path contract for a session (Addendum A3; Ruling 23; F0 clauses 1-2).

**Remarks.** **`session.json`, not `.yaml` (Ruling 23, recorded as an A3 erratum).** A3's
prose named `session.yaml`; at the time of Ruling 23, no YAML parser existed in any
`.csproj` in this repository, `System.Text.Json` was already used in 38 files, and the
sibling run-log path is `.jsonl`. Choosing YAML for this one file would have made it the
only YAML reader in the product for a format with no reader anywhere else — that was the actual
reasoning, and it still holds for THIS path today: `SessionFile` reads and writes
`System.Text.Json` only, via `SessionConfigStore`.





**Update (Ruling 35 / Ruling 36): a YAML-reading package now exists in this project.**
`AiDe.Core.csproj` takes a YAML library, scoped to the template loader
(`TemplateFrontmatterReader` / `TemplateSchema.cs`) for template
frontmatter — an unrelated format, an unrelated path. This does not reopen Ruling 23: Ruling 36
confirmed the two rulings do not conflict, because Ruling 23's subject was always the
session-config path specifically, never a claim that the *project* would forever contain no
YAML parser. Nothing under this type takes a YAML dependency; see
`SessionPathContractTests.SessionConfigSource_ContainsNoYamlToken`, which asserts that
directly against source rather than repeating a repo-wide claim in prose. (Deliberately not
naming the package here by its literal token:
`TemplateFrontmatterParserTests.TheDependencyIsScopedToTheTemplateLoader` asserts that
exactly one file in `src/` contains it, and this paragraph would otherwise be a second.)





**`runs/<run-id>.jsonl` is RESERVED, not built.** `RunLogStore` is Phase
3. This type only computes the path so the contract is fixed now and the directory shape never
has to change later; nothing in this slice ever calls `RunLogFile` to write —
asserted by `SessionPathContractTests` and `SessionConfigStoreTests` in
`AiDe.Core.Tests.Sessions`.

| Member | Summary |
|---|---|
| `string SessionFileName = "session.json"` | **(gap)** |
| `string EventsFileName = "session-events.jsonl"` | **(gap)** |
| `string RunsDirectoryName = "runs"` | **(gap)** |
| `string SessionsRoot(string workspaceRoot)` | `<workspaceRoot>/.aide/sessions` — every session's parent. |
| `string SessionDirectory(string workspaceRoot, string sessionId)` | `<workspaceRoot>/.aide/sessions/<session-id>`. |
| `string SessionFile(string workspaceRoot, string sessionId)` | The session's config file — see the remarks on YAML vs. JSON. |
| `string EventsFile(string workspaceRoot, string sessionId)` | The session's own append-only event log (`session.open` / `session.config` — clauses 3-4). Deliberately a SIBLING of `runs/`, never inside it: this is session-scoped state, not a run's record, and clause 2 forbids any… |
| `string RunsDirectory(string workspaceRoot, string sessionId)` | `<workspaceRoot>/.aide/sessions/<session-id>/runs` — RESERVED for Phase 3's `RunLogStore`. Computed, never created or written to, by this slice. |
| `string RunLogFile(string workspaceRoot, string sessionId, string runId)` | `<workspaceRoot>/.aide/sessions/<session-id>/runs/<run-id>.jsonl` — RESERVED for Phase 3's `RunLogStore`. F0 fixes the path so the shape never has to move; it must not be used to write here (clause 2, asserted by test). |

## `CatalogEntry`

*record* — `TemplateCatalog.cs`

One template as the catalog offers it — or refuses to, with its reasons.

| Member | Summary |
|---|---|
| `bool IsEnabled` | Whether the picker may offer this entry. A disabled entry still appears, carrying its errors. |
| `bool IsOverride` | Whether this entry shadows a same-id template from a lower-precedence source. |

## `TemplateCatalog`

*class* — `TemplateCatalog.cs`

The template catalog: every source's templates, resolved by precedence (B3.2, R18).

**Remarks.** **Nothing is silently dropped.** A template that fails load becomes a DISABLED entry
carrying its errors (Ruling 26(e)). The catalog view that renders it is Phase 3 (R22); what has to
be true now is that the model carries the failure, because a view cannot restore information the
model threw away.





**A broken override still wins.** If a workspace file shadows a built-in and fails to
load, the entry is the broken workspace one, disabled and badged — not a quiet fallback to the
built-in. Falling back would hide a broken file behind a catalog that looks healthy, which is the
silent drop again in another costume.

| Member | Summary |
|---|---|
| `IReadOnlyList<CatalogEntry> Entries { get; }` | Every entry, ordered by id, so two loads of one workspace list identically. |
| `TemplateCatalog BuiltIn()` | The catalog of just the twelve built-ins. |
| `TemplateCatalog Load(IReadOnlyList<ITemplateSource> sources)` | Loads every source and resolves same-id collisions by `PrecedenceOrder`. |
| `CatalogEntry? Find(string id)` | The entry for an id, or null when the catalog carries none. |

### `TemplateCatalog Load(IReadOnlyList<ITemplateSource> sources)`

Loads every source and resolves same-id collisions by `PrecedenceOrder`.

- **`sources`** — The registered sources, in any order. They are sorted here, so registration order carries no meaning and adding a source later cannot change what a caller has to pass.

## `TemplateCompiler`

*class* — `TemplateCompiler.cs`

Projects a template plus values to prompt text — deterministically (R18).

**Remarks.** **Same template version + values → byte-identical text.** Three things make that a
property rather than a hope: the renderer walks the TEMPLATE's declared fields, never the caller's
dictionary, so insertion and hash order cannot reach the output; the body arrives normalised to
line feeds with exactly one at the end; and nothing here reads a clock, a culture, a random source
or the environment.





**Total, not validating.** A field with no value renders empty. Required-ness is the form
engine's gate (F4) and blocking send is its job; a compiler that left `{{goal}}` in the
output would put template syntax in a prompt, which is worse than a blank.

| Member | Summary |
|---|---|
| `string Compile(PromptTemplate template, IReadOnlyDictionary<string, IReadOnlyList<string>> values)` | Compiles  against . |

### `string Compile(PromptTemplate template, IReadOnlyDictionary<string, IReadOnlyList<string>> values)`

Compiles  against .

- **`template`** — The template, as loaded.
- **`values`** — Values by field name. A text field takes the first value; a list or mentions field takes them all, in the order given — the caller's order IS the content, unlike its key order.

## `TemplateLoader`

*class* — `TemplateLoader.cs`

Load-time validation of one template file: every refusal B7 names, reported at once.

**Remarks.** **`when_to_use` and `why` are load-blocking (Ruling 26(e)).** A template that
cannot say when to pick it and why it earns its place does not load — and does not vanish either:
`TemplateCatalog` lists it as a disabled entry carrying these errors.





**All the errors, not the first.** Someone repairing a template file should learn
everything wrong with it in one pass; one-at-a-time validation turns a four-line fix into four
edit-and-reload cycles.





**Every type decision is made here, from the schema.** The reader hands over strings;
this file decides what is a version, a field type, a flag and a minimum. That is the whole of
"deserialize into the schema type only" — the document has no say.

| Member | Summary |
|---|---|
| `TemplateLoadResult Load(string text)` | Reads one template file's text. |

## `TemplateContract`

*class* — `TemplateSchema.cs`

The pinned frontmatter contract every prompt template is read under (Addendum B §B7).

**Remarks.** **Pinned from birth, not once it settles.** The validator ships in Phase 1 and enforces a
shape; a contract enforced in code with no documented shape is a shape asserted from code. The
registry entry is `docs/architecture/pinned-contracts.md` (Ruling 29), which names this
version, its home document, and its evolution rule.





**Evolution is additive within schema 1.** An unknown frontmatter field is PRESERVED —
see `UnknownFrontmatter` — never rejected, so a template written for a
later reader still loads here with the part this reader understands. A breaking change means
`template-schema/2` loading side by side, the discipline `weave/1` and
`loomkeeper/1` already follow.





**`min` and `tier_default` are schema-1 CONSTRAINTS, not preserved unknowns.**
Both appear in B3.1's own specimen, which is the document the schema was pinned from; treating
them as unknown would mean the schema's own example carries fields the schema has never heard of,
and `min` would be silently dropped from a template that declared it. They are typed members
— `Min` and `TierDefault`.

| Member | Summary |
|---|---|
| `string SchemaVersion = "template-schema/1"` | The pinned contract id. A change here is a contract change, not a re-parse. |

## `TemplateFieldType`

*enum* — `TemplateSchema.cs`

The field types `template-schema/1` knows (B3.1).

## `TemplateField`

*record* — `TemplateSchema.cs`

One typed field a template declares.

## `PromptTemplate`

*record* — `TemplateSchema.cs`

A template that loaded — frontmatter under `SchemaVersion`, plus its body.

## `TemplateError`

*record* — `TemplateSchema.cs`

One reason a template did not load. The code is stable; the message is for a person.

## `TemplateErrorCodes`

*class* — `TemplateSchema.cs`

Stable codes for every load refusal, so a catalog entry's error survives a reword.

| Member | Summary |
|---|---|
| `string MissingFrontmatter = "TS-0001"` | The file has no frontmatter block at all. |
| `string MalformedFrontmatter = "TS-0002"` | The frontmatter block is not well-formed YAML. |
| `string ExplicitYamlTag = "TS-0003"` | A node carries an explicit YAML tag, which would let the document choose a type. |
| `string MissingId = "TS-0004"` | No `id`. There is nothing to key the catalog on. |
| `string MissingWhenToUse = "TS-0005"` | No `when_to_use`, or a blank one. Load-blocking by B7 and Ruling 26(e). |
| `string MissingWhy = "TS-0006"` | No `why`, or a blank one. Load-blocking for the same reason. |
| `string InvalidVersion = "TS-0007"` | A `version` that is absent, unparseable, or below 1. |
| `string UnknownFieldType = "TS-0008"` | A field `type` outside `TemplateFieldType`. |
| `string MalformedField = "TS-0009"` | A malformed `fields` block — not a sequence, or an entry that is not a mapping. |
| `string DuplicateFieldName = "TS-0010"` | Two fields with one name, so a slot would be ambiguous. |
| `string UnresolvedSlot = "TS-0011"` | A body slot naming no declared field. |
| `string InvalidMin = "TS-0012"` | A `min` that is not a positive integer, or is declared on a non-list field. |
| `string DuplicateIdInSource = "TS-0013"` | Two templates in ONE source claiming one id (B7). |
| `string MissingIntentOrAudience = "TS-0014"` | An `intent` or `audience` that is absent or blank. |

## `TemplateLoadResult`

*record* — `TemplateSchema.cs`

What one load attempt produced: a template, or the reasons there is none.

| Member | Summary |
|---|---|
| `bool Loaded` | Whether this is a template the catalog may offer. |

## `TemplateDocument`

*record* — `TemplateSources.cs`

One template file a source offers, with wherever it came from.

## `ITemplateSource`

*interface* — `TemplateSources.cs`

A place templates come from (B3.2).

**Remarks.** Deliberately an interface over a list of documents rather than a directory: the built-in source is
inside the assembly, a workspace source is a directory, and a later pack or personal source is
another directory somewhere else. The catalog only needs "here are the files".

## `TemplateSources`

*class* — `TemplateSources.cs`

The source names, their fixed precedence, and the set Phase 1 actually registers.

**Remarks.** **The full order is pinned NOW, though Phase 1 ships two of the four (Ruling 26 cut i).**
Pack's lifecycle is `/updatepack`'s, which is Phase 3, and personal has no settings surface
yet. Fixing `personal > workspace > pack > built-in` here means registering either of
them later is DATA — constructing a source — rather than a renegotiation of which one wins.





**Sources are an ordered descriptor list.** The catalog sorts what it is handed by
`RankOf`, so a new source joins by being passed in; nothing in the loader enumerates
the sources it knows about.

| Member | Summary |
|---|---|
| `string Personal = "personal"` | The private, per-person source — `~/.aide/templates/`. Not registered in Phase 1. |
| `string Workspace = "workspace"` | The committed, repository-shared source — `.aide/templates/`. |
| `string Pack = "pack"` | The pack source, arriving via `/updatepack`. Not registered in Phase 1. |
| `string BuiltIn = "built-in"` | The twelve shipped with AI-DE. Always last, so anything can override it. |
| `IReadOnlyList<string> PrecedenceOrder = [Personal, Workspace, Pack, BuiltIn]` | Highest precedence first. Same-id templates resolve by this order and only this order. |
| `int RankOf(string sourceId)` | Where a source sits in `PrecedenceOrder`. Lower wins. |
| `IReadOnlyList<ITemplateSource> Phase1(string? workspaceRoot)` | The sources Phase 1 registers: workspace over built-in. |

### `int RankOf(string sourceId)`

Where a source sits in `PrecedenceOrder`. Lower wins.

**Throws `ArgumentException`.** The source id is not one this contract names.

### `IReadOnlyList<ITemplateSource> Phase1(string? workspaceRoot)`

The sources Phase 1 registers: workspace over built-in.

- **`workspaceRoot`** — The open workspace, or null when there is none.

## `BuiltInTemplateSource`

*class* — `TemplateSources.cs`

The twelve built-in templates, transcribed from Addendum B §B4 and shipped inside the assembly.

**Remarks.** **Real template files, not a table of C# constants.** They are embedded resources read through
the same loader every other source uses, so a mistake in one of them fails the same way a
workspace author's would — and the built-in catalog is evidence that the loader works rather than
a path around it.

| Member | Summary |
|---|---|
| `string SourceId` | **(gap)** |
| `IReadOnlyList<TemplateDocument> Read()` | **(gap)** |

## `WorkspaceTemplateSource`

*class* — `TemplateSources.cs`

The workspace's own templates — `.aide/templates/`, committed beside the code they serve.

| Member | Summary |
|---|---|
| `string RelativeDirectory = Path.Combine(".aide", "templates")` | Where a workspace keeps its templates, relative to the workspace root (B3.2). |
| `string SourceId` | **(gap)** |
| `IReadOnlyList<TemplateDocument> Read()` | **(gap)** |

### `IReadOnlyList<TemplateDocument> Read()`

**Remarks.** A workspace with no templates directory reads EMPTY, not broken. Most workspaces will never
have one, and a missing optional directory that threw would make the built-in catalog
unreachable for them.
