using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AiDe.App.ViewModels;
using AiDe.App.Workbench.Understanding;
using AiDe.Core.Understanding;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace AiDe.App.Tests;

/// <summary>
/// <b>INV-0007, finding 2 — the class control.</b> Each WebView2-hosting surface initialises its
/// browser <b>exactly once</b>, however many times the docking host re-parents it: every attach
/// after the first is heard, recorded as <c>re-attached</c>, and declined.
/// </summary>
/// <remarks>
/// <para>DC-138's control in the fast ring, for <b>both</b> surfaces; the shell probe
/// (<c>ComposerHostIntegrationTests.TheComposerPageSurvivesALaterRender</c>) proves the visible
/// consequence through the real docking host. Seen red by mutation: with the host's once-guard
/// removed, <c>InitialisationsStarted</c> read 4 for 3 re-parents.</para>
///
/// <para><b>The re-parent is the docking host's, not a stand-in for it.</b> <c>Adapter.Render()</c>
/// replaces <c>Manager.Layout</c>, which detaches every pane and attaches it to a new tree; setting
/// a host's content away and back raises the same <c>Unloaded</c>/<c>Loaded</c> pair on the
/// surface, which is the only thing the surface can see either way. The <c>re-attached</c> lines
/// are counted so the test cannot pass because nothing was re-parented.</para>
/// </remarks>
public sealed class TheWebSurfacesInitialiseOnceAcrossReparentsTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private const int Reparents = 3;

    /// <summary>
    /// The guard's own claim, deterministically: <b>an attach that lands while the runtime is still
    /// starting is a re-attach, not a second start.</b> The runtime start is held open on a
    /// <see cref="TaskCompletionSource"/>, <c>Loaded</c> is raised four times into it, and the
    /// initialiser runs once, after the release.
    /// </summary>
    /// <remarks>
    /// Seen red by mutation: with the flag set after the first await instead of before it,
    /// <c>InitialisationsStarted</c> read 4. The window-driven test above cannot pin this — there the
    /// runtime usually starts before the first re-parent lands, so that mutant passes it by timing.
    /// </remarks>
    [Fact]
    public void AnAttachDuringTheRuntimeStartIsAReattachNotASecondStart()
    {
        Sta.Run(() =>
        {
            var lines = new List<string>();
            var previous = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = lines.Add;

            try
            {
                var owner = new ContentControl();
                var runtime = new TaskCompletionSource();
                var initialised = 0;
                var failures = new List<string>();
                using var host = new WebSurfaceHost(
                    "held:once", owner,
                    initialise: () => { initialised++; return Task.CompletedTask; },
                    failed: failures.Add,
                    ensure: _ => runtime.Task);

                for (var i = 0; i < 4; i++)
                {
                    owner.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                }

                Assert.Equal(1, host.InitialisationsStarted);
                Assert.Equal(0, initialised);
                Assert.Equal(3, lines.Count(l => l.Contains("\"transition\":\"re-attached\"", StringComparison.Ordinal)));

                // The host's continuation runs inline on SetResult: Sta.Run installs no
                // SynchronizationContext and the source is not RunContinuationsAsynchronously, so the
                // count below is already moved when SetResult returns. A pumped context would need a
                // wait here instead.
                runtime.SetResult();
                Assert.Equal(1, initialised);
                Assert.Empty(failures);
                AssertOneTimeInitialization(host.InitialisationsStarted, initialised,
                    lines.Count(l => l.Contains("\"transition\":\"re-attached\"", StringComparison.Ordinal)));
            }
            finally
            {
                WorkbenchDiagnostics.Sink = previous;
            }
        });
    }

    /// <summary>
    /// <b>A failed start is reported once and not retried on the next attach</b> — the surface says
    /// so once, the log carries the exception with a stable code, and three more attaches are three
    /// <c>re-attached</c> lines, not three more error banners.
    /// </summary>
    [Fact]
    public void AFailedStartIsReportedOnceAndNotRetriedOnReattach()
    {
        Sta.Run(() =>
        {
            var lines = new List<string>();
            var previous = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = lines.Add;

            try
            {
                var owner = new ContentControl();
                var initialised = 0;
                var failures = new List<string>();
                using var host = new WebSurfaceHost(
                    "broken:once", owner,
                    initialise: () => { initialised++; return Task.CompletedTask; },
                    failed: failures.Add,
                    ensure: _ => throw new InvalidOperationException("no WebView2 runtime here"));

                for (var i = 0; i < 4; i++)
                {
                    owner.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                }

                Assert.Equal(1, host.InitialisationsStarted);
                Assert.Equal(0, initialised);
                Assert.Equal(["no WebView2 runtime here"], failures);

                var failed = Assert.Single(lines, l => l.Contains("\"transition\":\"init-failed\"", StringComparison.Ordinal));
                using var record = System.Text.Json.JsonDocument.Parse(failed);
                Assert.Equal("WEB.INIT_THREW", record.RootElement.GetProperty("errorCode").GetString());
                Assert.Equal(typeof(InvalidOperationException).FullName, record.RootElement.GetProperty("exceptionType").GetString());
                Assert.Equal(3, lines.Count(l => l.Contains("\"transition\":\"re-attached\"", StringComparison.Ordinal)));
            }
            finally
            {
                WorkbenchDiagnostics.Sink = previous;
            }
        });
    }

    /// <summary>
    /// <b>A retry the operator asks for is not a re-attach</b> (the composer's mockup <c>Retry</c> in
    /// the <c>editorerror</c> send-row state). After <c>init-failed</c>, <see cref="WebSurfaceHost.Retry"/>
    /// runs initialisation a second time; a second failure is reported again, not swallowed as a
    /// duplicate.
    /// </summary>
    [Fact]
    public void RetryAfterAFailedStartRunsInitialisationAgain()
    {
        Sta.Run(() =>
        {
            var lines = new List<string>();
            var previous = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = lines.Add;

            try
            {
                var owner = new ContentControl();
                var initialised = 0;
                var failures = new List<string>();
                var runtimeShouldFail = true;
                using var host = new WebSurfaceHost(
                    "broken:retry", owner,
                    initialise: () => { initialised++; return Task.CompletedTask; },
                    failed: failures.Add,
                    ensure: _ => runtimeShouldFail
                        ? throw new InvalidOperationException("no WebView2 runtime here")
                        : Task.CompletedTask);

                owner.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                Assert.Equal(1, host.InitialisationsStarted);
                Assert.Equal(["no WebView2 runtime here"], failures);

                // A re-attach after the failure is still declined — the contract Retry must not weaken.
                owner.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                Assert.Equal(1, host.InitialisationsStarted);
                Assert.Single(failures);

                runtimeShouldFail = false;
                host.Retry().GetAwaiter().GetResult();

                Assert.Equal(2, host.InitialisationsStarted);
                Assert.Equal(1, initialised);
                Assert.Single(failures);   // the second attempt succeeded; no second failure
            }
            finally
            {
                WorkbenchDiagnostics.Sink = previous;
            }
        });
    }

    /// <summary>A retry with nothing to retry (no attempt yet, or the last one succeeded) is a no-op.</summary>
    [Fact]
    public void RetryWithNoFailedAttempt_DoesNothing()
    {
        Sta.Run(() =>
        {
            var owner = new ContentControl();
            var initialised = 0;
            using var host = new WebSurfaceHost(
                "clean:retry", owner,
                initialise: () => { initialised++; return Task.CompletedTask; },
                failed: _ => { },
                ensure: _ => Task.CompletedTask);

            owner.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Assert.Equal(1, host.InitialisationsStarted);
            Assert.Equal(1, initialised);

            host.Retry().GetAwaiter().GetResult();

            Assert.Equal(1, host.InitialisationsStarted);
            Assert.Equal(1, initialised);
        });
    }

    [Fact]
    public void TheComposerInitialisesItsBrowserOnceAcrossReparents() =>
        Drive(() => new ComposerSurface("composer:once", "once — composer"), surface => surface.InitialisationsStarted, "composer:once");

    [Fact]
    public void TheCanvasInitialisesItsBrowserOnceAcrossReparents() =>
        Drive(() => new CanvasSurface("canvas:once", "once — canvas"), surface => surface.InitialisationsStarted, "canvas:once");

    /// <summary>
    /// Every discovered Loaded site needs an executed production observation, not a path exemption.
    /// </summary>
    /// <remarks>
    /// <para>Root <c>src/AiDe.App</c>; every <c>*.cs</c> and <c>*.xaml</c> outside <c>bin</c>/<c>obj</c>;
    /// tokens <c>Loaded +=</c> (any spacing), <c>LoadedEvent</c>, and XAML <c>Loaded="</c>.
    /// Each match has its own ordinal within its file. There is no allowlist: registry membership
    /// routes to a probe, and missing, unexecuted or failed observations reject that site.</para>
    /// </remarks>
    [Fact]
    public void NoElementOutsideTheHostHooksLoadedForItsOwnInitialisation()
    {
        var sites = DiscoverLoadedSites();
        var probes = new Dictionary<string, Action>(StringComparer.Ordinal)
        {
            ["src/AiDe.App/MainWindow.xaml.cs#0"] = ObserveMainWindow,
            ["src/AiDe.App/MainWindow.xaml.cs#1"] = ObserveMainWindow,
            ["src/AiDe.App/Workbench/TextPromptDialog.cs#0"] = ObserveTextPrompt,
            ["src/AiDe.App/Workbench/PerspectiveShell.cs#0"] =
                () => new PerspectiveShellTests().EveryRetainedSwitch_IsShownOnce_WithAMeasuredDuration(),
            ["src/AiDe.App/Workbench/WebSurfaceHost.cs#0"] = () =>
            {
                AnAttachDuringTheRuntimeStartIsAReattachNotASecondStart();
                AFailedStartIsReportedOnceAndNotRetriedOnReattach();
                RetryAfterAFailedStartRunsInitialisationAgain();
                RetryWithNoFailedAttempt_DoesNothing();
                TheComposerInitialisesItsBrowserOnceAcrossReparents();
                TheCanvasInitialisesItsBrowserOnceAcrossReparents();
            },
            ["src/AiDe.App/Workbench/Understanding/AtlasLoadingHost.cs#0"] = ObserveAtlasAttachments,
            ["src/AiDe.App/Workbench/Understanding/AtlasReaderView.cs#0"] = ObserveRetainedReader,
        };
        var observed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var site in sites)
        {
            Assert.True(probes.TryGetValue(site, out var probe), $"Unregistered Loaded site: {site}");
            probe!();
            observed.Add(site);
            output.WriteLine("Executed passing production witness: {0}", site);
        }
        AssertCoverage(sites, probes.Keys, observed);
    }

    private static readonly Regex LoadedHook = new(
        @"\bLoaded\s*\+=|\bLoadedEvent\b|\bLoaded=""", RegexOptions.CultureInvariant);

    private static IReadOnlyList<string> DiscoverLoadedSites()
    {
        var root = RepoRoot();
        return Directory.EnumerateFiles(Path.Combine(root, "src", "AiDe.App"), "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".xaml", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .SelectMany(path => LoadedHook.Matches(File.ReadAllText(path)).Select((_, ordinal) =>
                $"{Path.GetRelativePath(root, path).Replace('\\', '/')}#{ordinal}"))
            .ToArray();
    }

    private static void AssertCoverage(IEnumerable<string> sites, IEnumerable<string> registered, IEnumerable<string> observed)
    {
        var registeredSet = registered.ToHashSet(StringComparer.Ordinal);
        var observedSet = observed.ToHashSet(StringComparer.Ordinal);
        foreach (var site in sites)
        {
            Assert.Contains(site, registeredSet);
            Assert.True(observedSet.Contains(site), $"Loaded site has no executed passing production witness: {site}");
        }
    }

    [Theory]
    [InlineData("unknown-site")]
    [InlineData("membership-only")]
    [InlineData("new-hook-existing-file")]
    [InlineData("failed-probe")]
    public void LoadedCoverageOracle_RejectsUnwitnessedSites(string fault)
    {
        const string first = "existing.cs#0";
        var sites = fault is "unknown-site" or "new-hook-existing-file"
            ? new[] { first, "existing.cs#1" } : [first];
        var witnessed = fault is "membership-only" or "failed-probe" ? Array.Empty<string>() : [first];
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertCoverage(sites, [first], witnessed));
    }

    private sealed record LifecycleEvent(string Kind, int Generation = 0, int Operation = 0, bool GateCompleted = false);
    private sealed record CleanupObservation(int PendingGates, int PendingOperations,
        int LeaseDisposals, int ReaderDisposals, string? Error = null);
    private sealed record HostObservation(int Admissions, bool InitialSeen, bool InitialCleared,
        bool LateAdopted, LifecycleEvent[] Events, CleanupObservation Cleanup);
    private sealed record ReaderObservation(bool SameInstance, bool UnloadedAfterDetach,
        bool UnloadedAfterLoaded, bool CurrentResponseSeen, int BackgroundChanges,
        bool BackgroundDifferent, CleanupObservation Cleanup);

    private static void AssertOneTimeInitialization(int started, int completed, int reattached)
    {
        Assert.Equal(1, started);
        Assert.Equal(1, completed);
        Assert.Equal(Reparents, reattached);
    }

    private static void AssertCleanup(CleanupObservation observation)
    {
        Assert.Equal(0, observation.PendingGates);
        Assert.Equal(0, observation.PendingOperations);
        Assert.Equal(1, observation.LeaseDisposals);
        Assert.Equal(1, observation.ReaderDisposals);
        Assert.Null(observation.Error);
    }

    private static void AssertHostObservation(HostObservation observation)
    {
        Assert.Equal(1, observation.Admissions);
        Assert.True(observation.InitialSeen);
        Assert.True(observation.InitialCleared);
        Assert.False(observation.LateAdopted);
        AssertCleanup(observation.Cleanup);

        var events = observation.Events;
        var close = Assert.Single(events, e => e.Kind == "close-request");
        var closeIndex = Array.IndexOf(events, close);
        var beforeClose = events.Take(closeIndex).ToArray();
        Assert.Equal(Enumerable.Range(1, Reparents + 1),
            beforeClose.Where(e => e.Kind == "loaded").Select(e => e.Generation));
        Assert.Equal(Enumerable.Range(1, Reparents),
            beforeClose.Where(e => e.Kind == "unloaded").Select(e => e.Generation));

        var queries = events.Where(e => e.Kind == "query-start").ToArray();
        Assert.Equal(Reparents + 1, queries.Length);
        Assert.Equal(queries.Length, queries.Select(e => e.Operation).Distinct().Count());
        var lease = Assert.Single(events, e => e.Kind == "lease-dispose");
        var reader = Assert.Single(events, e => e.Kind == "reader-dispose");
        var leaseIndex = Array.IndexOf(events, lease);
        var readerIndex = Array.IndexOf(events, reader);
        Assert.True(closeIndex < leaseIndex, "Healthy scope disposed before close was requested.");
        Assert.True(leaseIndex < readerIndex, "Reader disposal must follow lease disposal.");
        foreach (var generation in Enumerable.Range(1, Reparents + 1))
        {
            var query = Assert.Single(queries, e => e.Generation == generation);
            var finish = Assert.Single(events, e => e.Kind == "query-finish" && e.Operation == query.Operation);
            Assert.Equal(generation, finish.Generation);
            Assert.True(Array.IndexOf(events, query) < Array.IndexOf(events, finish));
            Assert.True(Array.IndexOf(events, finish) < leaseIndex,
                $"Query {query.Operation} must finish before lease disposal begins.");
        }

        Assert.Equal(new[] { 2, 4 }, events.Where(e => e.Kind == "held").Select(e => e.Generation));
        foreach (var generation in new[] { 2, 4 })
        {
            var query = Assert.Single(queries, e => e.Generation == generation);
            var held = Assert.Single(events, e => e.Kind == "held" && e.Operation == query.Operation);
            var cancel = Assert.Single(events, e => e.Kind == "cancel" && e.Operation == query.Operation);
            var stillHeld = Assert.Single(events, e => e.Kind == "held-after-cancel" && e.Operation == query.Operation);
            var release = Assert.Single(events, e => e.Kind == "gate-release" && e.Operation == query.Operation);
            var finish = Assert.Single(events, e => e.Kind == "query-finish" && e.Operation == query.Operation);
            Assert.False(cancel.GateCompleted, "Cancellation must not complete the independent query gate.");
            Assert.False(stillHeld.GateCompleted);
            Assert.True(Array.IndexOf(events, held) < Array.IndexOf(events, cancel));
            Assert.True(Array.IndexOf(events, cancel) < Array.IndexOf(events, stillHeld));
            Assert.True(Array.IndexOf(events, stillHeld) < Array.IndexOf(events, release));
            Assert.True(Array.IndexOf(events, release) < Array.IndexOf(events, finish));
        }
        var pending = Assert.Single(events, e => e.Kind == "close-pending");
        var finalRelease = Assert.Single(events, e => e.Kind == "gate-release" && e.Generation == 4);
        Assert.True(closeIndex < Array.IndexOf(events, pending));
        Assert.True(Array.IndexOf(events, pending) < Array.IndexOf(events, finalRelease));
        Assert.DoesNotContain(events, e => e.Kind == "close-completed-early");
    }

    private static void AssertReaderObservation(ReaderObservation observation)
    {
        Assert.True(observation.SameInstance);
        Assert.True(observation.UnloadedAfterDetach);
        Assert.False(observation.UnloadedAfterLoaded);
        Assert.True(observation.CurrentResponseSeen);
        Assert.False(observation.BackgroundDifferent);
        Assert.Equal(0, observation.BackgroundChanges);
        AssertCleanup(observation.Cleanup);
    }

    private static HostObservation LegalOracleTrace()
    {
        // Typed ORACLE example only. Registry admission never uses this fixture.
        var events = new List<LifecycleEvent>();
        for (var generation = 1; generation <= Reparents + 1; generation++)
        {
            events.Add(new("loaded", generation));
            events.Add(new("query-start", generation, generation));
            if (generation is 1 or 3)
            {
                events.Add(new("gate-release", generation, generation));
                events.Add(new("query-finish", generation, generation));
            }
            if (generation <= Reparents)
            {
                if (generation == 2) events.Add(new("held", generation, generation));
                events.Add(new("unloaded", generation));
                if (generation == 2)
                {
                    events.Add(new("cancel", generation, generation));
                    events.Add(new("held-after-cancel", generation, generation));
                    events.Add(new("gate-release", generation, generation));
                    events.Add(new("query-finish", generation, generation));
                }
            }
            else
            {
                events.Add(new("held", generation, generation));
                events.Add(new("close-request"));
                events.Add(new("cancel", generation, generation));
                events.Add(new("close-pending"));
                events.Add(new("held-after-cancel", generation, generation));
                events.Add(new("gate-release", generation, generation));
                events.Add(new("query-finish", generation, generation));
            }
        }
        events.Add(new("lease-dispose"));
        events.Add(new("reader-dispose"));
        return new(1, true, true, false, events.ToArray(), new(0, 0, 1, 1));
    }

    [Fact]
    public void AttachmentEventOracle_AcceptsLegalRepeatedActivation() => AssertHostObservation(LegalOracleTrace());

    [Theory]
    [InlineData("duplicate-query")]
    [InlineData("missing-attach")]
    [InlineData("no-cancellation")]
    [InlineData("early-close")]
    [InlineData("healthy-scope-disposal")]
    [InlineData("reader-before-lease")]
    [InlineData("disposal-before-query-finish")]
    [InlineData("cancelled-gate-not-held")]
    [InlineData("adopted-late-response")]
    [InlineData("initial-response-not-seen")]
    [InlineData("initial-response-not-cleared")]
    public void AttachmentEventOracle_RejectsFaultyTypedObservations(string fault)
    {
        var observation = LegalOracleTrace();
        var events = observation.Events.ToList();
        var lease = events.Single(e => e.Kind == "lease-dispose");
        switch (fault)
        {
            case "duplicate-query": events.Insert(3, new("query-start", 1, 99)); break;
            case "missing-attach": events.RemoveAll(e => e.Kind == "loaded" && e.Generation == 3); break;
            case "no-cancellation": events.RemoveAll(e => e.Kind == "cancel" && e.Operation == 4); break;
            case "early-close":
                events[events.FindIndex(e => e.Kind == "close-pending")] = new("close-completed-early");
                break;
            case "healthy-scope-disposal":
                events.Remove(lease);
                events.Insert(events.FindIndex(e => e.Kind == "unloaded"), lease);
                break;
            case "reader-before-lease":
                events.Remove(lease);
                events.Add(lease);
                break;
            case "disposal-before-query-finish":
                events.Remove(lease);
                events.Insert(events.FindIndex(e => e.Kind == "query-finish" && e.Operation == 4), lease);
                break;
            case "cancelled-gate-not-held":
                var index = events.FindIndex(e => e.Kind == "cancel" && e.Operation == 2);
                events[index] = events[index] with { GateCompleted = true };
                break;
            case "adopted-late-response": observation = observation with { LateAdopted = true }; break;
            case "initial-response-not-seen": observation = observation with { InitialSeen = false }; break;
            case "initial-response-not-cleared": observation = observation with { InitialCleared = false }; break;
            default: throw new ArgumentOutOfRangeException(nameof(fault));
        }
        observation = observation with { Events = events.ToArray() };
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertHostObservation(observation));
    }

    [Theory]
    [InlineData("failed-reset")]
    [InlineData("rejected-current-response")]
    [InlineData("different-instance")]
    [InlineData("missing-detach")]
    public void RetainedReaderOracle_RejectsCallbackBlindObservations(string fault)
    {
        var observation = new ReaderObservation(true, true, false, true, 0, false, new(0, 0, 1, 1));
        observation = fault switch
        {
            "failed-reset" => observation with { UnloadedAfterLoaded = true, CurrentResponseSeen = false },
            "rejected-current-response" => observation with { CurrentResponseSeen = false },
            "different-instance" => observation with { SameInstance = false },
            "missing-detach" => observation with { UnloadedAfterDetach = false },
            _ => throw new ArgumentOutOfRangeException(nameof(fault)),
        };
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertReaderObservation(observation));
    }

    [Fact]
    public void CleanupOracle_RejectsPendingGatesAndOperations() =>
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertCleanup(new(1, 1, 1, 1)));

    [Fact]
    public void InitializationOracle_RejectsDuplicateOneTimeStart() =>
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertOneTimeInitialization(2, 2, 3));

    [Fact]
    public void HostProbe_AssertionWhileHeld_ReleasesAllGatesAndPreservesFailure()
    {
        CleanupObservation? cleaned = null;
        var failure = Assert.Throws<Xunit.Sdk.XunitException>(
            () => RunHostObservation(true, result => cleaned = result));
        Assert.Equal("ER18 deliberate assertion while query is held", failure.Message);
        Assert.NotNull(cleaned);
        AssertCleanup(cleaned!);
        output.WriteLine("Deliberate-failure cleanup: {0}", System.Text.Json.JsonSerializer.Serialize(cleaned));
    }

    [Fact]
    public void RetainedReaderProbe_BackgroundPerturbationFailsTheSameChecker()
    {
        var observation = RunRetainedReaderObservation(perturbBackground: true);
        Assert.True(observation.BackgroundChanges > 0);
        Assert.True(observation.BackgroundDifferent);
        Assert.True(observation.SameInstance);
        Assert.True(observation.UnloadedAfterDetach);
        Assert.False(observation.UnloadedAfterLoaded);
        Assert.True(observation.CurrentResponseSeen);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertReaderObservation(observation));
        output.WriteLine("Test-only Loaded property perturbation: {0}", System.Text.Json.JsonSerializer.Serialize(observation));
    }

    private static void ObserveMainWindow()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"aide-loaded-{Guid.NewGuid():N}");
        var lines = new ConcurrentQueue<string>();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = lines.Enqueue;
        try
        {
            Sta.Pump(() => new Border(), async (_, root) =>
            {
                var starts = 0;
                var window = new MainWindow(() =>
                {
                    ++starts;
                    return Task.FromResult(new MainWindowViewModel());
                }, directory, MainWindowResources());
                window.Show();
                try
                {
                    await Settle(root);
                    await window.WorkspaceReady;
                    var content = window.Content;
                    window.Content = null;
                    await Settle(root);
                    window.Content = content;
                    await Settle(root);
                    Assert.Equal(1, starts);
                    Assert.Single(lines, line => line.Contains("\"evt\":\"app.start\"", StringComparison.Ordinal));
                }
                finally
                {
                    window.Close();
                    await window.CloseOperation;
                }
            }, timeoutSeconds: 45);
        }
        finally
        {
            WorkbenchDiagnostics.Sink = previous;
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static void ObserveTextPrompt()
    {
        Sta.Run(() =>
        {
            var title = $"loaded-probe-{Guid.NewGuid():N}";
            Exception? failure = null;
            var observations = 0;
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var window = PresentationSource.CurrentSources.OfType<System.Windows.Interop.HwndSource>()
                    .Select(source => source.RootVisual).OfType<Window>().Single(w => w.Title == title);
                try
                {
                    var panel = Assert.IsType<StackPanel>(window.Content);
                    var box = Assert.Single(panel.Children.OfType<TextBox>());
                    Assert.Equal("selected", box.SelectedText);
                    Assert.True(box.IsKeyboardFocused);
                    observations++;
                }
                catch (Exception error) { failure = error; }
                finally { window.DialogResult = false; }
            }));
            Assert.Null(TextPromptDialog.Show(title, "selected", null));
            Assert.Null(failure);
            Assert.Equal(1, observations);
        });
    }

    private static ResourceDictionary MainWindowResources()
    {
        var app = System.Xml.Linq.XDocument.Load(Path.Combine(RepoRoot(), "src", "AiDe.App", "App.xaml")).Root!;
        var ns = app.Name.Namespace;
        var dictionary = new System.Xml.Linq.XElement(ns + "ResourceDictionary",
            app.Attributes().Where(attribute => attribute.IsNamespaceDeclaration).Select(attribute =>
                new System.Xml.Linq.XAttribute(attribute.Name, attribute.Value == "clr-namespace:AiDe.App"
                    ? "clr-namespace:AiDe.App;assembly=AiDe.App" : attribute.Value)),
            app.Element(ns + "Application.Resources")!.Elements());
        return (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(dictionary.ToString());
    }

    private void ObserveAtlasAttachments()
    {
        var observation = RunHostObservation(false);
        output.WriteLine("Host lifecycle events: {0}", System.Text.Json.JsonSerializer.Serialize(observation));
        AssertHostObservation(observation);
    }

    private void ObserveRetainedReader()
    {
        var observation = RunRetainedReaderObservation(false);
        output.WriteLine("Retained Reader Loaded witness: {0}", System.Text.Json.JsonSerializer.Serialize(observation));
        AssertReaderObservation(observation);
    }

    private static HostObservation RunHostObservation(bool failWhileHeld, Action<CleanupObservation>? cleaned = null)
    {
        HostObservation? observation = null;
        Sta.Pump(() => new Border(), async (_, root) =>
        {
            var port = new ObservedPort();
            var owner = new AtlasWorkspaceOwner();
            AtlasLoadingHost? host = null;
            RoutedEventHandler loaded = (_, e) =>
            {
                if (ReferenceEquals(e.OriginalSource, host)) port.Record("loaded", port.Generation);
            };
            RoutedEventHandler unloaded = (_, e) =>
            {
                if (ReferenceEquals(e.OriginalSource, host)) port.Record("unloaded", port.Generation);
            };
            Exception? primary = null;
            var initialSeen = false;
            var initialCleared = false;
            var lateAdopted = false;
            try
            {
                await owner.AttachAsync(() => port);
                host = new AtlasLoadingHost(owner);
                host.Loaded += loaded;
                host.Unloaded += unloaded;
                for (var generation = 1; generation <= Reparents + 1; generation++)
                {
                    port.Generation = generation;
                    root.Child = host;
                    await Settle(root);
                    Assert.Equal(generation, port.Operations.Count);
                    var operation = Assert.Single(port.Operations, item => item.Generation == generation);
                    var view = Assert.IsType<AtlasReaderView>(host.ReaderView);
                    if (failWhileHeld)
                        throw new Xunit.Sdk.XunitException("ER18 deliberate assertion while query is held");

                    if (generation is 1 or 3)
                    {
                        operation.Release(generation == 1 ? "initial" : "current-third");
                        await operation.Finished.Task;
                        await Settle(root);
                        Assert.Single(view.FileRoots);
                        Assert.True(HasInventoryMarker(view, generation == 1 ? "initial.cs" : "current-third.cs"));
                        if (generation == 1) initialSeen = HasInventoryMarker(view, "initial.cs");
                    }
                    else port.Record("held", generation, operation.Id);

                    if (generation <= Reparents)
                    {
                        root.Child = null;
                        await Settle(root);
                        if (generation == 1)
                            initialCleared = ReaderPresentationIsInert(host, view);
                        if (generation == 2)
                        {
                            port.Record(operation.Token.IsCancellationRequested ? "held-after-cancel" : "held-without-cancellation",
                                generation, operation.Id, operation.Gate.Task.IsCompleted);
                            operation.Release("late-detached");
                            await operation.Finished.Task;
                            await Settle(root);
                            lateAdopted |= !ReaderPresentationIsInert(host, view)
                                || HasInventoryMarker(view, "late-detached.cs");
                        }
                    }
                    else
                    {
                        port.Record("close-request");
                        var close = owner.DisposeAsync().AsTask();
                        await Settle(root);
                        port.Record(close.IsCompleted ? "close-completed-early" : "close-pending");
                        port.Record(operation.Token.IsCancellationRequested ? "held-after-cancel" : "held-without-cancellation",
                            generation, operation.Id, operation.Gate.Task.IsCompleted);
                        operation.Release("late-closing");
                        await operation.Finished.Task;
                        await close;
                        await Settle(root);
                        lateAdopted |= !ReaderPresentationIsInert(host, view)
                            || HasInventoryMarker(view, "late-closing.cs");
                    }
                }
            }
            catch (Exception error) when (error is not OutOfMemoryException) { primary = error; }
            finally
            {
                var cleanup = await CleanupProbe(root, owner, port, primary);
                if (host is not null)
                {
                    host.Loaded -= loaded;
                    host.Unloaded -= unloaded;
                }
                observation = new(port.Admissions, initialSeen, initialCleared, lateAdopted, port.Events, cleanup);
                cleaned?.Invoke(cleanup);
            }
            if (primary is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(primary).Throw();
        }, timeoutSeconds: 45);
        return Assert.IsType<HostObservation>(observation);
    }

    private static ReaderObservation RunRetainedReaderObservation(bool perturbBackground)
    {
        ReaderObservation? observation = null;
        Sta.Pump(() => new Border(), async (_, root) =>
        {
            var port = new ObservedPort { Generation = 1 };
            var owner = new AtlasWorkspaceOwner();
            AtlasReaderView? reader = null;
            System.ComponentModel.DependencyPropertyDescriptor? background = null;
            var watchingLoaded = false;
            var changes = 0;
            EventHandler changed = (_, _) => { if (watchingLoaded) changes++; };
            RoutedEventHandler perturb = (_, _) =>
            {
                reader!.Background = ReferenceEquals(reader.Background, System.Windows.Media.Brushes.Magenta)
                    ? System.Windows.Media.Brushes.Cyan : System.Windows.Media.Brushes.Magenta;
            };
            Exception? primary = null;
            var same = false;
            var detached = false;
            var afterLoaded = true;
            var responseSeen = false;
            var differentBackground = false;
            try
            {
                await owner.AttachAsync(() => port);
                var host = new AtlasLoadingHost(owner);
                root.Child = host;
                await Settle(root);
                var initial = Assert.Single(port.Operations);
                initial.Release("reader-initial");
                await initial.Finished.Task;
                await Settle(root);
                reader = Assert.IsType<AtlasReaderView>(host.ReaderView);
                Assert.Single(reader.FileRoots);
                Assert.True(HasInventoryMarker(reader, "reader-initial.cs"));

                host.Content = null;
                await Settle(root);
                detached = ReaderUnloaded(reader);
                var before = reader.Background;
                background = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                    Control.BackgroundProperty, reader.GetType());
                Assert.NotNull(background);
                background!.AddValueChanged(reader, changed);
                if (perturbBackground) reader.Loaded += perturb;
                watchingLoaded = true;
                host.Content = reader;
                await Settle(root);
                watchingLoaded = false;
                same = ReferenceEquals(host.Content, reader);
                afterLoaded = ReaderUnloaded(reader);
                differentBackground = !Equals(before, reader.Background);

                // Data refresh after the real Loaded transition; never manual lifecycle activation.
                port.Generation = 2;
                var refresh = reader.LoadAsync();
                Assert.Equal(2, port.Operations.Count);
                var current = port.Operations[1];
                current.Release("retained-reader-current");
                await refresh;
                await Settle(root);
                responseSeen = HasInventoryMarker(reader, "retained-reader-current.cs");
            }
            catch (Exception error) when (error is not OutOfMemoryException) { primary = error; }
            finally
            {
                watchingLoaded = false;
                if (reader is not null)
                {
                    reader.Loaded -= perturb;
                    background?.RemoveValueChanged(reader, changed);
                }
                var cleanup = await CleanupProbe(root, owner, port, primary);
                observation = new(same, detached, afterLoaded, responseSeen, changes, differentBackground, cleanup);
            }
            if (primary is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(primary).Throw();
        }, timeoutSeconds: 45);
        return Assert.IsType<ReaderObservation>(observation);
    }

    private static bool ReaderUnloaded(AtlasReaderView reader)
    {
        var field = typeof(AtlasReaderView).GetField("_unloaded",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<bool>(field!.GetValue(reader));
    }

    private static bool HasInventoryMarker(AtlasReaderView reader, string relativePath) =>
        reader.FileRoots.Any(node => string.Equals(node.RelativePath, relativePath, StringComparison.Ordinal));

    private static bool ReaderPresentationIsInert(AtlasLoadingHost host, AtlasReaderView reader)
    {
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var receipt = typeof(AtlasReaderView).GetField("_receiptToken", flags);
        var currentFile = typeof(AtlasReaderView).GetField("_currentFile", flags);
        Assert.NotNull(receipt);
        Assert.NotNull(currentFile);
        // BC2: valid inventory may stay cached; abandoned actionable presentation may not.
        return host.ReaderView is null && !reader.IsLoaded && ReaderUnloaded(reader)
            && reader.SourceText.Length == 0 && reader.OutlineRows.Count == 0 && !reader.CanGoBack
            && receipt!.GetValue(reader) is null && currentFile!.GetValue(reader) is null;
    }

    private static async Task<CleanupObservation> CleanupProbe(
        Border root, AtlasWorkspaceOwner owner, ObservedPort port, Exception? primary)
    {
        var errors = port.ReleaseAll();
        try { root.Child = null; await Settle(root); }
        catch (Exception error) when (error is not OutOfMemoryException) { errors.Add(error); }
        try { await owner.DisposeAsync(); await Settle(root); }
        catch (Exception error) when (error is not OutOfMemoryException) { errors.Add(error); }
        finally { port.DisposeRegistrations(); }
        var detail = errors.Count == 0 ? null : string.Join(Environment.NewLine, errors.Select(error => error.ToString()));
        if (primary is not null && detail is not null) primary.Data["ER18 cleanup failures"] = detail;
        return new(port.Operations.Count(call => !call.Gate.Task.IsCompleted),
            port.Operations.Count(call => !call.Finished.Task.IsCompleted),
            port.LeaseDisposals, port.ReaderDisposals, detail);
    }

    private sealed class ObservedPort : IAtlasWorkspaceReader, IAtlasReaderQueries
    {
        private readonly ConcurrentQueue<LifecycleEvent> _events = new();
        private int _eventCount;
        internal List<InventoryOperation> Operations { get; } = [];
        internal LifecycleEvent[] Events => _events.ToArray();
        internal int Generation { get; set; }
        internal int Admissions { get; private set; }
        internal int LeaseDisposals { get; private set; }
        internal int ReaderDisposals { get; private set; }

        internal void Record(string kind, int generation = 0, int operation = 0, bool gateCompleted = false)
        {
            if (Interlocked.Increment(ref _eventCount) > 128)
                throw new InvalidOperationException("ER18 lifecycle trace exceeded its 128-event bound.");
            _events.Enqueue(new(kind, generation, operation, gateCompleted));
        }

        public ValueTask<IAtlasReaderLease> AdmitAsync(CancellationToken cancellationToken)
        {
            Admissions++;
            return ValueTask.FromResult<IAtlasReaderLease>(new ObservedLease(this));
        }

        public ValueTask<AtlasInventoryPageDto> InventoryAsync(AtlasInventoryRequestDto request, CancellationToken cancellationToken)
        {
            var operation = new InventoryOperation(this, Operations.Count + 1, Generation, cancellationToken);
            Operations.Add(operation);
            Record("query-start", Generation, operation.Id);
            operation.Registration = cancellationToken.Register(
                () => Record("cancel", operation.Generation, operation.Id, operation.Gate.Task.IsCompleted));
            return new(operation.ObserveCompletion());
        }

        internal List<Exception> ReleaseAll()
        {
            var errors = new List<Exception>();
            foreach (var operation in Operations)
            {
                try { operation.Release("finally-cleanup"); }
                catch (Exception error) when (error is not OutOfMemoryException) { errors.Add(error); }
            }
            return errors;
        }

        internal void DisposeRegistrations()
        {
            foreach (var operation in Operations) operation.Registration.Dispose();
        }

        public ValueTask<AtlasSelectionDto> SelectAsync(AtlasSelectRequestDto request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("This lifecycle probe must not request a selection.");
        public ValueTask<AtlasSelectionDto> RestoreAsync(AtlasRestoreRequestDto request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("This lifecycle probe must not restore a selection.");
        public ValueTask DisposeAsync()
        {
            ReaderDisposals++;
            Record("reader-dispose");
            return ValueTask.CompletedTask;
        }

        internal sealed class InventoryOperation(ObservedPort port, int id, int generation, CancellationToken token)
        {
            internal int Id => id;
            internal int Generation => generation;
            internal CancellationToken Token => token;
            internal CancellationTokenRegistration Registration { get; set; }
            internal TaskCompletionSource<AtlasInventoryPageDto> Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal TaskCompletionSource Finished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            internal void Release(string label)
            {
                if (Gate.Task.IsCompleted) return;
                try { port.Record("gate-release", Generation, Id); }
                finally
                {
                    Gate.TrySetResult(new(
                        1, "scope", 7, "manifest", AtlasCompletionState.Complete,
                        [new($"file-{Id}", AtlasDirectoryEntryKind.File, null, label + ".cs", default, default,
                            new(AtlasDenominatorState.Known, 1, null), null)],
                        new(AtlasBoundsDimension.InventoryRows, 64, 64, 1, 0, AtlasDenominatorState.Known, 1, null, null, null),
                        null, [label]));
                }
            }

            internal async Task<AtlasInventoryPageDto> ObserveCompletion()
            {
                try
                {
                    var result = await Gate.Task.ConfigureAwait(false);
                    port.Record("query-finish", Generation, Id);
                    return result;
                }
                finally { Finished.TrySetResult(); }
            }
        }

        private sealed class ObservedLease(ObservedPort port) : IAtlasReaderLease
        {
            public string ScopeToken => "scope";
            public string InitialManifestToken => "manifest";
            public long CoreEpoch => 7;
            public DateTimeOffset ExpiresAt => DateTimeOffset.MaxValue;
            public IAtlasReaderQueries Queries => port;
            public CancellationToken Invalidated => CancellationToken.None;
            public bool IsTerminal => port.LeaseDisposals != 0;
            public ValueTask DisposeAsync()
            {
                port.LeaseDisposals++;
                port.Record("lease-dispose");
                return ValueTask.CompletedTask;
            }
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }

    private static void Drive<T>(Func<T> create, Func<T, int> initialisations, string surfaceId)
        where T : UIElement
    {
        var lines = new List<string>();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = line => { lock (lines) { lines.Add(line); } };

        try
        {
            ContentControl host = null!;

            Sta.Pump(
                create: create,
                content: surface => host = new ContentControl { Content = surface },
                body: async (_, surface) =>
                {
                    await Settle(surface);
                    Assert.Equal(1, initialisations(surface));

                    for (var i = 0; i < Reparents; i++)
                    {
                        // Away, then back: the Unloaded/Loaded pair every Adapter.Render() raises.
                        host.Content = null;
                        await Settle(surface);
                        host.Content = surface;
                        await Settle(surface);
                    }

                    Assert.Equal(1, initialisations(surface));
                },
                timeoutSeconds: 60);

            // THE LOG SAYS THE SAME, and is the non-vacuity: one `initialising`, then exactly N
            // `re-attached` — so the surface really was attached N more times and declined each.
            var mine = lines.Where(l => l.Contains($"\"surface\":\"{surfaceId}\"", StringComparison.Ordinal)).ToList();
            Assert.Single(mine, l => l.Contains("\"transition\":\"initialising\"", StringComparison.Ordinal));
            Assert.Equal(Reparents, mine.Count(l => l.Contains("\"transition\":\"re-attached\"", StringComparison.Ordinal)));

            // THE PAYLOAD (T7): the keys a reader greps for exist, and the counts the host does not
            // measure are null — not invented.
            using var record = System.Text.Json.JsonDocument.Parse(mine.First(l => l.Contains("\"transition\":\"re-attached\"", StringComparison.Ordinal)));
            Assert.Equal("web-surface.handshake", record.RootElement.GetProperty("evt").GetString());
            Assert.Equal(surfaceId, record.RootElement.GetProperty("surface").GetString());
            foreach (var key in new[] { "navigations", "inputs", "drops" })
            {
                Assert.Equal(System.Text.Json.JsonValueKind.Null, record.RootElement.GetProperty(key).ValueKind);
            }
        }
        finally
        {
            WorkbenchDiagnostics.Sink = previous;
        }
    }

    /// <summary>
    /// Lets the queued <c>Loaded</c>/<c>Unloaded</c> broadcast run before a count is read. The
    /// broadcast is queued at <c>Loaded</c> priority, above <c>Background</c>, so one operation at
    /// <c>Background</c> runs after it — and <c>Sta.Pump</c> pumps at <c>Background</c>, so nothing
    /// lower would ever be reached.
    /// </summary>
    private static Task Settle(UIElement element) =>
        element.Dispatcher.InvokeAsync(static () => { }, DispatcherPriority.Background).Task;
}
