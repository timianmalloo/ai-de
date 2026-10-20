---
id: proof-wl-t39-slash-claim
title: "T39 wl-slash-claim: the join refuses a trailing-slash claim and a claimed-after-refusal path"
type: proof-pack
status: draft
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: "2026-12-01"
summary: "leader_prejoin.py gains two refusals: a session claim whose path ends in / (PD-6 false grant), and a changed path whose newest refused decision row is newer than every covering claim. Red-green on a planted branch; one residual: CLI claim refusals other than contention leave no record."
---

# T39 `wl-slash-claim`

Controls for PD-6 (`docs/proof/workbench-loops-support/hygiene/pack-defects.md`) and `DC-NEW-wl-t36-claim`
instance 3 (`docs/lessons/defect-classes.md`). Extends T38's check (`../claim-check/README.md`).

## What landed

`python -I .agents/briefs/workbench-loops/leader_prejoin.py <root> --branch <b> --session <s> [--base <rev>]` now refuses (exit 1):

1. **Slash claim.** Any `claim` event of the session whose `path` ends in `/`. The message names the false grant and
   the form `coord claim --wi workbench-loops --path <dir>`.
2. **claimed-after-refusal.** A changed path whose newest `kind: refused` row in `.agents/decisions/<s>.jsonl` is
   newer (`at`) than every claim covering it. Another session's rows and unchanged paths are ignored.

## Evidence

| File | What it shows |
|---|---|
| `claims-red.txt` | Fixture before the change: 2 failures, the slash case and the refusal case (commit `2488198d`) |
| `claims-green.txt` | Fixture after: 0 failures (commit `1fc3675b`). Cases: slash refused; no-slash dir claim passes; refusal after claim refused; refusal before a later claim passes; other-session and unchanged-path refusals ignored |
| `L2-refusals.txt` | What coord writes on a refusal, and the kinds counted |
| `L3-real.txt` | Read-only runs on `wl-input-state-sweep` (refused: 24 unclaimed, 0 claimed-after-refusal, in this tree's copy of the log) and on this branch |

## Finding that corrects the brief

The brief said no refusal is recorded. `.agents/decisions/*.jsonl` holds 108 `refused` rows (`COORD-REFUSED`), written
by `append_decision` for a contention deny and for the edit hook. So check 2 was built.

## Residual

A claim refused at the CLI for `COORD-CLAIM-STALE-CONTROLS`, `-REGISTER-CLASS`, `-DERIVED-CLASS` or `-TTL-CAP`
(`coord-core.py` `cmd_claim`, ~5491-5535) returns 3 and writes no record. Instance 3 of the defect class was a
stale-controls refusal, so check 2 would not have caught it; rule 5's exit-status line stays prose for those four.
Closing it needs a pack change (Ruling 151).
