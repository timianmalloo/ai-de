using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using AiDe.Core.Projections;

namespace AiDe.App.Workbench;

/// <summary>
/// Architecture Entry-points listing over <see cref="IWorkspaceQueries.EntryPointsAsync"/>.
/// Open Sequence is disabled (<c>mapping-unavailable</c>) until mapper r4 is frozen and implemented.
/// </summary>
public sealed class EntryPointsSurface : ContentControl
{
    private readonly ListBox _list = new() { BorderThickness = new Thickness(0) };
    private readonly Button _openSequence;
    private readonly TextBlock _chrome = new() { Margin = new Thickness(12, 4, 12, 4), TextWrapping = TextWrapping.Wrap };
    private readonly StackPanel _body = new();

    public EntryPointsSurface(string? title = null)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "Entry-points" : title;
        _openSequence = new Button
        {
            Content = "Open Sequence",
            IsEnabled = false,
            Margin = new Thickness(12, 4, 12, 8),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        AutomationProperties.SetName(_openSequence, "Open Sequence mapping-unavailable");
        _chrome.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        _body.Children.Add(_chrome);
        _body.Children.Add(_openSequence);
        _body.Children.Add(_list);
        Content = _body;
        AutomationProperties.SetName(this, Title);
        _list.MouseDoubleClick += (_, _) => Activate(source: false);
        _list.PreviewKeyDown += (_, e) =>
        {
            if (e.Key is Key.Return or Key.Enter)
            {
                HandleKey(e.Key, Keyboard.Modifiers);
                e.Handled = true;
            }
        };
    }

    /// <summary>Enter = graph neighbourhood; Ctrl+Enter = View source. Sequence is not this path.</summary>
    internal void HandleKey(Key key, ModifierKeys modifiers)
    {
        if (key is not (Key.Return or Key.Enter))
        {
            return;
        }

        Activate(source: (modifiers & ModifierKeys.Control) == ModifierKeys.Control);
    }

    private void Activate(bool source)
    {
        if (_list.SelectedItem is not EntryPointListItem item
            || string.IsNullOrEmpty(item.Row.NodeId))
        {
            return;
        }

        ActivateRequested?.Invoke(
            this,
            new EntryPointsActivate(
                item.Row.NodeId,
                source ? NodeViewKind.Source : NodeViewKind.GraphNeighbourhood));
    }

    public string Title { get; }

    public bool NeedsInitialBind { get; private set; } = true;

    public event EventHandler? RetryRequested;

    public event EventHandler<EntryPointsActivate>? ActivateRequested;

    public void Show(EntryPointsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        NeedsInitialBind = false;
        _list.Items.Clear();
        foreach (var row in result.Rows)
        {
            _list.Items.Add(new EntryPointListItem(row));
        }

        var api = result.Rows.Count(r => r.Kind == EntryPointKind.Api);
        var ux = result.Rows.Count(r => r.Kind == EntryPointKind.Ux);
        var cli = result.Rows.Count(r => r.Kind == EntryPointKind.Cli);
        var unc = result.Rows.Count(r => r.Kind == EntryPointKind.Unclassified);
        // The disclosures are the bound; the counts are a summary of what IS on screen. Key the
        // caveat off the list itself, never off OmittedByCap: the two move together only by the
        // construction of today's single producer, so a disclosure the cap did not raise would be
        // dropped with no trace (session-contracts §8.3a).
        _chrome.Text = result.Disclosures.Count > 0
            ? string.Join(" · ", result.Disclosures)
            : $"{api} api · {ux} ux · {cli} cli · {unc} unclassified";
        _openSequence.IsEnabled = false;
    }

    public void ShowNoWorkspace()
    {
        NeedsInitialBind = false;
        _list.Items.Clear();
        _chrome.Text = "Open a workspace to see entry points.";
    }

    public void ShowError(string message)
    {
        _chrome.Text = string.IsNullOrWhiteSpace(message) ? "Could not read entry points." : message;
        var retry = new Button { Content = "Retry", Margin = new Thickness(12, 4, 12, 4) };
        AutomationProperties.SetName(retry, "Retry");
        retry.Click += (_, _) => RetryRequested?.Invoke(this, EventArgs.Empty);
        if (_body.Children.Count < 4)
        {
            _body.Children.Insert(1, retry);
        }
    }

    internal bool OpenSequenceEnabled => _openSequence.IsEnabled;

    internal void ListSelectFirst()
    {
        if (_list.Items.Count > 0)
        {
            _list.SelectedIndex = 0;
        }
    }

    internal sealed record EntryPointListItem(EntryPointRow Row)
    {
        public override string ToString() => $"{Row.Kind}: {Row.Display}";
    }
}

public sealed record EntryPointsActivate(string NodeId, NodeViewKind Kind);
