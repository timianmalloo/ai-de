---
id: kb-azure-references
title: "Azure Architecture Visualization — references, constants and limits"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [azure, reference, limits, licensing]
links:
  - { to: kb-azure-cloud-architecture, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The authoritative documents plus the exact constants this domain turns on: the Azure icon
  licence quoted verbatim, tag limits, ARM resource-ID formats, Resource Graph throttling, and
  the Bicep static limits.
---

# Reference information

## Standards, frameworks and specifications

- **Azure Well-Architected Framework** — five pillars: Reliability, Security, Cost Optimization,
  Operational Excellence, Performance Efficiency. Reference architectures are structured against them.
  *(Verified, [S2])*
- **Cloud Adoption Framework** — Strategy → Plan → Ready → Adopt → Govern → Manage; the "Ready" phase owns
  landing zones, naming abbreviations and the tagging strategy. *(Verified, [S18][S19])*
- **ARM template JSON schema** — `$schema` required; resource-group deployments use
  `https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#`. Top-level sections:
  `$schema`, `languageVersion`, `contentVersion`, `apiProfile`, `definitions`, `parameters`, `variables`,
  `functions`, `resources`, `outputs`. Language version 2.0 adds `definitions`. *(Verified, [S9])*
- **C4 model** — Context / Container / Component / Code plus supporting Deployment diagrams; notation- and
  tooling-independent. *(Verified, [S21][S22])*
- **TOSCA Simple Profile YAML v1.3** — OASIS Standard, RF on Limited Terms. *(Verified, [S25])*
- **Backstage software catalog** — `apiVersion: backstage.io/v1alpha1`, `kind: Component|API|Resource|System|Domain|…`,
  `metadata.name`, `spec.owner`, `spec.system`, `spec.dependsOn`. *(Verified, [S23][S24])*

## Versions, constants and limits

### Azure icon set — licence terms, quoted verbatim (load-bearing)

> "Microsoft permits the use of these icons in architectural diagrams, training materials, or
> documentation. You can copy, distribute, and display the icons only for the permitted use unless granted
> explicit permission by Microsoft. Microsoft reserves all other rights."

Restrictions, quoted: *"Don't crop, flip, or rotate icons. Don't distort or change icon shape in any way.
Don't use Microsoft product icons to represent your product or service."*

Current version **V24** (July 2026 changelog); download
`https://arch-center.azureedge.net/icons/Azure_Public_Service_Icons_V24.zip`; **SVG only**. Microsoft states:
*"The icons aren't provided in Visio stencil format … There are no plans to provide these icons as Visio
stencils."* Microsoft 365 and Power Platform icon sets are published separately. *(Verified, [S4])*

### Resource tag limits

| Constraint | Value |
|---|---|
| Max tags per resource / resource group / subscription | **50** name-value pairs |
| Max tags — Automation, CDN, public & private DNS (zone + A records), Log Analytics saved searches | **15** |
| Tag name max length (general) | **512** characters |
| Tag name max length (storage accounts) | **128** characters |
| Tag value max length | **256** characters |
| Tag name case sensitivity | case-**insensitive** |
| Tag value case sensitivity | case-**sensitive** |
| Prohibited characters in tag names | `< > % & \ ? /` |
| Tags on management groups | **not supported** |
| Inheritance parent → child | **not automatic** (requires Azure Policy) |

*(Verified, [S3])*

### ARM resource ID formats

```
# subscription-scoped
/subscriptions/{subscriptionId}/resourceGroups/{rg}/providers/{ns}/{type}/{name}

# child resource
/subscriptions/{subscriptionId}/resourceGroups/{rg}/providers/{ns}/{parentType}/{parentName}/{childType}/{childName}

# extension resource
{baseResourceId}/providers/{extensionNs}/{extensionType}/{extensionName}

# tenant-scoped
/providers/{ns}/{type}/{name}
```

Globally unique and stable within a tenant; case-insensitive in practice, with creation-time casing
preserved. Resource type naming is `{ProviderNamespace}/{ResourceType}`, e.g. `Microsoft.Compute/virtualMachines`;
child types extend the path, e.g. `Microsoft.Storage/storageAccounts/fileServices/shares`. Full type-name
segments minus one equals the number of name segments. *(Verified, [S8][S15][S16])*

### Azure Resource Graph limits

- Default quota **15 queries per 5-second window per user**; headers `x-ms-user-quota-remaining` and
  `x-ms-user-quota-resets-after` (`hh:mm:ss`).
- Max **10,000 subscriptions** per query; beyond that `x-ms-tenant-subscription-limit-hit: true`.
- Recommended grouping **< 300** subscriptions per query.
- Results paginate at **1000** per page — unpaginated queries silently return an incomplete graph.
- Uses the latest non-preview API of each provider; **eventually consistent**.
- `ResourceChanges` retains **14 days** of history.

*(Verified, [S5][S6][S7])*

### Bicep static facts

- **800 resources** maximum per Bicep file. *(Verified, [S11])*
- File grammar: `metadata <name> = ANY`, `targetScope`, `param`, `var`, `resource … = {…}`,
  `resource … = if (cond) {…}`, `resource … = [for x in xs: {…}]`, `module`, `output`. *(Verified, [S10])*
- `metadata service = '...'` is valid Bicep at file level; resource-level `@description` decorators exist but
  arbitrary typed resource decorators do not — this constrains where a service annotation can live.
  *(Grammar Verified [S10]; the constraint on resource-level annotation is Inferred)*

### CAF resource abbreviations (sample)

| Resource | Provider namespace | Abbreviation |
|---|---|---|
| AI Search | `Microsoft.Search/searchServices` | `srch` |
| Azure OpenAI | `Microsoft.CognitiveServices/accounts` (kind OpenAI) | `oai` |
| ML workspace | `Microsoft.MachineLearningServices/workspaces` | `mlw` |
| App Service plan | `Microsoft.Web/serverFarms` | `asp` |
| Event Hubs namespace | `Microsoft.EventHub/namespaces` | `evhns` |
| Data Factory | `Microsoft.DataFactory/factories` | `adf` |

Full table at [S18] (page last updated 2025-05-23, git-updated 2026-07-21). *(Verified, [S18])*

### CAF tagging categories

Functional (app, tier, env, region) · Classification (criticality, confidentiality, SLA) · Accounting
(department, costcenter, program) · Purpose (businessprocess, businessimpact) · Ownership (businessunit,
operations team). Service ownership is expressed by tag, not by an Azure-native ownership object.
*(Verified, [S19])*

## Foundational works

- **Simon Brown, the C4 model** — the deployment-diagram definitions quoted in `state-of-the-art.md` are the
  source of the deployment-node / infrastructure-node / container-instance distinction that makes curation
  tractable. *(Verified, [S21][S22])*
