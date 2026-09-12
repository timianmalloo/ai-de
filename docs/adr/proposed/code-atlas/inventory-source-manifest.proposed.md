---
id: adr-01M2BBCCAYMNMM1MFH0653Z0XF
title: "PROPOSED — independent inventory, manifests and live source binding"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "atlas-proposed-architecture"
tags: [code-atlas, proposed, inventory, source, coherence, privacy]
links:
  - { to: architecture-code-atlas-proposed, rel: refines }
  - { to: adr-01M2BBCC9EHCWVR1R4ZCZ7502T, rel: depends-on }
  - { to: review-code-atlas-data-constraints, rel: depends-on }
  - { to: proof-code-atlas-contract-grounding, rel: depends-on }
review-by: 2026-12-12
summary: >-
  Proposes policy-bounded physical inventory independent of semantic coverage, sealed observation
  manifests, and hash-validated live source reads without default retained source bodies.
---

# PROPOSED: physical inventory and exact source binding

- **Date / author:** 2026-09-12 / `atlas-architecture-astra`.
- **Deciders:** Owner content choices recorded below; Conductor reports Data/Security content
  passes with future conditions. Final convergence and Privacy/native/Test admission remain separate.
- **Native ID provenance:** supplied by Conductor after allocator verification at 37 existing
  ADRs/no current duplicate; registration pending, no numbered-ADR takeover.
- **Status:** PROPOSED. Owner content choices are resolved below; normative acceptance and
  Core/Shell acknowledgment still precede E-0 dispatch.

**Owner content choices — “E-0 source policy” and “Physical inventory boundary”:** separate
Astra Owner `61e506c4-2d12-42e9-85cb-153f2f916811`, turn 5, relayed by Conductor. Select
hash-validated live reads and the versioned tracked-plus-authorized-nonignored-untracked policy.
These content choices grant no source permission or historical-body guarantee.

## Context

Owner explicitly rejects type-only `NodeContent` as a file explorer. K0 and direct source reading
show that `ProjectionService.NodeContent` resolves indexed provenance then reads live bytes
with a 256 KiB cap; it does not bind those bytes to indexed content. Per-scope desired-generation
fencing does not establish one coherent workspace revision.

## Proposed decision

### Physical inventory

Enumerate authorized selected roots independently of semantic extraction. Default policy includes
tracked files and authorized nonignored untracked files; ignored categories have an explicit
limitation/inclusion path. Generated, vendor, migration and unsupported are visible classifications,
not automatic physical exclusions. A tracked file in those classes is still included.

Git membership does not authorize content. A missing tracked file remains an absent record.
Policy-forbidden metadata/counts are withheld, not exposed by “excluded file” lists.
Non-Git roots use physical enumeration and explicitly lack VCS context. One physical file record
can have many project memberships. Scan pages and project aliases cannot duplicate it.

Complete means enumeration finished within the declared policy boundary with no undisclosed
errors, not “first page returned.” Missing tracked files, inaccessible paths, cycles, unreadable
directories, cancellation and scan limits
produce partial/unknown states. No scan derives its population from existing semantic nodes.

### Manifest

Grain: one sealed selection of inventory observation plus finite scope snapshot/revision vector
under one visibility policy. It carries the root set, observation interval, file membership digest,
producer/schema versions, desired/committed scope generations and exact content bindings where known.

Each scope keeps the existing fence. A manifest checks the selected references from one committed
store read boundary and explicitly reports:

- coherent observed content set, **not** atomic OS snapshot;
- mixed scope targets or overlapping-file hash conflict;
- partial missing/incomplete scopes/inventory;
- unknown binding; and separately current-at-observation, stale or last-successful freshness.

A newer A and failed B cannot render as a single new coherent workspace. A timestamp or common
git HEAD cannot establish dirty-tree coherence. A seal references immutable entries; unsealed
work never appears complete.

### Source policy

Choose **hash-validated live reads, no durable source bodies by default**. Store minimal file/
symbol/version/hash/span evidence. Read, hash and decode the same authorized buffer in Core.
This reduces sensitive data retention and storage cost; exact historical source bodies may become
unavailable. Memento-backed Back history preserves selection/manifest, not bodies. When matching
bytes are gone, the old body is **Unavailable** and anchors are disabled; no substituted new content.

Return `IndexedMatch`, `LiveChanged`, `LiveUnindexed`, `Unavailable`, `UnsupportedEncoding`,
`TooLargeToVerify`, `ReadUnstable` or `Refused`. Indexed hash and read hash are separate fields;
null hash is an explicit unverifiable state. Only `IndexedMatch` activates indexed declaration spans.
On changed bytes, expose a trusted explicit open-live/refresh action, not a guessed relocated span.

`max_verified_file_bytes`, effective response/range size and allocation/deadline bounds are
**design-required E-0 admission fields**, not invented defaults. The current 256 KiB source ceiling
is existing-contract evidence, not an admitted allocation for the new reader; reconcile it with
IPC envelope overhead and measured resource use before code. Above the admitted bound, no prefix hash masquerades as whole-content
identity. Unverified live text, if permitted, has no indexed semantic highlight.
Continuation binds file/manifest/content hash/decoder/query/range/policy; changed inputs invalidate it.
Strict UTF-8 or BOM-declared UTF-16 behavior must be tested; invalid decoding cannot retain old spans.

Native safety must validate the opened object's resolved root/identity and authorization through
read/hash, detect concurrent mutation and disallow escape through symlinks/junctions/replacements.
Hard-link behavior needs a declared policy too. The safe concrete Windows API contract is **Flagged**
until an executed handle/race spike. Path prefix checks alone do not pass this gate.

## Alternatives and resolved Owner content choice

| Option | Benefit | Cost / disposition |
|---|---|---|
| Captured immutable bodies | Exact old-source display and deterministic replay of source text | Sensitive repository mirror, consent/retention/deletion/storage obligations; not default. |
| Git object reads | Immutable committed history where authorized | Dirty/untracked content needs a separate design; process/object API not established here. |
| Hash-validated live source | Minimal retained content, existing authority-side reading pattern | Selected by Owner turn 5 for E-0; old bodies may disappear and anchors then remain disabled. |
| Live read under old path/span | Cheap | Rejected: confident wrong highlight and false snapshot claim. |
| OS-atomic workspace snapshot | Strong simultaneous guarantee | No established implementation/API or demonstrated need; not claimed by a manifest. |

## Controls and release conditions

`InventoryIsNotSemanticNodes`, `MixedScopeManifestIsNotCoherent`,
`OldSpanNeverHighlightsNewBytes`, and `SEC-E0-PATH-ESCAPE-SYMLINK-TOCTOU` must fail when the
relevant invariant is deliberately removed. Include linked project files, hidden metadata, ignored/
generated/vendor/migration paths, source deletion, empty files, invalid encoding, range boundaries,
case collision, symlink/junction cycles/escape, race replacement and cancellation.

Instrument inventory/read/hash durations, byte/count limits, binding state and refusal codes on
normal paths; no raw source in telemetry. Architecture §§10.1/11.1/11.2 own the queue, attribute/
cardinality, sampling, growth and performance admission tables. No raw paths or default per-file
logs; controlled debug requires a bounded redaction/retention policy. BudgetExceeded is explicit,
not a complete scan with omitted files. Read back a real run, not only a green exit.
**LOA:** F/T0; P2/P5/P7/P9/P11; bounded read/manifest pattern, C4/C7/C9/C11.
**Rollback:** disable Atlas source capability, retain metadata/history and legacy behavior; never
silently downgrade `IndexedMatch` to the old unbound `NodeContent` contract.
No storage/body purge or irreversible migration is authorized by this ADR.
