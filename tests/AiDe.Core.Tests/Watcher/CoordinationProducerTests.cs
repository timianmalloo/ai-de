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
        using var lifetime = new EmitterTestLifetime(new SessionCoordinationEmitter(files.Writer));
        var emitter = lifetime.Emitter;
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
        Assert.Equal(0, liveAfterFailure);
        Assert.Equal(1, emitter.LiveCount);
    }

    [Fact]
    public void End_AppendDeniedThenRetried_DurablyEndsExactlyOnceAndKeepsRegistration()
    {
        using var files = new ProducerDirectory();
        using var lifetime = new EmitterTestLifetime(new SessionCoordinationEmitter(files.Writer));
        var emitter = lifetime.Emitter;
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
        Assert.Equal(1, liveAfterFailure);
        Assert.Equal(0, emitter.LiveCount);
    }

    [Fact]
    public async Task Register_PausedInsideWriter_IndependentSessionCompletes()
    {
        using var time = new PausedTime();
        using var files = new ProducerDirectory(time);
        using var lifetime = new EmitterTestLifetime(new SessionCoordinationEmitter(files.Writer));
        var emitter = lifetime.Emitter;
        const string other = "synthetic-independent";
        Task? independent = null;

        await WhileWritePaused(time, () => emitter.Register(Subject, Identity()), async () =>
        {
            independent = Task.Run(() =>
            {
                emitter.Register(other, Identity());
                emitter.Heartbeat(other);
                emitter.End(other);
            });
            await independent.WaitAsync(WaitBound);
            Assert.Equal(new[] { "register", "heartbeat", "session-end" }, Kinds(files, other));
            Assert.False(File.Exists(files.Log(Subject)));
        }, () => independent);

        Assert.Equal(new[] { "register" }, Kinds(files, Subject));
        Assert.Equal(1, emitter.LiveCount);
        Assert.Equal(0, GateCount(emitter));
    }

    [Fact]
    public async Task Register_PausedAppend_ConcurrentDuplicateRegistersOnce()
    {
        using var time = new PausedTime();
        using var files = new ProducerDirectory(time);
        using var lifetime = new EmitterTestLifetime(new SessionCoordinationEmitter(files.Writer));
        var emitter = lifetime.Emitter;
        Task? duplicate = null;

        await WhileWritePaused(time, () => emitter.Register(Subject, Identity()), () =>
        {
            duplicate = Task.Run(() => emitter.Register(Subject, Identity()));
            WaitForContender(emitter, duplicate);
            Assert.Equal(0, emitter.LiveCount);
            return Task.CompletedTask;
        }, () => duplicate);

        Assert.Equal(new[] { "register" }, Kinds(files, Subject));
        Assert.Equal(1, emitter.LiveCount);
        Assert.Equal(0, GateCount(emitter));
    }

    [Fact]
    public async Task Register_FormerWaiterOwnsGate_ThirdOperationCannotOvertakeHeartbeat()
    {
        using var time = new ThreeOperationTime();
        using var files = new ProducerDirectory(time);
        using var lifetime = new EmitterTestLifetime(new SessionCoordinationEmitter(files.Writer));
        var emitter = lifetime.Emitter;
        var owner = Task.Run(() => emitter.Register(Subject, Identity()));
        Task? waiter = null;
        Task? third = null;
        var reusedGate = false;
        var thirdReachedWriter = false;
        var liveWhileWaiting = -1;
        string[] durableWhileWaiting = [];
        try
        {
            Assert.True(time.FirstEntered.Wait(WaitBound), "Register did not reach the writer seam.");
            waiter = Task.Run(() => emitter.Heartbeat(Subject));
            WaitForContender(emitter, waiter);
            object originalGate;
            lock (PrivateField(emitter, "_gate")!)
            {
                var gates = Assert.IsAssignableFrom<System.Collections.IDictionary>(
                    PrivateField(emitter, "_sessionGates"));
                originalGate = Assert.IsAssignableFrom<object>(gates[Subject]);
                Assert.Equal(2, originalGate.GetType().GetField("References")!.GetValue(originalGate));
            }

            time.FirstRelease.Set();
            await owner.WaitAsync(WaitBound);
            Assert.True(time.SecondEntered.Wait(WaitBound), "Former waiter did not reach the writer seam.");
            third = Task.Run(() => emitter.End(Subject));
            // Observe admission, not elapsed time: an unscheduled third operation cannot pass.
            Assert.True(SpinWait.SpinUntil(() =>
            {
                lock (PrivateField(emitter, "_gate")!)
                {
                    return time.ThirdReachedWriter.IsSet || third.IsCompleted ||
                        (int)originalGate.GetType().GetField("References")!.GetValue(originalGate)! >= 2;
                }
            }, WaitBound), "Third operation neither acquired the original gate nor reached the writer.");
            lock (PrivateField(emitter, "_gate")!)
            {
                var gates = Assert.IsAssignableFrom<System.Collections.IDictionary>(
                    PrivateField(emitter, "_sessionGates"));
                reusedGate = ReferenceEquals(originalGate, gates[Subject]);
            }
            thirdReachedWriter = time.ThirdReachedWriter.IsSet;
            if (thirdReachedWriter)
            {
                await third.WaitAsync(WaitBound);
            }
            liveWhileWaiting = emitter.LiveCount;
            durableWhileWaiting = Kinds(files, Subject);
        }
        finally
        {
            time.FirstRelease.Set();
            time.SecondRelease.Set();
            await Task.WhenAll(owner, waiter ?? Task.CompletedTask, third ?? Task.CompletedTask)
                .WaitAsync(WaitBound);
        }

        var records = files.Read(Subject);
        output.WriteLine($"reusedGate={reusedGate}; thirdReachedWriterWhileHeartbeatPaused={thirdReachedWriter}; " +
            $"liveWhileWaiting={liveWhileWaiting}; durableWhileWaiting={string.Join(",", durableWhileWaiting)}; " +
            $"durableFinal={string.Join(",", Kinds(files, Subject))}; liveFinal={emitter.LiveCount}; gatesFinal={GateCount(emitter)}");
        Assert.Equal(new[] { "register", "heartbeat", "session-end" }, Kinds(files, Subject));
        Assert.True(reusedGate, "Third operation replaced the gate still owned by the former waiter.");
        Assert.False(thirdReachedWriter);
        Assert.Equal(new[] { "register" }, durableWhileWaiting);
        Assert.Equal(1, liveWhileWaiting);
        Assert.Single(records, record => record.GetProperty("kind").GetString() == "register");
        Assert.Equal(new[] { 1, 2, 3 }, records.Select(record => record.GetProperty("seq").GetInt32()));
        Assert.Equal(0, emitter.LiveCount);
        Assert.Equal(0, GateCount(emitter));
    }

    [Fact]
    public async Task Register_PausedAppend_EndWaitsForDurableRegistration()
    {
        using var time = new PausedTime();
        using var files = new ProducerDirectory(time);
        using var lifetime = new EmitterTestLifetime(new SessionCoordinationEmitter(files.Writer));
        var emitter = lifetime.Emitter;
        Task? end = null;

        await WhileWritePaused(time, () => emitter.Register(Subject, Identity()), () =>
        {
            end = Task.Run(() => emitter.End(Subject));
            WaitForContender(emitter, end);
            return Task.CompletedTask;
        }, () => end);

        Assert.Equal(new[] { "register", "session-end" }, Kinds(files, Subject));
        Assert.Equal(0, emitter.LiveCount);
        Assert.Equal(0, GateCount(emitter));
    }

    [Fact]
    public async Task HeartbeatAll_PausedAppend_ConcurrentEndCannotPrecedeHeartbeat()
    {
        using var time = new PausedTime();
        using var files = new ProducerDirectory(time);
        using var lifetime = new EmitterTestLifetime(new SessionCoordinationEmitter(files.Writer));
        var emitter = lifetime.Emitter;
        emitter.Register(Subject, Identity());
        Task? end = null;

        await WhileWritePaused(time, emitter.HeartbeatAll, () =>
        {
            end = Task.Run(() => emitter.End(Subject));
            WaitForContender(emitter, end);
            return Task.CompletedTask;
        }, () => end);
        emitter.HeartbeatAll();
        emitter.Heartbeat(Subject);
        emitter.End(Subject);

        Assert.Equal(new[] { "register", "heartbeat", "session-end" }, Kinds(files, Subject));
        Assert.Equal(0, emitter.LiveCount);
        Assert.Equal(0, GateCount(emitter));
    }

    [Fact]
    public async Task HeartbeatAll_StaleSnapshot_DoesNotHeartbeatEndedSession()
    {
        using var time = new PausedTime();
        using var files = new ProducerDirectory(time);
        using var lifetime = new EmitterTestLifetime(new SessionCoordinationEmitter(files.Writer));
        var emitter = lifetime.Emitter;
        emitter.Register(Subject, Identity());
        emitter.Register("synthetic-second", Identity());
        // P2 dispatches independently, not in HashSet order. Either session may heartbeat
        // before End, but neither can heartbeat after its admitted End.
        var ended = "synthetic-second";
        Task? end = null;

        await WhileWritePaused(time, () =>
        {
            var error = Record.Exception(emitter.HeartbeatAll);
            if (error is not null)
            {
                var batchError = Assert.IsType<CoordinationEmitterBatchException>(error);
                Assert.All(batchError.Results, result => Assert.False(result.Succeeded));
            }
        }, async () =>
        {
            end = Task.Run(() =>
            {
                var result = emitter.EndResult(ended);
                if (result.Code == CoordinationEmitterCodes.InputConflict)
                {
                    Assert.True(emitter.TryAbandonPending(ended).Succeeded);
                    emitter.End(ended);
                }
                else Assert.True(result.Succeeded);
            });
            WaitForContender(emitter, end, ended);
            await Task.CompletedTask;
        }, () => end);

        var endedKinds = Kinds(files, ended);
        Assert.Equal("session-end", endedKinds[^1]);
        Assert.Single(endedKinds, kind => kind == "session-end");
        emitter.Heartbeat(ended);
        Assert.Equal(endedKinds, Kinds(files, ended));
        Assert.Equal(1, emitter.LiveCount);
        Assert.Equal(0, GateCount(emitter));
    }

    [Fact]
    public void Register_RepeatedFailedFirstRegistrations_ReclaimsEverySessionGate()
    {
        using var files = new ProducerDirectory();
        using var lifetime = new EmitterTestLifetime(new SessionCoordinationEmitter(files.Writer));
        var emitter = lifetime.Emitter;
        for (var index = 0; index < 32; index++)
        {
            var session = $"synthetic-denied-{index}";
            using var denied = files.DenyAppend(session);
            Assert.IsAssignableFrom<IOException>(
                Record.Exception(() => emitter.Register(session, Identity())));
            Assert.Equal(0, emitter.LiveCount);
            Assert.Equal(0, GateCount(emitter));
        }
    }

    [Fact]
    public void Register_OrdinaryLifecycle_RecordsOneRegisterHeartbeatAndEnd()
    {
        using var files = new ProducerDirectory();
        using var lifetime = new EmitterTestLifetime(new SessionCoordinationEmitter(files.Writer));
        var emitter = lifetime.Emitter;

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
        using var lifetime = new EmitterTestLifetime(new SessionCoordinationEmitter(files.Writer));
        var emitter = lifetime.Emitter;
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

        public ProducerDirectory(TimeProvider? time = null)
        {
            Directory.CreateDirectory(Root);
            Writer = new CoordContractWriter(Root, time ?? new EpochTime());
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

    private static readonly TimeSpan WaitBound = TimeSpan.FromSeconds(10);

    private static string[] Kinds(ProducerDirectory files, string session) =>
        files.Read(session).Select(record => record.GetProperty("kind").GetString()!).ToArray();

    private static object? PrivateField(object instance, string name) =>
        instance.GetType().GetField(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(instance);

    private static int GateCount(SessionCoordinationEmitter emitter)
    {
        lock (PrivateField(emitter, "_gate")!)
        {
            var gates = Assert.IsAssignableFrom<System.Collections.IDictionary>(
                PrivateField(emitter, "_sessionGates"));
            return gates.Count;
        }
    }

    private static void WaitForContender(SessionCoordinationEmitter emitter, Task contender, string session = Subject)
    {
        Assert.True(SpinWait.SpinUntil(() =>
        {
            lock (PrivateField(emitter, "_gate")!)
            {
                var gates = PrivateField(emitter, "_sessionGates") as System.Collections.IDictionary;
                var gate = gates?[session];
                var references = gate?.GetType().GetField("References")?.GetValue(gate) as int?;
                return contender.IsCompleted || references >= 2;
            }
        }, WaitBound), "Contender neither finished nor acquired a gate reference.");
    }

    private static async Task WhileWritePaused(
        PausedTime time, Action write, Func<Task> concurrent, Func<Task?> contender)
    {
        time.Arm();
        var writing = Task.Run(write);
        try
        {
            Assert.True(time.Entered.Wait(WaitBound), "Real writer did not enter its time seam.");
            await concurrent();
        }
        finally
        {
            time.Release.Set();
            await Task.WhenAll(writing, contender() ?? Task.CompletedTask).WaitAsync(WaitBound);
        }
    }

    private sealed class ThreeOperationTime : TimeProvider, IDisposable
    {
        private int _calls;
        public ManualResetEventSlim FirstEntered { get; } = new();
        public ManualResetEventSlim FirstRelease { get; } = new();
        public ManualResetEventSlim SecondEntered { get; } = new();
        public ManualResetEventSlim SecondRelease { get; } = new();
        public ManualResetEventSlim ThirdReachedWriter { get; } = new();

        public override DateTimeOffset GetUtcNow()
        {
            var call = Interlocked.Increment(ref _calls);
            if (call == 3)
            {
                ThirdReachedWriter.Set();
            }
            else if (call is 1 or 2)
            {
                var entered = call == 1 ? FirstEntered : SecondEntered;
                var release = call == 1 ? FirstRelease : SecondRelease;
                entered.Set();
                if (!release.Wait(WaitBound))
                {
                    throw new TimeoutException($"Operation {call} writer seam was not released.");
                }
            }
            return DateTimeOffset.UnixEpoch;
        }

        public void Dispose()
        {
            FirstEntered.Dispose();
            FirstRelease.Dispose();
            SecondEntered.Dispose();
            SecondRelease.Dispose();
            ThirdReachedWriter.Dispose();
        }
    }

    private sealed class PausedTime : TimeProvider, IDisposable
    {
        private int _armed;
        public ManualResetEventSlim Entered { get; } = new();
        public ManualResetEventSlim Release { get; } = new();

        public void Arm() => Interlocked.Exchange(ref _armed, 1);

        public override DateTimeOffset GetUtcNow()
        {
            if (1 == Interlocked.Exchange(ref _armed, 0))
            {
                Entered.Set();
                if (!Release.Wait(WaitBound))
                {
                    throw new TimeoutException("Test writer seam was not released.");
                }
            }
            return DateTimeOffset.UnixEpoch;
        }

        public void Dispose()
        {
            Entered.Dispose();
            Release.Dispose();
        }
    }
}
