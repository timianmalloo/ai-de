using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiDe.App.Workbench.Sessions;

/// <summary>One entry in the Recent sessions list: which session, in which workspace.</summary>
/// <param name="SessionId">The session's id, which names its directory under the workspace.</param>
/// <param name="Name">Its operator-facing name.</param>
/// <param name="WorkspaceRoot">The workspace it is bound to — reopening restores this first.</param>
public sealed record RecentSessionEntry(
    [property: JsonPropertyName("sessionId")] string SessionId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("workspaceRoot")] string WorkspaceRoot);

/// <summary>
/// The Recent sessions list — <b>new construction</b> (R13 b3).
/// </summary>
/// <remarks>
/// <para><b>It is not <c>MainMenuBuilder.RecentWorkspaces</c>, and could not be.</b> That list is
/// installation-scoped and holds repository paths; this holds sessions, which live <i>inside</i> a
/// workspace and carry the workspace they are bound to. The plan named this as new construction
/// after Revision 1 asserted an existing destination that did not exist (DC-116).</para>
///
/// <para><b>Stored beside the shell's own state, for the reason its sibling records.</b> A recent
/// list kept inside a workspace is invisible from the first-run window that most needs it. The two
/// files sit in the same state directory and are read the same way.</para>
///
/// <para><b>An entry whose workspace is gone is dropped on read.</b> Reopening it could not restore
/// anything, and a menu offering something that cannot work teaches the user to distrust the menu.
/// The session directory itself is deliberately <i>not</i> required to exist: a session whose
/// workspace is present but whose directory was deleted still restores its workspace, which is the
/// half R13 b3 names.</para>
/// </remarks>
public static class RecentSessions
{
    /// <summary>The file name in the shell's state directory.</summary>
    public const string FileName = "recent-sessions.json";

    /// <summary>How many entries are kept. The same cap as the recent-workspaces list.</summary>
    public const int Cap = 8;

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    /// <summary>Recently opened sessions, most recent first, with unreachable workspaces dropped.</summary>
    public static IReadOnlyList<RecentSessionEntry> All(string? stateDirectory)
    {
        if (string.IsNullOrEmpty(stateDirectory))
        {
            return [];
        }

        var path = Path.Combine(stateDirectory, FileName);
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            var entries = JsonSerializer.Deserialize<List<RecentSessionEntry>>(File.ReadAllText(path)) ?? [];
            return [.. entries.Where(e => Directory.Exists(e.WorkspaceRoot)).Take(Cap)];
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>Records a session as recently opened. Newest first, deduplicated by id, capped.</summary>
    public static void Remember(string? stateDirectory, RecentSessionEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrEmpty(stateDirectory) || string.IsNullOrEmpty(entry.SessionId))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(stateDirectory);
            var path = Path.Combine(stateDirectory, FileName);

            var existing = File.Exists(path)
                ? JsonSerializer.Deserialize<List<RecentSessionEntry>>(File.ReadAllText(path)) ?? []
                : [];

            List<RecentSessionEntry> updated =
            [
                entry,
                .. existing.Where(e => !string.Equals(e.SessionId, entry.SessionId, StringComparison.Ordinal)),
            ];

            File.WriteAllText(path, JsonSerializer.Serialize(updated.Take(Cap), Json));
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException)
        {
            // A recent list that cannot be written is a convenience nobody gets, not a failure that
            // should stop a session being created.
        }
    }

    /// <summary>The entry for a session id, or null when it is not in the list (or is unreachable).</summary>
    public static RecentSessionEntry? Find(string? stateDirectory, string sessionId) =>
        All(stateDirectory).FirstOrDefault(
            e => string.Equals(e.SessionId, sessionId, StringComparison.Ordinal));
}
