using System.IO;
using System.Text.Json;

namespace AiDe.App.Workbench;

/// <summary>
/// Persists prompt-draft bodies (spec-editor-surfaces US-ED5) keyed by the stable layout
/// <c>SurfaceId</c>, in a JSON sidecar beside the layout. Mirrors <see cref="TerminalCustomizationStore"/>:
/// it lives off the Core layout model deliberately, so it needs no schema change, and it is
/// best-effort — a missing or corrupt sidecar starts clean, and a failed write never crashes the UI.
/// </summary>
public sealed class PromptDraftStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private Dictionary<string, string> _map = new(StringComparer.Ordinal);

    public PromptDraftStore(string path)
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
                _map = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(_path)) ?? new(StringComparer.Ordinal);
            }
        }
        catch
        {
            _map = new(StringComparer.Ordinal);
        }
    }

    public bool TryGet(string surfaceId, out string? body)
    {
        lock (_gate)
        {
            return _map.TryGetValue(surfaceId, out body);
        }
    }

    public void Save(string surfaceId, string body)
    {
        lock (_gate)
        {
            if (string.IsNullOrEmpty(body))
            {
                _map.Remove(surfaceId);
            }
            else
            {
                _map[surfaceId] = body;
            }

            try
            {
                var dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir)) { Directory.CreateDirectory(dir); }
                File.WriteAllText(_path, JsonSerializer.Serialize(_map));
            }
            catch
            {
                // Best-effort: a draft that fails to persist is still usable this session.
            }
        }
    }
}
