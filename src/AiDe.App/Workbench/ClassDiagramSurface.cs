using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.Core.Presentation;

namespace AiDe.App.Workbench;

/// <summary>
/// The class-diagram surface (spec-uml-erm-surfaces; ADR-0020 Phase 1): a dependency-free, native WPF
/// render of the class HIERARCHY derived from the graph — classes and interfaces as cards, each showing
/// its generalizations (`inherits`) and realizations (`implements`). Member-less by construction (no
/// extractor emits members yet); the header says so rather than implying empty classes. No WebView2, so
/// none of ADR-0015's airspace concerns. A member-bearing, notation-valid Mermaid render is Phase 2
/// (gated on Core `has_member`).
/// </summary>
public sealed class ClassDiagramSurface : ContentControl
{
    private readonly StackPanel _list;
    private readonly TextBlock _header;
    private readonly TextBlock _disclosure;
    private readonly TextBox _search;
    private ClassHierarchy _full = new([], [], 0);

    public ClassDiagramSurface(string title = "Class diagram")
    {
        AutomationProperties.SetName(this, title);
        SetResourceReference(BackgroundProperty, "SurfaceBrush");

        var root = new DockPanel { LastChildFill = true, Margin = new Thickness(14, 12, 14, 12) };

        _header = Text("Class hierarchy", 14, FontWeights.SemiBold);
        DockPanel.SetDock(_header, Dock.Top);
        root.Children.Add(_header);

        _search = new TextBox { Margin = new Thickness(0, 6, 0, 0), Padding = new Thickness(6, 3, 6, 3) };
        AutomationProperties.SetName(_search, "Filter types by name");
        _search.SetResourceReference(BackgroundProperty, "SunkenBrush");
        _search.SetResourceReference(ForegroundProperty, "TextBrush");
        _search.TextChanged += (_, _) => Render(ClassHierarchyModel.Filter(_full, _search.Text));
        DockPanel.SetDock(_search, Dock.Top);
        root.Children.Add(_search);

        _disclosure = Muted("", 11.5);
        _disclosure.Margin = new Thickness(0, 6, 0, 8);
        _disclosure.TextWrapping = TextWrapping.Wrap;
        DockPanel.SetDock(_disclosure, Dock.Top);
        root.Children.Add(_disclosure);

        _list = new StackPanel();
        root.Children.Add(new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _list,
        });

        Content = root;
        Clear();
    }

    /// <summary>The number of type cards currently shown (for tests).</summary>
    public int TypeCount { get; private set; }

    /// <summary>The number of generalization/realization relations currently shown (for tests).</summary>
    public int RelationCount { get; private set; }

    public bool IsEmpty => TypeCount == 0;

    /// <summary>Builds the hierarchy from a graph and renders it (ADR-0020).</summary>
    public void ShowGraph(IReadOnlyList<CanvasNode>? nodes, IReadOnlyList<CanvasEdge>? edges) =>
        Show(ClassHierarchyModel.Build(nodes, edges));

    /// <summary>Stores and renders a prebuilt hierarchy (search re-renders a filtered view of it).</summary>
    public void Show(ClassHierarchy hierarchy)
    {
        _full = hierarchy;
        _search.Visibility = hierarchy.Types.Count > 12 ? Visibility.Visible : Visibility.Collapsed;
        Render(string.IsNullOrWhiteSpace(_search.Text) ? hierarchy : ClassHierarchyModel.Filter(hierarchy, _search.Text));
    }

    private void Render(ClassHierarchy hierarchy)
    {
        _list.Children.Clear();
        TypeCount = hierarchy.Types.Count;
        RelationCount = hierarchy.Relations.Count;

        if (hierarchy.IsEmpty)
        {
            _header.Text = string.IsNullOrWhiteSpace(_search.Text) || _full.IsEmpty
                ? "Class hierarchy"
                : $"Class hierarchy — no type matches \u201c{_search.Text}\u201d";
            _disclosure.Text = "";
            _list.Children.Add(EmptyState());
            return;
        }

        var filtered = !string.IsNullOrWhiteSpace(_search.Text) && _full.Types.Count != hierarchy.Types.Count;
        _header.Text = filtered
            ? $"Class hierarchy — {hierarchy.Types.Count} of {_full.Types.Count} type(s) match \u201c{_search.Text}\u201d"
            : $"Class hierarchy — {hierarchy.Types.Count} type(s), {hierarchy.Relations.Count} relationship(s)";
        var notes = new List<string> { "Members are not extracted yet, so types show relationships only (ADR-0020 Phase 1)." };
        if (hierarchy.ExternalRelations > 0)
        {
            notes.Add($"{hierarchy.ExternalRelations} relationship(s) to base types/interfaces outside the analysed scope are not drawn.");
        }
        _disclosure.Text = string.Join("  ", notes);

        // Group relations by their source type for a scannable per-type card.
        var bySource = hierarchy.Relations
            .GroupBy(r => r.From, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var labelOf = hierarchy.Types.ToDictionary(t => t.Id, t => t.Label, StringComparer.Ordinal);

        // Group by context (namespace/bounded context) so a large hierarchy is scannable; types with
        // no context fall into a trailing "(no context)" group.
        var groups = hierarchy.Types
            .GroupBy(t => string.IsNullOrWhiteSpace(t.Context) ? null : t.Context, StringComparer.Ordinal)
            .OrderBy(g => g.Key is null ? 1 : 0)
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in groups)
        {
            if (groups.Count > 1)
            {
                _list.Children.Add(GroupHeader(group.Key ?? "(no context)", group.Count()));
            }

            foreach (var type in group.OrderBy(t => t.Label, StringComparer.OrdinalIgnoreCase))
            {
                _list.Children.Add(Card(type, bySource.TryGetValue(type.Id, out var rels) ? rels : [], labelOf));
            }
        }
    }

    private static TextBlock GroupHeader(string context, int count)
    {
        var t = new TextBlock
        {
            Text = context + "  ·  " + count + " type(s)",
            FontSize = 11.5,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(2, 10, 0, 4),
        };
        t.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return t;
    }

    public void Clear()
    {
        _full = new ClassHierarchy([], [], 0);
        _list.Children.Clear();
        TypeCount = 0;
        RelationCount = 0;
        _header.Text = "Class hierarchy";
        _disclosure.Text = "";
        _search.Visibility = Visibility.Collapsed;
        _list.Children.Add(EmptyState());
    }

    /// <summary>Shows a loading state while the graph is fetched (U9 state completeness).</summary>
    public void ShowLoading()
    {
        _list.Children.Clear();
        _search.Visibility = Visibility.Collapsed;
        _header.Text = "Class hierarchy";
        _disclosure.Text = "";
        _list.Children.Add(Centered("Loading the type hierarchy\u2026"));
    }

    /// <summary>Shows an explicit error state — never a misleading empty state — when the graph load fails.</summary>
    public void ShowError(string message)
    {
        _list.Children.Clear();
        _search.Visibility = Visibility.Collapsed;
        TypeCount = 0;
        RelationCount = 0;
        _header.Text = "Class hierarchy";
        _disclosure.Text = "";
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(24),
        };
        panel.Children.Add(Muted("The type hierarchy could not be loaded.", 13, FontWeights.SemiBold, center: true));
        panel.Children.Add(Muted(message, 12, center: true));
        _list.Children.Add(panel);
    }

    private static UIElement Centered(string text)
    {
        var t = new TextBlock
        {
            Text = text,
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(24),
        };
        t.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return t;
    }

    private Border Card(ClassTypeNode type, IReadOnlyList<ClassRelation> relations, IReadOnlyDictionary<string, string> labelOf)
    {
        var panel = new StackPanel { Margin = new Thickness(10, 8, 10, 8) };

        var title = new WrapPanel();
        if (type.IsInterface)
        {
            var stereo = Muted("«interface»", 11);
            stereo.Margin = new Thickness(0, 0, 6, 0);
            title.Children.Add(stereo);
        }
        title.Children.Add(Text(type.Label, 13, FontWeights.SemiBold));
        panel.Children.Add(title);

        if (relations.Count == 0)
        {
            panel.Children.Add(Muted("no drawn relationships in scope", 11.5));
        }
        else
        {
            foreach (var r in relations)
            {
                var target = labelOf.TryGetValue(r.To, out var l) ? l : r.To;
                var verb = r.Kind == ClassRelationKind.Generalization ? "▷ inherits" : "⊳ implements";
                panel.Children.Add(Muted($"{verb}  {target}", 12));
            }
        }

        var card = new Border
        {
            Child = panel,
            Margin = new Thickness(0, 0, 0, 6),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
        };
        card.SetResourceReference(BackgroundProperty, "RaisedBrush");
        card.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        AutomationProperties.SetName(card, (type.IsInterface ? "interface " : "class ") + type.Label);
        return card;
    }

    private static UIElement EmptyState()
    {
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(24),
        };
        panel.Children.Add(Muted("No classes or interfaces in view.", 13, center: true));
        panel.Children.Add(Muted("Open a workspace with source code to see its type hierarchy.", 12, center: true));
        return panel;
    }

    private static TextBlock Text(string text, double size, FontWeight weight)
    {
        var t = new TextBlock { Text = text, FontSize = size, FontWeight = weight, TextWrapping = TextWrapping.Wrap };
        t.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        return t;
    }

    private static TextBlock Muted(string text, double size, FontWeight? weight = null, bool center = false)
    {
        var t = new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = weight ?? FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = center ? TextAlignment.Center : TextAlignment.Left,
        };
        t.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return t;
    }
}
