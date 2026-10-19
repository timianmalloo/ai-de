---
id: proof-workbench-loops-wl-1b-folds
title: "Proof: WL-1b folds (T23 wl1b-folds, node 1)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1b"
tags: [proof, workbench-loops, wl-1b, folds]
links:
  - { to: design-workbench-loops-wl-1b, rel: implements }
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  Red-then-green evidence for checks A1, A2 and A3 of WL-1b: acceptance eligibility and work closure as pure
  Core folds over the v2 rows. Core suite green. No store write, service, IPC or UI.
---

# WL-1b folds: proof

| Check | Red | Green | Tests |
|---|---|---|---|
| A1 acknowledged is not satisfied | `A1-red.txt` (10 failed, stub throws) | `A1-green.txt` (10 passed) | `AcceptanceFoldTests.A1_*` |
| A2 blocked without a current link | `A2-red.txt` (7 failed of 14) | `A2-green.txt` (24 passed, whole class) | `AcceptanceFoldTests.A2_*` |
| A3 work closure | `A3-red.txt` (4 failed) | `A3-green.txt` (4 passed) | `WorkClosureFoldTests.A3_*` |
| Whole Core suite | | `suite-node1.txt` (3052 passed, 1 skipped, 0 failed) | |

Code: `src/AiDe.Core/Workbench/Records/Folds/` (`AcceptanceFold.cs`, `WorkClosureFold.cs`,
`AcceptanceBaseline.cs`, `AcceptancePolicy.cs`). The record types were published first (ab72eaca) for T24.

## Rulings applied

- Ruling 177 (DR-2): `AcceptancePolicy.CoveringKinds` is empty; a test pins every vocabulary kind.
- Ruling 179 (DR-4): the baseline is `intent:<notebook_id>@<revision_no>`. The folds produce no "Accepted" wording,
  so no refusing wording test is owed here. T25/T26 carry the Accepted line.

## Choices the design left open (marked, for review)

- A link whose artifact is not `attempt://<key>`, or whose attempt is absent from the input, reads `NotRecorded`
  (never current).
- A link with no obligation-history row at its `subject_revision` reads as a changed criterion (`Stale`).
- Several links: Satisfied beats Stale beats NotRecorded beats Missing. A link that is not `observed` is ignored,
  so an obligation with only such links reads `Missing`.
- `intent-not-accepted` is emitted only when the notebook has a revision (otherwise `intent-missing` stands alone).
- The baseline of a notebook with no revision is `""` (`AcceptanceBaseline.NoRevision`); it equals no stored baseline.
- `WorkClosureFold` prefers an `accepted` row at the current baseline, else the latest by `ingress_seq`.
