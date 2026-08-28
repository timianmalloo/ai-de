using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AiDe.Core.Extraction;

/// <summary>Why a compilation could not see everything a build would.</summary>
/// <remarks>
/// These are the extractor's <b>disclosures</b>. Each becomes a fact on the scope, so a projection
/// over an affected scope reports the omission instead of answering as though nothing were missing.
/// A silently incomplete answer is the failure mode this whole design exists to avoid.
/// </remarks>
public static class ExtractionDisclosures
{
    /// <summary>No <c>obj/project.assets.json</c>: package types are unresolved.</summary>
    public const string PackagesNotRestored = "packages-not-restored";

    /// <summary>A WPF project's XAML-generated partial members are not analysed.</summary>
    public const string XamlGeneratedMembersNotAnalysed = "xaml-generated-members-not-analysed";

    /// <summary>Source generators are never run, so generated symbols are absent (S2/S1).</summary>
    public const string GeneratedCodeNotAnalysed = "generated-code-not-analysed";

    /// <summary>A <c>ProjectReference</c> could not be resolved; edges into it are missing.</summary>
    public const string ProjectReferenceUnresolved = "project-reference-unresolved";
}

/// <summary>One project, compiled for one target framework, plus what could not be seen.</summary>
public sealed record CSharpCompilationResult(
    Compilation? Compilation,
    string TargetFramework,
    IReadOnlyList<string> Disclosures,
    IReadOnlyList<string> Notes);

/// <summary>
/// Reads a C# project file <b>as data</b> and produces a Roslyn compilation from it.
/// </summary>
/// <remarks>
/// <para><b>Nothing here evaluates MSBuild or runs a target.</b> That is the entire point: spike D3
/// measured that loading a repository through <c>MSBuildWorkspace</c> executes code the repository
/// supplied — an <c>Exec</c> in <c>InitialTargets</c> or a <c>RoslynCodeTaskFactory</c> inline task
/// needs nothing but a checked-in <c>.csproj</c>. Reading the file as XML cannot do that.</para>
///
/// <para><b>Measured at parity, not assumed.</b> Against <c>MSBuildWorkspace</c> on four project
/// shapes — plain, <c>ProjectReference</c>+WPF, <c>ProjectReference</c>, and multi-targeted — this
/// recovers 100% of dependency edges and loses no types, ~25x faster
/// (<c>spikes/extraction-fidelity</c>).</para>
/// </remarks>
public sealed class CSharpProjectReader
{
    /// <summary>How deep <c>ProjectReference</c> recursion may go before it is disclosed and stopped.</summary>
    /// <remarks>
    /// <c>simplify: recursive ProjectReference compilation rather than a build-order graph; ceiling is
    /// a depth of 8; upgrade trigger = a real repository exceeds it, or the repeated sub-compilation
    /// shows up in P2-PERF-01.</c>
    /// </remarks>
    private const int MaxReferenceDepth = 8;

    /// <summary>Every target framework the project declares. One scope per (project, framework).</summary>
    /// <remarks>
    /// Not per project: a multi-targeted project's <c>#if</c>-gated types genuinely differ between
    /// frameworks, so a single scope would have to pick one and be silently wrong about the others.
    /// Measured — <c>MSBuildWorkspace</c> loads one framework and sees one of two conditional types.
    /// </remarks>
    public IReadOnlyList<string> TargetFrameworks(string projectPath)
    {
        var doc = Load(projectPath);
        if (doc is null) return [];

        var many = Property(doc, "TargetFrameworks");
        if (!string.IsNullOrWhiteSpace(many))
        {
            return many.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        var one = Property(doc, "TargetFramework");
        return string.IsNullOrWhiteSpace(one) ? ["net10.0"] : [one];
    }

    public CSharpCompilationResult Compile(string projectPath, string targetFramework, CancellationToken cancellationToken)
    {
        var disclosures = new List<string>();
        var notes = new List<string>();

        // Source generators are never run — there is no analyzer host at all here — so their symbols
        // are absent by construction. Disclosed always, because "absent" must never read as "none".
        disclosures.Add(ExtractionDisclosures.GeneratedCodeNotAnalysed);

        var compilation = Build(
            projectPath, targetFramework, disclosures, notes,
            new Dictionary<string, Compilation>(StringComparer.OrdinalIgnoreCase), 0, cancellationToken);

        return new CSharpCompilationResult(
            compilation, targetFramework, disclosures.Distinct(StringComparer.Ordinal).ToList(), notes);
    }

    private Compilation? Build(
        string projectPath, string tfm, List<string> disclosures, List<string> notes,
        Dictionary<string, Compilation> visited, int depth, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var full = Path.GetFullPath(projectPath);
        if (visited.TryGetValue(full, out var already)) return already;

        if (depth > MaxReferenceDepth)
        {
            disclosures.Add(ExtractionDisclosures.ProjectReferenceUnresolved);
            notes.Add($"project-reference depth limit ({MaxReferenceDepth}) reached at {Path.GetFileName(full)}");
            return null;
        }

        var doc = Load(full);
        if (doc is null)
        {
            notes.Add($"could not parse {Path.GetFileName(full)} as XML");
            return null;
        }

        var dir = Path.GetDirectoryName(full)!;
        var parse = new CSharpParseOptions(preprocessorSymbols: Defines(doc, tfm));

        var trees = new List<SyntaxTree>();
        foreach (var file in Sources(doc, dir))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                trees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(file), parse, path: file));
            }
            catch (IOException ex)
            {
                notes.Add($"unreadable source {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        // ImplicitUsings are generated by the SDK into obj/, which this reader does not read.
        // Reproducing the documented set is reading a specification, not evaluating the project —
        // and without it every Console, Task and IReadOnlyList in a modern project is unresolved.
        var usings = ImplicitUsings(doc);
        if (usings.Count > 0)
        {
            var text = string.Concat(usings.Select(u => $"global using global::{u};\n"));
            trees.Add(CSharpSyntaxTree.ParseText(text, parse, path: "__ImplicitUsings.g.cs"));
        }

        if (IsWpf(doc) && Directory.EnumerateFiles(dir, "*.xaml", SearchOption.AllDirectories).Any())
        {
            disclosures.Add(ExtractionDisclosures.XamlGeneratedMembersNotAnalysed);
        }

        var references = new List<MetadataReference>();
        references.AddRange(FrameworkReferences(doc, tfm, notes));
        references.AddRange(PackageReferences(dir, disclosures, notes, depth));

        // Placeholder before recursing, so a reference cycle terminates instead of stack-overflowing.
        visited[full] = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(full));

        foreach (var referenced in ProjectReferences(doc, dir))
        {
            var child = Build(
                referenced, BestTargetFramework(referenced, tfm), disclosures, notes,
                visited, depth + 1, cancellationToken);

            if (child is not null)
            {
                references.Add(child.ToMetadataReference());
            }
            else
            {
                disclosures.Add(ExtractionDisclosures.ProjectReferenceUnresolved);
                notes.Add($"unresolved ProjectReference: {Path.GetFileName(referenced)}");
            }
        }

        var compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(full), trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        visited[full] = compilation;
        return compilation;
    }

    // ---------------------------------------------------------------- the project file, as data

    private static XDocument? Load(string path)
    {
        try { return XDocument.Load(path); }
        catch (Exception ex) when (ex is IOException or System.Xml.XmlException) { return null; }
    }

    private static string? Property(XDocument doc, string name) =>
        doc.Descendants(name).Select(e => e.Value).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static bool IsTrue(XDocument doc, string name) =>
        string.Equals(Property(doc, name), "true", StringComparison.OrdinalIgnoreCase);

    private static bool IsWpf(XDocument doc) => IsTrue(doc, "UseWPF");

    private static string Normalise(string p) => p.Replace((char)92, '/');

    private static List<string> Sources(XDocument doc, string dir)
    {
        var removed = doc.Descendants("Compile")
            .Select(e => (string?)e.Attribute("Remove"))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => Normalise(v!).TrimEnd('*', '/'))
            .ToList();

        var included = doc.Descendants("Compile")
            .Select(e => (string?)e.Attribute("Include"))
            .Where(v => !string.IsNullOrWhiteSpace(v) && !v!.Contains('*'))
            .Select(v => Path.GetFullPath(Path.Combine(dir, v!)))
            .Where(File.Exists);

        var globbed = Directory
            .EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
            .Where(f =>
            {
                var rel = Normalise(Path.GetRelativePath(dir, f));
                if (rel.StartsWith("bin/", StringComparison.OrdinalIgnoreCase)) return false;
                if (rel.StartsWith("obj/", StringComparison.OrdinalIgnoreCase)) return false;
                return !removed.Any(r => rel.StartsWith(r, StringComparison.OrdinalIgnoreCase));
            });

        return globbed.Concat(included).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<string> ProjectReferences(XDocument doc, string dir) =>
        doc.Descendants("ProjectReference")
            .Select(e => (string?)e.Attribute("Include"))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => Path.GetFullPath(Path.Combine(dir, v!)))
            .Where(File.Exists)
            .ToList();

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

            if (IsWpf(doc))
            {
                result.AddRange([
                    "System.Windows", "System.Windows.Controls", "System.Windows.Data",
                    "System.Windows.Documents", "System.Windows.Input", "System.Windows.Media",
                ]);
            }

            if (IsTrue(doc, "UseWindowsForms"))
            {
                result.AddRange(["System.Drawing", "System.Windows.Forms"]);
            }
        }

        result.AddRange(doc.Descendants("Using")
            .Select(e => (string?)e.Attribute("Include"))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim()));

        return result.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>The preprocessor symbols the compiler would see, synthesised from the TFM.</summary>
    private static List<string> Defines(XDocument doc, string tfm)
    {
        var symbols = new List<string>();
        var bare = tfm.Split('-')[0];
        var moniker = bare.ToUpperInvariant().Replace('.', '_');

        if (moniker.StartsWith("NETSTANDARD", StringComparison.Ordinal))
        {
            symbols.Add(moniker);
            symbols.Add("NETSTANDARD");
        }
        else if (moniker.StartsWith("NET", StringComparison.Ordinal) && Version.TryParse(bare[3..], out var v))
        {
            symbols.Add(moniker);
            symbols.Add("NET");
            symbols.Add("NETCOREAPP");
            for (var major = 5; major <= v.Major; major++)
            {
                var minorMax = major == v.Major ? v.Minor : 0;
                for (var minor = 0; minor <= minorMax; minor++)
                {
                    symbols.Add($"NET{major}_{minor}_OR_GREATER");
                }
            }
        }

        if (tfm.Contains("-windows", StringComparison.OrdinalIgnoreCase)) symbols.Add("WINDOWS");

        var declared = Property(doc, "DefineConstants");
        if (!string.IsNullOrWhiteSpace(declared))
        {
            // $(…) references are MSBuild expressions; evaluating them is exactly what this design
            // refuses to do, so they are skipped rather than guessed at.
            symbols.AddRange(declared
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !x.StartsWith("$(", StringComparison.Ordinal)));
        }

        return symbols.Distinct(StringComparer.Ordinal).ToList();
    }

    private string BestTargetFramework(string referencedProject, string preferred)
    {
        var available = TargetFrameworks(referencedProject);
        if (available.Count == 0) return preferred;
        if (available.Contains(preferred, StringComparer.OrdinalIgnoreCase)) return preferred;

        // A net10.0-windows consumer is satisfied by a net10.0 dependency.
        var bare = preferred.Split('-')[0];
        return available.FirstOrDefault(a => a.Split('-')[0].Equals(bare, StringComparison.OrdinalIgnoreCase))
            ?? available[0];
    }

    // ---------------------------------------------------------------- references

    private static List<MetadataReference> FrameworkReferences(XDocument doc, string tfm, List<string> notes)
    {
        var refs = new List<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bare = tfm.Split('-')[0];

        var wanted = bare.StartsWith("net", StringComparison.OrdinalIgnoreCase) &&
                     Version.TryParse(bare.Length > 3 ? bare[3..] : string.Empty, out var v)
            ? v
            : null;

        // The desktop pack goes FIRST when it applies. Both packs ship a WindowsBase.dll — the base
        // pack's is a 4.0.0.0 facade, the desktop pack's the real 10.0.0.0 assembly — and adding the
        // base pack first lets the facade win on filename, producing CS1705 on every WPF type.
        if ((IsWpf(doc) || IsTrue(doc, "UseWindowsForms")) &&
            tfm.Contains("-windows", StringComparison.OrdinalIgnoreCase))
        {
            var desktop = RefPack("Microsoft.WindowsDesktop.App.Ref", wanted);
            if (desktop is not null) Add(desktop);
            else notes.Add("UseWPF/UseWindowsForms is set but no WindowsDesktop reference pack was found");
        }

        var basePack = bare.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)
            ? RefPack("NETStandard.Library.Ref", null)
            : RefPack("Microsoft.NETCore.App.Ref", wanted);

        if (basePack is null) notes.Add($"no reference pack found for {tfm}");
        else Add(basePack);

        return refs;

        void Add(string directory)
        {
            foreach (var dll in Directory.EnumerateFiles(directory, "*.dll"))
            {
                if (!seen.Add(Path.GetFileName(dll))) continue;
                try { refs.Add(MetadataReference.CreateFromFile(dll)); }
                catch (Exception ex) when (ex is IOException or BadImageFormatException) { }
            }
        }
    }

    /// <summary>Newest reference pack not above <paramref name="wanted"/>, ordered by parsed version.</summary>
    private static string? RefPack(string packName, Version? wanted)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "dotnet", "packs", packName);

        if (!Directory.Exists(root)) return null;

        // Ordered by parsed VERSION, not lexically: a string sort puts 8.0.28 above 10.0.11 and
        // silently compiles against the wrong framework.
        return Directory.EnumerateDirectories(root)
            .Select(d => (Dir: d, Ok: Version.TryParse(Path.GetFileName(d), out var v), Version: v))
            .Where(x => x.Ok && (wanted is null || x.Version!.Major <= wanted.Major))
            .OrderByDescending(x => x.Version)
            .Select(x => SafeDirectories(Path.Combine(x.Dir, "ref"))
                .OrderByDescending(r => r, StringComparer.OrdinalIgnoreCase).FirstOrDefault())
            .FirstOrDefault(d => d is not null && Directory.EnumerateFiles(d, "*.dll").Any());

        static IEnumerable<string> SafeDirectories(string path) =>
            Directory.Exists(path) ? Directory.EnumerateDirectories(path) : [];
    }

    private static List<MetadataReference> PackageReferences(
        string dir, List<string> disclosures, List<string> notes, int depth)
    {
        var refs = new List<MetadataReference>();
        var assets = Path.Combine(dir, "obj", "project.assets.json");

        if (!File.Exists(assets))
        {
            // Reading this file executes nothing — but PRODUCING it requires `dotnet restore`, which
            // is itself MSBuild evaluation. A freshly cloned repository is in this state, and the
            // honest answer is to disclose rather than to run a restore.
            disclosures.Add(ExtractionDisclosures.PackagesNotRestored);
            if (depth == 0) notes.Add("obj/project.assets.json absent — package references unresolved");
            return refs;
        }

        try
        {
            using var stream = File.OpenRead(assets);
            using var json = JsonDocument.Parse(stream);

            if (!json.RootElement.TryGetProperty("packageFolders", out var folders) ||
                !json.RootElement.TryGetProperty("targets", out var targets))
            {
                disclosures.Add(ExtractionDisclosures.PackagesNotRestored);
                return refs;
            }

            var roots = folders.EnumerateObject().Select(p => p.Name).ToList();

            foreach (var target in targets.EnumerateObject())
            foreach (var lib in target.Value.EnumerateObject())
            {
                if (!lib.Value.TryGetProperty("compile", out var compile)) continue;
                foreach (var item in compile.EnumerateObject())
                {
                    if (item.Name.EndsWith("_._", StringComparison.Ordinal)) continue;
                    foreach (var root in roots)
                    {
                        var path = Path.Combine(
                            root,
                            lib.Name.Replace('/', Path.DirectorySeparatorChar),
                            item.Name.Replace('/', Path.DirectorySeparatorChar));

                        if (!File.Exists(path)) continue;
                        try { refs.Add(MetadataReference.CreateFromFile(path)); }
                        catch (Exception ex) when (ex is IOException or BadImageFormatException) { }
                        break;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            disclosures.Add(ExtractionDisclosures.PackagesNotRestored);
            notes.Add($"project.assets.json unreadable: {ex.Message}");
        }

        return refs;
    }
}
