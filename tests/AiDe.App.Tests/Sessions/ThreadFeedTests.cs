using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// <b>CV-5.3 — Ruling 82: the thread renders the conversation.</b> T1–T9 over the rendered reply
/// side (headless WPF, the real template): items in event order then the outcome line; the reasoning
/// item collapsed, muted, never announced; prose as the markdown subset with no link activation; a
/// tool item's kind · title · status with its detail on demand; <i>interrupted</i> on a stopped
/// turn; a failed past turn's conversation; the kind as text; in-place status updates; the entry
/// stop on a running last turn. The fixtures are the review's event model at the event level
/// (<see cref="ThreadFixtures"/>), extended with thoughts and tool facts as the wire states them.
/// </summary>
/// <remarks>
/// <b>Red observed</b> on the pre-ruling template (the reply as prose rows, the outcome line first):
/// T1 <c>a TextBlock reads "## Gaps I noticed"</c>; T2 · T4 · T5 · T6 · T7 <c>no disclosure named
/// Thinking / Detail of …</c>, <c>no ThreadText "read"</c>; T3 <c>a TextBlock reads "[the
/// review](docs/reviews/r.md)"</c>; T8 <c>no detail to focus</c>; T9 <c>Expected: Stop this turn ·
/// Actual: Provenance of b6</c>.
/// </remarks>
public sealed class ThreadFeedTests
{
    private const string Lane = "claude-code";

    // ── the event model with facts, as the product's sink writes it ──

    private static IEnumerable<EventLine> Thought(int s, string text) => text.Chunk(14).Select(c => ThreadFixtures.Line(s, Lane, new string(c), Coalesce.ThoughtKind));

    private static EventLine Call(int s, string id, string kind, string title, string input) =>
        new(ThreadFixtures.T0.AddSeconds(s), Lane, ConversationItems.CallKind, title, "run", new ToolFacts(id, kind, title, "pending", input, null));

    private static EventLine Result(int s, string id, string status, string output) =>
        new(ThreadFixtures.T0.AddSeconds(s), Lane, ConversationItems.ResultKind, status, "run", new ToolFacts(id, null, null, status, null, output));

    private static EventLine Acp(int s, string name) => ThreadFixtures.Line(s, Lane, "acp.session.update." + name, "acp.session.update." + name);

    private static EventLine Cond(int s, string kind, string text) => ThreadFixtures.Line(s, "conductor", text, kind);

    private const string Screenshot3Prose =
        "## Gaps I noticed\n\nTwo stores — see §4 and [the review](docs/reviews/r.md).\n\n| What | Result |\n|---|---|\n| Red first | `Failed` on the old order |\n| Green | **14 passed** |\n\n- `LayoutStore` — tree schema\n- `ZoneLayoutStore`\n\n```csharp\nvar x = 1;\n```";

    /// <summary>A completed turn shaped like the operator's screenshot-3 turn: thought · two tool items · the prose · acp rows · the conductor's lines.</summary>
    private static TurnView Completed(int ordinal) =>
        ThreadFixtures.Turn(ordinal, "Find the gaps and report.", ThreadFixtures.Decorations("review", "T1", "src/**", "goal block"), TurnState.Completed,
            new OutcomeView(Lane, null, 1, new Spend(9_000, 800, 1_900, 2), TimeSpan.FromSeconds(185), 0),
            [
                Cond(1, "run.accepted", $"Block b{ordinal} accepted, tier T1, 1 lane"),
                .. Thought(2, "A green theory beside a failing shell measures something the shell does not construct."),
                Call(3, "t1", "read", "Read LayoutStore.cs", "file_path: src/AiDe.Core/Workbench/LayoutStore.cs"),
                Result(3, "t1", "completed", "412 lines"),
                Call(4, "t2", "execute", "dotnet test --filter LayoutStore", "command: dotnet test --filter LayoutStore"),
                Result(5, "t2", "failed", "Failed! 13 passed, 1 failed"),
                .. ThreadFixtures.Reply(6, Lane, Screenshot3Prose, chunks: 12),
                Acp(7, "usage_update"),
                Cond(8, "run.completed", "completed: 1 edit, 9,900 tokens"),
            ]);

    private static (ThreadFeed Feed, RunChannelSessionThread Thread, RecordingAnnouncer Announcer) FeedOf(params IEnumerable<TurnView> turns) => ThreadFixtures.Feed(turns);

    private static IEnumerable<ThreadText> Texts(DependencyObject root) => ThreadFixtures.Visuals<ThreadText>(root).Where(t => t.IsVisible);

    /// <summary>
    /// What an AT reads: the plain text of the block's inlines. <c>TextBlock.Text</c> is empty for
    /// content built from <c>Run</c>s (Verified by probe on this build), so a falsifier over
    /// <c>Text</c> alone would pass vacuously on the prose (DC-016's class).
    /// </summary>
    private static string Plain(TextBlock text) => new TextRange(text.ContentStart, text.ContentEnd).Text;

    private static Expander Disclosure(DependencyObject root, string name) =>
        ThreadFixtures.Visuals<Expander>(root).FirstOrDefault(e => AutomationProperties.GetName(e) == name)
        ?? throw new Xunit.Sdk.XunitException($"no disclosure named '{name}'; the disclosures: {string.Join(" | ", ThreadFixtures.Visuals<Expander>(root).Select(AutomationProperties.GetName))}");

    private static ThreadText Word(DependencyObject root, string text) =>
        Texts(root).FirstOrDefault(t => Plain(t) == text)
        ?? throw new Xunit.Sdk.XunitException($"no ThreadText \"{text}\"; the texts: {string.Join(" | ", Texts(root).Select(t => Plain(t).Length > 40 ? Plain(t)[..40] + "…" : Plain(t)))}");

    private static double Top(FrameworkElement element, Visual ancestor) => element.TransformToAncestor(ancestor).Transform(new Point(0, 0)).Y;

    private static async Task PumpAsync(Window window)
    {
        for (var i = 0; i < 3; i++)
        {
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
            window.UpdateLayout();
        }
    }

    private static void Themed(Window window)
    {
        var theme = ThreadFixtures.AppTheme();
        window.Resources = theme;
        window.Background = (Brush)theme["SurfaceRaisedBrush"];
        window.Width = 1100;
        window.Height = 760;
    }

    private static Color Token(Window window, string key) => ThemeProbe.Token(window.Resources, key);

    /// <summary><b>T1.</b> Items in event order, the outcome line last, the fold counting only the non-conversation rows.</summary>
    [Fact]
    public void TheReplySide_RendersItemsThenTheOutcomeLine_AndFoldsOnlyNonConversationRows()
    {
        Sta.Pump(
            create: () => FeedOf(Completed(1)).Feed,
            configure: Themed,
            body: (window, feed) =>
            {
                var container = ThreadFixtures.Container(feed, 0);

                // Falsifier 1: markdown source rendered as text.
                Assert.DoesNotContain(Texts(container), t => Plain(t).Contains("##", StringComparison.Ordinal) || Plain(t).Contains("|---|", StringComparison.Ordinal));
                Assert.Contains(Texts(container), t => Plain(t).Contains("Red first", StringComparison.Ordinal));   // the walk sees the prose (the positive control)

                // Falsifier 2: a tool item absent.
                var title = Word(container, "Read LayoutStore.cs");
                var thinking = Word(container, "Thinking");
                var heading = Word(container, "Gaps I noticed");

                // Falsifier 3: the outcome line before the items — every item sits above the outcome word.
                var outcome = Word(container, "completed");
                Assert.True(Top(thinking, container) < Top(title, container), "the thought precedes the tool item");
                Assert.True(Top(title, container) < Top(heading, container), "the tool item precedes the prose");
                Assert.True(Top(heading, container) < Top(outcome, container), "the outcome line is last");

                // Falsifier 4: N events counting a conversation row — 3 non-conversation rows (accepted · usage · completed).
                var fold = ThreadFixtures.Fold(container);
                Assert.Equal("3 events", AutomationProperties.GetName(fold));
                feed.Rows[0].IsFoldOpen = true;
                feed.UpdateLayout();
                var folded = feed.Rows[0].FoldedEvents.Select(r => r.Kind).ToList();
                Assert.Equal(["run.accepted", "acp.session.update.usage_update", "run.completed"], folded);
                return Task.CompletedTask;
            });
    }

    /// <summary><b>T2.</b> Forty thought chunks announce nothing; the disclosure is collapsed; its ink is muted; no live setting.</summary>
    [Fact]
    public void TheReasoningItem_IsCollapsedByDefault_MutedAndNeverAnnounced()
    {
        Sta.Pump(
            create: () => FeedOf(ThreadFixtures.Five().Append(ThreadFixtures.Running(6, 0)).ToList()).Feed,
            configure: Themed,
            body: async (window, feed) =>
            {
                var (thread, announcer) = (ReadModel(feed), Announcer(feed));
                for (var i = 1; i <= 40; i++)
                {
                    thread.Append(6, ThreadFixtures.Line(i, Lane, $"thought chunk {i} ", Coalesce.ThoughtKind));
                }

                await PumpAsync(window);
                Assert.Empty(announcer.Announcements);
                Assert.Empty(announcer.Messages);

                var container = ThreadFixtures.Container(feed, 5);
                var disclosure = Disclosure(container, "Thinking");
                var peer = (IExpandCollapseProvider)UIElementAutomationPeer.CreatePeerForElement(disclosure).GetPattern(PatternInterface.ExpandCollapse)!;
                Assert.Equal(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);
                Assert.Equal(AutomationLiveSetting.Off, AutomationProperties.GetLiveSetting(disclosure));
                Assert.Equal(Token(window, "TextMutedBrush"), ThemeProbe.Ink(Word(container, "Thinking")));

                // Opened on demand: the joined 40 chunks, muted, plain text — and still nothing announced.
                disclosure.IsExpanded = true;
                feed.UpdateLayout();
                var thought = Texts(container).Single(t => Plain(t).StartsWith("thought chunk 1 thought chunk 2", StringComparison.Ordinal));
                Assert.Equal(Token(window, "TextMutedBrush"), ThemeProbe.Ink(thought));
                Assert.EndsWith("thought chunk 40 ", Plain(thought), StringComparison.Ordinal);
                Assert.Equal(AutomationLiveSetting.Off, AutomationProperties.GetLiveSetting(thought));
                await PumpAsync(window);
                Assert.Empty(announcer.Announcements);
            });
    }

    /// <summary><b>T3.</b> Headings · lists · code · tables render as elements; a link is its text with its URL, no <c>Hyperlink</c>, no <c>InvokePattern</c>.</summary>
    [Fact]
    public void ProseRendersTheMarkdownSubset_WithNoLinkActivation()
    {
        Sta.Pump(
            create: () => FeedOf(Completed(1)).Feed,
            configure: Themed,
            body: (window, feed) =>
            {
                var container = ThreadFixtures.Container(feed, 0);

                Assert.Empty(ThreadFixtures.Visuals<TextBlock>(container).SelectMany(t => t.Inlines).OfType<Hyperlink>());
                Assert.DoesNotContain(Texts(container), t => Plain(t).Contains("](", StringComparison.Ordinal));
                var link = Texts(container).Single(t => Plain(t).Contains("the review (docs/reviews/r.md)", StringComparison.Ordinal));
                var peer = UIElementAutomationPeer.CreatePeerForElement(link);
                Assert.Null(peer.GetPattern(PatternInterface.Invoke));
                Assert.True(peer.IsControlElement());

                // Headings: one size, the weight carries the level.
                var heading = Word(container, "Gaps I noticed");
                Assert.Equal(13.0, heading.FontSize);
                Assert.NotEqual(FontWeights.Normal, heading.FontWeight);

                // A table: cells as text, no box.
                Word(container, "Red first");
                Assert.Contains(Texts(container).SelectMany(t => t.Inlines.OfType<Run>()), r => r.Text == "14 passed" && r.FontWeight != FontWeights.Normal);

                // A list: the marker beside the item; code: mono on the sunken ground.
                Assert.Contains(Texts(container), t => Plain(t) == "•");
                var code = Word(container, "var x = 1;");
                Assert.Equal(ThreadFeed.Mono, code.FontFamily);
                Assert.Contains(Texts(container).SelectMany(t => t.Inlines.OfType<Run>()), r => r.Text == "LayoutStore" && r.FontFamily == ThreadFeed.Mono);

                // Ruling 87: the two code points as themselves.
                Assert.Contains(Texts(container), t => Plain(t).Contains("— see §4", StringComparison.Ordinal));
                return Task.CompletedTask;
            });
    }

    /// <summary><b>T4.</b> <i>read · Read LayoutStore.cs · done</i>; a failed result reads <i>failed</i> in <c>DangerBrush</c>; the detail carries the input, then the result.</summary>
    [Fact]
    public void AToolItem_ShowsKindTitleStatus_AndItsDetailOnDemand()
    {
        Sta.Pump(
            create: () => FeedOf(Completed(1)).Feed,
            configure: Themed,
            body: (window, feed) =>
            {
                var container = ThreadFixtures.Container(feed, 0);
                Word(container, "read");
                Word(container, "Read LayoutStore.cs");
                Assert.Equal(Token(window, "VerifiedBrush"), ThemeProbe.Ink(Word(container, "done")));
                Assert.Equal(Token(window, "DangerBrush"), ThemeProbe.Ink(Word(container, "failed")));
                Assert.Equal(ThreadFeed.Mono, Word(container, "dotnet test --filter LayoutStore").FontFamily);

                var detail = Disclosure(container, "Detail of Read LayoutStore.cs");
                Assert.False(detail.IsExpanded);
                detail.IsExpanded = true;
                feed.UpdateLayout();
                var pre = Texts(container).Single(t => Plain(t).Contains("file_path: src/AiDe.Core/Workbench/LayoutStore.cs", StringComparison.Ordinal));
                Assert.Equal(ThreadFeed.Mono, pre.FontFamily);
                Assert.True(pre.Text.IndexOf("input", StringComparison.Ordinal) < pre.Text.IndexOf("file_path", StringComparison.Ordinal), "input first");
                Assert.True(pre.Text.IndexOf("file_path", StringComparison.Ordinal) < pre.Text.IndexOf("result", StringComparison.Ordinal), "then the result");
                Assert.EndsWith("412 lines", pre.Text, StringComparison.Ordinal);

                // The detail is a keyboard-scrollable region: a focusable, named stop while open (A11-3).
                var region = ThreadFixtures.Visuals<ScrollViewer>(container).Single(s => AutomationProperties.GetName(s).StartsWith("Detail of Read LayoutStore.cs", StringComparison.Ordinal));
                Assert.True(region.Focusable && KeyboardNavigation.GetIsTabStop(region));
                Assert.True(region.MaxHeight <= 200);
                return Task.CompletedTask;
            });
    }

    /// <summary><b>T5.</b> A stopped turn whose last event is a call: <i>interrupted</i>, muted, static — never <i>running</i>, never a ring.</summary>
    [Fact]
    public void AToolCallWithNoResultOnANonLiveTurn_ReadsInterrupted_NeverRunning()
    {
        var stopped = ThreadFixtures.Turn(1, "Write the proof pack.", ThreadFixtures.Decorations("free-form", "T1", "docs/proof/**", "goal block"), TurnState.Stopped,
            new OutcomeView(Lane, null, 0, null, TimeSpan.FromSeconds(9), 0),
            [.. Thought(1, "Read before writing."), Call(2, "t1", "read", "Read docs/proof/pp-0141.md", "file_path: docs/proof/pp-0141.md"), Result(2, "t1", "completed", "40 lines"), Call(3, "t2", "edit", "Edit docs/proof/pp-0142.md", "file_path: docs/proof/pp-0142.md")]);

        Sta.Pump(
            create: () => FeedOf(stopped).Feed,
            configure: Themed,
            body: (window, feed) =>
            {
                var container = ThreadFixtures.Container(feed, 0);
                var word = Word(container, "interrupted");
                Assert.Equal(Token(window, "TextMutedBrush"), ThemeProbe.Ink(word));
                Assert.DoesNotContain(Texts(container), t => Plain(t) == "running");
                Assert.DoesNotContain(ThreadFixtures.Visuals<Ellipse>(container), e => e.IsVisible && e.ActualWidth > 0);
                Word(container, "done");   // the answered call keeps its own status
                return Task.CompletedTask;
            });
    }

    /// <summary><b>T6.</b> b17 at 40 turns: its Thinking and tool items present; no action buttons.</summary>
    [Fact]
    public void AFailedPastTurn_RendersItsConversation_AndFoldsOnlyItsActions()
    {
        var turns = ThreadFixtures.Forty();
        turns[16] = ThreadFixtures.Turn(17, turns[16].SourceText, turns[16].Decorations, TurnState.Failed,
            new OutcomeView(Lane, 1, 0, null, TimeSpan.FromSeconds(37), 0),
            [.. Thought(1, "The lease refuses docs/audit/."), Call(2, "t1", "edit", "Edit docs/audit/audit-log.jsonl", "file_path: docs/audit/audit-log.jsonl"),
             ThreadFixtures.Line(3, Lane, "b17 line 3: exit 1 — the lease refused docs/audit/", "stderr")],
            at: ThreadFixtures.T0.AddMinutes(7 * 16));

        Sta.Pump(
            create: () => FeedOf(turns).Feed,
            configure: Themed,
            body: (window, feed) =>
            {
                var container = ThreadFixtures.Container(feed, 16);
                Disclosure(container, "Thinking");
                Word(container, "Edit docs/audit/audit-log.jsonl");
                Assert.Equal(Token(window, "TextMutedBrush"), ThemeProbe.Ink(Word(container, "interrupted")));
                Assert.DoesNotContain(ThreadFixtures.Visuals<Button>(container), b => b.IsVisible && b.Content is "Send again as a new turn" or "Open the log");
                Assert.Equal("1 event", AutomationProperties.GetName(ThreadFixtures.Fold(container)));
                return Task.CompletedTask;
            });
    }

    /// <summary><b>T7.</b> The kind reaches UIA as text in the Control view (1.1.1); the link is <i>t (url)</i> with no <c>Hyperlink</c>.</summary>
    [Fact]
    public void TheToolKindIsAWord_AndTheLinkIsTextWithItsUrl()
    {
        Sta.Pump(
            create: () => FeedOf(Completed(1)).Feed,
            configure: Themed,
            body: (window, feed) =>
            {
                var container = ThreadFixtures.Container(feed, 0);
                var kind = UIElementAutomationPeer.CreatePeerForElement(Word(container, "execute"));
                Assert.True(kind.IsControlElement());
                Assert.Equal("execute", kind.GetName());
                Assert.Equal(AutomationControlType.Text, kind.GetAutomationControlType());

                var link = Texts(container).Single(t => Plain(t).Contains("the review (docs/reviews/r.md)", StringComparison.Ordinal));
                Assert.Empty(link.Inlines.OfType<Hyperlink>());
                Assert.Equal("Two stores — see §4 and the review (docs/reviews/r.md).", Plain(link));
                Assert.Equal("Two stores — see §4 and the review (docs/reviews/r.md).", UIElementAutomationPeer.CreatePeerForElement(link).GetName());
                return Task.CompletedTask;
            });
    }

    /// <summary><b>T8.</b> Focus on a running item's detail; the result arrives; the focused element is the same; the status reads <i>done</i>.</summary>
    [Fact]
    public void AStatusChange_UpdatesTheItemInPlace_FocusSurvives()
    {
        Sta.Pump(
            create: () => FeedOf(ThreadFixtures.Five().Append(ThreadFixtures.Running(6, 0)).ToList()).Feed,
            configure: Themed,
            body: async (window, feed) =>
            {
                var thread = ReadModel(feed);
                thread.Append(6, Call(1, "t1", "execute", "dotnet test", "command: dotnet test"));
                await PumpAsync(window);

                var container = ThreadFixtures.Container(feed, 5);
                Word(container, "running");
                var detail = Disclosure(container, "Detail of dotnet test");
                detail.IsExpanded = true;
                feed.UpdateLayout();
                var region = ThreadFixtures.Visuals<ScrollViewer>(container).Single(s => AutomationProperties.GetName(s).StartsWith("Detail of dotnet test", StringComparison.Ordinal));
                Assert.True(region.Focus());
                Assert.Same(region, Keyboard.FocusedElement);

                thread.Append(6, Result(2, "t1", "completed", "Passed! 14 passed"));
                await PumpAsync(window);

                Assert.Same(region, Keyboard.FocusedElement);
                Assert.True(detail.IsExpanded, "the disclosure's state survived the update");
                Word(ThreadFixtures.Container(feed, 5), "done");
                Assert.DoesNotContain(Texts(ThreadFixtures.Container(feed, 5)), t => t.DataContext is ConversationRow && Plain(t) == "running");   // the turn still runs; the item does not
                Assert.EndsWith("Passed! 14 passed", Plain(Texts(container).Single(t => Plain(t).StartsWith("input", StringComparison.Ordinal))), StringComparison.Ordinal);
            });
    }

    /// <summary><b>T9 (SC8 as amended).</b> On a running last turn the entry stop is <i>Stop this turn</i>, and Shift+Tab from the editor lands there.</summary>
    [Fact]
    public void OnARunningLastTurn_TheEntryStopIsStop_AndShiftTabFromTheEditorLandsThere()
    {
        Sta.Pump(
            create: () => FeedOf(ThreadFixtures.Five().Append(ThreadFixtures.Running(6, 0)).ToList()).Feed,
            content: feed =>
            {
                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var composer = new TextBox { Name = "composer", AcceptsReturn = true };
                Grid.SetRow(composer, 1);
                grid.Children.Add(feed);
                grid.Children.Add(composer);
                return grid;
            },
            configure: window => { window.Width = 1100; window.Height = 760; },
            body: async (window, feed) =>
            {
                var thread = ReadModel(feed);
                foreach (var line in Thought(1, "Read the proof pack before the change.").Append(Call(2, "t1", "read", "Read docs/proof/pp-0141.md", "file_path: docs/proof/pp-0141.md")))
                {
                    thread.Append(6, line);
                }

                await PumpAsync(window);
                var composer = (TextBox)((Grid)window.Content).Children[1];

                // Forward entry: Tab from the container lands on Stop, not on provenance.
                Assert.True(feed.FocusItem(5));
                ((UIElement)Keyboard.FocusedElement).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                var stop = Assert.IsType<Button>(Keyboard.FocusedElement);
                Assert.Equal("Stop this turn", stop.Content);

                // Backward entry from the editor: the same action.
                Assert.True(composer.Focus());
                Assert.True(feed.FocusCurrentItemLast());
                Assert.Same(stop, Keyboard.FocusedElement);

                // The inner stops follow in DOM order: Tab from Stop reaches provenance, then compiled prompt, Thinking, the detail, the fold.
                var names = new List<string>();
                for (var i = 0; i < 6; i++)
                {
                    ((UIElement)Keyboard.FocusedElement).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    var focused = (DependencyObject)Keyboard.FocusedElement;
                    if (ReferenceEquals(focused, composer))
                    {
                        break;
                    }

                    names.Add(AutomationProperties.GetName(focused));
                }

                Assert.Equal(["Provenance of b6", "Compiled prompt of b6", "Thinking", "Detail of Read docs/proof/pp-0141.md", "0 events"], names);
            });
    }

    /// <summary>The Console split: a thought row's kind word is <i>thought</i> and its ink muted (DESIGN.md SC1 as amended); a message row stays <i>message</i>.</summary>
    [Fact]
    public void TheSplitsThoughtRow_ReadsThoughtAndIsDim()
    {
        Sta.Pump(
            create: () => new ConsoleSurface(),
            configure: Themed,
            body: (window, split) =>
            {
                split.Show([Completed(1)]);
                window.UpdateLayout();
                var thought = split.Rows.OfType<ConsoleSplitRow.Line>().Single(l => l.Row.Kind == Coalesce.ThoughtKind);
                Assert.Equal("thought", thought.KindWord);
                Assert.Equal("message", split.Rows.OfType<ConsoleSplitRow.Line>().First(l => l.Row.Kind == Coalesce.MessageKind).KindWord);

                var index = split.Rows.ToList().IndexOf(thought);
                split.ScrollIntoView(split.Items[index]);
                split.UpdateLayout();
                var container = (ListBoxItem)split.ItemContainerGenerator.ContainerFromIndex(index);
                var text = Texts(container).Single(t => Plain(t) == thought.Text);
                Assert.Equal(Token(window, "TextMutedBrush"), ThemeProbe.Ink(text));
                return Task.CompletedTask;
            });
    }

    private static RunChannelSessionThread ReadModel(ThreadFeed feed) =>
        (RunChannelSessionThread)typeof(ThreadFeed).GetField("_thread", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(feed)!;

    private static RecordingAnnouncer Announcer(ThreadFeed feed) =>
        (RecordingAnnouncer)typeof(ThreadFeed).GetField("_announcer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(feed)!;

    private static bool IsInside(DependencyObject element, DependencyObject ancestor)
    {
        for (var node = element; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (ReferenceEquals(node, ancestor))
            {
                return true;
            }
        }

        return false;
    }
}
