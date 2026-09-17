using AiDe.Core.Watcher;

namespace AiDe.Core.Tests.Watcher;

public sealed class CoordinationEmitterPendingTests
{
    private static readonly SessionCoordinationIdentity Identity =
        new("synthetic-repo", "synthetic", "branch", "tree", "terminal", "agent");

    [Fact]
    public void Register_LostCompleteAcknowledgement_RetryDoesNotDuplicate()
    {
        using var files = new PendingDirectory();
        var lose = true;
        var writer = new CoordContractWriter(files.Root)
        {
            DisposeFault = () =>
            {
                if (lose) { lose = false; throw new IOException("synthetic acknowledgement loss"); }
            }
        };
        var emitter = new SessionCoordinationEmitter(writer);
        using var lifetime = new EmitterTestLifetime(emitter);
        try
        {
            Assert.IsAssignableFrom<IOException>(Record.Exception(() => emitter.Register("a", Identity)));
            var original = File.ReadAllBytes(files.Log("a"));

            emitter.Register("a", Identity);

            Assert.Equal(original, File.ReadAllBytes(files.Log("a")));
            Assert.Equal(1, emitter.LiveCount);
        }
        finally { emitter.End("a"); }
    }

    [Fact]
    public void HeartbeatAll_FirstSourceDenied_HealthySourceStillWritten()
    {
        using var files = new PendingDirectory();
        var emitter = new SessionCoordinationEmitter(new CoordContractWriter(files.Root));
        using var lifetime = new EmitterTestLifetime(emitter);
        emitter.Register("a", Identity);
        emitter.Register("b", Identity);
        try
        {
            using var denied = new FileStream(files.Log("a"), FileMode.Open, FileAccess.Read, FileShare.None);
            Assert.IsAssignableFrom<IOException>(Record.Exception(emitter.HeartbeatAll));
            Assert.Equal(2, File.ReadAllLines(files.Log("b")).Length);
        }
        finally { emitter.TryAbandonPending("a"); emitter.TryAbandonPending("b"); emitter.End("a"); emitter.End("b"); }
    }

    private sealed class PendingDirectory : IDisposable
    {
        public string Root { get; } = Path.Combine(AppContext.BaseDirectory, "emitter-fixtures", Guid.NewGuid().ToString("N"));
        public string Log(string id) => Path.Combine(Root, id + ".jsonl");
        public void Dispose() { if (Directory.Exists(Root)) Directory.Delete(Root, true); }
    }

    [Fact]
    public void RetryPending_UncertainRegister_PreservesActualPreparationAndAdmission()
    {
        using var files = new PendingDirectory();
        var fail = true;
        var writer = new CoordContractWriter(files.Root, new EpochTime())
        {
            DisposeFault = () => { if (fail) throw new IOException("synthetic"); }
        };
        var budget = new CoordinationEmitterBudget();
        var emitter = new SessionCoordinationEmitter(writer, budget);
        var uncertain = emitter.RegisterResult("a", Identity);
        var prepared = Assert.IsType<PreparedWrite>(uncertain.Prepared);
        var bytes = prepared.CopyBytes();
        Assert.Equal(CoordinationEmitterOutcome.Uncertain, uncertain.Outcome);
        Assert.Equal(CoordinationEmitterMembership.PendingRegistration, uncertain.Membership);
        Assert.Equal(CoordinationEmitterOutcome.Uncertain, emitter.TryAbandonPending("a").Outcome);
        Assert.Throws<CoordinationEmitterException>(emitter.Dispose);
        Assert.False(emitter.TryRetire());
        Assert.Equal(1, budget.Occupied);
        Assert.Equal(CoordinationEmitterCodes.InputConflict,
            emitter.RegisterResult("a", Identity with { AgentName = "changed" }).Code);
        var again = emitter.RetryPending("a");
        Assert.Same(prepared, again.Prepared);
        Assert.Equal(bytes, File.ReadAllBytes(files.Log("a")));

        fail = false;
        var admitted = emitter.RetryPending("a");

        Assert.Equal(CoordinationEmitterOutcome.Admitted, admitted.Outcome);
        Assert.Equal(prepared.Admission, admitted.Admission);
        Assert.Equal(bytes, File.ReadAllBytes(files.Log("a")));
        Assert.Equal(CoordinationEmitterMembership.Live, admitted.Membership);
        emitter.End("a");
        Assert.Equal(0, budget.Occupied);
        emitter.Dispose();
    }

    [Fact]
    public void RegisterResult_PreparationUnavailable_FreezesInputAndRejectsMutation()
    {
        using var files = new PendingDirectory();
        Directory.CreateDirectory(files.Root);
        var budget = new CoordinationEmitterBudget();
        var emitter = new SessionCoordinationEmitter(new CoordContractWriter(files.Root, new EpochTime()), budget);
        using var lifetime = new EmitterTestLifetime(emitter);
        var input = new Dictionary<string, string?> { [OtelAttributes.RepoDisplay] = "original" };
        using (new FileStream(Path.Combine(files.Root, ".coordlock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None))
        {
            var unavailable = emitter.RegisterResult("a", input);
            Assert.Equal(CoordinationEmitterOutcome.Unavailable, unavailable.Outcome);
            Assert.Null(unavailable.Prepared);
            Assert.Equal(CoordinationEmitterPhase.AwaitingPreparation, unavailable.Phase);
            input[OtelAttributes.RepoDisplay] = "changed";
            Assert.Equal(CoordinationEmitterCodes.InputConflict, emitter.RegisterResult("a", input).Code);
            Assert.False(File.Exists(files.Log("a")));
        }

        Assert.Equal(CoordinationEmitterOutcome.Admitted, emitter.RetryPending("a").Outcome);
        Assert.Contains("original", File.ReadAllText(files.Log("a")));
        Assert.DoesNotContain("changed", File.ReadAllText(files.Log("a")));
        Assert.Equal(1, emitter.LiveCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Control_PrewriteFailure_PreservesMembershipAndAbandonsOnlyPending(bool end)
    {
        using var files = new PendingDirectory();
        var budget = new CoordinationEmitterBudget();
        var emitter = new SessionCoordinationEmitter(new CoordContractWriter(files.Root), budget);
        using var lifetime = new EmitterTestLifetime(emitter);
        emitter.Register("a", Identity);
        using (new FileStream(files.Log("a"), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var failed = end ? emitter.EndResult("a") : emitter.HeartbeatResult("a");
            Assert.Equal(CoordinationEmitterOutcome.Unavailable, failed.Outcome);
            Assert.Equal(CoordinationEmitterMembership.Live, failed.Membership);
            Assert.Equal(1, budget.Occupied);
            Assert.Equal(CoordinationEmitterCodes.InputConflict,
                (end ? emitter.HeartbeatResult("a") : emitter.EndResult("a")).Code);
            Assert.Equal(CoordinationEmitterOutcome.Abandoned, emitter.TryAbandonPending("a").Outcome);
            Assert.Equal(1, budget.Occupied);
            Assert.Equal(1, emitter.LiveCount);
        }
        Assert.Equal(CoordinationEmitterOutcome.Admitted, emitter.EndResult("a").Outcome);
        Assert.Equal(0, budget.Occupied);
    }

    [Fact]
    public void RegisterResult_ProductionGlobalAcrossRoots_RefusesBeforeFactoryAndReadmitsAfterEnd()
    {
        using var first = new PendingDirectory();
        using var second = new PendingDirectory();
        using var excess = new PendingDirectory();
        var a = new SessionCoordinationEmitter(new CoordContractWriter(first.Root));
        var b = new SessionCoordinationEmitter(new CoordContractWriter(second.Root));
        var c = new SessionCoordinationEmitter(new CoordContractWriter(excess.Root));
        using var aLife = new EmitterTestLifetime(a);
        using var bLife = new EmitterTestLifetime(b);
        using var cLife = new EmitterTestLifetime(c);
        for (var index = 0; index < 64; index++)
        {
            a.Register("a" + index, Identity);
            b.Register("b" + index, Identity);
        }
        var factoryCalls = 0;
        var error = Assert.Throws<CoordinationEmitterException>(() =>
            c.Reconcile(new HashSet<string> { "excess" }, _ => { factoryCalls++; return Identity; }));
        Assert.Equal(CoordinationEmitterCodes.Capacity, error.Result.Code);
        Assert.Equal(0, factoryCalls);
        Assert.False(Directory.Exists(excess.Root));
        Assert.Equal(0, c.RetainedCount);
        a.End("a0");
        c.Reconcile(new HashSet<string> { "excess" }, _ => { factoryCalls++; return Identity; });
        Assert.Equal(1, factoryCalls);
        Assert.Equal(1, c.LiveCount);
    }

    [Fact]
    public async Task RegisterResult_ReservedSlotAndControlFlood_OnlyOneWaiterAndNoQuotaBypass()
    {
        using var files = new PendingDirectory();
        using var excess = new PendingDirectory();
        using var clock = new PausingTime();
        var budget = new CoordinationEmitterBudget();
        var emitter = new SessionCoordinationEmitter(new CoordContractWriter(files.Root, clock), budget);
        var other = new SessionCoordinationEmitter(new CoordContractWriter(excess.Root), budget);
        using var life = new EmitterTestLifetime(emitter);
        using var otherLife = new EmitterTestLifetime(other);
        for (var index = 0; index < 127; index++) emitter.Register("a" + index, Identity);
        clock.Arm();
        var register = Task.Run(() => emitter.RegisterResult("reserved", Identity));
        Task<CoordinationEmitterResult>? end = null;
        try
        {
            Assert.True(clock.Entered.Wait(TimeSpan.FromSeconds(10)));
            Assert.Equal(128, budget.Occupied);
            Assert.Equal(127, emitter.LiveCount);
            Assert.Equal(CoordinationEmitterCodes.Capacity, other.RegisterResult("excess", Identity).Code);
            Assert.False(Directory.Exists(excess.Root));
            end = Task.Run(() => emitter.EndResult("reserved"));
            Assert.True(SpinWait.SpinUntil(() => GateReferences(emitter, "reserved") == 2, TimeSpan.FromSeconds(10)));
            for (var index = 0; index < 200; index++)
                Assert.Equal(CoordinationEmitterOutcome.Busy, emitter.HeartbeatResult("reserved").Outcome);
            Assert.Equal(2, GateReferences(emitter, "reserved"));
            Assert.False(emitter.TryRetire());
        }
        finally
        {
            clock.Release.Set();
            await register.WaitAsync(TimeSpan.FromSeconds(10));
            if (end is not null) await end.WaitAsync(TimeSpan.FromSeconds(10));
        }
        Assert.Equal(127, budget.Occupied);
        Assert.Equal(0, GateReferences(emitter, "reserved"));
    }

    [Fact]
    public async Task HeartbeatAll_PausedClock_HealthyProgressAndCancellationRetainInFlight()
    {
        using var files = new PendingDirectory();
        using var clock = new PausingTime();
        var budget = new CoordinationEmitterBudget();
        var emitter = new SessionCoordinationEmitter(new CoordContractWriter(files.Root, clock), budget);
        using var lifetime = new EmitterTestLifetime(emitter);
        emitter.Register("a", Identity);
        emitter.Register("b", Identity);
        clock.Arm();
        using var cancellation = new CancellationTokenSource();
        var batch = emitter.HeartbeatAllResultsAsync(cancellation.Token);
        try
        {
            Assert.True(clock.Entered.Wait(TimeSpan.FromSeconds(10)));
            Assert.True(SpinWait.SpinUntil(() =>
                new[] { "a", "b" }.Any(id => GateReferences(emitter, id) == 0 &&
                    File.ReadAllLines(files.Log(id)).Length == 2), TimeSpan.FromSeconds(10)));
            cancellation.Cancel();
            var results = await batch.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Single(results, result => result.Outcome == CoordinationEmitterOutcome.InFlight);
            Assert.Single(results, result => result.Outcome == CoordinationEmitterOutcome.Admitted);
            Assert.Equal(2, budget.Occupied);
            Assert.False(emitter.TryRetire());
            Assert.Throws<CoordinationEmitterException>(emitter.Dispose);
            Assert.Equal(CoordinationEmitterOutcome.Busy, (await emitter.HeartbeatAllResultsAsync())[0].Outcome);
        }
        finally
        {
            clock.Release.Set();
            await batch.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(SpinWait.SpinUntil(() => GateReferences(emitter, "a") + GateReferences(emitter, "b") == 0,
                TimeSpan.FromSeconds(10)));
            Assert.True(SpinWait.SpinUntil(() => !BatchActive(emitter), TimeSpan.FromSeconds(10)));
        }
    }

    [Fact]
    public void RegisterResult_InputBound_CountsUtf16AndDoesNotRetainOversize()
    {
        using var files = new PendingDirectory();
        var emitter = new SessionCoordinationEmitter(new CoordContractWriter(files.Root), new CoordinationEmitterBudget());
        using var lifetime = new EmitterTestLifetime(emitter);
        var key = OtelAttributes.RepoDisplay;
        var valid = new Dictionary<string, string?> { [key] = new('A', 65_536 - key.Length - 1) };
        var frozen = CoordinationEmitterState.Freeze("a", valid);
        Assert.Equal(valid[key], frozen[key]);
        valid[key] += "A";
        Assert.Equal(CoordinationEmitterCodes.RetentionBound, emitter.RegisterResult("a", valid).Code);
        Assert.False(Directory.Exists(files.Root));
        Assert.Equal(2, emitter.RetainedDataBytes);
        Assert.Equal(CoordinationEmitterOutcome.Abandoned, emitter.TryAbandonPending("a").Outcome);
        Assert.Equal(0, emitter.RetainedCount);
        Assert.Equal(CoordinationEmitterCodes.RecoveryRequired, emitter.RetryPending("unknown").Code);
        Assert.Equal(CoordinationEmitterOutcome.NoOp, emitter.HeartbeatResult("unknown").Outcome);
        Assert.Equal(CoordinationEmitterOutcome.NoOp, emitter.EndResult("unknown").Outcome);
        Assert.Equal(0, emitter.RetainedCount);
    }

    [Fact]
    public async Task HeartbeatAll_BlockingBridge_DoesNotPostToCallerContext()
    {
        using var files = new PendingDirectory();
        var emitter = new SessionCoordinationEmitter(new CoordContractWriter(files.Root), new CoordinationEmitterBudget());
        using var lifetime = new EmitterTestLifetime(emitter);
        emitter.Register("a", Identity);
        await Task.Run(() =>
        {
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new RejectingContext());
            try { emitter.HeartbeatAll(); }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(2, File.ReadAllLines(files.Log("a")).Length);
    }

    private sealed class RejectingContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state) => throw new InvalidOperationException("Captured caller context");
    }

    [Fact]
    public void RegisterResult_AppendUnavailableWithNullFailurePrepared_RetainsReadyObjectForRetry()
    {
        using var files = new PendingDirectory();
        var emitter = new SessionCoordinationEmitter(new CoordContractWriter(files.Root, new EpochTime()),
            new CoordinationEmitterBudget());
        using var lifetime = new EmitterTestLifetime(emitter);
        FileStream? deny = null;
        var thread = Environment.CurrentManagedThreadId;
        void AfterPrepare(object? sender, System.Diagnostics.ActivityChangedEventArgs args)
        {
            if (deny is null && Environment.CurrentManagedThreadId == thread &&
                args.Previous?.OperationName == "coordination.native.prepare" &&
                args.Current?.OperationName == "coordination.emitter")
                deny = new FileStream(Path.Combine(files.Root, ".coordlock"), FileMode.Open,
                    FileAccess.ReadWrite, FileShare.None);
        }
        CoordinationEmitterResult unavailable;
        System.Diagnostics.Activity.CurrentChanged += AfterPrepare;
        try { unavailable = emitter.RegisterResult("a", Identity); }
        finally { System.Diagnostics.Activity.CurrentChanged -= AfterPrepare; deny?.Dispose(); }
        Assert.NotNull(deny);
        Assert.Equal(CoordinationEmitterOutcome.Unavailable, unavailable.Outcome);
        var original = Assert.IsType<PreparedWrite>(unavailable.Prepared);
        Assert.False(original.Attempted);
        Assert.False(File.Exists(files.Log("a")));
        Assert.True(emitter.RetainedDataBytes > original.Admission.ByteCount);

        var admitted = emitter.RetryPending("a");

        Assert.Equal(original.Admission, admitted.Admission);
        Assert.True(original.Attempted);
        Assert.Equal(original.CopyBytes(), File.ReadAllBytes(files.Log("a")));
        Assert.Equal(2, emitter.RetainedDataBytes);
    }

    [Fact]
    public void RegisterResult_RepairLfExceedsPacketBound_RefusesBeforeAppendAndCanAbandon()
    {
        using var files = new PendingDirectory();
        var writer = new CoordContractWriter(files.Root, new EpochTime());
        writer.WriteHeartbeat("a");
        var baseline = File.ReadAllBytes(files.Log("a"));
        var attrs = new Dictionary<string, string?> { [OtelAttributes.RepoDisplay] = "" };
        var size = writer.Prepare("register", "a", attrs).Prepared!.Admission.ByteCount;
        attrs[OtelAttributes.RepoDisplay] = new string('A', 65_536 - size);
        Assert.Equal(65_536, writer.Prepare("register", "a", attrs).Prepared!.Admission.ByteCount);
        File.WriteAllBytes(files.Log("a"), baseline[..^1]);
        var emitter = new SessionCoordinationEmitter(writer, new CoordinationEmitterBudget());
        using var lifetime = new EmitterTestLifetime(emitter);

        var refused = emitter.RegisterResult("a", attrs);

        Assert.Equal(CoordinationEmitterCodes.RetentionBound, refused.Code);
        Assert.Equal(baseline[..^1], File.ReadAllBytes(files.Log("a")));
        Assert.Equal(2, emitter.RetainedDataBytes);
        Assert.Equal(CoordinationEmitterCodes.RetentionBound, emitter.RetryPending("a").Code);
        Assert.Equal(CoordinationEmitterOutcome.Abandoned, emitter.TryAbandonPending("a").Outcome);
    }

    [Fact]
    public void Retention_MetadataAccounting_IncludesUtf16FingerprintAndPacket()
    {
        var prepared = new PreparedWrite(new string('R', 32_767), new string('F', 32_768),
            "a", 1, 0, new byte[32], new byte[65_536], false);
        var state = new CoordinationEmitterState("a") { Prepared = prepared };
        Assert.Equal(65_536, state.MetadataCodeUnits);
        Assert.Equal(2L * 65_537 + 32 + 65_536, state.RetainedDataBytes);
        state.Prepared = new PreparedWrite(new string('R', 32_768), new string('F', 32_768),
            "a", 1, 0, new byte[32], [], false);
        Assert.True(state.MetadataCodeUnits > CoordinationEmitterState.MaximumCodeUnits);
    }

    private static int GateReferences(SessionCoordinationEmitter emitter, string session)
    {
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var sync = typeof(SessionCoordinationEmitter).GetField("_gate", flags)!.GetValue(emitter)!;
        lock (sync)
        {
            var gates = (System.Collections.IDictionary)typeof(SessionCoordinationEmitter)
                .GetField("_sessionGates", flags)!.GetValue(emitter)!;
            return gates[session] is { } gate ? (int)gate.GetType().GetField("References")!.GetValue(gate)! : 0;
        }
    }

    private static bool BatchActive(SessionCoordinationEmitter emitter)
    {
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var sync = typeof(SessionCoordinationEmitter).GetField("_gate", flags)!.GetValue(emitter)!;
        lock (sync) { return (bool)typeof(SessionCoordinationEmitter).GetField("_batch", flags)!.GetValue(emitter)!; }
    }

    private sealed class EpochTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch;
    }

    private sealed class PausingTime : TimeProvider, IDisposable
    {
        private int _armed;
        internal ManualResetEventSlim Entered { get; } = new();
        internal ManualResetEventSlim Release { get; } = new();
        internal void Arm() => Interlocked.Exchange(ref _armed, 1);
        public override DateTimeOffset GetUtcNow()
        {
            if (Interlocked.Exchange(ref _armed, 0) == 1)
            {
                Entered.Set();
                if (!Release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("synthetic clock");
            }
            return DateTimeOffset.UnixEpoch;
        }
        public void Dispose() { Entered.Dispose(); Release.Dispose(); }
    }
}

internal sealed class EmitterTestLifetime(SessionCoordinationEmitter emitter) : IDisposable
{
    internal SessionCoordinationEmitter Emitter { get; } = emitter;

    public void Dispose()
    {
        foreach (var id in Emitter.OwnedSessionIds)
        {
            var abandoned = Emitter.TryAbandonPending(id);
            if (!abandoned.Succeeded) Assert.True(Emitter.RetryPending(id).Succeeded);
            Assert.True(Emitter.EndResult(id).Succeeded);
        }
        Assert.True(Emitter.TryRetire());
    }
}
