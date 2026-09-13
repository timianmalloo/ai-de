using System.Text.Json;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Perspectives;

/// <summary>
/// <b>ADR-0032 as amended by Ruling 84</b> (<c>note-adr-0030-0032-amendment-coordination</c>): a
/// third zone-envelope file for the third host perspective, its own §B4 default, and test 1
/// extended — a pre-Addendum-C Coding envelope drops the Loomkeeper kinds and reports them naming
/// Coordination. Headless on the store, the host's service and <see cref="LayoutPersistence"/>,
/// over the PRODUCT's kind rows (<see cref="DockHost.AdmissionFor(Perspective)"/>) and the product's
/// zone-backed service (DC-135). Seen red first: <c>docs/proof/coordination-perspective.md</c>.
/// </summary>
public sealed class ZoneLayoutSlotsTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aide-slots-c-" + Guid.NewGuid().ToString("N"));

    private string LayoutPath => Path.Combine(_dir, "layout.json");

    private static Perspective Coordination => PerspectiveSet.All.Single(p => p.Id == "coordination");

    private static IReadOnlySet<string> Kinds => SurfaceContentFactory.KnownKinds.ToHashSet(StringComparer.Ordinal);

    private static IReadOnlySet<string> NoSurfaces => new HashSet<string>(StringComparer.Ordinal);

    private static ZoneBackedLayoutService Host(Perspective row) => new(DockHost.AdmissionFor(row));

    private LayoutPersistence PersistenceFor(ZoneBackedLayoutService host) =>
        new(host, LayoutPath, NoSurfaces, restorableKinds: Kinds);

    private static string GoldenPath => Path.Combine(RepoRoot(), "tests", "AiDe.App.Tests", "Fixtures", "pre-perspectives.zones.json");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("AiDe.sln was not found above the test directory");
    }

    private static List<string> Ids(WorkbenchLayout layout, ZoneId zone) =>
        [.. layout.Zone(zone).Surfaces().Select(s => s.SurfaceId)];

    // ADR-0032 rule 1 (a new host perspective is a new FILE, never a schema field) — Coordination's
    // slot is the sibling `<layout>.coordination.zones.json`, written and restored independently of
    // Coding's grandfathered file and Architecture's sibling; and §B4's third default table, as
    // ruled (Ruling 84; the arrangement is the Owner's, Inferred until the operator runs it): Left =
    // Terminal sessions · Center = Ledger · Leaderboard · Message board, Ledger first · Right empty ·
    // Bottom empty and collapsed · Daydreams admitted, not in the default. A schema field, a fourth
    // Center tab, a Bottom that renders, or a seed the host has to drop from, fails.
    [Fact]
    public void CoordinationPersistsToItsOwnFile_AndItsDefaultIsLeftSessionsCenterLedgerLeaderboardBoard_BottomCollapsed()
    {
        Directory.CreateDirectory(_dir);
        var coordination = Coordination;

        // The slot: its own file, distinct from the other two hosts', by the one map.
        var slot = LayoutPersistence.SlotPathFor(LayoutPath, coordination);
        Assert.Equal(Path.Combine(_dir, "layout.coordination.zones.json"), slot);
        Assert.NotEqual(LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Coding), slot);
        Assert.NotEqual(LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Architecture), slot);

        // The default, from the host's own service (what the product seeds): nothing dropped.
        var host = Host(coordination);
        Assert.Empty(host.DefaultDropped);
        var layout = host.Zones;

        Assert.Equal(["sessions"], Ids(layout, ZoneId.Left));
        Assert.Equal(["ledger", "leaderboard", "board"], Ids(layout, ZoneId.Center));
        Assert.Equal(["ledger", "leaderboard", "board"], layout.Zone(ZoneId.Center).Surfaces().Select(s => s.Kind));
        Assert.Equal(0, Assert.IsType<ZoneStack>(layout.Zone(ZoneId.Center).Content).ActiveIndex);   // Ledger first
        Assert.Null(layout.Zone(ZoneId.Right).Content);
        Assert.Null(layout.Zone(ZoneId.Bottom).Content);
        Assert.True(layout.Zone(ZoneId.Bottom).Collapsed);
        Assert.False(layout.Zone(ZoneId.Left).Collapsed);
        Assert.False(layout.Zone(ZoneId.Center).Collapsed);
        Assert.Empty(layout.Floating);
        Assert.Null(layout.Maximized);
        Assert.DoesNotContain(layout.AllSurfaces(), s => s.Kind == "daydreams");
        Assert.Equal(4, layout.AllSurfaces().Count());   // what the switch announces: "4 panes"

        // The captions are the kind rows' titles (Ruling 62: "Terminal sessions", never "Sessions").
        Assert.Equal("Terminal sessions", layout.Zone(ZoneId.Left).Surfaces().Single().Title);
        Assert.Equal(["Ledger", "Leaderboard", "Message board"], layout.Zone(ZoneId.Center).Surfaces().Select(s => s.Title));

        // Every placed kind is one Coordination admits, at most once for a one-instance kind.
        var admission = DockHost.AdmissionFor(coordination);
        Assert.All(layout.AllSurfaces(), s => Assert.True(admission.Admits(s.Kind), $"'{s.Kind}' is not admitted by Coordination"));
        Assert.Equal(layout.AllSurfaces().Count(), layout.AllSurfaces().Select(s => s.Kind).Distinct().Count());

        // The file: a save writes the third file only; a fresh host restores the arrangement from it.
        using (var persistence = PersistenceFor(host))
        {
            Assert.True(host.Apply(new LayoutOperation.AddSurface(ZonesToTree.CenterStackId, new Surface("dd-1", "daydreams", "Daydreams"))).Applied);
            persistence.SaveNow();
            Assert.Null(persistence.LastSaveFailure);
        }

        Assert.True(File.Exists(slot), "the Coordination slot was not written");
        Assert.False(File.Exists(LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Coding)), "a Coordination save wrote Coding's file");
        Assert.False(File.Exists(LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Architecture)), "a Coordination save wrote Architecture's file");
        Assert.Equal(1, JsonDocument.Parse(File.ReadAllText(slot)).RootElement.GetProperty("schemaVersion").GetInt32());   // no schema bump

        var reopened = Host(coordination);
        using var again = PersistenceFor(reopened);
        var result = again.Restore();

        Assert.True(again.LastRestoreAppliedASavedArrangement);
        Assert.Empty(again.LastRestoreDropped);
        Assert.False(result.WasDefaulted);
        Assert.Equal(["ledger", "leaderboard", "board", "dd-1"], Ids(reopened.Zones, ZoneId.Center));
        Assert.Equal(["sessions"], Ids(reopened.Zones, ZoneId.Left));
        Assert.True(reopened.Zones.Zone(ZoneId.Bottom).Collapsed);
    }

    // ADR-0032 test 1 extended (Ruling 84 CONDITIONS; the amendment note's ADR-0032 row): the golden
    // pre-Addendum-C Coding file — the pre-C default plus one opened class diagram — restores into
    // Coding with ONLY what Coding still admits (the terminal); the four Loomkeeper kinds it carries
    // are dropped beside the seven Architecture kinds, each reported by caption and kind and by the
    // perspective that admits it — Coordination, with its gesture, for the four; Architecture for the
    // seven — and the event carries the dropped count and kinds. A Loomkeeper kind surviving in the
    // Coding slot, a report that does not name Coordination, or a crash, fails. Moved here from
    // PerspectiveLayoutSlotTests (its pre-amendment form kept the four in Coding and named
    // Architecture alone). RED before the allow-lists moved: 7 dropped, five kinds surviving.
    [Fact]
    public void RestoringAPreCCodingEnvelope_DropsTheFiveLoomkeeperKinds_AndReportsNamingCoordination()
    {
        Directory.CreateDirectory(_dir);
        File.Copy(GoldenPath, LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Coding));
        var coordination = Coordination;
        var host = Host(PerspectiveSet.Coding);
        using var persistence = PersistenceFor(host);

        var result = persistence.Restore();

        // fixture-derivation: ok — ADR-0032 test 1 names the exact surviving set; deriving it from the rows would make the oracle the filter under test
        Assert.Equal(["terminal"], host.Zones.AllSurfaces().Select(s => s.Kind).Distinct().OrderBy(k => k, StringComparer.Ordinal));
        Assert.DoesNotContain(host.Zones.AllSurfaces(), s => s.Kind is "sessions" or "board" or "leaderboard" or "ledger" or "daydreams");
        Assert.True(persistence.LastRestoreAppliedASavedArrangement);
        Assert.False(result.WasDefaulted);

        var dropped = persistence.LastRestoreDropped;
        Assert.Equal(11, dropped.Count);
        Assert.Equal(
            ["board", "canvas", "classdiagram", "contexts", "inspector", "joins", "leaderboard", "ledger", "sessions", "view", "view"],
            dropped.Select(d => d.Surface.Kind).OrderBy(k => k, StringComparer.Ordinal));
        Assert.All(dropped, d => Assert.Equal(DropReason.KindNotAdmitted, d.Reason));
        Assert.All(dropped.Where(d => d.Surface.Kind is "sessions" or "board" or "leaderboard" or "ledger"), d => Assert.Same(coordination, d.AdmittedBy));
        Assert.All(dropped.Where(d => d.Surface.Kind is not ("sessions" or "board" or "leaderboard" or "ledger")), d => Assert.Same(PerspectiveSet.Architecture, d.AdmittedBy));
        Assert.Equal(LayoutErrorCodes.PartialRestore, result.ErrorCode);

        // Reported by caption AND kind, naming BOTH admitting perspectives with their gestures —
        // through the same channel that reports a dropped surface today (spec §C4's form).
        var said = result.Announcement;
        Assert.StartsWith("11 panes from your saved layout aren't available in Coding — ", said, StringComparison.Ordinal);
        Assert.Contains("Sessions (terminal sessions)", said, StringComparison.Ordinal);   // the saved caption, then the kind's title
        Assert.Contains("Board (message board)", said, StringComparison.Ordinal);
        Assert.Contains("Leaderboard", said, StringComparison.Ordinal);
        Assert.Contains("Ledger", said, StringComparison.Ordinal);
        Assert.Contains("Class diagram", said, StringComparison.Ordinal);
        Assert.Contains("Domain (evidence)", said, StringComparison.Ordinal);
        Assert.Contains("live in Coordination (Ctrl+4)", said, StringComparison.Ordinal);
        Assert.Contains("live in Architecture (Ctrl+3)", said, StringComparison.Ordinal);
        Assert.Contains(result.MissingSurfaces, m => m.StartsWith("Graph", StringComparison.Ordinal));
        Assert.Contains(result.MissingSurfaces, m => m.StartsWith("Ledger", StringComparison.Ordinal));
    }

    // The operator's own 2026-09-13 layout (screenshots 4 and 5): the four Loomkeeper tabs in
    // Coding's Center under their saved captions, a terminal below. The report is the one sentence
    // DESIGN.md's errata renders, naming Coordination and its gesture — "They live in Coordination
    // (Ctrl+4)" — and the four are gone from Coding. Daydreams alone takes the singular form.
    [Theory]
    [InlineData(false, "4 panes from your saved layout aren't available in Coding — Ledger, Leaderboard, Sessions (terminal sessions), Board (message board). They live in Coordination (Ctrl+4); open them from its View menu.")]
    [InlineData(true, "1 pane from your saved layout isn't available in Coding — Daydreams. It lives in Coordination (Ctrl+4); open it from its View menu.")]
    public void TheOperatorsPreCCodingLayout_ReportsTheLoomkeeperTabsInOneSentence_NamingCoordinationAndItsGesture(bool daydreamsOnly, string expected)
    {
        Directory.CreateDirectory(_dir);
        var center = daydreamsOnly
            ? new ZoneStack([new Surface("daydreams", "daydreams", "Daydreams")])
            : new ZoneStack(
            [
                new Surface("ledger", "ledger", "Ledger"),
                new Surface("leaderboard", "leaderboard", "Leaderboard"),
                new Surface("sessions", "sessions", "Sessions"),
                new Surface("board", "board", "Board"),
            ]);
        var saved = WorkbenchLayout.Empty()
            .WithZone(new ZoneState(ZoneId.Center, center, 1.0, Collapsed: false))
            .WithZone(new ZoneState(ZoneId.Bottom, new ZoneStack([new Surface("terminal-1", "terminal", "Terminal — pwsh")]), 0.3, Collapsed: false));
        new ZoneLayoutStore(LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Coding)).Save(saved);
        var host = Host(PerspectiveSet.Coding);
        using var persistence = PersistenceFor(host);

        var result = persistence.Restore();

        Assert.Equal(expected, result.Announcement);
        Assert.Equal(["terminal-1"], Ids(host.Zones, ZoneId.Bottom));
        Assert.Null(host.Zones.Zone(ZoneId.Center).Content);
        Assert.All(persistence.LastRestoreDropped, d => Assert.Same(Coordination, d.AdmittedBy));
        Assert.False(result.WasDefaulted);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}
