---
id: api-aide-core-projections
title: "API: AiDe.Core.Projections"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Projections: 47 types, 66 members, 65% carrying a summary doc comment.
---

# API: `AiDe.Core.Projections`

**47 public types · 66 public members · 65% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `ContentMatch`

*record* — `ContentSearch.cs`

One line of a workspace file that contains the term.

## `ContentSearchResult`

*record* — `ContentSearch.cs`

What a corpus content search found, and what it did not look at.

## `ContextView`

*record* — `ContextProjection.cs`

One context as the domain view draws it.

## `CrossingMember`

*record* — `ContextProjection.cs`

One symbol-level edge that crosses a context boundary.

## `ContextEdge`

*record* — `ContextProjection.cs`

One relationship between contexts, and how much traffic it carries.

**Remarks.** **A count is not evidence.** "Editorial → Football, 47 edges" is a number the user cannot
check, act on, or disagree with; the 47 edges are the thing they came for, and until the count
can be opened it is an assertion about their code that they have to take on trust. Capped
because a crossing can run to thousands and a pane that renders all of them is a pane that
stops responding — `Weight` stays the true total, so the cap never becomes a
quieter wrong number.

| Member | Summary |
|---|---|
| `int MemberCap = 200` | **(gap)** |
| `double DominanceShare = 0.5` | The share of listed members one object must reach before the crossing is called dominated. |
| `int Undisclosed` | How many edges exist beyond the ones listed. |
| `CrossingMember? DominantTarget` | The single object most of this crossing points at, or null when no one object dominates. |
| `int DominantCount` | How many listed members point at `DominantTarget`. |

### `double DominanceShare = 0.5`

The share of listed members one object must reach before the crossing is called dominated.

**Remarks.** Half. Not tuned — chosen because "most of this crossing is one thing" is the claim being
made, and a majority is what that sentence means. A lower bar would flag ordinary coupling
to a widely-used type; a higher one would have missed nothing real yet and is not worth the
extra digit of false precision.

### `CrossingMember? DominantTarget`

The single object most of this crossing points at, or null when no one object dominates.

**Remarks.** **Found by eye once, so now it is computed.** On a real repository 57 of the 72
Football-to-Operations edges were one class — `AppDbContext` — which made a boundary
that mostly holds look like one that never did. Nothing in the tool said so; a human read a
list. A signal a person has to notice is a signal that gets noticed once.





Computed over the LISTED members, so a capped crossing reports the share of what it
actually examined rather than extrapolating to the full weight. `Undisclosed`
stays beside it, because a majority of 200 out of 4,000 is a different statement.

## `UncoveredGroup`

*record* — `ContextProjection.cs`

Symbols outside every context, gathered by the namespace they live in.

**Remarks.** **A percentage is not a task.** "68% covered" tells the user a number and gives them nowhere
to start; six namespaces ranked by size tells them which declaration to write next. The grouping
is presentation only — nothing here assigns a symbol to a context, because a symbol placed in
"the nearest" context is inference dressed as a declaration, which is exactly what ADR-0016
rejected folder convention for.

## `ContextMapView`

*record* — `ContextProjection.cs`

The domain view: contexts, what connects them, and what belongs to none.

**Remarks.** **Absence is not coverage.** Measured on a second repository, which has no
`bounded-contexts.yaml`: the pane reported zero uncovered symbols and said "every declared
symbol belongs to a context", which is the sentence a fully-mapped codebase produces. Nothing was
wrong with the arithmetic — no map means no uncovered list — and that is exactly why it needed a
separate field rather than a cleverer count.

| Member | Summary |
|---|---|
| `bool IsValid` | **(gap)** |

## `ContextProjection`

*class* — `ContextProjection.cs`

Groups the graph by declared bounded context (ADR-0016).

**Remarks.** **This is what makes the joins legible.** A `maps_to` edge inside one context is
unremarkable; the same edge crossing two contexts is a coupling someone chose, and until the
contexts are drawn there is no way to tell those apart.





**Uncovered symbols are counted, never assigned.** Placing a symbol in "the nearest"
context would be inference dressed as a declaration — the exact thing ADR-0016 rejected folder
convention for.

| Member | Summary |
|---|---|
| `ContextMapView Compute()` | **(gap)** |

## `EvidenceRead`

*record* — `EvidenceRead.cs`

A read of the evidence, and how much of it the read actually saw.

**Remarks.** **Every number the panes show is computed from a bounded read.** The shell searches with
a result cap, describes a bounded number of those matches, and takes a bounded number of
neighbours from each. On a repository the size of the ones measured so far all three caps are
slack, and nothing about the output would change if they were not — the crossing counts, the join
counts and the coverage percentage would simply be smaller, and still be presented as facts.





**This is the same defect this project keeps finding, one layer up.** A cap that
silently truncates turns a correct count into a confident wrong one: the member cap on a crossing
keeps its true weight beside the listed members for exactly this reason, and the coverage
denominator was fixed twice for counting the wrong population. A bound the user cannot see is a
bound they cannot allow for.





**It degrades to "not known", never to a plausible wrong number.** When the read is
complete `Shortfall` is null and nothing is said; when it is not, it names which cap
bit and by how much.

| Member | Summary |
|---|---|
| `EvidenceRead Empty { get; } = new([], 0, 0, 0, 0)` | **(gap)** |
| `bool IsComplete` | True when every matched node was read and none hit the neighbour limit. |
| `string? Shortfall` | What the read did not see, in words, or null when it saw everything. |

### `string? Shortfall`

What the read did not see, in words, or null when it saw everything.

**Remarks.** Both causes are reported, not just the first. They are different problems with different
fixes — one is "this workspace is bigger than the search cap", the other is "these particular
nodes are unusually connected" — and collapsing them into one sentence would leave the reader
guessing which they have.

## `GraphCluster`

*record* — `GraphOverview.cs`

One group of nodes, drawn as a single thing.

## `ClusterEdge`

*record* — `GraphOverview.cs`

A relationship between two groups, and how much of it there is.

## `WorkspaceOverview`

*record* — `GraphOverview.cs`

The workspace at a distance, and what it stands for.

## `OverviewQuery`

*record* — `GraphOverview.cs`

How to summarise the workspace.

## `GraphOverview`

*class* — `GraphOverview.cs`

The workspace summarised into groups, for a graph too large to show node by node.

**Remarks.** **The half of DC-035 that was still open.** The bounded default fixed the transport
failure by drawing 1,500 of 2,118 declared nodes and saying so — which is honest, and is still a
truncation rather than an overview. A user looking at a repository wants to see its SHAPE first;
1,500 dots is not a shape, and the 618 that were dropped are not the difference between
understanding it and not.





**Grouping by identifier prefix, and why that is not a hack.** Every id in this graph is
already hierarchical because the languages are: a C# symbol is
`TheTerrace.Features.Competitions.Season`, a module is `src/app/models`. The first
`Depth` segments name the thing a developer would call "where that lives", and increasing
Depth is exactly the zoom control a level-of-detail view needs. No clustering algorithm is used,
deliberately: a community-detection result is unstable under small changes to the graph, so the
same repository would regroup between two indexes and the picture would move for reasons the user
cannot see.





**What it must never do is hide the count.** A group drawn as one dot standing for 240
types is only honest while the 240 is on it — that is the whole difference between an overview and
a smaller lie.

| Member | Summary |
|---|---|
| `int MaxDepth = 6` | Deepest grouping worth offering; past this a group is usually one node. |
| `WorkspaceOverview Summarise(WorkspaceGraph graph, OverviewQuery query)` | Summarise a graph that has already been projected and filtered. |
| `string GroupFor(string id, int depth)` | The group an id belongs to at a given depth. |

### `string GroupFor(string id, int depth)`

The group an id belongs to at a given depth.

**Remarks.** **Public because a renderer needs the SAME answer.** A canvas colouring detail nodes
by group, or drawing a node inside its cluster, has to agree with the overview about which
group a node is in. Two definitions of one grouping is the shape of DC-022 — a predicate with
two producers — and the divergence would show as a node drawn in the wrong cluster, which
looks like a layout bug and is not one.





Both separators are handled because both are real: C# symbols are dotted, modules are paths,
and a scope-prefixed id like `bicep:main#siteName` has neither. An id with fewer segments
than the depth IS its own group — grouping it under a shorter prefix that no other node shares
would invent a container the repository does not have.

## `PathQuery`

*record* — `GraphPaths.cs`

Which route through the graph to look for.

## `GraphPath`

*record* — `GraphPaths.cs`

One route, as the edges that make it up.

| Member | Summary |
|---|---|
| `VerificationStatus Status` | The weakest evidence anywhere on the route. |

### `VerificationStatus Status`

The weakest evidence anywhere on the route.

**Remarks.** A chain is only as good as its worst link: one Inferred edge in a run of Verified ones makes
the whole claim inferred, and presenting the route without saying so would launder a guess
into a fact. `VerificationStatus` is ordered strongest-first, so the weakest is
the maximum.

## `PathResult`

*record* — `GraphPaths.cs`

The routes found, and what the search could not tell you.

## `GraphPaths`

*class* — `GraphPaths.cs`

How one node reaches another.

**Remarks.** **The question impact analysis is actually asking.** "What does this touch" is answered
by a neighbourhood; "how does the scheduler end up writing to the fixtures table" is a route, and
until now nothing could answer it. A user who can see two nodes and an edge count still cannot
tell whether a change here reaches there, or through what.





**Shortest routes only, and it says so.** Enumerating every path between two nodes in a
graph of 8,602 edges is exponential and would not be read if it succeeded. This returns the
SHORTEST routes — all of them at that length, up to the cap — because the shortest route is the
one a reader can hold, and a longer one that avoids it is a different question ("is there another
way") that should be asked deliberately rather than answered by accident.





**Directed, because dependency is directed.** `A depends_on B` does not mean B
depends on A, and a route that walks an edge backwards would answer "these are related" while
looking like "a change here reaches there".

| Member | Summary |
|---|---|
| `PathResult Find(WorkspaceGraph graph, PathQuery query)` | The shortest routes from one node to another, within the graph the query names. |

## `GraphNode`

*record* — `GraphProjection.cs`

One node of the whole graph, with how connected it is.

**Remarks.** **Without this the graph is mostly not the user's code.** Measured on a real repository: the
six most-connected nodes were `string`, `int`, `Task<TResult>`,
`DateTimeOffset`, `IReadOnlyList<T>` and `Guid` — 773 edges to
`string` alone. Ranking by raw degree therefore puts the BCL at the centre of a picture of
somebody's domain, and capping by raw degree drops their code to keep it.

**Reported by the user: the Knowledge chip read 0 on a repository holding 2,343 knowledge
nodes.** The graph carried each node's fine `Kind` — and on that repository the knowledge
kinds are `spec` and `knowledge-epl-fan-platform`, which is a name that repository
invented. A chip matching a fixed list of type names cannot work across repositories, and
widening the list only moves the problem to the next repository (DC-033).

The coarse class is a DECLARED dimension — the producer says it (`node_class`) — so a filter
can ask the question directly instead of recognising spellings.

## `GraphEdge`

*record* — `GraphProjection.cs`

One relationship, with the status of the evidence behind it.

## `GraphQuery`

*record* — `GraphProjection.cs`

Which part of the graph to build.

**Remarks.** **Why filtering belongs HERE and not at the caller.** A tool that wants the domain model
would otherwise fetch 2,813 nodes across a pipe and discard nine tenths of them, and — worse —
the CAP would have already chosen which nodes to send, by a ranking computed over a graph the
caller did not want. Filtering after a cap gives you the wrong 5,000 nodes trimmed to the right
kind, and nothing in the result says so.





So the filter runs BEFORE the cap, degree is computed over what survives it, and "most
connected" means most connected *in the graph that was asked for*.





**The group filter takes no depth, on purpose.** A group id already states its own depth
— `TheTerrace.Features` is two segments and `src/app` is two — so the depth is read back
out of the id rather than passed alongside it. A separate parameter would let a caller ask for
`TheTerrace.Features` at depth 3 and receive nothing, with no error and no way to tell that
from an empty group.

**Why excluding rather than selecting.** MEASURED on TheTerrace: the canvas's own default
spends **702,425 of 852,680 bytes on edges** — 82% — and two predicates are 74% of them
(`depends_on` 2,155, `calls` 1,272). Edges, not nodes, are what fills the frame, so the
only lever that buys a bigger picture is dropping a kind.





An *include* list would be a caller restating the extractors' vocabulary, and would go
stale silently the first time a reader emitted a predicate nobody had added to it — the shape this
codebase has paid for repeatedly (DC-042, DC-022). Excluding means a new predicate appears in
every view by default, which is the safe direction: a caller sees something unexpected rather than
silently missing something.





Applied BEFORE the cap, like every other filter here. An excluded edge frees its bytes for
nodes rather than being trimmed after the ranking has already been paid for (DC-035).

## `WorkspaceGraph`

*record* — `GraphProjection.cs`

The whole graph, and what it left out.

**Remarks.** **Why totals need their own field when the node total does not.** The overall total is
recoverable as `Nodes.Count + Omitted`, so a surface can already say "1,500 of 2,992". A
per-category total cannot be recovered that way: the omitted nodes are gone, and with them any
way to know what they were.





**And it is not a detail.** MEASURED on a real workspace: 878 knowledge nodes, median
relation degree **0**, against a median of 4 for everything else — so under a
most-connected-first cap roughly 620 of them are never candidates for a slot. The surface
reported "Knowledge 257" with no denominator, which is true about what was drawn and reads as a
statement about what exists.





**By KIND, not by the surface's categories.** Code / Data / Infra / Specs / Knowledge is
the canvas's taxonomy, and Core teaching itself that taxonomy would put a UI decision in the
projection — where it would then be wrong for every other consumer. Kinds are what Core knows;
the surface already has a `categoryOf` and can run it over these totals to get its own
denominators. MEASURED: 29 kinds, ~636 bytes.





**The knowledge flag travels with each kind, rather than kind alone.** Measured on a
real workspace, no kind appears both as knowledge and as source — so kind alone WOULD work
today. That is a property of one corpus, not a guarantee, and a total that is silently wrong in
a repository where a kind is used both ways would be indistinguishable from a correct one.

## `GraphKindTotal`

*record* — `GraphProjection.cs`

How many nodes of one kind the workspace declares, drawn or not.

## `GraphProjection`

*class* — `GraphProjection.cs`

The whole workspace as a graph, rather than one node and its neighbours.

**Remarks.** **The graph surface had never shown the graph.** It asked for one node
(`FindAsync("", 1)`) and then that node's neighbours, so a workspace of 12,100 assertions and
2,164 nodes rendered as **two** — the alphabetically first symbol and its single neighbour.
Reported by the user, comparing it against the same repository in Obsidian.





**Attributes are not edges.** `has_type`, `declared_in`, `api_version` and
the rest describe a node; drawing them puts the string "class" in the graph as a thing that other
things point at. The same rule the search already applies, applied here — one definition, in
`Attributes`, used by both.





**Bounded by DEGREE, not by name.** When a cap applies, the nodes kept are the ones the
graph is actually about: an alphabetical cut of a two-thousand-node graph is arbitrary, and looks
exactly like a complete small graph. What was dropped is counted, so a partial view can say so.

| Member | Summary |
|---|---|
| `int DefaultMaxNodes = 5_000` | Nodes returned before the cap applies. |
| `WorkspaceGraph Compute(int maxNodes = DefaultMaxNodes)` | **(gap)** |
| `WorkspaceGraph Compute(GraphQuery query)` | **(gap)** |

### `int DefaultMaxNodes = 5_000`

Nodes returned before the cap applies.

**Remarks.** Large enough that no repository measured so far reaches it, so the common case is the WHOLE
graph. A cap exists because a surface that receives ten million nodes stops responding, and a
pane that stops responding tells the user nothing at all.

## `IWorkspaceQueries`

*interface* — `IWorkspaceQueries.cs`

The workspace's read surface, however it is reached.

**Remarks.** **This seam exists so the shell does not know whether the core is in this process.**
ADR-0009 keeps both hosting modes supported — in-process first, then a separate daemon — and a
UI written against one of them is a UI that has to be rewritten to get the other. The whole
difference between the two is which implementation is handed in.





**Asynchronous because the remote case is the real one.** A synchronous seam would force
every remote call to block a thread, and on a UI thread that is a frozen window for the length of
a pipe round trip. The in-process adapter completes immediately, which costs it nothing.





**The result types are the core's own.** A parallel set of "view" types would be a second
definition of every result to keep in step, and the first divergence would show up as a field
present one way and missing the other.

## `LocalWorkspaceQueries`

*class* — `IWorkspaceQueries.cs`

The read surface answered by a `ProjectionService` in this process.

**Remarks.** Completed tasks rather than `Task.Run`: the projections are synchronous and fast, and moving
them to a thread pool thread would add a context switch and a scheduling hop to hide latency that
is not there.

| Member | Summary |
|---|---|
| `Task<DescribeResult> DescribeAsync(` | **(gap)** |
| `Task<ImpactResult> ImpactAsync(` | **(gap)** |
| `Task<FindResult> FindAsync(` | **(gap)** |
| `Task<ContentSearchResult> SearchContentAsync(` | **(gap)** |
| `Task<InteractionResult> InteractionAsync(` | **(gap)** |
| `Task<KnowledgeResult> KnowledgeAsync(` | **(gap)** |
| `Task<NodeContent> NodeContentAsync(string nodeId, CancellationToken cancellationToken)` | **(gap)** |
| `Task<EvidencePage> EvidenceAsync(` | **(gap)** |
| `Task<WorkspaceGraph> GraphAsync(GraphQuery query, CancellationToken cancellationToken)` | **(gap)** |
| `Task<PathResult> PathsAsync(PathQuery query, CancellationToken cancellationToken)` | **(gap)** |
| `Task<WorkspaceOverview> OverviewAsync(OverviewQuery query, CancellationToken cancellationToken)` | **(gap)** |

## `InteractionMessage`

*record* — `Interaction.cs`

One message in an interaction: who called whom, and what they called.

## `InteractionResult`

*record* — `Interaction.cs`

One caller's outgoing calls in order — the data a UML sequence diagram draws.

## `JoinEdge`

*record* — `JoinProjection.cs`

One join between artifact types, with how it was established.

## `JoinResult`

*record* — `JoinProjection.cs`

The joins found, and what stopped more from being found.

## `JoinProjection`

*class* — `JoinProjection.cs`

Joins code, schema and infrastructure evidence.

**Remarks.** **Confidence is the deliverable, not the edge.** An inferred join across three artifacts
looks more impressive than a verified one inside a single file, and it is exactly the kind of
claim a user acts on without checking. So every edge carries how it was established, and a
convention-derived join is `Inferred` however obvious it looks.





**Joins are computed, never stored.** Two definitions of one quantity is a defect
signature: if a join were written back as a fact, the store would hold both the evidence and a
derived claim about it, and they would drift the first time an extractor changed. This reads the
same assertions every other projection reads.

| Member | Summary |
|---|---|
| `JoinResult Compute()` | **(gap)** |

## `NodeContentKind`

*enum* — `NodeContent.cs`

How a node's content should be rendered — the authority's call, not the reader's guess.

**Remarks.** ADR-0018 node-content-reader-contract. The reader branches on this rather than inspecting the content or the node id, so a
diagram, a proof or a binary comes back as `None` and gets the metadata-and-edges
fallback instead of being mis-rendered as text that happens not to be text.

## `NodeContent`

*record* — `NodeContent.cs`

One node's content, for a reader that has the node and wants what is behind it.

**Remarks.** **Bounded like every other response.** A large file returns the first N bytes and says so,
never an oversized frame — the same rule that INV-0003 established for the graph, applied to the
one artifact a reader asked for rather than to 1,500 it did not.





**Why the authority reads the file and the client does not.** The App reading files itself
would make two authorities on what a node's content is, and they would disagree the moment one
resolved a path differently (DC-022). It would also put file access on the wrong side of the trust
boundary: the daemon confines every read to the workspace root, and a client doing its own reading
answers to nothing.

## `ProjectionErrorCodes`

*class* — `ProjectionService.cs`

*No doc comment on this type.* **(gap)**

| Member | Summary |
|---|---|
| `string LimitExceeded = "AIDE-MCP-LIMIT-EXCEEDED"` | **(gap)** |
| `string NodeUnknown = "AIDE-PROJECTION-NODE-UNKNOWN"` | **(gap)** |

## `ResultBounds`

*record* — `ProjectionService.cs`

What a bounded result actually returned, and what it left out. Every projection carries this:
a truncated result that does not publish its omission is indistinguishable from a complete one,
which is how a "bounded" tool silently becomes an unbounded context assembler.

## `EdgeView`

*record* — `ProjectionService.cs`

*No doc comment on this type.* **(gap)**

## `NodeView`

*record* — `ProjectionService.cs`

*No doc comment on this type.* **(gap)**

## `DescribeResult`

*record* — `ProjectionService.cs`

*No doc comment on this type.* **(gap)**

**Remarks.** **INV-0004.** The canvas hardcoded `"source"` as every neighbour's kind because the
describe result did not carry one — so a drill-down showed a table, a bicep resource and a class
as the same thing, and the filter could not tell them apart. The kind is a property of the
NEIGHBOUR, and only the projection can read it; a renderer inventing a default is a renderer
stating a fact it does not have.





**`KnowledgeIds` closes the same shape one field over.** The view model carried a
written-down gap — `IsKnowledge` defaulted to `false` on every drill-down neighbour
because a kind is not a node class, so the view "genuinely cannot tell knowledge from source".
`false` under-counted rather than mislabelled, which is the safer direction and still a
renderer answering a question it had no data for. It matters more now that the graph reports
`NotInView`: the default view is a map of the code, and a knowledge node the budget could
never draw is reached by drill-down instead — so the drill-down has to know what it is
holding.

## `ImpactResult`

*record* — `ProjectionService.cs`

*No doc comment on this type.* **(gap)**

## `FindMatch`

*record* — `ProjectionService.cs`

One search hit, and why it is one.

**Remarks.** The two fields are ADDED rather than replacing anything, and both are optional to read. A client
that ignores them behaves exactly as before — which is what makes this a widening of the contract
and not a break of it.

## `FindResult`

*record* — `ProjectionService.cs`

*No doc comment on this type.* **(gap)**

## `EvidencePage`

*record* — `ProjectionService.cs`

One page of current evidence, and where to continue.

## `AuthorshipOrigin`

*enum* — `ProjectionService.cs`

Who authored a record. Carried on every read result so a consuming agent can tell a repository
fact from something another agent wrote — without it, an agent-authored note is laundered back
out as workspace knowledge.

## `KnowledgeQuery`

*record* — `ProjectionService.cs`

*No doc comment on this type.* **(gap)**

## `KnowledgeNodeView`

*record* — `ProjectionService.cs`

*No doc comment on this type.* **(gap)**

## `KnowledgeResult`

*record* — `ProjectionService.cs`

*No doc comment on this type.* **(gap)**

## `ProjectionService`

*class* — `ProjectionService.cs`

Bounded, self-describing projections over the current fact set.

**Remarks.** Pattern: CQRS / Materialized Read Model. Every result is rebuildable from facts and every one is
capped on nodes, edges AND bytes — the byte cap matters because node labels come from repository
content, so a count-only cap still admits an unbounded payload.

| Member | Summary |
|---|---|
| `int MaxNeighborsCeiling = 50` | **(gap)** |
| `int MaxMembersRead = 80` | **(gap)** |
| `int MaxEdgesCeiling = 500` | **(gap)** |
| `int MaxNodesCeiling = 200` | **(gap)** |
| `int MaxResultBytes = 64 * 1024` | **(gap)** |
| `int MaxResponseBytes = 896 * 1024` | The most a single response may serialise to. |
| `int FrameBytes = Ipc.IpcFraming.MaxFrameBytes` | The transport's own limit, restated here only so the budget can be checked against it. |
| `int MaxFramedGraphBytes = FrameBytes - (64 * 1024)` | What a shrunk graph must fit inside — the frame, less real headroom. |
| `int AssertionOverheadBytes = 448` | What one assertion costs in JSON beyond its own text. |
| `int MaxShrinkAttempts = 12` | How many times a graph may be shrunk before it must already fit. |
| `int MaxRecoveryProbes = 4` | How many times a shrunk graph may probe upward for the size it overshot. |
| `int MinRecoveryGap = 50` | Below this, the nodes still recoverable are not worth a recompute to find. |
| `int MaxSearchResultsCeiling = 20_000` | The ceiling on a SEARCH, which is a different question from a neighbour list. |
| `int MaxClustersCeiling = 500` | Routes returned before the answer is truncated.  Groups returned before the rest are counted and dropped. |
| `int MaxPathsCeiling = 100` | **(gap)** |
| `int MaxPathLengthCeiling = 12` | The longest route worth returning, in edges. |
| `int MaxEvidencePageCeiling = 2_000` | Assertions per evidence page. |
| `DescribeResult Describe(string nodeId, int maxNeighbors)` | **(gap)** |
| `ImpactResult Impact(string nodeId, int maxNodes, int maxEdges)` | Bounded dependent-neighbourhood walk. Breadth-first with an explicit frontier cap, so the traversal cannot fan out into the whole graph — the caller always learns what was omitted. |
| `EvidencePage Evidence(string? cursor, int maxAssertions)` | One page of every current assertion, for a caller that wants the whole set. |
| `WorkspaceGraph Graph(int maxNodes)` | The whole workspace as a graph. |
| `WorkspaceGraph Graph(GraphQuery query)` | The graph the query asks for — filtered before the cap applies. |
| `WorkspaceOverview Overview(OverviewQuery query)` | The workspace at a distance: groups rather than nodes, for a graph too large to draw. |
| `PathResult Paths(PathQuery query)` | How one node reaches another, within the graph the query names. |
| `FindResult Find(string term, int maxResults)` | **(gap)** |
| `KnowledgeResult Knowledge(KnowledgeQuery query)` | US-4: knowledge navigation. Same facts, filtered to knowledge-kind subjects, with backlinks and the health findings the spec requires when source/owner/links are missing. |
| `InteractionResult Interaction(string nodeId, int maxMessages)` | The content behind one node, for a reader that already has the node.  Lines in the workspace's own files that contain a term.  One caller's outgoing calls, in call order — the feed for a UML sequence diagram. |
| `int MaxInteractionMessages = 200` | The most messages one interaction will return. |
| `ContentSearchResult SearchContent(string term, int maxMatches)` | **(gap)** |
| `int MaxContentFiles = 600` | The most files one content search will open. |
| `int MaxContentMatches = 200` | The most matches one content search will return. |
| `NodeContent NodeContent(string nodeId)` | **(gap)** |
| `int MaxContentBytes = 256 * 1024` | How much of one artifact travels. Well inside the frame, with the rest named. |
| `IReadOnlyList<(string Subject, string Predicate, string Object, string Status, int Count, string Revision)>` | Rebuilds the labelled claim cache from facts. Public because the equality test needs to prove the stored cache equals this derivation — a cache with no such test is a second source of truth. |

### `int MaxResponseBytes = 896 * 1024`

The most a single response may serialise to.

**Remarks.** **Derived from the transport, with headroom.** One IPC frame carries 1,048,576 bytes
(`IpcFraming.MaxFrameBytes`); this leaves a quarter of it spare for the response envelope
and for the difference between an estimate and the truth. A projection that fills the frame
exactly is one repository away from INV-0003.





**Why a byte budget and not a bigger count ceiling.** Every ceiling in this class
counts ITEMS and the transport limit is in BYTES, and node labels, subjects and paths all come
from repository content — so a count-only cap admits an unbounded payload. That is not a
hypothetical: MEASURED on a real repository, an evidence page of 2,000 assertions serialises
to **1,004,397 bytes**, which is 95.8% of the frame and fifteen times the
`MaxResultBytes` its own documentation claimed it stayed "comfortably inside".





**Why the headroom is the size it is.** A response is its payload plus an envelope,
and from IPC version 3 that is all it is: the payload is carried as JSON rather than as a
string holding JSON text, so nothing is escaped twice. Through version 2 it was, at a measured
**1.56–1.57x** — which is how a 727,244-byte graph inside a 768 KiB budget reached
1,137,104 bytes on the wire and was refused (DC-047). The budget had been checked on the inner
bytes and enforced on the outer ones, and its tests counted the inner bytes too, so the guard
and its proof were wrong together.





128 KiB below the frame, which is the envelope with room to spare.
`ThePayloadIsNotEncodedTwice` holds up the premise this rests on — that payload and frame
are within a few percent — and `TheBudgetCannotDriftPastWhatAFrameHolds` holds up the
arithmetic. Neither is optional: this number is only safe while both pass.

### `int MaxFramedGraphBytes = FrameBytes - (64 * 1024)`

What a shrunk graph must fit inside — the frame, less real headroom.

**Remarks.** Shrinking stops at the FIRST size that fits, so a target equal to the frame leaves whatever
margin the last step happened to produce. MEASURED with no headroom: 1,044,916 bytes against
a 1,048,576 frame — 3,660 bytes, which is one longer type name away from failing. A limit met
exactly is not a limit respected.

### `int AssertionOverheadBytes = 448`

What one assertion costs in JSON beyond its own text.

**Remarks.** MEASURED, not estimated: a 2,000-assertion page whose subjects, predicates, objects and paths
total 238,002 bytes serialises to 1,004,397 — **383 bytes per row** of field names,
timestamps, enum spellings and punctuation. Rounded up for headroom, because a guard that
under-counts is a guard that lets the frame overflow.

### `int MaxShrinkAttempts = 12`

How many times a graph may be shrunk before it must already fit.

**Remarks.** Each round takes at least a third off, so twelve rounds reduce five thousand nodes to fewer
than five — far past any real graph. It is a cost bound, not the thing that makes the loop
terminate; the guaranteed reduction does that.

### `int MaxRecoveryProbes = 4`

How many times a shrunk graph may probe upward for the size it overshot.

**Remarks.** Each probe halves the remaining gap. MEASURED on the calibrated fixture: none returns 868
nodes where 1,281 fit, two returns 1,193, four returns 1,274, and six returns 1,274 again —
by then `MinRecoveryGap` stops it. Four is where the curve flattens, and every
probe is a full recompute of the graph, which is the expensive half.

### `int MinRecoveryGap = 50`

Below this, the nodes still recoverable are not worth a recompute to find.

**Remarks.** It is also the precision of the monotonicity this class offers: a larger request can return
up to this many fewer nodes than a smaller one, because recovery approximates the largest
fitting size rather than finding it. Exact monotonicity needs the node ORDERING computed once
and candidate sizes evaluated against it — the ordering is identical for every size, so today
each probe redoes work that does not change. That is the upgrade, and it is not free.

### `int MaxSearchResultsCeiling = 20_000`

The ceiling on a SEARCH, which is a different question from a neighbour list.

**Remarks.** **Find used to borrow `MaxNeighborsCeiling`, and 50 is the wrong number
for it by two orders of magnitude.** The workbench asks for 20,000 matches to build the
context and join panes; it received 50. Those panes were computing crossing counts, join
counts and coverage from roughly three percent of a real workspace, and presenting the result
as the answer — while a spike reading the store directly showed the whole picture and
disagreed with the product for days.





A search returns identity columns only — id, kind, label — so the payload per row is
small and bounded, which is why this ceiling can be large where the neighbour one cannot.
`MaxResultBytes` still applies underneath.

### `int MaxClustersCeiling = 500`

Routes returned before the answer is truncated.

Groups returned before the rest are counted and dropped.

**Remarks.** A reader comparing routes is choosing between them, and nobody chooses between two hundred.
The cap is small on purpose and the truncation is reported.

An overview a person can read has tens of groups, not thousands; past a few hundred it is a
hairball again at a coarser grain, which is the failure it exists to prevent.

### `int MaxPathLengthCeiling = 12`

The longest route worth returning, in edges.

**Remarks.** Beyond about a dozen hops "A reaches B" stops being a fact about the design and becomes a
fact about the graph being connected — in a codebase almost everything reaches almost
everything if you allow enough steps.

### `int MaxEvidencePageCeiling = 2_000`

Assertions per evidence page.

**Remarks.** **A COUNT ceiling, and it does not bound the payload.** This used to say the page was
"sized so it stays comfortably inside `MaxResultBytes` once serialised". MEASURED:
2,000 assertions serialise to 1,004,397 bytes, which is fifteen times that constant and 95.8%
of an IPC frame. The sentence was written, believed, and never checked.





What actually bounds the page is `MaxResponseBytes`, applied row by row in
`Evidence`; this count is the coarser of the two limits and usually is not the one
that fires. An assertion carries its provenance, so it is far heavier per row than a search
match — which is the reason a count could never have been the bound.

### `EvidencePage Evidence(string? cursor, int maxAssertions)`

One page of every current assertion, for a caller that wants the whole set.

**Remarks.** The panes want all of it and were rebuilding it node by node through
`Describe`, which bounds neighbours at 50 and dropped two join edges of 124 doing
so. This asks the question they were actually asking.





**Bounded by BYTES as well as by count, and the byte bound is the one that matters.**
This method's documentation used to claim a page "can cross a pipe without breaching the
result-byte cap". It could not: MEASURED on a real repository, a 2,000-assertion page is
**1,004,397 bytes** against a 1,048,576-byte frame — 95.8% full, and over the frame
entirely on a repository with slightly longer type names. The claim was written, believed and
never checked, which is the same shape as INV-0003 one method along.





Truncating a page early is LOSSLESS here, and that is why the fix belongs at this level:
the cursor continues from the last row actually returned, so a byte-bounded page costs one
extra round trip and never drops a row.

### `WorkspaceGraph Graph(int maxNodes)`

The whole workspace as a graph.

**Remarks.** The question the graph surface was never asking. It requested one node and that node's
neighbours, so a workspace of 12,100 assertions rendered as two nodes — reported against the
same repository viewed in Obsidian.

### `WorkspaceOverview Overview(OverviewQuery query)`

The workspace at a distance: groups rather than nodes, for a graph too large to draw.

**Remarks.** Built over the same `Graph` projection the canvas uses, so an overview can never
summarise a node the detailed view would not show. Two answers to one question is the defect
signature this codebase has already paid for.

### `PathResult Paths(PathQuery query)`

How one node reaches another, within the graph the query names.

**Remarks.** Built over the same projection the graph surface uses, so a route can never contain an edge
the picture does not show — two answers to one question is the defect signature this
codebase has already paid for once.

### `InteractionResult Interaction(string nodeId, int maxMessages)`

The content behind one node, for a reader that already has the node.

Lines in the workspace's own files that contain a term.

One caller's outgoing calls, in call order — the feed for a UML sequence diagram.

**Remarks.** **On demand, for the one node asked for.** The graph carries no content by design —
fattening 1,500 nodes to serve the one a user selected is what overflowed the frame in the
first place (INV-0003, ADR-0018 node-content-reader-contract).





**Confined to the workspace.** The path is rebuilt from the scope's recorded location
plus the assertion's own provenance, then checked to be under the root before anything is
opened. A node id arrives from a client, and a client asking is not a reason to read a file.





**Bounded, and honest when it truncates.** Oversized content returns its first bytes
and a shortfall saying what was left — never an oversized frame, never a silent half-file.

**Why this is Core's and not the client's.** The App must not read workspace files:
two authorities on what a file contains disagree the first time one resolves a path
differently (DC-022), and file access belongs on the side of the boundary that can confine it
to the workspace. This is the same rule that put `NodeContent` here, applied to the
corpus instead of to one node.





**It searches files the STORE knows about, not the directory tree.** Walking the
tree would read `node_modules`, `bin`, and every generated bundle the extractors
already decided not to index — and would return hits in files the graph cannot navigate to,
which is a result a person cannot act on. Every hit names the node that owns the file.





**Every bound is enforced, not declared.** Files, matches, bytes and per-file size
all cap, and the result says when a cap fired. A limit that cannot fire is the defect it was
written to prevent (DC-016), and a budget that is reported but not applied is how a 1.18 MB
payload crossed a 1 MiB frame (INV-0003).

**Why this is not `calls`.** The `calls` edges are deduplicated to one row
per `(caller, callee)` pair, which is correct for a graph and destroys an interaction:
`A→B, A→C, A→B` collapses to two messages and the repeat is gone. `calls_at` keeps
every site. MEASURED on TheTerrace: 2,024 sites against 1,492 deduplicated edges, so keeping
them costs about a third more rows on one predicate and nothing at all on the graph payload —
`calls_at` is an attribute and is never drawn.





**Type-level, and it says so.** The caller and callee are types; the member is the
message name. A sequence diagram of one METHOD's activation needs method-level callers, which
the C# reader does not emit — so this draws "what this type calls, in order", which is a real
interaction and not the one a lifeline-per-method diagram would show. Better to hand over a
true smaller thing than a plausible larger one.

### `int MaxInteractionMessages = 200`

The most messages one interaction will return.

**Remarks.** A lifeline with 500 messages on it is not a diagram anybody reads. MEASURED on TheTerrace,
the busiest single caller has 46 outgoing call sites, so this bounds the pathological case
without truncating any real one.

### `int MaxContentFiles = 600`

The most files one content search will open.

**Remarks.** A person is waiting on this. TheTerrace has 1,178 indexed artifacts; reading all of them on
every keystroke is not a search box, it is a build step.
