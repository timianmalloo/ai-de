using System.Runtime.Versioning;
using AiDe.Core;
using AiDe.Core.Ipc;
using AiDe.Core.Upgrade;

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
            WorkspaceCore? core = null;

            try
            {
                var pipeName = IpcPipeName.ForWorkspace(workspacePath);

                // Opened AFTER the lock and before the pipe. A daemon that published an endpoint and
                // then failed to open its store would be reachable while unable to answer anything.
                var (endpoint, opened) = OpenWorkspace(workspacePath);
                core = opened;

                var server = new IpcServer(pipeName, endpoint, options);

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
            finally
            {
                // Closed before the workspace lock is released, so the next daemon never finds the
                // store still held by a process that has already given up its claim to the workspace.
                core?.Dispose();
            }
        }
    }

    /// <summary>
    /// Opens the workspace and puts its read surface behind the endpoint.
    /// </summary>
    /// <remarks>
    /// <para><b>Read projections and the daemon's own two operations.</b> Dispatch — writing to a
    /// terminal, staging a prompt — carries the two-phase receipt semantics of ADR-0010 and moves
    /// across as its own piece of work; registering a handler that half-implemented it would leave
    /// the boundary partly crossed, which is worse than one honestly not yet.</para>
    ///
    /// <para><b>The workspace id is the derived pipe name, not the path.</b> It travels in every
    /// request and appears in operator output, and the pipe name was already computed precisely so
    /// the path does not have to.</para>
    /// </remarks>
    private static (DaemonEndpoint Endpoint, WorkspaceCore Core) OpenWorkspace(string workspacePath)
    {
        var workspaceId = IpcPipeName.ForWorkspace(workspacePath);

        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AiDe", "workspaces", workspaceId);

        // BEFORE the store is opened. A migration interrupted by a power loss leaves a store that
        // may be anything, and the only thing known to be good is its snapshot — so the next start
        // is where that gets undone, because nothing at the moment of the crash got to run.
        var recovery = UpgradeCoordinator.RecoverIfIncomplete(
            Path.Combine(dataDirectory, "workspace.db"), dataDirectory);

        if (recovery.Recovered)
        {
            Console.WriteLine("recovered an interrupted migration; the pre-migration store was restored");
        }
        else if (recovery.Failure is not null)
        {
            // Announced, never swallowed. A store left half-migrated with no snapshot is a state the
            // operator has to know about; opening it anyway and hoping is how corruption becomes
            // permanent.
            Console.Error.WriteLine(recovery.Failure);
        }

        var core = WorkspaceCore.Open(workspaceId, workspacePath, dataDirectory);

        var endpoint = new DaemonEndpoint(
            workspaceId, new CapabilityRegistry(), _ => core.Store.CoreEpoch);

        DaemonOperations.Register(endpoint, () => core.Store.CoreEpoch);
        WorkspaceOperations.Register(endpoint, core.Projections);

        return (endpoint, core);
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
