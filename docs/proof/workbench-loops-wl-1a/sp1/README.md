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

## Ruling 148 (merge conditions)

Red `3ea879ef` · green `5af6bc43`. All tests are in `tests/AiDe.Core.Tests/Store/SchemaV2/Ruling148Tests.cs` and run through
Microsoft.Data.Sqlite's bundled engine. The two (f) pin tests were added in the green commit, because they pin the final
DDL text; the red (f) test is the const-literal check. Suites: `AiDe.Core.Tests` 2891 passed, 1 skipped, 0 failed
(`suite-green.txt`); `AiDe.App.Tests` 1062 passed, 0 failed (`suite-app-green.txt`). No census-read failures occurred.

| Cond. | Test class (tests) | Red | Green | sha (red / green) |
|---|---|---|---|---|
| (a) post-purge check = the gate's queries | `Ruling148PostPurgeCheckTests` (1) | `r148-a-red.txt` | `r148-a-green.txt` | 3ea879ef / 5af6bc43 |
| (b) per-dimension version-order trigger | `Ruling148VersionOrderTests` (12) | `r148-b-red.txt` | `r148-b-green.txt` | 3ea879ef / 5af6bc43 |
| (c) legacy only 1 to 0; v1 legacy=1 only from backfill | `Ruling148LegacyFlagTests` (3) | `r148-c-red.txt` | `r148-c-green.txt` | 3ea879ef / 5af6bc43 |
| (d) one home for `body_sha`; a revision requires a notebook | `Ruling148BodyShaAndNotebookTests` (5) | `r148-d-red.txt` | `r148-d-green.txt` | 3ea879ef / 5af6bc43 |
| (e) forbidden derived-state keys in assessment payload | `Ruling148AssessmentPayloadTests` (9) | `r148-e-red.txt` | `r148-e-green.txt` | 3ea879ef / 5af6bc43 |
| (f) v2 DDL frozen (literal + pinned hash) | `Ruling148FrozenDdlTests` (3) | `r148-f-red.txt` | `r148-f-green.txt` | 3ea879ef / 5af6bc43 |
| (g) composite FK intent disposition to revision | `Ruling148IntentReferenceTests` (2) | `r148-g-red.txt` | `r148-g-green.txt` | 3ea879ef / 5af6bc43 |
| (h) gate reports unknown outcome states | `Ruling148OutcomeStateGateTests` (1) | `r148-h-red.txt` | `r148-h-green.txt` | 3ea879ef / 5af6bc43 |

Decisions to read back. (1) The PTY `SavePromptRevision` now creates a non-legacy notebook owned by a standing journey
`journey-terminal-drafts`, because (c) forbids a new legacy notebook and the CHECK needs an owner for a non-legacy one.
(2) `body_sha` equality is held by one helper (`StoreWriter.BodySha`) plus a store trigger that refuses a NULL or non-64-char
hash; SQLite has no sha256 function, so the trigger cannot test equality itself, and the tests assert it for both writer paths.
(3) The forbidden-key list includes `state` and `status`; WL-5's per-kind schema may refine it. (4) `StoreCompactor` is the
(a) purge path; `TerminalStates` in `WorkspaceSchema` was removed as dead once the DDL became a literal.
