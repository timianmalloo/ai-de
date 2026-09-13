using System.IO;
using AiDe.App.Tests.Shell;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// Ruling 83 (SH-4.2, L2/L3): a <b>newly created</b> session document opens in Coding's <b>Left</b>
/// zone, docked — <c>Maximized == null</c>, no sibling zone collapsed by the open — and a reopen
/// follows the same zone rule. Ruling 47's maximize-on-create is superseded by the operator's own
/// gesture (<i>"new sessions should default into the left dock"</i>), and its unit,
/// <c>NewSessionPlacement</c>, retires with it: no caller, then no type.
/// </summary>
/// <remarks>
/// <b>Red before green:</b> the create path tabbed the document into the Center (the placement
/// policy's "any stack" fallback) and <c>MainWindow.NewSessionAsync</c> then maximized it —
/// <c>ANewSessionTakesTheWholeTreeTests</c> pinned exactly that and is replaced by this file.
/// </remarks>
public sealed class SessionDocumentPlacementTests : IDisposable
{
    private readonly ComposedCoding.Workspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public void ANewSessionDocument_OpensInTheLeftZone_Docked_WithMaximizedNull()
    {
        var config = _workspace.Session("Front door");

        Sta.Run(() =>
        {
            using var frame = ComposedCoding.Show(1440, 900);
            var shell = frame.Shell;
            var zonesBefore = shell.Coding.Service.Zones;

            var said = shell.OpenSessionDocument(config);
            frame.Settle();

            var zones = shell.Coding.Service.Zones;
            var surfaceId = SessionDocumentSurface.SurfaceIdFor(config.SessionId);
            Assert.Equal(ZoneId.Left, zones.FindZoneOf(surfaceId));
            Assert.Null(zones.Maximized);
            Assert.False(zones.Zone(ZoneId.Left).Collapsed);

            // Docked, not maximized: no sibling zone changed its collapsed state because of the open.
            foreach (var id in new[] { ZoneId.Right, ZoneId.Bottom, ZoneId.Center })
            {
                Assert.Equal(zonesBefore.Zone(id).Collapsed, zones.Zone(id).Collapsed);
            }

            // The Left kept the re-cut's extent (the open floors an empty tool zone; it never shrinks one).
            Assert.Equal(WorkbenchLayout.CodingLeftExtent, zones.Zone(ZoneId.Left).Extent);

            // The rendered surface: the document is on screen in the Left pane, active — you open it
            // to type in it — and no "maximized" is announced.
            Assert.NotNull(shell.Coding.Adapter.ContentFor(surfaceId));
            Assert.Equal(surfaceId, shell.Coding.Adapter.ActiveSurfaceId);
            Assert.DoesNotContain("aximized", said, StringComparison.Ordinal);
            Assert.StartsWith("Session “Front door” opened.", said, StringComparison.Ordinal);
            return 0;
        }, 60);
    }

    /// <summary>The second row: a reopen follows the zone rule too — Left, docked, nothing rearranged.</summary>
    [Fact]
    public void AReopenedSessionDocument_FollowsTheSameZoneRule_AndRearrangesNothing()
    {
        var first = _workspace.Session("First");
        var reopened = _workspace.Session("Reopened");

        Sta.Run(() =>
        {
            using var frame = ComposedCoding.Show(1440, 900);
            var shell = frame.Shell;
            shell.OpenSessionDocument(first);
            var shapeBefore = shell.Coding.Service.Zones.Shape();

            shell.OpenSessionDocument(reopened);   // the reopen path in MainWindow is exactly this call
            frame.Settle();

            var zones = shell.Coding.Service.Zones;
            Assert.Equal(ZoneId.Left, zones.FindZoneOf(SessionDocumentSurface.SurfaceIdFor(reopened.SessionId)));
            Assert.Equal(ZoneId.Left, zones.FindZoneOf(SessionDocumentSurface.SurfaceIdFor(first.SessionId)));   // a sibling tab; the first stays
            Assert.Null(zones.Maximized);
            Assert.True(zones.Zone(ZoneId.Bottom).Collapsed, "a reopen rearranged the Bottom");
            Assert.NotEqual(shapeBefore, zones.Shape());   // DC-016: the open did change the model (a second tab)
            return 0;
        }, 60);
    }

    /// <summary>
    /// The landing (spec §C5; DESIGN.md's row as amended): through the real shell, Coding lands on
    /// the session document at Left when one is open, else on the Center's empty copy — and each
    /// landing has a focus target (the UX &amp; Accessibility lens's SH-4.1 condition, inherited).
    /// </summary>
    [Fact]
    public void TheCodingLanding_IsTheSessionAtLeft_ElseTheCentersEmptyCopy_EachWithAFocusTarget()
    {
        var config = _workspace.Session("landing");

        Sta.Run(() =>
        {
            using var frame = ComposedCoding.Show(1440, 900);
            var shell = frame.Shell;

            var empty = PerspectiveShell.LandingSurfaceFor(shell.Coding);
            Assert.Equal(ZonesToTree.WelcomePlaceholder.SurfaceId, empty);
            var copy = shell.Coding.Adapter.ContentFor(empty!);
            Assert.True(copy is not null && HasFocusTarget(copy), "the Center's empty copy has no focus target — the landing falls through");

            shell.OpenSessionDocument(config);
            frame.Settle();
            var surfaceId = SessionDocumentSurface.SurfaceIdFor(config.SessionId);
            Assert.Equal(surfaceId, PerspectiveShell.LandingSurfaceFor(shell.Coding));
            var document = shell.Coding.Adapter.ContentFor(surfaceId);
            Assert.True(document is not null && HasFocusTarget(document), "the session document has no focus target");
            return 0;
        }, 60);

        static bool HasFocusTarget(System.Windows.DependencyObject element)
        {
            if (element is System.Windows.UIElement { Focusable: true, IsEnabled: true })
            {
                return true;
            }

            var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(element);
            for (var i = 0; i < count; i++)
            {
                if (HasFocusTarget(System.Windows.Media.VisualTreeHelper.GetChild(element, i)))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>The zone rule is data on the kind's row — what the open reads, and what a reader can check without a window.</summary>
    [Fact]
    public void TheSessionDocumentKindsRow_NamesTheLeftZone()
    {
        var row = Assert.Single(SurfaceContentFactory.Kinds, k => k.Kind == SessionDocumentSurface.Kind);
        Assert.Equal(ZoneId.Left, row.Zone);
    }

    /// <summary>
    /// L3: <c>NewSessionPlacement</c> has no callers — and, no dead code surviving the turn, no
    /// definition: the token is absent from the product and the probes. The window's create path
    /// carries no maximize.
    /// </summary>
    [Fact]
    public void NewSessionPlacement_IsGone_AndTheCreatePathCarriesNoMaximize()
    {
        var root = RepositoryRoot();
        var hits = new[] { "src", "tests" }
            .SelectMany(d => Directory.EnumerateFiles(Path.Combine(root, d), "*.cs", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => !f.EndsWith("SessionDocumentPlacementTests.cs", StringComparison.Ordinal))
            .Where(f => File.ReadAllText(f).Contains("NewSessionPlacement", StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(root, f))
            .ToList();
        Assert.True(hits.Count == 0, "NewSessionPlacement survives in:\n  " + string.Join("\n  ", hits));

        var window = File.ReadAllText(Path.Combine(root, "src", "AiDe.App", "MainWindow.xaml.cs"));
        var at = window.IndexOf("private async Task<string> NewSessionAsync()", StringComparison.Ordinal);
        Assert.True(at >= 0, "MainWindow.xaml.cs has no NewSessionAsync — this test is reading a document whose shape it does not understand (DC-016)");
        var next = window.IndexOf("\n    private ", at + 10, StringComparison.Ordinal);
        var body = next < 0 ? window[at..] : window[at..next];
        Assert.DoesNotContain("Maximized", body, StringComparison.Ordinal);
        Assert.DoesNotContain("WholeTree", body, StringComparison.Ordinal);
    }

    private static string RepositoryRoot()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);
        while (here is not null && !File.Exists(Path.Combine(here.FullName, "AiDe.sln")))
        {
            here = here.Parent;
        }

        Assert.NotNull(here);
        return here!.FullName;
    }
}
