using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using AiDe.App.Tests.Records;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Composer;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>T30 n5 (A) — Ruling 180 (3), the Owner's exact shape: a prepare that yields suggested lines shows them.</b> Node 4
/// found that the suggested lines were filled into the <i>Goal · Done when · Not in scope</i> fold, which is collapsed at
/// rest and which no prepare opened (<c>ComposerSurface.cs</c>'s <c>BuildStructure</c> and <c>PrepareTurnAsync</c>). The
/// user read <i>"prepared — press again to send"</i> with no suggestion on the screen, and a second Ctrl+Enter sent
/// without them. Every step is the user's gesture on the product's composition (<see cref="ComposerGesture"/>): the
/// words through the editor's input verb, one Ctrl+Enter under agentic-advisory with a stub compile host (no model is
/// called), and Tab out of the editor through the page's <c>focus.leave</c> verb, then WPF's own Tab traversal.
/// </summary>
public sealed class APrepareShowsItsSuggestedLinesTests(ITestOutputHelper output) : IDisposable
{
    private const string Words = "Rename the helper in the parser.";

    /// <summary>The live region's sentence for three suggested lines (DESIGN.md's composer status row).</summary>
    private const string ThreeSuggested = "3 suggested lines. Review them before you send.";

    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    /// <summary>
    /// Type, then Ctrl+Enter once: the fold opens; keyboard focus is where it was (never in a suggestion); the count is
    /// announced exactly once; and Tab from the editor reaches each suggested line, whose UIA name starts
    /// <c>? suggested</c>. The fold stays open while the user works in it, and an unkept line still does not count.
    /// </summary>
    [Fact]
    public void OnePrepareThatSuggestsLines_OpensTheFold_LeavesFocus_AnnouncesTheCountOnce_AndTabReachesEachLine()
    {
        ComposerGesture.Drive(_fixture, async (window, document) =>
        {
            var composer = document.Composer;
            var announcer = Announcer(document);
            AKeptSuggestedLineIsKeptTests.UnderAdvisory(composer);
            var fold = Fold(composer);
            Assert.False(fold.IsExpanded, "the fold must be collapsed at rest, before the prepare");

            ComposerGesture.Type(composer, Words);
            ComposerGesture.Settle(document);
            var editor = ComposerGesture.Logical<FrameworkElement>(composer).Single(e => AutomationProperties.GetName(e) == "Message");
            Keyboard.Focus(editor);
            var focusedBefore = Keyboard.FocusedElement;
            announcer.Clear();

            ComposerGesture.CtrlEnter(composer);
            await composer.Preparing!.WaitAsync(NotebookFixture.Bound);
            ComposerGesture.Settle(document);

            var focusedAfter = Keyboard.FocusedElement;
            var counts = announcer.Messages.Count(m => m.Contains("suggested line", StringComparison.Ordinal));
            output.WriteLine($"fold open: {fold.IsExpanded}; focus before: {Describe(focusedBefore)}; after: {Describe(focusedAfter)}; window active: {window.IsActive}");
            output.WriteLine("announced: " + string.Join(" | ", announcer.Messages));

            Assert.True(fold.IsExpanded, "a prepare that suggested lines left them in the closed fold");
            Assert.Same(focusedBefore, focusedAfter);
            Assert.False(fold.IsKeyboardFocusWithin, $"the prepare moved keyboard focus into a suggestion: {Describe(focusedAfter)}");
            Assert.Equal(1, counts);
            Assert.Contains(ThreeSuggested, announcer.Messages);

            // TAB FROM THE EDITOR: the page posts focus.leave forward, then WPF's Tab walks on.
            composer.MoveFocus(backward: false);
            var reached = new List<string>();
            for (var stop = 0; stop < 12 && Keyboard.FocusedElement is UIElement focused; stop++)
            {
                if (focused is TextBox line && fold.IsAncestorOf(line))
                {
                    reached.Add(AutomationProperties.GetName(line));
                }

                focused.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                if (ReferenceEquals(Keyboard.FocusedElement, focused))
                {
                    break;
                }
            }

            output.WriteLine("Tab reached: " + string.Join(" | ", reached));
            foreach (var label in new[] { "Goal", "Done when", "Not in scope" })
            {
                Assert.Contains(StructureLine.ModelLineMark + " " + label, reached);
            }

            // The user's own act closes the fold; the app never does. And an unkept line still does not count.
            Assert.True(fold.IsExpanded);
            Assert.All(
                ComposerDraft.PerPromptGoalFields,
                field => Assert.False(composer.Draft.GoalValues.TryGetValue(field, out var value) && value.Length > 0, $"{field} reached the draft unkept: \"{value}\""));
        });
    }

    private static Expander Fold(ComposerSurface composer) =>
        ComposerGesture.Logical<Expander>(composer).Single(e => AutomationProperties.GetName(e) == "Goal, Done when, Not in scope");

    /// <summary>The document's announcer: the composition passes a <see cref="RecordingAnnouncer"/> (as <c>ASendWhileATurnRunsOffersWaitOrParallelTests</c> reads it).</summary>
    private static RecordingAnnouncer Announcer(SessionDocumentSurface document) =>
        (RecordingAnnouncer)typeof(SessionDocumentSurface).GetField("_announcer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(document)!;

    private static string Describe(IInputElement? element) =>
        element is null ? "(none)" : $"{element.GetType().Name} \"{(element is DependencyObject d ? AutomationProperties.GetName(d) : string.Empty)}\"";
}
