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

    [Fact]
    public void ARoleCarriesItsTokensValue_NotAValueOfItsOwn()
    {
        Sta.Run(() =>
        {
            var resources = ThemeProbe.AppTheme();
            var theme = ComposerPageTheme.From(resources);

            foreach (var (property, token) in ComposerPageTheme.Roles)
            {
                var c = ThemeProbe.Token(resources, token);
                Assert.Equal($"#{c.R:X2}{c.G:X2}{c.B:X2}", theme[property]);
            }
        });
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
