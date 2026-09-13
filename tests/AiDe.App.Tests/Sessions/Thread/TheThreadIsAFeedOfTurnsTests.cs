using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Sessions.Thread;

/// <summary>
/// DS-1 <b>K1a · K1b · K5 · K6 · K8 · K9 · K10</b> — the APG feed's keyboard model (SC8) as a pure
/// decision and an act on the composed tree, the region cycle, Escape, the jump list and the tail.
/// </summary>
public sealed class TheThreadIsAFeedOfTurnsTests
{
    /// <summary><b>K1a.</b> <b>Red observed</b>: a decision that returned <c>MoveBy</c> for Up (the first draft's arrows-as-caret) failed the Up row.</summary>
    [Fact]
    public void Decide_ByKeyModifierAndSource_NeverThrows()
    {
        Assert.Equal(new FeedKeyDecision.MoveBy(1), FeedList.Decide(Key.PageDown, ModifierKeys.None, false));
        Assert.Equal(new FeedKeyDecision.MoveBy(-1), FeedList.Decide(Key.PageUp, ModifierKeys.None, false));
        Assert.Equal(new FeedKeyDecision.MoveTo(true), FeedList.Decide(Key.Home, ModifierKeys.None, false));
        Assert.Equal(new FeedKeyDecision.MoveTo(false), FeedList.Decide(Key.End, ModifierKeys.None, false));
        Assert.Equal(new FeedKeyDecision.Scroll(3), FeedList.Decide(Key.Down, ModifierKeys.None, false));
        Assert.Equal(new FeedKeyDecision.Scroll(-3), FeedList.Decide(Key.Up, ModifierKeys.None, false));
        Assert.Equal(new FeedKeyDecision.Leave(FocusLeave.ToEditor), FeedList.Decide(Key.End, ModifierKeys.Control, false));
        Assert.Equal(new FeedKeyDecision.Leave(FocusLeave.ToHeader), FeedList.Decide(Key.Home, ModifierKeys.Control, false));

        // Ctrl+PageUp / PageDown stay the pane switch; a source that owns its keys keeps them —
        // but Ctrl+Home / End leave from an inner control too.
        Assert.Equal(new FeedKeyDecision.None(), FeedList.Decide(Key.PageDown, ModifierKeys.Control, false));
        Assert.Equal(new FeedKeyDecision.None(), FeedList.Decide(Key.PageUp, ModifierKeys.Control, false));
        Assert.Equal(new FeedKeyDecision.None(), FeedList.Decide(Key.PageDown, ModifierKeys.None, true));
        Assert.Equal(new FeedKeyDecision.None(), FeedList.Decide(Key.Down, ModifierKeys.None, true));
        Assert.Equal(new FeedKeyDecision.Leave(FocusLeave.ToEditor), FeedList.Decide(Key.End, ModifierKeys.Control, true));

        // Total over the whole domain (D2): never throws.
        foreach (var key in Enum.GetValues<Key>())
        {
            foreach (var modifiers in new[] { ModifierKeys.None, ModifierKeys.Control, ModifierKeys.Shift, ModifierKeys.Alt, ModifierKeys.Control | ModifierKeys.Shift })
            {
                _ = FeedList.Decide(key, modifiers, false);
                _ = FeedList.Decide(key, modifiers, true);
            }
        }
    }

    [Fact]
    public void SourceOwnsItsKeys_ForAScrollerOrATextBoxInsideTheList_NeverForTheListItself()
    {
        Sta.Run(() =>
        {
            var (feed, _, _) = ThreadFixtures.Feed(ThreadFixtures.Five());
            var window = new Window { Width = 900, Height = 600, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false, Content = feed };
            window.Show();
            try
            {
                Assert.False(feed.SourceOwnsItsKeys(feed));
                Assert.False(feed.SourceOwnsItsKeys(ThreadFixtures.Container(feed, 4)));

                feed.Rows[4].IsCompiledOpen = true;
                feed.UpdateLayout();
                var scroller = ThreadFixtures.Visuals<ScrollViewer>(ThreadFixtures.Container(feed, 4)).First(s => AutomationProperties.GetName(s) == "Compiled prompt of b5");
                Assert.True(feed.SourceOwnsItsKeys(scroller));

                var outside = new ScrollViewer();
                Assert.False(feed.SourceOwnsItsKeys(outside));
            }
            finally
            {
                window.Close();
            }
        });
    }

    /// <summary>
    /// <b>K1b.</b> PageDown from b1 selects AND focuses b2's container; PageUp from b1 stays; PageDown
    /// from b40 stays; Home → b1; End → b40 (the platform lands on b39, Q5c); PageDown with focus on
    /// an inner control is the next turn, never +17; Down on a fold header scrolls, never moves.
    /// </summary>
    [Fact]
    public void FeedKeys_MoveTheCaretAndFocus_OrScroll_ByIndexNeverByGeometry()
    {
        Sta.Pump(
            create: () => ThreadFixtures.Feed(ThreadFixtures.Forty()).Feed,
            configure: window => { window.Width = 900; window.Height = 600; },
            body: (window, feed) =>
            {
                // Row 1, the harness sanity row: after Focus() the focused element is the container.
                Assert.True(feed.FocusItem(0));
                Assert.Same(feed.ItemContainerGenerator.ContainerFromIndex(0), Keyboard.FocusedElement);

                Press(feed, Key.PageDown);
                Assert.Equal(1, feed.SelectedIndex);
                var second = (ListBoxItem)feed.ItemContainerGenerator.ContainerFromIndex(1)!;
                Assert.Same(second, Keyboard.FocusedElement);
                Assert.Same(feed.Rows[1], second.DataContext);

                Press(feed, Key.PageUp);
                Press(feed, Key.PageUp);
                Assert.Equal(0, feed.SelectedIndex);

                Press(feed, Key.End);
                Assert.Equal(39, feed.SelectedIndex);
                Assert.Same(feed.ItemContainerGenerator.ContainerFromIndex(39), Keyboard.FocusedElement);

                Press(feed, Key.PageDown);
                Assert.Equal(39, feed.SelectedIndex);

                Press(feed, Key.Home);
                Assert.Equal(0, feed.SelectedIndex);

                // From an inner control (the fold header) PageDown is the next turn, never +17.
                var header = ThreadFixtures.HeaderToggle(ThreadFixtures.Fold(ThreadFixtures.Container(feed, 0)));
                Assert.True(header.Focus());
                Press(header, Key.PageDown);
                Assert.Equal(1, feed.SelectedIndex);
                Assert.Same(feed.ItemContainerGenerator.ContainerFromIndex(1), Keyboard.FocusedElement);

                // Down from a fold header is a scroll of three lines, not a move. The offset is read
                // AFTER the header takes focus: focusing it brings it into view, and b2's fold sits
                // below twenty-seven tool items since Ruling 82.
                var scroller = ThreadFixtures.Visuals<ScrollViewer>(feed).First();
                var headerOn2 = ThreadFixtures.HeaderToggle(ThreadFixtures.Fold((ListBoxItem)feed.ItemContainerGenerator.ContainerFromIndex(1)!));
                Assert.True(headerOn2.Focus());
                // Settled: the bring-into-view's scroll and the pixel-virtualized panel's re-anchoring
                // both complete at Background before the offset is read (the Test Architect's 7a).
                feed.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });
                feed.UpdateLayout();
                var before = scroller.VerticalOffset;
                Press(headerOn2, Key.Down);
                feed.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });
                feed.UpdateLayout();
                Assert.Equal(1, feed.SelectedIndex);
                Assert.Equal(before + 3 * FeedList.LineHeight, scroller.VerticalOffset, 1.0);

                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// <b>K5.</b> Tab from the caret's turn reaches exactly its stops — provenance ▸ · compiled
    /// prompt ▸ · the fold — then leaves the feed; a non-focusable Expander counts once (Q11); Shift+Tab
    /// from the composer's stand-in lands on the caret's last stop; Tab from the header with the
    /// caret unrealized lands on the caret's container.
    /// </summary>
    [Fact]
    public void Tab_FromTheCaretsTurn_ReachesExactlyItsStops_ThenLeavesToTheComposer()
    {
        Sta.Pump(
            create: () => ThreadFixtures.Feed(ThreadFixtures.Forty()).Feed,
            content: feed =>
            {
                var header = new Button { Content = "header", Name = "header" };
                var composer = new TextBox { Name = "composer", AcceptsReturn = true };
                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                Grid.SetRow(header, 0);
                Grid.SetRow(feed, 1);
                Grid.SetRow(composer, 2);
                grid.Children.Add(header);
                grid.Children.Add(feed);
                grid.Children.Add(composer);
                return grid;
            },
            configure: window => { window.Width = 900; window.Height = 600; },
            body: (window, feed) =>
            {
                var grid = (Grid)window.Content;
                var header = (Button)grid.Children[0];
                var composer = (TextBox)grid.Children[2];

                // Entry lands on the caret's container (the document routes Tab from its header —
                // the K6 test; here the feed's own entry, F6's target).
                feed.SelectedIndex = 0;
                Assert.True(header.Focus());
                Assert.True(feed.FocusCurrentItem());
                var container = feed.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                Assert.NotNull(container);
                Assert.Same(container, Keyboard.FocusedElement);

                // Tab walks exactly the turn's stops, then leaves to the composer.
                var stops = new List<string>();
                for (var i = 0; i < 8; i++)
                {
                    ((UIElement)Keyboard.FocusedElement).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    var focused = Keyboard.FocusedElement as DependencyObject;
                    if (ReferenceEquals(focused, composer))
                    {
                        break;
                    }

                    stops.Add(AutomationProperties.GetName(focused!));
                }

                // b1's three events are accepted · the answer · completed; the answer is conversation, so the fold holds 2 (Ruling 82).
                Assert.Equal(["Provenance of b1", "Compiled prompt of b1", "2 events"], stops);
                Assert.Same(composer, Keyboard.FocusedElement);

                // Shift+Tab from the composer's stand-in lands on the caret's last stop.
                Assert.True(feed.FocusCurrentItemLast());
                Assert.Equal("2 events", AutomationProperties.GetName((DependencyObject)Keyboard.FocusedElement));

                // With provenance open, "Use as the next draft" is one more stop; with the compiled
                // prompt open, its scroller is a named stop and PageDown scrolls it.
                feed.Rows[0].IsProvenanceOpen = true;
                feed.Rows[0].IsCompiledOpen = true;
                feed.UpdateLayout();
                Assert.True(feed.FocusItem(0));
                var names = new List<string>();
                for (var i = 0; i < 10; i++)
                {
                    ((UIElement)Keyboard.FocusedElement).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    var focused = (DependencyObject)Keyboard.FocusedElement;
                    if (ReferenceEquals(focused, composer))
                    {
                        break;
                    }

                    names.Add(AutomationProperties.GetName(focused) is { Length: > 0 } n ? n : (focused as ContentControl)?.Content as string ?? focused.GetType().Name);
                }

                Assert.Equal(["Provenance of b1", "Use as the next draft", "Compiled prompt of b1", "Compiled prompt of b1", "2 events"], names);
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// <b>K6.</b> F6 cycles header → thread → composer → header, the split only when open; an empty
    /// thread lands on the outside text (<i>Nothing has run yet.</i>); while not caught up it reads
    /// <i>Restoring …</i>; a refused region announces and records <c>THR-0002</c>.
    /// </summary>
    [Fact]
    public void F6_CyclesHeaderThreadComposer_TheSplitOnlyWhenOpen()
    {
        Sta.Pump(
            create: () => new SessionDocumentSurface(new SessionDocumentViewModel("20260912T140000Z-f6", "f6", Path.GetTempPath(), ["console"]), null, new RecordingAnnouncer()),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: (window, document) =>
            {
                // Empty: the thread region is the outside text.
                Assert.Equal(SessionDocumentSurface.Region.Header, document.FocusRegion(SessionDocumentSurface.Region.Header).Succeeded ? document.CurrentRegion : null);
                var toThread = document.CycleRegion(+1);
                Assert.Equal(CanvasFocusOutcome.Entered, toThread.Outcome);
                Assert.Equal(SessionDocumentSurface.Region.Thread, document.CurrentRegion);
                Assert.StartsWith("Nothing has run yet.", document.OutsideText, StringComparison.Ordinal);

                // The composer's page is not up in a headless run: its first WPF stop (a structure line) is the region.
                var toComposer = document.CycleRegion(+1);
                Assert.Equal(CanvasFocusOutcome.Entered, toComposer.Outcome);
                Assert.Equal(SessionDocumentSurface.Region.Composer, document.CurrentRegion);

                // Closed split: the next region is the header, never the split.
                Assert.Equal(SessionDocumentSurface.Region.Header, document.CycleRegion(+1).Succeeded ? document.CurrentRegion : null);

                // With turns and the split open: header → thread (the caret's container) → composer → split → header.
                var ordinal = document.ReadModel.Accept("first", ThreadFixtures.Decorations("free-form", "T0", null, "message"), "bytes", ThreadFixtures.T0);
                document.ReadModel.Append(ordinal, ThreadFixtures.Line(1, "claude-code", "hello"));
                document.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });
                document.OpenSplit();
                document.UpdateLayout();

                // Forty more, the caret on b1 while the viewport sits at the end (unrealized): Tab
                // from the header's last stop lands on b1's container — the document's rule (K5).
                foreach (var turn in ThreadFixtures.Forty())
                {
                    var o = document.ReadModel.Accept(turn.SourceText, turn.Decorations, turn.SentBytes, turn.At);
                    document.ReadModel.Conclude(o, TurnState.Completed, turn.At.AddSeconds(5), null, 1);
                }

                document.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });
                document.Thread.SelectedIndex = 0;
                document.Thread.ScrollIntoView(document.Thread.Items[^1]);
                document.UpdateLayout();
                Assert.Null(document.Thread.ItemContainerGenerator.ContainerFromIndex(0));
                Assert.True(document.JumpRows.Items.Count == 41);
                var jump = ThreadFixtures.Visuals<Button>(document).First(b => AutomationProperties.GetName(b).EndsWith("jump to a turn", StringComparison.Ordinal));
                Assert.True(jump.Focus());
                Press(jump, Key.Tab);
                var entered = Keyboard.FocusedElement as ListBoxItem;
                Assert.NotNull(entered);
                Assert.Same(document.Thread.Rows[0], entered!.DataContext);

                Assert.Equal(SessionDocumentSurface.Region.Header, document.FocusRegion(SessionDocumentSurface.Region.Header).Succeeded ? document.CurrentRegion : null);
                document.CycleRegion(+1);
                Assert.Equal(SessionDocumentSurface.Region.Thread, document.CurrentRegion);
                Assert.IsType<ListBoxItem>(Keyboard.FocusedElement);
                document.CycleRegion(+1);
                Assert.Equal(SessionDocumentSurface.Region.Composer, document.CurrentRegion);
                document.CycleRegion(+1);
                Assert.Equal(SessionDocumentSurface.Region.Split, document.CurrentRegion);
                document.CycleRegion(+1);
                Assert.Equal(SessionDocumentSurface.Region.Header, document.CurrentRegion);
                document.CycleRegion(-1);
                Assert.Equal(SessionDocumentSurface.Region.Split, document.CurrentRegion);

                // A refusal is announced (THR-0002 on the record): the split closed under focus.
                var lines = new List<string>();
                var previous = WorkbenchDiagnostics.Sink;
                WorkbenchDiagnostics.Sink = lines.Add;
                try
                {
                    document.CloseSplit();
                    var refused = document.FocusRegion(SessionDocumentSurface.Region.Split);
                    Assert.Equal(CanvasFocusOutcome.Refused, refused.Outcome);
                    Assert.Equal("Couldn't reach the Console.", refused.Announcement);
                    Assert.Contains(lines, l => l.Contains("\"error_code\":\"THR-0002\"", StringComparison.Ordinal));
                }
                finally
                {
                    WorkbenchDiagnostics.Sink = previous;
                }

                // Not caught up: the outside text reads Restoring…, never Nothing has run yet.
                var restoring = new SessionDocumentSurface(new SessionDocumentViewModel("20260912T140000Z-r", "payments", Path.GetTempPath(), ["console"]), null, new RecordingAnnouncer());
                var field = typeof(SessionDocumentSurface).GetField("_thread", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                var uncaught = new RunChannelSessionThread(caughtUp: false);
                Assert.False(uncaught.Current.IsCaughtUp);
                typeof(SessionDocumentSurface).GetMethod("RenderHeader", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(restoring, [uncaught.Current]);
                Assert.Equal("Restoring payments…", restoring.OutsideText);
                _ = field;
                restoring.Dispose();

                return Task.CompletedTask;
            });
    }

    /// <summary><b>K8.</b> Escape closes the disclosure whose subtree holds focus via its header; two open, focus in the second → only the second closes; from the container nothing closes; never leaves the document.</summary>
    [Fact]
    public void Escape_ClosesTheDisclosureHoldingFocus_ViaItsHeader_AndNeverLeavesTheDocument()
    {
        Sta.Pump(
            create: () => ThreadFixtures.Feed(ThreadFixtures.Five()).Feed,
            configure: window => { window.Width = 900; window.Height = 600; },
            body: (window, feed) =>
            {
                var row = feed.Rows[1];
                row.IsProvenanceOpen = true;
                row.IsCompiledOpen = true;
                feed.UpdateLayout();

                var container = ThreadFixtures.Container(feed, 1);
                var scroller = ThreadFixtures.Visuals<ScrollViewer>(container).First(s => AutomationProperties.GetName(s) == "Compiled prompt of b2");
                Assert.True(scroller.Focus());

                Press(scroller, Key.Escape);
                Assert.False(row.IsCompiledOpen);
                Assert.True(row.IsProvenanceOpen);

                // Focus moved to the header, never to the root.
                var focused = Keyboard.FocusedElement as DependencyObject;
                Assert.NotNull(focused);
                Assert.Equal("Compiled prompt of b2", AutomationProperties.GetName(focused!));

                // From the container: nothing closes, focus stays.
                Assert.True(container.Focus());
                Press(container, Key.Escape);
                Assert.True(row.IsProvenanceOpen);
                Assert.Same(container, Keyboard.FocusedElement);
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// <b>K9.</b> The jump list is a view of Turns; type-ahead on the display ordinal (typing
    /// <c>17</c> never selects b17 — the negative row); Escape closes it and returns focus to the
    /// button; Enter focuses the turn's CONTAINER; the button is enabled at 0 turns and says so.
    /// </summary>
    [Fact]
    public void TheJumpList_TypesAheadOnTheDisplayOrdinal_EnterFocusesTheContainer()
    {
        Sta.Pump(
            create: () =>
            {
                var document = new SessionDocumentSurface(new SessionDocumentViewModel("20260912T140000Z-jump", "jump", Path.GetTempPath(), ["console"]), null, new RecordingAnnouncer());
                foreach (var turn in ThreadFixtures.Forty())
                {
                    var o = document.ReadModel.Accept(turn.SourceText, turn.Decorations, turn.SentBytes, turn.At);
                    document.ReadModel.Conclude(o, turn.State, turn.At.AddSeconds(30), turn.Outcome?.ExitCode, turn.Outcome?.Edits);
                }

                return document;
            },
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: (window, document) =>
            {
                document.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });
                Assert.Equal("40 turns", document.TurnCountCaption);
                Assert.Equal(40, document.JumpRows.Items.Count);

                // The rows are a view of Turns: ordinal · the words · the outcome word; b17's says lane exited 1.
                var b17 = (SessionDocumentSurface.JumpRow)document.JumpRows.Items[16];
                Assert.Equal("b17", b17.DisplayOrdinal);
                Assert.Equal("lane exited 1", b17.Outcome);
                Assert.StartsWith("b17, ", b17.ToString(), StringComparison.Ordinal);
                Assert.EndsWith(", lane exited 1", b17.ToString(), StringComparison.Ordinal);

                document.JumpList.IsOpen = true;
                document.UpdateLayout();
                Assert.Equal(39, document.JumpRows.SelectedIndex);

                // The negative row: typing "17" (no prefix) is not a display ordinal — b17 is NOT selected.
                var list = document.JumpRows;
                void Type(string text)
                {
                    foreach (var ch in text)
                    {
                        list.RaiseEvent(new TextCompositionEventArgs(InputManager.Current.PrimaryKeyboardDevice, new TextComposition(InputManager.Current, list, ch.ToString())) { RoutedEvent = UIElement.TextInputEvent });
                    }
                }

                Type("17");
                Assert.NotEqual(16, list.SelectedIndex);

                // Escape closes the list and returns focus to the button — the exit is the entry, reversed.
                var jumpButton = ThreadFixtures.Visuals<Button>(document).Single(b => AutomationProperties.GetName(b).EndsWith("jump to a turn", StringComparison.Ordinal));
                list.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(list)!, 0, Key.Escape) { RoutedEvent = UIElement.PreviewKeyDownEvent });
                Assert.False(document.JumpList.IsOpen);
                Assert.Same(jumpButton, Keyboard.FocusedElement);

                document.JumpList.IsOpen = true;
                document.UpdateLayout();

                // Type-ahead on the DISPLAY ordinal: "b17" selects b17.
                Type("b17");
                Assert.Equal(16, list.SelectedIndex);

                // Enter focuses the turn's container, never an action.
                list.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(list)!, 0, Key.Enter) { RoutedEvent = UIElement.PreviewKeyDownEvent });
                Assert.False(document.JumpList.IsOpen);
                Assert.Equal(16, document.Thread.SelectedIndex);
                var focused = Keyboard.FocusedElement as ListBoxItem;
                Assert.NotNull(focused);
                Assert.Same(document.Thread.Rows[16], focused!.DataContext);

                // At 0 turns the button stays enabled and says so.
                var empty = new SessionDocumentSurface(new SessionDocumentViewModel("20260912T140000Z-e", "e", Path.GetTempPath(), ["console"]), null, new RecordingAnnouncer());
                Assert.Equal("no turns yet", empty.TurnCountCaption);
                empty.Dispose();
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// <b>K9's twin (SC8; Ruling 77).</b> The refused gesture's reason names the turn as a LINK:
    /// activating <i>b1, running</i> focuses b1's container — never an action on it — and the
    /// refusal was spoken assertively through the document's one announcer.
    /// </summary>
    [Fact]
    public void TheRunningLinkInTheRefusal_FocusesTheContainer_NeverAnAction()
    {
        Sta.Pump(
            create: () =>
            {
                var document = new SessionDocumentSurface(new SessionDocumentViewModel("20260912T140000Z-link", "link", Path.GetTempPath(), ["console"]), null, new RecordingAnnouncer());
                var running = ThreadFixtures.Running(1, 3);
                var o = document.ReadModel.Accept(running.SourceText, running.Decorations, running.SentBytes, running.At);
                foreach (var line in running.Events)
                {
                    document.ReadModel.Append(o, line);
                }

                return document;
            },
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: (window, document) =>
            {
                document.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });
                var announcer = (RecordingAnnouncer)typeof(SessionDocumentSurface).GetField("_announcer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(document)!;
                var before = announcer.Announcements.Count;

                Assert.Null(document.Composer.Send());
                Assert.Equal("b1 is running; the next turn waits for it.", document.Composer.Status);

                // Spoken, assertively, through the document's announcer — never a silent status line.
                var spoken = announcer.Announcements[^1];
                Assert.Equal(before + 1, announcer.Announcements.Count);
                Assert.Equal("b1 is running; the next turn waits for it.", spoken.Text);
                Assert.Equal(Urgency.Assertive, spoken.Urgency);

                // The ordinal is a link named for the turn; activating it lands on the CONTAINER.
                var status = ThreadFixtures.Visuals<TextBlock>(document.Composer).Single(t => AutomationProperties.GetName(t) == "Send status");
                var link = Assert.Single(status.Inlines.OfType<System.Windows.Documents.Hyperlink>());
                Assert.Equal("b1, running", AutomationProperties.GetName(link));
                link.RaiseEvent(new RoutedEventArgs(System.Windows.Documents.Hyperlink.ClickEvent));
                document.UpdateLayout();

                var focused = Keyboard.FocusedElement as ListBoxItem;
                Assert.NotNull(focused);
                Assert.Same(document.Thread.Rows[0], focused!.DataContext);
                Assert.Equal(0, document.Thread.SelectedIndex);
                Assert.DoesNotContain(ThreadFixtures.Visuals<Button>(focused), b => b.IsKeyboardFocused);
                return Task.CompletedTask;
            });
    }

    /// <summary><b>K10.</b> The fold's tail opens the split at the turn's heading, focused; the Console toggle announces; the split is a flat list of ListItems; PageDown moves by row.</summary>
    [Fact]
    public void TheTailButton_OpensTheSplitAtTheTurnsHeading_AndTheSplitIsAFlatListOfRows()
    {
        Sta.Pump(
            create: () =>
            {
                var document = new SessionDocumentSurface(new SessionDocumentViewModel("20260912T140000Z-tail", "tail", Path.GetTempPath(), ["console"]), null, new RecordingAnnouncer());
                foreach (var turn in ThreadFixtures.Five())
                {
                    var o = document.ReadModel.Accept(turn.SourceText, turn.Decorations, turn.SentBytes, turn.At);
                    foreach (var line in turn.Events)
                    {
                        document.ReadModel.Append(o, line);
                    }

                    document.ReadModel.Conclude(o, turn.State, turn.At.AddSeconds(30), turn.Outcome?.ExitCode, turn.Outcome?.Edits);
                }

                return document;
            },
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: (window, document) =>
            {
                document.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });
                var feed = document.Thread;
                feed.Rows[1].IsFoldOpen = true;
                feed.UpdateLayout();
                var container = ThreadFixtures.Container(feed, 1);
                var tail = ThreadFixtures.Visuals<Button>(container).First(b => b.Content is string s && s.StartsWith("the other", StringComparison.Ordinal));
                // b2's 139 working lines: 27 tool.call lines are items (Ruling 82); the 112 tool.result lines with no call fold, four shown.
                Assert.Equal("the other 108, in the Console", tail.Content);

                var announcer = (RecordingAnnouncer)typeof(SessionDocumentSurface).GetField("_announcer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(document)!;
                tail.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                document.UpdateLayout();

                Assert.True(document.IsSplitOpen);
                Assert.Equal(2, document.Split.At);
                Assert.Equal("Console open, at b2.", announcer.Last);

                var heading = Keyboard.FocusedElement as ListBoxItem;
                Assert.NotNull(heading);
                Assert.IsType<ConsoleSplitRow.TurnHeading>(heading!.DataContext);
                Assert.Equal(2, ((ConsoleSplitRow)heading.DataContext).Ordinal);

                // PageDown moves by one row (the first line), Home the first heading.
                Press(heading, Key.PageDown);
                Assert.IsType<ConsoleSplitRow.Line>(((ListBoxItem)Keyboard.FocusedElement).DataContext);
                Assert.Equal(document.Split.SelectedIndex, document.Split.Rows.TakeWhile(r => r is not ConsoleSplitRow.TurnHeading { Ordinal: 2 }).Count() + 1);
                return Task.CompletedTask;
            });
    }

    internal static void Press(UIElement target, Key key)
    {
        var source = PresentationSource.FromVisual(target) ?? throw new InvalidOperationException("no presentation source");
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key) { RoutedEvent = UIElement.PreviewKeyDownEvent });
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key) { RoutedEvent = UIElement.KeyDownEvent });
    }
}
