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
