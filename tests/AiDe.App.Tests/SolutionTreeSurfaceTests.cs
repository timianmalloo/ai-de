using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AiDe.App.Workbench;
using AiDe.Core.Projections;
using AiDe.Testing;

namespace AiDe.App.Tests;

/// <summary>
/// UV-1 Solution tree: visual-tree oracles over a Fake/recording DTO (not Core omit ctor, not IPC).
/// </summary>
public sealed class SolutionTreeSurfaceTests
{
    private static void OnSta(Action work) => Sta.Run(work, 60);

    private static T OnSta<T>(Func<T> work) => Sta.Run(work, 60);

    private static SolutionTreeNode Folder(
        string path, CensusFolderCoverage coverage) =>
        new(path, SolutionTreeNodeKind.CensusFolder, coverage, null, null);

    private static SolutionTreeNode File(
        string path, string nodeId = "Program", string? nodeKind = "class") =>
        new(path, SolutionTreeNodeKind.FileArtifact, null, nodeId, nodeKind);

    /// <summary>F* happy DTO: indexed src + file, unindexed_probe leaf, skip-count, no bin.</summary>
    private static SolutionTreeResult StarDto() => new(
        [
            Folder("", CensusFolderCoverage.IndexedParent),
            Folder("src", CensusFolderCoverage.IndexedParent),
            File("src/Program.cs"),
            Folder("unindexed_probe", CensusFolderCoverage.Unindexed),
        ],
        SkipListedDirectoriesOmitted: 1,
        OmittedByCap: 0,
        [],
        "rev-star");

    /// <summary>
    /// Core omit-set DTO for US-T5c visual-tree. omit_probe paths absent; unindexed_probe remains.
    /// App.Tests must not construct SolutionTreeProjection.
    /// </summary>
    private static SolutionTreeResult OmitSetDto() => new(
        [
            Folder("", CensusFolderCoverage.IndexedParent),
            Folder("src", CensusFolderCoverage.IndexedParent),
            File("src/Program.cs"),
            Folder("unindexed_probe", CensusFolderCoverage.Unindexed),
        ],
        SkipListedDirectoriesOmitted: 1,
        OmittedByCap: 2,
        [new SolutionTreeDisclosure(SolutionTreeShortfallCause.Cap, "Omitted (2)", 2, null)],
        "rev-omit");

    private static SolutionTreeResult RootOnlyDto() => new(
        [Folder("", CensusFolderCoverage.Unindexed)],
        SkipListedDirectoriesOmitted: 0,
        OmittedByCap: 0,
        [],
        "rev-empty");

    private static (SolutionTreeSurface Surface, Window Window) Mount(SolutionTreeResult dto)
    {
        var surface = new SolutionTreeSurface();
        surface.Show(dto);
        var window = new Window
        {
            Content = surface,
            Width = 480,
            Height = 360,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -10000,
            Top = -10000,
            ShowInTaskbar = false,
            ShowActivated = false,
        };
        window.Show();
        window.UpdateLayout();
        ExpandIndexed(surface.Tree);
        window.UpdateLayout();
        return (surface, window);
    }

    [Fact]
    public void Nest_HappyForest_DropsOrphanFile_DoesNotInventFolder()
    {
        var happy = SolutionTreeNodeItem.Nest(StarDto().Nodes);
        Assert.Equal("", Assert.Single(happy).Node.Path);
        var names = happy[0].Children.Select(c => c.Node.Path).ToList();
        Assert.Contains("src", names);
        Assert.Contains("unindexed_probe", names);
        var src = happy[0].Children.Single(c => c.Node.Path == "src");
        Assert.Equal("src/Program.cs", Assert.Single(src.Children).Node.Path);
        Assert.Empty(happy[0].Children.Single(c => c.Node.Path == "unindexed_probe").Children);

        var orphanFile = SolutionTreeNodeItem.Nest(
        [
            Folder("", CensusFolderCoverage.IndexedParent),
            Folder("src", CensusFolderCoverage.IndexedParent),
            File("src/App/Program.cs"),
        ]);
        Assert.Empty(orphanFile[0].Children.Single(c => c.Node.Path == "src").Children);
        Assert.DoesNotContain(Flatten(orphanFile), n => n.Node.Path == "src/App");

        var forest = SolutionTreeNodeItem.Nest(
        [
            Folder("", CensusFolderCoverage.IndexedParent),
            Folder("deep/nested", CensusFolderCoverage.Unindexed),
        ]);
        Assert.Contains(forest, n => n.Node.Path == "deep/nested");
        Assert.DoesNotContain(Flatten(forest), n => n.Node.Path == "deep");
    }

    [Fact]
    public void Show_StarDto_UnindexedProbeIsLeaf_WithKindAndCoverageInName_AndSkipChrome_NoBin()
    {
        OnSta(() =>
        {
            var (surface, window) = Mount(StarDto());
            try
            {
                var text = VisibleText(surface);
                Assert.Contains("unindexed_probe", text, StringComparison.Ordinal);
                Assert.Contains("Unindexed", text, StringComparison.Ordinal);
                Assert.Contains("1 skip-listed directories omitted", text, StringComparison.Ordinal);
                Assert.DoesNotContain("bin", text, StringComparison.Ordinal);

                var unindexed = FindItem(surface.Tree, n => n.Node.Path == "unindexed_probe");
                Assert.NotNull(unindexed);
                Assert.False(unindexed!.HasItems);
                Assert.Empty(Assert.IsType<SolutionTreeNodeItem>(unindexed.DataContext).Children);
                var peer = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(unindexed);
                Assert.Contains("census-folder", peer.GetName(), StringComparison.Ordinal);
                Assert.Contains("Unindexed", peer.GetName(), StringComparison.Ordinal);
                Assert.Contains("unindexed_probe", peer.GetName(), StringComparison.Ordinal);

                var file = FindItem(surface.Tree, n => n.Node.Path == "src/Program.cs");
                Assert.NotNull(file);
                var filePeer = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(file!);
                Assert.Contains("file-artifact", filePeer.GetName(), StringComparison.Ordinal);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Show_OmitSetDto_ChromeSaysOmittedN_NoOmitProbeRow_UnindexedProbeRemainsLeaf()
    {
        OnSta(() =>
        {
            var (surface, window) = Mount(OmitSetDto());
            try
            {
                var text = VisibleText(surface);
                Assert.Contains("Omitted (2)", text, StringComparison.Ordinal);
                Assert.DoesNotContain("omit_probe", text, StringComparison.Ordinal);
                Assert.Contains("unindexed_probe", text, StringComparison.Ordinal);
                Assert.Contains("Unindexed", text, StringComparison.Ordinal);

                var unindexed = FindItem(surface.Tree, n => n.Node.Path == "unindexed_probe");
                Assert.NotNull(unindexed);
                Assert.False(unindexed!.HasItems);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Show_RootOnlyDto_EmptyCopyAndShowGraph_NotAnUnindexedRootRow()
    {
        OnSta(() =>
        {
            var surface = new SolutionTreeSurface();
            surface.Show(RootOnlyDto());
            var text = VisibleText(surface);
            Assert.Contains("No indexed artifacts or folders to show.", text, StringComparison.Ordinal);
            Assert.Contains("Show Graph", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Unindexed", text, StringComparison.Ordinal);
            Assert.True(surface.Tree.Visibility != Visibility.Visible || surface.Tree.Items.Count == 0);
        });
    }

    [Fact]
    public void HardStates_LoadingErrorNoWorkspace_AreDistinct()
    {
        OnSta(() =>
        {
            var surface = new SolutionTreeSurface();
            surface.ShowLoading();
            Assert.Contains("Reading the workspace tree…", VisibleText(surface), StringComparison.Ordinal);

            surface.ShowError("daemon closed");
            var error = VisibleText(surface);
            Assert.Contains("Could not read the workspace tree.", error, StringComparison.Ordinal);
            Assert.Contains("Retry", error, StringComparison.Ordinal);
            Assert.DoesNotContain("Reading the workspace tree…", error, StringComparison.Ordinal);

            surface.ShowNoWorkspace();
            var nows = VisibleText(surface);
            Assert.Contains("Open a workspace to see its solution tree.", nows, StringComparison.Ordinal);
            Assert.DoesNotContain("Could not read the workspace tree.", nows, StringComparison.Ordinal);
            Assert.DoesNotContain("No indexed artifacts or folders to show.", nows, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Show_PythonTsCopy_IsExact()
    {
        OnSta(() =>
        {
            var dto = StarDto() with
            {
                Disclosures =
                [
                    new SolutionTreeDisclosure(
                        SolutionTreeShortfallCause.PythonTsPerFile,
                        "Python and TypeScript files are not listed individually. The scope folder is indexed.",
                        null,
                        null),
                ],
            };
            var surface = new SolutionTreeSurface();
            surface.Show(dto);
            Assert.Contains(
                "Python and TypeScript files are not listed individually. The scope folder is indexed.",
                VisibleText(surface),
                StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Header_MinHeightIs28_NotHeightOnTheItem()
    {
        OnSta(() =>
        {
            var (surface, window) = Mount(StarDto());
            try
            {
                var file = FindItem(surface.Tree, n => n.Node.Path == "src/Program.cs");
                Assert.NotNull(file);
                file!.ApplyTemplate();
                var header = file.Template?.FindName("PART_Header", file) as FrameworkElement;
                Assert.NotNull(header);
                Assert.True(header!.ActualHeight >= 27.4, $"header {header.ActualHeight}");
                Assert.True(header.ActualWidth >= 24);
                Assert.NotEqual(28, file.Height);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Tree_OptInRecyclingVirtualization()
    {
        OnSta(() =>
        {
            var surface = new SolutionTreeSurface();
            Assert.True(VirtualizingPanel.GetIsVirtualizing(surface.Tree));
            Assert.Equal(VirtualizationMode.Recycling, VirtualizingPanel.GetVirtualizationMode(surface.Tree));
        });
    }

    [Fact]
    public void EnterOnFileArtifact_RequestsViewSource_UnindexedDoubleClickDoesNotExpand()
    {
        OnSta(() =>
        {
            var (surface, window) = Mount(StarDto());
            try
            {
                SolutionTreeActivate? seen = null;
                surface.ActivateRequested += (_, a) => seen = a;

                var file = FindItem(surface.Tree, n => n.Node.Path == "src/Program.cs");
                Assert.NotNull(file);
                file!.IsSelected = true;
                surface.HandleKey(Key.Return, ModifierKeys.None);
                Assert.NotNull(seen);
                Assert.Equal(NodeViewKind.Source, seen!.Kind);
                Assert.Equal("Program", seen.NodeId);

                seen = null;
                surface.HandleKey(Key.Return, ModifierKeys.Control);
                Assert.Equal(NodeViewKind.GraphNeighbourhood, seen!.Kind);

                var unindexed = FindItem(surface.Tree, n => n.Node.Path == "unindexed_probe");
                Assert.NotNull(unindexed);
                unindexed!.IsSelected = true;
                var dbl = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                };
                typeof(MouseButtonEventArgs).GetProperty("ClickCount")!.SetValue(dbl, 2);
                unindexed.RaiseEvent(dbl);
                Assert.False(unindexed.IsExpanded);
                Assert.False(unindexed.HasItems);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Binder_CallsSolutionTreeAsync_NotGraphAsync_AndShowsOmitSetDto()
    {
        var queries = new RecordingTreeQueries { Result = OmitSetDto() };

        OnSta(() =>
        {
            using var shell = new WorkbenchShell(queries);
            var window = new Window
            {
                Content = new ContentControl(),
                Width = 1000,
                Height = 640,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                ShowActivated = false,
            };
            var host = (ContentControl)window.Content;
            var mode = new PerspectiveShell(host, shell.Hosts, () => new Grid(), shell.Announcer);
            shell.CommandRouter = mode.Execute;
            shell.DocumentOpening += h => mode.OnDocumentOpening(h);
            window.Show();

            try
            {
                mode.Activate(AiDe.Core.Workbench.PerspectiveSet.Architecture, "test");
                Assert.True(shell.Execute("surface.show.solution-tree"));
                Assert.Equal(1, queries.TreeCalls);
                Assert.NotNull(queries.LastQuery);
                Assert.Null(typeof(SolutionTreeQuery).GetProperty("DropRelativePaths"));

                var id = shell.Architecture.Service.Zones.AllSurfaces()
                    .Single(s => s.Kind == "solution-tree").SurfaceId;
                var surface = shell.Architecture.Adapter.SurfaceContent<SolutionTreeSurface>(id);
                Assert.NotNull(surface);
                var text = VisibleText(surface!);
                Assert.Contains("Omitted (2)", text, StringComparison.Ordinal);
                Assert.DoesNotContain("omit_probe", text, StringComparison.Ordinal);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void FactoryRow_IsArchitectureOne_DerivedView()
    {
        var row = Assert.Single(SurfaceContentFactory.Kinds, k => k.Kind == "solution-tree");
        Assert.Equal("Solution tree", row.Title);
        Assert.Same(AiDe.Core.Workbench.PerspectiveSet.Architecture, Assert.Single(row.Perspectives));
        Assert.Equal(SurfaceContentFactory.Instances.One, row.Instances);
        var entry = Assert.IsType<SurfaceContentFactory.SurfaceEntry.Derived>(row.Entry);
        Assert.Equal("_View", entry.Menu);
        Assert.False(row.Windowed);
        Assert.Null(row.Zone);
    }

    private sealed class RecordingTreeQueries : FakeWorkspaceQueries
    {
        public SolutionTreeResult? Result { get; set; }

        public int TreeCalls { get; private set; }

        public int GraphCalls { get; private set; }

        public SolutionTreeQuery? LastQuery { get; private set; }

        public override Task<SolutionTreeResult> SolutionTreeAsync(
            SolutionTreeQuery query, CancellationToken cancellationToken)
        {
            TreeCalls++;
            LastQuery = query;
            return Task.FromResult(Result ?? OmitSetDto());
        }

        public override Task<WorkspaceGraph> GraphAsync(
            GraphQuery query, CancellationToken cancellationToken)
        {
            GraphCalls++;
            return Task.FromResult(new WorkspaceGraph([], [], 0, [], "rev-1"));
        }
    }

    private static IEnumerable<SolutionTreeNodeItem> Flatten(IEnumerable<SolutionTreeNodeItem> roots)
    {
        foreach (var n in roots)
        {
            yield return n;
            foreach (var c in Flatten(n.Children))
            {
                yield return c;
            }
        }
    }

    private static string VisibleText(DependencyObject root)
    {
        var parts = new List<string>();
        WalkLogical(root, parts);
        WalkVisual(root, parts);
        return string.Join("\n", parts.Distinct(StringComparer.Ordinal));

        static void Capture(DependencyObject node, List<string> parts)
        {
            switch (node)
            {
                case TextBlock t when t.Visibility != Visibility.Collapsed && !string.IsNullOrWhiteSpace(t.Text):
                    parts.Add(t.Text);
                    break;
                case Button { Visibility: not Visibility.Collapsed, Content: string s }:
                    parts.Add(s);
                    break;
            }
        }

        static void WalkLogical(DependencyObject node, List<string> parts)
        {
            if (node is UIElement { Visibility: Visibility.Collapsed })
            {
                return;
            }

            Capture(node, parts);
            foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())
            {
                WalkLogical(child, parts);
            }
        }

        static void WalkVisual(DependencyObject node, List<string> parts)
        {
            if (node is UIElement { Visibility: Visibility.Collapsed })
            {
                return;
            }

            Capture(node, parts);
            var n = VisualTreeHelper.GetChildrenCount(node);
            for (var i = 0; i < n; i++)
            {
                WalkVisual(VisualTreeHelper.GetChild(node, i), parts);
            }
        }
    }

    private static TreeViewItem? FindItem(TreeView tree, Func<SolutionTreeNodeItem, bool> match)
    {
        return FindIn(tree, match);

        static TreeViewItem? FindIn(ItemsControl parent, Func<SolutionTreeNodeItem, bool> match)
        {
            parent.ApplyTemplate();
            parent.UpdateLayout();
            for (var i = 0; i < parent.Items.Count; i++)
            {
                if (parent.ItemContainerGenerator.ContainerFromIndex(i) is not TreeViewItem item)
                {
                    continue;
                }

                if (item.DataContext is SolutionTreeNodeItem vm && match(vm))
                {
                    return item;
                }

                item.IsExpanded = true;
                item.UpdateLayout();
                var nested = FindIn(item, match);
                if (nested is not null)
                {
                    return nested;
                }
            }

            return null;
        }
    }

    private static void ExpandIndexed(TreeView tree)
    {
        Expand(tree);

        static void Expand(ItemsControl parent)
        {
            parent.ApplyTemplate();
            parent.UpdateLayout();
            for (var i = 0; i < parent.Items.Count; i++)
            {
                if (parent.ItemContainerGenerator.ContainerFromIndex(i) is not TreeViewItem item)
                {
                    continue;
                }

                if (item.DataContext is SolutionTreeNodeItem vm
                    && vm.Node.Kind == SolutionTreeNodeKind.CensusFolder
                    && vm.Node.Coverage == CensusFolderCoverage.IndexedParent
                    && vm.Children.Count > 0)
                {
                    item.IsExpanded = true;
                    item.UpdateLayout();
                    Expand(item);
                }
            }
        }
    }
}
