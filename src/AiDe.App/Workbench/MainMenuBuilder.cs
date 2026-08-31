using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// Builds the menu bar from the SAME command catalog the palette reads.
/// </summary>
/// <remarks>
/// <para><b>One catalog, three ways in.</b> Menu, palette and chord all resolve to a catalog id, so
/// a command cannot exist in one and be missing from another — and the conformance test that walks
/// the catalog covers all three at once. Hand-writing menu items beside a catalog is how a menu
/// starts offering something the product no longer does.</para>
///
/// <para><b>Discoverability was the defect.</b> Opening a workspace was reachable only by
/// <c>Ctrl+K, O</c> and by an environment variable set before launch — so the daemon path, indexing,
/// and everything downstream were unreachable by anyone who had not been told. A chord nobody was
/// told about is not a feature.</para>
/// </remarks>
internal static class MainMenuBuilder
{
    /// <summary>Which catalog commands appear under which menu, in order.</summary>
    /// <remarks>
    /// Grouped by what the user is trying to DO, not by which subsystem implements it: opening a
    /// workspace and indexing it are one errand, and they live in different classes.
    /// </remarks>
    /// <summary>
    /// The icon for a menu command — a stroked <see cref="System.Windows.Shapes.Path"/> from the
    /// shared geometry set (App.xaml), keyed off the command id. Visual only (Design owns how a menu
    /// looks); a keyword heuristic with a layout-icon default so every item is iconed consistently.
    /// Null when app resources are not loaded (tests).
    /// </summary>
    private static System.Windows.Shapes.Path? IconFor(string id)
    {
        var key = id switch
        {
            _ when id.Contains("terminal", StringComparison.Ordinal) => "IconTerminal",
            _ when id.Contains("prompt", StringComparison.Ordinal) || id.Contains("dispatch", StringComparison.Ordinal) => "IconSend",
            "workspace.open" => "IconFolderOpen",
            _ when id.Contains("index", StringComparison.Ordinal) => "IconGraph",
            _ when id.Contains("refresh", StringComparison.Ordinal) => "IconRefresh",
            _ when id.Contains("canvas", StringComparison.Ordinal) => "IconGraph",
            _ => "IconLayout",
        };

        if (Application.Current?.TryFindResource(key) is not Geometry geometry)
        {
            return null;
        }

        return new System.Windows.Shapes.Path
        {
            Data = geometry,
            Stretch = Stretch.Uniform,
            Width = 15,
            Height = 15,
            Stroke = Application.Current?.TryFindResource("TextMutedBrush") as Brush ?? Brushes.Gray,
            StrokeThickness = 1.5,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        };
    }

    private static readonly (string Menu, string[] CommandIds)[] Layout =
    [
        // CORE-OWNED DATA in a design-owned file: which commands exist and which menu they belong
        // to is a Core decision, and TheMenuCoversEveryCatalogCommand makes adding a command and
        // placing it one atomic change. Recorded in docs/collaboration/session-contracts.md, with a
        // proposal to move this mapping onto the catalog entry so the seam stops crossing here.
        ("_File", ["workspace.open", "workspace.indexSolution", "workspace.reindexAll", "workspace.refresh"]),
        ("_Edit", ["workbench.moveSurface", "workbench.resizePane"]),
        ("_View", ["workbench.focusCanvas", "workbench.nextSurface", "workbench.previousSurface",
                   "workbench.reorderSurface", "workbench.newClassDiagram", "workbench.newCodeViewer",
                   "workbench.newDiagnostics"]),
        ("_Window", ["workbench.floatPane", "workbench.collapsePane", "workbench.maximizePane",
                     "workbench.closeSurface", "workbench.toggleLock", "workbench.resetLayout"]),
        ("_Terminal", ["terminal.new", "terminal.newAgent", "workbench.dispatchPrompt", "workbench.newPromptDraft"]),
        ("_Help", ["workspace.diagnostics"]),
    ];

    /// <summary>
    /// Recently opened workspaces, most recent first.
    /// </summary>
    /// <remarks>
    /// <para>Kept beside the shell's own state rather than in the workspace, because it is a fact
    /// about this INSTALLATION and not about any one repository — a recent list stored in a
    /// workspace would be invisible from the first-run window that most needs it.</para>
    ///
    /// <para>A path that no longer exists is dropped on read rather than shown and refused. A menu
    /// offering something that cannot work teaches the user to distrust the menu.</para>
    /// </remarks>
    internal static IReadOnlyList<string> RecentWorkspaces(string? stateDirectory)
    {
        if (string.IsNullOrEmpty(stateDirectory)) return [];

        var path = Path.Combine(stateDirectory, "recent-workspaces.txt");
        if (!File.Exists(path)) return [];

        try
        {
            return [.. File.ReadAllLines(path)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && Directory.Exists(l))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)];
        }
        catch (IOException)
        {
            return [];
        }
    }

    /// <summary>Records a workspace as recently opened. Newest first, deduplicated, capped.</summary>
    internal static void RememberWorkspace(string? stateDirectory, string workspaceRoot)
    {
        if (string.IsNullOrEmpty(stateDirectory) || string.IsNullOrEmpty(workspaceRoot)) return;

        try
        {
            Directory.CreateDirectory(stateDirectory);
            var path = Path.Combine(stateDirectory, "recent-workspaces.txt");

            var existing = File.Exists(path) ? File.ReadAllLines(path) : [];
            var updated = new[] { workspaceRoot }
                .Concat(existing.Where(l => !string.Equals(l.Trim(), workspaceRoot, StringComparison.OrdinalIgnoreCase)))
                .Where(l => l.Trim().Length > 0)
                .Take(8);

            File.WriteAllLines(path, updated);
        }
        catch (IOException)
        {
            // A recent list that cannot be written is a convenience nobody gets, not a failure that
            // should stop a workspace opening.
        }
    }

    internal static void Build(
        Menu menu,
        WorkbenchController controller,
        Action? onExit = null,
        IReadOnlyList<string>? recent = null,
        Action<string>? onOpenRecent = null)
    {
        ArgumentNullException.ThrowIfNull(menu);
        ArgumentNullException.ThrowIfNull(controller);

        menu.Items.Clear();

        foreach (var (header, commandIds) in Layout)
        {
            var top = new MenuItem { Header = header };

            foreach (var id in commandIds)
            {
                var command = WorkbenchCommandCatalog.All.FirstOrDefault(c => c.Id == id);

                // A layout entry naming a command the catalog does not have is a bug, not something
                // to render as a dead item. Skipped rather than shown, and the catalog conformance
                // test is what stops it happening silently.
                if (command is null) continue;

                var item = new MenuItem
                {
                    Header = command.Title,
                    InputGestureText = command.Gesture,
                    ToolTip = command.Hint,
                    Icon = IconFor(command.Id),
                };

                var captured = command.Id;
                item.Click += (_, _) => controller.Execute(captured);
                top.Items.Add(item);
            }

            if (top.Items.Count == 0) continue;

            if (header == "_File" && recent is { Count: > 0 } && onOpenRecent is not null)
            {
                top.Items.Add(new Separator());
                var recentMenu = new MenuItem { Header = "Recent _workspaces" };

                foreach (var path in recent)
                {
                    var item = new MenuItem
                    {
                        // The folder NAME is what the user recognises; the full path is the tooltip.
                        // A menu of long paths is a menu nobody reads.
                        Header = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar)),
                        ToolTip = path,
                    };

                    var captured = path;
                    item.Click += (_, _) => onOpenRecent(captured);
                    recentMenu.Items.Add(item);
                }

                top.Items.Add(recentMenu);
            }

            if (header == "_File" && onExit is not null)
            {
                top.Items.Add(new Separator());
                var exit = new MenuItem { Header = "E_xit", InputGestureText = "Alt+F4" };
                exit.Click += (_, _) => onExit();
                top.Items.Add(exit);
            }

            menu.Items.Add(top);
        }
    }
}
