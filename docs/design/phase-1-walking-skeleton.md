---
id: design-phase-1-walking-skeleton
title: "Phase 1 walking skeleton — detailed design"
type: design
status: in-review
owner: "@timianmalloo"
phase: "1"
tags: [design, phase-1, walking-skeleton, fact-store, mcp, dispatch, projections]
links:
  - { to: architecture, rel: implements }
  - { to: spec-ai-native-ide, rel: implements }
  - { to: conceptual-model-ai-native-ide, rel: refines }
  - { to: adr-0002-workspace-fact-store, rel: depends-on }
  - { to: adr-0009-in-process-first-daemon, rel: depends-on }
  - { to: adr-0010-two-phase-dispatch-receipt, rel: depends-on }
  - { to: adr-0011-session-processing-class-egress, rel: depends-on }
  - { to: threat-model-ai-native-ide, rel: depends-on }
  - { to: privacy-review-ai-native-ide, rel: depends-on }
review-by: 2027-02-26
review-suggested:
  - { by: spec-ai-native-ide, on: 2026-08-26, reason: "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice" }
summary: >-
  The implementable blueprint for AI-DE's Phase-1 walking skeleton: the SQLite fact schema and its
  enforced immutability control, the in-process authority core with command receipts and the
  write-ahead two-phase dispatch, the bounded describe/impact/knowledge projections, the stdio MCP
  gateway with processing-class egress binding, the health sidecar and freshness prober, and the
  accessible evidence/provenance pane.
---

# Design: Phase 1 walking skeleton

- **Status:** In review
- **Spec / architecture:** [`docs/specs/ai-native-ide.md`](../specs/ai-native-ide.md) · [`docs/architecture.md`](../architecture.md)
- **Delivery phase / vertical slice:** **Phase 1** of the architecture's phasing plan — the thinnest
  end-to-end path that touches every layer. **Real:** WPF shell + accessible evidence/provenance
  pane, in-process authority core, SQLite fact store, in-process fixture extractor, query/projection
  service (describe · impact · knowledge), stdio MCP `describe`/`find`, write-ahead dispatch receipt,
  health incident sidecar, freshness prober. **Mocked (each a contract, substituted not redesigned):**
  `ITerminalSession` (a fixture session that records bytes instead of owning a PTY), `IExtractor`
  (fixture adapter standing in for Roslyn/Bicep/DDL), the WebView2 graph canvas (the accessible
  list/tree is the Phase-1 surface and remains the a11y equivalent forever), and the separate daemon
  process + IPC (ADR-0009 — the core is in-process here, behind the same command contract).
- **Author(s) / date:** @timianmalloo · 2026-08-26

## Responsibility

Prove the composition: a repository artifact becomes a provenance-labelled fact, a fact becomes a
bounded projection, a projection reaches both a human (accessible pane) and an agent (MCP tool) under
policy, and a user-confirmed prompt reaches a session with a truthful receipt — with every claim
carrying source revision, provenance, and a confidence label.

**Not** responsible for: real language extraction (Phase 2), real ConPTY (Phase 2), the graph canvas
(Phase 2), cross-version IPC/upgrade (Phase 2), audit/coordination readers (Phase 4), traces (Phase 5).

---

## Data model (settled first — DM1–DM18)

### Bounded context and aggregates

Phase 1 touches three of the five contexts in the [conceptual model](conceptual-model.md): **Workspace
Registry**, **Evidence and Projection**, **Agent Operations**.

| Aggregate (root) | The one invariant it protects | Cross-aggregate rule |
|---|---|---|
| **Workspace Registry** (Workspace) | A canonical filesystem identity has at most one active membership; every command carries the current workspace epoch. | Others reference `WorkspaceId` + epoch by identity only. |
| **Scope Snapshot** (Scope) | Only the currently *desired* generation **and** authoritative artifact revision may become the committed generation. | Extractors submit immutable assertions keyed by scope/revision; the writer rejects a stale pair atomically. |
| **Relationship Claim** (Claim) | A displayed claim has ≥1 attributable assertion with compatible normalized subject/predicate/object. | Claims reference assertions by identity; assessment derives from selected snapshots. |
| **Prompt Dispatch** (Prompt Draft) | A dispatch binds exactly one immutable revision, workspace epoch, session ID+generation, and dispatch key — **and a durable attempt exists before any byte leaves the process**. | Delivery is one at-most-once attempt; resend after unknown requires a new human-confirmed key. |

One aggregate per transaction. The only cross-aggregate coupling is by identity.

### Durable representation (ADR-0002)

One SQLite file per workspace: **dimensions** for stable identities, **append-only facts** for change
over time, **labelled rebuildable caches** for current state.

**Grain declarations** — *one row is exactly one …*:

| Table | Grain: one row is exactly one… | Identified by | Recorded when |
|---|---|---|---|
| `workspace_dim`, `repository_dim`, `artifact_dim`, `node_dim`, `session_dim` | current version of one stable business identity | natural ID + surrogate `<entity>_key`; `valid_from_seq`/`valid_to_seq` | on registration or a meaning-changing attribute change |
| `scope_generation_desired_fact` | **request** to (re)extract one scope at one desired generation and authoritative artifact revision | `{workspace, scope, generation}` | when the scheduler enqueues |
| `scope_snapshot_committed_fact` | **commit** of one complete snapshot for one scope generation | `{workspace, scope, generation}` | inside the commit transaction |
| `evidence_assertion_fact` | assertion by one extractor about one normalized (subject, predicate, object) relation at one artifact revision | deterministic `assertion_id` (hash of scope+revision+s+p+o+extractor) | in the snapshot commit transaction |
| `command_receipt_fact` | one **completed** mutating command for one idempotency key | `{workspace, caller_principal, command_type, command_id}` | in the same transaction as its effect |
| `dispatch_attempt_fact` | one **attempt** to deliver one prompt revision to one session generation | dispatch key (≡ `command_id`) | **before** the PTY write |
| `dispatch_outcome_fact` | one **outcome event** for one dispatch key | `{dispatch_key, ingress_seq}` | after the write, or by the recovery sweep |
| `prompt_revision_fact` | one saved revision of one draft | `{draft_id, revision_no}` | on save |
| `health_incident` *(sidecar, not the DB)* | one occurrence-collapsed incident class for one scope | `{class, scope}` + occurrence count | on detection |

**Why `command_receipt_fact` is one grain but dispatch is two:** an ordinary command's effect and its
receipt commit in **one** SQLite transaction, so acceptance and completion are atomic — one row is
truthful. A dispatch's effect is a **PTY write outside any transaction**, so attempt and outcome are
separated in time and *must* be two grains; collapsing them is exactly the crash window ADR-0010 closes.

**Additivity (DM9).** There are no additive business measures. `assertion_count` on a snapshot is
**semi-additive**: additive *within* one snapshot, **non-additive across snapshots of the same scope**
(a later snapshot supersedes rather than adds). Summing assertion counts across generations is the
standing category error here and is prevented by always selecting the latest committed generation.

**History rule per attribute (DM10)** — Phase-1 dimensions:

| Dimension | Type-2 (history-preserving) | Type-1 (overwrite — a recorded decision to discard history) |
|---|---|---|
| `workspace_dim` | `root_path`, `epoch` — a changed root rewrites what past assertions meant | `display_name` (cosmetic only) |
| `repository_dim` | `canonical_root` | `display_name` |
| `artifact_dim` | `relative_path` (a moved artifact changes the meaning of its assertions) | `content_type` |
| `node_dim` | `node_kind` (source ↔ knowledge changes interpretation) | `display_label` |
| `session_dim` | `generation`, `processing_class` (an egress decision must never be re-read under a newer class) | `display_name` |

**Derive-don't-store (DM7).** Stored: assertions and events. **Derived**: the current claim set, the
current graph, node neighbor counts, and the health view. `claim_current_cache` is a **labelled,
rebuildable cache**, proven equal to its derivation by a rebuild-equality test (below).

**Enforced invariants (DM11), spike-verified.** Fact tables carry `BEFORE UPDATE`/`BEFORE DELETE`
`RAISE(ABORT)` triggers; **every writer connection sets `PRAGMA recursive_triggers=ON`** and the writer
**forbids `INSERT OR REPLACE`/UPSERT on fact tables** — without the pragma, REPLACE silently deletes a
fact without firing the trigger (`spikes/sqlite-fact-store` S4; S5 proves the pragma closes it). Read
connections set `PRAGMA query_only=1` (S6). Foreign keys on every connection. The **single-writer core
is the real boundary**; triggers/pragmas are defense-in-depth.

**Migration (DM16).** Phase 1 creates v1; the migrator is expand→migrate→move-reads→contract with a
forward and a down script per version, the down path exercised on a copied fixture database. No backfill
guesses: an unassignable value quarantines the row.

### Change surfaces this data must reach (E7)

`store` (SQLite tables + triggers) → `domain model` (`EvidenceAssertion`, `RelationshipClaim`,
`DispatchReceipt`) → `service` (`IWorkspaceStore`, `IProjectionService`, `ICommandDispatcher`) →
`projection/wire` (`DescribeResult`, `ImpactResult`, `KnowledgeResult`, MCP JSON schemas) →
`client type` (`EvidenceItemViewModel`, `ProvenanceViewModel`) → `UI` (evidence list + provenance pane
+ health strip) → `compute reader` (health view aggregates; freshness drift). Implementation ticks each
off; a projection that stops at the service layer is an incomplete change.

---

## Contracts

### Exposed

```csharp
// AiDe.Core.Store
public interface IWorkspaceStore                       // one workspace, one SQLite file
{
    long CurrentEpoch { get; }
    IWriteTransaction BeginWrite();                    // single writer; recursive_triggers=ON
    IReadSnapshot BeginRead();                         // query_only=1, snapshot isolation
}

// AiDe.Core.Extraction — the mocked seam (fixture in P1, Roslyn in P2)
public interface IExtractor
{
    string ScopeKind { get; }                          // "fixture" | "csharp" | "knowledge"
    Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken ct);
}
public sealed record ExtractionRequest(string WorkspaceId, string ScopeId,
    string ArtifactRevision, long DesiredGeneration, ExtractionTrigger Trigger);
public sealed record ExtractionResult(IReadOnlyList<EvidenceAssertion> Assertions,
    bool Complete, IReadOnlyList<ExtractionDiagnostic> Diagnostics);

// AiDe.Core.Projections — every result is bounded and self-describing
public interface IProjectionService
{
    DescribeResult   Describe(WorkspaceRef ws, string nodeId, int maxNeighbors);
    ImpactResult     Impact(WorkspaceRef ws, string nodeId, int maxNodes, int maxEdges, string? cursor);
    KnowledgeResult  Knowledge(WorkspaceRef ws, KnowledgeQuery query);   // US-4
    FindResult       Find(WorkspaceRef ws, string term, IReadOnlyList<string>? kinds, int maxResults);
}
public sealed record ResultBounds(int MaxNodes, int MaxEdges, int MaxBytes,
    int ReturnedNodes, int OmittedNodes, int ReturnedEdges, int OmittedEdges,
    bool ByteCapped, string? NextCursor);
// Every *Result carries: ResultBounds, SourceRevision, IReadOnlyList<Provenance>, Confidence.

// AiDe.Core.Commands — same shape in-process now, over IPC at Phase 2 (ADR-0009)
public interface ICommandDispatcher
{
    Task<CommandOutcome> ExecuteAsync(CommandEnvelope envelope, CancellationToken ct);
}
public sealed record CommandEnvelope(int ProtocolVersion, string WorkspaceId, long WorkspaceEpoch,
    CallerPrincipal Caller, string CommandType, string CommandId,
    DateTimeOffset Deadline, string? TraceParent, object Payload);

// AiDe.Core.Terminal — the mocked seam (fixture session in P1, ConPTY in P2)
public interface ITerminalSession
{
    string SessionId { get; }
    long Generation { get; }
    SessionProcessingClass ProcessingClass { get; }
    // Compares `expectedGeneration` to the live generation ATOMICALLY with the write,
    // on this session's single owner loop. Never retargets.
    Task<PtyWriteResult> WriteAsync(long expectedGeneration, ReadOnlyMemory<byte> bytes, CancellationToken ct);
}
```

**MCP tools (stdio, `ModelContextProtocol` 2.2.0)** — `describe` and `find` in Phase 1, each
node/edge/**byte**-bounded, each result carrying provenance, confidence, omission counts, and
**authorship origin**; both authorized against the caller session's processing class (ADR-0011).

### Consumed

| Contract | Source / spike | Confidence |
|---|---|---|
| `Microsoft.Data.Sqlite` 10.0.11 — WAL, unique rejection, recursive CTE, `recursive_triggers`, `query_only`, no nested tx | `spikes/sqlite-fact-store` RESULT.md (S1–S8, 2026-08-26) | **Verified** (cases only; 50k-edge scale is P1-PERF) |
| `ModelContextProtocol` 2.2.0 stdio — typed tool registration, 2026-07-28 negotiation, `isError` on invalid call | `spikes/mcp-server -- client` RESULT.md (M1–M4) | **Verified** |
| AspNetCore MCP HTTP accepts hostile `Origin` | `spikes/mcp-server -- http` (H1) | **Verified — HTTP not enabled in Phase 1** |
| .NET 10 `System.Threading.Channels`, `SemaphoreSlim`, `Activity` (tracing) | stdlib; already installed | Verified |

---

## Patterns

Climbed the Solution-Selection Ladder before naming any of these — **no new NuGet dependency is
introduced in Phase 1** beyond `Microsoft.Data.Sqlite` and the MCP SDK, both already spiked and
required by architecture decisions. Channels, `SemaphoreSlim`, `Activity`, and `System.Text.Json` are
stdlib (rung 3), so no library is taken for queuing, locking, tracing, or serialization.

| Pattern | Where | Justification / rejected alternative |
|---|---|---|
| **Append-Only Evidence Ledger** | fact store | History *is* the data. Rejected mutable graph rows (destroys provenance). |
| **Write-Ahead Receipt / Two-Phase Delivery** (LOA P8) | dispatch | The only way at-most-once is *true* across a crash. Rejected record-after-write. |
| **Idempotent Consumer / Command Receipt** | all commands | Retry returns the original outcome. Rejected check-then-act. |
| **Snapshot Replacement** | ingestion | Latest complete snapshot only. Rejected partial-delete-then-insert. |
| **Materialized View / CQRS read model** | projections | Reads are rebuildable bounded projections. Rejected renderer-queries-facts. |
| **Repository + Unit of Work** (narrow) | `IWorkspaceStore` | One transaction per aggregate; SQLite has no nested tx (spike S8). Rejected ambient transaction scope. |
| **Policy-Bound Egress** | MCP gateway | Authorization follows the session's processing class, not the transport. Rejected loopback-only. |
| **Circuit Breaker (per-scope quarantine)** | ingestion | Bounds a pathological scope. Rejected unbounded retry. |
| **Bulkhead + prioritized single writer** | writer loop | Control preempts between chunked ingestion transactions. Rejected one unbounded FIFO. |

`simplify:` markers planned for Phase 1 (ceiling + upgrade trigger recorded inline in code):
- `simplify: in-memory neighbor index rebuilt per query; ceiling ~50k edges; upgrade when P1-PERF p95 impact > 250ms`
- `simplify: freshness prober walks all scopes each tick; ceiling ~200 scopes; upgrade to a dirty-set when scope count exceeds that`

---

## Data shapes

```csharp
public sealed record EvidenceAssertion(
    string AssertionId,          // deterministic: SHA-256(scope|revision|subject|predicate|object|extractor)
    string ScopeId, string ArtifactRevision,
    string Subject, string Predicate, string Object,
    EvidenceOrigin Origin,       // Static | Runtime
    VerificationStatus Status,   // Verified | Inferred | Unverified   (never collapsed into one word)
    Provenance Provenance);      // artifact path-id, source location, extractor id+version, observed-at

public enum SessionProcessingClass { LocalOnly, ExternalProcessing, UnknownProcessing }
public sealed record CallerPrincipal(string Id, CallerKind Kind);  // STABLE across connections/epochs
public sealed record DispatchReceipt(string DispatchKey, DispatchState State, /* folded */ …);
public enum DispatchState { Pending, PtyWriteAccepted, Rejected, TimedOut, Failed, DeliveryUnknown }
```

**Stable error codes** (RFC 9457 `type` slugs where an HTTP surface later exists):
`AIDE-AUTH-EPOCH-STALE`, `AIDE-AUTH-CALLER-DENIED`, `AIDE-AUTH-CROSS-WORKSPACE`,
`AIDE-PATH-CONTAINMENT`, `AIDE-STORE-IMMUTABLE-VIOLATION`, `AIDE-SCOPE-GENERATION-STALE`,
`AIDE-DISPATCH-GENERATION-CHANGED`, `AIDE-DISPATCH-DELIVERY-UNKNOWN`, `AIDE-MCP-LIMIT-EXCEEDED`,
`AIDE-MCP-EGRESS-DENIED`, `AIDE-EXTRACT-TIMEOUT`, `AIDE-EXTRACT-QUARANTINED`, `AIDE-HEALTH-DEGRADED`.

---

## Error and concurrency model

- **One writer.** A single prioritized writer loop owns the write connection. Control work (receipts,
  dispatch, lifecycle) preempts **between** ingestion transactions; snapshot commits are **chunked to a
  max transaction duration** so a 10k-assertion snapshot cannot priority-invert a pending dispatch.
- **Reads never queue behind writes** — `query_only` snapshot reads with a deadline; cancellation
  returns an explicit partial/limit state rather than a truncated success.
- **Idempotency.** Every mutating command carries `command_id`; the receipt is read first. `dispatch_key`
  is **derived deterministically from `command_id`** — one namespace, not two.
- **Epoch fencing.** `core_epoch` is a store-persisted monotonic integer incremented inside the
  ownership-lock transaction at startup — never random, never clock-derived (so "stale" is decidable and
  ABA-free).
- **Dispatch sequence (normative, ADR-0010):** revalidate binding → **commit `Pending`** → write under the
  session-owner lock with an atomic generation compare → append outcome. **Recovery sweeps any `Pending`
  with no outcome to `DeliveryUnknown`.** A retry that reads *any* receipt, `Pending` included, returns it
  and never re-executes.
- **Cancellation** is cooperative throughout; the extractor contract requires honoring the token (the
  in-process enforcement mechanism, with process isolation as the Phase-2 escalation).

---

## Failure-mode analysis

| Failure mode | From which choice | Disposition | How addressed (or why accepted) | Detection | Test |
|---|---|---|---|---|---|
| Crash after PTY write, before outcome | non-transactional PTY side effect | **prevent + recover** | Write-ahead `Pending`; recovery sweep → `DeliveryUnknown`; retry returns existing receipt | `AIDE-DISPATCH-DELIVERY-UNKNOWN`, `aide.terminal.session` span | `P1-DISPATCH-02` |
| Crash after `Pending`, before PTY write | two-phase receipt | recover | Same sweep → `DeliveryUnknown` (honest: we cannot know); no auto-resend | same | `P1-DISPATCH-03` |
| Session generation changes between revalidate and write | check-then-act window | **prevent** | Generation compared **atomically with the write** on the session owner loop; `Rejected`, zero bytes | `AIDE-DISPATCH-GENERATION-CHANGED` | `P1-DISPATCH-04` |
| `INSERT OR REPLACE` silently overwrites a fact | SQLite trigger semantics (spike S4) | **prevent** | `recursive_triggers=ON` + writer forbids REPLACE/UPSERT on fact tables | `AIDE-STORE-IMMUTABLE-VIOLATION` | `P1-STORE-02` |
| Late/stale extractor commits over newer evidence | async extraction | **prevent** | Commit only when generation **and** revision equal the durable desired pair, checked in-transaction | `AIDE-SCOPE-GENERATION-STALE` | `P1-STORE-06` |
| Duplicate assertion on re-extract | deterministic IDs + replay | prevent | Unique natural-key index; idempotent ingest | constraint error 19 | `P1-STORE-05` |
| Silent watcher loss (graph rots, staleness reads fresh) | event-driven ingestion | **detect** | Freshness prober compares repo-observed vs indexed revision | `freshness.drift` metric + incident | `P1-FRESH-01` |
| Extractor hangs; two wedged workers stall all ingestion | in-process extractor, fixed pool | detect + mitigate | Cooperative timeout, per-scope quarantine after K failures, `workers.busy_duration` incident | `AIDE-EXTRACT-TIMEOUT` / `-QUARANTINED` | `P1-EXT-04` |
| Store unwritable (disk full/read-only) → its own incident unwritable | incidents-in-DB | **prevent** | Health incidents live in a **sidecar file**, independent of the DB | sidecar append, `AIDE-HEALTH-DEGRADED` | `P1-HEALTH-02` |
| Unbounded result floods a pane or an agent context | graph traversal | **prevent** | Node/edge/**byte** caps on every projection and tool, with omission counts | `AIDE-MCP-LIMIT-EXCEEDED` | `P1-MCP-03` |
| Hostile artifact label reaches an agent as an instruction | outbound data flow | mitigate | Repo strings returned only in typed data fields, never blended free-text | inertness assertion | `P1-MCP-INERT-01` |
| Path escape / junction swap between check and use | filesystem trust boundary | prevent | Trusted-side canonicalization + **handle identity** revalidated at each privileged use | `AIDE-PATH-CONTAINMENT` | `P1-FS-01..03` |
| Cache diverges from facts | materialized `claim_current_cache` | detect | **Rebuild-equality test** compares cache to derivation from empty | equality assertion | `P1-STORE-08` |
| Concurrent writers corrupt state | SQLite single-writer | prevent | One writer loop; second writer attempt rejected | busy/locked | `P1-STORE-07` |
| Long snapshot read starves WAL checkpoint | snapshot isolation | **accept** (bounded) | Read deadlines bound reader lifetime; checkpoint-lag metric exposed. **Residual risk:** sustained large reads could still grow WAL — bounded by the read deadline and surfaced, not eliminated; Phase-1 corpus makes this improbable. | `wal.checkpoint_lag` | metric asserted in `P1-PERF-04` |
| Clock skew misorders facts | wall-clock ordering | prevent | Ordering is **ingress sequence**, never wall-clock; timestamps are display metadata | — | `P1-STORE-04` |
| Backdated assertion rejected by interval trigger | two competing clocks | prevent | Version intervals in **ingress-sequence** terms; event time only *selects* the containing version | — | `P1-STORE-03` |

## Adversarial analysis (STRIDE-lite)

Phase-1 trust boundaries: **(B1)** filesystem/worktree → extractor; **(B2)** MCP caller → tool gateway;
**(B3)** repository content → projection → UI/agent; **(B4)** prompt draft → session; **(B5)** core →
SQLite/sidecar files. *(The shell→core pipe boundary does not exist in Phase 1 — the core is in-process,
ADR-0009 — and is re-established with its full control set at Phase 2.)*

| Boundary | STRIDE threat | Disposition | Control / rationale | Negative test |
|---|---|---|---|---|
| B1 | **T**: junction/symlink swap redirects extraction outside the workspace | mitigate | Open trusted directory handle; retain volume+file identity; revalidate by handle at every privileged use | `P1-FS-02` reparse swap → `AIDE-PATH-CONTAINMENT`, no extraction |
| B1 | **T**: TOCTOU replacement between check and read | mitigate | Handle-identity comparison, not path re-resolution | `P1-FS-03` |
| B1 | **D**: oversized/malformed artifact exhausts memory | mitigate | Input size caps + parser resource limits; scope marked failed, not fatal | `P1-EXT-05` oversized fixture |
| B2 | **S**: caller claims another workspace/session | mitigate | `CallerPrincipal` **server-derived**, never read from payload; workspace scoping checked per call | `P1-SEC-05` cross-workspace → `AIDE-AUTH-CROSS-WORKSPACE` |
| B2 | **I**: externally-processing agent exfiltrates workspace facts to its provider | **mitigate** | **Processing-class-bound authorization (ADR-0011)** — non-`LocalOnly` denied or minimum metadata; loopback is *not* the control | `P1-MCP-EGRESS-01..03` |
| B2 | **E**: agent write escalates into artifact truth | prevent | Write tools create only attributed `Note`/`Decision`/`Term`/claim records; artifact facts are extractor-owned | `P1-MCP-05` unapproved write rejected |
| B2 | **D**: oversized request/result floods context | mitigate | Request ≤64 KiB; node/edge/byte caps on every result | `P1-MCP-03` |
| B3 | **T**: hostile symbol label carries an instruction to an agent | mitigate | Typed data fields only; no free-text blending; AI-DE cannot bind agent behavior (stated, not assumed) | `P1-MCP-INERT-01` |
| B3 | **T**: hostile label injects active markup into the pane | mitigate | All artifact strings rendered as inert text; no active content in the Phase-1 pane | `P1-UI-05` |
| B4 | **S/R**: prompt retargeted to a session the user did not confirm | prevent | Immutable binding + atomic generation compare at the write | `P1-DISPATCH-04` |
| B4 | **R**: delivery outcome unattributable | mitigate | Append-only attempt+outcome series with ingress sequence; `DeliveryUnknown` is an explicit recorded state | `P1-DISPATCH-02` |
| B5 | **T**: fact tampering via direct SQL | mitigate | Immutability triggers + `recursive_triggers=ON` + REPLACE forbidden; **daemon-only data directory ACL** | `P1-STORE-01..02` |
| B5 | **I**: secrets/PII leak into logs, metrics, or the sidecar | mitigate | Field allowlist; prohibited-attribute list; seeded-secret negative fixtures | `P1-PRIV-01..03` |
| B5 | **E**: same-user process reads/writes the store directly | **accept** | A same-user compromised process is an explicit desktop residual (threat model boundary 1). **Residual risk:** full workspace compromise; out of scope for a single-user local tool. No control claimed that cannot hold. | — (documented, not tested) |
| B2 | supply chain: unpinned/vulnerable dependency ships | transfer → **named** | CI gate: locked restore, SBOM, licence scan, transitive-CVE scan, SHA-pinned actions (release plan §CI gates) | `P1-SUPPLY-01..05` |

## Privacy analysis (LINDDUN-lite)

Phase 1 handles **work data** (repository paths, symbol names, prompt text) and, by inference,
**personal data** (author identity in knowledge frontmatter, session/actor attribution).

| Data flow / category | LINDDUN finding | Disposition | Control / rationale | Retention & rights path |
|---|---|---|---|---|
| Repo artifacts → assertions → projections | **I/D**: source paths and symbol names are work data; raw source bodies would be over-collection | mitigate | Store normalized relation metadata + source *references* only; never raw bodies (allowlist enforced) | Rebuildable; workspace deletion purges with a receipt |
| Knowledge frontmatter → knowledge nodes | **I**: `owner` fields name real people | mitigate | Owner rendered in-workspace only; never in telemetry/metrics; classified `Internal` | Purged with the workspace; owner-deletion path |
| MCP result → agent → **provider** | **D/N**: indirect model egress with no lawful basis | **mitigate** | **ADR-0011** processing-class binding; non-`LocalOnly` denied | Denied at source; nothing to retain |
| Prompt draft/revisions + receipts | **N**: unbounded retention conflicts with the stated ceiling | mitigate | One rule: **90/365-day ceiling** (privacy review supersedes the spec's "until deletion") | Daily expiry + owner deletion; rebuild-and-swap compaction, never trigger-bypassing deletes |
| Telemetry / logs / sidecar | **D**: PII or secrets leak into observability | mitigate | Allowlisted fields only; paths/prompts/source/terminal text prohibited; **pseudonymous IDs rotate per core epoch** | Local-only, finite retention; no remote exporter (dependency gate + egress probe) |
| Fixture session record of dispatched bytes | **D**: prompt text persisted by a test double | mitigate | Fixture session holds bytes **in memory only**, never written to disk or logs | Process lifetime |

## UI and interaction design

**Medium & guidelines:** Windows desktop (WPF), **Microsoft Fluent 2** + Windows keyboard/focus
conventions. Archetype per spec Part C: **B1 Keyboard-Velocity GUI**, master-detail, compact density,
dark-adaptive. Phase 1 renders the **accessible evidence list + provenance pane + health strip** — the
list/tree is not a fallback, it is the permanent keyboard/screen-reader equivalent of the Phase-2 canvas.

**Tokens:** referenced from [`DESIGN.md`](../../DESIGN.md) — `{colors.surface}`, `{colors.text}`,
`{colors.text-muted}`, `{colors.accent}`, `{colors.verified}`, `{colors.inferred}`, `{colors.unverified}`,
`{colors.stale}`, `{colors.danger}`, `{typography.ui}`, `{typography.mono}`, `{spacing.scale}`,
`{rounded.sm}`, `{motion.fast}`. No arbitrary values in components.

**Key screens / flows:** one master-detail view. Focal point = the **selected node's provenance**.
Left: filterable evidence list (node label, kind, confidence chip). Right: provenance pane in the spec's
fixed evidence order — *what it is → confidence/provenance → related nodes → source location → actions*.
Bottom: health strip (stale scopes, failed extractor, rendered revision, core mode).

**Component states (the polish gate):**

| Component | default | hover/focus | active | disabled | loading | empty | error | success | first-run / overflow |
|---|---|---|---|---|---|---|---|---|---|
| Evidence list | rows w/ confidence chip | focus ring, roving tabindex | selected row `aria-selected` | n/a | skeleton rows | "No evidence yet — this workspace has no committed snapshot." + *Extract fixture* action | "Extraction failed for `orders` — showing the last successful revision." + Retry | — | first-run guides to Add repository; long labels ellipsize with full text in the pane, never truncated silently |
| Provenance pane | ordered evidence sections | focusable source link | — | — | shimmer on the sections | "Select an item to see its provenance." | "Provenance unavailable — the source artifact could not be read." | — | >50 neighbors → "Showing 50 of 218 — 168 omitted" + Continue |
| Confidence chip | label + shape (✓ Verified / ~ Inferred / ? Unverified) | tooltip = the rule | — | — | — | `not recorded` chip when absent | — | — | **never colour alone** — glyph + text always |
| Health strip | "All scopes current · rev a0bd699" | focus | — | — | "Checking…" | — | "1 scope stale · Bicep extractor failed" (amber) | — | overflow collapses to a count with a details popup |
| Dispatch confirm | target session + generation + final revision | focus-trapped dialog | Send | Send disabled until a ready target | "Sending…" | — | "Session busy — prompt remains staged." | "Delivered to the terminal (bytes accepted). This is not agent acceptance." | `DeliveryUnknown` → "Delivery outcome unknown. Confirm a new send if you want to retry." |

**Motion:** selection change 150ms ease-out; pane section reveal 200ms. Delivery status changes announce
**immediately** via a live region regardless of motion. `prefers-reduced-motion` → instant state change
with the identical announcement.

**UI copy (real, in-voice):** `Relationship inferred from naming convention; inspect source` ·
`Graph is stale — fixture extraction failed. Viewing last successful snapshot` ·
`Session is busy — prompt remains staged` · `Delivery outcome unknown — confirm a new send to retry` ·
`not recorded`. Copy states evidence, never reassurance.

**Accessibility (WCAG 2.2 AA) & performance:** full keyboard path to every list row, pane section, source
link, and dispatch control; roving tabindex in the list; focus restored to the triggering row after the
dialog closes; every state announced via `AutomationProperties` + a live region; **no colour-only
indication** (confidence, stale, and error all carry glyph + text); target sizes ≥24×24. Contrast measured
at token level in light/dark/high-contrast in `DESIGN.md`. Budget: node selection → pane render p95
<100ms; list filter p95 <250ms (spec Part C).

**AI-UX:** Phase 1 makes **no model call**, so no generated content is rendered. The applicable patterns
are **Governors** (dispatch preview/confirmation before a consequential send) and **Trust builders**
(provenance, confidence labels, `PtyWriteAccepted` ≠ agent acceptance disclosure). HAX **G1/G2** (state
capability and limits) apply to the dispatch dialog's honesty about what delivery proves.

---

## Telemetry

**Spans:** `aide.workspace.command`, `aide.ingestion.scope`, `aide.store.transaction`,
`aide.projection.query`, `aide.terminal.session`, `aide.mcp.request`, `aide.freshness.probe` — all
`Activity`-based, `traceparent` propagated, structured logs inside each span carrying trace/span IDs.

**Required attributes:** pseudonymous `workspace.id` (**rotating per core epoch**), `core.epoch`,
`command.id`, `scope.id`, `artifact.revision`, `schema.version`, `outcome`, `error.code`, duration, and
requested/returned/omitted node/edge/**byte** counts. **Prohibited:** paths, prompts, source text,
terminal text, credentials, personal identifiers.

**Metrics (event-pair histograms, not point gauges):** command queue age *at dequeue*; **scope settlement**
= first triggering event → committed snapshot for the *final* generation (superseded chains attributed to
the surviving generation's start, so coalescing cannot censor slow scopes); extraction duration/failure;
worker busy-duration; store transaction latency; projection duration; WAL bytes + **checkpoint lag**;
MCP outcome by tool; **freshness drift**; `telemetry.not_recorded`.

Telemetry failure is non-blocking and degrades to `not_recorded` — never a fabricated value.

---

## Test plan

**Triggered directives** (Testing Strategy trigger table — the *union*, D0 always):

| Trigger | Present because | Directive |
|---|---|---|
| T1 | fold/projection/claim-assessment pure logic | **D1** |
| T2 | fixture parsing, envelope validation, path canonicalization, cursor decoding | **D2** |
| T3 | new projects `AiDe.Core` / `AiDe.Mcp`, dependency direction | **D3** |
| T4 | SQLite persistence + sidecar filesystem | **D4** |
| T6 | MCP tools are an API other consumers call | **D5-provider** |
| T7 | command envelope + extractor snapshot payload schemas | **D6** |
| T8 | fixture extractor, fixture terminal session (substitutes at boundaries) | **D7** |
| T10 | MCP server + tool schemas/descriptions | **A2** |
| T14 | tool descriptions authored/edited | **A6** |
| — | **not triggered, explicitly:** T5 (consumes no external API), T9/T11/T12/T13 (no model call in v1) | — |

**Concrete cases** — the Phase-1 proof plan rows are the test list: `P1-SEC-01..05`, `P1-FS-01..03`,
`P1-STORE-01..11` (incl. **`INSERT OR REPLACE` under `recursive_triggers` on/off**, forbidden-mutation
attempt, rebuild-equality, backdated interval, concurrent writer, migration down, restore),
`P1-DISPATCH-01..04` (**crash-injection both sides of the PTY write**, retry-returns-receipt, generation
change), `P1-KNOW-01..03` (US-4 search/filter/neighborhood + missing-source health finding),
`P1-QUEUE-01..03`, `P1-MCP-01..05`, **`P1-MCP-EGRESS-01..03`**, **`P1-MCP-INERT-01..02`**, `P1-EXT-01..05`,
`P1-UI-01..05`, `P1-STORE-DEL-01`, `P1-PERF-01..05`, `P1-PRIV-01..03`, `P1-SUPPLY-01..05`.

**Oracle discipline (the review's condition):** fixture expected-graph manifests are **hand-derived from
the fixture source and reviewed before the extractor first runs against them** — a manifest snapshotted
from extractor output is an implementation mirror that cannot fail. Equality oracles use a **canonical
normalized form** (sorted node/edge tuples, declared field set), and each equality test states which claim
it proves. `P1-UI` diffs a captured focus-order/name-role-value sequence against an **expected** sequence
(a trace that cannot fail is not an oracle).

**D7 (substitutes):** `FixtureExtractor` and `FixtureTerminalSession` are the Phase-1 fakes. Neither has a
second implementation yet, so a shared conformance suite is not yet owed; `P1-EXT-01..03` pins the
extractor *interface* contract now so the Phase-2 Roslyn implementation is a substitution. The
`ITerminalSession` conformance suite becomes a **Phase-2 entry criterion** when ConPTY arrives.

**UI craft gate:** `ui-craft-gate.py` runs against the built surface as an automated control with the CD12
severity floors (accessibility Major-minimum; token discipline Major-minimum); `design-lint.py` keeps
`DESIGN.md` clean. A craft rule that lives only in prose is a memoir, not a control.

---

## Conformance notes

C4 typed boundaries, C5 side-effect protection (user-confirmed **write-ahead** dispatch), C6 idempotency
(`command_id` ≡ dispatch key), C7 fallback (stale last-successful projection), C11 principal propagation
(stable `CallerPrincipal` on every command) are all exercised in Phase 1. C1/C2/C3-model are **not
applicable**: Phase 1 initiates no model call — an explicit negative, not an omission.

## Flagged risks and residual unknowns

- SQLite recursive-CTE latency and index design at 50k edges — **unmeasured** until `P1-PERF`; the
  in-memory neighbor index carries a `simplify:` ceiling and upgrade trigger.
- WPF↔WebView2 airspace is **not exercised** in Phase 1 (no canvas); ADR-0008's reversal trigger sits in
  Phase 2.
- The fixture extractor's fidelity to a real Roslyn extractor is untested by construction — Phase-1 green
  proves the *pipeline*, not C# extraction. Stated so no one reads Phase 1 as more than it is.
- Session **processing-class attestation** is declared-not-proven in Phase 1: only a locally-launched
  fixture session is `LocalOnly`; everything else fails closed to `UnknownProcessing`.

## Status and next action

| | |
|---|---|
| **Completed** | Phase-1 detailed design: data model with grains/additivity/per-attribute history, contracts, patterns, error/concurrency model, failure-mode + STRIDE + LINDDUN analyses, UI design + `DESIGN.md`, telemetry, and the full triggered-directive test plan. |
| **Remaining** | Implementation of this slice; then Phase 2 (`/design` of the Roslyn extractor, ConPTY runtime, process split, upgrade/rollback). |
| **Best next action** | `/implement` this design red-first, starting with the store's immutability control and the write-ahead dispatch — the two mechanisms the council vetoes turned on. |

## Gate record

`GATE design · 2026-08-26 · Patterns Expert ⇄ Simplifier (mutual: no new dependency past rung 5; two simplify: markers with ceilings); Test Architect (D0+D1,D2,D3,D4,D5-provider,D6,D7,A2,A6 enumerated; hand-derived manifests; expected focus-order oracle); Security & Identity (5 Phase-1 boundaries walked; same-user residual explicitly accepted, not falsely controlled); Distributed Systems (write-ahead receipt, atomic generation compare, ingress-sequence ordering); Privacy (MCP egress mitigated at source; retention single-ruled); SRE (spans/metrics as event-pair histograms; sidecar independent of the store); UX & Accessibility (complete state set incl. empty/loading/error; no colour-only; keyboard path) · verdict: PASS-WITH-CONDITIONS · conditions: every listed negative/error-path test observed RED before its control exists; P1-PERF measures before any scale claim is promoted from Inferred · vetoes→resolution: no open hard veto; author did not self-clear.`

---
**Handoff:** → `/implement`.
