using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Sessions.Thread;

/// <summary>
/// DS-1 <b>A2 · A3 · A4 · U1 · U2 · U5 · M1 · C1 · C2 · S1 · T1 · Z1</b> — the notification mapping
/// and the raise seam, what is never announced, the actions' focus rule, the UIA contract, the
/// one-list identity, the off-thread channel, the stopped feed, model text as text, the records
/// without text, and the layers.
/// </summary>
public sealed class TheThreadAnnouncesAndExposesRealPropertiesTests
{
    /// <summary><b>A2.</b> <b>Red observed</b>: the first draft's mapping was inverted (Status → MostRecent, Assertive → All) — the fetched NVDA source says only MostRecent / ImportantMostRecent cancel speech.</summary>
    [Fact]
    public void NotificationMapping_QueuesStatuses_InterruptsErrors_AndTheAnnouncerRaisesWhatItMaps()
    {
        Assert.Equal((AutomationNotificationKind.ItemAdded, AutomationNotificationProcessing.All), NotificationMapping.For(Urgency.Status, AnnouncementKind.ItemAdded));
        Assert.Equal((AutomationNotificationKind.ActionCompleted, AutomationNotificationProcessing.All), NotificationMapping.For(Urgency.Status, AnnouncementKind.Completed));
        Assert.Equal((AutomationNotificationKind.ActionAborted, AutomationNotificationProcessing.ImportantMostRecent), NotificationMapping.For(Urgency.Assertive, AnnouncementKind.Aborted));
        Assert.Equal((AutomationNotificationKind.Other, AutomationNotificationProcessing.ImportantMostRecent), NotificationMapping.For(Urgency.Assertive, AnnouncementKind.Other));

        Sta.Pump(
            create: () => new TextBlock(),
            body: (window, region) =>
            {
                var announcer = new WorkbenchAnnouncer(region);
                announcer.Announce(new Announcement("Turn b5: the lane exited 1.", Urgency.Assertive, AnnouncementKind.Aborted, 5, "running→failed"));

                Assert.Equal("Turn b5: the lane exited 1.", announcer.Last);
                Assert.Equal((AutomationNotificationKind.ActionAborted, AutomationNotificationProcessing.ImportantMostRecent, NotificationMapping.ThreadActivityId), announcer.LastRaise);

                announcer.Announce(new Announcement("Turn b2 completed: 3 edits.", Urgency.Status, AnnouncementKind.Completed, 2, "running→completed"));
                Assert.Equal((AutomationNotificationKind.ActionCompleted, AutomationNotificationProcessing.All, NotificationMapping.ThreadActivityId), announcer.LastRaise);
                return Task.CompletedTask;
            });
    }

    /// <summary><b>A3.</b> 200 lines, a fold toggle and a PageDown announce nothing; the outcome after them announces exactly once (the positive control).</summary>
    [Fact]
    public void EventLines_TheReply_FoldsAndFocusMoves_AreNeverAnnounced_ButTheOutcomeIs()
    {
        Sta.Pump(
            create: () => ThreadFixtures.Feed(ThreadFixtures.Five().Append(ThreadFixtures.Running(6, 0)).ToList()).Feed,
            configure: window => { window.Width = 900; window.Height = 600; },
            body: async (window, feed) =>
            {
                var thread = ReadModel(feed);
                var announcer = Announcer(feed);

                for (var i = 1; i <= 200; i++)
                {
                    thread.Append(6, ThreadFixtures.Line(i, "claude-code", $"line {i}"));
                }

                await PumpAsync(window);
                feed.Rows[0].IsFoldOpen = true;
                feed.FocusItem(1);
                await PumpAsync(window);

                Assert.Empty(announcer.Announcements);

                thread.Conclude(6, TurnState.Completed, ThreadFixtures.T0.AddMinutes(10), null, 2);
                await PumpAsync(window);

                var one = Assert.Single(announcer.Announcements);
                Assert.Equal(6, one.Ordinal);
                Assert.Equal("running→completed", one.Transition);
                Assert.Equal(Urgency.Status, one.Urgency);
            });
    }

    /// <summary>
    /// <b>A4.</b> A turn's actions: never <c>IsDefault</c>; focus moves to the turn's CONTAINER
    /// before the act, so the merge that removes the button never drops focus to the window; Send
    /// again and Use as the next draft end with a leave to the editor.
    /// </summary>
    [Fact]
    public void AnActionKeepsFocusOnTheTurn_NeverIsDefault_AndSendAgainLeavesToTheEditor()
    {
        Sta.Pump(
            create: () => ThreadFixtures.Feed(ThreadFixtures.Five().Append(ThreadFixtures.Running(6, 2)).ToList()).Feed,
            configure: window => { window.Width = 900; window.Height = 600; },
            body: async (window, feed) =>
            {
                var thread = ReadModel(feed);
                var actions = new List<TurnAction>();
                var leaves = new List<FocusLeave>();
                feed.TurnActionRequested += actions.Add;
                feed.FocusLeaveRequested += leaves.Add;

                var container = ThreadFixtures.Container(feed, 5);
                var stop = ThreadFixtures.Visuals<Button>(container).Single(b => b.Content is "Stop this turn");
                Assert.False(stop.IsDefault);
                Assert.Equal("edits so far stay on disk and are listed on the turn", AutomationProperties.GetHelpText(stop));
                Assert.True(stop.ActualHeight >= 24 && stop.ActualWidth >= 24);

                Assert.True(stop.Focus());
                stop.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                // The act was requested; the run host concludes the turn; the merge removes the button.
                Assert.Equal(new TurnAction(6, TurnActionKind.Stop, null), Assert.Single(actions));
                thread.Conclude(6, TurnState.Stopped, ThreadFixtures.T0.AddMinutes(10), null, 1);
                await PumpAsync(window);
                await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);

                var focused = Keyboard.FocusedElement as DependencyObject;
                Assert.NotNull(focused);
                Assert.False(ReferenceEquals(focused, window), "focus dropped to the window");
                Assert.True(ReferenceEquals(focused, feed.ItemContainerGenerator.ContainerFromIndex(5)) || IsInside(focused!, feed), "focus left the turn");

                // The stopped last turn offers Send again · Open the log; Send again ends at the editor.
                container = ThreadFixtures.Container(feed, 5);
                var sendAgain = ThreadFixtures.Visuals<Button>(container).Single(b => b.Content is "Send again as a new turn");
                sendAgain.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                Assert.Equal(TurnActionKind.SendAgain, actions[^1].Kind);
                Assert.Equal([FocusLeave.ToEditor], leaves);
            });
    }

    /// <summary>
    /// <b>U1 · U2 · U5.</b> The feed is a <c>List</c>, each turn a <c>ListItem</c> reached through the
    /// list peer's children with real positions, named <i>b2, Refactor…</i> (≤ 120 chars + …), its
    /// words in the Control view; <c>ItemStatus</c> is the decoration line and <c>HelpText</c> the
    /// reason sentence; every disclosure is named for its turn; every focusable is named; targets ≥ 24 px.
    /// </summary>
    [Fact]
    public void TheFeedIsAList_EachTurnAListItem_PositionsAreReal_AndItsTextIsInTheControlView()
    {
        Sta.Pump(
            create: () => ThreadFixtures.Feed(ThreadFixtures.Forty()).Feed,
            configure: window => { window.Width = 1200; window.Height = 700; },
            body: (window, feed) =>
            {
                feed.ScrollIntoView(feed.Items[^1]);
                feed.UpdateLayout();

                var peer = UIElementAutomationPeer.CreatePeerForElement(feed);
                Assert.Equal(AutomationControlType.List, peer.GetAutomationControlType());
                Assert.Equal("Conversation", peer.GetName());
                Assert.Equal("live", peer.GetItemStatus());
                Assert.Equal("Page Down and Page Up move between turns; Up and Down scroll", peer.GetHelpText());

                // Row 1, the harness sanity row: the item peers come from the LIST peer's children.
                var items = peer.GetChildren().Where(c => c.GetAutomationControlType() == AutomationControlType.ListItem).ToList();
                Assert.Equal(feed.RealizedContainers, items.Count);

                var last = items.Last();
                Assert.StartsWith("b40, Write this session", last.GetName(), StringComparison.Ordinal);
                Assert.Equal(40, last.GetPositionInSet());
                Assert.Equal(40, last.GetSizeOfSet());
                Assert.Equal(TurnCopy.DecorationLine(feed.Rows[39].View.Decorations), last.GetItemStatus());
                Assert.Equal(string.Empty, last.GetHelpText());

                // The words, the decoration values, the outcome word and the reply are Control-view descendants.
                var controlView = Descendants(last).Where(p => p.IsControlElement()).Select(p => p.GetName()).ToList();
                Assert.Contains(controlView, n => n.StartsWith("Write this session", StringComparison.Ordinal));
                Assert.Contains("completed", controlView);
                Assert.Contains(controlView, n => n.StartsWith("Wrote docs/proof", StringComparison.Ordinal));

                // A failed past turn (b17): HelpText is the full reason sentence; the name is truncated with …
                feed.FocusItem(16);
                peer.ResetChildrenCache();
                var b17 = peer.GetChildren()
                    .First(c => c.GetAutomationControlType() == AutomationControlType.ListItem && c.GetName().StartsWith("b17,", StringComparison.Ordinal));
                Assert.Equal("The lane exited 1 after 0 edits. Send the same turn again as a new turn, or open the log.", b17.GetHelpText());
                Assert.Equal(17, b17.GetPositionInSet());

                // U5: every disclosure named for its turn; every focusable named; every target ≥ 24 px.
                var container = ThreadFixtures.Container(feed, 16);
                foreach (var expander in ThreadFixtures.Visuals<Expander>(container))
                {
                    var name = AutomationProperties.GetName(expander);
                    Assert.True(name.EndsWith("of b17", StringComparison.Ordinal) || name.EndsWith("events", StringComparison.Ordinal), $"a disclosure named '{name}'");
                    Assert.False(expander.Focusable);
                }

                foreach (var focusable in ThreadFixtures.Visuals<UIElement>(container).Where(e => e.Focusable && e.IsVisible && KeyboardNavigation.GetIsTabStop(e)))
                {
                    var name = AutomationProperties.GetName(focusable) is { Length: > 0 } n ? n : (focusable as ContentControl)?.Content as string;
                    Assert.False(string.IsNullOrEmpty(name), $"a focusable {focusable.GetType().Name} with no name");
                    Assert.True(focusable.RenderSize.Height >= 24 && focusable.RenderSize.Width >= 24, $"{name} is {focusable.RenderSize}");
                }

                return Task.CompletedTask;
            });
    }

    /// <summary><b>M1</b> (Ruling 74 condition 1). The split's rows, the jump list and the header's count are identities over <c>Turns</c> — never an equation between two sources.</summary>
    [Fact]
    public void TheSplit_TheJumpList_AndTheHeader_AreViewsOfTurns()
    {
        Sta.Run(() =>
        {
            var turns = ThreadFixtures.Five();
            var split = new ConsoleSurface();
            split.Show(turns);

            var expected = turns.SelectMany(t => new[] { $"b{t.Ordinal}" }.Concat(t.Events.Select(e => e.Text))).ToList();
            var actual = split.Rows.Select(r => r is ConsoleSplitRow.TurnHeading h ? $"b{h.Ordinal}" : ((ConsoleSplitRow.Line)r).Text).ToList();
            Assert.Equal(expected, actual);
            Assert.Equal(turns.Sum(t => t.Events.Count) + turns.Count, split.Rows.Count);

            // Grouped by turn, in order — never interleaved by time across turns.
            var ordinals = split.Rows.Select(r => r.Ordinal).ToList();
            Assert.Equal(ordinals.Order(), ordinals);

            Assert.Equal("59,460 tokens this session · bounded by your subscription", TurnCopy.SessionSpend(turns, null));
        });
    }

    /// <summary>
    /// <b>C1.</b> 500 snapshots raised off the UI thread, alternating running ↔ waiting with fresh
    /// request ids: exactly 500 announcements in order (the policy never coalesces), far fewer
    /// applies than raises (the render does), the caret and a focused fold survive, and the
    /// announcer is called after the render.
    /// </summary>
    [Fact]
    public void SnapshotsRaisedOffThread_AreAppliedInVersionOrder_TheRenderCoalesces_ThePolicyDoesNot_AndTheCaretSurvives()
    {
        Sta.Pump(
            create: () => ThreadFixtures.Feed(ThreadFixtures.Five().Append(ThreadFixtures.Running(6, 1)).ToList()).Feed,
            configure: window => { window.Width = 900; window.Height = 600; },
            body: async (window, feed) =>
            {
                var thread = ReadModel(feed);
                var announcer = Announcer(feed);
                feed.FocusItem(5);
                var header = ThreadFixtures.HeaderToggle(ThreadFixtures.Fold(ThreadFixtures.Container(feed, 5)));
                Assert.True(header.Focus());
                var appliesBefore = feed.Applies;

                await Task.Run(() =>
                {
                    for (var i = 1; i <= 250; i++)
                    {
                        thread.Wait(6, new WaitingRequest($"r{i}", "permission", $"asks to write outside the scope, request {i}.", [TurnActionKind.Deny, TurnActionKind.AllowOnce]));
                        thread.Resume(6);
                    }
                });

                for (var i = 0; i < 20 && announcer.Announcements.Count < 500; i++)
                {
                    await PumpAsync(window);
                }

                Assert.Equal(500, announcer.Announcements.Count);
                Assert.Equal(["running→waiting", "waiting→running"], announcer.Announcements.Take(2).Select(a => a.Transition));
                Assert.All(announcer.Announcements.Where((_, i) => i % 2 == 0), a => Assert.Equal(Urgency.Assertive, a.Urgency));
                Assert.True(feed.Applies - appliesBefore < 500, $"the render ran {feed.Applies - appliesBefore} times for 500 raises");

                Assert.Equal(5, feed.SelectedIndex);
                Assert.Same(header, Keyboard.FocusedElement);
                Assert.Same(feed.Rows[5], ThreadFixtures.Container(feed, 5).DataContext);
                Assert.Equal(TurnState.Running, feed.Rows[5].View.State);

                // Dispose between a raise and the pump: Apply runs on nothing.
                thread.Append(6, ThreadFixtures.Line(9, "claude-code", "late"));
                feed.Dispose();
                await PumpAsync(window);
                Assert.Equal(500, announcer.Announcements.Count);
            });
    }

    /// <summary><b>C2.</b> A throwing apply logs THR-0001, shows the row outside the scroller, announces once (assertive), and the channel survives for a second subscriber.</summary>
    [Fact]
    public void AThrowingApply_LogsTHR0001_ShowsTheRowOutsideTheScroller_AnnouncesOnce_AndTheChannelSurvives()
    {
        Sta.Pump(
            create: () => new SessionDocumentSurface(new SessionDocumentViewModel("20260912T140000Z-thr", "thr", Path.GetTempPath(), ["console"]), null, new RecordingAnnouncer()),
            configure: window => { window.Width = 1000; window.Height = 700; },
            body: async (window, document) =>
            {
                var lines = new List<string>();
                var previous = WorkbenchDiagnostics.Sink;
                WorkbenchDiagnostics.Sink = lines.Add;
                try
                {
                    var announcer = (RecordingAnnouncer)typeof(SessionDocumentSurface).GetField("_announcer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(document)!;
                    var second = 0;
                    document.ReadModel.Changed += _ => second++;

                    // A snapshot the apply cannot read: a turn list that throws when walked.
                    var poison = new ThreadSnapshot(new ThrowingTurns(), 1, true);
                    var onChanged = typeof(ThreadFeed).GetMethod("OnChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                    onChanged.Invoke(document.Thread, [poison]);
                    await PumpAsync(window);
                    await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);

                    Assert.True(document.Thread.IsStopped);
                    Assert.True(document.StoppedRowVisible);
                    Assert.Contains(lines, l => l.Contains("\"error_code\":\"THR-0001\"", StringComparison.Ordinal));
                    var stopped = Assert.Single(announcer.Announcements);
                    Assert.Equal(ThreadFeed.StoppedSentence, stopped.Text);
                    Assert.Equal(Urgency.Assertive, stopped.Urgency);

                    // The stopped row is outside the scroller (a sibling of the feed, not inside its extent).
                    var row = ThreadFixtures.Visuals<TextBlock>(document).Single(t => t.Text == ThreadFeed.StoppedSentence);
                    Assert.False(IsInside(row, document.Thread));

                    // The channel survives: a second subscriber still hears the read model.
                    document.ReadModel.Accept("after", ThreadFixtures.Decorations("free-form", "T0", null, "message"), "b", ThreadFixtures.T0);
                    Assert.Equal(1, second);
                }
                finally
                {
                    WorkbenchDiagnostics.Sink = previous;
                }
            });
    }

    /// <summary><b>S1.</b> A reply carrying markup renders as those characters; no Invoke-capable element is inside the reply.</summary>
    [Fact]
    public void AReplyWithMarkup_RendersAsCharacters_NoHyperlink()
    {
        Sta.Run(() =>
        {
            var hostile = ThreadFixtures.Turn(1, "words", ThreadFixtures.Decorations("free-form", "T0", null, "message"), TurnState.Answered,
                new OutcomeView("conductor", null, null, null, null, 1), "<a href=\"x\">Deny</a> **bold** [link](http://x)", [ThreadFixtures.Line(1, "conductor", "<script>alert(1)</script>")]);
            var (feed, _, _) = ThreadFixtures.Feed([hostile]);
            var window = new Window { Width = 900, Height = 600, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false, Content = feed };
            window.Show();
            try
            {
                var container = ThreadFixtures.Container(feed, 0);
                var reply = ThreadFixtures.Visuals<ThreadText>(container).Single(t => t.Text.Contains("Deny", StringComparison.Ordinal));
                Assert.Equal("<a href=\"x\">Deny</a> **bold** [link](http://x)", reply.Text);
                Assert.Empty(reply.Inlines.OfType<System.Windows.Documents.Hyperlink>());
                Assert.DoesNotContain(ThreadFixtures.Visuals<ButtonBase>(container), b => b.Content is "Deny");
            }
            finally
            {
                window.Close();
            }
        });
    }

    /// <summary><b>T1.</b> thread.* records exist and carry no text; an action yields one record with its request id.</summary>
    [Fact]
    public void ThreadRecordsExist_CarryNoText_AndAStopIsOneActionRecord()
    {
        Sta.Run(() =>
        {
            var lines = new List<string>();
            var previous = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = lines.Add;
            try
            {
                var secret = "docs/audit/audit-log.jsonl";
                var waiting = new TurnView(1, "e", $"write {secret}", ThreadFixtures.Decorations("free-form", "T1", "docs/**", "goal block"), TurnState.Waiting, null,
                    new WaitingRequest("req-9", "permission", $"claude-code asks to write outside the declared scope: {secret}.", [TurnActionKind.Deny, TurnActionKind.AllowOnce]), null, [], "b", ThreadFixtures.T0);
                var (feed, _, _) = ThreadFixtures.Feed([waiting]);
                var window = new Window { Width = 900, Height = 600, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false, Content = feed };
                window.Show();
                try
                {
                    var deny = ThreadFixtures.Visuals<Button>(ThreadFixtures.Container(feed, 0)).First(b => b.Content is "Deny");
                    deny.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                    var records = lines.Where(l => l.Contains("\"evt\":\"thread.", StringComparison.Ordinal)).ToList();
                    Assert.NotEmpty(records);
                    Assert.All(records, r => Assert.DoesNotContain(secret, r, StringComparison.Ordinal));
                    Assert.All(records, r => Assert.DoesNotContain("audit-log", r, StringComparison.Ordinal));
                    var action = Assert.Single(records, r => r.Contains("\"evt\":\"thread.action\"", StringComparison.Ordinal));
                    Assert.Contains("\"request_id\":\"req-9\"", action, StringComparison.Ordinal);
                    Assert.Contains("\"action\":\"Deny\"", action, StringComparison.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            }
            finally
            {
                WorkbenchDiagnostics.Sink = previous;
            }
        });
    }

    /// <summary><b>Z1.</b> The Core types live in Core with no WPF reachable; the feed's constructor takes exactly (ISessionThread, IWorkbenchAnnouncer[, string]) and references no store.</summary>
    [Fact]
    public void CoreTypesLiveInCore_AndTheFeedReferencesNoStore()
    {
        var core = typeof(ISessionThread).Assembly;
        Assert.Equal("AiDe.Core", core.GetName().Name);
        Assert.Same(core, typeof(TurnView).Assembly);
        Assert.Same(core, typeof(ThreadAnnouncementPolicy).Assembly);
        Assert.Same(core, typeof(Announcement).Assembly);
        Assert.DoesNotContain(core.GetReferencedAssemblies(), a => a.Name is "PresentationCore" or "PresentationFramework" or "WindowsBase");

        var constructor = Assert.Single(typeof(ThreadFeed).GetConstructors());
        var parameters = constructor.GetParameters().Select(p => p.ParameterType).ToList();
        Assert.Equal([typeof(ISessionThread), typeof(IWorkbenchAnnouncer), typeof(string)], parameters);

        foreach (var member in typeof(ThreadFeed).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
        {
            var ns = member.FieldType.Namespace ?? string.Empty;
            Assert.False(ns.StartsWith("AiDe.Core.Compilation", StringComparison.Ordinal), member.Name);
            Assert.NotEqual(typeof(ConsoleStreamModel), member.FieldType);
        }
    }

    private sealed class ThrowingTurns : IReadOnlyList<TurnView>
    {
        public TurnView this[int index] => throw new InvalidOperationException("a turn list that cannot be read");
        public int Count => 1;
        public IEnumerator<TurnView> GetEnumerator() => throw new InvalidOperationException("a turn list that cannot be read");
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static RunChannelSessionThread ReadModel(ThreadFeed feed) =>
        (RunChannelSessionThread)typeof(ThreadFeed).GetField("_thread", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(feed)!;

    private static RecordingAnnouncer Announcer(ThreadFeed feed) =>
        (RecordingAnnouncer)typeof(ThreadFeed).GetField("_announcer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(feed)!;

    private static bool IsInside(DependencyObject element, DependencyObject ancestor)
    {
        for (var node = element; node is not null; node = System.Windows.Media.VisualTreeHelper.GetParent(node))
        {
            if (ReferenceEquals(node, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<AutomationPeer> Descendants(AutomationPeer peer)
    {
        foreach (var child in peer.GetChildren() ?? [])
        {
            yield return child;
            foreach (var inner in Descendants(child))
            {
                yield return inner;
            }
        }
    }

    private static async Task PumpAsync(Window window)
    {
        for (var i = 0; i < 3; i++)
        {
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
            window.UpdateLayout();
        }
    }
}
