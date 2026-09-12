using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// The virtualized feed base the session thread and the Console split share (DS-1 P1, P2, P7):
/// a <c>ListBox</c> over a recycling <c>VirtualizingStackPanel</c> with pixel
/// scrolling, the six owned keys as a pure decision plus an act, the follow rule's structural anchor,
/// and containers that carry their own template so the theme's selection band never paints
/// the reading caret.
/// </summary>
/// <remarks>
/// <para><b>The theme never reaches a subclass on its own.</b> <c>App.xaml</c> delivers the whole
/// theme by implicit per-type styles, which bind an exact <c>TargetType</c>; a <c>FeedList</c> would
/// fall to Aero2's white ground and black ink. So the ink and the ground are set here by resource
/// reference (the <c>SurfaceChrome</c> idiom) and the container carries its own template
/// (spike Q11–Q13; DC-139).</para>
///
/// <para><b>The feed owns its keys from the container and from any inner control.</b> The
/// platform's PageDown moves by a page (17 items from 0), its End lands on 38 of 40 and its Up from
/// the last turn does not move in a variable-height, pixel-virtualized list (spike Q5a, Q5c) — so
/// PageDown / PageUp move the caret by one item, Home / End by index, Up / Down scroll three text
/// lines, and Ctrl+Home / Ctrl+End raise leaves. A source that owns its keys (an expanded compiled
/// scroller, a text box) keeps them (<see cref="SourceOwnsItsKeys"/>); Ctrl+PageUp / PageDown stay
/// the pane switch.</para>
///
/// <para><b>The selection is the reading caret, never intent</b> (a recorded deviation): single
/// selection, no selection ground, the 2 px focus ring the one visible state.</para>
/// </remarks>
public abstract class FeedList : ListBox, ICanvasFocusTarget
{
    /// <summary>The 13 px UI type's line box (DESIGN.md: 13 px, 1.5) — what Up / Down scroll by.</summary>
    public const double LineHeight = 13 * 1.5;

    /// <summary>How many text lines Up / Down scroll the viewport (DS-1 P2).</summary>
    public const int ScrollLines = 3;

    private ScrollViewer? _scroller;

    protected FeedList()
    {
        // P1 — the measured shape (spike Q1–Q3, Q12, Q15): Recycling and Pixel are NOT defaults.
        VirtualizingPanel.SetIsVirtualizing(this, true);
        VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Recycling);
        VirtualizingPanel.SetScrollUnit(this, ScrollUnit.Pixel);
        ScrollViewer.SetCanContentScroll(this, true);
        ScrollViewer.SetHorizontalScrollBarVisibility(this, ScrollBarVisibility.Disabled);

        // Once + Contained are the ListBox defaults (Q12); stated so a future change is a diff.
        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Once);
        KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.Contained);

        SelectionMode = SelectionMode.Single;
        BorderThickness = new Thickness(0);
        Padding = new Thickness(0);
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        FocusVisualStyle = null;

        // Ink and ground by resource reference — the implicit ListBox style binds the exact type
        // and never reaches this subclass (DC-139: a theme setter outranks an inherited value).
        SetResourceReference(ForegroundProperty, "TextBrush");
        SetResourceReference(BackgroundProperty, "SurfaceRaisedBrush");

        ItemContainerStyle = ContainerStyle();

        GotKeyboardFocus += OnGotKeyboardFocus;
    }

    /// <summary>Raised on Ctrl+Home / Ctrl+End; the document routes the leave.</summary>
    public event Action<FocusLeave>? FocusLeaveRequested;

    /// <summary>
    /// Whether the source of a key press owns its keys: a <see cref="ScrollViewer"/> or a
    /// <see cref="TextBoxBase"/> in its ancestry inside this list. The list's own scroll viewer is
    /// not such a source — it is the feed.
    /// </summary>
    public bool SourceOwnsItsKeys(DependencyObject? source)
    {
        var owns = false;
        for (var node = source; node is not null; node = ParentOf(node))
        {
            if (ReferenceEquals(node, this))
            {
                return owns;   // the walk reached the list: the answer is whether an owner sat inside it
            }

            if ((node is ScrollViewer viewer && !ReferenceEquals(viewer, Scroller)) || node is TextBoxBase)
            {
                owns = true;
            }
        }

        return false;   // not inside this list at all
    }

    /// <summary>The pure decision (K1a): never throws over the whole domain; <c>None</c> for keys the feed does not own.</summary>
    public static FeedKeyDecision Decide(Key key, ModifierKeys modifiers, bool sourceOwnsItsKeys)
    {
        var control = (modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        var alt = (modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;

        if (alt)
        {
            return new FeedKeyDecision.None();
        }

        // Ctrl+Home / Ctrl+End leave the feed from the container or from an inner control alike.
        if (control)
        {
            return key switch
            {
                Key.Home => new FeedKeyDecision.Leave(FocusLeave.ToHeader),
                Key.End => new FeedKeyDecision.Leave(FocusLeave.ToEditor),
                _ => new FeedKeyDecision.None(),   // Ctrl+PageUp / PageDown: the pane switch, untouched
            };
        }

        if (sourceOwnsItsKeys)
        {
            return new FeedKeyDecision.None();
        }

        return key switch
        {
            Key.PageDown => new FeedKeyDecision.MoveBy(+1),
            Key.PageUp => new FeedKeyDecision.MoveBy(-1),
            Key.Home => new FeedKeyDecision.MoveTo(First: true),
            Key.End => new FeedKeyDecision.MoveTo(First: false),
            Key.Down => new FeedKeyDecision.Scroll(+ScrollLines),
            Key.Up => new FeedKeyDecision.Scroll(-ScrollLines),
            _ => new FeedKeyDecision.None(),
        };
    }

    /// <summary>The act (K1b). Returns whether the decision was the feed's.</summary>
    protected bool Act(FeedKeyDecision decision)
    {
        switch (decision)
        {
            case FeedKeyDecision.MoveBy move:
                if (Items.Count > 0)
                {
                    var current = SelectedIndex < 0 ? Items.Count - 1 : SelectedIndex;
                    FocusItem(Math.Clamp(current + move.Delta, 0, Items.Count - 1));
                }

                return true;

            case FeedKeyDecision.MoveTo to:
                if (Items.Count > 0)
                {
                    FocusItem(to.First ? 0 : Items.Count - 1);
                }

                return true;

            case FeedKeyDecision.Scroll scroll:
                if (Scroller is { } viewer)
                {
                    viewer.ScrollToVerticalOffset(viewer.VerticalOffset + scroll.Lines * LineHeight);
                }

                return true;

            case FeedKeyDecision.Leave leave:
                FocusLeaveRequested?.Invoke(leave.To);
                return true;

            default:
                return false;
        }
    }

    /// <summary>The caret's item, realized and focused on its container — F6 / Tab entry (spike Q5g).</summary>
    public bool FocusCurrentItem()
    {
        if (Items.Count == 0)
        {
            return false;
        }

        return FocusItem(SelectedIndex < 0 ? Items.Count - 1 : SelectedIndex);
    }

    /// <summary>Backward entry (Shift+Tab from the composer): the caret's item's last tab stop, or its container when it has none.</summary>
    public bool FocusCurrentItemLast()
    {
        if (!FocusCurrentItem())
        {
            return false;
        }

        if (ItemContainerGenerator.ContainerFromIndex(SelectedIndex) is not ListBoxItem container)
        {
            return false;
        }

        var stops = TabStops(container).ToList();
        return stops.Count == 0 || stops[^1].Focus();
    }

    /// <summary>Selects, scrolls into view, lays out and focuses the CONTAINER at <paramref name="index"/> — never an inner control.</summary>
    public bool FocusItem(int index)
    {
        if (index < 0 || index >= Items.Count)
        {
            return false;
        }

        SelectedIndex = index;
        ScrollIntoView(Items[index]);
        UpdateLayout();

        if (ItemContainerGenerator.ContainerFromIndex(index) is ListBoxItem container)
        {
            return container.Focus();
        }

        // ScrollIntoView defers container generation to a dispatcher operation when the item was
        // unrealized (spike Q11's note); one more layout pass realizes it.
        UpdateLayout();
        return ItemContainerGenerator.ContainerFromIndex(index) is ListBoxItem realized && realized.Focus();
    }

    /// <summary>The scroll viewer inside the template, once the template has applied.</summary>
    protected ScrollViewer? Scroller
    {
        get
        {
            if (_scroller is null)
            {
                ApplyTemplate();
                _scroller = FindScroller(this);
            }

            return _scroller;
        }
    }

    /// <summary>
    /// The structural pin (P7): the last item's container is realized and its bottom edge sits
    /// within the viewport — read BEFORE a change, never from the offset (the extent is an
    /// estimate under variable heights; spike Q4a).
    /// </summary>
    public bool IsPinnedAtEnd
    {
        get
        {
            if (Items.Count == 0)
            {
                return true;
            }

            if (Scroller is not { } viewer
                || ItemContainerGenerator.ContainerFromIndex(Items.Count - 1) is not FrameworkElement last
                || !last.IsArrangeValid)
            {
                return false;
            }

            var bottom = last.TransformToAncestor(viewer).Transform(new Point(0, last.ActualHeight)).Y;
            return bottom <= viewer.ViewportHeight + 1;
        }
    }

    /// <summary>Scrolls to the end — called after the layout that added items, only when the feed was pinned before it.</summary>
    public void ScrollToEndOfFeed() => Scroller?.ScrollToEnd();

    /// <summary>How many containers the panel has realized — the 40-turn oracle's number (L2).</summary>
    public int RealizedContainers =>
        Enumerable.Range(0, Items.Count).Count(i => ItemContainerGenerator.ContainerFromIndex(i) is not null);

    // ── ICanvasFocusTarget: the thread as one region of the document's F6 cycle (P4) ──

    /// <inheritdoc/>
    public bool IsReady => Items.Count > 0;

    /// <inheritdoc/>
    public bool IsObscured => false;

    /// <inheritdoc/>
    public bool TryFocus() => FocusCurrentItem();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var decision = Decide(e.Key, e.KeyboardDevice.Modifiers, SourceOwnsItsKeys(e.OriginalSource as DependencyObject));
        if (Act(decision))
        {
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    /// <summary>
    /// Focus landing on the list itself (Tab under Once with an unrealized caret) is re-routed to
    /// the caret's container. Entry from the header by Tab is the document's to route (P4: it calls
    /// <see cref="FocusCurrentItem"/> before the platform picks a realized container); a click on a
    /// turn is that turn's, never re-routed.
    /// </summary>
    private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (ReferenceEquals(e.NewFocus, this))
        {
            FocusCurrentItem();
        }
    }

    /// <summary>The focusable tab stops inside one container, in tree order.</summary>
    protected static IEnumerable<UIElement> TabStops(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is UIElement { Focusable: true, IsVisible: true } element && KeyboardNavigation.GetIsTabStop(element))
            {
                yield return element;
            }

            foreach (var inner in TabStops(child))
            {
                yield return inner;
            }
        }
    }

    private static DependencyObject? ParentOf(DependencyObject node) =>
        node is Visual or System.Windows.Media.Media3D.Visual3D
            ? VisualTreeHelper.GetParent(node) ?? LogicalTreeHelper.GetParent(node)
            : LogicalTreeHelper.GetParent(node);

    private static ScrollViewer? FindScroller(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer viewer)
            {
                return viewer;
            }

            if (FindScroller(child) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// The container's own template (Q11–Q13; DC-139): a transparent 2 px border that lights
    /// <c>{colors.focus}</c> on <c>IsKeyboardFocused</c> — not <c>FocusWithin</c>, so an inner stop
    /// never lights two rings — and no selection or hover ground.
    /// </summary>
    protected static Style ContainerStyle()
    {
        var border = new FrameworkElementFactory(typeof(Border), "Ring");
        border.SetValue(Border.BorderThicknessProperty, new Thickness(2));
        border.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
        border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        border.SetValue(Border.SnapsToDevicePixelsProperty, true);
        border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(PaddingProperty));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(ContentControl.ContentTemplateProperty));
        presenter.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
        border.AppendChild(presenter);

        var template = new ControlTemplate(typeof(ListBoxItem)) { VisualTree = border };
        var focused = new Trigger { Property = IsKeyboardFocusedProperty, Value = true };
        focused.Setters.Add(new Setter(Border.BorderBrushProperty, new DynamicResourceExtension("FocusBrush"), "Ring"));
        template.Triggers.Add(focused);

        var style = new Style(typeof(ListBoxItem));
        style.Setters.Add(new Setter(TemplateProperty, template));
        style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        style.Setters.Add(new Setter(PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(MarginProperty, new Thickness(0)));
        style.Setters.Add(new Setter(FocusVisualStyleProperty, null));
        style.Setters.Add(new Setter(BackgroundProperty, Brushes.Transparent));
        return style;
    }
}
