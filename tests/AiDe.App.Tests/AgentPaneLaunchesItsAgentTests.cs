using System.Text.Json;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// An <c>agent:</c> pane launches its agent, and the launch decision says so.
/// </summary>
/// <remarks>
/// <para><b>The defect.</b> <c>TerminalSurface.Executable</c> was <c>{ get; init; }</c>, set by an
/// object initializer at the one construction site. An object initializer runs <b>after</b> the
/// constructor body, and the constructor starts the session — so <c>StartAsync</c> read the property
/// while it was still null, synchronously, before its first await. Every pane launched as a plain
/// shell.</para>
///
/// <para><b>Measured, not inferred:</b> 243 <c>terminal.start</c> records across two days,
/// <c>executable</c> null in all 243 — including the one whose surface id was
/// <c>agent:claude#aa8dcb</c> and whose tab read "Claude Code".</para>
///
/// <para><b>Why it defeated two rounds of reading.</b> The chain is individually correct at every
/// step: null executable → <c>launch</c> falls back to the shell → no readiness profile matches
/// <c>powershell</c> → <c>ShellIntegrationMode.PowerShell</c> rather than
/// <c>PowerShellHostedAgent</c> → <c>AgentCommandLine</c> is never reached. A fix to
/// <c>AgentCommandLine</c> was proven correct in isolation and could not have changed anything the
/// user saw, because the branch that calls it was never taken. Only the recorded decision
/// distinguished those two states.</para>
///
/// <para><b>This asserts the DECISION, not the process.</b> Launching a real agent would need one
/// installed and would start a billable session; what went wrong was never the launching, it was
/// which command was chosen.</para>
/// </remarks>
public sealed class AgentPaneLaunchesItsAgentTests
{
    /// <summary>The <c>terminal.start</c> records a surface produces while it is built.</summary>
    private static List<JsonElement> LaunchRecords(string surfaceId, string title) => Sta.Run(() =>
    {
        var lines = new List<string>();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = lines.Add;

        try
        {
            // The real factory, so the test cannot pass by constructing the surface the way the
            // product does not. The construction path IS the subject.
            using var content = new SurfaceContentFactory(queries: null)
                .Create(new Surface(surfaceId, "terminal", title)) as IDisposable;
        }
        catch (Exception ex) when (ex is not Xunit.Sdk.XunitException)
        {
            // A missing ConPTY or shell is an environment problem, not a finding. The decision is
            // recorded before the process is started, so the record exists either way.
            _ = ex;
        }
        finally
        {
            WorkbenchDiagnostics.Sink = previous;
        }

        return lines
            .Select(l => JsonDocument.Parse(l).RootElement)
            .Where(e => e.TryGetProperty("evt", out var v) && v.GetString() == "terminal.start")
            .ToList();
    });

    private static string? Text(JsonElement record, string property) =>
        record.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    [Fact]
    public void AnAgentPaneResolvesItsExecutableFromTheSurfaceId()
    {
        // THE DEFECT, at the point it goes wrong. Everything downstream is a consequence of this
        // one value, and it was null for 243 consecutive launches.
        var record = Assert.Single(LaunchRecords("agent:claude#aa8dcb", "Claude Code"));

        Assert.Equal("claude", Text(record, "executable"));
    }

    [Fact]
    public void AnAgentPaneHostsTheAgentRatherThanOpeningAShell()
    {
        // The consequence that reached the user: a tab titled "Claude Code" showing a PowerShell
        // prompt. PowerShellHostedAgent is the only mode that ever calls AgentCommandLine.
        var record = Assert.Single(LaunchRecords("agent:claude#aa8dcb", "Claude Code"));

        Assert.Equal("PowerShellHostedAgent", Text(record, "integration"));

        Assert.True(
            record.GetProperty("readinessProfile").GetBoolean(),
            "no readiness profile resolved, so the launch fell back to shell mode and the agent was "
            + "never hosted — which is the whole defect, one step downstream of the executable");
    }

    [Fact]
    public void APlainTerminalIsStillAPlainShell()
    {
        // The DC-016 guard, and it is not hypothetical: hardcoding hosted-agent mode would satisfy
        // both tests above while turning the default layout's terminal into an agent launch. 231 of
        // the 243 measured records are that pane, and it is correctly plain.
        var record = Assert.Single(LaunchRecords("terminal-1", "Terminal"));

        Assert.Null(Text(record, "executable"));
        Assert.Equal("PowerShell", Text(record, "integration"));
    }
}
