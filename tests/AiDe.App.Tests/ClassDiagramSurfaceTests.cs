using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Presentation;

namespace AiDe.App.Tests;

/// <summary>
/// The class-diagram surface (ADR-0020): renders a hierarchy built from graph nodes/edges. Host-side
/// WPF, so it runs on an STA thread with no WebView2.
/// </summary>
public sealed class ClassDiagramSurfaceTests
{
    private static void OnSta(Action work)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { work(); } catch (Exception ex) { failure = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "STA thread did not finish");
        if (failure is not null) { throw new InvalidOperationException("STA work failed", failure); }
    }

    private static CanvasNode N(string id, string kind) => new(id, id.Split('.')[^1], kind, false, null);
    private static CanvasEdge Edge(string from, string to, string pred) => new(from, to, pred, "Verified");

    [Fact]
    public void ShowGraph_RendersTheTypeHierarchy()
    {
        OnSta(() =>
        {
            var s = new ClassDiagramSurface();
            Assert.True(s.IsEmpty);

            s.ShowGraph(
                new[] { N("Shop.Order", "class"), N("Shop.IRepo", "interface"), N("Shop.Orders", "table") },
                new[] { Edge("Shop.Order", "Shop.IRepo", "implements") });

            Assert.Equal(2, s.TypeCount);      // table excluded
            Assert.Equal(1, s.RelationCount);  // one realization
            Assert.False(s.IsEmpty);
        });
    }

    [Fact]
    public void ShowGraph_EmptyWhenNoTypes_AndClearReturnsToEmpty()
    {
        OnSta(() =>
        {
            var s = new ClassDiagramSurface();
            s.ShowGraph(new[] { N("Shop.Orders", "table") }, []);
            Assert.True(s.IsEmpty);

            s.ShowGraph(new[] { N("A", "class"), N("B", "class") }, new[] { Edge("A", "B", "inherits") });
            Assert.Equal(2, s.TypeCount);
            Assert.Equal(1, s.RelationCount);

            s.Clear();
            Assert.True(s.IsEmpty);
            Assert.Equal(0, s.RelationCount);
        });
    }

    [Fact]
    public void ShowError_AfterPopulated_ClearsTypes_NotAMisleadingEmpty()
    {
        OnSta(() =>
        {
            var s = new ClassDiagramSurface();
            s.ShowGraph(new[] { N("A", "class"), N("B", "class") }, new[] { Edge("A", "B", "inherits") });
            Assert.False(s.IsEmpty);

            s.ShowError("daemon closed the connection");
            Assert.True(s.IsEmpty);            // no types claimed after a failed load
            Assert.Equal(0, s.RelationCount);

            s.ShowLoading();                   // does not throw
        });
    }
}
