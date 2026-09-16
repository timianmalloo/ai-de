using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AiDe.Core.Watcher;
using AiDe.Mcp;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Tests.Watcher;

public sealed class CoordinationFeedTests
{
    [Theory]
    [InlineData("board-post", 1)]
    [InlineData("board-post", 2)]
    [InlineData("update", 1)]
    [InlineData("update", 2)]
    [InlineData("heartbeat", 1)]
    [InlineData("heartbeat", 2)]
    [InlineData("session-end", 1)]
    [InlineData("session-end", 2)]
    public void Pump_ResolvedSessionRepositoryDrifts_RefusesWithoutNativeEffects(string kind, int generation)
    {
        using var fixture = new FeedFixture();
        fixture.Register();
        fixture.Pump();
        var registered = Assert.Single(fixture.Store.AllSessions());
        var foreign = registered with
        {
            Generation = new SessionGeneration(generation),
            Binding = registered.Binding with { Repository = new RepositoryIdentity("foreign-repository", "foreign") },
        };
        fixture.Store.RecordSession(foreign);
        var persisted = Assert.Single(fixture.Store.AllSessions());
        Assert.Equal(foreign.Binding.Repository, persisted.Binding.Repository);
        Assert.Equal(foreign.Generation, persisted.Generation);
        var heartbeat = fixture.Number("SELECT monotonic_ticks FROM session_heartbeat;");
        var offset = fixture.Number("SELECT accepted_offset FROM coord_projection_checkpoint;");
        AppendObservation(fixture, kind);

        var error = Assert.Throws<CoordinationSourceException>(() => fixture.Pump());

        Assert.Equal(CoordinationBindingErrors.Mismatch, error.Message);
        Assert.Equal(persisted, Assert.Single(fixture.Store.AllSessions()));
        Assert.Equal(heartbeat, fixture.Number("SELECT monotonic_ticks FROM session_heartbeat;"));
        Assert.Equal(0, fixture.Number("SELECT COUNT(*) FROM session_ended;"));
        Assert.Equal(0, fixture.Number("SELECT COUNT(*) FROM board_message_fact;"));
        Assert.Equal(1, fixture.Number("SELECT COUNT(*) FROM coord_projection_event;"));
        Assert.Equal(offset, fixture.Number("SELECT accepted_offset FROM coord_projection_checkpoint;"));
        var feed = fixture.Read();
        Assert.Equal(CoordinationReadStatus.Available, feed.Status);
        Assert.Equal(new[] { "pending", "applied" }, feed.Entries.Select(entry => entry.Outcome));
        Assert.Equal("OBSERVED_REGISTER", feed.Entries[1].ReasonCode);
        Assert.Equal(1, feed.Entries[1].SessionGeneration);
    }

    [Theory]
    [InlineData("board-post", "OBSERVED_BOARD")]
    [InlineData("update", "OBSERVED_UPDATE")]
    [InlineData("heartbeat", "OBSERVED_HEARTBEAT")]
    [InlineData("session-end", "OBSERVED_END")]
    public void Pump_ResolvedSameRepositoryNewGeneration_AppliesWithoutInventingGenerationAuthority(string kind, string reason)
    {
        using var fixture = new FeedFixture();
        fixture.Register();
        fixture.Pump();
        var registered = Assert.Single(fixture.Store.AllSessions());
        fixture.Store.RecordSession(registered with { Generation = new SessionGeneration(2) });
        AppendObservation(fixture, kind);

        fixture.Pump();

        var applied = fixture.Read().Entries.Last();
        Assert.Equal("applied", applied.Outcome);
        Assert.Equal(reason, applied.ReasonCode);
        Assert.Equal(2, applied.SessionGeneration);
        Assert.Equal(registered.Binding.Repository, Assert.Single(fixture.Store.AllSessions()).Binding.Repository);
    }

    [Fact]
    public void Pump_LegacyUnboundResolvedSession_RemainsCompatibleButNotPubliclyReadable()
    {
        using var fixture = new FeedFixture();
        fixture.Register();
        new CoordContractLogPump(fixture.Logs, fixture.Ingest).PumpOnce();
        var registered = Assert.Single(fixture.Store.AllSessions());
        fixture.Store.RecordSession(registered with
        {
            Binding = registered.Binding with { Repository = new RepositoryIdentity("foreign-repository", "foreign") },
        });
        AppendObservation(fixture, "board-post");

        new CoordContractLogPump(fixture.Logs, fixture.Ingest).PumpOnce();

        Assert.Equal(1, fixture.Number("SELECT COUNT(*) FROM board_message_fact WHERE repository_key='foreign-repository';"));
        Assert.Equal(CoordinationReadStatus.Unavailable, fixture.Read().Status);
    }

    [Theory]
    [InlineData("7")]
    [InlineData("[]")]
    [InlineData("\"private-marker\"")]
    public void Mcp_MalformedArgumentsContainer_ReturnsTypedInvalidRequest(string arguments)
    {
        var response = Tools.Call(new()
        {
            ["name"] = "aide_coordination_read",
            ["arguments"] = JsonNode.Parse(arguments),
        }, ServerContext.None("synthetic fixture"));

        var body = JsonNode.Parse(response["content"]![0]!["text"]!.GetValue<string>())!;
        Assert.Equal("InvalidRequest", body["Status"]!.GetValue<string>());
        Assert.Equal("COORD_READ_InvalidRequest", body["Code"]!.GetValue<string>());
        Assert.DoesNotContain("private-marker", response.ToJsonString());
    }

    [Theory]
    [InlineData("""{"name":"aide_coordination_read","arguments":7}""")]
    [InlineData("""{"name":"aide_coordination_read","arguments":[]}""")]
    [InlineData("""{"name":"aide_coordination_read","arguments":"private-marker"}""")]
    [InlineData("""{"name":7,"arguments":{}}""")]
    [InlineData("""{"name":[],"arguments":{}}""")]
    [InlineData("""{"name":null,"arguments":{}}""")]
    [InlineData("""{"arguments":{}}""")]
    public void Mcp_OuterRequestMalformedToolEnvelope_ReturnsCorrelatedNegativeResponse(string parameters)
    {
        var envelope = RouteRequest(parameters);

        var body = JsonNode.Parse(envelope["result"]!["content"]![0]!["text"]!.GetValue<string>())!;
        Assert.Equal("InvalidRequest", body["Status"]!.GetValue<string>());
        Assert.Equal("COORD_READ_InvalidRequest", body["Code"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("7")]
    [InlineData("[]")]
    [InlineData("\"private-marker\"")]
    public void Mcp_OuterRequestMalformedParameters_ReturnsCorrelatedRpcError(string parameters)
    {
        var envelope = RouteRequest(parameters);

        Assert.Equal(-32602, envelope["error"]!["code"]!.GetValue<int>());
        Assert.Null(envelope["result"]);
    }

    private static JsonNode RouteRequest(string parameters)
    {
        // Exercise the real outer request router, not a second test-only envelope builder.
        var handle = typeof(AiDe.Mcp.Program).GetMethod("Handle",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var request = new JsonObject
        {
            ["jsonrpc"] = "2.0", ["id"] = "request-17", ["method"] = "tools/call",
            ["params"] = JsonNode.Parse(parameters),
        };

        var response = Assert.IsType<string>(handle.Invoke(null,
            [request.ToJsonString(), ServerContext.None("synthetic fixture")]));

        var envelope = JsonNode.Parse(response)!;
        Assert.Equal("2.0", envelope["jsonrpc"]!.GetValue<string>());
        Assert.Equal("request-17", envelope["id"]!.GetValue<string>());
        Assert.DoesNotContain("private-marker", response);
        return envelope;
    }

    private static void AppendObservation(FeedFixture fixture, string kind) =>
        File.AppendAllText(fixture.FilePath, JsonSerializer.Serialize(new
        {
            kind, contract = CoordContract.Version, session = "session", at = 0, seq = 900,
            attrs = new Dictionary<string, string>
            {
                [CoordContract.BoardAttributes.Kind] = "question",
                [CoordContract.BoardAttributes.Content] = "private-marker",
                [OtelAttributes.GenAiModel] = "changed-model",
            },
        }) + "\n");

    [Fact]
    public void Read_Interleaved401Receipts_Freezes2002001ThenResumesNewTombstone()
    {
        using var fixture = new FeedFixture();
        fixture.Register();
        fixture.Post(99);
        fixture.Pump();
        fixture.Writer.WriteBoardPost("other", "question", "private-marker");
        fixture.Pump();
        fixture.Post(100);
        var duplicate = File.ReadLines(fixture.FilePath).Last();
        File.AppendAllText(fixture.FilePath, duplicate + "\n");
        fixture.Pump();

        var first = fixture.Read();
        var second = fixture.Read(first.Continuation);
        var third = fixture.Read(second.Continuation);
        var entries = first.Entries.Concat(second.Entries).Concat(third.Entries).ToArray();

        Assert.Equal(new[] { 200, 200, 1 }, new[] { first.Entries.Count, second.Entries.Count, third.Entries.Count });
        Assert.Equal(CoordinationReadStatus.Available, first.Status);
        Assert.Equal(401, entries.Select(e => e.N).Distinct().Count());
        Assert.Equal(402, first.SnapshotHighWater);
        Assert.Equal(first.SnapshotHighWater, third.SnapshotHighWater);
        Assert.Null(third.Continuation);
        Assert.Equal("duplicate-occurrence-accounted", Assert.Single(third.Entries).Outcome);
        Assert.Equal(entries.OrderBy(e => e.N), entries);
        Assert.Contains(entries.Zip(entries.Skip(1)), pair => pair.Second.N - pair.First.N > 1);
        Assert.Equal(200, entries.Count(e => e.Outcome == "pending"));
        Assert.All(entries.Where(e => e.Outcome == "applied"), e => Assert.NotNull(e.SessionId));
        Assert.Equal(199, entries.Count(e => e.MessageId is not null));

        fixture.Tombstone();
        var frozen = fixture.Read(second.Continuation);
        Assert.Equal(third.Entries, frozen.Entries);
        var resumed = fixture.Read(third.FreshResume);
        Assert.Equal("tombstone", Assert.Single(resumed.Entries).Outcome);
        Assert.Equal(entries.Last(e => e.Outcome == "applied").AdmissionN, resumed.Entries[0].AdmissionN);
        Assert.Null(resumed.FreshResume!.FrozenHighWater);
        var unchanged = fixture.Read(resumed.FreshResume);
        Assert.Empty(unchanged.Entries);
        Assert.Equal(resumed.LastReturnedN, unchanged.LastReturnedN);
        Assert.Equal("NotRecorded", unchanged.Recovery);
        Assert.Equal("NotRecorded", unchanged.SourceHealth);
        Assert.Equal("NotRecorded", unchanged.Lag);
        var serialized = JsonSerializer.Serialize(first);
        Assert.DoesNotContain("private-marker", serialized);
        Assert.DoesNotContain(fixture.Scope, serialized);
        Assert.DoesNotContain(fixture.Reader.Binding.Repository.CanonicalPath, serialized);
        Assert.DoesNotContain("private-root", serialized);
    }

    [Theory]
    [InlineData(1, CoordinationReadStatus.Available)]
    [InlineData(200, CoordinationReadStatus.Available)]
    [InlineData(201, CoordinationReadStatus.InvalidRequest)]
    [InlineData(0, CoordinationReadStatus.InvalidRequest)]
    [InlineData(-1, CoordinationReadStatus.InvalidRequest)]
    public void Read_EmptyBound_LimitAndAbsenceAreDistinct(int limit, CoordinationReadStatus expected)
    {
        using var fixture = new FeedFixture();
        Assert.Equal(CoordinationReadStatus.Unavailable, fixture.Read().Status);
        fixture.Pump();

        var result = fixture.Read(limit: limit);

        Assert.Equal(expected, result.Status);
        Assert.Empty(result.Entries);
        if (expected == CoordinationReadStatus.Available)
        {
            Assert.Equal(0, result.SnapshotHighWater);
            Assert.Equal(0, result.LastReturnedN);
            Assert.Null(result.Continuation);
            Assert.NotNull(result.FreshResume);
        }
        var wrong = fixture.Reader with
        {
            Binding = fixture.Reader.Binding with { Repository = new RepositoryIdentity("other-repo", "other") },
        };
        Assert.Equal(CoordinationReadStatus.Mismatch, BoardTools.ReadCoordination(fixture.Store, wrong, fixture.SourceId).Status);
        Assert.Equal(CoordinationReadStatus.Unsupported,
            BoardTools.ReadCoordination(new InMemoryWatcherObservationStore(), fixture.Reader, fixture.SourceId).Status);
    }

    [Theory]
    [InlineData("wrong", "legacy-native-1", 0, null, CoordinationReadStatus.Reset)]
    [InlineData("coordination-read/1", "wrong", 0, null, CoordinationReadStatus.Reset)]
    [InlineData("coordination-read/1", "legacy-native-1", -1, null, CoordinationReadStatus.InvalidRequest)]
    [InlineData("coordination-read/1", "legacy-native-1", 1, 0L, CoordinationReadStatus.InvalidRequest)]
    [InlineData("coordination-read/1", "legacy-native-1", 0, -1L, CoordinationReadStatus.InvalidRequest)]
    [InlineData("coordination-read/1", "legacy-native-1", 0, 9999L, CoordinationReadStatus.InvalidRequest)]
    public void Read_InvalidCursor_RejectsTyped(
        string version, string epoch, long after, long? high, CoordinationReadStatus expected)
    {
        using var fixture = new FeedFixture();
        fixture.Pump();

        var result = fixture.Read(new(fixture.SourceId, version, epoch, after, high));

        Assert.Equal(expected, result.Status);
        Assert.Empty(result.Entries);
        Assert.Null(result.SourceOrigin);
    }

    [Fact]
    public void Read_OtherSourceReceiptOrToken_IsNotASnapshotOfThisSource()
    {
        using var fixture = new FeedFixture();
        fixture.Post(1);
        fixture.Pump();
        fixture.Writer.WriteBoardPost("other", "question", "private-marker");
        fixture.Pump();
        fixture.Post(1);
        fixture.Pump();

        Assert.Equal(CoordinationReadStatus.InvalidRequest,
            fixture.Read(new(fixture.SourceId, CoordinationCursor.Version, CoordinationSourceCapture.Epoch, 0, 2)).Status);
        Assert.Equal(CoordinationReadStatus.InvalidRequest,
            fixture.Read(new("other", CoordinationCursor.Version, CoordinationSourceCapture.Epoch, 0, null)).Status);
    }

    [Fact]
    public void Binding_LegacyOrContradictorySource_IsNeverBackfilledOrApplied()
    {
        using var legacy = new FeedFixture();
        legacy.Register();
        new CoordContractLogPump(legacy.Logs, legacy.Ingest).PumpOnce();
        var before = legacy.Number("SELECT COUNT(*) FROM coord_projection_feed;");
        Assert.Equal(CoordinationReadStatus.Unavailable, legacy.Read().Status);
        Assert.Throws<CoordinationSourceException>(() => legacy.Pump());
        Assert.Equal(before, legacy.Number("SELECT COUNT(*) FROM coord_projection_feed;"));
        Assert.Equal(1, legacy.Number("SELECT COUNT(*) FROM coord_projection_checkpoint WHERE public_source_id IS NULL;"));

        using var conflict = new FeedFixture();
        conflict.Register();
        var bad = new CoordinationSourceBinding(new RepositoryIdentity("wrong-repo", "wrong"), "trusted-native");
        Assert.Throws<CoordinationSourceException>(() =>
            new CoordContractLogPump(conflict.Logs, conflict.Ingest, bad).PumpOnce());
        Assert.Empty(conflict.Store.AllSessions());
        Assert.Equal(0, conflict.Number("SELECT COUNT(*) FROM coord_projection_feed;"));
        Assert.Equal(0, conflict.Number("SELECT COUNT(*) FROM coord_projection_checkpoint;"));
    }

    [Theory]
    [InlineData("bound_repository_key='other'")]
    [InlineData("source_origin='other'")]
    [InlineData("public_source_id='other'")]
    [InlineData("scope='other'")]
    [InlineData("epoch='other'")]
    public void Binding_ImmutableDimensions_RefuseUpdate(string assignment)
    {
        using var fixture = new FeedFixture();
        fixture.Pump();

        Assert.Throws<SqliteException>(() => fixture.Execute($"UPDATE coord_projection_checkpoint SET {assignment};"));
        Assert.Throws<SqliteException>(() => fixture.Execute("""
            INSERT OR REPLACE INTO coord_projection_checkpoint SELECT * FROM coord_projection_checkpoint;
            """));
        Assert.Equal(CoordinationReadStatus.Available, fixture.Read().Status);
    }

    [Fact]
    public void Binding_UpdateRepositoryContradiction_RefusesWholeCaptureBeforeEffects()
    {
        using var fixture = new FeedFixture();
        fixture.Register();
        File.AppendAllText(fixture.FilePath, JsonSerializer.Serialize(new
        {
            kind = "update", contract = CoordContract.Version, session = "session", at = 0, seq = 2,
            attrs = new Dictionary<string, string> { [OtelAttributes.RepoPath] = "different-repository" },
        }) + "\n");

        Assert.Throws<CoordinationSourceException>(() => fixture.Pump());

        Assert.Empty(fixture.Store.AllSessions());
        Assert.Equal(0, fixture.Number("SELECT COUNT(*) FROM coord_projection_event;"));
        Assert.Equal(0, fixture.Number("SELECT COUNT(*) FROM coord_projection_checkpoint;"));
    }

    [Fact]
    public void Read_MalformedRecord_ReturnsImmutableRefusalWithoutRawContent()
    {
        using var fixture = new FeedFixture();
        File.WriteAllText(fixture.FilePath, "private-marker not-json\n");
        fixture.Pump();

        var result = fixture.Read();

        Assert.Equal(new[] { "pending", "refused" }, result.Entries.Select(e => e.Outcome));
        Assert.Equal("MALFORMED_RECORD", result.Entries[1].ReasonCode);
        Assert.DoesNotContain("private-marker", JsonSerializer.Serialize(result));
        Assert.Equal(CoordinationReadStatus.Unavailable,
            BoardTools.ReadCoordination(fixture.Store, fixture.Reader, new string('0', 64)).Status);
    }

    [Fact]
    public void Mcp_NullCursorOrLimit_RejectsSchemaViolations()
    {
        using var fixture = new FeedFixture();
        fixture.Pump();
        var context = new ServerContext(null, fixture.Database,
            new ResolvedIdentity(fixture.Reader, IdentitySource.Environment, null), null);
        foreach (var key in new[] { "cursor", "limit" })
        {
            var response = Tools.Call(new()
            {
                ["name"] = "aide_coordination_read",
                ["arguments"] = new JsonObject { ["source_id"] = fixture.SourceId, [key] = null },
            }, context);
            Assert.Contains("InvalidRequest", response["content"]![0]!["text"]!.GetValue<string>());
        }
    }

    [Fact]
    public void Binding_PartialOrHashCollision_RefusesInsteadOfMerging()
    {
        using var fixture = new FeedFixture();
        fixture.Pump();

        Assert.Throws<SqliteException>(() => fixture.Execute("""
            INSERT INTO coord_projection_checkpoint(scope,epoch,accepted_offset,prefix_digest,bound_repository_key)
            VALUES('partial','legacy-native-1',0,
            'E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855','repo');
            """));
        Assert.Throws<SqliteException>(() => fixture.Execute("""
            INSERT INTO coord_projection_checkpoint
            SELECT 'collision',epoch,accepted_offset,prefix_digest,bound_repository_key,source_origin,public_source_id
            FROM coord_projection_checkpoint;
            """));
        Assert.Equal(1, fixture.Number("SELECT COUNT(*) FROM coord_projection_checkpoint;"));
    }

    [Theory]
    [InlineData("bound_repository_key='other'")]
    [InlineData("source_origin='other'")]
    [InlineData("public_source_id='other'")]
    public void Binding_ValidOffsetAdvanceCannotHideRebinding(string assignment)
    {
        using var fixture = new FeedFixture();
        fixture.Pump();

        Assert.Throws<SqliteException>(() => fixture.Execute($"""
            BEGIN;
            INSERT INTO coord_projection_feed(scope,epoch,event_key,is_initial,outcome,application_state)
            VALUES('{fixture.Scope}','legacy-native-1','guard',1,'pending','pending');
            INSERT INTO coord_projection_event
            (scope,epoch,event_key,source_offset,source_end,raw_bytes,raw_digest,canonical_version,canonical_bytes,
             first_receipt_n,current_receipt_n,application_state)
            VALUES('{fixture.Scope}','legacy-native-1','guard',0,1,X'0A','digest','loomkeeper/1',X'0A',
                   last_insert_rowid(),last_insert_rowid(),'pending');
            UPDATE coord_projection_checkpoint SET accepted_offset=1,prefix_digest='digest',{assignment};
            COMMIT;
            """));

        Assert.Equal(0, fixture.Number("SELECT COUNT(*) FROM coord_projection_event;"));
        Assert.Equal(CoordinationReadStatus.Available, fixture.Read().Status);
    }

    [Fact]
    public async Task Read_ConcurrentWriterAfterBinding_UsesOneSnapshotForMaximumAndRows()
    {
        using var fixture = new FeedFixture();
        fixture.Register();
        fixture.Post(1);
        fixture.Pump();
        using var snapshot = SqliteWatcherObservationStore.OpenReadOnly(fixture.Database);
        using var bound = new ManualResetEventSlim();
        using var committed = new ManualResetEventSlim();
        snapshot.CoordinationReadAfterBinding = () =>
        {
            bound.Set();
            Assert.True(committed.Wait(TimeSpan.FromSeconds(10)));
        };

        var reading = Task.Run(() => BoardTools.ReadCoordination(snapshot, fixture.Reader, fixture.SourceId));
        try
        {
            Assert.True(bound.Wait(TimeSpan.FromSeconds(10)));
            fixture.Tombstone();
        }
        finally { committed.Set(); }
        var result = await reading.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal(4, result.SnapshotHighWater);
        Assert.Equal(4, result.Entries.Count);
        Assert.Equal("tombstone", Assert.Single(fixture.Read(result.FreshResume).Entries).Outcome);
    }

    [Fact]
    public void Mcp_ActualSchemaAndDispatch_RejectsOverridesAndReturnsOnlyMetadata()
    {
        using var fixture = new FeedFixture();
        fixture.Register();
        fixture.Post(1);
        fixture.Pump();
        var schema = Tools.Schema().Single(n => n!["name"]!.GetValue<string>() == "aide_coordination_read")!;
        Assert.False(schema["inputSchema"]!["additionalProperties"]!.GetValue<bool>());
        Assert.Equal(new[] { "source_id", "cursor", "limit" },
            schema["inputSchema"]!["properties"]!.AsObject().Select(p => p.Key));
        var context = new ServerContext(null, fixture.Database,
            new ResolvedIdentity(fixture.Reader, IdentitySource.Environment, null), null);
        var args = new JsonObject { ["source_id"] = fixture.SourceId, ["limit"] = 1 };
        var first = Call(args);
        Assert.Equal("Available", first["Status"]!.GetValue<string>());
        var next = Call(new() { ["source_id"] = fixture.SourceId, ["cursor"] = first["Continuation"]!.DeepClone() });
        Assert.Equal("Available", next["Status"]!.GetValue<string>());
        foreach (var key in new[] { "repository", "bound_repository_key", "root", "scope", "source_origin" })
        {
            var hostile = args.DeepClone().AsObject();
            hostile[key] = "private-marker";
            Assert.Equal("InvalidRequest", Call(hostile)["Status"]!.GetValue<string>());
        }
        args["cursor"] = "not-a-cursor";
        Assert.Equal("InvalidRequest", Call(args)["Status"]!.GetValue<string>());
        Assert.DoesNotContain("private-marker", first.ToJsonString());
        Assert.DoesNotContain("private-root", first.ToJsonString());
        Assert.Single(BoardTools.Read(fixture.Store, fixture.Reader).Entries);

        JsonObject Call(JsonObject arguments)
        {
            var response = Tools.Call(new() { ["name"] = "aide_coordination_read", ["arguments"] = arguments.DeepClone() }, context);
            return JsonNode.Parse(response["content"]![0]!["text"]!.GetValue<string>())!.AsObject();
        }
    }

    [Fact]
    public void Mcp_RealExclusiveLockOrMissingFile_ReturnsTypedUnavailable()
    {
        using var fixture = new FeedFixture();
        fixture.Pump();
        fixture.Store.Dispose();
        using var blocker = fixture.Connection();
        using var hold = blocker.CreateCommand();
        hold.CommandText = "PRAGMA journal_mode=DELETE; BEGIN EXCLUSIVE;";
        hold.ExecuteNonQuery();
        var context = new ServerContext(null, fixture.Database,
            new ResolvedIdentity(fixture.Reader, IdentitySource.Environment, null), null);

        AssertUnavailable(context);
        AssertUnavailable(context with { StorePath = Path.Combine(fixture.Logs, "missing.db") });

        hold.CommandText = "ROLLBACK;";
        hold.ExecuteNonQuery();

        void AssertUnavailable(ServerContext server)
        {
            var response = Tools.Call(new()
            {
                ["name"] = "aide_coordination_read",
                ["arguments"] = new JsonObject { ["source_id"] = fixture.SourceId },
            }, server);
            var body = response["content"]![0]!["text"]!.GetValue<string>();
            Assert.Contains("\"Status\":\"Unavailable\"", body);
            Assert.DoesNotContain("private-root", body);
        }
    }

    [Fact]
    public void Read_StorageFailure_IsTypedUnavailableAndEmitsSafeSpan()
    {
        using var fixture = new FeedFixture();
        fixture.Pump();
        var spans = new List<System.Diagnostics.Activity>();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
                System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == "coordination.cache.read")
                {
                    spans.Add(activity);
                }
            },
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);
        fixture.Store.CoordinationReadAfterBinding = () => throw new SqliteException("private-root private-marker", 5);

        var failed = fixture.Read();

        Assert.Equal(CoordinationReadStatus.Unavailable, failed.Status);
        Assert.Empty(failed.Entries);
        Assert.DoesNotContain("private-marker", JsonSerializer.Serialize(failed));
        fixture.Store.Dispose();
        Assert.Equal(CoordinationReadStatus.Unavailable, fixture.Read().Status);
        Assert.Equal(2, spans.Count);
        Assert.All(spans, span =>
        {
            Assert.Equal("COORD_READ_Unavailable", span.GetTagItem("error.type"));
            Assert.Equal(0, span.GetTagItem("coordination.read.entries"));
            Assert.True((double)span.GetTagItem("coordination.read.elapsed_ms")! >= 0);
            Assert.DoesNotContain("private-marker", JsonSerializer.Serialize(span.TagObjects));
        });
    }

    [Fact]
    public void FreshSchema_CheckpointBinding_HasExactlyThreeNullableColumns()
    {
        using var fixture = new FeedFixture();
        using var connection = fixture.Connection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*) FROM pragma_table_info('coord_projection_checkpoint')
            WHERE name IN ('bound_repository_key','source_origin','public_source_id') AND "notnull"=0;
            """;

        Assert.Equal(3L, command.ExecuteScalar());
    }

    [Fact]
    public void FreshSchema_BoundEmptyCheckpoint_AcceptsOnlyEmptyDigest()
    {
        using var fixture = new FeedFixture();

        fixture.Execute("""
            INSERT INTO coord_projection_checkpoint
            (scope,epoch,accepted_offset,prefix_digest,bound_repository_key,source_origin,public_source_id)
            VALUES('empty','legacy-native-1',0,
            'E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855',
            'repo','trusted-native','opaque');
            """);

        Assert.Equal(1, fixture.Number("SELECT COUNT(*) FROM coord_projection_checkpoint;"));
        Assert.Throws<SqliteException>(() => fixture.Execute("""
            INSERT INTO coord_projection_checkpoint
            (scope,epoch,accepted_offset,prefix_digest,bound_repository_key,source_origin,public_source_id)
            VALUES('bad','legacy-native-1',0,'wrong','repo','trusted-native','other');
            """));
    }

    private sealed class FeedFixture : IDisposable
    {
        private readonly string _root = Path.Combine(Environment.CurrentDirectory, "p24-feed-" + Guid.NewGuid().ToString("N"));
        public string Database => Path.Combine(_root, "watcher.db");
        public string Logs => Path.Combine(_root, "private-root");
        public string FilePath => Path.Combine(Logs, "session.jsonl");
        public string Scope => CoordinationSourceCapture.RootKey(Logs) + CoordinationSourceCapture.RootKey(FilePath).TrimEnd('|');
        public string SourceId => CoordinationSourceCapture.Hash(Encoding.UTF8.GetBytes(Scope));
        public SqliteWatcherObservationStore Store { get; }
        public FixedTimeProvider Time { get; } = new(DateTimeOffset.UnixEpoch);
        public InjectedContractIngest Ingest { get; }
        public CoordContractWriter Writer => new(Logs, Time);
        public SessionRecord Reader { get; }
        public void Pump() => new CoordContractLogPump(Logs, Ingest,
            new CoordinationSourceBinding(Reader.Binding.Repository, "trusted-native")).PumpOnce();
        public CoordinationReadResult Read(CoordinationCursor? cursor = null, int limit = 200) =>
            BoardTools.ReadCoordination(Store, Reader, SourceId, cursor, limit);

        public void Register()
        {
            var attrs = WatcherFixtures.HarnessRegistration().Attributes.ToDictionary(p => p.Key, p => p.Value);
            attrs[OtelAttributes.RepoPath] = _root;
            attrs[OtelAttributes.WorktreePath] = _root;
            Writer.WriteRegister("session", attrs);
        }

        public void Post(int count)
        {
            for (var i = 0; i < count; i++)
            {
                Writer.WriteBoardPost("session", "question", "private-marker");
            }
        }

        public void Tombstone() => Execute($"""
            BEGIN IMMEDIATE;
            INSERT INTO coord_projection_feed
            (scope,epoch,event_key,is_initial,admission_n,outcome,application_state,
             reason,session_id,session_generation,message_id,parent_event_key,
             recovery_status,eligibility_generation,payload_presence)
            SELECT scope,epoch,event_key,0,admission_n,'tombstone','applied',
                   'private-marker',session_id,session_generation,message_id,parent_event_key,
                   'none',eligibility_generation,0
            FROM coord_projection_feed WHERE scope='{Scope}' AND outcome='applied'
                AND message_id IS NOT NULL ORDER BY n DESC LIMIT 1;
            UPDATE coord_projection_event SET payload_presence=0,raw_bytes=NULL,canonical_bytes=NULL
            WHERE current_receipt_n=last_insert_rowid();
            COMMIT;
            """);

        public FeedFixture()
        {
            Directory.CreateDirectory(Logs);
            File.WriteAllBytes(FilePath, []);
            Store = SqliteWatcherObservationStore.Open(Database);
            var registrar = new TrustedRegistrar(Store, new SequentialCapabilityFactory(), new FakeMonotonicClock());
            Ingest = new InjectedContractIngest(new IngestHost(Store, registrar, Time));
            var attributes = WatcherFixtures.HarnessRegistration().Attributes.ToDictionary(p => p.Key, p => p.Value);
            attributes[OtelAttributes.RepoPath] = _root;
            attributes[OtelAttributes.WorktreePath] = _root;
            Writer.WriteRegister("session", attributes);
            var registration = Assert.IsType<ContractRegister>(Assert.Single(CoordContractLog.ReadDirectory(Logs)));
            Reader = new("reader", new SessionGeneration(1), Ingest.Host.PrepareObservation(registration));
            File.WriteAllBytes(FilePath, []);
        }

        public SqliteConnection Connection()
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = Database, Pooling = false, DefaultTimeout = 1, ForeignKeys = true,
            }.ToString());
            connection.Open();
            return connection;
        }

        public void Execute(string sql)
        {
            using var connection = Connection();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public long Number(string sql)
        {
            using var connection = Connection();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return (long)command.ExecuteScalar()!;
        }

        public void Dispose()
        {
            Store.Dispose();
            Directory.Delete(_root, recursive: true);
        }
    }
}
