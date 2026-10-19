---
id: proof-workbench-loops-wl-1b-currentness-handoff
title: "Hand-off: WL-1b currentness (T27 node 1)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1b"
tags: [handoff, workbench-loops, wl-1b, currentness]
links:
  - { to: proof-workbench-loops-wl-1b-currentness, rel: implements }
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  What T27 node 1 changed, the input mapping onto CurrentnessFold, and the one App suite failure outside this track.
---

# Hand-off

**Done:** checks A, B, C red then green. No seam request: the fold needed no input `NotebookModel` lacks.

**Input mapping (no Core change, no new store read or IPC):** attempt = `AttemptRow` with `RevisionNo = ran.RevisionNo`
and `ProjectionSha = ran.BodySha`; current revision = saved head `latest.RevisionNo` (the ran revision if none);
live sha = `EnvelopeHash.Sha256Hex(liveDraft)`. The empty-draft clause is kept explicitly in `CurrentnessOf`: an empty
draft passes `ran.BodySha` as the live sha, with its `assume:` unchanged. `CurrentnessFold` was not edited (its mutation
in `A-mutation.txt` was reverted). The `ProjectionSha` field now carries a body sha for the notebook caller; same
comparison, different digest source.

**Deleted:** the old `CurrentnessOf` body. `NotebookModel.StaleCopy` is now `= CurrentnessFold.StaleCopy` (one literal).

**App suite:** 1 failure, `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`: "foreground not held".
It repeats alone, is a foreground/SendInput probe, and no file this track touched is on its path. Not run on the base
tree, so "pre-existing" is Inferred. Logged in `desktop-hold.txt`.

**Next:** none for T27. The caller `SessionDocumentSurface.cs:1560` is unchanged.
