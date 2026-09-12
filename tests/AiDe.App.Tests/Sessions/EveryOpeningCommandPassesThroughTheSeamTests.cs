using System.Text.Json;
using System.Windows.Controls;
using AiDe.App.Workbench;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// <b>INV-0009 Phase 1 — the structural half of DC-148's control.</b> The behavioural half is
/// <see cref="ASessionDocumentIsShownWhereTheOperatorIsTests.ANewSessionCreatedWhileExplorerIsTheBodyIsShown"/>,
/// which proves one document and one sibling render. This proves the property it relies on: every
/// command in the shell that adds a dock document raises the one seam the window handles, the window
/// handles it by making the workbench the body, and the mode change is in the log with its trigger.
/// </summary>
/// <remarks>
/// <b>The scan states its own shape</b> (DC-118 half (b)): <b>root</b>
/// <c>src/AiDe.App/Workbench/WorkbenchShell.cs</c> only — the INV's sweep found every opening
/// command there and nowhere else; <b>recursion</b> none; <b>token set</b>
/// <c>new LayoutOperation.AddSurface(</c> (an opening) and <c>OpeningDocument();</c> (the seam);
/// <b>allowlist</b> empty — every <c>AddSurface</c> in the shell opens a document, so an add with no
/// seam call between it and the head of its command is a sibling of the operator's defect, not an
/// exception to the rule. The window's and the replay's wiring are asserted by the same token so
/// the replay cannot pass on a line the product does not carry (DC-135).
/// </remarks>
public sealed class EveryOpeningCommandPassesThroughTheSeamTests
{
    private const string Opening = "new LayoutOperation.AddSurface(";
    private const string Seam = "OpeningDocument();";
    private const string Wiring = "DocumentOpening += () => ";
    private const string Handling = ".Set(ShellViewMode.Workbench, \"document-opening\");";

    /// <summary>The heads a command body starts at: a controller delegate assignment, or the shared open method.</summary>
    private static readonly string[] Heads = ["Requested = ", "private string OpenReferenceDocument("];

    [Fact]
    public void EveryAddSurfaceInTheShell_IsPrecededByTheSeamWithinItsCommand()
    {
        var shell = SourceFile("src", "AiDe.App", "Workbench", "WorkbenchShell.cs");
        var openings = Occurrences(shell, Opening);
        Assert.NotEmpty(openings);

        foreach (var at in openings)
        {
            var head = Heads.Select(h => shell.LastIndexOf(h, at, StringComparison.Ordinal)).Max();
            Assert.True(head >= 0, $"no command head precedes the AddSurface at offset {at}");

            var body = shell[head..at];
            Assert.True(
                body.Contains(Seam, StringComparison.Ordinal),
                $"the opening command at offset {head} adds a surface at offset {at} without raising DocumentOpening first:\n{body}");
        }
    }

    [Fact]
    public void TheWindowAndTheReplay_WireTheSeamToTheWorkbenchBody()
    {
        var window = SourceFile("src", "AiDe.App", "MainWindow.xaml.cs");
        var replay = SourceFile("tests", "AiDe.App.ComposerProbe", "Program.SessionRender.cs");

        foreach (var (name, text) in new[] { ("MainWindow", window), ("the session-render replay", replay) })
        {
            var wired = text.IndexOf(Wiring, StringComparison.Ordinal);
            Assert.True(wired >= 0, $"{name} does not subscribe to Shell.DocumentOpening");

            var line = text[wired..text.IndexOf('\n', wired)];
            Assert.True(
                line.Contains(Handling, StringComparison.Ordinal),
                $"{name} handles DocumentOpening with something other than Set(Workbench, \"document-opening\"): {line}");
        }
    }

    /// <summary>
    /// <b>The window's half of Phases 2, 2b and 3, which no headless test can execute (the window
    /// shows modal sheets and reaches a daemon).</b> The replay drives the shell's revive, the
    /// binder and the flow with its own callbacks; this asserts the window calls the same things in
    /// the same order, so a window that stopped calling them would go red here rather than in an
    /// operator's log (DC-135). Root <c>MainWindow.xaml.cs</c>; tokens as spelled below; each looked
    /// up inside the named member only.
    /// </summary>
    [Fact]
    public void TheWindow_RevivesThenRendersThenBinds_AndReopensBound_AndOpensTheChosenWorkspace()
    {
        var window = SourceFile("src", "AiDe.App", "MainWindow.xaml.cs");

        // 2b: AttachWorkspace — the one site — revives, renders, then binds on record.
        var attach = Member(window, "private void AttachWorkspace(");
        Assert.Single(Occurrences(window, "Shell.AttachWorkspace("));
        Assert.Contains("Shell.AttachWorkspace(", attach, StringComparison.Ordinal);
        AssertInOrder(attach, "Shell.ReviveRestoredSessionDocuments(", "Shell.Adapter.Render();", "BindOnRecord(config,");

        // 2: the reopen binds on record after the open, and does not maximize (Ruling 47's guard covers that).
        var reopen = Member(window, "private async Task ReopenSessionAsync(");
        AssertInOrder(reopen, "Shell.OpenSessionDocument(config)", "BindOnRecord(config,");

        // 3: the flow's openWorkspace is the window's ordinary open path, and Queries is the fact.
        var newSession = Member(window, "private async Task<string> NewSessionAsync()");
        Assert.Contains("openWorkspace: OpenWorkspaceOrSayWhyAsync,", newSession, StringComparison.Ordinal);
        var open = Member(window, "private async Task<string?> OpenWorkspaceOrSayWhyAsync(");
        AssertInOrder(open, "await OpenWorkspaceAtAsync(folder)", "?.Queries is null ? said : null");

        // And BindOnRecord is the binder, with the sheet's own routable derivation and no task class.
        var onRecord = Member(window, "private string BindOnRecord(");
        Assert.Contains("SessionComposerBinder.Refuse(Shell, config, \"providers\", reason)", onRecord, StringComparison.Ordinal);
        Assert.Contains("BindComposer(config, RoutableBackendsOf(config), taskClass: null)", onRecord, StringComparison.Ordinal);
    }

    /// <summary>The member's text from its signature to the next member at method indentation.</summary>
    private static string Member(string source, string signature)
    {
        var at = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"MainWindow.xaml.cs has no {signature} — this test is reading a document whose shape it does not understand (DC-016).");
        var next = source.IndexOf("\n    private ", at + signature.Length, StringComparison.Ordinal);
        return next < 0 ? source[at..] : source[at..next];
    }

    private static void AssertInOrder(string text, params string[] tokens)
    {
        var from = 0;
        foreach (var token in tokens)
        {
            var at = text.IndexOf(token, from, StringComparison.Ordinal);
            Assert.True(at >= 0, $"'{token}' does not follow the previous token in:\n{text}");
            from = at + token.Length;
        }
    }

    /// <summary>The mode is in the log, not inferred from it: one <c>shell.mode</c> line per change, with its trigger.</summary>
    [Fact]
    public void Set_ToADifferentMode_WritesOneShellModeLineNamingTheTrigger()
    {
        var lines = new List<string>();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = lines.Add;

        try
        {
            Sta.Run(() =>
            {
                var mode = new ShellModeController(new ContentControl(), new Border(), () => new Grid());

                mode.Set(ShellViewMode.Explorer, "shell.toggleExplorer");
                mode.Set(ShellViewMode.Workbench, "document-opening");
            }, 30);
        }
        finally
        {
            WorkbenchDiagnostics.Sink = previous;
        }

        var modes = lines.Select(l => JsonDocument.Parse(l).RootElement)
            .Where(e => e.GetProperty("evt").GetString() == "shell.mode")
            .ToList();

        Assert.Equal(2, modes.Count);
        Assert.Equal("Explorer", modes[0].GetProperty("mode").GetString());
        Assert.Equal("Workbench", modes[0].GetProperty("from").GetString());
        Assert.Equal("shell.toggleExplorer", modes[0].GetProperty("trigger").GetString());
        Assert.Equal("Workbench", modes[1].GetProperty("mode").GetString());
        Assert.Equal("Explorer", modes[1].GetProperty("from").GetString());
        Assert.Equal("document-opening", modes[1].GetProperty("trigger").GetString());
    }

    /// <summary>A call that changes nothing writes nothing — the seam fires on every open, and most opens happen in the workbench.</summary>
    [Fact]
    public void Set_ToTheCurrentMode_WritesNothing()
    {
        var lines = new List<string>();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = lines.Add;

        try
        {
            Sta.Run(() =>
            {
                var mode = new ShellModeController(new ContentControl(), new Border(), () => new Grid());

                mode.Set(ShellViewMode.Workbench, "document-opening");
            }, 30);
        }
        finally
        {
            WorkbenchDiagnostics.Sink = previous;
        }

        Assert.DoesNotContain(lines, l => l.Contains("\"evt\":\"shell.mode\"", StringComparison.Ordinal));
    }

    private static List<int> Occurrences(string text, string token)
    {
        var found = new List<int>();
        for (var at = text.IndexOf(token, StringComparison.Ordinal); at >= 0; at = text.IndexOf(token, at + 1, StringComparison.Ordinal))
        {
            found.Add(at);
        }

        return found;
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

    private static string SourceFile(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepoRoot(), .. parts]));
}
