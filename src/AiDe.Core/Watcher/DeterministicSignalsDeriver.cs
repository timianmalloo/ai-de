namespace AiDe.Core.Watcher;

/// <summary>
/// The observable audit-entry evidence a signal derivation grounds on (conn-10). Deliberately minimal:
/// today the only honest, non-fuzzy verification signal from an audit entry is whether the recorded work
/// shipped a committed Proof Pack artifact (a <c>docs/proof/</c> path). More fields join here only when a
/// telemetry convention makes them observable - never a heuristic guess (spec L127, NG1).
/// </summary>
public sealed record EpisodeEvidence(bool HasProofPack);

/// <summary>An imported closed Work Episode paired with the audit evidence a signal derivation needs.</summary>
public sealed record ImportedEpisode(WorkEpisode Episode, EpisodeEvidence Evidence);

/// <summary>
/// Derives a <see cref="DeterministicEpisodeSignals"/> for an imported closed episode from what is
/// <b>honestly observable</b> - a committed Proof Pack (the only verification signal an audit entry
/// carries), the declared close outcome, and any spans recorded after the close. Everything not observable
/// is a conservative default that the scorer renders as Not-Recorded or Not-Scored, never a fabricated
/// value: acceptance stays null (unknown, not "met"), regression false (not "no regression exists"),
/// guidance/coordination requirements 0 (those dimensions render Not-Recorded), coverage uncalibrated.
/// Pure and deterministic. See <c>docs/design/watcher-signals-derivation.md</c>.
/// </summary>
public static class DeterministicSignalsDeriver
{
    public static DeterministicEpisodeSignals Derive(
        WorkEpisode episode, EpisodeEvidence evidence, IWatcherObservationStore store)
    {
        ArgumentNullException.ThrowIfNull(episode);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(store);

        // The one honest verification signal: a committed Proof Pack. No proof pack -> HasVerificationPath
        // false -> the scorer renders Not-Scored ("no minimum verification path"), which is correct.
        var verified = evidence.HasProofPack;

        // Work observed after the done condition (PACK-O drift). Endpoints are inclusive; imported episodes
        // typically have no spans in this store, so this is 0 - honest, never a wrong count.
        var actionsAfterDone = episode.ClosedAt is { } closed
            ? store.SpanCountInInterval(episode.SessionId, closed, DateTimeOffset.MaxValue)
            : 0;

        return new DeterministicEpisodeSignals(
            HasVerificationPath: verified,
            AcceptanceCriteriaMet: null,                 // unknown from an audit entry (null != false)
            RequiredVerificationExecuted: verified,      // the proof pack IS the executed-verification record
            RegressionPresent: false,                    // none observed (not a claim that none exists)
            UnresolvedFloorBlockers: new HashSet<FloorDomain>(),
            ActionsAfterDoneCondition: actionsAfterDone,
            PrematureCompletion: false,                  // not observable from an audit entry
            RequiredGuidanceTriggers: 0,                 // not observable -> GuidanceAdherence Not-Recorded
            SatisfiedGuidanceTriggers: 0,
            RequiredCoordinationSignals: 0,              // not observable -> CoordinationAndLearning Not-Recorded
            ObservedCoordinationSignals: 0,
            CoverageCalibrated: false,                   // no calibrated required-total -> coverage Not-Recorded
            RequiredSignalTotal: 0,
            ObservedSignalTotal: 0);
    }
}
