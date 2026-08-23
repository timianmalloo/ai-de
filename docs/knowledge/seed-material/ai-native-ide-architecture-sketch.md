---
id: seed-ai-native-ide-sketch
title: "Seed — AI-Native IDE Architecture Sketch (v0.1)"
type: doc
status: draft
owner: "@timianmalloo"
phase: ""
tags: [seed-material, ai-native-ide, graph, mcp]
links:
  - { to: knowledge-hub, rel: relates-to }
  - { to: architecture, rel: relates-to }
review-by: 2026-11-21
review-suggested:
  - { by: knowledge-hub, on: 2026-08-23, reason: "New domain knowledge base established; four findings change prior architecture assumptions (Kuzu archived, MCP stateless, lease fencing gap, thesis framing)" }
summary: >-
  The originating design sketch for an AI-native IDE ("Atlas"): a thin WPF shell hosting
  agent terminals and WebView2 panes over a local daemon whose Kuzu graph, built by
  artifact-only extractors, is served to agents via MCP and rendered as derived diagrams.
---
# AI-Native IDE — Architecture Sketch (v0.1)

*A working design to reason over before writing the detailed spec. Working name used throughout: **Atlas** (rename at will).*

---

## 0. Core thesis

Code is transient; the **models are the product**. Therefore:

1. The single source of truth is the repo's generated artifacts (Bicep, DDL, code in C#/JS/Java/Python/Rust).
2. Everything visual (architecture, domain, data, flow, dependencies, knowledge) is a **derived view over one graph**, never hand-drawn.
3. The shell is a thin host: native terminals for agents (Claude Code, Copilot CLI), web-rendered panes for everything visual.
4. Cross-project/cross-session context is not shared through terminal state — it flows through the **graph, exposed to agents via MCP**.

The graph is the product. The shell is the window. The extractors are the supply chain.

```
┌─────────────────────────────  Atlas Shell (WPF, .NET 9)  ─────────────────────────────┐
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────────────────────────────────┐  │
│  │ Terminal Pane │  │ Terminal Pane│  │  WebView2 Panes (tabs / splits)            │  │
│  │ (Claude Code) │  │ (Copilot /   │  │  • Graph Explorer (Cytoscape.js + ELK)     │  │
│  │  ConPTY       │  │  pwsh)       │  │  • Architecture (C4 via Structurizr/D2)    │  │
│  │               │  │  ConPTY      │  │  • Domain / Class (Mermaid)                │  │
│  │               │  │              │  │  • ERD (Mermaid/tbls)                      │  │
│  │               │  │              │  │  • Sequence/Activity (Mermaid from traces) │  │
│  └──────┬───────┘  └──────┬───────┘  └──────────────────┬─────────────────────────┘  │
│         │  stream tap      │                             │ ws / postMessage           │
└─────────┼──────────────────┼─────────────────────────────┼────────────────────────────┘
          │                  │                             │
          ▼                  ▼                             ▼
   ┌────────────────────────────────────────────────────────────────┐
   │              Atlas Daemon (local service, ASP.NET Core)        │
   │  • Graph store (Kuzu embedded)   • View generator (query→DSL)  │
   │  • Watcher/orchestrator          • Pub/sub (WebSocket)         │
   │  • MCP server (stdio + http)  ◄── Claude Code / Copilot agents │
   └───────────────────────────▲────────────────────────────────────┘
                               │ graph deltas (JSON)
        ┌──────────────┬───────┴──────┬──────────────┬─────────────┐
        │ Roslyn       │ Bicep        │ DDL          │ OTel trace  │  + JS/TS, Java,
        │ extractor    │ extractor    │ extractor    │ extractor   │    Python, Rust
        └──────────────┴──────────────┴──────────────┴─────────────┘
                        all reading ONLY repo artifacts
```

---

## 1. The Shell (host app)

### Stack
- **WPF on .NET 9** (not WinUI 3 — better docking ecosystem, the official terminal control targets WPF, no packaging friction).
- **Docking**: AvalonDock (mature) or Dock.Avalonia-style custom. Persist layouts per workspace as JSON.
- **Terminals**: `Microsoft.Terminal.Wpf` (the same control VS embeds) as primary; **EasyWindowsTerminalControl** as fallback/alternative because it exposes the ConPTY byte streams more openly.
- **Visual panes**: one shared **WebView2** environment, multiple `CoreWebView2` instances mapped to virtual host (`https://atlas.local/…` → local asset folder). All diagram/graph rendering is web tech.

### Terminal session model
```csharp
record TerminalSession(
    Guid Id,
    string Title,
    string WorkingDir,
    string RepoId,            // links session → graph scope
    AgentKind Agent,          // ClaudeCode | CopilotCli | Pwsh | Custom
    string Launch,            // e.g. "pwsh -NoLogo -Command claude"
    Dictionary<string,string> Env);  // inject ATLAS_WORKSPACE, ATLAS_MCP endpoint, etc.
```
- The shell owns ConPTY lifetimes; sessions survive pane rearrangement.
- Env injection is the quiet superpower: every agent process gets `ATLAS_WORKSPACE_ID`, `ATLAS_REPO_ID`, and the MCP endpoint — so the agent is *born knowing* which slice of the graph it lives in.

### Stream tap (shell ↔ agent signaling)
Two mechanisms, use both:
1. **Passive tap**: the shell watches ConPTY output for patterns (e.g., Claude Code tool-use summaries, `git commit` completions) → debounce → trigger incremental extract.
2. **Active signals — file-based event bus** (more robust than parsing ANSI): agents/hooks write JSON events to `<workspace>/.atlas/events/`. Daemon watches that folder. Claude Code **hooks** (post-tool-use / stop hooks) call `atlas emit changed --paths ...`. This avoids fragile terminal scraping and works identically for any agent.

Custom OSC escape sequences (agent prints `ESC]9;atlas;refresh\a`) are a nice-to-have v2; the file bus is v1.

### Panes and refresh
- Each visual pane = a URL + a subscription. Pane declares interest ("erd:repoA", "c4:workspace", "graph:query=…").
- Daemon pushes `viewInvalidated` over WebSocket → pane re-fetches rendered view. No polling.

---

## 2. Workspace & context model

- **Workspace** = named set of repos + one graph scope. Lives at `~/.atlas/workspaces/<name>/workspace.yaml`:

```yaml
name: commerce
repos:
  - id: ordering       ; path: C:\src\ordering
  - id: catalog        ; path: C:\src\catalog
  - id: infra          ; path: C:\src\platform-bicep
graph: kuzu            # store per workspace
conventions:
  serviceNameFrom: "bicep:metadata.service | csproj:AssemblyName"
```

- **Per-repo** `.atlas/repo.yaml`: which extractors run, entry points, DDD annotation style, DDL location, bicep entry files.
- **Cross-project sharing falls out for free**: one workspace graph, `repo` is just a property on every node. "Related but different projects" = same workspace. A second, wider workspace can federate several (v2: cross-workspace read-only mounts).
- Session context (what an agent learned) that should persist is written **into the graph** as `Note`/`Decision` nodes via MCP — not into transcripts. This is the difference between "shared scrollback" and shared knowledge.

---

## 3. Graph store (the brain)

### Choice: **Kuzu**, embedded in the daemon
| Option | Pros | Cons |
|---|---|---|
| **Kuzu** (embedded) | File-based, zero-ops, Cypher, fast, C#-callable, per-workspace DB file, trivially backed up/committed | Younger project; no built-in UI (you're building one anyway) |
| Neo4j Community | Mature, Bloom-ish tooling, huge Cypher docs | A server to run, licensing edges, heavyweight for a desktop tool |
| SQLite + edges table | Boring, bulletproof | You reimplement graph queries; path/impact queries get painful |

Recommendation: **Kuzu behind an interface** (`IGraphStore`) so a swap to Neo4j stays possible if the workspace graph ever needs to be multi-user/server-hosted.

### Schema (node + edge taxonomy)

**Node labels**
- Structure: `Repo`, `Project` (csproj/package), `Package` (external dep)
- Architecture: `Service`, `Component`, `Endpoint`, `Message` (event/command), `AzureResource`
- Domain: `BoundedContext`, `Aggregate`, `Entity`, `ValueObject`, `DomainEvent`, `Type` (plain class)
- Data: `Database`, `Schema`, `Table`, `Column`
- Flow: `Trace`, `Span` (retained selectively)
- Knowledge: `Decision` (ADR), `Note`, `Term` (ubiquitous language)

**Edge types**
`CONTAINS`, `DEPENDS_ON`, `REFERENCES`, `IMPLEMENTS`, `PERSISTS_TO` (Aggregate→Table), `EXPOSES` (Service→Endpoint), `PUBLISHES` / `CONSUMES` (→Message), `DEPLOYED_AS` (Service→AzureResource), `CALLS` (Span-derived), `DOCUMENTED_BY`, `RELATES_TO` (Note/Term links — your Obsidian-style edges)

**Every node carries provenance**:
```json
{ "id": "cs:Ordering.Domain.Order",
  "label": "Aggregate",
  "repo": "ordering", "file": "src/Domain/Order.cs", "line": 14,
  "commit": "abc123", "extractor": "roslyn@1.3", "seenAt": "…" }
```

### ID stability (the make-or-break detail)
IDs must be **deterministic from the artifact**, never GUIDs: `cs:{FQN}`, `sql:{db}.{schema}.{table}`, `bicep:{module}/{symbolicName}`, `pkg:nuget/{id}`. Renames become delete+add (correct!), and re-runs are idempotent upserts. Extractors emit **full snapshots per scope** and the daemon diffs → history for free, "what changed since Tuesday" as a query.

---

## 4. Extractors (the supply chain)

### Common contract
Every extractor is a standalone CLI: `atlas-extract-<kind> --repo <path> --out delta.json` emitting one normalized format:
```json
{ "scope": "repo:ordering/csharp",
  "nodes": [ … ], "edges": [ … ] }
```
Scope = the unit of replacement (daemon deletes-then-inserts within scope). This makes extractors independently testable, runnable in CI, callable from Claude Code hooks, and language-appropriate (the Java one can be Java).

### Roslyn extractor (C#) — the flagship
- Loads solution via `MSBuildWorkspace`; walks symbols.
- Emits: types, inheritance, project refs, NuGet deps.
- **DDD stereotypes**: resolved in priority order — (1) attributes you define (`[Aggregate]`, `[ValueObject]`, `[DomainEvent]`), (2) marker interfaces/base classes (`IAggregateRoot`, `Entity<T>`), (3) convention (namespace `*.Domain.*`). You mandate the attribute package across repos → domain views become precise, not a class-dump.
- **Higher-value passes** (phase 2): DI registrations (`AddScoped<I,T>`) → `IMPLEMENTS`/wiring edges; ASP.NET endpoints → `Endpoint` nodes; MediatR/handler patterns → `CONSUMES` edges; EF Core `DbSet`/mapping → `PERSISTS_TO` edges (this stitches domain↔data automatically).

### Bicep extractor
- `bicep build` → ARM JSON (fully parseable) → `AzureResource` nodes, `dependsOn` + module structure → edges.
- **Code↔infra stitching**: mandate `metadata service = 'ordering'` (or a tag) in Bicep on anything owned by a service → `DEPLOYED_AS` edges. Convention where annotation is missing (name matching), flagged as low-confidence.

### DDL extractor
- T-SQL: **ScriptDom** (Microsoft's real parser, NuGet, excellent) → tables, columns, FKs, indexes directly from `.sql` files. No live DB needed.
- Postgres/other: fall back to ephemeral container in CI + **tbls**, or pgsql-parser.
- FKs → `REFERENCES` edges; ERD is then a pure projection.

### Other languages (deliberately thinner)
- **JS/TS**: `ts-morph` — imports, exported classes/types.
- **Python**: import graph + `pyreverse` for classes.
- **Java**: JavaParser (extractor itself in Java, fine — contract is JSON).
- **Rust**: `cargo metadata` (deps) + `cargo-modules` (module graph). Accept class-level detail is weak here; dependency + module level is still valuable.

### OTel trace extractor (execution flow — the honest one)
Static call-graph extraction lies in a DI/async/message-driven world. So:
- Run a tiny **OTLP collector** in the daemon (or point the .NET Aspire dashboard exporter at it).
- Local runs emit traces → span trees → **Mermaid `sequenceDiagram`** per named scenario ("PlaceOrder"), participants = services/components resolved via the graph.
- Traces also mint `CALLS` edges (runtime-observed, marked as such) — so the dependency graph shows *declared* vs *observed* dependencies. That distinction alone will earn its keep.
- Keep static Roslyn call-flow only for intra-service straight-line flows (activity diagrams of a single handler).

---

## 5. View generation (diagrams as queries)

**Pattern: `Cypher query → projection model → text DSL → renderer`.** No diagram is ever authored; every diagram is a saved query + template.

| View | Query shape | DSL | Renderer |
|---|---|---|---|
| C4 Context/Container | Services, DEPLOYED_AS, DEPENDS_ON between services | **Structurizr DSL** (generated) or D2 | Structurizr Lite (container) or D2→SVG |
| Component | CONTAINS within a service | D2 / Mermaid | in-pane |
| Domain model | Aggregate/Entity/VO subgraph per BoundedContext | Mermaid `classDiagram` | Mermaid.js live |
| ERD | Table/Column/REFERENCES per database | Mermaid `erDiagram` | Mermaid.js live |
| Sequence | Span tree per scenario | Mermaid `sequenceDiagram` | Mermaid.js live |
| Dependency graph | raw graph, filters | none | **Cytoscape.js + ELK** interactive |
| Knowledge graph | Term/Note/Decision + RELATES_TO | none | Cytoscape.js (Obsidian-style) |

Notes:
- **Generated DSL/diagrams are also written to `<repo>/docs/diagrams/`** and committed → reviewable diffs of your architecture in PRs ("this change added a dependency from Catalog→Ordering" shows up in `git diff`), and Obsidian/Graphify keep working as passive viewers for free.
- The **Graph Explorer pane** is the flagship UI: search box, label/repo filters, expand-neighbors, pin, "impact of" mode (transitive closure highlight). Cytoscape.js + elkjs covers all of it; this is where the Obsidian feel lives.

---

## 6. MCP server (the context-sharing mechanism)

Hosted in the daemon (C# MCP SDK), exposed via stdio launcher + local HTTP. Registered in Claude Code config per workspace; Copilot likewise where MCP is supported.

**Tools (v1):**
| Tool | Purpose |
|---|---|
| `graph_query(cypher)` | Raw escape hatch for the agent |
| `describe(node_id)` | Node + neighbors + provenance ("what is Order?") |
| `find(term)` | Fuzzy search across names, terms, notes |
| `impact_of(node_id)` | Transitive dependents — "what breaks if I change this table" |
| `architecture(scope)` | Prose+DSL summary of a service/context — cheap agent grounding |
| `record_decision(title, body, links[])` | Agent writes an ADR node — **this is how session knowledge persists** |
| `link(a, b, type, note)` | Agent adds knowledge edges |
| `list_terms(context)` | Ubiquitous language lookup |

**Why this is the crown jewel:** a Claude Code session in `catalog` can ask `impact_of(sql:orders.dbo.Order)` and see consequences in `ordering` — cross-repo context without pasting anything. And `record_decision` means what one session learns, every future session (any repo, any agent) inherits. Write-tools write **only** knowledge nodes (Decision/Note/Term/links) — extractors own all artifact-derived truth, so agents can annotate reality but never fabricate it.

---

## 7. Refresh loop

```
save file ──► FileSystemWatcher (daemon, per repo, debounced ~500ms)
          ──► route by path → relevant extractor(s), scope-limited
          ──► delta upsert → diff → events
          ──► WebSocket "viewInvalidated" → panes re-render
agent hook ─► atlas emit … → .atlas/events/ → same pipeline (authoritative signal)
CI ────────► same extractor CLIs → commit generated docs/diagrams (drift check)
```
Incremental target: save-to-refreshed-diagram **< 2s** for single-file C# changes (Roslyn workspace kept warm in the daemon).

---

## 8. Build order (deliberately backwards from the shell)

**Phase 0 — Extractors only (1–2 weeks of evenings).** Roslyn + Bicep + ScriptDom CLIs → Mermaid files into `docs/diagrams/` → view in Obsidian/VS Code. *No shell, no daemon.* Validates the entire data model with zero UI risk, and is useful on day one.

**Phase 1 — Daemon + Graph Explorer as a plain web page.** Kuzu, watcher, WebSocket, Cytoscape explorer in a browser tab next to Windows Terminal. You now have 80% of the value with 20% of the UI work.

**Phase 2 — MCP server.** Point Claude Code at it. This is likely the moment the tool becomes indispensable — *before any custom shell exists.*

**Phase 3 — The Atlas shell.** WPF host, terminal control, WebView2 panes, docking, layouts. Now it's packaging value that already exists, not betting on it.

**Phase 4 —** OTel/sequence diagrams, DI/endpoint/EF passes, declared-vs-observed deps, Structurizr C4, cross-workspace federation, OSC signaling.

Rationale: the shell is the most fun and the least differentiating piece. The graph + MCP is the moat; if phases 0–2 don't feel indispensable, the shell wouldn't have saved it.

---

## 9. Decisions to reason over (for the spec)

1. **Annotation vs convention budget** — how much will you mandate (`[Aggregate]` attributes, Bicep `metadata service`, DDL naming)? Every mandate raises extraction precision and costs adoption friction. My lean: mandate attributes + Bicep metadata (you already mandate Bicep/DDL, so this is in-character), convention-with-confidence-flags elsewhere.
2. **Snapshot history** — diff-per-change in Kuzu, or lean on git history of generated `docs/diagrams` and keep the graph "current-state only"? (Lean: current-state graph + git for history. Cheaper, and "history" questions are rarer than "impact" questions.)
3. **How much runtime data lives in the graph** — retain spans per named scenario only, or all local traces? (Lean: named scenarios, explicitly captured.)
4. **Structurizr vs D2 for C4** — Structurizr gives a real model + multiple views; D2 is lighter and renders anywhere. (Lean: Mermaid/D2 in phases 0–2, Structurizr when C4 becomes central.)
5. **Terminal control bet** — `Microsoft.Terminal.Wpf` (first-party, thin API) vs EasyWindowsTerminalControl (stream access). Prototype both in a day; the stream tap matters less once the file event bus exists.
6. **Multi-repo write semantics for agents** — should an agent in repo A be able to `record_decision` scoped to repo B? (Lean: yes, workspace-scoped knowledge; provenance records which session wrote it.)
7. **Where the daemon boundary sits** — one daemon per workspace vs one global daemon hosting many workspaces. (Lean: one global daemon, workspace = tenant; simpler MCP registration.)

---

## 10. Non-goals (v1)
- Not an editor. Code editing stays in the agents/VS Code.
- No hand-drawn diagram editing — if a view is wrong, fix the code/annotations or the query, never the picture.
- No cloud/multi-user sync. Local-first; the graph file is committable if sharing is ever needed.
- No attempt at full semantic extraction for Java/Python/Rust beyond deps + modules + classes.
