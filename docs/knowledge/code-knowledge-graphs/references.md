---
id: kb-codegraph-references
title: "Code Knowledge Graphs — references, versions, licences and identifier schemes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [reference, scip, kythe, gql, licences, versions]
links:
  - { to: kb-code-knowledge-graphs, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Standards and papers plus the exact constants — SCIP symbol grammar, Kythe VName fields,
  store versions and licences, CodeQL incremental thresholds, and the GraphRAG cost multiplier.
---

# Reference information

## Standards and specifications

- **ISO/IEC 39075:2024 — GQL.** First edition, published **April 2024**; the first new ISO database-language
  standard since SQL. Covers data manipulation and schema for property graphs; a superset of the openCypher
  portable core. **No production-complete implementation confirmed as of August 2026.** *(Verified, [S1][S2])*
- **openCypher** — the open community specification and its TCK compliance suite. *(Verified, [S31])*
- **SQL/PGQ**, SQL:2023 Part 16 — property-graph matching inside SQL; implemented by DuckPGQ. *(Verified, [S14][S15])*
- **SCIP** — Protobuf schema (`scip.proto`) and symbol grammar in the `scip-code/scip` repository. *(Verified, [S3][S4][S5])*
- **Kythe schema** — the VName conventions and universal node/edge schema. *(Verified, [S23])*

## Foundational and evaluative papers

- **Incrementalizing Production CodeQL Analyses** — arXiv:2308.09660. The theory behind overlay databases.
  *(Verified, [S26])*
- **DuckPGQ: Bringing SQL/PGQ to DuckDB** — CWI, 2023. *(Verified, [S14])*
- **RAG vs GraphRAG: A Systematic Evaluation** — arXiv:2502.11371. The evidence that vector RAG matches or
  beats GraphRAG on general and local QA. *(Verified, [S30])*
- **UnWeaving the Knots of GraphRAG** — Samsung, 2026 preprint. Same conclusion, independently.
  *(Verified via [S30]'s context; the preprint itself Flagged)*
- **Scope graphs** — TU Delft / Eelco Visser; the name-resolution theory Stack Graphs was built on.
  *(Reference; not fetched)*

## Identifier schemes (copy these rather than inventing)

### SCIP symbol grammar

```
Symbol     = scheme SP package SP { descriptor }
descriptor = name [ disambiguator ] suffix
suffix     = '/'   Namespace
           | '#'   Type (class, struct, interface)
           | '.'   Term (field, enum value, variable)
           | '('   Method / function
           | ':'   Method parameter or type parameter

example:  scip-go github.com/gorilla/mux/ Router#HandleFunc(
```

Human-readable, deterministic from the artifact, stable across re-indexing, and legible in a diff.
*(Verified, [S4][S5])*

### Kythe VName (5-tuple)

```
corpus    logical collection, e.g. "github.com/org/repo"
root      optional path prefix
path      file path relative to corpus + root
language  "csharp", "go", "java", …
signature indexer-defined; MUST be stable for the same input
```

*(Verified, [S23])*

## Versions, licences and liveness

| Store / system | Version | Licence | Status as of 2026-08-23 |
|---|---|---|---|
| **Kuzu** | **v0.11.3 (final)** | MIT | ⛔ **Archived ~2025-10-10** ("working on something new") |
| KuzuDot / ladybug.net (.NET bindings) | — | community | ⛔ orphaned with Kuzu |
| DuckDB | 1.x (DuckPGQ best on 1.4.4 per community) | MIT | ✅ active |
| Neo4j Community | 5.x | **GPLv3** (server/embedded); driver Apache-2.0 | ✅ active |
| Memgraph | 2.x | **BSL 1.1** → Apache-2.0 in 2030 | ✅ active |
| Apache AGE | 1.5.x (PostgreSQL 11–17) | Apache-2.0 | ✅ active (ASF TLP since May 2022) |
| CozoDB | 0.7.x | MPL-2.0 | ✅ active — **maintenance state Flagged** |
| Oxigraph | 0.4.x | MIT/Apache-2.0 | ✅ active |
| TypeDB | 3.x | **BSL 1.1** → AGPL | ✅ active |
| SQLite | — | public domain | ✅ active |
| Glean | — | BSD 3-Clause | ✅ active (Meta blog, Dec 2024) |
| SCIP | — | Apache-2.0 (`scip-code/scip`) | ✅ active |
| LSIF | — | — | ⛔ read support removed in Sourcegraph **4.6** (2024); migration irreversible |
| Kythe | — | Apache-2.0 | ⚠️ low-activity; US team laid off **April 2024** |
| Stack Graphs | — | Apache-2.0 / MIT | ⛔ abandoned — explicit GitHub notice |
| Sourcetrail | — | — | ⛔ archived 2021 |
| CodeQL | CLI ≥ 2.21.0 / ≥ 2.23.8 | free for OSS; commercial for private repos | ✅ active |

*(Verified per row, [S3]–[S27])*

**Licence terms that decide embedding:** **GPLv3** (Neo4j server/embedded) is copyleft. **BSL 1.1**
(Memgraph, TypeDB) is *source-available, not open source* — redistribution and embedding in a shipped
product require a commercial agreement until the change date. **MIT / Apache-2.0 / MPL-2.0 / public domain**
(DuckDB, SCIP, AGE, CozoDB, Oxigraph, SQLite) are unproblematic. *(Verified, [S16][S21][S22])*

## Measured numbers

| Metric | Value | Source |
|---|---|---|
| SCIP vs LSIF — size | ~5–8× smaller uncompressed | [S3] |
| SCIP vs LSIF — processing | ~3× faster at Glean/Meta | [S3] |
| SCIP vs LSIF — mapping code | ~550 LOC vs ~1500 LOC | [S3] |
| CodeQL incremental speedup | **5%** (C#/C++) → **40%** (Go) | [S25] |
| CodeQL incremental prerequisites | CLI ≥ 2.21.0 (diff-informed), ≥ 2.23.8 (overlay), Git ≥ 2.38.0, all files tracked | [S25] |
| GraphRAG global-query token cost | **26–85×** local vector retrieval | [S28][S30] |
| ArcadeDB openCypher TCK pass rate | 97.8% (vendor-reported) | [S31] |

**Not measured anywhere, and we need it:** node/edge counts for a real C# solution's code graph, and
transitive-closure query latency at a million nodes. Kythe's docs imply millions of nodes for Google
monorepo compilations and CodeQL databases for ~100k LOC C# run from hundreds of MB to several GB — both
**Inferred**, neither an answer. *(Flagged)*
