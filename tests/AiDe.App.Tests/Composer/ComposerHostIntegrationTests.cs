namespace AiDe.App.Tests.Composer;

/// <summary>
/// Security <b>C10</b> and <b>C12</b>'s runtime halves, in the <b>real</b> WebView2 control: the page
/// can reach exactly one document, the settings floor really applied, the network is unreachable,
/// host objects are undefined — <b>and the editor still renders under the shipped policy</b>.
/// </summary>
/// <remarks>
/// <para><b>Why it is out of process.</b> The same reason the canvas-focus and web-host cases are:
/// the claim needs a real window with a real <c>WebView2</c> in it, which a <c>dotnet test</c> host
/// does not reliably provide (<b>DC-014</b>).</para>
///
/// <para><b>Why it is not a headless browser.</b> The question is about the control this shell hosts,
/// under the policy this shell ships. Proving it somewhere else would be a control that cannot fail
/// for the reason it exists (<b>DC-016</b>).</para>
///
/// <para><b>Its absence FAILS rather than skips</b> (<b>DC-012</b>): a missing probe or a missing
/// WebView2 runtime is a broken environment, and this says so loudly.</para>
/// </remarks>
public sealed class ComposerHostIntegrationTests
{
    [Fact]
    public void TheComposerRendersUnderItsPolicyAndCanReachExactlyOneDocument()
    {
        var probe = ProbePath();
        Assert.True(File.Exists(probe), $"the composer probe was not built at {probe}");

        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(probe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit((int)TimeSpan.FromMinutes(4).TotalMilliseconds), "the composer probe hung");

        Assert.True(
            process.ExitCode == 0,
            $"the composer probe failed with exit {process.ExitCode}. {stdout} {stderr}");

        // NON-VACUITY. Each of these lines is a measurement the probe could only print after the
        // thing it describes actually happened.
        Assert.Contains("the composer page rendered under its policy", stdout, StringComparison.Ordinal);
        Assert.Contains("hostObject resolution: rejected", stdout, StringComparison.Ordinal);
        Assert.Contains("fetch: rejected", stdout, StringComparison.Ordinal);
        Assert.Contains("editors: 1", stdout, StringComparison.Ordinal);
        Assert.Contains("fenced: true", stdout, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Ruling 47 (d):</b> exactly one <c>host.init</c> per mount after <c>Configure</c>, and field
    /// values survive it — driven against the shipped <c>ComposerSurface</c> and the shipped page.
    /// </summary>
    /// <remarks>
    /// <para><b>What was red before the fix, and how.</b> With the page restored to posting
    /// <c>editor.ready</c> only from inside its own <c>host.init</c> branch, this probe exits 12 —
    /// <c>ThePageNeverMounted</c> — because nothing pushes a first init and the page is waiting for
    /// one. That is the deadlock, observed, not argued.</para>
    ///
    /// <para><b>Both orders are run</b>: the shell configuring a document whose browser is still
    /// starting, and configuring one whose page mounted first. Each must yield exactly one init.</para>
    /// </remarks>
    [Fact]
    public void TheHandshakePushesExactlyOneHostInitPerMountAndFieldValuesSurviveIt()
    {
        var probe = ProbePath();
        Assert.True(File.Exists(probe), $"the composer probe was not built at {probe}");

        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(probe)
        {
            Arguments = "--handshake",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit((int)TimeSpan.FromMinutes(6).TotalMilliseconds), "the handshake probe hung");

        Assert.True(
            process.ExitCode == 0,
            $"the composer handshake probe failed with exit {process.ExitCode}. {stdout} {stderr}");

        // NON-VACUITY. Each line below could only be printed after the thing it describes happened,
        // and the count appears once per order rather than once in total.
        Assert.Equal(2, stdout.Split("host.init count=1", StringSplitOptions.None).Length - 1);

        Assert.Contains("configure-before-show=True", stdout, StringComparison.Ordinal);
        Assert.Contains("configure-before-show=False", stdout, StringComparison.Ordinal);
        Assert.Contains("rendered goal=Seeded before the page mounted", stdout, StringComparison.Ordinal);

        // The accessor that silently ate every page message, re-measured on every run. If this ever
        // stops reading `null`, the defensive read in the surface has a different justification and
        // its comment is a memoir.
        Assert.Contains("AdditionalObjects on a plain postMessage: null", stdout, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>INV-0007, finding 1 (the operator's symptom).</b> After the product's own New Session
    /// choreography — the shell opens the document, <c>Configure</c> runs as <c>MainWindow.BindComposer</c>
    /// calls it, the pane takes the tree (Ruling 47) — in the arrangement the operator's workbench log
    /// recorded, the composer's entry areas <b>keep their room</b>: the editor host is never smaller
    /// than the read-only compiled view, the compiled view keeps to 35% of the composer, the page
    /// mounts six fields, and a keystroke reaches the draft.
    /// </summary>
    /// <remarks>
    /// <para><b>Observed red on main, 2026-09-11, before any repair:</b> exit 24 — <i>"the editor
    /// host is starved: it has 110px of the composer's 689px while the read-only compiled view has
    /// 465px (67%, ceiling 35%)"</i>; on the F5 tree's choreography (no maximize) the editor host
    /// measured <b>0px</b> of 485. The compiled <c>TextBox</c> has no height ceiling and sits in a
    /// <c>StackPanel</c> docked Bottom, so it is measured unconstrained and its content height is
    /// subtracted from the editor before the editor is measured. <b>Necessity, measured in the same
    /// probe (<c>--cap-compiled</c>):</b> capping the compiled view from outside the product at 35%
    /// gave the editor host 202px in the same 485px composer and the run exited 0.</para>
    ///
    /// <para><b>Why the shell and not a bare window.</b> The handshake probe above hosts the surface
    /// in a bare <c>Window</c> and is green; the starvation needs the height a docked document
    /// actually gets, which only the real docking host under the real arrangement provides (DC-135).
    /// The window is 800px tall — a laptop; the rule is height-independent, and the operator's
    /// screenshot at roughly 1000px showed the same inversion.</para>
    /// </remarks>
    [Fact]
    public void TheComposersEntryAreasKeepTheirRoomAfterTheNewSessionChoreography()
    {
        var (exitCode, stdout, stderr) = RunProbe("--shell --maximize --height 800", TimeSpan.FromMinutes(6));

        Assert.True(
            exitCode == 0,
            $"the composer shell probe failed with exit {exitCode}. {stdout} {stderr}");

        // NON-VACUITY. The page mounted its fields, the writer kept at least the reader's room, and a
        // keystroke reached the draft — each line could only be printed after it happened.
        Assert.Contains("fields=6 editors=3", stdout, StringComparison.Ordinal);
        Assert.Contains("writer >= reader: True", stdout, StringComparison.Ordinal);
        Assert.Contains("reached draft=True", stdout, StringComparison.Ordinal);

        // INV-0007 PHASE 3: the surface measured itself. The bounds, every handshake transition and
        // the first accepted keystroke are on the normal path — no flag, no re-run — and each line
        // below could only be printed by the product, since the probe writes none of them.
        var composerLines = ComposerDiagnostics(stdout);
        Assert.Contains(composerLines, l => l.Contains("\"evt\":\"composer.layout\"", StringComparison.Ordinal));
        foreach (var transition in new[] { "initialising", "navigation-started", "configured", "page-ready", "init-pushed", "input-received" })
        {
            Assert.Contains(composerLines, l => l.Contains($"\"transition\":\"{transition}\"", StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// <b>INV-0007, finding 2.</b> A composer page that has mounted survives one later
    /// <c>Adapter.Render()</c> — which every layout command, pane open and restore performs — with
    /// its fields still on screen.
    /// </summary>
    /// <remarks>
    /// <para><b>Observed red on main, 2026-09-11, before any repair:</b> exit 25 — after one render,
    /// <i>"wpf loaded=2 unloaded=1, navigations started=2 (+1), editor.ready posted=2 (+1), router
    /// drops=2, host.init count=0 fields=0, editor text=''"</i> while the host's draft still held the
    /// text. The render re-parents the WebView2, WPF raises <c>Loaded</c> again,
    /// <c>ComposerSurface.InitialiseAsync</c> runs again and navigates the page again; the new page
    /// posts <c>editor.ready</c>, the router drops it as a duplicate of the first mount's, and no
    /// <c>host.init</c> follows — so the operator's fields vanish and nothing says why.</para>
    ///
    /// <para><b>Why main's own choreography does not trip it:</b> its two renders run in one dispatcher
    /// operation and WPF coalesces the pending <c>Loaded</c> (measured: <c>wpf loaded=1</c>). The
    /// next render after the mount is the one that does.</para>
    /// </remarks>
    [Fact]
    public void TheComposerPageSurvivesALaterRender()
    {
        var (exitCode, stdout, stderr) = RunProbe("--shell --maximize --render-after-mount --height 800", TimeSpan.FromMinutes(6));

        Assert.True(
            exitCode == 0,
            $"the composer shell probe failed with exit {exitCode}. {stdout} {stderr}");

        // NON-VACUITY: the fields were there after the render, and the render actually happened
        // (the line is printed only on the --render-after-mount path).
        Assert.Contains("after one later render:", stdout, StringComparison.Ordinal);
        Assert.Contains("fields=6, editor text=", stdout, StringComparison.Ordinal);

        // INV-0007 PHASE 2/3: the re-parent was heard and declined — one line says the docking host
        // attached the surface again, and no line says a ready was dropped or a page reloaded.
        var composerLines = ComposerDiagnostics(stdout);
        Assert.Contains(composerLines, l => l.Contains("\"transition\":\"re-attached\"", StringComparison.Ordinal));
        Assert.DoesNotContain(composerLines, l => l.Contains("\"transition\":\"message-dropped\"", StringComparison.Ordinal));
        Assert.Single(composerLines, l => l.Contains("\"transition\":\"navigation-started\"", StringComparison.Ordinal));
    }

    /// <summary>
    /// <b>DC-137, the allowed branch.</b> A genuine reload of the composer page — crash recovery's
    /// shape — is a new document whose <c>editor.ready</c> is a mount: one more navigation, one more
    /// <c>host.init</c> carrying the draft the host still holds, no ready dropped, six fields back.
    /// </summary>
    /// <remarks>
    /// Seen red by mutation through this probe: with the surface's <c>BeginNavigation</c> call
    /// removed, exit 26 — <i>init-pushed 1->1, router drops +1, host.init count=0 fields=0</i>, and
    /// the log's <c>message-dropped … editor.ready: this instance already reported ready</c>.
    /// </remarks>
    [Fact]
    public void AGenuineReloadRemountsThePageWithTheDraft()
    {
        var (exitCode, stdout, stderr) = RunProbe("--shell --maximize --reload-after-mount --height 800", TimeSpan.FromMinutes(6));

        Assert.True(exitCode == 0, $"the composer shell probe failed with exit {exitCode}. {stdout} {stderr}");

        Assert.Contains("after a reload: remounted=True, composer navigation-started 1->2, init-pushed 1->2, editor.ready posted +1, router drops +0, host.init count=1 fields=6", stdout, StringComparison.Ordinal);

        var composerLines = ComposerDiagnostics(stdout);
        Assert.Equal(2, composerLines.Count(l => l.Contains("\"transition\":\"navigation-started\"", StringComparison.Ordinal)));
        Assert.Equal(2, composerLines.Count(l => l.Contains("\"transition\":\"page-ready\"", StringComparison.Ordinal)));
        Assert.DoesNotContain(composerLines, l => l.Contains("\"transition\":\"message-dropped\"", StringComparison.Ordinal));
    }

    /// <summary>
    /// <b>DC-137, the cancelled branch.</b> A navigation the policy cancels replaces no document and
    /// resets nothing: the composer counts no navigation, readiness stands, and the next keystroke
    /// reaches the draft.
    /// </summary>
    /// <remarks>
    /// Seen red by mutation through this probe: with the cancel early-return removed from
    /// <c>OnNavigationStarting</c>, exit 27 — <i>navigation-started 1->2, page ready=False, router
    /// drops=1, reached draft=False</i>: the operator's typing silently lost.
    /// </remarks>
    [Fact]
    public void ACancelledNavigationResetsNothing()
    {
        var (exitCode, stdout, stderr) = RunProbe("--shell --maximize --escape-after-mount --height 800", TimeSpan.FromMinutes(6));

        Assert.True(exitCode == 0, $"the composer shell probe failed with exit {exitCode}. {stdout} {stderr}");

        // The browser really raised a second NavigationStarting (the page tried), and the composer
        // really did not count it.
        Assert.Contains("after a cancelled navigation: composer navigation-started 1->1, raw NavigationStarting=2, page ready=True, router drops=0, dispatched=True, reached draft=True", stdout, StringComparison.Ordinal);
        Assert.Single(ComposerDiagnostics(stdout), l => l.Contains("\"transition\":\"navigation-started\"", StringComparison.Ordinal));
    }

    /// <summary>The workbench diagnostics the probe echoed, for the composer surface only.</summary>
    private static List<string> ComposerDiagnostics(string stdout) =>
        stdout.Split('\n')
            .Where(l => l.StartsWith("diag: {\"ts\":", StringComparison.Ordinal) && l.Contains("\"surface\":\"composer:", StringComparison.Ordinal))
            .ToList();

    private static (int ExitCode, string Stdout, string Stderr) RunProbe(string arguments, TimeSpan timeout)
    {
        var probe = ProbePath();
        Assert.True(File.Exists(probe), $"the composer probe was not built at {probe}");

        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(probe)
        {
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit((int)timeout.TotalMilliseconds), $"the composer probe hung ({arguments})");

        return (process.ExitCode, stdout, stderr);
    }

    private static string ProbePath()
    {
        var configuration =
#if DEBUG
            "Debug";
#else
            "Release";
#endif
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "AiDe.App.ComposerProbe", "bin"));

        return Path.Combine(root, configuration, "net10.0-windows", "AiDe.App.ComposerProbe.exe");
    }
}
