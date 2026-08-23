---
id: kb-codegraph-glossary
title: "Code Knowledge Graphs — glossary"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [glossary, scip, kythe, gql, ubiquitous-language]
links:
  - { to: kb-code-knowledge-graphs, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Precise definitions for code-graph vocabulary — SCIP, VName, Angle, overlay database, scope
  graphs, BSL — so design documents name one concept one way.
---

# Glossary — ubiquitous language

| Term | Definition |
|---|---|
| **Angle** | Glean's query language: strongly typed, Datalog-inspired, with native recursive rules — so transitive closure is a rule, not a loop. *(Verified, [S32])* |
| **BSL** | Business Source License. **Source-available, not OSI open source**: restricts commercial embedding and redistribution until a change date. Used by Memgraph (→ Apache-2.0 in 2030) and TypeDB (→ AGPL). *(Verified, [S21][S22])* |
| **CSR** | Compressed Sparse Row — the adjacency storage layout Kuzu used for fast traversal. *(Verified, [S11])* |
| **Derived predicate** | In Glean, a fact computed from other facts rather than emitted by an indexer; re-evaluated when its inputs change. The mechanism behind incrementality. *(Verified, [S32])* |
| **FAMIX** | A language-independent meta-model for code artifacts, used by the Moose analysis platform. |
| **Glean** | Meta's open-source code fact database — typed facts, Angle queries, SCIP/LSIF ingestion. Server-only, BSD 3-Clause, actively maintained. *(Verified, [S7][S8])* |
| **GQL** | ISO/IEC 39075:2024 — the ISO property-graph query language, published April 2024, **not yet fully implemented by any production database**. *(Verified, [S1])* |
| **GraphRAG** | Retrieval augmented by an LLM-extracted knowledge graph with community detection and summarisation. Wins on multi-hop; loses on local lookups; **26–85× token cost** for global queries. *(Verified, [S28][S30])* |
| **KZip** | Kythe's compilation-unit archive: sources, compilation options and indexer metadata. *(Verified, [S23])* |
| **LSIF** | Language Server Index Format — JSON, opaque numeric IDs. **Deprecated**; read support removed in Sourcegraph 4.6. *(Verified, [S6])* |
| **openCypher** | The open community specification of Cypher, with a TCK. Its **portable core** — `MATCH`, `MERGE`, `WITH`, variable-length paths — is what survives across engines; procedures, APOC and admin commands do not. *(Verified, [S31])* |
| **Overlay database** | CodeQL's incrementality mechanism: a cached database of the default branch merged with a delta database of changed code. *(Verified, [S25])* |
| **Provenance** | The record of *what produced a fact* — repo, file, line, commit, extractor and version, timestamp. What separates a graph you can trust from a graph you cannot audit. |
| **Scope graphs** | The formal name-resolution theory (TU Delft / Eelco Visser) that GitHub's Stack Graphs implemented. |
| **SCIP** | SCIP Code Intelligence Protocol — Protobuf code index with **human-readable string symbol IDs**; LSIF's successor. *(Verified, [S3][S4])* |
| **Scope** (extraction) | The unit of replacement in our own model: an extractor emits a full snapshot per scope, and the daemon deletes then re-inserts within it, so re-runs are idempotent and diffs are free. *(From the seed architecture, not external)* |
| **SQL/PGQ** | Property-graph matching standardised inside SQL:2023 Part 16, via `GRAPH_TABLE(… MATCH …)`. Implemented by DuckPGQ. *(Verified, [S14][S15])* |
| **Stack Graphs** | GitHub's build-free incremental name resolution based on scope graphs. **Abandoned** — the repository carries an explicit unsupported notice. *(Verified, [S10])* |
| **Symbol identity** | A deterministic, artifact-derived identifier for a code entity that survives re-indexing. SCIP's grammar and Kythe's VName are the two established schemes; renames are correctly modelled as delete-plus-add. *(Verified, [S4][S23])* |
| **tree-sitter** | Incremental, error-resilient concrete-syntax-tree parsing with bindings in most languages including C#. A parse layer, not a semantic graph. |
| **VName** | Kythe's "Vector Name" — the 5-tuple `{corpus, root, path, language, signature}` identifying a code entity; `signature` must be stable for identical input. *(Verified, [S23])* |
