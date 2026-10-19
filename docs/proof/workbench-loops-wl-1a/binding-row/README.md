---
id: proof-workbench-loops-wl-1a-binding-row
title: "Evidence — WL-1a T16: the turn row shows its attempt binding"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, evidence]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  Red and green evidence that the turn row's provenance disclosure and its UIA HelpText show the attempt's revision, request and route (Rulings 161, 162).
---

# T16 binding row

Line: `Sent from revision {n} · request {first 7 of projection_sha} · via {route}`; `Attempt not recorded` with no attempt; a missing field reads `not recorded`.

- `binding-row-red.txt`: 11 of 14 fail on assertions (stubs return the fingerprint / empty string).
- `binding-row-green.txt`: 14 of 14 pass.
- `suite-green.txt`: Core 3018 pass; App 1113 pass plus one timing test that failed in the full run and passed alone.

Tests: `tests/AiDe.App.Tests/Workbench/Records/TheTurnRowShowsItsAttemptBindingTests.cs`.
No HTML view is rendered (surface cap).

## T16b fix-forward

- Gap 1 (restart): not buildable as briefed. The store holds the attempt (read-back finds it), but no production path restores turns into a reopened thread, and a live turn's `EnvelopeId` is the placeholder `turn-1`, so there is no row and no key to map. Pinned by `AfterARestart_TheStoreHoldsTheAttempt_ButTheThreadHasNoRowToSayOtherwise`. Seam request sent.
- Gap 2 (AC-R01a): `ASendEditSendSequence_LeavesTurnAsRowOnItsRevisionAndRequest_AndTurnBOnItsOwn` drives send A, edit, send B through the notebook and host. It passed first run (the behaviour already held), so it has no red.
- Gap 3: `SurfaceContentFactory` now reads `RouteWords.TerminalWord`; `TheTerminalWord_HasOneSource_AndBothSurfacesShowIt` (red: source still held the literal).
- Late-receipt window: `NotebookRuns.AttemptsChanged` and `ThreadFeed.RefreshAttempts` exist and are tested (`AReceiptThatLandsAfterTheSnapshot_...`, red: "Attempt not recorded"). Wiring them needs one line in `SessionDocumentSurface.cs` (seam request sent). Until then the window is open: `Record` runs after the host's dispatch at `NotebookRuns.cs:561` and a row's attempt is read only on the next thread snapshot (`ThreadFeed.Merge`).
- Files: `t16b-red.txt`, `t16b-green.txt`, `t16b-suite-core.txt` (3018 pass), `t16b-suite-app.txt` (1118 pass, 1 `SolutionTreeChordTests` timing failure, passes alone), `t16b-chord-alone.txt`.
- Wiring (granted, req-01M59NNMNABZ2FS5CJMTA9RHMP): `SessionDocumentSurface` subscribes `Notebook.Runs.AttemptsChanged` (marshalled to the UI thread) and unsubscribes on dispose. `ASentTurnsRow_ShowsItsBinding_WhileTheRunIsStillHeld_ThroughTheSurface` passed BEFORE the wiring (`t16b-wiring-red.txt` is a pass), so the product path shows no window in that sequence (later snapshots refresh the row); no red could be forced through the surface. The test stays as a guard.
