# Brief — T32 `wl-copy-pin` (DC-NEW-wl-t26-copy's class control) · Claude Code · Sonnet · node 1

**Goal:** a copy change can never pass unseen. The Owner (2026-10-20, ~01:12Z; F1 ~01:43Z) ordered this control: a
class with no control is a memoir (CI6). **DC-NEW-wl-t26-copy** (`docs/lessons/defect-classes.md`): a copy oracle that
reads the same constant the code renders can never go red on a copy change. 8 App test files assert through a `*Copy`
constant today; **do not rewrite them** (the Owner: the new test is the control; rewriting is out of scope).

**This node, in this order:**
- **(P1) The copy-pin test** — ONE App test file, `tests/AiDe.App.Tests/DesignCopyRowsArePinnedTests.cs`:
  - It reads `DESIGN.md` and finds every copy row that has an id (`| CHK3 | … |`, `RUN4a`, `DOCK1`, `SC…`, `NB…`, `AUTH…`,
    `DEMO…`, `CHECK…`: 29 id rows in the T26 join's merged tree, Leader count; recount at your base and state yours, `^\| [A-Z]+[0-9]+[a-z]? \|`).
  - It matches each row to the source constant (or `static string` builder) that renders it, and asserts the literal
    is equal. A row with placeholders (`{n}`, `{7-char key}`, `{journey}`, `{HH:mm:ss}`) is compared against the
    builder's output for fixed sample arguments, or by a placeholder-to-pattern match; say which and why.
  - A row with **no** constant, or a `*Copy` constant with **no** row, is a **named finding**, never a skip: the test
    fails naming it, unless it is on an explicit list in the test with a one-line reason each. The list may only
    shrink: a stale entry (now matched, or the row gone) fails. Rows that are layout or behaviour descriptions, not
    copy (e.g. SC7 "outcome line, reply, *N events* collapsed"), go on that list with the reason "not copy".
  - **Mutation red:** change one constant's literal (e.g. `ChecklistCopy`'s CHK1) and see the test red; revert. Save
    `P1-red-mutation.txt` and `P1-green.txt`.
  - The initial list is your **finding list for the Owner**: put it in the README as a table (row id · why unmatched).
- **(P2) F1, one COMMENT-ONLY commit, no new test:** in
  `tests/AiDe.Core.Tests/Compilation/TheTypedBoundaryIsInertButRealTests.cs:252-255`
  (`NothingInTheCompilationContextCallsAModel`) the reason "the rung is inert" becomes exactly:
  *"Core's compilation context makes no model call. The App's CompileCallHost, reached only from ComposerSendGate's
  Compiler, is the one call site (R21, Ruling 180)."* The four bans themselves do not change. The commit cites H4 rows
  11-14 of `docs/proof/workbench-loops-wl-1a/prefill/H4-lexical-ban-sweep.md`.
- **Then** the whole Core and App suites, and rule 22's ratchet (this node hands off for a join).

**Tier** T2 · **fan-out** 0 · **budget** 80 tool calls · 120k. **Hand off at 120k** by your own measured usage.
**Kick** at 15 min by mail. Stop at the kick, even mid-check: commit what is green, then run the suite.
**Rules 19, 20, 21, 22 and 23 apply.** Check the board before every App run, as its own command. A
`USER DEMO IN PROGRESS` post with no later `USER DEMO DONE` means no App run of any kind.

## Owned authored paths
`tests/AiDe.App.Tests/DesignCopyRowsArePinnedTests.cs` (new); `tests/AiDe.Core.Tests/Compilation/TheTypedBoundaryIsInertButRealTests.cs`
(the comment at :252-255 only); `docs/proof/workbench-loops-support/copy-pin/**`; the DC-NEW-wl-t26-copy entry's
Prevent and Status lines in `docs/lessons/defect-classes.md`. **No src change and no DESIGN.md change**: a mismatch is a
finding, not a fix. A src or DESIGN.md change is a seam request.
