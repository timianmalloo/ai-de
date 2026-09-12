using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The presenter and router over three bodies (ADR-0031 falsifying tests 1, 3 and 4's shell half;
/// ADR-0017 as amended; US-C1, US-C2, US-C7, US-C12). Hosts are composed the way the shell composes
/// them — <see cref="DockHost.Create"/> over a stub content factory — never a <c>Border</c> stand-in
/// for a host (DC-135); the Explore body is a plain element, as the presenter never looks inside it.
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

    private sealed record Fixture(PerspectiveShell Shell, ContentControl Body, DockHost A, DockHost B, RecordingAnnouncer Said, Func<int> ExplorerBuilds);

    private static Fixture Build(Func<UIElement>? explorer = null, IWorkbenchAnnouncer? hostAnnouncer = null)
    {
        var said = new RecordingAnnouncer();
        var a = DockHost.Create(PerspectiveSet.Coding, _ => new Border(), hostAnnouncer ?? said, (_, _) => { });
        var b = DockHost.Create(PerspectiveSet.Architecture, _ => new Border(), hostAnnouncer ?? said, (_, _) => { });
        var body = new ContentControl();
        var builds = 0;
        var shell = new PerspectiveShell(body, [a, b], () => { builds++; return explorer?.Invoke() ?? new Grid(); }, said);
        return new Fixture(shell, body, a, b, said, () => builds);
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

    // ADR-0031 test 1 / US-C2 — identity, three bodies: cycling Coding → Architecture → Explore →
    // Coding → Architecture shows reference-identical objects, the Explore factory runs once, and
    // each host's body is ITS OWN root, never the other's. RED if a switch rebuilt or swapped a body.
    [Fact]
    public void CyclingAllThreeBodies_ShowsTheSameInstances_AndBuildsExploreOnce()
    {
        Sta.Run(() =>
        {
            var f = Build();
            Assert.Same(f.A.Root, f.Body.Content);                     // starts on host A (US-C1)

            f.Shell.Activate(PerspectiveSet.Architecture, "test");
            Assert.Same(f.B.Root, f.Body.Content);
            Assert.NotSame(f.A.Root, f.B.Root);

            f.Shell.Activate(PerspectiveSet.Explore, "test");
            var explore = f.Body.Content;
            Assert.NotSame(f.A.Root, explore);
            Assert.NotSame(f.B.Root, explore);
            Assert.IsNotType<DockPanel>(explore);                       // US-C7: no ZoneRails around Explore

            f.Shell.Activate(PerspectiveSet.Coding, "test");
            Assert.Same(f.A.Root, f.Body.Content);

            f.Shell.Activate(PerspectiveSet.Architecture, "test");
            Assert.Same(f.B.Root, f.Body.Content);

            f.Shell.Activate(PerspectiveSet.Explore, "test");
            Assert.Same(explore, f.Body.Content);                       // the same Explore instance
            Assert.Equal(1, f.ExplorerBuilds());

            // The host services are two objects, and a change to one never reaches the other.
            Assert.NotSame(f.A.Service, f.B.Service);
            Assert.NotSame(f.A.Controller, f.B.Controller);
            return 0;
        }, 30);
    }

    // ADR-0032 test 4's second clause / US-C9 — switching perspective never resets the other
    // host's arrangement: each host's shape is byte-identical after A → B → Explore → A → B.
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

            foreach (var target in new[] { PerspectiveSet.Architecture, PerspectiveSet.Explore, PerspectiveSet.Coding, PerspectiveSet.Architecture, PerspectiveSet.Coding })
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

    // US-C1 — activating the active perspective is a no-op that emits nothing; the previous slot
    // holds the last DIFFERENT perspective (after A → B → A it holds B).
    [Fact]
    public void ActivatingTheActivePerspective_IsANoOp_AndThePreviousSlotIsOne()
    {
        WithSink(lines => Sta.Run(() =>
        {
            var f = Build();
            var changes = new List<PerspectiveChange>();
            f.Shell.Changed += (_, c) => changes.Add(c);

            var said = f.Shell.Activate(PerspectiveSet.Coding, "test");     // already active
            Assert.Empty(changes);
            Assert.Empty(ModeLines(lines));
            Assert.Contains("already showing", said, StringComparison.Ordinal);
            Assert.Same(PerspectiveSet.Coding, f.Shell.Previous);          // initialised to Coding

            f.Shell.Activate(PerspectiveSet.Architecture, "test");
            f.Shell.Activate(PerspectiveSet.Architecture, "test");          // no-op again
            f.Shell.Activate(PerspectiveSet.Coding, "test");

            Assert.Equal(2, changes.Count);
            Assert.Same(PerspectiveSet.Architecture, f.Shell.Previous);    // A → B → A: the slot holds B
            Assert.Equal(2, ModeLines(lines).Count);
            return 0;
        }, 30));
    }

    // US-C1 — Escape reaches the perspective level only from Explore: there it restores the
    // previous perspective (announced); from a host it does nothing and says it did not handle it.
    [Fact]
    public void Escape_RestoresThePreviousPerspective_FromExploreOnly()
    {
        Sta.Run(() =>
        {
            var f = Build();
            f.Shell.Activate(PerspectiveSet.Architecture, "test");

            Assert.False(f.Shell.Escape());                                 // a host: Escape keeps its roles
            Assert.Same(PerspectiveSet.Architecture, f.Shell.Active);

            f.Shell.Activate(PerspectiveSet.Explore, "test");
            Assert.True(f.Shell.Escape());
            Assert.Same(PerspectiveSet.Architecture, f.Shell.Active);       // back to where Explore was entered from
            Assert.Same(f.B.Root, f.Body.Content);
            Assert.Contains("Architecture perspective", f.Said.Last, StringComparison.Ordinal);
            return 0;
        }, 30);
    }

    // ADR-0031 test 3 — routing: with Architecture active a host-scoped command reaches host B's
    // controller and not host A's (a recording announcer per host); a Global entry verb reaches
    // host A whatever is active; a derived opener reaches the host that admits its kind.
    [Fact]
    public void Execute_RoutesHostCommandsToTheActiveHost_EntryVerbsToHostA_AndOpenersByKind()
    {
        Sta.Run(() =>
        {
            var saidA = new RecordingAnnouncer();
            var saidB = new RecordingAnnouncer();
            var a = DockHost.Create(PerspectiveSet.Coding, _ => new Border(), saidA, (_, _) => { });
            var b = DockHost.Create(PerspectiveSet.Architecture, _ => new Border(), saidB, (_, _) => { });
            var opened = new List<(string Host, string Kind)>();
            a.Controller.OpenSurfaceRequested = (kind, _) => { opened.Add(("A", kind)); return "opened"; };
            b.Controller.OpenSurfaceRequested = (kind, _) => { opened.Add(("B", kind)); return "opened"; };
            var terminals = 0;
            a.Controller.NewTerminalRequested = () => { terminals++; return "Terminal opened."; };
            var shell = new PerspectiveShell(new ContentControl(), [a, b], () => new Grid(), new RecordingAnnouncer());

            shell.Activate(PerspectiveSet.Architecture, "test");
            Assert.True(shell.Execute("workbench.moveSurface"));
            Assert.Contains("Move pane", saidB.Last, StringComparison.Ordinal);      // host B's controller
            Assert.Empty(saidA.All);                                                  // and not host A's

            Assert.True(shell.Execute("terminal.new"));                               // an entry verb: host A
            Assert.Equal(1, terminals);

            Assert.True(shell.Execute("surface.new.codeviewer"));                     // shared kind: the ACTIVE host
            Assert.Equal(("B", "codeviewer"), opened[^1]);

            shell.Activate(PerspectiveSet.Coding, "test");
            Assert.True(shell.Execute("surface.new.codeviewer"));
            Assert.Equal(("A", "codeviewer"), opened[^1]);
            Assert.True(shell.Execute("surface.new.classdiagram"));                   // Coding does not admit it: routed to B
            Assert.Equal(("B", "classdiagram"), opened[^1]);
            Assert.True(shell.Execute("workbench.moveSurface"));
            Assert.Contains("Move pane", saidA.Last, StringComparison.Ordinal);      // host A's controller now

            Assert.False(shell.Execute("no.such.command"));
            return 0;
        }, 30);
    }

    // ADR-0017 amendment clause 4 — a host-scoped command while Explore is the body is refused with
    // a reason and a stable code; never a silent model update, never a switch.
    [Fact]
    public void Execute_OfAHostCommandInExplore_IsRefusedWithAReason_AndSwitchesNothing()
    {
        WithSink(lines => Sta.Run(() =>
        {
            var f = Build();
            f.Shell.Activate(PerspectiveSet.Explore, "test");

            Assert.True(f.Shell.Execute("workbench.closeSurface"));

            Assert.Same(PerspectiveSet.Explore, f.Shell.Active);
            Assert.Contains("needs a docking host", f.Said.Last, StringComparison.Ordinal);
            var refused = ModeLines(lines).Single(e => e.GetProperty("outcome").GetString() == "refused");
            Assert.Equal(PerspectiveShell.NoHostCode, refused.GetProperty("error_code").GetString());
            return 0;
        }, 30));
    }

    // The seam, generalised: a document opening in a host whose body is not on screen switches to
    // that host (trigger `document-opening`); one opening where the operator already is does not.
    [Fact]
    public void OnDocumentOpening_SwitchesToTheDocumentsHost_OnlyWhenItIsNotTheBody()
    {
        WithSink(lines => Sta.Run(() =>
        {
            var f = Build();

            f.Shell.OnDocumentOpening(PerspectiveSet.Coding);                // already the body: nothing
            Assert.Empty(ModeLines(lines));

            f.Shell.Activate(PerspectiveSet.Explore, "perspective.explore");
            f.Shell.OnDocumentOpening(PerspectiveSet.Coding);                // from Explore: the host returns
            Assert.Same(PerspectiveSet.Coding, f.Shell.Active);
            Assert.Same(f.A.Root, f.Body.Content);

            f.Shell.OnDocumentOpening(PerspectiveSet.Architecture);          // a routed open: host B becomes the body
            Assert.Same(PerspectiveSet.Architecture, f.Shell.Active);

            var triggers = ModeLines(lines).Select(e => e.GetProperty("trigger").GetString()).ToList();
            Assert.Equal(["perspective.explore", "document-opening", "document-opening"], triggers);
            return 0;
        }, 30));
    }

    // US-C12 — every switch is one structured line with from, to, trigger, first_entry, outcome and
    // error_code (null on success); a first entry into Explore says so.
    [Fact]
    public void EverySwitch_WritesOneShellModeLine_WithItsFullShape()
    {
        WithSink(lines => Sta.Run(() =>
        {
            var f = Build();
            f.Shell.Activate(PerspectiveSet.Explore, "perspective.explore");
            f.Shell.Activate(PerspectiveSet.Coding, "escape");
            f.Shell.Activate(PerspectiveSet.Explore, "rail");

            var modes = ModeLines(lines);
            Assert.Equal(3, modes.Count);

            Assert.Equal("explore", modes[0].GetProperty("mode").GetString());
            Assert.Equal("coding", modes[0].GetProperty("from").GetString());
            Assert.Equal("perspective.explore", modes[0].GetProperty("trigger").GetString());
            Assert.True(modes[0].GetProperty("first_entry").GetBoolean());
            Assert.Equal("switched", modes[0].GetProperty("outcome").GetString());
            Assert.Equal(JsonValueKind.Null, modes[0].GetProperty("error_code").ValueKind);

            Assert.False(modes[1].GetProperty("first_entry").GetBoolean());
            Assert.Equal("escape", modes[1].GetProperty("trigger").GetString());
            Assert.False(modes[2].GetProperty("first_entry").GetBoolean());   // retained, not rebuilt
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
                foreach (var target in new[] { PerspectiveSet.Architecture, PerspectiveSet.Coding, PerspectiveSet.Architecture })
                {
                    f.Shell.Activate(target, "test");
                    Flush();
                    Flush();   // a second layout pass after the switch must not produce a second line
                }

                var shown = ModeLines(lines, "shell.mode.shown");
                Assert.Equal(3, shown.Count);
                Assert.Equal(["architecture", "coding", "architecture"], shown.Select(e => e.GetProperty("mode").GetString()).ToList());
                Assert.All(shown, e => Assert.True(e.GetProperty("duration_ms").GetDouble() >= 0));
                Assert.Equal(3, ModeLines(lines).Count);

                // Rapid repeated switching (spec §A10's boundary): a switch superseded before its
                // body's first layout pass is NOT recorded — its stale edge never fires alongside
                // the next entry with an earlier start (IO12: no plausible wrong number).
                lines.Clear();
                f.Shell.Activate(PerspectiveSet.Coding, "t1");
                f.Shell.Activate(PerspectiveSet.Architecture, "t2");
                f.Shell.Activate(PerspectiveSet.Coding, "t3");
                Flush();
                Flush();
                f.Shell.Activate(PerspectiveSet.Architecture, "t4");
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

    // spec §C5 — focus lands where the window says (the reader for Explore, never the canvas; the
    // active document for a host), once the body has had its first layout pass, and never on the
    // rail: the presenter asks the EntryFocus hook with the perspective and the body it showed.
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
                f.Shell.Activate(PerspectiveSet.Architecture, "test");
                Assert.Empty(placed);                                           // not before the layout pass
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);

                var (perspective, body) = Assert.Single(placed);
                Assert.Same(PerspectiveSet.Architecture, perspective);
                Assert.Same(f.B.Root, body);
                return 0;
            }
            finally { window.Close(); }
        }, 30);
    }

    // C7 — a body that fails to build: the active perspective, its body and focus are unchanged, the
    // failure is announced with its reason, the rail is told (its error state), and the switch line
    // carries outcome=failed with the stable code. Activating again retries the build.
    [Fact]
    public void ABodyThatFailsToBuild_LeavesTheActivePerspectiveUnchanged_AndReportsWhy()
    {
        WithSink(lines => Sta.Run(() =>
        {
            var attempts = 0;
            var f = Build(explorer: () =>
            {
                attempts++;
                if (attempts == 1) { throw new InvalidOperationException("the graph substrate is not ready"); }
                return new Grid();
            });
            PerspectiveBodyFailure? failure = null;
            f.Shell.BodyFailed += (_, e) => failure = e;
            var changes = 0;
            f.Shell.Changed += (_, _) => changes++;

            var said = f.Shell.Activate(PerspectiveSet.Explore, "rail");

            Assert.Same(PerspectiveSet.Coding, f.Shell.Active);
            Assert.Same(f.A.Root, f.Body.Content);
            Assert.Equal(0, changes);
            Assert.StartsWith("Couldn't open Explore — the graph substrate is not ready", said, StringComparison.Ordinal);
            Assert.NotNull(failure);
            Assert.Same(PerspectiveSet.Explore, failure!.Perspective);

            var line = ModeLines(lines).Single();
            Assert.Equal("failed", line.GetProperty("outcome").GetString());
            Assert.Equal(PerspectiveShell.BodyFailedCode, line.GetProperty("error_code").GetString());

            f.Shell.Activate(PerspectiveSet.Explore, "rail");                 // activation retries
            Assert.Same(PerspectiveSet.Explore, f.Shell.Active);
            Assert.Equal(2, attempts);
            return 0;
        }, 30));
    }

    // A presenter needs a host for every host-bodied perspective: composing one is a plan defect
    // that fails at construction, never a null body at the first switch.
    [Fact]
    public void ConstructingWithoutAHostForEveryHostPerspective_Throws()
    {
        Sta.Run(() =>
        {
            var said = new RecordingAnnouncer();
            var a = DockHost.Create(PerspectiveSet.Coding, _ => new Border(), said, (_, _) => { });
            Assert.Throws<ArgumentException>(() => new PerspectiveShell(new ContentControl(), [a], () => new Grid(), said));
            Assert.Throws<ArgumentException>(() => DockHost.Create(PerspectiveSet.Explore, _ => new Border(), said, (_, _) => { }));
            return 0;
        }, 30);
    }
}
