# Brief — T21 `wls-float` (slice WL-S) · Claude Code · Opus · node 8 (close condition 4, continued)

**Goal (Ruling 171 (3)):** WL-S is not accepted until **a live notebook floats and redocks**.
**Node 7** (e83a4d7d) made (A) red through the real shell, at `0097164f`: "the model floats the notebook, but the view
shows 0 floating window(s)". Its cause, from reading: `WorkbenchController.ApplyAndAnnounce` (:732) applies and announces
but never renders, and every command route ends there. (B) and (C) are drafted in `{PROOF}/n8-draft-B-C-tests.txt`.
**Seam `req-01M5ATF6PNMEW0SX2KKZA34D4D` is GRANTED** (late; the Leader missed it during node 7). It covers one line in
`src/AiDe.App/Workbench/DockHost.cs` after :73, `controller.LayoutApplied += adapter.Render;`, plus the `LayoutApplied`
event in `WorkbenchController`, raised after `service.Apply` in `ApplyAndAnnounce`.
**The Leader merged origin/main into your tree** (`3b8c6fe3`), so claims pass again.

**This node (n8), each red then green through the real gesture:**
- **(A) green.** Add the event and its wiring. (A)'s red at `0097164f` turns green. Then **sweep the class**: close,
  collapse, maximize and next-tab run as commands probably have the same gap. Give each a test through the palette,
  red on the parent of your fix (or by reverting the wiring), then green. Show that routes which already render (drag,
  restore) do not render twice, or that a second render is harmless, by a test or by file:line. Register the class in
  `docs/lessons/defect-classes.md` as `DC-NEW-wl-t21-float-d` (a layout command that changes the model and never
  renders), in the same commit as the fix.
- **(B) redock**, from the draft: mid-run and after completion, with the state intact, no DOCK1, and focus in the
  notebook. Node 7 expects from reading that focus after a redock may be wrong. Show it.
- **(C) a floating live notebook survives a restart**, from the draft: it restores floating, with its bounds and its
  read-back. Node 7 expects from reading that the floating window's tab has no context menu, because `DecorateTabs`
  walks only `Manager` (`WorkbenchAdapter.cs:82`). If that is red, it is part of (C).
- **Then** the WL-S Proof Pack's demo step 5 with real keyboard steps (the palette's *Float pane*, then
  *Move to → Center*; `Ctrl+K, F` is unbound). Then rule 22's ratchet: this node hands off for a join.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16). Node 7 measured 258k: **hand off at 180k**.
**Kick** at 15 min by mail.
**Desktop hold:** rules 19, 20, 21 and 22 apply. A "desktop hold occupied" result is re-run after 2 min and logged.

## Owned authored paths
T21's window, plus the one granted `DockHost.cs` line, the new tests under `tests/AiDe.App.Tests/Workbench/`, the WL-S
Proof Pack's float rows and step 5, and `docs/lessons/defect-classes.md` (your DC-NEW entry only).
