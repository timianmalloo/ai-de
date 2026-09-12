using System.Text.Json;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Workbench;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// R13 b1 and b3: <c>File → New Session</c> is reachable, Recent sessions is there and restores its
/// workspace, and <c>File → New Terminal Session</c> still produces today's terminal session.
/// </summary>
public sealed class TheFrontDoorIsInTheFileMenuTests : IDisposable
{
    private readonly string _state = Path.Combine(
        Path.GetTempPath(), "aide-f2-menu-" + Guid.NewGuid().ToString("N"));

    public TheFrontDoorIsInTheFileMenuTests() => Directory.CreateDirectory(_state);

    public void Dispose()
    {
        try { Directory.Delete(_state, recursive: true); } catch (IOException) { }
    }

    private static IEnumerable<MenuItem> Items(Menu menu) =>
        menu.Items.OfType<MenuItem>().SelectMany(top => top.Items.OfType<MenuItem>());

    private static MenuItem FileMenu(Menu menu) =>
        menu.Items.OfType<MenuItem>().First(m => Equals(m.Header, "_File"));

    [Fact]
    public void NewSessionIsInTheCatalogAndInTheFileMenu() => Sta.Run(() =>
    {
        var command = WorkbenchCommandCatalog.All.Single(c => c.Id == "session.new");
        Assert.Equal("Ctrl+N", command.Gesture);
        Assert.Equal("_File", command.Menu);

        // No other command claims the gesture — a chord two commands answer to is one the user
        // cannot predict.
        Assert.Single(WorkbenchCommandCatalog.All, c => c.Gesture == "Ctrl+N");

        var menu = new Menu();
        MainMenuBuilder.Build(menu, new WorkbenchController(new LayoutService(), new RecordingAnnouncer()));

        Assert.Contains(Items(menu), i => Equals(i.Header, command.Title));
    });

    [Fact]
    public void CtrlNIsReallyBoundToIt() => Sta.Run(() =>
    {
        // The catalog's gesture STRING is what the menu shows; this is the binding a key press
        // actually reaches. A command whose chord is only printed is not keyboard-reachable, and
        // the two live in different places — which is exactly how they drift.
        var host = new System.Windows.Controls.Grid();
        var ran = new List<string>();

        var controller = new WorkbenchController(new LayoutService(), new RecordingAnnouncer())
        {
            NewSessionRequested = () => { ran.Add("session.new"); return Task.FromResult("Session created."); },
        };

        controller.Bind(host);

        var binding = host.InputBindings.OfType<System.Windows.Input.KeyBinding>().SingleOrDefault(
            b => b.Key == System.Windows.Input.Key.N
                && b.Modifiers == System.Windows.Input.ModifierKeys.Control);

        Assert.NotNull(binding);

        var routed = Assert.IsType<System.Windows.Input.RoutedUICommand>(binding.Command);
        Assert.Equal("session.new", routed.Name);

        // And the command the binding names really reaches the flow, rather than resolving to an id
        // nothing handles — the two halves of "the chord works".
        Assert.Contains(
            host.CommandBindings.OfType<System.Windows.Input.CommandBinding>(),
            b => ReferenceEquals(b.Command, routed));

        controller.Execute(routed.Name);
        Assert.Equal(["session.new"], ran);
    });

    [Fact]
    public void InvokingItRunsTheFlow_AndSaysSoWhenNoFlowIsWired() => Sta.Run(() =>
    {
        var announcer = new RecordingAnnouncer();
        var controller = new WorkbenchController(new LayoutService(), announcer);

        // Unwired: the command is still handled and still announces. Silence is indistinguishable
        // from a dead key (DC-011).
        Assert.True(controller.Execute("session.new"));
        Assert.Contains("not available", announcer.Last, StringComparison.OrdinalIgnoreCase);

        controller.NewSessionRequested = () => Task.FromResult("Session “x” created.");
        Assert.True(controller.Execute("session.new"));
        Assert.Contains("created", announcer.Last, StringComparison.OrdinalIgnoreCase);
    });

    [Fact]
    public void FileNewTerminalSessionStillProducesTodaysTerminalSession() => Sta.Run(() =>
    {
        // R13 b4, asserted rather than assumed. The catalog row, the menu entry and the factory's
        // terminal kind are all unchanged, and the factory still builds a real TerminalSurface for
        // the "terminal" kind.
        var terminal = WorkbenchCommandCatalog.All.Single(c => c.Id == "terminal.new");
        Assert.Equal("_Terminal", terminal.Menu);
        Assert.Equal("Ctrl+K, T", terminal.Gesture);

        var menu = new Menu();
        MainMenuBuilder.Build(menu, new WorkbenchController(new LayoutService(), new RecordingAnnouncer()));
        Assert.Contains(Items(menu), i => Equals(i.Header, terminal.Title));

        // Every launchable harness still has its own command, derived from the profiles.
        foreach (var profile in AiDe.Core.Terminal.AgentReadinessProfiles.BuiltIn.All.Where(p => p.Launchable))
        {
            Assert.Contains(WorkbenchCommandCatalog.All, c => c.Id == profile.CommandId);
            Assert.Contains(Items(menu), i => Equals(i.Header, $"New {profile.DisplayName} session"));
        }

        // And the pane it opens is still built by the factory's terminal row.
        var row = SurfaceContentFactory.Kinds.Single(k => k.Kind == "terminal");
        Assert.True(row.Windowed, "the terminal kind must stay unwrapped — a Border cannot clip a child HWND");

        using var content = new SurfaceContentFactory(queries: null)
            .Create(new Surface("terminal-1", "terminal", "Terminal — pwsh")) as IDisposable;

        Assert.IsType<TerminalSurface>(content);
    });

    [Fact]
    public void ACreatedSessionAppearsInRecentSessions_AndItsEntryRestoresItsWorkspace()
    {
        var workspace = Path.Combine(_state, "workspace");
        Directory.CreateDirectory(workspace);

        var sheet = new NewSessionSheetViewModel(
            workspace, workspace, new AiDe.Core.AgentPlane.ProviderRegistry([]), DateTimeOffset.UtcNow)
        {
            TaskClass = "feature",
        };

        var created = sheet.Create(DateTimeOffset.UtcNow);

        RecentSessions.Remember(_state, new RecentSessionEntry(
            created.Config.SessionId, created.Config.Name, workspace));

        var recent = RecentSessions.All(_state);

        // It appears...
        var entry = Assert.Single(recent);
        Assert.Equal(created.Config.SessionId, entry.SessionId);
        Assert.Equal(created.Config.Name, entry.Name);

        // ...and the entry restores its workspace, which is what reopening needs first.
        Assert.Equal(workspace, entry.WorkspaceRoot);
        Assert.True(File.Exists(SessionPaths.SessionFile(entry.WorkspaceRoot, entry.SessionId)));
    }

    [Fact]
    public void RecentSessionsIsNewestFirst_Deduplicated_AndCapped()
    {
        var workspace = Path.Combine(_state, "workspace");
        Directory.CreateDirectory(workspace);

        for (var i = 0; i < RecentSessions.Cap + 3; i++)
        {
            RecentSessions.Remember(_state, new RecentSessionEntry($"s{i}", $"session {i}", workspace));
        }

        RecentSessions.Remember(_state, new RecentSessionEntry("s0", "session 0", workspace));

        var recent = RecentSessions.All(_state);

        Assert.Equal(RecentSessions.Cap, recent.Count);
        Assert.Equal("s0", recent[0].SessionId);
        Assert.Equal(recent.Count, recent.Select(e => e.SessionId).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ARecentSessionWhoseWorkspaceIsGoneIsDropped()
    {
        // A menu offering something that cannot work teaches the user to distrust the menu — the
        // same discipline the recent-workspaces list already applies.
        var workspace = Path.Combine(_state, "vanished");
        Directory.CreateDirectory(workspace);

        RecentSessions.Remember(_state, new RecentSessionEntry("s1", "session", workspace));
        Assert.Single(RecentSessions.All(_state));

        Directory.Delete(workspace);
        Assert.Empty(RecentSessions.All(_state));
    }

    [Fact]
    public void AnUnreadableRecentListDegradesToEmptyRatherThanThrowing()
    {
        File.WriteAllText(Path.Combine(_state, RecentSessions.FileName), "{ not the list");
        Assert.Empty(RecentSessions.All(_state));

        // And the file really was unreadable, or the assertion above proves nothing.
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<List<RecentSessionEntry>>("{ not the list"));
    }

    [Fact]
    public void TheFileMenuOffersRecentSessions_AndClickingOneReopensIt() => Sta.Run(() =>
    {
        var opened = new List<string>();
        var menu = new Menu();

        MainMenuBuilder.Build(
            menu,
            new WorkbenchController(new LayoutService(), new RecordingAnnouncer()),
            recentSessions: [new RecentSessionEntry("s1", "payments extraction", @"C:\repo")],
            onOpenRecentSession: opened.Add);

        var submenu = FileMenu(menu).Items.OfType<MenuItem>()
            .Single(m => Equals(m.Header, "Recent _sessions"));

        var item = submenu.Items.OfType<MenuItem>().Single();
        Assert.Equal("payments extraction", item.Header);
        Assert.Equal(@"C:\repo", item.ToolTip);

        item.RaiseEvent(new System.Windows.RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(["s1"], opened);
    });
}
