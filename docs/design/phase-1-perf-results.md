---
id: perf-results-phase-1
title: "P1-PERF — Phase-1 performance gate results"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [performance, benchmark, phase-1, evidence]
links:
  - { to: design-phase-1-walking-skeleton, rel: documents }
  - { to: architecture, rel: documents }
  - { to: proof-pack-phase-1-walking-skeleton, rel: relates-to }
review-by: 2027-02-26
review-suggested: []
summary: >-
  The measured Phase-1 performance run that promotes the architecture's Inferred targets to Verified:
  refresh, describe, impact, find, knowledge, query plans, and restore RTO on the 50,000-edge corpus —
  plus the append-only growth curve, which shows the refresh budget failing after ~10 generations.
---

# P1-PERF — Phase-1 performance gate results

**This run promotes the architecture's Phase-1 targets from Inferred to Verified — with one
exception, recorded below, that no reading of these numbers should gloss over.**

## Measurement environment

| | |
|---|---|
| Host | Windows 11 Pro 10.0.26200 |
| CPU | Intel Core Ultra 9 275HX — 24 cores / 24 threads |
| RAM | 127.4 GB |
| Disk | NVMe HFS002TEJ9X101N |
| Runtime | .NET 10.0.11, **Release** build, no debugger attached |
| Harness | `bench/AiDe.Bench` — `dotnet run --project bench/AiDe.Bench -c Release` |
| Corpus | `bench-corpus-v1` — deterministic seed 20260826 |
| Samples | 30 per measurement (the architecture's stated minimum), warm; cold reported separately |
| Percentiles | nearest-rank (never interpolated — at N=30 an interpolated p99 would invent a value between two observations) |
| Date | 2026-08-26 |

**Corpus shape.** 50,000 edges over ~10,000 distinct nodes, with 20 high-degree hubs, a 500-deep
chain, and one third of assertions `Inferred` so the weakest-status fold is exercised at scale. The
architecture states the corpus as "10,000-assertion / 50,000-edge"; in this fact model one assertion
*is* one edge, so those are two numbers for the same thing. The harness reads them as the two budgets
they actually gate: **refresh** = committing one 10,000-assertion scope snapshot; **query** = reading
against the full 50,000-edge graph.

## Results

| Measurement | p50 | p95 | p99 | Budget | Verdict |
|---|---:|---:|---:|---:|---|
| Refresh 10k assertions (fresh store) | 167.03 ms | **220.71 ms** | 260.33 ms | p95 < 500 ms | **PASS** |
| `describe` hottest hub, maxNeighbors=50 | 3.85 ms | **5.76 ms** | 5.86 ms | p95 < 100 ms | **PASS** |
| `impact` hub, 200 nodes / 500 edges | 20.38 ms | **23.56 ms** | 33.92 ms | p95 < 250 ms | **PASS** |
| `impact` deep chain | 11.49 ms | **14.69 ms** | 20.83 ms | p95 < 250 ms | **PASS** |
| `find` substring, maxResults=50 | 55.71 ms | **61.40 ms** | 63.47 ms | p95 < 100 ms | **PASS** |
| `knowledge` projection | 0.44 ms | **0.51 ms** | 0.62 ms | p95 < 100 ms | **PASS** |
| Restore + full 50k-claim rebuild | — | **0.35 s** | — | RTO < 15 min | **PASS** |

Cold (first-call) figures: describe 32.14 ms, impact 21.06 ms — page-cache and JIT warm-up only,
reported separately so they cannot inflate a warm distribution.

**Query plans (P1-PERF-04)** — no bounded read scans the fact table:

```
describe/impact by subject   SEARCH evidence_assertion_fact USING INDEX ix_assertion_subject (subject=? AND scope_id=? AND generation=?)
describe by object           SEARCH evidence_assertion_fact USING INDEX ix_assertion_object  (object=? AND scope_id=? AND generation=?)
latest committed snapshot    SEARCH scope_snapshot_committed_fact USING INDEX (scope_id=?)
current assertions (join)    SEARCH a USING INDEX ux_assertion_natural (scope_id=?)
```

## The finding this run exists to surface

**The refresh budget holds on a fresh store and stops holding in ordinary use.** Because the fact
store is append-only, re-extracting the same scope leaves every prior generation in the table, and
index maintenance grows with it:

| Prior generations of the same scope | Refresh p95 | vs 500 ms budget |
|---:|---:|---|
| 0 | 192.23 ms | within |
| 5 | 482.69 ms | at the edge |
| 10 | 566.81 ms | **over** |
| 20 | 784.58 ms | **over by 57%** |

A workspace that re-extracts a scope roughly ten times — a morning's editing — is already outside
the budget, and the cost keeps climbing. This is not a regression; it is the append-only design's
inherent cost, and it was invisible until measured.

**Mitigation exists but nothing triggers it.** The conceptual model already specifies retention
compaction (rebuild-and-swap from retained facts, never trigger-bypassing deletes). What does not
exist is a *policy that fires it* — no generation-retention rule, no compaction schedule, no health
signal when a scope's generation count crosses a threshold. Until that lands, the honest statement of
the Phase-1 refresh budget is: **p95 < 500 ms for the first ~5 generations of a scope, degrading
linearly thereafter.**

Recorded as: `docs/lessons/defect-classes.md` DC-010, Phase-2 work item, and a flagged risk in the
architecture. **No architecture text should describe refresh as meeting its budget without this
qualifier.**

## What changed to reach these numbers

The first run of this gate **failed 5 of 8 budgets** — describe 399 ms, impact 418/421 ms, find
475 ms, knowledge 450 ms, all against budgets of 100–250 ms. The `simplify:` marker recorded in the
Phase-1 design ("in-memory neighbour index rebuilt per query; ceiling ~50k edges; upgrade when
P1-PERF p95 impact > 250 ms") fired exactly as written.

The cause was precise and worth stating, because the query plans had looked fine all along: **the
indexes were correct and unused.** `ProjectionService` called `AllCurrentAssertions()` and filtered
in LINQ, so every bounded read paid a full-corpus materialization (~350 ms) regardless of how small
its result was. The fix pushed filtering into indexed SQL:

- traversal indexes reordered so the **traversal column leads** (`subject, scope_id, generation`) —
  with `scope_id` leading, a lookup by node could not use the index;
- a `predicate` index added for the knowledge projection;
- `AssertionsTouching` / `OutgoingAssertions` / `AssertionsWithPredicate` / `SearchNodeIds` added to
  `StoreReader`, each bounded in SQL;
- `describe`/`impact`/`find`/`knowledge` rewritten to use them. `AllCurrentAssertions` remains, used
  only by the claim-cache rebuild where the whole set genuinely *is* the answer.

Resulting improvement: describe 69×, knowledge ~880×, impact 12–29×, find 7.7×. All 59 behavioural
tests stayed green throughout — the change is a refactor, not a behaviour change.

## Not measured by this run

- **P1-PERF-05** (32 producers, 100 scopes, 200 events/s for 60 s, settlement histograms) — **not
  runnable**: the ingestion scheduler and its queue do not exist yet (Phase-1 remaining work). The
  settlement SLI it would measure has no emitting source, so running a load test now would produce a
  number with nothing behind it.
- **Refresh-failure path** (`P1-PERF-01`'s stale-last-successful oracle) is covered by
  `MalformedArtifact_KeepsLastGoodSnapshotAndRaisesAnIncident` in the unit suite, not here.
- **WAL checkpoint lag under sustained long reads** — the design accepts this as a bounded residual;
  no sustained-reader scenario was exercised.
- **Scale beyond 50,000 edges.** These numbers say nothing about 500,000. The `simplify:` ceiling has
  moved, not disappeared.
