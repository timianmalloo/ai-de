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

## Node 2

**Done:** (A) `CurrentnessFold.Evaluate(int ranRevisionNo, int? currentRevisionNo, string? ranSha, string? liveSha)` is the one
definition; the `AttemptRow` overload is a thin call passing `ProjectionSha`. `NotebookModel.CurrentnessOf` (line ~230) calls the
named form with body shas and builds no `AttemptRow`. Red `ec07c1e0`, green `21785bfc`. A source scan fails if any `src/` file
outside `src/AiDe.Core/Store/` constructs an `AttemptRow`. No `ShaKind` enum: the fold only compares two strings, and a label it
never reads would be dead code; the caller's one digest definition is named in the doc comment.

**(B) / Ruling 180 (5): no red test is possible; seam request sent** (`req-01M5AEKWQQYP4YK499Y5DN6GPD`). The notebook delegation
already reads no stored projection and makes no compile call: `NotebookModel.cs:232` compares the ran revision's `BodySha` with the
draft's body sha, so a re-prepare cannot move it. The only src path passing a live projection sha to the fold is
`AcceptanceFold.cs:140` via `AcceptanceInput.LiveProjectionSha` (`AcceptanceFold.cs:17`), and no src code builds an
`AcceptanceInput` yet (tests only). `SessionDocumentSurface.cs:728` re-projects the stored envelope fold for the attempt gate, not
for the fold. C2 is a contract on T24's builder: take the live side from stored rows, never `RenderView` or a prepare call.

**(C)** `SolutionTreeChord...RequestsRevealInGraph` fails on the clean base `27e761b7` ("foreground not held"), so it is
pre-existing, observed (`C2-base-chord.txt`). The base tree was removed.

**Suites:** Core 3060/0/1 skipped; App 1150 passed, 1 failed (the chord), 2 skipped. `suite-node2.txt`.
