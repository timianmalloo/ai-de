using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using AvalonDock;
using AvalonDock.Layout;

namespace AiDe.App.Tests;

/// <summary>
/// INV-0006 F1: the workbench had <b>no drag-completed hook</b>. AvalonDock owns the tab drag, mutates
/// its own tree, and tells nobody — so the zone model, which is the source of truth, learned about a
/// drag only when one of four unrelated commands next happened to reconcile. These drive a real
/// <see cref="DockingManager"/> on an STA thread and assert the model catches up on its own.
/// </summary>
/// <remarks>
/// What these still cannot assert — stated rather than implied — is that AvalonDock's own pointer drop
/// produces the tree mutation performed here. That link needs a driven surface (INV-0006 §9); these
/// mutate the docking tree directly, which is what AvalonDock's drop does to it.
/// </remarks>
public sealed class WorkbenchDragCompletedHookTests
{
    /// <summary>
    /// The host under the drag is <b>host B (Architecture)</b>: the kinds these drags move — the
    /// graph, the evidence views, contexts — are Architecture's by ADR-0030's allow-list, and the
    /// hook is wired per host (ADR-0031), so driving host B proves the wiring the second host got.
    /// Host B's default (Ruling 94, §B4): Left: graph — Center: contexts · domain (classdiagram) —
    /// Right: empty, collapsed.
    /// </summary>
    private sealed record Harness(WorkbenchShell Shell, Window Window)
    {
        public DockHost? HostOverride { get; init; }

        public DockHost Host => HostOverride ?? Shell.Architecture;

        public WorkbenchAdapter Adapter => Host.Adapter;

        public ZoneBackedLayoutService Zones => Host.Service;
    }

    /// <summary>
    /// Realizes the <b>real shell</b> offscreen — not a bare adapter. The hook is only a fix if the
    /// shell subscribes it to the reconcile, so the wiring is part of what is under test.
    /// </summary>
    private static T WithRealizedWorkbench<T>(Func<Harness, T> body) => Sta.Run(() =>
    {
        using var shell = new WorkbenchShell(queries: null);
        var window = new Window
        {
            Content = shell,
            Width = 1000,
            Height = 700,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -10000,
            Top = -10000,
            ShowInTaskbar = false,
            ShowActivated = false,
        };

        window.Show();
        foreach (var host in shell.Hosts)
        {
            Settle(window, host.Manager);
        }

        try { return body(new Harness(shell, window)); }
        finally { window.Close(); }
    }, 60);

    private static void Settle(Window window, DockingManager manager)
    {
        window.UpdateLayout();
        manager.UpdateLayout();
    }

    /// <summary>
    /// The control for F1. RED before the hook existed: the model still held the dragged pane in
    /// its old zone minutes after the drag, because nothing told it.
    /// </summary>
    [Theory]
    [InlineData("architecture", "graph", "contexts", ZoneId.Left, ZoneId.Center)]
    [InlineData("coordination", "sessions", "ledger", ZoneId.Left, ZoneId.Center)]
    public void ANativeDrag_ReachesTheModelImmediately_WithoutWaitingForAnUnrelatedCommand(
        string perspective, string dragged, string onto, ZoneId expectedBefore, ZoneId expectedAfter)
    {
        // Two of the three hosts here: the hook is wired per host (ADR-0031), so INV-0006 F1 is
        // proven for host B and host C alike, each with a pair its own allow-list admits. Host A's
        // default holds no pair (its Left is empty until a session opens, Ruling 83); its row is
        // InCoding_TheSessionDraggedIntoTheCenterAndBack_… below, over a session it opens first.
        var (zoneBefore, zoneAfter) = WithRealizedWorkbench(h =>
        {
            var host = h.Shell.Hosts.Single(x => x.Row.Id == perspective);
            var hh = h with { HostOverride = host };
            var before = host.Service.Zones.FindZoneOf(dragged);
            DragDocumentIntoPane(hh, dragged, onto);
            return (before, host.Service.Zones.FindZoneOf(dragged));
        });

        Assert.Equal(expectedBefore, zoneBefore);
        Assert.Equal(expectedAfter, zoneAfter);
    }

    /// <summary>
    /// The oracle, on a driven docking host: a drag moves the surface it dragged and nothing else.
    /// </summary>
    [Fact]
    public void ANativeDrag_MovesOnlyTheDraggedSurface_LeavingEveryBystanderInItsZone()
    {
        var (before, after) = WithRealizedWorkbench(h =>
        {
            var start = ZoneOfEverySurface(h.Zones);
            DragDocumentIntoPane(h, "contexts", "graph");
            return (start, ZoneOfEverySurface(h.Zones));
        });

        var moved = before.Keys.Where(id => before[id] != after[id]).OrderBy(id => id, StringComparer.Ordinal).ToList();
        Assert.Equal(["contexts"], moved);
    }

    /// <summary>
    /// Two drags with no command between them — the exact sequence that drifted for 3m49s in the
    /// reported session. Each reconciles as it lands, so the second is still only one drag from the
    /// model and no bystander moves.
    /// </summary>
    [Fact]
    public void TwoDragsWithNoCommandBetweenThem_EachReconcile_SoNeitherAccumulatesDrift()
    {
        var (before, after) = WithRealizedWorkbench(h =>
        {
            var start = ZoneOfEverySurface(h.Zones);
            DragDocumentIntoPane(h, "contexts", "graph");
            DragDocumentIntoPane(h, "domain", "graph");
            return (start, ZoneOfEverySurface(h.Zones));
        });

        var moved = before.Keys.Where(id => before[id] != after[id]).OrderBy(id => id, StringComparer.Ordinal).ToList();
        Assert.Equal(["contexts", "domain"], moved);
        Assert.Equal(ZoneId.Left, after["contexts"]);
        Assert.Equal(ZoneId.Left, after["domain"]);
    }

    /// <summary>
    /// The guard that makes the hook a drag-completed hook and not a per-frame reconcile.
    /// <c>Manager.LayoutUpdated</c> is WPF's <see cref="UIElement.LayoutUpdated"/> — AvalonDock
    /// declares no event of that name — so it fires on layout passes that rearranged nothing. This
    /// asserts that measured fact and that the hook stays silent through them.
    /// </summary>
    [Fact]
    public void LayoutPassesThatRearrangeNothing_RaiseNoArrangementChange()
    {
        var (layoutPasses, raised) = WithRealizedWorkbench(h =>
        {
            var passes = 0;
            var changes = 0;
            void CountPass(object? _, EventArgs __) => passes++;
            h.Adapter.Manager.LayoutUpdated += CountPass;
            h.Adapter.ViewArrangementChanged += (_, _) => changes++;

            // Resize, invalidate, re-run the layout pass — the shapes that fire LayoutUpdated in a
            // running app without a single pane changing place.
            for (var i = 0; i < 5; i++)
            {
                h.Window.Width = 900 + (i * 10);
                h.Adapter.Manager.InvalidateMeasure();
                Settle(h.Window, h.Adapter.Manager);
            }

            h.Adapter.Manager.LayoutUpdated -= CountPass;
            return (passes, changes);
        });

        Assert.True(layoutPasses > 0, "LayoutUpdated never fired — the premise of the guard is untested");
        Assert.Equal(0, raised);
    }

    /// <summary>
    /// Why the hook watches AvalonDock's own signal and not only WPF's layout pass. This is the
    /// measurement that corrected INV-0006's proposed wiring: a tab moved into another pane raises
    /// <see cref="LayoutRoot.Updated"/> and raises <c>Manager.LayoutUpdated</c> <b>zero</b> times in a
    /// host running no visual pass. Delete the docking-model subscription and this goes red.
    /// </summary>
    [Fact]
    public void ACrossPaneDrag_RaisesTheDockingModelsOwnUpdate_NotAWpfLayoutPass()
    {
        var (dockingUpdates, wpfLayoutPasses) = WithRealizedWorkbench(h =>
        {
            var docking = 0;
            var wpf = 0;
            h.Adapter.Manager.Layout.Updated += (_, _) => docking++;
            h.Adapter.Manager.LayoutUpdated += (_, _) => wpf++;

            DragDocumentIntoPane(h, "contexts", "graph");
            return (docking, wpf);
        });

        Assert.True(dockingUpdates >= 1, "LayoutRoot.Updated did not fire for a cross-pane move");
        Assert.Equal(0, wpfLayoutPasses);
    }

    /// <summary>
    /// INV-0006 F2. A drag emitted <b>nothing at all</b> — the reported session's log is empty across
    /// the whole defect window — so the gesture that produced it is permanently unrecoverable. This
    /// asserts what a real drag now writes: the trigger, and the zone assignment before and after it.
    /// </summary>
    [Fact]
    public void ADrag_EmitsALayoutMutationRecord_CarryingTheZoneAssignmentBeforeAndAfter()
    {
        var records = CapturingDiagnostics(() => WithRealizedWorkbench(h =>
        {
            DragDocumentIntoPane(h, "contexts", "graph");
            return 0;
        }));

        var drag = records
            .Select(line => JsonDocument.Parse(line).RootElement)
            .Where(r => r.GetProperty("evt").GetString() == "layout.mutation"
                        && r.GetProperty("operation").GetString() == "drag")
            .ToList();

        var reconciled = Assert.Single(drag, r => r.GetProperty("placement").GetString() == "reconciled");

        var before = reconciled.GetProperty("before").GetString()!;
        var after = reconciled.GetProperty("after").GetString()!;
        var moved = reconciled.GetProperty("moved").EnumerateArray().Select(e => e.GetString()).ToList();

        // The pair is the point: either half alone cannot separate a correct reconcile from a
        // whole-column relabel, which is the distinction the original report could not be answered on.
        Assert.Contains("Center:[contexts", before, StringComparison.Ordinal);
        Assert.Contains("Left:[contexts+graph", after, StringComparison.Ordinal);   // dropped first in the pane
        Assert.Equal(["contexts"], moved);
    }

    /// <summary>
    /// INV-0006 F3 as re-pointed by F-1 (Rulings 83/88; SH-4.2), through the real docking host: a
    /// collapsed tool zone that still holds panes is absent from the view by design, and the drag
    /// is APPLIED — the rail keeps its panes, nothing is refused, nothing is announced as "a
    /// collapsed panel still holds panes". Before F-1 this test pinned the refusal (then the only
    /// honest outcome of a mapping that pre-seeded the zone empty); the operator hit it as finding 4.
    /// </summary>
    [Fact]
    public void ADragWhileACollapsedZoneHoldsPanes_IsAppliedThroughTheDockingHost_AndNothingIsRefused()
    {
        var (announcement, refusals, provenance, held, collapsed) = WithRealizedWorkbench(h =>
        {
            var records = new List<string>();
            var previous = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = records.Add;
            try
            {
                // Right gets two panes (opened, which expands it) so a drag out of it leaves it
                // rendered; Left stays collapsed while still holding two (the graph and contexts).
                // The drag goes Right -> Center, both rendered.
                h.Zones.Apply(new LayoutOperation.AddSurface(ZonesToTree.RightStackId, new Surface("evidence", "view", "Evidence")));
                h.Zones.Apply(new LayoutOperation.AddSurface(ZonesToTree.RightStackId, new Surface("joins", "joins", "Joins")));
                h.Zones.Apply(new LayoutOperation.MoveSurface("contexts", new DropTarget(ZonesToTree.LeftStackId, DropKind.JoinStack)));
                h.Zones.Apply(new LayoutOperation.SetStackState(ZonesToTree.LeftStackId, StackState.Collapsed));
                h.Adapter.Render();
                Settle(h.Window, h.Adapter.Manager);
                var heldBefore = h.Zones.Zones.Zone(ZoneId.Left).Surfaces().Select(s => s.SurfaceId).ToList();

                DragDocumentIntoPane(h, "evidence", "domain");
                return (
                    h.Shell.Announcer.Last,
                    records.Count(r => r.Contains("\"placement\":\"refused\"", StringComparison.Ordinal)),
                    h.Zones.Zones.FindZoneOf("evidence"),
                    (Before: heldBefore, After: h.Zones.Zones.Zone(ZoneId.Left).Surfaces().Select(s => s.SurfaceId).ToList()),
                    h.Zones.Zones.Zone(ZoneId.Left).Collapsed);
            }
            finally { WorkbenchDiagnostics.Sink = previous; }
        });

        Assert.Equal(ZoneId.Center, provenance);
        Assert.Equal(0, refusals);
        Assert.DoesNotContain("could not be applied", announcement, StringComparison.Ordinal);
        Assert.Equal(2, held.Before.Count);                 // DC-016: the rail really held two
        Assert.Equal(held.Before, held.After);
        Assert.True(collapsed);
    }

    /// <summary>
    /// O-2's gesture (Ruling 88 condition 1) through the real docking host, in Coding's re-cut: the
    /// session document docked at Left is dragged into the empty Center — onto the placeholder's
    /// pane — and then back into a new pane on the left; both reconcile as the operator dropped
    /// them, the Bottom's collapsed terminal untouched, nothing refused.
    /// </summary>
    [Fact]
    public void InCoding_TheSessionDraggedIntoTheCenterAndBack_FollowsTheDrops_WithTheBottomCollapsed()
    {
        var (afterFirst, afterSecond, refusals, bottom) = WithRealizedWorkbench(h =>
        {
            var host = h.Shell.Coding;
            var hh = h with { HostOverride = host };
            var records = new List<string>();
            var previous = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = records.Add;
            try
            {
                Assert.True(host.Service.Apply(new LayoutOperation.AddSurface(ZonesToTree.LeftStackId, new Surface("session:s1", "session-document", "S1"))).Applied);
                host.Adapter.Render();
                Settle(h.Window, host.Manager);
                Assert.Equal(ZoneId.Left, host.Service.Zones.FindZoneOf("session:s1"));

                // Into the Center: the placeholder is a real document in the Center pane until the next render.
                DragDocumentIntoPane(hh, "session:s1", ZonesToTree.WelcomePlaceholder.SurfaceId);
                var first = host.Service.Zones.FindZoneOf("session:s1");

                // And back: a new pane to the LEFT of the Center's, as AvalonDock's side-drop makes one.
                var root = host.Manager.Layout;
                var document = root.Descendents().OfType<LayoutDocument>().Single(d => d.ContentId == "session:s1");
                var center = (LayoutDocumentPane)document.Parent;
                var group = (LayoutPanel)center.Parent;
                var pane = new LayoutDocumentPane();
                group.InsertChildAt(group.Children.IndexOf(center), pane);
                center.RemoveChild(document);
                pane.Children.Add(document);
                Settle(h.Window, host.Manager);

                return (first, host.Service.Zones.FindZoneOf("session:s1"),
                    records.Count(r => r.Contains("\"placement\":\"refused\"", StringComparison.Ordinal)),
                    host.Service.Zones.Zone(ZoneId.Bottom));
            }
            finally { WorkbenchDiagnostics.Sink = previous; }
        });

        Assert.Equal(ZoneId.Center, afterFirst);
        Assert.Equal(ZoneId.Left, afterSecond);
        Assert.Equal(0, refusals);
        Assert.True(bottom.Collapsed);
        Assert.Equal(["terminal-1"], bottom.Surfaces().Select(s => s.SurfaceId));
    }

    /// <summary>
    /// INV-0006 F3, the refusal that remains: a frame the mapping cannot place without guessing —
    /// two split-off columns and no Center column to place them beside — is refused, the panes
    /// return, and the strip says so in the one sentence that is still true. Through the docking
    /// host, so the shell's announcement is pinned by a real reconcile, not by a fixture literal.
    /// </summary>
    [Fact]
    public void ADragThatCannotBePlacedWithoutGuessing_IsRefusedAndAnnounced_AndThePanesReturn()
    {
        var (announcement, refusals, shape, before, returned) = WithRealizedWorkbench(h =>
        {
            var host = h.Shell.Coding;
            var records = new List<string>();
            var previous = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = records.Add;
            try
            {
                foreach (var id in new[] { "session:a", "session:b", "session:c" })
                {
                    Assert.True(host.Service.Apply(new LayoutOperation.AddSurface(ZonesToTree.LeftStackId, new Surface(id, "session-document", id))).Applied);
                }

                host.Adapter.Render();
                Settle(h.Window, host.Manager);
                var shapeBefore = host.Service.Zones.Shape();

                // The view AvalonDock hands the hook after ONE drop: b and c each in a new, unnamed
                // pane beside the Left pane and the Center pane gone with its placeholder — nothing
                // says which of the two new columns is the Center. Built as one tree and swapped in
                // whole, the way a drop lands, then poked once so the docking model reports it.
                var docs = host.Manager.Layout.Descendents().OfType<LayoutDocument>().ToDictionary(d => d.ContentId!, StringComparer.Ordinal);
                foreach (var doc in docs.Values)
                {
                    doc.Parent.RemoveChild(doc);
                }

                var left = new LayoutDocumentPane();
                ((ILayoutPaneSerializable)left).Id = ZonesToTree.LeftStackId;
                left.Children.Add(docs["session:a"]);
                var second = new LayoutDocumentPane();
                second.Children.Add(docs["session:b"]);
                var third = new LayoutDocumentPane();
                third.Children.Add(docs["session:c"]);
                var columns = new LayoutPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                columns.Children.Add(left);
                columns.Children.Add(second);
                columns.Children.Add(third);
                host.Manager.Layout = new LayoutRoot { RootPanel = columns };
                Settle(h.Window, host.Manager);
                columns.Children.Add(new LayoutDocumentPane());   // one docking-model update over the whole frame (an empty pane is skipped by the reader)
                Settle(h.Window, host.Manager);
                var said = h.Shell.Announcer.Last;
                var refused = records.Count(r => r.Contains("position-mapping-refused", StringComparison.Ordinal));
                var shapeAfter = host.Service.Zones.Shape();

                // The panes return: the next render draws the untouched model — the Left pane holds
                // the three again, the columns AvalonDock showed are gone.
                host.Adapter.Render();
                Settle(h.Window, host.Manager);
                var leftPane = (LayoutDocumentPane)host.Manager.Layout.Descendents().OfType<LayoutDocument>().Single(d => d.ContentId == "session:a").Parent;
                var returned = leftPane.Children.OfType<LayoutDocument>().Select(d => d.ContentId).ToList();
                return (said, refused, shapeAfter, shapeBefore, returned);
            }
            finally { WorkbenchDiagnostics.Sink = previous; }
        });

        Assert.True(refusals >= 1, "the ambiguous frame was not refused: " + shape + " :: " + announcement);
        Assert.Equal(before, shape);   // untouched by the refusal
        Assert.Equal(["session:a", "session:b", "session:c"], returned);   // and back on screen after the render
        Assert.Equal("That pane move could not be applied, so the panes will return to where they were.", announcement);
    }

    /// <summary>
    /// INV-0006 F4. Opening a workspace replaces the WHOLE arrangement, so every pane moves at once.
    /// <c>LayoutPersistence.Restore()</c> has always composed the sentence that says so, and the caller
    /// used the result only for a null test and threw the sentence away — which is half of why the
    /// reported session's first two screenshots read as "the tabs rearranged without me doing anything".
    /// </summary>
    [Fact]
    public void OpeningAWorkspace_AnnouncesWhatTheRestoreDid_AndRecordsTheBranchThatRan()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aide-f4-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(directory);

        try
        {
            var (firstOpen, firstRecords, secondOpen, secondRecords) = Sta.Run(() =>
            {
                var lines = new List<string>();
                var previous = WorkbenchDiagnostics.Sink;
                WorkbenchDiagnostics.Sink = line => { lock (lines) { lines.Add(line); } };
                try
                {
                    // Nothing saved yet: the honest sentence is the KEPT one, and the record must say
                    // keep-current — where it used to say restore-zones whatever actually happened.
                    using var first = new WorkbenchShell(queries: null, workspaceDataDirectory: directory);
                    var firstSaid = first.Announcer.Last;
                    var firstSeen = lines.ToList();

                    // Rearrange, flush, and open the workspace again.
                    first.Service.Apply(new LayoutOperation.MoveSurface(
                        "board", new DropTarget(ZonesToTree.LeftStackId, DropKind.JoinStack)));
                    first.Coding.Persistence!.SaveNow();

                    lines.Clear();
                    using var second = new WorkbenchShell(queries: null, workspaceDataDirectory: directory);
                    return (firstSaid, firstSeen, second.Announcer.Last, lines.ToList());
                }
                finally { WorkbenchDiagnostics.Sink = previous; }
            }, 60);

            Assert.Equal("Kept the current workbench arrangement.", firstOpen);
            Assert.Contains(firstRecords, r => IsWorkspaceOpen(r, "keep-current"));

            Assert.Equal("Restored your saved workbench arrangement.", secondOpen);
            Assert.Contains(secondRecords, r => IsWorkspaceOpen(r, "restore-zones"));
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch (IOException) { }
        }
    }

    private static bool IsWorkspaceOpen(string record, string placement)
    {
        var root = JsonDocument.Parse(record).RootElement;
        return root.TryGetProperty("operation", out var op) && op.GetString() == "workspace-open"
            && root.GetProperty("placement").GetString() == placement;
    }

    /// <summary>
    /// INV-0006 §11's first residual risk, closed by F1 and asserted rather than assumed: <b>the
    /// layout the user is looking at is the layout that gets saved.</b>
    /// </summary>
    /// <remarks>
    /// <para>Persistence marks dirty from the view's signal but writes the zone MODEL, so before F1
    /// every save between a drag and the next unrelated command wrote an arrangement the user was not
    /// looking at — including the shutdown flush in <c>Dispose()</c>, which is the most common moment
    /// to close an app right after rearranging it.</para>
    /// <para><b>The half that is NOT closed, stated rather than implied:</b> <c>MarkDirty</c> is
    /// hooked to <c>Manager.LayoutUpdated</c>, and a cross-pane drag raises that ZERO times here
    /// (see <see cref="ACrossPaneDrag_RaisesTheDockingModelsOwnUpdate_NotAWpfLayoutPass"/>), so a drag
    /// does not SCHEDULE a save. The arrangement is correct whenever a save runs, and the shutdown
    /// flush still writes it; a crash between the drag and shutdown does not.</para>
    /// </remarks>
    [Fact]
    public void ASaveAfterADrag_WritesTheArrangementTheUserIsLookingAt()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aide-drag-save-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(directory);

        try
        {
            var (onScreen, reopened) = Sta.Run(() =>
            {
                using var shell = new WorkbenchShell(queries: null, workspaceDataDirectory: directory);
                var window = new Window
                {
                    Content = shell,
                    Width = 1000,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                };
                window.Show();
                Settle(window, shell.Architecture.Manager);

                DragDocumentIntoPane(new Harness(shell, window), "contexts", "graph");
                var afterDrag = shell.Architecture.Service.Zones.Shape();
                shell.Architecture.Persistence!.SaveNow();
                window.Close();

                using var next = new WorkbenchShell(queries: null, workspaceDataDirectory: directory);
                return (afterDrag, next.Architecture.Service.Zones.Shape());
            }, 60);

            Assert.Contains("Left:[contexts+graph", onScreen, StringComparison.Ordinal);   // Ruling 94: the graph's home is the Left
            Assert.Equal(onScreen, reopened);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch (IOException) { }
        }
    }

    private static List<string> CapturingDiagnostics(Action body)
    {
        var lines = new List<string>();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = line => { lock (lines) { lines.Add(line); } };
        try { body(); }
        finally { WorkbenchDiagnostics.Sink = previous; }

        return lines;
    }

    // ── driving the docking host ────────────────────────────────────────────────────────────

    /// <summary>
    /// Moves a rendered document into another pane, the way AvalonDock's own drop does, and runs the
    /// layout pass that follows it.
    /// </summary>
    private static void DragDocumentIntoPane(Harness h, string surfaceId, string ontoSurfaceId)
    {
        var root = h.Adapter.Manager.Layout;

        LayoutDocument Document(string id) => root.Descendents().OfType<LayoutDocument>()
            .Single(d => string.Equals(d.ContentId, id, StringComparison.Ordinal));

        var document = Document(surfaceId);

        // The destination pane is named by a surface already in it — the adapter does not stamp its
        // panes with the zone's stack id, and inventing a second way to identify a pane here would be
        // a fixture that can disagree with the thing it is testing.
        var target = (LayoutDocumentPane)Document(ontoSurfaceId).Parent;

        document.Parent.RemoveChild(document);
        target.InsertChildAt(0, document);
        Settle(h.Window, h.Adapter.Manager);
    }

    private static Dictionary<string, ZoneId> ZoneOfEverySurface(ZoneBackedLayoutService service) =>
        Enum.GetValues<ZoneId>()
            .SelectMany(z => service.Zones.Zone(z).Surfaces().Select(s => (s.SurfaceId, Zone: z)))
            .ToDictionary(x => x.SurfaceId, x => x.Zone, StringComparer.Ordinal);
}
