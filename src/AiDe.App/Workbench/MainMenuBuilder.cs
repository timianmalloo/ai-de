using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// Renders the menu bar from the derived contribution of the active perspective — the SAME model
/// the palette reads (<see cref="PerspectiveMenu"/>).
/// </summary>
/// <remarks>
/// <para><b>One derivation, three ways in.</b> Menu, palette and rail all read
/// <see cref="PerspectiveMenu.For"/>, so a command cannot be offered by one and missing from another
/// — and the tests that walk the derivation cover all three at once. This file used to hold a
/// static tuple list of which commands sit under which menu; that list was Core-owned data in a
/// Design-owned file and, once perspectives arrived, would have become a per-perspective list three
/// times over (Ruling 55b, ADR-0030). The mapping now lives on the catalog row (<c>Menu</c>,
/// <c>Scope</c>) and the kind row (<c>Entry</c>), and this builder only renders.</para>
///
/// <para><b>Discoverability was the defect.</b> Opening a workspace was reachable only by
/// <c>Ctrl+K, O</c> and by an environment variable set before launch — so the daemon path, indexing,
/// and everything downstream were unreachable by anyone who had not been told. A chord nobody was
/// told about is not a feature.</para>
/// </remarks>
internal static class MainMenuBuilder
{
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
            "perspective.explore" => "IconExplore",
            _ when id.StartsWith("perspective.", StringComparison.Ordinal) => "IconLayout",
            _ when id.Contains("search", StringComparison.Ordinal) => "IconSearch",
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

    /// <param name="recentSessions">
    /// Recently opened sessions, most recent first (R13 b3). <b>New construction</b>, and
    /// deliberately not the same list as <paramref name="recent"/>: that one is installation-scoped
    /// and holds repositories, while a session lives inside a workspace and carries the workspace it
    /// is bound to — which is what reopening one restores first.
    /// </param>
    /// <param name="onOpenRecentSession">Reopens a recent session by id.</param>
    /// <param name="derived">
    /// The active perspective's contribution (<see cref="PerspectiveMenu.For"/>). Required: a
    /// builder that defaulted it would render one perspective's menu inside another, which is the
    /// drift the derivation exists to make impossible. The window passes the model it also hands
    /// the palette, so the two cannot disagree (E12).
    /// </param>
    internal static void Build(
        Menu menu,
        WorkbenchController controller,
        PerspectiveMenu derived,
        Action? onExit = null,
        IReadOnlyList<string>? recent = null,
        Action<string>? onOpenRecent = null,
        IReadOnlyList<Sessions.RecentSessionEntry>? recentSessions = null,
        Action<string>? onOpenRecentSession = null)
    {
        ArgumentNullException.ThrowIfNull(menu);
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(derived);

        menu.Items.Clear();

        foreach (var group in derived.Menus)
        {
            var header = group.Menu;
            var top = new MenuItem { Header = header };
            var previousWasPerspective = false;

            foreach (var command in group.Catalog)
            {
                var isPerspective = PerspectiveSet.ByCommandId(command.Id) is not null;

                // The perspective radio group is its own block at the head of View (§B3 rule 1).
                if (previousWasPerspective && !isPerspective)
                {
                    top.Items.Add(new Separator());
                }

                top.Items.Add(Item(command, controller, isPerspective, derived.Perspective));
                previousWasPerspective = isPerspective;
            }

            // The allow-list-derived "New/Show <Title>" block follows the catalog rows (§B3 rule 3).
            if (group.Catalog.Count > 0 && group.Derived.Count > 0)
            {
                top.Items.Add(new Separator());
            }

            foreach (var command in group.Derived)
            {
                top.Items.Add(Item(command, controller, isPerspective: false, derived.Perspective));
            }

            if (top.Items.Count == 0) continue;

            if (header == "_File" && recentSessions is { Count: > 0 } && onOpenRecentSession is not null)
            {
                top.Items.Add(new Separator());
                var sessionMenu = new MenuItem { Header = "Recent _sessions" };

                foreach (var entry in recentSessions)
                {
                    var item = new MenuItem
                    {
                        // The session's NAME is what the operator recognises; the workspace it is
                        // bound to is the tooltip, because reopening restores that first and a
                        // session name alone does not say where it will land.
                        Header = entry.Name,
                        ToolTip = entry.WorkspaceRoot,
                    };

                    var captured = entry.SessionId;
                    item.Click += (_, _) => onOpenRecentSession(captured);
                    sessionMenu.Items.Add(item);
                }

                top.Items.Add(sessionMenu);
            }

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

    /// <summary>One menu item for one offered command.</summary>
    /// <remarks>
    /// <para><b>The keystroke column shows a BOUND gesture or nothing (PS-M4, US-C10 b3).</b> The
    /// catalog's <c>Ctrl+K, X</c> chords are announced strings nothing binds; a menu that printed
    /// them promised keystrokes that did nothing. What is shown is the binding's own display string,
    /// so the label follows the keyboard layout.</para>
    ///
    /// <para><b>The perspective entries are a radio group with the active one checked (PS-M1).</b>
    /// A <see cref="PerspectiveMenuItem"/>: its automation peer exposes the Toggle state a screen
    /// reader announces, and a click never toggles it — the presenter owns the state (US-C1:
    /// activating the active perspective is a no-op, and a checked radio item does not un-check
    /// itself). The check is drawn in the icon column as the accent glyph the design's state table
    /// names, because the app's menu template renders no check of its own.</para>
    /// </remarks>
    private static MenuItem Item(WorkbenchCommand command, WorkbenchController controller, bool isPerspective, Perspective active)
    {
        var bound = KeyGestures.For(command).FirstOrDefault();
        var isActive = isPerspective && string.Equals(command.Id, active.CommandId, StringComparison.Ordinal);

        MenuItem item = isPerspective
            ? new PerspectiveMenuItem { IsChecked = isActive, Icon = isActive ? CheckGlyph() : IconFor(command.Id) }
            : new MenuItem { Icon = IconFor(command.Id) };

        item.Header = command.Title;
        item.InputGestureText = bound?.GetDisplayStringForCulture(CultureInfo.CurrentCulture) ?? string.Empty;
        item.ToolTip = command.Hint;

        var captured = command.Id;
        item.Click += (_, _) => controller.Execute(captured);

        return item;
    }

    /// <summary>The accent check glyph that marks the active perspective (DESIGN.md, the menu state table's "checked" row).</summary>
    internal static System.Windows.Shapes.Path CheckGlyph() => new()
    {
        Data = CheckGeometry,
        Stretch = Stretch.Uniform,
        Width = 15,
        Height = 15,
        Stroke = Application.Current?.TryFindResource("AccentBrush") as Brush ?? Brushes.Transparent,
        StrokeThickness = 2,
        StrokeLineJoin = PenLineJoin.Round,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
        Tag = CheckTag,
    };

    /// <summary>The check mark's shape — a glyph, not a token; brushes and sizes follow the icon set.</summary>
    internal static readonly Geometry CheckGeometry = Geometry.Parse("M 2,8 L 6,12 L 14,4");

    /// <summary>What a test reads to know the active row carries the check and no other row does.</summary>
    internal const string CheckTag = "perspective-check";

    /// <summary>
    /// A menu item whose checked state the presenter owns and whose click never toggles it.
    /// </summary>
    /// <remarks>
    /// <para>WPF's <see cref="MenuItem.OnClick"/> flips <see cref="MenuItem.IsChecked"/> for a
    /// checkable item before raising <see cref="MenuItem.Click"/> — one render frame later for a
    /// user click — so a "restore the check in the handler" repair speaks an un-checked state to a
    /// screen reader and draws it for a frame; and overriding <c>OnClick</c> to raise <c>Click</c>
    /// alone skips <c>PreviewClick</c>, which is the event the menu closes on, leaving the popup
    /// open in menu mode (both the UX &amp; Accessibility reviewer's findings, rounds 1 and 2).</para>
    ///
    /// <para>So the item is <b>not</b> checkable — WPF's own click pipeline runs untouched: no
    /// toggle, PreviewClick closes the menu, Click is deferred past the render — and the Toggle
    /// pattern a screen reader reads is exposed by the peer instead, from <see cref="MenuItem.IsChecked"/>,
    /// which <see cref="MenuItem.OnIsCheckedChanged"/> still announces through that peer.</para>
    /// </remarks>
    internal sealed class PerspectiveMenuItem : MenuItem
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);

        private sealed class Peer(MenuItem owner) : MenuItemAutomationPeer(owner)
        {
            public override object? GetPattern(PatternInterface patternInterface) =>
                patternInterface == PatternInterface.Toggle ? this : base.GetPattern(patternInterface);
        }
    }
}
