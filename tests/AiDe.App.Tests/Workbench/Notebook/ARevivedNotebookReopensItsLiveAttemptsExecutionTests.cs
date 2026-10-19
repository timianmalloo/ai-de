using System.IO;
using System.Runtime.Versioning;
using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Tests.Records;
using AiDe.App.Tests.Shell;
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
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Kinds;

/// <summary>
/// The WL-S design's §4: a saved <c>session-document</c> placement restores as one <c>notebook</c> of the same session,
/// plus one <c>execution</c> when an attempt was live. The slot file records no attempts, so "was live" is read from the
/// attempt store the notebook binds: the window closed mid-run, so the host's startup sweep concluded the attempt
/// <c>DeliveryUnknown{host_restart}</c>, and the revived notebook's read-back restores it as its last turn.
/// </summary>
/// <remarks>
/// Two lifetimes over one store. Today's window dies at the terminal write (T17's killed-attempt shape), or concludes
/// its run. Then the product's shell over a real <see cref="WorkspaceClient"/> to a daemon restores today's slot through
/// <see cref="LayoutPersistence"/> and revives the pane the way <c>MainWindow.AttachWorkspace</c> does.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class ARevivedNotebookReopensItsLiveAttemptsExecutionTests : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private string Root => _fixture.Root;

    private static string SessionPane => "session-document:" + NotebookFixture.SessionId;

    private static string NotebookPane => "notebook:" + NotebookFixture.SessionId;

    // Today's Factory slot (schema 1): the session's pane in the Center, the watcher at the Left, an empty Bottom.
    private static string TodaysSlot => $$"""
        {
          "schemaVersion": 1,
          "appVersion": "0.4.0",
          "savedUtc": "2026-10-18T22:33:53+00:00",
          "layout": {
            "zones": [
              { "id": "Left", "content": { "kind": "stack", "tabs": [
                  { "surfaceId": "sessions", "kind": "sessions", "title": "Sessions" } ], "activeIndex": 0 },
                "extent": 0.22, "collapsed": false },
              { "id": "Right", "extent": 0.22, "collapsed": false },
              { "id": "Bottom", "extent": 0.3, "collapsed": false },
              { "id": "Center", "content": { "kind": "stack", "tabs": [
                  { "surfaceId": "ledger", "kind": "ledger", "title": "Ledger" },
                  { "surfaceId": "{{SessionPane}}", "kind": "session-document", "title": "Binding" } ], "activeIndex": 1 },
                "extent": 1.0, "collapsed": false }
            ],
            "floating": []
          }
        }
        """;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ASessionPane_RestoresAsItsNotebook_PlusOneExecutionInTheBottom_OnlyWhenItsAttemptWasLiveAtClose(bool diedMidRun)
    {
        var accounts = new[] { new AccountRef("anthropic", "max") };
        new SessionConfigStore(Root, NotebookFixture.SessionId).Create("Binding", Root, accounts, accounts[0], DateTimeOffset.UnixEpoch);
        var key = TodaysWindow(diedMidRun);

        var pipe = "aide-t11-revive-" + Guid.NewGuid().ToString("N");
        var endpoint = new DaemonEndpoint(pipe, new CapabilityRegistry(), _ => _fixture.Store.CoreEpoch);
        DaemonOperations.Register(endpoint, () => _fixture.Store.CoreEpoch);
        WorkspaceOperations.RegisterNotebook(endpoint, new NotebookService(_fixture.Store));
        WorkspaceOperations.RegisterDispatch(endpoint, new BoundaryDispatcher(_fixture.Store));
        var server = new IpcServer(pipe, endpoint, new IpcServerOptions(StartupGrace: TimeSpan.FromSeconds(45)));
        using var life = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var running = server.RunAsync(life.Token);

        try
        {
            await using var client = await WorkspaceClient.ConnectAsync(pipe, TimeSpan.FromSeconds(20), CancellationToken.None);
            Sta.Pump(
                create: () => new Border(),
                configure: window => { window.Width = 1440; window.Height = 900; },
                body: async (window, border) =>
                {
                    using var shell = new WorkbenchShell(client);
                    border.Child = shell.WorkbenchRoot;

                    // Restore today's slot over the shell's service, then revive, as the window's workspace open does.
                    var layoutPath = Path.Combine(Root, "shell-state", "layout.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(layoutPath)!);
                    File.WriteAllText(LayoutPersistence.SlotPathFor(layoutPath, PerspectiveSet.Initial), TodaysSlot);
                    using var persistence = new LayoutPersistence(
                        shell.Service, layoutPath, new HashSet<string>(StringComparer.Ordinal), restorableKinds: SurfaceContentFactory.RestorableKinds);
                    persistence.Restore();
                    shell.Adapter.Render();
                    var revived = shell.ReviveRestoredSessionDocuments(Root);
                    Assert.Equal([NotebookFixture.SessionId], revived.Select(c => c.SessionId));
                    shell.Adapter.Render();

                    // The store answers "was live": the revived notebook reads its attempt back as its one turn. A concluded
                    // attempt reads back Completed: the store's ResponseComplete folds to Completed (AttemptStateFold's
                    // TerminalStates), because a receipt does not carry the goal that told Answered from Completed live.
                    var model = shell.Notebook(NotebookId.ForSession(NotebookFixture.SessionId))!;
                    var expected = diedMidRun ? TurnState.DeliveryUnknown : TurnState.Completed;
                    await UntilAsync(window, () => model.Thread.Current.Turns is [{ } only] && only.State == expected,
                        () => $"the revived notebook reads {model.Thread.Current.Turns.Count} turn(s) [{string.Join(",", model.Thread.Current.Turns.Select(t => t.State))}], "
                            + $"host bound={model.Runs.Host is not null}, read back={model.Runs.ReadBack.Count}; expected one {expected} turn for attempt {key}");

                    var zones = shell.Factory.Service.Zones;
                    var executionId = "execution:" + key;
                    if (diedMidRun)
                    {
                        await UntilAsync(window, () => zones.FindZoneOf(executionId) is not null,
                            () => $"no {executionId} opened; the Bottom holds [{string.Join(",", zones.Zone(ZoneId.Bottom).Content?.Surfaces().Select(s => s.SurfaceId) ?? [])}]");
                        var execution = Assert.Single(zones.AllSurfaces(), s => s.Kind == "execution");
                        Assert.Equal(executionId, execution.SurfaceId);
                        Assert.Equal("Execution — " + key, execution.Title);
                        Assert.Equal(ZoneId.Bottom, zones.FindZoneOf(executionId));
                    }
                    else
                    {
                        await Settle(window, rounds: 25);
                        Assert.DoesNotContain(zones.AllSurfaces(), s => s.Kind == "execution");
                    }

                    // The revive does not move the active tab the restore chose (ReviveRestoredSessionDocuments' contract).
                    Assert.Equal(NotebookPane, ((ZoneStack)zones.Zone(ZoneId.Center).Content!).Active.SurfaceId);
                    border.Child = null;
                });
        }
        finally
        {
            await life.CancelAsync();
            try { await running; } catch (OperationCanceledException) { }
        }
    }

    /// <summary>Today's window, bound to its own host: one turn that either dies at the terminal write or concludes. Returns its dispatch key.</summary>
    private string TodaysWindow(bool diesMidRun)
    {
        var key = string.Empty;
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                _fixture.ReleaseRun();
                var ledger = new DyingAtTheTerminal(new BoundaryDispatcher(_fixture.Store), diesMidRun);
                var host = await AttemptHost.OpenAsync(ledger, Path.Combine(Root, ".aide", "attempt-host.lock"), CancellationToken.None);
                var model = _fixture.NewModel();
                model.Runs.BindHost(host, new AttemptScope("workspace", _fixture.Store.CoreEpoch, SessionGeneration: 1));
                var surface = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                window.Content = surface;
                _fixture.ConfigureStub(surface);
                surface.Composer.Draft.SetFreeFormText("words before the close");
                Assert.NotNull(surface.Composer.Send());
                await UntilAsync(window,
                    () => diesMidRun ? ledger.Died : model.Thread.Current.Turns is [{ State: TurnState.Answered }],
                    () => diesMidRun ? "the host never reached its terminal write" : "the turn never concluded Answered");
                key = model.Runs.AttemptOf(1)!.DispatchKey;
                window.Content = null;
                surface.Dispose();
                model.Dispose();
                host.Dispose();
            });
        return key;
    }

    private static async Task Settle(System.Windows.Window window, int rounds = 1)
    {
        for (var i = 0; i < rounds; i++)
        {
            await Task.Delay(20);
            window.UpdateLayout();
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        }
    }

    private static async Task UntilAsync(System.Windows.Window window, Func<bool> condition, Func<string> what)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        }

        Assert.True(condition(), what());
    }

    private sealed class ProcessDeath() : Exception("simulated process death at the terminal write");

    /// <summary>The process dies as it writes the terminal row (T17's shape): the run ran, and nothing concluded it.</summary>
    private sealed class DyingAtTheTerminal(IAttemptLedger inner, bool dies) : IAttemptLedger
    {
        public bool Died { get; private set; }

        public Task<AttemptBeginResult> BeginAttemptAsync(AttemptRow attempt, CancellationToken cancellationToken) =>
            inner.BeginAttemptAsync(attempt, cancellationToken);

        public Task<AttemptAppendResult> AppendAttemptOutcomeAsync(OutcomeRow outcome, CancellationToken cancellationToken)
        {
            if (dies && AttemptReceipt.TerminalStates.Contains(outcome.State))
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
