using AiDe.App.Workbench;
using AiDe.Core.Projections;
using AiDe.Testing;

namespace AiDe.App.Tests;

/// <summary>
/// The search provider emits Core's bound, so the surface has something to render.
/// </summary>
/// <remarks>
/// <para><b>Closing my own unverified claim.</b> I reported that
/// <c>ContentSearchResult.FilesSkipped</c> and <c>Truncated</c> are "visible as a row". That was my
/// word, not an assertion, and the whole of 2026-09-01 was a demonstration that a careful reading of
/// source is not evidence about what reaches a screen — four confident wrong answers in one day
/// between two sessions, twice mine.</para>
///
/// <para><b>What this proves and what it does not.</b> It proves the PROVIDER emits the bound: given
/// a query that reports skipped files, the list it hands the surface contains the number. It does
/// <b>not</b> prove the surface renders it — that is
/// <c>BoundsReachTheSurfaceTests</c>, which walks the rendered tree and is the only method that has
/// held. Two halves of one claim, and this is the half I own.</para>
///
/// <para><b>Why the bound is a row at all.</b> The provider's contract is
/// <c>Func&lt;string, Task&lt;IReadOnlyList&lt;SearchResult&gt;&gt;&gt;</c> and has nowhere else to
/// put it. <c>DESIGN.md</c> §4a specifies a capped chip with a tooltip, which is the right shape and
/// is Design's to build; the row makes the number visible in the meantime rather than leaving a
/// result list that silently claims completeness it cannot have (DC-025).</para>
/// </remarks>
public sealed class SearchProviderBoundTests
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
                FilesSearched: searched,
                FilesSkipped: skipped,
                Truncated: truncated,
                new ResultBounds(0, 0, 1024, 0, 0, 0, 0, false, null),
                "rev-1"));
    }

    private static IReadOnlyList<SearchResult> Search(IWorkspaceQueries queries) =>
        OnStaThread(() =>
        {
            var shell = new WorkbenchShell(queries);
            return shell.SearchWorkspaceAsync("marker").GetAwaiter().GetResult();
        });

    private static T OnStaThread<T>(Func<T> body) =>
        Sta.Run<T>(body, 60);

    [Fact]
    public void SkippedFilesReachTheSurfaceAsANumber()
    {
        // The count, not the word "some". A reader cannot judge "a few files were skipped"; they can
        // judge 40 against 412.
        var results = Search(new Bounded(skipped: 40, truncated: false, searched: 412));

        var bound = Assert.Single(results, r => r.Kind == SearchResultKind.Other);

        Assert.Contains("40", bound.Label, StringComparison.Ordinal);
        Assert.Contains("412", bound.Label, StringComparison.Ordinal);
    }

    [Fact]
    public void AFiredMatchCapIsSaidOutLoud()
    {
        var results = Search(new Bounded(skipped: 0, truncated: true, searched: 100));

        var bound = Assert.Single(results, r => r.Kind == SearchResultKind.Other);

        Assert.Contains("match(es) only", bound.Label, StringComparison.Ordinal);
        Assert.False(string.IsNullOrEmpty(bound.Detail),
            "the row must say what the number means, not just state it");
    }

    [Fact]
    public void ACompleteSearchAddsNoBoundRow()
    {
        // The other half of DC-025, and the half that gets forgotten: a caveat that fires when
        // nothing was hidden trains a reader to skip caveats, and then the real one is invisible too.
        var results = Search(new Bounded(skipped: 0, truncated: false, searched: 412));

        Assert.DoesNotContain(results, r => r.Kind == SearchResultKind.Other);
    }

    [Fact]
    public void TheBoundSortsAfterEveryRealResult()
    {
        // It is a footer, not a hit. SearchModel orders by kind and Other is last, so this asserts
        // the two halves agree rather than that one of them happens to work.
        var results = Search(new Bounded(skipped: 40, truncated: false, searched: 412));

        var groups = SearchModel.Group(results);

        Assert.Equal(SearchResultKind.Other, groups[^1].Kind);
    }
}
