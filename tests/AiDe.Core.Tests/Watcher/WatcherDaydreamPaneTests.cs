using AiDe.Core.Presentation;
using AiDe.Core.Watcher;

namespace AiDe.Core.Tests.Watcher;

/// <summary>
/// D4 — the Daydreams pane read model: three stages, honest empty states, and a promote affordance
/// only where promotion is actually possible (US-9).
/// </summary>
public sealed class WatcherDaydreamPaneTests
{
    private static readonly DaydreamSignature Pattern = new(
        "implement", ScoreSchema.Weave1Version, WeaveVerdict.Blocked, "Correctness", "OutcomeIntegrity:1");

    private sealed class Fixed(params DaydreamCandidate[] candidates) : IWatcherDaydreamQuery
    {
        public IReadOnlyList<DaydreamCandidate> GetCandidates() => candidates;
    }

    private sealed class Broken : IWatcherDaydreamQuery
    {
        public IReadOnlyList<DaydreamCandidate> GetCandidates() => throw new InvalidOperationException("store gone");
    }

    private static DaydreamCandidate Candidate(
        DaydreamState state, string? blocked = null, int episodes = 2) =>
        new(Pattern, state,
            new CandidateEvidence([.. Enumerable.Range(1, episodes).Select(i => "ep-" + i)], "Inferred — few occurrences"),
            blocked);

    private static WatcherDaydreamPaneViewModel Loaded(IWatcherDaydreamQuery? query)
    {
        var pane = new WatcherDaydreamPaneViewModel(query);
        pane.Load();
        return pane;
    }

    // ---------------------------------------------------------------- states

    [Fact]
    public void NoStoreIsAnHonestUnavailableRatherThanAnEmptyList()
    {
        var pane = Loaded(null);

        Assert.Equal(PaneState.Empty, pane.State);
        Assert.Contains("no watcher store is attached", pane.StatusMessage);
        Assert.Empty(pane.Rows);
    }

    /// <summary>A failing store is Error, never a plausible-looking empty pane.</summary>
    /// <remarks>
    /// "No patterns observed yet" over an unreadable store is an absence over a set nobody looked
    /// at — the same shape the privacy probe refuses with exit 9.
    /// </remarks>
    [Fact]
    public void AnUnreadableStoreIsAnErrorNotAnEmptyPane()
    {
        var pane = Loaded(new Broken());

        Assert.Equal(PaneState.Error, pane.State);
        Assert.Contains("could not be read", pane.StatusMessage);
        Assert.Empty(pane.Rows);
    }

    [Fact]
    public void AnEmptyStoreSaysSoWithoutBlamingAnything()
    {
        var pane = Loaded(new Fixed());

        Assert.Equal(PaneState.Empty, pane.State);
        Assert.Equal("No patterns observed yet.", pane.StatusMessage);
    }

    // ---------------------------------------------------------------- the promote affordance

    /// <summary>
    /// Only a Promotable candidate offers promotion, and every other state says why not.
    /// </summary>
    /// <remarks>
    /// The surface reads <c>CanPromote</c> to decide whether the affordance exists <b>at all</b>,
    /// not whether it is enabled and refuses on click. A control a user can press and be refused by
    /// teaches that the refusal is negotiable.
    /// </remarks>
    [Theory]
    [InlineData(DaydreamState.NeedsDisconfirm, false)]
    [InlineData(DaydreamState.Disconfirmed, false)]
    [InlineData(DaydreamState.Deferred, false)]
    [InlineData(DaydreamState.Rejected, false)]
    [InlineData(DaydreamState.Retracted, false)]
    [InlineData(DaydreamState.Promoted, false)]
    [InlineData(DaydreamState.Promotable, true)]
    public void PromotionIsOfferedOnlyWhereItIsPossible(DaydreamState state, bool expected)
    {
        var row = WatcherDaydreamRow.From(Candidate(state, blocked: expected ? null : "a reason"));

        Assert.Equal(expected, row.CanPromote);
    }

    /// <summary>A blocked row carries the reason in the row itself, not behind an interaction.</summary>
    [Fact]
    public void ABlockedRowNamesWhatIsMissingWhereItIsRead()
    {
        var row = WatcherDaydreamRow.From(
            Candidate(DaydreamState.NeedsDisconfirm, "No disconfirming check has been attached."));

        Assert.Contains("No disconfirming check has been attached.", row.DisplayLabel);
        Assert.Contains("blocked: No disconfirming check has been attached.", row.AccessibleName);
    }

    /// <summary>A promotable row announces that it is ready, so the state is not colour-only.</summary>
    [Fact]
    public void APromotableRowAnnouncesItIsReady()
    {
        var row = WatcherDaydreamRow.From(Candidate(DaydreamState.Promotable));

        Assert.Contains("ready to promote", row.AccessibleName);
    }

    // ---------------------------------------------------------------- the three stages

    [Theory]
    [InlineData(DaydreamState.Observation, "Observations")]
    [InlineData(DaydreamState.NeedsDisconfirm, "Candidates")]
    [InlineData(DaydreamState.Promotable, "Candidates")]
    [InlineData(DaydreamState.Promoted, "Promoted")]
    public void EachStateSitsUnderTheStageTheSpecNames(DaydreamState state, string stage)
    {
        Assert.Equal(stage, WatcherDaydreamRow.StageOf(state));
    }

    /// <summary>
    /// A disconfirmed candidate stays visible under Candidates.
    /// </summary>
    /// <remarks>
    /// It is the most informative row on the surface — the system having done the disconfirming work
    /// and reported the answer nobody wanted. Hiding it would leave a reader looking only at the
    /// proposals that survived, which is the selection bias this pipeline exists to avoid.
    /// </remarks>
    [Fact]
    public void ADisconfirmedCandidateStaysVisible()
    {
        var pane = Loaded(new Fixed(Candidate(DaydreamState.Disconfirmed, "A completed check refuted it.")));

        Assert.Single(pane.RowsFor("Candidates"));
        Assert.Contains("refuted", pane.RowsFor("Candidates")[0].DisplayLabel);
    }

    /// <summary>Every stage is named even when it holds nothing, with a reason that fits it.</summary>
    /// <remarks>
    /// Each empty state names only what the pane has looked at. None mentions the extractor, the
    /// scorer, or any subsystem this surface does not read — the mistake DC-087 registered.
    /// </remarks>
    [Fact]
    public void EveryStageHasAnEmptyStateThatClaimsOnlyWhatItChecked()
    {
        Assert.Equal(3, WatcherDaydreamPaneViewModel.Stages.Count);

        foreach (var stage in WatcherDaydreamPaneViewModel.Stages)
        {
            var empty = WatcherDaydreamPaneViewModel.EmptyStateFor(stage);
            Assert.False(string.IsNullOrWhiteSpace(empty));
            Assert.DoesNotContain("extractor", empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("scorer", empty, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TheStatusLineSaysHowManyAreActuallyReady()
    {
        var none = Loaded(new Fixed(Candidate(DaydreamState.NeedsDisconfirm, "no check")));
        Assert.Contains("none ready to promote", none.StatusMessage);

        var one = Loaded(new Fixed(
            Candidate(DaydreamState.NeedsDisconfirm, "no check"), Candidate(DaydreamState.Promotable)));
        Assert.Contains("1 ready to promote", one.StatusMessage);
    }

    // ---------------------------------------------------------------- end to end over the store

    /// <summary>
    /// The pane reads real store facts through the real fold.
    /// </summary>
    /// <remarks>
    /// Through <c>WatcherDaydreamQuery</c> and the store rather than a stub, because the whole point
    /// of D4 is that the surface renders something a writer actually produced. A pane tested only
    /// against fixtures is the shape DC-089 registered.
    /// </remarks>
    [Fact]
    public void ThePaneRendersWhatWasWrittenToTheStore()
    {
        var store = new InMemoryWatcherObservationStore();
        store.AppendDaydreamObservation(new DaydreamObservation("obs-1", Pattern, "ep-1", DateTimeOffset.UnixEpoch));
        store.AppendDaydreamObservation(new DaydreamObservation("obs-2", Pattern, "ep-2", DateTimeOffset.UnixEpoch));

        var pane = Loaded(new WatcherDaydreamQuery(store));

        var row = Assert.Single(pane.RowsFor("Candidates"));
        Assert.Equal(2, row.Episodes);
        Assert.False(row.CanPromote);
        Assert.Contains("implement", row.Pattern);
        Assert.Contains("Correctness", row.Pattern);
    }

    /// <summary>One observation stays an Observation all the way to the surface.</summary>
    [Fact]
    public void OneObservationNeverReachesTheCandidatesStage()
    {
        var store = new InMemoryWatcherObservationStore();
        store.AppendDaydreamObservation(new DaydreamObservation("obs-1", Pattern, "ep-1", DateTimeOffset.UnixEpoch));

        var pane = Loaded(new WatcherDaydreamQuery(store));

        Assert.Empty(pane.RowsFor("Candidates"));
        Assert.Equal(PaneState.Empty, pane.State);
    }
}
