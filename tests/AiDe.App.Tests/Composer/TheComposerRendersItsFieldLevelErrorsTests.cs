using System.Text;
using System.Windows;
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

    private static ComposerSurface Build(string root, bool attachEnabled)
    {
        var surface = new ComposerSurface("composer:s-0001", "s-0001 — composer");

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

    [Fact]
    public void ARequiredFieldGapBlocksSendWithAFieldLevelErrorThatIsOnTheScreen()
    {
        Sta.Run(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "aide-render", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var surface = Build(root, attachEnabled: false);

                // The goal-block shape with nothing filled in: every one of the six fields blocks.
                surface.Draft.SwitchTo(ComposerShape.GoalBlock);

                Assert.Null(surface.Send());
                Assert.Equal(0, surface.Gate.SendCount);

                var rendered = RenderedText(surface);

                foreach (var field in GoalBlockFields.All)
                {
                    Assert.Contains(field, rendered, StringComparison.Ordinal);
                }

                // And it is the CONTRACT'S OWN message that is on screen, not a paraphrase the
                // surface invented — the same sentence the spawn contract would refuse with.
                var contractMessage = SpawnContract.Validate(surface.Draft.ToGoalBlock())[0].Message;
                Assert.Contains(contractMessage, rendered, StringComparison.Ordinal);
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
                    surface.MoveFocus();
                }

                surface.Draft.SwitchTo(ComposerShape.FreeForm);
                surface.Draft.SetFreeFormText("a prompt about @src/Payments\n");
                Assert.NotNull(surface.Send());

                Assert.Equal(0, surface.ClipboardReads);
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
