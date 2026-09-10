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
  Extracted public surface of AiDe.Core.Sessions: 16 types, 36 members, 92% carrying a summary doc comment.
---

# API: `AiDe.Core.Sessions`

**16 public types · 36 public members · 92% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

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
