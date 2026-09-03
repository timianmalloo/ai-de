namespace AiDe.Core.Watcher;

/// <summary>
/// Turns a contract-closed Work Episode into a scored one - the link the agent collaboration loop was
/// missing (US-16).
/// </summary>
/// <remarks>
/// <para><b>The break this closes.</b> An agent registers through the coordination contract, declares
/// an episode, and closes it; every one of those steps worked and was tested at its seam. Scoring had
/// exactly one producer - <c>WatcherHost.ImportAndScoreEpisodesFromAuditLog</c> - which reads AI-DE's
/// own audit log, and takes its session id from the log's <c>session</c> field while
/// <see cref="TrustedRegistrar"/> mints a fresh one. The two identifier spaces could never meet, so a
/// registered agent produced a closed episode, no scorecard, and therefore no standing, forever. No
/// seam test could show that; only a test that walks the whole chain.</para>
///
/// <para><b>Why a pass and not a hook on close.</b> Closing an episode is a <i>declaration</i>;
/// scoring it is a <i>judgement</i>. Coupling them would make the agent's own <c>episode-close</c>
/// line the thing that produced its score, and the two would fail together. An idempotent sweep over
/// closed-but-unscored is the shape every other watcher pass already has, so re-running it is free.</para>
///
/// <para><b>Registered sessions only</b>, which keeps the two scoring producers disjoint: an
/// audit-imported episode has no <see cref="SessionRecord"/>, so this never re-scores one under a
/// different task class and the upsert can never flip-flop between the two.</para>
///
/// <para><b>A pure function of the store</b>, deliberately: the host has a database, a pump and a
/// receiver, and none of them are involved in deciding whether an episode should be scored.</para>
/// </remarks>
public static class ClosedEpisodeScoring
{
    /// <summary>
    /// Scores every closed episode of a registered session that has no scorecard, and returns the
    /// number newly scored.
    /// </summary>
    /// <remarks>
    /// <para><b>The evidence is honestly empty.</b> A contract-declared episode carries no Proof
    /// Pack - the watcher observed spans and a declared outcome, and neither is evidence of outcome
    /// <i>quality</i>. So <see cref="EpisodeEvidence"/> is built with <c>HasProofPack: false</c> and
    /// <see cref="DeterministicSignalsDeriver"/>'s conservative defaults apply: no verification path,
    /// acceptance unknown, requirements zero. What falls out is <b>Not Scored, with the reason</b> -
    /// which is true, and is the honest first thing an agent can receive.</para>
    ///
    /// <para>It is emphatically <b>not a low score</b>. A derived-signals path that returned 0 for
    /// "nothing was observed" would be a statement about the agent where only a statement about the
    /// evidence is warranted, and it would be indistinguishable from a real failure.</para>
    ///
    /// <para><b>The task class is absent, not invented.</b> The coordination contract carries a goal
    /// and a done-condition but no task class, so the segment is
    /// <see cref="ScoreSegment.Unclassified"/> and therefore not comparable: the episode is scored
    /// and delivered, and ranks nowhere. Supplying a placeholder class to make a leaderboard row
    /// appear would put a value on a surface that reads as meaning something.</para>
    /// </remarks>
    public static int Run(
        IWatcherObservationStore store,
        TimeProvider time,
        string taskClass = ScoreSegment.Unclassified,
        IAdvisoryEvaluator? evaluator = null,
        CalibrationRegistry? registry = null,
        DaydreamRecorder? daydream = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentException.ThrowIfNullOrEmpty(taskClass);

        var scoring = new ScoringService(store, time, daydream);
        var scored = 0;

        foreach (var episode in store.AllEpisodes())
        {
            if (episode.State is not EpisodeState.Closed || store.FindScoredEpisode(episode.EpisodeId) is not null)
            {
                continue;
            }

            // No session record means an audit-imported episode, which the import path owns.
            if (store.FindSession(episode.SessionId) is not { } session)
            {
                continue;
            }

            var signals = DeterministicSignalsDeriver.Derive(episode, new EpisodeEvidence(HasProofPack: false), store);

            scoring.ScoreAndRecord(
                episode,
                signals,
                operatorId: episode.SessionId,
                taskClass: taskClass,
                workspace: WorkspaceKey.From(session.Binding.Repository),
                harness: session.Binding.Harness?.Name,
                model: session.Binding.Model?.Name,
                evaluator: evaluator,
                registry: registry);

            scored++;
        }

        return scored;
    }
}
