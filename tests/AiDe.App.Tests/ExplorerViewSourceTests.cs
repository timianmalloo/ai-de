using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation;
using AiDe.Core.Workbench;
using ICSharpCode.AvalonEdit;

namespace AiDe.App.Tests;

/// <summary>
/// <b>Ruling 93 — "View source" in Explore renders in the reader pane through
/// <c>NodeContentAsync</c>.</b> The operator's words: <i>"right click on a node … choose view
/// source and see the source in the right viewer eg code or rendered markdown or html."</i> One
/// rendered-surface control per <c>RenderKind</c> — code, markdown, plain text, html, the html
/// fallback, none/shortfall — plus loading, failure, the late-reply discard, and the menu's first
/// item. The reader is measured detached (the fast ring's idiom: Measure · Arrange · UpdateLayout);
/// the sandbox's live half is the out-of-process probe (<see cref="HtmlSandboxIntegrationTests"/>).
/// </summary>
public sealed class ExplorerViewSourceTests
{
    private static readonly CanvasNode Node = new("Shop.Order", "Order", "class", true, "Orders");
    private static readonly CanvasNode Other = new("Shop.Customer", "Customer", "class", false, "Orders");

    private static readonly IReadOnlyList<CanvasEdge> Edges =
    [
        new("Shop.Order", "csharp:Shop:net10.0", "declared_in", "Verified"),
        new("Shop.Order", "Shop.Customer", "depends_on", "Verified"),
    ];

    private static Func<string, CancellationToken, Task<NodeContent>> Source(NodeContent content) =>
        (_, _) => Task.FromResult(content);

    private static NodeReaderView Reader(Func<string, CancellationToken, Task<NodeContent>>? source)
    {
        var reader = new NodeReaderView { ContentSource = source, Width = 480, Height = 640 };
        reader.Show(Node, Edges);
        Layout(reader);
        return reader;
    }

    private static void Layout(NodeReaderView reader)
    {
        reader.Measure(new Size(480, 640));
        reader.Arrange(new Rect(0, 0, 480, 640));
        reader.UpdateLayout();
    }

    private static string Plain(TextBlock block) => new TextRange(block.ContentStart, block.ContentEnd).Text;

    private static void OnSta(Func<Task> work) =>
        Sta.Run(() => { work().GetAwaiter().GetResult(); return true; }, 60);

    // (1a) Code: highlighted, read-only, below the metadata and edges — the rendered surface.
    [Fact]
    public void ViewSource_RendersCodeReadOnlyAndHighlighted_BelowTheMetadataAndEdges()
    {
        OnSta(async () =>
        {
            var reader = Reader(Source(new NodeContent("Shop.Order", NodeContentKind.Code, "csharp", "public class Order { }")));

            await reader.ViewSourceAsync("Shop.Order");
            Layout(reader);

            Assert.Equal(NodeReaderContentState.Code, reader.ContentState);
            var editor = Assert.Single(ThreadFixtures.Visuals<TextEditor>(reader));
            Assert.True(editor.IsReadOnly, "the source is editable");
            Assert.Equal("C#", editor.SyntaxHighlighting?.Name);
            Assert.Equal("public class Order { }", editor.Text);
            Assert.True(editor.ActualHeight > 0, "the editor was not laid out");

            var edge = reader.FocusStops.Skip(1).OfType<Button>().Last();
            var edgeY = edge.TranslatePoint(new Point(0, 0), reader).Y;
            var editorY = editor.TranslatePoint(new Point(0, 0), reader).Y;
            Assert.True(editorY > edgeY, $"the content (Y={editorY:0.#}) is not below the last edge row (Y={edgeY:0.#})");
        });
    }

    // (1b) Markdown: the WPF prose renderer, links as text — never a control.
    [Fact]
    public void ViewSource_RendersMarkdownAsProse_WithLinksAsTextNotControls()
    {
        OnSta(async () =>
        {
            const string markdown = "# Decision\n\nThe body, with a [link](https://example.invalid/adr).\n";
            var reader = Reader(Source(new NodeContent("Shop.Order", NodeContentKind.Text, "markdown", markdown)));

            await reader.ViewSourceAsync("Shop.Order");
            Layout(reader);

            Assert.Equal(NodeReaderContentState.Markdown, reader.ContentState);
            var prose = Assert.Single(ThreadFixtures.Visuals<ProseView>(reader));
            var words = ThreadFixtures.Visuals<TextBlock>(prose).Select(Plain).ToList();
            Assert.Contains(words, w => w.Contains("Decision", StringComparison.Ordinal));           // rendered, not "# Decision"
            Assert.DoesNotContain(words, w => w.Contains("# Decision", StringComparison.Ordinal));
            Assert.Contains(words, w => w.Contains("https://example.invalid/adr", StringComparison.Ordinal)); // the URL is visible text
            Assert.Empty(ThreadFixtures.Visuals<TextBlock>(prose).SelectMany(t => t.Inlines).OfType<Hyperlink>());
        });
    }

    // (1c) Plain text (a .txt/.log node): read-only, no highlighting.
    [Fact]
    public void ViewSource_RendersPlainTextReadOnlyWithoutHighlighting()
    {
        OnSta(async () =>
        {
            var reader = Reader(Source(new NodeContent("Shop.Order", NodeContentKind.Text, null, "plain words")));

            await reader.ViewSourceAsync("Shop.Order");
            Layout(reader);

            Assert.Equal(NodeReaderContentState.Text, reader.ContentState);
            var editor = Assert.Single(ThreadFixtures.Visuals<TextEditor>(reader));
            Assert.Null(editor.SyntaxHighlighting);
            Assert.Equal("plain words", editor.Text);
        });
    }

    // (1d) Html: the sandbox host, and no document until the sandbox is asserted.
    [Fact]
    public void ViewSource_RendersHtmlInTheSandboxHost_AndHoldsTheDocumentUntilTheSandboxIsAsserted()
    {
        OnSta(async () =>
        {
            var reader = Reader(Source(new NodeContent("Shop.Order", NodeContentKind.Html, "html", "<h1>Hello</h1><script>document.title='ran'</script>")));
            try
            {
                await reader.ViewSourceAsync("Shop.Order");
                Layout(reader);

                Assert.Equal(NodeReaderContentState.Html, reader.ContentState);
                var host = Assert.Single(ThreadFixtures.Visuals<HtmlSandboxHost>(reader));
                Assert.False(host.IsSandboxed, "a detached host has no runtime, so nothing can have been asserted");
                Assert.Equal(0, host.DocumentsShown);
                Assert.Empty(ThreadFixtures.Visuals<TextEditor>(reader));
            }
            finally
            {
                reader.Dispose();
            }
        });
    }

    // (1e) Html fallback: when the sandbox cannot be asserted the source is shown highlighted as html.
    [Fact]
    public void ViewSource_FallsBackToHighlightedHtmlSource_WhenTheSandboxIsUnavailable()
    {
        OnSta(async () =>
        {
            const string html = "<h1>Hello</h1>";
            var reader = Reader(Source(new NodeContent("Shop.Order", NodeContentKind.Html, "html", html)));
            try
            {
                await reader.ViewSourceAsync("Shop.Order");
                Layout(reader);
                var host = Assert.Single(ThreadFixtures.Visuals<HtmlSandboxHost>(reader));

                host.ReportUnavailable("the WebView2 runtime is not installed");
                Layout(reader);

                Assert.Equal(NodeReaderContentState.HtmlFallback, reader.ContentState);
                Assert.Empty(ThreadFixtures.Visuals<HtmlSandboxHost>(reader));
                var editor = Assert.Single(ThreadFixtures.Visuals<TextEditor>(reader));
                Assert.Equal("HTML", editor.SyntaxHighlighting?.Name);
                Assert.Equal(html, editor.Text);
                Assert.Contains(
                    ThreadFixtures.Visuals<TextBlock>(reader).Select(Plain),
                    t => t.Contains("the WebView2 runtime is not installed", StringComparison.Ordinal));
            }
            finally
            {
                reader.Dispose();
            }
        });
    }

    // (1f) None: the shortfall sentence, verbatim.
    [Fact]
    public void ViewSource_RendersTheShortfallSentenceVerbatim_ForNone()
    {
        OnSta(async () =>
        {
            const string shortfall = "this node has no recorded source";
            var reader = Reader(Source(new NodeContent("Shop.Order", NodeContentKind.None, null, "", shortfall)));

            await reader.ViewSourceAsync("Shop.Order");
            Layout(reader);

            Assert.Equal(NodeReaderContentState.None, reader.ContentState);
            Assert.Contains(ThreadFixtures.Visuals<TextBlock>(reader).Select(Plain), t => t == shortfall);
            Assert.Empty(ThreadFixtures.Visuals<TextEditor>(reader));
        });
    }

    // (1g) A bounded reply keeps its shortfall beside the content.
    [Fact]
    public void ViewSource_ShowsTheShortfallBesideBoundedContent()
    {
        OnSta(async () =>
        {
            const string shortfall = "first 256 KB of 900 KB — open the source for the rest";
            var reader = Reader(Source(new NodeContent("Shop.Order", NodeContentKind.Text, "markdown", "# Big", shortfall)));

            await reader.ViewSourceAsync("Shop.Order");
            Layout(reader);

            Assert.Contains(ThreadFixtures.Visuals<TextBlock>(reader).Select(Plain), t => t == shortfall);
        });
    }

    // Loading is a state, shown while the query is in flight; and a late reply for another node is discarded.
    [Fact]
    public void ViewSource_ShowsLoadingWhileInFlight_AndDiscardsALateReplyForAnotherNode()
    {
        OnSta(async () =>
        {
            var reply = new TaskCompletionSource<NodeContent>();
            var reader = Reader((_, _) => reply.Task);

            var pending = reader.ViewSourceAsync("Shop.Order");
            Layout(reader);
            Assert.Equal(NodeReaderContentState.Loading, reader.ContentState);
            Assert.Contains(ThreadFixtures.Visuals<TextBlock>(reader).Select(Plain), t => t.StartsWith("Loading", StringComparison.Ordinal));

            reader.Show(Other, Edges);                                             // the selection moved on
            reply.SetResult(new NodeContent("Shop.Order", NodeContentKind.Code, "csharp", "class Order {}"));
            await pending;
            Layout(reader);

            Assert.Equal("Shop.Customer", reader.SelectedNodeId);
            Assert.Equal(NodeReaderContentState.Idle, reader.ContentState);
            Assert.Empty(ThreadFixtures.Visuals<TextEditor>(reader));
        });
    }

    // A query that throws is a sentence, not a crash and not a stuck "Loading".
    [Fact]
    public void ViewSource_SaysWhenTheQueryFails()
    {
        OnSta(async () =>
        {
            var reader = Reader((_, _) => throw new InvalidOperationException("the daemon went away"));

            await reader.ViewSourceAsync("Shop.Order");
            Layout(reader);

            Assert.Equal(NodeReaderContentState.Failed, reader.ContentState);
            Assert.Contains(
                ThreadFixtures.Visuals<TextBlock>(reader).Select(Plain),
                t => t.Contains("the daemon went away", StringComparison.Ordinal));
        });
    }

    // The Phase-1 placeholder sentence is deleted; before View source the content area says how to get it.
    [Fact]
    public void ThePlaceholderSentenceIsGone_AndTheIdleContentAreaNamesTheGesture()
    {
        OnSta(() =>
        {
            var reader = Reader(null);
            var words = ThreadFixtures.Visuals<TextBlock>(reader).Select(Plain).ToList();

            Assert.DoesNotContain(words, w => w.Contains("ADR-0018", StringComparison.Ordinal));
            Assert.DoesNotContain(words, w => w.Contains("arrives with the node-content query", StringComparison.Ordinal));
            Assert.Equal(NodeReaderContentState.Idle, reader.ContentState);
            Assert.Contains(words, w => w.Contains("View source", StringComparison.Ordinal));
            return Task.CompletedTask;
        });
    }

    // Explore's node menu: View source first (Ruling 93); a click loads the selected node's source.
    [Fact]
    public void ExploresNodeMenu_OffersViewSourceFirst_AndAClickRendersTheSelectedNodesSource()
    {
        OnSta(async () =>
        {
            var graph = new CanvasSurface("explorer", "Explorer");
            var reader = new NodeReaderView { ContentSource = Source(new NodeContent("Shop.Order", NodeContentKind.Code, "csharp", "class Order {}")) };
            var surface = new ExplorerSurface(graph, reader);
            try
            {
                reader.Show(Node, Edges);

                graph.RequestNodeContextMenu(new NodeContextMenuRequest("Shop.Order", "class", false));

                var menu = Assert.IsType<ContextMenu>(surface.LastNodeMenu);
                var first = Assert.IsType<MenuItem>(menu.Items[0]);
                Assert.Equal("View source", first.Header);

                first.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                await surface.PendingViewSource;

                Assert.Equal(NodeReaderContentState.Code, reader.ContentState);
                Assert.Equal("Shop.Order", reader.SelectedNodeId);
            }
            finally
            {
                graph.Dispose();
            }
        });
    }
}
