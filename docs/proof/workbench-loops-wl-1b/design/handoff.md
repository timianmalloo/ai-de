---
id: proof-workbench-loops-wl-1b-design-handoff
title: "Hand-off: T22 wl1b-design (WL-1b /design-slice)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1b"
tags: [handoff, workbench-loops, wl-1b, design]
links:
  - { to: design-workbench-loops-wl-1b, rel: implements }
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  What T22 left: the WL-1b design and the Proof Pack skeleton are committed; no code or tests by design. Lists
  the four DR drafts, the track cut, the one deviation (security rollups) and the next step.
---

# Hand-off: T22 `wl1b-design`

**Done.** `docs/design/workbench-loops-wl-1b.md` (the design, with the Patterns-Expert-vs-Simplifier and the
Data & Persistence reviews in its gate record) and `docs/proof/workbench-loops-wl-1b.md` (skeleton with the
draft human demo). This node wrote Markdown only: no code, no tests, so no red/green or suite files.

**Key decisions.** No schema change. No WL-1b disposition is an adoption, so no v3 (Ruling 156; proved by
T24's check B3). `checklist_id ≡ journey_id` (DR-1). No disposition covers a mandatory obligation (the
ADR-0040 default; DR-2). `work.accept` re-checks eligibility inside the writer transaction.

**For the Leader to raise (drafts, not rulings):** DR-1 checklist owner; DR-2 disposition policy; DR-3 two
currentness definitions (finding F1: `CurrentnessFold` has no caller in `src/`; the App's
`NotebookModel.CurrentnessOf` decides staleness alone); DR-4 where the declared deliverable lives.

**Deviation.** `docs/security/threat-model.md` / `privacy-review.md` rollups are outside this node's owned
paths. The Leader runs `docs-graph.py rollup` at the join.

**Track cut.** T23 `wl1b-folds` and T24 `wl1b-service` start now (T24 waits only on T23's record-types
commit for the evaluate path). T25 `wl1b-perf` n1 starts now; n2 after T24 joins. T26 `wl1b-checklist-kind`
after T24, T11 and T21 join; rules 19 and 20 gate its App suite.

**Next step.** Leader reads the design back, raises DR-1…DR-4, then dispatches T23, T24 and T25 n1.
