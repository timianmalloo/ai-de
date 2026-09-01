using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiDe.Core.Workbench;

// Persistence DTOs for the zone model. Separate from the tree LayoutStore because zones carry state
// the projected tree cannot (collapsed content, per-zone extent), so saving the projection would be
// lossy. Kept deliberately small; schema v1.
public sealed record ZoneEnvelope(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("appVersion")] string AppVersion,
    [property: JsonPropertyName("savedUtc")] DateTimeOffset SavedUtc,
    [property: JsonPropertyName("layout")] ZoneLayoutDto Layout);

public sealed record ZoneLayoutDto(
    [property: JsonPropertyName("zones")] List<ZoneStateDto> Zones,
    [property: JsonPropertyName("floating")] List<ZoneStackDto> Floating);

public sealed record ZoneStateDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("content")] ZoneContentDto? Content,
    [property: JsonPropertyName("extent")] double Extent,
    [property: JsonPropertyName("collapsed")] bool Collapsed);

public sealed record ZoneContentDto(
    [property: JsonPropertyName("kind")] string Kind, // "stack" | "split"
    [property: JsonPropertyName("tabs")] List<ZoneSurfaceDto>? Tabs,
    [property: JsonPropertyName("activeIndex")] int ActiveIndex,
    [property: JsonPropertyName("orientation")] string? Orientation,
    [property: JsonPropertyName("children")] List<ZoneContentDto>? Children,
    [property: JsonPropertyName("weights")] List<double>? Weights);

public sealed record ZoneStackDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("surfaces")] List<ZoneSurfaceDto> Surfaces,
    [property: JsonPropertyName("activeIndex")] int ActiveIndex);

public sealed record ZoneSurfaceDto(
    [property: JsonPropertyName("surfaceId")] string SurfaceId,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("title")] string Title);

/// <summary>
/// Saves and restores a <see cref="WorkbenchLayout"/> of named zones as JSON (ADR-0021 dz-persist),
/// preserving what the projected tree cannot: collapsed-zone content, per-zone extent, and exact
/// placement. Restore filters out surfaces the app can no longer provide, so a saved terminal whose
/// process is gone (or a surface kind the build dropped) does not resurrect — an empty zone simply
/// becomes a placeholder, never a broken pane.
/// </summary>
public sealed class ZoneLayoutStore(string filePath, string appVersion = "0.3.0")
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string FilePath => filePath;

    public void Save(WorkbenchLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var dto = new ZoneLayoutDto(
            [.. Enum.GetValues<ZoneId>().Select(id => ToDto(layout.Zone(id)))],
            [.. layout.Floating.Select(ToStackDto)]);
        var envelope = new ZoneEnvelope(CurrentSchemaVersion, appVersion, DateTimeOffset.UtcNow, dto);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, JsonSerializer.Serialize(envelope, Json));
    }

    /// <summary>
    /// Loads the saved zone layout, dropping surfaces that are no longer available. Returns null when
    /// there is no file, it cannot be read, or it does not deserialize — the caller then keeps its
    /// current arrangement (or the default). Never throws on a bad file.
    /// </summary>
    public WorkbenchLayout? Load(IReadOnlySet<string> availableSurfaces, IReadOnlySet<string> restorableKinds)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        ZoneEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<ZoneEnvelope>(File.ReadAllText(filePath), Json);
        }
        catch (JsonException)
        {
            return null; // unreadable → keep current; the file is left in place for inspection
        }

        if (envelope?.Layout is null || envelope.SchemaVersion != CurrentSchemaVersion)
        {
            return null;
        }

        bool Available(ZoneSurfaceDto s) =>
            availableSurfaces.Contains(s.SurfaceId) || restorableKinds.Contains(s.Kind);

        var zones = ImmutableDictionary.CreateBuilder<ZoneId, ZoneState>();
        foreach (var id in Enum.GetValues<ZoneId>())
        {
            var dto = envelope.Layout.Zones.FirstOrDefault(z => z.Id == id.ToString());
            zones[id] = dto is null
                ? new ZoneState(id, Content: null, id == ZoneId.Center ? 1.0 : ZoneState.DefaultExtent, Collapsed: false)
                : new ZoneState(id, FromDto(dto.Content, Available), dto.Extent,
                    Collapsed: id != ZoneId.Center && dto.Collapsed);
        }

        var floating = envelope.Layout.Floating
            .Select(f => FromStackDto(f, Available))
            .Where(f => f is not null)
            .Cast<StackNode>()
            .ToImmutableList();

        var layout = new WorkbenchLayout(zones.ToImmutable(), floating, Maximized: null);

        try
        {
            layout.AssertInvariant();
        }
        catch (InvalidOperationException)
        {
            return null; // a corrupt saved layout (e.g. a duplicated surface) → keep current
        }

        return layout;
    }

    // ── mapping ────────────────────────────────────────────────────────────────────────────

    private static ZoneStateDto ToDto(ZoneState zone) =>
        new(zone.Id.ToString(), ToDto(zone.Content), zone.Extent, zone.Collapsed);

    private static ZoneContentDto? ToDto(ZoneContent? content) => content switch
    {
        null => null,
        ZoneStack s => new ZoneContentDto("stack", [.. s.Tabs.Select(ToDto)], s.ActiveIndex, null, null, null),
        ZoneSplit p => new ZoneContentDto("split", null, 0, p.Orientation.ToString(),
            [.. p.Children.Select(c => ToDto(c)!)], [.. p.Weights]),
        _ => null,
    };

    private static ZoneStackDto ToStackDto(StackNode s) =>
        new(s.Id, [.. s.Surfaces.Select(ToDto)], s.ActiveIndex);

    private static ZoneSurfaceDto ToDto(Surface s) => new(s.SurfaceId, s.Kind, s.Title);

    private static ZoneContent? FromDto(ZoneContentDto? dto, Func<ZoneSurfaceDto, bool> available)
    {
        if (dto is null)
        {
            return null;
        }

        if (string.Equals(dto.Kind, "split", StringComparison.Ordinal)
            && dto.Children is { Count: > 0 } childrenDto)
        {
            var children = childrenDto
                .Select(c => FromDto(c, available))
                .Where(c => c is not null)
                .Cast<ZoneContent>()
                .ToImmutableList();

            return children.Count switch
            {
                0 => null,
                1 => children[0],
                _ => new ZoneSplit(
                    Enum.TryParse<Orientation>(dto.Orientation, out var o) ? o : Orientation.Horizontal,
                    children,
                    dto.Weights is { Count: > 0 } && dto.Weights.Count == children.Count
                        ? [.. dto.Weights]
                        : [.. Enumerable.Repeat(1.0 / children.Count, children.Count)]),
            };
        }

        var tabs = (dto.Tabs ?? [])
            .Where(available)
            .Select(FromDto)
            .ToImmutableList();

        return tabs.Count == 0 ? null : new ZoneStack(tabs, Math.Clamp(dto.ActiveIndex, 0, tabs.Count - 1));
    }

    private static StackNode? FromStackDto(ZoneStackDto dto, Func<ZoneSurfaceDto, bool> available)
    {
        var surfaces = dto.Surfaces.Where(available).Select(FromDto).ToImmutableList();
        return surfaces.Count == 0 ? null : new StackNode(dto.Id, surfaces, dto.ActiveIndex, StackState.Floating);
    }

    private static Surface FromDto(ZoneSurfaceDto s) => new(s.SurfaceId, s.Kind, s.Title);
}

