using System.IO;
using System.Runtime.Versioning;
using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Records;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Records;

/// <summary>
/// The product shell opens the workspace's <see cref="AttemptHost"/> over the client it is handed (ADR-0041) and binds
/// every session notebook to it at the shell's own construction site, so a sent turn in the product is an attempt the
/// host runs — through <see cref="GovernedRunHost.RunAsync"/>, still the one composition root.
/// </summary>
/// <remarks>
/// The client is a real <see cref="WorkspaceClient"/> over a named pipe to a daemon endpoint with the attempt ops over a
/// real schema-v2 store — the composition <c>MainWindow</c> hands the shell; the engine is
/// <see cref="Conductor.StubAcpAdapter"/>.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class TheShellOpensTheAttemptHostTests : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ASentTurn_InTheShellsSessionDocument_RunsThroughTheHostTheShellOpened()
    {
        var pipe = "aide-t8-host-" + Guid.NewGuid().ToString("N");
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
            var config = new SessionConfig(NotebookFixture.SessionId, "Binding", _fixture.Root, DateTimeOffset.UnixEpoch,
                [new AccountRef("anthropic", "max")], new AccountRef("anthropic", "max"));
            Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));

            string? dispatchKey = null;
            long roots = -1;
            Sta.Pump(
                create: () => new Border(),
                configure: window => { window.Width = 1200; window.Height = 800; },
                body: async (window, border) =>
                {
                    using var shell = new WorkbenchShell(client);
                    shell.OpenSessionDocument(config);
                    border.Child = shell.WorkbenchRoot;
                    var model = shell.Notebook(NotebookId.ForSession(config.SessionId))!;
                    await UntilAsync(window, () => model.Runs.Host is AttemptHost, () => "the shell never opened and bound an attempt host");

                    var composer = shell.SessionComposer(config.SessionId)!;
                    _fixture.ConfigureStub(composer);
                    _fixture.ReleaseRun();
                    using (var ledger = CompositionRootLedger.Open())
                    {
                        composer.Draft.SetFreeFormText("say ok");
                        Assert.NotNull(composer.Send());
                        await UntilAsync(window, () => model.Thread.Current.Turns is [{ State: not (TurnState.Running or TurnState.Preparing) }],
                            () => $"the turn never ended; it reads {model.Thread.Current.Turns[0].State}");
                        roots = ledger.Roots;
                    }

                    var turn = model.Thread.Current.Turns[0];
                    Assert.True(turn.State == TurnState.Answered,
                        $"the turn reads {turn.State}; events [{string.Join(", ", turn.Events.Select(e => e.Text))}]");
                    dispatchKey = model.Runs.AttemptOf(turn.Ordinal)?.DispatchKey;
                    border.Child = null;
                });

            // The attempt is the daemon's row, written by the host over the client: the product path, not a test store's.
            Assert.NotNull(dispatchKey);
            var receipt = await client.ReadAttemptAsync(dispatchKey!, CancellationToken.None);
            Assert.Equal(DispatchState.ResponseComplete, receipt?.Terminal?.State);
            Assert.Equal(1, roots);

            // The shell's disposal closed its host: the workspace's host lock is free again.
            using var next = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store),
                Path.Combine(_fixture.Root, ".aide", "attempt-host.lock"), CancellationToken.None);
        }
        finally
        {
            await life.CancelAsync();
            try { await running; } catch (OperationCanceledException) { }
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
}
