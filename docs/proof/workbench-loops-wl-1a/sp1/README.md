# WL-1a sp1 — workspace schema v2 (ADR-0039 §1–§5)

Track `wl-t2-sp1`, branch `sp1-schema-v2`. Commits: API `a5e66bea` · red `c9e2c8c3` · green `586a7eae`.
Each red file was produced against the typed API with the v1 schema still in place; each green file
after the schema, migration and snapshot work. Test classes live in `tests/AiDe.Core.Tests/Store/SchemaV2/`
(namespace `AiDe.Core.Tests.SchemaV2`).

| Check | Test class (tests) | Red | Green | Commits (red / green) |
|---|---|---|---|---|
| Migration golden round-trip: real v1 file (WAL, rows in every table) opens as v2, every v1 column byte-equal, legacy notebooks backfilled (`legacy=1`, no owner), `body_sha`/new columns NULL, report counts legacy notebooks, snapshot cleaned; a failing migration restores v1 | `MigrationGoldenTests` (4: `AV1Store_MigratesToV2_...`, `AMigratedStore_AcceptsTheV2Api`, `AFailedMigration_PutsTheV1StoreBack_...`, `TheV1Baseline_IsFrozen`) | `golden-migration-red.txt` | `golden-migration-green.txt` | c9e2c8c3 / 586a7eae |
| WAL snapshot/restore: write under WAL, capture, write more, restore -> pre-migration rows only; restore deletes `-wal`/`-shm` | `WalSafeSnapshotTests` (2) | `wal-snapshot-red.txt` | `wal-snapshot-green.txt` | c9e2c8c3 / 586a7eae |
| Fresh vs migrated `sqlite_master` equality (type, name, table, normalised DDL), incl. triggers of every fact/dimension table | `FreshVersusMigratedTests` (1) | `fresh-vs-migrated-red.txt` | `fresh-vs-migrated-green.txt` | c9e2c8c3 / 586a7eae |
| DM11 forbidden update/delete on every v2 table and on a widened v1 table | `ForbiddenUpdateTests` (19) | `dm11-forbidden-update-red.txt` | `dm11-forbidden-update-green.txt` | c9e2c8c3 / 586a7eae |
| One violation test per ADR-0039 §4 row (key PK; one `Dispatched`; one terminal x5 pairs; one permission decision; attempt->revision; parent revision / parent dispatch / queued-after / profile FKs; notebook->journey and legacy CHECK; every closed enum: purpose, origin_kind, state, cause, disposition kind and kind-per-subject, intent reference, assessment kind, observation kind) | `ConstraintTests` (28) | `adr-0039-4-constraints-red.txt` | `adr-0039-4-constraints-green.txt` | c9e2c8c3 / 586a7eae |
| Covering-index read path: `EXPLAIN QUERY PLAN` of the exact SQL each by-key / latest-per-key reader runs has a SEARCH and no SCAN | `ReadPlanTests` (13) | `read-plans-red.txt` | `read-plans-green.txt` | c9e2c8c3 / 586a7eae |
| Read-only on a newer file: `Report.ReadOnly`, `BeginWrite` throws `AIDE-STORE-NEWER-SCHEMA-READ-ONLY`, reads work, epoch and `schema_version` untouched | `ReadOnlyOnNewerFileTests` (1) | `read-only-newer-file-red.txt` | `read-only-newer-file-green.txt` | c9e2c8c3 / 586a7eae |
| Typed API: round trip of all 12 row types, new version row per dimension change, Type-0 guard (journey purpose/origin, profile family/engine, notebook owner after adoption), revision branch + body hash, PTY path unchanged | `TypedStoreApiTests` (7) | `typed-api-red.txt` | `typed-api-green.txt` | c9e2c8c3 / 586a7eae |
| Whole `AiDe.Core.Tests` project | all | — | `suite-green.txt` | 586a7eae |

Suite: 2856 tests, 2851 passed, 1 skipped, 4 failed. The 4 failures are `TerminalHostInLifePathTests` (2) and
`TerminalHostExitPathTests` (2), all with "the process table could not be read" — an environment limit of
`ConsoleHostCensus`; those files do not reference the store. `AiDe.App.Tests` builds with the extended
`DispatchState`; `PromptBarTests` pass (7/7).
