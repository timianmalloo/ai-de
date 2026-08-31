using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// A reference document (class diagram, code viewer) must never tab on top of the graph — that hides
/// the graph the user works from (the "graph pane disappeared" defect). These pin the placement policy
/// and the resulting layout, headlessly (the policy and the layout model are both pure).
/// </summary>
public sealed class DocumentPlacementTests
{
    [Fact]
    public void Decide_SplitsBesideTheGraph_WhenFocusedOnTheGraphAndNoDocumentStackExists()
    {
        var placement = DocumentPlacementPolicy.Decide(Layout.Default(), "graph");

        Assert.NotNull(placement);
        Assert.Null(placement!.TabIntoStackId);          // NOT a tab onto the graph
        Assert.Equal("stack-graph", placement.SplitBesideStackId);
        Assert.True(placement.IsSplit);
    }

    [Fact]
    public void OpeningAClassDiagram_KeepsTheGraphVisibleInItsOwnStack()
    {
        var service = new LayoutService(Layout.Default());
        var placement = DocumentPlacementPolicy.Decide(service.Current, "graph")!;
        var surface = new Surface("classdiagram#1", "classdiagram", "Class diagram");

        // Execute the split-beside-graph placement the shell would apply.
        service.Apply(new LayoutOperation.AddSurface(placement.SplitBesideStackId!, surface));
        service.Apply(new LayoutOperation.MoveSurface(
            surface.SurfaceId, new DropTarget(placement.SplitBesideStackId!, DropKind.SplitRight)));

        var graphStack = service.Current.AllStacks().First(s => s.Surfaces.Any(su => su.Kind == "canvas"));
        var diagramStack = service.Current.AllStacks().First(s => s.Surfaces.Any(su => su.SurfaceId == "classdiagram#1"));

        Assert.NotEqual(graphStack.Id, diagramStack.Id);                       // the diagram did NOT tab onto the graph
        Assert.Contains(graphStack.Surfaces, su => su.SurfaceId == "graph");   // the graph tab still exists, in its own stack
        Assert.DoesNotContain(graphStack.Surfaces, su => su.SurfaceId == "classdiagram#1");
    }

    [Fact]
    public void Decide_TabsIntoTheExistingDocumentStack_ForASecondDocument()
    {
        var service = new LayoutService(Layout.Default());
        var first = DocumentPlacementPolicy.Decide(service.Current, "graph")!;
        var s1 = new Surface("classdiagram#1", "classdiagram", "Class diagram");
        service.Apply(new LayoutOperation.AddSurface(first.SplitBesideStackId!, s1));
        service.Apply(new LayoutOperation.MoveSurface(
            s1.SurfaceId, new DropTarget(first.SplitBesideStackId!, DropKind.SplitRight)));

        // A second document, still focused on the graph, tabs into the document stack — not another split.
        var second = DocumentPlacementPolicy.Decide(service.Current, "graph");

        Assert.NotNull(second);
        Assert.NotNull(second!.TabIntoStackId);
        Assert.Null(second.SplitBesideStackId);
        var docStack = service.Current.AllStacks().First(s => s.Surfaces.Any(su => su.SurfaceId == "classdiagram#1"));
        Assert.Equal(docStack.Id, second.TabIntoStackId);
    }

    [Fact]
    public void Decide_HonoursTheFocusedDocumentStack()
    {
        var service = new LayoutService(Layout.Default());
        var first = DocumentPlacementPolicy.Decide(service.Current, "graph")!;
        var s1 = new Surface("codeviewer#1", "codeviewer", "Source");
        service.Apply(new LayoutOperation.AddSurface(first.SplitBesideStackId!, s1));
        service.Apply(new LayoutOperation.MoveSurface(
            s1.SurfaceId, new DropTarget(first.SplitBesideStackId!, DropKind.SplitRight)));

        // Focused ON the document — the next document tabs in beside it.
        var next = DocumentPlacementPolicy.Decide(service.Current, "codeviewer#1");
        var docStack = service.Current.AllStacks().First(s => s.Surfaces.Any(su => su.SurfaceId == "codeviewer#1"));

        Assert.Equal(docStack.Id, next!.TabIntoStackId);
    }
}

/// <summary>The workbench had no tracing; these pin that layout mutations now emit a readable record.</summary>
public sealed class WorkbenchDiagnosticsTests
{
    [Fact]
    public void LayoutMutation_EmitsAStructuredRecord_WithTheOperationPlacementAndTopology()
    {
        var captured = new List<string>();
        WorkbenchDiagnostics.Sink = captured.Add;
        try
        {
            var service = new LayoutService(Layout.Default());
            var surface = new Surface("classdiagram#1", "classdiagram", "Class diagram");
            service.Apply(new LayoutOperation.AddSurface("stack-graph", surface));

            WorkbenchDiagnostics.LayoutMutation(
                "open-classdiagram", "split-beside-graph", surface.SurfaceId, "graph", service.Current);

            var line = Assert.Single(captured);
            Assert.Contains("\"operation\":\"open-classdiagram\"", line);
            Assert.Contains("\"placement\":\"split-beside-graph\"", line);
            Assert.Contains("classdiagram#1", line);
            Assert.Contains("stack-graph", line);   // the topology is recorded
        }
        finally
        {
            WorkbenchDiagnostics.Sink = null;
        }
    }
}
