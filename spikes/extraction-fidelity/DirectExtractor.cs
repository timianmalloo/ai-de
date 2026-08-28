using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ExtractionFidelitySpike;

/// <summary>
/// <b>Option B, second iteration — the prototype the Component 1 contract is written against.</b>
/// <para>The first iteration (in <c>spikes/extraction-containment</c>) read one glob and one
/// framework and knew nothing about project references. It scored 159/159 on <c>AiDe.Core</c>, a
/// project that has neither a <c>ProjectReference</c> nor multi-targeting — which is exactly why
/// that number could not be trusted as a general result.</para>
/// <para>Everything here still reads the project file as <b>data</b>. No MSBuild evaluation, so no
/// path by which a repository's build logic executes.</para>
/// </summary>
internal static class DirectExtractor
{
    internal sealed record Extraction(
        string Project,
        string TargetFramework,
        int Sources,
        int References,
        int Types,
        int Edges,
        int UnresolvedEdges,
        double Millis,
        IReadOnlyList<string> TypeNames,
        IReadOnlyList<string> Notes)
    {
        internal double EdgeResolution => Edges == 0 ? 1.0 : 1.0 - ((double)UnresolvedEdges / Edges);
    }

    /// <summary>Extracts every target framework the project declares — one scope per (project, TFM).</summary>
    internal static List<Extraction> ExtractAll(string projectPath)
    {
        var frameworks = TargetFrameworks(projectPath);
        return frameworks.Select(tfm => Extract(projectPath, tfm)).ToList();
    }

    internal static List<string> TargetFrameworks(string projectPath)
    {
        var doc = XDocument.Load(projectPath);
        var many = Property(doc, "TargetFrameworks");
        if (!string.IsNullOrWhiteSpace(many))
            return many.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var one = Property(doc, "TargetFramework");
        return string.IsNullOrWhiteSpace(one) ? ["net10.0"] : [one];
    }

    internal static Extraction Extract(string projectPath, string tfm)
    {
        var watch = Stopwatch.StartNew();
        var notes = new List<string>();
        var compilation = Build(projectPath, tfm, notes, new Dictionary<string, Compilation>(StringComparer.OrdinalIgnoreCase), depth: 0);
        watch.Stop();

        if (compilation is null)
            return new Extraction(Path.GetFileName(projectPath), tfm, 0, 0, 0, 0, 0, watch.Elapsed.TotalMilliseconds, [], notes);

        var types = new List<INamedTypeSymbol>();
        Walk(compilation.Assembly.GlobalNamespace, types);

        // The extractor's real output is EDGES, not a type list. A project reference that fails to
        // resolve still leaves every locally declared type present and correct — and every edge
        // that pointed into it silently becomes an error type. Counting types would score that as
        // perfect fidelity, which is how a silent extraction failure gets shipped.
        var diag = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .GroupBy(d => d.Id).OrderByDescending(g => g.Count()).Take(4).ToList();
        if (diag.Count > 0)
        {
            notes.Add("compiler errors: " + string.Join(", ", diag.Select(g => $"{g.Key}x{g.Count()}")));
            notes.Add("  first: " + diag[0].First().GetMessage());
        }

        var unresolvedNames = new List<string>();
        var (edges, unresolved) = CountEdges(types, unresolvedNames);
        if (unresolved > 0)
        {
            var top = unresolvedNames.GroupBy(n => n, StringComparer.Ordinal)
                .OrderByDescending(g => g.Count()).Take(8)
                .Select(g => $"{g.Key}x{g.Count()}");
            notes.Add("unresolved edges point at: " + string.Join(", ", top));
        }

        return new Extraction(
            Path.GetFileName(projectPath), tfm,
            compilation.SyntaxTrees.Count(),
            compilation.References.Count(),
            types.Count, edges, unresolved,
            watch.Elapsed.TotalMilliseconds,
            types.Select(t => t.ToDisplayString()).OrderBy(n => n, StringComparer.Ordinal).ToList(),
            notes);
    }

    private static Compilation? Build(
        string projectPath, string tfm, List<string> notes,
        Dictionary<string, Compilation> visited, int depth)
    {
        var full = Path.GetFullPath(projectPath);
        if (visited.TryGetValue(full, out var already)) return already;
        if (depth > 8)
        {
            notes.Add($"project-reference depth limit reached at {Path.GetFileName(full)}");
            return null;
        }

        XDocument doc;
        try { doc = XDocument.Load(full); }
        catch (Exception ex) { notes.Add($"cannot parse {Path.GetFileName(full)}: {ex.Message}"); return null; }

        var dir = Path.GetDirectoryName(full)!;
        var sources = Sources(doc, dir);
        var defines = Defines(doc, tfm);

        var parse = new CSharpParseOptions(preprocessorSymbols: defines);
        var trees = sources
            .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), parse, path: f))
            .ToList();

        // ImplicitUsings are generated by the SDK into obj/ — which this extractor deliberately
        // does not read. Without synthesising them, every `Console`, `Task` and `IReadOnlyList` in
        // a modern project is an unresolved symbol. The first run of this spike scored Option B at
        // 88.7% edge resolution for exactly this reason and it looked like a limit of the approach.
        var usings = ImplicitUsings(doc);
        if (usings.Count > 0)
        {
            var text = string.Concat(usings.Select(u => $"global using global::{u};\n"));
            trees.Add(CSharpSyntaxTree.ParseText(text, parse, path: "__ImplicitUsings.g.cs"));
        }

        var references = new List<MetadataReference>();
        references.AddRange(FrameworkReferences(doc, tfm, notes, depth));
        references.AddRange(PackageReferences(dir, notes, depth));

        // Placeholder first, so a reference cycle terminates rather than recursing forever.
        var placeholder = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(full));
        visited[full] = placeholder;

        foreach (var referenced in ProjectReferences(doc, dir))
        {
            var childTfm = BestTargetFramework(referenced, tfm);
            var child = Build(referenced, childTfm, notes, visited, depth + 1);
            if (child is not null)
            {
                references.Add(child.ToMetadataReference());
            }
            else if (depth == 0)
            {
                notes.Add($"UNRESOLVED ProjectReference: {Path.GetFileName(referenced)}");
            }
        }

        var compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(full), trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        visited[full] = compilation;
        return compilation;
    }

    // ---------------------------------------------------------------- project file, read as data

    private static string? Property(XDocument doc, string name) =>
        doc.Descendants(name).Select(e => e.Value).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static List<string> Sources(XDocument doc, string dir)
    {
        var removed = doc.Descendants("Compile")
            .Select(e => (string?)e.Attribute("Remove"))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => Normalise(v!).TrimEnd('*', '/'))
            .ToList();

        var explicitly = doc.Descendants("Compile")
            .Select(e => (string?)e.Attribute("Include"))
            .Where(v => !string.IsNullOrWhiteSpace(v) && !v!.Contains('*'))
            .Select(v => Path.GetFullPath(Path.Combine(dir, v!)))
            .Where(File.Exists)
            .ToList();

        var globbed = Directory
            .EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
            .Where(f =>
            {
                var rel = Normalise(Path.GetRelativePath(dir, f));
                if (rel.StartsWith("bin/", StringComparison.OrdinalIgnoreCase)) return false;
                if (rel.StartsWith("obj/", StringComparison.OrdinalIgnoreCase)) return false;
                return !removed.Any(r => rel.StartsWith(r, StringComparison.OrdinalIgnoreCase));
            });

        return globbed.Concat(explicitly).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string Normalise(string p) => p.Replace((char)92, '/');

    /// <summary>
    /// The namespaces <c>ImplicitUsings</c> would have made global, plus any explicit
    /// <c>&lt;Using Include="…"/&gt;</c> items. These are documented SDK behaviour, so reproducing
    /// them is reading a specification rather than evaluating the project.
    /// </summary>
    private static List<string> ImplicitUsings(XDocument doc)
    {
        var result = new List<string>();
        var enabled = Property(doc, "ImplicitUsings");
        if (string.Equals(enabled, "enable", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
        {
            result.AddRange([
                "System", "System.Collections.Generic", "System.IO", "System.Linq",
                "System.Net.Http", "System.Threading", "System.Threading.Tasks",
            ]);

            if (string.Equals(Property(doc, "UseWPF"), "true", StringComparison.OrdinalIgnoreCase))
                result.AddRange(["System.Windows", "System.Windows.Controls", "System.Windows.Data",
                    "System.Windows.Documents", "System.Windows.Input", "System.Windows.Media"]);

            if (string.Equals(Property(doc, "UseWindowsForms"), "true", StringComparison.OrdinalIgnoreCase))
                result.AddRange(["System.Drawing", "System.Windows.Forms"]);
        }

        result.AddRange(doc.Descendants("Using")
            .Select(e => (string?)e.Attribute("Include"))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim()));

        return result.Distinct(StringComparer.Ordinal).ToList();
    }

    private static List<string> ProjectReferences(XDocument doc, string dir) =>
        doc.Descendants("ProjectReference")
            .Select(e => (string?)e.Attribute("Include"))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => Path.GetFullPath(Path.Combine(dir, v!)))
            .Where(File.Exists)
            .ToList();

    /// <summary>
    /// The preprocessor symbols the compiler would see. MSBuild synthesises the framework symbols
    /// (NET10_0, NETSTANDARD2_0, and every _OR_GREATER below them); read literally, a project's own
    /// DefineConstants adds to them.
    /// </summary>
    private static List<string> Defines(XDocument doc, string tfm)
    {
        var symbols = new List<string>();
        var moniker = tfm.Split('-')[0].ToUpperInvariant().Replace('.', '_');

        if (moniker.StartsWith("NETSTANDARD", StringComparison.Ordinal))
        {
            symbols.Add(moniker);
            symbols.Add("NETSTANDARD");
        }
        else if (moniker.StartsWith("NET", StringComparison.Ordinal) &&
                 Version.TryParse(tfm.Split('-')[0][3..], out var v))
        {
            symbols.Add(moniker);
            symbols.Add("NET");
            for (var major = 5; major <= v.Major; major++)
            {
                var minorMax = major == v.Major ? v.Minor : 0;
                for (var minor = 0; minor <= minorMax; minor++)
                    symbols.Add($"NET{major}_{minor}_OR_GREATER");
            }

            symbols.Add("NETCOREAPP");
        }

        if (tfm.Contains("-windows", StringComparison.OrdinalIgnoreCase)) symbols.Add("WINDOWS");

        var declared = Property(doc, "DefineConstants");
        if (!string.IsNullOrWhiteSpace(declared))
        {
            symbols.AddRange(declared
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => !s.StartsWith("$(", StringComparison.Ordinal)));
        }

        return symbols.Distinct(StringComparer.Ordinal).ToList();
    }

    private static string BestTargetFramework(string referencedProject, string preferred)
    {
        var available = TargetFrameworks(referencedProject);
        if (available.Contains(preferred, StringComparer.OrdinalIgnoreCase)) return preferred;

        // A net10.0-windows consumer is satisfied by a net10.0 dependency.
        var bare = preferred.Split('-')[0];
        var match = available.FirstOrDefault(a => a.Split('-')[0].Equals(bare, StringComparison.OrdinalIgnoreCase));
        return match ?? available[0];
    }

    // ---------------------------------------------------------------- references

    private static List<MetadataReference> FrameworkReferences(XDocument doc, string tfm, List<string> notes, int depth)
    {
        var refs = new List<MetadataReference>();
        var bare = tfm.Split('-')[0];

        string? pack;
        if (bare.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase))
        {
            pack = RefPack("NETStandard.Library.Ref", null);
        }
        else
        {
            var wanted = Version.TryParse(bare[3..], out var v) ? v : null;
            pack = RefPack("Microsoft.NETCore.App.Ref", wanted);
        }

        if (pack is null)
        {
            notes.Add($"no reference pack for {tfm}");
            return refs;
        }

        // WPF and WinForms live in a SEPARATE pack, and it must be added FIRST. Both packs ship a
        // WindowsBase.dll; the base pack's is a 4.0.0.0 facade and the desktop pack's is the real
        // 10.0.0.0 assembly. Adding the base pack first made the facade win on name, and every WPF
        // type then failed with CS1705 "uses a higher version than referenced assembly" — 591 of
        // them, which read as "Option B cannot do WPF".
        var usesDesktop =
            string.Equals(Property(doc, "UseWPF"), "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Property(doc, "UseWindowsForms"), "true", StringComparison.OrdinalIgnoreCase);

        if (usesDesktop && tfm.Contains("-windows", StringComparison.OrdinalIgnoreCase))
        {
            var wanted = Version.TryParse(bare[3..], out var v2) ? v2 : null;
            var desktop = RefPack("Microsoft.WindowsDesktop.App.Ref", wanted);
            if (desktop is not null)
            {
                Add(desktop);
                if (depth == 0) notes.Add("WindowsDesktop reference pack added first (UseWPF/UseWindowsForms)");
            }
            else if (depth == 0)
            {
                notes.Add("UseWPF is set but no WindowsDesktop reference pack was found");
            }
        }

        Add(pack);
        return refs;

        void Add(string directory)
        {
            var seen = refs.OfType<PortableExecutableReference>()
                .Select(r => Path.GetFileName(r.FilePath) ?? string.Empty)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var dll in Directory.EnumerateFiles(directory, "*.dll"))
            {
                if (!seen.Add(Path.GetFileName(dll))) continue;
                try { refs.Add(MetadataReference.CreateFromFile(dll)); } catch { }
            }
        }
    }

    /// <summary>Newest pack not exceeding <paramref name="wanted"/>, ordered by parsed version.</summary>
    private static string? RefPack(string packName, Version? wanted)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "dotnet", "packs", packName);
        if (!Directory.Exists(root)) return null;

        return Directory.EnumerateDirectories(root)
            .Select(d => (Dir: d, Ok: Version.TryParse(Path.GetFileName(d), out var v), Version: v))
            .Where(x => x.Ok && (wanted is null || x.Version!.Major <= wanted.Major))
            .OrderByDescending(x => x.Version)
            .Select(x => Directory.EnumerateDirectories(Path.Combine(x.Dir, "ref"))
                .OrderByDescending(r => r, StringComparer.OrdinalIgnoreCase).FirstOrDefault())
            .FirstOrDefault(d => d is not null && Directory.EnumerateFiles(d, "*.dll").Any());
    }

    private static List<MetadataReference> PackageReferences(string dir, List<string> notes, int depth)
    {
        var refs = new List<MetadataReference>();
        var assets = Path.Combine(dir, "obj", "project.assets.json");
        if (!File.Exists(assets))
        {
            if (depth == 0) notes.Add("project.assets.json ABSENT — package references unresolved (a fresh clone is in this state)");
            return refs;
        }

        try
        {
            using var stream = File.OpenRead(assets);
            using var json = JsonDocument.Parse(stream);
            if (!json.RootElement.TryGetProperty("packageFolders", out var folders)) return refs;
            var roots = folders.EnumerateObject().Select(p => p.Name).ToList();
            if (!json.RootElement.TryGetProperty("targets", out var targets)) return refs;

            foreach (var target in targets.EnumerateObject())
            foreach (var lib in target.Value.EnumerateObject())
            {
                if (!lib.Value.TryGetProperty("compile", out var compile)) continue;
                foreach (var item in compile.EnumerateObject())
                {
                    if (item.Name.EndsWith("_._", StringComparison.Ordinal)) continue;
                    foreach (var root in roots)
                    {
                        var full = Path.Combine(root,
                            lib.Name.Replace('/', Path.DirectorySeparatorChar),
                            item.Name.Replace('/', Path.DirectorySeparatorChar));
                        if (!File.Exists(full)) continue;
                        try { refs.Add(MetadataReference.CreateFromFile(full)); } catch { }
                        break;
                    }
                }
            }

            if (depth == 0) notes.Add($"project.assets.json present: {refs.Count} package assemblies");
        }
        catch (Exception ex)
        {
            notes.Add($"project.assets.json unreadable: {ex.Message}");
        }

        return refs;
    }

    // ---------------------------------------------------------------- the fidelity metric

    /// <summary>
    /// Every type this project's declarations point AT — base types, interfaces, field and property
    /// types, method returns and parameters. These are the extractor's <c>depends_on</c> edges, and
    /// an edge that resolves to an error type is an edge the graph would get wrong.
    /// </summary>
    internal static (int Edges, int Unresolved) CountEdges(
        IEnumerable<INamedTypeSymbol> types, List<string>? unresolvedNames = null)
    {
        var edges = 0;
        var unresolved = 0;

        foreach (var type in types)
        {
            foreach (var t in Referenced(type))
            {
                edges++;
                if (!IsUnresolved(t)) continue;
                unresolved++;
                unresolvedNames?.Add(NameOf(t));
            }
        }

        return (edges, unresolved);

        static string NameOf(ITypeSymbol t) => t switch
        {
            IArrayTypeSymbol a => NameOf(a.ElementType) + "[]",
            INamedTypeSymbol { IsGenericType: true } g when g.TypeKind != TypeKind.Error =>
                g.ConstructedFrom.Name + "<" + string.Join(",", g.TypeArguments.Select(NameOf)) + ">",
            _ => t.Name,
        };

        static IEnumerable<ITypeSymbol> Referenced(INamedTypeSymbol type)
        {
            if (type.BaseType is { SpecialType: not SpecialType.System_Object } b) yield return b;
            foreach (var i in type.Interfaces) yield return i;

            foreach (var member in type.GetMembers())
            {
                switch (member)
                {
                    case IFieldSymbol f when !f.IsImplicitlyDeclared:
                        yield return f.Type;
                        break;
                    case IPropertySymbol p:
                        yield return p.Type;
                        break;
                    case IMethodSymbol m when m.MethodKind is MethodKind.Ordinary or MethodKind.Constructor:
                        if (m.MethodKind == MethodKind.Ordinary) yield return m.ReturnType;
                        foreach (var parameter in m.Parameters) yield return parameter.Type;
                        break;
                }
            }
        }

        static bool IsUnresolved(ITypeSymbol t) => t switch
        {
            { TypeKind: TypeKind.Error } => true,
            IArrayTypeSymbol a => IsUnresolved(a.ElementType),
            INamedTypeSymbol { IsGenericType: true } g =>
                g.TypeArguments.Any(IsUnresolved) || g.TypeKind == TypeKind.Error,
            _ => false,
        };
    }

    private static void Walk(INamespaceSymbol ns, List<INamedTypeSymbol> into)
    {
        foreach (var t in ns.GetTypeMembers()) into.Add(t);
        foreach (var c in ns.GetNamespaceMembers()) Walk(c, into);
    }
}
