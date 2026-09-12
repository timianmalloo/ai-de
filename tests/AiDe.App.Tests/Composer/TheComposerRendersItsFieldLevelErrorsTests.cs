using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// A required-field gap <b>blocks send with a field-level error that is on the screen</b>, and the
/// attach affordance stays visible and disabled naming the setting that governs it (C21(b)).
/// </summary>
/// <remarks>
/// <b>This is the render half, and it is why the reflection guard in
/// <c>BoundsReachTheSurfaceTests</c> now lists <c>ComposerFieldError.Message</c> as covered.</b>
/// Reflection can see that a record has a field; only rendering can see whether a surface showed it.
/// The tree is walked rather than the wiring read — on this exact question a reading has been wrong
/// three times in one day.
/// </remarks>
public sealed class TheComposerRendersItsFieldLevelErrorsTests
{
    private sealed class NeverAsked : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
    }

    private static ComposerSendContext Context(string root) => new(
        RepositoryRoot: root,
        DataDirectory: root,
        AdapterInstallRoot: root,
        EngineId: "claude-code",
        Model: "sonnet",
        AccountLabel: "max-personal",
        TaskClass: "implement",
        ProofPackArtifacts: [],
        Providers: []);

    private static ComposerSurface Build(string root, bool attachEnabled, AiDe.App.Workbench.IWorkbenchAnnouncer? announcer = null)
    {
        var surface = new ComposerSurface("composer:s-0001", "s-0001 — composer", announcer);

        surface.Configure(
            new SessionConfig("s-0001", "first", "w-1", DateTimeOffset.UnixEpoch, ["claude-code"])
            {
                AttachEnabled = attachEnabled,
            },
            Context(root),
            ComposerFields.GoalBlock(),
            new AttachmentGate(root, new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max-personal"));

        return surface;
    }

    /// <summary>
    /// <b>Red observed before the change</b> (recorded in <c>docs/proof/composer-as-conversation.md</c>):
    /// the old form rendered six field names and the contract refused four of them (tier, fan-out
    /// cap, budget, not-in-scope). Now the three content lines are on screen, the session supplies
    /// the other three, and the one refusal is Ruling 75's sentence on the named line.
    /// </summary>
    [Fact]
    public void ARequiredFieldGapBlocksSendWithAFieldLevelErrorThatIsOnTheScreen()
    {
        Sta.Run(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "aide-render", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var announcer = new AiDe.App.Workbench.RecordingAnnouncer();
                var surface = Build(root, attachEnabled: false, announcer);

                // The goal-block form with nothing filled in is an empty prompt (Ruling 75 makes a
                // blank Goal a Message, and a Message with no words is not a task).
                surface.Draft.SwitchTo(ComposerShape.GoalBlock);

                Assert.Null(surface.Send());
                Assert.Equal(0, surface.Gate.SendCount);
                Assert.Equal("an empty prompt is not a task", surface.Status);

                // SPOKEN, NOT ONLY WRITTEN (SC6 / SC9; WCAG 4.1.3): a refusal reaches the announcer
                // assertively. Red observed: `Assert.Single() Failure: The collection was empty` —
                // the status line was a TextBlock with a LiveSetting and nothing ever raised the event.
                var refused = Assert.Single(announcer.Announcements);
                Assert.Equal("an empty prompt is not a task", refused.Text);
                Assert.Equal(AiDe.Core.Presentation.Sessions.Urgency.Assertive, refused.Urgency);

                // A goal block that EXISTS (Goal and Done when written) with Not in scope blank:
                // the one remaining gap blocks, by name, on screen — and nothing else does, because
                // tier, fan-out cap and budget are the session's (Rulings 56, 63, 72).
                surface.SetFieldText(surface.Fields[0].Id, 1, "Rename the helper in @src/Payments/Money.cs.");
                surface.Draft.SetGoalValue(GoalBlockFields.GoalKey, "Rename the helper.");
                surface.Draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "It compiles.");

                Assert.Null(surface.Send());
                Assert.Equal(0, surface.Gate.SendCount);

                var rendered = RenderedText(surface);

                // THREE CONTENT LINES ON SCREEN — never a tier, cap or budget box.
                Assert.Contains("Goal", rendered, StringComparison.Ordinal);
                Assert.Contains("Done when", rendered, StringComparison.Ordinal);
                Assert.Contains("Not in scope", rendered, StringComparison.Ordinal);
                Assert.Equal(ComposerDraft.PerPromptGoalFields.Order(StringComparer.Ordinal), surface.StructureMarks.Keys.Order(StringComparer.Ordinal));

                // The not-in-scope gap is Ruling 75's one sentence, on screen verbatim, and the line
                // is marked invalid (SC10: the mark is the line's ItemStatus, the reason its HelpText).
                Assert.Contains(ComposerCompiler.GoalBlockNeedsNotInScope, rendered, StringComparison.Ordinal);
                Assert.Equal("! invalid", surface.StructureMarks[GoalBlockFields.NotInScopeKey]);
                Assert.Equal("\u2713 edited", surface.StructureMarks[GoalBlockFields.GoalKey]);

                // And the contract agrees it is the ONE gap: the session's three values are complete.
                var contractErrors = SpawnContract.Validate(surface.Draft.ToGoalBlock());
                var error = Assert.Single(contractErrors);
                Assert.Equal(GoalBlockFields.NotInScopeKey, error.Field);

                // Filling it sends, with nothing typed for tier, fan-out cap or budget.
                surface.Draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "Nothing else.");
                var request = surface.Send();
                Assert.NotNull(request);
                Assert.Equal("T1", request!.Goal!.Tier);
                Assert.Equal(2, request.Goal.FanOutCap);
                Assert.True(request.Goal.Budget!.IsSubscriptionBounded);
                Assert.Equal("sent", surface.Status);

                // "sent" is a status: queued, never interrupting (SC9).
                Assert.Equal("sent", announcer.Announcements[^1].Text);
                Assert.Equal(AiDe.Core.Presentation.Sessions.Urgency.Status, announcer.Announcements[^1].Urgency);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        });
    }

    /// <summary>
    /// SC4 / U13: the tilde and the inferred ink mark a MODEL-derived value only — a rule's value
    /// is text in the text ink. Over a row of each source, and over the rest-state composer, whose
    /// tier comes from the rule (no tilde anywhere on it). <b>Red observed by mutation</b> (the
    /// old rule, <c>row.Name == "tier"</c>): see the Proof Pack's row for the failure text.
    /// </summary>
    [Fact]
    public void TheTildeAndTheInferredInkMarkAModelDerivedValueOnly()
    {
        Sta.Run(() =>
        {
            var theme = ThemeProbe.AppTheme();
            Color Ink(TextBlock block)
            {
                block.Resources = theme;
                return ThemeProbe.Ink(block);
            }

            var rule = ComposerSurface.DecorationValue(new AiDe.Core.Presentation.Sessions.DecorationRow("tier", "T1", "rule", "goal block filled by you, one lease"));
            Assert.Equal("T1", rule.Text);
            Assert.Equal(ThemeProbe.Token(theme, "TextBrush"), Ink(rule));

            var model = ComposerSurface.DecorationValue(new AiDe.Core.Presentation.Sessions.DecorationRow("tier", "T1", ComposerSurface.ModelSource, "the model filled Goal and Done when; one lease"));
            Assert.Equal("~ T1", model.Text);
            Assert.Equal(ThemeProbe.Token(theme, "InferredBrush"), Ink(model));

            // A derived (mechanical) lease is text too — "derived" is not the model.
            var lease = ComposerSurface.DecorationValue(new AiDe.Core.Presentation.Sessions.DecorationRow("lease", "src/**", "derived", "from your mention"));
            Assert.Equal("src/**", lease.Text);
            Assert.Equal(ThemeProbe.Token(theme, "TextBrush"), Ink(lease));

            // The rest-state composer: every decoration value is text, none wears a tilde.
            var root = Path.Combine(Path.GetTempPath(), "aide-tilde", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var surface = Build(root, attachEnabled: false);
                surface.Resources = theme;
                _ = RenderedText(surface);
                var line = Logical<WrapPanel>(surface).Single(p => AutomationProperties.GetName(p) == "This turn");
                var values = Logical<TextBlock>(line).Where(t => AutomationProperties.GetName(t).StartsWith("tier ", StringComparison.Ordinal)).ToList();
                var tier = Assert.Single(values);
                Assert.Equal("T0", tier.Text);
                Assert.DoesNotContain(Logical<TextBlock>(line), t => t.Text.StartsWith("~ ", StringComparison.Ordinal));
                Assert.Equal(ThemeProbe.Token(theme, "TextBrush"), ThemeProbe.Ink(tier));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        });
    }

    /// <summary>
    /// Ruling 72 condition (1) on the rendered surface: the settings line reads the budget as a state
    /// — <i>bounded by your subscription</i> — and no numeral stands in for the absent cap anywhere
    /// the composer renders.
    /// </summary>
    [Fact]
    public void TheSettingsLineRendersBoundedByYourSubscriptionWithNoNumeral()
    {
        Sta.Run(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "aide-render", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var surface = Build(root, attachEnabled: false);
                surface.Draft.SwitchTo(ComposerShape.GoalBlock);

                var rendered = RenderedText(surface);

                Assert.Contains("budget: bounded by your subscription", rendered, StringComparison.Ordinal);
                Assert.Contains("T0 \u2014 the ceiling of 2 does not apply to this turn", rendered, StringComparison.Ordinal);
                Assert.DoesNotContain(int.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture), rendered, StringComparison.Ordinal);
                Assert.DoesNotContain(long.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture), rendered, StringComparison.Ordinal);

                // The decoration line in SC2's grammar: This turn · class · tier · lease · shape.
                Assert.Contains("This turn", rendered, StringComparison.Ordinal);
                Assert.Equal(["class", "tier", "lease", "shape"], surface.Decorations.Select(d => d.Name));
                Assert.Equal("implement", surface.Decorations[0].Value);
                Assert.Equal("session-default", surface.Decorations[0].Source);
                Assert.Equal("message", surface.Decorations[3].Value);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        });
    }

    [Fact]
    public void C21b_TheAttachAffordanceStaysVisibleAndDisabledNamingTheSettingThatGovernsIt()
    {
        Sta.Run(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "aide-render", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var off = Build(root, attachEnabled: false);
                var attachOff = FindButton(off, "Attach");

                // VISIBLE, not absent. An absent affordance is indistinguishable from an unbuilt
                // feature, and a disabled one with no reason is indistinguishable from a bug.
                Assert.NotNull(attachOff);
                Assert.Equal(Visibility.Visible, attachOff!.Visibility);
                Assert.False(attachOff.IsEnabled);
                Assert.Contains("Attach files", (string)attachOff.ToolTip, StringComparison.Ordinal);
                Assert.Contains("session", (string)attachOff.ToolTip, StringComparison.OrdinalIgnoreCase);

                var on = Build(root, attachEnabled: true);
                var attachOn = FindButton(on, "Attach");
                Assert.NotNull(attachOn);
                Assert.True(attachOn!.IsEnabled);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        });
    }

    [Fact]
    public void TheClipboardIsNeverReadAcrossASessionOfTypingFocusChangesAndSends()
    {
        Sta.Run(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "aide-render", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var surface = Build(root, attachEnabled: false);

                // A whole session's worth of the three gestures the rider names: typing, focus
                // changes, and sends. No polling, no history, no paste-on-focus.
                for (var i = 1; i <= 50; i++)
                {
                    surface.SetFieldText(surface.Fields[0].Id, i, $"line {i} about @src/Payments");
                    surface.MoveFocus(backward: false);
                }

                surface.Draft.SwitchTo(ComposerShape.FreeForm);
                surface.Draft.SetFreeFormText("a prompt about @src/Payments\n");
                Assert.NotNull(surface.Send());

                Assert.Equal(0, surface.ClipboardReads);

                // And the committed-channel record this send would write is counts plus one boolean —
                // asserted HERE, at the surface, because that is where a real send produces it. With
                // attach off, `count: 0` alone is indistinguishable from "the operator chose not to";
                // the boolean is the difference between "could not" and "chose not".
                var record = surface.CommittedRecord();
                Assert.False(record["attach_enabled"]!.GetValue<bool>());
                Assert.Equal(0, record["attachments"]!["count"]!.GetValue<int>());
                Assert.DoesNotContain("@src/Payments", record.ToJsonString(), StringComparison.Ordinal);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        });
    }

    [Fact]
    public void ThePickerOffersEveryCatalogCardAndChoosingOneReMintsTheForm()
    {
        Sta.Run(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "aide-render", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var surface = new ComposerSurface("composer:s-0002", "s-0002 — composer");
                var catalog = TemplateCatalog.BuiltIn();

                surface.Configure(
                    new SessionConfig("s-0002", "second", "w-1", DateTimeOffset.UnixEpoch, ["claude-code"]),
                    Context(root),
                    ComposerFields.GoalBlock(),
                    new AttachmentGate(root, new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max-personal"),
                    catalog);

                var staleId = surface.Fields[0].Id;

                Assert.Equal(12, surface.TemplateCards.Count);
                Assert.All(surface.TemplateCards, card =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(card.Headline));
                    Assert.False(string.IsNullOrWhiteSpace(card.Detail));
                });

                var chosen = catalog.Entries.First(e => e.IsEnabled);
                surface.ChooseTemplate(chosen.Id);

                Assert.Equal(ComposerShape.Template, surface.Draft.Shape);
                Assert.Equal(chosen.Id, surface.Draft.TemplateId);
                Assert.DoesNotContain(surface.Fields, f => f.Id == staleId);

                // THE FORM IS RE-MINTED. A field id from the goal-block form is no longer one the host
                // holds, so an update naming it reaches nothing — which is the same rule as "the page
                // may only MATCH a host-minted id", applied to a form that changed under it.
                surface.SetFieldText(staleId, 1, "written into a field that is not there");
                Assert.Empty(surface.Draft.GoalValues);

                // And a required field still blocks the send with a field-level error.
                Assert.Null(surface.Send());
                Assert.Equal(0, surface.Gate.SendCount);
                Assert.NotEqual(string.Empty, surface.Status);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        });
    }

    [Fact]
    public void TheCompiledViewOnScreenIsTheWholePromptAndNotASummary()
    {
        Sta.Run(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "aide-render", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var surface = Build(root, attachEnabled: false);

                var body = string.Join("\n", Enumerable.Range(0, 400).Select(i => $"line {i} of the prompt"));
                surface.Draft.SetFreeFormText(body + "\n@src/Payments\n");
                surface.Draft.SwitchTo(ComposerShape.FreeForm);

                var request = surface.Send();

                Assert.NotNull(request);
                Assert.Equal(request!.Prompt, surface.CompiledView);
                Assert.Contains("line 399 of the prompt", surface.CompiledView, StringComparison.Ordinal);
                Assert.Equal(request.Prompt.Length, surface.CompiledView.Length);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        });
    }

    private static Button? FindButton(DependencyObject root, string startsWith)
    {
        if (root is Button button && button.Content is string content
            && content.StartsWith(startsWith, StringComparison.Ordinal))
        {
            return button;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            if (FindButton(VisualTreeHelper.GetChild(root, i), startsWith) is { } found)
            {
                return found;
            }
        }

        if (root is ContentControl host && host.Content is DependencyObject child)
        {
            return FindButton(child, startsWith);
        }

        return null;
    }

    private static IEnumerable<T> Logical<T>(DependencyObject node) where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())
        {
            if (child is T match)
            {
                yield return match;
            }

            foreach (var inner in Logical<T>(child))
            {
                yield return inner;
            }
        }
    }

    private static string RenderedText(FrameworkElement root)
    {
        root.Measure(new Size(1200, 900));
        root.Arrange(new Rect(0, 0, 1200, 900));
        root.UpdateLayout();

        var text = new StringBuilder();
        Harvest(root, text);
        return text.ToString();
    }

    private static void Harvest(DependencyObject node, StringBuilder text)
    {
        switch (node)
        {
            case TextBlock block:
                text.Append(block.Text).Append('\n');
                break;
            case TextBox box:
                text.Append(box.Text).Append('\n');
                break;
            case ContentControl { Content: string content }:
                text.Append(content).Append('\n');
                break;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            Harvest(VisualTreeHelper.GetChild(node, i), text);
        }

        if (node is ContentControl { Content: DependencyObject child })
        {
            Harvest(child, text);
        }
    }
}
