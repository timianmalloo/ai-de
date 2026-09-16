using System.Text.Json;
using AiDe.Core.Ipc;
using AiDe.Core.Projections;

namespace AiDe.Core.Tests;

/// <summary>UV-0 D-1 listing: has_type nodes as unclassified; never silent-drop.</summary>
public sealed class EntryPointsProjectionTests
{
    [Fact]
    public void Catalog_NamesEntryPointsDistinctFromSolutionTree()
    {
        Assert.Equal("entry-points", WorkspaceOperations.EntryPoints);
        Assert.NotEqual(WorkspaceOperations.SolutionTree, WorkspaceOperations.EntryPoints);
    }

    [Fact]
    public void QueryJson_HasOnlyMaxRowsOnTheWire()
    {
        var json = JsonSerializer.Serialize(new EntryPointsQuery(), WorkspaceOperations.Wire);
        Assert.Contains("maxRows", json, StringComparison.Ordinal);
        Assert.DoesNotContain("observation", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HasTypeNodes_AreUnclassified_NeverSilentDrop()
    {
        using var workspace = TestWorkspace.Create();
        workspace.CommitSnapshot(
            "fixture", 1, "rev-1",
            TestWorkspace.Assertion("Api.Orders", "has_type", "class"),
            TestWorkspace.Assertion("Cli.Program", "has_type", "class"));
        var projections = new ProjectionService(workspace.Store, Path.GetDirectoryName(workspace.DatabasePath)!);
        var result = projections.EntryPoints(new EntryPointsQuery());
        Assert.Equal(2, result.Rows.Count);
        Assert.Contains(result.Rows, r => r.NodeId == "Api.Orders");
        Assert.Contains(result.Rows, r => r.NodeId == "Cli.Program");
        Assert.All(result.Rows, r =>
        {
            Assert.Equal(EntryPointKind.Unclassified, r.Kind);
            Assert.Equal(EntryPointsProjection.UnclassifiedReasonPendingClassifier, r.UnclassifiedReason);
        });
        Assert.Equal(0, result.OmittedByCap);
    }

    [Fact]
    public void Cap_DisclosesOmittedCount()
    {
        using var workspace = TestWorkspace.Create();
        workspace.CommitSnapshot(
            "fixture", 1, "rev-1",
            TestWorkspace.Assertion("A", "has_type", "class"),
            TestWorkspace.Assertion("B", "has_type", "class"));
        var projections = new ProjectionService(workspace.Store, Path.GetDirectoryName(workspace.DatabasePath)!);
        var capped = projections.EntryPoints(new EntryPointsQuery(MaxRows: 1));
        Assert.Single(capped.Rows);
        Assert.Equal(1, capped.OmittedByCap);
        Assert.Contains("Omitted", capped.Disclosures[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Listing_DoesNotCarryE1ObservationIds()
    {
        var json = JsonSerializer.Serialize(
            new EntryPointRow(EntryPointKind.Unclassified, "T", "T", "classifier-not-admitted"),
            WorkspaceOperations.Wire);
        Assert.DoesNotContain("observation", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sequence", json, StringComparison.OrdinalIgnoreCase);
    }
}
