using AiDe.App.Workbench;
using AiDe.Core.Facts;
using AiDe.Core.Presentation;
using AiDe.Core.Projections;
using AiDe.Core.Workbench;
using AiDe.Testing;
using System.Windows;
using System.Windows.Controls;

namespace AiDe.App.Tests;

/// <summary>
/// Ruling 94 (F-C): the <c>inspector</c> (Provenance) kind is retired from the product — a kind that
/// was only ever the detail half of one pair has no host once the pair is cut — and its three fields
/// (origin · extractor · rev, the line <c>EvidencePaneViewModel.SelectAsync</c> builds) render as a
/// <b>second muted line under the selected Evidence row</b>: a detail-on-select inside the master,
/// never a second pane. Ruling 61's "renders the selected row's detail, never a second list" is
/// satisfied by the row; its owed "two kinds render different content" test is withdrawn with the
/// second kind.
/// </summary>
/// <remarks>
/// <b>Red first:</b> the master's row template held one line; selecting a row changed a selection
/// seam that only the retired detail pane listened to, so the master showed no provenance at all.
/// </remarks>
public sealed class EvidenceMasterDetailTests
{
    private static T OnStaThread<T>(Func<T> work) => Sta.Run<T>(work, 60);

    private static FrameworkElement Unwrap(FrameworkElement content) =>
        content is Border { Child: FrameworkElement inner } ? inner : content;

    /// <summary>Three rows (R1..R3); R2 describes with one extracted neighbour, R3 with none.</summary>
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
                "51e806f8"));

        public override Task<DescribeResult> DescribeAsync(string nodeId, int maxNeighbors, CancellationToken ct) =>
            Task.FromResult(new DescribeResult(
                new NodeView(nodeId, "kind", nodeId),
                nodeId == "R2"
                    ?
                    [
                        new EdgeView("R2", "calls", "R9", VerificationStatus.Verified, EvidenceOrigin.Static, "51e806f8+x3",
                            new Provenance("src/R2.cs", "12:1", "csharp", "1.4", DateTimeOffset.UnixEpoch)),
                    ]
                    : [],
                new ResultBounds(1, 1, 1024, 1, 0, 0, 0, false, null), "51e806f8"));
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

    private static IEnumerable<TextBlock> TextBlocks(DependencyObject root)
    {
        if (root is TextBlock t) { yield return t; }

        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            foreach (var s in TextBlocks(System.Windows.Media.VisualTreeHelper.GetChild(root, i)))
            {
                yield return s;
            }
        }
    }

    /// <summary>The realized container of a row, with its visible text lines — the row as the operator sees it.</summary>
    private static (ListBoxItem Item, List<string> VisibleLines) Row(ListBox list, string nodeId)
    {
        var row = list.ItemsSource!.Cast<EvidenceRowItem>().Single(i => i.Row.NodeId == nodeId);
        var item = Assert.IsType<ListBoxItem>(list.ItemContainerGenerator.ContainerFromItem(row));
        var lines = TextBlocks(item).Where(t => t.Visibility == Visibility.Visible && t.IsVisible).Select(t => t.Text).ToList();
        return (item, lines);
    }

    [Fact]
    public void SelectingARow_ShowsItsOriginExtractorAndRevisionAsASecondMutedLineUnderIt_AndNowhereElse()
    {
        OnStaThread(() =>
        {
            var factory = new SurfaceContentFactory(new MultiRowQueries());
            var content = factory.Create(new Surface("evidence", "view", "Evidence"));
            var master = Unwrap(content);
            var masterStack = Assert.IsType<StackPanel>(master);
            var list = masterStack.Children.OfType<ListBox>().Single();
            PumpUntil(() => list.ItemsSource is not null);
            Assert.Equal(3, list.ItemsSource!.Cast<object>().Count());

            // Realized, so the rows have containers and their lines have a visibility.
            var window = new Window { Content = content, Width = 480, Height = 400, FontSize = 13 /* MainWindow.xaml:16 — the shell's body size the pane inherits */, WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000, ShowActivated = false };
            try
            {
                window.Show();
                window.UpdateLayout();

                // Nothing selected: every row is its one list line.
                foreach (var id in new[] { "R1", "R2", "R3" })
                {
                    Assert.Equal([$"{id}  ·  kind"], Row(list, id).VisibleLines);
                }

                // The SAME gesture a click drives — SelectionChanged. R2's row grows a second line
                // carrying the three fields; R1 and R3 do not.
                list.SelectedItem = list.ItemsSource!.Cast<EvidenceRowItem>().Single(i => i.Row.NodeId == "R2");
                PumpUntil(() => Row(list, "R2").VisibleLines.Count == 2);
                window.UpdateLayout();

                var r2 = Row(list, "R2");
                Assert.Equal(2, r2.VisibleLines.Count);
                Assert.Equal("R2  ·  kind", r2.VisibleLines[0]);
                Assert.Equal("✓ Verified · Static · csharp 1.4 · rev 51e806f8", r2.VisibleLines[1]);
                Assert.Equal(["R1  ·  kind"], Row(list, "R1").VisibleLines);
                Assert.Equal(["R3  ·  kind"], Row(list, "R3").VisibleLines);

                // Muted, not a heading, and inside the master: no second pane, no section headings.
                var detail = TextBlocks(r2.Item).Single(t => t.Text == r2.VisibleLines[1]);
                Assert.Equal(FontWeights.Normal, detail.FontWeight);
                var first = TextBlocks(r2.Item).Single(t => t.Text == r2.VisibleLines[0]);
                Assert.True(detail.FontSize < first.FontSize, $"the detail line ({detail.FontSize}) is not smaller than the row ({first.FontSize})");
                Assert.DoesNotContain(TextBlocks(masterStack), t => t.Text is "What it is" or "Confidence and provenance" or "Related nodes" or "Source");

                // Selecting R3 — no neighbours — moves the line and says what it knows: nothing.
                list.SelectedItem = list.ItemsSource!.Cast<EvidenceRowItem>().Single(i => i.Row.NodeId == "R3");
                PumpUntil(() => Row(list, "R3").VisibleLines.Count == 2);
                window.UpdateLayout();
                Assert.Equal(["R3  ·  kind", "not recorded"], Row(list, "R3").VisibleLines);
                Assert.Equal(["R2  ·  kind"], Row(list, "R2").VisibleLines);
            }
            finally
            {
                window.Close();
            }

            return 0;
        });
    }

    /// <summary>The kind the pair's detail half was is gone from the product: no row builds it, and the factory says so honestly.</summary>
    [Fact]
    public void TheInspectorKind_IsRetiredFromTheProduct()
    {
        Assert.DoesNotContain(SurfaceContentFactory.Kinds, k => k.Kind == "inspector");
        Assert.DoesNotContain("inspector", SurfaceContentFactory.KnownKinds);
        Assert.Contains("inspector", SurfaceContentFactory.RetiredKinds.Keys);
        Assert.Contains("Ruling 94", SurfaceContentFactory.RetiredKinds["inspector"], StringComparison.Ordinal);

        // A retired kind is restorable in exactly one sense: it reaches the host's admission so a
        // saved envelope that carries it is dropped WITH a report, never silently at the store.
        Assert.Contains("inspector", SurfaceContentFactory.RestorableKinds);
        Assert.All(SurfaceContentFactory.KnownKinds, k => Assert.Contains(k, SurfaceContentFactory.RestorableKinds));
    }
}
