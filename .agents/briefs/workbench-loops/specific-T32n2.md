# Brief — T32 `wl-copy-pin` node 2 (the Owner's rulings on node 1's findings) · Claude Code · Sonnet · node 2

**Goal:** empty the shrink-only lists of `tests/AiDe.App.Tests/DesignCopyRowsArePinnedTests.cs` as far as the Owner's
rulings allow (recorded in `.agents/briefs/workbench-loops/obligations.md`, "Owner rulings on T32's findings",
2026-10-20). Node 1 (joined at `eb178bdb`; README `docs/proof/workbench-loops-support/copy-pin/README.md`) pinned 17 of
27 DESIGN.md id rows. It named 10 rows on `UnmatchedRows` and 23 copy members on `UnmatchedMembers`. The rulings:
- **SC7, SC8, SC9** stay on `UnmatchedRows` as accepted exclusions ("not copy"). Do not touch them.
- **CHK6** stays a partial pin. Its gap clauses are a later node's Core test. Not this node.
- **The 7 rows no code renders as a literal** (NB1, RUN1, RUN3, RUN4, CHECK1, AUTH1, DEMO1). For each row, either
  **pin the composed rendered text through the surface**, or **mark the row stale with the reason**.
- **The 23 members with no row:** for each one, **add its DESIGN.md row**. This is copy only: the row's text is what
  the code renders today (rule 14: the code on main outranks the document), and nothing in `src/` changes.

**This node, in this order (three checks, rule 16):**
- **(N1) The 23 member rows.** Red: delete every entry from `UnmatchedMembers` and run the test. It must fail naming
  all 23 (save `n2/N1-red.txt`). Green: add one DESIGN.md id row per member, in the copy table its area already uses
  (ChecklistCopy near the CHK rows, TurnCopy near the RUN/SC rows, and so on; the copy rows span `DESIGN.md:1287-1425`
  at base). Give each a new id that matches the test's `IdRow` regex (`^\| [A-Z]+[0-9]+[a-z]? \|`, the next free number
  in its series). The row's text is the member's rendered literal. For a builder, use the row-placeholder convention
  node 1's README documents ("How a row is compared"). Then pin each new row in the test the way node 1 pins rows
  (save `n2/N1-green.txt`). A member whose text cannot be a meaningful row stays on the list with a new one-line reason,
  for example a diagnostic never shown to the user. Name each such member in the done mail.
- **(N2) The 7 rows.** For each row, first search the surface for a composed rendering: the row's words built from
  pieces at render time. Grep `src/` for the row's distinctive words and open each hit. Then:
  - **Rendered by composition:** pin the rendered text through the surface in an App test that drives the real
    surface to the state, then reads the shown text (not a constant). Red by mutation: break one piece of the
    composition in a scratch edit, see the pin fail, and revert. Never commit the mutation.
  - **Not rendered:** mark the row stale in DESIGN.md (N3's convention), with the reason and the evidence. The
    evidence is the search you ran: the command and its zero hits. Remove the row from `UnmatchedRows`.
  Save `n2/N2-<row>.txt` per row with the search and the decision. A row whose state cannot be reached in a test
  without a src change is a **finding**, not a guess. Leave it on the list with that reason and name it.
- **(N3) The stale convention, red first.** A DESIGN.md copy row may carry, as the first text of its cell,
  `*(stale: <reason>)*` before its original sentence. The test treats a stale row as follows:
  - it is neither pinned nor allowed on `UnmatchedRows`;
  - **if any code renders its sentence, the test fails** ("stale row is rendered; un-stale it").
  Red: mark a pinned row stale in a scratch edit and see the test fail. Revert, and never commit the scratch edit.
  Save `n2/N3-red.txt`, then `n2/N3-green.txt`.
  Order of work: build N3's convention in the test before N2 marks any row stale.
- **Then** the whole App suite and the whole Core suite, then rule 22's ratchet, then rule 12. The done mail must carry
  the final `UnmatchedRows` and `UnmatchedMembers`, each with its reasons.

**Known App failures on this machine** (T31, N=10, joined at `7f0f358f`). These fail in every full run here:
`CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective`,
`ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation`, and
`CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`. Name them in the suite result as known. Any other failure is yours:
run it alone, record both results, and report it.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k. **Kick** at 30 min by mail.
**Rules 19, 20, 21, 22 and 23 apply.** Check the board before every App run, as its own command. A
`USER DEMO IN PROGRESS` post with no later `USER DEMO DONE` means no App run of any kind. In that case commit what is
green, mail the Leader `T32 n2 HELD by USER DEMO at <time>`, and end your turn with the same line. You are resumed on
`USER DEMO DONE`.

## Owned authored paths
`tests/AiDe.App.Tests/DesignCopyRowsArePinnedTests.cs`. One new App test file per composed row, if N2 finds any:
`tests/AiDe.App.Tests/<Row>RenderedCopyIsPinnedTests.cs`. `DESIGN.md`: the copy-row tables only (new rows, stale
marks), no other section. `docs/proof/workbench-loops-support/copy-pin/n2/**`. The DC-NEW-wl-t26-copy entry's Status
line in `docs/lessons/defect-classes.md`. **No src change.** A sentence the code should render differently is a
finding for the Owner, never a fix.
