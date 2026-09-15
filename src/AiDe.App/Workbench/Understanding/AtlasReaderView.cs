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
/// Native, read-only consumer for Atlas projections. The proof constructor retains
/// <see cref="IAtlasQueries"/>; the production constructor consumes a Core-owned reader lease.
/// </summary>
public sealed class AtlasReaderView : UserControl
{
    public const int PageSize = 64;
    public const int OutlinePageSize = 128;
    private const int SourceWindowLength = 32768;
    private readonly IAtlasQueries? _queries;
    private readonly IAtlasReaderLease? _lease;
    private readonly AtlasWorkspaceOwner? _owner;
    private readonly Dictionary<string, AtlasFileNode> _readerFiles = new(StringComparer.Ordinal);
    private string _manifestToken;
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
    private readonly AtlasStaticView _staticView = new();
    private readonly Grid _readerGrid = new();
    private readonly Button _sourceMode = new() { Content = "Source" };
    private readonly Button _classMode = new() { Content = "Class view" };
    private readonly Button _nextOutline = new() { Content = "Next declarations", IsEnabled = false };
    private readonly Button _nextSource = new() { Content = "Next source page", IsEnabled = false };
    private AtlasSelectionDto? _readerSelection;
    private int _outlinePageOffset;
    private int _sourcePageOffset;
    private int _sourcePageLength = SourceWindowLength;
    private string? _selectedDeclarationToken;
    private CancellationTokenSource? _requestBudget;
    private string? _receiptToken;
    private string? _currentFile;
    private BackFrame? _pendingPrevious;
    private bool _historyEvicted;
    private bool _restoringControls;
    private bool _unloaded;
    private int _loadedRows;
    private int? _nextOffset;
    private long _requestSequence;

    public AtlasReaderView(IAtlasQueries queries, string manifestToken)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _manifestToken = Required(manifestToken, nameof(manifestToken));
        BuildChrome();
    }

    public AtlasReaderView(IAtlasReaderLease lease) : this(lease, null) { }

    internal AtlasReaderView(IAtlasReaderLease lease, AtlasWorkspaceOwner? owner)
    {
        _lease = lease ?? throw new ArgumentNullException(nameof(lease));
        _owner = owner;
        _manifestToken = Required(lease.InitialManifestToken, nameof(lease));
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
    public AtlasPresentationMode Presentation { get; private set; }
    public AtlasStaticView StaticView => _staticView;
    public Button ClassViewButton => _classMode;
    public Button SourceViewButton => _sourceMode;
    public Button NextDeclarationsButton => _nextOutline;
    public Button NextSourceButton => _nextSource;
    public AtlasSelectionDto? CurrentSelection => _readerSelection;
    public string? CurrentBindingToken { get; private set; }
    public int OutlinePageOffset => _outlinePageOffset;
    public Task CurrentOperation { get; private set; } = Task.CompletedTask;

    public async Task SetPresentationAsync(AtlasPresentationMode presentation, CancellationToken cancellationToken = default)
    {
        if (Presentation == presentation) return;
        var previous = CaptureFrame();
        SetPresentation(presentation);
        if (presentation == AtlasPresentationMode.Source) return;
        if (_lease is null)
        {
            _staticView.ShowState("Structure unavailable. This reader returned no admitted structural metadata.");
            return;
        }
        if (_currentFile is not { } file)
        {
            _staticView.ShowState("Select a file to inspect its Class view.");
            return;
        }
        var request = ReaderRequest(file, _selectedDeclarationToken, _sourcePageOffset, _sourcePageLength, _outlinePageOffset);
        var (sequence, token) = BeginRequest(cancellationToken);
        ClearPresentation();
        await ApplyReaderSelectionAsync(sequence, () => _lease.Queries.SelectAsync(request, token),
            token, previous, request.DeclarationToken, request).ConfigureAwait(true);
    }

    public Task LoadNextOutlinePageAsync(CancellationToken cancellationToken = default) =>
        LoadReaderContinuationAsync(source: false, cancellationToken);

    public Task LoadNextSourcePageAsync(CancellationToken cancellationToken = default) =>
        LoadReaderContinuationAsync(source: true, cancellationToken);

    private async Task LoadReaderContinuationAsync(bool source, CancellationToken cancellationToken)
    {
        var offset = source ? _readerSelection?.Source.NextOffset : _readerSelection?.OutlineNextOffset;
        if (_lease is null || _currentFile is not { } file || offset is null)
        {
            _status.Text = source ? "No further retained source page." : "No further retained declaration page.";
            return;
        }
        var previous = CaptureFrame();
        var request = ReaderRequest(file, null, source ? offset.Value : _sourcePageOffset,
            source ? SourceWindowLength : _sourcePageLength, source ? _outlinePageOffset : offset.Value);
        var (sequence, token) = BeginRequest(cancellationToken);
        ClearPresentation();
        await ApplyReaderSelectionAsync(sequence, () => _lease.Queries.SelectAsync(request, token),
            token, previous, request: request).ConfigureAwait(true);
    }

    private void SetPresentation(AtlasPresentationMode presentation)
    {
        Presentation = presentation;
        _readerGrid.ColumnDefinitions[1].MinWidth = presentation == AtlasPresentationMode.Source ? 180 : 0;
        _readerGrid.ColumnDefinitions[2].MinWidth = presentation == AtlasPresentationMode.Source ? 260 : 0;
        _source.Visibility = _outline.Visibility =
            presentation == AtlasPresentationMode.Source ? Visibility.Visible : Visibility.Collapsed;
        _staticView.Visibility = presentation == AtlasPresentationMode.Class ? Visibility.Visible : Visibility.Collapsed;
        _sourceMode.IsEnabled = presentation != AtlasPresentationMode.Source;
        _classMode.IsEnabled = presentation != AtlasPresentationMode.Class;
        AutomationProperties.SetItemStatus(_sourceMode, presentation == AtlasPresentationMode.Source ? "Selected" : "Not selected");
        AutomationProperties.SetItemStatus(_classMode, presentation == AtlasPresentationMode.Class ? "Selected" : "Not selected");
    }

    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        CurrentOperation = LoadCoreAsync(cancellationToken);
        return CurrentOperation;
    }

    private async Task LoadCoreAsync(CancellationToken cancellationToken)
    {
        var (sequence, token) = BeginRequest(cancellationToken);
        _status.Text = "Loading repository files…";
        _bounds.Text = "Totals not recorded yet.";
        _loadMore.Visibility = Visibility.Collapsed;
        _loadedRows = 0;
        _nextOffset = null;
        _roots.Clear();
        _readerFiles.Clear();
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
        if (!CanLoadMore || _nextOffset is not { } offset)
        {
            return Task.CompletedTask;
        }

        var (sequence, token) = BeginRequest(cancellationToken);
        _status.Text = "Loading more repository files…";
        _loadMore.IsEnabled = false;
        return LoadPageAsync(sequence, offset, token);
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
        if (_lease is not null)
        {
            var request = ReaderRequest(file.FileValue!, null);
            await ApplyReaderSelectionAsync(sequence,
                () => _lease.Queries.SelectAsync(request, token),
                token, previous, request: request).ConfigureAwait(true);
            return;
        }
        await ApplySelectionAsync(
            sequence,
            () => _queries!.SelectAsync(new SelectionRequest(_manifestToken, file.FileValue!, null, sequence), token),
            token, previous).ConfigureAwait(true);
    }

    public async Task SelectDeclarationAsync(OutlineRow declaration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        var previous = CaptureFrame();
        var outlineOffset = _outlinePageOffset;
        var (sequence, token) = BeginRequest(cancellationToken);
        ClearPresentation();
        _status.Text = "Loading declaration…";
        if (_lease is not null)
        {
            var request = ReaderRequest(declaration.FileValue, declaration.ObservationKey,
                declaration.Span.Start, Math.Min(SourceWindowLength, declaration.Span.Length), outlineOffset);
            await ApplyReaderSelectionAsync(sequence,
                () => _lease.Queries.SelectAsync(request, token),
                token, previous, declaration.ObservationKey, request).ConfigureAwait(true);
            return;
        }
        await ApplySelectionAsync(
            sequence,
            () => _queries!.SelectAsync(new SelectionRequest(_manifestToken, declaration.FileValue, declaration.ObservationKey, sequence), token),
            token, previous, declaration.FileValue, declaration.ObservationKey).ConfigureAwait(true);
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
        var restored = _lease is not null
            ? await ApplyReaderSelectionAsync(sequence,
                () => _lease.Queries.RestoreAsync(new(1, _lease.ScopeToken, _lease.CoreEpoch, frame.ReceiptToken), token),
                token, null, frame.ObservationKey).ConfigureAwait(true)
            : await ApplySelectionAsync(sequence, () => _queries!.RestoreAsync(frame.ReceiptToken, sequence, token),
                token, null, frame.FileValue, frame.ObservationKey).ConfigureAwait(true);
        if (restored
            && IsCurrent(sequence) && !token.IsCancellationRequested)
        {
            _back.RemoveAt(_back.Count - 1);
            _backButton.IsEnabled = _back.Count > 0;
            _outlinePageOffset = frame.OutlinePageOffset;
            _sourcePageOffset = frame.SourceOffset;
            _sourcePageLength = frame.SourceLength;
            SetPresentation(frame.Presentation);
            if (_readerSelection is { } selection)
                _staticView.Render(AtlasStaticViewProjection.Create(selection, frame.ClassifierToken));
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
        AutomationProperties.SetName(_classMode, "Class view, file-local classifier occurrences");
        AutomationProperties.SetName(_sourceMode, "Source view");
        AutomationProperties.SetName(_nextOutline, "Next declarations page");
        AutomationProperties.SetName(_nextSource, "Next verified source page");
        foreach (var button in new[] { _sourceMode, _classMode, _nextOutline, _nextSource })
        {
            button.SetResourceReference(ForegroundProperty, "TextBrush");
            button.SetResourceReference(BackgroundProperty, "SurfaceBrush");
        }
        _sourceMode.Click += async (_, _) => await RunEventAsync(() => SetPresentationAsync(AtlasPresentationMode.Source));
        _classMode.Click += async (_, _) => await RunEventAsync(() => SetPresentationAsync(AtlasPresentationMode.Class));
        _nextOutline.Click += async (_, _) => await RunEventAsync(() => LoadNextOutlinePageAsync());
        _nextSource.Click += async (_, _) => await RunEventAsync(() => LoadNextSourcePageAsync());
        _staticView.ClassifierChanged += token =>
        {
            if (_readerSelection is { } selection)
                _staticView.Render(AtlasStaticViewProjection.Create(selection, token));
        };
        _staticView.DeclarationActivated += async (occurrence, _) => await RunEventAsync(async () =>
        {
            if (_readerSelection is not { } selection
                || !selection.Outline.Any(row => row.DeclarationToken == occurrence.Token)) return;
            var before = _requestSequence;
            await SelectDeclarationAsync(new OutlineRow(selection.FileToken, occurrence.Declaration)).ConfigureAwait(true);
            if (_requestSequence == before + 1 && _readerSelection?.DeclarationToken == occurrence.Token
                && CurrentBindingToken is not null)
            {
                SetPresentation(AtlasPresentationMode.Source);
                _source.TextArea.Focus();
            }
        });

        _files.ItemsSource = _roots;
        var label = new FrameworkElementFactory(typeof(TextBlock));
        label.SetBinding(TextBlock.TextProperty, new Binding(nameof(AtlasFileNode.Name)));
        _files.ItemTemplate = new HierarchicalDataTemplate(typeof(AtlasFileNode))
        {
            ItemsSource = new Binding(nameof(AtlasFileNode.Children)),
            VisualTree = label,
        };
        _files.ItemContainerStyle = ItemStyle(typeof(TreeViewItem), nameof(AtlasFileNode.AccessibleName));
        var outlineStyle = ItemStyle(typeof(ListBoxItem), nameof(OutlineRow.AccessibleName));
        outlineStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        _outline.ItemContainerStyle = outlineStyle;
        var outlineLabel = new FrameworkElementFactory(typeof(TextBlock));
        outlineLabel.SetBinding(TextBlock.TextProperty, new Binding());
        outlineLabel.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        outlineLabel.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.None);
        _outline.ItemTemplate = new DataTemplate(typeof(OutlineRow)) { VisualTree = outlineLabel };
        ScrollViewer.SetHorizontalScrollBarVisibility(_outline, ScrollBarVisibility.Disabled);
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

        _backButton.Click += async (_, _) => await RunEventAsync(() => GoBackAsync());
        _loadMore.Click += async (_, _) => await RunEventAsync(() => LoadMoreAsync());
        _files.SelectedItemChanged += async (_, e) =>
        {
            if (!_restoringControls && e.NewValue is AtlasFileNode { IsFile: true } file)
            {
                await RunEventAsync(() => SelectFileAsync(file));
            }
        };
        _outline.MouseDoubleClick += async (_, _) =>
        {
            if (_outline.SelectedItem is OutlineRow row)
            {
                await RunEventAsync(() => SelectDeclarationAsync(row));
            }
        };
        _outline.KeyDown += async (_, e) =>
        {
            if (e.Key is Key.Enter && _outline.SelectedItem is OutlineRow row)
            {
                e.Handled = true;
                await RunEventAsync(() => SelectDeclarationAsync(row));
            }
        };
        Loaded += (_, _) => _unloaded = false;
        Unloaded += (_, _) =>
        {
            if (_lease is not null)
            {
                Deactivate();
                return;
            }

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

        var grid = _readerGrid;
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 180 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Star) });
        Grid.SetColumn(_files, 0);
        Grid.SetColumn(_outline, 1);
        Grid.SetColumn(_source, 2);
        grid.Children.Add(_files);
        grid.Children.Add(_outline);
        grid.Children.Add(_source);
        Grid.SetColumn(_staticView, 1);
        Grid.SetColumnSpan(_staticView, 2);
        grid.Children.Add(_staticView);
        SetPresentation(AtlasPresentationMode.Source);

        var footer = new StackPanel { Margin = new Thickness(8, 4, 8, 8) };
        footer.Children.Add(_bounds);
        footer.Children.Add(_sourceStatus);

        var root = new DockPanel { LastChildFill = true };
        var presentationBar = new WrapPanel { Margin = new Thickness(8, 4, 8, 4) };
        foreach (var button in new[] { _sourceMode, _classMode, _nextOutline, _nextSource })
            presentationBar.Children.Add(button);
        DockPanel.SetDock(presentationBar, Dock.Top);
        root.Children.Add(presentationBar);
        DockPanel.SetDock(toolbar, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(toolbar);
        root.Children.Add(footer);
        root.Children.Add(grid);
        Content = root;
    }

    private async Task LoadPageAsync(long sequence, int offset, CancellationToken token)
    {
        if (_lease is not null)
        {
            await LoadReaderPageAsync(sequence, offset, token).ConfigureAwait(true);
            return;
        }

        var started = Stopwatch.GetTimestamp();
        try
        {
            var page = await _queries!.InventoryAsync(new PageRequest(offset, PageSize), token).ConfigureAwait(true);
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
            _nextOffset = page.NextOffset;
            _status.Text = page.Files.Length == 0 && offset == 0
                ? "No visible files in this scope."
                : $"Showing {_loadedRows} Atlas file rows.";
            _bounds.Text = BoundsTextFor(page.Bounds) + (page.NextOffset is null
                ? " No further retained page; this does not establish scope completeness." : "");
            _loadMore.Visibility = page.NextOffset.HasValue ? Visibility.Visible : Visibility.Collapsed;
            _loadMore.IsEnabled = page.NextOffset.HasValue;
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

    private async Task<bool> ApplySelectionAsync(long sequence, Func<Task<SelectionProjection>> query,
        CancellationToken token, BackFrame? previous, string? selectedFileValue = null, string? selectedObservationKey = null)
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
            if (selection.Source.State is SourceProjectionState.IndexedMatch
                && ((selectedFileValue is not null && selection.FileValue != selectedFileValue)
                    || (selectedObservationKey is not null
                        && !selection.Outline.Declarations.Any(row => row.ObservationKey == selectedObservationKey))))
            {
                ClearSource(SourceProjectionState.Unavailable,
                    "ATLAS-READER-OUTLINE: requested selection unavailable in the returned outline.");
                _status.Text = _sourceStatus.Text;
                return false;
            }

            _outlineRows.Clear();
            foreach (var row in selection.Outline.Declarations.Select(d => new OutlineRow(selection.FileValue, d)))
            {
                _outlineRows.Add(row);
            }

            ApplySource(selection);
            _status.Text = SelectionStatus(selection);
            _bounds.Text = BoundsTextFor(selection.Bounds) + " " + CoverageText(selection.Coverage);
            if (!IsCurrent(sequence) || token.IsCancellationRequested
                || selection.Source.State is not SourceProjectionState.IndexedMatch || selection.Source.Page is null)
            {
                return false;
            }

            if (selectedObservationKey is not null)
            {
                _outline.SelectedItem = _outlineRows.FirstOrDefault(row =>
                    row.FileValue == selectedFileValue && row.ObservationKey == selectedObservationKey);
            }

            if (!IsCurrent(sequence) || token.IsCancellationRequested)
            {
                return false;
            }

            _manifestToken = selection.ManifestToken;
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
        CurrentBindingToken = null;
        _readerSelection = null;
        _staticView.ShowState(state switch
        {
            SourceProjectionState.Changed => "Source changed — refresh required",
            SourceProjectionState.Refused => "Source access refused",
            _ => text,
        });
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
        _requestBudget = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _lease?.Invalidated ?? CancellationToken.None, _owner?.Token ?? CancellationToken.None);
        return (++_requestSequence, _requestBudget.Token);
    }

    private bool IsCurrent(long sequence) => sequence == _requestSequence && !_unloaded
        && _lease?.IsTerminal != true && _lease?.Invalidated.IsCancellationRequested != true;

    internal void Deactivate()
    {
        _unloaded = true;
        ++_requestSequence;
        _requestBudget?.Cancel();
        _requestBudget?.Dispose();
        _requestBudget = null;
        ClearPresentation();
        _pendingPrevious = null;
        _back.Clear();
        _backButton.IsEnabled = false;
    }

    private async Task RunEventAsync(Func<Task> operation)
    {
        try
        {
            var task = operation();
            CurrentOperation = _owner?.Track(task) ?? task;
            await CurrentOperation.ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Trace.TraceError("code=ATLAS-READER-EVENT exception.type={0}", ex.GetType().FullName);
            ClearSource(SourceProjectionState.Unavailable, "ATLAS-READER-EVENT: operation unavailable.");
        }
    }

    private AtlasSelectRequestDto ReaderRequest(string file, string? declaration, int sourceOffset = 0,
        int sourceLength = SourceWindowLength, int outlineOffset = 0) =>
        new(1, _lease!.ScopeToken, _lease.CoreEpoch, _manifestToken, file, declaration,
            sourceOffset, sourceLength, outlineOffset, OutlinePageSize,
            Presentation == AtlasPresentationMode.Class ? true : null);

    private async Task LoadReaderPageAsync(long sequence, int offset, CancellationToken token)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            var page = await _lease!.Queries.InventoryAsync(
                new(1, _lease.ScopeToken, _lease.CoreEpoch, _manifestToken, offset, PageSize), token).ConfigureAwait(true);
            if (!IsCurrent(sequence) || token.IsCancellationRequested) return;
            foreach (var entry in page.Files) _readerFiles[entry.FileToken] = AtlasFileNode.ReaderEntry(entry);
            _roots.Clear();
            foreach (var node in _readerFiles.Values) node.Children.Clear();
            foreach (var node in _readerFiles.Values)
            {
                if (node.ParentToken is { } parent && _readerFiles.TryGetValue(parent, out var folder))
                    folder.Children.Add(node);
                else _roots.Add(node);
            }
            _manifestToken = page.ManifestToken;
            _loadedRows = offset + page.Files.Length;
            _nextOffset = page.NextOffset;
            _status.Text = $"{page.Completion}; showing {_loadedRows} metadata rows. {string.Join(" ", page.Disclosures)}";
            _bounds.Text = ReaderBounds(page.Bounds);
            _loadMore.Visibility = page.NextOffset.HasValue ? Visibility.Visible : Visibility.Collapsed;
            _loadMore.IsEnabled = page.NextOffset.HasValue;
        }
        catch (OperationCanceledException)
        {
            if (IsCurrent(sequence)) _status.Text = "ATLAS-READER-CANCELED: inventory canceled.";
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Trace.TraceError("code=ATLAS-READER-INVENTORY exception.type={0}", ex.GetType().FullName);
            if (IsCurrent(sequence)) _status.Text = "ATLAS-READER-INVENTORY: inventory unavailable.";
        }
        finally
        {
            Trace.TraceInformation("operation=atlas.reader.inventory duration_ms={0} current={1}",
                Stopwatch.GetElapsedTime(started).TotalMilliseconds, IsCurrent(sequence));
        }
    }

    private async Task<bool> ApplyReaderSelectionAsync(long sequence, Func<ValueTask<AtlasSelectionDto>> query,
        CancellationToken token, BackFrame? previous, string? selectedDeclaration = null, AtlasSelectRequestDto? request = null)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            var selection = await query().ConfigureAwait(true);
            if (!IsCurrent(sequence) || token.IsCancellationRequested) return false;
            _outlineRows.Clear();
            foreach (var row in selection.Outline) _outlineRows.Add(new OutlineRow(selection.FileToken, row));
            _readerSelection = selection;
            _staticView.Render(AtlasStaticViewProjection.Create(selection));
            _bounds.Text = $"Source: {ReaderBounds(selection.SourceBounds)} Outline: {ReaderBounds(selection.OutlineBounds)} "
                + $"Outline {selection.OutlineState}: {selection.OutlineReason}; next {selection.OutlineNextOffset}; "
                + $"coverage {selection.Coverage.State}: {selection.Coverage.Value}; {selection.Coverage.Reason}.";
            var source = selection.Source;
            if (source.State is not SourceProjectionState.IndexedMatch || source.Text is null || source.PageSpan is null
                || source.BindingToken is null)
            {
                ClearSource(source.State, NonMatchText(source.State, [source.Reason ?? "", .. selection.Disclosures]));
                return false;
            }

            _source.Text = source.Text;
            CurrentHighlights = source.Highlights.Select(span => new AtlasTextSpan(span.Start, span.Length)).ToArray();
            if (source.Highlights.FirstOrDefault() is { } highlight)
                _source.Select(highlight.Start - source.PageSpan.Start, highlight.Length);
            _sourceStatus.Text = $"Indexed source match; decoder {source.DecoderId}; page {source.PageSpan.Start}–"
                + $"{source.PageSpan.Start + source.PageSpan.Length}; highlights {source.Highlights.Length}; next {source.NextOffset}.";
            _status.Text = $"Source ready. {string.Join(" ", selection.Disclosures)}";
            if (selectedDeclaration is not null)
                _outline.SelectedItem = _outlineRows.FirstOrDefault(row => row.ObservationKey == selectedDeclaration);
            if (!IsCurrent(sequence) || token.IsCancellationRequested) return false;
            _manifestToken = selection.ManifestToken;
            _receiptToken = selection.ReceiptToken;
            _currentFile = selection.FileToken;
            _selectedDeclarationToken = selection.DeclarationToken;
            CurrentBindingToken = source.BindingToken;
            _outlinePageOffset = request?.OutlineOffset ?? _outlinePageOffset;
            _sourcePageOffset = source.PageSpan.Start;
            _sourcePageLength = request?.SourceLength ?? source.PageSpan.Length;
            _nextOutline.IsEnabled = selection.OutlineNextOffset.HasValue;
            _nextSource.IsEnabled = source.NextOffset.HasValue;
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
            _backButton.IsEnabled = _back.Count > 0;
            return true;
        }
        catch (OperationCanceledException)
        {
            if (IsCurrent(sequence)) ClearSource(SourceProjectionState.Canceled, "ATLAS-READER-CANCELED: selection canceled.");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Trace.TraceError("code=ATLAS-READER-SELECTION exception.type={0}", ex.GetType().FullName);
            if (IsCurrent(sequence)) ClearSource(SourceProjectionState.Unavailable, "ATLAS-READER-SELECTION: selection unavailable.");
        }
        finally
        {
            Trace.TraceInformation("operation=atlas.reader.selection duration_ms={0} current={1}",
                Stopwatch.GetElapsedTime(started).TotalMilliseconds, IsCurrent(sequence));
        }
        return false;
    }

    private static string ReaderBounds(AtlasBoundsDto bounds) => AtlasStaticViewProjection.DescribeBounds(bounds);

    private void ClearPresentation()
    {
        _pendingPrevious = CaptureFrame();
        _receiptToken = null;
        _currentFile = null;
        _readerSelection = null;
        _selectedDeclarationToken = null;
        CurrentBindingToken = null;
        _nextOutline.IsEnabled = _nextSource.IsEnabled = false;
        _outlineRows.Clear();
        ClearSource(SourceProjectionState.Unavailable, "Source cleared while the requested selection loads.");
        _staticView.ShowState(_unloaded ? "Source changed — refresh required" : "Loading Class view…");
        _bounds.Text = "Selection bounds not recorded yet.";
    }

    private BackFrame? CaptureFrame() => _receiptToken is null ? _pendingPrevious : new(
        _receiptToken, _currentFile!,
        (_outline.SelectedItem as OutlineRow)?.ObservationKey,
        Presentation == AtlasPresentationMode.Class ? _staticView.FocusOrigin
            : _outline.IsKeyboardFocusWithin ? "outline" : _source.IsKeyboardFocusWithin ? "source" : "files",
        _source.SelectionStart, _source.SelectionLength, _source.VerticalOffset, _source.HorizontalOffset,
        FindVisual<ScrollViewer>(_outline)?.VerticalOffset ?? 0,
        FindVisual<ScrollViewer>(_files)?.VerticalOffset ?? 0,
        Presentation, _outlinePageOffset, _sourcePageOffset, _sourcePageLength,
        _staticView.Projection?.Classifier?.Token, _staticView.SelectedToken);

    private void RestoreViewState(BackFrame frame)
    {
        _restoringControls = true;
        try
        {
            SelectFileContainer(_files, frame.FileValue);
            UpdateLayout();
            if (frame.Presentation == AtlasPresentationMode.Class)
            {
                if (!_staticView.RestoreFocus(frame.StaticToken, frame.FocusName))
                    _status.Text += " Focus target unavailable on this restored page.";
                return;
            }
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
        double OutlineOffset, double FilesOffset, AtlasPresentationMode Presentation = AtlasPresentationMode.Source,
        int OutlinePageOffset = 0, int SourceOffset = 0, int SourceLength = SourceWindowLength,
        string? ClassifierToken = null, string? StaticToken = null);
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
    public string? ParentToken { get; private init; }
    public string? EntryToken { get; private init; }
    public ObservableCollection<AtlasFileNode> Children { get; } = [];
    public string AccessibleName => EntryToken is not null || IsFile ? $"{RelativePath}; {Details}" : $"{Name} folder";
    public override string ToString() => EntryToken is not null || IsFile ? $"{Name} — {Details}" : Name;

    public static AtlasFileNode Folder(string name, string relativePath) =>
        new(name, relativePath, null, false, "folder");

    internal static AtlasFileNode ReaderEntry(AtlasFileDto entry) =>
        new(entry.RelativePath, entry.RelativePath, entry.Kind is AtlasDirectoryEntryKind.File ? entry.FileToken : null,
            entry.Kind is AtlasDirectoryEntryKind.File,
            $"{entry.Kind}; {entry.Classification}; {entry.Availability}; declarations {entry.DeclarationTotal.State} "
            + $"{entry.DeclarationTotal.Value}; {entry.DeclarationTotal.Reason}; {entry.Reason}")
        { ParentToken = entry.ParentToken, EntryToken = entry.FileToken };

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
    internal OutlineRow(string fileToken, AtlasOutlineRowDto declaration)
    {
        FileValue = fileToken;
        ObservationKey = declaration.DeclarationToken;
        DisplayName = declaration.DisplayName;
        Kind = declaration.Kind;
        Span = new AtlasTextSpan(declaration.Span.Start, declaration.Span.Length);
    }

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
