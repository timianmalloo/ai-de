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
        _chrome.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
        _body.Children.Add(_chrome);
        _body.Children.Add(_openSequence);
        _body.Children.Add(_list);
        Content = _body;
        AutomationProperties.SetName(this, Title);
    }

    public string Title { get; }

    public bool NeedsInitialBind { get; private set; } = true;

    public event EventHandler? RetryRequested;

    public void Show(EntryPointsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        NeedsInitialBind = false;
        _list.Items.Clear();
        foreach (var row in result.Rows)
        {
            _list.Items.Add($"{row.Kind}: {row.Display}");
        }

        _chrome.Text = result.OmittedByCap > 0
            ? string.Join(" · ", result.Disclosures)
            : $"{result.Rows.Count} entry-point candidates (unclassified until classifier)";
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
}
