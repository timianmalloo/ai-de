using System.Windows;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// <b>INV-0009 Phase 2b, the shell's half.</b> A <c>session-document</c> surface a saved arrangement
/// restored — an island, no live document — is revived where its <c>session.json</c> loads, and
/// keeps its island where the file is missing, malformed, or reads as <c>null</c>. A file the store
/// cannot read is "gone" for this purpose, never a launch that throws.
/// </summary>
/// <remarks>
/// <b>Red first:</b> <see cref="Revive_WithASessionFileThatReadsAsNull_KeepsTheIslandRatherThanThrowing"/>
/// threw <c>InvalidOperationException</c> out of <c>ReviveRestoredSessionDocuments</c> — the
/// store's own "deserialized to null" — which <c>MainWindow.AttachWorkspace</c> would have raised
/// at launch (the Test Architect's finding 3).
/// </remarks>
public sealed class RestoredSessionDocumentsAreRevivedTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "aide-revive-" + Guid.NewGuid().ToString("N"));

    public RestoredSessionDocumentsAreRevivedTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    private const string Island = "No session is open. Create one from File → New Session.";

    /// <summary>A shown shell whose layout carries a restored session-document surface for <paramref name="sessionId"/> and nothing registered for it.</summary>
    private static T WithRestoredSurface<T>(string sessionId, Func<WorkbenchShell, string, T> body) => Sta.Run(() =>
    {
        var shell = new WorkbenchShell(queries: null);
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
            var surfaceId = SessionDocumentSurface.SurfaceIdFor(sessionId);
            var stack = shell.Service.Current.AllStacks().First();
            shell.Service.Apply(new LayoutOperation.AddSurface(stack.Id, new Surface(surfaceId, SessionDocumentSurface.Kind, "Restored")));
            shell.Adapter.Render();
            return body(shell, surfaceId);
        }
        finally
        {
            window.Close();
        }
    }, 60);

    private static string Rendered(WorkbenchShell shell, string surfaceId) =>
        shell.Adapter.ContentFor(surfaceId) is System.Windows.Controls.Border { Child: System.Windows.Controls.TextBlock text }
            ? text.Text
            : shell.Adapter.SurfaceContent<SessionDocumentSurface>(surfaceId) is not null ? "(live document)" : "(other)";

    [Fact]
    public void Revive_WithASessionFileThatLoads_RegistersTheDocumentAndTheNextRenderShowsIt()
    {
        var sessionId = SessionId.New(DateTimeOffset.UtcNow);
        new SessionConfigStore(_root, sessionId).Create("Kept", _root, ["claude-code"], DateTimeOffset.UtcNow);

        var (before, revived, after) = WithRestoredSurface(sessionId, (shell, surfaceId) =>
        {
            var island = Rendered(shell, surfaceId);
            var configs = shell.ReviveRestoredSessionDocuments(_root);
            shell.Adapter.Render();
            return (island, configs, Rendered(shell, surfaceId));
        });

        Assert.Equal(Island, before);
        Assert.Equal([sessionId], revived.Select(c => c.SessionId));
        Assert.Equal("(live document)", after);
    }

    [Fact]
    public void Revive_WithNoSessionFile_KeepsTheIsland()
    {
        var sessionId = SessionId.New(DateTimeOffset.UtcNow);

        var (revived, after) = WithRestoredSurface(sessionId, (shell, surfaceId) =>
        {
            var configs = shell.ReviveRestoredSessionDocuments(_root);
            shell.Adapter.Render();
            return (configs, Rendered(shell, surfaceId));
        });

        Assert.Empty(revived);
        Assert.Equal(Island, after);
    }

    [Fact]
    public void Revive_WithAMalformedSessionFile_KeepsTheIsland()
    {
        var sessionId = SessionId.New(DateTimeOffset.UtcNow);
        Directory.CreateDirectory(SessionPaths.SessionDirectory(_root, sessionId));
        File.WriteAllText(SessionPaths.SessionFile(_root, sessionId), "{");

        var (revived, after) = WithRestoredSurface(sessionId, (shell, surfaceId) =>
        {
            var configs = shell.ReviveRestoredSessionDocuments(_root);
            shell.Adapter.Render();
            return (configs, Rendered(shell, surfaceId));
        });

        Assert.Empty(revived);
        Assert.Equal(Island, after);
    }

    [Fact]
    public void Revive_WithASessionFileThatReadsAsNull_KeepsTheIslandRatherThanThrowing()
    {
        var sessionId = SessionId.New(DateTimeOffset.UtcNow);
        Directory.CreateDirectory(SessionPaths.SessionDirectory(_root, sessionId));
        File.WriteAllText(SessionPaths.SessionFile(_root, sessionId), "null");

        var (revived, after) = WithRestoredSurface(sessionId, (shell, surfaceId) =>
        {
            var configs = shell.ReviveRestoredSessionDocuments(_root);
            shell.Adapter.Render();
            return (configs, Rendered(shell, surfaceId));
        });

        Assert.Empty(revived);
        Assert.Equal(Island, after);
    }
}
