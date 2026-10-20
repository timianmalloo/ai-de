# Brief — T38 `wl-claim-check` (the join refuses a branch whose changed paths have no coord claim from the node's session) · Claude Code · Sonnet · node 1

**Goal:** the control owed by `DC-NEW-wl-t36-claim` (`docs/lessons/defect-classes.md`), by Owner ruling 2026-10-20
(`.agents/briefs/workbench-loops/obligations.md`, the last "Owner rulings on T36" row, item 5).
`.agents/briefs/workbench-loops/leader_prejoin.py` runs before every join. It gains a check: for the branch being
joined, every path the branch changes has a granted `coord claim` from **that node's session**. Otherwise it
**refuses** (exit 1), naming each unclaimed path.

**What exists (read it before you change it; rule 14 and rule 23):**
- `leader_prejoin.py` (47 lines): `python -I leader_prejoin.py <repo root>`; exit 0 clean, 1 refused, 2 usage. Today
  it refuses on foreign uncommitted files in the primary checkout. That behaviour and its usage line stay unchanged
  when the new arguments are absent.
- Claim records: `kind: "claim"` events in `.agents/log/<session>.jsonl` in the **primary** checkout's coordination
  store, with `session`, `path`, `at`, `ttl` and `wi` (for example
  `.agents/log/wl-t36-flake-remeasure.jsonl`, seq 2-3). Read `coord-core.py`'s claim code
  (`docs/ai-forward-pack/scripts/coord-core.py`, the `args.cmd == "claim"` branch near `:5472`) to learn:
  how a directory lease covers a file, what `--except` carves out, and which paths a claim **refuses by class**.
  Measured 2026-10-20: a claim on a `.agents/**` path is refused with `COORD-CLAIM-SELF`. The claim self-test also
  names a "derived" refusal class. A path that coord refuses to claim **cannot** be claimed, so the check must exempt
  it, using **coord's own classifier** (import or call it), never a second hand-written list. If the classifier
  cannot be reached without a src change to coord-core.py, raise a seam request (rule 4) and stop that step.
- The audit log and its derived views are written by `audit-log.py` under rule 12. Find out from coord whether they
  are claimable or refused by class. Treat them by what coord does, and say so in the README.

**This node, in this order:**
- **(C1) Red, then green.** `leader_prejoin.py <repo root> --branch <b> --session <s>`:
  - The changed paths are `git diff --name-only <merge-base of main and b> <b>`.
  - Each path is covered by a claim event from session `<s>` (exact path, or a covering directory lease minus its
    `--except` paths), or exempt by coord's class refusal. Otherwise the path is listed and the exit is 1.
  - The output names the session, the branch, the count checked, the count exempt (with the class), and each
    unclaimed path.
  - A missing or unreadable session log is exit 1 with that reason, never a pass (instrumentation: degrade to
    "not recorded", never to clean).
  - **Red on a planted branch:** a test (under `tools/`, your file name; follow how the repo's existing tool
    self-tests run, for example `tools/verify-gate-self-tests.py`) builds a throwaway git repo in a temp directory
    with a branch that changes 3 paths, plus a session log claiming 2 of them. It asserts exit 1 naming the third.
    It asserts exit 0 when all 3 are claimed, exempt for a `.agents/**` path, and exit 1 for a missing log. Write it
    first and see it fail before the check exists (rule 7).
- **(C2) Real branches, read-only:** run the check on two branches that exist and are joined:
  - `wl-flake-remeasure` with session `wl-t36-flake-remeasure` (expected: claimed, because it claimed late);
  - `wl-chk6-gap-pin` with session `wl-t35-chk6-gap-pin` (expected: refused on DESIGN.md and the register).
  Record the outputs in `{PROOF}/C2-real.txt`. If the branch or the log is gone, say so. **Never create, rename or
  delete a branch or a log in the primary checkout.**
- **(C3) Wire it:** the Leader's join pattern runs `leader_prejoin.py` before `conductor-join.py`. Add one clause to
  the **Joins** bullet of `docs/coordination/workbench-loops-leader-kickoff.md` (line 100, in "Cadence to the Owner")
  naming the new arguments. That is the only edit outside your tool.
- **(C4) Register:** in `DC-NEW-wl-t36-claim`, the Prevent item (2) and Status: `controlled` once C1 and C2 hold.
  Keep the Residual: a claim record shows a claim exists, not that it came before the edit.
- **(C5) README** (V2 frontmatter, rule 13): the check, the exemption rule and where it comes from, C1's red and
  green, C2's outputs and the residual. Then rule 22's ratchet and rule 12.

**No App run and no test suite run beyond your own test** (rule 18 is met by your test plus the ratchet; you change
no src and no test under `tests/`). **No src change.**
**Run `coord claim --wi workbench-loops --path <file>` before the first edit of each file, even when the hook
reports the paths free.** `coord claim` refuses without `--wi`. This node's own join will be the first one checked.
**You and T37 (`wl-input-state-sweep`) run at the same time.** T37 runs App suites and owns
`tools/verify-test-run.py`, `tools/expected-test-counts.json` and test files under `tests/`. Touch none of them.

**Tier** T1 · **fan-out** 0 · **budget** 40 tool calls · 80k. **Kick** by mail at about 12 min, from the ladder
(Inferred: ~6.9k tokens/min, the fastest recent node, puts 80k at about 12 min). A kick means: commit what is green,
then C4-C5 on what is done, labelled PARTIAL. Rules 16, 19, 21, 22, 23 and 24 apply.

## Owned authored paths
- `.agents/briefs/workbench-loops/leader_prejoin.py`.
- One new test file under `tools/` for C1.
- One clause in the Joins bullet of `docs/coordination/workbench-loops-leader-kickoff.md` (C3).
- The `DC-NEW-wl-t36-claim` entry in `docs/lessons/defect-classes.md`.
- `docs/proof/workbench-loops-support/claim-check/**`.
