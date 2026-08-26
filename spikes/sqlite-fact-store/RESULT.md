# Spike result — sqlite-fact-store

- **Run:** 2026-08-26 · Windows 11 Pro 10.0.26200 · .NET SDK 10.0.303 · `Microsoft.Data.Sqlite` 10.0.11
- **Command:** `dotnet run --project spikes/sqlite-fact-store`
- **Exit:** 0 (ALL CASES PASS)

## Captured output

```
PASS S1-WAL — PRAGMA journal_mode=WAL returns 'wal' — observed: wal
PASS S2-UNIQUE — duplicate natural key rejected with SqliteException — observed: constraint error 19
PASS S3-IMMUTABLE — UPDATE and DELETE both abort via trigger — observed: both raised ABORT
PASS S4-REPLACE-BYPASS — INSERT OR REPLACE bypasses delete trigger when recursive_triggers=0 — observed: bypass CONFIRMED: 'immutable' row silently replaced (object=TAMPERED)
PASS S5-REPLACE-BLOCKED — with recursive_triggers=ON, INSERT OR REPLACE aborts — observed: REPLACE blocked by delete trigger
PASS S6-QUERY-ONLY — PRAGMA query_only=1 connection rejects writes — observed: write rejected: SQLite Error 8:
PASS S7-RECURSIVE-CTE — bounded recursive CTE traversal returns expected frontier — observed: depth-2 frontier = A,B,C,E (D,F correctly excluded)
PASS S8-NO-NESTED-TX — second BeginTransaction on one connection throws InvalidOperationException — observed: SqliteConnection does not support nested transactions.
ALL CASES PASS
```

## Contract established (cases only — a floor, not a verdict)

1. WAL mode, unique-constraint rejection, recursive CTE traversal, and the absence of
   nested transactions behave as the architecture states (S1, S2, S7, S8).
2. **Immutability triggers alone are not an immutability control**: with the default
   `PRAGMA recursive_triggers=0`, `INSERT OR REPLACE` silently deletes-and-replaces a
   fact row without firing the `BEFORE DELETE` trigger (S4). The store contract therefore
   mandates `PRAGMA recursive_triggers=ON` on every writer connection (S5 shows it closes
   the bypass), forbids REPLACE/UPSERT conflict resolution in the writer, and puts read
   connections on `PRAGMA query_only=1` (S6). SQLite has no permission system in an
   embedded context; the daemon-as-sole-writer process boundary is the actual control and
   the triggers/pragmas are defense-in-depth.
3. **Not established here:** behavior at the 50,000-edge corpus scale (index design,
   CTE latency, WAL checkpoint behavior under long reads). That is P1-PERF's job; scale
   claims remain Inferred until it runs.
