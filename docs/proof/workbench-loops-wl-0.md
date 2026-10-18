---
id: proof-workbench-loops-wl-0
title: "Proof Pack: workbench-loops WL-0 (FR-001, FR-002)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-0"
tags: [workbench-loops, freshness, ipc-frame]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  A forced reindex under an unchanged git revision now indexes edited files, and entry-point
  listings are bounded by serialized bytes so they fit one IPC frame. Both proven red then green.
---

# Proof Pack: workbench-loops WL-0

- **Change:** branch `wl0-evidence`; red `e10b29d7`, green `e7aa167e`
- **Spec / design:** `docs/reviews/forensic-review-rev102.md` FR-001, FR-002
- **Tier:** T2
- **Author / date:** wl-t1-wl0 / 2026-10-18

## Claims & evidence

### Claim 1: FR-001 — same revision, dirty content: the new fact is indexed, the old one is not current
- **Evidence:** `docs/proof/workbench-loops-wl-0/fr001-red.txt` (fails: "a forced index of a changed file must emit the new facts") and `fr001-green.txt` (2 passed).
- **Oracle (and why it's trustworthy):** `DirtyEditSurvivesReindexTests.ADirtyEditUnderAnUnchangedRevisionIsIndexedAndTheOldFactIsNotCurrent` indexes a Bicep scope, edits the file, forces a reindex with the same revision string, then reads current assertions. It fails if the early return or the unique key comes back. `TheContentComponentNeverLeaksIntoTheShownRevision` holds `Base` stripping.
- **Red observed before green:** yes
- **Confidence:** Verified (executed)
- **Residual risk:** the digest is path, size and mtime (`ScopeFingerprints.Compute`), so an edit that keeps size and mtime is missed, as the fingerprint cache already accepts. `RefreshScopeAsync` called directly with a bare revision still cannot see edits; only the `IndexCSharpAsync` path adds the digest. The shell was not touched, so a real run through the shell is not yet observed.

### Claim 2: FR-002 — 1,300 facts of 2,000 characters fit the frame
- **Evidence:** `fr002-red.txt` (5,140,618 bytes against 1,048,576) and `fr002-green.txt` (3 passed).
- **Oracle:** `EveryOperationFitsTheFrameTests.EntryPointsRowsAreBoundedByBytesNotOnlyByCount` measures the serialized result with the existing frame oracle `WireBytes` and checks `OmittedByCap` equals the rows dropped.
- **Red observed before green:** yes
- **Confidence:** Verified (executed)
- **Residual risk:** the budget is `MaxResponseBytes - 512`; at least one row is always kept, so one row over 1 MiB alone would still overflow.

### Suite
- `suite-green.txt`: AiDe.Core.Tests 2783 passed, 1 skipped, 0 failed. `app-suite-green.txt`: AiDe.App.Tests 1062 passed, 0 failed.

## Verification commands
```
dotnet test tests/AiDe.Core.Tests -nologo
dotnet test tests/AiDe.App.Tests -nologo
```

## Human demo script (Owner)
Build: `dotnet build src/AiDe.App/AiDe.App.csproj -c Release`, then run `src/AiDe.App/bin/Release/net10.0-windows/AiDe.App.exe` (path Inferred from the project's target framework; confirm in the build output).
1. Open a workspace with a Bicep or C# project. Run the index command (Ctrl+K, then Shift+I). Note a fact, such as a resource name.
2. Edit that file and save it; do not commit, so HEAD is unchanged. Run the index with force.
3. Expected: the new fact appears and the old one is gone. Not yet joined: the shell captures HEAD once and passes it in, which the Core fix now handles; the shell seam (WorkbenchShell.cs, track T4) is untouched, so confirm after the join.
4. Open the entry-points view on a repository with very many types. Expected: a bounded list with an "Omitted (n)" disclosure, no "could not be loaded" error.

## Flagged risks / residual unknowns
Old snapshots carry no digest in their revision, so the first reindex after upgrade re-extracts every scope once (the extractor generation bump has the same effect).
Accepted by the Owner as named (2026-10-18): the size+mtime digest residual, and a single entry-point row larger than 1 MiB (the one-row floor).

## Status & next action
| | |
|---|---|
| **Completed** | FR-001 and FR-002 red-green, both suites green |
| **Remaining** | GUI demo of steps 2–4 owed to the user — **not observed**. The Owner reproduced red (`e10b29d7`) and green (`acaa4358`) by test only (2026-10-18). The GUI run is folded into the WL-1a real-lane demo after T4's shell seam joins; FR-001's shell path stays unobserved until then. |
| **Best next action** | Leader re-runs the Core suite on the joined tree |
