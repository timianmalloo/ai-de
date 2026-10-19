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

# Proof: WL-1b checklist service (T24, node 1)

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

- **Ruling 176 enforcement point.** `ObligationsSetRequest` carries an optional `ChecklistId`. Null or the journey id is
  the one checklist; anything else is refused with `AIDE-WORK-SECOND-CHECKLIST`. The service always writes rows with the
  journey id as `checklist_id`. The `simplify:` comment at the check records the upgrade trigger (the v3 owner column).
- **Ruling 177.** The service never reads `AcceptancePolicy`; it only calls `AcceptanceFold`, so no disposition covers a gap.
- **Ruling 179.** `WorkAcceptResult.Line` is `Accepted — intent revision N.`
- **Writer gate then read.** Every write takes `BeginWrite` (the single writer gate) before `BeginRead`, so no commit
  can land between the read and the append. WAL lets the read connection run beside the open writer.
- **Evidence causes** are the first word of the `AIDE-WORK-EVIDENCE-NOT-ELIGIBLE` message: `not-this-journey`,
  `not-complete`, `not-current`. A missing attempt reads as `not-this-journey`.
