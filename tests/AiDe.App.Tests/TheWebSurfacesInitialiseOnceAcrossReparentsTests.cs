using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;

namespace AiDe.App.Tests;

/// <summary>
/// <b>INV-0007, finding 2 — the class control.</b> Each WebView2-hosting surface initialises its
/// browser <b>exactly once</b>, however many times the docking host re-parents it: every attach
/// after the first is heard, recorded as <c>re-attached</c>, and declined.
/// </summary>
/// <remarks>
/// <para>DC-138's control in the fast ring, for <b>both</b> surfaces; the shell probe
/// (<c>ComposerHostIntegrationTests.TheComposerPageSurvivesALaterRender</c>) proves the visible
/// consequence through the real docking host. Seen red by mutation: with the host's once-guard
/// removed, <c>InitialisationsStarted</c> read 4 for 3 re-parents.</para>
///
/// <para><b>The re-parent is the docking host's, not a stand-in for it.</b> <c>Adapter.Render()</c>
/// replaces <c>Manager.Layout</c>, which detaches every pane and attaches it to a new tree; setting
/// a host's content away and back raises the same <c>Unloaded</c>/<c>Loaded</c> pair on the
/// surface, which is the only thing the surface can see either way. The <c>re-attached</c> lines
/// are counted so the test cannot pass because nothing was re-parented.</para>
/// </remarks>
public sealed class TheWebSurfacesInitialiseOnceAcrossReparentsTests
{
    private const int Reparents = 3;

    /// <summary>
    /// The guard's own claim, deterministically: <b>an attach that lands while the runtime is still
    /// starting is a re-attach, not a second start.</b> The runtime start is held open on a
    /// <see cref="TaskCompletionSource"/>, <c>Loaded</c> is raised four times into it, and the
    /// initialiser runs once, after the release.
    /// </summary>
    /// <remarks>
    /// Seen red by mutation: with the flag set after the first await instead of before it,
    /// <c>InitialisationsStarted</c> read 4. The window-driven test above cannot pin this — there the
    /// runtime usually starts before the first re-parent lands, so that mutant passes it by timing.
    /// </remarks>
    [Fact]
    public void AnAttachDuringTheRuntimeStartIsAReattachNotASecondStart()
    {
        Sta.Run(() =>
        {
            var lines = new List<string>();
            var previous = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = lines.Add;

            try
            {
                var owner = new ContentControl();
                var runtime = new TaskCompletionSource();
                var initialised = 0;
                var failures = new List<string>();
                using var host = new WebSurfaceHost(
                    "held:once", owner,
                    initialise: () => { initialised++; return Task.CompletedTask; },
                    failed: failures.Add,
                    ensure: _ => runtime.Task);

                for (var i = 0; i < 4; i++)
                {
                    owner.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                }

                Assert.Equal(1, host.InitialisationsStarted);
                Assert.Equal(0, initialised);
                Assert.Equal(3, lines.Count(l => l.Contains("\"transition\":\"re-attached\"", StringComparison.Ordinal)));

                // The host's continuation runs inline on SetResult: Sta.Run installs no
                // SynchronizationContext and the source is not RunContinuationsAsynchronously, so the
                // count below is already moved when SetResult returns. A pumped context would need a
                // wait here instead.
                runtime.SetResult();
                Assert.Equal(1, initialised);
                Assert.Empty(failures);
            }
            finally
            {
                WorkbenchDiagnostics.Sink = previous;
            }
        });
    }

    /// <summary>
    /// <b>A failed start is reported once and not retried on the next attach</b> — the surface says
    /// so once, the log carries the exception with a stable code, and three more attaches are three
    /// <c>re-attached</c> lines, not three more error banners.
    /// </summary>
    [Fact]
    public void AFailedStartIsReportedOnceAndNotRetriedOnReattach()
    {
        Sta.Run(() =>
        {
            var lines = new List<string>();
            var previous = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = lines.Add;

            try
            {
                var owner = new ContentControl();
                var initialised = 0;
                var failures = new List<string>();
                using var host = new WebSurfaceHost(
                    "broken:once", owner,
                    initialise: () => { initialised++; return Task.CompletedTask; },
                    failed: failures.Add,
                    ensure: _ => throw new InvalidOperationException("no WebView2 runtime here"));

                for (var i = 0; i < 4; i++)
                {
                    owner.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                }

                Assert.Equal(1, host.InitialisationsStarted);
                Assert.Equal(0, initialised);
                Assert.Equal(["no WebView2 runtime here"], failures);

                var failed = Assert.Single(lines, l => l.Contains("\"transition\":\"init-failed\"", StringComparison.Ordinal));
                using var record = System.Text.Json.JsonDocument.Parse(failed);
                Assert.Equal("WEB.INIT_THREW", record.RootElement.GetProperty("errorCode").GetString());
                Assert.Equal(typeof(InvalidOperationException).FullName, record.RootElement.GetProperty("exceptionType").GetString());
                Assert.Equal(3, lines.Count(l => l.Contains("\"transition\":\"re-attached\"", StringComparison.Ordinal)));
            }
            finally
            {
                WorkbenchDiagnostics.Sink = previous;
            }
        });
    }

    [Fact]
    public void TheComposerInitialisesItsBrowserOnceAcrossReparents() =>
        Drive(() => new ComposerSurface("composer:once", "once — composer"), surface => surface.InitialisationsStarted, "composer:once");

    [Fact]
    public void TheCanvasInitialisesItsBrowserOnceAcrossReparents() =>
        Drive(() => new CanvasSurface("canvas:once", "once — canvas"), surface => surface.InitialisationsStarted, "canvas:once");

    /// <summary>
    /// The sweep-shaped half of the control: <b>no element outside the host hooks <c>Loaded</c></b>.
    /// </summary>
    /// <remarks>
    /// <para>Root <c>src/AiDe.App</c>; every <c>*.cs</c> and <c>*.xaml</c> outside <c>bin</c>/<c>obj</c>;
    /// tokens <c>Loaded +=</c> (any spacing), <c>LoadedEvent</c>, and XAML <c>Loaded="</c>. The
    /// allow-list is the population that is safe by construction, by path: a <see cref="Window"/> is
    /// never re-parented, so its <c>Loaded</c> fires once (<c>MainWindow</c>, <c>TextPromptDialog</c>),
    /// and <see cref="WebSurfaceHost"/> is the guard itself. Adding a path here is a claim that its
    /// handler is idempotent under re-attach; say why beside the entry.</para>
    /// <para>Observed red against the un-fixed tree: <c>CanvasSurface.cs</c> and
    /// <c>ComposerSurface.cs</c> both hooked it.</para>
    /// </remarks>
    [Fact]
    public void NoElementOutsideTheHostHooksLoadedForItsOwnInitialisation()
    {
        string[] allowed =
        [
            "src/AiDe.App/MainWindow.xaml.cs",            // a Window: attached once, Loaded once
            "src/AiDe.App/Workbench/TextPromptDialog.cs", // a Window, likewise
            "src/AiDe.App/Workbench/WebSurfaceHost.cs",   // the guard
            // The presenter (ADR-0031): each switch hooks the NEW body's first Loaded as the switch's
            // stop edge (US-C12) and the handler unsubscribes itself on that first fire — so it is
            // idempotent under re-attach by construction (never runs twice per subscription, never
            // initialises anything); PerspectiveShellTests.EveryRetainedSwitch_IsShownOnce asserts it.
            "src/AiDe.App/Workbench/PerspectiveShell.cs",
        ];
        var hook = new System.Text.RegularExpressions.Regex(
            @"\bLoaded\s*\+=|\bLoadedEvent\b|\bLoaded=""", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        var root = RepoRoot();
        var offenders = Directory.EnumerateFiles(Path.Combine(root, "src", "AiDe.App"), "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".xaml", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => hook.IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(relative => !allowed.Contains(relative, StringComparer.Ordinal))
            .ToList();

        Assert.True(offenders.Count == 0, "a Loaded hook outside the allow-list — one-time work on a per-attach event (DC-138): " + string.Join(", ", offenders));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }

    private static void Drive<T>(Func<T> create, Func<T, int> initialisations, string surfaceId)
        where T : UIElement
    {
        var lines = new List<string>();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = line => { lock (lines) { lines.Add(line); } };

        try
        {
            ContentControl host = null!;

            Sta.Pump(
                create: create,
                content: surface => host = new ContentControl { Content = surface },
                body: async (_, surface) =>
                {
                    await Settle(surface);
                    Assert.Equal(1, initialisations(surface));

                    for (var i = 0; i < Reparents; i++)
                    {
                        // Away, then back: the Unloaded/Loaded pair every Adapter.Render() raises.
                        host.Content = null;
                        await Settle(surface);
                        host.Content = surface;
                        await Settle(surface);
                    }

                    Assert.Equal(1, initialisations(surface));
                },
                timeoutSeconds: 60);

            // THE LOG SAYS THE SAME, and is the non-vacuity: one `initialising`, then exactly N
            // `re-attached` — so the surface really was attached N more times and declined each.
            var mine = lines.Where(l => l.Contains($"\"surface\":\"{surfaceId}\"", StringComparison.Ordinal)).ToList();
            Assert.Single(mine, l => l.Contains("\"transition\":\"initialising\"", StringComparison.Ordinal));
            Assert.Equal(Reparents, mine.Count(l => l.Contains("\"transition\":\"re-attached\"", StringComparison.Ordinal)));

            // THE PAYLOAD (T7): the keys a reader greps for exist, and the counts the host does not
            // measure are null — not invented.
            using var record = System.Text.Json.JsonDocument.Parse(mine.First(l => l.Contains("\"transition\":\"re-attached\"", StringComparison.Ordinal)));
            Assert.Equal("web-surface.handshake", record.RootElement.GetProperty("evt").GetString());
            Assert.Equal(surfaceId, record.RootElement.GetProperty("surface").GetString());
            foreach (var key in new[] { "navigations", "inputs", "drops" })
            {
                Assert.Equal(System.Text.Json.JsonValueKind.Null, record.RootElement.GetProperty(key).ValueKind);
            }
        }
        finally
        {
            WorkbenchDiagnostics.Sink = previous;
        }
    }

    /// <summary>
    /// Lets the queued <c>Loaded</c>/<c>Unloaded</c> broadcast run before a count is read. The
    /// broadcast is queued at <c>Loaded</c> priority, above <c>Background</c>, so one operation at
    /// <c>Background</c> runs after it — and <c>Sta.Pump</c> pumps at <c>Background</c>, so nothing
    /// lower would ever be reached.
    /// </summary>
    private static Task Settle(UIElement element) =>
        element.Dispatcher.InvokeAsync(static () => { }, DispatcherPriority.Background).Task;
}
