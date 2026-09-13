using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests;

/// <summary>
/// Ruling 86: a refusal reads <c>WorkbenchAnnouncer.Clear</c>'s own remark — "a status message has
/// no natural end — it sits there until something else happens" — literally, because nothing else
/// DID happen after a refused native drag (the reconcile's success path announces nothing). A status
/// (refusal included) now clears on the next announcement, on the next applied layout operation
/// (which announces, including its own refusal), or after a bounded dwell — proven here with a short
/// test dwell rather than a real ten-second sleep.
/// </summary>
public sealed class TheStatusStripClearsOnSupersessionOrDwellTests
{
    private const int PumpTimeoutMs = 5000;

    /// <summary>Pumps the current thread's dispatcher (Background priority, where the dwell timer ticks) until <paramref name="until"/> is true or the timeout elapses.</summary>
    private static void PumpUntil(Func<bool> until)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(PumpTimeoutMs);
        while (!until() && DateTime.UtcNow < deadline)
        {
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
            Thread.Sleep(5);
        }
    }

    [Fact]
    public void ARefusal_ClearsItselfAfterTheBoundedDwell()
    {
        Sta.Run(() =>
        {
            var strip = new TextBlock();
            var announcer = new WorkbenchAnnouncer(strip, TimeSpan.FromMilliseconds(30));

            announcer.Announce(new Announcement(
                "That pane move could not be applied — a collapsed panel still holds panes. "
                + "Expand it and move the pane again.",
                Urgency.Status, AnnouncementKind.Aborted));

            Assert.Contains("could not be applied", announcer.Last, StringComparison.Ordinal);

            PumpUntil(() => announcer.Last.Length == 0);

            Assert.Equal(string.Empty, announcer.Last);
            Assert.Equal(string.Empty, strip.Text);
        });
    }

    [Fact]
    public void ASupersedingAnnouncement_ClearsTheRefusalImmediately_BeforeTheDwellElapses()
    {
        Sta.Run(() =>
        {
            var strip = new TextBlock();
            // A dwell long enough that the test would fail if it were the mechanism doing the work.
            var announcer = new WorkbenchAnnouncer(strip, TimeSpan.FromSeconds(30));

            announcer.Announce("That pane move could not be applied, so the panes will return to where they were.");
            Assert.Contains("could not be applied", announcer.Last, StringComparison.Ordinal);

            announcer.Announce("Moved terminal to Bottom.");

            Assert.Equal("Moved terminal to Bottom.", announcer.Last);
            Assert.Equal("Moved terminal to Bottom.", strip.Text);
        });
    }

    /// <summary>The manual <c>workbench.clearStatus</c> path is unchanged (Ruling 86's condition).</summary>
    [Fact]
    public void ManualClear_StillEmptiesTheLineImmediately()
    {
        Sta.Run(() =>
        {
            var strip = new TextBlock();
            var announcer = new WorkbenchAnnouncer(strip, TimeSpan.FromSeconds(30));

            announcer.Announce("some status");
            announcer.Clear();

            Assert.Equal(string.Empty, announcer.Last);
            Assert.Equal(string.Empty, strip.Text);
        });
    }

    /// <summary>The dwell never truncates the announcement itself — only the strip empties, afterwards.</summary>
    [Fact]
    public void TheDwell_NeverShortensTheAnnouncedText()
    {
        Sta.Run(() =>
        {
            var strip = new TextBlock();
            var announcer = new WorkbenchAnnouncer(strip, TimeSpan.FromMilliseconds(20));
            const string full = "Re-indexed: 12,400 assertion(s). Reopen a pane to see them.";

            announcer.Announce(full);

            Assert.Equal(full, announcer.Last);
            Assert.Equal(full, strip.Text);
        });
    }
}
