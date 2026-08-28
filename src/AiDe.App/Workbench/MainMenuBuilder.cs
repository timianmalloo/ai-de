using System.Windows.Controls;
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
    private static readonly (string Menu, string[] CommandIds)[] Layout =
    [
        ("_File", ["workspace.open", "workspace.indexSolution", "workspace.refresh"]),
        ("_View", ["workbench.focusCanvas", "workbench.resetLayout", "workbench.toggleLock"]),
        ("_Terminal", ["workbench.dispatchPrompt"]),
        ("_Help", ["workspace.diagnostics"]),
    ];

    internal static void Build(Menu menu, WorkbenchController controller, Action? onExit = null)
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
                };

                var captured = command.Id;
                item.Click += (_, _) => controller.Execute(captured);
                top.Items.Add(item);
            }

            if (top.Items.Count == 0) continue;

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
