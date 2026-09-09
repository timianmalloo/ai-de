using System.Diagnostics;
using AiDe.Core.Terminal;

namespace AiDe.Core.AgentPlane;

/// <summary>
/// The engine child process a governed lane speaks ACP to — started, inspected, and <b>reaped</b>.
/// </summary>
/// <remarks>
/// <para><b>Separate from <see cref="AcpPeer"/> because a process is not a protocol.</b> The peer
/// works on any pair of streams, which is what lets the whole framing and correlation contract be
/// tested without spawning anything. This owns the part that can leak: a live subprocess.</para>
///
/// <para><b>The environment is inspected before the lane runs</b>, using DC-027's existing control
/// rather than a second copy of its rules. That measurement found this machine's PATH at 22,297
/// characters — past the size <c>cmd.exe</c> silently drops — so a child launched through any
/// <c>.cmd</c> shim starts with an empty PATH and cannot find node, git or itself, while everything
/// on the surface looks healthy. Findings are reported, never repaired: PATH belongs to the person
/// whose machine it is, and a tool that quietly rewrites it has hidden the problem from the only
/// person who can fix it.</para>
///
/// <para><b>Disposal kills the whole tree.</b> The adapter is <c>node</c> running a package that
/// itself spawns the <c>claude</c> CLI, so killing the parent alone leaves the grandchild holding a
/// credential session open with nothing attached to its stdio.</para>
/// </remarks>
public sealed class AcpEngineProcess : IDisposable
{
    private readonly Process _process;
    private readonly Action<string> _diagnostics;
    private bool _reaped;

    private AcpEngineProcess(Process process, IReadOnlyList<string> environmentFindings, Action<string> diagnostics)
    {
        _process = process;
        _diagnostics = diagnostics;
        EnvironmentFindings = environmentFindings;
    }

    /// <summary>The child's process id, for a report that has to name what was left behind.</summary>
    public int ProcessId => _process.Id;

    /// <summary>Whether the child has ended.</summary>
    /// <remarks>
    /// Answerable after disposal too. <see cref="Process.HasExited"/> throws once the object is
    /// disposed, which would make "did the lane leave a process behind?" unanswerable at exactly the
    /// moment it is asked.
    /// </remarks>
    public bool HasExited => _reaped || _process.HasExited;

    /// <summary>The child's stdout — the peer's input.</summary>
    public TextReader Output => _process.StandardOutput;

    /// <summary>The child's stdin — the peer's output, and the protocol channel.</summary>
    public TextWriter Input => _process.StandardInput;

    /// <summary>What the environment inspection found. Empty means healthy, not unmeasured.</summary>
    public IReadOnlyList<string> EnvironmentFindings { get; }

    /// <summary>
    /// Starts the engine.
    /// </summary>
    /// <param name="launch">What to run, from <see cref="EngineCatalog.ResolveLaunch"/>.</param>
    /// <param name="workingDirectory">Where to run it. Not the ACP session cwd, which is separate.</param>
    /// <param name="diagnostics">Where the child's stderr and any environment finding go. Defaults to stderr.</param>
    /// <param name="inspectEnvironment">
    /// The environment probe. Defaults to <see cref="EnvironmentHealth.Inspect"/> — injectable only
    /// so the surfacing can be tested without a broken machine.
    /// </param>
    /// <exception cref="AgentPlaneException">
    /// <see cref="AgentPlaneErrorCodes.EngineDidNotStart"/> when the executable is not there or the
    /// operating system refuses. A named refusal rather than a raw Win32 exception, because "node is
    /// not on the PATH" is a fact an operator can act on.
    /// </exception>
    public static AcpEngineProcess Start(
        EngineLaunch launch,
        string workingDirectory,
        Action<string>? diagnostics = null,
        Func<IReadOnlyList<string>>? inspectEnvironment = null)
    {
        ArgumentNullException.ThrowIfNull(launch);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        var report = diagnostics ?? Console.Error.WriteLine;
        var findings = (inspectEnvironment ?? (() => EnvironmentHealth.Inspect()))();
        foreach (var finding in findings)
        {
            report("environment: " + finding);
        }

        var info = new ProcessStartInfo(launch.FileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in launch.Arguments)
        {
            info.ArgumentList.Add(argument);
        }

        Process? process;
        try
        {
            process = Process.Start(info);
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            throw new AgentPlaneException(
                AgentPlaneErrorCodes.EngineDidNotStart,
                $"the engine '{launch.FileName}' could not be started: {error.Message}");
        }

        if (process is null)
        {
            throw new AgentPlaneException(
                AgentPlaneErrorCodes.EngineDidNotStart,
                $"the engine '{launch.FileName}' did not start, and the operating system gave no reason");
        }

        var engine = new AcpEngineProcess(process, findings, report);
        engine.PumpStandardError();
        return engine;
    }

    /// <summary>Waits for the child to end on its own.</summary>
    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
        => _process.WaitForExitAsync(cancellationToken);

    /// <summary>
    /// Ends the lane's engine, whole tree, and waits until it is actually gone.
    /// </summary>
    /// <remarks>
    /// The wait is the part that matters: a kill request that is not waited on turns "the child is
    /// dead" into a claim rather than an observation, and the caller has no way to tell the
    /// difference.
    /// </remarks>
    public void Dispose()
    {
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit();
            }
        }
        catch (InvalidOperationException)
        {
            // It exited between the check and the kill. That is the outcome we wanted.
        }
        catch (System.ComponentModel.Win32Exception error)
        {
            _diagnostics($"acp: could not reap engine process {ProcessId}: {error.Message}");
        }

        _reaped = true;
        _process.Dispose();
    }

    /// <summary>
    /// Drains the child's stderr in the background.
    /// </summary>
    /// <remarks>
    /// <b>Not optional.</b> A child that fills its stderr pipe while the parent reads only stdout
    /// blocks in the write and stops answering — a deadlock that reads as a hung agent. The same bug
    /// <c>ProcessRunner</c> avoids by reading both streams before waiting.
    /// </remarks>
    private void PumpStandardError() => _ = Task.Run(async () =>
    {
        try
        {
            while (await _process.StandardError.ReadLineAsync() is { } line)
            {
                if (line.Length > 0)
                {
                    _diagnostics("engine stderr: " + line);
                }
            }
        }
        catch (Exception error) when (error is IOException or ObjectDisposedException)
        {
            // The child went away mid-read. Its exit is reported by the peer's own end-of-stream.
        }
    });
}
