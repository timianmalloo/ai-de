using System.Text.Json;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Perspectives;

/// <summary>
/// <b>ADR-0032 under ADR-0045</b>: Review's zone-envelope file and its empty default, and test 1
/// re-scoped — a pre-Addendum-C Coding envelope, read into Factory, keeps the Loomkeeper kinds
/// (admitted under Factory → Coordination) and reports the reading kinds naming Understand. Headless on the store, the host's service and <see cref="LayoutPersistence"/>,
/// over the PRODUCT's kind rows (<see cref="DockHost.AdmissionFor(Perspective)"/>) and the product's
/// zone-backed service (DC-135). Seen red first: <c>docs/proof/coordination-perspective.md</c>.
/// </summary>
public sealed class ZoneLayoutSlotsTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aide-slots-c-" + Guid.NewGuid().ToString("N"));

    private string LayoutPath => Path.Combine(_dir, "layout.json");

    // What the shell hands LayoutPersistence (Ruling 94): the buildable kinds plus the retired ones, so a
    // saved envelope carrying a retired kind is dropped by the host WITH a report, never by the store in silence.
    private static IReadOnlySet<string> Kinds => SurfaceContentFactory.RestorableKinds;

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

    // ADR-0032 rule 1 (a new host perspective is a new FILE, never a schema field) under ADR-0045:
    // Review's slot is the sibling `<layout>.review.zones.json`, written independently of Factory's
    // grandfathered file and Understand's (Architecture-named, until T11) sibling; and Review's
    // default is four empty zones — it admits no kind until WL-5 (the WL-S design's D3), so there is
    // nothing to seed and nothing to drop. A schema field, a seeded pane, or a save that writes
    // another host's file, fails.
    [Fact]
    public void ReviewPersistsToItsOwnFile_AndItsDefaultIsFourEmptyZones()
    {
        Directory.CreateDirectory(_dir);
        var review = PerspectiveSet.Review;

        var slot = LayoutPersistence.SlotPathFor(LayoutPath, review);
        Assert.Equal(Path.Combine(_dir, "layout.review.zones.json"), slot);
        Assert.NotEqual(LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Factory), slot);
        Assert.NotEqual(LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Understand), slot);

        var host = Host(review);
        Assert.Empty(host.DefaultDropped);
        var layout = host.Zones;
        Assert.Empty(layout.AllSurfaces());
        Assert.All(Enum.GetValues<ZoneId>(), z => Assert.False(layout.Zone(z).Collapsed, $"{z} is collapsed in Review's default"));
        Assert.Empty(layout.Floating);
        Assert.Null(layout.Maximized);

        using (var persistence = PersistenceFor(host))
        {
            persistence.SaveNow();
            Assert.Null(persistence.LastSaveFailure);
        }

        Assert.True(File.Exists(slot), "the Review slot was not written");
        Assert.False(File.Exists(LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Factory)), "a Review save wrote Factory's file");
        Assert.False(File.Exists(LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Understand)), "a Review save wrote Understand's file");
        Assert.Equal(1, JsonDocument.Parse(File.ReadAllText(slot)).RootElement.GetProperty("schemaVersion").GetInt32());   // no schema bump
    }

    // ADR-0032 test 1 under ADR-0045 §1(b): the golden pre-Addendum-C Coding file — the pre-C default
    // plus one opened class diagram — restores into Factory (Coding's file, re-rowed) KEEPING the
    // four Loomkeeper kinds it carries (they are admitted under Factory → Coordination now) beside the
    // terminal; the six reading kinds are dropped, reported by the perspective that admits them —
    // Understand, with its gesture — and the one RETIRED kind (Ruling 94's `inspector`) by the ruling
    // that retired it. A Loomkeeper kind dropped, a report that does not name Understand, a Provenance
    // dropped in silence, or a crash, fails. RED before the re-row: 11 dropped, the four named
    // Coordination.
    [Fact]
    public void RestoringAPreCCodingEnvelope_KeepsTheLoomkeeperKindsInFactory_AndReportsTheReadingKindsNamingUnderstand()
    {
        Directory.CreateDirectory(_dir);
        File.Copy(GoldenPath, LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Factory));
        var host = Host(PerspectiveSet.Factory);
        using var persistence = PersistenceFor(host);

        var result = persistence.Restore();

        // fixture-derivation: ok — ADR-0032 test 1 names the exact surviving set; deriving it from the rows would make the oracle the filter under test
        Assert.Equal(["board", "leaderboard", "ledger", "sessions", "terminal"], host.Zones.AllSurfaces().Select(s => s.Kind).Distinct().Order(StringComparer.Ordinal));
        Assert.True(persistence.LastRestoreAppliedASavedArrangement);
        Assert.False(result.WasDefaulted);

        var dropped = persistence.LastRestoreDropped;
        Assert.Equal(
            // fixture-derivation: ok — ADR-0032 test 1 names the exact dropped set; deriving it from the rows would make the oracle the filter under test
            ["canvas", "classdiagram", "contexts", "inspector", "joins", "view", "view"],
            dropped.Select(d => d.Surface.Kind).OrderBy(k => k, StringComparer.Ordinal));
        Assert.All(dropped, d => Assert.Equal(DropReason.KindNotAdmitted, d.Reason));
        Assert.All(dropped.Where(d => d.Surface.Kind != "inspector"), d => Assert.Same(PerspectiveSet.Understand, d.AdmittedBy));
        Assert.Null(dropped.Single(d => d.Surface.Kind == "inspector").AdmittedBy);   // retired: no perspective is its home
        Assert.Equal(LayoutErrorCodes.PartialRestore, result.ErrorCode);

        var said = result.Announcement;
        Assert.StartsWith("7 panes from your saved layout aren't available in Factory — ", said, StringComparison.Ordinal);
        Assert.Contains("Understand (Ctrl+3)", said, StringComparison.Ordinal);
        Assert.Contains("Provenance (retired by Ruling 94: its origin, extractor and revision now show under the selected Evidence row)", said, StringComparison.Ordinal);
        Assert.Contains("Class diagram", said, StringComparison.Ordinal);
        Assert.Contains("Domain (evidence)", said, StringComparison.Ordinal);
        Assert.DoesNotContain("Ledger", said, StringComparison.Ordinal);
        Assert.DoesNotContain("Coordination", said, StringComparison.Ordinal);
        Assert.Contains(result.MissingSurfaces, m => m.StartsWith("Graph", StringComparison.Ordinal));
    }

    // The operator's own 2026-09-13 layout (screenshots 4 and 5): the four Loomkeeper tabs in
    // Coding's Center under their saved captions, a terminal below. Ruling 84 dropped them from that
    // slot with a report; ADR-0045 §1(b) admits them under Factory, so the arrangement now restores as
    // saved — the tabs where the operator put them, nothing dropped. Daydreams alone too.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheOperatorsPreCCodingLayout_RestoresTheLoomkeeperTabsIntoFactory_AsSaved(bool daydreamsOnly)
    {
        Directory.CreateDirectory(_dir);
        Surface[] tabs = daydreamsOnly
            ? [new Surface("daydreams", "daydreams", "Daydreams")]
            :
            [
                new Surface("ledger", "ledger", "Ledger"),
                new Surface("leaderboard", "leaderboard", "Leaderboard"),
                new Surface("sessions", "sessions", "Sessions"),
                new Surface("board", "board", "Board"),
            ];
        var saved = WorkbenchLayout.Empty()
            .WithZone(new ZoneState(ZoneId.Center, new ZoneStack([.. tabs]), 1.0, Collapsed: false))
            .WithZone(new ZoneState(ZoneId.Bottom, new ZoneStack([new Surface("terminal-1", "terminal", "Terminal — pwsh")]), 0.3, Collapsed: false));
        new ZoneLayoutStore(LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Factory)).Save(saved);
        var host = Host(PerspectiveSet.Factory);
        using var persistence = PersistenceFor(host);

        var result = persistence.Restore();

        Assert.Empty(persistence.LastRestoreDropped);
        Assert.True(persistence.LastRestoreAppliedASavedArrangement);
        Assert.False(result.WasDefaulted);
        Assert.Equal(tabs.Select(t => t.SurfaceId), Ids(host.Zones, ZoneId.Center));
        Assert.Equal(["terminal-1"], Ids(host.Zones, ZoneId.Bottom));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}
