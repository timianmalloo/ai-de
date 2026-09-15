using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AiDe.Core.Understanding;

namespace AiDe.App.Workbench.Understanding;

/// <summary>Native, file-local occurrence compartments and their synchronized ordered list.</summary>
public sealed class AtlasStaticView : UserControl
{
    private readonly TextBlock _status = Label("Select a file to inspect its Class view.");
    private readonly TextBlock _bounds = Label("");
    private readonly ComboBox _classifiers = new();
    private readonly StackPanel _compartments = new();
    private readonly ListBox _list = new();
    private readonly TabControl _tabs = new();
    private readonly List<Button> _buttons = [];
    private bool _rendering;
    private string? _activationOrigin;

    public AtlasStaticView()
    {
        SetResourceReference(BackgroundProperty, "SurfaceBrush");
        AutomationProperties.SetName(this, "File-local Class view");
        AutomationProperties.SetName(_classifiers, "Classifier occurrences on this page");
        AutomationProperties.SetName(_list, "Equivalent declaration list");
        AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite);
        foreach (var control in new Control[] { _classifiers, _list, _tabs })
        {
            control.SetResourceReference(BackgroundProperty, "SurfaceSunkenBrush");
            control.SetResourceReference(ForegroundProperty, "TextBrush");
        }
        _classifiers.ItemTemplate = WrappedTemplate();
        _list.ItemTemplate = WrappedTemplate();
        var itemStyle = new Style(typeof(ListBoxItem));
        itemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        itemStyle.Setters.Add(new Setter(AutomationProperties.NameProperty, new Binding(nameof(AtlasStaticOccurrence.AccessibleName))));
        _list.ItemContainerStyle = itemStyle;
        ScrollViewer.SetHorizontalScrollBarVisibility(_list, ScrollBarVisibility.Disabled);
        VirtualizingPanel.SetIsVirtualizing(_list, true);
        VirtualizingPanel.SetVirtualizationMode(_list, VirtualizationMode.Recycling);
        _classifiers.SelectionChanged += (_, _) =>
        {
            if (!_rendering && _classifiers.SelectedItem is AtlasStaticOccurrence occurrence)
                ClassifierChanged?.Invoke(occurrence.Token);
        };
        _list.SelectionChanged += (_, _) =>
        {
            if (!_rendering && _list.SelectedItem is AtlasStaticOccurrence occurrence)
                SelectedToken = occurrence.Token;
        };
        _list.MouseDoubleClick += (_, _) => ActivateList();
        _list.KeyDown += (_, args) =>
        {
            if (args.Key != Key.Enter) return;
            args.Handled = true;
            ActivateList();
        };
        _tabs.Items.Add(CreatePresentationTab("Compartments",
            new ScrollViewer { Content = _compartments, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }));
        _tabs.Items.Add(CreatePresentationTab("Declaration list", _list));
        var header = new StackPanel { Margin = new Thickness(8) };
        header.Children.Add(Label(AtlasStaticViewProjection.ProfileDisclosure));
        header.Children.Add(_status);
        header.Children.Add(_classifiers);
        var footer = new StackPanel { Margin = new Thickness(8) };
        footer.Children.Add(_bounds);
        footer.Children.Add(Label(AtlasStaticViewProjection.RelationshipDisclosure));
        var root = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(header);
        root.Children.Add(footer);
        root.Children.Add(_tabs);
        Content = root;
    }

    public event Action<string>? ClassifierChanged;
    public event Action<AtlasStaticOccurrence, string>? DeclarationActivated;
    public AtlasStaticViewProjection? Projection { get; private set; }
    public ComboBox ClassifiersControl => _classifiers;
    public ListBox DeclarationList => _list;
    public TabControl Presentations => _tabs;
    public IReadOnlyList<Button> CompartmentButtons => _buttons;
    public string? SelectedToken { get; private set; }
    public string StatusText => _status.Text;

    private static TabItem CreatePresentationTab(string header, object content) =>
        new PresentationTabItem { Header = header, Content = content };

    private sealed class PresentationTabItem : TabItem
    {
        public PresentationTabItem() => SetResourceReference(StyleProperty, typeof(TabItem));

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            // assume: this Windows template paints selection through innerBorder; the pixel gate detects drift.
            // Override only its measured white fill, preserving native focus, keyboard and disabled triggers.
            if (Template?.FindName("innerBorder", this) is Border selectedFill)
                selectedFill.SetResourceReference(Border.BackgroundProperty, "SurfaceSunkenBrush");
        }
    }

    public void ShowState(string text)
    {
        _rendering = true;
        try
        {
            Projection = null;
            SelectedToken = null;
            _status.Text = text;
            _bounds.Text = "";
            _classifiers.ItemsSource = null;
            _classifiers.IsEnabled = false;
            _list.ItemsSource = null;
            _list.IsEnabled = false;
            _compartments.Children.Clear();
            _buttons.Clear();
        }
        finally { _rendering = false; }
    }

    public void Render(AtlasStaticViewProjection projection)
    {
        var started = Stopwatch.GetTimestamp();
        _rendering = true;
        try
        {
            Projection = projection;
            _activationOrigin = null;
            SelectedToken = null;
            _status.Text = projection.Status;
            _bounds.Text = projection.Bounds;
            _classifiers.ItemsSource = projection.Classifiers;
            _classifiers.SelectedItem = projection.Classifier;
            _classifiers.IsEnabled = projection.Classifiers.Count > 0 && projection.CanNavigate;
            _list.ItemsSource = projection.Occurrences;
            _list.IsEnabled = projection.CanNavigate;
            _compartments.Children.Clear();
            _buttons.Clear();
            if (projection.Classifier is { } classifier)
            {
                var body = new StackPanel { Margin = new Thickness(8) };
                var heading = Label($"{classifier.Declaration.Structure!.ClassifierFlavor} occurrence\n{classifier.Declaration.DisplayName}");
                heading.FontWeight = FontWeights.SemiBold;
                body.Children.Add(heading);
                body.Children.Add(Label($"Extracted; UTF-16 {classifier.Declaration.Span.Start} to "
                    + $"{classifier.Declaration.Span.Start + classifier.Declaration.Span.Length}."));
                if (classifier.IsClass)
                {
                    AddCompartment(body, "Properties", projection.Members.Where(row => row.Declaration.Kind == AtlasDeclarationKind.Property));
                    AddCompartment(body, "Operations", projection.Members.Where(row =>
                        row.Declaration.Kind is AtlasDeclarationKind.Method or AtlasDeclarationKind.Constructor));
                    AddCompartment(body, "Declares nested type", projection.Members.Where(row => row.IsClassifier));
                }
                else AddCompartment(body, "Direct declarations — not UML compartments", projection.Members);
                if (projection.Members.Count == 0) body.Children.Add(Label("No direct members on this page."));
                body.Children.Add(Label(projection.PageContext));
                var border = new Border { Child = body, BorderThickness = new Thickness(1), Margin = new Thickness(8) };
                border.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
                border.SetResourceReference(BackgroundProperty, "SurfaceSunkenBrush");
                AutomationProperties.SetName(border, classifier.IsClass ? "UML class occurrence" : "Classifier occurrence, not a UML class");
                _compartments.Children.Add(border);
            }
            else _compartments.Children.Add(Label(projection.Status + " " + projection.PageContext));
        }
        finally
        {
            _rendering = false;
            Trace.TraceInformation("operation=atlas.static.render rows={0} duration_ms={1}",
                projection.Occurrences.Count, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }

    public string FocusOrigin => _activationOrigin ?? (_list.IsKeyboardFocusWithin ? "static-list"
        : _buttons.Any(button => button.IsKeyboardFocusWithin) ? "static-compartment" : "static-classifier");

    public bool RestoreFocus(string? token, string origin)
    {
        SelectedToken = token;
        if (origin == "static-classifier") return _classifiers.Focus();
        var row = Projection?.Occurrences.FirstOrDefault(occurrence => occurrence.Token == token);
        if (row is null) return false;
        _list.SelectedItem = row;
        if (origin == "static-compartment" && _buttons.FirstOrDefault(button =>
                button.Tag is AtlasStaticOccurrence occurrence && occurrence.Token == token) is { } button)
        {
            _tabs.SelectedIndex = 0;
            UpdateLayout();
            button.BringIntoView();
            return button.Focus();
        }
        _tabs.SelectedIndex = 1;
        _list.ScrollIntoView(row);
        UpdateLayout();
        return (_list.ItemContainerGenerator.ContainerFromItem(row) as ListBoxItem)?.Focus() == true;
    }

    private void AddCompartment(Panel parent, string title, IEnumerable<AtlasStaticOccurrence> rows)
    {
        var members = rows.ToArray();
        if (members.Length == 0) return;
        parent.Children.Add(new Separator());
        parent.Children.Add(Label(title));
        foreach (var occurrence in members)
        {
            var button = new Button
            {
                Content = Label(occurrence.AccessibleName), Tag = occurrence, HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 2, 0, 2), IsEnabled = Projection!.CanNavigate,
            };
            button.SetResourceReference(ForegroundProperty, "TextBrush");
            button.SetResourceReference(BackgroundProperty, "SurfaceBrush");
            AutomationProperties.SetName(button, occurrence.AccessibleName);
            button.Click += (_, _) => Activate(occurrence, "static-compartment");
            _buttons.Add(button);
            parent.Children.Add(button);
        }
    }

    private void ActivateList()
    {
        if (_list.SelectedItem is AtlasStaticOccurrence row) Activate(row, "static-list");
    }

    private void Activate(AtlasStaticOccurrence occurrence, string origin)
    {
        if (Projection?.CanNavigate != true) return;
        SelectedToken = occurrence.Token;
        _activationOrigin = origin;
        _list.SelectedItem = occurrence;
        DeclarationActivated?.Invoke(occurrence, origin);
    }

    private static TextBlock Label(string text)
    {
        var label = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.None };
        label.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        return label;
    }

    private static DataTemplate WrappedTemplate()
    {
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new Binding());
        text.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        text.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.None);
        return new DataTemplate { VisualTree = text };
    }
}
