using System.Diagnostics;
using System.Runtime.Versioning;
using AiDe.Core.Ipc;

namespace AiDe.Core.Tests;

/// <summary>
/// The process split, as a real second process.
/// </summary>
/// <remarks>
/// <para><b>Everything else about this boundary has been proven in one process.</b> The
/// authorization decisions without a socket, the transport with a pipe between two objects in the
/// same test host. Neither can answer the question ADR-0009 actually deferred: does a shell reach a
/// <i>separate daemon process</i>, and does that process behave — take its lock, publish its pipe,
/// serve, and go away — as a process rather than as an object.</para>
///
/// <para><b>The lock case is the one that cannot be faked.</b> Two <see cref="WorkspaceLock"/>
/// instances in one process share a mutex the easy way; two daemon <i>processes</i> contending is
/// the actual scenario, and it is the only version that proves the store cannot get two writers.</para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class DaemonProcessTests
{
    private static string FreshWorkspace() =>
        Path.Combine(Path.GetTempPath(), $"aide-daemon-{Guid.NewGuid():N}");

    private static string LocateDaemon()
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

    /// <summary>Starts a daemon and waits until it says which pipe it is serving.</summary>
    /// <remarks>
    /// Waiting for the announcement rather than sleeping: a fixed delay is either too short (a flaky
    /// test) or too long (a slow one), and the daemon already reports readiness precisely so a
    /// supervisor need not guess.
    /// </remarks>
    private static async Task<(Process Process, string PipeName)> StartDaemonAsync(
        string workspace, params string[] extra)
    {
        var start = new ProcessStartInfo(LocateDaemon())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        start.ArgumentList.Add(workspace);
        foreach (var argument in extra)
        {
            start.ArgumentList.Add(argument);
        }

        var process = Process.Start(start);
        Assert.NotNull(process);

        var announcement = await process!.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(30));

        Assert.NotNull(announcement);
        Assert.StartsWith("listening ", announcement, StringComparison.Ordinal);

        return (process, announcement!["listening ".Length..].Trim());
    }

    private static void Stop(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }

        process.Dispose();
    }

    // ---- a shell reaches a separate process ---------------------------------

    [Fact]
    public async Task AShell_ReachesADaemonInAnotherProcess()
    {
        var workspace = FreshWorkspace();
        var (daemon, pipeName) = await StartDaemonAsync(workspace, "--startup-seconds", "40");

        try
        {
            Assert.Equal(IpcPipeName.ForWorkspace(workspace), pipeName);

            await using var client = await IpcClient.ConnectAsync(
                pipeName, TimeSpan.FromSeconds(20), CancellationToken.None);

            var opened = await client.OpenWorkspaceAsync(pipeName, 1, CancellationToken.None);
            Assert.True(opened.Ok, opened.Reason);

            var pong = await client.InvokeAsync(
                "ping", "cmd-1", pipeName, 1, null, CancellationToken.None);

            Assert.True(pong.Ok, pong.Reason);
            Assert.Equal("pong", pong.Payload);

            // The peer really is another process — the whole point of the phase.
            Assert.NotEqual(Environment.ProcessId, daemon.Id);
        }
        finally
        {
            Stop(daemon);
        }
    }

    // ---- P2-IPC-06: one daemon per workspace, across processes ---------------

    [Fact]
    public async Task ASecondDaemonForTheSameWorkspace_RefusesToStart()
    {
        // Two daemons on one workspace would both work, both write, and both believe they owned the
        // epoch. Nothing would fail at the time; the store would simply end up with a history that
        // has two authors.
        var workspace = FreshWorkspace();
        var (first, _) = await StartDaemonAsync(workspace, "--startup-seconds", "40");

        try
        {
            var start = new ProcessStartInfo(LocateDaemon())
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            start.ArgumentList.Add(workspace);

            using var second = Process.Start(start)!;
            var error = await second.StandardError.ReadToEndAsync();
            Assert.True(second.WaitForExit(30_000), "the second daemon did not exit");

            Assert.Equal(2, second.ExitCode);
            Assert.Contains(IpcErrorCodes.WorkspaceLocked, error, StringComparison.Ordinal);

            // And the first is unharmed: the loser exits, the incumbent keeps serving.
            Assert.False(first.HasExited, "the incumbent daemon died when a second one was attempted");
        }
        finally
        {
            Stop(first);
        }
    }

    [Fact]
    public async Task AfterTheFirstDaemonExits_AnotherCanTakeTheWorkspace()
    {
        // Otherwise a restart would need a reboot, and the lock protecting the store would be the
        // thing that made the workspace permanently unopenable.
        var workspace = FreshWorkspace();

        var (first, _) = await StartDaemonAsync(workspace, "--startup-seconds", "2");
        Assert.True(first.WaitForExit(40_000), "the first daemon did not exit on its startup grace");
        Assert.Equal(0, first.ExitCode);
        first.Dispose();

        var (second, _) = await StartDaemonAsync(workspace, "--startup-seconds", "40");
        Stop(second);
    }

    // ---- P2-IPC-05: an orphaned daemon does not linger ------------------------

    [Fact]
    public async Task ADaemonNobodyConnectsTo_ExitsOnItsOwn()
    {
        var workspace = FreshWorkspace();
        var (daemon, _) = await StartDaemonAsync(workspace, "--startup-seconds", "2");

        try
        {
            Assert.True(
                daemon.WaitForExit(40_000),
                "an orphaned daemon kept running, holding the workspace lock invisibly");
            Assert.Equal(0, daemon.ExitCode);
        }
        finally
        {
            Stop(daemon);
        }
    }

    [Fact]
    public async Task ADaemonWhoseClientLeaves_ExitsAfterTheIdleGrace()
    {
        var workspace = FreshWorkspace();
        var (daemon, pipeName) = await StartDaemonAsync(
            workspace, "--startup-seconds", "40", "--idle-seconds", "2");

        try
        {
            await using (var client = await IpcClient.ConnectAsync(
                pipeName, TimeSpan.FromSeconds(20), CancellationToken.None))
            {
                Assert.True((await client.OpenWorkspaceAsync(pipeName, 1, CancellationToken.None)).Ok);
            }

            Assert.True(
                daemon.WaitForExit(40_000),
                "the daemon kept running after its last client left");
            Assert.Equal(0, daemon.ExitCode);
        }
        finally
        {
            Stop(daemon);
        }
    }

    // ---- usage ---------------------------------------------------------------

    [Fact]
    public void WithNoWorkspace_TheDaemonRefusesToStart()
    {
        // A daemon that defaulted to some directory would take a lock on a workspace nobody asked
        // for and serve it under a pipe name nobody can predict.
        var start = new ProcessStartInfo(LocateDaemon())
        {
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEnd();

        Assert.True(process.WaitForExit(20_000));
        Assert.Equal(64, process.ExitCode);
        Assert.Contains("usage", error, StringComparison.OrdinalIgnoreCase);
    }
}
