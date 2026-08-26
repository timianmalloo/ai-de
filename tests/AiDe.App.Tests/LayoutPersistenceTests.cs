using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// US-9: "close the application, reopen the same workspace, my arrangement returns."
/// Every piece of this existed and was tested — and had no production caller, so the running app
/// never saved or loaded a layout at all. These pin the wiring, not the store.
/// </summary>
public sealed class LayoutPersistenceTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "aide-persist", Guid.NewGuid().ToString("N"));

    private string File_ => Path.Combine(_dir, "layout.json");

    private static readonly HashSet<string> Surfaces =
        ["explore", "domain", "terminal-1", "provenance"];

    private LayoutPersistence Open(ILayoutService service, Func<StackNode, bool>? display = null) =>
        new(service, File_, Surfaces, display, debounceMilliseconds: 20);

    [Fact]
    public void AnArrangement_SurvivesAShutdownAndRestart()
    {
        var first = new LayoutService();
        using (var persistence = Open(first))
        {
            persistence.Restore();
            first.Apply(new LayoutOperation.MoveSurface("domain",
                new DropTarget(first.Current.FindStackOf("provenance")!.Id, DropKind.SplitBottom)));
            persistence.SaveNow();
        }

        var arranged = first.Current.Shape();

        // A brand-new session, as if the app had been closed and reopened.
        var second = new LayoutService();
        using var reopened = Open(second);
        var result = reopened.Restore();

        Assert.False(result.WasDefaulted);
        Assert.Equal(arranged, second.Current.Shape());
    }

    // Dispose must flush: rearranging and immediately closing is the most common way to lose work.
    [Fact]
    public void DisposingFlushesAPendingSave()
    {
        var service = new LayoutService();
        var persistence = Open(service);
        persistence.Restore();
        service.Apply(new LayoutOperation.ResizeSplit("split-root", 0, 0.12));
        persistence.MarkDirty();          // debounce started, not yet elapsed
        persistence.Dispose();            // must not lose it

        var reopened = new LayoutService();
        using var second = Open(reopened);
        second.Restore();

        Assert.Equal(service.Current.Shape(), reopened.Current.Shape());
    }

    [Fact]
    public void WithNoSavedLayout_TheDefaultIsUsedWithoutComplaint()
    {
        var service = new LayoutService();
        using var persistence = Open(service);

        var result = persistence.Restore();

        Assert.True(result.WasDefaulted);
        Assert.Null(result.ErrorCode);
        service.Current.AssertInvariant();
    }

    // The off-screen guard, which previously had no production caller at all.
    [Fact]
    public void AFloatingPaneOnADisconnectedDisplay_IsRehomedNotJustReported()
    {
        var first = new LayoutService();
        using (var persistence = Open(first))
        {
            persistence.Restore();
            first.Apply(new LayoutOperation.MoveSurface("provenance", new DropTarget("", DropKind.Float)));
            // Park it far off any real screen.
            var floating = first.Current.Floating[0];
            first.Restore(first.Current with
            {
                Floating = first.Current.Floating.Replace(
                    floating, floating with { FloatingBounds = new LayoutRect(-9000, -9000, 300, 200) }),
            });
            persistence.SaveNow();
        }

        var second = new LayoutService();
        using var reopened = Open(second);
        var result = reopened.Restore();

        Assert.Equal(LayoutErrorCodes.PartialRestore, result.ErrorCode);
        Assert.Contains("Provenance", result.RehomedFloating);
        // Reporting alone would leave a window the user still cannot reach.
        Assert.Null(second.Current.Floating[0].FloatingBounds);
    }

    [Fact]
    public void FloatingBounds_RoundTripThroughTheEnvelope()
    {
        var bounds = new LayoutRect(120, 90, 420, 310);
        var first = new LayoutService();
        using (var persistence = Open(first))
        {
            persistence.Restore();
            first.Apply(new LayoutOperation.MoveSurface("provenance", new DropTarget("", DropKind.Float)));
            var floating = first.Current.Floating[0];
            first.Restore(first.Current with
            {
                Floating = first.Current.Floating.Replace(floating, floating with { FloatingBounds = bounds }),
            });
            persistence.SaveNow();
        }

        var second = new LayoutService();
        using var reopened = Open(second, display: _ => true);
        reopened.Restore();

        Assert.Equal(bounds, second.Current.Floating[0].FloatingBounds);
    }

    [Fact]
    public void AnUnwritableLocation_DegradesToNotSavedRatherThanThrowing()
    {
        var service = new LayoutService();
        // A path that cannot exist: saving must not take the app down on exit.
        var persistence = new LayoutPersistence(
            service, Path.Combine("Z:", "no-such-volume", "layout.json"), Surfaces);

        var exception = Record.Exception(() => { persistence.SaveNow(); persistence.Dispose(); });

        Assert.Null(exception);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }
}
