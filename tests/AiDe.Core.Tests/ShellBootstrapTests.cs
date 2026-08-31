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

    private static string DaemonPath()
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "AiDe.Daemon", "bin"));
        var configuration = Directory.Exists(Path.Combine(root, "Release")) ? "Release" : "Debug";
        var candidate = Path.Combine(root, configuration, "net10.0-windows", "AiDe.Daemon.exe");

        Assert.True(
            File.Exists(candidate),
            $"the daemon was not built. Expected it at:\n  {candidate}\n"
            + "Build the solution rather than the test project alone.");

        return candidate;
    }

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

        // Readiness barrier (DC-040): prove the first daemon is actually *serving* before the second
        // shell arrives, so reuse is measured against a ready daemon rather than a still-starting one.
        // Gating on an observed answer — not a fixed delay — is what makes this deterministic.
        Assert.NotNull(await first.FindAsync("", 1, CancellationToken.None));

        await using var second = await ShellBootstrap.ConnectOrLaunchAsync(
            workspace, DaemonPath(), CancellationToken.None);

        // The reuse invariant is one logical daemon — one store, one epoch — per workspace, enforced
        // by WorkspaceLock. Epoch equality is the DETERMINISTIC, workspace-scoped proof of it: even
        // if a second shell momentarily launched a redundant process under load, that process loses
        // the workspace lock and exits, and the shell then connects to the incumbent — so the epoch
        // it sees is the incumbent's.
        //
        // This replaced a system-wide `AiDe.Daemon` process count (DC-040). That count was a category
        // error: the daemon deliberately outlives its client (the idle grace holds warm state through
        // a shell restart), so other tests' lingering daemons polluted a machine-global counter, and
        // an ordinary load-induced-then-lock-resolved redundant launch — harmless in production —
        // could false-fail it. The counter measured the machine; the invariant is per workspace.
        Assert.Equal(first.Epoch, second.Epoch);

        // Triangulate the workspace-scoped invariant: a third connect finds the same incumbent. first,
        // second and third overlap in scope, so the daemon cannot idle-shut-down between them, which
        // is what makes the three-way epoch equality a stable oracle rather than a timing gamble.
        await using var third = await ShellBootstrap.ConnectOrLaunchAsync(
            workspace, DaemonPath(), CancellationToken.None);

        Assert.Equal(first.Epoch, third.Epoch);
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
