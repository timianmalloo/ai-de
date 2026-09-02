using System;
using System.Linq;
using System.Text.Json;
using AiDe.App.Workbench;
using Xunit;

namespace AiDe.App.Tests;

/// <summary>
/// The JS→C# wire contract for the node right-click viewer menu (smoke 9-1 §3, T-W4). The graph page
/// posts <c>{ kind:'node.contextmenu', nodeId, nodeKind, isKnowledge }</c>; <see cref="CanvasSurface"/>
/// deserializes it into <c>CanvasMessage</c> and raises <c>NodeContextMenuRequested</c>, which the
/// shell turns into <see cref="NodeViewMenu"/> options and dispatches to a viewer.
/// </summary>
/// <remarks>
/// <para>This exists because the drift that breaks the menu is invisible to every within-layer test:
/// if the JS property names and the C# record names disagree, the payload still deserializes, but
/// <c>NodeKind</c>/<c>IsKnowledge</c> come back null/false and the menu silently offers the wrong
/// viewers (E2E-A: a field reaches one layer and is missing at the wire; E12: prove the surfaces agree
/// with each other). The live WebView2 hop cannot be driven headlessly, so this pins the two ends of
/// it — the exact JSON the page posts, and the real record it lands in — as one chain.</para>
/// </remarks>
public sealed class NodeContextMenuWireContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void TheGraphPage_PostsTheContextMenuMessage_WithNodeKindAndKnowledge()
    {
        // The JS half of the seam: the page must attach the contextmenu listener and post the fields
        // the record reads. Remove the listener or a field and the menu never opens (or opens blind).
        var js = CanvasPage.Html;

        Assert.Contains("'contextmenu'", js, StringComparison.Ordinal);
        Assert.Contains("node.contextmenu", js, StringComparison.Ordinal);
        Assert.Contains("nodeKind", js, StringComparison.Ordinal);
        Assert.Contains("isKnowledge", js, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("method", false, NodeViewKind.Sequence)]      // right-click a method -> a sequence diagram
    [InlineData("class", false, NodeViewKind.ClassDiagram)]   // right-click a type   -> a class diagram
    [InlineData("spec", true, NodeViewKind.Read)]             // a document           -> read (IsKnowledge wins)
    public void TheExactPagePayload_DeserializesIntoTheRecord_AndDrivesTheRightViewer(
        string nodeKind, bool isKnowledge, NodeViewKind expected)
    {
        // The verbatim shape CanvasPage.Html posts: { kind, nodeId, nodeKind, isKnowledge }.
        var payload = JsonSerializer.Serialize(
            new { kind = "node.contextmenu", nodeId = "N", nodeKind, isKnowledge });

        var message = JsonSerializer.Deserialize<CanvasSurface.CanvasMessage>(payload, Web);

        Assert.NotNull(message);
        Assert.Equal("node.contextmenu", message!.Kind);
        Assert.Equal("N", message.NodeId);
        Assert.Equal(nodeKind, message.NodeKind);          // the field that must survive the wire
        Assert.Equal(isKnowledge, message.IsKnowledge);    // the authoritative flag (DC-042)

        // …and the fields that survived pick the right viewer grammar the shell will show.
        var kinds = NodeViewMenu.OptionsFor(message.NodeKind, message.IsKnowledge).Select(o => o.Kind);
        Assert.Contains(expected, kinds);
    }
}
