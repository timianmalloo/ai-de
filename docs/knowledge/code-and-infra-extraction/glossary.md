---
id: kb-extraction-glossary
title: "Code & Infrastructure Extraction — glossary"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [glossary, roslyn, sql, bicep, ubiquitous-language]
links:
  - { to: kb-code-and-infra-extraction, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Precise definitions for extraction vocabulary — design-time build, DocumentationCommentId,
  CST versus AST, dacpac, scope — so extractors, the graph and the docs use one word per concept.
---

# Glossary — ubiquitous language

| Term | Definition |
|---|---|
| **AdhocWorkspace** | An in-memory Roslyn workspace with no project file, SDK resolution or NuGet resolution. For snippets and tests. *(Verified, [S1])* |
| **ARM template** | The Azure Resource Manager JSON deployment format Bicep compiles to. **The correct extraction input**, because the compiler has already materialised implicit `dependsOn`. *(Verified, [S11])* |
| **`bicep jsonrpc`** | The **supported** programmatic Bicep API: JSON-RPC 2.0 over CLI stdio with `Content-Length` framing, stable since v0.29.45, with a documented backwards-compatibility promise. *(Verified, [S11])* |
| **CST vs AST** | A **concrete** syntax tree keeps every token including trivia (tree-sitter); an **abstract** syntax tree keeps the semantically significant structure (Roslyn, ScriptDOM). Only the latter is a basis for semantic resolution. |
| **dacpac** | A DacFx data-tier application package — an OPC/ZIP container holding a **fully resolved** SQL object model with cross-object references. Consumable offline; **producing one by `Extract` needs a live database**. *(Verified, [S13])* |
| **Design-time build** | An MSBuild evaluation resolving project properties and item groups **without producing binaries** — what `MSBuildWorkspace` runs per project. **Does not run source generators.** *(Verified, [S1]; the generator gap Flagged)* |
| **DocumentationCommentId** | The canonical XML-doc identifier for a C# symbol — `T:Namespace.Type`, `M:Namespace.Type.Method(System.String)`. Obtained via `ISymbol.GetDocumentationCommentId()`; use `OriginalDefinition` for generics. **The right stable node key for C#.** *(Verified, [S5][S6])* |
| **Graph delta** | Our term (not an industry standard) for an incremental change set of nodes and edges with provenance. No published format covers it — SCIP and Kythe are snapshot indices. |
| **`IDesignTimeDbContextFactory`** | The EF Core interface letting tooling construct a `DbContext` without the application host. The authoritative route to the EF model — and it **executes code**. *(Verified, [S9])* |
| **`ISymbol`** | Roslyn's interface for any named program element — namespace, type, method, parameter. *(Verified, [S5])* |
| **LSIF** | Language Server Index Format — a cache of **LSP results**, not a symbol database, by its own specification. Superseded by SCIP for code-fact indexing. *(Verified, quoted, [S16])* |
| **`MSBuildLocator`** | The shim that must register an MSBuild instance **before any MSBuild type loads in the process** — a CLR assembly-loading constraint, hence the separate-method requirement. *(Verified, [S2])* |
| **`MSBuildWorkspace`** | The Roslyn workspace that loads real `.sln`/`.csproj` by running design-time targets. The only one that respects actual project configuration. *(Verified, [S1])* |
| **SARIF** | Static Analysis Results Interchange Format — for **findings**, not structure. |
| **Scope** (extraction) | The unit of replacement: an extractor emits a full snapshot per scope, and the daemon deletes then re-inserts within it, making re-runs idempotent and diffs free. *(Our term, from the seed architecture)* |
| **SCIP** | Code Intelligence Protocol — Protobuf index of definitions, occurrences and relationships with human-readable symbol IDs. **No official C# indexer as of 2026-08-23.** *(Verified/Flagged, [S18])* |
| **ScriptDOM** | Microsoft's T-SQL AST parser, MIT and open source, **fully offline from `.sql` files**, with a typed visitor API. *(Verified, [S12])* |
| **`SymbolDisplayFormat`** | Roslyn's control over symbol-to-string formatting; `FullyQualifiedFormat` yields `global::Namespace.Type`. *(Verified, [S7])* |
| **tree-sitter** | Language-agnostic, error-resilient CST parsing with a C runtime and 100+ grammars. **No name resolution, no types.** The broken-build fallback, never the primary for a typed language. *(Verified, [S19])* |
| **`WorkspaceDiagnostic`** | The event Roslyn raises on load failure. **Must be checked explicitly** after `OpenSolutionAsync` — otherwise a partial load produces a quietly incomplete graph. *(Verified, [S1])* |
