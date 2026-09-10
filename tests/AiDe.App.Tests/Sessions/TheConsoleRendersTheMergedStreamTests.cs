using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// R16 b1: the Console renders the merged stream with a lane rail and tree filtering, and it is the
/// default on session open.
/// </summary>
/// <remarks>
/// <b>Asserted on the rendered rows, not on the model.</b> The clause's failure — "a two-lane stream
/// renders with no rail attribution" — is a rendering defect, and a model-only assertion is blind to
/// it in exactly the way <c>SurfaceContentTests</c> was written to prevent: the view model was right
/// and the pane showed one property.
/// </remarks>
public sealed class TheConsoleRendersTheMergedStreamTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(20);

    [Fact]
    public async Task ATwoLaneStreamRendersWithRailAttributionOnEveryLine()
    {
        var model = new SessionDocumentViewModel(
            "20260910T120000Z-deadbeef", "Front door", Path.GetTempPath(), [CanvasModeCatalog.ConsoleModeId, CanvasModeCatalog.TerminalModeId]);

        using var first = new RealLane("run-merged", "lane-a");
        using var second = new RealLane("run-merged", "lane-b");
        using var feedA = new SessionLane("lane-a", "claude-code", first.Events, model);
        using var feedB = new SessionLane("lane-b", "codex", second.Events, model);

        first.Say("planning the extraction");
        second.Say("reading the aggregate");
        first.Say("editing");

        Assert.True(await feedA.WaitForDeliveredAsync(2, Bound), $"lane-a delivered {feedA.Delivered} of 2");
        Assert.True(await feedB.WaitForDeliveredAsync(1, Bound), $"lane-b delivered {feedB.Delivered} of 1");

        var rendered = Sta.Run(() =>
        {
            using var console = new ConsoleSurface(model.Console);
            return (Rows: console.RenderedRows.ToList(), Rail: console.RailLanes.ToList());
        });

        // Every rendered line names its lane — a merged stream that reads as one voice is the defect.
        Assert.Equal(3, rendered.Rows.Count);
        Assert.Contains(rendered.Rows, r => r.StartsWith("claude-code: planning the extraction", StringComparison.Ordinal));
        Assert.Contains(rendered.Rows, r => r.StartsWith("codex: reading the aggregate", StringComparison.Ordinal));
        Assert.All(rendered.Rows, r => Assert.Contains(": ", r, StringComparison.Ordinal));

        // And the rail carries both lanes. Ordered before comparing, deliberately: the rail is in
        // first-seen order and two live lanes race to be first, so asserting the order would be
        // asserting a race rather than the clause.
        Assert.Equal(["lane-a", "lane-b"], rendered.Rail.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task AFilterCanExcludeAWholeLane_AndOneKindWithinALane()
    {
        var model = new SessionDocumentViewModel(
            "20260910T120000Z-deadbeef", "Front door", Path.GetTempPath(), [CanvasModeCatalog.ConsoleModeId, CanvasModeCatalog.TerminalModeId]);

        using var first = new RealLane("run-filter", "lane-a");
        using var second = new RealLane("run-filter", "lane-b");
        using var feedA = new SessionLane("lane-a", "claude-code", first.Events, model);
        using var feedB = new SessionLane("lane-b", "codex", second.Events, model);

        first.Say("a message");
        first.AskPermission("Write src/X.cs?");
        second.Say("another lane");

        Assert.True(await feedA.WaitForDeliveredAsync(2, Bound), $"lane-a delivered {feedA.Delivered} of 2");
        Assert.True(await feedB.WaitForDeliveredAsync(1, Bound), $"lane-b delivered {feedB.Delivered} of 1");

        var observed = Sta.Run(() =>
        {
            using var console = new ConsoleSurface(model.Console);

            var all = console.RenderedRows.Count;

            model.Console.SetLaneVisible("lane-b", false);
            var withoutLaneB = console.RenderedRows.ToList();

            model.Console.SetLaneVisible("lane-b", true);
            model.Console.SetKindVisible("lane-a", "permission.request", false);
            var withoutOneKind = console.RenderedRows.ToList();

            // The tree really is a tree: a lane node with its kinds beneath it.
            var tree = model.Console.FilterTree.ToList();

            return (All: all, WithoutLaneB: withoutLaneB, WithoutOneKind: withoutOneKind, Tree: tree);
        });

        Assert.Equal(3, observed.All);

        // A filter can exclude a lane.
        Assert.Equal(2, observed.WithoutLaneB.Count);
        Assert.DoesNotContain(observed.WithoutLaneB, r => r.StartsWith("codex:", StringComparison.Ordinal));

        // And one kind within a lane.
        Assert.Equal(2, observed.WithoutOneKind.Count);
        Assert.DoesNotContain(observed.WithoutOneKind, r => r.Contains("Write src/X.cs?", StringComparison.Ordinal));

        // The excluded rows were hidden, never dropped — the history the ordinal oracle reads is intact.
        Assert.Equal(3, model.Console.Rows.Count);
        Assert.Empty(model.Console.OrdinalGaps("lane-a"));

        Assert.Contains(observed.Tree, n => n.LaneId == "lane-a" && n.Kind is null);
        Assert.Contains(observed.Tree, n => n.LaneId == "lane-a" && n.Kind == "permission.request");
    }

    [Fact]
    public void OpeningASessionLandsOnConsole()
    {
        // "Fails if: opening lands on Terminal." The default is the catalog's first row, so there is
        // no second definition of it to drift.
        var model = new SessionDocumentViewModel(
            "20260910T120000Z-deadbeef", "Front door", Path.GetTempPath(), [CanvasModeCatalog.ConsoleModeId, CanvasModeCatalog.TerminalModeId]);

        Assert.Equal(CanvasModeCatalog.ConsoleModeId, model.ActiveModeId);
        Assert.Equal(CanvasModeCatalog.ConsoleModeId, CanvasModeCatalog.All[0].ModeId);
    }
}
