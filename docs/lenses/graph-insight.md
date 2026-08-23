---
id: lens-graph-insight
title: "Lens - graph insight (computed)"
type: doc
status: accepted
owner: "@maintainers"
tags: [lens, graph-analysis, computed]
links:
  - { to: lens-graph-structure, rel: relates-to }
review-by: ""
summary: >-
  Computed structural analysis of the knowledge graph - hubs, bridges,
  components, orphans and structural gaps. Regenerate with
  obsidian-setup.py --analyze --write. Derived, never authoritative.
---

# Graph insight - AI-DE

*Computed from `docs/docs-index.js` (generated 2026-08-23T22:42:03Z) by `obsidian-setup.py --analyze`. Dependency-free: no Obsidian or plugin required.*

## Shape

- **76 artifacts**, **90 typed links**, density 0.0305
- **1 connected component(s)**; largest holds 76 artifact(s)

| type | n |
|---|---|
| knowledge | 71 |
| doc | 3 |
| architecture | 1 |
| decision-note | 1 |

| relation | n |
|---|---|
| `refines` | 70 |
| `relates-to` | 18 |
| `documents` | 2 |

## Hubs - the most connected artifacts

*A hub carries the most context. If one is wrong or stale, the error propagates widest.*

| artifact | degree |
|---|---|
| `knowledge-hub` | 14 |
| `seed-ai-native-ide-sketch` | 11 |
| `kb-ai-native-ide-shell` | 9 |
| `kb-azure-cloud-architecture` | 8 |
| `kb-code-and-infra-extraction` | 8 |
| `kb-code-knowledge-graphs` | 8 |
| `kb-diagram-generation` | 8 |
| `kb-domain-modeling-and-erm` | 8 |

## Bridges - highest betweenness

*A bridge is the only path between regions. Losing it fragments the graph; these are the artifacts most worth keeping accurate.*

| artifact | betweenness |
|---|---|
| `knowledge-hub` | 1540.0 |
| `seed-ai-native-ide-sketch` | 938.0 |
| `kb-ai-native-ide-shell` | 429.0 |
| `kb-azure-cloud-architecture` | 429.0 |
| `kb-code-and-infra-extraction` | 429.0 |
| `kb-code-knowledge-graphs` | 429.0 |
| `kb-diagram-generation` | 429.0 |
| `kb-domain-modeling-and-erm` | 429.0 |

## Attention


**Orphans (no links either way)** - 0 *(an orphan is a finding, not a result (V10))*
- none

**Fragments (disconnected from the main graph)** - 0 *(reachable only in isolation)*
- none

**Leaves (single link)** - 62 *(weakly integrated - often correct, sometimes forgotten)*
- `audit-log`
- `kb-azure-comparables`
- `kb-azure-glossary`
- `kb-azure-open-questions`
- `kb-azure-references`
- `kb-azure-sota`
- `kb-azure-sources`
- `kb-codegraph-comparables`
- `kb-codegraph-glossary`
- `kb-codegraph-open-questions`
- `kb-codegraph-references`
- `kb-codegraph-sota`
- `kb-codegraph-sources`
- `kb-coord-comparables`
- `kb-coord-glossary`
- `kb-coord-open-questions`
- `kb-coord-references`
- `kb-coord-sota`
- `kb-coord-sources`
- `kb-diagrams-comparables`
- `kb-diagrams-glossary`
- `kb-diagrams-open-questions`
- `kb-diagrams-references`
- `kb-diagrams-sota`
- `kb-diagrams-sources`
- `kb-domain-comparables`
- `kb-domain-glossary`
- `kb-domain-open-questions`
- `kb-domain-references`
- `kb-domain-sota`
- `kb-domain-sources`
- `kb-extraction-comparables`
- `kb-extraction-glossary`
- `kb-extraction-open-questions`
- `kb-extraction-references`
- `kb-extraction-sota`
- `kb-extraction-sources`
- `kb-mcp-comparables`
- `kb-mcp-glossary`
- `kb-mcp-open-questions`
- `kb-mcp-references`
- `kb-mcp-sota`
- `kb-mcp-sources`
- `kb-micro-comparables`
- `kb-micro-glossary`
- `kb-micro-open-questions`
- `kb-micro-references`
- `kb-micro-sota`
- `kb-micro-sources`
- `kb-shell-comparables`
- `kb-shell-glossary`
- `kb-shell-open-questions`
- `kb-shell-references`
- `kb-shell-sota`
- `kb-shell-sources`
- `kb-uml-comparables`
- `kb-uml-glossary`
- `kb-uml-open-questions`
- `kb-uml-references`
- `kb-uml-sota`
- `kb-uml-sources`
- `note-collectknowledge-session-2026-08-23`

**Unowned** - 0 *(V13 requires an accountable owner)*
- none

**Missing review-by** - 0 *(no freshness SLA)*
- none

**Flagged review-suggested** - 2 *(an upstream change wants a look)*
- `seed-agent-coordination-spec`
- `seed-ai-native-ide-sketch`

## Structural gaps

*Expected relations that are absent. A prompt, not a failure - close the link or record why it does not apply.*

| artifact | type | gap |
|---|---|---|
| `architecture` | architecture | nothing refines or implements the architecture |

## Ownership

| owner | artifacts |
|---|---|
| @timianmalloo | 76 |
