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

## Node 2 - DOCK3 wording and zone label (Owner-confirmed DOCK2, DOCK3)

Tests: `tests/AiDe.Core.Tests/Workbench/FloatingRedockCopyTests.cs`.

| Check | Test | Red | Green |
|---|---|---|---|
| 1. exact sentences | `TheSingularSentence_IsExactlyThePinnedWording`, `ThePluralSentence_IsExactlyThePinnedWording`, `TheDesignRowDock3_CarriesTheSameSentencesWordForWord` | dock3-wording-red.txt (plural and DESIGN row fail on assertions) | dock3-wording-green.txt (4/4) |
| 2. zone label | `TheZoneInTheReport_IsTheShellsUserFacingLabel_NeverTheInternalZoneId` | dock3-zone-label-red.txt | dock3-zone-label-green.txt |

- Plural is now exactly `{N} floating panes in your saved layout are docked: {Title} in {zone}, {Title} in {zone}.` The wording lives in three consts on `FloatingRedockResult` (`SingularFormat`, `PluralFormat`, `PluralItemFormat`); the DESIGN.md DOCK3 row (line 1385, the only DESIGN.md edit) is checked against them.
- Zone label source: `ZoneLayoutService.ZoneName`, `src/AiDe.Core/Workbench/ZoneLayoutService.cs:272-279` (identical on origin/main, checked with `git show origin/main:...`): Left "the left zone", Right "the right zone", Bottom "the bottom zone", Center "the center". The same method feeds the shell's "Moved X to the left zone" (:38, :53-54). The default arm (`_ => id.ToString()`, :278) is not reached by any current ZoneId; the test enumerates `Enum.GetValues<ZoneId>()` and fails if a zone's label equals its enum name. Every zone has a user-facing word, so no seam request was needed; ZoneLayoutService.cs is untouched.
- Reach: `Dock` can land a pane in Left, Bottom or Center only (`TreeToZones.ClassifyStack`). Right has no floating route, so the report text is checked for those three and Right is covered by the label check alone.
- Disclosure on check 2's red: the label was already correct (node 1 used `ZoneName`), so the test passed from the start. Its red was produced by mutation (`zone.ToString()` in place of `ZoneName(zone)`, run, then reverted; not committed), which fails on `Assert.EndsWith ... "docked in the left zone."`. The red commit carries the red text, not the mutation.
- Red commit a150e0bf (tests plus the format-const extraction, wording unchanged); green commit 1a814602.
- Suite: suite-node2.txt. Core 3024 passed, 1 skipped. App 1125 passed, 1 skipped, 1 failed: `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` (known focus flake), passes alone (suite-node2-flake-alone.txt).
- The kick mail in the inbox (12:09Z) predates this node's start (12:15Z) and was not treated as this node's kick.
