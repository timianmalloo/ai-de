using AiDe.Core.Workbench;

namespace AiDe.Core.Tests;

/// <summary>
/// ADR-0012's "layout round-trip across an app upgrade" spike, made permanent.
///
/// ADR-0013 put layouts in a versioned envelope specifically so an upgrade would have a migration
/// hook. The version FIELD existed from day one; the HOOK did not — an older file was read as-is,
/// so the first release that renamed a surface would silently drop it from every saved layout.
/// These tests pin the hook.
/// </summary>
public sealed class LayoutUpgradeTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "aide-upgrade", Guid.NewGuid().ToString("N"));

    private string Path_ => Path.Combine(_dir, "layout.json");

    /// <summary>The surface set a LATER release ships, after renaming the terminal surface.</summary>
    private static readonly HashSet<string> V2Surfaces =
        ["explore", "domain", "graph", "contexts", "provenance", "terminal.session.1"];

    private void WriteV1Layout()
    {
        // A layout saved by the release that called the terminal surface "terminal-1".
        var store = new LayoutStore(Path_, appVersion: "0.3.0");
        store.Save(Layout.Default());
    }

    // The upgrade path: an old file, a new app, and a surface that was renamed between them.
    [Fact]
    public void AnOlderLayout_IsMigratedRatherThanSilentlyLosingRenamedSurfaces()
    {
        WriteV1Layout();

        // assumedCurrentVersion: 2 simulates the LATER build — the file is v1, the app is v2, and
        // the gap between them is what the migration chain exists to cross.
        var store = new LayoutStore(Path_, appVersion: "0.4.0", migrations: LayoutMigrations.Default);
        var result = store.Load(V2Surfaces, assumedCurrentVersion: 2);

        Assert.False(result.WasDefaulted);
        // The pane must survive the rename. Dropping it would silently delete part of an
        // arrangement the user built.
        Assert.Empty(result.MissingSurfaces);
        Assert.Contains(result.Layout.AllStacks().SelectMany(s => s.Surfaces),
            s => s.SurfaceId == "terminal.session.1");
        result.Layout.AssertInvariant();
    }

    [Fact]
    public void AMigratedLayout_IsRewrittenAtTheCurrentSchemaVersion()
    {
        WriteV1Layout();
        var store = new LayoutStore(Path_, appVersion: "0.4.0", migrations: LayoutMigrations.Default);

        store.Load(V2Surfaces, assumedCurrentVersion: 2);

        // Rewritten on read, so the migration is paid once rather than on every launch.
        Assert.Contains($"\"schemaVersion\": {LayoutStore.CurrentSchemaVersion}",
            File.ReadAllText(Path_), StringComparison.Ordinal);
    }

    [Fact]
    public void ALayoutAlreadyAtTheCurrentVersion_IsNotMigrated()
    {
        var store = new LayoutStore(Path_, appVersion: "0.4.0", migrations: LayoutMigrations.Default);
        store.Save(Layout.Default());
        var before = File.GetLastWriteTimeUtc(Path_);

        var result = store.Load(new HashSet<string> { "explore", "domain", "graph", "contexts", "provenance", "terminal-1" });

        Assert.False(result.WasDefaulted);
        Assert.Null(result.ErrorCode);
        Assert.Equal(before, File.GetLastWriteTimeUtc(Path_));
    }

    // An old file with no migration available must degrade honestly, not be read as if current.
    [Fact]
    public void AnOlderLayout_WithNoMigrationPath_DegradesRatherThanGuessing()
    {
        WriteV1Layout();

        // A store that knows about a v3 schema but has no way to get there from v1.
        var store = new LayoutStore(Path_, appVersion: "0.9.0", migrations: []);
        var result = store.Load(V2Surfaces, assumedCurrentVersion: 3);

        Assert.True(result.WasDefaulted);
        Assert.Equal(LayoutErrorCodes.VersionUnsupported, result.ErrorCode);
        Assert.Contains("could not be upgraded", result.Announcement, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(store.BackupPath));
    }

    [Fact]
    public void MigrationsRun_InOrder_AcrossMultipleVersions()
    {
        var applied = new List<int>();
        var migrations = new LayoutMigration[]
        {
            new(1, dto => { applied.Add(1); return dto; }),
            new(2, dto => { applied.Add(2); return dto; }),
        };

        WriteV1Layout();
        var store = new LayoutStore(Path_, appVersion: "0.5.0", migrations: migrations);
        store.Load(new HashSet<string> { "explore", "domain", "graph", "contexts", "provenance", "terminal-1" }, assumedCurrentVersion: 3);

        Assert.Equal([1, 2], applied);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }
}
