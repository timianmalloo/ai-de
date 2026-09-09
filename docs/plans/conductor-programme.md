---
id: plan-conductor-programme
title: "Execution graph — AI-DE Conductor programme, Phase 1 expanded"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [execution-graph, conductor, agent-plane, acp, phase-1, coordination]
links:
  - { to: spec-conductor, rel: relates-to }
  - { to: kb-multi-agent-coordination, rel: depends-on }
  - { to: note-conductor-phase1-plan-approval, rel: depends-on }
  - { to: note-conductor-r4-core-phase1-scope, rel: depends-on }
  - { to: note-conductor-acp-lane-separate-shape, rel: depends-on }
  - { to: note-conductor-tos-invariant-observed-auth, rel: depends-on }
  - { to: note-conductor-latency-slo-not-assertion, rel: depends-on }
  - { to: note-conductor-mode-cohort-not-partition, rel: depends-on }
  - { to: note-conductor-episode-source-seam, rel: depends-on }
  - { to: note-conductor-spec-errata-policy, rel: relates-to }
review-by: 2026-12-09
summary: >-
  Phase 1 of the Conductor spec as eight serial nodes at width 1. Both council vetoes
  returned BLOCK and converged: going serial dissolved the finding that the two tracks were
  never independent and made the restored dependency free. Approved by the Owner with five
  binding conditions.
---

# Execution graph — AI-DE Conductor programme

**Status: APPROVED** by Owner sign-off, 2026-09-09, at width 1 with eight nodes and five
binding conditions — all present below. Council: Test Architect (hard veto) **BLOCK, 8 Blockers**;
Simplifier (soft veto) **BLOCK, 7 Majors**; SRE **BLOCK (advisory, escalated)**; Tech Lead casting
vote **PASS-WITH-CONDITIONS, for the resolution**. Every Blocker is mapped to a named clause below.

## Goal state

- **Goal:** implement the Conductor spec v1.0 phase by phase per §12, preserving the
  terminal/ConPTY stack as a first-class observed-lane option.
- **Done when (Phase 1):** a real governed run on `claude-code`, in a provisioned worktree, scored
  end-to-end by the existing Watcher with zero terminal hosting; R1/R2/R4-core pass as tests; build
  and full suite green on main; Owner signs the E18 close.
- **Not in scope:** Antigravity · any change to `weave/1` dimensions, weights, floors or the
  leaderboard partition · API-key routing beyond the disabled-by-default config entry · removal or
  degradation of `Terminal/`, ConPTY, readiness profiles.
- **Tier:** T2 · **Width: 1 (serial)** · **Main-line budget:** 300 tool calls.

## Stage 0 — triage

**Planning was warranted, not skipped:** a multi-phase programme with loops, gates and six
triggered hard vetoes.

## Why width 1, and why a slower plan is admissible

The first draft ran two tracks. The Simplifier showed that fails the plan's **own** test, and the
Tech Lead's casting vote agreed:

- **The two tracks were never independent.** The ACP client's own exit condition requires an
  auth-status refusal (needs `ProviderRegistry`) and an absolute `cwd` (needs
  `WorktreeProvisioner`) — both owned by the other track. GO5 was claimed and is disproved by the
  plan's own oracles.
- **The ceiling was ~1.24×** — roughly 28 minutes best case, never achieved — against GO6's ~15×
  token multiplier for orchestrator-worker fan-out.
- **Speed ranks last** (GO4a). Speed↑ with tokens↑ and rigor→ is a loss, not a trade.

**GO4a admits a slower plan only when completeness ↑ AND rigor ↑ AND tokens ↓ — a conjunction.**
All three hold: three untraced acceptance bullets gain clauses; a real captured frame corpus
replaces author-written events; one context replaces two plus a seam protocol, its join rule and
two throwaway stubs.

### The cost model, labelled honestly

The unit is **1 unit = 900 s**, the median of 21 `implement-watcher-*` audit entries. **That unit
is `Flagged`, not `Verified`, for applicability:** the SRE established the comparables are a SQLite
store, a unidirectional HTTP receiver, JSONL parsing and a WPF pane — **none is a bidirectional
server loop driving a subprocess.** The arithmetic is real; the applicability is not established.

**The width decision does not rest on it.** Across the plausible range of the largest node, the
gain from widening is small everywhere:

| ACP-client size | Speedup at p=2 | at p=4 | Difference |
| --- | --- | --- | --- |
| 2 units | 1.30× | 1.52× | 0.22× |
| 3 units | 1.27× | 1.46× | 0.19× |
| 5 units | 1.22× | 1.37× | 0.15× |
| 8 units | 1.17× | 1.28× | 0.11× |

*(All Inferred.)* Widening buys **0.11–0.22×** regardless of the disputed number, so the decision
rests on the shape of the graph, not on the unit.

## The graph — eight serial nodes

`N0 → N1 → N2 → N3 → N4 → N5 → N6 → N7`

| Node | Goal | Tier | Model | Units |
| --- | --- | --- | --- | --- |
| **N0** | Capture the real ACP frame corpus | T0 | sonnet | 0.25 |
| **N1** | Run-event envelope, bounded to one source | T1 | opus | 0.5 |
| **N2** | Engine catalog — three data rows, one launch path | T0 | sonnet | 0.5 |
| **N3** | Plane services — five pieces, five clauses | T1 | sonnet | 3.0 |
| **N4** | ACP client — bidirectional peer | T2 | **opus** | 3.0 |
| **N5** | Integration and scoring — one store, one test | T1 | opus | 1.0 |
| **N6** | Leases and seams | T1 | sonnet | 0.75 |
| **N7** | Headless launcher + exit evidence | T2 | opus | 1.0 |

**Work `T₁` = 10.0 units. Span `T∞` = 10.0 units (serial).** There is no ceiling to quote: at
width 1, `Tₚ = T₁ = T∞`. That is the honest number, and it is the point.

### Node exit conditions — every clause names what would make it fail

**N0 — capture the real ACP frame corpus.**
The probes truncate every frame today (`probe-write.js:13,15` — `.slice(0,700)`, `.slice(0,900)`),
so the README's frame listings are a paraphrase of truncated console output and **no corpus
exists**. Run first, and urgently: the authenticated access this needs is exactly what the open
licensing question could remove.
- Every received line is appended **verbatim** (the raw line, never a re-serialization, which
  reorders keys) to `spikes/acp-subscription-lane/frames/*.jsonl`; sent frames captured too — the
  contract has two directions.
- Every line parses standalone; the file is non-empty; the distinct observed `method` and
  `update.sessionUpdate` values are reported **as observed**, confirming or contradicting the
  README's claimed list.
- Redacted before commit: the `_auth/status_update` account block and absolute home paths, with
  **structure preserved exactly** — key names, nesting, types and array shapes are the whole point.
- `PROVENANCE.md` states adapter version, CLI version, date, command lines, OS, redaction rule, and
  that these are **real captured frames, not authored ones**.
  *Fails if:* a frame is truncated, a line does not parse, or the corpus is hand-written.

**N1 — run-event envelope, bounded to one source.**
Spec §7.2 describes one envelope over four sources and lists 20 kinds. **Phase 1 has ACP only.**
`block.open`, `plan.submitted`, `council.verdict`, `decision` and `quota.pressure` have **no
Phase-1 producer**; `parent_agent_id` has none either (no conductor until Phase 2) — carry the
nullable field, build nothing on it.
- **One record** (the §14.1 shape), `kind` as an **open string, not an exhaustive enum** (the spec
  itself says consumers ignore unknown kinds), **one** mapper, no per-kind handler for a kind with
  no producer.
- **Every frame in N0's corpus round-trips**, and each real non-schema-v1 kind actually observed
  survives in `ext` **with no field lost**.
  *Fails if:* any captured frame loses a field, or a second mapper appears.

**N2 — engine catalog.**
- Three engine **data** rows load (pinned packages per the errata-policy note); **one** launch path
  (adapter) is exercised.
- **An engine whose `acp` mode is not `adapter` is refused with a named reason** — this is the
  clause that keeps the codex/copilot deferral *enforced rather than remembered*.
- Carries `simplify: adapter launch only; upgrade trigger = first native-ACP (copilot) spawn`.
  *Fails if:* an unknown or non-adapter engine is silently defaulted.

**N3 — plane services. Five pieces, five clauses** (the collapse must not hide an unproven piece;
five clauses may be proven by three tests, not five):
1. `ProviderRegistry` — an unknown provider id is **refused, not defaulted**.
2. `WorktreeProvisioner` — worktree provisioned on a **namespaced branch**, with `coord install`
   run **inside** it (`.git/config` is per-clone).
3. `GoalBlock`/`SpawnContract` — a **parameterized test over all six** spec-named fields (Goal,
   Done when, Not in scope, Tier, Fan-out cap, **Budget**), each asserting the field-level error
   **names that field**. *"Any missing field"* is satisfiable by one case and is not enough.
4. `GovernedSessionSource` — episode opens with attributes **byte-for-byte** equal to the goal block.
5. `mode` migration — **expand-only**, reusing `SqliteWatcherObservationStore.cs:988-1030` as the
   named template; **legacy rows read NULL after migration**, proven against a **pre-migration
   fixture database**, with row count and scores byte-identical.
   Plus: **an explicit Anthropic direct-api spawn attempt is rejected with the ToS reason** while a
   subscription account is configured (spec l.354, Ruling 8 clause 1); and **killing the engine
   process closes the episode `Blocked` and parks — never deletes — a dirty worktree** (spec l.353).

**N4 — ACP client, a bidirectional peer.**
Reuse rungs, recorded: `src/AiDe.Mcp/Program.cs` is a **false comparable for the whole** — a server
whose single peer never initiates, with no outbound requests, no id generation and no pending table.
**Copy ~35 lines** (NDJSON read loop, Result/Error envelope builders, the `-32601` case) **with a
`// mirrors src/AiDe.Mcp/Program.cs framing` marker in both files and a `simplify:` marker naming
the extraction trigger** — *a third stdio JSON-RPC consumer, or the first defect that must be fixed
in both places.* **Port the structure** from `probe-write.js` (a working 36-line bidirectional
client already in the repo); its hardcoded `m.id === 1|2|3` correlation is the one mechanism that
does not scale and is therefore the thing actually to design. **Extract nothing in Phase 1.** Also
inherit `Program.cs`'s recorded decisions: stdout is the protocol, diagnostics to stderr, one bad
message never kills the loop, an unknown method is **answered, not ignored**.
- Live round-trip: `initialize` (pinning `protocolVersion: 1` and **asserting the echoed value**) →
  `session/new` with an **absolute** `cwd` → `tool_call`.
- **Spawn is refused when observed auth status is absent** — fail closed, never assume subscription.
- **Backpressure, named.** Reuse the bounded-channel idiom from `IngestHost.cs:89-95` (which counts
  drops via `itemDropped`), **not** `ConPtyTerminalSession`'s `DropOldest`: terminal bytes are
  ephemeral by contract, but **ACP events are the run log, which §7.1 calls the truth** — a silent
  drop is data loss in the durable record. Overflow applies backpressure and any drop is **counted
  and visible**, never silent.
- Subprocess failure modes each have a case: child crash mid-session, partial/unterminated line,
  over-long line, child outpacing the reader, hung child (named timeout), orphaned process reaped.
- **Latency (Ruling 11):** the **required** assertion is a deterministic **ordinal** — the
  normalized run event is observable **before** the client's response to the next inbound ACP
  request for the same call id. Plus: the latency measurement **exists and is a number**; with the
  receipt timestamp removed it reads **"not recorded", never 0**. The 250 ms stays a **recorded
  SLO**, evaluated at N7.
- Ships `--self-test` covering the same guards `Program.cs:140-186` covers (DC-104).
  *Fails if:* an event is dropped silently, the ordinal inverts, or a killed child leaks a process.

**N5 — integration and scoring: one store, one test.**
Ruling 1's Condition 1 requires the audit-import and governed paths shown *"in one store in one
test, not in two separate tests"* — so N3/N4 of the old draft are **one node**.
- One governed episode reaches the store and is scored by the **unchanged** `ScoringService`.
- **One cell, exactly two `mode` cohorts**, both episodes present, under **one caller-chosen task
  class**. No sleeping; no dependence on ingest order.
- **The governed path passes `taskClass` explicitly** (Ruling 12), and the assertion **reads the
  stored `TaskClass`, not the call argument** — because `"audit-import"` is a *comparable* class
  (`Leaderboard.cs:66-70`), so an episode acquiring it by parameter default ranks silently inside
  the **wrong cohort** rather than surfacing as unranked.
- The ACP session's `cwd` **is** the provisioned worktree and the branch **is** namespaced (R1
  bullet 1's composition, which neither half proves alone).
- A **T0-tier run reaches dispatch with zero council lanes and zero plan artifacts** (spec l.362).
  *Fails if:* the two episodes land in different cells, or a `TaskClass` arrives by default.

**N6 — leases and seams.**
- An edit **outside** the lease raises a seam event; **an edit inside the lease raises none**
  (the negative control, without which an implementation that seams on every edit passes).
- Closing with an open seam **forces `Blocked`**.
  *Fails if:* seams fire indiscriminately, or a run closes with one open.

**N7 — headless launcher + exit evidence.** A **four-point falsifiable floor**:
1. **A named, pre-declared refactor task** in this repo, with its **diff oracle written before the
   run**. "A real refactor" undefined is a one-line edit.
2. **"Zero terminal hosting" as a positive assertion** — no terminal-host type resolved from the
   App container during the run, asserted by an instrumented counter equal to 0. An absence claim
   with no oracle is not evidence.
3. **The scored cell must be `IsComparable == true`.** An `Unclassified` cell **fails** — an
   episode that ranks nowhere is not "scored".
4. **A latency SLO breach fails the exit**, reported as measured p50/p95 **with the host named**,
   and is a finding for the Owner — never a footnote.
- The run is launched through the **real App-layer composition root** — the same one the deferred
  Surface will later call. **A hand-assembled test harness fails N7.** No second entry point.
- The `R4-core` "unchanged by diff" check runs **here**, over the whole phase diff (not at N5,
  which precedes the `mode` column's write path).
- Full gate set runs here.

## Immovable floor nodes — 9/9

| Floor | Trigger | Node |
| --- | --- | --- |
| Security & Identity (**hard**) | subprocess spawn, credentials, trust boundary, new dependency | N3, N4 |
| Distributed Systems (**hard**) | bidirectional async, ordering, **backpressure** | N4 |
| Data & Persistence (**hard**) | `mode` migration — signs it **expand-only** | N3, N5 |
| AI Systems (**hard**) | model-backed capability, non-determinism containment | N4, N5 |
| Test Architect (**hard**) | every correctness claim; no node without exit evidence | all |
| Simplifier (**soft**) | any new abstraction, layer, option | all |
| SRE | timeouts, retries, resource bounds, hot path | N4 |
| Tech Lead | casting vote on Architect ↔ Simplifier | as needed |
| Testing-Strategy union · E7 surface list · red-first · audit entries | standing | all |

**UX & Accessibility — retired from Phase 1 by the surface deferral (Ruling 13).** The row is named
rather than deleted; **9/9, not 10/10**, because a 10/10 claim would be contradicted by the
artifacts at close.

**E7 is retained, not suspended.** The Phase-1 surface list is still written store → model →
service → projection/wire → App composition, with the **UI row marked "deferred"** explicitly.
No reviewer may later read "deferred" as "not on the list".

## Gate policy — so a skip is a decision, not a lapse

Eight repo gates across eight nodes is ~56 invocations, and a skipped gate looks identical to a run
one. Therefore:

- **Every node:** build · tests · **`tools/verify-test-run.py` in CHECK mode** · `TreatWarningsAsErrors`.
- **`--update` is never the gate.** Verified: at `:193` `if invariant and not args.update:` **skips
  the split invariant**, and at `:269` the `--update` branch **returns 0 unconditionally** after
  merging observed counts over the baseline — it lowers the floor after a partial run and reports
  success. A baseline bump is a **separate, recorded, human-visible act** at converge.
- **By subject changed:** `verify-derived-views` · `verify-defect-register` · `verify-perf-assertions`
  · `verify-standins` · `verify-no-conflict-markers`.
- **Full set once at N7.**
- **Each node's close note lists which subject-changed gates were NOT run, and why** — so a skip is
  recorded rather than indistinguishable from a run.

## Loop bounds

| Loop | Variant (strictly decreases) | Floor | Exit | Cap |
| --- | --- | --- | --- | --- |
| Red→green per node | failing tests remaining | 0 | green **and** review clean | 3 |
| Council review | unresolved Blocker findings | 0 | none unresolved in domain | 2 |
| Build/test fix | failing tests remaining | 0 | suite green, counts unchanged | 3 |
| ACP contract drift | unhandled kinds in the corpus | 0 | every kind mapped or in `ext` | 2 |

**A cap firing is a defect signal about the variant — never a termination argument.**

## Execution: one worktree, four sessions

Width 1 means no second agent, so worktrees are not needed for isolation — but WT1 makes the
primary checkout a *recorded exception*. **Cut one worktree for the whole Phase-1 branch and run
four sequential sessions inside it**: WT1 satisfied, no exception to record.

| Session | Nodes | Boundary rationale |
| --- | --- | --- |
| 1 | N0, N1, N2 | Ends with three durable artifacts: corpus, envelope types, catalog data |
| 2 | N3 | Consumes the envelope **contract**, not the reasoning that produced it |
| 3 | **N4 alone** | **The most important boundary** — 31% of the work at opus tier, where a polluted context costs most. Re-grounds from the spike README, the corpus and N1's types, all written |
| 4 | N5, N6, N7 | One continuous verification arc; they need both halves live |

**Wrong places to break** (a full re-grounding for nothing): inside N4 — half a protocol client is
not a re-groundable artifact; between N5 and N6, or N6 and N7 — they share the governed-run harness.

## Model allocation (GO19)

**opus:** N1 (contract design), N4 (protocol reasoning), N5 (integration judgement), N7 (evidence
judgement). **sonnet:** N0, N2, N3, N6, **and every build/test fix loop** — low-novelty,
high-iteration work, where GO19 is explicit that the premium tier multiplies the least valuable
token by the largest number.

## Budget and degradation — never dropping a gate

**300 main-line tool calls.** Degradation in order:
1. Narrow N7's refactor task — the claim is "governed, worktree, scored, zero terminal hosting",
   not "large". The **diff oracle still precedes the run**.
2. **Stop and report.** Never drop a floor to fit the budget.

## Re-plan checkpoints

1. **First .NET ACP round-trip (N4).** If the hand-rolled peer does not hold, reconsider an
   unofficial NuGet client — a shape change, not a slip. *Residual risk, named: there is no other
   fallback, and at width 1 an N4 stall is a phase stall with no parallel progress to show.*
2. **After N5.** If the episode does not score through unchanged `ScoringService`, Ruling 3's
   reading is wrong and the seam question reopens.
3. **The licensing answer** (human). A NO re-cuts the phase: everything protocol-level still builds
   and tests against `codex-acp`, and **N0's corpus survives a NO** — a further reason it ran first.
4. **Adapter version bump** — re-check the pinned override constant (Ruling 8, condition 3).

## Non-goals (decided against — NOT debt)

Filing a decided non-goal as debt invites a future session to "pay it down" by building the thing
that was declined.

| Non-goal | Decided by | Trigger that would reopen it |
| --- | --- | --- |
| One-interface lane convergence (`ILane`) | Ruling 7 | `ObservedLaneBinding` reaches Phase 4 |
| `IEpisodeSource` interface | Ruling 3 | A **third** implementer (`BenchImportSource`) |

## Deferrals (owed, with triggers)

| Deferred | To | Re-entry trigger |
| --- | --- | --- |
| Single-lane Conductor Surface | — | **"The first time a run must be watched by a human rather than read from the store."** Carried as a named line in Planned-vs-actual. *(The label "Phase 1b" is retired — it appears nowhere in spec §12, and debt in an invented, ownerless phase becomes permanent.)* |
| Retiring the `"audit-import"` task-class default | Phase 3 | §8.4 controlled task classes. **Phase-3 finding:** `WatcherHost.cs:118` and `:146` carry **two defaults for one concept** — one comparable, one not. |
| codex / copilot launch paths | Phase 3+ | **Enforced, not remembered** — N2's refusal test goes red the moment someone half-implements one. This is the model the other deferrals should imitate. |

## Phases 2–4 (collapsed)

| Phase | Ships | Depends on | Edge |
| --- | --- | --- | --- |
| **2 — Conductor** | `ConductorHost` on Max, tools, plan/council/dispatch/steward, board + seams, converge (R3, R5, R10) | N1 envelope, N4 client, N6 seams | data |
| **3 — Observe & choose** | Sessions store, projections, restart, Profiler, derived metrics, bench import, cohorts, routing + standings (R6–R9) | N1, N5 cohorts; Phase 2 board | data + decision |
| **4 — Reach** | grok-build observed parity, routing-quality review, Antigravity spike (R11, R12) | N5 cohorts; resolves Ruling 7's deferred convergence | decision |

## Planned vs actual

*(Completed at close per GO18. Duration is **measured** — `audit-log.py start` was run — not
modeled. Note the instrument's own limits: the audit log carries `duration_seconds` on 4.5% of
entries and records **0 non-success outcomes across 465**, so a rework count of 0 must be read as
"not recorded" unless a node actively emits it.)*

| | Planned | Actual |
| --- | --- | --- |
| Nodes | 8 | — |
| Width | 1 | — |
| Work `T₁` | 10.0 units (unit **Flagged**) | — |
| Rework passes | not predicted | — |
| Floors met | 9/9 | — |
| Conductor Surface re-entry trigger fired? | no | — |
