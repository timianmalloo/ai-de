---
id: api-aide-core-watcher
title: "API: AiDe.Core.Watcher"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Watcher: 144 types, 269 members, 61% carrying a summary doc comment.
---

# API: `AiDe.Core.Watcher`

**144 public types · 269 public members · 61% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `IAdvisoryCredentialSource`

*interface* — `AdvisoryEvaluators.cs`

A presence-only check that a credential exists for a would-egress evaluator (ADR-0018). It never
exposes the secret itself - the watcher store holds non-secret facts only (architecture §4), so the
gate authorises on **presence**, and the secret is resolved elsewhere, at the call, by the
credential-backed transport. Absent by default.

## `NoCredential`

*class* — `AdvisoryEvaluators.cs`

A credential source that never has a credential - the safe default (local-only operation).

| Member | Summary |
|---|---|
| `bool HasCredential` | **(gap)** |

## `LocalHeuristicAdvisoryEvaluator`

*class* — `AdvisoryEvaluators.cs`

The deterministic, **local-only** advisory evaluator - the safe default that lets the advisory
dimensions (Evidence discipline, Solution economy) be judged without any model call, credential, or
egress. It grounds only on the quarantined evidence string the caller composes from deterministic
signals (a token list like `"verification=executed; coverage=9/10; actions_after_done=0;
premature=false; reuse=high"`) and maps it to a 0-4 rubric by fixed rules - never a guess: an
absent token scores conservatively (low), never optimistically.

**Remarks.** Because it is deterministic, its `EvaluatorStability` trivially passes (every repeat
is identical), but it still only folds into Weave points after the ADR-0019 calibration gates qualify
its `(version, taskClass, schemaVersion)` in the registry (slice 7) - the local heuristic is a
transparent proxy an operator can inspect, not a licence to score advisory dimensions unbounded.




It judges ONLY the two advisory dimensions; asked for any other it throws, because a deterministic
dimension is the deterministic scorer's job, never an evaluator's (spec rule 8).

| Member | Summary |
|---|---|
| `string EvaluatorVersion` | **(gap)** |
| `AdvisoryAssessment Evaluate(ScoreDimension dimension, WorkEpisode episode, string evidence)` | **(gap)** |

## `EgressGuardedAdvisoryEvaluator`

*class* — `AdvisoryEvaluators.cs`

The **egress + credential guard** (ADR-0018) around any advisory evaluator that would call out to a
model over the network. Before delegating it enforces, in order: the `EgressGate` has an
explicit per-path opt-in for this evaluator's path (else `EgressDenied`,
LK-0003 - default-deny), and a credential is present (else `InvalidBinding`,
LK-0002). Only then does the inner evaluator run. This is the boundary a real cloud judge sits behind;
the `LocalHeuristicAdvisoryEvaluator` needs no guard because it never egresses.

| Member | Summary |
|---|---|
| `string EvaluatorVersion` | **(gap)** |
| `AdvisoryAssessment Evaluate(ScoreDimension dimension, WorkEpisode episode, string evidence)` | **(gap)** |

## `QuadraticWeightedKappa`

*class* — `AdvisoryScoring.cs`

Quadratic Weighted Kappa - the human-agreement gate (spec rule 9b, ADR-0019). Measures agreement
between two 0..K-1 rating vectors, correcting for chance and penalising disagreement by the squared
band distance. 1 is perfect agreement; 0 is chance; negative is worse than chance.

| Member | Summary |
|---|---|
| `double Floor = 0.75` | **(gap)** |
| `double Compute(IReadOnlyList<int> a, IReadOnlyList<int> b, int categories = 5)` | **(gap)** |

## `EvaluatorStability`

*record* — `AdvisoryScoring.cs`

Evaluator stability over repeated runs of the same item - the reproducibility gate (spec rule 9a):
the ratings must stay in the same discrete 0-4 band at least 95% of the time and never differ by
more than one band.

| Member | Summary |
|---|---|
| `bool Passes` | **(gap)** |
| `EvaluatorStability Of(IReadOnlyList<int> repeats)` | **(gap)** |

## `CalibrationVerdict`

*record* — `AdvisoryScoring.cs`

The outcome of the ADR-0019 calibration gates for one advisory evaluator version.

## `AdvisoryCalibration`

*class* — `AdvisoryScoring.cs`

The advisory-evaluator calibration gates (spec rules 9, 14; ADR-0019). An evaluator version qualifies
to contribute score points only when ALL hold: (a) it is stable across repeats; (b) its agreement
with human labels reaches QWK >= 0.75; and (c) the anti-Goodhart counter-metrics (held-out outcome
integrity, regression rate, rework, dispute overturns) did not worsen - otherwise it is rejected as
score gaming or miscalibration.

| Member | Summary |
|---|---|
| `CalibrationVerdict Qualify(` | **(gap)** |

## `CalibrationRegistry`

*class* — `AdvisoryScoring.cs`

Records which advisory evaluator versions have qualified to contribute points, per
`(evaluatorVersion, taskClass, schemaVersion)` - because a change to any of the evaluator,
task class, or schema requires re-qualification (spec rules 10/13).

| Member | Summary |
|---|---|
| `void Qualify(string evaluatorVersion, string taskClass, string schemaVersion)` | **(gap)** |
| `bool IsQualified(string evaluatorVersion, string taskClass, string schemaVersion)` | **(gap)** |

## `AdvisoryAssessment`

*record* — `AdvisoryScoring.cs`

One advisory (model-judge) assessment of a dimension. Carries its evaluator version and evidence.

## `IAdvisoryEvaluator`

*interface* — `AdvisoryScoring.cs`

The model-judge seam (spec rule 8). A real implementation grounds on quarantined evidence and runs a
local model behind the credential/egress policy (ADR-0018, Phase 4/5); slice 7 depends only on the
interface, so the deterministic gate + fold are fully testable without a model.

## `AdvisoryWeaveScorer`

*class* — `AdvisoryScoring.cs`

Folds calibrated advisory assessments into a deterministic Weave scorecard (spec rule 9). An advisory
dimension earns points ONLY when its `(evaluatorVersion, taskClass, schemaVersion)` has qualified
in the registry; otherwise it stays excluded, exactly as the deterministic scorer left it. Advisory
never overrides a deterministic result: a Not Scored or Blocked base card is returned unchanged (a
tripped floor stands; an advisory judgment can never raise a deterministic failed dimension - rule 8).

| Member | Summary |
|---|---|
| `Scorecard Score(` | **(gap)** |

## `AuditLogEpisodeSource`

*class* — `AuditLogEpisodeSource.cs`

Reads committed AI-Forward audit-log entries that declare a goal-state (a top-level `goal` +
`done_when` + `session`, AL5b / front-matter CT19) and turns each into an **imported,
closed** `WorkEpisode`. This is the episode source that makes real Work Episodes exist
for the watcher: an audit entry is a durable, human/agent-committed record of a bounded goal that was
worked and closed, so importing it reads a *fact* - it does not fabricate a goal (spec L127, no
guessing NG1), and it does not forge a live operation (these are historical facts recorded directly
via `RecordEpisode`, the same way the coordination pump imports
registrations - the live, capability-verified path is `IWorkEpisodeService` for real-time
sessions). Entries without all three fields are skipped: not every audit entry is an episode.

| Member | Summary |
|---|---|
| `IReadOnlyList<WorkEpisode> Parse(IEnumerable<string> jsonlLines)` | Parses JSONL audit-log lines into imported closed episodes; malformed lines are skipped. |
| `IReadOnlyList<WorkEpisode> ReadFile(string path)` | Reads a repo's `audit-log.jsonl` into imported episodes; a missing file yields none. |
| `IReadOnlyList<ImportedEpisode> ParseWithEvidence(IEnumerable<string> jsonlLines)` | Parses lines into imported episodes paired with the observable audit evidence a signal derivation needs (conn-10) - currently whether the entry shipped a committed Proof Pack artifact. |
| `IReadOnlyList<ImportedEpisode> ReadFileWithEvidence(string path)` | Reads a repo's `audit-log.jsonl` into imported episodes + evidence; missing file → none. |

## `ClosedEpisodeScoring`

*class* — `ClosedEpisodeScoring.cs`

Turns a contract-closed Work Episode into a scored one - the link the agent collaboration loop was
missing (US-16).

**Remarks.** **The break this closes.** An agent registers through the coordination contract, declares
an episode, and closes it; every one of those steps worked and was tested at its seam. Scoring had
exactly one producer - `WatcherHost.ImportAndScoreEpisodesFromAuditLog` - which reads AI-DE's
own audit log, and takes its session id from the log's `session` field while
`TrustedRegistrar` mints a fresh one. The two identifier spaces could never meet, so a
registered agent produced a closed episode, no scorecard, and therefore no standing, forever. No
seam test could show that; only a test that walks the whole chain.





**Why a pass and not a hook on close.** Closing an episode is a *declaration*;
scoring it is a *judgement*. Coupling them would make the agent's own `episode-close`
line the thing that produced its score, and the two would fail together. An idempotent sweep over
closed-but-unscored is the shape every other watcher pass already has, so re-running it is free.





**Registered sessions only**, which keeps the two scoring producers disjoint: an
audit-imported episode has no `SessionRecord`, so this never re-scores one under a
different task class and the upsert can never flip-flop between the two.





**A pure function of the store**, deliberately: the host has a database, a pump and a
receiver, and none of them are involved in deciding whether an episode should be scored.

| Member | Summary |
|---|---|
| `int Run(` | Scores every closed episode of a registered session that has no scorecard, and returns the number newly scored. |

### `int Run(`

Scores every closed episode of a registered session that has no scorecard, and returns the
number newly scored.

**Remarks.** **The evidence is honestly empty.** A contract-declared episode carries no Proof
Pack - the watcher observed spans and a declared outcome, and neither is evidence of outcome
*quality*. So `EpisodeEvidence` is built with `HasProofPack: false` and
`DeterministicSignalsDeriver`'s conservative defaults apply: no verification path,
acceptance unknown, requirements zero. What falls out is **Not Scored, with the reason** -
which is true, and is the honest first thing an agent can receive.





It is emphatically **not a low score**. A derived-signals path that returned 0 for
"nothing was observed" would be a statement about the agent where only a statement about the
evidence is warranted, and it would be indistinguishable from a real failure.





**The task class is absent, not invented.** The coordination contract carries a goal
and a done-condition but no task class, so the segment is
`Unclassified` and therefore not comparable: the episode is scored
and delivered, and ranks nowhere. Supplying a placeholder class to make a leaderboard row
appear would put a value on a surface that reads as meaning something.

## `CoordContract`

*class* — `CoordinationContract.cs`

Pins the injected coordination-contract version. A record whose `contract` differs is rejected,
not re-parsed (Testing Strategy A6 - a schema change is a contract change). Bumping this is a
deliberate, gated change guarded by the version regression test.

| Member | Summary |
|---|---|
| `string Version = "loomkeeper/1"` | **(gap)** |
| `string VersionKey = "contract"` | **(gap)** |

## `EpisodeAttributes`

*class* — `CoordinationContract.cs`

The attribute keys an `episode-open` / `episode-close` line carries.

**Remarks.** Deliberately **not** in `OtelAttributes`. Those keys are OpenTelemetry
semantic conventions and are shared with the OTLP span path; a goal statement and a declared
outcome are this contract's own vocabulary, with no OTel convention behind them. Putting
them there would assert a standard that does not exist.

| Member | Summary |
|---|---|
| `string Goal = "episode.goal"` | **(gap)** |
| `string DoneWhen = "episode.done_when"` | **(gap)** |
| `string NotInScope = "episode.not_in_scope"` | **(gap)** |
| `string Outcome = "episode.outcome"` | **(gap)** |

## `BoardAttributes`

*class* — `CoordinationContract.cs`

The attribute keys a `board-post` line carries.

**Remarks.** There is deliberately no repository key. The board is per-repository and a session's
repository is fixed at registration, so it is **derived from the binding** rather than
supplied — an attribute would let a session post onto another repository's board by naming
it, which is the same class of hole as an update restating identity.

| Member | Summary |
|---|---|
| `string Kind = "board.kind"` | **(gap)** |
| `string Content = "board.content"` | **(gap)** |
| `string Parent = "board.parent"` | **(gap)** |

## `CoordContractEvent`

*record* — `CoordinationContract.cs`

A single injected-contract event emitted by a non-AI-Forward session over the `coord-core`
append log (spike S4). `ExternalSessionId` is the session's own id; the registrar mints
its own internal id, so the adapter owns the external->internal map.

## `ContractRegister`

*record* — `CoordinationContract.cs`

A registration: carries the same `OtelAttributes` keys as the OTLP path.

## `ContractHeartbeat`

*record* — `CoordinationContract.cs`

A liveness heartbeat for an already-registered external session.

## `ContractSessionEnd`

*record* — `CoordinationContract.cs`

A voluntary session end (minimal in slice 2: drops the external->internal mapping).

## `ContractUpdate`

*record* — `CoordinationContract.cs`

Later-known attributes for an already-registered session: the harness, the model.

**Remarks.** **Why a distinct kind rather than a second `register`.** A repeat registration is
dropped entirely — `ApplyRegister` returns before reaching the registrar, so the richer
attributes never arrive (observed:
`Apply_DuplicateRegister_DiscardsTheSecondAttributes_ItDoesNotMerge`). That is correct for a
duplicate: the first registration's capability must stand, or an external id could be used to
re-mint one. Enrichment is a different intent and needs its own verb.





**Which is the whole reason it exists.** AI-DE registers a terminal before knowing what
runs inside it, and the model is knowable only by the agent — chosen inside the session and
changeable mid-session. Without this the model can never be recorded for any AI-DE-launched
session, no matter what anyone builds.





**Additive within `loomkeeper/1`, deliberately.** The parser already skips a
syntactically valid line whose `kind` it does not handle, so an older reader ignores this
where a version bump would have made it reject the whole log. A schema change is a contract
change — but this adds a kind rather than altering one, and the existing tolerance is what makes
that safe rather than a hope.





**It cannot mint or alter identity.** Only the attributes an update may carry are
merged; repository, worktree, terminal and agent are fixed at registration. An update naming an
unknown session is dropped and counted, exactly as a heartbeat for one is.

## `ContractEpisodeOpen`

*record* — `CoordinationContract.cs`

A session declaring a bounded objective it is starting work on: the goal, the terminal condition
it will be judged against, and optionally what it is not doing.

**Remarks.** **Why the agent declares this and the shell cannot.** An episode is the unit scoring
attaches to, and it needs a goal. The workbench knows a terminal exists; it does not know what
the agent inside it is trying to do. Opening an episode per terminal with a placeholder goal
would *fabricate* one (NG1), and the scorer already treats a missing goal honestly — Not
Scored with the reason, never a low mark. So the declaration comes from the only party that has
it.





**Why this is the multi-harness unblock.** Before it,
`AuditLogEpisodeSource` was the only producer of episodes, so an episode existed
only where the AI-Forward pack had written an audit entry. A GitHub Copilot session or a plain
shell produced none, and the leaderboard could not compare what it was built to compare.





**A blank goal is malformed, not an empty episode.** Opening one with an empty
statement would score as Not Scored and read as "the agent declared nothing", when in fact the
declaration was invented here.

## `ContractEpisodeClose`

*record* — `CoordinationContract.cs`

A session closing its current episode with a declared outcome.

**Remarks.** The outcome is the **declared** lifecycle terminal state, not a quality judgement. Whether a
`Completed` claim is honest is the Weave's Outcome-integrity dimension, which reads
deterministic evidence rather than this field — so an agent claiming Completed on unmet
acceptance criteria is exactly the case the scorer already detects.

## `ContractBoardPost`

*record* — `CoordinationContract.cs`

A session posting to its repository's Message Board: a question, a decision, a breadcrumb, a
knowledge candidate, or a reply or acknowledgement of an existing message.

**Remarks.** **Why this kind exists.** The Message Board was implemented, tested and rendered, and
`MessageBoardService` had **no callers anywhere in the product** — no ingest path,
no MCP tool, no UI affordance. It was a read surface over a store nothing wrote to. An agent
asked to "post to the loomkeeper board" searched the repository for how, found nothing, and the
pane went on saying "No board posts yet". The agent was right; the mechanism did not exist. The
parser's own comment had been calling this "a future board post" since slice 2.





**The repository is not the sender's to choose.** It is read from the registered
session's binding. Accepting it as an attribute would let a session post onto another
repository's board by naming it — the same hole as an update restating identity, and the board
is precisely where a forged origin would be most persuasive to a reader.





**Content stays untrusted.** The service quarantines every post and flags
grader-injection shapes; this kind changes none of that. What arrives over a file anything can
append to is data, and the scorer reads typed signals rather than board prose, which is what
makes that guarantee hold rather than depend on the flag being accurate.

## `CoordContractParseStats`

*record* — `CoordinationContract.cs`

Parse-layer counters (IO1): how many lines were malformed or rejected on version.

## `CoordContractParser`

*class* — `CoordinationContract.cs`

Reads a `coord-core` append log tolerantly into ordered contract events, stdlib only. One JSON
object per line; a blank line (including the LOG-A leading newline), a CRLF terminator, and
surrounding whitespace are tolerated; a malformed line is skipped and counted; a line whose
`contract` version is not `Version` is rejected and counted. Events are
returned sorted `(at, externalSessionId, seq)` so replay is deterministic (mirrors coord-core fold).

A syntactically valid line whose `kind` is not one this slice handles (e.g. a future board post
sharing the same log) is silently skipped - it is not this parser's event, not an error.

| Member | Summary |
|---|---|
| `IReadOnlyList<CoordContractEvent> Parse(string jsonl)` | **(gap)** |
| `IReadOnlyList<CoordContractEvent> Parse(string jsonl, out CoordContractParseStats stats)` | **(gap)** |

## `CoordContractStats`

*record* — `CoordinationContract.cs`

A snapshot of the adapter counters (IO1 operator questions).

## `InjectedContractIngest`

*class* — `CoordinationContract.cs`

The injected-contract ingest adapter: maps contract events onto the same
`TrustedRegistrar`/`IngestHost` as the OTLP path, so a non-AI-Forward session
appears identically in the fact store (one ledger, projected, not duplicated - US-5).

The append log is a local, forgeable surface (ADR-0007), so - symmetrically with the OTLP token -
the `SessionCapability` is never read from the file: this adapter **mints** it at
`register` and holds `external-id -> RegisteredSession`, verifying every `heartbeat`
against the held capability. A heartbeat for a session never registered here has no capability and is
dropped and counted; a duplicate register is ignored (the first capability stands); a register whose
identity is incomplete is quarantined (LK-0004) without stopping the stream (US-11 fail honestly).

Pattern: Adapter over the ingest host's port (DDD ACL), keyed by the external session id.

| Member | Summary |
|---|---|
| `InjectedContractIngest(IngestHost host)` | **(gap)** |
| `CoordContractStats Stats` | **(gap)** |
| `void ApplyAll(IEnumerable<CoordContractEvent> events)` | Applies a batch in order. Callers pass parser output, already sorted. |
| `void Apply(CoordContractEvent evt)` | Applies one contract event. Never throws on a bad event; every disposition is counted. |

## `CoordContractWriter`

*class* — `CoordinationContractLog.cs`

Writes injected-contract events for one or more non-AI-Forward sessions to a coord-core-shaped
append log (spike S4): one file per session (`<dir>/<session>.jsonl`), one JSON object
per line, `seq` auto-assigned, an atomic single-write append, and the **LOG-A** guard - a
leading newline when the file did not already end in one, so a fused line is impossible to express.
This is the session-side half of the contract; `InjectedContractIngest` is the ingest half.

| Member | Summary |
|---|---|
| `CoordContractWriter(string logDir, TimeProvider? time = null)` | **(gap)** |
| `void WriteRegister(string externalSessionId, IReadOnlyDictionary<string, string?> attributes)` | Writes a registration with the same `OtelAttributes` keys as the OTLP path. |
| `void WriteHeartbeat(string externalSessionId)` | **(gap)** |
| `void WriteSessionEnd(string externalSessionId)` | **(gap)** |

## `CoordContractLog`

*class* — `CoordinationContractLog.cs`

Reads a coord-core append log directory into ordered contract events (stdlib, tolerant).

| Member | Summary |
|---|---|
| `IReadOnlyList<CoordContractEvent> ReadDirectory(string logDir)` | Reads every `*.jsonl` in  and parses them into one ordered event stream (`CoordContractParser` sorts by `(at, session, seq)`). A missing directory yields an empty list; a malformed line in any file is skipped and coun… |

## `CoordContractLogPump`

*class* — `CoordinationContractLog.cs`

Reads a contract log directory and applies it to an `InjectedContractIngest`. Re-running
is safe: the adapter is idempotent (a duplicate register is ignored, a heartbeat merely refreshes
liveness), so a whole-directory re-read never double-registers - which is why a naive "read it all"
pump is correct here without tracking file offsets.

| Member | Summary |
|---|---|
| `int PumpOnce()` | Reads the log directory once and applies every event; returns the count applied. |

## `DaydreamState`

*enum* — `DaydreamCandidate.cs`

Where a Daydream item stands on the promotion staircase (spec §"Daydream item" state vocabulary).

**Remarks.** Every landing has something that can stop the climb, and each is an acceptance criterion of US-9
rather than a design preference:

Observation ──(recurrence)──> NeedsDisconfirm ──(check survived)──> Promotable
│                                    │
(check refuted)                        (a human decides)
▼                                    ▼
Disconfirmed              Promoted · Deferred · Rejected
│
(source corrected/deleted/contradicted)
▼
Retracted

## `DisconfirmingOutcome`

*enum* — `DaydreamCandidate.cs`

What a completed disconfirming check found.

## `CandidateEvidence`

*record* — `DaydreamCandidate.cs`

The evidence a Candidate Lesson must carry before anyone may act on it (US-9, second criterion:
source episodes, confidence, counter-evidence, expected effect, and the disconfirming check).

**Remarks.** **Split into derived and authored, deliberately.** Source episodes and confidence are
computed from observations — the system knows them. Counter-evidence, expected effect and the
check are **authored**, and are null until someone supplies them. Nothing derives them,
because a generated "expected effect" is a guess wearing the costume of evidence.





A candidate missing any authored part is `NeedsDisconfirm`, which
is a state and not a warning: promotion is unreachable from it rather than discouraged.

| Member | Summary |
|---|---|
| `bool IsComplete` | True only when every authored part is present and the check has been run. |

## `DaydreamEvent`

*record* — `DaydreamCandidate.cs`

One append-only event in a candidate's life. The state is folded from these, never stored.

**Remarks.** The same discipline as every other fact in this store: a correction is a superseding event,
so the history of what was believed — and when, and by whom — survives the correction. A stored
state would be a second definition of a quantity the events already determine (DM7).





`Actor` is who caused it: the system for an observation or a threshold
crossing, and a named operator for anything requiring the human gate. It is recorded because
"who promoted this" is the first question anyone asks about a lesson they disagree with.

## `DaydreamEventKind`

*enum* — `DaydreamCandidate.cs`

The kinds of thing that happen to a candidate.

## `DaydreamCandidate`

*record* — `DaydreamCandidate.cs`

A candidate's current standing, folded from its events and its surviving evidence.

| Member | Summary |
|---|---|
| `bool CanPromote` | Whether a human may promote this now. |

### `bool CanPromote`

Whether a human may promote this now.

**Remarks.** Read by the surface to decide whether a promote affordance exists **at all** — not whether
it is enabled and shows an error on click. A control the user can press and be refused by
teaches that the refusal is negotiable.

## `DaydreamFold`

*class* — `DaydreamCandidate.cs`

Folds observations and candidate events into current standing. Pure: no store, no clock, no I/O.

**Remarks.** **Promotion is unreachable rather than refused.** There is no `Promote()` method
that validates and throws. A `Promoted` event on a candidate that was not
`Promotable` does not move it — the guard is in the transition, so an
event written by any path, including a hand-edited store, cannot promote something unpromotable.






**Evidence can be withdrawn.** Fold order is observations first, then events: if
episodes disappear — retention, correction, a purged workspace — a candidate that no longer
recurs falls back to `Observation` whatever its event history says.
A lesson outliving the evidence for it is the failure this ordering prevents.

| Member | Summary |
|---|---|
| `IReadOnlyList<DaydreamCandidate> Fold(` | Every pattern currently known, with its standing. |

## `DaydreamSignature`

*record* — `DaydreamObservation.cs`

What makes two episodes "the same thing happening again" (spec US-9).

**Remarks.** **Derived from the recorded scorecard, not from the scorer's inputs.**
`DeterministicEpisodeSignals` is never persisted — only `ScoredEpisode`
is (`RecordScorecard`). So a signature computed from the raw signals could be produced once,
live, and never again from the store. Daydream observes what was *recorded*.





**Typed values only, never prose.** The verdict, the tripped floors, and each
dimension's 0–4 rubric. Deliberately **not** `Rationale` or `Headline`, which are
generated sentences: keying on them would make a wording change look like a new pattern, and it
would put the scorer's phrasing on the path between an agent and a proposed lesson. The scorer's
injection invariance is inherited here rather than re-earned — board text cannot reach a
signature because no text can.





**Attribution is excluded on purpose.** Harness, model and operator are not part of the
signature. A pattern is a property of the *work*, and including them would produce claims of
the form "this harness tends to…" — a comparison the leaderboard already makes under cohort and
single-operator protections that a Daydream candidate would bypass entirely (US-10).





**Task class segments.** Two episodes in different task classes are not the same
pattern, for the same reason the leaderboard never ranks across one.

| Member | Summary |
|---|---|
| `DaydreamSignature For(ScoredEpisode episode, int lowRubricThreshold = 2)` | Derives the signature of one scored episode. |
| `bool IsUnremarkable` | True when nothing fell short and no floor tripped — a clean episode. |

### `DaydreamSignature For(ScoredEpisode episode, int lowRubricThreshold = 2)`

Derives the signature of one scored episode.

**Remarks.** is what counts as a shortfall. A dimension scoring at
or below it is part of what makes this episode recognisable; one scoring above it is the
system working and is not a pattern worth proposing a lesson about.

### `bool IsUnremarkable`

True when nothing fell short and no floor tripped — a clean episode.

**Remarks.** A clean episode is not a pattern to learn from. Observing them would fill the register with
"work went well", which is true, recurrent, and useless as a lesson.

## `DaydreamObservation`

*record* — `DaydreamObservation.cs`

One observed occurrence of one candidate pattern in one Work Episode at one observation time
(spec line 237 — the declared grain).

**Remarks.** Append-only. An observation is never edited; a re-observation of the same episode is a new row
and the fold deduplicates by episode, so replay is deterministic and a correction is a
superseding fact rather than a rewrite.

## `DaydreamRecurrence`

*record* — `DaydreamObservation.cs`

A pattern seen more than once, with the episodes that evidence it.

**Remarks.** `DistinctEpisodes` rather than a raw count: the same episode observed twice is one
occurrence. Without that, a re-scan of the store would manufacture recurrence out of nothing —
which is the cheapest possible way to produce a confident lesson from a single event.

| Member | Summary |
|---|---|
| `int DistinctEpisodes` | **(gap)** |

## `RecurrenceDetector`

*class* — `DaydreamObservation.cs`

Groups observations into recurrences. Pure: no store, no clock, no I/O.

**Remarks.** **The threshold is a declared safety floor, not a tuned number.** Two distinct episodes
is the minimum at which "again" is meaningful, and US-9's first acceptance criterion is that one
occurrence stays an Observation and is **not** generalised. Its statistical basis is
**not recorded**: no power analysis has been done, and this is stated rather than implied so
that raising it later is a decision with evidence rather than a correction of a guess. It may
tighten; it must never silently relax.

| Member | Summary |
|---|---|
| `IReadOnlyList<DaydreamRecurrence> Recurring(IEnumerable<DaydreamObservation> observations)` | The patterns that recur, ordered deterministically for replay. |

## `DaydreamRecorder`

*class* — `DaydreamRecorder.cs`

Turns a scored episode into an observation in the repository's Daydream record.

**Remarks.** **The one call site Daydream needs.** Everything downstream — recurrence, candidates,
the promotion staircase, the pane — folds from what this writes. It takes a
`ScoredEpisode` because that is what is actually persisted:
`DeterministicEpisodeSignals` is never stored, so a signature derived from the scorer's
inputs could be produced once, live, and never again.





**Called after a scorecard is recorded, not before.** Observing an episode the store
does not yet hold would put a pattern in the record whose evidence a reader cannot follow.





**The production call site is `ScoringService`**, immediately after the
scorecard is recorded — one site rather than one per scoring producer, because that is the single
place a `ScoredEpisode` comes into existence. The shell supplies a recorder built on
the open workspace, so the record is written into the repository the work happened in.





**It writes nothing for an agent&apos;s episode today, and that is measured.** A
contract-declared episode carries no Proof Pack, so nothing is observed: no floor trips, every
dimension is Not-Recorded, and a Not-Recorded dimension has a null rubric and so cannot fall
short. The signature is therefore unremarkable and `Observe` declines it. The concern
before wiring was the opposite — that every agent episode would carry the SAME Not-Scored
signature and one useless pattern would dominate the recurrence report — and
`WhatDaydreamSeesInAnAgentEpisodeTests` refutes it and is written to fail the day an agent
episode does carry evidence. The audit-import producer, which reads committed Proof Packs, is
what feeds this today.





**Suppression is deliberately NOT here.** `DreamCorpusReader` marks a
pattern already-known so it stops being re-*proposed*; it must never stop it being
*observed*. An observation is evidence, and dropping evidence because a lesson was already
written would make the record understate what happened — and would make a retraction in the pack
unrecoverable, since the occurrences it needed were never kept.

| Member | Summary |
|---|---|
| `bool Observe(ScoredEpisode episode)` | Records one episode as an observation, and reports whether it wrote one. |

### `bool Observe(ScoredEpisode episode)`

Records one episode as an observation, and reports whether it wrote one.

**Returns.** `true` only when a line was written. `false` covers three different situations — an unremarkable episode, an unavailable record, and a failed write — and the caller that needs to tell them apart asks `Unavailable`. Nothing here reports a write it did not make.

## `DaydreamRecordRead`

*record* — `DaydreamRepositoryRecord.cs`

What one read of the repository's Daydream record found — including what it could not read.

**Remarks.** `UnreadableLines` is reported rather than swallowed, following the audit log's own
verifier ("0 unreadable lines"). A line this version cannot parse is far more likely to be a
newer writer's than a corruption, and silently dropping it would render a partial record as a
complete one — an absence shown as a result (DC-025).

| Member | Summary |
|---|---|
| `DaydreamRecordRead Empty { get; } = new([], [], 0)` | **(gap)** |

## `DaydreamRepositoryRecord`

*class* — `DaydreamRepositoryRecord.cs`

The repository's Daydream record: two append-only logs the product maintains, in the repository
being worked on.

**Remarks.** **Why the repository and not the product's store.** A lesson about *this*
repository belongs *with* that repository, for the same reason `defect-classes.md` is
committed rather than kept in a tool's private directory: it survives a machine change, it
travels with a clone, and it is reviewable in a pull request. The per-workspace SQLite store
gave the rows the wrong lifetime — they outlived the repository they were about.
(`design-watcher-daydream-dream-seam` §4a; the owner's decision in
`note-20260902-two-decisions-the-loop-waits-on`.)





**Provenance is per-record, not per-file.** Every line carries
`"generated-by":"ai-de/daydream"`. These logs merge by *content union* across sessions
and worktrees (`tools/merge-append-only-log.py`, the DC-026 control), and a header line
would be merged, duplicated, or lost — while a field on each record survives every one of those.
The field's presence means the product wrote it; its absence means an agent or a human did.





**Enums are written as names.** An ordinal in a committed file is unreadable to the
human it is committed for, and reordering an enum would silently rewrite the meaning of every
historical line.





**Absence is stated.** A workspace with no repository has nowhere to write, and says so
rather than presenting an empty Daydream — the same rule `DreamCorpusReader` keeps
for an absent pack.

| Member | Summary |
|---|---|
| `string GeneratedBy = "ai-de/daydream"` | The provenance marker, one literal spelling in every format (see the design §4b). |
| `string? Root { get; }` | The repository root, or `null` when there is none. |
| `string? Unavailable { get; }` | Why the record is unavailable, or `null` when it is readable and writable. |
| `bool Available` | True when the record can be read and written. |
| `string? Directory` | The directory the record lives in, or `null` when unavailable. |
| `DaydreamRepositoryRecord For(string? repositoryRoot)` | Opens the record for a repository root, or reports why it cannot be opened. |
| `DaydreamRepositoryRecord Absent { get; } = For(null)` | The record for a workspace with no repository — an absence, stated. |
| `bool Append(DaydreamObservation observation)` | Appends one observation. A no-op when the record is unavailable. |
| `bool Append(DaydreamEvent candidateEvent)` | Appends one candidate event. A no-op when the record is unavailable. |
| `DaydreamRecordRead Read()` | Reads the whole record. An unavailable record reads as empty, never as an error. |

### `string? Unavailable { get; }`

Why the record is unavailable, or `null` when it is readable and writable.

**Remarks.** A sentence for a person, naming only what was actually checked. It never speculates about a
cause this class did not look at (DC-087).

### `DaydreamRepositoryRecord For(string? repositoryRoot)`

Opens the record for a repository root, or reports why it cannot be opened.

**Remarks.** The root is not created and not probed for a `.git` directory: the caller decides what a
repository is, and this class only needs somewhere that exists to write into.

## `AuditSignals`

*record* — `DeterministicSignalsDeriver.cs`

The optional, explicit deterministic signals an instrumented AI-Forward turn may record on its audit
entry (a `signals` object - the watcher telemetry convention). Every field is nullable by design:
a harness emits only what it actually observed, and the watcher falls back to a conservative default for
anything absent - never a fabricated value (spec L127, NG1). This is the reader-side data shape; the
writer half (audit-log.py emitting a `signals` object) is a future AI-Forward enhancement, so a
current entry simply omits it and scores conservatively. See
`docs/design/watcher-signals-telemetry.md`.

## `EpisodeEvidence`

*record* — `DeterministicSignalsDeriver.cs`

The observable audit-entry evidence a signal derivation grounds on (conn-10). At minimum a committed
Proof Pack artifact (a `docs/proof/` path); optionally the explicit `AuditSignals` an
instrumented turn recorded. Absent signals never fabricate a value - the deriver falls back to the
conservative default (spec L127, NG1).

## `ImportedEpisode`

*record* — `DeterministicSignalsDeriver.cs`

An imported closed Work Episode paired with the audit evidence a signal derivation needs.

## `DeterministicSignalsDeriver`

*class* — `DeterministicSignalsDeriver.cs`

Derives a `DeterministicEpisodeSignals` for an imported closed episode from what is
**honestly observable** - a committed Proof Pack (the only verification signal an audit entry
carries), the declared close outcome, and any spans recorded after the close. Everything not observable
is a conservative default that the scorer renders as Not-Recorded or Not-Scored, never a fabricated
value: acceptance stays null (unknown, not "met"), regression false (not "no regression exists"),
guidance/coordination requirements 0 (those dimensions render Not-Recorded), coverage uncalibrated.
Pure and deterministic. See `docs/design/watcher-signals-derivation.md`.

| Member | Summary |
|---|---|
| `DeterministicEpisodeSignals Derive(` | **(gap)** |

## `DisputeService`

*class* — `DisputeService.cs`

The operator-facing entry point for raising a dispute (US-16 / rule 12). It mints the dispute id and
timestamp and appends the `ScoreDispute` fact - the append-only, never-overwrites
guarantee lives in the store (conn-4). This is the API a UI command binds to; it exists so a caller
never hand-builds a dispute id or reaches past the store's append-only contract.

| Member | Summary |
|---|---|
| `ScoreDispute RaiseDispute(string episodeId, string operatorId, string reason, ScoreDimension? dimension = null)` | Raises a dispute against a scored episode with the operator's reason, optionally targeting one dimension (null = the whole score). Appends the fact and returns it. The reason is required - a dispute with no stated rea… |

## `DelegatingAdvisoryEvaluator`

*class* — `DisputeService.cs`

The cloud-judge scaffold: an `IAdvisoryEvaluator` that delegates the actual 0-4 rubric to
an injected model call. A real integration supplies the delegate (a call to a provider, grounded on
the quarantined evidence, returning a rubric), and this evaluator is placed **inside** an
`EgressGuardedAdvisoryEvaluator` so the network call only happens after the ADR-0018
egress opt-in and credential check pass. It exists so the seam is concrete and testable without a
provider: the deterministic parts (guarding, folding, calibration) are proven around it, and the one
undetermined piece - the model call - is a single injected function.

**Remarks.** The delegate returns only a rubric (0-4); the evaluator clamps it and wraps it with the version and a
rationale. It never egresses by itself - egress is the guard's job. A production call would validate
the model's structured output (LOA A1-A3) before returning the rubric.

| Member | Summary |
|---|---|
| `string EvaluatorVersion { get; } =` | **(gap)** |
| `AdvisoryAssessment Evaluate(ScoreDimension dimension, WorkEpisode episode, string evidence)` | **(gap)** |

## `DreamCorpus`

*record* — `DreamCorpusReader.cs`

What the offline Dream has already promoted, so Daydream stops re-proposing it.

**Remarks.** `Present` is the honest part: `false` means the AI-Forward Pack's corpus was not
found, which is different from finding it empty. A repository without the pack is the normal
case, not a failure, and the two must never render alike.

| Member | Summary |
|---|---|
| `DreamCorpus Absent { get; } =` | The corpus for a repository that has no pack — an absence, stated. |
| `bool AlreadyKnown(DaydreamSignature signature)` | Whether a candidate has already been promoted, by any route. |

### `bool AlreadyKnown(DaydreamSignature signature)`

Whether a candidate has already been promoted, by any route.

**Remarks.** Matched on the signature's own words appearing in a promoted learning's text. Deliberately
loose in the direction of **suppressing a duplicate proposal** rather than making a
claim: a false match costs a candidate that a human can still find on the surface, and a
false miss costs a re-proposal of something already known. Neither is a correctness failure,
which is why this is allowed to be a heuristic where nothing else in Daydream is.

## `DreamCorpusReader`

*class* — `DreamCorpusReader.cs`

Reads the AI-Forward Pack's promoted corpus, when a repository has one.

**Remarks.** **Detected, never assumed, and read-only.** AI-DE requires nothing of the pack. This
looks for two plain files a repository may or may not have, and reports their absence as an
absence. It never invokes `dream.py`: shelling out would make Python and a vendored pack a
runtime dependency of the product, which is the inversion
`design-watcher-daydream-dream-seam` exists to refuse.





**Why these two files and not an inbox.** A spike on 2026-09-02 read
`dream.py`'s `load_corpus` and found it reads five FIXED paths with no discovery and no
extension point — falsifying the original seam design, which had proposed emitting into an
inbox the script would have to have been taught to read. These two are what it actually
maintains, so they are what can be read back.

| Member | Summary |
|---|---|
| `DreamCorpus Read(string? repositoryRoot)` | Reads the corpus rooted at a repository, or reports its absence. |

## `EgressDecision`

*enum* — `EgressGate.cs`

Whether an egress path may be used.

## `EgressGate`

*class* — `EgressGate.cs`

The default-deny egress gateway (ADR-0018, extends ADR-0011). Outbound is blocked until an explicit
per-path opt-in enables exactly that path; every other path stays blocked. The gate ships in Phase 1,
before any component that could egress, so the local-only default is enforced from the start.

| Member | Summary |
|---|---|
| `EgressDecision Decide(string pathId)` | Blocked unless this exact path was opted in. |
| `void OptIn(string pathId)` | Enables exactly one path. Every other path remains blocked. |
| `void Revoke(string pathId)` | Revokes a previously opted-in path; it returns to blocked. |

## `RepositorySessions`

*record* — `FleetAggregator.cs`

One repository's sessions in the fleet map.

## `FleetView`

*record* — `FleetAggregator.cs`

The cross-repository fleet: the `repository -> sessions` map (spec item 3, US-3).

| Member | Summary |
|---|---|
| `int RepositoryCount` | **(gap)** |
| `int SessionCount` | **(gap)** |

## `FleetAggregator`

*class* — `FleetAggregator.cs`

Builds the cross-repository fleet map from one or more session sources - each store/daemon is per
workspace, so a fleet view is an aggregation over >=2 sources, grouped by the session's own
repository identity (its canonical path). Deterministic order: repositories by display name then
canonical path, sessions by id. Pure - it reads the slice-3 session read model, adds no store.

| Member | Summary |
|---|---|
| `FleetView Aggregate(IEnumerable<IWatcherSessionsQuery> sources)` | **(gap)** |
| `FleetView Aggregate(params IWatcherSessionsQuery[] sources)` | **(gap)** |

## `HarnessEvent`

*record* — `IngestHost.cs`

A harness event. Registration/heartbeat are handled synchronously; spans are queued.

## `HarnessSpanEvent`

*record* — `IngestHost.cs`

An observed span plus the capability the emitting process presented for its session.

## `IHarnessEventSource`

*interface* — `IngestHost.cs`

The transport port. An OTLP network receiver (slice 1b) or an in-process source implements this and
feeds `Enqueue`. Defined here as the seam; the host itself is transport-neutral.

## `IngestStats`

*record* — `IngestHost.cs`

A snapshot of the ingest counters - the operator questions answerable without a debugger (IO1):
how many spans came in, were dropped under load, stored, deduped, rejected as forged, or quarantined.

## `IngestHost`

*class* — `IngestHost.cs`

Hosts the ingest path: synchronous registration/heartbeat, plus an async, bounded span stream drained
into `OtelSpanMapper` + `SpanIngest`. A span flood is absorbed by the bounded
queue (drop-oldest), a forged span is rejected, and a malformed one is quarantined - one bad event can
never kill the drain loop, and every disposition increments a visible counter (US-11 fail honestly).

Pattern: bounded producer/consumer (LOA Channel<T> backpressure) - the repo's
`Channel.CreateBounded` + `DropOldest` idiom (ConPtyTerminalSession).

| Member | Summary |
|---|---|
| `IngestHost(` | **(gap)** |
| `RegisteredSession Register(HarnessRegistration registration)` | Maps and registers a session synchronously, returning its capability (LK-0004/LK-0002). |
| `void Heartbeat(string sessionId, SessionCapability capability)` | Records a heartbeat after verifying the capability (LK-0001). |
| `void UpdateHarnessAndModel(` | Records a harness and/or model learned after registration, capability-verified. |
| `WorkEpisode OpenEpisode(` | Opens a Work Episode for a verified session (US-6). Capability-gated like every other post-registration write. |
| `WorkEpisode ReframeEpisode(` | Reframes an open episode: the current one closes Superseded and a new generation opens. |
| `WorkEpisode CloseEpisode(string episodeId, SessionCapability capability, EpisodeOutcome outcome)` | Closes an episode with its declared outcome. The declaration is not a quality judgement. |
| `BoardMessage PostToBoard(` | Posts to a repository's Message Board on behalf of a verified session (US-4). |
| `BoardMessage ReplyOnBoard(` | Replies to an existing message. The service refuses an orphan. |
| `BoardMessage AcknowledgeOnBoard(` | Acknowledges an existing message. Carries no content by design. |
| `void EndSession(string sessionId)` | Marks a session ended (its terminal closed / it reported session-end). Liveness then reads Ended rather than lingering Alive/Stale. Called by the coordination ingest on a session-end event; the registrar's re-registra… |
| `void Enqueue(HarnessSpanEvent spanEvent)` | Enqueues a span event. Never blocks: under load the bounded queue drops its oldest item (counted), so a flood degrades to a coverage gap rather than unbounded growth. |
| `int DrainAvailable()` | Processes every span currently queued and returns the count. Deterministic (no waiting), so tests drain exactly what they enqueued. |
| `Task RunAsync(CancellationToken ct)` | The production loop: wait for spans, then drain, until cancelled. |
| `IngestStats Stats` | A point-in-time snapshot of the counters. |

### `void UpdateHarnessAndModel(`

Records a harness and/or model learned after registration, capability-verified.

**Remarks.** The reason this exists at all: AI-DE registers a terminal before knowing what runs inside it,
and the model is knowable only by the agent. Without a post-registration path the model can
never be recorded for any AI-DE-launched session, because a repeat `register` discards
its attributes rather than merging them (observed).

### `WorkEpisode OpenEpisode(`

Opens a Work Episode for a verified session (US-6). Capability-gated like every other
post-registration write.

**Remarks.** The reason this exists on the host: before it, `AuditLogEpisodeSource` was the
only producer of episodes, so an episode existed only where the AI-Forward pack had written
an audit entry. Any harness can now declare one over the coordination log, which is what the
leaderboard's cross-harness comparison and the specified Daydream both depend on.

### `BoardMessage PostToBoard(`

Posts to a repository's Message Board on behalf of a verified session (US-4).

**Remarks.** The reason these exist on the host: `MessageBoardService` had **no callers
anywhere in the product**. It was implemented, tested and rendered as a pane, and nothing
could write to it — a read surface over an empty store. An agent asked to post to the board
searched the repository for how, found nothing, and the pane went on saying "No board posts
yet". These are the ingest half of that path.

## `ScoreSegment`

*record* — `Leaderboard.cs`

The partition a Weave score is comparable within: one workspace, one task class, one score schema
version. A comparison never crosses any of the three (spec US-14, rule 10).

**Remarks.** **One type rather than three adjacent strings.** `TaskClass` and
`SchemaVersion` already sat side by side in this record, in `Leaderboard`, and in
the standing's trend filter; adding a third string of the same type would have made a reordered
triple compile and pass, in the values that reach a surface and are read as meaning something.
With one type the two filter predicates collapse into one equality, and the day a fourth axis
arrives every call site breaks at once instead of silently accepting the wrong order.





**The schema version is not the caller's to supply.** It comes from the scorecard the
scorer produced, so `ScoringService` composes the segment rather than accepting one -
two definitions of one quantity is a defect signature (DM7).

| Member | Summary |
|---|---|
| `string Unclassified = "unclassified"` | The task class of an episode whose kind of work was never declared. |
| `bool IsComparable` | Whether this segment is a cohort at all, and therefore whether a rank in it would mean anything. |
| `string? IncomparableReason` | Why this segment is not a cohort, or `null` when it is one. |

### `string Unclassified = "unclassified"`

The task class of an episode whose kind of work was never declared.

**Remarks.** The coordination contract carries a goal and a done-condition but **no task class** - an
agent declares what it is trying to do, not what kind of work it is. So the class is genuinely
absent, and this names the absence rather than inventing a kind. It is not a category anyone
can be ranked in: see `IsComparable`.

### `bool IsComparable`

Whether this segment is a cohort at all, and therefore whether a rank in it would mean anything.

**Remarks.** Two conditions, both absences rather than values. **No workspace** - the repository
could not be resolved, or the row predates segmentation, so the directives the work happened
under are unknown. **No task class** - pooling every undeclared episode would compare a
spike against a refactor and read the difference as an agent improving, which is the exact
error segmentation exists to prevent.





An incomparable segment still gets **scored** and still yields a standing; what it
does not get is a rank. That distinction is the whole point - Not Comparable is a statement
about the cohort, and a low score would be a statement about the agent.





The rule lives here rather than in the composer because two consumers already ask it
(the board and the standing's trend), and a rule spelled twice is a rule that drifts.

### `string? IncomparableReason`

Why this segment is not a cohort, or `null` when it is one.

**Remarks.** The reason travels with the verdict because the agent reading its standing sees only that it
has no rank. "No rank" with no cause is an empty state naming nothing (DC-087), and the two
causes want opposite responses: an undeclared task class is something the agent can fix, an
unresolvable repository is not.

## `ScoredEpisode`

*record* — `Leaderboard.cs`

A scored episode with its harness/model/operator attribution - the input to the leaderboard and
standing. `Weave` is the sum of the scored dimensions' earned points (there is no single
stored score; it is derived, DM7).

| Member | Summary |
|---|---|
| `string TaskClass` | The kind of work, from `Segment`. |
| `string SchemaVersion` | The score schema version, from `Segment`. |
| `double Weave` | **(gap)** |
| `double? CoverageRatio` | **(gap)** |
| `bool IsScoreable` | **(gap)** |

## `LeaderboardFacet`

*enum* — `Leaderboard.cs`

The three leaderboard axes (spec US-14). There is deliberately no per-operator facet.

## `LeaderboardCell`

*record* — `Leaderboard.cs`

One leaderboard cell. A cell below the cohort minimum or one that resolves to a single operator
renders Not Comparable, never a rank (spec US-14/US-10). Every comparable cell carries its cohort
size and Evidence Coverage.

## `Leaderboard`

*record* — `Leaderboard.cs`

A leaderboard for one `ScoreSegment` (comparisons never cross it).

| Member | Summary |
|---|---|
| `string TaskClass` | The kind of work this board covers, from `Segment`. |
| `string SchemaVersion` | The score schema version this board covers, from `Segment`. |
| `LeaderboardCell? Cell(LeaderboardFacet facet, string label)` | **(gap)** |

## `LeaderboardComposer`

*class* — `Leaderboard.cs`

Composes the harness / model / harness-model leaderboard within one task class and score schema
version (spec US-14, rules 10-11). A facet cell is Comparable only with a cohort of at least the
minimum (default 5) AND more than one distinct operator (a single-operator cell is a privacy proxy
for one human - US-10); comparable cells rank by median Weave. Deterministic and non-identifying.

| Member | Summary |
|---|---|
| `Leaderboard Compose(IReadOnlyList<ScoredEpisode> episodes, ScoreSegment segment, int cohortMinimum = 5)` | **(gap)** |

## `DimensionReason`

*record* — `Leaderboard.cs`

One evidence-backed reason for one dimension (spec US-16 - one reason per dimension).

## `AgentStanding`

*record* — `Leaderboard.cs`

An agent's per-turn standing (spec US-16). It carries the harness-model rank, the recent trend, and
one evidence-backed reason per dimension - and **deliberately no single aggregate scalar** to
optimize (the anti-Goodhart stance: there is no `Score` field, only a relative rank, a trend
direction, and per-dimension evidence).

**Remarks.** **Trend is nullable, and that is the point.** It was `int`, so an agent's first scored
episode reported **0** — the same value as "you did not move" — in the one feature whose
purpose is telling an agent whether it is improving or regressing. The spec is explicit that
"every displayed evaluation or learning claim has evidence/confidence, or renders Not Recorded",
and no-history is exactly that case.

## `StandingComposer`

*class* — `Leaderboard.cs`

Turns a scored episode + the leaderboard into per-turn standing (spec US-16). The rank is shown only
when the harness-model cell is comparable (else RankComparable is false and only trend + reasons
render); the reasons are one per dimension from the scorecard; no single optimizable number is exposed.

| Member | Summary |
|---|---|
| `AgentStanding Compose(` | Composes one episode's standing, deriving the trend from . |

### `AgentStanding Compose(`

Composes one episode's standing, deriving the trend from .

**Remarks.** **The history is a parameter, not the trend.** This took `int trend` and nothing in
src/ produced one — the caller was expected to compute it and there was no caller at all. A
value someone must remember to supply is a value that will eventually be supplied wrongly or
not at all; a history the method derives from cannot be forgotten, because the method cannot
be called without it.

## `LivenessProjection`

*class* — `LivenessProjection.cs`

Computes a session's liveness from its heartbeats and the monotonic clock - a derived view, never
stored (ADR-0001, DM7). Because it uses monotonic elapsed duration, a wall-clock change cannot flip
a session's state (spec US-2).

| Member | Summary |
|---|---|
| `LivenessProjection(IWatcherObservationStore store, IMonotonicClock clock, TimeSpan staleAfter)` | **(gap)** |
| `LivenessState Evaluate(string sessionId)` | Ended if the session was ended or has no heartbeat (an unknown or never-alive session collapses to Ended per the spec); otherwise Alive within the stale threshold, else Stale. |

## `BoardMessageKind`

*enum* — `MessageBoard.cs`

The kinds of Message Board entry (spec US-4). The first four are top-level posts; Reply and
Acknowledgement reference a parent message and cannot create an orphan thread.

## `BoardMessage`

*record* — `MessageBoard.cs`

One append event on a repository's Message Board (spec line 233). The envelope, order, and thread
references are append-only; only a policy redaction may null the `Content` and set
`Tombstoned`, leaving the immutable envelope (spec line 210). `Content` is
**quarantined untrusted data** - it can never instruct a grader (US-4 #4); grader-injection
shapes are additionally `InjectionFlagged` (US-4 #5).

## `GraderInjectionScanner`

*class* — `MessageBoard.cs`

A deterministic scanner for grader/learning-promoter injection shapes in untrusted board content
(US-4 #5). It is a **flag**, not a safety boundary: the invariance guarantee (an injection
fixture never changes a score) comes from the scorer consuming typed deterministic signals rather
than board text (slice 5), not from perfect detection here. A small pattern list, deliberately not
an ML classifier (Simplifier).

| Member | Summary |
|---|---|
| `bool LooksLikeInjection(string? content)` | **(gap)** |

## `IMessageBoard`

*interface* — `MessageBoard.cs`

The per-repository, append-only Message Board (spec US-4).

## `MessageBoardService`

*class* — `MessageBoard.cs`

The default in-process Message Board. Every write is capability-verified (only the authenticated
session posts as itself - LK-0001 on a forged capability); the message carries the session's own
trust as provenance (US-4 #1). A reply/acknowledgement must reference an existing parent **in the
same repository** or it is rejected as an orphan (US-4 #2). Content is stored quarantined and
injection-flagged (US-4 #4/#5). A policy redaction tombstones the payload (US-4 #6).

| Member | Summary |
|---|---|
| `MessageBoardService(` | **(gap)** |
| `BoardMessage Post(string repositoryKey, string sessionId, SessionCapability capability, BoardMessageKind kind, string content)` | **(gap)** |
| `BoardMessage Reply(string repositoryKey, string sessionId, SessionCapability capability, string parentMessageId, string content)` | **(gap)** |
| `BoardMessage Acknowledge(string repositoryKey, string sessionId, SessionCapability capability, string parentMessageId)` | **(gap)** |
| `void Redact(string messageId)` | **(gap)** |

## `IMonotonicClock`

*interface* — `MonotonicClock.cs`

A monotonic time source. Liveness uses elapsed monotonic duration, never the wall clock, so a
wall-clock change (NTP step, timezone, manual set) cannot flip a session's state (spec US-2;
defect class TEST-CLOCK). Abstracted so a test can drive time deterministically.

## `SystemMonotonicClock`

*class* — `MonotonicClock.cs`

The production clock, backed by the high-resolution monotonic `Stopwatch`.

| Member | Summary |
|---|---|
| `long Ticks` | **(gap)** |
| `long TicksPerSecond` | **(gap)** |

## `ObservedSpan`

*record* — `ObservedSpan.cs`

The observation fact grain: one row is exactly one observed operation emitted by one authenticated
session generation, identified by its source span identity, recorded at ingest. Immutable and
append-only (ADR-0017). Phase 1 carries operation metadata only - no prompt/code/transcript
content (that is Phase 5, behind the governance gate).

| Member | Summary |
|---|---|
| `string SpanId { get; } = ComputeId(SessionId, TraceId, SourceSpanId)` | Deterministic content-addressed identity: the same (session, trace, source span) yields the same id, so a redelivered span is a duplicate to ignore rather than a second row. Computed, never supplied. Pattern: LOA 5.3 … |

## `HarnessSpan`

*record* — `OtelSpanMapper.cs`

Transport-neutral harness span. An OTLP receiver or an in-process `ActivityListener`
constructs this, so the mapper is coupled to no single transport (spike S1: the mapping is the
contract, not the wire).

## `HarnessRegistration`

*record* — `OtelSpanMapper.cs`

A harness registration / session-start event, as a bag of attributes.

## `OtelAttributes`

*class* — `OtelSpanMapper.cs`

The pinned OpenTelemetry / GenAI attribute snapshot the ingest wire consumes. The GenAI keys are
marked **Development** upstream, so a change here is a contract change guarded by a regression
test (Testing Strategy A6) rather than silent drift (spike S1 finding 5).

| Member | Summary |
|---|---|
| `string SessionId = "session.id"` | **(gap)** |
| `string ServiceName = "service.name";          // -> Harness name` | **(gap)** |
| `string ServiceVersion = "service.version"` | **(gap)** |
| `string GenAiModel = "gen_ai.request.model";   // -> Model name` | **(gap)** |
| `string GenAiModelVersion = "gen_ai.model.version"` | **(gap)** |
| `string RepoPath = "repo.canonical_path"` | **(gap)** |
| `string RepoDisplay = "repo.display_name"` | **(gap)** |
| `string WorktreeBranch = "worktree.branch"` | **(gap)** |
| `string WorktreePath = "worktree.path"` | **(gap)** |
| `string TerminalId = "terminal.id"` | **(gap)** |
| `string AgentName = "agent.name"` | **(gap)** |

## `OtelSpanMapper`

*class* — `OtelSpanMapper.cs`

Maps harness telemetry into the watcher domain. Pure, deterministic, stateless.

Pattern: Anti-Corruption Layer + Adapter (DDD) - it is the one seam that keeps the preview
OTel/GenAI vocabulary out of the domain, so upstream schema churn changes only this type and its
regression test. It treats a span's `session.id` as a claim, never authority: the wire binds
spans to the capability issued at registration (ADR-0020), so the mapper mints no trust.

| Member | Summary |
|---|---|
| `ObservedSpan MapSpan(HarnessSpan span, DateTimeOffset recordedAt)` | Maps an OTel span to an `ObservedSpan`.  is stamped by the wire at ingest, never trusted from the span (clock-skew prevention). Throws `WatcherException` (LK-0004) when the span carries no `session.id`. |
| `SessionBinding MapRegistration(HarnessRegistration registration)` | Maps a registration event to a `SessionBinding`. Harness and model are absent when their attributes are absent (rendered Not Recorded, spec US-13); trust is `Verified` only when the harness names itself via `service.n… |

## `OtlpJsonParser`

*class* — `OtlpReceiver.cs`

Parses an OTLP/HTTP export in JSON encoding into `HarnessSpan`s, stdlib only (no protobuf
dependency - the harness is configured `OTEL_EXPORTER_OTLP_PROTOCOL=http/json`; slice-1b spike).
Resource and span attributes are merged per span. Malformed JSON yields an empty list, never throws.

| Member | Summary |
|---|---|
| `IReadOnlyList<HarnessSpan> Parse(string otlpJson)` | **(gap)** |

## `ISessionTokenResolver`

*interface* — `OtlpReceiver.cs`

Resolves a per-session bearer token to the session's capability. Unknown token => null.

## `SessionTokenRegistry`

*class* — `OtlpReceiver.cs`

An in-memory token->capability registry the registration flow populates.

| Member | Summary |
|---|---|
| `void Register(string token, SessionCapability capability)` | **(gap)** |
| `SessionCapability? Resolve(string token)` | **(gap)** |

## `OtlpReceiverStats`

*record* — `OtlpReceiver.cs`

A snapshot of the receiver counters (IO1 operator questions).

## `OtlpHttpReceiver`

*class* — `OtlpReceiver.cs`

A loopback-only OTLP/HTTP receiver: it accepts OTLP/JSON exports at `/v1/traces`, resolves the
per-session bearer token to a capability, parses spans, and enqueues them into the ingest host. A
bad body, oversize body, or unknown token is counted and dropped - never enqueued, never fatal
(the exporter is answered 200 so it does not retry a permanent error).

Pattern: Adapter over the ingest host's port. The capability never travels; only the opaque token does.

| Member | Summary |
|---|---|
| `OtlpHttpReceiver(IngestHost host, ISessionTokenResolver tokens, string loopbackPrefix, int maxBodyBytes = 4 * 1024 * 1024)` | **(gap)** |
| `OtlpReceiverStats Stats` | **(gap)** |
| `Task RunAsync(CancellationToken ct)` | Accepts exports until cancelled. One export per POST; one bad request never stops the loop. |
| `void Dispose()` | **(gap)** |

## `ScoreDispute`

*record* — `ScoreDispute.cs`

An operator's dispute of a scored episode (spec US-16 / rule 12). It is an **append-only fact**:
raising a dispute NEVER overwrites the prior Scorecard - "a dispute appends a superseding evaluation
record; prior scores are not overwritten" (spec rule 12). The episode then reads as **Disputed**,
a first-class state that must stay distinguishable from Scored/Blocked/Not Scored (spec §10).

**Remarks.** A dispute may target one `DisputedDimension` (the operator contests one dimension's
assessment) or the whole score (`null`). The `Reason` is the operator's own words -
non-secret, retained as the audit trail of why a score was contested. Resolution (deterministic
evidence or a human disposition producing a new Scorecard version) is a separate, later step; this
records the dispute itself, honestly and immutably.

## `DisputeProjection`

*class* — `ScoreDispute.cs`

The deterministic read over disputes: which episodes are Disputed and how many disputes each carries
(spec §10 - Disputed is derived from the append-only dispute facts, never a stored flag, DM7). Pure;
folds the store's disputes into an episode-keyed view the Sessions/Leaderboard surfaces consult.

| Member | Summary |
|---|---|
| `bool IsDisputed(string episodeId)` | Whether an episode has at least one dispute (its derived Disputed state). |
| `int DisputeCount(string episodeId)` | The number of disputes raised against an episode (an additive count). |
| `IReadOnlySet<string> DisputedEpisodeIds()` | The distinct episode ids that carry at least one dispute. |
| `bool IsSessionDisputed(string sessionId)` | Whether a session has any disputed episode - the session's derived Disputed state for the Sessions surface (US-16 "discoverable from the Sessions view"). A session is disputed iff any of its episodes carries a dispute… |

## `EvidenceComposer`

*class* — `ScoringService.cs`

Composes a closed episode's `DeterministicEpisodeSignals` into the quarantined evidence
token string the `LocalHeuristicAdvisoryEvaluator` grounds on (the
`key=value; key=value` vocabulary). It maps only the signals we actually capture; a dimension the
local heuristic looks for but we do not observe (e.g. `reuse`) is simply omitted, so the
evaluator scores it conservatively rather than optimistically (NG1). Deterministic and pure.

| Member | Summary |
|---|---|
| `string Compose(DeterministicEpisodeSignals signals)` | **(gap)** |

## `ScoringService`

*class* — `ScoringService.cs`

Turns a closed Work Episode + its deterministic signals into a persisted `ScoredEpisode`,
so a scored episode appears on the Leaderboard/Standing surfaces (US-14/US-16). It scores the four
deterministic dimensions always, and folds the two advisory dimensions ONLY when the supplied
evaluator's `(version, taskClass, schemaVersion)` has qualified in the calibration registry
(ADR-0019, rule 8) - otherwise they stay excluded exactly as the deterministic scorer left them.

**Remarks.** Advisory evaluation grounds on `EvidenceComposer`'s token string. Where no evaluator
is supplied (the safe default), only the deterministic Weave is recorded - which is enough to populate
the Leaderboard. The classification (harness/model/operator/taskClass) is supplied by the caller: it
comes from the session binding + the episode, which this pure service does not re-derive.




It never overrides a floor or a Not Scored verdict - that guarantee lives in
`AdvisoryWeaveScorer` (rule 8) and is exercised here end to end.

| Member | Summary |
|---|---|
| `ScoredEpisode ScoreAndRecord(` | Scores the episode and persists the result. When  and are supplied, the advisory dimensions are evaluated from the composed evidence and folded only if qualified; otherwise only the deterministic Weave is recorded. |

### `ScoredEpisode ScoreAndRecord(`

Scores the episode and persists the result. When  and
are supplied, the advisory dimensions are evaluated from the composed
evidence and folded only if qualified; otherwise only the deterministic Weave is recorded.

- **`workspace`** — The repository the work happened in, or `null` when it could not be resolved. Required rather than defaulted so every caller decides: a default here would silently record every score into the unknown cohort, which reads as a working leaderboard with no rows.

## `SessionCapability`

*class* — `SessionCapability.cs`

The unforgeable per-session secret. A process must present the matching capability on every event
(spec US-1). The raw token is never logged or emitted (O11), and comparison is constant-time to
deny a timing side-channel.

Pattern: Capability-based security. The capability is the authority; possessing the session id is
not (ADR-0007 / ADR-0020 - terminal output is forgeable).

| Member | Summary |
|---|---|
| `bool Matches(SessionCapability presented)` | Constant-time equality. Length is compared first only to size the fixed-time compare; the comparison itself does not short-circuit on content. |

## `ICapabilityFactory`

*interface* — `SessionCapability.cs`

Issues session capabilities. Abstracted so a test can inject a deterministic source.

## `CapabilityFactory`

*class* — `SessionCapability.cs`

The production factory: a 256-bit token from a cryptographic RNG.

| Member | Summary |
|---|---|
| `SessionCapability Create()` | **(gap)** |

## `SessionCoordinationIdentity`

*record* — `SessionCoordinationEmitter.cs`

The non-secret identity a terminal/agent session presents when it registers with the watcher - the
attributes the coordination-contract register event carries (US-4/US-6). Harness and model are
optional (a plain shell has neither); everything else is required for a well-formed registration.

| Member | Summary |
|---|---|
| `IReadOnlyDictionary<string, string?> ToAttributes()` | Maps the identity onto the OTel attribute keys the register event uses. |

## `SessionCoordinationEmitter`

*class* — `SessionCoordinationEmitter.cs`

Writes the coordination-contract log a session opts in with so it appears in the watcher (US-4): a
register on start, periodic heartbeats while alive (so liveness stays Alive rather than going Stale),
and a session-end on close. This is the app-side WRITER; the `WatcherHost`'s pump is the
reader. Running both in one process is what makes a terminal launched in the app show up live.

**Remarks.** Pure and explicit (Register / Heartbeat / HeartbeatAll / End) - no timer of its own, so it is
fully testable; the caller (the shell) drives heartbeats on whatever timer it already runs. It tracks
the live session ids so `HeartbeatAll` can keep them all alive with one call.




Re-reading the whole coordination log directory is idempotent on the reader side (registration
is keyed by external id), so a duplicate register is harmless; the emitter still guards against
re-registering an id it already tracks, to keep the log clean.

| Member | Summary |
|---|---|
| `int LiveCount` | The number of sessions currently registered and not yet ended. |
| `void Register(string externalSessionId, SessionCoordinationIdentity identity)` | Registers a session (once) and writes its register event with the identity's attributes. |
| `void Heartbeat(string externalSessionId)` | Writes a heartbeat for one registered session; a no-op for an unknown/ended session. |
| `void HeartbeatAll()` | Heartbeats every live session - the shell calls this on its refresh tick. |
| `void End(string externalSessionId)` | Writes a session-end for a session and stops tracking it; a no-op if unknown. |
| `void Reconcile(IReadOnlySet<string> currentSessionIds, Func<string, SessionCoordinationIdentity> identityFor)` | Reconciles the live set against the sessions that currently exist: registers a new one, heartbeats one already live, and ends one that has gone. This lets the caller drive the emitter from a simple periodic snapshot o… |

## `IngestOutcome`

*enum* — `SpanIngest.cs`

The outcome of attempting to ingest one span.

## `SpanIngest`

*class* — `SpanIngest.cs`

Ingests observed spans, verifying the session capability first (so a forged session cannot write
facts) and then appending idempotently by content-addressed id (ADR-0006 / ADR-0017).

| Member | Summary |
|---|---|
| `SpanIngest(IWatcherObservationStore store, ITrustedRegistrar registrar)` | **(gap)** |
| `IngestOutcome Ingest(string sessionId, SessionCapability capability, ObservedSpan span)` | Verifies the capability, then appends the span. A redelivered or out-of-order span is safe: duplicates return `DuplicateIgnored`, and facts are order-independent. |

## `SqliteWatcherObservationStore`

*class* — `SqliteWatcherObservationStore.cs`

The durable `IWatcherObservationStore` on one SQLite file, reusing the ADR-0002 fact-store
idiom (WAL, append-only facts enforced by triggers, a single writer). It substitutes for
`InMemoryWatcherObservationStore` behind the same seam - the same contract, now persisted
across a restart. Spans are an append-only fact (dedup by content-addressed primary key); sessions,
heartbeats, and the ended flag are current-state cells (upsert), mirroring the in-memory maps.

simplify: one connection guarded by a lock. Ceiling: fine at the reference scale for the skeleton.
Upgrade trigger: read volume grows enough to want the WorkspaceStore read/write connection split.

| Member | Summary |
|---|---|
| `string DatabasePath { get; }` | The backing database file. Exposed so a test can open a raw connection against it. |
| `SqliteWatcherObservationStore Open(string databasePath)` | **(gap)** |
| `bool TryAppendSpan(ObservedSpan span)` | **(gap)** |
| `int SpanCount(string sessionId)` | **(gap)** |
| `int SpanCountInInterval(string sessionId, DateTimeOffset from, DateTimeOffset toInclusive)` | **(gap)** |
| `void UpsertHeartbeat(string sessionId, long monotonicTicks)` | **(gap)** |
| `long? LastHeartbeat(string sessionId)` | **(gap)** |
| `void RecordSession(SessionRecord session)` | **(gap)** |
| `SessionRecord? FindSession(string sessionId)` | **(gap)** |
| `IReadOnlyList<SessionRecord> AllSessions()` | **(gap)** |
| `void RecordEpisode(WorkEpisode episode)` | **(gap)** |
| `WorkEpisode? FindEpisode(string episodeId)` | **(gap)** |
| `IReadOnlyList<WorkEpisode> EpisodesForSession(string sessionId)` | **(gap)** |
| `IReadOnlyList<WorkEpisode> AllEpisodes()` | **(gap)** |
| `void AppendDaydreamObservation(DaydreamObservation observation)` | **(gap)** |
| `IReadOnlyList<DaydreamObservation> AllDaydreamObservations()` | **(gap)** |
| `void AppendDaydreamEvent(DaydreamEvent daydreamEvent)` | **(gap)** |
| `IReadOnlyList<DaydreamEvent> AllDaydreamEvents()` | **(gap)** |
| `void AppendBoardMessage(BoardMessage message)` | **(gap)** |
| `IReadOnlyList<BoardMessage> BoardMessages(string repositoryKey)` | **(gap)** |
| `IReadOnlyList<BoardMessage> AllBoardMessages()` | **(gap)** |
| `BoardMessage? FindBoardMessage(string messageId)` | **(gap)** |
| `void RedactBoardMessage(string messageId)` | **(gap)** |
| `void MarkEnded(string sessionId)` | **(gap)** |
| `void ClearEnded(string sessionId)` | **(gap)** |
| `bool IsEnded(string sessionId)` | **(gap)** |
| `void RecordScorecard(ScoredEpisode scored)` | **(gap)** |
| `ScoredEpisode? FindScoredEpisode(string episodeId)` | **(gap)** |
| `IReadOnlyList<ScoredEpisode> AllScoredEpisodes()` | **(gap)** |
| `void AppendScoreDispute(ScoreDispute dispute)` | **(gap)** |
| `IReadOnlyList<ScoreDispute> DisputesForEpisode(string episodeId)` | **(gap)** |
| `IReadOnlyList<ScoreDispute> AllDisputes()` | **(gap)** |
| `void Dispose()` | **(gap)** |

## `StandingPublisher`

*class* — `StandingPublisher.cs`

Delivers a session's per-turn standing to the agent, as a file beside the contract log (US-16).

**Remarks.** **Why a file.** US-16's deliverable is that the agent **receives** its standing.
C1 added a `standing` tool to `McpToolGateway` — correct, tested, and
unreachable: the gateway has no caller and no transport, and ADR-0004 records the transport as
spiked and never built. Adding a tool nothing can call does not deliver a story about receiving.
`AIDE_CONTRACT_LOG` is the channel that already exists in both directions; the agent is
handed the directory, and the ingest proves the path works.





**It is still a pull.** Nothing is injected into the agent's context — the file sits
there and the agent chooses to read it. That distinction is what ADR-0019's anti-Goodhart section
turns on: an agent shown its score every turn regardless is a different decision from one that
asks.





**The subdirectory and the extension are both load-bearing.**
`CoordinationContractLog` reads `Directory.EnumerateFiles(logDir, "*.jsonl")` with no
`SearchOption` — top-directory-only. A standing written as `.jsonl` in the root would be
parsed by the contract pump every tick and counted MALFORMED, so this feature would work while
the ingest counters filled with corruption that was not corruption. Two independent properties
keep it invisible, and both are asserted by tests rather than assumed.





**No new environment variable.** One address for the channel, with the direction legible
from the path.

| Member | Summary |
|---|---|
| `string DirectoryName = "standing"` | The subdirectory of the coordination log that carries outbound standings. |
| `string GeneratedByField = "generated-by"` | The provenance marker every product-written artifact carries, under the one field name used in every format. |
| `string GeneratedBy = "ai-de/standing-publisher"` | This component's provenance value. |
| `string? Publish(` | Writes the standing for , or returns null when there is none. |
| `string FileNameFor(string sessionId)` | A session id turned into a file name the filesystem will not reinterpret. |

### `string GeneratedByField = "generated-by"`

The provenance marker every product-written artifact carries, under the one field name used in
every format.

**Remarks.** **The boundary is provenance, not permission.** The product and its agents are one
experience to the user and agents write into the repository continuously, so "who may write"
was the wrong question. It is **who maintains what they wrote**: agent-generated is
agent-maintained, product-generated is product-owned. A reader has to be able to tell, and
this is how - present means the product wrote it, absent means an agent or a human did.





**One spelling, everywhere.** `generated-by` literally, in JSON as in markdown
frontmatter - a marker with a per-format spelling is a marker every consumer has to know two
forms of, and a grep for one of them silently misses the other.

### `string? Publish(`

Writes the standing for , or returns null when there is none.

**Returns.** The path written, or `null` when the session has no scored episode.

**Remarks.** Returns null rather than writing an empty standing: an empty one reads as "you have no rank
and no reasons", which is a claim about the agent rather than about the absence of a score
(DC-087). No file is the honest state, and the agent can tell the two apart.

### `string FileNameFor(string sessionId)`

A session id turned into a file name the filesystem will not reinterpret.

**Remarks.** An agent session id is `agent:<name>#<hex>`, and on NTFS a colon opens an
**alternate data stream**: `Path.Combine(dir, "agent:claude#ab.json")` writes the file
"agent" carrying the stream "claude#ab.json". The write succeeds, the bytes are there, and
nothing enumerating the directory can see them. That is DC-086, found in the coordination log
this afternoon; this is the same id reaching the same filesystem by a different route.

## `ITrustedRegistrar`

*interface* — `TrustedRegistrar.cs`

Binds session identity and issues a per-session capability verified on every event (ADR-0020,
extends ADR-0007). Capabilities are held in-process and are never persisted to the observation
store, so the secret never reaches the durable facts.

## `TrustedRegistrar`

*class* — `TrustedRegistrar.cs`

The default in-process registrar. See `ITrustedRegistrar`.

| Member | Summary |
|---|---|
| `TrustedRegistrar(` | **(gap)** |
| `RegisteredSession Register(SessionBinding binding)` | **(gap)** |
| `RegisteredSession RegisterNextGeneration(string sessionId, SessionBinding binding)` | **(gap)** |
| `bool Verify(string sessionId, SessionCapability presented)` | **(gap)** |
| `void Heartbeat(string sessionId, SessionCapability capability)` | **(gap)** |
| `void End(string sessionId, SessionCapability capability)` | **(gap)** |
| `void UpdateHarnessAndModel(` | Records a harness and/or model learned after registration. Identity and trust are untouched. |

### `void UpdateHarnessAndModel(`

Records a harness and/or model learned after registration. Identity and trust are untouched.

**Remarks.** **Only these two fields, and deliberately.** Repository, worktree, terminal and agent
are established at registration and an update cannot restate them — otherwise a session could
migrate itself into another repository's view after the fact.





**Trust never rises.** A registration carrying a harness is classified
`Verified`; one without is `Asserted`. It would be natural to promote a session
that later supplies its harness, and it is exactly wrong: the coordination log is a local,
forgeable FILE (ADR-0007, and the design doc says so in as many words), so an update arriving
on it is evidence about the harness and not about the trustworthiness of the claim. A session
that registers `Asserted` stays `Asserted` with its model filled in.





Capability-gated like every other post-registration write, so knowing an id is not
enough to edit a session.

## `WatcherException`

*class* — `WatcherException.cs`

A watcher-core failure carrying a stable `Code` (Observability Standard O7). The
message is for humans and may change; the code is for machines and search and does not.

| Member | Summary |
|---|---|
| `WatcherException(string code, string message) : base(message)` | **(gap)** |
| `string Code { get; }` | A stable `WatcherErrorCodes` value. |

## `WatcherHost`

*class* — `WatcherHost.cs`

The in-process watcher host: it composes the observation store, the trusted registrar, the ingest
host, the injected coordination-contract ingest + its log pump, and (best-effort) the OTLP network
receiver into one running unit. Running it **in the same process as the read surfaces** is
deliberate: liveness compares monotonic ticks, which are process-relative, so hosting the ingest
beside the panes makes liveness exact - the cross-process caveat conn-2 recorded simply does not
arise here (a heartbeat and the liveness projection read one process's Stopwatch).

**Remarks.** This is composition, not new behaviour (Solution-Selection Ladder rung 2): every part already
exists and is tested in isolation (slices 1, 2, 3). The host wires them and owns their lifetime.




**Two ingest paths, one store.** The **coordination-contract log** (file-based, the
symbiotic path a non-AI-Forward session opts into by writing a register/heartbeat/session-end log
via `CoordContractWriter`) is drained by `PumpOnce` / `RunAsync`;
the **OTLP span** path (network) is drained by the same host when `TryStartOtlp`
started a receiver. Re-reading the whole coordination log directory is idempotent (registration is
keyed by external id), so a periodic pump never double-registers.

| Member | Summary |
|---|---|
| `WatcherHost Open(` | Opens the host: the SQLite watcher store at `<dataDirectory>/watcher.db`, and the coordination-contract log pump over . The registrar and the liveness projection share one monotonic clock so liveness is consistent in-… |
| `string CoordLogDirectory` | The coordination-contract log directory a session opts in by writing to. |
| `SessionCoordinationEmitter CreateEmitter()` | A writer for the coordination log this host reads - a terminal/agent session in the same process registers and heartbeats through it, and the pump ingests it, so the session appears live (US-4). |
| `int ImportEpisodesFromAuditLog(string auditLogPath)` | Imports the closed Work Episodes declared in a repo's AI-Forward audit log (the goal-state entries, AL5b) into the store, so real episodes exist to observe and score. Idempotent by episode id (a re-import of the same … |
| `int ImportAndScoreEpisodesFromAuditLog(` | Imports the workspace's declared-goal episodes (ep-capture) AND auto-scores each one (conn-10). Derives its `DeterministicEpisodeSignals` from the observable audit evidence (a committed Proof Pack, plus any explicit t… |
| `int ScoreClosedEpisodes(` | Scores every closed episode of a registered session that has no scorecard yet, and returns how many were newly scored (US-16's missing link). Delegates to `ClosedEpisodeScoring`, which is where the reasoning lives. |
| `IWatcherObservationStore Store` | The observation store, for the read surfaces (the app builds its queries from this). |
| `LivenessProjection Liveness` | The liveness projection sharing the host's monotonic clock (exact in-process). |
| `IngestHost Ingest` | The ingest host, exposed so an in-process source can enqueue spans directly. |
| `IngestStats Stats` | A snapshot of the ingest counters (IO1 - answerable without a debugger). |
| `int PumpOnce()` | Pumps the coordination-contract log once and drains any queued spans; returns the number of coordination events applied. Idempotent across calls (register is keyed by external id). |
| `Task RunAsync(TimeSpan interval, CancellationToken ct)` | The background loop: pump + drain every  until cancelled. A transient read failure (a log file mid-write) is absorbed and retried next tick - one bad read never kills the loop (US-11 fail honestly), and the store degr… |
| `bool TryStartOtlp(string loopbackPrefix, ISessionTokenResolver tokens, CancellationToken ct)` | Best-effort start of the OTLP HTTP receiver on a loopback prefix (the network span path). Returns false and leaves the host fully functional (coordination path only) when the prefix cannot bind - on Windows an `HttpLi… |
| `void Dispose()` | **(gap)** |

## `WatcherErrorCodes`

*class* — `WatcherIdentity.cs`

Stable, machine-readable error codes for the Loomkeeper observation core. The human-readable
message may change; these codes do not (Observability Standard O7).

| Member | Summary |
|---|---|
| `string ForgeryRejected = "LK-0001"` | A process presented a wrong, absent, or superseded session capability. |
| `string InvalidBinding = "LK-0002"` | A registration binding was missing a required identity field. |
| `string EgressDenied = "LK-0003"` | An egress path was denied because no explicit opt-in enabled it. |
| `string MalformedEvent = "LK-0004"` | A harness event could not be mapped to the domain (missing session or identity attribute). |

## `TrustClassification`

*enum* — `WatcherIdentity.cs`

How well a session's identity is established. Asserted identity cannot clear a floor.

## `LivenessState`

*enum* — `WatcherIdentity.cs`

Observed liveness of a session. Computed from heartbeats, never stored (ADR-0001).

## `RepositoryIdentity`

*record* — `WatcherIdentity.cs`

A repository identity. `CanonicalPath` disambiguates two repositories that share a
folder `DisplayName`, so the fleet map never collapses them (spec US-1).

**Remarks.** **The path is canonicalised on construction, because the field is called
CanonicalPath.** It used to be a plain string that nothing normalised — a name asserting an
invariant no code enforced — while `FleetAggregator` grouped by it with
`StringComparer.Ordinal`. One repository therefore became several: git reports forward
slashes where .NET reports backslashes, Windows paths are case-insensitive, and a trailing
separator is indistinguishable from its absence. That is US-3's second clause failing — an
aliased worktree appearing as a duplicate Repository.





**Fixed on the type rather than in the aggregator** because the same field is the
grouping key in `FleetAggregator`, the persisted column in the store, the registration guard
in `TrustedRegistrar` and the lookup key in the coordination contract. Normalising one
consumer leaves the other three disagreeing about whether two sessions share a repository — and
because it normalises on the way in AND on the way back out of the store, rows written before
this compare equal to rows written after without a migration.





**Case folding is platform-conditional, deliberately.** Windows paths are
case-insensitive and the shipped product is Windows desktop; POSIX paths are not, and folding
there would merge two genuinely distinct repositories — which is the exact collapse
`CanonicalPath` exists to prevent.

| Member | Summary |
|---|---|
| `RepositoryIdentity(string canonicalPath, string displayName)` | **(gap)** |
| `string CanonicalPath { get; init; }` | **(gap)** |
| `string DisplayName { get; init; }` | **(gap)** |
| `string Canonicalise(string path)` | Two spellings of one path become one string; two paths stay two. |

### `string Canonicalise(string path)`

Two spellings of one path become one string; two paths stay two.

**Remarks.** Public because `WorkspaceKey` keys the same directories and two implementations of
one canonicalisation is the shape that drifts apart — a repository grouping one way in the
fleet map and another on the leaderboard would be invisible until the cohorts disagreed.

## `WorkspaceKey`

*record* — `WatcherIdentity.cs`

The workspace a Weave score is keyed to: the **repository**, never the checkout.

**Remarks.** **Why the repository and not the checkout.** A score is workspace-keyed because how an
agent works is partly a product of the repository's directives, conventions and gates. Two
worktrees of one repository *share* all three, so keying on the checkout would segment on the
one axis that carries no difference in what is being measured.





**And the failure would have been quiet.** Splitting a cohort shrinks every leaderboard
cell; a cell under the minimum renders Not Comparable — which is the de-anonymisation guard. The
privacy protection would have fired correctly for a reason that was not privacy, and the surface
would have looked right while meaning something else.





**The repository is already what the product resolves.**
`WorkbenchShell.ResolveGitFacts` takes `--git-common-dir`'s parent, so a linked worktree
and its primary checkout both answer with the primary path — measured in this repository's own two
trees. Nothing *enforces* it, though: an externally registering agent composes its own
`repo.path`, so `From(string?)` is the one place that decides, and
`TheWorkspaceKeyIsTheRepositoryNotTheCheckout` is the control.





**Absence is stated, never defaulted.** An unresolvable workspace yields `null`, and a
null-workspace episode is excluded from every leaderboard cell rather than placed in a cohort of
unknowns. Falling back to the checkout path here would silently reintroduce the split and be
indistinguishable from working.

| Member | Summary |
|---|---|
| `string Value { get; }` | The canonicalised repository path. |
| `WorkspaceKey? From(string? canonicalPath)` | The key for a repository, or `null` when there is no path to key on. |
| `WorkspaceKey? From(RepositoryIdentity? repository)` | The key for a bound session's repository. |
| `string ToString()` | **(gap)** |

## `WorktreeIdentity`

*record* — `WatcherIdentity.cs`

A worktree of a repository.

## `TerminalIdentity`

*record* — `WatcherIdentity.cs`

A terminal hosting an agent session.

## `AgentIdentity`

*record* — `WatcherIdentity.cs`

The coding agent occupying a terminal.

## `HarnessIdentity`

*record* — `WatcherIdentity.cs`

The agent harness (Claude Code, GitHub Copilot, ...). A scoring/aggregation axis.

## `ModelIdentity`

*record* — `WatcherIdentity.cs`

The model behind the harness (Opus 4.8, GPT-5.6 Terra, ...). A scoring/aggregation axis.

## `struct`

*record* — `WatcherIdentity.cs`

A monotonically increasing generation for one session identity. A terminal restart yields a new
generation that cannot inherit the prior generation's liveness, capability, or claims (spec US-1).

| Member | Summary |
|---|---|
| `SessionGeneration Next()` | **(gap)** |

## `SessionBinding`

*record* — `WatcherIdentity.cs`

The identity a session is bound to at registration. `Harness` and `Model`
are nullable: when unknown they render Not Recorded, and the session is still observable (US-13).

## `SessionRecord`

*record* — `WatcherIdentity.cs`

Non-secret session metadata. The capability is deliberately NOT stored here (§Security).

## `RegisteredSession`

*record* — `WatcherIdentity.cs`

The result of a successful registration: the identity, its generation, and its capability.

| Member | Summary |
|---|---|
| `string SessionId` | **(gap)** |
| `SessionGeneration Generation` | **(gap)** |
| `SessionBinding Binding` | **(gap)** |

## `IWatcherObservationStore`

*interface* — `WatcherObservationStore.cs`

The persistence seam for watcher observations. This is the mock-substitutable contract the
architecture names (§4): the in-memory implementation serves the Phase-1 walking skeleton; the
SQLite implementation (extending the existing `Store/`, ADR-0002) replaces it later as a
substitution, not a redesign. It holds non-secret facts only - never a `SessionCapability`.

## `InMemoryWatcherObservationStore`

*class* — `WatcherObservationStore.cs`

In-memory observation store for the walking skeleton. Thread-safe under a single lock: writes
serialize through the daemon queue in production (ADR-0002), but a concurrent caller must still
never corrupt the store or double-append a span.

simplify: unbounded in memory. Ceiling: fine at the reference scale for the skeleton. Upgrade
trigger: the SQLite store lands (remaining Phase-1 task), which bounds and persists these facts.

| Member | Summary |
|---|---|
| `bool TryAppendSpan(ObservedSpan span)` | **(gap)** |
| `int SpanCount(string sessionId)` | **(gap)** |
| `int SpanCountInInterval(string sessionId, DateTimeOffset from, DateTimeOffset toInclusive)` | **(gap)** |
| `void UpsertHeartbeat(string sessionId, long monotonicTicks)` | **(gap)** |
| `long? LastHeartbeat(string sessionId)` | **(gap)** |
| `void RecordSession(SessionRecord session)` | **(gap)** |
| `SessionRecord? FindSession(string sessionId)` | **(gap)** |
| `IReadOnlyList<SessionRecord> AllSessions()` | **(gap)** |
| `void RecordEpisode(WorkEpisode episode)` | **(gap)** |
| `WorkEpisode? FindEpisode(string episodeId)` | **(gap)** |
| `IReadOnlyList<WorkEpisode> EpisodesForSession(string sessionId)` | **(gap)** |
| `IReadOnlyList<WorkEpisode> AllEpisodes()` | **(gap)** |
| `void AppendBoardMessage(BoardMessage message)` | **(gap)** |
| `void AppendDaydreamObservation(DaydreamObservation observation)` | **(gap)** |
| `IReadOnlyList<DaydreamObservation> AllDaydreamObservations()` | **(gap)** |
| `void AppendDaydreamEvent(DaydreamEvent daydreamEvent)` | **(gap)** |
| `IReadOnlyList<DaydreamEvent> AllDaydreamEvents()` | **(gap)** |
| `IReadOnlyList<BoardMessage> BoardMessages(string repositoryKey)` | **(gap)** |
| `IReadOnlyList<BoardMessage> AllBoardMessages()` | **(gap)** |
| `BoardMessage? FindBoardMessage(string messageId)` | **(gap)** |
| `void RedactBoardMessage(string messageId)` | **(gap)** |
| `void MarkEnded(string sessionId)` | **(gap)** |
| `void RecordScorecard(ScoredEpisode scored)` | **(gap)** |
| `ScoredEpisode? FindScoredEpisode(string episodeId)` | **(gap)** |
| `IReadOnlyList<ScoredEpisode> AllScoredEpisodes()` | **(gap)** |
| `void AppendScoreDispute(ScoreDispute dispute)` | **(gap)** |
| `IReadOnlyList<ScoreDispute> DisputesForEpisode(string episodeId)` | **(gap)** |
| `IReadOnlyList<ScoreDispute> AllDisputes()` | **(gap)** |
| `void ClearEnded(string sessionId)` | **(gap)** |
| `bool IsEnded(string sessionId)` | **(gap)** |

## `ScoreDimension`

*enum* — `WeaveScore.cs`

The six Weave dimensions (spec §"Weave Score"). Four carry deterministic signals; two are advisory.

## `FloorDomain`

*enum* — `WeaveScore.cs`

The canonical hard floors (spec rule 6). A trip in any of these produces a Blocked verdict.

## `AssessmentPosture`

*enum* — `WeaveScore.cs`

How a dimension was assessed. An advisory or un-signalled dimension is NotRecorded, never a fake 0.

## `WeaveVerdict`

*enum* — `WeaveScore.cs`

The honest verdict of a Scorecard.

## `DimensionWeight`

*record* — `WeaveScore.cs`

One dimension's weight and posture within a versioned score schema.

## `ScoreSchema`

*class* — `WeaveScore.cs`

A versioned score schema (spec rule 1/13; A6 - a change is a gated contract change). `weave/1`
pins the four deterministic dimensions (Outcome 30 · Focus 15 · Guidance 15 · Coordination 10 =
observed weight 70) and the two advisory ones (Evidence 15 · Economy 15 = 30), which are excluded
from points until the grader passes its calibration gates (ADR-0019, slice 7).

| Member | Summary |
|---|---|
| `string Weave1Version = "weave/1"` | **(gap)** |
| `string Version { get; }` | **(gap)** |
| `IReadOnlyList<DimensionWeight> Dimensions { get; }` | **(gap)** |
| `int TotalWeight` | **(gap)** |
| `ScoreSchema Weave1 { get; } = new(Weave1Version,` | **(gap)** |

## `DimensionAssessment`

*record* — `WeaveScore.cs`

One dimension's assessment. `EarnedPoints` is null unless the dimension was scored.

## `EvidenceCoverage`

*record* — `WeaveScore.cs`

Evidence Coverage - observed required signals / required signals (spec rule 3). Not a multiplier.

## `DeterministicEpisodeSignals`

*record* — `WeaveScore.cs`

The deterministic evidence gathered about a closed episode - the scorer's pure input. Populating this
from the store / coordination log / verification ingest is the wiring follow-on; the engine is pure.

## `Scorecard`

*record* — `WeaveScore.cs`

One evaluation of one closed episode under one schema version at one evaluation time (spec line 236).

## `WeaveScorer`

*class* — `WeaveScore.cs`

The deterministic Weave scorer. Pure, model-free: it turns a closed Work Episode's deterministic
evidence into an honest `Scorecard` - per-dimension 0-4 normalized to weight, the tripped
hard floors, Evidence Coverage, and a verdict. Advisory dimensions are declared-and-excluded, never
stubbed with fake numbers; a tripped floor suppresses the numeric headline; a missing
goal/done/verification path is Not Scored; a Partial headline never rescales to 0-100 (spec rules 1-9).

This is where `done_when` becomes measured: Focus-and-termination counts work after the done
condition, and Outcome-integrity checks the honest completion claim - the PACK-O drift / under-
validation faces (the AI-Forward goal-state work).

| Member | Summary |
|---|---|
| `Scorecard Score(WorkEpisode episode, DeterministicEpisodeSignals signals, TimeProvider time)` | **(gap)** |
| `Scorecard Score(WorkEpisode episode, DeterministicEpisodeSignals signals, ScoreSchema schema, TimeProvider time)` | **(gap)** |

## `Goal`

*record* — `WorkEpisode.cs`

A session-authored objective. Immutable; a change starts a new episode (spec line 211).

## `DoneCondition`

*record* — `WorkEpisode.cs`

The done-condition - the **terminal condition** against which the episode's outcome is judged
(the AI-Forward `done_when`: "point at a result and say whether it is met", CT19). Immutable.

## `struct`

*record* — `WorkEpisode.cs`

An episode's ordinal within its session's sequential episode chain (1, 2, 3 …).

| Member | Summary |
|---|---|
| `SessionGeneration Next()` | **(gap)** |

## `EpisodeOutcome`

*enum* — `WorkEpisode.cs`

The DECLARED lifecycle terminal state of an episode - not a quality score. Whether a
`Completed` claim is *honest* (the goal was actually met vs. drifted past the
done-condition) is the Weave's Outcome-integrity dimension (slice 5), deliberately not decided here.

## `EpisodeState`

*enum* — `WorkEpisode.cs`

Whether an episode is still open or has closed (derived from `ClosedAt`).

## `WorkEpisode`

*record* — `WorkEpisode.cs`

One Work Episode: one immutable goal + done-condition over one bounded interval of one session
(spec US-6, lines 201-234). The **unit scoring attaches to**. It mirrors the AI-Forward CT19
goal-state triple (Goal / DoneWhen / NotInScope) so it is the durable, scoreable projection of a
turn's goal-state, not a parallel structure.

| Member | Summary |
|---|---|
| `EpisodeState State` | Active until closed; Closed carries the interval end and the declared outcome. |

## `IWorkEpisodeService`

*interface* — `WorkEpisode.cs`

The Work Episode lifecycle. Only the authenticated session may open, reframe, or close *its*
episodes - every call presents the session capability and is verified (LK-0001 forgery on mismatch,
ADR-0020). Times use the wall-clock `TimeProvider` - the same base as span
`RecordedAt` - because an episode binds *recorded* activity, not a *live* condition.

## `WorkEpisodeService`

*class* — `WorkEpisode.cs`

The default in-process episode service. See `IWorkEpisodeService`.

| Member | Summary |
|---|---|
| `WorkEpisodeService(` | **(gap)** |
| `WorkEpisode Open(string sessionId, SessionCapability capability, Goal goal, DoneCondition doneWhen, string? notInScope = null)` | **(gap)** |
| `WorkEpisode Reframe(string episodeId, SessionCapability capability, Goal goal, DoneCondition doneWhen, string? notInScope = null)` | **(gap)** |
| `WorkEpisode Close(string episodeId, SessionCapability capability, EpisodeOutcome outcome)` | **(gap)** |

## `EpisodeProjection`

*class* — `WorkEpisode.cs`

The deterministic read projection over episodes (ADR-0001; DM7 derive-don't-store). It computes an
episode's state and the observable activity bound to its interval - spans whose `RecordedAt`
falls in `[OpenedAt, ClosedAt ?? now]` - never a stored tally.

| Member | Summary |
|---|---|
| `int ObservedSpanCount(WorkEpisode episode)` | The spans observed inside the episode's interval (an open episode uses `now` as the end). |
| `IReadOnlyList<WorkEpisode> ForSession(string sessionId)` | The session's episodes in generation order (its sequential episode chain). |
