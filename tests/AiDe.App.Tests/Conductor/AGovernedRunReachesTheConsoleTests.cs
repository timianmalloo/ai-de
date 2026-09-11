using System.Text.RegularExpressions;

namespace AiDe.App.Tests.Conductor;

/// <summary>
/// A governed run's events reach a Console surface in the shipped product, not only in a test.
/// </summary>
/// <remarks>
/// <para><b>Red first, and the red was mechanical.</b> <c>SessionLane</c> — the type whose entire
/// job is to drain the plane's queue into a session document — was constructed in nine places, all
/// of them test files, and in <b>no</b> file under <c>src/</c>. Every assertion about the console
/// rendering a merged stream was therefore an assertion about wiring the tests assembled
/// themselves, which is exactly what <c>GovernedRunHost</c>'s own remarks refuse: <i>a
/// hand-assembled harness does not count as exit evidence</i>.</para>
///
/// <para><b>A source scan rather than a behavioural assertion, deliberately.</b> The behavioural
/// test can be satisfied by a lane the test built; only counting construction sites in the product
/// can tell "the product wires this" from "the suite wires this".</para>
/// </remarks>
public sealed class AGovernedRunReachesTheConsoleTests
{
    private static DirectoryInfo RepoRoot()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);

        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AiDe.sln")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        return root!;
    }

    private static List<string> FilesUnder(string directory, string pattern)
    {
        var root = RepoRoot();
        var path = Path.Combine(root.FullName, directory);
        var hits = new List<string>();

        if (!Directory.Exists(path))
        {
            return hits;
        }

        foreach (var file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            if (Regex.IsMatch(File.ReadAllText(file), pattern))
            {
                hits.Add(Path.GetRelativePath(root.FullName, file));
            }
        }

        return hits;
    }

    [Fact]
    public void TheProductItselfConstructsASessionLane()
    {
        var product = FilesUnder("src", @"new SessionLane\s*\(");

        Assert.True(product.Count > 0,
            "no file under src/ constructs a SessionLane, so nothing in the shipped product carries a "
            + "governed run's events to a Console surface — every console assertion in the suite is "
            + "then about wiring the suite built for itself, which GovernedRunHost's own remarks "
            + "refuse as exit evidence.");
    }

    [Fact]
    public void TheScanActuallyFindsLanesSomewhere()
    {
        // The DC-016 guard. If SessionLane is renamed or the construction is wrapped, the assertion
        // above passes by examining nothing — the shape this whole file exists to prevent.
        var anywhere = FilesUnder("src", @"new SessionLane\s*\(").Count
            + FilesUnder("tests", @"new SessionLane\s*\(").Count;

        Assert.True(anywhere > 0,
            "no construction of a SessionLane was found anywhere in src/ or tests/, so the scan above "
            + "is checking an empty set. Either the type was renamed and this control needs updating, "
            + "or it has no callers at all.");
    }
}
