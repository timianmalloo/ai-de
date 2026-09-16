---
id: adr-01M2NJ2PQS7X6GE3F559JC4TNP-cross-harness-canonical-stream
title: "Cross-harness coordination uses the official request stream"
type: adr
status: draft
owner: "@timianmalloo"
phase: P0
tags: [coordination, canonical-stream, data, compatibility]
links:
  - { to: spec-cross-harness-coordination, rel: implements }
  - { to: design-cross-harness-coordination, rel: relates-to }
  - { to: adr-0020-trusted-registrar-harness-model-identity, rel: refines }
  - { to: adr-0023-watcher-observation-projection, rel: relates-to }
  - { to: proof-cross-harness-coordination, rel: tested-by }
review-by: 2026-10-16
summary: >-
  Draft ADR selecting the human-approved canonical official request log, not board dual-write.
  Records the event-log alternative to dimensional authoritative storage, accepted fold cost,
  unchanged-client compatibility, bounded existing-watcher-store additions and inherited debt.
---

# Official request stream; board is a coordination read model

**Status: P0 corrected draft; GATE P0-delta pending independent review.**
Official allocator: `coord-core.py allocate --scheme adr` returned
`adr-01M2NJ2PQS7X6GE3F559JC4TNP`. Established directory is `docs/adr/`, not
`docs/decisions/`. The official ID is intentionally not replaced by a guessed numeric ADR.

## Context

The actual human approved P0–P5 repairs, retaining the investigation's canonical-stream
boundary. ADR-0020 already says AI-Forward sessions reuse coord-core, and US-4 requires
repository-scoped provenance, parents, explicit failure and quarantined board content.
The source has two formats, not two interchangeable replicas: request-add/request-resolve
and loomkeeper/1 board-post. Ownership remains only `session-contracts.md §2`.

## Proposed decision

Use **one primary `.agents/requests.jsonl` through official tooling** for cross-harness
coordination facts and dispositions. Evolve append/read contracts compatibly. Stable
event keys plus payload equality make transport retries idempotent; history is neither
rewritten nor deduplicated. Board/MCP coordination writes go through the same canonical
command; SQLite is a rebuildable projection with atomic effect/receipt/checkpoint.
Native board facts retain their existing path and meaning.

This is the **event-log/event-sourced fold alternative** under DM5/DM13, chosen because
there is already an authoritative append history whose identity/payloads must survive.
Do not replace that history with a new dimensional authority database. Conceptual
dimensions (repository, endpoint generation, revision) remain immutable/versioned
references; changes are appended facts. Any dimensional/analytical view is derived,
never a second source. A current-valued ODS cache is not called an analytical star.

Accepted read cost: initial replay O(n), then bounded incremental fold/current-by-key
reads and cursor seeks; indexes/cache require replay-equality proof. Native newest-200
views are not coordination recovery feeds. No unmeasured hot-path full scan is accepted.

## Alternatives and consequences

* **Board canonical:** rejected; contradicts ADR-0020 and requires migrating queue users,
  authority semantics and historical identities merely to repair correlation.
* **Independent queue/board dual-write:** rejected; creates an atomicity gap and two
  definitions of acceptance. Standard inbox/read-model metadata is not another authority.
* **New normalized/temporal or dimensional canonical DB:** rejected for this scope;
  costly replacement of existing history with no necessary benefit.
* **Only wake/poll adapters:** rejected as complete solution; delivery cannot fix missing
  typed disposition or establish authority.

Positive: fallback remains official filesystem pull even when board/harness is unavailable.
Negative: transitional old readers do not understand enhanced completion; expose that
limitation without disabling ordinary legacy operations. **Unchanged, unenrolled
old-worktree clients retain official request-add/request-resolve/list on the same primary
`requests.jsonl`, without enrollment or upgrade.** Enrollment gates enhanced capabilities
only; no legacy client is disabled. Old coord-core append is unlocked: a new cooperative
lock does not automatically protect it, and an upgraded shim cannot prove coexistence.
O08/O10 pin an unmodified pre-P1 official client in an unenrolled synthetic worktree for
add→resolve→list with enhanced disabled and across rollback, asserting original IDs/payloads.
Enhanced writing stays disabled until separate mixed-client contention, complete-record
and conflict-safety proof exists with the unchanged client.
Authority verification is not supplied by content hashing; independent Security gate
must establish registrar evidence and authenticated actual-human NEW-transfer approval.

## Bounded store ruling and inherited architecture debt

ADR-0023 names the shared workspace fact store, while `WatcherHost.Open:66` opens a
SQLite `watcher.db`. Coordination application/feed/checkpoint caches are additive behind
the **existing watcher observation-store seam in existing `watcher.db` used by
WatcherHost/MCP**; `requests.jsonl` remains the sole canonical coordination source.
No new DB, native relocation, `workspace.db` migration or cross-DB transaction is in scope.
The existing physical-placement divergence is **inherited architecture debt, not claimed
conformant**. Do not expand the programme to resolve it. Data/Persistence and Distributed
Systems must coapprove the concrete additive representation before P2 code.

## Admission is not activation

Actual-human approval of all P0–P5 implementation remains in force; no renewed human phase
approval is requested. Independent P0-delta clearance admits ONLY compatible **dormant P1
fold/envelope/schema-validation and isolated tests**, not deployed SQLite schema; the
dormant subset does not complete P1. All privileged production paths deny absent qualified
verifier evidence. An apparently valid synthetic verifier receipt through the production
entrypoint must yield zero grants/transfers/endpoint sends/launches (O07).
Synthetic positive controls establish deterministic contract behavior, not production
authority. Authenticated authority fixtures, supported-channel qualification and P3/P4
activation remain BLOCKED; repository/peer prose is inert.

## Reversibility

Add reader capability before writer activation; additive cache migration only after
P2 real-store up/operational-down proof. Disable enhanced writer/importer, keep unchanged legacy clients operational, switch to compatible
reads and preserve all accepted facts. Do not restore old status files, delete new
events, dedup old messages or create a second consumed file. Contract/drop is a later,
separately approved change. Release sequences rollout; Data and DS review correctness.

## Evidence and gate

Source/read pins, concrete failure oracles and confidence labels are centralized in
the [Proof Pack](../proof/cross-harness-coordination-proof-pack.md).
This author proposes the ADR and **does not clear its independent authority, data,
DS, Test, Security, Privacy or UX gates**.
P0 is a corrected draft; P1 is pending implementation; P2–P5 are pending. O11–O20 real
SQLite/crash/replay/rollback floors are unchanged; static replay/cursor risks remain
Inferred. Live retention/erasure across payload copies and real foreground/background
GHCP/Codex/Grok/Claude conformance remain unresolved/BLOCKED; fakes do not clear them.
Proposed SLIs are not measurements. Docs index and rollups remain pending with conductor.

## P2 addendum — corrected proposal after independent DS BLOCK findings

**2026-09-16; Data/DS design scribe, not independent approval.** This addendum governs
P2 where earlier prose suggested all hot reads could avoid full-prefix validation or
all native sequences would remain allocated as before. The supplied independent DS
BLOCK findings remain open until independent Data/DS review; no automatic self-clear.
The concrete unexecuted DDL and transaction order are in design §3, not a migration.

The fixed P0 boundary stands: official `.agents/requests.jsonl` is ONE canonical
coordination stream; three additive application/feed/checkpoint caches use existing
`IWatcherObservationStore` in existing `watcher.db`. No new DB, native history relocation,
workspace.db migration, cross-DB transaction or second consumed file. ADR-0023 placement
divergence remains explicitly inherited debt, not conformance.

| Independent correction | Revised choice / still-open admission evidence |
|---|---|
| DS1: a new coordination feed leaves the two-native-service race | Future native per-repo Seq uses indexed MAX+1 inside a non-deferred store writer transaction and returns the allocated message. Preserve old IDs/Seq/duplicates, no UNIQUE retrofit. Native `sinceSeq` reads earliest N; old binaries, native tombstone mutations and snapshots are not complete change feeds |
| DS2: pending cap before a later parent deadlocks accepted work | Separate active retries from retained obligations. Commit overflow source-reference-only capacity-deferred quarantine + feed receipt + accounted checkpoint, then reach the later parent. Fair indexed recovery includes exhausted attempts; disk-full stops until capacity is restored, never silently drops |
| DS3: invented file incarnation hides source mutation | Proposed XHK/1 typed length-prefix keys; logical bound repo/origin/path/epoch established first capture. Same-byte replacement retains identity; changed/truncated prefix fails closed. Full accepted-prefix validation once per bounded snapshot/pass, not per page; O(prefix) cost acknowledged. Coherent snapshot/writer coexistence and 128-file/32 MiB capture ceilings still require Data/DS evidence |
| DS4: replay registration mints authority/liveness | Original OBSERVATION mapping is historical only; no RebindObservedRegistration capability API, Register replay, heartbeat/ended/generation mutation. Canonical projection needs no capability; native effects need current trusted lifecycle evidence. Atomic first-registration mapping and postcommit publication remain BLOCKED |
| DS5: receipt cycle and predicates masquerade as constraints | Three tables, one-way feed FK, actual parent-applied discriminator CHECK + same-scope composite FK. Immutable feed outcomes separate from mutable retry/current-state caches. No fourth receipt entity or unjustified cycle. Initial receipt existence/state-transition pairing are NOT store-enforced by this proposal: raw-SQL violation oracle and Data hard-floor BLOCK retained |
| DS6: retries/cursors/rebuild compare the wrong thing | Equal key/full bytes returns original ADMISSION receipt/mapping plus separate CURRENT state. Conflict refusal keyed by offending occurrence/digest never overwrites original. Ascending snapshot cursor continues from last returned, not high water. Same-watermark semantic rebuild does not require feed-sequence/retry-schedule equality |
| DS7: downgrade assumed to reject v8 | Observed old v7 returns for current >= 7; no future-version refusal. Actual old binary must open additive v8, retain new facts and old writes, then re-enable/replay through actual WatcherHost deployment; static source is not rollback proof |

Rejected shortcuts: invoking existing service/registrar callbacks within a purported
outer transaction; timestamp/path-change identity; blocking all scanning at the active
pending cap; count+1 sequencing protected only by an instance lock; newest-N recovery;
claiming transactional code is a raw-SQL constraint. Named patterns remain inbox,
read model, append-only transition feed and expand-migrate-contract, using existing
SQLite/stdlib; no speculative framework or dependency.

The proposed bounded correctness mode permits 128 records/4 MiB per page, 64 KiB per
admissible record, 1,024/16 MiB active pending, 64 retries/pass and 8 attempts/eligibility
cycle. None is measured, approved by Data/DS, or adequate evidence for larger scope.
Disk reserve and live retention/erasure remain unresolved. P1 producer-admission and P3
endpoint limits remain independent programme floors.

**Admission:** P2 RED test-only authoring may proceed, but no tests were authored/run in
this docs repair. P2 solution code remains unadmitted until real C# RED plus independent
Data/DS concrete schema/transaction clearance, including the explicit gaps above.
Human P0–P5 approval remains; P1 semantics review runs separately. Runtime replay/race
claims remain Inferred. Upstream work remains after verified completion of all six
phases; only project-neutral protocol contracts are candidates, never product-specific
C# watcher/store/WPF implementation by assumption.

## Accepted P2 amendment — 2026-09-16

### S1–S6 pre-release correction, 2026-09-16

Before changing DDL, adopt design §P2.2 S1–S6: global feed high-water insertion
guard; explicit integer stored types and exact TEXT/UTF-8-byte/NUL key constraints;
immutable duplicate-occurrence diagnostics, separate from admissions and current
state; contiguous boundary history derived from two indexed interval sources;
isolated payload-guard mutation and actual constructor migration-fault tests.
The three caches are retained. The event grain is one logical admission, not every
physical duplicate. Full-byte duplicate classification belongs to the future trusted
writer and is not established by raw SQL tests.

This amends fresh unreleased v8 only. Existing unreleased v8 fixtures are unsupported
upgrade inputs; released v7 migration stays additive. Ordinary tables plus explicit
storage checks avoid adopting STRICT without an established old-binary floor.
No production migration, activation, native R1/R2 repair or old-binary rollback is
claimed. Independent Data/Test re-gate remains required; author evidence cannot clear it.

The decision is amended by design `cross-harness-coordination.md` §11 A–E.
That section is the authoritative full transcription of the three accepted Data
amendments: deferred initial/current receipt constraints on the same three caches,
bounded optimistic append-compatible capture (not linearizable/excluding mutation),
and inert observation preparation with transactional original mapping/session facts
followed by post-commit publication without historical live registration.
It supersedes the one-way receipt gap and the earlier no-cycle candidate. Full-cache
DDL and capture/replay implementation remain later P2, not this native-ordering unit.

Independent DS: **PASS-WITH-CONDITIONS**, source `3d13270683655156f79dfe9d698cc0b410f2a32b`,
12-call review; DESIGN ADMITTED after this amendment is committed. Data's SQLite
3.49.1 reduced spike rejected 16 forbidden cases and exercised rollback at four write
boundaries; it is not proof of the C# full schema. No implementation/activation PASS.

P2.1 admits checked per-repository MAX(seq)+1 under SQLite IMMEDIATE plus insertion
and commit before return; in memory, store-wide locking. Add the distinct returning
AppendBoardMessageAllocated seam with a fail-closed unsupported default. Preserve
legacy void caller-sequenced insertion as a seed/import compatibility exception,
historical IDs/duplicate sequences and Int32 public signatures. Overflow refuses.
Reuse ix_board_message_repo; schema version remains 7, with no migration or index.
MCP sinceSeq reads earliest qualifying N ascending and advances by last returned Seq;
without sinceSeq retain newest-N. No historical-tie, tombstone or publisher-feed
completeness claim. Production authority remains DENY and enhanced append disabled.

Rejected: external allocation, service-instance-only locks, sequence renumbering,
uniqueness retrofit, silent Int64 API widening, and newest-N cursor recovery.
R1/R2 replay defects, full-P2 migration/rebuild/rollback and independent implementation
gates remain open. This amendment authorizes the narrow implementation, not shipment.

### P2.4A approved read-only binding amendment — 2026-09-16

Adopt the P2.4A contract in `docs/design/cross-harness-coordination.md`:
fresh pre-release v8, the same three caches, all-NULL or all-bound immutable source
identity, opaque deterministic scope hash, trusted pre-capture repository/origin,
and one-transaction source-local frozen receipt pagination. Legacy unbound sources
remain observational but unavailable through this public reader. Do not upgrade
candidate-v8 caches, backfill attribution or infer source trust from registration.
Reject global high-water pagination, mutable-event outcome joins, repository
arguments in MCP, and silent empty success on unsupported/unavailable stores.

The source file must exist for an empty bound checkpoint; zero is an accounted
empty prefix, never evidence that an absent source is healthy. Metadata-only
results distinguish cache availability from NotRecorded recovery/source health/lag.
Public Post authorization is unchanged. Production authority remains DENY and
enhanced canonical append disabled. Data/DS design approval is not code approval:
independent Test/Security/Data gates, recovery B, full P2 producer/bridge proof,
released-v7 binary rollback and P3–P5 remain open.

### P2.4B finite recovery decision / ERRATUM — 2026-09-16

Adopt design §P2.4B, committed before recovery code. Correct the earlier Data
description: eligibility is a nonnegative INTEGER generation counter, **not a
boolean**; only payload presence is boolean. New cumulative qualified registration
and parent components advance generation once each. Status/poll changes never
renew the eight-attempt budget. Refuse overflow without effects.

Choose paired deferred current-receipt FK plus staged-pointer/one-update
finalization, fixed per-lane checkpoint cursors, bounded same-repository dependency
lookups and reference-only overflow admissions. Reject boolean eligibility,
nullable tuple keys, poll-budget renewal, a second queue/writer, newest-sequence
parent discovery and blocked holders in reserved capacity. Counts derive from
bounded active rows. Source-validated ready references use the reserve only in
the transaction that applies them.

This is fresh unreleased-v8 design approval only. Existing candidate-v8 upgrade,
actual old-binary rollback, independent code gates and full P2 remain unclaimed.
All authority grants remain false; the canonical bridge and producers are separate.
