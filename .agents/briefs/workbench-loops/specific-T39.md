# Brief — T39 `wl-slash-claim` (the join refuses a trailing-slash claim, and reports a claim made after a refusal) · Claude Code · Sonnet · node 1

**Goal:** two more refusals in `.agents/briefs/workbench-loops/leader_prejoin.py`'s claim check (T38's
`--branch --session [--base]` mode), by Owner rulings of 2026-10-20 (`obligations.md`: the "trailing-slash false
grant" row and the "T37's join" row, condition 3).

1. **Slash claim (PD-6):** `coord claim --path <dir>/` is granted, and the record keeps the slash. That lease covers
   nothing (`docs/proof/workbench-loops-support/hygiene/pack-defects.md`, PD-6; measured with coord's own
   `lease_covers`). The check **refuses** when the node's session holds any `claim` event whose `path` ends in `/`.
   The message names the false grant and the no-slash form (`coord claim --wi workbench-loops --path <dir>`).
2. **Claimed after a refusal (DC-NEW-wl-t36-claim instance 3):** a node read `coord claim`'s exit-3 refusal as a
   warning and edited the file. Report a changed path whose last coord-recorded outcome for that path and session
   is a refusal as `claimed-after-refusal`, **if the coord record holds refusals at all.** The Leader measured on
   2026-10-20 (Inferred until you check it): no `.agents/log/*.jsonl` event has a refusal kind (claim 3,989,
   release 3,279, others; none refused). `.agents/decisions/<session>.jsonl` holds the commit hook's `kind: "allowed"`
   decisions only, including T37's unclaimed `tools/verify-gate-self-tests.py`. Read
   `docs/ai-forward-pack/scripts/coord-core.py`'s claim refusal path (near `:5475` onward, and `make_event`) to
   establish whether **any** refusal is written anywhere. **If none is, do not build this refusal.** Report it in the
   README as not checkable, with the evidence (the code path, and the kinds counted), and the residual: "a claim
   refused at the CLI leaves no record; rule 5's exit-status line is prose". Never edit `coord-core.py` (a pack copy,
   Ruling 151).

**This node, in this order:**
- **(L1) Red, then green, for (1):** extend `tools/test-leader-prejoin-claims.py` (T38's fixture: a throwaway repo
  and a session log) with a planted `claim` event whose path is `<dir>/` and a changed file inside `<dir>`. Expect
  exit 1 and the false-grant message. Also expect that a no-slash claim on `<dir>` still passes for that file. See it
  fail before the change (rule 7).
- **(L2) Establish (2).** Write your finding to `{PROOF}/L2-refusals.txt`: what the code writes on a refusal, and the
  kinds counted across `.agents/log` and `.agents/decisions`. If refusals are recorded, build (2) red-then-green the
  same way; if not, stop that step as above.
- **(L3) Real run, read-only:** run the check on `wl-input-state-sweep` with session `wl-t37-input-state-sweep` and
  `--base` the merge's first parent, plus on your own branch. Record both in `{PROOF}/L3-real.txt`.
- **(L4) Register:** in `DC-NEW-wl-t36-claim`, Prevent (3): what landed, and the residual if (2) is not checkable.
  In PD-6 (`pack-defects.md`), the Interim line: the join-side refusal landed.
- **(L5) README** (V2 frontmatter, rule 13), then rule 22's ratchet and rule 12.

**First write:** claim your proof directory, `coord claim --wi workbench-loops --path {PROOF}` (no trailing slash),
then each file before its first edit. **A claim's exit status is its result**: non-zero is not granted. On
`COORD-CLAIM-STALE-CONTROLS`, run `git merge origin/main` and claim again before the edit.
**No App run** and no test run beyond your fixture (rule 18 is met by the fixture plus the ratchet). **No src change.**

**Tier** T1 · **fan-out** 0 · **budget** 20 tool calls · 40k (Owner). **Kick** by mail at about 8 min (ladder: the last
two nodes ran at about 4.8k tokens/min, so 40k falls at about 8 min). A kick means: commit what is green, then L4-L5
on what is done, labelled PARTIAL. Rules 16, 19, 21, 22, 23 and 24 apply.

## Owned authored paths
- `.agents/briefs/workbench-loops/leader_prejoin.py`.
- `tools/test-leader-prejoin-claims.py`.
- The `DC-NEW-wl-t36-claim` entry in `docs/lessons/defect-classes.md`; the PD-6 section of
  `docs/proof/workbench-loops-support/hygiene/pack-defects.md`.
- `docs/proof/workbench-loops-support/slash-claim/**`.
