using AiDe.App.Workbench.Sessions;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// Ruling 38, item 2: for each App surface this node builds — the session document, the paired-zone
/// preset, and the Console merged stream — exercise it, then assert the reserved run-log directory
/// does not exist.
/// </summary>
/// <remarks>
/// <para><b>This is the cover for a declared residual, not a restatement of a guard.</b>
/// <c>SessionPathContractTests.RunLogReservation_IsUnusedOutsideSessionPaths_NothingInSrcWritesThere</c>
/// scans <c>src/</c> recursively for the three reservation tokens, which a hard-coded <c>"runs"</c>
/// literal defeats. These three cases are dynamic: they run the surface and then look at the disk,
/// so a literal is caught by what it did rather than by what it is spelled. They are the App-layer
/// twin of <c>SessionConfigStoreTests.Lifecycle_NeverWritesUnderTheReservedRunsDirectory</c>.</para>
///
/// <para><b><c>RunLogStore</c> is Phase 3.</b> If this slice squatted the path, Phase 3 would
/// inherit it and nothing today would go red — which is what makes the reservation load-bearing
/// rather than decorative.</para>
///
/// <para><b>Each case asserts the session directory DOES exist first.</b> Without that, all three
/// would pass over a workspace where nothing was written at all (DC-016).</para>
/// </remarks>
public sealed class TheSessionSurfacesNeverCreateTheReservedRunsDirectoryTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "aide-f2-runlog-" + Guid.NewGuid().ToString("N"));

    private const string SessionName = "front door";
    private const string TaskClass = "feature";

    public TheSessionSurfacesNeverCreateTheReservedRunsDirectoryTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    /// <summary>Surface 1 of 3: the session document, opened, switched, split and persisted.</summary>
    [Fact]
    public void TheSessionDocumentNeverCreatesIt()
    {
        var sessionId = CreateSession();
        var store = new SessionDocumentStore(_root, sessionId);
        var model = Model(sessionId);

        Sta.Run(() =>
        {
            using var document = new SessionDocumentSurface(model, store);

            model.SetActiveMode(CanvasModeCatalog.TerminalModeId);
            model.Split(CanvasModeCatalog.ConsoleModeId);
            model.SetCanvasSplitWeight(0.35);
            model.SetActiveMode(CanvasModeCatalog.ConsoleModeId);
        });

        AssertReservedPathIsUntouched(sessionId);
    }

    /// <summary>Surface 2 of 3: the paired-zone preset, opened and its splitter moved and saved.</summary>
    [Fact]
    public void ThePairedZonePresetNeverCreatesIt()
    {
        var sessionId = CreateSession();
        var store = new SessionDocumentStore(_root, sessionId);
        var model = Model(sessionId);

        Sta.Run(() =>
        {
            using var document = new SessionDocumentSurface(model, store);

            model.SetComposerWeight(0.6);
            model.SetComposerWeight(0.2);

            Assert.Equal(SessionZonePreset.PairedZone.ComposerZoneId, model.Preset.ComposerZoneId);
        });

        // The envelope really was written — otherwise "nothing created runs/" is true of a store
        // that wrote nothing at all.
        Assert.True(File.Exists(store.FilePath), $"expected {store.FilePath} to have been written");
        AssertReservedPathIsUntouched(sessionId);
    }

    /// <summary>Surface 3 of 3: the Console merged stream, driven by a real lane and filtered.</summary>
    [Fact]
    public async Task TheConsoleMergedStreamNeverCreatesIt()
    {
        var sessionId = CreateSession();
        var model = Model(sessionId);

        using (var lane = new RealLane("run-runlog", "lane-1"))
        using (var feed = new SessionLane("lane-1", "claude-code", lane.Events, model))
        {
            lane.Say("reading");
            lane.AskPermission("Write src/Payments/X.cs?");
            lane.Say("writing");

            Assert.True(
                await feed.WaitForDeliveredAsync(3, TimeSpan.FromSeconds(20)),
                $"the lane delivered {feed.Delivered} of 3");
        }

        Sta.Run(() =>
        {
            using var console = new ConsoleSurface(sessionId, model.Console);

            model.Console.SetLaneVisible("lane-1", false);
            model.Console.SetLaneVisible("lane-1", true);
            model.Console.SetKindVisible("lane-1", "agent.msg", false);

            Assert.NotEmpty(console.RenderedRows);
        });

        AssertReservedPathIsUntouched(sessionId);
    }

    private string CreateSession()
    {
        var sessionId = SessionId.New(DateTimeOffset.UtcNow);
        new SessionConfigStore(_root, sessionId).Create(
            SessionName, _root, [TaskClass], DateTimeOffset.UtcNow);
        return sessionId;
    }

    private SessionDocumentModel Model(string sessionId) => new(
        sessionId, SessionName, _root,
        availableModes: [CanvasModeCatalog.ConsoleModeId, CanvasModeCatalog.TerminalModeId]);

    private void AssertReservedPathIsUntouched(string sessionId)
    {
        var sessionDirectory = SessionPaths.SessionDirectory(_root, sessionId);

        // The DC-016 half: the surface really did work inside this session's own directory, so the
        // assertion below is about a path that was reachable rather than about an empty tree.
        Assert.True(
            Directory.Exists(sessionDirectory),
            $"expected the session directory {sessionDirectory} to exist");

        var reserved = SessionPaths.RunsDirectory(_root, sessionId);

        Assert.False(
            Directory.Exists(reserved),
            $"the reserved run-log directory {reserved} was created; RunLogStore is Phase 3 and this "
            + "slice must leave the path empty (Ruling 38)");

        // And nothing named it anywhere else under the workspace either — the literal a token scan
        // cannot see would not have to land in exactly the reserved place to matter.
        var strays = Directory.Exists(Path.Combine(_root, ".aide"))
            ? Directory.EnumerateDirectories(Path.Combine(_root, ".aide"), "runs", SearchOption.AllDirectories).ToList()
            : [];

        Assert.True(strays.Count == 0, "a 'runs' directory appeared under .aide: " + string.Join(", ", strays));
    }
}
