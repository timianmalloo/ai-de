using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AiDe.App.Workbench;
using AiDe.Core.Projections;
using AiDe.Testing;

namespace AiDe.App.Tests;

/// <summary>
/// Core's bound reaches the screen — the real provider driving the real surface.
/// </summary>
/// <remarks>
/// <para><b>The seam this closes.</b> Two tests already covered the halves:
/// <c>SearchProviderBoundTests</c> asserts the provider EMITS the bound, and the design session's
/// <c>BoundsReachTheSurfaceTests</c> asserts the surface RENDERS a row it is handed. Both pass
/// against an <i>implicit</i> contract — kind <c>Other</c>, counts in <c>Label</c>, a non-empty
/// <c>Detail</c> — that neither file stated. So a provider that stopped emitting the row would still
/// pass the surface's test, and a surface that dropped it would still pass the provider's. Two green
/// halves, and the gap between them silent.</para>
///
/// <para><b>Why it is worth a third test rather than a stricter one of the first two.</b> The whole
/// of 2026-09-01 was reading-based methods giving confident wrong answers about whether a value
/// reaches a screen — four of them, across two sessions. The only observer that does not care which
/// language a consumer was written in, or whether the value was renamed on the way, is the running
/// program. This drives the actual query result through the actual provider into the actual control
/// and reads the actual visual tree.</para>
///
/// <para><b>Reading the tree needs Inlines, not just children.</b> The surface puts the detail in a
/// <see cref="Run"/>, and a Run is not a visual child — a walk using only
/// <see cref="VisualTreeHelper"/> would miss it and report a rendered value as absent. That is
/// exactly the mistake that produced a false blocker earlier today, one layer deeper than the
/// blocker itself.</para>
/// </remarks>
public sealed class SearchBoundEndToEndTests
{
    private sealed class Bounded(int skipped, bool truncated, int searched) : FakeWorkspaceQueries
    {
        public override Task<FindResult> FindAsync(
            string term, int maxResults, CancellationToken cancellationToken) =>
            Task.FromResult(new FindResult(
                [], new ResultBounds(0, 0, 1024, 0, 0, 0, 0, false, null), "rev-1"));

        public override Task<ContentSearchResult> SearchContentAsync(
            string term, int maxMatches, CancellationToken cancellationToken) =>
            Task.FromResult(new ContentSearchResult(
                [new ContentMatch("Shop.Order", "src/Order.cs", 12, "// the marker")],
                searched, skipped, truncated,
                new ResultBounds(0, 0, 1024, 0, 0, 0, 0, false, null), "rev-1"));
    }

    /// <summary>Every piece of text the surface actually put on screen.</summary>
    private static string RenderedText(DependencyObject root)
    {
        var text = new System.Text.StringBuilder();

        void Walk(DependencyObject node)
        {
            if (node is TextBlock block)
            {
                // Text AND Inlines: a Run is not a visual child, and the detail lives in one.
                text.Append(' ').Append(block.Text);

                foreach (var inline in block.Inlines)
                {
                    if (inline is Run run) text.Append(' ').Append(run.Text);
                }
            }

            if (node is ContentControl { Content: DependencyObject inner }) Walk(inner);

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            {
                Walk(VisualTreeHelper.GetChild(node, i));
            }
        }

        Walk(root);
        return text.ToString();
    }

    private static string SearchAndRender(IWorkspaceQueries queries) => OnSta(() =>
    {
        var shell = new WorkbenchShell(queries);

        // The REAL provider — the same delegate SurfaceContentFactory hands the surface.
        var hits = shell.SearchWorkspaceAsync("marker").GetAwaiter().GetResult();

        var surface = new SearchSurface();
        var window = new Window
        {
            Content = surface, Width = 600, Height = 400,
            Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false,
        };

        window.Show();
        surface.ShowResults(hits);
        window.UpdateLayout();

        var rendered = RenderedText(surface);
        window.Close();
        return rendered;
    });

    private static T OnSta<T>(Func<T> body)
    {
        T result = default!;
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try { result = body(); }
            catch (Exception ex) { failure = ex; }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)), "the STA thread did not finish");

        if (failure is not null) throw failure;

        return result;
    }

    [Fact]
    public void TheSkippedFileCountIsOnScreen()
    {
        var rendered = SearchAndRender(new Bounded(skipped: 40, truncated: false, searched: 412));

        Assert.Contains("40", rendered, StringComparison.Ordinal);
        Assert.Contains("412", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void TheRowSaysWhatTheNumberMeans()
    {
        // A footer that renders half of itself states a number with no claim attached. The detail is
        // the claim, and it is the half a Run-blind tree walk would silently lose.
        var rendered = SearchAndRender(new Bounded(skipped: 40, truncated: false, searched: 412));

        Assert.Contains("lower bound", rendered, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AFiredCapIsOnScreenToo()
    {
        var rendered = SearchAndRender(new Bounded(skipped: 0, truncated: true, searched: 100));

        Assert.Contains("match(es) only", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void ACompleteSearchPutsNoCaveatOnScreen()
    {
        // The half that gets forgotten: a caveat shown when nothing was hidden trains a reader to
        // skip caveats, and then the real one is invisible too. Same defect as dropping it, reached
        // from the opposite direction.
        var rendered = SearchAndRender(new Bounded(skipped: 0, truncated: false, searched: 412));

        Assert.DoesNotContain("lower bound", rendered, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("not read", rendered, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheRealResultIsStillThere()
    {
        // The DC-016 guard for this file. If ShowResults silently rendered nothing, every assertion
        // above that looks for absence would pass, and the two that look for presence would be the
        // only thing standing between this test and vacuity.
        var rendered = SearchAndRender(new Bounded(skipped: 40, truncated: false, searched: 412));

        Assert.Contains("Order.cs", rendered, StringComparison.Ordinal);
    }
}
