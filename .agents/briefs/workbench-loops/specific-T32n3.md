# Brief — T32 `wl-copy-pin` node 3 (the three stale labels) · Claude Code · Sonnet · node 3

**Goal:** the 7 DESIGN.md copy rows that node 2 marked `*(stale: …)*` say the true thing. The Owner (2026-10-20,
after the n2 join) ruled the single label "stale" wrong: it merges three different cases, and the rows cite the
Owner, so each must name its real state. Node 2 is joined; read its proof in
`docs/proof/workbench-loops-support/copy-pin/n2/` and the stale handling in
`tests/AiDe.App.Tests/DesignCopyRowsArePinnedTests.cs` before changing anything.

**The labels (Owner, exact):**
1. **SUPERSEDED** — the sentence was replaced; the label names the replacement.
   - RUN4 → `TurnCopy`'s DeliveryUnknown `ReasonSentence` (open it, cite `file:line`).
   - CHECK1 → CHK2 (`ChecklistCopy.Acknowledged`).
   - DEMO1 → mockup-only text: "prototype only, never product copy".
2. **NOT BUILT** — a spec or slice owes it; the label names the owner. Such a row returns to a pin when that slice lands,
   and is never left excluded.
   - NB1 "Draft saved on this device." → WL-2's draft store (ADR-0043).
   - RUN1 "No reasoning summary supplied." → WL-4 (spec `docs/specs/workbench-loops-ux.md:686`, slice row
     `docs/architecture.md:1509`; open both and confirm the lines say so before citing them).
3. **UNOWNED** — design copy with no spec story: "design-only, no owning story", kept for review by the next slice
   that touches runs or route auth. Never retired.
   - RUN3 and AUTH1 (0 hits outside DESIGN.md in `docs/`; re-run that search and save it).

Each row's marker drops "Owner ruling 2026-10-20" as the reason. It states its label and its named replacement or
owner. Only the date of the ruling stays, as `(Owner, 2026-10-20)`.

**This node, in this order (two checks, rule 16):**
- **(L1) The test learns the three labels, red first.** Replace the single `stale` marker with three:
  `*(superseded by <replacement>: …)*`, `*(not built, owed by <slice>: …)*` and `*(design-only, no owning story: …)*`.
  Choose the exact marker syntax, document it in the test's summary comment, and keep it parseable. The test holds:
  - (a) A row with any of the three labels is neither pinned nor on `UnmatchedRows`, as `stale` rows are today.
  - (b) If code renders the sentence of a **not built** or **design-only** row, the test fails, naming the label and
    the owner: "<id>: now rendered; the owning slice <slice> landed it, move the row back to a pin". A join that adds
    the constant is then told what to do.
  - (c) A **superseded** row whose named replacement no longer exists fails. The replacement is either a pinned row id
    or a `*Copy` member, checked by the same reflection the test already uses.
  - (d) The plain `stale` marker is no longer accepted. A row carrying it fails with "use one of the three labels".
  Red: run the new test against the DESIGN.md as it stands (the 7 rows still say `stale`) and see (d) fail naming all 7
  (`n3/L1-red.txt`). Also show (b) red with a scratch edit that adds NB1's sentence to a `*Copy` constant, and (c) red
  with a scratch rename of RUN4's replacement. Revert both scratch edits and never commit either. Save
  `n3/L1-red-b.txt` and `n3/L1-red-c.txt`.
- **(L2) DESIGN.md: the 7 rows relabelled** exactly as above. Then the test is green (`n3/L2-green.txt`).
- **Then** the whole App and Core suites, rule 22's ratchet, and rule 12. Update the DC-NEW-wl-t26-copy Status line to
  name the three labels.

**Known App failures on this machine** (T31, N=10): `CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective`,
`ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation` and
`CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`. Name them as known. Any other failure is yours: run it alone and
report both results.

**Tier** T2 · **fan-out** 0 · **budget** 80 tool calls · 120k. **Kick** at 30 min by mail.
**Rules 19, 20, 21, 22 and 23 apply.** Check the board before every App run, as its own command. A
`USER DEMO IN PROGRESS` post with no later `USER DEMO DONE` means no App run. Commit what is green, mail the Leader
`T32 n3 HELD by USER DEMO at <time>`, and end your turn with the same line.

## Owned authored paths
`tests/AiDe.App.Tests/DesignCopyRowsArePinnedTests.cs`; `DESIGN.md` (the markers of rows NB1, RUN1, RUN3, RUN4,
CHECK1, AUTH1, DEMO1 only); `docs/proof/workbench-loops-support/copy-pin/n3/**`; the DC-NEW-wl-t26-copy Status line in
`docs/lessons/defect-classes.md`. **No src change.**
