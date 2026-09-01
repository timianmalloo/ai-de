using System.Linq;
using AiDe.App.Workbench;
using Xunit;

namespace AiDe.App.Tests;

/// <summary>
/// The contextual "Open as…" grammar (smoke 9-1 §3): each node type offers exactly the viewers it can
/// fill, driven by the producer's signal rather than a spelling guess (DC-042).
/// </summary>
public sealed class NodeViewMenuTests
{
    private static NodeViewKind[] Kinds(string? kind, bool isKnowledge = false) =>
        NodeViewMenu.OptionsFor(kind, isKnowledge).Select(o => o.Kind).ToArray();

    [Fact]
    public void AType_OffersSourceAndClassDiagram_NotSequence()
    {
        var kinds = Kinds("class");
        Assert.Contains(NodeViewKind.Source, kinds);
        Assert.Contains(NodeViewKind.ClassDiagram, kinds);
        Assert.DoesNotContain(NodeViewKind.Sequence, kinds);
    }

    [Fact]
    public void AMethod_OffersSequence_NotClassDiagram()
    {
        var kinds = Kinds("method");
        Assert.Contains(NodeViewKind.Source, kinds);
        Assert.Contains(NodeViewKind.Sequence, kinds);
        Assert.DoesNotContain(NodeViewKind.ClassDiagram, kinds);
    }

    [Fact]
    public void AKnowledgeNode_OffersRead_NotSource_EvenWhenItsKindIsSpec()
    {
        // The authoritative IsKnowledge flag wins over the kind spelling: a spec is a document, not
        // code, whatever its has_type reads (DC-042).
        var kinds = Kinds("spec", isKnowledge: true);
        Assert.Contains(NodeViewKind.Read, kinds);
        Assert.DoesNotContain(NodeViewKind.Source, kinds);
        Assert.DoesNotContain(NodeViewKind.ClassDiagram, kinds);
    }

    [Fact]
    public void AKnowledgeKind_BySpellingAlone_AlsoOffersRead()
    {
        Assert.Contains(NodeViewKind.Read, Kinds("investigation"));
        Assert.Contains(NodeViewKind.Read, Kinds("adr"));
    }

    [Fact]
    public void ADataShape_OffersSource_NotClassDiagram()
    {
        var kinds = Kinds("table");
        Assert.Contains(NodeViewKind.Source, kinds);
        Assert.DoesNotContain(NodeViewKind.ClassDiagram, kinds);
    }

    [Fact]
    public void AnUnknownOrExternalNode_OffersOnlyWhatAlwaysWorks_NeverAnEmptyViewer()
    {
        var kinds = Kinds("external");
        Assert.Contains(NodeViewKind.Metadata, kinds);
        Assert.Contains(NodeViewKind.GraphNeighbourhood, kinds);
        Assert.DoesNotContain(NodeViewKind.Source, kinds);
        Assert.DoesNotContain(NodeViewKind.ClassDiagram, kinds);
    }

    [Fact]
    public void EveryNode_OffersAtLeastOneChoice()
    {
        Assert.NotEmpty(NodeViewMenu.OptionsFor(null, false));
        Assert.NotEmpty(NodeViewMenu.OptionsFor("", false));
        Assert.NotEmpty(NodeViewMenu.OptionsFor("wat", false));
    }
}
