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
summary: "57 synthetic checks and six rejected subject faults; relation projection qualified only against synthetic assertions."
---

# E2 contract spike result

## Relation experiment — 2026-09-15

Design `15b53fe9` and independent design-text receipt `d062650d` supply the
contract; programme Owner admitted this experiment under Ruling 121. Existing
28 controls and three original subject faults remain. No production semantics
or design text changed. The new seventh collection is `relations`.

**Red first:** adding `missing-seventh-collection` before changing the validator
produced exit 1: `FAIL InvalidOperationException: missing-seventh-collection:`.
This observes the six-collection implementation accepting a missing relation carrier.

**Green:** `dotnet run --project spikes/atlas-architecture-contract/AtlasArchitectureContractSpike.csproj`
rebuilt and ran on Windows, exit 0, 57 checks. New named observations:

| Controls | Observed result |
|---|---|
| missing-seventh-collection; shape-{concepts,invariants,layers,anchors,resources,aliases,relations} | Named shape refusal; no relation output |
| relation-missing-endpoint / dangling-endpoint / wrong-endpoint-type | endpoint-shape / unresolved endpoint:missing / endpoint-kind; zero edges |
| relation-unsupported-kind | unsupported-kind; zero edges |
| relation-missing-evidence / stale-evidence | unresolved anchor:missing / a; zero edges |
| relation-missing-assertion | unresolved assertion:missing; zero edges |
| relation-mismatched-assertion / mismatched-evidence-binding | assertion-mismatch; zero edges |
| relation-empty-evidence / ref-overflow | relation-refs:anchors; zero edges |
| seven-collection-224-boundary | 7 x 32 rows admitted; 32 produced relations |
| cap-{concepts,invariants,layers,anchors,resources,aliases,relations} | 33 named rows and 225 total refused with both bound diagnostics; zero edges |
| relation-produced-target-and-current | Two produced edges with typed endpoints, kind, basis, state, retained anchor records and assertion references |
| current-declaration-stays-declared | Current explicit declaration retains Declared relationship label and explicit-declaration basis |

The emitted projection contains `declared: concept:order -> concept:money`,
domain-association, target, explicit-declaration, `Declared relationship`; and
`dependency: resource:resource-a -> resource:resource-b`, deployment-dependency,
current, supported-source-assertion, `Synthetic supported depends_on`.
Both retain anchor `a`, target `synthetic:abc`, scope `fixture`, SHA256 of `abc`,
start 0, length 3. Only dependency retains `synthetic-dependency` assertion ref.
These are **produced projection records**, not assertions against fixture input.

After the successful rebuild, each command below used
`dotnet run --no-build --project spikes/atlas-architecture-contract/AtlasArchitectureContractSpike.csproj -- --fault NAME`:

| Subject fault | Exit | Observed rejecting oracle |
|---|---:|---|
| alias-drop | 1 | two-aliases-one-produced-root |
| alias-wrong-root | 1 | different-root-not-collapsed |
| scope-drop | 1 | equal-symbols-distinct-scopes |
| relation-drop | 1 | seven-collection-224-boundary |
| relation-wrong-endpoint | 1 | relation-produced-target-and-current |
| relation-admit-mismatch | 1 | relation-mismatched-assertion: errors= unresolved= edges=2 |

The synthetic registry predicate is fixed `depends_on`, binding the exact typed
resource pair, deployment-dependency kind, current state, and anchor `a` within
the synthetic observation. Missing assertion IDs remain unresolved; mismatching
bindings fail. The carrier cannot register an assertion or upgrade a declaration.

**Limits:** this is a strict bounded experimental subset supporting only
domain-association and deployment-dependency. Other relation kinds, real producer
identity/epoch validation, source authorization, full parser byte/depth limits,
wire/native performance, and complete US-E8 deployment equality are unqualified.
The 32-reference bound is tested for relation anchor refs, not every pre-existing
carrier reference list. Source-as-data still supplies no concrete deployment scope;
no invented tuple was added. Linux and project coverage remain Conductor work.
Independent reviewer must clear this changed spike; author green is not acceptance.
Class/control: the earlier input-readback defect is prevented here by output
oracles plus wrong-endpoint/drop mutations; bad admission has its own bypass fault.

Prior evidence below remains historical and is not rewritten as a first-pass success.

## Authority and scope

Owner F1/F2 decision `57a09ed6` and Core request
`req-01M2KBCP9A8893CWVDDGKKTBYS` (Ruling 121) authorize only this standalone
framework-only net10 console spike and evidence. No solution, package, production, cloud,
analyzed-code execution, desktop, or source-access-policy change. Grant is branch-local and
does not admit production `ReadAsync` or a native surface.

## Current repair evidence — supersedes the original 23-check claim

Independent review `8eb44853eefd935fb680c3824595b93b07e5b803` BLOCKED the original
evidence: alias assertions inspected input, the scope case also changed file, the authority
assertion was constant, and several advertised bounds lacked negative oracles. That history
remains below; the original 23 rows did not independently establish those four claims.

**Observed red first:** after adding the whitespace case before changing validation,
`dotnet run --project spikes/atlas-architecture-contract/AtlasArchitectureContractSpike.csproj`
returned exit 1 with `FAIL InvalidOperationException: blank-required-text:`. Required text now
rejects whitespace. Unknown layer kind, 33 unique rows and 257-character text have separate
named negative checks.

Alias evidence now comes from `ProjectResources`: validation must succeed before any root
group is returned; output contains root groups and complete immutable alias-anchor records.
The two aliases retain distinct anchor IDs `a`/`b` and their target, scope, content hash and span.
Missing root emits no partial groups; moving one alias to the other root keeps the groups
separate. The identity fixture differs **only** in scope, with workspace/file/symbol identical.
The constant authority properties and their counted assertion were deleted; the two actual
hostile-field rejection checks remain. No authority resolver is claimed.

After rebuilding the final source, the same command returned exit 0 and:

```text
PASS duplicate-id: duplicate-id,aggregate-member,layer-member
PASS unknown-role: unknown-role,aggregate-root
PASS invalid-shape: shape:concepts
PASS invalid-label-shape: field:label
PASS blank-required-text: field:label
PASS text-overflow: field:label
PASS row-overflow: bound:concepts
PASS unknown-layer-kind: layer-kind
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
PASS equal-symbols-distinct-scopes: all identity components equal except scope; two produced roots
PASS missing-root-produces-no-groups: alias-root; no partial output
PASS different-root-not-collapsed: two roots each retain their own alias
PASS two-aliases-one-produced-root: produced root retains both complete alias anchor records
PASS literal-source-subset: literal type/name read as data; targetScope supplies scope KIND only
PASS partial-identity-refused: identical literal type/name cannot merge without deployment scope
PASS expression-name-unresolved: expression not evaluated
UNRESOLVED fully-known-deployment-positive: admitted Bicep subset supplies no actual subscription/resource-group identity; no invented tuple injected.
PASS 28 contract checks; six fixture groups observed; full deployment equality remains UNRESOLVED (not US-E8 acceptance).
```

Three explicit fault injections alter the subject, not its assertions. After that rebuild:

| Command suffix after `dotnet run --no-build --project spikes/atlas-architecture-contract/AtlasArchitectureContractSpike.csproj --` | Observed exit | Final diagnostic |
|---|---|---|
| `--fault alias-drop` | 1 | `FAIL InvalidOperationException: two-aliases-one-produced-root: produced root retains both complete alias anchor records` |
| `--fault alias-wrong-root` | 1 | `FAIL InvalidOperationException: different-root-not-collapsed: two roots each retain their own alias` |
| `--fault scope-drop` | 1 | `FAIL InvalidOperationException: equal-symbols-distinct-scopes: all identity components equal except scope; two produced roots` |

Alias-drop truncates the produced aliases; wrong-root assigns aliases to the first group;
scope-drop removes scope from the subject's grouping key. These are experiment-only switches,
not production options. A first fault run accidentally used an earlier binary after test edits;
it is excluded from final-revision evidence. The explicit rebuild and all three repeated fault
runs above correct that stale-artifact mistake. No Linux result from the old source is reused.

Class -> sweep -> derive -> prevent: the class is an oracle asserting its inputs/constants
instead of independently observing the produced result. Swept alias groups, identity dimensions,
authority and advertised bounds. Derived output-sensitive checks, isolated scope, removed the
constant, and added boundary negatives. The three subject faults now fail named oracles. Full
deployment identity remains the same open producer question, not a new scope for this repair.

## Historical initial Windows command and output (review-blocked)

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
