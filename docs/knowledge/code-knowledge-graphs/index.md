---
id: kb-code-knowledge-graphs
title: "Code Knowledge Graphs & Graph Stores — domain knowledge"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [knowledge-graph, graph-database, scip, glean, kythe, codeql, gql, kuzu]
links:
  - { to: knowledge-hub, rel: refines }
  - { to: seed-ai-native-ide-sketch, rel: relates-to }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Evidence base for storing and querying a code knowledge graph. Headline: Kuzu — the store the
  seed architecture selected — was archived in October 2025, and no embedded, actively
  maintained, permissively licensed Cypher store with a first-class .NET API exists to replace it.
---

# Code Knowledge Graphs & Graph Stores — domain knowledge

**Domain & problem:** AI-DE builds a knowledge graph of a codebase (code + infrastructure + database +
runtime traces + human decisions), stores it in an embedded graph database, and serves it to coding agents
over MCP and to humans as derived diagrams. Every node carries deterministic IDs and provenance; extractors
emit full snapshots per scope and the daemon diffs them.

**Canonical framing:** The field frames this as **code intelligence indexing** and has converged on two
distinct shapes — a *portable index format* (SCIP, LSIF, Kythe entries) that captures definitions,
references and relationships for navigation, and a *queryable fact database* (Glean, CodeQL) that supports
arbitrary derived queries including transitive closure. Our framing is the second, extended past code into
infrastructure, data and runtime — which no existing system does, and which is both the opportunity and the
reason nothing off-the-shelf fits.

**Compiled:** 2026-08-23 · **Lead:** Domain Researcher · **Status:** fresh

*(`data-and-constants.md` is folded into `references.md` §"Versions, licences and identifier schemes" —
this domain's constants are symbol grammars, licences and project-liveness facts.)*

## Headline findings

1. **Kuzu is archived and abandoned.** Kùzu Inc. archived the repository around 2025-10-10 with the message
   "working on something new"; the final release was **v0.11.3**. Community forks exist (Kineviz's
   "bighorn") and are unsupported. The `KuzuDot` community .NET binding is orphaned with it. **The seed
   architecture's primary storage recommendation no longer exists as a maintained option.** — *(Verified, [S11][S12][S13])*
2. **There is no embedded, actively maintained, permissively licensed, Cypher-speaking graph store with a
   first-class .NET API.** Every candidate fails at least one criterion: Kuzu is archived; Neo4j's embedded
   mode is Java-only and GPLv3; Memgraph and TypeDB are BSL 1.1 (source-available, redistribution
   restricted); Apache AGE needs a PostgreSQL server; CozoDB and Oxigraph have **no .NET bindings**;
   DuckDB+DuckPGQ has an excellent .NET story but speaks SQL/PGQ, not Cypher, and DuckPGQ is a research-grade
   extension. — *(Verified per store, [S11][S16][S17][S18][S19][S20][S21][S22]; the conclusion Inferred)*
3. **SCIP has decisively replaced LSIF.** Sourcegraph removed LSIF data-reading in v4.6 (2024) and the
   migration is destructive and irreversible. SCIP is Protobuf-based with **human-readable string symbol
   IDs**, reported at Meta as ~5–8× smaller and ~3× faster to process, needing ~550 lines of mapping code
   against ~1500 for LSIF. A C# indexer (`scip-dotnet`) exists. — *(Verified, [S3][S4][S5][S6])*
4. **The prior art is thinning out.** GitHub's **Stack Graphs** carries an explicit "no longer supported or
   updated by GitHub" notice; **Sourcetrail** was archived in 2021 for lack of a sustainable maintainer;
   **Kythe**'s US development team was laid off in April 2024 and it is in low-activity maintenance. The
   survivors are **Glean** (Meta, actively invested as of Dec 2024), **SCIP** and **CodeQL**. — *(Verified, [S8][S9][S10][S24][S27])*
5. **ISO/IEC 39075:2024 (GQL) exists but nothing fully implements it.** Published April 2024 — the first new
   ISO query-language standard since SQL. No production database fully implements it as of August 2026;
   Neo4j has committed to convergence and characterises existing Cypher as "95% there". Sticking to the
   **openCypher portable core** is therefore the practical hedge. — *(Verified, [S1][S2][S31])*
6. **Stable symbol identity is a solved problem worth copying rather than inventing.** SCIP's grammar is
   `<scheme> <package> <descriptor>*` with suffix-typed descriptors (`/` namespace, `#` type, `.` term,
   `(` method, `:` parameter). Kythe's **VName** is the 5-tuple `{corpus, root, path, language, signature}`.
   Both are deterministic from the artifact, both survive re-indexing. — *(Verified, [S4][S5][S23])*
7. **CodeQL demonstrates that incremental analysis is real but modest.** Overlay databases (cache the
   default branch, merge a delta) plus diff-informed analysis give **5% to 40%** speedup depending on
   language — C# near the low end, Go near the high end. Requires CLI ≥ 2.21.0 (diff-informed) and ≥ 2.23.8
   (overlay), plus Git ≥ 2.38.0 with all files tracked. — *(Verified, [S25][S26])*
8. **GraphRAG helps for multi-hop, hurts for lookups, and costs a great deal.** Published evaluations find
   vanilla vector RAG matches or beats GraphRAG on general and local QA; GraphRAG wins on multi-entity
   traversal. Microsoft's own evaluation puts global-context queries at **26–85× the token cost** of local
   vector retrieval. Hybrid — vector first, graph for relational context — dominates production. — *(Verified, [S28][S29][S30])*
9. **No published node/edge counts or transitive-closure latencies exist for C# code graphs.** This is a
   genuine hole: nobody has published what a medium C# solution's graph actually weighs, or how long an
   "impact of" query takes at a million nodes. Any performance claim in our design will be our own
   measurement or a guess. — *(Flagged — absence, exhaustively searched)*
10. **Glean is the closest architectural relative and is alive.** Schema-defined typed *facts* produced by
    language indexers, queried in **Angle** (a strongly-typed Datalog dialect with native recursive rules),
    with SCIP/LSIF ingestion and incremental derived predicates. It is server-only, Haskell-built, BSD
    3-Clause, and has no .NET story — so it is a design reference, not a dependency. — *(Verified, [S7][S8][S9][S32])*

## Confidence summary

Verified: every liveness fact (Kuzu archived, Stack Graphs abandoned, Sourcetrail archived, Kythe
de-staffed, Glean active), every licence, the SCIP and Kythe identifier grammars, GQL's publication and
non-implementation, CodeQL's incremental thresholds and speedup range, and the GraphRAG evaluations.
Inferred: that no store satisfies all four of our criteria simultaneously (a conclusion across verified
per-store facts); GraphRAG's implication for code context. Flagged: CozoDB's current maintenance state;
`scip-dotnet`'s completeness on C# edge cases (partial classes, source generators, global usings, nullable
annotations); Graphify and CodeSee's current status; **and the absence of any published graph-size or
query-latency numbers for C#**.

**Load-bearing Flagged claims:** the missing performance numbers. A design that assumes sub-second
transitive queries at repository scale is assuming, not knowing — and the archived system that claimed it
(Kuzu) can no longer be benchmarked as a live option.

## Design implications

- **Reopen the storage decision.** The seed architecture chose Kuzu behind an `IGraphStore` interface. The
  interface was the right call and it is what makes this survivable; the choice behind it must be remade.
  The realistic shortlist is now: **SQLite with an adjacency table and recursive CTEs** (boring, embedded,
  first-class .NET, public domain, no Cypher), **DuckDB + DuckPGQ** (embedded, MIT, excellent .NET, standard
  SQL/PGQ, research-grade extension), or **an out-of-process Neo4j/Memgraph** (mature Cypher, but a server
  to run and a licence to read carefully).
- **Do not let the query language drive the choice.** Cypher is pleasant and it is not the requirement; the
  requirement is transitive closure, stable IDs, provenance and a .NET story. Recursive CTEs express
  transitive closure adequately, and SQL/PGQ expresses it in a standard.
- **Copy SCIP's symbol grammar rather than inventing one.** `<scheme> <package> <descriptor>*` with typed
  suffixes already solves what the seed sketch calls "the make-or-break detail", it is human-readable in a
  diff, and adopting it makes ingesting `scip-dotnet` output later a possibility rather than a rewrite.
- **Consider consuming SCIP instead of writing extractors, for languages where an indexer exists.** The
  seed plan writes a Roslyn extractor; `scip-dotnet` already exists. Its completeness on C# edge cases is
  unverified — that is a spike, not a decision.
- **Model facts, not nodes, if the graph is to answer derived questions.** Glean's design — typed facts plus
  derived predicates — is what makes "all transitive callers" a query rather than a traversal loop. Even on
  a relational store, the fact/derivation split is the shape to copy.
- **Budget the agent-facing context aggressively.** Given 26–85× token costs for global graph context, the
  MCP tools must return *bounded neighbourhoods and summaries*, never subgraphs. This is a tool-design
  constraint, not an optimisation.
- **Measure the graph early, because nobody else has.** Node/edge counts and impact-query latency for a real
  C# solution are unpublished. Phase 0 should emit those numbers as a deliverable.
- **Treat project liveness as a first-class selection criterion.** Four of the systems surveyed died in the
  last five years. Whatever is chosen, the `IGraphStore` seam and a documented export format are what make
  the next death survivable.

## How to use this base

Personas and the design skills cite these files as evidence (BoK §III.1). The liveness facts in
`comparables.md` have short half-lives — re-check the store's repository before any decision that depends on
it being maintained. Refresh when a GQL implementation ships or when a replacement for Kuzu emerges.
