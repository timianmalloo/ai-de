---
id: review-phase-2-exit
title: "Phase-2 exit review — real code, terminal, and process split"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [review, phase-gate, phase-2]
links:
  - { to: design-phase-2-real-code-and-terminal, rel: relates-to }
  - { to: architecture, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-02-28
summary: >-
  Phase 2's three components are built and measured. Every capability the phase promised is
  demonstrable, four gates are green over 541 tests, and the three performance budgets are met with
  large headroom. Five residual risks are carried into Phase 3, three of them by explicit decision.
---

# Phase-2 exit review

**Reviewed 2026-08-28.** The phase promised: *inspect a real C# solution; operate one real terminal
beside a derived view; run the core as a separate daemon with upgrade and rollback.*

## Against the phase's own exit criteria

| Criterion (from `architecture.md`) | State | Evidence |
|---|---|---|
| Select a source type from a real C# solution | ✅ | `CSharpExtractor` indexes this repository: 1,281 assertions from `AiDe.Core`, queryable through the same projections every pane uses |
| Launch `pwsh`, observe real session state | ✅ | ConPTY runtime + OSC 133 with a per-session nonce; measured Ready → Busy → Ready through a real pseudo console |
| Terminal text never enters the graph | ✅ | `P2-PRIV-01/02` — seeded markers proven absent from every store, log, metric and span |
| Perform an upgrade and an injected-failure rollback | ✅ | `P2-UPGRADE-01..03` — snapshot → journal → migrate → gate → commit, with startup recovery |
| Run the core as a separate daemon | ✅ | `AiDe.Daemon.exe` over an owner-SID-restricted named pipe; reads, the first write, and now prompt dispatch all cross it |

## What the numbers say

| Gate | Budget | Measured |
|---|---|---|
| `P2-PERF-01` scope settlement | p95 < 10 s | **619 ms** (`AiDe.Core`), **259 ms** (`AiDe.App`) |
| `P2-PERF-02` daemon boundary | describe p95 < 100 ms | **0.92 ms** — a ~0.35 ms flat tax, 0.35% of budget |
| `P2-PERF-03` terminal throughput | 1 MiB/s sustained | **1.00 MiB/s held for 10 s**, parse p95 1.85 ms |
| Terminal redraw | p95 < 16.67 ms | **5.50 ms** for 200×50 |
| Test suite | — | **541 executing**, four gates green |

Every budget is met by an order of magnitude or more. That is worth stating plainly *and* discounting
appropriately: these are single-client, single-connection, one-machine measurements on a repository of
five projects. They bound nothing about a large solution, concurrent shells, or a saturated lane.

## What this phase learned the hard way

Three findings changed decisions rather than just fixing bugs, and each was found by measurement
after reasoning had already produced a confident wrong answer:

1. **`MSBuildWorkspace` executes repository code** (D3). Four vectors, two needing nothing but a
   checked-in `.csproj`. The design had carried "analyzers can be suppressed" as though it closed the
   boundary. Registered **DC-019**; the extractor now never runs MSBuild at all.
2. **A refusal that was correct in-process became a shared outage** (DC-020). A stale-epoch dispatch
   threw, escaped the handler and the listen loop, and would have killed the daemon for every shell
   on the workspace. Found by a test written to assert something else entirely.
3. **A guard that watched by name missed a name that moved** (DC-018). For four commits the IPC
   boundary, terminal runtime and upgrade coordinator emitted spans no privacy assertion could see.

The pattern across all three: **the control was real, and its scope was narrower than the belief
about it.**

## Residual risks carried into Phase 3

| Risk | Disposition | Why it is acceptable now |
|---|---|---|
| **Option B fidelity on unmet project shapes** | **Live** | 100% edge resolution measured on four shapes including `ProjectReference`, WPF and multi-targeting — but not on shared projects, `Directory.Build.props` inheritance, or `Compile Link`. **Fidelity failures in an extractor are silent**, which makes this the most dangerous open item |
| **A2 sandbox network egress** | Deferred by decision | Only matters if the low-integrity sandbox is ever adopted; Strategy 1 means it is not on the shipping path |
| **Cross-monitor DPI** | Accepted, non-blocking (D5) | Hardware-gated; the DPI arithmetic has evidence at 150%, the monitor-transition case does not |
| **No terminal scrollback** | Accepted (D6) | Stated product limitation with a designed upgrade path |
| **`P2-FOCUS-03` does not cover the OS→browser hop** | Stated | Keys are injected at the renderer's input layer, so a host that swallowed a key before the browser saw it would not be caught |

## What is built but not yet reachable by a user

Honest accounting, because "the code exists" and "the product does it" are different claims:

- **Upgrade and rollback** have no UI. The choreography is tested; nothing in the shell triggers it.
- **MCP tools** are registered and tested but not exposed through the app.
- **The canvas shows one node's neighbourhood**, chosen by `Find`. There is no navigation, no
  layout algorithm, and no way to pick a different root from the page.
- **`ADR-0010` stays `proposed`** until dispatch has been exercised against a real agent session
  rather than a terminal.

## Verdict

`GATE phase-2-exit · 2026-08-28 · Test Architect (541 tests; every phase criterion has a named test; P2-FOCUS-03's environmental limit is stated rather than papered over); Security & Identity (D3's finding closed by removing the mechanism, not by containing it; DC-019 and DC-020 registered with controls); Data & Persistence (scope grain settled at (project, target framework) on measured evidence; no new fact table; disclosures carried as facts); SRE (three budgets measured, all met, scope of the measurement stated); Distributed Systems (the two-phase receipt survives the process split; the enlarged crash window is tested); Privacy (seeded markers; DC-018's gap closed and controlled); Simplifier (the extractor replaced a dependency with ~250 lines of project-file reading at full measured fidelity) · verdict: **PASS-WITH-CONDITIONS** · conditions: the Option-B fidelity spike is extended to shared projects and Directory.Build.props before a non-AiDe repository is indexed in anger; ADR-0010 is promoted only after dispatch runs against a real agent session · authors did not self-clear.`

**Handoff:** → Phase 3 (architecture/data/infra joins), with the extractor's fidelity spike as the
first owed item rather than a Phase-3 feature.
