# Brief — T30 `wl-prefill` (AC-W01a, Rulings 180 and 182) · Claude Code · Opus · node 3

**Goal (Rulings 180 and 182):** the prefill is the default, visibly suggested, kept across a prepare, and it never claims
more than it does.
**Node 2 is accepted as a branch node** (07103c7c). File > New Session and the settings use the gate. The mark reads
`? suggested`, and DESIGN.md defines `?` as the glyph. The OwnWindow fixture is in. Item (0) measured that a kept line
does **not** survive a restart, and **Ruling 182** (on main) amends Ruling 180 (3): survival across the next prepare is
required now, and restart survival is WL-2's. **Build no persistence.**

**This node (n3), each red then green, through the real gesture:**
- **(A) Ruling 182 (2): nothing implies the draft is saved.** No composer text, badge or UIA name may say an unsent draft
  or a kept line is saved: not "saved", "draft saved", "kept for later", or similar. Red: a test that refuses
  saved-state copy on the composer, its suggested and kept marks, and the settings row. Make it red by planting such a
  string as a mutation, if no real one exists, and record it as a mutation red. Then green on the tree.
- **(B) Ruling 182 (4): what was sent survives.** Send a turn whose compiled prompt includes a goal line (kept or typed),
  then restart through the shell's revive path, as T17d's tests reopen a notebook. The restored turn row's
  compiled-prompt disclosure shows that goal line (Rulings 163 and 181). Red first. If it already holds, the red is a
  mutation that drops the goal block from the restored row, recorded as one.
- **(C) The settings row states the gate's real reason.** Node 2 found that `CompileModeWhy` says "Gate 1 admits it" for
  any session stored as `agentic-advisory`, even when the gate now refuses that mode. Red: a session stored advisory,
  with the gate refusing, must state the gate's refusal reason, and must not claim admission. Then green.
- **Then:** the prefill Proof Pack gets Ruling 182 (3)'s residual line, the same words as the WL-1a Proof Pack's
  (`aa8247ff`): unsent typing and kept suggestions are lost on restart until WL-2.

**Node 4 (not this one):**
- Ruling 180 (4), the cost line and the first 50 calls.
- Ruling 180 (6), compile-eval.
- The four sheet tests that read the real `~/.aide/proof`; the Leader grants the seam to inject the gate in them at node
  4's dispatch.
- Rule 22's ratchet, ahead of T30's join.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k. Node 2 measured 255k, so **hand off at 150k** by your
own measured usage. **Kick** at 12 min by mail.
**Rules 19, 20, 21 apply.** A join may be in flight: check the board before every App run. No real model call in any test.

## Owned authored paths
As in node 2. The restored-row disclosure is read only (T28's and T17's code); if (B) needs an edit there, send a seam
request.
