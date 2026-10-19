using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using AiDe.App.Tests.Records;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Sessions;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>T30 n3 (B) — Ruling 182 (4): whatever was sent survives a restart.</b> An unsent draft is lost on restart until
/// WL-2 (Ruling 182 (3)), but a sent turn is not a draft. Its compiled prompt, goal block included, is on the turn row's
/// <i>compiled prompt</i> disclosure after a restart (Rulings 163, 181). The gesture: type, Ctrl+Enter (prepare under
/// advisory), Keep each suggested line through its invoke peer, Ctrl+Enter (send). Then the product's shell over a real
/// <see cref="AiDe.Core.Ipc.WorkspaceClient"/> to a daemon revives the session's pane, as T17d's tests reopen a notebook,
/// and the restored row's disclosure is opened through its header toggle. No model is called: the compile host is a stub.
/// </summary>
public sealed class ASentGoalLineSurvivesARestartTests(ITestOutputHelper output) : IDisposable
{
    private const string Words = "Rename the helper in the parser.";

    private const string SentGoal = "Suggestion 1 for goal.";

    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ASentTurnsGoalLine_IsOnTheRestoredRowsCompiledPromptDisclosure_AfterARestartThroughTheShellsRevive()
    {
        var accounts = new[] { new AccountRef("anthropic", "max") };
        new SessionConfigStore(_fixture.Root, NotebookFixture.SessionId).Create("Binding", _fixture.Root, accounts, accounts[0], DateTimeOffset.UnixEpoch);
        var sentBefore = "(not read)";
        ComposerGesture.Drive(_fixture, async (window, document) =>
        {
            var composer = document.Composer;
            AKeptSuggestedLineIsKeptTests.UnderAdvisory(composer);
            ComposerGesture.Type(composer, Words);
            ComposerGesture.Settle(document);
            ComposerGesture.CtrlEnter(composer);
            await composer.Preparing!.WaitAsync(NotebookFixture.Bound);
            ComposerGesture.Settle(document);
            foreach (var line in new[] { "Goal", "Done when", "Not in scope" })
            {
                AKeptSuggestedLineIsKeptTests.Invoke(AKeptSuggestedLineIsKeptTests.KeepButton(composer, line));
                ComposerGesture.Settle(document);
            }

            ComposerGesture.CtrlEnter(composer);
            await document.LastBinding.WaitAsync(NotebookFixture.Bound);
            await document.Notebook.Runs.LastRun.WaitAsync(NotebookFixture.Bound);
            ComposerGesture.Settle(document);
            var turn = Assert.Single(document.Notebook.Thread.Current.Turns);
            sentBefore = turn.SentBytes;
            output.WriteLine($"before restart: {turn.DisplayOrdinal} {turn.State}; sent {sentBefore.Length} chars; carries the goal line: {sentBefore.Contains(SentGoal, StringComparison.Ordinal)}");
            Assert.Contains(SentGoal, sentBefore, StringComparison.Ordinal);
        });

        var pipe = "aide-t30-b-" + Guid.NewGuid().ToString("N");
        var endpoint = new AiDe.Core.Ipc.DaemonEndpoint(pipe, new AiDe.Core.Ipc.CapabilityRegistry(), _ => _fixture.Store.CoreEpoch);
        AiDe.Core.Ipc.DaemonOperations.Register(endpoint, () => _fixture.Store.CoreEpoch);
        AiDe.Core.Ipc.WorkspaceOperations.RegisterNotebook(endpoint, new AiDe.Core.Ipc.NotebookService(_fixture.Store));
        AiDe.Core.Ipc.WorkspaceOperations.RegisterDispatch(endpoint, new AiDe.Core.Dispatch.BoundaryDispatcher(_fixture.Store));
        var server = new AiDe.Core.Ipc.IpcServer(pipe, endpoint, new AiDe.Core.Ipc.IpcServerOptions(StartupGrace: TimeSpan.FromSeconds(45)));
        using var life = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var running = server.RunAsync(life.Token);
        try
        {
            await using var client = await AiDe.Core.Ipc.WorkspaceClient.ConnectAsync(pipe, TimeSpan.FromSeconds(20), CancellationToken.None);
            var restored = (Rows: -1, Ordinal: "(none)", Bytes: "(not read)", Shown: "(not read)");
            Sta.Pump(
                create: () => new Border(),
                configure: window => { window.Width = 1200; window.Height = 800; },
                body: async (window, border) =>
                {
                    using var shell = new AiDe.App.Workbench.WorkbenchShell(client);
                    border.Child = shell.WorkbenchRoot;
                    var surfaceId = SessionDocumentSurface.SurfaceIdFor(NotebookFixture.SessionId);
                    shell.Service.Apply(new AiDe.Core.Workbench.LayoutOperation.AddSurface(shell.Service.Current.AllStacks().First().Id, new AiDe.Core.Workbench.Surface(surfaceId, SessionDocumentSurface.Kind, "Binding")));
                    shell.Adapter.Render();
                    Assert.Single(shell.ReviveRestoredSessionDocuments(_fixture.Root));
                    shell.Adapter.Render();
                    var revived = shell.Adapter.SurfaceContent<SessionDocumentSurface>(surfaceId)!;
                    await revived.LastReadBack.WaitAsync(NotebookFixture.Bound);
                    ComposerGesture.Settle(revived);
                    var feed = revived.Thread;
                    restored = (feed.Rows.Count, "(none)", "(not read)", "(not read)");
                    if (feed.Rows.Count == 1)
                    {
                        var row = feed.Rows[0];
                        var container = ThreadFixtures.Container(feed, 0);
                        var disclosure = ThreadFixtures.Visuals<Expander>(container).First(e => AutomationProperties.GetName(e) == row.CompiledName);
                        ((IToggleProvider)UIElementAutomationPeer.CreatePeerForElement(ThreadFixtures.HeaderToggle(disclosure)).GetPattern(PatternInterface.Toggle)!).Toggle();
                        ComposerGesture.Settle(revived);
                        var scroller = ThreadFixtures.Visuals<ScrollViewer>(container).First(s => AutomationProperties.GetName(s) == row.CompiledName);
                        restored = (1, row.CompiledName, row.SentBytes, string.Concat(ThreadFixtures.Visuals<TextBlock>(scroller).Select(t => t.Text)));
                    }

                    border.Child = null;
                });

            output.WriteLine($"after restart: {restored.Rows} restored row(s); \"{restored.Ordinal}\"; row SentBytes {restored.Bytes.Length} chars; disclosure shows {restored.Shown.Length} chars; equal to what was sent: {restored.Shown == sentBefore}");
            output.WriteLine($"disclosure text: \"{restored.Shown}\"");
            Assert.Equal(1, restored.Rows);
            Assert.Contains(SentGoal, restored.Shown, StringComparison.Ordinal);
            Assert.Equal(sentBefore, restored.Shown);
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

    /// <summary>The restored row's <i>not recorded</i> line is DESIGN.md's copy, word for word.</summary>
    [Fact]
    public void TheNotRecordedLine_IsTheCopyDesignMdNames()
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var row = Assert.Single(System.IO.File.ReadAllLines(System.IO.Path.Combine(dir!.FullName, "DESIGN.md")), l => l.Contains("a restored turn", StringComparison.Ordinal) && l.Contains("Ruling 182 (4)", StringComparison.Ordinal));
        Assert.Contains(AiDe.App.Workbench.Records.NotebookRuns.CompiledPromptNotRecorded, row, StringComparison.Ordinal);
    }
}
