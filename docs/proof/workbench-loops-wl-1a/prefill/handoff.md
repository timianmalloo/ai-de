---
id: handoff-workbench-loops-wl-1a-prefill-n1
title: "Hand-off: WL-1a T30 node 1 (Ruling 180 (1)-(3))"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, wl-1a, handoff, compile-mode, ruling-180]
links:
  - { to: architecture, rel: implements }
  - { to: proof-workbench-loops-wl-1a-prefill, rel: relates-to }
review-by: "2026-11-19"
summary: >-
  T30 n1 stopped at the Leader's rule-16 kick with (A), (B) and (C) green and the whole suites run. Open: the seam for
  the product wiring of (A) and (B), the restart half of C1, and node 2's cost line, first-50 record and eval run.
---

# Hand-off — T30 node 1

**Done:** (A) default, (B) control, (C) kept line — each red then green; see `README.md` for shas and proof files.
Whole suites: `suite-node1.txt`.

**Red:** none of this node's tests. Two App failures with the foreground not held (SolutionTreeChord, CtrlDigit),
re-run after 2 min, same result — see `suite-node1.txt`.

**Next step (in order):**
1. Seam `req-01M5ATGTTQS78NG5W7ZMZE8G00` was GRANTED at 19:48Z, after the kick (`mail-01M5AV9ZAC6D46942MYCN1XTVH`), and is
   not used yet. Wiring it is not one step: a deterministic red through File → New Session needs the sheet's gate input
   injected. (i) `NewSessionSheetViewModel.Create` passes
   `compileModes: _adapterInstallRoot is null ? null : CompileModeGate.Evaluate(_adapterInstallRoot)`; (iii)
   `ComposerSurface.AdapterInstallRoot` getter, and `SessionDocumentSurface.CompileModeLadder` defaults to the gate over
   it. Red first through File → New Session (the sheet's test seam) and through the settings with no injected ladder.
2. C1's restart half needs the composer draft persisted in the App (`ComposerDraftStore` has no App caller) — a
   decision for the Leader, not a one-line change.
3. Node 2: Ruling 180 (4) cost line and the first 50 advisory calls; (6) `tools/compile-eval`; the Proof Pack.
