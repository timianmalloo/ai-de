using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
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
    private readonly ScrollViewer _scroller;
    private readonly TextBlock _header;
    private readonly TextBlock _disclosure;
    private readonly TextBox _search;
    private readonly ToggleButton _diagramToggle;
    private readonly ToggleButton _hideInterfaces;
    private ClassHierarchy _full = new([], [], 0);

    // Above this many drawn types a node-and-arrow diagram is an unreadable tangle, so the list stays.
    // The search narrows a large hierarchy into this range, where the diagram earns its place.
    private const int DiagramMax = 40;

    /// <summary>
    /// Fetches a type's declared members (attributes + operations) for its UML compartment — the shell
    /// wires this to the workspace's <c>DescribeAsync</c>. Null leaves the compartment as a pending marker.
    /// </summary>
    public Func<string, Task<(IReadOnlyList<string> Members, int Declared)>>? MembersSource { get; set; }

    // Bumped on every diagram render; an async member fill checks it before touching the tree so a fill
    // from a superseded render (a search keystroke, a toggle) cannot write into the current one.
    private int _renderGen;

    // Test hook: how many member-compartment fills were dispatched by the last render. Incremented
    // synchronously (before the first await) so a test can observe it right after ShowGraph returns.
    private int _membersRequested;
    public int MembersRequestedCount => _membersRequested;

    // Member data per type id (attributes, operations, real declared count), fetched once per graph and
    // reused across re-renders (toggles). Variable-height boxes need members BEFORE layout, so we render
    // from this cache and re-render when a prefetch fills it. Cleared when a new graph is shown.
    private readonly Dictionary<string, (IReadOnlyList<string> Attributes, IReadOnlyList<string> Operations, int Declared)> _memberCache
        = new(StringComparer.Ordinal);

    public ClassDiagramSurface(string title = "Class diagram")
    {
        AutomationProperties.SetName(this, title);
        SetResourceReference(BackgroundProperty, "SurfaceBrush");

        var root = new DockPanel { LastChildFill = true, Margin = new Thickness(14, 12, 14, 12) };

        _header = Text("Class hierarchy", 14, FontWeights.SemiBold);
        _diagramToggle = new ToggleButton
        {
            Content = "Diagram",
            IsChecked = true,   // the surface is a "class diagram" — show one by default
            Padding = new Thickness(9, 2, 9, 2),
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "Show the types as a connected diagram (boxes + inheritance arrows), or as a list",
        };
        AutomationProperties.SetName(_diagramToggle, "Show as a diagram");
        _diagramToggle.Checked += (_, _) => RenderCurrent();
        _diagramToggle.Unchecked += (_, _) => RenderCurrent();

        _hideInterfaces = new ToggleButton
        {
            Content = "Hide interfaces",
            Padding = new Thickness(9, 2, 9, 2),
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "Hide interface types and the 'implements' arrows to them — a class with many interfaces reads too broad",
        };
        AutomationProperties.SetName(_hideInterfaces, "Hide interfaces");
        _hideInterfaces.Checked += (_, _) => RenderCurrent();
        _hideInterfaces.Unchecked += (_, _) => RenderCurrent();

        var headerRow = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(_diagramToggle, Dock.Right);
        DockPanel.SetDock(_hideInterfaces, Dock.Right);
        headerRow.Children.Add(_diagramToggle);
        headerRow.Children.Add(_hideInterfaces);
        headerRow.Children.Add(_header);
        DockPanel.SetDock(headerRow, Dock.Top);
        root.Children.Add(headerRow);

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
        _scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _list,
        };
        root.Children.Add(_scroller);

        Content = root;
        Clear();
    }

    /// <summary>The number of type cards currently shown (for tests).</summary>
    public int TypeCount { get; private set; }

    /// <summary>The number of generalization/realization relations currently shown (for tests).</summary>
    public int RelationCount { get; private set; }

    public bool IsEmpty => TypeCount == 0;

    /// <summary>For tests: whether the current render is the visual diagram (a Canvas) rather than the list.</summary>
    internal bool ShowingDiagram => _scroller.Content is Canvas;

    /// <summary>For tests: the number of type boxes drawn in the visual diagram.</summary>
    internal int DrawnBoxCount => _scroller.Content is Canvas c ? c.Children.OfType<Border>().Count() : 0;

    /// <summary>Test hook: the measured heights of the drawn boxes — variable, sized to each type's members.</summary>
    internal IReadOnlyList<double> DrawnBoxHeights =>
        _scroller.Content is Canvas c
            ? c.Children.OfType<Border>()
                .Select(b =>
                {
                    b.Measure(new Size(b.Width, double.PositiveInfinity));
                    return b.DesiredSize.Height;
                })
                .ToList()
            : [];

    /// <summary>For tests: force diagram or list mode (as the header toggle does).</summary>
    internal void SetDiagramMode(bool on) => _diagramToggle.IsChecked = on;

    /// <summary>For tests: hide/show interface types (as the header toggle does).</summary>
    internal void SetHideInterfaces(bool on) => _hideInterfaces.IsChecked = on;

    /// <summary>Builds the hierarchy from a graph and renders it (ADR-0020).</summary>
    public void ShowGraph(IReadOnlyList<CanvasNode>? nodes, IReadOnlyList<CanvasEdge>? edges) =>
        Show(ClassHierarchyModel.Build(nodes, edges));

    /// <summary>Stores and renders a prebuilt hierarchy (search re-renders a filtered view of it).</summary>
    public void Show(ClassHierarchy hierarchy)
    {
        _full = hierarchy;
        _memberCache.Clear();   // a new graph: member data from the previous type set is stale
        _membersRequested = 0;  // reset the per-graph member-request tally (test hook)
        _search.Visibility = hierarchy.Types.Count > 12 ? Visibility.Visible : Visibility.Collapsed;
        Render(string.IsNullOrWhiteSpace(_search.Text) ? hierarchy : ClassHierarchyModel.Filter(hierarchy, _search.Text));
    }

    private void RenderCurrent() =>
        Render(string.IsNullOrWhiteSpace(_search.Text) ? _full : ClassHierarchyModel.Filter(_full, _search.Text));

    private void Render(ClassHierarchy hierarchy)
    {
        if (_hideInterfaces.IsChecked == true)
        {
            hierarchy = WithoutInterfaces(hierarchy);
        }

        TypeCount = hierarchy.Types.Count;
        RelationCount = hierarchy.Relations.Count;

        if (hierarchy.IsEmpty)
        {
            _scroller.Content = _list;
            _list.Children.Clear();
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

        var notes = new List<string>
        {
            "Members are not extracted yet, so types show relationships only (ADR-0020 Phase 1).",
        };
        if (hierarchy.ExternalRelations > 0)
        {
            notes.Add($"{hierarchy.ExternalRelations} relationship(s) to base types/interfaces outside the analysed scope are not drawn.");
        }

        if (_diagramToggle.IsChecked == true)
        {
            RenderDiagram(hierarchy, notes);
        }
        else
        {
            RenderList(hierarchy, notes);
        }
    }

    private void RenderList(ClassHierarchy hierarchy, List<string> notes)
    {
        _scroller.Content = _list;
        _list.Children.Clear();
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

    // The visual diagram (ADR-0020): types as boxes, generalization/realization as UML connectors with a
    // hollow triangle at the base end, laid out in inheritance ranks (bases on top, derived below, arrows
    // pointing up). Capped to the most-connected DiagramMax types — a diagram of hundreds is a tangle, and
    // search narrows a large hierarchy into a readable one.
    private void RenderDiagram(ClassHierarchy hierarchy, List<string> notes)
    {
        var gen = ++_renderGen;   // supersedes any in-flight member fills from an earlier render
        var degree = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var r in hierarchy.Relations)
        {
            degree[r.From] = degree.GetValueOrDefault(r.From) + 1;
            degree[r.To] = degree.GetValueOrDefault(r.To) + 1;
        }

        var drawn = hierarchy.Types
            .OrderByDescending(t => degree.GetValueOrDefault(t.Id))
            .ThenBy(t => t.Label, StringComparer.OrdinalIgnoreCase)
            .Take(DiagramMax)
            .ToList();
        var drawnIds = drawn.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);

        if (hierarchy.Types.Count > drawn.Count)
        {
            notes.Insert(
                0, $"Showing the {drawn.Count} most-connected of {hierarchy.Types.Count} types — search to focus, or switch to List.");
        }
        _disclosure.Text = string.Join("  ", notes);

        var edges = hierarchy.Relations
            .Where(r => drawnIds.Contains(r.From) && drawnIds.Contains(r.To))
            .ToList();

        // Rank by inheritance depth: a base (no drawn outgoing edge) is rank 0 at the top; a type deriving
        // from a rank-r type is r+1, drawn below it, so its generalization arrow points UP to the base.
        var outByFrom = edges
            .GroupBy(e => e.From, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(x => x.To).ToList(), StringComparer.Ordinal);
        var rank = new Dictionary<string, int>(StringComparer.Ordinal);

        int Rank(string id, HashSet<string> visiting)
        {
            if (rank.TryGetValue(id, out var cached)) { return cached; }
            if (!visiting.Add(id)) { return 0; }   // cycle guard — inheritance should not cycle, but never loop
            var r = 0;
            if (outByFrom.TryGetValue(id, out var tos))
            {
                foreach (var to in tos)
                {
                    if (drawnIds.Contains(to)) { r = Math.Max(r, 1 + Rank(to, visiting)); }
                }
            }
            visiting.Remove(id);
            rank[id] = r;
            return r;
        }

        foreach (var t in drawn) { Rank(t.Id, []); }

        // Boxes are a fixed width but a VARIABLE height — each sized to its own members, which is what
        // makes this a real UML class diagram rather than uniform nodes. Members must therefore be known
        // before layout: build each box from the cache, measure it, then place rows by measured height.
        const double boxW = 224, gapX = 30, gapY = 76, pad = 14;

        var boxes = new Dictionary<string, Border>(StringComparer.Ordinal);
        var heights = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var t in drawn)
        {
            _memberCache.TryGetValue(t.Id, out var m);
            var loading = MembersSource is not null && !_memberCache.ContainsKey(t.Id);
            var box = DiagramBox(t, boxW, m.Attributes, m.Operations, m.Declared, loading);
            box.Measure(new Size(boxW, double.PositiveInfinity));
            boxes[t.Id] = box;
            heights[t.Id] = Math.Max(56, box.DesiredSize.Height);
        }

        var rows = drawn.GroupBy(t => rank[t.Id]).OrderBy(g => g.Key).ToList();
        var rects = new Dictionary<string, Rect>(StringComparer.Ordinal);
        var maxCols = 1;
        var y = (double)pad;
        foreach (var row in rows)
        {
            var items = row.OrderBy(t => t.Label, StringComparer.OrdinalIgnoreCase).ToList();
            maxCols = Math.Max(maxCols, items.Count);
            var rowH = items.Max(t => heights[t.Id]);
            for (var i = 0; i < items.Count; i++)
            {
                var id = items[i].Id;
                rects[id] = new Rect(pad + i * (boxW + gapX), y, boxW, heights[id]);
            }
            y += rowH + gapY;
        }

        var canvas = new Canvas
        {
            Width = pad * 2 + maxCols * (boxW + gapX) - gapX,
            Height = Math.Max(pad * 2, y - gapY + pad),
            Background = Brushes.Transparent,
        };

        // Connectors first, so the boxes paint over their endpoints.
        foreach (var e in edges)
        {
            if (rects.TryGetValue(e.From, out var from) && rects.TryGetValue(e.To, out var to))
            {
                AddConnector(canvas, from, to, e.Kind);
            }
        }

        foreach (var t in drawn)
        {
            var box = boxes[t.Id];
            var r = rects[t.Id];
            Canvas.SetLeft(box, r.X);
            Canvas.SetTop(box, r.Y);
            canvas.Children.Add(box);
        }

        _scroller.Content = canvas;

        // Fill the member cache for any type we don't have yet, then re-render at the true heights. One
        // DescribeAsync per uncached drawn type; the render generation drops a stale prefetch (toggle/search).
        var uncached = MembersSource is null
            ? new List<string>()
            : drawn.Select(t => t.Id).Where(id => !_memberCache.ContainsKey(id)).ToList();
        _membersRequested += uncached.Count;   // cumulative per Show; observed synchronously by tests
        if (uncached.Count > 0)
        {
            _ = PrefetchMembersAsync(gen, uncached, hierarchy, notes);
        }
    }

    // Fetches members for the given types into the cache, then re-renders so the boxes take their true
    // height. Guarded by the render generation so a superseded render (toggle/search) drops silently.
    private async Task PrefetchMembersAsync(int gen, List<string> ids, ClassHierarchy hierarchy, List<string> notes)
    {
        if (MembersSource is null) { return; }

        var fetched = new List<(string Id, IReadOnlyList<string> Attributes, IReadOnlyList<string> Operations, int Declared)>();
        foreach (var id in ids)
        {
            try
            {
                var (members, declared) = await MembersSource(id);
                var (attrs, ops) = SplitMembers(members);
                fetched.Add((id, attrs, ops, declared));
            }
            catch
            {
                fetched.Add((id, [], [], 0));   // a failed fetch caches "no members"; the diagram stays valid
            }
        }

        if (gen != _renderGen) { return; }   // a newer render superseded this prefetch

        foreach (var f in fetched)
        {
            _memberCache[f.Id] = (f.Attributes, f.Operations, f.Declared);
        }

        RenderDiagram(hierarchy, notes);   // cache is now warm → boxes get their real heights, no re-prefetch
    }

    // The UML attribute / operation split: a field/property has no parameter list; an operation has "(".
    private static (IReadOnlyList<string> Attributes, IReadOnlyList<string> Operations) SplitMembers(
        IReadOnlyList<string> members)
    {
        var attributes = members.Where(m => !m.Contains('(', StringComparison.Ordinal)).ToList();
        var operations = members.Where(m => m.Contains('(', StringComparison.Ordinal)).ToList();
        return (attributes, operations);
    }

    // Hides interface types and any relationship touching one — a class implementing many interfaces
    // makes the diagram read too broad. Relationships from a kept type to a now-hidden interface are
    // recounted as external (disclosed, not drawn), so the count stays honest.
    private static ClassHierarchy WithoutInterfaces(ClassHierarchy h)
    {
        var types = h.Types.Where(t => !t.IsInterface).ToList();
        var ids = types.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        var relations = new List<ClassRelation>();
        var external = h.ExternalRelations;
        foreach (var r in h.Relations)
        {
            if (ids.Contains(r.From) && ids.Contains(r.To)) { relations.Add(r); }
            else if (ids.Contains(r.From)) { external++; }
        }

        return new ClassHierarchy(types, relations, external);
    }

    private static void AddConnector(Canvas canvas, Rect from, Rect to, ClassRelationKind kind)
    {
        var x1 = from.X + from.Width / 2;   // top-centre of the derived type
        var y1 = from.Y;
        var x2 = to.X + to.Width / 2;       // bottom-centre of the base type
        var y2 = to.Y + to.Height;

        var line = new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, StrokeThickness = 1.3 };
        line.SetResourceReference(Shape.StrokeProperty, "BorderBrush");
        if (kind == ClassRelationKind.Realization)
        {
            line.StrokeDashArray = [3, 3];   // realization is a dashed line in UML
        }

        canvas.Children.Add(line);

        // Hollow triangle at the base end — the UML generalization/realization arrowhead.
        var angle = Math.Atan2(y2 - y1, x2 - x1);
        const double size = 10;
        var head = new Polygon
        {
            Points =
            [
                new Point(x2, y2),
                new Point(x2 - size * Math.Cos(angle - 0.5), y2 - size * Math.Sin(angle - 0.5)),
                new Point(x2 - size * Math.Cos(angle + 0.5), y2 - size * Math.Sin(angle + 0.5)),
            ],
            StrokeThickness = 1.3,
        };
        head.SetResourceReference(Shape.StrokeProperty, "BorderBrush");
        head.SetResourceReference(Shape.FillProperty, "SurfaceBrush");   // hollow: filled with the background
        canvas.Children.Add(head);
    }

    private const int MaxPerCompartment = 15;

    // A UML class box: three stacked compartments — the name (with an «interface» stereotype and an
    // italic name for interfaces), the attributes, then the operations — each separated by a rule and
    // each SIZED TO ITS CONTENT, so the box height reflects the type. This is the UML classifier shape.
    private static Border DiagramBox(
        ClassTypeNode type,
        double w,
        IReadOnlyList<string>? attributes,
        IReadOnlyList<string>? operations,
        int declared,
        bool loading)
    {
        var stack = new StackPanel();

        // Name compartment.
        var nameArea = new StackPanel { Margin = new Thickness(6, 5, 6, 5) };
        if (type.IsInterface)
        {
            nameArea.Children.Add(Muted("«interface»", 10, center: true));
        }

        var name = Text(type.Label, 12.5, FontWeights.SemiBold);
        name.TextAlignment = TextAlignment.Center;
        name.TextWrapping = TextWrapping.NoWrap;
        name.TextTrimming = TextTrimming.CharacterEllipsis;
        name.ToolTip = type.Id;
        if (type.IsInterface) { name.FontStyle = FontStyles.Italic; }   // UML: interface/abstract names are italic
        nameArea.Children.Add(name);
        stack.Children.Add(nameArea);

        stack.Children.Add(CompartmentRule());

        var attrs = attributes ?? [];
        var ops = operations ?? [];
        var listed = attrs.Count + ops.Count;

        // Attributes compartment, then operations compartment — always present (UML keeps the three
        // compartments even when a member list is empty), separated by a rule.
        stack.Children.Add(Compartment(attrs, loading, footer: null));
        stack.Children.Add(CompartmentRule());
        var undeclared = declared > listed ? declared - listed : 0;
        var footer = undeclared > 0 && !loading ? $"(+{undeclared} more not listed)" : null;
        stack.Children.Add(Compartment(ops, loading, footer));

        var box = new Border
        {
            Width = w,
            Child = stack,
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1),
        };
        box.SetResourceReference(BackgroundProperty, "RaisedBrush");
        box.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        AutomationProperties.SetName(box, (type.IsInterface ? "interface " : "class ") + type.Label);
        return box;
    }

    private static Border CompartmentRule()
    {
        var rule = new Border { BorderThickness = new Thickness(0, 1, 0, 0), Height = 1 };
        rule.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        return rule;
    }

    // One UML member compartment: the member lines (capped, with a "…+N more"), a "…" while loading, or a
    // minimal empty band when the type declares none of that member kind.
    private static Border Compartment(IReadOnlyList<string> items, bool loading, string? footer)
    {
        var panel = new StackPanel { Margin = new Thickness(6, 3, 6, 3) };

        if (loading)
        {
            panel.Children.Add(Muted("…", 10.5));
        }
        else if (items.Count == 0 && footer is null)
        {
            panel.Children.Add(new Border { Height = 4, Background = Brushes.Transparent });   // empty compartment band
        }
        else
        {
            for (var i = 0; i < items.Count && i < MaxPerCompartment; i++)
            {
                panel.Children.Add(MemberLine(items[i]));
            }
            if (items.Count > MaxPerCompartment)
            {
                panel.Children.Add(Muted($"…+{items.Count - MaxPerCompartment} more", 10));
            }
            if (footer is not null)
            {
                panel.Children.Add(Muted(footer, 10));
            }
        }

        return new Border { Child = panel };
    }

    private static TextBlock MemberLine(string text)
    {
        var line = new TextBlock
        {
            Text = text,
            FontSize = 10.5,
            FontFamily = new FontFamily("Consolas, Cascadia Mono, Menlo, monospace"),
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            ToolTip = text,
        };
        line.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        return line;
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
        _scroller.Content = _list;
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
        _scroller.Content = _list;
        _list.Children.Clear();
        _search.Visibility = Visibility.Collapsed;
        _header.Text = "Class hierarchy";
        _disclosure.Text = "";
        _list.Children.Add(Centered("Loading the type hierarchy\u2026"));
    }

    /// <summary>Shows an explicit error state — never a misleading empty state — when the graph load fails.</summary>
    public void ShowError(string message)
    {
        _scroller.Content = _list;
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
