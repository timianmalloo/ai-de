using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ExtractionContainmentSpike;

/// <summary>
/// <b>Option B — extract without MSBuild at all.</b>
/// <para>The project file is read as <i>data</i>: XML parsed for its source globs and references,
/// never evaluated. No targets run, so there is no path by which a repository's build logic can
/// execute. The question this probe answers is therefore not "is it safe" — it is safe by
/// construction — but <b>what does it cost in fidelity</b>.</para>
/// </summary>
internal static class DirectRoslynProbe
{
    internal sealed record Result(
        bool Loaded, int Sources, int References, int Types, int Members,
        double Millis, IReadOnlyList<string> TypeNames, IReadOnlyList<string> Notes);

    internal static Result Run(string projectPath)
    {
        var notes = new List<string>();
        var watch = Stopwatch.StartNew();
        var dir = Path.GetDirectoryName(Path.GetFullPath(projectPath))!;

        // ---------------------------------------------------------------- sources
        // The SDK's default glob. Read from the XML only to honour explicit Compile Remove/Include;
        // nothing here evaluates a property or runs a task.
        XDocument doc;
        try
        {
            doc = XDocument.Load(projectPath);
        }
        catch (Exception ex)
        {
            return new Result(false, 0, 0, 0, 0, watch.Elapsed.TotalMilliseconds, [], [$"could not parse project XML: {ex.Message}"]);
        }

        var removed = doc.Descendants("Compile")
            .Select(e => (string?)e.Attribute("Remove"))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Replace((char)92, '/').TrimEnd('*', '/'))
            .ToList();

        var sources = Directory
            .EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
            .Where(f =>
            {
                var rel = Path.GetRelativePath(dir, f).Replace((char)92, '/');
                if (rel.StartsWith("bin/", StringComparison.OrdinalIgnoreCase)) return false;
                if (rel.StartsWith("obj/", StringComparison.OrdinalIgnoreCase)) return false;
                return !removed.Any(r => rel.StartsWith(r, StringComparison.OrdinalIgnoreCase));
            })
            .ToList();

        // ---------------------------------------------------------------- references
        var references = new List<MetadataReference>();
        var refPack = LatestRefPack();
        if (refPack is null)
        {
            notes.Add("no Microsoft.NETCore.App.Ref pack found — framework types unresolved");
        }
        else
        {
            foreach (var dll in Directory.EnumerateFiles(refPack, "*.dll"))
            {
                try { references.Add(MetadataReference.CreateFromFile(dll)); } catch { }
            }
            notes.Add($"framework references from the ref pack: {refPack}");
        }

        // Package references, IF a restore has already happened. This is the load-bearing caveat and
        // it is measured, not assumed: project.assets.json is DATA and reading it executes nothing —
        // but PRODUCING it requires `dotnet restore`, which is itself MSBuild evaluation. On a
        // freshly cloned repository this file does not exist.
        var assets = Path.Combine(dir, "obj", "project.assets.json");
        if (File.Exists(assets))
        {
            var added = AddPackageReferences(assets, references);
            notes.Add($"project.assets.json present: {added} package assemblies resolved (restore had already run)");
        }
        else
        {
            notes.Add("**project.assets.json ABSENT** — no package references. A freshly cloned repository is in this state until a restore runs, and restore is itself MSBuild evaluation.");
        }

        // ---------------------------------------------------------------- compile
        var trees = sources.Select(f =>
            CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f)).ToList();

        var compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(projectPath),
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var types = new List<INamedTypeSymbol>();
        Walk(compilation.Assembly.GlobalNamespace, types);
        var members = types.Sum(t => t.GetMembers().Length);
        watch.Stop();

        return new Result(
            true, sources.Count, references.Count, types.Count, members,
            watch.Elapsed.TotalMilliseconds,
            types.Select(t => t.ToDisplayString()).OrderBy(n => n).ToList(),
            notes);

        static void Walk(INamespaceSymbol ns, List<INamedTypeSymbol> into)
        {
            foreach (var t in ns.GetTypeMembers()) into.Add(t);
            foreach (var child in ns.GetNamespaceMembers()) Walk(child, into);
        }
    }

    private static string? LatestRefPack()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "dotnet", "packs", "Microsoft.NETCore.App.Ref");
        if (!Directory.Exists(root)) return null;

        // Ordered by parsed VERSION, not by string: the first attempt sorted lexically and picked
        // 8.0.28 over 10.0.11, which silently compiled the fixture against the wrong framework.
        // The symbol counts still matched, so nothing looked wrong — a fidelity comparison against
        // the wrong baseline is worse than no comparison.
        return Directory.EnumerateDirectories(root)
            .Select(d => (Dir: d, Ok: Version.TryParse(Path.GetFileName(d), out var v), Version: v))
            .Where(x => x.Ok)
            .OrderByDescending(x => x.Version)
            .Select(x => Directory.EnumerateDirectories(Path.Combine(x.Dir, "ref"))
                .OrderByDescending(r => r, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault())
            .FirstOrDefault(d => d is not null && Directory.EnumerateFiles(d, "*.dll").Any());
    }

    private static int AddPackageReferences(string assetsPath, List<MetadataReference> into)
    {
        var added = 0;
        try
        {
            using var stream = File.OpenRead(assetsPath);
            using var json = JsonDocument.Parse(stream);
            if (!json.RootElement.TryGetProperty("packageFolders", out var folders)) return 0;
            var roots = folders.EnumerateObject().Select(p => p.Name).ToList();

            if (!json.RootElement.TryGetProperty("targets", out var targets)) return 0;
            foreach (var target in targets.EnumerateObject())
            foreach (var lib in target.Value.EnumerateObject())
            {
                if (!lib.Value.TryGetProperty("compile", out var compile)) continue;
                foreach (var item in compile.EnumerateObject())
                {
                    if (item.Name.EndsWith("_._", StringComparison.Ordinal)) continue;
                    foreach (var root in roots)
                    {
                        var full = Path.Combine(root, lib.Name.Replace('/', Path.DirectorySeparatorChar), item.Name.Replace('/', Path.DirectorySeparatorChar));
                        if (!File.Exists(full)) continue;
                        try { into.Add(MetadataReference.CreateFromFile(full)); added++; } catch { }
                        break;
                    }
                }
            }
        }
        catch { }
        return added;
    }
}
