---
id: note-conductor-phase1-plan-approval
title: "Decision note — Phase 1 plan approved at width 1, with six binding conditions"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, execution-graph, council, veto, width, plan-approval]
links:
  - { to: plan-conductor-programme, rel: relates-to }
  - { to: note-conductor-latency-slo-not-assertion, rel: refines }
  - { to: note-conductor-r4-core-phase1-scope, rel: relates-to }
review-by: 2026-12-09
summary: >-
  Both council vetoes returned BLOCK. They converged rather than conflicted: going fully
  serial dissolved the Simplifier's finding that the two tracks were never independent and
  made the Test Architect's restored dependency free. Rulings 9-11 resolve them; the plan is
  approved at width 1 with seven serial nodes and six binding conditions.
---

# Decision note — Phase 1 plan approved at width 1, with six binding conditions

**Ruled by:** Owner agent, 2026-09-09. Confidence: **Verified** — the Owner opened DC-107,
spec lines 203/353/354/362/447, `Program.cs:45-113`, `SqliteWatcherObservationStore.cs:988-1030`
and `verify-test-run.py:192-280` before ruling, and confirmed all four reviewer citations.

## The council's verdicts

| Reviewer | Veto | Verdict | Direction |
| --- | --- | --- | --- |
| Test Architect | **hard** | BLOCK — 8 Blockers | **adds proof** |
| Simplifier | soft | BLOCK — 7 Majors | **cuts scope** |
| SRE | advisory | BLOCK (escalated) | measurement honesty + bounds |

**They converged.** Adopting width 1 dissolved the Simplifier's sharpest finding — that the two
tracks were never independent — and made the Test Architect's restored dependency free, because a
serial plan pays nothing for it. The fan-out contract, its join rule, and two throwaway stubs all
disappeared with it.

## Four errors in the conductor's plan, each verified against source

1. **`verify-test-run.py --update` was prescribed as the standing gate.** At `:193`,
   `if invariant and not args.update:` **skips the split invariant**; at `:269` the `--update`
   branch **returns 0 unconditionally** after merging observed counts over the baseline. That
   lowers the floor after a partial run and reports success — DC-012's control with the control
   switched off. *The Owner noted: had this already been running as the gate, it would have been
   escalated to the human as an **EvaluatorIntegrity** trip; it is a condition only because it had
   not yet shipped.*
2. **GO5 independence was claimed and is disproved by the plan's own exit conditions.** The ACP
   client's oracle requires an auth-status refusal (needs `ProviderRegistry`) and an absolute `cwd`
   (needs `WorktreeProvisioner`) — both in the other track.
3. **The cost model was laundered.** The 900 s unit is a real median of `implement-watcher-*`
   entries, but those are a SQLite store, a unidirectional HTTP receiver, JSONL parsing and a WPF
   pane — **none is a bidirectional server loop driving a subprocess.** Honest arithmetic, dishonest
   applicability.
4. **A reuse rung was skipped on the largest node.** `src/AiDe.Mcp/Program.cs` is 199 lines of
   hand-rolled **NDJSON JSON-RPC 2.0 over stdio** — `ReadLineAsync`/`WriteLineAsync` framing,
   method dispatch, `-32601` *answered not ignored*, and a `--self-test`. Its `.csproj` records:
   *"No PackageReference. JSON-RPC over stdio is a hundred lines against System.Text.Json."*

## Rulings

### Ruling 9 — width 1

**Adopt a fully serial plan. Delete the fan-out contract, the join rule and both stubs.**

GO5 independence was never true; the second track adds no completeness and no rigor while raising
tokens, and **speed ranks last** (GO4a). The verified reuse rung means the 3.0-unit size that
carried the width argument was overstated anyway.

*Scope effect:* the ACP→plane edge becomes a real serial dependency **at no cost**. The SRE's
shared-NuGet-cache contention and cost-model findings become **non-load-bearing and are closed by
this ruling**, not by re-derivation.

*Conditions:* the ACP-client node records the reuse rung; the migration node records
`SqliteWatcherObservationStore:988-1030` as its template; planned-vs-actual carries a **measured**
duration (`audit-log.py start`, AL4a), never a modeled one.

### Ruling 10 — Conductor Surface deferred to Phase 1b

Spec §12 lists the surface in Phase 1's **ship** column, but the same row's **exit criterion names
no UI**, and no R1/R2/R4-core bullet requires a pane. **This is an Owner scope cut that EXTENDS the
spec, not a reading of it — marked plainly.**

*The E7 surface list is **NOT** suspended.* It is still written for Phase 1 with the UI row marked
**"deferred to 1b"** explicitly, and `verify-surface-ownership.py` still runs over the surfaces that
remain. **Frozen: no reviewer may later treat "deferred" as "not on the list".**

*Conditions:* (1) the exit run is launched through **real App-layer composition** — a test harness
that assembles the plane by hand **fails** it; (2) the Phase-1b entry note names the surface as its
first node and carries the E7 UI row forward.

### Ruling 11 — Ruling 5 amended

Keep the recorded SLO; **add a host-independent deterministic ordinal as the required assertion**;
attach the latency conditions to the ACP-client node as tests. See
`note-conductor-latency-slo-not-assertion` for the amendment in place.

### Ruling 8, clause 1 — restored

The explicit Anthropic **direct-api spawn rejection** becomes its own exit clause on the
plane-services node, oracle = spec line 354. Spec line 203 makes it a **MUST enforced in code**; the
plan had kept only clause 2, leaving the MUST with no test.

## Plan-approval gate: **APPROVED**

Approved to proceed to `/prepare-for-coordination` in the **7-node serial shape**, subject to six
conditions written into the plan **before** the skill runs:

1. **Standing gate is `verify-test-run.py` in CHECK mode.** `--update` is never the gate; a baseline
   update is a separate, recorded, human-visible act.
2. **Each of the 8 Test-Architect Blockers maps to a named node and clause** — spec 353 and 354 →
   plane services; 362 → integration-and-scoring; Ruling 5 conditions → ACP client; the five
   collapsed pieces named individually; the mode Condition 1 gets a test against a **pre-migration
   fixture database**.
3. **The ACP frame corpus is captured verbatim** from a live session, committed with provenance
   (engine version, date, command line), **reviewed for paths, tokens and secrets before commit**,
   and is the oracle for **both** sides of the envelope contract. Author-synthesized events may
   remain as additional cases, **never as the only cases**.
4. **N7 is rewritten as a four-point falsifiable floor:** a *named, pre-declared* refactor task with
   a diff oracle written before the run; "zero terminal hosting" as a **positive** assertion (no
   terminal-host type resolved from the App container), not an absence; the scored cell must be
   `IsComparable == true` — an `Unclassified` cell **fails**; and an SLO breach **fails** the exit.
5. **The ACP-client node carries a backpressure and subprocess-failure design**, reusing one of the
   repo's two bounded-channel idioms and naming which.
6. **The plan opens with its goal state and `audit-log.py start`** so planned-vs-actual carries a
   measured duration.

**Rigor floors touched: none removed.** E7 retained with a deferred row; red-first unchanged; the
Testing-Strategy union **widened** by Rulings 8 and 11; audit entries gain a measured duration.
