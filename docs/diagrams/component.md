---
id: diagram-component
title: "Component diagram — AI-DE"
type: doc
status: current
owner: "@timianmalloo"
phase: "0"
tags: [diagram, component, c4, architecture]
links:
  - { to: forensic-review-rev102, rel: relates-to }
  - { to: architecture, rel: documents }
  - { to: diagram-layers, rel: relates-to }
review-by: 2027-09-02
summary: >-
  The real assembly and namespace dependency edges of AI-DE, read from the composition roots rather
  than from the architecture prose, with the external systems at the boundary.
review-suggested: []
---

# Recovered component and container map — 2026-10-17

Current composition at `9f13ca7a`, replacing the prior diagram's stale claim that the shell
defaults to in-process Core. Diagram scope is deployable/container and major component boundaries,
not an exhaustive call graph. All solid edges below are **Verified source composition**; provider
network behavior is a boundary whose live policy remains separately qualified.

```mermaid
flowchart TB
 Operator[Local operator] --> App[AiDe.App — WPF / AvalonDock]
 App --> Client[Local pipe client and shell view models]
 Client --> Daemon[AiDe.Daemon — workspace lock and owner-scoped pipe]
 Daemon --> Core[AiDe.Core — workspace authority]
 Repo[Repository files — untrusted input] --> Extract[Static extractors]
 Extract --> Core
 Core --> Store[(workspace.db: assertions and snapshots)]
 Core --> Query[Bounded projections]
 Query --> Client
 App --> Session[Conversation / composer / compile / run]
 Session --> Envelope[Append-only compilation envelopes]
 Session --> Harness[ACP / ConPTY harness process]
 Harness --> Provider[Optional inference / remote services]
 App --> Watcher[Watcher / board / episode scoring]
 Watcher --> WatchDb[(watcher.db and contract records)]
 Harness --> Stdio[AiDe.Mcp — stdio coordination tools]
 Stdio --> WatchDb
 Client --> Views[Graph / tree / evidence / static UML / source]
```

| Claim | Confidence and observed source |
|---|---|
| Shell's normal daemon path | Verified — `MainWindowViewModel.cs:252`, `ShellBootstrap.ConnectOrLaunchAsync` |
| One workspace authority/store | Verified — `AiDe.Daemon/Program.cs`, `WorkspaceCore.Open`, `WorkspaceStore` |
| Extractor set | Verified — `WorkspaceExtractors.Default`: C#, Python, TypeScript, Bicep, EF, SQL, knowledge plus fixture fallback |
| Native/hosted surface composition | Verified — `SurfaceContentFactory.Kinds` and `WorkbenchShell` selection/query wiring |
| ACP/compile/ConPTY and watcher are distinct paths | Verified — App conductor/session roots and `WatcherHost` |
| Stdio graph gateway availability | Verified distinction — `AiDe.Mcp/Tools.Schema` exposes identity/board/episode, not Core graph tools |
| Provider runtime safety and fidelity | Flagged for version-bound live qualification; local desktop does not mean local inference |

Freshness and byte-bound defects are in FR-001/002. A graph whose source revision is stale is not
promoted to current truth by this diagram. Full user-visible factory/PR/test/risk workflows remain
proposed; see the build inventory rather than extrapolating from a registered control.
