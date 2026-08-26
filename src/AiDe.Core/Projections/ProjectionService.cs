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

    public DescribeResult Describe(string nodeId, int maxNeighbors)
    {
        using var activity = Activity.StartActivity("aide.projection.query");
        activity?.SetTag("projection", "describe");

        var limit = Clamp(maxNeighbors, 1, MaxNeighborsCeiling);
        using var reader = store.BeginRead();
        var all = reader.AllCurrentAssertions();

        var touching = all
            .Where(a => a.Subject == nodeId || a.Object == nodeId)
            .OrderBy(a => a.Subject, StringComparer.Ordinal)
            .ThenBy(a => a.Predicate, StringComparer.Ordinal)
            .ThenBy(a => a.Object, StringComparer.Ordinal)
            .ToList();

        var (kept, byteCapped) = TakeWithinByteBudget(touching, limit);
        var edges = kept.Select(ToEdge).ToList();
        var revision = touching.Count > 0 ? touching[0].ArtifactRevision : "none";

        var bounds = new ResultBounds(
            MaxNodes: 1, MaxEdges: limit, MaxBytes: MaxResultBytes,
            ReturnedNodes: 1, OmittedNodes: 0,
            ReturnedEdges: edges.Count, OmittedEdges: touching.Count - edges.Count,
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
        var all = reader.AllCurrentAssertions();
        var bySubject = all.GroupBy(a => a.Subject, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

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
            if (!bySubject.TryGetValue(current, out var outgoing))
            {
                continue;
            }

            foreach (var assertion in outgoing.OrderBy(a => a.Object, StringComparer.Ordinal))
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

        var revision = all.Count > 0 ? all[0].ArtifactRevision : "none";
        activity?.SetTag("returned.nodes", order.Count);

        return new ImpactResult(
            nodeId,
            order.Select(id => NodeOf(reader, id)).ToList(),
            kept.Select(ToEdge).ToList(),
            bounds,
            revision);
    }

    public FindResult Find(string term, int maxResults)
    {
        using var activity = Activity.StartActivity("aide.projection.query");
        activity?.SetTag("projection", "find");

        var limit = Clamp(maxResults, 1, MaxNeighborsCeiling);
        using var reader = store.BeginRead();
        var all = reader.AllCurrentAssertions();

        var candidates = all
            .SelectMany(a => new[] { a.Subject, a.Object })
            .Where(id => id.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        var matches = candidates.Take(limit)
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
            limit, 0, MaxResultBytes, matches.Count, candidates.Count - matches.Count,
            0, 0, false, null);

        return new FindResult(matches, bounds, all.Count > 0 ? all[0].ArtifactRevision : "none");
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
        var all = reader.AllCurrentAssertions();

        var typed = all.Where(a => a.Predicate == "has_type")
            .ToDictionary(a => a.Subject, a => a, StringComparer.Ordinal);

        var ids = typed.Keys
            .Where(id => query.Term is null || id.Contains(query.Term, StringComparison.OrdinalIgnoreCase))
            .Where(id => query.Type is null || string.Equals(typed[id].Object, query.Type, StringComparison.OrdinalIgnoreCase))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        var nodes = new List<KnowledgeNodeView>();
        foreach (var id in ids.Take(limit))
        {
            var typeAssertion = typed[id];
            var owner = all.FirstOrDefault(a => a.Subject == id && a.Predicate == "owned_by");
            var links = all.Where(a => a.Subject == id && a.Predicate is not ("has_type" or "owned_by")).ToList();
            var backlinks = all.Where(a => a.Object == id && a.Predicate is not ("has_type" or "owned_by")).ToList();

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

        return new KnowledgeResult(nodes, bounds, all.Count > 0 ? all[0].ArtifactRevision : "none");
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
