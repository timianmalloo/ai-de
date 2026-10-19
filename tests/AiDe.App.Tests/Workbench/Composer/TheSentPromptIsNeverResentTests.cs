using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using AiDe.App.Conductor;
using AiDe.App.Tests.Records;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Dispatch;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>Ruling 181 (2), the hard resend guard.</b> After a submit the compiled field shows what was sent — display-only.
/// The send takes <c>RenderedView ?? RenderView(draft, template)</c> (<c>ComposerSendGate.Send</c>), so a sent text that
/// reached the gate's view would go again on the next press. A second Ctrl+Enter with no keystroke sends nothing: no
/// turn, no dispatch, no attempt row, and the gate's own refusal. A queued send (Ruling 95) and a send held by the
/// restart read-back (Ruling 175) each show their own state, never <i>sent as</i>.
/// </summary>
public sealed class TheSentPromptIsNeverResentTests(ITestOutputHelper output) : IDisposable
{
    private const string Words = "why are we using grep? is graphify and obsidian vault not configured?";

    /// <summary>The gate's refusal of a send whose compiled bytes are empty (<c>ComposerSendGate.Send</c>).</summary>
    private const string EmptyPromptRefusal = "an empty prompt is not a task";

    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void ASecondCtrlEnterRightAfterASubmit_WithNoKeystroke_SendsNothing()
    {
        ComposerGesture.Drive(_fixture, async (window, document) =>
        {
            var composer = document.Composer;
            ComposerGesture.Open(composer);
            ComposerGesture.Type(composer, Words);
            ComposerGesture.Settle(document);
            ComposerGesture.CtrlEnter(composer);
            await document.LastBinding.WaitAsync(NotebookFixture.Bound);
            await document.LastLaunch.WaitAsync(NotebookFixture.Bound);

            // b1 ENDED FIRST: a press while it runs is Ruling 95's offer, which sends nothing either way. The guard is
            // proved only where a leaked view would really go — an idle session with an open block.
            await ComposerGesture.UntilAsync(window, () => document.Notebook.Thread.Current.InFlight is null, () => "b1 never ended");
            ComposerGesture.Settle(document);
            var b1 = Assert.Single(document.Notebook.Thread.Current.Turns);
            var attempts = ComposerGesture.AttemptRows(_fixture);
            var blocks = composer.Gate.BlocksSent;
            var launch = document.LastLaunch;
            Assert.Equal(b1.SentBytes, ComposerGesture.Field(composer).Text);
            Assert.Equal(ComposerSurface.SentHeading("b1"), ComposerGesture.Heading(composer).Header);

            ComposerGesture.CtrlEnter(composer);
            ComposerGesture.Settle(document);
            await Task.Delay(200);
            ComposerGesture.Settle(document);
            output.WriteLine($"after the second press: turns {document.Notebook.Thread.Current.Turns.Count} · attempt rows {ComposerGesture.AttemptRows(_fixture)} (was {attempts}) · blocks sent {composer.Gate.BlocksSent} (was {blocks}) · status \"{composer.Status}\" · field {ComposerGesture.Field(composer).Text.Length} chars · header \"{ComposerGesture.Heading(composer).Header}\"");

            Assert.Single(document.Notebook.Thread.Current.Turns);
            Assert.Equal(attempts, ComposerGesture.AttemptRows(_fixture));
            Assert.Equal(blocks, composer.Gate.BlocksSent);
            Assert.Same(launch, document.LastLaunch);
            Assert.Equal(EmptyPromptRefusal, composer.Status);

            // The display is unchanged by the refused press: still b1's bytes, still headed as sent.
            Assert.Equal(b1.SentBytes, ComposerGesture.Field(composer).Text);
            Assert.Equal(ComposerSurface.SentHeading("b1"), ComposerGesture.Heading(composer).Header);
        });
    }

    /// <summary>
    /// Ruling 95: a send while b1 runs, taken as <i>Wait — send after b1</i>, is queued, not sent. The field does not
    /// claim <i>sent as b2</i>: it shows the live new draft under the live heading, and the status line says b2 is queued.
    /// </summary>
    [Fact]
    public void AQueuedSend_ShowsItsQueuedState_NotTheSentState()
    {
        ComposerGesture.Drive(
            _fixture,
            async (window, document) =>
            {
                var composer = document.Composer;
                ComposerGesture.Open(composer);
                ComposerGesture.Type(composer, "first words");
                ComposerGesture.Settle(document);
                ComposerGesture.CtrlEnter(composer);
                await document.LastBinding.WaitAsync(NotebookFixture.Bound);
                ComposerGesture.Settle(document);
                Assert.NotNull(document.Notebook.Thread.Current.InFlight);
                Assert.Equal(ComposerSurface.SentHeading("b1"), ComposerGesture.Heading(composer).Header);

                ComposerGesture.Type(composer, "second words");
                ComposerGesture.CtrlEnter(composer);
                ComposerGesture.Settle(document);
                var wait = Link(composer, "Wait — send after b1");
                Assert.True(wait is not null, $"the status line offers no Wait action; it reads \"{composer.Status}\"");
                wait!.RaiseEvent(new RoutedEventArgs(Hyperlink.ClickEvent));
                ComposerGesture.Settle(document);

                var (header, name, itemStatus) = ComposerGesture.Heading(composer);
                var field = ComposerGesture.Field(composer);
                output.WriteLine($"after Wait: turns {string.Join(", ", document.Notebook.Thread.Current.Turns.Select(t => t.DisplayOrdinal + " " + t.State))} · status \"{composer.Status}\" · header \"{header}\" · name \"{name}\" · field {field.Text.Length} chars · draft \"{composer.Draft.SourceText}\"");

                Assert.Equal(TurnState.Queued, document.Notebook.Thread.Current.Turns[1].State);
                Assert.Equal("b2 queued — sends after b1", composer.Status);
                Assert.Equal(ComposerSurface.CompiledPromptHeading, header);
                Assert.Equal(ComposerSurface.CompiledPromptLiveName, name);
                Assert.Equal(string.Empty, itemStatus);
                Assert.Equal(string.Empty, composer.Draft.SourceText);
                Assert.Equal(string.Empty, field.Text);
            },
            releaseRun: false);
    }

    /// <summary>
    /// Ruling 175: a send while a reopened notebook's turns restore is held, not sent. The field keeps the live view of the
    /// words still in the editor under the live heading; a second press admits nothing. Once the turn is admitted as b1
    /// the field shows it as sent, and a further press sends nothing.
    /// </summary>
    [Fact]
    public void AHeldSend_ShowsItsWaitingWords_NotTheSentState_UntilItIsAdmitted()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
                _fixture.ReleaseRun();
                var opening = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                AttemptHost? opened = null;
                var model = _fixture.NewModel();
                model.Runs.BindHostWhenOpen(
                    async () =>
                    {
                        await opening.Task;
                        opened = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), ComposerGesture.LockFile(_fixture), CancellationToken.None);
                        return (IAttemptHost)opened;
                    },
                    ComposerGesture.Scope(_fixture));
                var document = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                try
                {
                    window.Content = document;
                    var composer = document.Composer;
                    ComposerGesture.ConfigureAsTheBinderDoes(_fixture, composer);
                    ComposerGesture.Settle(document);
                    Assert.False(document.LastReadBack.IsCompleted, "the read-back is not pending while the host opens");

                    ComposerGesture.Open(composer);
                    ComposerGesture.Type(composer, "held words");
                    ComposerGesture.Settle(document);
                    var live = ComposerGesture.Field(composer).Text;
                    ComposerGesture.CtrlEnter(composer);
                    ComposerGesture.Settle(document);
                    ComposerGesture.CtrlEnter(composer);
                    ComposerGesture.Settle(document);
                    var held = ComposerGesture.Heading(composer);
                    output.WriteLine($"held: turns {model.Thread.Current.Turns.Count} · status \"{composer.Status}\" · header \"{held.Header}\" · field {ComposerGesture.Field(composer).Text.Length} chars · draft \"{composer.Draft.SourceText}\"");

                    Assert.Empty(model.Thread.Current.Turns);
                    Assert.Equal("held words", composer.Draft.SourceText);
                    Assert.Equal(SessionDocumentSurface.RestoringTurns, composer.Status);
                    Assert.Contains("held words", live, StringComparison.Ordinal);
                    Assert.Equal(live, ComposerGesture.Field(composer).Text);
                    Assert.Equal(ComposerSurface.CompiledPromptHeading, held.Header);
                    Assert.Equal(ComposerSurface.CompiledPromptLiveName, held.Name);

                    opening.SetResult();
                    await ComposerGesture.UntilAsync(window, () => model.Runs.Host is not null, () => "the host never bound");
                    await document.LastReadBack.WaitAsync(NotebookFixture.Bound);
                    await document.LastAdmission.WaitAsync(NotebookFixture.Bound);
                    await ComposerGesture.UntilAsync(window, () => model.Thread.Current.Turns.Count == 1 && model.Thread.Current.InFlight is null, () => $"{model.Thread.Current.Turns.Count} turns after the admission");
                    ComposerGesture.Settle(document);
                    var b1 = Assert.Single(model.Thread.Current.Turns);
                    Assert.Equal(live, b1.SentBytes);
                    Assert.Equal(b1.SentBytes, ComposerGesture.Field(composer).Text);
                    Assert.Equal(ComposerSurface.SentHeading("b1"), ComposerGesture.Heading(composer).Header);

                    var attempts = ComposerGesture.AttemptRows(_fixture);
                    ComposerGesture.CtrlEnter(composer);
                    ComposerGesture.Settle(document);
                    output.WriteLine($"after the admission and a further press: turns {model.Thread.Current.Turns.Count} · attempt rows {ComposerGesture.AttemptRows(_fixture)} (was {attempts}) · status \"{composer.Status}\"");
                    Assert.Single(model.Thread.Current.Turns);
                    Assert.Equal(attempts, ComposerGesture.AttemptRows(_fixture));
                    Assert.Equal(EmptyPromptRefusal, composer.Status);
                    await document.LastLaunch.WaitAsync(NotebookFixture.Bound);
                }
                finally
                {
                    window.Content = null;
                    document.Dispose();
                    model.Dispose();
                    opened?.Dispose();
                }
            });
    }

    private static Hyperlink? Link(ComposerSurface composer, string name)
    {
        var status = ThreadFixtures.Visuals<TextBlock>(composer).Single(t => AutomationProperties.GetName(t) == "Send status");
        return status.Inlines.OfType<Hyperlink>().FirstOrDefault(l => AutomationProperties.GetName(l) == name);
    }
}
