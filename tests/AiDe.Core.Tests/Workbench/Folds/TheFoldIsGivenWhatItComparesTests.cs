using AiDe.Core.Workbench.Records.Folds;

namespace AiDe.Core.Tests.Workbench.Folds;

/// <summary>
/// Ruling 178 / DM7, node 2: <see cref="CurrentnessFold"/> takes what it compares as named inputs. No caller builds a
/// placeholder <c>AttemptRow</c> to carry a body sha in the projection-sha slot; the row is the store's own record.
/// </summary>
public sealed class TheFoldIsGivenWhatItComparesTests
{
    [Fact]
    public void TheFold_HasAnInputThatNamesTheBindingAndBothShas() =>
        Assert.NotNull(typeof(CurrentnessFold).GetMethod(
            nameof(CurrentnessFold.Evaluate),
            [typeof(int), typeof(int?), typeof(string), typeof(string)]));

    [Fact]
    public void NoCallerOutsideTheStore_ConstructsAnAttemptRow()
    {
        var src = Path.Combine(RepoRoot(), "src");
        var storeReaders = Path.Combine(src, "AiDe.Core", "Store") + Path.DirectorySeparatorChar;

        var offenders = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => !f.StartsWith(storeReaders, StringComparison.Ordinal))
            .Where(f => File.ReadAllText(f).Contains("new AttemptRow(", StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(src, f))
            .ToList();

        Assert.Empty(offenders);
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
