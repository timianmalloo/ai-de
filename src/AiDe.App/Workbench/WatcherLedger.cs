using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AiDe.Core.Watcher;

namespace AiDe.App.Workbench;

/// <summary>
/// The Ledger read: every work episode the watcher has recorded, newest first. Where the Leaderboard
/// RANKS scored episodes and the Board shows breadcrumb messages, the Ledger is the raw append-only
/// record — "what work has this workspace seen", scored or not — the third view over the same
/// <see cref="IWatcherObservationStore"/> the Board and Leaderboard read.
/// </summary>
public interface IWatcherLedgerQuery
{
    /// <summary>Every recorded work episode. Ordering is the caller's concern (the row builder sorts).</summary>
    IReadOnlyList<WorkEpisode> GetEpisodes();
}

/// <summary>Reads the work-episode ledger straight off the observation store (its append-only fact table).</summary>
public sealed class WatcherLedgerQuery(IWatcherObservationStore store) : IWatcherLedgerQuery
{
    private readonly IWatcherObservationStore _store = store;

    public IReadOnlyList<WorkEpisode> GetEpisodes() => _store.AllEpisodes();
}

/// <summary>One dense line in the Ledger: what the episode was for, when it opened, and whether it closed.</summary>
/// <remarks>
/// Pure and dependency-free so the label mapping is unit-tested off the UI thread, the same discipline
/// as <see cref="SessionRowPresenter"/> and the leaderboard row.
/// </remarks>
public sealed record LedgerRow(string DisplayLabel)
{
    private const string NoGoal = "(no goal recorded)";

    public static LedgerRow From(WorkEpisode episode)
    {
        var state = episode.State == EpisodeState.Closed ? "Closed" : "Active";
        var when = episode.OpenedAt.ToLocalTime().ToString("MMM d HH:mm", CultureInfo.CurrentCulture);
        var goal = string.IsNullOrWhiteSpace(episode.Goal.Statement) ? NoGoal : episode.Goal.Statement.Trim();
        return new LedgerRow($"{state} · {when} · {goal}");
    }

    /// <summary>Newest first — a ledger reads top-down as most-recent-first.</summary>
    public static IReadOnlyList<LedgerRow> Rows(IReadOnlyList<WorkEpisode> episodes) =>
        [.. episodes.OrderByDescending(e => e.OpenedAt).Select(From)];

    /// <summary>The honest status line: whether observation is wired, and how much it has recorded.</summary>
    public static string StatusFor(IWatcherLedgerQuery? query)
    {
        if (query is null)
        {
            // Same shape as the Board/Sessions no-store case: the surface is present, observation is not.
            return "Work-episode observation is not available.";
        }

        var count = query.GetEpisodes().Count;
        return count == 0
            ? "No work episodes recorded yet — an episode appears when a session declares a goal."
            : $"{count} work episode(s) recorded.";
    }
}
