namespace AiDe.App.Tests.Perspectives;

/// <summary>
/// <b>T21 node 4 check 2.</b> The off-screen report sentence ("… moved onto this display because the
/// saved display is not connected") has one definition in <c>src</c>, in <c>AiDe.Core</c>, so the tree
/// restore (<c>LayoutStore.Load</c>) and the zone restore (<c>LayoutPersistence.Restore</c>) say the
/// same words. Two definitions of one fact drift (DM7).
/// </summary>
public sealed class OffScreenSentenceDefinitionTests
{
    [Fact]
    public void TheOffScreenSentence_IsDefinedOnce_InCore()
    {
        const string Phrase = "saved display is not connected";
        var src = Path.Combine(RepoRoot(), "src");
        var sep = Path.DirectorySeparatorChar;
        var hits = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{sep}bin{sep}", StringComparison.Ordinal) && !f.Contains($"{sep}obj{sep}", StringComparison.Ordinal))
            .SelectMany(f => File.ReadLines(f)
                .Select((line, i) => (Where: $"{Path.GetRelativePath(src, f)}:{i + 1}", Line: line)))
            .Where(h => h.Line.Contains(Phrase, StringComparison.Ordinal))
            .Select(h => h.Where)
            .ToList();

        var only = Assert.Single(hits);
        Assert.StartsWith($"AiDe.Core{sep}", only, StringComparison.Ordinal);
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
