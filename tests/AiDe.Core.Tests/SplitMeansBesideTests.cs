using AiDe.Core.Workbench;

namespace AiDe.Core.Tests;

/// <summary>
/// A split drop puts the surface BESIDE its target, not on top of it.
/// </summary>
/// <remarks>
/// <para><b>What was wrong.</b> <c>ZoneBackedLayoutService.Move</c> read the drop's target node and
/// ignored its <c>Kind</c> for everything except <c>Float</c>. So a <c>SplitRight</c> against the
/// Center resolved to "move into the Center" — the surface was tabbed on top of the very thing it
/// had been asked to sit beside.</para>
///
/// <para><b>Why that is worse than not having a policy.</b>
/// <c>DocumentPlacementPolicy</c> computes this placement deliberately, with the comment "split one
/// BESIDE the graph so the graph stays visible". The result was applied and then silently
/// reinterpreted as its opposite — a decision that is computed, honoured in form and inverted in
/// effect. Found by driving the app rather than reading it: the probe showed
/// <c>splitBeside=zone-center</c> followed by a sixth tab in zone-center, on top of the graph.</para>
///
/// <para><b>A zone layout cannot split within a zone</b> — that is what zones are — but it can honour
/// the intent, which is that both surfaces stay visible. Beside the Center is the Right zone; beside
/// anything else is the Center, the largest region and the one no rail collapses.</para>
/// </remarks>
public sealed class SplitMeansBesideTests
{
    private static ZoneBackedLayoutService WithGraphInCentre()
    {
        var service = new ZoneBackedLayoutService();

        // The default layout already puts the canvas in the Center; assert it rather than assume,
        // or every expectation below is about a layout this test invented.
        Assert.Contains(
            service.Current.AllStacks().SelectMany(s => s.Surfaces),
            s => s.Kind == "canvas");

        return service;
    }

    private static string? ZoneOf(ZoneBackedLayoutService service, string surfaceId)
    {
        foreach (var stack in service.Current.AllStacks())
        {
            if (stack.Surfaces.Any(s => s.SurfaceId == surfaceId))
            {
                return stack.Id;
            }
        }

        return null;
    }

    private static string GraphStackId(ZoneBackedLayoutService service) =>
        service.Current.AllStacks().First(s => s.Surfaces.Any(x => x.Kind == "canvas")).Id;

    [Fact]
    public void ASplitAgainstTheGraphDoesNotLandInTheGraphsOwnStack()
    {
        // THE DEFECT, as the user met it: a code viewer opened over the graph rather than beside it.
        var service = WithGraphInCentre();
        var graphStack = GraphStackId(service);

        var doc = new Surface("codeviewer#1", "codeviewer", "Source");
        Assert.True(service.Apply(new LayoutOperation.AddSurface(graphStack, doc)).Applied);

        service.Apply(new LayoutOperation.MoveSurface(
            doc.SurfaceId, new DropTarget(graphStack, DropKind.SplitRight)));

        Assert.NotEqual(graphStack, ZoneOf(service, doc.SurfaceId));
    }

    [Fact]
    public void TheGraphStaysWhereItWas()
    {
        // Beside means the OTHER surface does not move. A "split" that relocated the graph instead
        // would satisfy the assertion above and still lose the thing the policy protects.
        var service = WithGraphInCentre();
        var graphStack = GraphStackId(service);
        var canvas = service.Current.AllStacks()
            .SelectMany(s => s.Surfaces).First(s => s.Kind == "canvas");

        var doc = new Surface("codeviewer#2", "codeviewer", "Source");
        service.Apply(new LayoutOperation.AddSurface(graphStack, doc));
        service.Apply(new LayoutOperation.MoveSurface(
            doc.SurfaceId, new DropTarget(graphStack, DropKind.SplitRight)));

        Assert.Equal(graphStack, ZoneOf(service, canvas.SurfaceId));
    }

    [Fact]
    public void AJoinStackDropStillTabsIn()
    {
        // The other half. "Split" and "join" are different requests, and honouring the first must not
        // quietly redirect the second — a user dragging onto a tab strip is asking to tab.
        var service = WithGraphInCentre();
        var graphStack = GraphStackId(service);

        var doc = new Surface("codeviewer#3", "codeviewer", "Source");
        service.Apply(new LayoutOperation.AddSurface(graphStack, doc));
        service.Apply(new LayoutOperation.MoveSurface(
            doc.SurfaceId, new DropTarget(graphStack, DropKind.JoinStack)));

        Assert.Equal(graphStack, ZoneOf(service, doc.SurfaceId));
    }

    [Fact]
    public void TheSurfaceIsNeverLost()
    {
        // The invariant the old fallback existed to protect, kept. Whatever a drop means, the surface
        // is somewhere afterwards — a placement rule that could drop a pane on the floor would be a
        // worse defect than the one being fixed.
        var service = WithGraphInCentre();
        var graphStack = GraphStackId(service);

        foreach (var kind in new[]
                 { DropKind.SplitLeft, DropKind.SplitRight, DropKind.SplitTop, DropKind.SplitBottom })
        {
            var doc = new Surface($"doc#{kind}", "codeviewer", "Source");
            service.Apply(new LayoutOperation.AddSurface(graphStack, doc));
            service.Apply(new LayoutOperation.MoveSurface(
                doc.SurfaceId, new DropTarget(graphStack, kind)));

            Assert.NotNull(ZoneOf(service, doc.SurfaceId));
        }
    }
}
