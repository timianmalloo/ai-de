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

## Human demo script (confirmed against origin/main a85ffa58 by reading the code; not yet run by a person)

*Keyboard only. Steps 3 and 6 start real runs and need a real account on the claude-code lane. The tests prove this path
with a stub harness over a real daemon and store, not with a real adapter.*

### 0. Build and launch
Run from a **clean worktree of origin/main**, never the primary checkout (as the WL-1a script, step 0).
1. `dotnet build AiDe.sln -c Release`
2. Run `src/AiDe.App/bin/Release/net10.0-windows/AiDe.App.exe`.

### 1. Declare the work
3. **Ctrl+1** (Factory). **Ctrl+N** opens *New session*; create a session as in WL-1a steps 3-4. In the editor, type a
   request for a plan, for example: "Write a rollback plan for the schema v3 migration. Do not implement it." Do not send yet.
4. Open the **View** menu and choose **New checklist** (the menu offers it in Factory and Understand). **Expect:** a pane titled "Checklist — {your notebook's title}" docks on the right; it reads
   "No obligations yet. Add the conditions this work must meet." and the top line reads
   "Blocked — no obligations are declared for this work."
5. Tab to the text box "New obligation", type "The plan names the rollback trigger.", Tab to **Add obligation**, press
   Space or Enter. **Expect:** the row shows the text, "Mandatory", "Evidence missing." and "Not reviewed." (every added
   obligation is mandatory; there is no toggle). The top line reads
   "Blocked — “The plan names the rollback trigger.” has no current evidence."

### 2. Acknowledged is not satisfied
6. On the row, press **Acknowledge**. **Expect:** the row's second label reads "Acknowledged — not satisfied.
   Acknowledging is not evidence." The evidence label still reads "Evidence missing." and the top line still reads the
   same Blocked sentence.

### 3. Run, and link evidence (real model and adapter)
7. Return to the editor and send with **Ctrl+Enter** (under an agentic compile rung the first press prepares and the
   second sends). Let the run complete. This also saves revision 1 of the notebook.
8. In the checklist, press **Link the latest run** on the row (it is disabled until a run has completed; there is no
   picker, it links the newest run). **Expect:** the row reads "Satisfied by run {7 characters} (intent revision 1)."
   The top line reads "Blocked — intent revision 1 is not accepted."

### 4. See acceptance blocked with the named gap
9. Press **Accept deliverable**. **Expect:** the store refuses, and the note under the top line is read out as
   "Not accepted. Blocked — intent revision 1 is not accepted." The top line is unchanged. Nothing is accepted.

### 5. Accept the intent, then the declared deliverable
10. Press **Accept intent (revision 1)**. **Expect:** that button now reads "Intent revision 1 is accepted." and is
    disabled; the top line reads "Ready to accept."
11. Press **Accept deliverable**. **Expect:** the top line reads "Accepted: the declared deliverable at intent revision 1.
    This records acceptance of the deliverable only. It does not say the work is implemented or merged."

### 6. Stale evidence is never relabelled current (real model and adapter)
12. In the editor change the draft text, then send with **Ctrl+Enter**. This saves revision 2 (there is no separate
    save) and starts a second real run. **Expect:** the row reads "The evidence belongs to an earlier draft. Link
    evidence from the current draft." The top line reads "Accepted at an earlier intent revision. The intent has changed
    since, so this acceptance does not cover the current revision." The intent button reads "Accept intent (revision 2)"
    and is enabled.
13. Press **Accept deliverable**. **Expect:** the note under the top line reads "Not accepted. Blocked — “The plan names
    the rollback trigger.” has only evidence from an earlier draft. Blocked — intent revision 2 is not accepted."
    Nothing new is accepted.

### 7. Saved review state is not acceptance
14. Add a second obligation as in step 5 ("The rollback is rehearsed."). On its row press **Needs decision**.
    **Expect:** its row reads "Evidence missing." and "Needs a decision."
15. Close the app and start it again, and reopen the same workspace. **Expect:** before the notebook is revived the
    pane may read "This checklist belongs to work journey {id}. Its notebook is not open. Reopen it from File → Recent
    sessions to review this checklist." Once the notebook is back, the checklist shows the same rows and states, and
    the top line still reads "Accepted at an earlier intent revision. …". No new acceptance row exists: pressing
    **Accept deliverable** reads out "Not accepted." with the Blocked sentences, including
    "Blocked — “The rollback is rehearsed.” needs a decision."

### Still owed
- A person runs steps 3 and 6 on the real lane. No test covers them with a real adapter.
- Not confirmed in code: the status line after *New checklist* (no literal "Checklist opened." was found); that closing the window saves the layout (the tests call the save directly); that a workspace must
  be reopened for the revive; the Alt+V route to the View menu.

## Acceptance

*Empty until the Owner rules.*
