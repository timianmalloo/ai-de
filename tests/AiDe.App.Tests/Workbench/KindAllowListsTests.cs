using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Perspectives;

/// <summary>
/// <b>ADR-0045 §1(b) and the WL-S design's §3</b> — the allow-list column after the re-row: the five
/// Loomkeeper kinds (<c>sessions · board · leaderboard · ledger · daydreams</c>) are admitted under
/// Factory → Coordination, as a contiguous group; no kind is lost (every row is admitted by at least
/// one perspective); Review admits none until WL-5. Over the PRODUCT's rows and the product's
/// admission (<see cref="DockHost.AdmissionFor(Perspective)"/>), never a test copy (DC-135). Seen red
/// first against Ruling 84's Coordination-only column:
/// <c>docs/proof/workbench-loops-wl-s/sp3/kind-census-red.txt</c>.
/// </summary>
public sealed class KindAllowListsTests
{
    // fixture-derivation: ok — ADR-0045 §1(b) names these five as THE set; deriving them from the rows would make the oracle the column under test
    private static readonly string[] Loomkeeper = ["sessions", "board", "leaderboard", "ledger", "daydreams"];

    private static Perspective Row(string id) => Assert.Single(PerspectiveSet.All, p => p.Id == id);

    [Fact]
    public void TheFiveLoomkeeperKinds_AreAdmittedUnderFactory_AsOneContiguousGroup()
    {
        var factory = Row("factory");
        var host = DockHost.AdmissionFor(factory);

        foreach (var kind in Loomkeeper)
        {
            var row = Assert.Single(SurfaceContentFactory.Kinds, k => k.Kind == kind);

            // The column, literally: one perspective, and it is Factory.
            Assert.Equal(["factory"], row.Perspectives.Select(p => p.Id));
            Assert.Equal(SurfaceContentFactory.Instances.One, row.Instances);

            // The zone rule: an open docks it in the Center, never beside the session document in the
            // Left, where it would take Factory's landing (SH-4.2; the design's §3).
            Assert.Equal(ZoneId.Center, row.Zone);

            Assert.True(host.Admits(kind), $"Factory does not admit '{kind}'");
            Assert.True(host.IsOneInstance(kind));

            // Where an open lands (US-C3): from every perspective, Factory.
            foreach (var active in PerspectiveSet.All)
            {
                Assert.Same(factory, PerspectiveMenu.Resolve(kind, active));
            }
        }

        // "Factory → Coordination" is a group: the five rows are contiguous in the kind list, in the
        // View menu's order, so the derived menu lists them together.
        var order = SurfaceContentFactory.Kinds.Select(k => k.Kind).ToList();
        var first = order.IndexOf("sessions");
        Assert.Equal(Loomkeeper, order.Skip(first).Take(Loomkeeper.Length));
    }

    // No kind silently lost by the re-row (ADR-0045 §1; the brief's "keep every existing kind admitted
    // somewhere"), and the whole column, literally, so a row moved between perspectives fails here.
    [Fact]
    public void EveryKindIsAdmittedSomewhere_AndTheColumnIsTheDesignsTable()
    {
        var expected = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["canvas"] = ["understand"],
            ["view"] = ["understand"],
            ["classdiagram"] = ["understand"],
            ["sequence"] = ["understand"],
            ["contexts"] = ["understand"],
            ["joins"] = ["understand"],
            ["solution-tree"] = ["understand"],
            ["entry-points"] = ["understand"],
            ["terminal"] = ["factory"],
            ["sessions"] = ["factory"],
            ["board"] = ["factory"],
            ["leaderboard"] = ["factory"],
            ["ledger"] = ["factory"],
            ["daydreams"] = ["factory"],
            ["prompt"] = ["factory"],
            ["search"] = ["factory"],
            ["codeviewer"] = ["factory", "understand"],
            ["diagnostics"] = ["factory"],
            ["notebook"] = ["factory"],
            ["console"] = ["factory"],
            ["execution"] = ["factory"],
        };

        Assert.Equal(expected.Keys.Order(StringComparer.Ordinal), SurfaceContentFactory.Kinds.Select(k => k.Kind).Order(StringComparer.Ordinal));

        foreach (var row in SurfaceContentFactory.Kinds)
        {
            Assert.NotEmpty(row.Perspectives);
            Assert.Equal(expected[row.Kind], row.Perspectives.Select(p => p.Id).Order(StringComparer.Ordinal));
        }

        // Review admits no kind until WL-5 lands its own (the design's D3).
        Assert.DoesNotContain(SurfaceContentFactory.Kinds, k => k.Perspectives.Any(p => p.Id == "review"));

        // The one shared kind routes to the reading host from Review, and stays where it is admitted.
        Assert.Same(Row("understand"), PerspectiveMenu.Resolve("codeviewer", Row("review")));
        Assert.Same(Row("factory"), PerspectiveMenu.Resolve("codeviewer", Row("factory")));
        Assert.Same(Row("understand"), PerspectiveMenu.Resolve("codeviewer", Row("understand")));
    }
}
