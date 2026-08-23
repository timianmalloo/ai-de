---
id: kb-azure-sources
title: "Azure Architecture Visualization — sources"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [azure, sources, citations]
links:
  - { to: kb-azure-cloud-architecture, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The full access-dated source list behind the Azure architecture-visualization knowledge base,
  keyed [S1]..[S33] as cited throughout the topic.
---

# Sources

All accessed **2026-08-23**. Citation keys `[Sn]` are used throughout this topic.

| # | Title | Type | URL | Used for |
|---|---|---|---|---|
| S1 | Azure Architecture Center | primary (vendor docs) | https://learn.microsoft.com/en-us/azure/architecture/ | AAC catalogue, reference-architecture format, visual conventions |
| S2 | Azure Well-Architected Framework — Pillars | primary | https://learn.microsoft.com/en-us/azure/well-architected/pillars | The five-pillar list |
| S3 | Azure Resource Manager — Tag resources | primary | https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/tag-resources | Exact tag limits, case rules, prohibited characters |
| S4 | Azure architecture icons | primary | https://learn.microsoft.com/en-us/azure/architecture/icons/ | Icon licence quoted verbatim, V24, SVG-only, no Visio stencils |
| S5 | Azure Resource Graph — Overview | primary | https://learn.microsoft.com/en-us/azure/governance/resource-graph/overview | Capabilities, eventual consistency, tables |
| S6 | Azure Resource Graph — Query language | primary | https://learn.microsoft.com/en-us/azure/governance/resource-graph/concepts/query-language | KQL tables, joins, scope |
| S7 | Azure Resource Graph — Throttled requests guidance | primary | https://learn.microsoft.com/en-us/azure/governance/resource-graph/concepts/guidance-for-throttled-requests | 15 q/5 s quota, headers, 10 000-subscription cap |
| S8 | Resource providers and types | primary | https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/resource-providers-and-types | Type naming scheme, child-type path rules |
| S9 | ARM template syntax | primary | https://learn.microsoft.com/en-us/azure/azure-resource-manager/templates/syntax | `$schema`, top-level sections, language version 2.0 |
| S10 | Bicep file structure and syntax | primary | https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/file | Grammar, `metadata`, `targetScope` |
| S11 | Bicep — Declare resources | primary | https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/resource-declaration | `for`, `if`, `@batchSize`, implicit `dependsOn`, 800-resource limit |
| S12 | Bicep — Existing resources | primary | https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/existing-resource | `existing` semantics and its static-analysis blind spot |
| S13 | Bicep — Child resource name and type | primary | https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/child-resource-name-type | Parent/child modelling |
| S14 | Bicep — VS Code extension (Visualizer) | primary | https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/visual-studio-code | Visualizer capability and limits |
| S15 | Bicep resource functions | primary | https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/bicep-functions-resource | `resourceId` / `extensionResourceId` formats |
| S16 | Azure Resource Manager — Overview | primary | https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/overview | Scope hierarchy, terminology |
| S17 | Azure resource naming rules | primary | https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules | Name constraints per resource type |
| S18 | CAF — Resource abbreviations | primary | https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations | Abbreviation table (page updated 2025-05-23) |
| S19 | CAF — Resource tagging strategy | primary | https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-tagging | Tagging categories, ownership by tag |
| S20 | Application Insights — Application Map | primary | https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-map | Runtime topology derivation and its limits |
| S21 | C4 model | primary (author's site) | https://c4model.com/ | The four levels, notation independence |
| S22 | C4 — Deployment diagram | primary | https://c4model.com/diagrams/deployment | Deployment/infrastructure node definitions, icon guidance (quoted) |
| S23 | Backstage — Descriptor format | primary | https://backstage.io/docs/features/software-catalog/descriptor-format/ | `catalog-info.yaml` schema |
| S24 | Backstage — System model | primary | https://backstage.io/docs/features/software-catalog/system-model/ | `Component`/`Resource`/`System` definitions (quoted) |
| S25 | TOSCA Simple Profile YAML v1.3 | standard (OASIS) | https://docs.oasis-open.org/tosca/TOSCA-Simple-Profile-YAML/v1.3/ | Topology model, IPR terms |
| S26 | Score documentation | primary (CNCF Sandbox) | https://docs.score.dev/docs/ | Workload-level scope, delegation to platform |
| S27 | mingrammer/diagrams | primary (repo, MIT) | https://github.com/mingrammer/diagrams | Azure node set, explicit "not IaC" positioning |
| S28 | InfraMap (cycloidio) | primary (repo, Apache 2.0) | https://github.com/cycloidio/inframap | Relevance-pruning argument vs `terraform graph` |
| S29 | Rover (im2nguyen) | primary (repo, MIT) | https://github.com/im2nguyen/rover | Interactive Terraform plan explorer |
| S30 | Pluralith CLI | primary (repo) | https://github.com/pluralith/pluralith-cli | Alpha status, API-key requirement, cost overlay |
| S31 | Cloudcraft | secondary (vendor site) | https://www.cloudcraft.co/ | Azure support confirmation, live import, 2D/3D |
| S32 | Hava.io | secondary (vendor site) | https://www.hava.io/ | Azure support claim — marketing content only |
| S33 | Cloudockit | secondary (vendor site) | https://www.cloudockit.com/ | Report-oriented output — limited content retrieved |

## Source-quality notes

- S1–S26 are primary: vendor documentation, the C4 author's own site, the Backstage project docs, the OASIS
  standard. Claims drawn from them are labelled **Verified**.
- S31–S33 are vendor marketing pages. Claims drawn from them are **Inferred** at best; where a page failed to
  resolve (Lucidscale) or was not fetched (Brainboard, Multicloud-diagrams) the claim is **Flagged**.
- The claim "no first-party Azure auto-diagramming product exists" rests on exhaustive search rather than a
  positive citation. That is a weaker form of evidence and is marked as such wherever it is used.
