---
id: coordination-workbench-loops-leader-handoff-1
title: "Leader hand-off 1 — workbench-loops drive (wl-leader-20261018 → wl-leader2-20261019)"
type: plan
status: proposed
owner: "@timianmalloo"
tags: [coordination, handoff, workbench-loops, leader]
links:
  - { to: coordination-workbench-loops, rel: implements }
  - { to: coordination-workbench-loops-leader-kickoff, rel: depends-on }
review-by: "2026-11-18"
summary: >-
  The first Leader's hand-off after the serial spine landed: what is on main, every track's tree, branch and last sha,
  the binding rulings (148–154 and the 151(3) T3 reading), the step-8 ratchet procedure, the owed items, and the wave-2
  dispatch order with per-track budgets split per GO9. The resume brief is the last section.
---

# Leader hand-off 1 — workbench loops

**Plan:** `docs/coordination/coordination-workbench-loops.md` (its "Changes since dispatch" log is current to this note).
**Briefs:** `.agents/briefs/workbench-loops/`. Holds `brief-common.md` (the CTX-F standing rules, now including rule 13:
V2 frontmatter plus `docs-graph validate` before done), `render_briefs.py` (it renders `specific-<T>.md` and the common
rules into `brief-<T>-<session>.md`), the rendered wave-2 briefs T5/T6/T7/T8/T11, and the Leader tools `gate_ratchet.py`,
`keep_both.py` and `list_requests.py`. Copy the folder to your scratchpad and run the tools from there with `python -I`.

## On main (origin/main, every join through `conductor-join.py`, step 8 per Rulings 151/153)
| join | sha | notes |
|---|---|---|
| WL-0 (T1 + fix-forward T1b) | `4e615327` | FR-001/FR-002 red→green; the GUI demo of steps 2–4 is **owed**, folded into the WL-1a real-lane demo |
| T2 schema v2 (+ Rulings 148, 150 reworks) | `d0cee41b` | ADR-0039 as amended by Rulings 148 + 150 |
| T12 census (+ T12b, + T12c root cause) | `36bcfaea`, `45c9b940` | census retries only its read; raw C0 characters in JSON escaped (0x15 root cause) |
| T3 RunEvent/TurnState | `d35aae27` | under the Owner's 151(3) reading, recorded verbatim in its join audit; condition (b) discharged on main |
| T13 gate hygiene (+ T13b, Ruling 154) | `34e82fc4` | strict step 8: 5 of 47 red; the register union-merges (`.gitattributes`) |
| T4 re-row | **pending**: local join commits `81880c04` + `5979a7ff` are unpushed; App floor lowered 1064→1026 with the reason (37 test attributes in 5 deleted files) | ratchet refused 2 green→red (verify-cited-controls, verify-fixture-derivation); fix-forward **T4c** running at hand-off time, see the state table |

## Track state at hand-off
| track | branch / tree | last sha | state | next |
|---|---|---|---|---|
| T1 wl0-evidence | `wl0-evidence` · `C:\Projects\ai-de-wl0-evidence` | `dc3b148a` | joined | cleanup (report-only) |
| T2 sp1-schema-v2 | `sp1-schema-v2` · `C:\Projects\ai-de-sp1-schema-v2` | `888aeb36` | joined | cleanup |
| T3 sp2-runevent-vocab | `sp2-runevent-vocab` · `C:\Projects\ai-de-sp2-runevent-vocab` | `fba3e3b5` | joined | cleanup |
| T12 census-flake | `census-flake` · `C:\Projects\ai-de-census-flake` | `b306e90e` | joined | cleanup |
| T13 gate-hygiene | `gate-hygiene` · `C:\Projects\ai-de-gate-hygiene` | (on main at `34e82fc4`) | joined | cleanup |
| T4 sp3-perspective-rerow | `sp3-perspective-rerow` · `C:\Projects\ai-de-sp3-perspective-rerow` | see below | **see the final-state section** | re-join, ratchet, push |
| T9 wls-unparent-spike | `wls-unparent-spike` · `C:\Projects\ai-de-wls-unparent-spike` | `a89c26a8` | review-ready (spike exit 0, re-run by the Leader) | join after T4 |
| T10 e1-notebook-spike | `e1-notebook-spike` · `C:\Projects\ai-de-e1-notebook-spike` | `29553f30` | review-ready (E-1 PASS, not reverse; merged origin/main; spikes index row added) | join after T9 |
| Q0 grok / agy | `q0-grok-wl`, `q0-agy-wl` (tagged `archive/q0-*`) | `ca7734aa`, `8c71e9e8` | done: both **observed-only** | keep (cleanup holds tag-only commits) |
| scratch | `C:\Projects\ai-de-ratchet-parent`, `-ratchet-head` (detached) | — | Leader's ratchet trees | reuse; `git checkout -f --detach <sha>` + `git clean -fdq -e artifacts` before each run |

**Open requests to the Leader: 0** (cumulative: 10. Resolved: 5, all answered. Expired: T4 seams 2–5b, missed by the first
Leader and granted retroactively in T4b). **Open DRs: 0.**

## Binding rulings (addendum-c register, `docs/notes/addendum-c-council-rulings.md`; next is Ruling 155)
- **148** — ADR-0039 amended: T2's six departures, with merge conditions (a)–(h), all landed.
- **149** — ExplorerSurface deleted. The in-app knowledge/spec loss is stated with `docs/index.html` as the interim path.
  **The knowledge-inclusive canvas mode is a WL-S close condition** (in T11's brief).
- **150** — `journey-terminal-drafts` standing owner; `body_sha` test-asserted; the forbidden payload keys are exactly
  ADR-0039 §3's fold-derived names.
- **151** — the step-8 gate policy: a **finding-level ratchet** against the first parent, both run back to back in the
  same environment. test-run, terminal-host-exit-paths and project-coverage are never ratcheted; their evidence is the
  conductor recount. Any green→red stops the join.
- **152** — ADR-0045 deviations D2/D4/D5 accepted. **D2's flip to Center is a T11 merge condition.** ADR-0045 is
  amended by 149 + 152.
- **153** — the verify-ruling-citations filter (drop "no heading in rulings.md" only where addendum-c has exactly one
  heading; record the counts). Normalisation is limited to the tree-root prefix.
- **154** — T13's exemptions: the machine-path opt-outs are accepted; the mockups stay scanned with a
  design-system-colour-only exemption.
- **The Owner's 151(3) reading (T3's join audit, verbatim):** the exception covers only a *transiently* unreadable census
  read. A deterministic cause is not excused, except where a join's diff touches no terminal-host or process-lifecycle
  code. With T12c on main, that question is closed unless it recurs.

## Step 8, as practised (do this for every join until main is green)
1. `conductor-join.py <branch> --epoch <n> …` (start it in the background; it takes 10–20 min). If its recount fails,
   read the trx: census reads are now fixed, so any failure is real. If it refuses to *lower* a floor, the deletions
   must explain it; then run `verify-test-run.py --update --allow-lower "<reason>"`, the two split recounts
   (`join.json`), `--no-run` (must be OK), and `--continue --docs-only`.
2. It always stops at step 8 (strict line red). Then run
   `python -I gate_ratchet.py <parent tree @ origin/main> <head tree @ join HEAD> <out>`.
3. **PASS** → `git push origin <join-sha>:main`, run the join's Release build, write an `audit-log.py append`
   recording the ratchet (filter counts, exclusions), commit `docs/audit` + `.agents`, push. **REFUSE** → the finding
   is real: send it back to its track as a fix-forward, re-join, and re-ratchet.
4. Lessons from tonight: evidence READMEs need frontmatter (rule 13). "R-n" or "Rulings 1 to N" in prose reads as a
   ruling citation. Tests must derive kind lists (fixture-derivation). Comments must cite existing controls. `-X ours`
   on authored files needs a three-way diff.

## Owed items (not drive-blocking, but tracked)
- **WL-0 GUI demo** (steps 2–4), folded into the WL-1a real-lane demo.
- **Manual native IME check** (E-1): it needs a human to enable a Japanese input profile, then `--check ime,ime-control`
  with a dated record. Until then WL-2 must not make the notebook the default authoring surface.
- **WL-S knowledge-mode row** (Ruling 149): a WL-S close condition, carried in T11.
- **Project-coverage CI step** (for the user): the workflow steps are in
  `docs/proof/workbench-loops-support/hygiene/residuals.md` RES-1. Declined for T13 because it changes what runs on every push.
- **Pack defects PD-1/PD-2** (`docs/proof/workbench-loops-support/hygiene/pack-defects.md`), plus the docs HTML surface cap
  at 100/100 (artifacts are Markdown only) and the gate's false positive on a lambda throw (T12b). These are next steps
  for the pack.
- **Audit-capture:** 3 drive entries still need their owners' corrections (wl-leader ×1, wl-t1 ×1, wl-t2 ×1). The
  Owner's two are done.

## Wave 2 — dispatch order and budgets (split per GO9; T4's 344/280 calls and 632k context was the signal)
1. **T5 `wl1a-attempt-host`** (Opus, `/design-slice` WL-1a first). The brief is in the folder; base it on origin/main once
   T4 is on main. **Two nodes**: node 1 = design + host interface + gate/crash windows; node 2 = Stop/sweep/run-log/
   redaction wiring/NFR-11/Proof Pack. Hand off at 200 calls or 300k. 8 h total.
2. **T7 `wl1a-redaction`** on **Grok** via `coord-runner.py --launch`. Compile its brief into a finished compilation
   first (`prompt-compile.py skeleton --text-file` → fill → `finish`), using the Q0 contract shape
   (`argv grok agent -m grok-4.7 --reasoning-effort high --no-leader stdio`; read the served model per response). Fall
   back to Sonnet in the same tree. 40 calls · 150k · 1 h.
3. **T6 `wl1a-folds`** (Sonnet) after T5's design commit. 140 · 400k · 3 h.
4. **T8 `wl1a-notebook-binding`** (Opus) after T5's host-interface commit. Two nodes. It carries the Ruling 148
   adoption-actor obligation and T3's follow-ups.
5. **T11 `wls-kinds-migration`** (Opus) after T8 + T9. Two nodes. It carries D2's Center flip and the knowledge mode.
6. Then the WL-1a slice Proof Pack + the Owner demo (real claude-code lane, keyboard only, plus the owed WL-0 GUI steps)
   → WL-1b → WL-1c → WL-2 ∥ WL-3 …, each refreshed from actuals (GO19).

## Final state at release (written last)
<!-- filled by the Leader immediately before `coord leader release` -->

## Resume brief (the fresh Leader's first lines)
```
python docs/ai-forward-pack/scripts/audit-log.py start --session wl-leader2-20261019 --skill execute-with-coordination
python docs/ai-forward-pack/scripts/coord-core.py leader pin wl-leader2-20261019 --ttl 900
```
Then read this note, the plan's "Changes since dispatch" log, and `.agents/briefs/workbench-loops/obligations.md`. Run
`coord doctor` and `git worktree list`. Run the request census (`list_requests.py`) and answer every open one before the
first report. Continue at **"Final state at release"** above.
