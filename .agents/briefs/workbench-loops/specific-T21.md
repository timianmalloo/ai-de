# Brief — T21 `wls-float` (slice WL-S) · Claude Code · Opus · node 7 (close condition 4)

**Goal (Ruling 171 (3)):** WL-S is not accepted until **a live notebook floats and redocks**. T21 (nodes 1–6) is joined on
main (`8608d4eb`), and T11's notebook kind is now joined on main too (`d25d0311`). This node proves the live case.

**This node (n7). Each check is red then green, through real gestures on the real shell composition
(`WorkbenchShell` + the `notebook` kind + a bound `AttemptHost`, as T11's and T17d's tests build it):**
- **(A) A notebook with a turn running floats, and the run continues.** Open a notebook, send a long turn on the stub
  lane so it is Running, then float its pane (floatPane, the user's gesture). Expect: one floating window holds the
  notebook; the same `NotebookModel` instance and the same in-flight attempt keep going, with no restart, no second
  dispatch and no lost events; the turn completes inside the floating window, and its row reads as completed there.
- **(B) It redocks, and the run's state is intact.** Redock the floating notebook, mid-run and after completion (two
  cases). Expect: it returns to its zone (Center for Factory). The thread, the composer draft and the attempt row are
  the same as before the float. No "Pane hidden" announcement is made, since a float is not a hide (DOCK1 is for a
  close). Focus lands in the notebook.
- **(C) A floating live notebook survives a restart.** Save the layout with a floating notebook, then restart (the
  shell's revive path). Expect: the notebook restores floating, with its bounds (Ruling 174), and reads its attempts
  back (T17d). A turn that was running at close shows as T17's restored state. Do not invent a new state.
- **Then** update the WL-S Proof Pack (`docs/proof/workbench-loops-wl-s.md`). The float step in the human demo script
  stops being "pending: close condition 4"; it gets its exact keyboard steps and expected observations, and the
  float/redock risk row moves to the evidence. Run rule 22's ratchet before the done mail.

If the shell needs a code change outside T21's window (for example `WorkbenchShell.cs` routing the notebook's float),
send a seam request naming file:line and keep the red committed. Do not edit the file without a grant.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16) · **kick** at 15 min by mail.
**Desktop hold:** a "desktop hold occupied" result is re-run after 2 min and logged. Rules 19, 20, 21 and 22 apply.

## Owned authored paths
T21's window, as in node 6. Plus new tests under `tests/AiDe.App.Tests/Workbench/` for (A)–(C), and
`docs/proof/workbench-loops-wl-s.md` (the float step, the float risk row and the evidence rows only).

## Not in scope
The move-then-crash residual (`WorkbenchShell.cs`, needs a grant; it stays Inferred), and the "No surface “”." copy.
