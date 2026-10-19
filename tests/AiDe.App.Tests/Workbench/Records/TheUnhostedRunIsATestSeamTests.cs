using System.IO;
using System.Reflection;
using AiDe.App.Workbench.Records;

namespace AiDe.App.Tests.Records;

/// <summary>
/// Ruling 159(2), the test seam: a direct run (no attempt host, so nothing recorded) exists only as the internal
/// <c>NotebookRuns.UnhostedForTests()</c>, reachable through InternalsVisibleTo, and nothing under <c>src/</c> calls it.
/// </summary>
public sealed class TheUnhostedRunIsATestSeamTests
{
    private const string Seam = "UnhostedForTests";

    [Fact]
    public void TheDirectRunSeam_IsInternal()
    {
        var seam = typeof(NotebookRuns).GetMethod(Seam, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(seam is { IsAssembly: true },
            $"the direct-run seam must be an internal NotebookRuns.{Seam}(), reachable only through InternalsVisibleTo");
    }

    /// <summary>
    /// <b>Root:</b> <c>src/</c>. <b>Recursion:</b> every <c>*.cs</c>, skipping <c>bin/</c> and <c>obj/</c>, code lines
    /// only (a line starting <c>//</c> is prose). <b>Token:</b> <c>UnhostedForTests</c>. <b>Allowlist, exactly:</b> its
    /// declaration in <c>AiDe.App/Workbench/Records/NotebookRuns.cs</c>. Any call in product code reddens this by path.
    /// </summary>
    [Fact]
    public void NothingUnderSrc_CallsTheDirectRunSeam_OnlyItsDeclarationNamesIt()
    {
        var src = Path.Combine(RepoRoot(), "src");
        var sites = new List<(string File, string Kind)>();
        foreach (var file in Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            foreach (var line in File.ReadLines(file))
            {
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal) || !line.Contains(Seam, StringComparison.Ordinal))
                {
                    continue;
                }

                var kind = line.Contains($"internal void {Seam}()", StringComparison.Ordinal) ? "declaration" : "call";
                sites.Add((Path.GetRelativePath(src, file).Replace('\\', '/'), kind));
            }
        }

        Assert.Equal([("AiDe.App/Workbench/Records/NotebookRuns.cs", "declaration")], sites);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
