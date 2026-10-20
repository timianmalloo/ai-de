# Brief — T26 `wl1b-checklist-kind` (slice WL-1b) · Claude Code · Opus · node 2

**Goal (design row T26):** the minimal `checklist` kind, WL-1b's only user-facing part.
**Node 1 is accepted as a branch node** (f4285f32): D1 and D2 are green, D2's reds are three mutation reds (accept
while blocked; R1; the Owner's null-sha), and the surface refreshes on visibility. **Leader reading accepted:** the
surface id is `checklist:<sessionId>`. The service must always be called with the journey resolved from that session's
notebook (`owner_journey_id`), never a journey derived in the App.

**This node (n2), in this order:**
- **(0) The three literal entries** (seam `req-01M5BAKZ9BAA28CF4R07PTRKGV`, granted). In `PerspectiveMenuTests.cs`: the
  expected-kinds row with Kinds.Count 21 → 22 (:238), and "New checklist" after "New code viewer" in the Factory
  (:315) and Understand (:345) View literals. In `MainMenuTests.cs:167`: "New checklist" after "New code viewer".
  Keep them literal; the three node-1 reds turn green.
- **(D3) Saved review state is not acceptance**, red then green through the real gesture. Record `deferred` and
  `needs-decision`, save the layout, then restart through the shell's revive. The checklist restores with the same
  states, `work` has no `accepted` row, and the gap line is unchanged.
  **Plus the Leader's condition from seam `req-01M5B8W7NJRW7A1X4G21J2QKPT`:** restore with a DIFFERENT notebook focused.
  The checklist must still show its OWN journey, and if its notebook is not open, it says so and names the journey,
  binding nothing else.
- **(E) The one-live-sha `simplify:`** (node 1). The surface passes the newest current-revision run's envelope sha for
  every link. Write a test that pins the ceiling: an obligation with two linked runs at the head revision. Either it
  reads correctly, or the ceiling is stated in the README. Do not widen the Core request.
- **Then** run rule 22's ratchet, because this node hands off for the WL-1b checklist join. Also record that the
  checklist copy (DESIGN.md CHK1–CHK12) awaits the Owner's confirmation (design :592).

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k. Node 1 measured 321k, so **hand off at 120k** by
your own measured usage. **Kick** at 15 min by mail. Stop at the kick, even mid-check: commit what is green, then run
the suite.
**Rules 19, 20, 21 and 22 apply.** Check the board before every App run, as its own command.

## Owned authored paths
As in node 1, plus the three granted literal entries.
