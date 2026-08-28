using AiDe.Core.Extraction;
using Microsoft.CodeAnalysis;

namespace ExtractionFidelitySpike;

/// <summary>
/// Runs the measurement through <see cref="CSharpProjectReader"/> — the reader the product ships.
/// </summary>
/// <remarks>
/// The spike originally carried its own prototype, which was the right shape while the contract was
/// being decided. Now that the reader exists, measuring the prototype would report a number for code
/// nobody runs, and the two would drift apart exactly where it mattered least visibly.
/// </remarks>
internal static class ShippedReader
{
    private static readonly CSharpProjectReader Reader = new();

    internal static IReadOnlyList<string> TargetFrameworks(string projectPath) =>
        Reader.TargetFrameworks(projectPath);

    internal static List<DirectExtractor.Extraction> ExtractAll(string projectPath)
    {
        var results = new List<DirectExtractor.Extraction>();

        foreach (var tfm in Reader.TargetFrameworks(projectPath))
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var compiled = Reader.Compile(projectPath, tfm, CancellationToken.None);
            watch.Stop();

            var types = new List<INamedTypeSymbol>();
            if (compiled.Compilation is not null)
            {
                Walk(compiled.Compilation.Assembly.GlobalNamespace, types);
            }

            var unresolvedNames = new List<string>();
            var (edges, unresolved) = DirectExtractor.CountEdges(types, unresolvedNames);

            var notes = compiled.Notes.Concat(compiled.Disclosures.Select(d => "discloses: " + d)).ToList();
            if (unresolved > 0)
            {
                notes.Add("unresolved edges point at: " + string.Join(", ", unresolvedNames
                    .GroupBy(n => n, StringComparer.Ordinal)
                    .OrderByDescending(g => g.Count()).Take(8)
                    .Select(g => $"{g.Key}x{g.Count()}")));
            }

            results.Add(new DirectExtractor.Extraction(
                Path.GetFileName(projectPath), tfm,
                compiled.Compilation?.SyntaxTrees.Count() ?? 0,
                compiled.Compilation?.References.Count() ?? 0,
                types.Count, edges, unresolved, watch.Elapsed.TotalMilliseconds,
                types.Select(t => t.ToDisplayString()).OrderBy(n => n, StringComparer.Ordinal).ToList(),
                notes));
        }

        return results;
    }

    private static void Walk(INamespaceSymbol ns, List<INamedTypeSymbol> into)
    {
        foreach (var t in ns.GetTypeMembers()) into.Add(t);
        foreach (var c in ns.GetNamespaceMembers()) Walk(c, into);
    }
}
