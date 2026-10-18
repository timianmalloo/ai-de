using System.Security.Cryptography;
using System.Text;
using AiDe.Core.Facts;
using AiDe.Core.Store;
using AiDe.Core.Store.Records;
using AiDe.Core.Upgrade;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Tests.SchemaV2;

/// <summary>ADR-0039 §5: the first workspace.db migration chain, proven on a real v1 file.</summary>
public sealed class MigrationGoldenTests : IDisposable
{
    private readonly string _directory = SchemaV2Support.NewDirectory();

    private string Path_ => Path.Combine(_directory, "workspace.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void TheV1Baseline_IsFrozen()
    {
        // CreateSql is the v1 baseline the migration starts from. Editing it silently re-defines v1;
        // v2 changes belong in the migration. The pin makes that edit loud.
        var hash = Convert.ToHexStringLower(SHA256.HashData(
            Encoding.UTF8.GetBytes(WorkspaceSchema.CreateSql.Replace("\r\n", "\n"))));

        Assert.Equal("a1e6b5f8fb7ed7fc6f5d5a9e5ab3a3f34f6c0a0a0a0a0a0a0a0a0a0a0a0a0a0", hash);
    }

    [Fact]
    public void AV1Store_MigratesToV2_CarryingEveryRowAndBackfillingNothingItCannotKnow()
    {
        SchemaV2Support.CreateV1Database(Path_);
        List<string> before;
        using (var raw = SchemaV2Support.OpenRaw(Path_, readOnly: true))
        {
            before = SchemaV2Support.Dump(raw, SchemaV2Support.V1Columns);
        }

        using (var store = WorkspaceStore.Open(Path_))
        {
            Assert.False(store.Report.ReadOnly);
            Assert.Equal(1, store.Report.MigratedFromVersion);
            Assert.Equal(2, store.Report.FileVersion);
            Assert.Equal(2, store.Report.LegacyNotebooksBackfilled);

            using var reader = store.BeginRead();
            Assert.Equal(DispatchState.PtyWriteAccepted, reader.ReadDispatchReceipt("key-a")!.State);
            Assert.Equal(DispatchState.DeliveryUnknown, reader.ReadDispatchReceipt("key-b")!.State);
        }

        SqliteConnection.ClearAllPools();
        using var after = SchemaV2Support.OpenRaw(Path_, readOnly: true);

        Assert.Equal(before, SchemaV2Support.Dump(after, SchemaV2Support.V1Columns));
        Assert.Equal(2, SchemaV2Support.Count(after, "SELECT max(version) FROM schema_version"));
        Assert.Equal(2, SchemaV2Support.Count(after, "SELECT count(*) FROM schema_version"));

        // Legacy notebooks: one per draft, no owner, flagged legacy. Nothing the migration cannot know.
        Assert.Equal(2, SchemaV2Support.Count(after,
            "SELECT count(*) FROM notebook_dim WHERE legacy = 1 AND owner_journey_id IS NULL AND version_no = 1"));
        Assert.Equal(2, SchemaV2Support.Count(after, "SELECT count(DISTINCT notebook_id) FROM notebook_dim"));
        Assert.Equal(0, SchemaV2Support.Count(after, "SELECT count(*) FROM journey_dim"));

        // body_sha / parent_revision_no stay NULL = Not recorded; no new attempt/outcome column is guessed.
        Assert.Equal(0, SchemaV2Support.Count(after,
            "SELECT count(*) FROM prompt_revision_fact WHERE body_sha IS NOT NULL OR parent_revision_no IS NOT NULL"));
        Assert.Equal(0, SchemaV2Support.Count(after, """
            SELECT count(*) FROM dispatch_attempt_fact WHERE projection_sha IS NOT NULL OR route_fingerprint IS NOT NULL
               OR profile_key IS NOT NULL OR scope_snapshot IS NOT NULL OR parent_dispatch_key IS NOT NULL
               OR queued_after_dispatch_key IS NOT NULL
            """));
        Assert.Equal(0, SchemaV2Support.Count(after,
            "SELECT count(*) FROM dispatch_outcome_fact WHERE cause IS NOT NULL OR detail IS NOT NULL"));

        // The snapshot is only deleted once the gate has passed; a clean run leaves none behind.
        Assert.Empty(Directory.GetFiles(_directory, "*.pre-migration.db", SearchOption.AllDirectories));
    }

    [Fact]
    public void AMigratedStore_AcceptsTheV2Api()
    {
        SchemaV2Support.CreateV1Database(Path_);

        using var store = WorkspaceStore.Open(Path_);
        SchemaV2Support.SeedEveryV2Table(store);
        using var writer = store.BeginWrite();
        writer.AppendNotebook(new NotebookRow("draft-a", "j1", Legacy: false));
        writer.Commit();

        using var reader = store.BeginRead();
        Assert.False(reader.ReadNotebook("draft-a")!.Legacy);
        Assert.Equal(2, reader.ReadNotebook("draft-a")!.VersionNo);
    }

    [Fact]
    public void AFailedMigration_PutsTheV1StoreBack_AndOpenReportsIt()
    {
        SchemaV2Support.CreateV1Database(Path_);
        using (var raw = SchemaV2Support.OpenRaw(Path_))
        {
            // A table the migration needs to create, already present with the wrong shape: the DDL throws.
            SchemaV2Support.Run(raw, "CREATE TABLE journey_dim (stray TEXT);");
        }

        List<string> before;
        using (var raw = SchemaV2Support.OpenRaw(Path_, readOnly: true))
        {
            before = SchemaV2Support.Dump(raw, SchemaV2Support.V1Columns);
        }

        var ex = Assert.Throws<WorkspaceStoreException>(() => WorkspaceStore.Open(Path_));
        Assert.Equal(StoreErrorCodes.MigrationFailed, ex.ErrorCode);

        using var after = SchemaV2Support.OpenRaw(Path_, readOnly: true);
        Assert.Equal(1, SchemaV2Support.Count(after, "SELECT max(version) FROM schema_version"));
        Assert.Equal(before, SchemaV2Support.Dump(after, SchemaV2Support.V1Columns));
    }
}

/// <summary>ADR-0039 §5: a fresh v2 store and a migrated one are the same schema.</summary>
public sealed class FreshVersusMigratedTests : IDisposable
{
    private readonly string _directory = SchemaV2Support.NewDirectory();

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void AFreshStoreAndAMigratedOneHaveTheSameSqliteMaster()
    {
        var freshPath = Path.Combine(_directory, "fresh.db");
        var migratedPath = Path.Combine(_directory, "migrated.db");
        WorkspaceStore.Open(freshPath).Dispose();
        SchemaV2Support.CreateV1Database(migratedPath);
        WorkspaceStore.Open(migratedPath).Dispose();
        SqliteConnection.ClearAllPools();

        using var fresh = SchemaV2Support.OpenRaw(freshPath, readOnly: true);
        using var migrated = SchemaV2Support.OpenRaw(migratedPath, readOnly: true);
        var freshObjects = SchemaV2Support.SchemaObjects(fresh);

        Assert.Equal(freshObjects, SchemaV2Support.SchemaObjects(migrated));

        // Not vacuous: the v2 tables and their immutability triggers are in the compared set.
        foreach (var table in WorkspaceSchema.FactTables)
        {
            Assert.Contains(freshObjects, o => o.StartsWith($"trigger trg_{table}_no_update on {table}:", StringComparison.Ordinal));
            Assert.Contains(freshObjects, o => o.StartsWith($"trigger trg_{table}_no_delete on {table}:", StringComparison.Ordinal));
        }

        Assert.Contains(freshObjects, o => o.StartsWith("table journey_dim ", StringComparison.Ordinal));
        Assert.Equal(2, WorkspaceSchema.Version);
    }
}

/// <summary>ADR-0039 §5: a binary older than the file opens it read-only, with a report.</summary>
public sealed class ReadOnlyOnNewerFileTests : IDisposable
{
    private readonly string _directory = SchemaV2Support.NewDirectory();

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void ABinaryOlderThanTheFile_OpensReadOnly_WithAReport_AndCannotWrite()
    {
        var path = Path.Combine(_directory, "workspace.db");
        WorkspaceStore.Open(path).Dispose();
        long epochBefore;
        using (var raw = SchemaV2Support.OpenRaw(path))
        {
            SchemaV2Support.Run(raw, "INSERT INTO schema_version (version, applied_at) VALUES (99, '2099-01-01T00:00:00Z');");
            epochBefore = SchemaV2Support.Count(raw, "SELECT core_epoch FROM core_state");
        }

        SqliteConnection.ClearAllPools();
        using var store = WorkspaceStore.Open(path);

        Assert.True(store.Report.ReadOnly);
        Assert.Equal(99, store.Report.FileVersion);
        Assert.Equal(WorkspaceSchema.Version, store.Report.BinaryVersion);
        Assert.Contains("99", store.Report.Summary, StringComparison.Ordinal);

        var ex = Assert.Throws<WorkspaceStoreException>(() => store.BeginWrite());
        Assert.Equal(StoreErrorCodes.NewerSchemaReadOnly, ex.ErrorCode);

        using (var reader = store.BeginRead())
        {
            Assert.Empty(reader.ListJourneys());
        }

        store.Dispose();
        SqliteConnection.ClearAllPools();
        using var after = SchemaV2Support.OpenRaw(path, readOnly: true);
        Assert.Equal(epochBefore, SchemaV2Support.Count(after, "SELECT core_epoch FROM core_state"));
        Assert.Equal(2, SchemaV2Support.Count(after, "SELECT count(*) FROM schema_version"));
    }
}

/// <summary>ADR-0039 §5: snapshot and restore are WAL-safe — pre-migration rows only, no replay.</summary>
public sealed class WalSafeSnapshotTests : IDisposable
{
    private readonly string _directory = SchemaV2Support.NewDirectory();

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void WriteUnderWal_Capture_WriteMore_Restore_YieldsPreMigrationRowsOnly()
    {
        var path = Path.Combine(_directory, "workspace.db");
        var store = WorkspaceStore.Open(path);
        using (var writer = store.BeginWrite())
        {
            writer.SavePromptRevision("pre", 1, "kept one");
            writer.SavePromptRevision("pre", 2, "kept two");
            writer.Commit();
        }

        // The store is still open: these rows live in the -wal file, not yet in the main file.
        var snapshot = StoreSnapshot.Capture(path, Path.Combine(_directory, "snap"));

        using (var writer = store.BeginWrite())
        {
            writer.SavePromptRevision("post", 1, "must not survive");
            writer.Commit();
        }

        store.Dispose();
        SqliteConnection.ClearAllPools();
        StoreSnapshot.Restore(snapshot, path);

        WorkspaceStore.Open(path).Dispose();
        SqliteConnection.ClearAllPools();
        using var raw = SchemaV2Support.OpenRaw(path, readOnly: true);
        Assert.Equal(2, SchemaV2Support.Count(raw, "SELECT count(*) FROM prompt_revision_fact WHERE draft_id = 'pre'"));
        Assert.Equal(0, SchemaV2Support.Count(raw, "SELECT count(*) FROM prompt_revision_fact WHERE draft_id = 'post'"));
    }

    [Fact]
    public void Restore_DeletesTheStaleWalAndShmSidecars_SoNothingReplaysOverTheSnapshot()
    {
        var path = Path.Combine(_directory, "workspace.db");
        WorkspaceStore.Open(path).Dispose();
        SqliteConnection.ClearAllPools();
        var snapshot = StoreSnapshot.Capture(path, Path.Combine(_directory, "snap"));
        File.WriteAllText(path + "-wal", "stale frames from after the snapshot");
        File.WriteAllText(path + "-shm", "stale index");

        StoreSnapshot.Restore(snapshot, path);

        Assert.False(File.Exists(path + "-wal"));
        Assert.False(File.Exists(path + "-shm"));
    }
}
