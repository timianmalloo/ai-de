using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// The lane's prose as the markdown subset renders it (Ruling 82; <c>DESIGN.md</c> the reply row as
/// amended): headings at one size with the weight carrying the level, paragraphs, lists, fenced
/// code in mono on the sunken ground, tables by hairline — never a box — and a link as its text
/// followed by its URL in muted mono: <b>text, never a control</b> (no <see cref="Hyperlink"/>, no
/// Invoke pattern; DS-1 S1). <see cref="Text"/> in, <see cref="ThreadText"/> blocks out; rebuilt
/// when the text changes, which a streaming message does per chunk.
/// </summary>
/// <remarks>
/// <b>Reuse-in-codebase, answered:</b> the composer's page carries CodeMirror's markdown
/// <i>language</i> (an editor's highlighter) and the docs site renders in a browser — neither is a
/// WPF renderer, and Markdig is not an installed dependency (ladder L1: native before a new
/// package). The parser is Core's <see cref="ProseMarkdown"/>, golden-tested; this maps blocks to
/// elements and nothing else.
/// </remarks>
public sealed class ProseView : StackPanel
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(ProseView), new PropertyMetadata(string.Empty, (d, _) => ((ProseView)d).Rebuild()));

    /// <summary>The markdown source.</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private void Rebuild()
    {
        Children.Clear();
        foreach (var block in ProseMarkdown.Parse(Text ?? string.Empty))
        {
            Children.Add(Element(block));
        }
    }

    private static UIElement Element(ProseBlock block) => block switch
    {
        ProseBlock.Heading h => Line(h.Inlines, 13, h.Level switch { 1 => FontWeights.Bold, 2 => FontWeights.SemiBold, _ => FontWeights.Medium }, new Thickness(0, 8, 0, 2)),
        ProseBlock.Paragraph p => Line(p.Inlines, 13, FontWeights.Normal, new Thickness(0, 4, 0, 0)),
        ProseBlock.ListBlock l => List(l),
        ProseBlock.Code c => Code(c.Text),
        ProseBlock.Table t => Table(t),
        _ => throw new ArgumentOutOfRangeException(nameof(block), block, "a block outside the subset"),
    };

    private static ThreadText Line(IReadOnlyList<ProseInline> inlines, double size, FontWeight weight, Thickness margin)
    {
        var text = new ThreadText { FontSize = size, FontWeight = weight, Margin = margin, TextWrapping = TextWrapping.Wrap, LineHeight = size * 1.5 };
        text.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        foreach (var span in inlines)
        {
            text.Inlines.Add(Run(span));
            if (span.Kind == ProseInlineKind.Link)
            {
                // The URL beside its text, visible to everyone — never hover-only, never a control (A11-2).
                var url = new Run(" (" + span.Url + ")") { FontFamily = ThreadFeed.Mono, FontSize = 12 };
                url.SetResourceReference(TextElement.ForegroundProperty, "TextMutedBrush");
                text.Inlines.Add(url);
            }
        }

        return text;
    }

    private static Run Run(ProseInline span) => span.Kind switch
    {
        ProseInlineKind.Code => new Run(span.Text) { FontFamily = ThreadFeed.Mono, FontSize = 12 },
        ProseInlineKind.Bold => new Run(span.Text) { FontWeight = FontWeights.SemiBold },
        ProseInlineKind.Italic => new Run(span.Text) { FontStyle = FontStyles.Italic },
        _ => new Run(span.Text),
    };

    private static StackPanel List(ProseBlock.ListBlock list)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
        for (var i = 0; i < list.Items.Count; i++)
        {
            var row = new DockPanel();
            var marker = new ThreadText { Text = list.Ordered ? (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "." : "•", FontSize = 13, MinWidth = 20, LineHeight = 13 * 1.5 };
            marker.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
            DockPanel.SetDock(marker, Dock.Left);
            row.Children.Add(marker);
            row.Children.Add(Line(list.Items[i], 13, FontWeights.Normal, new Thickness(0)));
            panel.Children.Add(row);
        }

        return panel;
    }

    private static Border Code(string text)
    {
        var block = new ThreadText { Text = text, FontFamily = ThreadFeed.Mono, FontSize = 12, TextWrapping = TextWrapping.Wrap };
        block.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        var border = new Border { Child = block, Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 4, 0, 0), CornerRadius = new CornerRadius(4) };
        border.SetResourceReference(Border.BackgroundProperty, "SurfaceSunkenBrush");
        return border;
    }

    private static Grid Table(ProseBlock.Table table)
    {
        var grid = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        var columns = Math.Max(table.Header.Count, table.Rows.Count == 0 ? 0 : table.Rows.Max(r => r.Count));
        for (var c = 0; c < columns; c++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = c == columns - 1 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });
        }

        var rows = table.Rows.Prepend(table.Header).ToList();
        for (var r = 0; r < rows.Count; r++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (var c = 0; c < rows[r].Count && c < columns; c++)
            {
                // A hairline under every row (DX13: a table by hairline, never a box); the header's weight carries its role.
                var cell = new Border { Padding = new Thickness(c == 0 ? 0 : 10, 3, 10, 3), BorderThickness = new Thickness(0, 0, 0, 1) };
                cell.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
                cell.Child = Line(rows[r][c], 13, r == 0 ? FontWeights.SemiBold : FontWeights.Normal, new Thickness(0));
                Grid.SetRow(cell, r);
                Grid.SetColumn(cell, c);
                grid.Children.Add(cell);
            }
        }

        return grid;
    }
}
