---
id: backlog-watcher-telemetry
title: "Watcher telemetry — backlog"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [watcher, telemetry, backlog, continuous-improvement]
links:
  - { to: spec-agentic-watcher-substrate, rel: relates-to }
  - { to: design-watcher-signals-telemetry, rel: relates-to }
  - { to: design-watcher-advisory-evaluator, rel: relates-to }
review-by: "2026-11-30"
summary: >-
  Cross-session backlog for the watcher scoring/telemetry loop. The writer half
  (audit-log.py emits an honest signals object) and the /implement emitter wiring are
  DONE and live in ai-de; these are the remaining next steps a future session picks up.
---

# Watcher telemetry — backlog

**Context.** The watcher scoring loop is now closed end-to-end for the four *deterministic*
dimensions: an `/implement` turn emits honest signals (`--signal-acceptance-met`,
`--signal-verification-path`, `--signal-verification-executed`) at its audit close →
`audit-log.py` writes an optional `signals` object (AL2a) → the watcher's
`DeterministicSignalsDeriver` (`src/AiDe.Core/Watcher/DeterministicSignalsDeriver.cs`) reads
it → the imported episode scores `OutcomeIntegrity`, `FocusAndTermination`,
`GuidanceAdherence`, `CoordinationAndLearning` instead of staying at its conservative default.

Everything below is what remains. Each item states **what**, **why**, its **gating/prereqs**,
the **seam** (where the code already waits), and an **acceptance** condition. Honesty rule
throughout (spec L127 / NG1): never emit or fabricate a signal that was not actually observed —
absent stays a conservative default, never a guessed score.

---

## B1 — Advisory-dimension activation (GATED on calibration data)

**What.** Make the two *advisory* dimensions — `EvidenceDiscipline` and `SolutionEconomy` —
fold into the verdict, so an episode can reach a full 6-dimension **Scored** verdict rather than
**Partial**.

**Why.** The deterministic dims describe *whether* the work closed correctly; the advisory dims
describe *how well* it was done (evidence quality, solution economy/elegance). They are the half
of the "agentic score" that judges craft, and they are currently dormant.

**Gating / prereqs (this is why it is not done):**
1. **Human-labelled calibration data** — a set of episodes with human scores for the two advisory
   dims, so `CalibrationRegistry.Qualify(evaluatorVersion, taskClass, schemaVersion)` can qualify
   the evaluator against real ground truth rather than a test bypass.
2. For the *cloud* judge specifically: credentials **and** an explicit egress opt-in (the local
   heuristic needs neither — it is on-device).

**Seam (already built and dormant — do NOT rebuild):**
- `AdvisoryWeaveScorer` folds the advisory dims only when the evaluator is qualified in the
  registry (ADR-0019 advisory-evaluator-calibration).
- `LocalHeuristicAdvisoryEvaluator` (`EvaluatorVersion = "local-heuristic/1"`) is the on-device,
  no-egress evaluator; the cloud judge is the same seam behind the egress opt-in + creds.
- `WatcherHost.ImportAndScoreEpisodesFromAuditLog(path, taskClass, evaluator?, registry?)` already
  accepts the optional evaluator + registry; `WorkbenchShell` calls the default (deterministic-only)
  overload because there is no calibration data yet — that is the correct honest state.

**Acceptance.** With calibration data loaded and `local-heuristic/1` qualified for a task class, a
fully-instrumented episode of that class reaches a **Scored** (6-dim) verdict; an un-qualified or
un-instrumented one still scores conservatively. The fold-when-qualified vs excluded-by-default
behaviour is already covered by the advisory-seam test — extend it with a real calibration fixture.

---

## B2 — Live end-to-end smoke (OPTIONAL, high-value, ungated)

**What.** Run a real ai-de `/implement` turn (which now emits the honest signals at close) and
confirm in the watcher UX that the imported episode scores its four deterministic dimensions
rather than staying conservative.

**Why.** The loop is proven by contract + both halves' unit tests, but a live run is the E11
"prove the rendered surface" check — it exercises the real writer → real audit log → real
`DeterministicSignalsDeriver` → real Workbench watcher pane, end to end.

**Gating.** None. The writer and emitter are live on `main`.

**Acceptance.** A closed `/implement` episode with an emitted `signals` object shows a
Partial-with-4-deterministic-dims (or better) score in the watcher UX, and an un-instrumented
episode shows Not-Scored/conservative — visibly distinct.

---

## B3 — Extend the honest emitter to `/investigate` (OPTIONAL)

**What.** Wire the `/investigate` skill's audit-close step to emit the same honest signals it can
observe — a verified root-cause fix has a *verification path* (the fix's test) and an *acceptance*
(the defect no longer reproduces).

**Why.** `/investigate` is the other skill whose close carries a genuine Proof-Pack-equivalent, so
its episodes could score the deterministic dims too. Symmetric with the `/implement` wiring already
landed.

**Gating.** None — same pattern as the `/implement` wiring.

**Seam.** Mirror the `/implement` change: canonical `pack/commands/investigate/SKILL.md` +
`pack/adapters/copilot/prompts/investigate.prompt.md` in ai-forward, then vendor the delta into
ai-de's `.claude/skills/investigate/SKILL.md` + `.github/prompts/investigate.prompt.md`. Emit a
signal **only when genuinely true** (a fix that was not verified omits `--signal-verification-path`).

**Acceptance.** An `/investigate` run that verified its fix emits `--signal-acceptance-met true`
+ `--signal-verification-path true`; one that stopped at a hypothesis omits them.

---

## Cross-repo note

The canonical writer + `/implement` emitter live in the **ai-forward** pack (`pack/scripts/`,
`pack/commands/implement/`, `pack/adapters/copilot/prompts/`); ai-de carries a **surgically
vendored** copy of just the signals delta. A future full `/updatepack` will reconcile ai-de's
vendored pack with ai-forward wholesale — until then, keep cross-repo changes delta-scoped
(register: PACK-P, and the DC-id collision hazard when multiple sessions append to the register).
