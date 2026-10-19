---
id: wl-s-float-guard-proof
title: "WL-S T20 float guard - proof pack"
type: proof-pack
status: active
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-19
summary: "Ruling 171 (1): workbench.floatPane refuses with a stated cause, and a restored floating window is docked with a report line; red and green for both checks."
---

# T20 float guard (Ruling 171 (1))

Float is not drawn yet (T21), so a floated pane vanished and could not be moved or closed. This
guard keeps every pane visible until T21 lands.

| Check | Test (tests/AiDe.App.Tests/Workbench/FloatGuardTests.cs) | Red | Green |
|---|---|---|---|
| 1. floatPane refuses | `FloatPane_RefusesWithAStatedCause_PaneStaysDockedAndFocused` | floatpane-refusal-red.txt | floatpane-refusal-green.txt |
| 2. restore docks floating | `RestoringAFloatingWindow_DocksThePaneIntoItsKindsZone_DrawnReachableAndReported` | restore-docking-red.txt | restore-docking-green.txt |

Suites: suite-green.txt (Core 3020 passed, 1 skipped; App 1125 passed, 1 skipped, 1 foreground flake that passes alone).

## What changed

- `WorkbenchController.cs`: the `workbench.floatPane` case announces `FloatUnavailableAnnouncement` and changes nothing. With no focused pane it still says "No pane is focused."
- `src/AiDe.Core/Workbench/FloatingRedock.cs` (new): `Dock` moves each floating pane into the zone of its kind (`TreeToZones.ClassifyStack`) and returns the report line.
- `LayoutPersistence.cs` lines 151-152 and 162 only: call `Dock` before `RestoreZones`, append `redock.Report(...)` to the announcement. T11 also edits this file; the change is those three lines.
- Re-scoped: `ARefusedOperation_IsStillAnnounced` used floatPane as its example refusal; it now uses collapsePane.

## Notes for the Leader

- Red 1 first failed on a fixture defect (Factory's default has no pane in the projected tree), so the check-1 test was corrected and the red re-run; floatpane-refusal-red.txt records both. Both checks' first red run is commit 426025ca; the corrected red follows it.
- No `notebook` kind exists on this base, so check 2 floats a terminal (its zone is Bottom). The code is kind-generic.
- Seam request sent with the two sentences (the refusal const and the restore report line) and the DESIGN.md copy-row question. Built with the proposed sentences; DESIGN.md untouched.
- The "kind has no zone" fallback is `TreeToZones.ClassifyStack`'s own default (Left); admission still runs after and drops an inadmissible kind with the existing report.
- T21 removes the refusal and the `Dock` call.
