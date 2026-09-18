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
    public void Fep_ComposedOracle_KindsAndUnclassifiedNeverSilent()
    {
        using var workspace = TestWorkspace.Create();
        workspace.CommitSnapshot(
            "fixture", 1, "rev-1",
            TestWorkspace.Assertion("Orders.OrdersController", "has_type", "class"),
            TestWorkspace.Assertion("Shell.MainWindow", "has_type", "class"),
            TestWorkspace.Assertion("App.Program", "has_type", "class"),
            TestWorkspace.Assertion("App.Program", "has_member", "+ Main()"),
            TestWorkspace.Assertion("Domain.Order", "has_type", "class"));
        var projections = new ProjectionService(workspace.Store, Path.GetDirectoryName(workspace.DatabasePath)!);
        var result = projections.EntryPoints(new EntryPointsQuery());
        Assert.Equal(5, result.Rows.Count); // F-EP five-row oracle (N4 PWC)
        Assert.Equal(
            (EntryPointKind.Api, "Orders.OrdersController"),
            (result.Rows.Single(r => r.Display == "Orders.OrdersController").Kind,
             result.Rows.Single(r => r.Display == "Orders.OrdersController").NodeId));
        Assert.Equal(
            (EntryPointKind.Ux, "Shell.MainWindow"),
            (result.Rows.Single(r => r.Display == "Shell.MainWindow").Kind,
             result.Rows.Single(r => r.Display == "Shell.MainWindow").NodeId));
        var program = result.Rows.Single(r => r.Display == "App.Program");
        Assert.Equal(EntryPointKind.Cli, program.Kind);
        Assert.Equal("App.Program", program.NodeId);
        var main = result.Rows.Single(r => r.Display.Contains("Main()", StringComparison.Ordinal));
        Assert.Equal(EntryPointKind.Cli, main.Kind);
        Assert.Equal("App.Program", main.NodeId);
        var order = result.Rows.Single(r => r.Display == "Domain.Order");
        Assert.Equal(EntryPointKind.Unclassified, order.Kind);
        Assert.Equal("Domain.Order", order.NodeId);
        Assert.Equal(EntryPointsProjection.UnclassifiedReasonPendingClassifier, order.UnclassifiedReason);
        Assert.Equal(0, result.OmittedByCap);
    }

    [Fact]
    public void CallerMaxRows_IsClampedToDefaultCeiling()
    {
        using var workspace = TestWorkspace.Create();
        workspace.CommitSnapshot(
            "fixture", 1, "rev-1",
            TestWorkspace.Assertion("A", "has_type", "class"),
            TestWorkspace.Assertion("B", "has_type", "class"));
        var projections = new ProjectionService(workspace.Store, Path.GetDirectoryName(workspace.DatabasePath)!);
        var huge = projections.EntryPoints(new EntryPointsQuery(MaxRows: 100_000));
        Assert.True(huge.Rows.Count <= EntryPointsProjection.DefaultMaxRows);
        Assert.Equal(0, huge.OmittedByCap);
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
    public void KindFromDisplay_ApiUxCli_ElseUnclassified()
    {
        Assert.Equal(EntryPointKind.Api, EntryPointsListing.KindFromDisplay("Orders.OrdersController"));
        Assert.Equal(EntryPointKind.Cli, EntryPointsListing.KindFromDisplay("App.Program"));
        Assert.Equal(EntryPointKind.Cli, EntryPointsListing.KindFromDisplay("Main"));
        Assert.Equal("Main", EntryPointsListing.MemberBareName("+ Main()"));
        Assert.Equal(EntryPointKind.Ux, EntryPointsListing.KindFromDisplay("Shell.MainWindow"));
        Assert.Equal(EntryPointKind.Unclassified, EntryPointsListing.KindFromDisplay("Domain.Order"));
    }

    [Fact]
    public void HasMember_MainIsCli_GraphIdIsDeclaringType()
    {
        using var workspace = TestWorkspace.Create();
        workspace.CommitSnapshot(
            "fixture", 1, "rev-1",
            TestWorkspace.Assertion("App.Program", "has_type", "class"),
            TestWorkspace.Assertion("App.Program", "has_member", "+ Main()"));
        var projections = new ProjectionService(workspace.Store, Path.GetDirectoryName(workspace.DatabasePath)!);
        var result = projections.EntryPoints(new EntryPointsQuery());
        var main = Assert.Single(result.Rows, r => r.Display.Contains("Main()", StringComparison.Ordinal));
        Assert.Equal(EntryPointKind.Cli, main.Kind);
        Assert.Equal("App.Program", main.NodeId);
    }

    [Fact]
    public void MembersTruncated_DisclosesFortyCap()
    {
        using var workspace = TestWorkspace.Create();
        workspace.CommitSnapshot(
            "fixture", 1, "rev-1",
            TestWorkspace.Assertion("Huge", "has_type", "class"),
            TestWorkspace.Assertion("Huge", "members_truncated", "300"));
        var projections = new ProjectionService(workspace.Store, Path.GetDirectoryName(workspace.DatabasePath)!);
        var result = projections.EntryPoints(new EntryPointsQuery());
        Assert.Contains(result.Disclosures, d => d.Contains("40 members", StringComparison.Ordinal));
    }

    [Fact]
    public void EmptyIndex_RowsEmpty_OmitZero()
    {
        using var workspace = TestWorkspace.Create();
        workspace.CommitSnapshot("fixture", 1, "rev-1");
        var projections = new ProjectionService(workspace.Store, Path.GetDirectoryName(workspace.DatabasePath)!);
        var result = projections.EntryPoints(new EntryPointsQuery());
        Assert.Empty(result.Rows);
        Assert.Equal(0, result.OmittedByCap);
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
