using AiDe.App.Workbench.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// R16 b3: a permission request surfaces within one event cycle, regardless of the active canvas
/// mode.
/// </summary>
/// <remarks>
/// <para><b>"Within one event cycle" is an ordinal here, never a duration.</b> There is no clock
/// reading that separates a correct implementation from a lucky one, and a <c>&lt; N ms</c>
/// assertion is a wall-clock budget that <c>tools/verify-perf-assertions.py</c> refuses (DC-107).
/// The observable asserted instead is arithmetic: the request was raised while the document's
/// dispatch count was still the permission event's own, and the queue then went on to dispatch
/// more.</para>
///
/// <para><b>Parameterised over the active mode, because that was the defect.</b> Written red-first
/// against an implementation that raised the request only while Console was active: the Console
/// case passed and the Terminal case failed with <c>Assert.True(model.Permission.IsRaised)</c>,
/// which is exactly the shape of the bug R16 b3's "regardless of active mode" exists to prevent —
/// the overlay appearing only when you happen to switch back.</para>
///
/// <para><b>The event is a real <c>session/request_permission</c> frame</b> pushed through the
/// production peer and mapper (<see cref="RealLane"/>), not a hand-made <c>RunEvent</c>: the kind
/// under test is one the mapper assigns, so a synthetic envelope would be asserting about the test's
/// own spelling of it.</para>
/// </remarks>
public sealed class APermissionRequestSurfacesBeforeTheNextEventTests
{
    /// <summary>The permission frame is the second of four, so its ordinal and dispatch index are 2.</summary>
    private const long PermissionOrdinal = 2;

    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(20);

    [Theory]
    [InlineData(CanvasModeCatalog.ConsoleModeId)]
    [InlineData(CanvasModeCatalog.TerminalModeId)]
    public async Task ThePermissionIsObservableBeforeTheQueueDequeuesTheNextEvent(string activeMode)
    {
        var model = new SessionDocumentModel(
            "20260910T120000Z-deadbeef",
            "Front door",
            Path.GetTempPath(),
            availableModes: [CanvasModeCatalog.ConsoleModeId, CanvasModeCatalog.TerminalModeId]);

        model.SetActiveMode(activeMode);

        using var lane = new RealLane("run-permission", "lane-1");
        using var feed = new SessionLane("lane-1", "claude-code", lane.Events, model);

        lane.Say("reading the payments aggregate");
        lane.AskPermission("Write src/Payments/PaymentAggregate.cs?");
        lane.Say("writing");
        lane.Say("done");

        Assert.True(
            await feed.WaitForDeliveredAsync(4, Bound),
            $"the lane delivered {feed.Delivered} of 4 events");

        // 1. It is showing at all.
        Assert.True(model.Permission.IsRaised, $"no permission surfaced in {activeMode} mode");

        // 2. It is the permission event's own ordinal — not some later event's.
        Assert.Equal(PermissionOrdinal, model.Permission.RaisedAtOrdinal);

        // 3. THE CLAUSE. The document had dispatched exactly the permission event and nothing after
        //    it at the moment the request became observable, so nothing was dequeued in between.
        Assert.Equal(PermissionOrdinal, model.Permission.DispatchedWhenRaised);

        // 4. And the queue really did carry on — without this the three above would also pass on a
        //    stream that simply ended at the permission request (DC-016).
        Assert.Equal(4, model.Dispatched);
    }

    [Fact]
    public async Task NothingIsRaisedBeforeARequestArrives()
    {
        // The "not recorded" half: absent reads as -1, which no real ordinal can be, rather than 0 —
        // which would read as "raised before the first event".
        var model = new SessionDocumentModel(
            "20260910T120000Z-deadbeef", "Front door", Path.GetTempPath());

        using var lane = new RealLane("run-quiet", "lane-1");
        using var feed = new SessionLane("lane-1", "claude-code", lane.Events, model);

        lane.Say("nothing to ask about");
        Assert.True(await feed.WaitForDeliveredAsync(1, Bound), "the lane delivered nothing");

        Assert.False(model.Permission.IsRaised);
        Assert.Equal(SessionPermissionSurface.NotRaised, model.Permission.RaisedAtOrdinal);
        Assert.Equal(SessionPermissionSurface.NotRaised, model.Permission.DispatchedWhenRaised);
    }
}
