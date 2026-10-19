using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using AiDe.App.Tests.Records;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench.Composer;
using AiDe.Core.Presentation.Sessions;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>T28 n4 — the heading never states something false (Ruling 181 under the Owner's direction).</b> A queued send's
/// field is headed <i>queued as b2</i> (n3). When b1 completes and the drain sends b2, that is no longer true: the field,
/// still showing b2 because nothing was typed, is headed <i>sent as b2</i>. When the drain is refused (b1 failed, Ruling 95)
/// b2 is still queued and the heading stays. Every test takes the user's gesture on the product's composition
/// (<see cref="ComposerGesture"/>) with b1 in flight.
/// </summary>
public sealed class TheCompiledFieldAfterTheQueueDrainsTests(ITestOutputHelper output) : IDisposable
{
    private const string Second = "why are we using grep? is graphify and obsidian vault not configured?";

    /// <summary>The gate's refusal of a send whose compiled bytes are empty (<c>ComposerSendGate.Send</c>).</summary>
    private const string EmptyPromptRefusal = "an empty prompt is not a task";

    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    /// <summary>
    /// (A) b1 completes, the queue drains, b2 is sent, and nothing was typed since: the header, the field's UIA name and the
    /// disclosure's ItemStatus read <i>sent as b2</i>, and the field still holds b2's bytes.
    /// </summary>
    [Fact]
    public void WhenTheQueueDrainsAndB2IsSent_WithNoKeystroke_TheHeadingSwitchesToSentAsB2()
    {
        ComposerGesture.Drive(
            _fixture,
            async (window, document) =>
            {
                var composer = document.Composer;
                await QueueB2Async(document);
                var queued = ComposerGesture.Heading(composer).Header;

                await DrainAsync(window, document);
                var field = ComposerGesture.Field(composer);
                var (header, name, itemStatus) = ComposerGesture.Heading(composer);
                var b2 = document.Notebook.Thread.Current.Turns[1];
                output.WriteLine($"before the drain: header \"{queued}\" · after it: turns {string.Join(", ", document.Notebook.Thread.Current.Turns.Select(t => t.DisplayOrdinal + " " + t.State))} · header \"{header}\" · name \"{name}\" · ItemStatus \"{itemStatus}\" · field {field.Text.Length} chars (b2 holds {b2.SentBytes.Length})");

                Assert.Equal(ComposerSurface.QueuedHeading("b2"), queued);
                Assert.Equal(ComposerSurface.SentHeading("b2"), header);
                Assert.Equal(ComposerSurface.SentHeading("b2"), name);
                Assert.Equal(ComposerSurface.SentHeading("b2"), itemStatus);
                Assert.Equal(b2.SentBytes, field.Text);
            },
            releaseRun: false);
    }

    /// <summary>(A) If the user typed since the queued send, the drain changes nothing: the live draft stays under the live heading.</summary>
    [Fact]
    public void WhenTheQueueDrains_AfterTheUserTyped_TheLiveDraftStays()
    {
        ComposerGesture.Drive(
            _fixture,
            async (window, document) =>
            {
                var composer = document.Composer;
                await QueueB2Async(document);
                ComposerGesture.Type(composer, "third words");
                ComposerGesture.Settle(document);

                await DrainAsync(window, document);
                var field = ComposerGesture.Field(composer);
                var (header, name, itemStatus) = ComposerGesture.Heading(composer);
                output.WriteLine($"after the drain, typed since: header \"{header}\" · name \"{name}\" · ItemStatus \"{itemStatus}\" · field \"{field.Text}\"");

                Assert.Equal(ComposerSurface.CompiledPromptHeading, header);
                Assert.Equal(ComposerSurface.CompiledPromptLiveName, name);
                Assert.Equal(string.Empty, itemStatus);
                Assert.Contains("third words", field.Text, StringComparison.Ordinal);
                Assert.DoesNotContain(Second, field.Text, StringComparison.Ordinal);
            },
            releaseRun: false);
    }

    /// <summary>
    /// (B) b1 fails, so the drain holds b2 (Ruling 95: it sends only after Completed or Answered). b2 is still queued, and
    /// the heading, the UIA name and the ItemStatus still read <i>queued as b2</i>.
    /// </summary>
    [Fact]
    public void WhenB1Fails_AndTheDrainIsRefused_TheHeadingStaysQueuedAsB2()
    {
        ComposerGesture.Drive(
            _fixture,
            async (window, document) =>
            {
                var composer = document.Composer;
                Conductor.StubAcpAdapter.Install(Path.Combine(_fixture.Root, "install"), new Conductor.StubAcpAdapter.Mode(FailAtInitialize: true));
                await QueueB2Async(document);

                _fixture.ReleaseRun();
                await ComposerGesture.UntilAsync(
                    window,
                    () => document.Notebook.Thread.Current.Turns[0].State == TurnState.Failed,
                    () => $"b1 never failed: turns {string.Join(", ", document.Notebook.Thread.Current.Turns.Select(t => t.DisplayOrdinal + " " + t.State))}");
                await Task.Delay(300);
                ComposerGesture.Settle(document);

                var turns = document.Notebook.Thread.Current.Turns;
                var field = ComposerGesture.Field(composer);
                var (header, name, itemStatus) = ComposerGesture.Heading(composer);
                output.WriteLine($"after b1 failed: turns {string.Join(", ", turns.Select(t => t.DisplayOrdinal + " " + t.State))} · header \"{header}\" · name \"{name}\" · ItemStatus \"{itemStatus}\" · field {field.Text.Length} chars");

                Assert.Equal(TurnState.Queued, turns[1].State);
                Assert.Equal(ComposerSurface.QueuedHeading("b2"), header);
                Assert.Equal(ComposerSurface.QueuedHeading("b2"), name);
                Assert.Equal(ComposerSurface.QueuedHeading("b2"), itemStatus);
                Assert.Equal(turns[1].SentBytes, field.Text);

                // The held b2 is cancelled so the run's tail has no queued launch to wait for.
                ThreadFixtures.Visuals<ButtonBase>(ThreadFixtures.Container(document.Thread, 1)).First(b => b.Content is "Cancel").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                ComposerGesture.Settle(document);
            },
            releaseRun: false);
    }

    /// <summary>
    /// (C) After the switch to <i>sent as b2</i>, a press with no keystroke sends nothing: no turn, no attempt row, no
    /// block, the gate's own refusal, and the heading stays.
    /// </summary>
    [Fact]
    public void AfterTheSwitchToSentAsB2_ACtrlEnterWithNoKeystroke_SendsNothing()
    {
        ComposerGesture.Drive(
            _fixture,
            async (window, document) =>
            {
                var composer = document.Composer;
                await QueueB2Async(document);
                await DrainAsync(window, document);
                Assert.Equal(ComposerSurface.SentHeading("b2"), ComposerGesture.Heading(composer).Header);
                var attempts = ComposerGesture.AttemptRows(_fixture);
                var blocks = composer.Gate.BlocksSent;

                ComposerGesture.CtrlEnter(composer);
                ComposerGesture.Settle(document);
                await Task.Delay(200);
                ComposerGesture.Settle(document);
                var turns = document.Notebook.Thread.Current.Turns;
                var (header, _, _) = ComposerGesture.Heading(composer);
                output.WriteLine($"after the press: turns {string.Join(", ", turns.Select(t => t.DisplayOrdinal + " " + t.State))} · attempt rows {ComposerGesture.AttemptRows(_fixture)} (was {attempts}) · blocks sent {composer.Gate.BlocksSent} (was {blocks}) · status \"{composer.Status}\" · header \"{header}\"");

                Assert.Equal(2, turns.Count);
                Assert.Equal(attempts, ComposerGesture.AttemptRows(_fixture));
                Assert.Equal(blocks, composer.Gate.BlocksSent);
                Assert.Equal(EmptyPromptRefusal, composer.Status);
                Assert.Equal(ComposerSurface.SentHeading("b2"), header);
            },
            releaseRun: false);
    }

    /// <summary>b1 typed and sent, left in flight; b2 typed, Ctrl+Enter, Wait; the disclosure opened on b2's queued view.</summary>
    private static async Task QueueB2Async(AiDe.App.Workbench.Sessions.SessionDocumentSurface document)
    {
        var composer = document.Composer;
        ComposerGesture.Type(composer, "first words");
        ComposerGesture.Settle(document);
        ComposerGesture.CtrlEnter(composer);
        await document.LastBinding.WaitAsync(NotebookFixture.Bound);
        ComposerGesture.Settle(document);
        Assert.NotNull(document.Notebook.Thread.Current.InFlight);

        ComposerGesture.Type(composer, Second);
        ComposerGesture.Settle(document);
        ComposerGesture.CtrlEnterThenWait(composer, "b1");
        ComposerGesture.Settle(document);
        ComposerGesture.Open(composer);
        ComposerGesture.Settle(document);
        Assert.Equal(TurnState.Queued, document.Notebook.Thread.Current.Turns[1].State);
    }

    /// <summary>Lets b1 complete; the drain sends b2 and b2 completes. Returns when the session is idle with both turns run.</summary>
    private async Task DrainAsync(System.Windows.Window window, AiDe.App.Workbench.Sessions.SessionDocumentSurface document)
    {
        _fixture.ReleaseRun();
        await ComposerGesture.UntilAsync(
            window,
            () => document.Notebook.Thread.Current is { InFlight: null, Queued: null, Turns.Count: 2 } && ComposerGesture.AttemptRows(_fixture) == 2,
            () => $"b1 and b2 never both ran: turns {string.Join(", ", document.Notebook.Thread.Current.Turns.Select(t => t.DisplayOrdinal + " " + t.State))} · attempt rows {ComposerGesture.AttemptRows(_fixture)}");
        await document.LastLaunch.WaitAsync(NotebookFixture.Bound);
        ComposerGesture.Settle(document);
    }
}
