using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiDe.Core.Watcher;
using Xunit.Abstractions;

namespace AiDe.Core.Tests.Watcher;

/// <summary>Synthetic local files; injected faults call real FileStream before losing acknowledgement.</summary>
public sealed class CoordinationPreparedWriteTests(ITestOutputHelper output)
{
    [Fact]
    public void Prepare_CallerMutatesAttributesAndClock_ExactFrozenBytesAreAppended()
    {
        using var files = new Files();
        var attributes = new Dictionary<string, string?> { ["payload"] = "before" };
        var clock = new MutableTime();
        var writer = new CoordContractWriter(files.Root, clock);
        var prepared = Ready(writer.Prepare("update", "subject", attributes));
        attributes["payload"] = "after";
        clock.Now = DateTimeOffset.UnixEpoch.AddDays(1);
        var expected = prepared.CopyBytes();
        var exposed = prepared.CopyBytes();
        exposed[0] = 0;
        Assert.False(File.Exists(files.Log()));

        var result = writer.Append(prepared);

        Assert.Equal(CoordinationWriteStatus.Admitted, result.Status);
        Assert.Equal(expected, File.ReadAllBytes(files.Log()));
        Assert.Contains("before", File.ReadAllText(files.Log()));
        using var json = JsonDocument.Parse(File.ReadAllText(files.Log()));
        Assert.Equal(0, json.RootElement.GetProperty("at").GetDouble());
        output.WriteLine($"preparedBytes={expected.Length}; admittedBytes={new FileInfo(files.Log()).Length}; seq={result.Admission!.Sequence}");
    }

    [Fact]
    public void Append_IdenticalNeverAttemptedPreparation_CannotBorrowAdmission()
    {
        using var files = new Files();
        var a = files.Writer();
        var b = files.Writer();
        var first = Ready(a.Prepare("heartbeat", "subject"));
        var second = Ready(b.Prepare("heartbeat", "subject"));
        Assert.Equal(first.CopyBytes(), second.CopyBytes());
        Assert.Equal(CoordinationWriteStatus.Admitted, a.Append(first).Status);
        var before = File.ReadAllBytes(files.Log());

        var stale = b.Append(second);
        var replay = b.Append(first);

        Assert.Equal(CoordinationWriteStatus.Refused, stale.Status);
        Assert.Equal(CoordinationWriteCodes.StalePreparation, stale.Code);
        Assert.Equal(CoordinationWriteStatus.Admitted, replay.Status);
        Assert.Equal(first.Admission, replay.Admission);
        Assert.Equal(before, File.ReadAllBytes(files.Log()));
        output.WriteLine($"stale={stale.Code}; replaySeq={replay.Admission!.Sequence}; bytesAfter={before.Length}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(19)]
    public void Append_PrefixWrittenThenIOException_RetriesOnlyMissingExactSuffix(int prefix)
    {
        using var files = new Files();
        var writer = new CoordContractWriter(files.Root, new Epoch())
        {
            WriteFault = (stream, bytes) =>
            {
                stream.Write(bytes.Span[..prefix]);
                stream.Flush(true);
                throw new IOException("synthetic prefix loss");
            },
        };
        var prepared = Ready(writer.Prepare("heartbeat", "subject"));

        var uncertain = writer.Append(prepared);
        Assert.Equal(CoordinationWriteStatus.Uncertain, uncertain.Status);
        Assert.Same(prepared, uncertain.Prepared);
        Assert.Equal(prepared.CopyBytes()[..prefix], File.ReadAllBytes(files.Log()));
        var recovered = files.Writer().Append(prepared);

        Assert.Equal(CoordinationWriteStatus.Admitted, recovered.Status);
        Assert.Equal(prepared.Admission, recovered.Admission);
        Assert.Equal(prepared.CopyBytes(), File.ReadAllBytes(files.Log()));
        Assert.Single(CoordContractLog.ReadDirectory(files.Root));
        output.WriteLine($"partial={prefix}; uncertain={uncertain.Code}; recoveredLength={new FileInfo(files.Log()).Length}; seq={recovered.Admission!.Sequence}");
    }

    [Theory]
    [InlineData("flush")]
    [InlineData("dispose")]
    public void Append_FullBytesButLostDurabilityAcknowledgement_ReconcilesSameAdmission(string fault)
    {
        using var files = new Files();
        var writer = new CoordContractWriter(files.Root, new Epoch())
        {
            FlushFault = fault == "flush" ? _ => throw new IOException("synthetic flush loss") : null,
            DisposeFault = fault == "dispose" ? () => throw new IOException("synthetic dispose loss") : null,
        };
        var prepared = Ready(writer.Prepare("heartbeat", "subject"));

        var uncertain = writer.Append(prepared);
        Assert.Equal(CoordinationWriteStatus.Uncertain, uncertain.Status);
        var before = File.ReadAllBytes(files.Log());
        var recovered = files.Writer().Append(prepared);

        Assert.Equal(prepared.CopyBytes(), before);
        Assert.Equal(CoordinationWriteStatus.Admitted, recovered.Status);
        Assert.Equal(prepared.Admission, recovered.Admission);
        Assert.Equal(before, File.ReadAllBytes(files.Log()));
        output.WriteLine($"fault={fault}; uncertain={uncertain.Code}; exactBytes={before.Length}; replaySeq={recovered.Admission!.Sequence}");
    }

    [Theory]
    [InlineData("different")]
    [InlineData("extra")]
    [InlineData("truncate")]
    public void Append_SourceChangedAfterPartialWrite_RemainsUncertainWithoutFurtherAppend(string mutation)
    {
        using var files = new Files();
        files.Writer().WriteHeartbeat("subject");
        var writer = new CoordContractWriter(files.Root, new Epoch())
        {
            WriteFault = (stream, bytes) =>
            {
                stream.Write(bytes.Span[..10]);
                throw new IOException("synthetic partial");
            },
        };
        var prepared = Ready(writer.Prepare("heartbeat", "subject"));
        Assert.Equal(CoordinationWriteStatus.Uncertain, writer.Append(prepared).Status);
        var bytes = File.ReadAllBytes(files.Log());
        switch (mutation)
        {
            case "different": bytes[^1] = (byte)'!'; break;
            case "extra": bytes = [.. bytes, (byte)'!']; break;
            case "truncate": bytes = bytes[..((int)prepared.Admission.Start - 1)]; break;
        }
        File.WriteAllBytes(files.Log(), bytes);

        var result = files.Writer().Append(prepared);

        Assert.Equal(CoordinationWriteStatus.Uncertain, result.Status);
        Assert.Equal(CoordinationWriteCodes.WriteUncertain, result.Code);
        Assert.Equal(bytes, File.ReadAllBytes(files.Log()));
        output.WriteLine($"mutation={mutation}; outcome={result.Status}; preservedBytes={bytes.Length}");
    }

    [Fact]
    public void Append_UnresolvedFlushThenOutage_PreservesPreparationUntilRecovery()
    {
        using var files = new Files();
        var writer = new CoordContractWriter(files.Root, new Epoch())
        {
            FlushFault = _ => throw new IOException("synthetic flush loss"),
        };
        var prepared = Ready(writer.Prepare("heartbeat", "subject"));
        Assert.Equal(CoordinationWriteStatus.Uncertain, writer.Append(prepared).Status);
        using (var denial = new FileStream(files.Log(), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var unavailable = files.Writer().Append(prepared);
            Assert.Equal(CoordinationWriteStatus.Uncertain, unavailable.Status);
            Assert.Same(prepared, unavailable.Prepared);
        }

        var recovered = files.Writer().Append(prepared);

        Assert.Equal(CoordinationWriteStatus.Admitted, recovered.Status);
        Assert.Equal(prepared.CopyBytes(), File.ReadAllBytes(files.Log()));
    }

    [Fact]
    public void Append_QuotaFilledSincePreparation_RefusesWithoutCreatingFile129()
    {
        using var files = new Files();
        var prepared = Ready(files.Writer().Prepare("heartbeat", "subject"));
        for (var index = 0; index < CoordContractWriter.MaximumFiles; index++)
            File.WriteAllBytes(files.Log("seed-" + index), []);

        var result = files.Writer().Append(prepared);

        Assert.Equal(CoordinationWriteCodes.FileBound, result.Code);
        Assert.Equal(128, Directory.GetFiles(files.Root, "*.jsonl").Length);
        Assert.False(File.Exists(files.Log()));
        output.WriteLine($"result={result.Code}; jsonlFiles=128; subjectExists=false");
    }

    [Fact]
    public void Prepare_ExternalRootOverrun_PreservesHistoryAndReportsUnavailable()
    {
        using var files = new Files();
        using (var stream = File.Create(files.Log("external")))
            stream.SetLength(CoordContractWriter.MaximumRootBytes + 1);
        var before = Hash(files.Log("external"));

        var result = files.Writer().Prepare("heartbeat", "subject");

        Assert.Equal(CoordinationWriteStatus.Unavailable, result.Status);
        Assert.Equal(CoordinationWriteCodes.RootOverrun, result.Code);
        Assert.Equal(before, Hash(files.Log("external")));
        Assert.False(File.Exists(files.Log()));
    }

    [Fact]
    public void Prepare_CompleteTailRepairAtRootLimit_CountsSeparatorAndPreservesUnknownTail()
    {
        using var files = new Files();
        files.Writer().WriteHeartbeat("subject");
        var raw = File.ReadAllText(files.Log()).TrimEnd('\n');
        File.WriteAllText(files.Log(), raw);
        var prepared = Ready(files.Writer().Prepare("heartbeat", "subject"));
        Assert.Equal((byte)'\n', prepared.CopyBytes()[0]);
        using (var padding = File.Create(files.Log("padding")))
            padding.SetLength(CoordContractWriter.MaximumRootBytes - Encoding.UTF8.GetByteCount(raw)
                - prepared.Admission.ByteCount + 1);

        var result = files.Writer().Append(prepared);

        Assert.Equal(CoordinationWriteCodes.RootBound, result.Code);
        Assert.Equal(raw, File.ReadAllText(files.Log()));
        File.WriteAllText(files.Log(), raw + "\n{\"session\":");
        var unknown = files.Writer().Prepare("heartbeat", "subject");
        Assert.Equal(CoordinationWriteStatus.Unavailable, unknown.Status);
        Assert.Equal(CoordinationWriteCodes.SourceUnavailable, unknown.Code);
    }

    [Fact]
    public void Prepare_UnsafeFilenameCollidesWithSafeId_RefusesExistingContentIdentity()
    {
        using var files = new Files();
        var unsafeId = "synthetic" + Path.GetInvalidFileNameChars()[0] + "id";
        files.Writer().WriteHeartbeat(unsafeId);
        var filename = CoordContractWriter.FileNameFor(unsafeId);
        var alias = Path.GetFileNameWithoutExtension(filename);
        var before = File.ReadAllBytes(Path.Combine(files.Root, filename));

        var result = files.Writer().Prepare("heartbeat", alias);

        Assert.Equal(CoordinationWriteCodes.IdentityConflict, result.Code);
        Assert.Equal(before, File.ReadAllBytes(Path.Combine(files.Root, filename)));
    }

    [Fact]
    public void Prepare_ExistingSequenceWouldDuplicate_RefusesRatherThanRenumbering()
    {
        using var files = new Files();
        File.WriteAllText(files.Log(), "{\"session\":\"subject\",\"seq\":2}\n");

        var result = files.Writer().Prepare("heartbeat", "subject");

        Assert.Equal(CoordinationWriteCodes.SequenceConflict, result.Code);
        Assert.Equal("{\"session\":\"subject\",\"seq\":2}\n", File.ReadAllText(files.Log()));
    }

    [Fact]
    public async Task Append_HeldThroughFlush_NormalizedAliasWriterFailsFastAndGateReclaims()
    {
        using var files = new Files();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var writer = new CoordContractWriter(files.Root, new Epoch())
        {
            FlushFault = stream =>
            {
                entered.Set();
                if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
                stream.Flush(true);
            },
        };
        var prepared = Ready(writer.Prepare("heartbeat", "subject"));
        var active = Task.Run(() => writer.Append(prepared));
        CoordinationWriteResult? busy = null;
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
            var alias = Path.Combine(files.Root, ".") + Path.DirectorySeparatorChar;
            busy = new CoordContractWriter(alias, new Epoch()).Prepare("heartbeat", "other");
            Assert.Equal(CoordinationWriteStatus.Unavailable, busy.Status);
            Assert.Equal(CoordinationWriteCodes.WriterBusy, busy.Code);
            Assert.Throws<IOException>(() => new FileStream(
                Path.Combine(files.Root, CoordContractWriter.RootLockFile),
                FileMode.Open, FileAccess.ReadWrite, FileShare.None));
        }
        finally { release.Set(); await active.WaitAsync(TimeSpan.FromSeconds(10)); }

        Assert.Equal(CoordinationWriteStatus.Admitted, (await active).Status);
        files.Writer().WriteHeartbeat("other");
        var table = (System.Collections.IDictionary)typeof(CoordContractWriter)
            .GetField("RootGates", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(null)!;
        var sync = typeof(CoordContractWriter).GetField("RootTableLock",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!;
        lock (sync) Assert.False(table.Contains(CoordinationSourceCapture.RootKey(files.Root)));
        Assert.Equal(2, CoordContractLog.ReadDirectory(files.Root).Count);
        output.WriteLine($"aliasBusy={busy!.Code}; completed=2; reclaimedRoot=true");
    }

    [Fact]
    public void PrepareAndAppend_ExternalOsRootLockBusy_ReclaimsLocalEntryAndPreservesHolder()
    {
        using var files = new Files();
        var writer = files.Writer();
        var seed = Ready(writer.Prepare("heartbeat", "subject"));
        Assert.Equal(CoordinationWriteStatus.Admitted, writer.Append(seed).Status);
        var pending = Ready(writer.Prepare("heartbeat", "subject"));
        var before = File.ReadAllBytes(files.Log());
        var lockPath = Path.Combine(files.Root, CoordContractWriter.RootLockFile);
        var table = (System.Collections.IDictionary)typeof(CoordContractWriter)
            .GetField("RootGates", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(null)!;
        var sync = typeof(CoordContractWriter).GetField("RootTableLock",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!;
        var key = CoordinationSourceCapture.RootKey(files.Root);
        lock (sync) Assert.False(table.Contains(key));

        // Independent OS handle, not a borrowed writer lease or the in-process root monitor.
        using (var holder = new FileStream(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var originalHandle = holder.SafeFileHandle;
            for (var repeat = 0; repeat < 3; repeat++)
            {
                AssertBusyAndReclaimed(writer.Prepare("heartbeat", "subject"), "Prepare");
                AssertBusyAndReclaimed(writer.Append(pending), "Append");
            }

            void AssertBusyAndReclaimed(CoordinationWriteResult result, string operation)
            {
                Assert.Equal(CoordinationWriteStatus.Unavailable, result.Status);
                Assert.Equal(CoordinationWriteCodes.WriterBusy, result.Code);
                Assert.Equal(before, File.ReadAllBytes(files.Log()));
                Assert.Same(originalHandle, holder.SafeFileHandle);
                Assert.False(originalHandle.IsClosed);
                Assert.False(originalHandle.IsInvalid);
                Assert.Equal(-1, holder.ReadByte());
                holder.Flush(true);
                var denial = Assert.Throws<IOException>(() =>
                {
                    using var contender = new FileStream(lockPath, FileMode.Open,
                        FileAccess.ReadWrite, FileShare.None);
                });
                Assert.Contains(denial.HResult & 0xffff, new[] { 32, 33, 11 });
                output.WriteLine($"operation={operation}; busy={result.Code}; acceptedBytes={before.Length}; originalOsHolderValid=true; competingOsOpenDenied=true");
                lock (sync)
                    Assert.False(table.Contains(key), "retained local root entry after OS-open failure");
            }
        }

        Assert.Equal(CoordinationWriteStatus.Admitted, writer.Append(pending).Status);
        var next = Ready(writer.Prepare("heartbeat", "subject"));
        Assert.Equal(CoordinationWriteStatus.Admitted, writer.Append(next).Status);
        Assert.Equal(before.Concat(pending.CopyBytes()).Concat(next.CopyBytes()).ToArray(),
            File.ReadAllBytes(files.Log()));
        lock (sync) Assert.False(table.Contains(key));
        Assert.Equal(3, CoordContractLog.ReadDirectory(files.Root).Count);
        output.WriteLine("deniedPrepare=3; deniedAppend=3; holderReleased=true; pendingAdmitted=true; freshPrepareAppendAdmitted=true; events=3; reclaimedRoot=true");
    }

    [Fact]
    public void Prepare_RecordRefusalAndAdmission_EmitMeasuredOutcomeWithoutPayload()
    {
        using var files = new Files();
        using var scope = new Activity("synthetic-writer-test").Start();
        var recorded = new List<(string Operation, string? Outcome, object? Duration, string? Code)>();
        void Stopped(Activity activity)
        {
            if (activity.ParentId == scope.Id &&
                activity.OperationName.StartsWith("coordination.native.", StringComparison.Ordinal))
                recorded.Add((activity.OperationName, activity.GetTagItem("coordination.outcome")?.ToString(),
                    activity.GetTagItem("coordination.duration_ms"), activity.GetTagItem("error.type")?.ToString()));
        }
        // Activity.CurrentChanged exposes completed Activities without requiring an exporter.
        void Changed(object? sender, ActivityChangedEventArgs args)
        {
            if (args.Previous is { Duration: var elapsed } previous && elapsed > TimeSpan.Zero) Stopped(previous);
        }
        Activity.CurrentChanged += Changed;
        try
        {
            files.Writer().WriteHeartbeat("subject");
            var refused = files.Writer().Prepare("update", "subject",
                new Dictionary<string, string?> { ["payload"] = new string('A', 65_537) });
            Assert.Equal(CoordinationWriteCodes.RecordBound, refused.Code);
        }
        finally { Activity.CurrentChanged -= Changed; }

        Assert.Contains(recorded, item => item.Outcome == "Admitted" && item.Duration is double);
        Assert.Contains(recorded, item => item.Code == CoordinationWriteCodes.RecordBound);
        output.WriteLine(string.Join("; ", recorded.Select(item => $"{item.Operation}:{item.Outcome}:{item.Duration}:{item.Code}")));
    }

    private static PreparedWrite Ready(CoordinationWriteResult result)
    {
        Assert.Equal(CoordinationWriteStatus.Ready, result.Status);
        return Assert.IsType<PreparedWrite>(result.Prepared);
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Prepare_LegacyNonemptyLinesAndGappedSequence_PreservesNativeCount(string newline)
    {
        using var files = new Files();
        File.WriteAllText(files.Log(), "{\"session\":\"subject\",\"seq\":99}" + newline + " " + newline
            + "{\"session\":\"subject\",\"seq\":1}" + newline);

        var prepared = Ready(files.Writer().Prepare("heartbeat", "subject"));

        Assert.Equal(3, prepared.Admission.Sequence);
        Assert.Equal(CoordinationWriteStatus.Admitted, files.Writer().Append(prepared).Status);
    }

    [Fact]
    public void Prepare_WindowsCaseAlias_RefusesDifferentExactSession()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var files = new Files();
        files.Writer().WriteHeartbeat("Subject");
        var original = File.ReadAllBytes(files.Log("Subject"));

        var result = files.Writer().Prepare("heartbeat", "subject");

        Assert.Equal(CoordinationWriteCodes.IdentityConflict, result.Code);
        Assert.Equal(original, File.ReadAllBytes(files.Log("Subject")));
    }

    [Fact]
    public void Write_LegacyVoidFailure_ContainsExactUncertainPreparation()
    {
        using var files = new Files();
        var writer = new CoordContractWriter(files.Root, new Epoch())
        {
            FlushFault = _ => throw new IOException("synthetic failed flush"),
        };

        var error = Assert.Throws<CoordinationWriteException>(() => writer.WriteHeartbeat("subject"));

        Assert.Equal(CoordinationWriteCodes.WriteUncertain, error.Code);
        var prepared = Assert.IsType<PreparedWrite>(error.Prepared);
        Assert.Equal(prepared.CopyBytes(), File.ReadAllBytes(files.Log()));
        Assert.Equal(CoordinationWriteStatus.Admitted, files.Writer().Append(prepared).Status);
    }

    [Theory]
    [InlineData("quota")]
    [InlineData("bytes")]
    [InlineData("sequence")]
    public async Task Append_TwoParticipatingProcesses_OnlyOneCanClaimFinalCapacityOrSequence(string race)
    {
        using var files = new Files();
        if (race == "quota")
        {
            for (var index = 0; index < 127; index++)
                File.WriteAllBytes(files.Log("seed-" + index), []);
        }
        if (race == "bytes")
        {
            var size = Ready(files.Writer().Prepare("heartbeat", "subject")).Admission.ByteCount;
            using var padding = File.Create(files.Log("padding"));
            padding.SetLength(CoordContractWriter.MaximumRootBytes - size);
        }
        using var first = StartProbe(files.Root, "subject");
        Process? second = null;
        try
        {
            Assert.Equal("Ready", await ReadStatus(first));
            second = StartProbe(files.Root, race == "sequence" ? "subject" : "other");
            Assert.Equal("Ready", await ReadStatus(second));
            await first.StandardInput.WriteLineAsync("append");
            await second.StandardInput.WriteLineAsync("append");
            var results = await Task.WhenAll(ReadStatus(first), ReadStatus(second));
            await Task.WhenAll(first.WaitForExitAsync(), second.WaitForExitAsync())
                .WaitAsync(TimeSpan.FromSeconds(15));

            Assert.Equal(0, first.ExitCode);
            Assert.Equal(0, second.ExitCode);
            Assert.Single(results, status => status == "Admitted");
            Assert.Single(results, status => status is "Refused" or "Unavailable");
            Assert.Equal(race switch { "quota" => 128, "bytes" => 2, _ => 1 },
                Directory.GetFiles(files.Root, "*.jsonl").Length);
            var rootBytes = Directory.GetFiles(files.Root, "*.jsonl").Sum(path => new FileInfo(path).Length);
            Assert.InRange(rootBytes, 1, CoordContractWriter.MaximumRootBytes);
            var item = Assert.Single(CoordContractLog.ReadDirectory(files.Root));
            Assert.Equal(1, item.Seq);
            output.WriteLine($"race={race}; first={results[0]}; second={results[1]}; files={Directory.GetFiles(files.Root, "*.jsonl").Length}; rootBytes={rootBytes}; events=1; sequence=1");
        }
        finally
        {
            StopProbe(first);
            if (second is not null) { StopProbe(second); second.Dispose(); }
        }
    }

    private static Process StartProbe(string root, string session)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src", "AiDe.Core")))
            directory = directory.Parent;
        var repository = directory?.FullName ?? throw new InvalidOperationException("Fixture repository missing");
        var probe = Path.Combine(repository, "tests", "AiDe.Core.NativeWriterProbe",
            "bin", "Debug", "net10.0", "AiDe.Core.NativeWriterProbe.dll");
        Assert.True(File.Exists(probe), "Build the isolated native writer probe before this test.");
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, RedirectStandardInput = true,
            RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
        };
        start.ArgumentList.Add(probe);
        start.ArgumentList.Add(root);
        start.ArgumentList.Add(session);
        return Process.Start(start) ?? throw new InvalidOperationException("Probe did not start");
    }

    private static async Task<string> ReadStatus(Process process)
    {
        var line = await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15));
        using var json = JsonDocument.Parse(line ?? throw new InvalidOperationException("Probe emitted no result"));
        return json.RootElement.GetProperty("status").GetString()!;
    }

    private static void StopProbe(Process process)
    {
        if (!process.HasExited) process.Kill(entireProcessTree: true);
        if (!process.WaitForExit(10_000)) throw new TimeoutException("Probe cleanup did not complete");
    }

    private static byte[] Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return SHA256.HashData(stream);
    }

    private sealed class Epoch : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch;
    }

    private sealed class MutableTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Files : IDisposable
    {
        public string Root { get; } = Path.Combine(AppContext.BaseDirectory,
            "prepared-writer-fixtures", Guid.NewGuid().ToString("N"));
        public Files() => Directory.CreateDirectory(Root);
        public string Log(string session = "subject") => Path.Combine(Root, session + ".jsonl");
        public CoordContractWriter Writer() => new(Root, new Epoch());
        public void Dispose() => Directory.Delete(Root, true);
    }
}
