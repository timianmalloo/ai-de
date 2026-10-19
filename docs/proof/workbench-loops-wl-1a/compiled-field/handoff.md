---
id: proof-workbench-loops-wl-1a-compiled-field-handoff
title: "Hand-off — WL-1a T28: the compiled field after a submit"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, evidence, handoff, composer, ruling-96]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  T28 after node 1 (/investigate): the bug is reproduced on the product composition with a real Ctrl+Enter, and the root
  cause is verified (the submit's BeginNextTurn empties the live-draft view). The red test is committed. The fix is not
  written; it waits for the Owner's ruling on what the field shows after a submit.
---

# Hand-off — T28 `wl1a-compiled-field`, after node 1

**Done (node 1, `/investigate`):**
- Reproduced with the product composition and a real Ctrl+Enter: `_compiled.Text.Length` 69 → 0, height 48, open (`reproduction.txt`).
- Root cause **Verified**: `SessionDocumentSurface.cs:672` → `ComposerSurface.cs:1138` / `:1149` / `:1225` (README §1).
- Defect class `DC-NEW-wl-t28-compiled` appended to `docs/lessons/defect-classes.md`.
- Red test: `tests/AiDe.App.Tests/Workbench/Composer/TheCompiledFieldAfterASubmitTests.cs` (namespace `AiDe.App.Tests.Composer`).
  `OpenedAfterACtrlEnterSubmit_TheCompiledFieldShowsTheSentPrompt` and
  `OpenWhileTyping_ThenCtrlEnter_TheCompiledFieldKeepsTheSentPrompt`, both red at `f3bdf334` (`compiled-after-submit-red.txt`).
- Whole suites at the node head: `suite-node1-core.txt`, `suite-node1-app.txt` (exit codes in `suite-node1-exits.txt`).
  The two red facts are the expected App failures.
- **Finding (coordination, not this node's class):** the session scratchpad is shared across the Leader's sub-agents. A
  first suite run wrote `core.txt` / `app.txt` there, and T21's run in `C:\Projects\ai-de-wls-float` overwrote both files
  with its own results (its build paths are in them). That run was discarded. The suites were re-run with output written
  into this tree's proof folder. Briefs could require per-track scratch file names.

**Red, by design:** the two facts above. Nothing else this node added is red.

**Open:**
- Seam `req-01M5AE5QGZMKFC36RA4C93QSPJ` (rule 14): the brief's oracle `Text == RenderView(draft).Text, non-empty` cannot
  hold after a submit, because the code's contract is the live, now-empty draft. The red test asserts option A instead:
  the field shows the sent prompt.
- **Owner decision (phase 0 of the README's plan):** A, the field keeps the sent prompt ("sent as b1") until the next
  keystroke (recommended); or B, an empty state that points to *Compiled prompt of b1*.

**Next step (node 2, after the ruling):** phase 1 of the README's plan. Hold the admitted turn's bytes as a display-only
view. They must never reach `ComposerSendGate.RenderedView` (`ComposerSendGate.cs:347`), or a second Ctrl+Enter would
resend b1. Turn both facts green, and add the keystroke-replaces-it and empty-resend-refused tests. Then phase 2 (the two
Ruling 96 tests and `TheComposerRendersItsFieldLevelErrorsTests:381`) and phase 3 (the class gate).
