using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The menu bar, built from the command catalog.
/// </summary>
/// <remarks>
/// <para><b>Reported as a defect.</b> Opening a workspace was reachable only by <c>Ctrl+K, O</c> or
/// by an environment variable set before launch — so indexing, the graph and everything downstream
/// were unreachable by anyone who had not been told the chord. A command reachable only by a chord
/// nobody was told about is not a feature.</para>
///
/// <para>Built from the SAME catalog the palette reads, so a menu item cannot offer something the
/// product no longer does.</para>
/// </remarks>
public sealed class MainMenuTests
{
    private static void OnSta(Action work) =>
        Sta.Run(work, 60);

    private static (Menu Menu, RecordingAnnouncer Announcer) Build() => Build(PerspectiveSet.Coding);

    private static (Menu Menu, RecordingAnnouncer Announcer) Build(Perspective perspective)
    {
        var announcer = new RecordingAnnouncer();
        var controller = new WorkbenchController(new ZoneBackedLayoutService(), announcer);
        var menu = new Menu();
        MainMenuBuilder.Build(menu, controller, PerspectiveMenu.For(perspective));
        return (menu, announcer);
    }

    private static IEnumerable<MenuItem> Items(Menu menu) =>
        menu.Items.OfType<MenuItem>().SelectMany(top => top.Items.OfType<MenuItem>());

    [Fact]
    public void OpeningAWorkspaceIsReachableFromTheMenu() => OnSta(() =>
    {
        // The reported defect, as an assertion.
        var (menu, _) = Build();

        var open = WorkbenchCommandCatalog.All.First(c => c.Id == "workspace.open");
        Assert.Contains(Items(menu), i => Equals(i.Header, open.Title));
    });

    [Fact]
    public void EveryMenuItemShowsItsKeyboardChord() => OnSta(() =>
    {
        // Re-scoped by Addendum C (US-C10 b3, PS-M4): an item shows a keystroke ONLY when one is bound,
        // rendered from the binding's display string; an item whose command has no bound gesture shows
        // none. The earlier form asserted a chord string on every item, bound or not — and the
        // `Ctrl+K, X` chords are announced strings nothing binds, so the menu promised keystrokes that
        // did nothing (the fifth page-one fact).
        var (menu, _) = Build();
        var byTitle = WorkbenchCommandCatalog.All.ToDictionary(c => c.Title, StringComparer.Ordinal);
        var checkedItems = 0;

        foreach (var item in Items(menu))
        {
            if (!byTitle.TryGetValue(item.Header?.ToString() ?? string.Empty, out var command))
            {
                continue;   // a derived open/show entry: no catalog row, nothing bound (asserted in PerspectiveMenuTests)
            }

            checkedItems++;
            var bound = KeyGestures.For(command).FirstOrDefault();
            if (bound is null)
            {
                Assert.True(string.IsNullOrEmpty(item.InputGestureText),
                    $"{command.Id} shows '{item.InputGestureText}' but nothing binds it");
            }
            else
            {
                Assert.Equal(
                    bound.GetDisplayStringForCulture(System.Globalization.CultureInfo.CurrentCulture),
                    item.InputGestureText);
            }
        }

        Assert.True(checkedItems > 10, $"only {checkedItems} catalog-backed items checked — this test would pass by looking at nothing");
    });

    [Fact]
    public void EveryMenuItemResolvesToARealCatalogCommand() => OnSta(() =>
    {
        // Hand-written menu items beside a catalog are how a menu starts offering something the
        // product no longer does. Since Addendum C the offered set is the derived contribution —
        // the catalog's rows plus the allow-list-derived openers — and every rendered item must be
        // one of those, in every perspective.
        foreach (var perspective in PerspectiveSet.All)
        {
            var (menu, _) = Build(perspective);
            var titles = PerspectiveMenu.For(perspective).Commands.Select(c => c.Title).ToHashSet(StringComparer.Ordinal);

            Assert.All(Items(menu), item => Assert.Contains(item.Header?.ToString() ?? string.Empty, titles));
        }
    });

    [Fact]
    public void InvokingAMenuItemRunsItsCommand() => OnSta(() =>
    {
        var (menu, announcer) = Build();

        var item = Items(menu).First(i => Equals(i.Header, "Reset workbench layout"));
        item.RaiseEvent(new System.Windows.RoutedEventArgs(MenuItem.ClickEvent));

        // Announced, like every other route into the same command.
        Assert.False(string.IsNullOrWhiteSpace(announcer.Last));
    });

    [Fact]
    public void TheMenuCoversEveryCatalogCommand() => OnSta(() =>
    {
        // The control for a fuller menu: a command added to the catalog and not to any menu is
        // reachable only by a chord again, which is the defect this whole surface exists to fix.
        //
        // Re-scoped by Addendum C (US-C4): one menu no longer covers every command — a host-only
        // operation is absent in Explore by rule — so the invariant is that every catalog command
        // has a placement in AT LEAST ONE perspective's rendered menu (the entry verbs in every one).
        var offered = new HashSet<string>(StringComparer.Ordinal);
        foreach (var perspective in PerspectiveSet.All)
        {
            var (menu, _) = Build(perspective);
            offered.UnionWith(Items(menu).Select(i => i.Header?.ToString() ?? string.Empty));
        }

        var missing = WorkbenchCommandCatalog.All
            .Where(c => !offered.Contains(c.Title))
            .Select(c => c.Id)
            .ToList();

        Assert.True(missing.Count == 0, "catalog commands with no menu entry in any perspective: " + string.Join(", ", missing));

        // And the entry verbs are in every perspective's File menu (US-C11; §B3 rule 1).
        foreach (var perspective in PerspectiveSet.All)
        {
            var (menu, _) = Build(perspective);
            var file = menu.Items.OfType<MenuItem>().First(m => Equals(m.Header, "_File")).Items.OfType<MenuItem>()
                .Select(i => i.Header?.ToString()).ToList();
            Assert.Contains("New session…", file);
            Assert.Contains("New terminal", file);
        }
    });

    // ADR-0030 test 3 as amended by Ruling 84 — the mutation test, through the REAL builder: the
    // Loomkeeper entries are in Coordination's View menu and nowhere else. Coordination's View lists
    // the five Show entries in the kind rows' order and the ledger verb (`watcher.raiseDispute`,
    // scoped `Admits("leaderboard")`, so it follows the leaderboard by construction); Coding's View
    // lists none of the six and keeps its own derived group; Coordination has no Prompt menu (a host
    // without the composer — spec §B3 rule 3). Then the mutation: a test-time kind row admitted only
    // by Coordination is rendered in Coordination's menu and in no other, with no edit to the
    // builder. RED before the allow-lists moved: Coding's View listed all six, Coordination's none.
    [Fact]
    public void CoordinationsViewMenu_ListsTheFiveShowEntriesAndTheDisputeVerb_AndCodingsListsNeither() => OnSta(() =>
    {
        var coordination = PerspectiveSet.All.Single(p => p.Id == "coordination");
        string[] loomkeeper = ["Show terminal sessions", "Show message board", "Show leaderboard", "Show ledger", "Show daydreams"];
        const string dispute = "Raise score dispute on the latest scored episode";

        var (menu, _) = Build(coordination);
        var headers = menu.Items.OfType<MenuItem>().Select(m => m.Header?.ToString()).ToList();
        Assert.Equal(["_File", "_Edit", "_View", "_Window", "_Help"], headers);

        var view = ViewTitles(menu);
        Assert.Equal(loomkeeper, view.Where(loomkeeper.Contains).ToList());          // all five, in row order
        Assert.Contains(dispute, view);
        Assert.True(view.IndexOf(dispute) < view.IndexOf(loomkeeper[0]), "the catalog verb precedes the derived group (PS-M2)");

        var (coding, _) = Build(PerspectiveSet.Coding);
        var codingView = ViewTitles(coding);
        Assert.DoesNotContain(codingView, loomkeeper.Contains);
        Assert.DoesNotContain(dispute, codingView);
        Assert.Equal(["New search", "New code viewer", "Show diagnostics"], codingView.Where(t => t.StartsWith("New ", StringComparison.Ordinal) || t.StartsWith("Show ", StringComparison.Ordinal)).ToList());

        foreach (var other in new[] { PerspectiveSet.Explore, PerspectiveSet.Architecture })
        {
            var (menuOfOther, _) = Build(other);
            Assert.DoesNotContain(ViewTitles(menuOfOther), loomkeeper.Contains);
            Assert.DoesNotContain(dispute, ViewTitles(menuOfOther));
        }

        // The mutation: one appended row, admitted by Coordination only.
        var mutant = new SurfaceContentFactory.SurfaceKind(
            "fleet-mutant", "Fleet mutant", "A test-time kind admitted by Coordination alone.",
            static (_, _) => new System.Windows.Controls.Border(),
            Perspectives: [coordination], SurfaceContentFactory.Instances.One, new SurfaceContentFactory.SurfaceEntry.Derived("_View"));
        var kinds = SurfaceContentFactory.Kinds.Append(mutant).ToList();

        foreach (var perspective in PerspectiveSet.All)
        {
            var announcer = new RecordingAnnouncer();
            var controller = new WorkbenchController(new ZoneBackedLayoutService(), announcer);
            var rendered = new Menu();
            MainMenuBuilder.Build(rendered, controller, PerspectiveMenu.For(perspective, kinds, WorkbenchCommandCatalog.All));
            var titles = ViewTitles(rendered);

            if (perspective == coordination)
            {
                Assert.Contains("Show fleet mutant", titles);
            }
            else
            {
                Assert.DoesNotContain("Show fleet mutant", titles);
            }
        }

        static List<string> ViewTitles(Menu m) =>
            m.Items.OfType<MenuItem>().First(top => Equals(top.Header, "_View")).Items.OfType<MenuItem>()
                .Select(i => i.Header?.ToString() ?? string.Empty).ToList();
    });

    [Fact]
    public void RecentWorkspacesArePersistedNewestFirst_AndDeduplicated()
    {
        var dir = Path.Combine(Path.GetTempPath(), "aide-recent-" + Guid.NewGuid().ToString("N"));
        var a = Path.Combine(dir, "alpha");
        var b = Path.Combine(dir, "beta");
        Directory.CreateDirectory(a);
        Directory.CreateDirectory(b);

        try
        {
            MainMenuBuilder.RememberWorkspace(dir, a);
            MainMenuBuilder.RememberWorkspace(dir, b);
            MainMenuBuilder.RememberWorkspace(dir, a);

            var recent = MainMenuBuilder.RecentWorkspaces(dir);

            Assert.Equal([a, b], recent);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void ARecentWorkspaceThatNoLongerExistsIsDropped()
    {
        // A menu offering something that cannot work teaches the user to distrust the menu.
        var dir = Path.Combine(Path.GetTempPath(), "aide-recent-" + Guid.NewGuid().ToString("N"));
        var gone = Path.Combine(dir, "deleted");
        Directory.CreateDirectory(gone);

        try
        {
            MainMenuBuilder.RememberWorkspace(dir, gone);
            Directory.Delete(gone);

            Assert.Empty(MainMenuBuilder.RecentWorkspaces(dir));
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void TheFileMenuOffersAnExit_WhenTheHostSuppliesOne() => OnSta(() =>
    {
        var announcer = new RecordingAnnouncer();
        var controller = new WorkbenchController(new ZoneBackedLayoutService(), announcer);
        var menu = new Menu();
        var exited = false;

        MainMenuBuilder.Build(menu, controller, PerspectiveMenu.For(PerspectiveSet.Coding), () => exited = true);

        var exit = menu.Items.OfType<MenuItem>()
            .First(m => Equals(m.Header, "_File"))
            .Items.OfType<MenuItem>()
            .First(i => Equals(i.Header, "E_xit"));

        exit.RaiseEvent(new System.Windows.RoutedEventArgs(MenuItem.ClickEvent));
        Assert.True(exited);
    });
}
