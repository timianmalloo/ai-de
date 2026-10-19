# Brief — T28 `wl1a-compiled-field` (WL-1a fix-forward) · Claude Code · Opus · node 2 (the fix)

**Goal (Ruling 181, read it in `docs/notes/addendum-c-council-rulings.md`):** after a submit, the composer's compiled field
keeps showing what was sent, headed **"Compiled prompt — sent as b{n}"**, until the next keystroke in the editor. It is
display-only, and it can never be resent.
**Node 1 is accepted** (e3ec0278; the Owner checked `ComposerSendGate.cs:230/:347` and `ComposerSurface.cs:1224-1225`).
The root cause is Verified: `Admit` → `BeginNextTurn` → `NextBlock` (`RenderedView = null`) → empty draft → `_compiled.Text = ""`.
The red test is `f3bdf334`.

**Every test in this node drives the user's real gesture:** type through `SetFieldText`, press Ctrl+Enter through
`OnAcceleratorKey`, open the disclosure through its toggle peer, and read the open field on the real
`SessionDocumentSurface` composition (your node-1 reproduction).

**This node (n2), each red then green:**
- **(A) Ruling 181 (1).** After a submit the field shows the sent turn's compiled text under the heading
  `Compiled prompt — sent as b{n}` (one const; the same words in UIA, name or ItemStatus). The next editor keystroke
  returns it to the live view of the new draft. Add a DESIGN.md row amending line 879's "exactly the outgoing text" to
  name the sent-state. Your node-1 red `f3bdf334` turns green, with its oracle updated to the heading if needed. Say so
  in handoff.md.
- **(B) Ruling 181 (2), the HARD resend guard.** The shown text never reaches `ComposerSendGate.RenderedView`, because
  `:347` sends `RenderedView ?? RenderView(...)`. Red: a second Ctrl+Enter right after a submit, with no keystroke,
  sends **nothing**. Assert no new turn, no dispatch, and no new attempt row, plus the gate's own refusal ("an empty
  prompt is not a task"). Prove the red by planting the leak (route the display through `RenderedView`), and record it
  as a mutation red. Also cover a queued send (Ruling 95) and a held send (Ruling 175), so each shows its own state, not
  the sent-state.
- **(C) Ruling 181 (3), phase 2: re-drive Ruling 96's tests through the gesture.** In
  `tests/AiDe.App.Tests/Sessions/TheWriterKeepsItsRoomTests.cs`, `TheCompiledPromptDisclosure_IsOnScreen_AtEveryTurnCount`
  (:107-142) gets a typed draft and a **non-empty** assertion. `…OpenedAtTheOperatorsBelt_RendersTheCompiledText`
  (:163-220) drives the composition and the gesture. The sibling at
  `Composer/TheComposerRendersItsFieldLevelErrorsTests.cs:381` moves onto a `SessionDocumentSurface`, or is relabelled
  pre-admit, with its name saying so. Each must fail on the unfixed code first: show it against `f3bdf334`'s parent
  state, or by a mutation, and save the output.
**Next node (n3), not this one:** phase 3, the class gate. A content oracle on a user-visible field must drive the user's
gesture, and must not be set up by changing the fixture directly.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16) · **kick** at 15 min by mail.
**Desktop hold:** a "desktop hold occupied" result is re-run after 2 min and logged in `{PROOF}/desktop-hold.txt`. It is
never counted as a pass or a fail. Rules 19 and 20 apply.

## Owned authored paths
- `src/AiDe.App/Workbench/Composer/ComposerSurface.cs` (`BeginNextTurn`, `RenderCompiledView`, the compiled field's
  heading/UIA, and the new const only)
- the `DESIGN.md` row for line 879
- `tests/AiDe.App.Tests/Sessions/TheWriterKeepsItsRoomTests.cs` (the two tests above)
- `tests/AiDe.App.Tests/Composer/TheComposerRendersItsFieldLevelErrorsTests.cs` (the :381 test only)
- your node-1 test file and new tests beside it
- `{PROOF}/**`

`ComposerSendGate.cs` and `SessionDocumentSurface.cs` are **read only**. A needed edit there is a seam request.

## Not in scope
The prefill (T30, Ruling 180), which follows on this file after this node joins. The class gate (n3).
