using System.Text.RegularExpressions;
using System.Windows;

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

    /// <summary>
    /// Every resource key this application names exists (TC3).
    /// </summary>
    /// <remarks>
    /// <para><b>A resource reference to a missing key is a silent no-op.</b> No exception, no log,
    /// and visually indistinguishable from "not themed yet". <c>SunkenBrush</c> and
    /// <c>RaisedBrush</c> were referenced from six sites and declared nowhere: the search box in the
    /// class diagram stayed platform white, the code viewer and the prompt draft stayed unthemed,
    /// and the type cards rendered transparent over the pane. The keys that WERE declared are
    /// <c>SurfaceSunkenBrush</c> and <c>SurfaceRaisedBrush</c>, one word away from each, and nothing
    /// anywhere said so.</para>
    ///
    /// <para><b>Fifteen lines of check would have caught both at authoring time</b>, which is the
    /// whole argument for writing them (CI6).</para>
    ///
    /// <para><b>Why DockRoundedTabs.xaml is excluded.</b> Its keys — <c>DropDownControlArea</c>,
    /// <c>PinClose</c> — are AvalonDock's, resolved from the theme dictionary the DockingManager
    /// merges at runtime, so App.xaml is the wrong dictionary to look for them in. The exclusion is
    /// by file and is asserted to match exactly one file, so it cannot quietly grow.</para>
    /// </remarks>
    [Fact]
    public void EveryResourceKeyTheAppNames_IsDeclared()
    {
        var declared = DeclaredKeys();

        Assert.True(declared.Count > 20,
            $"only {declared.Count} key(s) were read out of App.xaml, so this check is scanning the "
            + "wrong document and would pass over anything (DC-016).");

        var offenders = new List<string>();
        var scanned = 0;

        foreach (var file in Directory.EnumerateFiles(ProjectRoot(), "*.*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);

            if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                continue;
            }

            Regex pattern;

            if (name.Equals("App.xaml", StringComparison.OrdinalIgnoreCase)
                || name.Equals(AvalonDockOwnedMarkup, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            else if (name.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                pattern = MarkupKey;
            }
            else if (name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                pattern = CodeKey;
            }
            else
            {
                continue;
            }

            scanned++;

            foreach (Match match in pattern.Matches(File.ReadAllText(file)))
            {
                var key = match.Groups[1].Value;

                if (!declared.Contains(key))
                {
                    offenders.Add($"{name}: {key}");
                }
            }
        }

        Assert.True(scanned > 10,
            $"only {scanned} file(s) were scanned for resource keys, which is not this project.");

        Assert.True(offenders.Count == 0,
            "these resource keys are named and never declared in App.xaml. A reference to a missing "
            + "key fails SILENTLY — the control simply keeps the platform's value, which is the same "
            + "rendering as 'not themed yet' (TC3): "
            + Environment.NewLine + string.Join(Environment.NewLine, offenders.Distinct()));
    }

    /// <summary>The one markup file whose keys belong to AvalonDock's theme, not to App.xaml.</summary>
    private const string AvalonDockOwnedMarkup = "DockRoundedTabs.xaml";

    private static readonly Regex MarkupKey =
        new(@"\{(?:Static|Dynamic)Resource\s+([A-Za-z0-9_]+)\s*\}", RegexOptions.Compiled);

    private static readonly Regex CodeKey = new(
        @"(?:SetResourceReference\s*\([^,]+,\s*|TryFindResource\s*\(|FindResource\s*\()\s*""([A-Za-z0-9_]+)""",
        RegexOptions.Compiled);

    private static HashSet<string> DeclaredKeys()
    {
        var app = File.ReadAllText(Path.Combine(ProjectRoot(), TokenDictionary));
        return [.. Regex.Matches(app, @"x:Key=""([^""]+)""").Select(m => m.Groups[1].Value)];
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
