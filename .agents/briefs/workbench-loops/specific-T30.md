# Brief — T30 `wl-prefill` (AC-W01a, Rulings 180 and 182) · Claude Code · Opus · node 4

**Goal:** the user sees their own finding fixed (Owner), and Ruling 180's last two conditions are met.
**T30 n1–n3b are joined on main** (`d986e4a4`). Under gate 1, the prefill is the default: lines are marked
`? suggested`, kept across a prepare, and never described as saved. The settings row gives the gate's real reason.
A restored row's compiled prompt is rebuilt and verified, or reads *Compiled prompt not recorded*. Keep's help text is
in the Owner's words.

**This node (n4), in this order:**
- **(0) The prefill demo script (Owner; docs only, first).** Add a section **"Human demo script — the prefill"** to
  `docs/proof/workbench-loops-wl-1a/prefill/README.md`. It runs **after** the user's WL-S walk-through, on a **fresh
  clean worktree of origin/main at `d986e4a4` or later**, Release build, never the primary. Keyboard only. **Read every
  gesture, label and string from the code, and cite file:line in a gesture table, as the WL-1a script did** (rule 14;
  the Leader's own unchecked claims were filed today, so check, do not recall). It must cover:
  - the two-press prepare-then-send under `agentic-advisory`: the first Ctrl+Enter prepares, the second sends;
  - **the one model call on the user's account, flagged in a line before the step**, the way WL-1a step 22 flagged
    real adapter usage;
  - the `? suggested` lines appearing, editing one inline, **Keep**, the line staying across a second prepare, and the
    Keep help text read in UIA;
  - the settings row naming the mode and the gate's reason, and switching to `mechanical-only`;
  - a restart: a sent turn's row shows the rebuilt compiled prompt, or *Compiled prompt not recorded* for a turn with
    attachments or a template; and the residual, said plainly: unsent typing and kept suggestions are lost on restart
    until WL-2.
  Mark anything you could not read from code as **rehearse**.
- **(A) Ruling 180 (4): the cost is shown.** Each advisory call's spend reaches the existing compile-cost line. Red
  first, with a stubbed call that reports a spend. Then the recorder for the first 50 advisory calls' cost and latency:
  where they land (a reader over the existing `called` rows, not a new store), and a test that the reader counts them.
  **No real model call in any test.**
- **(B) Ruling 180 (6): compile-eval runs in the track.** Run `tools/compile-eval` (score.py / ring.py) with ADR-0036's
  floors on the advisory path, the way the eval's README says. Record the result. If it needs a real model call or
  corpus data this machine lacks, record NOT RUN with the reason and the exact command; do not fake a pass. Carry the
  AI-systems lens's conditions from `docs/notes/wl-dr-prefill-goal-done.md` §4 into the Proof Pack as a checked list.
**Node 5 (not this one):** the four sheet tests that read the real `~/.aide/proof`, with the Leader's seam grant at
dispatch, then rule 22's ratchet.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k; **hand off at 150k**. **Kick** at 15 min by mail.
**Rules 19, 20, 21 apply.** The WL-S walk-through may start at any moment, so check the board before every App run, as
its own command.

## Owned authored paths
As in node 3, plus `docs/proof/workbench-loops-wl-1a/prefill/**` and the compile-cost line's region. A change to the
eval's tooling is a seam request.
