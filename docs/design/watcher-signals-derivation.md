---
id: design-watcher-signals-derivation
title: "Loomkeeper Deterministic Signals Derivation + Auto-Score (conn-10)"
type: design
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, design, signals, scoring, auto-score, conn-10, phase-2]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: design-watcher-episode-capture, rel: depends-on }
  - { to: design-watcher-weave-score, rel: depends-on }
  - { to: note-conn-10-11-episode-source-blocker, rel: refines }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Derives DeterministicEpisodeSignals for an imported closed episode from what is honestly observable - a
  committed Proof Pack artifact sets HasVerificationPath/RequiredVerificationExecuted; acceptance stays null
  (unknown); the rest are honest defaults - then auto-scores on import via ScoringService. An episode with a
  proof pack scores an honest Partial (Focus only, coverage Not-Recorded); one without is Not-Scored. No
  signal is fabricated (spec L127, NG1).
---

# Deterministic Signals Derivation + Auto-Score (conn-10)

## Problem & spec trace

`design-watcher-episode-capture` supplies closed Work Episodes; the WeaveScorer needs a
`DeterministicEpisodeSignals` to score one. This slice derives those signals from what is **honestly
observable** about an imported episode and its committed audit evidence, and auto-scores on import so the
Leaderboard/Standing surfaces populate (US-14/US-16). The governing constraint is the spec's own rule
(L127): **a missing signal renders Not-Scored — it is never fabricated** (No-Guessing NG1).

## The one honest signal we can observe: a committed Proof Pack

The scorer gates a score on `HasVerificationPath` (`WeaveScore.cs:169`) and trips a Correctness floor on
`!RequiredVerificationExecuted`. The **only** deterministic, non-fuzzy verification signal available from
an audit entry is whether the recorded work shipped a **committed Proof Pack artifact** (`docs/proof/*.md`
in the entry's `artifacts`). That is a real file recording an executed verification path — reading it is a
fact, not a guess. So:

| Signal | Derivation | Honesty |
|---|---|---|
| `HasVerificationPath` | `true` iff the audit entry lists a `docs/proof/` artifact | a committed proof pack is real evidence |
| `RequiredVerificationExecuted` | same as above | the proof pack *is* the executed-verification record |
| `AcceptanceCriteriaMet` | **null** (unknown) | we cannot observe acceptance from an audit entry; null ≠ false (no floor trip), and OutcomeIntegrity renders Not-Recorded |
| `RegressionPresent` | `false` | no observed regression; not a claim of "no regression exists" |
| `ActionsAfterDoneCondition` | spans in `[ClosedAt, ∞)` for the session | observable; ~0 for imported episodes |
| `PrematureCompletion` | `false` | not observable from an audit entry |
| guidance/coordination required+observed | `0/0` | not observable → those dimensions render Not-Recorded (Proportional guards `required<=0`) |
| `UnresolvedFloorBlockers` | empty | none observed |
| `CoverageCalibrated` | `false` | no calibrated required-signal total → coverage renders Not-Recorded, never a fake 100%/0% |

## The resulting scores (honest by construction)

- **Proof-pack episode:** passes the gate; no floor trips (acceptance null, verification executed);
  **FocusAndTermination** scores (4 when no work-after-done and not premature); OutcomeIntegrity/Guidance/
  Coordination render Not-Recorded → verdict **Partial** (`Partial: <earned> / <observed weight> observed`),
  coverage Not-Recorded. This is exactly the "low-coverage/Partial until richer telemetry" the design aims
  for — a real, honest number, not a fabricated one.
- **No-proof-pack episode:** `HasVerificationPath=false` → verdict **Not-Scored** ("no minimum verification
  path"). Honest — the episode exists and is visible, but cannot be scored yet.

## Contracts

- **`EpisodeEvidence(bool HasProofPack)`** (new `sealed record`): the observable audit-entry evidence a
  signal derivation needs. Parsed from the entry's `artifacts` (a `docs/proof/` path → `HasProofPack`).
- **`AuditLogEpisodeSource.ParseWithEvidence` / `ReadFileWithEvidence`** → `IReadOnlyList<ImportedEpisode>`
  where `ImportedEpisode(WorkEpisode Episode, EpisodeEvidence Evidence)`. Reuses the ep-capture line parse;
  adds artifact extraction. The existing evidence-free `Parse`/`ReadFile` remain (used by the plain import).
- **`DeterministicSignalsDeriver.Derive(WorkEpisode episode, EpisodeEvidence evidence, IWatcherObservationStore store)`**
  (pure, static) → `DeterministicEpisodeSignals` per the table above.
- **`WatcherHost.ImportAndScoreEpisodesFromAuditLog(path, operatorId, taskClass)`** → records each imported
  episode, derives its signals, and `ScoringService.ScoreAndRecord`s it (deterministic Weave only — no
  advisory evaluator, the safe default). Returns the count. Idempotent: `RecordEpisode` and
  `RecordScorecard` are upserts (a re-import re-scores, never duplicates).

## Data model

No new persisted shape: this composes the existing `WorkEpisode` dimension and `ScoredEpisode` cell
(`design-watcher-weave-score`). The scorecard is a **derived cache** (DM7) — `RecordScorecard` replaces it
on recompute, never historises it. The **grain** is unchanged: one `ScoredEpisode` is exactly one
evaluation of one closed episode under one schema version. `operatorId`/`taskClass` for an imported
episode are a **classification decision** (below), not new state.

### Classification decision (recorded)

An imported episode has no live operator/task-class. Choice: `operatorId = episode.SessionId` (the session
that did the work is the honest grouping key) and `taskClass = "audit-import"` (an explicit, honest class
that marks these as imported rather than live-scored); `harness`/`model` = null (not observed). This keeps
the Leaderboard grouping coherent and never invents a human operator (privacy: no identifiable human).

## Change-surface list (E7)

store (episode + scorecard, both exist) → deriver (new) → host import+score (new) → shell one-shot call
(changed) → Leaderboard/Standing read projection (exists) → UI pane (conn-9 fingerprint already counts
scored episodes, so the pane refreshes). No new field crosses the wire.

## Failure-mode analysis

| Mode | Disposition |
|---|---|
| Entry with no proof-pack artifact | `HasVerificationPath=false` → Not-Scored (honest, by design). |
| Entry with a proof-pack artifact but unmet work | acceptance stays null → no false "met"; Focus still scores from observed drift only. |
| Imported episode has no spans | `ActionsAfterDoneCondition=0` (honest); Focus full unless premature. |
| Re-import / re-score | Upsert by id — no duplicate episode or scorecard. |
| Corrupt audit line | Skipped (ep-capture behaviour, inherited). |
| ScoreAndRecord throws mid-batch | Per-episode try in the shell loop tick (the loop already swallows a tick hiccup); a bad entry does not abort the batch. |
| Operator grouping leaks a human id | operatorId is the opaque session id, never a name (privacy). |

## Adversarial analysis (STRIDE-lite)

Trust boundary: the **audit log file** (read from the workspace). **Spoofing/Tampering** — a hand-edited
audit entry could claim a proof-pack artifact it never had, inflating a session to a Partial score. This is
a **local, single-user** tool reading the operator's own repo (spec: on-device, no identifiable human);
the audit log is committed history the operator already trusts. **Disposition: consciously accept** — the
threat is self-tampering of one's own local record for a non-comparable local score; residual risk is a
locally-inflated Partial that never leaves the device. No secret/PII is read (only artifact paths + goal
text the operator authored). **Information disclosure** — goal/done text is the operator's own; stays
local. **Elevation** — none (pure read + local store write).

## Privacy analysis (LINDDUN-lite)

The episode carries the operator's own goal/done-when text and a session id. **Identifiability** — the
session id is an opaque GUID, not a human identity; `operatorId` is that GUID, never a name (spec: no score
grouped by identifiable human). **Disclosure through telemetry** — none added; the score is a local store
row. **Retention** — inherits the store's retention. No new personal-data flow beyond what ep-capture
already imports. Disposition: the no-identifiable-human rule is upheld by construction.

## Telemetry design

The auto-score path is deterministic and local; the operator question it answers ("how many episodes were
imported and scored") is the method's **return count**, surfaced in the shell (Not-Recorded degradation: a
missing audit log returns 0, never a wrong number). No new spans/error-codes/HTTP surface. A future gap
(named): per-episode score outcome is not yet emitted as a metric — recorded as an instrumentation gap.

## Test plan (Testing Strategy triggers)

- **D0** on every test.
- **D1 (unit + mutation)** — the deriver's honesty mapping: proof-pack→HasVerificationPath (mutation:
  invert → an episode that should score becomes Not-Scored / vice-versa); acceptance stays null (mutation:
  set true → OutcomeIntegrity wrongly scores); no-proof→Not-Scored end to end through the scorer.
- **D4 (real-infra)** — `ImportAndScoreEpisodesFromAuditLog` against a real SQLite host: a proof-pack entry
  yields a Partial scorecard, a no-proof entry yields Not-Scored, re-run upserts (no duplicate).
- **E11/E12** — the score reaches the store's `AllScoredEpisodes`/leaderboard read (the surface conn-9
  refreshes); a cross-check that the scored episode's id matches the imported episode's id.

## Residual risk

Only Focus is deterministically scorable today, so every scored imported episode is a **Partial** — honest,
but thin. Richer dimensions (Guidance/Coordination/Outcome-acceptance) need telemetry conventions that do
not exist yet; deriving them is the next telemetry slice, not this one. Proof-pack presence is a coarse
verification signal (it does not read the proof pack's contents); a committed-but-empty proof pack would
still set HasVerificationPath — accepted (the artifact's existence is the operator's own committed claim).
