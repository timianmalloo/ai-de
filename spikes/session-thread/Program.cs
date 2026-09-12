// Spike: the WPF facts the session-thread design would otherwise assert from memory.
//
// docs/design/session-thread-itemscontrol.md (DS-1) designs the thread as a ListBox-derived feed
// with a recycling VirtualizingStackPanel, variable-height turns, an Expander per turn for the
// folded reply, a pinned composer beneath, and UIA exposure as List / ListItem with ItemStatus.
// Each of those is a platform behaviour, not a design choice, so each is measured here rather than
// recalled. No model calls; no repository code; pure WPF.
//
//   dotnet run --project spikes/session-thread -- host    show the window and print MEASURE lines
//   dotnet run --project spikes/session-thread            probe: launch the host, walk its UIA tree,
//                                                         merge both reports, write RESULT-raw.txt
//
// Questions (Q1..Q9) — each answered by a MEASURE or PROBE line, never by prose:
//   Q1 ListBox with Recycling VSP: realized containers at 1 / 5 / 40 / 400 turns (does it virtualize
//      with variable-height items and pixel scrolling?)
//   Q2 The composer's top edge at 1 / 5 / 40 / 400 turns (equal = pinned)
//   Q3 Layout cost 40 vs 400 turns, as a RATIO (DC-107: never an absolute budget)
//   Q4 Follow rule: does a ListBox pinned at its end follow an appended item by itself? Does a
//      reader mid-thread get moved?
//   Q5 Keyboard: the ListBox's own PageDown (moves by a page) vs the feed override (by one turn);
//      Home / End / Up / Down; Tab from the current turn reaches its fold and then leaves the feed;
//      the fold's inner controls in a NON-current turn are not tab stops (roving tab stop)
//   Q6 Automation peers in-process: ListBox -> List, ListBoxItem -> ListItem, PositionInSet /
//      SizeOfSet, ItemStatus, Name; a bare ItemsControl's peer for comparison
//   Q7 Expander: ExpandCollapse pattern available; does its header toggle accept Enter
//      (KeyboardNavigation.AcceptsReturn) as well as Space?
//   Q8 LiveSetting reads back Polite / Assertive; RaiseNotificationEvent does not throw
//   Q9 Out-of-process UIA (what a screen reader's client sees): the List, its realized ListItems,
//      their Name / ItemStatus / PositionInSet / SizeOfSet, the fold's ExpandCollapsePattern
//      (invoked over UIA), the live regions' LiveSetting
//   Q10 (added at the Stage-4 gate) an update to the running turn: replace-by-index vs an
//      INotifyPropertyChanged row — which keeps the container, the focus AND the selection?
//   Q11 the designed fold shape: a non-focusable Expander whose HeaderSite is the one stop
//   Q12 which of the design's KeyboardNavigation / virtualization settings are ListBox defaults
//   Q13 Recycling: does an expanded fold leak to the turn that reuses the container, and does the
//      original turn remember it when scrolled back?
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

internal static class Program
{
    private const string WindowTitle = "Session Thread Spike Host";
    private const double WindowWidth = 1440;
    private const double WindowHeight = 900;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "host")
        {
            return RunHost();
        }

        if (args.Length > 0 && args[0] == "composer")
        {
            return RunComposerLeg();
        }

        return RunProbe();
    }

    // ── The turn, as the fixture renders it ────────────────────────────────────────────────────
    public sealed class Turn
    {
        public int Ordinal { get; init; }
        public string Words { get; init; } = "";
        public string Decoration { get; init; } = "";
        public string Outcome { get; init; } = "";
        public IReadOnlyList<string> Events { get; init; } = Array.Empty<string>();
        public string Gutter => "b" + Ordinal.ToString(CultureInfo.InvariantCulture);
        public string Name => Gutter + ", " + Words;
        public string FoldHeader => Events.Count.ToString(CultureInfo.InvariantCulture) + " events";
    }

    /// <summary>A row view-model that changes in place (the reconcile-by-key shape, DC-029).</summary>
    public sealed class MutableTurn : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        public int Ordinal { get; private init; }
        public string Words { get; private init; } = "";
        public string Decoration { get; private init; } = "";
        public string Outcome { get; private set; } = "";
        public IReadOnlyList<string> Events { get; private set; } = Array.Empty<string>();
        public string Gutter => "b" + Ordinal.ToString(CultureInfo.InvariantCulture);
        public string Name => Gutter + ", " + Words;
        public string FoldHeader => Events.Count.ToString(CultureInfo.InvariantCulture) + " events";

        public static MutableTurn From(Turn t) => new() { Ordinal = t.Ordinal, Words = t.Words, Decoration = t.Decoration, Outcome = t.Outcome, Events = t.Events };

        public void Update(string outcome, IReadOnlyList<string> events)
        {
            Outcome = outcome;
            Events = events;
            foreach (var n in new[] { nameof(Outcome), nameof(Events), nameof(FoldHeader) })
            {
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(n));
            }
        }
    }

    /// <summary>Deterministic variable-height turns: the words cycle 40..600 chars, the events 3..20.</summary>
    private static ObservableCollection<Turn> Fixture(int count)
    {
        const string seed = "Refactor the layout store's reconcile so a maximized stack remembers its siblings; keep the tests green. ";
        var list = new ObservableCollection<Turn>();
        for (var i = 1; i <= count; i++)
        {
            var reps = 1 + (i * 7) % 6;                  // 1..6 repetitions → 100..600 chars
            var words = string.Concat(Enumerable.Repeat(seed, reps)).TrimEnd();
            var events = Enumerable.Range(1, 3 + (i * 5) % 18)
                .Select(k => $"15:{k:00}:{(k * 7) % 60:00}  claude-code  edit src/File{k}.cs")
                .ToArray();
            list.Add(new Turn
            {
                Ordinal = i,
                Words = words,
                Decoration = i % 4 == 0 ? "class defect · tier T2 · lease src/** · goal block" : "class free-form · tier T0 · message",
                Outcome = i % 9 == 0 ? "lane exited 1 · claude-code · 37 s · 12 events" : "completed · claude-code · 3 edits · 12,400 tokens · 4 min 12 s",
                Events = events,
            });
        }

        return list;
    }

    // ── The feed: a ListBox with the APG feed keys ─────────────────────────────────────────────
    public sealed class ThreadFeed : ListBox
    {
        public event Action? EditorRequested;   // Ctrl+End
        public event Action? HeaderRequested;   // Ctrl+Home

        public ThreadFeed()
        {
            // SPIKE_VIRT=off turns virtualization OFF (Q15: the wrong shape's layout cost, to calibrate L2's ceiling).
            VirtualizingPanel.SetIsVirtualizing(this, Environment.GetEnvironmentVariable("SPIKE_VIRT") != "off");
            VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Recycling);
            VirtualizingPanel.SetScrollUnit(this, ScrollUnit.Pixel);
            ScrollViewer.SetCanContentScroll(this, true);
            ScrollViewer.SetHorizontalScrollBarVisibility(this, ScrollBarVisibility.Disabled);
            // SPIKE_TABNAV=Local|Once|Continue|Contained|Cycle — the mode under test (default Once — the mode the design chooses; Local and Continue trap Tab at the fold, measured).
            var mode = Enum.TryParse<KeyboardNavigationMode>(Environment.GetEnvironmentVariable("SPIKE_TABNAV"), out var m) ? m : KeyboardNavigationMode.Once;
            KeyboardNavigation.SetTabNavigation(this, mode);
            KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.Contained);
            SelectionMode = SelectionMode.Single;
            BorderThickness = new Thickness(0);
            AutomationProperties.SetName(this, "Conversation");
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (HandleFeedKey(e.Key, e.KeyboardDevice.Modifiers))
            {
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        /// <summary>The feed's own keys, as a pure decision a headless test can call (the NodeReaderView idiom).</summary>
        public bool HandleFeedKey(System.Windows.Input.Key key, ModifierKeys modifiers)
        {
            var ctrl = (modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            switch (key)
            {
                case System.Windows.Input.Key.PageDown when !ctrl:
                    MoveBy(+1);
                    return true;
                case System.Windows.Input.Key.PageUp when !ctrl:
                    MoveBy(-1);
                    return true;
                case System.Windows.Input.Key.End when ctrl:
                    EditorRequested?.Invoke();
                    return true;
                case System.Windows.Input.Key.Home when ctrl:
                    HeaderRequested?.Invoke();
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>F6 / Tab entry: the current turn, realized and focused (never WPF's default traversal).</summary>
        public bool FocusCurrentTurn()
        {
            if (Items.Count == 0)
            {
                return false;
            }

            var index = SelectedIndex < 0 ? Items.Count - 1 : SelectedIndex;
            SelectedIndex = index;
            ScrollIntoView(Items[index]);
            UpdateLayout();
            return ItemContainerGenerator.ContainerFromIndex(index) is ListBoxItem item && item.Focus();
        }

        private void MoveBy(int delta)
        {
            if (Items.Count == 0)
            {
                return;
            }

            var next = Math.Clamp(SelectedIndex < 0 ? 0 : SelectedIndex + delta, 0, Items.Count - 1);
            SelectedIndex = next;
            ScrollIntoView(Items[next]);
            UpdateLayout();
            if (ItemContainerGenerator.ContainerFromIndex(next) is ListBoxItem item)
            {
                item.Focus();
            }
        }
    }

    private static DataTemplate TurnTemplate(bool rovingHeader = false)
    {
        // Grid: 40px gutter | the turn body. Built in code so the spike has no XAML to load.
        var grid = new FrameworkElementFactory(typeof(Grid));
        var gutterCol = new FrameworkElementFactory(typeof(ColumnDefinition));
        gutterCol.SetValue(ColumnDefinition.WidthProperty, new GridLength(40));
        var bodyCol = new FrameworkElementFactory(typeof(ColumnDefinition));
        bodyCol.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
        grid.AppendChild(gutterCol);
        grid.AppendChild(bodyCol);

        var gutter = new FrameworkElementFactory(typeof(TextBlock));
        gutter.SetBinding(TextBlock.TextProperty, new Binding(nameof(Turn.Gutter)));
        gutter.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Consolas"));
        gutter.SetValue(Grid.ColumnProperty, 0);
        grid.AppendChild(gutter);

        var body = new FrameworkElementFactory(typeof(StackPanel));
        body.SetValue(Grid.ColumnProperty, 1);
        body.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 12, 0, 8));

        var words = new FrameworkElementFactory(typeof(TextBlock));
        words.SetBinding(TextBlock.TextProperty, new Binding(nameof(Turn.Words)));
        words.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        words.SetValue(FrameworkElement.MaxWidthProperty, 96 * 7.0);   // ≈ a 96ch measure at 13px
        words.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        body.AppendChild(words);

        var deco = new FrameworkElementFactory(typeof(TextBlock));
        deco.SetBinding(TextBlock.TextProperty, new Binding(nameof(Turn.Decoration)));
        deco.SetValue(FrameworkElement.HeightProperty, 24.0);
        body.AppendChild(deco);

        var outcome = new FrameworkElementFactory(typeof(TextBlock));
        outcome.SetBinding(TextBlock.TextProperty, new Binding(nameof(Turn.Outcome)));
        outcome.SetValue(FrameworkElement.HeightProperty, 24.0);
        body.AppendChild(outcome);

        // The fold: an Expander whose header is "N events" and whose content is the event lines.
        var fold = new FrameworkElementFactory(typeof(Expander));
        fold.SetBinding(HeaderedContentControl.HeaderProperty, new Binding(nameof(Turn.FoldHeader)));
        fold.SetValue(Expander.IsExpandedProperty, false);
        fold.SetValue(FrameworkElement.NameProperty, "fold");
        // Roving tab stop: the fold is a tab stop only inside the CURRENT (selected) turn.
        var roving = new Binding(nameof(ListBoxItem.IsSelected))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ListBoxItem), 1),
        };
        if (rovingHeader)
        {
            // Q11: the Expander is not focusable; its HeaderSite (a ToggleButton inside the template)
            // roves through an implicit style in the Expander's own resources.
            fold.SetValue(UIElement.FocusableProperty, false);
            fold.SetValue(KeyboardNavigation.IsTabStopProperty, false);
            // The implicit ToggleButton style lives in the FEED's resources here (a factory cannot set
            // Resources); in XAML it would sit in Expander.Resources. Implicit styles for Control-derived
            // elements cross the template boundary, which is the mechanism under test.
        }
        else
        {
            fold.SetBinding(KeyboardNavigation.IsTabStopProperty, roving);
        }
        var lines = new FrameworkElementFactory(typeof(ItemsControl));
        lines.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(Turn.Events)));
        fold.AppendChild(lines);
        body.AppendChild(fold);

        grid.AppendChild(body);
        return new DataTemplate { VisualTree = grid };
    }

    private static Style TurnContainerStyle()
    {
        var style = new Style(typeof(ListBoxItem));
        style.Setters.Add(new Setter(AutomationProperties.NameProperty, new Binding(nameof(Turn.Name))));
        style.Setters.Add(new Setter(AutomationProperties.ItemStatusProperty, new Binding(nameof(Turn.Decoration))));
        // Roving tab stop: only the CURRENT (selected) turn is a tab stop, so Tab enters the feed once
        // and PageDown / PageUp move between turns.
        style.Setters.Add(new Setter(KeyboardNavigation.IsTabStopProperty, new Binding(nameof(ListBoxItem.IsSelected)) { RelativeSource = RelativeSource.Self }));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        return style;
    }

    // ── The host ───────────────────────────────────────────────────────────────────────────────
    private static int RunHost()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        var report = new StringBuilder();
        void M(string key, object value)
        {
            var line = $"MEASURE {key}={Convert.ToString(value, CultureInfo.InvariantCulture)}";
            Console.WriteLine(line);
            report.AppendLine(line);
        }

        var header = new Border { Height = 28, Background = Brushes.WhiteSmoke };
        var headerButton = new Button { Content = "Console", Width = 80, HorizontalAlignment = HorizontalAlignment.Left };
        AutomationProperties.SetName(headerButton, "Console: show the merged stream beside the thread");
        header.Child = headerButton;

        var feed = new ThreadFeed { ItemTemplate = TurnTemplate(), ItemContainerStyle = TurnContainerStyle() };

        var composer = new Border { Height = 130 + 4 * 24, Background = Brushes.LightGray };
        var composerInput = new TextBox { AcceptsReturn = true, Height = 130, VerticalAlignment = VerticalAlignment.Top };
        AutomationProperties.SetName(composerInput, "Message");
        composer.Child = composerInput;

        var polite = new TextBlock { Text = "", MinHeight = 16 };
        AutomationProperties.SetLiveSetting(polite, AutomationLiveSetting.Polite);
        AutomationProperties.SetName(polite, "Status (polite)");
        var assertive = new TextBlock { Text = "", MinHeight = 16 };
        AutomationProperties.SetLiveSetting(assertive, AutomationLiveSetting.Assertive);
        AutomationProperties.SetName(assertive, "Status (assertive)");

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(header, 0);
        Grid.SetRow(feed, 1);
        Grid.SetRow(composer, 2);
        var live = new StackPanel();
        live.Children.Add(polite);
        live.Children.Add(assertive);
        Grid.SetRow(live, 3);
        grid.Children.Add(header);
        grid.Children.Add(feed);
        grid.Children.Add(composer);
        grid.Children.Add(live);

        feed.EditorRequested += () => composerInput.Focus();
        feed.HeaderRequested += () => headerButton.Focus();

        var window = new Window
        {
            Title = WindowTitle,
            Width = WindowWidth,
            Height = WindowHeight,
            Content = grid,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };

        window.ContentRendered += (_, _) => window.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            try
            {
                Measure(window, feed, composer, composerInput, headerButton, polite, assertive, M);
                Console.WriteLine("HOST READY");
            }
            catch (Exception ex)
            {
                Console.WriteLine("HOST ERROR " + ex);
            }
        });

        // The probe closes us by killing the process; a human closes the window.
        return app.Run(window);
    }

    private static void Measure(
        Window window, ThreadFeed feed, Border composer, TextBox composerInput, Button headerButton,
        TextBlock polite, TextBlock assertive, Action<string, object> M)
    {
        double ComposerTop() => composer.TransformToAncestor(window).Transform(new Point(0, 0)).Y;
        int Realized(int n) => Enumerable.Range(0, n).Count(i => feed.ItemContainerGenerator.ContainerFromIndex(i) is not null);
        ScrollViewer Scroller() => FindChild<ScrollViewer>(feed) ?? throw new InvalidOperationException("no ScrollViewer in the ListBox template");

        // Q1..Q3: virtualization, the pinned composer, the layout cost ratio.
        var layoutMs = new Dictionary<int, double>();
        foreach (var n in new[] { 1, 5, 40, 400 })
        {
            var sw = Stopwatch.StartNew();
            feed.ItemsSource = Fixture(n);
            feed.UpdateLayout();
            feed.ScrollIntoView(feed.Items[^1]);
            feed.UpdateLayout();
            sw.Stop();
            layoutMs[n] = sw.Elapsed.TotalMilliseconds;
            M($"turns_{n}.composer_top", ComposerTop());
            M($"turns_{n}.realized_containers", Realized(n));
            M($"turns_{n}.viewport_height", Scroller().ViewportHeight);
            M($"turns_{n}.extent_height", Scroller().ExtentHeight);
            M($"turns_{n}.layout_ms", Math.Round(layoutMs[n], 2));
        }

        M("layout_ratio_400_over_40", Math.Round(layoutMs[400] / Math.Max(layoutMs[40], 0.01), 2));
        // Q15: p95 over 10 InvalidateMeasure + UpdateLayout passes per size (the L2 idiom), same run.
        foreach (var n in new[] { 40, 400 })
        {
            feed.ItemsSource = Fixture(n);
            feed.UpdateLayout();
            var samples = new List<double>();
            for (var k = 0; k < 10; k++)
            {
                var sw2 = Stopwatch.StartNew();
                feed.InvalidateMeasure();
                foreach (var i in Enumerable.Range(0, n)) { if (feed.ItemContainerGenerator.ContainerFromIndex(i) is UIElement c) { c.InvalidateMeasure(); } }
                feed.UpdateLayout();
                sw2.Stop();
                samples.Add(sw2.Elapsed.TotalMilliseconds);
            }
            samples.Sort();
            M($"q15.turns_{n}.p95_layout_ms", Math.Round(samples[(int)Math.Ceiling(samples.Count * 0.95) - 1], 2));
            M($"q15.turns_{n}.realized", Enumerable.Range(0, n).Count(i => feed.ItemContainerGenerator.ContainerFromIndex(i) is not null));
        }
        M("q15.virtualizing", Environment.GetEnvironmentVariable("SPIKE_VIRT") != "off");
        M("panel_type", FindChild<VirtualizingStackPanel>(feed)?.GetType().Name ?? "not a VirtualizingStackPanel");
        M("tab_navigation_mode_under_test", KeyboardNavigation.GetTabNavigation(feed));

        // Q4: the follow rule. Pinned at the end, then append: does the offset follow by itself?
        var source = Fixture(40);
        feed.ItemsSource = source;
        feed.UpdateLayout();
        var scroller = Scroller();
        scroller.ScrollToEnd();
        feed.UpdateLayout();
        var pinnedBefore = scroller.VerticalOffset >= scroller.ScrollableHeight - 0.5;
        M("follow.pinned_before_append", pinnedBefore);
        M("follow.offset_before_append", Math.Round(scroller.VerticalOffset, 1));
        M("follow.scrollable_before_append", Math.Round(scroller.ScrollableHeight, 1));
        M("follow.extent_before_append", Math.Round(scroller.ExtentHeight, 1));
        source.Add(new Turn { Ordinal = 41, Words = "appended", Outcome = "running", Decoration = "class free-form · tier T0 · message", Events = new[] { "a", "b", "c" } });
        feed.UpdateLayout();
        M("follow.offset_after_append", Math.Round(scroller.VerticalOffset, 1));
        M("follow.scrollable_after_append", Math.Round(scroller.ScrollableHeight, 1));
        M("follow.extent_after_append", Math.Round(scroller.ExtentHeight, 1));
        M("follow.listbox_followed_by_itself", scroller.VerticalOffset >= scroller.ScrollableHeight - 0.5);
        // A reader mid-thread: scroll to the middle, append, read the offset again.
        scroller.ScrollToVerticalOffset(scroller.ScrollableHeight / 2);
        feed.UpdateLayout();
        var midBefore = scroller.VerticalOffset;
        source.Add(new Turn { Ordinal = 42, Words = "appended again", Outcome = "running", Decoration = "class free-form · tier T0 · message", Events = new[] { "a" } });
        feed.UpdateLayout();
        M("follow.mid_thread_offset_before", Math.Round(midBefore, 1));
        M("follow.mid_thread_offset_after", Math.Round(scroller.VerticalOffset, 1));
        M("follow.mid_thread_reader_moved", Math.Abs(scroller.VerticalOffset - midBefore) > 0.5);

        // Q5: keyboard. A real window, real keyboard focus, key events raised on the focused element.
        feed.ItemsSource = Fixture(40);
        feed.UpdateLayout();
        feed.SelectedIndex = 39;
        feed.ScrollIntoView(feed.Items[39]);
        feed.UpdateLayout();
        var last = (ListBoxItem)feed.ItemContainerGenerator.ContainerFromIndex(39)!;
        M("keys.focus_last_item", last.Focus());
        M("keys.selected_after_focus", feed.SelectedIndex);

        Press(last, System.Windows.Input.Key.PageUp);
        M("keys.pageup_selected", feed.SelectedIndex);
        M("keys.pageup_focused_is_item", Keyboard.FocusedElement is ListBoxItem);
        Press(FocusedItem(feed), System.Windows.Input.Key.PageDown);
        M("keys.pagedown_selected", feed.SelectedIndex);
        Press(FocusedItem(feed), System.Windows.Input.Key.Up);
        M("keys.up_selected", feed.SelectedIndex);
        Press(FocusedItem(feed), System.Windows.Input.Key.Down);
        M("keys.down_selected", feed.SelectedIndex);
        Press(FocusedItem(feed), System.Windows.Input.Key.Home);
        M("keys.home_selected", feed.SelectedIndex);
        Press(FocusedItem(feed), System.Windows.Input.Key.End);
        M("keys.end_selected", feed.SelectedIndex);

        // Tab from the current turn: the fold, then out of the feed.
        var current = FocusedItem(feed);
        current.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        M("keys.tab1_focused", Describe(Keyboard.FocusedElement));
        (Keyboard.FocusedElement as UIElement)?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        M("keys.tab2_focused", Describe(Keyboard.FocusedElement));
        M("keys.tab2_left_the_feed", Keyboard.FocusedElement is DependencyObject d && !feed.IsAncestorOf(d));
        (Keyboard.FocusedElement as UIElement)?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        M("keys.tab3_focused", Describe(Keyboard.FocusedElement));
        M("keys.tab3_left_the_feed", Keyboard.FocusedElement is DependencyObject d3 && !feed.IsAncestorOf(d3));
        // Shift+Tab from the composer returns to the feed's current turn.
        (Keyboard.FocusedElement as UIElement)?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
        M("keys.shift_tab_back_focused", Describe(Keyboard.FocusedElement));
        // The fold in a NON-current turn is not a tab stop (roving tab stop).
        var other = (ListBoxItem)feed.ItemContainerGenerator.ContainerFromIndex(feed.SelectedIndex == 39 ? 38 : 39)!;
        var otherFold = FindChild<Expander>(other);
        M("keys.other_turn_fold_is_tab_stop", otherFold is null ? "not realized" : (object)KeyboardNavigation.GetIsTabStop(otherFold));
        var currentFold = FindChild<Expander>(FocusedItemOrSelected(feed));
        M("keys.current_turn_fold_is_tab_stop", currentFold is null ? "not realized" : (object)KeyboardNavigation.GetIsTabStop(currentFold));

        // Ctrl+End reaches the editor; Ctrl+Home the header.
        FocusedItemOrSelected(feed).Focus();
        M("keys.ctrl_end_handled", feed.HandleFeedKey(System.Windows.Input.Key.End, ModifierKeys.Control));
        M("keys.ctrl_end_focused", Describe(Keyboard.FocusedElement));
        M("keys.ctrl_end_reached_editor", ReferenceEquals(Keyboard.FocusedElement, composerInput));
        FocusedItemOrSelected(feed).Focus();
        M("keys.ctrl_home_handled", feed.HandleFeedKey(System.Windows.Input.Key.Home, ModifierKeys.Control));
        M("keys.ctrl_home_reached_header", ReferenceEquals(Keyboard.FocusedElement, headerButton));
        M("keys.plain_end_not_handled_by_feed", !feed.HandleFeedKey(System.Windows.Input.Key.End, ModifierKeys.None));

        // Tab INTO the feed from the header when the current turn is scrolled out of view (unrealized):
        // WPF's default traversal vs the feed's own FocusCurrentTurn().
        feed.SelectedIndex = 39;
        Scroller().ScrollToTop();
        feed.UpdateLayout();
        M("keys.entry.current_turn_realized_after_scroll_to_top", feed.ItemContainerGenerator.ContainerFromIndex(39) is not null);
        headerButton.Focus();
        headerButton.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        M("keys.entry.default_tab_from_header_focused", Describe(Keyboard.FocusedElement));
        M("keys.entry.default_tab_landed_in_feed", Keyboard.FocusedElement is DependencyObject d0 && feed.IsAncestorOf(d0));
        headerButton.Focus();
        M("keys.entry.focus_current_turn_returned", feed.FocusCurrentTurn());
        M("keys.entry.focus_current_turn_focused", Describe(Keyboard.FocusedElement));

        // The baseline: a plain ListBox's own PageDown from the first item moves by a PAGE, not one.
        var plain = new ListBox { ItemsSource = Fixture(40).Select(t => t.Words).ToList(), Height = 400 };
        var probeWindow = new Window { Width = 800, Height = 500, Content = plain, Left = -10000, Top = -10000, ShowActivated = false };
        probeWindow.Show();
        plain.UpdateLayout();
        plain.SelectedIndex = 0;
        var first = (ListBoxItem)plain.ItemContainerGenerator.ContainerFromIndex(0)!;
        first.Focus();
        Press(first, System.Windows.Input.Key.PageDown);
        M("keys.plain_listbox_pagedown_from_0_selects", plain.SelectedIndex);
        probeWindow.Close();

        // Q6: automation peers, in-process.
        var feedPeer = UIElementAutomationPeer.CreatePeerForElement(feed);
        M("peer.feed_type", feedPeer.GetType().Name);
        M("peer.feed_control_type", feedPeer.GetAutomationControlType());
        M("peer.feed_name", feedPeer.GetName());
        var itemPeer = UIElementAutomationPeer.CreatePeerForElement(last);
        M("peer.item_type", itemPeer.GetType().Name);
        M("peer.item_control_type", itemPeer.GetAutomationControlType());
        M("peer.item_name", itemPeer.GetName());
        M("peer.item_status", itemPeer.GetItemStatus());
        M("peer.item_position_in_set", itemPeer.GetPositionInSet());
        M("peer.item_size_of_set", itemPeer.GetSizeOfSet());
        M("peer.item_selection_pattern", itemPeer.GetPattern(PatternInterface.SelectionItem) is not null);
        M("peer.item_scrollitem_pattern", itemPeer.GetPattern(PatternInterface.ScrollItem) is not null);
        var bare = new ItemsControl { ItemsSource = new[] { "a", "b" } };
        var barePeer = UIElementAutomationPeer.CreatePeerForElement(bare);
        M("peer.bare_itemscontrol_peer", barePeer?.GetType().Name ?? "null (no peer)");
        M("peer.bare_itemscontrol_control_type", barePeer?.GetAutomationControlType().ToString() ?? "n/a");

        // Q7: the Expander's pattern and its keys.
        var fold = FindChild<Expander>(last) ?? throw new InvalidOperationException("no Expander realized in the last turn");
        var foldPeer = UIElementAutomationPeer.CreatePeerForElement(fold);
        M("fold.peer_type", foldPeer.GetType().Name);
        M("fold.control_type", foldPeer.GetAutomationControlType());
        M("fold.expandcollapse_pattern", foldPeer.GetPattern(PatternInterface.ExpandCollapse) is not null);
        M("fold.name", foldPeer.GetName());
        var toggle = FindChild<ToggleButton>(fold);
        M("fold.header_toggle_type", toggle?.GetType().Name ?? "not found");
        M("fold.header_accepts_return", toggle is null ? "n/a" : (object)KeyboardNavigation.GetAcceptsReturn(toggle));
        M("fold.header_is_tab_stop", toggle is null ? "n/a" : (object)KeyboardNavigation.GetIsTabStop(toggle));
        M("plain_button_accepts_return", KeyboardNavigation.GetAcceptsReturn(new Button()));
        M("plain_togglebutton_accepts_return", KeyboardNavigation.GetAcceptsReturn(new ToggleButton()));
        if (toggle is not null)
        {
            toggle.Focus();
            Press(toggle, System.Windows.Input.Key.Enter);
            M("fold.expanded_after_enter", fold.IsExpanded);
            fold.IsExpanded = false;
            Press(toggle, System.Windows.Input.Key.Space);
            Release(toggle, System.Windows.Input.Key.Space);
            M("fold.expanded_after_space", fold.IsExpanded);
            fold.IsExpanded = false;
        }

        // Q8: live settings and the notification event.
        M("live.polite", UIElementAutomationPeer.CreatePeerForElement(polite).GetLiveSetting());
        M("live.assertive", UIElementAutomationPeer.CreatePeerForElement(assertive).GetLiveSetting());
        try
        {
            var p = UIElementAutomationPeer.CreatePeerForElement(polite);
            polite.Text = "Turn b2 completed: 3 edits, 12,400 tokens, 4 minutes 12 seconds";
            p.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            p.RaiseNotificationEvent(AutomationNotificationKind.ActionCompleted, AutomationNotificationProcessing.MostRecent, polite.Text, "aide.thread.outcome");
            var a = UIElementAutomationPeer.CreatePeerForElement(assertive);
            assertive.Text = "Turn b5: the lane exited 1 before any edit.";
            a.RaiseNotificationEvent(AutomationNotificationKind.ActionAborted, AutomationNotificationProcessing.ImportantAll, assertive.Text, "aide.thread.error");
            M("live.raise_notification_threw", false);
        }
        catch (Exception ex)
        {
            M("live.raise_notification_threw", ex.GetType().Name);
        }

        // Q10: an event line arrives on the running (current) turn while focus is on its fold.
        //      (a) replace the item at its index with a new record; (b) mutate a row that raises
        //      PropertyChanged. Which keeps the container, and the focus, alive?
        var rows = new ObservableCollection<Turn>(Fixture(40));
        feed.ItemsSource = rows;
        feed.SelectedIndex = 39;
        feed.ScrollIntoView(feed.Items[39]);
        feed.UpdateLayout();
        var before = (ListBoxItem)feed.ItemContainerGenerator.ContainerFromIndex(39)!;
        var beforeFold = FindChild<ToggleButton>(FindChild<Expander>(before)!)!;
        beforeFold.Focus();
        M("replace.focus_on_fold_before", ReferenceEquals(Keyboard.FocusedElement, beforeFold));
        rows[39] = new Turn { Ordinal = 40, Words = rows[39].Words, Decoration = rows[39].Decoration, Outcome = "running · claude-code · 5 events", Events = new[] { "a", "b", "c", "d", "e" } };
        feed.UpdateLayout();
        var after = feed.ItemContainerGenerator.ContainerFromIndex(39) as ListBoxItem;
        M("replace.same_container_after_index_replace", ReferenceEquals(before, after));
        M("replace.focus_after_index_replace", Describe(Keyboard.FocusedElement));
        M("replace.focus_still_in_feed_after_index_replace", Keyboard.FocusedElement is DependencyObject dr && feed.IsAncestorOf(dr));
        M("replace.selected_after_index_replace", feed.SelectedIndex);

        var mrows = new ObservableCollection<MutableTurn>(Fixture(40).Select(MutableTurn.From));
        feed.ItemsSource = mrows;
        feed.SelectedIndex = 39;
        feed.ScrollIntoView(feed.Items[39]);
        feed.UpdateLayout();
        var mBefore = (ListBoxItem)feed.ItemContainerGenerator.ContainerFromIndex(39)!;
        var mFold = FindChild<ToggleButton>(FindChild<Expander>(mBefore)!)!;
        mFold.Focus();
        M("mutate.focus_on_fold_before", ReferenceEquals(Keyboard.FocusedElement, mFold));
        mrows[39].Update("running · claude-code · 5 events", new[] { "a", "b", "c", "d", "e" });
        feed.UpdateLayout();
        M("mutate.same_container_after_update", ReferenceEquals(mBefore, feed.ItemContainerGenerator.ContainerFromIndex(39)));
        M("mutate.focus_after_update", Describe(Keyboard.FocusedElement));
        M("mutate.focus_still_on_fold_after_update", ReferenceEquals(Keyboard.FocusedElement, mFold));
        M("mutate.fold_header_after_update", FindChild<Expander>(mBefore)!.Header?.ToString() ?? "null");

        // Q11: the designed fold shape — the Expander is NOT focusable; its HeaderSite toggle is the
        //      one roving stop (an implicit ToggleButton style in the Expander's resources, binding
        //      IsTabStop to the ancestor ListBoxItem.IsSelected). Tab: turn → HeaderSite → composer?
        //      Space / Enter on the HeaderSite toggle the fold?
        var feed2 = new ThreadFeed { ItemTemplate = TurnTemplate(rovingHeader: true), ItemContainerStyle = TurnContainerStyle() };
        var headerStyle = new Style(typeof(ToggleButton));
        headerStyle.Setters.Add(new Setter(KeyboardNavigation.IsTabStopProperty, new Binding(nameof(ListBoxItem.IsSelected))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ListBoxItem), 1),
        }));
        feed2.Resources.Add(typeof(ToggleButton), headerStyle);
        var grid = (Grid)feed.Parent;
        Grid.SetRow(feed2, 1);
        grid.Children.Remove(feed);
        grid.Children.Add(feed2);
        feed2.ItemsSource = Fixture(40);
        feed2.UpdateLayout();
        Pump();
        feed2.SelectedIndex = 39;
        feed2.ScrollIntoView(feed2.Items[39]);
        feed2.UpdateLayout();
        Pump();   // ListBox.ScrollIntoView defers to a dispatcher op when the container is not yet generated
        feed2.UpdateLayout();
        var c2 = feed2.ItemContainerGenerator.ContainerFromIndex(39) as ListBoxItem ?? throw new InvalidOperationException("q11: the last container is not realized");
        c2.ApplyTemplate();
        feed2.UpdateLayout();
        var e2 = FindChild<Expander>(c2) ?? throw new InvalidOperationException("q11: no Expander in the last container");
        e2.ApplyTemplate();
        var h2 = FindChild<ToggleButton>(e2) ?? throw new InvalidOperationException("q11: no HeaderSite in the Expander");
        M("q11.expander_focusable", e2.Focusable);
        M("q11.expander_is_tab_stop", KeyboardNavigation.GetIsTabStop(e2));
        M("q11.header_is_tab_stop_current", KeyboardNavigation.GetIsTabStop(h2));
        var o2 = feed2.ItemContainerGenerator.ContainerFromIndex(38) as ListBoxItem;
        var oh2 = o2 is null ? null : FindChild<Expander>(o2) is { } oe ? FindChild<ToggleButton>(oe) : null;
        M("q11.header_is_tab_stop_other", oh2 is null ? "not realized" : (object)KeyboardNavigation.GetIsTabStop(oh2));
        c2.Focus();
        c2.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        M("q11.tab1_focused", Describe(Keyboard.FocusedElement));
        (Keyboard.FocusedElement as UIElement)?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        M("q11.tab2_focused", Describe(Keyboard.FocusedElement));
        M("q11.tab2_left_the_feed", Keyboard.FocusedElement is DependencyObject d2 && !feed2.IsAncestorOf(d2));
        (Keyboard.FocusedElement as UIElement)?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
        M("q11.shift_tab_back_focused", Describe(Keyboard.FocusedElement));
        h2.Focus();
        Press(h2, System.Windows.Input.Key.Enter);
        M("q11.expanded_after_enter", e2.IsExpanded);
        e2.IsExpanded = false;
        Press(h2, System.Windows.Input.Key.Space);
        Release(h2, System.Windows.Input.Key.Space);
        M("q11.expanded_after_space", e2.IsExpanded);
        e2.IsExpanded = false;
        grid.Children.Remove(feed2);
        grid.Children.Add(feed);

        // Q12: which of P2's settings are ListBox defaults?
        var plainBox = new ListBox();
        M("q12.listbox_default_tab_navigation", KeyboardNavigation.GetTabNavigation(plainBox));
        M("q12.listbox_default_directional_navigation", KeyboardNavigation.GetDirectionalNavigation(plainBox));
        M("q12.listbox_default_is_virtualizing", VirtualizingPanel.GetIsVirtualizing(plainBox));
        M("q12.listbox_default_virtualization_mode", VirtualizingPanel.GetVirtualizationMode(plainBox));
        M("q12.listbox_default_scroll_unit", VirtualizingPanel.GetScrollUnit(plainBox));
        M("q12.listbox_default_can_content_scroll", ScrollViewer.GetCanContentScroll(plainBox));
        M("q12.listboxitem_default_is_tab_stop", KeyboardNavigation.GetIsTabStop(new ListBoxItem()));

        // Q13: Recycling and unbound visual state — expand b3's fold, scroll it out of realization,
        //      read the container that took its place, scroll back: does b3 remember? does b30 inherit?
        feed.ItemsSource = Fixture(40);
        feed.UpdateLayout();
        Pump();
        feed.ScrollIntoView(feed.Items[2]);
        feed.UpdateLayout();
        Pump();
        var b3 = feed.ItemContainerGenerator.ContainerFromIndex(2) as ListBoxItem ?? throw new InvalidOperationException("q13: b3 not realized");
        var b3Fold = FindChild<Expander>(b3) ?? throw new InvalidOperationException("q13: no fold in b3");
        b3Fold.IsExpanded = true;
        feed.UpdateLayout();
        M("q13.b3_expanded_before_scroll", b3Fold.IsExpanded);
        Scroller().ScrollToEnd();
        feed.UpdateLayout();
        Pump();
        feed.UpdateLayout();
        M("q13.b3_realized_after_scroll_to_end", feed.ItemContainerGenerator.ContainerFromIndex(2) is not null);
        var realizedAtEnd = Enumerable.Range(0, 40).Where(i => feed.ItemContainerGenerator.ContainerFromIndex(i) is not null).ToArray();
        M("q13.realized_at_end", string.Join(",", realizedAtEnd.Select(i => "b" + (i + 1))));
        var expandedAtEnd = realizedAtEnd.Where(i => FindChild<Expander>((ListBoxItem)feed.ItemContainerGenerator.ContainerFromIndex(i)!) is { IsExpanded: true }).Select(i => "b" + (i + 1)).ToArray();
        M("q13.turns_showing_expanded_at_end", expandedAtEnd.Length == 0 ? "none" : string.Join(",", expandedAtEnd));
        var reused = realizedAtEnd.Select(i => (ListBoxItem)feed.ItemContainerGenerator.ContainerFromIndex(i)!).FirstOrDefault(c => ReferenceEquals(c, b3));
        M("q13.b3_container_reused_for", reused is null ? "not reused (still b3 or discarded)" : "b" + (feed.ItemContainerGenerator.IndexFromContainer(reused) + 1));
        Scroller().ScrollToTop();
        feed.UpdateLayout();
        Pump();
        feed.ScrollIntoView(feed.Items[2]);
        feed.UpdateLayout();
        Pump();
        var b3Again = feed.ItemContainerGenerator.ContainerFromIndex(2) as ListBoxItem;
        M("q13.b3_same_container_after_return", ReferenceEquals(b3, b3Again));
        M("q13.b3_expanded_after_return", b3Again is null ? "not realized" : (object)(FindChild<Expander>(b3Again)?.IsExpanded ?? false));

        // Leave 40 turns on screen for the out-of-process probe, the last turn current.
        feed.ItemsSource = Fixture(40);
        feed.SelectedIndex = 39;
        feed.ScrollIntoView(feed.Items[39]);
        feed.UpdateLayout();
        FocusedItemOrSelected(feed).Focus();
    }

    /// <summary>Drains queued dispatcher work down to Background priority (a deferred ScrollIntoView).</summary>
    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

    // ── Q14: the REAL composer in an Auto row of the design's Grid (header Auto | thread * | composer Auto)
    //    at 1440 × 900, measured detached (no browser). Does the editor host get its floor, does the
    //    compiled view's DC-137 cap run under an infinite constraint, does the thread row survive?
    private static int RunComposerLeg()
    {
        void M(string key, object value) => Console.WriteLine($"MEASURE {key}={Convert.ToString(value, CultureInfo.InvariantCulture)}");
        var root = Path.Combine(Path.GetTempPath(), "aide-thread-spike", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            foreach (var belt in new[] { false, true })
            {
                var composer = new AiDe.App.Workbench.Composer.ComposerSurface("composer:spike", "spike — composer");
                composer.Draft.SwitchTo(AiDe.Core.Presentation.Composer.ComposerShape.GoalBlock);
                composer.Draft.SetGoalValue(AiDe.Core.AgentPlane.GoalBlockFields.GoalKey, string.Join("\n", Enumerable.Range(1, 12).Select(i => $"Goal line {i}: investigate why the composer accepts no typing and name the cause.")));
                composer.Draft.SetGoalValue(AiDe.Core.AgentPlane.GoalBlockFields.DoneWhenKey, string.Join("\n", Enumerable.Range(1, 10).Select(i => $"Done when {i}: a red test exists and the INV is written.")));
                composer.Draft.SetGoalValue(AiDe.Core.AgentPlane.GoalBlockFields.NotInScopeKey, string.Join("\n", Enumerable.Range(1, 8).Select(i => $"Not in scope {i}: the vendored bundle.")));
                composer.Configure(
                    new AiDe.Core.Sessions.SessionConfig("s-spike", "spike", "w-1", DateTimeOffset.UnixEpoch, ["claude-code"]),
                    new AiDe.App.Workbench.Composer.ComposerSendContext(
                        RepositoryRoot: root, DataDirectory: root, AdapterInstallRoot: root, EngineId: "claude-code", Model: "sonnet",
                        AccountLabel: "max-personal", TaskClass: "implement", ProofPackArtifacts: [], Providers: []),
                    AiDe.App.Workbench.Composer.ComposerFields.GoalBlock(),
                    new AiDe.Core.Presentation.Composer.AttachmentGate(root, new AiDe.Core.Presentation.Composer.AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max-personal"));

                var header = new Border { Height = 28 };
                var feed = new ThreadFeed { ItemTemplate = TurnTemplate(), ItemContainerStyle = TurnContainerStyle(), ItemsSource = Fixture(40) };
                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                Grid.SetRow(header, 0); Grid.SetRow(feed, 1); Grid.SetRow(composer, 2);
                grid.Children.Add(header); grid.Children.Add(feed); grid.Children.Add(composer);
                if (belt)
                {
                    composer.MaxHeight = Math.Floor(0.45 * 900);   // the document's belt: the composer's share of the surface
                }

                grid.Measure(new Size(1440, 900));
                grid.Arrange(new Rect(0, 0, 1440, 900));
                grid.UpdateLayout();

                var tag = belt ? "q14.belt" : "q14.auto";
                var editor = Logical<Microsoft.Web.WebView2.Wpf.WebView2>(composer).FirstOrDefault();
                var compiled = Logical<TextBox>(composer).FirstOrDefault(t => t.IsReadOnly);
                M($"{tag}.composer_actual_height", Math.Round(composer.ActualHeight, 1));
                M($"{tag}.composer_desired_height", Math.Round(composer.DesiredSize.Height, 1));
                M($"{tag}.editor_host_actual_height", editor is null ? "not found" : (object)Math.Round(editor.ActualHeight, 1));
                M($"{tag}.compiled_actual_height", compiled is null ? "not found" : (object)Math.Round(compiled.ActualHeight, 1));
                M($"{tag}.compiled_max_height", compiled is null ? "not found" : (object)Math.Round(compiled.MaxHeight, 1));
                M($"{tag}.thread_row_actual_height", Math.Round(feed.ActualHeight, 1));
                M($"{tag}.composer_top", Math.Round(composer.TransformToAncestor(grid).Transform(new Point(0, 0)).Y, 1));
                composer.Dispose();
            }
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }

        return 0;
    }

    private sealed class NeverAsked : AiDe.Core.Presentation.Composer.IAttachmentAffirmation
    {
        public bool Confirm(AiDe.Core.Presentation.Composer.OutsideWorkspaceAffirmation affirmation) => false;
    }

    private static IEnumerable<T> Logical<T>(DependencyObject node) where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(node))
        {
            if (child is not DependencyObject element) { continue; }
            if (element is T match) { yield return match; }
            foreach (var found in Logical<T>(element)) { yield return found; }
        }
    }

    private static ListBoxItem FocusedItem(ListBox feed) =>
        Keyboard.FocusedElement as ListBoxItem ?? FocusedItemOrSelected(feed);

    private static ListBoxItem FocusedItemOrSelected(ListBox feed) =>
        Keyboard.FocusedElement as ListBoxItem
        ?? (ListBoxItem?)feed.ItemContainerGenerator.ContainerFromIndex(Math.Max(feed.SelectedIndex, 0))
        ?? throw new InvalidOperationException("no focused or selected container");

    private static void Press(UIElement target, System.Windows.Input.Key key)
    {
        // A KeyDown raised on the focused element with a real PresentationSource: the ListBox's own
        // handlers and the feed's override both run. Modifiers cannot be injected this way, so the
        // Ctrl+ legs call HandleFeedKey directly (recorded as such in RESULT.md).
        var source = PresentationSource.FromVisual(target) ?? throw new InvalidOperationException("target has no PresentationSource");
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key) { RoutedEvent = Keyboard.KeyDownEvent });
    }

    private static void Release(UIElement target, System.Windows.Input.Key key)
    {
        var source = PresentationSource.FromVisual(target) ?? throw new InvalidOperationException("target has no PresentationSource");
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key) { RoutedEvent = Keyboard.KeyUpEvent });
    }

    private static string Describe(IInputElement? element) => element switch
    {
        null => "null",
        ListBoxItem item => $"ListBoxItem[{AutomationProperties.GetName(item)}]",
        FrameworkElement fe when !string.IsNullOrEmpty(fe.Name) => $"{fe.GetType().Name}#{fe.Name}",
        FrameworkElement fe => fe.GetType().Name + (AutomationProperties.GetName(fe) is { Length: > 0 } n ? $"[{n}]" : ""),
        _ => element.GetType().Name,
    };

    private static T? FindChild<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T hit)
            {
                return hit;
            }

            if (FindChild<T>(child) is { } deeper)
            {
                return deeper;
            }
        }

        return null;
    }

    // ── The probe: what a UIA client (a screen reader) sees, from another process ─────────────
    private static int RunProbe()
    {
        var report = new StringBuilder();
        void W(string line = "") { Console.WriteLine(line); report.AppendLine(line); }

        var exe = Environment.ProcessPath!;
        var psi = new ProcessStartInfo(exe, "host") { UseShellExecute = false, RedirectStandardOutput = true };
        using var child = Process.Start(psi)!;
        var hostLines = new List<string>();
        var ready = new ManualResetEventSlim(false);
        child.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) { return; }
            lock (hostLines) { hostLines.Add(e.Data); }
            if (e.Data.StartsWith("HOST READY", StringComparison.Ordinal) || e.Data.StartsWith("HOST ERROR", StringComparison.Ordinal)) { ready.Set(); }
        };
        child.BeginOutputReadLine();

        if (!ready.Wait(TimeSpan.FromSeconds(90)))
        {
            Console.Error.WriteLine("FAIL: the host never reported READY.");
            try { child.Kill(true); } catch { }
            return 2;
        }

        W("SESSION THREAD SPIKE — WPF facts for docs/design/session-thread-itemscontrol.md");
        W(new string('=', 100));
        W($"probed : {DateTime.Now:yyyy-MM-dd HH:mm:ss}  ·  .NET {Environment.Version}  ·  {Environment.OSVersion}");
        W();
        W("── In-process measurements (the host) ──");
        lock (hostLines) { foreach (var l in hostLines) { W(l); } }
        W();

        AutomationElement? window = null;
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline && window is null)
        {
            Thread.Sleep(300);
            window = AutomationElement.RootElement.FindFirst(TreeScope.Children,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ProcessIdProperty, child.Id),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)));
        }

        if (window is null)
        {
            W("PROBE FAIL: the host window never appeared in the UIA tree.");
            try { child.Kill(true); } catch { }
            return 2;
        }

        W("── Out-of-process UIA (what a screen reader's client sees) ──");
        var list = window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.List));
        if (list is null)
        {
            W("PROBE list=NOT FOUND");
        }
        else
        {
            W($"PROBE list.name={list.Current.Name}");
            W($"PROBE list.class={list.Current.ClassName}");
            var items = list.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));
            W($"PROBE list.listitems_visible_to_uia={items.Count}");
            var shown = 0;
            foreach (AutomationElement item in items)
            {
                if (shown++ >= 3) { break; }
                var c = item.Current;
                W($"PROBE item.name={Trim(c.Name)}");
                W($"PROBE item.item_status={Trim(c.ItemStatus)}");
                W($"PROBE item.position_in_set={Try(() => item.GetCurrentPropertyValue(AutomationElement.PositionInSetProperty))}");
                W($"PROBE item.size_of_set={Try(() => item.GetCurrentPropertyValue(AutomationElement.SizeOfSetProperty))}");
                W($"PROBE item.keyboard_focusable={c.IsKeyboardFocusable}");
                W($"PROBE item.patterns={string.Join(",", item.GetSupportedPatterns().Select(p => p.ProgrammaticName.Replace("PatternIdentifiers.Pattern", "")))}");
            }

            // The last item is the current one (focused in the host): the fold and its pattern.
            var lastItem = items.Count > 0 ? items[items.Count - 1] : null;
            var fold = lastItem?.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.IsExpandCollapsePatternAvailableProperty, true));
            if (fold is null)
            {
                W("PROBE fold=NOT FOUND (no ExpandCollapse-capable descendant in the last item)");
            }
            else
            {
                W($"PROBE fold.name={Trim(fold.Current.Name)}");
                W($"PROBE fold.control_type={fold.Current.ControlType.ProgrammaticName}");
                var ec = (ExpandCollapsePattern)fold.GetCurrentPattern(ExpandCollapsePattern.Pattern);
                W($"PROBE fold.state_before={ec.Current.ExpandCollapseState}");
                ec.Expand();
                Thread.Sleep(200);
                W($"PROBE fold.state_after_expand={ec.Current.ExpandCollapseState}");
                ec.Collapse();
                Thread.Sleep(200);
                W($"PROBE fold.state_after_collapse={ec.Current.ExpandCollapseState}");
            }
        }

        foreach (var name in new[] { "Status (polite)", "Status (assertive)" })
        {
            var region = window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, name));
            W(region is null
                ? $"PROBE live[{name}]=NOT FOUND"
                : $"PROBE live[{name}].live_setting={Try(() => region.GetCurrentPropertyValue(AutomationElementIdentifiers.LiveSettingProperty))} control_type={region.Current.ControlType.ProgrammaticName}");
        }

        var focused = Try(() => AutomationElement.FocusedElement?.Current.Name ?? "null");
        W($"PROBE focused.name={Trim(focused)}");

        try { child.Kill(true); } catch { }

        var dir = ProjectDirectory();
        File.WriteAllText(Path.Combine(dir, "RESULT-raw.txt"), report.ToString(), new UTF8Encoding(false));
        W();
        W($"raw report written to {Path.Combine(dir, "RESULT-raw.txt")}");
        return 0;
    }

    private static string Try(Func<object?> read)
    {
        try { return Convert.ToString(read(), CultureInfo.InvariantCulture) ?? "null"; }
        catch (Exception ex) { return "ERR " + ex.GetType().Name; }
    }

    private static string Trim(string? s) => s is null ? "" : s.Length > 90 ? s[..87] + "..." : s;

    private static string ProjectDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.EnumerateFiles("*.csproj").Any())
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? AppContext.BaseDirectory;
    }
}
