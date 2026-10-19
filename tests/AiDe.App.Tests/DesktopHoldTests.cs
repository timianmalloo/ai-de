namespace AiDe.App.Tests;

public sealed class DesktopHoldTests
{
    [Fact]
    public void OccupantOf_SeesAHoldThatAnnounceStartWrote()
    {
        var occupant = DesktopHold.OccupantOf([DesktopHold.StartedAnnouncement("wl-t13", "run-7", 4242)]);

        Assert.NotNull(occupant);
        Assert.Contains("wl-t13", occupant);
        Assert.Contains("ActualPID4242", occupant);
    }

    [Fact]
    public void OccupantOf_ReleasesTheHoldWhenTheEndIsAnnounced()
    {
        var occupant = DesktopHold.OccupantOf(
        [
            DesktopHold.StartedAnnouncement("wl-t13", "run-7", 4242),
            """{"session":"wl-t13","contract":"R115 desktop run-7 END RELEASED","reason":"x"}""",
        ]);

        Assert.Null(occupant);
    }
}
