using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Records;

/// <summary>
/// The notebook binds to <see cref="IAttemptHost"/> as designed (docs/design/workbench-loops-wl-1a.md): a sent turn is
/// dispatched as an attempt of the revision it ran, the attempt is the host's — so it outlives the pane — and a restart
/// reads the attempt and its outcome events back from the store.
/// </summary>
/// <remarks>
/// The host is the real <see cref="AttemptHost"/> over <see cref="BoundaryDispatcher"/> and a real schema-v2 store, and
/// the run is the product's composition root spawning <see cref="Conductor.StubAcpAdapter"/>. "Restart" is a new host,
/// a new model and a new surface over the same store and the same session envelope file.
/// </remarks>
public sealed class TheAttemptHostBindingTests : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private string LockFile => Path.Combine(_fixture.Root, ".aide", "attempt-host.lock");

    private AttemptScope Scope => new("ws-t8", _fixture.Store.CoreEpoch, SessionGeneration: 1);

    [Fact]
    public void ASentTurn_IsAnAttemptOfItsRevision_AndARestartReadsTheAttemptAndItsEventsBack()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                string dispatchKey;
                int revisionNo;

                // The session's home, as session creation makes it: the envelope store (the gate's G1 fold) lives there.
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));

                var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
                using (host)
                {
                    using var model = _fixture.NewModel();
                    model.Runs.BindHost(host, Scope);
                    var document = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                    window.Content = document;
                    _fixture.ConfigureStub(document);
                    _fixture.ReleaseRun();

                    document.Composer.Draft.SetFreeFormText("say ok");
                    Assert.NotNull(document.Composer.Send());
                    var ordinal = Assert.Single(model.Thread.Current.Turns).Ordinal;
                    await UntilAsync(window, () => model.Thread.Current.Turns[0].State is not (TurnState.Running or TurnState.Preparing),
                        () => $"the attempt never ended; it reads {model.Thread.Current.Turns[0].State}");

                    var attempt = model.Runs.AttemptOf(ordinal);
                    Assert.True(model.Thread.Current.Turns[0].State == TurnState.Answered,
                        $"the turn reads {model.Thread.Current.Turns[0].State}; events [{string.Join(", ", model.Thread.Current.Turns[0].Events.Select(e => e.Text))}]");
                    Assert.NotNull(attempt);
                    Assert.Equal(DispatchState.ResponseComplete, attempt!.Terminal?.State);
                    Assert.Equal(model.Id.Value, attempt.Attempt.NotebookId);
                    Assert.Equal(model.RevisionOf(ordinal)!.RevisionNo, attempt.Attempt.RevisionNo);
                    Assert.Equal(document.Composer.Gate.LastSubmission!.ProjectionSha, attempt.Attempt.ProjectionSha);
                    dispatchKey = attempt.DispatchKey;
                    revisionNo = attempt.Attempt.RevisionNo;

                    window.Content = null;
                    document.Dispose();
                }

                // Restart: nothing in memory survives but the store and the session's envelope file.
                using var restarted = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
                using var reopened = _fixture.NewModel();
                reopened.Runs.BindHost(restarted, Scope);
                using var surface = new SessionDocumentSurface(reopened, null, new RecordingAnnouncer());
                window.Content = surface;
                await surface.LastReadBack.WaitAsync(NotebookFixture.Bound);

                var readBack = Assert.Single(reopened.Runs.ReadBack);
                Assert.Equal(dispatchKey, readBack.DispatchKey);
                Assert.Equal(revisionNo, readBack.Attempt.RevisionNo);
                Assert.NotNull(readBack.Dispatched);
                Assert.Equal(DispatchState.ResponseComplete, readBack.Terminal?.State);
                window.Content = null;
            });
    }

    /// <summary>
    /// The members the binding routes through, by the names T5 node 2 declared (rule 14: the code outranks the design's
    /// <c>Subscribe</c>) — Stop (§4), the queue (Dispatch with a predecessor, Send now) and the live lines.
    /// </summary>
    [Fact]
    public void TheHostInterface_CarriesTheDesignedStopSendNowAndSubscribeMembers()
    {
        string[] declared = ["DispatchAsync", "StopAsync", "SendNowAsync", "SubscribeAsync"];
        var missing = declared.Where(name => typeof(IAttemptHost).GetMethod(name) is null).ToArray();

        Assert.True(missing.Length == 0, $"IAttemptHost has no {string.Join(", ", missing)}");
        Assert.Equal(typeof(Task<AttemptSubscription>), typeof(IAttemptHost).GetMethod("SubscribeAsync")!.ReturnType);
    }

    /// <summary>
    /// A Stop pressed on the pane is the host's Stop (ADR-0041 §4): the attempt carries the StopRequested mark only
    /// <see cref="IAttemptHost.StopAsync"/> writes, and ends by its rule — the stub never answers <c>session/cancel</c>, so
    /// the deadline makes it <c>DeliveryUnknown{stop_not_acknowledged}</c>, never a token cancel's guess.
    /// </summary>
    [Fact]
    public void APaneStop_ReachesTheHostsStopAsync_AndTheAttemptEndsByItsStopRule()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
                using var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None,
                    new AttemptHostOptions(_fixture.Root) { StopDeadline = TimeSpan.FromSeconds(1) });
                using var model = _fixture.NewModel();
                model.Runs.BindHost(host, Scope);
                using var document = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                window.Content = document;
                _fixture.ConfigureStub(document);

                document.Composer.Draft.SetFreeFormText("hold");
                Assert.NotNull(document.Composer.Send());
                var ordinal = Assert.Single(model.Thread.Current.Turns).Ordinal;
                await UntilAsync(window, () => _fixture.RunOpened && model.Runs.AttemptOf(ordinal) is not null, () => "the attempt never launched");
                await UntilAsync(window, () => Button(document, "Stop this turn") is not null, () => "the turn offers no Stop");

                Button(document, "Stop this turn")!.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                await UntilAsync(window, () => model.Thread.Current.Turns[0].State is not (TurnState.Running or TurnState.Preparing or TurnState.StopRequested),
                    () => $"the stopped attempt never ended; the turn reads {model.Thread.Current.Turns[0].State}");

                var attempt = (await host.ReadAsync(model.Runs.AttemptOf(ordinal)!.DispatchKey, CancellationToken.None))!;
                Assert.Contains(attempt.Outcomes, o => o.State == DispatchState.StopRequested);
                Assert.Equal((DispatchState.DeliveryUnknown, "stop_not_acknowledged"), (attempt.Terminal?.State, attempt.Terminal?.Cause));
                Assert.Equal(TurnState.DeliveryUnknown, model.Thread.Current.Turns[0].State);
                window.Content = null;
            });
    }

    /// <summary>
    /// The queue is the host's (ADR-0045 §4, design "Queued launch"): a turn sent while one runs is dispatched at once
    /// as the held successor of the running attempt, the host launches it after its predecessor's
    /// <c>ResponseComplete</c>, and both turns' live lines reach the thread through <see cref="IAttemptHost.SubscribeAsync"/>.
    /// </summary>
    [Fact]
    public void AQueuedTurn_IsTheHostsHeldSuccessor_AndEachAttemptsLiveLinesReachTheThread()
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

                document.Composer.Draft.SetFreeFormText("first");
                Assert.NotNull(document.Composer.Send());
                await UntilAsync(window, () => _fixture.RunOpened, () => "the first attempt never opened its session");

                document.Composer.Draft.SetFreeFormText("second");
                Assert.Null(document.Composer.Send());
                var wait = WaitLink(document.Composer);
                Assert.True(wait is not null, $"the status line offers no Wait; it reads \"{document.Composer.Status}\"");
                wait!.RaiseEvent(new RoutedEventArgs(Hyperlink.ClickEvent));
                var turns = model.Thread.Current.Turns;
                await UntilAsync(window, () => model.Runs.AttemptOf(turns[1].Ordinal) is not null,
                    () => "the queued turn was never dispatched to the host at queue time");
                Assert.Equal(TurnState.Queued, model.Thread.Current.Turns[1].State);

                _fixture.ReleaseRun();
                await UntilAsync(window, () => model.Thread.Current.Turns.All(t => t.State == TurnState.Answered),
                    () => $"the turns read {string.Join(", ", model.Thread.Current.Turns.Select(t => t.State))}");

                var first = model.Runs.AttemptOf(turns[0].Ordinal)!;
                var second = model.Runs.AttemptOf(turns[1].Ordinal)!;
                Assert.Equal(first.DispatchKey, second.Attempt.QueuedAfterDispatchKey);
                Assert.Equal(DispatchState.ResponseComplete, second.Terminal?.State);
                foreach (var turn in model.Thread.Current.Turns)
                {
                    Assert.True(turn.Events.Any(e => e.Text.Contains("ok", StringComparison.Ordinal)),
                        $"turn {turn.Ordinal} holds no live line from its attempt: [{string.Join(", ", turn.Events.Select(e => e.Text))}]");
                }

                window.Content = null;
            });
    }

    private static Button? Button(SessionDocumentSurface document, string word) =>
        ThreadFixtures.Visuals<Button>(ThreadFixtures.Container(document.Thread, 0))
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
