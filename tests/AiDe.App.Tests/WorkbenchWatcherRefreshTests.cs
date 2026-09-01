using AiDe.App.Workbench;
using AiDe.Core.Watcher;

namespace AiDe.App.Tests;

/// <summary>
/// conn-9: the watcher read panes auto-refresh only when the store changes. The subtle claim is that the
/// change signal (<see cref="WorkbenchShell.WatcherFingerprint"/>) catches a *liveness transition* - a
/// session going Ended while the session count is unchanged - not only a count change. A count-only
/// signal would leave a pane showing a session as live after it ended (the DC-064 shape, one layer up).
/// </summary>
public sealed class WorkbenchWatcherRefreshTests : IDisposable
{
    private readonly string _data =
        Path.Combine(Path.GetTempPath(), "aide-conn9-data", Guid.NewGuid().ToString("N"));

    private readonly string _coord =
        Path.Combine(Path.GetTempPath(), "aide-conn9-coord", Guid.NewGuid().ToString("N"));

    public WorkbenchWatcherRefreshTests()
    {
        Directory.CreateDirectory(_data);
        Directory.CreateDirectory(_coord);
    }

    private static SessionCoordinationIdentity Identity(string terminal)
        => new("C:/repos/hw", "hw", "main", "C:/repos/hw", terminal, "agent");

    [Fact]
    public void Fingerprint_Changes_OnALivenessTransition_NotOnlyOnCount()
    {
        using var host = WatcherHost.Open(_data, _coord);
        var emitter = host.CreateEmitter();

        var empty = WorkbenchShell.WatcherFingerprint(host);

        emitter.Register("ext-1", Identity("ext-1"));
        emitter.Register("ext-2", Identity("ext-2"));
        host.PumpOnce();
        var twoAlive = WorkbenchShell.WatcherFingerprint(host);
        Assert.NotEqual(empty, twoAlive); // a new session must flip the signal

        // End one: the session count is still 2, but one is now Ended - the fingerprint MUST change so
        // the pane re-renders and stops showing the ended session as live.
        emitter.End("ext-1");
        host.PumpOnce();
        var oneEnded = WorkbenchShell.WatcherFingerprint(host);
        Assert.Equal(2, host.Store.AllSessions().Count);
        Assert.NotEqual(twoAlive, oneEnded);
    }

    public void Dispose()
    {
        try { Directory.Delete(_data, recursive: true); }
        catch (IOException) { /* transient handle on Windows */ }

        try { Directory.Delete(_coord, recursive: true); }
        catch (IOException) { /* transient handle on Windows */ }
    }
}
