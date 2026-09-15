---
id: spike-atlas-architecture-contract
title: "Atlas E2 explicit carrier and resource-identity contract spike"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [atlas, spike, domain, azure]
links:
  - { to: design-atlas-architecture-views, rel: relates-to }
review-by: 2026-12-15
summary: "Synthetic JSON carrier checks pass; fully known deployment identity remains unresolved."
---

# E2 contract spike result

## Authority and scope

Owner F1/F2 decision `57a09ed6` and Core request
`req-01M2KBCP9A8893CWVDDGKKTBYS` (Ruling 121) authorize only this standalone
framework-only net10 console spike and evidence. No solution, package, production, cloud,
analyzed-code execution, desktop, or source-access-policy change. Grant is branch-local and
does not admit production `ReadAsync` or a native surface.

## Observed Windows command and output

Run from repository root on 2026-09-15:

```text
dotnet run --project spikes/atlas-architecture-contract/AtlasArchitectureContractSpike.csproj
PASS duplicate-id: duplicate-id,aggregate-member,layer-member
PASS unknown-role: unknown-role,aggregate-root
PASS invalid-shape: shape:concepts
PASS invalid-label-shape: field:label
PASS anchor-authority-injection: unknown-field:accepted
PASS missing-aggregate-root: aggregate-root,aggregate-root-membership
PASS missing-invariant: aggregate-invariant
PASS root-outside-members: aggregate-root-membership
PASS untyped-membership: layer-member
PASS unknown-dimension: layer-dimension
PASS unknown-state: layer-state
PASS hostile-acceptance-marker: unknown-field:accepted
PASS alias-not-declaration: alias-root
PASS stale-anchor: unresolved=a
PASS missing-target: unresolved=a
PASS out-of-scope: unresolved=a
PASS explicit-domain-layer-positive: entity+value-object+aggregate; declared invariant; logical/current and deployment/target
PASS authority-not-promoted: declared semantics; authority unestablished; enforcement not proven
PASS two-aliases-one-declaration: one referenced root; two retained alias anchors
PASS equal-symbols-distinct-scopes: two declaration roots; same symbol
PASS literal-source-subset: literal type/name read as data; targetScope supplies scope KIND only
PASS partial-identity-refused: identical literal type/name cannot merge without deployment scope
PASS expression-name-unresolved: expression not evaluated
UNRESOLVED fully-known-deployment-positive: admitted Bicep subset supplies no actual subscription/resource-group identity; no invented tuple injected.
PASS 23 contract checks; six fixture groups observed; full deployment equality remains UNRESOLVED (not US-E8 acceptance).
```

Exit code 0. Compilation used the repository SDK pin `10.0.303`, inherited warnings-as-errors,
and no additional dependency. The initial run had 20 passing checks; a direct scope review
added nested acceptance-marker, invalid-label-shape and root-membership checks, yielding 23.
Negative inputs run before the positive fixture and assert their specific rejection/unresolved
diagnostics. A thrown exception is a spike failure, not a passing negative case.

## Six-group disposition

| Group | Observed result | Limit |
|---|---|---|
| Explicit role/layer positive | Entity, value object, aggregate with entity root/member/invariant references, logical/current and deployment/target accepted | Explicit declarations, not demonstrated domain correctness or enforcement |
| Duplicate/role/shape | Named rejection diagnostics | Bounded experimental carrier, not a production JSON parser/schema |
| Source anchors | Stale hash, missing target, foreign scope remain unresolved | Synthetic binding registry only; not Atlas authorization |
| Hostile acceptance | Top-level and anchor acceptance markers rejected; authority remains unestablished | No authority resolver or accepted-spec promotion is implemented |
| Resource aliases | Two aliases reference one exact declaration and retain anchor; same symbol in two scopes stays distinct | RootRef cannot prove two declarations are the same deployed resource |
| Full vs partial identity | Literal Bicep type/name extracted as data; same partial identity and expression name cannot merge | **Positive fully known deployment identity is unresolved** |

## Carrier and binding contract actually exercised

`architecture.fixture.json` is synthetic. Version 1 contains concepts, invariants, layers,
anchors, resources and aliases. IDs are document-local, unique across arrays; references use
IDs rather than display labels. Unknown fields are rejected in the document and rows;
arrays cap at 32 rows, ordinary required string fields at 256 characters. Aggregate root must
be an entity included among its explicit members, and at least one invariant must resolve.
Declared state and dimension are closed sets. The spike does not prove enforcement.

The synthetic admitted identity is `synthetic:abc`, scope `fixture`, content exactly `abc`,
span 0+3 and its actual SHA256. Sharing that anchor between claims deliberately tests carrier
binding, not source-declaration classification. The experiment does not use or mint Atlas
scope tokens, inspect live files, validate real declaration locations, or authorize access.
Missing/stale anchors are unresolved and cannot establish trusted evidence. A production
carrier still needs byte limits before JSON parsing, complete schema validation, real
manifest-bound anchor resolution, cancellation and all hostile-input tests.

## Source-as-data resource evidence and the failed positive oracle

The embedded minimal Bicep literal case is:

```bicep
targetScope = 'resourceGroup'
resource store 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'example'
}
```

An alternate case uses `name: nameParameter`. The experiment reads only the fixed complete
literal declaration/type and literal name, without compiling or evaluating Bicep. It does not
claim a general Bicep parser: comments, nested declarations, modules, interpolation, loops,
multiple declarations and malformed forms need separate supported-producer qualification.

`resourceGroup` is a scope **kind**, not an actual subscription/resource-group identity.
Neither literal resource name nor source-file scope supplies that missing value. Parameter
defaults cannot certify deployment inputs. Therefore two identical literal cases still have
unknown deployment scope and cannot compare equal as deployed identities. No hand-entered
scope/type/name tuple was supplied just to make equality pass.

**Exact open question for Owner:** which admitted source-bound deployment-context producer
supplies actual deployment scope and full parent/name chain without execution or parameter
guessing? The source subset tested here cannot. The positive cross-declaration equality oracle
is not discharged, and US-E8.b remains open. Exact representation comparison is the proposed
rule; no case folding, name folding or ARM-ID normalization is qualified.

## Remaining proof and containment

Windows console compilation/execution only. Linux project coverage and common before/after
gate wall time belong to Conductor, still pending here. No full test suite or native proof ran.
No production parser, authorization proof, user-facing domain/Layer/Azure view, or full-story
acceptance is implied by exit 0. Independent review follows; author clears no veto.

The bounded spike files are the only new implementation artifacts. No generated bin/obj
content is evidence to commit. The csproj remains outside `AiDe.sln` and is still subject to
the repository project-coverage gate on both operating systems.
