---
id: diagram-class
title: "Class diagram — scoring and coordination types"
type: doc
status: current
owner: "@timianmalloo"
phase: "0"
tags: [diagram, class, uml, watcher, scoring]
links:
  - { to: forensic-review-rev102, rel: relates-to }
  - { to: architecture, rel: documents }
  - { to: diagram-sequence, rel: relates-to }
  - { to: spec-agentic-watcher-substrate, rel: relates-to }
review-by: 2027-09-02
summary: >-
  The types behind the Weave score and the leaderboard, drawn from the declarations in
  WeaveScore.cs and Leaderboard.cs, including the two fields the design deliberately does not have.
review-suggested:
  - { by: architecture, on: 2026-10-18, reason: "2026-10-18 workbench-loops architecture: §WL added, ADR-0039..0045 proposed, Perspective set re-rowed (ADR-0045 supersedes ADR-0017/0030 rows)" }
---

# Recovered class and durable-data model

Current source-level model, **Verified** against `WorkspaceCore`, `EvidenceAssertion`,
`WorkspaceStore`, `ProjectionService` and `WorkEpisode`. This is a selected explanatory slice,
not an exhaustive inventory of public classes. Native class-association inference has the separate
FR-004 ambiguity/ownership caveat.

```mermaid
classDiagram
 class WorkspaceCore {
  +string WorkspaceId
  +string RootPath
  +RefreshScopeAsync()
  +IndexCSharpAsync()
 }
 class WorkspaceStore {
  +long CoreEpoch
  +BeginRead()
  +BeginWrite()
 }
 class ProjectionService {
  +Graph()
  +EntryPoints()
  +SearchContent()
 }
 class EvidenceAssertion {
  +string ScopeId
  +string ArtifactRevision
  +string Subject
  +string Predicate
  +string Object
  +EvidenceOrigin Origin
  +VerificationStatus Status
 }
 class Provenance {
  +string ArtifactPathId
  +string SourceLocation
  +string ExtractorId
  +string ExtractorVersion
 }
 class WorkEpisode {
  +string EpisodeId
  +string SessionId
  +Goal Goal
  +DoneCondition DoneWhen
  +EpisodeOutcome Outcome
 }
 WorkspaceCore *-- WorkspaceStore
 WorkspaceCore --> ProjectionService
 ProjectionService --> WorkspaceStore
 EvidenceAssertion *-- Provenance
```

## Durable grain and history

One fact is one extractor's normalized subject/predicate/object assertion at one artifact revision.
`WorkspaceSchema.cs` contains dimensions (`workspace_dim`, `node_dim`, `session_dim`), append-only
scope/receipt/dispatch/prompt facts and an explicitly rebuildable `claim_current_cache`. Update/delete
triggers protect immutable tables. Scope generation and core epoch gate commits. Separate watcher
observations and session/envelope journals are not merged into the code-fact schema by assumption.

The proposed factory WorkItem/Criterion/Assessment/Decision model is **Inferred future design** in
the proposal, not a recovered class or a new implemented table. Source/test facts do not establish
semantic acceptance solely because they can be linked.
