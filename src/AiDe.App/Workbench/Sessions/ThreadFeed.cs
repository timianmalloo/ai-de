using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Workbench.Sessions;

/// <summary>An operator's act on one turn — the thread's one channel for every action (DS-1 §Contracts).</summary>
public sealed record TurnAction(int Ordinal, TurnActionKind Kind, string? RequestId);

/// <summary>
/// The session thread: one session's accepted turns, in order, above the composer — each turn's
/// words, decoration line, provenance and compiled bytes on demand, and the lane's reply folded
/// beneath it — rendered from one read model (SC1; DS-1 P1–P8).
/// </summary>
/// <remarks>
/// <para><b>Every snapshot reaches the policy in <c>Version</c> order; only the render coalesces
/// (DS-1 P6, C1).</b> <c>Changed</c> may arrive off the UI thread: the handler enqueues the
/// snapshot it was given (never reads <c>Current</c> back) and posts one dispatcher operation
/// while none is pending. <c>Apply</c> drains in order, feeds each to the policy, merges the latest
/// into the rows in place (P5), lays out once, and posts a second operation that announces — the
/// render pass runs between the two at its higher priority (DC-077).</para>
///
/// <para><b>A throwing apply stops the feed loudly</b> (<c>THR-0001</c>, DC-134): the visible
/// stopped row and one assertive announcement, the channel kept alive for a second subscriber.</para>
/// </remarks>
public sealed class ThreadFeed : FeedList, IDisposable
{
    /// <summary>The prose measure (DESIGN.md: 96ch).</summary>
    public const int MeasureCharacters = 96;

    private readonly ISessionThread _thread;
    private readonly IWorkbenchAnnouncer _announcer;
    private readonly ThreadAnnouncementPolicy _policy = new();
    private readonly ObservableCollection<TurnItem> _items = [];
    private readonly ConcurrentQueue<ThreadSnapshot> _pending = new();
    private readonly List<Announcement> _toAnnounce = [];
    private readonly string _surfaceId;
    private int _scheduled;
    private long _lastVersion = -1;
    private bool _disposed;

    /// <param name="thread">The read model.</param>
    /// <param name="announcer">The shared announcer (one across hosts, ADR-0031).</param>
    /// <param name="surfaceId">The document's surface id, for the records.</param>
    public ThreadFeed(ISessionThread thread, IWorkbenchAnnouncer announcer, string surfaceId = "session-document")
    {
        ArgumentNullException.ThrowIfNull(thread);
        ArgumentNullException.ThrowIfNull(announcer);

        _thread = thread;
        _announcer = announcer;
        _surfaceId = surfaceId;

        AutomationProperties.SetName(this, "Conversation");
        AutomationProperties.SetHelpText(this, "Page Down and Page Up move between turns; Up and Down scroll");
        AutomationProperties.SetItemStatus(this, "live");

        MeasureWidth = MeasureCharacters * AdvanceOfZero(FontFamily);

        Resources[DisclosureStyleKey] = DisclosureStyle();
        Resources[ToolDisclosureStyleKey] = ToolDisclosureStyle();
        ItemTemplate = TurnTemplate();
        ItemContainerStyle = TurnContainerStyle();
        ItemsSource = _items;

        _thread.Changed += OnChanged;

        // The initial state, applied synchronously: a pre-loaded read model renders on construction.
        _pending.Enqueue(_thread.Current);
        Apply();
    }

    /// <summary>Raised for every act on a turn. <c>SendAgain</c> and <c>UseAsNextDraft</c> end with <see cref="FeedList.FocusLeaveRequested"/>(ToEditor).</summary>
    public event Action<TurnAction>? TurnActionRequested;

    /// <summary>Raised once when the feed stops updating (<c>THR-0001</c>); the document shows the stopped row.</summary>
    public event Action? Stopped;

    /// <summary>The feed's own fault state: <c>ItemStatus</c> "stopped"; "live" otherwise.</summary>
    public bool IsStopped { get; private set; }

    /// <summary>The prose measure in device-independent pixels: 96 × the advance of "0" in the UI type at 13 px.</summary>
    public double MeasureWidth { get; }

    /// <summary>Whether the operator asked for reduced motion — the WPF adapter reads the system setting; a test flips it.</summary>
    public Func<bool> ReducedMotion { get; set; } = static () => !SystemParameters.ClientAreaAnimation;

    /// <summary>The rows, in ordinal order — a rebuildable projection of the read model's turns.</summary>
    public IReadOnlyList<TurnItem> Rows => _items;

    /// <summary>How many times <c>Apply</c> ran — the coalescing witness (C1).</summary>
    public int Applies { get; private set; }

    /// <summary>The last apply's layout time — reported, never asserted absolutely (DC-107).</summary>
    public double LastLayoutMs { get; private set; }

    /// <summary>The jump list's Enter and the composer's "b1 is running" link: the turn's CONTAINER, never an action.</summary>
    public bool FocusTurn(int ordinal)
    {
        var index = IndexOf(ordinal);
        var landed = index >= 0 && FocusItem(index);
        ThreadDiagnostics.Focus(_surfaceId, "jump", "header", "b" + ordinal.ToString(CultureInfo.InvariantCulture), landed);
        return landed;
    }

    private int IndexOf(int ordinal)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i].Ordinal == ordinal)
            {
                return i;
            }
        }

        return -1;
    }

    // ── the apply pipeline ──

    private void OnChanged(ThreadSnapshot snapshot)
    {
        _pending.Enqueue(snapshot);

        // The scheduled-flag flush: 500 raises post one operation, not 500.
        if (Interlocked.Exchange(ref _scheduled, 1) == 0)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, Apply);
        }
    }

    private void Apply()
    {
        Interlocked.Exchange(ref _scheduled, 0);

        if (_disposed || IsStopped)
        {
            return;
        }

        try
        {
            ThreadSnapshot? latest = null;
            var announcements = new List<Announcement>();

            while (_pending.TryDequeue(out var snapshot))
            {
                if (_lastVersion >= 0 && snapshot.IsCaughtUp && snapshot.Version != _lastVersion + 1 && snapshot.Version != _lastVersion)
                {
                    ThreadDiagnostics.Error(_surfaceId, ThreadDiagnostics.VersionGap, "VersionGap", "the read model skipped a version", _lastVersion + 1, snapshot.Version);
                }

                if (snapshot.IsCaughtUp)
                {
                    _lastVersion = snapshot.Version;
                }

                announcements.AddRange(_policy.Next(snapshot));
                latest = snapshot;
            }

            if (latest is null)
            {
                return;
            }

            Applies++;
            var pinned = IsPinnedAtEnd;
            var stopwatch = Stopwatch.StartNew();

            Merge(latest);
            UpdateLayout();

            var moved = false;
            if (pinned)
            {
                ScrollToEndOfFeed();
                UpdateLayout();
                moved = true;
            }

            stopwatch.Stop();
            LastLayoutMs = stopwatch.Elapsed.TotalMilliseconds;

            ThreadDiagnostics.Layout(
                _surfaceId, latest.Turns.Count, RealizedContainers,
                composerTop: null, viewport: Scroller?.ViewportHeight, LastLayoutMs, pinned, moved, latest.Version);

            if (announcements.Count > 0)
            {
                _toAnnounce.AddRange(announcements);

                // A second operation, after the render pass (DC-077): a focus move the operator
                // made first is spoken first, and the status queues behind it.
                Dispatcher.BeginInvoke(DispatcherPriority.Background, Announce);
            }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Stop(error);
        }
    }

    private void Announce()
    {
        if (_disposed)
        {
            return;
        }

        var batch = _toAnnounce.ToList();
        _toAnnounce.Clear();

        foreach (var announcement in batch)
        {
            _announcer.Announce(announcement);
            ThreadDiagnostics.Announce(_surfaceId, announcement.Ordinal, announcement.Transition, announcement.Urgency.ToString(), _lastVersion);
        }
    }

    /// <summary>The positional merge (P5): same ordinal → the row's <c>View</c>; a new ordinal → append. The caret survives.</summary>
    private void Merge(ThreadSnapshot snapshot)
    {
        var turns = snapshot.Turns;
        for (var i = 0; i < turns.Count; i++)
        {
            var turn = turns[i];
            if (i < _items.Count)
            {
                var row = _items[i];
                var wasLive = row.IsLive;
                row.View = turn;

                // The running turn shows its lines live; a concluded turn folds them (SC7).
                if (row.IsLive)
                {
                    row.IsFoldOpen = true;
                }
                else if (wasLive)
                {
                    row.IsFoldOpen = false;
                }
            }
            else
            {
                var row = new TurnItem(turn) { IsFoldOpen = turn.State is TurnState.Running or TurnState.Waiting };
                _items.Add(row);
            }
        }

        // The stream is append-only; a snapshot shorter than the rows is a contract violation the
        // rows do not follow (a turn never disappears).
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].IsLast = i == _items.Count - 1;
        }

        // THE QUEUED ROW READS THE SNAPSHOT, NOT ITSELF (Ruling 95): who it waits behind and whether
        // it waits on the operator are the snapshot's derivations, set on the row at every merge.
        if (snapshot.Queued is { } queued && IndexOf(queued.Ordinal) is var at && at >= 0)
        {
            _items[at].QueuedSentence = TurnCopy.QueuedSentence(snapshot);
            _items[at].QueuedAwaitsYou = snapshot.QueuedAwaitsYou;
        }

        if (_items.Count > 0 && SelectedIndex < 0)
        {
            SelectedIndex = _items.Count - 1;
        }
    }

    private void Stop(Exception error)
    {
        IsStopped = true;
        AutomationProperties.SetItemStatus(this, "stopped");
        ThreadDiagnostics.Error(_surfaceId, ThreadDiagnostics.ApplyFailed, error.GetType().FullName ?? error.GetType().Name, error.Message);

        try
        {
            Stopped?.Invoke();
        }
        finally
        {
            _announcer.Announce(new Announcement(StoppedSentence, Urgency.Assertive, AnnouncementKind.Aborted));
        }
    }

    /// <summary>The stopped row's words (SC9): visible outside the scroller and announced once.</summary>
    public const string StoppedSentence = "The thread stopped updating; reopen the session.";

    // ── Escape (K8): the disclosure whose subtree holds focus closes via its header; never leaves the document ──

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseDisclosureHoldingFocus();
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    /// <summary>
    /// The turn's first action button when it offers one (SC8 as amended: on a running, waiting,
    /// failed or stopped last turn the entry stop is its first action) — the Shift+Tab-from-the-editor
    /// landing; Tab from the container lands there too because the actions carry <c>TabIndex</c> 0
    /// inside the container's local tab scope. Null otherwise, so DOM order applies.
    /// </summary>
    protected override UIElement? EntryStop(ListBoxItem container) =>
        container.DataContext is TurnItem { HasActions: true }
            ? TabStops(container).OfType<Button>().FirstOrDefault(b => b.Tag is TurnActionKind)
            : null;

    private static void CloseDisclosureHoldingFocus()
    {
        if (Keyboard.FocusedElement is not DependencyObject focused)
        {
            return;
        }

        for (var node = focused; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (node is ListBoxItem)
            {
                return;   // the container itself, or no disclosure holds focus: a no-op
            }

            if (node is Expander { IsExpanded: true } expander)
            {
                // Focus the header first — a collapse under a focused scroller would drop focus to the root.
                if (expander.Template?.FindName("HeaderSite", expander) is UIElement header)
                {
                    header.Focus();
                }

                expander.IsExpanded = false;
                return;
            }
        }
    }

    // ── actions ──

    private void OnActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TurnActionKind kind } button && RowOf(button) is { } row)
        {
            Request(row, kind);
        }
    }

    private void OnTailClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && RowOf(button) is { } row)
        {
            Request(row, TurnActionKind.OpenConsoleAt);
        }
    }

    private void OnUseAsNextDraftClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && RowOf(button) is { } row)
        {
            Request(row, TurnActionKind.UseAsNextDraft);
        }
    }

    /// <summary>The row an element inside a turn belongs to: the container's DataContext.</summary>
    private static TurnItem? RowOf(DependencyObject element)
    {
        for (var node = element; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (node is ListBoxItem { DataContext: TurnItem row })
            {
                return row;
            }
        }

        return null;
    }

    private static void ResetCompiledScroll(object sender, RoutedEventArgs e)
    {
        // A recycled container keeps its scroller's offset otherwise (spike Q13's class): every
        // opening of the compiled prompt starts at its top.
        if (sender is Expander { Content: ScrollViewer scroller })
        {
            scroller.ScrollToTop();
        }
    }

    private void Request(TurnItem row, TurnActionKind kind)
    {
        // BEFORE the act: the merge that disables or removes the button finds focus on the
        // container, never on the window (DS-1 A4).
        var index = IndexOf(row.Ordinal);
        if (index >= 0 && ItemContainerGenerator.ContainerFromIndex(index) is ListBoxItem container)
        {
            container.Focus();
        }

        var requestId = row.View.Waiting?.RequestId;
        ThreadDiagnostics.Action(_surfaceId, row.Ordinal, kind.ToString(), requestId);
        TurnActionRequested?.Invoke(new TurnAction(row.Ordinal, kind, requestId));

        if (kind is TurnActionKind.SendAgain or TurnActionKind.UseAsNextDraft)
        {
            Act(new FeedKeyDecision.Leave(FocusLeave.ToEditor));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _thread.Changed -= OnChanged;
        ItemsSource = null;
    }

    // ── the template (DESIGN.md §The turn; DS-1 §The control's shape) ──

    private const string DisclosureStyleKey = "FeedDisclosure";
    /// <summary>The one monospace stack the thread, the split, the document and the composer share.</summary>
    internal static readonly FontFamily Mono = new("Cascadia Mono, Consolas, monospace");

    /// <summary>
    /// The one clock every timestamp on the surface renders in — the operator's local time, as the
    /// turn's <c>hh:mm</c> and the split's heading already do. A line stamped at receipt is UTC
    /// (<c>AcpPeer</c> stamps <c>GetUtcNow()</c>); a <c>StringFormat</c> alone rendered that clock,
    /// one hour off the heading above it (DM-A: two conversions of one instant).
    /// </summary>
    internal static readonly IValueConverter LocalClock = new LocalClockConverter();

    private sealed class LocalClockConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is DateTimeOffset at ? at.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture) : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException("a rendered clock is never written back");
    }

    // One frozen instance for every ring: the storyboard animates a clone (a frozen Freezable is
    // cloned on animation), so sharing is safe — and Freeze() says so rather than relying on it.
    private static readonly RotateTransform RingTransform = Frozen(new RotateTransform(0));

    private static RotateTransform Frozen(RotateTransform transform)
    {
        transform.Freeze();
        return transform;
    }

    private static double AdvanceOfZero(FontFamily family)
    {
        var typeface = new Typeface(family, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var text = new FormattedText("0", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 13, Brushes.Black, 1.0);
        return text.WidthIncludingTrailingWhitespace;
    }

    private Style TurnContainerStyle()
    {
        var style = new Style(typeof(ListBoxItem), ContainerStyle());
        // A local tab scope per turn: the actions' TabIndex 0 (SC8 as amended) ranks them first
        // inside their own turn and never ahead of another turn's stops.
        style.Setters.Add(new Setter(KeyboardNavigation.TabNavigationProperty, KeyboardNavigationMode.Local));
        style.Setters.Add(new Setter(AutomationProperties.NameProperty, new Binding(nameof(TurnItem.Name))));
        style.Setters.Add(new Setter(AutomationProperties.ItemStatusProperty, new Binding(nameof(TurnItem.DecorationLine))));
        style.Setters.Add(new Setter(AutomationProperties.HelpTextProperty, new Binding(nameof(TurnItem.HelpText))));
        return style;
    }

    private static FrameworkElementFactory F(Type type, string? name = null) =>
        name is null ? new FrameworkElementFactory(type) : new FrameworkElementFactory(type, name);

    /// <param name="brush">
    /// The ink, set on the template — or null when a Style owns it: a value the template sets
    /// outranks a Style trigger (DP precedence: template 4, style trigger 6), so an element whose
    /// ink changes with its state must leave it to the style's base setter and triggers.
    /// </param>
    /// <summary>One bound text of the row grammar — internal so the Console split builds its rows from the thread's own segments (one grammar, two surfaces).</summary>
    internal static FrameworkElementFactory Text(string bindingPath, double size = 13, string? brush = "TextBrush", bool mono = false, bool wrap = false)
    {
        var text = F(typeof(ThreadText));
        text.SetBinding(TextBlock.TextProperty, new Binding(bindingPath));
        text.SetValue(TextBlock.FontSizeProperty, size);
        if (brush is not null)
        {
            text.SetResourceReference(TextBlock.ForegroundProperty, brush);
        }

        text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        if (mono)
        {
            text.SetValue(TextBlock.FontFamilyProperty, Mono);
        }

        if (wrap)
        {
            text.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        }

        return text;
    }

    private static FrameworkElementFactory Segment(params FrameworkElementFactory[] parts)
    {
        var segment = F(typeof(StackPanel));
        segment.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        segment.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 12, 0));
        segment.SetValue(FrameworkElement.MinHeightProperty, 24.0);
        foreach (var part in parts)
        {
            segment.AppendChild(part);
        }

        return segment;
    }

    private DataTemplate TurnTemplate()
    {
        var grid = F(typeof(Grid));
        var gutterColumn = F(typeof(ColumnDefinition));
        gutterColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(40));
        var bodyColumn = F(typeof(ColumnDefinition));
        bodyColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
        grid.AppendChild(gutterColumn);
        grid.AppendChild(bodyColumn);

        // The hairline between turns (DX13): one 1 px top edge on every turn but the first would need
        // the index; a bottom hairline on every turn reads the same — the composer's boundary closes the last.
        var hairline = F(typeof(Border));
        hairline.SetValue(Grid.ColumnSpanProperty, 2);
        hairline.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 0, 1));
        hairline.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        hairline.SetValue(Border.SnapsToDevicePixelsProperty, true);
        hairline.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Bottom);
        grid.AppendChild(hairline);

        // Gutter: the ordinal, mono, muted (DESIGN.md:1105). Never an avatar.
        var gutter = Text(nameof(TurnItem.DisplayOrdinal), 12, "TextMutedBrush", mono: true);
        gutter.SetValue(Grid.ColumnProperty, 0);
        gutter.SetValue(FrameworkElement.MarginProperty, new Thickness(12, 12, 0, 0));
        gutter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Top);
        grid.AppendChild(gutter);

        var body = F(typeof(StackPanel));
        body.SetValue(Grid.ColumnProperty, 1);
        body.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 12, 12, 8));

        // The words: verbatim, 13 px, wrapping at the 96ch measure (DESIGN.md:1106).
        var words = Text(nameof(TurnItem.Words), 13, "TextBrush", wrap: true);
        words.SetValue(FrameworkElement.MaxWidthProperty, MeasureWidth);
        words.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        body.AppendChild(words);

        body.AppendChild(DecorationLine());

        // The reply side (Ruling 82): the conversation's items in event order, THEN the outcome line
        // (whose fold holds only the non-conversation rows), the reason, the actions.
        var replySide = F(typeof(StackPanel));
        replySide.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 8, 0, 0));

        var conversation = F(typeof(ItemsControl));
        conversation.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(TurnItem.Conversation)));
        conversation.SetValue(ItemsControl.ItemTemplateSelectorProperty, new ConversationTemplates(ProseItemTemplate(), ReasoningItemTemplate(), ToolItemTemplate()));
        conversation.SetValue(KeyboardNavigation.IsTabStopProperty, false);
        conversation.SetValue(UIElement.FocusableProperty, false);
        replySide.AppendChild(conversation);

        replySide.AppendChild(OutcomeLine());
        replySide.AppendChild(ReasonBox());
        replySide.AppendChild(ActionRow());
        body.AppendChild(replySide);

        grid.AppendChild(body);
        return new DataTemplate(typeof(TurnItem)) { VisualTree = grid };
    }

    /// <summary><c>class · tier · lease [+n more] · shape [· template] · provenance ▸ · compiled prompt ▸ · hh:mm</c> (DESIGN.md:1107).</summary>
    private FrameworkElementFactory DecorationLine()
    {
        var line = F(typeof(WrapPanel));
        line.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 4, 0, 0));
        line.SetValue(FrameworkElement.MinHeightProperty, 24.0);

        var rows = F(typeof(ItemsControl));
        rows.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(TurnItem.Decorations)));
        var rowsPanel = F(typeof(WrapPanel));
        rows.SetValue(ItemsControl.ItemsPanelProperty, new ItemsPanelTemplate(rowsPanel));
        rows.SetValue(ItemsControl.ItemTemplateProperty, DecorationSegmentTemplate());
        rows.SetValue(KeyboardNavigation.IsTabStopProperty, false);
        rows.SetValue(UIElement.FocusableProperty, false);
        line.AppendChild(rows);

        // Provenance: one disclosure away, named for its turn, its state on the row (P8).
        var provenance = Disclosure("provenance", nameof(TurnItem.ProvenanceName), nameof(TurnItem.IsProvenanceOpen));
        provenance.AppendChild(ProvenanceTable());
        line.AppendChild(provenance);

        // Compiled prompt: exactly the sent bytes, ≤ 200 px, its scroller focusable and named while expanded.
        var compiled = Disclosure("compiled prompt", nameof(TurnItem.CompiledName), nameof(TurnItem.IsCompiledOpen));
        var scroller = F(typeof(ScrollViewer));
        scroller.SetValue(FrameworkElement.MaxHeightProperty, 200.0);
        scroller.SetValue(FrameworkElement.MaxWidthProperty, MeasureWidth);
        scroller.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        scroller.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        scroller.SetValue(UIElement.FocusableProperty, true);
        scroller.SetBinding(KeyboardNavigation.IsTabStopProperty, new Binding(nameof(TurnItem.IsCompiledOpen)));
        scroller.SetBinding(AutomationProperties.NameProperty, new Binding(nameof(TurnItem.CompiledName)));
        scroller.SetResourceReference(Control.BackgroundProperty, "SurfaceSunkenBrush");
        scroller.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 4, 0, 4));
        compiled.AddHandler(Expander.ExpandedEvent, new RoutedEventHandler(ResetCompiledScroll));
        var bytes = Text(nameof(TurnItem.SentBytes), 12, "TextBrush", mono: true, wrap: true);
        bytes.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 6, 8, 6));
        scroller.AppendChild(bytes);
        compiled.AppendChild(FocusRing(scroller));
        line.AppendChild(compiled);

        var time = Text(nameof(TurnItem.Time), 12, "TextMutedBrush", mono: true);
        time.SetValue(FrameworkElement.MinHeightProperty, 24.0);
        line.AppendChild(time);

        return line;
    }

    private static DataTemplate DecorationSegmentTemplate()
    {
        var value = Text(nameof(DecorationRow.Value), 12, "TextBrush");

        // The label is the row's name; the shape row carries its value alone (SC2's grammar).
        var label = F(typeof(ThreadText));
        label.SetBinding(TextBlock.TextProperty, new Binding(nameof(DecorationRow.Name)));
        label.SetValue(TextBlock.FontSizeProperty, 12.0);
        label.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        label.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        label.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 4, 0));
        var labelStyle = new Style(typeof(ThreadText));
        var shape = new DataTrigger { Binding = new Binding(nameof(DecorationRow.Name)), Value = "shape" };
        shape.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
        labelStyle.Triggers.Add(shape);
        label.SetValue(FrameworkElement.StyleProperty, labelStyle);

        var valueStyle = new Style(typeof(ThreadText));
        var lease = new DataTrigger { Binding = new Binding(nameof(DecorationRow.Name)), Value = "lease" };
        lease.Setters.Add(new Setter(TextBlock.FontFamilyProperty, Mono));
        valueStyle.Triggers.Add(lease);
        value.SetValue(FrameworkElement.StyleProperty, valueStyle);
        value.SetBinding(AutomationProperties.HelpTextProperty, new Binding(nameof(DecorationRow.Reason)));

        return new DataTemplate(typeof(DecorationRow)) { VisualTree = Segment(label, value) };
    }

    private FrameworkElementFactory ProvenanceTable()
    {
        var panel = F(typeof(StackPanel));
        panel.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 4, 0, 4));

        var rows = F(typeof(ItemsControl));
        rows.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(TurnItem.Decorations)));
        rows.SetValue(KeyboardNavigation.IsTabStopProperty, false);
        rows.SetValue(UIElement.FocusableProperty, false);

        var row = F(typeof(StackPanel));
        row.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        row.SetValue(FrameworkElement.MinHeightProperty, 20.0);
        var name = Text(nameof(DecorationRow.Name), 12, "TextMutedBrush");
        name.SetValue(FrameworkElement.MinWidthProperty, 72.0);
        var value = Text(nameof(DecorationRow.Value), 12, "TextBrush", mono: true);
        value.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 12, 0));
        var source = Text(nameof(DecorationRow.Source), 12, "TextMutedBrush");
        source.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
        var reason = Text(nameof(DecorationRow.Reason), 12, "TextMutedBrush", wrap: true);
        row.AppendChild(name);
        row.AppendChild(value);
        row.AppendChild(source);
        row.AppendChild(reason);
        rows.SetValue(ItemsControl.ItemTemplateProperty, new DataTemplate(typeof(DecorationRow)) { VisualTree = row });
        panel.AppendChild(rows);

        var use = F(typeof(Button));
        use.SetValue(ContentControl.ContentProperty, ThreadAnnouncementPolicy.ActionWord(TurnActionKind.UseAsNextDraft));
        use.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        use.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 4, 0, 0));
        use.SetValue(FrameworkElement.MinHeightProperty, 24.0);
        use.SetValue(FrameworkElement.MinWidthProperty, 24.0);
        use.SetValue(Control.PaddingProperty, new Thickness(8, 2, 8, 2));
        use.SetValue(Button.IsDefaultProperty, false);
        use.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnUseAsNextDraftClick));
        panel.AppendChild(use);
        return panel;
    }

    /// <summary>glyph · word · lane · counts · the fold (DESIGN.md:1110).</summary>
    private FrameworkElementFactory OutcomeLine()
    {
        var line = F(typeof(WrapPanel));
        line.SetValue(FrameworkElement.MinHeightProperty, 24.0);

        var glyph = F(typeof(Grid));
        glyph.SetValue(FrameworkElement.WidthProperty, 12.0);
        glyph.SetValue(FrameworkElement.HeightProperty, 12.0);
        glyph.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 6, 0));
        glyph.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        glyph.SetValue(UIElement.IsHitTestVisibleProperty, false);

        // The glyph: a Path beside the word — decoration, no peer (SC10). The running ring spins
        // unless motion is reduced; it is static beside the word "running" then.
        var ring = F(typeof(Ellipse));
        ring.SetValue(Shape.StrokeThicknessProperty, 2.0);
        ring.SetValue(Shape.StrokeDashArrayProperty, new DoubleCollection([3, 2]));
        ring.SetValue(UIElement.RenderTransformOriginProperty, new Point(0.5, 0.5));
        ring.SetValue(UIElement.RenderTransformProperty, RingTransform);
        ring.SetValue(FrameworkElement.StyleProperty, RingStyle(nameof(TurnItem.State), TurnState.Running));
        glyph.AppendChild(ring);

        var mark = F(typeof(Path));
        mark.SetValue(Shape.StrokeThicknessProperty, 2.0);
        mark.SetValue(Shape.StretchProperty, Stretch.Uniform);
        mark.SetValue(FrameworkElement.StyleProperty, MarkStyle());
        glyph.AppendChild(mark);
        line.AppendChild(glyph);

        var word = Text(nameof(TurnItem.OutcomeWord), 12, brush: null);
        word.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        word.SetValue(FrameworkElement.StyleProperty, OutcomeWordStyle());
        word.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 0));
        line.AppendChild(word);

        var lane = Text(nameof(TurnItem.Lane), 12, "AccentBrush", mono: true);
        lane.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 0));
        line.AppendChild(lane);

        var counts = Text(nameof(TurnItem.Counts), 12, "TextBrush", mono: true);
        counts.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 0));
        line.AppendChild(counts);

        // The fold: "N events" (P8), its content the last four lines and the tail into the Console.
        var fold = Disclosure(null, nameof(TurnItem.FoldHeader), nameof(TurnItem.IsFoldOpen), headerBinding: nameof(TurnItem.FoldHeader));
        fold.AppendChild(FoldContent());
        line.AppendChild(fold);

        return line;
    }

    private FrameworkElementFactory FoldContent()
    {
        var panel = F(typeof(StackPanel));
        panel.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 2, 0, 2));

        var lines = F(typeof(ItemsControl));
        lines.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(TurnItem.FoldedEvents)));
        lines.SetValue(ItemsControl.ItemTemplateProperty, EventLineTemplate());
        lines.SetValue(KeyboardNavigation.IsTabStopProperty, false);
        lines.SetValue(UIElement.FocusableProperty, false);
        panel.AppendChild(lines);

        var tail = F(typeof(Button));
        tail.SetBinding(ContentControl.ContentProperty, new Binding(nameof(TurnItem.TailText)));
        tail.SetBinding(UIElement.VisibilityProperty, Visible(nameof(TurnItem.HasOtherEvents)));
        tail.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        tail.SetValue(FrameworkElement.MinHeightProperty, 24.0);
        tail.SetValue(FrameworkElement.MinWidthProperty, 24.0);
        tail.SetValue(Control.PaddingProperty, new Thickness(8, 2, 8, 2));
        tail.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 2, 0, 0));
        tail.SetValue(Button.IsDefaultProperty, false);
        tail.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnTailClick));
        panel.AppendChild(tail);
        return panel;
    }

    // ── the conversation's items (Ruling 82; DESIGN.md the reply row and the events row as amended) ──

    /// <summary>One template per item kind, chosen by the row's facets — prose, reasoning, tool.</summary>
    private sealed class ConversationTemplates(DataTemplate prose, DataTemplate reasoning, DataTemplate tool) : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object item, DependencyObject container) => item switch
        {
            ConversationRow { IsReasoning: true } => reasoning,
            ConversationRow { IsTool: true } => tool,
            ConversationRow => prose,
            _ => null,
        };
    }

    /// <summary>Prose: the markdown subset rendered by <see cref="ProseView"/>, wrapping at the 96ch measure; no link activation.</summary>
    private DataTemplate ProseItemTemplate()
    {
        var prose = F(typeof(ProseView));
        prose.SetBinding(ProseView.TextProperty, new Binding(nameof(ConversationRow.Text)));
        prose.SetValue(FrameworkElement.MaxWidthProperty, MeasureWidth);
        prose.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        prose.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 2, 0, 2));
        return new DataTemplate(typeof(ConversationRow)) { VisualTree = prose };
    }

    /// <summary>
    /// Reasoning: the CLI's collapsed-thinking idiom — a three-dot glyph beside one disclosure
    /// <i>Thinking</i> in muted ink, collapsed by default, its state on the row; opened, the joined
    /// thought as 12 px muted plain text at the measure. Never announced: no live setting, and the
    /// policy diffs states, never rows (SC9).
    /// </summary>
    private DataTemplate ReasoningItemTemplate()
    {
        var line = F(typeof(DockPanel));
        line.SetValue(FrameworkElement.MinHeightProperty, 24.0);
        line.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);

        var dots = F(typeof(Path));
        dots.SetValue(Path.DataProperty, Geometry.Parse("M 1,4 A 1.5,1.5 0 1 0 4,4 A 1.5,1.5 0 1 0 1,4 M 7,4 A 1.5,1.5 0 1 0 10,4 A 1.5,1.5 0 1 0 7,4 M 13,4 A 1.5,1.5 0 1 0 16,4 A 1.5,1.5 0 1 0 13,4"));
        dots.SetResourceReference(Shape.FillProperty, "TextMutedBrush");
        dots.SetValue(FrameworkElement.WidthProperty, 16.0);
        dots.SetValue(FrameworkElement.HeightProperty, 8.0);
        dots.SetValue(Shape.StretchProperty, Stretch.Uniform);
        dots.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Top);
        dots.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 8, 8, 0));
        dots.SetValue(UIElement.IsHitTestVisibleProperty, false);
        dots.SetValue(DockPanel.DockProperty, Dock.Left);
        line.AppendChild(dots);

        var disclosure = Disclosure(ThinkingWord, null, nameof(ConversationRow.IsOpen));
        disclosure.SetValue(AutomationProperties.NameProperty, ThinkingWord);
        disclosure.SetValue(HeaderedContentControl.HeaderTemplateProperty, MutedHeaderTemplate());
        var thought = Text(nameof(ConversationRow.Text), 12, "TextMutedBrush", wrap: true);
        thought.SetValue(FrameworkElement.MaxWidthProperty, MeasureWidth);
        thought.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        thought.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 2, 0, 6));
        disclosure.AppendChild(thought);
        line.AppendChild(disclosure);

        return new DataTemplate(typeof(ConversationRow)) { VisualTree = line };
    }

    /// <summary>The copy of the reasoning item's line (DESIGN.md, copy added by the errata).</summary>
    public const string ThinkingWord = "Thinking";

    /// <summary>A disclosure header in muted ink (the reasoning item's <i>Thinking</i>), outranking the toggle's accent by a local value on the text.</summary>
    private static DataTemplate MutedHeaderTemplate()
    {
        var text = F(typeof(ThreadText));
        text.SetBinding(TextBlock.TextProperty, new Binding("."));
        text.SetValue(TextBlock.FontSizeProperty, 12.0);
        text.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        return new DataTemplate(typeof(string)) { VisualTree = text };
    }

    /// <summary>
    /// A tool item: one 24 px line — a neutral marker · the kind as a muted word (1.1.1) · the title
    /// in mono · the status word (the ring while running; <c>{colors.verified}</c> done,
    /// <c>{colors.danger}</c> failed, muted <i>interrupted</i>) · <i>detail ▸</i> — whose disclosure
    /// opens the input and the result as 12 px mono on the sunken ground, ≤ 200 px then scrolling,
    /// a named, focusable region (A11-3) inside the disclosure's subtree so Escape closes it (K8).
    /// </summary>
    private DataTemplate ToolItemTemplate()
    {
        var item = F(typeof(Expander));
        item.SetValue(FrameworkElement.StyleProperty, new DynamicResourceExtension(ToolDisclosureStyleKey));
        item.SetBinding(AutomationProperties.NameProperty, new Binding(nameof(ConversationRow.DetailName)));
        item.SetBinding(Expander.IsExpandedProperty, new Binding(nameof(ConversationRow.IsOpen)) { Mode = BindingMode.TwoWay });
        item.SetValue(UIElement.FocusableProperty, false);
        item.SetValue(KeyboardNavigation.IsTabStopProperty, false);
        item.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        item.SetBinding(HeaderedContentControl.HeaderProperty, new Binding("."));
        item.SetValue(HeaderedContentControl.HeaderTemplateProperty, ToolLineTemplate(MeasureWidth));
        item.AddHandler(Expander.ExpandedEvent, new RoutedEventHandler(ResetCompiledScroll));

        var region = F(typeof(ScrollViewer));
        region.SetValue(FrameworkElement.MaxHeightProperty, 200.0);
        region.SetValue(FrameworkElement.MaxWidthProperty, MeasureWidth);
        region.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        region.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        region.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        region.SetValue(UIElement.FocusableProperty, true);
        region.SetBinding(KeyboardNavigation.IsTabStopProperty, new Binding(nameof(ConversationRow.IsOpen)));
        region.SetBinding(AutomationProperties.NameProperty, new Binding(nameof(ConversationRow.DetailRegionName)));
        region.SetResourceReference(Control.BackgroundProperty, "SurfaceSunkenBrush");
        region.SetValue(FrameworkElement.MarginProperty, new Thickness(24, 2, 0, 6));
        var pre = Text(nameof(ConversationRow.Detail), 12, "TextBrush", mono: true, wrap: true);
        pre.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 6, 8, 6));
        region.AppendChild(pre);
        item.AppendChild(FocusRing(region));

        return new DataTemplate(typeof(ConversationRow)) { VisualTree = item };
    }

    /// <summary>
    /// A keyboard-scrollable region's focus ring (2.4.7): a 2 px border that lights
    /// <c>{colors.focus}</c> while focus is within — the container's ring idiom, since a
    /// <see cref="ScrollViewer"/>'s default template draws no border and the platform's dotted
    /// adorner is black on the sunken ground (the UX &amp; Accessibility lens's F2).
    /// </summary>
    private static FrameworkElementFactory FocusRing(FrameworkElementFactory scroller)
    {
        var ring = F(typeof(Border));
        ring.SetValue(Border.BorderThicknessProperty, new Thickness(2));
        ring.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        ring.SetValue(Border.SnapsToDevicePixelsProperty, true);
        ring.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        var style = new Style(typeof(Border));
        style.Setters.Add(new Setter(Border.BorderBrushProperty, Brushes.Transparent));
        var focused = new Trigger { Property = UIElement.IsKeyboardFocusWithinProperty, Value = true };
        focused.Setters.Add(new Setter(Border.BorderBrushProperty, new DynamicResourceExtension("FocusBrush")));
        style.Triggers.Add(focused);
        ring.SetValue(FrameworkElement.StyleProperty, style);
        ring.AppendChild(scroller);
        return ring;
    }

    /// <summary>The tool item's line: marker · kind · title · status. The title trims at half the measure so the status and the toggle stay in view (2.4.11); its full text stays its name.</summary>
    private static DataTemplate ToolLineTemplate(double measure)
    {
        var line = F(typeof(StackPanel));
        line.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        line.SetValue(FrameworkElement.MinHeightProperty, 24.0);

        var marker = F(typeof(Path));
        marker.SetValue(Path.DataProperty, new EllipseGeometry(new Point(3, 3), 3, 3));
        marker.SetValue(FrameworkElement.WidthProperty, 6.0);
        marker.SetValue(FrameworkElement.HeightProperty, 6.0);
        marker.SetResourceReference(Shape.FillProperty, "TextMutedBrush");
        marker.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        marker.SetValue(FrameworkElement.MarginProperty, new Thickness(7, 0, 11, 0));
        marker.SetValue(UIElement.IsHitTestVisibleProperty, false);
        line.AppendChild(marker);

        var kind = Text(nameof(ConversationRow.Kind), 12, "TextMutedBrush");
        kind.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
        var kindStyle = new Style(typeof(ThreadText));
        var noKind = new DataTrigger { Binding = new Binding(nameof(ConversationRow.Kind)), Value = string.Empty };
        noKind.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
        kindStyle.Triggers.Add(noKind);
        kind.SetValue(FrameworkElement.StyleProperty, kindStyle);
        line.AppendChild(kind);

        var title = Text(nameof(ConversationRow.Title), 13, "TextBrush", mono: true);
        title.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 0));
        title.SetValue(FrameworkElement.MaxWidthProperty, measure / 2);
        title.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        line.AppendChild(title);

        var ring = F(typeof(Ellipse));
        ring.SetValue(FrameworkElement.WidthProperty, 10.0);
        ring.SetValue(FrameworkElement.HeightProperty, 10.0);
        ring.SetValue(Shape.StrokeThicknessProperty, 2.0);
        ring.SetValue(Shape.StrokeDashArrayProperty, new DoubleCollection([3, 2]));
        ring.SetValue(UIElement.RenderTransformOriginProperty, new Point(0.5, 0.5));
        ring.SetValue(UIElement.RenderTransformProperty, RingTransform);
        ring.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        ring.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 4, 0));
        ring.SetValue(UIElement.IsHitTestVisibleProperty, false);
        ring.SetValue(FrameworkElement.StyleProperty, RingStyle(nameof(ConversationRow.IsRunning), true));
        line.AppendChild(ring);

        var status = Text(nameof(ConversationRow.StatusWord), 12, brush: null);
        status.SetValue(FrameworkElement.StyleProperty, ToolStatusStyle());
        status.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 0));
        line.AppendChild(status);

        return new DataTemplate(typeof(ConversationRow)) { VisualTree = line };
    }

    /// <summary>The status word's ink by status (the base is the style's setter, never the template's: DP precedence).</summary>
    private static Style ToolStatusStyle()
    {
        var style = new Style(typeof(ThreadText));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, new DynamicResourceExtension("TextBrush")));
        void Colour(ToolStatus status, string brush)
        {
            var trigger = new DataTrigger { Binding = new Binding(nameof(ConversationRow.Status)), Value = status };
            trigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, new DynamicResourceExtension(brush)));
            style.Triggers.Add(trigger);
        }

        Colour(ToolStatus.Done, "VerifiedBrush");
        Colour(ToolStatus.Failed, "DangerBrush");
        Colour(ToolStatus.Interrupted, "TextMutedBrush");
        return style;
    }

    private const string ToolDisclosureStyleKey = "ToolDisclosure";

    /// <summary>
    /// The tool item's disclosure template: the header content (the line) and the <i>detail</i>
    /// toggle share one row; the expanded region spans the row beneath at full width — inside the
    /// Expander's subtree, so the K8 Escape rule and the ExpandCollapse pattern hold as for every
    /// other disclosure.
    /// </summary>
    internal static Style ToolDisclosureStyle()
    {
        var root = F(typeof(StackPanel));

        var line = F(typeof(StackPanel));
        line.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        var header = F(typeof(ContentPresenter));
        header.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(HeaderedContentControl.HeaderProperty));
        header.SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(HeaderedContentControl.HeaderTemplateProperty));
        header.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        line.AppendChild(header);

        var toggle = F(typeof(ToggleButton), "HeaderSite");
        // TWO-WAY, as the stock Expander template binds its HeaderSite (Ruling 96). A TemplateBinding
        // is one-way, templated parent to child: the click checked the toggle, the chevron rotated,
        // and IsExpanded never moved — so the ExpandSite trigger never fired and the reader got 0 px
        // (the operator's screenshot: chevron down, nothing beneath; the ledger: compiled null).
        // Every oracle had set IsExpanded through the property and was green.
        toggle.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(Expander.IsExpanded)) { RelativeSource = RelativeSource.TemplatedParent, Mode = BindingMode.TwoWay });
        toggle.SetValue(ContentControl.ContentProperty, "detail");
        toggle.SetValue(FrameworkElement.MinHeightProperty, 24.0);
        toggle.SetValue(FrameworkElement.MinWidthProperty, 24.0);
        toggle.SetValue(Control.PaddingProperty, new Thickness(2, 0, 4, 0));
        toggle.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        toggle.SetValue(Control.TemplateProperty, HeaderToggleTemplate());
        toggle.SetValue(FrameworkElement.FocusVisualStyleProperty, null);
        toggle.SetValue(AutomationProperties.NameProperty, new TemplateBindingExtension(AutomationProperties.NameProperty));
        line.AppendChild(toggle);
        root.AppendChild(line);

        var content = F(typeof(ContentPresenter), "ExpandSite");
        content.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
        content.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
        root.AppendChild(content);

        var template = new ControlTemplate(typeof(Expander)) { VisualTree = root };
        var expanded = new Trigger { Property = Expander.IsExpandedProperty, Value = true };
        expanded.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "ExpandSite"));
        template.Triggers.Add(expanded);

        var style = new Style(typeof(Expander));
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        return style;
    }

    /// <summary>ts (muted) · lane (accent) · message; stderr in danger (DESIGN.md:1112). Binds the row grammar's names — an <see cref="EventLine"/> or a <see cref="TurnRow"/> alike.</summary>
    public static DataTemplate EventLineTemplate()
    {
        // A DockPanel, not a horizontal StackPanel: a StackPanel measures every child at infinite
        // width, so a wrapping text never wraps and a long line is clipped (the list disables the
        // horizontal scrollbar). The last child fills what the fixed segments leave.
        var row = F(typeof(DockPanel));
        row.SetValue(FrameworkElement.MinHeightProperty, 20.0);

        row.AppendChild(Clock(nameof(EventLine.At)));
        row.AppendChild(Segment(Text(nameof(EventLine.Lane), 12, "AccentBrush", mono: true)));

        var message = Text(nameof(EventLine.Text), 12, brush: null, wrap: true);
        message.SetValue(FrameworkElement.StyleProperty, StderrInk(nameof(EventLine.Kind)));
        row.AppendChild(message);

        return new DataTemplate(typeof(EventLine)) { VisualTree = row };
    }

    /// <summary>The row's clock: <c>hh:mm:ss</c> in the operator's local time, muted mono, docked left.</summary>
    internal static FrameworkElementFactory Clock(string bindingPath)
    {
        var time = F(typeof(ThreadText));
        time.SetBinding(TextBlock.TextProperty, new Binding(bindingPath) { Converter = LocalClock });
        time.SetValue(TextBlock.FontSizeProperty, 12.0);
        time.SetValue(TextBlock.FontFamilyProperty, Mono);
        time.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return Segment(time);
    }

    /// <summary>A fixed segment of a row: docked left, 8 px after it, centred on the line.</summary>
    internal static FrameworkElementFactory Segment(FrameworkElementFactory text, Dock dock = Dock.Left)
    {
        text.SetValue(DockPanel.DockProperty, dock);
        text.SetValue(FrameworkElement.MarginProperty, dock == Dock.Left ? new Thickness(0, 0, 8, 0) : new Thickness(8, 0, 0, 0));
        text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        return text;
    }

    /// <summary>The message text's ink: the theme's text, danger when the bound kind is <c>stderr</c> (DESIGN.md:1112) — one style, both surfaces.</summary>
    internal static Style StderrInk(string kindPath)
    {
        var style = new Style(typeof(ThreadText));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, new DynamicResourceExtension("TextBrush")));
        var stderr = new DataTrigger { Binding = new Binding(kindPath), Value = "stderr" };
        stderr.Setters.Add(new Setter(TextBlock.ForegroundProperty, new DynamicResourceExtension("DangerBrush")));
        style.Triggers.Add(stderr);

        // A thought row is dim (Ruling 82; the split's row as amended) — the same style, both surfaces.
        var thought = new DataTrigger { Binding = new Binding(kindPath), Value = Coalesce.ThoughtKind };
        thought.Setters.Add(new Setter(TextBlock.ForegroundProperty, new DynamicResourceExtension("TextMutedBrush")));
        style.Triggers.Add(thought);
        return style;
    }

    /// <summary>The boxed reason on a failed, stopped or waiting last turn — plain text; its sentence is the container's HelpText.</summary>
    private static FrameworkElementFactory ReasonBox()
    {
        var box = F(typeof(Border));
        box.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        box.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        box.SetValue(Border.PaddingProperty, new Thickness(10, 6, 10, 6));
        box.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 6, 0, 0));
        box.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        box.SetBinding(UIElement.VisibilityProperty, Visible(nameof(TurnItem.ShowsReasonBox)));
        box.SetValue(FrameworkElement.StyleProperty, ReasonBoxStyle());

        var text = Text(nameof(TurnItem.HelpText), 12, "TextBrush", wrap: true);
        box.AppendChild(text);
        return box;
    }

    private FrameworkElementFactory ActionRow()
    {
        var actions = F(typeof(ItemsControl));
        actions.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(TurnItem.Actions)));
        actions.SetBinding(UIElement.VisibilityProperty, Visible(nameof(TurnItem.HasActions)));
        actions.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 6, 0, 0));
        actions.SetValue(KeyboardNavigation.IsTabStopProperty, false);
        actions.SetValue(UIElement.FocusableProperty, false);
        var panel = F(typeof(WrapPanel));
        actions.SetValue(ItemsControl.ItemsPanelProperty, new ItemsPanelTemplate(panel));

        var button = F(typeof(Button));
        button.SetBinding(ContentControl.ContentProperty, new Binding(".") { Converter = ActionWordConverter.Instance });
        button.SetBinding(FrameworkElement.TagProperty, new Binding("."));
        button.SetValue(FrameworkElement.MinHeightProperty, 24.0);
        button.SetValue(FrameworkElement.MinWidthProperty, 24.0);
        button.SetValue(Control.PaddingProperty, new Thickness(10, 3, 10, 3));
        button.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 4));
        button.SetValue(Button.IsDefaultProperty, false);
        button.SetValue(KeyboardNavigation.TabIndexProperty, 0);   // the entry stop (SC8 as amended)
        button.SetBinding(AutomationProperties.HelpTextProperty, new Binding(".") { Converter = ActionHelpConverter.Instance });
        button.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnActionClick));
        actions.SetValue(ItemsControl.ItemTemplateProperty, new DataTemplate(typeof(TurnActionKind)) { VisualTree = button });
        return actions;
    }

    /// <summary>
    /// A disclosure = an <see cref="Expander"/> (native ExpandCollapse), not focusable itself, its
    /// state two-way on the row, its header the one stop (P8; spike Q11, Q13).
    /// </summary>
    private static FrameworkElementFactory Disclosure(string? header, string? nameBinding, string openBinding, string? headerBinding = null)
    {
        var expander = F(typeof(Expander));
        expander.SetValue(FrameworkElement.StyleProperty, new DynamicResourceExtension(DisclosureStyleKey));
        if (headerBinding is null)
        {
            expander.SetValue(HeaderedContentControl.HeaderProperty, header);
        }
        else
        {
            expander.SetBinding(HeaderedContentControl.HeaderProperty, new Binding(headerBinding));
        }

        if (nameBinding is not null)
        {
            expander.SetBinding(AutomationProperties.NameProperty, new Binding(nameBinding));
        }

        expander.SetBinding(Expander.IsExpandedProperty, new Binding(openBinding) { Mode = BindingMode.TwoWay });
        expander.SetValue(UIElement.FocusableProperty, false);
        expander.SetValue(KeyboardNavigation.IsTabStopProperty, false);
        expander.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 12, 0));
        return expander;
    }

    /// <summary>
    /// The keyed <c>FeedDisclosure</c> style: an explicit template whose header toggle draws the
    /// 2 px focus ring (DS-1 P8). Internal so the composer's two disclosures wear the same ring
    /// rather than the platform's dotted adorner (DESIGN.md:124 — one focus ring on the page).
    /// </summary>
    internal static Style DisclosureStyle()
    {
        var root = F(typeof(StackPanel));

        var toggle = F(typeof(ToggleButton), "HeaderSite");
        // TWO-WAY, as the stock Expander template binds its HeaderSite (Ruling 96). A TemplateBinding
        // is one-way, templated parent to child: the click checked the toggle, the chevron rotated,
        // and IsExpanded never moved — so the ExpandSite trigger never fired and the reader got 0 px
        // (the operator's screenshot: chevron down, nothing beneath; the ledger: compiled null).
        // Every oracle had set IsExpanded through the property and was green.
        toggle.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(Expander.IsExpanded)) { RelativeSource = RelativeSource.TemplatedParent, Mode = BindingMode.TwoWay });
        toggle.SetValue(ContentControl.ContentProperty, new TemplateBindingExtension(HeaderedContentControl.HeaderProperty));
        toggle.SetValue(ContentControl.ContentTemplateProperty, new TemplateBindingExtension(HeaderedContentControl.HeaderTemplateProperty));
        toggle.SetValue(FrameworkElement.MinHeightProperty, 24.0);
        toggle.SetValue(FrameworkElement.MinWidthProperty, 24.0);
        toggle.SetValue(Control.PaddingProperty, new Thickness(2, 0, 4, 0));
        toggle.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        toggle.SetValue(Control.TemplateProperty, HeaderToggleTemplate());
        toggle.SetValue(FrameworkElement.FocusVisualStyleProperty, null);
        toggle.SetValue(AutomationProperties.NameProperty, new TemplateBindingExtension(AutomationProperties.NameProperty));
        // The stop is the toggle, so the explanation an AT reads at the stop must be on it too.
        toggle.SetValue(AutomationProperties.HelpTextProperty, new TemplateBindingExtension(AutomationProperties.HelpTextProperty));
        root.AppendChild(toggle);

        var content = F(typeof(ContentPresenter), "ExpandSite");
        content.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
        content.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
        root.AppendChild(content);

        var template = new ControlTemplate(typeof(Expander)) { VisualTree = root };
        var expanded = new Trigger { Property = Expander.IsExpandedProperty, Value = true };
        expanded.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "ExpandSite"));
        template.Triggers.Add(expanded);

        var style = new Style(typeof(Expander));
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        return style;
    }

    private static ControlTemplate HeaderToggleTemplate()
    {
        var ring = F(typeof(Border), "Ring");
        ring.SetValue(Border.BorderThicknessProperty, new Thickness(2));
        ring.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
        ring.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        ring.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        ring.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
        ring.SetValue(Border.SnapsToDevicePixelsProperty, true);

        var inner = F(typeof(StackPanel));
        inner.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

        var chevron = F(typeof(Path), "Chevron");
        chevron.SetValue(Path.DataProperty, Geometry.Parse("M 0,0 L 4,4 L 0,8"));
        chevron.SetValue(Shape.StrokeThicknessProperty, 1.5);
        chevron.SetResourceReference(Shape.StrokeProperty, "AccentBrush");
        chevron.SetValue(FrameworkElement.WidthProperty, 6.0);
        chevron.SetValue(FrameworkElement.HeightProperty, 10.0);
        chevron.SetValue(Shape.StretchProperty, Stretch.Uniform);
        chevron.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 0, 6, 0));
        chevron.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        chevron.SetValue(UIElement.RenderTransformOriginProperty, new Point(0.5, 0.5));
        chevron.SetValue(UIElement.RenderTransformProperty, new RotateTransform(0));
        inner.AppendChild(chevron);

        var text = F(typeof(ContentPresenter));
        text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        text.SetValue(System.Windows.Documents.TextElement.FontSizeProperty, 12.0);
        text.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "AccentBrush");
        inner.AppendChild(text);
        ring.AppendChild(inner);

        var template = new ControlTemplate(typeof(ToggleButton)) { VisualTree = ring };
        var focused = new Trigger { Property = UIElement.IsKeyboardFocusedProperty, Value = true };
        focused.Setters.Add(new Setter(Border.BorderBrushProperty, new DynamicResourceExtension("FocusBrush"), "Ring"));
        template.Triggers.Add(focused);
        var isChecked = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
        isChecked.Setters.Add(new Setter(UIElement.RenderTransformProperty, new RotateTransform(90), "Chevron"));
        template.Triggers.Add(isChecked);
        return template;
    }

    private static Style OutcomeWordStyle()
    {
        // The base ink is the style's setter, the states its triggers — never on the template
        // (the template's value would outrank every trigger and the five colours would never paint).
        var style = new Style(typeof(ThreadText));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, new DynamicResourceExtension("TextBrush")));
        void Colour(TurnState state, string brush)
        {
            var trigger = new DataTrigger { Binding = new Binding(nameof(TurnItem.State)), Value = state };
            trigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, new DynamicResourceExtension(brush)));
            style.Triggers.Add(trigger);
        }

        Colour(TurnState.Completed, "VerifiedBrush");
        Colour(TurnState.Answered, "VerifiedBrush");
        Colour(TurnState.Failed, "DangerBrush");
        Colour(TurnState.Waiting, "InferredBrush");
        Colour(TurnState.Stopped, "TextMutedBrush");
        Colour(TurnState.NotRecorded, "TextMutedBrush");
        Colour(TurnState.Queued, "TextMutedBrush");
        Colour(TurnState.Cancelled, "TextMutedBrush");
        return style;
    }

    private static Style ReasonBoxStyle()
    {
        var style = new Style(typeof(Border));
        void Colour(TurnState state, string brush)
        {
            var trigger = new DataTrigger { Binding = new Binding(nameof(TurnItem.State)), Value = state };
            trigger.Setters.Add(new Setter(Border.BorderBrushProperty, new DynamicResourceExtension(brush)));
            style.Triggers.Add(trigger);
        }

        Colour(TurnState.Failed, "DangerBrush");
        Colour(TurnState.Stopped, "BorderBrush");
        Colour(TurnState.Waiting, "InferredBrush");
        Colour(TurnState.Queued, "BorderBrush");
        return style;
    }

    /// <summary>The check / cross / pause / stop marks beside the word; hidden while running (the ring shows then).</summary>
    private static Style MarkStyle()
    {
        var style = new Style(typeof(Path));
        style.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
        void Mark(TurnState state, string geometry, string brush)
        {
            var trigger = new DataTrigger { Binding = new Binding(nameof(TurnItem.State)), Value = state };
            trigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible));
            trigger.Setters.Add(new Setter(Path.DataProperty, Geometry.Parse(geometry)));
            trigger.Setters.Add(new Setter(Shape.StrokeProperty, new DynamicResourceExtension(brush)));
            style.Triggers.Add(trigger);
        }

        Mark(TurnState.Completed, "M 1,6 L 4.5,9.5 L 11,2", "VerifiedBrush");
        Mark(TurnState.Answered, "M 1,6 L 4.5,9.5 L 11,2", "VerifiedBrush");
        Mark(TurnState.Failed, "M 1,1 L 11,11 M 11,1 L 1,11", "DangerBrush");
        Mark(TurnState.Waiting, "M 3,1 L 3,11 M 9,1 L 9,11", "InferredBrush");
        Mark(TurnState.Stopped, "M 2,2 L 10,2 L 10,10 L 2,10 Z", "TextMutedBrush");
        Mark(TurnState.NotRecorded, "M 6,2 L 6,7 M 6,10 L 6,10.5", "TextMutedBrush");
        Mark(TurnState.Queued, "M 6,2 L 6,6 L 9,8 M 1.5,6 A 4.5,4.5 0 1 0 10.5,6 A 4.5,4.5 0 1 0 1.5,6", "TextMutedBrush");
        Mark(TurnState.Cancelled, "M 2,2 L 10,2 L 10,10 L 2,10 Z", "TextMutedBrush");
        return style;
    }

    /// <summary>
    /// A running ring: visible while the bound <paramref name="path"/> equals <paramref name="runningValue"/>
    /// (the turn's state, or a tool item's <c>IsRunning</c>), spinning unless motion is reduced (the
    /// seam), stopped on rebind (ExitActions) — the one moving element on the surface.
    /// </summary>
    private static Style RingStyle(string path, object runningValue)
    {
        var style = new Style(typeof(Ellipse));
        style.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
        style.Setters.Add(new Setter(Shape.StrokeProperty, new DynamicResourceExtension("TextBrush")));

        var running = new DataTrigger { Binding = new Binding(path), Value = runningValue };
        running.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible));
        style.Triggers.Add(running);

        var spin = new MultiDataTrigger();
        spin.Conditions.Add(new System.Windows.Condition(new Binding(path), runningValue));
        spin.Conditions.Add(new System.Windows.Condition(
            new Binding(nameof(IsMotionReduced)) { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ThreadFeed), 1) },
            false));
        var storyboard = new Storyboard();
        var angle = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1.2)) { RepeatBehavior = RepeatBehavior.Forever };
        Storyboard.SetTargetProperty(angle, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
        storyboard.Children.Add(angle);
        var begin = new BeginStoryboard { Storyboard = storyboard, Name = "Spin" };
        spin.EnterActions.Add(begin);
        spin.ExitActions.Add(new StopStoryboard { BeginStoryboardName = "Spin" });
        style.Triggers.Add(spin);

        // A StopStoryboard resolves its name in the STYLE's name scope; XAML registers x:Name there,
        // code must — without it the first outcome threw "'Spin' name cannot be found" inside Apply
        // and the feed stopped (THR-0001), caught by A4 before it reached an operator.
        ((INameScope)style).RegisterName("Spin", begin);
        return style;
    }

    /// <summary>The reduced-motion seam as a bindable property: read once per bind through <see cref="ReducedMotion"/>.</summary>
    public bool IsMotionReduced => ReducedMotion();

    private static readonly BooleanToVisibilityConverter BooleanToVisibility = new();

    internal static Binding Visible(string path) => new(path) { Converter = BooleanToVisibility };

    private sealed class ActionWordConverter : IValueConverter
    {
        public static readonly ActionWordConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is TurnActionKind kind ? ThreadAnnouncementPolicy.ActionWord(kind) : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    private sealed class ActionHelpConverter : IValueConverter
    {
        public static readonly ActionHelpConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
        {
            TurnActionKind.Stop => "edits so far stay on disk and are listed on the turn",
            TurnActionKind.Cancel => "drops the queued turn; its words go back to the editor",
            TurnActionKind.SendNow => "sends the queued turn as its own run",
            _ => string.Empty,
        };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
