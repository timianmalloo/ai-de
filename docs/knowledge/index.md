---
id: knowledge-hub
title: "AI-DE Domain Knowledge — index"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [knowledge-base, index, ai-native-ide, multi-agent, modelling]
links:
  - { to: architecture, rel: relates-to }
  - { to: seed-ai-native-ide-sketch, rel: documents }
  - { to: seed-agent-coordination-spec, rel: documents }
review-by: 2026-11-21
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture" }
summary: >-
  The synthesis over ten sourced domain knowledge bases for AI-DE — an AI-native IDE built on a
  code knowledge graph — including the four findings that change the seed architecture and the
  ranked list of spikes that would settle the rest.
---

# AI-DE Domain Knowledge — index

**Domain & problem:** Building a development environment for working with coding agents — a Windows shell
hosting agent terminals beside derived visual panes, over a local daemon whose **code knowledge graph** is
built by artifact-only extractors, served to agents via **MCP**, rendered as generated diagrams, and worked
in parallel by **multiple coordinated agents**.

**Compiled:** 2026-08-23 · **Lead:** Domain Researcher · **Status:** fresh
**Seed material:** [`seed-material/`](seed-material/) — the originating architecture sketch and the agent
coordination specification, brought in-repo so this base's internal citations resolve.

---

## The four findings that change the plan

Everything else in these ten bases is context. These four are decisions.

### 1. Kuzu is archived — the storage choice must be remade

Kùzu Inc. archived the repository around **2025-10-10**; v0.11.3 is final; the community .NET binding is
orphaned with it. Worse, **no replacement satisfies all four criteria** the seed architecture implicitly
assumed — embedded, actively maintained, permissively licensed, first-class .NET. Every candidate gives one
up: DuckDB+DuckPGQ has no Cypher, SQLite has no property-graph model, Neo4j embedded is Java-only and GPLv3,
Memgraph and TypeDB are BSL. **The `IGraphStore` interface in the sketch is what makes this survivable** —
it was the right call and it is now load-bearing. → [`code-knowledge-graphs/`](code-knowledge-graphs/index.md)

### 2. The leases in the coordination spec are advisory, not exclusive

Kleppmann's fencing-token argument is unrefuted: a process can hold an **expired** lease and still be
executing, so TTL plus heartbeat gives *efficiency*, never *correctness*. The fix requires **the resource
being written** to reject a stale fencing token. Either add one, or state plainly that claims are
advisory-for-efficiency. Silence is the only unacceptable option.
→ [`multi-agent-coordination/`](multi-agent-coordination/index.md)

### 3. MCP has changed underneath the plan

The current revision `2026-07-28` is **stateless** — no `initialize` handshake, no sessions — and it
**deprecates Sampling, Roots and Logging**, replacing server-initiated interaction with MRTR. The good news
is larger: the **C# SDK is jointly maintained by Microsoft and Anthropic**, stable at 2.2.0. The other half
is that **Claude Code's hooks have no Copilot equivalent** — documented absence, not oversight — so the file
event bus is the universal floor and hooks are a Claude-Code accelerator.
→ [`mcp-and-agent-integration/`](mcp-and-agent-integration/index.md)

### 4. The thesis survives its own history, in a specific form

"The models are the product" has failed five times since the 1970s — CASE, 4GL/RAD, MDA, low-code, and now
LLM spec-driven tools — defeated by five structural forces. **Four of those five do not apply to the
code-derived-views variant**, because the code stays authoritative: there is no escape hatch to collapse,
no proprietary editor to maintain in, no workflow imposed on developers, and divergence is structurally
impossible. And SysML v2.0 (September 2025) adopted exactly this position — *diagrams are views of the
abstract syntax, not the source of truth*. **State the thesis in the derived-views form, not the ambitious
one.** → [`uml-mde-and-4gl/`](uml-mde-and-4gl/index.md)

---

## The knowledge bases

The ten below establish the AI-DE thesis and substrate. Four more (2026-08-29) establish the **WPF client's
visual & interaction layer** — the modern-soft look, the operational panes, the unified graph experience, and
the content-rendering surfaces:

| Topic | What it establishes | The finding to read first |
|---|---|---|
| [**Modern & Soft WPF Styling**](wpf-modern-ui-styling/index.md) | DWM rounded corners/Mica, WindowChrome, the .NET Fluent theme, MIT control libraries, soft-shadow perf, IDE UX exemplars | The **built-in .NET 9/10 Fluent theme** makes the modern look library-optional; **effects don't composite over hosted panes** (airspace) |
| [**Operational & Test Dashboards**](operational-and-test-dashboards/index.md) | Test reporting (Allure/ReportPortal), CI-as-DAG, RED/USE metrics, MIT charting libs (ScottPlot/LiveCharts2/OxyPlot) | A dashboard is **6–12 actionable panels with drill-down**, and must **expose the silent failures** (means hide tails; green ≠ gate ran) |
| [**Unified Graph Experience & Visualization**](graph-experience-and-visualization/index.md) | GraphRAG (+LazyGraphRAG/LightRAG), 2D/3D force-graph libs (Sigma/Cytoscape/3d-force-graph, MIT), node-based UIs, composing Obsidian + Graphify | Embed a **web force-graph in WebView2** (not native GraphX); the load-bearing new piece is the **node-introspection router** fusing code+knowledge |
| [**Editor & Content Rendering Surfaces**](editor-and-content-rendering-surfaces/index.md) | Code viewing (Monaco/AvalonEdit/RoslynPad — MIT) and markdown/HTML (Markdig/Markdig.Wpf, WebView2) | Every option is **MIT**; **native for plain markdown & C#, web (Monaco/HTML) for breadth & interactivity**; only Monaco is cleanly reusable from VS Code |

**Material update flagged:** the graph-experience base records **LazyGraphRAG (~700× cheaper global queries)**,
which revises Code-Knowledge-Graphs finding #8 (GraphRAG = 26–85× cost) — that base is flagged for review.

The user's diagram / UML / ERM requests were **already covered** and are cross-referenced, not duplicated:
diagramming → `diagram-generation`, UML/MDE/generative → `uml-mde-and-4gl`, ERM/ORM → `domain-modeling-and-erm`,
trace/topology → `microservice-interaction-visualization`.
diagramming → `diagram-generation`, UML/MDE/generative → `uml-mde-and-4gl`, ERM/ORM → `domain-modeling-and-erm`,
trace/topology → `microservice-interaction-visualization`.

## The ten thesis & substrate bases

| Topic | What it establishes | The finding to read first |
|---|---|---|
| [**Code Knowledge Graphs**](code-knowledge-graphs/index.md) | Graph stores, GQL, SCIP/Glean/Kythe/CodeQL prior art, symbol identity, incrementality | **Kuzu is archived**; no store meets all four criteria; copy SCIP's symbol grammar |
| [**Code & Infra Extraction**](code-and-infra-extraction/index.md) | Roslyn, ScriptDom, Bicep, ts-morph, tree-sitter; what static analysis cannot see | **DI, routes and EF mapping are structurally invisible**; `Azure.Bicep.Core` is unsupported |
| [**MCP & Agent Integration**](mcp-and-agent-integration/index.md) | The spec, SDKs, client matrix, hooks, security | Spec is **stateless**; Sampling/Roots deprecated; **no Copilot hook surface** |
| [**Multi-Agent Coordination**](multi-agent-coordination/index.md) | Claims-log coordination, leases, worktrees, the published evidence | **Fencing tokens**; Anthropic's own caution on coding parallelism; METR's −19% |
| [**AI-Native IDE Shell**](ai-native-ide-shell/index.md) | WPF vs WinUI 3, ConPTY, WebView2, docking, OSC 133 | WPF is right **today**; the terminal control is an unsupported CI artefact; **OSC 133 is free signalling** |
| [**Diagram Generation**](diagram-generation/index.md) | Mermaid, D2, PlantUML, Structurizr, Cytoscape/ELK, layout | **Layout stability** is the unsolved problem; PlantUML's default profile is a security hole |
| [**Microservice Interaction Visualization**](microservice-interaction-visualization/index.md) | OTel, service graphs, trace→sequence, declared vs observed | **Messaging conventions are Development-status**; pub/sub breaks parent-child; reflexion vocabulary |
| [**Domain Modelling & ERM**](domain-modeling-and-erm/index.md) | DDD canon, extractable stereotypes, ERM notations, EF Core bridge | **Bounded contexts are not extractable**; the anemic model is invisible to structure |
| [**UML, MDE & 4GL**](uml-mde-and-4gl/index.md) | The fifty-year graveyard of models-as-product, and the narrow successes | Four of five failure modes **do not apply** to derived views |
| [**Azure & Cloud Architecture**](azure-cloud-architecture/index.md) | Bicep/ARM extraction, Resource Graph, C4, icon licensing | **Inventory is not architecture** — the curation policy is the product |

---

## Cross-cutting design implications

These recur across three or more bases, which is what makes them worth stating once here.

- **Extraction fidelity is not uniform, and pretending otherwise is the failure mode.** Types are resolved
  facts; DI and route edges are pattern matches; Bicep loops are unresolved expressions; trace edges are
  observations from one run. **Every node and edge needs a confidence attribute and the evidence that
  produced it** — one field, and it is the difference between a graph that can be trusted and one that will
  be confidently wrong. *(extraction · azure · micro · domain)*
- **Curation is the product; extraction is the commodity.** Azure inventory, complete ER diagrams and
  component-level UML all fail the same way — accurate, complete, unreadable. The policy deciding what
  becomes a node, what folds into a parent, and what is elided is where the value is.
  *(azure · diagrams · domain)*
- **The joins are the moat, and their quality is unmeasured.** A graph of C# alone is a slower symbol table
  with a worse query language than Roslyn already provides. The value is code ↔ infra ↔ data ↔ runtime — and
  if `metadata service` is missing, EF mappings are dynamic, or traces are sparse, those joins are
  low-confidence. **Measure them early.** *(codegraph · extraction · azure · domain)*
- **Prove things with numbers nobody has published.** No node/edge counts for a C# code graph, no
  transitive-closure latency at a million nodes, no merge-conflict rate for concurrent agents, no post-2020
  UML adoption data. Several are gaps we would fill by measuring.
  *(codegraph · coord · uml)*
- **Bound what reaches an agent's context.** GraphRAG's global queries cost **26–85×** local retrieval, and
  published evaluations find vector RAG matches or beats graph retrieval for local lookups. MCP tools must
  return bounded neighbourhoods and summaries, never subgraphs. *(codegraph · mcp)*
- **Never make a derived view editable.** It is the one rule that keeps this project out of the cycle that
  killed CASE, 4GL and MDA. *(uml · diagrams)*
- **Design for project mortality.** Four systems surveyed died within five years (Kuzu, Stack Graphs,
  Sourcetrail, plus LSIF deprecated and Kythe de-staffed). Interfaces at every substrate boundary and a
  documented export format turn the next death into a migration. *(codegraph · diagrams)*

---

## The spikes, ranked

Each is a day or two and each would remove or reshape a large piece of planned work.

1. **Pick the graph store** — benchmark SQLite+CTEs and DuckDB+DuckPGQ on a real C# solution's graph, and
   produce the node/edge and impact-query numbers nobody has published. *(the largest open decision)*
2. **`scip-dotnet`** — is there a usable C# SCIP indexer? If yes, the Roslyn extractor may be unnecessary.
3. **Source generators vs design-time builds** — if generated code is absent from the semantic model, the DI
   graph may be unrecoverable, which changes what the domain view can claim.
4. **SVG byte-determinism** — render the same source twice on two Chrome versions and diff. Decides whether
   generated diagrams can be committed as images.
5. **Terminal renderer** — prototype `EasyWindowsTerminalControl` and xterm.js-in-WebView2 side by side.
   Owning ConPTY keeps this deferrable, which is why it is fifth and not first.
6. **The Azure icon licence** — a real answer before anything ships that renders them.

---

## How to use this base

Personas and the design skills cite these files as evidence (BoK §III.1). Each topic's `references.md`
carries the constants to **quote rather than recall**, and each `sources.md` gives the access-dated
provenance. Confidence labels are used consistently throughout: **Verified** (observed in an authoritative
primary source), **Inferred** (reasoned from verified facts), **Flagged** (single, weak, dated, contested, or
an absence of evidence).

**Freshness varies sharply by topic.** MCP is the fastest-moving — re-read its primary sources before any
MCP-facing design that starts more than a quarter from now. Graph-store liveness has a short half-life for
the same reason Kuzu's death was the headline. UML and DDD are stable for years.

**Handoff:** → `/adddomainexperts` (expert lenses citing this base) → `/specify` → `/define-architecture`.
The architecture work should open with the four findings above, because each of them changes an answer the
seed sketch already gave.
