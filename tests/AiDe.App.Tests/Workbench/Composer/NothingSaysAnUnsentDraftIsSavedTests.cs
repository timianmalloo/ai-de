using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using AiDe.App.Tests.Records;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>T30 n3 (A) — Ruling 182 (2): nothing implies that an unsent draft or a kept line is saved.</b> An unsent draft and
/// its kept lines are lost on restart until WL-2 (Ruling 182 (3)), so no composer text, badge or UIA name may claim they
/// are saved. The test walks the user's gesture on the product's composition (<see cref="ComposerGesture"/>): type,
/// Ctrl+Enter (prepare under advisory, so the lines read <i>? suggested</i>), Keep Goal through its invoke peer (the line
/// reads <i>✓ kept</i>), then the header's <i>Session settings</i>. At each step it reads every string the surface shows
/// or names: text, content, headers, tool tips, the UIA name, help text and item status, the status line, the marks and
/// the field descriptors. It refuses saved-state copy in any of them. The user's own words and the model's suggestions
/// (text boxes) are not the product's copy and are not read. The composer page's markup is read too.
/// </summary>
public sealed class NothingSaysAnUnsentDraftIsSavedTests(ITestOutputHelper output) : IDisposable
{
    /// <summary>Copy that says or implies the draft is kept beyond the session: <i>saved</i>, <i>kept for later</i>, and the like.</summary>
    private static readonly Regex SavedStateCopy = new(
        @"\b(saved|saves|autosaved?|auto-saved?|for later|persist(s|ed|ent)?|stored|survives?|restored on restart)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private const string Words = "Rename the helper in the parser.";

    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void TheComposer_ItsSuggestedAndKeptMarks_AndTheSettingsRow_SayNothingIsSaved()
    {
        var seen = new List<(string Moment, string Text)>();
        ComposerGesture.Drive(_fixture, async (window, document) =>
        {
            var composer = document.Composer;
            AKeptSuggestedLineIsKeptTests.UnderAdvisory(composer);

            ComposerGesture.Type(composer, Words);
            ComposerGesture.Settle(document);
            Read("an unsent draft", composer, seen);

            ComposerGesture.CtrlEnter(composer);
            await composer.Preparing!.WaitAsync(NotebookFixture.Bound);
            ComposerGesture.Settle(document);
            Assert.Equal(StructureLine.ModelLineMark, composer.StructureMarks[GoalBlockFields.GoalKey]);
            Read("suggested lines", composer, seen);

            AKeptSuggestedLineIsKeptTests.Invoke(AKeptSuggestedLineIsKeptTests.KeepButton(composer, "Goal"));
            ComposerGesture.Settle(document);
            Assert.Equal("✓ kept", composer.StructureMarks[GoalBlockFields.GoalKey]);
            Read("a kept line", composer, seen);

            var settings = ComposerGesture.Logical<Button>(document).Single(b => AutomationProperties.GetName(b) == "Session settings");
            ((IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(settings).GetPattern(PatternInterface.Invoke)!).Invoke();
            ComposerGesture.Settle(document);
            var popups = OwnWindow.Popups(Window.GetWindow(document));
            Assert.NotEmpty(popups);
            foreach (var popup in popups)
            {
                seen.AddRange(Shown(popup).Select(text => ("the settings row", text)));
            }
        });

        foreach (var page in new[] { "composer.html", "composer-host.html" })
        {
            seen.Add(("the composer page " + page, Regex.Replace(File.ReadAllText(Path.Combine(RepoRoot(), "src", "AiDe.App", "Web", page)), "<[^>]*>", " ")));
        }

        var refused = seen.Where(s => SavedStateCopy.IsMatch(s.Text)).ToList();
        output.WriteLine($"read {seen.Count} strings across {seen.Select(s => s.Moment).Distinct().Count()} moments: {string.Join(", ", seen.GroupBy(s => s.Moment).Select(g => $"{g.Key} {g.Count()}"))}");
        refused.ForEach(r => output.WriteLine($"SAVED-STATE COPY at {r.Moment}: \"{r.Text}\""));
        Assert.True(refused.Count == 0, "saved-state copy is shown for an unsent draft or a kept line: " + string.Join(" | ", refused.Select(r => $"{r.Moment}: \"{r.Text}\"")));
    }

    /// <summary>What the composer shows or names now: its visual and logical trees, the status line, the marks and the field descriptors.</summary>
    private static void Read(string moment, ComposerSurface composer, List<(string, string)> seen)
    {
        seen.AddRange(Shown(composer).Select(text => (moment, text)));
        seen.Add((moment, composer.Status));
        seen.AddRange(composer.StructureMarks.Values.Select(mark => (moment, mark)));
        seen.AddRange(composer.Fields.SelectMany(f => new[] { f.Name, f.Label, f.Hint ?? string.Empty }).Select(text => (moment, text)));
    }

    /// <summary>Every non-empty product string under <paramref name="root"/>; text boxes hold the user's or the model's words and are skipped.</summary>
    private static IEnumerable<string> Shown(DependencyObject root)
    {
        var nodes = ThreadFixtures.Visuals<DependencyObject>(root).Concat(ComposerGesture.Logical<DependencyObject>(root)).Prepend(root).Distinct();
        foreach (var node in nodes)
        {
            var texts = new List<string?>
            {
                AutomationProperties.GetName(node),
                AutomationProperties.GetHelpText(node),
                AutomationProperties.GetItemStatus(node),
            };
            switch (node)
            {
                case TextBlock block:
                    texts.Add(block.Text);
                    break;
                case HeaderedContentControl headered:
                    texts.Add(headered.Header as string);
                    texts.Add(headered.Content as string);
                    break;
                case ContentControl content:
                    texts.Add(content.Content as string);
                    break;
            }

            if (node is FrameworkElement { ToolTip: string tip })
            {
                texts.Add(tip);
            }

            foreach (var text in texts.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                yield return text!;
            }
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
