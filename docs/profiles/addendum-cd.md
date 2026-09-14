---
id: profile-addendum-cd
title: "Session profile — the Addenda C/D programme"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "conductor-addendum-c"
tags: [profile, session-profiler, conductor, addendum-c, addendum-d, efficiency, adherence, coordination]
links:
  - { to: design-session-profiler, rel: relates-to }
  - { to: profile-sp-0002, rel: depends-on }
  - { to: profile-conductor-phase1, rel: relates-to }
  - { to: coordination-addendum-cd, rel: relates-to }
  - { to: note-pack-findings-addendum-cd, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-13
review-suggested: []
summary: >-
  A /session-profiler pass over the Addenda C/D programme (2026-09-11 -> 2026-09-13): the conductor
  session claude:919ba21f, its 52 depth-1 nodes and 114 depth-2 persona reviews read from the
  harness's subagents store (which session-profile.py sp-0002 could not see), joined to the audit
  log's per-node durations and the coordination plan's ledger. Est. list-price cost $2,198, 98-99%
  of input from cache; 44.8% of the conductor's active main line was joins and recounts; a
  deferred tool blocked one node for 2 h 15 min; the DC-113 shell shape recurred 102 times on
  commit lines against 4 recorded consequences.
---

# Session profile — the Addenda C/D programme

*A `/session-profiler` pass (profiler session `profiler`, 2026-09-14) over the conductor session `claude:919ba21f` and everything it launched between 2026-09-11T17:03Z and 2026-09-14T01:04Z. The script's own pass is [`profile-sp-0002`](sp-0002/profile.md); this document is the curated half — every Inferred row there confirmed or struck against the transcripts, and every sub-agent number read directly from `~/.claude/projects/C--projects-ai-de/919ba21f-…/subagents/agent-*.jsonl` (+ `.meta.json`), a store the script does not read (finding AC-01). A missing measurement reads **not recorded** (IO8). Cost in USD is **est.** throughout: the harness stores no cost for these sessions; the figure is the token count priced at the first-party list rates the `claude-api` skill carries (cached 2026-06-24: Opus 5 $5/$0.50/$6.25/$25 per MTok in/cache-read/cache-write/out; Sonnet 5 $2/$0.20/$2.50/$10; Fable 5.1 $10/$0.25/$12.50/$50) — the operator runs on a subscription, so this is a list-price equivalent, not a bill.*

## Scope and stores

| store | what it gave | read how |
|---|---|---|
| `~/.claude/projects/C--projects-ai-de/919ba21f-….jsonl` (23.9 MB, 6,951 records) | the conductor main line: 999 requests, 89 profiler turns (36 operator prompts, 50 `<task-notification>` wake-ups), 1 compaction, 772 Bash calls | `session-profile.py profile` (sp-0002) + a direct read for costs and tool timings |
| `…/919ba21f-…/subagents/agent-*.jsonl` + `.meta.json` (166 agents) | every node and review: model, requests, tokens, tool calls, tool waits, context, spans | direct read (the script reads only `isSidechain` records in the main file and found 0 sub-agent requests) |
| `docs/audit/audit-log.jsonl` (92 entries in the window) | `duration_seconds` per node where a marker was set; tier/fan-out; the 14 join entries | direct read |
| `docs/coordination/addendum-cd.md` §Execution ledger | planned seconds per node; the "2.2×" claim | read; recomputed from its own rows |
| `.agents/` (log, decisions, sessions) | 649 claims in the window; 31 `COORD-REFUSED` decisions; 6 live sessions; `metrics` | `coord-core.py session list` / `metrics` + a direct read of `.agents/decisions/*.jsonl` |
| `git worktree list` | 40 `atlas/*` trees (the peer Copilot fleet) beside the conductor's | read, not touched |
| Copilot store (`copilot:45bbc625`, gpt-6-astra) | the peer Atlas fleet's session in the same 3-day window, cwd `C:\projects\ai-de` | `session-profile.py` only; the transcript is the peer's and was not opened |
| sub-agent task notifications | `subagent_tokens` / `duration_ms`: **not recorded** — this harness version's `<task-notification>` carries status, summary and result only; the tokens live in the subagents store above | direct read |

## The numbers (Verified unless marked est.)

### The conductor main line (`claude:919ba21f`, Claude Code, claude-opus-5 on every request)

| measure | value |
|---|---|
| session span | 2026-09-11T17:03Z → 2026-09-14T01:04Z = **56.0 h** wall, of which two idle gaps of 11.0 h (2026-09-12T02:14→13:15Z) and 18.3 h (2026-09-12T22:17→2026-09-13T16:35Z) = 29.3 h with no turn live |
| active main-line time (sum of turn walls) | **30,032 s = 8.3 h** over 89 profiler turns |
| requests | 999, all `claude-opus-5` |
| input: uncached / cache-read / cache-write | 2,098 / **500,537,155** / 5,332,109 tokens — cache share **98.95%** |
| output (of which reasoning) | 963,674 (235,228) |
| context | max **966,688** tokens before the one compaction (turn 60, 2026-09-12T18:39Z → 75,763); back to 861,568 by turn 88 |
| est. cost | **$308** (cache reads are $250 of it) |
| tools | Bash 772 (intent description on **100%**), Agent 52, SendMessage 23, Write 37, Read 12, Edit 9, AskUserQuestion 4, TaskStop 2, Skill 1 |
| goal state | audit entries for the substantive turns carry `goal`/`done_when`/`tier`/`fan_out` (e.g. `r0-owner-rulings-50-55` T2/3, every node entry T1–T2/0–3); the **14 join entries carry `goal`/`done_when` but no `tier`, no `fan_out`, no `duration_seconds`** (14/14) |
| audit session ids | the conductor wrote under two ids: `claude-conductor-addendum-c` (20), `conductor-addendum-c` (8) |

### Where the conductor's active time went (Bash wall by class; 15,912 s of Bash in 30,032 s active)

| class | calls | seconds | share of active |
|---|---|---|---|
| gate / recount | 100 | 7,570 | 25.2% |
| join (merge/resolve/commit/push) | 174 | 5,881 | 19.6% |
| other (reads, edits, probes) | 323 | 1,643 | 5.5% |
| git (other) | 74 | 306 | 1.0% |
| build | 19 | 218 | 0.7% |
| coordination layer | 53 | 217 | 0.7% |
| audit / prompt log | 29 | 77 | 0.3% |
| **joins + gates/recounts** | 274 | **13,451** | **44.8%** |
| model-side (dispatch, briefs, review reading; = active − Bash) | — | 14,120 | 47.0% |

**Recounts.** 78 main-line calls invoked `verify-test-run.py`/`dotnet test` for **8,997 s**; the 21 whole-suite `--update` recounts of ≥ 200 s took **median 393 s (6.5 min)** — not "~5 min" — values: 219, 221, 271, 272, 282, 298, 299, 335, 385, 389, 392, 401, 403, 403, 424, 426, 433, 452, 453, 476, 603. The three scripted joins (`conductor-join.py`, landed 2026-09-13T22:25Z) took 1,359 s; the three `run-verify-gates.py` calls 2,343 s. Inside the nodes: **726 test invocations, 19,909 s (5.5 h)** — every node ran its suites in-tree, then the join re-ran the whole suite on the merged tree.

### The nodes (depth 1: 52 launched; 38 writing nodes, 7 Owner rulings, 7 persona confirmations, 1 profiler)

| measure | writing + rulings (depth 1) | persona reviews launched by nodes (depth 2) |
|---|---|---|
| agents | 52 | 114 (test-architect 27, ux-accessibility 19, the-simplifier 16, security-identity-architect 9, data-persistence-architect 8) |
| effective model | opus-5 33, sonnet-5 12, fable-5-1 7 | opus-5 108, sonnet-5 6 |
| requests / tool calls | 8,280 / 8,541 | 2,326 / 2,989 |
| cache-read / cache-write / output tokens | 2,980,547,957 / 44,564,624 / 1,875,490 | 246,281,007 / 18,249,419 / 975,235 |
| cache share (all sub-agents) | 98.09% | — |
| est. cost | **$1,634** | **$257** (test-architect $76, ux-accessibility $41, simplifier $34, security $24) |
| node-seconds (writing nodes, store spans) | 239,742 s = 66.6 h | median review span 440 s |
| shell waits inside nodes | 51,531 s (21% of node-seconds) | — |
| context | 18 of 52 nodes exceeded 500k tokens; max 966,282 (CV-1, 1 compaction); median max 392,309 | — |
| intent trace on shell calls | 6,560 / 6,684 (98.1%%) | 1,313 / 1,357 (96.8%%) |
| **programme est. total** | **$2,198** = main $308 + nodes $1,634 + reviews $257 | |

### Per node — store span vs audit duration vs plan, tokens, est. cost

*`store span` = first to last record in the node's transcript (a resumed node includes its wait and its second run); `audit` = `duration_seconds` from the node's own marker; `planned` = the wave table; `reviews` = depth-2 agents the node launched. Cost est. at list rates.*

| node | model | start (Z) | store span s | audit s | planned s | span/plan | req | tools | reviews | cache-read | output | est. $ |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| F5a: commit Ruling 49, gates, stop | sonnet-5 | 09-11T17:27 | 476 | 407 | — | — | 45 | 73 | 0 | 4,424,066 | 8,910 | 1 |
| R0: Owner rules on Addendum C | fable-5-1 | 09-11T17:28 | 230 | not recorded | — | — | 9 | 38 | 0 | 319,525 | 11,028 | 1 |
| M0: current-state UX inventory | sonnet-5 | 09-11T17:29 | 324 | not recorded | — | — | 31 | 52 | 0 | 3,700,034 | 4,435 | 1 |
| S1: /specify Addendum C perspectives | opus-5 | 09-11T17:36 | 4,007 | not recorded | — | — | 150 | 145 | 8 | 42,593,569 | 49,305 | 33 |
| Investigate: composer accepts no typing | opus-5 | 09-11T17:44 | 2,500 | 2,399 | — | — | 144 | 143 | 0 | 34,215,514 | 41,263 | 20 |
| Investigate: contrast recurrence, census gate | opus-5 | 09-11T18:08 | 2,960 | 2,655 | — | — | 155 | 154 | 1 | 43,085,699 | 75,475 | 26 |
| Implement INV-0007 phases 1–4 | opus-5 | 09-11T18:28 | 3,822 | 3,519 | — | — | 182 | 181 | 3 | 46,625,420 | 43,017 | 27 |
| TA: confirm N1–N3 on Addendum C | opus-5 | 09-11T18:47 | 223 | not recorded | — | — | 12 | 11 | 0 | 753,318 | 7,607 | 1 |
| Simplifier: confirm (a),(b) on Addendum C | sonnet-5 | 09-11T18:47 | 142 | not recorded | — | — | 12 | 23 | 0 | 830,814 | 4,187 | 0 |
| TA: re-confirm the five substitutions | opus-5 | 09-11T18:52 | 143 | not recorded | — | — | 5 | 9 | 0 | 273,967 | 3,672 | 1 |
| Owner: file PR-A/PR-B, rule §R rows | fable-5-1 | 09-11T18:53 | 253 | not recorded | — | — | 7 | 42 | 0 | 231,357 | 17,269 | 2 |
| D1: /ui-design the perspectives UX | opus-5 | 09-11T19:03 | 4,967 | 4,812 | — | — | 200 | 198 | 3 | 95,750,272 | 33,377 | 54 |
| Implement contrast phases 1–5 | opus-5 | 09-11T19:15 | 3,373 | 3,131 | — | — | 201 | 200 | 2 | 53,054,112 | 51,584 | 30 |
| S2: /specify Addendum D compile step | opus-5 | 09-11T20:08 | 65,516 | 4,992 | — | — | 154 | 147 | 12 | 51,490,033 | 55,610 | 55 |
| Owner: file Rulings 64–70, rule F-2/F-3 | fable-5-1 | 09-11T21:40 | 357 | not recorded | — | — | 6 | 45 | 0 | 222,600 | 1,615 | 1 |
| Ruling 71: pin Bash off on the lane | opus-5 | 09-11T21:50 | 2,216 | 1,565 | — | — | 126 | 127 | 1 | 19,316,416 | 40,592 | 13 |
| A1: /define-architecture C + D | opus-5 | 09-11T21:51 | 6,143 | 5,826 | — | — | 225 | 216 | 11 | 111,454,488 | 35,105 | 80 |
| D2: /ui-design elevate the session | opus-5 | 09-11T21:52 | 5,281 | 5,135 | — | — | 211 | 210 | 3 | 85,475,197 | 46,696 | 52 |
| Ruling 66: lease from editor source only | sonnet-5 | 09-11T22:28 | 1,359 | 1,258 | — | — | 90 | 103 | 1 | 16,289,583 | 26,211 | 4 |
| Investigate: new session doc never renders | opus-5 | 09-11T22:38 | 2,638 | 2,354 | — | — | 155 | 154 | 0 | 35,304,349 | 36,940 | 21 |
| Implement INV-0009 phases 1,2,2b,3,4 | opus-5 | 09-11T23:24 | 4,061 | 3,874 | — | — | 234 | 233 | 2 | 65,085,071 | 40,402 | 36 |
| P1: /prepare-for-coordination C + D | opus-5 | 09-11T23:38 | 2,172 | 1,964 | — | — | 100 | 99 | 3 | 18,468,434 | 27,239 | 12 |
| Owner: D2/A1 findings and open questions | fable-5-1 | 09-11T23:39 | 503 | not recorded | — | — | 11 | 63 | 0 | 682,774 | 15,661 | 3 |
| Apply errata E1–E7 and Rulings 74–78 | opus-5 | 09-11T23:49 | 1,520 | 1,320 | — | — | 131 | 130 | 0 | 25,221,291 | 16,666 | 15 |
| S2: settings fields and sentinels | sonnet-5 | 09-12T00:27 | 2,229 | 1,960 | 1,500 | 1.49 | 119 | 122 | 0 | 22,554,176 | 36,499 | 6 |
| DS-1: /design-slice thread ItemsControl | opus-5 | 09-12T00:28 | 6,359 | 6,188 | — | — | 161 | 153 | 12 | 58,646,634 | 63,629 | 64 |
| SH-1: registry, allow-lists, derived menu | opus-5 | 09-12T13:35 | 7,881 | 7,065 | 2,945 | 2.68 | 252 | 251 | 3 | 106,412,140 | 44,071 | 65 |
| CV-0: the read-only turn | opus-5 | 09-12T13:36 | 5,621 | 5,344 | 2,945 | 1.91 | 245 | 244 | 3 | 98,616,234 | 55,407 | 58 |
| Investigate: terminal hosts not cleaned up (5th) | opus-5 | 09-12T13:38 | 2,395 | 2,117 | — | — | 128 | 127 | 0 | 23,576,206 | 26,633 | 14 |
| Implement INV-0010 slices 1–4 | opus-5 | 09-12T14:21 | 4,221 | 2,054 | — | — | 227 | 226 | 2 | 69,730,093 | 27,666 | 39 |
| CV-1: the composer as a conversation | opus-5 | 09-12T15:18 | 13,313 | 13,061 | 4,812 | 2.77 | 608 | 604 | 6 | 289,756,433 | 106,532 | 168 |
| X-1: census controls | sonnet-5 | 09-12T15:41 | 5,334 | 3,770 | 2,945 | 1.81 | 241 | 240 | 0 | 70,887,972 | 71,133 | 16 |
| UX&A: confirm SH-1's prescribed fix | sonnet-5 | 09-12T15:47 | 59 | not recorded | — | — | 8 | 11 | 0 | 292,550 | 1,027 | 0 |
| SH-2: second host, presenter, slots, rail | opus-5 | 09-12T15:58 | 6,741 | 6,392 | 4,127 | 1.63 | 345 | 344 | 8 | 182,637,650 | 26,759 | 97 |
| UX&A: confirm SH-2's NEW-1 fix | sonnet-5 | 09-12T17:52 | 126 | not recorded | — | — | 7 | 10 | 0 | 243,731 | 39 | 0 |
| SH-3: Coding default, Evidence pair, Architecture content | sonnet-5 | 09-12T18:07 | 6,960 | 5,002 | 2,945 | 2.36 | 381 | 380 | 2 | 160,850,354 | 102,552 | 39 |
| CV-2: mechanical compile, envelope store, Prepare | opus-5 | 09-12T20:01 | 7,326 | 7,064 | 4,127 | 1.78 | 327 | 326 | 4 | 173,927,859 | 58,992 | 94 |
| Owner ruling: operator's five UI findings | fable-5-1 | 09-13T16:37 | 423 | not recorded | — | — | 10 | 54 | 0 | 1,321,300 | 19,212 | 4 |
| X-3: Shell-lane seams and two status-bar defects | sonnet-5 | 09-13T16:39 | 6,930 | 1,690 | — | — | 457 | 459 | 2 | 182,499,739 | 90,867 | 39 |
| D3: /ui-design elevate on the operator's findings | opus-5 | 09-13T16:49 | 5,277 | 4,504 | — | — | 246 | 244 | 3 | 100,386,926 | 40,911 | 57 |
| PD-5 prep: the compile-session pin spike harness | sonnet-5 | 09-13T17:02 | 2,002 | 1,375 | 1,500 | 1.33 | 148 | 163 | 0 | 32,746,327 | 41,792 | 8 |
| Owner ruling: D3's two open questions | fable-5-1 | 09-13T18:19 | 171 | not recorded | — | — | 9 | 19 | 0 | 317,788 | 2,918 | 1 |
| SH-4.1: the Coordination perspective (host C) | opus-5 | 09-13T18:20 | 15,498 | 5,843 | — | — | 352 | 347 | 3 | 147,258,370 | 40,678 | 95 |
| CV-5.2: Coalesce — the Console's message grain | opus-5 | 09-13T18:21 | 3,286 | 3,074 | — | — | 164 | 163 | 3 | 40,915,834 | 31,844 | 24 |
| CV-3: the compile call, the pin, gate 1, the eval harness | opus-5 | 09-13T18:44 | 5,562 | 4,849 | 4,127 | 1.35 | 242 | 241 | 3 | 103,252,837 | 60,193 | 58 |
| CV-5.3: the conversation — thought row, items, rendering | opus-5 | 09-13T19:26 | 5,291 | 5,021 | — | — | 210 | 209 | 3 | 73,912,610 | 72,423 | 42 |
| Security loop 2 on CV-3's pin | opus-5 | 09-13T20:18 | 268 | not recorded | — | — | 17 | 18 | 0 | 1,537,422 | 9,833 | 2 |
| CV-5.4: the editor's rest (Ruling 80) | opus-5 | 09-13T21:10 | 3,777 | 3,147 | — | — | 181 | 178 | 2 | 48,809,155 | 37,371 | 33 |
| CV-4: admission's code — gate 2 reader, ring, drift | sonnet-5 | 09-13T21:11 | 3,148 | not recorded | 2,945 | 1.07 | 141 | 150 | 0 | 37,500,413 | 48,880 | 9 |
| SH-4.2: Coding's re-cut, the left dock, the reconcile fix | opus-5 | 09-13T22:51 | 7,253 | 6,701 | — | — | 420 | 419 | 5 | 194,233,218 | 56,154 | 109 |
| Owner: ratify the Console toggle deviation | fable-5-1 | 09-14T01:02 | 47 | not recorded | — | — | 5 | 10 | 0 | 100,277 | 2,744 | 1 |
| /session-profiler over the Addenda C/D programme | opus-5 | 09-14T01:04 | 289 | not recorded | — | — | 33 | 33 | 0 | 2,755,806 | 4,865 | 2 |

**Planned vs actual.** Store-span basis over the 11 planned nodes: CV-4 1.07, PD-5 prep 1.33, CV-3 1.35, S2 1.49, SH-2 1.63, CV-2 1.78, X-1 1.81, CV-0 1.91, SH-3 2.36, SH-1 2.68, CV-1 2.77 → **median 1.78×**. Audit-duration basis: PD-5 prep 0.92, CV-3 1.17, X-1 1.28, S2 1.31, SH-2 1.55, SH-3 1.70, CV-2 1.71, CV-0 1.81, SH-1 2.40, CV-1 2.71 → median 1.62×. The ledger's own eight rows (S2, CV-0, SH-1, X-1, SH-2, CV-1, SH-3, CV-2) give **median 1.76×**; W1–W3's nine planned nodes (adding DS-1 6,188/2,500 and PD-5 1,375/1,500, SH-3 by audit) give 1.71×. **The ledger's headline "W1–W3 median ratio is 2.2×" is not derivable from its rows** (AC-08). Nodes above 2× by any basis: CV-1 (2.7–2.8×), SH-1 (2.4–2.7×), DS-1 (2.5×), SH-3 (1.7× by audit, 2.4× by store span). Only PD-5 came in under plan (0.9×).

**Store span vs audit duration.** The two agree within 70–820 s for every node that ran once (the grounding before `start` plus the report after `append`). Six nodes were **resumed** via `SendMessage` and their audit duration stops at the first run: S2 `/specify` (+60,524 s: a 20 s answer 18 h later), SH-4.1 (+9,655 s: 8,143 s of it a blocked `EnterWorktree`, AC-03), X-3 (+5,240 s: the review fixes, whose entry has no duration), INV-0010 (+2,167 s: slice 0, marker consumed), SH-3 (+1,958 s), X-1 (+1,564 s) — AC-09.

**Width actually used vs the cap of 3** (writing nodes, spans split at > 30 min gaps): max **4** at 2026-09-11T22:38Z for 752 s (the plan records one raise to 4); time-weighted while ≥ 1 node was live (89,927 s): width 1 = 39.3%, width 2 = 31.7%, width 3 = 28.1%, width 4 = 0.8%; mean **1.9**. The Conversation lane's serial spine (CV-0 → CV-1 → CV-2 → CV-3 → CV-4) held the width down, as the plan predicted.

**EnterWorktree waits (both refused with "the current working directory C:\projects\ai-de is the repository root"):** X-1: census  379 s at 2026-09-12T15:42:20Z; SH-4.1: the  8,143 s at 2026-09-13T18:21:06Z — 8,522 s in total.

## The operator's questions (IO2)

| question | answer |
|---|---|
| cost and tokens per node and per join | per node: the table above (est. $1–$168; CV-1 $168 at 289.8M cache-read tokens is the largest; the seven Owner rulings on Fable total $12). Per join: **not recorded** as a figure — the 14 join entries carry no duration and no token count; measured from the transcript, the joins + recounts cost 13,451 s of main-line Bash and the main line's tokens in those turns are not separable per join without the turn table (the joins are the `<task-notification>` turns 43–88; their cache-read alone is ~217,849,021 tokens ≈ $109 est.). |
| the cache share | main line 98.95%; sub-agents 98.09% (input_tokens uncached: 2,098 main, 22,824 sub-agents) — the cost *is* the cache-read of a 400k–966k prefix on every request |
| joins vs dispatch/review on the conductor's wall | joins + gates/recounts = **13,451 s = 44.8% of the 30,032 s active main line**; all Bash 53.0%; model-side (briefs, dispatch, reading reports) 47.0%. The 14 whole-suite recounts are 20 `--update` calls ≥ 200 s (some joins recounted twice) at median 6.5 min — 3 runs each (whole + both Core halves) inside one call. |
| rework passes | the CV-4 + X-5 join recounted twice (453 s, then 452 s after the merge was amended with the resolved register); INV-0010's slice-0 correction (marker consumed, no duration); X-3's review fixes (no marker); the hung App run (turn 62: 2,899 s wall, `Run the test that hung, alone` at 2026-09-12T18:57Z, DC-165); the runaway spike (PD-5 run 2 aborted at 6 min, DC-185; run 3 green 2026-09-13T20:37Z); the S2 join's three re-baselines (221 + 103 + 335 s). Nodes ran 726 test invocations in-tree (19,909 s): CV-1 76, SH-4.2 51, CV-2 50, SH-2 49, CV-3 42, CV-5.3 40 — red-first singles and whole-suite gates mixed. |
| width used vs cap 3 | mean 1.9; width 3 for 28.1% of the live time, 4 once for 752 s (the recorded raise); see above |
| nodes over their planned seconds | every planned node except PD-5 (0.9×): CV-4 1.1×, PD-5 prep 1.3×, CV-3 1.3×, S2 1.5×, SH-2 1.6×, CV-2 1.8×, X-1 1.8×, CV-0 1.9×, SH-3 2.4×, SH-1 2.7×, CV-1 2.8× (store-span basis; audit basis 0.1–0.4 lower). Median 1.78× (store) / 1.75× (audit, the ledger's rows) — **not 2.2×**. |
| the operator's five findings (2026-09-13 09:27–09:33 local = 16:27–16:33Z; the message at 16:35:57Z) | **answered in code within the day**: Owner ruling (Fable) 16:37:59–16:45:02Z → Rulings 80–89; D3 `/ui-design` 16:49–18:17Z (5,277 s, $57 est.) → the ranked plan; CV-5.2 joined 19:23Z, CV-5.3 21:03Z, CV-5.4 22:22Z, SH-4.1 22:47Z, SH-4.2 **01:00Z (18:00 local)** — 8 h 24 min after the message. The findings-driven nodes (Owner ruling, D3, CV-5.2/5.3/5.4, SH-4.1/4.2) cost **$364 est.**; the whole window 16:35Z→01:10Z (15 nodes + 24 reviews + the main line) **$631 est.** (sub-agents $536 + main $95). Not met: the visual rows of D3's O-1…O-11 on the final build are the operator's attended rows (the plan's Stage 10 close). |
| repeat-mistake shapes | **DC-113** (a gate run piped into `tail`/`head`/`grep` with no `pipefail`): 168 main-line lines, **102 of them also `git commit`/`merge`/`push` on the same line**; 271 in nodes (21 with a commit); the register recorded 4 consequences; the join script (22:25Z) covers the join line only and the shape recurred at 00:52Z — and in this profiler run (`run-verify-gates.py \| tail -45` reported exit 0 over "5 of 35 FAILED"). **Mangled heredocs**: 70 failed heredoc runs (2 main, 68 across 28 nodes, max 5 per node) + 27 that ran with a `SyntaxWarning` — ≈ 70 burned requests, ≈ $20 est. **`DC-nnn` allocation drift**: 38 main-line calls handled placeholders/allocation; 20 of the 31 `COORD-REFUSED` decisions in the window were on `docs/lessons/defect-classes.md` inside 45 min (2026-09-12T17:09–17:54Z: cv-1, sh-2, conductor) plus 1 from the Copilot fleet at 14:21Z (DC-163). **Console encoding**: 3 main + 13 node `UnicodeEncodeError`/`charmap` results (pack finding #1). |

## Findings

| id | severity | confidence | finding | session | evidence | fix |
|---|---|---|---|---|---|---|
| AC-01 | Major | Verified | **Sub-agent telemetry is invisible to the profiler.** `session-profile.py` reads only `isSidechain` records in the main transcript; this harness version writes sub-agents to `<session>/subagents/agent-*.jsonl` (+ `.meta.json`). sp-0002 recorded 0 sub-agent requests; the store holds 166 agents, 10,606 requests, 3,226,828,964 cache-read tokens, est. $1,891 — **86% of the programme's cost** — and SP-01/SP-07 never ran over a node. | claude:919ba21f | `profile_claude()` lines 808–950 (`sub_requests` from `isSidechain` only); sp-0002 turn table `subs` column vs the store | F-18 |
| AC-02 | Major | Verified | **A `<task-notification>` wake-up is counted as a human turn.** 50 of the conductor's 89 profiler turns are notification records; SP-09 (no goal state) and SP-06 (fan-out without tier) fired on them, and the family table's `no_goal` = 167 is mostly this. | claude:919ba21f, claude:18fe7a5a | sp-0002 turns 5, 11–16, 18, 20–22 …; `profile_claude()` `is_human = … or isinstance(content, str)` | F-19 |
| AC-03 | Major | Verified | **A deferred host tool called from a background node blocks until the main line surfaces it, then refuses.** `EnterWorktree` from SH-4.1 waited **8,143 s** (18:21:06 → 20:36:49Z) and from X-1 379 s; both results: "Cannot enter worktree: the cwd is the repository root". 8,522 s of node wall on a call that could never succeed; the audit marker, set after, hides it. | SH-4.1, X-1 | `agent-<id>.jsonl` tool_use → tool_result timestamps; the refusal text | F-25 |
| AC-04 | Major | Verified | **The DC-113 shape is ~25–100× its recorded recurrences.** 168 main-line lines pipe a gate run into `tail`/`head`/`grep` with no `pipefail`; **102 also commit/merge/push on that line**; nodes 271/21; the register counts 4 (the times a red was hidden). The join script covers the join line only; the shape recurred after it landed and inside this profiler run. | claude:919ba21f + 28 nodes | regex over Bash `command` fields (`verify-*.py … \| tail`, no `pipefail`) | F-20 |
| AC-05 | Minor | Verified | **A multi-line program passed through a shell heredoc fails on quoting and burns the request.** 70 failed heredoc runs (2 main, 68 in 28 nodes, max 5/node: `unexpected EOF`, `SyntaxError: unterminated string`, `invalid escape`) + 27 warned runs. | 28 nodes | tool_result text over `<<` commands | F-21 |
| AC-06 | Major | Verified | **Whole-suite recounts are the conductor's largest cost centre and are not measured by the join.** Joins + gates/recounts = 13,451 s = **44.8%** of the active main line; 20 whole-suite `--update` recounts at median 6.5 min (the plan's "~5 min"); nodes ran 726 test invocations (5.5 h) before each join re-ran the whole suite; two joins recounted twice. No join entry records `recount_seconds`. | claude:919ba21f | Bash timings; `join-*` entries | F-22 (measure), then a CE ring decision for the human |
| AC-07 | Major | Verified | **The 14 join entries carry `goal`/`done_when` but no `tier`, no `fan_out`, no `duration_seconds`**, and `audit-log.py selfcheck` passes them (it checks `done_when` only). The joins are the one activity whose cost this profile could not read from the audit log. | claude-conductor-addendum-c | `join-cv1-x2` … `join-converge-docs` fields | F-22 |
| AC-08 | Minor | Verified | **The ledger's headline "W1–W3 median ratio is 2.2×" is not derivable from its rows** — the rows' median is 1.76× (the ledger's eight), 1.71× (W1–W3's nine), 1.78× (store spans, eleven). DC-184's shape (a figure in prose that no literal produces); the next plan's multiplier is the decision it changes. | coordination-addendum-cd | §Execution ledger rows recomputed | F-23 |
| AC-09 | Minor | Verified | **A resumed node's second run sets no marker.** Six nodes were resumed by `SendMessage`; their audit durations stop at the first run (Δ 1,564–60,524 s vs store span); X-3's review-fix entry and INV-0010's slice-0 entry read no duration. DC-190's sibling: "one marker measures one run" holds, and the resume is a run nobody marked. | S2, SH-4.1, X-3, INV-0010, SH-3, X-1 | store span − audit duration | F-24 |
| AC-10 | Major | Verified | **Node context has no ceiling.** 18 of 52 nodes exceeded 500k tokens; CV-1 reached 966k and compacted once; a request at 800k context bills ~400k cache-read tokens (~$0.20–0.40 est.); nodes' cache reads = 2,980,547,957 tokens = est. $1,490 = **68% of the programme**. CTX-A's shape inside sub-agents. | 18 nodes | usage rows per request | F-14 (extended to a per-node context ceiling) |
| AC-11 | Major | Verified | **SP-15 (peer fleet): two fleets with the primary checkout as harness cwd for 35.5 h.** `copilot:45bbc625` (gpt-6-astra; 13 turns, t12 = 727 requests, 19 sub-agents, 102,723 s, 133,199 AIU) ran 2026-09-12T13:38Z → 09-14T01:06Z with cwd `C:\projects\ai-de` beside the conductor (cwd the same; joins in the primary by plan). The register was contended across fleets (1 peer refusal + 20 of ours in 45 min); 40 `atlas/*` worktrees stand beside ours; `coord metrics`: 0 of 88 coordination sessions started in the primary. | claude:919ba21f × copilot:45bbc625 | sp-0002 SP-15; `.agents/decisions`; `git worktree list`; `coord session list` | pack findings #5, #17 (confirmed; no new control) |
| AC-12 | Minor | Verified | **Declared budgets are not measurements.** CV-1's entry declares `main_calls: 700`; the store counts 604 tool calls. SH-1 and CV-0 each slept 601 s on a sentinel file waiting for reviewers whose notifications the harness delivers anyway. | cv-1, sh-1, cv-0 | audit entry vs store; Bash `Block until …` calls | pack finding #16 (confirmed, with the delta) |
| SP-01 | Major | Verified (re-measured) | Context accretion on the main line: one 56 h session, one compaction at 966,688, context ≥ 600k for 60 of 89 turns; est. $250 of the main line's $308 is cache reads. | claude:919ba21f | sp-0002 | F-09 (WT1a: a session per wave) |
| SP-07 | Major | Verified | Sub-agent runaway by the catalog's bar: CV-1 608 requests / 604 tool calls / 13,313 s / 6 reviews / est. $168; SH-4.2 420 / 419 / 7,253 s; X-3 (sonnet) 457 / 459. Each stayed inside its declared call budget (6,000); the bar the catalog uses (a delegation past a sane budget) and the plan's bar disagree by 10×. | CV-1, SH-4.2, X-3 | subagents store | F-04 + F-14 ext. (a token/context budget beside the call budget) |

### Struck (with the reason)

| id | raised on | struck because |
|---|---|---|
| SP-09 (no goal state) | claude:919ba21f t5–t88 (and 18fe7a5a) | 50 of the flagged turns are `<task-notification>` wake-ups (AC-02); for the operator turns, the substantive ones have audit entries carrying `goal`/`done_when`/`tier`/`fan_out` (the same basis as `profile-conductor-phase1`'s strike). What survives is narrowed into AC-07 (the join entries). |
| SP-06 (council above tier) | t22, t40, t77 | each is a wave dispatch (width 3) declared in the plan's fan-out contract and in every node entry's `fan_out: 3`; the "first reply" lacks `Tier:` because the turn is a notification wake-up. Artifact of AC-02; the profiler cannot read the plan's contract. |
| SP-14 (family gap: openai/copilot 10.44 vs anthropic/copilot 0.67 drift/turn) | *:* | the turn mix is not comparable (the 3 anthropic/copilot turns are 1–5-request turns; the 9 openai turns average 126 requests); the ≥ 3 comparable turns per family bar is not met. Kept as data for the peer fleet's own pass. |
| SP-15 claude:919ba21f × claude:3f8dad7c | 2026-09-13T19:30Z | `3f8dad7c` is one prompt (a logic puzzle, CV-5.3's thought-frame probe) with no assistant request and no write. |
| SP-12 (images in the main context) | t64–t69 | the five screenshots are the operator's findings — the evidence, not drift. |
| SP-17 (reasoning visibility 44%%) | claude:919ba21f | re-measured; F-12 is already applied; changes nothing. |
| two audit session ids for one conductor (`conductor-addendum-c` / `claude-conductor-addendum-c`) | audit log | a recall inconvenience (`auditlog --session` splits the actor); no control the profiler can hold; noted in the coordination notes. |
| the 33 `coord metrics` refusals / 99.3%% edits under a lease | `.agents` | the layer worked as designed; the refusals that matter are the register cluster, carried by AC-11 and pack finding #5. |

## Fixes (each with the pack surface and the control that fails on recurrence)

| fix | what | where in the pack | control that fails on recurrence (how it was seen red) | closes |
|---|---|---|---|---|
| F-18 (new) | The Claude reader walks `<session>/subagents/agent-*.jsonl` + `.meta.json`; each agent becomes a node row (model from usage, requests, tokens, tool calls, tool waits, ctx max, span, resumes); SP-01/SP-07 run per node | `scripts/session-profile.py` `profile_claude()`; `docs/design/session-profiler.md` | a fixture session with a `subagents/` dir: the profile's `sub_requests` must equal the store's; SP-07 must fire on a 608-request node — red today (sp-0002 reports 0 sub-agent requests over 10,606) | AC-01, AC-10, SP-07 |
| F-19 (new) | A user record whose text starts `<task-notification>`, `<local-command-…>` or `<command-name>` is a continuation, not a turn; goal-state/tier detectors read only operator turns | `scripts/session-profile.py`; the family table's `no_goal` | a fixture with a notification record creates no turn; SP-09 stays silent on it — red today (49 of 89 turns) | AC-02, SP-09, SP-06 |
| F-20 (new) | A shell rule in the managed block: *a gate's exit status is never behind a pipe* — `set -o pipefail` or the gate on its own line; profiler SP-24 counts gate-run-piped lines per session and the subset that commit/merge/push | `adapters/managed-blocks/*.block.md` (CT26's neighbour); `scripts/session-profile.py`; `execute-with-coordination` | SP-24 red on this corpus (168/102); DC-113's recurrence count moves from 4 to the measured shape | AC-04 |
| F-21 (new) | Brief rule: a program longer than one line is written to the scratchpad with Write and run as a file — never a heredoc; profiler SP-25 counts failed heredoc runs | node brief template (`execute-with-coordination`); `scripts/session-profile.py` | SP-25 red on this corpus (70) | AC-05 |
| F-22 (new) | `conductor-join.py` sets the marker at step 1, appends with `--tier T1 --fan-out 0` and the measured `recount_seconds`; `audit-log.py selfcheck` flags a `kind:skill` entry with `done_when` but no `tier` | `tools/conductor-join.py` (to lift into the pack); `scripts/audit-log.py selfcheck` | selfcheck red on the 14 join entries today | AC-06 (measure), AC-07 |
| F-23 (new) | The ledger's ratio line is derived from its rows by a script (`verify-coordination-ledger.py`: recompute the median, compare to the prose) | `prepare-for-coordination` / `execute-with-coordination` Stage 10 | the verify script red on today's ledger (2.2 vs 1.75) | AC-08 (DC-184 recurrence) |
| F-24 (new) | A resume message carries the `start` line; profiler SP-26 flags a node whose store span exceeds its audit duration by > 30 min | brief/resume template; `scripts/session-profile.py` | SP-26 red on six nodes today | AC-09 (DC-190 sibling) |
| F-25 (new) | Node briefs: never call `EnterWorktree`/`ExitWorktree` — absolute paths only (the tool refuses when the session cwd is the primary); profiler SP-23 flags a sub-agent tool wait > 600 s on a non-shell tool | brief template; `scripts/session-profile.py` | SP-23 red on SH-4.1 (8,143 s) | AC-03 |
| F-14 (extended) | The goal state's `Main-line budget` gains a **context ceiling** (e.g. 400k tokens) and a hand-off rule (split the slice or `/compact` at the ceiling); the audit entry records `ctx_max` | `knowledge/communication-and-task-discipline.md` CT19; `scripts/audit-log.py`; GO7 | SP-01 per node (needs F-18) red on 18 nodes | AC-10, SP-07 |
| F-09 (existing) | A session per wave, not per programme: the conductor ran 56 h in one session with one compaction at 966k | `knowledge/session-worktree-discipline.md` WT1a | SP-01 on the main line (re-flagged) | SP-01 |
| pack findings #1, #2, #5, #6, #16, #17 | **confirmed** with counts: #1 (3 + 13 encoding errors), #2 (this run: the script's own `append` consumed the skill's marker at 01:07:40Z, so the closing entry's duration starts from a re-mark at 01:07:56Z), #5 (20 register refusals in 45 min, 3 sessions + the peer), #6 (168/102 — the script covers the join line only, hence F-20), #16 (700 declared vs 604 measured), #17 (40 atlas trees; 35.5 h cwd overlap). None struck. | `docs/notes/pack-findings-addendum-cd.md` | as each row names | AC-04, AC-09, AC-11, AC-12 |

## Model family × harness (the tuning view)

*From `session-profile.py compare` over the window (3 Claude Code sessions, 1 Copilot). `drift/turn` = sub-agents + re-reads + skill repeats + missing goal state + fan-out without tier + converge nudges + cap firings; for anthropic/claude the `no_goal` term (167) is inflated by AC-02.*

| family | harness | turns | req/turn | cache-read/turn | out/turn | reasoning/turn | reasoning visible | effort | intent trace | cost/turn | ttft p90 (median) | ctx end (median) | wall s/turn | drift/turn |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| anthropic | claude | 209 | 9.5 | 4,538,808 | 10,881 | 2,897 | 44.9% | not recorded | 100.0% | not recorded (est. $308 over 999 main-line requests of 919ba21f) | not recorded | 630,872 | 258 | 1.47 (sub_agents 132, no_goal 167†, no_tier_with_fanout 8†) |
| anthropic | copilot | 3 | 3.3 | 360,872 | 3,656 | 1,728 | 39.7% | high | 100.0% | 128.0 AIU | 15.2 | 170,866 | 221 | 0.67 |
| openai | copilot | 9 | 126.0 | 82,763,921 | 389,676 | 226,701 | 0.8% | high | 100.0% | 21,705 AIU | 80.8 | 362,533 | 13,990 | 10.44 (rereads 44, sub_agents 41, skill repeats 2) |
| anthropic sub-agents (this fleet, not in the script's view) | claude | 166 agents | 159 req/agent (depth 1) · 20 (depth 2) | 57,318,230 per depth-1 agent | 36,067 per depth-1 agent | not recorded per agent | — | not recorded | 98.1% / 96.8% | est. $31 / $2 per agent (depth 1 / 2) | not recorded | median max ctx 392,309 | 3,054 s median span (depth 1) | see AC-03/05/09 |

† inflated by AC-02 (notification turns).

**Per-family tuning notes.**

- **anthropic / Claude Code (this fleet).** (1) The bill is the prefix: 98–99% of input is cache-read of a 400k–966k context; the levers are session hygiene on the main line (WT1a — a session per wave; F-09) and a context ceiling per node (F-14 ext.). (2) The family writes the DC-113 shell shape habitually (168 main / 271 nodes) and it survives the join script — a rule in the managed block plus SP-24 (F-20). (3) Heredoc programs fail 70 times in nodes — write-then-run (F-21). (4) Nodes call a deferred host tool that refuses in this configuration and wait hours for the refusal (F-25). (5) Reasoning visibility 44.9% — F-12 applied; every text-derived drift judgement above stays Inferred where it rests on the summary. (6) Effort is **not recorded** by this harness; whether the mechanical closes (joins) ran at reasoning prices (SP-22) cannot be read.
- **anthropic / Copilot.** 3 turns, all trivial — no tuning claim.
- **openai / Copilot (the peer Atlas fleet).** Reported, not tuned here: 126 requests/turn, 44 re-reads, a 727-request turn with 19 sub-agents over 28.5 h, delegations at 1.2–2.7M tokens (SP-07), 0.8% reasoning visible. The peer's own profiler pass owns it; the only shared surface is `.agents/` (AC-11).

## Coordination notes

- **Overlaps.** Both fleets' harness sessions have cwd `C:\projects\ai-de` (the primary); the conductor's joins ran in the primary by the plan's order of operations (WT4's recorded exception); `coord metrics` reports 0 of 88 coordination sessions started in the primary (24 predate the field). The peer's coordination session `copilot-atlas-fleet-45bb` is registered in `C:/Projects/ai-de-conductor-code-atlas`. Reconciled against `git worktree list`: 40 `atlas/*` trees + the conductor's remaining trees + this profiler's; none touched.
- **Contention.** 31 `COORD-REFUSED` in the window; 20 on the register in 45 min across cv-1, sh-2 and the conductor, 1 from the peer on the register, 1 from the peer on `AtlasIdentity.cs`; the rest on test files held by a sibling node (sh-2 × 6, cv-1 × 5). DC-163's class; pack finding #5's control (a register-class path is never claimable) closes the 21.
- **Identifiers.** Three id systems that do not cross-reference: the harness session (`919ba21f`), the audit session (`conductor-addendum-c` then `claude-conductor-addendum-c`), and the node briefs' names (`CV-1`), joined here by hand through the Agent tool's `description` and the entry's `shortname` — the same gap `profile-conductor-phase1` §Scope note recorded.
- **This run's own hygiene.** The profiler ran in its own worktree (`side/session-profile-addendum-cd`), claimed nothing, and read the primary's stores; the script's audit append consumed the skill's `start` marker (pack finding #2, re-observed), so the closing entry's duration is from the re-mark at 2026-09-14T01:07:56Z and the script's entry carries the first 181 s. It piped a gate run into `tail` once (AC-04, on itself) and read the state instead of the exit code; a heredoc turned `\a` in a register path into a bell character (AC-05, on itself) — caught by a grep for the path, repaired by a script written to a file and run (F-21's rule).

## What this profile could not measure, and why

| measure | status | why |
|---|---|---|
| cost in USD | est. only | Claude Code stores no `cost-state` for these sessions; the figures are token counts at first-party list rates and the operator is on a subscription |
| per-join tokens and seconds | not recorded | the join entries carry no marker and no recount figure (AC-07); the joins are notification turns whose tokens the turn table holds but which also carry dispatch work |
| effort per request | not recorded | the harness writes `effort` on assistant records but the profiler does not surface it; SP-22 cannot be answered |
| `subagent_tokens` / `duration_ms` in task notifications | not recorded | this harness version's notification carries status/summary/result only |
| the peer fleet's writes in the primary | not read | the Copilot transcript is the peer's; only its `session-profile.py` view and the `.agents` decisions were used |
| why `EnterWorktree` blocked 2 h 15 min | not recorded | the store shows the wait and the refusal, not the queue it sat in (a permission surface is the Inferred cause; label Inferred) |

## Register

Three placeholder classes are appended to `docs/lessons/defect-classes.md` as `DC-nnn (profiler a|b|c)` for the join to allocate: **a** the deferred-tool block-then-refuse (AC-03), **b** the heredoc burn (AC-05), **c** the unmarked resume (AC-09). Recurrence notes for DC-113 (the count, AC-04) and DC-184 (the ledger ratio, AC-08) are recorded here for the conductor to fold into those entries; CTX-A is re-flagged inside sub-agents (AC-10).

## Status

| Completed | Remaining | Best next action |
|---|---|---|
| `discover` → 4 sessions named; `profile` → sp-0002 (27 script findings) + this curated pass; `compare` run; every Inferred row confirmed or struck against the transcript; 166 sub-agents read from the store the script cannot see; the operator's eight questions answered with numbers or "not recorded"; 12 findings, 8 struck, 10 fixes each with a control; 3 placeholder classes; PROFILES.md row | the pack changes (F-18 … F-25, F-14 ext.) are proposals — `/extendaibundle` for the profiler and the managed block, the conductor for `conductor-join.py`; the CE ring decision on the node/join double whole-suite run is the human's | `/dream` over `docs/profiles/` (this file + sp-0002) to promote F-18/F-19/F-20 into controls; then `/extendaibundle` for the subagents reader — every later profile of a Claude Code conductor is blind without it |
