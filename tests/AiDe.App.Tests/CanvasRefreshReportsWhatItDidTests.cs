using System.Windows;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.Core.Presentation;

namespace AiDe.App.Tests;

/// <summary>
/// A refresh reports what it did, so nothing can announce a centring that did not happen.
/// </summary>
/// <remarks>
/// <para><b>The defect, measured rather than read.</b> Three shell sites ran
/// <c>Announcer.Announce($"Graph centred on {id}.")</c> and then <c>_ = canvas.RefreshAsync(id)</c>.
/// On a real surface with the page still loading, the design session observed: <c>Ready</c> false,
/// the task completed, and the graph source asked <b>0</b> times. The user had been told the graph
/// centred on a node it never looked up.</para>
///
/// <para><b>Why a real WebView2 and not a fake.</b> The whole subject is the window between
/// construction and <c>NavigationCompleted</c> — a fake that is ready on construction has no such
/// window, and a test driving it would pass while proving the opposite of the thing at issue
/// (DC-016). The not-ready case is asserted BEFORE waiting for load, which is the only moment it
/// exists.</para>
///
/// <para><b>Two claims, not one.</b> That the refresh reports honestly, and that a request made
/// while loading is later HONOURED. Reporting the loss without fixing it would be a true sentence
/// about a broken feature.</para>
/// </remarks>
public sealed class CanvasRefreshReportsWhatItDidTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    /// <summary>Counts what the surface actually asked for — the measurement that found this.</summary>
    private sealed class CountingSource
    {
        public int Calls;
        public readonly List<string?> Roots = [];

        public Task<CanvasGraph> Get(string? rootId, CancellationToken _)
        {
            Interlocked.Increment(ref Calls);
            lock (Roots) Roots.Add(rootId);

            CanvasNode[] nodes =
            [
                new("node.Drawn", "Drawn", "class", IsRoot: rootId == "node.Drawn"),
                new("node.Other", "Other", "class", IsRoot: false),
            ];

            return Task.FromResult(new CanvasGraph(
                nodes, [], null, 0, [], null, DeclaredByKind: null));
        }
    }

    private static void OnUiThread(Func<CanvasSurface, CountingSource, Task> work)
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                var canvas = new CanvasSurface("canvas-1", "Graph");
                var source = new CountingSource();
                canvas.GraphSource = source.Get;

                var window = new Window
                {
                    Content = canvas, Width = 640, Height = 480,
                    Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false,
                };

                window.Show();

                var dispatcher = Dispatcher.CurrentDispatcher;
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(dispatcher));

                var done = false;
                _ = Task.Run(() => { }).ContinueWith(async _ =>
                {
                    try { await work(canvas, source); }
                    catch (Exception ex) { failure = ex; }
                    finally { done = true; }
                }, TaskScheduler.FromCurrentSynchronizationContext());

                var deadline = DateTime.UtcNow + Timeout;
                while (!done && DateTime.UtcNow < deadline)
                {
                    dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
                    Thread.Sleep(10);
                }

                if (!done) failure ??= new TimeoutException("the canvas refresh test did not complete");

                window.Close();
                canvas.Dispose();
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(Timeout + TimeSpan.FromSeconds(30)), "the canvas UI thread did not finish");

        // A FAILED ASSERTION IS RETHROWN AS ITSELF. Wrapping everything in "requires the WebView2
        // runtime" — as the harness this was modelled on does — reports a real defect as a broken
        // machine, which is the same shape as the defect this file is about: a message naming a
        // cause that is not the cause. Only an environment failure gets the environment message.
        if (failure is Xunit.Sdk.XunitException) throw failure;

        if (failure is not null)
        {
            // Not a skip, for the reason CanvasFocusIntegrationTests states: a missing WebView2
            // runtime is a broken environment, and a quiet pass here would restore exactly the
            // false-success this file exists to prevent.
            throw new InvalidOperationException(
                "the canvas refresh test requires a real window and the WebView2 runtime. "
                + failure.Message, failure);
        }
    }

    private static async Task<bool> WaitForReady(CanvasSurface canvas)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!canvas.Ready && DateTime.UtcNow < deadline) await Task.Delay(50);

        return canvas.Ready;
    }

    [Fact]
    public void ARefreshBeforeThePageIsReadySaysSoInsteadOfLookingLikeSuccess() =>
        OnUiThread(async (canvas, source) =>
        {
            // BEFORE the wait, deliberately: this is the only moment the window exists, and it is
            // the state a canvas is in for the first moments after it opens — the normal case for a
            // user who clicks a search hit straight away, not an exotic one.
            Assert.False(canvas.Ready, "the page was already loaded, so this test observed nothing");

            var result = await canvas.RefreshAsync("node.Drawn");

            Assert.Equal(CanvasRefreshOutcome.Deferred, result.Outcome);
            Assert.Equal(0, source.Calls);   // the measurement that found the defect
        });

    [Fact]
    public void ARootRequestedWhileLoadingIsHonouredWhenThePageArrives() =>
        OnUiThread(async (canvas, source) =>
        {
            Assert.False(canvas.Ready);

            await canvas.RefreshAsync("node.Drawn");

            Assert.True(await WaitForReady(canvas), "the canvas page never finished loading");

            // The load's own refresh carries the held root. Reporting the loss without honouring the
            // request would be an honest sentence about a feature that silently does nothing.
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
            while (DateTime.UtcNow < deadline)
            {
                lock (source.Roots)
                {
                    if (source.Roots.Contains("node.Drawn")) return;
                }

                await Task.Delay(50);
            }

            string asked;
            lock (source.Roots) asked = string.Join(", ", source.Roots.Select(r => r ?? "(none)"));

            Assert.Fail($"the deferred root was never applied; the source was asked for: {asked}");
        });

    [Fact]
    public void ANodeOutsideTheDrawnGraphIsReportedAsNotInView() =>
        OnUiThread(async (canvas, _) =>
        {
            Assert.True(await WaitForReady(canvas), "the canvas page never finished loading");

            // Not an error state and not rare: the graph draws a bounded most-connected-first slice,
            // and knowledge nodes have a measured median relation degree of 0, so a hit the user
            // picked is often outside it. The old code announced a centring for this case too.
            var result = await canvas.RefreshAsync("node.NotDrawn");

            Assert.Equal(CanvasRefreshOutcome.NotInView, result.Outcome);
        });

    [Fact]
    public void ANodeInTheDrawnGraphIsCentredAndNamedFromTheGraph() =>
        OnUiThread(async (canvas, _) =>
        {
            Assert.True(await WaitForReady(canvas), "the canvas page never finished loading");

            var result = await canvas.RefreshAsync("node.Drawn");

            Assert.Equal(CanvasRefreshOutcome.Centred, result.Outcome);

            // The LABEL comes from the drawn graph, not from the caller's id, so the sentence the
            // user hears cannot disagree with the picture they are looking at.
            Assert.Equal("Drawn", result.Label);
        });
}
