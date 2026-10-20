# Brief — T33 `wl-fold-diag` (the restored-row fold, observed) · Claude Code · Sonnet · node 1 · READ-ONLY

**Goal:** observe why
`AReopenedNotebookRendersItsRecordedAttemptsTests.ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation`
(`tests/AiDe.App.Tests/Workbench/Records/AReopenedNotebookRendersItsRecordedAttemptsTests.cs:220`) fails with "Space on
the restored row's fold did not open it". It guards keyboard and UI Automation reach to a restored turn's events: an
accessibility path. **Report the observed state, not a cause. No fix.** A fix, if one is due, is a later node the
Owner rules on (Owner, 2026-10-20).

**What is known (read, do not re-derive):**
- It passed in the T30 first-join recount at `d986e4a4` and first failed at `5d66bd0f`. The Owner checked that no
  `src/` or `tests/` diff lies between them. The WebView2 runtime's newest install (155.0.4283.62, 2026-10-18 10:56Z)
  predates the pass.
- T31 measured it at 10/10 in the full suite and 10/10 alone on `84538f7f`. It also fails alone in clean `rt-` trees on
  every join head and parent since.
- In the T32 n2 join's recount, the two foreground-dependent tests (CtrlDigit, SolutionTreeChord) **passed**, and this
  one still failed. So "the desktop has no foreground" does not explain it.

**This node, in this order:**
- **(D1) Capture on origin/main.** Write a **scratch** diagnostic App test that repeats the failing test's steps, from
  the same setup, up to and including the Space. At each step it writes to a file in your proof folder:
  - the UIA subtree of the restored row and its fold (control type, name, AutomationId, IsKeyboardFocusable,
    IsOffscreen, ExpandCollapse pattern state), before and after the Space;
  - the focused element (WPF `Keyboard.FocusedElement` and the UIA focused element);
  - the fold's `IsExpanded` before and after;
  - whether the key reached the element: a `PreviewKeyDown`/`KeyDown` handler record on the fold and on the row, and
    whether the event was `Handled` and by whom.
  Run it **alone**. Also run the real test alone in the same session. Save both runs' output.
  The scratch test is **never committed under `tests/`**. Save its source as `D1-capture-test.cs.txt` in the proof
  folder, and delete it from `tests/` before any commit.
- **(D2) The same capture on `d986e4a4`, now, on this machine.** Make a clean detached tree:
  `git worktree add --detach C:/Projects/rt-wl-fold-diag-d986 d986e4a4`. Copy the scratch test in. If an API differs
  at that sha, adapt it minimally and record each difference. Run it and the real test alone, and save both.
  Remove the tree afterwards (`git worktree remove --force`).
  - **Fails there too:** the cause is outside the repo. Write down what the two captures show and where they differ.
  - **Passes there:** the "no src/tests diff" reading is wrong. Re-read the range
    (`git diff --stat d986e4a4 5d66bd0f`, then the whole `d986e4a4..origin/main` range for the files the test's path
    touches), and list each file that differs and lies on the path, by `file:line`.
- **(D3) Report:** a `README.md` (V2 frontmatter, rule 13) with the two captures side by side, the observed difference
  (or "none observed"), and what was not captured. **No cause is claimed.** The tell-words of NG1 (*should, probably,
  likely, presumably*) do not appear. A belief goes in an `assume:` line with what would confirm it.

**No red-first and no whole-suite run** in this node. Rules 7 and 18 do not apply: no check is added and no code is
committed. The proof is the capture. Name that in the done mail. Rule 22's ratchet still runs (proof files only).

**Tier** T2 · **fan-out** 0 · **budget** 80 tool calls · 120k. **Kick** at 30 min by mail.
**Rules 19, 20, 21, 22 and 23 apply.** Read the board before every App run, as its own command. A
`USER DEMO IN PROGRESS` post with no later `USER DEMO DONE` means no App run. Commit what you have, mail the Leader
`T33 HELD by USER DEMO at <time>`, and end your turn with the same line.

## Owned authored paths
`docs/proof/workbench-loops-support/fold-diag/**` (README, captures, `D1-capture-test.cs.txt`, `ratchet.txt`). A
scratch test under `tests/AiDe.App.Tests/` that exists only between runs and is **never committed**. The `rt-` tree
named above. **No src change, no committed test change, no DESIGN.md change.**
