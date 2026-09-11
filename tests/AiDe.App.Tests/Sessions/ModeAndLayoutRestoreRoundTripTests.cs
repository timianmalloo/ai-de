using AiDe.App.Workbench.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// R13 b3: reopening a session restores its canvas mode and its layout — a <b>real round trip</b>
/// through the store, not a re-instantiated view model.
/// </summary>
/// <remarks>
/// <b>A NEW store instance reads it back.</b> The plan says "a new process or store instance", and
/// the reason is that an in-memory hand-back proves nothing about persistence: the failure being
/// prevented is a document that reopens Console-default when Terminal was active, and that failure
/// lives on the disk boundary.
/// </remarks>
/// <remarks>
/// <b>Registers the Terminal row Ruling 45 cut from <c>BuiltIn</c>.</b> The mechanism under test
/// here needs a SECOND canvas mode to exist at all; which modes the product ships is a different
/// question, and Ruling 45 answered it by cutting a row whose content Phase 1 cannot bind. Every
/// assertion below is unchanged — the proof survives the cut rather than being weakened by it, which
/// is also Ruling 22's clause re-proven against a test-registered mode.
/// </remarks>
[Collection(CanvasModes.Name)]
public sealed class ModeAndLayoutRestoreRoundTripTests : IDisposable
{
    private readonly TerminalModeForTests _terminal = new();

    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "aide-f2-restore-" + Guid.NewGuid().ToString("N"));

    private readonly string _sessionId = SessionId.New(DateTimeOffset.UtcNow);

    public ModeAndLayoutRestoreRoundTripTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        _terminal.Dispose();

        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void ReopeningRestoresTheActiveMode_TheSplit_AndBothSplitterPositions()
    {
        var written = new SessionDocumentStore(_root, _sessionId);

        var before = NewModel();
        before.SetActiveMode(CanvasModeCatalog.TerminalModeId);
        before.Split(CanvasModeCatalog.ConsoleModeId);
        before.SetCanvasSplitWeight(0.35);
        before.SetComposerWeight(0.61);
        written.Save(before.Envelope());

        // A NEW store instance, reading the bytes the first one wrote.
        var reopened = new SessionDocumentStore(_root, _sessionId);
        var saved = reopened.Load();

        Assert.NotNull(saved);

        var after = NewModel();
        Assert.Equal(CanvasModeCatalog.ConsoleModeId, after.ActiveModeId);   // the default it starts from
        after.Restore(saved);

        Assert.Equal(CanvasModeCatalog.TerminalModeId, after.ActiveModeId);
        Assert.Equal(CanvasModeCatalog.ConsoleModeId, after.SplitModeId);
        Assert.Equal(0.35, after.CanvasSplitWeight, 3);
        Assert.Equal(0.61, after.Preset.ComposerWeight, 3);
    }

    [Fact]
    public void TheRestoredStateReachesTheRenderedDocument()
    {
        var store = new SessionDocumentStore(_root, _sessionId);

        var before = NewModel();
        before.SetActiveMode(CanvasModeCatalog.TerminalModeId);
        before.Split(CanvasModeCatalog.ConsoleModeId);
        before.SetCanvasSplitWeight(0.3);
        store.Save(before.Envelope());

        Sta.Run(() =>
        {
            var model = NewModel();
            model.Restore(new SessionDocumentStore(_root, _sessionId).Load()!);

            using var document = new SessionDocumentSurface(model);

            // The rendered canvas really is split at the restored ratio — the model's number alone
            // would not say the splitter moved.
            Assert.Equal(0.3, document.RenderedPrimaryWeight, 3);
            Assert.Equal(0.7, document.RenderedSecondaryWeight, 3);

            // And the restored mode really is the terminal, not a document that says "terminal" and
            // shows the console.
            Assert.IsType<AiDe.App.Workbench.TerminalSurface>(document.ContentFor(model.ActiveModeId));
            Assert.IsType<ConsoleSurface>(document.ContentFor(model.SplitModeId!));
        });
    }

    [Fact]
    public void ASessionWithNoSavedEnvelopeOpensOnTheDefaults()
    {
        // No envelope is not a corrupt envelope: a first open has nothing to restore and must not
        // read as a failed restore.
        Assert.Null(new SessionDocumentStore(_root, _sessionId).Load());

        var model = NewModel();
        Assert.Equal(CanvasModeCatalog.ConsoleModeId, model.ActiveModeId);
        Assert.False(model.IsSplit);
    }

    [Fact]
    public void AnUnreadableEnvelopeOpensOnTheDefaultsRatherThanThrowing()
    {
        var store = new SessionDocumentStore(_root, _sessionId);
        Directory.CreateDirectory(SessionPaths.SessionDirectory(_root, _sessionId));
        File.WriteAllText(store.FilePath, "{ this is not the envelope");

        Assert.Null(store.Load());
    }

    private SessionDocumentViewModel NewModel() => new(
        _sessionId, "Front door", _root,
        [CanvasModeCatalog.ConsoleModeId, CanvasModeCatalog.TerminalModeId]);
}
