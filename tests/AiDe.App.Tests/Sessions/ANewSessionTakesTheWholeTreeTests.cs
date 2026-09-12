using System.Windows;
using System.Xml.Linq;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// Ruling 47: creating a session maximizes its document's pane; reopening one does not.
/// </summary>
/// <remarks>
/// <para><b>This file is the ruling's condition.</b> Maximize-on-create was ratified <i>on the
/// condition that it gets its own red-first oracle before merge</i>, because when it was written
/// nothing in <c>tests/</c> exercised either half — the only maximize coverage was the command path
/// (<c>workbench.maximizePane</c>), which proves the operation works and says nothing about when the
/// product invokes it. A product behaviour with no oracle was the one thing this slice was otherwise
/// strict about.</para>
///
/// <para><b>Observed red before green, twice, and the second time is the one that counts.</b>
/// <c>NewSessionPlacement.GiveItTheWholeTree</c> was written first as a stub returning its
/// announcement unchanged. The first oracle failed against it with <i>"expected Maximized, got
/// Docked"</i> — and then failed against the real body too, which is how the projection gap below
/// was found. The assertion that shipped was re-run against the stub rather than assumed to
/// inherit that red, and failed with <i>"nothing was minimized: the projection showed 4 stack(s)
/// before and 4 after, so the pane did not take the tree."</i> A test that has never been red is a
/// test whose discriminating power is unmeasured, and a test whose <i>predecessor</i> was red is
/// not the same test.</para>
///
/// <para><b>Three assertions, because the behaviour has three halves.</b> The effect, the absence of
/// it on the reopen path, and the wiring that decides which path a session takes. The third is read
/// from <c>MainWindow.xaml.cs</c> rather than from a constructed window, for the same reason
/// <c>RailButtonsDoNotLieTests</c> reads markup: building <c>MainWindow</c> starts the shell. The
/// cost is that it sees the call and not the click; the benefit is that it sees a future edit that
/// moves the call to the wrong method.</para>
///
/// <para><b>What the first red revealed, and why these assertions are not about
/// <see cref="StackState"/>.</b> The obvious oracle — <i>the session's stack reads
/// <c>StackState.Maximized</c></i> — is <b>unsatisfiable in this product</b>, and it stayed red
/// after the real body was written. The shell runs <c>ZoneBackedLayoutService</c>
/// (<c>WorkbenchShell.cs:113</c>), whose tree projection <c>ZonesToTree</c> builds <i>every</i>
/// stack at the default <c>StackState.Docked</c> and always hands <c>Layout</c> an <b>empty</b>
/// maximize memo (<c>ZonesToTree.cs:67</c>). <c>StackState</c> does not round-trip through the
/// projection at all. The pre-existing maximize coverage the ratification cites
/// (<c>WorkbenchLayoutTests</c>, <c>WorkbenchControllerTests</c>) constructs <c>new
/// LayoutService()</c> — the <i>tree</i> service, which the shell does not use — so it proves the
/// operation and says nothing about the service the product runs on.</para>
///
/// <para><b>So the oracle observes the EFFECT, which is what the operator sees and what DESIGN.md
/// actually promises:</b> <i>"fills the tree; siblings are temporarily minimized"</i>.
/// <c>ZoneLayoutService.Maximize</c> collapses the sibling zones, and <c>ZonesToTree</c> omits a
/// collapsed zone from the projection — so after a maximize the siblings are <b>gone from
/// <c>Current</c></b> and the session's own stack is still there. That is a projection-level fact
/// about the running service, not an enum nobody sets.</para>
/// </remarks>
public sealed class ANewSessionTakesTheWholeTreeTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "aide-maximize-" + Guid.NewGuid().ToString("N"));

    public ANewSessionTakesTheWholeTreeTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    private SessionConfig Create(string name) =>
        new SessionConfigStore(_root, SessionId.New(DateTimeOffset.UtcNow))
            .Create(name, _root, [], DateTimeOffset.UtcNow);

    /// <summary>Runs a body against a real shell in a real shown window.</summary>
    private static T WithShell<T>(Func<WorkbenchShell, T> assert) => Sta.Run(() =>
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

        try { return assert(shell); }
        finally { window.Close(); }
    }, 60);

    /// <summary>Every stack id the projection currently shows. A collapsed zone is simply absent.</summary>
    private static List<string> VisibleStacks(WorkbenchShell shell) =>
        [.. shell.Service.Current.AllStacks().Select(s => s.Id).OrderBy(s => s, StringComparer.Ordinal)];

    private static bool Shows(WorkbenchShell shell, string sessionId) =>
        shell.Service.Current.FindStackOf(SessionDocumentSurface.SurfaceIdFor(sessionId)) is not null;

    [Fact]
    public void ANewSessionsPaneFillsTheTree()
    {
        var config = Create("Front door");

        var (before, after, shows, said) = WithShell(shell =>
        {
            shell.OpenSessionDocument(config);
            var opened = VisibleStacks(shell);

            var announcement = NewSessionPlacement.GiveItTheWholeTree(
                shell.Service, config.SessionId, "Session opened.");

            return (opened, VisibleStacks(shell), Shows(shell, config.SessionId), announcement);
        });

        // The document reached the layout, and it is still there afterwards — without this the
        // "siblings went away" assertion could pass by taking the session's pane with them (DC-016).
        Assert.True(before.Count > 1,
            "the default arrangement showed one stack or none, so there were no siblings to minimize "
            + "and this case measures nothing");
        Assert.True(shows, "maximizing removed the session's own pane from the projection");

        // The effect DESIGN.md promises: siblings are temporarily minimized, so they leave the
        // projection. This is the assertion that was red against the stub.
        Assert.True(after.Count < before.Count,
            $"nothing was minimized: the projection showed {before.Count} stack(s) before and "
            + $"{after.Count} after, so the pane did not take the tree.");

        // Announced, never silent — a maximize that said nothing is indistinguishable from a dead
        // command (DC-011).
        Assert.StartsWith("Session opened.", said, StringComparison.Ordinal);
        Assert.Contains("Maximized", said, StringComparison.Ordinal);
    }

    [Fact]
    public void OpeningADocumentOnItsOwnChangesNothingAboutTheArrangement()
    {
        var config = Create("Reopened");

        var (before, after, shows) = WithShell(shell =>
        {
            var opened = VisibleStacks(shell);

            // The reopen path in MainWindow is exactly this call and nothing else.
            shell.OpenSessionDocument(config);

            return (opened, VisibleStacks(shell), Shows(shell, config.SessionId));
        });

        Assert.True(shows, "reopening did not put the session's document in the layout at all");

        // Every stack that was showing is still showing. Reopening adds a document; it does not
        // rearrange the workbench behind an operator who asked for a tab.
        Assert.Empty(before.Except(after, StringComparer.Ordinal));
    }

    [Fact]
    public void TheMaximizeIsOnTheCreatePathAndNotOnTheReopenPath()
    {
        var source = File.ReadAllText(MainWindowSource());

        static string Method(string source, string signature)
        {
            var at = source.IndexOf(signature, StringComparison.Ordinal);
            Assert.True(at >= 0, $"MainWindow.xaml.cs has no {signature} — this test is reading a "
                + "document whose shape it does not understand (DC-016).");

            // To the start of the next member declaration at method indentation.
            var next = source.IndexOf("\n    private ", at + signature.Length, StringComparison.Ordinal);
            return next < 0 ? source[at..] : source[at..next];
        }

        const string Call = "GiveTheNewSessionTheWholeTree";

        // New Session became a task command when the chooser began opening the chosen workspace
        // (INV-0009 Phase 3); the maximize is still on it and on nothing that shows an existing
        // session — a reopen, or the documents a saved arrangement restores at workspace-open.
        Assert.Contains(Call, Method(source, "private async Task<string> NewSessionAsync()"), StringComparison.Ordinal);
        Assert.DoesNotContain(Call, Method(source, "private void ReopenSession("), StringComparison.Ordinal);
        Assert.DoesNotContain(Call, Method(source, "private async Task ReopenSessionAsync("), StringComparison.Ordinal);
        Assert.DoesNotContain(Call, Method(source, "private void AttachWorkspace("), StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>INV-0009 Phase 4.</b> The maximize is in the log: one <c>layout.mutation</c> line with
    /// <c>operation: maximize-stack</c>, the stack it maximized, and the projected tree after — and
    /// the same line, <c>placement: refused</c>, when the service refuses it. The operator's log
    /// showed the maximize only by its consequence, three lines later, as a reconcile reading three
    /// collapsed zones (INV-0009 §3, line 21).
    /// </summary>
    [Fact]
    public void TheMaximizeWritesItsOwnMutationLine()
    {
        var config = Create("Logged");
        var lines = new List<string>();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = lines.Add;

        try
        {
            WithShell(shell =>
            {
                shell.OpenSessionDocument(config);
                NewSessionPlacement.GiveItTheWholeTree(shell.Service, config.SessionId, "Session opened.");

                // A second maximize while one stands is refused by the service, and that is logged too.
                return NewSessionPlacement.GiveItTheWholeTree(shell.Service, config.SessionId, "Again.");
            });
        }
        finally
        {
            WorkbenchDiagnostics.Sink = previous;
        }

        var maximizes = lines
            .Select(l => System.Text.Json.JsonDocument.Parse(l).RootElement)
            .Where(e => e.GetProperty("evt").GetString() == "layout.mutation"
                && e.GetProperty("operation").GetString() == "maximize-stack")
            .ToList();

        Assert.Equal(2, maximizes.Count);

        var surfaceId = SessionDocumentSurface.SurfaceIdFor(config.SessionId);
        Assert.Equal("maximized", maximizes[0].GetProperty("placement").GetString());
        Assert.Equal(surfaceId, maximizes[0].GetProperty("surface").GetString());

        // The stack it names is in the tree after, holding the session's surface.
        var stackId = maximizes[0].GetProperty("stack").GetString();
        Assert.False(string.IsNullOrEmpty(stackId), "the maximized stack is not named");
        var stack = Assert.Single(
            maximizes[0].GetProperty("stacks").EnumerateArray(),
            s => s.GetProperty("id").GetString() == stackId);
        Assert.Contains(stack.GetProperty("surfaces").EnumerateArray(), v => v.GetString()!.StartsWith(surfaceId, StringComparison.Ordinal));

        Assert.Equal("refused", maximizes[1].GetProperty("placement").GetString());
        Assert.Equal(stackId, maximizes[1].GetProperty("stack").GetString());
    }

    private static string MainWindowSource()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);

        while (here is not null && !File.Exists(Path.Combine(here.FullName, "AiDe.sln")))
        {
            here = here.Parent;
        }

        Assert.NotNull(here);

        var path = Path.Combine(here!.FullName, "src", "AiDe.App", "MainWindow.xaml.cs");
        Assert.True(File.Exists(path), $"MainWindow.xaml.cs was not found at {path}");
        return path;
    }
}
