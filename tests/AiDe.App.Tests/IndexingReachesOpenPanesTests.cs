using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// A command that changes the store tells the open panes, so what is on screen is what was written.
/// </summary>
/// <remarks>
/// <para><b>DC-045, made checkable.</b> A re-index of a real repository wrote 10,242 assertions —
/// the entire knowledge half of the workspace — and every open pane kept rendering the projection it
/// had fetched when it loaded. The user re-indexed, read a message saying it had worked, and looked
/// at a Knowledge count of 0 taken from a graph twenty-six seconds out of date. Both halves were
/// working; nothing joined them.</para>
///
/// <para>These assert the SIGNAL, at the seam where it was missing. Whether a given pane re-reads
/// cheaply or expensively belongs to whoever owns the pane; that it is TOLD does not.</para>
/// </remarks>
public sealed class IndexingReachesOpenPanesTests
{
    private static (WorkbenchController Controller, RecordingAnnouncer Announcer) Build()
    {
        var service = new LayoutService();
        var announcer = new RecordingAnnouncer();
        return (new WorkbenchController(service, announcer), announcer);
    }

    [Fact]
    public async Task IndexingRaisesTheChangeSignal()
    {
        var (controller, announcer) = Build();
        var told = new TaskCompletionSource();

        controller.WorkspaceDataChanged += () => told.TrySetResult();
        controller.WorkspaceIndex = () => Task.FromResult("Indexed 66 of 66 scope(s).");

        Assert.True(controller.Execute("workspace.indexSolution"));

        await told.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains(announcer.Messages, a => a.Contains("66", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReIndexingEverythingRaisesItToo()
    {
        // The forced path is a separate command with its own completion handling, and it is the one
        // a user reaches for precisely when they believe the graph is wrong.
        var (controller, _) = Build();
        var told = new TaskCompletionSource();

        controller.WorkspaceDataChanged += () => told.TrySetResult();
        controller.WorkspaceReindexAll = () => Task.FromResult("Re-indexed.");

        Assert.True(controller.Execute("workspace.reindexAll"));

        await told.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RefreshingTheWorkspaceRaisesItToo()
    {
        var (controller, _) = Build();
        var told = new TaskCompletionSource();

        controller.WorkspaceDataChanged += () => told.TrySetResult();
        controller.WorkspaceRefresh = () => Task.FromResult("Refreshed.");

        Assert.True(controller.Execute("workspace.refresh"));

        await told.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AFailedIndexDoesNotClaimTheDataChanged()
    {
        // A pane that re-reads after a failure shows the same thing twice and costs a full query for
        // it. Worse, a signal that fires whether or not anything happened stops meaning anything.
        var (controller, announcer) = Build();
        var raised = 0;

        controller.WorkspaceDataChanged += () => Interlocked.Increment(ref raised);
        controller.WorkspaceIndex = () => throw new InvalidOperationException("the store is locked");

        Assert.True(controller.Execute("workspace.indexSolution"));

        await WaitFor(() => announcer.Messages.Any(a => a.Contains("locked", StringComparison.Ordinal)));
        Assert.Equal(0, Volatile.Read(ref raised));
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++) await Task.Delay(20);
        Assert.True(condition(), "the command never reported an outcome");
    }
}
