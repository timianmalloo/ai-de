using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Tests.Records;
using AiDe.App.Tests.Shell;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Checklist;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Dispatch;
using AiDe.Core.Ipc;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Checklist;

/// <summary>
/// The product's shell composed as <c>MainWindow</c> composes it — the three hosts under one <see cref="PerspectiveShell"/>
/// whose router every command goes through — over a real <see cref="WorkspaceClient"/> to a daemon endpoint carrying the
/// notebook, attempt and checklist operations over a real schema-v2 store. Every step is the user's gesture: a menu
/// item invoked through UI Automation, a button invoked through UI Automation, text set through the value pattern.
/// </summary>
internal sealed class ChecklistHarness : IDisposable
{
    public NotebookFixture Fixture { get; } = new();

    public void Dispose() => Fixture.Dispose();

    public SessionConfig Config(string sessionId, string title) =>
        new(sessionId, title, Fixture.Root, DateTimeOffset.UnixEpoch, [new AccountRef("anthropic", "max")], new AccountRef("anthropic", "max"));

    /// <summary>Starts the daemon endpoint, connects the client, and runs <paramref name="body"/> on a pumped STA window.</summary>
    public async Task RunAsync(Func<Window, ContentControl, WorkspaceClient, Task> body)
    {
        var pipe = "aide-t26-checklist-" + Guid.NewGuid().ToString("N");
        var endpoint = new DaemonEndpoint(pipe, new CapabilityRegistry(), _ => Fixture.Store.CoreEpoch);
        DaemonOperations.Register(endpoint, () => Fixture.Store.CoreEpoch);
        WorkspaceOperations.RegisterNotebook(endpoint, new NotebookService(Fixture.Store));
        WorkspaceOperations.RegisterDispatch(endpoint, new BoundaryDispatcher(Fixture.Store));
        WorkspaceOperations.RegisterChecklist(endpoint, new ChecklistService(Fixture.Store));
        var server = new IpcServer(pipe, endpoint, new IpcServerOptions(StartupGrace: TimeSpan.FromSeconds(45)));
        using var life = new CancellationTokenSource(TimeSpan.FromSeconds(170));
        var running = server.RunAsync(life.Token);

        try
        {
            await using var client = await WorkspaceClient.ConnectAsync(pipe, TimeSpan.FromSeconds(20), CancellationToken.None);
            Sta.Pump(
                create: () => new ContentControl(),
                configure: window => { window.Width = 1440; window.Height = 900; },
                body: (window, region) => body(window, region, client),
                timeoutSeconds: 160);
        }
        finally
        {
            await life.CancelAsync();
            try { await running; } catch (OperationCanceledException) { }
        }
    }

    /// <summary>The window's composition: the router over the three hosts, with the active host's body in <paramref name="region"/>.</summary>
    public static PerspectiveShell Compose(WorkbenchShell shell, ContentControl region)
    {
        var perspectives = new PerspectiveShell(region, shell.Hosts, shell.Announcer);
        shell.CommandRouter = perspectives.Execute;
        return perspectives;
    }

    /// <summary>File → New Session's end: the notebook registered and docked, its attempt host bound.</summary>
    public async Task<SessionDocumentSurface> OpenNotebookAsync(Window window, WorkbenchShell shell, SessionConfig config)
    {
        Directory.CreateDirectory(SessionPaths.SessionDirectory(Fixture.Root, config.SessionId));
        shell.OpenSessionDocument(config);
        var model = shell.Notebook(NotebookId.ForSession(config.SessionId))!;
        await UntilAsync(window, () => model.Runs.Host is AttemptHost, () => "the shell never opened and bound an attempt host");
        await SettleAsync(window);
        return DocumentOf(shell, config.SessionId);
    }

    public static SessionDocumentSurface DocumentOf(WorkbenchShell shell, string sessionId)
    {
        var content = shell.Factory.Adapter.ContentFor(SessionDocumentSurface.SurfaceIdFor(sessionId));
        Assert.NotNull(content);
        var document = ComposedCoding.Visuals<SessionDocumentSurface>(content!).FirstOrDefault() ?? content as SessionDocumentSurface;
        Assert.True(document is not null, $"'{sessionId}' renders no SessionDocumentSurface");
        return document!;
    }

    /// <summary>
    /// <see cref="NotebookFixture.ConfigureStub(SessionDocumentSurface)"/>'s wiring with the task class the window would
    /// pass: the class is in the projection's domain and the draft is not, so changing it changes the live compile
    /// while the draft stays the same.
    /// </summary>
    public void ConfigureStub(SessionDocumentSurface document, string taskClass)
    {
        var install = Path.Combine(Fixture.Root, "install");
        Conductor.StubAcpAdapter.Install(install);
        document.Composer.Configure(
            new SessionConfig(NotebookFixture.SessionId, "Binding", Fixture.Root, DateTimeOffset.UnixEpoch, [new AccountRef("anthropic", "max")], new AccountRef("anthropic", "max")),
            new ComposerSendContext(
                RepositoryRoot: Fixture.Root,
                DataDirectory: Path.Combine(Fixture.Root, "data"),
                AdapterInstallRoot: install,
                EngineId: "claude-code",
                Model: "claude-sonnet-5",
                AccountLabel: "max",
                TaskClass: taskClass,
                ProofPackArtifacts: [],
                Providers: [new ProviderRow("anthropic", ProviderAuth.Subscription, [new ProviderAccount("max", AccountHealth.Ready)])]),
            ComposerFields.FreeForm(),
            new AttachmentGate(Fixture.Root, new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max"));
        document.Composer.Gate.Compiler = (_, _) => throw new OperationCanceledException();
        document.Composer.Draft.SwitchTo(ComposerShape.FreeForm);
    }

    /// <summary>Types the words and sends them through the stub harness, which answers: one Completed turn.</summary>
    /// <param name="taskClass">The run's task class; another class over the same words is a new projection of the same revision.</param>
    public async Task<string> RunToCompletionAsync(Window window, SessionDocumentSurface document, string words, string taskClass = "implement")
    {
        ConfigureStub(document, taskClass);
        Fixture.ReleaseRun();
        var turns = document.Notebook.Thread.Current.Turns.Count;
        document.Composer.Draft.SetFreeFormText(words);
        await NotebookFixture.SendAndSettleAsync(document);
        await UntilAsync(window, () => document.Notebook.Thread.Current.Turns.Count == turns + 1
                && document.Notebook.Thread.Current.Turns[^1].State is TurnState.Completed or TurnState.Answered,
            () => $"the turn never completed; it reads {document.Notebook.Thread.Current.Turns.LastOrDefault()?.State}");
        var turn = document.Notebook.Thread.Current.Turns[^1];
        await UntilAsync(window, () => document.Notebook.Runs.AttemptOf(turn.Ordinal) is not null, () => "the completed turn has no attempt");
        return document.Notebook.Runs.AttemptOf(turn.Ordinal)!.Attempt.DispatchKey;
    }

    /// <summary>View → New checklist, as the window's menu offers it in the active perspective, invoked through UI Automation.</summary>
    public static async Task<string> OpenChecklistFromViewAsync(Window window, WorkbenchShell shell, PerspectiveShell perspectives)
    {
        var menu = new Menu();
        MainMenuBuilder.Build(menu, shell.Execute, PerspectiveMenu.For(perspectives.Active));
        var view = menu.Items.OfType<MenuItem>().Single(m => Equals(m.Header, "_View"));
        var item = view.Items.OfType<MenuItem>().FirstOrDefault(m => Equals(m.Header, "New checklist"));
        Assert.True(item is not null,
            $"View in {perspectives.Active.Id} offers no \"New checklist\"; it offers [{string.Join(", ", view.Items.OfType<MenuItem>().Select(m => m.Header))}]");
        Invoke(item!);
        await SettleAsync(window);
        return (shell.Announcer as WorkbenchAnnouncer)?.Last ?? string.Empty;
    }

    /// <summary>The checklist surface open in <paramref name="host"/>, waited for until its refresh settles.</summary>
    public static async Task<ChecklistSurface> ChecklistIn(Window window, DockHost host, string surfaceId)
    {
        host.Adapter.Render();
        await SettleAsync(window);
        var content = host.Adapter.ContentFor(surfaceId);
        Assert.True(content is not null, $"{host.Row.Id} renders no pane for '{surfaceId}'");
        var surface = ComposedCoding.Visuals<ChecklistSurface>(content!).FirstOrDefault() ?? content as ChecklistSurface;
        Assert.True(surface is not null, $"'{surfaceId}' renders no ChecklistSurface");
        await SettledAsync(window, surface!);
        return surface!;
    }

    /// <summary>Waits for the surface's last refresh or action, and for any refresh a store change queued behind it.</summary>
    public static async Task SettledAsync(Window window, ChecklistSurface surface)
    {
        for (var round = 0; round < 3; round++)
        {
            await surface.LastAction.WaitAsync(NotebookFixture.Bound);
            await SettleAsync(window);
        }
    }

    public static async Task PressAsync(Window window, ChecklistSurface surface, string name)
    {
        var button = Buttons(surface).FirstOrDefault(b => AutomationProperties.GetName(b) == name);
        Assert.True(button is not null, $"the checklist shows no button named \"{name}\"; it shows [{string.Join(" | ", Buttons(surface).Select(AutomationProperties.GetName))}]");
        Assert.True(button!.IsEnabled, $"\"{name}\" is disabled");
        Invoke(button);
        await SettledAsync(window, surface);
    }

    public static async Task TypeObligationAsync(Window window, ChecklistSurface surface, string text)
    {
        var box = ComposedCoding.Visuals<TextBox>(surface).Single(t => AutomationProperties.GetName(t) == ChecklistCopy.NewObligationName);
        var peer = UIElementAutomationPeer.CreatePeerForElement(box);
        Assert.IsAssignableFrom<IValueProvider>(peer.GetPattern(PatternInterface.Value)).SetValue(text);
        await SettleAsync(window);
    }

    public static IEnumerable<Button> Buttons(ChecklistSurface surface) => ComposedCoding.Visuals<Button>(surface);

    /// <summary>Every row's UIA name: the obligation, its evidence label, its human label.</summary>
    public static IReadOnlyList<string> RowNames(ChecklistSurface surface) =>
        [.. ComposedCoding.Visuals<Border>(surface).Where(b => AutomationProperties.GetAutomationId(b) == "checklist.row").Select(AutomationProperties.GetName)];

    /// <summary>Every string the surface renders, for the Ruling 179 oracle.</summary>
    public static IReadOnlyList<string> AllText(ChecklistSurface surface) =>
        [.. ComposedCoding.Visuals<TextBlock>(surface).Select(t => t.Text),
            .. ComposedCoding.Visuals<FrameworkElement>(surface).Select(AutomationProperties.GetName),
            .. Buttons(surface).Select(b => b.Content as string ?? string.Empty)];

    public static async Task SettleAsync(Window window, int rounds = 2)
    {
        for (var i = 0; i < rounds; i++)
        {
            window.UpdateLayout();
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        }

        window.UpdateLayout();
    }

    public static async Task UntilAsync(Window window, Func<bool> condition, Func<string> what)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        }

        Assert.True(condition(), what());
    }

    private sealed class NeverAsked : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
    }

    private static void Invoke(UIElement element)
    {
        var peer = UIElementAutomationPeer.CreatePeerForElement(element);
        Assert.IsAssignableFrom<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke)).Invoke();
    }
}
