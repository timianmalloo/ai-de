using System.Windows;
using System.Windows.Media;
using AiDe.Core.Terminal;

namespace AiDe.App.Workbench;

/// <summary>
/// Resolves a <see cref="TerminalColor"/> to something the renderer can draw with.
/// </summary>
/// <remarks>
/// <para>This is the one place the terminal model meets the theme, and the split is deliberate: the
/// screen model records what the <i>wire</i> said ("palette index 4"), and only here is that turned
/// into a shade. That keeps the whole of <c>AiDe.Core</c> free of a rendering framework and lets the
/// look change without touching a parser.</para>
///
/// <para><b>The ANSI sixteen are a vocabulary, not a preference.</b> Programs address them by index
/// and expect red to mean red, so what a theme may choose is the shade — never the meaning. The
/// values live in <c>App.xaml</c> with every other token; the fallbacks below exist only for the
/// case where this type is used outside a running application, which is exactly what a unit test
/// does.</para>
///
/// <para><b>The 240 above the sixteen are computed, not authored.</b> Indexes 16–231 are the
/// standard 6×6×6 cube and 232–255 the grey ramp, both defined by the protocol rather than by us —
/// so writing them out as tokens would be transcribing a specification and inviting a typo nobody
/// would ever notice.</para>
/// </remarks>
public sealed class TerminalPalette
{
    private static readonly Color[] Fallback16 =
    [
        Color.FromRgb(0x1A, 0x1F, 0x26), Color.FromRgb(0xE0, 0x7A, 0x6F),
        Color.FromRgb(0x5F, 0xB9, 0x8F), Color.FromRgb(0xD8, 0xA6, 0x50),
        Color.FromRgb(0x5B, 0x9D, 0xD9), Color.FromRgb(0xB0, 0x8A, 0xD0),
        Color.FromRgb(0x5F, 0xB2, 0xB9), Color.FromRgb(0xC3, 0xCB, 0xD6),
        Color.FromRgb(0x5A, 0x64, 0x72), Color.FromRgb(0xF0, 0x9A, 0x90),
        Color.FromRgb(0x84, 0xD2, 0xAC), Color.FromRgb(0xEF, 0xC2, 0x75),
        Color.FromRgb(0x8F, 0xC0, 0xEA), Color.FromRgb(0xCB, 0xA9, 0xE4),
        Color.FromRgb(0x88, 0xCD, 0xD3), Color.FromRgb(0xF2, 0xF6, 0xFA),
    ];

    private readonly Color[] _palette = new Color[256];

    public TerminalPalette()
    {
        for (var i = 0; i < 16; i++)
        {
            _palette[i] = Resource($"TerminalAnsi{i}", Fallback16[i]);
        }

        BuildCube();
        BuildGreyRamp();

        Background = Resource("TerminalBackground", Color.FromRgb(0x0D, 0x10, 0x14));
        Foreground = Resource("TerminalForeground", Color.FromRgb(0xE4, 0xE9, 0xEF));
        Cursor = Resource("TerminalCursor", Color.FromRgb(0x5B, 0x9D, 0xD9));
    }

    /// <summary>
    /// A palette from an explicit per-session <see cref="TerminalColorScheme"/> rather than the global
    /// resources, so two terminals can render with different schemes at once.
    /// </summary>
    public TerminalPalette(TerminalColorScheme scheme)
    {
        for (var i = 0; i < 16; i++)
        {
            _palette[i] = scheme.Ansi16[i];
        }

        BuildCube();
        BuildGreyRamp();

        Background = scheme.Background;
        Foreground = scheme.Foreground;
        Cursor = scheme.Cursor;
    }

    public Color Background { get; }

    public Color Foreground { get; }

    public Color Cursor { get; }

    /// <summary>The colour to draw <paramref name="color"/> in, given which role it plays.</summary>
    public Color Resolve(TerminalColor color, bool isBackground) => color.Kind switch
    {
        TerminalColorKind.Indexed => _palette[Math.Clamp(color.Index, 0, 255)],
        TerminalColorKind.Rgb => Color.FromRgb(color.R, color.G, color.B),
        _ => isBackground ? Background : Foreground,
    };

    /// <summary>Indexes 16–231: a 6×6×6 cube on the standard non-linear ramp.</summary>
    private void BuildCube()
    {
        ReadOnlySpan<byte> steps = [0, 95, 135, 175, 215, 255];

        for (var r = 0; r < 6; r++)
        {
            for (var g = 0; g < 6; g++)
            {
                for (var b = 0; b < 6; b++)
                {
                    _palette[16 + (r * 36) + (g * 6) + b] =
                        Color.FromRgb(steps[r], steps[g], steps[b]);
                }
            }
        }
    }

    /// <summary>Indexes 232–255: 24 greys from near-black to near-white.</summary>
    private void BuildGreyRamp()
    {
        for (var i = 0; i < 24; i++)
        {
            var level = (byte)(8 + (i * 10));
            _palette[232 + i] = Color.FromRgb(level, level, level);
        }
    }

    /// <summary>
    /// Reads a colour token, falling back when there is no running application.
    /// </summary>
    /// <remarks>
    /// The fallback is not a second source of truth for the palette — it is the same values, present
    /// so a unit test can construct this type without standing up an <see cref="Application"/>. If
    /// they ever disagree, the token wins, and <c>PaletteMatchesTheTokens</c> is what says so.
    /// </remarks>
    private static Color Resource(string key, Color fallback)
    {
        if (Application.Current?.TryFindResource(key) is Color color)
        {
            return color;
        }

        return fallback;
    }
}
