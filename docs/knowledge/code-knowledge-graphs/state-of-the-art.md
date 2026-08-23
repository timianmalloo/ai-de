---
id: kb-codegraph-sota
title: "Code Knowledge Graphs — state of the art"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [graph-stores, opencypher, gql, scip, glean, kythe, codeql, graphrag]
links:
  - { to: kb-code-knowledge-graphs, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The current state of embedded graph stores, graph query languages, the surviving code-graph
  systems, symbol-identity schemes, incremental indexing and graph-served retrieval for LLM
  agents.
---

# State of the art — code knowledge graphs & graph stores

## Embedded and local graph stores

| Store | Query language | Embeddable | .NET story | Licence | Status | Caveat that decides it |
|---|---|---|---|---|---|---|
| **Kuzu** v0.11.3 | Cypher (openCypher superset) | in-process, serverless | `KuzuDot`, `ladybug.net` — **both orphaned** | MIT | ⛔ **Archived Oct 2025** | Abandoned; v0.11.3 final; no security patches |
| **DuckDB + DuckPGQ** | SQL/PGQ (SQL:2023 Part 16) + SQL | embedded native | **DuckDB.NET**, active, Apache-2.0 | MIT (DuckDB); research extension | ✅ Active (CWI Amsterdam) | DuckPGQ is a community extension, not bundled |
| **Neo4j Community** | Cypher (Neo4j dialect) | Java-only embedded | `Neo4j.Driver` (Apache-2.0) over Bolt | **GPLv3** | ✅ Active | Copyleft; .NET must run the server out-of-process |
| **Memgraph** | openCypher, GQL previews | process/Docker | Neo4j .NET driver via Bolt | **BSL 1.1** → Apache-2.0 in 2030 | ✅ Active | BSL prohibits redistribution; embedding needs an OEM licence |
| **Apache AGE** | openCypher via `cypher()` + SQL | PostgreSQL extension | Npgsql + `cypher()` | Apache-2.0 | ✅ Active (ASF TLP since 2022) | Needs a PostgreSQL server; partial openCypher conformance |
| **CozoDB** | Datalog (CozoScript) | Rust/Python/JS/Java | ❌ **none** | MPL-2.0 | ✅ Active (low activity) | No C# story; unfamiliar paradigm; excellent recursion |
| **Oxigraph** | SPARQL 1.1 | Rust crate; Python/WASM | ❌ **none** (HTTP/dotNetRDF workaround only) | MIT/Apache-2.0 | ✅ Active | RDF triples, not property graphs; verbose for code facts |
| **TypeDB** | TypeQL | server process | official .NET client | **BSL 1.1** → AGPL | ✅ Active | BSL restricts commercial embedding; steep learning curve |
| **SQLite + recursive CTEs** | SQL | best-in-class embedded | `Microsoft.Data.Sqlite` (Apache-2.0) | public domain | ✅ Active | Not a property graph — adjacency table + CTE; no Cypher |

*(Verified per row, [S11]–[S22])*

**The intersection is empty.** Nothing is simultaneously embedded, active, permissively licensed,
Cypher-speaking and .NET-native. Every design must give up one of those five. *(Inferred from the table)*

For the record, Kuzu's model — columnar on-disk storage with CSR adjacency indices, vectorised execution
and MVCC ACID — was genuinely well suited to batch analytical graph queries over code. That is now archival
information. *(Verified, [S11])*

## Query languages

**openCypher** is the open community specification: `MATCH`, `CREATE`, `MERGE`, `WHERE`, `RETURN`, `WITH`,
`UNWIND`, variable-length paths, parameters. That core is highly portable across Neo4j, Memgraph, ArcadeDB
(reporting 97.8% TCK pass), Amazon Neptune, Apache AGE, and formerly Kuzu. **Not portable**: stored
procedures, APOC functions, administrative and index commands, vendor aggregations. Neo4j's Cypher is a
superset with substantial proprietary extension. *(Verified, [S31])*

**ISO/IEC 39075:2024 — GQL** was published April 2024, the first new ISO database-language standard since
SQL. It is designed around openCypher's portable core plus schema-constrained and schema-free modes,
session and transaction control, and composite graph types. **No production database fully implements it**
as of August 2026; a reference parser exists (`gqlcpp/gql`, Apache-2.0, C++ AST only); Neo4j has publicly
committed to convergence. *(Verified, [S1][S2])*

**SQL/PGQ** (SQL:2023 Part 16) embeds property-graph matching inside SQL via
`GRAPH_TABLE(… MATCH …)`; DuckPGQ implements it. *(Verified, [S14][S15])*

**SPARQL 1.1** is the W3C RDF standard — federated queries, property paths, named graphs — and does not
port to property-graph engines. **Gremlin** (Apache TinkerPop) is imperative traversal, used by Neptune and
JanusGraph, and is not the idiom of any code-graph system surveyed. **Datalog** is: CozoDB's CozoScript and
Glean's Angle both express transitive closure as a recursive rule without explicit path quantifiers, which
is exactly the "all transitive callers of X" query. *(Verified, [S32]; SPARQL/Gremlin Inferred)*

## The surviving prior art

**Glean (Meta)** — models schema-defined typed **facts** about source: definitions, references, types,
calls, inheritance, imports, module dependencies, produced by language indexers and also ingested from
SCIP/LSIF. Queried in **Angle**, a strongly-typed Datalog-inspired language with native recursive rules.
Incremental by design: derived predicates re-evaluate on fact change without a full re-index. Server-only,
not in-process; no .NET; BSD 3-Clause. **Actively invested in** as of Meta's December 2024 engineering post.
*(Verified, [S7][S8][S9][S32])*

**SCIP (Sourcegraph)** — a Protobuf index of source locations, definitions, references, documentation and
relationships (implements, calls), centred on **human-readable string symbol IDs**. Grammar:

```
Symbol     = scheme SP package SP { descriptor }
descriptor = name [ disambiguator ] suffix
suffix     = '/'   namespace
           | '#'   type (class, struct, interface)
           | '.'   term (field, enum value, variable)
           | '('   method / function
           | ':'   method or type parameter

example:  scip-go github.com/gorilla/mux/ Router#HandleFunc(
```

Against LSIF: ~5–8× smaller uncompressed, ~3× faster to process, no opaque numeric IDs, easier incremental
indexing — Meta reported ~550 lines of Glean mapping for SCIP versus ~1500 for LSIF. Indexers exist for C#
(`scip-dotnet`), Go, Java/Scala/Kotlin, TypeScript/JavaScript, Rust, Python, Swift, Ruby. The repository
moved to the `scip-code` organisation. **LSIF read support was dropped in Sourcegraph 4.6 and the migration
is irreversible.** *(Verified, [S3][S4][S5][S6])*

**Kythe (Google)** — VName-addressed nodes and edges over a universal cross-language schema. The **VName**
is a 5-tuple `{corpus, root, path, language, signature}` where `signature` is indexer-defined and must be
stable for the same input. Databases are per-compilation-unit `.kzip` archives merged by a serving layer, so
incrementality is at file/unit granularity. Apache-2.0. **Low-activity maintenance** — the US development
team was laid off in April 2024 and maintenance moved to a new team. *(Verified, [S23][S24])*

**CodeQL (GitHub)** — a full semantic AST plus data-flow and control-flow graph for 10+ languages, queried
in QL (object-oriented Datalog), aimed at security and quality rather than navigation. Entity IDs are
**opaque and database-internal**, explicitly not designed for stable cross-build symbol identity.
Incremental support is real: **overlay databases** cache the default branch and merge a delta, and
**diff-informed analysis** scans only changed lines, for a **5–40%** speedup (C#/C++ at the low end, Go at
the high end); requires CLI ≥ 2.21.0 and ≥ 2.23.8 respectively, and Git ≥ 2.38.0 with all files tracked.
Free for open source; commercial licence for private repositories. *(Verified, [S25][S26])*

**The dead and the dying** — GitHub's **Stack Graphs** (scope-graph name resolution from TU Delft theory,
elegant and incremental) carries the notice *"This repository is no longer supported or updated by
GitHub"*; **Sourcetrail** went commercial → open source (2019) → archived (2021), its own EOL issue citing
insufficient uptake and a Qt/LLVM/Java dependency stack no community could take over; **Kythe** is
de-staffed. *(Verified, [S10][S27][S24])*

**Adjacent** — **tree-sitter** provides incremental, error-resilient concrete syntax trees with bindings in
every major language including C#, and is the parse layer under many of the above but not a semantic graph;
**Semgrep** is AST pattern-matching, not a graph store; **Moose/FAMIX** is a language-independent code
meta-model alive in the research community; **srcML** renders source as marked-up XML. **Graphify** and
**CodeSee** could not be confirmed from primary sources. *(Verified except the last two, which are Flagged)*

## Serving a graph to LLM agents

**GraphRAG** (Microsoft) extracts entities and relations with an LLM, detects communities, summarises them,
and augments retrieval. The published evaluation is unflattering for the general case: arXiv:2502.11371 and
Samsung's "UnWeaving the Knots of GraphRAG" both find **vanilla vector RAG matches or beats GraphRAG on
general and local QA**, and Microsoft's own ablation shows the graph adds nothing for simple factoid
questions. GraphRAG's advantage is specifically **multi-entity, multi-hop and community-summarisation**
queries — which is the shape of "what does changing this interface affect", but the win is not automatic.
The cost is the decisive number: **global-context queries cost 26–85× the tokens of local vector
retrieval**. Hybrid — vector first, graph for relational context — dominates production deployments.
*(Verified, [S28][S29][S30])*

## The frontier

- **A replacement for Kuzu.** No embedded Cypher store with a .NET story has emerged. FalkorDB is suggested
  in migration discussions but is Redis-based and server-only. *(Flagged — unresolved)*
- **A production GQL implementation.** Committed to, not shipped.
- **Published graph metrics for real codebases.** Nobody has published node/edge counts for a medium C#
  solution or transitive-closure latency at a million nodes. Kythe's documentation implies millions of nodes
  for Google-monorepo compilations; CodeQL databases for ~100k LOC C# run from hundreds of MB to several GB.
  Both are **Inferred**, and neither answers the question.
