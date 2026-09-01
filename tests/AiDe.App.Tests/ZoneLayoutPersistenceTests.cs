using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// dz-persist end-to-end through <see cref="LayoutPersistence"/>: a zone arrangement saved by one
/// session is restored faithfully by the next (US-9), and an absent save keeps the current arrangement
/// rather than resetting.
/// </summary>
public sealed class ZoneLayoutPersistenceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aide-zone-persist-" + Guid.NewGuid().ToString("N"));

    private string LayoutPath => Path.Combine(_dir, "layout.json");

    private static IReadOnlySet<string> Available => new HashSet<string>(StringComparer.Ordinal);

    private static IReadOnlySet<string> Kinds => new HashSet<string>(
        new[] { "view", "canvas", "terminal", "inspector", "sessions", "board", "leaderboard", "contexts", "joins" },
        StringComparer.Ordinal);

    [Fact]
    public void AZoneArrangement_IsSavedAndRestored_AcrossSessions()
    {
        // Session 1: move the terminal to the Right zone, collapse the Left, and save.
        var svc1 = new ZoneBackedLayoutService();
        svc1.Apply(new LayoutOperation.MoveSurface(
            "terminal-1", new DropTarget(ZonesToTree.RightStackId, DropKind.JoinStack)));
        svc1.Apply(new LayoutOperation.SetStackState(ZonesToTree.LeftStackId, StackState.Collapsed));

        using (var p1 = new LayoutPersistence(svc1, LayoutPath, Available, restorableKinds: Kinds))
        {
            p1.SaveNow();
        }

        // Session 2: a fresh service restores the saved arrangement.
        var svc2 = new ZoneBackedLayoutService();
        using var p2 = new LayoutPersistence(svc2, LayoutPath, Available, restorableKinds: Kinds);
        var result = p2.Restore();

        Assert.False(result.WasDefaulted);
        Assert.Equal(ZoneId.Right, svc2.Zones.FindZoneOf("terminal-1")); // placement returned
        Assert.True(svc2.Zones.Zone(ZoneId.Left).Collapsed);             // collapsed state returned
    }

    [Fact]
    public void WithNoSavedLayout_RestoreKeepsTheCurrentArrangement_WithoutResetting()
    {
        var svc = new ZoneBackedLayoutService();
        svc.Apply(new LayoutOperation.AddSurface(ZonesToTree.RightStackId, new Surface("outline", "inspector", "Outline")));

        using var p = new LayoutPersistence(svc, LayoutPath, Available, restorableKinds: Kinds);
        var result = p.Restore(); // no file on disk

        Assert.False(result.WasDefaulted);
        Assert.Equal(ZoneId.Right, svc.Zones.FindZoneOf("outline")); // current arrangement kept, not reset
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) { Directory.Delete(_dir, recursive: true); } }
        catch (IOException) { }
    }
}
