using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using AiDe.Core.Watcher;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace AiDe.Core.Tests.Watcher;

/// <summary>
/// P2 native-notice diagnostics against the legacy API, intentionally RED and unshippable.
/// These are not an unqualified compatibility promise for a future enhanced admission API.
/// </summary>
public sealed class RegistrationNoticeReliabilityTests(ITestOutputHelper output)
{
    private const int OutstandingLimit = 128;

    [Fact]
    public void DrainRegistrationNotices_PublicationFails_RetryRetainsOriginalCorrection()
    {
        using var fixture = new NoticeFixture();
        var registered = fixture.First.Register(fixture.Registration("terminal-1"));
        fixture.FirstRegistrar.End(registered.SessionId, registered.Capability);
        fixture.Clock.Advance(TimeSpan.FromSeconds(17));
        var original = Assert.Single(Pending(fixture.First));
        AssertCorrection(fixture, original, registered);
        var before = fixture.NativeState();
        var delivery = Assert.Single(fixture.First.DrainRegistrationNotices());
        Assert.Equal(original, delivery);
        var destination = Path.Combine(fixture.Output, RegistrationPublisher.DirectoryName,
            StandingPublisher.FileNameFor(original.SessionId));
        Directory.CreateDirectory(destination);

        // The checked writer cleans its unique temporary file; legacy Drain still loses the notice.
        var failure = Record.Exception(() => RegistrationPublisher.Publish(fixture.Output, delivery));

        Assert.Equal("COORD_NOTICE_IO", Assert.IsType<WatcherException>(failure).Code);
        Assert.Empty(Directory.GetFiles(fixture.Output, "*.tmp", SearchOption.AllDirectories));
        Assert.Equal(before, fixture.NativeState());
        Directory.Delete(destination);
        var retry = fixture.First.DrainRegistrationNotices();
        output.WriteLine(JsonSerializer.Serialize(new
        {
            scenario = "N1", failure = failure!.GetType().Name,
            accepted = 1, original, retryCount = retry.Count,
            nativeStateUnchanged = before == fixture.NativeState(),
            temporaryFilePreserved = File.Exists(destination + ".tmp"),
        }));
        Assert.Equal(before, fixture.NativeState());
        Assert.True(fixture.FirstRegistrar.Verify(registered.SessionId, registered.Capability));
        var retried = Assert.Single(retry);
        Assert.Equal(original, retried);
        AssertPublished(original, RegistrationPublisher.Publish(fixture.Output, retried));
        Assert.Equal(before, fixture.NativeState());
        Assert.Empty(Pending(fixture.First));
    }

    [Fact]
    public void Register_TwoHostsHave128OutstandingCorrections_RefusesBeforeNativeMutation()
    {
        using var fixture = new NoticeFixture();
        for (var index = 0; index < OutstandingLimit; index++)
        {
            var host = index % 2 == 0 ? fixture.First : fixture.Second;
            host.Register(fixture.Registration($"terminal-{index}"));
        }
        Assert.Equal(OutstandingLimit / 2, Pending(fixture.First).Length);
        Assert.Equal(OutstandingLimit / 2, Pending(fixture.Second).Length);
        Assert.Equal(OutstandingLimit, fixture.FirstStore.AllSessions().Count);
        Assert.Equal(OutstandingLimit, fixture.SecondStore.AllSessions().Count);
        var originals = Pending(fixture.First).Concat(Pending(fixture.Second)).ToArray();
        Assert.All(originals, notice =>
        {
            Assert.Equal(fixture.Sent, notice.RepositorySent);
            Assert.Equal(fixture.Used, notice.RepositoryUsed);
            Assert.Contains("linked worktree", notice.Reason, StringComparison.Ordinal);
        });
        var before = fixture.NativeState();
        fixture.Clock.Advance(TimeSpan.FromSeconds(31));
        RegisteredSession? admitted = null;

        var refusal = Record.Exception(() =>
            admitted = fixture.Second.Register(fixture.Registration("terminal-overflow")));

        var after = fixture.NativeState();
        var outstanding = Pending(fixture.First).Length + Pending(fixture.Second).Length;
        output.WriteLine(JsonSerializer.Serialize(new
        {
            scenario = "N2", before, after, outstanding,
            admitted = admitted is not null, refusal = refusal?.GetType().Name,
        }));
        Assert.Equal(before, after);
        Assert.Equal(OutstandingLimit, outstanding);
        Assert.Equal(originals, Pending(fixture.First).Concat(Pending(fixture.Second)).ToArray());
        Assert.Null(admitted);
    }

    [Fact]
    public void Publish_CorrectedRegistrationAndIdenticalRetry_PreservesLegacyDocumentBytes()
    {
        using var fixture = new NoticeFixture();
        var registered = fixture.First.Register(fixture.Registration("ordinary"));
        var original = Assert.Single(Pending(fixture.First));
        AssertCorrection(fixture, original, registered);
        var before = fixture.NativeState();

        var delivered = Assert.Single(fixture.First.DrainRegistrationNotices());
        var path = RegistrationPublisher.Publish(fixture.Output, delivered);
        AssertPublished(original, path);
        var hash = SHA256.HashData(File.ReadAllBytes(path));
        var retryPath = RegistrationPublisher.Publish(fixture.Output, delivered);

        Assert.Equal(path, retryPath);
        Assert.Equal(hash, SHA256.HashData(File.ReadAllBytes(retryPath)));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.json"));
        Assert.Empty(Directory.GetFiles(fixture.Output, "*.jsonl"));
        Assert.Empty(Pending(fixture.First));
        Assert.Equal(before, fixture.NativeState());
        output.WriteLine("POSITIVE: real publication, original fields, identical retry bytes, native state unchanged.");
    }

    [Fact]
    public void Register_RepositoryAlreadyCanonical_ProducesNoCorrectionNotice()
    {
        using var fixture = new NoticeFixture();

        var registered = fixture.First.Register(fixture.Registration("canonical", fixture.Repository));

        Assert.Equal(fixture.Used, registered.Binding.Repository.CanonicalPath);
        Assert.Single(fixture.FirstStore.AllSessions());
        Assert.Empty(Pending(fixture.First));
        Assert.Empty(fixture.First.DrainRegistrationNotices());
        Assert.False(Directory.Exists(Path.Combine(fixture.Output, RegistrationPublisher.DirectoryName)));
        Assert.True(fixture.FirstRegistrar.Verify(registered.SessionId, registered.Capability));
        output.WriteLine("POSITIVE: canonical registration accepted with zero notices.");
    }

    [Fact]
    public void RepositoryFor_SyntheticKnownRoots_MatchesRealFilesystemLocator()
    {
        using var fixture = new NoticeFixture();
        var real = new FileSystemRepositoryLocator();

        Assert.Equal(fixture.Used, new RepositoryIdentity(real.RepositoryFor(fixture.Worktree)!, "test").CanonicalPath);
        Assert.Equal(fixture.Repository, fixture.Locator.RepositoryFor(fixture.Sent));
        Assert.Null(real.RepositoryFor(fixture.Repository));
        Assert.Null(fixture.Locator.RepositoryFor(fixture.Used));
        Assert.Null(real.RepositoryFor(Path.Combine(fixture.Root, "unknown")));
        Assert.Null(fixture.Locator.RepositoryFor(Path.Combine(fixture.Root, "unknown")));
        output.WriteLine("POSITIVE: substituted known-root locator agrees with real locator on owned synthetic files.");
    }

    private static void AssertCorrection(NoticeFixture fixture, RegistrationNotice notice, RegisteredSession session)
    {
        Assert.Equal(session.SessionId, notice.SessionId);
        Assert.Equal(fixture.Sent, notice.RepositorySent);
        Assert.Equal(fixture.Used, notice.RepositoryUsed);
        Assert.Contains("linked worktree", notice.Reason, StringComparison.Ordinal);
    }

    private static void AssertPublished(RegistrationNotice original, string path)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        var root = json.RootElement;
        Assert.Equal(original.SessionId, root.GetProperty("sessionId").GetString());
        Assert.Equal(original.RepositorySent, root.GetProperty("repositorySent").GetString());
        Assert.Equal(original.RepositoryUsed, root.GetProperty("repositoryUsed").GetString());
        Assert.Equal(original.Reason, root.GetProperty("reason").GetString());
        Assert.Equal(RegistrationPublisher.GeneratedBy, root.GetProperty(RegistrationPublisher.GeneratedByField).GetString());
        Assert.Equal(StandingPublisher.FileNameFor(original.SessionId), Path.GetFileName(path));
    }

    // Legacy has no non-destructive public notice/capability census. Read only these exact fields;
    // never drain for quota measurement. A changed implementation must deliberately revise this probe.
    private static RegistrationNotice[] Pending(IngestHost host) =>
        ((ConcurrentQueue<RegistrationNotice>)typeof(IngestHost)
            .GetField("_notices", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(host)!).ToArray();

    private static int CapabilityCount(TrustedRegistrar registrar) =>
        ((IDictionary)typeof(TrustedRegistrar)
            .GetField("_capabilityBySession", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(registrar)!).Count;

    private sealed class KnownRoots(string worktree, string repository) : IRepositoryLocator
    {
        public string? RepositoryFor(string checkoutPath) =>
            new RepositoryIdentity(checkoutPath, "test").CanonicalPath ==
            new RepositoryIdentity(worktree, "test").CanonicalPath ? repository : null;
    }

    private sealed class NoticeFixture : IDisposable
    {
        private int _firstIds;
        private int _secondIds;
        public string Root { get; }
        public string Repository { get; }
        public string Worktree { get; }
        public string Output { get; }
        public string Sent => new RepositoryIdentity(Worktree, "test").CanonicalPath;
        public string Used => new RepositoryIdentity(Repository, "test").CanonicalPath;
        public FakeMonotonicClock Clock { get; } = new();
        public KnownRoots Locator { get; }
        public SqliteWatcherObservationStore FirstStore { get; }
        public SqliteWatcherObservationStore SecondStore { get; }
        public TrustedRegistrar FirstRegistrar { get; }
        private TrustedRegistrar SecondRegistrar { get; }
        public IngestHost First { get; }
        public IngestHost Second { get; }

        public NoticeFixture()
        {
            var project = new DirectoryInfo(AppContext.BaseDirectory);
            while (!File.Exists(Path.Combine(project.FullName, "AiDe.Core.Tests.csproj")))
                project = project.Parent ?? throw new InvalidOperationException("Test project root not found.");
            Root = Path.Combine(project.FullName, "obj", "notice-fixtures", Guid.NewGuid().ToString("N"));
            Repository = Path.Combine(Root, "repository");
            Worktree = Path.Combine(Root, "linked");
            Output = Path.Combine(Root, "contract-log");
            Directory.CreateDirectory(Worktree);
            Directory.CreateDirectory(Output);
            var git = Path.Combine(Repository, ".git", "worktrees", "linked");
            Directory.CreateDirectory(git);
            File.WriteAllText(Path.Combine(Worktree, ".git"), $"gitdir: {git}");
            Locator = new KnownRoots(Worktree, Repository);
            FirstStore = SqliteWatcherObservationStore.Open(Path.Combine(Root, "watcher.db"));
            SecondStore = SqliteWatcherObservationStore.Open(Path.Combine(Root, "watcher.db"));
            FirstRegistrar = new TrustedRegistrar(FirstStore, new SequentialCapabilityFactory(),
                Clock, () => $"notice-first-{++_firstIds}");
            SecondRegistrar = new TrustedRegistrar(SecondStore, new SequentialCapabilityFactory(),
                Clock, () => $"notice-second-{++_secondIds}");
            var time = new FixedTimeProvider(DateTimeOffset.UnixEpoch);
            First = new IngestHost(FirstStore, FirstRegistrar, time, locator: Locator);
            Second = new IngestHost(SecondStore, SecondRegistrar, time, locator: Locator);
        }

        public HarnessRegistration Registration(string terminal, string? repository = null) =>
            new(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [OtelAttributes.RepoPath] = repository ?? Worktree,
                [OtelAttributes.RepoDisplay] = "synthetic-notice-fixture",
                [OtelAttributes.WorktreePath] = Worktree,
                [OtelAttributes.WorktreeBranch] = "fixture",
                [OtelAttributes.TerminalId] = terminal,
                [OtelAttributes.AgentName] = "synthetic-agent",
                [OtelAttributes.ServiceName] = "github-copilot",
            });

        public string NativeState()
        {
            using var raw = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = FirstStore.DatabasePath, Mode = SqliteOpenMode.ReadOnly, Pooling = false,
            }.ToString());
            raw.Open();
            using var count = raw.CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM agent_session_dim";
            return JsonSerializer.Serialize(new
            {
                sessionRows = Convert.ToInt64(count.ExecuteScalar()),
                firstCapabilities = CapabilityCount(FirstRegistrar),
                secondCapabilities = CapabilityCount(SecondRegistrar),
                sessions = FirstStore.AllSessions().OrderBy(session => session.SessionId, StringComparer.Ordinal)
                    .Select(session => new
                    {
                        session.SessionId, generation = session.Generation.Value,
                        heartbeat = FirstStore.LastHeartbeat(session.SessionId),
                        ended = FirstStore.IsEnded(session.SessionId),
                    }),
            });
        }

        public void Dispose()
        {
            SecondStore.Dispose();
            FirstStore.Dispose();
            Directory.Delete(Root, recursive: true);
        }
    }
}
