using System.Diagnostics;
using AiDe.Core.Store.Records;

namespace AiDe.Core.Workbench.Records.Folds;

/// <summary>Whether an attempt's output still belongs to the live draft.</summary>
public enum Currentness
{
    Current,
    Stale,

    /// <summary>Either side of the comparison is unknown. Never read as current.</summary>
    NotRecorded,
}

/// <summary>A currentness verdict; <see cref="Copy"/> is the operator-facing line for a stale result, else null.</summary>
public sealed record CurrentnessResult(Currentness State, string? Copy);

/// <summary>
/// ADR-0040 §5 for WL-1a: an attempt bound to <c>(notebook_id, revision_no, projection_sha)</c> is
/// stale when the notebook's current revision or the live envelope's projection moved. The attempt
/// row is retained; staleness is derived, never stored.
/// </summary>
public static class CurrentnessFold
{
    /// <summary>Spec §C3, verbatim.</summary>
    public const string StaleCopy = "This output belongs to an earlier draft. Run again to refresh it.";

    /// <summary>A stored attempt, judged by its own <c>projection_sha</c> against the live projection sha.</summary>
    public static CurrentnessResult Evaluate(AttemptRow attempt, int? currentRevisionNo, string? liveProjectionSha)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        return Evaluate(attempt.RevisionNo, currentRevisionNo, attempt.ProjectionSha, liveProjectionSha);
    }

    /// <summary>
    /// The one definition. <paramref name="ranSha"/> and <paramref name="liveSha"/> are the same kind of digest, taken
    /// by the caller's one definition of it (a projection sha for a stored attempt, a body sha for a notebook result);
    /// the fold only compares them, so neither side is ever a recompile's answer.
    /// </summary>
    public static CurrentnessResult Evaluate(int ranRevisionNo, int? currentRevisionNo, string? ranSha, string? liveSha)
    {
        var started = Stopwatch.GetTimestamp();

        var result = Compare(ranRevisionNo, currentRevisionNo, ranSha, liveSha);

        FoldSignal.Record("currentness", Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return result;
    }

    private static CurrentnessResult Compare(int ranRevisionNo, int? currentRevisionNo, string? ranSha, string? liveSha)
    {
        if (currentRevisionNo is null || liveSha is null || ranSha is null)
        {
            return new CurrentnessResult(Currentness.NotRecorded, null);
        }

        return currentRevisionNo == ranRevisionNo
            && string.Equals(liveSha, ranSha, StringComparison.Ordinal)
            ? new CurrentnessResult(Currentness.Current, null)
            : new CurrentnessResult(Currentness.Stale, StaleCopy);
    }
}
