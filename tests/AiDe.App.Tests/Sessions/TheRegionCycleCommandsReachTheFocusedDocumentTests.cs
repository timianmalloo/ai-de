using System.Windows;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// DC-068's registry leg for DS-1 P4/SC8: the catalog rows <c>session.cycleRegion</c> (F6) and
/// <c>session.cycleRegionBack</c> (Shift+F6) reach the FOCUSED session document's own
/// <see cref="SessionDocumentSurface.CycleRegion"/> through <c>WorkbenchController.Execute</c> —
/// the palette/menu leg of the cycle, alongside the document's own <c>PreviewKeyDown</c> handler
/// (unchanged, the fallback per DS-1 P4).
/// </summary>
public sealed class TheRegionCycleCommandsReachTheFocusedDocumentTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "aide-region-cycle-" + Guid.NewGuid().ToString("N"));

    public TheRegionCycleCommandsReachTheFocusedDocumentTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    private SessionConfig Create(string name) =>
        new SessionConfigStore(_root, SessionId.New(DateTimeOffset.UtcNow))
            .Create(name, _root, [], DateTimeOffset.UtcNow);

    [Fact]
    public void ExecutingSessionCycleRegion_MovesTheFocusedDocumentForwardOneRegion()
    {
        var config = Create("Registry cycle");

        var (before, after) = Sta.Run(() =>
        {
            using var shell = new WorkbenchShell(queries: null);
            var window = new Window
            {
                Content = shell.Manager,
                Width = 1000,
                Height = 640,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                ShowActivated = false,
            };

            window.Show();
            window.UpdateLayout();

            try
            {
                shell.OpenSessionDocument(config);
                shell.Adapter.Render();
                window.UpdateLayout();

                var document = shell.Adapter.SurfaceContent<SessionDocumentSurface>(
                    SessionDocumentSurface.SurfaceIdFor(config.SessionId));
                Assert.NotNull(document);

                document!.FocusRegion(SessionDocumentSurface.Region.Header);
                shell.Coding.Controller.FocusedSurfaceId = document.SurfaceId;

                var beforeRegion = document.CurrentRegion;
                Assert.True(shell.Execute("session.cycleRegion"));

                return (beforeRegion, document.CurrentRegion);
            }
            finally
            {
                window.Close();
            }
        }, 60);

        Assert.Equal(SessionDocumentSurface.Region.Header, before);
        Assert.Equal(SessionDocumentSurface.Region.Thread, after);
    }

    [Fact]
    public void ExecutingSessionCycleRegionBack_MovesTheFocusedDocumentBackwardOneRegion()
    {
        var config = Create("Registry cycle back");

        var (before, after) = Sta.Run(() =>
        {
            using var shell = new WorkbenchShell(queries: null);
            var window = new Window
            {
                Content = shell.Manager,
                Width = 1000,
                Height = 640,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                ShowActivated = false,
            };

            window.Show();
            window.UpdateLayout();

            try
            {
                shell.OpenSessionDocument(config);
                shell.Adapter.Render();
                window.UpdateLayout();

                var document = shell.Adapter.SurfaceContent<SessionDocumentSurface>(
                    SessionDocumentSurface.SurfaceIdFor(config.SessionId));
                Assert.NotNull(document);

                document!.FocusRegion(SessionDocumentSurface.Region.Header);
                shell.Coding.Controller.FocusedSurfaceId = document.SurfaceId;

                var beforeRegion = document.CurrentRegion;
                Assert.True(shell.Execute("session.cycleRegionBack"));

                return (beforeRegion, document.CurrentRegion);
            }
            finally
            {
                window.Close();
            }
        }, 60);

        // Header cycling backward wraps to the last region (the composer, with the split closed).
        Assert.Equal(SessionDocumentSurface.Region.Header, before);
        Assert.Equal(SessionDocumentSurface.Region.Composer, after);
    }

    /// <summary>No session document focused: the command reports why rather than doing nothing (DC-011).</summary>
    [Fact]
    public void ExecutingSessionCycleRegion_WithNoSessionFocused_IsHandledNotUnknown()
    {
        var handled = Sta.Run(() =>
        {
            using var shell = new WorkbenchShell(queries: null);
            return shell.Execute("session.cycleRegion");
        }, 60);

        Assert.True(handled);
    }

    /// <summary>
    /// The same case, but through the real wiring (<c>WorkbenchShell.WireSessionRegionCycle</c>),
    /// asserting the refusal reaches the shell's own announcer rather than only that the command
    /// returned true. Red before the fix: the wiring's synthetic "no session document" refusal was
    /// constructed and handed back to <c>WorkbenchController.CycleSessionRegion</c>, which discards
    /// the result on the theory that <see cref="SessionDocumentSurface"/> always speaks its own
    /// refusal — true for a refusal <see cref="SessionDocumentSurface.CycleRegion"/> produces, false
    /// for this one, which no code ever announced (a silent refusal, DC-011/SC 4.1.3 — found in
    /// review).
    /// </summary>
    [Fact]
    public void ExecutingSessionCycleRegion_WithNoSessionFocused_AnnouncesWhy()
    {
        var last = Sta.Run(() =>
        {
            using var shell = new WorkbenchShell(queries: null);
            Assert.True(shell.Execute("session.cycleRegion"));
            return shell.Announcer.Last;
        }, 60);

        Assert.Equal("No session document is focused.", last);
    }
}
