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

---

# Second round — the Tech Lead's casting vote, and Rulings 12–14

**Casting vote: PASS-WITH-CONDITIONS, *for* the resolution** — take both vetoes, go width 1.
*"These two vetoes were never in tension: one adds proof, one removes scope, and doing both is
strictly better than either."* Three Blockers, one of which overruled a deferral. It also corrected
the Simplifier and the Owner, not only the conductor.

## Ruling 12 — the governed path passes `taskClass` explicitly

**Verified by the conductor and re-verified by the Owner:** `Leaderboard.cs:66-70` makes **only**
`Unclassified` and a null `Workspace` incomparable. So **`"audit-import"` is a comparable class** —
and it is a **parameter default** at `WatcherHost.cs:118`, acquired by omission. A governed episode
picking it up does **not** surface as unranked; it **silently ranks inside the wrong cohort**. DM's
*"a backfill never guesses"* means Phase 3 cannot repair it.

**Ruling:** the governed call site passes `taskClass` explicitly in Phase 1, and the integration
node asserts **neither episode's `TaskClass` came from a parameter default**. Retiring the default
still waits for Phase 3 — the observed path is untouched, so no existing episode moves.
*"Retiring the default may wait; passing the argument may not."*

**Condition:** the assertion **reads the stored `TaskClass`, not the call argument.**

**New Phase-3 finding, surfaced by the Owner:** `WatcherHost.cs:118` and `:146` carry **two
defaults for one concept** — one comparable (`"audit-import"`), one not (`Unclassified`).

## Ruling 13 — split the launcher from the Surface

Ruling 10 cut the Surface node wholesale. But that node was *"App wiring **+** single-lane
Conductor Surface"* — cutting it deletes **the only launcher in the graph**, making Ruling 10's own
condition (the exit run must use real App-layer composition) **unsatisfiable**.

**Ruling:** cut the Surface (pane render, `ui-craft-gate`, `verify-surface-ownership.py`, declared
owner — honestly gone). **Keep** the headless App-layer launch path. Floor row becomes **"9/9, UX &
Accessibility retired from Phase 1 by the surface deferral"** — a 10/10 claim would be contradicted
by the artifacts at close. **Amends Ruling 10; does not reverse it.**

**Condition:** the launch path is **the same composition root the Surface will later call** — no
test-owned wiring, no second entry point.

## Ruling 14 — two deferrals were misfiled

- **"Phase 1b" is retired as a label.** It appears **nowhere** in spec §12 (grep: 0 matches), so it
  is an invented, ownerless container — the standard shape of debt that becomes permanent. Replaced
  by a named **re-entry trigger**: *"the first time a run must be watched by a human rather than
  read from the store,"* carried as a line in Planned-vs-actual.
- **The one-interface lane convergence is NOT debt.** Ruling 7 *decided against* `ILane`. Filing a
  decided non-goal as debt **invites a future session to build the thing Ruling 7 forbids**.
  Recorded as a **non-goal with a trigger** (`ObservedLaneBinding`, Phase 4). Non-goals and
  deferrals are now **separate sections** in the plan.
- The codex/copilot deferral is named **the gold standard**: N2's *"an unknown engine is refused,
  not defaulted"* means the deferral is **enforced by a test that goes red** the moment someone
  half-implements it, rather than remembered.

## Confirmations

1. **Node count is 8, not 7** — N0 plus the split launcher. Understating it would put
   Planned-vs-actual in the wrong from day one.
2. **Gate policy confirmed, with an addition:** each node's close note lists which subject-changed
   gates were **not** run and why — *so a skip is recorded rather than indistinguishable from a run.*
3. **Reuse call confirmed, duplication on the record.** The Tech Lead corrected the Simplifier:
   `Program.cs` is a **false comparable** — a server whose single peer never initiates, with no
   outbound requests, no id generation, no pending table. The true comparable is `probe-write.js`,
   a working 36-line bidirectional client already in the repo. Copy ~35 lines with a cross-reference
   marker in **both** files, ship `--self-test`, **extract nothing**; trigger = a third stdio
   JSON-RPC consumer or the first defect fixed twice. **Condition:** the copied region carries a
   `simplify:` marker naming that trigger.
4. **Session boundaries confirmed:** one worktree for the Phase-1 branch, four sequential sessions,
   boundaries after N2, after plane services, and after the ACP client — **no WT1 exception to
   record.**

## On dispatching N0 without a ruling

The conductor dispatched the frame-corpus capture before asking, because the probes truncate every
frame (`probe-write.js:13,15`) and the open licensing question could remove the authenticated access
that makes capture possible. **The Owner: "your judgement stands. Capturing evidence in an open
window is not a scope change."** Recorded here with the licensing risk as the stated reason for not
waiting.

## Sign-off

**GRANTED** — Phase 1 is **8 nodes, width 1**, both vetoes taken, Surface deferred with a trigger,
lane convergence a non-goal. Five conditions, all now present in the plan: explicit `taskClass` plus
a stored-value assertion; floor row 9/9 with UX & A named as retired; "Phase 1b" removed and the
re-entry trigger carried; non-goals separated from deferrals; gate-skip recording per node and the
`simplify:` marker on the copied framing.
