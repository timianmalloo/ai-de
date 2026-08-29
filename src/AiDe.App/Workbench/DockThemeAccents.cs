using System.Windows;
using System.Windows.Media;
using AvalonDock;

namespace AiDe.App.Workbench;

/// <summary>
/// Retokenises the AvalonDock VS2013 dark theme to the app palette — the VS-blue accent to our
/// accent, and the theme's background/border grays to our surface/border tokens.
/// </summary>
/// <remarks>
/// <para><b>By value, not by key name.</b> The theme's accent is the VS-blue family
/// (<c>#007ACC</c> and its hover/pressed tints), spread across ~30 component resource keys
/// (<c>DocumentWellTabSelectedActiveBackground</c>, <c>ToolWindowCaptionActiveBackground</c>,
/// <c>ControlAccentBrushKey</c>, …). Rather than name each key — which risks missing one and
/// leaving a stray blue — this recolours every themed brush whose <em>colour</em> is in that
/// family. The key set was established by enumerating a themed <see cref="DockingManager"/> at
/// runtime, not guessed (E15).</para>
///
/// <para><b>No template surgery.</b> The overrides are written as DIRECT entries into the manager's
/// resources, which take precedence over the same keys in its merged theme dictionaries, so the
/// tab and caption templates' <c>DynamicResource</c> lookups resolve to ours. Document-tab corners
/// stay square — that is the IDE convention (VS / VS Code / JetBrains) and rounding them would need
/// the fragile template surgery this deliberately avoids. See
/// <c>docs/notes/avalondock-tab-styling-decision.md</c>.</para>
/// </remarks>
public static class DockThemeAccents
{
    // VS accent-blue family -> palette equivalents (DESIGN.md: accent #5B9DD9, focus #8FC0EA), plus
    // the theme's dark background/border grays -> our surface/border tokens, so the docking chrome
    // reads as one palette AND the rounded island cards (SurfaceRaised #1A1F26) sit over darker gaps
    // (surface #12151A / sunken #0D1014) and so read as RAISED. Text grays are deliberately left
    // alone. Every value was established by enumerating a themed manager at runtime, not guessed.
    private static readonly IReadOnlyDictionary<uint, Color> Map = new Dictionary<uint, Color>
    {
        // accent family
        [0xFF007ACC] = Rgb("#5B9DD9"), // primary accent: selected tab / active caption / control accent
        [0xFF1C97EA] = Rgb("#7DB4E3"), // hover
        [0xFF0E6198] = Rgb("#3E7AB0"), // pressed / darker
        [0xFF52B0EF] = Rgb("#8FC0EA"), // light hover
        [0xFF0097FB] = Rgb("#8FC0EA"), // accent text
        [0xFF55AAFF] = Rgb("#8FC0EA"), // accent text hover
        [0xFF59A8DE] = Rgb("#7DB4E3"), // caption grip
        // surface / border grays -> our dark palette
        [0xFF2D2D30] = Rgb("#12151A"), // dominant chrome background -> surface
        [0xFF252526] = Rgb("#0D1014"), // document well background -> sunken
        [0xFF1B1B1C] = Rgb("#0D1014"), // darkest background -> sunken
        [0xFF3F3F46] = Rgb("#2A313B"), // divider / border
        [0xFF3E3E40] = Rgb("#2A313B"), // divider / border
        [0xFF393939] = Rgb("#2A313B"), // divider / border
        [0xFF46464A] = Rgb("#2A313B"), // divider / border
    };

    /// <summary>
    /// Overrides the theme's accent brushes on <paramref name="manager"/> and returns how many were
    /// retokenised. Safe to call once, after the theme is applied.
    /// </summary>
    public static int Retokenise(DockingManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        var overrides = new List<KeyValuePair<object, Brush>>();
        Collect(manager.Resources, overrides, new HashSet<ResourceDictionary>());

        foreach (var pair in overrides)
        {
            manager.Resources[pair.Key] = pair.Value;
        }

        return overrides.Count;
    }

    private static void Collect(
        ResourceDictionary dictionary,
        List<KeyValuePair<object, Brush>> overrides,
        HashSet<ResourceDictionary> visited)
    {
        if (!visited.Add(dictionary))
        {
            return;
        }

        foreach (var key in dictionary.Keys)
        {
            object? value;
            try { value = dictionary[key]; }
            catch { continue; }

            if (value is SolidColorBrush brush)
            {
                var c = brush.Color;
                var argb = ((uint)c.A << 24) | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;
                if (Map.TryGetValue(argb, out var mapped))
                {
                    var replacement = new SolidColorBrush(mapped);
                    replacement.Freeze();
                    overrides.Add(new KeyValuePair<object, Brush>(key, replacement));
                }
            }
        }

        foreach (var merged in dictionary.MergedDictionaries)
        {
            Collect(merged, overrides, visited);
        }
    }

    private static Color Rgb(string hex) => (Color)ColorConverter.ConvertFromString(hex)!;
}
