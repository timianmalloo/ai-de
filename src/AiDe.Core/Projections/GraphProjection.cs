using AiDe.Core.Facts;

namespace AiDe.Core.Projections;

/// <summary>One node of the whole graph, with how connected it is.</summary>
/// <param name="Degree">
/// Edges touching this node. Carried because the surface has to choose what to draw, and choosing
/// without it means choosing alphabetically — which is how a graph of two thousand nodes gets
/// rendered as whichever two happened to sort first.
/// </param>
/// <param name="IsExternal">
/// True when nothing in this workspace DECLARES the node — a framework or package type that is only
/// ever pointed at.
/// </param>
/// <remarks>
/// <b>Without this the graph is mostly not the user's code.</b> Measured on a real repository: the
/// six most-connected nodes were <c>string</c>, <c>int</c>, <c>Task&lt;TResult&gt;</c>,
/// <c>DateTimeOffset</c>, <c>IReadOnlyList&lt;T&gt;</c> and <c>Guid</c> — 773 edges to
/// <c>string</c> alone. Ranking by raw degree therefore puts the BCL at the centre of a picture of
/// somebody's domain, and capping by raw degree drops their code to keep it.
/// </remarks>
public sealed record GraphNode(string Id, string Label, string Kind, int Degree, bool IsExternal);

/// <summary>One relationship, with the status of the evidence behind it.</summary>
public sealed record GraphEdge(string From, string To, string Predicate, VerificationStatus Status);

/// <summary>
/// Which part of the graph to build.
/// </summary>
/// <param name="MaxNodes">The cap. What it drops is counted and reported, never silent.</param>
/// <param name="Kinds">
/// Keep only nodes of these kinds (the values <c>has_type</c> carries — <c>class</c>,
/// <c>python-module</c>, <c>table</c>). Null or empty means every kind.
/// </param>
/// <param name="ScopeId">Keep only nodes this scope declares. Null means every scope.</param>
/// <param name="GroupId">
/// Keep only the nodes inside one overview group — the drill-down from a cluster to its contents.
/// </param>
/// <param name="IncludeExternal">
/// Whether to keep nodes nothing in the workspace declares — framework and package types.
/// </param>
/// <remarks>
/// <para><b>Why filtering belongs HERE and not at the caller.</b> A tool that wants the domain model
/// would otherwise fetch 2,813 nodes across a pipe and discard nine tenths of them, and — worse —
/// the CAP would have already chosen which nodes to send, by a ranking computed over a graph the
/// caller did not want. Filtering after a cap gives you the wrong 5,000 nodes trimmed to the right
/// kind, and nothing in the result says so.</para>
///
/// <para>So the filter runs BEFORE the cap, degree is computed over what survives it, and "most
/// connected" means most connected <em>in the graph that was asked for</em>.</para>
///
/// <para><b>The group filter takes no depth, on purpose.</b> A group id already states its own depth
/// — <c>TheTerrace.Features</c> is two segments and <c>src/app</c> is two — so the depth is read back
/// out of the id rather than passed alongside it. A separate parameter would let a caller ask for
/// <c>TheTerrace.Features</c> at depth 3 and receive nothing, with no error and no way to tell that
/// from an empty group.</para>
/// </remarks>
public sealed record GraphQuery(
    int MaxNodes = GraphProjection.DefaultMaxNodes,
    IReadOnlyList<string>? Kinds = null,
    string? ScopeId = null,
    bool IncludeExternal = true,
    string? GroupId = null);

/// <summary>The whole graph, and what it left out.</summary>
/// <param name="Omitted">Nodes present in the evidence and not returned, because a cap applied.</param>
/// <param name="Disclosures">What the extraction said it could not see, lifted out of the edges.</param>
public sealed record WorkspaceGraph(
    IReadOnlyList<GraphNode> Nodes,
    IReadOnlyList<GraphEdge> Edges,
    int Omitted,
    IReadOnlyList<string> Disclosures,
    string SourceRevision);

/// <summary>
/// The whole workspace as a graph, rather than one node and its neighbours.
/// </summary>
/// <remarks>
/// <para><b>The graph surface had never shown the graph.</b> It asked for one node
/// (<c>FindAsync("", 1)</c>) and then that node's neighbours, so a workspace of 12,100 assertions and
/// 2,164 nodes rendered as <b>two</b> — the alphabetically first symbol and its single neighbour.
/// Reported by the user, comparing it against the same repository in Obsidian.</para>
///
/// <para><b>Attributes are not edges.</b> <c>has_type</c>, <c>declared_in</c>, <c>api_version</c> and
/// the rest describe a node; drawing them puts the string "class" in the graph as a thing that other
/// things point at. The same rule the search already applies, applied here — one definition, in
/// <see cref="EvidencePredicates.Attributes"/>, used by both.</para>
///
/// <para><b>Bounded by DEGREE, not by name.</b> When a cap applies, the nodes kept are the ones the
/// graph is actually about: an alphabetical cut of a two-thousand-node graph is arbitrary, and looks
/// exactly like a complete small graph. What was dropped is counted, so a partial view can say so.</para>
/// </remarks>
public sealed class GraphProjection(IReadOnlyList<EvidenceAssertion> assertions, string sourceRevision)
{
    /// <summary>
    /// Nodes returned before the cap applies.
    /// </summary>
    /// <remarks>
    /// Large enough that no repository measured so far reaches it, so the common case is the WHOLE
    /// graph. A cap exists because a surface that receives ten million nodes stops responding, and a
    /// pane that stops responding tells the user nothing at all.
    /// </remarks>
    public const int DefaultMaxNodes = 5_000;

    public WorkspaceGraph Compute(int maxNodes = DefaultMaxNodes) =>
        Compute(new GraphQuery(maxNodes));

    public WorkspaceGraph Compute(GraphQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var disclosures = new List<string>();

        // Declared HERE: something in this workspace says what it is or where it lives. Everything
        // else is a name this code refers to and does not contain.
        var declared = assertions
            .Where(a => a.Predicate is "has_type" or "declared_in")
            .Select(a => a.Subject)
            .ToHashSet(StringComparer.Ordinal);

        var kinds = new Dictionary<string, string>(StringComparer.Ordinal);
        var declaredIn = new Dictionary<string, string>(StringComparer.Ordinal);
        var mentioned = new HashSet<string>(StringComparer.Ordinal);
        var candidateEdges = new List<GraphEdge>();

        foreach (var assertion in assertions)
        {
            if (assertion.Predicate == "discloses")
            {
                disclosures.Add(assertion.Object);
                continue;
            }

            // An attribute describes its subject. `has_type` gives the node its kind; the others are
            // recorded as facts and are not relationships between two things.
            if (EvidencePredicates.Attributes.Contains(assertion.Predicate))
            {
                if (assertion.Predicate == "has_type")
                {
                    kinds[assertion.Subject] = assertion.Object;
                }
                else if (assertion.Predicate == "declared_in")
                {
                    declaredIn[assertion.Subject] = assertion.Object;
                }

                mentioned.Add(assertion.Subject);
                continue;
            }

            mentioned.Add(assertion.Subject);
            mentioned.Add(assertion.Object);

            candidateEdges.Add(new GraphEdge(
                assertion.Subject, assertion.Object, assertion.Predicate, assertion.Status));
        }

        // The FILTER, applied before the cap. Filtering afterwards would rank and trim the whole
        // graph and only then discard, so the caller would receive the wrong nodes of the right kind.
        var wanted = query.Kinds is { Count: > 0 }
            ? query.Kinds.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : null;

        var included = mentioned
            .Where(id => query.IncludeExternal || declared.Contains(id))
            .Where(id => wanted is null || wanted.Contains(kinds.GetValueOrDefault(id, "external")))
            .Where(id => query.ScopeId is null
                || string.Equals(declaredIn.GetValueOrDefault(id), query.ScopeId, StringComparison.Ordinal))
            .Where(id => query.GroupId is null || InGroup(id, query.GroupId))
            .ToHashSet(StringComparer.Ordinal);

        // Degree is counted over the SURVIVING edges, so "most connected" means most connected in
        // the graph that was asked for rather than in one the caller filtered away.
        var edges = candidateEdges
            .Where(e => included.Contains(e.From) && included.Contains(e.To))
            .ToList();

        var degree = included.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);

        foreach (var edge in edges)
        {
            degree[edge.From]++;
            degree[edge.To]++;
        }

        // DECLARED first, then by degree. A node this workspace declares is part of the thing being
        // looked at; a node it only points at is context. Ordering by degree alone put `string` at
        // the top and dropped the user's own types at the cap.
        var ordered = degree
            .OrderByDescending(kv => declared.Contains(kv.Key))
            .ThenByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .ToList();

        var nodes = ordered.Take(query.MaxNodes).ToList();
        var kept = nodes.Select(kv => kv.Key).ToHashSet(StringComparer.Ordinal);
        var omitted = Math.Max(0, ordered.Count - kept.Count);

        // An edge whose other end was dropped is dropped with it. Drawing a half-edge into nothing
        // is worse than omitting it: it looks like a node the layout failed to place.
        var visible = edges
            .Where(e => kept.Contains(e.From) && kept.Contains(e.To))
            .ToList();

        return new WorkspaceGraph(
            [.. nodes.Select(kv => new GraphNode(
                kv.Key, Label(kv.Key), kinds.GetValueOrDefault(kv.Key, "external"), kv.Value,
                IsExternal: !declared.Contains(kv.Key)))],
            visible,
            omitted,
            [.. disclosures.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)],
            sourceRevision);
    }

    /// <summary>
    /// Whether a node belongs to the overview group with this id.
    /// </summary>
    /// <remarks>
    /// The depth comes from the GROUP's own shape, so a drill-down cannot ask the wrong question:
    /// grouping <paramref name="id"/> to the same number of segments the group id has must produce
    /// the group id itself. Delegating to <see cref="GraphOverview.GroupFor"/> keeps one definition
    /// of "which group is this in" — two would put a node in a cluster the overview does not have.
    /// </remarks>
    private static bool InGroup(string id, string groupId)
    {
        var separator = groupId.Contains('/', StringComparison.Ordinal) ? '/' : '.';
        var depth = groupId.Contains('#', StringComparison.Ordinal)
            ? 1
            : groupId.Split(separator).Length;

        return string.Equals(GraphOverview.GroupFor(id, depth), groupId, StringComparison.Ordinal);
    }

    /// <summary>
    /// The short name a reader recognises, keeping the scope prefix where there is one.
    /// </summary>
    /// <remarks>
    /// A fully-qualified name is unreadable at graph scale and a bare last segment is ambiguous, so
    /// this keeps <c>bicep:main#siteName</c> whole and shortens a dotted symbol to its last part.
    /// </remarks>
    private static string Label(string id)
    {
        if (id.Contains(':', StringComparison.Ordinal)) return id;

        var cut = id.LastIndexOf('.');
        return cut > 0 && cut < id.Length - 1 ? id[(cut + 1)..] : id;
    }
}
