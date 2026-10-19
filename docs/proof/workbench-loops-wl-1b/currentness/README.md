---
id: proof-workbench-loops-wl-1b-currentness
title: "Proof: WL-1b currentness (T27 wl1b-currentness, node 1)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1b"
tags: [proof, workbench-loops, wl-1b, currentness]
links:
  - { to: design-workbench-loops-wl-1b, rel: implements }
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  Ruling 178: NotebookModel.CurrentnessOf delegates to CurrentnessFold, so the notebook's stale notice and the
  checklist's evidence state share one fold, and the stale sentence is one literal in src/.
---

# Proof: one currentness definition

Tests: `tests/AiDe.App.Tests/Workbench/Records/OneCurrentnessDefinitionTests.cs` (model-level, no window).

| Check | Test | Red | Green |
|---|---|---|---|
| A | `A_TheNotebookNoticeAndTheChecklistEvidence_ReadStale_FromTheSameFold` | `A-red.txt` (fold ran 1 time, not 2) | `A-green.txt`; `A-mutation.txt` (fold mutated to Current: both surfaces flip, `(Current, Satisfied)`) |
| B | `B_CurrentnessOf_KeepsItsVerdicts_AndAsksTheFoldForEachRecordedOne` | `B-red.txt` (0 fold runs, not 3) | `B-green.txt` |
| C | `C_TheStaleSentenceIsOneLiteralInSrc_AndEverySurfaceNamesTheSameValue` | `C-red.txt` (2 files held the literal) | `C-green.txt` |

B's behaviour half (verdicts) already held before the change, so its red is the delegation signal only. The old
behaviour is pinned by the existing `TheOldResultIsMarkedStaleTests` and `TheProductRecordsRevisionsTests`, which still pass.

Suites: `suite-node1.txt` (Core 3052 passed / 1 skipped / 0 failed; App 1150 passed / 2 skipped / 1 failed, see handoff).
`desktop-hold.txt` logs the one foreground failure.
