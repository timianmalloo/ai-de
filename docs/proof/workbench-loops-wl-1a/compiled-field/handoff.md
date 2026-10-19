---
id: proof-workbench-loops-wl-1a-compiled-field-handoff
title: "Hand-off — WL-1a T28: the compiled field after a submit"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, evidence, handoff, composer, ruling-96, ruling-181]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  T28 after node 2: Ruling 181 (1)-(3) landed red then green through the user's gesture. The field keeps the sent turn,
  headed 'Compiled prompt — sent as b{n}', display-only, until the next keystroke; a second Ctrl+Enter sends nothing
  (planted-leak mutation red); Ruling 96's tests are re-driven. Next is n3, the class gate.
---

# Hand-off — T28 `wl1a-compiled-field`, after node 2

**Done (node 2, `/implement`, Ruling 181).** The commits are `eaf8966c` (red tests and the heading consts), `1edfc015` (the red evidence) and `5da5d032` (the fix, then green).
- **(A)** The node-1 red tests (from `f3bdf334`) are **green**: `OpenedAfterACtrlEnterSubmit_TheCompiledFieldShowsTheSentPrompt` and `OpenWhileTyping_ThenCtrlEnter_TheCompiledFieldKeepsTheSentPrompt`. Their oracle is unchanged: the field equals the sent prompt. They now also assert the heading `ComposerSurface.SentHeading("b1")` on the header, the field's UIA name and the disclosure's ItemStatus. New tests: `AfterASubmit_TheNextKeystroke_ReturnsTheFieldToTheLiveViewOfTheNewDraft` and `TheSentHeading_IsTheCopyDesignMdNames`. DESIGN.md:879 is amended, and the sent-state row is added after it.
- **(B)** `TheSentPromptIsNeverResentTests` has 3 facts. They were red by the planted-leak mutation M1 (`B-red.txt`). Under M1 a second press resent b1 as b2, with 2 attempt rows.
- **(C)** The two Ruling 96 tests and the `:381` sibling are re-driven through the gesture on a `SessionDocumentSurface`. The sibling is renamed `TheCompiledViewOnScreenAfterASubmit_IsTheWholeSentPromptAndNotASummary`. The red is on the unfixed code (`C-red.txt`). The `IsOnScreen` test's red is mutation M2 (`C-red-mutation.txt`), which the original test survives.
- The shared gesture helper is `tests/AiDe.App.Tests/Workbench/Composer/ComposerGesture.cs`.
- The whole suites are in `suite-node2.txt` (Core, then App), with the per-suite runs in `suite-node2-core.txt` and `suite-node2-app.txt`.

**Red, by design:** none of this node's tests.

**Open:**
- **Inferred design choice, not ruled:** a queued send shows the live, empty draft under the plain heading, with the queued sentence on the status line. It does not show "queued as b2" (README §5). If the Owner wants a queued heading, add one const.
- **Seam `req-01M5AE5QGZMKFC36RA4C93QSPJ`** (node 1) is answered by Ruling 181. The Leader can close it.

**Next step (node 3, the class gate):** a content oracle on a user-visible composer field must drive the user's gesture, using `ComposerGesture` or the same verbs, and must not be set up by changing the fixture directly. The gate must be seen failing on the original Ruling 96 test (`2a89cf0b:tests/AiDe.App.Tests/Sessions/TheWriterKeepsItsRoomTests.cs:107-142`) before it is green on the re-driven ones. After that, T30 (Ruling 180, the prefill) follows on `ComposerSurface.cs`.
