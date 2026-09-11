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
    private sealed record Harness(WorkbenchShell Shell, Window Window)
    {
        public WorkbenchAdapter Adapter => Shell.Adapter;

        public ZoneBackedLayoutService Zones => (ZoneBackedLayoutService)Shell.Service;
    }

    /// <summary>
    /// Realizes the <b>real shell</b> offscreen — not a bare adapter. The hook is only a fix if the
    /// shell subscribes it to the reconcile, so the wiring is part of what is under test.
    /// </summary>
    private static T WithRealizedWorkbench<T>(Func<Harness, T> body) => Sta.Run(() =>
    {
        var shell = new WorkbenchShell(queries: null);
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
        Settle(window, shell.Manager);

        try { return body(new Harness(shell, window)); }
        finally { window.Close(); }
    }, 60);

    private static void Settle(Window window, DockingManager manager)
    {
        window.UpdateLayout();
        manager.UpdateLayout();
    }

    /// <summary>
    /// The control for F1. RED before the hook existed: the model still held "domain" in the Center
    /// minutes after the drag, because nothing told it.
    /// </summary>
    [Fact]
    public void ANativeDrag_ReachesTheModelImmediately_WithoutWaitingForAnUnrelatedCommand()
    {
        var (zoneBefore, zoneAfter) = WithRealizedWorkbench(h =>
        {
            var before = h.Zones.Zones.FindZoneOf("domain");
            DragDocumentIntoPane(h, "domain", "explore");
            return (before, h.Zones.Zones.FindZoneOf("domain"));
        });

        Assert.Equal(ZoneId.Center, zoneBefore);
        Assert.Equal(ZoneId.Left, zoneAfter);
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
            DragDocumentIntoPane(h, "domain", "explore");
            return (start, ZoneOfEverySurface(h.Zones));
        });

        var moved = before.Keys.Where(id => before[id] != after[id]).OrderBy(id => id, StringComparer.Ordinal).ToList();
        Assert.Equal(["domain"], moved);
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
            DragDocumentIntoPane(h, "domain", "explore");
            DragDocumentIntoPane(h, "sessions", "explore");
            return (start, ZoneOfEverySurface(h.Zones));
        });

        var moved = before.Keys.Where(id => before[id] != after[id]).OrderBy(id => id, StringComparer.Ordinal).ToList();
        Assert.Equal(["domain", "sessions"], moved);
        Assert.Equal(ZoneId.Left, after["domain"]);
        Assert.Equal(ZoneId.Left, after["sessions"]);
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

            DragDocumentIntoPane(h, "domain", "explore");
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
            DragDocumentIntoPane(h, "domain", "explore");
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
        Assert.Contains("Center:[graph+domain+", before, StringComparison.Ordinal);
        Assert.Contains("Left:[domain+explore+", after, StringComparison.Ordinal);
        Assert.Equal(["domain"], moved);
    }

    /// <summary>
    /// INV-0006 F3. A collapsed tool zone that still holds panes makes every native drag revert — the
    /// zone is not rendered, so the reconcile sees its panes go missing and refuses. That refusal is
    /// correct and was completely silent: the user's drag simply undid itself with no message.
    /// </summary>
    [Fact]
    public void ADragThatCannotBeApplied_IsAnnouncedInsteadOfSilentlyReverting()
    {
        var (announcement, refusals) = WithRealizedWorkbench(h =>
        {
            var records = new List<string>();
            var previous = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = records.Add;
            try
            {
                h.Shell.Service.Apply(new LayoutOperation.SetStackState(ZonesToTree.LeftStackId, StackState.Collapsed));
                h.Adapter.Render();
                Settle(h.Window, h.Adapter.Manager);

                DragDocumentIntoPane(h, "domain", "terminal-1");
                return (h.Shell.Announcer.Last, records.Count(r => r.Contains("position-mapping-refused", StringComparison.Ordinal)));
            }
            finally { WorkbenchDiagnostics.Sink = previous; }
        });

        Assert.Contains("could not be applied", announcement, StringComparison.Ordinal);
        Assert.Contains("collapsed panel", announcement, StringComparison.Ordinal);
        Assert.True(refusals >= 1, "the refused reconcile was not recorded");
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
                    var first = new WorkbenchShell(queries: null, workspaceDataDirectory: directory);
                    var firstSaid = first.Announcer.Last;
                    var firstSeen = lines.ToList();

                    // Rearrange, flush, and open the workspace again.
                    first.Service.Apply(new LayoutOperation.MoveSurface(
                        "domain", new DropTarget(ZonesToTree.LeftStackId, DropKind.JoinStack)));
                    first.Persistence!.SaveNow();

                    lines.Clear();
                    var second = new WorkbenchShell(queries: null, workspaceDataDirectory: directory);
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
