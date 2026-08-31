using System.Globalization;
using System.Text.Json;

namespace AiDe.Core.Watcher;

/// <summary>
/// Reads committed AI-Forward audit-log entries that declare a goal-state (a top-level <c>goal</c> +
/// <c>done_when</c> + <c>session</c>, AL5b / front-matter CT19) and turns each into an <b>imported,
/// closed</b> <see cref="WorkEpisode"/>. This is the episode source that makes real Work Episodes exist
/// for the watcher: an audit entry is a durable, human/agent-committed record of a bounded goal that was
/// worked and closed, so importing it reads a <i>fact</i> - it does not fabricate a goal (spec L127, no
/// guessing NG1), and it does not forge a live operation (these are historical facts recorded directly
/// via <see cref="IWatcherObservationStore.RecordEpisode"/>, the same way the coordination pump imports
/// registrations - the live, capability-verified path is <see cref="IWorkEpisodeService"/> for real-time
/// sessions). Entries without all three fields are skipped: not every audit entry is an episode.
/// </summary>
public static class AuditLogEpisodeSource
{
    /// <summary>Parses JSONL audit-log lines into imported closed episodes; malformed lines are skipped.</summary>
    public static IReadOnlyList<WorkEpisode> Parse(IEnumerable<string> jsonlLines)
    {
        ArgumentNullException.ThrowIfNull(jsonlLines);

        var episodes = new List<WorkEpisode>();
        foreach (var line in jsonlLines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            WorkEpisode? episode;
            try
            {
                episode = FromLine(line);
            }
            catch (JsonException)
            {
                continue; // a corrupt line is skipped, never a wrong episode (IO8)
            }

            if (episode is not null)
            {
                episodes.Add(episode);
            }
        }

        return episodes;
    }

    /// <summary>Reads a repo's <c>audit-log.jsonl</c> into imported episodes; a missing file yields none.</summary>
    public static IReadOnlyList<WorkEpisode> ReadFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return File.Exists(path) ? Parse(File.ReadLines(path)) : [];
    }

    private static WorkEpisode? FromLine(string line)
    {
        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;

        var goal = ReadString(root, "goal");
        var doneWhen = ReadString(root, "done_when");
        var session = ReadString(root, "session");

        // The three fields that make an entry a scoreable episode (AL5b). Any missing -> not an episode.
        if (string.IsNullOrWhiteSpace(goal) || string.IsNullOrWhiteSpace(doneWhen) || string.IsNullOrWhiteSpace(session))
        {
            return null;
        }

        var id = ReadString(root, "id");
        var episodeId = string.IsNullOrEmpty(id) ? $"ep:{Guid.NewGuid():N}" : $"ep:{id}";

        var datetime = ReadTimestamp(root, "datetime");
        var startedAt = ReadTimestamp(root, "started_at");
        var openedAt = startedAt ?? datetime ?? DateTimeOffset.UnixEpoch;
        var closedAt = datetime ?? openedAt;

        return new WorkEpisode(
            EpisodeId: episodeId,
            SessionId: session!,
            Generation: new EpisodeGeneration(1),
            Goal: new Goal(goal!),
            DoneWhen: new DoneCondition(doneWhen!),
            NotInScope: ReadString(root, "not_in_scope"),
            OpenedAt: openedAt,
            ClosedAt: closedAt,
            Outcome: MapOutcome(ReadString(root, "outcome")));
    }

    /// <summary>
    /// Maps an audit <c>outcome</c> to an <see cref="EpisodeOutcome"/>. Only an explicit success is a met
    /// outcome (so the scorer's honest-completion check can trip Focus for a non-success close); a blocked
    /// close is Blocked; anything else (failed/partial/unknown) is Abandoned - never silently Completed.
    /// </summary>
    private static EpisodeOutcome MapOutcome(string? outcome) => outcome?.ToLowerInvariant() switch
    {
        "success" => EpisodeOutcome.Completed,
        "blocked" => EpisodeOutcome.Blocked,
        _ => EpisodeOutcome.Abandoned,
    };

    private static string? ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateTimeOffset? ReadTimestamp(JsonElement root, string name)
    {
        var text = ReadString(root, name);
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null;
    }
}
