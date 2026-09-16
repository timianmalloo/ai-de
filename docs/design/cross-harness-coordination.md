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
  references and a single official request stream. Specifies conditional additive storage,
  atomic replay/cursor floors, failure/privacy controls and unperformed phase gates.
---

# Canonical facts, not another approval database

**T2 / Peer Mode / P0 DRAFT. No independent PASS; no executable schema chosen or applied.**
Conceptual model first: [spec Part A](../specs/cross-harness-coordination.md#2-part-a--conceptual-domain-model).
This design settles the safety contract; P2 must confirm physical placement and execute
red tests before admitting a migration. DM1–DM18, Testing Strategy D0/trigger union,
Observability O1–O13 and the Solution-Selection Ladder were read from `.claude/knowledge/`.

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

1. Pin old payload corpus and old CLI behavior. Add tolerant readers and new actionable
   read mode first, default off; unknown new fields must not break legacy records.
2. Keep `request-add` / `request-resolve` and legacy list semantics intact. New typed
   response events must not pretend to be old resolution/approval. Old readers may
   conservatively leave enhanced threads open; they cannot report enhanced completion.
3. Upgrade **every active canonical writer** to the shared append serialization and
   capability checks while retaining its old command surface. An unupgraded writer
   does not magically honor a new lock. Unenrolled writers block new-mode activation;
   expose compatibility/availability, never infer enrollment from source on disk.
4. Golden tests cover old reader/new stream, new reader/old writer, concurrent legacy
   command/new command, missing optional fields and unknown event types. New writer
   activation requires explicit capability inventory, verified fallback and review.
5. Disable new writer first to roll back; retain readable canonical events and IDs.
   Roll back projection/read mode separately. Never revive an older writable status copy.

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
   revocation/supersession basis. P1 Security must establish the actual supported
   channel and verification mechanism; no home-grown signing dependency is proposed.
5. Check current revocation/supersession at turn boundary and immediately before
   irreversible action. Historical valid evidence stays historical; a stale grant
   cannot authorize a current action. Unknown verifier/channel/generation fails closed.

The implementation approval for this programme is not itself a verifier-issued
machine capability. P0 contains **no authenticated test grant**. Authority adapters
remain blocked until independent Security evidence exists. Raw repo/peer prose is
never promoted into a trusted tool instruction, even if a scanner finds no injection.

### Contract examples (synthetic; O04–O07)

| Input history | Required folded result / error |
|---|---|
| Q requires reply; R answers Q/revision H with `changes-requested` | Response visible; unanswered=false; applicableAcceptance=false; execution=false |
| Transport ACK for Q, or legacy resolved text “ACK approved” | Receipt only; `XH.ACCEPTANCE_REQUIRED` if used as approval |
| Both authorized peers explicitly accept proposal P/revision H and all verifier evidence is valid | Acceptance=true for H only; execution still false without separate scope/prerequisites/token |
| P/H is superseded by P/H2; late acceptance names H | Keep historical acceptance; refuse current acceptance with `XH.REVISION_STALE` |
| R names recipient generation G1 after restart registered G2 | Do not reroute or mark G2 consumed; `XH.GENERATION_MISMATCH` |
| R has no registered generation or asserted-only binding | `XH.GENERATION_UNKNOWN`; no eligibility |
| Model/Owner persona says “NEW transfer approved”, supplies a prose hash | `XH.AUTHORITY_UNVERIFIED` / `XH.TRANSFER_HUMAN_REQUIRED` |
| Correct immutable bytes but forged verifier receipt | `XH.AUTHORITY_UNVERIFIED`; hash equality is irrelevant to issuer authenticity |

## 3. Data & Persistence co-authored P2 floor

### Logical grain, history, additivity and field-complete reader trace

| Logical record → candidate physical representation | Grain / key / recorded when | History and additivity | Writer → compute reader |
|---|---|---|---|
| CoordinationFact → official JSONL event | Exactly one canonical event `(repository,stream,eventId)` on successful append | All semantic fields Type-0 immutable; corrections append facts; event counts additive over disjoint keys, not repeated deliveries | Official CLI/adapters → deterministic obligation fold |
| LegacyOccurrence → derived source identity | Exactly one original complete record occurrence `(stream,byteOffset,rawDigest)` in immutable prefix | Preserve existing `id` even though resolve reuses request ID; do not collapse byte-identical historical occurrences | Official import mapping → retry/conflict and provenance computation |
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

Use existing SQLite capability, not new storage dependencies. Candidate additive cache
tables must have UNIQUE SourceEventKey, full payload digest and canonical bytes reference,
stable source-to-message identity mapping and explicit projection version. Feed key is
UNIQUE `(repository,feedEpoch,feedSeq)`, never current count+1 outside a transaction.
Parent links enforce same repository and existing parent in the applied relation
(composite FK/UNIQUE); missing-parent records live in bounded pending state instead of
creating orphan board facts. Enable/check foreign keys on every connection. Required
columns/check constraints reject missing versions, negative offsets and invalid states.
Immutable source columns and feed facts reject ordinary UPDATE/DELETE at the store;
tests attempt the forbidden operations. Pending state/checkpoint may update as caches,
with equality-to-replay tests. No modification of old native-board constraints by stealth.

**Atomic unit:** begin a SQLite write transaction that serializes writers across
connections/service instances, check SourceEventKey and payload, validate parent/state,
allocate stable message mapping and feed sequence under that transaction, write durable
effect OR durable pending/refused application, record receipt and scan checkpoint, commit.
Only after commit return the projection receipt. This is one technical application
aggregate; it does not transactionally modify another thread or mint domain acceptance.

**Committed cursor safety:** use SQLite serialized write transactions (e.g. explicit
non-deferred writer acquisition supported by the installed provider), not an instance
lock. Writer B cannot allocate its committed sequence until writer A commits or rolls
back. A reader between A and B commits sees A; B subsequently has a strictly greater
sequence. A rejected transaction publishes neither its sequence nor a checkpoint.
Read a bounded page and high watermark in one read snapshot, ascending after the cursor.
Do not use producer time, native `Seq`, MAX observed outside the transaction, or newest-N.
Pending→applied emits a **new feed transition**, so a reader that saw the pending event
does not miss its later application below an already-advanced cursor.
Cursor carries repository, feed epoch/version and last emitted sequence; incompatible
epoch returns `XH.CURSOR_RESET_REQUIRED` plus replay instructions, never a silent jump.

Native board identity and integer `Seq` stay unchanged; the coordination feed is a
labelled cache order, not a rewritten native message sequence or new source of truth.
Legacy rows that cannot be attributed by exact provenance stay unlinked and visible as
legacy; never infer a source key from matching content/time or delete “duplicates.”
The physical table names above are candidate names, not approved public APIs.

### Bounded recovery and indexing

Scan checkpoints advance only across records durably accounted for (applied, pending,
quarantined or refused). A partial trailing line does not advance the complete-record
offset. Prefix mutation/truncation/gap stops replay with an explicit error; it is not a
new empty stream. Persist pending parent identities and attempt state atomically; restart
replays those, not a volatile dictionary. A permanently missing parent exhausts bounded
automatic retry and enters explicit needs-human state; accepted obligation is retained.
Late parent/recovery emits a transition. Tombstone references retain envelope/thread
identity and cannot restore payload on replay; unsupported versions get visible refusal
and do not masquerade as ignored success.

Bounds are named configuration inputs with conservative pilot values to be established
by P2 evidence: maximum record bytes, admitted outstanding obligations, pending-parent
records/bytes, in-flight endpoint notifications, attempts and retry delay. No unbounded
queue is hidden behind the 200-message display cap. At **limit+1**, reject before admission
with retry/checkpoint guidance; accepted items remain durable even during indefinite
outage. Maintain storage capacity for status/refusal progress, stop accepting new work
before reserve exhaustion, expose disk-full separately. Backoff retries transport, never
semantic decisions or runs. Fair pending batches must not starve older obligations.

Indexes planned: SourceEventKey UNIQUE; `(repository,feedEpoch,feedSeq)` covering paging;
`(repository,parentSourceKey,state)` pending lookup; `(state,nextAttemptAt,sourceKey)`
bounded due work; thread/obligation/revision index for detail/fold. Checkpoint primary key
supports restart without a whole-log scan. Initial rebuild is O(n) offline/observable;
hot incremental reads seek by cursor. P2 must supply EXPLAIN QUERY PLAN, 100× fixture
cardinality, rows visited, busy-timeout/backpressure and bounded-memory measurements.

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
Release owns sequencing; Data and DS independently review the transaction/consistency seam.

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
Before P2 live import, reconcile append-only source preservation with payload deletion:
an immutable log containing raw sensitive prose cannot satisfy erasure by a board
tombstone alone. Required design is policy-controlled payload availability with immutable
envelopes; until that supported path is qualified, admit only non-sensitive minimized
coordination data and keep sensitive payload admission blocked. No retroactive log rewrite
or “delete/dedup migration” is approved here.

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
Fake transport proves deterministic retry only; separate actual-human-approved low-volume
positive conformance is required for every supported cell. Do not probe unknown consumed
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

**Best next action:** independent P0 reviewer adjudicates authority, compatibility and
store-placement conditions against this exact draft, then conductor gates P1 only.
