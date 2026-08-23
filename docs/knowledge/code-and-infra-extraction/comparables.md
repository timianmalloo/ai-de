---
id: kb-extraction-comparables
title: "Code & Infrastructure Extraction — comparable tools"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [comparables, extraction, offline, roslyn, sql, bicep]
links:
  - { to: kb-code-and-infra-extraction, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Extractors compared by the question that decides whether they can be used at all — does this
  work offline from repository artifacts, or does it need a live database, a build, or a
  network call?
---

# Comparable solutions & problem framings

**The column that decides everything is "offline?"** — an artifact-only supply chain cannot use a tool that
needs a live database or a completed build.

## C#

| Tool | Extracts | Offline? | Does well | Does badly | Confidence |
|---|---|---|---|---|---|
| **Roslyn `MSBuildWorkspace`** | Full semantic model — types, members, references, attributes, XML docs | ✅ from `.sln`/`.csproj` + SDK (no network/DB) | Fully resolved symbols; attributes; generic instantiation; incremental recompile | **Cold load 1.5–4 min**; hangs on unresolvable refs; **DI/routes/EF invisible**; no source generators at design time | Verified [S1][S3][S4][S5] |
| **Roslyn `AdhocWorkspace`** | Syntax + semantics for in-memory code | ✅ | Prototyping, unit tests, snippets | No SDK, no NuGet, no multi-file resolution | Verified [S1] |
| **tree-sitter** | Error-resilient CST | ✅ | Works on broken/incomplete code; very fast | **No name resolution or types** — cannot tell `AddScoped<IFoo,Foo>` from `AddScoped<Other>` | Verified [S19] |
| **`dotnet ef dbcontext script`** | Authoritative EF model as DDL | ⚠️ offline from network, but **executes code** | The real model, conventions and fluent API included | Not static; needs `Microsoft.EntityFrameworkCore.Design` and a compilable project | Verified [S9] |

## SQL

| Tool | Extracts | Offline? | Does well | Does badly | Confidence |
|---|---|---|---|---|---|
| **ScriptDOM** | T-SQL AST — DDL, DML, procs, triggers, views | ✅ from `.sql` | Complete typed parse tree; visitor API; SQL 2022 syntax; MIT, open source | T-SQL only; **no cross-object resolution**; grammar lags brand-new syntax | Verified [S12] |
| **DacFx / `.dacpac`** | Resolved object model — tables, columns, PK/FK, views, procs, cross-object edges | ⚠️ consuming a `.dacpac` is offline; **`Extract` needs a live DB** | Richest SQL metadata available | Complex `TSqlModel` API; limited non-T-SQL support | Verified [S13] |
| **sqlglot** | SQL AST across 30+ dialects incl. DDL | ✅ pure Python, no deps | Multi-dialect; lineage helpers; trivial to install | **Deliberately lenient** — parses invalid SQL without error; no cross-file resolution | Verified [S14] |
| **pgsql-parser / libpg_query** | PostgreSQL AST via PG's own parser | ✅ | Exact PG semantics | PostgreSQL only | Inferred |
| **tbls** | Tables, FKs, indexes, comments → Markdown | ❌ **needs a live DSN** | Great docs and diffs; many drivers | Cannot read `.sql` files | Verified [S15] |
| **SchemaSpy / SchemaCrawler** | Full schema + ER diagrams | ❌ **JDBC required** | Rich reports; mature | Not offline | Inferred |
| **SQLFluff** | Parse tree (as a linter) | ✅ | Linting workflows | Not designed as a schema-graph source | Inferred |

## Infrastructure as code

| Tool | Extracts | Offline? | Does well | Does badly | Confidence |
|---|---|---|---|---|---|
| **`bicep build`** | ARM JSON with **explicit `dependsOn`** (implicit edges materialised) | ✅ | The correct extraction input; stable; `--stdout` pipes | Needs the CLI installed; external modules need `bicep restore` | Verified [S11] |
| **`bicep jsonrpc`** | Compile, diagnostics, file references | ✅ | **The supported programmatic API**; avoids per-file cold start; documented back-compat | Subprocess + JSON-RPC client to implement; process lifetime undocumented | Verified [S11] |
| **`Azure.Bicep.Core`** | Same, in-process | ✅ | No subprocess | **Explicitly unsupported — "breaking changes at any time"** | Verified [S10] |
| **`bicep decompile`** | Bicep from ARM JSON | ✅ | Migration aid | **Lossy, best-effort**; warns on nested templates, `copy` loops, conditionals — never a source of truth | Verified [S11] |
| **`terraform graph`** | DOT dependency graph | ❌ needs `terraform init` | Complete graph | Provider download; verbose output | Inferred |
| **`hashicorp/hcl/v2`** | HCL AST | ✅ | Resource types, labels, references | **No provider attribute schemas** | Inferred |

## Other languages

| Tool | Extracts | Offline? | Does well | Does badly | Confidence |
|---|---|---|---|---|---|
| **ts-morph** | TS/JS AST + type resolution, imports/exports | ✅ | Friendly API over the TS compiler; real type inference | Dynamic JS patterns invisible | Verified [S8] |
| **JavaParser** (symbol-solver 3.28.2) | Java AST + symbols, Java 1–25 | ✅ (classpath improves resolution) | Mature; full symbol solver | **Resolution degrades without a complete classpath**; LGPL-3/Apache-2.0 dual | Verified [S17] |
| **Spoon** | Higher-level Java transformation on Eclipse JDT | ✅ | Program transformation | Heavier | Inferred |
| **Python `ast` / griffe / pyreverse** | AST; API surfaces; UML-style diagrams | ✅ | No imports needed for `ast`; griffe covers annotations | Dynamic Python invisible | Verified |
| **`cargo metadata` / cargo-modules** | Crate/dependency/feature graph; module tree | ✅ | Accurate dependency level | Class-level detail weak in Rust generally | Verified |
| **SCIP indexers** | Definitions, occurrences, relationships | TS ✅; **Java needs compilation**; Python needs deps | Standard cross-language format; efficient Protobuf | **No official C# indexer** | Verified/Flagged [S18] |

## Output formats considered as our delta contract

| Format | What it is for | Verdict |
|---|---|---|
| **SCIP** | Symbol definitions, occurrences, relationships | Closest fit; a snapshot index, not a delta |
| **LSIF 0.6.0** | Caching LSP query results | **Rejected by its own spec** — "doesn't define a symbol database" |
| **Kythe entries** | Cross-language facts with VName identity | Conceptually right, heavyweight |
| **SARIF** | Static-analysis findings | Different problem |
| **CycloneDX / SPDX** | SBOM — dependency and licence inventory | Different problem; useful for `Package` nodes |
| **OpenAPI** | HTTP contracts | Useful as an *input* for `Endpoint` facts |

*(Verified, [S16][S18])*
