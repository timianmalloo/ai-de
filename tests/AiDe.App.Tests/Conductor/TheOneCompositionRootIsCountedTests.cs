using AiDe.App.Conductor;
using AiDe.App.Tests.Sessions;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Conductor;

/// <summary>
/// §F5 clause 5: a governed run is launched through the same composition root, and the claim is a
/// number rather than a sentence.
/// </summary>
/// <remarks>
/// <para><b>The counter is shown discriminating in both directions.</b> A ledger that only ever
/// reads what the happy path produces is a ledger nobody has seen fail: the first case here drives
/// two refused runs and reads <c>2</c>, the second drives a real lane's worth of events through a
/// real <see cref="SessionLane"/> with no root at all and reads <c>0</c>. Together they say the
/// number tracks compositions and not activity in general (DC-104).</para>
///
/// <para><b>No adapter, no node, no network — and that is the design.</b>
/// <see cref="CompositionRootLedger"/> counts the attempt, so an <c>engineId</c> the catalog does
/// not carry still registers its root before <c>EngineCatalog.ResolveLaunch</c> refuses. A falsifier
/// that needed a live subscription run would be one nobody re-runs.</para>
/// </remarks>
public sealed class TheOneCompositionRootIsCountedTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(20);

    /// <summary>A request the catalog will refuse — everything else about it is well formed.</summary>
    internal static GovernedRunRequest RefusedRequest(string engineId = "no-such-engine") => new(
        RepositoryRoot: Path.GetTempPath(),
        DataDirectory: Path.GetTempPath(),
        AdapterInstallRoot: Path.GetTempPath(),
        EngineId: engineId,
        Model: "claude-opus-5",
        AccountLabel: "max",
        TaskClass: "wiring",
        Goal: new GoalBlock("wire the seam", "the run happens", "everything else", "T2", 1, null),
        Lease: new Lease(["src/AiDe.App/Conductor/**"]),
        Prompt: "wire the seam",
        ProofPackArtifacts: [],
        Providers: []);

    [Fact]
    public async Task TwoRefusedRunsStillCountTwoRoots()
    {
        using var ledger = CompositionRootLedger.Open();

        for (var i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<AgentPlaneException>(
                () => GovernedRunHost.RunAsync(RefusedRequest()));
        }

        Assert.Equal(2, ledger.Roots);
    }

    [Fact]
    public async Task EventsFlowingWithNoRootCountNoRoots()
    {
        var model = new SessionDocumentViewModel(
            "20260911T120000Z-nolaunch", "No root", Path.GetTempPath(), [CanvasModeCatalog.ConsoleModeId]);

        using var ledger = CompositionRootLedger.Open();
        using var lane = new RealLane("run-noroot", "lane-1");
        using var feed = new SessionLane("lane-1", "claude-code", lane.Events, model);

        lane.Say("one");
        lane.Say("two");

        Assert.True(
            await feed.WaitForDeliveredAsync(2, Bound),
            $"the lane delivered {feed.Delivered} of 2 events");

        // Events moved, rows landed, and no run was composed. Without this half the assertion above
        // would pass on a ledger that counts nothing at all.
        Assert.Equal(2, model.Dispatched);
        Assert.Equal(0, ledger.Roots);
    }
}
