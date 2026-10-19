using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The presenter and router over ADR-0045's three docking hosts (ADR-0031 falsifying tests 1, 3 and
/// 4's shell half; ADR-0017 as amended; US-C1, US-C2, US-C12). Hosts are composed the way the shell
/// composes them — <see cref="DockHost.Create"/> over a stub content factory — never a
/// <c>Border</c> stand-in for a host (DC-135).
/// </summary>
public sealed class PerspectiveShellTests
{
    private sealed class RecordingAnnouncer : IWorkbenchAnnouncer
    {
        public List<string> All { get; } = [];

        public string Last => All.LastOrDefault() ?? string.Empty;

        public void Announce(string message) => All.Add(message);

        public void Clear() => All.Add(string.Empty);
    }

    private sealed record Fixture(PerspectiveShell Shell, ContentControl Body, DockHost A, DockHost B, DockHost C, RecordingAnnouncer Said, Func<int> ContentBuilds);

    // Host A is Factory, host B Understand, host C Review — the three rows of ADR-0045 §1(a).
    private static Fixture Build(IWorkbenchAnnouncer? hostAnnouncer = null)
    {
        var said = new RecordingAnnouncer();
        var contentBuilds = 0;
        FrameworkElement Content(Surface _) { contentBuilds++; return new Border(); }
        var a = DockHost.Create(PerspectiveSet.Factory, Content, hostAnnouncer ?? said, (_, _) => { });
        var b = DockHost.Create(PerspectiveSet.Understand, Content, hostAnnouncer ?? said, (_, _) => { });
        var c = DockHost.Create(PerspectiveSet.Review, Content, hostAnnouncer ?? said, (_, _) => { });
        var body = new ContentControl();
        var shell = new PerspectiveShell(body, [a, b, c], said);
        return new Fixture(shell, body, a, b, c, said, () => contentBuilds);
    }

    private static List<JsonElement> ModeLines(List<string> lines, string evt = "shell.mode") =>
        lines.Select(l => JsonDocument.Parse(l).RootElement)
            .Where(e => e.GetProperty("evt").GetString() == evt)
            .ToList();

    private static T WithSink<T>(Func<List<string>, T> body)
    {
        var lines = new List<string>();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = lines.Add;
        try { return body(lines); }
        finally { WorkbenchDiagnostics.Sink = previous; }
    }

    // ADR-0031 test 1 / US-C2 over ADR-0045's three hosts — cycling Factory → Review → Understand →
    // Factory → Understand → Review shows reference-identical objects for every body; no host's
    // content factory runs for a switch (a switch only unparents — a rebuilt pane would run it
    // again); each host's body is ITS OWN root, and its announcement counts ITS default's panes
    // (Review's default is empty: nothing until WL-5).
    [Fact]
    public void CyclingThreeHosts_ReturnsTheSameInstances_AndInvokesNoFactoryTwice()
    {
        Sta.Run(() =>
        {
            var f = Build();
            Assert.Same(f.A.Root, f.Body.Content);

            // Render every host once, as the shell does at construction, so the content factory has
            // run for each pane exactly once before the cycle begins.
            f.A.Adapter.Render();
            f.B.Adapter.Render();
            f.C.Adapter.Render();
            var builtBefore = f.ContentBuilds();
            Assert.True(builtBefore > 0, "the stub content factory never ran — the fixture rendered nothing (DC-016)");

            var announced = f.Shell.Activate(PerspectiveSet.Review, "test");
            Assert.Same(f.C.Root, f.Body.Content);
            Assert.NotSame(f.A.Root, f.C.Root);
            Assert.NotSame(f.B.Root, f.C.Root);
            Assert.Equal("Review perspective — 0 panes.", announced);

            Assert.Equal("Understand perspective — 2 panes.", f.Shell.Activate(PerspectiveSet.Understand, "test"));
            Assert.Same(f.B.Root, f.Body.Content);

            f.Shell.Activate(PerspectiveSet.Factory, "test");
            Assert.Same(f.A.Root, f.Body.Content);

            f.Shell.Activate(PerspectiveSet.Understand, "test");
            Assert.Same(f.B.Root, f.Body.Content);            // the same host B instance returns

            f.Shell.Activate(PerspectiveSet.Review, "test");
            Assert.Same(f.C.Root, f.Body.Content);
            Assert.Same(f.C, f.Shell.ActiveHost);
            Assert.Same(f.C, f.Shell.HostFor(PerspectiveSet.Review));

            Assert.Equal(builtBefore, f.ContentBuilds());      // no factory invoked twice for a switch

            // Three host services, three controllers — no two the same object.
            Assert.Equal(3, new[] { f.A.Service, f.B.Service, f.C.Service }.Distinct().Count());
            Assert.Equal(3, new[] { f.A.Controller, f.B.Controller, f.C.Controller }.Distinct().Count());
            Assert.Equal([PerspectiveSet.Factory, PerspectiveSet.Understand, PerspectiveSet.Review], new[] { f.A.Row, f.B.Row, f.C.Row });
            return 0;
        }, 30);
    }

    // Spec §C5 / DESIGN.md's landing row (Ruling 83; ADR-0045 §1(a)): a switch to Understand lands
    // on the Center's active tab (Graph), to Factory on the Left's active tab — the session document
    // (SH-4.2) — else the Center's active tab, which for an empty Center is its empty copy; Review's
    // Center is empty until WL-5, so its landing is the empty copy. A landing zone the arrangement
    // has emptied or collapsed falls back the same way. Stated on the row because the view's own
    // "active" after a body is first parented is whichever pane control realized last.
    [Fact]
    public void TheLanding_IsTheRowsZonesActiveTab_LeftForFactory_CenterForUnderstandAndReview_ElseTheEmptyCopy()
    {
        Sta.Run(() =>
        {
            var f = Build();
            f.A.Adapter.Render();
            f.B.Adapter.Render();
            f.C.Adapter.Render();

            Assert.Equal("graph", PerspectiveShell.LandingSurfaceFor(f.B));     // Ruling 140: the Center's active tab is the Graph
            Assert.Equal(ZonesToTree.WelcomePlaceholder.SurfaceId, PerspectiveShell.LandingSurfaceFor(f.C));

            // Factory (SH-4.2): the Left's active document when one is open, else the Center's
            // empty copy — the placeholder's id, whose content the shell composes.
            Assert.Equal(ZonesToTree.WelcomePlaceholder.SurfaceId, PerspectiveShell.LandingSurfaceFor(f.A));
            Assert.True(f.A.Service.Apply(new LayoutOperation.AddSurface(ZonesToTree.LeftStackId, new Surface("session:s1", "session-document", "S1"))).Applied);
            f.A.Adapter.Render();
            Assert.Equal("session:s1", PerspectiveShell.LandingSurfaceFor(f.A));

            Assert.Equal(ZoneId.Left, f.A.Row.Landing);
            Assert.Equal(ZoneId.Center, f.B.Row.Landing);
            Assert.Equal(ZoneId.Center, f.C.Row.Landing);

            // The landing follows the zone's ACTIVE tab, not its first: activate the second Center
            // tab in Understand and the landing moves with it (Ruling 140's Tree).
            Assert.True(f.B.Service.Apply(new LayoutOperation.ActivateSurface("tree")).Applied);
            Assert.Equal("tree", PerspectiveShell.LandingSurfaceFor(f.B));

            // And in Factory, the Left collapsed with the session held: the Center's empty copy again.
            Assert.True(f.A.Service.Apply(new LayoutOperation.SetStackState(ZonesToTree.LeftStackId, StackState.Collapsed)).Applied);
            f.A.Adapter.Render();
            Assert.Equal(ZonesToTree.WelcomePlaceholder.SurfaceId, PerspectiveShell.LandingSurfaceFor(f.A));
            return 0;
        }, 30);
    }

    // SH-4.1 re-run on the Factory host (ADR-0045 §1(b); the WL-S design's §3 oracle 1). Folding the
    // five Loomkeeper kinds into Factory is an occupancy change: its Center can now hold Ledger ·
    // Leaderboard · Message board beside a session document in its Left, and the view's own "active"
    // is whichever pane realized or was activated last. The landing is the row's data, not that: the
    // session document; with the Left collapsed, the Center's active tab the operator chose. Through
    // the product's DockHost and admission (DC-135). RED before the re-row: the initial host refused
    // the ledger (sp3/sh41-factory-red.txt).
        // The Factory → Coordination group, derived from what the product admits: Factory's Center kinds
    // opened from the derived View menu (the session Console is a verb, not a menu entry).
    private static IEnumerable<(string Kind, string Title)> LoomkeeperKinds() =>
        SurfaceContentFactory.Kinds
            .Where(k => k.Perspectives.Contains(PerspectiveSet.Factory) && k.Zone == ZoneId.Center && k.Entry is SurfaceContentFactory.SurfaceEntry.Derived)
            .Select(k => (k.Kind, k.Title));

    [Fact]
    public void TheFactoryLanding_IsTheSessionDocument_WhenTheCoordinationKindsOccupyItsCenter()
    {
        Sta.Run(() =>
        {
            var factory = DockHost.Create(PerspectiveSet.Initial, _ => new Border(), new RecordingAnnouncer(), (_, _) => { });
            Assert.True(factory.Service.Apply(new LayoutOperation.AddSurface(ZonesToTree.LeftStackId, new Surface("session:s1", "session-document", "S1"))).Applied);
            foreach (var (kind, title) in LoomkeeperKinds())
            {
                Assert.True(
                    factory.Service.Apply(new LayoutOperation.AddSurface(ZonesToTree.CenterStackId, new Surface(kind, kind, title))).Applied,
                    $"the initial host refused '{kind}' — the Loomkeeper kinds are not admitted under Factory");
            }

            factory.Adapter.Render();
            Assert.Equal("session:s1", PerspectiveShell.LandingSurfaceFor(factory));

            // The view's notion of active moves to the Center's ledger (as a later-realizing pane
            // control would make it); the landing does not follow it.
            Assert.True(factory.Service.Apply(new LayoutOperation.ActivateSurface("ledger")).Applied);
            factory.Adapter.Render();
            factory.Adapter.ActivateInView("ledger");
            Assert.Equal("ledger", factory.Adapter.ActiveSurfaceId);
            Assert.Equal("session:s1", PerspectiveShell.LandingSurfaceFor(factory));

            // The Left collapsed with the session held: the Center's active tab, literally the ledger.
            Assert.True(factory.Service.Apply(new LayoutOperation.SetStackState(ZonesToTree.LeftStackId, StackState.Collapsed)).Applied);
            factory.Adapter.Render();
            Assert.Equal("ledger", PerspectiveShell.LandingSurfaceFor(factory));
            Assert.Equal("factory", factory.Row.Id);
            return 0;
        }, 30);
    }

    // The UX & Accessibility lens's finding (SH-4.1), re-run where the Loomkeeper kinds now live
    // (ADR-0045 §1(b)): each of the five, opened in Factory's Center in its first-run state — no
    // watcher store, every pane in its not-available state — must carry a focus target, or a landing
    // that falls back to the Center's active tab falls through to an arbitrary tab header. Through the
    // product's factory (no queries, no watcher) and the product's host, as the shell composes it
    // (DC-135).
    [Fact]
    public void TheLoomkeeperPanes_HaveAFocusTarget_InFactorysFirstRunState()
    {
        Sta.Run(() =>
        {
            var factory = new SurfaceContentFactory(null);
            var host = DockHost.Create(PerspectiveSet.Factory, factory.Create, new RecordingAnnouncer(), (_, _) => { });
            foreach (var (kind, title) in LoomkeeperKinds())
            {
                Assert.True(host.Service.Apply(new LayoutOperation.AddSurface(ZonesToTree.CenterStackId, new Surface(kind, kind, title))).Applied, kind);
            }

            host.Adapter.Render();

            foreach (var surface in host.Service.Zones.Zone(ZoneId.Center).Surfaces())
            {
                host.Adapter.ActivateInView(surface.SurfaceId);
                var pane = host.Adapter.ContentFor(surface.SurfaceId);
                Assert.True(pane is not null && HasFocusTarget(pane), $"'{surface.SurfaceId}' has no focus target in its not-available state");
            }

            return 0;
        }, 30);

        static bool HasFocusTarget(DependencyObject element)
        {
            if (element is UIElement { Focusable: true, IsEnabled: true })
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

            return element is ContentControl { Content: DependencyObject inner } && HasFocusTarget(inner)
                || element is System.Windows.Controls.Decorator { Child: DependencyObject child } && HasFocusTarget(child)
                || element is Panel panel && panel.Children.OfType<DependencyObject>().Any(HasFocusTarget);
        }
    }

    // ADR-0032 test 4's second clause / US-C9 — switching perspective never resets another host's
    // arrangement: each host's shape is byte-identical after a cycle through all three.
    [Fact]
    public void CyclingThePerspectives_LeavesEveryHostsArrangementUntouched()
    {
        Sta.Run(() =>
        {
            var f = Build();
            f.A.Service.Apply(new LayoutOperation.SetStackState(ZonesToTree.LeftStackId, StackState.Collapsed));
            f.B.Service.Apply(new LayoutOperation.AddSurface(ZonesToTree.RightStackId, new Surface("seq#1", "sequence", "Sequence diagram")));
            f.B.Service.Apply(new LayoutOperation.ResizeSplit(ZonesToTree.RootSplitId, 0, -0.05));
            var a = f.A.Service.Zones.Shape();
            var b = f.B.Service.Zones.Shape();
            var extentsA = Extents(f.A);
            var extentsB = Extents(f.B);

            foreach (var target in new[] { PerspectiveSet.Understand, PerspectiveSet.Review, PerspectiveSet.Factory, PerspectiveSet.Understand, PerspectiveSet.Factory })
            {
                f.Shell.Activate(target, "test");
            }

            Assert.Equal(a, f.A.Service.Zones.Shape());
            Assert.Equal(b, f.B.Service.Zones.Shape());
            Assert.Equal(extentsA, Extents(f.A));   // Shape() ignores extents; a reset splitter would hide here
            Assert.Equal(extentsB, Extents(f.B));
            return 0;
        }, 30);

        static List<double> Extents(DockHost host) => [.. Enum.GetValues<ZoneId>().Select(z => host.Service.Zones.Zone(z).Extent)];
    }

    // US-C1 — activating the active perspective is a no-op that emits nothing, however often.
    // (The previous-perspective slot existed for Escape-from-Explore only and left with the
    // full-window body — the WL-S design's D4.)
    [Fact]
    public void ActivatingTheActivePerspective_IsANoOp_AndEmitsNothing()
    {
        WithSink(lines => Sta.Run(() =>
        {
            var f = Build();
            var changes = new List<PerspectiveChange>();
            f.Shell.Changed += (_, c) => changes.Add(c);

            var said = f.Shell.Activate(PerspectiveSet.Factory, "test");     // already active
            Assert.Empty(changes);
            Assert.Empty(ModeLines(lines));
            Assert.Contains("already showing", said, StringComparison.Ordinal);

            f.Shell.Activate(PerspectiveSet.Understand, "test");
            f.Shell.Activate(PerspectiveSet.Understand, "test");          // no-op again
            f.Shell.Activate(PerspectiveSet.Factory, "test");

            Assert.Equal(2, changes.Count);
            Assert.Equal([(PerspectiveSet.Factory, PerspectiveSet.Understand), (PerspectiveSet.Understand, PerspectiveSet.Factory)], changes.Select(c => (c.From, c.To)));
            Assert.Equal(2, ModeLines(lines).Count);
            return 0;
        }, 30));
    }

    // ADR-0031 test 3 over ADR-0045's rows — routing: a host-scoped command reaches the ACTIVE
    // host's controller only (a recording announcer per host); a Global entry verb reaches host A
    // (Factory) whatever is active; a derived opener reaches the host that admits its kind — the
    // active one when it does, else Understand → Review → Factory.
    [Fact]
    public void Execute_RoutesHostCommandsToTheActiveHost_EntryVerbsToHostA_AndOpenersByKind()
    {
        Sta.Run(() =>
        {
            var saidA = new RecordingAnnouncer();
            var saidB = new RecordingAnnouncer();
            var saidC = new RecordingAnnouncer();
            var a = DockHost.Create(PerspectiveSet.Factory, _ => new Border(), saidA, (_, _) => { });
            var b = DockHost.Create(PerspectiveSet.Understand, _ => new Border(), saidB, (_, _) => { });
            var c = DockHost.Create(PerspectiveSet.Review, _ => new Border(), saidC, (_, _) => { });
            var opened = new List<(string Host, string Kind)>();
            a.Controller.OpenSurfaceRequested = (kind, _) => { opened.Add(("A", kind)); return "opened"; };
            b.Controller.OpenSurfaceRequested = (kind, _) => { opened.Add(("B", kind)); return "opened"; };
            c.Controller.OpenSurfaceRequested = (kind, _) => { opened.Add(("C", kind)); return "opened"; };
            var terminals = 0;
            a.Controller.NewTerminalRequested = () => { terminals++; return "Terminal opened."; };
            var shell = new PerspectiveShell(new ContentControl(), [a, b, c], new RecordingAnnouncer());

            shell.Activate(PerspectiveSet.Understand, "test");
            Assert.True(shell.Execute("workbench.moveSurface"));
            Assert.Contains("Move pane", saidB.Last, StringComparison.Ordinal);      // host B's controller
            Assert.Empty(saidA.All);                                                  // and not host A's

            Assert.True(shell.Execute("terminal.new"));                               // an entry verb: host A
            Assert.Equal(1, terminals);

            Assert.True(shell.Execute("surface.new.codeviewer"));                     // shared kind: the ACTIVE host
            Assert.Equal(("B", "codeviewer"), opened[^1]);
            Assert.True(shell.Execute("surface.show.ledger"));                        // a Loomkeeper kind: Factory's (ADR-0045 §1(b))
            Assert.Equal(("A", "ledger"), opened[^1]);

            shell.Activate(PerspectiveSet.Review, "test");                            // Review admits no kind (D3)
            Assert.True(shell.Execute("surface.new.codeviewer"));                     // the reading host wins
            Assert.Equal(("B", "codeviewer"), opened[^1]);
            Assert.True(shell.Execute("workbench.moveSurface"));
            Assert.Contains("Move pane", saidC.Last, StringComparison.Ordinal);      // host C's controller

            shell.Activate(PerspectiveSet.Factory, "test");
            Assert.True(shell.Execute("surface.new.codeviewer"));
            Assert.Equal(("A", "codeviewer"), opened[^1]);
            Assert.True(shell.Execute("surface.new.classdiagram"));                   // Factory does not admit it: routed to B
            Assert.Equal(("B", "classdiagram"), opened[^1]);
            Assert.True(shell.Execute("workbench.moveSurface"));
            Assert.Contains("Move pane", saidA.Last, StringComparison.Ordinal);      // host A's controller now

            Assert.False(shell.Execute("no.such.command"));
            return 0;
        }, 30);
    }

    // The seam, generalised: a document opening in a host whose body is not on screen switches to
    // that host (trigger `document-opening`); one opening where the operator already is does not.
    [Fact]
    public void OnDocumentOpening_SwitchesToTheDocumentsHost_OnlyWhenItIsNotTheBody()
    {
        WithSink(lines => Sta.Run(() =>
        {
            var f = Build();

            f.Shell.OnDocumentOpening(PerspectiveSet.Factory);                // already the body: nothing
            Assert.Empty(ModeLines(lines));

            f.Shell.Activate(PerspectiveSet.Review, "perspective.review");
            f.Shell.OnDocumentOpening(PerspectiveSet.Factory);                // from Review: the host returns
            Assert.Same(PerspectiveSet.Factory, f.Shell.Active);
            Assert.Same(f.A.Root, f.Body.Content);

            f.Shell.OnDocumentOpening(PerspectiveSet.Understand);             // a routed open: host B becomes the body
            Assert.Same(PerspectiveSet.Understand, f.Shell.Active);

            var triggers = ModeLines(lines).Select(e => e.GetProperty("trigger").GetString()).ToList();
            Assert.Equal(["perspective.review", "document-opening", "document-opening"], triggers);
            return 0;
        }, 30));
    }

    // US-C12 — every switch is one structured line with mode, from and trigger, the stable ids of
    // the re-rowed rows.
    [Fact]
    public void EverySwitch_WritesOneShellModeLine_WithItsFullShape()
    {
        WithSink(lines => Sta.Run(() =>
        {
            var f = Build();
            f.Shell.Activate(PerspectiveSet.Review, "perspective.review");
            f.Shell.Activate(PerspectiveSet.Factory, "rail");
            f.Shell.Activate(PerspectiveSet.Understand, "document-opening");

            var modes = ModeLines(lines);
            Assert.Equal(3, modes.Count);

            Assert.Equal(["review", "factory", "understand"], modes.Select(m => m.GetProperty("mode").GetString()));
            Assert.Equal(["factory", "review", "factory"], modes.Select(m => m.GetProperty("from").GetString()));
            Assert.Equal(["perspective.review", "rail", "document-opening"], modes.Select(m => m.GetProperty("trigger").GetString()));
            return 0;
        }, 30));
    }

    // US-C12's stop edge — in a shown window every switch is followed by exactly one
    // shell.mode.shown line carrying a measured duration, and never a second one for the same
    // switch however many layout passes follow: the hook removes itself on the body's first Loaded
    // (the allow-list claim in TheWebSurfacesInitialiseOnceAcrossReparentsTests).
    [Fact]
    public void EveryRetainedSwitch_IsShownOnce_WithAMeasuredDuration()
    {
        WithSink(lines => Sta.Run(() =>
        {
            var f = Build();
            var window = new Window
            {
                Content = f.Body,
                Width = 600, Height = 400, WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false,
            };
            window.Show();
            try
            {
                foreach (var target in new[] { PerspectiveSet.Understand, PerspectiveSet.Factory, PerspectiveSet.Understand })
                {
                    f.Shell.Activate(target, "test");
                    Flush();
                    Flush();   // a second layout pass after the switch must not produce a second line
                }

                var shown = ModeLines(lines, "shell.mode.shown");
                Assert.Equal(3, shown.Count);
                Assert.Equal(["understand", "factory", "understand"], shown.Select(e => e.GetProperty("mode").GetString()).ToList());
                Assert.All(shown, e => Assert.True(e.GetProperty("duration_ms").GetDouble() >= 0));
                Assert.Equal(3, ModeLines(lines).Count);

                // Rapid repeated switching (spec §A10's boundary): a switch superseded before its
                // body's first layout pass is NOT recorded — its stale edge never fires alongside
                // the next entry with an earlier start (IO12: no plausible wrong number).
                lines.Clear();
                f.Shell.Activate(PerspectiveSet.Factory, "t1");
                f.Shell.Activate(PerspectiveSet.Understand, "t2");
                f.Shell.Activate(PerspectiveSet.Factory, "t3");
                Flush();
                Flush();
                f.Shell.Activate(PerspectiveSet.Understand, "t4");
                Flush();
                Flush();

                var rapid = ModeLines(lines, "shell.mode.shown");
                Assert.Equal(["t3", "t4"], rapid.Select(e => e.GetProperty("trigger").GetString()).ToList());
                Assert.Equal(4, ModeLines(lines).Count);   // every switch is still on the switch line
                return 0;
            }
            finally { window.Close(); }
        }, 30));

        static void Flush() =>
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                () => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
    }

    // spec §C5 — focus lands where the window says (the landing zone's active document), once the
    // body has had its first layout pass, and never on the rail: the presenter asks the EntryFocus
    // hook with the perspective and the body it showed.
    [Fact]
    public void AfterASwitch_FocusIsPlacedByTheEntryFocusHook_OnTheBodysFirstLayoutPass()
    {
        Sta.Run(() =>
        {
            var f = Build();
            var placed = new List<(Perspective, FrameworkElement)>();
            f.Shell.EntryFocus = (p, body) => { placed.Add((p, body)); return true; };
            var window = new Window
            {
                Content = f.Body,
                Width = 600, Height = 400, WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false,
            };
            window.Show();
            try
            {
                f.Shell.Activate(PerspectiveSet.Understand, "test");
                Assert.Empty(placed);                                           // not before the layout pass
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);

                var (perspective, body) = Assert.Single(placed);
                Assert.Same(PerspectiveSet.Understand, perspective);
                Assert.Same(f.B.Root, body);
                return 0;
            }
            finally { window.Close(); }
        }, 30);
    }

    // A presenter needs a host for every row: composing one short is a plan defect that fails at
    // construction, never a missing body at the first switch.
    [Fact]
    public void ConstructingWithoutAHostForEveryPerspective_Throws()
    {
        Sta.Run(() =>
        {
            var said = new RecordingAnnouncer();
            var a = DockHost.Create(PerspectiveSet.Factory, _ => new Border(), said, (_, _) => { });
            Assert.Throws<ArgumentException>(() => new PerspectiveShell(new ContentControl(), [a], said));
            return 0;
        }, 30);
    }
}
