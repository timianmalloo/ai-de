---
id: note-conn-10-11-episode-source-blocker
title: "conn-10/conn-11 are blocked on an episode-lifecycle source + verification telemetry"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, scoring, dispute, blocker, conn-10, conn-11, decision-note]
links:
  - { to: spec-agentic-watcher-substrate, rel: relates-to }
  - { to: design-watcher-weave-score, rel: relates-to }
  - { to: design-watcher-session-emitter, rel: relates-to }
review-by: 2027-02-26
review-suggested: []
summary: >-
  conn-10 (auto-score-on-close) and conn-11 (raise-dispute + cloud judge) cannot ship honestly yet: no
  terminal session opens a goal/done-when Work Episode, and there is no telemetry convention for observing
  a verification path - so a deterministic signals deriver could only ever return HasVerificationPath=false,
  which the scorer correctly renders Not-Scored, and disputes have no scored episode to target. Deferring
  both behind an episode-lifecycle capture slice rather than fabricating signals (spec L127; no-guessing).
---

# conn-10 / conn-11 are blocked on a real episode-lifecycle source

## Decision

**Defer conn-10 (auto-score-on-close) and conn-11 (raise-dispute command + cloud judge)** until a Work
Episode lifecycle exists for terminal agent sessions. Do **not** ship a signals deriver or an auto-score
hook that fabricates the signals it cannot observe.

## Why (the evidence)

Grounding in the scoring code and the spec makes the blocker concrete, not a matter of effort:

1. **The scorer refuses to score without a verification path.** `WeaveScorer.NotScoredReason`
   (`WeaveScore.cs:169`) returns `"no minimum verification path"` unless `signals.HasVerificationPath`
   is true - and returns `"no goal"` / `"no done condition"` / `"the episode is not closed"` before that.
   So a real score requires a closed episode **with a declared goal + done-condition** and an
   **observed verification path**.
2. **No terminal session opens such an episode.** `WorkEpisode.Open(...)` needs a `Goal` and a
   `DoneCondition`. Nothing in the tool captures an agent's declared goal/done-when for a terminal
   session, so there are **no closed episodes to score**. Auto-score-on-close would never fire.
3. **There is no telemetry convention for observing verification / acceptance / guidance.** Of the 14
   `DeterministicEpisodeSignals` fields, only a few are deterministically observable from a terminal
   session (e.g. spans after the close time -> `ActionsAfterDoneCondition`). `HasVerificationPath`,
   `RequiredVerificationExecuted`, `AcceptanceCriteriaMet`, the guidance-trigger counts, and
   `RegressionPresent` have **no observable source** in the current spans/coordination. A deriver would
   have to set `HasVerificationPath=false` honestly -> the scorer renders **Not-Scored** for every real
   episode.
4. **The spec forbids fabricating the gap shut.** Spec L127: *"every missing signal renders Not [scored/
   comparable]"* and L141 separates deterministic signals from advisory ones. Setting
   `HasVerificationPath=true` (or inventing a goal/done-when) to make a score appear would be exactly the
   assumption-as-fact failure the watcher exists to catch - and a No-Guessing-Protocol violation (NG1).
5. **conn-11 depends on the same prerequisite.** `DisputeService.RaiseDispute(episodeId, ...)` disputes a
   **scored** episode; with no scored episodes, a dispute command has nothing to target. The cloud judge
   (`DelegatingAdvisoryEvaluator` behind `EgressGuardedAdvisoryEvaluator`) additionally needs an operator
   egress opt-in + credentials that are out of scope for a local smoke test.

## The unblocking slice (the real next step)

**Episode-lifecycle capture** - open a `WorkEpisode` when a session declares a goal-state
(Goal / DoneWhen / NotInScope - the ai-forward `done_when` structure the audit log already records, AL5b),
observe its interval, and close it (Completed / Abandoned / ...) when the done-condition is met or the
session ends. Only once episodes with declared goals exist does an honest signals deriver have inputs,
and only then does auto-score (conn-10) produce anything but Not-Scored. A verification-path telemetry
convention (a span/marker the harness emits when it runs its checks) is the second input that lifts a
score above Not-Scored.

## Alternatives rejected

- **Ship a deriver + auto-score now (dormant):** speculative machinery with no live input (YAGNI); every
  card it could produce today is Not-Scored, so it proves nothing and adds surface to maintain.
- **Fabricate a goal/done-when from the session (e.g. "complete the terminal task"):** invents the very
  goal-state the score is meant to measure adherence to - a No-Guessing violation and self-defeating.
- **Set HasVerificationPath=true so a number appears:** manufactures a score with no evidence; the spec's
  Not-Scored rule exists precisely to prevent this.

## Confidence

Verified - grounded in `WeaveScore.cs:169` (NotScoredReason), `WorkEpisode.cs:31/54` (Open needs
Goal+DoneWhen), `WeaveScore.cs:72` (the 14-field signal set), and spec L127/L141.
