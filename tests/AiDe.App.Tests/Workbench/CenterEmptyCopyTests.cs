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
            Assert.Contains("New session", AutomationProperties.GetName(action!) + action!.Content, StringComparison.Ordinal);
            Assert.True(action.Focusable && action.IsEnabled, "the first action is not a focus target");

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
            // A focus target still exists — the landing can fall back here when the Left is collapsed.
            Assert.True(docked is UIElement { Focusable: true } || ComposedCoding.Visuals<UIElement>(docked!).Any(e => e.Focusable), "the docked-at-left copy has no focus target");
            // Named for assistive technology from its own sentences, not the placeholder's "Welcome".
            Assert.Contains(ComposedCoding.Visuals<FrameworkElement>(docked!).Prepend(docked!), e => AutomationProperties.GetName(e).Contains("docked at the left", StringComparison.Ordinal));

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
            Assert.Contains("The session at the left could not be restored; its recovery is on the left.", texts);
            Assert.DoesNotContain(texts, t => t.Contains("No session open", StringComparison.Ordinal));
            Assert.DoesNotContain(texts, t => t.Contains("docked at the left", StringComparison.Ordinal));
            return 0;
        }, 60);
    }

    /// <summary>The pure half: the copy is a function of the zones and the live registry, checked without a window.</summary>
    [Theory]
    [InlineData("none", "No session open.")]
    [InlineData("left-live", "The session is docked at the left.")]
    [InlineData("left-dead", "Nothing open here.")]
    [InlineData("bottom-live", "The session is docked at the bottom.")]
    [InlineData("two-live", "The sessions are docked at the left.")]
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
            case "left-dead":
                layout = ZoneLayoutService.OpenPane(layout, s1, ZoneId.Left).Layout;
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
    }
}
