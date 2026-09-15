---
id: proof-atlas-architecture-contract
title: "Atlas E2 carrier and resource identity spike evidence"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [atlas, proof, spike, domain, azure]
links:
  - { to: design-atlas-architecture-views, rel: relates-to }
review-by: 2026-12-15
summary: "23 Windows synthetic contract checks; deployment equality positive remains unresolved."
---

# Bounded E2 contract evidence

Run: `dotnet run --project spikes/atlas-architecture-contract/AtlasArchitectureContractSpike.csproj`
on Windows, 2026-09-15. Observed exit 0, ending:

```text
UNRESOLVED fully-known-deployment-positive: admitted Bicep subset supplies no actual subscription/resource-group identity; no invented tuple injected.
PASS 23 contract checks; six fixture groups observed; full deployment equality remains UNRESOLVED (not US-E8 acceptance).
```

The complete command output, specific negative cases first, fixture description and limitations
are committed in [RESULT.md](../../spikes/atlas-architecture-contract/RESULT.md).
Positive JSON fixture: [architecture.fixture.json](../../spikes/atlas-architecture-contract/architecture.fixture.json).
Executable assertions: [Program.cs](../../spikes/atlas-architecture-contract/Program.cs).

Observed negative oracles: duplicate IDs, unknown role, invalid array/label shape, missing or
incorrect aggregate root/member/invariant, invalid layer membership/state/dimension, hostile
acceptance marker, alias-to-alias misuse, stale/missing/foreign source binding, partial deployment
identity, and expression name. Positive declaration roles/layers and exact declaration aliases
pass. All are synthetic contract checks, not real Atlas authorization or native product tests.

**Open:** positive full deployment identity cannot be derived from the tested literal Bicep
subset; actual deployment-scope context is absent. No invented tuple was used to manufacture
that positive. No comparison, grants, runtime-use or deployed-inventory claim is admitted.
Linux/project-coverage before/after timings and independent review remain with Conductor.

Authority: Owner F1/F2 `57a09ed6`; exact-path Core grant request
`req-01M2KBCP9A8893CWVDDGKKTBYS`, Ruling 121. The spike does not admit implementation.
