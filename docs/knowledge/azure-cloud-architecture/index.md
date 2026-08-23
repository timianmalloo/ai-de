---
id: kb-azure-cloud-architecture
title: "Azure & Cloud Architecture Visualization — domain knowledge"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [azure, bicep, arm, c4, architecture-visualization, iac]
links:
  - { to: knowledge-hub, rel: refines }
  - { to: seed-ai-native-ide-sketch, rel: relates-to }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Evidence base for deriving and rendering Azure architecture views from Bicep/ARM and live
  resource state: what static IaC analysis can and cannot recover, the exact icon-licence and
  tag constraints, and the inventory-is-not-architecture problem that decides whether the view
  is useful.
---

# Azure & Cloud Architecture Visualization — domain knowledge

**Domain & problem:** AI-DE extracts Azure infrastructure from Bicep (via `bicep build` → ARM JSON) into
`AzureResource` nodes and `dependsOn` edges, stitches them to code services via a mandated
`metadata service = '...'` annotation or tag, and renders derived cloud-architecture views at C4
container/deployment level.

**Canonical framing:** The field frames this as two disjoint activities that are routinely confused —
**resource inventory** (what is deployed: Resource Graph, Cloudockit, Hava, live-import diagrammers) and
**architecture** (what was intended: Azure Architecture Center reference architectures, C4 deployment
diagrams). Our framing — *derive architecture from declared IaC* — sits between them and inherits the
hazards of both. Divergence from the canonical framing: most tools read **live subscriptions**; we read
**repository artifacts**, which trades currency for reviewability and loses everything parameter-resolved.

**Compiled:** 2026-08-23 · **Lead:** Domain Researcher · **Status:** fresh

*(The template's `data-and-constants.md` is folded into `references.md` §"Versions, constants and limits"
here — this domain's constants are licence terms and service limits, which belong beside their source.)*

## Headline findings

1. **The Azure icon licence is narrow and load-bearing.** Microsoft permits the icons "in architectural
   diagrams, training materials, or documentation" and "reserves all other rights"; bundling them into a
   product that generates and serves diagrams is not obviously inside that grant. SVG only; no Visio
   stencils, and none planned. — *(Verified, [S4])*
2. **There is no first-party Azure auto-diagramming product.** The Bicep Visualizer (single file, no
   export, no cross-module) and Application Insights Application Map (runtime only, instrumented
   components only) are the closest, and neither produces an architecture diagram. — *(Verified, [S14][S20])*
3. **`existing` resources are invisible to static analysis.** A `resource x '...' existing = {…}` is not
   deployed and its scope may be a runtime value, so a Bicep-only extractor cannot know what it refers to.
   This is a structural hole in artifact-only extraction, not a bug. — *(Verified, [S12])*
4. **Loops and conditionals destroy static node identity.** `[for item in collection: …]` maps one symbolic
   name to N deploy-time resources, and `if (condition)` resources may not exist at all. Without parameter
   values, an extractor must either enumerate literal collections or represent iteration symbolically. — *(Verified, [S11])*
5. **Implicit `dependsOn` is recoverable; cross-module dependency is not, cheaply.** Bicep materialises any
   symbolic reference as `dependsOn` in the compiled ARM JSON — so compiling first is strictly better than
   parsing Bicep. But module-to-module edges only exist as param/output chains that must be traced. — *(Verified, [S10][S11]; cross-module difficulty Inferred)*
6. **Resource IDs are the natural stable identifier** — globally unique, hierarchical, and already the
   ARM-canonical path — but they are only fully formed for deployed resources; a pre-deployment graph must
   key on `{module}/{symbolicName}` and reconcile later. — *(ID format Verified [S8][S15]; the reconciliation gap Inferred)*
7. **Tag-based service ownership has hard limits worth designing to:** 50 tags per resource (15 for some
   services), tag names case-**insensitive** and values case-**sensitive**, `< > % & \ ? /` prohibited in
   names, no tags on management groups, and **no automatic inheritance** to child resources. — *(Verified, [S3])*
8. **Backstage's system model is the closest existing standard to what we would otherwise invent** —
   `Component` (software) `dependsOn` `Resource` (infrastructure), both grouped by `System`. Adopting its
   vocabulary costs nothing and buys interoperability. TOSCA is conceptually complete but has negligible
   Azure adoption; Score is workload-level and cannot model topology. — *(Verified, [S23][S24][S25][S26])*
9. **Azure Resource Graph is a query surface, not a topology surface.** KQL over ARM state, eventually
   consistent, throttled at 15 queries per 5-second window, capped at 10,000 subscriptions, and it returns
   **no dependency edges**. Useful for reconciliation and drift, useless as the primary graph source. — *(Verified, [S5][S7])*
10. **The WAF has exactly five pillars** — Reliability, Security, Cost Optimization, Operational
    Excellence, Performance Efficiency — a fact worth pinning because six-pillar variants circulate. — *(Verified, [S2])*

## Confidence summary

Verified: all ten headline claims and every constant (icon licence text, tag limits, resource-ID formats,
WAF pillars, ARG throttling, the 800-resource Bicep file limit). Inferred: the C4↔Azure concept mapping (no
Microsoft-published mapping exists) and the cross-module extraction difficulty. Flagged: Lucidscale's Azure
depth (URL failed to resolve), Brainboard and Multicloud-diagrams (not directly fetched), Cloudockit and
Hava (marketing content only), and the absence of a first-party diagramming product (an argument from
exhaustive search, which is weaker than a positive citation).

**Load-bearing Flagged claims:** none of the Flagged items gate a decision — they are competitor detail. The
one *Verified* claim that most deserves re-checking before it is relied on is the **icon licence**, because
its consequence is legal rather than technical.

## Design implications

- **Compile, don't parse.** Run `bicep build` and extract from ARM JSON: implicit dependencies are already
  materialised as `dependsOn`, and the JSON has a published schema. Parsing `.bicep` directly re-implements
  the compiler and loses the implicit edges.
- **Accept a partial graph, and mark it partial.** Parameter-conditional topology, `existing` references and
  loop cardinality are genuinely unresolvable from artifacts alone. Emit `unresolved` nodes carrying the
  expression that produced them rather than guessing — the `assume:` discipline applied to extraction.
- **The curation policy is the product, not the extraction.** Decide explicitly which resource types are
  architectural nodes (App Service, Storage, Service Bus), which are properties to fold into a parent (NSG
  rules, diagnostic settings), and which are binding glue (role assignments, private DNS). No authoritative
  Microsoft taxonomy exists, so it is our decision and it must be written down and versioned.
- **Put `metadata service` on the resource, not the file** — or decide, and record, that a module *is* a
  service. A file-level annotation is ambiguous the first time one module deploys resources for two services.
- **Borrow Backstage's vocabulary** (`Component` / `Resource` / `System` / `dependsOn` / `spec.owner`) for
  node and edge names, even if we never emit `catalog-info.yaml`. Free interoperability, zero cost.
- **Do not bundle Azure icons** into the shipped product until the licence question has a real answer;
  render with neutral shapes plus a legend, which C4 explicitly permits.
- **Treat Resource Graph as the reconciliation channel**, not the source: "declared in Bicep but absent in
  Azure" and "present in Azure but in no template" are two genuinely valuable findings, and both need
  paginated, throttle-aware queries.

## How to use this base

Personas and the design skills cite these files as evidence (BoK §III.1). The constants in `references.md`
are the ones to quote rather than recall. Refresh when Azure's icon terms, tag limits or Bicep language
features move; re-run `/collectknowledge` and bump the date.
