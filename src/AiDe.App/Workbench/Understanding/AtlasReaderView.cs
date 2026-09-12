using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AiDe.Core.Understanding;
using ICSharpCode.AvalonEdit;

namespace AiDe.App.Workbench.Understanding;

/// <summary>
/// Native, read-only consumer for the Atlas query port. It owns no source I/O and asks only
/// <see cref="IAtlasQueries"/> for inventory, selection and receipt restore projections.
/// </summary>
public sealed class AtlasReaderView : UserControl
{
    public const int PageSize = 64;
    private readonly IAtlasQueries _queries;
    private readonly string _manifestToken;
    private readonly ObservableCollection<AtlasFileNode> _roots = [];
    private readonly ObservableCollection<OutlineRow> _outlineRows = [];
    private readonly Stack<BackFrame> _back = new();
    private readonly TreeView _files = new();
    private readonly ListBox _outline = new();
    private readonly TextEditor _source = new();
    private readonly Button _backButton = new() { Content = "Back", IsEnabled = false };
    private readonly Button _loadMore = new() { Content = "Load more", Visibility = Visibility.Collapsed };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _bounds = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _sourceStatus = new() { TextWrapping = TextWrapping.Wrap };
    private CancellationTokenSource? _requestBudget;
    private SelectionProjection? _current;
    private int _loadedRows;
    private long _requestSequence;

    public AtlasReaderView(IAtlasQueries queries, string manifestToken)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _manifestToken = Required(manifestToken, nameof(manifestToken));
        BuildChrome();
    }

    public IReadOnlyList<AtlasFileNode> FileRoots => _roots;
    public IReadOnlyList<OutlineRow> OutlineRows => _outlineRows;
    public IReadOnlyList<AtlasTextSpan> CurrentHighlights { get; private set; } = [];
    public string StatusText => _status.Text;
    public string BoundsText => _bounds.Text;
    public string SourceStatusText => _sourceStatus.Text;
    public string SourceText => _source.Text;
    public bool IsSourceReadOnly => _source.IsReadOnly;
    public bool CanGoBack => _backButton.IsEnabled;
    public bool CanLoadMore => _loadMore.Visibility == Visibility.Visible && _loadMore.IsEnabled;
    public Button BackButton => _backButton;
    public Button LoadMoreButton => _loadMore;
    public TreeView FilesControl => _files;
    public ListBox OutlineControl => _outline;
    public TextEditor SourceControl => _source;
    public TextBlock StatusControl => _status;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var (sequence, token) = BeginRequest(cancellationToken);
        _status.Text = "Loading repository files…";
        _bounds.Text = "Totals not recorded yet.";
        _loadMore.Visibility = Visibility.Collapsed;
        _loadedRows = 0;
        _roots.Clear();
        await LoadPageAsync(sequence, 0, token).ConfigureAwait(true);
    }

    public Task LoadMoreAsync(CancellationToken cancellationToken = default)
    {
        var (sequence, token) = BeginRequest(cancellationToken);
        _status.Text = "Loading more repository files…";
        return LoadPageAsync(sequence, _loadedRows, token);
    }

    public async Task SelectFileAsync(AtlasFileNode file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (!file.IsFile)
        {
            return;
        }

        PushCurrentFrame();
        var (sequence, token) = BeginRequest(cancellationToken);
        _status.Text = "Loading selection…";
        _outlineRows.Clear();
        await ApplySelectionAsync(
            sequence,
            _queries.SelectAsync(new SelectionRequest(_manifestToken, file.FileValue!, null, sequence), token),
            token).ConfigureAwait(true);
    }

    public async Task SelectDeclarationAsync(OutlineRow declaration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        PushCurrentFrame();
        var (sequence, token) = BeginRequest(cancellationToken);
        _status.Text = "Loading declaration…";
        await ApplySelectionAsync(
            sequence,
            _queries.SelectAsync(new SelectionRequest(_manifestToken, declaration.FileValue, declaration.ObservationKey, sequence), token),
            token).ConfigureAwait(true);
    }

    public async Task GoBackAsync(CancellationToken cancellationToken = default)
    {
        if (_back.Count == 0)
        {
            _status.Text = "Back unavailable: no prior Atlas receipt.";
            _backButton.IsEnabled = false;
            return;
        }

        var frame = _back.Pop();
        var (sequence, token) = BeginRequest(cancellationToken);
        _status.Text = "Restoring prior Atlas receipt…";
        _backButton.IsEnabled = _back.Count > 0;
        await ApplySelectionAsync(sequence, _queries.RestoreAsync(frame.ReceiptToken, sequence, token), token).ConfigureAwait(true);
        RestoreFocus(frame);
    }

    private void BuildChrome()
    {
        AutomationProperties.SetName(this, "Atlas native reader");
        SetResourceReference(BackgroundProperty, "SurfaceBrush");
        _status.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        _bounds.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        _sourceStatus.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        _backButton.SetResourceReference(ForegroundProperty, "TextBrush");
        _loadMore.SetResourceReference(ForegroundProperty, "TextBrush");
        AutomationProperties.SetName(_backButton, "Back to restored Atlas receipt");
        AutomationProperties.SetName(_loadMore, "Load more Atlas files");
        AutomationProperties.SetName(_files, "Atlas files");
        AutomationProperties.SetName(_outline, "Atlas member outline");
        AutomationProperties.SetName(_source, "Atlas source page read-only");
        AutomationProperties.SetName(_status, "Atlas reader status");
        AutomationProperties.SetName(_bounds, "Atlas pagination and bounds");
        AutomationProperties.SetName(_sourceStatus, "Atlas source state");

        _files.ItemsSource = _roots;
        _outline.ItemsSource = _outlineRows;
        _files.Focusable = true;
        _outline.Focusable = true;
        _source.Focusable = true;
        _source.IsReadOnly = true;
        _source.ShowLineNumbers = true;
        _source.FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace");
        _source.FontSize = 12.5;
        _source.WordWrap = false;
        _source.SetResourceReference(BackgroundProperty, "SurfaceSunkenBrush");
        _source.SetResourceReference(ForegroundProperty, "TextBrush");
        VirtualizingStackPanel.SetIsVirtualizing(_files, true);
        VirtualizingStackPanel.SetIsVirtualizing(_outline, true);

        _backButton.Click += (_, _) => _ = GoBackAsync();
        _loadMore.Click += (_, _) => _ = LoadMoreAsync();
        _files.SelectedItemChanged += (_, e) =>
        {
            if (e.NewValue is AtlasFileNode { IsFile: true } file)
            {
                _ = SelectFileAsync(file);
            }
        };
        _outline.MouseDoubleClick += (_, _) =>
        {
            if (_outline.SelectedItem is OutlineRow row)
            {
                _ = SelectDeclarationAsync(row);
            }
        };
        _outline.KeyDown += (_, e) =>
        {
            if (e.Key is Key.Enter && _outline.SelectedItem is OutlineRow row)
            {
                e.Handled = true;
                _ = SelectDeclarationAsync(row);
            }
        };

        var toolbar = new DockPanel { LastChildFill = true, Margin = new Thickness(8, 8, 8, 4) };
        DockPanel.SetDock(_backButton, Dock.Left);
        DockPanel.SetDock(_loadMore, Dock.Right);
        toolbar.Children.Add(_backButton);
        toolbar.Children.Add(_loadMore);
        toolbar.Children.Add(_status);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 180 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 180 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Star), MinWidth = 260 });
        Grid.SetColumn(_files, 0);
        Grid.SetColumn(_outline, 1);
        Grid.SetColumn(_source, 2);
        grid.Children.Add(_files);
        grid.Children.Add(_outline);
        grid.Children.Add(_source);

        var footer = new StackPanel { Margin = new Thickness(8, 4, 8, 8) };
        footer.Children.Add(_bounds);
        footer.Children.Add(_sourceStatus);

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(toolbar, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(toolbar);
        root.Children.Add(footer);
        root.Children.Add(grid);
        Content = root;
    }

    private async Task LoadPageAsync(long sequence, int offset, CancellationToken token)
    {
        try
        {
            var page = await _queries.InventoryAsync(new PageRequest(offset, PageSize), token).ConfigureAwait(true);
            if (!IsCurrent(sequence))
            {
                return;
            }

            foreach (var file in page.Files)
            {
                AddFile(file);
            }

            _loadedRows += page.Files.Length;
            _status.Text = page.Files.Length == 0 && offset == 0
                ? "No visible files in this scope."
                : $"Showing {_loadedRows} Atlas file rows.";
            _bounds.Text = BoundsTextFor(page.Bounds);
            _loadMore.Visibility = MoreAvailable(page.Bounds) ? Visibility.Visible : Visibility.Collapsed;
            _loadMore.IsEnabled = MoreAvailable(page.Bounds);
        }
        catch (OperationCanceledException) when (IsCurrent(sequence))
        {
            _status.Text = "Atlas inventory request canceled.";
        }
        catch (Exception ex) when (IsCurrent(sequence) && ex is not OutOfMemoryException)
        {
            _status.Text = "Atlas inventory failed: " + ex.Message;
            _loadMore.Visibility = Visibility.Collapsed;
        }
    }

    private async Task ApplySelectionAsync(long sequence, Task<SelectionProjection> pending, CancellationToken token)
    {
        try
        {
            var selection = await pending.ConfigureAwait(true);
            if (!IsCurrent(sequence))
            {
                return;
            }

            _current = selection;
            _outlineRows.Clear();
            foreach (var row in selection.Outline.Declarations.Select(d => new OutlineRow(selection.FileValue, d)))
            {
                _outlineRows.Add(row);
            }

            ApplySource(selection);
            _status.Text = SelectionStatus(selection);
            _bounds.Text = BoundsTextFor(selection.Bounds) + " " + CoverageText(selection.Coverage);
            _backButton.IsEnabled = _back.Count > 0;
        }
        catch (OperationCanceledException) when (IsCurrent(sequence))
        {
            ClearSource(SourceProjectionState.Canceled, "Atlas selection request canceled.");
        }
        catch (Exception ex) when (IsCurrent(sequence) && ex is not OutOfMemoryException)
        {
            ClearSource(SourceProjectionState.Unavailable, "Atlas selection failed: " + ex.Message);
        }
    }

    private void ApplySource(SelectionProjection selection)
    {
        if (selection.Source.State is not SourceProjectionState.IndexedMatch || selection.Source.Page is null)
        {
            ClearSource(selection.Source.State, NonMatchText(selection.Source.State, selection.Limitations));
            return;
        }

        var page = selection.Source.Page;
        CurrentHighlights = page.Highlights;
        _source.Text = page.Text;
        if (page.Highlights.Length > 0)
        {
            var first = page.Highlights[0];
            _source.Select(first.Start - page.PageSpan.Start, first.Length);
        }

        _sourceStatus.Text = $"Indexed source match; decoder {selection.Source.DecoderId}; page {page.PageSpan.Start}–{page.PageSpan.End}; highlights {page.Highlights.Length}.";
    }

    private void ClearSource(SourceProjectionState state, string text)
    {
        _source.Text = "";
        _source.Select(0, 0);
        CurrentHighlights = [];
        _sourceStatus.Text = text;
        _status.Text = SourceStatusLabel(state);
    }

    private void AddFile(AtlasFileEntry entry)
    {
        var parts = entry.RelativePath.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        var level = _roots;
        AtlasFileNode? current = null;
        for (var i = 0; i < parts.Length; i++)
        {
            var last = i == parts.Length - 1;
            current = level.FirstOrDefault(node => string.Equals(node.Name, parts[i], StringComparison.Ordinal) && node.IsFile == last);
            if (current is null)
            {
                current = last ? AtlasFileNode.File(parts[i], entry) : AtlasFileNode.Folder(parts[i], entry.RelativePath);
                level.Add(current);
            }

            level = current.Children;
        }
    }

    private (long Sequence, CancellationToken Token) BeginRequest(CancellationToken cancellationToken)
    {
        _requestBudget?.Cancel();
        _requestBudget?.Dispose();
        _requestBudget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        return (++_requestSequence, _requestBudget.Token);
    }

    private bool IsCurrent(long sequence) => sequence == _requestSequence;

    private void PushCurrentFrame()
    {
        if (_current is null)
        {
            return;
        }

        _back.Push(new BackFrame(_current.ReceiptToken, FocusName()));
        _backButton.IsEnabled = true;
    }

    private string FocusName() =>
        Keyboard.FocusedElement is DependencyObject focused
            ? AutomationProperties.GetName(focused)
            : "";

    private void RestoreFocus(BackFrame frame)
    {
        if (string.Equals(frame.FocusName, AutomationProperties.GetName(_outline), StringComparison.Ordinal))
        {
            _outline.Focus();
        }
        else if (string.Equals(frame.FocusName, AutomationProperties.GetName(_source), StringComparison.Ordinal))
        {
            _source.Focus();
        }
        else
        {
            _files.Focus();
        }
    }

    private static string BoundsTextFor(AtlasBounds bounds) =>
        bounds.TotalState switch
        {
            AtlasDenominatorState.Known => $"Returned {bounds.ReturnedRows} of {bounds.TotalCount} rows; limit {bounds.EffectiveLimit}/{bounds.RequestedLimit}; bytes {bounds.ReturnedBytes}.",
            AtlasDenominatorState.Unknown => $"Returned {bounds.ReturnedRows} rows; total not recorded ({bounds.OmissionReason}); limit {bounds.EffectiveLimit}/{bounds.RequestedLimit}; bytes {bounds.ReturnedBytes}.",
            AtlasDenominatorState.Withheld => $"Returned {bounds.ReturnedRows} rows; total withheld ({bounds.OmissionReason}); limit {bounds.EffectiveLimit}/{bounds.RequestedLimit}; bytes {bounds.ReturnedBytes}.",
            _ => throw new ArgumentOutOfRangeException(nameof(bounds), bounds.TotalState, "Unsupported denominator state."),
        };

    private static bool MoreAvailable(AtlasBounds bounds) =>
        bounds.TotalState is AtlasDenominatorState.Known
            ? bounds.ReturnedRows > 0 && bounds.ReturnedRows < bounds.TotalCount
            : !string.IsNullOrWhiteSpace(bounds.LimitingDimension);

    private static string CoverageText(SelectionCoverage coverage) =>
        coverage.State switch
        {
            AtlasDenominatorState.Known => $"Coverage {coverage.Value:P0}.",
            AtlasDenominatorState.Unknown => $"Coverage not recorded ({coverage.Reason}).",
            AtlasDenominatorState.Withheld => $"Coverage withheld ({coverage.Reason}).",
            _ => throw new ArgumentOutOfRangeException(nameof(coverage), coverage.State, "Unsupported coverage state."),
        };

    private static string SelectionStatus(SelectionProjection selection)
    {
        var limits = selection.Limitations.Length == 0 ? "" : " " + string.Join(" ", selection.Limitations);
        return $"Selected {selection.FileValue}; generation {selection.Generation}.{limits}";
    }

    private static string NonMatchText(SourceProjectionState state, IEnumerable<string> limitations)
    {
        var suffix = string.Join(" ", limitations);
        var text = state switch
        {
            SourceProjectionState.Changed => "Source changed; previous text and highlights cleared.",
            SourceProjectionState.Unavailable => "Source unavailable; previous text and highlights cleared.",
            SourceProjectionState.Unverifiable => "Source unverifiable; previous text and highlights cleared.",
            SourceProjectionState.UnsupportedEncoding => "Unsupported encoding; previous text and highlights cleared.",
            SourceProjectionState.TooLargeToVerify => "Project/TFM context not established.",
            SourceProjectionState.ReadUnstable => "Source read unstable; previous text and highlights cleared.",
            SourceProjectionState.Refused => "Source refused; previous text and highlights cleared.",
            SourceProjectionState.Canceled => "Source request canceled; previous text and highlights cleared.",
            SourceProjectionState.IndexedMatch => "",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported source state."),
        };

        return string.IsNullOrWhiteSpace(suffix) ? text : text + " " + suffix;
    }

    private static string SourceStatusLabel(SourceProjectionState state) => state switch
    {
        SourceProjectionState.IndexedMatch => "Source ready.",
        SourceProjectionState.Changed => "Source changed.",
        SourceProjectionState.Unavailable => "Source unavailable.",
        SourceProjectionState.Unverifiable => "Source unverifiable.",
        SourceProjectionState.UnsupportedEncoding => "Source unsupported.",
        SourceProjectionState.TooLargeToVerify => "Source file-limited.",
        SourceProjectionState.ReadUnstable => "Source unstable.",
        SourceProjectionState.Refused => "Source refused.",
        SourceProjectionState.Canceled => "Source canceled.",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported source state."),
    };

    private static string Required(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value must not be blank.", name) : value;

    private sealed record BackFrame(string ReceiptToken, string FocusName);
}

public sealed class AtlasFileNode
{
    private AtlasFileNode(string name, string relativePath, string? fileValue, bool isFile, string details)
    {
        Name = name;
        RelativePath = relativePath;
        FileValue = fileValue;
        IsFile = isFile;
        Details = details;
    }

    public string Name { get; }
    public string RelativePath { get; }
    public string? FileValue { get; }
    public bool IsFile { get; }
    public string Details { get; }
    public ObservableCollection<AtlasFileNode> Children { get; } = [];
    public string AccessibleName => IsFile ? $"{RelativePath}; {Details}" : $"{Name} folder";
    public override string ToString() => IsFile ? $"{Name} — {Details}" : Name;

    public static AtlasFileNode Folder(string name, string relativePath) =>
        new(name, relativePath, null, false, "folder");

    public static AtlasFileNode File(string name, AtlasFileEntry entry)
    {
        var limitation = string.IsNullOrWhiteSpace(entry.Reason) ? "" : "; " + entry.Reason;
        var details = $"{entry.Kind}; {entry.Classification}; {entry.Availability}{limitation}";
        return new(name, entry.RelativePath, entry.FileValue, true, details);
    }
}

public sealed class OutlineRow
{
    public OutlineRow(string fileValue, OutlineDeclaration declaration)
    {
        FileValue = fileValue;
        ObservationKey = declaration.ObservationKey;
        DisplayName = declaration.DisplayName;
        Kind = declaration.Kind;
        Span = declaration.Span;
    }

    public string FileValue { get; }
    public string ObservationKey { get; }
    public string DisplayName { get; }
    public AtlasDeclarationKind Kind { get; }
    public AtlasTextSpan Span { get; }
    public string AccessibleName => $"{Kind} {DisplayName} at {Span.Start}";
    public override string ToString() => $"{Kind} {DisplayName}";
}
