---
id: kb-extraction-references
title: "Code & Infrastructure Extraction — references, packages, versions and APIs"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [reference, nuget, api-names, versions, licences]
links:
  - { to: kb-code-and-infra-extraction, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Every package ID, API name, version, licence and measured timing in this domain, read from
  NuGet, npm, Maven or official documentation — including the two verbatim disclaimers that
  decide which APIs may be used.
---

# Reference information

## Specifications

- **LSIF 0.6.0** — Language Server Index Format. Its own scope statement, quoted: *"The data stored is
  result data usually returned from a LSP request. The dump doesn't contain any program symbol information
  nor does the LSIF define any symbol semantics… the LSIF therefore doesn't define a symbol database."*
  *(Verified, [S16])*
- **SCIP** — Code Intelligence Protocol, Protobuf; repository `github.com/scip-code/scip` (formerly
  `sourcegraph/scip`). *(Verified, [S18])*
- **ARM template schema** — `https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#`.
  *(Verified, [S11])*
- **Bicep JSON-RPC** — JSON-RPC 2.0 over stdio with `Content-Length: N\r\n\r\n{message}\r\n\r\n` framing.
  *(Verified, [S11])*

## Packages, versions and APIs

| Item | Value | Source |
|---|---|---|
| `Microsoft.CodeAnalysis.Common` | **4.14.0** (moniker `roslyn-dotnet-5.3.0`) | NuGet |
| `Microsoft.CodeAnalysis.Workspaces.MSBuild` targets | `net472`, `net10.0` | NuGet |
| `Microsoft.Build.Locator` requirement | `MSBuildLocator.RegisterDefaults()` **before any MSBuild type is loaded in the process**; must be in a separate method | NuGet package page |
| `ISymbol` | `Microsoft.CodeAnalysis`, in `Microsoft.CodeAnalysis.dll` | learn.microsoft.com |
| `SymbolDisplayFormat` | `Microsoft.CodeAnalysis.SymbolDisplayFormat` | learn.microsoft.com |
| Canonical symbol ID API | `ISymbol.GetDocumentationCommentId()` → `string?`; static `DocumentationCommentId.CreateDeclarationId(ISymbol)` | learn.microsoft.com + roslyn source |
| DocCommentId forms | `T:Namespace.Type` · `M:Namespace.Type.Method(System.String)` · `P:` · `F:` · `E:` | Roslyn XML doc spec |
| `SymbolDisplayFormat.FullyQualifiedFormat` | produces `global::Namespace.Type` | Roslyn docs |
| **MSBuildWorkspace — 300 projects** | **~1.5 minutes** | dotnet/roslyn#14325 |
| **MSBuildWorkspace — Roslyn repo (~60 projects)** | **~4 minutes** | dotnet/roslyn#23823 |
| Hang mitigation | `SkipUnrecognizedProjects = true` | roslyn issue discussions |
| `Microsoft.SqlServer.TransactSql.ScriptDom` | open-sourced at `github.com/microsoft/SqlScriptDOM`, **MIT** | GitHub |
| `Microsoft.SqlServer.DacFx` | NuGet ID as written; `DacPackage.Load` offline, `DacServices.Extract()` **needs a live DB** | NuGet + learn.microsoft.com |
| `.sqlproj` SDK | `Microsoft.Build.Sql` | NuGet |
| `Azure.Bicep.Core` target | `net10.0` only | NuGet |
| **`Azure.Bicep.Core` support status** | **"While it is public, it is not a supported package. Any dependency you take on this package will be done at your own risk and we reserve the right to push breaking changes to this package at any time."** *(verbatim)* | NuGet package description |
| `bicep jsonrpc` stable since | **v0.29.45** | learn.microsoft.com |
| `bicep jsonrpc` methods | `bicep/compile`, `bicep/version`, `bicep/getFileReferences` | learn.microsoft.com |
| `ts-morph` | npm `ts-morph` (formerly `ts-simple-ast`) | npm |
| JavaParser | `com.github.javaparser:javaparser-symbol-solver-core:3.28.2`, **LGPL-3 / Apache-2.0** | GitHub |
| sqlglot | MIT, pure Python, **no dependencies**, **30+ dialects** | GitHub |
| SCIP repository | `github.com/scip-code/scip` | GitHub |
| LSIF spec version | **0.6.0** | microsoft.github.io |
| EF design package | `Microsoft.EntityFrameworkCore.Design` | learn.microsoft.com |
| EF model precedence | Conventions → Data Annotations → **Fluent API (`OnModelCreating`) highest** | learn.microsoft.com |
| tbls | Go; **requires a live DSN** (`hostname:5432/dbname`) | GitHub |
| tree-sitter | C runtime; 100+ grammars; error-resilient CST; **no name resolution** | GitHub |

*(All Verified at 2026-08-23 from the source named in the right-hand column.)*

## The two disclaimers that decide API choices

1. **`Azure.Bicep.Core`** — quoted in full above. Use `bicep build` or `bicep jsonrpc` instead.
2. **LSIF** — quoted in full above. It is an LSP-result cache, not a symbol database; SCIP is the format to
   look at for a code-fact index.

## Static-analysis boundary — the authoritative list

Recoverable statically from Roslyn: types, members, inheritance, project references, NuGet dependencies,
attributes, XML documentation, partial-class aggregation (`INamedTypeSymbol` merges parts,
`ISymbol.Locations` covers all sites), generic instantiation.

**Not** recoverable statically, with the workaround and its cost:

| Concern | Workaround | What it costs |
|---|---|---|
| DI registrations | syntax walker on `AddScoped`/`AddTransient`/`AddSingleton` | misses dynamic/programmatic registration **silently** |
| Minimal-API routes | walker on `Map*` invocations | literal string templates only |
| MediatR handlers | walker for `IRequestHandler<,>` | direct implementations only |
| EF `DbSet` / relationships | `IDesignTimeDbContextFactory` + `dotnet ef dbcontext script` | **executes code** — no longer artifact-only |

*(Verified, [S5][S9])*
