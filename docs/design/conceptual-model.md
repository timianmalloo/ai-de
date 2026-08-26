---
id: conceptual-model-ai-native-ide
title: "AI-DE conceptual domain model"
type: design
status: in-review
owner: "@timianmalloo"
phase: "0"
tags: [ddd, domain-model, facts, workspace, agent-coordination]
links:
  - { to: architecture, rel: refines }
  - { to: spec-ai-native-ide, rel: implements }
  - { to: adr-0002-workspace-fact-store, rel: relates-to }
review-by: 2027-02-21
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture" }
summary: >-
  Defines the bounded contexts, aggregate invariants, fact grains, history rules, and identity-only
  relationships used by the AI-DE workspace fact store.
---

# AI-DE conceptual domain model

## Bounded contexts

| Context | Ubiquitous language | Authority |
|---|---|---|
| Workspace Registry | Workspace, repository membership, worktree membership, workspace epoch | Canonical local scope and daemon ownership. |
| Evidence and Projection | Evidence assertion, scope snapshot, relationship claim, projection | Attributable code/infra/runtime evidence and derived views. |
| Agent Operations | Agent session, session generation, prompt revision, dispatch command, delivery receipt | Terminal lifecycle and user-confirmed prompt transfer. |
| Work Coordination | Work item, coordination claim, assessment | Human work intent plus advisory evidence fold. |
| Audit Reading | Audit reference, classification, integrity state | Privacy-filtered view of source-owned audit records. |

## Aggregates and invariants

```mermaid
classDiagram
  class WorkspaceRegistry {
    +WorkspaceId
    +WorkspaceEpoch
    +RepositoryMembership
    +WorktreeMembership
    invariant one canonical membership per opened identity
  }
  class ScopeSnapshot {
    +ScopeId
    +DesiredGeneration
    +CommittedGeneration
    invariant commit only current desired generation
  }
  class RelationshipClaim {
    +ClaimId
    invariant one or more attributable assertions
  }
  class PromptDraft {
    +DraftId
    invariant immutable revision and command binding
  }
  class AgentSession {
    +SessionId
    +Generation
    invariant one active worktree reference
  }
  class WorkItem {
    +WorkItemId
    invariant intent differs from assessment
  }
  WorkspaceRegistry --> ScopeSnapshot : contains by identity
  ScopeSnapshot --> RelationshipClaim : selects assertions
  PromptDraft --> AgentSession : targets by identity
  WorkItem --> AgentSession : associates by identity
```

| Aggregate root | One invariant it protects | Cross-aggregate rule |
|---|---|---|
| Workspace Registry | A canonical filesystem identity belongs to at most one active membership in a workspace and every command carries the current workspace epoch. | Other aggregates refer only to `WorkspaceId`, membership ID, and epoch. |
| Scope Snapshot | Only the currently desired scope generation **and authoritative artifact revision** can become the committed generation. | Extractors submit immutable assertions by scope/revision; the writer rejects stale generation or revision atomically. |
| Relationship Claim | A displayed claim has one or more attributable assertions with compatible normalized subject/predicate/object. | Claims reference assertions by identity and derive assessment from their selected scope snapshots. |
| Agent Session | An active session has at most one current worktree reference and one generation. | Prompt Dispatch references `{SessionId, Generation}`; a changed generation invalidates a pending command. |
| Prompt Draft | A dispatch command binds exactly one immutable revision, workspace epoch, target session generation, and idempotency key. | Delivery is one at-most-once terminal-stream attempt; a resend after unknown delivery requires human confirmation. |
| Work Item | Declared intent is never overwritten by an evidence-derived assessment. | Claims/receipts are folded into a separate assessment by identity. |

## Logical fact grains and history rules

| Shape | Grain: one row/fact is exactly one… | Key/order | History and additivity |
|---|---|---|---|
| `workspace_dim`, `repository_dim`, `worktree_dim`, `artifact_dim`, `node_dim`, `session_dim`, `agent_dim`, `tool_dim`, `view_dim` | current record for one stable business identity; a version only when history changes a fact’s meaning | natural ID plus mandatory `<Entity>Key` surrogate version key; `valid_from`, `valid_to`; deterministic current flag | Default Type-1 for display-only/current identity attributes. Add Type-2 only for path/root, session generation, or agent/tool policy when existing facts must retain the former meaning. Every fact records the applicable `<Entity>Key`. No stored aggregate measures. |
| `scope_snapshot_fact` | extraction request/commit state for one scope, desired generation, and authoritative artifact revision | `{workspace, scope, generation, artifact revision}`; commit order is daemon ingress sequence | Append-only. The current snapshot is the desired/committed generation and revision pair recorded by the daemon. |
| `evidence_assertion_fact` | extractor/observer statement about one normalized relation at one source revision or observation time | deterministic assertion ID plus `{scope snapshot, source revision}` | Append-only; uniqueness prevents duplicate replay. |
| `claim_assessment_fact` | assessment of one relationship claim from one selected assertion set at one ingress sequence | `{claim, selected scope generation set, ingress sequence}` | Append-only, rebuildable. |
| `command_receipt_fact` | accepted/rejected/completed outcome for one mutating command | `{workspace, callerPrincipal, command type, commandId}` | Append-only; uniqueness makes a retry return the original outcome. `callerPrincipal` is the **stable** workspace-owner/enrolled-client identity, invariant across connections and core epochs (never a connection-scoped value), so dedup holds across a crash/reconnect. |
| `dispatch_attempt_fact` / `dispatch_outcome_fact` | **the prompt delivery receipt, split into two event grains:** one attempt (the `Pending` write-ahead committed *before* the PTY write) and one or more outcomes appended after | dispatch key (≡ `commandId`) plus daemon ingress sequence | Append-only. A deterministic fold per dispatch key yields the displayed outcome; core recovery sweeps an attempt with no outcome to `DeliveryUnknown`; a late authenticated `AgentAccepted` appends without rewriting a prior row (ADR-0010). |
| `coordination_claim_fact`, `work_assessment_fact`, `prompt_revision_fact`, `audit_reference_fact`, `trace_observation_fact` | one named advisory claim, assessment, revision, classified audit record, or observation at one recorded instant | entity identity plus daemon ingress sequence | Append-only. Timestamps are display metadata; **the coordination fold orders per-session writer sequence first and daemon ingress sequence only to totalize across sessions**, so a release cannot fold before its claim, and a `Done` assessment requires corroborating non-claim evidence, not a single agent claim. |

## Store enforcement and rebuild contract

1. SQLite enables foreign keys on every connection, sets **`PRAGMA recursive_triggers=ON`** on every
   writer connection, and sets **`PRAGMA query_only=1`** on every read connection.
2. Fact tables reject `UPDATE` and `DELETE` through `BEFORE UPDATE`/`BEFORE DELETE` `RAISE(ABORT)`
   triggers, and the writer **forbids `INSERT OR REPLACE`/UPSERT conflict resolution** on fact tables.
   This is required, not optional: with the default `recursive_triggers=0`, `INSERT OR REPLACE`
   silently deletes-and-replaces a fact row *without firing the delete trigger* — verified by
   `spikes/sqlite-fact-store` case S4, and closed by S5 (`recursive_triggers=ON`) and S6 (`query_only`).
   SQLite has no embedded permission system; the **single-writer core process is the real boundary** and
   the triggers/pragmas are defense-in-depth. Correction is a superseding fact.
3. One writer transaction validates daemon/workspace epoch, scope generation **and artifact
   revision** or command idempotency key, inserts facts/receipts, and updates only labelled
   rebuildable projections.
4. Fact ordering is total: daemon ingress sequence, then deterministic fact ID. Current projections
   replay facts in that order.
5. A fixture replay test rebuilds every current projection from an empty cache and compares it to
   the stored projection. Forbidden-mutation, stale-scope, duplicate-ingest, interval, foreign-key,
   concurrent-writer, backup/restore, and purge tests are required before Phase 1 acceptance.

## Lifecycle

### Migration, query, and retention policy

- Migrations use expand → migrate → move reads → contract. Each version carries forward and down
  scripts; the daemon applies the migration before opening writable workspace commands and
  exercises the down path on a copied fixture database. A backfill that cannot assign a
  deterministic value quarantines the record rather than guessing.
- Dimension tables use a unique natural-ID/current partial index plus non-overlap trigger. **Dimension
  version intervals are defined in ingress-sequence terms, not wall-clock.** The ingester's as-of
  lookup uses a fact's event time *only* to select the version whose ingress interval contains it — one
  deterministic function — so a backdated trace (imported days after observation) binds to the correct
  historical version instead of being rejected by a clock mismatch (the two-clocks defect the review
  found). A `BEFORE INSERT` trigger rejects a fact whose selected version cannot be resolved; the
  equivalent update trigger exists only for mutable projection/cache tables. Fact tables use
  foreign-key, deterministic assertion/command uniqueness, and immutable-row triggers.
- History rules are declared **per attribute** (Type-0/1/2), not per table: the Phase-1 `/design` schema
  enumerates every dimension attribute with its rule and a one-line justification for each Type-1, so no
  attribute silently discards history (DM10).
- Bounded impact traversal indexes `{workspace, selected scope, subject}` and
  `{workspace, selected scope, object}`; latest-per-key projections index
  `{natural_id, ingress_sequence DESC}`. Phase 1’s fixture corpus contains 10,000 assertions and
  50,000 edges, records `EXPLAIN QUERY PLAN`, and fails if bounded `describe`/`impact` selects
  perform a full fact-table scan or exceed the architecture p95 limits.
- Default retention is workspace lifetime for rebuildable evidence, 30 days for named traces,
  ephemeral for terminal output, and policy-configured for audit metadata/coordination. Deletion
  receipts live in a parent local control ledger retained 90 days, rather than in the deleted
  workspace database.

Workspace deletion is an idempotent command: mark workspace deletion requested, prevent new
commands, remove projections/caches/exports, delete retained local facts according to policy, and
write a deletion receipt. Backups and source repository history are reported separately when they
cannot be purged by the workspace daemon.

Retention expiry and erasure do **not** bypass immutable-fact triggers with ad-hoc row deletes.
They run as an administrative compaction: quiesce the workspace, rebuild a new database from only
retained facts, validate replay/projections, write the deletion receipt to the parent control
ledger, atomically swap database files, then securely remove the former database, WAL, snapshots,
and exports. Restore filters the same expiry/tombstone policy, so a backup cannot resurrect
purged data.
