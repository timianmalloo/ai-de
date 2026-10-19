---
id: proof-workbench-loops-wl-1b-service
title: "Proof: WL-1b checklist service (T24, B1-B3)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1b"
tags: [proof, workbench-loops, wl-1b, service]
links:
  - { to: design-workbench-loops-wl-1b, rel: implements }
  - { to: adr-0040-journey-purpose-identity, rel: implements }
review-by: "2026-11-18"
summary: >-
  Red and green output for checks B1, B2 and B3 of the WL-1b checklist service, with the Core suite run of node 1.
---

# Proof: WL-1b checklist service (T24, nodes 1 and 2)

Each check was written red first, saw its failure, and was committed before the code that turned it green.
Tests: `tests/AiDe.Core.Tests/Checklist/ChecklistOperationsTests.cs` (through the service, over a real SQLite v2 store).

| Check | Red | Green | What it proves |
|---|---|---|---|
| B1 accept only when eligible, in one transaction | `B1-red.txt` (4ef9607d) | `B1-green.txt` (fc82fe3b) | a gap refuses with `AIDE-WORK-ACCEPTANCE-BLOCKED` and writes no row; eligible writes one; again returns the same row; an edit after the intent was accepted is seen by the re-fold inside accept; the blocked path emits `work.accept`; the result names "intent revision N" and never "implemented", "done" or "merged" (Ruling 179) |
| B2 evidence link guards | `B2-red.txt` (d1e6d454) | `B2-green.txt` (5a2ada04) | other journey's attempt, a Failed / Interrupted / Rejected / running attempt, a stale attempt and an unrecorded projection are refused with the cause first in the message; `artifact_ref` equals `AttemptArtifactRef` and `HasEvidence` reads true; an identical link writes nothing; unknown obligation, wrong disposition kind and 2,001-character text or reason are refused |
| B3 no adoption, one checklist per journey | `B3-red.txt` (edec591e) | `B3-green.txt` (a1aa13df) | the whole scenario leaves `notebook_dim` identical; legacy, terminal-drafts, investigation, foreign and unknown notebooks are refused on all five operations; two journeys get distinct obligation ids; a second checklist for a journey is refused with `AIDE-WORK-SECOND-CHECKLIST` and writes nothing (Ruling 176) |

`suite-node1.txt` is the whole Core suite on this branch: 3069 passed, 11 failed, 1 skipped. All 11 failures are the
`wl1b-folds` branch's own committed red tests (7 of A2, 4 of A3) that came in with the dependency merge; none is a
`ChecklistOperationsTests` or `ReadPlanTests` row.

## Decisions a reader should know

- **Ruling 176 enforcement point (moved in node 2, check C).** `StoreWriter.AppendObligation` refuses a `checklist_id`
  that is not a `purpose = work` journey's id with `AIDE-WORK-SECOND-CHECKLIST`. The writer can know: it reads
  `journey_dim` through the same transaction (`ScalarRow`, as `AppendJourney` does). `ObligationsSetRequest.ChecklistId` is
  removed. The `simplify:` comment with the upgrade trigger (the v3 owner column) now sits in the writer.
- **Ruling 177.** The service never reads `AcceptancePolicy`; it only calls `AcceptanceFold`, so no disposition covers a gap.
- **Ruling 179.** `WorkAcceptResult.Line` is `Accepted — intent revision N.`
- **Writer gate then read.** Every write takes `BeginWrite` (the single writer gate) before `BeginRead`, so no commit
  can land between the read and the append. WAL lets the read connection run beside the open writer.
- **Evidence causes** are the first word of the `AIDE-WORK-EVIDENCE-NOT-ELIGIBLE` message: `not-this-journey`,
  `not-complete`, `not-current`. A missing attempt reads as `not-this-journey`.

## Node 2

| Check | Red | Green | What it proves |
|---|---|---|---|
| A `checklist.evaluate` | `A-red.txt` (280fb9e2, mutation) | `A-green.txt` (bdd53549) | not eligible and `NotAccepted` before acceptance; `EvidenceCandidates` holds only completed attempts of the head revision; after acceptance the closure is `Accepted` at the current baseline, then historical (`AtCurrentBaseline` false) after an intent edit; a non-work notebook is refused |
| B endpoint round-trip | `B-red.txt` (451d1b96, mutation) | `B-green.txt` (38e9c37b) | all five operations over the pipe through `RegisterChecklist`; a blocked accept and an unknown obligation arrive as `IpcRequestException.Code` (`AIDE-WORK-ACCEPTANCE-BLOCKED`, `AIDE-WORK-OBLIGATION-UNKNOWN`); the disposition, link and work rows carry `shell:<pid>`, the pipe principal |
| C Ruling 176 at the writer | `C-red.txt` (8599a8b9, real red) | `C-green.txt` (0cdc4486) | the writer refuses a made-up id and an investigation journey's id and writes nothing; a work journey's id is accepted |

**A and B reds are mutation runs, not natural reds.** `checklist.evaluate` and `RegisterChecklist` already existed from
node 1, so the new tests passed at once. Each red file says which line was changed (the candidates filter replaced by
`true`; the `work.accept` actor replaced by `"payload"`), shows the test failing for that reason, and the source was restored
before the commit. C is a natural red: the writer did not refuse.

`suite-node2-start.txt`: 3080 passed, 0 failed, 1 skipped at 1e1e0ba7 (the folds are green). `suite-node2.txt`: 3085 passed,
0 failed, 1 skipped. Net tests: +7 added (A 3, B 1, C 3 cases: one Theory of 2 plus one Fact), -2 removed (the two B3
request-field tests).

**Files outside the brief's owned list, edited because C forced them:** `SchemaV2Support.cs:144` and `TypedStoreApiTests.cs:44-45`
seeded an obligation under checklist id `cl1`, which is no journey. They now use `j1`, the work journey the same seed creates.
