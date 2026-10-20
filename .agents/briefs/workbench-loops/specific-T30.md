# Brief — T30 `wl-prefill` (AC-W01a, Rulings 180 and 182) · Claude Code · Opus · node 5

**Goal:** the user SEES the prefill. Ruling 180 (3) says derived lines are "visibly marked as suggested… editable
inline"; AC-W01a says "visible before submission".
**Node 4** (5f26781b, on the branch) found that on main the suggested lines are filled into a collapsed fold that no
prepare opens (`ComposerSurface.cs:1648-1652`, `:831-837`). The user sees "prepared — press again to send" with no
suggestions, and the second Ctrl+Enter sends without them. **This is first.**

**This node (n5), each red then green, through the real gesture:**
- **(A) A prepare that yields suggested lines shows them. This is the Owner's exact shape, under Ruling 180 (3).** The
  Owner checked origin/main: the Expander is `IsExpanded=false` (`ComposerSurface.cs:1648-1652`), and the `ShowDerived`
  loop (:830-835) never opens it. Type, then press Ctrl+Enter once (advisory default, stubbed compile returning at least
  one derived line). Then:
  - the "Goal · Done when · Not in scope" fold **opens**;
  - the focus **stays in the editor**, so the next keystroke never lands in a suggestion by surprise;
  - the live region announces the count **once**, e.g. "N suggested lines. Review them before you send." Read the exact
    string from DESIGN.md, or add a row there;
  - each suggested line is reachable by **Tab from the editor** and carries `? suggested` as its UIA name prefix.
  An unkept line still does not count at the second press (Ruling 180 as written). Re-collapsing the fold is the user's
  act, never the app's. Red: today they sit in the closed fold. Register it as a **DC-NEW-wl-t28-compiled recurrence**,
  a gate that passed without driving the user's view, in the same commit as the fix.
- **(B) The first-50 reader** (seam `req-01M5B9DJWPJBEBCTP2GFJCZVRR`, granted): `tools/compile-eval/first-calls.py`
  as designed in your node-4 hand-off. Its `--self-test` output goes red then green, saved. This meets C4.
- **(C) The four sheet tests that read the real `~/.aide/proof`** (`TheNewSessionSheetTests`,
  `TheRunBindingComesFromTheProviderFileTests`, `TheSheetListsAccountsWithDerivedStatesTests`,
  `TheConfigureDialogIsTheFirstUseSurfaceTests`): inject the gate through `CompileModeGateInput`. **Seam granted here:**
  the gate-injection lines in those four test files only. Red: a guard test that fails while any test constructs the
  sheet with an adapter root and no injected gate. Then green.
- **Then:** the prefill demo script's step for the suggestions (node 4's section) now shows them, so update its
  "See:" line. Run rule 22's ratchet: this node hands off for T30's second join.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k. Every Opus node this epoch overran, and node 4
measured 376k, so **hand off at 120k** by your own measured usage. **Kick** at 15 min by mail. Stop at the kick, even
mid-check: commit what is green, then run the suite.
**Rules 19, 20, 21 and 22 apply.** Check the board before every App run, as its own command. No real model call in any
test.

## Owned authored paths
As in node 4, plus the four sheet test files (the gate-injection lines only) and the new guard test. A change to the
fold's shared component outside `ComposerSurface.cs` is a seam request.
