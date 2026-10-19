using System.ComponentModel;
using System.IO;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Tests.Records;
using AiDe.App.Tests.Shell;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Perspectives;

/// <summary>
/// <b>T21 close condition 4 (Ruling 171 (3)).</b> A live notebook floats and redocks, and the run goes on through both.
/// </summary>
/// <remarks>
/// <para><b>The composition is the product's.</b> <see cref="WorkbenchShell"/> over a real <see cref="WorkspaceClient"/> to a
/// daemon endpoint, with the <c>notebook</c> kind and an <see cref="AttemptHost"/> the shell binds itself, as
/// <c>ALiveNotebookSurvivesHideAndReopenTests</c> builds it. The run is real: the composition root spawns
/// <see cref="AiDe.App.Tests.Conductor.StubAcpAdapter"/>, which holds the prompt until <see cref="NotebookFixture.ReleaseRun"/>.</para>
/// <para><b>The float is the user's gesture.</b> Keyboard focus goes into the notebook, so the shell's own focus tracking
/// names the pane. Then the command palette runs <i>Float pane</i>, its keyboard route: no chord binds
/// <c>Ctrl+K, F</c> (<c>KeyGestures.For</c> binds single-chord gestures only). Nothing in the test renders by hand.</para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed partial class ALiveNotebookFloatsAndRedocksTests : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    /// <summary>(A) A notebook with a turn running floats, and the run continues and completes in the floating window.</summary>
    [Fact]
    public Task ANotebookFloatedMidRun_KeepsItsModelAndItsOneAttempt_AndTheTurnCompletesInTheFloatingWindow() =>
        WithDaemonAsync((client, config) =>
            Sta.Pump(
                create: () => new Border(),
                configure: window => { window.Width = 1440; window.Height = 900; },
                body: async (window, border) =>
                {
                    using var shell = new WorkbenchShell(client);
                    var live = await OpenALiveNotebookAsync(window, border, shell, config);
                    var eventsBefore = live.Model.Thread.Current.Turns[0].Events.ToList();

                    await FloatByThePaletteAsync(window, shell, live);

                    // One floating window holds the notebook, and the pane is in no zone.
                    var zones = shell.Factory.Service.Zones;
                    Assert.True(zones.FindZoneOf(live.SurfaceId) is null, $"the notebook is still docked in {zones.FindZoneOf(live.SurfaceId)}; the shell said [{live.Heard()}]");
                    Assert.Equal(live.SurfaceId, Assert.Single(Assert.Single(zones.Floating).Surfaces).SurfaceId);
                    Assert.True(shell.Factory.Manager.FloatingWindows.Count() == 1,
                        $"the model floats the notebook, but the view shows {shell.Factory.Manager.FloatingWindows.Count()} floating window(s); the shell said [{live.Heard()}]");
                    var floating = Assert.Single(shell.Factory.Manager.FloatingWindows);
                    Assert.True(floating.IsVisible, "the notebook's floating window is not shown");
                    Assert.Same(live.Document, DocumentOf(shell, live.SurfaceId));
                    Assert.True(FloatRenderTests.HostedBy(live.Document, floating), "the floated notebook is not drawn in the floating window");
                    Assert.False(FloatRenderTests.HostedBy(live.Document, window), "the floated notebook is still drawn in the workbench window");

                    // The same model and the same attempt, still in flight.
                    Assert.Same(live.Model, shell.Notebook(live.Id));
                    Assert.Same(live.Model, live.Document.Notebook);
                    Assert.False(live.Model.IsDisposed, "floating the pane disposed the notebook's model");
                    Assert.Equal(TurnState.Running, live.Model.Thread.Current.Turns[0].State);
                    Assert.True(live.Model.Runs.InFlight, "floating the pane left no run in flight");
                    Assert.Equal(live.Key, live.Model.Runs.AttemptOf(live.Ordinal)!.Attempt.DispatchKey);

                    // The turn completes inside the floating window.
                    _fixture.ReleaseRun();
                    await UntilAsync(window, () => live.Document.ReadModel.Current.Turns is [{ State: TurnState.Answered }],
                        () => $"the floating notebook never heard the run end; it reads {live.Document.ReadModel.Current.Turns.FirstOrDefault()?.State}");
                    Assert.True(FloatRenderTests.HostedBy(live.Document, Assert.Single(shell.Factory.Manager.FloatingWindows)), "the turn completed, but not in the floating window");
                    var turn = Assert.Single(live.Model.Thread.Current.Turns);
                    Assert.NotNull(turn.Outcome);

                    // No restart, no second dispatch, no lost events.
                    var receipt = live.Model.Runs.AttemptOf(live.Ordinal)!;
                    Assert.Equal(live.Key, receipt.Attempt.DispatchKey);
                    Assert.True(AttemptReceipt.TerminalStates.Contains(receipt.Outcomes[^1].State), $"the attempt's last outcome is {receipt.Outcomes[^1].State}, not terminal");
                    var (processes, prompts) = Dispatches();
                    Assert.True(processes == 1 && prompts == 1, $"the float restarted or re-sent the run: {processes} stub process(es), {prompts} session/prompt request(s)\n{AiDe.App.Tests.Conductor.StubAcpAdapter.Traces(_fixture.Root)}");
                    Assert.True(turn.Events.Count >= eventsBefore.Count && turn.Events.Take(eventsBefore.Count).SequenceEqual(eventsBefore),
                        $"the float lost events: {eventsBefore.Count} before the float, {turn.Events.Count} at the end");
                    Assert.True(turn.Events.Count > eventsBefore.Count, "no event arrived after the float");
                    Assert.DoesNotContain(SessionDocumentSurface.PaneHiddenCopy, live.Said);
                    border.Child = null;
                }));

    /// <summary>A notebook open in the shell's Center with one turn held running by the stub.</summary>
    private sealed record Live(NotebookId Id, string SurfaceId, NotebookModel Model, SessionDocumentSurface Document, int Ordinal, string Key, List<string> Said)
    {
        public string Heard() => string.Join(" | ", Said);
    }

    private async Task<Live> OpenALiveNotebookAsync(Window window, Border border, WorkbenchShell shell, SessionConfig config)
    {
        var said = new List<string>();
        DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock))
            .AddValueChanged(shell.LiveRegion, (_, _) => said.Add(shell.LiveRegion.Text));

        shell.OpenSessionDocument(config);
        border.Child = shell.WorkbenchRoot;
        var id = NotebookId.ForSession(config.SessionId);
        var model = shell.Notebook(id)!;
        await UntilAsync(window, () => model.Runs.Host is AttemptHost, () => "the shell never opened and bound an attempt host");

        var surfaceId = SessionDocumentSurface.SurfaceIdFor(config.SessionId);
        Assert.Equal(ZoneId.Center, shell.Factory.Service.Zones.FindZoneOf(surfaceId));
        var document = DocumentOf(shell, surfaceId);
        _fixture.ConfigureStub(document);
        document.Composer.Draft.SetFreeFormText("hold");
        Assert.NotNull(document.Composer.Send());
        await UntilAsync(window, () => _fixture.RunOpened && model.Thread.Current.Turns is [{ State: TurnState.Running }],
            () => $"the run never went live; the turn reads {model.Thread.Current.Turns.FirstOrDefault()?.State} (is node on PATH?)");
        var ordinal = model.Thread.Current.Turns[0].Ordinal;
        await UntilAsync(window, () => model.Runs.AttemptOf(ordinal) is not null, () => "the live turn has no attempt");
        await UntilAsync(window, () => model.Thread.Current.Turns[0].Events.Count > 0, () => "the live turn has no event before the float");
        return new Live(id, surfaceId, model, document, ordinal, model.Runs.AttemptOf(ordinal)!.Attempt.DispatchKey, said);
    }

    /// <summary>The user's float: focus inside the notebook, then the palette's <i>Float pane</i> by the keyboard.</summary>
    private static async Task FloatByThePaletteAsync(Window window, WorkbenchShell shell, Live live)
    {
        await Settle(window);
        Assert.True(live.Document.ConsoleToggle.Focus(), "the notebook took no keyboard focus");
        await Settle(window);
        Assert.True(shell.Controller.FocusedSurfaceId == live.SurfaceId, $"the shell's focus tracking names '{shell.Controller.FocusedSurfaceId}', not the notebook");

        shell.Palette.Open();
        shell.Palette.SearchBox.Text = "Float pane";
        Assert.Equal("workbench.floatPane", shell.Palette.Visible.FirstOrDefault()?.Id);
        Assert.True(shell.Palette.HandleKey(Key.Enter), "the palette did not take Enter");
        await Settle(window);
        await Task.Delay(300);
        await Settle(window);
    }

    /// <summary>How many stub processes ran, and how many <c>session/prompt</c> requests they received in all.</summary>
    private (int Processes, int Prompts) Dispatches()
    {
        var logs = Directory.EnumerateFiles(_fixture.Root, ".aide-stub-*.log").ToList();
        var prompts = logs.SelectMany(File.ReadAllLines).Count(line => ReceivedPrompt().IsMatch(line));
        return (logs.Count, prompts);
    }

    [GeneratedRegex("""^\S+ < .*"method"\s*:\s*"session/prompt"\s*""")]
    private static partial Regex ReceivedPrompt();

    /// <summary>A daemon endpoint over the fixture's store, a <see cref="WorkspaceClient"/> to it, and the session's config, saved.</summary>
    private async Task WithDaemonAsync(Action<WorkspaceClient, SessionConfig> body)
    {
        var pipe = "aide-t21-float-" + Guid.NewGuid().ToString("N");
        var endpoint = new DaemonEndpoint(pipe, new CapabilityRegistry(), _ => _fixture.Store.CoreEpoch);
        DaemonOperations.Register(endpoint, () => _fixture.Store.CoreEpoch);
        WorkspaceOperations.RegisterNotebook(endpoint, new NotebookService(_fixture.Store));
        WorkspaceOperations.RegisterDispatch(endpoint, new BoundaryDispatcher(_fixture.Store));
        var server = new IpcServer(pipe, endpoint, new IpcServerOptions(StartupGrace: TimeSpan.FromSeconds(45)));
        using var life = new CancellationTokenSource(TimeSpan.FromSeconds(150));
        var running = server.RunAsync(life.Token);

        try
        {
            await using var client = await WorkspaceClient.ConnectAsync(pipe, TimeSpan.FromSeconds(20), CancellationToken.None);
            var accounts = new[] { new AccountRef("anthropic", "max") };
            var config = new SessionConfigStore(_fixture.Root, NotebookFixture.SessionId).Create("Binding", _fixture.Root, accounts, accounts[0], DateTimeOffset.UnixEpoch);
            body(client, config);
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

    private static async Task Settle(Window window)
    {
        for (var i = 0; i < 3; i++)
        {
            window.UpdateLayout();
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        }

        window.UpdateLayout();
    }

    private static async Task UntilAsync(Window window, Func<bool> condition, Func<string> what)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        }

        Assert.True(condition(), what());
    }
}
