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

/// <summary>One floating stack. The four float fields are its window's bounds (Ruling 174).</summary>
/// <remarks>
/// The bounds are device-independent pixels in virtual-screen coordinates. They are optional and
/// additive, so schema 1 is kept: an older file has none, and an older reader ignores them. They are
/// read all-or-none, as the tree store reads its own (<see cref="LayoutStore"/>), and a width or height
/// of zero or less counts as no bounds.
/// </remarks>
public sealed record ZoneStackDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("surfaces")] List<ZoneSurfaceDto> Surfaces,
    [property: JsonPropertyName("activeIndex")] int ActiveIndex,
    [property: JsonPropertyName("floatX")] double? FloatX = null,
    [property: JsonPropertyName("floatY")] double? FloatY = null,
    [property: JsonPropertyName("floatWidth")] double? FloatWidth = null,
    [property: JsonPropertyName("floatHeight")] double? FloatHeight = null);

public sealed record ZoneSurfaceDto(
    [property: JsonPropertyName("surfaceId")] string SurfaceId,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("title")] string Title);

/// <summary>Why <see cref="ZoneLayoutStore.Read"/> returned no layout (ADR-0032 rule 4). <see cref="None"/> when it returned one.</summary>
public enum ZoneLoadRefusal
{
    None,

    /// <summary>No file at the slot's path — a first run, or a slot never written.</summary>
    NoFile,

    /// <summary>A <c>schemaVersion</c> this build does not read; the file is refused whole, never partially applied.</summary>
    NewerSchema,

    /// <summary>Unreadable JSON, no layout, or an arrangement that breaks the frame invariant (a duplicated surface id).</summary>
    Corrupt,
}

/// <summary>What a read produced: the layout, or null with the reason.</summary>
/// <param name="PartlyRead">True when the layout was returned without a part of the file that could not be read.</param>
public sealed record ZoneLoadResult(WorkbenchLayout? Layout, ZoneLoadRefusal Refusal, bool PartlyRead = false)
{
    /// <summary>
    /// The saved titles of the surfaces in the part that was left out, in saved order. Empty when that
    /// part held no surface with a title, or when nothing was left out.
    /// </summary>
    public IReadOnlyList<string> LeftOutTitles { get; init; } = [];
}

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

    /// <summary>
    /// The words after the quoted titles in the report line for a floating window moved off a display
    /// that is no longer connected (AC-D01b), without a final period. Both restores say it: the tree
    /// path (<see cref="LayoutStore"/>) and the zone path (<c>LayoutPersistence</c>).
    /// </summary>
    public const string RehomedSentenceTail = "moved onto this display because the saved display is not connected";

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string FilePath => filePath;

    /// <summary>The sibling a save is written to before it replaces the file: <c>&lt;file&gt;.tmp</c>.</summary>
    public string TempPath => filePath + ".tmp";

    /// <summary>
    /// Writes the arrangement — atomically (ADR-0032 rule 3). The envelope is serialised to
    /// <see cref="TempPath"/> and then moved over the destination in one step, so the file the
    /// product reads is never a torn write: a crash before the move leaves the original, and a
    /// stale temp file is overwritten by the next save.
    /// </summary>
    /// <param name="backupPath">
    /// When given and the destination exists, the SAME call that replaces the original preserves
    /// it there (<see cref="File.Replace(string, string, string?)"/>): the pre-perspective bytes,
    /// or a refused file, are kept by the write that would otherwise lose them. A backup that
    /// cannot be created fails the save and leaves the destination untouched — the caller reports
    /// it; the original is never written over on a best-effort promise.
    /// </param>
    public void Save(WorkbenchLayout layout, string? backupPath = null)
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

        File.WriteAllText(TempPath, JsonSerializer.Serialize(envelope, Json));
        try
        {
            if (backupPath is not null && File.Exists(filePath))
            {
                File.Replace(TempPath, filePath, backupPath);
            }
            else
            {
                File.Move(TempPath, filePath, overwrite: true);
            }
        }
        finally
        {
            // A replace that failed leaves the temp file beside the intact original; it is removed
            // so the next save starts clean, and the failure still propagates to the caller.
            try { File.Delete(TempPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    /// <summary>
    /// Loads the saved zone layout, dropping surfaces that are no longer available, and, when
    /// nothing is returned, says why (ADR-0032 rule 4): no file · a newer schema than this build
    /// reads · a corrupt file (unreadable JSON, a missing layout, or an arrangement that breaks
    /// the frame invariant). Never throws on a bad file. The file is never rewritten by a read; a
    /// refused file is left in place for the caller to back up before its slot is next saved.
    /// </summary>
    public ZoneLoadResult Read(IReadOnlySet<string> availableSurfaces, IReadOnlySet<string> restorableKinds)
    {
        if (!File.Exists(filePath))
        {
            return new ZoneLoadResult(null, ZoneLoadRefusal.NoFile);
        }

        ZoneEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<ZoneEnvelope>(File.ReadAllText(filePath), Json);
        }
        catch (JsonException)
        {
            return new ZoneLoadResult(null, ZoneLoadRefusal.Corrupt); // the file is left in place for inspection
        }

        if (envelope?.Layout is null)
        {
            return new ZoneLoadResult(null, ZoneLoadRefusal.Corrupt);
        }

        if (envelope.SchemaVersion > CurrentSchemaVersion)
        {
            return new ZoneLoadResult(null, ZoneLoadRefusal.NewerSchema);
        }

        if (envelope.SchemaVersion < CurrentSchemaVersion)
        {
            // No store ever wrote a lower version (schema 1 is the first); a 0 or a missing field
            // is a file this store did not write, not a newer build's.
            return new ZoneLoadResult(null, ZoneLoadRefusal.Corrupt);
        }

        bool Available(ZoneSurfaceDto s) =>
            availableSurfaces.Contains(s.SurfaceId) || restorableKinds.Contains(s.Kind);

        // System.Text.Json passes null for an absent positional member whatever its annotation
        // (DC-NEW-wl-t21-float). A file with no "zones" field reads as empty zones, as a zone missing
        // from the list already does, and a floating stack with no "surfaces" field is left out. Both
        // are reported as a partial read.
        var partlyRead = envelope.Layout.Zones is null;
        var savedZones = envelope.Layout.Zones ?? [];
        var leftOutTitles = new List<string>();

        // A zone entry whose id names no zone of this build cannot be placed. It is left out, and its
        // surfaces are named by their saved titles (T21 node 5, the Owner's refinement).
        foreach (var unknown in savedZones.Where(z => !Enum.GetNames<ZoneId>().Contains(z.Id, StringComparer.Ordinal)))
        {
            partlyRead = true;
            leftOutTitles.AddRange(TitlesIn(unknown.Content));
        }

        var zones = ImmutableDictionary.CreateBuilder<ZoneId, ZoneState>();
        foreach (var id in Enum.GetValues<ZoneId>())
        {
            var dto = savedZones.FirstOrDefault(z => z.Id == id.ToString());
            zones[id] = dto is null
                ? new ZoneState(id, Content: null, id == ZoneId.Center ? 1.0 : ZoneState.DefaultExtent, Collapsed: false)
                : new ZoneState(id, FromDto(dto.Content, Available), dto.Extent,
                    Collapsed: id != ZoneId.Center && dto.Collapsed);
        }

        // A file with no "floating" field (hand-edited, or written before floating was stored) reads as
        // no floating panes. That is an older file, not a partial read.
        var floating = ImmutableList.CreateBuilder<StackNode>();
        foreach (var f in envelope.Layout.Floating ?? [])
        {
            if (f.Surfaces is null)
            {
                partlyRead = true;
            }
            else if (FromStackDto(f, Available) is { } stack)
            {
                floating.Add(stack);
            }
        }

        var layout = new WorkbenchLayout(zones.ToImmutable(), floating.ToImmutable(), Maximized: null);

        try
        {
            layout.AssertInvariant();
        }
        catch (InvalidOperationException)
        {
            return new ZoneLoadResult(null, ZoneLoadRefusal.Corrupt); // e.g. a duplicated surface → keep current
        }

        return new ZoneLoadResult(layout, ZoneLoadRefusal.None, partlyRead) { LeftOutTitles = leftOutTitles };
    }

    private static IEnumerable<string> TitlesIn(ZoneContentDto? content) =>
        content is null
            ? []
            : (content.Tabs ?? []).Select(t => t.Title)
                .Concat((content.Children ?? []).SelectMany(TitlesIn))
                .Where(title => !string.IsNullOrEmpty(title));

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
        new(s.Id, [.. s.Surfaces.Select(ToDto)], s.ActiveIndex,
            s.FloatingBounds?.X, s.FloatingBounds?.Y, s.FloatingBounds?.Width, s.FloatingBounds?.Height);

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
        var bounds = dto is { FloatX: { } x, FloatY: { } y, FloatWidth: { } w and > 0, FloatHeight: { } h and > 0 }
            ? new LayoutRect(x, y, w, h)
            : (LayoutRect?)null;
        return surfaces.Count == 0 ? null : new StackNode(dto.Id, surfaces, dto.ActiveIndex, StackState.Floating, floatingBounds: bounds);
    }

    private static Surface FromDto(ZoneSurfaceDto s) => new(s.SurfaceId, s.Kind, s.Title);
}

