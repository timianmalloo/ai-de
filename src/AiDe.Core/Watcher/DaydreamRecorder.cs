using System.Security.Cryptography;
using System.Text;

namespace AiDe.Core.Watcher;

/// <summary>
/// What one call to <see cref="DaydreamRecorder.Observe"/> did, and why.
/// </summary>
/// <remarks>
/// <para><b>Four outcomes rather than a bool, because the bool meant three things and two of them
/// are opposites.</b> A signature with no floors and no shortfalls can arise two ways: the episode
/// was assessed and nothing went wrong, or <b>nothing was assessed at all</b>. Both are
/// "unremarkable" to the signature and they mean the reverse of each other — one is the system
/// working, the other is the system seeing nothing.</para>
///
/// <para><b>Measured, on 2026-09-02, not predicted.</b> An episode with no Proof Pack scores
/// <c>NotScored</c> with <b>no assessments at all</b> and no tripped floors — a floor is an
/// <i>observed</i> failure, so nothing can trip when nothing is observed. Its signature is therefore
/// empty and Daydream declines it. That is correct: there is nothing to learn from an episode
/// nobody watched. But reporting it identically to a clean episode would let "Daydream is quiet"
/// read as "work is going well", which is DC-025's shape — and the design's own §8 says the operator
/// question this must answer is <i>is Daydream seeing anything</i>.</para>
/// </remarks>
public enum DaydreamObservationOutcome
{
    /// <summary>A pattern was written to the record.</summary>
    Recorded,

    /// <summary>Assessed, and nothing fell short. The system working; not a lesson.</summary>
    NothingWentWrong,

    /// <summary>
    /// <b>Nothing was assessed</b> — no dimension carried a rubric, so no shortfall was possible.
    /// </summary>
    /// <remarks>
    /// Not a quiet day. The scorer had no evidence, so Daydream is deaf to this episode however many
    /// of them arrive. An operator seeing this repeatedly is being told the instrumentation is
    /// missing, not that the work is clean.
    /// </remarks>
    NothingWasAssessed,

    /// <summary>There is no record to write to. <see cref="DaydreamRepositoryRecord.Unavailable"/> says why.</summary>
    RecordUnavailable,
}

/// <summary>
/// Turns a scored episode into an observation in the repository's Daydream record.
/// </summary>
/// <remarks>
/// <para><b>The one call site Daydream needs.</b> Everything downstream — recurrence, candidates,
/// the promotion staircase, the pane — folds from what this writes. It takes a
/// <see cref="ScoredEpisode"/> because that is what is actually persisted:
/// <c>DeterministicEpisodeSignals</c> is never stored, so a signature derived from the scorer's
/// inputs could be produced once, live, and never again.</para>
///
/// <para><b>Called after a scorecard is recorded, not before.</b> Observing an episode the store
/// does not yet hold would put a pattern in the record whose evidence a reader cannot follow.</para>
///
/// <para><b>The one call site is <c>ScoringService.ScoreAndRecord</c>, immediately after
/// <c>RecordScorecard</c></b> — one level below where this class originally proposed it, because
/// <c>ScoringService</c> is the single place a <see cref="ScoredEpisode"/> comes into existence and
/// both producers pass through it. Placing it on the tick pass instead would have made two call
/// sites, and the audit-import one would have been the forgotten one.</para>
///
/// <para><b>What it actually sees today, measured rather than assumed.</b> An agent's episode
/// carries no Proof Pack, so it scores <c>NotScored</c> with no assessments and no tripped floors,
/// and this class declines it as <see cref="DaydreamObservationOutcome.NothingWasAssessed"/>. The
/// producer feeding the record today is therefore <b>audit-import</b>, whose episodes read committed
/// Proof Packs and do trip floors. That is not a defect to fix here — it is the instrumentation gap
/// upstream, and the outcome enum exists so it reports as a gap rather than as a quiet day.</para>
///
/// <para><b>Suppression is deliberately NOT here.</b> <see cref="DreamCorpusReader"/> marks a
/// pattern already-known so it stops being re-<i>proposed</i>; it must never stop it being
/// <i>observed</i>. An observation is evidence, and dropping evidence because a lesson was already
/// written would make the record understate what happened — and would make a retraction in the pack
/// unrecoverable, since the occurrences it needed were never kept.</para>
/// </remarks>
public sealed class DaydreamRecorder(
    DaydreamRepositoryRecord record,
    Func<DateTimeOffset>? clock = null,
    int lowRubricThreshold = 2)
{
    private readonly DaydreamRepositoryRecord _record =
        record ?? throw new ArgumentNullException(nameof(record));

    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);

    /// <summary>
    /// Records one episode as an observation, and says what it did.
    /// </summary>
    /// <returns>
    /// Which of the four <see cref="DaydreamObservationOutcome"/> cases applied. Nothing here
    /// reports a write it did not make, and <b>nothing collapses "nothing went wrong" into "nothing
    /// was assessed"</b> — see the enum for why those two must not render alike.
    /// </returns>
    public DaydreamObservationOutcome Observe(ScoredEpisode episode)
    {
        ArgumentNullException.ThrowIfNull(episode);

        var signature = DaydreamSignature.For(episode, lowRubricThreshold);

        if (signature.IsUnremarkable)
        {
            // A clean episode is not a pattern to learn from — recording them would fill the record
            // with "work went well", which is true, recurrent, and useless as a lesson (US-9).
            //
            // But an episode nothing assessed reaches this same branch, from the opposite cause, and
            // the two must not report alike. Asked of the RUBRICS rather than of the verdict: a
            // shortfall is a low rubric, so a card with no rubric anywhere is one this signature can
            // never key on, whatever its verdict says.
            return episode.Scorecard.Assessments.Any(a => a.Rubric0to4 is not null)
                ? DaydreamObservationOutcome.NothingWentWrong
                : DaydreamObservationOutcome.NothingWasAssessed;
        }

        return _record.Append(new DaydreamObservation(
                IdFor(episode.EpisodeId, signature), signature, episode.EpisodeId, _clock()))
            ? DaydreamObservationOutcome.Recorded
            : DaydreamObservationOutcome.RecordUnavailable;
    }

    /// <summary>
    /// A deterministic id for one episode observed under one signature.
    /// </summary>
    /// <remarks>
    /// Deterministic rather than a fresh GUID so that re-scoring the same episode produces the same
    /// id — which is what lets a content union across two worktrees collapse a genuine duplicate
    /// instead of preserving two rows that mean one thing. It is not a uniqueness guarantee and
    /// nothing relies on it as one: the fold deduplicates by episode, so recurrence cannot be
    /// manufactured however many rows arrive.
    /// </remarks>
    internal static string IdFor(string episodeId, DaydreamSignature signature)
    {
        var material = string.Join(
            "",
            episodeId, signature.TaskClass, signature.SchemaVersion,
            signature.Verdict.ToString(), signature.Floors, signature.Shortfalls);

        return "obs-" + Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(material)))[..16];
    }
}
