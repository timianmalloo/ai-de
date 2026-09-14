using System.Collections.Generic;

namespace AiDe.App.Workbench;

/// <summary>What a right-click in Explore's graph offers for a node (Ruling 93).</summary>
public enum ExplorerNodeAction
{
    /// <summary>Select the node and render its content in the reader's content area.</summary>
    ViewSource,

    /// <summary>Select the node: the reader shows its metadata and typed edges.</summary>
    MetadataAndEdges,
}

/// <summary>One choice of Explore's node menu — the action and its label.</summary>
public sealed record ExplorerNodeOption(ExplorerNodeAction Action, string Label);

/// <summary>
/// Explore's node context menu (Ruling 93): <b>View source</b> first, then the reading gesture the
/// reader already answers. Every action stays in Explore — a reading act on the current pane —
/// never a routed kind-open (those are the raising host's <c>Open as…</c> grammar, Rulings 58/59,
/// frozen). Pure, so the order is asserted without a window.
/// </summary>
public static class ExplorerNodeMenu
{
    public static IReadOnlyList<ExplorerNodeOption> OptionsFor(NodeContextMenuRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new List<ExplorerNodeOption>
        {
            new(ExplorerNodeAction.ViewSource, "View source"),
            new(ExplorerNodeAction.MetadataAndEdges, "Metadata & edges"),
        };
    }
}
