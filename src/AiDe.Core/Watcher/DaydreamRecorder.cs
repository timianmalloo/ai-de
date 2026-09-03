using System.Security.Cryptography;
using System.Text;

namespace AiDe.Core.Watcher;

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
/// <para><b>The production call site is <see cref="ScoringService"/></b>, immediately after the
/// scorecard is recorded — one site rather than one per scoring producer, because that is the single
/// place a <see cref="ScoredEpisode"/> comes into existence. The shell supplies a recorder built on
/// the open workspace, so the record is written into the repository the work happened in.</para>
///
/// <para><b>It writes nothing for an agent&apos;s episode today, and that is measured.</b> A
/// contract-declared episode carries no Proof Pack, so nothing is observed: no floor trips, every
/// dimension is Not-Recorded, and a Not-Recorded dimension has a null rubric and so cannot fall
/// short. The signature is therefore unremarkable and <see cref="Observe"/> declines it. The concern
/// before wiring was the opposite — that every agent episode would carry the SAME Not-Scored
/// signature and one useless pattern would dominate the recurrence report — and
/// <c>WhatDaydreamSeesInAnAgentEpisodeTests</c> refutes it and is written to fail the day an agent
/// episode does carry evidence. The audit-import producer, which reads committed Proof Packs, is
/// what feeds this today.</para>
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
    /// Records one episode as an observation, and reports whether it wrote one.
    /// </summary>
    /// <returns>
    /// <c>true</c> only when a line was written. <c>false</c> covers three different situations —
    /// an unremarkable episode, an unavailable record, and a failed write — and the caller that
    /// needs to tell them apart asks <see cref="DaydreamRepositoryRecord.Unavailable"/>. Nothing
    /// here reports a write it did not make.
    /// </returns>
    public bool Observe(ScoredEpisode episode)
    {
        ArgumentNullException.ThrowIfNull(episode);

        var signature = DaydreamSignature.For(episode, lowRubricThreshold);

        // A clean episode is not a pattern to learn from. Recording them would fill the record with
        // "work went well" — true, recurrent, and useless as a lesson (US-9).
        if (signature.IsUnremarkable)
        {
            return false;
        }

        return _record.Append(new DaydreamObservation(
            IdFor(episode.EpisodeId, signature), signature, episode.EpisodeId, _clock()));
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
