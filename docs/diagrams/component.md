---
id: diagram-component
title: "Component diagram — AI-DE"
type: doc
status: current
owner: "@timianmalloo"
phase: "0"
tags: [diagram, component, c4, architecture]
links:
  - { to: architecture, rel: documents }
  - { to: diagram-layers, rel: relates-to }
review-by: 2027-09-02
summary: >-
  The real assembly and namespace dependency edges of AI-DE, read from the composition roots rather
  than from the architecture prose, with the external systems at the boundary.
---

# Component diagram — AI-DE

Every edge below was read from a composition root or a `using` list, not from the architecture
document. Where the architecture names a component that Phase 1 satisfies in-process, the diagram
shows what the code actually does today and the note says what the boundary becomes.

```mermaid
flowchart TB
  classDef ext fill:#0D1014,stroke:#98A3B2,stroke-dasharray:4 3,color:#98A3B2
  classDef app fill:#1A1F26,stroke:#5B9DD9,color:#E4E9EF
  classDef core fill:#1A1F26,stroke:#2A313B,color:#E4E9EF

  subgraph shell["AiDe.App — WPF shell"]
    WorkbenchShell["WorkbenchShell<br/>composition + watcher loop"]
    SurfaceFactory["SurfaceContentFactory<br/>15 surface kinds"]
    MainMenu["MainMenuBuilder<br/>derives menu from the command catalog"]
  end

  subgraph corelib["AiDe.Core"]
    WorkspaceCore["WorkspaceCore<br/>in-process authority core"]
    Store["Store<br/>WorkspaceStore · schema · reader/writer"]
    Extraction["Extraction<br/>7 extractor families"]
    Projections["ProjectionService<br/>graph · paths · content · search · joins"]
    Dispatch["DispatchService<br/>prompt delivery receipts"]
    Mcp["McpToolGateway<br/>bounded read tools"]
    Health["HealthIncidentSidecar"]
    Ipc["Ipc<br/>pipe · framing · capability registry"]
    Terminal["Terminal<br/>ConPTY sessions"]
    Watcher["WatcherHost<br/>registrar · ingest · board · scoring"]
    Upgrade["Upgrade<br/>preflight and rollback state"]
  end

  Daemon["AiDe.Daemon<br/>one process · one workspace · one pipe"]

  Repo[("Repository files")]
  Sqlite[("SQLite<br/>workspace store")]
  WatcherDb[("SQLite<br/>watcher observations")]
  Agent["Agent process<br/>any harness"]
  Otlp["OTLP HTTP receiver<br/>loopback, optional"]

  WorkbenchShell --> SurfaceFactory
  WorkbenchShell --> MainMenu
  WorkbenchShell --> WorkspaceCore
  WorkbenchShell --> Watcher
  SurfaceFactory --> Projections
  SurfaceFactory --> Terminal
  SurfaceFactory --> Watcher

  WorkspaceCore --> Store
  WorkspaceCore --> Extraction
  WorkspaceCore --> Projections
  WorkspaceCore --> Dispatch
  WorkspaceCore --> Mcp
  WorkspaceCore --> Health
  Mcp --> Projections
  Projections --> Store
  Extraction --> Store
  Dispatch --> Store

  Daemon --> WorkspaceCore
  Daemon --> Ipc
  Daemon --> Upgrade
  Ipc --> WorkspaceCore

  Extraction -.reads.-> Repo
  Store -.writes.-> Sqlite
  Watcher -.writes.-> WatcherDb
  Terminal -.ConPTY.-> Agent
  Agent -.coordination log / OTLP.-> Otlp
  Otlp -.spans.-> Watcher
  Agent -.contract log.-> Watcher

  class shell,WorkbenchShell,SurfaceFactory,MainMenu app
  class Repo,Sqlite,WatcherDb,Agent,Otlp ext
```

## Boundary notes

| Edge | What it is today | What it becomes |
|---|---|---|
| `AiDe.App` → `WorkspaceCore` | A direct in-process call. `CallerPrincipal` is simply true. | An authenticated local pipe to `AiDe.Daemon`, with the principal established by the transport from the connection and never from anything the caller sends (ADR-0009). |
| `AiDe.Daemon` startup | Takes the workspace lock **before** a pipe exists. | Unchanged — the order is the startup contract. A daemon that served first and discovered second would already have published an endpoint, and two daemons on one workspace are two writers to one store. |
| `Extraction` → repository | In-process adapters, one per language family. | A versioned process/JSON boundary only when an extractor needs language or runtime isolation. |
| `WatcherHost` hosting | In the **same process** as the read surfaces, deliberately: liveness compares monotonic ticks, which are process-relative. | Unchanged while liveness is exact by construction. |

## Confidence

| Claim | Label | Basis |
|---|---|---|
| Assembly and namespace edges | Verified | `WorkspaceCore` constructor, `AiDe.Daemon/Program.cs` using list, `SurfaceContentFactory` constructor parameters, `WatcherHost` fields. |
| External systems at the boundary | Verified | `SqliteWatcherObservationStore`, `WorkspaceSchema`, `ConPtyInterop`, `OtlpReceiver`. |
| The Phase-2 daemon split | Verified as *intent* | Stated in ADR-0009 and in the `AiDe.Daemon` doc comment; the process exists and the split is not yet the shell's default path. |
