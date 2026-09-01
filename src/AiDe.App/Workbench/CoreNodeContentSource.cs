using AiDe.Core.Projections;

namespace AiDe.App.Workbench;

/// <summary>
/// The real content source: Core's <c>NodeContentAsync</c>, behind the client seam.
/// </summary>
/// <remarks>
/// <para><b>The substitution the seam was built for.</b> <see cref="MockNodeContentSource"/> was
/// written to stand in "until Core ships <c>NodeContentAsync</c>" — and Core shipped it, after which
/// nothing swapped the field, so the code viewer went on showing a labelled SAMPLE against a fully
/// indexed workspace. A stand-in is only honest while the thing it stands in for is missing; once it
/// arrives, the stand-in is a defect that looks exactly like a feature.</para>
///
/// <para><b>It translates, it does not decide.</b> The render kind and the language come from the
/// authority; this maps Core's enum to the client mirror and nothing else. A client that inferred
/// "this looks like C#" from the id would be a second authority on what a node contains, disagreeing
/// with the first the moment one resolved a path differently (DC-022) — the same reason the App does
/// not read workspace files at all.</para>
///
/// <para><b>An unknown kind degrades to <see cref="NodeContentKind.None"/>.</b> If Core adds a render
/// kind this build has never heard of, the viewer falls back to metadata and edges — which is what it
/// does for a diagram or a binary. Guessing <c>Code</c> would put unhighlighted, possibly binary text
/// in a syntax-highlighted control and claim it was source.</para>
/// </remarks>
public sealed class CoreNodeContentSource(IWorkspaceQueries queries) : INodeContentSource
{
    private readonly IWorkspaceQueries _queries =
        queries ?? throw new ArgumentNullException(nameof(queries));

    public async Task<NodeContent> GetAsync(
        string nodeId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(nodeId);

        var content = await _queries.NodeContentAsync(nodeId, cancellationToken)
            .ConfigureAwait(false);

        return new NodeContent(
            content.NodeId,
            Map(content.RenderKind),
            content.Language,
            content.Content,
            content.Shortfall);
    }

    /// <summary>Core's render kind, as the client mirror of it.</summary>
    private static NodeContentKind Map(Core.Projections.NodeContentKind kind) => kind switch
    {
        Core.Projections.NodeContentKind.Code => NodeContentKind.Code,
        Core.Projections.NodeContentKind.Text => NodeContentKind.Text,

        // Includes None, and anything a later Core adds. See the remarks: falling back to
        // metadata+edges is the honest answer to "this build does not know how to render that".
        _ => NodeContentKind.None,
    };
}
