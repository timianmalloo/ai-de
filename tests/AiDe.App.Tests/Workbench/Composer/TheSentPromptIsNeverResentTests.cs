using System.IO;
using System.Windows;
using System.Windows.Controls;
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
/// turn, no dispatch, no attempt row, and the gate's own refusal. A queued send (Ruling 95) shows its bytes headed
/// <i>queued as b{n}</i> under the same guard (T28 n3); a send held by the restart read-back (Ruling 175) shows its own
/// state, never <i>sent as</i>.
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
            output.WriteLine($"before the second press: field {ComposerGesture.Field(composer).Text.Length} chars · header \"{ComposerGesture.Heading(composer).Header}\" · draft {composer.Draft.SourceText.Length} chars");

            // The guard first: what a second press SENDS is asserted before what the field SHOWS.
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
    /// Ruling 181 (2) for a queued send (T28 n3): the field shows b2's bytes headed <i>queued as b2</i>, display-only. A
    /// second Ctrl+Enter right after the queued send, with no keystroke, queues nothing and sends nothing — no second
    /// queued row, no dispatch, no attempt row — and shows Ruling 95's own refusal. Once b1 and then b2 have run, a further
    /// press with no keystroke still sends nothing: the shown text never reached the gate's view, so the gate refuses the
    /// empty draft and never sends b2's bytes as b3.
    /// </summary>
    [Fact]
    public void ASecondCtrlEnterRightAfterAQueuedSend_WithNoKeystroke_QueuesNothingAndSendsNothing()
    {
        ComposerGesture.Drive(
            _fixture,
            async (window, document) =>
            {
                var composer = document.Composer;
                ComposerGesture.Type(composer, "first words");
                ComposerGesture.Settle(document);
                ComposerGesture.CtrlEnter(composer);
                await document.LastBinding.WaitAsync(NotebookFixture.Bound);
                ComposerGesture.Settle(document);
                Assert.NotNull(document.Notebook.Thread.Current.InFlight);

                ComposerGesture.Type(composer, Words);
                ComposerGesture.Settle(document);
                ComposerGesture.CtrlEnterThenWait(composer, "b1");
                await document.LastBinding.WaitAsync(NotebookFixture.Bound);
                ComposerGesture.Settle(document);
                var b2 = document.Notebook.Thread.Current.Turns[1];
                var attempts = ComposerGesture.AttemptRows(_fixture);
                var blocks = composer.Gate.BlocksSent;
                var launch = document.LastLaunch;

                // The guard first: what a second press SENDS is asserted before what the field SHOWS.
                ComposerGesture.CtrlEnter(composer);
                ComposerGesture.Settle(document);
                await Task.Delay(200);
                ComposerGesture.Settle(document);
                var turns = document.Notebook.Thread.Current.Turns;
                var shown = (ComposerGesture.Field(composer).Text, ComposerGesture.Heading(composer));
                output.WriteLine($"after the second press: turns {string.Join(", ", turns.Select(t => t.DisplayOrdinal + " " + t.State))} · attempt rows {ComposerGesture.AttemptRows(_fixture)} (was {attempts}) · blocks sent {composer.Gate.BlocksSent} (was {blocks}) · status \"{composer.Status}\" · field {shown.Text.Length} chars · header \"{shown.Item2.Header}\"");

                Assert.Equal(2, turns.Count);
                Assert.Single(turns, t => t.State == TurnState.Queued);
                Assert.Equal(attempts, ComposerGesture.AttemptRows(_fixture));
                Assert.Equal(blocks, composer.Gate.BlocksSent);
                Assert.Same(launch, document.LastLaunch);
                Assert.Equal("b2 is queued; cancel it or wait.", composer.Status);

                // b1 ENDS, THE DRAIN SENDS b2, b2 ENDS: the session is idle with an open block, where a leaked view would
                // really go. A press with no keystroke since the queued send sends nothing.
                _fixture.ReleaseRun();
                await ComposerGesture.UntilAsync(
                    window,
                    () => document.Notebook.Thread.Current is { InFlight: null, Queued: null, Turns.Count: 2 } && ComposerGesture.AttemptRows(_fixture) == 2,
                    () => $"b1 and b2 never both ran: turns {string.Join(", ", document.Notebook.Thread.Current.Turns.Select(t => t.DisplayOrdinal + " " + t.State))} · attempt rows {ComposerGesture.AttemptRows(_fixture)}");
                await document.LastLaunch.WaitAsync(NotebookFixture.Bound);
                ComposerGesture.Settle(document);
                var drained = ComposerGesture.AttemptRows(_fixture);
                var drainedBlocks = composer.Gate.BlocksSent;
                ComposerGesture.CtrlEnter(composer);
                ComposerGesture.Settle(document);
                await Task.Delay(200);
                ComposerGesture.Settle(document);
                output.WriteLine($"after the drain and a further press: turns {string.Join(", ", document.Notebook.Thread.Current.Turns.Select(t => t.DisplayOrdinal + " " + t.State))} · attempt rows {ComposerGesture.AttemptRows(_fixture)} (was {drained}) · blocks sent {composer.Gate.BlocksSent} (was {drainedBlocks}) · status \"{composer.Status}\"");

                Assert.Equal(2, document.Notebook.Thread.Current.Turns.Count);
                Assert.Equal(drained, ComposerGesture.AttemptRows(_fixture));
                Assert.Equal(drainedBlocks, composer.Gate.BlocksSent);
                Assert.Equal(EmptyPromptRefusal, composer.Status);

                // The display after the refused press right after the queue: still b2's bytes, still headed as queued.
                Assert.Equal(b2.SentBytes, shown.Text);
                Assert.Equal(ComposerSurface.QueuedHeading("b2"), shown.Item2.Header);
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
}
