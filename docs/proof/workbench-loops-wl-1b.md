---
id: proof-workbench-loops-wl-1b
title: "Proof Pack: WL-1b — obligations, evidence and acceptance"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1b"
tags: [proof, workbench-loops, wl-1b, checklist, obligation, evidence, disposition, acceptance]
links:
  - { to: architecture, rel: implements }
  - { to: adr-0039-workbench-records-durable-representation, rel: implements }
  - { to: adr-0040-journey-purpose-identity, rel: implements }
  - { to: design-workbench-loops-wl-1b, rel: implements }
review-by: "2026-11-18"
summary: >-
  The slice Proof Pack skeleton for WL-1b: one row per §WL.11 WL-1b check, each with an empty evidence cell
  for its track (T23 folds, T24 service, T25 bench, T26 checklist kind). It carries the draft human demo
  script. Nothing here is evidence yet.
---

# Proof Pack: WL-1b — obligations, evidence and acceptance

*Skeleton written by T22 (`wl1b-design`) at `e8944ebc`. Every evidence cell is empty until its track joins.
The design is `docs/design/workbench-loops-wl-1b.md`.*

## Claims & evidence — one row per §WL.11 WL-1b check

| §WL.11 check (architecture.md:1504) | Design checks | Track | Evidence folder | Red | Green |
|---|---|---|---|---|---|
| acknowledged ≠ satisfied (a row vs a fold) | A1 | T23 | `workbench-loops-wl-1b/folds/` | — | — |
| acceptance blocked while a mandatory obligation lacks a current link | A2, B1, B2 | T23, T24 | `…/folds/`, `…/service/` | — | — |
| saved review state is not acceptance | A3, D3 | T23, T26 | `…/folds/`, `…/checklist/` | — | — |
| FK/trigger overhead measured P1-PERF-style at 100× | C1 | T25 | `…/perf/` | — | — |
| no WL-1b disposition is an adoption (Ruling 156; design decision) | B3 | T24 | `…/service/` | — | — |
| demo: accept a plan deliverable without claiming implementation (AC-W03c) | D1, D2 | T26 | `…/checklist/` | — | — |

## Oracles — why each is trustworthy

*To be written per check at each join: what the test reads, and why it could fail.*

## Measurements (T25)

*Empty. The bench output (`bench/AiDe.Bench -c Release -- wl1b`) lands under `workbench-loops-wl-1b/perf/`.*

| Measure | 1× | 100× breadth | 100× depth | Budget |
|---|---|---|---|---|
| append p95, product store | — | — | — | reported |
| append p95, control store (no WL-1b triggers/FK) | — | — | — | reported |
| overhead (product − control) | — | — | — | reported |
| `checklist.evaluate` p95 | — | — | — | ≤ 100 ms |
| EXPLAIN plans, WL-1b reads | — | — | — | no full scan |

## Residual risk

*From the design, to be re-read at the close:* R1 (the live projection must be the attempt envelope's), R2
(harness child and the pipe capability, Inferred), R3 (100 ms is the nearest stated budget), R4 (obligation
removal is a new revision only).

## Human demo script (DRAFT — the Leader confirms every gesture against the code after T26 joins)

*Draft by T22. Gestures and strings come from the design, not from built code. Every step is **rehearse**
until the Leader reads it against the T26 join head. Keyboard only.*

### 0. Build and launch
Run from a **clean worktree of origin/main**, never the primary checkout (as the WL-1a script, step 0).
1. `dotnet build AiDe.sln -c Release`
2. Run `src/AiDe.App/bin/Release/net10.0-windows/AiDe.App.exe`.

### 1. Declare the work
3. **Ctrl+1** (Factory). Open a notebook (as WL-1a step 3–4). Type a request for a **plan**, for example:
   "Write a rollback plan for the schema v3 migration. Deliverable: a plan. Do not implement it."
4. From **View**, open **New Checklist**. **Expect:** "No obligations yet. Add the conditions this work must
   meet."
5. Add one obligation, mark it *Mandatory*: "The plan names the rollback trigger."
   **Expect:** the row reads *proposed* · "Evidence missing". The top line reads
   "Blocked — “The plan names the rollback trigger.” has no current evidence."

### 2. Acknowledged is not satisfied
6. On the row, choose *Acknowledge*. **Expect:** "Acknowledged — not satisfied. Acknowledging is not
   evidence." The top line still reads the same *Blocked* sentence.

### 3. Run, and link evidence
7. Send the notebook (**Ctrl+Enter**) and let the run complete on the real lane.
8. On the row, choose *Link evidence* and pick the completed run. **Expect:** "Satisfied by run {7 characters}
   (revision 1)."

### 4. See acceptance blocked with the named gap
9. Choose *Accept deliverable*. **Expect:** the store refuses, and the reason is read out:
   "Blocked — intent revision 1 is not accepted."

### 5. Record a disposition, then accept the declared deliverable
10. Choose *Accept intent (revision 1)*.
11. Choose *Accept deliverable*. **Expect:** "Accepted: the declared deliverable at intent revision 1. This
    records acceptance of the deliverable only. It does not say the work is implemented or merged."

### 6. Stale evidence is never relabelled current
12. Edit the notebook and save (revision 2). **Expect:** the row reads "The evidence belongs to an earlier
    draft. Link evidence from the current draft." The acceptance line reads "Accepted … at intent revision 1"
    as history, and the top line is *Blocked* again for revision 2.

### 7. Saved review state is not acceptance
13. Add a second mandatory obligation and mark it *Needs decision*. Close the app and reopen it.
    **Expect:** the checklist restores with the same states. No new acceptance exists, and the top line reads
    "Blocked — “…” needs a decision."

### Still owed
- The Leader's gesture table (file:line per gesture) after T26 joins.
- The real-lane run in step 7 is the user's.

## Acceptance

*Empty until the Owner rules.*
