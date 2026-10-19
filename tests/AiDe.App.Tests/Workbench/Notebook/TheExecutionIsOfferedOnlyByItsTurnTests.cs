using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Kinds;

/// <summary>
/// The Owner's condition (1) on <see cref="SurfaceContentFactory.SurfaceEntry.TurnAction"/>: the <c>execution</c> kind is
/// offered by no View menu and by no Center empty state, in any perspective. Its one door is a turn's <i>Open the log</i>.
/// <c>TheExecutionOpensFromOpenLogTests</c> presses that door end to end; this class pins that no other door exists.
/// </summary>
/// <remarks>
/// Both surfaces are rendered: the menu bar by <see cref="MainMenuBuilder"/> from <see cref="PerspectiveMenu.For(Perspective)"/>,
/// and the empty state by <see cref="CenterEmptyState.Build"/> from <see cref="CenterEmptyState.CopyFor"/>. Where the empty
/// copy points at the View menu, that menu is the offer, so it is read too.
/// </remarks>
public sealed class TheExecutionIsOfferedOnlyByItsTurnTests
{
    private const string Execution = "execution";

    public static TheoryData<string> Perspectives() => [.. PerspectiveSet.All.Select(p => p.Id)];

    [Theory]
    [MemberData(nameof(Perspectives))]
    public void NoViewMenu_OffersTheExecution(string perspectiveId) => Sta.Run(() =>
    {
        var perspective = PerspectiveSet.All.Single(p => p.Id == perspectiveId);

        // The View menu, as the menu bar renders it — and every other menu with it.
        Assert.DoesNotContain(RenderedMenuTitles(perspective), t => t.Contains(Execution, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(PerspectiveMenu.For(perspective).Commands, c => c.Id.EndsWith(Execution, StringComparison.Ordinal));
    }, 60);

    [Theory]
    [MemberData(nameof(Perspectives))]
    public void NoCenterEmptyState_OffersTheExecution(string perspectiveId) => Sta.Run(() =>
    {
        var perspective = PerspectiveSet.All.Single(p => p.Id == perspectiveId);
        var titles = RenderedMenuTitles(perspective);

        // The Center's empty state, in each state its copy is derived from.
        foreach (var (state, layout, live) in EmptyStates(perspective))
        {
            var copy = CenterEmptyState.CopyFor(perspective, layout, live);
            var rendered = CenterEmptyState.Build(copy, () => { });

            var actions = Logical(rendered).OfType<Button>().Select(AutomationProperties.GetName).ToList();
            Assert.All(actions, name => Assert.Equal("New session", name));
            var texts = Logical(rendered).OfType<TextBlock>().Select(t => t.Text).ToList();
            Assert.DoesNotContain(texts, t => t.Contains(Execution, StringComparison.OrdinalIgnoreCase));

            if (texts.Any(t => t.Contains("View menu", StringComparison.Ordinal)))
            {
                Assert.True(!titles.Any(t => t.Contains(Execution, StringComparison.OrdinalIgnoreCase)),
                    $"{perspective.Title}, {state}: the empty state points at a View menu that offers the execution [{string.Join(", ", titles)}]");
            }
        }
    }, 60);

    [Fact]
    public void TheExecutionsOnlyDoor_IsTheTurnsOpenTheLog()
    {
        var row = Assert.Single(SurfaceContentFactory.Kinds, k => k.Kind == Execution);
        Assert.Equal(new SurfaceContentFactory.SurfaceEntry.TurnAction(TurnActionKind.OpenLog), row.Entry);
        Assert.Throws<ArgumentException>(() => PerspectiveMenu.Opener(row));
    }

    private static IEnumerable<(string State, WorkbenchLayout Layout, Func<string, bool> Live)> EmptyStates(Perspective perspective)
    {
        var empty = WorkbenchLayout.Default(perspective);
        yield return ("its default layout", empty, _ => false);

        var notebook = new Surface(SessionDocumentSurface.SurfaceIdFor("s-1"), SessionDocumentSurface.Kind, "s-1");
        var moved = ZoneLayoutService.OpenPane(empty, notebook, ZoneId.Left).Layout;
        yield return ("a live notebook at the left", moved, _ => true);
        yield return ("a notebook at the left that was not revived", moved, _ => false);
    }

    private static List<string> RenderedMenuTitles(Perspective perspective)
    {
        var controller = new WorkbenchController(new ZoneBackedLayoutService(), new RecordingAnnouncer());
        var menu = new Menu();
        MainMenuBuilder.Build(menu, controller, PerspectiveMenu.For(perspective));
        return Items(menu.Items.OfType<MenuItem>()).Select(i => i.Header?.ToString() ?? string.Empty).ToList();

        static IEnumerable<MenuItem> Items(IEnumerable<MenuItem> level) =>
            level.SelectMany(i => Items(i.Items.OfType<MenuItem>()).Prepend(i));
    }

    private static IEnumerable<DependencyObject> Logical(DependencyObject root) =>
        LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>().SelectMany(Logical).Prepend(root);
}
