using System.Windows;
using System.Windows.Media;
using AiDe.App.Workbench;
using AvalonDock;

namespace AiDe.App.Tests;

/// <summary>
/// The AvalonDock accent is retokenised from VS blue to the app palette — proven, not assumed.
/// </summary>
/// <remarks>
/// Guards the facelift's docking-chrome accent: the VS2013 dark theme ships <c>#007ACC</c> on the
/// selected document tab and active tool-window caption, and the shell must show the palette accent
/// <c>#5B9DD9</c> instead. Asserts through the real key the tab template binds to, so a theme
/// upgrade that renames or drops the key fails here rather than shipping a stray blue.
/// </remarks>
public sealed class DockThemeAccentsTests
{
    private static readonly Color Accent = (Color)ColorConverter.ConvertFromString("#5B9DD9")!;
    private static readonly Color VsBlue = (Color)ColorConverter.ConvertFromString("#007ACC")!;

    private static T OnSta<T>(Func<T> work)
    {
        T result = default!;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { result = work(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) throw failure;
        return result;
    }

    [Fact]
    public void Retokenise_TurnsTheSelectedTabAccent_FromVsBlue_ToPalette() => OnSta(() =>
    {
        var manager = new DockingManager { Theme = new AvalonDock.Themes.Vs2013DarkTheme() };
        manager.Measure(new Size(800, 600));

        // The key the selected-document-tab background binds to, found before the override so the
        // test breaks if the theme stops shipping it.
        var selectedTabKey = FindKey(manager.Resources, "DocumentWellTabSelectedActiveBackground");
        Assert.NotNull(selectedTabKey);
        Assert.Equal(VsBlue, ((SolidColorBrush)manager.Resources[selectedTabKey!]).Color);

        var count = DockThemeAccents.Retokenise(manager);
        Assert.True(count > 0, "no accent brushes were retokenised");

        // After: the same key resolves to the palette accent (direct override beats the merged theme).
        Assert.Equal(Accent, ((SolidColorBrush)manager.Resources[selectedTabKey!]).Color);
        return true;
    });

    private static object? FindKey(ResourceDictionary dictionary, string idFragment)
    {
        var visited = new HashSet<ResourceDictionary>();
        object? Walk(ResourceDictionary d)
        {
            if (!visited.Add(d)) return null;
            foreach (var key in d.Keys)
            {
                if (key?.ToString()?.Contains(idFragment, StringComparison.Ordinal) == true)
                {
                    object? v; try { v = d[key]; } catch { continue; }
                    if (v is SolidColorBrush) return key;
                }
            }
            foreach (var md in d.MergedDictionaries)
            {
                var found = Walk(md);
                if (found is not null) return found;
            }
            return null;
        }
        return Walk(dictionary);
    }
}
