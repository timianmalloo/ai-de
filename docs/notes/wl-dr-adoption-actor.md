---
id: note-wl-dr-adoption-actor
title: "Decision request — where Ruling 148's notebook adoption actor lives (v3 column vs fact row)"
type: decision-note
status: accepted
owner: "@timianmalloo"
tags: [decision, workbench-loops, schema, adr-0039, ruling-148]
links:
  - { to: adr-0039-workbench-records-durable-representation, rel: refines }
  - { to: coordination-workbench-loops, rel: relates-to }
review-by: "2026-11-18"
summary: >-
  Ruling 148 obliges T8 to record who adopted a legacy notebook. notebook_dim has no actor column and the v2 DDL is
  frozen. Within the Owner's two admissible shapes, the Data & Persistence Architect (Adversary, read-only) recommends
  (a) a v3 expand-only migration adding notebook_dim.adopted_by, enforced by one BEFORE INSERT trigger. It does not
  block WL-1a unless T8 writes an adoption row. The Leader recommends (a), landed in the first slice that offers
  adoption to the user.
---

# Decision request — the notebook adoption actor (Ruling 148)

**Asked by:** Leader `wl-leader2-20261019`, 2026-10-19. **Rules:** Owner `wl-owner-20261018`. **Source:** item 4 of T8's
seam request `req-01M59AS2R0986PT82ARD0QZP8G`. The Owner bounded it on report 4: no edit to the frozen v2 text, no re-pin.

## The question
Where does the adoption actor live: (a) a v3 migration adding `notebook_dim.adopted_by`, or (b) an append-only fact or
disposition row keyed by the adopting version row? And when does it land?

## The review (Data & Persistence Architect, Adversary Mode, read-only; verdict PASS-WITH-CONDITIONS for (a))
- **v2 edit: BLOCK (Verified).** `V2Sql` is a const (`WorkspaceSchema.cs:230`) whose hash is pinned
  (`Ruling148Tests.cs:380-387`), and `ApplyMigrations` runs only versions above the file's (`WorkspaceStore.cs:206`). An
  edited v2 would give fresh stores the column and migrated stores none.
- **(b) still needs a migration (Verified).** `disposition_fact` allows only five subject kinds in a column CHECK
  (`WorkspaceSchema.cs:336`, `:348`), which SQLite cannot alter. `evidence_link_fact` has a different grain (`:292-305`).
  So (b) is a new table, which is a v3 migration too. Its grain ("one actor per adopting version") is the version row's
  own grain (ADR-0039:100): a 1:1 split, not a fact. Whichever row is written first, the "every adoption has an actor"
  rule cannot be a trigger, only a gate query.
- **(a), as recommended.** Grain: one `notebook_dim` row is one notebook version `(notebook_id, version_no)`.
  `adopted_by` is NULL on version 1 and required above 1. There is no `adopted_at`, because `recorded_at` is that time
  (DM7). DDL: `ALTER TABLE notebook_dim ADD COLUMN adopted_by TEXT NULL` plus `trg_notebook_adoption` (BEFORE INSERT,
  `AIDE-STORE-ADOPTION-VIOLATION`).
  Each invariant gets a red-first test, and the store must refuse:
  1. an adoption with a NULL or blank actor;
  2. an actor on version 1;
  3. adopting into `journey-terminal-drafts`;
  4. a second adoption;
  5. an adoption row with `legacy = 1`.
  Invariants 3 and 4 move code-only rules (`StoreWriter.Workbench.cs:60-70`) into the store. **Invariant 5 is stricter
  than `trg_notebook_legacy_flag` (`:413-418`), so the Owner must accept that tightening explicitly.**
- **Migration and rollback.**
  - `Version = 3`, plus `(3, V3Sql, BackfillV3)` in `Migrations` (`:458-461`). `EnsureSchema` also runs `V3Sql` on the
    fresh path (`WorkspaceStore.cs:310-312`).
  - `FreshVersusMigratedTests` is extended to fresh v3 against v1→v3 and v2→v3, and a v3 hash is pinned beside the
    unchanged v2 pin.
  - Rollback: snapshot restore through `UpgradeCoordinator` (`WorkspaceStore.cs:171-188`); an old binary opens a v3 file
    read-only (`:97`, `:135-147`). A tested down step drops the trigger, then the column, then the version row, and
    asserts the schema equals a fresh v2 store.
  - Backfill writes nothing: a pre-v3 adoption reads "actor not recorded". A `StoreOpenReport` count is informational.
    `AppendNotebook` has no production caller (Verified by grep).
- **WL-1a: not blocked, with one condition.** If T8 writes any `notebook_dim` row above version 1 (including a re-own out
  of `journey-terminal-drafts`, Ruling 150), v3 lands inside T8, red first, before T8 joins. Otherwise v3 lands in the
  first slice that offers adoption, and until then `AppendNotebook`'s adoption branch refuses. Run the demo on an owned
  notebook.
- **Residual risk.** The review executed no SQLite statement: `DROP COLUMN` and the trigger's NULL handling stay Inferred
  until the red tests run. Trigger cost at 100x is unmeasured (WL-1b). Real store files were not inspected.

## Options
- **A (recommended):** shape (a) as above, landing in the first slice that offers adoption (T8 if it writes an adoption
  row). Until then, `AppendNotebook`'s adoption branch refuses with a stable code. Ruling 148's obligation is tracked
  against that slice. Invariant 5's tightening is accepted.
- **B:** shape (a), landed now as its own track inside WL-1a regardless of T8. Cost: one more Opus track with a schema
  migration on the WL-1a critical path, for a capability the demo does not exercise.
- **C:** shape (b), a new `notebook_adoption_fact` in v3. The reviewer rates it weaker: the actor rule can only be a gate
  query, and the row is a 1:1 split of the version row.

## Default if no ruling by the deadline
A, without the explicit tightening of invariant 5 (the v3 track then implements invariants 1–4 only, and 5 waits for a
ruling).

## Ruling
**Ruling 156** (Owner, 2026-10-19T06:06:44Z, `docs/notes/addendum-c-council-rulings.md`): option **A with invariant 5**. v3 lands in the first slice that offers adoption. Until then `AppendNotebook`'s adoption branch refuses with a stable code (carried in T8's brief). If a WL-1a track must write an adoption row, v3 lands inside it first.
