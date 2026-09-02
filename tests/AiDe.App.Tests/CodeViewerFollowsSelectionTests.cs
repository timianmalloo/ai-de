using System.Threading;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.Core.Projections;
using AiDe.Testing;
using Xunit;

namespace AiDe.App.Tests;

/// <summary>
/// The Source pane follows graph selection (smoke 9-1: "select a node … nothing updates in any of
/// the source tabs", and "source worked with no workspace open"). Before this wiring nothing routed
/// a canvas selection into an open code viewer, and a viewer with no workspace showed a fake sample.
/// </summary>
public sealed class CodeViewerFollowsSelectionTests
{
    private sealed class ContentQueries : FakeWorkspaceQueries
    {
        public override Task<AiDe.Core.Projections.NodeContent> NodeContentAsync(
            string nodeId, CancellationToken ct) =>
            Task.FromResult(new AiDe.Core.Projections.NodeContent(
                nodeId, AiDe.Core.Projections.NodeContentKind.Code, "csharp",
                $"// {nodeId}\nclass Foo {{ }}"));
    }

    [Fact]
    public void SelectingANode_LoadsItsSourceIntoOpenCodeViewers()
        => OnSta(() =>
        {
            var shell = new WorkbenchShell(new ContentQueries());
            var viewer = new CodeViewerView();
            Assert.True(viewer.IsFallback);   // starts in the "Select a node" empty state

            RunSync(shell.ShowNodeInCodeViewersAsync("Shop.Order", new[] { viewer }));

            Assert.Equal("Shop.Order", viewer.NodeId);
            Assert.Contains("Shop.Order", viewer.ShownText);
        });

    [Fact]
    public void WithNoWorkspaceOpen_ShowingANodeIsANoOp_NoFakeSource()
        => OnSta(() =>
        {
            var shell = new WorkbenchShell(queries: null);   // no workspace
            var viewer = new CodeViewerView();

            RunSync(shell.ShowNodeInCodeViewersAsync("Shop.Order", new[] { viewer }));

            Assert.Null(viewer.NodeId);        // stays empty — no fabricated source
            Assert.True(viewer.IsFallback);
        });

    // With Task.FromResult providers the continuation resumes synchronously on this bare STA thread
    // (no SyncContext), but pump once to be safe.
    private static void RunSync(Task t)
    {
        while (!t.IsCompleted)
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        }

        t.GetAwaiter().GetResult();
    }

    private static void OnSta(System.Action body) =>
        Sta.Run(body, 30);
}
