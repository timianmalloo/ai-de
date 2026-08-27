using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// SC 4.1.3 (Status Messages) made behaviour: every completed layout change reaches assistive
/// technology, and it does so without moving focus. No exemplar documents doing this, so these tests
/// are the only thing keeping the claim honest.
/// </summary>
public sealed class WorkbenchControllerTests
{
    private static (WorkbenchController Controller, RecordingAnnouncer Announcer, ILayoutService Service) Build()
    {
        var service = new LayoutService();
        var announcer = new RecordingAnnouncer();
        var controller = new WorkbenchController(service, announcer)
        {
            FocusedStackId = service.Current.FindStackOf("explore")!.Id,
            FocusedSurfaceId = "explore",
        };
        return (controller, announcer, service);
    }

    [Fact]
    public void ARealAnnouncer_AcceptsAnAnnouncementFromABackgroundThread()
    {
        // Announcements now arrive from background work — a re-index reports its outcome when it
        // finishes — and the live region is a WPF control, so an unmarshalled call throws exactly
        // when the product is trying to tell the user something.
        //
        // RecordingAnnouncer cannot catch this: it has no dispatcher. Mutation proved it, by
        // removing the marshalling and failing nothing (DC-016).
        Exception? thrown = null;

        var thread = new Thread(() =>
        {
            var liveRegion = new System.Windows.Controls.TextBlock();
            var announcer = new WorkbenchAnnouncer(liveRegion);

            try
            {
                // Off the STA thread that owns the control, which is the whole point.
                Task.Run(() => announcer.Announce("from background work"))
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                thrown = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "the STA thread did not finish");

        Assert.Null(thrown);
    }

    // ---- workspace.refresh: a write, reached from the palette ---------------

    [Fact]
    public async Task Refresh_AnnouncesThatItStarted_AndThenWhatHappened()
    {
        // Announced twice on purpose. Re-indexing takes as long as it takes, and a command that
        // acknowledged nothing until it finished would be indistinguishable from a key that never
        // registered.
        var (controller, announcer, _) = Build();
        controller.WorkspaceRefresh = () => Task.FromResult("Re-indexed: 12 assertion(s).");

        Assert.True(controller.Execute("workspace.refresh"));
        Assert.Contains(announcer.Messages, a => a.Contains("Re-indexing", StringComparison.Ordinal));

        await WaitForAnnouncement(announcer, "12 assertion");
    }

    [Fact]
    public async Task Refresh_AnnouncesAFailure_RatherThanFallingSilent()
    {
        // The outcome a user most needs told. A re-index that failed silently leaves the previous
        // evidence rendering with nothing to say it is now stale.
        var (controller, announcer, _) = Build();
        controller.WorkspaceRefresh = () => Task.FromException<string>(
            new InvalidOperationException("the extractor could not read the repository"));

        Assert.True(controller.Execute("workspace.refresh"));

        await WaitForAnnouncement(announcer, "failed");
    }

    [Fact]
    public void Refresh_WithNoWorkspace_SaysSoInsteadOfDoingNothing()
    {
        // A palette command that silently does nothing is exactly what the catalog conformance test
        // exists to prevent, and "no workspace" is the state where it is easiest to ship.
        var (controller, announcer, _) = Build();

        Assert.True(controller.Execute("workspace.refresh"));
        Assert.Contains(
            announcer.Messages,
            a => a.Contains("nothing to re-index", StringComparison.Ordinal));
    }

    private static async Task WaitForAnnouncement(RecordingAnnouncer announcer, string fragment)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (announcer.Messages.Any(a => a.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.Fail($"nothing announced containing '{fragment}'; heard: {string.Join(" | ", announcer.Messages)}");
    }

    // The headline: every catalog command announces something when invoked.
    [Fact]
    public void EveryCatalogCommand_Announces()
    {
        foreach (var command in WorkbenchCommandCatalog.All)
        {
            var (controller, announcer, _) = Build();

            var handled = controller.Execute(command.Id);

            Assert.True(handled, $"{command.Id} was not handled");
            Assert.False(string.IsNullOrWhiteSpace(announcer.Last),
                $"{command.Id} completed without announcing anything");
        }
    }

    [Fact]
    public void UnknownCommand_IsNotHandled()
    {
        var (controller, _, _) = Build();
        Assert.False(controller.Execute("workbench.notARealCommand"));
    }

    // A refusal is information. Silence is indistinguishable from a dead key.
    [Fact]
    public void ARefusedOperation_IsStillAnnounced()
    {
        var (controller, announcer, service) = Build();
        service.IsLocked = true;

        controller.Execute("workbench.floatPane");

        Assert.Contains("locked", announcer.Last, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToggleLock_AnnouncesBothDirections()
    {
        var (controller, announcer, service) = Build();

        controller.Execute("workbench.toggleLock");
        Assert.True(service.IsLocked);
        Assert.Contains("locked", announcer.Last, StringComparison.OrdinalIgnoreCase);

        controller.Execute("workbench.toggleLock");
        Assert.False(service.IsLocked);
        Assert.Contains("unlocked", announcer.Last, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MaximizeCommand_TogglesOnTheSameKey()
    {
        var (controller, _, service) = Build();
        var stackId = controller.FocusedStackId!;

        controller.Execute("workbench.maximizePane");
        Assert.Equal(StackState.Maximized,
            service.Current.AllStacks().First(s => s.Id == stackId).State);

        controller.Execute("workbench.maximizePane");
        Assert.Equal(StackState.Docked,
            service.Current.AllStacks().First(s => s.Id == stackId).State);
    }

    [Fact]
    public void NextSurface_CyclesWithinTheFocusedPane()
    {
        var (controller, _, service) = Build();
        var stackId = controller.FocusedStackId!;
        var before = service.Current.AllStacks().First(s => s.Id == stackId).ActiveIndex;

        controller.Execute("workbench.nextSurface");

        var after = service.Current.AllStacks().First(s => s.Id == stackId).ActiveIndex;
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void CommandsWithoutAFocusedPane_SaySoRatherThanFailingSilently()
    {
        var service = new LayoutService();
        var announcer = new RecordingAnnouncer();
        var controller = new WorkbenchController(service, announcer);   // nothing focused

        controller.Execute("workbench.floatPane");

        Assert.Contains("No pane is focused", announcer.Last, StringComparison.Ordinal);
    }

    // The keyboard resize interaction, end to end through the controller.
    [Fact]
    public void ResizeSession_AnnouncesEachStepAndCancelsBackToTheStart()
    {
        var (controller, announcer, service) = Build();
        var before = service.Current.Shape();

        controller.Execute("workbench.resizePane");
        Assert.True(controller.IsResizing);
        Assert.Contains("Arrow keys", announcer.Last, StringComparison.Ordinal);

        Assert.True(controller.HandleResizeKey(System.Windows.Input.Key.Right));
        Assert.NotEqual(before, service.Current.Shape());

        Assert.True(controller.HandleResizeKey(System.Windows.Input.Key.Escape));
        Assert.Equal(before, service.Current.Shape());
        Assert.False(controller.IsResizing);
        Assert.Contains("cancelled", announcer.Last, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResizeKeys_AreIgnoredWhenNotResizing()
    {
        var (controller, _, _) = Build();
        Assert.False(controller.HandleResizeKey(System.Windows.Input.Key.Right));
    }

    // The move path a drop will also use — the same operation, so the two paths cannot diverge.
    [Fact]
    public void Move_AppliesAndAnnounces()
    {
        var (controller, announcer, service) = Build();
        var target = service.Current.FindStackOf("provenance")!.Id;

        controller.Move("explore", new DropTarget(target, DropKind.JoinStack));

        Assert.Contains("moved", announcer.Last, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(target, service.Current.FindStackOf("explore")!.Id);
    }
}

/// <summary>
/// The pointer path through the controller (1b.10). The point of these is that the drag and the
/// keyboard reach the same place through the same operation — the preview the user sees is derived
/// from the very target the drop will apply.
/// </summary>
public sealed class WorkbenchDragTests
{
    private static readonly LayoutRect PaneBounds = new(0, 0, 400, 300);

    private static (WorkbenchController C, RecordingAnnouncer A, ILayoutService S, PaneHitBox[] P) Build()
    {
        var service = new LayoutService();
        var announcer = new RecordingAnnouncer();
        var controller = new WorkbenchController(service, announcer);
        var stackId = service.Current.FindStackOf("provenance")!.Id;
        return (controller, announcer, service, [new PaneHitBox(stackId, PaneBounds, 28)]);
    }

    [Fact]
    public void DragOver_AnnouncesTheDestinationBeforeAnyDrop()
    {
        var (controller, announcer, service, panes) = Build();
        var before = service.Current.Shape();

        var target = controller.DragOver(panes, new LayoutPoint(10, 150));

        Assert.Equal(DropKind.SplitLeft, target!.Kind);
        Assert.Contains("Destination: split, left of", announcer.Last, StringComparison.Ordinal);
        // Hovering must not change anything yet — the user has not released.
        Assert.Equal(before, service.Current.Shape());
    }

    // A screen reader flooded with the same sentence on every mouse-move hears nothing useful.
    [Fact]
    public void DragOver_AnnouncesOnlyWhenTheDestinationChanges()
    {
        var (controller, announcer, _, panes) = Build();

        controller.DragOver(panes, new LayoutPoint(10, 150));
        controller.DragOver(panes, new LayoutPoint(12, 152));
        controller.DragOver(panes, new LayoutPoint(14, 148));
        Assert.Single(announcer.Messages);

        controller.DragOver(panes, new LayoutPoint(390, 150));
        Assert.Equal(2, announcer.Messages.Count);
    }

    [Fact]
    public void ThePreview_IsDerivedFromTheTargetTheDropWillApply()
    {
        var (controller, _, _, panes) = Build();

        controller.DragOver(panes, new LayoutPoint(390, 150));

        Assert.Equal(DropKind.SplitRight, controller.HoveredTarget!.Kind);
        Assert.Equal(new LayoutRect(200, 0, 200, 300), controller.HoveredPreview);
    }

    [Fact]
    public void Drop_AppliesTheHoveredDestination()
    {
        var (controller, _, service, panes) = Build();
        controller.DragOver(panes, new LayoutPoint(200, 150));   // centre → join

        Assert.True(controller.Drop("explore"));

        Assert.Equal(panes[0].StackId, service.Current.FindStackOf("explore")!.Id);
        Assert.Null(controller.HoveredTarget);
        service.Current.AssertInvariant();
    }

    [Fact]
    public void CancelDrag_LeavesTheLayoutUntouchedAndSaysSo()
    {
        var (controller, announcer, service, panes) = Build();
        var before = service.Current.Shape();
        controller.DragOver(panes, new LayoutPoint(10, 150));

        controller.CancelDrag();

        Assert.Equal(before, service.Current.Shape());
        Assert.Null(controller.HoveredTarget);
        Assert.Contains("cancelled", announcer.Last, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Drop_WithNoHoveredDestination_DoesNothing()
    {
        var (controller, _, service, _) = Build();
        var before = service.Current.Shape();

        Assert.False(controller.Drop("explore"));
        Assert.Equal(before, service.Current.Shape());
    }

    [Fact]
    public void ALockedLayout_OffersNoDestinationAndExplainsWhy()
    {
        var (controller, announcer, service, panes) = Build();
        service.IsLocked = true;

        var target = controller.DragOver(panes, new LayoutPoint(200, 150));

        Assert.Null(target);
        Assert.Contains("locked", announcer.Last, StringComparison.OrdinalIgnoreCase);
        Assert.False(controller.Drop("explore"));
    }
}
