---
id: api-aide-core-store
title: "API: AiDe.Core.Store"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Store: 10 types, 52 members, 69% carrying a summary doc comment.
---

# API: `AiDe.Core.Store`

**10 public types · 52 public members · 69% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `NodeMatchKind`

*enum* — `NodeSearchHit.cs`

Why a node came back from a search.

**Remarks.** A result whose relevance is invisible reads as a wrong result. Searching `addEventListener`
and being shown a class called `Element` is correct and looks like a bug until the row says
`has_member = addEventListener`.

## `NodeSearchHit`

*record* — `NodeSearchHit.cs`

One search hit: the node, why it matched, and the text that matched.

## `CompactionResult`

*record* — `StoreCompactor.cs`

What a compaction did, so the operator can see it happened and what it cost.

## `StoreCompactor`

*class* — `StoreCompactor.cs`

Prunes superseded scope generations by rebuilding the store, never by deleting facts.

**Remarks.** **Why this exists.** P1-PERF measured refresh p95 at 192 ms on a fresh store, 567 ms
after ten generations of the same scope, and 785 ms after twenty — against a 500 ms budget. The
cause is the append-only design working as intended: every re-extraction leaves its predecessor
behind, and index maintenance grows with the table. A morning's editing puts a workspace outside
its budget.





**Why rebuild-and-swap.** Fact tables carry immutability triggers and the writer forbids
REPLACE, so there is no legitimate DELETE path — and manufacturing one (dropping the triggers,
or a "privileged" bypass) would hollow out the invariant everywhere in order to fix performance
in one place. Instead a new database is built containing only the retained facts, verified
against the original, and swapped in atomically. The invariant is never suspended; the old file
simply stops being the current one.





**What is safe to drop.** Only the latest *complete* snapshot per scope contributes
to current evidence — that is already the store's read rule. Older generations are diagnostics.
Retaining more than one keeps a short history for investigation; retaining zero is not offered,
because a scope with no committed snapshot would have no evidence at all.

| Member | Summary |
|---|---|
| `int DefaultThreshold = 1` | Generations per scope beyond which compaction runs. |
| `int DefaultRetain = 1` | Generations kept per scope after a compaction — the one that renders. |
| `IReadOnlyList<(string ScopeId, int Generations)> ScopesNeedingCompaction(` | Scopes whose generation count has passed the threshold. |
| `CompactionResult Compact(int retain = DefaultRetain, int threshold = DefaultThreshold)` | Rebuilds the store keeping only the most recent  generations per scope. The store must be closed: compaction replaces the file. |

### `int DefaultThreshold = 1`

Generations per scope beyond which compaction runs.

**Remarks.** **One, because the rebuild turned out to be cheap and the waste turned out not to be.**
The original eight came from the P1-PERF latency curve — refresh is inside budget at five
prior generations and over it by ten — and it answered the question "when does this start to
hurt?". It never answered "how big does the store get?", and that is the one a user sees.





MEASURED on a real workspace: at just **two** generations per scope — far under the
old threshold, so nothing ever fired — the store was **53.3 MB of which 27.9 MB was
superseded**. Compacting took **1.09 seconds** and halved it. Deciding there was nothing
to do takes **1–34 ms**. A threshold that never fires on real usage is not a threshold, it
is an opinion.

### `int DefaultRetain = 1`

Generations kept per scope after a compaction — the one that renders.

**Remarks.** **Two was speculative and it was costing double.** The extra generation was kept "for
investigation", and nothing could investigate it: every read in this codebase composes with
the latest-generation filter, and the one reader that takes a generation explicitly is handed
the latest. History no query can reach is not history, it is residue — and the audit log, the
change log and the incident sidecar are where this project actually records what happened.





Safe at one because every committed snapshot is complete: a failed extraction returns
before committing anything, so the newest snapshot is always the one that renders.

## `StoreReader`

*class* — `StoreReader.cs`

A snapshot read. The connection is pinned `query_only=1`, so this path cannot write even by
accident (spike S6).

| Member | Summary |
|---|---|
| `(long Generation, string ArtifactRevision, int AssertionCount)? LatestCommittedSnapshot(string scopeId)` | The latest committed generation for a scope, or null if nothing complete exists yet. |
| `IReadOnlyList<StoredAssertion> CurrentAssertions(string scopeId)` | Current evidence for a scope: assertions of the latest COMPLETE snapshot only. A partial or superseded snapshot never contributes, so the graph cannot silently mix generations. |
| `IReadOnlyList<StoredAssertion> AssertionsTouching(string nodeId, int limit)` | Assertions where the node is the subject OR the object, bounded in SQL. |
| `int CountAssertionsTouching(string nodeId)` | Total assertions touching a node, so a bounded read can report what it omitted. |
| `IReadOnlyList<StoredAssertion> OutgoingAssertions(string nodeId, int limit)` | Outgoing edges only — one traversal step of a bounded impact walk. |
| `StoredAssertion? DeclaringAssertion(string nodeId)` | Assertions with a given predicate — the knowledge projection's entry point.  The assertion that says where a node was DECLARED — its scope and its path within it. |
| `IReadOnlyList<(string Callee, string Member, string Location)> OutgoingCallsInOrder(` | One caller's outgoing calls, in the order they are written. |
| `IReadOnlyList<(string NodeId, string ScopeId, string ArtifactPath)> FilesToSearch(int limit)` | The distinct files the graph knows about, each with a node that is declared in it. |
| `string? ScopeLocation(string scopeId)` | Where a scope's files live, relative to the workspace root, or null when it never said. |
| `(IReadOnlyList<(string NodeId, string Type)> Rows, int TotalMatched) KnowledgeNodes(` | The ids currently classified as knowledge.  The knowledge nodes a query asks for, with their declared type, and how many matched in all. |
| `IReadOnlySet<string> KnowledgeNodeIds(int limit)` | **(gap)** |
| `IReadOnlyList<StoredAssertion> AssertionsWithPredicate(string predicate, int limit)` | **(gap)** |
| `IReadOnlyList<string> ReadDeclaredSubjects()` | Node identities matching a substring, with the total matched so omissions are reportable.  Subjects this workspace's own artifacts DECLARE — the things it owns. |
| `(IReadOnlyList<NodeSearchHit> Matches, int TotalMatched) SearchNodes(string term, int limit)` | Nodes whose identity contains the term, and nodes one of whose ATTRIBUTE VALUES does. |
| `long HighestDesiredGeneration()` | The highest generation any scope has ever been asked for, or 0 for an empty store. |
| `string CurrentSourceRevision()` | The source revision currently rendered, for a result's provenance header. |
| `IReadOnlyList<StoredAssertion> CurrentAssertionPage(` | All current assertions across every scope that has a complete snapshot.  A page of the current assertions, ordered stably, starting after . |
| `IReadOnlyList<StoredAssertion> AllCurrentAssertions()` | **(gap)** |
| `DispatchReceipt? ReadDispatchReceipt(string dispatchKey)` | Folds a dispatch key's attempt + outcome events into one displayed receipt. |
| `IReadOnlyList<string> PendingDispatchKeys()` | Dispatch keys with an attempt but no outcome — what recovery must resolve. |
| `string? ReadCommandOutcome(string workspaceId, CallerPrincipal caller, string commandType, string commandId)` | **(gap)** |
| `(long Generation, SessionProcessingClass ProcessingClass)? ReadSession(string sessionId)` | **(gap)** |
| `string? ReadNodeKind(string nodeId)` | **(gap)** |
| `string? ReadNodeLabel(string nodeId)` | **(gap)** |
| `IReadOnlyCollection<string> ReadKnowledgeIds(IReadOnlyCollection<string> nodeIds)` | Which of  the workspace classifies as knowledge. |
| `IReadOnlyList<(string Subject, string Predicate, string Object, string Status, int Count, string Revision)>` | Reads the labelled cache. Provably equal to its derivation — see the rebuild test. |
| `void Dispose()` | **(gap)** |

### `IReadOnlyList<StoredAssertion> AssertionsTouching(string nodeId, int limit)`

Assertions where the node is the subject OR the object, bounded in SQL.

**Remarks.** Deliberately a UNION ALL of two single-column lookups rather than one `OR` predicate:
SQLite will not use two different indexes to satisfy one OR, so the OR form degrades into
the full scan this method exists to avoid (measured, P1-PERF 2026-08-26).

### `StoredAssertion? DeclaringAssertion(string nodeId)`

Assertions with a given predicate — the knowledge projection's entry point.

The assertion that says where a node was DECLARED — its scope and its path within it.

**Remarks.** Asked of the store rather than picked out of a neighbour list, because a neighbour list
is capped. The content reader first filtered `AssertionsTouching(id, 50)` for the fact
carrying a path, and on a node with 244 edges that fact was not among the first 50 — so the
most connected types in a real workspace reported "no recorded source" while the least
connected ones worked. DC-035, in code written the same day the class was recorded twice.





Ordered so a declaration wins over a mention: `has_type` and `declared_in` are
what a producer emits ABOUT a thing it declared, and their provenance is that thing's own
file. Any other assertion's path is where it was REFERRED to.

### `IReadOnlyList<(string Callee, string Member, string Location)> OutgoingCallsInOrder(`

One caller's outgoing calls, in the order they are written.

**Remarks.** The interaction, as opposed to the relationship. `calls` is deduplicated to one
row per `(caller, callee)` pair — right for a graph, where the same relationship written
seven times is one arrow, and wrong for a sequence diagram, where `A→B, A→C, A→B` must
stay three messages. A diagram that silently drops a repeat is confidently incomplete.





**Ordered by source position, because that is the only order there is.** No ordinal
column was added: every assertion already carries `source_location` as `line:col`,
and a call sequence has exactly one correct order — the order it is written in. Sorted
numerically rather than as text, or line 10 would come before line 9.

### `IReadOnlyList<(string NodeId, string ScopeId, string ArtifactPath)> FilesToSearch(int limit)`

The distinct files the graph knows about, each with a node that is declared in it.

**Remarks.** The corpus a content search may read. It is the set of files the EXTRACTORS chose to
index, not the directory tree: walking the tree would open `node_modules`, `bin`
and every generated bundle the readers already decided to skip, and would return hits in
files the graph cannot navigate to — a result nobody can act on.





One representative node per file, chosen the same way `DeclaringAssertion`
chooses one: a declaration before a reference, then lowest id for determinism. A file holds
many nodes and the hit needs somewhere to go, not everywhere it could go.

### `string? ScopeLocation(string scopeId)`

Where a scope's files live, relative to the workspace root, or null when it never said.

**Remarks.** Written by the core when it indexes a scope, because the core is what chose the path. An
empty string is a real answer — the scope IS the workspace root — and is distinct from null,
which means nothing recorded it and no content can be resolved.

### `(IReadOnlyList<(string NodeId, string Type)> Rows, int TotalMatched) KnowledgeNodes(`

The ids currently classified as knowledge.

The knowledge nodes a query asks for, with their declared type, and how many matched in all.

**Remarks.** `node_kind` is the dimension that separates knowledge from source — the one thing that
knows, now that `has_type` is emitted by six extractors and says nothing about which
half of the graph a node belongs to (INV-0004).

**The term and the type are applied HERE, not to the result.** The caller used to
take 200 knowledge ids in id order and then filter them by term in memory, so a search only
ever saw the alphabetically first 200 of 1,255 — and a document whose id sorted later was
reported as not existing. That is DC-035 for the third time in this file: a bounded read
whose filter is applied to the RESULT of the read rather than expressed in it.





The total is counted over the same filtered set, so a caller can say what it left out
instead of presenting a truncation as an answer.

### `IReadOnlyList<string> ReadDeclaredSubjects()`

Node identities matching a substring, with the total matched so omissions are reportable.

Subjects this workspace's own artifacts DECLARE — the things it owns.

**Remarks.** A leading-wildcard LIKE cannot use an index, so this selects only the identity columns: it
scans a covering index rather than hydrating every row's provenance, which is what made the
naive version cost a full-corpus materialization.

Distinct from every node in the graph, which also contains external package types a
repository merely depends on. Any denominator that counts those is measuring the wrong
population — bounded-context coverage above all, because nobody can assign
`Azure.Storage.Blobs.BlobClient` to a context in their own codebase.

### `(IReadOnlyList<NodeSearchHit> Matches, int TotalMatched) SearchNodes(string term, int limit)`

Nodes whose identity contains the term, and nodes one of whose ATTRIBUTE VALUES does.

**Remarks.** Identity-only search cannot answer the question a person actually asks. Searching
`addEventListener` found ONE node by id and could not find the class that HAS that
member; searching a Bicep resource's deployed name found the name and not the resource.
MEASURED across TheTerrace: matching attribute values adds 1–14 nodes per term that identity
search cannot reach at all, and they are the ones a person meant.





**An attribute match returns the node that OWNS the attribute, never the value.**
That is why the original query excluded attribute objects: a value is not a node, and putting
`api_version = 2023-01-01` in a result list as though it were a thing you can navigate
to is how dates ended up in the graph. The exclusion was right about the object and wrong
about the subject — the owner is a real node, and it is the answer.





Each row carries WHY it matched and the matched text, because a result whose relevance
is invisible reads as a wrong result. The evidence is truncated in SQL rather than after, so
a long value never crosses the boundary just to be trimmed on the far side.

### `long HighestDesiredGeneration()`

The highest generation any scope has ever been asked for, or 0 for an empty store.

**Remarks.** The in-memory counter starts at zero on every open, so without this a workspace's SECOND
index after a restart re-uses generation 1 and violates the desired-generation primary key.
The daemon opens the store fresh every time it starts, which made "index, restart, index"
a guaranteed failure — found by a test that indexed twice across a reopen, which nothing had
done before.

### `IReadOnlyList<StoredAssertion> CurrentAssertionPage(`

All current assertions across every scope that has a complete snapshot.

A page of the current assertions, ordered stably, starting after .

**Remarks.** A deliberate full read, used only where the whole set IS the answer — the claim-cache rebuild.
Bounded reads must never call this: at 50,000 edges it costs roughly 350 ms of materialization
no matter how small the caller's result is (measured, P1-PERF 2026-08-26).

**Paged because the caller is across a pipe.** The panes want every current
assertion — 12,085 of them on one real repository — and they were reconstructing that set one
node at a time through `Describe`, which is bounded at 50 neighbours per node and lost
two join edges out of 124 doing it. A single unbounded response would blow the result-byte
cap instead, so the answer is neither: pages, with a cursor.





The cursor is the last row's `(subject, predicate, object)` — the same tuple the
ORDER BY uses, so a page boundary cannot skip or repeat a row. An id-based cursor would order
by something the query does not, which is how paging quietly loses records.

### `IReadOnlyCollection<string> ReadKnowledgeIds(IReadOnlyCollection<string> nodeIds)`

Which of  the workspace classifies as knowledge.

**Remarks.** Reads the same `node_class = knowledge` fact `GraphProjection` reads, rather
than deciding again from the kind. Two authorities on what counts as knowledge is the shape
of INV-0004 — where a hardcoded default rendered a table, a bicep resource and a class all as
"source" — and two definitions of one quantity is a defect signature in its own right.





Bounded by the caller's id list, so it costs what the result costs rather than what the
graph costs (P1-PERF-02); an empty input reads nothing at all.

## `StoredAssertion`

*record* — `StoreReader.cs`

An assertion as stored, carrying its computed identity back out.

## `StoreWriter`

*class* — `StoreWriter.cs`

The single writer's unit of work. Every append allocates the next ingress sequence, which is the
total order for all facts — wall-clock never orders anything.

**Remarks.** Pattern: Unit of Work. The writer deliberately exposes no general SQL surface and never uses
INSERT OR REPLACE / UPSERT on a fact table: REPLACE is the documented bypass of the immutability
triggers (spike S4), so forbidding it in the writer API is the control, and the pragma is the net.

| Member | Summary |
|---|---|
| `long NextIngressSequence()` | Allocates the next total-order position. Monotonic within the workspace. |
| `void DesireScopeGeneration(string scopeId, long generation, string artifactRevision)` | **(gap)** |
| `(long Generation, string ArtifactRevision)? ReadDesired(string scopeId)` | The latest desired (generation, revision) pair for a scope, or null if none. |
| `void CommitSnapshot(` | Commits a complete snapshot and its assertions in ONE transaction, but only when the worker's (generation, revision) pair still equals the durable desired pair. A late or stale worker is rejected here rather than bein… |
| `void RecordCommandReceipt(` | Records a completed command. Its uniqueness is what makes a retry return the original outcome. |
| `void RecordDispatchAttempt(` | The write-ahead half of ADR-0010: durable BEFORE any byte leaves the process. |
| `void RecordDispatchOutcome(string dispatchKey, DispatchState state, string? errorCode)` | Appends an outcome event. Never rewrites a prior row — a late acceptance appends. |
| `void SavePromptRevision(string draftId, int revisionNo, string body)` | **(gap)** |
| `void UpsertSession(string sessionId, long generation, SessionProcessingClass processingClass, string displayName)` | **(gap)** |
| `void UpsertNode(string nodeId, string nodeKind, string displayLabel)` | Records a node's kind and label, closing the previous row when either changes. |
| `void Commit()` | **(gap)** |
| `void Dispose()` | **(gap)** |

### `void UpsertNode(string nodeId, string nodeKind, string displayLabel)`

Records a node's kind and label, closing the previous row when either changes.

**Remarks.** **Unchanged is a NO-OP, deliberately.** This is a Type-2 dimension: every call used
to close the current row and open a new one, so re-indexing a workspace rewrote the history
of every node that had not changed. History whose every row is an artefact of re-running the
indexer cannot answer the question it exists for — "when did this change?" — because the
answer is always "just now".





It also removed a flip-flop: while a node's kind was computed per scope, a node
declared by one scope and referenced by another alternated between rows on every index.

## `StoreErrorCodes`

*class* — `WorkspaceStore.cs`

Stable error codes this layer can raise. Never a bare exception message.

| Member | Summary |
|---|---|
| `string ImmutableViolation = "AIDE-STORE-IMMUTABLE-VIOLATION"` | **(gap)** |
| `string ScopeGenerationStale = "AIDE-SCOPE-GENERATION-STALE"` | **(gap)** |
| `string EpochStale = "AIDE-AUTH-EPOCH-STALE"` | **(gap)** |

## `WorkspaceStoreException`

*class* — `WorkspaceStore.cs`

A store-layer failure carrying a stable, greppable code.

| Member | Summary |
|---|---|
| `string ErrorCode { get; } = errorCode` | **(gap)** |

## `WorkspaceStore`

*class* — `WorkspaceStore.cs`

One workspace, one SQLite file. Owns the single writer, the core epoch, and the total fact order.

**Remarks.** Pattern: Repository + Unit of Work (narrow). SQLite has no nested transactions
(spikes/sqlite-fact-store S8), so one write transaction per aggregate operation is the contract,
enforced by the writer semaphore rather than an ambient transaction scope.

| Member | Summary |
|---|---|
| `long CoreEpoch { get; }` | Monotonic, store-persisted, incremented inside the open transaction. Never random and never clock-derived, so "stale" is decidable and an old epoch value cannot recur (ABA). |
| `WorkspaceStore Open(string databasePath)` | **(gap)** |
| `StoreWriter BeginWrite()` | Acquire the single writer. Blocks until the previous writer completes — the control path waits rather than erroring, which is the right shape for one local operator. |
| `StoreReader BeginRead()` | A read connection pinned to `query_only=1`, so a read path physically cannot write (spike S6) — reads never queue behind the writer. |
| `void Dispose()` | **(gap)** |
