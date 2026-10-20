---
id: proof-wl-t38-claim-check
title: "T38 wl-claim-check: the join refuses a branch with unclaimed changed paths"
type: proof-pack
status: draft
owner: "@timianmalloo"
links:
  - rel: implements
    target: lessons-defect-classes
review-by: 2026-12-01
summary: "leader_prejoin.py gains --branch/--session: every path a branch changes needs a coord claim from the node's session, or coord must refuse to claim it. Red-green on a planted branch; two real joined branches refused."
---

# T38 `wl-claim-check`

Control for `DC-NEW-wl-t36-claim` (`docs/lessons/defect-classes.md`).

## The check

`python -I .agents/briefs/workbench-loops/leader_prejoin.py <repo root> --branch <b> --session <s> [--base <rev>]`

- Changed paths: `git diff --name-only <merge-base main b> b` (`--base` replaces the merge-base).
- Claims: `kind: claim` events with `session == <s>` in `<repo root>/.agents/log/<s>.jsonl`. A path is covered when
  coord-core's own `lease_covers` says so (exact path, or a directory lease minus its `--except` paths).
- Output names the session, branch, count checked, count exempt (with class) and each unclaimed path. Exit 1 on any.
- Without the new arguments the tool behaves as before (foreign uncommitted files; same usage line prefix).
- A missing or unreadable log, or a branch that changes no path (for example one already merged, where the merge-base is
  its own head), exits 1 with that reason. Never a pass.

## Exemption rule and where it comes from

A path coord refuses to claim cannot be claimed, so it is exempt. The test is coord-core's own, imported from
`docs/ai-forward-pack/scripts/coord-core.py` (no second list):

| Exempt | Source in coord-core.py | Measured |
|---|---|---|
| `.agents/**` (includes `.agents/briefs/**`, this tool) | `make_event`, `COORD-CLAIM-SELF` | claim on `leader_prejoin.py` refused 2026-10-20 |
| `register` class (`docs/audit/audit-log.jsonl`, ...) | `classify` over `.agents/artifacts.yml`, `COORD-CLAIM-REGISTER-CLASS` | C2: `register 1` |
| `derived` class (`docs/audit/audit-data.js`, ...) | `classify`, `COORD-CLAIM-DERIVED-CLASS` | C2: `derived 1` |

The audit log and its derived views are therefore exempt, as coord treats them. Registry root is `<repo>/.agents`
(`load_registry(root)` reads `root/artifacts.yml`); a first version passed the repo root and exempted nothing. C2
caught it.

## C1 red and green

`tools/test-leader-prejoin-claims.py` builds a temp git repo: branch `node` changes `src/a.txt`, `src/b.txt`,
`docs/c.md` and `.agents/log/x.jsonl`. Cases: 2 of 3 claimed (exit 1, names `docs/c.md`); all claimed with the
`.agents` path exempt (exit 0, counts printed); directory leases (exit 0); `--except` carve-out (exit 1); missing log
(exit 1); unreadable log (exit 1). `C1-red.txt`: every case fails against the old tool (usage, exit 2). `C1-green.txt`:
`ok (0 failures)`.

## C2 on real branches (read-only, `C2-real.txt`)

Both branches are already merged, so the merge-base is their head and the diff is empty. The tool refuses that case; the
runs pass `--base <fork point>` (merge-base of the join commit's first parent and the branch).

- `wl-flake-remeasure` / `wl-t36-flake-remeasure`: **refused**, 9 of 12 unclaimed (the proof files). The brief expected
  "claimed". The session log holds claims for only `defect-classes.md` and the README.
- `wl-chk6-gap-pin` / `wl-t35-chk6-gap-pin`: refused, 10 of 12 unclaimed: DESIGN.md, `defect-classes.md`, the Core test,
  and the proof folder. The brief expected DESIGN.md and the register; the log has no claim at all for the other paths.

## Residual

A claim record shows a claim exists, not that it came before the edit. The check ignores TTL and release. Proof files
a node writes under its own folder must be claimed too, or the join refuses.
