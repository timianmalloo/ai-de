using AiDe.Core.Workbench;

namespace AiDe.Core.Tests.Workbench;

/// <summary>
/// ADR-0030 rule 1 / falsifying test 1: the Perspective set is a closed Core row set — three rows in
/// the order Coding · Explore · Architecture, each with a body and a catalog command derived from
/// the row — and the routing order for an inadmissible kind-open is Architecture · Coding.
/// </summary>
/// <remarks>
/// These are data invariants, so each was seen red by MUTATION before it was trusted (the record,
/// with the mutation ids, is <c>docs/proof/perspective-registry.md</c>): a fourth row appended; the
/// Explore row given a <c>DockHost</c> body; a perspective's command filtered out of the catalog's
/// derivation; the routing order reversed.
/// </remarks>
public sealed class PerspectiveSetTests
{
    // US-C1: exactly three, in rail order; a fourth entry is the falsifier. Tests is reserved with no row.
    [Fact]
    public void TheSetHasExactlyThreeRows_InTheOrderCodingExploreArchitecture()
    {
        Assert.Equal(["coding", "explore", "architecture"], PerspectiveSet.All.Select(p => p.Id));
        Assert.Equal([1, 2, 3], PerspectiveSet.All.Select(p => p.Order));
        Assert.DoesNotContain(PerspectiveSet.All, p => p.Id.Contains("test", StringComparison.OrdinalIgnoreCase));
        Assert.Same(PerspectiveSet.Coding, PerspectiveSet.Initial);
    }

    // Ruling 52d: Explore's body is one full-window surface; Coding and Architecture are hosts.
    [Fact]
    public void TheBodiesAreTwoHostsAndOneFullWindowSurface()
    {
        Assert.Equal(PerspectiveBody.DockHost, PerspectiveSet.Coding.Body);
        Assert.Equal(PerspectiveBody.FullWindow, PerspectiveSet.Explore.Body);
        Assert.Equal(PerspectiveBody.DockHost, PerspectiveSet.Architecture.Body);
    }

    // ADR-0030 rule 1: the three perspective commands are DERIVED into the catalog from the rows, so
    // the catalog cannot list a perspective the set lacks — and `shell.toggleExplorer` is gone.
    [Fact]
    public void EveryRowHasACatalogCommand_DerivedFromIt_AndTheToggleIsGone()
    {
        foreach (var perspective in PerspectiveSet.All)
        {
            var command = Assert.Single(WorkbenchCommandCatalog.All, c => c.Id == perspective.CommandId);

            Assert.EndsWith(" perspective", command.Title, StringComparison.Ordinal);   // the accessible name ends in "perspective" (US-C1)
            Assert.Equal("_View", command.Menu);                                        // the View menu's radio group (§B3 rule 1)
            Assert.Equal(CommandScope.Global, command.Scope);                 // offered in every perspective
            Assert.Same(perspective, PerspectiveSet.ByCommandId(command.Id));
        }

        Assert.Equal(
            PerspectiveSet.All.Count,
            WorkbenchCommandCatalog.All.Count(c => c.Id.StartsWith("perspective.", StringComparison.Ordinal)));
        Assert.DoesNotContain(WorkbenchCommandCatalog.All, c => c.Id == "shell.toggleExplorer");
    }

    // US-C10: Ctrl+1/2/3 (D1's decision), spelled from the rail digit so the string and the binding
    // cannot disagree; no other catalog command announces a digit gesture.
    [Fact]
    public void ThePerspectiveGesturesAreCtrlPlusTheRailDigit_AndNothingElseUsesThem()
    {
        Assert.Equal(["Ctrl+1", "Ctrl+2", "Ctrl+3"], PerspectiveSet.All.Select(p => p.Gesture));

        var digitGestures = WorkbenchCommandCatalog.All
            .Where(c => c.Gesture.Length == 6 && c.Gesture.StartsWith("Ctrl+", StringComparison.Ordinal) && char.IsDigit(c.Gesture[^1]))
            .Select(c => c.Id)
            .ToList();

        Assert.Equal(PerspectiveSet.All.Select(p => p.CommandId), digitGestures);
    }

    // US-C3: the reading host wins a shared kind, so Architecture is tried before Coding; Explore is
    // never a routing target (it admits no docked kind).
    [Fact]
    public void TheRoutingOrderIsArchitectureThenCoding()
    {
        Assert.Equal([PerspectiveSet.Architecture, PerspectiveSet.Coding], PerspectiveSet.RoutingOrder);
        Assert.DoesNotContain(PerspectiveSet.Explore, PerspectiveSet.RoutingOrder);
    }

    [Fact]
    public void TheCommandLookupReturnsNullForAnUnknownId_NotAThrowOrADefault()
    {
        Assert.Null(PerspectiveSet.ByCommandId("perspective.tests"));
        Assert.Null(PerspectiveSet.ByCommandId(string.Empty));
        Assert.Same(PerspectiveSet.Architecture, PerspectiveSet.ByCommandId("perspective.architecture"));
    }
}
