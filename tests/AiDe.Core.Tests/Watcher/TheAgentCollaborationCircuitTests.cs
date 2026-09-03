using AiDe.Core.Watcher;

namespace AiDe.Core.Tests.Watcher;

/// <summary>
/// The whole agent loop as ONE chain, and the exact link where it currently ends.
/// </summary>
/// <remarks>
/// <para><b>Why this exists.</b> Every link in the collaboration loop is tested at its own seam and
/// passes. Three capabilities were nonetheless found this evening that nothing could reach —
/// <c>MessageBoardService</c>, <c>StandingComposer</c>, <c>McpToolGateway</c> — because a seam test
/// asks "does this component work" and never "can anything get here from there". A chain of correct
/// components is not a working chain, and the only thing that distinguishes them is a test that
/// walks the whole thing.</para>
///
/// <para><b>What is proven here:</b> an agent registers through the contract log, receives a
/// capability, declares an episode, and closes it — all through the channel an agent actually has,
/// with no shell and no UI.</para>
///
/// <para><b>And where the chain ends.</b> A closed episode is <b>never scored</b>. Scoring has
/// exactly one producer, <c>WatcherHost.ImportAndScoreEpisodesFromAuditLog</c>, which reads AI-DE's
/// own audit log — and an audit-imported episode takes its <c>SessionId</c> from the log's
/// <c>session</c> field, while <c>TrustedRegistrar</c> mints a fresh id for a registered agent. The
/// two identifier spaces never meet. So an agent that registers and declares work produces a closed
/// episode, no scorecard, and therefore no standing, forever.</para>
///
/// <para><b>These assertions are deliberately written to FAIL when the gap closes.</b> The last two
/// pin an absence, and the day someone wires scoring to the agent path they will go red and say so
/// — which is the point. An absence nobody has pinned is indistinguishable from an absence nobody
/// has noticed, and this one is a decision waiting to be made rather than a defect to patch: where a
/// contract-closed episode's deterministic signals come from is a design question, not a wiring
/// one.</para>
/// </remarks>
public sealed class TheAgentCollaborationCircuitTests
{
    private const double At = 1000;
    private const string Repo = "C:/repos/app";

    private static Dictionary<string, string?> RegisterAttrs() => new(StringComparer.Ordinal)
    {
        [OtelAttributes.RepoPath] = Repo,
        [OtelAttributes.RepoDisplay] = "app",
        [OtelAttributes.WorktreePath] = Repo,
        [OtelAttributes.WorktreeBranch] = "main",
        [OtelAttributes.TerminalId] = "term-1",
        [OtelAttributes.AgentName] = "agent-ext",
        [OtelAttributes.ServiceName] = "claude-code",
    };

    private static (InjectedContractIngest Adapter, InMemoryWatcherObservationStore Store) Circuit()
    {
        var store = new InMemoryWatcherObservationStore();
        var n = 0;
        var registrar = new TrustedRegistrar(
            store, new SequentialCapabilityFactory(), new FakeMonotonicClock(), () => $"session-{++n}");
        var host = new IngestHost(store, registrar, new FixedTimeProvider(DateTimeOffset.UnixEpoch.AddSeconds(At)));

        return (new InjectedContractIngest(host), store);
    }

    [Fact]
    public void AnAgentRegistersDeclaresAndClosesWorkThroughTheContractAlone()
    {
        // The whole inbound half, driven the way an agent drives it: lines into the contract log.
        // No shell, no UI, no MCP — those are the paths an agent does NOT have.
        var (adapter, store) = Circuit();

        adapter.Apply(new ContractRegister("ext-1", RegisterAttrs(), At, 1));

        var session = Assert.Single(store.AllSessions());

        adapter.Apply(new ContractEpisodeOpen(
            "ext-1",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [CoordContract.EpisodeAttributes.Goal] = "close the collaboration loop",
                [CoordContract.EpisodeAttributes.DoneWhen] = "an agent receives a standing",
            },
            At + 1, 2));

        var episode = Assert.Single(store.EpisodesForSession(session.SessionId));
        Assert.Equal(EpisodeState.Active, episode.State);

        adapter.Apply(new ContractEpisodeClose(
            "ext-1",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [CoordContract.EpisodeAttributes.Outcome] = "completed",
            },
            At + 2, 3));

        var closed = Assert.Single(store.EpisodesForSession(session.SessionId));
        Assert.Equal(EpisodeState.Closed, closed.State);
    }

    [Fact]
    public void ACompletedAgentEpisodeIsNeverScored_WhichIsWhereTheLoopEnds()
    {
        // THE BREAK, pinned. Scoring has one producer — ImportAndScoreEpisodesFromAuditLog — and it
        // is not on the agent's path. Nothing closes an episode INTO a scorecard.
        //
        // This assertion is meant to fail the day that changes. If it does, the loop closed and this
        // test should be rewritten to assert the standing rather than deleted.
        var (adapter, store) = Circuit();

        adapter.Apply(new ContractRegister("ext-1", RegisterAttrs(), At, 1));
        adapter.Apply(new ContractEpisodeOpen(
            "ext-1",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [CoordContract.EpisodeAttributes.Goal] = "g",
                [CoordContract.EpisodeAttributes.DoneWhen] = "d",
            },
            At + 1, 2));
        adapter.Apply(new ContractEpisodeClose(
            "ext-1",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [CoordContract.EpisodeAttributes.Outcome] = "completed",
            },
            At + 2, 3));

        Assert.Empty(store.AllScoredEpisodes());
    }

    [Fact]
    public void AndSoNoStandingIsPublished_ForTheRightReason()
    {
        // The distinction that matters to whoever reads this next: the standing is absent because
        // there is no SCORE, not because delivery is broken. StandingPublisher works — proven in
        // StandingReachesTheAgentTests against a scored episode — and correctly writes nothing when
        // there is nothing to say (DC-087).
        var (adapter, store) = Circuit();
        adapter.Apply(new ContractRegister("ext-1", RegisterAttrs(), At, 1));

        var session = Assert.Single(store.AllSessions());
        var directory = Path.Combine(Path.GetTempPath(), "aide-circuit-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(directory);

        try
        {
            var scored = store.AllScoredEpisodes();
            var latest = store.EpisodesForSession(session.SessionId)
                .FirstOrDefault(e => scored.Any(s => s.EpisodeId == e.EpisodeId));

            var published = StandingPublisher.Publish(directory, session.SessionId, scored, latest?.EpisodeId);

            Assert.Null(published);
            Assert.Empty(store.AllScoredEpisodes());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
