# Brief — T30 `wl-prefill` (AC-W01a, Ruling 180) · Claude Code · Opus · node 2

**Goal (Ruling 180):** goal, done criteria and exclusions are prefilled by the model by default, visibly suggested, and
kept when kept. This node makes it reach the user.
**Node 1 is accepted as a branch node** (bd8ef0d7): the advisory default with an injected gate, the Compile mode control,
and Keep writing into the draft across a re-prepare. **It is not user-visible yet:** File > New Session still opens
mechanical-only.

**This node (n2), in this order:**
- **(0) Measure C1's restart half before anything is decided (rule 14).** Keep a suggested line, prepare (stub), then
  restart through the shell's revive path, as T17d's tests reopen a notebook. Read the composer draft. Node 1 found
  `ComposerDraftStore` has no caller in `src/AiDe.App`, but a notebook revision saved at prepare may carry the draft.
  Record in `{PROOF}/c1-restart-measure.txt` whether the kept line survives, and through which path (file:line). If it
  does not survive, **stop that item**, say so in handoff.md with the evidence, and the Leader brings the Owner a DR on
  draft persistence. Do not build persistence.
- **(A) Wire the default into the real app**, red first through the real menu: **File > New Session** opens
  `agentic-advisory` when gate 1 admits, and the real session settings evaluate the gate over the bound adapter. Use
  the two granted lines from seam `req-01M5ATGTTQS78NG5W7ZMZE8G00`: the `compileModes` argument at
  `NewSessionSheetViewModel.cs:556`, and a read-only `ComposerSurface.AdapterInstallRoot` getter. The sheet's gate
  input is injected so the red is deterministic. No real `~/.aide/proof` read happens in a test.
- **(B) The visible mark reads "suggested" (Owner, under Ruling 180 (3)).** "suggested" replaces "? derived" for
  model-derived lines, as one const; UIA uses the same word; DESIGN.md is amended at the row that defines "? derived".
  Keep "? derived" only if DESIGN.md uses it for a different, non-model derivation, and cite that row. Red: a
  model-derived line shows "suggested", and a kept line no longer shows it.
- **(C) DC-NEW-wl-t30-prefill, the class control (Owner).** Add a fixture that scopes window queries to the test's own
  window, so a test never reads another test thread's window. Red: one test that fails under the whole-suite interleave
  (or a forced concurrent window), then green through the fixture. Sweep the App tests that query windows globally;
  name them in handoff.md and move each one you can onto the fixture.
**Node 3 (not this one):** Ruling 180 (4), the cost line and the first 50 calls; (6) compile-eval; the Proof Pack; rule 22.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k. Node 1 measured 275k, the fourth node over cap this
epoch: **hand off at 150k**, measured from your own usage. **Kick** at 12 min by mail.
**Rules 19, 20, 21 apply.** No real model call in any test.

## Owned authored paths
As in node 1, plus the two granted lines, the new fixture and the tests moved onto it under
`tests/AiDe.App.Tests/`, the DESIGN.md "? derived" row, and `docs/lessons/defect-classes.md` (your DC-NEW entry only).
`ComposerGesture.cs` (T28's helper) may be extended for the binder's goal-block form; node 1's finding is that it
leaves the draft free-form. Say what you changed.
