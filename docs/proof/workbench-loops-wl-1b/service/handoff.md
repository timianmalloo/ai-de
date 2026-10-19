---
id: proof-workbench-loops-wl-1b-service-handoff
title: "Hand-off: T24 wl1b-service node 1"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1b"
tags: [handoff, workbench-loops, wl-1b, service]
links:
  - { to: design-workbench-loops-wl-1b, rel: implements }
  - { to: adr-0040-journey-purpose-identity, rel: implements }
review-by: "2026-11-18"
summary: >-
  What node 1 of T24 left: B1, B2 and B3 are green, the five operations are registered, and what remains.
---

# Hand-off: T24 `wl1b-service`, node 1

**Done.** B1, B2 and B3 are red then green (see `README.md`). The five operations (`checklist.evaluate`,
`checklist.obligations.set`, `checklist.disposition.record`, `checklist.evidence.link`, `work.accept`) are in
`ChecklistService`, registered by `WorkspaceOperations.RegisterChecklist`, wired in `Program.cs`, and reachable through five
`WorkspaceClient` methods. The three new readers are in `StoreReader.Workbench.cs` with `ReadPlanTests` rows.
The `assume:` on `AttemptArtifactRef` is deleted (the writer now exists).

**Not green, and not mine.** The branch carries `wl1b-folds` at its A2-red commit. Seven A2 tests and four A3 tests
fail until T23's folds are green. Two consequences for this track:

1. `checklist.evaluate` calls `WorkClosureFold.Evaluate`, which is still a `NotImplementedException` stub on this branch.
   `checklist.evaluate` has no test yet for that reason. `work.accept` builds its `WorkClosure` directly from the row it
   just wrote, so it does not depend on the stub.
2. B1's eligible path ran against the A1 fold, which marks any `observed` link Satisfied. When T23's A2 is green, B1 and
   B2 must be re-run: stale and unrecorded links then show as gaps inside `work.accept`.

**Next step (node 2).** Re-merge `wl1b-folds` once T23 is green, then: (a) a test for `checklist.evaluate` and its
`EvidenceCandidates`; (b) an endpoint round-trip test through the daemon pipe for the five operations, including a refusal
code coming back as an error response and the actor being the pipe principal (the registration has no test of its own yet);
(c) the D6 golden JSON per request and result record.

**Not in scope, left alone.** Ruling 178 (`NotebookModel.CurrentnessOf` delegating to `CurrentnessFold`), the bench, the
checklist kind, all App code.
