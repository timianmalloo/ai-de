using System.Text;
using System.Text.Json;
using AiDe.Core.Watcher;
using Xunit.Abstractions;

namespace AiDe.Core.Tests.Watcher;

/// <summary>
/// Approved P2 producer RED floor. Synthetic fixtures exercise the native writer, not ingest.
/// These tests do not establish partial-write recovery, pending limits, or queue boundedness.
/// </summary>
public sealed class CoordinationProducerTests(ITestOutputHelper output)
{
    private const string Subject = "synthetic-subject";
    private const int CompleteLineLimit = 64 * 1024;
    private const int RootFileLimit = 128;
    private const long RootByteLimit = 32L * 1024 * 1024;

    [Fact]
    public void Register_AppendDeniedThenRetried_DurablyRegistersExactlyOnce()
    {
        using var files = new ProducerDirectory();
        var emitter = new SessionCoordinationEmitter(files.Writer);
        using (files.DenyAppend(Subject))
        {
            Assert.IsAssignableFrom<IOException>(
                Record.Exception(() => emitter.Register(Subject, Identity())));
        }
        var liveAfterFailure = emitter.LiveCount;

        emitter.Register(Subject, Identity());
        emitter.Register(Subject, Identity());

        var records = files.Read(Subject);
        output.WriteLine($"liveAfterFailure={liveAfterFailure}; durableRegisters={records.Length}; liveAfterRetry={emitter.LiveCount}");
        Assert.Single(records);
        AssertEvent(records[0], "register", Subject);
        Assert.Equal("synthetic-project", records[0].GetProperty("attrs").GetProperty(OtelAttributes.RepoDisplay).GetString());
        Assert.Equal(1, emitter.LiveCount);
    }

    [Fact]
    public void End_AppendDeniedThenRetried_DurablyEndsExactlyOnceAndKeepsRegistration()
    {
        using var files = new ProducerDirectory();
        var emitter = new SessionCoordinationEmitter(files.Writer);
        emitter.Register(Subject, Identity());
        var registration = File.ReadAllBytes(files.Log(Subject));
        using (files.DenyAppend(Subject))
        {
            Assert.IsAssignableFrom<IOException>(Record.Exception(() => emitter.End(Subject)));
        }
        var liveAfterFailure = emitter.LiveCount;
        Assert.Equal(registration, File.ReadAllBytes(files.Log(Subject)));

        emitter.End(Subject);
        emitter.End(Subject);

        var records = files.Read(Subject);
        var ends = records.Where(record => "session-end" == record.GetProperty("kind").GetString()).ToArray();
        output.WriteLine($"liveAfterFailure={liveAfterFailure}; durableEnds={ends.Length}; records={records.Length}; liveAfterRetry={emitter.LiveCount}");
        Assert.Single(ends);
        Assert.Equal(2, records.Length);
        AssertEvent(records[0], "register", Subject);
        AssertEvent(records[1], "session-end", Subject);
        Assert.Equal(registration, File.ReadAllBytes(files.Log(Subject))[..registration.Length]);
        Assert.Equal(0, emitter.LiveCount);
    }

    [Fact]
    public void Register_OrdinaryLifecycle_RecordsOneRegisterHeartbeatAndEnd()
    {
        using var files = new ProducerDirectory();
        var emitter = new SessionCoordinationEmitter(files.Writer);

        emitter.Register(Subject, Identity());
        emitter.Register(Subject, Identity());
        emitter.Heartbeat(Subject);
        emitter.End(Subject);
        emitter.End(Subject);
        emitter.Heartbeat(Subject);

        var records = files.Read(Subject);
        Assert.Equal(3, records.Length);
        AssertEvent(records[0], "register", Subject);
        AssertEvent(records[1], "heartbeat", Subject);
        AssertEvent(records[2], "session-end", Subject);
        Assert.Equal(new[] { 1, 2, 3 }, records.Select(record => record.GetProperty("seq").GetInt32()));
        Assert.Equal(0, emitter.LiveCount);
        output.WriteLine("durableRegisters=1; durableHeartbeats=1; durableEnds=1; live=0");
    }

    [Fact]
    public void Register_OtherSessionAppendDenied_IndependentSessionStillCompletes()
    {
        using var files = new ProducerDirectory();
        var emitter = new SessionCoordinationEmitter(files.Writer);
        using var denied = files.DenyAppend(Subject);
        const string other = "synthetic-other";

        emitter.Register(other, Identity());
        emitter.Heartbeat(other);
        emitter.End(other);

        var records = files.Read(other);
        Assert.Equal(3, records.Length);
        AssertEvent(records[0], "register", other);
        AssertEvent(records[1], "heartbeat", other);
        AssertEvent(records[2], "session-end", other);
        Assert.Equal(0, denied.Length);
        Assert.Equal(0, emitter.LiveCount);
        output.WriteLine("deniedSubjectBytes=0; otherSessionRecords=3; otherSessionEnded=true");
    }

    [Theory]
    [InlineData('A')]
    [InlineData('\u00e9')]
    public void Write_CompleteEncodedLineAt64KiB_AcceptsExactlyTheBound(char glyph)
    {
        using var files = new ProducerDirectory();
        var attributes = PayloadAtLineBytes(CompleteLineLimit, glyph);

        files.Writer.Write("update", Subject, attributes);

        var wire = File.ReadAllBytes(files.Log(Subject));
        output.WriteLine($"glyph=U+{(int)glyph:X4}; completeLineBytes={wire.Length}; terminalByte={wire[^1]}");
        Assert.Equal(CompleteLineLimit, wire.Length);
        Assert.Equal((byte)'\n', wire[^1]);
        var record = Assert.Single(files.Read(Subject));
        AssertEvent(record, "update", Subject);
        Assert.Equal(attributes["payload"], record.GetProperty("attrs").GetProperty("payload").GetString());
    }

    [Theory]
    [InlineData('A')]
    [InlineData('\u00e9')]
    public void Write_CompleteEncodedLine64KiBPlusOne_RefusesWithoutAcceptedBytes(char glyph)
    {
        using var files = new ProducerDirectory();
        var attributes = PayloadAtLineBytes(CompleteLineLimit + 1, glyph);

        var refusal = Record.Exception(() => files.Writer.Write("update", Subject, attributes));

        var acceptedBytes = files.Length(Subject);
        output.WriteLine($"glyph=U+{(int)glyph:X4}; candidateCompleteLineBytes={CompleteLineLimit + 1}; acceptedBytes={acceptedBytes}; refusal={refusal?.GetType().FullName ?? "none"}");
        Assert.Equal(0, acceptedBytes);
        // Write is void today: do not invent a future typed admission-result or exception taxonomy.
        Assert.NotNull(refusal);
    }

    [Fact]
    public void WriteHeartbeat_RootAlreadyHas128Files_RefusesFile129()
    {
        using var files = new ProducerDirectory();
        for (var index = 0; index < RootFileLimit; index++)
        {
            using var seed = File.Create(files.Log($"synthetic-seed-{index:D3}"));
        }

        var refusal = Record.Exception(() => files.Writer.WriteHeartbeat(Subject));

        var count = Directory.GetFiles(files.Root, "*.jsonl").Length;
        output.WriteLine($"rootFilesBefore={RootFileLimit}; rootFilesAfter={count}; acceptedBytes={files.Length(Subject)}; refusal={refusal?.GetType().FullName ?? "none"}");
        Assert.Equal(RootFileLimit, count);
        Assert.False(File.Exists(files.Log(Subject)));
        Assert.NotNull(refusal);
    }

    [Fact]
    public void WriteHeartbeat_RootAlreadyHas32MiB_RefusesWithoutChangingExistingBytes()
    {
        using var files = new ProducerDirectory();
        var seedPath = files.Log("synthetic-quota-only");
        // This is a byte-quota fixture, deliberately not a valid consumer log.
        using (var seed = File.Create(seedPath))
        {
            seed.SetLength(RootByteLimit);
        }
        var hashBefore = Hash(seedPath);

        var refusal = Record.Exception(() => files.Writer.WriteHeartbeat(Subject));

        var bytesAfter = Directory.GetFiles(files.Root, "*.jsonl").Sum(path => new FileInfo(path).Length);
        output.WriteLine($"rootBytesBefore={RootByteLimit}; rootBytesAfter={bytesAfter}; acceptedBytes={files.Length(Subject)}; refusal={refusal?.GetType().FullName ?? "none"}");
        Assert.Equal(hashBefore, Hash(seedPath));
        Assert.Equal(RootByteLimit, bytesAfter);
        Assert.Equal(0, files.Length(Subject));
        Assert.NotNull(refusal);
    }

    private static IReadOnlyDictionary<string, string?> PayloadAtLineBytes(int completeLineBytes, char glyph)
    {
        using var calibration = new ProducerDirectory();
        calibration.Writer.Write("update", Subject, new Dictionary<string, string?> { ["payload"] = "" });
        var envelopeBytes = checked((int)calibration.Length(Subject));
        var unitBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(glyph.ToString())) - 2;
        var payloadBytes = completeLineBytes - envelopeBytes;
        var value = new string(glyph, payloadBytes / unitBytes) + new string('A', payloadBytes % unitBytes);
        Assert.Equal(completeLineBytes, envelopeBytes + Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(value)) - 2);
        return new Dictionary<string, string?> { ["payload"] = value };
    }

    private static SessionCoordinationIdentity Identity() =>
        new("synthetic-repository", "synthetic-project", "synthetic-branch",
            "synthetic-worktree", "synthetic-terminal", "synthetic-agent");

    private static void AssertEvent(JsonElement record, string kind, string session)
    {
        Assert.Equal(kind, record.GetProperty("kind").GetString());
        Assert.Equal(session, record.GetProperty("session").GetString());
        Assert.Equal(CoordContract.Version, record.GetProperty("contract").GetString());
        Assert.Equal(0, record.GetProperty("at").GetDouble());
    }

    private static byte[] Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return System.Security.Cryptography.SHA256.HashData(stream);
    }

    private sealed class ProducerDirectory : IDisposable
    {
        public string Root { get; } = Path.Combine(
            AppContext.BaseDirectory, "producer-fixtures", Guid.NewGuid().ToString("N"));

        public CoordContractWriter Writer { get; }

        public ProducerDirectory()
        {
            Directory.CreateDirectory(Root);
            Writer = new CoordContractWriter(Root, new EpochTime());
        }

        public string Log(string session) => Path.Combine(Root, session + ".jsonl");

        public long Length(string session) => File.Exists(Log(session)) ? new FileInfo(Log(session)).Length : 0;

        public FileStream DenyAppend(string session)
        {
            using (new FileStream(Log(session), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)) { }
            return new FileStream(Log(session), FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public JsonElement[] Read(string session) => File.ReadAllLines(Log(session))
            .Select(line =>
            {
                using var document = JsonDocument.Parse(line);
                return document.RootElement.Clone();
            }).ToArray();

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }

    private sealed class EpochTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch;
    }
}
