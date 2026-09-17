using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using AiDe.Core.Watcher;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace AiDe.Core.Tests.Watcher;

[Trait("Platform", "Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class NativeAdmissionRuntimeTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void RegisterNative_TwoHostsAt128_RefusesWithoutAnyMutation(bool existingTerminal, bool inFlight)
    {
        using var fixture = new Fixture();
        for (var index = 0; index < 128; index++)
            fixture.Admit(index % 2, $"op-{index}", $"terminal-{index}");
        if (inFlight) fixture.MarkInFlight();
        var before = fixture.Snapshot();
        var minted = fixture.FirstFactory.Count + fixture.SecondFactory.Count;

        var failure = Assert.Throws<WatcherException>(() =>
            fixture.Admit(1, "overflow", existingTerminal ? "terminal-0" : "overflow"));

        Assert.Equal(NativeAdmissionErrors.Capacity, failure.Code);
        Assert.Equal(before, fixture.Snapshot());
        Assert.Equal(minted, fixture.FirstFactory.Count + fixture.SecondFactory.Count);
        Assert.Equal(128, fixture.FirstStore.PendingNativeNoticeCount());
        output.WriteLine("ENHANCED N2: 128 retained; session/fact/notice/liveness/capability membership and mint count unchanged.");
    }

    [Fact]
    public void RegisterNative_TwoDifferentStores_SharesProcessCapacity()
    {
        using var first = new Fixture();
        using var second = new Fixture();
        for (var index = 0; index < 64; index++)
        {
            first.Admit(0, $"a-{index}", $"a-{index}");
            second.Admit(0, $"b-{index}", $"b-{index}");
        }
        var before = second.Snapshot();

        Assert.Equal(NativeAdmissionErrors.Capacity,
            Assert.Throws<WatcherException>(() => second.Admit(1, "overflow", "overflow")).Code);
        Assert.Equal(before, second.Snapshot());
    }

    [Fact]
    public void RegisterNative_FullQuota_UncorrectedAdmissionNeedsNoNotice()
    {
        using var fixture = new Fixture();
        for (var index = 0; index < 128; index++) fixture.Admit(0, $"op-{index}", $"terminal-{index}");

        var accepted = fixture.Admit(1, "uncorrected", "canonical", canonical: true);

        Assert.Equal("NONE", accepted.Admission.CorrectionCode);
        Assert.Equal(fixture.Repository, accepted.Admission.RepositorySent);
        Assert.Equal(128, fixture.FirstStore.PendingNativeNoticeCount());
        Assert.Equal(129, fixture.Count("native_registration_admission_fact"));
        Assert.NotNull(accepted.Capability);
    }

    [Theory]
    [InlineData((int)NativeAdmissionFaultPoint.AfterAdmission)]
    [InlineData((int)NativeAdmissionFaultPoint.AfterNotice)]
    [InlineData((int)NativeAdmissionFaultPoint.AfterSession)]
    [InlineData((int)NativeAdmissionFaultPoint.AfterEndClear)]
    [InlineData((int)NativeAdmissionFaultPoint.AfterHeartbeat)]
    [InlineData((int)NativeAdmissionFaultPoint.BeforeCommit)]
    public void RegisterNative_PrecommitFault_RollsBackAndReleasesReservation(int pointValue)
    {
        var point = (NativeAdmissionFaultPoint)pointValue;
        using var fixture = new Fixture();
        var original = fixture.Admit(0, "original", "terminal");
        fixture.FirstRegistrar.End(original.Admission.Session.SessionId, original.Capability!);
        fixture.Clock.Advance(TimeSpan.FromSeconds(10));
        var before = fixture.Snapshot();
        fixture.FirstRoot.Fault = observed => { if (point == observed) throw new InjectedFaultException(); };

        Assert.Throws<InjectedFaultException>(() => fixture.Admit(0, "retry", "terminal"));

        Assert.Equal(before, fixture.Snapshot());
        Assert.True(fixture.FirstRegistrar.Verify(original.Admission.Session.SessionId, original.Capability!));
        fixture.FirstRoot.Fault = null;
        var accepted = fixture.Admit(0, "retry", "terminal");
        Assert.Equal(2, accepted.Admission.Session.Generation.Value);
        Assert.Equal(2, fixture.FirstStore.PendingNativeNoticeCount());
        for (var index = 2; index < 128; index++) fixture.Admit(1, $"remaining-{index}", $"terminal-{index}");
        Assert.Equal(128, fixture.FirstStore.PendingNativeNoticeCount());
        output.WriteLine($"Fault {point}: entire before/after snapshot equal; released reservation filled through 128.");
    }

    [Fact]
    public void RegisterNative_CommitThenLostReturn_ReplaysHistoricalReceiptWithoutMintOrLifecycle()
    {
        using var fixture = new Fixture();
        fixture.FirstRoot.Fault = point =>
        {
            if (NativeAdmissionFaultPoint.AfterCommit == point) throw new InjectedFaultException();
        };
        Assert.Throws<InjectedFaultException>(() => fixture.Admit(0, "lost", "terminal"));
        Assert.Equal(1, fixture.Count("native_registration_admission_fact"));
        Assert.Equal(1, fixture.FirstStore.PendingNativeNoticeCount());
        Assert.Equal(0, fixture.Capabilities(fixture.FirstRegistrar));
        fixture.FirstRoot.Fault = null;
        var firstReplay = fixture.Admit(0, "lost", "terminal");
        var generation2 = fixture.Admit(1, "second", "terminal");
        fixture.SecondRegistrar.End(generation2.Admission.Session.SessionId, generation2.Capability!);
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        var before = fixture.Snapshot();
        var minted = fixture.FirstFactory.Count;

        var replay = fixture.Admit(0, "lost", "terminal");

        Assert.Equal(firstReplay.Admission, replay.Admission);
        Assert.Equal(1, replay.Admission.Session.Generation.Value);
        Assert.Equal(2, generation2.Admission.Session.Generation.Value);
        Assert.True(replay.Replayed);
        Assert.Null(replay.Capability);
        Assert.Equal(minted, fixture.FirstFactory.Count);
        Assert.Equal(before, fixture.Snapshot());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RegisterNative_OperationReusedWithDifferentInputOrContext_Refuses(bool changeContext)
    {
        using var fixture = new Fixture();
        fixture.Admit(0, "original", "terminal");
        var before = fixture.Snapshot();
        var registration = fixture.Registration("terminal");
        var context = fixture.Context("terminal");
        if (changeContext) context = context with { PublicationRoot = fixture.OtherOutput };
        else registration = registration with
        {
            Attributes = new Dictionary<string, string?>(registration.Attributes)
            {
                [OtelAttributes.RepoDisplay] = "different exact claim",
            },
        };

        var failure = Assert.Throws<WatcherException>(() =>
            fixture.FirstHost.RegisterNative(fixture.FirstRoot, "original", registration, context));

        Assert.Equal(NativeAdmissionErrors.OperationConflict, failure.Code);
        Assert.Equal(before, fixture.Snapshot());
    }

    [Theory]
    [InlineData("heartbeat")]
    [InlineData("end")]
    [InlineData("update")]
    public async Task Lifecycle_G1VerifiedThenG2Registered_RefusesStaleWrite(string operation)
    {
        using var fixture = new Fixture();
        var first = fixture.Admit(0, "first", "terminal");
        using var verified = new ManualResetEventSlim();
        using var resume = new ManualResetEventSlim();
        fixture.FirstRegistrar.NativeLifecycleFault = _ =>
        {
            verified.Set();
            Assert.True(resume.Wait(TimeSpan.FromSeconds(15)));
        };
        var stale = Task.Run(() => Record.Exception(() =>
            fixture.Lifecycle(fixture.FirstRegistrar, first, operation)));
        try
        {
            Assert.True(verified.Wait(TimeSpan.FromSeconds(15)));
            var second = fixture.Admit(1, "second", "terminal");
            Assert.Equal(first.Admission.Session.SessionId, second.Admission.Session.SessionId);
            Assert.Equal(2, second.Admission.Session.Generation.Value);
            var before = fixture.Snapshot();
            resume.Set();

            var failure = Assert.IsType<WatcherException>(await stale);

            Assert.Equal(NativeAdmissionErrors.Stale, failure.Code);
            Assert.Equal(before, fixture.Snapshot());
        }
        finally
        {
            resume.Set();
            await stale;
        }
    }

    [Fact]
    public async Task RegisterNative_ConcurrentTerminalAdoption_OneSessionTwoSerializedGenerations()
    {
        using var fixture = new Fixture();
        using var start = new ManualResetEventSlim();
        var first = Task.Run(() => { start.Wait(); return fixture.Admit(0, "first", "terminal"); });
        var second = Task.Run(() => { start.Wait(); return fixture.Admit(1, "second", "terminal"); });
        start.Set();

        var results = await Task.WhenAll(first, second);

        Assert.Single(fixture.FirstStore.AllSessions());
        Assert.Equal(results[0].Admission.Session.SessionId, results[1].Admission.Session.SessionId);
        Assert.Equal(new long[] { 1, 2 }, results.Select(result => result.Admission.Session.Generation.Value).Order());
        Assert.Equal(2, fixture.Count("native_registration_admission_fact"));
        Assert.Equal(2, fixture.FirstStore.PendingNativeNoticeCount());
    }

    [Fact]
    public void Lifecycle_CurrentGeneration_UpdatesExpectedBindingThenHeartbeatAndEnd()
    {
        using var fixture = new Fixture();
        var accepted = fixture.Admit(0, "one", "terminal");

        fixture.Lifecycle(fixture.FirstRegistrar, accepted, "update");
        fixture.Clock.Advance(TimeSpan.FromSeconds(2));
        fixture.Lifecycle(fixture.FirstRegistrar, accepted, "heartbeat");
        fixture.Lifecycle(fixture.FirstRegistrar, accepted, "end");

        var id = accepted.Admission.Session.SessionId;
        Assert.Equal("new-model", fixture.FirstStore.FindSession(id)!.Binding.Model!.Name);
        Assert.Equal(TrustClassification.Asserted, fixture.FirstStore.FindSession(id)!.Binding.Trust);
        Assert.Equal(fixture.Clock.Ticks, fixture.FirstStore.LastHeartbeat(id));
        Assert.True(fixture.FirstStore.IsEnded(id));
        Assert.Equal(accepted.Admission, fixture.Admit(0, "one", "terminal").Admission);
    }

    [Theory]
    [InlineData(OtelAttributes.RepoPath, "abc\0suffix")]
    [InlineData(OtelAttributes.TerminalId, "terminal\0suffix")]
    [InlineData(OtelAttributes.AgentName, "agent\0suffix")]
    [InlineData(OtelAttributes.ServiceVersion, "version\0suffix")]
    [InlineData(OtelAttributes.WorktreePath, @"\\outside\share")]
    public void RegisterNative_InvalidRawInput_NeverReadsFilesystemOrMutates(string key, string value)
    {
        using var fixture = new Fixture();
        var reads = 0;
        fixture.FirstRoot.Fault = point => { if (NativeAdmissionFaultPoint.BeforeContextRead == point) reads++; };
        var registration = fixture.Registration("terminal");
        registration = registration with
        {
            Attributes = new Dictionary<string, string?>(registration.Attributes) { [key] = value },
        };
        var before = fixture.Snapshot();

        Assert.Throws<WatcherException>(() =>
            fixture.FirstHost.RegisterNative(fixture.FirstRoot, "bad", registration, fixture.Context("terminal")));

        Assert.Equal(0, reads);
        Assert.Equal(before, fixture.Snapshot());
        Assert.Equal(0, fixture.FirstFactory.Count);
    }

    [Fact]
    public void RegisterNative_OriginalClaimAndNoticeBytes_AgreeWithHistoricalDecision()
    {
        using var fixture = new Fixture();
        var rawClaim = fixture.Worktree.ToUpperInvariant() + "\\";
        var registration = fixture.Registration("terminal");
        registration = registration with
        {
            Attributes = new Dictionary<string, string?>(registration.Attributes) { [OtelAttributes.RepoPath] = rawClaim },
        };
        var accepted = fixture.FirstHost.RegisterNative(fixture.FirstRoot, "one", registration, fixture.Context("terminal"));
        var (bytes, digest) = fixture.Notice();

        using var json = JsonDocument.Parse(bytes);
        Assert.Equal(rawClaim, accepted.Admission.RepositorySent);
        Assert.Equal(rawClaim, json.RootElement.GetProperty("repositorySent").GetString());
        Assert.Equal(accepted.Admission.RepositoryUsed, json.RootElement.GetProperty("repositoryUsed").GetString());
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), digest);
        Assert.Equal(accepted.Admission.DecisionDigest, NativeAdmissionCodec.DecisionDigest(accepted.Admission));
        NativeAdmissionCodec.RequireNotice(accepted.Admission, bytes, digest);
        var wrongBytes = bytes.Concat(new byte[] { 32 }).ToArray();
        Assert.Equal(NativeAdmissionErrors.Integrity, Assert.Throws<WatcherException>(() =>
            NativeAdmissionCodec.RequireNotice(accepted.Admission, wrongBytes,
                Convert.ToHexStringLower(SHA256.HashData(wrongBytes)))).Code);
        Assert.Equal(NativeAdmissionErrors.Integrity, Assert.Throws<WatcherException>(() =>
            NativeAdmissionCodec.RequireNotice(accepted.Admission, bytes, new string('0', 64))).Code);
        Assert.NotEqual(accepted.Admission.DecisionDigest,
            NativeAdmissionCodec.DecisionDigest(accepted.Admission with { RepositorySent = "different" }));
        Assert.Empty(fixture.FirstHost.DrainRegistrationNotices());
    }

    [Fact]
    public void RegisterNative_CorrectedFact_HasInitialNoticeAndNoPrecommitCapability()
    {
        using var fixture = new Fixture();
        var commits = 0;
        fixture.FirstRoot.Fault = point =>
        {
            if (point is NativeAdmissionFaultPoint.BeforeCommit or NativeAdmissionFaultPoint.AfterCommit)
            {
                Assert.Equal(0, fixture.Capabilities(fixture.FirstRegistrar));
                commits++;
            }
        };

        var accepted = fixture.Admit(0, "one", "terminal");

        Assert.Equal(1, fixture.Count("native_registration_admission_fact"));
        Assert.Equal(1, fixture.Count("registration_notice_delivery"));
        Assert.Equal(2, commits);
        Assert.Equal(1, fixture.Capabilities(fixture.FirstRegistrar));
        Assert.Equal(accepted.Admission.Session, fixture.FirstStore.FindSession(accepted.Admission.Session.SessionId));
    }

    [Fact]
    public void RegisterNative_ExactReplay_DoesNotReadContextFilesAgain()
    {
        using var fixture = new Fixture();
        var accepted = fixture.Admit(0, "one", "terminal");
        fixture.FirstRoot.Fault = point =>
        {
            if (NativeAdmissionFaultPoint.BeforeContextRead == point) throw new InjectedFaultException();
        };

        var replay = fixture.Admit(0, "one", "terminal");

        Assert.Equal(accepted.Admission, replay.Admission);
        Assert.Null(replay.Capability);
        Assert.Equal(1, fixture.FirstFactory.Count);
    }

    [Theory]
    [InlineData(@"\\outside\share")]
    [InlineData(@"\\?\C:\device")]
    [InlineData(@"C:\allowed\..\outside")]
    [InlineData(@"C:\allowed\NUL")]
    public void RegisterNative_InvalidContextPath_RefusesBeforeFilesystemRead(string path)
    {
        using var fixture = new Fixture();
        var reads = 0;
        fixture.FirstRoot.Fault = point => { if (NativeAdmissionFaultPoint.BeforeContextRead == point) reads++; };
        var context = fixture.Context("terminal") with { PublicationRoot = path };

        Assert.Equal(NativeAdmissionErrors.ContextMismatch, Assert.Throws<WatcherException>(() =>
            fixture.FirstHost.RegisterNative(fixture.FirstRoot, "one", fixture.Registration("terminal"), context)).Code);

        Assert.Equal(0, reads);
        Assert.Equal(0, fixture.Count("native_registration_admission_fact"));
    }

    [Fact]
    public void RegisterNative_ForgedGitPointer_CannotOverrideCompositionRepository()
    {
        using var fixture = new Fixture();
        File.WriteAllText(Path.Combine(fixture.Worktree, ".git"), "gitdir: \\\\outside\\share\\.git\\worktrees\\forged");

        var accepted = fixture.Admit(0, "one", "terminal");

        Assert.Equal(fixture.Repository, accepted.Admission.RepositoryUsed);
        Assert.Equal(0, fixture.Locator.Reads);
    }

    [Fact]
    public void RegisterNative_InvalidUtf16OrMissingVersion_RefusesBeforeMint()
    {
        using var fixture = new Fixture();
        var attributes = new Dictionary<string, string?>(fixture.Registration("terminal").Attributes)
        {
            [OtelAttributes.AgentName] = new string('\ud800', 1),
        };
        Assert.Equal(NativeAdmissionErrors.InvalidInput, Assert.Throws<WatcherException>(() =>
            fixture.FirstHost.RegisterNative(fixture.FirstRoot, "one", new(attributes), fixture.Context("terminal"))).Code);
        attributes[OtelAttributes.AgentName] = "synthetic";
        attributes.Remove(OtelAttributes.ServiceVersion);

        Assert.Equal(NativeAdmissionErrors.InvalidInput, Assert.Throws<WatcherException>(() =>
            fixture.FirstHost.RegisterNative(fixture.FirstRoot, "one", new(attributes), fixture.Context("terminal"))).Code);

        Assert.Equal(0, fixture.FirstFactory.Count);
        Assert.Equal(0, fixture.Count("native_registration_admission_fact"));
    }

    [Fact]
    public void Enrollment_FileOwnerExclusion_ReleasesWhenAllRootsEnd()
    {
        using var fixture = new Fixture();
        using var competitor = new FileStream(fixture.FirstStore.DatabasePath, FileMode.Open,
            FileAccess.ReadWrite, FileShare.ReadWrite);
        Assert.Throws<IOException>(() => competitor.Lock(long.MaxValue - 1, 1));
        fixture.FirstRoot.Dispose();
        Assert.Throws<IOException>(() => competitor.Lock(long.MaxValue - 1, 1));
        fixture.SecondRoot.Dispose();

        competitor.Lock(long.MaxValue - 1, 1);
        Assert.Equal(NativeAdmissionErrors.Owner, Assert.Throws<WatcherException>(() =>
            new NativeRegistrationAdmissionRoot(fixture.FirstStore)).Code);
        competitor.Unlock(long.MaxValue - 1, 1);
        using var reopened = new NativeRegistrationAdmissionRoot(fixture.FirstStore);
        var accepted = fixture.FirstHost.RegisterNative(reopened, "new", fixture.Registration("terminal"), fixture.Context("terminal"));
        Assert.False(accepted.Replayed);
    }

    [Fact]
    public void RegisterNative_Activity_EmitsOutcomeAndDurationWithoutRawClaims()
    {
        using var fixture = new Fixture();
        var stopped = new List<System.Diagnostics.Activity>();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == "AiDe.NativeAdmission",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
                System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activity => stopped.Add(activity),
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        fixture.Admit(0, "one", "terminal");
        fixture.Admit(0, "one", "terminal");

        Assert.Equal(2, stopped.Count);
        Assert.Equal(false, stopped[0].GetTagItem("native.admission.replayed"));
        Assert.Equal(true, stopped[1].GetTagItem("native.admission.replayed"));
        Assert.IsType<double>(stopped[0].GetTagItem("native.admission.duration_ms"));
        Assert.DoesNotContain(fixture.Worktree, JsonSerializer.Serialize(stopped.Select(activity => activity.TagObjects)));
    }

    [Fact]
    public void RegisterNative_UnwiredPublicContext_ExplicitlyUnavailableAndLegacyStillUsable()
    {
        using var fixture = new Fixture();
        var before = fixture.Snapshot();

        Assert.Equal(NativeAdmissionErrors.Unavailable, Assert.Throws<WatcherException>(() =>
            fixture.FirstHost.RegisterNative("public", fixture.Registration("terminal"))).Code);
        Assert.Equal(before, fixture.Snapshot());
        var legacy = fixture.FirstHost.Register(fixture.Registration("terminal"));
        Assert.True(fixture.FirstRegistrar.Verify(legacy.SessionId, legacy.Capability));
        Assert.Equal(0, fixture.Count("native_registration_admission_fact"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RegisterNative_ReenrollmentWithExcessBacklog_RefusesAndPreservesHistory(bool canonical)
    {
        using var fixture = new Fixture();
        fixture.Admit(0, "seed", "terminal");
        fixture.SeedExcessBacklog();
        fixture.FirstRoot.Dispose();
        fixture.SecondRoot.Dispose();
        using var reopened = new NativeRegistrationAdmissionRoot(fixture.SecondStore);
        var before = fixture.Snapshot();

        Assert.Equal(NativeAdmissionErrors.Capacity, Assert.Throws<WatcherException>(() =>
            fixture.SecondHost.RegisterNative(reopened, "fresh", fixture.Registration("fresh", canonical),
                fixture.Context("fresh"))).Code);
        Assert.Equal(before, fixture.Snapshot());
        Assert.Equal(129, fixture.FirstStore.PendingNativeNoticeCount());
    }

    private sealed class InjectedFaultException : Exception;

    internal sealed class CountingFactory : ICapabilityFactory
    {
        private readonly SequentialCapabilityFactory _inner = new();
        internal int Count { get; private set; }
        public SessionCapability Create() { Count++; return _inner.Create(); }
    }

    private sealed class Fixture : IDisposable
    {
        private int _firstId, _secondId;
        internal string Root { get; }
        internal string Worktree { get; }
        internal string Output { get; }
        internal string OtherOutput { get; }
        internal string Repository => "bounded-repository-identity";
        internal FakeMonotonicClock Clock { get; } = new();
        internal CountingFactory FirstFactory { get; } = new();
        internal CountingFactory SecondFactory { get; } = new();
        internal SqliteWatcherObservationStore FirstStore { get; }
        internal SqliteWatcherObservationStore SecondStore { get; }
        internal NativeRegistrationAdmissionRoot FirstRoot { get; }
        internal NativeRegistrationAdmissionRoot SecondRoot { get; }
        internal TrustedRegistrar FirstRegistrar { get; }
        internal TrustedRegistrar SecondRegistrar { get; }
        internal IngestHost FirstHost { get; }
        internal IngestHost SecondHost { get; }
        internal CountingLocator Locator { get; } = new();

        internal Fixture()
        {
            Root = Path.Combine(Path.GetTempPath(), "aide-native-" + Guid.NewGuid().ToString("N"));
            Worktree = Path.Combine(Root, "MixedCaseWorktree");
            Output = Path.Combine(Root, "publication");
            OtherOutput = Path.Combine(Root, "other-publication");
            Directory.CreateDirectory(Worktree);
            Directory.CreateDirectory(Output);
            Directory.CreateDirectory(OtherOutput);
            FirstStore = SqliteWatcherObservationStore.Open(Path.Combine(Root, "watcher.db"));
            SecondStore = SqliteWatcherObservationStore.Open(Path.Combine(Root, ".", "watcher.db"));
            FirstRoot = new(FirstStore);
            SecondRoot = new(SecondStore);
            FirstRegistrar = new(FirstStore, FirstFactory, Clock, () => $"first-{++_firstId}");
            SecondRegistrar = new(SecondStore, SecondFactory, Clock, () => $"second-{++_secondId}");
            var time = new FixedTimeProvider(DateTimeOffset.UnixEpoch);
            FirstHost = new(FirstStore, FirstRegistrar, time, locator: Locator);
            SecondHost = new(SecondStore, SecondRegistrar, time, locator: Locator);
        }

        internal NativeRegistrationContext Context(string terminal) =>
            new(new(new(Repository, "trusted display"), "branch", Worktree), new(terminal), Output);

        internal HarnessRegistration Registration(string terminal, bool canonical = false) =>
            new(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [OtelAttributes.RepoPath] = canonical ? Repository : Worktree,
                [OtelAttributes.RepoDisplay] = "direct API claim",
                [OtelAttributes.WorktreePath] = Worktree,
                [OtelAttributes.WorktreeBranch] = "branch",
                [OtelAttributes.TerminalId] = terminal,
                [OtelAttributes.AgentName] = "synthetic",
                [OtelAttributes.ServiceName] = "github-copilot",
                [OtelAttributes.ServiceVersion] = "test",
            });

        internal RegistrationAdmissionResult Admit(int host, string operation, string terminal, bool canonical = false) =>
            (0 == host ? FirstHost : SecondHost).RegisterNative(0 == host ? FirstRoot : SecondRoot,
                operation, Registration(terminal, canonical), Context(terminal));

        internal void Lifecycle(TrustedRegistrar registrar, RegistrationAdmissionResult result, string operation)
        {
            var id = result.Admission.Session.SessionId;
            switch (operation)
            {
                case "heartbeat": registrar.Heartbeat(id, result.Capability!); break;
                case "end": registrar.End(id, result.Capability!); break;
                case "update": registrar.UpdateHarnessAndModel(id, result.Capability!, null, new("new-model", "1")); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        }

        private SqliteConnection Open()
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = FirstStore.DatabasePath, Pooling = false,
            }.ToString());
            connection.Open();
            return connection;
        }

        internal int Count(string table)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT count(*) FROM {table}";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        internal int Capabilities(TrustedRegistrar registrar) => ((IDictionary)typeof(TrustedRegistrar)
            .GetField("_capabilityBySession", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(registrar)!).Count;

        internal string Snapshot()
        {
            using var connection = Open();
            var tables = new Dictionary<string, List<object?[]>>();
            foreach (var table in new[] { "agent_session_dim", "session_heartbeat", "session_ended",
                         "native_registration_admission_fact", "registration_notice_delivery" })
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {table} ORDER BY rowid";
                using var reader = command.ExecuteReader();
                var rows = new List<object?[]>();
                while (reader.Read())
                {
                    var values = new object[reader.FieldCount];
                    reader.GetValues(values);
                    rows.Add(values.Select(value => value is DBNull ? null : value).ToArray());
                }
                tables.Add(table, rows);
            }
            return JsonSerializer.Serialize(new { tables, firstCaps = Capabilities(FirstRegistrar), secondCaps = Capabilities(SecondRegistrar) });
        }

        internal (byte[] Bytes, string Digest) Notice()
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT publication_bytes,publication_digest FROM registration_notice_delivery";
            using var reader = command.ExecuteReader();
            Assert.True(reader.Read());
            return ((byte[])reader.GetValue(0), reader.GetString(1));
        }

        internal void SeedExcessBacklog()
        {
            using var connection = Open();
            for (var index = 1; index <= 128; index++)
            {
                using var command = connection.CreateCommand();
                command.CommandText = """
                    INSERT INTO native_registration_admission_fact
                    SELECT $op,schema_version,input_digest,context_digest,decision_digest,$op,generation,
                        repository_sent,repository_used,worktree_path,worktree_branch,terminal_id,agent_name,
                        harness_name,harness_version,model_name,model_version,trust,correction_code,recorded_at_ms
                    FROM native_registration_admission_fact WHERE operation_id='seed';
                    INSERT INTO registration_notice_delivery
                    SELECT $id,$op,schema_version,decision_digest,correction_code,publication_bytes,publication_digest,
                        target_kind,target_root,state,attempt,ownership_version,due_at_ms,owner_id,published_at_ms
                    FROM registration_notice_delivery WHERE operation_id='seed';
                    """;
                command.Parameters.AddWithValue("$op", $"backlog-{index}");
                command.Parameters.AddWithValue("$id", index.ToString("x32"));
                command.ExecuteNonQuery();
            }
        }

        internal void MarkInFlight()
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE registration_notice_delivery SET state='InFlight',owner_id='synthetic-worker',
                    attempt=1,ownership_version=1;
                """;
            Assert.Equal(128, command.ExecuteNonQuery());
        }

        public void Dispose()
        {
            SecondRoot.Dispose();
            FirstRoot.Dispose();
            SecondStore.Dispose();
            FirstStore.Dispose();
            Directory.Delete(Root, recursive: true);
        }

        internal sealed class CountingLocator : IRepositoryLocator
        {
            internal int Reads { get; private set; }
            public string? RepositoryFor(string checkoutPath) { Reads++; return null; }
        }
    }
}
