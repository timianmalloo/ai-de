using System.Windows.Controls;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// ADR-0017's retain-never-rebuild invariant, applied to canvas modes (R16 b2) — with an oracle
/// that can detect disposal.
/// </summary>
/// <remarks>
/// <para><b><c>Assert.Same</c> passes on a disposed instance.</b> ADR-0017's own comment claims a
/// reference check goes red if the subject was "recreated <i>or disposed</i>"; that is false for the
/// disposed half, and the ADR is <c>status: proposed</c> about the shell body swap rather than about
/// canvas modes. The mechanism transfers, the proof does not — so this test asserts three things
/// together, and ships a falsifier that shows all three going red on the rebuild path.</para>
///
/// <para><b>What is exercised.</b> A <b>real lane</b> (<see cref="RealLane"/>: the production
/// <c>AcpPeer</c>, <c>AcpRunEventMapper</c> and <c>AcpEventQueue</c> over a stream the test writes
/// into) delivers events while the document does a <b>mode switch</b> and a <b>tab switch</b>. The
/// tab switch is modelled the way the docking host does one: the document is unparented from its
/// host and re-parented, which is a view change and must not be a session loss.</para>
///
/// <para><b>The three assertions.</b> <c>Assert.Same</c> on the console surface <i>and</i> on the
/// lane; <see cref="SessionDisposalLedger"/> total of zero; and no gap in the lane's delivered
/// ordinals. The third is the one only a live lane can pose: a rebuilt console starts its history
/// wherever it was built, so every ordinal before that is missing.</para>
/// </remarks>
public sealed class ModeSwitchRetainsTheSurfaceAndTheLaneTests
{
    /// <summary>Events delivered before the switches, so the rebuild has real history to lose.</summary>
    private const int Settled = 3;

    /// <summary>Events pushed while the switches happen — the "in flight" half.</summary>
    private const int InFlight = 21;

    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(30);

    /// <summary>What one exercise observed. Captured inside the pump, asserted outside it.</summary>
    private sealed record Observed(
        object ConsoleBefore,
        object ConsoleAfter,
        SessionLane LaneBefore,
        SessionLane LaneAfter,
        long Disposals,
        IReadOnlyList<long> Gaps,
        int Rows);

    [Fact]
    public void AModeSwitchAndATabSwitchRetainTheConsoleAndItsLane()
    {
        var observed = Drive(rebuild: false);

        // 1. The same console surface, and the same lane.
        Assert.Same(observed.ConsoleBefore, observed.ConsoleAfter);
        Assert.Same(observed.LaneBefore, observed.LaneAfter);

        // 2. Nothing was disposed — the half Assert.Same cannot see.
        Assert.Equal(0, observed.Disposals);

        // 3. No event was lost while the switches happened.
        Assert.Empty(observed.Gaps);

        // And the exercise really carried traffic: all three above would pass over an empty stream
        // (DC-016).
        Assert.Equal(Settled + InFlight, observed.Rows);
    }

    /// <summary>
    /// The companion falsifier: the rebuild path, with the <b>same three oracles</b> run against it,
    /// each of which must go red.
    /// </summary>
    /// <remarks>
    /// Without this the clause above is <c>Assert.Same</c> wearing a live lane as a costume: three
    /// green assertions are evidence about the implementation only if they are known to be capable
    /// of failing. Each is run through <c>Record.Exception</c> and its failure is asserted, so the
    /// redness is observed by the suite on every run rather than described once in a report.
    /// </remarks>
    [Fact]
    public void TheRebuildPathTurnsAllThreeOraclesRed()
    {
        var observed = Drive(rebuild: true);

        var sameSurface = Record.Exception(() => Assert.Same(observed.ConsoleBefore, observed.ConsoleAfter));
        var sameLane = Record.Exception(() => Assert.Same(observed.LaneBefore, observed.LaneAfter));
        var noDisposals = Record.Exception(() => Assert.Equal(0, observed.Disposals));
        var noGaps = Record.Exception(() => Assert.Empty(observed.Gaps));

        Assert.NotNull(sameSurface);
        Assert.NotNull(sameLane);
        Assert.NotNull(noDisposals);
        Assert.NotNull(noGaps);

        // Named, so a reader of a failure here sees which oracle stopped discriminating.
        Assert.True(observed.Disposals > 0, "the rebuild path disposed nothing, so the counter is inert");
        Assert.NotEmpty(observed.Gaps);
        Assert.True(observed.LaneBefore.IsDisposed, "the rebuilt lane's predecessor was left running");
    }

    private static Observed Drive(bool rebuild)
    {
        Observed? observed = null;
        ContentControl host = null!;

        var model = NewModel();

        Sta.Pump(
            create: () => new SessionDocumentSurface(model),
            content: document => host = new ContentControl { Content = document },
            body: async (_, document) =>
            {
                using var ledger = SessionDisposalLedger.Open();
                using var lane = new RealLane("run-retain", "lane-1");

                var feed = new SessionLane(
                    "lane-1", "claude-code", lane.Events, model,
                    marshal: work => document.Dispatcher.Invoke(work));
                document.AttachLane(feed);

                var consoleBefore = document.ContentFor(CanvasModeCatalog.ConsoleModeId);

                // Settle first, so the rebuild has history to lose. Without this the falsifier could
                // pass for the wrong reason — a rebuild that happened before anything arrived loses
                // nothing and shows no gap.
                for (var i = 0; i < Settled; i++)
                {
                    lane.Say($"settled {i}");
                }

                Assert.True(
                    await feed.WaitForDeliveredAsync(Settled, Bound),
                    $"the lane delivered {feed.Delivered} of {Settled} before the switches");

                // Events in flight, on their own thread, for the whole of the switching below.
                var producer = Task.Run(async () =>
                {
                    for (var i = 0; i < InFlight; i++)
                    {
                        lane.Say($"in flight {i}");
                        await Task.Delay(2);
                    }
                });

                var activeModel = model;
                var currentFeed = feed;
                var consoleAfter = consoleBefore;

                // THE MODE SWITCH — Console to Terminal and back, with real content on both sides.
                model.SetActiveMode(CanvasModeCatalog.TerminalModeId);

                if (rebuild)
                {
                    // The rebuild path: the switch throws away what it was showing and builds it
                    // again over fresh state, which is precisely what ADR-0017 forbids.
                    (consoleBefore as IDisposable)?.Dispose();
                    currentFeed.Dispose();

                    activeModel = NewModel();
                    consoleAfter = new ConsoleSurface(activeModel.Console);
                    currentFeed = new SessionLane(
                        "lane-1", "claude-code", lane.Events, activeModel,
                        marshal: work => document.Dispatcher.Invoke(work));
                }

                // THE TAB SWITCH — unparented from its host and re-parented, as the docking host
                // does when another document tab is selected.
                host.Content = null;
                host.Content = document;

                model.SetActiveMode(CanvasModeCatalog.ConsoleModeId);

                await producer;

                if (rebuild)
                {
                    // The rebuilt lane can only see what is left in the queue, so the wait is for
                    // "something arrived", not for the whole stream — the point is the gap.
                    Assert.True(
                        await currentFeed.WaitForDeliveredAsync(1, Bound),
                        "the rebuilt lane delivered nothing at all, so the gap oracle proves nothing");
                }
                else
                {
                    Assert.True(
                        await currentFeed.WaitForDeliveredAsync(Settled + InFlight, Bound),
                        $"the lane delivered {currentFeed.Delivered} of {Settled + InFlight}");
                }

                observed = new Observed(
                    consoleBefore,
                    rebuild ? consoleAfter : document.ContentFor(CanvasModeCatalog.ConsoleModeId),
                    feed,
                    currentFeed,
                    ledger.Total,
                    activeModel.Console.OrdinalGaps("lane-1"),
                    activeModel.Console.Rows.Count);
            },
            timeoutSeconds: 90);

        Assert.NotNull(observed);
        return observed;
    }

    private static SessionDocumentViewModel NewModel() => new(
        "20260910T120000Z-deadbeef",
        "Front door",
        Path.GetTempPath(),
        [CanvasModeCatalog.ConsoleModeId, CanvasModeCatalog.TerminalModeId]);
}
