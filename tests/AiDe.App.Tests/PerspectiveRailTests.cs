using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The rail's activation model (spec §C4 as amended; DESIGN.md PS-R2): a single-selection group
/// with MANUAL activation. Down moves focus and switches nothing; Enter (and Space, and a click)
/// activate; the selection follows the presenter, never the keyboard; the selected state is the
/// real UIA one and the accessible name is constant. Headless — the control is the model.
/// </summary>
public sealed class PerspectiveRailTests
{
    private static (PerspectiveRail Rail, Window Window) Shown()
    {
        var rail = new PerspectiveRail();
        var window = new Window
        {
            Content = rail,
            Width = 200, Height = 300, WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false,
        };
        window.Show();
        window.UpdateLayout();
        return (rail, window);
    }

    private static KeyEventArgs Press(UIElement target, Key key)
    {
        var args = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(target)!, 0, key)
        {
            RoutedEvent = Keyboard.PreviewKeyDownEvent,
        };
        target.RaiseEvent(args);
        return args;
    }

    // US-C1 — the rail has exactly the three destinations, in order, each with the constant
    // accessible name "<Title> perspective" (no state suffix), and Coding selected initially.
    [Fact]
    public void TheRail_HasThreeDestinationsInOrder_NamedConstantly_WithCodingSelected()
    {
        Sta.Run(() =>
        {
            var (rail, window) = Shown();
            try
            {
                var items = rail.Destinations.ToList();
                Assert.Equal(3, items.Count);
                Assert.Equal(
                    ["Coding perspective", "Explore perspective", "Architecture perspective"],
                    items.Select(i => AutomationProperties.GetName(i)).ToList());
                Assert.True(items[0].IsSelected);
                Assert.Equal(1, items.Count(i => i.IsSelected));
                Assert.All(items, i => Assert.Equal(44, i.Height));
                return 0;
            }
            finally { window.Close(); }
        }, 30);
    }

    // PS-R2 — Down moves focus only: nothing is activated, the selection is unchanged. Enter
    // activates the focused destination. A repeated Down wraps.
    [Fact]
    public void Down_MovesFocusWithoutActivating_AndEnterActivatesTheFocusedDestination()
    {
        Sta.Run(() =>
        {
            var (rail, window) = Shown();
            try
            {
                var requested = new List<Perspective>();
                rail.ActivationRequested += (_, p) => requested.Add(p);
                var items = rail.Destinations.ToList();
                items[0].Focus();
                Assert.True(items[0].IsKeyboardFocused);

                var down = Press(items[0], Key.Down);
                Assert.True(down.Handled);
                Assert.True(items[1].IsKeyboardFocused);
                Assert.Empty(requested);                       // Down switched nothing
                Assert.True(items[0].IsSelected);              // and the selection did not move
                Assert.False(items[1].IsSelected);

                Press(items[1], Key.Down);
                Press(items[2], Key.Down);                     // wraps
                Assert.True(items[0].IsKeyboardFocused);
                Press(items[0], Key.Up);
                Assert.True(items[2].IsKeyboardFocused);
                Assert.Empty(requested);

                var enter = Press(items[2], Key.Enter);
                Assert.True(enter.Handled);
                Assert.Equal([PerspectiveSet.Architecture], requested);
                Assert.True(items[0].IsSelected);              // the rail follows the presenter, never the key

                Press(items[2], Key.Space);
                Assert.Equal(2, requested.Count);
                return 0;
            }
            finally { window.Close(); }
        }, 30);
    }

    // A click activates — and the list never selects on it: mouse-down on a destination raises the
    // activation request and leaves the selection where the presenter put it.
    [Fact]
    public void MouseDown_ActivatesTheDestination_WithoutSelectingIt()
    {
        Sta.Run(() =>
        {
            var (rail, window) = Shown();
            try
            {
                var requested = new List<Perspective>();
                rail.ActivationRequested += (_, p) => requested.Add(p);
                var items = rail.Destinations.ToList();

                var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = Mouse.PreviewMouseDownEvent,
                    Source = items[2],
                };
                items[2].RaiseEvent(args);

                Assert.True(args.Handled);
                Assert.Equal([PerspectiveSet.Architecture], requested);
                Assert.True(items[0].IsSelected);
                Assert.False(items[2].IsSelected);
                return 0;
            }
            finally { window.Close(); }
        }, 30);
    }

    // The selection has one writer: Reflect, from the presenter's change event; exactly one item is
    // selected after it, and a failure state set on a destination clears when it is reflected.
    [Fact]
    public void Reflect_SelectsExactlyOneDestination_AndClearsItsFailure()
    {
        Sta.Run(() =>
        {
            var (rail, window) = Shown();
            try
            {
                rail.ShowFailure(PerspectiveSet.Explore, "the graph substrate is not ready");
                var explore = rail.ItemFor(PerspectiveSet.Explore)!;
                Assert.True(explore.HasFailure);
                Assert.Equal("Couldn't open Explore: the graph substrate is not ready. Activate to try again.", AutomationProperties.GetItemStatus(explore));

                rail.Reflect(PerspectiveSet.Explore);

                var items = rail.Destinations.ToList();
                Assert.True(items[1].IsSelected);
                Assert.Equal(1, items.Count(i => i.IsSelected));
                Assert.False(explore.HasFailure);
                Assert.Equal("Explore perspective", AutomationProperties.GetName(explore));   // never suffixed
                return 0;
            }
            finally { window.Close(); }
        }, 30);
    }

    // SC 4.1.2 — to UIA the rail is a tab list whose items carry the selection pattern, and
    // IsSelected there is the real selection.
    [Fact]
    public void ToUia_TheRailIsATabList_AndItsItemsExposeTheRealSelection()
    {
        Sta.Run(() =>
        {
            var (rail, window) = Shown();
            try
            {
                var peer = UIElementAutomationPeer.CreatePeerForElement(rail)!;
                Assert.Equal(AutomationControlType.Tab, peer.GetAutomationControlType());

                var children = peer.GetChildren();
                Assert.Equal(3, children.Count);
                Assert.All(children, c => Assert.Equal(AutomationControlType.TabItem, c.GetAutomationControlType()));

                var selection = children.Select(c => (ISelectionItemProvider)c.GetPattern(PatternInterface.SelectionItem)!).ToList();
                Assert.Equal([true, false, false], selection.Select(s => s.IsSelected).ToList());

                rail.Reflect(PerspectiveSet.Architecture);
                Assert.Equal([false, false, true], selection.Select(s => s.IsSelected).ToList());
                return 0;
            }
            finally { window.Close(); }
        }, 30);
    }

    // The UX & Accessibility reviewer's blocker (SC 2.1.1 / 2.4.3): the rail is REACHABLE by Tab —
    // from the New session button, one Tab lands on the selected destination (WAI-ARIA tabs enter
    // on the active tab), and Tab again leaves the group. A container that is a one-stop group
    // (TabNavigation=Once on the rail's parent) never lets Tab in; MainWindow.xaml must not set it.
    [Fact]
    public void Tab_EntersTheRailOnTheSelectedDestination_AndLeavesIt()
    {
        Sta.Run(() =>
        {
            var button = new Button { Content = "New session" };
            var rail = new PerspectiveRail();
            var after = new TextBox();
            var panel = new StackPanel();
            KeyboardNavigation.SetDirectionalNavigation(panel, KeyboardNavigationMode.Cycle);   // as MainWindow.xaml's ActivityRail
            panel.Children.Add(button);
            panel.Children.Add(rail);
            var root = new StackPanel();
            root.Children.Add(panel);
            root.Children.Add(after);
            var window = new Window
            {
                Content = root,
                Width = 300, Height = 400, WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false,
            };
            window.Show();
            window.UpdateLayout();
            try
            {
                rail.Reflect(PerspectiveSet.Architecture);
                Assert.True(button.Focus());

                Assert.True(button.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
                var entered = Assert.IsType<PerspectiveRailItem>(Keyboard.FocusedElement);
                Assert.Same(PerspectiveSet.Architecture, entered.Row);                 // enters on the selected destination

                Assert.True(entered.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
                Assert.Same(after, Keyboard.FocusedElement);                          // one stop, then out

                // The UX & Accessibility reviewer's second-round blocker (NEW-1): after arrowing to a
                // destination that is NOT the selected one and leaving, the group must still be
                // enterable — from either direction — or the rail is lost to the keyboard after the
                // canonical explore-and-leave flow (SC 2.1.1).
                entered.Focus();
                Press(entered, Key.Up);                                              // focus a non-selected destination
                var explored = Assert.IsType<PerspectiveRailItem>(Keyboard.FocusedElement);
                Assert.NotSame(entered, explored);
                Assert.True(explored.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
                Assert.Same(after, Keyboard.FocusedElement);

                Assert.True(after.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous)));
                Assert.IsType<PerspectiveRailItem>(Keyboard.FocusedElement);         // Shift+Tab re-enters the rail
                Assert.True(((PerspectiveRailItem)Keyboard.FocusedElement!).MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous)));
                Assert.Same(button, Keyboard.FocusedElement);

                Assert.True(button.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
                Assert.IsType<PerspectiveRailItem>(Keyboard.FocusedElement);         // and Tab re-enters it
                return 0;
            }
            finally { window.Close(); }
        }, 30);
    }

    [Fact]
    public void TheRailsContainer_IsNotAOneStopGroup_InTheWindowsXaml()
    {
        var xaml = File.ReadAllText(Path.Combine(RepoRoot(), "src", "AiDe.App", "MainWindow.xaml"));
        var rail = xaml[xaml.IndexOf("x:Name=\"ActivityRail\"", StringComparison.Ordinal)..];
        rail = rail[..rail.IndexOf('>')];
        Assert.DoesNotContain("TabNavigation=\"Once\"", rail, StringComparison.Ordinal);
    }

    // Escape reaches the perspective level on the BUBBLING route only — the canvas, the reader and
    // any text box inside Explore consume theirs first — never on the tunnelling one, where the
    // window would see the key before the control that owns it.
    [Fact]
    public void TheWindowsEscapeHandler_IsOnTheBubblingRoute()
    {
        var window = File.ReadAllText(Path.Combine(RepoRoot(), "src", "AiDe.App", "MainWindow.xaml.cs"));
        var at = window.IndexOf("_perspectives.Escape()", StringComparison.Ordinal);
        Assert.True(at > 0, "the window has no Escape-from-Explore handler");
        var handler = window[Math.Max(0, at - 700)..at];
        Assert.Contains("KeyDown += (_, e) =>", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviewKeyDown += (_, e) =>", handler, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("AiDe.sln was not found above the test directory");
    }

    // PS-R5 — the selection has ONE writer. Every other path a ListBox selects on — a page key, a
    // right-click, typeahead, the UIA SelectionItem pattern's Select(), a programmatic SelectedItem
    // — leaves the selection where the presenter put it, and the two that mean "go there" raise an
    // activation request instead.
    [Fact]
    public void NoOtherWriter_MovesTheSelection_AndSelectRequestsActivation()
    {
        Sta.Run(() =>
        {
            var (rail, window) = Shown();
            try
            {
                var requested = new List<Perspective>();
                rail.ActivationRequested += (_, p) => requested.Add(p);
                var items = rail.Destinations.ToList();
                items[0].Focus();

                // PageDown: focus moves, the selection does not, nothing is requested.
                Assert.True(Press(items[0], Key.PageDown).Handled);
                Assert.True(items[2].IsKeyboardFocused);
                Assert.True(items[0].IsSelected);
                Assert.Empty(requested);

                // Right-click: nothing.
                var right = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
                {
                    RoutedEvent = Mouse.PreviewMouseDownEvent,
                    Source = items[1],
                };
                items[1].RaiseEvent(right);
                Assert.True(right.Handled);
                Assert.True(items[0].IsSelected);
                Assert.Empty(requested);

                // Typeahead is off.
                Assert.False(rail.IsTextSearchEnabled);

                // A programmatic selection is reverted and becomes an activation request.
                rail.SelectedItem = PerspectiveSet.Explore;
                Assert.True(items[0].IsSelected);
                Assert.False(items[1].IsSelected);
                Assert.Equal([PerspectiveSet.Explore], requested);

                // The UIA pattern's Select() is an activation request, never a write.
                var peer = UIElementAutomationPeer.CreatePeerForElement(rail)!;
                var third = (ISelectionItemProvider)peer.GetChildren()[2].GetPattern(PatternInterface.SelectionItem)!;
                third.Select();
                Assert.True(items[0].IsSelected);
                Assert.Equal([PerspectiveSet.Explore, PerspectiveSet.Architecture], requested);
                Assert.True(((ISelectionProvider)peer.GetPattern(PatternInterface.Selection)!).IsSelectionRequired);
                return 0;
            }
            finally { window.Close(); }
        }, 30);
    }

    // PS-R4 — every destination's glyph resolves from the registry the window can see: the two
    // geometries kept in the window's resources this horizon and the application's IconExplore
    // alike. RED before the fix: Application.TryFindResource never searches a Window's dictionary,
    // so Coding and Architecture rendered as blank pills (the WPF lens's finding).
    [Fact]
    public void EveryDestinationsGlyph_ResolvesFromTheWindowsOrTheApplicationsRegistry()
    {
        Sta.Run(() =>
        {
            var rail = new PerspectiveRail();
            var window = new Window
            {
                Content = rail,
                Width = 200, Height = 300, WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false,
            };
            foreach (var title in new[] { "Coding", "Explore", "Architecture" })
            {
                window.Resources[$"Icon{title}"] = Geometry.Parse("M0 0L10 10");   // the window's registry, as MainWindow.xaml carries it
            }

            window.Show();
            window.UpdateLayout();
            try
            {
                Assert.All(rail.Destinations, item => Assert.NotNull(item.Glyph));
                return 0;
            }
            finally { window.Close(); }
        }, 30);
    }

    // PS-R4 / US-C10 b3 — the tooltip's keystroke is the bound gesture's display string, never a
    // typed chord; every destination's tooltip carries its bound gesture.
    [Fact]
    public void TheTooltip_RendersTheBoundGesturesDisplayString()
    {
        Assert.Equal("Coding — Ctrl+1", PerspectiveRailItem.TooltipFor(PerspectiveSet.Coding));
        Assert.Equal("Explore — graph & reader — Ctrl+2", PerspectiveRailItem.TooltipFor(PerspectiveSet.Explore));
        Assert.Equal("Architecture — Ctrl+3", PerspectiveRailItem.TooltipFor(PerspectiveSet.Architecture));
    }

    // spec §C6 — the window title carries the active perspective's name after a switch.
    [Fact]
    public void TheWindowTitle_NamesTheWorkspaceAndTheActivePerspective()
    {
        Assert.Equal("Coding — AI-DE", MainWindow.TitleFor(null, PerspectiveSet.Coding));
        Assert.Equal("ai-de — Architecture — AI-DE", MainWindow.TitleFor("ai-de", PerspectiveSet.Architecture));
    }
}
