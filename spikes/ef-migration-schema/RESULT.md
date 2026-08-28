# Phase-3 spike — EF Core migrations as schema evidence

**Run 2026-08-28** · corpus: `TheTerrace` — 62 migration classes, **zero `.sql` files**
**Re-run:** `dotnet run --project spikes/ef-migration-schema -- C:/Projects/TheTerrace`

## Why this replaced the planned DDL parser

The Phase-3 plan called for a DDL parser. The first repository it was checked against contains **no
DDL at all** — its schema is 62 EF Core migration classes in C#. A DDL parser would have shipped
with no corpus.

## The oracle was free and sitting next to the input

EF regenerates `AppDbContextModelSnapshot.cs` on every `migrations add` and checks it in, so the
repository carries an authoritative statement of its own current schema. Without it this spike would
have been its own parser agreeing with itself.

## Result

| | Count |
|---|---|
| Migrations folded | 62 |
| Tables after the fold | 64 |
| Columns | 566 |
| Parse time | **99 ms** |
| Tables EF maps | 62 |
| **Tables EF maps that the fold found** | **62 — 100.0%** |
| Tables the fold found that EF does not map | 2 |

## The two "extra" tables are the finding

`TeamAliasRepair` and `TeamAliasFixtureRepair` are created by a migration's `Up`, used by four raw
`Sql()` statements for a data repair, **and never dropped by `Up`** — the `DropTable` calls for them
are in `Down` (line 634+, after `Down` begins at 555). They are not mapped to an entity, so EF's
model snapshot does not list them.

**They exist in the database and are absent from the model.** So:

- The model snapshot answers *"what does EF map?"*
- The fold answers *"what do the migrations create?"*

For a graph about a database the second is the right question, and **the fold is more correct than
its own oracle here.** Treating the disagreement as a fold error would have taught the component to
hide real tables — which is exactly how a silently incomplete answer gets built on purpose.

The verdict is therefore asymmetric: a **missing** table is a defect in the fold; an **extra** one is
information, kept and disclosed.

## Constraints this inherits from Phase 2

Read as **syntax** — Roslyn parse trees only. No EF, no database, no `dotnet ef`. A repository's code
is read, never run.

Ordering is by the **timestamp prefix in the file name**, which is how EF orders them. Any other
ordering puts a create after a drop and produces a schema that never existed.

## Untested

- **Raw `Sql()` statements are not read.** Four of them in this repository create indexes and move
  data. A schema changed only by raw SQL is invisible to the fold and must be disclosed.
- Positional arguments. EF's generated migrations always use named arguments (`name:`, `table:`),
  which is what makes this readable without binding; a hand-edited migration using positional
  arguments is not read.
- Column *types* — property names are read, the builder calls describing their types are not.
- Multiple `DbContext`s in one repository.

## Consequence

Component 2 of the Phase-3 design is confirmed as an **EF-migration reader**, not a DDL parser, and
gains a second disclosure: `schema-changed-by-raw-sql-not-read`.
