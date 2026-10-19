using System.Text.Json.Nodes;
using AiDe.Core.Workbench;

namespace AiDe.Core.Tests.Workbench;

/// <summary>
/// <b>T21 node 4 check 1 (the Owner's first T21 close condition; DC-NEW-wl-t21-float's two open
/// siblings).</b> A saved zone layout with no <c>"zones"</c> field, or a floating stack with no
/// <c>"surfaces"</c> field, loads what it can, says that part was left out, and never throws.
/// </summary>
/// <remarks>
/// System.Text.Json passes null for an absent positional member whatever its annotation, so
/// <c>ZoneLayoutDto.Zones</c> and <c>ZoneStackDto.Surfaces</c> arrive null and the read dereferenced them.
/// </remarks>
public sealed class ZoneLayoutPartlyReadTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "fixtures", name);

    private static IReadOnlySet<string> None => new HashSet<string>(StringComparer.Ordinal);

    private static IReadOnlySet<string> Kinds => new HashSet<string>(["view", "canvas"], StringComparer.Ordinal);

    [Fact]
    public void ASavedLayoutWithNoZonesField_RestoresItsFloatingWindow_WithEmptyZones_AndSaysPartWasLeftOut()
    {
        var path = Fixture("zone-layout-no-zones.json");
        Assert.False(JsonNode.Parse(File.ReadAllText(path))!["layout"]!.AsObject().ContainsKey("zones"),
            "the fixture must omit the \"zones\" field");

        ZoneLoadResult? result = null;
        var thrown = Record.Exception(() => result = new ZoneLayoutStore(path).Read(None, Kinds));

        Assert.Null(thrown);
        Assert.Equal(ZoneLoadRefusal.None, result!.Refusal);
        Assert.True(result.PartlyRead, "the read did not say that part of the file was left out");
        var restored = Assert.IsType<WorkbenchLayout>(result.Layout);
        Assert.All(Enum.GetValues<ZoneId>(), id => Assert.Null(restored.Zone(id).Content));
        Assert.Equal("sessions", Assert.Single(Assert.Single(restored.Floating).Surfaces).SurfaceId);
        Assert.Empty(result.LeftOutTitles);
    }

    [Fact]
    public void AFloatingStackWithNoSurfacesField_IsLeftOut_TheZonesRestore_AndTheReadSaysSo()
    {
        var path = Fixture("zone-layout-floating-no-surfaces.json");
        var stack = JsonNode.Parse(File.ReadAllText(path))!["layout"]!["floating"]![0]!.AsObject();
        Assert.False(stack.ContainsKey("surfaces"), "the fixture's floating stack must omit the \"surfaces\" field");

        ZoneLoadResult? result = null;
        var thrown = Record.Exception(() => result = new ZoneLayoutStore(path).Read(None, Kinds));

        Assert.Null(thrown);
        Assert.Equal(ZoneLoadRefusal.None, result!.Refusal);
        Assert.True(result.PartlyRead, "the read did not say that part of the file was left out");
        var restored = Assert.IsType<WorkbenchLayout>(result.Layout);
        Assert.Empty(restored.Floating);
        Assert.Equal(ZoneId.Left, restored.FindZoneOf("explore"));
        Assert.Empty(result.LeftOutTitles);
    }

    // T21 node 5 check (A), the named form (Owner, 2026-10-19 ~15:10Z): a zone entry this build does
    // not know cannot be placed, so it is left out, and the read names its surfaces by their saved
    // titles, in saved order, through a split. Before this the entry vanished without a word.
    [Fact]
    public void AZoneEntryTheBuildDoesNotKnow_IsLeftOut_AndTheReadNamesItsSurfacesByTitle_InSavedOrder()
    {
        var path = Fixture("zone-layout-unknown-zone.json");
        var zones = JsonNode.Parse(File.ReadAllText(path))!["layout"]!["zones"]!.AsArray();
        Assert.Contains(zones, z => (string?)z!["id"] == "Nowhere");

        ZoneLoadResult? result = null;
        var thrown = Record.Exception(() => result = new ZoneLayoutStore(path).Read(None, Kinds));

        Assert.Null(thrown);
        Assert.Equal(ZoneLoadRefusal.None, result!.Refusal);
        Assert.True(result.PartlyRead, "the read did not say that the unknown zone was left out");
        Assert.Equal(["Notebook 2", "Graph"], result.LeftOutTitles);
        var restored = Assert.IsType<WorkbenchLayout>(result.Layout);
        Assert.Equal(ZoneId.Left, restored.FindZoneOf("explore"));
        Assert.Null(restored.FindZoneOf("graph"));
    }

    // T21 node 5 check (A), the plain form: the left-out part holds no surface with a title, so the
    // read says part was left out and names nothing.
    [Fact]
    public void AZoneEntryTheBuildDoesNotKnow_HoldingNoSurface_IsLeftOut_AndTheReadNamesNothing()
    {
        ZoneLoadResult? result = null;
        var thrown = Record.Exception(
            () => result = new ZoneLayoutStore(Fixture("zone-layout-unknown-zone-empty.json")).Read(None, Kinds));

        Assert.Null(thrown);
        Assert.Equal(ZoneLoadRefusal.None, result!.Refusal);
        Assert.True(result.PartlyRead, "the read did not say that the unknown zone was left out");
        Assert.Empty(result.LeftOutTitles);
    }

    // The control against over-reporting: an older file that simply has no "floating" field is not
    // malformed, so nothing is said to be left out.
    [Fact]
    public void ASavedLayoutWithNoFloatingField_IsReadWhole()
    {
        var result = new ZoneLayoutStore(Fixture("zone-layout-no-floating.json")).Read(None, Kinds);

        Assert.Equal(ZoneLoadRefusal.None, result.Refusal);
        Assert.False(result.PartlyRead);
    }
}
