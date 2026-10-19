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
