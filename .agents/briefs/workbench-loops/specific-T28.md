# Brief — T28 `wl1a-compiled-field` (WL-1a fix-forward) · Claude Code · Opus · node 1 (investigate)

**Goal (Owner, 2026-10-19 ~15:50Z, high priority, ahead of every WL-1b join):** the user submitted a prompt during the
WL-1a walk-through, and the compiled prompt did not show in the compiled field. This is a **recurrence of Ruling 96**
(`docs/notes/addendum-c-council-rulings.md`, "Ruling 96 — F-E", about :1626-1634). Ruling 96's cause was only **Inferred**
("consistent with the belt not re-measuring on expand"), and its measurement was made a condition.
**This node runs `/investigate`** (`.claude/skills/investigate/SKILL.md`). It **stops for review before any fix lands**:
the Owner reads the root cause first.
**Done when, all in this node:**
1. **Reproduced on the real composed tree with an actual submit.** Use the real `WorkbenchShell` / `SessionDocumentSurface`
   / `ComposerSurface` composition, as the existing on-screen composer tests build it. Submit with Ctrl+Enter or the send
   path the user takes (demo script step 5, `docs/proof/workbench-loops-wl-1a.md`), not just a draft change. Then expand
   the compiled disclosure. Record `_compiled.Text.Length`, `_compiled.ActualHeight` and `IsExpanded`, before and after
   the submit, in `{PROOF}/reproduction.txt`. Say which one is wrong.
2. **The verified root cause, by file:line.** Verified means you observed it. A plausible chain is not enough (NG rules).
   If the cause differs before and after a submit, name both.
3. **Why Ruling 96's control did not catch it, as a defect class.** Open the test Ruling 96's condition (2) produced
   (find it; it extends "the existing on-screen test" to the content). State what it drives and what it does not drive.
   For example: a draft change but no submit, or a fixture composition but not the real one. Append
   `DC-NEW-wl-t28-compiled` to `docs/lessons/defect-classes.md` with class / sweep / derive / prevent. The sweep covers
   every other test that asserts composer content on a path the user's gesture does not take.
4. **A red test, committed red only.** It drives a real submit and asserts that the expanded field shows the non-empty
   compiled text (`Text == RenderView(draft).Text`, non-empty, `ActualHeight ≥ CompiledPromptMinHeight`). Save
   `{PROOF}/compiled-after-submit-red.txt`. **Do not write the fix in this node.**
5. `{PROOF}/README.md`, with the root cause, the class, the phased repair plan (code + tests) and its confidence labels.
   Also `handoff.md`.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16) · **kick** at 12 min by mail. The Leader extends
the kick for this node to 20 min, because reproduction on the real tree is one long check; rule 16's 120 calls / 200k cap
still binds.

## Desktop hold (DC-NEW-wl-leader3)
Other tracks may run App UI tests in parallel. A result that says "Ruling 115 desktop hold is occupied" is **not a
failure and not a pass**: record it, wait two minutes, and re-run. Rules 19 and 20 hold.

## Owned authored paths
New tests under `tests/AiDe.App.Tests/Workbench/Composer/` (or beside the existing on-screen composer test; name the
file in handoff.md); `docs/lessons/defect-classes.md` (your DC-NEW entry only); `{PROOF}/**`. **Reading any file is
free. Editing product code is not in this node.**

## Not in scope
The fix (node 2, after the Owner's read of the root cause). The goal / done-criteria prefill (a separate assessment, T29).
