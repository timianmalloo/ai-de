---
id: kb-azure-glossary
title: "Azure Architecture Visualization — glossary"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [azure, glossary, ubiquitous-language]
links:
  - { to: kb-azure-cloud-architecture, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The ubiquitous language for Azure architecture extraction — ARM, Bicep, symbolic name versus
  resource name, deployment node versus infrastructure node — defined so code, specs and
  diagrams use one word per concept.
---

# Glossary — ubiquitous language

Use these exact terms in code, specs and generated views.

| Term | Definition |
|---|---|
| **AAC** | Azure Architecture Center — Microsoft's reference-architecture catalogue. *(Verified, [S1])* |
| **ARM** | Azure Resource Manager — the deployment and management layer; also the JSON template format Bicep compiles to. *(Verified, [S16])* |
| **API version** | The per-resource-type version pinned in a declaration (`Microsoft.X/Y@2024-01-01`). Part of the type identity, not of the resource identity. *(Verified, [S10])* |
| **Bicep** | Microsoft's DSL for declaring Azure resources; compiles to ARM JSON. *(Verified, [S10])* |
| **CAF** | Cloud Adoption Framework — end-to-end adoption guidance; source of the naming and tagging conventions. *(Verified, [S18][S19])* |
| **C4 model** | Simon Brown's Context / Container / Component / Code method, plus supporting Deployment diagrams. *(Verified, [S21])* |
| **Container instance** (C4) | A running instance of a container — an App Service instance, Container App, or AKS workload. *(C4 term Verified [S22]; the Azure mapping Inferred)* |
| **`dependsOn`** | ARM's explicit ordering directive. Bicep additionally *materialises* an implicit `dependsOn` for every symbolic reference when it compiles. *(Verified, [S11])* |
| **Deployment node** (C4) | "Where an instance of a software system/container is running" — physical, virtualised, containerised, or an execution environment. Nestable. *(Verified, quoted [S22])* |
| **`existing`** | Bicep keyword marking a resource as pre-existing and *not deployed by this template*; its scope may be runtime-resolved. The primary blind spot of static extraction. *(Verified, [S12])* |
| **Extension resource** | A resource that adds capability to another (role assignment, lock, tag). Attaches at any scope. *(Verified, [S16])* |
| **Implicit dependency** | A dependency created by referencing another resource's symbolic name rather than by writing `dependsOn`. *(Verified, [S11])* |
| **Infrastructure node** (C4) | Supporting infrastructure on a deployment diagram — "DNS services, load balancers, firewalls, etc." *(Verified, quoted [S22])* |
| **Inventory** | A complete enumeration of deployed resources. Distinct from architecture; conflating the two is this domain's signature failure. *(Inferred; argued in `open-questions.md`)* |
| **KQL** | Kusto Query Language — the query language of Resource Graph and Azure Monitor. *(Verified, [S6])* |
| **Landing zone** | A pre-provisioned, governed subscription/environment scaffold from CAF's "Ready" phase. *(Verified, [S18])* |
| **Management group** | A scope above subscription, nestable to six levels. Cannot carry tags. *(Verified, [S3][S16])* |
| **Resource Graph** | The KQL query service over live ARM state; eventually consistent, throttled, edge-free. *(Verified, [S5])* |
| **Resource ID** | The globally unique hierarchical ARM path identifying a resource. The natural stable key — but only for deployed resources. *(Verified, [S8][S15])* |
| **Resource provider** | An Azure REST API namespace supplying resource types, e.g. `Microsoft.Compute`. Registered per subscription. *(Verified, [S8])* |
| **Symbolic name** | The Bicep-file-local identifier for a resource. **Not** the resource's name in Azure, and not stable across refactors — a distinction that matters for pre-deployment node IDs. *(Verified, [S10])* |
| **`targetScope`** | The Bicep declaration of deployment scope: `resourceGroup` \| `subscription` \| `managementGroup` \| `tenant`. *(Verified, [S10])* |
| **TOSCA** | OASIS Topology and Orchestration Specification for Cloud Applications — an open cloud-topology standard with negligible Azure adoption. *(Verified, [S25])* |
| **WAF** | Well-Architected Framework — the five-pillar workload-quality framework. *(Verified, [S2])* |
