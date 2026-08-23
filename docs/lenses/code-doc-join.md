---
id: lens-code-doc-join
title: "Lens - code/doc join"
type: doc
status: accepted
owner: "@maintainers"
tags: [lens, graphify, code-graph, traceability]
links:
  - { to: lens-graph-structure, rel: relates-to }
review-by: ""
summary: >-
  Derived join between the documentation graph (intent) and the Graphify code
  graph (reality): documentation referencing code that does not exist, and the
  most connected code symbols no artifact governs. A prompt, never a gate.
---

# Lens - code/doc join

> **This is a lens, not a record.** It is *derived* at read time from the docs graph
> (`docs/docs-index.js`, itself derived from artifact frontmatter) and the code graph
> (`graphify-out/graph.json`, derived from source). Both regenerate; neither is edited.
> Findings below are **prompts, not failures** (GK11/OB13) - close the gap, or record
> why it does not apply.

*Code graph: **2127 nodes**, **2521 edges**, 215 source files. Docs graph: **76 artifacts**, 32 distinct paths referenced.*

## Edge provenance (GK6)

*A citation is not a promotion: an `INFERRED` edge quoted as established is the Confident Guess with extra steps.*

| graphify tag | pack label | edges |
|---|---|---|
| `EXTRACTED` | **Verified** | 2490 |
| `INFERRED` | **Inferred** | 31 |
| `AMBIGUOUS` | **Flagged** | 0 |

## Gap 1 - documentation with no implementation

*An artifact references a code path that exists neither on disk nor in the code graph. Either it was never written, or it moved and the document now lies.*

| artifact | referenced path |
|---|---|
| `kb-mcp-agent-integration` | `.vscode/mcp.json` |
| `kb-mcp-comparables` | `.claude/settings.json` |
| `kb-mcp-comparables` | `.claude/settings.local.json` |
| `kb-mcp-comparables` | `.cursor/mcp.json` |
| `kb-mcp-comparables` | `.vscode/mcp.json` |
| `kb-mcp-references` | `.claude/settings.json` |
| `kb-mcp-references` | `.claude/settings.local.json` |
| `kb-mcp-references` | `.github/workflows/copilot-setup-steps.yml` |
| `kb-mcp-references` | `.vscode/mcp.json` |
| `kb-mcp-references` | `schema/2026-07-28/schema.ts` |
| `kb-mcp-sota` | `.claude/settings.json` |
| `kb-mcp-sota` | `.claude/settings.local.json` |
| `kb-mcp-sota` | `.github/workflows/copilot-setup-steps.yml` |
| `kb-mcp-sota` | `.vscode/mcp.json` |
| `kb-mcp-sources` | `.vscode/mcp.json` |
| `seed-agent-coordination-spec` | `.agents/state.db` |
| `seed-agent-coordination-spec` | `src/Foo.cs` |
| `seed-agent-coordination-spec` | `tools/agentctl.mcp` |
| `seed-ai-native-ide-sketch` | `.atlas/repo.yaml` |
| `seed-ai-native-ide-sketch` | `src/Domain/Order.cs` |

## Gap 2 - risk with no governance

*The most connected code symbols that **no** documentation artifact references. Change here carries the most blast radius and has the least written intent behind it (GK10). Run `graphify affected "<symbol>"` before touching one.*

| symbol | degree | file:line | community |
|---|---|---|---|
| `docs-graph.py` | 66 | `docs/ai-forward-pack/scripts/docs-graph.py:L1` | docs-graph.py |
| `coord-core.py` | 49 | `docs/ai-forward-pack/scripts/coord-core.py:L1` | coord-core.py |
| `audit-log.py` | 37 | `docs/ai-forward-pack/scripts/audit-log.py:L1` | audit-log.py |
| `prompt-log.py` | 28 | `docs/ai-forward-pack/scripts/prompt-log.py:L1` | prompt-log.py |
| `docs-explorer-core.js` | 27 | `docs/ai-forward-pack/scripts/docs-explorer-core.js:L1` | docs-explorer-core.js |
| `dream.py` | 24 | `docs/ai-forward-pack/scripts/dream.py:L1` | dream.py |
| `main()` | 23 | `docs/ai-forward-pack/scripts/coord-core.py:L1331` | coord-core.py |
| `cmd_derive()` | 21 | `docs/ai-forward-pack/scripts/docs-graph.py:L1016` | docs-graph.py |
| `apply-learnings.py` | 19 | `docs/ai-forward-pack/scripts/apply-learnings.py:L1` | apply-learnings.py |
| `obsidian-setup.py` | 18 | `docs/ai-forward-pack/scripts/obsidian-setup.py:L1` | obsidian-setup.py |
| `DocsGraphError` | 16 | `docs/ai-forward-pack/scripts/docs-graph.py:L96` | docs-graph.py |
| `scan()` | 15 | `docs/ai-forward-pack/scripts/docs-graph.py:L714` | docs-graph.py |
| `agent-coordination-explorer.jsx` | 15 | `docs/knowledge/seed-material/agent-coordination-explorer.jsx:L1` | agent-coordination-explorer.jsx |
| `cmd_context()` | 14 | `docs/ai-forward-pack/scripts/docs-graph.py:L1443` | docs-graph.py |
| `graphify-setup.py` | 14 | `docs/ai-forward-pack/scripts/graphify-setup.py:L1` | graphify-setup.py |

## How to act on this

1. **Gap 1** - fix the reference, or delete the claim. A path that does not resolve is a documentation defect, and the pack's own rule is that a stale record is a defect rather than debt (`end-to-end-integrity.md` E17).
2. **Gap 2** - either write the governing design, or record why the symbol needs none. A god node with no design is where the next expensive surprise comes from.
3. **Recurring shapes** - register them as defect *classes* with a control, not as one-off fixes (`continuous-improvement.md` CI1-CI6).

*Code graph built at commit `75198484e7e0e4a1b7c621289ad564a14a60d31b`. Rebuild with `graphify-setup.py --build` after material code change.*
