using System.Windows;
using System.Windows.Automation;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Kinds;

/// <summary>
/// The WL-S design's §4: the <c>execution</c> kind is Factory's, one per attempt, docks in the Bottom, carries the
/// caption <i>Execution — &lt;attempt&gt;</i> as its UIA name, and is opened by a turn's "Open the log" — never by a
/// derived menu entry or a catalog verb.
/// </summary>
public sealed class TheExecutionKindTests
{
    [Fact]
    public void TheExecutionRow_IsFactorysOnly_OnePerAttempt_DocksInTheBottom_OpenedByTheTurnsOpenLog()
    {
        var row = Assert.Single(SurfaceContentFactory.Kinds, k => k.Kind == "execution");
        Assert.Equal(["factory"], row.Perspectives.Select(p => p.Id));
        Assert.Equal(SurfaceContentFactory.Instances.Many, row.Instances);
        Assert.Equal(ZoneId.Bottom, row.Zone);

        // The opener is a turn's action on the notebook, so no View-menu entry and no catalog verb is a second door.
        Assert.Equal("TurnAction", row.Entry.GetType().Name);
        Assert.Equal("TurnAction { Action = OpenLog }", row.Entry.ToString());

        Assert.True(DockHost.AdmissionFor(PerspectiveSet.Factory).Admits("execution"));
        Assert.False(DockHost.AdmissionFor(PerspectiveSet.Understand).Admits("execution"));
        Assert.False(DockHost.AdmissionFor(PerspectiveSet.Review).Admits("execution"));
    }

    [Fact]
    public void AnExecutionSurface_IsNamedForItsAttempt_InUia()
    {
        var name = Sta.Run(() =>
        {
            var content = new SurfaceContentFactory(null).Create(new Surface("execution:dk-1", "execution", "Execution — dk-1"));
            return AutomationProperties.GetName(Unwrap(content));
        }, 30);

        Assert.Equal("Execution — dk-1", name);
    }

    private static DependencyObject Unwrap(FrameworkElement content) =>
        content is System.Windows.Controls.ContentControl { Content: FrameworkElement inner } && AutomationProperties.GetName(content) is "" ? inner : content;
}
