using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.Core.Workbench;
using AvalonDock;
using AvalonDock.Layout;
using CoreOrientation = AiDe.Core.Workbench.Orientation;

namespace AiDe.App.Workbench;

/// <summary>
/// Renders the owned <see cref="Layout"/> model into AvalonDock, and supplies the accessibility the
/// library does not (ADR-0012).
/// </summary>
/// <remarks>
/// The adapter is deliberately **one-way**: model → view. Pointer gestures enter as
/// <see cref="LayoutOperation"/> requests through <see cref="ILayoutService.Apply"/>, never as direct
/// view mutations — that is what keeps the keyboard path and the drag path provably identical
/// (SC 2.5.7). The view is a projection; it is never the source of truth.
/// </remarks>
public sealed class WorkbenchAdapter
{
    /// <summary>
    /// Automation names starting with this prefix are the library's type names leaking through as
    /// accessible names — the defect the UIA probe found (spikes/avalondock-a11y).
    /// </summary>
    public const string LeakedNamePrefix = "AvalonDock.";

    private readonly ILayoutService _service;
    private readonly Func<Surface, FrameworkElement>? _contentFactory;

    public WorkbenchAdapter(
        DockingManager manager, ILayoutService service,
        Func<Surface, FrameworkElement>? contentFactory = null)
    {
        Manager = manager;
        _service = service;
        _contentFactory = contentFactory;

        // The naming pass must re-run whenever the layout changes: tabs are realized and recycled as
        // panes are docked, floated and collapsed, so a one-off startup hook would name the first
        // arrangement and silently lose every one after it.
        Manager.LayoutUpdated += (_, _) => ApplyAccessibleNames();
    }

    public DockingManager Manager { get; }

    /// <summary>Projects the current model into AvalonDock and names everything for assistive tech.</summary>
    public void Render()
    {
        // Reconcile, do not rebuild (DC-029). Reuse the content element already realized for each
        // surface that still exists, so a mutation to ONE pane (opening a terminal, splitting,
        // restoring a layout) does not reconstruct — and thereby destroy the live state of — every
        // OTHER pane. A rebuilt terminal looks identical to the one it replaced but its process is
        // gone: each ConPTY child runs in a kill-on-close job, so orphaning its surface kills it.
        var keep = _service.Current.AllStacks()
            .SelectMany(s => s.Surfaces).Select(s => s.SurfaceId)
            .ToHashSet(StringComparer.Ordinal);

        var reuse = new Dictionary<string, FrameworkElement>(StringComparer.Ordinal);
        if (Manager.Layout is { } current)
        {
            foreach (var doc in current.Descendents().OfType<LayoutDocument>())
            {
                if (doc.ContentId is not { } id || doc.Content is not FrameworkElement fe)
                {
                    continue;
                }

                if (keep.Contains(id) && !reuse.ContainsKey(id))
                {
                    // Free the element so it can re-parent into the new tree without a "already has a
                    // parent" fault when the layout is replaced below.
                    doc.Content = null;
                    reuse[id] = fe;
                }
                else if (!keep.Contains(id) && fe is IDisposable disposable)
                {
                    // A surface that was closed: end it NOW rather than at a finalizer, so a closed
                    // terminal's process stops deterministically instead of lingering until GC.
                    disposable.Dispose();
                }
            }
        }

        var panel = BuildPanel(_service.Current.Root, reuse);
        Manager.Layout = new LayoutRoot { RootPanel = panel };
        ApplyAccessibleNames();
    }

    /// <summary>
    /// Names every realized tab from the <see cref="LayoutContent.Title"/> it is bound to.
    /// </summary>
    /// <remarks>
    /// Without this, AvalonDock reports each tab's **.NET type name** — `AvalonDock.Layout.LayoutDocument`
    /// — as its accessible name, so every surface sounds identical to a screen reader
    /// (verified, spikes/avalondock-a11y). A typed `TabItem` style setting the same property does
    /// **not** reach these items; that was tested and rejected. Walking the realized visual tree does.
    /// </remarks>
    public void ApplyAccessibleNames() => NameTabs(Manager);

    internal static int NameTabs(DependencyObject root)
    {
        var named = 0;
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TabItem tab && tab.DataContext is LayoutContent content
                && !string.IsNullOrEmpty(content.Title))
            {
                AutomationProperties.SetName(tab, content.Title);
                named++;
            }

            named += NameTabs(child);
        }

        return named;
    }

    /// <summary>
    /// Every automation name in the subtree — the regression control's input.
    /// </summary>
    /// <remarks>
    /// Reflection cannot catch the leaked-name defect because it is a data-binding fault rather than
    /// a missing type, so the control has to read the realized tree.
    /// </remarks>
    internal static IEnumerable<string> AutomationNames(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is FrameworkElement fe)
            {
                var name = AutomationProperties.GetName(fe);
                if (!string.IsNullOrEmpty(name))
                {
                    yield return name;
                }

                if (fe is TabItem { DataContext: LayoutContent content } && string.IsNullOrEmpty(name))
                {
                    // An unnamed tab still reports SOMETHING to UIA — the bound object's ToString().
                    // Surfacing it here is what lets the control fail on the real defect.
                    yield return content.ToString() ?? string.Empty;
                }
            }

            foreach (var nested in AutomationNames(child))
            {
                yield return nested;
            }
        }
    }

    // ── model → AvalonDock projection ─────────────────────────────────────────────────────

    private LayoutPanel BuildPanel(LayoutNode node, IReadOnlyDictionary<string, FrameworkElement> reuse)
    {
        if (node is SplitNode split)
        {
            var panel = new LayoutPanel
            {
                Orientation = split.Orientation == CoreOrientation.Horizontal
                    ? System.Windows.Controls.Orientation.Horizontal
                    : System.Windows.Controls.Orientation.Vertical,
            };

            foreach (var child in split.Children)
            {
                switch (child)
                {
                    case SplitNode:
                        panel.Children.Add(BuildPanel(child, reuse));
                        break;
                    case StackNode stack:
                        panel.Children.Add(BuildPane(stack, reuse));
                        break;
                    default:
                        break;
                }
            }

            return panel;
        }

        var single = new LayoutPanel();
        single.Children.Add(BuildPane((StackNode)node, reuse));
        return single;
    }

    /// <summary>
    /// The content element currently hosting <paramref name="surfaceId"/>, or null.
    /// </summary>
    /// <remarks>
    /// Read from AvalonDock's own tree by <c>ContentId</c> rather than from a parallel dictionary:
    /// a second map of surface-to-content is a second thing to keep in step with a layout the user
    /// rearranges, and it would go stale exactly when a pane is moved or closed.
    /// </remarks>
    public FrameworkElement? ContentFor(string surfaceId) =>
        Manager.Layout?.Descendents().OfType<LayoutDocument>()
            .FirstOrDefault(d => string.Equals(d.ContentId, surfaceId, StringComparison.Ordinal))
            ?.Content as FrameworkElement;

    private LayoutDocumentPane BuildPane(StackNode stack, IReadOnlyDictionary<string, FrameworkElement> reuse)
    {
        var pane = new LayoutDocumentPane();
        foreach (var surface in stack.Surfaces)
        {
            var content = reuse.TryGetValue(surface.SurfaceId, out var kept)
                ? kept
                : _contentFactory?.Invoke(surface) ?? new ContentControl();

            pane.Children.Add(new LayoutDocument
            {
                // ContentId is the key AvalonDock uses to reunite a restored layout with its content,
                // so it must be the surface's stable identity, not its display title.
                ContentId = surface.SurfaceId,
                // A renamed surface carries its display name on the content itself (Design-owned
                // session state). Reconcile keeps that content instance alive across re-renders, so a
                // rename persists without a Core model change; the model title is the fallback.
                Title = (content as IHasDisplayName)?.DisplayName is { Length: > 0 } displayName
                    ? displayName
                    : surface.Title,
                Content = content,
            });
        }

        return pane;
    }
}
