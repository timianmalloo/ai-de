---
id: kb-codegraph-sources
title: "Code Knowledge Graphs — sources"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [sources, citations]
links:
  - { to: kb-code-knowledge-graphs, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The full access-dated source list behind the code-knowledge-graph base, keyed [S1]..[S32],
  including the liveness citations that carry the domain's most consequential findings.
---

# Sources

All accessed **2026-08-23**. Citation keys `[Sn]` are used throughout this topic.

| # | Title | Type | URL | Used for |
|---|---|---|---|---|
| S1 | ISO/IEC 39075:2024 — GQL | standard (ISO catalogue) | https://www.iso.org/standard/76120.html | GQL publication date and status |
| S2 | GQL Standards site | standards body | https://www.gqlstandards.org/home | GQL scope and implementation status |
| S3 | Sourcegraph — Announcing SCIP | primary (vendor blog) | https://sourcegraph.com/blog/announcing-scip | SCIP vs LSIF size/speed/mapping-LOC figures |
| S4 | `scip-code/scip` repository | primary (repo) | https://github.com/scip-code/scip | SCIP spec, indexer list, bindings |
| S5 | SCIP symbol documentation | primary (spec) | https://github.com/scip-code/scip/blob/main/docs/scip.md | Symbol grammar (quoted) |
| S6 | LSIF → SCIP migration guide | primary (docs) | https://sourcegraph.com/docs/admin/how-to/lsif-scip-migration | LSIF deprecation in 4.6; irreversible migration |
| S7 | Glean homepage | primary | https://glean.software/ | Glean overview |
| S8 | Meta Engineering — indexing code at scale with Glean (Dec 2024) | primary (vendor blog) | https://engineering.fb.com/2024/12/19/developer-tools/glean-open-source-code-indexing/ | **Liveness**: continued open-source investment |
| S9 | `facebookincubator/Glean` | primary (repo) | https://github.com/facebookincubator/Glean | Licence, indexers |
| S10 | `github/stack-graphs` | primary (repo) | https://github.com/github/stack-graphs | **Abandonment notice, quoted** |
| S11 | `kuzudb/kuzu` (archived) | primary (repo) | https://github.com/kuzudb/kuzu | **Archival**, v0.11.3 final, MIT, storage model |
| S12 | The Register — KuzuDB abandoned (Oct 2025) | secondary (news) | https://www.theregister.com/software/2025/10/14/kuzudb-graph-database-abandoned-community-mulls-options/1142229 | Archival date, community reaction, forks |
| S13 | `KuzuDot/KuzuDot` | primary (community repo) | https://github.com/KuzuDot/KuzuDot | .NET bindings, now orphaned |
| S14 | DuckPGQ — CWI paper (2023) | academic | https://ir.cwi.nl/pub/33317/33317.pdf | SQL/PGQ implementation in DuckDB |
| S15 | DuckDB graph-queries docs | primary (docs) | https://duckdb.org/docs/current/guides/sql_features/graph_queries | DuckPGQ install and syntax |
| S16 | Neo4j Java embedded docs | primary (docs) | https://neo4j.com/docs/java-reference/current/java-embedded/setup/ | Embedded mode is Java-only; GPLv3 |
| S17 | `neo4j/neo4j-dotnet-driver` | primary (repo) | https://github.com/neo4j/neo4j-dotnet-driver | Bolt driver, Apache-2.0 |
| S18 | Apache AGE overview | primary | https://age.apache.org/overview/ | PostgreSQL extension, openCypher support |
| S19 | `cozodb/cozo` | primary (repo) | https://github.com/cozodb/cozo | Bindings (no .NET), MPL-2.0 |
| S20 | `oxigraph/oxigraph` | primary (repo) | https://github.com/oxigraph/oxigraph | Bindings (no .NET), licence |
| S21 | Memgraph BSL licence text | primary (legal) | https://github.com/memgraph/memgraph/blob/master/licenses/BSL.txt | BSL 1.1 restrictions, 2030 change date |
| S22 | `typedb/typedb` | primary (repo) | https://github.com/typedb/typedb | BSL licence, official .NET client |
| S23 | Kythe schema reference | primary (spec) | https://kythe.io/docs/schema/ | VName 5-tuple (quoted) |
| S24 | Wikipedia — Google Kythe | secondary | https://en.wikipedia.org/wiki/Google_Kythe | **US team laid off April 2024** |
| S25 | GitHub Docs — CodeQL incremental analysis | primary (docs) | https://docs.github.com/en/code-security/how-tos/find-and-fix-code-vulnerabilities/scan-from-the-command-line/incremental-analysis | Overlay DB, 5–40% speedup, CLI thresholds |
| S26 | Incrementalizing Production CodeQL Analyses | academic | https://arxiv.org/abs/2308.09660 | Incremental analysis theory |
| S27 | Sourcetrail EOL issue #1214 | primary (repo issue) | https://github.com/CoatiSoftware/Sourcetrail/issues/1214 | **Why it was archived** — maintenance burden |
| S28 | `microsoft/graphrag` | primary (repo) | https://github.com/microsoft/graphrag | GraphRAG architecture |
| S29 | Microsoft Research — GraphRAG project | primary | https://www.microsoft.com/en-us/research/project/graphrag/ | Paper and ablations |
| S30 | RAG vs GraphRAG: a systematic evaluation | academic | https://arxiv.org/html/2502.11371v3 | Vector RAG matches/beats GraphRAG on local QA; cost multiplier |
| S31 | openCypher | primary (spec repo) | https://github.com/opencypher/openCypher | Portable core, TCK |
| S32 | Glean query introduction (Angle) | primary (docs) | https://glean.software/docs/query/intro/ | Angle syntax, recursive rules |

## Source-quality notes

- The **liveness findings** — the most consequential in this base — rest on primary sources wherever
  possible: the archived repository itself for Kuzu ([S11]) corroborated by trade press ([S12]); GitHub's own
  notice for Stack Graphs ([S10]); the project's own EOL issue for Sourcetrail ([S27]); Meta's own engineering
  blog for Glean's continued investment ([S8]). Kythe's de-staffing rests on **Wikipedia** ([S24]) and is
  therefore the weakest of the five — treat it as Verified-but-secondary and re-check before citing it in a
  decision.
- Licences were read from the repository or the licence file, not recalled.
- The Samsung "UnWeaving the Knots of GraphRAG" preprint was not fetched directly; it is referenced through
  [S30]'s context and is **Flagged**.
- **Absence of evidence** is recorded as such: no published C# graph-size or query-latency numbers were
  found despite explicit search, and that is stated as a gap rather than filled with an estimate.
