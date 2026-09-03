namespace AiDe.Core.Watcher;

/// <summary>
/// A scored episode with its harness/model/operator attribution - the input to the leaderboard and
/// standing. <see cref="Weave"/> is the sum of the scored dimensions' earned points (there is no single
/// stored score; it is derived, DM7).
/// </summary>
public sealed record ScoredEpisode(
    string EpisodeId, string? Harness, string? Model, string OperatorId,
    string TaskClass, string SchemaVersion, Scorecard Scorecard)
{
    public double Weave => Scorecard.Assessments.Where(a => a.EarnedPoints is not null).Sum(a => a.EarnedPoints!.Value);

    public double? CoverageRatio => Scorecard.Coverage is { Required: > 0 } c ? (double)c.Observed / c.Required : null;

    public bool IsScoreable => Scorecard.Verdict is WeaveVerdict.Scored or WeaveVerdict.Partial;
}

/// <summary>The three leaderboard axes (spec US-14). There is deliberately no per-operator facet.</summary>
public enum LeaderboardFacet { Harness, Model, HarnessModel }

/// <summary>
/// One leaderboard cell. A cell below the cohort minimum or one that resolves to a single operator
/// renders Not Comparable, never a rank (spec US-14/US-10). Every comparable cell carries its cohort
/// size and Evidence Coverage.
/// </summary>
public sealed record LeaderboardCell(
    LeaderboardFacet Facet, string Label, int Cohort, double? MedianWeave, double? Coverage,
    int? Rank, bool Comparable, string? NotComparableReason);

/// <summary>A leaderboard for one task class and score schema version (comparisons never cross either).</summary>
public sealed record Leaderboard(string TaskClass, string SchemaVersion, IReadOnlyList<LeaderboardCell> Cells)
{
    public LeaderboardCell? Cell(LeaderboardFacet facet, string label)
        => Cells.FirstOrDefault(c => c.Facet == facet && string.Equals(c.Label, label, StringComparison.Ordinal));
}

/// <summary>
/// Composes the harness / model / harness-model leaderboard within one task class and score schema
/// version (spec US-14, rules 10-11). A facet cell is Comparable only with a cohort of at least the
/// minimum (default 5) AND more than one distinct operator (a single-operator cell is a privacy proxy
/// for one human - US-10); comparable cells rank by median Weave. Deterministic and non-identifying.
/// </summary>
public sealed class LeaderboardComposer
{
    public Leaderboard Compose(IReadOnlyList<ScoredEpisode> episodes, string taskClass, string schemaVersion, int cohortMinimum = 5)
    {
        ArgumentNullException.ThrowIfNull(episodes);

        // Segmentation (rule 10): never compare across task class or score schema version.
        var scoped = episodes
            .Where(e => e.IsScoreable
                && string.Equals(e.TaskClass, taskClass, StringComparison.Ordinal)
                && string.Equals(e.SchemaVersion, schemaVersion, StringComparison.Ordinal))
            .ToList();

        var cells = new List<LeaderboardCell>();
        cells.AddRange(FacetCells(scoped, LeaderboardFacet.Harness, e => e.Harness, cohortMinimum));
        cells.AddRange(FacetCells(scoped, LeaderboardFacet.Model, e => e.Model, cohortMinimum));
        cells.AddRange(FacetCells(scoped, LeaderboardFacet.HarnessModel,
            e => e.Harness is null || e.Model is null ? null : $"{e.Harness} / {e.Model}", cohortMinimum));

        return new Leaderboard(taskClass, schemaVersion, cells);
    }

    private static IEnumerable<LeaderboardCell> FacetCells(
        IReadOnlyList<ScoredEpisode> episodes, LeaderboardFacet facet, Func<ScoredEpisode, string?> label, int cohortMinimum)
    {
        var groups = episodes
            .Select(e => (Label: label(e), Episode: e))
            .Where(x => x.Label is not null)
            .GroupBy(x => x.Label!, x => x.Episode, StringComparer.Ordinal)
            .Select(g =>
            {
                var members = g.ToList();
                var cohort = members.Count;
                var operators = members.Select(m => m.OperatorId).Distinct(StringComparer.Ordinal).Count();

                string? reason = null;
                if (cohort < cohortMinimum)
                {
                    reason = $"cohort {cohort} < {cohortMinimum}";
                }
                else if (operators < 2)
                {
                    reason = "single operator (privacy-protected small cohort)";
                }

                var comparable = reason is null;
                return new LeaderboardCell(
                    facet, g.Key, cohort,
                    comparable ? Median(members.Select(m => m.Weave)) : null,
                    comparable ? Median(members.Where(m => m.CoverageRatio is not null).Select(m => m.CoverageRatio!.Value)) : null,
                    Rank: null,
                    comparable,
                    reason);
            })
            .ToList();

        // Rank the comparable cells within this facet by median Weave, best first.
        var ranked = groups.Where(c => c.Comparable)
            .OrderByDescending(c => c.MedianWeave)
            .ThenBy(c => c.Label, StringComparer.Ordinal)
            .Select((c, i) => c with { Rank = i + 1 });

        var notComparable = groups.Where(c => !c.Comparable);
        return ranked.Concat(notComparable);
    }

    private static double? Median(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        if (sorted.Count == 0)
        {
            return null;
        }

        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }
}

/// <summary>One evidence-backed reason for one dimension (spec US-16 - one reason per dimension).</summary>
public sealed record DimensionReason(ScoreDimension Dimension, string Reason);

/// <summary>
/// An agent's per-turn standing (spec US-16). It carries the harness-model rank, the recent trend, and
/// one evidence-backed reason per dimension - and <b>deliberately no single aggregate scalar</b> to
/// optimize (the anti-Goodhart stance: there is no <c>Score</c> field, only a relative rank, a trend
/// direction, and per-dimension evidence).
/// </summary>
/// <param name="Trend">
/// Signed movement in Weave points against the previous episode in the same cohort, or
/// <c>null</c> when there is no previous episode to move against.
/// </param>
/// <remarks>
/// <b>Trend is nullable, and that is the point.</b> It was <c>int</c>, so an agent's first scored
/// episode reported <b>0</b> — the same value as "you did not move" — in the one feature whose
/// purpose is telling an agent whether it is improving or regressing. The spec is explicit that
/// "every displayed evaluation or learning claim has evidence/confidence, or renders Not Recorded",
/// and no-history is exactly that case.
/// </remarks>
public sealed record AgentStanding(
    string EpisodeId, string? Harness, string? Model,
    int? Rank, int? Cohort, int? Trend, bool RankComparable,
    IReadOnlyList<DimensionReason> Reasons);

/// <summary>
/// Turns a scored episode + the leaderboard into per-turn standing (spec US-16). The rank is shown only
/// when the harness-model cell is comparable (else RankComparable is false and only trend + reasons
/// render); the reasons are one per dimension from the scorecard; no single optimizable number is exposed.
/// </summary>
public sealed class StandingComposer
{
    /// <summary>
    /// Composes one episode's standing, deriving the trend from <paramref name="history"/>.
    /// </summary>
    /// <remarks>
    /// <b>The history is a parameter, not the trend.</b> This took <c>int trend</c> and nothing in
    /// src/ produced one — the caller was expected to compute it and there was no caller at all. A
    /// value someone must remember to supply is a value that will eventually be supplied wrongly or
    /// not at all; a history the method derives from cannot be forgotten, because the method cannot
    /// be called without it.
    /// </remarks>
    public AgentStanding Compose(
        ScoredEpisode subject, Leaderboard board, IReadOnlyList<ScoredEpisode> history)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(history);

        var trend = TrendFor(subject, history);

        LeaderboardCell? cell = subject.Harness is not null && subject.Model is not null
            ? board.Cell(LeaderboardFacet.HarnessModel, $"{subject.Harness} / {subject.Model}")
            : null;

        var comparable = cell is { Comparable: true };
        var reasons = subject.Scorecard.Assessments
            .Select(a => new DimensionReason(a.Dimension, a.Rationale))
            .ToList();

        return new AgentStanding(
            subject.EpisodeId, subject.Harness, subject.Model,
            comparable ? cell!.Rank : null,
            comparable ? cell!.Cohort : null,
            trend,
            comparable,
            reasons);
    }

    /// <summary>
    /// Movement in Weave points against the previous episode of the same cohort, or null.
    /// </summary>
    /// <remarks>
    /// <para><b>The cohort is harness + model + task class + schema</b> — the same partition the
    /// leaderboard compares within (rule 11). A trend across task classes would compare a refactor
    /// against a spike and call the difference improvement.</para>
    ///
    /// <para><b>Null when there is no previous episode</b>, never zero. Zero means "scored the same";
    /// absent means "there is nothing to compare against yet", and an agent reading its first
    /// standing must be able to tell those apart.</para>
    ///
    /// <para>Ordered by <c>EvaluatedAt</c> and filtered to episodes strictly before the subject's,
    /// so a re-scored or backfilled episode does not become its own predecessor.</para>
    /// </remarks>
    private static int? TrendFor(ScoredEpisode subject, IReadOnlyList<ScoredEpisode> history)
    {
        var previous = history
            .Where(e => !string.Equals(e.EpisodeId, subject.EpisodeId, StringComparison.Ordinal))
            .Where(e => string.Equals(e.Harness, subject.Harness, StringComparison.Ordinal)
                && string.Equals(e.Model, subject.Model, StringComparison.Ordinal)
                && string.Equals(e.TaskClass, subject.TaskClass, StringComparison.Ordinal)
                && string.Equals(e.SchemaVersion, subject.SchemaVersion, StringComparison.Ordinal))
            .Where(e => e.Scorecard.EvaluatedAt <= subject.Scorecard.EvaluatedAt)
            .OrderByDescending(e => e.Scorecard.EvaluatedAt)
            .ThenByDescending(e => e.EpisodeId, StringComparer.Ordinal)
            .FirstOrDefault();

        return previous is null ? null : (int)Math.Round(subject.Weave - previous.Weave);
    }
}
