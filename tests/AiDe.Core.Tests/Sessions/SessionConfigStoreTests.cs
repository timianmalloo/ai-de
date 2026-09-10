using AiDe.Core.Sessions;

namespace AiDe.Core.Tests.Sessions;

/// <summary>
/// F0 clauses 1-3 (docs/plans/conductor-front-door.md): <c>session.json</c> round-trips through
/// <see cref="System.Text.Json"/>, backend toggles apply to new runs only and never rewrite a
/// config already picked up, and nothing writes under the reserved <c>runs/</c> path.
/// </summary>
public sealed class SessionConfigStoreTests : IDisposable
{
    private readonly string _workspaceRoot;

    public SessionConfigStoreTests()
    {
        _workspaceRoot = Directory.CreateTempSubdirectory("aide-session-store-tests-").FullName;
    }

    public void Dispose() => Directory.Delete(_workspaceRoot, recursive: true);

    [Fact]
    public void Create_WritesSessionJsonAtTheContractPath()
    {
        var sessionId = SessionId.New();
        var store = new SessionConfigStore(_workspaceRoot, sessionId);
        var now = DateTimeOffset.UtcNow;

        store.Create("payments extraction", "workspace-1", ["claude-code"], now);

        var path = SessionPaths.SessionFile(_workspaceRoot, sessionId);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Create_ThenLoad_RoundTripsEveryField()
    {
        var sessionId = SessionId.New();
        var store = new SessionConfigStore(_workspaceRoot, sessionId);
        var now = DateTimeOffset.UtcNow;

        var created = store.Create("payments extraction", "workspace-1", ["claude-code", "codex"], now);
        var loaded = store.Load();

        Assert.Equal(sessionId, loaded.SessionId);
        Assert.Equal("payments extraction", loaded.Name);
        Assert.Equal("workspace-1", loaded.WorkspaceId);
        Assert.Equal(created.CreatedAt, loaded.CreatedAt);
        Assert.Equal(created.EnabledBackends, loaded.EnabledBackends);
    }

    [Fact]
    public void Load_SurvivesAFreshStoreInstance_ProvingItIsReallyPersisted()
    {
        var sessionId = SessionId.New();
        var writer = new SessionConfigStore(_workspaceRoot, sessionId);
        writer.Create("payments extraction", "workspace-1", ["claude-code"], DateTimeOffset.UtcNow);

        var reader = new SessionConfigStore(_workspaceRoot, sessionId);
        var loaded = reader.Load();

        Assert.Equal(["claude-code"], loaded.EnabledBackends);
    }

    /// <summary>
    /// Clause 3's oracle: a toggle applies to NEW runs only. A run captures the config in effect at
    /// its own start; because <see cref="SessionConfig"/> is an immutable record and every mutation
    /// through the store produces a fresh instance, a snapshot a run already captured must never
    /// change under it when a later toggle lands.
    /// </summary>
    [Fact]
    public void SetEnabledBackends_NeverMutatesAConfigARunAlreadyCaptured()
    {
        var sessionId = SessionId.New();
        var store = new SessionConfigStore(_workspaceRoot, sessionId);
        store.Create("payments extraction", "workspace-1", ["claude-code", "codex"], DateTimeOffset.UtcNow);

        // "run 1" captures the config in effect now.
        var runOneConfig = store.Load();
        Assert.Equal(["claude-code", "codex"], runOneConfig.EnabledBackends);

        // The operator disables codex mid-session.
        store.SetEnabledBackends(["claude-code"], DateTimeOffset.UtcNow);

        // run 1's already-captured snapshot must read exactly as it did when captured.
        Assert.Equal(["claude-code", "codex"], runOneConfig.EnabledBackends);

        // A NEW run ("run 2") captures the toggle.
        var runTwoConfig = store.Load();
        Assert.Equal(["claude-code"], runTwoConfig.EnabledBackends);
    }

    [Fact]
    public void SetEnabledBackends_EmitsASessionConfigEvent()
    {
        var sessionId = SessionId.New();
        var store = new SessionConfigStore(_workspaceRoot, sessionId);
        store.Create("payments extraction", "workspace-1", ["claude-code"], DateTimeOffset.UtcNow);

        store.SetEnabledBackends(["claude-code", "codex"], DateTimeOffset.UtcNow);

        var events = store.ReadEvents();
        Assert.Equal(2, events.Count); // session.open from Create, session.config from the toggle
        Assert.Equal(SessionEventKinds.Open, events[0].Kind);
        Assert.Equal(SessionEventKinds.Config, events[1].Kind);
    }

    /// <summary>
    /// The append-only half of clause 3's oracle, at the persisted-event layer rather than the
    /// in-memory-record layer: an event already written to <c>session-events.jsonl</c> is never
    /// rewritten by a later toggle.
    /// </summary>
    [Fact]
    public void SessionEventsFile_EarlierEventsSurviveByteForByteAfterALaterToggle()
    {
        var sessionId = SessionId.New();
        var store = new SessionConfigStore(_workspaceRoot, sessionId);
        store.Create("payments extraction", "workspace-1", ["claude-code"], DateTimeOffset.UtcNow);

        var eventsPath = SessionPaths.EventsFile(_workspaceRoot, sessionId);
        var firstLineAfterCreate = File.ReadAllLines(eventsPath)[0];

        store.SetEnabledBackends(["claude-code", "codex"], DateTimeOffset.UtcNow);
        store.SetEnabledBackends(["codex"], DateTimeOffset.UtcNow);

        var firstLineAfterTwoToggles = File.ReadAllLines(eventsPath)[0];
        Assert.Equal(firstLineAfterCreate, firstLineAfterTwoToggles);
    }

    /// <summary>
    /// Fails if: anything writes a run log anywhere. Dynamic half — exercises the store's full
    /// lifecycle (create, two toggles, reads) and asserts the reserved subtree never came into
    /// existence. <c>RunLogStore</c> is Phase 3 and this node must not build it.
    /// </summary>
    [Fact]
    public void Lifecycle_NeverWritesUnderTheReservedRunsDirectory()
    {
        var sessionId = SessionId.New();
        var store = new SessionConfigStore(_workspaceRoot, sessionId);
        store.Create("payments extraction", "workspace-1", ["claude-code"], DateTimeOffset.UtcNow);
        store.SetEnabledBackends(["claude-code", "codex"], DateTimeOffset.UtcNow);
        store.SetEnabledBackends(["codex"], DateTimeOffset.UtcNow);
        _ = store.Load();
        _ = store.ReadEvents();

        var runsDirectory = SessionPaths.RunsDirectory(_workspaceRoot, sessionId);
        var reservedRunLog = SessionPaths.RunLogFile(_workspaceRoot, sessionId, "run-1");

        Assert.False(Directory.Exists(runsDirectory), $"{runsDirectory} must not exist — RunLogStore is Phase 3");
        Assert.False(File.Exists(reservedRunLog), $"{reservedRunLog} must not exist — RunLogStore is Phase 3");
    }
}
