using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using AiDe.App.Tests.Sessions;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Workbench;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>T30 n2 (A) — Ruling 180 (1) through the real menu.</b> <c>File → New Session</c>, clicked through the menu item's
/// invoke peer, runs <see cref="NewSessionFlow"/>; the session it writes opens <c>agentic-advisory</c> when Gate 1
/// admits and <c>mechanical-only</c> when it refuses. Only the gate's input is injected
/// (<see cref="NewSessionSheetViewModel.CompileModeGateInput"/>, set on the sheet the flow shows), so the red is
/// deterministic and no test reads <c>~/.aide/proof</c>.
/// </summary>
public sealed class FileNewSessionOpensInTheGatesDefaultTests(ITestOutputHelper output) : IDisposable
{
    private readonly string _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "aide-t30-new-" + Guid.NewGuid().ToString("N"))).FullName;

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    /// <summary>Gate 1 admits, Gate 2 refuses: the ladder as this machine reads it today (Ruling 180's Owner check).</summary>
    private static CompileModeAvailability GateOneAdmits() => new(
        new Dictionary<string, CompileModeRefusal>(StringComparer.Ordinal) { [CompileModes.Agentic] = new("CE-0029", "Gate 2 has no admission report") },
        Recount: null,
        ArtifactPath: "injected");

    [Fact]
    public void FileNewSession_WhenGateOneAdmits_OpensAgenticAdvisory_GatedOverTheSheetsAdapter()
    {
        var (mode, asked, adapter) = NewSessionThroughTheMenu(_ => GateOneAdmits());
        Assert.Equal([adapter], asked);
        Assert.Equal(CompileModes.AgenticAdvisory, mode);
    }

    [Fact]
    public void FileNewSession_WhenGateOneRefuses_OpensMechanicalOnly()
    {
        var proof = Directory.CreateDirectory(Path.Combine(_root, "no-proof")).FullName;
        var (mode, asked, adapter) = NewSessionThroughTheMenu(root => CompileModeGate.Evaluate(root, proof));
        Assert.Equal([adapter], asked);
        Assert.Equal(CompileModes.MechanicalOnly, mode);
    }

    /// <summary>
    /// The File menu built by <see cref="MainMenuBuilder"/> over a controller whose <c>session.new</c> runs the flow, as
    /// <c>MainWindow.NewSession</c> wires it; the <i>New Session</i> item clicked through its invoke peer. Returns the
    /// stored session's compile mode and the roots the gate was asked about.
    /// </summary>
    private (string Mode, List<string> Asked, string Adapter) NewSessionThroughTheMenu(Func<string, CompileModeAvailability> gate)
    {
        var adapter = InstalledAdapterRoot.Create(_root);
        var asked = new List<string>();
        var flow = new NewSessionFlow(
            activeWorkspaceRoot: () => _root,
            chooseWorkspace: null,
            openWorkspace: null,
            showSheet: sheet =>
            {
                sheet.CompileModeGateInput = root =>
                {
                    asked.Add(root);
                    return gate(root);
                };
                sheet.TaskClass = "implement";
                return true;
            },
            registry: () => new ProviderRegistry([new ProviderRow("anthropic", ProviderAuth.Subscription, [new ProviderAccount("max", AccountHealth.Ready)])]),
            workspaceId: root => root,
            adapterInstallRoot: () => adapter);

        NewSessionOutcome? outcome = null;
        Sta.Run(() =>
        {
            Task<string>? running = null;
            var controller = new WorkbenchController(new LayoutService(), new RecordingAnnouncer())
            {
                NewSessionRequested = () => running = Run(),
            };
            var menu = new Menu();
            MainMenuBuilder.Build(menu, controller, PerspectiveMenu.For(PerspectiveSet.Factory));
            var title = WorkbenchCommandCatalog.All.Single(c => c.Id == "session.new").Title;
            var item = menu.Items.OfType<MenuItem>().SelectMany(top => top.Items.OfType<MenuItem>()).Single(i => Equals(i.Header, title));
            ((IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(item).GetPattern(PatternInterface.Invoke)!).Invoke();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            Assert.True(running is not null, "clicking File → New Session did not run the flow");
            running!.GetAwaiter().GetResult();

            async Task<string> Run()
            {
                outcome = await flow.StartAsync();
                return outcome.Announcement;
            }
        });

        Assert.True(outcome?.Created is not null, $"no session was created: {outcome?.Announcement}");
        var stored = new SessionConfigStore(_root, outcome!.Created!.Config.SessionId).Load();
        output.WriteLine($"File → New Session: \"{outcome.Announcement}\" · gate asked about [{string.Join(", ", asked)}] · stored compile_mode {stored.CompileMode}");
        return (stored.CompileMode, asked, adapter);
    }
}
