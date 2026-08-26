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
