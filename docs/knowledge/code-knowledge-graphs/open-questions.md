---
id: kb-codegraph-open-questions
title: "Code Knowledge Graphs — open questions & failure modes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [open-questions, failure-modes, project-mortality]
links:
  - { to: kb-code-knowledge-graphs, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The unresolved storage question left by Kuzu's death, the missing performance numbers nobody
  has published, this domain's high project-mortality pattern, and the strongest counter-argument
  — that an in-memory symbol table would do.
---

# Open questions & domain failure modes

## Unresolved by research

1. **What replaces Kuzu?** No embedded, Cypher-speaking, actively maintained, permissively licensed store
   with a .NET API exists as of August 2026. FalkorDB is suggested in migration discussions but is
   Redis-based and server-only. The realistic options each give something up: DuckDB+DuckPGQ (no Cypher,
   research-grade extension), SQLite + recursive CTEs (no property-graph model), or an out-of-process server
   (Neo4j GPLv3, Memgraph BSL). **This is the largest open decision in the whole project.** *(Flagged)*
2. **What does a real C# code graph weigh, and how slow is an impact query?** No published node/edge counts
   for a medium C# solution; no transitive-closure latency figures at a million nodes for any system in this
   context. Kuzu claimed sub-second analytical queries at that scale and can no longer be benchmarked as a
   live option. **We will have to measure this ourselves.** *(Flagged — the most load-bearing gap)*
3. **Is `scip-dotnet` complete enough to consume instead of writing a Roslyn extractor?** Its handling of
   C#'s awkward cases — partial classes, source generators, global using aliases, nullable annotations — has
   not been independently audited. A day's spike would settle it and could remove a large piece of planned
   work. *(Flagged)*
4. **Will any production database implement GQL, and when?** Committed to by Neo4j, no date published.
   Sticking to the openCypher portable core is the hedge; it is not a guarantee. *(Flagged)*
5. **What is CozoDB's actual maintenance state?** MPL-2.0 and technically attractive (embedded, Datalog,
   excellent recursion, time-travel queries) but low activity and **no .NET bindings**, which would mean
   P/Invoke. *(Flagged)*
6. **Are Graphify and CodeSee alive and relevant?** Neither could be confirmed from primary sources; CodeSee
   was acquired by GitHub in 2024 with unclear status since. *(Flagged)*
7. **How much graph context can actually be given to an agent before it degrades?** The 26–85× cost figure
   bounds the budget but says nothing about the quality curve. Our own eval. *(Open)*

## Known failure modes of this domain

- **Project mortality — the pattern that should shape the architecture.** Kuzu (2025), Stack Graphs
  (2024/25), Sourcetrail (2021) dead; Kythe de-staffed (2024); LSIF deprecated. **Four of the systems
  surveyed died within five years.** The architectural response is the `IGraphStore` seam the seed sketch
  already has, plus a documented, re-importable export format — so the next death costs a migration rather
  than a rewrite. Sourcetrail's own EOL issue names the cause: a dependency stack too complex for anyone to
  take over. *(Verified, [S10][S11][S12][S24][S27])*
- **Unstable identity.** If a node's ID is a GUID, or derived from anything that changes on refactor, then
  re-running the extractor churns the graph and history becomes noise. SCIP and Kythe both solve this and
  both make renames a delete-plus-add, which is the correct semantics.
- **Silent incompleteness from partial indexing.** An indexer that skips what it cannot resolve returns a
  well-formed graph missing whole subsystems, and nothing errors. The counter is to emit explicit
  `unresolved` facts with their provenance.
- **Provenance treated as metadata rather than as the point.** Without repo/file/line/commit/extractor+version
  on every node, the graph cannot be audited, diffed meaningfully, or trusted when two extractors disagree.
- **Confusing an index with a database.** SCIP is a navigation index; it does not answer "what transitively
  depends on this table". Choosing an index format and then discovering you need arbitrary queries is a
  predictable and expensive detour.
- **Licence discovery at packaging time.** GPLv3 (Neo4j embedded) and BSL 1.1 (Memgraph, TypeDB) are both in
  the obvious shortlist and both restrict exactly what a shipped desktop product does.
- **Assuming GraphRAG helps.** The published evidence says it does not, for local lookups, and costs 26–85×
  for global ones. Serving a graph to an agent is only a win where the questions are genuinely multi-hop.
- **Incremental optimism.** CodeQL's measured C# speedup is at the *low* end of its 5–40% range. Assuming
  incrementality will deliver a sub-2-second save-to-diagram loop is an assumption, not a finding.

## Disconfirming views we deliberately sought

**Counter-argument 1: a knowledge graph is overkill — an in-memory symbol table would do.**

Visual Studio's Roslyn workspace, ReSharper, and every language server (OmniSharp, Roslyn LSP) serve
go-to-definition, find-all-references and type hierarchy from **in-memory semantic models** over codebases
of millions of lines at sub-100ms, with no graph database at all. Sourcegraph's own SCIP pipeline is
designed for *server-side batch indexing*, not interactive response. The cost of serialising a code graph
to an embedded store and keeping it in sync with file changes may well dominate the cost of the queries it
enables.

*How it fares:* **it is right about code-only questions and wrong about ours.** An in-memory Roslyn
workspace cannot persist provenance across sessions, cannot serve an agent that has no live compiler
session, and — decisively — cannot correlate C# symbols with Bicep resources, SQL tables and runtime spans,
because those facts are not in a Roslyn workspace at all. The graph earns its place at the *joins*, not at
the code. That also sharpens the design: for pure code-navigation questions, the honest answer may be to
defer to the language server rather than to re-implement it worse.

**Counter-argument 2: GraphRAG is not better than vector RAG for code retrieval.**

Two independent evaluations (arXiv:2502.11371; Samsung's "UnWeaving the Knots of GraphRAG") conclude that
vector RAG matches or outperforms GraphRAG on general and local queries at a fraction of the cost, and
Microsoft's own ablations show the graph adds nothing for simple factoid questions.

*How it fares:* **it stands, and it constrains the MCP tool design rather than the graph.** The graph's
value is in the traversal queries an embedding cannot answer — `impact_of`, transitive callers,
declared-versus-observed — not in being a better retrieval corpus. The implication is concrete: do not
build a "search the graph semantically" tool and expect it to beat grep or embeddings; build bounded
traversal tools and let the agent's existing search handle lookups.

**Residual risk both leave standing:** the graph's unique value rests entirely on the cross-domain joins
(code ↔ infra ↔ data ↔ runtime). If those joins turn out to be low-confidence in practice — because the
`metadata service` annotation is missing, or EF mappings are dynamic, or traces are sparse — then what
remains is a slower symbol table with a worse query language. **The joins are the product, and their
quality is currently unmeasured.**
