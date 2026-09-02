using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AiDe.App.Workbench;

/// <summary>
/// The UML sequence-diagram surface (uml-sequence-diagram): participants as header boxes atop vertical
/// dashed lifelines, and ordered messages as horizontal arrows drawn top-to-bottom — solid/filled for
/// calls, dashed/open for returns, a loop for self-messages. Dependency-free native WPF, no WebView2,
/// mirroring <see cref="ClassDiagramSurface"/>.
/// </summary>
/// <remarks>
/// <b>No longer a scaffold.</b> This said the ordered call data was something "the graph does not yet
/// emit". It does: <c>calls_at</c> assertions carry the callee, the member and the call site, and
/// <c>WorkbenchShell.ShowNodeInSequenceDiagramsAsync</c> feeds them here through
/// <c>InteractionAsync</c>. Measured on the workspace where this surface was reported empty:
/// <b>4,967</b> of them. The remark outlived its subject and kept asserting a gap that had closed,
/// which is how the empty state below came to blame an extractor that had already done the work.
/// </remarks>
public sealed class SequenceDiagramSurface : ContentControl, IHasDisplayName
{
    private const double ColWidth = 160;
    private const double BoxW = 130;
    private const double BoxH = 34;
    private const double TopPad = 12;
    private const double RowH = 46;
    private const double SidePad = 24;

    private readonly ScrollViewer _scroller;
    private readonly TextBlock _title;
    private readonly TextBlock _empty;
    private readonly Grid _root;

    private int _participantCount;
    private int _messageCount;

    public SequenceDiagramSurface()
    {
        _title = new TextBlock
        {
            Margin = new Thickness(12, 8, 12, 8),
            FontSize = 14,
            Text = "Sequence diagram",
        };
        _title.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

        _empty = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            MaxWidth = 420,
            TextWrapping = TextWrapping.Wrap,
            // Set per state by ShowEmpty. The one thing this must never do again is name a cause
            // the surface has not checked — see the remark on ShowEmpty.
            Text = WaitingForSelection,
        };
        _empty.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        _scroller = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(12),
        };

        _root = new Grid();
        _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(_title, 0);
        Grid.SetRow(_scroller, 1);
        _root.Children.Add(_title);
        _root.Children.Add(_scroller);

        Content = _root;
        AutomationProperties.SetName(this, "Sequence diagram");

        Show(SequenceModel.Empty);
    }

    /// <summary>The tab title (survives re-render like other surfaces).</summary>
    public string DisplayName { get; private set; } = "Sequence diagram";

    /// <summary>True while there is nothing to draw (the empty state is shown).</summary>
    public bool IsEmpty { get; private set; } = true;

    /// <summary>Participants drawn by the last render (test hook).</summary>
    public int ParticipantCount => _participantCount;

    /// <summary>Messages drawn by the last render (test hook).</summary>
    public int MessageCount => _messageCount;

    /// <summary>The node whose interactions are shown, or null when nothing has been loaded (Phase E).</summary>
    public string? NodeId { get; private set; }

    /// <summary>The empty state before any node has been chosen — a prompt, not a diagnosis.</summary>
    internal const string WaitingForSelection =
        "Select a node in the graph to see its calls in order.";

    /// <summary>Shows a specific node's interactions and records which node, so a re-render does not re-fetch.</summary>
    public void ShowFor(string nodeId, SequenceModel model)
    {
        NodeId = nodeId;
        Show(model, nodeId);
    }

    /// <summary>
    /// The empty state, saying WHICH emptiness this is.
    /// </summary>
    /// <remarks>
    /// <para><b>What this replaces.</b> One message covered every empty case: <i>"Sequence diagrams
    /// need ordered call data from the extractor - this surface renders it as soon as that
    /// lands."</i> It was shown before any node had been selected, which is the ordinary state of a
    /// freshly opened tab, and it named a cause the surface had never checked.</para>
    ///
    /// <para><b>It was false when it mattered.</b> The workspace it was reported against held
    /// <b>4,967</b> <c>calls_at</c> assertions. The extractor had done its job; nothing had been
    /// selected. The owner read the message, concluded the call data had not landed, and reported it
    /// as an extractor gap - a correct inference from a confident and wrong statement.</para>
    ///
    /// <para><b>The rule this encodes:</b> an empty state may say what it is waiting for, and may
    /// name a cause only when it has observed one. "Nothing to show" plus a guess at why is worse
    /// than "nothing to show", because the guess is the part that gets acted on (DC-087).</para>
    /// </remarks>
    private void ShowEmpty(string? nodeId)
    {
        IsEmpty = true;

        _empty.Text = nodeId is null
            ? WaitingForSelection
            : $"{Shorten(nodeId)} has no recorded calls." + System.Environment.NewLine
              + "Only calls the extractor could resolve appear here, so a node that calls only into "
              + "external packages shows none.";

        _scroller.Content = _empty;
        _title.Text = "Sequence diagram";
    }

    private static string Shorten(string nodeId)
    {
        var cut = nodeId.LastIndexOf('.');
        return cut > 0 && cut < nodeId.Length - 1 ? nodeId[(cut + 1)..] : nodeId;
    }

    /// <summary>Renders the interaction, or the empty state when it has no participants.</summary>
    public void Show(SequenceModel model, string? nodeId = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        _participantCount = 0;
        _messageCount = 0;

        if (model.IsEmpty)
        {
            ShowEmpty(nodeId);
            return;
        }

        IsEmpty = false;
        _title.Text = $"Sequence diagram — {model.Participants.Count} participant(s), {model.Messages.Count} message(s)";

        var canvas = new Canvas();
        var xById = new Dictionary<string, double>(StringComparer.Ordinal);

        // Participant header boxes + their lifelines.
        var lifelineTop = TopPad + BoxH;
        var lifelineBottom = lifelineTop + ((model.Messages.Count + 1) * RowH);
        for (var i = 0; i < model.Participants.Count; i++)
        {
            var p = model.Participants[i];
            var cx = SidePad + (i * ColWidth) + (BoxW / 2);
            xById[p.Id] = cx;

            var box = new Border
            {
                Width = BoxW,
                Height = BoxH,
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = p.Label,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(6, 0, 6, 0),
                },
            };
            box.SetResourceReference(Border.BackgroundProperty, "SurfaceRaisedBrush");
            box.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
            ((TextBlock)box.Child).SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            Canvas.SetLeft(box, SidePad + (i * ColWidth));
            Canvas.SetTop(box, TopPad);
            canvas.Children.Add(box);

            var lifeline = new Line
            {
                X1 = cx, Y1 = lifelineTop, X2 = cx, Y2 = lifelineBottom,
                StrokeThickness = 1, StrokeDashArray = [3, 3],
            };
            lifeline.SetResourceReference(Shape.StrokeProperty, "BorderBrush");
            canvas.Children.Add(lifeline);

            _participantCount++;
        }

        // Messages, top-to-bottom in order.
        foreach (var m in model.Messages.OrderBy(x => x.Order))
        {
            if (!xById.TryGetValue(m.FromId, out var fx) || !xById.TryGetValue(m.ToId, out var tx))
            {
                continue;
            }

            var y = lifelineTop + ((m.Order + 1) * RowH);
            DrawMessage(canvas, m, fx, tx, y);
            _messageCount++;
        }

        canvas.Width = SidePad + (model.Participants.Count * ColWidth) + SidePad;
        canvas.Height = lifelineBottom + RowH;
        _scroller.Content = canvas;
    }

    private static void DrawMessage(Canvas canvas, SequenceMessage m, double fromX, double toX, double y)
    {
        var dashed = m.Kind == SequenceMessageKind.Return;

        if (m.Kind == SequenceMessageKind.Self)
        {
            // A self-message: out and back on the same lifeline, a small loop to the right.
            const double w = 34, h = 18;
            AddLine(canvas, fromX, y, fromX + w, y, dashed);
            AddLine(canvas, fromX + w, y, fromX + w, y + h, dashed);
            AddLine(canvas, fromX + w, y + h, fromX, y + h, dashed);
            AddArrowHead(canvas, fromX, y + h, angle: Math.PI, filled: true);
            AddLabel(canvas, m.Label, fromX + w + 6, y - 2, left: true);
            return;
        }

        AddLine(canvas, fromX, y, toX, y, dashed);
        var toLeft = toX < fromX;
        AddArrowHead(canvas, toX, y, angle: toLeft ? Math.PI : 0, filled: m.Kind == SequenceMessageKind.Call);
        AddLabel(canvas, m.Label, (fromX + toX) / 2, y - 16, left: false);
    }

    private static void AddLine(Canvas canvas, double x1, double y1, double x2, double y2, bool dashed)
    {
        var line = new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, StrokeThickness = 1.2 };
        if (dashed) { line.StrokeDashArray = [4, 3]; }
        line.SetResourceReference(Shape.StrokeProperty, "TextBrush");
        canvas.Children.Add(line);
    }

    private static void AddArrowHead(Canvas canvas, double x, double y, double angle, bool filled)
    {
        const double size = 9;
        if (filled)
        {
            var head = new Polygon
            {
                Points = new PointCollection
                {
                    new(x, y),
                    new(x - (size * Math.Cos(angle - 0.4)), y - (size * Math.Sin(angle - 0.4))),
                    new(x - (size * Math.Cos(angle + 0.4)), y - (size * Math.Sin(angle + 0.4))),
                },
            };
            head.SetResourceReference(Shape.FillProperty, "TextBrush");
            canvas.Children.Add(head);
            return;
        }

        foreach (var spread in new[] { angle - 0.4, angle + 0.4 })
        {
            var barb = new Line
            {
                X1 = x, Y1 = y,
                X2 = x - (size * Math.Cos(spread)),
                Y2 = y - (size * Math.Sin(spread)),
                StrokeThickness = 1.2,
            };
            barb.SetResourceReference(Shape.StrokeProperty, "TextBrush");
            canvas.Children.Add(barb);
        }
    }

    private static void AddLabel(Canvas canvas, string text, double x, double y, bool left)
    {
        var label = new TextBlock
        {
            Text = text,
            FontSize = 11.5,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = ColWidth,
        };
        label.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(label, left ? x : x - (label.DesiredSize.Width / 2));
        Canvas.SetTop(label, y);
        canvas.Children.Add(label);
    }
}
