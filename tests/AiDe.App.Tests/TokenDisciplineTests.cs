using System.Text.RegularExpressions;

namespace AiDe.App.Tests;

/// <summary>
/// Enforces UI Standard U3 ("reference a token, never an arbitrary value") OUTWARD against the built
/// XAML.
/// </summary>
/// <remarks>
/// This exists because the repo's deterministic craft gate (<c>ui-craft-gate.py</c>, wrapping
/// Impeccable) parses web sources and returns an empty result set for a WPF project — a clean report
/// over a corpus it never read, which is a success-shaped failure, not a pass. Rather than record the
/// gap as prose, it becomes this control: a lesson recorded as prose is a memoir.
///
/// Scope is honest about what it can and cannot see: it catches raw colour literals in component
/// markup. It cannot judge hierarchy, archetype fit, or whether the copy is true.
/// </remarks>
public sealed class TokenDisciplineTests
{
    private static readonly Regex RawHex = new(@"#[0-9a-fA-F]{3}(?:[0-9a-fA-F]{3})?\b", RegexOptions.Compiled);

    /// <summary>App.xaml is the token dictionary — the one place a literal colour is the point.</summary>
    private const string TokenDictionary = "App.xaml";

    public static TheoryData<string> ComponentMarkup()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.EnumerateFiles(ProjectRoot(), "*.xaml", SearchOption.AllDirectories))
        {
            if (!Path.GetFileName(file).Equals(TokenDictionary, StringComparison.OrdinalIgnoreCase))
            {
                data.Add(file);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ComponentMarkup))]
    public void ComponentMarkup_UsesTokensNotRawColourValues(string xamlPath)
    {
        var offenders = File.ReadAllLines(xamlPath)
            .Select((line, index) => (Line: line, Number: index + 1))
            .Where(l => RawHex.IsMatch(l.Line))
            .Select(l => $"{Path.GetFileName(xamlPath)}:{l.Number}: {l.Line.Trim()}")
            .ToList();

        Assert.True(offenders.Count == 0,
            "Component markup must reference a semantic brush from App.xaml, never a raw colour:"
            + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>The corpus check the craft gate itself failed: a control that scans nothing is not a control.</summary>
    [Fact]
    public void TheScan_CoversANonEmptyCorpus()
    {
        Assert.NotEmpty(ComponentMarkup());
    }

    private static string ProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AiDe.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "src", "AiDe.App");
    }
}
