---
id: api-aide-core
title: "API: AiDe.Core"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core: 2 types, 14 members, 50% carrying a summary doc comment.
---

# API: `AiDe.Core`

**2 public types · 14 public members · 50% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `WorkspaceCore`

*class* — `WorkspaceCore.cs`

The in-process authority core (ADR-0009): the composition root the shell talks to in Phase 1 and
the same contract a separate daemon exposes over IPC from Phase 2, so the split is a deployment
substitution rather than a redesign.

| Member | Summary |
|---|---|
| `string WorkspaceId { get; }` | **(gap)** |
| `string RootPath { get; }` | **(gap)** |
| `string DataDirectory { get; private set; } = string.Empty` | Where workspace-local state lives. The layout file sits beside the fact store (ADR-0013). |
| `WorkspaceStore Store { get; }` | **(gap)** |
| `ProjectionService Projections { get; }` | **(gap)** |
| `DispatchService Dispatch { get; }` | **(gap)** |
| `McpToolGateway Mcp { get; }` | **(gap)** |
| `HealthIncidentSidecar Incidents { get; }` | **(gap)** |
| `WorkspaceCore Open(string workspaceId, string rootPath, string dataDirectory, IExtractor? extractor = null)` | Opens a workspace and runs recovery before serving anything. Sweeping first is deliberate: a caller must never be able to observe an unresolved attempt and read it as "never sent". |
| `Task<ExtractionResult> RefreshScopeAsync(` | Extracts one scope and commits it as a complete snapshot. An incomplete extraction is recorded as a health incident and the previous snapshot stands — a failed refresh never empties the graph. |
| `Task<IndexResult> IndexCSharpAsync(` | Discovers every C# scope under the workspace root and refreshes each one. |
| `IReadOnlyList<(string ScopeId, int Generations)> CheckCompactionNeeded(` | Raises a health incident for any scope whose generation count has passed the compaction threshold. |
| `string DatabasePath` | The path compaction operates on. Compaction requires the store to be closed. |
| `void Dispose()` | **(gap)** |

### `Task<ExtractionResult> RefreshScopeAsync(`

Extracts one scope and commits it as a complete snapshot. An incomplete extraction is recorded
as a health incident and the previous snapshot stands — a failed refresh never empties the graph.

- **`rootPathOverride`** — What the extractor should read, when it is not the workspace root. A C# scope is one PROJECT built for one framework, so the request must carry that project's path — the workspace root names the repository, not the thing being extracted.

### `Task<IndexResult> IndexCSharpAsync(`

Discovers every C# scope under the workspace root and refreshes each one.

- **`force`** — Re-extract every scope even when its inputs are unchanged. The escape hatch for "I do not believe the cache", which is a thing an operator must always be able to say.

**Remarks.** **Per scope, not per repository.** Each project/framework pair gets its own budget,
its own generation and its own snapshot, so one project that fails to load quarantines itself
and leaves every other project's evidence standing (`P2-EXT-02`).





**The per-scope budget is enforced here.** A 60-second cap per scope, from the
design — applied with a linked token so a caller cancelling the whole index still stops
everything, while one slow project cannot consume the entire run.

### `IReadOnlyList<(string ScopeId, int Generations)> CheckCompactionNeeded(`

Raises a health incident for any scope whose generation count has passed the compaction
threshold.

**Remarks.** P1-PERF measured refresh going over budget at roughly ten generations of the same scope. The
growth is the append-only design working as intended, so the operator is told rather than the
slowdown being absorbed silently — a workspace that has quietly become slow is the shape of
problem people stop reporting and start working around.

This reports; it does not compact. Compaction replaces the database file, so it belongs to a
deliberate maintenance moment, not to a background timer that could fire mid-session.

## `IndexResult`

*record* — `WorkspaceCore.cs`

The result of indexing a whole repository's C# scopes.
