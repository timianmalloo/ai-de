using System.Diagnostics;
using AiDe.Core;
using AiDe.Core.Extraction;

namespace AiDe.Bench;

/// <summary>
/// Indexes a repository through the product's own path and reports what the graph now contains.
/// </summary>
/// <remarks>
/// <b>Someone else's repository, not ours.</b> AiDe's own projects were the corpus the extractor was
/// built against, which makes them the worst possible evidence that it works. A repository written
/// without knowledge of this tool is the first honest test — and the first one where "we could not
/// see that" has real consequences for a user.
/// </remarks>
internal static class IndexRepository
{
    internal static async Task<int> RunAsync(string repositoryPath)
    {
        var root = Path.GetFullPath(repositoryPath);
        if (!Directory.Exists(root))
        {
            Console.WriteLine($"no repository at {root}");
            return 2;
        }

        var data = Path.Combine(Path.GetTempPath(), "aide-index", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(data);

        Console.WriteLine($"Indexing {root}");
        Console.WriteLine(new string('=', 100));

        var scopes = CSharpScopeDiscovery.Discover(root);
        Console.WriteLine($"discovered {scopes.Count} scope(s):");
        foreach (var scope in scopes) Console.WriteLine($"  {scope.ScopeId}");
        Console.WriteLine();

        try
        {
            using var core = WorkspaceCore.Open(
                "bench-index", root, data,
                new CompositeExtractor(new CSharpExtractor(), new FixtureExtractor()));

            var watch = Stopwatch.StartNew();
            var result = await core.IndexCSharpAsync("bench-rev");
            watch.Stop();

            Console.WriteLine($"indexed  : {result.ScopesIndexed}/{result.ScopesFound} scope(s) in {watch.Elapsed.TotalSeconds:F1}s");
            Console.WriteLine($"assertions: {result.Assertions:N0}");
            Console.WriteLine($"failed    : {(result.Failed.Count == 0 ? "none" : string.Join(", ", result.Failed))}");
            Console.WriteLine($"disclosed : {(result.Disclosures.Count == 0 ? "none" : string.Join(", ", result.Disclosures))}");
            Console.WriteLine();

            // The point of indexing is that you can then ASK something. A count of rows proves the
            // write worked; a query proves the graph is usable.
            var find = core.Projections.Find(string.Empty, 5);
            Console.WriteLine($"sample nodes ({find.Matches.Count} of many):");
            foreach (var match in find.Matches) Console.WriteLine($"  {match.NodeId}");

            var probe = find.Matches.FirstOrDefault(m => !m.NodeId.StartsWith("scope:", StringComparison.Ordinal));
            if (probe is not null)
            {
                var describe = core.Projections.Describe(probe.NodeId, 8);
                Console.WriteLine();
                Console.WriteLine($"describe {probe.NodeId}: {describe.Neighbors.Count} neighbour(s)");
                foreach (var edge in describe.Neighbors.Take(8))
                {
                    Console.WriteLine($"  {edge.Subject} --{edge.Predicate}--> {edge.Object}  [{edge.Status}] {edge.Provenance.ArtifactPathId}:{edge.Provenance.SourceLocation}");
                }
            }

            Console.WriteLine(new string('=', 100));
            return result.ScopesIndexed > 0 ? 0 : 1;
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(data, recursive: true); } catch (IOException) { }
        }
    }
}
