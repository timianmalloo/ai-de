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
summary: "57 Windows synthetic checks and seven rejected faults, including full relation binding corruption."
---

# Bounded E2 contract evidence

## Latest binding proof correction (2026-09-15)

Independent review blocked `63e68e8f` because its relation oracle omitted
Scope/Hash/Start/Length equality. The new `relation-corrupt-binding` subject fault
first passed that old oracle despite visibly corrupt output (exit 0), then failed
the repaired full-record equality oracle at `relation-produced-target-and-current`
(exit 1). A subsequent normal rebuild passed 57 checks. All six prior faults
were rerun and rejected; total subject faults now seven.

[RESULT.md](../../spikes/atlas-architecture-contract/RESULT.md#relation-binding-proof-correction--2026-09-15)
records exact expected binding, commands, observations and limits. Independent
review remains pending; the earlier full-binding claim is superseded, not silently
treated as previously proven. Deployment identity and production authority remain open.

## Latest relation experiment (2026-09-15)

Windows rebuild/run observed exit 0 and 57 checks. Original 28 controls remain;
new relation tests exercise typed endpoints, kind/evidence/assertion rejection,
declaration provenance, emitted relation records and seven-collection bounds.
The missing seventh collection oracle failed before the validator fix (exit 1).
All six explicit subject faults exited 1 after rebuild, including bad admission,
dropped relations and wrong projected endpoint. Named commands, outputs and
limitations are in [RESULT.md](../../spikes/atlas-architecture-contract/RESULT.md#relation-experiment--2026-09-15).

Verified: synthetic output/refusal behavior only. Flagged: independent review and
Conductor Linux/coverage pending; production producer, authorization and full
deployment identity remain unresolved. Design `15b53fe9` is unchanged.
The original repair evidence below is preserved as history.

Repair following independent BLOCK `8eb44853eefd935fb680c3824595b93b07e5b803`:
`dotnet run --project spikes/atlas-architecture-contract/AtlasArchitectureContractSpike.csproj`
on Windows, 2026-09-15. The new blank-required-text oracle first failed against prior behavior.
After repair and explicit rebuild, observed exit 0, ending:

```text
UNRESOLVED fully-known-deployment-positive: admitted Bicep subset supplies no actual subscription/resource-group identity; no invented tuple injected.
PASS 28 contract checks; six fixture groups observed; full deployment equality remains UNRESOLVED (not US-E8 acceptance).
```

The complete command output, specific negative cases first, fixture description and limitations
are committed in [RESULT.md](../../spikes/atlas-architecture-contract/RESULT.md).
The original 23-check transcript remains there as review-blocked history. Current checks observe
produced alias groups and complete retained anchors, isolate scope as the only identity change,
remove the constant authority assertion, and cover whitespace/text/row/kind limits.

Following the rebuild, `dotnet run --no-build --project spikes/atlas-architecture-contract/AtlasArchitectureContractSpike.csproj -- --fault <name>`
returned exit 1 for all three faults: `alias-drop` failed `two-aliases-one-produced-root`;
`alias-wrong-root` failed `different-root-not-collapsed`; `scope-drop` failed
`equal-symbols-distinct-scopes`. RESULT retains exact terminal diagnostics and source changes.
An earlier no-build run against stale output was excluded and all three rerun after rebuild.

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
