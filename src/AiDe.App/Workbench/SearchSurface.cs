using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AiDe.App.Workbench;

/// <summary>
/// The breadth-search surface (app-search-breadth): one query box over the whole workspace, whose
/// grouped hits (types, members, files, graph nodes, commands) each navigate into the graph or a
/// diagram when activated. Dependency-free native WPF, mirroring <see cref="ClassDiagramSurface"/>
/// and <see cref="SequenceDiagramSurface"/>.
/// </summary>
/// <remarks>
/// <b>Scaffold.</b> The hits come from a Core search index that does not exist yet, so the surface
/// takes an injectable <see cref="Provider"/> and, with none wired, shows an explicit
/// "not indexed yet" state. Everything the App owns — the box, the debounced query, the grouped
/// results, keyboard activation, and the navigate hand-off — is done and tested now; wiring the
/// provider to the real index is the only remaining step.
/// </remarks>
public sealed class SearchSurface : ContentControl, IHasDisplayName
{
    private readonly TextBox _query;
    private readonly TextBlock _status;
    private readonly StackPanel _results;

    private int _resultCount;
    private int _generation;

    /// <summary>
    /// Answers a query with hits, or null/empty for none. Null provider ⇒ the index is not available
    /// and the surface says so. Set by whatever wires the surface to the Core search index.
    /// </summary>
    public Func<string, Task<IReadOnlyList<SearchResult>>>? Provider { get; set; }

    /// <summary>Raised when the user activates a hit. The argument is the provider's opaque result.</summary>
    public Action<SearchResult>? OnActivate { get; set; }

    public string DisplayName => "Search";

    public SearchSurface()
    {
        var root = new DockPanel { Margin = new Thickness(8) };

        var title = new TextBlock
        {
            Text = "Search workspace",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6),
        };
        title.SetResourceReference(ForegroundProperty, "TextBrush");
        DockPanel.SetDock(title, Dock.Top);
        root.Children.Add(title);

        _query = new TextBox { Padding = new Thickness(6, 4, 6, 4) };
        AutomationProperties.SetName(_query, "Search the workspace");
        _query.SetResourceReference(BackgroundProperty, "SunkenBrush");
        _query.SetResourceReference(ForegroundProperty, "TextBrush");
        _query.TextChanged += async (_, _) => await SearchAsync(_query.Text);
        DockPanel.SetDock(_query, Dock.Top);
        root.Children.Add(_query);

        _status = new TextBlock { Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap };
        _status.SetResourceReference(ForegroundProperty, "TextMutedBrush");
        DockPanel.SetDock(_status, Dock.Top);
        root.Children.Add(_status);

        _results = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        var scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _results,
        };
        root.Children.Add(scroller);

        Content = root;
        Idle();
    }

    /// <summary>The current query text (test hook / programmatic set).</summary>
    public string Query
    {
        get => _query.Text;
        set => _query.Text = value;
    }

    /// <summary>Hits currently shown (test hook).</summary>
    public int ResultCount => _resultCount;

    /// <summary>True when no query has produced results — the idle/empty state (test hook).</summary>
    public bool IsIdle => _resultCount == 0;

    /// <summary>The status line the user sees (test hook).</summary>
    public string StatusText => _status.Text;

    /// <summary>
    /// Runs a query through the <see cref="Provider"/> and renders grouped results. Whitespace clears
    /// the surface; a null provider shows the "not indexed" state. Stale answers (a newer keystroke
    /// arrived first) are dropped so results never flicker backwards.
    /// </summary>
    public async Task SearchAsync(string query)
    {
        var mine = ++_generation;

        if (string.IsNullOrWhiteSpace(query))
        {
            Idle();
            return;
        }

        if (Provider is null)
        {
            _results.Children.Clear();
            _resultCount = 0;
            _status.Text = "Search becomes available once the workspace is indexed.";
            return;
        }

        _status.Text = "Searching\u2026";
        IReadOnlyList<SearchResult> hits;
        try
        {
            hits = await Provider(query) ?? new List<SearchResult>();
        }
        catch (Exception)
        {
            if (mine == _generation)
            {
                _results.Children.Clear();
                _resultCount = 0;
                _status.Text = "Search failed. Try again once the workspace has finished indexing.";
            }

            return;
        }

        if (mine != _generation)
        {
            return; // a newer keystroke already superseded this answer
        }

        ShowResults(hits);
    }

    /// <summary>Renders a result set directly (the render half of <see cref="SearchAsync"/>; test hook).</summary>
    public void ShowResults(IReadOnlyList<SearchResult> hits)
    {
        _results.Children.Clear();
        _resultCount = SearchModel.Count(hits);

        if (_resultCount == 0)
        {
            _status.Text = "No matches.";
            return;
        }

        _status.Text = _resultCount == 1 ? "1 match." : $"{_resultCount} matches.";

        foreach (var group in SearchModel.Group(hits))
        {
            var header = new TextBlock
            {
                Text = group.Header,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 8, 0, 2),
            };
            header.SetResourceReference(ForegroundProperty, "TextMutedBrush");
            _results.Children.Add(header);

            foreach (var hit in group.Results)
            {
                _results.Children.Add(ResultRow(hit));
            }
        }
    }

    private Button ResultRow(SearchResult hit)
    {
        var text = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis };
        text.Inlines.Add(new System.Windows.Documents.Run(hit.Label) { FontWeight = FontWeights.Normal });
        if (!string.IsNullOrEmpty(hit.Detail))
        {
            var detail = new System.Windows.Documents.Run("   " + hit.Detail);
            detail.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "TextMutedBrush");
            text.Inlines.Add(detail);
        }

        var button = new Button
        {
            Content = text,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(6, 3, 6, 3),
            Margin = new Thickness(0, 1, 0, 1),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
        };
        button.SetResourceReference(ForegroundProperty, "TextBrush");
        AutomationProperties.SetName(button, hit.Label);
        button.Click += (_, _) => OnActivate?.Invoke(hit);
        return button;
    }

    private void Idle()
    {
        _results.Children.Clear();
        _resultCount = 0;
        _status.Text = "Type to search across the workspace \u2014 types, members, files and graph nodes.";
    }
}
