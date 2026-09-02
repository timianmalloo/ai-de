using AiDe.Testing;
using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Presentation;
using AiDe.Core.Projections;
using AiDe.Core.Watcher;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// What the surface actually SHOWS, not what its view model contains.
/// </summary>
/// <remarks>
/// <para><b>This exists because of a defect the whole suite was blind to.</b> When the evidence pane
/// became asynchronous, the factory kept binding <c>pane.Rows</c> and <c>pane.StatusMessage</c> at
/// construction — before the load had run. <c>Rows</c> is replaced by the load and is not
/// observable, so the pane sat on "Loading evidence…" permanently. Every test passed: the pane view
/// model was correct and thoroughly covered, and nothing asserted on what the control displayed.
/// It was found by running the application and looking at it.</para>
///
/// <para>The layer between a correct view model and a rendered control is ordinary imperative code,
/// and it is exactly where this class of defect lives (<b>E11/E12</b>: prove the rendered surface,
/// and its consistency across surfaces). These tests reach into the built control tree.</para>
/// </remarks>
public sealed class SurfaceContentTests
{
    private static T OnStaThread<T>(Func<T> work) =>
        Sta.Run<T>(work, 60);

    /// <summary>Unwraps the SurfaceChrome island frame the factory puts around non-windowed panes.</summary>
    private static FrameworkElement Unwrap(FrameworkElement content) =>
        content is System.Windows.Controls.Border { Child: FrameworkElement inner } ? inner : content;

    /// <summary>A read surface that answers immediately with one known match.</summary>
    private sealed class StubQueries : FakeWorkspaceQueries
    {
        public override Task<DescribeResult> DescribeAsync(string nodeId, int maxNeighbors, CancellationToken ct) =>
            Task.FromResult(new DescribeResult(
                new NodeView(nodeId, "kind", nodeId), [],
                new ResultBounds(1, 1, 1024, 1, 0, 0, 0, false, null), "rev-1"));

        public override Task<ImpactResult> ImpactAsync(string nodeId, int maxNodes, int maxEdges, CancellationToken ct) =>
            Task.FromResult(new ImpactResult(
                nodeId, [], [], new ResultBounds(1, 1, 1024, 0, 0, 0, 0, false, null), "rev-1"));

        public override Task<FindResult> FindAsync(string term, int maxResults, CancellationToken ct) =>
            Task.FromResult(new FindResult(
                [new FindMatch("Service.Orders", "csharp.type", "Service.Orders", AuthorshipOrigin.RepositoryArtifact)],
                new ResultBounds(1, 1, 1024, 1, 0, 0, 0, false, null),
                "rev-1"));

        public override Task<KnowledgeResult> KnowledgeAsync(string? term, string? type, int maxResults, CancellationToken ct) =>
            Task.FromResult(new KnowledgeResult(
                [], new ResultBounds(1, 1, 1024, 0, 0, 0, 0, false, null), "rev-1"));

        public override Task<EvidencePage> EvidenceAsync(string? cursor, int maxAssertions, CancellationToken ct) =>
            Task.FromResult(new EvidencePage([], null, "rev-1"));


        public override Task<PathResult> PathsAsync(PathQuery query, CancellationToken ct) =>
            Task.FromResult(new PathResult([], false, null, "rev-1"));

        public override Task<WorkspaceGraph> GraphAsync(GraphQuery query, CancellationToken ct) =>
            Task.FromResult(new WorkspaceGraph([], [], 0, [], "rev-1"));

    }

    /// <summary>What a built surface ended up showing.</summary>
    /// <remarks>
    /// Plain data, read INSIDE the STA body. A WPF control belongs to the thread that created it, so
    /// returning the control and reading its properties from the test thread throws — the assertions
    /// have to be about values, not about objects.
    /// </remarks>
    private sealed record SurfaceView(int ItemCount, string StatusText);

    /// <summary>Builds an evidence surface and pumps the dispatcher until its async load lands.</summary>
    private static SurfaceView BuiltEvidenceSurface(IWorkspaceQueries queries) =>
        OnStaThread(() =>
        {
            var content = new SurfaceContentFactory(queries)
                .Create(new Surface("view-1", "view", "Explore"));

            var stack = Assert.IsType<StackPanel>(Unwrap(content));
            var list = stack.Children.OfType<ListBox>().Single();
            var status = stack.Children.OfType<TextBlock>().Single();

            // The load completes on another thread and marshals back to this dispatcher, so the
            // dispatcher has to be pumped for the result to arrive — the same thing the running
            // application does by having a message loop.
            //
            // Pumped with a real DispatcherFrame rather than an inline Invoke: on a thread that is
            // not running a message loop, Invoke executes straight away and never drains the queue
            // the continuation was posted to.
            var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10);

            while (DateTimeOffset.UtcNow < deadline
                && list.ItemsSource is null
                && status.Text.Contains("Loading", StringComparison.OrdinalIgnoreCase))
            {
                var frame = new System.Windows.Threading.DispatcherFrame();
                System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() => frame.Continue = false),
                    System.Windows.Threading.DispatcherPriority.Background);
                System.Windows.Threading.Dispatcher.PushFrame(frame);
                Thread.Sleep(5);
            }

            return new SurfaceView(
                list.ItemsSource?.Cast<object>().Count() ?? 0,
                status.Text);
        });

    [Fact]
    public void TheEvidenceSurface_ShowsItsRows_OnceTheAsyncLoadCompletes()
    {
        // The exact defect: the control kept its construction-time binding and never saw the result.
        Assert.True(BuiltEvidenceSurface(new StubQueries()).ItemCount > 0);
    }

    [Fact]
    public void TheEvidenceSurface_DoesNotStayOnItsLoadingMessage()
    {
        // Its own case because "Loading…" forever is the specific shape this defect takes, and it is
        // indistinguishable from a slow workspace unless something says otherwise.
        var view = BuiltEvidenceSurface(new StubQueries());

        Assert.DoesNotContain("Loading", view.StatusText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rev-1", view.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnreachableWorkspace_SaysSoOnTheSurface_RatherThanLoadingForever()
    {
        // A read surface that throws is what an unreachable daemon looks like from here. Leaving the
        // Loading text in place would present it as merely slow (DC-011).
        var view = BuiltEvidenceSurface(new ThrowingQueries());

        Assert.DoesNotContain("Loading", view.StatusText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unavailable", view.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithNoWorkspace_TheEvidenceSurface_SaysToOpenAWorkspace_NotThatItIsUnavailableInThisBuild()
    {
        // "… is not available in this build" points the user at a build/packaging defect for what
        // is actually the ordinary "no workspace open" empty state — the exact confusion the Explore
        // pane showed in smoke-test 9-2. The message must name the real cause and the real action,
        // in the same voice as the graph pane ("No workspace is open. Open one to see …").
        var text = OnStaThread(() =>
        {
            var content = new SurfaceContentFactory(null).Create(new Surface("view-1", "view", "Explore"));
            return Assert.IsType<TextBlock>(Unwrap(content)).Text;
        });

        Assert.Contains("workspace", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Explore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("not available in this build", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EveryKindTheFactoryClaimsToKnow_ProducesSomethingOtherThanTheUnavailablePane()
    {
        // KnownKinds is load-bearing now: the restore uses it to decide what it can rebuild, so a
        // kind listed there and not handled below would resurrect a pane that renders nothing.
        OnStaThread(() =>
        {
            var factory = new SurfaceContentFactory(new StubQueries());

            foreach (var kind in SurfaceContentFactory.KnownKinds)
            {
                var content = factory.Create(new Surface($"s-{kind}", kind, kind));
                Assert.False(Unwrap(content) is TextBlock t && t.Text.Contains("not available",
                    StringComparison.OrdinalIgnoreCase), kind);
            }

            return 0;
        });
    }

    [Fact]
    public void TheJoinsSurfaceIsBuilt_AndIsInTheDefaultLayout()
    {
        // JoinProjection was written, tested, and never called by the running application. A
        // projection nobody can see is a control that cannot fire.
        var content = OnStaThread(() =>
            new SurfaceContentFactory(null).Create(new Surface("joins", "joins", "Joins")));

        Assert.IsType<JoinSurface>(Unwrap(content));
        Assert.Contains(Layout.Default().AllStacks().SelectMany(s => s.Surfaces),
            s => s.Kind == "joins");
    }

    [Fact]
    public void TheSessionsSurface_ShowsAnObservedSessionRow()
    {
        // The Loomkeeper Sessions surface renders honestly and synchronously - one observed session
        // reaches a legible row (#15), and the status is not a Loading message (the load is a local fold).
        var view = OnStaThread(() =>
        {
            var query = new StubSessionsQuery(new WatcherSessionSnapshot(
                "s1",
                new SessionBinding(
                    new RepositoryIdentity("C:/repos/ai-de", "ai-de"),
                    new WorktreeIdentity(new RepositoryIdentity("C:/repos/ai-de", "ai-de"), "main", "C:/repos/ai-de"),
                    new TerminalIdentity("term-1"),
                    new AgentIdentity("agent-1"),
                    new HarnessIdentity("Claude Code", "1.0"),
                    new ModelIdentity("Opus 4.8", "2026-08"),
                    TrustClassification.Verified),
                LivenessState.Alive,
                3));

            var content = new SurfaceContentFactory(null, query).Create(new Surface("sessions", "sessions", "Sessions"));
            var stack = Assert.IsType<StackPanel>(Unwrap(content));
            var scroller = stack.Children.OfType<ScrollViewer>().Single();
            var rows = Assert.IsType<StackPanel>(scroller.Content);
            var status = stack.Children.OfType<TextBlock>().Last();
            return new SurfaceView(rows.Children.Count, status.Text);
        });

        Assert.Equal(1, view.ItemCount);
        Assert.DoesNotContain("Loading", view.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheSessionsSurface_WithNoWatcherStore_SaysObservationIsUnavailable_NotBlank()
    {
        var view = OnStaThread(() =>
        {
            var content = new SurfaceContentFactory(null).Create(new Surface("sessions", "sessions", "Sessions"));
            var stack = Assert.IsType<StackPanel>(Unwrap(content));
            var status = stack.Children.OfType<TextBlock>().Single();
            return status.Text;
        });

        Assert.Contains("not available", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Loading", view, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheSessionsSurface_IsInTheDefaultLayout()
    {
        // The same visibility lesson as Joins: a surface not in the default layout is a control
        // nobody can see. Sessions is the point of the watcher UI.
        Assert.Contains(Layout.Default().AllStacks().SelectMany(s => s.Surfaces),
            s => s.Kind == "sessions");
    }

    [Fact]
    public void TheBoardSurface_ShowsAPost_AndIsInTheDefaultLayout()
    {
        // The Message Board surface (US-4) renders honestly and synchronously - one post reaches the
        // ListBox, and the status is not a Loading message (the load is a local fold).
        var view = OnStaThread(() =>
        {
            var query = new StubBoardQuery(new BoardMessage(
                "m1", "ai-de", BoardMessageKind.Breadcrumb, "s1", TrustClassification.Verified,
                null, "watch the daemon lock ordering", false, false, false, DateTimeOffset.UnixEpoch, 1));

            var content = new SurfaceContentFactory(null, null, query).Create(new Surface("board", "board", "Board"));
            var stack = Assert.IsType<StackPanel>(Unwrap(content));
            var list = stack.Children.OfType<ListBox>().Single();
            var status = stack.Children.OfType<TextBlock>().Single();
            return new SurfaceView(list.ItemsSource?.Cast<object>().Count() ?? 0, status.Text);
        });

        Assert.Equal(1, view.ItemCount);
        Assert.DoesNotContain("Loading", view.StatusText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Layout.Default().AllStacks().SelectMany(s => s.Surfaces), s => s.Kind == "board");
    }

    [Fact]
    public void TheBoardSurface_WithNoWatcherStore_SaysUnavailable_NotBlank()
    {
        var status = OnStaThread(() =>
        {
            var content = new SurfaceContentFactory(null).Create(new Surface("board", "board", "Board"));
            // Empty state is now a centred message host (Grid), not a top-left line in a StackPanel.
            var host = Assert.IsType<Grid>(Unwrap(content));
            return host.Children.OfType<TextBlock>().Single().Text;
        });

        Assert.Contains("not available", status, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Loading", status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheLeaderboardSurface_ShowsACell_AndIsInTheDefaultLayout()
    {
        // The Leaderboard surface (US-14) renders honestly and synchronously - a comparable cohort
        // reaches the ListBox as at least one cell, and the status is not a Loading message.
        var view = OnStaThread(() =>
        {
            var episodes = Enumerable.Range(0, 5).Select(i =>
            {
                var card = new Scorecard($"ep-{i}", "weave/1", WeaveVerdict.Partial,
                    [new DimensionAssessment(ScoreDimension.OutcomeIntegrity, 30, 4, 80 + i, AssessmentPosture.Deterministic, "r")],
                    [], new EvidenceCoverage(9, 10), $"Partial: {80 + i} / 30 observed", DateTimeOffset.UnixEpoch);
                return new ScoredEpisode($"ep-{i}", "Claude Code", "Opus 4.8", i % 2 == 0 ? "op1" : "op2", "refactor", "weave/1", card);
            }).ToArray();
            var query = new StubLeaderboardQuery(episodes);

            var content = new SurfaceContentFactory(null, null, null, query).Create(new Surface("leaderboard", "leaderboard", "Leaderboard"));
            var stack = Assert.IsType<StackPanel>(Unwrap(content));
            var list = stack.Children.OfType<ListBox>().Single();
            var status = stack.Children.OfType<TextBlock>().Single();
            return new SurfaceView(list.ItemsSource?.Cast<object>().Count() ?? 0, status.Text);
        });

        Assert.True(view.ItemCount >= 1);
        Assert.DoesNotContain("Loading", view.StatusText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Layout.Default().AllStacks().SelectMany(s => s.Surfaces), s => s.Kind == "leaderboard");
    }

    [Fact]
    public void TheLedgerSurface_ShowsAnEpisode_AndIsInTheDefaultLayout()
    {
        // The Ledger (US: "the ledger viewable too") renders honestly and synchronously — one recorded
        // work episode reaches the ListBox as a row, and the status is not a Loading message (the load
        // is a local read off the observation store).
        var view = OnStaThread(() =>
        {
            var store = new AiDe.Core.Watcher.InMemoryWatcherObservationStore();
            store.RecordEpisode(new AiDe.Core.Watcher.WorkEpisode(
                "e1", "s1", new AiDe.Core.Watcher.EpisodeGeneration(1),
                new AiDe.Core.Watcher.Goal("Ship the ledger"), new AiDe.Core.Watcher.DoneCondition("done"),
                null, DateTimeOffset.UnixEpoch, null, null));
            var query = new WatcherLedgerQuery(store);

            var content = new SurfaceContentFactory(null, null, null, null, null, query)
                .Create(new Surface("ledger", "ledger", "Ledger"));
            var stack = Assert.IsType<StackPanel>(Unwrap(content));
            var list = stack.Children.OfType<ListBox>().Single();
            var status = stack.Children.OfType<TextBlock>().Single();
            return new SurfaceView(list.ItemsSource?.Cast<object>().Count() ?? 0, status.Text);
        });

        Assert.Equal(1, view.ItemCount);
        Assert.DoesNotContain("Loading", view.StatusText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Layout.Default().AllStacks().SelectMany(s => s.Surfaces), s => s.Kind == "ledger");
    }

    [Fact]
    public void TheLedgerSurface_WithNoWatcherStore_SaysUnavailable_NotBlank()
    {
        var status = OnStaThread(() =>
        {
            var content = new SurfaceContentFactory(null).Create(new Surface("ledger", "ledger", "Ledger"));
            var host = Assert.IsType<Grid>(Unwrap(content));
            return host.Children.OfType<TextBlock>().Single().Text;
        });

        Assert.Contains("not available", status, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Loading", status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheSessionsSurface_LeadsWithLiveSessions_AndCollapsesTheInactiveHistory()
    {
        // The graveyard fix (UX-SESSIONS-GRAVEYARD, smoke video 2026-09-02): one Alive session must be
        // visible up top while the stale/ended history is collapsed behind a count, not rendered as a
        // wall that buries the live one.
        var view = OnStaThread(() =>
        {
            WatcherSessionSnapshot Snap(string id, LivenessState liveness) => new(
                id,
                new SessionBinding(
                    new RepositoryIdentity("C:/repos/ai-de", "ai-de"),
                    new WorktreeIdentity(new RepositoryIdentity("C:/repos/ai-de", "ai-de"), "main", "C:/repos/ai-de"),
                    new TerminalIdentity(id),
                    new AgentIdentity(id),
                    new HarnessIdentity("Claude Code", "1.0"),
                    new ModelIdentity("Opus 5", "2026-09"),
                    TrustClassification.Verified),
                liveness,
                liveness == LivenessState.Alive ? 3 : 0);

            var query = new StubSessionsQuery(
                Snap("live-1", LivenessState.Alive),
                Snap("dead-1", LivenessState.Ended),
                Snap("dead-2", LivenessState.Stale),
                Snap("dead-3", LivenessState.Ended));

            var content = new SurfaceContentFactory(null, query).Create(new Surface("sessions", "sessions", "Sessions"));
            var stack = Assert.IsType<StackPanel>(Unwrap(content));

            // The live session is in a visible ScrollViewer at the top.
            var liveScroller = stack.Children.OfType<ScrollViewer>().First();
            var liveRows = Assert.IsType<StackPanel>(liveScroller.Content);

            // The stale+ended history is collapsed inside an Expander whose header states the count.
            var expander = stack.Children.OfType<System.Windows.Controls.Expander>().Single();
            return (LiveCount: liveRows.Children.Count, Expanded: expander.IsExpanded, Header: expander.Header?.ToString() ?? "");
        });

        Assert.Equal(1, view.LiveCount);                                   // one live row, up top
        Assert.False(view.Expanded);                                       // inactive history collapsed by default
        Assert.Contains("3 inactive", view.Header, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A watcher read that answers immediately with a fixed set of sessions.</summary>
    private sealed class StubSessionsQuery(params WatcherSessionSnapshot[] sessions) : IWatcherSessionsQuery
    {
        public IReadOnlyList<WatcherSessionSnapshot> GetSessions() => sessions;
    }

    /// <summary>A watcher read that answers immediately with a fixed set of board posts.</summary>
    private sealed class StubBoardQuery(params BoardMessage[] messages) : IWatcherBoardQuery
    {
        public IReadOnlyList<BoardMessage> GetMessages() => messages;
    }

    /// <summary>A watcher read that answers immediately with a fixed set of scored episodes.</summary>
    private sealed class StubLeaderboardQuery(params ScoredEpisode[] episodes) : IWatcherLeaderboardQuery
    {
        public IReadOnlyList<ScoredEpisode> GetScoredEpisodes() => episodes;
    }

    /// <summary>A read surface that fails, standing in for a daemon that cannot be reached.</summary>
    private sealed class ThrowingQueries : FakeWorkspaceQueries
    {
        public override Task<DescribeResult> DescribeAsync(string nodeId, int maxNeighbors, CancellationToken ct) =>
            throw new InvalidOperationException("the daemon is not reachable");

        public override Task<ImpactResult> ImpactAsync(string nodeId, int maxNodes, int maxEdges, CancellationToken ct) =>
            throw new InvalidOperationException("the daemon is not reachable");

        public override Task<FindResult> FindAsync(string term, int maxResults, CancellationToken ct) =>
            throw new InvalidOperationException("the daemon is not reachable");

        public override Task<KnowledgeResult> KnowledgeAsync(string? term, string? type, int maxResults, CancellationToken ct) =>
            throw new InvalidOperationException("the daemon is not reachable");

        public override Task<EvidencePage> EvidenceAsync(string? cursor, int maxAssertions, CancellationToken ct) =>
            throw new InvalidOperationException("the daemon is not reachable");


        public override Task<PathResult> PathsAsync(PathQuery query, CancellationToken ct) =>
            Task.FromResult(new PathResult([], false, null, "rev-1"));

        public override Task<WorkspaceGraph> GraphAsync(GraphQuery query, CancellationToken ct) =>
            throw new InvalidOperationException("the daemon is not reachable");

    }
}
