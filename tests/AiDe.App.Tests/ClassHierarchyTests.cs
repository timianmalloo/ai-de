using AiDe.App.Workbench;
using AiDe.Core.Presentation;

namespace AiDe.App.Tests;

/// <summary>
/// The class-hierarchy projection (ADR-0020): a pure function from graph nodes/edges to a UML type
/// hierarchy, so it is verified headlessly. Members are out of scope (not extracted yet).
/// </summary>
public sealed class ClassHierarchyTests
{
    private static CanvasNode N(string id, string kind) => new(id, id.Split('.')[^1], kind, false, null);
    private static CanvasEdge E(string from, string to, string pred) => new(from, to, pred, "Verified");

    [Fact]
    public void Build_KeepsOnlyClassAndInterfaceNodes()
    {
        var nodes = new[]
        {
            N("Shop.Order", "class"),
            N("Shop.IRepo", "interface"),
            N("Shop.Orders", "table"),        // data — excluded
            N("infra.web", "azure-resource"), // infra — excluded
            N("Shop.Total", "method"),        // member-ish — excluded
            N("Shop.Kind", "enum"),           // enums excluded from the hierarchy
        };

        var h = ClassHierarchyModel.Build(nodes, []);

        Assert.Equal(2, h.Types.Count);
        Assert.Contains(h.Types, t => t.Id == "Shop.Order" && !t.IsInterface);
        Assert.Contains(h.Types, t => t.Id == "Shop.IRepo" && t.IsInterface);
    }

    [Fact]
    public void Build_MapsInheritsToGeneralization_AndImplementsToRealization()
    {
        var nodes = new[] { N("A", "class"), N("B", "class"), N("I", "interface") };
        var edges = new[] { E("A", "B", "inherits"), E("A", "I", "implements"), E("A", "B", "calls") };

        var h = ClassHierarchyModel.Build(nodes, edges);

        Assert.Equal(2, h.Relations.Count);   // calls is dropped
        Assert.Contains(h.Relations, r => r is { From: "A", To: "B", Kind: ClassRelationKind.Generalization });
        Assert.Contains(h.Relations, r => r is { From: "A", To: "I", Kind: ClassRelationKind.Realization });
    }

    [Fact]
    public void Build_CountsRelationsToExternalTargets_WithoutDrawingThem()
    {
        var nodes = new[] { N("A", "class") };
        // Base type and interface are outside the analysed scope (not kept type nodes).
        var edges = new[] { E("A", "System.Object", "inherits"), E("A", "System.IDisposable", "implements") };

        var h = ClassHierarchyModel.Build(nodes, edges);

        Assert.Empty(h.Relations);
        Assert.Equal(2, h.ExternalRelations);
    }

    [Fact]
    public void Build_IgnoresRelationsFromNonTypeSources()
    {
        var nodes = new[] { N("A", "class") };
        var edges = new[] { E("Shop.Orders", "A", "inherits") };  // From a table, not a type

        var h = ClassHierarchyModel.Build(nodes, edges);

        Assert.Empty(h.Relations);
        Assert.Equal(0, h.ExternalRelations);
    }

    [Fact]
    public void Build_DeduplicatesRepeatedRelations()
    {
        var nodes = new[] { N("A", "class"), N("B", "class") };
        var edges = new[] { E("A", "B", "inherits"), E("A", "B", "inherits") };

        var h = ClassHierarchyModel.Build(nodes, edges);

        Assert.Single(h.Relations);
    }

    [Fact]
    public void Build_EmptyWhenNoTypes()
    {
        var h = ClassHierarchyModel.Build([N("Shop.Orders", "table")], [E("Shop.Orders", "x", "inherits")]);
        Assert.True(h.IsEmpty);
        Assert.Empty(h.Relations);
    }

    [Fact]
    public void Build_CarriesContextForGrouping()
    {
        var nodes = new[]
        {
            new CanvasNode("Shop.Order", "Order", "class", false, "Shop"),
            new CanvasNode("Billing.Invoice", "Invoice", "class", false, "Billing"),
        };

        var h = ClassHierarchyModel.Build(nodes, []);

        Assert.Equal("Shop", h.Types.Single(t => t.Id == "Shop.Order").Context);
        Assert.Equal("Billing", h.Types.Single(t => t.Id == "Billing.Invoice").Context);
    }

    [Fact]
    public void Filter_KeepsMatchingTypes_AndDropsRelationsToFilteredTargets()
    {
        var full = ClassHierarchyModel.Build(
            new[] { N("Shop.Order", "class"), N("Shop.OrderLine", "class"), N("Shop.Customer", "class") },
            new[] { E("Shop.OrderLine", "Shop.Order", "inherits"), E("Shop.Order", "Shop.Customer", "implements") });

        var f = ClassHierarchyModel.Filter(full, "Order");

        Assert.Equal(2, f.Types.Count);                                   // Order, OrderLine
        Assert.DoesNotContain(f.Types, t => t.Id == "Shop.Customer");
        Assert.Single(f.Relations);                                      // OrderLine->Order survives
        Assert.Equal(1, f.ExternalRelations);                            // Order->Customer target filtered out
    }

    [Fact]
    public void Filter_EmptyTerm_ReturnsUnchanged()
    {
        var full = ClassHierarchyModel.Build(new[] { N("A", "class") }, []);
        Assert.Same(full, ClassHierarchyModel.Filter(full, "  "));
        Assert.Same(full, ClassHierarchyModel.Filter(full, null));
    }

    [Fact]
    public void Filter_IsCaseInsensitive()
    {
        var full = ClassHierarchyModel.Build(new[] { N("Shop.Invoice", "class") }, []);
        Assert.Single(ClassHierarchyModel.Filter(full, "invoice").Types);
    }

    [Fact]
    public void Build_HandlesNulls()
    {
        var h = ClassHierarchyModel.Build(null, null);
        Assert.True(h.IsEmpty);
    }
}
