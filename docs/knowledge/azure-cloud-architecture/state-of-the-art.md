---
id: kb-azure-sota
title: "Azure Architecture Visualization — state of the art"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [azure, bicep, arm, resource-graph, c4]
links:
  - { to: kb-azure-cloud-architecture, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Current practice for deriving Azure architecture views: Microsoft's own guidance and visual
  conventions, what Resource Graph and the Bicep Visualizer actually provide, and the precise
  boundary of what Bicep static analysis can determine.
---

# State of the art — Azure & cloud architecture visualization

## Microsoft's own guidance and visual conventions

**Azure Architecture Center (AAC)** is the canonical reference catalogue: Solution Ideas, Example
Workloads, Reference Architectures, Technology Decision Guides. Reference architectures pair a diagram
with the service list, per-WAF-pillar design considerations, and deployment guidance. Visual convention
is SVG icons arranged in nested rectangular groupings (region → VNet → subnet → resource group) with
directional arrows for data flow. *(Verified, [S1])*

**Well-Architected Framework** — exactly five pillars: Reliability, Security, Cost Optimization,
Operational Excellence, Performance Efficiency. *(Verified, [S2])*

**Cloud Adoption Framework** covers Strategy → Plan → Ready → Adopt → Govern → Manage. The "Ready" phase
carries the two things that matter to an extractor: **landing zones** and the **naming and tagging
conventions**, including the resource-abbreviation table (`asp` for `Microsoft.Web/serverFarms`, `evhns`
for `Microsoft.EventHub/namespaces`). CAF expresses service ownership through *tags* (`app`,
`businessunit`, `operations-team`) — Azure has no first-class ownership object on a resource.
*(Verified, [S18][S19]; "no first-class ownership object" is Inferred from its absence in the docs)*

**The icon set** is published at the AAC, currently V24 (July 2026 changelog), SVG only. Its terms are
quoted in full in `references.md` and are the single most consequential constraint in this domain for a
product that renders diagrams.

## Deriving architecture from declared infrastructure

**Compile-first is settled practice.** `bicep build` emits ARM JSON in which every implicit dependency —
any reference to another resource's symbolic name — has been materialised into an explicit `dependsOn`.
Extracting from the compiled artifact recovers the dependency graph without re-implementing name
resolution. *(Verified, [S10][S11])*

What Bicep static analysis **can** determine: resource types and API versions (always explicit), explicit
and compiler-materialised `dependsOn`, parent/child relationships, module decomposition with declared
params and outputs, file- and resource-level `metadata`, and resource names composed of literals and simple
interpolation. *(Verified, [S10][S11][S13])*

What it **cannot** determine — the honest boundary of the whole approach:

| Construct | Why static analysis loses it |
|---|---|
| `existing` resources | Not deployed by the template; scope may be `resourceGroup(paramValue)` — a runtime value |
| `[for item in collection: …]` | Cardinality unknown unless the collection is a literal |
| `if (condition)` | Resource may or may not exist, depending on parameters |
| Parameter-driven names/SKUs/topology | e.g. `param deployRedis bool` gating a whole subsystem |
| Module inputs and outputs | Module topology is a function of its parameters; output→input chains must be traced |
| Key Vault references | Runtime-resolved at deployment |
| `@batchSize` / serial loops | Ordering is a deployment-time concern |
| Cross-scope deployment | `scope: subscription()` / `managementGroup()` makes the graph multi-scope |

*(Verified, [S10][S11][S12])* Documented hard limit: **800 resources per Bicep file**. *(Verified, [S11])*

**Azure Resource Graph** is the live-state counterpart: KQL over ARM, primary table `Resources`, plus
`ResourceContainers` and domain tables, with `ResourceChanges` giving 14 days of history. It is
**eventually consistent**, throttled at **15 queries per 5-second window per user**
(`x-ms-user-quota-remaining`, `x-ms-user-quota-resets-after`), capped at **10,000 subscriptions** per query
with `< 300` recommended per group, and paginates at 1000 results. It returns properties, not edges.
*(Verified, [S5][S6][S7])*

**Bicep Visualizer** (VS Code) renders one file's resources and dependencies as a node-link graph. No
cross-module traversal, no `existing` handling, no service ownership, no documented export format.
*(Verified, [S14])*

**Application Insights Application Map** derives topology at runtime by following HTTP dependency calls
between instrumented components; nodes are identified by telemetry `roleName`. It sees only what emits
telemetry and knows nothing of declared infrastructure. *(Verified, [S20])*

## C4 at deployment level

C4 defines System Context, Container, Component, Code, plus supporting diagrams including **Deployment**,
which is explicitly "based upon a UML deployment diagram". A **deployment node** "represents where an
instance of a software system/container is running … Deployment nodes can be nested." An
**infrastructure node** covers "DNS services, load balancers, firewalls, etc." On icons, C4 says: "Feel
free to use icons provided by Amazon Web Services, Azure, etc … just make sure any icons you use are
included in your diagram key/legend." *(Verified, [S21][S22])*

Mapping Azure onto those concepts — **Inferred**, because no Microsoft-published C4 mapping exists:

| C4 concept | Azure |
|---|---|
| Deployment environment | Subscription + resource group, or an `env` tag |
| Deployment node (nested) | Region → VNet → subnet → App Service Plan / AKS cluster |
| Container instance | App Service instance, Container App, AKS workload |
| Infrastructure node | Load Balancer, Application Gateway, Azure DNS, Key Vault, NSG |
| Software system | The named workload, spanning several Azure resources |

The scoping hierarchy underneath: Tenant → Management Group (nestable to six levels) → Subscription →
Resource Group → Resource → Child resource, plus extension resources (role assignments, locks, tags) that
attach at any scope. Resources cannot span resource groups; resource groups cannot span subscriptions.
*(Verified, [S16])*

## The frontier

- **No standard cloud-topology model has won.** TOSCA v1.3 (OASIS, RF on Limited Terms) is a complete
  node/relationship/lifecycle model with negligible Azure adoption and no ARM/Bicep mapping. Score (CNCF
  Sandbox) is deliberately workload-level and delegates resource resolution to platform teams. OAM/KubeVela
  is Kubernetes-centric. *(Verified [S25][S26]; OAM Flagged — not fetched directly)*
- **Backstage is the de-facto model people actually use**: `Component`, `API`, `Resource`, `System`,
  `Domain`, with `spec.dependsOn` and `spec.owner` in `catalog-info.yaml`. `Resource` is "the infrastructure
  a component needs to operate at runtime"; `System` is "a collection of resources and components that
  exposes one or several public APIs". *(Verified, [S23][S24])*
- **The unsolved problem is curation, not extraction.** InfraMap advertises itself against `terraform graph`
  precisely because it shows "only the resources that are most important/relevant" — an explicit admission
  that a complete dependency graph is not an architecture diagram. Nobody publishes an authoritative
  taxonomy of which Azure resource types are architectural. *(InfraMap claim Verified [S28]; the general point Inferred)*
