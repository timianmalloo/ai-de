using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Perspectives;

/// <summary>
/// <b>ADR-0030 rule 2 as amended by Ruling 84</b> — the allow-list column's fifth column: the five
/// Loomkeeper kinds (<c>sessions · board · leaderboard · ledger · daydreams</c>) are admitted by
/// Coordination and by nothing else, as a set; Coding admits none of them; a Loomkeeper open from
/// any other perspective resolves to Coordination. Over the PRODUCT's rows and the product's
/// admission (<see cref="DockHost.AdmissionFor(Perspective)"/>), never a test copy (DC-135). Seen red
/// first — every row read <c>[Coding]</c>: <c>docs/proof/coordination-perspective.md</c>.
/// </summary>
public sealed class KindAllowListsTests
{
    // fixture-derivation: ok — Ruling 84 names these five as THE set; deriving them from the rows would make the oracle the column under test
    private static readonly string[] Loomkeeper = ["sessions", "board", "leaderboard", "ledger", "daydreams"];

    [Fact]
    public void TheFiveLoomkeeperKinds_AreAdmittedByCoordinationOnly_AndCodingAdmitsNone()
    {
        var coordination = PerspectiveSet.All.Single(p => p.Id == "coordination");
        var coding = DockHost.AdmissionFor(PerspectiveSet.Coding);
        var architecture = DockHost.AdmissionFor(PerspectiveSet.Architecture);
        var host = DockHost.AdmissionFor(coordination);

        foreach (var kind in Loomkeeper)
        {
            var row = Assert.Single(SurfaceContentFactory.Kinds, k => k.Kind == kind);

            // The column, literally: one perspective, and it is Coordination.
            Assert.Equal(["coordination"], row.Perspectives.Select(p => p.Id));
            Assert.Equal(SurfaceContentFactory.Instances.One, row.Instances);

            // The admission the hosts enforce, from the same rows.
            Assert.False(coding.Admits(kind), $"Coding admits '{kind}' — Ruling 84: Coding admits no Loomkeeper kind");
            Assert.False(architecture.Admits(kind), $"Architecture admits '{kind}'");
            Assert.True(host.Admits(kind), $"Coordination does not admit '{kind}'");
            Assert.True(host.IsOneInstance(kind));

            // Where an open lands (US-C3; ADR-0030 Resolve): from anywhere, Coordination.
            Assert.Same(coordination, PerspectiveMenu.Resolve(kind, PerspectiveSet.Coding));
            Assert.Same(coordination, PerspectiveMenu.Resolve(kind, PerspectiveSet.Architecture));
            Assert.Same(coordination, PerspectiveMenu.Resolve(kind, PerspectiveSet.Explore));
            Assert.Same(coordination, PerspectiveMenu.Resolve(kind, coordination));

            // What the drop report names for a Coding envelope that carries the kind.
            Assert.Same(coordination, coding.AdmittedBy(kind));
        }

        // The oracle the review names verbatim.
        Assert.Same(coordination, PerspectiveMenu.Resolve("ledger", PerspectiveSet.Coding));

        // As a SET: Coordination admits exactly these five and no other kind — a sixth row naming
        // Coordination, or one of the five naming a second perspective, fails here.
        Assert.Equal(
            Loomkeeper.OrderBy(k => k, StringComparer.Ordinal),
            SurfaceContentFactory.Kinds.Where(k => k.Perspectives.Contains(coordination)).Select(k => k.Kind).OrderBy(k => k, StringComparer.Ordinal));

        // Coding's remaining column: what it keeps after the five leave (the re-cut of its default is
        // SH-4.2's; the column is this slice's).
        Assert.Equal(
            ["codeviewer", "diagnostics", "prompt", "search", "session-document", "terminal"],
            SurfaceContentFactory.Kinds.Where(k => k.Perspectives.Contains(PerspectiveSet.Coding)).Select(k => k.Kind).OrderBy(k => k, StringComparer.Ordinal));
    }
}
