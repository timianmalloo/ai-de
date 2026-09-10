using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// R13 b3 and R16 b1/b2: the session document opens in the paired-zone preset, on Console, and its
/// canvas splits Console beside Terminal with both halves live.
/// </summary>
/// <remarks>
/// <para><b>The preset is new construction.</b> No "preset" concept existed before this node —
/// <c>TerminalColorScheme.Presets</c> is a palette table and is unrelated — so these assertions are
/// about a value this slice introduced, and they read the <b>rendered</b> column widths rather than
/// the model's numbers: the clause is that the two zones <i>open in</i> the split, which is a
/// statement about what the document shows.</para>
///
/// <para><b>The composer half is today's real composer.</b> Addendum A §10 absorbs
/// <c>PromptDraftViewModel</c>'s transfer rules into the composer and keeps the class for the
/// standalone draft surface, so the paired zone hosts a working
/// <c>PromptDraftSurface</c> — not a placeholder, and not a deletion of a live surface.</para>
/// </remarks>
public sealed class TheSessionDocumentOpensInThePairedZoneTests
{
    private static SessionDocumentModel Model() => new(
        "20260910T120000Z-deadbeef",
        "Front door",
        Path.GetTempPath(),
        availableModes: [CanvasModeCatalog.ConsoleModeId, CanvasModeCatalog.TerminalModeId]);

    [Fact]
    public void TheComposerAndCanvasZonesOpenInTheSpecifiedSplit() => Sta.Run(() =>
    {
        var model = Model();
        using var document = new SessionDocumentSurface(model);

        // The rendered split, not the model's intention.
        Assert.Equal(SessionZonePreset.PairedZone.ComposerWeight, document.RenderedComposerWeight, 3);
        Assert.Equal(SessionZonePreset.PairedZone.CanvasWeight, document.RenderedCanvasWeight, 3);

        // Both halves are really there, and the composer half is the working composer.
        Assert.NotNull(document.Composer);
        Assert.Equal(SessionZonePreset.PairedZone.Orientation, model.Preset.Orientation);
    });

    [Fact]
    public void ASessionOpensOnConsole_NotOnTerminal() => Sta.Run(() =>
    {
        var model = Model();
        using var document = new SessionDocumentSurface(model);

        Assert.Equal(CanvasModeCatalog.ConsoleModeId, model.ActiveModeId);
        Assert.IsType<ConsoleSurface>(document.ContentFor(model.ActiveModeId));

        // Lazily, so opening a session does not start a terminal nobody asked for.
        Assert.False(document.HasBuilt(CanvasModeCatalog.TerminalModeId));
    });

    [Fact]
    public void TheDocumentIsBuiltByTheFactoryUnderItsOwnKind() => Sta.Run(() =>
    {
        var model = Model();
        using var document = new SessionDocumentSurface(model);

        var factory = new SurfaceContentFactory(
            queries: null, sessionDocumentFor: _ => document);

        var content = factory.Create(new AiDe.Core.Workbench.Surface(
            SessionDocumentSurface.SurfaceIdFor(model.SessionId),
            SessionDocumentSurface.Kind,
            model.Title));

        var inner = content is System.Windows.Controls.Border { Child: System.Windows.FrameworkElement child }
            ? child
            : content;

        Assert.Same(document, inner);
    });

    [Fact]
    public void MovingTheZoneSplitterNeverLetsEitherHalfVanish() => Sta.Run(() =>
    {
        var model = Model();
        using var document = new SessionDocumentSurface(model);

        model.SetComposerWeight(0.0);
        Assert.Equal(SessionZonePreset.MinimumWeight, document.RenderedComposerWeight, 3);

        model.SetComposerWeight(1.0);
        Assert.Equal(1.0 - SessionZonePreset.MinimumWeight, document.RenderedComposerWeight, 3);
    });
}
