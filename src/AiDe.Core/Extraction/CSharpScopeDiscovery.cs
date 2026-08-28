namespace AiDe.Core.Extraction;

/// <summary>One extraction scope: a project built for one target framework.</summary>
/// <param name="ScopeId">Stable identity, of the form <c>csharp:&lt;project&gt;:&lt;tfm&gt;</c>.</param>
public sealed record ScopeDescriptor(string ScopeId, string ProjectPath, string TargetFramework)
{
    /// <summary>What the user sees in a pane title or a scope list.</summary>
    public string DisplayName =>
        $"{Path.GetFileNameWithoutExtension(ProjectPath)} ({TargetFramework})";
}

/// <summary>
/// Finds the C# scopes in a repository — <b>one per (project, target framework)</b>.
/// </summary>
/// <remarks>
/// <para>The grain is the finding, not a preference: a multi-targeted project's <c>#if</c>-gated
/// types genuinely differ between frameworks, so a single scope per project would have to pick one
/// and be silently wrong about the others (measured in <c>spikes/extraction-fidelity</c>).</para>
///
/// <para><b>Directories are skipped by name, and the list is deliberately short.</b> Skipping too
/// much is how a real project silently fails to appear; <c>bin</c>, <c>obj</c> and <c>.git</c> are
/// the ones that contain no source a user wrote.</para>
/// </remarks>
public static class CSharpScopeDiscovery
{
    private static readonly string[] Skip = ["bin", "obj", ".git", "node_modules"];

    /// <summary>
    /// Every C# scope under <paramref name="rootPath"/>, ordered so the list is stable between runs.
    /// </summary>
    /// <remarks>
    /// A stable order matters more than it looks: scope ids feed generation numbers and the health
    /// view, and a set that reshuffles between runs makes two identical repositories look different.
    /// </remarks>
    public static IReadOnlyList<ScopeDescriptor> Discover(string rootPath, CSharpProjectReader? reader = null)
    {
        if (!Directory.Exists(rootPath)) return [];

        reader ??= new CSharpProjectReader();
        var scopes = new List<ScopeDescriptor>();

        foreach (var project in Projects(rootPath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileNameWithoutExtension(project);
            var frameworks = reader.TargetFrameworks(project);

            if (frameworks.Count == 0)
            {
                // A project file that cannot be READ still gets a scope, deliberately. Returning
                // nothing here would make an unparseable project VANISH: not indexed, not failed,
                // not counted — the user would never learn it exists. The scope goes on to fail
                // extraction, which quarantines it and raises a health incident, so "we could not
                // read this project" is a reported outcome rather than an absence.
                scopes.Add(new ScopeDescriptor($"csharp:{name}:unknown", project, "unknown"));
                continue;
            }

            foreach (var tfm in frameworks)
            {
                scopes.Add(new ScopeDescriptor($"csharp:{name}:{tfm}", project, tfm));
            }
        }

        return scopes;
    }

    private static IEnumerable<string> Projects(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var directory = pending.Pop();

            string[] entries;
            try { entries = Directory.GetFiles(directory, "*.csproj"); }
            catch (UnauthorizedAccessException) { continue; }
            catch (IOException) { continue; }

            foreach (var project in entries) yield return project;

            IEnumerable<string> children;
            try { children = Directory.EnumerateDirectories(directory); }
            catch (UnauthorizedAccessException) { continue; }
            catch (IOException) { continue; }

            foreach (var child in children)
            {
                var name = Path.GetFileName(child);
                if (Skip.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
                pending.Push(child);
            }
        }
    }
}

/// <summary>
/// Routes an extraction to the extractor that owns its scope kind.
/// </summary>
/// <remarks>
/// Routing on the scope id's prefix rather than on a registration table because the prefix is
/// already the scope's identity — a separate mapping is a second thing that can disagree with the
/// ids actually in the store.
/// </remarks>
public sealed class CompositeExtractor(IExtractor csharp, IExtractor fallback) : IExtractor
{
    public string ScopeKind => "composite";

    public Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken) =>
        request.ScopeId.StartsWith("csharp:", StringComparison.Ordinal)
            ? csharp.ExtractAsync(request, cancellationToken)
            : fallback.ExtractAsync(request, cancellationToken);
}
