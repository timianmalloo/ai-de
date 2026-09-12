using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using Kind = AiDe.App.Workbench.SurfaceContentFactory.SurfaceKind;

namespace AiDe.App.Tests.Perspectives;

/// <summary>
/// ADR-0030: the Perspective set is a closed Core row set, the allow-list is a column on the App's
/// surface-kind rows, and the menu, the palette and the routed kind-open are DERIVED from the join
/// of those two row sets — never a second hand-written list (Ruling 55b).
/// </summary>
/// <remarks>
/// The data-invariant tests (the allow-list matrix, the non-empty set, the entry verbs) were each
/// seen red by MUTATION before being trusted; the record is <c>docs/proof/perspective-registry.md</c>.
/// The derivation tests were red against the static tuple list the builder held before this slice.
/// </remarks>
public sealed class PerspectiveMenuTests
{
    private static void OnSta(Action work) => Sta.Run(work, 60);

    private static IEnumerable<MenuItem> Items(Menu menu) =>
        menu.Items.OfType<MenuItem>().SelectMany(top => top.Items.OfType<MenuItem>());

    private static IReadOnlyList<string> Titles(Menu menu, string header) =>
        [.. menu.Items.OfType<MenuItem>()
            .Where(m => Equals(m.Header, header))
            .SelectMany(m => m.Items.OfType<MenuItem>())
            .Select(i => i.Header?.ToString() ?? string.Empty)];

    private static IReadOnlyList<string> Headers(Menu menu) =>
        [.. menu.Items.OfType<MenuItem>().Select(m => m.Header?.ToString() ?? string.Empty)];

    private static Perspective ById(string id) => PerspectiveSet.All.Single(p => p.Id == id);

    private static (Menu Menu, RecordingAnnouncer Announcer) Render(Perspective perspective, PerspectiveMenu? derived = null)
    {
        var menu = new Menu();
        var announcer = new RecordingAnnouncer();
        MainMenuBuilder.Build(
            menu,
            new WorkbenchController(new ZoneBackedLayoutService(), announcer),
            derived ?? PerspectiveMenu.For(perspective));
        return (menu, announcer);
    }

    // ── US-C10 b2: the announced-gesture uniqueness collector ──────────────────────────────

    // One collector over every announced gesture string: the catalog (which already carries the
    // derived harness rows), the allow-list-derived open/show rows of every perspective, and the
    // window-scope bindings. No string may be announced for more than one command. RED on the day
    // it was written with four collisions:
    //   Ctrl+K, M → workbench.moveSurface + workbench.newClassDiagram
    //   Ctrl+K, D → workspace.diagnostics + workbench.newPromptDraft + workbench.newDiagnostics
    //   Ctrl+K, F → workbench.floatPane + workbench.newSearch
    //   Ctrl+K, G → workbench.focusCanvas + terminal.new.copilot ("New GitHub Copilot session")
    // The scan states its shape (DC-118): root = WorkbenchCommandCatalog.All ∪ PerspectiveMenu.For(p)
    // over every p ∪ KeyGestures.For over all of them; token = the Gesture string and each
    // binding's display string; allowlist = none.
    [Fact]
    public void NoAnnouncedGestureStringNamesMoreThanOneCommand()
    {
        var announced = new List<(string Gesture, string CommandId)>();
        var commands = WorkbenchCommandCatalog.All
            .Concat(PerspectiveSet.All.SelectMany(p => PerspectiveMenu.For(p).Commands))
            .DistinctBy(c => c.Id, StringComparer.Ordinal)
            .ToList();

        foreach (var command in commands)
        {
            if (!string.IsNullOrWhiteSpace(command.Gesture))
            {
                announced.Add((command.Gesture, command.Id));
            }

            foreach (var binding in KeyGestures.For(command))
            {
                announced.Add((binding.GetDisplayStringForCulture(CultureInfo.CurrentCulture), command.Id));
            }
        }

        Assert.True(announced.Count > 20, "the collector read almost nothing — it would pass by looking at nothing (DC-016)");
        Assert.Contains(commands, c => c.Id.StartsWith(PerspectiveMenu.ShowCommandPrefix, StringComparison.Ordinal));

        var collisions = announced
            .GroupBy(a => a.Gesture, StringComparer.Ordinal)
            .Select(g => (Gesture: g.Key, Commands: g.Select(a => a.CommandId).Distinct(StringComparer.Ordinal).ToList()))
            .Where(g => g.Commands.Count > 1)
            .Select(g => $"{g.Gesture} → {string.Join(" + ", g.Commands)}")
            .ToList();

        Assert.True(collisions.Count == 0,
            "gesture strings announced for more than one command:\n  " + string.Join("\n  ", collisions));
    }

    // US-C10 b3, the copy half: no operator-facing string in the App names an unbound chord. The
    // scan states its shape (DC-118): root src/AiDe.App, recursive, token `Ctrl+K,` inside a string
    // literal; allowlist = the catalog and the harness profiles (the chord strings' one home as
    // DATA) and one frozen pre-existing site outside this slice's paths. A NEW literal reddens.
    [Fact]
    public void NoOperatorCopyNamesAnUnboundChord()
    {
        // The chord strings' one home as DATA is the Core catalog and the harness profiles — outside
        // this root, so no allowlist is needed. Frozen, pre-existing, outside the Shell lane's paths
        // (Addendum C plan §2): the diagnostics surface's empty state says "Run Re-index (Ctrl+K, I)".
        // Recorded as a finding for its owner; the count is pinned exactly, so a removed-then-re-added
        // literal cannot net to zero.
        var root = Path.Combine(RepoRoot(), "src", "AiDe.App");
        var frozen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(root, "Workbench", "DiagnosticsSurface.cs")] = 1,
        };

        var offenders = new List<string>();
        var scanned = 0;
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            scanned++;
            var hits = File.ReadLines(file)
                .Select((line, i) => (line, i))
                .Where(x => x.line.Contains("Ctrl+K,", StringComparison.Ordinal)
                    && x.line.Contains('"')
                    && !x.line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                .ToList();
            var pinned = frozen.GetValueOrDefault(file, 0);
            if (hits.Count != pinned)
            {
                offenders.Add($"{Path.GetRelativePath(RepoRoot(), file)}: {hits.Count} chord literal(s), {pinned} pinned"
                    + string.Concat(hits.Select(h => $"\n      :{h.i + 1}: {h.line.Trim()}")));
            }
        }

        Assert.True(scanned > 50, $"only {scanned} file(s) scanned — this test would pass by looking at nothing (DC-016)");

        Assert.True(offenders.Count == 0, "operator copy promising an unbound chord:\n  " + string.Join("\n  ", offenders));
    }

    // US-C10 b1: each perspective command has a single-stroke gesture WPF binds — on the number row
    // and the keypad — and the announced string is the binding's own display string (copy never lies).
    [Fact]
    public void EveryPerspectiveCommandIsBound_OnTheDigitRowAndTheKeypad_AndTheAnnouncedStringIsTheBinding()
    {
        foreach (var perspective in PerspectiveSet.All)
        {
            var command = WorkbenchCommandCatalog.All.Single(c => c.Id == perspective.CommandId);
            var bound = KeyGestures.For(command).ToList();

            Assert.Equal(2, bound.Count);
            Assert.All(bound, g => Assert.Equal(ModifierKeys.Control, g.Modifiers));
            Assert.Equal(Key.D0 + perspective.Order, bound[0].Key);
            Assert.Equal(Key.NumPad0 + perspective.Order, bound[1].Key);
            Assert.Equal(command.Gesture, bound[0].GetDisplayStringForCulture(CultureInfo.InvariantCulture));
        }
    }

    // US-C10 b1, the window half: Bind installs the bindings at the host the window passes, one per
    // gesture, routed to the perspective's own command id.
    [Fact]
    public void Bind_InstallsSixPerspectiveKeyBindings_AtWindowScope() => OnSta(() =>
    {
        var host = new Grid();
        new WorkbenchController(new ZoneBackedLayoutService(), new RecordingAnnouncer()).Bind(host);

        var perspectiveBindings = host.InputBindings.OfType<KeyBinding>()
            .Where(b => b.Command is RoutedUICommand routed && PerspectiveSet.ByCommandId(routed.Name) is not null)
            .Select(b => (Id: ((RoutedUICommand)b.Command).Name, b.Key, b.Modifiers))
            .ToList();

        Assert.Equal(6, perspectiveBindings.Count);
        foreach (var perspective in PerspectiveSet.All)
        {
            Assert.Contains((perspective.CommandId, Key.D0 + perspective.Order, ModifierKeys.Control), perspectiveBindings);
            Assert.Contains((perspective.CommandId, Key.NumPad0 + perspective.Order, ModifierKeys.Control), perspectiveBindings);
        }
    });

    // ── ADR-0030 rule 2 / US-C3 b5: the allow-list column ─────────────────────────────────

    // The non-empty-set build test: every kind row names at least one admitting perspective, none
    // names one whose body is not a host, none names a perspective twice. An empty set is an
    // unreachable surface — the contexts/joins/daydreams defect the inventory found.
    [Fact]
    public void EveryKindRowAdmitsAtLeastOneHostPerspective_AndNeverAFullWindowOne()
    {
        Assert.True(SurfaceContentFactory.Kinds.Count >= 18, "the kind list shrank — this test would be checking less than the product has");

        foreach (var row in SurfaceContentFactory.Kinds)
        {
            Assert.True(row.Perspectives.Count > 0, $"kind '{row.Kind}' admits no perspective: unreachable by omission (US-C3 b5)");
            Assert.All(row.Perspectives, p => Assert.Equal(PerspectiveBody.DockHost, p.Body));
            Assert.Equal(row.Perspectives.Count, row.Perspectives.Distinct().Count());
            Assert.All(row.Perspectives, p => Assert.Contains(p, PerspectiveSet.All));
            Assert.False(string.IsNullOrWhiteSpace(row.Title), row.Kind);
            Assert.False(string.IsNullOrWhiteSpace(row.Summary), row.Kind);
        }

        Assert.DoesNotContain(SurfaceContentFactory.Kinds, k => k.Perspectives.Contains(PerspectiveSet.Explore));
    }

    // §A7's table as ruled (Rulings 59–61): the kind × perspective matrix and the Instances column,
    // literally. A row moved between perspectives fails here, by design — Ruling 60's condition is
    // that the Loomkeeper kinds move as a set, in one ruling.
    [Fact]
    public void TheAllowListsEqualTheSpecsTable()
    {
        var expected = new (string Kind, string[] Admitting, SurfaceContentFactory.Instances Instances)[]
        {
            ("session-document", ["coding"], SurfaceContentFactory.Instances.Many),
            ("terminal",         ["coding"], SurfaceContentFactory.Instances.Many),
            ("prompt",           ["coding"], SurfaceContentFactory.Instances.Many),
            ("sessions",         ["coding"], SurfaceContentFactory.Instances.One),
            ("board",            ["coding"], SurfaceContentFactory.Instances.One),
            ("leaderboard",      ["coding"], SurfaceContentFactory.Instances.One),
            ("ledger",           ["coding"], SurfaceContentFactory.Instances.One),
            ("daydreams",        ["coding"], SurfaceContentFactory.Instances.One),
            ("search",           ["coding"], SurfaceContentFactory.Instances.Many),
            ("codeviewer",       ["architecture", "coding"], SurfaceContentFactory.Instances.Many),
            ("diagnostics",      ["coding"], SurfaceContentFactory.Instances.One),
            ("canvas",           ["architecture"], SurfaceContentFactory.Instances.One),
            ("view",             ["architecture"], SurfaceContentFactory.Instances.One),
            ("inspector",        ["architecture"], SurfaceContentFactory.Instances.One),
            ("classdiagram",     ["architecture"], SurfaceContentFactory.Instances.Many),
            ("sequence",         ["architecture"], SurfaceContentFactory.Instances.Many),
            ("contexts",         ["architecture"], SurfaceContentFactory.Instances.One),
            ("joins",            ["architecture"], SurfaceContentFactory.Instances.One),
        };

        Assert.Equal(expected.Length, SurfaceContentFactory.Kinds.Count);

        foreach (var (kind, admitting, instances) in expected)
        {
            var row = Assert.Single(SurfaceContentFactory.Kinds, k => k.Kind == kind);
            Assert.Equal(admitting, row.Perspectives.Select(p => p.Id).OrderBy(id => id, StringComparer.Ordinal));
            Assert.Equal(instances, row.Instances);
        }
    }

    // A kind whose door is a catalog entry verb names one that exists and is offered everywhere;
    // every other kind derives its entry under one of the six menus. Stated on the row, as a closed
    // type, so an absence cannot hide a kind.
    [Fact]
    public void EveryEntryVerbRowNamesAGlobalCatalogCommand_AndEveryOtherRowDerivesItsEntry()
    {
        var verbs = SurfaceContentFactory.Kinds.Where(k => k.Entry is SurfaceContentFactory.SurfaceEntry.Verb).ToList();
        Assert.Equal(["terminal", "session-document"], verbs.Select(k => k.Kind));

        foreach (var row in verbs)
        {
            var verb = (SurfaceContentFactory.SurfaceEntry.Verb)row.Entry;
            var command = Assert.Single(WorkbenchCommandCatalog.All, c => c.Id == verb.CommandId);
            Assert.Equal(CommandScope.Global, command.Scope);
            Assert.Equal("_File", command.Menu);
            Assert.Throws<ArgumentException>(() => PerspectiveMenu.Opener(row));
        }

        foreach (var row in SurfaceContentFactory.Kinds.Where(k => k.Entry is SurfaceContentFactory.SurfaceEntry.Derived))
        {
            Assert.Contains(((SurfaceContentFactory.SurfaceEntry.Derived)row.Entry).Menu, PerspectiveMenu.MenuOrder);
        }
    }

    // ── US-C4: the §B3 literal-table oracle, through the real builder ─────────────────────

    // "Given each perspective in turn, when the menu is built, then it equals the literal expected
    // table in §B3 for that perspective (entries and their menus)." Rendered through
    // MainMenuBuilder (E11) with a test-built controller and no Exit/Recent hand-offs, so the File
    // column is the catalog's entries only. Top-level names per PS-M1.
    //
    // Deviations from the spec's table as printed, recorded in the Proof Pack: the File entries
    // carry the catalog's titles ("New session…" keeps its ellipsis — it opens a sheet); the derived
    // titles are "<Verb> <title>" in the catalog's own voice (US-C4's text: "New prompt draft",
    // "New sequence diagram"), where the table capitalised the noun; and "Focus graph canvas" is
    // absent from Explore because the seam it runs is bound to a DOCKED canvas (PS-M3).
    [Fact]
    public void TheRenderedMenuEqualsTheSpecsLiteralTable_ForEveryPerspective() => OnSta(() =>
    {
        // Coding: all six menus.
        var (coding, _) = Render(PerspectiveSet.Coding);
        Assert.Equal(["_File", "_Edit", "_View", "_Window", "_Prompt", "_Help"], Headers(coding));
        Assert.Equal(
            ["New session…", "New terminal", "New Claude Code session", "New GitHub Copilot session",
             "Open a repository as a workspace…", "Index C# projects in this workspace",
             "Re-index everything (ignore the cache)", "Re-index this workspace"],
            Titles(coding, "_File"));
        Assert.Equal(
            ["Coding perspective", "Explore perspective", "Architecture perspective",
             "Next tab in pane", "Previous tab in pane", "Move tab left/right",
             "Raise score dispute on the latest scored episode", "Clear the status message",
             "Show terminal sessions", "Show message board", "Show leaderboard", "Show ledger", "Show daydreams",
             "New search", "New code viewer", "Show diagnostics"],
            Titles(coding, "_View"));
        Assert.Equal(["Dispatch prompt to terminal…", "New prompt draft"], Titles(coding, "_Prompt"));
        Assert.Equal(["Move pane…", "Resize pane…"], Titles(coding, "_Edit"));
        Assert.Equal(
            ["Float pane", "Collapse pane", "Maximize pane", "Close surface", "Lock/unlock layout", "Reset workbench layout"],
            Titles(coding, "_Window"));
        Assert.Equal(["Diagnostics report"], Titles(coding, "_Help"));

        // Explore: File · View · Help — not a host, so no Edit, no Window, no Prompt; no derived rows.
        var (explore, _) = Render(PerspectiveSet.Explore);
        Assert.Equal(["_File", "_View", "_Help"], Headers(explore));
        Assert.Equal(Titles(coding, "_File"), Titles(explore, "_File"));
        Assert.Equal(
            ["Coding perspective", "Explore perspective", "Architecture perspective", "Clear the status message"],
            Titles(explore, "_View"));

        // Architecture: all but Prompt; the reading kinds' derived rows in §B3's order.
        var (architecture, _) = Render(PerspectiveSet.Architecture);
        Assert.Equal(["_File", "_Edit", "_View", "_Window", "_Help"], Headers(architecture));
        Assert.Equal(Titles(coding, "_File"), Titles(architecture, "_File"));
        Assert.Equal(
            ["Coding perspective", "Explore perspective", "Architecture perspective",
             "Next tab in pane", "Previous tab in pane", "Move tab left/right",
             "Focus graph canvas", "Clear the status message",
             "Show graph", "Show evidence", "Show provenance", "New class diagram", "New sequence diagram",
             "Show contexts", "Show joins", "New code viewer"],
            Titles(architecture, "_View"));
        Assert.Equal(Titles(coding, "_Edit"), Titles(architecture, "_Edit"));
        Assert.Equal(Titles(coding, "_Window"), Titles(architecture, "_Window"));
    });

    // PS-M1 / US-C1 b3: the perspective entries are a radio group with the active one checked and
    // drawn (the accent check glyph in the icon column — the app's menu template renders no check
    // of its own), the Toggle state exposed to the automation tree from IsChecked, and activating the
    // active one through WPF's own click pipeline (the mouse, the keyboard and UIA Invoke all share
    // it) does NOT un-check it — no Unchecked event, the check stays — while the menu still closes
    // and nothing keeps mouse capture (the UX & Accessibility reviewer's round-2 finding: an
    // OnClick override that skipped PreviewClick left the popup open in menu mode).
    [Fact]
    public void ThePerspectiveEntriesAreARadioGroup_WithTheActiveOneCheckedAndDrawn_AndAClickNeverTogglesIt() => OnSta(() =>
    {
        foreach (var perspective in PerspectiveSet.All)
        {
            var (menu, announcer) = Render(perspective);
            var window = new Window { Content = menu, Width = 600, Height = 200, WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false };
            window.Show();

            try
            {
                var radio = Items(menu).OfType<MainMenuBuilder.PerspectiveMenuItem>().ToList();

                Assert.Equal(PerspectiveSet.All.Select(p => $"{p.Title} perspective"), radio.Select(i => i.Header?.ToString()));
                Assert.Equal([perspective.Title + " perspective"], radio.Where(i => i.IsChecked).Select(i => i.Header?.ToString()));
                Assert.All(radio, i => Assert.False(i.IsCheckable, "a checkable item is toggled by WPF's own click"));

                // Drawn: the active row carries the check glyph, no other row does.
                Assert.Equal(
                    [perspective.Title + " perspective"],
                    radio.Where(i => i.Icon is System.Windows.Shapes.Path { Tag: MainMenuBuilder.CheckTag }).Select(i => i.Header?.ToString()));

                // Exposed: every row offers the Toggle pattern; its state is the check.
                foreach (var item in radio)
                {
                    var toggle = Assert.IsAssignableFrom<IToggleProvider>(UIElementAutomationPeer.CreatePeerForElement(item).GetPattern(PatternInterface.Toggle));
                    Assert.Equal(item.IsChecked ? ToggleState.On : ToggleState.Off, toggle.ToggleState);
                }

                var active = radio.Single(i => i.IsChecked);
                var view = menu.Items.OfType<MenuItem>().First(m => Equals(m.Header, "_View"));
                var uncheckedEvents = 0;
                active.Unchecked += (_, _) => uncheckedEvents++;

                view.IsSubmenuOpen = true;
                Pump();
                Assert.True(view.IsSubmenuOpen);

                var invoke = Assert.IsAssignableFrom<IInvokeProvider>(UIElementAutomationPeer.CreatePeerForElement(active).GetPattern(PatternInterface.Invoke));
                invoke.Invoke();
                Pump();

                Assert.True(active.IsChecked, "activating the active perspective un-checked its menu item");
                Assert.Equal(0, uncheckedEvents);
                Assert.Contains(perspective.Title, announcer.Last, StringComparison.Ordinal);   // the click ran the command
                Assert.False(view.IsSubmenuOpen, "the View menu stayed open after a perspective entry was activated");
                Assert.Null(Mouse.Captured);
            }
            finally
            {
                window.Close();
            }
        }
    });

    private static void Pump() =>
        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

    // PS-M4 on the rendered menu: the perspective items show Ctrl+1/2/3; a derived opener shows nothing.
    [Fact]
    public void ThePerspectiveItemsShowTheirBoundGesture_AndDerivedOpenersShowNone() => OnSta(() =>
    {
        var (menu, _) = Render(PerspectiveSet.Coding);
        var items = Items(menu).ToList();

        Assert.Equal("Ctrl+1", items.Single(i => Equals(i.Header, "Coding perspective")).InputGestureText);
        Assert.Equal("Ctrl+2", items.Single(i => Equals(i.Header, "Explore perspective")).InputGestureText);
        Assert.Equal("Ctrl+3", items.Single(i => Equals(i.Header, "Architecture perspective")).InputGestureText);
        Assert.Equal("Ctrl+N", items.Single(i => Equals(i.Header, "New session…")).InputGestureText);
        Assert.Equal(string.Empty, items.Single(i => Equals(i.Header, "Show daydreams")).InputGestureText);
        Assert.Equal(string.Empty, items.Single(i => Equals(i.Header, "New terminal")).InputGestureText);   // Ctrl+K, T: announced, unbound
    });

    // ── US-C4 b3: the palette's rows are exactly the menu's, in every perspective ─────────

    [Fact]
    public void ThePaletteRowsEqualTheMenusCommands_InEveryPerspective() => OnSta(() =>
    {
        var announcer = new RecordingAnnouncer();
        var palette = new CommandPalette(new WorkbenchController(new ZoneBackedLayoutService(), announcer), announcer);

        foreach (var perspective in PerspectiveSet.All)
        {
            var derived = PerspectiveMenu.For(perspective);
            palette.Menu = derived;

            // The rendered bar against the palette — two projections of one model must agree (E12).
            Assert.Equal(
                Items(Render(perspective, derived).Menu).Select(i => i.Header?.ToString()),
                palette.Visible.Select(c => c.Title));
        }

        palette.Menu = PerspectiveMenu.For(PerspectiveSet.Coding);
        Assert.DoesNotContain(palette.Visible, c => c.Id == "surface.new.classdiagram");   // the falsifier US-C4 names
        palette.Menu = PerspectiveMenu.For(PerspectiveSet.Architecture);
        Assert.Contains(palette.Visible, c => c.Id == "surface.new.classdiagram");
        Assert.DoesNotContain(palette.Visible, c => c.Id == "surface.new.prompt");
    });

    // PS-M4, US-C10 b3: the palette speaks a keystroke only when one is bound.
    [Fact]
    public void ThePaletteSpeaksAKeystrokeOnlyWhenOneIsBound() => OnSta(() =>
    {
        var announcer = new RecordingAnnouncer();
        var palette = new CommandPalette(new WorkbenchController(new ZoneBackedLayoutService(), announcer), announcer);
        var window = new Window { Content = palette.Root, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false };
        window.Show();

        try
        {
            palette.Open();
            palette.SearchBox.Text = "daydreams";
            palette.HandleKey(Key.Down);   // wraps onto the one row, announcing it
            Assert.StartsWith("Show daydreams. Shows the daydreams pane", announcer.Last, StringComparison.Ordinal);
            Assert.DoesNotContain("Ctrl", announcer.Last, StringComparison.Ordinal);

            palette.SearchBox.Text = "Architecture perspective";
            palette.HandleKey(Key.Down);
            Assert.StartsWith("Architecture perspective. Ctrl+3. ", announcer.Last, StringComparison.Ordinal);

            palette.SearchBox.Text = "New terminal";
            palette.HandleKey(Key.Down);
            Assert.StartsWith("New terminal. Opens a plain shell terminal", announcer.Last, StringComparison.Ordinal);   // Ctrl+K, T is not bound
        }
        finally
        {
            window.Close();
        }
    });

    // ── US-C4: the mutation test ──────────────────────────────────────────────────────────

    // A test-time kind row admitted only by Architecture is appended to the kinds list. Its "New
    // <title>" entry appears in Architecture's View menu and palette and nowhere else — with NO edit
    // to the builder, the palette or the controller's switch. A per-perspective tuple list cannot
    // pass this; the join does.
    [Fact]
    public void ATestTimeKindRowAdmittedOnlyByArchitecture_AppearsThereAndNowhereElse() => OnSta(() =>
    {
        var row = new Kind(
            "testkind", "Test kind", "A kind that exists only in this test.",
            static (_, s) => new TextBlock { Text = s.Title },
            Perspectives: [PerspectiveSet.Architecture],
            SurfaceContentFactory.Instances.Many,
            new SurfaceContentFactory.SurfaceEntry.Derived("_View"));
        IReadOnlyList<Kind> kinds = [.. SurfaceContentFactory.Kinds, row];

        var announcer = new RecordingAnnouncer();
        var palette = new CommandPalette(new WorkbenchController(new ZoneBackedLayoutService(), announcer), announcer);

        foreach (var perspective in PerspectiveSet.All)
        {
            var derived = PerspectiveMenu.For(perspective, kinds, WorkbenchCommandCatalog.All);
            var (menu, _) = Render(perspective, derived);
            palette.Menu = derived;

            var expected = perspective == PerspectiveSet.Architecture;
            Assert.Equal(expected, Titles(menu, "_View").Contains("New test kind"));
            Assert.Equal(expected, palette.Visible.Any(c => c.Id == "surface.new.testkind"));
        }

        // And the product's own list is untouched: the seam is a parameter, not a mutation.
        Assert.DoesNotContain(SurfaceContentFactory.Kinds, k => k.Kind == "testkind");
    });

    // ── US-C3: the routing table ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("codeviewer", "explore", "architecture")]     // the reading host wins a shared kind
    [InlineData("codeviewer", "coding", "coding")]            // the active perspective admits it: stays
    [InlineData("codeviewer", "architecture", "architecture")]
    [InlineData("sequence", "coding", "architecture")]
    [InlineData("sequence", "explore", "architecture")]
    [InlineData("terminal", "architecture", "coding")]
    [InlineData("terminal", "explore", "coding")]
    [InlineData("daydreams", "architecture", "coding")]
    [InlineData("canvas", "coding", "architecture")]
    public void Resolve_RoutesAnInadmissibleKindOpen_ArchitectureThenCoding(string kind, string active, string expected)
    {
        var resolved = PerspectiveMenu.Resolve(kind, ById(active));

        Assert.NotNull(resolved);
        Assert.Equal(expected, resolved!.Id);
    }

    // The law over every (kind × active) cell, not nine of them (D2): the result admits the kind;
    // it is the active perspective iff the active perspective admits the kind; otherwise it is the
    // first of the routing order that does.
    [Fact]
    public void Resolve_ObeysItsLaw_OverEveryKindAndEveryPerspective()
    {
        var cells = 0;
        foreach (var row in SurfaceContentFactory.Kinds)
        {
            foreach (var active in PerspectiveSet.All)
            {
                cells++;
                var resolved = PerspectiveMenu.Resolve(row.Kind, active);
                Assert.NotNull(resolved);
                Assert.Contains(resolved!, row.Perspectives);
                Assert.Equal(row.Perspectives.Contains(active), ReferenceEquals(resolved, active));
                if (!row.Perspectives.Contains(active))
                {
                    Assert.Same(PerspectiveSet.RoutingOrder.First(p => row.Perspectives.Contains(p)), resolved);
                }
            }
        }

        Assert.Equal(SurfaceContentFactory.Kinds.Count * PerspectiveSet.All.Count, cells);
    }

    // A kind no perspective admits resolves to nothing — the caller reports, never guesses. The
    // build test above makes this unreachable for the product's rows; the function stays total.
    [Fact]
    public void Resolve_ReturnsNullForAKindNoPerspectiveAdmits()
    {
        Assert.Null(PerspectiveMenu.Resolve("no-such-kind", PerspectiveSet.Coding));
    }

    // ── The derived opener ids are the controller's contract ──────────────────────────────

    // The round-trip law (D2): every derived opener's id parses back to its row's verb and kind.
    [Fact]
    public void TryParseOpener_RoundTripsEveryDerivedOpener()
    {
        var derived = SurfaceContentFactory.Kinds.Where(k => k.Entry is SurfaceContentFactory.SurfaceEntry.Derived).ToList();
        Assert.True(derived.Count >= 16);

        foreach (var row in derived)
        {
            Assert.True(PerspectiveMenu.TryParseOpener(PerspectiveMenu.Opener(row).Id, out var show, out var kind));
            Assert.Equal(row.Instances == SurfaceContentFactory.Instances.One, show);
            Assert.Equal(row.Kind, kind);
        }
    }

    [Theory]
    [InlineData("surface.show.sessions", true, "sessions")]
    [InlineData("surface.new.classdiagram", false, "classdiagram")]
    public void TryParseOpener_SplitsADerivedIdIntoVerbAndKind(string id, bool show, string kind)
    {
        Assert.True(PerspectiveMenu.TryParseOpener(id, out var isShow, out var parsed));
        Assert.Equal(show, isShow);
        Assert.Equal(kind, parsed);
    }

    [Theory]
    [InlineData("surface.show.")]
    [InlineData("surface.new.")]
    [InlineData("workbench.moveSurface")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParseOpener_RejectsAnythingElse_AndNeverThrows(string? id)
    {
        Assert.False(PerspectiveMenu.TryParseOpener(id, out _, out _));
    }

    // ── The presenter and the shell together: a document opens where the operator is ─────

    // The Test Architect's finding, as a control: with the window's own handler
    // (MainWindow.OnDocumentOpening — a host on screen → stay; a full-window body → the initial host
    // returns, INV-0009), Architecture's derived openers leave Architecture active, raise no
    // ModeChanged and write no shell.mode line — and Explore's still return to Coding. RED before
    // the guard: every open switched to Coding.
    [Fact]
    public void ADerivedOpenerInAHostPerspective_DoesNotSwitchThePerspective_ButFromExploreTheHostReturns()
    {
        var lines = new List<string>();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = lines.Add;

        try
        {
            OnSta(() =>
            {
                var shell = new WorkbenchShell(queries: null);
                var window = new Window
                {
                    Content = new ContentControl(),
                    Width = 1000, Height = 640, WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false,
                };
                var host = (ContentControl)window.Content;
                var mode = new ShellModeController(host, shell.Manager, () => new Grid());
                shell.DocumentOpening += () => MainWindow.OnDocumentOpening(mode);   // the product's rule, not a copy
                window.Show();

                try
                {
                    mode.Set(PerspectiveSet.Architecture, "test");
                    var switches = 0;
                    mode.ModeChanged += (_, _) => switches++;
                    var linesBefore = lines.Count(l => l.Contains("\"evt\":\"shell.mode\"", StringComparison.Ordinal));

                    Assert.True(shell.Controller.Execute("surface.new.classdiagram"));
                    Assert.Equal("Class diagram opened.", shell.Announcer.Last);
                    Assert.Same(PerspectiveSet.Architecture, mode.Mode);
                    Assert.Equal(0, switches);
                    Assert.Equal(linesBefore, lines.Count(l => l.Contains("\"evt\":\"shell.mode\"", StringComparison.Ordinal)));

                    mode.Set(PerspectiveSet.Explore, "test");
                    Assert.True(shell.Controller.Execute("surface.new.codeviewer"));
                    Assert.Same(PerspectiveSet.Coding, mode.Mode);   // INV-0009: the document opens into a body on screen
                }
                finally
                {
                    window.Close();
                }
            });
        }
        finally
        {
            WorkbenchDiagnostics.Sink = previous;
        }
    }

    // A derived id naming a kind that is not a row is answered honestly, not thrown; and the three
    // node-menu literals the shell opens by kind are rows (the realistic stale-id path).
    [Fact]
    public void AStaleDerivedId_IsAnsweredHonestly_AndTheNodeMenusKindsAreRows()
    {
        var said = Sta.Run(() =>
        {
            var shell = new WorkbenchShell(queries: null);
            Assert.True(shell.Controller.Execute("surface.new.no-such-kind"));
            return shell.Announcer.Last;
        }, 60);

        Assert.Equal("There is no 'no-such-kind' surface in this build.", said);

        foreach (var kind in new[] { "codeviewer", "classdiagram", "sequence" })
        {
            Assert.Contains(SurfaceContentFactory.Kinds, k => k.Kind == kind);
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
