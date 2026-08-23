---
id: kb-extraction-sources
title: "Code & Infrastructure Extraction — sources"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [sources, citations]
links:
  - { to: kb-code-and-infra-extraction, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The access-dated source list behind the extraction knowledge base, keyed [S1]..[S19], with
  package IDs and API names read from registries rather than recalled.
---

# Sources

All accessed **2026-08-23**. Citation keys `[Sn]` are used throughout this topic.

| # | Title | Type | URL | Used for |
|---|---|---|---|---|
| S1 | Roslyn SDK — work with a workspace | primary (vendor docs) | https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/work-with-workspace | `MSBuildWorkspace` vs `AdhocWorkspace`, `WorkspaceDiagnostic` |
| S2 | `Microsoft.Build.Locator` on NuGet | primary (registry) | https://www.nuget.org/packages/Microsoft.Build.Locator | The register-before-any-MSBuild-type constraint |
| S3 | dotnet/roslyn issue #14325 | primary (repo issue) | https://github.com/dotnet/roslyn/issues/14325 | **~1.5 min for 300 projects** |
| S4 | dotnet/roslyn issue #23823 | primary (repo issue) | https://github.com/dotnet/roslyn/issues/23823 | **~4 min for the Roslyn solution**; `SkipUnrecognizedProjects` |
| S5 | `ISymbol` API reference | primary (vendor docs) | https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol | `GetDocumentationCommentId()`, symbol model, static-analysis boundary |
| S6 | `DocumentationCommentId` (Roslyn source + docs) | primary | https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis | `CreateDeclarationId`, ID format |
| S7 | `SymbolDisplayFormat` API reference | primary | https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.symboldisplayformat | `FullyQualifiedFormat` |
| S8 | `ts-morph` on npm | primary (registry) | https://www.npmjs.com/package/ts-morph | Offline TS analysis, `compilerNode` escape hatch |
| S9 | EF Core CLI + modeling docs | primary (vendor docs) | https://learn.microsoft.com/en-us/ef/core/cli/dotnet · https://learn.microsoft.com/en-us/ef/core/modeling/ | `dotnet ef dbcontext script`, `IDesignTimeDbContextFactory`, model precedence |
| S10 | `Azure.Bicep.Core` on NuGet | primary (registry) | https://www.nuget.org/packages/Azure.Bicep.Core | **The unsupported-package disclaimer, quoted verbatim**; `net10.0` target |
| S11 | Bicep CLI + JSON-RPC docs | primary (vendor docs) | https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/bicep-cli · …/bicep-cli-jsonrpc | `bicep build`, jsonrpc stability from v0.29.45, wire framing, decompile caveat, implicit `dependsOn` |
| S12 | `microsoft/SqlScriptDOM` | primary (repo) | https://github.com/microsoft/SqlScriptDOM | MIT licence, offline T-SQL AST, grammar-driven fix workflow |
| S13 | `Microsoft.SqlServer.DacFx` on NuGet | primary (registry) | https://www.nuget.org/packages/Microsoft.SqlServer.DacFx | `.dacpac` model, `DacServices.Extract` live-DB requirement |
| S14 | `tobymao/sqlglot` | primary (repo) | https://github.com/tobymao/sqlglot | MIT, no deps, 30+ dialects, deliberate leniency |
| S15 | `k1LoW/tbls` | primary (repo) | https://github.com/k1LoW/tbls | **Live DSN requirement** |
| S16 | LSIF 0.6.0 specification | standard | https://microsoft.github.io/language-server-protocol/specifications/lsif/0.6.0/specification/ | **The "not a symbol database" statement, quoted** |
| S17 | `javaparser/javaparser` | primary (repo) | https://github.com/javaparser/javaparser | Version 3.28.2, LGPL-3/Apache-2.0, symbol solver, Java 1–25 |
| S18 | `scip-code/scip` | primary (repo) | https://github.com/scip-code/scip | SCIP format, indexer list — **and the absence of an official C# indexer** |
| S19 | `tree-sitter/tree-sitter` | primary (repo) | https://github.com/tree-sitter/tree-sitter | Error-resilient CST, grammar coverage, no name resolution |

## Source-quality notes

- **Package IDs, versions, licences and API names were read from NuGet, npm, Maven coordinates or the
  project repository** — none is recalled. The two verbatim disclaimers (`Azure.Bicep.Core`, LSIF) are
  quoted because paraphrasing them would lose exactly the force that makes them decisive.
- The **measured MSBuildWorkspace timings** come from GitHub issue discussions ([S3][S4]), which are primary
  but anecdotal — they are real reports from real solutions, not a controlled benchmark, and should be
  treated as order-of-magnitude.
- **SchemaSpy, SchemaCrawler, Spoon, pgsql-parser, SQLFluff and the Terraform HCL tooling** were not fetched
  directly; claims about them are **Inferred**.
- The claim that **no official `scip-dotnet` indexer exists** is an argument from the absence of a listing on
  the official indexer page — weaker than a positive citation, and marked **Flagged** wherever used.
