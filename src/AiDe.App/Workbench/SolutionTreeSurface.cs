using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using AiDe.Core.Projections;

namespace AiDe.App.Workbench;

/// <summary>One activate from the tree, mapped onto an existing <see cref="NodeViewKind"/>.</summary>
public sealed record SolutionTreeActivate(string NodeId, NodeViewKind Kind);

/// <summary>
/// Wraps one <see cref="SolutionTreeNode"/> for the TreeView. Children are nested from the flat
/// DTO; Coverage is not recomputed.
/// </summary>
internal sealed class SolutionTreeNodeItem
{
    public SolutionTreeNodeItem(SolutionTreeNode node)
    {
        Node = node;
        Children = [];
    }

    public SolutionTreeNode Node { get; }

    public ObservableCollection<SolutionTreeNodeItem> Children { get; }

    public bool IsStale { get; set; }

    public string DisplayName
    {
        get
        {
            var path = Node.Path;
            if (string.IsNullOrEmpty(path))
            {
                return ".";
            }

            var slash = path.LastIndexOf('/');
            return slash < 0 ? path : path[(slash + 1)..];
        }
    }

    public string KindWord => Node.Kind == SolutionTreeNodeKind.FileArtifact
        ? "file-artifact"
        : "census-folder";

    public string CoverageLabel => Node.Coverage == CensusFolderCoverage.Unindexed ? "Unindexed" : "";

    public Geometry KindGlyph => Node.Kind == SolutionTreeNodeKind.FileArtifact
        ? SolutionTreeGlyphs.File
        : SolutionTreeGlyphs.Folder;

    public DoubleCollection KindGlyphDash =>
        Node.Kind == SolutionTreeNodeKind.CensusFolder
        && Node.Coverage == CensusFolderCoverage.Unindexed
            ? SolutionTreeGlyphs.Dash
            : SolutionTreeGlyphs.Solid;

    public string AccessibleName
    {
        get
        {
            var coverage = Node.Coverage switch
            {
                CensusFolderCoverage.Unindexed => " Unindexed",
                CensusFolderCoverage.IndexedParent => " indexed-parent",
                _ => "",
            };
            var stale = IsStale ? " Stale" : "";
            return $"{DisplayName} {KindWord}{coverage}{stale}";
        }
    }

    /// <summary>
    /// Nest the flat DTO by parent census-folder already in the list. No path-split.
    /// </summary>
    public static IReadOnlyList<SolutionTreeNodeItem> Nest(IReadOnlyList<SolutionTreeNode> nodes)
    {
        var comparer = PathComparer;
        var folders = new HashSet<string>(comparer);
        var items = new Dictionary<string, SolutionTreeNodeItem>(comparer);
        foreach (var node in nodes)
        {
            var path = Normalize(node.Path);
            if (node.Kind == SolutionTreeNodeKind.CensusFolder)
            {
                folders.Add(path);
            }

            items[path] = new SolutionTreeNodeItem(node with { Path = path });
        }

        var roots = new List<SolutionTreeNodeItem>();
        foreach (var node in nodes)
        {
            var path = Normalize(node.Path);
            var parent = ParentOf(path);
            if (parent is null)
            {
                roots.Add(items[path]);
                continue;
            }

            if (folders.Contains(parent) && items.TryGetValue(parent, out var folder))
            {
                folder.Children.Add(items[path]);
                continue;
            }

            if (node.Kind == SolutionTreeNodeKind.CensusFolder)
            {
                roots.Add(items[path]);
            }
        }

        return roots;
    }

    internal static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    internal static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path) || path == ".")
        {
            return "";
        }

        var s = path.Replace('\\', '/').TrimEnd('/');
        return s == "." ? "" : s;
    }

    internal static string? ParentOf(string relative)
    {
        if (string.IsNullOrEmpty(relative))
        {
            return null;
        }

        var slash = relative.LastIndexOf('/');
        return slash < 0 ? "" : relative[..slash];
    }
}

/// <summary>N8 kind glyphs: mockup 16px stroke paths. Decorative; UIA Name stays on the row.</summary>
internal static class SolutionTreeGlyphs
{
    public static Geometry Folder { get; } = Frozen(Geometry.Parse("M2.5 5h3.2l1.2 1.4H13.5V12.5H2.5z"));

    public static Geometry File { get; } = Frozen(Geometry.Parse("M4.5 2.5h5l2.5 2.5V13.5h-7.5z M9.5 2.5V5h2.5"));

    public static Geometry Stale { get; } = Frozen(Geometry.Parse("M8 2.5a5.5 5.5 0 1 0 0.01 0z M8 5v3.2l2 1.3"));

    public static DoubleCollection Dash { get; } = Frozen(new DoubleCollection { 2, 2 });

    public static DoubleCollection Solid { get; } = Frozen(new DoubleCollection());

    private static Geometry Frozen(Geometry geometry)
    {
        geometry.Freeze();
        return geometry;
    }

    private static DoubleCollection Frozen(DoubleCollection dashes)
    {
        dashes.Freeze();
        return dashes;
    }
}

/// <summary>
/// Architecture Solution tree: WPF TreeView over <see cref="IWorkspaceQueries.SolutionTreeAsync"/>.
/// </summary>
public sealed class SolutionTreeSurface : ContentControl
{
    private readonly StackPanel _chrome = new() { Margin = new Thickness(12, 8, 12, 4) };
    private readonly Grid _body = new();
    private readonly StackPanel _empty;
    private readonly TextBlock _loading;
    private readonly StackPanel _error;
    private readonly TextBlock _noWorkspace;
    private IReadOnlyList<SolutionTreeNodeItem> _roots = [];
    private bool _stale;
    private enum LoadState { Unloaded, NoWorkspace, Loading, Shown, Error }
    private LoadState _state = LoadState.Unloaded;

    public SolutionTreeSurface(string title = "Solution tree")
    {
        AutomationProperties.SetName(this, title);
        SetResourceReference(BackgroundProperty, "SurfaceBrush");

        Tree = BuildTree();
        _empty = Wayfinder(
            "No indexed artifacts or folders to show.",
            Action("Show Graph", () => ShowGraphRequested?.Invoke(this, EventArgs.Empty)));
        _loading = Muted("Reading the workspace tree…");
        _error = Wayfinder(
            "Could not read the workspace tree.",
            Action("Retry", () => RetryRequested?.Invoke(this, EventArgs.Empty)));
        _noWorkspace = Muted("Open a workspace to see its solution tree.");

        _body.Children.Add(Tree);
        _body.Children.Add(_empty);
        _body.Children.Add(_loading);
        _body.Children.Add(_error);
        _body.Children.Add(_noWorkspace);

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(_chrome, Dock.Top);
        root.Children.Add(_chrome);
        root.Children.Add(_body);
        Content = root;

        ShowPane(_noWorkspace);
        _state = LoadState.Unloaded;
    }

    internal TreeView Tree { get; }

    internal bool NeedsInitialBind => _state == LoadState.Unloaded;

    internal bool IsNoWorkspace => _state == LoadState.NoWorkspace;

    internal bool HasLoaded => _state is LoadState.Shown or LoadState.Error;

    public event EventHandler<SolutionTreeActivate>? ActivateRequested;

    public event EventHandler? ShowGraphRequested;

    public event EventHandler? RetryRequested;

    /// <summary>Last file-artifact node menu built for a pointer Reveal / View source.</summary>
    internal ContextMenu? LastNodeMenu { get; private set; }

    /// <summary>
    /// Dual-activate menu only (DESIGN.md: View source · Reveal in graph). Not the full Open-as list.
    /// </summary>
    internal ContextMenu? BuildFileMenu(SolutionTreeNodeItem item)
    {
        if (item.Node.Kind != SolutionTreeNodeKind.FileArtifact
            || string.IsNullOrEmpty(item.Node.NodeId))
        {
            return null;
        }

        var nodeId = item.Node.NodeId;
        var menu = new ContextMenu();
        menu.Items.Add(ActivateItem("View source", NodeViewKind.Source, nodeId));
        menu.Items.Add(ActivateItem("Reveal in graph", NodeViewKind.GraphNeighbourhood, nodeId));
        LastNodeMenu = menu;
        return menu;
    }

    private MenuItem ActivateItem(string label, NodeViewKind kind, string nodeId)
    {
        var item = new MenuItem { Header = label };
        item.Click += (_, _) =>
            ActivateRequested?.Invoke(this, new SolutionTreeActivate(nodeId, kind));
        return item;
    }

    public void ShowLoading()
    {
        _state = LoadState.Loading;
        _stale = false;
        _chrome.Children.Clear();
        ShowPane(_loading);
    }

    public void ShowNoWorkspace()
    {
        _state = LoadState.NoWorkspace;
        _stale = false;
        _chrome.Children.Clear();
        ShowPane(_noWorkspace);
    }

    public void ShowError(string _)
    {
        _state = LoadState.Error;
        if (_roots.Count > 0)
        {
            _stale = false;
            RenderChrome(_lastResult);
            foreach (UIElement child in _body.Children)
            {
                child.Visibility = ReferenceEquals(child, Tree) || ReferenceEquals(child, _error)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            return;
        }

        _chrome.Children.Clear();
        ShowPane(_error);
    }

    public void MarkStale()
    {
        _stale = true;
        foreach (var item in Walk(_roots))
        {
            item.IsStale = true;
        }

        RenderChrome(_lastResult);
    }

    private SolutionTreeResult? _lastResult;

    public void Show(SolutionTreeResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _lastResult = result;
        _stale = false;
        _state = LoadState.Shown;

        var nonRoot = result.Nodes.Where(n => !string.IsNullOrEmpty(n.Path)).ToList();
        if (nonRoot.Count == 0 && result.Disclosures.Count == 0)
        {
            _roots = [];
            Tree.ItemsSource = null;
            RenderChrome(result);
            ShowPane(_empty);
            return;
        }

        _roots = SolutionTreeNodeItem.Nest(result.Nodes);
        Tree.ItemsSource = _roots;
        RenderChrome(result);
        ShowPane(Tree);
    }

    internal void HandleKey(Key key, ModifierKeys modifiers)
    {
        if (key is not (Key.Return or Key.Enter))
        {
            return;
        }

        if (Tree.SelectedItem is not SolutionTreeNodeItem item)
        {
            return;
        }

        if (item.Node.Kind == SolutionTreeNodeKind.FileArtifact
            && !string.IsNullOrEmpty(item.Node.NodeId))
        {
            var kind = (modifiers & ModifierKeys.Control) == ModifierKeys.Control
                ? NodeViewKind.GraphNeighbourhood
                : NodeViewKind.Source;
            ActivateRequested?.Invoke(this, new SolutionTreeActivate(item.Node.NodeId, kind));
            return;
        }

        if (item.Node.Kind == SolutionTreeNodeKind.CensusFolder
            && item.Node.Coverage == CensusFolderCoverage.IndexedParent
            && item.Children.Count > 0
            && (modifiers & ModifierKeys.Control) == 0)
        {
            if (ContainerOf(Tree, item) is { } container)
            {
                container.IsExpanded = !container.IsExpanded;
            }
        }
    }

    private TreeView BuildTree()
    {
        var tree = new TreeView
        {
            BorderThickness = new Thickness(0),
            Background = null,
            Padding = new Thickness(4, 0, 4, 8),
        };
        AutomationProperties.SetName(tree, "Solution tree");
        tree.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
        VirtualizingPanel.SetIsVirtualizing(tree, true);
        VirtualizingPanel.SetVirtualizationMode(tree, VirtualizationMode.Recycling);
        VirtualizingPanel.SetScrollUnit(tree, ScrollUnit.Pixel);

        var header = new FrameworkElementFactory(typeof(Border));
        header.SetValue(FrameworkElement.MinHeightProperty, 28.0);
        header.SetValue(Border.BackgroundProperty, Brushes.Transparent);

        var dock = new FrameworkElementFactory(typeof(DockPanel));
        dock.SetValue(DockPanel.LastChildFillProperty, true);

        var coverage = new FrameworkElementFactory(typeof(TextBlock), "coverage");
        coverage.SetBinding(TextBlock.TextProperty, new Binding(nameof(SolutionTreeNodeItem.CoverageLabel)));
        coverage.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 0, 0, 0));
        coverage.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        coverage.SetValue(DockPanel.DockProperty, Dock.Right);
        coverage.SetResourceReference(TextBlock.ForegroundProperty, "UnverifiedBrush");

        var glyph = new FrameworkElementFactory(typeof(Path), "kindGlyph");
        glyph.SetBinding(Path.DataProperty, new Binding(nameof(SolutionTreeNodeItem.KindGlyph)));
        glyph.SetBinding(Path.StrokeDashArrayProperty, new Binding(nameof(SolutionTreeNodeItem.KindGlyphDash)));
        glyph.SetValue(FrameworkElement.NameProperty, "kindGlyph");
        glyph.SetValue(FrameworkElement.WidthProperty, 16.0);
        glyph.SetValue(FrameworkElement.HeightProperty, 16.0);
        glyph.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
        glyph.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        glyph.SetValue(DockPanel.DockProperty, Dock.Left);
        glyph.SetValue(Path.StretchProperty, Stretch.Uniform);
        glyph.SetValue(Path.StrokeThicknessProperty, 1.5);
        glyph.SetValue(Path.FillProperty, Brushes.Transparent);
        glyph.SetResourceReference(Path.StrokeProperty, "TextBrush");

        var name = new FrameworkElementFactory(typeof(TextBlock));
        name.SetBinding(TextBlock.TextProperty, new Binding(nameof(SolutionTreeNodeItem.DisplayName)));
        name.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        name.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        name.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

        dock.AppendChild(coverage);
        dock.AppendChild(glyph);
        dock.AppendChild(name);
        header.AppendChild(dock);

        var template = new HierarchicalDataTemplate(typeof(SolutionTreeNodeItem))
        {
            ItemsSource = new Binding(nameof(SolutionTreeNodeItem.Children)),
            VisualTree = header,
        };
        var hideCoverage = new DataTrigger
        {
            Binding = new Binding(nameof(SolutionTreeNodeItem.CoverageLabel)),
            Value = "",
        };
        hideCoverage.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed, "coverage"));
        template.Triggers.Add(hideCoverage);
        tree.ItemTemplate = template;

        var itemStyle = new Style(typeof(TreeViewItem));
        itemStyle.Setters.Add(new Setter(
            AutomationProperties.NameProperty,
            new Binding(nameof(SolutionTreeNodeItem.AccessibleName))));
        itemStyle.Setters.Add(new EventSetter(
            UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler(OnItemMouseDown)));
        itemStyle.Setters.Add(new EventSetter(
            UIElement.MouseLeftButtonDownEvent,
            new MouseButtonEventHandler(OnItemMouseDown)));
        tree.ItemContainerStyle = itemStyle;

        tree.PreviewKeyDown += (_, e) =>
        {
            if (e.Key is Key.Return or Key.Enter)
            {
                HandleKey(e.Key, Keyboard.Modifiers);
                if (Tree.SelectedItem is SolutionTreeNodeItem item
                    && item.Node.Kind == SolutionTreeNodeKind.FileArtifact)
                {
                    e.Handled = true;
                }
            }
        };

        tree.PreviewMouseLeftButtonDown += OnItemMouseDown;
        tree.ContextMenuOpening += OnContextMenuOpening;

        return tree;
    }

    private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (Tree.SelectedItem is SolutionTreeNodeItem item
            && BuildFileMenu(item) is { } menu)
        {
            Tree.ContextMenu = menu;
            return;
        }

        e.Handled = true;
    }

    private void OnItemMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2)
        {
            return;
        }

        var container = Ancestor<TreeViewItem>(e.OriginalSource as DependencyObject)
            ?? (sender as TreeViewItem);
        if (container?.DataContext is not SolutionTreeNodeItem vm)
        {
            if (Tree.SelectedItem is SolutionTreeNodeItem selected)
            {
                vm = selected;
                container = null;
            }
            else
            {
                return;
            }
        }

        if (vm.Node.Kind == SolutionTreeNodeKind.FileArtifact
            && !string.IsNullOrEmpty(vm.Node.NodeId))
        {
            ActivateRequested?.Invoke(this, new SolutionTreeActivate(vm.Node.NodeId, NodeViewKind.Source));
            e.Handled = true;
            return;
        }

        if (vm.Node.Kind == SolutionTreeNodeKind.CensusFolder
            && vm.Node.Coverage == CensusFolderCoverage.Unindexed)
        {
            e.Handled = true;
            if (container is not null)
            {
                container.IsExpanded = false;
            }
        }
    }

    private static T? Ancestor<T>(DependencyObject? start) where T : DependencyObject
    {
        for (var d = start; d is not null; d = VisualTreeHelper.GetParent(d))
        {
            if (d is T match)
            {
                return match;
            }
        }

        return null;
    }

    private void RenderChrome(SolutionTreeResult? result)
    {
        _chrome.Children.Clear();
        if (_stale)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(12, 4, 12, 4),
            };
            var glyph = new Path
            {
                Name = "staleGlyph",
                Data = SolutionTreeGlyphs.Stale,
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform,
                StrokeThickness = 1.5,
                Fill = Brushes.Transparent,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
            };
            glyph.SetResourceReference(Shape.StrokeProperty, "InferredBrush");
            var stale = Muted("Stale");
            stale.Margin = new Thickness(0);
            stale.SetResourceReference(TextBlock.ForegroundProperty, "InferredBrush");
            row.Children.Add(glyph);
            row.Children.Add(stale);
            _chrome.Children.Add(row);
        }

        if (result is not null)
        {
            foreach (var disclosure in result.Disclosures)
            {
                var line = Muted(disclosure.Message);
                if (disclosure.Cause is SolutionTreeShortfallCause.Io
                    or SolutionTreeShortfallCause.Permission
                    or SolutionTreeShortfallCause.ReparsePoint
                    or SolutionTreeShortfallCause.UnresolvablePath
                    or SolutionTreeShortfallCause.Cap)
                {
                    line.SetResourceReference(TextBlock.ForegroundProperty, "UnverifiedBrush");
                }

                _chrome.Children.Add(line);
            }

            if (result.SkipListedDirectoriesOmitted > 0)
            {
                _chrome.Children.Add(Muted(
                    $"{result.SkipListedDirectoriesOmitted} skip-listed directories omitted"));
            }
        }
    }

    private void ShowPane(UIElement pane)
    {
        foreach (UIElement child in _body.Children)
        {
            child.Visibility = ReferenceEquals(child, pane) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static StackPanel Wayfinder(string heading, Button action)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(24),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 420,
        };
        var title = new TextBlock
        {
            Text = heading,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12),
        };
        title.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        panel.Children.Add(title);
        panel.Children.Add(action);
        return panel;
    }

    private static Button Action(string label, Action onClick)
    {
        var button = new Button
        {
            Content = label,
            MinHeight = 28,
            Padding = new Thickness(12, 2, 12, 2),
            HorizontalAlignment = HorizontalAlignment.Left,
            Command = new RelayCommand(onClick),
        };
        AutomationProperties.SetName(button, label);
        button.SetResourceReference(Control.BackgroundProperty, "SurfaceRaisedBrush");
        button.SetResourceReference(Control.ForegroundProperty, "TextBrush");
        button.SetResourceReference(Control.BorderBrushProperty, "BorderStrongBrush");
        return button;
    }

    private static TextBlock Muted(string text)
    {
        var block = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(12, 4, 12, 4),
        };
        block.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return block;
    }

    private static IEnumerable<SolutionTreeNodeItem> Walk(IEnumerable<SolutionTreeNodeItem> roots)
    {
        foreach (var n in roots)
        {
            yield return n;
            foreach (var c in Walk(n.Children))
            {
                yield return c;
            }
        }
    }

    private static TreeViewItem? ContainerOf(ItemsControl parent, SolutionTreeNodeItem item)
    {
        if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem found)
        {
            return found;
        }

        for (var i = 0; i < parent.Items.Count; i++)
        {
            if (parent.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem child)
            {
                var nested = ContainerOf(child, item);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }
}
