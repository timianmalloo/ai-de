using System.Windows;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// CV-1/CV-2's seam request: <c>WorkbenchShell.RegisterSessionDocument</c> passes its own
/// <see cref="IWorkbenchAnnouncer"/> as the document's third constructor argument (ADR-0031, one
/// announcer across hosts). Before this landed the shell built the document with no announcer, so
/// the document fell back to its own private live region and every one of its outcomes — Console
/// open/closed, a refused region — was spoken into a strip the shell's own <c>Announcer.Last</c>
/// never saw.
/// </summary>
public sealed class TheShellsAnnouncerReachesTheSessionDocumentTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "aide-announcer-seam-" + Guid.NewGuid().ToString("N"));

    public TheShellsAnnouncerReachesTheSessionDocumentTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    private SessionConfig Create(string name) =>
        new SessionConfigStore(_root, SessionId.New(DateTimeOffset.UtcNow))
            .Create(name, _root, [], null, DateTimeOffset.UtcNow);

    /// <summary>
    /// Opens a real session document through the real shell, opens its Console split (a document
    /// outcome that announces, <c>SessionDocumentSurface.OpenSplit</c>), and reads back what the
    /// SHELL's announcer last said — the seam is proven only through the real composition root
    /// (E11), not by constructing the document directly with the shell's announcer handed in.
    /// </summary>
    [Fact]
    public void OpeningTheConsoleSplit_AnnouncesThroughTheShellsAnnouncer_NotAPrivateLiveRegion()
    {
        var config = Create("Announcer seam");

        var last = Sta.Run(() =>
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

                var document = shell.Adapter.SurfaceContent<SessionDocumentSurface>(
                    SessionDocumentSurface.SurfaceIdFor(config.SessionId));
                Assert.NotNull(document);

                document!.OpenSplit();
                return shell.Announcer.Last;
            }
            finally
            {
                window.Close();
            }
        }, 60);

        Assert.Contains("Console open", last, StringComparison.Ordinal);
    }
}
