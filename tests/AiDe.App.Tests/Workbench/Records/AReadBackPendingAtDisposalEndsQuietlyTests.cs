using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Store.Records;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Records;

/// <summary>
/// T17e (the Owner's follow-up to Rulings 172 and 175): since the read-back starts when the host binds, it can still be
/// running when its notebook, host and store are disposed, which is what closing the shell does. Each test holds the
/// read-back's store read on a gate, disposes, then releases the gate, so the order is fixed, never a race left to a rerun.
/// </summary>
/// <remarks>
/// The gate ignores the read's cancellation token on purpose: a store read already on its way cannot be recalled, so the
/// read-back must notice the disposal when the read comes back.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class AReadBackPendingAtDisposalEndsQuietlyTests : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private string LockFile => Path.Combine(_fixture.Root, ".aide", "attempt-host.lock");

    private AttemptScope Scope => new("workspace", _fixture.Store.CoreEpoch, SessionGeneration: 1);

    /// <summary>
    /// The shell's close order (<see cref="WorkbenchShell.Dispose"/>): the document, the notebook, then the host, and the
    /// store last, all while the read is held. The released read meets a disposed store, and the read-back still ends
    /// quietly with no turn.
    /// </summary>
    [Fact]
    public void AReadBackHeldWhileTheDocumentNotebookHostAndStoreAreDisposed_EndsQuietly_AndWritesNoTurn()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                await RecordTwoAttemptsAsync(window);
                var (model, surface, host, gate, readBack) = await ReopenWithTheReadHeldAsync(window);

                window.Content = null;
                surface.Dispose();
                model.Dispose();
                host.Dispose();
                _fixture.Store.Dispose();
                gate.Released.SetResult();

                var escaped = await Record.ExceptionAsync(() => readBack.WaitAsync(NotebookFixture.Bound));
                Assert.True(escaped is null, $"the read-back pending at disposal ended with {escaped?.GetType().Name}: {escaped?.Message}");
                await Settle(window, rounds: 5);
                Assert.True(model.Thread.Current.Turns.Count == 0, $"{model.Thread.Current.Turns.Count} turn rows were written after disposal");
            });
    }

    /// <summary>
    /// The notebook is disposed while its read is held, and the store stays open, so the released read finds both attempts.
    /// A disposed notebook still takes no turn from them.
    /// </summary>
    [Fact]
    public void AReadBackHeldWhileItsNotebookIsDisposed_RestoresNoTurn_WhenTheReadReturnsAfterward()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                await RecordTwoAttemptsAsync(window);
                var (model, surface, host, gate, readBack) = await ReopenWithTheReadHeldAsync(window);

                window.Content = null;
                surface.Dispose();
                model.Dispose();
                gate.Released.SetResult();

                var escaped = await Record.ExceptionAsync(() => readBack.WaitAsync(NotebookFixture.Bound));
                Assert.True(escaped is null, $"the read-back pending at disposal ended with {escaped?.GetType().Name}: {escaped?.Message}");
                await Settle(window, rounds: 5);
                Assert.True(model.Thread.Current.Turns.Count == 0, $"{model.Thread.Current.Turns.Count} turn rows were written after disposal");
                Assert.Null(model.Runs.HostRefusal);
                host.Dispose();
            });
    }

    /// <summary>
    /// The shell's real shutdown (<see cref="WorkbenchShell.Dispose"/>, which <c>MainWindow</c>'s <c>Closed</c> calls),
    /// while a revived notebook's read-back is held in the daemon. The shell runs over a real <see cref="WorkspaceClient"/>
    /// to an in-process daemon, and only that daemon's <c>attempt.read</c> is gated. The read-back ends quietly with no turn.
    /// </summary>
    [Fact]
    public async Task TheShellShuttingDown_WhileARevivedNotebookReadsBack_EndsTheReadBackQuietly_AndWritesNoTurn()
    {
        var accounts = new[] { new AccountRef("anthropic", "max") };
        new SessionConfigStore(_fixture.Root, NotebookFixture.SessionId).Create("Binding", _fixture.Root, accounts, accounts[0], DateTimeOffset.UnixEpoch);
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) => await RecordTwoAttemptsAsync(window));

        var pipe = "aide-t17e-dispose-" + Guid.NewGuid().ToString("N");
        var capabilities = new CapabilityRegistry();
        var endpoint = new DaemonEndpoint(pipe, capabilities, _ => _fixture.Store.CoreEpoch);
        DaemonOperations.Register(endpoint, () => _fixture.Store.CoreEpoch);
        WorkspaceOperations.RegisterNotebook(endpoint, new NotebookService(_fixture.Store));
        var dispatcher = new BoundaryDispatcher(_fixture.Store);
        WorkspaceOperations.RegisterDispatch(endpoint, dispatcher);

        // The gate, on the wire: attempt.read waits, then runs the daemon's own handler (a second endpoint over the same
        // registry, so the client's capability holds there too).
        var unheld = new DaemonEndpoint(pipe, capabilities, _ => _fixture.Store.CoreEpoch);
        WorkspaceOperations.RegisterDispatch(unheld, dispatcher);
        var arrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var released = new ManualResetEventSlim();
        endpoint.Register(WorkspaceOperations.AttemptRead, (request, peer) =>
        {
            arrived.TrySetResult();
            released.Wait(NotebookFixture.Bound);
            return unheld.Invoke(request, peer);
        });

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
                    WorkbenchShell? shell = new(client);
                    try
                    {
                        border.Child = shell.WorkbenchRoot;
                        var surfaceId = SessionDocumentSurface.SurfaceIdFor(NotebookFixture.SessionId);
                        shell.Service.Apply(new LayoutOperation.AddSurface(shell.Service.Current.AllStacks().First().Id, new Surface(surfaceId, SessionDocumentSurface.Kind, "Binding")));
                        shell.Adapter.Render();
                        Assert.Equal([NotebookFixture.SessionId], shell.ReviveRestoredSessionDocuments(_fixture.Root).Select(c => c.SessionId));
                        shell.Adapter.Render();
                        var model = shell.Notebook(NotebookId.ForSession(NotebookFixture.SessionId))!;

                        await arrived.Task.WaitAsync(NotebookFixture.Bound);
                        var readBack = model.Runs.ReadingBack;
                        Assert.False(readBack.IsCompleted, "the read-back is not pending while its read is held");

                        border.Child = null;
                        shell.Dispose();
                        shell = null;
                        released.Set();

                        var escaped = await Record.ExceptionAsync(() => readBack.WaitAsync(NotebookFixture.Bound));
                        Assert.True(escaped is null, $"the read-back pending at shutdown ended with {escaped?.GetType().Name}: {escaped?.Message}");
                        await Settle(window, rounds: 10);
                        Assert.True(model.Thread.Current.Turns.Count == 0, $"{model.Thread.Current.Turns.Count} turn rows were written after the shell shut down");
                    }
                    finally
                    {
                        released.Set();
                        shell?.Dispose();
                    }
                });
        }
        finally
        {
            await life.CancelAsync();
            try
            {
                await running;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    /// <summary>
    /// A reopen in the shell's order (the host binds after the document is built) whose read-back's store read is held:
    /// returns once the read has arrived at the gate, with the read-back task that is pending.
    /// </summary>
    private async Task<(NotebookModel Model, SessionDocumentSurface Surface, AttemptHost Host, Gate Gate, Task ReadBack)> ReopenWithTheReadHeldAsync(Window window)
    {
        var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
        var gate = new Gate();
        var opened = new TaskCompletionSource<IAttemptHost>(TaskCreationOptions.RunContinuationsAsynchronously);
        var model = _fixture.NewModel();
        model.Runs.BindHostWhenOpen(() => opened.Task, Scope);
        var surface = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
        window.Content = surface;
        opened.SetResult(HeldReads.Over(host, gate));
        await gate.Arrived.Task.WaitAsync(NotebookFixture.Bound);
        var readBack = model.Runs.ReadingBack;
        Assert.False(readBack.IsCompleted, "the read-back is not pending while its read is held");
        return (model, surface, host, gate, readBack);
    }

    /// <summary>Today's window: two answered turns, each an attempt of its revision.</summary>
    private async Task RecordTwoAttemptsAsync(Window window)
    {
        Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
        _fixture.ReleaseRun();
        var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
        var model = _fixture.NewModel();
        model.Runs.BindHost(host, Scope);
        var surface = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
        window.Content = surface;
        _fixture.ConfigureStub(surface);
        surface.Composer.Draft.SetFreeFormText("first words");
        await NotebookFixture.SendAndSettleAsync(surface);
        await UntilAsync(window, () => Ended(model, 0), () => "A never ended");
        surface.Composer.Draft.SetFreeFormText("second words");
        await NotebookFixture.SendAndSettleAsync(surface);
        await UntilAsync(window, () => Ended(model, 1), () => "B never ended");
        window.Content = null;
        surface.Dispose();
        model.Dispose();
        host.Dispose();
    }

    private static bool Ended(NotebookModel model, int index) =>
        model.Thread.Current.Turns.Count > index && model.Thread.Current.Turns[index].State is not (TurnState.Running or TurnState.Preparing);

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

    private static async Task Settle(Window window, int rounds)
    {
        for (var i = 0; i < rounds; i++)
        {
            await Task.Delay(20);
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        }
    }

    /// <summary>The held read: <see cref="Arrived"/> when it reaches the gate, and it goes on to the store once <see cref="Released"/>.</summary>
    public sealed class Gate
    {
        public TaskCompletionSource Arrived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Released { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>The real host, whose read-back call (<see cref="IAttemptHost.ReadAsync"/>) waits at a <see cref="Gate"/>.</summary>
    public class HeldReads : DispatchProxy
    {
        private IAttemptHost _inner = null!;
        private Gate _gate = null!;

        public static IAttemptHost Over(IAttemptHost inner, Gate gate)
        {
            var proxy = Create<IAttemptHost, HeldReads>();
            ((HeldReads)(object)proxy)._inner = inner;
            ((HeldReads)(object)proxy)._gate = gate;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == nameof(IAttemptHost.ReadAsync) && args is [string dispatchKey, CancellationToken cancellationToken])
            {
                return HeldAsync(dispatchKey, cancellationToken);
            }

            try
            {
                return targetMethod.Invoke(_inner, args);
            }
            catch (TargetInvocationException error) when (error.InnerException is { } inner)
            {
                ExceptionDispatchInfo.Throw(inner);
                throw;
            }
        }

        private async Task<AttemptReceipt?> HeldAsync(string dispatchKey, CancellationToken cancellationToken)
        {
            _gate.Arrived.TrySetResult();
            await _gate.Released.Task.ConfigureAwait(false);
            return await _inner.ReadAsync(dispatchKey, cancellationToken).ConfigureAwait(false);
        }
    }
}
