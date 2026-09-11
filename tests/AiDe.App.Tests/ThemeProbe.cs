using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace AiDe.App.Tests;

/// <summary>
/// Reads the application's real theme dictionary and measures what it actually resolves to.
/// </summary>
/// <remarks>
/// <para><b>The real file, not a copy of it.</b> <c>MenuTemplateTests</c> mirrors App.xaml's menu
/// block into a string literal, and a mirror is a second palette: the day one changes, the check
/// goes on passing against the other. This loads <c>src/AiDe.App/App.xaml</c> itself, rewrapping the
/// <c>Application.Resources</c> element as a <c>ResourceDictionary</c> so loose XAML can parse it —
/// nothing about the brushes, templates or implicit styles is restated here.</para>
///
/// <para><b>Why a real window.</b> A style setter is not a value until the property system has
/// applied it, and a template trigger is not applied until the template is. Constructing a control
/// and reading <c>Foreground</c> without showing it measures the type's default, which is exactly
/// the reading that made "the palette is fine" true and the product illegible.</para>
///
/// <para><b>TC6.</b> Contrast is computed from the resolved pairing. No ratio in this repository's
/// source or documentation is a number somebody remembered.</para>
/// </remarks>
internal static class ThemeProbe
{
    /// <summary>The WCAG 2.x relative luminance of an opaque colour.</summary>
    internal static double Luminance(Color c)
    {
        static double Channel(byte v)
        {
            var s = v / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(c.R)) + (0.7152 * Channel(c.G)) + (0.0722 * Channel(c.B));
    }

    /// <summary>The WCAG 2.x contrast ratio between two opaque colours.</summary>
    internal static double Contrast(Color foreground, Color background)
    {
        var a = Luminance(foreground);
        var b = Luminance(background);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    /// <summary>Composites <paramref name="over"/> (with its own alpha) onto an opaque ground.</summary>
    internal static Color Composite(Color over, Color ground, double extraOpacity = 1.0)
    {
        var alpha = (over.A / 255.0) * extraOpacity;
        return Color.FromRgb(
            (byte)Math.Round((over.R * alpha) + (ground.R * (1 - alpha))),
            (byte)Math.Round((over.G * alpha) + (ground.G * (1 - alpha))),
            (byte)Math.Round((over.B * alpha) + (ground.B * (1 - alpha))));
    }

    /// <summary>
    /// The application's own resource dictionary, parsed from <c>src/AiDe.App/App.xaml</c>.
    /// </summary>
    internal static ResourceDictionary AppTheme()
    {
        var path = Path.Combine(RepositoryRoot(), "src", "AiDe.App", "App.xaml");
        Assert.True(File.Exists(path), $"App.xaml was not found at {path}");

        var text = File.ReadAllText(path);

        const string Open = "<Application.Resources>";
        const string Close = "</Application.Resources>";

        var start = text.IndexOf(Open, StringComparison.Ordinal);
        var end = text.IndexOf(Close, StringComparison.Ordinal);

        Assert.True(
            start >= 0 && end > start,
            "App.xaml no longer has a single <Application.Resources> element, so this probe is "
            + "reading a document whose shape it does not understand — which would pass by scanning "
            + "nothing (DC-016).");

        var inner = text[(start + Open.Length)..end];

        var xaml =
            "<ResourceDictionary "
            + "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" "
            + "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" "
            + "xmlns:avalonDock=\"clr-namespace:AvalonDock.Controls;assembly=AvalonDock\">"
            + inner
            + "</ResourceDictionary>";

        return (ResourceDictionary)XamlReader.Parse(xaml);
    }

    /// <summary>A named token brush from the real dictionary.</summary>
    internal static Color Token(ResourceDictionary theme, string key)
    {
        var brush = theme[key] as SolidColorBrush;
        Assert.True(brush is not null,
            $"the theme has no SolidColorBrush named '{key}'. A resource reference to a missing key "
            + "is a silent no-op (TC3), which is why this is an assertion and not a null check.");
        return brush!.Color;
    }

    /// <summary>
    /// Runs a body against a real, shown, laid-out window carrying the real theme.
    /// </summary>
    /// <param name="ground">The window's ground — the pane colour the site actually sits on.</param>
    internal static void OnThemedWindow(string ground, Func<ResourceDictionary, FrameworkElement> build, Action<ResourceDictionary, FrameworkElement> measure)
    {
        Sta.Run(() =>
        {
            var theme = AppTheme();
            var window = new Window
            {
                Resources = theme,
                Width = 900,
                Height = 700,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                ShowActivated = false,
                Background = (Brush)theme[ground],
            };

            var subject = build(theme);
            window.Content = subject;

            try
            {
                window.Show();
                window.UpdateLayout();
                subject.UpdateLayout();
                measure(theme, subject);
            }
            finally
            {
                window.Close();
            }
        });
    }

    /// <summary>
    /// The opaque colour actually behind an element: the first ancestor that paints one, with any
    /// translucent grounds between composited onto it.
    /// </summary>
    /// <remarks>
    /// This is the half the previous fix skipped. Theming an ink without establishing what is behind
    /// it is how <c>{colors.text}</c> ended up on the platform's white at 1.22:1 (TC2).
    /// </remarks>
    internal static Color EffectiveBackground(DependencyObject element)
    {
        var stack = new List<Color>();
        var current = element;

        while (current is not null)
        {
            var brush = current switch
            {
                System.Windows.Controls.Control c => c.Background,
                System.Windows.Controls.Panel p => p.Background,
                System.Windows.Controls.Border b => b.Background,
                System.Windows.Controls.TextBlock t => t.Background,
                _ => null,
            };

            if (brush is SolidColorBrush solid && solid.Color.A > 0)
            {
                stack.Add(solid.Color);

                if (solid.Color.A == 255)
                {
                    break;
                }
            }

            current = (current is Visual or System.Windows.Media.Media3D.Visual3D
                          ? VisualTreeHelper.GetParent(current)
                          : null)
                      ?? LogicalTreeHelper.GetParent(current);
        }

        Assert.True(stack.Count > 0 && stack[^1].A == 255,
            "no opaque ground was found above this element. An ink with no established ground is an "
            + "unmeasured pairing, which is the defect class itself (TC2).");

        var ground = stack[^1];

        for (var i = stack.Count - 2; i >= 0; i--)
        {
            ground = Composite(stack[i], ground);
        }

        return ground;
    }

    /// <summary>The element's resolved ink, as the property system reports it after layout.</summary>
    internal static Color Ink(DependencyObject element) => element switch
    {
        System.Windows.Controls.TextBlock t => ((SolidColorBrush)t.Foreground).Color,
        System.Windows.Controls.Control c => ((SolidColorBrush)c.Foreground).Color,
        _ => throw new InvalidOperationException($"{element.GetType().Name} carries no Foreground"),
    };

    /// <summary>Finds the first descendant of a type, by visual tree, that satisfies a predicate.</summary>
    internal static T? FirstDescendant<T>(DependencyObject root, Func<T, bool>? where = null)
        where T : DependencyObject
    {
        if (root is T typed && (where is null || where(typed)))
        {
            return typed;
        }

        // Only a Visual has a visual tree; a ColumnDefinition reached through the logical tree is a
        // DependencyObject and VisualTreeHelper throws on it.
        if (root is Visual or System.Windows.Media.Media3D.Visual3D)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var found = FirstDescendant(VisualTreeHelper.GetChild(root, i), where);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is DependencyObject dependency)
            {
                var found = FirstDescendant(dependency, where);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    /// <summary>One measured pairing, in the shape the review's table used.</summary>
    internal sealed record Pairing(
        int Number, string Site, string What, double Floor, Color Foreground, Color Background)
    {
        public double Ratio => Contrast(Foreground, Background);

        public bool Clears => Ratio >= Floor;

        public string Row => string.Format(
            CultureInfo.InvariantCulture,
            "| {0} | {1:0.00} | {2:0.0} | `#{3:X2}{4:X2}{5:X2}` | `#{6:X2}{7:X2}{8:X2}` | {9} | {10} |",
            Number, Ratio, Floor,
            Foreground.R, Foreground.G, Foreground.B,
            Background.R, Background.G, Background.B,
            Site, Clears ? "**clears**" : "**FAILS**");
    }

    /// <summary>Writes the measured table where a human can read it without a failing assertion.</summary>
    internal static string WriteReport(string name, IEnumerable<Pairing> pairings)
    {
        var report = new StringBuilder();
        report.AppendLine(CultureInfo.InvariantCulture, $"# {name}");
        report.AppendLine();
        report.AppendLine("| # | Ratio | Floor | Foreground | Background | Site | Verdict |");
        report.AppendLine("|---|---:|---:|---|---|---|---|");

        foreach (var pairing in pairings)
        {
            report.AppendLine(pairing.Row);
        }

        var path = Path.Combine(Path.GetTempPath(), "aide-contrast-report.md");
        File.WriteAllText(path, report.ToString());
        return path;
    }

    private static string RepositoryRoot()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);

        while (here is not null && !File.Exists(Path.Combine(here.FullName, "AiDe.sln")))
        {
            here = here.Parent;
        }

        Assert.NotNull(here);
        return here!.FullName;
    }
}
