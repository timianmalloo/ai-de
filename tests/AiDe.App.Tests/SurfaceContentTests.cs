using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Projections;
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
    private static T OnStaThread<T>(Func<T> work)
    {
        var result = default(T);
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                result = work();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw new InvalidOperationException("the STA body threw", failure);
        }

        return result!;
    }

    /// <summary>Unwraps the SurfaceChrome island frame the factory puts around non-windowed panes.</summary>
    private static FrameworkElement Unwrap(FrameworkElement content) =>
        content is System.Windows.Controls.Border { Child: FrameworkElement inner } ? inner : content;

    /// <summary>A read surface that answers immediately with one known match.</summary>
    private sealed class StubQueries : IWorkspaceQueries
    {
        public Task<DescribeResult> DescribeAsync(string nodeId, int maxNeighbors, CancellationToken ct) =>
            Task.FromResult(new DescribeResult(
                new NodeView(nodeId, "kind", nodeId), [],
                new ResultBounds(1, 1, 1024, 1, 0, 0, 0, false, null), "rev-1"));

        public Task<ImpactResult> ImpactAsync(string nodeId, int maxNodes, int maxEdges, CancellationToken ct) =>
            Task.FromResult(new ImpactResult(
                nodeId, [], [], new ResultBounds(1, 1, 1024, 0, 0, 0, 0, false, null), "rev-1"));

        public Task<FindResult> FindAsync(string term, int maxResults, CancellationToken ct) =>
            Task.FromResult(new FindResult(
                [new FindMatch("Service.Orders", "csharp.type", "Service.Orders", AuthorshipOrigin.RepositoryArtifact)],
                new ResultBounds(1, 1, 1024, 1, 0, 0, 0, false, null),
                "rev-1"));

        public Task<KnowledgeResult> KnowledgeAsync(string? term, string? type, int maxResults, CancellationToken ct) =>
            Task.FromResult(new KnowledgeResult(
                [], new ResultBounds(1, 1, 1024, 0, 0, 0, 0, false, null), "rev-1"));

        public Task<EvidencePage> EvidenceAsync(string? cursor, int maxAssertions, CancellationToken ct) =>
            Task.FromResult(new EvidencePage([], null, "rev-1"));


        public Task<WorkspaceGraph> GraphAsync(GraphQuery query, CancellationToken ct) =>
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
    public void WithNoWorkspace_TheSurfaceSaysWhatIsUnavailable()
    {
        var text = OnStaThread(() =>
        {
            var content = new SurfaceContentFactory(null).Create(new Surface("view-1", "view", "Explore"));
            return Assert.IsType<TextBlock>(Unwrap(content)).Text;
        });

        Assert.Contains("not available", text, StringComparison.OrdinalIgnoreCase);
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

    /// <summary>A read surface that fails, standing in for a daemon that cannot be reached.</summary>
    private sealed class ThrowingQueries : IWorkspaceQueries
    {
        public Task<DescribeResult> DescribeAsync(string nodeId, int maxNeighbors, CancellationToken ct) =>
            throw new InvalidOperationException("the daemon is not reachable");

        public Task<ImpactResult> ImpactAsync(string nodeId, int maxNodes, int maxEdges, CancellationToken ct) =>
            throw new InvalidOperationException("the daemon is not reachable");

        public Task<FindResult> FindAsync(string term, int maxResults, CancellationToken ct) =>
            throw new InvalidOperationException("the daemon is not reachable");

        public Task<KnowledgeResult> KnowledgeAsync(string? term, string? type, int maxResults, CancellationToken ct) =>
            throw new InvalidOperationException("the daemon is not reachable");

        public Task<EvidencePage> EvidenceAsync(string? cursor, int maxAssertions, CancellationToken ct) =>
            throw new InvalidOperationException("the daemon is not reachable");


        public Task<WorkspaceGraph> GraphAsync(GraphQuery query, CancellationToken ct) =>
            throw new InvalidOperationException("the daemon is not reachable");

    }
}
