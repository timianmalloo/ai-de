---
id: api-aide-core-extraction
title: "API: AiDe.Core.Extraction"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Extraction: 40 types, 104 members, 75% carrying a summary doc comment.
---

# API: `AiDe.Core.Extraction`

**40 public types · 104 public members · 75% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `BicepExtractor`

*class* — `BicepExtractor.cs`

The infrastructure extractor — Bicep read as **data**, never compiled.

**Remarks.** **Why not `bicep build`.** Spike D3 measured that compiling repository-supplied
input runs repository-supplied logic, and Bicep resolves module references and evaluates template
functions at build time. Invoking the compiler on a cloned repository is the same exposure
MSBuild was, so the same answer applies: read it.





**Measured at parity for what it claims.** Against `az bicep build` on a real
677-line template: 24 of 24 resources, 19 of 19 types, 18 of 18 parameters
(`spikes/bicep-as-data`).





**Names are CONSTANT-FOLDED, not evaluated.** Parameters with a declared default and
variables are substituted, string interpolation and four pure string functions are folded over
values already known, and everything else is refused and counted. MEASURED across every
`.bicep` file in TheTerrace and this repository — 27 resource declarations — that resolves
20 names and leaves 7. The residue is `guid(...)`, whose arguments are resource IDs that do
not exist until a deployment names a subscription, and one parameter with no default. Both are
boundaries, not gaps: no amount of reading a file closes either.





The reason it folds at all is a defect rather than coverage. The old test for "literal" was
*contains no `$` and no `(`*, which a bare identifier passes — so
`name: workspaceName` was asserted as the name `workspaceName`. That was **10 of the
27**, undisclosed, because they never reached the expression branch.





**The value of an `@secure()` parameter is never read.** Not redacted after the
fact: the parameter is recorded as existing and as secret, and its value is never looked at, so
there is no path by which it could reach a store, a log or a projection.

| Member | Summary |
|---|---|
| `string ExtractorId = "bicep-extractor"` | **(gap)** |
| `string ScopeKind` | **(gap)** |

## `BoundedContext`

*record* — `BoundedContextMap.cs`

One declared bounded context.

## `ContextProblem`

*record* — `BoundedContextMap.cs`

A validation problem. Every one of these FAILS the load — none is a warning.

## `BoundedContextMap`

*record* — `BoundedContextMap.cs`

The declared context map, and what validating it against the real symbols found.

| Member | Summary |
|---|---|
| `bool IsValid` | **(gap)** |
| `double Coverage` | **(gap)** |
| `string Describe()` | **(gap)** |

## `BoundedContextReader`

*class* — `BoundedContextMap.cs`

Reads and **validates** `docs/bounded-contexts.yaml` (ADR-0016).

**Remarks.** **Validated, not merely parsed.** A context naming a namespace that does not exist fails
loudly. A declaration file that silently tolerates stale entries becomes fiction within a release,
and fiction that looks like configuration is worse than no configuration.





**A deliberately small YAML subset**, and anything outside it is an ERROR rather than a
best-effort guess. Hand-rolling a general YAML parser is how a config file starts meaning
something slightly different from what its author read — so this accepts exactly the shape ADR-0016
documents and rejects the rest by name. `simplify: a subset reader rather than a YAML dependency;
ceiling is the documented shape; upgrade trigger = a real map needs anchors, nested maps or
multi-line scalars.`

| Member | Summary |
|---|---|
| `string DefaultRelativePath = "docs/bounded-contexts.yaml"` | The file's conventional location, relative to a repository root. |
| `BoundedContextMap Load(` | Loads and validates the map against . |
| `bool IsCodeSymbol(string id)` | Whether an id names a code symbol, as opposed to another artifact kind's subject. |
| `bool Matches(string pattern, string symbol)` | Whether a namespace pattern covers a symbol. `*` is a suffix wildcard only. |

### `BoundedContextMap Load(`

Loads and validates the map against .

- **`knownSymbols`** — Every symbol the extractor found. Validation without these would only check the file's shape, which is the half that never goes stale.

### `bool IsCodeSymbol(string id)`

Whether an id names a code symbol, as opposed to another artifact kind's subject.

**Remarks.** Scope-qualified ids — `bicep:main#appName`, `table:Orders`, `schema:...` — are
subjects of other artifact kinds. They are real evidence and they belong in the store; they
are simply not what a bounded-context map is about. The rule lives here rather than at each
call site because two callers already need it and a second copy is a second thing to drift.

## `CSharpExtractor`

*class* — `CSharpExtractor.cs`

The C# semantic extractor — real symbols, without ever running the repository's build.

**Remarks.** **Named for the language, not the library.** Roslyn is still the semantic engine, but
"RoslynExtractor" implied the Roslyn *workspace* layer, and that is precisely the part not
used: `MSBuildWorkspace` loads projects by evaluating MSBuild, which spike D3 measured
executing repository-supplied code by four vectors, two of which need nothing but a checked-in
`.csproj`.





**One scope is one (project, target framework).** Declared here rather than left
implicit, because it is the grain of every row this emits.





**An edge that did not resolve is not emitted.** Emitting it as `Inferred` would be
worse than silence: the name is whatever the source typed, unresolved by anything, so the edge
would point at a node that may not exist. What the user gets instead is a disclosure on the scope
saying the picture is incomplete and why.

| Member | Summary |
|---|---|
| `string ExtractorId = "csharp-extractor"` | **(gap)** |
| `string DisclosurePredicate = "discloses"` | The predicate a scope uses to declare what it could not see. |
| `string ScopeKind` | **(gap)** |
| `IReadOnlyList<string> TargetFrameworks(string projectPath)` | Every target framework the project at  declares. |
| `Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)` | **(gap)** |
| `string ScopeNodeId(string scopeId)` | The node a scope's own facts hang off. |
| `int MaxMembersPerType = 40` | Members carried per type before the compartment is truncated. |

### `IReadOnlyList<string> TargetFrameworks(string projectPath)`

Every target framework the project at  declares.

**Remarks.** The caller creates one scope per entry — see the grain note on the class.

### `int MaxMembersPerType = 40`

Members carried per type before the compartment is truncated.

**Remarks.** Enough for any type a person reads at once. A class with more than this has a problem the
diagram cannot fix, and carrying all of them would cost every other type on the canvas.

## `ExtractionDisclosures`

*class* — `CSharpProjectReader.cs`

Why a compilation could not see everything a build would.

**Remarks.** These are the extractor's **disclosures**. Each becomes a fact on the scope, so a projection
over an affected scope reports the omission instead of answering as though nothing were missing.
A silently incomplete answer is the failure mode this whole design exists to avoid.

| Member | Summary |
|---|---|
| `string PackagesNotRestored = "packages-not-restored"` | No `obj/project.assets.json`: package types are unresolved. |
| `string XamlGeneratedMembersNotAnalysed = "xaml-generated-members-not-analysed"` | A WPF project's XAML-generated partial members are not analysed. |
| `string GeneratedCodeNotAnalysed = "generated-code-not-analysed"` | Source generators are never run, so generated symbols are absent (S2/S1). |
| `string ProjectReferenceUnresolved = "project-reference-unresolved"` | A `ProjectReference` could not be resolved; edges into it are missing. |
| `string SourceDidNotParse = "source-did-not-parse"` | A source file did not parse, so every type in it is absent from the graph. |
| `string BuildConditionsNotEvaluated = "build-conditions-not-evaluated"` | A property carried a `Condition` that was taken at face value rather than evaluated. |
| `string BicepExpressionsNotEvaluated = "bicep-expressions-not-evaluated"` | A Bicep resource name is an expression only the compiler could resolve. |
| `string BicepResourceCountIndeterminate = "bicep-resource-count-indeterminate"` | A template declares loops or conditional resources, so the DECLARATION count is not the deployment count. |
| `string SchemaFromMigrationsNotDatabase = "schema-from-migrations-not-database"` | The schema is what the migrations INTEND, not what a server holds. |
| `string SchemaChangedByRawSqlNotRead = "schema-changed-by-raw-sql-not-read"` | A migration changed the schema through raw `Sql()`, which is not legible as syntax. |

### `string SourceDidNotParse = "source-did-not-parse"`

A source file did not parse, so every type in it is absent from the graph.

**Remarks.** **The state a developer is in most often, and it was invisible.** Measured on a copy of a
real repository with one deliberate syntax error: the index reported `10 of 10 scopes, 0
failed` and produced 106 fewer assertions than the working copy, with nothing anywhere
saying a file had not been read. Roslyn parses broken source into a tree with error nodes
rather than throwing, so the extraction succeeds and simply finds less — which is
indistinguishable from a smaller file (DC-025).

### `string BuildConditionsNotEvaluated = "build-conditions-not-evaluated"`

A property carried a `Condition` that was taken at face value rather than evaluated.

**Remarks.** Evaluating conditions IS MSBuild evaluation, which this design refuses. Taking them at face
value is right far more often than it is wrong — but when it is wrong it changes which code
compiles, so it is stated rather than assumed away.

### `string BicepResourceCountIndeterminate = "bicep-resource-count-indeterminate"`

A template declares loops or conditional resources, so the DECLARATION count is not the
deployment count.

**Remarks.** A `[for ...]` resource becomes one deployed resource per item in a collection nothing
here evaluates, and an `if (...)` resource may not be deployed at all. Reporting "24
resources" for a template that deploys forty, or eighteen, would be a confident wrong number.

### `string SchemaFromMigrationsNotDatabase = "schema-from-migrations-not-database"`

The schema is what the migrations INTEND, not what a server holds.

**Remarks.** They diverge — a hand-applied change, a failed deployment, a database restored from an older
backup — and a join that pretended otherwise would be exactly the inferred-edge failure this
phase is most exposed to.

### `string SchemaChangedByRawSqlNotRead = "schema-changed-by-raw-sql-not-read"`

A migration changed the schema through raw `Sql()`, which is not legible as syntax.

**Remarks.** Owed by the spike rather than invented here: the corpus repository has four such statements,
and they create indexes and move data. A fold that stayed silent about them would report a
schema that looks complete.

## `CSharpCompilationResult`

*record* — `CSharpProjectReader.cs`

One project, compiled for one target framework, plus what could not be seen.

## `CSharpProjectReader`

*class* — `CSharpProjectReader.cs`

Reads a C# project file **as data** and produces a Roslyn compilation from it.

**Remarks.** **Nothing here evaluates MSBuild or runs a target.** That is the entire point: spike D3
measured that loading a repository through `MSBuildWorkspace` executes code the repository
supplied — an `Exec` in `InitialTargets` or a `RoslynCodeTaskFactory` inline task
needs nothing but a checked-in `.csproj`. Reading the file as XML cannot do that.





**Measured at parity, not assumed.** Against `MSBuildWorkspace` on four project
shapes — plain, `ProjectReference`+WPF, `ProjectReference`, and multi-targeted — this
recovers 100% of dependency edges and loses no types, ~25x faster
(`spikes/extraction-fidelity`).

| Member | Summary |
|---|---|
| `SyntaxTreeCache Trees { get; } = new()` | Parsed trees, reused across index runs for files that have not changed. |
| `IReadOnlyList<string> TargetFrameworks(string projectPath)` | Every target framework the project declares. One scope per (project, framework). |
| `CSharpCompilationResult Compile(string projectPath, string targetFramework, CancellationToken cancellationToken)` | **(gap)** |

### `SyntaxTreeCache Trees { get; } = new()`

Parsed trees, reused across index runs for files that have not changed.

**Remarks.** Per reader instance, so a long-lived daemon keeps it and a one-shot spike does not leak it.
Parsing is ~96% of everything extraction does — profiled twice — which is what makes this the
one cache worth having and every other one premature.

### `IReadOnlyList<string> TargetFrameworks(string projectPath)`

Every target framework the project declares. One scope per (project, framework).

**Remarks.** Not per project: a multi-targeted project's `#if`-gated types genuinely differ between
frameworks, so a single scope would have to pick one and be silently wrong about the others.
Measured — `MSBuildWorkspace` loads one framework and sees one of two conditional types.

## `ScopeDescriptor`

*record* — `CSharpScopeDiscovery.cs`

One extraction scope: a project built for one target framework.

| Member | Summary |
|---|---|
| `string DisplayName` | What the user sees in a pane title or a scope list. |

## `CSharpScopeDiscovery`

*class* — `CSharpScopeDiscovery.cs`

Finds the C# scopes in a repository — **one per (project, target framework)**.

**Remarks.** The grain is the finding, not a preference: a multi-targeted project's `#if`-gated
types genuinely differ between frameworks, so a single scope per project would have to pick one
and be silently wrong about the others (measured in `spikes/extraction-fidelity`).





**Directories are skipped by name, and the list is deliberately short.** Skipping too
much is how a real project silently fails to appear; `bin`, `obj` and `.git` are
the ones that contain no source a user wrote.

| Member | Summary |
|---|---|
| `IReadOnlyList<ScopeDescriptor> Discover(string rootPath, CSharpProjectReader? reader = null)` | Every C# scope under , ordered so the list is stable between runs. |
| `IReadOnlyList<ScopeDescriptor> DiscoverAll(string rootPath, CSharpProjectReader? reader = null)` | Every Phase-3 scope: C# projects, Bicep templates, and EF migration directories. |

### `IReadOnlyList<ScopeDescriptor> Discover(string rootPath, CSharpProjectReader? reader = null)`

Every C# scope under , ordered so the list is stable between runs.

**Remarks.** A stable order matters more than it looks: scope ids feed generation numbers and the health
view, and a set that reshuffles between runs makes two identical repositories look different.

### `IReadOnlyList<ScopeDescriptor> DiscoverAll(string rootPath, CSharpProjectReader? reader = null)`

Every Phase-3 scope: C# projects, Bicep templates, and EF migration directories.

**Remarks.** One list rather than three call sites, because a repository is indexed as a whole and a
caller that had to remember to ask for infrastructure separately would eventually forget.

## `CompositeExtractor`

*class* — `CSharpScopeDiscovery.cs`

Routes an extraction to the extractor that owns its scope kind.

**Remarks.** Routing on the scope id's prefix rather than on a registration table because the prefix is
already the scope's identity — a separate mapping is a second thing that can disagree with the
ids actually in the store.

| Member | Summary |
|---|---|
| `string ScopeKind` | **(gap)** |
| `IExtractor RouteFor(string scopeId)` | Which extractor a scope id resolves to. Exposed so routing can be ASSERTED. |
| `Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)` | **(gap)** |

### `IExtractor RouteFor(string scopeId)`

Which extractor a scope id resolves to. Exposed so routing can be ASSERTED.

**Remarks.** The router is four positional constructor parameters, and getting their order wrong is silent:
a mis-ordered composite routes bicep scopes to the schema extractor, both fail, and the run
reports a repository with no infrastructure in it. That happened. A test can now read the
decision instead of trusting the call site.

## `EfSchemaExtractor`

*class* — `EfSchemaExtractor.cs`

The schema extractor — EF Core migrations folded into the tables they create.

**Remarks.** **This replaced the planned DDL parser on evidence.** The first repository it was checked
against holds 62 migration classes and **zero** `.sql` files, so a DDL parser would have
shipped with no corpus. Measured against EF's own checked-in model snapshot it recovers
**62 of 62** tables in 99 ms (`spikes/ef-migration-schema`).





**Migrations are append-only and ordered, so the schema is a fold over them** — the same
shape as the fact store itself, which is why schema evidence needs no new table and sits beside
code evidence at a different grain. Ordering is by the timestamp prefix in the FILE NAME, which
is how EF orders them; any other ordering puts a create after a drop and yields a schema that
never existed.





**Read as syntax.** No EF, no database, no `dotnet ef`. Phase 2's constraint carries
forward without exception.

| Member | Summary |
|---|---|
| `string ExtractorId = "ef-schema-extractor"` | **(gap)** |
| `string ScopeKind` | **(gap)** |
| `Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)` | **(gap)** |

## `ExtractionFacts`

*class* — `ExtractionFacts.cs`

Shared rules every extractor applies to the facts it is about to return.

**Remarks.** **Written on the third copy, not the first.** The Python and TypeScript readers each grew
the same six-line dedupe after the same failure — a raw
`UNIQUE constraint failed: evidence_assertion_fact…` from the middle of an index on a real
repository — and the C# reader hit it a third time the moment it started emitting
`uses_table`, because one store class names the same table in four statements.





**Why the store's key is not the thing to loosen.** P1-STORE-05 rejects the same fact
twice for one revision deliberately: it is the control that catches a producer emitting
contradictory or duplicated evidence. Silencing it would trade a loud correct failure for a quiet
wrong graph. An identical triple carries no information, so removing it before the write is the
honest fix — and doing it in one place means the fourth extractor inherits it.

| Member | Summary |
|---|---|
| `IReadOnlyList<EvidenceAssertion> Distinct(IEnumerable<EvidenceAssertion> assertions)` | One fact per distinct subject-predicate-object, keeping the first occurrence. |

### `IReadOnlyList<EvidenceAssertion> Distinct(IEnumerable<EvidenceAssertion> assertions)`

One fact per distinct subject-predicate-object, keeping the first occurrence.

**Remarks.** The FIRST is kept because provenance points at where a fact was first seen, and the earliest
mention is the one a reader following the graph would want to open. Status is deliberately
not part of the key: an extractor that asserts the same triple both Verified and Inferred has
a defect, and collapsing the pair here would hide it — the store's key still catches that.

## `ExtractionErrorCodes`

*class* — `FixtureExtractor.cs`

*No doc comment on this type.* **(gap)**

| Member | Summary |
|---|---|
| `string Timeout = "AIDE-EXTRACT-TIMEOUT"` | **(gap)** |
| `string Quarantined = "AIDE-EXTRACT-QUARANTINED"` | **(gap)** |
| `string PathContainment = "AIDE-PATH-CONTAINMENT"` | **(gap)** |
| `string Malformed = "AIDE-EXTRACT-MALFORMED"` | **(gap)** |

## `ExtractionRequest`

*record* — `FixtureExtractor.cs`

One scope's extraction, and what the rest of the workspace contains.

**Remarks.** **Why a whole-workspace set rather than per-scope discovery.** A Python or TypeScript
scope is one directory, and an import that names a sibling package resolves to a file in a
DIFFERENT scope. Resolving that from inside the scope is impossible, and resolving it by
extraction order would be resolution that is wrong whenever the order changes — the same trap
the Python extractor already avoids within a scope by collecting modules before it reads any.





Null means "not supplied", which is not the same as empty: an extractor treats it as no
cross-scope knowledge and falls back to disclosing what it could not resolve.

## `ExtractionDiagnostic`

*record* — `FixtureExtractor.cs`

*No doc comment on this type.* **(gap)**

## `ExtractionResult`

*record* — `FixtureExtractor.cs`

*No doc comment on this type.* **(gap)**

## `IExtractor`

*interface* — `FixtureExtractor.cs`

The extractor seam. Phase 1 ships the fixture adapter; Phase 2 substitutes Roslyn behind it.

## `FixtureExtractor`

*class* — `FixtureExtractor.cs`

Reads a fixture repository and emits provenance-labelled assertions.

**Remarks.** Two artifact shapes, both deliberately repo-shaped rather than synthetic:

`*.facts` — one relation per line, `Subject -> predicate -> Object [Status]`,
standing in for a source extractor.
`*.md` with YAML-ish frontmatter — knowledge nodes and their `links:` edges,
which is what US-4's knowledge navigation actually reads.

A malformed line becomes a diagnostic and marks the snapshot incomplete; it never silently
vanishes, because an empty graph reported as a clean graph is the failure this design forbids.

| Member | Summary |
|---|---|
| `string ExtractorId = "fixture-extractor"` | **(gap)** |
| `string ScopeKind` | **(gap)** |
| `Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)` | **(gap)** |

## `WorkspaceKnowledge`

*record* — `KnowledgeExtractor.cs`

The knowledge graph: documents that declare an identity, a kind and typed links.

Every markdown file in the workspace, and the node id it declares — or none.

**Remarks.** **Reported by the user: the graph showed knowledge as ZERO and code as a large count.**
The reason was not that repositories have no knowledge — it is that nothing ever looked. A reader
for these documents has existed since Phase 1 inside the fixture extractor, with tests, and
scope discovery produced six kinds of scope (`csharp`, `bicep`, `schema`,
`python`, `typescript`, `sql`) and no knowledge scope at all. The capability was
real, tested, and unreachable on any real repository.





**A zero that means "nobody looked" reads as "there is none".** That is the shape this
product exists to avoid, and it was in the product's own headline surface — on a repository whose
entire premise is that *docs hold intent, code holds reality, and the expensive defects live
in the gap*. Half of that sentence was never being read.





**The body is now read, for exactly one thing.** Until this landed the reader saw only
frontmatter and disclosed `knowledge-body-not-analysed` on every scope — 877 documents on
TheTerrace present in the graph as their own metadata. A markdown hyperlink to another document
is the one thing in a body that is a DECLARATION: the author wrote a path, and the path either
resolves to a file this scope read or it does not. Everything else a body contains was measured
and left unread on purpose; `nowledgeBody` carries the numbers and the reasons, and
each one is disclosed with a count rather than skipped in silence.





**Nothing here is matched by resemblance.** The user's decision of 2026-08-30 — *"do
not infer, the graph should only be on observable links/relationships"* — rules out reading
prose for names that look like code or like a document id. Measured against it: 26,924 inline
code spans in TheTerrace's documents match zero C# node ids exactly, and the knowledge-id variant
that does fire is wrong often enough to reject (``architecture`` names an MCP tool in 4 of
its 5 occurrences in this repository). A link is different in kind: `[x](../y.md)` has one
reading.





**READ WIDELY, EMIT NARROWLY — and this reverses a decision made the day before.**
Knowledge scopes NEST: discovery yields a scope for every directory holding a document with an
id, so `docs` and `docs/adr` are both scopes, and a reader that walked its scope
RECURSIVELY indexed `docs/adr/0001.md` from both. MEASURED on TheTerrace: **2,371
`node_class` rows for 878 distinct documents** — every knowledge fact stored ~2.7 times.
Walking each scope's OWN directory fixes that exactly, and on its own it cost **30 of the 42
prose-link edges**: a link from `docs/adr` to `docs/specs` only resolved because the
recursive parent had read both sides. That change was made, measured and reverted rather than
shipped (DC-051).





**So the two jobs are separated instead of traded.** RESOLUTION reads the whole
workspace — `orkspaceKnowledge`, built once per revision by `WorkspaceCore` and
handed to every scope, exactly as `WorkspaceModules` already is for Python and TypeScript.
EMISSION covers only the markdown directly in this scope's directory, so each document is
extracted by exactly one scope. Measured on TheTerrace: 878 documents preserved, `node_class`
2,371 → 878, distinct `links_to` edges 42 → 42 (rows 68 → 42).





**What that overturns.** The reader shipped on 2026-08-31 with *"a link above the
scope is its own boundary"* — a path climbing out of the scope was refused because a wider
scope might hold it and this reader had no way to know. That was RIGHT while the scope was the
unit of resolution, and is WRONG now that each document belongs to exactly one scope: under the
old rule a link from `docs/adr/0001.md` to `../specs/workspace.md` would be a boundary
on the only scope that will ever read `0001.md`, and the edge would exist nowhere. The
boundary has not been deleted, it has MOVED OUT to the workspace root, which is the real edge of
what this product reads. Measured consequence on this repository: 19 links to
`../../spikes/*/RESULT.md` that used to be counted as "outside the scope" are now correctly
counted as "resolves to a markdown file that declares no id".





**Widening resolution is not widening inference.** The user's decision of 2026-08-30
still governs: a link enters the graph only because an author WROTE a path and that path names a
file this reader opened and found an id in. Nothing here matches by name, by resemblance or by
proximity — the workspace map is keyed by PATH, and a document's id is only ever read out of the
file the path lands on.





**Why not point the fixture extractor at the repository instead.** It enumerates
`*` recursively with no exclusions — pointed at a real checkout it would walk
`node_modules`, `bin` and `.git`. It also stamps `fixture-extractor` into
provenance, which would be a lie on a real document. The parsing is shared
(`nowledgeFrontmatter`); only the walking and the identity differ.

**Keyed by PATH, never by name.** The map is what makes cross-directory resolution
possible without inference: a link is followed to a file, and the id is whatever that file says
it is. Nothing is matched by resemblance.





The comparer is ORDINAL-IGNORE-CASE, and this is part of the contract rather than an
implementation detail — these paths come off a Windows filesystem where `../ADR/0001.md` and
`../adr/0001.md` are one file, and a case-exact lookup would silently miss one of them.
`Survey` is the only thing that should build one.

## `KnowledgeExtractor`

*class* — `KnowledgeExtractor.cs`

*No doc comment on this type.* **(gap)**

| Member | Summary |
|---|---|
| `string ScopeKind` | **(gap)** |
| `Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)` | **(gap)** |
| `WorkspaceKnowledge Survey(string root)` | Every markdown file under , and the id it declares — or none. |

### `WorkspaceKnowledge Survey(string root)`

Every markdown file under , and the id it declares — or none.

**Remarks.** **Built ONCE per revision by `WorkspaceCore` and handed to every scope**, for
the same reason `WorkspaceModules` is: thirty-nine knowledge scopes each walking the
whole tree is thirty-nine walks, and resolving against what has already been extracted
instead would make an edge depend on the order the scopes happened to run in.





**Only the frontmatter block is read.** The id is decided in the first few lines or
not at all, and this opens every markdown file in the repository — 1,087 on TheTerrace, of
which 209 are ordinary READMEs that are in the map purely so a link to one is reported as a
boundary rather than as a broken cross-reference.

## `Disclosures`

*class* — `KnowledgeExtractor.cs`

What this reader does not see, stated on the scope with a count.

**Remarks.** **Every one of these is conditional.** The disclosure they replaced —
`knowledge-body-not-analysed` — fired on every scope whether or not anything had been
hidden, and it would now be false on any scope whose prose links resolve. That is the exact
shape the Python reader had to correct: *"a blanket 'imports are not resolved' was true
when none were, and became a closed gap reported as open the moment resolution landed"*.
A disclosure that cannot be absent teaches a reader to stop reading disclosures.





**Boundaries and gaps are kept apart** (DC-050). What this product declines to read
— headings, glossary terms, backticked identifiers, a link out of this scope — is a statement
about scope. A link naming a markdown file that is not there is a defect IN THE DOCUMENT, and
merging the two would bury the second inside the first.

| Member | Summary |
|---|---|
| `string ArtifactsWithoutIds = "knowledge-artifacts-without-ids"` | A markdown file with frontmatter but no id cannot be a node. |
| `string LinkTargetMissing = "knowledge-prose-link-target-missing"` | GAP: a prose link names a markdown file that is nowhere in the workspace. |
| `string LinkTargetNotANode = "knowledge-prose-link-target-not-a-node"` | BOUNDARY: a prose link resolves to a markdown file that declares no id. |
| `string LinkTargetOutsideWorkspace = "knowledge-prose-link-target-outside-workspace"` | BOUNDARY: a prose link points above the WORKSPACE root, where this product does not look. |
| `string HeadingsNotAnalysed = "knowledge-headings-not-analysed"` | BOUNDARY: a document's structure is counted, not extracted. |
| `string GlossaryTermsNotAnalysed = "knowledge-glossary-terms-not-analysed"` | BOUNDARY: a glossary's term definitions are counted as documents, not read. |
| `string InlineCodeNotResolved = "knowledge-inline-code-not-resolved"` | BOUNDARY: backticked identifiers are not matched against anything. |
| `string ImportsNotResolved = "python-imports-not-resolved"` | No name resolution: an import names a module path, not a symbol. |
| `string StandardLibraryNotIndexed = "python-standard-library-not-indexed"` | Imports naming the standard library — a boundary of the product, not a gap in it. |
| `string NestedDeclarationsNotAnalysed = "python-nested-declarations-not-analysed"` | Declarations nested deeper than a class's own body — closures, and definitions inside methods. |
| `string DynamicImportsNotAnalysed = "python-dynamic-imports-not-analysed"` | Nothing dynamic is followed — importlib, __import__, conditional imports. |
| `string RenamesNotFollowed = "sql-renames-not-followed"` | A rename is not followed, so the table or column keeps its earlier name. |
| `string DynamicDdlNotEvaluated = "sql-dynamic-ddl-not-evaluated"` | DDL inside a string literal — a message, or dynamic SQL nobody evaluated. |
| `string ColumnDetailNotRead = "sql-column-detail-not-read"` | Column types, constraints and indexes are not read. |
| `string NotTheDatabase = "sql-schema-from-files-not-database"` | This is the schema the FILES declare, not what a server holds. |
| `string TypesNotChecked = "typescript-types-not-checked"` | No type checking: an import names a module specifier, not a symbol. |
| `string NonExportedNotAnalysed = "typescript-non-exported-not-analysed"` | RETIRED. Kept only so the string has one home: stores written before this reader read non-exported declarations still carry it, and a test asserts it is no longer emitted. Disclosing a gap that has been closed is the … |
| `string DynamicImportsNotAnalysed = "typescript-dynamic-imports-not-analysed"` | Nothing but a static `import`/`export … from` statement is followed — `import()`, `require()` in ANY form, and re-export globs are not. |
| `string ExportsNotRecognised = "typescript-exports-not-recognised"` | An export whose spelling this reader does not know (DC-033's own alarm). |
| `string NodeBuiltinsNotIndexed = "typescript-node-builtins-not-indexed"` | An import naming Node's runtime — a boundary of the product, not a gap in it. |
| `string PackagesNotIndexed = "typescript-packages-not-indexed"` | An import naming an npm package — a boundary of the product, not a gap in it. |
| `string ImportsNotResolved = "typescript-imports-not-resolved"` | A specifier this scope does not contain and which nobody can identify. |
| `string GeneratedSourceNotRead = "typescript-generated-source-not-read"` | Bundled or generated JavaScript, skipped because nobody wrote it. |
| `string NestedDeclarationsNotAnalysed = "typescript-nested-declarations-not-analysed"` | A declaration inside a function, a method or a namespace block — something no importer can reach by name. |

### `string ArtifactsWithoutIds = "knowledge-artifacts-without-ids"`

A markdown file with frontmatter but no id cannot be a node.

**Remarks.** **Counted over this scope's OWN directory only**, since that is now the only markdown
it emits for. The residual: a directory whose markdown declares graph frontmatter and no
id is not a scope (nothing in it declares one), so nothing counts its files — where
before, an ancestor scope's recursive walk did. MEASURED on both corpora at the moment of
the change: TheTerrace has 209 markdown files in non-scope directories and ai-de 187, and
**zero of either** carry graph frontmatter without an id, so nothing observable is lost
today. Stated here rather than left silent (DC-025): if that number stops being zero the
fix is in DISCOVERY — such a directory is one that meant to hold knowledge — not another
recursive walk here, which is the thing this change exists to remove.

### `string LinkTargetOutsideWorkspace = "knowledge-prose-link-target-outside-workspace"`

BOUNDARY: a prose link points above the WORKSPACE root, where this product does not look.

**Remarks.** **Renamed from `knowledge-prose-link-target-outside-scope`, because the
boundary moved.** It used to mean "above this scope's directory", which fired 71 times
across 16 scopes on TheTerrace for links that a sibling scope could perfectly well
resolve — a boundary reported where there was none. Now that resolution reads the whole
workspace, the only place this reader genuinely cannot look is outside the workspace, and
that is what the disclosure says.





**It fires on NEITHER corpus, and that is measured rather than assumed** — 0 of
TheTerrace's 237 prose links and 0 of this repository's escape the workspace root. Kept,
and proved by fixture rather than by corpus, because a docs tree that links into a
sibling checkout is one commit away and this repository is itself worked in sibling
worktrees; the alternative is calling such a link a broken cross-reference, which is a
wrong number rather than a missing one (DC-016, DC-050).

### `string NestedDeclarationsNotAnalysed = "python-nested-declarations-not-analysed"`

Declarations nested deeper than a class's own body — closures, and definitions inside
methods.

**Remarks.** A class's METHODS are read now, as members. What remains is what a module cannot reach:
MEASURED across 113 Python files in two repositories, 42 closures and 12 classes declared
inside another class or a function. Counted rather than stated flatly, because "nested
declarations are not analysed" and "42 closures are not analysed" are different claims
about how much is missing (DC-050).

### `string DynamicImportsNotAnalysed = "typescript-dynamic-imports-not-analysed"`

Nothing but a static `import`/`export … from` statement is followed —
`import()`, `require()` in ANY form, and re-export globs are not.

**Remarks.** The wording used to say "require with a VARIABLE", which implied a literal
`require('fs')` was read. It never was. MEASURED on TheTerrace: two of the six
hand-written JavaScript files use CommonJS and nothing else, so the implication was false
about a third of the real corpus. Reading it would mean matching `require(` anywhere
in a file, which is the unanchored shape this reader has just been fixed for; when a
consumer needs CommonJS, the anchored statement form is the way to add it.

### `string NestedDeclarationsNotAnalysed = "typescript-nested-declarations-not-analysed"`

A declaration inside a function, a method or a namespace block — something no importer
can reach by name.

**Remarks.** A class's or interface's MEMBERS are read now, as members. What remains is what a module
cannot reach: MEASURED across 8 hand-written files in two repositories, **54** such
declarations, 27 in each repository and every one of them in the same shared file — a UMD
module whose entire body sits inside a factory function. Counted rather than stated
flatly, because "nested declarations are not analysed" and "27 functions in this one file
are not analysed" are different claims about how much is missing (DC-050), and because it
used to fire on all 13 of TheTerrace's TypeScript scopes when only 2 of them hide
anything (DC-025).

## `KnowledgeRecord`

*record* — `KnowledgeFrontmatter.cs`

What a knowledge artifact declares about itself in its frontmatter.

## `KnowledgeFrontmatter`

*class* — `KnowledgeFrontmatter.cs`

Reading the YAML-ish frontmatter that makes a markdown file a node in the knowledge graph.

**Remarks.** **Shared because two readers need the same answer.** The fixture reader has parsed this
since Phase 1; the knowledge reader parses it on real repositories. Two copies of a format parser
is two things to drift, and the drift would show as a document that is a node in one view and not
in the other.





**A subset reader, not a YAML parser.** The fields that carry graph structure — id, type,
owner, links — are read; everything else in the block is skipped. A YAML dependency would buy
anchors and flow mappings that this format does not use, and would make what the tool can see a
question about a package version.





`simplify: line-oriented frontmatter reading rather than YAML; ceiling is id, type, owner
and inline `- { to: …, rel: … }` links; upgrade trigger = a consumer needs nested or multi-line
values, or the format grows beyond what one line can express.`

| Member | Summary |
|---|---|
| `KnowledgeRecord? Read(IReadOnlyList<string> lines, out bool missingId)` | The record a file declares, or null when it is not a knowledge artifact. |

### `KnowledgeRecord? Read(IReadOnlyList<string> lines, out bool missingId)`

The record a file declares, or null when it is not a knowledge artifact.

- **`missingId`** — Set when a file HAS frontmatter but no id — a real defect in the document, distinct from an ordinary markdown file that was never meant to be a node. Collapsing the two would either report every README as broken or hide a document that meant to join the graph and cannot.

## `ModuleNaming`

*class* — `ModuleNaming.cs`

How a module-shaped scope names its files, so that two scopes cannot name the same node.

**Remarks.** **The defect this closes.** Both the Python and TypeScript extractors named a module by
its path RELATIVE TO ITS OWN SCOPE, and a scope is one directory. Every Python package has an
`__init__.py`, so a repository with five packages produced five scopes each declaring a
module called `__init__` — which is ONE node in the graph, carrying the merged edges of five
unrelated files. The same holds for `index.ts`, `main`, `setup` and
`conftest`. Nothing failed; the graph was simply wrong in a way no count could show.





**The id is the repository-relative path, without its extension.** Unique by
construction, readable, and the same string a person would type to open the file. It is also
what makes cross-scope resolution possible: an import naming a sibling package is a path from the
repository root, and now so is every module id.

| Member | Summary |
|---|---|
| `string ScopePrefix(string scopeId)` | The scope's own directory, relative to the repository root, from its id. |
| `string Qualify(string scopePrefix, string moduleWithinScope)` | The globally unique id of a module, from its path within the scope. |

### `string ScopePrefix(string scopeId)`

The scope's own directory, relative to the repository root, from its id.

**Remarks.** Discovery builds these ids as `python:<relative path>`, so the prefix is already
carried; deriving it here avoids threading the repository root through a contract that four
other extractors do not need.

## `NodeBuiltinModules`

*class* — `NodeBuiltinModules.cs`

Node.js's own built-in module names — the runtime this product does not index.

**Remarks.** **Why this exists.** The sibling of `ythonStandardLibrary`, added for the
same reason and against the same measurement. DC-050 is registered for a disclosure that merges
what the product *will not* read with what it *could not* read, and its residual-risk
line named this reader as the next place it would appear: *"TypeScript discloses 11 unresolved
specifiers and has had no equivalent look."*





**MEASURED on TheTerrace.** Of the specifiers that survived the precision fix, the ones
this reader could not resolve were `node:url`, `node:fs/promises` and
`@playwright/test` — Node's runtime twice and npm once. Counting them as "something this
scope does not contain" is arithmetically true and reads as a coverage hole; drawing them puts
`fs`, `path` and `url` among the most connected nodes in the graph, which is what
drawing `sys`, `os` and `json` did to Python's.





**Generated, not remembered.** Taken verbatim from `require('module').builtinModules`
on Node v24.18.0 — the runtime's own answer, the same discipline the Python list follows. A
hand-written list would be a guess about a set the runtime publishes.





**The runtime distinguishes two kinds and so does this.** Most builtins answer to a bare
specifier (`fs`, `path`) *and* to the reserved `node:` prefix.
`node:test`, `node:sqlite` and `node:sea` answer only to the prefix — they appear
in `builtinModules` WITH it — so a bare `test` or `sqlite` is a package on npm and
not this. Reading that distinction out of the runtime rather than assuming it is the difference
between a boundary and a wrong one: `sqlite` is a real npm package.





**It is a floor, not a promise.** A module added in a later Node is missing here and
falls back to being reported as unresolved, which is the safe direction — over-claiming would
hide a real unknown inside a name nobody checked. The `node:` prefix has no such problem: it
is reserved by the runtime, so nothing on npm can ever claim it and any name behind it is a
builtin by construction.

| Member | Summary |
|---|---|
| `string Prefix = "node:"` | The reserved scheme Node gives its own modules. Nothing on npm may use it. |
| `bool Contains(string specifier)` | Whether a module specifier names Node's own runtime. |

### `bool Contains(string specifier)`

Whether a module specifier names Node's own runtime.

**Remarks.** A `node:` specifier is one by construction. A bare one is matched on its top-level
segment. The known imprecision is stated rather than hidden: npm carries packages called
`path`, `util` and `events` (browser polyfills), and a repository importing one
of those is recorded here as importing Node. That is the same trade the Python list makes,
and it errs towards calling a boundary a boundary rather than towards inventing a gap.

## `PythonExtractor`

*class* — `PythonExtractor.cs`

Python modules, their top-level declarations, and what they import.

**Remarks.** **Six repositories disclosed unread Python before this existed.** The disclosure was the
right behaviour and it is not a substitute for reading the code — a graph that says "there is
Python here and I cannot see it" is honest and still blind.





**It reads structure, not semantics, and says so.** There is no Python compiler here:
this recognises module-level `import`, `from … import`, `class` and `def` at
column zero, and nothing else. Names are not resolved, so an import edge points at the module
PATH as written rather than at a symbol; a call graph is not attempted. Every one of those gaps
is a disclosure on the scope rather than a silence — the C# extractor's rule, applied to a
language where the gap is much wider.





**Why not a real parser.** The Solution-Selection Ladder asks for the smallest thing that
is still correct, and correct here means "does not assert what it cannot see". A dependency on a
Python grammar would buy type resolution this product has nowhere to put yet, and would make the
extractor's reach a question about a third-party package's version. When call edges or resolved
imports are actually wanted, that is the upgrade trigger.





`simplify: line-oriented recognition rather than a Python grammar; ceiling is top-level
declarations and import edges with unresolved targets; upgrade trigger = a consumer needs call
edges, resolved import targets, or anything nested inside a class or function.`

| Member | Summary |
|---|---|
| `string ScopeKind` | **(gap)** |
| `Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)` | **(gap)** |

## `Disclosures`

*class* — `PythonExtractor.cs`

Gaps this extractor always has, stated on every scope it produces.

| Member | Summary |
|---|---|
| `string ArtifactsWithoutIds = "knowledge-artifacts-without-ids"` | A markdown file with frontmatter but no id cannot be a node. |
| `string LinkTargetMissing = "knowledge-prose-link-target-missing"` | GAP: a prose link names a markdown file that is nowhere in the workspace. |
| `string LinkTargetNotANode = "knowledge-prose-link-target-not-a-node"` | BOUNDARY: a prose link resolves to a markdown file that declares no id. |
| `string LinkTargetOutsideWorkspace = "knowledge-prose-link-target-outside-workspace"` | BOUNDARY: a prose link points above the WORKSPACE root, where this product does not look. |
| `string HeadingsNotAnalysed = "knowledge-headings-not-analysed"` | BOUNDARY: a document's structure is counted, not extracted. |
| `string GlossaryTermsNotAnalysed = "knowledge-glossary-terms-not-analysed"` | BOUNDARY: a glossary's term definitions are counted as documents, not read. |
| `string InlineCodeNotResolved = "knowledge-inline-code-not-resolved"` | BOUNDARY: backticked identifiers are not matched against anything. |
| `string ImportsNotResolved = "python-imports-not-resolved"` | No name resolution: an import names a module path, not a symbol. |
| `string StandardLibraryNotIndexed = "python-standard-library-not-indexed"` | Imports naming the standard library — a boundary of the product, not a gap in it. |
| `string NestedDeclarationsNotAnalysed = "python-nested-declarations-not-analysed"` | Declarations nested deeper than a class's own body — closures, and definitions inside methods. |
| `string DynamicImportsNotAnalysed = "python-dynamic-imports-not-analysed"` | Nothing dynamic is followed — importlib, __import__, conditional imports. |
| `string RenamesNotFollowed = "sql-renames-not-followed"` | A rename is not followed, so the table or column keeps its earlier name. |
| `string DynamicDdlNotEvaluated = "sql-dynamic-ddl-not-evaluated"` | DDL inside a string literal — a message, or dynamic SQL nobody evaluated. |
| `string ColumnDetailNotRead = "sql-column-detail-not-read"` | Column types, constraints and indexes are not read. |
| `string NotTheDatabase = "sql-schema-from-files-not-database"` | This is the schema the FILES declare, not what a server holds. |
| `string TypesNotChecked = "typescript-types-not-checked"` | No type checking: an import names a module specifier, not a symbol. |
| `string NonExportedNotAnalysed = "typescript-non-exported-not-analysed"` | RETIRED. Kept only so the string has one home: stores written before this reader read non-exported declarations still carry it, and a test asserts it is no longer emitted. Disclosing a gap that has been closed is the … |
| `string DynamicImportsNotAnalysed = "typescript-dynamic-imports-not-analysed"` | Nothing but a static `import`/`export … from` statement is followed — `import()`, `require()` in ANY form, and re-export globs are not. |
| `string ExportsNotRecognised = "typescript-exports-not-recognised"` | An export whose spelling this reader does not know (DC-033's own alarm). |
| `string NodeBuiltinsNotIndexed = "typescript-node-builtins-not-indexed"` | An import naming Node's runtime — a boundary of the product, not a gap in it. |
| `string PackagesNotIndexed = "typescript-packages-not-indexed"` | An import naming an npm package — a boundary of the product, not a gap in it. |
| `string ImportsNotResolved = "typescript-imports-not-resolved"` | A specifier this scope does not contain and which nobody can identify. |
| `string GeneratedSourceNotRead = "typescript-generated-source-not-read"` | Bundled or generated JavaScript, skipped because nobody wrote it. |
| `string NestedDeclarationsNotAnalysed = "typescript-nested-declarations-not-analysed"` | A declaration inside a function, a method or a namespace block — something no importer can reach by name. |

### `string ArtifactsWithoutIds = "knowledge-artifacts-without-ids"`

A markdown file with frontmatter but no id cannot be a node.

**Remarks.** **Counted over this scope's OWN directory only**, since that is now the only markdown
it emits for. The residual: a directory whose markdown declares graph frontmatter and no
id is not a scope (nothing in it declares one), so nothing counts its files — where
before, an ancestor scope's recursive walk did. MEASURED on both corpora at the moment of
the change: TheTerrace has 209 markdown files in non-scope directories and ai-de 187, and
**zero of either** carry graph frontmatter without an id, so nothing observable is lost
today. Stated here rather than left silent (DC-025): if that number stops being zero the
fix is in DISCOVERY — such a directory is one that meant to hold knowledge — not another
recursive walk here, which is the thing this change exists to remove.

### `string LinkTargetOutsideWorkspace = "knowledge-prose-link-target-outside-workspace"`

BOUNDARY: a prose link points above the WORKSPACE root, where this product does not look.

**Remarks.** **Renamed from `knowledge-prose-link-target-outside-scope`, because the
boundary moved.** It used to mean "above this scope's directory", which fired 71 times
across 16 scopes on TheTerrace for links that a sibling scope could perfectly well
resolve — a boundary reported where there was none. Now that resolution reads the whole
workspace, the only place this reader genuinely cannot look is outside the workspace, and
that is what the disclosure says.





**It fires on NEITHER corpus, and that is measured rather than assumed** — 0 of
TheTerrace's 237 prose links and 0 of this repository's escape the workspace root. Kept,
and proved by fixture rather than by corpus, because a docs tree that links into a
sibling checkout is one commit away and this repository is itself worked in sibling
worktrees; the alternative is calling such a link a broken cross-reference, which is a
wrong number rather than a missing one (DC-016, DC-050).

### `string NestedDeclarationsNotAnalysed = "python-nested-declarations-not-analysed"`

Declarations nested deeper than a class's own body — closures, and definitions inside
methods.

**Remarks.** A class's METHODS are read now, as members. What remains is what a module cannot reach:
MEASURED across 113 Python files in two repositories, 42 closures and 12 classes declared
inside another class or a function. Counted rather than stated flatly, because "nested
declarations are not analysed" and "42 closures are not analysed" are different claims
about how much is missing (DC-050).

### `string DynamicImportsNotAnalysed = "typescript-dynamic-imports-not-analysed"`

Nothing but a static `import`/`export … from` statement is followed —
`import()`, `require()` in ANY form, and re-export globs are not.

**Remarks.** The wording used to say "require with a VARIABLE", which implied a literal
`require('fs')` was read. It never was. MEASURED on TheTerrace: two of the six
hand-written JavaScript files use CommonJS and nothing else, so the implication was false
about a third of the real corpus. Reading it would mean matching `require(` anywhere
in a file, which is the unanchored shape this reader has just been fixed for; when a
consumer needs CommonJS, the anchored statement form is the way to add it.

### `string NestedDeclarationsNotAnalysed = "typescript-nested-declarations-not-analysed"`

A declaration inside a function, a method or a namespace block — something no importer
can reach by name.

**Remarks.** A class's or interface's MEMBERS are read now, as members. What remains is what a module
cannot reach: MEASURED across 8 hand-written files in two repositories, **54** such
declarations, 27 in each repository and every one of them in the same shared file — a UMD
module whose entire body sits inside a factory function. Counted rather than stated
flatly, because "nested declarations are not analysed" and "27 functions in this one file
are not analysed" are different claims about how much is missing (DC-050), and because it
used to fire on all 13 of TheTerrace's TypeScript scopes when only 2 of them hide
anything (DC-025).

## `PythonStandardLibrary`

*class* — `PythonStandardLibrary.cs`

The Python standard library's top-level module names.

**Remarks.** **Why this exists.** An import that names something the repository does not contain was
counted as unresolved and disclosed as such. On a real workspace that produced
`python-imports-not-resolved (246 import(s) name something this scope does not contain)` —
which reads like a coverage hole and was treated as one, until the targets were measured:
**all 246, across all 32 distinct names, were the standard library** — sys, pathlib, json,
argparse, os, subprocess, urllib.





Nothing was wrong with the resolution. The number was arithmetically right and said
something false, which is the shape this codebase keeps meeting. `import sys` resolving to
nothing in the repository is not a gap in the graph any more than a C# file using
`System.String` is — the C# extractor already declines to draw the BCL for the same reason.





**Generated, not remembered.** Taken verbatim from `sys.stdlib_module_names` on
CPython 3.12.10 — the interpreter's own answer. A hand-written list would be a guess about a set
the runtime publishes.





Single-underscore internals (`_socket`, `_ast`) are dropped because nothing
imports them by that name. `__future__` is KEPT — dropping it cost **26 false unknowns**
on a real workspace, because the filter was written as "private names" and `__future__` is
the one module in the set that looks private and is imported constantly.





**It is a floor, not a promise.** A module added in a later Python is missing here and
falls back to being reported as unresolved, which is the safe direction: over-claiming would hide
a real unknown import inside a name nobody checked.

| Member | Summary |
|---|---|
| `bool Contains(string importTarget)` | Whether an import target names the standard library. |

### `bool Contains(string importTarget)`

Whether an import target names the standard library.

**Remarks.** Matched on the TOP-LEVEL package only, because `urllib.request` and
`importlib.util` are the standard library exactly as much as `urllib` is, and a set
of every submodule would be a set that goes stale one Python release at a time.

## `ScopeFingerprints`

*class* — `ScopeFingerprints.cs`

What a scope's inputs looked like the last time it was extracted successfully.

**Remarks.** **Every index re-extracted every scope.** On a real repository that is 4.5 seconds and
seven scopes, and it grows with the codebase — paid in full whether one file changed or none.
A fingerprint that has not moved means the evidence in the store is already the answer.





**A skip is reported, never disguised as work.** `IndexResult` counts reused scopes
separately from indexed ones, because "7 of 7 indexed" would be a true sentence about a run that
read nothing, and the operator's next question after a surprising graph is always "did it
actually look?".





**It fails towards re-extraction.** An unreadable directory, a missing sidecar, a
changed extractor version — every uncertainty produces a fingerprint that does not match, and the
scope is read again. The cost of an unnecessary extraction is seconds; the cost of a skipped one
is a graph that quietly describes code that no longer exists.

| Member | Summary |
|---|---|
| `string ExtractorGeneration = "2026-09-01.8"` | **(gap)** |
| `ScopeFingerprints Load(string dataDirectory)` | **(gap)** |
| `bool IsUnchanged(string scopeId, string fingerprint)` | True when this scope's inputs are byte-for-byte what they were when it last ran. |
| `void Record(string scopeId, string fingerprint)` | **(gap)** |
| `void Invalidate(string scopeId)` | Forgets a scope, so the next run reads it whatever the filesystem says. |
| `bool Reconcile(IEnumerable<string> discoveredScopeIds)` | Forgets every scope this run did not see, and reports whether the SET of scopes changed. |
| `IReadOnlyCollection<string> Known` | Scope ids this sidecar still remembers. For reporting what a run left behind. |
| `void Save()` | **(gap)** |
| `string Compute(string rootPath, ScopeDescriptor scope)` | A stable digest of a scope's input files: relative path, size and modification time. |

### `bool Reconcile(IEnumerable<string> discoveredScopeIds)`

Forgets every scope this run did not see, and reports whether the SET of scopes changed.

**Remarks.** **A project appearing is not a change to any existing scope.** Every per-scope
fingerprint can be identical while the workspace has gained a project, lost one, or had one
renamed — and a cache keyed only per scope would report "all reused" for a workspace whose
shape had changed underneath it.





Discovery runs on every index regardless, so a NEW scope is always extracted — it has
no fingerprint to match. The case this closes is the opposite one: a scope that has gone.
Its evidence would otherwise sit in the store forever, describing code that no longer exists,
with nothing to remove it and nothing to say so.

### `string Compute(string rootPath, ScopeDescriptor scope)`

A stable digest of a scope's input files: relative path, size and modification time.

**Remarks.** Not content hashes. Reading every byte of every file to decide whether to read every
byte of every file is a cache that costs what it saves. Path, length and mtime miss only an
edit that preserves both size and timestamp, which a tool does not do by accident.





Returns empty when the scope's inputs cannot be enumerated, and an empty fingerprint
never matches — so an unreadable scope is always re-read.

## `SourceRevision`

*class* — `SourceRevision.cs`

The revision a fact is stored under: the caller's artifact revision, plus the extractor
generation that read it.

**Remarks.** **Why a fact's identity includes its reader.** The store's natural key is
`(scope_id, artifact_revision, subject, predicate, object, extractor_id)` — P1-STORE-05,
"one revision, one answer". That was true while the extractor was fixed. It stopped being true
the moment extraction could improve for input that had not changed: the same bytes read by a
better reader are a DIFFERENT observation, and the key had no way to say so.





**What went wrong without it.** `ScopeFingerprints.ExtractorGeneration` was bumped
so an upgrade would invalidate every cached scope — and it did. But the reuse check inside
`RefreshScopeAsync` asks a second, independent question ("does the store already hold this
revision?"), knew nothing about the generation, and answered yes. So the re-index visited all 66
scopes and wrote nothing: *"Indexed 66 of 66 scope(s): 0 assertion(s)"*, with the Knowledge
chip still reading 0 on a repository holding 2,343 knowledge nodes.





Removing that second guard would not have been enough. Had it re-extracted, every unchanged
fact would have collided with the unique index, because the key genuinely could not represent the
new observation. The guard was the symptom; the key was the cause.





`simplify: the generation is carried in the revision STRING rather than its own column in
the natural key; ceiling is that a stored revision is no longer the caller's literal text, so
anything showing one to a person calls `ase` first; upgrade trigger = the store
gains migration machinery, at which point extractor_generation becomes a real column and this
type collapses to nothing.`

| Member | Summary |
|---|---|
| `string Stamp(string artifactRevision)` | The revision to STORE facts under. Idempotent: stamping an already-stamped revision returns it unchanged, so a caller that passes one through twice does not create a third identity. |
| `string Base(string revision)` | The revision to SHOW: the caller's own text, with any extractor stamp removed. |

### `string Base(string revision)`

The revision to SHOW: the caller's own text, with any extractor stamp removed.

**Remarks.** Strips any generation, not only the current one, because a surface routinely renders evidence
written by an older build — a stale scope's disclosure exists precisely for that case.

## `SourceText`

*class* — `SourceText.cs`

Removing the parts of a file that are commentary, before a line-oriented reader believes them.

**Remarks.** **Four readers were caught inventing on the same day.** A shared control fed each
extractor a corpus with no declarations and plenty of text SHAPED like declarations, and the SQL,
TypeScript and Python readers all reported things that existed only inside comments:
`table:Ghost` from `-- CREATE TABLE Ghost`, a class from
`/* export class Removed {} */`, a class from inside a docstring.





**Commented-out code is the worst possible input for a line-oriented reader**, because it
is real syntax — it was code, which is why it is shaped exactly like code. Every repository is
full of it. A fact read out of a comment is not a gap, it is a confident claim about something
that does not exist, and it arrives labelled Verified.





`simplify: character-scan comment removal rather than a lexer per language; ceiling is
line comments, block comments and quoted strings; upgrade trigger = a reader needs to know what
was inside a string as opposed to merely skipping it.`

| Member | Summary |
|---|---|
| `string WithoutCComments(` | The text with C-style comments blanked out, keeping every line and column. |
| `string WithoutPythonComments(string text)` | The text with `#` comments and triple-quoted blocks blanked out. |

### `string WithoutCComments(`

The text with C-style comments blanked out, keeping every line and column.

- **`blankStrings`** — Whether the CONTENTS of string literals are blanked too. A C-family reader wants them kept — a SQL statement lives inside one. A DDL reader wants them gone: `PRINT 'about to create table X'` names no table, and dynamic SQL is DDL this reader cannot evaluate, so it reads neither and discloses the count instead.
- **`singleQuotedStringsOnly`** — Whether only `'…'` delimits a string. In SQL `"…"` is a quoted IDENTIFIER — a table or column name — so blanking it deletes the very thing a schema reader is looking for. Blanking `"main"."Thing"` cost this reader a test, which is the cheapest possible way to find out that the two languages disagree about a quote character.

**Remarks.** Replaced with spaces rather than deleted so that line numbers, and therefore provenance, stay
true. A reader that reports the wrong line is a reader nobody can follow back to the source.

### `string WithoutPythonComments(string text)`

The text with `#` comments and triple-quoted blocks blanked out.

**Remarks.** A docstring is the Python case that matters: it holds example code at column zero, which is
precisely what the declaration reader looks for.

## `SqlSchemaExtractor`

*class* — `SqlSchemaExtractor.cs`

Tables declared in raw SQL, for repositories whose schema is not EF migrations.

**Remarks.** **Found by measuring a SECOND repository.** BioHacker declares its whole schema in
`src/Baseline.Sql/Schema/001-schema.sql` — eight `CREATE TABLE` statements in 197
lines — and the tool reported `sql-not-analysed (2 file(s))` and produced **zero** joins.
The disclosure was honest and the graph was still blind to the entire schema side of that
repository. Every measurement before it came from a codebase that happened to use EF.





**Same node shape as `fSchemaExtractor`, deliberately.** A table is
`table:Name` with `has_type table` and `has_column`, because the join projection
already reads that vocabulary — a second spelling for the same thing would be DC-022 with two
producers of one predicate, and the joins would silently see half the tables.





**The scripts are FOLDED, not just read.** A schema is the sum of its statements in
order. MEASURED: one repository carries 125 `ALTER TABLE … ADD` statements, so reading
`CREATE` alone would have shown its schema as it stood at the first migration and presented
that as current — the same defect the EF reader avoids by folding migrations. Drops are applied
too, because a column that no longer exists is a WRONG fact rather than a missing one.





`simplify: line-oriented recognition of CREATE TABLE, ALTER TABLE ADD/DROP COLUMN and
DROP TABLE, not a SQL grammar; ceiling is table and column NAMES; upgrade trigger = a consumer
needs column types, constraints, indexes, or renames followed.`

| Member | Summary |
|---|---|
| `string ScopeKind` | **(gap)** |
| `Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)` | **(gap)** |

## `Disclosures`

*class* — `SqlSchemaExtractor.cs`

Gaps this reader always has, stated on every scope it produces.

| Member | Summary |
|---|---|
| `string ArtifactsWithoutIds = "knowledge-artifacts-without-ids"` | A markdown file with frontmatter but no id cannot be a node. |
| `string LinkTargetMissing = "knowledge-prose-link-target-missing"` | GAP: a prose link names a markdown file that is nowhere in the workspace. |
| `string LinkTargetNotANode = "knowledge-prose-link-target-not-a-node"` | BOUNDARY: a prose link resolves to a markdown file that declares no id. |
| `string LinkTargetOutsideWorkspace = "knowledge-prose-link-target-outside-workspace"` | BOUNDARY: a prose link points above the WORKSPACE root, where this product does not look. |
| `string HeadingsNotAnalysed = "knowledge-headings-not-analysed"` | BOUNDARY: a document's structure is counted, not extracted. |
| `string GlossaryTermsNotAnalysed = "knowledge-glossary-terms-not-analysed"` | BOUNDARY: a glossary's term definitions are counted as documents, not read. |
| `string InlineCodeNotResolved = "knowledge-inline-code-not-resolved"` | BOUNDARY: backticked identifiers are not matched against anything. |
| `string ImportsNotResolved = "python-imports-not-resolved"` | No name resolution: an import names a module path, not a symbol. |
| `string StandardLibraryNotIndexed = "python-standard-library-not-indexed"` | Imports naming the standard library — a boundary of the product, not a gap in it. |
| `string NestedDeclarationsNotAnalysed = "python-nested-declarations-not-analysed"` | Declarations nested deeper than a class's own body — closures, and definitions inside methods. |
| `string DynamicImportsNotAnalysed = "python-dynamic-imports-not-analysed"` | Nothing dynamic is followed — importlib, __import__, conditional imports. |
| `string RenamesNotFollowed = "sql-renames-not-followed"` | A rename is not followed, so the table or column keeps its earlier name. |
| `string DynamicDdlNotEvaluated = "sql-dynamic-ddl-not-evaluated"` | DDL inside a string literal — a message, or dynamic SQL nobody evaluated. |
| `string ColumnDetailNotRead = "sql-column-detail-not-read"` | Column types, constraints and indexes are not read. |
| `string NotTheDatabase = "sql-schema-from-files-not-database"` | This is the schema the FILES declare, not what a server holds. |
| `string TypesNotChecked = "typescript-types-not-checked"` | No type checking: an import names a module specifier, not a symbol. |
| `string NonExportedNotAnalysed = "typescript-non-exported-not-analysed"` | RETIRED. Kept only so the string has one home: stores written before this reader read non-exported declarations still carry it, and a test asserts it is no longer emitted. Disclosing a gap that has been closed is the … |
| `string DynamicImportsNotAnalysed = "typescript-dynamic-imports-not-analysed"` | Nothing but a static `import`/`export … from` statement is followed — `import()`, `require()` in ANY form, and re-export globs are not. |
| `string ExportsNotRecognised = "typescript-exports-not-recognised"` | An export whose spelling this reader does not know (DC-033's own alarm). |
| `string NodeBuiltinsNotIndexed = "typescript-node-builtins-not-indexed"` | An import naming Node's runtime — a boundary of the product, not a gap in it. |
| `string PackagesNotIndexed = "typescript-packages-not-indexed"` | An import naming an npm package — a boundary of the product, not a gap in it. |
| `string ImportsNotResolved = "typescript-imports-not-resolved"` | A specifier this scope does not contain and which nobody can identify. |
| `string GeneratedSourceNotRead = "typescript-generated-source-not-read"` | Bundled or generated JavaScript, skipped because nobody wrote it. |
| `string NestedDeclarationsNotAnalysed = "typescript-nested-declarations-not-analysed"` | A declaration inside a function, a method or a namespace block — something no importer can reach by name. |

### `string ArtifactsWithoutIds = "knowledge-artifacts-without-ids"`

A markdown file with frontmatter but no id cannot be a node.

**Remarks.** **Counted over this scope's OWN directory only**, since that is now the only markdown
it emits for. The residual: a directory whose markdown declares graph frontmatter and no
id is not a scope (nothing in it declares one), so nothing counts its files — where
before, an ancestor scope's recursive walk did. MEASURED on both corpora at the moment of
the change: TheTerrace has 209 markdown files in non-scope directories and ai-de 187, and
**zero of either** carry graph frontmatter without an id, so nothing observable is lost
today. Stated here rather than left silent (DC-025): if that number stops being zero the
fix is in DISCOVERY — such a directory is one that meant to hold knowledge — not another
recursive walk here, which is the thing this change exists to remove.

### `string LinkTargetOutsideWorkspace = "knowledge-prose-link-target-outside-workspace"`

BOUNDARY: a prose link points above the WORKSPACE root, where this product does not look.

**Remarks.** **Renamed from `knowledge-prose-link-target-outside-scope`, because the
boundary moved.** It used to mean "above this scope's directory", which fired 71 times
across 16 scopes on TheTerrace for links that a sibling scope could perfectly well
resolve — a boundary reported where there was none. Now that resolution reads the whole
workspace, the only place this reader genuinely cannot look is outside the workspace, and
that is what the disclosure says.





**It fires on NEITHER corpus, and that is measured rather than assumed** — 0 of
TheTerrace's 237 prose links and 0 of this repository's escape the workspace root. Kept,
and proved by fixture rather than by corpus, because a docs tree that links into a
sibling checkout is one commit away and this repository is itself worked in sibling
worktrees; the alternative is calling such a link a broken cross-reference, which is a
wrong number rather than a missing one (DC-016, DC-050).

### `string NestedDeclarationsNotAnalysed = "python-nested-declarations-not-analysed"`

Declarations nested deeper than a class's own body — closures, and definitions inside
methods.

**Remarks.** A class's METHODS are read now, as members. What remains is what a module cannot reach:
MEASURED across 113 Python files in two repositories, 42 closures and 12 classes declared
inside another class or a function. Counted rather than stated flatly, because "nested
declarations are not analysed" and "42 closures are not analysed" are different claims
about how much is missing (DC-050).

### `string DynamicImportsNotAnalysed = "typescript-dynamic-imports-not-analysed"`

Nothing but a static `import`/`export … from` statement is followed —
`import()`, `require()` in ANY form, and re-export globs are not.

**Remarks.** The wording used to say "require with a VARIABLE", which implied a literal
`require('fs')` was read. It never was. MEASURED on TheTerrace: two of the six
hand-written JavaScript files use CommonJS and nothing else, so the implication was false
about a third of the real corpus. Reading it would mean matching `require(` anywhere
in a file, which is the unanchored shape this reader has just been fixed for; when a
consumer needs CommonJS, the anchored statement form is the way to add it.

### `string NestedDeclarationsNotAnalysed = "typescript-nested-declarations-not-analysed"`

A declaration inside a function, a method or a namespace block — something no importer
can reach by name.

**Remarks.** A class's or interface's MEMBERS are read now, as members. What remains is what a module
cannot reach: MEASURED across 8 hand-written files in two repositories, **54** such
declarations, 27 in each repository and every one of them in the same shared file — a UMD
module whose entire body sits inside a factory function. Counted rather than stated
flatly, because "nested declarations are not analysed" and "27 functions in this one file
are not analysed" are different claims about how much is missing (DC-050), and because it
used to fire on all 13 of TheTerrace's TypeScript scopes when only 2 of them hide
anything (DC-025).

## `SyntaxTreeCache`

*class* — `SyntaxTreeCache.cs`

Parsed syntax trees, reused across index runs for files that have not changed.

**Remarks.** **Built on a measurement, not a hunch.** Extraction was profiled twice: the READ phase is
~98% of a scope's time, and PARSING is ~97% of the read — 381–446ms of a 389–482ms read for 120
files, against 8–10ms to build the compilation and resolve references. Parsing source is therefore
about 96% of everything extraction does, and it is the only place a cache is worth having.





**This is file-granularity incremental.** The fingerprint cache already skips a scope
whose files are all unchanged; this covers the common case it cannot — one file edited in a
project of a hundred and twenty, where the scope must be re-read and 119 files did not move.





**Keyed by identity, not by content.** Path, length, modification time and the parse
options together. Hashing the bytes to decide whether to re-read the bytes is a cache that costs
what it saves. The failure mode of the weaker key is an edit that preserves both size and
timestamp, which a tool does not do by accident — the same trade the scope fingerprint makes, for
the same reason.





**A `yntaxTree` is immutable**, so sharing one between compilations and
threads is safe by construction rather than by convention.

| Member | Summary |
|---|---|
| `int Capacity = 20_000` | How many trees are held before the cache empties itself. |
| `int Hits { get; private set; }` | Trees served from the cache since it was created. For reporting the win, not tuning. |
| `int Misses { get; private set; }` | Trees parsed because they were absent or stale. |
| `SyntaxTree GetOrParse(string path, CSharpParseOptions parse, Func<string, SyntaxTree> factory)` | The parsed tree for a file, parsing it only if this exact file has not been seen. |

### `int Capacity = 20_000`

How many trees are held before the cache empties itself.

**Remarks.** A crude bound, deliberately. The alternative is an eviction policy tuned against a workload
nobody has measured, and the cost of being wrong here is one slow index rather than a defect.
Twenty thousand files is far beyond anything measured — the largest real workspace read so
far held about two thousand four hundred.

### `SyntaxTree GetOrParse(string path, CSharpParseOptions parse, Func<string, SyntaxTree> factory)`

The parsed tree for a file, parsing it only if this exact file has not been seen.

**Remarks.** takes part in the key: the same file compiled with different
preprocessor symbols is a different tree, and serving one for the other would put symbols in
the graph that the project does not define.

## `TypeScriptExtractor`

*class* — `TypeScriptExtractor.cs`

TypeScript and JavaScript modules, their top-level declarations, and what they import.

**Remarks.** **The largest remaining disclosure.** `typescript-not-analysed (165 file(s))` was the
biggest thing this tool said it could not see, on a repository where the C# half was fully
mapped.





**Structure, not semantics — the same bargain as Python.** There is no TypeScript
compiler here: this recognises `import`/`export` STATEMENTS, and column-zero `class`,
`interface`, `type`, `enum`, `function` and `namespace` whether or not they are exported, plus
the members declared directly in a class's or an interface's body. Types
are not checked, call graphs are not built, and a module specifier is resolved only when it names
a file this scope contains. Every gap is a disclosure on the scope.





**Members are named, not typed, and are an ATTRIBUTE.** A member renders as
`+ add()` or `- values` — no parameter or return types, because this reader holds a
line of text rather than a compiled symbol and `typescript-types-not-checked` is a standing
disclosure. `has_member` is registered in `EvidencePredicates.Attributes`, so forty members
on a type cost forty rows and nothing on the canvas.





**Precision before volume — MEASURED, and the reason this reader was rewritten.** On
TheTerrace it produced 14 import edges and **not one of them described a dependency between two
things in the repository**: ten named text that is not a specifier at all (a sentence from an
audit log, bundled help text, two spans of compiled JavaScript, and one code-generation template
that read exactly like a real npm dependency), and the two Verified ones were a module importing
itself. The cause was a `from '…'` matcher with no anchor — the `uses_table` defect in another
reader, where a keyword matched anywhere in a string turned "we update the record" into a table
called `the`. **An extractor that asserts something false is worse than one that asserts
nothing**, because the false fact arrives labelled and gets believed.





**What it will not read at all.** Build output (`bin`, `obj`, `artifacts`, `publish`),
and any file whose longest line says a machine wrote it. Both were measured: most of the 88
modules this reader produced were a vendored browser driver under a `bin/Debug/` tree, and every
invented specifier came out of a bundle or a generated data file. What is skipped is counted and
disclosed on the scope, because a skipped file is a boundary and a boundary needs a number.





**Why the same shape as PythonExtractor and not a shared base.** The two look alike and
are not the same: TypeScript's specifiers carry extensions and index files, its imports are
statements that may span lines, and JSX changes what a valid line looks like. A shared base would
have to be parameterised by every one of those, which is more machinery than either extractor
contains. If a third language arrives that fits the pattern, that is the moment the abstraction is
earned.





`simplify: line-oriented recognition rather than a TypeScript grammar; ceiling is
column-zero declarations, the members of a column-zero class or interface named without their
types, and static import/export statements resolved only within the workspace,
with npm and Node's runtime counted rather than drawn; upgrade trigger = a consumer needs type
relationships, call edges, member signatures, CommonJS `require`, tsconfig path aliases, or
anything declared inside a function or a namespace block.`

| Member | Summary |
|---|---|
| `string ScopeKind` | **(gap)** |

## `Disclosures`

*class* — `TypeScriptExtractor.cs`

Gaps this extractor always has, stated on every scope it produces.

| Member | Summary |
|---|---|
| `string ArtifactsWithoutIds = "knowledge-artifacts-without-ids"` | A markdown file with frontmatter but no id cannot be a node. |
| `string LinkTargetMissing = "knowledge-prose-link-target-missing"` | GAP: a prose link names a markdown file that is nowhere in the workspace. |
| `string LinkTargetNotANode = "knowledge-prose-link-target-not-a-node"` | BOUNDARY: a prose link resolves to a markdown file that declares no id. |
| `string LinkTargetOutsideWorkspace = "knowledge-prose-link-target-outside-workspace"` | BOUNDARY: a prose link points above the WORKSPACE root, where this product does not look. |
| `string HeadingsNotAnalysed = "knowledge-headings-not-analysed"` | BOUNDARY: a document's structure is counted, not extracted. |
| `string GlossaryTermsNotAnalysed = "knowledge-glossary-terms-not-analysed"` | BOUNDARY: a glossary's term definitions are counted as documents, not read. |
| `string InlineCodeNotResolved = "knowledge-inline-code-not-resolved"` | BOUNDARY: backticked identifiers are not matched against anything. |
| `string ImportsNotResolved = "python-imports-not-resolved"` | No name resolution: an import names a module path, not a symbol. |
| `string StandardLibraryNotIndexed = "python-standard-library-not-indexed"` | Imports naming the standard library — a boundary of the product, not a gap in it. |
| `string NestedDeclarationsNotAnalysed = "python-nested-declarations-not-analysed"` | Declarations nested deeper than a class's own body — closures, and definitions inside methods. |
| `string DynamicImportsNotAnalysed = "python-dynamic-imports-not-analysed"` | Nothing dynamic is followed — importlib, __import__, conditional imports. |
| `string RenamesNotFollowed = "sql-renames-not-followed"` | A rename is not followed, so the table or column keeps its earlier name. |
| `string DynamicDdlNotEvaluated = "sql-dynamic-ddl-not-evaluated"` | DDL inside a string literal — a message, or dynamic SQL nobody evaluated. |
| `string ColumnDetailNotRead = "sql-column-detail-not-read"` | Column types, constraints and indexes are not read. |
| `string NotTheDatabase = "sql-schema-from-files-not-database"` | This is the schema the FILES declare, not what a server holds. |
| `string TypesNotChecked = "typescript-types-not-checked"` | No type checking: an import names a module specifier, not a symbol. |
| `string NonExportedNotAnalysed = "typescript-non-exported-not-analysed"` | RETIRED. Kept only so the string has one home: stores written before this reader read non-exported declarations still carry it, and a test asserts it is no longer emitted. Disclosing a gap that has been closed is the … |
| `string DynamicImportsNotAnalysed = "typescript-dynamic-imports-not-analysed"` | Nothing but a static `import`/`export … from` statement is followed — `import()`, `require()` in ANY form, and re-export globs are not. |
| `string ExportsNotRecognised = "typescript-exports-not-recognised"` | An export whose spelling this reader does not know (DC-033's own alarm). |
| `string NodeBuiltinsNotIndexed = "typescript-node-builtins-not-indexed"` | An import naming Node's runtime — a boundary of the product, not a gap in it. |
| `string PackagesNotIndexed = "typescript-packages-not-indexed"` | An import naming an npm package — a boundary of the product, not a gap in it. |
| `string ImportsNotResolved = "typescript-imports-not-resolved"` | A specifier this scope does not contain and which nobody can identify. |
| `string GeneratedSourceNotRead = "typescript-generated-source-not-read"` | Bundled or generated JavaScript, skipped because nobody wrote it. |
| `string NestedDeclarationsNotAnalysed = "typescript-nested-declarations-not-analysed"` | A declaration inside a function, a method or a namespace block — something no importer can reach by name. |

### `string ArtifactsWithoutIds = "knowledge-artifacts-without-ids"`

A markdown file with frontmatter but no id cannot be a node.

**Remarks.** **Counted over this scope's OWN directory only**, since that is now the only markdown
it emits for. The residual: a directory whose markdown declares graph frontmatter and no
id is not a scope (nothing in it declares one), so nothing counts its files — where
before, an ancestor scope's recursive walk did. MEASURED on both corpora at the moment of
the change: TheTerrace has 209 markdown files in non-scope directories and ai-de 187, and
**zero of either** carry graph frontmatter without an id, so nothing observable is lost
today. Stated here rather than left silent (DC-025): if that number stops being zero the
fix is in DISCOVERY — such a directory is one that meant to hold knowledge — not another
recursive walk here, which is the thing this change exists to remove.

### `string LinkTargetOutsideWorkspace = "knowledge-prose-link-target-outside-workspace"`

BOUNDARY: a prose link points above the WORKSPACE root, where this product does not look.

**Remarks.** **Renamed from `knowledge-prose-link-target-outside-scope`, because the
boundary moved.** It used to mean "above this scope's directory", which fired 71 times
across 16 scopes on TheTerrace for links that a sibling scope could perfectly well
resolve — a boundary reported where there was none. Now that resolution reads the whole
workspace, the only place this reader genuinely cannot look is outside the workspace, and
that is what the disclosure says.





**It fires on NEITHER corpus, and that is measured rather than assumed** — 0 of
TheTerrace's 237 prose links and 0 of this repository's escape the workspace root. Kept,
and proved by fixture rather than by corpus, because a docs tree that links into a
sibling checkout is one commit away and this repository is itself worked in sibling
worktrees; the alternative is calling such a link a broken cross-reference, which is a
wrong number rather than a missing one (DC-016, DC-050).

### `string NestedDeclarationsNotAnalysed = "python-nested-declarations-not-analysed"`

Declarations nested deeper than a class's own body — closures, and definitions inside
methods.

**Remarks.** A class's METHODS are read now, as members. What remains is what a module cannot reach:
MEASURED across 113 Python files in two repositories, 42 closures and 12 classes declared
inside another class or a function. Counted rather than stated flatly, because "nested
declarations are not analysed" and "42 closures are not analysed" are different claims
about how much is missing (DC-050).

### `string DynamicImportsNotAnalysed = "typescript-dynamic-imports-not-analysed"`

Nothing but a static `import`/`export … from` statement is followed —
`import()`, `require()` in ANY form, and re-export globs are not.

**Remarks.** The wording used to say "require with a VARIABLE", which implied a literal
`require('fs')` was read. It never was. MEASURED on TheTerrace: two of the six
hand-written JavaScript files use CommonJS and nothing else, so the implication was false
about a third of the real corpus. Reading it would mean matching `require(` anywhere
in a file, which is the unanchored shape this reader has just been fixed for; when a
consumer needs CommonJS, the anchored statement form is the way to add it.

### `string NestedDeclarationsNotAnalysed = "typescript-nested-declarations-not-analysed"`

A declaration inside a function, a method or a namespace block — something no importer
can reach by name.

**Remarks.** A class's or interface's MEMBERS are read now, as members. What remains is what a module
cannot reach: MEASURED across 8 hand-written files in two repositories, **54** such
declarations, 27 in each repository and every one of them in the same shared file — a UMD
module whose entire body sits inside a factory function. Counted rather than stated
flatly, because "nested declarations are not analysed" and "27 functions in this one file
are not analysed" are different claims about how much is missing (DC-050), and because it
used to fire on all 13 of TheTerrace's TypeScript scopes when only 2 of them hide
anything (DC-025).

## `UnanalysedLanguages`

*class* — `UnanalysedLanguages.cs`

Source this build cannot read, counted so its absence from the graph is stated.

**Remarks.** **Measured on a fourth repository.** 63 Python files and 40 TypeScript files produced
`scopes: 0 of 0`, zero assertions and an **empty disclosure list**. Every number was
correct and the result was indistinguishable from an empty directory — "nothing here" and
"nothing I can read" rendered identically.





**This is the same class three repositories running.** A missing context map read as
perfect coverage; a bounded search read as the whole workspace; unreadable source reads as no
source. Each time the arithmetic was right and the claim was false, which is why none of them
could be fixed by counting more carefully.





**It names languages, never guesses at support.** Listing a language here is a statement
that files exist and were not read. It is not a roadmap, and the wording says so — a disclosure
that reads like a promise is a different kind of lie.

| Member | Summary |
|---|---|
| `IReadOnlyList<string> Survey(string rootPath)` | Disclosure strings for languages present in the workspace and not extracted. |

### `IReadOnlyList<string> Survey(string rootPath)`

Disclosure strings for languages present in the workspace and not extracted.

**Remarks.** The count is included because "some Python" and "10,760 Python files" are different
statements about how much of a repository the graph is silent on. Capped at a shallow-ish
walk depth by the skip list rather than by a limit, so the number is the real one.

## `WorkspaceExtractors`

*class* — `WorkspaceExtractors.cs`

What this product can extract, in one place.

**Remarks.** **Written because the composition was assembled by hand at every boundary, and the
boundaries disagreed.** The daemon composed C# and the fixture adapter only, so the running
application could not see infrastructure or schema at all — while a spike composed all four and
reported joins the product had no way to show. Two answers to "what does this tool read",
depending which entry point you asked.





**And the hand-written form is easy to get wrong quietly.** The same spike passed its
extractors POSITIONALLY, which put `icepExtractor` in the `fallback` slot and
routed every `bicep:` scope to the schema extractor. Both scopes failed, and the write-up
concluded the repository had no Bicep in it. It had two templates and 24 resource declarations.
A composition nobody can mis-order is worth more than a comment asking them not to.

| Member | Summary |
|---|---|
| `IExtractor Default()` | The composition every entry point uses. Named arguments, deliberately. |
| `IReadOnlyDictionary<string, string> RoutedKinds { get; } =` | The scope-id prefix each extractor answers for, as the router reads them. |

### `IReadOnlyDictionary<string, string> RoutedKinds { get; } =`

The scope-id prefix each extractor answers for, as the router reads them.

**Remarks.** Stated so a test can assert the routing rather than trusting the constructor's parameter
order — which is exactly what went wrong. Anything not listed falls through to the fallback.
