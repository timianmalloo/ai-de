using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Watcher;

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

    /// <summary>
    /// RQ1 still: the class is picked from a set. Ruling 72 (b): the set's <c>free-form</c> row is
    /// preselected — the operator's declared default, visible as a row, not a hidden value.
    /// </summary>
    [Fact]
    public void TheClassIsPickedFromTheOfferedSet_AndFreeFormIsPreselected() => Sta.Run(() =>
    {
        var sheet = Sheet();
        var body = NewSessionSheetDialog.Build(sheet, announce: null, onCreate: () => { });

        var picker = Assert.Single(Walk<ListBox>(body));

        Assert.Equal(sheet.TaskClassOptions.Count, picker.Items.Count);
        Assert.Equal(0, picker.SelectedIndex);
        Assert.Equal(TaskClasses.FreeForm, ((ListBoxItem)picker.Items[0]).Tag);
        Assert.Equal(TaskClasses.FreeForm, sheet.TaskClass);
        Assert.True(sheet.CanCreate);

        // RQ1 — no TextBox on the sheet is the task class: a cohort key is chosen, never typed. The
        // text boxes that exist are free text (the name) or a number (the fan-out ceiling).
        var boxes = Walk<TextBox>(body)
            .Select(System.Windows.Automation.AutomationProperties.GetName)
            .ToList();
        Assert.Equal(["Session name", "Fan-out ceiling", "Cap: requests", "Cap: tokens"], boxes);
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

    /// <summary>Create is enabled from open (zero required inputs); choosing another class changes it.</summary>
    [Fact]
    public void CreateIsEnabledFromOpen_AndChoosingAnotherClassChangesIt() => Sta.Run(() =>
    {
        var sheet = Sheet();
        var body = NewSessionSheetDialog.Build(sheet, announce: null, onCreate: () => { });
        var picker = Assert.Single(Walk<ListBox>(body));
        var create = Walk<Button>(body).Single(b => (string?)b.Content == "Create session");

        Assert.True(create.IsEnabled);
        Assert.True(sheet.TaskClassAnswered);

        picker.SelectedIndex = 2;

        Assert.Equal(sheet.TaskClassOptions[2].Id, sheet.TaskClass);
        Assert.NotEqual(TaskClasses.FreeForm, sheet.TaskClass);
        Assert.True(sheet.TaskClassAnswered);
        Assert.True(create.IsEnabled);
    });

    /// <summary>RQ4 at Ruling 72: the answered state is a glyph and a word from open; never an asterisk, never "no default".</summary>
    [Fact]
    public void TheAnsweredStateIsAGlyphAndAWordFromOpen_NeverAnAsterisk() => Sta.Run(() =>
    {
        var sheet = Sheet();
        var body = NewSessionSheetDialog.Build(sheet, announce: null, onCreate: () => { });

        var lines = Walk<TextBlock>(body).Select(t => t.Text).ToList();
        Assert.Contains(lines, line => line.Contains(TaskClassVocabulary.AnsweredLabel, StringComparison.Ordinal));
        Assert.DoesNotContain(lines, line => line.Contains(TaskClassVocabulary.RequiredLabel, StringComparison.Ordinal));
        Assert.DoesNotContain(lines, line => line.Trim() == "*");
    });

    /// <summary>
    /// Ruling 72 (a) on the sheet: the budget renders as a state with no numeral, and the cap is an
    /// affordance the operator opts into — its two number boxes exist only once it is enforced.
    /// </summary>
    [Fact]
    public void TheBudgetIsAStateWithAnOptionalCapAffordance() => Sta.Run(() =>
    {
        var sheet = Sheet();
        var body = NewSessionSheetDialog.Build(sheet, announce: null, onCreate: () => { });

        var lines = Walk<TextBlock>(body).Select(t => t.Text).ToList();
        Assert.Contains(RunBudget.SubscriptionBoundedDisplay, lines);

        var enforce = Walk<CheckBox>(body).Single(c => (string?)c.Content == "Enforce a cap");
        Assert.False(enforce.IsChecked);
        var requests = Walk<TextBox>(body).Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Cap: requests");
        var tokens = Walk<TextBox>(body).Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Cap: tokens");
        Assert.Equal(Visibility.Collapsed, ((FrameworkElement)LogicalTreeHelper.GetParent(requests)).Visibility);

        enforce.IsChecked = true;
        Assert.Equal(Visibility.Visible, ((FrameworkElement)LogicalTreeHelper.GetParent(requests)).Visibility);
        requests.Text = "40";
        tokens.Text = "90000";

        Assert.Equal(new RunBudget(40, 90_000), sheet.BudgetCap);

        enforce.IsChecked = false;
        Assert.Null(sheet.BudgetCap);

        // The boxes are built once: re-ticking brings the typed numbers back as the cap.
        enforce.IsChecked = true;
        Assert.Equal(new RunBudget(40, 90_000), sheet.BudgetCap);
    });

    /// <summary>The fan-out ceiling is prefilled from the session default and typed as a number.</summary>
    [Fact]
    public void TheFanOutCeilingIsPrefilledAndEditable() => Sta.Run(() =>
    {
        var sheet = Sheet();
        var body = NewSessionSheetDialog.Build(sheet, announce: null, onCreate: () => { });

        var ceiling = Walk<TextBox>(body).Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Fan-out ceiling");
        Assert.Equal(SessionConfig.DefaultFanOutCeiling.ToString(System.Globalization.CultureInfo.InvariantCulture), ceiling.Text);

        ceiling.Text = "3";
        Assert.Equal(3, sheet.FanOutCeiling);

        var create = Walk<Button>(body).Single(b => (string?)b.Content == "Create session");
        ceiling.Text = "-1";
        Assert.False(create.IsEnabled);

        // Unparseable is "not written", said so — never read as a negative bound.
        ceiling.Text = "abc";
        Assert.Null(sheet.FanOutCeiling);
        Assert.False(create.IsEnabled);
        Assert.Contains("whole number", sheet.BlockedReason!, StringComparison.Ordinal);
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

        // The one gap an operator can still open from the sheet: a blank name (the class is
        // preselected and the picker has no deselect).
        var name = Walk<TextBox>(body).Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Session name");
        name.Text = string.Empty;

        var create = Walk<Button>(body).Single(b => (string?)b.Content == "Create session");
        Assert.False(create.IsEnabled);
        var row = LogicalTreeHelper.GetParent(LogicalTreeHelper.GetParent(create));

        var reason = Walk<TextBlock>(row).SingleOrDefault(
            t => t.Text == sheet.BlockedReason);

        // RQ5 — beside the button, naming the field. An inert control never leaves the operator
        // guessing, and a reason 200px away is a reason nobody connects to the button.
        Assert.True(reason is not null,
            "the disabled Create button's row does not carry its reason; it rendered as a footnote "
            + "under the buttons, in the vocabulary of the ranking subsystem");
        Assert.Equal(Visibility.Visible, reason!.Visibility);
    });

    /// <summary>
    /// The preselected value is a declared default, not a guessed one.
    /// </summary>
    /// <remarks>
    /// Ruling 19's rule was that a chosen class and a guessed one are indistinguishable afterwards;
    /// Ruling 72 resolves it the other way for the session default — <c>free-form</c> is the
    /// operator's own stated default, a legitimate cohort key (a session that opens with no task
    /// in mind), preselected as a visible row the operator can change. What must never happen is a
    /// value that is not <c>free-form</c> arriving unselected: the preselection is exactly the
    /// declared constant, and the dialog decides nothing the model does not already say.
    /// </remarks>
    [Fact]
    public void ThePreselectedClassIsExactlyTheDeclaredDefault() => Sta.Run(() =>
    {
        var sheet = Sheet();
        var body = NewSessionSheetDialog.Build(sheet, announce: null, onCreate: () => { });

        Assert.Equal(TaskClasses.FreeForm, sheet.TaskClass);
        Assert.Equal(TaskClasses.FreeForm, ((ListBoxItem)Assert.Single(Walk<ListBox>(body)).SelectedItem).Tag);
        Assert.True(sheet.TaskClassAnswered);
        Assert.Null(sheet.BlockedReason);

        // The falsifier: a dialog that hard-codes row 0 would pass the assertions above. With the
        // model holding another class before Build, that row — and only that row — is selected.
        var review = Sheet();
        review.TaskClass = "review";
        var reviewBody = NewSessionSheetDialog.Build(review, announce: null, onCreate: () => { });
        Assert.Equal("review", ((ListBoxItem)Assert.Single(Walk<ListBox>(reviewBody)).SelectedItem).Tag);
        Assert.Equal("review", review.TaskClass);
    });
}
