using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// RQ1–RQ5: the task class is chosen from a set, and the reason it is required is at the field.
/// </summary>
/// <remarks>
/// <para><b>The operator asked the same question twice in one session</b> — "not sure what Task
/// Class is here and why it is mandatory", then "still dont know why the task class exists" — while
/// the answer was on screen. A question repeated after the answer was shown is evidence about the
/// affordance, not about the operator, and it is what raises this from a copy edit to a control
/// change.</para>
///
/// <para><b>The requirement was never the failure.</b> It is the cohort key every comparison is
/// scoped by, and a guessed class is indistinguishable from a chosen one afterwards. The failures
/// were asking for a closed vocabulary through an open text box, and explaining the rule in the
/// vocabulary of the subsystem that needs it, 200px below the field.</para>
/// </remarks>
public sealed class TheTaskClassIsChosenNotTypedTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "aide-taskclass-" + Guid.NewGuid().ToString("N"));

    public TheTaskClassIsChosenNotTypedTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    private NewSessionSheetViewModel Sheet() => new(
        _root, _root, new ProviderRegistry([]), new DateTimeOffset(2026, 9, 11, 9, 0, 0, TimeSpan.Zero));

    private static IEnumerable<T> Walk<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T typed)
        {
            yield return typed;
        }

        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            foreach (var found in Walk<T>(child))
            {
                yield return found;
            }
        }
    }

    [Fact]
    public void TheClassIsPickedFromTheOfferedSet_AndNothingIsPreselected() => Sta.Run(() =>
    {
        var sheet = Sheet();
        var body = NewSessionSheetDialog.Build(sheet, announce: null, onCreate: () => { });

        var picker = Assert.Single(Walk<ListBox>(body));

        Assert.Equal(sheet.TaskClassOptions.Count, picker.Items.Count);
        Assert.Equal(-1, picker.SelectedIndex);
        Assert.Null(sheet.TaskClass);
        Assert.False(sheet.CanCreate);

        // RQ1 — the only TextBox left on the sheet is the session NAME, which is free text because a
        // name is free text. A cohort key is not.
        var boxes = Walk<TextBox>(body).ToList();
        Assert.Single(boxes);
        Assert.Equal(
            "Session name",
            System.Windows.Automation.AutomationProperties.GetName(boxes[0]));
    });

    [Fact]
    public void EveryOfferedClassSaysWhatChoosingItDecides() => Sta.Run(() =>
    {
        var sheet = Sheet();
        var body = NewSessionSheetDialog.Build(sheet, announce: null, onCreate: () => { });
        var picker = Assert.Single(Walk<ListBox>(body));

        foreach (var (item, option) in picker.Items.Cast<ListBoxItem>().Zip(sheet.TaskClassOptions))
        {
            var lines = Walk<TextBlock>(item).Select(t => t.Text).ToList();

            Assert.Contains(option.Id, lines);
            Assert.Contains(option.WhatItIs, lines);

            // An icon-only row and a bare identifier are the same failure: the operator is choosing
            // what this session gets compared against, so each row says what it covers.
            Assert.EndsWith(".", option.WhatItIs, StringComparison.Ordinal);
        }
    });

    [Fact]
    public void ChoosingOneAnswersTheFieldAndEnablesCreate() => Sta.Run(() =>
    {
        var sheet = Sheet();
        var body = NewSessionSheetDialog.Build(sheet, announce: null, onCreate: () => { });
        var picker = Assert.Single(Walk<ListBox>(body));
        var create = Walk<Button>(body).Single(b => (string?)b.Content == "Create session");

        Assert.False(create.IsEnabled);
        Assert.False(sheet.TaskClassAnswered);

        picker.SelectedIndex = 1;

        Assert.Equal(sheet.TaskClassOptions[1].Id, sheet.TaskClass);
        Assert.True(sheet.TaskClassAnswered);
        Assert.True(create.IsEnabled);
    });

    [Fact]
    public void TheRequiredStateIsAGlyphAndAWord_NeverAnAsterisk() => Sta.Run(() =>
    {
        var sheet = Sheet();
        var body = NewSessionSheetDialog.Build(sheet, announce: null, onCreate: () => { });
        var picker = Assert.Single(Walk<ListBox>(body));

        var unanswered = Walk<TextBlock>(body).Select(t => t.Text).ToList();
        Assert.Contains(unanswered, line => line.Contains(TaskClassVocabulary.RequiredLabel, StringComparison.Ordinal));
        Assert.DoesNotContain(unanswered, line => line.Trim() == "*");

        picker.SelectedIndex = 0;

        var answered = Walk<TextBlock>(body).Select(t => t.Text).ToList();
        Assert.Contains(answered, line => line.Contains(TaskClassVocabulary.AnsweredLabel, StringComparison.Ordinal));
        Assert.DoesNotContain(answered, line => line.Contains(TaskClassVocabulary.RequiredLabel, StringComparison.Ordinal));
    });

    [Fact]
    public void TheExplanationSitsAtTheField_AboveTheControl_AndNamesAConsequence() => Sta.Run(() =>
    {
        var sheet = Sheet();
        var body = NewSessionSheetDialog.Build(sheet, announce: null, onCreate: () => { });

        var children = ((StackPanel)body).Children.OfType<UIElement>().ToList();

        var explanation = children.FindIndex(
            c => c is TextBlock text && text.Text == TaskClassVocabulary.Explanation);
        var picker = children.FindIndex(c => c is ListBox);

        Assert.True(explanation >= 0, "the sheet does not render the explanation at all");
        Assert.True(picker >= 0, "the sheet does not render the picker at all");

        // RQ3 — above the control, before the answer is needed. Not a footnote under the buttons.
        Assert.True(explanation < picker,
            "the explanation renders BELOW the control it explains, which is where the shipped "
            + "sheet put it and why the question was asked twice");

        // RQ2 — a consequence, not a mechanism. "Ranks in the wrong cohort" names a subsystem.
        Assert.DoesNotContain("cohort", TaskClassVocabulary.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("compared against", TaskClassVocabulary.Explanation, StringComparison.Ordinal);
    });

    [Fact]
    public void TheDisabledCreateCarriesItsReasonInTheSameRow() => Sta.Run(() =>
    {
        var sheet = Sheet();
        var body = NewSessionSheetDialog.Build(sheet, announce: null, onCreate: () => { });

        var create = Walk<Button>(body).Single(b => (string?)b.Content == "Create session");
        var row = LogicalTreeHelper.GetParent(LogicalTreeHelper.GetParent(create));

        var reason = Walk<TextBlock>(row).SingleOrDefault(
            t => t.Text == TaskClassVocabulary.ChooseOneToCreate);

        // RQ5 — beside the button, naming the field. An inert control never leaves the operator
        // guessing, and a reason 200px away is a reason nobody connects to the button.
        Assert.True(reason is not null,
            "the disabled Create button's row does not carry its reason; it rendered as a footnote "
            + "under the buttons, in the vocabulary of the ranking subsystem");
        Assert.Equal(Visibility.Visible, reason!.Visibility);
    });

    /// <summary>
    /// The picker does not become a default by another route.
    /// </summary>
    /// <remarks>
    /// Ruling 19 and <c>AssertNoDefaultFor</c> pin the TYPE; this pins the SHEET. Offering a set and
    /// preselecting its first row would satisfy every other assertion here and reintroduce exactly
    /// the defect the no-default rule exists for — a chosen class and a guessed one, indistinguishable
    /// afterwards.
    /// </remarks>
    [Fact]
    public void OfferingASetIsNotTheSameAsSupplyingOne() => Sta.Run(() =>
    {
        var sheet = Sheet();
        _ = NewSessionSheetDialog.Build(sheet, announce: null, onCreate: () => { });

        Assert.Null(sheet.TaskClass);
        Assert.False(sheet.TaskClassAnswered);
        Assert.Equal(TaskClassVocabulary.ChooseOneToCreate, sheet.BlockedReason);
    });
}
