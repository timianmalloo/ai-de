namespace AiDe.Core.Projections;

/// <summary>
/// How a node's content should be rendered — the authority's call, not the reader's guess.
/// </summary>
/// <remarks>
/// ADR-0018 node-content-reader-contract. The reader branches on this rather than inspecting the content or the node id, so a
/// diagram, a proof or a binary comes back as <see cref="None"/> and gets the metadata-and-edges
/// fallback instead of being mis-rendered as text that happens not to be text.
/// </remarks>
public enum NodeContentKind
{
    /// <summary>Source code. Render read-only, highlighted by <see cref="NodeContent.Language"/>.</summary>
    Code,

    /// <summary>Prose — markdown or plain text.</summary>
    Text,

    /// <summary>No inline content. The reader shows metadata and edges instead.</summary>
    None,
}

/// <summary>
/// One node's content, for a reader that has the node and wants what is behind it.
/// </summary>
/// <param name="NodeId">The node asked for, echoed so a late reply can be matched or discarded.</param>
/// <param name="RenderKind">How to render it.</param>
/// <param name="Language">A highlighting tag — <c>csharp</c>, <c>python</c> — or null.</param>
/// <param name="Content">The text, possibly truncated. Empty when <see cref="RenderKind"/> is None.</param>
/// <param name="Shortfall">
/// What was left out, in words a reader can show. Null when nothing was.
/// </param>
/// <remarks>
/// <para><b>Bounded like every other response.</b> A large file returns the first N bytes and says so,
/// never an oversized frame — the same rule that INV-0003 established for the graph, applied to the
/// one artifact a reader asked for rather than to 1,500 it did not.</para>
///
/// <para><b>Why the authority reads the file and the client does not.</b> The App reading files itself
/// would make two authorities on what a node's content is, and they would disagree the moment one
/// resolved a path differently (DC-022). It would also put file access on the wrong side of the trust
/// boundary: the daemon confines every read to the workspace root, and a client doing its own reading
/// answers to nothing.</para>
/// </remarks>
public sealed record NodeContent(
    string NodeId,
    NodeContentKind RenderKind,
    string? Language,
    string Content,
    string? Shortfall = null);
