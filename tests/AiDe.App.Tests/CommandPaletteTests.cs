using System.Windows;
using System.Windows.Input;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The palette is the mechanism US-9 names for SC 2.5.7. Without it the command catalog is a list
/// nobody can invoke — the conformance test would pass while the product stayed mouse-only.
/// </summary>
public sealed class CommandPaletteTests
{
    private static T OnSta<T>(Func<T> work) =>
        Sta.Run<T>(work, 60);

    private static T With<T>(Func<CommandPalette, RecordingAnnouncer, ILayoutService, T> assert) => OnSta(() =>
    {
        var service = new LayoutService();
        var announcer = new RecordingAnnouncer();
        var controller = new WorkbenchController(service, announcer)
        {
            FocusedStackId = service.Current.FindStackOf("explore")!.Id,
            FocusedSurfaceId = "explore",
        };
        var palette = new CommandPalette(controller, announcer);
        var window = new Window
        {
            Content = palette.Root,
            Left = -10000,
            Top = -10000,
            ShowInTaskbar = false,
            ShowActivated = false,
        };
        window.Show();
        window.UpdateLayout();
        try { return assert(palette, announcer, service); }
        finally { window.Close(); }
    });

    // The headline: every catalog command is reachable here.
    [Fact]
    public void ThePalette_ListsEveryLayoutCommand()
    {
        var listed = With((palette, _, _) => palette.Visible.Select(c => c.Id).ToList());

        Assert.Equal(WorkbenchCommandCatalog.All.Count, listed.Count);
        foreach (var command in WorkbenchCommandCatalog.All)
        {
            Assert.Contains(command.Id, listed);
        }
    }

    [Fact]
    public void Opening_AnnouncesItselfAndPreselectsTheFirstCommand()
    {
        var (announcement, selected) = With((palette, announcer, _) =>
        {
            palette.Open();
            return (announcer.Last, palette.Results.SelectedIndex);
        });

        Assert.Contains("Command palette", announcement, StringComparison.Ordinal);
        Assert.Equal(0, selected);
    }

    // Selection moves while focus stays in the search box, so the listbox never announces itself —
    // the change has to be announced explicitly or a screen-reader user hears nothing.
    [Fact]
    public void ArrowingThroughResults_AnnouncesEachCommand()
    {
        var announced = With((palette, announcer, _) =>
        {
            palette.Open();
            palette.HandleKey(Key.Down);
            return announcer.Last;
        });

        Assert.Contains(WorkbenchCommandCatalog.All[1].Title, announced, StringComparison.Ordinal);
    }

    [Fact]
    public void Typing_FiltersTheList()
    {
        var ids = With((palette, _, _) =>
        {
            palette.Open();
            palette.SearchBox.Text = "resize";
            return palette.Visible.Select(c => c.Id).ToList();
        });

        Assert.Contains("workbench.resizePane", ids);
        Assert.DoesNotContain("workbench.closeSurface", ids);
    }

    [Fact]
    public void Enter_RunsTheSelectedCommandAndCloses()
    {
        var (locked, open) = With((palette, _, service) =>
        {
            palette.Open();
            palette.SearchBox.Text = "lock";
            palette.HandleKey(Key.Enter);
            return (service.IsLocked, palette.IsOpen);
        });

        Assert.True(locked);
        Assert.False(open);
    }

    [Fact]
    public void Escape_ClosesWithoutRunningAnything()
    {
        var (locked, open, announcement) = With((palette, announcer, service) =>
        {
            palette.Open();
            palette.SearchBox.Text = "lock";
            palette.HandleKey(Key.Escape);
            return (service.IsLocked, palette.IsOpen, announcer.Last);
        });

        Assert.False(locked);
        Assert.False(open);
        Assert.Contains("closed", announcement, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KeysAreIgnoredWhenClosed()
    {
        var handled = With((palette, _, _) => palette.HandleKey(Key.Down));
        Assert.False(handled);
    }

    [Fact]
    public void ArrowingWraps_SoEveryCommandIsReachableByRepeatedPresses()
    {
        var reachedLast = With((palette, _, _) =>
        {
            palette.Open();
            palette.HandleKey(Key.Up);   // wrap backwards from the first item
            return palette.Results.SelectedIndex == palette.Visible.Count - 1;
        });

        Assert.True(reachedLast);
    }

    [Fact]
    public void EveryListedCommand_ActuallyExecutes()
    {
        var unhandled = With((palette, _, service) =>
        {
            var failures = new List<string>();
            foreach (var command in palette.Visible)
            {
                var fresh = new LayoutService();
                var announcer = new RecordingAnnouncer();
                var controller = new WorkbenchController(fresh, announcer)
                {
                    FocusedStackId = fresh.Current.FindStackOf("explore")!.Id,
                    FocusedSurfaceId = "explore",
                };
                if (!controller.Execute(command.Id))
                {
                    failures.Add(command.Id);
                }
            }

            return failures;
        });

        Assert.True(unhandled.Count == 0,
            "palette lists commands that do nothing: " + string.Join(", ", unhandled));
    }
}
