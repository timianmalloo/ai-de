using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// Deriving UML association/aggregation from members (uml-richer-relations): a field/property whose
/// type is another drawn type is a structural "has-a"; a collection of it is an aggregation.
/// </summary>
public sealed class ClassHierarchyAssociationTests
{
    private static readonly IReadOnlyList<ClassTypeNode> Types =
    [
        new("cart", "Cart", false, null),
        new("order", "Order", false, null),
        new("item", "Item", false, null),
    ];

    private static IReadOnlyList<ClassRelation> Derive(params string[] cartMembers) =>
        ClassHierarchyModel.DeriveAssociations(
            Types,
            new Dictionary<string, IReadOnlyList<string>> { ["cart"] = cartMembers });

    [Fact]
    public void ASingleTypedField_IsAnAssociation()
    {
        var rels = Derive("+ ActiveOrder : Order");
        var rel = Assert.Single(rels);
        Assert.Equal("cart", rel.From);
        Assert.Equal("order", rel.To);
        Assert.Equal(ClassRelationKind.Association, rel.Kind);
    }

    [Theory]
    [InlineData("+ Items : List<Item>")]
    [InlineData("+ Items : IReadOnlyList<Item>")]
    [InlineData("+ Items : Item[]")]
    [InlineData("+ Items : Dictionary<string, Item>")]
    public void ACollectionOfADrawnType_IsAnAggregation(string member)
    {
        var rel = Assert.Single(Derive(member));
        Assert.Equal("item", rel.To);
        Assert.Equal(ClassRelationKind.Aggregation, rel.Kind);
    }

    [Fact]
    public void Methods_AreSkipped()
    {
        Assert.Empty(Derive("+ Process(Order) : void", "+ Find(int) : Item"));
    }

    [Fact]
    public void AFieldOfANonDrawnType_ProducesNoRelation()
    {
        Assert.Empty(Derive("- _count : int", "+ Name : string"));
    }

    [Fact]
    public void ASelfTypedField_IsSkipped()
    {
        Assert.Empty(ClassHierarchyModel.DeriveAssociations(
            Types, new Dictionary<string, IReadOnlyList<string>> { ["order"] = ["+ Parent : Order"] }));
    }

    [Fact]
    public void NullableAndNamespacedTypes_StillMatch()
    {
        var rels = Derive("+ Latest : MyApp.Domain.Order?");
        var rel = Assert.Single(rels);
        Assert.Equal("order", rel.To);
        Assert.Equal(ClassRelationKind.Association, rel.Kind);
    }

    [Fact]
    public void DuplicateFieldsOfTheSameType_ProduceOneAssociation()
    {
        Assert.Single(Derive("+ First : Order", "+ Second : Order"));
    }
}
