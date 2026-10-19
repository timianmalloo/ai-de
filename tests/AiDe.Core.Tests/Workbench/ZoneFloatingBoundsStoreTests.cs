using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AiDe.Core.Workbench;

namespace AiDe.Core.Tests.Workbench;

/// <summary>
/// <b>T21 node 3, check 1 (Ruling 174 (1)).</b> A floating stack's bounds are stored in the zone file
/// as four optional <c>double?</c> fields on <see cref="ZoneStackDto"/>, read all-or-none as the tree
/// store does (<c>LayoutStore.cs:318</c>), with no <c>schemaVersion</c> bump because the change is
/// additive. An older file without the fields still loads, with no bounds, and an older reader
/// ignores the fields.
/// </summary>
/// <remarks>
/// Units: device-independent pixels in virtual-screen coordinates, the units AvalonDock's floating
/// window writes and <c>VirtualScreen.IsOnAConnectedDisplay</c> reads. A width or height of zero or
/// less counts as no bounds (the Data and Persistence review's condition).
/// </remarks>
public sealed class ZoneFloatingBoundsStoreTests : IDisposable
{
    private static readonly string OldFixture = Path.Combine(AppContext.BaseDirectory, "fixtures", "zone-layout-floating-no-bounds.json");

    // A display to the left of the primary one has negative virtual-screen X; fractions are DIPs at a scale other than 100%.
    private static readonly LayoutRect Saved = new(-1210.5, 84.25, 640, 420);

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aide-zone-bounds-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_dir, "zones.json");

    private static IReadOnlySet<string> None => new HashSet<string>(StringComparer.Ordinal);

    private static IReadOnlySet<string> Kinds => new HashSet<string>(["view", "canvas", "sessions"], StringComparer.Ordinal);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void AFloatingStacksBounds_RoundTripThroughTheZoneFile_AsFourOptionalFields_WithNoSchemaBump()
    {
        var store = new ZoneLayoutStore(FilePath);
        store.Save(LayoutWithOneFloatingStack(Saved));

        var envelope = JsonNode.Parse(File.ReadAllText(FilePath))!;
        Assert.Equal(1, envelope["schemaVersion"]!.GetValue<int>());
        var stack = envelope["layout"]!["floating"]![0]!.AsObject();
        Assert.True(stack.ContainsKey("floatX"), $"the saved floating stack has no bounds fields: {stack.ToJsonString()}");

        var read = store.Read(None, Kinds);
        Assert.Equal(ZoneLoadRefusal.None, read.Refusal);
        var restored = Assert.Single(read.Layout!.Floating);
        Assert.Equal(Saved, restored.FloatingBounds);
    }

    [Fact]
    public void AnOlderFileWithNoBoundsFields_LoadsItsFloatingStack_WithNoBounds()
    {
        var stack = JsonNode.Parse(File.ReadAllText(OldFixture))!["layout"]!["floating"]![0]!.AsObject();
        Assert.False(stack.ContainsKey("floatX"), "the fixture must be an older-shaped file with no bounds fields");

        ZoneLoadResult? result = null;
        var thrown = Record.Exception(() => result = new ZoneLayoutStore(OldFixture).Read(None, Kinds));

        Assert.Null(thrown);
        Assert.Equal(ZoneLoadRefusal.None, result!.Refusal);
        var floating = Assert.Single(result.Layout!.Floating);
        Assert.Equal("sessions", Assert.Single(floating.Surfaces).SurfaceId);
        Assert.Null(floating.FloatingBounds);
    }

    [Theory]
    [InlineData("floatHeight", null)]   // three of the four fields
    [InlineData("floatWidth", 0.0)]     // no width
    [InlineData("floatHeight", -5.0)]   // a negative height
    public void PartialOrEmptyBounds_ReadAsNoBounds_AndTheStackStillLoads(string field, double? value)
    {
        var store = new ZoneLayoutStore(FilePath);
        store.Save(LayoutWithOneFloatingStack(Saved));
        var envelope = JsonNode.Parse(File.ReadAllText(FilePath))!;
        var stack = envelope["layout"]!["floating"]![0]!.AsObject();
        Assert.True(stack.ContainsKey(field), $"the saved floating stack has no \"{field}\" field to change: {stack.ToJsonString()}");
        if (value is { } v)
        {
            stack[field] = v;
        }
        else
        {
            stack.Remove(field);
        }

        File.WriteAllText(FilePath, envelope.ToJsonString());

        var read = store.Read(None, Kinds);
        Assert.Equal(ZoneLoadRefusal.None, read.Refusal);
        Assert.Null(Assert.Single(read.Layout!.Floating).FloatingBounds);
    }

    [Fact]
    public void AnOlderReader_IgnoresTheBoundsFields()
    {
        new ZoneLayoutStore(FilePath).Save(LayoutWithOneFloatingStack(Saved));

        // The schema-1 stack shape before Ruling 174, read with System.Text.Json's defaults as the older binary did.
        var old = JsonSerializer.Deserialize<OldEnvelope>(File.ReadAllText(FilePath));

        var stack = Assert.Single(old!.Layout.Floating);
        Assert.Equal("sessions", Assert.Single(stack.Surfaces).SurfaceId);
    }

    private static WorkbenchLayout LayoutWithOneFloatingStack(LayoutRect bounds)
    {
        var service = new ZoneBackedLayoutService();
        var floated = service.Apply(new LayoutOperation.MoveSurface("sessions", new DropTarget(string.Empty, DropKind.Float)));
        Assert.True(floated.Applied, floated.Announcement);
        var stack = Assert.Single(service.Zones.Floating);
        return service.Zones with { Floating = [stack with { FloatingBounds = bounds }] };
    }

    private sealed record OldEnvelope([property: JsonPropertyName("layout")] OldLayout Layout);

    private sealed record OldLayout([property: JsonPropertyName("floating")] List<OldStack> Floating);

    private sealed record OldStack(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("surfaces")] List<ZoneSurfaceDto> Surfaces,
        [property: JsonPropertyName("activeIndex")] int ActiveIndex);
}
