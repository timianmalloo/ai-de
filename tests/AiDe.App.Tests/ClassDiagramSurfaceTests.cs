using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Presentation;

namespace AiDe.App.Tests;

/// <summary>
/// The class-diagram surface (ADR-0020): renders a hierarchy built from graph nodes/edges. Host-side
/// WPF, so it runs on an STA thread with no WebView2.
/// </summary>
public sealed class ClassDiagramSurfaceTests
{
    private static void OnSta(Action work)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { work(); } catch (Exception ex) { failure = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "STA thread did not finish");
        if (failure is Xunit.Sdk.XunitException) throw failure;   // the message IS the finding (DC-078)

        if (failure is not null) { throw new InvalidOperationException("STA work failed", failure); }
    }

    private static CanvasNode N(string id, string kind) => new(id, id.Split('.')[^1], kind, false, null);
    private static CanvasEdge Edge(string from, string to, string pred) => new(from, to, pred, "Verified");

    [Fact]
    public void ShowGraph_RendersTheTypeHierarchy()
    {
        OnSta(() =>
        {
            var s = new ClassDiagramSurface();
            Assert.True(s.IsEmpty);

            s.ShowGraph(
                new[] { N("Shop.Order", "class"), N("Shop.IRepo", "interface"), N("Shop.Orders", "table") },
                new[] { Edge("Shop.Order", "Shop.IRepo", "implements") });

            Assert.Equal(2, s.TypeCount);      // table excluded
            Assert.Equal(1, s.RelationCount);  // one realization
            Assert.False(s.IsEmpty);
        });
    }

    [Fact]
    public void ShowGraph_EmptyWhenNoTypes_AndClearReturnsToEmpty()
    {
        OnSta(() =>
        {
            var s = new ClassDiagramSurface();
            s.ShowGraph(new[] { N("Shop.Orders", "table") }, []);
            Assert.True(s.IsEmpty);

            s.ShowGraph(new[] { N("A", "class"), N("B", "class") }, new[] { Edge("A", "B", "inherits") });
            Assert.Equal(2, s.TypeCount);
            Assert.Equal(1, s.RelationCount);

            s.Clear();
            Assert.True(s.IsEmpty);
            Assert.Equal(0, s.RelationCount);
        });
    }

    [Fact]
    public void ShowError_AfterPopulated_ClearsTypes_NotAMisleadingEmpty()
    {
        OnSta(() =>
        {
            var s = new ClassDiagramSurface();
            s.ShowGraph(new[] { N("A", "class"), N("B", "class") }, new[] { Edge("A", "B", "inherits") });
            Assert.False(s.IsEmpty);

            s.ShowError("daemon closed the connection");
            Assert.True(s.IsEmpty);            // no types claimed after a failed load
            Assert.Equal(0, s.RelationCount);

            s.ShowLoading();                   // does not throw
        });
    }

    [Fact]
    public void ShowGraph_DefaultsToTheVisualDiagram_WithABoxPerType_AndListModeSwitchesBack()
    {
        OnSta(() =>
        {
            var s = new ClassDiagramSurface();
            s.ShowGraph(
                new[] { N("A", "class"), N("B", "class"), N("C", "class") },
                new[] { Edge("A", "B", "inherits"), Edge("C", "B", "inherits") });

            // A "class diagram" shows a diagram by default — boxes and connectors, not a list.
            Assert.True(s.ShowingDiagram);
            Assert.Equal(3, s.DrawnBoxCount);

            s.SetDiagramMode(false);
            Assert.False(s.ShowingDiagram);   // the toggle falls back to the scannable list

            s.SetDiagramMode(true);
            Assert.True(s.ShowingDiagram);
        });
    }

    [Fact]
    public void HideInterfaces_DropsInterfaceTypesAndTheirRealizations()
    {
        OnSta(() =>
        {
            var s = new ClassDiagramSurface();
            s.ShowGraph(
                new[] { N("Shop.Order", "class"), N("Shop.Base", "class"), N("Shop.IRepo", "interface") },
                new[] { Edge("Shop.Order", "Shop.Base", "inherits"), Edge("Shop.Order", "Shop.IRepo", "implements") });

            Assert.Equal(3, s.TypeCount);       // two classes + one interface
            Assert.Equal(2, s.RelationCount);   // one generalization + one realization

            s.SetHideInterfaces(true);
            Assert.Equal(2, s.TypeCount);       // the interface is gone
            Assert.Equal(1, s.RelationCount);   // its realization is gone; the generalization stays

            s.SetHideInterfaces(false);
            Assert.Equal(3, s.TypeCount);
        });
    }

    [Fact]
    public void ShowGraph_WithAMembersSource_DispatchesAMemberFillPerDrawnBox()
    {
        OnSta(() =>
        {
            var s = new ClassDiagramSurface
            {
                // deterministic stub standing in for the DescribeAsync round-trip
                MembersSource = id => Task.FromResult(
                    ((IReadOnlyList<string>)new[] { "+ Id : int", "# Save() : void" }, 2)),
            };

            s.ShowGraph(
                new[] { N("A", "class"), N("B", "class"), N("C", "class") },
                new[] { Edge("A", "B", "inherits") });

            Assert.True(s.ShowingDiagram);
            Assert.Equal(3, s.DrawnBoxCount);
            // Every drawn box asks its source to fill the UML member compartment.
            Assert.Equal(3, s.MembersRequestedCount);
        });
    }

    [Fact]
    public void ShowGraph_WithoutAMembersSource_StillRendersBoxes_AndRequestsNoFills()
    {
        OnSta(() =>
        {
            var s = new ClassDiagramSurface();   // no MembersSource wired
            s.ShowGraph(new[] { N("A", "class"), N("B", "class") }, new[] { Edge("A", "B", "inherits") });

            Assert.Equal(2, s.DrawnBoxCount);       // the diagram is still valid without members
            Assert.Equal(0, s.MembersRequestedCount);
        });
    }

    [Fact]
    public void ShowGraph_SizesEachBoxToItsMembers_SoMemberRichTypesAreTaller()
    {
        OnSta(() =>
        {
            // "A" declares many members; "B" declares none. A completed-task source with no
            // SynchronizationContext resolves synchronously, so the boxes are sized by the time ShowGraph returns.
            var many = Enumerable.Range(0, 8).Select(i => $"+ Field{i} : int").ToArray();
            var s = new ClassDiagramSurface
            {
                MembersSource = id => Task.FromResult(
                    id == "A"
                        ? ((IReadOnlyList<string>)many, many.Length)
                        : ((IReadOnlyList<string>)System.Array.Empty<string>(), 0)),
            };

            s.ShowGraph(
                new[] { N("A", "class"), N("B", "class") },
                new[] { Edge("A", "B", "inherits") });

            var heights = s.DrawnBoxHeights;
            Assert.Equal(2, heights.Count);
            // Variable height: the member-rich box is meaningfully taller than the member-less one.
            Assert.True(heights.Max() > heights.Min() + 40,
                $"expected a member-rich box to be taller; heights were [{string.Join(", ", heights)}]");
        });
    }

    [Fact]
    public void ShowGraph_WithMembersSource_DoesNotDuplicateTheTruncationNote_AcrossThePrefetchRerender()
    {
        OnSta(() =>
        {
            // >40 types triggers the "Showing N most-connected of M" note. A members source makes the
            // surface prefetch then re-render; the note must appear ONCE, not once per render.
            var nodes = Enumerable.Range(0, 45).Select(i => N($"T{i}", "class")).ToArray();
            var s = new ClassDiagramSurface
            {
                MembersSource = _ => Task.FromResult(
                    ((IReadOnlyList<string>)new[] { "+ X : int" }, 1)),
            };

            s.ShowGraph(nodes, System.Array.Empty<CanvasEdge>());

            var occurrences = s.DisclosureText.Split("most-connected").Length - 1;
            Assert.Equal(1, occurrences);
        });
    }

    [Fact]
    public void ShowGraph_DoesNotClaimMembersAreUnextracted()
    {
        OnSta(() =>
        {
            var s = new ClassDiagramSurface();
            s.ShowGraph(new[] { N("A", "class"), N("B", "class") }, new[] { Edge("A", "B", "inherits") });
            // The stale ADR-0020 Phase 1 note ("Members are not extracted yet") is gone — members are extracted.
            Assert.DoesNotContain("not extracted", s.DisclosureText, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void ShowDependencies_DrawsDashedDependencyArrows_OnlyWhenToggledOn()
    {
        OnSta(() =>
        {
            var s = new ClassDiagramSurface();
            s.ShowGraph(
                new[] { N("A", "class"), N("B", "class"), N("C", "class") },
                new[] { Edge("A", "B", "inherits"), Edge("A", "C", "depends_on") });

            Assert.True(s.ShowingDiagram);
            Assert.Equal(0, s.DrawnDependencyCount);   // dependencies are off by default (too dense)

            s.SetShowDependencies(true);
            Assert.Equal(1, s.DrawnDependencyCount);   // the A -> C dependency is now drawn

            s.SetShowDependencies(false);
            Assert.Equal(0, s.DrawnDependencyCount);
        });
    }

    [Fact]
    public void ShowGraph_DrawsAssociations_WhenAFieldIsTypedAsAnotherDrawnClass()
    {
        OnSta(() =>
        {
            var s = new ClassDiagramSurface
            {
                // A has a single-typed field (association) and a collection field (aggregation) to B.
                MembersSource = id => Task.FromResult(id == "A"
                    ? ((IReadOnlyList<string>)new[] { "+ Ref : B", "+ Items : List<B>" }, 2)
                    : ((IReadOnlyList<string>)Array.Empty<string>(), 0)),
            };

            s.ShowGraph(
                new[] { N("A", "class"), N("B", "class") },
                Array.Empty<CanvasEdge>());

            Assert.True(s.ShowingDiagram);
            // With completed-task members the prefetch re-render runs synchronously, so the derived
            // association + aggregation to B are drawn (2 connectors), even with no inheritance edges.
            Assert.True(s.DrawnAssociationCount >= 1,
                $"expected derived associations, drew {s.DrawnAssociationCount}");
        });
    }
}
