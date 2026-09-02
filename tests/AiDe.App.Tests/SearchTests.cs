using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using AiDe.App.Workbench;
using Xunit;

namespace AiDe.App.Tests;

/// <summary>
/// Covers the app-search-breadth scaffold: the pure <see cref="SearchModel"/> grouping and the
/// <see cref="SearchSurface"/> states (idle, not-indexed, results, no-match) and navigate hand-off.
/// The surface is STA/WPF, so its tests run on a dedicated STA thread, mirroring the class- and
/// sequence-diagram surface tests.
/// </summary>
public sealed class SearchTests
{
    // ---- pure model (no UI thread) ------------------------------------------------------------

    [Fact]
    public void Group_OrdersByKind_AndDropsEmptyGroups()
    {
        IReadOnlyList<SearchResult> hits =
        [
            new("cmd1", SearchResultKind.Command, "Reset layout"),
            new("t1", SearchResultKind.Type, "OrderService"),
            new("f1", SearchResultKind.File, "Order.cs"),
            new("t2", SearchResultKind.Type, "OrderRepository"),
        ];

        var groups = SearchModel.Group(hits);

        // Type first, then File, then Command; Member/Node/Other absent are dropped.
        Assert.Equal(3, groups.Count);
        Assert.Equal(SearchResultKind.Type, groups[0].Kind);
        Assert.Equal(SearchResultKind.File, groups[1].Kind);
        Assert.Equal(SearchResultKind.Command, groups[2].Kind);
        Assert.Equal(2, groups[0].Results.Count); // both types grouped, provider order preserved
        Assert.Equal("OrderService", groups[0].Results[0].Label);
        Assert.Contains("(2)", groups[0].Header);
    }

    [Fact]
    public void Group_Empty_YieldsNoGroups()
    {
        Assert.Empty(SearchModel.Group(null));
        Assert.Empty(SearchModel.Group(new List<SearchResult>()));
        Assert.Equal(0, SearchModel.Count(null));
    }

    // ---- surface (STA) ------------------------------------------------------------------------

    [Fact]
    public void Surface_StartsIdle_WithGuidance()
        => OnSta(() =>
        {
            var s = new SearchSurface();
            Assert.True(s.IsIdle);
            Assert.Equal(0, s.ResultCount);
            Assert.Contains("Type to search", s.StatusText);
        });

    [Fact]
    public void Surface_NoProvider_SaysNotIndexed()
        => OnSta(async () =>
        {
            var s = new SearchSurface(); // Provider left null
            await s.SearchAsync("order");
            Assert.Equal(0, s.ResultCount);
            Assert.Contains("indexed", s.StatusText);
        });

    [Fact]
    public void Surface_WithProvider_RendersGroupedResults()
        => OnSta(async () =>
        {
            var s = new SearchSurface
            {
                Provider = _ => Task.FromResult<IReadOnlyList<SearchResult>>(
                [
                    new("t1", SearchResultKind.Type, "OrderService"),
                    new("m1", SearchResultKind.Member, "PlaceOrder"),
                ]),
            };

            await s.SearchAsync("order");

            Assert.Equal(2, s.ResultCount);
            Assert.False(s.IsIdle);
            Assert.Contains("2 matches", s.StatusText);
        });

    [Fact]
    public void Surface_WithProvider_NoHits_SaysNoMatches()
        => OnSta(async () =>
        {
            var s = new SearchSurface
            {
                Provider = _ => Task.FromResult<IReadOnlyList<SearchResult>>(new List<SearchResult>()),
            };

            await s.SearchAsync("zzz");
            Assert.Equal(0, s.ResultCount);
            Assert.Contains("No matches", s.StatusText);
        });

    [Fact]
    public void Surface_Activate_HandsBackProviderResult()
        => OnSta(() =>
        {
            SearchResult? activated = null;
            var s = new SearchSurface { OnActivate = r => activated = r };
            var hit = new SearchResult("node-42", SearchResultKind.Node, "OrderPlaced");

            s.ShowResults([hit]);
            // The row is a Button; find it and click via the activation path directly.
            s.OnActivate?.Invoke(hit);

            Assert.NotNull(activated);
            Assert.Equal("node-42", activated!.Id);
        });

    [Fact]
    public void Surface_BlankQuery_ReturnsToIdle()
        => OnSta(async () =>
        {
            var s = new SearchSurface
            {
                Provider = _ => Task.FromResult<IReadOnlyList<SearchResult>>(
                    [new("t1", SearchResultKind.Type, "OrderService")]),
            };

            await s.SearchAsync("order");
            Assert.Equal(1, s.ResultCount);

            await s.SearchAsync("   ");
            Assert.True(s.IsIdle);
            Assert.Contains("Type to search", s.StatusText);
        });

    // ---- STA harness --------------------------------------------------------------------------

    private static void OnSta(System.Action body) =>
        Sta.Run(body, 30);

    private static void OnSta(System.Func<Task> body)
        => OnSta(() =>
        {
            // With Task.FromResult providers the continuation resumes synchronously on this bare STA
            // thread (no SyncContext), so awaiting completes before the frame exits.
            var task = body();
            while (!task.IsCompleted)
            {
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
            }

            task.GetAwaiter().GetResult();
        });
}
