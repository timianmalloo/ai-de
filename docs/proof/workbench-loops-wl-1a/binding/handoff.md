---
id: proof-workbench-loops-wl-1a-binding-handoff
title: "Hand-off — WL-1a T8 node 1: notebook model re-key, prepare-from-revision, stale marker"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, wl-1a, t8, evidence, handoff]
links:
  - { to: adr-0045-perspective-set-factory-review-understand, rel: implements }
  - { to: adr-0043-notebook-editor-codemirror-canonical-source, rel: implements }
  - { to: design-workbench-loops-wl-1a, rel: depends-on }
review-by: "2026-11-18"
summary: >-
  Track T8 (wl-t8-bind) node 1 of 2: the session state is re-keyed from surface to NotebookModel, Prepare saves an
  immutable notebook revision that the sent turn binds to, and an edit after a run marks the old result stale. Red and
  green evidence, what is not done, and node 2's next steps.
---

# WL-1a T8 node 1 — hand-off

Track T8 `wl1a-notebook-binding`, session `wl-t8-bind`, branch `wl1a-notebook-binding` (base `66ae2be7`).
Node 1 of the GO9 split: model re-key, prepare-from-revision, stale marker. Node 2 is not started.

## Done (red, then green)

| Check | Tests (`tests/AiDe.App.Tests/Workbench/Records/`) | Red | Green |
|---|---|---|---|
| Model re-key (ADR-0045 §4) | `TheModelOutlivesItsSurfaceTests.CreateHideMoveCloseAndReopen_NeverCreateOrDisposeTheNotebookModel` · `.ASurfaceBoundToAModel_LeavesTheModelAndItsThread_ToTheNextSurface` · `.ASurfaceThatOwnsItsModel_DisposesIt_AndTheCountSeesIt` (the falsifier) | `model-rekey-red.txt` | `model-rekey-green.txt` |
| Prepare-from-revision (ADR-0043 §2, v1 protocol) | `PrepareFromRevisionTests.APrepare_SavesOneImmutableRevisionPerDistinctBody` · `.TheAttemptForAPreparedRevision_IsBoundByTheStore_AndAnUnsavedRevisionIsRefused` · `.ThePrepareGesture_SavesTheRevisionFirst_AndTheSentTurnIsBoundToIt` | `prepare-from-revision-red.txt` | `prepare-from-revision-green.txt` |
| Stale marker (spec §C3) | `TheOldResultIsMarkedStaleTests.AnEditMarksTheResultStale_AndTheEmptyNextBlockDoesNot` · `.TheSurfaceShowsTheStaleCopy_WhenTheDraftIsEditedAfterARun_AndClearsItWhenEditedBack` | `stale-marker-red.txt` | `stale-marker-green.txt` |

Suites (`suite-green.txt`, both projects): **Core green** — 2935 passed, 1 skipped, 0 failed. **App red** — 1033 passed,
**1 failed**: `AiDe.App.Tests.SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` — "Ctrl+Enter
probe failed with exit code 2. foreground not held; SendInput would prove another window". T8 touches no Solution Tree
code (`git diff 66ae2be7` shows no SolutionTree file), and the probe needs the foreground, which other sessions on this
machine held. **Inferred** environmental, **not verified**: it was not re-run, per the Leader's stop at the node cap.
The suite binary predates two final edits (NotebookModel no longer disposes its semaphore; one doc comment).
`records-after-final-edits.txt` re-runs the 8 Records tests on the final build: green.

### What changed

- `src/AiDe.App/Workbench/Records/NotebookModel.cs` (new): `NotebookId` (derived from the session id, nothing stored),
  `NotebookRevision`, `INotebookRevisions` (the save seam), `ResultCurrentness`, and `NotebookModel` — the thread of
  turns, the revision chain, the turn → revision binding and the derived currentness. Disposal emits
  `notebook.model.dispose` on `aide.notebook.model`.
- `WorkbenchShell`: `_notebooks : Dictionary<NotebookId, NotebookModel>` and `_sessionDocuments` re-keyed to
  `NotebookId`; models are disposed only in `Dispose`; `Notebook(id)` reads the registry.
- `SessionDocumentSurface`: binds to a `NotebookModel` (the shell's) or owns one (built from a bare view model — every
  existing headless test); its thread is the model's; Launch binds the turn to its revision (`LastBinding`); the stale
  mark (`StaleNotice`, UIA name = the copy) is derived from the latest bound turn and the editor's bytes.
- `ComposerSurface`: `RevisionPreparer` runs before `ComposerSendGate.PrepareAsync` (prepare *from* a revision);
  `DraftChanged` fires after each compiled-view render. No page message kind is added; `ComposerMessageRouter` and
  `ComposerPageContract` are untouched (`git diff 66ae2be7 -- src/AiDe.Core/Presentation/Composer` is empty).
- `SurfaceContentFactory.SessionDocument` (the `session-document` row's Build): a retained document re-rendered into
  a new pane is released by its stale island first — DC-NEW-wl-t8-bind, found by the close → reopen step.

### Marked assumption

`assume:` an empty editor after a send is the composer's next block, not an edit, so it leaves the result current
(`NotebookModel.CurrentnessOf`). Confirm at the Owner's WL-1a demo step "edit and see the old result marked stale".
If false, one clause and one assertion change.

## Not done / red

- **Product revisions are not recorded yet.** The shell constructs `NotebookModel(model, revisions: null)`, because the
  App reaches the store only over IPC (D1) and no op saves a notebook revision. Seam request
  `req-01M59AS2R0986PT82ARD0QZP8G` (to the Leader, for T5): `notebook.open`, `notebook.revision.save`,
  `notebook.revision.latest`, and the Ruling 148 adoption actor (`notebook_dim` has no actor column). Until it lands,
  `RevisionState` reads "notebook revisions are not recorded…" and the stale mark cannot show in the product (every turn
  is `NotRecorded`). The tests prove the behaviour over a real schema-v2 store through T2's append API
  (`StoreNotebookRevisions`).
- **Ruling 148 adoption actor:** not implemented. It needs the schema column and op above.
- **Node 2 (not started):** run-outlives-pane (the per-run CTS `_runs`, the queue `_queued` and the envelope store
  handle still live in `SessionDocumentSurface`, so closing a pane still cancels its run); restart read-back; UIA names
  for the execution states; T3's follow-ups (`ComposerSurface.cs` "running" for `StopRequested`; `ConsoleSurface.cs`
  live states).

## Next (node 2), in order

1. Consume `IAttemptHost` (T5): Launch awaits `LastBinding`, builds `AttemptDispatch` from `Notebook.RevisionOf(ordinal)`
   (refuse when `NotRecorded`), and the CTS/queue leave the surface. Red first: hide the pane mid-run, reopen, the run
   continues.
2. On close, dispose the surface and keep the model; on reopen build a new surface over the same model (removes the
   DC-NEW-wl-t8-bind reparent path; `CreateHideMoveCloseAndReopen_…` stays the guard).
3. Wire the production `INotebookRevisions` over the daemon op once the seam request is granted; add the adoption actor.
4. Restart read-back; UIA names; T3's two follow-ups.
