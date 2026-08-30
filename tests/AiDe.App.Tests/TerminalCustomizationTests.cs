using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Terminal;
using AiDe.Core.Workbench;
using AvalonDock;
using AvalonDock.Layout;

namespace AiDe.App.Tests;

/// <summary>
/// Controls for the per-session terminal customization (spec-terminal-sessions): the colour-scheme
/// model, the scheme→palette mapping, and the rename→tab-caption mechanism. A real
/// <see cref="TerminalSurface"/> spawns a ConPTY/PowerShell, so these test the load-bearing parts
/// without one: the scheme and palette are pure, and the rename path is exercised through the adapter
/// with a fake <see cref="IHasDisplayName"/> content.
/// </summary>
public sealed class TerminalCustomizationTests
{
    [Fact]
    public void ColourSchemePresets_EachHaveSixteenAnsiColours_AndAName()
    {
        Assert.NotEmpty(TerminalColorScheme.Presets);
        foreach (var scheme in TerminalColorScheme.Presets)
        {
            Assert.Equal(16, scheme.Ansi16.Count);
            Assert.False(string.IsNullOrWhiteSpace(scheme.Name));
        }

        Assert.Contains(TerminalColorScheme.Presets, s => s.Name == "Default");
        Assert.Contains(TerminalColorScheme.Presets, s => s.Name == "High-contrast");
    }

    // The scheme constructor must map the ANSI vocabulary from the SCHEME, not the global resources,
    // so two sessions can render with different schemes at once.
    [Fact]
    public void TerminalPalette_FromScheme_ResolvesEveryIndexedColourFromTheScheme()
    {
        var scheme = TerminalColorScheme.HighContrast;
        var palette = new TerminalPalette(scheme);

        for (var i = 0; i < 16; i++)
        {
            Assert.Equal(scheme.Ansi16[i], palette.Resolve(TerminalColor.FromIndex(i), isBackground: false));
        }

        Assert.Equal(scheme.Background, palette.Background);
        Assert.Equal(scheme.Foreground, palette.Foreground);
        Assert.Equal(scheme.Cursor, palette.Cursor);
    }

    // Two schemes must actually differ, or the feature is cosmetic.
    [Fact]
    public void TerminalPalette_DifferentSchemes_ProduceDifferentBackgrounds()
    {
        var a = new TerminalPalette(TerminalColorScheme.Default);
        var b = new TerminalPalette(TerminalColorScheme.HighContrast);
        Assert.NotEqual(a.Background, b.Background);
    }

    // The rename mechanism: a surface's DisplayName becomes the tab caption; without one, the model
    // title is used. This is what makes a rename survive a re-render (reconcile reuses the instance).
    [Fact]
    public void Render_UsesContentDisplayName_ForTabCaption_WhenPresent()
    {
        OnSta(() =>
        {
            var manager = new DockingManager();
            var adapter = new WorkbenchAdapter(
                manager, new LayoutService(),
                s => s.Kind == "terminal" ? new NamedControl("Renamed!") : new Border());
            var window = Offscreen(manager);
            window.Show();
            adapter.Render();

            var doc = manager.Layout!.Descendents().OfType<LayoutDocument>()
                .First(d => d.ContentId == "terminal-1");
            Assert.Equal("Renamed!", doc.Title);
            window.Close();
        });
    }

    [Fact]
    public void Render_FallsBackToModelTitle_WhenContentHasNoDisplayName()
    {
        OnSta(() =>
        {
            var manager = new DockingManager();
            var adapter = new WorkbenchAdapter(
                manager, new LayoutService(),
                s => s.Kind == "terminal" ? new NamedControl(null) : new Border());
            var window = Offscreen(manager);
            window.Show();
            adapter.Render();

            var doc = manager.Layout!.Descendents().OfType<LayoutDocument>()
                .First(d => d.ContentId == "terminal-1");
            Assert.Equal("Terminal — pwsh", doc.Title);
            window.Close();
        });
    }

    [Fact]
    public void CustomizationStore_RoundTripsCustomization_BySurfaceId()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aide-term-{Guid.NewGuid():N}.json");
        try
        {
            new TerminalCustomizationStore(path)
                .Save("terminal#abc", new TerminalCustomization("Build shell", "Warm", "#FF5B9DD9"));

            // A fresh store on the same path is the restart: the customization comes back.
            Assert.True(new TerminalCustomizationStore(path).TryGet("terminal#abc", out var got));
            Assert.Equal("Build shell", got!.Name);
            Assert.Equal("Warm", got.Scheme);
            Assert.Equal("#FF5B9DD9", got.TabColour);
        }
        finally
        {
            if (File.Exists(path)) { File.Delete(path); }
        }
    }

    [Fact]
    public void CustomizationStore_DropsAnEntryResetToDefault()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aide-term-{Guid.NewGuid():N}.json");
        try
        {
            var store = new TerminalCustomizationStore(path);
            store.Save("t1", new TerminalCustomization("Named", "Cool", "#FF000000"));
            store.Save("t1", new TerminalCustomization(null, "Default", null));

            Assert.False(new TerminalCustomizationStore(path).TryGet("t1", out _));
        }
        finally
        {
            if (File.Exists(path)) { File.Delete(path); }
        }
    }

    [Fact]
    public void CustomizationStore_StartsClean_WhenTheFileIsCorrupt()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aide-term-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{ this is not valid json ");
            Assert.False(new TerminalCustomizationStore(path).TryGet("anything", out _));
        }
        finally
        {
            if (File.Exists(path)) { File.Delete(path); }
        }
    }

    private sealed class NamedControl(string? name) : Border, IHasDisplayName
    {
        public string? DisplayName { get; } = name;
    }

    private static Window Offscreen(DockingManager manager) => new()
    {
        Content = manager,
        Width = 900,
        Height = 600,
        WindowStartupLocation = WindowStartupLocation.Manual,
        Left = -10000,
        Top = -10000,
        ShowInTaskbar = false,
        ShowActivated = false,
    };

    private static void OnSta(Action work)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { work(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)), "STA thread did not finish");
        if (failure is not null)
        {
            throw new InvalidOperationException("STA work failed", failure);
        }
    }
}
