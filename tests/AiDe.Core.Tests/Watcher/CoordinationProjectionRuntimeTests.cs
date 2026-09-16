using System.Text;
using AiDe.Core.Watcher;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Tests.Watcher;

public sealed class CoordinationProjectionRuntimeTests
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(8, false)]
    [InlineData(2, true)]
    public void Pump_EquivalentRootAfterReopen_PreservesOriginalIdentitiesAndCheckpoint(
        int separatorCount, bool alternateSeparator)
    {
        using var files = new Pipeline();
        files.Write();
        var bytes = File.ReadAllBytes(files.LogFile);
        using var reader = SqliteWatcherObservationStore.Open(files.Database);
        var (first, _, _) = files.Compose(reader);
        first.PumpOnce();
        var originalSession = Assert.Single(reader.AllSessions());
        var originalMessage = Assert.Single(reader.AllBoardMessages());
        var firstAdmission = files.Number("SELECT MIN(first_receipt_n) FROM coord_projection_event;");
        var lastAdmission = files.Number("SELECT MAX(first_receipt_n) FROM coord_projection_event;");
        var feedCount = files.Count("coord_projection_feed");
        reader.Dispose();
        using var reopened = SqliteWatcherObservationStore.Open(files.Database);
        var (_, ingest, _) = files.Compose(reopened);
        var separator = alternateSeparator ? Path.AltDirectorySeparatorChar : Path.DirectorySeparatorChar;
        var equivalentRoot = files.Logs + new string(separator, separatorCount);
        var retry = new CoordContractLogPump(equivalentRoot, ingest);

        Assert.Equal(2, retry.PumpOnce());
        Assert.Equal(2, retry.PumpOnce());

        Assert.Equal(originalSession, Assert.Single(reopened.AllSessions()));
        Assert.Equal(originalMessage, Assert.Single(reopened.AllBoardMessages()));
        Assert.Equal(firstAdmission, files.Number("SELECT MIN(first_receipt_n) FROM coord_projection_event;"));
        Assert.Equal(lastAdmission, files.Number("SELECT MAX(first_receipt_n) FROM coord_projection_event;"));
        Assert.Equal(feedCount, files.Count("coord_projection_feed"));
        Assert.Equal(2, files.Count("coord_projection_event"));
        Assert.Equal(1, files.Count("coord_projection_checkpoint"));
        Assert.Equal(2, retry.LastRun.Replayed);
        Assert.Equal(bytes, File.ReadAllBytes(files.LogFile));
        Assert.Equal(CoordinationSourceCapture.RootKey(files.Logs),
            CoordinationSourceCapture.RootKey(equivalentRoot));
        var capture = Assert.Single(CoordinationSourceCapture.Read(equivalentRoot, reopened, ingest.Host));
        var replay = reopened.ProjectCoordination(capture.Pages[0], capture.Checkpoint, ingest.Host.ObservationAllocators);
        Assert.Equal(firstAdmission, Assert.Single(replay.Results, result => result.Kind is ContractRegister).Admission);
        var post = Assert.Single(replay.Results, result => result.Kind is ContractBoardPost);
        Assert.Equal(lastAdmission, post.Admission);
        Assert.Equal(originalMessage.MessageId, post.MessageId);
    }

    public static IEnumerable<object[]> FilesystemRoots()
    {
        var roots = OperatingSystem.IsWindows()
            ? new[] { Path.GetPathRoot(Environment.CurrentDirectory)!, @"\\server\share", @"\\server\share\" }
            : new[] { "/" };
        return from root in roots
               from count in new[] { 0, 1, 8 }
               select new object[] { root, count };
    }

    [Theory]
    [MemberData(nameof(FilesystemRoots))]
    public void RootKey_FilesystemRootWithRedundantSeparators_PreservesQualifiedRoot(string root, int count)
    {
        var expected = Path.GetPathRoot(Path.GetFullPath(root))!;
        expected = OperatingSystem.IsWindows() ? expected.ToUpperInvariant() : expected;
        var spelling = root + new string(Path.DirectorySeparatorChar, count);

        var key = CoordinationSourceCapture.RootKey(spelling);

        Assert.NotEmpty(expected);
        Assert.Equal(expected, Encoding.UTF8.GetString(Convert.FromBase64String(key[..^1])));
        Assert.Equal(CoordinationSourceCapture.RootKey(root), key);
    }

    [Fact]
    public void Pump_BeforeCommit_RollsBackEffectsReceiptsCheckpointAndMemory()
    {
        using var files = new Pipeline();
        files.Write();
        using var store = SqliteWatcherObservationStore.Open(files.Database);
        var (pump, ingest, _) = files.Compose(store);
        store.ProjectionFault = CoordinationFault.BeforeCommit;

        Assert.Throws<IOException>(() => pump.PumpOnce());

        using var reader = SqliteWatcherObservationStore.OpenReadOnly(files.Database);
        Assert.Empty(reader.AllSessions());
        Assert.Empty(reader.AllBoardMessages());
        Assert.Equal(0, files.Count("coord_projection_event"));
        Assert.Equal(0, files.Count("coord_projection_feed"));
        Assert.Equal(0, files.Count("coord_projection_checkpoint"));
        Assert.Null(ingest.ObservedSession("session"));
        Assert.Equal(0, ingest.Stats.Registered);
        Assert.Equal(2, pump.PumpOnce());
        Assert.Single(reader.AllSessions());
        Assert.Single(reader.AllBoardMessages());
    }

    [Fact]
    public void Pump_AfterCommitBeforeAcknowledgement_ReplayReturnsOriginalReceiptAndEffectWithoutAuthority()
    {
        using var files = new Pipeline();
        files.Write();
        using (var store = SqliteWatcherObservationStore.Open(files.Database))
        {
            var (pump, ingest, _) = files.Compose(store);
            store.ProjectionFault = CoordinationFault.AfterCommit;
            Assert.Throws<IOException>(() => pump.PumpOnce());
            Assert.Null(ingest.ObservedSession("session"));
            Assert.Equal(0, ingest.Stats.BoardPosts);
        }
        using var reader = SqliteWatcherObservationStore.OpenReadOnly(files.Database);
        var original = Assert.Single(reader.AllBoardMessages());
        var firstReceipt = files.Number("SELECT MAX(first_receipt_n) FROM coord_projection_event;");
        var receiptCount = files.Count("coord_projection_feed");
        using var reopened = SqliteWatcherObservationStore.Open(files.Database);
        var (retry, observed, registrar) = files.Compose(reopened);

        Assert.Equal(2, retry.PumpOnce());

        Assert.Equal(original, Assert.Single(reader.AllBoardMessages()));
        Assert.Equal(firstReceipt, files.Number("SELECT MAX(first_receipt_n) FROM coord_projection_event;"));
        Assert.Equal(receiptCount, files.Count("coord_projection_feed"));
        Assert.Equal(2, retry.LastRun.Replayed);
        var capture = Assert.Single(CoordinationSourceCapture.Read(files.Logs, reopened, observed.Host));
        var replayResult = reopened.ProjectCoordination(capture.Pages[0], capture.Checkpoint, observed.Host.ObservationAllocators);
        var replayedPost = Assert.Single(replayResult.Results, result => result.Kind is ContractBoardPost);
        Assert.Equal(firstReceipt, replayedPost.Admission);
        Assert.Equal(original.MessageId, replayedPost.MessageId);
        var session = Assert.Single(reader.AllSessions());
        Assert.Equal(session, observed.ObservedSession("session"));
        Assert.False(registrar.Verify(session.SessionId, new SequentialCapabilityFactory().Create()));
        Assert.Throws<WatcherException>(() => new MessageBoardService(reopened, registrar, files.Time)
            .Post(files.Root, session.SessionId, new SequentialCapabilityFactory().Create(),
                BoardMessageKind.Question, "not authorized"));
    }

    [Fact]
    public void Pump_ExactDuplicateOccurrence_AccountsBytesWithoutNewEffectOrAdmission()
    {
        using var files = new Pipeline();
        files.Write();
        using var store = SqliteWatcherObservationStore.Open(files.Database);
        var (pump, _, _) = files.Compose(store);
        pump.PumpOnce();
        var original = Assert.Single(store.AllBoardMessages());
        var line = File.ReadAllLines(files.LogFile)[1];
        File.AppendAllText(files.LogFile, line + "\n", new UTF8Encoding(false));

        Assert.Equal(3, pump.PumpOnce());
        Assert.Equal(3, pump.PumpOnce());

        Assert.Equal(original, Assert.Single(store.AllBoardMessages()));
        Assert.Equal(2, files.Count("coord_projection_event"));
        Assert.Equal(5, files.Count("coord_projection_feed"));
        Assert.Equal(new FileInfo(files.LogFile).Length,
            files.Number("SELECT accepted_offset FROM coord_projection_checkpoint;"));
    }

    [Fact]
    public void Pump_ConflictingDuplicate_RefusesWithoutRewritingHistoryOrAdvancing()
    {
        using var files = new Pipeline();
        files.Write();
        using var store = SqliteWatcherObservationStore.Open(files.Database);
        var (pump, _, _) = files.Compose(store);
        pump.PumpOnce();
        var end = files.Number("SELECT accepted_offset FROM coord_projection_checkpoint;");
        var original = Assert.Single(store.AllBoardMessages());
        var conflicting = File.ReadAllLines(files.LogFile)[1].Replace("question-content", "different-content");
        File.AppendAllText(files.LogFile, conflicting + "\n", new UTF8Encoding(false));

        var error = Assert.Throws<CoordinationSourceException>(() => pump.PumpOnce());

        Assert.Equal(CoordinationErrors.DuplicateConflict, error.Code);
        Assert.Equal(error.Code, pump.LastRun.Diagnostic);
        Assert.Equal(original, Assert.Single(store.AllBoardMessages()));
        Assert.Equal(end, files.Number("SELECT accepted_offset FROM coord_projection_checkpoint;"));
        Assert.Equal(4, files.Count("coord_projection_feed"));
    }

    [Theory]
    [InlineData("changed")]
    [InlineData("truncated")]
    [InlineData("missing")]
    public void Pump_AcceptedSourceGap_RefusesWithoutAdvance(string change)
    {
        using var files = new Pipeline();
        files.Write();
        using var store = SqliteWatcherObservationStore.Open(files.Database);
        var (pump, _, _) = files.Compose(store);
        pump.PumpOnce();
        var original = Assert.Single(store.AllBoardMessages());
        var end = files.Number("SELECT accepted_offset FROM coord_projection_checkpoint;");
        switch (change)
        {
            case "changed":
                File.WriteAllText(files.LogFile,
                    File.ReadAllText(files.LogFile).Replace("question-content", "modified-content"), new UTF8Encoding(false));
                break;
            case "truncated":
                File.WriteAllText(files.LogFile, "", new UTF8Encoding(false));
                break;
            case "missing":
                File.Delete(files.LogFile);
                break;
        }

        var error = Assert.Throws<CoordinationSourceException>(() => pump.PumpOnce());

        Assert.Equal(CoordinationErrors.SourceGap, error.Code);
        Assert.Equal(error.Code, pump.LastRun.Diagnostic);
        Assert.Equal(original, Assert.Single(store.AllBoardMessages()));
        Assert.Equal(end, files.Number("SELECT accepted_offset FROM coord_projection_checkpoint;"));
    }

    [Fact]
    public void Pump_IncompleteTail_DefersUntilLfAndReportsUnsupportedVersion()
    {
        using var files = new Pipeline();
        files.Write();
        var tail = """{"contract":"loomkeeper/999","kind":"heartbeat","session":"session","seq":3,"at":0}""";
        File.AppendAllText(files.LogFile, tail, new UTF8Encoding(false));
        using var store = SqliteWatcherObservationStore.Open(files.Database);
        var (pump, _, _) = files.Compose(store);

        Assert.Equal(2, pump.PumpOnce());
        Assert.Equal(2, files.Count("coord_projection_event"));
        File.AppendAllText(files.LogFile, "\n", new UTF8Encoding(false));
        Assert.Equal(2, pump.PumpOnce());

        Assert.Equal(1, pump.LastRun.Refused);
        Assert.Equal(3, files.Count("coord_projection_event"));
        Assert.Equal(1, files.Number("SELECT COUNT(*) FROM coord_projection_feed WHERE reason='UNSUPPORTED_VERSION';"));
        Assert.Equal(new FileInfo(files.LogFile).Length, files.Number("SELECT accepted_offset FROM coord_projection_checkpoint;"));
    }

    [Fact]
    public void Pump_OverlargeCompleteRecord_RejectsBeforeAnyAdmission()
    {
        using var files = new Pipeline();
        files.Write();
        File.AppendAllText(files.LogFile, new string('x', 65536) + "\n");
        using var store = SqliteWatcherObservationStore.Open(files.Database);
        var (pump, _, _) = files.Compose(store);

        Assert.Equal(CoordinationErrors.Bounds, Assert.Throws<CoordinationSourceException>(() => pump.PumpOnce()).Code);

        Assert.Empty(store.AllSessions());
        Assert.Equal(0, files.Count("coord_projection_event"));
    }

    [Theory]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(129)]
    [InlineData(257)]
    public void Pump_PageBoundaries_KeepContiguousCheckpointsAndStableReplay(int records)
    {
        using var files = new Pipeline();
        files.Write();
        for (var index = 2; index < records; index++)
        {
            files.Writer.WriteHeartbeat("session");
        }
        using var store = SqliteWatcherObservationStore.Open(files.Database);
        var (pump, _, _) = files.Compose(store);

        Assert.Equal(records, pump.PumpOnce());
        var receiptCount = files.Count("coord_projection_feed");
        Assert.Equal(records, pump.PumpOnce());

        Assert.Equal(records, files.Count("coord_projection_event"));
        Assert.Equal(receiptCount, files.Count("coord_projection_feed"));
        Assert.Equal(records, pump.LastRun.Replayed);
        Assert.Equal(new FileInfo(files.LogFile).Length, pump.LastRun.Bytes);
        Assert.True(pump.LastRun.ElapsedMilliseconds >= 0);
        Assert.Equal(new FileInfo(files.LogFile).Length, files.Number("SELECT accepted_offset FROM coord_projection_checkpoint;"));
        Assert.Single(store.AllBoardMessages());
    }

    [Fact]
    public void Pump_TooManyFiles_RejectsBeforeAnyAdmission()
    {
        using var files = new Pipeline();
        Directory.CreateDirectory(files.Logs);
        for (var index = 0; index < 129; index++)
        {
            File.WriteAllText(Path.Combine(files.Logs, index + ".jsonl"), "\n");
        }
        using var store = SqliteWatcherObservationStore.Open(files.Database);
        var (pump, _, _) = files.Compose(store);

        Assert.Equal(CoordinationErrors.Bounds, Assert.Throws<CoordinationSourceException>(() => pump.PumpOnce()).Code);

        Assert.Equal(0, files.Count("coord_projection_event"));
    }

    [Fact]
    public void Pump_LateParentAndAuthorityWork_RetainPendingPayloads()
    {
        using var files = new Pipeline();
        files.Write();
        files.Writer.WriteBoardPost("session", "reply", "late reply", "missing");
        files.Writer.Write("episode-open", "session", new Dictionary<string, string?>
        {
            [CoordContract.EpisodeAttributes.Goal] = "goal",
            [CoordContract.EpisodeAttributes.DoneWhen] = "done",
        });
        using var store = SqliteWatcherObservationStore.Open(files.Database);
        var (pump, _, _) = files.Compose(store);

        pump.PumpOnce();

        Assert.Equal(2, pump.LastRun.Pending);
        Assert.Equal(2, files.Number("""
            SELECT COUNT(*) FROM coord_projection_event
            WHERE application_state='pending' AND raw_bytes IS NOT NULL AND canonical_bytes IS NOT NULL;
            """));
        Assert.Single(store.AllBoardMessages());
    }

    [Fact]
    public void Project_StaleCapture_DiscardsWithoutAllocatingOrPublishing()
    {
        using var files = new Pipeline();
        files.Write();
        using var first = SqliteWatcherObservationStore.Open(files.Database);
        using var second = SqliteWatcherObservationStore.Open(files.Database);
        var (pump, ingest, _) = files.Compose(first);
        var stale = Assert.Single(CoordinationSourceCapture.Read(files.Logs, second, ingest.Host));
        pump.PumpOnce();

        var result = second.ProjectCoordination(stale.Pages[0], stale.Checkpoint,
            new(() => throw new InvalidOperationException("session allocated"),
                () => throw new InvalidOperationException("message allocated"), 0, files.Time.GetUtcNow()));

        Assert.True(result.Stale);
        Assert.Empty(result.Results);
        Assert.Equal(2, files.Count("coord_projection_event"));
        Assert.Single(second.AllBoardMessages());
    }

    [Fact]
    public async Task Pump_IndependentWriters_CommitOneMappingAndEffect()
    {
        using var files = new Pipeline();
        files.Write();
        using var first = SqliteWatcherObservationStore.Open(files.Database);
        using var second = SqliteWatcherObservationStore.Open(files.Database);
        var (a, _, _) = files.Compose(first);
        var (b, _, _) = files.Compose(second);

        await Task.WhenAll(Task.Run(() => a.PumpOnce()), Task.Run(() => b.PumpOnce()))
            .WaitAsync(TimeSpan.FromSeconds(15));

        using var reader = SqliteWatcherObservationStore.OpenReadOnly(files.Database);
        Assert.Single(reader.AllSessions());
        Assert.Single(reader.AllBoardMessages());
        Assert.Equal(2, files.Count("coord_projection_event"));
        Assert.Equal(4, files.Count("coord_projection_feed"));
        Assert.Equal(1, files.Count("coord_projection_checkpoint"));
    }

    private sealed class Pipeline : IDisposable
    {
        public string Root { get; } = Path.Combine(Environment.CurrentDirectory, "p23-native-" + Guid.NewGuid().ToString("N"));
        public string Database => Path.Combine(Root, "watcher.db");
        public string Logs => Path.Combine(Root, "wire");
        public string LogFile => Path.Combine(Logs, "session.jsonl");
        public FixedTimeProvider Time { get; } = new(DateTimeOffset.UnixEpoch);
        public CoordContractWriter Writer => new(Logs, Time);

        public Pipeline() => Directory.CreateDirectory(Root);

        public void Write()
        {
            var attrs = WatcherFixtures.HarnessRegistration().Attributes.ToDictionary(pair => pair.Key, pair => pair.Value);
            attrs[OtelAttributes.RepoPath] = Root;
            attrs[OtelAttributes.WorktreePath] = Root;
            Writer.WriteRegister("session", attrs);
            Writer.WriteBoardPost("session", "question", "question-content");
        }

        public (CoordContractLogPump Pump, InjectedContractIngest Ingest, TrustedRegistrar Registrar) Compose(
            SqliteWatcherObservationStore store)
        {
            var registrar = new TrustedRegistrar(store, new SequentialCapabilityFactory(), new FakeMonotonicClock());
            var ingest = new InjectedContractIngest(new IngestHost(store, registrar, Time));
            return (new(Logs, ingest), ingest, registrar);
        }

        public long Count(string table) => Number("SELECT COUNT(*) FROM " + table + ";");

        public long Number(string sql)
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = Database, Mode = SqliteOpenMode.ReadOnly, Pooling = false,
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return (long)command.ExecuteScalar()!;
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
