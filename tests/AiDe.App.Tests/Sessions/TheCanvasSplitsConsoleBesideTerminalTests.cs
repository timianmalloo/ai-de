using AiDe.App.Workbench.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// R16 b2 / Ruling 21: the canvas zone splits into two modes side by side, both live, and the split
/// survives a mode switch.
/// </summary>
/// <remarks>
/// <b>"One live pane and one rebuilt-on-focus pane" is the failure named by the clause</b>, and it
/// is what a naive tab control produces: only the selected tab's content is realized, so the other
/// half is created when it is focused and destroyed when it is not. These cases assert reference
/// identity across the switch on <i>both</i> halves, which is what distinguishes two live panes from
/// one live pane and one that looks live when you happen to be looking at it.
/// </remarks>
public sealed class TheCanvasSplitsConsoleBesideTerminalTests
{
    private static SessionDocumentModel Model() => new(
        "20260910T120000Z-deadbeef",
        "Front door",
        Path.GetTempPath(),
        availableModes: [CanvasModeCatalog.ConsoleModeId, CanvasModeCatalog.TerminalModeId]);

    [Fact]
    public void ConsoleSplitsBesideTerminal_AndBothHalvesAreLive() => Sta.Run(() =>
    {
        var model = Model();
        using var document = new SessionDocumentSurface(model);

        var console = document.ContentFor(CanvasModeCatalog.ConsoleModeId);

        model.Split(CanvasModeCatalog.TerminalModeId);

        Assert.True(model.IsSplit);
        Assert.Equal(CanvasModeCatalog.ConsoleModeId, model.ActiveModeId);
        Assert.Equal(CanvasModeCatalog.TerminalModeId, model.SplitModeId);

        // Both halves have real width, so neither is a collapsed sliver pretending to be a pane.
        Assert.True(document.RenderedPrimaryWeight > 0);
        Assert.True(document.RenderedSecondaryWeight > 0);

        var terminal = document.ContentFor(CanvasModeCatalog.TerminalModeId);

        // Both are built, and both are the SAME instances the document is showing.
        Assert.True(document.HasBuilt(CanvasModeCatalog.ConsoleModeId));
        Assert.True(document.HasBuilt(CanvasModeCatalog.TerminalModeId));
        Assert.Same(console, document.ContentFor(CanvasModeCatalog.ConsoleModeId));
        Assert.Same(terminal, document.ContentFor(CanvasModeCatalog.TerminalModeId));
    });

    [Fact]
    public void TheSplitSurvivesAModeSwitch() => Sta.Run(() =>
    {
        var model = Model();
        using var document = new SessionDocumentSurface(model);

        var console = document.ContentFor(CanvasModeCatalog.ConsoleModeId);
        model.Split(CanvasModeCatalog.TerminalModeId);
        var terminal = document.ContentFor(CanvasModeCatalog.TerminalModeId);

        // Switching to the mode the OTHER half holds is a swap, not a collapse: both halves keep
        // rendering and the split stays open.
        model.SetActiveMode(CanvasModeCatalog.TerminalModeId);

        Assert.True(model.IsSplit);
        Assert.Equal(CanvasModeCatalog.TerminalModeId, model.ActiveModeId);
        Assert.Equal(CanvasModeCatalog.ConsoleModeId, model.SplitModeId);

        Assert.Same(console, document.ContentFor(CanvasModeCatalog.ConsoleModeId));
        Assert.Same(terminal, document.ContentFor(CanvasModeCatalog.TerminalModeId));
    });

    [Fact]
    public void ACanvasCannotBeSplitAgainstItsOwnActiveMode() => Sta.Run(() =>
    {
        // Two halves rendering one mode would read as two lanes, which is the opposite of what the
        // split is for.
        var model = Model();
        using var document = new SessionDocumentSurface(model);

        Assert.Throws<ArgumentException>(() => model.Split(model.ActiveModeId));
    });
}
