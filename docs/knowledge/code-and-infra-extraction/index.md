---
id: kb-code-and-infra-extraction
title: "Code & Infrastructure Extraction — domain knowledge"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [roslyn, scriptdom, bicep, ts-morph, tree-sitter, scip, extraction]
links:
  - { to: knowledge-hub, rel: refines }
  - { to: seed-ai-native-ide-sketch, rel: relates-to }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Evidence base for artifact-only extractors: what Roslyn, ScriptDOM and Bicep can recover
  statically, the three C# patterns (DI, routes, ORM mapping) that static analysis structurally
  cannot see, and the supported-versus-unsupported programmatic APIs.
---

# Code & Infrastructure Extraction — domain knowledge

**Domain & problem:** AI-DE's supply chain is a set of standalone extractor CLIs that read **only**
repository artifacts — source, Bicep, DDL — and emit normalised JSON graph deltas with provenance. No live
database, no running application.

**Canonical framing:** The field splits this into **compiler-backed extraction** (Roslyn, the TypeScript
compiler API, JavaParser's symbol solver — accurate, slow, needs a resolvable build) and **grammar-based
extraction** (tree-sitter, sqlglot — fast, error-resilient, semantically blind). Our framing matches, with
one addition the field does not have: we extract across *four different artifact kinds* (C#, Bicep, DDL,
traces) and join them, which is where the interesting failures live.

**Compiled:** 2026-08-23 · **Lead:** Domain Researcher · **Status:** fresh

*(`data-and-constants.md` is folded into `references.md` §"Packages, versions and APIs" — the constants
here are package IDs, API names and measured load times.)*

## Headline findings

1. **`MSBuildWorkspace` is the only Roslyn workspace that respects real project configuration, and its
   registration constraint is a CLR loading rule, not a convention.** `MSBuildLocator.RegisterDefaults()`
   must be called **before any MSBuild type is loaded in the process** — which means it cannot be done
   lazily and must live in a different method from the code that uses MSBuild types. `AdhocWorkspace`
   resolves no SDK, no NuGet, no multi-targeting. — *(Verified, [S1][S2])*
2. **Cold solution load is minutes, and that shapes the daemon's architecture.** `OpenSolutionAsync` performs
   a **design-time build per project, serially by default**, and does not reuse Visual Studio's cache.
   Reported: **~1.5 minutes for 300 projects**; **~4 minutes for Roslyn's own ~60-project solution**.
   `SkipUnrecognizedProjects = true` prevents hangs on unknown project types — at the cost of silently
   excluding valid but unusual ones. — *(Verified, [S3][S4])*
3. **`ISymbol.GetDocumentationCommentId()` is the canonical stable symbol key, and it already exists.**
   It produces the XML-doc standard form — `T:Namespace.Type`, `M:Namespace.Type.Method(System.String)`,
   `P:`, `F:`, `E:`. Use `symbol.OriginalDefinition.GetDocumentationCommentId()` for generics to avoid
   parameterisation artefacts; the static `DocumentationCommentId.CreateDeclarationId(ISymbol)` yields the
   same string. This is a better default than inventing `cs:{FQN}`. — *(Verified, [S5][S6])*
4. **Three high-value C# patterns are structurally invisible to static analysis**, and this is the single
   most important boundary in this domain: **DI registrations** (`AddScoped<I,T>`), **ASP.NET route
   mapping**, and **EF Core model shape**. All three are built by *running code* — lambdas, conventions,
   assembly scans, fluent chains. Syntax-walker approximations work for the literal cases and miss the
   dynamic ones, silently. — *(Verified, [S5][S9])*
5. **`Azure.Bicep.Core` is explicitly unsupported for third-party use**, in Microsoft's own words on the
   package: *"While it is public, it is not a supported package. Any dependency you take on this package
   will be done at your own risk and we reserve the right to push breaking changes to this package at any
   time."* The **supported** programmatic path is `bicep jsonrpc` — JSON-RPC 2.0 over stdio with
   `Content-Length` framing, stable since **v0.29.45**, with a documented backwards-compatibility promise. — *(Verified, [S10][S11])*
6. **Extract from ARM JSON, not from Bicep source.** The compiler resolves implicit dependencies (a property
   reference to another resource) into explicit `dependsOn` entries in the ARM output. The Bicep source has
   *fewer* explicit edges than its own compiled form. — *(Verified, [S11])*
7. **ScriptDOM is open source and works fully offline from `.sql` files.** Now at
   `github.com/microsoft/SqlScriptDOM` under MIT, producing a typed T-SQL AST with a visitor API, covering
   SQL Server 2022 syntax. **DacFx** is richer — a `.dacpac` carries a fully resolved object model with
   cross-object references — but `DacServices.Extract()` needs a **live database**; consuming an existing
   `.dacpac` via `DacPackage.Load` is offline. — *(Verified, [S12][S13])*
8. **LSIF is the wrong format to adopt, by its own specification.** It states that the dump *"doesn't
   contain any program symbol information nor does the LSIF define any symbol semantics… the LSIF therefore
   doesn't define a symbol database"* — it caches LSP *results* (hover strings, definition ranges), not a
   semantic graph. SCIP is the correct reference format. — *(Verified, quoted, [S16])*
9. **There is no official `scip-dotnet` indexer** in the Sourcegraph ecosystem as of 2026-08-23. Community
   tools exist and are not listed on the official indexer page. This directly qualifies the
   code-knowledge-graph base's suggestion to consume SCIP for C# — **verify before depending on it.** — *(Flagged, [S17])*
10. **Design-time builds do not run source generators.** Files injected by generators — Mapperly, EF
    compiled models, the .NET 8+ DI source generator — are **absent from the semantic model** unless the
    project was built first and the generated files written to disk. Since source generators are exactly
    where some DI registrations become statically visible, this is a compounding blind spot. — *(Flagged — known limitation; the per-generator workaround needs testing, [S5])*

## Confidence summary

Verified: all package IDs and API names, the `Azure.Bicep.Core` support disclaimer (quoted verbatim), the
LSIF self-description (quoted), ScriptDOM's licence and offline capability, the measured MSBuildWorkspace
load times, `GetDocumentationCommentId` semantics, `bicep jsonrpc`'s stability and wire format, and the
static-analysis boundary for DI/routes/EF. Inferred: Terraform HCL offline parsing limits; SchemaSpy and
SchemaCrawler's JDBC requirement. Flagged: multi-targeting behaviour in `MSBuildWorkspace` (community
consensus, no definitive doc page); source-generator workarounds; whether `Microsoft.Build.Sql` can build a
`.dacpac` without a live SQL Server; `bicep jsonrpc` process lifetime; **the absence of an official
`scip-dotnet` indexer**.

**Load-bearing Flagged claims:** the missing `scip-dotnet` indexer (it decides whether we consume SCIP or
write a Roslyn extractor) and the source-generator gap (it decides whether the DI graph is recoverable at
all). Both are one spike each.

## Design implications

- **Keep the Roslyn workspace warm in the daemon.** A 1.5–4 minute cold load is fatal to a save-to-refresh
  loop and irrelevant if it happens once at startup. This is the strongest architectural consequence in
  this base.
- **Use `GetDocumentationCommentId()` as the C# node ID**, on `OriginalDefinition`. It is canonical,
  deterministic, human-readable in a diff, and it is what XML documentation already uses — so it joins
  naturally to generated API docs.
- **Be explicit that DI, routes and EF mapping are approximations.** Emit those edges with a confidence
  attribute and the evidence (the syntax node matched), never as though they were resolved facts. A wrong
  `PERSISTS_TO` edge is worse than a missing one because nothing will contradict it.
- **For EF specifically, prefer the design-time path over the syntax walker.** `IDesignTimeDbContextFactory`
  plus `dotnet ef dbcontext script` gives the *authoritative* model — at the cost that it **executes code**.
  That is a legitimate trade, and it must be labelled: this extractor is not artifact-only.
- **Call `bicep build` (or `bicep jsonrpc`), never reference `Azure.Bicep.Core`.** The unsupported package
  is the tempting shortcut and it is a documented breaking-change risk.
- **Choose the SQL path by what the repository actually contains.** `.sql` migration files → ScriptDOM
  (offline, T-SQL only). A `.sqlproj`/`.dacpac` → DacFx's `TSqlModel` (richer, resolved cross-object
  references). Non-T-SQL dialects → sqlglot, accepting that it is deliberately lenient and validates nothing.
- **Adopt tree-sitter only as the broken-build fallback.** It cannot distinguish `AddScoped<IFoo, Foo>()`
  from `AddScoped<SomethingElse>()` because it does no name resolution — for a .NET-first tool, Roslyn's
  semantic model is the correct primary even with the load cost.
- **Check `WorkspaceDiagnostic` after every load and surface failures as graph findings.** A partially
  loaded solution produces a well-formed, quietly incomplete graph.
- **Spike `scip-dotnet` and the source-generator gap before committing the extractor design.** Two days that
  could remove or reshape a large piece of planned work.

## How to use this base

Personas and the design skills cite these files as evidence (BoK §III.1). The package IDs, API names and the
quoted support disclaimers in `references.md` are the ones to quote rather than recall. Refresh when Roslyn
or Bicep ship a major version, or when an official `scip-dotnet` appears.
