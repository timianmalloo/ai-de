# Brief — T35 `wl-chk6-gap-pin` (CHK6's gap clauses, pinned against DESIGN.md) · Claude Code · Sonnet · node 1

**Goal:** every blocking-gap sentence that Core's `AcceptanceFold` writes is pinned by a **Core test** against its
DESIGN.md text, and every sentence has DESIGN.md text to pin against. This is the docs-and-test node of the Owner's T32
ruling (2) (`.agents/briefs/workbench-loops/obligations.md`, "Owner rulings on T32's findings"; Owner, 2026-10-20).
CHK6 is pinned today on `Ready to accept.` only (`tests/AiDe.App.Tests/DesignCopyRowsArePinnedTests.cs:129`;
`docs/proof/workbench-loops-support/copy-pin/README.md:37`), because the gap clauses are inline interpolated literals
with no constant to pin.

**What is known (read at base, confirm by opening):**
- The fold writes **7** gap sentences, one per `AcceptanceGapCodes` value, in
  `src/AiDe.Core/Workbench/Records/Folds/AcceptanceFold.cs`: lines 77-78 (checklist-empty), 83-84 (intent-missing),
  94-95 (intent-not-accepted, `{revision}`), 185-190 (evidence-stale, evidence-not-recorded, evidence-missing, each with
  the obligation's text), 196-197 (needs-decision).
- DESIGN.md's CHK6 row (`DESIGN.md:1433`) lists **3** of them, in its parenthesis separated by ` · `, with the
  placeholders `{obligation}` and `{n}`: no current evidence, intent revision not accepted, no obligations declared.
- So **4** sentences have no DESIGN.md text: intent-missing, evidence-stale, evidence-not-recorded, needs-decision.
  Confirm this count yourself before writing anything; if it differs, report the difference in the done mail.
- Existing fold tests: `tests/AiDe.Core.Tests/Workbench/Folds/AcceptanceFoldTests.cs` (how to build each input state).
  Repo-root lookups in Core tests: for example `tests/AiDe.Core.Tests/Sessions/SessionPathContractTests.cs:178`.

**The oracle rule (DC-NEW-wl-t26-copy; Owner, binding):** the expected text is read **from DESIGN.md**, never from the
code under test. The test may not copy a sentence out of `AcceptanceFold.cs` into an expected string, and may not read
`AcceptanceGap.Sentence` to build its own expectation.

**This node, in this order (two checks, rule 16):**
- **(G1) The 4 missing clauses go into DESIGN.md, copy only.** Add each to CHK6's parenthesised list, in the same
  ` · `-separated form and with the same placeholders (`{obligation}`, `{n}`). Each clause is the text the code renders
  today (rule 14: the code on main outranks the document). Change nothing else in DESIGN.md. Keep CHK6's cell starting
  with `Ready to accept.`, so the App pin on CHK6 still holds. Choose your own way only if you find a reason this form
  cannot work; then say why in the README and the done mail.
- **(G2) The Core test, red first.** One new test file,
  `tests/AiDe.Core.Tests/Workbench/Folds/AcceptanceGapSentencesMatchDesignTests.cs`. It:
  - reads DESIGN.md's CHK6 row and parses its clause list;
  - drives `AcceptanceFold.Evaluate` to each of the 7 gap codes, with known values for the obligation text and the
    revision number;
  - fills the DESIGN.md clause's placeholders with those same known values, and asserts the fold's sentence equals it;
  - fails if any gap code has no clause, or any clause matches no gap code. The failure names the code or the clause.
  **Red 1:** run it against DESIGN.md as it stands at base. It must fail naming the 4 missing clauses
  (`{PROOF}/G2-red-missing.txt`). Then do G1, and the test is green (`{PROOF}/G2-green.txt`).
  **Red 2, the mutation (Owner):** change one clause in a **scratch** copy of `AcceptanceFold.cs` (for example a word
  in the evidence-missing sentence), run the test, see it fail naming that clause, and restore the file
  (`{PROOF}/G2-red-mutation.txt`). The scratch edit is never committed; `git diff --stat src/` must be empty before
  every commit.
- **(G3) A disagreement is reported, never fixed.** If a sentence the code renders and its existing DESIGN.md clause
  disagree (any of the 3 existing clauses), change **neither**. Put the clause on a shrink-only list in the test with
  the reason "text differs, Owner to rule", put both texts in the README, and name it first in the done mail. (At base
  the Leader read the 3 existing clauses as equal to the code's text; confirm this, do not assume it.)
- **Then** the whole Core suite and the whole App suite (mandatory, Owner), rule 22's ratchet, and rule 12. Update the
  DC-NEW-wl-t26-copy Status line to say CHK6's gap clauses are pinned by the new Core test.

**Known App results on this machine.** Name these as known, never as new failures:
- `ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation`: record it as **NOT CHECKED: the OS
  reports the left mouse button down** (Owner, 2026-10-20). It is not a failure of this node.
- `CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective` and
  `CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`: known failures (T31, N=10).
Any other failure is yours: run it alone and report both results. Never inject mouse or keyboard input outside a
test's own window, and never call `SendInput` or `mouse_event` to change the desktop's state.

**Tier** T2 · **fan-out** 0 · **budget** 80 tool calls · 120k. **Kick** at 30 min by mail.
**Rules 19, 20, 21, 22 and 23 apply.** Check the board before every App run, as its own command. A
`USER DEMO IN PROGRESS` post with no later `USER DEMO DONE`: finish the run in flight, commit what is green, mail the
Leader `T35 HELD by USER DEMO at <time>`, and end your turn with the same line.

## Owned authored paths
`tests/AiDe.Core.Tests/Workbench/Folds/AcceptanceGapSentencesMatchDesignTests.cs` (new). `DESIGN.md`: the CHK6 row's
cell only. `{PROOF}/**`. The DC-NEW-wl-t26-copy Status line in `docs/lessons/defect-classes.md`. A scratch edit of
`src/AiDe.Core/Workbench/Records/Folds/AcceptanceFold.cs` that exists only during Red 2 and is **never committed**.
**No src change. No change to `tests/AiDe.App.Tests/DesignCopyRowsArePinnedTests.cs`**: if its CHK6 comment now reads
wrong, name it in the done mail as a finding.
