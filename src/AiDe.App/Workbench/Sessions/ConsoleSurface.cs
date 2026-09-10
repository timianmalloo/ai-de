using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// The Console canvas mode (R16 b1): the merged stream across every lane of one session, with a
/// lane rail and tree filtering.
/// </summary>
/// <remarks>
/// <para><b>Attribution is on the row, not on the layout.</b> Every line carries the lane that
/// produced it and renders it as a chip beside the text, so a two-lane stream can never present as
/// one voice — which is the failure R16 b1's clause names. The rail on the left is the same fact
/// summarised: who is talking, and how much.</para>
///
/// <para><b>Filtering is a tree, and it hides rather than drops.</b> A lane node excludes the whole
/// lane; a kind node under it excludes one kind of that lane's traffic. The model keeps every row it
/// ever received either way, so a filter can never destroy the history the ordinal oracle reads.</para>
///
/// <para><b>Disposal is announced</b> (<see cref="SessionDisposalSignal"/>). This surface is
/// retained across a canvas mode switch and a tab switch; the ledger is how that claim is checked
/// rather than asserted, because <c>Assert.Same</c> passes on a disposed instance.</para>
/// </remarks>
public sealed class ConsoleSurface : ContentControl, IDisposable
{
    private readonly StackPanel _rows = new();
    private readonly TreeView _filter = new();
    private readonly ConsoleStreamModel _model;
    private bool _disposed;

    /// <param name="model">The merged stream. Shared with the document, never copied.</param>
    public ConsoleSurface(ConsoleStreamModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _model = model;

        AutomationProperties.SetName(this, "Console");
        SetResourceReference(BackgroundProperty, "SurfaceBrush");

        AutomationProperties.SetName(_filter, "Lane and kind filter");
        _filter.Width = 190;
        _filter.BorderThickness = new Thickness(0);
        _filter.Background = null;

        var rail = new DockPanel { LastChildFill = true, Margin = new Thickness(10, 10, 6, 10) };
        var railHeading = Muted("Lanes");
        railHeading.FontWeight = FontWeights.SemiBold;
        DockPanel.SetDock(railHeading, Dock.Top);
        rail.Children.Add(railHeading);
        rail.Children.Add(_filter);
        DockPanel.SetDock(rail, Dock.Left);

        AutomationProperties.SetName(_rows, "Merged stream");

        var scroller = new ScrollViewer
        {
            Content = _rows,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(6, 10, 10, 10),
        };

        var root = new DockPanel { LastChildFill = true };
        root.Children.Add(rail);
        root.Children.Add(scroller);
        Content = root;

        _model.Changed += Render;
        Render();
    }

    /// <summary>The merged stream this console shows.</summary>
    public ConsoleStreamModel Model => _model;

    /// <summary>The lane names the rail is showing, in rail order — what a test reads instead of the tree.</summary>
    public IReadOnlyList<string> RailLanes =>
        [.. _filter.Items.OfType<TreeViewItem>().Select(i => (string)i.Tag)];

    /// <summary>Every rendered line, as "lane: text" — the rendered attribution, not the model's.</summary>
    public IReadOnlyList<string> RenderedRows =>
        [.. _rows.Children.OfType<FrameworkElement>().Select(e => (string)e.Tag)];

    private void Render()
    {
        _rows.Children.Clear();

        foreach (var row in _model.VisibleRows)
        {
            var chip = new Border
            {
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 0, 6, 0),
                Margin = new Thickness(0, 1, 8, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock { Text = row.LaneName, FontSize = 11 },
            };
            chip.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");

            var text = new TextBlock { Text = row.Text, TextWrapping = TextWrapping.Wrap };
            text.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

            var line = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 2, 0, 2),

                // The rendered attribution, in one readable string, so a test asserts on what the
                // pane shows rather than on the model it was built from.
                Tag = $"{row.LaneName}: {row.Text}",
            };
            line.Children.Add(chip);
            line.Children.Add(text);
            AutomationProperties.SetName(line, $"{row.LaneName}, {row.Kind}: {row.Text}");

            _rows.Children.Add(line);
        }

        RenderFilter();
    }

    private void RenderFilter()
    {
        _filter.Items.Clear();

        foreach (var lane in _model.Rail)
        {
            var laneNode = new TreeViewItem
            {
                Header = FilterHeader(
                    $"{lane.LaneName} ({lane.Rows})",
                    lane.Visible,
                    visible => _model.SetLaneVisible(lane.LaneId, visible)),
                IsExpanded = true,
                Tag = lane.LaneId,
            };

            foreach (var kind in _model.FilterTree.Where(
                n => string.Equals(n.LaneId, lane.LaneId, StringComparison.Ordinal) && n.Kind is not null))
            {
                laneNode.Items.Add(new TreeViewItem
                {
                    Header = FilterHeader(
                        $"{kind.Label} ({kind.Rows})",
                        kind.Visible,
                        visible => _model.SetKindVisible(kind.LaneId, kind.Kind!, visible)),
                    Tag = $"{kind.LaneId}/{kind.Kind}",
                });
            }

            _filter.Items.Add(laneNode);
        }
    }

    private static FrameworkElement FilterHeader(string label, bool visible, Action<bool> onChanged)
    {
        var box = new CheckBox { Content = label, IsChecked = visible };
        AutomationProperties.SetName(box, label);
        box.Checked += (_, _) => onChanged(true);
        box.Unchecked += (_, _) => onChanged(false);
        return box;
    }

    private static TextBlock Muted(string text)
    {
        var block = new TextBlock { Text = text, FontSize = 12, Margin = new Thickness(0, 0, 0, 6) };
        block.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return block;
    }

    /// <summary>Detaches from the stream. Announced first, so the disposal is counted either way.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Started BEFORE anything is torn down: the ledger counts the attempt, not the success.
        using var counted = SessionDisposalSignal.Source.StartActivity(
            SessionDisposalLedger.SurfaceDisposeActivity);

        _disposed = true;
        _model.Changed -= Render;
    }
}
