using System.Reflection;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Projections;
using AiDe.Core.Store;

namespace AiDe.Core.Tests;

/// <summary>
/// No read operation can build a response the transport will refuse — including one added tomorrow.
/// </summary>
/// <remarks>
/// <para><b>Why this is reflective rather than a list.</b> INV-0003 was found by a user opening a
/// repository. The follow-up audit was run BY HAND against one repository and found two more
/// (<c>evidence</c> at 95.8% of the frame, <c>find</c> returning 461,750 bytes while reporting a
/// 64 KiB cap). Hand-auditing found them once; nothing would find the next one, because the next one
/// will be an operation nobody has added yet.</para>
///
/// <para>So the operation list comes from <see cref="IWorkspaceQueries"/> itself. Writing the list
/// out would be a fixture restating the product's own list (DC-021) and would go stale in exactly
/// the case that matters — a new method with no byte bound, which is precisely how the last three
/// got in.</para>
///
/// <para><b>The fixture is deliberately hostile</b>: long identifiers, because every ceiling in the
/// read surface counts ITEMS while the transport limit is in BYTES, and item size comes from
/// repository content.</para>
/// </remarks>
public sealed class EveryOperationFitsTheFrameTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "aide-frame", Guid.NewGuid().ToString("N"));

    public EveryOperationFitsTheFrameTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    private ProjectionService Hostile()
    {
        var store = WorkspaceStore.Open(Path.Combine(_dir, "facts.db"));
        var padding = new string('N', 300);
        var facts = new List<EvidenceAssertion>();

        for (var i = 0; i < 3_000; i++)
        {
            var subject = $"Long.Namespace.{padding}.Type{i}";

            facts.Add(new EvidenceAssertion(
                "scope", "rev-1", subject, "has_type", "class",
                EvidenceOrigin.Static, VerificationStatus.Verified,
                new Provenance($"src/{padding}/File{i}.cs", "1:1", "test", "1.0.0", DateTimeOffset.UnixEpoch)));

            // A hub, so Describe and Impact have a worst case worth measuring.
            facts.Add(new EvidenceAssertion(
                "scope", "rev-1", subject, "depends_on", $"Long.Namespace.{padding}.Hub",
                EvidenceOrigin.Static, VerificationStatus.Verified,
                new Provenance($"src/{padding}/File{i}.cs", "1:1", "test", "1.0.0", DateTimeOffset.UnixEpoch)));
        }

        facts.Add(new EvidenceAssertion(
            "scope", "rev-1", $"Long.Namespace.{padding}.Hub", "has_type", "class",
            EvidenceOrigin.Static, VerificationStatus.Verified,
            new Provenance("src/Hub.cs", "1:1", "test", "1.0.0", DateTimeOffset.UnixEpoch)));

        using (var writer = store.BeginWrite())
        {
            writer.DesireScopeGeneration("scope", 1, "rev-1");
            writer.CommitSnapshot("scope", 1, "rev-1", facts, complete: true);
            writer.Commit();
        }

        return new ProjectionService(store);
    }

    private static int WireBytes(object payload) =>
        System.Text.Encoding.UTF8.GetByteCount(
            System.Text.Json.JsonSerializer.Serialize(
                payload, payload.GetType(),
                new System.Text.Json.JsonSerializerOptions(
                    System.Text.Json.JsonSerializerDefaults.Web)));

    /// <summary>Each operation invoked at its most expensive legal request.</summary>
    private static Dictionary<string, Func<ProjectionService, string, object>> AtCeiling() => new(StringComparer.Ordinal)
    {
        [nameof(IWorkspaceQueries.EvidenceAsync)] =
            (p, _) => p.Evidence(null, ProjectionService.MaxEvidencePageCeiling),

        [nameof(IWorkspaceQueries.FindAsync)] =
            (p, _) => p.Find("Type", ProjectionService.MaxSearchResultsCeiling),

        [nameof(IWorkspaceQueries.KnowledgeAsync)] =
            (p, _) => p.Knowledge(new KnowledgeQuery(null, null, ProjectionService.MaxNeighborsCeiling)),

        [nameof(IWorkspaceQueries.DescribeAsync)] =
            (p, hub) => p.Describe(hub, ProjectionService.MaxNeighborsCeiling),

        [nameof(IWorkspaceQueries.ImpactAsync)] =
            (p, hub) => p.Impact(hub, ProjectionService.MaxNodesCeiling, ProjectionService.MaxEdgesCeiling),

        [nameof(IWorkspaceQueries.GraphAsync)] =
            (p, _) => p.Graph(new GraphQuery(GraphProjection.DefaultMaxNodes)),

        // The one operation here whose size comes from a FILE rather than from the fact table, which
        // is exactly why it needs weighing: repository content is unbounded and a frame is not.
        [nameof(IWorkspaceQueries.NodeContentAsync)] =
            (p, hub) => p.NodeContent(hub),

        [nameof(IWorkspaceQueries.PathsAsync)] =
            (p, hub) => p.Paths(new PathQuery(hub, hub, ProjectionService.MaxPathsCeiling, ProjectionService.MaxPathLengthCeiling)),

        [nameof(IWorkspaceQueries.OverviewAsync)] =
            (p, _) => p.Overview(new OverviewQuery(1, ProjectionService.MaxClustersCeiling)),
    };

    [Fact]
    public void EveryReadOperationIsCoveredByThisTest()
    {
        // THE CONTROL ON THE CONTROL. A new method on the read surface with no entry here is a new
        // way to overflow the frame that nobody is watching — which is how the last three got in.
        var declared = typeof(IWorkspaceQueries)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        var covered = AtCeiling().Keys.ToHashSet(StringComparer.Ordinal);

        var missing = declared.Except(covered).Order(StringComparer.Ordinal).ToList();

        Assert.True(missing.Count == 0,
            $"IWorkspaceQueries gained {string.Join(", ", missing)} with no frame-size check. " +
            "Add it to AtCeiling() — an operation whose response nobody has weighed is INV-0003 " +
            "waiting for a big enough repository.");
    }

    [Fact]
    public void NoOperationCanBuildAResponseTheTransportWouldRefuse()
    {
        var projections = Hostile();
        var hub = $"Long.Namespace.{new string('N', 300)}.Hub";

        var oversized = new List<string>();

        foreach (var (operation, invoke) in AtCeiling())
        {
            var bytes = WireBytes(invoke(projections, hub));

            if (bytes > IpcFraming.MaxFrameBytes)
            {
                oversized.Add($"{operation} = {bytes:N0} bytes");
            }
        }

        Assert.True(oversized.Count == 0,
            $"these responses cannot cross the {IpcFraming.MaxFrameBytes:N0}-byte frame: " +
            string.Join("; ", oversized));
    }
}
