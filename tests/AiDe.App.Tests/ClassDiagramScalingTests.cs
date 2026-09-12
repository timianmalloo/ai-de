using System.Diagnostics;
using AiDe.App.Workbench;
using AiDe.Core.Facts;
using AiDe.Core.Presentation;
using AiDe.Core.Projections;
using AiDe.Testing;

namespace AiDe.App.Tests;

/// <summary>
/// Ruling 54's class-diagram scaling fix, measured (ADR-0029: recorded, never asserted — a number,
/// not a pass) against a synthetic workspace shaped like the one the operator reported: a graph
/// dominated by knowledge nodes, this workspace's own order of magnitude (~1,500 nodes).
/// </summary>
/// <remarks>
/// <para><b>Why this shape, not an arbitrary large graph.</b> <c>ClassHierarchyModel.IsType</c>
/// already discards non-class kinds, so a mixed graph is CORRECT either way — the defect is at the
/// CAP, which runs on the unfiltered set (<c>CanvasGraphViewModel.OverviewNodeCap = 1,500</c>, before
/// SH-3 unconditionally). Knowledge nodes measured elsewhere in this codebase have a median relation
/// degree of 0 (<c>GraphProjectionTests</c>, <c>CanvasGraphViewModel</c> remarks) — the fixture below
/// gives every knowledge node degree 1 and every class degree 0, so the OLD ranking
/// (declared-first, then degree) places every knowledge node ahead of every class. With exactly
/// <c>OverviewNodeCap</c> knowledge nodes present, the pre-fix path draws ZERO classes: not a
/// contrived worst case, but the ordinary consequence of "most-connected-first" when the corpus that
/// crowds out the code happens to reference itself more than the code does.</para>
///
/// <para><b>The fix is <c>CanvasGraphViewModel.KindFilter = ClassHierarchyModel.TypeKinds</c>,
/// not <c>ExcludeKnowledge</c>.</b> Both would pass this fixture (it only has classes and one
/// knowledge kind), but the kind allow-list is the narrower, already-wired mechanism
/// (<c>GraphQuery.Kinds</c> crosses the Graph and Overview wire today) and keeps Ruling 53's
/// reading-experience filter (knowledge-only exclusion) separate from Ruling 54's cap-budget
/// problem (patterns-expert review) — a real workspace with tables, azure resources or functions
/// competing for the cap would show the two mechanisms diverge; this fixture does not need to,
/// since it exists to pin the numbers, not to distinguish the two.</para>
/// </remarks>
public sealed class ClassDiagramScalingTests
{
    private const int ClassCount = 500;
    private const int KnowledgeCount = CanvasGraphViewModel.OverviewNodeCap; // fills the cap alone

    private static EvidenceAssertion Say(string subject, string predicate, string obj) =>
        new("scope", "rev-1", subject, predicate, obj, EvidenceOrigin.Static, VerificationStatus.Verified,
            new Provenance("test", null, "test", "1", DateTimeOffset.UnixEpoch));

    private static IReadOnlyList<EvidenceAssertion> BuildFixture()
    {
        var assertions = new List<EvidenceAssertion>();

        // 500 classes, declared, degree 0 — a leaf-heavy but entirely ordinary type set.
        for (var i = 0; i < ClassCount; i++)
        {
            assertions.Add(Say($"Shop.Type{i}", "has_type", "class"));
        }

        // 1,500 knowledge nodes, declared, degree 1 (each references the next) — median degree 0 in
        // the real corpus this codebase measured elsewhere; giving them exactly 1 is the smallest
        // degree that still ranks them ahead of the classes' 0 under the OLD unfiltered ranking.
        for (var i = 0; i < KnowledgeCount; i++)
        {
            assertions.Add(Say($"kb.Doc{i}", "has_type", "doc"));
            assertions.Add(Say($"kb.Doc{i}", "node_class", "knowledge"));
            assertions.Add(Say($"kb.Doc{i}", "references", $"kb.Doc{(i + 1) % KnowledgeCount}"));
        }

        return assertions;
    }

    private sealed class ProjectionBackedQueries(IReadOnlyList<EvidenceAssertion> assertions) : FakeWorkspaceQueries
    {
        private readonly GraphProjection _projection = new(assertions, "rev-1");

        public override Task<WorkspaceGraph> GraphAsync(GraphQuery query, CancellationToken ct) =>
            Task.FromResult(_projection.Compute(query));
    }

    [Fact]
    public async Task ApplyingTheTypeKindFilterBeforeTheCap_RendersMoreRealTypes_AndIsNotSlower()
    {
        var queries = new ProjectionBackedQueries(BuildFixture());

        var before = Stopwatch.StartNew();
        var unfiltered = await new CanvasGraphViewModel(queries).LoadAsync();
        before.Stop();
        var beforeTypes = ClassHierarchyModel.Build(unfiltered.Nodes, unfiltered.Edges).Types.Count;

        var after = Stopwatch.StartNew();
        var filtered = await new CanvasGraphViewModel(queries)
        {
            KindFilter = [.. ClassHierarchyModel.TypeKinds],
        }.LoadAsync();
        after.Stop();
        var afterTypes = ClassHierarchyModel.Build(filtered.Nodes, filtered.Edges).Types.Count;

        // Recorded, not asserted (ADR-0029) — the numbers this run measured, for the Proof Pack.
        Console.WriteLine(
            $"class-diagram scaling: before(no filter)={beforeTypes} type(s) of {ClassCount} in {before.ElapsedMilliseconds} ms; "
            + $"after(KindFilter=TypeKinds)={afterTypes} type(s) of {ClassCount} in {after.ElapsedMilliseconds} ms; "
            + $"cap={CanvasGraphViewModel.OverviewNodeCap}, knowledge fixture size={KnowledgeCount}");

        // The one thing this DOES assert (a correctness floor, not a performance claim): excluding
        // knowledge before the cap never draws FEWER real types than including it, and here — by
        // construction — strictly more: the knowledge corpus fills the cap on its own.
        Assert.Equal(0, beforeTypes);
        Assert.Equal(ClassCount, afterTypes);
    }
}
