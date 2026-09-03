---
id: plan-daydream-and-collaboration-slices
title: "Delivery plan — Daydream and Collaboration, split across two sessions"
type: doc
status: in-review
owner: "@timianmalloo"
phase: "3"
tags: [plan, execution-graph, loomkeeper, daydream, collaboration, parallel-sessions]
links:
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: design-watcher-daydream-dream-seam, rel: depends-on }
  - { to: architecture-loomkeeper, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
review-by: 2027-03-02
review-suggested: []
summary: >-
  The two remaining Loomkeeper tracks split between two concurrent sessions — Daydream to the
  UI/Experience session, Collaboration to the Core session — with the slice and phase breakdown for
  each, the shared files that force sequencing, and the rule for which session touches them when.
---

# Delivery plan — Daydream and Collaboration

Two sessions working in parallel on one repository. This plan exists so the split is a **dispatch**
rather than a hope: it names who owns what, what each slice delivers, and — the part that actually
prevents collisions — which files both tracks need and when each is allowed to touch them.

## Where this starts from

Measured, not recalled. Every watcher service was swept for production callers on 2026-09-02:

| Capability | US | State |
|---|---|---|
| Register, liveness, contract alignment | 1, 2, 5 | Built and reached |
| Message Board | 4 | Built; **writer added today**, unverified in a running build |
| Work Episodes, live and cross-harness | 6 | Built; **live path added today** |
| Weave scorecard, harness/model attribution, leaderboard | 7, 13, 14 | Built and reached |
| Fail honestly | 11 | Built and reached |
| **Per-turn standing** | **16, 8** | `StandingComposer` — **zero production callers** |
| **Cross-repository fleet map** | **3** | `FleetAggregator` — **zero production callers** |
| Advisory evaluators (8 types) | 7 (partial) | **Declared absence** — ADR-0019, Phase 4 |
| **Daydream** | **9** | **No code at all** |
| Configuration, Privacy & Capture, Watcher Health panes | 15, 10, 12 | Not built |

The advisory cluster is the one item on this list that is **deliberately** absent. Everything else
above the line is either done or a gap nobody decided on.

## The split

| Track | Session | Scope |
|---|---|---|
| **Collaboration** | Core (`ai-de-a7`) | US-16, US-8, US-3, US-12, US-15, US-10 — closing the loops that exist and the three unbuilt Observatory panes |
| **Daydream** | UI/Experience (`claude-ui-experience`) | US-9 end to end — observation, candidate lifecycle, the Daydreams pane, and the seam to the offline `/dream` |

**Why this split and not another.** Daydream is one vertical with a single owner and almost no
overlap; the collaboration items are five small independent closes. Splitting the other way would
have put five separate merges into one session's path and one long slice into the other's.

## Phases, and what they mean here

Every slice in both tracks runs the same four phases. They are not ceremony — each exists because
skipping it has cost this project a defect that is in the register.

| Phase | What it produces | Why |
|---|---|---|
| **P0 · Ground** | The consumer read, not assumed | DC-088: choosing a value without reading the consumer recreated DC-086 one layer over |
| **P1 · Red** | A test named for the failure, observed failing | DC-016: a control that cannot fail for the reason it matters |
| **P2 · Build** | The smallest change that turns it green | — |
| **P3 · Prove** | Full suite, gates, register entry if a class emerged, audit entry | A run that left no trace is not finished |

A slice is done when P3 passes, not when P2 compiles.

---

# Track A — Daydream (UI/Experience session)

Authority: US-9 and `design-watcher-daydream-dream-seam`. The staircase the slices build toward:

```
observation ──(recurrence, deterministic)──> candidate
candidate  ──(disconfirming check survived)──> promotable
promotable ──(a human decides)──> promoted
promoted   ──(source corrected/deleted/contradicted)──> retracted or superseded
```

## D1 — The observation engine (pure)

**Delivers:** a pattern signature derived from a closed episode's *typed* signals, and recurrence
detection over a set of episodes. No store, no UI, no I/O.

- **P0** — read `WeaveScorer`'s signal set and `Scorecard`; the signature must derive from
  `TrippedFloors`, `WeaveVerdict`, and unsatisfied guidance triggers, and **never from prose**. That
  is what makes an injection fixture unable to move a signature, inherited rather than re-earned.
- **P1** — one occurrence is not a candidate; two matching episodes are; two *superficially similar*
  episodes with different typed signals are not.
- **P2** — `DaydreamObservation`, `PatternSignature`, `RecurrenceDetector`. New files only.
- **P3** — suite, gates, audit.

**Collision risk: none.** All new files.

## D2 — Persistence

**Delivers:** append-only daydream facts, and the read used by everything after.

- **P0** — read how `RecordEpisode` / `RecordScorecard` are declared and stored, and follow that
  grain exactly rather than inventing a second convention.
- **P1** — an observation is never updated in place; a re-observation is a new row; the fold is
  deterministic on replay.
- **P2** — store interface additions + SQLite schema.
- **P3** — suite, gates, audit.

**Collision risk: HIGH.** `WatcherObservationStore.cs`, `SqliteWatcherObservationStore.cs`. See the
sequencing rule below.

## D3 — The candidate lifecycle

**Delivers:** the staircase, with every landing that can stop the climb.

- **P0** — re-read US-9's five acceptance criteria; each maps to one test.
- **P1** — one occurrence stays an Observation; a candidate with no disconfirming check has
  promotion **disabled**, not discouraged; a refuted candidate is Disconfirmed and stays blocked; a
  deleted source episode returns a candidate below threshold to Observation.
- **P2** — the fold and the state transitions.
- **P3** — suite, gates, **register entry expected** (the recurrence threshold is a declared safety
  floor and needs its basis recorded), audit.

**Collision risk: none.** New files.

## D4 — The Daydreams pane

**Delivers:** the three-stage surface — Observations, Candidates, Promoted — with promotion disabled
until its prerequisites are visible.

- **P0** — read `WatcherBoardPaneViewModel` and follow its read-model shape.
- **P1** — the pane renders each stage's empty state honestly, and never renders a promote affordance
  for a candidate lacking a disconfirming check.
- **P2** — read model, surface kind, menu entry.
- **P3** — suite, craft gate, gates, audit.

**Collision risk: HIGH.** `SurfaceContentFactory.cs`, `WorkbenchCommands.cs`, `MainMenuBuilder.cs`.

## D5 — The seam to the offline Dream

**Delivers:** candidates emitted in `dream.py`'s signal shape, promoted learnings read back, and the
pack treated as an optional **detected** integration.

- **P0** — read `dream.py`'s stager to confirm the shape it accepts. The seam design labels this
  **Inferred** and says a spike must confirm it before anything depends on it. This phase is that spike.
- **P1** — the seam with the pack absent reports absence, and a `dream.py` run producing no parseable
  output is reported as a failure rather than as a clean run (R4).
- **P2** — one-way emit; read-back marks a candidate already-known.
- **P3** — suite, gates, audit.

**Collision risk: none.**

---

# Track B — Collaboration (Core session)

Ordered smallest-first, so the loop that exists gets closed before new surfaces are added.

## C1 — Wire the per-turn standing (US-16, US-8)

**Delivers:** an agent sees its rank, trend and one evidence-backed reason per dimension.

`StandingComposer` is built and tested with **zero production callers**. ADR-0019 does **not** cover
it: the ADR cites per-turn standing *twice in the present tense* as the mitigation that justifies
rejecting "expose the full scoring target to the agent" — an ADR cannot defer the thing it relies on
to reject something else. What ADR-0019 defers is the **evaluators**, and standing needs none of
them: four deterministic dimensions score today and the two advisory ones render *Not Recorded*,
which is exactly what the ADR prescribes.

**The delivery half, which this plan originally missed.** US-16 is written from the agent's seat —
*"As an agent, I want to see how my harness and model are scoring and why, each turn"*, and its
acceptance criterion is that the agent **receives** its standing. The first version of this slice's
P1 said only that a standing is *produced*, which `WatcherLeaderboardPaneViewModel` could satisfy in
an hour while the agent still received nothing and the operator saw it instead. **That is DC-089 one
layer up: not a service with no caller, but an acceptance criterion that does not reach its own
deliverable.** Found by Core in C1's P0, against the spec line rather than against the plan.

The agent's only channels are the five MCP tools (`announce_claim`, `describe`, `find`,
`record_decision`, `record_note`) — of which `describe` and `find` are pulls — and
`AIDE_CONTRACT_LOG`, which is inbound only. So the standing needs an **MCP `standing` tool the agent
pulls at a turn boundary**: it fits the existing `Guarded(caller, name, read)` shape beside two
precedents, inherits the capability check rather than adding one, and keeps the standing a **pull**.
A push would put the scorer's output into the agent's context every turn whether or not it asked,
which is the precise thing ADR-0019's anti-Goodhart section is careful about.

This changes C1's scope from "wire an existing composer" to "add an MCP tool", and adds
`Mcp/McpToolGateway.cs` to the files it touches. That file is Core's alone, so the shared-file table
is unaffected.

- **P1** — an **agent** receives its standing through the tool; a non-comparable harness-model cell
  yields trend and reasons with **no rank**; no aggregate scalar is exposed anywhere; an operator
  view over the same composer is a separate later slice, because US-14's pane already has a shape
  and the agent channel does not exist at all.

## C2 — Cross-repository fleet map (US-3)

`FleetAggregator` is built with zero production callers. Needs ≥2 sources to aggregate, which is the
thing to confirm in P0 before building a surface over it.

## C3 — Watcher Health pane (US-12)

"Let the user watch the watcher." `IngestStats` and `CoordContractStats` already count everything an
operator would ask; `WatcherFingerprint` exists and is used only for change detection. This is a
surface over counters that exist.

## C4 — Configuration and credentials (US-15)

`IAdvisoryCredentialSource` is reachable **within** the advisory cluster. The configuration surface
is a separate question from ADR-0019's calibration deferral and can proceed.

## C5 — Privacy & Capture pane (US-10)

Retention, purpose limitation and deletion, surfaced. The rules already exist in the leaderboard's
cohort and single-operator refusals; this makes them inspectable.

---

# The shared files, and the rule

Both tracks need the same six files. This is where a parallel split actually fails, so it is
explicit rather than left to good intentions.

| File | Wanted by | Rule |
|---|---|---|
| `SurfaceContentFactory.cs` | D4, C3, C4, C5 | **Batch.** Whoever adds a surface adds it alone, claims for the edit only, releases immediately. Never hold across a slice. |
| `WorkbenchCommands.cs` | D4, C3, C4, C5 | As above. |
| `MainMenuBuilder.cs` | D4, C3, C4, C5 | As above. |
| `WatcherObservationStore.cs` | D2, C1–C5 | **Sequence.** D2 lands before Core's store-touching slices, or Core's land first — agreed in the moment, not assumed. |
| `SqliteWatcherObservationStore.cs` | D2 | As above. |
| `WatcherHost.cs` | D2, C1, C2 | As above. |

**A claim is for the edit, never for the area.** `overlaps()` matches by path segment, so a claim on
a directory refuses every file beneath it and blocks the other session entirely. A refused claim is a
**defect signal** — the plan is wrong, not the timing — and the response is to talk, not to wait out
the TTL.

## Ordering that removes the risk rather than managing it

D1 and D3 touch nothing shared. C1 is small and touches the store lightly. So:

1. **Both start immediately** — Daydream on D1, Collaboration on C1. Zero overlap.
2. **D2 and C2 negotiate the store** before either begins.
3. **D4 and C3–C5 negotiate the surface trio** the same way.

The first phase of each track is deliberately the one with no shared file, so the split is real from
the first minute rather than after a coordination round trip.

## One coupling between the tracks, recorded because it is not obvious

C1 ships an agent its own rank. D1 excludes harness and model from a pattern signature so Daydream
cannot produce "this harness tends to…". **Those two are consistent only together.** The leaderboard
protects comparison with a cohort minimum and a single-operator refusal; standing shows an agent a
rank that passed those checks, and Daydream avoids making the comparison at all. If either side
changed — standing exposing something the cohort rule would have blocked, or Daydream keying on
attribution — the pair would stop agreeing and neither change would look wrong on its own.

Whichever session changes one must say so to the other. Noted by Core while reading D1.

## What would make this plan wrong

Recorded so it can be checked rather than assumed:

- **If Daydream needs an agent-supplied observation**, it needs a contract kind and the tracks
  collide on `CoordinationContract.cs`. The design says it does not — Daydream *observes* closed
  episodes and does not receive them — and D1's P0 is where that gets confirmed or falsified.
- **If `dream.py`'s stager does not accept the proposed shape**, D5 changes and the seam design's
  `Inferred` label was doing real work. D5's P0 is the spike that settles it.
- **If the recurrence threshold cannot be given a statistical basis**, D3 ships it as a declared
  safety floor with the basis recorded as absent — never as a number chosen because it looked right.
