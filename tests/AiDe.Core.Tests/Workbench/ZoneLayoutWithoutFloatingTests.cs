using System.Text.Json.Nodes;
using AiDe.Core.Workbench;

namespace AiDe.Core.Tests.Workbench;

/// <summary>
/// <b>T21 null check (Ruling 171 (2)).</b> A saved zone layout with no <c>"floating"</c> field (a
/// file written before floating was stored, or edited by hand) restores with an empty floating list
/// and never throws. <see cref="ZoneLayoutStore.Read"/> promises "never throws on a bad file", and a
/// missing optional field is not even a bad file.
/// </summary>
/// <remarks>
/// System.Text.Json passes null for a positional record parameter whose property is absent, so
/// <c>ZoneLayoutDto.Floating</c> arrives null and the read projected it with <c>.Select</c>.
/// </remarks>
public sealed class ZoneLayoutWithoutFloatingTests
{
    private static readonly string Fixture = Path.Combine(AppContext.BaseDirectory, "fixtures", "zone-layout-no-floating.json");

    private static IReadOnlySet<string> None => new HashSet<string>(StringComparer.Ordinal);

    private static IReadOnlySet<string> Kinds => new HashSet<string>(["view", "canvas"], StringComparer.Ordinal);

    [Fact]
    public void ASavedLayoutWithNoFloatingField_RestoresWithAnEmptyFloatingList_AndNeverThrows()
    {
        var layoutNode = JsonNode.Parse(File.ReadAllText(Fixture))!["layout"]!.AsObject();
        Assert.False(layoutNode.ContainsKey("floating"), "the fixture must omit the \"floating\" field");

        ZoneLoadResult? result = null;
        var thrown = Record.Exception(() => result = new ZoneLayoutStore(Fixture).Read(None, Kinds));

        Assert.Null(thrown);
        Assert.Equal(ZoneLoadRefusal.None, result!.Refusal);
        var restored = Assert.IsType<WorkbenchLayout>(result.Layout);
        Assert.Empty(restored.Floating);
        Assert.Equal(ZoneId.Left, restored.FindZoneOf("explore"));
        Assert.Equal(ZoneId.Center, restored.FindZoneOf("graph"));

        var service = new ZoneBackedLayoutService();
        service.RestoreZones(restored);
        Assert.Empty(service.Zones.Floating);
        Assert.Empty(service.Current.Floating);
        Assert.Equal(ZoneId.Center, service.Zones.FindZoneOf("graph"));
    }
}
