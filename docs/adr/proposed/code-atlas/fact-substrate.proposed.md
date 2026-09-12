---
id: adr-01M2BBCCCDC207J5KQW74WX2M2
title: "PROPOSED — one SQLite fact substrate and evidence-led schema evolution"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "atlas-proposed-architecture"
tags: [code-atlas, proposed, persistence, dimensional, replay, migration]
links:
  - { to: architecture-code-atlas-proposed, rel: refines }
  - { to: architecture, rel: depends-on }
  - { to: adr-01M2BBCCAYMNMM1MFH0653Z0XF, rel: depends-on }
  - { to: proof-code-atlas-contract-grounding, rel: depends-on }
review-by: 2026-12-12
summary: >-
  Reuses existing generic evidence facts and dimensions for Atlas observations and seals.
  New physical schema requires measured need, additive compatibility, replay and tested rollback.
---

# PROPOSED: reuse the fact substrate before adding a schema

- **Date / author:** 2026-09-12 / `atlas-architecture-astra`.
- **Deciders:** separate Owner/Data/Distributed/Test review pending.
- **Native ID provenance:** Conductor-supplied native allocation after 37-ADR/no-duplicate check;
  registration pending. Not an accepted numbered global ADR.
- **Status:** PROPOSED; no migration or writer edit authorized.

## Established local contract

Direct reading of `src/AiDe.Core/Store/WorkspaceSchema.cs:1–125` at `e4229845` establishes:
schema v1; workspace/node/session dimensions; sequence-based version intervals; generic
`evidence_assertion_fact`; desired scope-generation and committed-snapshot facts.
Natural assertion uniqueness is `(scope_id, artifact_revision, subject, predicate, object,
extractor_id)`, not `(scope,generation,...)`. K0 reports a separate executed 40-test batch
covering Store immutability/compaction and IPC/boundary contracts. New Atlas replay is not tested.

Canonical architecture already chooses dimensional operational storage. Continue that deviation
from an analytics-only star-schema convention for the audit/history requirement; do not call this
an ODS/current-value store or create an independent analytical source of truth.

## Proposed decision

Encode Atlas logical observation records as versioned generic predicates and immutable
observation subjects in the existing store. Reuse node dimensions for stable entities.
Use existing writer/fencing/transaction conventions. No second graph engine or bespoke event
store; no mandatory new table merely because the conceptual model has a new name.

| Record | Grain and invariant |
|---|---|
| Inventory membership | One file/path observation under one enumeration/policy root. Same physical identity shared across project memberships. |
| Declaration/content binding | One syntax occurrence of a logical symbol under exact content/decoder/producer versions. |
| Manifest entry/seal | One selected observation reference; one terminal manifest seal covers required entry count/digest and completion. |
| Later relationship/clause/resource | One attributable typed assertion at one immutable input version. |
| Later assessment/review/run | One pinned assessment or authority/review event or invocation disposition; origin immutable. |

Canonical typed predicate/object values define the logical schema and are versioned.
An observation is consumable only after its fields validate and its seal/commit exists.
Observation identity/revision distinguishes repeated observations across generations under the
actual natural key. Retry of identical observation is idempotent; different payload under the same
operation ID is a conflict. No `INSERT OR REPLACE`/fact update to suppress collisions.

Semantic kinds, resource classifications, authority policy and root changes that alter past meaning
are Type-2 or new immutable observations. Cosmetic dimension labels can retain existing Type-1
behavior, but historical signatures/labels are projected from immutable observations. No historical
view joins to today's cosmetic label and pretends it was yesterday's signature.
Existing generic assertions do not automatically enforce every proposed dimension-version reference;
design must prove producer/reader interval and version consistency, not assert a nonexistent FK.

One aggregate per transaction; scope observations and manifest seals reference other aggregates
by identity. Staged unsealed entries cannot influence complete projections.
The one Core writer remains the authority; read-only clients never bypass it.

## Measures, caches and retention

Inventory bytes/file counts and declaration totals are **semi-additive across observation time**;
sum only disjoint members in one manifest. Symbol and declaration counts differ for partials.
Coverage ratios, confidence/rates, hashes and spans are **non-additive**. Recompute ratios from
their declared denominator; unknown or policy-withheld denominator stays unknown.
Invocation bytes/tokens/spend/counts are additive by unique invocation; latency percentiles are not.

Current manifest selection and graph/list layouts are derived, not new facts to synchronize.
`claim_current_cache` and any Atlas projection index are named caches with versioned input keys.
Drop and replay from retained committed facts must reproduce canonical results including bounds,
origin, authority and gaps. Rebuildability excludes source bytes not retained by the source policy.

Append-only facts are not a license for indefinite sensitive retention. E-0 retains minimal
metadata within existing workspace policy; queryable historical range must be explicit.
Compaction/purge remains an authorized maintenance contract. If historical facts are no longer
retained, comparison/Back reports unavailable rather than rebuilding from current state.
Raw source/model/session text is not placed in a generic object field to evade privacy controls.

## Alternatives and evolution gate

| Alternative | Disposition |
|---|---|
| Dedicated Atlas graph DB | Rejected: new authority, migration/synchronization/security surface without measured need. |
| One giant mutable workspace JSON | Rejected: hides grain, rewrites history, unbounded writes and false aggregate boundary. |
| Dedicated physical fact tables now | Defer. May be right if generic facts fail measured query/integrity requirements, but no speculative migration. |
| Generic observations + seal | Recommended first, with canonical schema validation and replay/plan proof. |

If generic representation fails a demonstrated invariant/performance requirement:

1. Execute a bounded real-store spike and obtain Data/Owner choice.
2. Expand with compatible additive tables/indexes and explicit schema/capability version.
3. Backfill only deterministic evidence; missing historical binding becomes unknown.
4. Compare/replay both read paths, including history and refusal states.
5. Roll back by disabling new capability/read path and reopening preserved old shape; execute
   rollback on a representative store before release.
6. Contract/drop only in a separate admitted release after readers are retired.

No irreversible migration, deletion or compaction rewrite is selected here.

## Proof, cost and conformance

`RebuildFromRetainedFacts` must fail when a view depends on current labels, unsealed entries or
mutated origin. Real SQLite tests attempt UPDATE/DELETE/REPLACE against facts, exercise duplicate
retry/natural-key collisions and stale-generation rejection. Query plans must show bounded
indexed retrieval at the admitted workload; “SQLite is fast” is not a performance proof.

Cost trade-off: generic fields can increase assertion volume/read joins. Measure fact bytes,
query latency and replay time before promoting a dedicated schema. Source-body avoidance reduces
retention cost but does not imply metadata is free or nonsensitive.
**LOA:** T0, CQRS/immutable observations; P2/P7/P8/P9/P10; C4/C6/C7/C9.
All Atlas correctness claims remain proposed until these tests and independent gates execute.
