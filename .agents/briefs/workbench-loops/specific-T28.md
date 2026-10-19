# Brief — T28 `wl1a-compiled-field` (WL-1a fix-forward) · Claude Code · Opus · node 3

**Goal (Ruling 181; Owner's direction under it, after the T28 n2 join):** every submit shows its prompt in the compiled
field, and nothing shown there can be sent again. The defect class behind the recurrence is closed by a gate.
**Nodes 1 and 2 are joined on main** (`join-wl1a-compiled-field`). After a submit, the field shows the sent turn under
`Compiled prompt — sent as b{n}`. `ShowCompiled` is its only writer. The resend guard is proven by a planted-leak mutation.

**This node (n3), each red then green, through the user's real gesture:**
- **(A) A queued send shows its prompt (Owner, under Ruling 181 (1)).** A queued send is a submit. Today, after Ctrl+Enter
  with a turn in flight, the field shows the live empty draft under the plain heading. That reproduces the user's
  complaint ("I submitted a prompt but the compiled prompt did not show in the compiled field"). After a queued send the
  field shows **that turn's** compiled text, headed **`Compiled prompt — queued as b{n}`**: one const, the same words in
  UIA (name and ItemStatus), and a DESIGN.md row beside the sent-state row. The next editor keystroke returns it to the
  live view. Red through the gesture: a turn in flight, type, Ctrl+Enter (queued), open the field. Read `_queued` and
  `BeginNextTurn` on main first (rule 14). Node 2 deliberately excluded the queued case (`_queued is null`).
- **(B) The hard resend guard holds for the queued state.** A second Ctrl+Enter right after a queued send, with no
  keystroke, must not queue the turn again or send it: no second queued row, no dispatch, no attempt row. The gate's own
  refusal is shown. The display stays display-only and never feeds `RenderedView`. Prove the red with a planted leak, as
  in node 2, and record it as a mutation red.
- **(C) The class gate (Ruling 181 (3), DC-NEW-wl-t28-compiled).** A content oracle on a user-visible composer field must
  drive the user's gesture, not set its state by changing the fixture directly. Build it as a test or a `tools/` gate
  that fails when a test asserts composer field content (`_compiled.Text`, `CompiledView`, a UIA name of the compiled
  field) without going through `ComposerGesture` (node 2's helper) on a `SessionDocumentSurface`, unless an allow-list
  entry names it as pre-admit with a reason. Show it **failing** on a planted bare-composer content assertion, then
  green on the tree. Update DC-NEW-wl-t28-compiled's status and control line in `docs/lessons/defect-classes.md`.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16). Node 2 measured 247k: **hand off at 170k**.
**Kick** at 15 min by mail.
**Desktop hold:** a "desktop hold occupied" result is re-run after 2 min and logged. Rules 19, 20 and 21 apply.

## Owned authored paths
As in node 2, plus the new gate (a test under `tests/AiDe.App.Tests/Workbench/Composer/`, or `tools/verify-composer-gesture-oracles.py`;
name it in handoff.md, and if it is a `tools/` gate, wire it the way the existing `tools/verify-*.py` gates are found),
`docs/lessons/defect-classes.md` (your DC-NEW entry only), and the DESIGN.md row for the queued state.

## Not in scope
The prefill (T30, Ruling 180). It is dispatched on this file after this node joins.
