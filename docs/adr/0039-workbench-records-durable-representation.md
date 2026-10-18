---
id: adr-0039-workbench-records-durable-representation
title: "ADR-0039 — Workbench records: the spec's aggregates as interval-versioned dimensions, widened existing facts and derived folds in the workspace store"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [architecture, data-model, dimensions, facts, journey, notebook, attempt, assessment, workbench-loops]
links:
  - { to: architecture, rel: implements }
  - { to: spec-workbench-loops-ux, rel: implements }
  - { to: adr-0002-workspace-fact-store, rel: refines }
  - { to: adr-0010-two-phase-dispatch-receipt, rel: refines }
  - { to: adr-0034-envelope-event-store, rel: depends-on }
  - { to: conceptual-model-ai-native-ide, rel: refines }
  - { to: adr-0040-journey-purpose-identity, rel: relates-to }
  - { to: adr-0041-execution-attempt-lifecycle, rel: relates-to }
  - { to: adr-0042-native-harness-profile-binding, rel: relates-to }
review-by: 2027-04-18
summary: >-
  The spec's nine aggregates persist in the existing per-workspace SQLite store by widening what is
  already there — the two-grain dispatch receipt becomes the execution attempt, prompt_revision_fact
  becomes the notebook revision — plus four new dimensions/facts (journey, notebook, permission
  decision, evidence link, assessment, disposition, obligation, harness profile, profile
  observation). Nothing derived is stored; terminal attempt outcomes are host-asserted rows, every
  other state is a fold over a per-attempt run log that WL-1 builds. Dimensions are
  interval-versioned like session_dim, so no row is ever updated in place. The first workspace.db
  migration chain, WAL-safe snapshot and a constraint table are part of the decision.
review-suggested:
  - { by: architecture, on: 2026-10-18, reason: "2026-10-18 workbench-loops architecture: §WL added, ADR-0039..0045 proposed, Perspective set re-rowed (ADR-0045 supersedes ADR-0017/0030 rows)" }
---

# ADR-0039: Workbench records — widened facts, interval-versioned dimensions, derived folds

- **Status:** Accepted — owner ruling 2026-10-18 (@timianmalloo); council record follows. Previously Proposed; council 2026-10-18 — Data & Persistence PASS-WITH-CONDITIONS (conditions
  1–7, 10–12 folded in below), Simplifier PASS-WITH-CONDITIONS (merges 1–8 adopted), Distributed
  Systems conditions on keys/uniqueness adopted. See `docs/architecture.md` §WL.14.
- **Date:** 2026-10-18 (revised the same day after the gate)
- **Deciders (Peer Mode):** Data & Persistence Architect (author), Enterprise Architect, Tech Lead
- **Context spec/architecture:** `docs/specs/workbench-loops-ux.md` §A4, US-J01/W03/S03/B01/E03/R01/
  P02/T01/G01/L01; `docs/architecture.md` §Durable representation and §WL.6; the pack's
  `domain-and-data-modelling.md` DM1–DM18; `docs/design/conceptual-model.md`.

## Context

The spec names nine aggregate roots with one invariant each (§A4) and requires the next step to
bind them to existing store and wire contracts. What exists, all **Verified** 2026-10-18:

- `WorkspaceSchema` v1 (`src/AiDe.Core/Store/WorkspaceSchema.cs`): fact tables with
  `BEFORE UPDATE/DELETE … RAISE(ABORT)` triggers; `core_state.ingress_seq` as the one total order;
  `prompt_revision_fact (draft_id, revision_no, body, saved_at, ingress_seq)` (:170-177);
  `dispatch_attempt_fact` keyed by a deterministic `dispatch_key` and `dispatch_outcome_fact` as
  one-or-more outcome events per key (:146-168) — ADR-0010's two-grain write-ahead receipt with a
  sweep (`DispatchService.SweepPendingToUnknown` :139); dimensions interval-versioned with
  `valid_from_seq / valid_to_seq` and a current-flag index (`StoreWriter.cs:169-179`) — **no
  dimension is updated in place**.
- `EnsureSchema` creates v1 and returns as soon as `schema_version` exists; it never reads the
  stored version (`WorkspaceStore.cs:78-96`); `UpgradeCoordinator.Run` has no production caller
  and `StoreSnapshot.Capture` copies only the main file under WAL (`UpgradeCoordinator.cs:24-30`).
  The watcher store has the additive `Migrations` pattern (`SqliteWatcherObservationStore.cs:1064-1085`).
- `runs/<run-id>.jsonl` is **reserved, not built** (`SessionPaths.cs:28-32`); the only run-event
  sink is the in-memory `RunEventRelay`. The compile envelope stream is append-only JSONL
  (ADR-0034) with `Submitted.ProjectionSha` (`EnvelopeEvents.cs:218-223`).

The invariants that force the shape: immutability of past meaning (AC-E03a, AC-R01a, AC-S03c,
AC-P02b); facts a flag would collapse (acknowledgment ≠ satisfaction; saved review state ≠
acceptance, AC-P02c; transport-complete ≠ evidence-present ≠ accepted); derived, never stored
(DM "two definitions of one quantity"); one store, one order (DM6).

LOA: P2 determinism at the floor, P8 idempotency at the side-effect boundary, P10 history is the data.

## Decision

### 1. Reuse-in-codebase first: widen, do not duplicate

| Spec aggregate | Durable home | What changes |
|---|---|---|
| Execution attempt | **`dispatch_attempt_fact` + `dispatch_outcome_fact`** (ADR-0010's pair, widened) | nullable columns on the attempt row: `projection_sha`, `route_fingerprint` (ADR-0042 defines it), `profile_key` (FK to the profile version), `scope_snapshot` (JSON value object), `parent_dispatch_key` (retry), `queued_after_dispatch_key` (queue); `run_id := dispatch_key` (one identity). PTY dispatches keep `route_fingerprint = pty`. The outcome vocabulary is extended (§3). One sweep. |
| Notebook revision | **`prompt_revision_fact`** (widened) | `ADD COLUMN parent_revision_no INTEGER NULL`, `body_sha TEXT NULL`; `draft_id ≡ notebook_id` by documented alias. Legacy rows keep `body_sha NULL` = Not recorded, computed on read; no attempt ever bound a legacy row (asserted by a migration query). **No `kind` column**: `submitted` ≡ ∃ attempt row for `(notebook_id, revision_no)`; `branch` ≡ `parent_revision_no ≠ revision_no − 1`. |
| Notebook | `notebook_dim` (new, interval-versioned) | `notebook_id`, `owner_journey_id`, `legacy INTEGER NOT NULL DEFAULT 0`, `CHECK (legacy = 1 OR owner_journey_id IS NOT NULL)`. Pre-workbench drafts migrate as `legacy = 1`, surface as "Unassigned (pre-workbench)", and are adopted by an explicit user action that writes a new version row — never by the backfill. |
| Journey | `journey_dim` (new, interval-versioned) | `journey_id`, `purpose` (`work` \| `investigation`), `origin_scope` (JSON), `origin_journey_id`, `origin_kind` (`promoted-from` \| `investigation-of` \| `review-assignment-of`), `origin_object_ids` (JSON), `title`. Every bridge in the spec creates its target at confirm, so the bridge is Type-0 columns on the created journey, not a link table; cancel writes no row. A title change is a new version row (Type-2 by construction). |
| Permission decision | `permission_decision_fact` (new) | `session_id, run_id, seq` (the exact request; **unique**), `action`, `scope`, `arguments_sha` (sha256 of canonical `{name, kind, rawInput, locations}`), `options_offered`, `option_chosen`, `actor`, `expires_at`. One row answers one request and is consumed once. |
| Evidence link | `evidence_link_fact` (new) | one link from one claim/criterion revision to one artifact or attempt: `subject_kind`, `subject_id`, `subject_revision`, `artifact_ref`, `producer`, `baseline` (scope snapshot), `limitations`, `verification_status` |
| Assessment | `assessment_fact` (new) | one assessment (`change-packet` \| `test-worthiness` \| `drift` \| `rigor`) of one scope at one baseline with `omissions`; a new baseline is a new row |
| Checklist obligation | `checklist_obligation_fact` (new) | `obligation_id`, `checklist_id`, `revision`, `text`, `mandatory INTEGER NOT NULL`. The row's existence is "proposed". |
| Disposition | `disposition_fact` (new) | one actor's disposition on one subject at one baseline: `subject_kind` (`obligation` \| `assessment` \| `work` \| `investigation` \| `intent`), `subject_id`, `kind` (obligation: `acknowledged` \| `deferred` \| `needs-decision`; review: `fixed` \| `false-positive` \| `intentional` \| `deferred` \| `needs-decision`; work: `accepted`; investigation: `answered` \| `bounded-partial` \| `unresolved`; intent: `intent-accepted` referencing `(notebook_id, revision_no)`), `actor`, `reason`, `baseline`. **Satisfaction is never a disposition** — it is the fold over `evidence_link_fact` at the current baseline. |
| Harness profile | `harness_profile_dim` (new, interval-versioned) | `profile_id`, `engine_id` (FK to the catalog row — launch shape has one home), `family`, `native_home`, `declared_account`, `operator_token_ref` (opt-in; a reference, never the secret). A change of native home or account is a new version; attempts carry `profile_key`, so a past route never changes meaning. |
| Profile observation | `profile_observation_fact` (new) | one observation of one profile version: `kind` (`installed` \| `version` \| `authenticated` \| `account` \| `model-available` \| `quota` \| `capability:<name>`), `value`, `source`, `observed_at`, `duration_ms`, `exit_code`. Absence = Not recorded. |
| Saved layout | the layout envelope (ADR-0013/0032) | unchanged — a preference, not evidence |
| Coordination | read model (ADR-0044) | no rows |

**Deferred to the slice that has a producer:** `test_attempt_fact` (v3, WL-5, after the
test-result extractor spike); a steer amendment (WL-4 — expected to be an attempt row with
`amends_dispatch_key`, three link columns on one table). Declaring their DDL now would guess a
shape against an unknown producer.

### 2. Grain, history and additivity

One row is exactly one: dispatch attempt as bound at write-ahead · outcome event on one attempt ·
immutable revision · journey version · notebook version · permission decision on one request ·
evidence link · assessment at one baseline · disposition by one actor at one baseline · obligation
at one checklist revision · profile version · profile observation.

History rule: **every dimension is interval-versioned** (the `session_dim` pattern); no `UPDATE`
anywhere — the store's trigger set extends to the new dimensions (`BEFORE UPDATE … RAISE(ABORT)`),
and a DM11 test attempts the forbidden update. Type-0 attributes (`purpose`, `origin_*`,
`owner_journey_id`, `family`, `engine_id`) are additionally guarded by a `CHECK` that a new version
row carries the same value as its predecessor (enforced in `StoreWriter`, tested).

Additivity: counts of attempts/obligations/dispositions are additive over disjoint populations and
**levels over time** (a count in state X at T is not summed across T); rates keep numerator and
denominator; `quota` observations are **semi-additive** (a level, never summed over time); parallel
attempt durations are non-additive; tokens/cost live only in the run log's `usage` events and are
summed only where emitted — otherwise Not recorded.

### 3. Stored versus derived — one home per quantity

- **Host-asserted outcome rows** (`dispatch_outcome_fact.state`, CHECK-constrained — **the one
  vocabulary**, cited by ADR-0041): `Pending`, `Dispatched`, `Rejected`, `StopRequested`,
  `PermissionCancelledByStop`, `ResponseComplete`, `Failed`, `Interrupted`, `DeliveryUnknown`
  (with `cause`: `never_dispatched` \| `relay_lost` \| `stop_not_acknowledged` \| `prompt_timeout` \|
  `host_restart` \| `run_log_unwritable`), plus the existing PTY states, plus `WorktreeProvisioned
  {path, branch}` and `WorktreeDisposed {kind, reason, dirty}`. Terminals are `ResponseComplete`,
  `Failed`, `Interrupted`, `DeliveryUnknown`, `Rejected`; **at most one per attempt** (partial
  unique). The host writes them from its own correlated `await prompt` (ADR-0041), never from an
  uncorrelated event, and then writes the envelope's `consumed` row (ADR-0034), idempotent by
  `envelope_id`.
- **Fold-derived** (never stored): the non-terminal attempt states (running, waiting, denied,
  over-limit), `HasEvidence`, acceptance eligibility, closure, currentness/staleness, readiness.
- **Run log** (`runs/<dispatch_key>.jsonl`, built in WL-1, `RunLogStore`): one line per
  `RunEvent`, appended and flushed **before** the event is published to the relay; an outcome row
  that cites a causing `(run_id, seq)` is written only after that line is durable; a fold that finds
  a cited seq absent reads Not recorded, never a state. Frames are stored **redacted at ingest**
  (ADR-0042 §6).

### 4. Constraints the store enforces (one test per row, each attempting the violation)

| Constraint | Protects |
|---|---|
| `dispatch_attempt_fact.dispatch_key` PRIMARY KEY; key derived deterministically (ADR-0041 §1) | at-most-once attempt identity |
| partial UNIQUE `dispatch_outcome_fact (dispatch_key) WHERE state = 'Dispatched'` | one launch per attempt (P8) |
| partial UNIQUE `dispatch_outcome_fact (dispatch_key) WHERE state IN (terminals)` | one terminal per attempt; a timer cannot beat evidence |
| UNIQUE `permission_decision_fact (session_id, run_id, seq)` | one decision per request |
| FK `dispatch_attempt_fact (draft_id, revision_no) → prompt_revision_fact` | an attempt binds an existing revision |
| self-FKs `parent_revision_no`, `parent_dispatch_key`, `queued_after_dispatch_key` | retry/queue/branch chains resolve |
| FK `notebook_dim.owner_journey_id → journey_dim` with the legacy CHECK | ownership rule |
| CHECK on every closed enum (`purpose`, `origin_kind`, `state`, `cause`, disposition `kind`) | vocabulary drift |
| `BEFORE UPDATE/DELETE RAISE(ABORT)` on every new fact and dimension table | immutability |
| covering indexes `(key, ingress_seq DESC)` for every latest-per-key read; a P1-PERF-style no-full-scan plan assertion | read path |

Fold-only (correctly): acceptance eligibility, closure, currentness, steer-only-on-active.

### 5. Migration — the first `workspace.db` chain

- `WorkspaceSchema.Version = 2`; `EnsureSchema` reads `max(version)` and applies an ordered
  `Migrations` list through `UpgradeCoordinator.Run` (the watcher pattern); migration DDL emits the
  triggers for every new table (the fresh path gets them from `TriggerSql()`, the migrated path
  must too — tested by `sqlite_master` equality fresh-vs-migrated); new fact tables are appended to
  `FactTables`. A binary whose `Version` is below the file's `max(version)` opens **read-only** with a
  report.
- The expand is **additive only** (ADD COLUMN, CREATE TABLE); there is no copy and no contract
  step, so no cut-over window and no down path for a drop. Rollback = the pre-migration snapshot.
- Snapshot is WAL-safe: `VACUUM INTO` (or `wal_checkpoint(TRUNCATE)` with the store closed) and
  `Restore` deletes `-wal`/`-shm` first; one test: write under WAL → capture → write more → restore
  → pre-migration rows only, no replay.
- Backfill writes nothing it cannot know: no `kind`, no owner for legacy notebooks (`legacy = 1`),
  `body_sha` NULL for legacy rows; the migration report counts legacy notebooks to the human.

### 6. Purge and retention

Purge is by **journey**: a tombstone fact plus an export, then one maintenance transaction that
runs through a named privileged path (the trigger-drop path the compactor already uses:
rebuild-and-swap) and lists per AC-L01c every row and every `runs/<dispatch_key>.jsonl` it removed
(per attempt, never per session directory). It **refuses** while another journey's `origin_journey_id`
points at the purged one (reported, not cascaded) and states that the envelope stream (ADR-0034)
and provider-side data are not purged. The retention default remains the product decision the
spec defers; until recorded, nothing expires.

## Alternatives considered

- **A second attempt/outcome pair beside the dispatch receipt** (the first draft): rejected — the
  grain and the sweep are identical; two folds for one ubiquitous-language concept.
- **A separate `workbench.db`:** rejected — a second store for one workspace loses the ingress
  total order and the FK enforcement (DM6).
- **Copy `prompt_revision_fact` into a new table, then contract:** rejected — three nullable
  columns are the same meaning; the copy introduced a cut-over window and an untestable drop.
- **A `journey_link_fact` table:** rejected — every bridge creates its target; the link is Type-0
  columns on the created journey (re-opened only if the IA finds a bridge that links two existing
  journeys).
- **Stored obligation states (`proposed`, `satisfied-by`, `stale`):** rejected — each is derivable;
  storing them is the AC-P02c defect in table form.
- **Labelled current-state caches now:** rejected — indexes first; a cache only when NFR-02 is
  measured red (the `claim_current_cache` precedent remains available).
- **Event-sourcing everything into JSONL:** rejected — decisions and attempts need write-time
  uniqueness and FK rejection.

## Consequences

- **Positive:** v2 adds nine tables and five columns instead of sixteen tables; one attempt
  identity across PTY and native routes; every invariant has a constraint or a fold with a test;
  the migration mechanism exists after this change, not just the DDL.
- **Negative / accepted trade-offs:** latest-per-key reads everywhere (indexed); the run log is a
  second durable artifact with a stated commit order (line before publish, row after line); purge
  is a maintenance operation, not a `DELETE`.
- **Follow-ups / new risks:** run-log growth and compaction are undesigned (WL-4); FK/trigger
  overhead at scale unmeasured (P1-PERF-style run in WL-1b); `rebuild-equality` precedent to be
  cited by test file name in the slice.

## Evidence

- `WorkspaceSchema.cs:9, 35-38, 146-177, 201-208`, `WorkspaceStore.cs:73-96`, `StoreWriter.cs:130-179`,
  `UpgradeCoordinator.cs:24-30`, `SqliteWatcherObservationStore.cs:1064-1085`, `SessionPaths.cs:28-32`,
  `EnvelopeEvents.cs:218-223` — **Verified** 2026-10-18 (council reads).
- ADR-0002's spike S1–S8 (immutability) — **Verified** 2026-08-26.
- v2 DDL, migration up/down, WAL snapshot, purge — **Flagged** until the WL-1a reds exist.
