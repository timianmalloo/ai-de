using AiDe.App.Workbench;
using AiDe.Core.Presentation;

namespace AiDe.App.Tests.Canvas;

/// <summary>
/// The knowledge-inclusive representation is a MODE of the admitted <c>canvas</c> kind (Ruling 149
/// (2); design WL-S §8), never a second graph surface or kind.
/// </summary>
/// <remarks>
/// <para><b>Through the real surface.</b> Every check drives a real <see cref="CanvasSurface"/> with
/// its real WebView2 page: the mode is chosen by the page's own control, and the drawn nodes are read
/// back from the page's DOM. A fake page would have no control to press.</para>
///
/// <para><b>The source mirrors the seam.</b> The test's graph source applies the canvas's mode to a
/// real <see cref="CanvasGraphViewModel"/> and returns the knowledge node only when the view model's
/// <see cref="CanvasGraphViewModel.ExcludeKnowledge"/> is off: the same parameter the projection
/// reads (<c>GraphProjection.cs</c>, <c>ExcludeKnowledge_DropsDeclaredKnowledgeNodes_…</c>).</para>
/// </remarks>
public sealed class KnowledgeModeTests
{
    private const string CodeNode = "src/Code.cs";
    private const string KnowledgeNode = "docs/knowledge/glossary.md";

    private sealed class SeamSource(CanvasSurface canvas)
    {
        public readonly CanvasGraphViewModel Graph = new(null);
        public int Calls;

        public Task<CanvasGraph> Get(string? rootId, CancellationToken _)
        {
            Interlocked.Increment(ref Calls);
            canvas.ApplyKnowledgeMode(Graph);

            List<CanvasNode> nodes = [new(CodeNode, "Code", "class", IsRoot: false)];
            if (!Graph.ExcludeKnowledge)
            {
                nodes.Add(new(KnowledgeNode, "Glossary", "knowledge", IsRoot: false, IsKnowledge: true));
            }

            return Task.FromResult(new CanvasGraph(nodes, [], null, 0, [], null, DeclaredByKind: null));
        }
    }

    private static void OnUiThread(Func<CanvasSurface, SeamSource, Task> work)
    {
        SeamSource? source = null;
        Sta.Pump<CanvasSurface>(
            create: () =>
            {
                var canvas = new CanvasSurface("canvas-1", "Graph");
                source = new SeamSource(canvas);
                canvas.GraphSource = source.Get;
                return canvas;
            },
            body: (_, canvas) => work(canvas, source!));
    }

    private static async Task WaitFor(Func<Task<bool>> condition, string what)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition()) return;
            await Task.Delay(50);
        }

        Assert.Fail("timed out waiting for " + what);
    }

    private static async Task<bool> Drawn(CanvasSurface canvas, string id) =>
        await canvas.EvaluateAsync($"document.querySelector('.node[data-id=\"{id}\"]') !== null") == "true";

    private static Task<string> Attr(CanvasSurface canvas, string attribute) =>
        canvas.EvaluateAsync($"(function(){{var b=document.getElementById('knowledge');return b?b.getAttribute('{attribute}'):'(no control)';}})()");

    [Fact]
    public void TheSelector_OffersAKnowledgeInclusiveMode_ThatShowsThenHidesKnowledgeNodes_OnTheOneCanvas() =>
        OnUiThread(async (canvas, source) =>
        {
            await WaitFor(() => Task.FromResult(canvas.Ready), "the canvas page");
            await WaitFor(() => Drawn(canvas, CodeNode), "the code-only default to draw");
            Assert.False(await Drawn(canvas, KnowledgeNode), "the default drew a knowledge node");

            var control = await canvas.EvaluateAsync("document.getElementById('knowledge') !== null");
            Assert.True(control == "true", "the canvas offers no knowledge-inclusive mode control");

            await canvas.EvaluateAsync("document.getElementById('knowledge').click()");
            await WaitFor(() => Drawn(canvas, KnowledgeNode), "the knowledge node to draw in the mode");
            Assert.True(canvas.KnowledgeInclusive);

            await canvas.EvaluateAsync("document.getElementById('knowledge').click()");
            await WaitFor(async () => !await Drawn(canvas, KnowledgeNode), "the knowledge node to hide again");
            Assert.False(canvas.KnowledgeInclusive);

            // One surface, one page: the mode is on this canvas, never a second graph surface.
            Assert.Equal("1", await canvas.EvaluateAsync("document.querySelectorAll('#stage').length"));
        });

    [Fact]
    public void TheMode_TurnsTheExcludeKnowledgeSeamOff_ForThisCanvasOnly_AndTheDefaultStaysCodeOnly() =>
        Sta.Run(() =>
        {
            using var chosen = new CanvasSurface("canvas-1", "Graph");
            using var other = new CanvasSurface("canvas-2", "Graph");
            var chosenGraph = new CanvasGraphViewModel(null);
            var otherGraph = new CanvasGraphViewModel(null);

            chosen.ApplyKnowledgeMode(chosenGraph);
            other.ApplyKnowledgeMode(otherGraph);
            Assert.True(chosenGraph.ExcludeKnowledge, "the code-only default changed");
            Assert.True(otherGraph.ExcludeKnowledge, "the code-only default changed");

            chosen.SetKnowledgeInclusive(true);
            chosen.ApplyKnowledgeMode(chosenGraph);
            other.ApplyKnowledgeMode(otherGraph);

            Assert.False(chosenGraph.ExcludeKnowledge, "the mode did not turn the seam off");
            Assert.True(otherGraph.ExcludeKnowledge, "the mode leaked to another canvas instance");
            return 0;
        });

    [Fact]
    public void TheMode_IsKeyboardReachable_AndItsStateIsExposedToUia_InTheDesignsWords() =>
        OnUiThread(async (canvas, _) =>
        {
            await WaitFor(() => Task.FromResult(canvas.Ready), "the canvas page");

            // A native <button> is in the Tab order and maps to a UIA Toggle through aria-pressed.
            Assert.Equal("\"BUTTON\"", await canvas.EvaluateAsync("(document.getElementById('knowledge')||{}).tagName||'none'"));
            Assert.Equal("\"Knowledge-inclusive mode\"", await Attr(canvas, "aria-label"));
            Assert.Equal("\"false\"", await Attr(canvas, "aria-pressed"));

            await canvas.EvaluateAsync("document.getElementById('knowledge').focus()");
            Assert.True(await canvas.SendKeyAsync("Enter", 0x0D, text: "\r"), "the key could not be sent");

            await WaitFor(() => Task.FromResult(canvas.KnowledgeInclusive), "Enter to choose the mode");
            Assert.Equal("\"true\"", await Attr(canvas, "aria-pressed"));
        });
}
