using AiDe.App.Workbench;
using AiDe.Core.Watcher;

namespace AiDe.App.Tests;

/// <summary>
/// conn-11 - the operator's append-only score dispute (US rule 12). The claims: disputing the latest
/// scored episode appends a ScoreDispute (never changing the score) under a fixed local operator id (no
/// human identity); a Not-Scored card is not disputable (no number to dispute); and an empty store yields
/// an honest "nothing to dispute yet" message rather than a throw.
/// </summary>
public sealed class WorkbenchDisputeTests
{
    private static readonly DateTimeOffset Opened = DateTimeOffset.UnixEpoch;
    private static readonly DateTimeOffset Closed = DateTimeOffset.UnixEpoch.AddMinutes(10);

    private static WorkEpisode ClosedEpisode(string id = "ep:al-1")
        => new(id, "sess-1", new EpisodeGeneration(1), new Goal("Ship it"),
               new DoneCondition("tests green"), null, Opened, Closed, EpisodeOutcome.Completed);

    private static void SeedScore(IWatcherObservationStore store, WorkEpisode episode, bool hasProofPack)
    {
        store.RecordEpisode(episode);
        var signals = DeterministicSignalsDeriver.Derive(episode, new EpisodeEvidence(hasProofPack), store);
        new ScoringService(store, TimeProvider.System)
            .ScoreAndRecord(episode, signals, operatorId: episode.SessionId, taskClass: "audit-import", TestWorkspaces.Repo);
    }

    [Fact]
    public void RaiseDisputeOnLatest_AppendsADispute_AgainstTheScoredEpisode_UnderALocalOperator()
    {
        var store = new InMemoryWatcherObservationStore();
        var episode = ClosedEpisode();
        SeedScore(store, episode, hasProofPack: true); // a Partial, genuinely-scored card

        var message = WorkbenchShell.RaiseDisputeOnLatest(store, TimeProvider.System, "loomkeeper-operator", "unfair");

        Assert.StartsWith("Dispute recorded", message);
        var disputes = store.DisputesForEpisode(episode.EpisodeId);
        Assert.Single(disputes);
        Assert.Equal("loomkeeper-operator", disputes[0].OperatorId); // never a human identity
        Assert.Equal("unfair", disputes[0].Reason);
    }

    [Fact]
    public void RaiseDisputeOnLatest_ANotScoredCard_IsNotDisputable()
    {
        var store = new InMemoryWatcherObservationStore();
        SeedScore(store, ClosedEpisode(), hasProofPack: false); // no proof pack -> Not-Scored

        var message = WorkbenchShell.RaiseDisputeOnLatest(store, TimeProvider.System, "loomkeeper-operator", "unfair");

        Assert.Equal("There is no scored episode to dispute yet.", message);
        Assert.Empty(store.AllDisputes());
    }

    [Fact]
    public void RaiseDisputeOnLatest_NothingScored_YieldsAnHonestMessage_NotAThrow()
    {
        var store = new InMemoryWatcherObservationStore();

        var message = WorkbenchShell.RaiseDisputeOnLatest(store, TimeProvider.System, "loomkeeper-operator", "unfair");

        Assert.Equal("There is no scored episode to dispute yet.", message);
        Assert.Empty(store.AllDisputes());
    }
}
