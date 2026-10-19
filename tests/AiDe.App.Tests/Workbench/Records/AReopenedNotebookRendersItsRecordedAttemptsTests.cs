using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench;
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
/// Ruling 163 (T17): a reopened notebook renders its recorded attempts. "Restart" is a new host, a new model and a new
/// surface over the same store and the same session envelope file; every assertion reads the real surface's rows.
/// </summary>
public sealed class AReopenedNotebookRendersItsRecordedAttemptsTests : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private string LockFile => Path.Combine(_fixture.Root, ".aide", "attempt-host.lock");

    private AttemptScope Scope => new("ws-t17", _fixture.Store.CoreEpoch, SessionGeneration: 1);

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

    private static bool Ended(NotebookModel model, int index) =>
        model.Thread.Current.Turns.Count > index && model.Thread.Current.Turns[index].State is not (TurnState.Running or TurnState.Preparing);

    /// <summary>A new surface over a new model and a new host; the read-back awaited and its rows rendered.</summary>
    private async Task<(AttemptHost Host, NotebookModel Model, SessionDocumentSurface Surface)> ReopenAsync(Window window, IAttemptLedger? ledger = null)
    {
        var host = await AttemptHost.OpenAsync(ledger ?? new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
        var model = _fixture.NewModel();
        model.Runs.BindHost(host, Scope);
        var surface = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
        window.Content = surface;
        _fixture.ConfigureStub(surface);
        await surface.LastReadBack.WaitAsync(NotebookFixture.Bound);
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        return (host, model, surface);
    }

    private static void Close(Window window, AttemptHost host, NotebookModel model, SessionDocumentSurface surface)
    {
        window.Content = null;
        surface.Dispose();
        model.Dispose();
        host.Dispose();
    }

    [Fact]
    public void AReopenedNotebook_RendersOneRowPerRecordedAttempt_InDispatchOrder_AndTheNextTurnContinuesAfterThem()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
                _fixture.ReleaseRun();

                // A restart with ZERO recorded attempts: no row, and the first turn is b1 (no off-by-one).
                var (host, model, surface) = await ReopenAsync(window);
                Close(window, host, model, surface);
                (host, model, surface) = await ReopenAsync(window);
                Assert.Empty(surface.Thread.Rows);
                surface.Composer.Draft.SetFreeFormText("first words");
                await NotebookFixture.SendAndSettleAsync(surface);
                await UntilAsync(window, () => Ended(model, 0), () => "A never ended");
                Assert.Equal("b1", model.Thread.Current.Turns[0].DisplayOrdinal);

                surface.Composer.Draft.SetFreeFormText("second words");
                await NotebookFixture.SendAndSettleAsync(surface);
                await UntilAsync(window, () => Ended(model, 1), () => "B never ended");
                var a = model.Runs.AttemptOf(1)!.Attempt;
                var b = model.Runs.AttemptOf(2)!.Attempt;

                // A revision with no attempt: saved, never sent. No row may be invented for it.
                Assert.NotNull(await model.PrepareRevisionAsync("a draft never sent", CancellationToken.None));
                Close(window, host, model, surface);

                (host, model, surface) = await ReopenAsync(window);
                await UntilAsync(window, () => surface.Thread.Rows.Count >= 2, () => $"the reopened notebook renders {surface.Thread.Rows.Count} rows for 2 recorded attempts");
                var rows = surface.Thread.Rows;
                Assert.Equal(2, rows.Count);
                Assert.Equal(["b1", "b2"], rows.Select(r => r.DisplayOrdinal));
                Assert.Equal(["first words", "second words"], rows.Select(r => r.Words));
                Assert.Equal($"Sent from revision {a.RevisionNo} · request {a.ProjectionSha![..7]} · via claude-code", rows[0].BindingLine);
                Assert.Equal($"Sent from revision {b.RevisionNo} · request {b.ProjectionSha![..7]} · via claude-code", rows[1].BindingLine);

                // The next turn continues after the restored ones, and its attempt is its own.
                surface.Composer.Draft.SetFreeFormText("third words");
                await NotebookFixture.SendAndSettleAsync(surface);
                await UntilAsync(window, () => Ended(model, 2), () => "C never ended");
                Assert.Equal("b3", model.Thread.Current.Turns[2].DisplayOrdinal);
                await UntilAsync(window, () => surface.Thread.Rows.Count == 3 && surface.Thread.Rows[2].BindingLine.StartsWith("Sent from", StringComparison.Ordinal),
                    () => $"b3 reads \"{surface.Thread.Rows.ElementAtOrDefault(2)?.BindingLine}\"");
                var c = model.Runs.AttemptOf(3)!.Attempt;
                Assert.NotEqual(a.DispatchKey, c.DispatchKey);
                Assert.NotEqual(b.DispatchKey, c.DispatchKey);
                Assert.Equal($"Sent from revision {a.RevisionNo} · request {a.ProjectionSha[..7]} · via claude-code", surface.Thread.Rows[0].BindingLine);
                Assert.Equal($"Sent from revision {b.RevisionNo} · request {b.ProjectionSha[..7]} · via claude-code", surface.Thread.Rows[1].BindingLine);
                Assert.Equal($"Sent from revision {c.RevisionNo} · request {c.ProjectionSha![..7]} · via claude-code", surface.Thread.Rows[2].BindingLine);
                Close(window, host, model, surface);
            });
    }

    [Fact]
    public void AHostKilledMidRun_ReopensAsDeliveryUnknown_WithTheAttemptsLastSeqAndTs_AndItsBindingLine()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
                _fixture.ReleaseRun();

                // The process dies as it writes the terminal row: the run ran and logged its events; nothing concluded it.
                var dying = new DyingAtTheTerminal(new BoundaryDispatcher(_fixture.Store));
                var (host, model, surface) = await ReopenAsync(window, dying);
                surface.Composer.Draft.SetFreeFormText("words before the kill");
                Assert.NotNull(surface.Composer.Send());
                await UntilAsync(window, () => dying.Died, () => "the host never reached its terminal write");
                var attempt = model.Runs.AttemptOf(1)!.Attempt;
                Close(window, host, model, surface);

                var logged = RunLogStore.Read(RunLogStore.PathFor(_fixture.Root, NotebookFixture.SessionId, attempt.DispatchKey));
                Assert.NotEmpty(logged);
                var last = logged[^1];

                (host, model, surface) = await ReopenAsync(window);
                await UntilAsync(window, () => surface.Thread.Rows.Count >= 1, () => "the reopened notebook renders no row for the killed attempt");
                var row = Assert.Single(surface.Thread.Rows);
                Assert.Equal(TurnState.DeliveryUnknown, row.State);
                Assert.Equal("delivery unknown", row.OutcomeWord);
                Assert.Equal($"Sent from revision {attempt.RevisionNo} · request {attempt.ProjectionSha![..7]} · via claude-code", row.BindingLine);
                var lines = row.View.Events.Select(e => e.Text).ToList();
                Assert.Contains(lines, l => l.StartsWith($"DeliveryUnknown: {DispatchErrorCodes.DeliveryUnknown} host_restart", StringComparison.Ordinal));

                // Ruling 164: the line follows the cause's reason sentence, and the row's UIA HelpText repeats it verbatim.
                var expected = TurnCopy.ReasonSentence(row.View) + " "
                    + string.Create(CultureInfo.InvariantCulture, $"Last event received: #{last.Seq} at {last.Ts.ToLocalTime():HH:mm:ss}.");
                Assert.Equal(expected, row.HelpText);
                Assert.Equal(expected, AutomationProperties.GetHelpText(ThreadFixtures.Container(surface.Thread, 0)));
                Assert.DoesNotContain(lines, l => l.StartsWith("Last ", StringComparison.Ordinal));
                Close(window, host, model, surface);
            });
    }

    [Fact]
    public void AKilledAttemptWithNoEvents_ReopensAsDeliveryUnknown_WithLastEventNotRecorded()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
                _fixture.ReleaseRun();

                var dying = new DyingAtTheTerminal(new BoundaryDispatcher(_fixture.Store));
                var (host, model, surface) = await ReopenAsync(window, dying);
                surface.Composer.Draft.SetFreeFormText("words before the kill");
                Assert.NotNull(surface.Composer.Send());
                await UntilAsync(window, () => dying.Died, () => "the host never reached its terminal write");
                var attempt = model.Runs.AttemptOf(1)!.Attempt;
                Close(window, host, model, surface);

                // The attempt is recorded; its run log holds no event.
                File.WriteAllText(RunLogStore.PathFor(_fixture.Root, NotebookFixture.SessionId, attempt.DispatchKey), string.Empty);

                (host, model, surface) = await ReopenAsync(window);
                await UntilAsync(window, () => surface.Thread.Rows.Count >= 1, () => "the reopened notebook renders no row for the killed attempt");
                var row = Assert.Single(surface.Thread.Rows);
                Assert.Equal(TurnState.DeliveryUnknown, row.State);
                var expected = TurnCopy.ReasonSentence(row.View) + " Last event received: not recorded.";
                Assert.Equal(expected, row.HelpText);
                Assert.Equal(expected, AutomationProperties.GetHelpText(ThreadFixtures.Container(surface.Thread, 0)));
                Close(window, host, model, surface);
            });
    }

    /// <summary>
    /// Brief check 3: a restored row's events are reached from the row by keyboard the way a live row's are — Tab from
    /// the row's container to its fold, Space opens it — and every folded event line is exposed to UI Automation.
    /// </summary>
    [Fact]
    public void ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
                _fixture.ReleaseRun();

                var (host, model, surface) = await ReopenAsync(window);
                surface.Composer.Draft.SetFreeFormText("first words");
                await NotebookFixture.SendAndSettleAsync(surface);
                await UntilAsync(window, () => Ended(model, 0), () => "A never ended");
                Close(window, host, model, surface);

                (host, model, surface) = await ReopenAsync(window);
                await UntilAsync(window, () => surface.Thread.Rows.Count >= 1, () => "the reopened notebook renders no row");
                var feed = surface.Thread;
                var row = Assert.Single(feed.Rows);
                Assert.NotEmpty(row.View.Events);
                Assert.False(row.IsFoldOpen);
                feed.UpdateLayout();

                // From the row's container, Tab reaches its fold header, as on a live row.
                Assert.True(feed.FocusItem(0));
                var container = ThreadFixtures.Container(feed, 0);
                Assert.Same(container, Keyboard.FocusedElement);
                ToggleButton? fold = null;
                List<string> stops = [];
                for (var i = 0; i < 8 && fold is null; i++)
                {
                    ((UIElement)Keyboard.FocusedElement).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    var focused = (DependencyObject)Keyboard.FocusedElement;
                    stops.Add(AutomationProperties.GetName(focused));
                    if (AutomationProperties.GetName(focused) == row.FoldHeader)
                    {
                        fold = focused as ToggleButton;
                    }
                }

                Assert.True(fold is not null, $"Tab from the restored row reached [{string.Join(", ", stops)}], never its fold \"{row.FoldHeader}\"");

                // Space opens the fold.
                var source = PresentationSource.FromVisual(fold)!;
                fold.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Space) { RoutedEvent = UIElement.KeyDownEvent });
                fold.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Space) { RoutedEvent = UIElement.KeyUpEvent });
                await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
                feed.UpdateLayout();
                Assert.True(row.IsFoldOpen, "Space on the restored row's fold did not open it");

                // Every folded event line is in the row's UI Automation tree.
                var names = new List<string>();
                Collect(UIElementAutomationPeer.CreatePeerForElement(container), names);
                Assert.NotEmpty(row.FoldedEvents);
                foreach (var line in row.FoldedEvents)
                {
                    Assert.True(names.Any(n => n.Contains(line.Text, StringComparison.Ordinal)), $"the event \"{line.Text}\" is not exposed to UIA under the restored row");
                }

                Close(window, host, model, surface);
            });
    }

    /// <summary>
    /// Leader's check (the T17 condition on ordinals): a turn sent while the read-back of two attempts is still pending
    /// waits for it, so it is b3 after the two restored rows, and every row names its own attempt.
    /// </summary>
    [Fact]
    public void ATurnSentBeforeTheReadBackFinishes_WaitsForIt_AndIsB3_AndEveryRowNamesItsOwnAttempt()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
                _fixture.ReleaseRun();

                var (host, model, surface) = await ReopenAsync(window);
                surface.Composer.Draft.SetFreeFormText("first words");
                await NotebookFixture.SendAndSettleAsync(surface);
                await UntilAsync(window, () => Ended(model, 0), () => "A never ended");
                surface.Composer.Draft.SetFreeFormText("second words");
                await NotebookFixture.SendAndSettleAsync(surface);
                await UntilAsync(window, () => Ended(model, 1), () => "B never ended");
                var a = model.Runs.AttemptOf(1)!.Attempt;
                var b = model.Runs.AttemptOf(2)!.Attempt;
                Close(window, host, model, surface);

                // Reopen with the host's reads held: the read-back is pending when the next turn is sent.
                var held = new HeldReads(new BoundaryDispatcher(_fixture.Store));
                host = await AttemptHost.OpenAsync(held, LockFile, CancellationToken.None);
                held.Hold();
                model = _fixture.NewModel();
                model.Runs.BindHost(host, Scope);
                surface = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                window.Content = surface;
                _fixture.ConfigureStub(surface);
                await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
                Assert.False(surface.LastReadBack.IsCompleted, "the read-back finished before the send; the race is not exercised");

                surface.Composer.Draft.SetFreeFormText("third words");
                Assert.NotNull(surface.Composer.Send());
                await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
                held.Release();
                await surface.LastReadBack.WaitAsync(NotebookFixture.Bound);
                await UntilAsync(window, () => surface.Thread.Rows.Count == 3 && Ended(model, 2),
                    () => $"{surface.Thread.Rows.Count} rows; [{string.Join(", ", surface.Thread.Rows.Select(r => r.DisplayOrdinal + " " + r.Words))}]");

                var rows = surface.Thread.Rows;
                Assert.Equal(["b1 first words", "b2 second words", "b3 third words"], rows.Select(r => r.DisplayOrdinal + " " + r.Words));
                var c = model.Runs.AttemptOf(rows[2].Ordinal)!.Attempt;
                Assert.Equal(a.DispatchKey, model.Runs.AttemptOf(rows[0].Ordinal)!.Attempt.DispatchKey);
                Assert.Equal(b.DispatchKey, model.Runs.AttemptOf(rows[1].Ordinal)!.Attempt.DispatchKey);
                Assert.NotEqual(a.DispatchKey, c.DispatchKey);
                Assert.NotEqual(b.DispatchKey, c.DispatchKey);
                await UntilAsync(window, () => rows[2].BindingLine.StartsWith("Sent from", StringComparison.Ordinal), () => $"b3 reads \"{rows[2].BindingLine}\"");
                Assert.Equal($"Sent from revision {a.RevisionNo} · request {a.ProjectionSha![..7]} · via claude-code", rows[0].BindingLine);
                Assert.Equal($"Sent from revision {b.RevisionNo} · request {b.ProjectionSha![..7]} · via claude-code", rows[1].BindingLine);
                Assert.Equal($"Sent from revision {c.RevisionNo} · request {c.ProjectionSha![..7]} · via claude-code", rows[2].BindingLine);
                Close(window, host, model, surface);
            });
    }

    /// <summary>The real ledger, with its attempt reads held from <see cref="Hold"/> until <see cref="Release"/>.</summary>
    private sealed class HeldReads(IAttemptLedger inner) : IAttemptLedger
    {
        private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _holding;

        public void Hold() => _holding = true;

        public void Release() => _released.TrySetResult();

        public Task<AttemptBeginResult> BeginAttemptAsync(AttemptRow attempt, CancellationToken cancellationToken) =>
            inner.BeginAttemptAsync(attempt, cancellationToken);

        public Task<AttemptAppendResult> AppendAttemptOutcomeAsync(OutcomeRow outcome, CancellationToken cancellationToken) =>
            inner.AppendAttemptOutcomeAsync(outcome, cancellationToken);

        public async Task<AttemptReceipt?> ReadAttemptAsync(string dispatchKey, CancellationToken cancellationToken)
        {
            if (_holding)
            {
                await _released.Task.WaitAsync(cancellationToken);
            }

            return await inner.ReadAttemptAsync(dispatchKey, cancellationToken);
        }

        public Task<AttemptSweepResult> SweepAttemptsAsync(AttemptSweepRequest request, CancellationToken cancellationToken) =>
            inner.SweepAttemptsAsync(request, cancellationToken);

        public Task<AttemptAppendResult> WithdrawAttemptAsync(string dispatchKey, CancellationToken cancellationToken) =>
            inner.WithdrawAttemptAsync(dispatchKey, cancellationToken);
    }

    private static void Collect(AutomationPeer? peer, List<string> names)
    {
        if (peer is null)
        {
            return;
        }

        names.Add(peer.GetName());
        foreach (var child in peer.GetChildren() ?? [])
        {
            Collect(child, names);
        }
    }

    /// <summary>Stands in for the process dying: thrown where no host or notebook code catches it.</summary>
    private sealed class ProcessDeath() : Exception("simulated process death at the terminal write");

    /// <summary>The real ledger, with the process dying at its terminal write (W4 of the write-ahead crash windows).</summary>
    private sealed class DyingAtTheTerminal(IAttemptLedger inner) : IAttemptLedger
    {
        public bool Died { get; private set; }

        public Task<AttemptBeginResult> BeginAttemptAsync(AttemptRow attempt, CancellationToken cancellationToken) =>
            inner.BeginAttemptAsync(attempt, cancellationToken);

        public Task<AttemptAppendResult> AppendAttemptOutcomeAsync(OutcomeRow outcome, CancellationToken cancellationToken)
        {
            if (AttemptReceipt.TerminalStates.Contains(outcome.State))
            {
                Died = true;
                return Task.FromException<AttemptAppendResult>(new ProcessDeath());
            }

            return inner.AppendAttemptOutcomeAsync(outcome, cancellationToken);
        }

        public Task<AttemptReceipt?> ReadAttemptAsync(string dispatchKey, CancellationToken cancellationToken) =>
            inner.ReadAttemptAsync(dispatchKey, cancellationToken);

        public Task<AttemptSweepResult> SweepAttemptsAsync(AttemptSweepRequest request, CancellationToken cancellationToken) =>
            inner.SweepAttemptsAsync(request, cancellationToken);

        public Task<AttemptAppendResult> WithdrawAttemptAsync(string dispatchKey, CancellationToken cancellationToken) =>
            inner.WithdrawAttemptAsync(dispatchKey, cancellationToken);
    }
}
