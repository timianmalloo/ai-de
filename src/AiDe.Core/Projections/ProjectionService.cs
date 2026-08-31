using System.Diagnostics;
using System.Text;
using AiDe.Core.Facts;
using AiDe.Core.Store;

namespace AiDe.Core.Projections;

public static class ProjectionErrorCodes
{
    public const string LimitExceeded = "AIDE-MCP-LIMIT-EXCEEDED";
    public const string NodeUnknown = "AIDE-PROJECTION-NODE-UNKNOWN";
}

/// <summary>
/// What a bounded result actually returned, and what it left out. Every projection carries this:
/// a truncated result that does not publish its omission is indistinguishable from a complete one,
/// which is how a "bounded" tool silently becomes an unbounded context assembler.
/// </summary>
public sealed record ResultBounds(
    int MaxNodes,
    int MaxEdges,
    int MaxBytes,
    int ReturnedNodes,
    int OmittedNodes,
    int ReturnedEdges,
    int OmittedEdges,
    bool ByteCapped,
    /// <summary>
    /// <b>Always null. No projection returning <see cref="ResultBounds"/> pages.</b>
    /// </summary>
    /// <remarks>
    /// Kept because the wire shape is published, and removing a field is a breaking change for a
    /// field nobody reads. Said out loud because a caller could reasonably loop on it and never get
    /// past the first page, with nothing failing. The one projection that DOES page —
    /// <see cref="ProjectionService.Evidence"/> — returns <see cref="EvidencePage"/> and its own
    /// cursor, which is populated and tested.
    /// </remarks>
    string? NextCursor);

public sealed record EdgeView(
    string Subject,
    string Predicate,
    string Object,
    VerificationStatus Status,
    EvidenceOrigin Origin,
    string ArtifactRevision,
    Provenance Provenance);

public sealed record NodeView(string NodeId, string NodeKind, string DisplayLabel);

/// <param name="NeighborKinds">
/// Each neighbouring node's own <c>has_type</c>, keyed by node id.
/// </param>
/// <remarks>
/// <b>INV-0004.</b> The canvas hardcoded <c>"source"</c> as every neighbour's kind because the
/// describe result did not carry one — so a drill-down showed a table, a bicep resource and a class
/// as the same thing, and the filter could not tell them apart. The kind is a property of the
/// NEIGHBOUR, and only the projection can read it; a renderer inventing a default is a renderer
/// stating a fact it does not have.
/// </remarks>
public sealed record DescribeResult(
    NodeView Node,
    IReadOnlyList<EdgeView> Neighbors,
    ResultBounds Bounds,
    string SourceRevision,
    IReadOnlyDictionary<string, string>? NeighborKinds = null);

public sealed record ImpactResult(
    string RootNodeId,
    IReadOnlyList<NodeView> Nodes,
    IReadOnlyList<EdgeView> Edges,
    ResultBounds Bounds,
    string SourceRevision);

public sealed record FindMatch(string NodeId, string NodeKind, string DisplayLabel, AuthorshipOrigin Authorship);

public sealed record FindResult(IReadOnlyList<FindMatch> Matches, ResultBounds Bounds, string SourceRevision);

/// <summary>One page of current evidence, and where to continue.</summary>
/// <param name="NextCursor">Null when this page is the last. Opaque to the caller, by design.</param>
public sealed record EvidencePage(
    IReadOnlyList<Facts.EvidenceAssertion> Assertions,
    string? NextCursor,
    string SourceRevision);

/// <summary>
/// The paging cursor: the last row's ordering tuple, encoded.
/// </summary>
/// <remarks>
/// Base64 of the three ordered fields, so it survives a JSON round trip and a caller cannot
/// construct one by hand and expect it to mean something. Ordering by the same tuple the cursor
/// carries is what makes a page boundary unable to skip or repeat a row.
/// </remarks>
internal static class EvidenceCursor
{
    private const char Separator = '';

    internal static string Format(string subject, string predicate, string obj, string scopeId) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            string.Join(Separator, subject, predicate, obj, scopeId)));

    internal static (string Subject, string Predicate, string Object, string ScopeId)? Parse(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return null;

        try
        {
            var parts = System.Text.Encoding.UTF8
                .GetString(Convert.FromBase64String(cursor))
                .Split(Separator);

            // A malformed cursor restarts from the beginning rather than throwing. The caller gets
            // rows it has already seen, which is wasteful and correct; the alternative is a failed
            // read for a value the caller was never supposed to inspect.
            return parts.Length == 4 ? (parts[0], parts[1], parts[2], parts[3]) : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

/// <summary>
/// Who authored a record. Carried on every read result so a consuming agent can tell a repository
/// fact from something another agent wrote — without it, an agent-authored note is laundered back
/// out as workspace knowledge.
/// </summary>
public enum AuthorshipOrigin
{
    RepositoryArtifact,
    Human,
    Agent,
}

public sealed record KnowledgeQuery(string? Term, string? Type, int MaxResults);

public sealed record KnowledgeNodeView(
    string NodeId,
    string Type,
    string? Owner,
    IReadOnlyList<EdgeView> Links,
    IReadOnlyList<EdgeView> Backlinks,
    string? SourceLocation,
    IReadOnlyList<string> HealthFindings);

public sealed record KnowledgeResult(
    IReadOnlyList<KnowledgeNodeView> Nodes,
    ResultBounds Bounds,
    string SourceRevision);

/// <summary>
/// Bounded, self-describing projections over the current fact set.
/// </summary>
/// <remarks>
/// Pattern: CQRS / Materialized Read Model. Every result is rebuildable from facts and every one is
/// capped on nodes, edges AND bytes — the byte cap matters because node labels come from repository
/// content, so a count-only cap still admits an unbounded payload.
/// </remarks>
public sealed class ProjectionService(WorkspaceStore store)
{
    private static readonly ActivitySource Activity = new("aide.projection.query");

    public const int MaxNeighborsCeiling = 50;
    public const int MaxEdgesCeiling = 500;
    public const int MaxNodesCeiling = 200;
    public const int MaxResultBytes = 64 * 1024;

    /// <summary>
    /// The most a single response may serialise to.
    /// </summary>
    /// <remarks>
    /// <para><b>Derived from the transport, with headroom.</b> One IPC frame carries 1,048,576 bytes
    /// (<c>IpcFraming.MaxFrameBytes</c>); this leaves a quarter of it spare for the response envelope
    /// and for the difference between an estimate and the truth. A projection that fills the frame
    /// exactly is one repository away from INV-0003.</para>
    ///
    /// <para><b>Why a byte budget and not a bigger count ceiling.</b> Every ceiling in this class
    /// counts ITEMS and the transport limit is in BYTES, and node labels, subjects and paths all come
    /// from repository content — so a count-only cap admits an unbounded payload. That is not a
    /// hypothetical: MEASURED on a real repository, an evidence page of 2,000 assertions serialises
    /// to <b>1,004,397 bytes</b>, which is 95.8% of the frame and fifteen times the
    /// <see cref="MaxResultBytes"/> its own documentation claimed it stayed "comfortably inside".</para>
    ///
    /// <para><b>Why it is not three quarters of the frame, which is what it looks like.</b> A
    /// response is serialised to JSON, and that JSON is then carried as a <b>string field</b> inside
    /// the IPC envelope — so every quote in it is escaped again. MEASURED on a real workspace: a
    /// 727,244-byte graph, comfortably inside a 768 KiB budget, reached <b>1,137,104 bytes</b> on the
    /// wire and the transport refused it. The inflation was 1.56–1.57x across every payload measured.
    /// The budget had been checked on the inner bytes and enforced on the outer ones.</para>
    ///
    /// <para>Half a frame, so the assumed worst case is 2x — comfortably above the 1.57x measured,
    /// because a guard that under-counts lets the frame overflow and the user is told only that the
    /// graph could not be loaded. <c>TheBudgetFitsTheFrameTests</c> asserts the arithmetic, so the
    /// two constants cannot drift apart.</para>
    ///
    /// <para><c>simplify: a factor against a measured inflation rather than the exact framed size;
    /// ceiling is that row-wise bounds (evidence, find) cannot afford to serialise per row; upgrade
    /// trigger = the envelope carries the payload as raw JSON instead of an escaped string, at which
    /// point the two sizes agree and the factor disappears.</c></para>
    /// </remarks>
    public const int MaxResponseBytes = 480 * 1024;

    /// <summary>The transport's own limit, restated here only so the budget can be checked against it.</summary>
    public const int FrameBytes = Ipc.IpcFraming.MaxFrameBytes;

    /// <summary>
    /// What a shrunk graph must fit inside — the frame, less real headroom.
    /// </summary>
    /// <remarks>
    /// Shrinking stops at the FIRST size that fits, so a target equal to the frame leaves whatever
    /// margin the last step happened to produce. MEASURED with no headroom: 1,044,916 bytes against
    /// a 1,048,576 frame — 3,660 bytes, which is one longer type name away from failing. A limit met
    /// exactly is not a limit respected.
    /// </remarks>
    public const int MaxFramedGraphBytes = FrameBytes - (64 * 1024);

    /// <summary>
    /// What one assertion costs in JSON beyond its own text.
    /// </summary>
    /// <remarks>
    /// MEASURED, not estimated: a 2,000-assertion page whose subjects, predicates, objects and paths
    /// total 238,002 bytes serialises to 1,004,397 — <b>383 bytes per row</b> of field names,
    /// timestamps, enum spellings and punctuation. Rounded up for headroom, because a guard that
    /// under-counts is a guard that lets the frame overflow.
    /// </remarks>
    public const int AssertionOverheadBytes = 448;

    /// <summary>
    /// How many times a graph may be shrunk before it must already fit.
    /// </summary>
    /// <remarks>
    /// Each round takes at least a third off, so twelve rounds reduce five thousand nodes to fewer
    /// than five — far past any real graph. It is a cost bound, not the thing that makes the loop
    /// terminate; the guaranteed reduction does that.
    /// </remarks>
    public const int MaxShrinkAttempts = 12;

    /// <summary>
    /// The ceiling on a SEARCH, which is a different question from a neighbour list.
    /// </summary>
    /// <remarks>
    /// <para><b>Find used to borrow <see cref="MaxNeighborsCeiling"/>, and 50 is the wrong number
    /// for it by two orders of magnitude.</b> The workbench asks for 20,000 matches to build the
    /// context and join panes; it received 50. Those panes were computing crossing counts, join
    /// counts and coverage from roughly three percent of a real workspace, and presenting the result
    /// as the answer — while a spike reading the store directly showed the whole picture and
    /// disagreed with the product for days.</para>
    ///
    /// <para>A search returns identity columns only — id, kind, label — so the payload per row is
    /// small and bounded, which is why this ceiling can be large where the neighbour one cannot.
    /// <see cref="MaxResultBytes"/> still applies underneath.</para>
    /// </remarks>
    public const int MaxSearchResultsCeiling = 20_000;

    /// <summary>
    /// Routes returned before the answer is truncated.
    /// </summary>
    /// <remarks>
    /// A reader comparing routes is choosing between them, and nobody chooses between two hundred.
    /// The cap is small on purpose and the truncation is reported.
    /// </remarks>
    /// <summary>
    /// Groups returned before the rest are counted and dropped.
    /// </summary>
    /// <remarks>
    /// An overview a person can read has tens of groups, not thousands; past a few hundred it is a
    /// hairball again at a coarser grain, which is the failure it exists to prevent.
    /// </remarks>
    public const int MaxClustersCeiling = 500;

    public const int MaxPathsCeiling = 100;

    /// <summary>
    /// The longest route worth returning, in edges.
    /// </summary>
    /// <remarks>
    /// Beyond about a dozen hops "A reaches B" stops being a fact about the design and becomes a
    /// fact about the graph being connected — in a codebase almost everything reaches almost
    /// everything if you allow enough steps.
    /// </remarks>
    public const int MaxPathLengthCeiling = 12;

    /// <summary>
    /// Assertions per evidence page.
    /// </summary>
    /// <remarks>
    /// <para><b>A COUNT ceiling, and it does not bound the payload.</b> This used to say the page was
    /// "sized so it stays comfortably inside <see cref="MaxResultBytes"/> once serialised". MEASURED:
    /// 2,000 assertions serialise to 1,004,397 bytes, which is fifteen times that constant and 95.8%
    /// of an IPC frame. The sentence was written, believed, and never checked.</para>
    ///
    /// <para>What actually bounds the page is <see cref="MaxResponseBytes"/>, applied row by row in
    /// <see cref="Evidence"/>; this count is the coarser of the two limits and usually is not the one
    /// that fires. An assertion carries its provenance, so it is far heavier per row than a search
    /// match — which is the reason a count could never have been the bound.</para>
    /// </remarks>
    public const int MaxEvidencePageCeiling = 2_000;

    public DescribeResult Describe(string nodeId, int maxNeighbors)
    {
        using var activity = Activity.StartActivity("aide.projection.query");
        activity?.SetTag("projection", "describe");

        var limit = Clamp(maxNeighbors, 1, MaxNeighborsCeiling);
        using var reader = store.BeginRead();

        // The bound is applied in SQL, not after materializing the corpus: a bounded read must cost
        // what its result costs, not what the graph costs (P1-PERF-02).
        var touching = reader.AssertionsTouching(nodeId, limit);
        var total = reader.CountAssertionsTouching(nodeId);

        var (kept, byteCapped) = TakeWithinByteBudget(touching, limit);
        var edges = kept.Select(ToEdge).ToList();
        var revision = touching.Count > 0 ? touching[0].ArtifactRevision : reader.CurrentSourceRevision();

        var bounds = new ResultBounds(
            MaxNodes: 1, MaxEdges: limit, MaxBytes: MaxResultBytes,
            ReturnedNodes: 1, OmittedNodes: 0,
            ReturnedEdges: edges.Count, OmittedEdges: Math.Max(0, total - edges.Count),
            ByteCapped: byteCapped, NextCursor: null);

        activity?.SetTag("returned.edges", edges.Count);
        activity?.SetTag("omitted.edges", bounds.OmittedEdges);

        // The kinds of the nodes on the other end. Read here because the projection is the only
        // thing that can: the canvas has ids and nothing else.
        var kinds = edges
            .SelectMany(e => new[] { e.Subject, e.Object })
            .Where(id => !string.Equals(id, nodeId, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(id => id, id => NodeOf(reader, id).NodeKind, StringComparer.Ordinal);

        return new DescribeResult(NodeOf(reader, nodeId), edges, bounds, revision, kinds);
    }

    /// <summary>
    /// Bounded dependent-neighbourhood walk. Breadth-first with an explicit frontier cap, so the
    /// traversal cannot fan out into the whole graph — the caller always learns what was omitted.
    /// </summary>
    public ImpactResult Impact(string nodeId, int maxNodes, int maxEdges)
    {
        using var activity = Activity.StartActivity("aide.projection.query");
        activity?.SetTag("projection", "impact");

        var nodeLimit = Clamp(maxNodes, 1, MaxNodesCeiling);
        var edgeLimit = Clamp(maxEdges, 1, MaxEdgesCeiling);

        using var reader = store.BeginRead();

        var visited = new HashSet<string>(StringComparer.Ordinal) { nodeId };
        var order = new List<string> { nodeId };
        var edges = new List<StoredAssertion>();
        var queue = new Queue<string>();
        queue.Enqueue(nodeId);
        var omittedNodes = 0;
        var omittedEdges = 0;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            // One indexed lookup per frontier node, bounded by the remaining edge budget. The walk
            // therefore costs what it visits — previously it grouped the entire corpus up front,
            // so a 3-node neighbourhood paid for all 50,000 edges (P1-PERF-03).
            var remaining = edgeLimit - edges.Count + 1;
            var outgoing = reader.OutgoingAssertions(current, Math.Max(1, remaining));
            if (outgoing.Count == 0)
            {
                continue;
            }

            foreach (var assertion in outgoing)
            {
                if (edges.Count >= edgeLimit)
                {
                    omittedEdges++;
                    continue;
                }

                if (!visited.Contains(assertion.Object))
                {
                    if (order.Count >= nodeLimit)
                    {
                        omittedNodes++;
                        continue;
                    }

                    visited.Add(assertion.Object);
                    order.Add(assertion.Object);
                    queue.Enqueue(assertion.Object);
                }

                edges.Add(assertion);
            }
        }

        var (kept, byteCapped) = TakeWithinByteBudget(edges, edges.Count);
        omittedEdges += edges.Count - kept.Count;

        var bounds = new ResultBounds(
            nodeLimit, edgeLimit, MaxResultBytes,
            order.Count, omittedNodes, kept.Count, omittedEdges, byteCapped, null);

        var revision = edges.Count > 0 ? edges[0].ArtifactRevision : reader.CurrentSourceRevision();
        activity?.SetTag("returned.nodes", order.Count);

        return new ImpactResult(
            nodeId,
            order.Select(id => NodeOf(reader, id)).ToList(),
            kept.Select(ToEdge).ToList(),
            bounds,
            revision);
    }

    /// <summary>
    /// One page of every current assertion, for a caller that wants the whole set.
    /// </summary>
    /// <remarks>
    /// <para>The panes want all of it and were rebuilding it node by node through
    /// <see cref="Describe"/>, which bounds neighbours at 50 and dropped two join edges of 124 doing
    /// so. This asks the question they were actually asking.</para>
    ///
    /// <para><b>Bounded by BYTES as well as by count, and the byte bound is the one that matters.</b>
    /// This method's documentation used to claim a page "can cross a pipe without breaching the
    /// result-byte cap". It could not: MEASURED on a real repository, a 2,000-assertion page is
    /// <b>1,004,397 bytes</b> against a 1,048,576-byte frame — 95.8% full, and over the frame
    /// entirely on a repository with slightly longer type names. The claim was written, believed and
    /// never checked, which is the same shape as INV-0003 one method along.</para>
    ///
    /// <para>Truncating a page early is LOSSLESS here, and that is why the fix belongs at this level:
    /// the cursor continues from the last row actually returned, so a byte-bounded page costs one
    /// extra round trip and never drops a row.</para>
    /// </remarks>
    public EvidencePage Evidence(string? cursor, int maxAssertions)
    {
        using var activity = Activity.StartActivity("aide.projection.query");
        activity?.SetTag("projection", "evidence");

        var limit = Clamp(maxAssertions, 1, MaxEvidencePageCeiling);
        using var reader = store.BeginRead();

        var after = EvidenceCursor.Parse(cursor);
        var rows = reader.CurrentAssertionPage(after, limit);

        // The BYTE bound, applied before the count bound can pretend to be one.
        var kept = new List<StoredAssertion>(rows.Count);
        var bytes = 0;
        var truncatedByBytes = false;

        foreach (var row in rows)
        {
            var size = Encoding.UTF8.GetByteCount(row.Subject)
                + Encoding.UTF8.GetByteCount(row.Predicate)
                + Encoding.UTF8.GetByteCount(row.Object)
                + Encoding.UTF8.GetByteCount(row.ScopeId)
                + Encoding.UTF8.GetByteCount(row.ArtifactRevision)
                + Encoding.UTF8.GetByteCount(row.Provenance.ArtifactPathId)
                + AssertionOverheadBytes;

            // At least one row always goes back. A page that returns nothing because its first row
            // is enormous is a caller that can never make progress, which is worse than one frame
            // that is slightly over.
            if (kept.Count > 0 && bytes + size > MaxResponseBytes)
            {
                truncatedByBytes = true;
                break;
            }

            bytes += size;
            kept.Add(row);
        }

        activity?.SetTag("returned.assertions", kept.Count);
        activity?.SetTag("returned.bytes", bytes);
        activity?.SetTag("truncated.by_bytes", truncatedByBytes);

        // A page that came back full MIGHT have more behind it; one that came back short cannot —
        // UNLESS the byte bound cut it short, in which case there is certainly more and the cursor
        // must say so or the caller stops early believing it has everything.
        var next = truncatedByBytes || rows.Count == limit
            ? EvidenceCursor.Format(kept[^1].Subject, kept[^1].Predicate, kept[^1].Object, kept[^1].ScopeId)
            : null;

        return new EvidencePage(
            [.. kept.Select(r => new EvidenceAssertion(
                r.ScopeId, r.ArtifactRevision, r.Subject, r.Predicate, r.Object,
                r.Origin, r.Status, r.Provenance))],
            next,
            reader.CurrentSourceRevision());
    }

    /// <summary>
    /// The whole workspace as a graph.
    /// </summary>
    /// <remarks>
    /// The question the graph surface was never asking. It requested one node and that node's
    /// neighbours, so a workspace of 12,100 assertions rendered as two nodes — reported against the
    /// same repository viewed in Obsidian.
    /// </remarks>
    public WorkspaceGraph Graph(int maxNodes) => Graph(new GraphQuery(maxNodes));

    /// <summary>The graph the query asks for — filtered before the cap applies.</summary>
    public WorkspaceGraph Graph(GraphQuery query)
    {
        using var activity = Activity.StartActivity("aide.projection.query");
        activity?.SetTag("projection", "graph");

        using var reader = store.BeginRead();

        var assertions = reader.AllCurrentAssertions()
            .Select(a => new EvidenceAssertion(
                a.ScopeId, a.ArtifactRevision, a.Subject, a.Predicate, a.Object,
                a.Origin, a.Status, a.Provenance))
            .ToList();

        var projection = new GraphProjection(assertions, reader.CurrentSourceRevision());
        var graph = projection.Compute(
            query with { MaxNodes = Clamp(query.MaxNodes, 1, GraphProjection.DefaultMaxNodes) });

        // The graph at its own COUNT ceiling still overflows the frame — MEASURED, 1,522,915 bytes
        // for 5,000 permitted nodes on a real repository. The canvas asks for a bounded default now,
        // but the operation is still reachable, and an operation that can never succeed is a defect
        // whoever calls it. Shrink to fit and let Omitted say so, rather than build a response the
        // transport will refuse.
        // Shrink until it FITS, and check that it did.
        //
        // The previous version applied ONE proportional correction and returned whatever came back.
        // That assumes bytes fall in proportion to node count, and they do not: nodes are kept in
        // degree order, so the ones that survive a cut are the most connected ones, and the edges
        // they carry dominate the payload. Cutting 15% of the nodes can cut 2% of the bytes.
        //
        // MEASURED on a real workspace: the response reached 1,176,341 bytes against a 1,048,576
        // frame, and the only thing the user saw was "The graph could not be loaded" on opening the
        // workspace — a shrink that had run, reported success by returning, and not worked.
        var weight = FramedCost(graph);
        var attempts = 0;

        while (weight > MaxFramedGraphBytes && graph.Nodes.Count > 1 && attempts < MaxShrinkAttempts)
        {
            var proportional = (int)(graph.Nodes.Count * (MaxFramedGraphBytes / (double)weight) * 0.85);

            // A third off every round at minimum, so this terminates even on a graph whose bytes
            // barely move when its node count does. Without it the loop ends only at the attempt
            // cap — and the cap firing would mean returning a response the transport refuses, which
            // is a circuit breaker used as a termination argument (GO12).
            var next = Math.Clamp(proportional, 1, graph.Nodes.Count * 2 / 3);

            graph = projection.Compute(query with { MaxNodes = next });
            weight = FramedCost(graph);
            attempts++;
        }

        activity?.SetTag("shrunk.attempts", attempts);
        activity?.SetTag("returned.bytes", weight);

        activity?.SetTag("returned.nodes", graph.Nodes.Count);
        activity?.SetTag("returned.edges", graph.Edges.Count);
        activity?.SetTag("omitted.nodes", graph.Omitted);

        return graph;
    }

    /// <summary>
    /// The workspace at a distance: groups rather than nodes, for a graph too large to draw.
    /// </summary>
    /// <remarks>
    /// Built over the same <see cref="Graph"/> projection the canvas uses, so an overview can never
    /// summarise a node the detailed view would not show. Two answers to one question is the defect
    /// signature this codebase has already paid for.
    /// </remarks>
    public WorkspaceOverview Overview(OverviewQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        using var activity = Activity.StartActivity("aide.projection.query");
        activity?.SetTag("projection", "overview");

        // The node graph underneath is asked for at the PROJECTION ceiling rather than the canvas
        // default: an overview summarises, so it should summarise as much as it can see, and the
        // result is groups whose count is bounded regardless.
        var graph = Graph(query.Query ?? new GraphQuery(GraphProjection.DefaultMaxNodes, IncludeExternal: false));

        var overview = GraphOverview.Summarise(graph, query with
        {
            MaxClusters = Clamp(query.MaxClusters, 1, MaxClustersCeiling),
        });

        activity?.SetTag("returned.clusters", overview.Clusters.Count);
        activity?.SetTag("returned.cluster_edges", overview.Edges.Count);
        activity?.SetTag("omitted.clusters", overview.OmittedClusters);

        return overview;
    }

    /// <summary>How one node reaches another, within the graph the query names.</summary>
    /// <remarks>
    /// Built over the same projection the graph surface uses, so a route can never contain an edge
    /// the picture does not show — two answers to one question is the defect signature this
    /// codebase has already paid for once.
    /// </remarks>
    public PathResult Paths(PathQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        using var activity = Activity.StartActivity("aide.projection.query");
        activity?.SetTag("projection", "paths");

        var graph = Graph(query.Query ?? new GraphQuery());

        var result = GraphPaths.Find(graph, query with
        {
            MaxPaths = Clamp(query.MaxPaths, 1, MaxPathsCeiling),
            MaxLength = Clamp(query.MaxLength, 1, MaxPathLengthCeiling),
        });

        activity?.SetTag("returned.paths", result.Paths.Count);
        activity?.SetTag("truncated", result.Truncated);

        return result;
    }

    public FindResult Find(string term, int maxResults)
    {
        using var activity = Activity.StartActivity("aide.projection.query");
        activity?.SetTag("projection", "find");

        var limit = Clamp(maxResults, 1, MaxSearchResultsCeiling);
        using var reader = store.BeginRead();

        // Identity columns only: a leading-wildcard LIKE cannot use an index, so the cheapest
        // correct shape is to scan a covering index instead of hydrating every row's provenance.
        var (candidates, totalMatched) = reader.SearchNodeIds(term, limit);

        // The byte bound, ENFORCED rather than merely declared. This built the whole match list and
        // then reported `MaxBytes: 65,536` beside it — MEASURED at 461,750 bytes returned on a real
        // repository, and the ceiling permits 20,000 results where this repository happened to have
        // 2,764. A caller reading the bounds was told a limit that could not fire (DC-016), and a
        // repository with more matches would have overflowed the frame exactly like INV-0003.
        var matches = new List<FindMatch>();
        var bytes = 0;
        var byteCapped = false;

        foreach (var id in candidates)
        {
            var node = NodeOf(reader, id);

            var match = new FindMatch(node.NodeId, node.NodeKind, node.DisplayLabel,
                // Phase 1 has no agent-authored records yet; stating the origin explicitly now
                // means the field exists on the wire before agents can write, rather than being
                // retrofitted after the laundering path is already open.
                AuthorshipOrigin.RepositoryArtifact);

            var size = Encoding.UTF8.GetByteCount(match.NodeId)
                + Encoding.UTF8.GetByteCount(match.NodeKind)
                + Encoding.UTF8.GetByteCount(match.DisplayLabel)
                + AssertionOverheadBytes;

            // At least one result always comes back, for the same reason the evidence page keeps
            // one row: a search that returns nothing because its first hit is long is worse than a
            // response slightly over an internal budget.
            if (matches.Count > 0 && bytes + size > MaxResponseBytes)
            {
                byteCapped = true;
                break;
            }

            bytes += size;
            matches.Add(match);
        }

        activity?.SetTag("returned.bytes", bytes);
        activity?.SetTag("byte.capped", byteCapped);

        var bounds = new ResultBounds(
            limit, 0, MaxResponseBytes, matches.Count, Math.Max(0, totalMatched - matches.Count),
            0, 0, byteCapped, null);

        return new FindResult(matches, bounds, reader.CurrentSourceRevision());
    }

    /// <summary>
    /// US-4: knowledge navigation. Same facts, filtered to knowledge-kind subjects, with backlinks
    /// and the health findings the spec requires when source/owner/links are missing.
    /// </summary>
    public KnowledgeResult Knowledge(KnowledgeQuery query)
    {
        using var activity = Activity.StartActivity("aide.projection.query");
        activity?.SetTag("projection", "knowledge");

        var limit = Clamp(query.MaxResults, 1, MaxNeighborsCeiling);
        using var reader = store.BeginRead();

        // KNOWLEDGE FIRST, then everything else. This read the first 200 `has_type` assertions and
        // filtered THOSE to knowledge — so on any real repository the 200 were C# types in
        // alphabetical order and the filter left nothing. MEASURED: 0 items returned on a workspace
        // holding 468 knowledge nodes.
        //
        // The same defect as DC-035 one projection along: a cap applied before the filter returns
        // the wrong slice trimmed to the right shape, and nothing in the result says so. The node
        // class has its own index, so asking for knowledge directly is also the cheaper query.
        var knowledge = reader.KnowledgeNodeIds(MaxNodesCeiling);

        var typedAssertions = reader.AssertionsWithPredicate("has_type", MaxSearchResultsCeiling);
        var typed = typedAssertions
            .Where(a => knowledge.Contains(a.Subject))
            .GroupBy(a => a.Subject, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var ids = typed.Keys
            .Where(id => query.Term is null || id.Contains(query.Term, StringComparison.OrdinalIgnoreCase))
            .Where(id => query.Type is null || string.Equals(typed[id].Object, query.Type, StringComparison.OrdinalIgnoreCase))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        var nodes = new List<KnowledgeNodeView>();
        foreach (var id in ids.Take(limit))
        {
            var typeAssertion = typed[id];
            var touching = reader.AssertionsTouching(id, MaxEdgesCeiling);
            var owner = touching.FirstOrDefault(a => a.Subject == id && a.Predicate == "owned_by");
            // `review_by` and `declared_in` describe the document; they are not links to other
            // knowledge, and drawing them as such puts a date in the graph as a thing to navigate to.
            var links = touching.Where(a => a.Subject == id
                && a.Predicate is not ("has_type" or "owned_by" or "review_by" or "declared_in" or "node_class")).ToList();

            var backlinks = touching.Where(a => a.Object == id
                && a.Predicate is not ("has_type" or "owned_by" or "review_by" or "declared_in" or "node_class")).ToList();

            // Missing evidence is surfaced as a health finding rather than rendered as a clean node —
            // the spec's "absence of evidence stays explicit".
            var findings = new List<string>();
            if (owner is null)
            {
                findings.Add("owner not recorded");
            }

            if (typeAssertion.Object == "unknown")
            {
                findings.Add("type not recorded");
            }

            if (links.Count == 0 && backlinks.Count == 0)
            {
                findings.Add("orphan: no inbound or outbound links");
            }

            if (typeAssertion.Provenance.SourceLocation is null)
            {
                findings.Add("source location not recorded");
            }

            // A review date that has passed is the one health finding that arrives on its own: the
            // document has not changed, the calendar has. Read from the frontmatter the pack already
            // writes, so a stale artifact says so rather than waiting to be noticed.
            var reviewBy = touching.FirstOrDefault(a => a.Subject == id && a.Predicate == "review_by");

            if (reviewBy is not null
                && DateOnly.TryParse(reviewBy.Object, System.Globalization.CultureInfo.InvariantCulture, out var due)
                && due < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                findings.Add($"review overdue since {due:yyyy-MM-dd}");
            }

            nodes.Add(new KnowledgeNodeView(
                id, typeAssertion.Object, owner?.Object,
                links.Select(ToEdge).ToList(), backlinks.Select(ToEdge).ToList(),
                typeAssertion.Provenance.SourceLocation, findings));
        }

        var bounds = new ResultBounds(
            limit, MaxEdgesCeiling, MaxResultBytes, nodes.Count, ids.Count - nodes.Count,
            nodes.Sum(n => n.Links.Count + n.Backlinks.Count), 0, false, null);

        return new KnowledgeResult(nodes, bounds,
            typedAssertions.Count > 0 ? typedAssertions[0].ArtifactRevision : reader.CurrentSourceRevision());
    }

    /// <summary>
    /// Rebuilds the labelled claim cache from facts. Public because the equality test needs to prove
    /// the stored cache equals this derivation — a cache with no such test is a second source of truth.
    /// </summary>
    public IReadOnlyList<(string Subject, string Predicate, string Object, string Status, int Count, string Revision)>
        DeriveClaimCurrent()
    {
        using var reader = store.BeginRead();
        return reader.AllCurrentAssertions()
            .GroupBy(a => (a.Subject, a.Predicate, a.Object))
            .Select(g => (
                g.Key.Subject, g.Key.Predicate, g.Key.Object,
                // The weakest status wins: a relation is only as established as its least certain
                // supporting assertion. Promoting on the strongest would manufacture confidence.
                Status: g.Max(a => a.Status).ToString(),
                Count: g.Count(),
                Revision: g.First().ArtifactRevision))
            .OrderBy(r => r.Subject, StringComparer.Ordinal)
            .ThenBy(r => r.Predicate, StringComparer.Ordinal)
            .ThenBy(r => r.Object, StringComparer.Ordinal)
            .ToList();
    }

    private static NodeView NodeOf(StoreReader reader, string nodeId)
        => new(nodeId, reader.ReadNodeKind(nodeId) ?? "unknown", reader.ReadNodeLabel(nodeId) ?? nodeId);

    private static EdgeView ToEdge(StoredAssertion a)
        => new(a.Subject, a.Predicate, a.Object, a.Status, a.Origin, a.ArtifactRevision, a.Provenance);

    /// <summary>
    /// Applies the count cap and then the byte budget. Labels come from repository content, so a
    /// count-only cap still admits a multi-megabyte payload built from adversarially long names.
    /// </summary>
    private static (List<StoredAssertion> Kept, bool ByteCapped) TakeWithinByteBudget(
        IReadOnlyList<StoredAssertion> source, int countLimit)
    {
        var kept = new List<StoredAssertion>();
        var bytes = 0;
        var capped = false;

        foreach (var assertion in source.Take(countLimit))
        {
            var size = Encoding.UTF8.GetByteCount(assertion.Subject)
                + Encoding.UTF8.GetByteCount(assertion.Predicate)
                + Encoding.UTF8.GetByteCount(assertion.Object)
                + Encoding.UTF8.GetByteCount(assertion.Provenance.ArtifactPathId);

            if (bytes + size > MaxResultBytes)
            {
                capped = true;
                break;
            }

            bytes += size;
            kept.Add(assertion);
        }

        return (kept, capped);
    }

    /// <summary>
    /// What a graph will cost on the wire, near enough to decide whether it fits.
    /// </summary>
    /// <remarks>
    /// The same shape as the assertion estimate and for the same reason: ids and labels come from
    /// repository content, so counting nodes tells you nothing about bytes. Deliberately an estimate
    /// rather than a serialisation — serialising to find out whether to serialise costs what it
    /// saves, and the budget already carries a quarter-frame of headroom for the difference.
    /// </remarks>
    private static readonly System.Text.Json.JsonSerializerOptions Wire =
        new(System.Text.Json.JsonSerializerDefaults.Web);

    /// <summary>
    /// What a graph costs ON THE WIRE, framed exactly as the transport frames it.
    /// </summary>
    /// <remarks>
    /// <para>The graph is one object, so — unlike a row-wise bound — it can simply be measured, and
    /// the thing measured is the thing the transport counts: the payload serialised, escaped, and
    /// wrapped in the envelope. <see cref="Weigh"/> counts the payload only, which is the estimate
    /// that let a 727,244-byte graph reach 1,137,104 bytes on the wire and be refused.</para>
    ///
    /// <para>Serialising twice is not free, so it is only paid where it could matter: a graph under a
    /// third of a frame cannot reach it at any inflation observed (1.57x), and returns the estimate,
    /// which is under budget by the same arithmetic.</para>
    /// </remarks>
    private static int FramedCost(WorkspaceGraph graph)
    {
        var estimate = Weigh(graph);

        if (estimate * 3 <= FrameBytes) return estimate;

        return Encoding.UTF8.GetByteCount(System.Text.Json.JsonSerializer.Serialize(
            Ipc.IpcResponse.Success(System.Text.Json.JsonSerializer.Serialize(graph, Wire)), Wire));
    }

    private static int Weigh(WorkspaceGraph graph)
    {
        var bytes = 0;

        foreach (var node in graph.Nodes)
        {
            bytes += Encoding.UTF8.GetByteCount(node.Id)
                + Encoding.UTF8.GetByteCount(node.Label)
                + Encoding.UTF8.GetByteCount(node.Kind)
                + 64;
        }

        foreach (var edge in graph.Edges)
        {
            bytes += Encoding.UTF8.GetByteCount(edge.From)
                + Encoding.UTF8.GetByteCount(edge.To)
                + Encoding.UTF8.GetByteCount(edge.Predicate)
                + 64;
        }

        return bytes;
    }

    private static int Clamp(int requested, int min, int max) => Math.Max(min, Math.Min(requested, max));
}
