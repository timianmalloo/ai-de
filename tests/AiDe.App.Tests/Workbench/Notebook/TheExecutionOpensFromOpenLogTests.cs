using System.IO;
using System.Runtime.Versioning;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Tests.Records;
using AiDe.App.Tests.Shell;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Execution;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Kinds;

/// <summary>
/// The WL-S design's §4 (seam <c>req-01M59XNJH52JRRZ1DAJYK40X9D</c>): a turn's <i>Open the log</i> opens its attempt's
/// execution — <c>execution:&lt;DispatchKey&gt;</c>, read through <see cref="NotebookRuns.AttemptOf"/> — in the Bottom
/// zone, and a second press focuses the one already open instead of adding another.
/// </summary>
/// <remarks>
/// The shell is the product's, over a real <see cref="WorkspaceClient"/> to a daemon endpoint with the attempt ops over a
/// real schema-v2 store (the <c>TheShellOpensTheAttemptHostTests</c> composition), so the attempt is the host's row.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class TheExecutionOpensFromOpenLogTests : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task OpenLog_OpensOneExecutionForTheTurnsAttempt_InTheBottom_AndASecondPressFocusesIt()
    {
        var pipe = "aide-t11-exec-" + Guid.NewGuid().ToString("N");
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

            Sta.Pump(
                create: () => new Border(),
                configure: window => { window.Width = 1440; window.Height = 900; },
                body: async (window, border) =>
                {
                    using var shell = new WorkbenchShell(client);
                    shell.OpenSessionDocument(config);
                    border.Child = shell.WorkbenchRoot;
                    var model = shell.Notebook(NotebookId.ForSession(config.SessionId))!;
                    await UntilAsync(window, () => model.Runs.Host is AttemptHost, () => "the shell never opened and bound an attempt host");

                    var sessionId = SessionDocumentSurface.SurfaceIdFor(config.SessionId);
                    var document = DocumentOf(shell, sessionId);

                    // An engine not in the catalog: the host records the attempt and the run concludes Failed, so the
                    // last turn offers "Open the log".
                    _fixture.Configure(document);
                    document.Composer.Draft.SetFreeFormText("say ok");
                    Assert.NotNull(document.Composer.Send());
                    await UntilAsync(window, () => model.Thread.Current.Turns is [{ State: TurnState.Failed }],
                        () => $"the turn never failed; it reads {model.Thread.Current.Turns.FirstOrDefault()?.State}");
                    var turn = model.Thread.Current.Turns[0];
                    await UntilAsync(window, () => model.Runs.AttemptOf(turn.Ordinal) is not null, () => "the failed turn has no attempt");
                    var key = model.Runs.AttemptOf(turn.Ordinal)!.Attempt.DispatchKey;
                    var executionId = "execution:" + key;

                    // The operator's press, as UI Automation makes it.
                    await PressOpenLogAsync(window, document);

                    var zones = shell.Factory.Service.Zones;
                    var execution = Assert.Single(zones.AllSurfaces(), s => s.Kind == "execution");
                    Assert.Equal(executionId, execution.SurfaceId);
                    Assert.Equal("Execution — " + key, execution.Title);
                    Assert.Equal(ZoneId.Bottom, zones.FindZoneOf(executionId));
                    Assert.False(zones.Zone(ZoneId.Bottom).Collapsed, "the execution opened into a collapsed Bottom");
                    Assert.Equal(executionId, ((ZoneStack)zones.Zone(ZoneId.Bottom).Content!).Active.SurfaceId);
                    Assert.Equal(executionId, shell.Factory.Adapter.ActiveSurfaceId);
                    Assert.DoesNotContain(zones.AllSurfaces(), s => s.Kind == "console");   // the log is no longer the Console

                    // Rendered, and named for its attempt in UIA.
                    var content = shell.Factory.Adapter.ContentFor(executionId);
                    Assert.NotNull(content);
                    var surface = ComposedCoding.Visuals<ExecutionSurface>(content!).FirstOrDefault() ?? content as ExecutionSurface;
                    Assert.True(surface is not null, "the execution renders no ExecutionSurface");
                    Assert.Equal("Execution — " + key, AutomationProperties.GetName(surface));

                    // Focus moves back to the notebook; a second press focuses the one execution, never a second.
                    Assert.True(shell.Factory.Service.Apply(new LayoutOperation.ActivateSurface(sessionId)).Applied);
                    shell.Factory.Adapter.Render();
                    await Settle(window);
                    Assert.Equal(sessionId, shell.Factory.Adapter.ActiveSurfaceId);

                    await PressOpenLogAsync(window, DocumentOf(shell, sessionId));
                    Assert.Single(shell.Factory.Service.Zones.AllSurfaces(), s => s.Kind == "execution");
                    Assert.Equal(executionId, shell.Factory.Adapter.ActiveSurfaceId);
                    border.Child = null;
                });
        }
        finally
        {
            await life.CancelAsync();
            try { await running; } catch (OperationCanceledException) { }
        }
    }

    private static SessionDocumentSurface DocumentOf(WorkbenchShell shell, string surfaceId)
    {
        var content = shell.Factory.Adapter.ContentFor(surfaceId);
        Assert.NotNull(content);
        var document = ComposedCoding.Visuals<SessionDocumentSurface>(content!).FirstOrDefault() ?? content as SessionDocumentSurface;
        Assert.True(document is not null, $"'{surfaceId}' renders no SessionDocumentSurface");
        return document!;
    }

    private static async Task PressOpenLogAsync(System.Windows.Window window, SessionDocumentSurface document)
    {
        await Settle(window);
        var button = ComposedCoding.Visuals<Button>(document.Thread).FirstOrDefault(b => b.Tag is TurnActionKind.OpenLog);
        Assert.True(button is not null, "the failed turn shows no \"Open the log\" button");
        var peer = UIElementAutomationPeer.CreatePeerForElement(button);
        Assert.IsAssignableFrom<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke)).Invoke();
        await Settle(window);
    }

    private static async Task Settle(System.Windows.Window window)
    {
        window.UpdateLayout();
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        window.UpdateLayout();
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
