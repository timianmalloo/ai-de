using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.Core.Projections;

namespace AiDe.App.Workbench;

/// <summary>
/// The context map: contexts as boxes, and the traffic between them.
/// </summary>
/// <remarks>
/// <para><b>The crossing count is the point.</b> A context map that only names contexts is a
/// picture of a decision; the number of edges leaving each one is the evidence for whether that
/// decision held. A context with no crossings is isolated and a context with hundreds is not
/// bounded, and neither is visible from a list of names.</para>
///
/// <para><b>An invalid map renders its problems, not a partial diagram.</b> A context map drawn
/// from a file that failed validation is wrong in a way nobody can see, which is worse than one
/// that refuses and says why.</para>
/// </remarks>
public sealed class ContextMapSurface : ContentControl
{
    private readonly StackPanel _body = new() { Margin = new Thickness(12) };

    public ContextMapSurface(string title)
    {
        AutomationProperties.SetName(this, title);
        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _body,
        };

        Render(null);
    }

    /// <summary>Supplies the view. Null until a workspace with a context map attaches.</summary>
    public Func<ContextMapView>? Source { get; set; }

    /// <summary>Raised when a context box is chosen, so another surface can show only that context.</summary>
    public event EventHandler<string>? ContextSelected;

    public void Refresh() => Render(Source?.Invoke());

    private void Render(ContextMapView? view)
    {
        _body.Children.Clear();

        if (view is null)
        {
            _body.Children.Add(Muted(
                "No context map. Add docs/bounded-contexts.yaml to the workspace and index it."));
            return;
        }

        if (!view.IsValid)
        {
            _body.Children.Add(Heading("The context map is invalid and is not drawn"));
            _body.Children.Add(Muted(
                "A map drawn from a file that failed validation is wrong in a way nobody can see."));

            foreach (var problem in view.Problems.Take(12))
            {
                _body.Children.Add(new TextBlock
                {
                    Text = "• " + problem,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 0),
                    Foreground = Brushes.IndianRed,
                });
            }

            return;
        }

        _body.Children.Add(Heading($"{view.Contexts.Count} declared context(s)"));

        foreach (var context in view.Contexts.OrderByDescending(c => c.Symbols))
        {
            var box = ContextBox(context);
            var name = context.Name;

            box.Cursor = System.Windows.Input.Cursors.Hand;
            box.ToolTip = $"Show only {name} in the graph";
            box.MouseLeftButtonUp += (_, _) => ContextSelected?.Invoke(this, name);

            // Keyboard-reachable, because a filter only a mouse can apply is a filter half the users
            // do not have.
            box.Focusable = true;
            box.KeyDown += (_, e) =>
            {
                if (e.Key is System.Windows.Input.Key.Enter or System.Windows.Input.Key.Space)
                {
                    ContextSelected?.Invoke(this, name);
                    e.Handled = true;
                }
            };

            AutomationProperties.SetName(box, $"{name}. {context.Symbols} symbols, {context.Crossings} crossings.");
            _body.Children.Add(box);
        }

        _body.Children.Add(Heading("Crossings"));

        if (view.Edges.Count == 0)
        {
            _body.Children.Add(Muted(
                "No edges cross a context boundary. Either the contexts are genuinely independent, " +
                "or most of the code is not covered by any of them."));
        }

        foreach (var edge in view.Edges.Take(30))
        {
            _body.Children.Add(new TextBlock
            {
                Text = $"{edge.From}  →  {edge.To}     {edge.Weight} edge(s)",
                Margin = new Thickness(0, 2, 0, 0),
                FontFamily = new FontFamily("Cascadia Mono, Consolas"),
            });
        }

        // Coverage is stated with the contexts, not tucked away: "we have contexts" must not quietly
        // mean "we have contexts for part of the code".
        _body.Children.Add(Muted(
            view.UncoveredSymbols == 0
                ? "Every declared symbol belongs to a context."
                : $"{view.UncoveredSymbols} declared symbol(s) belong to no context. That may be correct — " +
                  "forcing them into one to raise a number is how a context map stops meaning anything."));
    }

    private static Border ContextBox(ContextView context)
    {
        var hue = 0;
        foreach (var c in context.Name) hue = ((hue * 31) + c) % 360;

        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = context.Name, FontWeight = FontWeights.SemiBold });

        if (!string.IsNullOrWhiteSpace(context.Description))
        {
            panel.Children.Add(new TextBlock
            {
                Text = context.Description,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.75,
                Margin = new Thickness(0, 2, 0, 0),
            });
        }

        panel.Children.Add(new TextBlock
        {
            Text = $"{context.Symbols} symbol(s) · {context.InternalEdges} internal edge(s) · " +
                   $"{context.Crossings} crossing(s)",
            Margin = new Thickness(0, 4, 0, 0),
            Opacity = 0.85,
        });

        return new Border
        {
            // The same hue the canvas uses for this context, so the two views are one picture.
            BorderBrush = new SolidColorBrush(FromHsl(hue)),
            BorderThickness = new Thickness(2, 2, 2, 2),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 6, 0, 0),
            Child = panel,
        };
    }

    /// <summary>The canvas colours by <c>hsl(h, 55%, 55%)</c>; this matches it.</summary>
    private static Color FromHsl(int hue)
    {
        double h = hue / 60.0, c = 0.55 * (1 - Math.Abs((2 * 0.55) - 1)) * 2;
        var x = c * (1 - Math.Abs((h % 2) - 1));
        var m = 0.55 - (c / 2);

        var (r, g, b) = (int)h switch
        {
            0 => (c, x, 0.0),
            1 => (x, c, 0.0),
            2 => (0.0, c, x),
            3 => (0.0, x, c),
            4 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };

        return Color.FromRgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
    }

    private static TextBlock Heading(string text) => new()
    {
        Text = text,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 12, 0, 4),
    };

    private static TextBlock Muted(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Opacity = 0.7,
        Margin = new Thickness(0, 8, 0, 0),
    };
}
