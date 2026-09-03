---
id: api-aide-core-presentation
title: "API: AiDe.Core.Presentation"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Presentation: 32 types, 81 members, 64% carrying a summary doc comment.
---

# API: `AiDe.Core.Presentation`

**32 public types · 81 public members · 64% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `CanvasNode`

*record* — `CanvasGraphViewModel.cs`

One node as the canvas draws it.

One node as the canvas draws it.

**Remarks.** **The count is on the node because a dot standing for 240 types is only honest while the 240 is
on it.** Without it the overview renders a group exactly like a single type, and the picture
says the workspace is small rather than that the view is summarised. Defaulted to 1, so every
existing construction means what it always meant.

## `CanvasEdge`

*record* — `CanvasGraphViewModel.cs`

One edge as the canvas draws it.

| Member | Summary |
|---|---|
| `bool IsJoin` | True for a join across artifact types — code to schema, schema to infrastructure. |
| `bool IsInferred` | True when the claim is a convention rather than a declaration. |

### `bool IsJoin`

True for a join across artifact types — code to schema, schema to infrastructure.

**Remarks.** Drawn differently because it is a different KIND of claim. An edge inside one artifact was
resolved by a compiler; a join between two was resolved by a convention or a literal match,
and it looks more authoritative than it is precisely because it spans more.

## `CanvasGraph`

*record* — `CanvasGraphViewModel.cs`

What the canvas renders, including what it could not show.

## `CanvasGraphViewModel`

*class* — `CanvasGraphViewModel.cs`

Builds the canvas's view from the same read surface every other pane uses.

**Remarks.** **In Core, with no WPF and no browser.** The canvas is a rendering of a projection, and
what it shows is decidable — and testable — without a window. Putting this in the WPF layer would
make "does the graph show the right nodes" a question only answerable by looking at one.





**Every empty case gets its own message.** "No workspace", "nothing indexed yet" and "a
node with no neighbours" are three different situations with three different next actions, and a
blank canvas for all three tells the user nothing (**DC-011**).

| Member | Summary |
|---|---|
| `int OverviewNodeCap = 1_500` | The graph around , or around whatever Find offers first.  How many nodes the canvas asks for when nothing is focused.  Nodes in the default overview, before it says what it left out. |
| `Task<CanvasGraph> LoadAsync(` | **(gap)** |
| `Task<CanvasGraph> OverviewAsync(int depth = 3, CancellationToken cancellationToken = default)` | The workspace as GROUPS rather than nodes — the source for a semantic-zoom render. |
| `Task<CanvasGraph> GroupAsync(string groupId, CancellationToken cancellationToken = default)` | The member nodes of one overview group — the drill-down from a cluster (semantic zoom) to the real nodes it stands for. |
| `Task<CanvasGraph> RouteAsync(` | The declared context a node belongs to, or null.  How one node reaches another, rendered as the same graph the canvas already draws. |
| `Func<string, string?> ContextLookup { get; set; } = _` | **(gap)** |
| `string? ContextFilter { get; set; }` | When set, only nodes in this context are drawn. |

### `int OverviewNodeCap = 1_500`

The graph around , or around whatever Find offers first.

How many nodes the canvas asks for when nothing is focused.

Nodes in the default overview, before it says what it left out.

**Remarks.** The projection's own ceiling, so the canvas asks for everything the read surface will give.
A lower number here omitted 813 of TheTerrace's 2,813 nodes — a limit the SURFACE imposed on
itself while the store and the projection were both willing. How much of it to draw at once
is a rendering decision and belongs with the renderer; withholding it here would make that
decision on the renderer's behalf and hide the rest.

**Derived from a measurement, and from the spec.** MEASURED on a real repository: the
whole graph serialises to **1,522,284 bytes** against a 1 MiB frame — so the previous
default could not be delivered at all (INV-0003), and the user saw "the daemon closed the
connection". Declared-only nodes are ~294 bytes each including their edges, so a frame holds
roughly 3,500 of them; this cap keeps real headroom under that.





**But the size is the smaller reason.** `docs/specs/knowledge-exploration.md`
US-K2 says the whole graph is never rendered at once, and a 2,815-node hairball is unreadable
even when it fits. The fix for "one arbitrary alphabetical node" was a bounded overview of
MEANINGFUL nodes; loading everything over-corrected past it. What is dropped is counted and
reported, which is the part that makes a bounded view honest rather than a smaller lie.

### `Task<CanvasGraph> OverviewAsync(int depth = 3, CancellationToken cancellationToken = default)`

The workspace as GROUPS rather than nodes — the source for a semantic-zoom render.

- **`depth`** — How many identifier segments to group by. Higher is finer.

**Remarks.** **Why this exists beside the node graph.** The bounded default draws 1,500 of a
workspace's declared nodes and says what it dropped, which is honest and is still a hairball.
A group view answers the question the first look actually asks — *what is in here* — and
the node view answers the second.





**Each group is a node carrying its size.** The canvas already knows how to draw
nodes and edges, so an overview arrives as the same shape with `Count`
set — rather than as a second payload with a second renderer that would then disagree with
this one about what a node is.





**Grouping is the projection's, not this method's.** `GraphOverview.GroupFor` is
public precisely so a caller that drills into a group computes the same membership the
overview did; two definitions of "which group is this node in" is the defect signature this
codebase has paid for repeatedly (DC-022).

### `Task<CanvasGraph> GroupAsync(string groupId, CancellationToken cancellationToken = default)`

The member nodes of one overview group — the drill-down from a cluster (semantic zoom) to the
real nodes it stands for.

**Remarks.** **Membership is the projection's, via `GraphQuery.GroupId`.** The group whose
super-node the canvas drew carried its id; asked to open it, this hands that id straight back to
`GraphAsync`, which keeps only the nodes `GraphOverview.GroupFor`
assigns to it. So "which nodes are in this group" has one definition, shared by the overview that
drew the group and the drill-down that opens it (the DC-022 trap this avoids).





**RootId is the group id.** The result is a rooted view, so the canvas keeps Back and
Overview live — a group opened with no way back is the keyboard trap the canvas exists to avoid.

### `Task<CanvasGraph> RouteAsync(`

The declared context a node belongs to, or null.

How one node reaches another, rendered as the same graph the canvas already draws.

**Remarks.** Null is a real answer and is drawn as such: a node in no context is uncovered, not
unimportant, and colouring it as though it belonged somewhere would be the inference
ADR-0016 refuses.

**Deliberately returns `CanvasGraph` rather than a route type.** A route
IS a subgraph, and giving it its own shape would mean a second renderer, a second set of
bindings and a second place for the two sessions to disagree about what a node looks like.
The design session binds what it already binds; only the caption changes.





**The caption carries the weakest link.** A route drawn without it looks like a fact
about the code, when one inferred edge anywhere along it makes the whole claim inferred.





**Every empty case says which one it is.** "No workspace", "that node is not in the
graph" and "there is no route within eight edges" are three different situations with three
different next actions (DC-011).

### `string? ContextFilter { get; set; }`

When set, only nodes in this context are drawn.

**Remarks.** The ROOT is kept even when it is outside the filter, and labelled as such. Dropping it would
leave a graph with no anchor and no way back — a filter that can strip the thing you were
looking at is one nobody trusts twice.

## `PaneState`

*enum* — `EvidencePaneViewModel.cs`

The complete state set for the evidence surface. Loading, Empty and Error are first-class here
because they are the states the urge to complete skips — and an unavailable result rendered as a
clean empty one is the specific dishonesty this product exists to avoid.

## `ConfidenceBadge`

*record* — `EvidencePaneViewModel.cs`

A confidence badge that never relies on colour. Glyph and text carry the meaning; colour is the
third signal, so the badge still reads correctly in high-contrast mode and for a colour-blind
operator (WCAG 2.2 AA, "not colour alone").

| Member | Summary |
|---|---|
| `ConfidenceBadge For(VerificationStatus status)` | **(gap)** |
| `string AccessibleName` | What a screen reader announces. Never just the colour name. |

## `EvidenceRow`

*record* — `EvidencePaneViewModel.cs`

One row of the accessible evidence list — the permanent keyboard/screen-reader surface.

**Remarks.** The pane lists search results, and a search now matches attribute VALUES as well as identity. A
row that came back because one of its members matched is **correct** and reads as a wrong
result until it says so — the same defect fixed on the search surface, found here by enumerating
client records rather than by anybody noticing the pane.

| Member | Summary |
|---|---|
| `string ListLine` | The one line a list shows: the label, its kind, and why it matched. |
| `string AccessibleName` | **(gap)** |

### `string ListLine`

The one line a list shows: the label, its kind, and why it matched.

**Remarks.** Composed HERE rather than in the pane, so the visible text and the accessible name are built
from the same fields and cannot drift apart — the chip's two rendering paths drifting is what
made the same false claim twice in one surface.

## `ProvenanceSection`

*record* — `EvidencePaneViewModel.cs`

One section of the provenance pane, in the spec's fixed evidence order.

## `EvidencePaneViewModel`

*class* — `EvidencePaneViewModel.cs`

The Phase-1 evidence surface: a filterable list plus a provenance pane.

**Remarks.** This is not a fallback for a graph canvas — it is the accessibility equivalent the spec requires
to exist permanently, exposing the same selected-node identity, provenance, navigation actions and
result-limit state as the Phase-2 canvas will.

| Member | Summary |
|---|---|
| `PaneState State { get; private set; } = PaneState.Loading` | **(gap)** |
| `IReadOnlyList<EvidenceRow> Rows { get; private set; } = []` | **(gap)** |
| `string? SelectedNodeId { get; private set; }` | **(gap)** |
| `IReadOnlyList<ProvenanceSection> Provenance { get; private set; } = []` | **(gap)** |
| `string StatusMessage { get; private set; } = "Loading evidence…"` | The one string the operator reads when something is off. Always states evidence, never reassurance. |
| `string? SourceRevision { get; private set; }` | **(gap)** |
| `string LiveAnnouncement { get; private set; } = string.Empty` | Announced through a live region, so state changes reach a screen reader without motion. |
| `Task LoadAsync(string searchTerm = "", CancellationToken cancellationToken = default)` | **(gap)** |
| `void MarkStale(string reason)` | Marks the view stale without discarding it — the last successful revision still renders. |
| `void Filter(string term)` | **(gap)** |
| `Task SelectAsync(string nodeId, CancellationToken cancellationToken = default)` | Selects a node and builds its provenance in the spec's fixed order: what it is → confidence/provenance → related nodes → source location → actions. |
| `string EmptySelectionMessage` | Empty-pane copy, shown before anything is selected. |

## `WatcherBoardRow`

*record* — `WatcherBoardPaneViewModel.cs`

One row of the Message Board surface (US-4): a per-repository post rendered as a dense, scannable,
screen-reader-complete line. `Content` is **quarantined untrusted data** -
it is shown to the operator but never treated as instruction; an injection-shaped post is marked
with a visible flag (US-4 #5), a redacted post shows a tombstone, and neither is silently blank.

| Member | Summary |
|---|---|
| `string RedactedText = "[redacted]"` | The literal shown for a post whose content was redacted (spec line 210). |
| `string FlagPrefix = "⚠ flagged · "` | The prefix a flagged post carries so it reads as untrusted, not as a directive (US-4 #5). |
| `string DisplayLabel` | The dense one-line label (G6 density). Untrusted content is prefixed, never blank. |
| `string AccessibleName` | The full row a screen reader announces (WCAG 2.2 AA). |
| `WatcherBoardRow From(BoardMessage message)` | Builds an honest row: a tombstone shows [redacted], never the (now null) content. |

## `IWatcherBoardQuery`

*interface* — `WatcherBoardPaneViewModel.cs`

The read seam the Board pane consumes. A null query means no watcher store is wired.

## `WatcherBoardQuery`

*class* — `WatcherBoardPaneViewModel.cs`

Folds the observation store's cross-repo board into the pane's read (US-4). Order preserved.

| Member | Summary |
|---|---|
| `IReadOnlyList<BoardMessage> GetMessages()` | **(gap)** |

## `WatcherBoardPaneViewModel`

*class* — `WatcherBoardPaneViewModel.cs`

The Loomkeeper Message Board surface view model - the compute reader for shared agent communication
(US-4). Renders posts across repositories with quarantined untrusted content shown but never as
instruction, injection flags visible, and redactions as tombstones. Synchronous load (a local store
fold), so it degrades to an explicit state and never strands on "Loading…" (DC-011).

| Member | Summary |
|---|---|
| `PaneState State { get; private set; } = PaneState.Loading` | **(gap)** |
| `IReadOnlyList<WatcherBoardRow> Rows { get; private set; } = []` | **(gap)** |
| `string StatusMessage { get; private set; } = "Loading board…"` | **(gap)** |
| `string LiveAnnouncement { get; private set; } = string.Empty` | **(gap)** |
| `void Load()` | **(gap)** |

## `WatcherDaydreamRow`

*record* — `WatcherDaydreamPaneViewModel.cs`

One row of the Daydreams surface (US-9): a pattern, where it stands, and what is stopping it.

**Remarks.** **The block reason is part of the row, not a tooltip.** A candidate that cannot be
promoted has to say *which* prerequisite is missing where it is read. "Promotion disabled"
with the reason a click away is the empty state DC-087 registered — a surface stating a
condition it never explains.





**No content from an agent appears here.** A signature is built from typed values only,
so unlike the Message Board there is no quarantined prose to render and no injection flag to
show. The rows are describable entirely from the store's own vocabulary.

| Member | Summary |
|---|---|
| `string DisplayLabel` | The dense one-line label (G6 density). |
| `string AccessibleName` | The full row a screen reader announces (WCAG 2.2 AA). |
| `WatcherDaydreamRow From(DaydreamCandidate candidate)` | Builds a row, naming the pattern from its typed parts rather than any prose. |
| `string StageOf(DaydreamState state)` | The three stages the spec's Daydreams tab shows: Observations, Candidates, Promoted. |

### `string StageOf(DaydreamState state)`

The three stages the spec's Daydreams tab shows: Observations, Candidates, Promoted.

**Remarks.** Disconfirmed, Deferred and Rejected stay under **Candidates** rather than being hidden or
given a fourth stage. A refuted candidate is the most informative thing on this surface —
it is the system having done the disconfirming work and reported the answer nobody wanted —
and moving it out of sight would leave a reader looking at only the proposals that survived.

## `IWatcherDaydreamQuery`

*interface* — `WatcherDaydreamPaneViewModel.cs`

The read seam the Daydreams pane consumes. A null query means no watcher store is wired.

## `WatcherDaydreamQuery`

*class* — `WatcherDaydreamPaneViewModel.cs`

Folds the repository's Daydream record into the pane's read (US-9).

**Remarks.** The fold runs here rather than in the view model, so the pane renders a decision it did not
make. Every state — including whether promotion is possible — comes from
`DaydreamFold`, which is where the acceptance criteria are tested.





**The repository is the record, not the store.** This read used to fold
`IWatcherObservationStore`'s `daydream_*_fact` tables. Those tables still exist —
deleting a shipped migration is worse than leaving one unused — but they are no longer
authoritative, and nothing reads them. Two definitions of one quantity is a defect signature
(DM7), so there is deliberately no parallel copy to fall back to.
(`design-watcher-daydream-dream-seam` §4a.)

| Member | Summary |
|---|---|
| `string? Unavailable` | **(gap)** |
| `int UnreadableLines` | **(gap)** |
| `string? ReachFinding` | The probe's finding, or `null` when no probe is wired. |
| `IReadOnlyList<DaydreamCandidate> GetCandidates()` | **(gap)** |

### `string? ReachFinding`

The probe's finding, or `null` when no probe is wired.

**Remarks.** Optional so a host with no scored episodes to compare against gets no finding rather than a
fabricated one — an absent probe must never render as "nothing to report", which is the
distinction the probe exists to make in the first place.

## `WatcherDaydreamPaneViewModel`

*class* — `WatcherDaydreamPaneViewModel.cs`

The Loomkeeper Daydreams surface view model (US-9) — three stages, each with an honest empty
state, and promotion visible only where it is actually possible.

**Remarks.** Synchronous load (a local store fold), so it degrades to an explicit state and never strands on
"Loading…" (DC-011).

| Member | Summary |
|---|---|
| `IReadOnlyList<string> Stages { get; } = ["Observations", "Candidates", "Promoted"]` | The stages in reading order, so an empty one is still shown and named. |
| `PaneState State { get; private set; } = PaneState.Loading` | **(gap)** |
| `IReadOnlyList<WatcherDaydreamRow> Rows { get; private set; } = []` | **(gap)** |
| `string StatusMessage { get; private set; } = "Loading daydreams…"` | **(gap)** |
| `string LiveAnnouncement { get; private set; } = string.Empty` | **(gap)** |
| `IReadOnlyList<WatcherDaydreamRow> RowsFor(string stage)` | Rows for one stage, in reading order. An empty stage returns an empty list. |
| `string EmptyStateFor(string stage)` | What to show under a stage with nothing in it. |
| `void Load()` | **(gap)** |

### `string EmptyStateFor(string stage)`

What to show under a stage with nothing in it.

**Remarks.** Each names only what it has looked at. "Nothing to show" is complete; "nothing to show
because X" is a claim, and a surface that has not checked X is not entitled to make it
(DC-087). None of these mentions the extractor, the scorer, or any subsystem this pane does
not read.

## `WatcherLeaderboardRow`

*record* — `WatcherLeaderboardPaneViewModel.cs`

One row of the Leaderboard surface (US-14): a facet cell within one (task class, score schema)
segment. A cell below the cohort minimum or one that resolves to a single operator renders
**Not Comparable** with its reason, never a rank (US-14/US-10 - a single operator is not
de-anonymised off a public board). Comparable cells carry rank, cohort and median Weave. There is
deliberately no single optimisable scalar the operator can chase (US-16).

| Member | Summary |
|---|---|
| `string NotComparableText = "Not Comparable"` | The literal a non-comparable cell shows in place of a rank (US-10/US-14). |
| `string RankText` | **(gap)** |
| `string MedianText` | **(gap)** |
| `string DisplayLabel` | The dense one-line label (G6 density). |
| `string AccessibleName` | The full row a screen reader announces (WCAG 2.2 AA). |
| `WatcherLeaderboardRow From(Leaderboard board, LeaderboardCell cell)` | **(gap)** |

## `IWatcherLeaderboardQuery`

*interface* — `WatcherLeaderboardPaneViewModel.cs`

The read seam the Leaderboard pane consumes. A null query means no watcher store is wired.

## `WatcherLeaderboardQuery`

*class* — `WatcherLeaderboardPaneViewModel.cs`

Folds the store's materialised scored episodes into the leaderboard's read (US-14).

| Member | Summary |
|---|---|
| `IReadOnlyList<ScoredEpisode> GetScoredEpisodes()` | **(gap)** |

## `IWatcherDisputeQuery`

*interface* — `WatcherLeaderboardPaneViewModel.cs`

The read seam for the derived Disputed state (US-16). A null query means disputes are not shown.

## `WatcherDisputeQuery`

*class* — `WatcherLeaderboardPaneViewModel.cs`

Folds the store's append-only disputes into the disputed-episode set (US-16 / rule 12).

| Member | Summary |
|---|---|
| `IReadOnlySet<string> DisputedEpisodeIds()` | **(gap)** |

## `WatcherLeaderboardPaneViewModel`

*class* — `WatcherLeaderboardPaneViewModel.cs`

The Loomkeeper Leaderboard surface view model - the comparative view of agent effectiveness
(US-14). It discovers the distinct (task class, score schema) segments present in the scored
episodes and composes one leaderboard per segment through `LeaderboardComposer` (never
comparing across segments - rule 11), flattening the cells into honest rows: a rank where
comparable, "Not Comparable" with a reason where the cohort is too small or single-operator
(US-10). Synchronous load; degrades to an explicit state (DC-011).

| Member | Summary |
|---|---|
| `PaneState State { get; private set; } = PaneState.Loading` | **(gap)** |
| `IReadOnlyList<WatcherLeaderboardRow> Rows { get; private set; } = []` | **(gap)** |
| `string StatusMessage { get; private set; } = "Loading leaderboard…"` | **(gap)** |
| `string LiveAnnouncement { get; private set; } = string.Empty` | **(gap)** |
| `void Load()` | **(gap)** |

## `LivenessBadge`

*record* — `WatcherSessionsPaneViewModel.cs`

A liveness badge that never relies on colour. Glyph and text carry the meaning; colour is the third
signal (token), so the badge reads correctly in high-contrast and for a colour-blind operator
(WCAG 2.2 AA, "not colour alone" - mirrors `ConfidenceBadge`).

| Member | Summary |
|---|---|
| `LivenessBadge For(LivenessState state)` | **(gap)** |
| `string AccessibleName` | What a screen reader announces. Never just the colour name. |

## `WatcherSessionText`

*class* — `WatcherSessionsPaneViewModel.cs`

The literal shown when a dimension was never observed - honest, never blank (spec US-13).

| Member | Summary |
|---|---|
| `string NotRecorded = "Not Recorded"` | **(gap)** |

## `WatcherSessionRow`

*record* — `WatcherSessionsPaneViewModel.cs`

One session row of the Sessions surface - a dense, scannable, screen-reader-complete line. An
absent harness or model renders `NotRecorded`, never blank and never
a guess.

| Member | Summary |
|---|---|
| `string DisputedText = "⚠ Disputed"` | The prefix a session with a disputed episode carries (US-16 discoverability, no colour-alone). |
| `string DisplayLabel` | The dense one-line label (G6 Multi-Panel Data Terminal density). |
| `string AccessibleName` | The full row a screen reader announces (WCAG 2.2 AA). |
| `WatcherSessionRow From(WatcherSessionSnapshot snapshot)` | Builds an honest row from a snapshot: null harness/model become Not Recorded. |

## `WatcherSessionSnapshot`

*record* — `WatcherSessionsPaneViewModel.cs`

A point-in-time read of one session: its binding, computed liveness, span count, and Disputed state.

## `IWatcherSessionsQuery`

*interface* — `WatcherSessionsPaneViewModel.cs`

The read seam the Sessions pane consumes. A null pane query means no watcher store is wired.

## `WatcherSessionsQuery`

*class* — `WatcherSessionsPaneViewModel.cs`

Folds the observation store + liveness into session snapshots - the deterministic compute reader
(DM7: liveness is computed here, never stored). Ordered by the store's own enumeration
(repo, worktree, session), which the pane preserves.

| Member | Summary |
|---|---|
| `IReadOnlyList<WatcherSessionSnapshot> GetSessions()` | **(gap)** |

## `WatcherSessionsPaneViewModel`

*class* — `WatcherSessionsPaneViewModel.cs`

The Loomkeeper Sessions surface view model - the compute reader that closes the Phase-1
change-surface. It renders observed sessions honestly: Not Recorded for an unproven harness/model,
a no-colour-alone liveness badge, and the full state set. Mirrors `EvidencePaneViewModel`,
but its load is **synchronous** (a local store fold, no I/O) - so it can never strand on a
"Loading…" message the way an async construction-time binding did (DC-011).

| Member | Summary |
|---|---|
| `PaneState State { get; private set; } = PaneState.Loading` | **(gap)** |
| `IReadOnlyList<WatcherSessionRow> Rows { get; private set; } = []` | **(gap)** |
| `string StatusMessage { get; private set; } = "Loading sessions…"` | The one string the operator reads - always evidence, never reassurance. |
| `string LiveAnnouncement { get; private set; } = string.Empty` | Announced through a live region so a state change reaches a screen reader without motion. |
| `void Load()` | Loads the sessions synchronously. Degrades to an explicit state, never a blank success. |

## `WorkspaceDiagnostics`

*record* — `WorkspaceDiagnosticsViewModel.cs`

What the operator can see about the daemon behind the shell.

| Member | Summary |
|---|---|
| `string Describe()` | One paragraph for the announcement channel and the diagnostics pane. |

## `WorkspaceDiagnosticsViewModel`

*class* — `WorkspaceDiagnosticsViewModel.cs`

Reads the daemon's operational state so the shell can show it.

**Remarks.** **Read-only, deliberately.** Upgrade and rollback are choreographed against a store that
a running binary may not be able to read halfway through — the ordering (snapshot, journal,
migrate, gate, commit) exists precisely because a half-finished upgrade is worse than one that
never started. A button that starts that from inside the app being upgraded is not a convenience;
this surfaces the state and names what a rollback would do, and the act itself stays with the
Bootstrap.





**The MCP tool list is the registered set, not a guess.** It is read from the gateway so
a tool added without appearing here would be a discrepancy rather than a documentation lag.

| Member | Summary |
|---|---|
| `WorkspaceDiagnostics Read()` | **(gap)** |
| `IReadOnlyList<string> McpToolGatewayNames { get; } =` | The tools the MCP gateway exposes. Local-only and read-only by ADR-0004. |

### `IReadOnlyList<string> McpToolGatewayNames { get; } =`

The tools the MCP gateway exposes. Local-only and read-only by ADR-0004.

**Remarks.** **Derived from the gateway, not restated.** This was a hand-written
`["describe", "impact", "find", "knowledge"]` and it was wrong in both directions:
`impact` and `knowledge` are daemon IPC operations and have never been gateway
tools, while `standing` — added for US-16 — was missing. An operator reading the
diagnostics was told about two tools that do not exist and not told about one that
does.





A second authority on what a component exposes disagrees with it eventually; this one
disagreed on three of five entries. Reflected over the methods that actually return an
`McpToolResult`, so a tool added tomorrow appears here without
anyone remembering this list exists (DC-021).
