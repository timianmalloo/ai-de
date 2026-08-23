---
id: kb-extraction-sota
title: "Code & Infrastructure Extraction — state of the art"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [roslyn, scriptdom, dacfx, bicep, ts-morph, javaparser, tree-sitter]
links:
  - { to: kb-code-and-infra-extraction, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  What each extractor actually provides — Roslyn's three layers and its blind spots, the SQL
  parsing options and which need a live database, the supported Bicep API, and the
  cross-language tools with their real granularity.
---

# State of the art — static extraction

## C# / Roslyn

Three entry points, in descending abstraction:

**Workspace layer.** `MSBuildWorkspace` (`Microsoft.CodeAnalysis.Workspaces.MSBuild`, targeting `net472`
and `net10.0`) loads `.sln`/`.csproj` by running MSBuild design-time targets. It requires
`Microsoft.Build.Locator` — specifically `MSBuildLocator.RegisterDefaults()` **called before any MSBuild
type is loaded in the process**, which is a CLR assembly-loading constraint, so it cannot be done lazily and
must sit in a separate method from the consuming code. It raises `WorkspaceDiagnostic` events on load
failure that **must be checked explicitly** after `OpenSolutionAsync`. `AdhocWorkspace`
(`Microsoft.CodeAnalysis.Common`) is in-memory only: no filesystem, no SDK, no reference resolution.
*(Verified, [S1][S2])*

**Compilation layer.** `CSharpCompilation`, `SemanticModel`, `SyntaxTree`, reached via
`Project.GetCompilationAsync()`:
- `SemanticModel.GetSymbolInfo(node)` → `ISymbol` for any syntax node.
- `ISymbol.GetDocumentationCommentId()` → the canonical `T:` / `M:` / `P:` / `F:` / `E:` identifier.
- `ISymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)` → `global::Namespace.Type`.
- Incremental: `Compilation.WithReplacedSyntaxTree()` reuses unchanged declaration tables; workspace-level
  incrementality flows through the `Workspace.CurrentSolution` event cycle.

**Syntax layer.** `SyntaxTree.GetRoot()` without semantic binding — the right fallback when references
cannot be resolved. *(Verified, [S5][S6][S7])*

### What Roslyn cannot resolve statically

| Concern | Why | Accepted workaround, and its cost |
|---|---|---|
| DI registrations (`AddScoped<I,T>()`) | The service graph is not a language concept; registration is a runtime method chain | Syntax walker matching `AddScoped`/`AddTransient`/`AddSingleton` generic names — **misses dynamic and programmatic registration, silently** |
| Minimal-API routes (`app.MapGet("/path", …)`) | The route template is a string evaluated at runtime | Walker over `InvocationExpression` → `MemberAccess("Map*")` — **captures literals only** |
| MediatR handler discovery | Convention-based assembly scan at runtime | Walker for `IRequestHandler<,>` implementations — accurate for direct implementations only |
| EF Core `DbSet` membership | Model built by `OnModelCreating` plus conventions at runtime | `IDesignTimeDbContextFactory` + `dotnet ef dbcontext script` — **executes code** |
| EF Core `HasMany`/`HasOne` shape | Fluent API evaluated at runtime | Same |

*(Verified, [S5][S9])* .NET 8+ source generators shift *some* DI registration to compile time — but only for
source-gen-augmented patterns, and design-time builds do not run generators (see `open-questions.md`).

Partial classes are **not** a problem: `INamedTypeSymbol` aggregates all parts and `ISymbol.Locations`
covers every declaration site. *(Inferred from Roslyn design docs)*

## T-SQL and DDL

**ScriptDOM** (`Microsoft.SqlServer.TransactSql.ScriptDom`, open-sourced 2024 at
`github.com/microsoft/SqlScriptDOM`, MIT) parses T-SQL to a typed AST with a visitor API, **fully offline
from `.sql` files**, covering SQL Server 2022 syntax. It is grammar-driven, so genuinely new T-SQL syntax
lags until the grammar is extended; the repository documents the fix workflow. It does **not** parse MySQL,
PostgreSQL or ANSI SQL beyond the T-SQL dialect, and it performs **no cross-object resolution** — foreign
keys across files are not linked for you. *(Verified, [S12])*

**DacFx** (`Microsoft.SqlServer.DacFx`) is the richer model: a `.dacpac` contains a fully resolved object
model including cross-object references and column-level metadata, consumed offline via `DacPackage.Load`
and the `TSqlModel` API. But **producing** one with `DacServices.Extract()` requires a live connection. The
`.sqlproj` route (`Microsoft.Build.Sql` SDK) builds a `.dacpac` from `.sql` source at design time — whether
that itself needs a live server is **unverified**. *(Verified, [S13]; the `.sqlproj` question Flagged)*

**sqlglot** (Python, MIT, no dependencies) parses 30+ dialects to a Python AST offline, including DDL. It is
**intentionally lenient** — it will parse syntactically invalid SQL without raising — which makes it right
for fragment extraction and wrong for validation. *(Verified, [S14])*

**Requires a live database, therefore out of scope for artifact-only extraction:** **tbls** (Go; needs a DSN
such as `hostname:5432/dbname`), **SchemaSpy** and **SchemaCrawler** (both JDBC). **pgsql-parser /
libpg_query** wraps PostgreSQL's own parser — offline and excellent, PostgreSQL only. **SQLFluff** is a
linter with a parse API, not a schema-graph source. *(Verified for tbls [S15]; SchemaSpy/SchemaCrawler Inferred)*

## Azure Bicep and ARM

`bicep build` transpiles `.bicep` → ARM JSON entirely offline; `bicep build-params` handles `.bicepparam`;
`bicep decompile` is explicitly **lossy and best-effort** and must not be treated as a source of truth.

Two programmatic paths, and the difference matters:

| Path | Status |
|---|---|
| `Azure.Bicep.Core` NuGet (targets `net10.0`) | **Unsupported.** Package description, verbatim: *"While it is public, it is not a supported package. Any dependency you take on this package will be done at your own risk and we reserve the right to push breaking changes to this package at any time."* |
| **`bicep jsonrpc`** | **Supported and stable since v0.29.45.** JSON-RPC 2.0 over stdio with `Content-Length: N\r\n\r\n{message}\r\n\r\n` framing. Methods include `bicep/compile`, `bicep/version`, `bicep/getFileReferences`. The schema is declared backwards-compatible: fields may be added, never removed or renamed |

*(Verified, [S10][S11])*

**The compiled ARM JSON is the correct extraction input**, because the compiler materialises implicit
dependencies — any property reference to another resource — into explicit `dependsOn`. Parsing `.bicep`
directly sees fewer edges than the compiler emits. *(Verified, [S11])*

**Terraform**, for contrast: `terraform graph` emits DOT but requires `terraform init` (provider download).
Offline HCL parsing via `hashicorp/hcl/v2` gives resource types, labels and references but not
provider-defined attribute schemas. *(Inferred)*

## Other languages

| Language | Tool | Offline? | Real granularity |
|---|---|---|---|
| TypeScript/JS | **ts-morph** (npm; wraps the TS compiler API) | ✅ from `tsconfig.json` or globs | Classes, interfaces, functions, imports/exports, **type resolution**; `compilerNode` escapes to raw `ts.*`. Dynamic JS patterns invisible |
| Java | **JavaParser** `com.github.javaparser:javaparser-symbol-solver-core:3.28.2` (LGPL-3 / Apache-2.0) | ✅ from `.java`, needs classpath for resolution | Java 1–25 syntax; `JavaSymbolSolver` for symbols; **resolution degrades without a complete classpath**. **Spoon** sits higher, on Eclipse JDT |
| Python | `ast` (stdlib), **griffe**, **pyreverse** (pylint) | ✅ | AST offline with no imports; griffe for API surfaces incl. annotations; pyreverse for UML-style class/package diagrams |
| Rust | `cargo metadata --format-version 1`, **cargo-modules** | ✅ | Workspace/crate/dependency/feature graph; module structure. `rust-analyzer` indexes in-memory but is not a batch extractor |
| Any | **tree-sitter** (C runtime, 100+ grammars) | ✅ | Error-resilient **CST**; partial trees on broken syntax; fast enough for keystrokes. **No name resolution, no types, no cross-file references** |
| Any | **SCIP indexers** (`scip-typescript`, `scip-java`, `scip-python`) | TS ✅; Java needs compilation; Python needs deps installed | Occurrence positions, `SymbolInformation`, relationships (implements, overrides). **No official C# indexer as of 2026-08-23** |

*(Verified, [S8][S17][S18][S19])*

## Normalised output formats — what each is actually for

| Format | Actually for | Suitable as our delta format? |
|---|---|---|
| **SCIP** (Protobuf) | Symbol definitions, occurrences, cross-symbol relationships | Closest existing fit; snapshot/append index, **not** a mutable delta |
| **LSIF** (JSON-lines, spec 0.6.0) | Caching **LSP results** — hover strings, definition ranges | **No.** Its own spec: *"doesn't contain any program symbol information nor does the LSIF define any symbol semantics… therefore doesn't define a symbol database"* |
| **Kythe entries** | Cross-language facts with VName identity | Conceptually yes; heavyweight |
| **CodeQL dbscheme** | Its own analysis database | No — internal |
| **SARIF** | Static-analysis **findings** | No — a different thing entirely |
| **CycloneDX / SPDX** | Dependency and licence **inventory** (SBOM) | No — dependency facts only |
| **OpenAPI** | HTTP API contracts | Useful *as an input* for endpoint facts |

*(Verified, [S16][S18])*

## The frontier

- **No official `scip-dotnet`.** The one gap that would otherwise let us skip writing a Roslyn extractor.
- **Source generators versus design-time builds** — the generated code that would make DI statically visible
  is exactly the code a design-time build does not produce.
- **No standard "graph delta of code facts" format exists.** SCIP and Kythe are snapshot indices; the
  scope-snapshot-then-diff approach in the seed architecture is a reasonable invention precisely because
  nothing standard covers it.
