using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ShapePath = System.Windows.Shapes.Path;
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
                Assert.DoesNotContain(
                    Flatten((IEnumerable<SolutionTreeNodeItem>)surface.Tree.ItemsSource!),
                    n => n.Node.Path is "bin" or "bin/");

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

                var srcFolder = FindItem(surface.Tree, n => n.Node.Path == "src");
                Assert.NotNull(srcFolder);
                var folderGlyph = FindKindGlyph(srcFolder!);
                var fileGlyph = FindKindGlyph(file!);
                var unindexedGlyph = FindKindGlyph(unindexed);
                Assert.NotNull(folderGlyph);
                Assert.NotNull(fileGlyph);
                Assert.NotNull(unindexedGlyph);
                Assert.Same(SolutionTreeGlyphs.Folder, folderGlyph!.Data);
                Assert.Same(SolutionTreeGlyphs.File, fileGlyph!.Data);
                Assert.Same(SolutionTreeGlyphs.Folder, unindexedGlyph!.Data);
                Assert.True(
                    unindexedGlyph.StrokeDashArray is { Count: > 0 },
                    "unindexed kind glyph must be dashed");
                Assert.True(
                    folderGlyph.StrokeDashArray is null or { Count: 0 },
                    "indexed-parent folder glyph must be solid");
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Show_IoDto_ChromeSaysNotRecorded_NoIoProbeRow_UnindexedProbeRemainsLeaf()
    {
        OnSta(() =>
        {
            var dto = StarDto() with
            {
                Disclosures =
                [
                    new SolutionTreeDisclosure(
                        SolutionTreeShortfallCause.Io, "Not recorded", null, "io_probe"),
                ],
            };
            var (surface, window) = Mount(dto);
            try
            {
                var text = VisibleText(surface);
                Assert.Contains("Not recorded", text, StringComparison.Ordinal);
                Assert.DoesNotContain("io_probe", text, StringComparison.Ordinal);
                var unindexed = FindItem(surface.Tree, n => n.Node.Path == "unindexed_probe");
                Assert.NotNull(unindexed);
                Assert.False(unindexed!.HasItems);
                Assert.Contains("Unindexed", VisibleText(unindexed), StringComparison.Ordinal);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Show_PermissionDto_ChromeSaysNotRecorded_NoOmitProbeRow_UnindexedProbeRemainsLeaf()
    {
        OnSta(() =>
        {
            var dto = StarDto() with
            {
                Disclosures =
                [
                    new SolutionTreeDisclosure(
                        SolutionTreeShortfallCause.Permission, "Not recorded", null, "omit_probe"),
                ],
            };
            var (surface, window) = Mount(dto);
            try
            {
                var text = VisibleText(surface);
                Assert.Contains("Not recorded", text, StringComparison.Ordinal);
                Assert.DoesNotContain("omit_probe", text, StringComparison.Ordinal);
                Assert.Contains("1 skip-listed directories omitted", text, StringComparison.Ordinal);
                var unindexed = FindItem(surface.Tree, n => n.Node.Path == "unindexed_probe");
                Assert.NotNull(unindexed);
                Assert.False(unindexed!.HasItems);
                Assert.Contains("Unindexed", VisibleText(unindexed), StringComparison.Ordinal);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void NodeItem_CarriesEverySolutionTreeNodeField_SourceRevisionStaysOnTheResult()
    {
        var node = File("src/Program.cs", "Program", "class");
        var item = new SolutionTreeNodeItem(node);
        Assert.Same(node, item.Node);
        Assert.Equal(node.Path, item.Node.Path);
        Assert.Equal(node.Kind, item.Node.Kind);
        Assert.Equal(node.Coverage, item.Node.Coverage);
        Assert.Equal(node.NodeId, item.Node.NodeId);
        Assert.Equal(node.NodeKind, item.Node.NodeKind);
        var fields = typeof(SolutionTreeNode).GetProperties()
            .Select(p => p.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Coverage", "Kind", "NodeId", "NodeKind", "Path"], fields);
        Assert.Equal("rev-star", StarDto().SourceRevision);
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

                seen = null;
                var fileDbl = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                };
                typeof(MouseButtonEventArgs).GetProperty("ClickCount")!.SetValue(fileDbl, 2);
                file.RaiseEvent(fileDbl);
                Assert.NotNull(seen);
                Assert.Equal(NodeViewKind.Source, seen!.Kind);
                Assert.Equal("Program", seen.NodeId);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void FileArtifact_NodeMenu_IsViewSourceAndReveal_UnindexedHasNone()
    {
        OnSta(() =>
        {
            var (surface, window) = Mount(StarDto());
            try
            {
                var fileVm = Flatten(_rootsOf(surface)).Single(n => n.Node.Path == "src/Program.cs");
                var menu = surface.BuildFileMenu(fileVm);
                Assert.NotNull(menu);
                var headers = menu!.Items.OfType<MenuItem>().Select(i => (string)i.Header).ToList();
                Assert.Equal(["View source", "Reveal in graph"], headers);

                SolutionTreeActivate? seen = null;
                surface.ActivateRequested += (_, a) => seen = a;
                ((MenuItem)menu.Items[1]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Assert.Equal(NodeViewKind.GraphNeighbourhood, seen!.Kind);

                var unindexedVm = Flatten(_rootsOf(surface)).Single(n => n.Node.Path == "unindexed_probe");
                Assert.Null(surface.BuildFileMenu(unindexedVm));
            }
            finally
            {
                window.Close();
            }
        });

        static IReadOnlyList<SolutionTreeNodeItem> _rootsOf(SolutionTreeSurface surface) =>
            ((IEnumerable<SolutionTreeNodeItem>?)surface.Tree.ItemsSource)?.ToList()
            ?? [];
    }

    [Fact]
    public void ShowActivateError_ViewSourceAndReveal_RetryReraises_SelectionUnchanged()
    {
        OnSta(() =>
        {
            var (surface, window) = Mount(StarDto());
            try
            {
                var file = FindItem(surface.Tree, n => n.Node.Path == "src/Program.cs");
                Assert.NotNull(file);
                file!.IsSelected = true;
                surface.HandleKey(Key.Return, ModifierKeys.None);

                SolutionTreeActivate? seen = null;
                surface.ActivateRequested += (_, a) => seen = a;
                surface.ShowActivateError(NodeViewKind.Source);
                Assert.Contains("Could not open source.", VisibleText(surface), StringComparison.Ordinal);
                Assert.True(file.IsSelected);

                ClickNamed(surface, "Retry");
                Assert.NotNull(seen);
                Assert.Equal(NodeViewKind.Source, seen!.Kind);
                Assert.Equal("Program", seen.NodeId);
                Assert.True(file.IsSelected);

                seen = null;
                surface.HandleKey(Key.Return, ModifierKeys.Control);
                surface.ShowActivateError(NodeViewKind.GraphNeighbourhood);
                Assert.Contains("Could not reveal in graph.", VisibleText(surface), StringComparison.Ordinal);
                ClickNamed(surface, "Retry");
                Assert.Equal(NodeViewKind.GraphNeighbourhood, seen!.Kind);
                Assert.True(file.IsSelected);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Empty_ShowGraph_RaisesShowGraphRequested()
    {
        OnSta(() =>
        {
            var surface = new SolutionTreeSurface();
            var raised = false;
            surface.ShowGraphRequested += (_, _) => raised = true;
            surface.Show(RootOnlyDto());
            ClickNamed(surface, "Show Graph");
            Assert.True(raised);
        });
    }

    [Fact]
    public void MarkStale_ChromeCarriesStaleWordAndGlyph()
    {
        OnSta(() =>
        {
            var (surface, window) = Mount(StarDto());
            try
            {
                surface.MarkStale();
                Assert.Contains("Stale", VisibleText(surface), StringComparison.Ordinal);
                var staleGlyph = FindNamedPath(surface, "staleGlyph");
                Assert.NotNull(staleGlyph?.Data);
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
    public void ShowGraph_OpensExistingCanvasKind_OnArchitecture()
    {
        var queries = new RecordingTreeQueries { Result = RootOnlyDto() };

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
                var id = shell.Architecture.Service.Zones.AllSurfaces()
                    .Single(s => s.Kind == "solution-tree").SurfaceId;
                var surface = shell.Architecture.Adapter.SurfaceContent<SolutionTreeSurface>(id);
                Assert.NotNull(surface);
                ClickNamed(surface!, "Show Graph");
                Assert.Contains(
                    shell.Architecture.Service.Zones.AllSurfaces(),
                    s => s.Kind == "canvas");
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SwitchingToExplore_LeavesExplorerAsGraphAndReader()
    {
        var queries = new RecordingTreeQueries { Result = StarDto() };

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
            var mode = new PerspectiveShell(host, shell.Hosts, () => new Grid { Name = "ExploreBody" }, shell.Announcer);
            shell.CommandRouter = mode.Execute;
            shell.DocumentOpening += h => mode.OnDocumentOpening(h);
            window.Show();

            try
            {
                mode.Activate(AiDe.Core.Workbench.PerspectiveSet.Architecture, "test");
                Assert.True(shell.Execute("surface.show.solution-tree"));
                mode.Activate(AiDe.Core.Workbench.PerspectiveSet.Explore, "test");
                Assert.IsType<Grid>(mode.ExplorerSurface);
                Assert.Equal("ExploreBody", ((Grid)mode.ExplorerSurface!).Name);
                Assert.IsNotType<SolutionTreeSurface>(mode.ExplorerSurface);
                Assert.Same(
                    AiDe.Core.Workbench.PerspectiveSet.Architecture,
                    Assert.Single(SurfaceContentFactory.Kinds.Single(k => k.Kind == "solution-tree").Perspectives));
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ViewSourceFault_ShowsCouldNotOpenSource_RetryReinvokes_SelectionUnchanged()
    {
        var queries = new RecordingTreeQueries
        {
            Result = StarDto(),
            ContentFault = new InvalidOperationException("denied"),
        };

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
                var id = shell.Architecture.Service.Zones.AllSurfaces()
                    .Single(s => s.Kind == "solution-tree").SurfaceId;
                var surface = shell.Architecture.Adapter.SurfaceContent<SolutionTreeSurface>(id);
                Assert.NotNull(surface);
                surface!.Measure(new Size(480, 360));
                surface.Arrange(new Rect(0, 0, 480, 360));
                surface.UpdateLayout();
                ExpandIndexed(surface.Tree);
                window.UpdateLayout();

                var file = FindItem(surface.Tree, n => n.Node.Path == "src/Program.cs");
                Assert.True(file is not null, "file-artifact container was not generated");
                file!.IsSelected = true;
                surface.HandleKey(Key.Return, ModifierKeys.None);
                PumpUntil(() => VisibleText(surface).Contains("Could not open source.", StringComparison.Ordinal));

                Assert.Contains("Could not open source.", VisibleText(surface), StringComparison.Ordinal);
                Assert.True(file.IsSelected);
                Assert.True(queries.ContentCalls >= 1);
                var calls = queries.ContentCalls;
                ClickNamed(surface, "Retry");
                PumpUntil(() => queries.ContentCalls > calls);
                Assert.True(queries.ContentCalls > calls);
                Assert.True(file.IsSelected);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void OverlappingPopulate_DropsTheOlderResult()
    {
        var hold = new TaskCompletionSource<SolutionTreeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var queries = new RecordingTreeQueries
        {
            HoldFirst = hold,
            FastResult = StarDto() with { SkipListedDirectoriesOmitted = 7 },
        };

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
                var id = shell.Architecture.Service.Zones.AllSurfaces()
                    .Single(s => s.Kind == "solution-tree").SurfaceId;
                var surface = shell.Architecture.Adapter.SurfaceContent<SolutionTreeSurface>(id);
                Assert.NotNull(surface);
                PumpUntil(() => queries.TreeCalls >= 1, timeoutMs: 2000);

                shell.RetrySolutionTreePopulate(surface!);
                PumpUntil(() => queries.TreeCalls >= 2, timeoutMs: 2000);
                PumpUntil(
                    () => VisibleText(surface!).Contains("7 skip-listed directories omitted", StringComparison.Ordinal),
                    timeoutMs: 2000);

                Assert.True(hold.TrySetResult(StarDto() with { SkipListedDirectoriesOmitted = 1 }));
                var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                for (var i = 0; i < 15; i++)
                {
                    dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                    Thread.Sleep(20);
                }

                Assert.Contains(
                    "7 skip-listed directories omitted",
                    VisibleText(surface!),
                    StringComparison.Ordinal);
                Assert.DoesNotContain(
                    "1 skip-listed directories omitted",
                    VisibleText(surface!),
                    StringComparison.Ordinal);
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

        public int ContentCalls { get; private set; }

        public Exception? ContentFault { get; set; }

        public TaskCompletionSource<SolutionTreeResult>? HoldFirst { get; set; }

        public SolutionTreeResult? FastResult { get; set; }

        public SolutionTreeQuery? LastQuery { get; private set; }

        public override Task<AiDe.Core.Projections.NodeContent> NodeContentAsync(
            string nodeId, CancellationToken cancellationToken)
        {
            ContentCalls++;
            if (ContentFault is not null)
            {
                return Task.FromException<AiDe.Core.Projections.NodeContent>(ContentFault);
            }

            return base.NodeContentAsync(nodeId, cancellationToken);
        }

        public override Task<SolutionTreeResult> SolutionTreeAsync(
            SolutionTreeQuery query, CancellationToken cancellationToken)
        {
            TreeCalls++;
            LastQuery = query;
            if (TreeCalls == 1 && HoldFirst is not null)
            {
                return HoldFirst.Task;
            }

            return Task.FromResult(FastResult ?? Result ?? OmitSetDto());
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

    private static void PumpUntil(Func<bool> settled, int timeoutMs = 2000)
    {
        var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            if (settled())
            {
                return;
            }

            Thread.Sleep(20);
        }

        Assert.Fail("timed out waiting for the UI to settle");
    }

    private static void ClickNamed(DependencyObject root, string name)
    {
        foreach (var button in LogicalButtons(root))
        {
            if (button.Visibility != Visibility.Visible || !IsShown(button))
            {
                continue;
            }

            if (string.Equals(AutomationProperties.GetName(button), name, StringComparison.Ordinal)
                || (button.Content as string) == name)
            {
                if (button.Command is { } command && command.CanExecute(null))
                {
                    command.Execute(null);
                    return;
                }

                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return;
            }
        }

        Assert.Fail($"no visible button named {name}");
    }

    private static bool IsShown(DependencyObject node)
    {
        for (var d = node; d is not null; d = LogicalTreeHelper.GetParent(d))
        {
            if (d is UIElement { Visibility: not Visibility.Visible })
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<Button> LogicalButtons(DependencyObject root)
    {
        if (root is Button button)
        {
            yield return button;
        }

        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            foreach (var nested in LogicalButtons(child))
            {
                yield return nested;
            }
        }
    }

    private static ShapePath? FindKindGlyph(TreeViewItem item)
    {
        item.ApplyTemplate();
        item.UpdateLayout();
        return FindNamedPath(item, "kindGlyph") ?? FirstPath(item, skipExpander: true);
    }

    private static ShapePath? FindNamedPath(DependencyObject root, string name)
    {
        if (root is FrameworkElement fe && fe.Name == name && root is ShapePath named)
        {
            return named;
        }

        var n = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < n; i++)
        {
            var found = FindNamedPath(VisualTreeHelper.GetChild(root, i), name);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static ShapePath? FirstPath(DependencyObject root, bool skipExpander = false)
    {
        if (skipExpander && root is ToggleButton)
        {
            return null;
        }

        if (root is ShapePath path && path.Width >= 15)
        {
            return path;
        }

        var n = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < n; i++)
        {
            var found = FirstPath(VisualTreeHelper.GetChild(root, i), skipExpander);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
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
