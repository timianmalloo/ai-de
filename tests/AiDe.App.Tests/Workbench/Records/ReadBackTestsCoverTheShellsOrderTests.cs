using System.IO;
using System.Text.RegularExpressions;

namespace AiDe.App.Tests.Records;

/// <summary>
/// Ruling 172(4), the control against the test shape that hid WL-1a's read-back gap: T17's tests bound the host BEFORE
/// building the document, while the shell builds the document while the host still opens, so they passed and the product
/// never read back. A test-source census over <c>tests/</c>.
/// </summary>
/// <remarks>
/// The pattern, by name, <b>bind-first read-back</b>: a file that observes the restart read-back (it names
/// <c>LastReadBack</c> or <c>Runs.ReadBack</c>) and calls <c>.BindHost(</c> with a <c>new SessionDocumentSurface(</c>
/// within the next <see cref="Window"/> lines. Such a file passes only when it also covers the asynchronous order: it
/// calls <c>.BindHostWhenOpen(</c> or <c>.ReviveRestoredSessionDocuments(</c>.
/// </remarks>
public sealed partial class ReadBackTestsCoverTheShellsOrderTests
{
    /// <summary>How many lines after a <c>BindHost</c> call a document construction counts as "bound first".</summary>
    private const int Window = 3;

    [GeneratedRegex(@"\b(LastReadBack|Runs\.ReadBack)\b")]
    private static partial Regex ObservesTheReadBack();

    [GeneratedRegex(@"\.BindHost\(")]
    private static partial Regex BindsTheHost();

    [GeneratedRegex(@"new SessionDocumentSurface\(")]
    private static partial Regex BuildsTheDocument();

    [GeneratedRegex(@"\.(BindHostWhenOpen|ReviveRestoredSessionDocuments)\(")]
    private static partial Regex CoversTheAsyncOrder();

    /// <summary>The bind-first lines of <paramref name="lines"/> (1-based), or none when the file is not a bind-first read-back test.</summary>
    internal static IReadOnlyList<int> BindFirstReadBack(string[] lines)
    {
        var text = string.Join('\n', lines);
        if (!ObservesTheReadBack().IsMatch(text) || CoversTheAsyncOrder().IsMatch(text))
        {
            return [];
        }

        List<int> found = [];
        for (var i = 0; i < lines.Length; i++)
        {
            if (BindsTheHost().IsMatch(lines[i]) && lines.Skip(i).Take(Window + 1).Any(l => BuildsTheDocument().IsMatch(l)))
            {
                found.Add(i + 1);
            }
        }

        return found;
    }

    [Fact]
    public void ThePattern_FlagsABindFirstReadBack_AndPassesOneThatAlsoCoversTheAsyncOrder()
    {
        string[] bindFirst =
        [
            "model.Runs.BindHost(host, Scope);",
            "var surface = new SessionDocumentSurface(model, null, announcer);",
            "await surface.LastReadBack;",
        ];
        Assert.Equal([1], BindFirstReadBack(bindFirst));
        Assert.Empty(BindFirstReadBack([.. bindFirst, "other.Runs.BindHostWhenOpen(open, Scope);"]));
        Assert.Empty(BindFirstReadBack(["model.Runs.BindHost(host, Scope);", "var surface = new SessionDocumentSurface(model, null, announcer);"]));
    }

    [Fact]
    public void NoReadBackTest_BindsTheHostBeforeTheDocument_WithoutAlsoCoveringTheShellsAsynchronousOrder()
    {
        var tests = Path.Combine(RepoRoot(), "tests");
        var files = Directory.EnumerateFiles(tests, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && Path.GetFileName(f) != $"{nameof(ReadBackTestsCoverTheShellsOrderTests)}.cs")
            .ToList();

        var readBackTests = files.Count(f => ObservesTheReadBack().IsMatch(File.ReadAllText(f)));
        Assert.True(readBackTests >= 3, $"the census found {readBackTests} read-back test files; it is not reading tests/");

        var offenders = files
            .Select(f => (File: Path.GetRelativePath(RepoRoot(), f).Replace('\\', '/'), Lines: BindFirstReadBack(File.ReadAllLines(f))))
            .Where(x => x.Lines.Count > 0)
            .Select(x => $"{x.File}:{string.Join(",", x.Lines)}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "bind-first read-back: these tests bind the host before building the document and never cover the shell's order "
            + "(BindHostWhenOpen, then the document; Ruling 172): " + string.Join("; ", offenders));
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
