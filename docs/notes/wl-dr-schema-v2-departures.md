---
id: note-wl-dr-schema-v2-departures
title: "Decision request — T2 schema v2 departures from ADR-0039 (for Ruling 148)"
type: decision-note
status: proposed
owner: "@timianmalloo"
tags: [decision, workbench-loops, schema, adr-0039]
links:
  - { to: adr-0039-workbench-records-durable-representation, rel: refines }
  - { to: coordination-workbench-loops, rel: relates-to }
review-by: "2026-11-18"
summary: >-
  T2 (branch sp1-schema-v2, green 586a7eae) departed from ADR-0039 in six places. The Data & Persistence
  Architect's adversarial review (PASS-WITH-CONDITIONS, veto cleared) verified the evidence for each and
  added two Major findings outside the six. The Leader recommends accepting all six with the amendment
  text below and making the conditions merge conditions.
---

# DR — schema v2 departures from ADR-0039

**Requester:** Leader `wl-leader-20261018`. **Rules:** Owner `wl-owner-20261018`, into
`docs/notes/addendum-c-council-rulings.md` as **Ruling 148** (the ADR-0039 amendment of record).
**Fallback if unruled:** T2 stays unjoined and WL-1a's host and fold tracks wait. No default is applied early.
**Evidence:** branch `sp1-schema-v2` (a5e66bea API · c9e2c8c3 red · 586a7eae green · 7369bcfa proof);
`docs/proof/workbench-loops-wl-1a/sp1/README.md`. The review read the tree and ran a SQLite 3.49.1 probe.
Every "Verified" below is the reviewer's. "Inferred" marks what it did not observe.

| # | ADR-0039 clause departed from | Evidence that forced it | Review verdict | Leader recommendation + amendment text |
|---|---|---|---|---|
| 1 | §3 "`dispatch_outcome_fact.state`, CHECK-constrained"; §4 "FK `dispatch_attempt_fact (draft_id, revision_no) → prompt_revision_fact`", the self-FK `parent_revision_no`, and "FK `notebook_dim.owner_journey_id → journey_dim`" | `ALTER TABLE … ADD CONSTRAINT` / `ADD FOREIGN KEY` is a syntax error (Verified, probe). §5 "The expand is **additive only**" forbids copy-and-swap. `notebook → journey` could not be an FK anyway, because the journey key is versioned `(journey_id, version_no)` (Verified). | ACCEPT-WITH-CONDITION. A trigger reference does not protect the parent: the delete succeeds and `foreign_key_check` is blind (Verified). Today the no-delete triggers cover it, but the §6 purge path drops triggers. | **Accept.** Condition: the post-purge reference check runs the same queries as the `attempts-bind-revisions` migration gate. *Amendment:* "Where SQLite cannot declare a constraint (ALTER on a v1 table, or a reference to a versioned dimension key), it is a BEFORE INSERT trigger raising `AIDE-STORE-VOCABULARY-VIOLATION` / `AIDE-STORE-REFERENCE-VIOLATION`. The migration gate and the post-purge check run the same reference queries, because `foreign_key_check` cannot see them." |
| 2 | §2 "every dimension is interval-versioned (the `session_dim` pattern)"; Context "`valid_from_seq / valid_to_seq`" | `session_dim` closes its interval by `UPDATE … SET valid_to_seq` (`StoreWriter.cs:161`) and has no immutability trigger. "No dimension is updated in place" and "interval closed by UPDATE" cannot both hold (Verified). | ACCEPT-WITH-CONDITION. The writer orders by `version_no`, the readers by `ingress_seq`, and the store enforces neither; on an `ingress_seq` tie, `ListJourneys`/`ListNotebooks` return two rows (Verified). | **Accept.** Condition: a per-dimension BEFORE INSERT trigger requires `NEW.version_no = coalesce(max,0)+1` and `NEW.ingress_seq > max(ingress_seq)` for the key, with one violation test each. *Amendment:* "v2 dimensions are append-only version rows without `valid_to_seq`. The interval is implied by the next version, and the latest version is the highest `version_no`, equal by a store-enforced trigger to the highest `ingress_seq`. `session_dim`'s UPDATE-closed interval is a v1 exception, not the pattern." |
| 3 | §2 Type-0 list includes `owner_journey_id` | §1 itself says legacy notebooks "are adopted by an explicit user action that writes a new version row", so §2 contradicted §1 (Verified, `StoreWriter.Workbench.cs:41-61`). | ACCEPT-WITH-CONDITION. The `legacy` flag is unguarded (an owned notebook can return to `legacy=1`); any caller can create a new `legacy=1` notebook; an adoption row records no actor. | **Accept.** Condition: `legacy` changes only 1→0; a version-1 `legacy=1` row comes only from the backfill; both tested. The adoption actor is recorded when WL-1a's binding lands (T8). *Amendment:* "`owner_journey_id` is Type-0 except for one transition, NULL→journey on a `legacy=1` notebook, written by explicit adoption. `legacy` changes only 1→0." |
| 4 | §1 "no attempt ever bound a legacy row (asserted by a migration query)" | `DispatchService.cs:96-103` and `BoundaryDispatcher.cs:68-71` save a revision and bind an attempt to it in one transaction, so the assertion is false on the PTY path (Verified). | ACCEPT-WITH-CONDITION. Related **Major**: post-migration PTY `SavePromptRevision` (`StoreWriter.cs:145-154`) writes no `body_sha` and no `notebook_dim` row. NULL `body_sha` stops meaning legacy, and `body_sha` has two homes (DM7). | **Accept.** Condition: one home for `body_sha`, either set by every revision writer with an equality test `sha256(body) = body_sha`, or always computed on read. Every revision save creates or requires a notebook, by writer or trigger. *Amendment:* "Legacy revisions may be bound by PTY attempts. The migration gate asserts every attempt binds an existing revision." |
| 5 | §1 `assessment_fact` names no content column | Without one, an assessment carries no findings: the ADR had a gap (Verified, `WorkbenchRecords.cs:146`). | ACCEPT-WITH-CONDITION. Nothing stops a producer from storing derived status in the JSON (the AC-P02c defect in JSON form; Inferred). | **Accept.** Condition: a per-kind payload schema that forbids derived-status keys (`satisfied`, `accepted`, `stale`, `current`…), plus a refusing test. Because WL-5 is the first producer, the schema and test may land with WL-5's producer; T2 adds the forbidden-key guard now. *Amendment:* "`payload` holds the producer's findings as asserted at the baseline. It never holds a state that §3 derives." |
| 6 | §5 "the migration report counts legacy notebooks to the human" (no API named) | `WorkspaceStore.Open` returns a `StoreOpenReport`; a newer file opens read-only and `BeginWrite` throws `AIDE-STORE-NEWER-SCHEMA-READ-ONLY` (Verified). | ACCEPT | **Accept.** *Amendment:* "`Open` returns a report of file version, binary version, read-only flag, migrated-from and legacy count. The legacy count reaches the human through it." |

## Findings outside the six (conformance fixes, not departures) — the Leader makes them merge conditions

- **[Major] The migration DDL is not frozen.** `V2Sql` is built from live lists (`Enum.GetNames<DispatchState>()`,
  `WorkbenchVocabulary.*`, `TerminalStates`). A later value is then accepted by a fresh store and rejected by a
  migrated file, and `FreshVersusMigratedTests` cannot see it because both paths read the same text (Verified).
  **Fix:** freeze the v2 DDL as a literal or a pinned hash, like `TheV1Baseline_IsFrozen`. Vocabulary growth becomes a v3 trigger.
- **[Major] Intent references are not enforced.** An `intent-accepted` disposition's `(notebook_id, revision_no)` has only a
  NOT NULL CHECK. `disposition_fact` is a new table, so a real composite FK to `prompt_revision_fact (draft_id, revision_no)`
  can be declared (Verified). **Fix:** declare it.
- **[Minor] The migration gate does not check existing outcome `state` values against the vocabulary.** An unknown v1 value
  would throw at `Enum.Parse` on read (Inferred). **Fix:** a gate query, with a report row.

## Residual risk (accepted unless the Owner rules otherwise)
- Behaviour on real v1 files with unknown states or orphans.
- Trigger overhead at 100× rows, which WL-1b measures (ADR-0039 Follow-ups).
- The purge path with triggers dropped (condition 1 covers its reference check).
- The probe ran SQLite 3.49.1 through Python, not Microsoft.Data.Sqlite's bundled e_sqlite3. The ALTER grammar is stable across versions (Inferred).

## What happens after the ruling
A T2 rework session in the same tree (`C:\Projects\ai-de-sp1-schema-v2`) lands every accepted condition and both
Major fixes, red first. The Leader re-reads the state. T2 joins only with a green suite, or with the census-read
failures shown to be baseline (they fail on `main` without T2: the WL-0 join's first recount,
`AppWindowCloseLeavesNoTerminalHostTests:82`). Track T12 is fixing that census.
