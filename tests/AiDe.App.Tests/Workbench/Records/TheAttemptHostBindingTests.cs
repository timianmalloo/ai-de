using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Presentation.Sessions;

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
    /// The design's node-2 members the binding routes through — Stop (§4), Send now (the queue) and Subscribe (live
    /// events and run-log catch-up). Red until T5 node 2 (wl-t5-host) lands them on <see cref="IAttemptHost"/>.
    /// </summary>
    [Fact]
    public void TheHostInterface_CarriesTheDesignedStopSendNowAndSubscribeMembers()
    {
        string[] designed = ["StopAsync", "SendNowAsync", "Subscribe"];
        var missing = designed.Where(name => typeof(IAttemptHost).GetMethod(name) is null).ToArray();

        Assert.True(missing.Length == 0,
            $"IAttemptHost has no {string.Join(", ", missing)} yet (design 'IAttemptHost', T5 node 2). Until then a host-run "
            + "turn cannot be stopped, a queued turn is drained by the notebook rather than the host, and its live lines "
            + "do not reach the thread.");
    }

    private static async Task UntilAsync(Window window, Func<bool> condition, Func<string> what)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        }

        Assert.True(condition(), what());
    }
}
