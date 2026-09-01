using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// Edge rails for collapsed tool zones (ADR-0021 collapse-to-rail). Wraps the docking host in a
/// <see cref="DockPanel"/> and shows a thin, clickable strip on the Left, Right or Bottom edge
/// whenever that tool zone is collapsed — the panes are retained in the model, and the rail is the
/// one-click way back (AC-F4). When a zone is expanded its rail is hidden and the dock host reclaims
/// the space. This is custom chrome around AvalonDock (which renders documents, not auto-hiding
/// anchorables), driven entirely by the zone model.
/// </summary>
public sealed class ZoneRails
{
    private const double RailThickness = 26;

    private readonly DockPanel _root = new() { LastChildFill = true };
    private readonly FrameworkElement _dockHost;
    private readonly Border _left;
    private readonly Border _right;
    private readonly Border _bottom;
    private readonly Func<WorkbenchLayout?> _zones;
    private readonly Action<ZoneId> _expand;
    private bool _hostAttached;

    public ZoneRails(FrameworkElement dockHost, Func<WorkbenchLayout?> zones, Action<ZoneId> expand)
    {
        _dockHost = dockHost ?? throw new ArgumentNullException(nameof(dockHost));
        _zones = zones ?? throw new ArgumentNullException(nameof(zones));
        _expand = expand ?? throw new ArgumentNullException(nameof(expand));

        _left = MakeRail(ZoneId.Left, vertical: true);
        _right = MakeRail(ZoneId.Right, vertical: true);
        _bottom = MakeRail(ZoneId.Bottom, vertical: false);

        DockPanel.SetDock(_left, Dock.Left);
        DockPanel.SetDock(_right, Dock.Right);
        DockPanel.SetDock(_bottom, Dock.Bottom);

        // Rails dock to the edges now; the host is attached lazily on first Root access (below) so a
        // caller that only wants the bare DockingManager can still parent it itself.
        _root.Children.Add(_left);
        _root.Children.Add(_right);
        _root.Children.Add(_bottom);

        Refresh();
    }

    /// <summary>
    /// The composed element to host in place of the bare docking manager. Attaching the host is
    /// deferred to here so that constructing the rails does not claim the host as a child — a caller
    /// (or a test) that hosts the manager directly is unaffected until it actually asks for the root.
    /// </summary>
    public FrameworkElement Root
    {
        get
        {
            if (!_hostAttached)
            {
                _root.Children.Add(_dockHost); // last child fills the centre
                _hostAttached = true;
            }

            return _root;
        }
    }

    /// <summary>Re-reads the zone model and shows a rail for each collapsed tool zone.</summary>
    public void Refresh()
    {
        var layout = _zones();
        UpdateRail(_left, ZoneId.Left, layout);
        UpdateRail(_right, ZoneId.Right, layout);
        UpdateRail(_bottom, ZoneId.Bottom, layout);
    }

    /// <summary>Whether a zone's rail is currently shown — exposed for tests.</summary>
    public bool RailVisible(ZoneId zone) => RailFor(zone).Visibility == Visibility.Visible;

    private Border RailFor(ZoneId zone) => zone switch
    {
        ZoneId.Left => _left,
        ZoneId.Right => _right,
        ZoneId.Bottom => _bottom,
        _ => throw new ArgumentOutOfRangeException(nameof(zone), zone, "only tool zones have rails"),
    };

    private void UpdateRail(Border rail, ZoneId zone, WorkbenchLayout? layout)
    {
        var state = layout?.Zone(zone);
        var show = state is { Collapsed: true };
        rail.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show && rail.Child is Button button)
        {
            var count = state!.Surfaces().Count();
            button.Content = Label(zone, count);
            button.ToolTip = $"Expand the {Name(zone)} ({count} pane{(count == 1 ? "" : "s")})";
            AutomationProperties.SetName(button, $"Expand the {Name(zone)}");
        }
    }

    private Border MakeRail(ZoneId zone, bool vertical)
    {
        var button = new Button
        {
            Style = Application.Current?.TryFindResource("RoundedButton") as Style,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(2),
            Cursor = System.Windows.Input.Cursors.Hand,
            Content = Label(zone, 0),
        };

        if (vertical)
        {
            button.LayoutTransform = new RotateTransform(zone == ZoneId.Left ? 90 : -90);
        }

        button.Click += (_, _) => _expand(zone);

        var rail = new Border
        {
            Background = Application.Current?.TryFindResource("SurfaceSunkenBrush") as Brush ?? Brushes.Gray,
            BorderBrush = Application.Current?.TryFindResource("BorderBrush") as Brush,
            BorderThickness = new Thickness(zone == ZoneId.Bottom ? 0 : 1, zone == ZoneId.Bottom ? 1 : 0, 0, 0),
            Child = button,
            Visibility = Visibility.Collapsed,
        };

        if (vertical)
        {
            rail.Width = RailThickness;
        }
        else
        {
            rail.Height = RailThickness;
        }

        return rail;
    }

    private static string Label(ZoneId zone, int count) => count > 0 ? $"{Name(zone)}  ({count})" : Name(zone);

    private static string Name(ZoneId zone) => zone switch
    {
        ZoneId.Left => "Left",
        ZoneId.Right => "Right",
        ZoneId.Bottom => "Bottom",
        _ => zone.ToString(),
    };
}
