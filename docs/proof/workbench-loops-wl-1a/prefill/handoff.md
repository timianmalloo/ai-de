---
id: handoff-workbench-loops-wl-1a-prefill-n1
title: "Hand-off: WL-1a T30 node 2 (Ruling 180 (1)-(3) reach the user)"
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
  T30 n2: C1's restart half measured (the kept line does not survive; DR on draft persistence owed); File > New
  Session and the real settings evaluate the gate; the mark reads "suggested"; the OwnWindow fixture. Open: the DR,
  node 3's cost line, first-50 record, eval run and Proof Pack.
---

# Hand-off — T30 node 2

**Done:** (A), (B), (C), each red then green; see `README.md` § Node 2 for shas and proof files. Whole suites:
`suite-node2.txt` (Core 3119/0/1; App 1230/3/2 — one passed on re-run, two are the foreground/desktop-hold pair).

## (0) C1's restart half — NOT met; item stopped

The kept Goal does **not** survive a restart through `WorkbenchShell.ReviveRestoredSessionDocuments`
(`c1-restart-measure.txt`). Measured: before restart the draft goal is the kept line; the stored head revision's body is
the editor's words only; after the revive the draft goal is `""`, and the editor's words are gone too. Paths:
`ComposerSurface.cs:813` saves `_draft.SourceText`, which is the editor text only in the goal-block form
(`ComposerDraft.cs:109`); the App's `NotebookRevision` holds no body (`NotebookModel.cs:30`); the revived composer starts
from `new ComposerDraft()` (`ComposerSurface.cs:96`); `ComposerDraftStore` has no App caller. **No persistence was
built.** The Leader brings the Owner a DR on draft persistence. The test pinning the measured fact turns red on purpose
when persistence lands; its last two asserts then invert.

## Findings (not acted on)

1. **Existing sheet tests now read `~/.aide/proof` incidentally.** The sheet's default gate input is the real
   `CompileModeGate.Evaluate` (the product needs it). Tests that call `Create` on a sheet with an adapter root and do not
   inject `CompileModeGateInput` read the real artifact first (`CompileModeGate.cs:107`): `TheNewSessionSheetTests`,
   `TheRunBindingComesFromTheProviderFileTests`, `TheSheetListsAccountsWithDerivedStatesTests`,
   `TheConfigureDialogIsTheFirstUseSurfaceTests` (by grep; not each traced). Their result is still deterministic here
   (the fixture's adapter sha never equals the pin, so Gate 1 refuses), but the rule's "no real read" is broken for
   them. Fix: a seam to inject the gate input in those files.
2. **The settings line can say "Gate 1 admits it" while the rung is disabled.** `SessionDocumentSurface.CompileModeWhy`
   returns that sentence for any session whose stored mode is `agentic-advisory`, without reading the ladder. Seen in
   `A-settings-red.txt`: the line said it while advisory was offered as *not available*. A demotion after creation would
   show it to the user (C3). Node 1's code; one red-then-green for node 3.
3. **"? derived" kept nowhere in the code.** DESIGN.md's only other *derived* word near it is the tier decoration's
   *T1 derived* (DESIGN.md:894), a mechanical derivation, not this mark; it is unchanged.
4. **The composer's lexical guard** (`ThePrivacyResidualsAreCoveredTests.NoAssistPathExistsAnywhereInTheComposer`)
   refuses `Suggest` in any composer source, so the const is `ModelLineMark`, not `SuggestedMark` as the seam grant
   named it (`suite-core-node2-lexical-red.txt`, fixed at `f2b382fa`).

## (C) sweep

`grep -rn "CurrentSources|Application.Current|EnumWindows|FindWindow|AutomationElement.RootElement|GetForegroundWindow"
tests/AiDe.App.Tests`: `TheCompileModeControlTests.OpenSettings` (moved onto `OwnWindow.Popups`);
`CtrlDigitSwitchesThePerspectiveFromInsideAPageTests` (`GetForegroundWindow`, compared with its own hwnd — already scoped;
not moved). Nothing else. The class control is partial: no scan refuses a new unscoped read (next step).

## Next step (in order)

1. The Owner's DR on draft persistence (item (0)).
2. Node 3: Ruling 180 (4) cost line and the first 50 advisory calls; (6) `tools/compile-eval`; the Proof Pack; rule 22.
3. Findings 1 and 2 above.

Ratchet (rule 22) not run: the next step is node 3, not a join.
