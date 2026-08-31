using System.Diagnostics;
using System.Runtime.Versioning;
using AiDe.Core.Ipc;

namespace AiDe.Core.Tests;

/// <summary>
/// The shell reaching a daemon — starting one if it must.
/// </summary>
/// <remarks>
/// <para><b>This is the step that makes the process split the product rather than a demonstration.</b>
/// Everything beneath it was proven in tests while the shell still ran the core in-process, which is
/// the "built but inert" state this session has closed twice already.</para>
///
/// <para>Every case here starts a <b>real daemon process</b>. A fake would answer the question
/// "does the bootstrap call what I told it to" rather than "does a shell get a working daemon",
/// which is the only question worth asking of a launcher.</para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class ShellBootstrapTests : IDisposable
{
    private readonly List<string> _workspaces = [];

    private string FreshWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aide-bootstrap-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        _workspaces.Add(path);
        return path;
    }

    /// <summary>
    /// The daemon built alongside THESE tests.
    /// </summary>
    /// <remarks>
    /// <para><b>The configuration comes from this assembly's own path</b>, not from whichever
    /// configuration directory happens to exist. It used to prefer Release when a Release directory
    /// was present — so a single <c>dotnet publish -c Release</c>, run for something else entirely,
    /// left these Debug tests launching a Release daemon built hours earlier. When the IPC protocol
    /// changed, three tests failed with <c>ipc.unsupported_version</c>, which is the protocol
    /// working correctly and the harness pointing at the wrong binary (DC-023: a gate running a
    /// stale build).</para>
    ///
    /// <para>The staleness check is the half that matters. Picking the right directory does not help
    /// if what is in it was built before the change under test, and "the daemon is old" is otherwise
    /// indistinguishable from "the daemon is broken".</para>
    /// </remarks>
    private static string DaemonPath()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);
        var configuration =
            here.FullName.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase) ? "Release" : "Debug";

        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "AiDe.Daemon", "bin"));
        var candidate = Path.Combine(root, configuration, "net10.0-windows", "AiDe.Daemon.exe");

        Assert.True(
            File.Exists(candidate),
            $"the daemon was not built in {configuration}. Expected it at:\n  {candidate}\n"
            + "Build the solution rather than the test project alone.");

        // Not timestamps: a daemon that did not need rebuilding is OLDER than the tests and
        // perfectly current, so a time comparison reports staleness on every incremental build.
        // What actually matters is whether it carries the same AiDe.Core — the protocol, the
        // envelope and the operations all live there — and .NET builds deterministically, so equal
        // source gives equal bytes.
        var mine = Path.Combine(AppContext.BaseDirectory, "AiDe.Core.dll");
        var theirs = Path.Combine(Path.GetDirectoryName(candidate)!, "AiDe.Core.dll");

        if (File.Exists(mine) && File.Exists(theirs))
        {
            Assert.True(
                File.ReadAllBytes(mine).AsSpan().SequenceEqual(File.ReadAllBytes(theirs)),
                $"the {configuration} daemon at\n  {candidate}\n"
                + "was built against a different AiDe.Core than these tests. Launching it would test "
                + "a build that is not the one under test — which is how an IPC protocol change "
                + "looked like three broken tests (DC-023). Build the solution.");
        }

        return candidate;
    }

    private static int DaemonsRunning() =>
        Process.GetProcessesByName("AiDe.Daemon").Length;

    // ---- launching -----------------------------------------------------------

    [Fact]
    public async Task WithNoDaemonRunning_OneIsLaunchedAndAnswers()
    {
        var workspace = FreshWorkspace();

        await using var client = await ShellBootstrap.ConnectOrLaunchAsync(
            workspace, DaemonPath(), CancellationToken.None);

        // A connected client is not enough — it must answer, which means the daemon opened its
        // store and registered its operations, not merely accepted a pipe.
        var result = await client.FindAsync("", 10, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(client.Epoch > 0, "the handshake did not carry a usable epoch");
    }

    [Fact]
    public async Task ASecondShell_ReusesTheRunningDaemon_RatherThanStartingAnother()
    {
        // One daemon per workspace is the store's single-writer invariant. The bootstrap tries the
        // pipe before launching precisely so the common case costs nothing — and if it launched
        // first, the second shell would start a process whose only job is to discover it is
        // redundant and exit.
        var workspace = FreshWorkspace();

        await using var first = await ShellBootstrap.ConnectOrLaunchAsync(
            workspace, DaemonPath(), CancellationToken.None);

        var before = DaemonsRunning();

        await using var second = await ShellBootstrap.ConnectOrLaunchAsync(
            workspace, DaemonPath(), CancellationToken.None);

        Assert.Equal(first.Epoch, second.Epoch);
        Assert.True(
            DaemonsRunning() <= before,
            "a second daemon was started for a workspace that already had one");
    }

    [Fact]
    public async Task TwoWorkspaces_GetTheirOwnDaemons()
    {
        // The lock and the pipe name are both per workspace; sharing a daemon across two would put
        // two stores behind one epoch.
        await using var alpha = await ShellBootstrap.ConnectOrLaunchAsync(
            FreshWorkspace(), DaemonPath(), CancellationToken.None);
        await using var beta = await ShellBootstrap.ConnectOrLaunchAsync(
            FreshWorkspace(), DaemonPath(), CancellationToken.None);

        var alphaResult = await alpha.FindAsync("", 5, CancellationToken.None);
        var betaResult = await beta.FindAsync("", 5, CancellationToken.None);

        Assert.NotNull(alphaResult);
        Assert.NotNull(betaResult);
    }

    // ---- failure is reported, never degraded ---------------------------------

    [Fact]
    public async Task WhenNoDaemonIsInstalled_TheFailureSaysSo()
    {
        // Distinguished from "the daemon would not start": a missing build is an installation
        // problem, and reporting it as a startup failure sends the investigation somewhere else.
        //
        // What must NOT happen is a silent fallback to running the core in this process. That would
        // work, and would abandon the trust boundary, the workspace lock and the epoch fence at the
        // moment they were most obviously needed.
        var missing = Path.Combine(Path.GetTempPath(), $"no-daemon-{Guid.NewGuid():N}.exe");

        var thrown = await Assert.ThrowsAsync<DaemonUnavailableException>(
            () => ShellBootstrap.ConnectOrLaunchAsync(
                FreshWorkspace(), missing, CancellationToken.None));

        Assert.Contains("installed", thrown.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cancellation_IsHonouredWhileWaitingForALaunchedDaemon()
    {
        // The launch deadline is thirty seconds. A shell closing during a cold start must not be
        // held open for the rest of it.
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ShellBootstrap.ConnectOrLaunchAsync(
                FreshWorkspace(), DaemonPath(), cancellation.Token));
    }

    public void Dispose()
    {
        foreach (var workspace in _workspaces)
        {
            try
            {
                Directory.Delete(workspace, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
