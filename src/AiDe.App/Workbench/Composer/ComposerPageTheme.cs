using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace AiDe.App.Workbench.Composer;

/// <summary>
/// The theme the composer page draws with, as CSS custom properties read from the shell's token
/// dictionary — pushed on <c>host.init</c> so the page carries no palette of its own.
/// </summary>
/// <remarks>
/// <para><b>One palette, read where it is declared (INV-0008, Fix C).</b> The page used to carry
/// its own copy — <c>#1E1E1E</c>, <c>#D4D4D4</c>, <c>#7F858A</c> — and its hint measured 4.47:1
/// against a floor of 4.5 that nothing in the shell's dictionary would have produced. Every role
/// below is a token in <c>App.xaml</c>; the page's stylesheet references the property and keeps the
/// dictionary's value as its fallback for the frame before the push arrives.</para>
///
/// <para><b>Additive on the envelope.</b> <c>theme</c> is one more field on <c>host.init</c>; a page
/// that does not read it renders its fallbacks, and a host that does not send it (the composer
/// probe) leaves the page on them. The handshake itself is untouched.</para>
///
/// <para><b>A missing token is omitted, never invented.</b> A role whose brush is not in the
/// dictionary is left out of the push so the page keeps its fallback — the census then measures
/// that fallback — rather than being sent a colour this class made up.</para>
/// </remarks>
public static class ComposerPageTheme
{
    /// <summary>CSS custom property → the <c>App.xaml</c> token it is read from. One row per role the page draws with.</summary>
    public static readonly IReadOnlyList<(string Property, string Token)> Roles =
    [
        ("--surface", "SurfaceBrush"),
        ("--surface-raised", "SurfaceRaisedBrush"),
        ("--surface-sunken", "SurfaceSunkenBrush"),
        ("--text", "TextBrush"),
        ("--text-muted", "TextMutedBrush"),
        ("--text-disabled", "DisabledTextBrush"),
        ("--accent", "AccentBrush"),
        ("--accent-contrast", "AccentContrastBrush"),
        ("--border", "BorderBrush"),
        ("--danger", "DangerBrush"),
        ("--focus", "FocusBrush"),
    ];

    /// <summary>The running application's theme, or an empty set when there is no application (a bare test host).</summary>
    public static IReadOnlyDictionary<string, string> Current() => From(Application.Current?.Resources);

    /// <summary>Every role whose token resolves in <paramref name="resources"/> to a solid colour, as <c>#RRGGBB</c>.</summary>
    public static IReadOnlyDictionary<string, string> From(ResourceDictionary? resources)
    {
        var theme = new Dictionary<string, string>(StringComparer.Ordinal);
        if (resources is null) return theme;

        foreach (var (property, token) in Roles)
        {
            if (resources[token] is SolidColorBrush brush)
            {
                var c = brush.Color;
                theme[property] = string.Create(CultureInfo.InvariantCulture, $"#{c.R:X2}{c.G:X2}{c.B:X2}");
            }
        }

        return theme;
    }
}
