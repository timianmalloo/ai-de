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

public sealed record DescribeResult(
    NodeView Node,
    IReadOnlyList<EdgeView> Neighbors,
    ResultBounds Bounds,
    string SourceRevision);

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
    /// Assertions per evidence page.
    /// </summary>
    /// <remarks>
    /// Sized so a page stays comfortably inside <see cref="MaxResultBytes"/> once serialised — an
    /// assertion carries its provenance, so it is far heavier per row than a search match. The
    /// caller pages; it does not get one enormous answer, and it does not get a silent truncation.
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

        return new DescribeResult(NodeOf(reader, nodeId), edges, bounds, revision);
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
    /// The panes want all of it and were rebuilding it node by node through <see cref="Describe"/>,
    /// which bounds neighbours at 50 and dropped two join edges of 124 doing so. This asks the
    /// question they were actually asking. Bounded per page rather than per call, so it can cross a
    /// pipe without breaching the result-byte cap.
    /// </remarks>
    public EvidencePage Evidence(string? cursor, int maxAssertions)
    {
        using var activity = Activity.StartActivity("aide.projection.query");
        activity?.SetTag("projection", "evidence");

        var limit = Clamp(maxAssertions, 1, MaxEvidencePageCeiling);
        using var reader = store.BeginRead();

        var after = EvidenceCursor.Parse(cursor);
        var rows = reader.CurrentAssertionPage(after, limit);

        activity?.SetTag("returned.assertions", rows.Count);

        // A page that came back full MIGHT have more behind it; one that came back short cannot.
        // Erring towards "there is more" costs one empty round trip and never loses a row.
        var next = rows.Count < limit
            ? null
            : EvidenceCursor.Format(rows[^1].Subject, rows[^1].Predicate, rows[^1].Object, rows[^1].ScopeId);

        return new EvidencePage(
            [.. rows.Select(r => new EvidenceAssertion(
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

        var graph = new GraphProjection(assertions, reader.CurrentSourceRevision())
            .Compute(query with { MaxNodes = Clamp(query.MaxNodes, 1, GraphProjection.DefaultMaxNodes) });

        activity?.SetTag("returned.nodes", graph.Nodes.Count);
        activity?.SetTag("returned.edges", graph.Edges.Count);
        activity?.SetTag("omitted.nodes", graph.Omitted);

        return graph;
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

        var matches = candidates
            .Select(id =>
            {
                var node = NodeOf(reader, id);
                return new FindMatch(node.NodeId, node.NodeKind, node.DisplayLabel,
                    // Phase 1 has no agent-authored records yet; stating the origin explicitly now
                    // means the field exists on the wire before agents can write, rather than being
                    // retrofitted after the laundering path is already open.
                    AuthorshipOrigin.RepositoryArtifact);
            })
            .ToList();

        var bounds = new ResultBounds(
            limit, 0, MaxResultBytes, matches.Count, Math.Max(0, totalMatched - matches.Count),
            0, 0, false, null);

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

        // Knowledge nodes are found by predicate, which has its own index — the projection never
        // reads the source corpus it is not interested in (P1-PERF-03).
        var typedAssertions = reader.AssertionsWithPredicate("has_type", MaxNodesCeiling);
        var typed = typedAssertions
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
            var links = touching.Where(a => a.Subject == id && a.Predicate is not ("has_type" or "owned_by")).ToList();
            var backlinks = touching.Where(a => a.Object == id && a.Predicate is not ("has_type" or "owned_by")).ToList();

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

    private static int Clamp(int requested, int min, int max) => Math.Max(min, Math.Min(requested, max));
}
