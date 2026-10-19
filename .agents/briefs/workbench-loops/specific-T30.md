# Brief — T30 `wl-prefill` (AC-W01a, Ruling 180) · Claude Code · Opus · node 1

**Goal (Ruling 180; read it in `docs/notes/addendum-c-council-rulings.md`):** goal, done criteria and exclusions are
prefilled by the compile step's model by default, as the user asked after the WL-1a demo ("i should not be the one filling
in goal and done criteria - that should come from the LLM analysis of my prompt"). New sessions open in
`agentic-advisory` when Ruling 68's gate 1 admits it, the compile-mode control is exposed, and a kept line is kept.
**Only Ruling 68's DEFAULT is amended.** The full `agentic` rung stays behind the 50+50 floors. Condition (5), C2, is
**not** this track's: it moved to T26's D2.
**Read first:** T29's assessment `docs/notes/wl-dr-prefill-goal-done.md`, merged into your branch. It names every seam
with file:line: `SessionConfig.cs:136`, `SessionConfigStore.SetCompileMode` (:342), `ComposerSendGate.PrepareAsync`
(:148, :579-670), `ComposerSurface.cs:1537-1547` and `:2361-2368`, `CompileModeGate.cs:43-44`, `Envelope.cs:68-79`, and
the settings row at `SessionDocumentSurface.cs:1413`. Rule 14 holds: re-open each one on your branch, because T28 changed
`ComposerSurface.cs` after T29 read it.

**This node (n1), each red then green, through the user's real gesture where a UI is involved:**
- **(A) Ruling 180 (1): the default.** A new session's compile mode is `agentic-advisory` when gate 1 admits it, and
  `mechanical-only` otherwise. The settings row states which mode and why, with the gate's own reason text. Test both
  branches through a deterministic gate seam: no real `~/.aide/proof` read in a unit test; inject the gate's input.
- **(B) Ruling 180 (2): the control.** The compile mode is exposed in session settings and changeable by keyboard,
  with UIA name and role. Choosing `mechanical-only` is always allowed. Choosing `agentic-advisory` is offered only
  when gate 1 admits it, otherwise disabled with the reason. It goes through `SetCompileMode` and the gate.
- **(C) Ruling 180 (3): suggested, editable, kept.** Derived lines are visibly marked as suggested (the existing
  *? derived* mark, or the Owner's word "suggested"; read DESIGN.md first) and are editable inline. They count only
  when kept, so an unkept line never enters `projection_sha` (`Envelope.cs:68-79`). **Keep writes the line into the
  draft (C1)**, so it survives the next prepare and a restart. That fixes the lost-on-re-prepare defect T29 found at
  `ComposerSurface.cs:1543-1546`. Red through the gesture: type, prepare (stub compile returns three derived lines),
  Keep one, prepare again or restart, and the kept line is still there.
**Node 2 (not this one):** Ruling 180 (4), the cost line per advisory call and the first 50 calls' cost and latency
recorded; (6) `tools/compile-eval` run in the track, with the AI-systems lens's conditions carried; the Proof Pack.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16) · **kick** at 15 min by mail.
**Desktop hold:** rules 19, 20, 21 and 22 apply. A "desktop hold occupied" result is re-run after 2 min and logged.
**No real model call in any test.** The compile host is stubbed. A real advisory call spends the user's subscription.

## Owned authored paths
- `src/AiDe.Core/Sessions/SessionConfig.cs` (the default only) and `SessionConfigStore.cs` (if the default needs it)
- `src/AiDe.App/Workbench/Composer/ComposerSurface.cs` (the derived-line Keep and mark regions; T28's sent and queued
  regions are read only)
- `src/AiDe.App/Workbench/Composer/ComposerSendGate.cs` (the Prepare path for kept lines only)
- the session-settings row in `SessionDocumentSurface.cs` (:1413's region only)
- new tests under `tests/AiDe.App.Tests/Workbench/Composer/` and `tests/AiDe.Core.Tests/Sessions/`
- the DESIGN.md rows for the mode copy and the suggested mark
- `{PROOF}/**`

Any other file needs a seam request.

## Not in scope
Condition (5) (T26). Raising the `agentic` rung. Any change to the compile prompt or the eval floors.
