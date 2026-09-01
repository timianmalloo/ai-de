---
id: design-watcher-dispute-command
title: "Loomkeeper Raise-Dispute Command + Cloud-Judge Seam (conn-11)"
type: design
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, design, dispute, command, cloud-judge, conn-11, phase-2]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: design-watcher-signals-derivation, rel: depends-on }
  - { to: design-watcher-score-dispute, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  A keyboard-reachable workbench command that raises an append-only operator dispute against the latest
  genuinely-scored episode (US rule 12) via DisputeService - the score is never changed. A Not-Scored card
  is not disputable. Also documents the real cloud judge (DelegatingAdvisoryEvaluator behind
  EgressGuardedAdvisoryEvaluator) as egress-gated, operator-cred future work.
---

# Raise-Dispute Command + Cloud-Judge Seam (conn-11)

## Problem & spec trace

conn-10 makes scored episodes exist. Spec **rule 12**: an operator may dispute a score they disagree with,
**append-only** — the dispute is evidence for review and never changes the score. This slice gives that
recourse a keyboard-reachable command, and documents where the real cloud judge plugs in.

## Design

- **Command** `watcher.raiseDispute` ("Raise score dispute on the latest scored episode", `Ctrl+K, Ctrl+U`,
  `_View` menu) — added to `WorkbenchCommandCatalog`, dispatched by `WorkbenchController.Execute` →
  `RaiseDispute()` → the `RaiseDisputeRequested` delegate the shell sets. Mirrors the existing
  `terminal.new` command pattern exactly.
- **`WorkbenchShell.RaiseDisputeOnLatest(store, time, operatorId, reason)`** (internal, pure, static): the
  most-recently-scored episode **that carries a real verdict** (Not-Scored has no number to dispute) is
  disputed via the append-only `DisputeService.RaiseDispute`. Returns a status message. The instance
  wrapper `RaiseDisputeOnLatestScore(reason)` supplies `_watcherHost.Store`, `TimeProvider.System`, and a
  **fixed local operator id** `"loomkeeper-operator"` (never a human identity — spec: no score grouped by
  an identifiable human).
- **Append-only, score unchanged:** `DisputeService` only `AppendScoreDispute`s; nothing in this path
  mutates a `Scorecard` (rule 12). The dispute is a new fact, not an edit.

## The cloud-judge seam (documented, not wired)

The advisory (qualitative) dimensions are scored by an `IAdvisoryEvaluator`. The **safe default** is the
local heuristic; the **real cloud judge** is `DelegatingAdvisoryEvaluator(version, judge)` wrapped by
`EgressGuardedAdvisoryEvaluator`, folded by `ScoringService.ScoreAndRecord(..., evaluator, registry)` only
when the evaluator has qualified in the calibration registry (ADR-0019, rule 8). Wiring a live cloud judge
is **deferred** and gated on: (1) an **operator egress opt-in** (the guard blocks egress by default — no
episode content leaves the device without consent, spec §Capture/Scoring Governance); (2) **operator
credentials** for the judge endpoint; (3) the evaluator **passing calibration** before its advisory
dimensions may fold. Until all three hold, only the deterministic Weave is recorded — which is exactly
today's honest behaviour. No code change is needed to adopt it: pass a qualified evaluator + registry to
`ScoreAndRecord`.

## Data model

No new persisted shape: composes the existing `ScoreDispute` (append-only dispute log, `design-watcher-score-dispute`)
and `ScoredEpisode`. The dispute log is **append-only** (rule 12) — a dispute is a fact, never updated. No
grain/additivity change.

## Change-surface list (E7)

store (dispute log exists) → DisputeService (exists) → shell helper (new) → Controller command (new) →
catalog + menu builder (new entry, kept in sync — DC-068) → announcer status (existing). No new field
crosses the wire; the dispute pane already reads the log (conn-7).

## Failure-mode analysis

| Mode | Disposition |
|---|---|
| No scored episode yet | Honest message ("no scored episode to dispute yet"), no throw. |
| Only Not-Scored cards | Filtered out — not disputable (a Not-Scored card has no number to dispute). |
| Watcher unavailable (null host) | Honest message ("watcher is not available"), no throw. |
| Empty/whitespace reason | `DisputeService` rejects (ArgumentException) — guarded upstream; the command supplies a non-empty default. |
| Re-dispute the same episode | Append-only — a second dispute is a second fact (rule 12), by design. |
| Command missing from the menu | Caught by the menu-conformance tests (DC-068) before merge. |

## Adversarial analysis (STRIDE-lite)

Trust boundary: the **operator-initiated command** (local, single-user). **Spoofing** — operatorId is a
fixed local constant, not a claimed identity; there is no auth surface (local tool). **Tampering** — the
dispute is append-only; it cannot alter a score. **Repudiation** — the dispute *is* the attributable
evidence (that is its purpose). **Information disclosure** — the dispute reason is the operator's own text,
stored locally; **the cloud-judge seam is the only egress path and is guarded off by default** (opt-in +
creds required). **DoS** — a flood of disputes is append-only local rows; bounded by the operator's own
action. **Elevation** — none. Disposition: mitigations are inherent (append-only, local, no egress without
opt-in); the cloud-judge egress is **consciously deferred** behind the guard.

## Privacy analysis (LINDDUN-lite)

The dispute carries a fixed local operator id (not a human identity) and the operator's own reason text,
stored locally. **Identifiability** — no human identity recorded. **Disclosure** — none added; no egress
(the cloud judge is off). **Non-compliance** — inherits the store's retention. No new personal-data flow.

## Telemetry design

The command returns a status string that the announcer surfaces (the operator question "did my dispute
record?" is answered inline). Degradation: unavailable/no-score returns an honest message, never a wrong
one. No new spans/error-codes/HTTP.

## Test plan (Testing Strategy triggers)

- **D0** on every test.
- **D1 (unit + mutation)** — `RaiseDisputeOnLatest`: appends a dispute against a scored episode under the
  local operator id (mutation: drop the Not-Scored filter → a Not-Scored card wrongly becomes disputable);
  a Not-Scored-only store is not disputable; an empty store yields an honest message, not a throw.
- **Conformance (existing controls)** — the catalog/menu drift is held by
  `MainMenuTests.TheMenuCoversEveryCatalogCommand` + `Phase3SurfacingTests.DeclaredMenusMatchWhatTheBuilderRenders`
  (DC-068).

## Residual risk

The command disputes the *latest* scored episode with a default reason (no per-episode selection UI or
reason prompt yet — a follow-on when an episode-selection surface exists). The cloud judge is documented
but not wired (deferred on egress opt-in + creds + calibration).
