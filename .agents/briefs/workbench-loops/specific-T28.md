# Brief — T28 `wl1a-compiled-field` (WL-1a fix-forward) · Claude Code · Sonnet · node 4 (the drain heading)

**Goal (Owner's direction under Ruling 181):** the compiled field's heading never states something false.
**Nodes 1–3 are joined on main** (n3 at `367f4de4`). A submit shows `Compiled prompt — sent as b{n}`. A queued send shows
`Compiled prompt — queued as b{n}`. A second press never resends, and the gesture-oracle class gate is in place.

**This node (n4). Each check is red then green, through the real gesture, with a turn in flight
(`ComposerGesture.CtrlEnterThenWait`):**
- **(A) Drain → sent.** Queue b2 behind b1, then let b1 complete. When the queue drains and b2 is sent, and the field is
  still showing b2 because the user has typed nothing since, the heading switches from `queued as b2` to
  `Compiled prompt — sent as b2`. The text, the UIA name and the ItemStatus switch with it. If the user has typed
  since, nothing changes: the live draft stays. Red: assert `sent as b2` after b1 completes, with no keystroke. Today
  it still reads `queued as b2`. Node 3's hand-off: nothing re-renders the field on the drain, and the fix is expected
  to be one `ShowCompiled` call on the drain path. Open the code first (rule 14).
- **(B) Drain refused → still queued.** b1 fails, or is stopped, so b2 is held and not sent (Ruling 95: the drain sends
  only on Completed/Answered). The heading stays `queued as b2`, because that is still true. Red: plant the switch on
  every drain outcome (mutation), and show this test fails. Then revert.
- **(C) The resend guard after the switch.** After (A)'s switch to `sent as b2`, a press with no keystroke sends
  nothing, as node 2's guard does. Prove the red with a planted leak, recorded as a mutation red.

**Tier** T2 · **fan-out** 0 · **budget** 80 tool calls · 150k · **kick** at 12 min by mail.
**Rule 22:** this node hands off for a join, so run the ratchet before the done mail (the derived-view exception applies).

## Owned authored paths
`src/AiDe.App/Workbench/Composer/ComposerSurface.cs` (the drain path's `ShowCompiled` call only), the compiled-field tests
under `tests/AiDe.App.Tests/Workbench/Composer/`, and `{PROOF}/**`. `ComposerSendGate.cs` and `SessionDocumentSurface.cs`
are read only.

## Not in scope
The prefill (T30). It follows on this file after this node joins.
