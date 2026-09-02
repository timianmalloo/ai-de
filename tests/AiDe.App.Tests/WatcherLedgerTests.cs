using System;
using System.Collections.Generic;
using System.Linq;
using AiDe.App.Workbench;
using AiDe.Core.Watcher;
using Xunit;

namespace AiDe.App.Tests;

/// <summary>
/// The Ledger read (US: "the ledger viewable too, not just message board and leaderboard"): the
/// append-only record of every work episode, newest first, over the same observation store the Board
/// and Leaderboard read.
/// </summary>
public sealed class WatcherLedgerTests
{
    private static WorkEpisode Episode(string id, string goal, DateTimeOffset openedAt, DateTimeOffset? closedAt = null) =>
        new(id, "s1", new EpisodeGeneration(1), new Goal(goal), new DoneCondition("done"), null, openedAt, closedAt, null);

    [Fact]
    public void ARow_ShowsState_Time_AndGoal()
    {
        var open = Episode("e1", "Fix the windowing", DateTimeOffset.UnixEpoch);
        var closed = Episode("e2", "Ship the ledger", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddMinutes(5));

        Assert.StartsWith("Active", LedgerRow.From(open).DisplayLabel, StringComparison.Ordinal);
        Assert.Contains("Fix the windowing", LedgerRow.From(open).DisplayLabel, StringComparison.Ordinal);
        Assert.StartsWith("Closed", LedgerRow.From(closed).DisplayLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void ARow_WithNoGoal_SaysSo_RatherThanBlank()
    {
        var label = LedgerRow.From(Episode("e1", "   ", DateTimeOffset.UnixEpoch)).DisplayLabel;
        Assert.Contains("no goal recorded", label, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rows_AreNewestFirst()
    {
        var older = Episode("old", "older", DateTimeOffset.UnixEpoch.AddMinutes(1));
        var newer = Episode("new", "newer", DateTimeOffset.UnixEpoch.AddMinutes(9));

        var rows = LedgerRow.Rows([older, newer]);

        Assert.Contains("newer", rows[0].DisplayLabel, StringComparison.Ordinal);
        Assert.Contains("older", rows[1].DisplayLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void TheQuery_ReturnsEveryEpisodeInTheStore()
    {
        var store = new InMemoryWatcherObservationStore();
        store.RecordEpisode(Episode("e1", "one", DateTimeOffset.UnixEpoch));
        store.RecordEpisode(Episode("e2", "two", DateTimeOffset.UnixEpoch.AddMinutes(1)));

        var episodes = new WatcherLedgerQuery(store).GetEpisodes();

        Assert.Equal(2, episodes.Count);
    }

    [Fact]
    public void Status_WithNoStore_SaysObservationUnavailable_NotBlank()
    {
        Assert.Contains("not available", LedgerRow.StatusFor(null), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Status_WithAnEmptyStore_SaysNothingRecordedYet_NotUnavailable()
    {
        var status = LedgerRow.StatusFor(new WatcherLedgerQuery(new InMemoryWatcherObservationStore()));
        Assert.Contains("No work episodes", status, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("not available", status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Status_CountsWhatIsRecorded()
    {
        var store = new InMemoryWatcherObservationStore();
        store.RecordEpisode(Episode("e1", "one", DateTimeOffset.UnixEpoch));

        Assert.Contains("1 work episode", LedgerRow.StatusFor(new WatcherLedgerQuery(store)), StringComparison.OrdinalIgnoreCase);
    }
}
