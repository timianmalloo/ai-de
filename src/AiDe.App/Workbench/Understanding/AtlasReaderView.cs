using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
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
    private readonly List<BackFrame> _back = [];
    private readonly TreeView _files = new();
    private readonly ListBox _outline = new();
    private readonly TextEditor _source = new();
    private readonly Button _backButton = new() { Content = "Back", IsEnabled = false };
    private readonly Button _loadMore = new() { Content = "Load more", Visibility = Visibility.Collapsed };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _bounds = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _sourceStatus = new() { TextWrapping = TextWrapping.Wrap };
    private CancellationTokenSource? _requestBudget;
    private string? _receiptToken;
    private string? _currentFile;
    private BackFrame? _pendingPrevious;
    private bool _historyEvicted;
    private bool _restoringControls;
    private bool _unloaded;
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
        _back.Clear();
        _historyEvicted = false;
        _backButton.IsEnabled = false;
        ClearPresentation();
        _pendingPrevious = null;
        _status.Text = "Loading repository files…";
        _bounds.Text = "Totals not recorded yet.";
        await LoadPageAsync(sequence, 0, token).ConfigureAwait(true);
    }

    public Task LoadMoreAsync(CancellationToken cancellationToken = default)
    {
        if (!CanLoadMore)
        {
            return Task.CompletedTask;
        }

        var (sequence, token) = BeginRequest(cancellationToken);
        _status.Text = "Loading more repository files…";
        _loadMore.IsEnabled = false;
        return LoadPageAsync(sequence, _loadedRows, token);
    }

    public async Task SelectFileAsync(AtlasFileNode file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (!file.IsFile)
        {
            return;
        }

        var previous = CaptureFrame();
        var (sequence, token) = BeginRequest(cancellationToken);
        ClearPresentation();
        _status.Text = "Loading selection…";
        await ApplySelectionAsync(
            sequence,
            () => _queries.SelectAsync(new SelectionRequest(_manifestToken, file.FileValue!, null, sequence), token),
            token, previous).ConfigureAwait(true);
    }

    public async Task SelectDeclarationAsync(OutlineRow declaration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        var previous = CaptureFrame();
        var (sequence, token) = BeginRequest(cancellationToken);
        ClearPresentation();
        _status.Text = "Loading declaration…";
        await ApplySelectionAsync(
            sequence,
            () => _queries.SelectAsync(new SelectionRequest(_manifestToken, declaration.FileValue, declaration.ObservationKey, sequence), token),
            token, previous).ConfigureAwait(true);
    }

    public async Task GoBackAsync(CancellationToken cancellationToken = default)
    {
        if (_back.Count == 0)
        {
            _status.Text = _historyEvicted
                ? "Back unavailable: older receipts were evicted (50-frame limit)."
                : "Back unavailable: no prior Atlas receipt.";
            _backButton.IsEnabled = false;
            return;
        }

        var frame = _back[^1];
        var (sequence, token) = BeginRequest(cancellationToken);
        ClearPresentation();
        _status.Text = "Restoring prior Atlas receipt…";
        if (await ApplySelectionAsync(sequence, () => _queries.RestoreAsync(frame.ReceiptToken, sequence, token), token, null).ConfigureAwait(true))
        {
            _back.RemoveAt(_back.Count - 1);
            _backButton.IsEnabled = _back.Count > 0;
            RestoreViewState(frame);
        }
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
        var label = new FrameworkElementFactory(typeof(TextBlock));
        label.SetBinding(TextBlock.TextProperty, new Binding(nameof(AtlasFileNode.Name)));
        _files.ItemTemplate = new HierarchicalDataTemplate(typeof(AtlasFileNode))
        {
            ItemsSource = new Binding(nameof(AtlasFileNode.Children)),
            VisualTree = label,
        };
        _files.ItemContainerStyle = ItemStyle(typeof(TreeViewItem), nameof(AtlasFileNode.AccessibleName));
        _outline.ItemContainerStyle = ItemStyle(typeof(ListBoxItem), nameof(OutlineRow.AccessibleName));
        _outline.ItemsSource = _outlineRows;
        _files.Focusable = true;
        _outline.Focusable = true;
        _source.Focusable = true;
        _source.IsReadOnly = true;
        _source.ShowLineNumbers = true;
        _source.FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace");
        _source.FontSize = 13;
        _source.WordWrap = false;
        _source.SetResourceReference(BackgroundProperty, "SurfaceSunkenBrush");
        _source.SetResourceReference(ForegroundProperty, "TextBrush");
        VirtualizingStackPanel.SetIsVirtualizing(_files, true);
        VirtualizingStackPanel.SetIsVirtualizing(_outline, true);
        VirtualizingPanel.SetVirtualizationMode(_files, VirtualizationMode.Recycling);
        VirtualizingPanel.SetVirtualizationMode(_outline, VirtualizationMode.Recycling);
        ScrollViewer.SetCanContentScroll(_files, true);
        ScrollViewer.SetCanContentScroll(_outline, true);
        _files.SetResourceReference(BackgroundProperty, "SurfaceBrush");
        _files.SetResourceReference(ForegroundProperty, "TextBrush");
        _outline.SetResourceReference(BackgroundProperty, "SurfaceBrush");
        _outline.SetResourceReference(ForegroundProperty, "TextBrush");
        KeyboardNavigation.SetTabNavigation(_files, KeyboardNavigationMode.Continue);
        KeyboardNavigation.SetTabNavigation(_outline, KeyboardNavigationMode.Continue);

        _backButton.Click += async (_, _) => await GoBackAsync();
        _loadMore.Click += async (_, _) => await LoadMoreAsync();
        _files.SelectedItemChanged += async (_, e) =>
        {
            if (!_restoringControls && e.NewValue is AtlasFileNode { IsFile: true } file)
            {
                await SelectFileAsync(file);
            }
        };
        _outline.MouseDoubleClick += async (_, _) =>
        {
            if (_outline.SelectedItem is OutlineRow row)
            {
                await SelectDeclarationAsync(row);
            }
        };
        _outline.KeyDown += async (_, e) =>
        {
            if (e.Key is Key.Enter && _outline.SelectedItem is OutlineRow row)
            {
                e.Handled = true;
                await SelectDeclarationAsync(row);
            }
        };
        Loaded += (_, _) => _unloaded = false;
        Unloaded += (_, _) =>
        {
            _unloaded = true;
            ++_requestSequence;
            _requestBudget?.Cancel();
            _requestBudget?.Dispose();
            _requestBudget = null;
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
        var started = Stopwatch.GetTimestamp();
        try
        {
            var page = await _queries.InventoryAsync(new PageRequest(offset, PageSize), token).ConfigureAwait(true);
            if (!IsCurrent(sequence))
            {
                return;
            }

            token.ThrowIfCancellationRequested();
            foreach (var file in page.Files)
            {
                AddFile(file);
            }

            _loadedRows = checked(page.Request.Offset + page.Files.Length);
            _status.Text = page.Files.Length == 0 && offset == 0
                ? "No visible files in this scope."
                : $"Showing {_loadedRows} Atlas file rows.";
            _bounds.Text = BoundsTextFor(page.Bounds) + (page.Bounds.TotalState is AtlasDenominatorState.Known
                ? "" : " Further pages unavailable: continuation not supplied.");
            _loadMore.Visibility = MoreAvailable(page) ? Visibility.Visible : Visibility.Collapsed;
            _loadMore.IsEnabled = MoreAvailable(page);
        }
        catch (OperationCanceledException)
        {
            if (IsCurrent(sequence))
            {
                _status.Text = "ATLAS-READER-CANCELED: inventory request canceled.";
                _loadMore.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Trace.TraceError("code=ATLAS-READER-INVENTORY sequence={0} exception.type={1}", sequence, ex.GetType().FullName);
            if (IsCurrent(sequence))
            {
                _status.Text = "ATLAS-READER-INVENTORY: inventory unavailable.";
                _loadMore.Visibility = Visibility.Collapsed;
            }
        }
        finally
        {
            Trace.TraceInformation("operation=atlas.inventory sequence={0} duration_ms={1} current={2}",
                sequence, Stopwatch.GetElapsedTime(started).TotalMilliseconds, IsCurrent(sequence));
        }
    }

    private async Task<bool> ApplySelectionAsync(long sequence, Func<Task<SelectionProjection>> query, CancellationToken token, BackFrame? previous)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            var selection = await query().ConfigureAwait(true);
            if (!IsCurrent(sequence))
            {
                return false;
            }

            token.ThrowIfCancellationRequested();
            _outlineRows.Clear();
            foreach (var row in selection.Outline.Declarations.Select(d => new OutlineRow(selection.FileValue, d)))
            {
                _outlineRows.Add(row);
            }

            ApplySource(selection);
            _receiptToken = selection.ReceiptToken;
            _currentFile = selection.FileValue;
            _pendingPrevious = null;
            if (previous is not null)
            {
                if (_back.Count == 50)
                {
                    _back.RemoveAt(0);
                    _historyEvicted = true;
                }

                _back.Add(previous);
            }

            _status.Text = SelectionStatus(selection);
            _bounds.Text = BoundsTextFor(selection.Bounds) + " " + CoverageText(selection.Coverage);
            _backButton.IsEnabled = _back.Count > 0;
            return true;
        }
        catch (OperationCanceledException)
        {
            if (IsCurrent(sequence))
            {
                ClearSource(SourceProjectionState.Canceled, "ATLAS-READER-CANCELED: selection request canceled.");
                _status.Text = _sourceStatus.Text;
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Trace.TraceError("code=ATLAS-READER-SELECTION sequence={0} exception.type={1}", sequence, ex.GetType().FullName);
            if (IsCurrent(sequence))
            {
                ClearSource(SourceProjectionState.Unavailable, "ATLAS-READER-SELECTION: selection unavailable.");
                _status.Text = _sourceStatus.Text;
            }
        }
        finally
        {
            Trace.TraceInformation("operation=atlas.selection sequence={0} duration_ms={1} current={2}",
                sequence, Stopwatch.GetElapsedTime(started).TotalMilliseconds, IsCurrent(sequence));
        }

        return false;
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
        for (var i = 0; i < parts.Length; i++)
        {
            var last = i == parts.Length - 1;
            var path = string.Join("\\", parts.Take(i + 1));
            var isFile = last && entry.Kind is AtlasDirectoryEntryKind.File;
            var current = level.FirstOrDefault(node => string.Equals(node.RelativePath, path, StringComparison.Ordinal)
                && (!isFile || string.Equals(node.FileValue, entry.FileValue, StringComparison.Ordinal)));
            if (current is null)
            {
                current = last ? AtlasFileNode.File(parts[i], entry) : AtlasFileNode.Folder(parts[i], path);
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

    private bool IsCurrent(long sequence) => sequence == _requestSequence && !_unloaded;

    private void ClearPresentation()
    {
        _pendingPrevious = CaptureFrame();
        _receiptToken = null;
        _currentFile = null;
        _outlineRows.Clear();
        ClearSource(SourceProjectionState.Unavailable, "Source cleared while the requested selection loads.");
        _bounds.Text = "Selection bounds not recorded yet.";
    }

    private BackFrame? CaptureFrame() => _receiptToken is null ? _pendingPrevious : new(
        _receiptToken, _currentFile!,
        (_outline.SelectedItem as OutlineRow)?.ObservationKey,
        _outline.IsKeyboardFocusWithin ? "outline" : _source.IsKeyboardFocusWithin ? "source" : "files",
        _source.SelectionStart, _source.SelectionLength, _source.VerticalOffset, _source.HorizontalOffset,
        FindVisual<ScrollViewer>(_outline)?.VerticalOffset ?? 0,
        FindVisual<ScrollViewer>(_files)?.VerticalOffset ?? 0);

    private void RestoreViewState(BackFrame frame)
    {
        _restoringControls = true;
        try
        {
            SelectFileContainer(_files, frame.FileValue);
            _outline.SelectedItem = _outlineRows.FirstOrDefault(row => row.ObservationKey == frame.ObservationKey);
            UpdateLayout();
            FindVisual<ScrollViewer>(_outline)?.ScrollToVerticalOffset(frame.OutlineOffset);
            FindVisual<ScrollViewer>(_files)?.ScrollToVerticalOffset(frame.FilesOffset);
            _source.Select(Math.Min(frame.SelectionStart, _source.Text.Length),
                Math.Min(frame.SelectionLength, Math.Max(0, _source.Text.Length - frame.SelectionStart)));
            _source.ScrollToVerticalOffset(frame.VerticalOffset);
            _source.ScrollToHorizontalOffset(frame.HorizontalOffset);
            if (frame.FocusName == "outline")
            {
                if (_outline.SelectedItem is { } row &&
                    _outline.ItemContainerGenerator.ContainerFromItem(row) is ListBoxItem item)
                    item.Focus();
                else
                    _outline.Focus();
            }
            else if (frame.FocusName == "source")
                _source.TextArea.Focus();
            else if (SelectedFileContainer(_files) is { } file)
                file.Focus();
            else
                _files.Focus();
        }
        finally
        {
            _restoringControls = false;
        }
    }

    private static Style ItemStyle(Type type, string nameProperty)
    {
        var style = new Style(type);
        style.Setters.Add(new Setter(AutomationProperties.NameProperty, new Binding(nameProperty)));
        style.Setters.Add(new Setter(ToolTipProperty, new Binding(nameProperty)));
        style.Setters.Add(new Setter(VirtualizingPanel.IsVirtualizingProperty, true));
        style.Setters.Add(new Setter(VirtualizingPanel.VirtualizationModeProperty, VirtualizationMode.Recycling));
        style.Setters.Add(new Setter(ForegroundProperty, new DynamicResourceExtension("TextBrush")));
        var focus = new Trigger { Property = IsKeyboardFocusWithinProperty, Value = true };
        focus.Setters.Add(new Setter(BorderBrushProperty, SystemColors.HighlightBrush));
        focus.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(2)));
        style.Triggers.Add(focus);
        return style;
    }

    private static T? FindVisual<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) return match;
            if (FindVisual<T>(child) is { } nested) return nested;
        }

        return null;
    }

    private static TreeViewItem? SelectedFileContainer(ItemsControl parent)
    {
        foreach (var row in parent.Items)
        {
            if (parent.ItemContainerGenerator.ContainerFromItem(row) is not TreeViewItem item) continue;
            if (item.IsSelected) return item;
            if (SelectedFileContainer(item) is { } child) return child;
        }

        return null;
    }

    private static bool ContainsFile(AtlasFileNode node, string fileValue) =>
        node.FileValue == fileValue || node.Children.Any(child => ContainsFile(child, fileValue));

    private static void SelectFileContainer(ItemsControl parent, string fileValue)
    {
        foreach (AtlasFileNode node in parent.Items)
        {
            if (!ContainsFile(node, fileValue)) continue;
            parent.UpdateLayout();
            if (parent.ItemContainerGenerator.ContainerFromItem(node) is not TreeViewItem item) return;
            if (node.FileValue == fileValue)
            {
                item.IsSelected = true;
                return;
            }

            item.IsExpanded = true;
            item.UpdateLayout();
            SelectFileContainer(item, fileValue);
            return;
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

    private static bool MoreAvailable(InventoryPage page) =>
        page.Bounds.TotalState is AtlasDenominatorState.Known && page.Files.Length > 0
        && (long)page.Request.Offset + page.Files.Length < page.Bounds.TotalCount;

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
            SourceProjectionState.TooLargeToVerify => "Source exceeds the verification budget; previous text and highlights cleared.",
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
        SourceProjectionState.TooLargeToVerify => "Source exceeds verification budget.",
        SourceProjectionState.ReadUnstable => "Source unstable.",
        SourceProjectionState.Refused => "Source refused.",
        SourceProjectionState.Canceled => "Source canceled.",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported source state."),
    };

    private static string Required(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value must not be blank.", name) : value;

    private sealed record BackFrame(string ReceiptToken, string FileValue, string? ObservationKey, string FocusName,
        int SelectionStart, int SelectionLength, double VerticalOffset, double HorizontalOffset,
        double OutlineOffset, double FilesOffset);
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
        return new(name, entry.RelativePath.Replace('/', '\\'),
            entry.Kind is AtlasDirectoryEntryKind.File ? entry.FileValue : null,
            entry.Kind is AtlasDirectoryEntryKind.File, details);
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
