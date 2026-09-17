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
        using var files = new ProducerDirectory();
        var budget = new CoordinationEmitterBudget();
        using var lifetime = new EmitterTestLifetime(new SessionCoordinationEmitter(files.Writer, budget));
        var emitter = lifetime.Emitter;
        const string ended = "synthetic-second";
        emitter.Register(ended, Identity());
        var captured = State(emitter, ended);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var parent = new System.Diagnostics.Activity("synthetic-target-batch").Start();
        var arrivals = 0;
        void PauseTarget(object? sender, System.Diagnostics.ActivityChangedEventArgs args)
        {
            if (!ReferenceEquals(args.Previous, parent) ||
                args.Current?.OperationName != "coordination.emitter") return;
            Interlocked.Increment(ref arrivals);
            entered.Set();
            if (!release.Wait(WaitBound)) throw new TimeoutException("Target lifecycle barrier was not released.");
        }
        System.Diagnostics.Activity.CurrentChanged += PauseTarget;
        var batch = Task.Run(() => emitter.HeartbeatAllResultsAsync());
        IReadOnlyList<CoordinationEmitterResult> results;
        try
        {
            Assert.True(entered.Wait(WaitBound), "The sole captured target did not reach Activity.Start.");
            Assert.Same(captured, State(emitter, ended));
            // This lifecycle is created after capture, so only the target batch worker is paused.
            using var independent = new System.Diagnostics.Activity("synthetic-independent-work").Start();
            emitter.Register(Subject, Identity());
            emitter.Heartbeat(Subject);
            emitter.End(Subject);
            Assert.Equal(new[] { "register", "heartbeat", "session-end" }, Kinds(files, Subject));

            var end = emitter.EndResult(ended);
            var receipt = Receipt("end-before-stale-release", end, emitter, files, ended, captured);
            Assert.True(end.Succeeded, receipt);
            Assert.Equal(CoordinationEmitterOutcome.Admitted, end.Outcome);
            Assert.Equal(new[] { "register", "session-end" }, Kinds(files, ended));
            Assert.Null(State(emitter, ended));
        }
        finally
        {
            release.Set();
            try { results = await batch.WaitAsync(WaitBound); }
            finally { System.Diagnostics.Activity.CurrentChanged -= PauseTarget; }
        }

        var stale = Assert.Single(results);
        Receipt("released-stale-target", stale, emitter, files, ended, captured);
        Assert.Equal(1, arrivals);
        Assert.Equal(ended, stale.Session);
        Assert.Equal(CoordinationEmitterOutcome.Refused, stale.Outcome);
        Assert.Equal(CoordinationEmitterCodes.StaleLifecycle, stale.Code);
        Assert.Null(stale.Prepared);
        Assert.Equal(new[] { "register", "session-end" }, Kinds(files, ended));
        Assert.Equal(new[] { 1, 2 }, files.Read(ended).Select(row => row.GetProperty("seq").GetInt32()));
        using var followup = new System.Diagnostics.Activity("synthetic-followup").Start();
        Assert.Equal(CoordinationEmitterOutcome.NoOp, emitter.HeartbeatResult(ended).Outcome);
        Assert.Equal(new[] { "register", "session-end" }, Kinds(files, ended));
        Assert.Equal(0, emitter.LiveCount);
        Assert.Equal(0, GateCount(emitter));
        Assert.Equal(0, budget.Occupied);
    }

    [Fact]
    public async Task End_RootHeldThroughFlush_RetainsIntentForOneExplicitRetry()
    {
        using var files = new ProducerDirectory();
        var budget = new CoordinationEmitterBudget();
        using var lifetime = new EmitterTestLifetime(new SessionCoordinationEmitter(files.Writer, budget));
        var emitter = lifetime.Emitter;
        emitter.Register(Subject, Identity());
        var original = File.ReadAllBytes(files.Log(Subject));
        var intent = Assert.IsType<CoordinationEmitterState>(State(emitter, Subject));
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var holder = new CoordContractWriter(files.Root, new EpochTime())
        {
            FlushFault = stream =>
            {
                stream.Flush(flushToDisk: true);
                entered.Set();
                if (!release.Wait(WaitBound)) throw new TimeoutException("Root flush holder was not released.");
            }
        };
        var holding = Task.Run(() => holder.WriteHeartbeat("synthetic-holder"));
        try
        {
            Assert.True(entered.Wait(WaitBound), "The real writer never reached flush under root exclusion.");
            var unavailable = emitter.EndResult(Subject);
            Receipt("root-held-end", unavailable, emitter, files, Subject, intent);
            Assert.Equal(CoordinationWriteCodes.WriterBusy, unavailable.Code);
            Assert.Equal(CoordinationEmitterOutcome.Unavailable, unavailable.Outcome);
            Assert.Equal(CoordinationEmitterMembership.Live, unavailable.Membership);
            Assert.Equal(CoordinationEmitterPhase.AwaitingPreparation, unavailable.Phase);
            Assert.Null(unavailable.Admission);
            Assert.Null(unavailable.Prepared);
            Assert.Equal(original, File.ReadAllBytes(files.Log(Subject)));
            Assert.Same(intent, State(emitter, Subject));
            Assert.Equal(CoordinationEmitterOperation.End, intent.Pending);
            Assert.Equal(1, emitter.LiveCount);
            Assert.Equal(1, emitter.RetainedCount);
            Assert.Equal(1, budget.Occupied);
            Assert.False(holding.IsCompleted);
        }
        finally
        {
            release.Set();
            await holding.WaitAsync(WaitBound);
        }

        var retried = emitter.RetryPending(Subject);
        var receipt = Receipt("one-explicit-end-retry", retried, emitter, files, Subject, intent);
        Assert.True(retried.Succeeded, receipt);
        Assert.Equal(CoordinationEmitterOutcome.Admitted, retried.Outcome);
        Assert.Equal(2, retried.Admission?.Sequence);
        Assert.Equal(Subject, retried.Admission?.Session);
        Assert.Equal(original.Length, retried.Admission?.Start);
        Assert.Equal(new[] { "register", "session-end" }, Kinds(files, Subject));
        Assert.Equal(original, File.ReadAllBytes(files.Log(Subject))[..original.Length]);
        Assert.Equal(new[] { "heartbeat" }, Kinds(files, "synthetic-holder"));
        Assert.Null(intent.Pending);
        Assert.Null(State(emitter, Subject));
        Assert.Equal(0, emitter.LiveCount);
        Assert.Equal(0, emitter.RetainedCount);
        Assert.Equal(0, GateCount(emitter));
        Assert.Equal(0, budget.Occupied);
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

    private static CoordinationEmitterState? State(SessionCoordinationEmitter emitter, string session)
    {
        lock (PrivateField(emitter, "_gate")!)
        {
            var states = Assert.IsType<Dictionary<string, CoordinationEmitterState>>(PrivateField(emitter, "_states"));
            return states.GetValueOrDefault(session);
        }
    }

    private string Receipt(string stage, CoordinationEmitterResult result, SessionCoordinationEmitter emitter,
        ProducerDirectory files, string session, CoordinationEmitterState? captured)
    {
        var current = State(emitter, session);
        var receipt = JsonSerializer.Serialize(new
        {
            stage, result.Session, result.Succeeded, outcome = result.Outcome.ToString(), result.Code,
            membership = result.Membership.ToString(), phase = result.Phase?.ToString(), result.Admission,
            preparedIdentity = result.Prepared is null ? (int?)null :
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(result.Prepared),
            preparedAdmission = result.Prepared?.Admission,
            preparedBytes = result.Prepared?.CopyBytes(),
            capturedIdentity = captured is null ? (int?)null :
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(captured),
            sameState = ReferenceEquals(captured, current),
            pending = current?.Pending?.ToString(), currentPhase = current?.Phase.ToString(),
            emitter.LiveCount, emitter.RetainedCount,
            nativeJsonl = Directory.GetFiles(files.Root, "*.jsonl").Order(StringComparer.Ordinal)
                .ToDictionary(path => Path.GetFileName(path), ReadWire)
        });
        output.WriteLine(receipt);
        return receipt;
    }

    private static string ReadWire(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

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
