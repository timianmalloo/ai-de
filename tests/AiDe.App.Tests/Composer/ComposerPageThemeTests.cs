using System.Text.RegularExpressions;
using System.Windows;
using AiDe.App.Workbench.Composer;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// The composer page's theme is read from the shell's token dictionary — every role resolves, and
/// resolves to the token it names.
/// </summary>
/// <remarks>
/// <para>The rendered pairings are the census's to measure (<c>ShellContrastCensusTests</c>, the
/// webview population). This proves the push's <i>source</i>: a role whose token is renamed in
/// <c>App.xaml</c> would silently drop out of the push and the page would fall back to its
/// stylesheet value — the census would still measure that fallback, but nothing would say the
/// shell and the page had stopped agreeing. This does (TC3).</para>
/// </remarks>
public sealed class ComposerPageThemeTests
{
    private static readonly Regex Hex6 = new("^#[0-9A-F]{6}$", RegexOptions.Compiled);

    [Fact]
    public void EveryRoleResolvesFromTheTokenDictionary_ToASixDigitHex()
    {
        Sta.Run(() =>
        {
            var theme = ComposerPageTheme.From(ThemeProbe.AppTheme());

            var missing = ComposerPageTheme.Roles
                .Where(r => !theme.ContainsKey(r.Property))
                .Select(r => $"{r.Property} ← {r.Token}")
                .ToList();

            Assert.True(missing.Count == 0,
                "these composer page roles name a token App.xaml does not declare as a SolidColorBrush, so "
                + "the page would draw its fallback while the shell drew the token:" + Environment.NewLine
                + string.Join(Environment.NewLine, missing));

            Assert.All(theme.Values, value => Assert.Matches(Hex6, value));
            Assert.Equal(ComposerPageTheme.Roles.Count, theme.Count);
        });
    }

    /// <summary>
    /// The role table, pinned from INV-0008 §7 Fix C rather than read back from <c>Roles</c> — a
    /// mapping that drifts (<c>--text-muted</c> to the disabled token would still clear every floor)
    /// fails here, not in a census that only measures ratios.
    /// </summary>
    [Theory]
    [InlineData("--surface", "SurfaceBrush")]
    [InlineData("--surface-raised", "SurfaceRaisedBrush")]
    [InlineData("--surface-sunken", "SurfaceSunkenBrush")]
    [InlineData("--text", "TextBrush")]
    [InlineData("--text-muted", "TextMutedBrush")]
    [InlineData("--text-disabled", "DisabledTextBrush")]
    [InlineData("--accent", "AccentBrush")]
    [InlineData("--accent-contrast", "AccentContrastBrush")]
    [InlineData("--border", "BorderBrush")]
    [InlineData("--danger", "DangerBrush")]
    [InlineData("--focus", "FocusBrush")]
    public void ARoleCarriesItsTokensValue_NotAValueOfItsOwn(string property, string token)
    {
        Sta.Run(() =>
        {
            var resources = ThemeProbe.AppTheme();
            var theme = ComposerPageTheme.From(resources);

            Assert.Contains((property, token), ComposerPageTheme.Roles);

            var c = ThemeProbe.Token(resources, token);
            Assert.Equal($"#{c.R:X2}{c.G:X2}{c.B:X2}", theme[property]);
        });
    }

    [Fact]
    public void TheRoleTableHasExactlyThePinnedRoles()
    {
        Assert.Equal(11, ComposerPageTheme.Roles.Count);
    }

    /// <summary>
    /// Every way a colour can be written in the page's stylesheet: <c>var(--x, fallback)</c> (any
    /// fallback), <c>var(--x)</c> with none, a hex literal of any width, a functional colour, or a
    /// named colour. Each is classified below; only <c>var(--role, #6hex-of-the-token)</c> passes.
    /// </summary>
    private static readonly Regex ColourUse = new(
        @"var\((--[A-Za-z0-9-]+)(?:\s*,\s*([^)]*))?\)"
        + @"|(?<!var\([^)]*)(#[0-9A-Fa-f]{3,8})\b"
        + @"|\b(rgba?|hsla?|color|lab|lch|oklab|oklch)\("
        + @"|(?<=:\s*|\s)(white|black|red|green|blue|gray|grey|yellow|orange|purple|silver|navy|teal|maroon|lime|aqua|fuchsia|olive)(?=\s*[;!\s])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Every colour the composer page's stylesheet uses is <c>var(--role, #fallback)</c> with a role
    /// the host pushes and a fallback equal to that role's token in <c>App.xaml</c>; the host-probe
    /// page, which has no host to push to, uses only token values. This is the reader half of the
    /// push (E8): the census measures the pushed value, so a fallback that drifted would never be
    /// rendered under test — it would ship, unmeasured, for the frame before the push and for any
    /// page the host never reached.
    /// </summary>
    [Theory]
    [InlineData("composer.html", true, 20)]
    [InlineData("composer-host.html", false, 5)]
    public void EveryFallbackInThePageIsItsTokensDeclaredValue(string page, bool mustUseCustomProperties, int atLeast)
    {
        Sta.Run(() =>
        {
            var resources = ThemeProbe.AppTheme();
            var byRole = ComposerPageTheme.From(resources);
            var tokenValues = byRole.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var path = Path.Combine(WebRoot(), page);
            var css = StyleBlock(File.ReadAllText(path));
            var offenders = new List<string>();
            var uses = 0;

            foreach (Match m in ColourUse.Matches(css))
            {
                uses++;
                if (m.Groups[1].Success)
                {
                    var role = m.Groups[1].Value;
                    var fallback = m.Groups[2].Success ? m.Groups[2].Value.Trim() : null;
                    if (!byRole.TryGetValue(role, out var expected))
                    {
                        offenders.Add($"{role}: not a role the host pushes");
                    }
                    else if (fallback is null)
                    {
                        offenders.Add($"{role}: no fallback — the frame before the push would render the browser's initial value");
                    }
                    else if (!string.Equals(fallback, expected, StringComparison.OrdinalIgnoreCase))
                    {
                        offenders.Add($"{role}: fallback {fallback} but the token is {expected}");
                    }
                }
                else if (m.Groups[3].Success)
                {
                    var literal = m.Groups[3].Value;
                    if (mustUseCustomProperties)
                    {
                        offenders.Add($"{literal}: a literal colour, not var(--role, #fallback)");
                    }
                    else if (!tokenValues.Contains(literal))
                    {
                        offenders.Add($"{literal}: not a token's value (or not six-digit)");
                    }
                }
                else
                {
                    offenders.Add($"{m.Value.Trim()}: a functional or named colour, outside the token system");
                }
            }

            Assert.True(uses >= atLeast, $"{page}: only {uses} colour use(s) found in its stylesheet (expected at least {atLeast}), so this scan is reading the wrong block (DC-016)");
            Assert.True(offenders.Count == 0,
                $"{page}: colours outside the pushed roles / their token values (INV-0008 Fix C):" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
        });
    }

    private static string StyleBlock(string html)
    {
        // The FIRST real <style> element, not the one an HTML comment mentions (DC-141).
        var withoutComments = Regex.Replace(html, "<!--.*?-->", "", RegexOptions.Singleline);
        var m = Regex.Match(withoutComments, "<style>(.*?)</style>", RegexOptions.Singleline);
        Assert.True(m.Success, "no <style> element outside comments");
        return Regex.Replace(m.Groups[1].Value, @"/\*.*?\*/", "", RegexOptions.Singleline);
    }

    private static string WebRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AiDe.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "src", "AiDe.App", "Web");
    }

    /// <summary>The on-accent pairing the page's mention chip draws is the token pair the shell's tabs use — one pairing, not two.</summary>
    [Fact]
    public void TheOnAccentInk_IsTheAccentContrastToken()
    {
        Sta.Run(() =>
        {
            var resources = ThemeProbe.AppTheme();
            var theme = ComposerPageTheme.From(resources);

            var ink = ThemeProbe.Token(resources, "AccentContrastBrush");
            var ground = ThemeProbe.Token(resources, "AccentBrush");

            Assert.Equal($"#{ink.R:X2}{ink.G:X2}{ink.B:X2}", theme["--accent-contrast"]);
            Assert.True(ThemeProbe.Contrast(ink, ground) >= 4.5,
                $"AccentContrastBrush on AccentBrush measures {ThemeProbe.Contrast(ink, ground):0.00}:1 in the dictionary — below the 4.5 body floor");
        });
    }

    [Fact]
    public void NoApplication_MeansNoTheme_NotAnInventedOne()
    {
        Assert.Empty(ComposerPageTheme.From(null));
    }

    [Fact]
    public void AnEmptyDictionary_YieldsNoRoles()
    {
        Sta.Run(() => Assert.Empty(ComposerPageTheme.From(new ResourceDictionary())));
    }
}
