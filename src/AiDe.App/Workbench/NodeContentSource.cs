namespace AiDe.App.Workbench;

/// <summary>
/// How a node's content should be rendered (the client mirror of ADR-0018 node-content-reader-contract's RenderKind). The authority
/// (Core's future <c>NodeContentAsync</c>) decides this, so the viewer's per-kind branch is data, not a
/// client guess.
/// </summary>
public enum NodeContentKind
{
    /// <summary>Source code — render read-only with syntax highlighting (US-ED1/ED2).</summary>
    Code,

    /// <summary>Markdown/plain prose — render as text (rich markdown is a later step).</summary>
    Text,

    /// <summary>No inline content (a diagram/proof/binary node) — the reader falls back to metadata+edges.</summary>
    None,
}

/// <summary>
/// One node's content for the reader/viewer — the client mirror of ADR-0018 node-content-reader-contract's <c>NodeContent</c>. Bounded:
/// oversized content returns a <see cref="Shortfall"/> ("first N — open the source"), never an oversized
/// frame. <see cref="Language"/> is the authority's language tag (e.g. "csharp"), used to pick highlighting.
/// </summary>
public sealed record NodeContent(
    string NodeId,
    NodeContentKind RenderKind,
    string? Language,
    string Content,
    string? Shortfall = null);

/// <summary>
/// The client seam the reader uses to fetch a selected node's content on demand (ADR-0018 node-content-reader-contract). Core's
/// future <c>IWorkspaceQueries.NodeContentAsync</c> is the real implementation; until it ships,
/// <see cref="MockNodeContentSource"/> stands in behind this interface so the viewer is buildable and
/// testable and the eventual wiring is a one-line substitution, not a redesign.
/// </summary>
public interface INodeContentSource
{
    Task<NodeContent> GetAsync(string nodeId, CancellationToken cancellationToken = default);
}

/// <summary>
/// A stand-in content source until Core ships <c>NodeContentAsync</c> (ADR-0018 node-content-reader-contract Phase 1). It returns a
/// clearly-labelled SAMPLE — it does NOT read files (the App is not a second file-content authority,
/// DC-022) — so the viewer can render and be tested end-to-end while the real query is built.
/// </summary>
public sealed class MockNodeContentSource : INodeContentSource
{
    public Task<NodeContent> GetAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        var sample =
            "// SAMPLE — awaiting Core's NodeContentAsync query (ADR-0018 node-content-reader-contract).\n" +
            "// The real source for '" + nodeId + "' will render here read-only.\n" +
            "namespace Sample;\n\n" +
            "public sealed class Placeholder\n{\n" +
            "    public string NodeId { get; } = \"" + nodeId + "\";\n" +
            "}\n";

        return Task.FromResult(new NodeContent(nodeId, NodeContentKind.Code, "csharp", sample));
    }
}
