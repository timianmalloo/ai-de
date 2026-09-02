using System.Threading;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.Core.Projections;
using AiDe.Testing;
using Xunit;

namespace AiDe.App.Tests;

/// <summary>
/// A sequence diagram follows the "Sequence diagram" action (smoke 9-1 #14: "sequence diagram — no
/// context"). Phase E wires <see cref="WorkbenchShell.ShowNodeInSequenceDiagramsAsync"/> to Core's
/// ordered interaction feed so an opened sequence pane draws a real node's calls rather than a
/// scaffold. The interaction feed — not the deduped <c>calls</c> edges — is used, so repeated and
/// ordered messages survive.
/// </summary>
public sealed class SequenceDiagramFollowsSelectionTests
{
    private sealed class InteractionQueries : FakeWorkspaceQueries
    {
        public override Task<InteractionResult> InteractionAsync(
            string nodeId, int maxMessages, CancellationToken ct) =>
            Task.FromResult(new InteractionResult(
                nodeId,
                new[]
                {
                    new InteractionMessage(1, nodeId, "Shop.Repo", "Load", "12:5"),
                    new InteractionMessage(2, nodeId, "Shop.Mailer", "Send", "13:5"),
                },
                Truncated: false,
                new ResultBounds(0, 0, 0, 0, 0, 0, 0, false, null),
                SourceRevision: "r1"));
    }

    [Fact]
    public void ShowingANode_PopulatesOpenSequenceDiagrams()
        => OnSta(() =>
        {
            var shell = new WorkbenchShell(new InteractionQueries());
            var surface = new SequenceDiagramSurface();
            Assert.True(surface.IsEmpty);   // starts in the empty state

            RunSync(shell.ShowNodeInSequenceDiagramsAsync("Shop.Order", new[] { surface }));

            Assert.Equal("Shop.Order", surface.NodeId);
            Assert.False(surface.IsEmpty);
            Assert.True(surface.ParticipantCount > 0);
            Assert.True(surface.MessageCount > 0);
        });

    [Fact]
    public void WithNoWorkspaceOpen_ShowingANodeIsANoOp()
        => OnSta(() =>
        {
            var shell = new WorkbenchShell(queries: null);   // no workspace
            var surface = new SequenceDiagramSurface();

            RunSync(shell.ShowNodeInSequenceDiagramsAsync("Shop.Order", new[] { surface }));

            Assert.Null(surface.NodeId);   // stays empty — nothing fabricated
            Assert.True(surface.IsEmpty);
        });

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
