using AiDe.App.Workbench;
using AiDe.Core.Ipc;
using AiDe.Core.Projections;

namespace AiDe.App.Tests;

/// <summary>
/// Ruling 85: <c>AttachWorkspace</c>'s fixture default <c>artifactRevision = "rev-1"</c> left the
/// product path — the shell attaches the workspace's observed <c>HEAD</c> (<see cref="WorkbenchShell.ResolveGitFacts"/>)
/// or the honest <see cref="WorkbenchShell.RevisionNotRecorded"/>, never a literal that merely looks
/// measured. This is why <c>EvidencePaneViewModel</c>'s status doubled to <i>"rev rev-1"</i>: the
/// value it formatted already carried the word.
/// </summary>
public sealed class TheAttachedRevisionIsObservedNotAFixtureTests
{
    private sealed class NoOpQueries : AiDe.Testing.FakeWorkspaceQueries
    {
        // AttachWorkspace's own wiring (BindContexts -> ReadAssertions -> the context map) probes
        // these two on attach; this test is about the revision, not the context surface, so empty
        // answers rather than the base class's refusal.
        public override Task<FindResult> FindAsync(string term, int maxResults, CancellationToken cancellationToken) =>
            Task.FromResult(new FindResult([], new ResultBounds(0, 0, 1024, 0, 0, 0, 0, false, null), "n/a"));

        public override Task<EvidencePage> EvidenceAsync(string? cursor, int maxAssertions, CancellationToken cancellationToken) =>
            Task.FromResult(new EvidencePage([], null, "n/a"));
    }

    /// <summary>Captures the revision a re-index closure actually receives, without a daemon or a store.</summary>
    private static (WorkbenchShell Shell, Func<string> LastRevision) Attached(string? workspaceRoot)
    {
        string? seen = null;
        var commands = new LocalWorkspaceCommands(
            refresh: (_, revision, _) => { seen = revision; return Task.FromResult(0); },
            index: (revision, _, _) => { seen = revision; return Task.FromResult(new IndexSummary(0, 0, 0, [], [])); });

        var shell = new WorkbenchShell(queries: null);
        shell.AttachWorkspace(new NoOpQueries(), dataDirectory: null, commands, workspaceRoot: workspaceRoot);

        return (shell, () => seen ?? throw new InvalidOperationException("no re-index ran yet"));
    }

    [Fact]
    public void ANonGitWorkspace_AttachesTheHonestNotRecorded_NeverTheOldFixture() => Sta.Run(() =>
    {
        var temp = Path.Combine(Path.GetTempPath(), "aide-attach-norev-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var (shell, lastRevision) = Attached(temp);
            using var _ = shell;

            shell.Controller.WorkspaceIndex!().GetAwaiter().GetResult();

            Assert.Equal(WorkbenchShell.RevisionNotRecorded, lastRevision());
            Assert.NotEqual("rev-1", lastRevision());
        }
        finally
        {
            try { Directory.Delete(temp, recursive: true); } catch (IOException) { }
        }
    });

    [Fact]
    public void AGitWorkspace_AttachesTheObservedHead_NotAFixtureLiteral() => Sta.Run(() =>
    {
        var (shell, lastRevision) = Attached(Directory.GetCurrentDirectory());
        using var _ = shell;

        shell.Controller.WorkspaceIndex!().GetAwaiter().GetResult();

        var expected = WorkbenchShell.ResolveGitFacts(Directory.GetCurrentDirectory()).Head;
        Assert.NotNull(expected);
        Assert.Equal(expected, lastRevision());
        Assert.NotEqual("rev-1", lastRevision());
    });

    /// <summary>A caller that supplies its own revision (every test elsewhere) is unaffected.</summary>
    [Fact]
    public void ACallerSuppliedRevision_PassesThroughUnchanged() => Sta.Run(() =>
    {
        string? seen = null;
        var commands = new LocalWorkspaceCommands(
            refresh: (_, revision, _) => { seen = revision; return Task.FromResult(0); },
            index: (revision, _, _) => { seen = revision; return Task.FromResult(new IndexSummary(0, 0, 0, [], [])); });

        using var shell = new WorkbenchShell(queries: null);
        shell.AttachWorkspace(new NoOpQueries(), dataDirectory: null, commands, artifactRevision: "rev-1");

        shell.Controller.WorkspaceIndex!().GetAwaiter().GetResult();

        Assert.Equal("rev-1", seen);
    });
}
