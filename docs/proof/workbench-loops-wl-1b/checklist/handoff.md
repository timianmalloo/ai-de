---
id: proof-workbench-loops-wl-1b-checklist-handoff
title: "Hand-off — WL-1b T26 checklist kind after node 3"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1b"
tags: [workbench-loops, handoff, checklist]
links:
  - { to: architecture, rel: implements }
  - { to: proof-workbench-loops-wl-1b-checklist, rel: relates-to }
review-by: "2026-11-18"
summary: >-
  T26 node 3 is done: the Owner's copy confirmation (CHK3 and CHK9 changed, pinned by literal tests that were red
  first) and M6 settled by option (b), the checklist Invalidate removed with the refreshing path named. The next step
  is the WL-1b checklist join.
---

# T26 hand-off after node 3

The two conditions the Owner set before the T26 join are closed. Detail is in `README.md`, under "node 3".

**(F) The Owner's copy confirmation.**

- Red `2ce683f0`: `TheChecklistSaysTheOwnersCopyTests` pins CHK3 and CHK9 as literals (`copy-red.txt`).
- Green `9f7d557a`: `ChecklistCopy.cs`, DESIGN.md CHK3 and CHK9, and design `:426`, in one commit (`copy-green.txt`).
- The design's CHK3 line `:418` changed in the node-3 close commit. Seam `req-01M5BDHVX8T545SQC0V4BESVF9` was granted
  for it.
- The Owner's confirmation, with the date and the two changes, is recorded in `README.md`.

**(G) M6, option (b).**

- `4e4b0710`: the (a)-shaped test. It is green with the call (`m6-green-with-call.txt`) and green under M6
  (`m6-b-mutation-not-red.txt`). A probe (`m6-b-probe-visibility-edges.txt`) found the path.
- `02f9c02d`: the call is removed (`WorkbenchShell.cs:3714-3720`), and a two-line comment names the path. 13 of 13
  checklist App tests are green (`m6-b-green-without-call.txt`).
- The path: `WorkbenchShell.cs:1930` (every open renders) → `WorkbenchAdapter.cs:303` (every render re-parents the
  panes it keeps) → the checklist's `IsVisibleChanged` → `RefreshAsync`, which resolves the notebook lazily.

**Suites on `02f9c02d` (`suite-node3.txt`).**

- Core: 3119 passed, 1 skipped, 0 failed.
- App: 1256 passed, 2 skipped, 2 failed of 1260. Both failures are node 2's known ones, and neither is a checklist
  test.
  - `CtrlDigitSwitchesThePerspectiveFromInsideAPageTests` fails on `foreground held=False`.
  - `AReopenedNotebookRendersItsRecordedAttemptsTests.ARestoredRowsEvents_…` fails alone too, and it fails the same way
    with the M6 call restored. So it is verified as not caused by this node.

**Owed:**

- Desk runs for the two App failures above.
- Inferred and not run: a checklist in a host other than Factory. See the README residual. It does not separate M6.

**Ratchet:** see `ratchet.txt`.

**Next step:** the WL-1b checklist join.

**M9 (Owner):** the reopen test depends on the visibility refresh. With `RefreshAsync()` in the `IsVisibleChanged` handler replaced by `Task.CompletedTask` it is red (`"Add obligation" is disabled`; `m9-red-visibility-refresh-mutation.txt`). Reverted, `git diff src` empty, 3 of 3 green (`m9-green.txt`).
