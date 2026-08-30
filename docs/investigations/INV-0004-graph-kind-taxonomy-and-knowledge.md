---
id: inv-0004-graph-kind-taxonomy-and-knowledge
title: "INV-0004 — Why 'Knowledge' is 0, the bicep-as-knowledge mislabel, and the 'not resolved' disclosures"
type: investigation
status: resolved
owner: "@timianmalloo"
phase: ""
tags: [graph, taxonomy, kind, knowledge, extractor, disclosures, investigation]
links:
  - { to: spec-knowledge-exploration, rel: relates-to }
  - { to: design-knowledge-explorer-mode, rel: relates-to }
review-by: 2027-02-28
summary: >-
  Three related findings from the graph on TheTerrace. (1) The category filter's Knowledge/Specs chips
  are 0 because the app's graph is built from CODE extractors (C#, python, typescript, EF/SQL schema,
  bicep/azure) — the repo's docs/knowledge artifacts (markdown specs, ADRs, designs) are not extracted
  into it. (2) A bicep resource reads "kind: knowledge" in the reader because the system has two 'kind'
  notions — the fine has_type (azure-resource) and a coarse node_kind (source vs knowledge, a
  dimensional Type-2 classification); the reader was showing node_kind. (3) The "not resolved"
  disclosures are the projection's honest analysis boundaries — mostly legitimate, a few extractor
  coverage gaps. Design fixes landed; the node_kind and neighbour-kind items are handed to Core.
---

# INV-0004 — Graph kind taxonomy, "Knowledge = 0", and the disclosures

## 1. Why the **Knowledge** (and Specs) filter chips are 0

**The app's graph is the CODE graph.** It is built by the code/data/infra extractors — `CSharpExtractor`
(class/interface/record/struct/enum/method), `PythonExtractor` (python-module/class/function),
`TypeScriptExtractor`, `EfSchemaExtractor` and `SqlSchemaExtractor` (table/column/schema), and
`BicepExtractor` (azure-resource/module/parameter). **None of these index the repository's
docs/knowledge artifacts** — the markdown specs, ADRs, designs and decision notes under `docs/`. So the
graph contains **no knowledge or spec nodes**, and the Knowledge/Specs chips are correctly 0.

This is a **gap against the spec, not a bug in the graph.** `spec-knowledge-exploration` **US-K1** wants
"one graph over all artifacts — code, knowledge, specs, architecture, ADRs, designs …". The current
extractors realise the *code* half; the *knowledge* half (a docs/markdown extractor, or integrating the
docs graph the pack already maintains via frontmatter/Graphify) is **unbuilt**. Building it is the work
that makes those chips light up. **Owner: Core (a new docs/knowledge extractor).**

## 2. The "kind: knowledge" mislabel on a bicep resource

**Root cause — two different meanings of "kind".** The system carries:
- **`has_type`** — the *fine* type from the extractor: `azure-resource`, `table`, `class`,
  `python-module`, … (`GraphProjection`: "has_type gives the node its kind"). The **overview graph**
  and the **category filter** use this, which is why the counts are right (azure → Infra, table → Data,
  class → Code).
- **`node_kind`** — a *coarse* dimensional Type-2 attribute, **`source` vs `knowledge`**
  (`WorkspaceSchema.cs:56`: "Type-2: source <-> knowledge changes interpretation"). The **reader** was
  showing this (`describe.Node.NodeKind`, `CanvasGraphViewModel:176`), so a bicep resource read
  "knowledge" — its coarse class — instead of `azure-resource`, its type.

**Design fix (landed):** the reader (`NodeReaderView`) now prefers the node's **`has_type`** edge over
the coarse `node_kind` for the displayed type, so the bicep node reads **`azure-resource`**.

**Still to confirm on the Core side (handed off):**
- **Is a bicep resource's `node_kind` legitimately `knowledge`, or is that itself wrong?** In the
  dimensional model `node_kind` distinguishes *extracted source* from *curated knowledge*; a bicep
  resource is extracted from code, so it would be expected to be `source`. If `BicepExtractor` (or the
  projection) is emitting `knowledge` for it, that is a **Core mislabel** to fix at the source, not just
  in the reader. **Owner: Core.**
- **Neighbour nodes are hardcoded `Kind = "source"`** in the describe path
  (`CanvasGraphViewModel:210`), so in a *focused* graph every neighbour loses its real `has_type` and
  the filter would categorise them all as Code. The overview path is correct; the focus path is not.
  Fixing it needs the neighbour's `has_type` carried on the describe result. **Owner: Core.**

## 3. The "… not resolved / not analysed" disclosures — external, or a gap?

These are the projection's **honest disclosures** about the bounds of its analysis (the pack's
provenance discipline — say what you did *not* establish rather than imply completeness). Categorised:

| Disclosure | Category | Verdict |
|---|---|---|
| `python-imports-not-resolved (N imports name something this scope does not contain)` | **External / out-of-scope** | **Fine** where the target is an external package or a module not in the indexed scope. Worth a spot-check only if a *large* count (117, 72) is actually *internal* modules the extractor missed. |
| `python-dynamic-imports-not-analysed`, `python-nested-declarations-not-analysed` | **Extractor coverage gap** | The python extractor does not follow dynamic (`importlib`) or nested imports/declarations. **Addressable** if python coverage matters. |
| `schema-changed-by-raw-sql-not-read` | **Extractor coverage gap** | Schema is read from EF/migrations, not from raw SQL DDL, so a table altered by raw SQL is missed. **Addressable** if the app uses raw SQL DDL. |
| `schema-from-migrations-not-database` | **By design** | The schema is derived from migrations, not a live DB connection — deliberate (no runtime DB dependency). Fine. |
| `bicep-expressions-not-evaluated`, `bicep-resource-count-indeterminate` | **By design (structural)** | Bicep is extracted structurally; runtime expression evaluation and loop counts are not computed. Fine. |
| `build-conditions-not-evaluated` | **By design (structural)** | Conditional compilation is not evaluated; the structure is extracted regardless. Fine. |
| `generated-code-not-analysed` | **By design** | Generated code is derivative of its generator; skipping it avoids double-counting. Fine. |

**Net:** none are bugs — they are the projection being honest about its edges. Most are legitimate
external/structural boundaries; the **python dynamic/nested imports** and **raw-SQL schema** items are
genuine **extractor coverage gaps** worth improving *if* those artifact kinds are load-bearing for
TheTerrace. **Owner: Core (extractor coverage), and it is a priority call, not a defect.**

## Handoffs to Core (recorded in session-contracts §4c)
1. **A docs/knowledge extractor** so knowledge/spec artifacts enter the graph (US-K1) and the
   Knowledge/Specs chips populate.
2. **Fix `node_kind = knowledge` on extracted (source) nodes** at the extractor/projection, or confirm
   the classification is intended.
3. **Carry each neighbour's `has_type`** on the describe result so the focus-path graph and filter
   categorise neighbours by their real type instead of the hardcoded `"source"`.
4. **Python dynamic/nested imports and raw-SQL schema** extractor coverage — a priority call.

## Design fixes landed here
- Category filter split **Infra** (bicep/azure) out of **Data** (database/data-models), matching the
  user's "data = database and data models".
- The reader prefers the specific **`has_type`** over the coarse `node_kind` for the displayed type.
