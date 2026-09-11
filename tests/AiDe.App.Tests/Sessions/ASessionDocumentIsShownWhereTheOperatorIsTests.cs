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
/// visible=False parent=(none)</i>. <see cref="LeavingExplorerShowsTheSessionCreatedInsideIt"/> —
/// exit 0: the same composer object, untouched, loaded and mounted six fields the moment the workbench
/// returned to the body. <see cref="AReopenedSessionIsShownAndItsComposerIsBound"/> — <b>exit 32</b>:
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
    /// The necessity half, kept as an oracle: the same document, created while Explorer was the body,
    /// renders the moment the workbench is the body again. Nothing about the document changed — the
    /// host it sits in was simply put back into the tree.
    /// </summary>
    [Fact]
    public void LeavingExplorerShowsTheSessionCreatedInsideIt()
    {
        var (exitCode, stdout, stderr) = ComposerHostIntegrationTests.RunProbe(
            "--session-render --prior-document --explorer --return-to-workbench --height 720", Budget);

        Assert.True(exitCode == 0, $"the session-render probe failed with exit {exitCode}. {stdout} {stderr}");

        // Inside Explorer: configured, never loaded. After the return: loaded, mounted, six fields.
        var inside = Line(stdout, "after New Session:");
        Assert.Contains("mode=Explorer workbench root loaded=False visible=False, composer wpf loaded=0", inside, StringComparison.Ordinal);
        Assert.Contains(" configured=1 init-pushed=0,", inside, StringComparison.Ordinal);
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
