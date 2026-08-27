using System.Runtime.Versioning;
using AiDe.Core.Ipc;

namespace AiDe.Daemon;

/// <summary>
/// The workspace daemon: one process, one workspace, one pipe.
/// </summary>
/// <remarks>
/// <para><b>This is the process split ADR-0009 deferred.</b> Phase 1 called the core in-process, so
/// <c>CallerPrincipal</c> was simply true. From here it is a claim arriving over a pipe, established
/// by the transport from the connection and never from anything a caller sends.</para>
///
/// <para><b>Order matters and is the startup contract.</b> The workspace lock is taken <i>first</i>,
/// before a pipe exists and before anything is opened. A daemon that started serving and then
/// discovered it was the second one would already have published an endpoint clients could reach,
/// and two daemons on one workspace are two writers to one store — each believing it owns the epoch,
/// both working perfectly, with the damage visible only later as a history with two authors.</para>
///
/// <para><b>It exits when nobody needs it.</b> Not a tidiness measure: a daemon outliving every shell
/// holds the workspace lock, so an orphan makes the workspace unopenable by anything else while
/// being invisible to the user.</para>
///
/// <para><b>Exit codes are the contract</b> with Shell Bootstrap: <b>0</b> served and went idle,
/// <b>2</b> another daemon already owns this workspace, <b>3</b> could not start, <b>64</b> the
/// arguments were wrong.</para>
/// </remarks>
[SupportedOSPlatform("windows")]
internal static class Program
{
    private const int ExitOk = 0;
    private const int ExitWorkspaceLocked = 2;
    private const int ExitStartupFailed = 3;
    private const int ExitBadUsage = 64;

    private static async Task<int> Main(string[] args)
    {
        if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
        {
            await Console.Error.WriteLineAsync(
                "usage: AiDe.Daemon <workspace-path> [--idle-seconds N] [--startup-seconds N]");
            return ExitBadUsage;
        }

        var workspacePath = Path.GetFullPath(args[0]);
        var options = new IpcServerOptions(
            IdleGrace: Seconds(args, "--idle-seconds"),
            StartupGrace: Seconds(args, "--startup-seconds"));

        // FIRST. Before the pipe, before the store, before anything a client could reach.
        if (!WorkspaceLock.TryAcquire(workspacePath, out var workspaceLock))
        {
            await Console.Error.WriteLineAsync(
                $"{IpcErrorCodes.WorkspaceLocked}: another daemon already serves this workspace");
            return ExitWorkspaceLocked;
        }

        using (workspaceLock)
        {
            try
            {
                var pipeName = IpcPipeName.ForWorkspace(workspacePath);
                var server = new IpcServer(pipeName, BuildEndpoint(workspacePath), options);

                // stdout, so a supervisor can confirm which pipe to reach without guessing. The
                // workspace PATH is not printed: the name is derived precisely so the path does not
                // have to travel with it.
                Console.WriteLine($"listening {pipeName}");
                await Console.Out.FlushAsync();

                using var lifetime = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    lifetime.Cancel();
                };

                await server.RunAsync(lifetime.Token);
                return ExitOk;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                await Console.Error.WriteLineAsync($"daemon could not start: {ex.Message}");
                return ExitStartupFailed;
            }
        }
    }

    /// <summary>
    /// The operations this daemon serves.
    /// </summary>
    /// <remarks>
    /// <para><b>Deliberately minimal, and stated so rather than disguised.</b> This phase delivers
    /// the boundary — process, pipe, identity, lifetime — not the migration of the core's command
    /// surface behind it. <c>ping</c> and <c>epoch</c> are real operations that exercise the whole
    /// path end to end; moving <c>describe</c>, <c>find</c>, <c>impact</c> and the dispatch surface
    /// across is the next piece of the process split, and doing half of it here would leave a
    /// boundary that is partly crossed, which is worse than one that is honestly not yet.</para>
    ///
    /// <para>The epoch is fixed at 1 for the same reason: reading it from the store means opening
    /// the store, which is the migration above.</para>
    /// </remarks>
    private static DaemonEndpoint BuildEndpoint(string workspacePath)
    {
        var endpoint = new DaemonEndpoint(
            IpcPipeName.ForWorkspace(workspacePath), new CapabilityRegistry(), _ => 1);

        endpoint.Register("ping", (_, _) => IpcResponse.Success("pong"));
        endpoint.Register("epoch", (_, _) => IpcResponse.Success("1"));

        return endpoint;
    }

    /// <summary>Reads a duration flag, ignoring anything malformed rather than failing to start.</summary>
    /// <remarks>
    /// A daemon that refused to start over an unparseable tuning flag would turn a typo in a
    /// supervisor's command line into an unopenable workspace. The defaults are safe.
    /// </remarks>
    private static TimeSpan? Seconds(string[] args, string flag)
    {
        var index = Array.IndexOf(args, flag);

        return index >= 0
            && index + 1 < args.Length
            && double.TryParse(args[index + 1], out var seconds)
            && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : null;
    }
}
