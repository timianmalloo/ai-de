using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// The rail's destinations (spec §C4 as amended; DESIGN.md PS-R2): the three perspectives as a
/// <b>single-selection group with manual activation</b>. Up/Down move focus only; Space, Enter or
/// a click activate; Tab leaves the rail (roving tab-stop); activating the selected item is the
/// presenter's no-op. The selected state is the real one — exposed to UIA through
/// <c>SelectionItemPattern</c> on a vertical tab list — never a name suffix (SC 4.1.2).
/// </summary>
/// <remarks>
/// <para><b>Why not a <see cref="RadioButton"/> group or a bare <see cref="TabControl"/>.</b> Both
/// select on arrow: the first Down would switch the perspective and make the <i>opening</i> state
/// ("focus stays on the trigger until the body's first layout pass") impossible. A
/// <see cref="ListBox"/> also selects on arrow and on mouse-down, so this control intercepts those
/// gestures before the list sees them and raises <see cref="ActivationRequested"/> instead; the
/// <b>only</b> writer of the selection is <see cref="Reflect"/>, which the window calls from the
/// presenter's own change event — so the rail can never show a destination the body is not.</para>
///
/// <para><b>Headless by construction.</b> The activation model is the class, not the window, so
/// <c>PerspectiveRailTests</c> drives it on a plain STA thread: Down does not switch; Enter does.
/// The pixels (the 3px bar, the accent glyph, the 44 × 44 target) are the item style in
/// <c>MainWindow.xaml</c> and are proven by the runtime UIA walk (P-1).</para>
/// </remarks>
public sealed class PerspectiveRail : ListBox
{
    public PerspectiveRail()
    {
        SelectionMode = SelectionMode.Single;
        ItemsSource = PerspectiveSet.All;
        SelectedItem = PerspectiveSet.Initial;
        Background = Brushes.Transparent;
        BorderThickness = new Thickness(0);
        Padding = new Thickness(0);
        Focusable = false;   // the items are the tab stops, entered once (KeyboardNavigation.TabNavigation=Once)

        IsTextSearchEnabled = false;   // typeahead would select on a keystroke

        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Once);
        KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.None);   // arrows are ours
        AutomationProperties.SetName(this, "Perspectives");
    }

    // The one destination the presenter last reflected, and whether Reflect is the caller of a
    // selection change — every other writer of the selection (a page key, a right-click, a UIA
    // Select, a programmatic SelectedItem) is reverted and turned into an activation request.
    private Perspective _active = PerspectiveSet.Initial;
    private bool _reflecting;

    /// <summary>Raised when the operator activates a destination — click, Enter or Space. The window runs the perspective's catalog command.</summary>
    public event EventHandler<Perspective>? ActivationRequested;

    /// <summary>The containers, in rail order, once generated.</summary>
    public IEnumerable<PerspectiveRailItem> Destinations =>
        PerspectiveSet.All.Select(ItemContainerGenerator.ContainerFromItem).OfType<PerspectiveRailItem>();

    /// <summary>The container of <paramref name="perspective"/>, or null before the containers exist.</summary>
    public PerspectiveRailItem? ItemFor(Perspective perspective) =>
        ItemContainerGenerator.ContainerFromItem(perspective) as PerspectiveRailItem;

    /// <summary>
    /// Shows <paramref name="active"/> as the selected destination — the ONE writer of the selection,
    /// called from the presenter's change event so the rail follows the body and never leads it.
    /// </summary>
    public void Reflect(Perspective active)
    {
        ArgumentNullException.ThrowIfNull(active);
        _active = active;
        _reflecting = true;
        try
        {
            SelectedItem = active;
        }
        finally
        {
            _reflecting = false;
        }

        ItemFor(active)?.ClearFailure();

        // Tab enters the group on the selected destination (WAI-ARIA tabs), not on the first — by
        // TAB INDEX, never by clearing IsTabStop: the rail is a Once group that re-enters on its
        // last-focused item, and a last-focused item that is not a tab stop makes the whole group
        // unenterable after the explore-and-leave flow (the UX & Accessibility reviewer's second
        // blocker, observed red). Every destination stays a tab stop; the selected one is first.
        foreach (var item in Destinations)
        {
            KeyboardNavigation.SetTabIndex(item, ReferenceEquals(item.Row, active) ? 0 : 1);
        }
    }

    /// <summary>
    /// Any selection change that <see cref="Reflect"/> did not make is the list selecting on its
    /// own — reverted, and the attempted destination raised as an activation request instead.
    /// </summary>
    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);
        if (_reflecting)
        {
            return;
        }

        var attempted = e.AddedItems.OfType<Perspective>().FirstOrDefault();
        _reflecting = true;
        try
        {
            SelectedItem = _active;
        }
        finally
        {
            _reflecting = false;
        }

        if (attempted is not null && attempted != _active)
        {
            Activate(attempted);
        }
    }

    /// <summary>Puts a destination into its error state (spec §C4): the body failed to build; activation retries.</summary>
    public void ShowFailure(Perspective perspective, string reason) => ItemFor(perspective)?.ShowFailure(reason);

    protected override DependencyObject GetContainerForItemOverride() => new PerspectiveRailItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is PerspectiveRailItem;

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is PerspectiveRailItem container && item is Perspective row)
        {
            container.Bind(row);
            KeyboardNavigation.SetTabIndex(container, row == _active ? 0 : 1);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new PerspectiveRailPeer(this);

    /// <summary>Up/Down/Home/End move focus within the group; Enter and Space activate the focused destination. Nothing else is touched.</summary>
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        var items = Destinations.ToList();
        var focused = items.FirstOrDefault(i => i.IsKeyboardFocusWithin);

        switch (e.Key)
        {
            case Key.Down or Key.Up or Key.Home or Key.End or Key.PageUp or Key.PageDown when items.Count > 0:
                var at = focused is null ? -1 : items.IndexOf(focused);
                var next = e.Key switch
                {
                    Key.Home or Key.PageUp => 0,
                    Key.End or Key.PageDown => items.Count - 1,
                    Key.Down => (at + 1 + items.Count) % items.Count,
                    _ => (at - 1 + items.Count) % items.Count,
                };
                items[next].Focus();
                e.Handled = true;   // the list never sees the arrow, so it never selects on it
                return;

            case Key.Enter or Key.Space when focused?.Row is { } row:
                Activate(row);
                e.Handled = true;
                return;
        }

        base.OnPreviewKeyDown(e);
    }

    /// <summary>A click activates — the list is not allowed to select on mouse-down.</summary>
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (ContainerOf(e.OriginalSource as DependencyObject) is { Row: { } row } container)
        {
            container.Focus();
            Activate(row);
            e.Handled = true;
            return;
        }

        base.OnPreviewMouseLeftButtonDown(e);
    }

    /// <summary>A right-click selects in a ListBox; here it does nothing (no destination has a context menu).</summary>
    protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
    {
        if (ContainerOf(e.OriginalSource as DependencyObject) is not null)
        {
            e.Handled = true;
            return;
        }

        base.OnPreviewMouseRightButtonDown(e);
    }

    private PerspectiveRailItem? ContainerOf(DependencyObject? element) =>
        element is null ? null : ContainerFromElement(element) as PerspectiveRailItem;

    private void Activate(Perspective row) => ActivationRequested?.Invoke(this, row);

    /// <summary>
    /// The rail as a vertical tab list to UIA (PS-R2), its items as tab items with the selection
    /// pattern — whose <c>Select()</c> is an ACTIVATION request, never a direct write of the
    /// selection (the pattern the role advertises must not desync the rail from the body).
    /// </summary>
    private sealed class PerspectiveRailPeer(PerspectiveRail owner) : ListBoxAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;

        protected override string GetClassNameCore() => nameof(PerspectiveRail);

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item) => new ItemPeer(item, this, owner);

        public override object GetPattern(PatternInterface patternInterface) =>
            patternInterface == PatternInterface.Selection
                ? new RequiredSelection((ISelectionProvider)base.GetPattern(PatternInterface.Selection))
                : base.GetPattern(patternInterface);

        /// <summary>A tab list always has one selected item.</summary>
        private sealed class RequiredSelection(ISelectionProvider inner) : ISelectionProvider
        {
            public bool CanSelectMultiple => false;

            public bool IsSelectionRequired => true;

            public IRawElementProviderSimple[] GetSelection() => inner.GetSelection();
        }

        private sealed class ItemPeer(object item, SelectorAutomationPeer owner, PerspectiveRail rail) : ListBoxItemAutomationPeer(item, owner)
        {
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;

            protected override string GetClassNameCore() => nameof(PerspectiveRailItem);

            public override object GetPattern(PatternInterface patternInterface) =>
                patternInterface == PatternInterface.SelectionItem ? new ActivatingSelection(this, rail, Item) : base.GetPattern(patternInterface);

            /// <summary>The selection-item pattern over a destination: reads the real state; a Select is an activation request.</summary>
            private sealed class ActivatingSelection(ItemPeer peer, PerspectiveRail rail, object item) : ISelectionItemProvider
            {
                private ISelectionItemProvider Inner => (ISelectionItemProvider)peer.GetInnerSelection();

                public bool IsSelected => Inner.IsSelected;

                public IRawElementProviderSimple SelectionContainer => Inner.SelectionContainer;

                public void Select()
                {
                    if (item is Perspective row)
                    {
                        rail.Activate(row);
                    }
                }

                public void AddToSelection() => Select();

                public void RemoveFromSelection()
                {
                    // A tab list always has one selected item; removing is not an operation.
                }
            }

            private object GetInnerSelection() => base.GetPattern(PatternInterface.SelectionItem);
        }
    }
}

/// <summary>
/// One destination on the rail: its row, its constant accessible name (<i>"&lt;Title&gt; perspective"</i>
/// — never suffixed with state), its tooltip with the keystroke rendered from the bound gesture
/// (PS-R4), and its error state. The visual states are the item style in <c>MainWindow.xaml</c>.
/// </summary>
public sealed class PerspectiveRailItem : ListBoxItem
{
    /// <summary>The body-failure reason while the destination is in its error state; null otherwise.</summary>
    public static readonly DependencyProperty FailureProperty = DependencyProperty.Register(
        nameof(Failure), typeof(string), typeof(PerspectiveRailItem), new PropertyMetadata(null));

    /// <summary>True while <see cref="Failure"/> is set — what the item style's error trigger reads.</summary>
    public static readonly DependencyProperty HasFailureProperty = DependencyProperty.Register(
        nameof(HasFailure), typeof(bool), typeof(PerspectiveRailItem), new PropertyMetadata(false));

    /// <summary>The glyph, resolved from the icon registry by the row's title (<c>IconCoding</c>, <c>IconExplore</c>, <c>IconArchitecture</c> — PS-R4).</summary>
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(Geometry), typeof(PerspectiveRailItem), new PropertyMetadata(null));

    public PerspectiveRailItem()
    {
        Focusable = true;
        IsTabStop = true;

        // The target size is the control's, not the style's: 44 × 44 is SC 2.5.8's floor, and a
        // floor that lived only in a window's style would be absent wherever the style is.
        Height = 44;
        MinWidth = 44;
    }

    public Perspective? Row { get; private set; }

    public string? Failure
    {
        get => (string?)GetValue(FailureProperty);
        private set
        {
            SetValue(FailureProperty, value);
            SetValue(HasFailureProperty, value is not null);
        }
    }

    public bool HasFailure => (bool)GetValue(HasFailureProperty);

    public Geometry? Glyph => (Geometry?)GetValue(GlyphProperty);

    /// <summary>The tooltip's text: the title and the bound gesture's display string — never a typed chord (US-C10 b3).</summary>
    public static string TooltipFor(Perspective row)
    {
        ArgumentNullException.ThrowIfNull(row);

        var command = WorkbenchCommandCatalog.All.First(c => string.Equals(c.Id, row.CommandId, StringComparison.Ordinal));
        var bound = KeyGestures.For(command).FirstOrDefault()?.GetDisplayStringForCulture(CultureInfo.CurrentCulture);
        var what = row.Body == PerspectiveBody.FullWindow ? $"{row.Title} — graph & reader" : row.Title;
        return bound is null ? what : $"{what} — {bound}";
    }

    internal void Bind(Perspective row)
    {
        Row = row;
        AutomationProperties.SetName(this, $"{row.Title} perspective");
        ToolTip = TooltipFor(row);
        // A resource REFERENCE, resolved when the item enters the tree: it walks the window's
        // dictionary (where IconCoding/IconArchitecture live this horizon) and the application's
        // (IconExplore). Application.TryFindResource never searches a Window's resources — the WPF
        // lens's finding: two of three glyphs resolved to null and rendered as blank pills.
        SetResourceReference(GlyphProperty, $"Icon{row.Title}");
    }

    internal void ShowFailure(string reason)
    {
        var status = $"Couldn't open {Row?.Title}: {reason}. Activate to try again.";
        Failure = status;
        AutomationProperties.SetItemStatus(this, status);
        ToolTip = status;
    }

    internal void ClearFailure()
    {
        if (Failure is null)
        {
            return;
        }

        Failure = null;
        AutomationProperties.SetItemStatus(this, string.Empty);
        ToolTip = Row is null ? null : TooltipFor(Row);
    }
}
