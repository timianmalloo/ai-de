using System.Diagnostics;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using AiDe.Core.Watcher;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Tests.Watcher;

public sealed class CanonicalCoordinationProjectionRuntimeTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("official-runtime-");
    private string Primary => Path.Combine(_root.FullName, "primary");
    private string Source => Path.Combine(Primary, ".agents", "requests.jsonl");
    private string Database => Path.Combine(_root.FullName, "cache.db");
    private CoordinationSourceBinding Binding => new(new(Primary, "Synthetic"), CanonicalCoordinationSourceBinding.Origin);
    private static CanonicalStreamIdentity[] Streams => [new("opaque-repo", "opaque-stream")];
    private SessionRecord Reader => new("synthetic-reader", new(1), new(Binding.Repository,
        new(Binding.Repository, "synthetic", Primary), new("terminal"), new("agent"),
        null, null, TrustClassification.Asserted));

    public CanonicalCoordinationProjectionRuntimeTests()
    {
        Directory.CreateDirectory(Primary);
        Git(Primary, "init", "--quiet");
        Git(Primary, "commit", "--quiet", "--allow-empty", "-m", "synthetic");
        Directory.CreateDirectory(Path.GetDirectoryName(Source)!);
    }

    [Fact]
    public void RunOnce_ActualPrimarySource_ProducesReceiptWithoutNativeEffects()
    {
        var bytes = "{\"kind\":\"request-add\",\"id\":\"logical-request\"}\r\n"u8.ToArray();
        File.WriteAllBytes(Source, bytes);
        using var store = SqliteWatcherObservationStore.Open(Database);

        var result = ProjectFromExistingSource(store);

        Assert.Equal(CoordinationReadStatus.Available, result.Status);
        Assert.Equal("applied", Assert.Single(result.Entries).State);
        Assert.Equal(0, Number("SELECT count(*) FROM board_message_fact"));
        Assert.Equal(0, Number("SELECT count(*) FROM session_heartbeat"));
        Assert.Equal(bytes, File.ReadAllBytes(Source));
    }

    private CoordinationReadResult ProjectFromExistingSource(SqliteWatcherObservationStore store)
    {
        var descriptor = OfficialDescriptorAcquisition.Acquire(Binding, Primary, Primary, Streams);
        var result = CanonicalCoordinationOfficialRuntime.RunOnce(descriptor, store, Reader);
        Assert.Null(result.Stats.Diagnostic);
        return result.Read;
    }

    private OfficialDescriptorAcquisition Acquire(OfficialHashMode mode = OfficialHashMode.Sha256,
        CanonicalStreamIdentity[]? streams = null) =>
        OfficialDescriptorAcquisition.Acquire(Binding, Primary, Primary, streams ?? Streams, mode);

    [Fact]
    public void RunOnce_AbsentThenEmpty_DistinguishesUnavailableWithoutCreatingSource()
    {
        using var store = SqliteWatcherObservationStore.Open(Database);
        var descriptor = Acquire();
        var absent = CanonicalCoordinationOfficialRuntime.RunOnce(descriptor, store, Reader);
        Assert.Equal("Unavailable", absent.SourceStatus);
        Assert.Equal(OfficialCoordinationErrors.Unavailable, absent.Stats.Diagnostic);
        Assert.False(File.Exists(Source));
        Assert.Equal(0, Number("SELECT count(*) FROM coord_projection_checkpoint"));

        File.WriteAllBytes(Source, []);
        var empty = CanonicalCoordinationOfficialRuntime.RunOnce(descriptor, store, Reader);
        Assert.Equal("Empty", empty.SourceStatus);
        Assert.Equal(CoordinationReadStatus.Available, empty.Read.Status);
        Assert.Empty(empty.Read.Entries);
        Assert.Equal(0, empty.Checkpoint!.Offset);
        Assert.Equal(CoordinationSourceCapture.Hash([]), empty.Checkpoint.PrefixDigest);
        Assert.Equal("Denied", empty.Authorization);
        Assert.Equal("OfficialApiUnavailable", empty.ResponseActions);
    }

    [Fact]
    public void RunOnce_IgnoredInvalidLegacyAndTail_AccountsPhysicalBytesWithoutQuestions()
    {
        byte[] bytes = [.. "\n \t\r\n{\"kind\":\"unknown\",\"trusted\":true}\n"u8.ToArray(),
            255, 10, .. "{\"kind\":\"coordination-v9\",\"schemaVersion\":9}\n"u8.ToArray(),
            .. Legacy("request-add"), .. Legacy("request-resolve"), 0xE2, 0x82];
        File.WriteAllBytes(Source, bytes);
        using var store = SqliteWatcherObservationStore.Open(Database);
        var native = NativeSnapshot();

        var result = CanonicalCoordinationOfficialRuntime.RunOnce(Acquire(), store, Reader);

        Assert.Equal("DeferredTail", result.SourceStatus);
        Assert.Equal(7, result.Stats.Records);
        Assert.Equal(bytes.Length - 2, result.Checkpoint!.Offset);
        Assert.Equal(3, result.Admissions.Count(a => a.InterpretationReason == OfficialCoordinationErrors.Ignored));
        Assert.Equal(2, result.Admissions.Count(a => a.State == "applied"));
        Assert.Equal(5, Number("SELECT count(*) FROM coord_projection_event WHERE official_canonical_bytes IS NULL"));
        Assert.Equal(2, Number("SELECT count(*) FROM coord_projection_event WHERE substr(official_identity,1,4)=x'58484C31'"));
        Assert.Equal(5, Number("SELECT count(*) FROM coord_projection_event WHERE substr(official_identity,1,4)=x'58484931'"));
        Assert.Equal(7, result.Read.Entries.Count);
        Assert.All(result.Read.Entries, e => Assert.Null(e.SessionId));
        Assert.Equal(native, NativeSnapshot());
    }

    [Fact]
    public void ProjectOfficialPage_RecordedAtEqualAndSemanticConflict_PreservesOriginalBytesAndReceipt()
    {
        var original = Response(1);
        File.WriteAllBytes(Source, [.. original, 10]);
        using var store = SqliteWatcherObservationStore.Open(Database);
        var descriptor = Acquire();
        var first = CanonicalCoordinationOfficialRuntime.RunOnce(descriptor, store, Reader);
        var originalRow = Rows("SELECT * FROM coord_projection_event");
        Append([.. Response(1, recordedAt: 2), 10, .. Response(1, "different", 3), 10]);

        var next = CanonicalCoordinationOfficialRuntime.RunOnce(descriptor, store, Reader);
        var capture = OfficialCapturedPage.Capture(descriptor);
        var replay = store.ProjectOfficialPage(descriptor, Assert.Single(capture.Pages), null);

        Assert.Null(next.Stats.Diagnostic);
        Assert.Equal(CoordinationOccurrenceKind.Equal, next.Admissions[0].OccurrenceKind);
        Assert.Equal(CoordinationOccurrenceKind.Conflict, next.Admissions[1].OccurrenceKind);
        Assert.Equal("XH.EVENT_CONFLICT", next.Admissions[1].InterpretationReason);
        Assert.All(next.Admissions, a => Assert.Equal(first.Admissions[0].Admission, a.Admission));
        Assert.All(replay.Admissions, a => Assert.True(a.Replayed));
        Assert.Equal(originalRow, Rows("SELECT * FROM coord_projection_event"));
        Assert.Equal(3, Number("SELECT count(*) FROM coord_projection_feed"));
        Assert.Equal(3, replay.Admissions.Count);
        Assert.Equal(CoordinationOccurrenceKind.Conflict, next.Read.Entries[2].OccurrenceKind);
    }

    [Fact]
    public void RunOnce_InvalidDigestAndSpoofedStream_RetainsRawWithoutEnhancedIdentity()
    {
        var invalid = JsonNode.Parse(Response(1))!;
        invalid["payloadDigest"] = new string('0', 64);
        var spoofed = JsonNode.Parse(Response(2))!;
        spoofed["streamId"] = "not-allowed";
        File.WriteAllBytes(Source, [.. Encoding.UTF8.GetBytes(invalid.ToJsonString()), 10, .. Seal(spoofed), 10]);
        using var store = SqliteWatcherObservationStore.Open(Database);

        var result = CanonicalCoordinationOfficialRuntime.RunOnce(Acquire(), store, Reader);

        Assert.Equal(CanonicalErrors.Digest, result.Admissions[0].InterpretationReason);
        Assert.Equal(CoordinationBindingErrors.Mismatch, result.Admissions[1].InterpretationReason);
        Assert.Equal(2, Number("SELECT count(*) FROM coord_projection_event WHERE official_canonical_bytes IS NULL"));
        Assert.Equal(0, Number("SELECT count(*) FROM coord_projection_event WHERE substr(official_identity,1,4)=x'58484531'"));
        Assert.All(result.Read.Entries, e => Assert.NotNull(e.ReasonCode));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Capture_MaximumContent_KeepsExactFramingAndLegacyContent(string ending)
    {
        var raw = MaximumLegacy(65536, ending);
        File.WriteAllBytes(Source, raw);
        using var store = SqliteWatcherObservationStore.Open(Database);

        var result = CanonicalCoordinationOfficialRuntime.RunOnce(Acquire(), store, Reader);

        Assert.Null(result.Stats.Diagnostic);
        Assert.Equal(raw.Length, result.Checkpoint!.Offset);
        Assert.Equal(raw, Blob("SELECT official_raw_bytes FROM coord_projection_event"));
        Assert.Equal(raw[..65536], Blob("SELECT official_canonical_bytes FROM coord_projection_event"));
        Assert.Equal(0, Number("SELECT count(*) FROM coord_projection_event WHERE raw_bytes IS NOT NULL"));
    }

    [Fact]
    public void Capture_SixtyFourMaximumCrLfFrames_SplitsAtSixtyThreeAndMakesProgress()
    {
        var raw = MaximumLegacy(65536, "\r\n");
        File.WriteAllBytes(Source, Enumerable.Range(0, 64).SelectMany(_ => raw).ToArray());
        var descriptor = Acquire();
        var capture = OfficialCapturedPage.Capture(descriptor);
        Assert.Equal(2, capture.Pages.Count);
        Assert.Equal(63, capture.Pages[0].Frames().Count());
        Assert.Single(capture.Pages[1].Frames());
        Assert.All(capture.Pages, p => Assert.InRange(p.End - p.Offset, 1, OfficialCapturedPage.MaxPageBytes));
        using var store = SqliteWatcherObservationStore.Open(Database);
        var result = CanonicalCoordinationOfficialRuntime.RunOnce(descriptor, store, Reader);
        Assert.Equal(64, result.Admissions.Count);
        Assert.Equal(64L * 65538, result.Checkpoint!.Offset);
    }

    [Fact]
    public void Capture_OverContentAndCaptureBounds_PreservesAcceptedHistory()
    {
        File.WriteAllBytes(Source, Legacy("request-add"));
        using var store = SqliteWatcherObservationStore.Open(Database);
        var descriptor = Acquire();
        var accepted = CanonicalCoordinationOfficialRuntime.RunOnce(descriptor, store, Reader);
        var before = CacheSnapshot();
        Append(MaximumLegacy(65537, "\n"));
        Assert.Equal(CoordinationErrors.Bounds, Assert.Throws<CoordinationSourceException>(
            () => OfficialCapturedPage.Capture(descriptor, accepted.Checkpoint)).Code);
        using (var file = new FileStream(Source, FileMode.Open, FileAccess.Write))
            file.SetLength(OfficialCapturedPage.MaxCaptureBytes + 1L);
        Assert.Equal(CoordinationErrors.Bounds, Assert.Throws<CoordinationSourceException>(
            () => OfficialCapturedPage.Capture(descriptor, accepted.Checkpoint)).Code);
        Assert.Equal(before, CacheSnapshot());
    }

    [Fact]
    public void Capture_IncompleteUtf8Tail_DefersUntilLfWithoutCheckpointingTail()
    {
        File.WriteAllBytes(Source, [0xE2, 0x82]);
        using var store = SqliteWatcherObservationStore.Open(Database);
        var descriptor = Acquire();
        var first = CanonicalCoordinationOfficialRuntime.RunOnce(descriptor, store, Reader);
        Assert.Equal(0, first.Checkpoint!.Offset);
        Assert.Empty(first.Admissions);
        Append([0xAC, 10]);
        var second = CanonicalCoordinationOfficialRuntime.RunOnce(descriptor, store, Reader);
        Assert.Equal(4, second.Checkpoint!.Offset);
        Assert.Equal("refused", Assert.Single(second.Admissions).State);
        Assert.Null(NullableBlob("SELECT official_canonical_bytes FROM coord_projection_event"));
    }

    [Theory]
    [InlineData("changed")]
    [InlineData("truncated")]
    [InlineData("missing")]
    public void Capture_AcceptedSourceChanges_ReturnsGapAndRetainsCheckpoint(string change)
    {
        File.WriteAllBytes(Source, Legacy("request-add"));
        using var store = SqliteWatcherObservationStore.Open(Database);
        var descriptor = Acquire();
        var first = CanonicalCoordinationOfficialRuntime.RunOnce(descriptor, store, Reader);
        var before = CacheSnapshot();
        if (change == "missing") File.Delete(Source);
        else File.WriteAllBytes(Source, change == "truncated" ? [] : Legacy("request-end"));

        Assert.Equal(CoordinationErrors.SourceGap, Assert.Throws<CoordinationSourceException>(
            () => OfficialCapturedPage.Capture(descriptor, first.Checkpoint)).Code);
        Assert.Equal(before, CacheSnapshot());
    }

    [Fact]
    public void ProjectOfficialPage_ChangedStaleEmptySnapshot_RejectsEvenWhenOffsetAlreadyApplied()
    {
        File.WriteAllBytes(Source, Legacy("request-add"));
        using var store = SqliteWatcherObservationStore.Open(Database);
        var descriptor = Acquire();
        var accepted = CanonicalCoordinationOfficialRuntime.RunOnce(descriptor, store, Reader);
        var before = CacheSnapshot();
        File.WriteAllBytes(Source, Legacy("request-end"));
        var forgedExpected = accepted.Checkpoint! with { PrefixDigest = CoordinationSourceCapture.Hash(File.ReadAllBytes(Source)) };
        var page = Assert.Single(OfficialCapturedPage.Capture(descriptor, forgedExpected).Pages);

        Assert.Equal(CoordinationErrors.StaleSnapshot, Assert.Throws<CoordinationSourceException>(
            () => store.ProjectOfficialPage(descriptor, page, forgedExpected)).Code);
        Assert.Equal(before, CacheSnapshot());
    }

    [Fact]
    public void Acquire_LinkedCheckoutAndReorderedOpaquePairs_UsesOneFullDescriptor()
    {
        var linked = Path.Combine(_root.FullName, "linked");
        Git(Primary, "worktree", "add", "--quiet", "--detach", linked, "HEAD");
        CanonicalStreamIdentity[] pairs = [new("\uE000", "s"), new("\U00010000", "s")];
        var primary = Acquire(streams: pairs);
        var second = OfficialDescriptorAcquisition.Acquire(Binding, Primary, linked, pairs.Reverse().ToArray());
        Assert.Equal(primary.SourceId, second.SourceId);
        Assert.Equal(primary.Descriptor.ToArray(), second.Descriptor.ToArray());
        Assert.Equal(Source, second.SourcePath);
        Assert.InRange(primary.Descriptor.Length, 5, OfficialDescriptorAcquisition.MaxDescriptorBytes);
        Assert.Equal(64, primary.SourceId.Length);
        Assert.InRange(Encoding.UTF8.GetByteCount(primary.Scope), 1, 4096);
        Assert.Empty(typeof(OfficialDescriptorAcquisition).GetConstructors());
        Assert.Empty(typeof(OfficialCapturedPage).GetConstructors());
    }

    [Fact]
    public void Acquire_CallerRepositorySpoof_RejectsBeforeEmptyOrMalformedSource()
    {
        File.WriteAllBytes(Source, [255]);
        var wrong = new CoordinationSourceBinding(new(_root.FullName, "same-name"), CanonicalCoordinationSourceBinding.Origin);
        Assert.Equal(CoordinationBindingErrors.Mismatch, Assert.Throws<CoordinationSourceException>(() =>
            OfficialDescriptorAcquisition.Acquire(wrong, Primary, Primary, Streams)).Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    public void Acquire_TooManyOrNoAllowances_RefusesBoundedDescriptor(int count)
    {
        var pairs = Enumerable.Range(0, count).Select(i => new CanonicalStreamIdentity("r", i.ToString())).ToArray();
        Assert.Equal(OfficialCoordinationErrors.Descriptor, Assert.Throws<CoordinationSourceException>(
            () => Acquire(streams: pairs)).Code);
    }

    [Fact]
    public void Acquire_FullDescriptorByteLimit_RejectsBoundedButOversizedPairs()
    {
        var field = string.Concat(Enumerable.Repeat("\U00010000", 511));
        var pairs = Enumerable.Range(0, 16).Select(i => new CanonicalStreamIdentity(field + (char)('a' + i), field)).ToArray();
        Assert.Equal(OfficialCoordinationErrors.Descriptor, Assert.Throws<CoordinationSourceException>(
            () => Acquire(streams: pairs)).Code);
    }

    [Fact]
    public void ProjectOfficialPage_FullDescriptorCollision_RefusesRebindingBeforeAdmission()
    {
        File.WriteAllBytes(Source, Legacy("request-add"));
        using var store = SqliteWatcherObservationStore.Open(Database);
        var original = Acquire(OfficialHashMode.Collision);
        CanonicalCoordinationOfficialRuntime.RunOnce(original, store, Reader);
        var before = CacheSnapshot();
        var different = Acquire(OfficialHashMode.Collision, [new("different", "allowance")]);
        Assert.Equal(original.SourceId, different.SourceId);
        var page = Assert.Single(OfficialCapturedPage.Capture(different).Pages);

        Assert.Equal(OfficialCoordinationErrors.Rebind, Assert.Throws<CoordinationSourceException>(
            () => store.ProjectOfficialPage(different, page, null)).Code);
        Assert.Equal(before, CacheSnapshot());
    }

    [Fact]
    public void ProjectOfficialPage_DifferentScopeSamePhysicalSource_RefusesReconfiguration()
    {
        File.WriteAllBytes(Source, Legacy("request-add"));
        using var store = SqliteWatcherObservationStore.Open(Database);
        var original = Acquire();
        CanonicalCoordinationOfficialRuntime.RunOnce(original, store, Reader);
        var before = CacheSnapshot();
        var changed = Acquire(streams: [.. Streams, new("another", "stream")]);
        Assert.NotEqual(original.Scope, changed.Scope);
        Assert.Equal(OfficialCoordinationErrors.Rebind, Assert.Throws<CoordinationSourceException>(
            () => store.ReadOfficialCheckpoint(changed)).Code);
        Assert.Equal(before, CacheSnapshot());
    }

    [Fact]
    public void ProjectOfficialPage_CompactKeyCollision_ComparesFullIdentityAndRollsBackWholePage()
    {
        File.WriteAllBytes(Source, [.. Response(1), 10, .. Response(2), 10]);
        using var store = SqliteWatcherObservationStore.Open(Database);
        store.OfficialEventHashMode = OfficialHashMode.Collision;
        var descriptor = Acquire();
        var page = Assert.Single(OfficialCapturedPage.Capture(descriptor).Pages);
        var before = CacheSnapshot();
        Assert.Equal(OfficialCoordinationErrors.KeyCollision, Assert.Throws<CoordinationSourceException>(
            () => store.ProjectOfficialPage(descriptor, page, null)).Code);
        Assert.Equal(before, CacheSnapshot());
    }

    [Fact]
    public void ProjectOfficialPage_NativeUnboundCheckpoint_CannotRetrobind()
    {
        File.WriteAllBytes(Source, []);
        using var store = SqliteWatcherObservationStore.Open(Database);
        var descriptor = Acquire();
        Execute("INSERT INTO coord_projection_checkpoint(scope,epoch,accepted_offset,prefix_digest) VALUES($scope,'native',0,$empty)",
            ("$scope", descriptor.Scope), ("$empty", CoordinationSourceCapture.Hash([])));
        var before = CacheSnapshot();
        var page = Assert.Single(OfficialCapturedPage.Capture(descriptor).Pages);
        Assert.Equal(OfficialCoordinationErrors.Rebind, Assert.Throws<CoordinationSourceException>(
            () => store.ProjectOfficialPage(descriptor, page, null)).Code);
        Assert.Equal(before, CacheSnapshot());
    }

    [Theory]
    [InlineData("scope")]
    [InlineData("epoch")]
    [InlineData("offset")]
    [InlineData("prefix")]
    public void ProjectOfficialPage_ForgedExpectedCheckpoint_RejectsWithoutPartialRows(string field)
    {
        File.WriteAllBytes(Source, Legacy("request-add"));
        using var store = SqliteWatcherObservationStore.Open(Database);
        var descriptor = Acquire();
        var page = Assert.Single(OfficialCapturedPage.Capture(descriptor).Pages);
        var zero = OfficialCheckpoint.Empty(descriptor);
        var expected = field switch
        {
            "scope" => zero with { Scope = "other" },
            "epoch" => zero with { Epoch = "other" },
            "offset" => zero with { Offset = 1 },
            _ => zero with { PrefixDigest = new string('0', 64) },
        };
        Assert.Equal(CoordinationErrors.StaleSnapshot, Assert.Throws<CoordinationSourceException>(
            () => store.ProjectOfficialPage(descriptor, page, expected)).Code);
        Assert.Equal(0, Number("SELECT count(*) FROM coord_projection_checkpoint"));
        Assert.Equal(0, Number("SELECT count(*) FROM coord_projection_event"));
        Assert.Equal(0, Number("SELECT count(*) FROM coord_projection_feed"));
    }

    [Fact]
    public void ProjectOfficialPage_BeforeCommitFailure_RollsBackCheckpointEventAndReceipt()
    {
        File.WriteAllBytes(Source, [.. Response(1), 10]);
        using var store = SqliteWatcherObservationStore.Open(Database);
        var descriptor = Acquire();
        var page = Assert.Single(OfficialCapturedPage.Capture(descriptor).Pages);
        var before = CacheSnapshot();
        var native = NativeSnapshot();
        store.ProjectionFault = CoordinationFault.BeforeCommit;
        Assert.Equal("COORD_FAULT_BeforeCommit", Assert.Throws<IOException>(
            () => store.ProjectOfficialPage(descriptor, page, null)).Message);
        Assert.Equal(before, CacheSnapshot());
        Assert.Equal(native, NativeSnapshot());
        Assert.Equal(1, Assert.Single(store.ProjectOfficialPage(descriptor, page, null).Admissions).Admission);
    }

    [Fact]
    public void ProjectOfficialPage_LostAcknowledgementThenRestart_ReplaysOriginalAdmissionAndState()
    {
        File.WriteAllBytes(Source, [.. Response(1), 10]);
        var descriptor = Acquire();
        var page = Assert.Single(OfficialCapturedPage.Capture(descriptor).Pages);
        using (var store = SqliteWatcherObservationStore.Open(Database))
        {
            store.ProjectionFault = CoordinationFault.AfterCommit;
            Assert.Equal("COORD_FAULT_AfterCommit", Assert.Throws<IOException>(
                () => store.ProjectOfficialPage(descriptor, page, null)).Message);
        }
        var before = CacheSnapshot();
        using var reopened = SqliteWatcherObservationStore.Open(Database);
        var result = reopened.ProjectOfficialPage(descriptor, page, null);
        var admission = Assert.Single(result.Admissions);
        Assert.Equal(1, admission.Admission);
        Assert.Equal(1, admission.CurrentReceipt);
        Assert.Equal("applied", admission.State);
        Assert.True(admission.Replayed);
        Assert.Equal(before, CacheSnapshot());
    }

    [Fact]
    public async Task ProjectOfficialPage_TwoStoresCompete_OneAdmissionBothReceiveOriginalReceipt()
    {
        File.WriteAllBytes(Source, [.. Response(1), 10]);
        using var first = SqliteWatcherObservationStore.Open(Database);
        using var second = SqliteWatcherObservationStore.Open(Database);
        var descriptor = Acquire();
        var page = Assert.Single(OfficialCapturedPage.Capture(descriptor).Pages);

        var results = await Task.WhenAll(Task.Run(() => first.ProjectOfficialPage(descriptor, page, null)),
            Task.Run(() => second.ProjectOfficialPage(descriptor, page, null)));

        Assert.All(results, r => Assert.Equal(1, Assert.Single(r.Admissions).Admission));
        Assert.Equal(1, Number("SELECT count(*) FROM coord_projection_event"));
        Assert.Equal(1, Number("SELECT count(*) FROM coord_projection_feed"));
        Assert.Equal(page.End, Number("SELECT accepted_offset FROM coord_projection_checkpoint"));
    }

    [Fact]
    public void ReadCoordination_FourHundredOneWithInterleavedScope_UsesFrozen2002001ThenFreshResume()
    {
        File.WriteAllBytes(Source, Enumerable.Range(0, 401).SelectMany(i => Legacy("request-add", "id-" + i)).ToArray());
        using var store = SqliteWatcherObservationStore.Open(Database);
        var descriptor = Acquire();
        var capture = OfficialCapturedPage.Capture(descriptor);
        Assert.Equal(new[] { 128, 128, 128, 17 }, capture.Pages.Select(p => p.Frames().Count()));
        var checkpoint = store.ProjectOfficialPage(descriptor, capture.Pages[0], null).Checkpoint;
        using var other = new CanonicalCoordinationProjectionRuntimeTests();
        File.WriteAllBytes(other.Source, Legacy("request-add"));
        var interleaved = CanonicalCoordinationOfficialRuntime.RunOnce(other.Acquire(), store, other.Reader);
        Assert.Single(interleaved.Admissions);
        foreach (var page in capture.Pages.Skip(1)) checkpoint = store.ProjectOfficialPage(descriptor, page, checkpoint).Checkpoint;
        var first = store.ReadCoordination(new(descriptor.SourceId, null, 200, Reader));
        var second = store.ReadCoordination(new(descriptor.SourceId, first.Continuation, 200, Reader));
        var third = store.ReadCoordination(new(descriptor.SourceId, second.Continuation, 200, Reader));
        Assert.Equal(new[] { 200, 200, 1 }, new[] { first.Entries.Count, second.Entries.Count, third.Entries.Count });
        Assert.Equal(402, third.SnapshotHighWater);
        Assert.Equal(401, first.Entries.Concat(second.Entries).Concat(third.Entries).Select(e => e.N).Distinct().Count());
        Append(Legacy("request-add", "late"));
        var later = CanonicalCoordinationOfficialRuntime.RunOnce(descriptor, store, Reader);
        Assert.Single(later.Admissions);
        var frozen = store.ReadCoordination(new(descriptor.SourceId, second.Continuation, 200, Reader));
        Assert.Single(frozen.Entries);
        var fresh = store.ReadCoordination(new(descriptor.SourceId, third.FreshResume, 200, Reader));
        Assert.Equal(403, Assert.Single(fresh.Entries).N);
        Assert.Equal(CoordinationReadStatus.Mismatch,
            store.ReadCoordination(new(descriptor.SourceId, null, 200, other.Reader)).Status);
    }

    [Fact]
    public void RunOnce_RebuildSameCorpus_ReproducesExactReceiptsWatermarkAndBytes()
    {
        File.WriteAllBytes(Source, [.. Response(1), 10, .. Response(1, recordedAt: 3), 10,
            .. Response(1, "conflict"), 10, .. Legacy("request-add"), .. "\n"u8.ToArray()]);
        var descriptor = Acquire();
        using var first = SqliteWatcherObservationStore.Open(Database);
        var result = CanonicalCoordinationOfficialRuntime.RunOnce(descriptor, first, Reader);
        var rebuiltPath = Database + ".rebuilt";
        using var rebuilt = SqliteWatcherObservationStore.Open(rebuiltPath);
        var again = CanonicalCoordinationOfficialRuntime.RunOnce(descriptor, rebuilt, Reader);
        Assert.Equal(result.Checkpoint, again.Checkpoint);
        Assert.Equal(JsonSerializer.Serialize(result.Read), JsonSerializer.Serialize(again.Read));
        Assert.Equal(Rows("SELECT * FROM coord_projection_event"), Rows("SELECT * FROM coord_projection_event", rebuiltPath));
        Assert.Equal(Rows("SELECT * FROM coord_projection_feed"), Rows("SELECT * FROM coord_projection_feed", rebuiltPath));
        Assert.Equal(5, again.Checkpoint is null ? 0 : again.Admissions.Count);
    }

    [Fact]
    public void RunOnce_ExpandedInvalidResponse_RetainsRawButNeverCanonicalMasquerade()
    {
        var body = JsonNode.Parse(Response(1))!;
        body["payload"]!["numbers"] = new JsonArray(Enumerable.Range(0, 6000)
            .Select(_ => (JsonNode?)JsonValue.Create(100.0)).ToArray());
        var content = Encoding.UTF8.GetBytes(body.ToJsonString().Replace("100", "1e2", StringComparison.Ordinal));
        File.WriteAllBytes(Source, [.. content, 10]);
        using var store = SqliteWatcherObservationStore.Open(Database);
        var result = CanonicalCoordinationOfficialRuntime.RunOnce(Acquire(), store, Reader);
        Assert.Equal(CanonicalErrors.Schema, Assert.Single(result.Admissions).InterpretationReason);
        Assert.Null(NullableBlob("SELECT official_canonical_bytes FROM coord_projection_event"));
        Assert.Equal(content.Length + 1, result.Checkpoint!.Offset);
    }

    [Fact]
    public void RunOnce_NormalPath_EmitsMeasuredCountsAndDurationWithoutPayloadTags()
    {
        File.WriteAllBytes(Source, Legacy("request-add"));
        using var store = SqliteWatcherObservationStore.Open(Database);
        Activity? observed = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => { if (activity.OperationName == "coordination.official.project") observed = activity; },
        };
        ActivitySource.AddActivityListener(listener);
        var result = CanonicalCoordinationOfficialRuntime.RunOnce(Acquire(), store, Reader);
        Assert.Equal(1, result.Stats.Records);
        Assert.Equal(File.ReadAllBytes(Source).Length, result.Stats.Bytes);
        Assert.InRange(result.Stats.ElapsedMilliseconds, 0, double.MaxValue);
        Assert.NotNull(observed);
        Assert.Equal(1, observed.GetTagItem("coordination.project.records"));
        Assert.Equal(result.Stats.ElapsedMilliseconds, observed.GetTagItem("coordination.project.elapsed_ms"));
        Assert.DoesNotContain(observed.TagObjects, t => t.Value is string text && text.Contains("logical-request"));
    }

    private static byte[] Legacy(string kind, string id = "logical-request") =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { kind, id }) + "\n");

    private static byte[] MaximumLegacy(int contentLength, string ending)
    {
        var content = Encoding.UTF8.GetString(Legacy("request-add")).TrimEnd('\n');
        return Encoding.UTF8.GetBytes(content + new string(' ', contentLength - Encoding.UTF8.GetByteCount(content)) + ending);
    }

    private static byte[] Response(int id, string text = "synthetic", double recordedAt = 1)
    {
        var body = JsonNode.Parse(CanonicalCoordinationRecordTests.Example())!;
        body["repositoryId"] = "opaque-repo";
        body["streamId"] = "opaque-stream";
        body["eventId"] = "event-" + id;
        body["recordedAt"] = recordedAt;
        body["payload"]!["text"] = text;
        var bytes = Seal(body);
        Assert.Equal("OK", CanonicalCoordinationRecord.Parse(bytes).Code);
        return bytes;
    }

    private static byte[] Seal(JsonNode body)
    {
        var bytes = Encoding.UTF8.GetBytes(body.ToJsonString());
        body["payloadDigest"] = Convert.ToHexStringLower(SHA256.HashData(CanonicalCoordinationCodec.Bytes(bytes)));
        return Encoding.UTF8.GetBytes(body.ToJsonString());
    }

    private void Append(byte[] bytes)
    {
        using var stream = new FileStream(Source, FileMode.Append, FileAccess.Write);
        stream.Write(bytes);
    }

    private string CacheSnapshot() => string.Concat(new[] { "event", "feed", "checkpoint" }
        .Select(table => Rows("SELECT * FROM coord_projection_" + table + " ORDER BY rowid")));

    private string NativeSnapshot()
    {
        using var connection = new SqliteConnection($"Data Source={Database};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'coord_projection_%' AND name NOT LIKE 'sqlite_%' ORDER BY name";
        var tables = new List<string>();
        using (var reader = command.ExecuteReader()) while (reader.Read()) tables.Add(reader.GetString(0));
        return string.Concat(tables.Select(t => Rows("SELECT * FROM \"" + t.Replace("\"", "\"\"") + "\" ORDER BY rowid")));
    }

    private string Rows(string sql, string? path = null)
    {
        using var connection = new SqliteConnection($"Data Source={path ?? Database};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var rows = new List<object[]>();
        while (reader.Read())
        {
            var row = new object[reader.FieldCount];
            reader.GetValues(row);
            rows.Add(row);
        }
        return JsonSerializer.Serialize(rows);
    }

    private byte[] Blob(string sql) => Assert.IsType<byte[]>(NullableBlob(sql));
    private byte[]? NullableBlob(string sql)
    {
        using var connection = new SqliteConnection($"Data Source={Database};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar() as byte[];
    }

    private void Execute(string sql, params (string Name, object Value)[] parameters)
    {
        using var connection = new SqliteConnection($"Data Source={Database};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        command.ExecuteNonQuery();
    }

    private long Number(string sql)
    {
        using var connection = new SqliteConnection($"Data Source={Database};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static string Git(string directory, params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = directory,
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var key in start.Environment.Keys.Where(k => k.StartsWith("GIT_", StringComparison.Ordinal)).ToArray())
            start.Environment.Remove(key);
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        start.Environment["GIT_AUTHOR_NAME"] = start.Environment["GIT_COMMITTER_NAME"] = "Synthetic";
        start.Environment["GIT_AUTHOR_EMAIL"] = start.Environment["GIT_COMMITTER_EMAIL"] = "synthetic@example.invalid";
        start.Environment["GIT_AUTHOR_DATE"] = start.Environment["GIT_COMMITTER_DATE"] = "2000-01-01T00:00:00Z";
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(10000), "Synthetic Git timed out");
        Assert.Equal(0, process.ExitCode);
        Assert.Equal("", stderr);
        return stdout.Trim();
    }

    public void Dispose()
    {
        foreach (var file in _root.EnumerateFiles("*", new EnumerationOptions
                 { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint }))
            file.Attributes &= ~FileAttributes.ReadOnly;
        _root.Delete(recursive: true);
    }
}
