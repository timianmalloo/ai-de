---
id: kb-codegraph-comparables
title: "Code Knowledge Graphs — comparable systems"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [comparables, glean, scip, kythe, codeql, graph-stores, liveness]
links:
  - { to: kb-code-knowledge-graphs, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Code-graph systems and graph stores compared, with project liveness treated as a first-class
  column — four of the systems surveyed died in the last five years, which is itself the
  domain's most important pattern.
---

# Comparable solutions & problem framings

> **Read the Status column first.** This domain has an unusually high mortality rate: Kuzu (2025),
> Stack Graphs (2024/25), Sourcetrail (2021) are dead, and Kythe is de-staffed. Liveness is a selection
> criterion here, not a footnote.

## Code-graph systems

| System | How it frames the problem | Approach | Does well | Does badly | Status | Confidence |
|---|---|---|---|---|---|---|
| **Glean** (Meta) | "Facts about code in a typed queryable database" | Language indexers → typed fact schema → **Angle** (Datalog) queries; ingests SCIP/LSIF | Precise semantic queries; native transitive closure; cross-language; incremental derived predicates | Server-only, not embeddable; Haskell build; no .NET | ✅ **Active** (Meta blog, Dec 2024) | Verified [S7][S8][S9] |
| **SCIP** (Sourcegraph) | "Precise, portable code-navigation index" | Protobuf index with human-readable symbol IDs, uploaded to a backend | Compact and fast; well-specified grammar; `scip-dotnet` exists; easy incremental indexing | Not a queryable graph DB; needs a backend to serve; limited relationship types | ✅ Active (`scip-code` org) | Verified [S3][S4][S5] |
| **LSIF** | Predecessor of SCIP | JSON graph with opaque numeric IDs | — | ~5–8× larger, ~3× slower; **read support removed in Sourcegraph 4.6**; migration irreversible | ⛔ **Deprecated** | Verified [S6] |
| **Kythe** (Google) | "Universal graph of code facts with stable VNames" | `.kzip` compilation units → serving graph; VName 5-tuple identity | Deeply cross-language and cross-repo; the best-specified identity scheme | Complex build; no turnkey embedded option | ⚠️ **Low-activity** — US team laid off Apr 2024 | Verified [S23][S24] |
| **CodeQL** (GitHub) | "Query language over code's semantic graphs" | Build-time extraction → TRAP/DB → QL (OO Datalog) | Full data-flow and control-flow; industry standard for security; real incrementality | Opaque IDs, **not** for stable symbol identity; proprietary for private repos; no embedded .NET DB | ✅ Active | Verified [S25][S26] |
| **Stack Graphs** (GitHub) | "Build-free incremental name resolution" | Scope-graph theory (TU Delft); Rust library | Elegant, language-agnostic rules, genuinely incremental | — | ⛔ **Abandoned** — explicit "no longer supported" notice | Verified [S10] |
| **Sourcetrail** | "Interactive code-graph visualiser" | Call/include/type-use extraction with a custom UI | Visual, multi-language | — | ⛔ **Archived 2021** — insufficient uptake; Qt/LLVM/Java stack too complex for community takeover | Verified [S27] |
| **tree-sitter** | "Incremental, error-resilient parsing" | Concrete syntax trees; bindings everywhere incl. C# | The parse layer under much of the above; tolerant of broken code | Not a semantic graph | ✅ Active | Verified |
| **Semgrep** | "Pattern matching over ASTs" | ~30 languages, rule-based | Security rules | Not a graph store; no navigation | ✅ Active | Verified |
| **Moose / FAMIX** | "Language-independent code meta-model" | FAMIX schema in Pharo Smalltalk | Research-grade analysis and visualisation | Not mainstream tooling | ✅ Active (research) | Verified |
| **srcML** | "Source as marked-up XML" | `<function>`, `<call>` markup for C/C++/Java/C#/JS | Lightweight static analysis | Syntactic only | ✅ Active | Verified |
| **Graphify**, **CodeSee** | commercial code maps | — | — | — | **Flagged** — not confirmed from primary sources | Flagged |

## Graph stores, scored against our four criteria

Criteria: **E** embedded in-process · **A** actively maintained · **P** permissive licence · **N** first-class .NET.

| Store | E | A | P | N | Query language | Verdict |
|---|---|---|---|---|---|---|
| Kuzu | ✅ | ⛔ | ✅ MIT | ~ (community, orphaned) | Cypher | **Dead** — was the closest to all four |
| DuckDB + DuckPGQ | ✅ | ✅ | ✅ MIT | ✅ DuckDB.NET | SQL/PGQ | **Strongest survivor**; extension is research-grade, no Cypher |
| SQLite + recursive CTEs | ✅ | ✅ | ✅ public domain | ✅ Microsoft.Data.Sqlite | SQL | **Boring and viable**; you implement graph semantics yourself |
| Neo4j Community | ⛔ (Java only) | ✅ | ⛔ GPLv3 | ~ Bolt driver | Cypher | Server out-of-process; copyleft |
| Memgraph | ⛔ | ✅ | ⛔ BSL 1.1 | ~ Bolt driver | openCypher | OEM licence needed to embed |
| Apache AGE | ⛔ (needs PostgreSQL) | ✅ | ✅ Apache-2.0 | ~ Npgsql | openCypher subset | A server, and partial conformance |
| CozoDB | ✅ | ✅ | ✅ MPL-2.0 | ⛔ **none** | Datalog | Excellent recursion, no C# |
| Oxigraph | ✅ | ✅ | ✅ MIT/Apache | ⛔ **none** | SPARQL | RDF model mismatch |
| TypeDB | ⛔ | ✅ | ⛔ BSL 1.1 | ✅ official client | TypeQL | BSL; unusual model |

*(Verified per store, [S11]–[S22])* **No row scores all four.** *(Inferred)*

## Retrieval approaches for agents

| Approach | Wins at | Loses at | Cost | Confidence |
|---|---|---|---|---|
| **Vector RAG** | "find function X"; local/factoid lookups | multi-hop relational questions | baseline | Verified [S29][S30] |
| **GraphRAG** (Microsoft) | multi-entity, multi-hop, community summarisation | general and local QA — matched or beaten by vector RAG | **26–85×** tokens for global-context queries | Verified [S28][S29][S30] |
| **Hybrid** (vector first, graph for context) | production reality | — | between | Verified [S30] |

## Adjacent ideas worth borrowing

- **SCIP's symbol grammar** — a solved answer to stable, human-readable, deterministic identity. The seed
  architecture calls this "the make-or-break detail" and then invents a scheme; SCIP's already exists, is
  diff-readable, and makes ingesting `scip-dotnet` a later option rather than a rewrite.
- **Kythe's VName 5-tuple** — the cross-repo, cross-language generalisation, and the explicit requirement
  that `signature` be stable for identical input.
- **Glean's fact/derived-predicate split** — what turns "all transitive callers" from a traversal loop into
  a query, and what makes incrementality tractable. Copyable onto a relational store.
- **CodeQL's overlay database** — cache the default branch, merge a delta. The right shape for
  save-to-refresh incrementality, with the honest caveat that its measured win in C# is at the low end.
- **Sourcetrail's post-mortem** — the archived issue names the cause as unsustainable maintenance of a
  complex dependency stack, which is a direct argument for keeping extractors as independent CLIs with a
  JSON contract rather than a monolith.
