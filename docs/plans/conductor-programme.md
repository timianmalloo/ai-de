---
id: plan-conductor-programme
title: "Execution graph — AI-DE Conductor programme, Phase 1 expanded"
type: doc
status: in-review
owner: "@timianmalloo"
phase: "1"
tags: [execution-graph, conductor, agent-plane, acp, phase-1, coordination]
links:
  - { to: spec-conductor, rel: relates-to }
  - { to: kb-multi-agent-coordination, rel: depends-on }
  - { to: note-conductor-r4-core-phase1-scope, rel: depends-on }
  - { to: note-conductor-acp-lane-separate-shape, rel: depends-on }
  - { to: note-conductor-tos-invariant-observed-auth, rel: depends-on }
review-by: 2026-12-09
summary: >-
  The bounded execution graph for Conductor Phase 1 (R1, R2, R4-core), with Phases 2-4 as
  collapsed nodes. Width is two tracks, not the permitted four: the ACP client dominates the
  span, so widening around it buys ~1.2x against a token multiplier that ranks above speed.
---

# Execution graph — AI-DE Conductor programme

## Goal state

- **Goal:** implement the Conductor spec v1.0 phase by phase per §12, preserving the
  terminal/ConPTY stack as a first-class observed-lane option.
- **Done when (Phase 1):** a real governed run on `claude-code`, in a provisioned worktree,
  scored end-to-end by the existing Watcher with zero terminal hosting; R1/R2/R4-core pass as
  tests; build and full suite green on main; Owner signs the E18 close.
- **Not in scope:** Antigravity · any change to `weave/1` dimensions, weights, floors or the
  leaderboard partition · API-key routing beyond the disabled-by-default config entry · removal
  or degradation of `Terminal/`, ConPTY, readiness profiles.
- **Tier:** T2 · **Fan-out cap:** 4 (permitted) · **Width used: 2** (justified below).

## Stage 0 — triage

**Planning is warranted.** This is not the 1–2 node case: it is a multi-phase programme with
fan-out, loops, and at least six triggered hard vetoes. Planning was **not** skipped.

## Ground truth this graph is built on

All established before planning, all cited:

| Fact | Source | Consequence for the graph |
| --- | --- | --- |
| ACP works on a Max subscription from a non-Claude-Code client | `spikes/acp-subscription-lane/` — **verified by execution** | Phase 1 exit evidence is reachable; no re-cut needed |
| ACP is **bidirectional** (agent calls the client) | same spike | The client is a **server loop**; this is the largest node |
| `ITerminalSession.Output` must **never** be persisted; ACP events **must** be | `ITerminalSession.cs:68-72` vs spec §7.2 | Ruling 7: `ITerminalSession` frozen; no `ILane` interface either |
| No `IEpisodeSource` seam exists; live path is `IngestHost` | Ruling 3 | `GovernedSessionSource` uses the live path; no interface invented |
| Two doors default to different `taskClass`; `Unclassified` is not comparable | `WatcherHost.cs:118`, `ClosedEpisodeScoring.cs:69`, `Leaderboard.cs:66-70` | Ruling 4 + **DC-110**; the one-cell-two-cohorts test is the oracle |
| CI refuses `Assert(measured < constant)` | `verify-perf-assertions.py`, DC-107 | Ruling 5: 250 ms is a recorded SLO, not an assertion |
| API-key env sources outrank the subscription | spike residual 2 | Ruling 8: gate on **observed** auth status, fail closed |
| `AgentPlane/`, `Conductor/`, `Sessions/` do not exist | `ls src/AiDe.Core/` | Zero file contention between new-subsystem tracks |
| `WorkbenchShell.cs` is 2,755 lines and the only `AgentWorktree` caller | measured | **All App-layer work is serial**, at converge |
| Comparable node cost | audit history, `implement-watcher-*` | 664–1238 s, **median 882 s** (n=21) — **Verified** |

**Cost unit.** 1 unit = 900 s, the rounded median of the comparables above (**Verified**). Every
per-node figure below is **Inferred** from that unit; only the unit itself is measured.

## The optimized graph

| Node | Goal | Exit condition (oracle) | Tier | Capability | Model | Deps | Units |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **N1** | Run-event envelope (§7.2/§14.1): kinds, `cost`, **`ext` preservation** | A synthetic unknown event round-trips with `ext` intact; a known kind loses nothing | T1 | Reasoning | opus | — | 0.5 |
| **N2** | `EngineCatalog`/`EngineSpec` as data; pinned adapter packages | Catalog loads all three engines; an unknown engine is refused, not defaulted | T0 | Det. mechanics | sonnet | N1 | 0.5 |
| **T-A** | `AcpClient`/`AcpSession`: NDJSON JSON-RPC **server loop**, subprocess lifecycle, **observed-auth spawn gate** (R8) | Live round-trip against the pinned adapter: initialize → `session/new` (absolute cwd) → `tool_call`; spawn **refused** when auth status absent | T2 | Reasoning | **opus** | N1, N2 | 3.0 |
| **T-B** | Collapsed: `ProviderRegistry`/accounts · `WorktreeProvisioner` · `GoalBlock`/`SpawnContract` · **`GovernedSessionSource`** · `mode` migration | Goal block missing any CT19 field fails **field-level**; worktree provisioned + `coord install` inside it; episode opens with attrs byte-for-byte | T1 | Reasoning | sonnet | N1 | 3.0 |
| **N3** | Integration: T-A's live events drive T-B's source | One governed episode reaches the store and is scored by **unchanged** `ScoringService` (shown by diff) | T1 | Independent review | opus | T-A, T-B | 1.0 |
| **N4** | One-cell-two-cohorts test + audit-import path still green | One `ScoreSegment` cell, exactly two `mode` values, both episodes present, **in one test** | T1 | Det. mechanics | sonnet | N3 | 0.75 |
| **N5** | Out-of-lease edit raises a seam; run cannot close with an open seam | An edit outside the lease produces a seam event; close with an open seam forces `Blocked` | T1 | Reasoning | sonnet | T-A, N3 | 0.75 |
| **N6** | App wiring + single-lane Conductor Surface | Pane renders live run events; `ui-craft-gate` clean; surface has a declared owner | T1 | Reasoning | sonnet | N1, N3 | 1.0 |
| **N7** | Exit-evidence run + Proof Pack | A real refactor run on `claude-code`, in a worktree, scored, **zero terminal hosting**; latency reported vs 250 ms with host named | T2 | Independent review | opus | all | 0.75 |

**Deliberately collapsed (GO13).** `ProviderRegistry`, `WorktreeProvisioner`, `GoalBlock` and
`GovernedSessionSource` are one track, not four. They share a context, none is a gate, and
separating them buys no independent verification while costing four context loads.

**Deliberately promoted.** `N3`, `N4`, `N5` are verification gates that would otherwise hide
inside an implementation step. `N7` is promoted because it carries the phase's whole evidentiary
claim.

## The span attack, done before the width (GO4)

The first draft ran `GovernedSessionSource` *after* `AcpClient`, because that is the order the
spec describes them in. That edge is **incidental, not real**: the source consumes the **N1
envelope contract**, not `AcpClient`'s implementation, so it can be built red-first against
synthetic events. **Moving it into T-B removed a full node from the critical path** — the single
largest improvement in this plan, and it came from deleting an edge, not from adding a worker.

| Measure | Value |
| --- | --- |
| Work `T₁` | **9.75 units ≈ 2.4 h** |
| Span `T∞` | N1 → N2 → T-A → N3 → N5 → N7 = **6.0 units ≈ 1.5 h** |
| Ceiling at p=2 | `(9.75−6.0)/2 + 6.0` = **7.9 units** → **1.24×** |
| Ceiling at p=4 | `(9.75−6.0)/4 + 6.0` = **6.9 units** → **1.41×** |

## Why width 2, not the permitted 4 — stated honestly

**`T-A` is 31% of all work and sits on the span.** Going from two tracks to four buys
**1.24× → 1.41×**, about 14% more wall-clock, and buys it by adding two more agent contexts,
two more worktrees, two more seams to arbitrate, and two more places for inter-agent
misalignment. GO6 prices orchestrator-worker fan-out at roughly **15× tokens**, and the objective
here is **lexicographic with tokens ranking above speed** — so paying a large token multiplier for
14% of wall-clock is a **loss on the ranked objective, not a trade**.

This is also the exact shape the evidence base already measured: *three independent gates
parallelised ran 19% slower because one was 83% of the work and spawn cost exceeded the whole
available gain.*

**The lever is not width. It is that `T-A` does not rework** — which is why the ACP spike was run
before this plan existed, and why its contract findings (bidirectionality, NDJSON framing, `ext`
on turn one, absolute `cwd`) are recorded as inputs to `T-A` rather than discoveries inside it.

**GO5 independence, evidenced.** `T-A` and `T-B` pass all three tests: no data edge (both consume
the frozen N1 contract; neither consumes the other's output), no decision edge (neither result
changes the other's shape), no shared exclusive resource (`AgentPlane/` and `Conductor/` +
`Sessions/` do not exist yet — measured). **All App-layer work is excluded from both tracks** and
serialized into N6, because `WorkbenchShell.cs` is a single 2,755-line shared file.

## Fan-out contract (the one fan-out: T-A ∥ T-B)

1. **Width cap:** 2. Raising it requires a recorded Owner ruling.
2. **Transient-failure policy:** a track's own build/test failure is the track's to fix, in its
   loop, up to that loop's cap. A *seam* failure stops the track and goes to the conductor.
3. **Per-branch exit:** the track's exit condition in the node table, plus its Proof Pack.
4. **Join rule, including partials:** topological merge, T-B before T-A (T-B's episode path is
   what T-A's events flow into). **A partial is not merged** — a track that misses its exit
   condition blocks its own merge and *only its dependents*; the other track still merges.
5. **Containment:** one worktree per track, own branch, `coord install` run inside each
   (`.git/config` is per-clone). No track may edit outside its lease; doing so raises a seam
   automatically, which is N5's subject.

## Loop bounds (GO8–GO10)

| Loop | Variant (strictly decreases) | Floor | Exit | Cap |
| --- | --- | --- | --- | --- |
| Red→green per node | failing tests remaining | 0 | all green **and** review clean | 3 passes |
| Council review | unresolved **Blocker** findings | 0 | no unresolved Blockers in domain | 2 rounds |
| Build/test fix | failing tests remaining | 0 | suite green, counts updated | 3 passes |
| ACP contract drift | unhandled event kinds observed | 0 | all observed kinds either mapped or in `ext` | 2 passes |

**Every cap is a circuit breaker. A cap firing is a defect signal about the variant, not a
licence to raise it** — and never a termination argument.

## Immovable floor nodes (GO12)

Reorderable; **not removable**.

| Floor | Trigger | Attached to |
| --- | --- | --- |
| Security & Identity (**hard**) | subprocess spawn, credentials, trust boundary, new dependency | T-A, T-B |
| Distributed Systems (**hard**) | bidirectional async, ordering, backpressure | T-A |
| Data & Persistence (**hard**) | `mode` column migration — must sign it **expand-only** | T-B, N4 |
| AI Systems (**hard**) | model-backed capability, non-determinism containment | T-A, N3 |
| Test Architect (**hard**) | every correctness claim; **no track without exit evidence** | all |
| Simplifier (**soft**) | any new abstraction, layer, option | per track |
| UX & Accessibility + `ui-craft-gate` | user-facing surface | N6 |
| SRE | timeouts, retries, resource bounds, hot path | T-A |
| Tech Lead | casting vote on Architect ↔ Simplifier | as needed |
| Testing-Strategy union · E7 surface list · red-first · audit entries | standing | all |

**Repo gates every track must keep green:** `verify-test-run.py --update` with the
portable/non-portable split · `verify-surface-ownership.py` (N6) · `verify-standins.py` ·
`verify-perf-assertions.py` (Ruling 5) · `verify-derived-views.py` · `verify-defect-register.py` ·
`verify-no-conflict-markers.py` · `TreatWarningsAsErrors=true`.

**Defect classes designed out, not rediscovered:** DC-027 (reuse `EnvironmentHealth.Inspect` for
subprocess env) · DC-096 (why `ITerminalSession` stays frozen) · DC-053 / WT13 (worktrees do not
isolate `stash`/`bisect`/notes) · DC-107 (no wall-clock budget assertions) · DC-110 (task class
from the work, not the door) · DC-012 (test-count floor) · DC-104 (new gates ship with
`--self-test`).

## Model allocation per phase (GO19)

**opus** — N1 (contract design), T-A (protocol reasoning), N3 (integration judgement), N7
(evidence judgement). **sonnet** — N2, T-B, N4, N5, N6, and **every build/test fix loop**: those
are low-novelty and high-iteration, and GO19 is explicit that putting the premium tier there
multiplies the least valuable token by the largest number.

## Budget and degradation path (GO15)

**Main-line budget:** 300 tool calls. **Degradation, in order, never dropping a gate:**

1. Defer **N6** (Conductor Surface) to a Phase-1b under an Owner ruling. R1/R2/R4-core and the
   exit evidence do not require a pane; §12 lists it as a Phase-1 *ship*, so this is a **scope
   cut requiring a ruling**, not a silent omission.
2. Narrow N7's exit run to a smaller refactor — the claim is "governed, worktree, scored, zero
   terminal hosting", not "large".
3. **Stop and report.** Never drop a floor to fit the budget.

## Re-plan checkpoints (GO17)

1. **First .NET ACP round-trip in T-A.** If the hand-rolled client does not hold, reconsider one
   of the unofficial NuGet clients — a shape change, not a slip.
2. **After N3.** If the episode does not score through unchanged `ScoringService`, Ruling 3's
   reading is wrong and the seam question reopens.
3. **Licensing answer from the human.** A NO re-cuts the phase: everything protocol-level still
   builds and tests against `codex-acp`, but N7's exit run on the Max account cannot proceed.
4. **Adapter version bump** during the phase — re-check the pinned override constant (Ruling 8
   condition 3).

## Phases 2–4 (collapsed, with their real edges)

| Phase | Ships | Depends on | Edge type |
| --- | --- | --- | --- |
| **2 — Conductor** | `ConductorHost` on Max, tools, plan/council/dispatch/steward, board + seams, converge (R3, R5, R10) | Phase 1's N1 envelope + T-A client + N5 seams | data |
| **3 — Observe & choose** | Sessions store, projections, restart, Profiler pane, derived metrics, bench import, cohorts, routing + standings (R6–R9) | Phase 1 N1/N4; Phase 2 board. **Retires the `"audit-import"` door default (DC-110)** | data + decision |
| **4 — Reach** | grok-build observed parity, routing-quality review, Antigravity spike (R11, R12) | Phase 1 N4 cohorts; **`ObservedLaneBinding` here resolves Ruling 7's deferred one-interface convergence** | decision |

## Planned vs actual

*(To be completed at Phase-1 close per GO18 — nodes, wall-clock, rework passes, floors met, and a
mitigation record where a plan change was validated red→green.)*

| | Planned | Actual |
| --- | --- | --- |
| Nodes | 9 | — |
| Span | 6.0 units | — |
| Width | 2 | — |
| Rework passes | 0 | — |
| Floors met | 10/10 | — |
