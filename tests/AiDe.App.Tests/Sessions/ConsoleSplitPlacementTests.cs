using System.Windows;
using System.Windows.Controls;
using AiDe.App.Tests.Shell;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// Ruling 89 (SH-4.2, C5 — CV-5.2's seam request <c>req-01M2E1HEMCMHRW1AX4SY8RJFQ7</c>): with the
/// session docked at Left, the Console split is a <b>document</b> — kind <c>console</c>, identity
/// <c>console:&lt;sessionId&gt;</c>, caption <i>Console — &lt;session&gt;</i> — opened and focused in the
/// <b>Center</b> zone, never inside the Left stack; one per session (a second toggle focuses it); a
/// sibling tab when the session is itself in the Center; it closes when its session document closes;
/// the Center's empty copy yields to it while it is open. Its content is CV-5.2's
/// <see cref="ConsoleSurface"/> — <see cref="SessionDocumentSurface.Split"/>, the one instance.
/// </summary>
/// <remarks>
/// <b>Red before green:</b> no <c>console</c> row, no <c>session.console</c> verb, and the header's
/// toggle opened the split inside the document's own grid (the Left pane at 673 px — IA-6).
/// </remarks>
public sealed class ConsoleSplitPlacementTests : IDisposable
{
    private readonly ComposedCoding.Workspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    private static string ConsoleId(string sessionId) => "console:" + sessionId;

    [Fact]
    public void WithTheSessionAtLeft_TheSplitOpensInTheCenterZone_AsAConsoleDocument()
    {
        var config = _workspace.Session("payments extraction");

        Sta.Run(() =>
        {
            using var frame = ComposedCoding.Show(1440, 900);
            var shell = frame.Shell;
            shell.OpenSessionDocument(config);
            frame.Settle();
            var sessionId = SessionDocumentSurface.SurfaceIdFor(config.SessionId);
            var document = frame.Document(sessionId);
            Assert.Equal(ZoneId.Left, shell.Coding.Service.Zones.FindZoneOf(sessionId));

            // The header's toggle — the operator's gesture — dispatches the verb.
            document.ConsoleToggle.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            frame.Settle();

            var zones = shell.Coding.Service.Zones;
            var console = Assert.Single(zones.AllSurfaces(), s => s.Kind == "console");
            Assert.Equal(ConsoleId(config.SessionId), console.SurfaceId);
            Assert.Equal("Console — payments extraction", console.Title);
            Assert.Equal(ZoneId.Center, zones.FindZoneOf(console.SurfaceId));
            Assert.DoesNotContain(zones.Zone(ZoneId.Left).Surfaces(), s => s.Kind == "console");
            Assert.Equal(console.SurfaceId, ((ZoneStack)zones.Zone(ZoneId.Center).Content!).Active.SurfaceId);

            // Rendered in the Center pane, focused, holding the document's own split — not a second one.
            var content = shell.Coding.Adapter.ContentFor(console.SurfaceId);
            Assert.NotNull(content);
            var split = ComposedCoding.Visuals<ConsoleSurface>(content!).SingleOrDefault();
            Assert.True(split is not null, "the console document renders no ConsoleSurface");
            Assert.Same(document.Split, split);
            Assert.False(document.IsSplitOpen, "the split still opened inside the session document's own grid");
            Assert.Equal(console.SurfaceId, shell.Coding.Adapter.ActiveSurfaceId);
            Assert.True(document.ConsoleToggle.IsChecked == true);

            // The Center's empty copy yields to the tab while it is open.
            Assert.Null(frame.CenterEmpty());

            // One per session: a second toggle focuses the existing one, never adds a second.
            Assert.True(shell.Coding.Service.Apply(new LayoutOperation.ActivateSurface(sessionId)).Applied);
            shell.Coding.Adapter.Render();
            document.ConsoleToggle.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            frame.Settle();
            Assert.Single(shell.Coding.Service.Zones.AllSurfaces(), s => s.SurfaceId == ConsoleId(config.SessionId));
            Assert.Equal(1, shell.Coding.Service.Zones.AllSurfaces().Count(s => s.Kind == "console"));
            Assert.Equal(console.SurfaceId, shell.Coding.Adapter.ActiveSurfaceId);

            // It closes with its session.
            Assert.True(shell.Coding.Service.Apply(new LayoutOperation.CloseSurface(sessionId)).Applied);
            shell.Coding.Adapter.Render();
            frame.Settle();
            Assert.DoesNotContain(shell.Coding.Service.Zones.AllSurfaces(), s => s.Kind == "console");
            Assert.NotNull(frame.CenterEmpty());
            return 0;
        }, 60);
    }

    /// <summary>The session itself in the Center: the Console opens as a sibling tab in that stack.</summary>
    [Fact]
    public void WithTheSessionInTheCenter_TheConsoleIsASiblingTab()
    {
        var config = _workspace.Session("in the center");

        Sta.Run(() =>
        {
            using var frame = ComposedCoding.Show(1440, 900);
            var shell = frame.Shell;
            shell.OpenSessionDocument(config);
            var sessionId = SessionDocumentSurface.SurfaceIdFor(config.SessionId);
            Assert.True(shell.Coding.Service.Apply(new LayoutOperation.MoveSurface(sessionId, new DropTarget(ZonesToTree.CenterStackId, DropKind.JoinStack))).Applied);
            shell.Coding.Adapter.Render();
            frame.Settle();
            var document = frame.Document(sessionId);

            document.ConsoleToggle.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            frame.Settle();

            var center = (ZoneStack)shell.Coding.Service.Zones.Zone(ZoneId.Center).Content!;
            Assert.Equal([sessionId, ConsoleId(config.SessionId)], center.Tabs.Select(t => t.SurfaceId));
            Assert.Null(shell.Coding.Service.Zones.Zone(ZoneId.Left).Content);
            return 0;
        }, 60);
    }

    /// <summary>The verb from the keyboard/menu: <c>session.console</c> reaches the focused session's document; with none focused it says so.</summary>
    [Fact]
    public void TheVerb_OpensTheFocusedSessionsConsole_AndRefusesWithNoSessionFocused()
    {
        var config = _workspace.Session("by verb");

        Sta.Run(() =>
        {
            using var frame = ComposedCoding.Show(1440, 900);
            var shell = frame.Shell;

            Assert.True(shell.Execute("session.console"));
            Assert.Equal("No session document is focused.", shell.LiveRegion.Text);
            Assert.DoesNotContain(shell.Coding.Service.Zones.AllSurfaces(), s => s.Kind == "console");

            shell.OpenSessionDocument(config);
            frame.Settle();
            var sessionId = SessionDocumentSurface.SurfaceIdFor(config.SessionId);
            // Keyboard focus inside the document is what the controller's focused surface follows.
            var document = frame.Document(sessionId);
            System.Windows.Input.Keyboard.Focus(document.ConsoleToggle);
            frame.Settle();
            Assert.Equal(sessionId, shell.Coding.Controller.FocusedSurfaceId);
            Assert.True(shell.Execute("session.console"));
            frame.Settle();
            Assert.Equal(ZoneId.Center, shell.Coding.Service.Zones.FindZoneOf(ConsoleId(config.SessionId)));
            Assert.StartsWith("Console open", shell.LiveRegion.Text, StringComparison.Ordinal);
            return 0;
        }, 60);
    }

    /// <summary>The kind's row and its verb, as the seam request spelled them (data; no window).</summary>
    [Fact]
    public void TheConsoleKindsRow_IsCodingsManyInstancesVerbRow_HomedInTheCenter()
    {
        var row = Assert.Single(SurfaceContentFactory.Kinds, k => k.Kind == "console");
        Assert.Equal("Console", row.Title);
        Assert.Equal("One session's Console: every wire frame, one row per message.", row.Summary);
        Assert.Equal(["coding"], row.Perspectives.Select(p => p.Id));
        Assert.Equal(SurfaceContentFactory.Instances.Many, row.Instances);
        Assert.Equal("session.console", Assert.IsType<SurfaceContentFactory.SurfaceEntry.Verb>(row.Entry).CommandId);
        Assert.Equal(ZoneId.Center, row.Zone);

        var verb = Assert.Single(WorkbenchCommandCatalog.All, c => c.Id == "session.console");
        Assert.Equal("_View", verb.Menu);
        Assert.Equal(CommandScope.Admits(SessionDocumentSurface.Kind), verb.Scope);
        Assert.True(DockHost.AdmissionFor(PerspectiveSet.Coding).Admits("console"));
        Assert.False(DockHost.AdmissionFor(PerspectiveSet.Architecture).Admits("console"));
        Assert.False(DockHost.AdmissionFor(PerspectiveSet.Coordination).Admits("console"));
    }
}
