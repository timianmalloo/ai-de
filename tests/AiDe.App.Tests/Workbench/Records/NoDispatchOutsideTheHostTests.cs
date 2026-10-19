using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Records;

/// <summary>
/// Ruling 159 (DR req-01M59FX8EAK66B8PQEDQR4TRGN, option A): no turn runs outside the attempt host. A turn sent before
/// the host opens waits for it; a window that cannot open the host refuses Send with the cause and never runs directly
/// (ADR-0041's write-ahead invariant); a dead holder never locks the user out; the attempt's epoch is read at dispatch.
/// </summary>
public sealed class NoDispatchOutsideTheHostTests : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private string LockFile => Path.Combine(_fixture.Root, ".aide", "attempt-host.lock");

    private AttemptScope Scope => new("ws-t8", _fixture.Store.CoreEpoch, SessionGeneration: 1);

    private Task<IAttemptHost> OpenHost() =>
        AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None).ContinueWith(
            opened => (IAttemptHost)opened.Result, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);

    [Fact]
    public void ATurnSentBeforeTheHostOpens_WaitsForIt_AndRunsAsAnAttempt()
    {
        Pump(async (window, model, document) =>
        {
            var opening = new TaskCompletionSource<IAttemptHost>(TaskCreationOptions.RunContinuationsAsynchronously);
            model.Runs.BindHostWhenOpen(() => opening.Task, Scope);

            document.Composer.Draft.SetFreeFormText("say ok");
            Assert.NotNull(document.Composer.Send());
            var ordinal = Assert.Single(model.Thread.Current.Turns).Ordinal;
            await Task.Delay(800);
            Assert.False(_fixture.RunOpened, "a turn ran before the host opened: a direct run, outside the host");

            using var host = (AttemptHost)await OpenHost();
            opening.SetResult(host);
            _fixture.ReleaseRun();
            await UntilAsync(window, () => model.Thread.Current.Turns[0].State == TurnState.Answered,
                () => $"the waiting turn reads {model.Thread.Current.Turns[0].State}");
            Assert.Equal(DispatchState.ResponseComplete, model.Runs.AttemptOf(ordinal)?.Terminal?.State);
        });
    }

    [Fact]
    public void WhenAnotherWindowHoldsTheHost_SendIsRefusedWithTheHostedElsewhereCopy_AndTheTurnNeverRuns()
    {
        Pump(async (window, model, document) =>
        {
            using var holder = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
            model.Runs.BindHostWhenOpen(OpenHost, Scope);

            document.Composer.Draft.SetFreeFormText("say ok");
            document.Composer.Send();
            await UntilAsync(window, () => model.Runs.HostRefusal is not null, () => "the window never learned it cannot host runs");
            await Task.Delay(500);

            Assert.Equal(NotebookRuns.HostedElsewhere, model.Runs.HostRefusal);
            Assert.Equal("This workspace's runs are hosted in another window. Send from that window, or close it to host runs here.", NotebookRuns.HostedElsewhere);
            Assert.False(_fixture.RunOpened, "the refused window ran the turn directly");
            Assert.All(model.Thread.Current.Turns, t => Assert.Null(model.Runs.AttemptOf(t.Ordinal)));
        });
    }

    [Fact]
    public void AnyOtherOpenFailure_RefusesWithItsOwnCause_AndARetryBindsTheHost()
    {
        Pump(async (window, model, document) =>
        {
            var tries = 0;
            model.Runs.BindHostWhenOpen(() => ++tries == 1 ? Task.FromException<IAttemptHost>(new IOException("the lock file's folder is read-only")) : OpenHost(), Scope);
            await UntilAsync(window, () => model.Runs.HostRefusal is not null, () => "the open failure was never surfaced");

            Assert.Contains("the lock file's folder is read-only", model.Runs.HostRefusal, StringComparison.Ordinal);
            Assert.NotEqual(NotebookRuns.HostedElsewhere, model.Runs.HostRefusal);

            await model.Runs.RetryHostAsync();
            Assert.Null(model.Runs.HostRefusal);
            Assert.NotNull(model.Runs.Host);
            using var host = (AttemptHost)model.Runs.Host!;

            document.Composer.Draft.SetFreeFormText("say ok");
            Assert.NotNull(document.Composer.Send());
            _fixture.ReleaseRun();
            await UntilAsync(window, () => model.Thread.Current.Turns[0].State == TurnState.Answered,
                () => $"the turn after the retry reads {model.Thread.Current.Turns[0].State}");
            Assert.NotNull(model.Runs.AttemptOf(model.Thread.Current.Turns[0].Ordinal));
        });
    }

    [Fact]
    public void TheAttemptsEpoch_IsReadAtDispatch_NotFixedAtBind()
    {
        Pump(async (window, model, document) =>
        {
            using var host = (AttemptHost)await OpenHost();
            var current = _fixture.Store.CoreEpoch;

            // Bound under an epoch a daemon restart has since replaced; the refresh reads the new handshake's.
            model.Runs.BindHost(host, Scope with { WorkspaceEpoch = current + 7, RefreshEpoch = _ => Task.FromResult(current) });
            document.Composer.Draft.SetFreeFormText("say ok");
            Assert.NotNull(document.Composer.Send());
            _fixture.ReleaseRun();
            await UntilAsync(window, () => model.Thread.Current.Turns[0].State is not (TurnState.Running or TurnState.Preparing),
                () => "the attempt never ended");

            Assert.Equal(TurnState.Answered, model.Thread.Current.Turns[0].State);
            Assert.Equal(current, model.Runs.AttemptOf(model.Thread.Current.Turns[0].Ordinal)?.Attempt.WorkspaceEpoch);
        });
    }

    /// <summary>Ruling 159(3): the lock is the holding process's own handle, so a holder that died frees it for the next open.</summary>
    [Fact]
    public async Task ADeadHolder_NeverLocksTheUserOut_TheNextOpenSucceeds()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LockFile)!);
        using var holder = Process.Start(new ProcessStartInfo("powershell.exe",
            $"-NoProfile -Command \"$f=[IO.File]::Open('{LockFile}','OpenOrCreate','ReadWrite','None'); Start-Sleep 120\"")
        { UseShellExecute = false, CreateNoWindow = true })!;
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        AgentPlaneException? refused = null;
        while (refused is null && DateTime.UtcNow < deadline)
        {
            try
            {
                (await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None)).Dispose();
                await Task.Delay(100);
            }
            catch (AgentPlaneException error)
            {
                refused = error;
            }
        }

        Assert.Equal(AttemptErrorCodes.HostNotSole, refused?.Code);
        holder.Kill(entireProcessTree: true);
        await holder.WaitForExitAsync();

        using var opened = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
        Assert.NotNull(opened);
    }

    private void Pump(Func<Window, NotebookModel, SessionDocumentSurface, Task> body) =>
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
                using var model = _fixture.NewModel();
                using var document = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                window.Content = document;
                _fixture.ConfigureStub(document);
                await body(window, model, document);
                window.Content = null;
            });

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
