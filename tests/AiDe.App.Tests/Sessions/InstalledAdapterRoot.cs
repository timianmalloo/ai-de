using AiDe.Core.AgentPlane;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// An adapter root where claude-code's pinned entry module IS on disk — the sheet's "launch
/// observed and installed" input (Ruling 105 (2)) — without npm and without the network. The path
/// is exactly what <see cref="EngineCatalog.ResolveLaunch"/> composes, so a catalog pin change
/// moves the fixture with it.
/// </summary>
internal static class InstalledAdapterRoot
{
    /// <summary>Creates <c>&lt;root&gt;/adapters/node_modules/&lt;package&gt;/&lt;entry&gt;</c> for claude-code and returns the adapter root.</summary>
    public static string Create(string root)
    {
        var adapters = Path.Combine(root, "adapters");
        var entry = EngineCatalog.ResolveLaunch("claude-code", adapters).Arguments[0];
        Directory.CreateDirectory(Path.GetDirectoryName(entry)!);
        File.WriteAllText(entry, "// stand-in entry module: the sheet reads presence, never content");
        return adapters;
    }
}
