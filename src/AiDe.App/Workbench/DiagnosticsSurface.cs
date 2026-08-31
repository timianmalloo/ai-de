using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace AiDe.App.Workbench;

/// <summary>
/// The report a <see cref="DiagnosticsSurface"/> renders. Built by the shell from the last re-index
/// (<c>IndexSummary</c>, its disclosures folded by <c>DisclosureSummary.Fold</c>) plus the daemon
/// diagnostics. A plain record so the surface is verifiable headlessly.
/// </summary>
public sealed record DiagnosticsReport(
    string? IndexSummary,
    IReadOnlyList<string> Disclosures,
    int FailedScopes,
    string? Daemon)
{
    public bool HasIndex => IndexSummary is not null;
}

/// <summary>
/// The workspace Diagnostics pane: the re-index analysis coverage (folded "not analysed" disclosures,
/// grouped by category with the counts summed) and the daemon state — the browsable home for what was
/// a 200-line wall in the one-line status strip. Host-side WPF, so it renders on an STA thread.
/// </summary>
public sealed class DiagnosticsSurface : ContentControl
{
    private readonly TextBlock _header;
    private readonly ScrollViewer _scroller;
    private readonly StackPanel _body;

    public DiagnosticsSurface(string title = "Diagnostics")
    {
        AutomationProperties.SetName(this, title);

        _header = new TextBlock
        {
            Text = title,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8),
        };
        _header.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

        _body = new StackPanel();
        _scroller = new ScrollViewer
        {
            Content = _body,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        var root = new DockPanel { Margin = new Thickness(14), LastChildFill = true };
        DockPanel.SetDock(_header, Dock.Top);
        root.Children.Add(_header);
        root.Children.Add(_scroller);

        Content = root;
        ShowEmpty();
    }

    /// <summary>Test hook: whether the pane is showing the "nothing yet" empty state.</summary>
    internal bool IsEmpty { get; private set; }

    /// <summary>Test hook: how many folded disclosure lines are drawn.</summary>
    internal int DisclosureLineCount { get; private set; }

    /// <summary>Test hook: whether an index-summary header is shown.</summary>
    internal bool HasIndexSummary { get; private set; }

    public void ShowLoading()
    {
        Reset();
        _body.Children.Add(Muted("Reading diagnostics…"));
    }

    public void ShowError(string message)
    {
        Reset();
        var t = Muted($"Diagnostics unavailable: {message}");
        _body.Children.Add(t);
    }

    public void ShowEmpty()
    {
        Reset();
        IsEmpty = true;
        _body.Children.Add(Muted(
            "No index has run this session. Run Re-index (Ctrl+K, I) to see which parts of the workspace were analysed."));
    }

    public void Show(DiagnosticsReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        Reset();

        if (report.IndexSummary is { Length: > 0 } summary)
        {
            HasIndexSummary = true;
            _body.Children.Add(Line(summary, bold: true, size: 12.5));
            if (report.FailedScopes > 0)
            {
                _body.Children.Add(Muted($"{report.FailedScopes} scope(s) failed and were quarantined."));
            }
            _body.Children.Add(Spacer(6));
        }

        if (report.Disclosures.Count > 0)
        {
            _body.Children.Add(SectionHeader($"Not analysed — {report.Disclosures.Count} boundary type(s)"));

            // Group the folded, one-per-class lines by category (the token before the first hyphen:
            // "knowledge", "python", "calls", …) so a reader scans categories, not 100 sentences.
            var groups = report.Disclosures
                .GroupBy(Category, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                _body.Children.Add(GroupHeader(group.Key, group.Count()));
                foreach (var line in group.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    _body.Children.Add(Line("  " + line, bold: false, size: 11.5));
                    DisclosureLineCount++;
                }
            }
            _body.Children.Add(Spacer(8));
        }
        else if (report.HasIndex)
        {
            _body.Children.Add(Muted("Everything in scope was analysed — no boundaries to report."));
            _body.Children.Add(Spacer(8));
        }

        if (report.Daemon is { Length: > 0 } daemon)
        {
            _body.Children.Add(SectionHeader("Daemon"));
            _body.Children.Add(Wrapped(daemon));
        }

        if (!HasIndexSummary && report.Disclosures.Count == 0 && string.IsNullOrEmpty(report.Daemon))
        {
            ShowEmpty();
        }
    }

    // The category is the class name's first hyphen-delimited token; a name with no hyphen is its own.
    private static string Category(string disclosureLine)
    {
        var name = disclosureLine.TrimStart();
        var space = name.IndexOf(' ');
        if (space > 0) { name = name[..space]; }
        var hyphen = name.IndexOf('-');
        return hyphen > 0 ? name[..hyphen] : name;
    }

    private void Reset()
    {
        _body.Children.Clear();
        IsEmpty = false;
        HasIndexSummary = false;
        DisclosureLineCount = 0;
    }

    private static TextBlock Line(string text, bool bold, double size)
    {
        var t = new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 1, 0, 1),
        };
        t.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        return t;
    }

    private static TextBlock SectionHeader(string text)
    {
        var t = new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 8, 0, 4),
        };
        t.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        return t;
    }

    private static TextBlock GroupHeader(string category, int count)
    {
        var t = new TextBlock
        {
            Text = $"{category} · {count}",
            FontSize = 11.5,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 5, 0, 1),
        };
        t.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return t;
    }

    private static TextBlock Wrapped(string text)
    {
        var t = new TextBlock { Text = text, FontSize = 11.5, TextWrapping = TextWrapping.Wrap };
        t.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        return t;
    }

    private static TextBlock Muted(string text)
    {
        var t = new TextBlock { Text = text, FontSize = 11.5, TextWrapping = TextWrapping.Wrap };
        t.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return t;
    }

    private static FrameworkElement Spacer(double height) =>
        new Border { Height = height, Background = Brushes.Transparent };
}
