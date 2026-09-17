---
id: design-cross-harness-coordination
title: "Cross-harness coordination: canonical facts and fail-closed projection"
type: design
status: draft
owner: "@timianmalloo"
phase: P0
tags: [coordination, data-model, identity, replay, migration]
links:
  - { to: spec-cross-harness-coordination, rel: implements }
  - { to: architecture-loomkeeper, rel: refines }
  - { to: adr-0020-trusted-registrar-harness-model-identity, rel: depends-on }
  - { to: adr-0023-watcher-observation-projection, rel: depends-on }
  - { to: adr-01M2NJ2PQS7X6GE3F559JC4TNP-cross-harness-canonical-stream, rel: depends-on }
  - { to: proof-cross-harness-coordination, rel: tested-by }
review-by: 2026-10-16
summary: >-
  Data & Persistence P0 co-authored blueprint for typed obligations, immutable authority
  references and a single official request stream. Specifies bounded additive storage,
  atomic replay/cursor floors, failure/privacy controls and unperformed phase gates.
---

# Canonical facts, not another approval database

Current [Proof Pack](../proof/cross-harness-coordination-proof-pack.md);
the mechanical path correction changes no design gate or approved scope.

**T2 / P0 CORRECTED DRAFT. GATE P0-delta pending independent review; no self-clearance.**
Conceptual model first: [spec Part A](../specs/cross-harness-coordination.md#2-part-a--conceptual-domain-model).
No executable schema is chosen or applied. P2 is bounded to additive caches behind the
existing watcher observation-store seam in existing `watcher.db` used by WatcherHost/MCP.
Data/Persistence and Distributed Systems must coapprove the concrete representation before
P2 code. The original receipt read DM1–DM18, Testing Strategy D0/trigger union,
Observability O1–O13 and the Solution-Selection Ladder; this delta records independent F1–F6.

## 1. Canonical boundary and compatibility

Canonical coordination truth is primary-checkout `.agents/requests.jsonl`, resolved by
official `coord-core.py`; callers never infer the primary from a folder basename or
append directly. Board/MCP coordination responses call the same official command seam.
There is **no distributed queue+SQLite write transaction**: success means canonical
admission only; projection is eventual and recoverable. No second disposition ledger,
ownership register, consumed file or two independent status writers.

The named patterns are **append-only event log**, **deterministic fold**, **idempotent
consumer/inbox**, **projection/read model**, **capability-bound adapter**, and
**expand–migrate–contract**. Ladder: the existing coord helper and store abstractions
hold rung 2; stdlib encoding and SQLite transactions/constraints hold rungs 3–4.
No broker, event-sourcing framework, new hash library or generic workflow engine.
The ADR records the event-log alternative to dimensional authoritative tables and
accepts current-state fold/latest-per-key read cost. Patterns/Simplifier review pending.

### Rollout floor before a new live writer

1. Pin an **unmodified pre-P1 official client** and old payload corpus. Existing
   **unchanged, unenrolled old-worktree clients** retain official `request-add`,
   `request-resolve` and `list` on the **same primary `requests.jsonl`**, without
   enrollment or upgrade. No legacy client is disabled.
2. Enrollment gates **enhanced capabilities only**. Compatible dormant P1 fold/envelope/
   schema-validation and isolated tests may begin only after independent P0-delta
   clearance. Enhanced writing remains disabled until coexistence is proved.
3. The primary coord-core old append is **unlocked**. A new cooperative lock cannot
   automatically protect an unchanged old client; serializing upgraded old-command
   implementations is not legacy compatibility evidence. An upgraded shim is not the test.
4. O08/O10 run the pinned unmodified client from an **unenrolled synthetic worktree**:
   add→resolve→list with enhanced disabled and across actual rollback, asserting original
   IDs and payloads. Before enhanced activation, require **separate mixed-client
   contention, complete-record and conflict-safety evidence** with that unchanged client.
   No coexistence mechanism is claimed selected or proved by this draft.
5. Add tolerant readers/actionable read mode default off. Cover old reader/new stream,
   new reader/old writer, missing optional fields and unknown event types. Typed response
   events cannot pretend to be old approval. Legacy readers may conservatively leave
   enhanced threads open, but ordinary legacy add/resolve/list must still work unchanged.
6. Disable enhanced writing first to roll back; keep unchanged legacy clients operational
   and retain canonical events, original IDs and payloads. Roll back projection/read mode
   separately. Never revive an older writable status copy.

Native non-coordination board posts keep the existing contract-log path. A coordination
marker and source reference distinguish projections; board acknowledgement of native
content remains native acknowledgement, not proposal acceptance.

## 2. Typed contract (proposed names; not current CLI/API)

New immutable envelope:

| Field group | Meaning / validation |
|---|---|
| `schemaVersion`, `eventType`, `eventId` | Recognized version and closed event vocabulary; stable ID across transport retry |
| `repositoryId`, `streamId` | Registrar-bound repository and primary stream, not sender-selected cross-repo destination |
| `threadId`, `obligationId`, `inReplyTo`, `causationId` | Exact initiating obligation and event; same-repository parents only |
| `sender`, `recipient` | Actor/session/generation references; generation is explicit, never a current-alias lookup |
| `proposal` | Proposal ID, immutable revision, repository, commit, path, blob ID and full content SHA-256 |
| `authorityRefs` | Immutable references plus verifier receipt, never prose interpreted as a grant |
| `disposition`, `supersedes` | Closed typed response and explicit prior revision/event reference |
| `producerSeq`, `producerAt`, `recordedAt` | Diagnostic provenance; wall clock is not causal order or a pagination key |
| `payload`, `payloadDigest`, `digestVersion` | Quarantined bounded data, full canonical payload digest; exact retry equality |
| `scopeRef`, `candidateRef`, `runTokenRef` | Required only for relevant eligibility/START facts; refs, never bearer secret bytes |

Event vocabulary: obligation-created, response-recorded, proposal-superseded,
proposal-accepted, transport-queued, endpoint-arrived, recipient-consumed,
triage-recorded, availability-recorded, execution-checked, run-started,
run-outcome-recorded, resource-released. A rejection/refusal is an explicit result;
it need not invent a successful domain fact. Stable refusal receipts can be audited.

Disposition vocabulary: `answer`, `changes-requested`, `rejected`, `needs-human`,
`unable`, `deferred`. A deferred response includes next actor/checkpoint and preserves
the remaining obligation. `proposal-accepted` is a separate validated fact, not an
arbitrary disposition string extracted from prose. A notice explicitly has no response
obligation. The fold computes unanswered, remaining, applicable acceptance, and eligible;
none is a second authoritative stored flag.

Canonical digest contract: versioned UTF-8 JSON encoding of the entire immutable
semantic envelope, excluding receipt-generated metadata and the digest field itself;
ordinal key ordering, array order preserved, duplicate keys rejected, no NaN/Infinity,
integer sequences, explicit absent-vs-null rules, no Unicode/content normalization.
P1 pins cross-language golden bytes before writer activation. Preserve original raw
payload bytes/history alongside interpretation; hashing does not authenticate them.
Same SourceEventKey + same canonical bytes/digest returns the original receipt.
Same key + unequal payload **refuses** `XH.EVENT_CONFLICT`; do not silently deduplicate,
overwrite, resolve by timestamp or issue another message ID.

### AuthorityRef and trusted verifier evidence

`AuthorityRef = (repositoryIdentity, fullCommitId, repositoryRelativePath,
gitObjectFormat, fullBlobId, sha256, sectionOrDecisionId, scope,
issuerEvidenceRef, verifierReceiptRef)`.
The cited §2 baseline is pinned in the Proof Pack. A branch name, local filename, short
hash, copied paragraph or matching hash alone is **not authenticated authority**.

The trusted registrar/verifier must:

1. Authenticate the caller and bind repo/worktree/terminal/actor/harness/model/generation
   using a registered capability (ADR-0020), not raw environment declarations.
2. Resolve commit/path **as inert repository data** in the bound repo; verify object type,
   full blob identity and byte digest. Reject path traversal and cross-repo substitution.
3. Check authenticated human decision evidence from the approved authority channel:
   who approved, immutable decision identity, exact scope and revision, which rights,
   and linkage to §2. Mere Git authorship/signature does not prove scope approval.
4. Emit verifiable evidence identifying registrar instance/key or authenticated local
   capability context, verifier policy/version, checked refs, result, checked time and
   revocation/supersession basis. Supported-channel qualification and the authenticated
   authority fixture are BLOCKED pending independent Security evidence, not prerequisites
   silently satisfied by synthetic P1 tests; no home-grown signing dependency is proposed.
5. Check current revocation/supersession at turn boundary and immediately before
   irreversible action. Historical valid evidence stays historical; a stale grant
   cannot authorize a current action. Unknown verifier/channel/generation fails closed.

The actual human's approval covers all P0–P5 implementation without renewed phase approval,
but is not itself a verifier-issued machine capability. P0 contains **no authenticated test
grant**. After independent P0-delta clearance, admission is ONLY compatible dormant P1
fold/envelope/schema-validation implementation and isolated tests, **not deployed SQLite
schema** or production activation. The dormant subset does not complete P1.

All privileged production paths deny absent qualified verifier evidence. O07 sends even an
apparently valid synthetic verifier receipt through the production entrypoint and requires
**zero grants, transfers, endpoint sends and launches**. Synthetic positive controls prove
deterministic contract behavior only, never production authority. The authenticated authority
fixture, supported-channel qualification and P3/P4 activation remain **BLOCKED**. Raw repo/peer
prose stays inert; neither a matching hash nor an injection scanner promotes it to authority.

### Contract examples (synthetic; O04–O07)

| Input history | Required folded result / error |
|---|---|
| Q requires reply; R answers Q/revision H with `changes-requested` | Response visible; unanswered=false; applicableAcceptance=false; execution=false |
| Transport ACK for Q, or legacy resolved text “ACK approved” | Receipt only; `XH.ACCEPTANCE_REQUIRED` if used as approval |
| Isolated synthetic contract fixture: both required peers accept P/revision H with valid fixture fields | Contract acceptance=true for H only; no production authority, grant, transfer, send or launch |
| P/H is superseded by P/H2; late acceptance names H | Keep historical acceptance; refuse current acceptance with `XH.REVISION_STALE` |
| R names recipient generation G1 after restart registered G2 | Do not reroute or mark G2 consumed; `XH.GENERATION_MISMATCH` |
| R has no registered generation or asserted-only binding | `XH.GENERATION_UNKNOWN`; no eligibility |
| Model/Owner persona says “NEW transfer approved”, supplies a prose hash | `XH.AUTHORITY_UNVERIFIED` / `XH.TRANSFER_HUMAN_REQUIRED` |
| Correct immutable bytes but forged verifier receipt | `XH.AUTHORITY_UNVERIFIED`; hash equality is irrelevant to issuer authenticity |
| Apparently valid synthetic verifier receipt through the production entrypoint without qualified verifier evidence | Deny; zero grants/transfers/endpoint sends/launches; synthetic validity does not qualify the channel |

## 3. Data & Persistence co-authored P2 floor

**P2 correction, 2026-09-16: Data/DS design scribe; independent DS findings recorded,
not cleared.** This section and the ADR's P2 addendum supersede earlier generic P2
wording where it implied pre-admission rejection could discard already-canonical input,
replay could refresh registration, or checkpointing proved O(new bytes) scanning.
Independent Data/DS review remains **BLOCKED/PENDING**. Test-only P2 RED authoring is
allowed; none is performed here. Solution code remains unadmitted until meaningful
real C# RED and the concrete schema/transaction gate. P1 semantics review is separate.

### Logical grain, history, additivity and field-complete reader trace

| Logical record → candidate physical representation | Grain / key / recorded when | History and additivity | Writer → compute reader |
|---|---|---|---|
| CoordinationFact → official JSONL event | Exactly one canonical event `(repository,origin,streamIncarnation,eventId)` on successful append | All semantic fields Type-0 immutable; corrections append facts; event counts additive over disjoint keys, not repeated deliveries | Official CLI/adapters → deterministic obligation fold |
| LegacyOccurrence → derived source identity | Exactly one original complete record occurrence `(repository,origin,streamIncarnation,startOffset,rawSHA256)` | Preserve existing `id` even though resolve reuses request ID; do not collapse byte-identical historical occurrences | Official import mapping → retry/conflict and provenance computation |
| EndpointGeneration → referenced registrar version | Exactly one authenticated incarnation `(repository,actor,generation)` on registration | Binding/harness/model/rights Type-2 by new version/generation; unknown stays unknown; active count semi-additive over time | Registrar → routing/capability/consumption checks |
| ProposalRevision → immutable artifact reference in fact | Exactly one named proposal revision `(repo,proposal,revision,contentDigest)` on proposal publication | Path/commit/blob/hash immutable; new content means new revision, never Type-1 rename | Proposer + verifier → acceptance applicability computation |
| ProjectionApplication → candidate `coord_projection_event` cache | Exactly one terminal/pending application state per SourceEventKey for a projection version | Raw/digest/ref immutable; state is a labelled rebuildable cache; applied counts additive per key, pending gauge semi-additive | Import transaction → retry equality, pending recovery and board fold |
| Projection feed → candidate `coord_projection_feed` cache | Exactly one committed visible transition `(repository,feedEpoch,feedSeq)` at application-state commit | Append visible transitions including pending→applied and tombstone; counts additive; order non-additive | Same import transaction → cursor page computation |
| Checkpoint → candidate `coord_projection_checkpoint` cache | Exactly one restart scan position per `(stream,projectionVersion)` | Type-1 allowed ONLY for recomputable optimization, not authority/history; positions non-additive | Same import transaction → restart scan selection |
| Thread view → computed DTO/cache only if measured need | Exactly one obligation/revision view at named source watermark | No independent status writes; recompute; open-count gauge semi-additive | Pure fold → CLI/board/MCP/UI/actionable list |
| Availability interval / latency observation → typed facts/telemetry | One endpoint interval or one measured operation, keyed by generation/event | New interval per state; disjoint durations additive; percentiles/rates non-additive; lag non-additive | Adapter/recipient/local observer → SLI denominator and overdue computation |
| Run admission / outcome / release → canonical typed facts with scheduler reference | One checked token consumption; one outcome or release attestation per run/event | Immutable actor,candidate,process identity; no outcome overwritten by release; run count additive over distinct runs | Launcher/scheduler → single-use/start/release eligibility checks |

Envelope writer/reader completeness: version/type → parser/dispatch; event/stream/repo →
uniqueness, isolation and replay; thread/obligation/causation/parent → correlation/parent
validation; sender/recipient/generation → authorization/routing; proposal/hash/supersedes →
acceptance applicability; authority/scope/candidate/token refs → eligibility; timestamps/
producerSeq → ordering diagnostics only; payload/digest/version → display/quarantine and
conflict detection. Receipt/feed/checkpoint fields → duplicate detection, paging and restart.
No persisted field may ship with only DTO round-trip tests and no compute reader.

### Candidate physical constraints — no migration authorization yet

Coordination application/feed/checkpoint caches are additive behind the **existing watcher
observation-store seam in existing `watcher.db` used by WatcherHost/MCP**. `requests.jsonl`
remains the sole canonical coordination source. **No new DB, native relocation,
`workspace.db` migration or cross-DB transaction.** The physical-placement divergence from
ADR-0023 is tracked **inherited architecture debt**, not claimed conformant and not work
expanded into this programme. Data/Persistence and Distributed Systems must coapprove the
concrete additive representation **before P2 code**; candidate table names are not approval.

### P2-A. Source identity and bounded capture proposal

Use the existing bound `RepositoryIdentity.CanonicalPath`, not a wire repository or a
basename. `WatcherIdentity.cs:85–111` supplies `Canonicalise`: backslash identity
separators, trailing-separator handling and Windows-only invariant case folding.
Reuse that function for the absolute source-path identity; keep the actual filesystem
path separate. Do not introduce another case rule or infer repository identity from a
path alias. Unresolved binding/alias evidence blocks capture, not an invented binding.

Proposed injective encoding **XHK/1**: bytes `58 48 4b 01`, then a big-endian unsigned
32-bit field count, then, for each field, a one-byte type (`01` strict UTF-8 text,
`02` unsigned 64-bit integer, `03` opaque bytes), a big-endian unsigned 32-bit byte
length, and those bytes. Integers contain exactly eight big-endian bytes. No null,
delimiter joining, Unicode normalization or implicit text conversion. Reject invalid
Unicode and overflow. Domain tags are first text fields. The entire encoding is the
key; a hash alone is not claimed injective. Cross-language golden vectors are owed.

Let `K(tag,...)` mean that encoding. A logical stream incarnation is
`K("stream", boundRepo, origin, normalizedSourcePath, projectionEpoch)`, recorded on
first capture. Origin is the closed discriminator `official-coord` or `native-contract`;
it is importer-selected, not sender-selected. Enhanced SourceEventKey is
`K("event", boundRepo, origin, incarnation, originalEventId)`. Legacy key is
`K("occurrence", boundRepo, origin, incarnation, startOffset, rawSHA256)`.
Scope is `K("scope", boundRepo, projectionEpoch, projectionVersion)`. The projection
epoch is a persisted explicit rebuild/reset identity, **not** file creation time,
inode/file ID, replacement time or a newly guessed incarnation.

Never automatically mint a new incarnation on replacement/truncation. At each bounded
read-pass/recovery snapshot, compare the **full accepted prefix digest once**, not once
per page. Same-byte physical replacement keeps the logical identity; changed/truncated
accepted prefix yields `XH.SOURCE_GAP`, no checkpoint advance. Missing previously bound
source is a gap, not an empty successful scan. New paths require explicit binding;
renaming cannot quietly become a new source. File identity/creation metadata is only
diagnostic. Arbitrary historical mutation detection costs O(accepted prefix bytes);
neither this design nor a checkpoint promises O(new bytes) for that mode.

Capture a finite byte snapshot under a source handle that excludes mutation while
copying, then hash/parse that immutable snapshot. Whether the supported Windows handle
and unchanged writer coexistence can actually supply that exclusion is **unverified and
blocks code selection**; do not substitute before/after timestamps or two matching
hashes for a coherent snapshot. Append beyond the captured length belongs to the next
pass. Preserve offsets and full raw digests before `ReadAttrs` or timestamp sorting.
Record length includes the terminating LF (and CR if present); a partial final record
is neither fabricated with an added newline nor checkpointed.

Proposed pilot ceilings, **not measurements or approved defaults**: 128 source files
per discovery pass (enumerate at most 129 to detect overflow), 32 MiB aggregate captured
bytes, 128 complete records/4 MiB per apply page, 64 KiB decoded/admissible record.
One official source is resolved by coord-core: `.agents/requests.jsonl`. Native discovery
is top-directory `*.jsonl` under the already-bound contract directory, never recursive,
and sorted by normalized identity. No broad repository scan or second consumed file.
Oversized complete records within the snapshot can be streamed to a raw digest and
source-reference quarantine without copying their payload into the DB. A snapshot,
discovery or binding ceiling failure stops that pass with explicit backpressure and no
unaccounted advance. A byte ceiling is not permission to truncate a record or source
list and call it complete. Data/DS must review a larger-scope mode; no automatic approval.

### P2-B. Concrete three-table candidate (DDL for review, not an applied migration)

One application aggregate, rooted at `(scope, source_key)`, protects **one durable
effect/disposition and its receipt/checkpoint commit**. Native historical registration
is a referenced observation, not another aggregate whose live authority replay may
mutate. Three new tables only: application cache, immutable transition/receipt feed,
and checkpoint cache. No fourth receipt entity, cyclic FK or immediate cyclic trigger.
`scope` encodes repository/epoch/version as above; every FK retains that same scope.
`ever_applied` is a sticky identity marker, not an authorization or live-session flag.

```sql
CREATE TABLE coord_projection_event (
  scope BLOB NOT NULL,
  source_key BLOB NOT NULL,
  stream_key BLOB NOT NULL,
  original_id TEXT,
  raw_start INTEGER NOT NULL CHECK(raw_start >= 0),
  raw_length INTEGER NOT NULL CHECK(raw_length > 0),
  raw_sha BLOB NOT NULL CHECK(length(raw_sha) = 32),
  digest_version INTEGER CHECK(digest_version = 1),
  canonical_sha BLOB CHECK(length(canonical_sha) = 32),
  canonical_bytes BLOB CHECK(length(canonical_bytes) <= 65536),
  payload_ref BLOB NOT NULL,
  mapping BLOB,
  conflict_of BLOB,
  logical_parent_key BLOB,
  applied_parent_key BLOB,
  parent_applied INTEGER,
  ever_applied INTEGER NOT NULL DEFAULT 0 CHECK(ever_applied IN (0,1)),
  state TEXT NOT NULL CHECK(state IN ('pending','applied','refused','quarantine')),
  reason TEXT NOT NULL,
  attempts INTEGER NOT NULL DEFAULT 0 CHECK(attempts BETWEEN 0 AND 8),
  due INTEGER CHECK(due >= 0),
  recovery_n INTEGER NOT NULL DEFAULT 0 CHECK(recovery_n >= 0),
  active_bytes INTEGER NOT NULL DEFAULT 0 CHECK(active_bytes BETWEEN 0 AND 65536),
  PRIMARY KEY(scope, source_key),
  UNIQUE(scope, stream_key, raw_start),
  UNIQUE(scope, source_key, ever_applied),
  FOREIGN KEY(scope, conflict_of)
    REFERENCES coord_projection_event(scope, source_key),
  FOREIGN KEY(scope, applied_parent_key, parent_applied)
    REFERENCES coord_projection_event(scope, source_key, ever_applied),
  CHECK((digest_version IS NULL AND canonical_sha IS NULL AND canonical_bytes IS NULL)
     OR (digest_version IS NOT NULL AND canonical_sha IS NOT NULL)),
  CHECK((applied_parent_key IS NULL AND parent_applied IS NULL)
     OR (applied_parent_key IS NOT NULL AND parent_applied IS NOT NULL AND parent_applied = 1)),
  CHECK((state = 'applied' AND ever_applied = 1)
     OR (state <> 'applied' AND ever_applied = 0)),
  CHECK(state <> 'applied'
     OR (logical_parent_key IS NULL AND applied_parent_key IS NULL)
     OR (logical_parent_key IS NOT NULL AND applied_parent_key IS NOT NULL
         AND applied_parent_key = logical_parent_key)),
  CHECK(conflict_of IS NULL OR state = 'refused')
);
CREATE TABLE coord_projection_feed (
  n INTEGER PRIMARY KEY AUTOINCREMENT,
  scope BLOB NOT NULL,
  source_key BLOB NOT NULL,
  transition_key BLOB NOT NULL,
  outcome TEXT NOT NULL CHECK(outcome IN
    ('pending','applied','refused','quarantine','tombstone')),
  reason TEXT NOT NULL,
  mapping BLOB,
  UNIQUE(scope, source_key, transition_key),
  FOREIGN KEY(scope, source_key)
    REFERENCES coord_projection_event(scope, source_key)
);
CREATE TABLE coord_projection_checkpoint (
  scope BLOB NOT NULL,
  stream_key BLOB NOT NULL,
  projection_version INTEGER NOT NULL CHECK(projection_version = 1),
  complete_offset INTEGER NOT NULL CHECK(complete_offset >= 0),
  prefix_sha BLOB NOT NULL CHECK(length(prefix_sha) = 32),
  PRIMARY KEY(scope, stream_key)
);
CREATE INDEX coord_feed_page ON coord_projection_feed(scope, n);
CREATE INDEX coord_parent_work
  ON coord_projection_event(scope, logical_parent_key, state, raw_start, source_key);
CREATE INDEX coord_due_work
  ON coord_projection_event(scope, state, due, source_key);
CREATE INDEX coord_first_receipt ON coord_projection_feed(scope, source_key, n);
CREATE INDEX board_future_seq ON board_message_fact(repository_key, seq);
CREATE TRIGGER coord_feed_no_update BEFORE UPDATE ON coord_projection_feed
BEGIN SELECT RAISE(ABORT, 'XH.IMMUTABLE_FEED'); END;
CREATE TRIGGER coord_feed_no_delete BEFORE DELETE ON coord_projection_feed
BEGIN SELECT RAISE(ABORT, 'XH.IMMUTABLE_FEED'); END;
CREATE TRIGGER coord_event_no_delete BEFORE DELETE ON coord_projection_event
BEGIN SELECT RAISE(ABORT, 'XH.IMMUTABLE_SOURCE'); END;
CREATE TRIGGER coord_event_identity_no_update BEFORE UPDATE OF
  scope, source_key, stream_key, original_id, raw_start, raw_length, raw_sha,
  digest_version, canonical_sha, payload_ref, conflict_of, logical_parent_key
  ON coord_projection_event
BEGIN SELECT RAISE(ABORT, 'XH.IMMUTABLE_SOURCE'); END;
CREATE TRIGGER coord_event_sticky BEFORE UPDATE ON coord_projection_event
WHEN (OLD.ever_applied = 1 AND NEW.ever_applied <> 1)
  OR (OLD.mapping IS NOT NULL AND NEW.mapping IS NOT OLD.mapping)
  OR (OLD.canonical_bytes IS NOT NULL
      AND NEW.canonical_bytes IS NOT OLD.canonical_bytes)
BEGIN SELECT RAISE(ABORT, 'XH.IMMUTABLE_MAPPING'); END;
```

This is **unexecuted candidate SQL**, including indexes/triggers. FK/recursive-trigger
settings must be enabled/read back on every writer; raw SQL bypass tests include
`INSERT OR REPLACE`. Payload materialization (null to bounded canonical bytes) must
compare the full versioned bytes against a freshly validated source reference; SQL
does not compute that canonicalization or digest. `active_bytes` is an operational
budget cache, not another fact; writer reservation/release must equal materialized
active payload bytes. Counts/byte totals are computed, not stored as another counter.
Pending/retry/checkpoint fields are mutable Type-1 optimization caches; source identity,
original ID and each feed outcome/mapping are immutable. Later changes append feed
rows; no trigger freezes all current state or blocks existing native-table writes.
These synthetic-only payload copies do not settle the live erasure design in §7.

**Explicit constraint gap / BLOCKED:** one-way feed-to-event FK rejects orphan feed
rows, but cannot require every event to have an initial receipt at commit. Nor does it
alone prevent a raw current-state UPDATE without its matching feed transition. The
transaction-boundary protocol below supplies application atomicity, not store-enforced
initial-receipt existence. A raw `BEGIN; INSERT event; COMMIT` without feed is an explicit
violation test expected to expose this candidate's gap. Data's hard floor is **not waived**.
Data/DS must resolve it before schema/code admission. If a cycle is later justified,
name `first_receipt_n` and prove exactly the initial receipt, never a generic receipt
that prevents later transitions. No speculative cyclic FK is silently added here.

The first feed row (`MIN(n)` for the source key in scope) is the immutable **ADMISSION**
receipt; later feed rows are transition receipts. Duplicate same enhanced key and equal
full canonical bytes returns that original admission receipt/mapping plus separately
labelled **CURRENT** state/mapping. Digests are an index aid, never equality alone.
New-offset duplicates need no new event, admission or state-transition receipt.
They append one immutable `duplicate-occurrence-accounted` diagnostic and advance the
checkpoint atomically. Reprocessing an accounted occurrence appends nothing. The
diagnostic is not a semantic retry, native effect, capability refresh or current state.
The trusted writer must compare FULL versioned canonical bytes, not hashes alone,
before classifying a duplicate; SQL cannot validate unavailable captured bytes.

### P2.2 S1–S6 correction contract — recorded before DDL, 2026-09-16

This supersedes the earlier occurrence-grain wording for the three cache tables.
One event is exactly one logical admission, with immutable first occurrence and original
admission receipt. One feed row is an admission, semantic transition, or explicitly
separate occurrence-accounting diagnostic. One checkpoint is the contiguous accounted
prefix of boundary `(scope, epoch)`; scope encodes repository/stream/projection incarnation,
never a bare event ID. Counts are additive within a category, not across these categories.
Current pointers/checkpoints are derived Type-1 caches; receipt and occurrence history
is immutable. No fourth counter/table or independently maintained boundary authority.

S1: every committed feed insertion must exceed the existing GLOBAL high-water mark.
Explicit holes are allowed. Validate the actual assigned `n` AFTER INSERT so SQLite's
BEFORE INSERT default rowid sentinel cannot bypass the rule; automatic allocation remains
valid. No legacy ID, sequence or history rewrite.

S2/S3: constrain integer stored types plus numeric ranges, including nullable generation
and diagnostic offsets. No due/retry columns exist in this foundation; future additions
must apply the same rule. Keys remain exact TEXT, no implicit normalization; reject BLOB,
embedded NUL and UTF-8 byte-length overflow (scope 4096, epoch 128, event key 512).
Explicit `typeof` checks preserve ordinary-table format. STRICT was considered: installed
Microsoft.Data.Sqlite is 10.0.11, but old-binary compatibility is not established, so no
new schema-format floor is adopted on an inferred compatibility claim.

S4: the diagnostic has `is_initial=0`, original `admission_n`, database-allocated `n`,
and nullable feed `source_offset`, `source_end`, `raw_digest`, required ONLY for this
outcome. Application state, session/message/parent mapping are NULL. A full
`(scope,epoch,event_key,admission_n)` FK anchors it. It never updates current state and
cannot be used as the current pointer. Event first intervals UNION diagnostic intervals
form history. Each new interval starts at the latest endpoint in its boundary (initial
zero); a checkpoint must end at that same endpoint and advance monotonically.
The writer returns original ADMISSION mapping plus separate CURRENT state. SQL fixtures
prove accounting/reference facts only; real capture, full-byte classification and the
atomic writer remain mandatory unmet integration floors.

S5: ordered endpoint indexes on both histories and indexed scope/epoch range predicates
replace unbounded epoch-conflict and offset-only endpoint scans. Query plans and bounded
predicate-visit fixtures establish only those lookup paths, not whole-pump runtime.

S6: payload tests stage a valid next receipt without automatic pointer movement, then
attempt changed canonical bytes with the valid pointer. Intact payload rejection must
be `COORD_PAYLOAD_IMMUTABLE`; removing ONLY that clause must admit the mutation.
Keep positive ordinary-transition and paired tombstone clearing controls.

Version policy: amend only the fresh UNRELEASED v8 candidate before activation.
Released v7→fresh v8 remains additive. Existing unreleased v8 fixtures are NOT upgraded
by this change; no DROP, downgrade or history deletion is a repair path. Old-binary
rollback remains unexecuted. Supplied Data/Test findings authorize this bounded author
repair, not self-clearance: independent Data/Test re-gate follows all six corrections.

Conflicting canonical bytes do not mutate the original event or its state. Persist one
refusal application keyed by `K("conflict", originalKey, offendingOccurrenceKey,
canonicalDigestVersion, offendingCanonicalDigest)` with `conflict_of` naming the original,
then its admission feed row. Retry of that occurrence returns the same refusal.
Repeated delivery never manufactures infinite rows; genuinely distinct offending
occurrences remain distinct source facts subject to storage backpressure. Changed
already-accounted source position is `SOURCE_GAP`, not a conflict overwriting that position.

### P2-C. Prepare, write, commit, publish

1. From the bounded immutable snapshot, validate/prepare **inert typed data**:
   source identity, canonical bytes/reference, parent identity, proposed native effect,
   lifecycle evidence reference and observation mapping. `PreparedEffect` is not a
   callback/delegate that calls a registrar/service outside the DB transaction.
2. Acquire a **non-deferred SQLite writer transaction before any lookup/allocation**.
   Establish the installed provider's exact transaction API in the real C# RED fixture;
   an instance lock or default-overload guess is not proof.
3. Lookup source key/position, compare full bytes, resolve same-scope applied parent.
   Insert application (or stable refusal/pending reference) first. Parent already applied
   can be referenced by `(scope, key, 1)`; a missing parent leaves only the logical key.
4. Write the durable effect, if allowed, and source-to-original-ID mapping using the
   **same connection and transaction**; then INSERT admission/transition feed (database
   allocates `n`), then UPSERT the complete-record checkpoint/digest, then COMMIT.
   Pending-to-applied first sets the resolved discriminator/FK and writes the effect,
   then appends its transition; parent identity remains valid after payload tombstoning.
5. Publish process-local state only after commit; ACK/return only after that. A reader
   sees either the whole durable unit or none. Uncertain commit, lost ACK and process
   restart retry the **same identity**, never another GUID/event ID.

**No `RebindObservedRegistration` capability-mint API.** Persist original
OBSERVATION mapping; replay returns a historical reference, never `Register` again,
never heartbeat refresh, ended-state clearing or generation mutation. Canonical
coordination projection needs no capability. Native effects require current trusted
lifecycle evidence or stay pending/refused; a historical mapping cannot self-mint it.
Current `TrustedRegistrar.Issue:81–93` publishes a capability before separate session/
ended/heartbeat writes. Wrapping `Register` in an outer transaction is not this protocol.
Native first registration needs an independently cleared inert-prepare / same-transaction
durable registration+mapping / postcommit publication design, with the lost-publication
recovery case. **That exact seam remains BLOCKED**, not solved by this scribe.

### P2-D. Native ordering and reliability inventory

Independent DS correction: two `MessageBoardService` instances require future per-repo
native `Seq` allocation **inside the store writer transaction**, using indexed
`MAX(seq)+1` after writer acquisition and returning the allocated `BoardMessage`.
Preserve every old ID/Seq/payload and historical duplicate Seq; no retrofit UNIQUE or
dedup. Check integer exhaustion explicitly; never wrap or renumber. The current
`AppendBoardMessage` accepting caller Seq is not claimed to provide this guarantee.
Future native MCP `sinceSeq` uses earliest N ascending after the cursor, not newest N;
snapshot mode without `sinceSeq` may retain its documented newest-N behavior.

| Surface | Completeness contract after a future qualified P2 implementation |
|---|---|
| New coordination feed | Every visible transition including tombstone; ordered committed pages |
| Updated native incremental reader + updated store writers | Future insert ordering only; existing duplicate Seq history is disclosed, not repaired |
| Old binaries, native tombstone mutations, publisher snapshots | **NOT change-feed completeness**; old writers can reintroduce duplicate/nonmonotone Seq |

Neither the new feed nor updated native ordering retroactively upgrades old clients.
Old-binary rollback preserves data/operability; it suspends the new reliability guarantee.

### P2-E. Capacity without child-before-parent deadlock

Active retry capacity is **not retained-obligation capacity**. Proposed pilot bounds:
1,024 active pending records and 16 MiB active pending payload; 64 retries per pass and
8 attempts per eligibility cycle. These are proposals, not measured defaults.
On overflow **after canonical acceptance**, commit a visible capacity-deferred
`quarantine` application containing a bounded source reference only (no payload copy),
its feed receipt and checkpoint. Continue scanning so a later parent can arrive.
The checkpoint accounts for retained canonical work; it never drops it.

Indexed parent arrival reconsiders bounded fair batches, including capacity-deferred
and attempts-exhausted children. Recoverable edges are:
`pending -> applied`; `pending -> quarantine(attempts-exhausted)`;
`quarantine(capacity-deferred|attempts-exhausted) -> pending|applied` on evidenced parent
arrival, restored capacity or explicit operator recovery. A new eligibility cycle
resets attempts only for that evidence and increments `recovery_n`; clock ticks alone
cannot bypass exhaustion. Unsupported versions require qualified-version recovery,
not blind retries. Polling metadata changes need not emit facts; visible state changes
do. Tombstoned applied parents still satisfy identity, never revive payload.

Due-work and parent-work continuations use deterministic `(due, source_key)` or
`(raw_start, source_key)` order with a rotating per-scope cursor and bounded share for
each queue. A deferred batch cannot restart forever at its first child. Attempt limits
end automatic retries, not the obligation. Canonical references may accumulate with
canonical history; there is no promise of infinite finite-disk retention. Disk-full or
unavailable DB stops scanning/admission with `XH.STORAGE_UNAVAILABLE`/backpressure and
**no uncommitted checkpoint advance**. Reserve capacity for status/progress; its concrete
size, accounting and recovery threshold remain Data/SRE gate inputs. If even a reference
cannot commit, stop; recovery starts only after capacity is restored. No second spool.
P1 producer-admission and P3 endpoint-queue limits remain separate floors, not excuses
to omit this bounded P2 scanner over already-accepted input.

### P2-F. Cursor, reconstruction, migration and measurement gates

Cursor is `(repository, projectionEpoch, projectionVersion, lastReturnedN)`. Read earliest
ascending page and high watermark from **one read snapshot**; continuation is the last
returned row, **never high watermark**. Empty pages preserve continuation. Database-global
AUTOINCREMENT gaps across repositories/rollbacks are legal; sequence contiguity is not
promised. A reader between writer A and B commits sees A, then B above its continuation.
Pending-to-applied and tombstone transitions are new feed rows; invalid scope/epoch/version
requires explicit reset. O18 covers 401 rows as 200/200/1 **and larger corpora**.

Semantic rebuild compares original identities/mappings, payload availability, parent
relationships and folded outcomes at the **same canonical watermark**. It does not
compare regenerated feed sequence numbers, retry timing, attempt counts or scheduling.
An epoch reset requires cursor reset, not pretending an old feed is the new one.
Indexes above are candidates; source lookup, first-receipt, cursor, parent and due
queries require real EXPLAIN QUERY PLAN plus 100x cardinality/rows-visited/bytes evidence.
Full-prefix capture once per pass must be counted separately from indexed page cost.

Candidate v8 is additive on observed v7. `EnsureSchema:1057–1062` returns when
`current >= SchemaVersion`; **there is no old-reader future-version block**. That is
static evidence, not rollback proof. Actual `WatcherHost.Open` deployment must expand a
representative old DB, preserve old writes/IDs/Seq including duplicates, retain newly
accepted facts through an **actual old-binary open/rollback**, then re-enable and rebuild.
No DROP, version downgrade, historical dedup, native relocation, new DB, workspace.db
transaction or new trigger blocking old writes. Release/Data/DS clearance remains owed.

Operator questions and normal-path emitting sources (all **unimplemented/unmeasured**):
capture duration/files/prefix bytes/page bytes -> projection span; active records/bytes,
oldest deferred age and reserve remaining -> bounded gauges; attempts, deferred/recovered
transitions and source gaps -> counters; source-key/outcome/cursor and commit uncertainty
-> structured logs without raw payload/capabilities. A failed metric reads Not Recorded,
not zero. O20/O28 must observe both success and failure emission. No UI/endpoint work here.

## 4. Expand → migrate → contract; real rollback required

| Step | Forward / compatibility | Rollback exercise required in P2 |
|---|---|---|
| Expand | Add versioned cache shapes; deployer applies migration; old tables/payloads remain readable and writable; importer disabled | Run old binary against expanded DB, compare original rows/IDs/payload bytes; disable new components, leave additions inert |
| Migrate | Replay canonical source, no guessed linkage; verify source-to-cache equality; old/native board unaffected | Crash at each boundary; restart old path and official queue; re-enable importer and obtain identical logical result |
| Move reads | Opt-in coordination readers consume complete feed; legacy reader available with honest limitations | Toggle reads back without changing source; verify new facts survive and can be projected again |
| Contract | Remove obsolete code path only after caller inventory and approval; no DROP/DELETE/dedup/consumed-file migration in P0–P5 | Back out code/config while retaining expanded shapes; separately reviewed future destructive migration if ever needed |

“Down” for this additive change means the **tested operational rollback** above, not
dropping newly accepted history. The fixture must run expansion and rollback on a
representative old database plus new accepted events; exercise the actual deployer.
Do not clear migration safety with compilation, an in-memory store, or a proposed plan.
Release owns sequencing; Data and DS must coapprove the concrete additive representation
before P2 code and independently review the transaction/consistency seam. P0-delta admission
does not deploy SQLite schema. O11–O20 retain the complete mandatory real-store floor.

## 5. Failure-mode analysis

| Category / mode | Disposition | Oracle |
|---|---|---|
| Input: duplicate key changed payload, malformed bytes/version | Prevent conflict overwrite; detect explicit errors; preserve accepted history | O09, O17 |
| Dependency: disk full / DB busy / adapter unavailable | Mitigate pre-admission backpressure; recover via stable retry and canonical pull | O20–O22 |
| Concurrency: two writers, crash before receipt, snapshot cursor | Prevent with store transaction/constraints; recover same mapping | O11–O15 |
| State: late parent, restart/end generations, tombstone replay | Persist pending disposition; refuse stale identity; never resurrect redacted content | O16–O19 |
| Resource: limit+1 / permanently missing parent / unbounded retries | Explicit refusal before acceptance; bounded attempts then human queue | O20–O21 |
| Time: skew, lease expiry, stale acceptance | Detect via causation/generation/ref checks; no clock-derived authority | O05–O07, O24 |
| Visibility: newest-N gap or unavailable rendered empty | Cursor-complete API plus explicit gap/availability | O18, O26 |

## 6. Adversarial analysis (STRIDE-lite)

| Boundary / threat | Disposition and control | Negative test |
|---|---|---|
| Producer→official append / Spoofing | Mitigate registrar-bound generation/capability; no asserted identity floor | O06–O07 forged actor/verifier |
| Repo artifact→authority verifier / Tampering, Elevation | Mitigate commit/path/blob/content plus authenticated decision evidence; no prose-to-tools | O04–O07 hash substitution, traversal, Owner NEW transfer |
| Canonical→projection / Tampering, Repudiation | Mitigate stable identity/conflict refusal and provenance; preserve originals | O09, O11, O17 |
| Board/MCP→recipient / Information disclosure, Elevation | Mitigate bound repo routing, quarantine and minimized typed context | O22, O26 cross-repo/injection |
| Queues→adapter / Denial of service | Mitigate bounded bytes/items/attempts and explicit backpressure | O20–O21 |
| Launcher→scheduler / Spoofing, Replay | Mitigate checked token + PID creation/parent identity; outcome distinct from release | O23–O25 |
| Human decision→execution / Repudiation, Elevation | Transfer authentication-channel qualification to named Security reviewer; fail closed meanwhile | O07; independent gate unresolved |

No threat is silently “accepted.” Boundary reviews remain independent. Scanner matches
are diagnostics, not the security boundary.

## 7. Privacy analysis (LINDDUN-lite)

| Flow / categories | Threat | Disposition / retention and rights |
|---|---|---|
| Registrar→events: actor/session/worktree/process refs | Linkability, Identifiability | Minimize to opaque IDs and repo-relative refs; local access controls; no personnel analytics |
| Request→board/recipient: work prose/artifact refs | Detectability, Disclosure | Only required context to enrolled recipients; no third-party egress or full transcript copies; credentials/run bearer tokens never persisted |
| Authority evidence→verifier: human decision identity | Non-repudiation, Unawareness | Use minimal evidence reference; disclose durable recording/purpose; human-reviewed access/retention |
| Availability→SLI: model health/paused duration | Linkability, Non-compliance | Coarse reason codes, not private quota/account details; never infer employee performance |
| Canonical→caches/export/backups | Non-compliance, Disclosure | Policy deletion must cover every payload copy and restore path; envelope tombstone preserves referential integrity |

P0 preserves existing history and performs no deletion. Production retention duration,
legal basis, erasure authority and backup expiry are **unresolved operator/Privacy
questions**, not indefinite-retention consent. Minimize new payloads to references;
policy-authorized redaction must not invent acceptance or erase audit identity.
Live retention/erasure across source, caches, exports, endpoint copies and backups remains
unresolved. Before P2 live import, reconcile append-only source preservation with payload deletion:
an immutable log containing raw sensitive prose cannot satisfy erasure by a board
tombstone alone. Required design is policy-controlled payload availability with immutable
envelopes. Dormant tests use only non-sensitive minimized synthetic fixtures; they do not
qualify live retention/erasure or clear sensitive payload admission. No retroactive log
rewrite or “delete/dedup migration” is approved here.

## 8. AI, UX and observability lenses

**AI:** deterministic validation/fold/authority/run eligibility; no model judges whether a
grant is valid. A model can propose a typed response, but boundary validation is mandatory.
P3 canned-output routing tests and an adversarial usability/eval corpus pair with installed
real-endpoint conformance. Model text, raw repository content and peer prose are data.

**UX:** same computed thread DTO for CLI/board/MCP/UI, with source/parent/revision identity
and negative states retained. Existing G6/WPF surface is reused, no new design language
or assets in P0. P5 owes keyboard/screen-reader/contrast/reduced-motion and craft tests
against the built surface, plus complete state copy. An unavailable endpoint is a
visible **BLOCKED** positive-conformance row, not a green fake-only endpoint.

**Observability:** span each append/project/deliver/triage/execute check with trace context;
W3C traceparent where supported, otherwise an explicit unlinked-boundary disclosure.
Use existing logging/Activity/Meter facilities; no exporter dependency here. Structured
events carry bounded result/error code, source-event correlation and stage. High-cardinality
IDs are log/span attributes, never metric labels. No raw prose, credentials, capability
bytes or personal details in telemetry. Load-bearing errors/metrics get O28 assertions.
No HTTP endpoint is added; RFC 9457 becomes mandatory if one is later proposed.

Proposed error registry: `XH.EVENT_CONFLICT`, `XH.AUTHORITY_UNVERIFIED`,
`XH.TRANSFER_HUMAN_REQUIRED`, `XH.ACCEPTANCE_REQUIRED`, `XH.REVISION_STALE`,
`XH.GENERATION_UNKNOWN`, `XH.GENERATION_MISMATCH`, `XH.PARENT_PENDING`,
`XH.PARENT_UNRESOLVED`, `XH.VERSION_UNSUPPORTED`, `XH.CURSOR_RESET_REQUIRED`,
`XH.SOURCE_GAP`, `XH.BACKPRESSURE`, `XH.STORAGE_UNAVAILABLE`,
`XH.ENDPOINT_UNAVAILABLE`, `XH.TOKEN_USED`, `XH.HOLDER_UNRECONCILED`.
One implementation registry later; messages may vary, codes and stages may not.

## 9. P3/P4 endpoint and launcher contracts

Each of foreground/background **GitHub Copilot, Codex, Grok and Claude** needs an
installed/vendor version-pinned spike before choosing its adapter. Record registration,
addressability (including sibling write versus owned read), running-turn deferred input,
idle/resume/wake behavior, unavailable/quota/context/auth failures, cancellation limits
and evidence for generation-specific arrival **and recipient consumption**.
Fake transport proves deterministic retry only; separate low-volume positive conformance
within the approved programme is required for every supported cell. All eight foreground/
background GHCP/Codex/Grok/Claude cells remain **BLOCKED** pending supported availability
and witnessed arrival, consumption and supported post-turn wake; fakes clear none.
Do not probe unknown consumed
nonces or resurrect an old conversation to manufacture evidence. Unavailable stays BLOCKED.

Launcher provenance is `(actor,generation,repository,worktree,candidateCommit,runId,
pid,pidCreationTime,parentPid,parentCreationTime,parentEvidenceRef)`, bound at launch,
not hard-coded in a reusable test helper. Validate token scope, expiry and holder identity;
atomically consume once; duplicate delivery returns original run receipt. Token expiry
prevents new START, not proof of process death. END/outcome and RELEASE are separate;
failure can be an outcome without a safe release. No regrant over an unreconciled live
holder. Existing Atlas bug, peer slots and observer remain untouched.

## 10. Definition-of-done status (20-item gate read)

| DoD items | P0 evidence/status |
|---|---|
| 1 responsibility; 3 change surfaces; 4 phasing; 5 conventions; 8 ladder; 19 ledger; 20 status | Draft documented; see linked plan/proof, not independently accepted |
| 2 data model/invariants | Model and grain drafted; store enforcement/replay/up-and-rollback unperformed |
| 6 consumed contracts; 7 pattern reviews | Source contracts inspected; vendor spikes and independent Patterns/Simplifier review pending |
| 9 failure modes; 10 STRIDE; 11 LINDDUN; 16 test union; 17 telemetry | Explicit draft tables and oracles; independent reviews/execution pending |
| 12 UI; 13 design language; 14 craft gate | No UI changes in P0; P5 inherits existing design language and must complete concrete UI/craft evidence |
| 15 rollups | Conductor checkpoint owns security/privacy rollups, typed backlinks and derived regeneration |
| 18 independent vetoes | NOT cleared; no self-issued PASS |

**Status:** P0 corrected draft; P1 pending implementation; P2–P5 pending. Proposed SLIs
are not measurements; docs index and security/privacy rollups remain conductor-owned/pending.
**GATE P0-delta pending independent review. Best next action:** independent delta reviewer,
then conductor-admitted Python P1 author for the compatible dormant subset only. No self-clearance.

## 11. Accepted Data/DS amendments and P2.1 admission — 2026-09-16

This amendment supersedes the contradictory P2-A/B/C candidate text above, including
the one-way receipt gap and the former prohibition on cyclic foreign keys. The old
candidate is retained as a correction record, **not executable approved DDL**.
Human approval continues to cover P0–P5. The independent DS review of source
`3d13270683655156f79dfe9d698cc0b410f2a32b` (12 calls) returned
**PASS-WITH-CONDITIONS**: the following three Data amendments and narrow ordering
design are DESIGN ADMITTED once committed. This is not implementation, migration,
activation, or full-P2 PASS. Data's reduced SQLite 3.49.1 spike rejected 16 forbidden
cases and rolled back first/current receipt transactions at four write boundaries.
That is reduced-schema evidence, not C# full-schema verification.

### A. Receipt aggregate correction (later P2 implementation)

Keep exactly the same three event/feed/checkpoint caches in existing `watcher.db`;
the canonical `.agents` stream is unchanged. Each event has immutable NOT NULL
`first_receipt_n`, `first_kind = 1`, and `current_receipt_n >= first_receipt_n`.
Feed rows carry `is_initial` in {0,1}, `admission_n`, and `application_state`
separate from immutable `outcome`. Initial rows have `is_initial=1`,
`admission_n=NULL`, and cannot be tombstones. Transition rows have `is_initial=0`,
non-null `admission_n`, and `n > admission_n`. Ordinary outcome equals application
state; a tombstone's application state remains applied.

Use deferred composite foreign keys: event first receipt → feed `(n,is_initial)`;
event current receipt/state → feed `(n,application_state)`; feed → event; transition
admission → event first receipt. Include scope/source identity in every relevant
key so another event's receipt cannot satisfy it. A partial UNIQUE index permits
exactly one initial receipt per event. Applied-parent eligibility is an actual
CHECK discriminator column plus composite FK, not an application-only check.
Transition triggers enforce an increasing current pointer, no remapping and no
resurrection. Derive current reason/mapping through the feed join, not duplicate
mutable columns. Payload clearing is permitted only with a paired tombstone;
release active bytes but preserve identity, digest and applied-parent identity.
Never rematerialize cleared content.

Acquire SQLite IMMEDIATE before key lookup or allocation. Within that transaction:
insert initial feed and obtain `n` → insert event referencing `n` → typed native,
session and mapping effects → checkpoint → COMMIT → publish memory/ACK. Deferred
constraints are checked at commit. No registrar callback may escape this transaction.
Full-cache implementation and raw-SQL/full-C# constraint tests remain later P2.

### B. Capture guarantee correction (later P2 implementation)

Use bounded optimistic, append-compatible capture, **not** a linearizable snapshot
or mutation-excluding handle. Capture complete LF records; defer an incomplete tail.
Validate copied prefix bytes and length and the accepted-prefix digest once per pass;
apply immutable pages from that copy. Reload checkpoint in the write transaction:
if it advanced beyond the snapshot, discard and recapture within the finite pass
budget. Changed, truncated or missing accepted prefix reports `SOURCE_GAP` and
retains checkpoint/incarnation. Mutation after validation and transient ABA are
explicitly outside the guarantee. No blind new incarnation or unbounded recapture.

Retain scoped injective repository/origin/path/epoch identity and ceilings of 128
files, 32 MiB aggregate capture, 64 KiB record, 128 records/4 MiB page. These are
design bounds, not measured production SLIs. Full capture implementation is later P2.

### C. Observation-only replay correction (later P2 implementation)

Prepare inert data → transactionally write the original OBSERVATION mapping and
session facts → publish observations only after commit. Historical replay cannot
Register, increment generation, refresh heartbeat, clear ended, or issue capability.
Native observed content may persist through an internal typed projection bound to
the observed mapping, preserving provenance, quarantine and thread constraints,
without execution rights. Public Post/Reply/Acknowledge capability checks stay
unchanged. Missing live authority leaves authority-requiring operations pending or
refused. There is no public ID/generation rebinding. This is not implemented by P2.1.

### D. Narrow native ordering/paging contract (P2.1 implementation admitted)

Allocate each future repository sequence as checked `MAX(seq)+1` under SQLite
IMMEDIATE in the same transaction as insertion; return the persisted BoardMessage
only after COMMIT. Reuse existing `ix_board_message_repo(repository_key)`; it bounds
the aggregate to the repository, not an O(1) max lookup. No schema-version bump,
new index or migration. In memory the store-wide lock covers max and insert.

Keep existing IDs and historical duplicate Seq values. Add
`AppendBoardMessageAllocated(BoardMessage)` returning the allocated row to
`IWatcherObservationStore`; a default may throw NotSupportedException, never
fallback to allocation outside a transaction. Supported stores/proxies implement it.
Existing service signatures stay unchanged but return the allocated result, not
the proposal. All updated reliable writers use this path instead of count+1.

**Compatibility exception:** retain legacy void `AppendBoardMessage` as explicit
caller-sequenced seed/import insertion. Existing tests seed gaps and duplicates and
SQLite inserts the supplied sequence. Delegating that method to an allocator would
rewrite historical order; therefore it does not delegate. Legacy writes are outside
the new reliable cursor guarantee. No unqualified binary-compatibility claim.
The public Seq/BoardEntry/sinceSeq contract is Int32; checked overflow refuses before
insertion, including a raw SQLite Int64 maximum. Do not widen the wire silently.

MCP reads with `sinceSeq` return the **earliest** qualifying N ascending; use the
last returned sequence as the next cursor. Without `sinceSeq`, preserve recent-N
behavior. Do not claim completeness for historical ties, native tombstones or the
publisher snapshot. Public capability checks, production authorization DENY and
enhanced append disabled remain unchanged.

### E. Evidence boundary and class control

The supplied unmodified seven-case C#/SQLite receipt is 2 PASS / 5 RED: serial
sequences 1/2 pass; R3 has B allocate 1 and wait, A commit 1, reader cursor 1,
B commit 1, then reader `>1` empty. The relevant defect class is allocation outside
the durable consistency boundary; count is not the maximum and a service lock is
not a store lock. Sweep: the service count+1 producer, both store implementations,
the scheduling proxy and MCP newest-N paging form this narrow change surface.
Derive ordering in the store; prevent it with hole/max/overflow/concurrent-store,
reader-between-commits and >200-page tests. The R3 preinsert proposal equality is
incidental to the old bug and must not constrain a store-allocated result; preserve
its actual reader-between-commits oracle and exact committed fact identity.

R1 duplicate pump and R2 restart/ended/generation/heartbeat remain mandatory later
P2 REDs. P1 `535b` independent PASS was reported but is not joined here; do not copy
its source. Independent C#/Data/DS review of the implementation is still required.

### P2.2 bounded author checkpoint

The first independently testable increment is the additive v8 receipt aggregate in
the actual SQLite constructor. Its grain is one immutable captured native source
occurrence, one immutable admission/transition receipt, and one accounted prefix
per stream/epoch. The current pointer is the only mutable event state; mappings
remain in immutable receipts. This increment does not activate a reader or infer
an observation mapping from existing history. The native pump remains explicitly
unqualified until the subsequent typed-effect and bounded-capture increment lands.

The partial class separates only the coordination cache SQL from the existing
watcher store; it is not a new store, database, authority, or generic transaction
framework. Raw-SQL fixtures admit inert records only; there is no production cache
admission API or callback seam in this checkpoint. A receipt-only test is not proof
of atomic native effects. The SQL error tokens `COORD_EVENT_IDENTITY`,
`COORD_EVENT_IMMUTABLE`, `COORD_PAYLOAD_IMMUTABLE`, `COORD_RECEIPT_IMMUTABLE`,
`COORD_TRANSITION_REFUSED`, `COORD_CHECKPOINT_GAP`, and
`COORD_CHECKPOINT_IMMUTABLE` identify the respective schema refusals.

Four real SQLite R1/R2 REDs were observed on `5bb8bf21` before source changes.
The finite author checkpoint is schema/receipt constraints plus exact replay and
conflicting-position refusals. If the author budget ends at this checkpoint,
R1/R2 and native source-effect-checkpoint atomicity remain blocking—not waived.

### P2.3 native runtime implementation contract

The legacy SQLite pump now uses the trusted composition's normalized log-directory
root, normalized file path, and fixed `legacy-native-1` logical epoch. Base64 UTF-8
components separated by `|` are injective; neither timestamps nor payload repository
attributes identify the source. Capture is bounded to 128 files, 32 MiB total,
64 KiB per complete LF record, and 128 records/4 MiB per transaction page.
Only complete records are eligible. The accepted prefix is verified once against
the immutable capture; each writer reloads the checkpoint and discards stale pages.
This is optimistic validation, not linearizable filesystem exclusion or ABA detection.

Prepared records carry only bytes, typed events, binding data and timestamps.
The trusted composition supplies existing ID factories separately; they allocate
only under SQLite IMMEDIATE, after receipt/key lookup. They do not call services
or registrars and cannot be selected by payload. Initial pending receipt → event →
native observation → terminal receipt → checkpoint commit is the write order.
Internal enum fault points bracket commit; no public callback or payload switch exists.

Registration/session/end/heartbeat and valid board content are observations, not
authority. Durable registration receipts retain the external/internal mapping.
Post-commit memory holds only SessionRecord, never RegisteredSession/capability.
Public Post/Reply/Acknowledge authorization remains unchanged. Authority-dependent
episode work and late parents stay pending with their source bytes; they are not
quarantined as completed. Direct legacy Apply retains its public signature and
recognizes an exact previously captured registration by typed value, without
inventing source identity or re-registering historical sessions.

The runtime counters and last diagnostic report capture volume, replay, stale
snapshot, pending/refused dispositions and elapsed duration on each normal pump.
Fault/restart tests must read all three cache tables and native effects through
independent SQLite connections. This author checkpoint does not clear full P2,
canonical bridging, parent retry scheduling, P3–P5, activation, or independent gates.

### P2.4A — approved bound cache-reader contract (2026-09-16)

This is the read-only A unit, not pending recovery B. Data/Distributed Systems
design approval is conditional on the source contract below; independent
implementation review remains open. Production authority is DENY; enhanced
canonical append stays disabled. No producer is activated by this unit.

The same three caches remain fresh, pre-release schema v8. This is not an upgrade
of an existing candidate-v8 database. Released v7 remains the migration floor;
actual released-binary rollback qualification is later work.

One checkpoint is one normalized source scope/epoch and its accounted prefix.
Add `bound_repository_key`, `source_origin`, `public_source_id`: either all NULL
or all nonempty. The repository uses the existing canonical identity; origin
comes from trusted composition; the public ID is a deterministic SHA-256 of the
existing normalized scope, uniquely constrained. The hash hides paths, not
authorization. Scope, epoch and binding are immutable, including NULL→bound.
No attribution backfill, rebinding or collision merge is permitted. Legacy native
inserts default to all NULL and remain observational; public reads return
Unavailable for them.

The new trusted pump overload receives repository and origin before capture.
It establishes a bound zero checkpoint before admitting the first records,
including an existing empty source file. Zero requires the empty-prefix digest
and no accepted source content. Contradictory registration repository data refuses
the capture without effects; registration never supplies source trust. Existing
source-gap, contiguous-advance and replacement prohibitions remain in force.

The additive `IWatcherObservationStore.ReadCoordination(CoordinationReadRequest)`
returns a typed result; unsupported implementations default to Unsupported,
never empty success. A request has an opaque source ID, versioned source/epoch
cursor, after-N, optional frozen-H and a limit of 1–200. Its internal reader
repository is derived from the service's SessionRecord. MCP exposes only source,
cursor and pagination; it cannot override repository or name a root/scope.

One read transaction resolves and validates the checkpoint/binding before any
feed query, including empty results. Unknown/unbound is Unavailable, a different
reader repository is Mismatch without disclosure, malformed cursor/limit is
InvalidRequest, and wrong version/epoch is Reset. Nonzero H must be a receipt
of this source/epoch, not a different source's global receipt. Capture the local
maximum N and read ascending `after-N < n <= H` using `(scope,epoch,n)`.
All admissions, transitions, duplicate-accounting diagnostics, refusals and
tombstones travel as immutable receipt outcomes, not mutable current-event state.
Global sequence interleaving is not loss evidence.

Return actual last-returned-N (unchanged on empty), frozen H, continuation retaining
H until complete, then no continuation and FreshResume after H without frozen H.
The 401-row oracle is 200/200/1, followed by a new transition visible on FreshResume.
Metadata includes admission/session/message identifiers and source ID/origin only;
no source root, raw scope, payload or prose. Stable reason codes are allowlisted.
Availability describes the cache read only. Source health, recovery and lag are
NotRecorded, not fabricated zero or recipient health. I/O/busy fails typed
Unavailable. Normal reads emit duration, volume and stable outcome via a span.

Surface/proof matrix: trusted binding → checkpoint constraints → transactional
receipt reader → interface → BoardTools adapter → existing Tools schema/dispatch.
Real isolated SQLite fixtures and actual MCP dispatch prove the seams; old native
BoardTools.Read and producer behavior retain their tests. Structural source-binding
proof is not human-channel qualification. Recovery eligibility/counter semantics,
full P2, producer/canonical bridge qualification, actual binary rollback, and
P3–P5 remain outside this unit.

### P2.4B — finite recovery amendment and ERRATUM (2026-09-16)

**ERRATUM:** the earlier Data statement “eligibility and presence are booleans”
was wrong. `eligibility_generation` is a nonnegative NOT NULL INTEGER counter.
`payload_presence` alone is a NOT NULL 0/1 flag. `recovery_status` is NOT NULL
`none|active|deferred|exhausted`. Neither counter is live authority or a session
generation. Production authority remains DENY; enhanced canonical append is disabled.

The counter increases once per newly observed qualified dependency component:
original registration mapping and same-repository requested parent. Registration
then parent means 0→1→2; finding both together means 0→2 with no intermediate
budget. Seen components are cumulative. Repeated polls, disappearance/reappearance,
capacity and status changes never reset attempts. Each event/generation gets eight
attempts. Overflow refuses atomically. An eighth failed attempt releases both
payloads and retains the source obligation. Exhausted references receive bounded
eligibility probes, not absent-parent semantic retries.

The same three fresh unreleased-v8 caches carry the representation; this is not an
upgrade of existing candidate-v8 data. Released-v7 old-binary qualification remains
later. Each event retains source offsets, raw and canonical digests/version,
requested parent ID, seen components, attempt 0..8 and UTC due time. Feed receipts
snapshot the target application/recovery/eligibility/presence tuple. A NOT NULL
deferred composite current-receipt FK includes that entire tuple. Terminal tag is
`none`, never NULL; diagnostic application state is NULL and cannot be current.
Optional same-scope parent FKs remain optional; a cross-stream board parent is
resolved by same-repository board ID, including legacy facts with no receipt.

Within BEGIN IMMEDIATE, recheck expected receipt/generation/binding/dependencies/
capacity. Insert a transition whose trigger advances **only current_receipt_n**;
old state/tag/payload tuple remains locally valid. One guarded UPDATE installs the
complete target tuple and both payloads. Staging requires a settled old tuple;
another transition while unsettled refuses. Missing finalization or half-null
payloads fail commit. Payloads unchanged by a transition stay byte-identical.
Only source-validated pending references can rehydrate; terminal resurrection
is forbidden. Operational attempt/due changes preserve the settled tuple.

Ordinary admissions retain at most 1,023 pending pairs and 16,646,144 bytes.
One slot plus 131,072 bytes is reserved for verified-ready reference rehydration
and application in the same transaction (absolute 1,024 / 16,777,216 bytes).
Blocked reserve activation rolls back. Overflow admits a visible deferred
reference plus pending receipt and checkpoint, without payload copies. No
alternative pending JSON list, new queue, historical DROP or dedup is permitted.
Capacity derives from indexed bounded active rows, not competing counters.

Each complete pump pass has 64 attempts (32 dependency-ready, 16 deferred, 16
ordinary-due) and 256 metadata examinations (128/64/64). Each lane has fixed
checkpoint columns for last-examined admission, frozen high-water and last-served
logical turn. Least-served source then scope determines service. Blocked probes
advance; wrapping starts next pass. Borrow only after every lane has opportunity.
Attempt keys are deduplicated per pass. Source capture verifies one bounded
32-MiB root snapshot per pass; recovery reuses its validated ≤64-KiB records.
Source deletion/GAP/mismatch keeps the obligation and reports a typed failure.
Native effect, transition and recovery checkpoint commit together; observation
publication follows commit. No recovery registration callback, capability mint,
heartbeat or end refresh is allowed.

Surface list: cache DDL → inert recovery rows → existing ProjectRecord/native SQL
→ pump pass boundary → post-commit Observe → actual pass counters and tests.
Feed A historical outcomes remain immutable; no invented recovery-health zero.
TimeProvider-derived UTC is scheduling only, with no unconditional SLA.
Independent Data/DS/Test implementation review remains required.

#### B counter/measurement implementation clarification — 2026-09-16

Eight means the combined initial `ApplyObservation` call and subsequent recovery
calls in that eligibility generation. A successful initial admission records one;
a deferred reference that actually called the method also records one. Metadata
selection, absent-dependency probes and exhausted-reference probes do not call it
and do not consume semantic attempts. No polling or capacity change resets the count.
If reserved activation returns pending, roll back the payload/native transaction
and persist the observed attempt and any qualified generation change with cursor
progress in a separate transaction. A thrown transaction leaves its event attempt
and cursor uncommitted; the pass measurement still counts the attempted call.

`LastRecovery.Status` distinguishes `NotRecorded`, `Completed` and `Failed`.
The normal `coordination.recovery` Activity and the pump result expose the observed
examinations, attempted calls, committed applied effects, elapsed milliseconds and
stable error code. A partial failure rethrows; it is not converted to success.
`COORD_RECOVERY_FAILED` covers non-source failures; existing source error codes
remain exact; `COORD_RECOVERY_COUNTER_OVERFLOW` denies logical service-counter
overflow before cursor movement. Raw payloads never enter these tags.
Logical last/high/served columns are nonnegative SQLite integers; UTC scheduling
remains a signed integer timestamp, not an eligibility flag or a logical clock.
This is clarification within B, not independent approval or a new permission gate.

### P2 native registration admission and notice delivery correction — 2026-09-17

This supersedes any suggestion that an in-memory producer preparation or a drained
notice constitutes durable native admission. The user supplied conditional Data/
Security/Distributed Systems code admission for a **dormant, synthetic-only**
implementation. It is not independent code approval or permission for live
sensitive admission. Canonical official-API qualification and production authority
remain ENHANCED DENY. Native file publication is not semantic acknowledgement,
consumption, a human assertion, or a work-area grant.

**Model and durable representation.** The existing watcher database gains exactly
two tables in additive version 9, based on this branch's fresh-v8 cache layout:

* `native_registration_admission_fact`: one accepted trusted-ingress operation,
  identified by operation ID. Its immutable decision records the input/context
  digests, original supplied repository before normalization, corrected repository,
  typed reason, session/generation and minimal frozen binding. The original claim
  exists here once. A source observation is not human authority.
* `registration_notice_delivery`: one correction obligation per admission,
  identified by stable notice ID, with admission/version-digest foreign key,
  immutable derived publication bytes/digest and typed native target. Only
  Pending/InFlight/Published delivery metadata changes: attempt, due time,
  owner and ownership version. Publication is monotonic; identity and bytes are
  not. No bearer/capability, arbitrary attributes, transcript or source prose is
  persisted. Counts derive from rows; no competing persistent quota counter.

All attributes are immutable history except the delivery scheduling cells.
Operation count is additive; outstanding-count snapshots are non-additive across
time. Publication bytes are a frozen derived projection, not a second source;
the future admission writer must prove serialization equality and digest equality
against the admission before commit. SQL structural validation alone cannot prove
SHA-256 equality. No historical reconstruction, DROP, fake coordination-event
offset, v7-table trigger, new database, or invented durable store identity.

**Trusted ingress and atomicity.** Add an enhanced entry point; leave legacy
writers usable and explicitly unqualified. Capture bounded typed input under
composition-owned local roots before filesystem access. Reject UNC/network,
device/traversal, reparse escapes and forged `.git` pointers selecting a root.
Neither payload nor environment establishes trusted root binding. Reuse the
actual host's trusted workspace context; introducing configured binding requires
an explicit composition contract, not inference.

Within registrar → store lock order, serialize terminal adoption and expected
generation CAS. One store-owned IMMEDIATE transaction writes the admission,
notice, session upsert, end clearing and heartbeat. Prepare an opaque capability
without exposing it; install/return only after confirmed commit. Same operation
and input/context returns the original admission; conflicting reuse fails.
Unknown commit outcome reconciles the operation ID, never re-registers, bumps
generation or mints authority. Enhanced Heartbeat/End/UpdateHarnessAndModel
validate capability and stored expected binding/generation in the protected
operation. No whole-Core auth rewrite; legacy v7 ABA/uncooperative writes are not
claimed fenced.

**Capacity and ownership.** Reserve before any registrar mutation for a correction.
The process-global 128 bound includes reserved, queued and in-flight obligations
across hosts/store handles. Full returns `COORD_NOTICE_CAPACITY` with unchanged
facts, generation, capability and membership. Hydrate known Pending/InFlight
before fresh enhanced admission; excess recovered backlog blocks new enhanced
admission, not legacy use, and never deletes facts. Identify the actual store,
not a path spelling. Any new durable identity requires a prior recorded decision
and real-engine spike. One enrolled enhanced owner/publisher per store uses OS
exclusion; this is not a global fence against legacy clients. Failed precommit
reservation releases; committed Pending remains until publication reconciliation.
One bounded fair due batch avoids poison-head starvation. Stale completion fails
ownership CAS; cancellation after commit retains the obligation.

**Native publication.** Keep `registration/` compatible. Install an immutable
safe notice-ID filename via unique exclusive attempt temporary file, flush and
atomic no-overwrite move. Existing full bytes and digest equal means idempotent
success; difference means conflict, preserving the original/history. The
session-keyed JSON stays a derived latest snapshot, never consumption authority.
Late older replay cannot replace the newer known snapshot. File success plus lost
DB acknowledgement retries the same ID/bytes, not another consumed file. Native
success does not complete any canonical target. Recovery only publishes notices;
it never invokes registration/lifecycle/capability issuance.

**Migration and rollback.** Enforce fact update/delete/REPLACE refusal; immutable
notice identity/bytes; monotonic publication; explicit SQLite type, null, length,
key and numeric bounds; session-generation uniqueness; and due-selection index.
Pending selection is bounded to at most 129 by the future reader, not by deleting
excess history. Validate constructor migration abort atomicity on real SQLite.
Rollback disables enhanced entry points/worker and retains new facts. Released
old-binary compatibility and prerelease-v8 variant upgrades are not claimed.
Live retention/erasure remains BLOCKED.

**Change reach and proof boundary.** Trusted host input → correction → protected
registrar preparation → store transaction → durable notice → claimed attempt →
native publisher → optional existing Workbench notice dispatch. Only the dispatch
section may change; no layout/style/Atlas work. Actual host/native N1/N2, per-write
fault rollback, operation replay/conflict, capacity across handles, late stale
lifecycle CAS, immutable bytes and retry/restart are required. D0/D1/D2/D4/D6
apply. Normal telemetry emits opaque IDs/counts/codes/duration, never raw paths
or prose. Core/native-file tests do not replace P5 rendered-surface qualification.

The bounded author increment first supplies the **dormant DDL floor** and real
constructor/SQL tests. It deliberately exposes no admission or delivery API and
does not connect a worker. N1/N2 remain semantic RED until the complete protected
pipeline is implemented. No storage-unit green is a host-path or P2 green.

**S1-S3 schema clarification (2026-09-17).** Fixed-hex digest/notice identifiers
require explicit NUL exclusion and exact UTF-8 byte length as well as TEXT,
character length and lowercase-hex shape. The shared fresh/v8-to-v9 DDL now
enforces these. Existing v9 files are not retrofitted; prior prerelease-v8 variants
and released old binaries remain unqualified. See
[the S1-S3 proof](../proof/p2-notice-schema-s123.md) for byte-exact parameter
fixtures, isolated mutants and populated-v8 rollback snapshots.
The fact-to-initial-notice child FK does **not** require any child to exist;
one admission plus its required initial notice must be enforced by the protected
writer transaction. Structural publication bytes/digest shape is not content
digest verification. ClaimReference payload-retention/erasure stays unresolved.

#### Protected native admission subset / actual API amendment — 2026-09-17

This unit implements the existing v9 contract, not another schema checkpoint.
`IngestHost.Register`, `TrustedRegistrar.Issue` and the legacy drain stay usable
and unqualified. New `RegisterNative` returns a `RegistrationAdmissionResult`;
unsupported public composition returns `COORD_NATIVE_UNAVAILABLE`, not legacy
fallback. There is no trusted workspace/terminal source in the current IngestHost
constructor. Consequently enrollment is **internal and synthetic-only** in this
unit. Its `NativeRegistrationContext` is supplied by the local test composition,
not read from attributes, environment, repository prose, or human-looking fields.
Default production enrollment and P3 human verification remain DENY.

`NativeRegistrationAdmissionRoot` owns enrollment and calls the actual registrar.
Context contains the existing WorktreeIdentity and TerminalIdentity plus a local
publication root. Validate raw attribute values before mapping or SQL binding:
bounded strings, no NUL or invalid UTF-16, no missing required values silently
manufactured. Hash the exact sorted API input; capture repository_sent before
RepositoryIdentity.Canonicalise. This is **direct API provenance**, not a
fabricated source-file offset. Repository identity is not treated as a path.
Correction uses the explicitly bound WorktreeIdentity.Repository, only when the
claim names that worktree. No payload-driven filesystem locator or `.git` read
is made. Local context paths are validated before checking their ancestors for
reparse points. TOCTOU-safe publication and real workspace enrollment are deferred.

One registrar gate -> process coordinator gate -> store gate -> SQLite IMMEDIATE
transaction encloses operation lookup, terminal adoption, generation selection,
admission/optional initial notice/session/end-clear/heartbeat writes and commit.
Use existing RecordSession(transaction), FindSession(transaction) and SQL helpers.
Prepare the capability inside that boundary but install it only after commit.
Exact operation/input/context replay reads the historical admission and returns
**no capability**. Replay neither reads current lifecycle to create a new receipt
nor renews a capability. A lost return after commit is reconciled by that lookup.
Conflicting reuse returns `COORD_NATIVE_OPERATION_CONFLICT` without overwrite.

The fact's minimal frozen binding does not contain repo display text. Derive its
display deterministically from repository_used; do not add a column or guess a
missing source claim. For an uncorrected claim, repository_used preserves the
same original identity string required by the v9 NONE constraint; the domain
projection applies its existing identity canonicalization. Immutable notice bytes
are a deterministic projection with a content digest. Only LINKED_WORKTREE
creates a notice and consumes quota. All enhanced lifecycle calls check the
installed expected SessionRecord against the stored record inside IMMEDIATE;
an update advances the expected record only after commit. Legacy writers remain
outside that fence; no v7 ABA or machine-wide uncooperative-writer guarantee.

Reuse the bounded reservation/disposable-lease idiom: one process-wide critical
section accounts reserved plus indexed Pending/InFlight counts across enrolled
stores before capability preparation. SQLite rows remain the only durable
authority. Windows file-handle volume/file-index identity, not path spelling,
keys enrollment; a byte-range lock on that file excludes another enhanced
process. Non-Windows enrollment is unavailable in this bounded unit. At most
128 enrolled stores; excess recovered backlog refuses fresh enhanced admission,
never deletes data or blocks legacy APIs. Root disposal releases enrollment;
re-enrollment hydrates before admitting. No invented StoreId table.

Typed internal fault seams run only under the protected gate, after each write,
before commit and after commit/before return. They are inaccessible to payloads.
Tests must pin source/project/binary bytes before mutations and execute the
same focal assertions against capacity/replay/lifecycle/notice-byte mutants.
Legacy N1/N2 remain labeled diagnostics; new opt-in N2 is distinct.
Claim/Complete/Requeue and hardened N1 publication are deferred as a complete
transport unit; no old Publish success marks the new delivery row Published.
Canonical primary.requests emission stays disabled. No UserAuthority DTO field.

### NativeNoticeN1 transport contract — 2026-09-17, before implementation

This bounded unit implements native correction publication, not the canonical
coordination stream. One existing delivery row is one accepted admission's
immutable native notice. No new table, queue, consumed file, or status authority.
The v1 publication bytes already frozen by admission are the wire version; they
are never reconstructed using a retry time or a new ID. The v1 shape retains
generatedBy, noticeId, sessionId, repositorySent, repositoryUsed and fixed reason.
The existing schema_version=1 selects this codec. Admission history is unchanged.

Immutable files are `registration/notices/n-<32 lowercase hex NoticeID>.json`.
The ID is validated before concatenation. Each attempt exclusively creates a
random temporary file in the same directory, flushes to disk, then installs by
atomic no-overwrite move. Existing full bytes AND digest equal means replay;
unequal means COORD_NOTICE_CONFLICT, preserving both target and source. Publication
roots come only from internal trusted composition, never caller notice prose.
Validate bounds and local absolute paths before filesystem access. Reject UNC,
device/traversal forms, `.git` components and reparse ancestors. Validate the
derived legacy filename too, before any filesystem operation: sanitizing
characters does not remove Windows device names. Pin publication directories
against rename during the effect on Windows. This does not mint
authentication or a capability from a path; hostile ACL/root enrollment remains
the production binder's unqualified responsibility.

The session-keyed JSON stays a derived latest projection, never an acknowledgement.
Choose its bytes from all accepted notices for the same session and root, ordered
by generation, including pending notices. Serialize publishers per enrolled
physical store; an older retry cannot replace a newer known publication. Install
the compatibility projection with a unique exclusive temporary and atomic replace.
TransportPublished requires confirmed immutable bytes and confirmed selected
latest bytes. It does not mean model arrival, triage, acceptance, or grant.

Reuse the Windows physical-file enrollment lock and a per-enrollment publication
mutex. Pending -> InFlight increments attempt and ownership_version; completion
and requeue compare notice ID, owner, attempt and ownership_version. No TTL proves
death. Recovery runs only while holding the enrollment's OS owner lock and
publication mutex: a previous worker in that enrollment is then quiescent, and
a previous process cannot own the OS lock. Recovery preserves bytes/ID and advances
the row version. A bounded snapshot of up to 32 candidates, ordered by attempt then
due time and ID, prevents one poison item from monopolizing retries. Cancellation,
file failure and lost database acknowledgement leave Pending or recoverable
InFlight. Reopen a real store and reconcile exact file bytes; never re-register.

Global capacity remains reserved + Pending + InFlight across retained enrollments.
Only successful publication acknowledgement removes an obligation from that count.
The retained read-only counter observes Published; quiescent retirement then closes
its reader and OS owner handle. Read failure is Uncertain, not zero. Publish attempts
emit status, attempt count, elapsed milliseconds and aggregate counts without raw
paths or prose. A default public IngestHost worker call is explicitly Unavailable;
an internal enrolled worker overload is executable by synthetic composition only.
No actual MCP/UI activation is claimed; legacy Drain retains its documented loss
semantics and its two RED diagnostics. No App helper or GUI is introduced merely
to make unavailable composition look reachable.

New cross-harness requests/dispositions must use the official primary.requests API.
Native success never completes an unqualified canonical target; canonical writing
remains disabled. Rejected alternatives: callback-as-durable-ACK, serialized
callbacks, destructive drain, TTL reclamation, per-session immutable filenames,
overwrite-on-conflict, and a second status store. Independent Test/Data/Security
review follows this author unit; full P2 recovery/binder/retention and P3-P5 remain.

Correction during implementation: limiting latest selection to Published plus
current could regress a newer file whose database ACK was lost. Accepted facts,
not delivery status or a filesystem document, now select latest. A pending
newer accepted correction may therefore appear in the compatibility document;
that appearance is not an acknowledgement of its immutable transport.

### Finite publication repair — 2026-09-17 author contract correction

The compatibility filename is an identity boundary, not a lossy display mapping.
Enhanced admission accepts only session IDs made from lowercase ASCII letters,
digits, hyphen and underscore, at most 250 characters, with reserved Windows
device names refused. This finite domain preserves ordinary UUID/session filenames
and excludes replacement, case-folding and trailing-dot aliases. Validation occurs
before capability preparation and transactional admission writes. Existing historical
rows are not renamed, backfilled or deduplicated. Refusal is `COORD_NATIVE_CONTEXT`.
Existing compatibility JSON naming another session is `COORD_NOTICE_CONFLICT`,
checked before enhanced admission and again before publication.

The public legacy helper remains a writer with its existing JSON and filename
mapping. It must refuse a known owner mismatch rather than overwrite another
session. Both writers reuse the Windows-local checked ancestor/registration
directory pins and exclusive unique temporary-file installation. No directory
reparse point may be followed; no global ACL or permission changes are permitted.
This is a qualified Windows-local guard, not a cross-platform or hostile
cross-process file-race guarantee. Native file publication is transport evidence,
never canonical completion or human authority.

Surface list: trusted session-ID selection → admission guard → retained fact/notice
→ immutable file and compatibility JSON → legacy reader. No payload/schema change,
App edit, binder activation or canonical bridge is part of this repair.
Oracles use synthetic local files, the exact `a:b`/`a?b` pair, a registration
junction, same-owner/same-attempt wrong-version completion, and failure of the
retained independent accounting reader after 128 accepted obligations.
The existing worker-store disposal test does not establish that last property.
Independent Test/Data/Security re-gating remains required.

### P2 COMPAT portability repair — 2026-09-17

The Windows-only guard on **public legacy** `RegistrationPublisher.Publish` is
a regression, not an approved compatibility break. The two existing unguarded
ordinary-publication tests failed on Ubuntu 24.04 / .NET SDK 10.0.303 at
`194863021d030faef6fb20a9f7bc385fb0c8d260`, both with
`COORD_NATIVE_UNAVAILABLE`. Their JSON and filenames must remain unchanged.
The enhanced production binder and `PublishNative` remain Windows-qualified;
restoring this helper does not admit, complete, triage, or grant an operation.

Reuse the existing serializer, filename mapper, owner parser and error codes.
Add only a Linux directory-descriptor scope: open `/`, walk each validated
component with `openat(O_DIRECTORY|O_NOFOLLOW|O_CLOEXEC)`, create missing
components with `mkdirat`, and retain all handles through installation.
Within the pinned registration directory, take a nonblocking `flock` to serialize
cooperating publishers, check existing exact session ownership through a
no-follow read, create a unique exclusive 0600 temporary file, flush it, and
install relative to that same descriptor. For an absent target, `linkat` must
fail rather than overwrite a concurrent creator. For a same-owner latest
document, `renameat` atomically replaces the directory entry, never follows it.
Remove the temporary entry with `unlinkat` and close every handle on failure.
No new package, persistent lock file, global permission change, or schema.

The Linux x86_64 spike executed these contracts on the session's WSL filesystem
(the measured `stat -f` type is `ext2/ext3`, not DrvFS);
Linux man-pages `open(2)`, `rename(2)`, `link(2)`, and `flock(2)` describe their
signatures and semantics. Constants were measured from installed Python's `os`
and `fcntl` modules and exercised through libc 2.39, not copied from memory.
Reject NUL, relative paths, device/UNC forms and filesystem-root publication;
a single-leading-slash Linux absolute path is not UNC.

Scope is **pinned directory identity**, not a promise to prevent an owner of an
ancestor from renaming that directory elsewhere. Replacing a path component with
a symlink does not redirect descriptor-relative writes to the symlink target.
The advisory lock does not fence arbitrary noncooperating writers. macOS,
other Unix kernels and non-x86_64 architectures are unqualified and refuse;
do not apply the measured Linux x86_64 ABI constants to them.
No macOS support claim was found in the consulted architecture/README.

Surface list: public caller root → existing validation/serialization → Linux
descriptor scope → exact-owner check → atomic latest file → unchanged ordinary
reader/pump exclusions. Windows native immutable bytes, monotonic latest
projection and production admission are untouched.

Execution graph: baseline/API spike → this contract → retained tests/repair →
Linux controls/mutants → Windows regression → evidence/commit. These are data
dependencies; builds are serial to avoid shared output contention. Six modeled
equal-cost nodes give work/span 6/6 (Inferred), width one, zero agents. Fixed
oracles cover outside-symlink writes, conflicting ownership, no-overwrite
creation, descriptor cleanup and pinned-directory replacement; stop when each
is observed or explicitly qualified. Independent COMPAT review is the next gate,
not a claim the implementation author can clear.

### 2026-09-17 Data continuation: bounded canonical-byte experiment

The current Data choice is a **conformance-locked C# reader of the P1 definition**,
not a second writer, fold or status authority. Canonical `.agents` capture remains
the single source of truth. No Python-per-UI-read dependency is admitted.
`spikes/canonical-coordination-contract/` is experimental tooling only. It is
linked into tests, not Core; its probe references Core for existing identity
conversion. This does not admit a bridge, source adapter, migration or live pump.

The pinned Python definition is commit
`ebd4f1c8473b70934ec29d778419289719ef5481`, `coord_protocol.py:104-108`,
`validate_fact:122-171`, and `coord-core.py:462-568`. The versioned golden fixture
records SHA-256 for the actual six imported source/support/test files. Python
fixtures are called directly; no transcript or live coordination data is used.
Byte parity is established for the retained corpus, **not the entire reader
contract**. The probe explicitly emits `fullContractQualified: false`.

Required bridge changes remain proposals:

* Select a physical primary checkout from trusted composition first. Official
  `repo_identity.canonical_project` returns a display/project-name string;
  native `RepositoryIdentity` carries canonical path and display name.
  Synthetic distinct repositories with equal remote basenames return the same
  project name. Neither that name nor an event's `repositoryId` may select a store.
  The trusted mapping to the canonical event repository ID is still unqualified.
* Use an origin-qualified, versioned scope containing repository, origin,
  physical source path and epoch. Four strict UTF-8/base64 components separated
  by `|` are injective before hashing: base64 cannot contain that delimiter.
  The 6,561-tuple test exercises empty values, delimiters, NUL and Unicode.
  Filesystem normalization and opaque/public-source-ID server mapping are separate;
  the test is not a claim that physical and public identities coincide.
* P1 accepts up to 65,536 input bytes **excluding** CRLF. Canonical-origin capture
  must retain up to 65,538 raw bytes, including CRLF, without silently dropping
  a valid record. Existing native capture/cache guards include LF and remain
  unchanged at 65,536. Propose origin-qualified raw limits and corresponding
  additive cache constraints; do not widen all native/publication guards.
  Responses already bound validated canonical bytes to 32,768. Facts bound their
  full Python reserialization to 65,536, which also bounds their smaller compact
  semantic representation. Do not infer an unbounded validated-output problem
  from an unchecked serializer expansion.
* Complete P1 envelope/error parity before a reader can be called qualified:
  exact fields, all five event types, endpoints with **string** generations,
  schema integers, timestamp and sequence rules, proposal/authority shapes and
  origin-specific bounds. The byte codec deliberately does not implement these
  admission rules. The retained invalid ledger makes this missing surface visible.

Independent review of the new generator, numeric normalization, parser and tests
is next. The full Proof Pack records the byte evidence and remaining errors.
No runtime clearance follows from this experiment.

### P2 validator and synthetic binding unit (2026-09-17)

The user supplied independent Domain/Test **CODEC-scope PASS** for `d8ce5a94`,
not a full reader qualification. The oracle remains **ebd4f1c8473b70934ec29d778419289719ef5481**
even if the P1 timestamp-fix branch advances. This supersedes only the preceding
experimental-code placement: move the one codec definition into internal Core and
link that source into the spike. Core tests use their existing friend assembly.
There is no public arbitrary-JSON API and no runtime Python dependency.

The finite contract for this unit is:

* Input is one inert UTF-8 record. Check the 65,538 raw-byte ceiling before copying
  or parsing, strip at most LF then CR, and check 65,536 content bytes. Reject
  duplicate decoded keys, invalid UTF-8, unpaired surrogates, nonfinite numbers,
  container depth over 16, and malformed JSON. A node ceiling of 65,536 is derived
  from the byte ceiling (each JSON value occupies at least one input byte).
* Use System.Text.Json lexing and the pinned `python-json-v1-dotnet-r-v1` codec:
  scalar Unicode key order, no normalization, integer/float distinction, BigInteger,
  negative zero. Python 3.12.10 here has a 4,300 decimal-integer digit limit.
  Beyond that explicit adapter support boundary return Unsupported, not parity.
* Validate the exact 23-field v1 envelope and each subtype against the pinned
  Python functions. Endpoints carry two strings, never native integer generations.
  Response references deliberately retain P1's loose authority-list contract;
  fact references are exact text-valued shapes. This validates shape, not local
  blob integrity, issuer identity, proposal authority, or recipient generation.
  Those remain Unknown/Denied regardless of strings inside payloads.
* Responses bound digest bytes to 32,768; facts bound full Python default-separator
  reserialization, including digest and recordedAt, to 65,536. Only digest
  construction excludes the two top-level fields; validation still checks them.
  Timestamp integers outside finite binary64 return stable `XH.FIELD_INVALID`;
  this deliberately handles the pinned P1 direct OverflowError defect, without
  importing the parallel fix or changing the oracle.
* Malformed records return typed Invalid with stable codes. Unknown string kinds,
  event types and integer versions return visible Unsupported. Unversioned
  request-add/request-resolve bodies retain their original fields and bytes:
  source provenance is separate and cannot synthesize repository or generations.
* Synthetic binding accepts the existing internal CoordinationSourceBinding plus
  an explicit trusted primary path, checkout, public source ID and finite list of
  opaque repository/stream pairs. Membership is checked before filesystem access.
  Only the fixed primary `.agents/requests.jsonl` source is eligible. Resolve
  checkout membership from isolated Git metadata; never select a root using event
  fields or remote basenames. Reject reparses in the checked paths. This is
  snapshot validation, not a race-free live-file read or a publication capability.
* Return OriginBound/NotStorable when a valid canonical-origin record exceeds the
  existing native raw ceiling. Do not widen the native cache, add DDL, call the
  store or wire Main. Unknown or ambiguous source configuration is Unbound.

Surface list: byte input -> lexical codec -> schema validator -> immutable inert
record -> source membership/provenance -> typed admission result. Store, native
Main, UI, authority resolver and full response/proposal fold are deliberately
absent. Tests and the spike are the only consumers in this unit.

Verification contract: old whole-envelope goldens and five codec-accepted bad
envelopes; fresh whole-body Python positives/negatives; decimal parsing/full-byte
edges; exact line and output bounds; two isolated physical repositories with
equal remote basenames and a linked worktree; spoofed/unknown mappings and legacy
no-synthesis. A finite guard/binding fault-injection pass must fail. Normal-path
results expose status/code, raw/canonical sizes and duration without payload logs.
New-code independent acceptance remains separate from the supplied CODEC PASS.

Implementation note: this internal unit now exists in `CanonicalCoordinationCodec`,
`CanonicalCoordinationRecord` and `CanonicalCoordinationSourceBinding`. Its only
consumers are the retained spike and focused tests. Raw source membership is a
snapshot check, with no held-handle live-read claim. The Windows test uses a
junction (a real reparse point), not a request to enable symbolic-link privilege.
Full Python size boundaries are generated by the pinned oracle, not estimated
from a number of references. See the existing canonical Proof Pack for observed
passes, repair runs, receipt paths and the still-unqualified integration surface.

### Finite reciprocal binding correction (B1/B2, 2026-09-17)

The supplied independent counterexamples refine the existing snapshot-membership
contract, not its authority. A linked checkout must have two agreeing directions:
its bounded `.git` forward pointer selects an immediate admin directory beneath
the trusted primary `.git/worktrees`; admin `commondir` resolves to that primary
`.git`; admin `gitdir` resolves back to the supplied checkout's `.git`.
Resolve relative forward pointers against checkout and relative backlinks against
admin. Compare the already resolved backlink with the supplied checkout using the
existing filesystem comparison contract. A mismatching backlink is rejected
before accessing its target. Revalidate reparse-free path components before each
bounded metadata read; empty, malformed, oversized or unavailable metadata cannot
manufacture a bound source.

Do not send resolved metadata back through `FileSystemRepositoryLocator`'s
legacy lexical parent inference. That helper's other callers and behavior remain
unchanged. Reuse the existing bounded pointer reader, guards, error codes and
physical trusted primary path; no locator framework or native root-hash change.
Mutual pointer consistency remains a snapshot check, not ownership authentication
or a race-free held-handle capture. Event repository/stream values never select
the source root or a store. The official primary requests file is the only output.

The canonical Proof Pack records exact B1/B2 red/green, three focal mutants,
Windows real-Git fixtures and unchanged pinned parser/corpus evidence.
Independent acceptance, live capture, origin-qualified storage and full P2 remain
separate pending gates.

### Official store v10 admission contract — 2026-09-17

The user's independent binding/codec and comparator clearances admit this dormant
synthetic store work. They do not clear a new implementation. Comparison means
validated canonical **bytes**, excluding only payloadDigest and recordedAt after
validation, not their hash. The ebd4f1c oracle stays pinned.

Add version 10 through the existing constructor migration from verified v9.
Fresh creation composes the same migration. Do not rewrite v8, rebuild tables,
delete rows, change the native 65,536-byte CHECK, or tighten native payload
presence rules. Add event source_kind (native by default), official_raw_bytes,
official_canonical_bytes and official_identity; checkpoint official_binding;
feed occurrence_kind. These are additions to the existing three caches only.

The existing v9 predicates admit inert terminal rows with absent native payload.
Retain those predicates verbatim and conjoin new origin-partition guards; no
replacement of existing triggers is necessary. Native rows have NULL official
columns. Official events require a pre-existing bound checkpoint with matching
scope/epoch, canonical origin and nonempty native repository, native payload
presence zero, terminal applied/refused state, no recovery, attempts, eligibility,
seen components, due time, native original-ID or requested parent. Official feed
rows have no native session/generation/message/parent mapping. Initial receipts
retain the old inert parent_application_state='applied'; occurrence diagnostics
retain both parent columns NULL and never move the current receipt.

Official raw bytes are complete LF frames: content at most 65,536 bytes, raw at
most 65,537 with LF or 65,538 with CRLF, and exact end-minus-start length.
Interpreted canonical bytes are BLOBs at most 65,536 bytes. Refused input may be
raw-only; raw bytes are never a substitute for validated canonical bytes.
Official payload, full identity, source kind and binding are immutable. Full
tagged identity includes native repository/origin plus the complete protocol key,
or a separate legacy/invalid physical-occurrence key. Uniqueness uses the full
BLOB, not a hash. SQL checks structure/provenance association, not schema/digest
validity or authentication of the composition that supplied a descriptor.

One future descriptor contains native root, physical primary, fixed file, origin,
codec/allowance versions and every allowed opaque pair sorted by encoded bytes.
Scope/public ID derive from all descriptor bytes, with full-descriptor collision
comparison; no NULL-to-bound upgrade. Actor payload cannot select that descriptor.
The future ProjectOfficialPage must acquire IMMEDIATE before lookup, validate
binding/CAS/captured-prefix/framing, create an empty checkpoint before admission,
and atomically account contiguous occurrences before commit/return. Equal identity
and comparison bytes reuse admission; unequal bytes emit a Conflict occurrence
receipt without changing original state. Applied means ingested, not acceptance.
No native allocator, admission, capability or recovery call is allowed.

**Bounded checkpoint for this turn:** structural v10 partition/migration and
metadata-only occurrence reader. Capture, full descriptor/key construction,
ProjectOfficialPage, replay/conflict classification and 401-record projection
remain unimplemented if the 45-call ceiling is reached. This is deliberately
not a canonical bridge completion claim. Existing source binding remains
snapshot-only and NotStorable. Official writing and privileged authority remain
Unavailable/Denied; no live importer is connected.
