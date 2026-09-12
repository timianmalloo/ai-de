using AiDe.App.Tests.Composer;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// <b>INV-0009.</b> A session document the shell announces as <i>opened</i> is <b>shown</b>: its
/// composer enters a rendered visual tree, measures itself, and mounts its six fields — in the
/// arrangement the operator's workbench log recorded at 22:33:53Z, through the product's own mode
/// controller, and on both paths that open one (<c>File → New Session</c> and a reopen).
/// </summary>
/// <remarks>
/// <para><b>Why the operator's arrangement and not a fresh shell.</b> Three harnesses passed on
/// 2026-09-11 against a state the product was not in (DC-135). The probe these oracles launch replays
/// the operator's launch from the log: the workspace-open restore's zone payload verbatim, the
/// session document opened before it and dropped by it, and — the step the log names by its
/// <c>explorer-graph</c> line — Explorer mode entered before New Session. The run that omits a step is
/// the control for the run that includes it.</para>
///
/// <para><b>Observed on <c>main</c> <c>1aadde84</c>, 2026-09-11, before any repair.</b>
/// <see cref="ANewSessionRendersInTheOperatorsRestoredArrangement"/> — exit 0, <i>composer wpf
/// loaded=1 … init-pushed=1, page fields=6</i>: the restored arrangement and Ruling 47's maximize do
/// not stop the document rendering. <see cref="ANewSessionCreatedWhileExplorerIsTheBodyIsShown"/> —
/// <b>exit 30</b>, <i>"the new session's composer never entered a rendered visual tree: WPF raised no
/// Loaded on it and its browser host never initialised"</i>, with the announcement reading <i>Session
/// opened. Composer bound … Maximized the center</i> and the workbench root <i>loaded=False
/// visible=False parent=(none)</i>. <see cref="ANewSessionCreatedInsideExplorerLeavesItBecauseADocumentOpened"/>
/// (then <c>LeavingExplorerShowsTheSessionCreatedInsideIt</c>) — exit 0: the same composer object,
/// untouched, loaded and mounted six fields the moment the workbench returned to the body; after Phase
/// 1 that state is unreachable and the oracle asserts the trigger instead.
/// <see cref="AReopenedSessionIsShownAndItsComposerIsBound"/> — <b>exit 32</b>:
/// the pane still rendered <i>No session is open</i> after the reopen, and the composer was never
/// configured.</para>
/// </remarks>
public sealed class ASessionDocumentIsShownWhereTheOperatorIsTests
{
    private static readonly TimeSpan Budget = TimeSpan.FromMinutes(4);

    /// <summary>
    /// The control: in the operator's restored arrangement, with the workbench as the body, New
    /// Session's choreography renders the document. This is the run that rules out the restored
    /// layout, the second restored session document, the maximize and the factory slot as causes.
    /// </summary>
    [Fact]
    public void ANewSessionRendersInTheOperatorsRestoredArrangement()
    {
        var (exitCode, stdout, stderr) = ComposerHostIntegrationTests.RunProbe(
            "--session-render --prior-document --height 720", Budget);

        Assert.True(exitCode == 0, $"the session-render probe failed with exit {exitCode}. {stdout} {stderr}");

        // NON-VACUITY: the replay reached the operator's arrangement (the probe exits 33 otherwise),
        // the prior document rendered unconfigured as the log shows, and the new one loaded.
        Assert.Contains("restore (22:33:53Z replay): applied-saved=True", stdout, StringComparison.Ordinal);
        Assert.Contains("prior document (22:33:28Z replay): announced=", stdout, StringComparison.Ordinal);
        Assert.Contains(" configured=0 init-pushed=0 layout-lines=1 status='repositoryRoot:", stdout, StringComparison.Ordinal);
        AssertShown(stdout, "after New Session:");
    }

    /// <summary>
    /// <b>The operator's symptom.</b> Explorer is the body; File → New Session runs; the session must
    /// be shown. On the unfixed product the document is added to a docking host that is not in any
    /// visual tree, its composer is configured and never loaded, and the shell announces success.
    /// </summary>
    [Fact]
    public void ANewSessionCreatedWhileExplorerIsTheBodyIsShown()
    {
        var (exitCode, stdout, stderr) = ComposerHostIntegrationTests.RunProbe(
            "--session-render --prior-document --explorer --sibling --height 720", Budget);

        Assert.True(exitCode == 0, $"the session-render probe failed with exit {exitCode}. {stdout} {stderr}");

        Assert.Contains("explorer (22:34:00Z replay): mode=Explorer explorer-graph initialising=1", stdout, StringComparison.Ordinal);
        AssertShown(stdout, "after New Session:");

        // THE CLASS: a sibling dock document opened by a catalog command in the same state.
        var sibling = Line(stdout, "sibling (code viewer, same state):");
        Assert.Contains(" in-layout=True ", sibling, StringComparison.Ordinal);
        Assert.Contains(" isLoaded=True isVisible=True", sibling, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The fix, named by its trigger.</b> New Session while Explorer is the body leaves Explorer
    /// <i>because a document opened</i> — the <c>shell.mode</c> line the product wrote names
    /// <c>document-opening</c>, not the rail's toggle — and a later return to the workbench is a
    /// no-op: the composer loaded once, so nothing was re-parented (DC-138).
    /// </summary>
    /// <remarks>
    /// Before INV-0009 Phase 1 this was the diagnosis's necessity half: the document created inside
    /// Explorer stayed unloaded (<i>mode=Explorer … composer wpf loaded=0 … configured=1
    /// init-pushed=0</i>) until the workbench was put back, at which point the untouched composer
    /// loaded and mounted six fields. That state is no longer reachable by design, so the oracle now
    /// asserts the mechanism that replaced it.
    /// </remarks>
    [Fact]
    public void ANewSessionCreatedInsideExplorerLeavesItBecauseADocumentOpened()
    {
        var (exitCode, stdout, stderr) = ComposerHostIntegrationTests.RunProbe(
            "--session-render --prior-document --explorer --return-to-workbench --height 720", Budget);

        Assert.True(exitCode == 0, $"the session-render probe failed with exit {exitCode}. {stdout} {stderr}");

        Assert.Contains("explorer (22:34:00Z replay): mode=Explorer explorer-graph initialising=1", stdout, StringComparison.Ordinal);

        var after = Line(stdout, "after New Session:");
        Assert.Contains("mode=Workbench last-mode-trigger=document-opening ", after, StringComparison.Ordinal);
        AssertShown(stdout, "after New Session:");

        // The explicit return is a no-op: the same trigger on record, and not one more WPF Loaded on
        // the composer — nothing was re-parented by a body that was already the workbench.
        var returned = Line(stdout, "after returning to the workbench:");
        Assert.Contains("mode=Workbench last-mode-trigger=document-opening ", returned, StringComparison.Ordinal);
        Assert.Equal(LoadedCount(after), LoadedCount(returned));
        AssertShown(stdout, "after returning to the workbench:");
    }

    /// <summary>
    /// A reopened session (<c>File → Recent sessions</c>, <c>MainWindow.ReopenSessionAsync</c>) is
    /// shown with its composer bound. On the unfixed product a session whose surface the restore
    /// already placed keeps its "No session is open" island — the live document never reaches the
    /// view — and nothing on the reopen path configures the composer.
    /// </summary>
    [Fact]
    public void AReopenedSessionIsShownAndItsComposerIsBound()
    {
        var (exitCode, stdout, stderr) = ComposerHostIntegrationTests.RunProbe(
            "--session-render --reopen --height 720", Budget);

        Assert.True(exitCode == 0, $"the session-render probe failed with exit {exitCode}. {stdout} {stderr}");

        Assert.Contains("reopen: pane content=", stdout, StringComparison.Ordinal);
        Assert.Contains("live-document-in-view=True", stdout, StringComparison.Ordinal);
        var composer = Line(stdout, "reopen: composer ");
        Assert.Contains(" configured=1 init-pushed=1 ", composer, StringComparison.Ordinal);
        Assert.Contains(" page fields=6 ", composer, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>INV-0009 Phase 2b.</b> At workspace-open, a session document the saved arrangement restored
    /// is revived — live, its composer bound — where its <c>session.json</c> still loads; one whose
    /// session is gone keeps the "No session is open" island; and the active tab the restore chose
    /// stays active.
    /// </summary>
    /// <remarks>
    /// <b>Observed red before the binding half landed</b> (the revive alone, 2026-09-11): exit 34,
    /// <i>active document live=True renders='Compiled view' … configured=0 init-pushed=0 page
    /// fields=0</i> — the blank editor of finding C, produced by a revive with no bind. Before the
    /// revive existed every run printed <i>restored active document: live-document=False renders='No
    /// session is open…'</i>.
    /// </remarks>
    [Fact]
    public void ARestoredSessionDocumentIsRevivedAndBoundAtWorkspaceOpen()
    {
        var (exitCode, stdout, stderr) = ComposerHostIntegrationTests.RunProbe(
            "--session-render --bind-on-restore --height 720", Budget);

        Assert.True(exitCode == 0, $"the session-render probe failed with exit {exitCode}. {stdout} {stderr}");

        Assert.Contains("restore (22:33:53Z replay): applied-saved=True", stdout, StringComparison.Ordinal);
        // The sentence's middle dots do not survive the probe's console encoding; the engine does.
        Assert.Contains("bind-on-restore: revived=[20260911T175821Z-1edfa710] bound='Composer bound to claude-code ", stdout, StringComparison.Ordinal);

        var documents = Line(stdout, "bind-on-restore: active document ");
        Assert.Contains("active document live=True renders='Compiled view' active in view=session-document:20260911T175821Z-1edfa710;", documents, StringComparison.Ordinal);
        Assert.Contains("gone document live=False renders='No session is open.", documents, StringComparison.Ordinal);

        var composer = Line(stdout, "bind-on-restore: composer ");
        Assert.Contains(" configured=1 init-pushed=1 ", composer, StringComparison.Ordinal);
        Assert.Contains(" page fields=6 ", composer, StringComparison.Ordinal);

        // The binding is in the log, with the checkout it bound to (INV-0009 F5).
        Assert.Contains("\"evt\":\"session-document.bound\",\"session\":\"20260911T175821Z-1edfa710\"", stdout, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>INV-0009 Phase 3 (DC-149), the Owner's ruling.</b> File → New Session with no workspace
    /// open: the chooser interposes, the chosen workspace is <b>opened</b> before the sheet, the
    /// session is created in it and its composer is bound to the chosen root — the product's own
    /// <c>NewSessionFlow</c>, with the window's open path stood in by what the window reports. And a
    /// cancelled chooser opens nothing and creates nothing.
    /// </summary>
    /// <remarks>
    /// <b>The red this replaces</b> is the state the <c>--prior-document</c> step replays and
    /// <see cref="ANewSessionRendersInTheOperatorsRestoredArrangement"/> asserts: the chooser-bound
    /// session's composer refused with <i>repositoryRoot: this window has no open workspace</i> —
    /// the flow handed the chosen root to the session store and never to the window. The flow's
    /// own unit oracle (<c>TheChooserOpensTheChosenWorkspace_ThenTheSheetBindsToWhatTheWindowReports</c>)
    /// would not compile against the flow that had no open step.
    /// </remarks>
    [Fact]
    public void ASessionCreatedThroughTheChooserIsBoundToTheChosenWorkspace()
    {
        var (exitCode, stdout, stderr) = ComposerHostIntegrationTests.RunProbe(
            "--session-render --chooser --height 720", Budget);

        Assert.True(exitCode == 0, $"the session-render probe failed with exit {exitCode}. {stdout} {stderr}");

        // Open before the sheet; the sheet bound to the workspace the window reports; then the callback.
        Assert.Contains("chooser: order=[choose,open,sheet:window-root,opened] created=True", stdout, StringComparison.Ordinal);

        var composer = Line(stdout, "chooser: composer ");
        Assert.Contains(" configured=1 init-pushed=1 page fields=6 status='' bound-to-chosen-root=True", composer, StringComparison.Ordinal);

        Assert.Contains("chooser cancelled: order=[choose] created=False surfaces before=9 after=9 sessions before=1 after=1", stdout, StringComparison.Ordinal);
    }

    /// <summary>The <c>composer wpf loaded=N</c> count on a measurement line.</summary>
    private static int LoadedCount(string line)
    {
        const string token = "composer wpf loaded=";
        var at = line.IndexOf(token, StringComparison.Ordinal);
        Assert.True(at >= 0, $"the line carries no '{token}': {line}");
        var digits = new string(line[(at + token.Length)..].TakeWhile(char.IsDigit).ToArray());
        return int.Parse(digits, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>The measurement line that begins with <paramref name="prefix"/>, or a failure naming it.</summary>
    private static string Line(string stdout, string prefix)
    {
        var line = stdout.Split('\n').FirstOrDefault(l => l.StartsWith(prefix, StringComparison.Ordinal));
        Assert.True(line is not null, $"the probe printed no '{prefix}' line. {stdout}");
        return line!;
    }

    /// <summary>The document is shown: WPF loaded its composer, the browser initialised, the page mounted six fields.</summary>
    private static void AssertShown(string stdout, string prefix)
    {
        var line = Line(stdout, prefix);
        Assert.Contains("workbench root loaded=True visible=True", line, StringComparison.Ordinal);
        Assert.DoesNotContain("composer wpf loaded=0", line, StringComparison.Ordinal);
        Assert.Contains(" isLoaded=True isVisible=True ", line, StringComparison.Ordinal);
        Assert.Contains(" initialising=1 ", line, StringComparison.Ordinal);
        Assert.Contains(" init-pushed=1,", line, StringComparison.Ordinal);
        Assert.Contains(" layout-lines=1,", line, StringComparison.Ordinal);
        Assert.Contains(" page fields=6,", line, StringComparison.Ordinal);
    }
}
