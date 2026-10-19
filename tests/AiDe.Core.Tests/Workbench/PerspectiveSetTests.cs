using AiDe.Core.Workbench;

namespace AiDe.Core.Tests.Workbench;

/// <summary>
/// ADR-0030 rule 1's build test <b>as superseded by ADR-0045 §1</b>: the Perspective set is a closed
/// Core row set — exactly three rows in the order Factory · Review · Understand, every one a docking
/// host, each with a landing zone, a bound single-stroke gesture spelled from its rail digit, and a
/// catalog command derived from the row — and the routing order for an inadmissible kind-open is
/// Understand · Review · Factory ("the reading host wins a shared kind").
/// </summary>
/// <remarks>
/// Seen red against the four-row set (Coding · Explore · Architecture · Coordination) before the
/// re-row: <c>docs/proof/workbench-loops-wl-s/sp3/three-rows-red.txt</c> and
/// <c>no-fullwindow-red.txt</c>. The assertions read ids and titles, never the row properties, so
/// they compile against both sets and fail on the data.
/// </remarks>
public sealed class PerspectiveSetTests
{
    private static Perspective Row(string id) => Assert.Single(PerspectiveSet.All, p => p.Id == id);

    // ADR-0045 §1(a): exactly three, in the spec's order, each with a bound gesture spelled from its
    // Order and carried by its catalog command; a FOURTH entry is the falsifier. Tests is reserved
    // with no row (Ruling 54).
    [Fact]
    public void All_HasExactlyThreeRows_FactoryReviewUnderstand_EachWithABoundGesture()
    {
        Assert.Equal(["factory", "review", "understand"], PerspectiveSet.All.Select(p => p.Id));
        Assert.Equal(["Factory", "Review", "Understand"], PerspectiveSet.All.Select(p => p.Title));
        Assert.Equal([1, 2, 3], PerspectiveSet.All.Select(p => p.Order));
        Assert.Equal(["Ctrl+1", "Ctrl+2", "Ctrl+3"], PerspectiveSet.All.Select(p => p.Gesture));
        Assert.Equal(["perspective.factory", "perspective.review", "perspective.understand"], PerspectiveSet.All.Select(p => p.CommandId));
        Assert.DoesNotContain(PerspectiveSet.All, p => p.Id.Contains("test", StringComparison.OrdinalIgnoreCase));
        Assert.Same(Row("factory"), PerspectiveSet.Initial);

        foreach (var perspective in PerspectiveSet.All)
        {
            // The gesture is bound, not merely printed: the catalog row derived from this one carries
            // it verbatim, and `KeyGestures.For` (App) binds Ctrl+<Order> from the same digit.
            var command = Assert.Single(WorkbenchCommandCatalog.All, c => c.Id == perspective.CommandId);
            Assert.Equal($"Ctrl+{perspective.Order}", command.Gesture);
        }
    }

    // ADR-0045 §1(a) landings, with the design's D2: Factory lands on the Left (the session document,
    // Ruling 83) until T11 docks the notebook in the Center and flips this value in the same change;
    // Review lands on its Center (the change packet's zone); Understand on its Center (the visualizer).
    [Fact]
    public void EachRowHasItsOwnLandingZone_FactoryLeftUntilTheNotebook_ReviewAndUnderstandCenter()
    {
        Assert.Equal([ZoneId.Left, ZoneId.Center, ZoneId.Center], PerspectiveSet.All.Select(p => p.Landing));
    }

    // ADR-0045 §1(b): no row keeps a full-window body, and none can. The WL-S design's D5 deletes the
    // one-value Body column, so every row is a docking host by construction; this test is what fails
    // if a full-window body — the column, its enum, or the word in the source — comes back.
    [Fact]
    public void NoRowUsesFullWindow_TheBodyColumnIsGone()
    {
        Assert.Null(typeof(Perspective).Assembly.GetType("AiDe.Core.Workbench.PerspectiveBody"));
        Assert.Null(typeof(Perspective).GetProperty("Body"));

        var src = Path.Combine(RepoRoot(), "src");
        var offenders = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            .Where(f => File.ReadAllText(f).Contains("FullWindow", StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(src, f))
            .ToList();
        Assert.Empty(offenders);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("AiDe.sln was not found above the test directory");
    }

    // ADR-0030 rule 1: the perspective commands are DERIVED into the catalog from the rows, so the
    // catalog cannot list a perspective the set lacks — and `shell.toggleExplorer` is gone.
    [Fact]
    public void EveryRowHasACatalogCommand_DerivedFromIt_AndTheToggleIsGone()
    {
        foreach (var perspective in PerspectiveSet.All)
        {
            var command = Assert.Single(WorkbenchCommandCatalog.All, c => c.Id == perspective.CommandId);

            Assert.EndsWith(" perspective", command.Title, StringComparison.Ordinal);   // the accessible name ends in "perspective" (US-C1)
            Assert.Equal("_View", command.Menu);                                        // the View menu's radio group (§B3 rule 1)
            Assert.Equal(CommandScope.Global, command.Scope);                           // offered in every perspective
            Assert.Same(perspective, PerspectiveSet.ByCommandId(command.Id));
        }

        Assert.Equal(
            PerspectiveSet.All.Count,
            WorkbenchCommandCatalog.All.Count(c => c.Id.StartsWith("perspective.", StringComparison.Ordinal)));
        Assert.DoesNotContain(WorkbenchCommandCatalog.All, c => c.Id == "shell.toggleExplorer");
    }

    // US-C10: Ctrl+1/2/3, spelled from the rail digit so the string and the binding cannot disagree;
    // no other catalog command announces a digit gesture (Ctrl+4 is unbound after the re-row).
    [Fact]
    public void ThePerspectiveGesturesAreCtrlPlusTheRailDigit_AndNothingElseUsesThem()
    {
        var digitGestures = WorkbenchCommandCatalog.All
            .Where(c => c.Gesture.Length == 6 && c.Gesture.StartsWith("Ctrl+", StringComparison.Ordinal) && char.IsDigit(c.Gesture[^1]))
            .Select(c => c.Id)
            .ToList();

        Assert.Equal(PerspectiveSet.All.Select(p => p.CommandId), digitGestures);
        Assert.DoesNotContain(WorkbenchCommandCatalog.All, c => c.Gesture == "Ctrl+4");
    }

    // US-C3 under ADR-0045 §1(a): the reading host wins a shared kind, so Understand is tried first,
    // then Review, then Factory — every row is a routing target.
    [Fact]
    public void TheRoutingOrderIsUnderstandThenReviewThenFactory()
    {
        Assert.Equal(["understand", "review", "factory"], PerspectiveSet.RoutingOrder.Select(p => p.Id));
        Assert.Equal(PerspectiveSet.All.Count, PerspectiveSet.RoutingOrder.Distinct().Count());
    }

    [Fact]
    public void TheCommandLookupReturnsNullForAnUnknownId_NotAThrowOrADefault()
    {
        Assert.Null(PerspectiveSet.ByCommandId("perspective.tests"));
        Assert.Null(PerspectiveSet.ByCommandId("perspective.coding"));
        Assert.Null(PerspectiveSet.ByCommandId(string.Empty));
        Assert.Same(Row("understand"), PerspectiveSet.ByCommandId("perspective.understand"));
    }
}
