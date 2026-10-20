# Brief — T26 `wl1b-checklist-kind` (slice WL-1b) · Claude Code · Opus · node 3

**Goal (design row T26):** the minimal `checklist` kind, WL-1b's only user-facing part.
**Node 2 is accepted as a branch node** (b82eba50): the three literal entries, the refusing test (mutation M4), D3
(M7, M5) and the one-live-sha ceiling (M8) are green. Your `ChecklistSurface.cs` §2 owner row is **on main**
(9ec42853, the Leader, seam `req-01M5BD7Y0K33J3FYQ2HEZ1S1W4`); the Leader merged origin/main into your branch before
this dispatch, so the ratchet's verify-surface-ownership refusal must now be gone.
This node closes the two conditions the Owner set **before the T26 join** (Owner, 2026-10-20 ~01:10Z, not a ruling).

**This node (n3), in this order:**
- **(F) The Owner's copy confirmation.** CHK1, CHK2, CHK4, CHK5, CHK6, CHK7, CHK8, CHK10, CHK11 and CHK12 are
  **confirmed as written** (the Owner read DESIGN.md:1410-1421 at b82eba50). Two change, exactly:
  - **CHK3** → `Satisfied by run {7-char key} (intent revision {n}).` (one term, "intent revision", everywhere).
  - **CHK9** → `This notebook has no work journey. It was made before work journeys existed. Obligations need a work journey.`
    ("(pre-workbench)" is internal jargon the user has never seen.)
  Red first: no test pins either sentence today (every test reads `ChecklistCopy.Satisfied` / `ChecklistCopy.NotAWorkJourney`,
  so a copy change can never go red). Add a test that asserts each **literal** sentence as the user sees it on the
  surface (CHK3 through the link gesture; CHK9 through a notebook with no work journey), red on the old copy. Then
  change `ChecklistCopy.cs` (:33, :70-72), the two DESIGN.md rows (CHK3, CHK9) and the design doc line
  `docs/design/workbench-loops-wl-1b.md:426` **in one commit**; the reds turn green. Record the Owner's confirmation,
  with the two changes and the date, in `docs/proof/workbench-loops-wl-1b/checklist/README.md`.
- **(G) Settle M6** (the checklist `Invalidate` in `WorkbenchShell.cs:3716-3720` removed, no red; `d3-not-red-invalidate-mutation.txt`).
  An unkilled mutation on a user-visible refresh is DC-NEW-wl-t28-compiled's shape. Do **one**:
  - **(a)** a test that drives the user's gesture that call serves: the checklist is **already visible** saying its
    notebook is not open (CHK10), with **no visibility edge** after that, and the user reopens the notebook through the
    real route (File → Recent sessions, or the shell's own reopen command, never a fixture call). Assert the visible
    checklist leaves CHK10 and shows that journey's obligations. **Red under M6**, green with the call restored. If an
    obligation change is the shorter path to a red under M6 (the Owner's wording: change an obligation, then assert the
    visible checklist shows the change), that also satisfies (a); say which you used and why.
  - **(b)** if no gesture can make M6 red: remove the call, and name by file:line the path that already refreshes the
    view in that case, with the (a)-shaped test green without the call. Save its output as the proof.
- **Then** run the whole Core and App suites, then rule 22's ratchet (this node hands off for the T26 join). Name, in
  the done mail, the head sha each suite and the ratchet ran on.

**Tier** T2 · **fan-out** 0 · **budget** 80 tool calls · 120k. **Hand off at 120k** by your own measured usage.
**Kick** at 15 min by mail. Stop at the kick, even mid-check: commit what is green, then run the suite.
**Rules 19, 20, 21 and 22 apply.** Check the board before every App run, as its own command. A `USER DEMO IN PROGRESS`
post with no later `USER DEMO DONE` means **no App run of any kind**: commit what is green and wait or hand off.

## Owned authored paths
As in nodes 1 and 2, plus (granted for this node): `DESIGN.md` rows CHK3 and CHK9 only; `docs/design/workbench-loops-wl-1b.md:426`
only; `src/AiDe.App/Workbench/WorkbenchShell.cs:3714-3720` only (the M6 call and its comment, option (b) only).
