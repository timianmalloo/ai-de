using System.Text.RegularExpressions;
using AiDe.App.Conductor;
using AiDe.App.Tests.Sessions;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Conductor;

/// <summary>
/// A governed run's events reach a Console surface in the shipped product, not only in a test — and
/// the console receives exactly the events the run counted.
/// </summary>
/// <remarks>
/// <para><b>Red first, and the red was mechanical.</b> <see cref="SessionLane"/> — the type whose
/// entire job is to drain the plane's queue into a session document — was constructed in nine
/// places, all of them test files, and in <b>no</b> file under <c>src/</c>. Every assertion about
/// the console rendering a merged stream was therefore an assertion about wiring the tests
/// assembled themselves, which is exactly what <c>GovernedRunHost</c>'s own remarks refuse: <i>a
/// hand-assembled harness does not count as exit evidence</i>.</para>
///
/// <para><b>A source scan rather than a behavioural assertion, for the first half.</b> The
/// behavioural test can be satisfied by a lane the test built; only counting construction sites in
/// the product can tell "the product wires this" from "the suite wires this".</para>
/// </remarks>
public sealed class AGovernedRunReachesTheConsoleTests
{
    private const int Frames = 5;

    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(20);

    private static DirectoryInfo RepoRoot()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);

        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AiDe.sln")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        return root!;
    }

    private static List<string> FilesUnder(string directory, string pattern)
    {
        var root = RepoRoot();
        var path = Path.Combine(root.FullName, directory);
        var hits = new List<string>();

        if (!Directory.Exists(path))
        {
            return hits;
        }

        foreach (var file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            if (Regex.IsMatch(File.ReadAllText(file), pattern))
            {
                hits.Add(Path.GetRelativePath(root.FullName, file));
            }
        }

        return hits;
    }

    [Fact]
    public void TheProductItselfConstructsASessionLane()
    {
        var product = FilesUnder("src", @"new SessionLane\s*\(");

        Assert.True(product.Count > 0,
            "no file under src/ constructs a SessionLane, so nothing in the shipped product carries a "
            + "governed run's events to a Console surface — every console assertion in the suite is "
            + "then about wiring the suite built for itself, which GovernedRunHost's own remarks "
            + "refuse as exit evidence.");
    }

    [Fact]
    public void TheScanActuallyFindsLanesSomewhere()
    {
        // The DC-016 guard. If SessionLane is renamed or the construction is wrapped, the assertion
        // above passes by examining nothing — the shape this whole file exists to prevent.
        var anywhere = FilesUnder("src", @"new SessionLane\s*\(").Count
            + FilesUnder("tests", @"new SessionLane\s*\(").Count;

        Assert.True(anywhere > 0,
            "no construction of a SessionLane was found anywhere in src/ or tests/, so the scan above "
            + "is checking an empty set. Either the type was renamed and this control needs updating, "
            + "or it has no callers at all.");
    }

    // ------------------------------------------------------------------- the equality, observed

    /// <summary>
    /// The sink receives <b>exactly</b> the events the run counted, and those events are the rows
    /// the Console renders.
    /// </summary>
    /// <remarks>
    /// <para><b>Exactly, because "about the same number" is a second opinion.</b> The count the run
    /// reports as <c>EventsObserved</c> and the count the console received are one quantity with one
    /// producer (DM7), and that stays true only because the sink is invoked from inside the drain
    /// that does the counting — which is what <c>GovernedRunHost.DrainAsync</c> does and what this
    /// asserts. <see cref="RunEventRelay.Refused"/> is asserted at zero beside it: a relay that
    /// quietly lost one would otherwise make the equality a coincidence of timing.</para>
    ///
    /// <para><b>The drain is the shipped drain and the queue is the shipped queue.</b> What is not
    /// exercised here is a whole <c>RunAsync</c> — that needs an adapter, node, a provisioned
    /// worktree and a scored episode, none of which a unit suite can supply, and it is what §F5's
    /// live exit run is for. The half this suite can hold is split accordingly:
    /// <c>ASendLaunchesAGovernedRunTests</c> proves the product reaches the composition root, and
    /// this proves the contract the root's sink parameter carries once it does.</para>
    ///
    /// <para><b>The prompt completes before the last frame, deliberately.</b> The drain's exit
    /// condition is evaluated when an event arrives, so a prompt answered while the queue is empty
    /// would leave the drain parked rather than finished. Completing it and then pushing one last
    /// frame makes the ending deterministic rather than timed.</para>
    /// </remarks>
    [Fact]
    public void TheSinkReceivesExactlyTheEventsTheRunCountedAndTheConsoleShowsThem()
    {
        var root = Path.Combine(Path.GetTempPath(), "aide-console-seam", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            Sta.Pump(
                create: () => new SessionDocumentSurface(new SessionDocumentViewModel(
                    "20260911T140000Z-console", "Console seam", root, [CanvasModeCatalog.ConsoleModeId])),
                body: async (_, document) =>
                {
                    using var relay = new RunEventRelay();
                    using var source = new RealLane("run-console", "plane-lane");
                    using var feed = new SessionLane(
                        "lane:console", "claude-code", relay.Reader, document.Model,
                        work => document.Dispatcher.Invoke(work));

                    var prompt = new TaskCompletionSource();
                    var seams = new LeaseMonitor(new Lease(["src/**"]), root);
                    var reported = new List<string>();

                    var drain = GovernedRunHost.DrainAsync(
                        source.Queue, prompt.Task, seams, reported.Add, relay.Publish, CancellationToken.None);

                    source.Say("reading the payments aggregate");
                    source.Say("thinking");
                    source.AskPermission("Write src/Payments/PaymentAggregate.cs?");
                    source.Say("writing");

                    Assert.True(
                        await feed.WaitForDeliveredAsync(Frames - 1, Bound),
                        $"the lane delivered {feed.Delivered} of {Frames - 1} events before the prompt answered");

                    prompt.SetResult();
                    source.Say("done");

                    var drained = await drain;

                    // THE EQUALITY. Three readings of one quantity, and they are the same number.
                    Assert.Equal(Frames, drained.Events);
                    Assert.Equal((long)Frames, relay.Published);
                    Assert.Equal(0L, relay.Refused);

                    Assert.True(
                        await feed.WaitForDeliveredAsync(Frames, Bound),
                        $"the lane delivered {feed.Delivered} of {Frames} events");

                    Assert.Equal((long)Frames, feed.Delivered);
                    Assert.Equal((long)Frames, document.Model.Dispatched);

                    // And it reached the SURFACE, not merely the model.
                    var console = (ConsoleSurface)document.ContentFor(CanvasModeCatalog.ConsoleModeId);
                    Assert.Equal(Frames, console.RenderedRows.Count);
                    Assert.All(
                        console.RenderedRows,
                        row => Assert.StartsWith("claude-code:", row, StringComparison.Ordinal));
                });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// With no sink the drain counts exactly what it counted before the parameter existed.
    /// </summary>
    /// <remarks>
    /// The default has to be <i>inert</i>, not merely optional: the headless path names no sink, and
    /// a default that changed the drain's behaviour would change the one path §F5 measures.
    /// </remarks>
    [Fact]
    public async Task WithNoSinkTheDrainStillCountsEveryEvent()
    {
        using var source = new RealLane("run-nosink", "plane-lane");

        var prompt = new TaskCompletionSource();
        var seams = new LeaseMonitor(new Lease(["src/**"]), Path.GetTempPath());
        var reported = new List<string>();

        var drain = GovernedRunHost.DrainAsync(
            source.Queue, prompt.Task, seams, reported.Add, sink: null, CancellationToken.None);

        for (var i = 1; i < Frames; i++)
        {
            source.Say($"line {i}");
        }

        // With no sink there is nothing downstream to wait on, so the queue's own published count is
        // what says the frames have arrived — the plane's number, not one this test keeps.
        var deadline = DateTimeOffset.UtcNow + Bound;
        while (source.Queue.Published < Frames - 1 && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(5);
        }

        prompt.SetResult();
        source.Say("done");

        var drained = await drain;

        Assert.Equal(Frames, drained.Events);
        Assert.Equal(Frames, drained.Kinds.Count);
    }

    [Fact]
    public void TheRunsOwnCountIsTheDrainsCount()
    {
        // What makes the equality above an equality with EventsObserved rather than with a parallel
        // number: the result READS the drain's count, and nothing else computes one. If this stops
        // being true, the oracle above is measuring something the run no longer reports.
        var host = File.ReadAllText(Path.Combine(
            RepoRoot().FullName, "src", "AiDe.App", "Conductor", "GovernedRunHost.cs"));

        Assert.Contains("EventsObserved: drained.Events", host, StringComparison.Ordinal);
    }
}
