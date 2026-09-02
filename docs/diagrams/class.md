---
id: diagram-class
title: "Class diagram — scoring and coordination types"
type: doc
status: current
owner: "@timianmalloo"
phase: "0"
tags: [diagram, class, uml, watcher, scoring]
links:
  - { to: architecture, rel: documents }
  - { to: diagram-sequence, rel: relates-to }
  - { to: spec-agentic-watcher-substrate, rel: relates-to }
review-by: 2027-09-02
summary: >-
  The types behind the Weave score and the leaderboard, drawn from the declarations in
  WeaveScore.cs and Leaderboard.cs, including the two fields the design deliberately does not have.
---

# Class diagram — scoring and coordination

Scope: `AiDe.Core.Watcher`, the scoring half. Every member shown is a public declaration in the
named file; nothing is drawn that does not exist.

```mermaid
classDiagram
  direction LR

  class WeaveScorer {
    +Score(WorkEpisode, DeterministicEpisodeSignals, TimeProvider) Scorecard
    +Score(WorkEpisode, DeterministicEpisodeSignals, ScoreSchema, TimeProvider) Scorecard
    ~ComposeScoredCard(...) Scorecard
    note: pure and model-free
  }

  class ScoreSchema {
    +string Version
    +IReadOnlyList~DimensionWeight~ Dimensions
    +int TotalWeight
    +ScoreSchema Weave1$
  }

  class DimensionWeight {
    +ScoreDimension Dimension
    +int Weight
    +AssessmentPosture Posture
  }

  class DimensionAssessment {
    +ScoreDimension Dimension
    +int Weight
    +int? Rubric0to4
    +double? EarnedPoints
    +AssessmentPosture Posture
    +string Rationale
  }

  class Scorecard {
    +string EpisodeId
    +string SchemaVersion
    +WeaveVerdict Verdict
    +IReadOnlyList~DimensionAssessment~ Assessments
    +IReadOnlyList~FloorDomain~ TrippedFloors
    +EvidenceCoverage? Coverage
    +string Headline
    +DateTimeOffset EvaluatedAt
  }

  class EvidenceCoverage {
    +int Observed
    +int Required
  }

  class DeterministicEpisodeSignals {
    +bool HasVerificationPath
    +bool? AcceptanceCriteriaMet
    +bool RequiredVerificationExecuted
    +bool RegressionPresent
    +IReadOnlySet~FloorDomain~ UnresolvedFloorBlockers
    +int ActionsAfterDoneCondition
    +bool PrematureCompletion
    +bool CoverageCalibrated
  }

  class ScoredEpisode {
    +string EpisodeId
    +string? Harness
    +string? Model
    +string OperatorId
    +string TaskClass
    +Scorecard Scorecard
    +double Weave
    +double? CoverageRatio
    +bool IsScoreable
  }

  class LeaderboardComposer {
    +Compose(episodes, taskClass, schemaVersion, cohortMinimum) Leaderboard
  }

  class Leaderboard {
    +string TaskClass
    +string SchemaVersion
    +IReadOnlyList~LeaderboardCell~ Cells
    +Cell(LeaderboardFacet, string) LeaderboardCell
  }

  class LeaderboardCell {
    +LeaderboardFacet Facet
    +string Label
    +int Cohort
    +double? MedianWeave
    +double? Coverage
    +int? Rank
    +bool Comparable
    +string? NotComparableReason
  }

  class AgentStanding {
    +int? Rank
    +int? Cohort
    +int Trend
    +bool RankComparable
    +IReadOnlyList~DimensionReason~ Reasons
    note: no aggregate scalar, by design
  }

  WeaveScorer ..> ScoreSchema : reads
  WeaveScorer ..> DeterministicEpisodeSignals : consumes
  WeaveScorer --> Scorecard : produces
  ScoreSchema *-- DimensionWeight
  Scorecard *-- DimensionAssessment
  Scorecard o-- EvidenceCoverage
  ScoredEpisode *-- Scorecard
  LeaderboardComposer ..> ScoredEpisode : segments
  LeaderboardComposer --> Leaderboard : composes
  Leaderboard *-- LeaderboardCell
  StandingComposer ..> Leaderboard : reads
  StandingComposer --> AgentStanding : produces
```

## The two absences worth naming

A class diagram usually argues from what a type has. These two types argue from what they refuse
to have, and the refusals are load-bearing:

- **`ScoredEpisode` has no stored score.** `Weave` is a computed property — the sum of the scored
  dimensions' earned points. Two definitions of one quantity is a defect signature, so there is
  exactly one, derived on read.
- **`AgentStanding` has no `Score` field.** An agent shown its own standing every turn is shown a
  relative rank, a trend direction, and one evidence-backed reason per dimension. There is
  deliberately no single number for it to optimise.

## Enumerations

| Enum | Members | Note |
|---|---|---|
| `ScoreDimension` | OutcomeIntegrity · FocusAndTermination · EvidenceDiscipline · GuidanceAdherence · SolutionEconomy · CoordinationAndLearning | Four deterministic, two advisory in `weave/1`. |
| `AssessmentPosture` | Deterministic · Advisory · NotRecorded | An un-signalled dimension is NotRecorded, **never a fake 0**. |
| `WeaveVerdict` | Scored · Partial · Blocked · NotScored | Partial never rescales to 0–100. |
| `FloorDomain` | Correctness · Security · Privacy · DataIntegrity · EvaluatorIntegrity | Any trip produces Blocked and suppresses the numeric headline. |
| `LeaderboardFacet` | Harness · Model · HarnessModel | Deliberately no per-operator facet. |
| `BoardMessageKind` | Question · Decision · Breadcrumb · KnowledgeCandidate · Reply · Acknowledgement | The last two reference a parent and cannot create an orphan thread. |

## Confidence

| Claim | Label | Basis |
|---|---|---|
| Every member drawn | Verified | Declarations in `src/AiDe.Core/Watcher/WeaveScore.cs` and `Leaderboard.cs`. |
| `Weave` and `AgentStanding` absences | Verified | The computed property and the record declaration; both carry the reason in their own doc comments. |
| Enum members | Verified | The enum declarations, in source order. |
