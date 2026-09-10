---
id: profile-conductor-phase1
title: "Session profile — Conductor Phase 1"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [profile, session-profiler, conductor, phase-1, efficiency, adherence]
links:
  - { to: design-session-profiler, rel: relates-to }
  - { to: profile-sp-0001, rel: depends-on }
  - { to: plan-conductor-programme, rel: relates-to }
  - { to: note-conductor-phase1-e18-close, rel: relates-to }
review-by: 2027-03-09
review-suggested: []
summary: >-
  A /session-profiler pass scoped to Conductor Phase 1, joining the audit log's per-node
  measurements (the only Phase-1-specific ground truth) against the harness-level telemetry in
  profile-sp-0001. Three of eight plan nodes carry a measured duration; the other five, and every
  piece of orchestration overhead around them, read "not recorded." 35.6% of the Phase-1 wall-clock
  window is inside a measured node; 64.4% is not.
---

# Session profile — Conductor Phase 1

*A `/session-profiler` pass over this repo's Phase-1 work (the Conductor programme, N0–N7),
grounded in `docs/audit/audit-log.jsonl` and corroborated by the whole-repo harness measurement in
[`profile-sp-0001`](sp-0001/profile.md). Every number below is read from one of those two stores or
is explicitly marked "not recorded" — nothing here is modeled or estimated without a stated basis
(IO8).*

## Scope note — two identifier systems that do not cross-reference

Phase 1's work is recorded under **two separate session-identifier namespaces that never join**:

1. The **harness's own session id** (a UUID, e.g. `18fe7a5a-c1b6-434e-8033-3f0c4e841f24`) — what
   `session-profile.py` reads from `~/.claude`'s own store. Phase 1 ran inside exactly **one**
   harness session, `18fe7a5a`, continuously from its start through the Phase-2 docs work this
   document is itself part of.
2. The **audit log's `session` field** — a human-chosen label (`conductor-phase1`,
   `18fe7a5a-c1b6-434e-8033-3f0c4e841f24` used interchangeably by the orchestrator, and now
   `conductor-phase2-docs`). Every Phase-1 *node* entry (N1+N2, N3, N4, N5+N6, N7) is labelled
   `conductor-phase1`; every *orchestration* entry (setup, the N0 spike, the licensing escalation,
   the N1/N2 verify-and-route step, the E18 close) is labelled with the harness UUID directly.

**Consequence, stated plainly:** there is no shared key between "what the harness billed on turn N"
and "which plan node was running," beyond manually aligning timestamps. This profile does that
alignment by hand, once, below — it is not a capability either tool has on its own, and is named as
a gap in §5.

## 1. The measured node durations — ground truth from the audit log

| Node(s) | Audit entry (`shortname`) | `duration_seconds` | Evidence |
|---|---|---:|---|
| Planning (`/optimize-graph`, precedes N0) | `optimize-graph-conductor-programme` | **511** | `al-01M23QXKTKV5CB8EDSHRW1ZERB` |
| N0 — frame corpus | `spike-acp-subscription-lane` | **not recorded** | `al-01M23PFQGTS3QQQ355Y118FY8B` — no `started_at` |
| N1+N2 — envelope, engine catalog | `conductor N1+N2 envelope and engine catalog` | **not recorded** | `al-01M23SGWV17MAQ4DBPHNDBD3XV` — no `started_at` |
| N3 — plane services | `conductor-n3-plane-services` | **not recorded** | `al-01M23V95K6X8P96F4D540D03RD` — no `started_at` |
| N4 — ACP client | `conductor-n4-acp-client` | **1931** | `al-01M23XBNAFVPJYERFDH5NFB0MY` |
| N5+N6 — integration/scoring, leases/seams | `conductor N5+N6 …` | **1374** | `al-01M240YKF6X0SCFYGPNVVYYHEP` |
| N7 — headless launcher, exit evidence | `conductor-n7-exit-evidence` | **2404** | `al-01M243D3ERW7DWHV12X4DP8X59` |

**This is exactly the shape `docs/plans/conductor-programme.md`'s own "Planned vs actual" section
already reported** (measured, not modeled): *"one `start` yields a duration on exactly one
subsequent `append`"* — every entry with no `started_at` is one whose session's `start` marker was
either never called or was already consumed by an earlier `append` in the same audit session label.
**Read as "not recorded," never as zero.** This profile independently re-derived the same figures by
reading the raw JSONL rather than by trusting the plan document's own citation of them — they match.

Also present in the same window, not a plan node but real orchestration cost: `conductor-phase1-setup`
(tier T2, fan-out 4, not recorded), `verify-n1-n2-and-route-n3` (fan-out 1, not recorded),
`licensing-ruling-human` (the escalation to the human operator, not recorded),
`prepare-for-coordination-phase1` (not recorded), `dispatch-upstream-pack-fixes` (fan-out 1, a
tangential upstream-pack fix run concurrently, not recorded), `conductor-phase1-e18-close` (not
recorded).

## 2. Where the time actually went

**The Phase-1 window, read from timestamps:** first Phase-1 audit entry `2026-09-09T18:08:39Z`
(`Implement AI-DE Conductor spec v1.0 (Phase 1)`) — actually `conductor-phase1-setup` and the goal
dispatch begin at the harness turn logged `17:57:10Z` — through the E18 close at
`2026-09-09T22:48:20Z`. **Window: 4h 51m 10s (17,470 s).**

**Measured, inside that window:** optimize-graph 511 s + N4 1931 s + N5+N6 1374 s + N7 2404 s =
**6,220 s (103.7 min).**

**That is 35.6% of the window measured; 64.4% (11,250 s, ≈187.5 min) is not recorded against any
node** — spread across N0, N1+N2, N3, the setup/verify/licensing/coordination-prep steps, and the
E18 close's own reasoning. **The plan's own node table lists N0–N3 as roughly 4.25 of 10.0 nominal
"units"** (0.25+0.5+0.5+3.0 = 4.25, against 10.0 total) **— i.e. the planned model already expected
these to be a large share of the work, and the audit log confirms they ran, just not how long they
took.** Nothing here distinguishes "N0-N3 were fast but unmeasured" from "N0-N3 were the majority of
the unmeasured 187.5 minutes" — that split is **not recorded**, not inferred either way.

**N7's own internal split is known and worth restating here, because it is the sharpest number in
the phase:** the live exit run itself (spawn → handshake → prompt → close → score) took **102 s**.
N7's total measured duration is **2,404 s**. **≈95.8% of N7's time is the evidence built around a
102-second run** — the pre-declared oracle, the Proof Pack, the R4-core diff, and the full gate set
that (per the plan's own record) caught four gates that eight prior nodes had silently never run.
This is the single clearest "what was front-loaded / non-recurring vs per-node" finding available:
**verification cost dominates execution cost by roughly 23:1 at the one node it was measured.**

**Front-loaded, non-recurring (paid once, before any implementation node):** the `/optimize-graph`
planning pass (511 s, measured) plus `conductor-phase1-setup`, `spike-acp-subscription-lane` (N0),
and `prepare-for-coordination-phase1` (all not recorded). **Per-node, recurring in shape (paid once
per node, would scale with node count):** N1+N2, N3, N4, N5+N6, N7.

## 3. Findings

| id | severity | confidence | finding | session | evidence | fix |
|---|---|---|---|---|---|---|
| PP1-01 | Major | Verified | Only 3 of 8 plan nodes (N4, N5+N6, N7) carry a measured duration; N0, N1+N2, N3 and every orchestration step around them read "not recorded" because `audit-log.py append` consumes the one preceding `start`, and a session with intervening entries between its `start` and its node's own `append` loses the marker before the node it was meant for. | conductor-phase1 / 18fe7a5a | Table in §1, `docs/audit/audit-log.jsonl` entries cited directly | **new** (below) |
| PP1-02 | Major | Verified | 64.4% of the Phase-1 wall-clock window (17,470 s) is outside any measured node duration. | 18fe7a5a | §2 arithmetic, timestamps `al-01M23NR2DK9ESSP40KJ5PWSJ11` → `al-01M245Q6Y9TAVQGQ7TDTHHMK56` | F-14 (extend to node-level, not only main-line) |
| PP1-03 | Major | Verified | At N7, the one node where both figures exist, verification cost (2,404 s total) outweighs the execution it verifies (102 s exit run) by ≈23:1. | conductor-phase1 | `docs/proof/conductor-agent-plane.md` "What the exit run cost, and what it bought"; `al-01M243D3ERW7DWHV12X4DP8X59` | F-04 (delegation/phase budget should separate "do the work" from "prove the work" as distinct, named costs) |
| PP1-04 | Major | Verified | The declared "~290 of 300 tool calls" budget spend (E18 close) has no itemized counter anywhere in the audit log — no field records calls-used per node or per phase. It is a narrative estimate, not a measurement. | 18fe7a5a | `docs/notes/conductor-phase1-e18-close.md`; no `tool_calls` field in any `conductor-*` audit entry, confirmed by direct read of the JSONL | F-14 |
| SP-01 (re-confirmed) | Major | Verified | Context accretion: the harness session grew from 61,516 to 632,596+ tokens over 33 main-line turns spanning Phase 1 and into Phase 2. | claude:18fe7a5a | `profile-sp-0001` turn table, turns 0–32 | F-09, F-01 |
| SP-06 (disconfirmed for this session) | — struck — | — | "Council above tier: a fan-out on a turn declaring no tier" was raised against `claude:18fe7a5a` turns 0 and 2. **Disconfirmed:** the corresponding audit entries carry an explicit `tier`/`fan_out` (`conductor-phase1-setup`: tier T2, fan-out 4; `verify-n1-n2-and-route-n3`: fan-out 1). The goal state exists and is recorded — the profiler's text-pattern heuristic scans only the parent turn's own reply, not a dispatched sub-agent's own prompt, which is where CT19's fields actually live for a delegated node. This is a **detector-scope gap in the heuristic, not a CT19 compliance gap** in the session. | claude:18fe7a5a | `al-01M23NR2DK9ESSP40KJ5PWSJ11`, `al-01M23STE7W6R4EEN3GRV4B76T8` (tier/fan_out present) vs `profile-sp-0001` t0/t2 (heuristic found none) | **new** (below) |
| SP-09 (disconfirmed for this session) | — struck — | — | "No goal state" was raised against `claude:18fe7a5a` turns 0–2. **Disconfirmed on the same basis as SP-06 above:** every substantive Phase-1 audit entry (`conductor-n4-acp-client`, the N5+N6 entry, `conductor-n7-exit-evidence`) carries populated `goal` and `done_when` fields, quoted verbatim in the audit log. The goal state is written; it is written into the delegated node's own prompt, which the harness-turn heuristic does not parse. | claude:18fe7a5a | Same audit entries; `goal`/`done_when` fields present and populated | **new** (below) |
| SP-15 (struck) | — struck — | — | "Concurrent sessions in one checkout" flagged `claude:18fe7a5a` overlapping `claude:02e96152` and `claude:bf8a84e2`. **Struck by the Simplifier:** both overlapping sessions are single-turn, 2–3-second `git status --short` checks in the primary checkout, not concurrent editing work — no WT1/WT4 hazard, no shared mutable state at risk. | claude:18fe7a5a | `profile-sp-0001` per-session tables for `02e96152`/`bf8a84e2` — 1 turn, 2–3 s wall each | none — noise |
| SP-17 (re-confirmed) | Nit | Verified | Reasoning visibility: 96,308 reasoning tokens billed on `claude:18fe7a5a`'s main line; 156,143 chars of reasoning text on disk (~46% visible). | claude:18fe7a5a | `profile-sp-0001` | F-12 |

## 4. Fixes

| fix | what | where in the pack | control that fails on recurrence | findings it closes |
|---|---|---|---|---|
| **new** — "node-scoped start, never silently consumed" | `audit-log.py start` should be keyed to the **node/shortname it is paired with at `append`**, not to "whichever `append` comes next in the same session label." A `start --session X` followed by an unrelated `append --session X` for a *different* shortname should warn (or record the duration against neither) rather than silently handing the elapsed time to the wrong entry, and a substantive (`tier` ≥ T1) `append` with no matching `start` should be flagged, not silently written as "not recorded" with no signal anywhere that it happened. | `docs/ai-forward-pack/scripts/audit-log.py`; `pack-doctor.py` audit-log check | `pack-doctor.py` (or a new `verify-audit-durations.py`) fails when a `tier: T1`/`T2` audit entry with `outcome: success` carries no `duration_seconds` and no explicit `duration: not-recorded` marker distinguishing "not instrumented" from "instrumented, zero elapsed" | PP1-01, PP1-02 |
| **new** — "goal-state detector reads the dispatched prompt, not only the parent reply" | The session-profiler's SP-09/SP-06 heuristic should also scan the **text handed to a dispatched sub-agent** (the Task-tool `prompt` field, or the audit entry's own `prompt`/`goal`/`done_when` fields when the turn's task-notification correlates to one) for `Goal:`/`Tier:` markers, not only the parent turn's visible reply. | `docs/ai-forward-pack/scripts/session-profile.py` (the SP-06/SP-09 heuristics) | a fixture turn whose parent reply is a bare task-notification but whose linked audit entry carries a populated goal/tier must NOT raise SP-06/SP-09 — a red-first test asserting exactly that non-firing | SP-06, SP-09 (both disconfirmed above) |
| F-14 | A budget on the **main line**, counted, not narrated — extend to **per-node** granularity for a multi-node plan (each node's own tool-call count against its own declared budget), not only a whole-phase estimate. | `knowledge/communication-and-task-discipline.md` CT19; `scripts/audit-log.py --main-budget`; selfcheck | selfcheck reports a substantive turn/node with no counted budget-vs-spend as a gap | PP1-02, PP1-04 |
| F-04 | Every delegation (here: every plan node) carries a tool-call budget **and a named convergence condition that distinguishes "doing the work" from "proving the work"** — N7's 23:1 verification-to-execution ratio should be a planned, budgeted split, not an emergent one discovered after the fact. | `knowledge/execution-graph-optimization.md` GO7; the node table in `optimize-graph`'s output | a node's plan entry names an execution-budget and a verification-budget separately; the audit entry records both | PP1-03 |
| F-09 | Session hygiene — re-confirmed applicable: the 6-hour, 33-turn, 10x-context-growth harness session that carried all of Phase 1 plus this Phase-2 docs work is exactly the shape F-09 exists to bound. | `knowledge/session-worktree-discipline.md` WT1a | pack-doctor WARNs on long-context defaults; this profile is itself the recurrence signal | SP-01 |
| F-12 | Ask the host for its richest reasoning summary. | `INSTALL.md` 1.6 | SP-17 reports visible-reasoning share; below 10% marks text-derived findings Inferred (n/a here — findings against this session are Verified from the store, not text-derived) | SP-17 |

**Not entered as a fix (Test-Architect-vetoed):** "count tokens per plan node" as a blanket ask —
struck. No control was named that could observe it failing on recurrence with the current two-namespace
identifier gap (§0); it depends on the harness-UUID/audit-session join proposed as a residual below,
which is not yet built.

## 5. What this profile could **not** measure, and why

- **Per-node token/cost (AIU).** The harness telemetry in `profile-sp-0001` is aggregated **per
  turn**, and a plan node's dispatched work does not align 1:1 with a harness turn — one turn can
  carry a task-notification for one node's completion while the node's own work happened across a
  prior turn's sub-agent delegation, invisible as a separate line. Reading off a token cost per node
  from the turn table would be a guess dressed as a measurement; not done.
- **Per-node tool-call counts.** No field in `docs/audit/audit-log.jsonl` records calls-used per
  entry. The "~290 of 300" figure in the E18 close note is the conductor's own narrative count, not
  independently re-derivable from any store this profiler reads.
- **Sub-agent-level cost for the Phase-1 node dispatches specifically.** `profile-sp-0001` resolves
  named delegate rows (tool calls, tokens, wall clock) for several **Copilot** sessions in this repo
  (e.g. `the-simplifier: 23 tool calls, 1,467,913 tokens, 1560s` under `copilot:e3c8ed7d`). No
  equivalent delegate-level row exists anywhere for `claude:18fe7a5a`'s own task-notification turns
  — the profiler's Claude-harness reader did not resolve them to named sub-agent rows the way it did
  for Copilot's. This looks like a genuine capability gap between how the two harnesses' stores
  expose delegated work, not a Phase-1-specific absence; it is reported here as **not recorded**,
  not modeled.
- **Cross-harness / cross-model comparison for Phase 1 itself.** Phase 1 used exactly one harness
  (Claude Code) and, so far as the audit log records, one model family throughout. `profile-sp-0001`'s
  SP-14 model-family-gap finding is a **whole-repo** number pulled largely from unrelated Copilot
  sessions (turn mix 3 vs 321, explicitly caveated in that finding) and **does not characterize
  Phase 1** — it is not repeated here as a Phase-1 finding, to avoid exactly the kind of borrowed-evidence
  citation E15/IO1 forbid.
- **A harness-UUID ↔ audit-session join.** §0's two-namespace gap means no query in either store can
  answer "how many harness tokens did node N4 cost" directly; this profile's §2 alignment was done
  once, by hand, against timestamps, and is not a repeatable capability.

## 6. Status

| | |
|---|---|
| **Completed** | Node-duration ground truth extracted and cross-verified against the raw audit log (§1); front-loaded-vs-recurring and verification-vs-execution splits computed from measured figures only (§2); two heuristic findings (SP-06, SP-09) disconfirmed against the audit log rather than accepted at face value; one noise finding (SP-15) struck; findings and fixes tables produced (§3–4); missing telemetry named explicitly rather than estimated around (§5). |
| **Remaining** | The two proposed new fixes (node-scoped `start`, prompt-aware goal-state detection) are proposals only — neither has a red-first test yet, and neither was registered as a `docs/lessons/defect-classes.md` class in this run: that file is owned by a concurrent agent in this worktree and was out of scope here. |
| **Best next action** | Phase 2 N0's control obligations (per `note-conductor-phase1-e18-close` condition 5: `audit-log.py start` per node, opened before any Phase-2 code) directly close PP1-01/PP1-02 going forward if followed; the node-scoped-start fix proposed here would make that discipline self-enforcing rather than convention-dependent. |
