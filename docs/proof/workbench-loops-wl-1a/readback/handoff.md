---
id: proof-workbench-loops-wl-1a-readback-handoff
title: "Hand-off — WL-1a T17d: the read-back runs when the host binds"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, evidence, handoff]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  T17d after node 2: checks 1-3 are red then green, the whole suites ran, and the Proof Pack and the slice acceptance
  are written. The App suite has one regression (NoDispatchOutsideTheHostTests:47), waiting on seam req-01M5A52AQHR32CPQFSBS9NGJ4T; do not join yet.
---

# T17d hand-off (node 2)

## State

- **Checks 1–2** (node 1): red 04b75bf0 → green 69452f35. The read-back starts when the host binds, once per bind.
- **Check 3**: red 7bd617ad (per file), then node 2 made it per site, red 0c92dafd → green 4be08710.
  - Converted to the shell's order (`BindHostWhenOpen`, opener completed after construction): `AReopened…:59` (ReopenAsync), `:310` (HeldReads), `TheAttemptHostBindingTests:89`, `TheRows…:77`. No assertion changed.
  - Named in `NotAShellReopen`: seven first opens over an empty store, plus `TheReadBackRunsWhenTheHostBindsTests:197`, which binds before construction on purpose to keep Ruling 172(2)'s before-construction case.
  - Mutation: with 69452f35's `src/` reverted, the converted files fail 7/14 (every reopen test).
- **Suites: not green** (`suite-run.txt`). Core 3024/0/1. App 1134 passed, 2 failed, 2 skipped:
  - `NoDispatchOutsideTheHostTests.ATurnSentBeforeTheHostOpens_WaitsForIt_AndRunsAsAnAttempt`: a regression from 69452f35, deterministic. Its :47 expects a row right after Send while the host opens, and the Send now waits (Ruling 172(3)). Seam req-01M5A52AQHR32CPQFSBS9NGJ4T is open.
  - `SolutionTreeChordTests`: environmental (foreground not held).
- **Proof:** `README.md` (this folder); the slice pack's Acceptance section reads "reopened (Ruling 172)" with these paths and points at demo step 18.

## Next step

1. Rule seam req-01M5A52AQHR32CPQFSBS9NGJ4T. If granted, change `NoDispatchOutsideTheHostTests.cs:47` to `Assert.Empty(model.Thread.Current.Turns)` and take the ordinal after `opening.SetResult(host)`. Re-run the file and then the App suite, and save `suite-green.txt`. Only then join `wl1a-readback`: until then the branch carries one red test.
2. After the join, the user's walk-through includes demo step 18 (restart: every recorded attempt is a row again).
3. T11 merges main after T17d (Ruling 172), for D2.
