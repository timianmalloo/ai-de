using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// An agent launched inside AI-DE carries the AI-Forward Pack's identity, in a form the pack can
/// write to a file.
/// </summary>
/// <remarks>
/// <para><b>Why this is not cosmetic.</b> <c>coord-core.py</c> reads <c>AGENT_SESSION</c> to know
/// who is acting. Without it, <c>check</c> reports "AGENT_SESSION is unset, so this session has no
/// identity to check" — and the <b>precommit guard prints an advisory and returns 0</b>. The
/// boundary that stops one session committing over another's files silently becomes a notice.</para>
///
/// <para>That degradation is a deliberate trade inside the pack: the hook runs on every commit
/// including a human's by hand, and a floor that refuses all of them gets deleted rather than
/// adopted. Which is exactly why the whole guarantee rests on the launcher supplying the identity,
/// and why the fix belongs here.</para>
///
/// <para><b>Observed the day this was written:</b> a squash merge swept another session's entire
/// pack refresh into a commit describing only the merger's work. Nothing objected at any point.
/// Three sessions then spent an afternoon identifying an "unreachable" fourth session that had
/// simply never been given an identity to register with.</para>
/// </remarks>
public sealed class AgentCarriesThePacksIdentityTests
{
    /// <summary>
    /// The pack's session id is path-safe, because the pack uses it as a file name.
    /// </summary>
    /// <remarks>
    /// <c>coord-core.py</c> writes <c>logdir / "{}.jsonl".format(session)</c> and reads it back with
    /// <c>glob("*.jsonl")</c>. Handing it <c>agent:claude#a90b5c</c> on Windows makes the write an
    /// NTFS alternate data stream the glob cannot enumerate — DC-086 exactly, reintroduced inside
    /// the pack where we do not own the fix. This is the assertion that stops the raw surface id
    /// being passed through as an "obvious simplification".
    /// </remarks>
    [Theory]
    [InlineData("agent:claude#a90b5c")]
    [InlineData("agent:copilot#1f12c8")]
    [InlineData("terminal-1")]
    [InlineData("s-terminal")]
    public void ThePackSessionIdIsAlwaysSafeAsAFileName(string surfaceId)
    {
        var id = WorkbenchShell.PackSessionId(surfaceId);

        Assert.DoesNotContain(':', id);
        Assert.All(
            Path.GetInvalidFileNameChars(),
            invalid => Assert.DoesNotContain(invalid, id));
        Assert.Equal(id, Path.GetFileName(id));
    }

    /// <summary>Distinct panes stay distinct sessions, and the same pane is stable across calls.</summary>
    /// <remarks>
    /// Sanitising alone would map two panes onto one identity, which is quieter than the defect it
    /// fixes: two agents' coordination events would fold into one record and each would appear to
    /// hold the other's claims.
    /// </remarks>
    [Fact]
    public void DistinctPanesGetDistinctIdsAndTheSamePaneIsStable()
    {
        Assert.NotEqual(
            WorkbenchShell.PackSessionId("agent:claude#a90b5c"),
            WorkbenchShell.PackSessionId("agent:claude#fb96e3"));

        Assert.NotEqual(
            WorkbenchShell.PackSessionId("agent:claude#a90b5c"),
            WorkbenchShell.PackSessionId("agent:copilot#a90b5c"));

        Assert.Equal(
            WorkbenchShell.PackSessionId("agent:claude#a90b5c"),
            WorkbenchShell.PackSessionId("agent:claude#a90b5c"));
    }

    /// <summary>
    /// The variables actually reach a terminal's environment, not just the helper.
    /// </summary>
    /// <remarks>
    /// Asserted through the shell's own hook rather than by calling <c>PackSessionId</c> again: a
    /// correct id that never reaches the child process is the whole defect, and a test that only
    /// exercises the helper would pass while the environment stayed empty. That is the same shape
    /// as DC-084, whose fix this test sits directly on top of.
    /// </remarks>
    [Fact]
    public void TheVariablesReachTheEnvironmentATerminalIsLaunchedWith()
    {
        var environment = Sta.Run(() =>
        {
            var previous = TerminalSurface.EnvironmentFor;
            TerminalSurface.EnvironmentFor = null;
            try
            {
                _ = new WorkbenchShell(queries: null);
                return TerminalSurface.EnvironmentFor?.Invoke("agent:claude#a90b5c");
            }
            finally
            {
                TerminalSurface.EnvironmentFor = previous;
            }
        });

        Assert.NotNull(environment);
        Assert.Equal(
            WorkbenchShell.PackSessionId("agent:claude#a90b5c"),
            environment!["AGENT_SESSION"]);
        Assert.False(string.IsNullOrWhiteSpace(environment["AGENT_NAME"]));
    }

    /// <summary>
    /// The id still reads as the pane it came from.
    /// </summary>
    /// <remarks>
    /// Derived rather than random so the pack's <c>.agents</c> record and Loomkeeper's watcher
    /// record describe the same session. A GUID would be path-safe and correlate with nothing,
    /// which defeats the point of bridging the two at all — an operator looking at
    /// <c>.agents/sessions</c> should be able to tell which pane wrote it.
    /// </remarks>
    [Fact]
    public void TheIdStillNamesTheAgentAndThePane()
    {
        var id = WorkbenchShell.PackSessionId("agent:claude#a90b5c");

        Assert.StartsWith("aide-", id);
        Assert.Contains("claude", id);
        Assert.Contains("a90b5c", id);
    }
}
