---
id: design-code-atlas-e0
title: "Code Atlas E-0 — bounded implementation design"
type: design
status: proposed
owner: "@timianmalloo"
phase: "atlas-e0-design"
tags: [code-atlas, e0, design, inventory, csharp, source-binding, native, bounded]
links:
  - { to: architecture-code-atlas-proposed, rel: refines }
  - { to: spec-addendum-e-code-atlas, rel: implements }
  - { to: adr-01M2BBCC9EHCWVR1R4ZCZ7502T, rel: depends-on }
  - { to: adr-01M2BBCCAYMNMM1MFH0653Z0XF, rel: depends-on }
  - { to: adr-01M2BBCCCDC207J5KQW74WX2M2, rel: depends-on }
  - { to: adr-01M2BBCCDZT9CGP0W5YE3YKXDW, rel: depends-on }
  - { to: proof-code-atlas-contract-grounding, rel: depends-on }
  - { to: coordination-code-atlas-resume, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Concrete E-0 records, deterministic identity and fact encoding, bounded write/read protocols,
  numeric budget candidates, native state contract and vertical worklist. Source-safety,
  writer-admission and live SH3 adapter joins remain explicit; design content is not code permission.
---

# Code Atlas E-0 — implementation design, PROPOSED

**Author:** `atlas-architecture-astra`, 2026-09-12. **Risk tier:** T2.
**Responsibility:** let a user select an authorized physical file, select its independently
identified C# type/member, read matching source or an explicit changed/unavailable result, and
return through native Architecture history without losing identity or inventing evidence.

This design does not reopen the whole architecture. It specializes the content at
`51961b6c2a57c5274e9a87ac14c5626a3d3e645b`, including Owner turns 5/6, the Privacy fields and
SRE controls. Conductor checkpoint `cd2e6e9e7defc8415159f6e861a5aa39843c13be` carries that
architecture and its content reviews/rulings. Its candidate-spec evidence-label correction
supersedes older report-derived verification wording; the functional source/identity requirements
remain the input. No normative registration, source permission or main-integration approval is
inferred from those commits.

**Source-safety worker `90e3a4be` is a worker reference, not a Git revision.** Its Windows
handle/link/race/decoder mechanism is pending this design's join. No report or safe native API
is invented here, and this worker does not duplicate its research.

## 1. Scope, framing and grounding

### 1.1 Terminal behavior and explicit exclusions

The phase's acceptance sentence is:

> Physical authorized inventory → independently identified C# type/member →
> **exact hash-bound available source OR typed live-changed/unavailable; no stale old-body
> substitution** → Back through the native Architecture composition.

Mandatory E-0 symbol families: source-declared types, ordinary methods/overloads, constructors,
properties/accessors, partial type/member declaration navigation. Indexers, fields/events,
operators/conversions, destructors, local functions/lambdas and other unprobed kinds remain
explicit unsupported/staged obligations. Do not advertise them as implemented E-0 support.
Generated/vendor/migration/unsupported **files** are not blanket omissions from physical inventory.

Out: all E-1…E-4 views, model calls, comparison/decision ingestion, public export, a fourth
perspective, new renderer or graph database, persistent navigation aggregate and new source-body
archive. A copy-anchor gesture may be supplied by the existing trusted broker after authorization;
it is not automatic source export and is not required to close E-0.

### 1.2 Evidence ledger — what was actually established

| Input / contract | Evidence and confidence | Design consequence |
|---|---|---|
| Whole architecture and candidate requirements | Read in this authoring chain; architecture frozen at `51961b6c`; Conductor `cd2e6e9e` observed. **Verified document content**, not native/product proof. | Preserve logical/version distinction, Owner live-read/default-inventory/fact-substrate/floor choices and E-0-only runtime scope. |
| Existing store composition | Direct `WorkspaceCore.cs:25–118`, `WorkspaceStore.cs:40–152`: Core opens one `workspace.db`, owns its epoch and exposes `Store`; `Open` bumps epoch. | Inject that existing store. A new Atlas service must never open another writer or bump the epoch to obtain a reader. |
| Actual writer API | Direct `StoreWriter.cs:35–111,169–258`: `DesireScopeGeneration`, `ReadDesired`, `CommitSnapshot`, `UpsertNode`, `Commit`, rollback on `Dispose`. | Use its real Unit of Work; validate assertion scope/revision before calling it; no general SQL write facade. |
| Writer admission limitation | Direct `WorkspaceStore.BeginWrite`: synchronous `_writerGate.Wait()` with no timeout/cancellation or priority parameter. Existing Core calls it directly. | A timed/priority writer-admission handoff is required. Wrapping `BeginWrite` in an abandoned `Task.Run` is not cancellation. |
| Historical-read limitation | Direct `StoreReader.cs:19–63,493–557`: current snapshot/page queries select latest complete scopes; `CurrentSourceRevision` picks one revision; reader has a query-only connection, not an explicit transaction. | Neither API supplies a pinned manifest/history vector. New Understanding reader uses exact referenced generations, not those helpers as a snapshot shortcut. |
| Internal bounded SQL access | Direct `StoreReader.cs:707–739`: internal `Command(sql, parameters)` on query-only connection. | A narrowly implemented reader inside the same Core assembly can use parameterized exact-reference SQL without editing `StoreReader` or exposing SQL to clients. |
| Evidence identity | Direct `Facts/EvidenceAssertion.cs:1–58`: `Static|Runtime` origin; `Verified|Inferred|Unverified`; SHA-256 over unit-separated fields; generation/ObservedAt are outside assertion ID. | E-0 emits Static evidence; JSON escaping and closed identifiers prevent separator injection; retries check existing observations rather than reinserting identical assertions. |
| Extractor seam | Direct `Extraction/FixtureExtractor.cs:35–57`: typed `ExtractionRequest`, `ExtractionResult`, `IExtractor.ExtractAsync(..., CancellationToken)`. | Reuse its conventions but do not pretend display-only output exposes Roslyn semantic objects. Real compilation-input adapter remains an explicit Core join. |
| Roslyn and baseline suites | K0 report at `e4229845`: reported 79 Core + separate 40 Store/IPC tests and synthetic 4.14 probes, including project collisions, moved spans and partial syntax references. | Bounded reported execution only; not independent execution here, full-kind coverage, native safety, or E-0 product proof. Prior restore network activity **NOT RECORDED**. |
| UI tokens and test/telemetry rules | Opened relevant `DESIGN.md` token rows; C# expressive-value-type rule; Testing Strategy trigger table; Observability O1–O13. | Reuse tokens and typed IDs; D0/D1/D2/D3/D4/D5-provider/D6/D7 union; normal-path measurements without sensitive telemetry. |
| Live SH3 and source-safety mechanism | Conductor/user boundary; no live SH3 file inspected or modified for this design. | Registration/transport/host wiring and Windows mechanism must join through their owners; no test double can satisfy those product exits. |

Grounding follows architecture → four E-0 ADRs → K0/Owner constraints → exact existing
writer/reader/record contracts. The optional code graph was absent at prior grounding; no new
graph or broad repository inventory was generated. Existing classes E2E-A/C/D/F, RIG-A/E,
DM-A/F, CTX-D and the identity-conflation register entry inform the falsifiers below.

### 1.3 Design alternatives and execution shape

| Candidate | Decision |
|---|---|
| Type-only `NodeContent` plus a tree skin | Reject: hides unsupported physical files and lacks bound member identity. |
| New standalone Atlas database/daemon/demo composition | Reject: fake production seam or second authority; violates the chosen substrate and native journey. |
| New Understanding records/services using the real injected store, with serialized owner adapters | Select: independently testable without claiming shared wiring is already complete. |
| Parse `has_member` or invent a no-reference compiler as fallback | Reject: display text and synthetic compilation are not the required project/TFM semantics. |

Plan used: contract grounding → concrete design → structural checks/commit, one serial reasoning
path with no child agents. Estimated work/span 20 minutes (**Inferred**, not runtime latency).
Source-safety research and Owner coordination run externally, not duplicated here. No floor was
waived to produce a document: execution, native proof and independent design admission remain gates.

## 2. Domain model and durable grain

### 2.1 Entities, value objects and invariants

E-0 lives in **Repository Evidence Inventory** with **Code Atlas Views** as a presentation context.
Use readonly value types for WorkspaceId, RootId, FileId, CompilationScopeId, SymbolId,
DeclarationId, ContentId, ManifestId, PolicyRevision and RequestId; never interchangeable strings
inside Core. Wire DTOs have validated canonical strings. Origins/confidence/availability are
separate closed vocabularies.

| Durable root | Invariant / identity references |
|---|---|
| InventoryObservation | One bounded enumeration of one authorized root/policy has an explicit terminal observation status. Records describe physical membership independent of semantic support. |
| SourceObservation | Exact authorized bytes, decoder contract and verification state determine whether any indexed span can be used. Refer to FileId and indexed ContentId. No durable body by default. |
| DeclarationObservation | One logical symbol occurrence is tied to one FileId, ContentId, scope, producer version and distinct syntax spans/partial role. |
| WorkspaceManifest | A seal pins an explicit inventory observation and semantic observation vector. It never substitutes current facts for the referenced generations. |

Roots refer by identity. Immutable chunk staging is an implementation of observation/seal
publication, not a new domain aggregate or Event Sourcing framework. One bounded staging unit is
written per transaction; only a valid root seal makes the assembled observation selectable.
**Navigation is view-local state**, not a durable root or append-only fact stream.

### 2.2 Natural IDs versus versions — encoding fixed

Define `CanonicalTupleV1(parts)` as compact UTF-8 JSON array of strings, in declared order, with
the default safe System.Text.Json escaping contract pinned by golden byte tests: no whitespace,
no culture-sensitive formatting, control characters escaped, no unordered dictionary. Components
are compared ordinally after their domain normalization. Encoding changes require identity-v2,
not a silent serializer switch.

`Id(prefix, parts) = prefix + lowercase hex SHA256(CanonicalTupleV1(parts))`.
Store the canonical tuple with the identity and compare on a hash-key collision; conflict is
`AIDE-ATLAS-IDENTITY-CONFLICT`, never merge-by-hash. Canonical component/record limits are in §6.

| ID prefix | Ordered tuple parts / stable semantics |
|---|---|
| `af1:` file | WorkspaceId, registered RootId/worktree, root-relative physical path in the root's verified case policy. Path normalization does not grant containment. Rename creates a new file identity; no rename inference in E-0. |
| `ac1:` compilation scope | WorkspaceId, RootId, project FileId, target framework, declared configuration/compilation-variant key. No transient Roslyn GUID or content hash. References/options digest is an observation attribute. |
| `as1:` type/member | `ac1` scope, `"csharp"`, kind (`type|method|constructor|property|accessor`), canonical Roslyn documentation ID. No revision/hash/line/display signature. Partial definition/implementation normalize to the same logical member. |
| `ab1:` content | `"sha256"`, exact byte hash, byte length as invariant decimal. DecoderId and decoded-text digest accompany a binding rather than changing the meaning of the byte hash. |
| `ad1:` declaration | SymbolId, FileId, ContentId, DecoderId, partial role, identifier/declaration/body UTF-16 start+length values, producer contract version. Absent body is literal `"none"`, not zero-length body. |
| `ar1:` immutable record | record kind/schema and canonical semantic payload. Wall-clock/read duration/ingress sequence are observation metadata, not logical identity. |
| `am1:` manifest | root/policy, selected inventory record identity, sorted scope observation identities, producer/schema versions and coherence/coverage semantic payload. No timestamp-only identity. |

No field entering `EvidenceAssertion.ComputeId` contains literal U+001F: prefixes/predicates/
extractor IDs are closed ASCII; scope/revision/subject are encoded IDs; object is safely escaped
canonical JSON. A hostile source signature/control character survives as data, not a tuple boundary.
Validate this before constructing the existing assertion.

Root case policy and canonical path syntax must come from the accepted source-safety contract.
Do not invent unconditional lowercase or claim lexical path normalization defeats links.
The root policy may explicitly refuse ambiguous case-colliding paths; the inventory then reports
partial coverage, not a merged identity.

### 2.3 Record schema and writer/compute-reader trace

All records carry `schema=1`, identity, WorkspaceId, RootId, PolicyRevision and producer/version.
Collections are immutable ordered arrays; absence is nullable only with a typed reason.

| Record | Fields beyond common header | Writer → compute readers |
|---|---|---|
| `InventoryFileV1` | FileId + canonical tuple; relative path; parent FileId/path-node key; kind; tracked/untracked/nonGit membership; project-membership IDs; category flags; presence; source-availability; semantic-capability reason. | Inventory walker → tree/filter, outline routing, coverage fold and source authorization lookup. |
| `ContentBindingV1` | FileId, byte hash/length, DecoderId, decoded-text hash/length, observed-state, optional VCS revision, provenance; hash absent only with explicit unverified/unavailable reason. No body. | Safe-read adapter/coordinator → declaration validator, live source comparison and manifest coherence. |
| `SymbolDeclarationV1` | SymbolId + canonical tuple, CompilationScopeId, containing SymbolId?, kind, display signature, DeclarationId, FileId/ContentId/DecoderId, identifier/declaration/body spans, partial role/counterpart references, semantic status. | Roslyn declaration collector → scope chooser, outline, source span validation and Back identity. |
| `ScopeObservationV1` | CompilationScopeId, options/reference digests, request target, completion state, member-kind capability set, chunk references, file-binding digest, permitted diagnostics and counts. | Semantic coordinator → manifest join and capability/coverage projection. |
| `InventoryObservationV1` | Policy/root boundary, chunk references, observation interval, observed counts, excluded/withheld flags, completion/error/cancel state, membership digest. | Inventory coordinator → manifest join, physical tree, refresh disclosures. |
| `ManifestSealV1` | ManifestId, request target, inventory observation ref, ordered scope refs, desired/committed generation vector, coherence and freshness axes, retained-history availability, coverage counts with denominator state. | Seal validator/coordinator → every query, cursor, consistency badge and selection validation. |

`SourceReadResultV1` is request-local, not a persisted source-body observation:
indexed and read ContentId, read time, decoder, page bytes, validated spans or disabled-anchor
reason, content state and byte bounds. Normal reads emit aggregate telemetry; do not append one
durable record per source click. Persisted bindings come from admitted inventory/extraction runs.

### 2.4 Grain, history and additivity

| Physical representation | Exact grain / history |
|---|---|
| Existing `node_dim` | One version of a logical file/symbol entity represented by `af1/as1` and canonical identity facts. Use existing `UpsertNode`; current implementation closes/reopens when kind **or label** changes and no-ops unchanged rows. Do not assume its schema comment makes label writes Type-1. |
| Existing `evidence_assertion_fact` | One typed Atlas record assertion in one immutable chunk revision, under an Atlas-owned scope/generation. Records and fact metadata are append-only. |
| Existing desired/committed scope facts | One durable desired target or one chunk/seal scope commit. Generation fences apply per scope; additional root-run fence protects publication order. |
| Existing command receipt | One admitted refresh command's terminal outcome under principal/command ID, with payload fingerprint in the safe outcome envelope. Duplicate payload returns original outcome; different payload conflicts. |
| Bounded in-memory derived cache | One manifest/policy/schema keyed inventory/outline materialization. Disposable, labelled cache, never evidence. No new cache table is required in E-0. |

Logical IDs and provenance are immutable. Meaning-changing kind/path/classification/signature/
capability/permission observations are new records; historical display reads its pinned record,
not today's dimension label. Current selection/history is not persisted as facts.
Source bodies remain unretained by default.

File/declaration counts and bytes are **semi-additive over observation time**; additive only over
disjoint entries in one manifest. Logical-symbol counts do not sum partial declarations.
Coverage ratios and latency percentiles are **non-additive** and require stated denominators.
Invocation/queue outcome counts and transferred bytes are additive over unique operations.
Hash, IDs, spans, sequence and generation are not summable measures.

## 3. Existing SQLite substrate — concrete encoding and bounded writes

### 3.1 Closed fact vocabulary and chunk references

Use one predicate per typed immutable record:
`atlas.v1.inventory-file`, `atlas.v1.content-binding`, `atlas.v1.declaration`,
`atlas.v1.inventory-observation`, `atlas.v1.scope-observation`, `atlas.v1.manifest-seal`.
Subject is `ar1:`/`am1:` record identity; object is its canonical JSON. The payload retains logical
IDs; no raw `has_member` parse and no raw source body in a generic object column.
Origin is existing `EvidenceOrigin.Static`. Existing `VerificationStatus` describes what the
producer established; partial/unsupported/binding availability is carried separately and never
represented by a fabricated Verified source claim.

An Atlas chunk scope is a canonical ID for `(WorkspaceId, RootId, collection kind,
CompilationScopeId-or-none, batch ordinal, content revision)`. Compute content revision first
from the ordered canonical records plus producer/schema version; it is also the chunk's artifact
revision. A chunk scope is immutable for that content version: a new content revision creates
a distinct scope, while an A→B→A revert reuses A's original committed scope/reference rather
than reinserting identical assertions under a new generation. Root-run scopes remain stable and
monotonically fenced. Sort by `(record kind, FileId,
SymbolId-or-none, DeclarationId-or-none, RecordId)`; start a new chunk at 64 records or
256 KiB serialized payload, whichever comes first. A record over 32 KiB is explicitly
unsupported/BudgetExceeded; it is not truncated into a valid identity.
Maximum 2,048 chunk references per manifest; root seal payload ≤256 KiB.

A `ChunkRef` contains exact scope ID, generation, artifact revision, record count and payload
digest. Unchanged chunk revision reuses the prior committed reference and its original provenance;
it is not inserted again under a new generation. Historic reads use those exact references.

**Generic-reader compatibility is a real shared-owner join.** Current readers can enumerate all
latest complete scopes. This design does not assume they ignore new Atlas vocabulary. Before the
first product write, Core must prove Atlas records do not pollute existing graph/evidence views,
or supply a serialized scope/vocabulary exclusion through the proper owner. Live GraphProjection,
Canvas/Evidence and related tests stay untouched by this worker and parallel Atlas writers.
No Atlas product producer is wired until that proof exists.

### 3.2 Refresh and commit algorithm

All mutation is performed by a new `Understanding/AtlasObservationWriter` using the **injected
Core-owned WorkspaceStore**, never opening its own DB. The write-admission port in §4.2 supplies
the real StoreWriter lease; no component touches its internal connection or reflection.

1. Authorize root/policy and validate the bounded request before scheduling. Refresh command ID
   and canonical request fingerprint are registered through the trusted Core command context.
2. Under the admitted writer lease, read the Atlas root-run scope's desired pair. Allocate
   `nextGeneration = previous+1` with checked overflow, append `DesireScopeGeneration`, and commit.
   Only Atlas's admitted coordinator owns this scope vocabulary; generic refresh code may not
   enqueue it as another extractor. Overflow is a stable error, never wraparound.
3. Enumerate and collect semantic observations outside write transactions. Every safe source
   read contributes exactly the bytes/decoder binding used by the compiler tree, or an explicit gap.
   A new root request can supersede this run; cancellation cannot falsely complete it.
4. Before **each** chunk write, acquire the bounded writer lease and recheck the root desired pair.
   A mismatch rejects obsolete work. Resolve the content-addressed chunk scope: an exact prior
   committed chunk is reused, including after a revert. Otherwise append its initial desired
   generation and `CommitSnapshot` in one
   transaction, with records already validated and bounded.
5. Adapter validates every assertion's ScopeId, ArtifactRevision, origin, predicate and producer
   before `CommitSnapshot`; that method does not validate every cross-field equality itself.
   Upsert affected dimensions only for the chunk's distinct logical IDs. Call `Commit` only after
   all validations pass; otherwise Dispose rolls back. Check cancellation/deadline before mutation
   and before commit, not only at queue entry.
6. Build the root seal from exact committed chunk/scope references. In one bounded transaction,
   recheck root desired target and validate all referenced scope/gen/revision/count/digest values.
   Append the root seal through `CommitSnapshot` and record the refresh terminal command receipt.
   The root attempt's artifact revision binds RequestId plus root generation and target fingerprint,
   so a newly observed seal never reinserts an old natural assertion key. Manifest identity includes
   that declared observation target; logical file/symbol identity does not.
   A competing newer request wins the root fence; obsolete chunks remain unselected evidence.
7. Publish the manifest ID only after readback verifies the stored seal and references. UI receives
   its coherent/mixed/partial/unknown and freshness axes, never a single guessed workspace revision.

`complete=true` on an **Atlas manifest scope** means the typed manifest record/reference vector
was fully generated and sealed. It is **not** physical or semantic completeness: a fully described
mixed/partial observation remains mixed/partial in `ManifestSealV1`. This scope contract differs
from a C# extraction scope and must be carried explicitly by the vocabulary adapter and tested.
Interrupted **unsealed** records are never published as a manifest. Root physical completion is
false when tracked files are missing, paths inaccessible, cycles/collisions unresolved or enumeration
was capped/cancelled. Unknown semantic bindings cannot make coherence true.

After cancellation/failure before sealing, retain last-successful manifest and report the current
attempt's typed terminal state. Recovering orphaned staging never upgrades it by age. No automatic
purge of old/unselected chunks or evidence to meet a byte budget.

### 3.3 Idempotency and replay

Command idempotency key is `(WorkspaceId, authenticated principal, "atlas.refresh.v1", RequestId)`.
Canonical request fingerprint includes root, policy, explicit scope selection and budget-profile
version, but not transient deadline remaining or transport trace ID. Same key/different fingerprint
is `AIDE-ATLAS-IDEMPOTENCY-CONFLICT`.

The actual fact natural key excludes generation. Reuse immutable chunk revisions before writing;
never change timestamps to defeat uniqueness, use `REPLACE`, or swallow a constraint violation.
Crash after commit/before reply returns the stored receipt and seal on retry. Crash before seal
leaves last-successful active; new admission can restart under a new root generation. No old worker
can later publish an obsolete root.

Reader lookup uses `Store.BeginRead()` and Core-internal `StoreReader.Command` with fixed SQL
templates, parameters and limits. Do not expose SQL or table names over a port. No current-reader
helper may substitute its newest generation for a manifest's exact generation.

Example read shape (a query specification, not new production SQL):

```sql
SELECT a.assertion_id, a.subject, a.predicate, a.object
FROM evidence_assertion_fact AS a
JOIN scope_snapshot_committed_fact AS c
  ON c.scope_id = a.scope_id AND c.generation = a.generation
WHERE a.scope_id = $scope AND a.generation = $generation
  AND a.artifact_revision = $revision AND a.predicate = $predicate
  AND a.subject > $after
ORDER BY a.subject
LIMIT $limit;
```

Exact-subject source/declaration reads lead on the existing subject index. Chunk enumeration
is bounded by the producer's 64-record cap; query-plan tests must prove the actual schema can
retrieve the admitted workload without whole-store scans. The reader does not claim BeginRead
starts a transaction. Immutable references avoid newest-state mixing; the final shared adapter must
coordinate compaction/retirement so a multi-query read either retains its references or returns
`HistoryUnavailable`. Never fill a disappeared reference from a current row.

Drop all Atlas caches and reconstruct inventory, outline, source-binding decisions and manifest
badges from retained facts. Canonical result equality includes coverage, unknowns, origin and bounds.
Replaying cannot reproduce unretained bodies; history reports unavailable if bytes are gone.

### 3.4 Schema reuse and rollback

E-0 selects **no physical schema migration** and no new authoritative DB/cache table.
The new reader lives in the existing Core assembly and uses the existing query-only connection.
Any required index/table is a change request only after actual query-plan/benchmark evidence
disproves the generic representation. No silent schema-version bump or guessed backfill.

Rollback disables new Atlas capability and its owner-installed registration/producer adapter.
Preserve all appended facts/old entry points; do not rewrite IDs, revert schema_version, or drop
evidence. The compatibility gate must show legacy consumers remain safe with Atlas facts present;
otherwise product writes are not admitted. If physical evolution later becomes necessary, execute
expand → compatible backfill/unknown → read comparison → rollback → separately admitted contraction.

## 4. Ports, ownership and concrete component contracts

These are **proposed signatures**, not claims that these types already exist. Implement them under
`src/AiDe.Core/Understanding/` and companion new test paths only after explicit authoring admission.
Use immutable records/value types, guard clauses, cancellation tokens and existing error conventions.
No new package, project, service, framework or model dependency is selected.

### 4.1 New production components and state ownership

| Component / proposed file | Exposes / consumes | Ownership, lifetime and terminal behavior |
|---|---|---|
| `AtlasIdentity.cs` | CanonicalTuple/typed IDs, checked span/value validation | Pure, deterministic; no I/O or caches. Reject duplicate/collision/over-limit rather than repair identity. |
| `AtlasContracts.cs` | Records, enums, error codes, budget profile and versioned DTOs | One vocabulary for writer/query/wire/native readers; no WPF/SQL/provider types. |
| `AtlasInventory.cs` | `EnumerateAsync(AuthorizedRoot, InventoryPolicy, ObservationTarget, ct)` → InventoryObservation + records | One bounded root job; consumes safe enumeration/metadata port. Maintains visited authorized file identity set; no semantic-node input. |
| `AtlasDeclarations.cs` | `CollectAsync(CompilationInputBundle, ObservationTarget, ct)` → ScopeObservation + declarations | Consumes real approved project/TFM/options/references and exact bound syntax inputs; installed Roslyn 4.14 only. No `has_member` parsing or synthetic no-reference production fallback. |
| `AtlasObservationWriter.cs` | `StageAsync(ObservationChunk, RootFence, ct)`; `SealAsync(ManifestDraft, RootFence, ct)` | Uses injected store plus real admitted writer leases. Bounded transactions, root+scope fencing, receipt/idempotency rules. |
| `AtlasRefresh.cs` | `RefreshAsync(AtlasRefreshRequest, CallerContext, ct)` → `AtlasRefreshResult` | Owns queues and finite run state; no shared transport registration. One active root coordinator; at most two compilation jobs. |
| `AtlasQueries.cs` | Capabilities/Inventory/Outline/Source/Manifest queries | Uses fixed historical SQL and source port; current policy checked on each query and continuation. No App file read. |
| `AtlasSerialization.cs` | `SerializeV1`/`ParseV1`, size validation and canonical golden fixtures | Data-only tests can run without live IPC edits. It is not a substitute IPC server/client. |
| `AtlasPorts.cs` | Narrow source/compilation/writer-admission/authority ports | Ports name unresolved real adapters; test fakes stay in tests and cannot be registered as product defaults. |

No new `AtlasNavigation` domain aggregate/class is mandated. Native views use existing view-local
state/Memento restoration; Core validates selection through AtlasQueries.

### 4.2 Required real adapter joins — no fake wiring

| Join | Concrete required contract | Why still pending / who resolves |
|---|---|---|
| **S-SAFE: file capability** | `EnumerateAuthorizedAsync(rootCapability, policyRevision, budget, ct)` and `ReadVerifiedAsync(fileCapability, expectedBinding?, readBudget, ct)` return authorized metadata or exact immutable byte/text buffer with ContentId/DecoderId/state; refuse escape/race/invalid encoding, never active mismatched span. Dispose releases handles/buffers. | Source-safety worker `90e3a4be` establishes Windows handle/link/race/decoder semantics and cancellation. Use its executed result, not a chosen API here. |
| **S-COMP: real compilation inputs** | `LoadCompilationInputsAsync(CompilationScopeRequest, ct)` returns actual project FileId, TFM/configuration, compiler options/reference digests, approved metadata references and source-tree inputs tied to FileId/ContentId/DecoderId. Every tree's text is exactly its recorded decoded input. | Core owner binds its real project reader/discovery to this contract. Existing IExtractor output alone has no semantic object contract. No arbitrary project task/target execution or permissive MSBuild load is authorized by selecting a file. Any required build evaluation runs under existing approved trust policy. |
| **S-WRITE: bounded writer lease** | `AcquireAtlasWriteAsync(rootFence, admissionBudget, ct)` returns the real `StoreWriter` lease or typed busy/cancel/stale; max wait 500 ms, one Atlas lease, 8 pending control/16 pending Atlas requests, control priority with fairness below. All acquisitions pass the shared writer-admission owner. | Existing `BeginWrite` is synchronous/uncancellable. Core must supply a serialized cancellation/timeout-capable admission seam, potentially a narrow Store API addition, and integrate legacy control priority. No fake scheduler or abandoned Task.Run establishes this. |
| **S-AUTH: policy/epoch** | Validated `CallerContext` binds principal, workspace, CoreEpoch, root capability, permissions and current policy revision. Native/IPC inputs cannot mint it. Reauthorize stale policy/epoch and narrow continuation results. | Existing authority owner supplies the context; new Understanding code does not open/authenticate a second authority. |
| **S-WIRE: transport integration** | Register `atlas.*.v1` operations against the same local query implementation, negotiated capabilities and authoritative request context; preserve all DTO fields and trace context, enforce frame limit and cancellation. | Existing **WorkspaceClient/WorkspaceOperations are live SH3 files**. Their owner installs the adapter serially after acknowledgment; this design does not edit them or declare another transport. |
| **S-HOST: native integration** | Owner registers actual Architecture tree/source/outline/inspector using the existing factory/menu/host and injects query client; state persists through retained host, no fourth perspective. | SurfaceContentFactory/WorkbenchShell/WorkbenchAdapter and live Canvas/Evidence/GraphProjection/ZoneLayout files/tests are forbidden during this track. Reconcile actual current main after acknowledgment; reported SH2 `b4e61022` is not an unopened source contract. |
| **S-COMPAT: shared evidence/history** | Existing graph/evidence consumers must ignore/isolate Atlas storage predicates; compaction cannot silently invalidate retained manifests. Real-store/legacy-query and history-retirement tests establish this. | Core/shared-reader owners resolve before product writes. No broad filter edit or fake presentation bypass in the new namespace. |

These are bounded **implementation joins**, not reasons to stop drafting or to reopen the whole
council. The producer and query logic can be written/tested after their separate new-namespace
admission; claiming the deployed walking skeleton waits for the real joins. Shared adapters are
serialized handoffs, never parallel edits disguised as another filename.

### 4.3 Compilation and source collection algorithm

1. Choose one real CompilationScopeId, not just a namespace or project display label. If a file
   belongs to several projects/TFMs, return choices; do not silently use the first.
2. Validate the bundle: every source input refers to an authorized physical file or is explicitly
   a nonphysical/generated input. Virtual inputs do not invent physical inventory files.
   E-0 advertises source-declared mandatory kinds only where the actual file/binding is available.
3. Traverse declared types and mandatory member families. Skip compiler-implicit/backing/accessor
   duplicates through symbol kind/implicit flags, not name patterns. Accessors in the mandatory
   family remain independently addressable; property and accessor IDs are distinct.
4. Obtain Roslyn declaration ID, preserving overload signatures. Canonicalize a partial member to
   its definition identity; collect both definition and implementation syntax references. Deduplicate
   exact `(FileId, span, role)` occurrences; return multiple declarations explicitly.
5. Record separate identifier/declaration/body spans. All are zero-based half-open UTF-16 ranges
   over the exact decoded tree. Body is absent for abstract/declaration-only members; a full syntax
   range is not automatically the body. A lookup miss is not evidence about null ID creation.
6. Reject missing/ambiguous semantic IDs from the stable-symbol set with typed capability reason.
   A version-bound syntax occurrence can still be shown if safe, but cannot masquerade as a
   logical member. Compiler errors/unsupported language version mark semantic coverage partial.
7. Sort canonical records, enforce per-scope/run limits, emit scope observation and stage chunks.
   Any timeout/cancel/limit reports its actual coverage; it cannot silently omit a mandatory family
   and call the outline complete.

No extractor body is retained. Read/hash/buffer reuse is permitted only inside the source-safety
contract and memory budget. Source/display re-read must validate the current bytes again; successful
compilation earlier does not authorize later reads or prove later bytes match.

## 5. Query, serialization, cursor and source-page contract

### 5.1 Operations and envelopes

Operation vocabulary: `atlas.capabilities.v1`, `atlas.refresh.v1`, `atlas.manifest.v1`,
`atlas.inventory.v1`, `atlas.outline.v1`, `atlas.source.v1`. Refresh is a separately authorized
command; the other five are read-only. No `atlas.interpret`, diagram, comparison or export operation.

| Operation | Input | Result / principal computation |
|---|---|---|
| Capabilities | Authenticated workspace/root | Schema/identity/budget versions; admitted member families; per-port availability; disabled reasons. Missing safety/wire/native joins are not “supported.” |
| Refresh | RequestId, RootId, PolicyRevision, explicit project/TFM selection, budget profile | Command receipt, desired root generation, terminal outcome and sealed ManifestId if available. |
| Manifest | Requested ManifestId or explicit latest-observed/last-successful selector | Exact observation vector, coherent/mixed/partial/unknown, freshness, policy and retained-history availability. |
| Inventory | ManifestId, parent key/filter, cursor?, pageSize | One physical record per FileId, project memberships separately, counts/denominator, continuation and source/semantic availability. |
| Outline | ManifestId, FileId, optional CompilationScopeId, cursor? | Scope alternatives or mandatory-kind logical symbols and declaration choices; no display-derived IDs. |
| Source | ManifestId, FileId, optional DeclarationId, cursor? | IndexedMatch / LiveChanged / LiveUnindexed / Unavailable / UnsupportedEncoding / TooLargeToVerify / ReadUnstable / Refused; active spans only for IndexedMatch. |

Common envelope fields:

`schemaVersion, operation, requestId, selectionGeneration, workspaceId, coreEpoch, rootId,
policyRevision, manifestId?, outcome, error?, capabilities, coverage, bounds, data`.

`outcome` is `complete|partial|cancelled|refused|unavailable|budget-exceeded|failed`.
Transport/refusal and content state are separate: a successful read can truthfully return
`LiveChanged` without being an indexed-source success. Failure cannot serialize as empty success.

Use existing web-default JSON conventions explicitly pinned in `AtlasSerialization`: camelCase,
closed string enums; no integer enum acceptance; reject duplicate properties and unknown **required
capabilities**; bounded depth 16. Unknown envelope/schema/operation is refused, not defaulted.
Unknown optional fields within negotiated v1 may be ignored only where the schema says optional;
security/identity fields are never inferred from missing values.
Ingress/generation/epoch values serialize as invariant decimal strings to preserve Int64 precision;
counts/byte lengths are checked bounded integers. Timestamps are UTC display/provenance metadata,
not ordering/authority keys.

Scope/revision vectors are typed arrays; no comma-separated scope string. Span fields include
unit=`utf16`, ContentId, DecoderId and range. Source result carries indexed and read hash IDs
separately, read timestamp, byte/decoded bounds and disabled-anchor reason.
Source page bytes use UTF-8 encoded text in a base64 field with explicit encoding; this is framing,
**not encryption or a privacy permission**. Page boundaries cannot split a code point or lie about
UTF-16 offsets. The safe-decoder adapter owns transcoding/offset proof.

`coverage` distinguishes physical visible-in-policy population from semantic supported/observed
population and category counts. `bounds` contains requested/effective limits, returned rows/bytes,
known total or `unknown|withheld`, omitted count when permitted, reason and cursor.
Do not sum or expose withheld categories to infer hidden paths/counts.

### 5.2 Stateless continuations and manifest validation

Use an opaque, server-authenticated cursor envelope from the existing authority's approved token
facility; if none is available, the serialized adapter must establish one before admission.
The payload binds principal/workspace/epoch/root/policy, operation/filter/sort, ManifestId, exact
chunk reference and last key; source cursors also bind read ContentId/DecoderId/range.
Cursor bytes ≤4 KiB, age limit 5 minutes, no source body/path secrets in clear cursor payload.
This is a token contract, not a newly selected signing/crypto provider API.

Every page reauthorizes. Policy/epoch/manifest retirement invalidates cursor with a typed reason.
Changed bytes invalidate source continuation; do not append a new version to an old page.
An expired token can restart against the same retained manifest after authorization; if unavailable,
return HistoryUnavailable, not latest. Source/outline ordering is stable and the complete tie-break
is part of the cursor; tests intentionally create equal display names/partial locations.

### 5.3 Numeric frame and materialization limits

| Limit | Proposed numeric value / rationale | Oracle |
|---|---|---|
| Complete encoded response | **256 KiB**, including envelope; require real IPC compatibility before registration | Serialized response byte 262,145 is refused/re-paged, never accepted by a larger test-only transport. |
| Source page text | **128 KiB UTF-8**, base64 ≤174,764 bytes; metadata ≤32 KiB leaves bounded framing headroom | Multibyte/escaping fixture asserts full frame ≤256 KiB and exact offsets. |
| Inventory / outline page | **128 rows**, client may request 1…128 | 129 requested clamps with disclosed effective limit; cursor covers every permitted row exactly once. |
| One source verification buffer | **8 MiB exact file bytes**, decoded UTF-16 allocation ≤16 MiB | 8 MiB+1 refuses verified-binding claim; no hash of prefix used as whole hash. |
| In-flight source reads | **2**, combined owned source/decoded buffers ≤48 MiB | Third read waits within queue budget or returns busy; disposal/count returns to baseline after cancel. |
| Record / canonical ID | Record **32 KiB**; individual canonical tuple **8 KiB** | Oversize signature/path does not produce truncated valid identity; inventory gap remains disclosed. |
| Manifest record / chunk vector | Seal **256 KiB**, ≤2,048 references | Over-limit seal fails before write; no partial valid-looking root published. |
| Cursor / JSON | Cursor **4 KiB**, age **5 min**, JSON depth **16** | Oversize/deep/expired/tampered cross-policy token rejected. |

These are selected **candidate implementation limits with rationale**, not measured performance.
The Owner/design admission must accept the budget profile before dispatch; changing a value creates
`AtlasBudgetProfileV2`, not an unlogged tuning constant. If the real IPC frame limit is lower, the
owner serializes an explicitly smaller compatible profile and reruns the same oracles—never a
hidden truncation. No new transport is justified by this table.

## 6. Scheduling, cancellation, growth and telemetry budgets

### 6.1 One named profile, numeric queues

`AtlasBudgetProfileV1` is a single immutable definition shared by producers/readers/tests; no
per-view copies. Prior architecture left fields open; this table fills them for admission.
Limits are modelled starting values on the workload below, not observed throughput guarantees.

| Work | queue_capacity | full_mode | coalescing_key | control_priority | max_in_flight | deadline | Required metric |
|---|---|---|---|---|---|---|---|
| Root inventory/refresh | **4 requests / 2 MiB descriptors** | Reject newest with BudgetExceeded after **250 ms** admission wait | root + policy; replace only obsolete unstarted refresh | Background **10**, below control **0** | **1 root coordinator** | Enumeration **30 s**, whole refresh **120 s** | depth, wait, superseded count, terminal reason |
| Compilation collection | **8 requests / 4 MiB descriptors** | Backpressure then refuse after **250 ms**; no successful empty scope | CompilationScopeId + desired root generation | Background **10** | **2 compilations** | **30 s/scope**, shared **120 s** root ceiling | depth, duration, declared/unsupported counts |
| Interactive queries | **16 requests / 2 MiB descriptors** | Busy/BudgetExceeded after **100 ms** | native view ID + selection generation + operation; only obsolete work | Interactive **5** | **4 queries**, of which ≤**2** source reads | Query **2 s**, source **2 s**, queued source wait **250 ms** | wait, read/hash/return bytes, late drops |
| Shared writer admission | **8 control + 16 Atlas requests**, Atlas payload backlog ≤**4 MiB** | Backpressure/refuse before lease after **500 ms**; never drop accepted command | Same command/chunk identity only; different payload conflicts | Control **0**; after **8** controls allow one waiting background request | **1 actual workspace writer** | Lease wait **500 ms**; chunk transaction target **100 ms p95**, watchdog **250 ms** | wait, commit duration, conflicts, rollback |

**The writer row is a required real shared-owner change, not current behavior.** Core must reconcile
existing direct writer calls with this admission contract; a private Atlas queue cannot enforce
global control priority. No preemption inside a SQLite transaction. The 64-record/256-KiB chunk
cap bounds its work; cancellation checked before commit rolls back uncommitted work. The 250-ms
watchdog detects a budget violation and prevents starting more chunks; actual interrupt/rollback
behavior must be demonstrated, not assumed from a stopwatch.

No automatic transient retries in E-0. Operator retry uses idempotency rules. New selection cancels
old reads and increments generation; old responses are ignored even if underlying cancellation
arrives late. User-visible cancellation acknowledgement target **100 ms**, resource-quiescence
target **2 s**. Safe native/SQLite adapter proof must establish the latter; if an operation cannot
be interrupted, report `Cancelling` until ownership ends and block further admission rather than
claiming cleanup completed or leaking unbounded background tasks.

Enumeration consumes a finite traversal budget: **25,000 authorized path observations**, depth
**64**, **128** open/pending directory descriptors maximum, no follow-through of unapproved links.
Every examined entry decrements remaining-entry budget; visited directory identity prevents cycles.
Budget exhaustion is a terminal **partial/BudgetExceeded** outcome, not success.
Semantic collection caps **100,000 declaration occurrences/root** and **25,000/scope**. These
limits do not redefine physical scope or “complete”; disclosure and an explicit larger admitted
profile are required for larger workspaces.

### 6.2 Agreed-workload candidate and acceptance measurements

Baseline acceptance workload: **2,500 authorized visible paths**, up to **20 project/TFM variants**,
**50,000 mandatory-kind declaration occurrences**, includes unsupported/generated/vendor/migration
files and at least two overloaded/partial cross-project collision cases. It is a purpose-built safe
fixture plus an Owner-approved real selectable workspace with recorded actual dimensions.
No private source corpus is copied into the product.

| Axis | Numeric candidate / rationale | Measurement and breach behavior |
|---|---|---|
| Native tree open | **≤2 s p95**, inherited spec target | 30 cold opens; include real store/query/transport/render, not only control construction. |
| Native filter / retained switch | **≤150 ms p95**, inherited spec target | 100 warm interactions each; show exact included population and use virtualization. |
| UI-thread stall | **≤50 ms maximum** during E-0 navigation | Dispatcher instrumentation on the real host; move I/O/serialization off UI thread, not suppress frames. |
| Bounded inventory/outline query | **≤100 ms warm p95**, **≤500 ms cold p95** | 100 warm/30 cold query runs with actual plans, sizes and machine recorded. |
| New Atlas manifest growth | **≤64 MiB** incremental facts/dimensions for one baseline full changed manifest; unchanged refresh **≤512 KiB** for the bounded new seal/receipt while content chunks are reused | Measure DB/WAL deltas after checkpoint under controlled test. Bytes are budgets, not a claim SQLite has no overhead. |
| Atlas evidence storage admission | Warn **512 MiB**, refuse next refresh at **1 GiB** attributable Atlas evidence; preserve existing data | Size estimator plus physical-store guard; do not purge evidence. Operator decides retention or revised budget. |
| Workspace physical headroom | No Atlas new write when store+WAL exceeds **4 GiB** or free space below **1 GiB** | Existing non-Atlas growth is disclosed; Atlas never attributes all DB size to itself. |
| Derived cache | **64 MiB**, ≤**2** manifests, least-recently-used eviction | Cache eviction only; replay canonical equality, explicit cold cost, no source-body retention. |
| Replay | **≤10 s** baseline retained manifest; owned replay memory **≤128 MiB** | Clear caches and reconstruct from real retained facts, record counts and canonical checksum. |
| Owned coordinator/query memory | **≤128 MiB** excluding compiler internals, source buffers accounted separately | Count owned queues/materializations/buffers; unknown compiler memory is reported, not called zero. |
| Compiler process resource guard | Additional process working-set alarm **1 GiB above measured pre-run baseline** | This is a coarse shared-process safety alarm, not exact Atlas attribution; stop admitting new compilation, cancel cooperatively and report uncertainty. No claim of hard isolation. |
| View-local history | **100 entries / 2 MiB**, no bodies | Evict oldest view state visibly; does not delete facts or revoke available historical source. |
| Snapshot retention | **0 automatic evidence purges** | No invented time period. Existing approved retention/rights policy must name authorized maintenance; history becomes unavailable honestly when retired. |

Numbers other than inherited spec targets are **Inferred starting budgets**: 10× traversal headroom,
two source buffers and two compiler jobs bound parallel pressure; 64-record transactions limit
control delay; metadata byte budgets accommodate tens of thousands of declarations without a
mirror of source bodies. They require execution/countersign before product claims.

On any breach, return/emit `AIDE-ATLAS-BUDGET-EXCEEDED` with bounded dimension name, known
observed/limit values, permitted scope and recovery. Preserve last-successful. Offer explicit retry,
authorized scope change, or approved larger profile/maintenance—never auto-purge evidence, silently
hide files or reduce a required test. Scope change produces a new manifest.

Dedicated tables/index evolution is considered only after **three** reproducible budget failures
on the recorded workload plus actual SQLite query plans/replay evidence identifying generic
representation as the cause. First inspect the existing indexed/bounded query shape. One slow UI,
a single noisy timing or the presence of a large DB does not justify schema migration.

### 6.3 Telemetry — numeric cost and privacy limits

Use existing Activity/Meter/structured logging infrastructure; no new observability package.
One Activity per operation: `aide.atlas.refresh`, `.inventory`, `.declarations`, `.stage`, `.seal`,
`.query`, `.source`, `.render`. Transport adapter propagates W3C traceparent/tracestate;
request/selection IDs remain application correlation, not substitutes for trace context.

Metrics: operation count/duration; queue depth/wait/rejections; source bytes read/hashed/returned;
declaration/file counts by permitted category; cache/store growth; replay duration/memory; late
response drops; BudgetExceeded and cancellation/quiescence. Units are explicit (ms, bytes, count).
Correctness/coverage counts are **100% unsampled**.

Allowed metric attributes: operation (≤8), outcome (≤8), budget dimension (≤16), coarse supported
language/category (≤8), schema version (≤2 simultaneously). Do not attach all attributes to every
metric: per-instrument maximum **256 series**, whole Atlas maximum **2,048 series**. Unknown
values map to a fixed `other` bucket; dropped-label/series count is observable.
No path/file/symbol/manifest/session/principal/request ID, hash, raw exception or body in labels.

Normal completion logs: one structured summary per refresh/query, **≤2 KiB/event**, **≤256 KiB/
workspace/minute** via bounded suppression counter; critical state also has unsampled metric and
existing incident channel. Routine unavailable/unsupported is not ERROR. No default per-file logs,
source snippets or model/session text. A test scans captured telemetry for forbidden source/path data.

Sample normal traces at **10%** with deterministic trace-ID sampling; error/budget operation
traces retain **100% metadata-only** subject to the same byte policy. Controlled diagnostic mode
is **disabled in E-0**: no unapproved per-file debug sampling or arbitrary retention promise.
If later enabled, it needs a separately approved purpose/time/byte/sample/retention/redaction
profile. Operational telemetry retention follows the approved existing sink policy; unknown policy
blocks that sink, not the local in-memory metrics.
E-0 model calls/tokens/spend are **0 by construction**; test no model/agent-plane dependencies.

## 7. UI & interaction design — native, inert, view-local

Reuse the accepted **G6 multi-panel data terminal** and existing Windows/WPF guidance. E-0
requires no WebView, diagram control or generated imagery. No new DESIGN.md/token system.
The Shell owner supplies the actual Architecture composition after SH3 handoff.

| Region | State/data and behavior |
|---|---|
| Physical tree | Virtualized project/folder/file hierarchy; one FileId record with separate project aliases/memberships. Filter does not change inventory scope. Unsupported files stay selectable. |
| Outline | Explicit project/TFM chooser when ambiguous; mandatory kinds, overload signatures and partial declaration choices. Unsupported kinds labelled, never parsed from display strings. |
| Source | Exact selected file/version label, read-state badge, active span only if hash/decoder matches; no body retained in history. LiveChanged requires explicit open-live/refresh action. |
| Inspector/status | Root/policy/manifest, scope/revision vector, coverage/denominator/bounds and source availability. UI renders Core truth; no independent count/authority computation. |
| Back/Forward | View-local Memento snapshots: manifest/file/scope/symbol/declaration, lens, selection generation, focus and scroll. Core reauthorizes every restored read/action. No navigation fact stream or mandated new class. |

Synchronization: accept one selection token from the user action; increment generation, cancel
obsolete work and fan results only to views matching manifest+generation. A late tree/outline/source
response cannot win by arriving last. Policy invalidation clears active spans and requires fresh
authorization. Back to missing old bytes shows Unavailable, preserves selection context and disables
old anchors; it never restores current text under an old highlight.

State contract for every interactive element: default, hover, visible focus, selected/active,
disabled-with-reason, loading, empty, filtered-zero, success and error. Tree additionally supports
no workspace/no index, refreshing/stale/partial, overflow path, policy-hidden limitation and busy.
Source adds unavailable/live-changed/unindexed/unsupported-encoding/too-large/read-unstable/cancelled.
Outline adds no declarations, multiple scopes/partials and unsupported-kind gaps.
No spinner without a terminal cancellation/error state.

Tokens already read from DESIGN.md: `{colors.surface}`, `{colors.text}`, `{colors.text-muted}`,
`{colors.text-disabled}`, `{colors.accent}`, `{colors.accent-contrast}`, `{colors.focus}`,
`{colors.border-strong}` and `{colors.border}`. Accent-selected surfaces use **accent-contrast**
ink; focus is the approved outer ring, not low-contrast accent-on-accent. Control boundaries use
border-strong; separators alone use border. No new colors, colormap or arbitrary spacing.
Use existing type/spacing tokens, tabular unit-bearing counts and textual uncertainty—not color alone.

Copy: “Semantic coverage unavailable for this file.” “Source changed since indexing; old anchor
disabled.” “Historical source unavailable; selection retained.” “Showing 128 entries; continue for
more.” “Inventory incomplete: access or budget limited.” “Atlas unavailable until its native
adapter is admitted.” No “all files” where policy/partial state contradicts it.

Keyboard focus route: Architecture rail → tree → outline/tab → source → inspector; Back restores
the prior focus target or nearest valid region, with UIA announcement. UIA name/role/state/value,
keyboard/pointer parity, dark/light/high-contrast, 100/150/200% DPI, window resize and reduced-motion
are required native tests. Hard cuts/no navigation animation. Readability does not depend on motion.
The deterministic UI craft detector/token checks run against the built applicable surface with
manual/UIA coverage for native semantics the detector cannot inspect; a green empty corpus is not proof.
No AI-facing controls or HAX model interactions in E-0.

## 8. Error & concurrency model

Stable registry in `AtlasContracts.cs`, not interpolated messages:

| Error | Disposition and recovery | Falsifier |
|---|---|---|
| `AIDE-ATLAS-INVALID-REQUEST` | Refuse before I/O; bounded field-path diagnostics | Empty IDs, duplicate JSON, invalid span/range, overflow/depth. |
| `AIDE-ATLAS-POLICY-REFUSED` / `EPOCH-STALE` | Refuse; no sensitive path/count/body; reauthorize | Forged root/principal, policy change mid-page or Back. |
| `AIDE-ATLAS-SOURCE-UNAVAILABLE` / `SOURCE-CHANGED` / `SOURCE-UNSTABLE` | Typed source state; disable old spans; refresh/open-live only by user | Deleted/replaced/changing file, hash/decoder mismatch. |
| `AIDE-ATLAS-ENCODING-UNSUPPORTED` | No verified text/spans; retain physical entry | Invalid bytes/BOM or unapproved decoder. |
| `AIDE-ATLAS-SYMBOL-UNRESOLVED` / `SCOPE-AMBIGUOUS` | Show source-safe occurrence or scope choices; never fabricate ID | Null ID, malformed declaration, same file in two TFMs. |
| `AIDE-ATLAS-IDENTITY-CONFLICT` | Reject conflicting record and seal; incident | Canonical tuple hash collision/fault injection or delimiter trick. |
| `AIDE-ATLAS-IDEMPOTENCY-CONFLICT` | Refuse differing payload under same command key | Retry changes root/policy/scope/budget. |
| `AIDE-ATLAS-GENERATION-STALE` | Rollback obsolete chunk/seal; no overwrite | New root desire while old worker stages or seals. |
| `AIDE-ATLAS-HISTORY-UNAVAILABLE` / `CURSOR-INVALID` | Explicit unavailable/expired; no latest substitution | Retired exact chunk, modified filter/policy, expired token. |
| `AIDE-ATLAS-BUDGET-EXCEEDED` / `BUSY` | Partial/refused with measured dimension; preserve old evidence | Queue/file/frame/path/declaration/store/memory/time caps. |
| `AIDE-ATLAS-CANCELLED` / `DEADLINE` | Cancel queued work; quiesce owned resources; unsealed work invisible | Cancellation before queue/lease/commit/reply, blocked I/O. |
| `AIDE-ATLAS-ADAPTER-UNAVAILABLE` / `SCHEMA-UNSUPPORTED` | Disable actual capability; no substitute provider/composition | Missing S-SAFE/S-WIRE/S-HOST or old peer. |
| `AIDE-ATLAS-STORE-FAILED` | Rollback; incident outside store; retain last-successful | Disk full, lock failure, malformed stored payload. |

Errors inside Activities carry trace context and stable code. No raw exception text/path leakage.
RFC 9457 is **not applicable**: E-0 introduces no HTTP endpoint. A future HTTP adapter would
need Problem Details rather than reusing this statement as permission to omit them.

## 9. Failure-mode analysis

| Mode from chosen design | Disposition | Test / telemetry |
|---|---|---|
| Inventory changes during scan or has missing/cyclic/inaccessible entries | Detect membership/visited/budget failures; partial outcome, no fake complete | T02/T03/T19; inventory outcome/count limits. |
| Semantic loader reads bytes different from indexed hash | Prevent bundle mismatch; reject declaration binding | T06/T07; binding mismatch counter. |
| Scoped logical keys accidentally include revisions or exclude TFM/project | Prevent canonical tuple validation; regression | T04/T05; identity conflict diagnostic. |
| Old partial/source span chosen arbitrarily | Prevent full declaration-choice contract; no active mismatch | T05/T07/T17. |
| Duplicate/natural-key collision on reindex | Reuse exact chunks/receipts; reject differing payload | T08/T09. |
| Staged chunks commit but seal fails/crash occurs | Unselected staging stays invisible; root fence and last-successful | T09/T10; failed-seal/rollback counter. |
| Mixed scopes appear current/coherent | Exact manifest vector plus explicit separate states | T10/T11; manifest state metrics. |
| Shared synchronous writer blocks cancellation | Do not admit S-WRITE until real bounded seam proven | T12/T13; wait/quiescence watchdog. |
| Cache uses current generation or wrong policy | Key by manifest/policy/schema; validate on read; cache replay | T11/T14. |
| History compaction removes referenced facts during read | Coordinate maintenance or explicit HistoryUnavailable | T14; missing-reference incident, no substitution. |
| Payload/cursor truncation loses critical state | Bound whole serialized result and reject invalid envelope | T15/T16. |
| Native composition unreachable/late responses overwrite | Real owner registration plus generation guard | T17/T18. |
| Store/cache/log memory grows without bound | Numeric profile, warning/refusal; caches only evicted; no evidence purge | T19/T20/T21. |
| Cleanup returns success before handles/tasks stop | Quiescence test and explicit Cancelling state | T07/T13; active owned-resource gauge. |

## 10. Adversarial analysis (STRIDE-lite)

| Boundary | Threat(s) | Disposition / named control | Negative oracle |
|---|---|---|---|
| UI/IPC → authority | Spoofing/elevation: forged principal/root/epoch; tampered selector | Mitigate: S-AUTH derives context; typed CommandGateway reauthorizes, no content-issued actions | T01/T16/T18. |
| Root/filesystem → inventory/source | Tampering/disclosure/elevation: symlink/junction/hard-link/replacement escape | Transfer mechanism to **S-SAFE executed worker contract**; Atlas validates state/binding and refuses unadmitted adapter | T02/T07; block release until real native adversarial run. |
| Project inputs → compiler | Elevation/DoS: project targets or oversized hostile syntax | Approved Core compilation bundle only; no arbitrary build/task evaluation; bound byte/decl/time queues | T06/T19. |
| New records → store/global readers | Tampering/repudiation/DoS: forged scope, duplicates, graph pollution, false completed seal | Closed predicates, root fence, receipt/payload check, real S-COMPAT isolation test | T08–T11/T22. |
| Query/cursor → historical reads | Disclosure/tampering: cursor widens scope, hidden-count arithmetic | Authenticated bounded cursor, current policy, exact references, withheld denominator | T15/T16. |
| Source/spec strings → native UI | Elevation: command URI/script/clipboard trigger; disclosure | Inert text; TrustedGestureCommandBroker accepts typed IDs from actual gesture, not content links | T18. |
| Queues/telemetry → operator | DoS/repudiation/disclosure: saturation, misleading success, private labels | Bounded profile, typed BudgetExceeded, unsampled correctness metrics, forbidden-data scan | T12/T19/T21. |

No threat is accepted merely because it is local. Source safety and global compatibility are
explicit unclosed transfers until their evidence and owners join, not claims of mitigation already run.

## 11. Privacy analysis (LINDDUN-lite)

Work data can contain personal identifiers in paths, signatures, comments and project metadata.
E-0 processing purpose is **local understanding of the selected authorized workspace**. The actual
basis/authorization comes from approved workspace policy, not this text or a blanket permission
inferred from the user's request. No E-3 history harvesting, E-4 model context or public export.

| Risk | E-0 disposition / retained categories | Test / admission |
|---|---|---|
| Linkability | Workspace-scoped file/symbol IDs and hashes stay local; no cross-workspace identity index or telemetry labels | T16/T21. |
| Identifiability | Minimize names/paths to allowed UI/store purpose; hidden metadata/counts withheld | T02/T16/T21. |
| Non-repudiation | Minimal principal/command receipt for accountable writes; no wholesale user/session history | T08; existing authorized receipt retention policy. |
| Detectability | Hidden-file existence cannot be reconstructed from totals/omissions/cursors | T02/T16. |
| Disclosure | Bodies transient in approved buffers; no source logs, telemetry snippets, private fixture export or second body cache | T07/T14/T21. |
| Unawareness | UI shows root/policy, physical-v-semantic coverage, retained metadata and body unavailability | T17/T18; native content review. |
| Non-compliance | Reuse approved per-class purpose/retention/rights policy; unknown policy blocks persistence/sink; no arbitrary duration or auto-purge | Privacy countersign before product metadata writes. |

Lifecycle requirements:
- Source buffers: release on completion/cancel/error under the actual S-SAFE disposal contract.
  Do not claim secure memory zeroization or kernel cache erasure without proof; no durable copy.
- Metadata/hashes/declarations/manifests: existing approved workspace retention basis and authorized
  retirement/delete mechanism must cover these classes; until it does, persistence admission is blocked.
- Derived caches/history: disposable within numeric limits, clear on workspace close/policy revocation;
  no assertion that clearing a view deletes retained facts.
- Logs/receipts: approved sink/receipt retention and access/rectification/deletion handling,
  including caches/backups/derived manifests. A tombstone is not byte deletion.
- Rights requests route to the accountable workspace/data owner and admitted maintenance mechanism;
  append-only facts are not rewritten by an Atlas feature to simulate compliance.

## 12. Test plan — concrete oracles, not an executed Proof Pack

Testing Strategy union: **D0** all tests; **D1** pure/state logic and mutation; **D2** canonical
encoding/parsers/serializers/wide ranges; **D3** new namespace/dependency/composition; **D4** real
SQLite/filesystem/queues/native resources; **D5-provider** exposed query contract; **D6** payload/
fact schema golden compatibility; **D7** every fake paired with the real admitted adapter.
**D5-consumer** is not triggered by an HTTP/gRPC consumer here; use provider/payload and real
local-IPC compatibility proof instead. **A1–A6** are not triggered: no model/MCP/tool/prompt
capability is added. This design document is not a shipped model prompt.

| ID / proposed test class | Assertions and red mutation | Real/substitute boundary |
|---|---|---|
| T01 `AtlasContractValidationTests` | Reject malformed IDs, enum integers, duplicate/deep JSON, missing critical fields, overflows; remove a validator to observe red | Pure serialized records. |
| T02 `AtlasInventoryPolicyTests` | Unsupported/generated/vendor/migration remain; hidden counts withheld; linked project memberships do not duplicate FileId; remove nonsemantic entries ⇒ red | Safe fixtures plus real admitted enumeration pair. |
| T03 `AtlasInventoryBoundaryTests` | Missing tracked/inaccessible/cycle/case-collision/nonGit/untracked rules and partial finish; mutation marks cap complete ⇒ red | Real files, safe worker adapter; isolated cleanup. |
| T04 `AtlasIdentityTests` | Cross-project/TFM collisions prevented, whitespace/line shift preserves SymbolId; delimiter/unicode/culture properties; remove scope or add revision ⇒ red | Roslyn 4.14 synthetic inputs and canonical golden bytes. |
| T05 `AtlasDeclarationTests` | Every mandatory family, overload/property/accessor distinction, partial definition+implementation; first-only partial or parsed display key ⇒ red | Installed Roslyn with real syntax inputs. |
| T06 `AtlasCompilationBindingTests` | Real project options/reference/TFM preserved; text/hash mismatch, unsupported language/version/null ID, virtual generated inputs disclosed | Fake bundle pair plus actual Core loader integration; no synthetic product fallback. |
| T07 `AtlasSourceSafetyContractTests` | Escape/link/replacement/concurrent write, changed bytes, BOM/CRLF/non-BMP, invalid encoding, byte/UTF-16 bounds, cancellation/handles | **Source-worker real Windows adapter required**, not a mocked proof. |
| T08 `AtlasStoreIdempotencyTests` | Same request/chunk retry returns original; A→B→A reuses the original immutable A chunk without a natural-key conflict; different fingerprint conflicts; unit separator cannot collide | Real disposable WorkspaceStore. |
| T09 `AtlasStoreFencingTests` | New root desire defeats old chunk/seal; dispose rollback; fault between chunks/seal/reply; duplicate facts cannot be hidden by upsert | Real SQLite; fault injection at named boundaries. |
| T10 `AtlasManifestTests` | A-new/B-old/failed, shared-file hash mismatch, partial/unknown independent of seal-row complete flag | Real stored observations; remove vector check ⇒ red. |
| T11 `AtlasPinnedReadTests` | Current-source helper cannot leak into old manifest; exact generation/decoder/coverage survives all reads | Real retained generations; insert newer rows mid-query. |
| T12 `AtlasAdmissionTests` | Saturate numeric queues; latest unstarted coalesces only; control fairness 8:1, bounded writer acquisition; bypass shared gate ⇒ red | Deterministic scheduler plus actual S-WRITE integration. |
| T13 `AtlasCancellationTests` | 100-ms acknowledgement/2-s quiescence, cancel at queue/lease/precommit/postcommit; no dangling tasks or late UI overwrite | Controlled scheduler and real filesystem/SQLite pair. |
| T14 `AtlasReplayAndRetirementTests` | Clear caches, exact canonical replay; remove retained referenced chunk ⇒ HistoryUnavailable, not latest; mutate current label ⇒ old view unchanged | Real DB/cache/maintenance seam. |
| T15 `AtlasWireGoldenTests` | All scope/revision/hash/span/contentState/coverage/bounds fields survive; frame/UTF-8 pages/Int64 strings/cursor ties; remove one field ⇒ red | New serializer tests first; **actual old/new IPC peers later**. |
| T16 `AtlasAuthorizationCursorTests` | Forged/replayed/expired/cross-policy cursor, principal/epoch/policy revoke, hidden denominator privacy | Real authority adapter; test tokens not production authority. |
| T17 `AtlasNativeJourneyTests` | Real rail→tree→scope/outline→partial/member→source→Back, matching identity/focus/scroll; old body unavailable disables anchor | **Actual owner-registered host/client/store**, never manual test-only factory. |
| T18 `AtlasNativeStatesTests` | Full states, UIA/keyboard/pointer/themes/DPI/resize, inert URI/script content; late selection response rejected | Actual native controls and broker; applicable craft/token checks. |
| T19 `AtlasBudgetTests` | Every numeric boundary at N−1/N/N+1; no successful complete on cap; larger data requires new admitted profile | Units plus real workload/performance protocol. |
| T20 `AtlasGrowthPlanTests` | Physical/attributable growth, replay, 64-record transactions and actual EXPLAIN plans at workload; no auto-purge | Real SQLite with retained iterations and measured byte deltas. |
| T21 `AtlasTelemetryPrivacyTests` | Trace context, stable code/unit, unsampled coverage, series/log caps; seeded private path/body never emitted | Captured real Activity/Meter/log sinks and native operation. |
| T22 `AtlasLegacyCompatibilityTests` | Atlas persisted predicates do not change existing graph/evidence behavior; disable capability rollback leaves legacy operable | **Shared-owner serial test handoff**, no live SH3 test edits by this track. |

All new test files belong under `tests/AiDe.Core.Tests/Understanding/` or, only when explicitly
admitted, `tests/AiDe.App.Tests/Workbench/Understanding/`. Existing forbidden tests are not edited.
Each new control is observed red by old behavior or an intentional mutation before claiming green.
Use deterministic clocks/fault points rather than sleeps. Temporary fixture resources are isolated
inside approved test-owned project artifact paths, disposed and counted before/after; no private corpus.

Native performance protocol records hardware/OS/runtime/versions, root policy, dimensions, cold/warm
definition, all sample values and p95 by nearest-rank `ceil(0.95*n)`. No claim is made from an exit code:
read TRX counts, serialized data, actual store state, normal-path telemetry and rendered native state.

## 13. Exact smallest vertical worklist and serialized handoffs

Each row is an independently demonstrable vertical **subset of the same E-0 path**, not a future
Atlas phase. Authoring permission is separate and still pending. Implementation delegates are
expected to be GPT-5.5 after Conductor/Owner admission; no child agents were launched here.

| Slice | Authored new paths / bounded task | End-to-end exit, real vs substitutes | Dependency / owner handoff |
|---|---|---|---|
| **E0-V1 one authorized physical file** | Core Understanding `AtlasIdentity.cs`, `AtlasContracts.cs`, `AtlasPorts.cs`, `AtlasInventory.cs`, minimal `AtlasObservationWriter.cs`/`AtlasQueries.cs`; associated T01/T02/T08/T15; new native view files only if admitted | One actual allowed unsupported text file appears through real SQLite/read DTO/native tree/source or typed unavailable. Source/writer fakes prove unit contracts only; real native composition required before deployable slice exit. | S-SAFE, S-WRITE, S-AUTH, S-COMPAT and first serialized S-WIRE/S-HOST installation. Core/Shell retain all shared edits. |
| **E0-V2 one project and mandatory declarations** | `AtlasDeclarations.cs`, bounded `AtlasRefresh.cs`; scope/outline additions in the same new files; T04–T07/T11/T17 | Real project/TFM file → type → overload/property/accessor/constructor → chosen partial declaration → bound source; unsupported files remain. No no-reference compiler in product. | S-COMP actual loader; same admitted writer/query/native path, not a second composition. |
| **E0-V3 history, concurrency and mixed refresh** | Finish manifest/queue/cursor/source continuation and view-local Back in existing new Understanding files; T03/T09–T16/T18 | Index then edit, cancel refresh, A-new/B-failed, policy revoke, restart and Back. Real facts/IPC/source/native state agree; fault injection only at failure boundaries. | Writer-admission/compaction/transport cancellation handshake proven; shared changes serialized by owners. |
| **E0-V4 workload and admission proof** | No new abstraction by default; bounded fixes in admitted new files, T19–T22 and proof artifact authored by Conductor | Approved real workload meets named limits; data beyond limits fails honestly; replay/rollback and legacy compatibility demonstrated; human keyboard/pointer demo completed. | Independent design/implementation review, source-safety and Owner horizon gate. E0 closure does not admit E-1…E-4. |

Shared-path hold list, exact as instructed:

- `src/AiDe.Core/Ipc/WorkspaceClient.cs`, `WorkspaceOperations.cs`;
- `src/AiDe.App/Workbench/SurfaceContentFactory.cs`, `WorkbenchShell.cs`, `WorkbenchAdapter.cs`;
- existing Canvas, Evidence, GraphProjection, ZoneLayout files and their tests.

No writer claims these paths through a new directory or integration helper. S-WRITE may require
a narrow `WorkspaceStore` admission addition and S-COMP a real Core project-input adapter; those
are separate owner-authored change requests, not authorization included in this document.
No fake host, alternate daemon, manual production test composition or additional transport is
introduced to make a parallel track appear complete.

**Concrete join payload to Conductor:** versioned request/result fixtures, canonical identity/fact
goldens, required capability/error vocabulary, exact shared registration command names, wire byte
ceiling, authenticated context/cursor and cancellation contracts, single-writer admission policy,
legacy vocabulary isolation test, native view constructor dependency (`IAtlasQueries`) and full
native journey script. Acknowledge and serialize these before any shared edit.

## 14. Patterns, conformance and verification disposition

Solution ladder: the behavior is required; reuse existing store/Unit of Work/authority/query/native
host first; use installed Roslyn/System.Text.Json/.NET value types next; select no new package or
generic framework. Patterns: **Ports and Adapters**, existing narrow **Repository/Unit of Work**,
**ImmutableObservation + ManifestSeal**, **CQRS** read separation, view-local **Memento**
(restoration only), **Generation-Token/request correlation**, typed **CommandGateway/
TrustedGestureCommandBroker**, bounded producer/consumer admission and explicit idempotency.
No Event Sourcing framework, event bus, generic workflow engine or model orchestration.

LOA **F / T0 hot path** only. P1/P2 deterministic floor; P3 human-action boundary; P4 typed
operations; P5 hash/fence/schema validation; P6 independent admission; P7 state in existing store
and view-local UI; P8 receipts; P9 typed wire; P10 normal-path measured telemetry; P11 principal/
policy revalidation. C1–C3 model-call requirements are N/A because E-0 has no model calls;
C4/C5/C6/C7/C8/C9/C10/C11 apply to typed boundaries, verified commands, idempotency, fallback,
named patterns, anti-pattern absence, audit and principal propagation. No implementation
conformance is self-certified by this design.

### 14.1 Design checklist disposition

The design-slice definition-of-done was read. This document supplies responsibility, model/grain/
history/reader trace, E7 path, phase placement, local contracts, patterns/ladder, failure/STRIDE/
LINDDUN tables, native state/token contract, telemetry, numeric budgets and complete test union.
It deliberately **does not tick executed/independently reviewed items**:

- “Every consumed contract established; unfamiliar ones spiked” remains open for S-SAFE,
  S-COMP, S-WRITE and the live transport/host/history compatibility joins.
- “Append-only/interval invariants enforced and tested,” “rebuild test,” “every control red-first”
  and native/performance/telemetry execution await implementation, despite existing K0 baselines.
- “Patterns survives both reviewers” and “hard vetoes resolved” require focused design admission;
  whole-architecture content clearance does not automatically approve this new numeric/record design.
- Existing DESIGN.md is reused; no token change/new visual asset. Render/craft/contrast proof
  applies to actual new controls at owner integration, not a documentation-only mock.
- Global index/audit/change/security/privacy rollup writes are **Conductor's join task** under this
  one-file authoring restriction. They were not run here; no shared artifact is silently changed.

`GATE atlas-e0-design-content · 2026-09-12 · independent focused review via Conductor/Owner · criteria: concrete bounded writer/read/native contracts and numeric profile accepted, S-SAFE/S-COMP/S-WRITE and shared joins dispositioned · verdict: PROPOSED, REVIEW PENDING · author does not clear hard vetoes.`

`GATE atlas-e0-authoring · 2026-09-12 · Owner + Conductor/Core/Shell · criteria: exact new-path authoring permission and shared serialized handoffs recorded, current-main/SH3 responsibilities reconciled · verdict: NOT GRANTED BY THIS DOCUMENT.`

`GATE atlas-e0-delivery · 2026-09-12 · Test/Security/Distributed/Privacy/native + Owner · criteria: real adapter and native path, red-first controls, workload/telemetry/replay/rollback readback, no fake production seam · verdict: UNEXECUTED.`

## 15. Uncertainties, residuals and handoff

| Remaining join / uncertainty | Smallest resolving action, not broad new research |
|---|---|
| Windows handle/link/race/decoder/cancel contract | Join worker `90e3a4be`'s actual evidence and check S-SAFE fields/budgets; no inferred API. |
| Real compilation input exposure | Core owner confirms exact loader adapter and project trust policy, including text/hash identity; no declaration parsing fallback. |
| Bounded writer acquisition/control fairness | Owner/Core admit the narrow real shared-writer change; current synchronous BeginWrite cannot prove the selected timeout. |
| Shared wire/host and generic-reader safety | SH3 owner serially supplies S-WIRE/S-HOST/S-COMPAT after acknowledgment; run the real native/legacy contract tests. |
| Query plans/growth/resource assumptions | Execute the named safe workload/profile before accepting numeric claims; schema evolution only on the explicit repeated-evidence trigger. |
| Retention/rights coverage for new metadata | Privacy owner confirms existing policy covers E-0 classes and approved sink/maintenance behavior. No arbitrary retention period is supplied. |
| Cursor token facility | Authority owner binds the specified authenticated cursor contract to its actual approved facility; no new crypto provider selected here. |

The next action is **focused E-0 design/authoring admission and the explicit joins**, not another
whole-architecture council and not abandonment while Owner resolves coordination. This design
provides executable task boundaries and falsifiers now; it does not mislabel incomplete real adapters
as production wiring. Later Atlas phases are outside this worklist.

| Completed | Remaining | Best next action |
|---|---|---|
| Concrete E-0 records, canonical IDs, generic fact/manifest write algorithm, versioned read/wire contracts, numeric candidate budgets, native state contract and T01–T22 matrix. | Focused design/budget countersign, real safety/compiler/writer/SH3/history joins and explicit authoring admission; all execution/native proof. | Conductor/Owner admit the exact E0-V1 new-path task and serialize its required real adapters; continue the bounded E-0 worklist without reopening the full vision. |
