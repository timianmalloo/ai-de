using AiDe.App.Workbench;
using AiDe.Core.Presentation;
using AiDe.Core.Projections;
using AiDe.Core.Workbench;
using AiDe.Testing;
using System.Windows;
using System.Windows.Controls;

namespace AiDe.App.Tests;

/// <summary>
/// US-C6's positive oracle (Ruling 61): the Evidence master (<c>view</c>) and Provenance detail
/// (<c>inspector</c>) kinds render DIFFERENT content from the same store — never two copies of the
/// same list.
/// </summary>
/// <remarks>
/// <para><b>The red this pins.</b> Before this slice both kinds' <c>SurfaceKind.Build</c> delegates
/// called the identical <c>SurfaceContentFactory.Evidence(Surface)</c> method
/// (<c>SurfaceContentFactory.cs</c>, pre-SH-3) — confirmed by direct inspection of that file before
/// it was edited. A test asserting the two kinds differ would have failed on that code: both trees
/// were the same evidence list, exactly as the file's own "FINDING, NOT A FIX" comment (still
/// present, above the two rows) records. SH-3's fix gives the factory a shared, testable
/// <see cref="EvidenceSelectionSource"/> seam and splits the build into
/// <c>EvidenceMaster</c>/<c>EvidenceDetail</c>.</para>
/// </remarks>
public sealed class EvidenceMasterDetailTests
{
    private static T OnStaThread<T>(Func<T> work) => Sta.Run<T>(work, 60);

    private static FrameworkElement Unwrap(FrameworkElement content) =>
        content is Border { Child: FrameworkElement inner } ? inner : content;

    /// <summary>Three rows (R1..R3), each describable with a distinguishable node id.</summary>
    private sealed class MultiRowQueries : FakeWorkspaceQueries
    {
        public override Task<FindResult> FindAsync(string term, int maxResults, CancellationToken ct) =>
            Task.FromResult(new FindResult(
                [
                    new FindMatch("R1", "kind", "R1", AuthorshipOrigin.RepositoryArtifact),
                    new FindMatch("R2", "kind", "R2", AuthorshipOrigin.RepositoryArtifact),
                    new FindMatch("R3", "kind", "R3", AuthorshipOrigin.RepositoryArtifact),
                ],
                new ResultBounds(3, 3, 1024, 3, 0, 0, 0, false, null),
                "rev-1"));

        public override Task<DescribeResult> DescribeAsync(string nodeId, int maxNeighbors, CancellationToken ct) =>
            Task.FromResult(new DescribeResult(
                new NodeView(nodeId, "kind", nodeId), [],
                new ResultBounds(1, 1, 1024, 1, 0, 0, 0, false, null), "rev-1"));
    }

    private static void PumpUntil(Func<bool> ready)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10);
        while (DateTimeOffset.UtcNow < deadline && !ready())
        {
            var frame = new System.Windows.Threading.DispatcherFrame();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                new Action(() => frame.Continue = false),
                System.Windows.Threading.DispatcherPriority.Background);
            System.Windows.Threading.Dispatcher.PushFrame(frame);
            Thread.Sleep(5);
        }
    }

    private static IEnumerable<string> AllText(DependencyObject root)
    {
        if (root is TextBlock t) { yield return t.Text; }

        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            foreach (var s in AllText(System.Windows.Media.VisualTreeHelper.GetChild(root, i)))
            {
                yield return s;
            }
        }
    }

    [Fact]
    public void TheMasterAndDetailPanes_RenderDifferentContent_AndTrackSelection()
    {
        OnStaThread(() =>
        {
            var selection = new EvidenceSelectionSource();
            var factory = new SurfaceContentFactory(new MultiRowQueries(), evidenceSelection: selection);

            var master = Unwrap(factory.Create(new Surface("evidence", "view", "Evidence")));
            var masterStack = Assert.IsType<StackPanel>(master);
            var list = masterStack.Children.OfType<ListBox>().Single();
            PumpUntil(() => list.ItemsSource is not null);
            Assert.Equal(3, list.ItemsSource!.Cast<object>().Count());

            var detail = Unwrap(factory.Create(new Surface("provenance", "inspector", "Provenance")));
            var detailStack = Assert.IsType<StackPanel>(detail);

            // Given no selection, the detail shows the §C4 empty copy verbatim, and neither pane
            // carries the other's shape (no list in the detail; no detail heading in the master).
            Assert.Contains(EvidencePaneViewModel.EmptySelectionMessage, AllText(detailStack));
            Assert.Empty(detailStack.Children.OfType<ListBox>());
            Assert.DoesNotContain("What it is", AllText(masterStack));

            // When R2 is selected in the master (the SAME gesture a click drives — SelectionChanged),
            // the detail shows R2's id and its four SelectAsync sections, and no list of rows.
            list.SelectedItem = list.ItemsSource!.Cast<EvidenceRow>().Single(r => r.NodeId == "R2");
            PumpUntil(() => AllText(detailStack).Contains("What it is"));

            Assert.Contains("R2", AllText(detailStack));
            Assert.Contains("What it is", AllText(detailStack));
            Assert.Contains("Confidence and provenance", AllText(detailStack));
            Assert.Contains("Related nodes", AllText(detailStack));
            Assert.Contains("Source", AllText(detailStack));
            Assert.Empty(detailStack.Children.OfType<ListBox>());

            // The master is unaffected: still the full list, no detail section.
            Assert.Equal(3, list.ItemsSource!.Cast<object>().Count());
            Assert.DoesNotContain("What it is", AllText(masterStack));

            // When R3 is then selected, the detail changes to R3.
            list.SelectedItem = list.ItemsSource!.Cast<EvidenceRow>().Single(r => r.NodeId == "R3");
            PumpUntil(() => AllText(detailStack).Contains("R3"));

            Assert.Contains("R3", AllText(detailStack));
            Assert.DoesNotContain("R2", AllText(detailStack));

            return 0;
        });
    }

    [Fact]
    public void ADetailPaneBuiltAfterASelectionAlreadyExists_ShowsItImmediately()
    {
        // The detail can open SECOND (Provenance opened after Evidence already has a selection) —
        // it must not wait for the next click to catch up.
        OnStaThread(() =>
        {
            var selection = new EvidenceSelectionSource();
            var factory = new SurfaceContentFactory(new MultiRowQueries(), evidenceSelection: selection);
            selection.Select("R1");

            var detail = Unwrap(factory.Create(new Surface("provenance", "inspector", "Provenance")));
            var detailStack = Assert.IsType<StackPanel>(detail);
            PumpUntil(() => AllText(detailStack).Contains("R1"));

            Assert.Contains("R1", AllText(detailStack));
            return 0;
        });
    }
}
