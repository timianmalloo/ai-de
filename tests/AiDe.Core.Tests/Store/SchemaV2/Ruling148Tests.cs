using System.Security.Cryptography;
using System.Text;
using AiDe.Core.Facts;
using AiDe.Core.Store;
using AiDe.Core.Store.Records;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Tests.SchemaV2;

// Ruling 148 (ADR-0039 amendment of record): the eight merge conditions on schema v2. Every test here
// ATTEMPTS the violation through Microsoft.Data.Sqlite's bundled engine - the repo's normal test path.
// New stable codes appear as literals so a missing implementation fails on behaviour, not on a compile error.

/// <summary>Shared helpers for the Ruling 148 proofs.</summary>
internal static class Ruling148
{
    internal const string VersionOrderCode = "AIDE-STORE-VERSION-ORDER-VIOLATION";
    internal const string BodyHashCode = "AIDE-STORE-BODY-HASH-REQUIRED";
    internal const string DerivedStateCode = "AIDE-STORE-DERIVED-STATE-IN-PAYLOAD";

    internal static string Violation(Action<StoreWriter> act, WorkspaceStore store)
    {
        using var writer = store.BeginWrite();
        var ex = Assert.ThrowsAny<Exception>(() => act(writer));
        return ex is WorkspaceStoreException w ? w.ErrorCode : ((SqliteException)ex).Message;
    }

    internal static string Sha(string body) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(body)));

    internal static object? Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = command.ExecuteScalar();
        return value is DBNull ? null : value;
    }

    internal static void SeedJourney(WorkspaceStore store, string journeyId = "j1")
    {
        using var writer = store.BeginWrite();
        writer.AppendJourney(new JourneyRow(journeyId, "work", "a journey"));
        writer.Commit();
    }
}

/// <summary>(a) The post-purge reference check runs the same queries as the attempts-bind-revisions gate.</summary>
public sealed class Ruling148PostPurgeCheckTests : IDisposable
{
    private readonly string _directory = SchemaV2Support.NewDirectory();

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void ACompactionThatLeavesAnOrphanAttempt_IsRefused_AndTheStoreIsUntouched()
    {
        var path = Path.Combine(_directory, "workspace.db");
        using (var store = WorkspaceStore.Open(path))
        {
            using var writer = store.BeginWrite();
            for (var generation = 1; generation <= 2; generation++)
            {
                writer.DesireScopeGeneration("scope-1", generation, $"rev-{generation}");
                writer.CommitSnapshot("scope-1", generation, $"rev-{generation}", [], complete: true);
            }

            writer.Commit();
        }

        SqliteConnection.ClearAllPools();
        using (var raw = SchemaV2Support.OpenRaw(path))
        {
            // The trigger is dropped to plant the orphan the purge path could otherwise leave behind.
            SchemaV2Support.Run(raw, """
                DROP TRIGGER trg_dispatch_attempt_binds_revision;
                INSERT INTO dispatch_attempt_fact (dispatch_key, workspace_id, workspace_epoch, draft_id,
                                                   revision_no, session_id, session_generation, attempted_at, ingress_seq)
                VALUES ('orphan', 'ws', 1, 'ghost', 9, 's', 1, 'now', 900);
                """);
        }

        SqliteConnection.ClearAllPools();

        var ex = Assert.Throws<WorkspaceStoreException>(() => new StoreCompactor(path).Compact());

        Assert.Equal(StoreErrorCodes.ReferenceViolation, ex.ErrorCode);
        SqliteConnection.ClearAllPools();
        using var after = SchemaV2Support.OpenRaw(path, readOnly: true);
        Assert.Equal(2, SchemaV2Support.Count(after, "SELECT count(*) FROM scope_snapshot_committed_fact"));
    }
}

/// <summary>(b) A per-dimension BEFORE INSERT trigger enforces the next version_no and a strictly greater ingress_seq.</summary>
public sealed class Ruling148VersionOrderTests : IDisposable
{
    private readonly TestWorkspace _workspace = TestWorkspace.Create();

    public void Dispose() => _workspace.Dispose();

    private WorkspaceStore Store => _workspace.Store;

    private static string Insert(string table, int version, long seq) => table switch
    {
        "journey_dim" => $"""
            INSERT INTO journey_dim (journey_id, version_no, purpose, title, recorded_at, ingress_seq)
            VALUES ('jx', {version}, 'work', 't', 'now', {seq});
            """,
        "notebook_dim" => $"""
            INSERT INTO notebook_dim (notebook_id, version_no, owner_journey_id, legacy, recorded_at, ingress_seq)
            VALUES ('nx', {version}, 'j1', 0, 'now', {seq});
            """,
        _ => $"""
            INSERT INTO harness_profile_dim (profile_id, version_no, engine_id, family, native_home, recorded_at, ingress_seq)
            VALUES ('px', {version}, 'e', 'claude', 'h', 'now', {seq});
            """,
    };

    private void SeedVersionOne(string table)
    {
        Ruling148.SeedJourney(Store);
        using var writer = Store.BeginWrite();
        writer.ExecuteRawInternal(Insert(table, 1, 1000));
        writer.Commit();
    }

    [Theory]
    [InlineData("journey_dim")]
    [InlineData("notebook_dim")]
    [InlineData("harness_profile_dim")]
    public void ANewKeyMustStartAtVersionOne(string table)
    {
        Ruling148.SeedJourney(Store);

        var code = Ruling148.Violation(w => w.ExecuteRawInternal(Insert(table, 2, 1000)), Store);

        Assert.Equal(Ruling148.VersionOrderCode, code);
    }

    [Theory]
    [InlineData("journey_dim")]
    [InlineData("notebook_dim")]
    [InlineData("harness_profile_dim")]
    public void AVersionCannotSkipAhead(string table)
    {
        SeedVersionOne(table);

        var code = Ruling148.Violation(w => w.ExecuteRawInternal(Insert(table, 3, 1001)), Store);

        Assert.Equal(Ruling148.VersionOrderCode, code);
    }

    [Theory]
    [InlineData("journey_dim")]
    [InlineData("notebook_dim")]
    [InlineData("harness_profile_dim")]
    public void ANextVersionCannotTieOrPrecedeTheLatestIngressSequence(string table)
    {
        SeedVersionOne(table);

        Assert.Equal(Ruling148.VersionOrderCode, Ruling148.Violation(w => w.ExecuteRawInternal(Insert(table, 2, 1000)), Store));
        Assert.Equal(Ruling148.VersionOrderCode, Ruling148.Violation(w => w.ExecuteRawInternal(Insert(table, 2, 999)), Store));
    }

    [Theory]
    [InlineData("journey_dim")]
    [InlineData("notebook_dim")]
    [InlineData("harness_profile_dim")]
    public void TheNextVersionWithAGreaterIngressSequenceIsAccepted(string table)
    {
        SeedVersionOne(table);

        using var writer = Store.BeginWrite();
        writer.ExecuteRawInternal(Insert(table, 2, 1001));
        writer.Commit();
    }
}

/// <summary>(c) legacy changes only 1 to 0, and a version-1 legacy=1 row comes only from the backfill.</summary>
public sealed class Ruling148LegacyFlagTests : IDisposable
{
    private readonly TestWorkspace _workspace = TestWorkspace.Create();

    public void Dispose() => _workspace.Dispose();

    private WorkspaceStore Store => _workspace.Store;

    [Fact]
    public void ANewLegacyNotebook_CannotBeCreatedByACaller()
    {
        var code = Ruling148.Violation(w => w.AppendNotebook(new NotebookRow("nb-new", null, Legacy: true)), Store);

        Assert.Equal(StoreErrorCodes.Type0Violation, code);
    }

    [Fact]
    public void AnOwnedNotebook_CannotReturnToLegacy()
    {
        Ruling148.SeedJourney(Store);
        using (var writer = Store.BeginWrite())
        {
            writer.AppendNotebook(new NotebookRow("nb1", "j1", Legacy: false));
            writer.Commit();
        }

        var code = Ruling148.Violation(w => w.AppendNotebook(new NotebookRow("nb1", "j1", Legacy: true)), Store);

        Assert.Equal(StoreErrorCodes.Type0Violation, code);
    }

    [Fact]
    public void TheStoreRefusesBothEvenWhenTheApiIsBypassed()
    {
        Ruling148.SeedJourney(Store);

        var created = Ruling148.Violation(w => w.ExecuteRawInternal("""
            INSERT INTO notebook_dim (notebook_id, version_no, owner_journey_id, legacy, recorded_at, ingress_seq)
            VALUES ('raw-legacy', 1, NULL, 1, 'now', 1000);
            """), Store);
        Assert.Equal(StoreErrorCodes.Type0Violation, created);

        using (var writer = Store.BeginWrite())
        {
            writer.ExecuteRawInternal("""
                INSERT INTO notebook_dim (notebook_id, version_no, owner_journey_id, legacy, recorded_at, ingress_seq)
                VALUES ('raw-owned', 1, 'j1', 0, 'now', 1001);
                """);
            writer.Commit();
        }

        var returned = Ruling148.Violation(w => w.ExecuteRawInternal("""
            INSERT INTO notebook_dim (notebook_id, version_no, owner_journey_id, legacy, recorded_at, ingress_seq)
            VALUES ('raw-owned', 2, 'j1', 1, 'now', 1002);
            """), Store);
        Assert.Equal(StoreErrorCodes.Type0Violation, returned);
    }
}

/// <summary>(d) body_sha has one home (every writer sets it); every revision save creates or requires a notebook.</summary>
public sealed class Ruling148BodyShaAndNotebookTests : IDisposable
{
    private readonly TestWorkspace _workspace = TestWorkspace.Create();

    public void Dispose() => _workspace.Dispose();

    private WorkspaceStore Store => _workspace.Store;

    [Fact]
    public void ThePtySavePath_SetsBodySha_EqualToSha256OfTheBody()
    {
        using (var writer = Store.BeginWrite())
        {
            writer.SavePromptRevision("pty-draft", 1, "hello pty");
            writer.Commit();
        }

        using var raw = SchemaV2Support.OpenRaw(_workspace.DatabasePath, readOnly: true);
        Assert.Equal(Ruling148.Sha("hello pty"),
            Ruling148.Scalar(raw, "SELECT body_sha FROM prompt_revision_fact WHERE draft_id = 'pty-draft'"));
    }

    [Fact]
    public void TheTypedSavePath_SetsBodySha_EqualToSha256OfTheBody()
    {
        Ruling148.SeedJourney(Store);
        using (var writer = Store.BeginWrite())
        {
            writer.AppendNotebook(new NotebookRow("nb1", "j1", Legacy: false));
            writer.AppendNotebookRevision(new NotebookRevisionRow("nb1", 1, "typed body"));
            writer.Commit();
        }

        using var raw = SchemaV2Support.OpenRaw(_workspace.DatabasePath, readOnly: true);
        Assert.Equal(Ruling148.Sha("typed body"),
            Ruling148.Scalar(raw, "SELECT body_sha FROM prompt_revision_fact WHERE draft_id = 'nb1'"));
    }

    [Fact]
    public void ThePtySavePath_CreatesTheNotebook_AsAnOwnedNonLegacyOne()
    {
        using (var writer = Store.BeginWrite())
        {
            writer.SavePromptRevision("pty-draft", 1, "a");
            writer.SavePromptRevision("pty-draft", 2, "b");
            writer.Commit();
        }

        using var raw = SchemaV2Support.OpenRaw(_workspace.DatabasePath, readOnly: true);
        Assert.Equal(1, SchemaV2Support.Count(raw,
            "SELECT count(*) FROM notebook_dim WHERE notebook_id = 'pty-draft' AND legacy = 0 AND owner_journey_id IS NOT NULL"));
    }

    [Fact]
    public void ARevisionForANotebookThatDoesNotExist_IsRefused()
    {
        var code = Ruling148.Violation(w => w.AppendNotebookRevision(new NotebookRevisionRow("ghost", 1, "b")), Store);

        Assert.Equal(StoreErrorCodes.ReferenceViolation, code);
    }

    [Fact]
    public void ARevisionWithoutABodyHash_IsRefusedByTheStore()
    {
        Ruling148.SeedJourney(Store);
        using (var writer = Store.BeginWrite())
        {
            writer.AppendNotebook(new NotebookRow("nb1", "j1", Legacy: false));
            writer.Commit();
        }

        var code = Ruling148.Violation(w => w.ExecuteRawInternal("""
            INSERT INTO prompt_revision_fact (draft_id, revision_no, body, saved_at, ingress_seq)
            VALUES ('nb1', 1, 'no hash', 'now', 1000);
            """), Store);

        Assert.Equal(Ruling148.BodyHashCode, code);
    }
}

/// <summary>(e) assessment payload never holds a state that ADR-0039 section 3 derives.</summary>
public sealed class Ruling148AssessmentPayloadTests : IDisposable
{
    private readonly TestWorkspace _workspace = TestWorkspace.Create();

    public void Dispose() => _workspace.Dispose();

    private WorkspaceStore Store => _workspace.Store;

    [Theory]
    [InlineData("{\"satisfied\":true}")]
    [InlineData("{\"accepted\":false}")]
    [InlineData("{\"stale\":true}")]
    [InlineData("{\"current\":true}")]
    [InlineData("{\"Satisfied\":true}")]
    [InlineData("{\"findings\":[{\"id\":\"f1\",\"stale\":false}]}")]
    [InlineData("{\"a\":{\"b\":{\"closed\":true}}}")]
    public void AForbiddenDerivedStateKey_AtAnyDepth_IsRefused(string payload)
    {
        var code = Ruling148.Violation(w => w.AppendAssessment(
            new AssessmentRow("as-bad", "drift", "scope", "base", "[]", payload)), Store);

        Assert.Equal(Ruling148.DerivedStateCode, code);
    }

    [Fact]
    public void APayloadThatIsNotJson_IsRefused()
    {
        var code = Ruling148.Violation(w => w.AppendAssessment(
            new AssessmentRow("as-bad", "drift", "scope", "base", "[]", "not json")), Store);

        Assert.Equal(Ruling148.DerivedStateCode, code);
    }

    [Fact]
    public void FindingsAsAssertedAtTheBaseline_AreAccepted()
    {
        using var writer = Store.BeginWrite();
        writer.AppendAssessment(new AssessmentRow("as-ok", "drift", "scope", "base", "[]",
            "{\"findings\":[{\"id\":\"f1\",\"text\":\"the spec and the code disagree\",\"severity\":\"major\"}]}"));
        writer.Commit();
    }
}

/// <summary>(f) The v2 DDL is frozen: vocabulary growth is a v3 migration, never an edit to v2.</summary>
public sealed class Ruling148FrozenDdlTests
{
    [Fact]
    public void V2Sql_IsACompileTimeLiteral_SoNoLiveListCanReachIt()
    {
        var field = typeof(WorkspaceSchema).GetField(nameof(WorkspaceSchema.V2Sql),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(field);
        Assert.True(field.IsLiteral, "V2Sql must be a const: a static built from live lists drifts between fresh and migrated stores");
    }

    [Fact]
    public void TheV2MigrationDdl_IsPinnedByHash()
    {
        // A change to the v2 DDL (or to the triggers appended to it) re-defines a schema already deployed.
        // New vocabulary or constraints belong in a v3 migration; this pin makes the edit loud.
        var ddl = WorkspaceSchema.Migrations.Single(m => m.Version == 2).Ddl.ReplaceLineEndings(((char)10).ToString());

        Assert.Equal("a75cc353890a9d683dc33977bcdf4013b1e6e636c0163bd37538072797f55df2", Ruling148.Sha(ddl));
    }

    [Fact]
    public void EveryLiveVocabularyValue_IsAlreadyInTheFrozenDdl_SoGrowthFailsHereUntilV3Exists()
    {
        var ddl = WorkspaceSchema.V2Sql;
        var live = WorkbenchVocabulary.JourneyPurposes
            .Concat(WorkbenchVocabulary.JourneyOriginKinds).Concat(WorkbenchVocabulary.DeliveryUnknownCauses)
            .Concat(WorkbenchVocabulary.AssessmentKinds).Concat(WorkbenchVocabulary.DispositionSubjectKinds)
            .Concat(WorkbenchVocabulary.DispositionKindsBySubject.Values.SelectMany(v => v))
            .Concat(WorkbenchVocabulary.ProfileObservationKinds).Concat(WorkbenchVocabulary.ForbiddenPayloadKeys)
            .Concat(Enum.GetNames<DispatchState>());

        foreach (var value in live.Distinct())
        {
            Assert.Contains($"'{value}'", ddl, StringComparison.Ordinal);
        }

        foreach (var terminal in new[] { "ResponseComplete", "Failed", "Interrupted", "DeliveryUnknown", "Rejected" })
        {
            Assert.Matches($@"ux_outcome_one_terminal[^;]*'{terminal}'", ddl);
        }
    }
}

/// <summary>(g) An intent-accepted disposition references an existing (notebook, revision) by a declared composite FK.</summary>
public sealed class Ruling148IntentReferenceTests : IDisposable
{
    private readonly TestWorkspace _workspace = TestWorkspace.Create();

    public void Dispose() => _workspace.Dispose();

    private WorkspaceStore Store => _workspace.Store;

    [Fact]
    public void AnIntentAcceptance_OfARevisionThatDoesNotExist_IsRefused()
    {
        var message = Ruling148.Violation(w => w.AppendDisposition(new DispositionRow(
            "intent", "j1", "intent-accepted", "user", "base", NotebookId: "no-such-notebook", RevisionNo: 7)), Store);

        Assert.Contains("FOREIGN KEY constraint failed", message);
    }

    [Fact]
    public void AnIntentAcceptance_OfAnExistingRevision_IsAccepted()
    {
        Ruling148.SeedJourney(Store);
        using var writer = Store.BeginWrite();
        writer.AppendNotebook(new NotebookRow("nb1", "j1", Legacy: false));
        writer.AppendNotebookRevision(new NotebookRevisionRow("nb1", 1, "intent"));
        writer.AppendDisposition(new DispositionRow(
            "intent", "j1", "intent-accepted", "user", "base", NotebookId: "nb1", RevisionNo: 1));
        writer.Commit();
    }
}

/// <summary>(h) The migration gate checks existing outcome states against the vocabulary and reports the unknowns.</summary>
public sealed class Ruling148OutcomeStateGateTests : IDisposable
{
    private readonly string _directory = SchemaV2Support.NewDirectory();

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void AV1FileWithAnUnknownOutcomeState_FailsTheGate_NamesTheState_AndStaysV1()
    {
        var path = Path.Combine(_directory, "workspace.db");
        SchemaV2Support.CreateV1Database(path);
        using (var raw = SchemaV2Support.OpenRaw(path))
        {
            SchemaV2Support.Run(raw,
                "INSERT INTO dispatch_outcome_fact VALUES ('key-a', 20, 'Bogus', NULL, '2026-10-01T00:00:20Z');");
        }

        SqliteConnection.ClearAllPools();

        var ex = Assert.Throws<WorkspaceStoreException>(() => WorkspaceStore.Open(path));

        Assert.Equal(StoreErrorCodes.MigrationFailed, ex.ErrorCode);
        Assert.Contains("Bogus", ex.Message, StringComparison.Ordinal);
        SqliteConnection.ClearAllPools();
        using var after = SchemaV2Support.OpenRaw(path, readOnly: true);
        Assert.Equal(1, SchemaV2Support.Count(after, "SELECT max(version) FROM schema_version"));
    }
}
