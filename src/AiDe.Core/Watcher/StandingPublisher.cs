using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiDe.Core.Watcher;

/// <summary>
/// Delivers a session's per-turn standing to the agent, as a file beside the contract log (US-16).
/// </summary>
/// <remarks>
/// <para><b>Why a file.</b> US-16's deliverable is that the agent <b>receives</b> its standing.
/// C1 added a <c>standing</c> tool to <see cref="Mcp.McpToolGateway"/> — correct, tested, and
/// unreachable: the gateway has no caller and no transport, and ADR-0004 records the transport as
/// spiked and never built. Adding a tool nothing can call does not deliver a story about receiving.
/// <c>AIDE_CONTRACT_LOG</c> is the channel that already exists in both directions; the agent is
/// handed the directory, and the ingest proves the path works.</para>
///
/// <para><b>It is still a pull.</b> Nothing is injected into the agent's context — the file sits
/// there and the agent chooses to read it. That distinction is what ADR-0019's anti-Goodhart section
/// turns on: an agent shown its score every turn regardless is a different decision from one that
/// asks.</para>
///
/// <para><b>The subdirectory and the extension are both load-bearing.</b>
/// <c>CoordinationContractLog</c> reads <c>Directory.EnumerateFiles(logDir, "*.jsonl")</c> with no
/// <c>SearchOption</c> — top-directory-only. A standing written as <c>.jsonl</c> in the root would be
/// parsed by the contract pump every tick and counted MALFORMED, so this feature would work while
/// the ingest counters filled with corruption that was not corruption. Two independent properties
/// keep it invisible, and both are asserted by tests rather than assumed.</para>
///
/// <para><b>No new environment variable.</b> One address for the channel, with the direction legible
/// from the path.</para>
/// </remarks>
public static class StandingPublisher
{
    /// <summary>The subdirectory of the coordination log that carries outbound standings.</summary>
    public const string DirectoryName = "standing";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>
    /// Writes the standing for <paramref name="episodeId"/>, or returns null when there is none.
    /// </summary>
    /// <returns>The path written, or <c>null</c> when the session has no scored episode.</returns>
    /// <remarks>
    /// Returns null rather than writing an empty standing: an empty one reads as "you have no rank
    /// and no reasons", which is a claim about the agent rather than about the absence of a score
    /// (DC-087). No file is the honest state, and the agent can tell the two apart.
    /// </remarks>
    public static string? Publish(
        string coordLogDirectory,
        string sessionId,
        IReadOnlyList<ScoredEpisode> scoredEpisodes,
        string? episodeId)
    {
        ArgumentException.ThrowIfNullOrEmpty(coordLogDirectory);
        ArgumentException.ThrowIfNullOrEmpty(sessionId);
        ArgumentNullException.ThrowIfNull(scoredEpisodes);

        if (string.IsNullOrEmpty(episodeId))
        {
            return null;
        }

        var subject = scoredEpisodes.FirstOrDefault(
            e => string.Equals(e.EpisodeId, episodeId, StringComparison.Ordinal));

        if (subject is null)
        {
            return null;
        }

        var board = new LeaderboardComposer().Compose(scoredEpisodes, subject.TaskClass, subject.SchemaVersion);
        var standing = new StandingComposer().Compose(subject, board, scoredEpisodes);

        var directory = Path.Combine(coordLogDirectory, DirectoryName);
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, FileNameFor(sessionId));

        // Written whole and replaced, never appended. The agent reads the current standing; two of
        // them in one file is not a document anything can parse, and an append-only shape here would
        // look like the contract log without being one.
        //
        // Via a temp file and a move so a reader never observes a half-written document — the file
        // is read by another process on its own schedule, so "in the middle of a write" is a state
        // that will occur rather than one that might.
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(standing, Json));
        File.Move(temporary, path, overwrite: true);

        return path;
    }

    /// <summary>
    /// A session id turned into a file name the filesystem will not reinterpret.
    /// </summary>
    /// <remarks>
    /// An agent session id is <c>agent:&lt;name&gt;#&lt;hex&gt;</c>, and on NTFS a colon opens an
    /// <b>alternate data stream</b>: <c>Path.Combine(dir, "agent:claude#ab.json")</c> writes the file
    /// "agent" carrying the stream "claude#ab.json". The write succeeds, the bytes are there, and
    /// nothing enumerating the directory can see them. That is DC-086, found in the coordination log
    /// this afternoon; this is the same id reaching the same filesystem by a different route.
    /// </remarks>
    public static string FileNameFor(string sessionId)
    {
        var safe = new string([.. sessionId.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '-' : c)]);
        return safe + ".json";
    }
}
