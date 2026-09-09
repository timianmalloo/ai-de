using System.Diagnostics;
using AiDe.Core.AgentPlane;
using AiDe.Core.Terminal;

namespace AiDe.Core.Tests.AgentPlane;

/// <summary>
/// The child process the ACP peer speaks to — started, inspected, and <b>reaped</b>.
/// </summary>
/// <remarks>
/// <para><b>Why a real process.</b> "The child is gone" is not a statement any fake can make. An
/// adapter that survives its lane keeps a subscription slot, a file lock and a credential session
/// open, and the failure is invisible from inside the parent — which is exactly the shape of failure
/// that needs an oracle outside it.</para>
///
/// <para><b>Why the environment is inspected here.</b> DC-027 measured this machine's PATH at 22,297
/// characters, past the size <c>cmd.exe</c> silently drops — so a child launched through any
/// <c>.cmd</c> shim starts with an empty PATH and cannot find node, git, or itself, while the
/// surface looks healthy. <see cref="EnvironmentHealth"/> is that control, already written; a lane
/// that rediscovered it would be paying for the same measurement twice.</para>
/// </remarks>
public sealed class AcpEngineProcessTests
{
    private static EngineLaunch Hang() => new(AcpProbeLauncher.Executable(), ["--hang"]);

    /// <summary>Disposing the lane's engine leaves no process behind.</summary>
    [Fact]
    public void DisposingTheEngineReapsTheChildRatherThanOrphaningIt()
    {
        var engine = AcpEngineProcess.Start(Hang(), AppContext.BaseDirectory);
        var processId = engine.ProcessId;
        Assert.False(engine.HasExited);

        engine.Dispose();

        Assert.True(engine.HasExited);
        Assert.False(StillRunning(processId), $"process {processId} outlived the lane that started it");
    }

    /// <summary>The engine's environment is inspected before the lane runs, and findings are surfaced.</summary>
    [Fact]
    public void AnEnvironmentFindingIsSurfacedBeforeTheLaneRunsRatherThanDiscoveredByTheAgent()
    {
        var diagnostics = new List<string>();

        using var engine = AcpEngineProcess.Start(
            Hang(),
            AppContext.BaseDirectory,
            diagnostics.Add,
            () => ["PATH is 22,297 characters, past the size cmd.exe carries"]);

        Assert.Single(engine.EnvironmentFindings);
        Assert.Contains(diagnostics, d => d.Contains("cmd.exe carries", StringComparison.Ordinal));
    }

    /// <summary>The default inspector is the existing control, not a second copy of its rules.</summary>
    [Fact]
    public void TheDefaultEnvironmentInspectorIsEnvironmentHealth()
    {
        using var engine = AcpEngineProcess.Start(Hang(), AppContext.BaseDirectory);

        Assert.Equal(EnvironmentHealth.Inspect(), engine.EnvironmentFindings);
    }

    /// <summary>A command that is not there fails as a named refusal, not an unhandled Win32 error.</summary>
    [Fact]
    public void AnEngineExecutableThatIsNotThereIsANamedRefusal()
    {
        var error = Assert.Throws<AgentPlaneException>(
            () => AcpEngineProcess.Start(
                new EngineLaunch("aide-no-such-engine-binary", []), AppContext.BaseDirectory));

        Assert.Equal(AgentPlaneErrorCodes.EngineDidNotStart, error.Code);
        Assert.Contains("aide-no-such-engine-binary", error.Message, StringComparison.Ordinal);
    }

    private static bool StillRunning(int processId)
    {
        try
        {
            using var found = Process.GetProcessById(processId);
            return !found.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
