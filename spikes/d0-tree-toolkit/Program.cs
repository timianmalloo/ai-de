// THROWNAWAY spike: installed WPF TreeView / TreeViewItem contract for D-0.
// Not referenced by production. Re-run: dotnet run --project spikes/d0-tree-toolkit
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var log = new List<string>();
        void M(string key, object? value)
        {
            var line = "MEASURE " + key + "=" + Format(value);
            log.Add(line);
            Console.WriteLine(line);
        }

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var window = new Window
        {
            Title = "D-0 tree toolkit spike",
            Width = 480,
            Height = 720,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = 40,
            Top = 40,
        };

        window.Loaded += (_, _) =>
        {
            try
            {
                Measure(window, M);
            }
            catch (Exception ex)
            {
                M("fatal", ex.GetType().Name + ": " + ex.Message);
                log.Add(ex.ToString());
                Console.Error.WriteLine(ex);
            }
            finally
            {
                WriteRaw(log);
                window.Close();
                app.Shutdown();
            }
        };

        app.Run(window);
        return log.Exists(l => l.StartsWith("MEASURE fatal=", StringComparison.Ordinal)) ? 1 : 0;
    }

    private static void Measure(Window window, Action<string, object?> M)
    {
        M("runtime.presentationframework", typeof(TreeView).Assembly.GetName().Version);
        M("runtime.tfm", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        M("runtime.os", Environment.OSVersion.VersionString);

        var host = new Grid();
        window.Content = host;

        MeasureEmpty(host, M);
        MeasureDefaultsAndHeight(host, M);
        MeasureUnindexedLeaf(host, M);
        MeasureKeyboard(host, M);
        MeasureUia(host, M);
        MeasureVirtualization(host, M);
        MeasureNesting(M);
        MeasureHierarchicalBinding(host, M);
        MeasureHeightClipsChildren(host, M);
        MeasureRejectedListViewRole(host, M);
        MeasureAppStyleGap(M);

        window.UpdateLayout();
    }

    private static void MeasureEmpty(Grid host, Action<string, object?> M)
    {
        var tree = NewTree();
        Mount(host, tree);
        M("empty.items", tree.Items.Count);
        M("empty.containers", CountContainers(tree));
        var peer = UIElementAutomationPeer.CreatePeerForElement(tree);
        M("empty.peer", peer?.GetType().Name);
        M("empty.control_type", peer?.GetAutomationControlType());
        M("empty.name_default", peer?.GetName());
        Unmount(host, tree);
    }

    private static void MeasureDefaultsAndHeight(Grid host, Action<string, object?> M)
    {
        var tree = NewTree();
        var plain = new TreeViewItem { Header = "unindexed_probe" };
        var min28 = new TreeViewItem { Header = "src", MinHeight = 28 };
        var header28 = new TreeViewItem
        {
            Header = new Border
            {
                MinHeight = 28,
                Child = new TextBlock
                {
                    Text = "Program.cs",
                    VerticalAlignment = VerticalAlignment.Center,
                },
            },
        };
        tree.Items.Add(plain);
        tree.Items.Add(min28);
        tree.Items.Add(header28);
        Mount(host, tree);

        M("default.is_virtualizing", VirtualizingPanel.GetIsVirtualizing(tree));
        M("default.virtualization_mode", VirtualizingPanel.GetVirtualizationMode(tree));
        M("default.scroll_unit", VirtualizingPanel.GetScrollUnit(tree));
        M("default.items_panel", PanelType(tree));
        M("default.item_is_virtualizing", VirtualizingPanel.GetIsVirtualizing(plain));
        M("default.item_tab_stop", plain.IsTabStop);
        M("default.item_is_expanded", plain.IsExpanded);
        M("default.item_has_items", plain.HasItems);
        M("default.item_height", plain.ActualHeight);
        M("default.item_header_height", HeaderHeight(plain));
        M("default.item_minheight", plain.MinHeight);
        M("min28.item_height", min28.ActualHeight);
        M("min28.header_height", HeaderHeight(min28));
        M("header28.item_height", header28.ActualHeight);
        M("header28.header_height", HeaderHeight(header28));
        M("header28.hit_width", header28.ActualWidth);
        M("header28.hit_meets_24x24", header28.ActualWidth >= 24 && HeaderHeight(header28) >= 24);
        M("header28.row_is_28", Math.Abs(HeaderHeight(header28) - 28) < 0.6 || HeaderHeight(header28) >= 28);
        Unmount(host, tree);
    }

    private static void MeasureUnindexedLeaf(Grid host, Action<string, object?> M)
    {
        var tree = NewTree();
        var parent = new TreeViewItem { Header = "src", IsExpanded = true };
        parent.Items.Add(new TreeViewItem { Header = "Program.cs" });
        var leaf = new TreeViewItem { Header = "unindexed_probe" };
        tree.Items.Add(parent);
        tree.Items.Add(leaf);
        Mount(host, tree);

        var parentExpander = FindExpander(parent);
        var leafExpander = FindExpander(leaf);
        M("unindexed.has_items", leaf.HasItems);
        M("unindexed.is_expanded", leaf.IsExpanded);
        M("unindexed.expander_found", leafExpander is not null);
        M("unindexed.expander_visibility", leafExpander?.Visibility);
        M("unindexed.expander_width", leafExpander?.ActualWidth);
        M("parent.has_items", parent.HasItems);
        M("parent.expander_visibility", parentExpander?.Visibility);
        M("parent.expander_width", parentExpander?.ActualWidth);

        leaf.Focus();
        var right = RaiseKey(leaf, Key.Right, Keyboard.KeyDownEvent);
        M("unindexed.right.handled", right.Handled);
        M("unindexed.right.is_expanded", leaf.IsExpanded);
        M("unindexed.right.items", leaf.Items.Count);

        var enter = RaiseKey(leaf, Key.Return, Keyboard.KeyDownEvent);
        M("unindexed.enter.handled", enter.Handled);
        M("unindexed.enter.is_expanded", leaf.IsExpanded);

        var left = RaiseKey(leaf, Key.Left, Keyboard.KeyDownEvent);
        M("unindexed.left.handled", left.Handled);
        M("unindexed.left.is_expanded", leaf.IsExpanded);

        var dbl = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent,
        };
        typeof(MouseButtonEventArgs).GetProperty("ClickCount")!.SetValue(dbl, 2);
        leaf.RaiseEvent(dbl);
        M("unindexed.dblclick.is_expanded", leaf.IsExpanded);
        M("unindexed.dblclick.items", leaf.Items.Count);

        leaf.IsExpanded = false;
        var peer = UIElementAutomationPeer.CreatePeerForElement(leaf);
        var expand = peer?.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;
        M("unindexed.uia.control_type", peer?.GetAutomationControlType());
        M("unindexed.uia.expand_pattern", expand is not null);
        M("unindexed.uia.expand_state", expand?.ExpandCollapseState);

        var parentPeer = UIElementAutomationPeer.CreatePeerForElement(parent);
        var parentExpand = parentPeer?.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;
        M("parent.uia.expand_state", parentExpand?.ExpandCollapseState);

        Unmount(host, tree);
    }

    private static void MeasureKeyboard(Grid host, Action<string, object?> M)
    {
        var tree = NewTree();
        var fileActivate = 0;
        var fileReveal = 0;
        var folderActivate = 0;
        var previewLog = new List<string>();

        tree.PreviewKeyDown += (_, e) =>
        {
            var mods = Keyboard.Modifiers;
            previewLog.Add(e.Key + "+" + mods);
            if (e.Key is Key.Return or Key.Enter)
            {
                if (tree.SelectedItem is TreeViewItem selected)
                {
                    var isFile = Equals(selected.Tag, "file-artifact");
                    if ((mods & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        if (isFile)
                        {
                            fileReveal++;
                        }

                        e.Handled = true;
                    }
                    else if (isFile)
                    {
                        fileActivate++;
                        e.Handled = true;
                    }
                    else
                    {
                        folderActivate++;
                    }
                }
            }
        };

        var folder = new TreeViewItem { Header = "src", Tag = "census-folder", IsExpanded = true };
        var file = new TreeViewItem { Header = "Program.cs", Tag = "file-artifact" };
        var unindexed = new TreeViewItem { Header = "unindexed_probe", Tag = "census-folder" };
        folder.Items.Add(file);
        tree.Items.Add(folder);
        tree.Items.Add(unindexed);
        Mount(host, tree);

        file.IsSelected = true;
        file.Focus();
        RaiseKey(file, Key.Return, Keyboard.PreviewKeyDownEvent);
        RaiseKey(file, Key.Return, Keyboard.KeyDownEvent);
        M("kb.file.enter.activate", fileActivate);
        M("kb.file.enter.reveal", fileReveal);
        M("kb.file.enter.folder_activate", folderActivate);
        M("kb.preview_saw", string.Join("|", previewLog));
        M("kb.modifiers_at_raise", Keyboard.Modifiers);

        previewLog.Clear();
        fileActivate = fileReveal = folderActivate = 0;
        var ctrlEnterBindingFired = 0;
        var cmd = new RoutedCommand("Reveal", typeof(Program));
        tree.CommandBindings.Add(new CommandBinding(cmd, (_, _) => ctrlEnterBindingFired++));
        tree.InputBindings.Add(new KeyBinding(cmd, new KeyGesture(Key.Enter, ModifierKeys.Control)));
        M("kb.ctrl_enter.gesture_matches_ctrl", new KeyGesture(Key.Enter, ModifierKeys.Control).Matches(tree, new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(tree)!, 0, Key.Enter) { RoutedEvent = Keyboard.KeyDownEvent }));
        M("kb.ctrl_enter.binding_count", tree.InputBindings.Count);
        M("kb.ctrl_enter.command_fired_on_raise", ctrlEnterBindingFired);

        unindexed.IsSelected = true;
        unindexed.Focus();
        var before = unindexed.IsExpanded;
        RaiseKey(unindexed, Key.Return, Keyboard.KeyDownEvent);
        M("kb.unindexed.enter.expands", unindexed.IsExpanded && !before);
        M("kb.unindexed.enter.is_expanded", unindexed.IsExpanded);

        folder.IsSelected = true;
        folder.Focus();
        folder.IsExpanded = false;
        var right = RaiseKey(folder, Key.Right, Keyboard.KeyDownEvent);
        M("kb.indexed.right.handled", right.Handled);
        M("kb.indexed.right.is_expanded", folder.IsExpanded);

        var down = RaiseKey(folder, Key.Down, Keyboard.KeyDownEvent);
        M("kb.indexed.down.handled", down.Handled);
        M("kb.selected_after_down", (tree.SelectedItem as TreeViewItem)?.Header);

        M("kb.source.treeview_handles_return", "no — TreeView.OnKeyDown switch has no Key.Return/Enter (dotnet/wpf release/10.0 TreeView.cs:506-590)");
        M("kb.source.treeviewitem_handles_return", "no — TreeViewItem.OnKeyDown handles Add/Subtract/Left/Right/Down/Up only (TreeViewItem.cs OnKeyDown)");
        M("kb.source.ctrl_arrows", "TreeView.OnKeyDown with IsControlKeyDown scrolls; Enter is not in that set. TreeViewItem skips Left/Right/Up/Down when Control is down.");
        Unmount(host, tree);
    }

    private static void MeasureUia(Grid host, Action<string, object?> M)
    {
        var tree = NewTree();
        var style = new Style(typeof(TreeViewItem));
        style.Setters.Add(new Setter(AutomationProperties.NameProperty, new Binding("AutomationName")));
        tree.ItemContainerStyle = style;
        tree.ItemTemplate = LabelTemplate();

        var root = new NodeVm("", "census-folder", "indexed-parent", "workspace");
        var src = new NodeVm("src", "census-folder", "indexed-parent", "src");
        var file = new NodeVm("src/Program.cs", "file-artifact", null, "Program.cs");
        var unindexed = new NodeVm("unindexed_probe", "census-folder", "unindexed", "unindexed_probe");
        src.Children.Add(file);
        root.Children.Add(src);
        root.Children.Add(unindexed);
        tree.ItemsSource = new[] { root };
        Mount(host, tree);

        ExpandAll(tree);
        var rootItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromItem(root)!;
        var srcItem = (TreeViewItem)rootItem.ItemContainerGenerator.ContainerFromItem(src)!;
        var fileItem = (TreeViewItem)srcItem.ItemContainerGenerator.ContainerFromItem(file)!;
        var unItem = (TreeViewItem)rootItem.ItemContainerGenerator.ContainerFromItem(unindexed)!;

        M("uia.bound.root.name_dp", AutomationProperties.GetName(rootItem));
        M("uia.bound.src.name_dp", AutomationProperties.GetName(srcItem));
        M("uia.bound.file.name_dp", AutomationProperties.GetName(fileItem));
        M("uia.bound.unindexed.name_dp", AutomationProperties.GetName(unItem));

        var filePeer = UIElementAutomationPeer.CreatePeerForElement(fileItem);
        var unPeer = UIElementAutomationPeer.CreatePeerForElement(unItem);
        var treePeer = UIElementAutomationPeer.CreatePeerForElement(tree);
        M("uia.bound.file.peer_name", filePeer?.GetName());
        M("uia.bound.unindexed.peer_name", unPeer?.GetName());
        M("uia.bound.file.kind_in_name", filePeer?.GetName()?.Contains("file-artifact", StringComparison.Ordinal) == true);
        M("uia.bound.unindexed.coverage_in_name", unPeer?.GetName()?.Contains("Unindexed", StringComparison.OrdinalIgnoreCase) == true);
        M("uia.tree.control_type", treePeer?.GetAutomationControlType());
        M("uia.item.control_type", filePeer?.GetAutomationControlType());
        M("uia.item.peer_type", filePeer?.GetType().Name);

        var unbound = new TreeViewItem { Header = "Program.cs" };
        tree.ItemsSource = null;
        tree.Items.Clear();
        tree.ItemContainerStyle = null;
        tree.Items.Add(unbound);
        tree.UpdateLayout();
        var unboundPeer = UIElementAutomationPeer.CreatePeerForElement(unbound);
        M("uia.unbound.peer_name", unboundPeer?.GetName());
        M("uia.unbound.name_is_header_only", unboundPeer?.GetName() == "Program.cs");
        Unmount(host, tree);
    }

    private static void MeasureVirtualization(Grid host, Action<string, object?> M)
    {
        const int n = 400;
        var defaultTree = NewTree();
        defaultTree.Height = 280;
        for (var i = 0; i < n; i++)
        {
            defaultTree.Items.Add(new TreeViewItem { Header = "f" + i.ToString("D3", CultureInfo.InvariantCulture) });
        }

        Mount(host, defaultTree);
        M("virt.off.realized", CountContainers(defaultTree));
        M("virt.off.is_virtualizing", VirtualizingPanel.GetIsVirtualizing(defaultTree));
        M("virt.off.panel", PanelType(defaultTree));
        Unmount(host, defaultTree);

        var virt = NewTree();
        virt.Height = 280;
        VirtualizingPanel.SetIsVirtualizing(virt, true);
        VirtualizingPanel.SetVirtualizationMode(virt, VirtualizationMode.Recycling);
        VirtualizingPanel.SetScrollUnit(virt, ScrollUnit.Pixel);
        for (var i = 0; i < n; i++)
        {
            virt.Items.Add(new TreeViewItem { Header = "f" + i.ToString("D3", CultureInfo.InvariantCulture) });
        }

        Mount(host, virt);
        M("virt.on.realized", CountContainers(virt));
        M("virt.on.is_virtualizing", VirtualizingPanel.GetIsVirtualizing(virt));
        M("virt.on.mode", VirtualizingPanel.GetVirtualizationMode(virt));
        M("virt.on.panel", PanelType(virt));
        Unmount(host, virt);

        var nested = NewTree();
        nested.Height = 280;
        VirtualizingPanel.SetIsVirtualizing(nested, true);
        VirtualizingPanel.SetVirtualizationMode(nested, VirtualizationMode.Recycling);
        var folder = new TreeViewItem { Header = "src", IsExpanded = true };
        for (var i = 0; i < n; i++)
        {
            folder.Items.Add(new TreeViewItem { Header = "f" + i.ToString("D3", CultureInfo.InvariantCulture) });
        }

        nested.Items.Add(folder);
        Mount(host, nested);
        M("virt.nested.folder_realized", nested.ItemContainerGenerator.ContainerFromIndex(0) is not null);
        M("virt.nested.children_realized", CountContainers(folder));
        M("virt.nested.folder_is_virtualizing", VirtualizingPanel.GetIsVirtualizing(folder));
        M("virt.nested.folder_panel", PanelType(folder));
        Unmount(host, nested);
    }

    private static void MeasureNesting(Action<string, object?> M)
    {
        var happy = Nest(
        [
            new Dto("", "census-folder", "indexed-parent"),
            new Dto("src", "census-folder", "indexed-parent"),
            new Dto("src/Program.cs", "file-artifact", null),
            new Dto("unindexed_probe", "census-folder", "unindexed"),
        ]);
        M("nest.happy.roots", string.Join(",", happy.Roots.Select(r => r.Path)));
        M("nest.happy.root_children", string.Join(",", happy.Roots[0].Children.Select(c => c.Path)));
        M("nest.happy.src_children", string.Join(",", happy.Roots[0].Children.First(c => c.Path == "src").Children.Select(c => c.Path)));
        M("nest.happy.unindexed_children", happy.Roots[0].Children.First(c => c.Path == "unindexed_probe").Children.Count);
        M("nest.happy.dropped", string.Join(",", happy.Dropped));

        var missingParent = Nest(
        [
            new Dto("", "census-folder", "indexed-parent"),
            new Dto("src", "census-folder", "indexed-parent"),
            new Dto("src/App/Program.cs", "file-artifact", null),
        ]);
        M("nest.orphan_file.dropped", string.Join(",", missingParent.Dropped));
        M("nest.orphan_file.invented_app", missingParent.All.Any(n => n.Path == "src/App"));
        M("nest.orphan_file.src_children", missingParent.Roots[0].Children.First(c => c.Path == "src").Children.Count);

        var orphanFolder = Nest(
        [
            new Dto("", "census-folder", "indexed-parent"),
            new Dto("deep/nested", "census-folder", "unindexed"),
        ]);
        M("nest.orphan_folder.roots", string.Join(",", orphanFolder.Roots.Select(r => r.Path)));
        M("nest.orphan_folder.invented_deep", orphanFolder.All.Any(n => n.Path == "deep"));
        M("nest.orphan_folder.dropped", string.Join(",", orphanFolder.Dropped));
    }

    private static void MeasureHierarchicalBinding(Grid host, Action<string, object?> M)
    {
        var tree = NewTree();
        tree.ItemTemplate = LabelTemplate();
        var nest = Nest(
        [
            new Dto("", "census-folder", "indexed-parent"),
            new Dto("src", "census-folder", "indexed-parent"),
            new Dto("src/Program.cs", "file-artifact", null),
            new Dto("unindexed_probe", "census-folder", "unindexed"),
        ]);
        tree.ItemsSource = nest.Roots;
        Mount(host, tree);
        var rootItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromItem(nest.Roots[0])!;
        rootItem.IsExpanded = true;
        tree.UpdateLayout();
        var srcVm = nest.Roots[0].Children.First(c => c.Path == "src");
        var unVm = nest.Roots[0].Children.First(c => c.Path == "unindexed_probe");
        var srcItem = (TreeViewItem)rootItem.ItemContainerGenerator.ContainerFromItem(srcVm)!;
        var unItem = (TreeViewItem)rootItem.ItemContainerGenerator.ContainerFromItem(unVm)!;
        srcItem.IsExpanded = true;
        tree.UpdateLayout();

        M("bind.root.has_items", rootItem.HasItems);
        M("bind.src.has_items", srcItem.HasItems);
        M("bind.unindexed.has_items", unItem.HasItems);
        M("bind.unindexed.expander", FindExpander(unItem)?.Visibility);
        M("bind.src.expander", FindExpander(srcItem)?.Visibility);
        M("bind.src.child_count", srcItem.Items.Count);
        M("bind.one_level_plus_nested", srcItem.Items.Count == 1 && unItem.Items.Count == 0);
        Unmount(host, tree);
    }

    private static void MeasureHeightClipsChildren(Grid host, Action<string, object?> M)
    {
        var tree = NewTree();
        var clipped = new TreeViewItem { Header = "src", Height = 28, IsExpanded = true };
        clipped.Items.Add(new TreeViewItem { Header = "Program.cs" });
        var minOnly = new TreeViewItem { Header = "lib", MinHeight = 28, IsExpanded = true };
        minOnly.Items.Add(new TreeViewItem { Header = "a.cs" });
        tree.Items.Add(clipped);
        tree.Items.Add(minOnly);
        Mount(host, tree);
        M("clip.height28.item_height", clipped.ActualHeight);
        M("clip.height28.header_height", HeaderHeight(clipped));
        M("clip.height28.child_container", clipped.ItemContainerGenerator.ContainerFromIndex(0) is not null);
        var child = clipped.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewItem;
        M("clip.height28.child_height", child?.ActualHeight);
        M("clip.height28.child_visible_height", child is null ? 0 : child.RenderSize.Height);
        M("clip.height28.clips_children", child is not null && clipped.ActualHeight + 0.5 < HeaderHeight(clipped) + child.ActualHeight);
        M("clip.min28.item_height", minOnly.ActualHeight);
        M("clip.min28.header_height", HeaderHeight(minOnly));
        var minChild = minOnly.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewItem;
        M("clip.min28.child_height", minChild?.ActualHeight);
        M("clip.min28.clips_children", minChild is not null && minOnly.ActualHeight + 0.5 < HeaderHeight(minOnly) + minChild.ActualHeight);
        Unmount(host, tree);
    }

    private static void MeasureRejectedListViewRole(Grid host, Action<string, object?> M)
    {
        var list = new ListView { Height = 80 };
        list.Items.Add("src");
        Mount(host, list);
        var listPeer = UIElementAutomationPeer.CreatePeerForElement(list);
        var item = list.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
        var itemPeer = item is null ? null : UIElementAutomationPeer.CreatePeerForElement(item);
        M("alt.listview.control_type", listPeer?.GetAutomationControlType());
        M("alt.listview.item_control_type", itemPeer?.GetAutomationControlType());
        Unmount(host, list);

        var items = new ItemsControl { Height = 80 };
        items.Items.Add("src");
        Mount(host, items);
        var icPeer = UIElementAutomationPeer.CreatePeerForElement(items);
        M("alt.itemscontrol.control_type", icPeer?.GetAutomationControlType());
        M("alt.itemscontrol.peer_type", icPeer?.GetType().Name);
        Unmount(host, items);
    }

    private static void MeasureAppStyleGap(Action<string, object?> M)
    {
        M("app.xaml.treeview_style", "Foreground/Background/BorderBrush only (src/AiDe.App/App.xaml:725-729). No Height, virtualization, template, or AutomationProperties.");
        M("app.xaml.treeviewitem_style", "Foreground/Background Transparent (App.xaml:731-734). No 28px, no expander override.");
        M("design.compact_row", "DESIGN.md:177 Compact density = 28px list rows");
        M("design.unverified", "DESIGN.md colors.unverified #98A3B2 (line 26); spec Part C binds Unindexed to {colors.unverified}");
        M("packages.wpf", "no WPF package — framework reference via UseWPF on net10.0-windows (AiDe.App.csproj:4-8; global.json sdk 10.0.303)");
        M("packages.webview2", "Microsoft.Web.WebView2 1.0.3485.44 present but D-0 host is WPF; HTML tree rejected");
    }

    private static TreeView NewTree()
    {
        return new TreeView
        {
            Background = Brushes.White,
            Foreground = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
        };
    }

    private static HierarchicalDataTemplate LabelTemplate()
    {
        var factory = new FrameworkElementFactory(typeof(TextBlock));
        factory.SetBinding(TextBlock.TextProperty, new Binding(nameof(NodeVm.Label)));
        return new HierarchicalDataTemplate(typeof(NodeVm))
        {
            ItemsSource = new Binding(nameof(NodeVm.Children)),
            VisualTree = factory,
        };
    }

    private static void Mount(Grid host, UIElement child)
    {
        host.Children.Clear();
        host.Children.Add(child);
        host.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
        host.UpdateLayout();
    }

    private static void Unmount(Grid host, UIElement child)
    {
        host.Children.Remove(child);
        host.UpdateLayout();
    }

    private static void ExpandAll(TreeView tree)
    {
        foreach (var item in tree.Items)
        {
            if (tree.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem tvi)
            {
                ExpandRecursive(tvi);
            }
        }

        tree.UpdateLayout();
    }

    private static void ExpandRecursive(TreeViewItem item)
    {
        item.IsExpanded = true;
        item.UpdateLayout();
        item.ApplyTemplate();
        for (var i = 0; i < item.Items.Count; i++)
        {
            if (item.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem child)
            {
                ExpandRecursive(child);
            }
        }
    }

    private static int CountContainers(ItemsControl ic)
    {
        var n = 0;
        for (var i = 0; i < ic.Items.Count; i++)
        {
            if (ic.ItemContainerGenerator.ContainerFromIndex(i) is not null)
            {
                n++;
            }
        }

        return n;
    }

    private static string PanelType(ItemsControl ic)
    {
        ic.ApplyTemplate();
        var presenter = FindNamed<ItemsPresenter>(ic, "ItemsHost")
            ?? FindDescendant<ItemsPresenter>(ic);
        presenter?.ApplyTemplate();
        if (presenter is not null && VisualTreeHelper.GetChildrenCount(presenter) > 0
            && VisualTreeHelper.GetChild(presenter, 0) is Panel panel)
        {
            return panel.GetType().Name;
        }

        return ic.ItemsPanel?.VisualTree?.Type?.Name ?? "none";
    }

    private static T? FindNamed<T>(DependencyObject root, string name)
        where T : FrameworkElement
    {
        if (root is T named && named.Name == name)
        {
            return named;
        }

        var n = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < n; i++)
        {
            var found = FindNamed<T>(VisualTreeHelper.GetChild(root, i), name);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static double HeaderHeight(TreeViewItem item)
    {
        item.ApplyTemplate();
        var header = item.Template?.FindName("PART_Header", item) as FrameworkElement;
        return header?.ActualHeight ?? item.ActualHeight;
    }

    private static ToggleButton? FindExpander(DependencyObject root)
    {
        if (root is ToggleButton { Name: "Expander" } named)
        {
            return named;
        }

        ToggleButton? any = null;
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ToggleButton tb && tb.Name == "Expander")
            {
                return tb;
            }

            var nested = FindExpander(child);
            if (nested is not null)
            {
                return nested;
            }

            if (any is null && child is ToggleButton fallback)
            {
                any = fallback;
            }
        }

        return any;
    }

    private static T? FindDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        var n = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < n; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            var nested = FindDescendant<T>(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static KeyEventArgs RaiseKey(UIElement el, Key key, RoutedEvent routed)
    {
        var source = PresentationSource.FromVisual(el)
            ?? throw new InvalidOperationException("no PresentationSource");
        var args = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key)
        {
            RoutedEvent = routed,
        };
        el.RaiseEvent(args);
        return args;
    }

    private static NestResult Nest(IReadOnlyList<Dto> nodes)
    {
        var folders = nodes
            .Where(n => n.Kind == "census-folder")
            .Select(n => n.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var vms = new Dictionary<string, NodeVm>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in nodes)
        {
            vms[n.Path] = new NodeVm(n.Path, n.Kind, n.Coverage, LabelOf(n));
        }

        var roots = new List<NodeVm>();
        var dropped = new List<string>();
        foreach (var n in nodes)
        {
            var parent = ParentPath(n.Path);
            if (parent is null)
            {
                roots.Add(vms[n.Path]);
                continue;
            }

            if (folders.Contains(parent))
            {
                vms[parent].Children.Add(vms[n.Path]);
                continue;
            }

            if (n.Kind == "census-folder")
            {
                roots.Add(vms[n.Path]);
                continue;
            }

            dropped.Add(n.Path);
        }

        return new NestResult(roots, dropped, vms.Values.ToList());
    }

    private static string? ParentPath(string path)
    {
        if (path.Length == 0)
        {
            return null;
        }

        var i = path.LastIndexOf('/');
        return i < 0 ? "" : path[..i];
    }

    private static string LabelOf(Dto n)
        => n.Path.Length == 0 ? "workspace" : n.Path[(n.Path.LastIndexOf('/') + 1)..];

    private static void WriteRaw(List<string> log)
    {
        var dir = SpikeDir();
        var path = System.IO.Path.Combine(dir, "RESULT-raw.txt");
        System.IO.File.WriteAllLines(path, log, Encoding.UTF8);
        Console.WriteLine("MEASURE raw_path=" + path);
    }

    private static string SpikeDir()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (System.IO.File.Exists(System.IO.Path.Combine(dir, "D0TreeToolkitSpike.csproj")))
            {
                return dir;
            }

            var parent = System.IO.Directory.GetParent(dir);
            if (parent is null)
            {
                break;
            }

            dir = parent.FullName;
        }

        return System.IO.Directory.GetCurrentDirectory();
    }

    private static string Format(object? value)
        => value switch
        {
            null => "<null>",
            double d => d.ToString("0.###", CultureInfo.InvariantCulture),
            float f => f.ToString("0.###", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "<null>",
        };

    private sealed record Dto(string Path, string Kind, string? Coverage);

    private sealed record NestResult(List<NodeVm> Roots, List<string> Dropped, List<NodeVm> All);

    private sealed class NodeVm
    {
        public NodeVm(string path, string kind, string? coverage, string label)
        {
            Path = path;
            Kind = kind;
            Coverage = coverage;
            Label = label;
        }

        public string Path { get; }
        public string Kind { get; }
        public string? Coverage { get; }
        public string Label { get; }
        public ObservableCollection<NodeVm> Children { get; } = [];
        public string AutomationName
        {
            get
            {
                if (Kind == "census-folder")
                {
                    var coverageWord = Coverage == "unindexed" ? "Unindexed" : "indexed-parent";
                    return Label + " census-folder " + coverageWord;
                }

                return Label + " file-artifact";
            }
        }
    }
}
