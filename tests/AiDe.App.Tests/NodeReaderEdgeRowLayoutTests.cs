using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench;
using AiDe.Core.Presentation;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// <b>Ruling 92 — the edge row is a left-aligned grid with one baseline.</b> The operator's words:
/// <i>"need to fixt the layout of the metadata view"</i>; their screenshot: every typed-edge row
/// floating centred under left-aligned metadata rows, the status a superscript beside a larger
/// target. The reader is rendered in a window carrying the App's real theme (its implicit Button
/// style is the mechanism: <c>ChromeButtonTemplate</c> centres its presenter and ignores
/// <c>HorizontalContentAlignment</c>), because a detached reader would render the platform's
/// default template, which honours the alignment — the layout that was tested would not be the
/// layout that shipped (DC-046).
/// </summary>
public sealed class NodeReaderEdgeRowLayoutTests
{
    private static readonly CanvasNode Node = new("Shop.Order", "Order", "class", true, "Orders");

    private static readonly IReadOnlyList<CanvasEdge> Edges =
    [
        new("Shop.Order", "csharp:Shop:net10.0", "declared_in", "Verified"),
        new("Shop.Order", "Shop.Customer", "depends_on", "Verified"),
        new("Shop.Billing.Invoice", "Shop.Order", "calls", "Inferred"),
    ];

    // Ruling 92 CONDITION: every row's predicate shares the metadata rows' label X, and the three
    // blocks of a row share one FontSize.
    [Fact]
    public void EveryEdgeRowsPredicateSharesTheMetadataLabelsLeftEdge_AndItsThreeBlocksShareOneSize()
    {
        ThemeProbe.OnThemedWindow(
            "SurfaceBrush",
            _ =>
            {
                var reader = new NodeReaderView { Width = 420, Height = 600 };
                reader.Show(Node, Edges);
                return reader;
            },
            (_, subject) =>
            {
                var reader = (NodeReaderView)subject;
                var labels = ThreadFixtures.Visuals<TextBlock>(reader)
                    .Where(t => t.Text is "id" or "type" or "context")
                    .ToList();
                Assert.Equal(3, labels.Count);
                var labelX = labels.Select(l => l.TranslatePoint(new Point(0, 0), reader).X).Distinct().ToList();
                Assert.True(labelX.Count == 1, $"the metadata labels do not share one X: {string.Join(", ", labelX)}");

                var rows = reader.FocusStops.Skip(1).OfType<Button>().ToList();
                Assert.Equal(Edges.Count, rows.Count);

                foreach (var row in rows)
                {
                    var blocks = ThreadFixtures.Visuals<TextBlock>(row).ToList();
                    Assert.True(blocks.Count == 3, $"a row renders {blocks.Count} text blocks, not predicate · target · status");

                    var predicate = blocks[0];
                    var x = predicate.TranslatePoint(new Point(0, 0), reader).X;
                    Assert.True(
                        Math.Abs(x - labelX[0]) < 0.5,
                        $"the predicate '{predicate.Text}' starts at X={x:0.##} while the metadata labels start at X={labelX[0]:0.##}");

                    var sizes = blocks.Select(b => b.FontSize).Distinct().ToList();
                    Assert.True(
                        sizes.Count == 1,
                        $"the row for '{predicate.Text}' renders {sizes.Count} font sizes ({string.Join(", ", sizes)}) — one baseline needs one size");
                }
            });
    }

    // The status keeps its muted ink (the distinction is colour, not size) and the row's blocks
    // sit on one line — the target is neither above nor below the predicate.
    [Fact]
    public void TheStatusIsMutedNotSmaller_AndTheThreeBlocksShareOneLine()
    {
        ThemeProbe.OnThemedWindow(
            "SurfaceBrush",
            _ =>
            {
                var reader = new NodeReaderView { Width = 420, Height = 600 };
                reader.Show(Node, Edges);
                return reader;
            },
            (theme, subject) =>
            {
                var reader = (NodeReaderView)subject;
                var muted = ThemeProbe.Token(theme, "TextMutedBrush");
                foreach (var row in reader.FocusStops.Skip(1).OfType<Button>())
                {
                    var blocks = ThreadFixtures.Visuals<TextBlock>(row).ToList();
                    Assert.Equal(3, blocks.Count);
                    var status = blocks.Single(b => b.Text is "Verified" or "Inferred");
                    Assert.Equal(muted, ((SolidColorBrush)status.Foreground).Color);
                    Assert.True(
                        status.FontSize == blocks.Max(b => b.FontSize),
                        $"the status renders at {status.FontSize} px beside a {blocks.Max(b => b.FontSize)} px target — a superscript, not a muted peer");

                    var centres = blocks
                        .Select(b => b.TranslatePoint(new Point(0, b.ActualHeight / 2), reader).Y)
                        .ToList();
                    Assert.True(
                        centres.Max() - centres.Min() < 1.0,
                        $"the row's blocks do not share one line: centres at {string.Join(", ", centres.Select(c => c.ToString("0.##")))}");

                    // The row's own template, not the App's centring one — and its named chrome
                    // part resolves in the template's name scope, so the hover and focus triggers
                    // that target it can fire (DC-166's shape, checked on the reading side).
                    Assert.NotSame(Application.Current?.TryFindResource(typeof(Button)), row.Template);
                    Assert.IsType<Border>(row.Template.FindName("Chrome", row));
                    Assert.IsType<Border>(row.Template.FindName("FocusRing", row));
                    Assert.Equal(2, row.Template.Triggers.Count);
                }
            });
    }
}
