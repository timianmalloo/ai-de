using AiDe.Core.Workbench;

namespace AiDe.Core.Tests;

/// <summary>
/// ADR-0013 — the owned versioned envelope. The contract US-9 sets is that a layout which cannot be
/// honoured degrades to the default **and says what was lost**, preserving the original file —
/// never a broken window, never a silently dropped surface.
/// </summary>
public sealed class WorkbenchStoreTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "aide-layout", Guid.NewGuid().ToString("N"));

    private string Path_ => Path.Combine(_dir, "layout.json");

    // DERIVED from the default layout, not typed. This list is "the surfaces this release ships",
    // which changes whenever one is added — and a hardcoded copy turns adding a surface into three
    // unrelated test failures that say nothing about persistence. It has already done so twice.
    private static readonly HashSet<string> AllSurfaces =
        [.. AiDe.Core.Workbench.Layout.Default().AllStacks()
            .SelectMany(stack => stack.Surfaces).Select(surface => surface.SurfaceId)];


    [Fact]
    public void Envelope_RoundTripsTheLayoutExactly()
    {
        var service = new LayoutService();
        service.Apply(new LayoutOperation.MoveSurface("domain",
            new DropTarget(service.Current.FindStackOf("provenance")!.Id, DropKind.SplitBottom)));
        service.Apply(new LayoutOperation.MoveSurface("terminal-1", new DropTarget("", DropKind.Float)));
        var expected = service.Current.Shape();

        var store = new LayoutStore(Path_);
        store.Save(service.Current);
        var restored = store.Load(AllSurfaces);

        Assert.False(restored.WasDefaulted);
        Assert.Null(restored.ErrorCode);
        Assert.Equal(expected, restored.Layout.Shape());
    }

    [Fact]
    public void Load_WithNoFile_StartsFromTheDefaultWithoutComplaining()
    {
        var result = new LayoutStore(Path_).Load(AllSurfaces);

        Assert.True(result.WasDefaulted);
        Assert.Null(result.ErrorCode);
        result.Layout.AssertInvariant();
    }

    // The degradation contract. Fails RED against a store that throws or returns a broken tree.
    [Fact]
    public void Load_Unreadable_FallsBackToDefaultAndPreservesTheOriginalFile()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path_, "{ this is not json");
        var store = new LayoutStore(Path_);

        var result = store.Load(AllSurfaces);

        Assert.True(result.WasDefaulted);
        Assert.Equal(LayoutErrorCodes.Unreadable, result.ErrorCode);
        Assert.Contains("kept", result.Announcement, StringComparison.OrdinalIgnoreCase);
        // The user's file is evidence of their intent — it must survive.
        Assert.True(File.Exists(store.BackupPath));
        result.Layout.AssertInvariant();
    }

    [Fact]
    public void Load_FromANewerSchema_DegradesRatherThanGuessing()
    {
        Directory.CreateDirectory(_dir);
        var store = new LayoutStore(Path_);
        store.Save(Layout.Default());
        File.WriteAllText(Path_, File.ReadAllText(Path_)
            .Replace("\"schemaVersion\": 1", "\"schemaVersion\": 99", StringComparison.Ordinal));

        var result = store.Load(AllSurfaces);

        Assert.True(result.WasDefaulted);
        Assert.Equal(LayoutErrorCodes.VersionUnsupported, result.ErrorCode);
        Assert.Contains("newer version", result.Announcement, StringComparison.OrdinalIgnoreCase);
    }

    // Partial restore must name what it lost, not say "some panes were unavailable".
    [Fact]
    public void Load_WithAMissingSurface_NamesItAndStillProducesAValidLayout()
    {
        var store = new LayoutStore(Path_);
        store.Save(Layout.Default());

        var result = store.Load(new HashSet<string> { "explore", "domain", "provenance" });   // terminal-1 gone

        Assert.Equal(LayoutErrorCodes.PartialRestore, result.ErrorCode);
        Assert.Contains("Terminal — pwsh", result.MissingSurfaces);
        Assert.Contains("Terminal — pwsh", result.Announcement, StringComparison.Ordinal);
        Assert.DoesNotContain(result.Layout.AllStacks().SelectMany(s => s.Surfaces),
            s => s.SurfaceId == "terminal-1");
        result.Layout.AssertInvariant();
    }

    [Fact]
    public void Load_WithAFloatingPaneOnADisconnectedDisplay_ReportsTheRehoming()
    {
        var service = new LayoutService();
        service.Apply(new LayoutOperation.MoveSurface("provenance", new DropTarget("", DropKind.Float)));
        var store = new LayoutStore(Path_);
        store.Save(service.Current);

        var result = store.Load(AllSurfaces, displayIsConnected: _ => false);

        Assert.Equal(LayoutErrorCodes.PartialRestore, result.ErrorCode);
        Assert.Contains("Provenance", result.RehomedFloating);
        Assert.Contains("not connected", result.Announcement, StringComparison.OrdinalIgnoreCase);
    }

    // A malformed-but-parseable tree must not be adopted: validate before trusting.
    [Fact]
    public void Load_WithAStructurallyInvalidTree_Degrades()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path_, """
            { "schemaVersion": 1, "appVersion": "0.3.0", "savedAt": "2026-08-26T00:00:00+00:00",
              "payload": { "Root": { "Id": "s", "Kind": "stack", "ActiveIndex": 0,
              "State": "Docked", "Surfaces": [] }, "Floating": [] } }
            """);

        var result = new LayoutStore(Path_).Load(AllSurfaces);

        Assert.True(result.WasDefaulted);
        Assert.Equal(LayoutErrorCodes.Unreadable, result.ErrorCode);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }
}
