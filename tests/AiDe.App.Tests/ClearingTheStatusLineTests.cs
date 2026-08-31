using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The status line can be put away, and putting it away still says so.
/// </summary>
/// <remarks>
/// <para>Asked for after a real index filled roughly four fifths of the window with a message the
/// user could not dismiss. A status message has no natural end — it sits there until something else
/// happens, and the longest one is usually the last one.</para>
///
/// <para><b>Clearing announces a short confirmation rather than nothing.</b> Silence was the first
/// attempt and `EveryCatalogCommand_Announces` refused it. That control is right: a command that does
/// its work without saying so is indistinguishable from a dead key (DC-011), and for a screen-reader
/// user it IS one — SC 4.1.3 Status Messages exists for this. The complaint was never that the line
/// exists, it was that it had grown to fill the window.</para>
/// </remarks>
public sealed class ClearingTheStatusLineTests
{
    private static (WorkbenchController Controller, RecordingAnnouncer Announcer) Build()
    {
        var announcer = new RecordingAnnouncer();
        return (new WorkbenchController(new LayoutService(), announcer), announcer);
    }

    [Fact]
    public void ClearingReplacesAWallOfTextWithOneShortLine()
    {
        var (controller, announcer) = Build();

        controller.Execute("workspace.diagnostics");
        announcer.Announce(new string('x', 4_000));

        Assert.True(controller.Execute("workbench.clearStatus"));

        Assert.Single(announcer.Messages);
        Assert.True(announcer.Last.Length < 40,
            $"clearing left {announcer.Last.Length} characters on the status line");
    }

    [Fact]
    public void ClearingStillSaysSomething()
    {
        // The accessibility floor. A user who cannot see the line has only the announcement to tell
        // them the command did anything at all.
        var (controller, announcer) = Build();
        announcer.Announce("something long and unwanted");

        controller.Execute("workbench.clearStatus");

        Assert.NotEmpty(announcer.Last);
    }

    [Fact]
    public void ClearingAnAlreadyEmptyLineIsHarmless()
    {
        // A user who presses it twice is not doing anything wrong, and must not be told they are.
        var (controller, announcer) = Build();

        controller.Execute("workbench.clearStatus");
        controller.Execute("workbench.clearStatus");

        Assert.Single(announcer.Messages);
    }

    [Fact]
    public void TheCommandIsReachableFromTheCatalogRatherThanOnlyByChord()
    {
        // A command nobody can find is a command nobody has. The catalog is what the palette and the
        // menu both read.
        var command = Assert.Single(
            AiDe.Core.Workbench.WorkbenchCommandCatalog.All, c => c.Id == "workbench.clearStatus");

        Assert.False(string.IsNullOrWhiteSpace(command.Gesture));
        Assert.Equal("_View", command.Menu);
    }

    [Fact]
    public void ANewAnnouncementStillReplacesTheClearedLine()
    {
        // Clearing must not leave the line stuck: the next thing that happens is what it should say.
        var (controller, announcer) = Build();

        controller.Execute("workbench.clearStatus");
        announcer.Announce("Indexed 64 of 64 scope(s).");

        Assert.Equal("Indexed 64 of 64 scope(s).", announcer.Last);
    }
}
