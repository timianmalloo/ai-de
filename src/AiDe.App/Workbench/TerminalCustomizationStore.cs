using System.IO;
using System.Text.Json;

namespace AiDe.App.Workbench;

/// <summary>One terminal's persisted customization. All optional — a null field means "unset".</summary>
public sealed record TerminalCustomization(string? Name, string? Scheme, string? TabColour);

/// <summary>
/// Persists per-session terminal customization (name, colour scheme, tab colour) keyed by the stable
/// layout <c>SurfaceId</c>, in a JSON sidecar beside the layout. This is the cross-restart half of
/// the customization the surface already keeps in memory (DC-029 keeps it within a session); it lives
/// off the Core layout model deliberately, so it needs no schema change. Best-effort: a missing or
/// corrupt sidecar starts clean, and a failed write never crashes the UI.
/// </summary>
public sealed class TerminalCustomizationStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private Dictionary<string, TerminalCustomization> _map = new(StringComparer.Ordinal);

    public TerminalCustomizationStore(string path)
    {
        _path = path;
        Load();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                _map = JsonSerializer.Deserialize<Dictionary<string, TerminalCustomization>>(
                    File.ReadAllText(_path)) ?? new(StringComparer.Ordinal);
            }
        }
        catch
        {
            _map = new(StringComparer.Ordinal);
        }
    }

    public bool TryGet(string surfaceId, out TerminalCustomization? customization)
    {
        lock (_gate)
        {
            return _map.TryGetValue(surfaceId, out customization);
        }
    }

    public void Save(string surfaceId, TerminalCustomization customization)
    {
        lock (_gate)
        {
            // Nothing customized: drop the row rather than persist an all-default entry.
            var isDefault = customization is { Name: null, TabColour: null }
                && customization.Scheme is null or "Default";

            if (isDefault)
            {
                _map.Remove(surfaceId);
            }
            else
            {
                _map[surfaceId] = customization;
            }

            Persist();
        }
    }

    private void Persist()
    {
        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(_path, JsonSerializer.Serialize(_map, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Persistence is best-effort; a failed write must not crash the UI.
        }
    }
}
