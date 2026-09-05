---
id: architecture-loomkeeper
title: "Loomkeeper Watcher Substrate - Architecture"
type: architecture
status: draft
owner: "@timianmalloo"
phase: "discovery"
tags: [loomkeeper, agent-observability, architecture, scoring, leaderboard, daydream, watcher]
links:
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: architecture, rel: refines }
  - { to: kb-agentic-session-observability, rel: depends-on }
  - { to: adr-0002-workspace-fact-store, rel: depends-on }
  - { to: adr-0001-derived-evidence-views, rel: depends-on }
  - { to: adr-0006-terminal-delivery-semantics, rel: depends-on }
  - { to: adr-0007-agent-session-adapter, rel: depends-on }
  - { to: adr-0011-session-processing-class-egress, rel: depends-on }
  - { to: adr-0016-bounded-context-declaration, rel: depends-on }
  - { to: adr-0017-watcher-observation-projection, rel: depends-on }
  - { to: adr-0018-credential-backed-grading-egress, rel: depends-on }
  - { to: adr-0019-advisory-evaluator-calibration, rel: depends-on }
  - { to: adr-0020-trusted-registrar-harness-model-identity, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Top-level architecture for Loomkeeper, the local agentic watcher subsystem. It observes many
  terminal-agent sessions across repositories by composing the existing AI-DE fact store, derived
  views, delivery semantics, session adapter, and egress governance, and adds a trusted registrar,
  harness/model attribution, a calibrated advisory evaluator, a leaderboard, per-turn standing
  feedback, and a human-gated Daydream learning loop - local-only by default.
---

# Architecture: Loomkeeper Watcher Substrate

- **Status:** Draft
- **Tier:** T2 (LOA archetype **G Continuous Sentinel**, composed with **D Grounded Synthesizer** and **H Long-Horizon Agent** for Daydream memory only)
- **Driving spec:** [`docs/specs/agentic-watcher-substrate.md`](../specs/agentic-watcher-substrate.md)
- **Author / date:** @timianmalloo · 2026-08-30
- **Baseline:** Loomkeeper is a **subsystem of the AI-DE workbench** ([`docs/architecture.md`](../architecture.md)). It reuses that architecture's local authority core, SQLite fact store, and governance boundaries; it adds observation, evaluation, and learning over agent sessions. This document is the target shape, not a claim of implementation.
- **Grounding traversal:** `spec-agentic-watcher-substrate` (implements) → `kb-agentic-session-observability` (depends-on) → `architecture` (refines) → ADR-0001/0002/0006/0007/0011/0016 (depends-on). No stale or orphaned nodes found; the watcher's durable-store, delivery, identity, and egress needs each already have an accepted decision to extend rather than reinvent.

## 1. Context and the load-bearing constraint

A lead directs several coding agents across worktrees and repositories and cannot see the work as one system: each session is visible one terminal at a time. Loomkeeper joins observation, coordination, evaluation, and continuous learning into one honest surface. The spec fixes the non-negotiable posture (quoted, authoritative): **local-only by default; no personnel scoring; model judgments advisory, never authoritative; missing signals render Not Recorded; learning is human-gated and retractable; credential-backed egress is an explicit opt-in, never a default.**

The load-bearing structural fact: **Loomkeeper is a projection layer, not a new source of truth.** The riskiest failure available is a second store that drifts from the repositories it watches (DM6; the "second source of truth" anti-pattern). The whole shape below exists to keep Loomkeeper a *reader and projector* of facts the sessions and the existing daemon already produce.

## 2. The system as a system (stocks, flows, feedback, delays, boundary)

- **Stocks (what accumulates):** append-only observation facts (spans, board messages, work episodes, evidence records, scorecards, daydream observations); stable dimensions (repository, worktree, terminal, agent, **harness**, **model**, session generation); the promoted-learning register; the calibration/validation corpora.
- **Flows:** session registration and heartbeats; telemetry spans from adapters; board posts; grader evaluations; per-turn standing feedback back to agents; deletion/retraction.
- **Feedback loops:** heartbeat→liveness; evidence→score→standing→next-turn behaviour (the learning loop, bounded by the anti-Goodhart counter-metrics, §7.2); observation→candidate→promotion→recurrence measurement; ingest backpressure (bounded queue, ADR-0002).
- **Delays:** grader latency (advisory, off the critical path); eventual consistency of the cross-repo fleet view; the deliberate human gate before promotion.
- **Boundary drawn:** one machine, v1. **Inside:** local observation, evaluation, learning, and the Observatory UI. **Outside (excluded, deny-by-default):** multi-host operation, cloud sync, external OTLP export, hosted grading, and personnel analytics — each requires a new spec and privacy/security review. Credential-backed off-device grading is the *one* boundary crossing the user may explicitly open, per-path (§7.1).

**Leverage point:** the highest-leverage decision is **where the durable facts live** (ADR-0017 watcher-observation-projection), because it is the only decision here that a schema migration cannot cheaply reverse. Everything else — graders, leaderboard, UI — is a derived view or a replaceable component.

## 3. Candidate shapes considered

1. **Standalone watcher daemon with its own database** (own store, own ingest). *Rejected:* creates a second source of truth that drifts from the repositories, duplicates the daemon's ingest/queue/identity machinery, and re-opens the egress questions ADR-0011 already settled.
2. **Pure UI over the existing daemon, no new persistence.** *Rejected:* the watcher needs cross-repo aggregation, scorecards, and a learning register that outlive a single workspace daemon; a UI-only shape cannot hold them.
3. **Chosen — an observation projection composed into the existing local authority core.** Loomkeeper adds watcher facts and dimensions to the per-workspace fact store (ADR-0002), reuses derived views (ADR-0001), delivery idempotency (ADR-0006), the session adapter (ADR-0007), and egress governance (ADR-0011); it adds a **trusted registrar**, an **advisory evaluator**, a **leaderboard**, **standing feedback**, and a **Daydream loop**, plus a **read-only fleet aggregator** across per-workspace stores. This is the smallest shape that is still complete, and it makes the AI-Forward symbiosis structural rather than aspirational.

## 4. Components and boundaries

```
                 ┌────────────────────────── AI-DE workbench (existing) ──────────────────────────┐
 agent sessions  │  Adapters ── Trusted Registrar ──►  Ingest queue (ADR-0006 idempotent)         │
 (Claude Code,   │   │ native OTLP / injected contract / coord-core append                        │
  Copilot, …)    │   ▼                                                                            │
                 │  Per-workspace SQLite fact store (ADR-0002)  ◄── dimensions: repo/worktree/    │
                 │   • facts: span, board msg, episode, evidence, scorecard, daydream obs          │
                 │   • dims:  …/terminal/agent/HARNESS/MODEL/generation                            │
                 │   │                                                                             │
                 │   ├──► Deterministic Projection Engine (T0) ── liveness, trace/trajectory,      │
                 │   │      Weave composition, hard floors, Evidence Coverage, LEADERBOARD (ADR-1) │
                 │   ├──► Advisory Evaluator (T2, local rubric grader) ── qualitative dims +        │
                 │   │      Candidate Lessons, gated by calibration (ADR-0019 advisory-evaluator-calibration)                     │
                 │   ├──► Standing Composer ── per-turn rank + why → agent (via injected contract) │
                 │   ├──► Daydream Loop (H, persisted) ── observation→candidate→human gate→promote  │
                 │   └──► Governance Gate ── capture policy, redaction (fail-closed, pre-persist),  │
                 │          Config + Credentials (DPAPI), Egress opt-in (ADR-0011/0018)            │
                 │                                                                                  │
   Fleet aggregator (read-only, across per-workspace stores) ──►  Observatory UI (WPF, G6)          │
                 └──────────────────────────────────────────────────────────────────────────────┘
```

- **Adapters + Trusted Registrar** (extends **ADR-0007**): bind identity and issue a per-session capability verified on every event (§6, ADR-0020 trusted-registrar-harness-model-identity). Native OTLP where a harness emits it; an **injected coordination contract** for sessions from repositories without the AI-Forward pack; `coord-core` append semantics for AI-Forward sessions (symbiotic, not a second ledger).
- **Fact store** (**ADR-0002**, extended by **ADR-0017 watcher-observation-projection**): the watcher's facts and the harness/model dimensions are additions to the existing store, not a new database.
- **Deterministic Projection Engine (T0):** everything that must be reproducible — identity, liveness, trace/trajectory, Weave composition, the five deterministic dimensions, the hard floors, Evidence Coverage, and the leaderboard. Derived views per **ADR-0001**.
- **Advisory Evaluator (T2):** local rubric grader for qualitative dimensions and Candidate Lessons; **advisory only**, gated by **ADR-0019 advisory-evaluator-calibration**; cannot raise a deterministic failed dimension (LOA P5).
- **Standing Composer:** builds each agent's per-turn rank + one evidence-backed reason per dimension; never exposes the held-out target (spec US-16).
- **Daydream Loop (H):** the only persisted-memory component; observation→candidate→disconfirm→**human gate**→promotion→recurrence measurement; aligns to the Dream/defect-class records.
- **Governance Gate:** capture policy, redaction before persistence (fail-closed), Configuration, credential storage (DPAPI), and the egress opt-in — the T0 gateway that ships in Phase 1 even though the credential-backed grader arrives later (the ADR-0011 lesson: the gate ships before the thing it governs).
- **Fleet aggregator + Observatory UI (G6):** read-only cross-repo projection and the WPF surface designed in `docs/specs/agentic-watcher-substrate.md` Part C and `DESIGN.md`.

## 5. Durable data representation (settled — ADR-0017 watcher-observation-projection)

Loomkeeper **reuses ADR-0002's dimensions + append-only facts** rather than choosing a representation of its own; this is the DM-default (core entities as dimensions, change-over-time as append-only facts) and it makes history and the audit trail *be* the data. Additions:

- **New dimensions:** `Harness` (Claude Code, Copilot, …) and `Model` (Opus 4.8, GPT-5.6 Terra, …), each versioned; both attributes of the Agent Session, never a separate hierarchy level.
- **New append-only facts:** `ObservedSpan`, `BoardMessage`, `WorkEpisode`, `EvidenceRecord`, `Scorecard`, `DaydreamObservation` — grain as declared in the spec's grain table.
- **Type-2 history** for `CapturePolicy`/`ScoringPolicy`/`WatcherConfiguration` (a version in force per repo/interval).
- **Derived, never stored** (ADR-0001): liveness roster, Trace/Trajectory, Weave summary, Evidence Coverage, the **leaderboard**, recurrence counts, and "current learning in force." The leaderboard is a projection over comparable episodes, computed per (task class, score schema version, harness/model) — never a stored ranking.
- **Accepted trade:** current-state reads are "latest row per key" and must be indexed or materialised as a labelled, rebuildable cache (ADR-0002's accepted cost).

## 6. Identity and delegated trust (ADR-0020 trusted-registrar-harness-model-identity, extends ADR-0007)

Terminal output is forgeable (ADR-0007), so identity is **asserted until verified**. The Trusted Registrar binds `repository → worktree → terminal → agent → harness → model → session generation` and issues a **per-session capability** verified on every subsequent event; a process reusing another session's identifier without its capability is **rejected and recorded as a forgery attempt**. Environment-asserted identity is labelled and **cannot satisfy a correctness floor**. A terminal restart yields a **new session generation** that cannot inherit the prior session's liveness, claims, or score. Non-AI-Forward sessions receive the **injected coordination contract** (registration, repository identity, heartbeat, message, telemetry); AI-Forward sessions coordinate through the existing `coord-core` records — one ledger, projected, not duplicated.

## 7. The two riskiest surfaces (front and centre)

### 7.1 Credential and egress model (ADR-0018 credential-backed-grading-egress, extends ADR-0011)

This is the surface that can leak work off the device, so it is designed first and fails closed.

- **Outbound denied by default** at the watcher process boundary (reusing the ADR-0011 egress-deny-by-default posture). The default state is `Egress blocked`.
- **Credentials are local secrets**, sealed with **DPAPI CurrentUser** (the repo's established at-rest mechanism, Engineering Governance §4). They never appear in logs, telemetry, board, score, or learning, and are revocable.
- **Credential-backed off-device grading is an `ExternalProcessing` egress path** in ADR-0011 terms: denied until the user accepts an **explicit, per-path opt-in notice** (purpose, endpoint, data classes). Opting in reclassifies *that one grading path only*; every other path stays local-only. Revocation disables the path, drops the secret, and keeps no derived copy.
- **The gateway ships in Phase 1** (deny-by-default, class-bound), before any component that could egress, and a **red-first negative test** proves a non-opted-in path cannot call out.
- **Spike-gated (S2, S3):** the DPAPI credential lifecycle (S2, low risk — reuse) and, critically, **how process-level outbound denial is actually enforced on Windows/.NET (S3, Flagged — the load-bearing security claim)** must be proven by PoC before Phase 4. Until then the decision is provisional and the credential-backed grader is not built.

### 7.2 Advisory evaluator qualification and task-class calibration (ADR-0019 advisory-evaluator-calibration)

The leaderboard and every advisory dimension are only meaningful if the grader is calibrated; an uncalibrated comparison is worse than none.

- **Separate, versioned corpora:** a calibration corpus and an independently-adjudicated held-out validation corpus, as first-class contract artifacts.
- **Two qualification gates before any advisory dimension contributes points:** (a) stability — 20 repeated evaluations stay in the same discrete 0–4 band ≥95% of the time and never differ by >1 band; (b) human agreement — quadratic weighted kappa ≥0.75 on the held-out corpus. Otherwise the dimension is visible but excluded (Advisory / Not Scored).
- **Comparability is scoped:** comparisons and leaderboard ranks are permitted only within the same calibrated task class and score schema version; a below-cohort or single-human cell renders **Not Comparable**, never a rank.
- **Any change re-qualifies:** an evaluator model, prompt, rubric, schema, or corpus change must re-pass stability, human agreement, prompt-injection invariance, and held-out outcome checks before it can contribute points.
- **Anti-Goodhart:** a visible score rise is not accepted as improvement unless held-out outcome integrity, regression rate, rework, and dispute-overturn are no worse — this is the loop-termination guard on §2's learning feedback, and the reason the Standing Composer exposes evidence and trend, never a single optimizable scalar (spec US-16).
- **Status:** calibration is an open evaluation-methodology risk (spec flagged). The architecture fixes *how* calibration is structured and gated; it does not claim the grader is calibrated. The calibration harness is a Phase-4 deliverable with its own Proof Pack.

## 8. Cross-cutting concerns (designed in, not deferred)

- **Trust / delegated identity:** §6; per-session capability; least-privilege (LOA P11); asserted identity cannot clear a floor.
- **Idempotency:** span ingest is idempotent under duplicate/out-of-order delivery (ADR-0006); a redelivered span does not double-count; deletion/retraction is a resumable process with a receipt, not one cross-aggregate transaction.
- **Observability (self):** Loomkeeper instruments its own ingest lag, event gaps, score coverage, grader cost/latency, failure rate, and learning recurrence (Watcher Health surface); it degrades to "not recorded," never a plausible wrong number (Instrumentation-over-Inference).
- **Failure modes:** watcher offline → sessions continue, observations paused, nothing stale shown as current; grader unavailable → advisory dimensions excluded, deterministic score stands; adapter degraded → Blind Spot / Partially Observed; redaction failure → content dropped before persistence (fail-closed).
- **Determinism at the floor (LOA P2):** identity, liveness, floors, Weave composition, and the leaderboard are T0; the model is confined to advisory qualitative signals behind the ADR-0019 advisory-evaluator-calibration gate.

## 9. LOA conformance (C1–C11, checked)

C1 tier annotation — every component tiered (§4). C2 budget — grader calls carry a token/latency budget (advisory, off critical path). C3 receipts — every evaluation and promotion records inputs, versions, and the acting principal. C5 side-effect protection — model output cannot fire a floor, a promotion, or an egress without a deterministic or human gate. C8 patterns named — Continuous Sentinel + Grounded Synthesizer. C11 principal propagation — the acting harness/model/session is recorded on every score and standing. Security (auth/secrets/PII) and Distributed Systems (idempotent ingest) hard-veto surfaces are addressed in §6–§8; full threat and privacy models remain next-phase conditions.

## 10. Vertical delivery phasing (define whole, ship in slices)

Each phase is a thin end-to-end path (adapter → ingest → store → projection → UI) that deploys, is exercised by automated E2E tests, and is human-demoable. Mocked seams are contracts.

| Phase | User-visible capability it proves | Real vs mocked | How a human validates | Unblocks |
|---|---|---|---|---|
| **1 — Walking skeleton** | One registered session appears in the Sessions treegrid with honest liveness and Not Recorded for everything unproven; the egress-deny gateway is live | Real: registrar, idempotent ingest, fact store, liveness projection, UI row, egress-deny gate. Mocked: grader, cross-repo (single store) | Start a session; watch it register, go Alive, then Stale; confirm nothing off-device is reachable | 2, 3 |
| **2 — Deterministic Weave + floors** | A Work Episode gets a Weave Score from the five deterministic dimensions, with hard floors and Evidence Coverage; Blocked and Not Scored are honest | Real: episode lifecycle, deterministic scoring, floors, coverage. Mocked: advisory dimensions (excluded) | Open a Scorecard; force a floor failure; see the numeric headline suppressed | 4 |
| **3 — Board + cross-repo fleet** | A per-repo append-only Message Board and the repo→session map across ≥2 repositories | Real: board (coord-core append), fleet aggregator over 2 stores | Post/reply/acknowledge; switch repositories; see quarantine of injected content | 5 |
| **4 — Advisory grader + calibration + leaderboard + standing** | Qualitative dimensions and a leaderboard appear once the grader passes the ADR-0019 advisory-evaluator-calibration gates; agents receive per-turn standing | Real: local grader, calibration harness, leaderboard, standing composer, credential-backed egress path (opt-in). Requires S2/S3 spikes green | Run the calibration harness; see an unqualified dimension excluded; open the leaderboard; read a standing | 5 |
| **5 — Daydream + governance surfaces** | Observation→candidate→human-gated promotion; Configuration + credentials + egress opt-in; Privacy & Capture; deletion/retraction | Real: Daydream loop, Configuration, credential store, deletion process | Promote a candidate through the gate; opt into an egress path and revoke it; delete captured data and read the receipt | — |

**Phase 1 is the walking skeleton:** it touches every layer (identity → ingest → store → projection → UI → governance gate) and proves the composition and the deny-by-default posture before any scoring or model exists.

## 11. Confidence ledger and required spikes

| Claim | Evidence | Label |
|---|---|---|
| Dimensions + append-only facts is the right durable store | ADR-0002 accepted; `spikes/sqlite-fact-store` verified | Verified (reuse) |
| Weave/liveness/leaderboard as derived views | ADR-0001 accepted | Verified (reuse) |
| Idempotent span ingest under duplicate/out-of-order delivery | ADR-0006 accepted | Verified (reuse) |
| Asserted-vs-verified identity; authenticated agent ack | ADR-0007 accepted | Verified (reuse) |
| Egress-deny-by-default, class-bound authorization | ADR-0011 accepted | Verified (reuse) |
| Claude Code does not pass OTLP into subprocesses (a Blind Spot source) | KB `agentic-session-observability` (fetched) | Verified |
| **S1** Claude Code / Copilot OTLP + injected-contract ingestion shape | not yet run | **Flagged — spike before Phase 1** |
| **S2** DPAPI CurrentUser credential seal/unseal + revocation | repo uses DPAPI at rest | Inferred — spike before Phase 4 |
| **S3** Process-level outbound-denial enforcement on Windows/.NET | not established | **Flagged — the load-bearing security claim; spike before Phase 4** |
| **S4** `coord-core` append / one-file-per-session alignment for board + injected contract | `coord-core.py` exists in repo | Inferred — spike before Phase 3 |
| Grader is calibratable to QWK ≥0.75 on a real task class | not established | **Flagged — Phase-4 calibration harness with its own Proof Pack** |

No spike results are fabricated: S1–S4 and the calibration claim are named preconditions, and the decisions that depend on them (§7) are explicitly provisional until the PoC runs.

## 12. Residual architectural risk

- **S3 (outbound denial)** is the highest architectural risk: the entire local-only guarantee rests on it. If process-level denial is not enforceable, the egress model must fall back to a stronger control (no network stack registered at all in v1) — recorded as the ADR-0018 credential-backed-grading-egress fallback.
- **Calibration may not reach QWK ≥0.75** for some task classes; those classes stay advisory/Not Scored rather than being force-ranked — an accepted degradation, not a defect.
- **Cross-repo fleet consistency** is eventual; the UI must label a stale or paused repository rather than present it as current.
- Full **threat model, privacy model, and native WPF/AT accessibility proof** remain next-phase conditions inherited from the spec gate.

## 13. Gate record

`GATE define-architecture · 2026-08-30 · reviewers (Adversary Mode): Enterprise Architect, Distributed Systems Architect, Security & Identity, SRE, Data & Persistence Architect, Simplifier, Patterns Expert · exit criteria: archetype + tiers named; durable representation settled as ADR reusing ADR-0002; four new load-bearing ADRs written; two riskiest surfaces designed first; cross-cutting concerns designed in; vertical phasing with a walking skeleton; required spikes named not fabricated · verdict: PASS-WITH-CONDITIONS · vetoes: Security and Distributed Systems hard-veto surfaces addressed by reuse of ADR-0007/0011/0006; conditions — S1/S3 spikes before their phases, full threat/privacy models, and the Phase-4 calibration Proof Pack`

**Handoff:** → `/design` of the Phase-1 walking-skeleton components (Trusted Registrar, idempotent span ingest, liveness projection, the egress-deny gateway).
