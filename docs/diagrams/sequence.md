---
id: diagram-sequence
title: "Sequence diagrams — extraction and agent registration"
type: doc
status: current
owner: "@timianmalloo"
phase: "0"
tags: [diagram, sequence, extraction, watcher, coordination]
links:
  - { to: forensic-review-rev102, rel: relates-to }
  - { to: architecture, rel: documents }
  - { to: diagram-component, rel: relates-to }
  - { to: spec-agentic-watcher-substrate, rel: relates-to }
review-by: 2027-09-02
summary: >-
  The two flows the product turns on: repository change to rendered surface, and terminal launch to
  a scored, attributed episode — with the failure and refusal paths drawn, not only the happy one.
review-suggested: []
---

# Recovered sequence paths and failure boundaries

Source-recovered scenarios at `9f13ca7a`. These diagrams describe production control flow,
not a captured runtime trace. **Verified** arrows cite the composition below; defects are drawn
as actual missing protections, not idealized checks.

```mermaid
sequenceDiagram
 participant User as Operator
 participant UI as Desktop shell
 participant Boot as ShellBootstrap
 participant D as Workspace daemon
 participant Q as Core projections
 User->>UI: Open local workspace
 UI->>Boot: ConnectOrLaunchAsync
 Boot->>D: Owner-scoped pipe handshake
 D-->>UI: Workspace query/command client
 UI->>D: Bounded graph or source query
 D->>Q: Query current stored evidence
 Q-->>D: Rows, provenance and disclosures
 Note over Q,D: FR-002: entry-point row cap does not enforce serialized bytes
 D-->>UI: Framed response, or explicit transport failure
```

```mermaid
sequenceDiagram
 participant Repo as Repository content
 participant Index as IndexCSharpAsync
 participant Refresh as RefreshScopeAsync
 participant Store as Snapshot store
 Repo->>Index: Content changed, same supplied revision
 Index->>Index: Fingerprint changed or force=true
 Index->>Refresh: Refresh scope with supplied revision
 Refresh->>Store: Read latest committed revision
 Store-->>Refresh: Same revision, nonempty snapshot
 Refresh-->>Index: Complete, zero assertions (reused)
 Index-->>Repo: Reports scope indexed; old symbol remains
 Note over Repo,Store: FR-001 reproduced; changed-revision control refreshes correctly
```

```mermaid
sequenceDiagram
 participant Operator as Human operator
 participant Composer as Conversation and composer
 participant Compiler as Compile / typed validation
 participant Journal as Envelope events
 participant Agent as ACP or terminal lane
 Operator->>Composer: Draft goal and context
 Composer->>Compiler: Deterministic or explicitly gated model compilation
 Compiler->>Journal: Append compilation provenance
 Compiler-->>Operator: Reviewable typed result
 Operator->>Agent: Deliberate send through chosen lane
 Agent-->>Composer: Streamed result / receipt / failure
 Note over Agent: External provider/tool capabilities need separate qualification
```

Sources: `MainWindowViewModel.cs:252`, `WorkspaceCore.cs:154–160,402–498`,
`ProjectionService.cs:704–730`, `EnvelopeStore`, App composer/conductor and `BoundaryDispatcher`.
Two-phase terminal dispatch and ACP streaming have different receipt semantics; no diagram claims
one common exactly-once guarantee. Periodic independent freshness requeue is not composed today.
The product's static sequence viewer draws type-level call-site scenarios; runtime/method-level
causality is still to build. Registration/episode flows are described in the watcher spec and API
bundle; they are not evidence that a session without environment registration is scored.
