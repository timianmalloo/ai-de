using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using AiDe.App.Conductor;
using AiDe.App.Tests.Records;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Sessions;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>T30 (C) — Ruling 180 (3), C1: a suggested line counts only when kept, and a kept line is kept.</b> Under
/// <c>agentic-advisory</c> the compile step suggests Goal, Done when and Not in scope, each marked <i>? derived</i> with
/// a <i>keep</i> control. T29 found that Keep appended an operator row to the prepared envelope but never wrote the
/// draft, so the next prepare re-opened the line and the model re-derived it (<c>ComposerSurface.cs</c>'s keep
/// callback). Every step is the user's gesture on the product's composition (<see cref="ComposerGesture"/>): the words
/// through the editor's input verb, Ctrl+Enter to prepare, the Keep button through its invoke peer. No model is called:
/// the compile host is a stub that answers a different suggestion on each call, so a re-derived line is visible.
/// </summary>
public sealed class AKeptSuggestedLineIsKeptTests(ITestOutputHelper output) : IDisposable
{
    private const string Words = "Rename the helper in the parser.";

    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    /// <summary>
    /// Type, prepare (three suggested lines), Keep Goal, edit the prompt, prepare again: Goal still reads the kept line,
    /// is marked kept, is in the draft, and the second prepare suggested no Goal.
    /// </summary>
    [Fact]
    public void KeepingASuggestedGoal_ThenPreparingAgain_TheKeptGoalIsStillThere()
    {
        ComposerGesture.Drive(_fixture, async (window, document) =>
        {
            var composer = document.Composer;
            var prompts = UnderAdvisory(composer);

            ComposerGesture.Type(composer, Words);
            ComposerGesture.Settle(document);
            ComposerGesture.CtrlEnter(composer);
            await composer.Preparing!.WaitAsync(NotebookFixture.Bound);
            ComposerGesture.Settle(document);
            Assert.Equal("? derived", composer.StructureMarks[GoalBlockFields.GoalKey]);
            Assert.Equal(Suggestion(1, "goal"), Line(composer, "Goal").Text);

            Assert.Equal(StructureLine.KeepHelp, AutomationProperties.GetHelpText(KeepButton(composer, "Goal")));
            Invoke(KeepButton(composer, "Goal"));
            ComposerGesture.Settle(document);
            Assert.Equal("✓ kept", composer.StructureMarks[GoalBlockFields.GoalKey]);

            ComposerGesture.Type(composer, Words + " Keep the public names.");
            ComposerGesture.Settle(document);
            ComposerGesture.CtrlEnter(composer);
            await composer.Preparing!.WaitAsync(NotebookFixture.Bound);
            ComposerGesture.Settle(document);

            output.WriteLine($"compile calls {prompts.Count}; after the second prepare: Goal \"{Line(composer, "Goal").Text}\" marked \"{composer.StructureMarks[GoalBlockFields.GoalKey]}\"; draft goal \"{DraftGoal(composer)}\"; Done when \"{Line(composer, "Done when").Text}\" marked \"{composer.StructureMarks[GoalBlockFields.DoneWhenKey]}\"");
            Assert.Equal(2, prompts.Count);
            Assert.Equal(Suggestion(1, "goal"), DraftGoal(composer));
            Assert.Equal(Suggestion(1, "goal"), Line(composer, "Goal").Text);
            Assert.Equal("✓ kept", composer.StructureMarks[GoalBlockFields.GoalKey]);
            Assert.False(composer.Gate.DerivedLines.ContainsKey(GoalBlockFields.GoalKey), "the second prepare asked the model for the kept Goal again");

            // The unkept lines were suggested again, and are still only suggestions: never in the draft.
            Assert.Equal(Suggestion(2, "done_when"), Line(composer, "Done when").Text);
            Assert.Equal("? derived", composer.StructureMarks[GoalBlockFields.DoneWhenKey]);
            Assert.False(composer.Draft.GoalValues.TryGetValue(GoalBlockFields.DoneWhenKey, out var doneWhen) && doneWhen.Length > 0, $"an unkept suggestion reached the draft: \"{doneWhen}\"");
        });
    }

    /// <summary>
    /// T30 n2 (B), the Owner under Ruling 180 (3): a model-derived line is marked <i>suggested</i>, on screen and in UIA
    /// (the line's ItemStatus), and a kept line no longer says it.
    /// </summary>
    [Fact]
    public void AModelDerivedLine_IsMarkedSuggested_OnScreenAndInUia_AndAKeptLineIsNot()
    {
        ComposerGesture.Drive(_fixture, async (window, document) =>
        {
            var composer = document.Composer;
            UnderAdvisory(composer);

            ComposerGesture.Type(composer, Words);
            ComposerGesture.Settle(document);
            ComposerGesture.CtrlEnter(composer);
            await composer.Preparing!.WaitAsync(NotebookFixture.Bound);
            ComposerGesture.Settle(document);
            output.WriteLine($"suggested: mark \"{composer.StructureMarks[GoalBlockFields.GoalKey]}\" · UIA ItemStatus \"{AutomationProperties.GetItemStatus(Line(composer, "Goal"))}\"");
            Assert.Equal("? suggested", composer.StructureMarks[GoalBlockFields.GoalKey]);
            Assert.Equal("suggested", AutomationProperties.GetItemStatus(Line(composer, "Goal")));

            Invoke(KeepButton(composer, "Goal"));
            ComposerGesture.Settle(document);
            output.WriteLine($"kept: mark \"{composer.StructureMarks[GoalBlockFields.GoalKey]}\" · UIA ItemStatus \"{AutomationProperties.GetItemStatus(Line(composer, "Goal"))}\"");
            Assert.Equal("✓ kept", composer.StructureMarks[GoalBlockFields.GoalKey]);
            Assert.DoesNotContain("suggested", AutomationProperties.GetItemStatus(Line(composer, "Goal")), StringComparison.Ordinal);
            Assert.Equal("? suggested", composer.StructureMarks[GoalBlockFields.DoneWhenKey]);
        });
    }

    /// <summary>A suggested line that is never kept never reaches the draft, so it cannot project (Envelope.cs's Confirmed).</summary>
    [Fact]
    public void ASuggestedLineThatIsNotKept_NeverReachesTheDraft()
    {
        ComposerGesture.Drive(_fixture, async (window, document) =>
        {
            var composer = document.Composer;
            UnderAdvisory(composer);

            ComposerGesture.Type(composer, Words);
            ComposerGesture.Settle(document);
            ComposerGesture.CtrlEnter(composer);
            await composer.Preparing!.WaitAsync(NotebookFixture.Bound);
            ComposerGesture.Settle(document);

            Assert.Equal(Suggestion(1, "goal"), Line(composer, "Goal").Text);
            Assert.All(
                ComposerDraft.PerPromptGoalFields,
                field => Assert.False(composer.Draft.GoalValues.TryGetValue(field, out var value) && value.Length > 0, $"{field} reached the draft unkept: \"{value}\""));
        });
    }

    /// <summary>
    /// <b>C1's restart half, measured (rule 14).</b> Keep a suggested Goal, prepare again (the notebook revision is saved
    /// at prepare), then restart through the shell's revive path over a daemon, as T17d's check 1 reopens a notebook, and
    /// read the revived composer's draft and the stored revision. <b>Measured 2026-10-19 (T30 n2):</b> the kept line does
    /// not survive. The revision holds the editor's words only, and the revived composer opens a new, empty draft
    /// (<c>c1-restart-measure.txt</c>). This pins that fact until the Owner rules on draft persistence; a persistence fix
    /// turns it red on purpose, and then its last two asserts invert.
    /// </summary>
    [Fact]
    public async Task AKeptGoal_IsInNeitherTheStoredRevisionNorTheRevivedDraft_AfterARestartThroughTheShellsRevive()
    {
        var accounts = new[] { new AccountRef("anthropic", "max") };
        new SessionConfigStore(_fixture.Root, NotebookFixture.SessionId).Create("Binding", _fixture.Root, accounts, accounts[0], DateTimeOffset.UnixEpoch);
        ComposerGesture.Drive(_fixture, async (window, document) =>
        {
            var composer = document.Composer;
            UnderAdvisory(composer);
            ComposerGesture.Type(composer, Words);
            ComposerGesture.Settle(document);
            ComposerGesture.CtrlEnter(composer);
            await composer.Preparing!.WaitAsync(NotebookFixture.Bound);
            ComposerGesture.Settle(document);
            Invoke(KeepButton(composer, "Goal"));
            ComposerGesture.Settle(document);

            // An edit first: a second press on a prepared draft would send it, and a send consumes the draft.
            ComposerGesture.Type(composer, Words + " Keep the public names.");
            ComposerGesture.Settle(document);
            ComposerGesture.CtrlEnter(composer);
            await composer.Preparing!.WaitAsync(NotebookFixture.Bound);
            ComposerGesture.Settle(document);
            output.WriteLine($"before restart: draft goal \"{DraftGoal(composer)}\"; mark \"{composer.StructureMarks[GoalBlockFields.GoalKey]}\"; editor \"{composer.Draft.SourceText}\"");
            Assert.Equal(Suggestion(1, "goal"), DraftGoal(composer));
        });

        var pipe = "aide-t30-c1-" + Guid.NewGuid().ToString("N");
        var endpoint = new AiDe.Core.Ipc.DaemonEndpoint(pipe, new AiDe.Core.Ipc.CapabilityRegistry(), _ => _fixture.Store.CoreEpoch);
        AiDe.Core.Ipc.DaemonOperations.Register(endpoint, () => _fixture.Store.CoreEpoch);
        AiDe.Core.Ipc.WorkspaceOperations.RegisterNotebook(endpoint, new AiDe.Core.Ipc.NotebookService(_fixture.Store));
        AiDe.Core.Ipc.WorkspaceOperations.RegisterDispatch(endpoint, new AiDe.Core.Dispatch.BoundaryDispatcher(_fixture.Store));
        var server = new AiDe.Core.Ipc.IpcServer(pipe, endpoint, new AiDe.Core.Ipc.IpcServerOptions(StartupGrace: TimeSpan.FromSeconds(45)));
        using var life = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var running = server.RunAsync(life.Token);
        try
        {
            await using var client = await AiDe.Core.Ipc.WorkspaceClient.ConnectAsync(pipe, TimeSpan.FromSeconds(20), CancellationToken.None);
            var head = await client.ReadLatestNotebookRevisionAsync(NotebookFixture.SessionId, CancellationToken.None);
            output.WriteLine($"stored head revision {head?.RevisionNo}: body \"{head?.Body}\"");
            Assert.DoesNotContain(Suggestion(1, "goal"), head!.Body, StringComparison.Ordinal);
            var revivedGoal = "(not read)";
            Sta.Pump(
                create: () => new Border(),
                configure: window => { window.Width = 1200; window.Height = 800; },
                body: async (window, border) =>
                {
                    using var shell = new AiDe.App.Workbench.WorkbenchShell(client);
                    border.Child = shell.WorkbenchRoot;
                    var surfaceId = AiDe.App.Workbench.Sessions.SessionDocumentSurface.SurfaceIdFor(NotebookFixture.SessionId);
                    shell.Service.Apply(new AiDe.Core.Workbench.LayoutOperation.AddSurface(shell.Service.Current.AllStacks().First().Id, new AiDe.Core.Workbench.Surface(surfaceId, AiDe.App.Workbench.Sessions.SessionDocumentSurface.Kind, "Binding")));
                    shell.Adapter.Render();
                    Assert.Single(shell.ReviveRestoredSessionDocuments(_fixture.Root));
                    shell.Adapter.Render();
                    var revived = shell.Adapter.SurfaceContent<AiDe.App.Workbench.Sessions.SessionDocumentSurface>(surfaceId)!;
                    await revived.LastReadBack.WaitAsync(NotebookFixture.Bound);
                    ComposerGesture.Settle(revived);
                    revivedGoal = DraftGoal(revived.Composer);
                    output.WriteLine($"after restart: draft goal \"{revivedGoal}\"; shape {revived.Composer.Draft.Shape}; editor \"{revived.Composer.Draft.SourceText}\"");
                    border.Child = null;
                });
            Assert.Equal(string.Empty, revivedGoal);
        }
        finally
        {
            await life.CancelAsync();
            try
            {
                await running;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    /// <summary>
    /// The session's rung set to <c>agentic-advisory</c>, the draft on the goal-block form (as
    /// <c>SessionComposerBinder.Bind</c> opens it), and a stub compile host that records each prompt.
    /// </summary>
    private static List<string> UnderAdvisory(ComposerSurface composer)
    {
        composer.Draft.SwitchTo(ComposerShape.GoalBlock);
        composer.Gate.BindSession(NotebookFixture.SessionId, CompileModes.AgenticAdvisory, "claude-code", "implement");
        var prompts = new List<string>();
        composer.Gate.Compiler = (request, _) =>
        {
            prompts.Add(request.Prompt);
            return Task.FromResult(Answered(prompts.Count));
        };
        return prompts;
    }

    /// <summary>What the stub suggests for <paramref name="line"/> on its <paramref name="call"/>-th call.</summary>
    private static string Suggestion(int call, string line) => $"Suggestion {call} for {line}.";

    private static CompileResult Answered(int call)
    {
        string Row(string name, double confidence) =>
            $$"""{"name":"{{name}}","value":"{{Suggestion(call, name)}}","confidence":{{confidence.ToString(System.Globalization.CultureInfo.InvariantCulture)}},"grounded_in":[{"input":"source_text","span":[0,6]}]}""";
        var raw = $$"""{"contract":"compile-output/1","decorations":[{{Row("goal", 0.8)}},{{Row("done_when", 0.7)}},{{Row("not_in_scope", 0.6)}}]}""";
        return new CompileResult(CompileCallOutcomes.Answered, null, raw, null, "claude-opus-5[1m]", 5, 0, 0, 1, false, 0, "account", null, []);
    }

    private static string DraftGoal(ComposerSurface composer) =>
        composer.Draft.GoalValues.TryGetValue(GoalBlockFields.GoalKey, out var value) ? value : string.Empty;

    private static TextBox Line(ComposerSurface composer, string name) =>
        ComposerGesture.Logical<TextBox>(composer).Single(b => AutomationProperties.GetName(b) == name);

    private static Button KeepButton(ComposerSurface composer, string name) =>
        ComposerGesture.Logical<Button>(composer).Single(b => AutomationProperties.GetName(b) == "Keep " + name);

    /// <summary>The button's own click, through its automation peer.</summary>
    private static void Invoke(Button button) =>
        ((IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)!).Invoke();
}
