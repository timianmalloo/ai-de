using System.Diagnostics;
using System.Text.Json;
using AiDe.Core.Watcher;
using Microsoft.Data.Sqlite;
using System.Collections;
using System.Reflection;

namespace AiDe.Core.Tests.Watcher;

[Trait("Platform", "Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class NativeNoticePublicationTests
{
    [Fact]
    public void Admit_ExistingLegacyOwnerMismatch_RefusesBeforeAdmission()
    {
        using var fixture = new Fixture(() => "a-b");
        var path = RegistrationPublisher.Publish(fixture.Output, new("a:b", "sent", "used", "synthetic"));
        var original = File.ReadAllBytes(path);
        var before = fixture.NativeState();

        Assert.Equal("COORD_NOTICE_CONFLICT", Assert.Throws<WatcherException>(() => fixture.Admit("first")).Code);
        Assert.Equal(before, fixture.NativeState());
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Single(Directory.GetFiles(fixture.Output, "*.json", SearchOption.AllDirectories));
    }

    [Fact]
    public void PublishLegacy_MissingOrdinaryRoot_CreatesPinnedPathAndPreservesJson()
    {
        using var fixture = new Fixture();
        var root = Path.Combine(fixture.Output, "new", "root");
        var notice = new RegistrationNotice("safe-session", "sent", "used", "synthetic");

        var path = RegistrationPublisher.Publish(root, notice);

        Assert.Equal(Path.Combine(root, "registration", "safe-session.json"), path);
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        Assert.Equal("safe-session", document.RootElement.GetProperty("sessionId").GetString());
        Assert.Equal("synthetic", document.RootElement.GetProperty("reason").GetString());
        Assert.Empty(Directory.GetFiles(fixture.Output, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public void Admit_AmbiguousCompatibilityIds_RefusesBothBeforeAdmissionOrFiles()
    {
        var ids = new Queue<string>(["a:b", "a?b"]);
        using var fixture = new Fixture(() => ids.Dequeue());
        var before = fixture.NativeState();
        var errors = new List<Exception?>();
        foreach (var id in new[] { "a:b", "a?b" })
            errors.Add(Record.Exception(() =>
            {
                fixture.Admit(id, id);
                fixture.Run();
            }));

        Assert.True(errors.All(error => error is WatcherException { Code: "COORD_NATIVE_CONTEXT" }),
            $"Both IDs must refuse; errors={string.Join(",", errors.Select(error => error?.Message ?? "SUCCESS"))}; " +
            $"files={string.Join(",", Directory.GetFiles(fixture.Output, "*.json", SearchOption.AllDirectories).Select(File.ReadAllText))}");
        Assert.Equal(before, fixture.NativeState());
        Assert.Empty(Directory.GetFileSystemEntries(fixture.Output));
    }

    [Fact]
    public void PublishLegacy_AmbiguousExistingOwner_RefusesWithoutOverwrite()
    {
        using var fixture = new Fixture();
        var path = RegistrationPublisher.Publish(fixture.Output, new("a:b", "sent", "used", "synthetic"));
        var original = File.ReadAllBytes(path);
        var error = Assert.Throws<WatcherException>(() =>
            RegistrationPublisher.Publish(fixture.Output, new("a?b", "sent", "used", "synthetic")));

        Assert.Equal("COORD_NOTICE_CONFLICT", error.Code);
        Assert.Equal(error.Code, error.Message);
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Empty(Directory.GetFiles(fixture.Output, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public void PublishLegacy_RegistrationJunction_RefusesWithoutSiblingFiles()
    {
        using var fixture = new Fixture();
        var sibling = Path.Combine(fixture.Root, "sibling");
        Directory.CreateDirectory(sibling);
        var junction = Path.Combine(fixture.Output, "registration");
        var start = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
        };
        foreach (var argument in new[] { "/c", "mklink", "/J", junction, sibling }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        process.WaitForExit();
        Assert.Equal(0, process.ExitCode);
        try
        {
            var error = Record.Exception(() =>
                RegistrationPublisher.Publish(fixture.Output, new("safe-session", "sent", "used", "synthetic")));
            Assert.True(error is WatcherException { Code: "COORD_NATIVE_CONTEXT" },
                $"Expected context refusal; error={error?.Message ?? "SUCCESS"}; siblingFiles={Directory.GetFiles(sibling).Length}");
            Assert.Equal(error!.Message, ((WatcherException)error).Code);
            Assert.Empty(Directory.GetFileSystemEntries(sibling));
        }
        finally { Directory.Delete(junction); }
    }

    [Fact]
    public void Complete_WrongVersionSameOwnerAndAttempt_RetainsInFlight()
    {
        using var fixture = new Fixture();
        fixture.Admit("first");
        var claimed = fixture.Store.ClaimNativeNotice(fixture.Notice().NoticeId, "same-owner")!;

        Assert.False(fixture.Store.CompleteNativeNotice(claimed with { Version = claimed.Version + 1 }, 0));
        Assert.Equal(1, fixture.Store.PendingNativeNoticeCount());
        Assert.True(fixture.Store.RequeueNativeNotice(claimed, 0));
        Assert.Equal(1, fixture.Run().Published);
    }

    [Fact]
    public void Admit_RetainedAccountingReaderFails_UncertainPreservesOwnerAndCapacity()
    {
        using var first = new Fixture();
        using var second = new Fixture();
        for (var index = 0; index < 128; index++) first.Admit($"op-{index}", $"terminal-{index}");
        first.AdmissionRoot.Dispose();
        var enrollments = (IDictionary)typeof(NoticeAdmissionCoordinator)
            .GetField("Enrollments", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var readers = enrollments.Values.Cast<object>().Select(value =>
            (SqliteWatcherObservationStore)value.GetType().GetProperty("Reader", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(value)!);
        var reader = readers.Single(value => value.DatabasePath == first.Database);
        var connection = (SqliteConnection)typeof(SqliteWatcherObservationStore)
            .GetField("_connection", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(reader)!;
        Assert.NotSame(first.Store, reader);
        connection.Close();
        try
        {
            Assert.Equal("COORD_NOTICE_UNCERTAIN", Assert.Throws<WatcherException>(() => second.Admit("overflow")).Code);
            Assert.Equal(128, first.Store.PendingNativeNoticeCount());
            Assert.Equal(0L, second.Scalar("SELECT count(*) FROM native_registration_admission_fact"));
            using var ownerProbe = new FileStream(first.Database, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            Assert.Throws<IOException>(() => ownerProbe.Lock(long.MaxValue - 1, 1));
        }
        finally { connection.Open(); }

        Assert.Equal("COORD_NOTICE_CAPACITY", Assert.Throws<WatcherException>(() => second.Admit("overflow")).Code);
        Assert.Equal(1, first.Run(1).Published);
        Assert.False(second.Admit("overflow").Replayed);
        while (first.Store.PendingNativeNoticeCount() > 0) Assert.True(first.Run(32).Published > 0);
        Assert.Equal(1, second.Run().Published);
        using var released = new FileStream(first.Database, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        released.Lock(long.MaxValue - 1, 1);
        released.Unlock(long.MaxValue - 1, 1);
    }

    [Fact]
    public void Publish_DeniedThenRetried_PreservesFrozenAdmissionAndBytes()
    {
        using var fixture = new Fixture();
        var admission = fixture.Admit("first");
        var original = fixture.Notice();
        var before = fixture.NativeState();
        var target = fixture.Immutable(original);
        Directory.CreateDirectory(target);

        var failed = fixture.Run();

        Assert.Equal(0, failed.Published);
        Assert.Equal(1, fixture.Store.PendingNativeNoticeCount());
        Assert.Equal(before, fixture.NativeState());
        Directory.Delete(target);
        Assert.Equal(1, fixture.Run().Published);
        Assert.Equal(original.Bytes, File.ReadAllBytes(target));
        Assert.Equal(original.NoticeId, fixture.Notice().NoticeId);
        Assert.Equal(before, fixture.NativeState());
        Assert.Equal(1, admission.Admission.Session.Generation.Value);
        Assert.Empty(Directory.GetFiles(fixture.Output, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public void Publish_SameIdDifferentBytes_ConflictsWithoutOverwrite()
    {
        using var fixture = new Fixture();
        fixture.Admit("first");
        var notice = fixture.Notice();
        Directory.CreateDirectory(Path.GetDirectoryName(fixture.Immutable(notice))!);
        var conflicting = notice.Bytes.ToArray();
        conflicting[0] = (byte)'[';
        File.WriteAllBytes(fixture.Immutable(notice), conflicting);

        var result = fixture.Run();

        Assert.Equal("COORD_NOTICE_CONFLICT", result.ErrorCode);
        Assert.Equal(conflicting, File.ReadAllBytes(fixture.Immutable(notice)));
        Assert.Equal(notice.Bytes, fixture.Notice().Bytes);
        Assert.Equal(1, fixture.Store.PendingNativeNoticeCount());
    }

    [Fact]
    public void Publish_FileSucceededDatabaseAckFailed_ReopensAndReconciles()
    {
        using var fixture = new Fixture();
        fixture.Admit("first");
        var notice = fixture.Notice();
        var before = fixture.NativeState();
        fixture.Sql("""
            CREATE TRIGGER synthetic_ack_failure BEFORE UPDATE ON registration_notice_delivery
            WHEN NEW.state='Published' BEGIN SELECT RAISE(ABORT,'synthetic'); END;
            """);

        var failed = fixture.Run();

        Assert.Equal("COORD_NOTICE_UNCERTAIN", failed.ErrorCode);
        Assert.Equal(notice.Bytes, File.ReadAllBytes(fixture.Immutable(notice)));
        Assert.Equal(1, fixture.Store.PendingNativeNoticeCount());
        fixture.Sql("DROP TRIGGER synthetic_ack_failure;");
        fixture.AdmissionRoot.Dispose();
        fixture.Store.Dispose();
        using var reopened = SqliteWatcherObservationStore.Open(fixture.Database);
        var result = new NativeNoticeWorker(reopened).Run(16, 1);
        Assert.Equal(1, result.Published);
        Assert.Equal(0, reopened.PendingNativeNoticeCount());
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(fixture.Immutable(notice))!, "*.json"));
        Assert.Equal(before, fixture.NativeState());
        Assert.Equal(notice.Bytes, File.ReadAllBytes(fixture.Immutable(notice)));
    }

    [Fact]
    public void Publish_FullGlobalQuota_ActualPublicationReleasesCapacity()
    {
        using var first = new Fixture();
        using var second = new Fixture();
        for (var i = 0; i < 128; i++) first.Admit($"op-{i}", $"terminal-{i}");
        first.AdmissionRoot.Dispose();
        Assert.Equal("COORD_NOTICE_CAPACITY",
            Assert.Throws<WatcherException>(() => second.Admit("overflow")).Code);

        Assert.Equal(1, first.Run(1).Published);
        var admitted = second.Admit("overflow");

        Assert.False(admitted.Replayed);
        Assert.Equal(128L, first.Scalar("SELECT count(*) FROM native_registration_admission_fact"));
        Assert.Equal(127, first.Store.PendingNativeNoticeCount());
        Assert.Equal(1, second.Store.PendingNativeNoticeCount());
        while (first.Store.PendingNativeNoticeCount() > 0) Assert.True(first.Run(32).Published > 0);
        using var ownerProbe = new FileStream(first.Database, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        ownerProbe.Lock(long.MaxValue - 1, 1);
        ownerProbe.Unlock(long.MaxValue - 1, 1);
    }

    [Fact]
    public void Publish_OlderRetryAfterNewer_DerivedLatestDoesNotRegress()
    {
        using var fixture = new Fixture();
        fixture.Admit("older");
        var older = fixture.Notice();
        Directory.CreateDirectory(fixture.Immutable(older));
        fixture.Admit("newer");
        Assert.Equal(1, fixture.Run().Published);
        var latest = File.ReadAllBytes(fixture.Latest(older.SessionId));
        Directory.Delete(fixture.Immutable(older));

        Assert.Equal(1, fixture.Run().Published);

        Assert.Equal(latest, File.ReadAllBytes(fixture.Latest(older.SessionId)));
        Assert.Equal(older.Bytes, File.ReadAllBytes(fixture.Immutable(older)));
        Assert.Equal(2, Directory.GetFiles(Path.GetDirectoryName(fixture.Immutable(older))!, "*.json").Length);
    }

    [Fact]
    public async Task Publish_ConcurrentIdenticalAttempts_OneImmutableFileNoSharedTemps()
    {
        using var fixture = new Fixture();
        fixture.Admit("first");
        var notice = fixture.Notice();
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            RegistrationPublisher.PublishNative(notice, notice.Bytes))));

        Assert.Equal(notice.Bytes, File.ReadAllBytes(fixture.Immutable(notice)));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(fixture.Immutable(notice))!, "*.json"));
        Assert.Empty(Directory.GetFiles(fixture.Output, "*.tmp", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData(@"\\server\private")]
    [InlineData(@"\\?\C:\private")]
    [InlineData(@"C:\private\..\escape")]
    [InlineData(@"C:\private\.git\escape")]
    [InlineData(@"C:\private\CON")]
    public void Publish_InvalidRoot_RejectsBeforeFilesWithoutPrivateErrors(string root)
    {
        using var fixture = new Fixture();
        fixture.Admit("first");
        var notice = fixture.Notice() with { Root = root };

        var failure = Assert.Throws<WatcherException>(() => RegistrationPublisher.PublishNative(notice, notice.Bytes));

        Assert.Equal("COORD_NATIVE_CONTEXT", failure.Code);
        Assert.Equal(failure.Code, failure.Message);
        Assert.Empty(Directory.GetFileSystemEntries(fixture.Output));
    }

    [Fact]
    public void Publish_CancelledAfterAdmission_RetainsObligationAndEmitsPrivateSafeCounts()
    {
        using var fixture = new Fixture();
        fixture.Admit("first");
        var before = fixture.NativeState();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "AiDe.NativeNotice",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = spans.Add,
        };
        ActivitySource.AddActivityListener(listener);

        var cancelled = new NativeNoticeWorker(fixture.Store).Run(16, 0, cancellation.Token);

        Assert.Equal(0, cancelled.Published);
        Assert.Equal("COORD_NOTICE_CANCELLED", cancelled.ErrorCode);
        Assert.Equal(1, fixture.Store.PendingNativeNoticeCount());
        Assert.Equal(before, fixture.NativeState());
        Assert.Equal(1, fixture.Run().Published);
        Assert.Contains(spans, span => span.GetTagItem("native.notice.published")?.ToString() == "1");
        Assert.DoesNotContain("PRIVATE_MARKER", JsonSerializer.Serialize(spans.Select(span => span.TagObjects)));
    }

    [Fact]
    public void Publish_PoisonFirstCandidate_FairBoundedBatchReachesNext()
    {
        using var fixture = new Fixture();
        fixture.Admit("first", "one");
        var poison = fixture.Notice();
        Directory.CreateDirectory(fixture.Immutable(poison));
        Assert.Equal(0, fixture.Run(1).Published);
        fixture.Admit("second", "two");

        var next = fixture.Run(1);

        Assert.Equal(1, next.Attempted);
        Assert.Equal(1, next.Published);
        Assert.Equal(1, fixture.Store.PendingNativeNoticeCount());
    }

    [Fact]
    public void Complete_WrongOwnerOrStaleVersion_CannotChangeNewAttempt()
    {
        using var fixture = new Fixture();
        fixture.Admit("first");
        var original = fixture.Notice();
        var first = fixture.Store.ClaimNativeNotice(original.NoticeId, "first-owner")!;
        Assert.False(fixture.Store.CompleteNativeNotice(first with { Owner = "wrong-owner" }, 0));
        Assert.True(fixture.Store.RequeueNativeNotice(first, 0));
        var second = fixture.Store.ClaimNativeNotice(original.NoticeId, "second-owner")!;

        Assert.False(fixture.Store.CompleteNativeNotice(first, 0));
        Assert.False(fixture.Store.RequeueNativeNotice(first, 0));
        Assert.Equal(first.Attempt + 1, second.Attempt);
        Assert.Equal(1, fixture.Store.PendingNativeNoticeCount());
        Assert.True(fixture.Store.RequeueNativeNotice(second, 0));
        Assert.Equal(1, fixture.Run().Published);
    }

    [Fact]
    public void Publish_NewAcceptedPendingNotice_LatestSurvivesOlderAttempt()
    {
        using var fixture = new Fixture();
        fixture.Admit("older");
        var older = fixture.Notice();
        fixture.Admit("newer");
        var newerBytes = fixture.Bytes("SELECT publication_bytes FROM registration_notice_delivery WHERE operation_id='newer'");
        Directory.CreateDirectory(fixture.Immutable(older));
        fixture.Sql("""
            CREATE TRIGGER synthetic_ack_failure BEFORE UPDATE ON registration_notice_delivery
            WHEN NEW.state='Published' BEGIN SELECT RAISE(ABORT,'synthetic'); END;
            """);
        Assert.Equal(0, fixture.Run().Published);
        Assert.Equal(newerBytes, File.ReadAllBytes(fixture.Latest(older.SessionId)));
        Directory.Delete(fixture.Immutable(older));

        RegistrationPublisher.PublishNative(older, fixture.Store.LatestNativeNoticeBytes(older));

        Assert.Equal(newerBytes, File.ReadAllBytes(fixture.Latest(older.SessionId)));
        Assert.Equal(2, fixture.Store.PendingNativeNoticeCount());
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("fffffffffffffffffffffffffffffffZ")]
    public void Publish_InvalidId_RejectsBeforeDirectoryCreation(string id)
    {
        using var fixture = new Fixture();
        fixture.Admit("first");
        var notice = fixture.Notice() with { NoticeId = id };
        Assert.Equal("COORD_NATIVE_INTEGRITY",
            Assert.Throws<WatcherException>(() => RegistrationPublisher.PublishNative(notice, notice.Bytes)).Code);
        Assert.Empty(Directory.GetFileSystemEntries(fixture.Output));
    }

    [Fact]
    public void Publish_DefaultComposition_IsUnavailableNotEmpty()
    {
        using var fixture = new Fixture();
        Assert.Equal("COORD_NATIVE_UNAVAILABLE",
            Assert.Throws<WatcherException>(() => fixture.Host.PublishNativeNotices()).Code);
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("LPT1")]
    public void Publish_ReservedLegacyFilename_RejectsBeforeDirectoryCreation(string session)
    {
        using var fixture = new Fixture();
        fixture.Admit("first");
        var notice = fixture.Notice() with { SessionId = session };
        Assert.Equal("COORD_NATIVE_CONTEXT",
            Assert.Throws<WatcherException>(() => RegistrationPublisher.PublishNative(notice, notice.Bytes)).Code);
        Assert.Empty(Directory.GetFileSystemEntries(fixture.Output));
    }

    [Fact]
    public void Publish_StoreReadFails_IsUncertainAndRetainsPending()
    {
        using var fixture = new Fixture();
        fixture.Admit("first");
        fixture.Store.Dispose();

        var result = new NativeNoticeWorker(fixture.Store).Run(16, 0);

        Assert.Equal("COORD_NOTICE_UNCERTAIN", result.ErrorCode);
        Assert.Equal(1L, fixture.Scalar("SELECT count(*) FROM registration_notice_delivery WHERE state='Pending'"));
    }

    private sealed class Fixture : IDisposable
    {
        private int _session;
        internal string Root { get; } = Path.Combine(AppContext.BaseDirectory, "n1-fixtures", Guid.NewGuid().ToString("N"));
        internal string Database => Path.Combine(Root, "watcher.db");
        internal string Output => Path.Combine(Root, "output");
        internal string Worktree => Path.Combine(Root, "PRIVATE_MARKER");
        internal SqliteWatcherObservationStore Store { get; }
        internal NativeRegistrationAdmissionRoot AdmissionRoot { get; }
        internal IngestHost Host { get; }
        internal Fixture(Func<string>? sessionIds = null)
        {
            Directory.CreateDirectory(Output);
            Directory.CreateDirectory(Worktree);
            Store = SqliteWatcherObservationStore.Open(Database);
            AdmissionRoot = new(Store);
            var registrar = new TrustedRegistrar(Store, new SequentialCapabilityFactory(),
                new FakeMonotonicClock(), sessionIds ?? (() => $"session-{++_session}"));
            Host = new(Store, registrar, new FixedTimeProvider(DateTimeOffset.UnixEpoch));
        }
        internal RegistrationAdmissionResult Admit(string op, string terminal = "terminal") =>
            Host.RegisterNative(AdmissionRoot, op, new(new Dictionary<string, string?>
            {
                [OtelAttributes.RepoPath] = Worktree, [OtelAttributes.RepoDisplay] = "synthetic",
                [OtelAttributes.WorktreePath] = Worktree, [OtelAttributes.WorktreeBranch] = "branch",
                [OtelAttributes.TerminalId] = terminal, [OtelAttributes.AgentName] = "synthetic",
            }), new(new(new("repository", "repository"), "branch", Worktree), new(terminal), Output));
        internal NativeNoticeBatch Run(int maximum = 16) => Host.PublishNativeNotices(new NativeNoticeWorker(Store), maximum);
        internal string Immutable(NativeNoticeDelivery notice) =>
            Path.Combine(Output, "registration", "notices", $"n-{notice.NoticeId}.json");
        internal string Latest(string session) => Path.Combine(Output, "registration", StandingPublisher.FileNameFor(session));
        private SqliteConnection Open()
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Database, Pooling = false }.ToString());
            connection.Open();
            return connection;
        }
        internal NativeNoticeDelivery Notice()
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT n.notice_id,n.operation_id,n.target_root,n.publication_bytes,n.publication_digest,a.session_id
                FROM registration_notice_delivery n JOIN native_registration_admission_fact a USING(operation_id)
                ORDER BY a.rowid LIMIT 1;
                """;
            using var reader = command.ExecuteReader();
            Assert.True(reader.Read());
            return new(reader.GetString(0), reader.GetString(1), reader.GetString(2), (byte[])reader[3],
                reader.GetString(4), reader.GetString(5), "test-owner", 1, 1);
        }
        internal void Sql(string sql)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
        internal long Scalar(string sql)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return (long)command.ExecuteScalar()!;
        }
        internal byte[] Bytes(string sql)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return (byte[])command.ExecuteScalar()!;
        }
        internal string NativeState()
        {
            using var connection = Open();
            var tables = new List<List<object[]>>();
            foreach (var table in new[] { "native_registration_admission_fact", "agent_session_dim", "session_ended", "session_heartbeat" })
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {table} ORDER BY rowid";
                using var reader = command.ExecuteReader();
                var rows = new List<object[]>();
                while (reader.Read()) { var row = new object[reader.FieldCount]; reader.GetValues(row); rows.Add(row); }
                tables.Add(rows);
            }
            return JsonSerializer.Serialize(tables);
        }
        public void Dispose()
        {
            // Failed tests retain synthetic obligations. This cleanup is not publication evidence.
            Sql("""
                DROP TRIGGER IF EXISTS synthetic_ack_failure;
                UPDATE registration_notice_delivery SET state='InFlight',owner_id='test-cleanup',
                    attempt=attempt+1,ownership_version=ownership_version+1 WHERE state='Pending';
                UPDATE registration_notice_delivery SET state='Published',owner_id=NULL,
                    published_at_ms=0,ownership_version=ownership_version+1 WHERE state='InFlight';
                """);
            AdmissionRoot.Dispose();
            using (var reopened = SqliteWatcherObservationStore.Open(Database))
            using (var retired = new NoticeAdmissionCoordinator(reopened)) { }
            Store.Dispose();
            Directory.Delete(Root, true);
        }
    }
}
