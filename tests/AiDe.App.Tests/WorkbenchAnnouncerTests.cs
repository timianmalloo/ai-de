using System.Windows.Controls;
using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The status strip is one line; a long announcement (a re-index reports 200+ disclosures) must not
/// grow it. The full text stays available on hover via the tooltip.
/// </summary>
public sealed class WorkbenchAnnouncerTests
{
    private static void OnSta(Action work)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { work(); } catch (Exception ex) { failure = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "STA thread did not finish");
        if (failure is Xunit.Sdk.XunitException) throw failure;   // the message IS the finding (DC-078)

        if (failure is not null) { throw new InvalidOperationException("STA work failed", failure); }
    }

    [Fact]
    public void Announce_PutsALongMessageInTheTooltip_SoTheOneLineStripCanCarryIt()
    {
        OnSta(() =>
        {
            var region = new TextBlock();
            var announcer = new WorkbenchAnnouncer(region);
            var longMessage = "Indexed 64 of 64 scope(s): 29,314 assertion(s). Not analysed: " +
                string.Join(", ", Enumerable.Range(0, 40).Select(i => $"reason-{i}")) + ".";

            announcer.Announce(longMessage);

            Assert.Equal(longMessage, region.Text);        // the full text is still the announced/AT value
            Assert.Equal(longMessage, region.ToolTip);     // …and available on hover for the truncated strip
        });
    }

    [Fact]
    public void Announce_LeavesShortMessagesWithoutATooltip()
    {
        OnSta(() =>
        {
            var region = new TextBlock();
            var announcer = new WorkbenchAnnouncer(region);

            announcer.Announce("Class diagram opened.");

            Assert.Equal("Class diagram opened.", region.Text);
            Assert.Null(region.ToolTip);   // a short status line needs no hover-for-more
        });
    }
}
