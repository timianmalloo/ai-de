using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The environment contract reaches a terminal even when no workspace ever attaches.
/// </summary>
/// <remarks>
/// <para><b>The defect.</b> <c>TerminalSurface.EnvironmentFor</c> was assigned only inside
/// <see cref="WorkbenchShell.AttachWorkspace"/>, and both call sites in <c>MainWindow</c> skip it
/// when the view model hands back no queries — the daemon unreachable, no default workspace, an
/// open that failed. The shell stays on its first-run surface and terminals still open, with the
/// hook null and every pane launched with an empty environment.</para>
///
/// <para><b>Per-run, not per-pane, which is what identified it.</b> The launch log shows
/// <c>environmentCount</c> 5 in runs on either side and 0 for every pane in the runs between —
/// including a plain <c>terminal-1</c> eleven seconds before the agent pane that first drew
/// attention. A pane-level cause cannot produce that; a run-level gate can. Found by another
/// session ordering the records rather than reading the one that looked interesting.</para>
///
/// <para><b>Why it matters beyond a missing variable.</b> Registration happens without the
/// environment, so the session still appears in the watcher and nothing looks broken. What silently
/// does not happen is everything that needs <c>AIDE_CONTRACT_LOG</c>: the model <c>update</c> event
/// and the <c>episode-open</c>/<c>episode-close</c> kinds. The agent is observed and cannot
/// participate.</para>
/// </remarks>
public sealed class EnvironmentContractSurvivesNoWorkspaceTests
{
    /// <summary>
    /// A shell built with no workspace still supplies an environment for its terminals.
    /// </summary>
    /// <remarks>
    /// This is the whole invariant. The hook reads current state on every call, so assigning it at
    /// construction costs nothing and removes the gate — a later <c>AttachWorkspace</c> changes what
    /// it returns, never whether it exists.
    /// </remarks>
    [Fact]
    public void AShellWithNoWorkspaceStillSuppliesTheEnvironmentContract()
    {
        var environment = Sta.Run(() =>
        {
            var previous = TerminalSurface.EnvironmentFor;
            TerminalSurface.EnvironmentFor = null;
            try
            {
                _ = new WorkbenchShell(queries: null);
                return TerminalSurface.EnvironmentFor?.Invoke("agent:claude#abc123");
            }
            finally
            {
                TerminalSurface.EnvironmentFor = previous;
            }
        });

        Assert.NotNull(environment);
        Assert.Equal("agent:claude#abc123", environment!["AIDE_SESSION"]);
        Assert.Equal("agent:claude#abc123", environment["AIDE_TERMINAL_ID"]);
        Assert.True(environment.ContainsKey("AIDE_CONTRACT_VERSION"));
    }

    /// <summary>
    /// With no workspace, the variables that would have to be invented are absent.
    /// </summary>
    /// <remarks>
    /// <para><c>ResolveGitFacts</c> returns the display name <c>"workspace"</c> as its fallback path
    /// when there is no root. The identity record must send something, because the attribute is
    /// required there. An environment variable must not: an agent reading
    /// <c>AIDE_WORKTREE=workspace</c> would take it for a path, and absent is the honest form —
    /// the same rule <c>AIDE_BRANCH</c> already followed.</para>
    ///
    /// <para>This guards the fix against the obvious wrong version of itself: making the hook always
    /// present is only correct if what it returns stays truthful.</para>
    /// </remarks>
    [Fact]
    public void WithNoWorkspaceTheUnknowableVariablesAreAbsentRatherThanInvented()
    {
        var environment = Sta.Run(() =>
        {
            var previous = TerminalSurface.EnvironmentFor;
            TerminalSurface.EnvironmentFor = null;
            try
            {
                _ = new WorkbenchShell(queries: null);
                return TerminalSurface.EnvironmentFor?.Invoke("terminal-1");
            }
            finally
            {
                TerminalSurface.EnvironmentFor = previous;
            }
        });

        Assert.NotNull(environment);
        Assert.False(environment!.ContainsKey("AIDE_WORKSPACE"));
        Assert.False(environment.ContainsKey("AIDE_WORKTREE"));
        Assert.False(environment.ContainsKey("AIDE_BRANCH"));

        // Not launched as an agent, so there is no harness to name.
        Assert.False(environment.ContainsKey("AIDE_AGENT"));
        Assert.False(environment.ContainsKey("AIDE_HARNESS"));
    }
}
