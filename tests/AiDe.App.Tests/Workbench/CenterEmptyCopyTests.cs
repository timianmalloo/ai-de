using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Shell;

/// <summary>
/// Ruling 83 condition 2 (SH-4.2, L4): the Center's empty copy in Coding is <b>two true sentences,
/// by state</b> — <i>No session open.</i> with the New session action while no session document is
/// in the layout; <i>The session is docked at the left.</i> with no first action while one is open
/// at Left — and it never reads <i>No session open</i> while one is open. A third state: a session
/// surface the saved arrangement restored whose session is gone — <i>Nothing open here.</i>
/// </summary>
/// <remarks>
/// <b>Red before green:</b> the Center's placeholder rendered the factory's fallback, <i>"Welcome"
/// is not available in this build</i> — a build-defect sentence for the product's ordinary empty
/// state — in every state (<c>docs/proof/coding-recut-left-dock.md</c>).
/// </remarks>
public sealed class CenterEmptyCopyTests : IDisposable
{
    private readonly ComposedCoding.Workspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    private static IEnumerable<string> Texts(FrameworkElement root) =>
        ComposedCoding.Visuals<TextBlock>(root).Select(t => t.Text).Where(t => t.Length > 0);

    [Fact]
    public void WhileASessionIsOpenAtLeft_TheCenterSays_TheSessionIsDockedAtTheLeft_NeverNoSessionOpen()
    {
        var config = _workspace.Session("payments extraction");

        Sta.Run(() =>
        {
            using var frame = ComposedCoding.Show(1440, 900);
            var shell = frame.Shell;

            // State 1 — no session in the layout: "No session open." + the New session action + the recent-sessions line.
            var empty = frame.CenterEmpty();
            Assert.NotNull(empty);
            var texts = Texts(empty!).ToList();
            Assert.Contains("No session open.", texts);
            Assert.Contains("Or open a recent one from File → Recent sessions.", texts);
            Assert.DoesNotContain(texts, t => t.Contains("not available in this build", StringComparison.Ordinal));
            var action = ComposedCoding.Visuals<Button>(empty!).SingleOrDefault();
            Assert.True(action is not null, "the no-session state has no first action");
            // Named once, the gesture as the accelerator (not repeated in the name) and visible as a chip.
            Assert.Equal("New session", AutomationProperties.GetName(action!));
            Assert.Equal("Ctrl+N", AutomationProperties.GetAcceleratorKey(action!));
            Assert.Contains(ComposedCoding.Visuals<TextBlock>(action!), t => t.Text == "Ctrl+N");
            Assert.True(action!.Focusable && action.IsEnabled, "the first action is not a focus target");

            // State 2 — a session open at Left: the docked sentence, the View-menu line, NO first action.
            shell.OpenSessionDocument(config);
            frame.Settle();
            Assert.Equal(ZoneId.Left, shell.Coding.Service.Zones.FindZoneOf(SessionDocumentSurface.SurfaceIdFor(config.SessionId)));
            var docked = frame.CenterEmpty();
            Assert.NotNull(docked);
            texts = Texts(docked!).ToList();
            Assert.Contains("The session is docked at the left.", texts);
            Assert.Contains("Code viewers, prompt drafts and search open here, from the View menu.", texts);
            Assert.DoesNotContain(texts, t => t.Contains("No session open", StringComparison.Ordinal));
            Assert.DoesNotContain(texts, t => t.Contains("not available in this build", StringComparison.Ordinal));
            Assert.Empty(ComposedCoding.Visuals<Button>(docked!));
            // The focus target is the heading — a Control-view text element whose sentence is its
            // name and whose help is the second line — so an AT landing here hears the state (a
            // panel with a Name has no peer and is silent: the UX lens's finding).
            var heading = ComposedCoding.Visuals<TextBlock>(docked!).Single(t => t.Text == "The session is docked at the left.");
            Assert.True(heading.Focusable, "the docked-at-left heading is not the focus target");
            var peer = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(heading);
            Assert.True(peer is not null && peer.IsControlElement(), "the heading has no Control-view automation peer — a screen reader hears nothing");
            Assert.Equal("The session is docked at the left.", peer!.GetName());
            Assert.Equal("Code viewers, prompt drafts and search open here, from the View menu.", peer.GetHelpText());
            Assert.DoesNotContain(ComposedCoding.Visuals<Grid>(docked!), g => g.Focusable);

            // The Left collapsed with the session held: still docked there, behind its rail — said.
            Assert.True(shell.Coding.Service.Apply(new LayoutOperation.SetStackState(ZonesToTree.LeftStackId, StackState.Collapsed)).Applied);
            shell.Coding.Adapter.Render();
            frame.Settle();
            Assert.Contains("The session is docked at the left, collapsed — expand the rail to reach it.", Texts(frame.CenterEmpty()!));
            Assert.True(shell.Coding.Service.Apply(new LayoutOperation.SetStackState(ZonesToTree.LeftStackId, StackState.Docked)).Applied);
            shell.Coding.Adapter.Render();
            frame.Settle();

            // State 1 again after the session closes: the copy follows the model, render by render.
            Assert.True(shell.Coding.Service.Apply(new LayoutOperation.CloseSurface(SessionDocumentSurface.SurfaceIdFor(config.SessionId))).Applied);
            shell.Coding.Adapter.Render();
            frame.Settle();
            var again = frame.CenterEmpty();
            Assert.NotNull(again);
            Assert.Contains("No session open.", Texts(again!));
            return 0;
        }, 60);
    }

    /// <summary>The third state: a restored session surface whose session is gone — the Center says so and points left.</summary>
    [Fact]
    public void WhileARestoredSessionAtLeftCouldNotBeRevived_TheCenterSays_NothingOpenHere()
    {
        Sta.Run(() =>
        {
            using var frame = ComposedCoding.Show(1440, 900);
            var shell = frame.Shell;

            // A saved arrangement's session surface, restored with no live document behind it (the factory's island).
            var gone = new Surface(SessionDocumentSurface.SurfaceIdFor("20260101T000000Z-gone"), SessionDocumentSurface.Kind, "gone");
            Assert.True(shell.Coding.Service.Apply(new LayoutOperation.AddSurface(ZonesToTree.LeftStackId, gone)).Applied);
            shell.Coding.Adapter.Render();
            frame.Settle();

            var copy = frame.CenterEmpty();
            Assert.NotNull(copy);
            var texts = Texts(copy!).ToList();
            Assert.Contains("Nothing open here.", texts);
            Assert.Contains("The session at the left could not be restored. Reopen it from File → Recent sessions, or close its tab.", texts);
            Assert.DoesNotContain(texts, t => t.Contains("No session open", StringComparison.Ordinal));
            Assert.DoesNotContain(texts, t => t.Contains("docked at the left", StringComparison.Ordinal));

            // And the island at Left names the session and the same way out — never "No session is
            // open" one pane over from a Center that says one is here.
            var island = shell.Coding.Adapter.ContentFor(gone.SurfaceId);
            Assert.NotNull(island);
            var islandTexts = Texts(island!).ToList();
            Assert.Contains(islandTexts, t => t.StartsWith("“gone” could not be restored", StringComparison.Ordinal) && t.Contains("File → Recent sessions", StringComparison.Ordinal));
            Assert.DoesNotContain(islandTexts, t => t.Contains("No session is open", StringComparison.Ordinal));
            var islandText = ComposedCoding.Visuals<TextBlock>(island!).Single(t => t.Text.StartsWith("“gone”", StringComparison.Ordinal));
            var islandPeer = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(islandText);
            Assert.True(islandText.Focusable && islandPeer is not null && islandPeer.IsControlElement(), "the island is not a focusable Control-view element");
            return 0;
        }, 60);
    }

    /// <summary>The pure half: the copy is a function of the zones and the live registry, checked without a window.</summary>
    [Theory]
    [InlineData("none", "No session open.")]
    [InlineData("left-live", "The session is docked at the left.")]
    [InlineData("left-live-collapsed", "The session is docked at the left, collapsed — expand the rail to reach it.")]
    [InlineData("left-dead", "Nothing open here.")]
    [InlineData("bottom-live", "The session is docked at the bottom.")]
    [InlineData("two-live", "The sessions are docked at the left.")]
    [InlineData("two-dead", "Nothing open here.")]
    public void TheCopy_IsAFunctionOfTheZonesAndTheLiveRegistry(string state, string heading)
    {
        var s1 = new Surface("session:a", SessionDocumentSurface.Kind, "A");
        var s2 = new Surface("session:b", SessionDocumentSurface.Kind, "B");
        var layout = WorkbenchLayout.Default(PerspectiveSet.Coding);
        var live = new HashSet<string>(StringComparer.Ordinal);
        switch (state)
        {
            case "left-live":
                layout = ZoneLayoutService.OpenPane(layout, s1, ZoneId.Left).Layout;
                live.Add(s1.SurfaceId);
                break;
            case "left-live-collapsed":
                layout = ZoneLayoutService.OpenPane(layout, s1, ZoneId.Left).Layout;
                layout = ZoneLayoutService.CollapseZone(layout, ZoneId.Left).Layout;
                live.Add(s1.SurfaceId);
                break;
            case "left-dead":
                layout = ZoneLayoutService.OpenPane(layout, s1, ZoneId.Left).Layout;
                break;
            case "two-dead":
                layout = ZoneLayoutService.OpenPane(layout, s1, ZoneId.Left).Layout;
                layout = ZoneLayoutService.OpenPane(layout, s2, ZoneId.Bottom).Layout;
                break;
            case "bottom-live":
                layout = ZoneLayoutService.OpenPane(layout, s1, ZoneId.Bottom).Layout;
                live.Add(s1.SurfaceId);
                break;
            case "two-live":
                layout = ZoneLayoutService.OpenPane(layout, s1, ZoneId.Left).Layout;
                layout = ZoneLayoutService.OpenPane(layout, s2, ZoneId.Left).Layout;
                live.Add(s1.SurfaceId);
                live.Add(s2.SurfaceId);
                break;
        }

        var copy = CenterEmptyState.CopyFor(PerspectiveSet.Coding, layout, live.Contains);
        Assert.Equal(heading, copy.Heading);
        Assert.Equal(state == "none", copy.OffersNewSession);
        if (state == "two-dead")
        {
            Assert.Equal("The sessions at the left and the bottom could not be restored. Reopen them from File → Recent sessions, or close their tabs.", copy.Body);
        }
    }
}
