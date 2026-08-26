// Spike: does a real UI Automation client see a usable tree for an AvalonDock workbench?
//
// ADR-0012 records that AvalonDock 5.0.0 ships zero AutomationPeer types. That is a fact about the
// assembly; it is NOT the same question as "what does an assistive technology actually see", because
// WPF synthesises a generic peer for any FrameworkElement that does not supply one. This spike asks
// the second question, which is the one that decides whether the ADR's bet survives.
//
// It queries the same UIA tree Accessibility Insights reads, but programmatically, so the evidence
// is re-runnable and diffable rather than a screenshot.
//
//   dotnet run -- host    show the window (used as a child process)
//   dotnet run            probe: launch the host, walk its UIA tree, print the report
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using AvalonDock;
using AvalonDock.Layout;

internal static class Program
{
    private const string WindowTitle = "AvalonDock A11y Probe Host";

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "host")
        {
            RunHost();
            return 0;
        }

        return RunProbe();
    }

    // ── The host: a realistic workbench — two docked panes, a tabbed stack, and (as the control
    //    baseline) a plain WPF GridSplitter, which we know DOES supply a peer. ─────────────────
    private static void RunHost()
    {
        var app = new Application();

        var manager = new DockingManager();
        var anchorables = new LayoutAnchorablePane();
        anchorables.Children.Add(new LayoutAnchorable
        { Title = "Provenance", ContentId = "provenance", Content = new TextBlock { Text = "provenance" } });
        anchorables.Children.Add(new LayoutAnchorable
        { Title = "Health", ContentId = "health", Content = new TextBlock { Text = "health" } });

        var documents = new LayoutDocumentPane();
        documents.Children.Add(new LayoutDocument
        { Title = "Explore", ContentId = "explore", Content = new TextBlock { Text = "explore" } });
        documents.Children.Add(new LayoutDocument
        { Title = "Domain", ContentId = "domain", Content = new TextBlock { Text = "domain" } });

        var panel = new LayoutPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        panel.Children.Add(documents);
        panel.Children.Add(anchorables);
        manager.Layout = new LayoutRoot { RootPanel = panel };

        // Control baseline in the same window and the same UIA tree.
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(120) });
        Grid.SetRow(manager, 0);
        grid.Children.Add(manager);

        var splitter = new GridSplitter
        {
            Height = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Name = "BaselineGridSplitter",
        };
        AutomationProperties.SetName(splitter, "Baseline WPF GridSplitter");
        Grid.SetRow(splitter, 1);
        grid.Children.Add(splitter);

        var baselineTabs = new TabControl();
        baselineTabs.Items.Add(new TabItem { Header = "Baseline Tab A", Content = new TextBlock { Text = "a" } });
        baselineTabs.Items.Add(new TabItem { Header = "Baseline Tab B", Content = new TextBlock { Text = "b" } });
        AutomationProperties.SetName(baselineTabs, "Baseline WPF TabControl");
        Grid.SetRow(baselineTabs, 2);
        grid.Children.Add(baselineTabs);

        // Fix candidate B: a visual-tree pass that names realized TabItems from their bound
        // LayoutContent.Title. Candidate A (a typed TabItem style in DockingManager.Resources) was
        // tested and did NOT reach them.
        if (Environment.GetEnvironmentVariable("AIDE_FIX_TAB_NAMES") == "2")
        {
            manager.LayoutUpdated += (_, _) => NameTabs(manager);
        }

        var window = new Window
        {
            Title = WindowTitle,
            Width = 1000,
            Height = 640,
            Content = grid,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };

        app.Run(window);
    }

    /// <summary>Names every realized TabItem from the LayoutContent it is bound to.</summary>
    private static void NameTabs(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TabItem tab && tab.DataContext is LayoutContent content
                && !string.IsNullOrEmpty(content.Title))
            {
                AutomationProperties.SetName(tab, content.Title);
            }

            NameTabs(child);
        }
    }

    // ── The probe ─────────────────────────────────────────────────────────────────────────────
    private static int RunProbe()
    {
        AttachConsole(-1);
        var report = new StringBuilder();
        void W(string line = "") { Console.WriteLine(line); report.AppendLine(line); }

        var exe = Environment.ProcessPath!;
        using var child = Process.Start(new ProcessStartInfo(exe, "host") { UseShellExecute = false })!;

        AutomationElement window = null;
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline && window is null)
        {
            Thread.Sleep(400);
            window = AutomationElement.RootElement.FindFirst(TreeScope.Children,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ProcessIdProperty, child.Id),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)));
        }

        if (window is null)
        {
            Console.Error.WriteLine("FAIL: the host window never appeared in the UIA tree.");
            try { child.Kill(true); } catch { }
            return 2;
        }

        Thread.Sleep(1200);   // let the docking layout settle

        W("UIA PROBE — AvalonDock 5.0.0 workbench");
        W(new string('=', 100));
        W($"host window : {window.Current.Name}  (pid {child.Id})");
        W($"probed      : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        W();

        var all = window.FindAll(TreeScope.Descendants, System.Windows.Automation.Condition.TrueCondition);
        W($"total elements in the control view : {all.Count}");
        W();

        // 1. What control types does the tree actually contain?
        var byType = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var namedPanes = new List<string>();
        var transformable = new List<string>();
        var keyboardFocusable = new List<string>();
        AutomationElement adSplitter = null;
        AutomationElement baselineSplitter = null;

        foreach (AutomationElement e in all)
        {
            var c = e.Current;
            var type = c.ControlType?.ProgrammaticName?.Replace("ControlType.", string.Empty) ?? "?";
            byType[type] = byType.TryGetValue(type, out var n) ? n + 1 : 1;

            var label = $"{type}/'{Trim(c.Name)}'" + (string.IsNullOrEmpty(c.ClassName) ? "" : $" [{c.ClassName}]");

            if (type is "Pane" or "Tab" or "TabItem" or "Group" && !string.IsNullOrWhiteSpace(c.Name))
            {
                namedPanes.Add(label);
            }

            if (e.TryGetCurrentPattern(TransformPattern.Pattern, out _))
            {
                transformable.Add(label);
            }

            if (c.IsKeyboardFocusable)
            {
                keyboardFocusable.Add(label);
            }

            if (c.ClassName is "LayoutGridResizerControl" or "LayoutGridResizer"
                || (type == "Thumb" && c.ClassName != "GridSplitter"))
            {
                adSplitter = e;
            }

            if (c.AutomationId == "BaselineGridSplitter" || c.Name == "Baseline WPF GridSplitter")
            {
                baselineSplitter = e;
            }
        }

        // Every Thumb, in full: "not present" and "present but unusable" are different findings
        // with different fixes, so the claim is checked rather than inferred from a lookup miss.
        W("-- Every Thumb in the tree (the splitter candidates) --------------------------------");
        foreach (AutomationElement th in window.FindAll(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Thumb)))
        {
            var tc = th.Current;
            var pats = th.GetSupportedPatterns().Select(x => x.ProgrammaticName
                .Replace("PatternIdentifiers.Pattern", string.Empty)).ToArray();
            W($"  ClassName='{tc.ClassName}' Name='{Trim(tc.Name)}' AutomationId='{tc.AutomationId}'");
            W($"    KeyboardFocusable={tc.IsKeyboardFocusable} Enabled={tc.IsEnabled} "
              + $"Bounds={tc.BoundingRectangle.Width:F0}x{tc.BoundingRectangle.Height:F0}");
            W($"    Patterns=[{(pats.Length == 0 ? "none" : string.Join(", ", pats))}]");
        }

        W();
        W("── Control types present ───────────────────────────────────────────────────────────");
        foreach (var (type, count) in byType)
        {
            W($"  {count,4}  {type}");
        }

        W();
        W("── Are the panes identifiable to AT? ───────────────────────────────────────────────");
        if (namedPanes.Count == 0)
        {
            W("  NONE. No Pane/Tab/TabItem/Group carries a usable Name.");
        }
        else
        {
            foreach (var p in namedPanes.Take(20))
            {
                W($"  {p}");
            }
        }

        W();
        W("── THE DECIDING QUESTION: is the AvalonDock splitter reachable and resizable? ───────");
        W(Describe("AvalonDock LayoutGridResizerControl", adSplitter));
        W(Describe("WPF GridSplitter (control baseline)", baselineSplitter));

        W();
        W($"── Keyboard-focusable elements: {keyboardFocusable.Count} ───────────────────────────");
        foreach (var k in keyboardFocusable.Take(20))
        {
            W($"  {k}");
        }

        W();
        W($"── Elements exposing TransformPattern (programmatic resize): {transformable.Count} ──");
        foreach (var t in transformable.Take(10))
        {
            W($"  {t}");
        }

        W();
        W(new string('=', 100));
        var splitterUsable = adSplitter is not null
            && (adSplitter.Current.IsKeyboardFocusable
                || adSplitter.TryGetCurrentPattern(TransformPattern.Pattern, out _));
        W($"VERDICT  panes named to AT ........ {(namedPanes.Count > 0 ? "YES" : "NO")}");
        W($"VERDICT  AD splitter in UIA tree .. {(adSplitter is not null ? "YES" : "NO")}");
        W($"VERDICT  AD splitter operable ..... {(splitterUsable ? "YES" : "NO")}");
        W($"VERDICT  baseline splitter operable {(baselineSplitter is not null && (baselineSplitter.Current.IsKeyboardFocusable || baselineSplitter.TryGetCurrentPattern(TransformPattern.Pattern, out _)) ? "YES" : "NO")}");

        try { child.CloseMainWindow(); child.WaitForExit(3000); if (!child.HasExited) child.Kill(true); }
        catch { /* the host is a probe fixture; failing to close it must not fail the probe */ }

        File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "uia-report.txt"), report.ToString());
        return 0;
    }

    private static string Describe(string label, AutomationElement e)
    {
        if (e is null)
        {
            return $"  {label,-38} : NOT PRESENT IN THE UIA TREE";
        }

        var c = e.Current;
        var patterns = e.GetSupportedPatterns()
            .Select(p => p.ProgrammaticName.Replace("PatternIdentifiers.Pattern", string.Empty))
            .ToArray();
        return $"  {label,-38} : ControlType={c.ControlType?.ProgrammaticName?.Replace("ControlType.", "")}"
             + $" Name='{Trim(c.Name)}' KeyboardFocusable={c.IsKeyboardFocusable}"
             + $" Enabled={c.IsEnabled} Offscreen={c.IsOffscreen}"
             + Environment.NewLine
             + $"  {new string(' ', 38)}   Patterns=[{(patterns.Length == 0 ? "none" : string.Join(", ", patterns))}]";
    }

    private static string Trim(string s) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length > 42 ? s[..42] + "…" : s);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int processId);
}
