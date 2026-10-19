---
id: proof-workbench-loops-wl-1b-service-handoff
title: "Hand-off: T24 wl1b-service node 2"
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
  What node 2 of T24 left: A, B and C are done, the Core suite is green, and only the D6 golden JSON remains.
---

# Hand-off: T24 `wl1b-service`, node 2

**Done.** A (`checklist.evaluate`), B (endpoint round-trip) and C (Ruling 176 at the store writer) are committed with
red and green output; see `README.md`. The Core suite is green (3085 passed, 0 failed, 1 skipped, `suite-node2.txt`).
No App suite was run.

**The A and B reds are mutations.** The code existed from node 1, so there was no natural red; the README says what was
mutated. C was a natural red.

**C moved the enforcement.** The writer could know (it reads `journey_dim` in its transaction), so the field
`ObligationsSetRequest.ChecklistId` and its two tests are gone. Two Core test fixtures were edited outside the brief's
owned list (`cl1` to `j1`); the README names them. Leader: the Owner may want to see that Ruling 176 now lives at
`StoreWriter.Workbench.cs` `AppendObligation`.

**Next node, not done.** The D6 golden JSON for the request and result records: `ObligationsSetRequest` (now without
`ChecklistId`), `ObligationsSetResult`, `DispositionRecordRequest/Result`, `EvidenceLinkRequest/Result`,
`ChecklistEvaluateRequest/Result` (carries `AcceptanceEvaluation`, `WorkClosure`), `WorkAcceptRequest/Result`.
