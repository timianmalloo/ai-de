using System.Text;
using System.Text.Json;
using AiDe.Core;
using AiDe.Core.Extraction;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Projections;

namespace AiDe.Core.Tests;

/// <summary>
/// Whatever the workspace holds, the graph that comes back fits in one message.
/// </summary>
/// <remarks>
/// <para><b>DC-047, made checkable.</b> The graph was shrunk to fit by one proportional correction
/// applied once and never re-checked. Nodes are kept in degree order, so the ones that survive a cut
/// are the most connected — and their edges are most of the payload. Cutting 15% of the nodes can
/// cut 2% of the bytes, so the single correction fell short and the response went out anyway:
/// 1,176,341 bytes against a 1,048,576 frame, reported to the user as "The graph could not be
/// loaded" on opening a workspace.</para>
///
/// <para><b>Measured against the real serialisation, not the estimator.</b> Asserting that the
/// estimate is under the estimate's own budget would be a control marking its own work — the
/// question is whether the bytes on the wire fit the frame, so the test serialises with the wire's
/// own options and counts.</para>
///
/// <para><b>The fixture is hub-shaped on purpose.</b> A uniform graph would shrink proportionally
/// and pass on the un-fixed code, which would make this a test of a fixture rather than of the
/// product (DC-028). Hub dominance IS the failure mode, and it is what real repositories look
/// like — the top nodes on TheTerrace carry hundreds of edges each.</para>
/// </remarks>
public sealed class TheGraphAlwaysFitsInAFrameTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "aide-frame", Guid.NewGuid().ToString("N"));

    public TheGraphAlwaysFitsInAFrameTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    private static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The bytes the TRANSPORT counts — the payload serialised, then carried as a string field in
    /// the envelope, where every quote in it is escaped again.
    /// </summary>
    /// <remarks>
    /// Measuring the payload alone is what let a 727,244-byte graph reach 1,137,104 bytes on the
    /// wire: the budget was checked on the inner bytes and enforced on the outer ones. The inflation
    /// measured 1.56-1.57x on every real payload weighed.
    /// </remarks>
    private static int OnTheWire(WorkspaceGraph graph) =>
        Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(
            IpcResponse.Success(JsonSerializer.Serialize(graph, Wire)), Wire));

    /// <summary>A graph whose weight sits in a few hundred hubs, the way a real one does.</summary>
    private WorkspaceCore Fill(int nodes, int hubs, int edgesPerHub)
    {
        var core = WorkspaceCore.Open("ws", _dir, Path.Combine(_dir, "data"), new FixtureExtractor());
        var assertions = new List<EvidenceAssertion>();

        // Ids as long as real ones. Counting nodes tells you nothing about bytes, which is the whole
        // reason the budget is in bytes.
        string Id(int i) => $"TheTerrace.Features.Membership.Application.Handlers.Generated.Type{i:D5}";

        var provenance = new Provenance("p", null, "test", "1", DateTimeOffset.UtcNow);

        for (var i = 0; i < nodes; i++)
        {
            assertions.Add(new EvidenceAssertion(
                "scope", "rev-1", Id(i), "has_type", "class",
                EvidenceOrigin.Static, VerificationStatus.Verified, provenance));
        }

        for (var h = 0; h < hubs; h++)
        {
            for (var e = 1; e <= edgesPerHub; e++)
            {
                // Distinct per hub: the natural key rejects the same triple twice, and a generator
                // that repeats one produces a store error rather than the graph being tested.
                var target = (h * edgesPerHub + e) % nodes;

                if (target == h) continue;

                assertions.Add(new EvidenceAssertion(
                    "scope", "rev-1", Id(h), "depends_on", Id(target),
                    EvidenceOrigin.Static, VerificationStatus.Verified, provenance));
            }
        }

        using var writer = core.Store.BeginWrite();
        writer.DesireScopeGeneration("scope", 1, "rev-1");
        writer.CommitSnapshot("scope", 1, "rev-1", assertions, complete: true);
        writer.Commit();

        return core;
    }

    [Fact]
    public void AGraphTooBigForOneMessageComesBackSmallEnoughForOne()
    {
        using var core = Fill(nodes: 3_900, hubs: 60, edgesPerHub: 4);

        var graph = core.Projections.Graph(new GraphQuery(GraphProjection.DefaultMaxNodes));

        Assert.True(OnTheWire(graph) <= IpcFraming.MaxFrameBytes,
            $"the graph serialises to {OnTheWire(graph):N0} bytes and one message carries "
            + $"{IpcFraming.MaxFrameBytes:N0} — this is the response the transport refuses");
    }

    [Fact]
    public void TheDefaultCanvasRequestFitsToo()
    {
        // The exact query the canvas makes when a workspace opens, which is where the user met this.
        using var core = Fill(nodes: 3_900, hubs: 60, edgesPerHub: 4);

        var graph = core.Projections.Graph(new GraphQuery(1_500, IncludeExternal: false));

        Assert.True(OnTheWire(graph) <= IpcFraming.MaxFrameBytes,
            $"the canvas's opening request serialises to {OnTheWire(graph):N0} bytes");
    }

    [Fact]
    public void ShrinkingStillReturnsAUsableGraphAndSaysWhatItLeftOut()
    {
        // Fitting by returning nothing would pass the assertion above and be a worse product.
        using var core = Fill(nodes: 3_900, hubs: 60, edgesPerHub: 4);

        var graph = core.Projections.Graph(new GraphQuery(GraphProjection.DefaultMaxNodes));

        Assert.True(graph.Nodes.Count > 100, $"only {graph.Nodes.Count} node(s) survived the shrink");
        Assert.True(graph.Omitted > 0, "nodes were dropped and Omitted does not say so");
    }

    [Fact]
    public void TheBudgetCannotDriftPastWhatAFrameHolds()
    {
        // The row-wise bounds (evidence, find) cannot afford to serialise per row, so they trust a
        // factor: the payload is assumed to at most double once escaped into the envelope. That
        // assumption is only safe while the budget is at most half a frame, and nothing else says so.
        Assert.True(ProjectionService.MaxResponseBytes * 2 <= ProjectionService.FrameBytes,
            $"a {ProjectionService.MaxResponseBytes:N0}-byte payload can reach "
            + $"{ProjectionService.MaxResponseBytes * 2:N0} bytes framed, and the frame is "
            + $"{ProjectionService.FrameBytes:N0}");

        Assert.True(ProjectionService.MaxFramedGraphBytes < ProjectionService.FrameBytes,
            "a graph shrunk to exactly the frame has no headroom, and shrinking stops at the first "
            + "size that fits");
    }

    [Fact]
    public void AGraphThatAlreadyFitsIsNotShrunk()
    {
        // The loop must be a response to a real overflow, not a cost paid by every workspace.
        using var core = Fill(nodes: 40, hubs: 5, edgesPerHub: 3);

        var graph = core.Projections.Graph(new GraphQuery(GraphProjection.DefaultMaxNodes));

        Assert.Equal(40, graph.Nodes.Count);
        Assert.Equal(0, graph.Omitted);
    }
}
