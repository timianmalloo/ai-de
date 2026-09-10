using System.Text.Json;
using System.Text.Json.Nodes;

namespace AiDe.Core.Sessions;

/// <summary>
/// Reads and writes one session's <c>session.json</c> and <c>session-events.jsonl</c> (clauses 1-3).
/// </summary>
/// <remarks>
/// <para><b>Toggles apply to new runs only (clause 3).</b> <see cref="SessionConfig"/> is an
/// immutable record; <see cref="SetEnabledBackends"/> never mutates an existing instance, it writes a
/// new one. A caller that already captured a <see cref="SessionConfig"/> (modelling a run reading its
/// config at start) is holding a value a later toggle cannot reach — proven in
/// <c>SessionConfigStoreTests.SetEnabledBackends_NeverMutatesAConfigARunAlreadyCaptured</c>. The
/// append-only event log gives the same guarantee one layer down, at the persisted bytes: earlier
/// lines are never rewritten (<c>SessionEventsFile_EarlierEventsSurviveByteForByteAfterALaterToggle</c>).
/// </para>
///
/// <para>Idiom matches <c>Health.HealthIncidentSidecar</c>: a single lock around read-modify-write,
/// plain <c>System.Text.Json</c>, tolerant JSONL reads.</para>
/// </remarks>
public sealed class SessionConfigStore
{
    private static readonly JsonSerializerOptions ConfigJsonOptions = new() { WriteIndented = true };

    private readonly Lock _gate = new();

    public SessionConfigStore(string workspaceRoot, string sessionId)
    {
        WorkspaceRoot = workspaceRoot;
        SessionId = sessionId;
    }

    public string WorkspaceRoot { get; }

    public string SessionId { get; }

    /// <summary>Creates the session: writes <c>session.json</c> and emits <c>session.open</c>.</summary>
    public SessionConfig Create(
        string name, string workspaceId, IReadOnlyList<string> enabledBackends, DateTimeOffset now)
    {
        lock (_gate)
        {
            var config = new SessionConfig(SessionId, name, workspaceId, now, [.. enabledBackends]);
            WriteConfigUnsafe(config);
            AppendEventUnsafe(SessionEventKinds.Open, config, now);
            return config;
        }
    }

    /// <summary>The current, live config — what a NEW run would pick up.</summary>
    public SessionConfig Load()
    {
        lock (_gate)
        {
            return ReadConfigUnsafe();
        }
    }

    /// <summary>
    /// Applies a backend toggle for new runs and emits <c>session.config</c>. Never mutates a
    /// <see cref="SessionConfig"/> a caller already holds — see the remarks on this type.
    /// </summary>
    public SessionConfig SetEnabledBackends(IReadOnlyList<string> enabledBackends, DateTimeOffset now)
    {
        lock (_gate)
        {
            var updated = ReadConfigUnsafe() with { EnabledBackends = [.. enabledBackends] };
            WriteConfigUnsafe(updated);
            AppendEventUnsafe(SessionEventKinds.Config, updated, now);
            return updated;
        }
    }

    /// <summary>Every event this session has ever emitted, in append order.</summary>
    public IReadOnlyList<SessionEvent> ReadEvents()
    {
        lock (_gate)
        {
            return ReadEventsUnsafe();
        }
    }

    private SessionConfig ReadConfigUnsafe()
    {
        var path = SessionPaths.SessionFile(WorkspaceRoot, SessionId);
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SessionConfig>(json)
            ?? throw new InvalidOperationException($"{path} deserialized to null");
    }

    private void WriteConfigUnsafe(SessionConfig config)
    {
        Directory.CreateDirectory(SessionPaths.SessionDirectory(WorkspaceRoot, SessionId));
        File.WriteAllText(
            SessionPaths.SessionFile(WorkspaceRoot, SessionId),
            JsonSerializer.Serialize(config, ConfigJsonOptions));
    }

    private void AppendEventUnsafe(string kind, SessionConfig config, DateTimeOffset now)
    {
        Directory.CreateDirectory(SessionPaths.SessionDirectory(WorkspaceRoot, SessionId));

        var nextSeq = ReadEventsUnsafe() is { Count: > 0 } existing ? existing[^1].Seq + 1 : 1;
        var body = new JsonObject
        {
            ["enabledBackends"] = new JsonArray([.. config.EnabledBackends.Select(b => JsonValue.Create(b))]),
        };
        var line = JsonSerializer.Serialize(new SessionEvent(nextSeq, now, kind, body));

        File.AppendAllText(SessionPaths.EventsFile(WorkspaceRoot, SessionId), line + Environment.NewLine);
    }

    private List<SessionEvent> ReadEventsUnsafe()
    {
        var path = SessionPaths.EventsFile(WorkspaceRoot, SessionId);
        if (!File.Exists(path))
        {
            return [];
        }

        var events = new List<SessionEvent>();
        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var evt = JsonSerializer.Deserialize<SessionEvent>(line);
            if (evt is not null)
            {
                events.Add(evt);
            }
        }

        return events;
    }
}
