---
id: note-conductor-phase1-e18-close
title: "E18 close — Conductor Phase 1, counter-signed Completed"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, phase-1, e18, close, ct25, self-assessment]
links:
  - { to: plan-conductor-programme, rel: relates-to }
  - { to: note-conductor-phase1-plan-approval, rel: refines }
  - { to: proof-conductor-agent-plane, rel: tested-by }
review-by: 2026-12-09
summary: >-
  Phase 1 closed Completed and counter-signed by the Owner on evidence it opened itself.
  All four done-when clauses met. Five conditions carried into Phase 2, including a
  mandatory first node holding the DC-115 control, the curated documentation half and
  the session profiler.
---

# E18 close — Conductor Phase 1

**Outcome: `Completed`.** Counter-signed by the Owner agent, 2026-09-09, on evidence it opened
itself (`result.json`, the pre-run oracle note, the DC-115 register entry, the R4-core diff).

## Done-when, clause by clause

| Clause | Verdict | Evidence |
| --- | --- | --- |
| Exit evidence — real governed run on `claude-code`, in a worktree, scored end-to-end, zero terminal hosting | **Met**, one qualification | Run `run-832a8655` / episode `6c1ed36f`, 102 s, `end_turn`, auth `kind=account label="Claude Max"`, 7 permissions inside lease, 0 seams, tree parked, engine reaped. Launched via `AiDe.App.exe --conduct` → the real App composition root |
| R1 / R2 / R4-core pass as tests | **Met** | R4-core mutation-verified: breaking the observed door's task class gave `Assert.Single() Failure: The collection contained 2 items` |
| Build and full suite green on main | **Met** | 0 warnings, 0 errors; `verify-test-run` OK, **2276 executed** (399/399, 1877/1877), gate run **bare** so the exit code is real |
| Owner signs the E18 close | **Met** | This note |

**The four-point exit floor:** oracle committed *before* the run (`cfc3932`), 7 clauses all held ·
`terminalHostConstructions: 0`, **falsifiable** — a companion test starts a real ConPTY and the same
counter reads 1 · `IsComparable: true`, verdict `Partial: 15/15 observed`, read from
`scored_episode_cell` · p50 **0.022 ms**, p95 **0.0641 ms** over 287 events, host `TIMMALLSTRIX`.

**R4-core's decisive check:** `Dispatch/` and `Terminal/` **byte-unchanged, zero files**. `Watcher/`
+104/−2, the only deletions being `private const int SchemaVersion = 5;` and a `workspace TEXT NULL`
line that gained a trailing comma.

## The qualification, stated not buried

**DC-115.** The run rooted in a local **clone**, not the operator's linked worktree. Committing the
Proof Pack before the close does **not** credit it in a linked worktree: `RepositoryCorrection`
rebinds the session to the parent checkout and `ProofPackVerifier` reads *that* tree. Both halves
measured. So the shape spec §6.4 describes cannot currently be credited for its own evidence.

The Owner ruled this a **finding with a condition, not a refusal** — the done-when did not require
that shape, and no verdict was produced under it.

## CT25 self-assessment — what this phase did NOT demonstrate

A governed lane in the operator's own worktree (DC-115) · Budget/FanOutCap enforcement (both
validated, neither fires) · a rendered leaderboard rank (comparability asserted at `ScoreSegment`;
the composer's cohort minimum of 5 renders every cell NotComparable with two episodes) · lease
enforcement over **shell** writes (`LeaseMonitor` sees `kind:"edit"` frames only) · a seam firing
live, or a killed engine live (both unit-proven; the exit run raised zero seams) · any distribution
— **one run, one host, one latency sample set**, in-process normalization only.

## Three failures that are the conductor's own

1. **The by-subject gate policy was wrong.** N7's full sweep found **four gates failing that eight
   nodes never ran** — `verify-id-allocators` (20 `AP-` codes, no allocator),
   `verify-bounds-are-enforced`, `verify-cited-controls`, `verify-api-crefs`. The policy's own
   sentence — *"a skipped gate looks identical to a run one"* — was true of the policy.
2. **The "pre-existing site figures" framing was incomplete.** 6 stale measured at setup; 10 at N7,
   every one a figure this phase moved. Now 14/14.
3. **Floor 3's sequence was insufficient** — DC-115 above.

**Budget:** 300 main-line calls declared, ~290 spent and passed. GO9 makes a firing cap a finding
about the estimate, not a licence. Recorded, not absorbed.

## The loomkeeper capture

`.agents/log/18fe7a5a-….jsonl` carries an `episode-open` and an `episode-close`
(`outcome: Completed`, `artifacts: docs/proof/conductor-agent-plane.md` — verified to exist and to
sit under `docs/proof/`, which the contract requires).

**Recorded honestly: `AIDE_CONTRACT_LOG` is unset**, so this is the committed stand-in, and the
session never registered with the watcher — the contract drops an episode-open from an unregistered
session. The line is **reachable forward but will not be scored now**. Status is
**"not recorded", never "captured".** Before today, a grep for `loomkeeper/1` across `.agents/log/`
returned **zero**; this is the first episode the channel has ever carried.

## Owner's five conditions, carried into Phase 2

1. **No scored governed episode in a linked worktree** of the operator's checkout until DC-115 has a
   control with a red-first test. A `Not Scored` verdict recorded under that shape is an
   **EvaluatorIntegrity trip and goes to the human**, not to the Owner.
2. **Phase 2's gate policy replaces "by subject": the full gate set runs at every node close.** A
   cheaper policy is admissible only if it makes a skipped gate **visibly different** from a run one.
3. **Phase 2's budget is estimated from the three measured node durations** (N4 1931 s, N5+N6 1374 s,
   N7 2404 s), never carried over as 300. The Phase 1 overrun is recorded as a finding about the
   estimate.
4. The close's audit entry records the channel as **"not recorded — `AIDE_CONTRACT_LOG` unset"** and
   names the Proof Pack as the artifact.
5. **Phase 2 N0 opens with `audit-log.py start`** and closes with its own append **before any Phase 2
   code is authored.**

**Phase 2's mandatory first node (N0)** holds exactly: the curated `/document` half (AgentPlane
architecture doc + ADRs) · `/session-profiler` over this run and this phase · a control for DC-115.

Deferred with re-entry triggers in the plan's Deferrals table, **not left as residual-list debt**:
Budget/FanOutCap enforcement · shell-write lease coverage · the two-doors evidence asymmetry.

**Below threshold, recorded:** one `AiDe.App.Tests` host hang (18 min, no result file, cause
unknown). **If it recurs in Phase 2 N0 it becomes a class before any other Phase 2 work proceeds.**
