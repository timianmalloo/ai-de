# Lane — `claude-lane-main-red-0915` (liveness)

**Ownership lives in `docs/collaboration/session-contracts.md` §2. Nothing here restates it.**

| | |
|---|---|
| **Agent** | `claude-lane-main-red` (Claude Code, Opus 5) |
| **Session id** | `claude-lane-main-red-0915` |
| **Worktree** | `C:/Projects/ai-de-lane-main-red-0915` |
| **Branch** | `lane/main-red-0915` |
| **Based on** | `main` `bcf4959b` |
| **Status** | Ruling 126 groups 1–2 only — product tests, no product source |
| **Last updated** | 2026-09-16 |

## Doing right now

The repair for the tests that have kept `main` red since 2026-09-12 (issue #13; 15 failing at
`bcf4959b`, nine of them bit-identical to INV-0012's deterministic set). **Groups 1–2 only** per
Ruling 126: `[Trait("Platform","Windows")]` at **method** level on the four `EngineCatalogTests`
locator tests and on `PurgeAndTheSessionDeleteCascadeTests.ASiblingHeldOpen…` (attribute lines only —
class-level would strip Linux coverage from the other 16 tests in that class), plus one new portable
characterisation test, `TheLocatorTellsShimFromExecutableOnlyByWindowsSuffixTests`, that pins the
Linux behaviour and carries the residual and its trigger (Rulings 117, 118).

**Groups 3–4 are HELD** — the WPF extent tests and the intermittent App population — until the Atlas
accepted integration lands or **2026-09-18 21:00Z**, whichever is first; if the deadline fires it
goes back to the Owner with Atlas's status. The cap is a defect signal, not a licence to proceed.

## To peers

Measured before the first edit (Ruling 126 (i)): **neither** `atlas/main-integration` nor
`integration/atlas-five-gates` modifies any of the three files — `git diff --numstat` against each
merge-base returns `0 0` for all three — so a text merge cannot conflict on these edits. Notices sent
to the Atlas closer, the five-gates conductor and the watcher.

## Waiting on

| From | What |
|---|---|
| **Atlas / five-gates** | acknowledgement, and the accepted integration's landing (groups 3–4 unblock with it) |
