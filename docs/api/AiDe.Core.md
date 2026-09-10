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
  Extracted public surface of AiDe.Core: 3 types, 15 members, 61% carrying a summary doc comment.
---

# API: `AiDe.Core`

**3 public types · 15 public members · 61% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `PathComparison`

*class* — `PathComparison.cs`

How two filesystem paths compare on the machine this is running on.

**Remarks.** **One rule, named once, because spelling it inline is how it keeps going wrong.** The
same defect has now been fixed four times — INV-0005, `RepositoryIdentity.ToFileSystemPath`,
`RepositoryCorrection`/`ProofPackVerifier`, and the three containment
checks in `Extraction/` and `Projections/` — and every instance was the same line
written from memory at a new call site. `WatcherIdentity` states the rule in prose
(*"THIS IS NOT A FILESYSTEM PATH"*) and prose is a memoir, so this is the member every
path comparison consults and `tools/verify-containment-comparisons.py` is the gate that
refuses a fifth hand-written copy.





**Case-insensitive ONLY on Windows.** POSIX paths are case-sensitive:
`/repo/Secrets` and `/repo/secrets` are two different directories, and folding the
case there admits a path the rest of the system would never write. On Windows they are one
directory, so folding is what the filesystem itself does and refusing it would be a claim about
the caller's typing rather than about the file.





**Not a `FileSystemPath` type.** That distinction was considered and rejected:
the defects it was proposed for all operate on strings that genuinely ARE filesystem paths, so
a wrapper type would not have caught any of them, and it would leave a permanent `.Value`
escape hatch for the next one to live in. The bug is the comparison rule, so the fix is a
comparison rule.

| Member | Summary |
|---|---|
| `StringComparison ForThisFileSystem` | The `StringComparison` that matches this machine's filesystem: ordinal everywhere, and case-insensitive additionally on Windows. |

### `StringComparison ForThisFileSystem`

The `StringComparison` that matches this machine's filesystem: ordinal
everywhere, and case-insensitive additionally on Windows.

**Remarks.** A property rather than a `static readonly` field so it is evaluated per call rather
than at type-initialisation. Nothing in this process changes operating system mid-run, but a
cached platform answer is the shape that survives into a context where it is wrong, and the
ternary costs nothing next to the string comparison it qualifies.

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
| `void Dispose()` | Closes what this object opened — both stores. |

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

### `void Dispose()`

Closes what this object opened — both stores.

**Remarks.** The watcher store is disposed here because `Open` created it. Whoever opens a handle
owns closing it, and the alternative — taking the store as a constructor parameter so the
caller owns its lifetime — would move that ownership rather than remove it.

## `IndexResult`

*record* — `WorkspaceCore.cs`

The result of indexing a whole repository's C# scopes.
