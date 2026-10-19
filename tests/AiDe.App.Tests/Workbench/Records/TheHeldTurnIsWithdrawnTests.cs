using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Automation;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Tests.Conductor.Attempts;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Store.Records;

namespace AiDe.App.Tests.Records;

/// <summary>
/// Ruling 95's Cancel on a turn the host holds as its queued successor (seam req-01M59ERYY7362ASCQ4E20XA4MA; the Owner's
/// shape on report 8): <see cref="IAttemptHost.WithdrawAsync"/> writes the EXISTING terminal <c>Rejected</c> with cause
/// <c>never_dispatched</c> under the gate lock, so neither the drainer nor Send now launches it; a withdraw that loses the
/// launch race is refused with a stable code and the attempt stays live.
/// </summary>
public sealed class TheHeldTurnIsWithdrawnTests : IDisposable
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(60);
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private string LockFile => Path.Combine(_fixture.Root, ".aide", "attempt-host.lock");

    private AttemptScope Scope => new("ws-t8", _fixture.Store.CoreEpoch, SessionGeneration: 1);

    [Fact]
    public async Task WithdrawingAHeldSuccessor_WritesRejectedNeverDispatched_AndNothingLaunchesIt()
    {
        using var f = new AttemptHostFixture();
        using var host = await f.OpenAsync();

        // Held: its predecessor never completed (G1 rejected it), so only Send now would launch it.
        var rejected = await host.DispatchAsync(f.Dispatch("env-1", currentProjection: "edited-since"), CancellationToken.None);
        await rejected.Completion.WaitAsync(Bound);
        var queued = await host.DispatchAsync(f.Dispatch("env-2", queuedAfter: rejected.Receipt.Attempt.DispatchKey), CancellationToken.None);
        var key = queued.Receipt.Attempt.DispatchKey;

        var withdrawn = await host.WithdrawAsync(key, CancellationToken.None);
        var sentAfter = await host.SendNowAsync(key, CancellationToken.None);
        await Task.Delay(500);

        Assert.Equal((DispatchState.Rejected, "never_dispatched"), (withdrawn.Terminal?.State, withdrawn.Terminal?.Cause));
        Assert.Null(withdrawn.Dispatched);
        Assert.Equal(DispatchState.Rejected, (await queued.Completion.WaitAsync(Bound)).Terminal?.State);
        Assert.True(sentAfter.AlreadyAttempted, "Send now launched a withdrawn successor");
        Assert.Equal(0, f.PromptsReceived);
        Assert.DoesNotContain(key, host.Live);
    }

    [Fact]
    public async Task AWithdrawThatLosesTheLaunchRace_IsRefusedWithAStableCode_AndTheAttemptStaysLive()
    {
        using var f = new AttemptHostFixture(released: false);
        using var host = await f.OpenAsync();

        var rejected = await host.DispatchAsync(f.Dispatch("env-1", currentProjection: "edited-since"), CancellationToken.None);
        await rejected.Completion.WaitAsync(Bound);
        var queued = await host.DispatchAsync(f.Dispatch("env-2", queuedAfter: rejected.Receipt.Attempt.DispatchKey), CancellationToken.None);
        var key = queued.Receipt.Attempt.DispatchKey;

        // The launch claim is the one the drainer takes; Send now wins it here, deterministically.
        var launched = await host.SendNowAsync(key, CancellationToken.None);
        Assert.False(launched.AlreadyAttempted);
        await f.PromptsArrivedAsync();

        var refusal = await Assert.ThrowsAsync<AgentPlaneException>(() => host.WithdrawAsync(key, CancellationToken.None));

        Assert.Equal(AttemptHostCodes.WithdrawRefused, refusal.Code);
        Assert.Contains(key, host.Live);
        Assert.Null((await host.ReadAsync(key, CancellationToken.None))!.Terminal);

        f.Release();
        Assert.Equal(DispatchState.ResponseComplete, (await queued.Completion.WaitAsync(Bound)).Terminal?.State);
    }

    /// <summary>Test (1)'s UI half: the composer's Cancel on a host-held turn is the withdraw, and the turn reads the operator's act.</summary>
    [Fact]
    public void CancelOnAHostHeldTurn_WithdrawsIt_AndTheTurnReadsCancelledByYou()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
                using var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
                using var model = _fixture.NewModel();
                model.Runs.BindHost(host, Scope);
                using var document = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                window.Content = document;
                _fixture.ConfigureStub(document);
                var second = await QueueBehindARunningTurnAsync(window, document, model);

                await UntilAsync(window, () => document.Thread.Items.Count > 1, () => $"the feed holds {document.Thread.Items.Count} item(s), not the queued turn");
                var cancel = Button(document, 1, ThreadAnnouncementPolicy.ActionWord(TurnActionKind.Cancel));
                Assert.True(cancel is not null, "the host-held turn offers no Cancel");
                cancel!.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                await UntilAsync(window, () => model.Thread.Current.Turns[1].State == TurnState.Cancelled,
                    () => $"the withdrawn turn reads {model.Thread.Current.Turns[1].State}");

                var attempt = (await host.ReadAsync(second, CancellationToken.None))!;
                Assert.Equal((DispatchState.Rejected, "never_dispatched"), (attempt.Terminal?.State, attempt.Terminal?.Cause));
                Assert.Null(attempt.Dispatched);
                Assert.Equal("cancelled by you", TurnCopy.OutcomeWord(model.Thread.Current.Turns[1].State, null));

                _fixture.ReleaseRun();
                await UntilAsync(window, () => model.Thread.Current.Turns[0].State == TurnState.Answered,
                    () => $"the first turn reads {model.Thread.Current.Turns[0].State}");
                await Task.Delay(500);
                Assert.Equal(TurnState.Cancelled, model.Thread.Current.Turns[1].State);
                Assert.Null((await host.ReadAsync(second, CancellationToken.None))!.Dispatched);
                window.Content = null;
            });
    }

    /// <summary>Test (2): a host's own Rejected{never_dispatched} with no withdraw by the operator is a refusal, never "cancelled by you".</summary>
    [Fact]
    public void AHostRejectionOfAHeldTurn_WithNoWithdraw_ReadsRejected_NeverCancelled()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
                using var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
                using var model = _fixture.NewModel();
                model.Runs.BindHost(host, Scope);
                using var document = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                window.Content = document;
                _fixture.ConfigureStub(document);
                var second = await QueueBehindARunningTurnAsync(window, document, model);

                // The same terminal a withdraw writes, but no operator act: written by the store's own gate.
                IAttemptLedger ledger = new BoundaryDispatcher(_fixture.Store);
                await ledger.AppendAttemptOutcomeAsync(new OutcomeRow(second, DispatchState.Rejected, Cause: "never_dispatched"), CancellationToken.None);

                _fixture.ReleaseRun();
                await UntilAsync(window, () => model.Thread.Current.Turns.All(t => t.State is not (TurnState.Queued or TurnState.Running or TurnState.Preparing)),
                    () => $"the turns read {string.Join(", ", model.Thread.Current.Turns.Select(t => t.State))}");

                Assert.Equal(TurnState.Rejected, model.Thread.Current.Turns[1].State);
                Assert.NotEqual("cancelled by you", TurnCopy.OutcomeWord(model.Thread.Current.Turns[1].State, null));
                window.Content = null;
            });
    }

    private async Task<string> QueueBehindARunningTurnAsync(Window window, SessionDocumentSurface document, NotebookModel model)
    {
        document.Composer.Draft.SetFreeFormText("first");
        Assert.NotNull(document.Composer.Send());
        await UntilAsync(window, () => _fixture.RunOpened, () => "the first attempt never opened its session");

        document.Composer.Draft.SetFreeFormText("second");
        Assert.Null(document.Composer.Send());
        var wait = WaitLink(document.Composer);
        Assert.True(wait is not null, $"the status line offers no Wait; it reads \"{document.Composer.Status}\"");
        wait!.RaiseEvent(new RoutedEventArgs(Hyperlink.ClickEvent));
        var ordinal = model.Thread.Current.Turns[1].Ordinal;
        await UntilAsync(window, () => model.Runs.AttemptOf(ordinal) is not null, () => "the queued turn was never dispatched to the host");
        Assert.Equal(TurnState.Queued, model.Thread.Current.Turns[1].State);
        return model.Runs.AttemptOf(ordinal)!.DispatchKey;
    }

    private static Button? Button(SessionDocumentSurface document, int index, string word) =>
        ThreadFixtures.Visuals<Button>(ThreadFixtures.Container(document.Thread, index))
            .FirstOrDefault(b => b.Content is string s && s == word);

    private static Hyperlink? WaitLink(ComposerSurface composer) =>
        ThreadFixtures.Visuals<TextBlock>(composer).Single(t => AutomationProperties.GetName(t) == "Send status")
            .Inlines.OfType<Hyperlink>().FirstOrDefault(l => AutomationProperties.GetName(l)?.StartsWith("Wait", StringComparison.Ordinal) == true);

    private static async Task UntilAsync(Window window, Func<bool> condition, Func<string> what)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        }

        Assert.True(condition(), what());
    }
}
