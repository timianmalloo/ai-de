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

    public static CurrentnessResult Evaluate(AttemptRow attempt, int? currentRevisionNo, string? liveProjectionSha)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        var started = Stopwatch.GetTimestamp();

        var result = Compare(attempt, currentRevisionNo, liveProjectionSha);

        FoldSignal.Record("currentness", Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return result;
    }

    private static CurrentnessResult Compare(AttemptRow attempt, int? currentRevisionNo, string? liveProjectionSha)
    {
        if (currentRevisionNo is null || liveProjectionSha is null || attempt.ProjectionSha is null)
        {
            return new CurrentnessResult(Currentness.NotRecorded, null);
        }

        return currentRevisionNo == attempt.RevisionNo
            && string.Equals(liveProjectionSha, attempt.ProjectionSha, StringComparison.Ordinal)
            ? new CurrentnessResult(Currentness.Current, null)
            : new CurrentnessResult(Currentness.Stale, StaleCopy);
    }
}
