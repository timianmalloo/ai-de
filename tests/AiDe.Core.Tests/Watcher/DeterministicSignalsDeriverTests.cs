using AiDe.Core.Watcher;

namespace AiDe.Core.Tests.Watcher;

/// <summary>
/// LK-SIGNALS-01..07 - the deterministic signals derivation + auto-score (conn-10). The claims: a committed
/// Proof Pack is the one honest verification signal (proof pack -> HasVerificationPath, so the episode
/// scores an honest Partial; no proof pack -> Not-Scored); acceptance stays null (never fabricated to a met
/// outcome, so no floor trips and OutcomeIntegrity renders Not-Recorded); and the host import auto-scores
/// each imported episode, idempotently (a re-run upserts, never duplicates).
/// </summary>
public sealed class DeterministicSignalsDeriverTests
{
    private static readonly DateTimeOffset Opened = DateTimeOffset.UnixEpoch;
    private static readonly DateTimeOffset Closed = DateTimeOffset.UnixEpoch.AddMinutes(10);

    private static WorkEpisode ClosedEpisode(EpisodeOutcome outcome = EpisodeOutcome.Completed)
        => new("ep:al-1", "sess-1", new EpisodeGeneration(1), new Goal("Ship it"),
               new DoneCondition("tests green"), null, Opened, Closed, outcome);

    // ---- deriver honesty ----------------------------------------------------------------------------

    [Fact]
    public void Derive_WithProofPack_SetsTheVerificationPath_ButNeverFabricatesAcceptance()
    {
        var signals = DeterministicSignalsDeriver.Derive(
            ClosedEpisode(), new EpisodeEvidence(HasProofPack: true), new InMemoryWatcherObservationStore());

        Assert.True(signals.HasVerificationPath);
        Assert.True(signals.RequiredVerificationExecuted);
        Assert.Null(signals.AcceptanceCriteriaMet); // unknown from an audit entry - never fabricated to "met"
        Assert.False(signals.RegressionPresent);
        Assert.False(signals.CoverageCalibrated);
    }

    [Fact]
    public void Derive_WithoutProofPack_HasNoVerificationPath()
    {
        var signals = DeterministicSignalsDeriver.Derive(
            ClosedEpisode(), new EpisodeEvidence(HasProofPack: false), new InMemoryWatcherObservationStore());

        Assert.False(signals.HasVerificationPath);
        Assert.False(signals.RequiredVerificationExecuted);
    }

    // ---- honest scoring end to end (through the real WeaveScorer) -----------------------------------

    [Fact]
    public void ProofPackEpisode_ScoresPartial_NotNotScored_AndTripsNoFloor()
    {
        var episode = ClosedEpisode();
        var signals = DeterministicSignalsDeriver.Derive(
            episode, new EpisodeEvidence(HasProofPack: true), new InMemoryWatcherObservationStore());

        var card = new WeaveScorer().Score(episode, signals, new FixedTimeProvider(Closed));

        Assert.Equal(WeaveVerdict.Partial, card.Verdict); // Focus scores; acceptance null -> Outcome Not-Recorded
        Assert.Empty(card.TrippedFloors);                 // acceptance null + verification executed -> no floor
    }

    [Fact]
    public void NoProofPackEpisode_IsNotScored_NotBlocked()
    {
        var episode = ClosedEpisode();
        var signals = DeterministicSignalsDeriver.Derive(
            episode, new EpisodeEvidence(HasProofPack: false), new InMemoryWatcherObservationStore());

        var card = new WeaveScorer().Score(episode, signals, new FixedTimeProvider(Closed));

        Assert.Equal(WeaveVerdict.NotScored, card.Verdict); // "no minimum verification path" - honest
    }

    // ---- host import + auto-score (D4, real SQLite) -------------------------------------------------

    [Fact]
    public void ImportAndScore_ProofPackEntryPartial_NoProofNotScored_ReRunUpserts()
    {
        var data = Path.Combine(Path.GetTempPath(), $"aide-conn10-data-{Guid.NewGuid():N}");
        var coord = Path.Combine(Path.GetTempPath(), $"aide-conn10-coord-{Guid.NewGuid():N}");
        var auditPath = Path.Combine(Path.GetTempPath(), $"aide-conn10-audit-{Guid.NewGuid():N}.jsonl");
        Directory.CreateDirectory(data);
        Directory.CreateDirectory(coord);
        File.WriteAllLines(auditPath,
        [
            """{"id":"al-proof","session":"sess-p","goal":"g","done_when":"d","outcome":"success","artifacts":["src/x.cs","docs/proof/x.md"]}""",
            """{"id":"al-noproof","session":"sess-n","goal":"g","done_when":"d","outcome":"success","artifacts":["src/y.cs"]}""",
        ]);

        try
        {
            using var host = WatcherHost.Open(data, coord);

            var imported = host.ImportAndScoreEpisodesFromAuditLog(auditPath);
            Assert.Equal(2, imported);

            var scored = host.Store.AllScoredEpisodes();
            Assert.Equal(2, scored.Count);
            Assert.Contains(scored, s => s.EpisodeId == "ep:al-proof" && s.Scorecard.Verdict == WeaveVerdict.Partial);
            Assert.Contains(scored, s => s.EpisodeId == "ep:al-noproof" && s.Scorecard.Verdict == WeaveVerdict.NotScored);

            // operatorId is the session id (the honest grouping key), never a human identity.
            Assert.Equal("sess-p", scored.Single(s => s.EpisodeId == "ep:al-proof").OperatorId);

            // Idempotent: a re-run re-scores, it does not duplicate.
            host.ImportAndScoreEpisodesFromAuditLog(auditPath);
            Assert.Equal(2, host.Store.AllScoredEpisodes().Count);
        }
        finally
        {
            try { Directory.Delete(data, recursive: true); } catch (IOException) { }
            try { Directory.Delete(coord, recursive: true); } catch (IOException) { }
            try { File.Delete(auditPath); } catch (IOException) { }
        }
    }
}
