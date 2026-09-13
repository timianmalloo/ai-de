using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// §A11 / §B5: the four composer states — <c>draft · preparing · prepared(reason) · stale</c> — each
/// with a reason string in voice and a next-action control, walked on the real surface (STA) with a
/// fake compiler; the compile line's strings are table-driven in Core (<c>TheCompileLineHasTenStringsTests</c>).
/// </summary>
public sealed class ThePrepareStatesWalkTests
{
    private sealed class NeverAsked : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
    }

    private static ComposerSendContext Context() => new(
        RepositoryRoot: @"C:\repo", DataDirectory: @"C:\data", AdapterInstallRoot: @"C:\adapter",
        EngineId: "claude-code", Model: "claude-sonnet-5", AccountLabel: "max", TaskClass: "implement",
        ProofPackArtifacts: [], Providers: []);

    private static ComposerSurface Build(string mode, Func<CompileRequest, CancellationToken, Task<CompileResult>> compiler)
    {
        var surface = new ComposerSurface("composer:s-cv3", "s-cv3 — composer");
        surface.Configure(
            new SessionConfig("s-cv3", "first", "w-1", DateTimeOffset.UnixEpoch, ["claude-code"]) { CompileMode = mode },
            Context(),
            ComposerFields.FreeForm(),
            new AttachmentGate(@"C:\repo", new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max"));
        surface.Gate.Compiler = compiler;
        return surface;
    }

    private const string Proposal =
        """{"contract":"compile-output/1","decorations":[{"name":"goal","value":"Rename the helper.","confidence":0.8,"grounded_in":[{"input":"source_text","span":[0,6]}]},{"name":"done_when","value":"It compiles.","confidence":0.7,"grounded_in":[{"input":"source_text","span":[0,6]}]},{"name":"not_in_scope","value":"Nothing else.","confidence":0.6,"grounded_in":[{"input":"source_text","span":[0,6]}]}]}""";

    private static CompileResult Answered(string raw) => new(CompileCallOutcomes.Answered, null, raw, null, "claude-opus-5[1m]", 5, 0, 0, 1, false, 0, "account", null, []);

    /// <summary>
    /// Pumps the STA thread's dispatcher until the preparing gesture in flight has finished — the
    /// surface's <c>await</c> continuations post to this dispatcher (the context is installed by
    /// <see cref="OnDispatcher"/>), exactly as the product's do.
    /// </summary>
    private static void Drain(ComposerSurface surface)
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (surface.Preparing is { IsCompleted: false } && DateTime.UtcNow < deadline)
        {
            dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
            Thread.Sleep(5);
        }

        Assert.True(surface.Preparing?.IsCompleted, "the preparing gesture did not finish");
        surface.Preparing!.GetAwaiter().GetResult();
    }

    private static void OnDispatcher(Action body) => Sta.Run(() =>
    {
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        body();
    });

    /// <summary>
    /// draft (no line) → preparing (Cancel on screen, a Send ignored with its reason) → prepared
    /// (the line, the derived marks, no control on a success) → stale (the mark, Prepare again) →
    /// prepared again → sent.
    /// </summary>
    [Fact]
    public void TheFourStatesEachCarryAReasonAndANextAction()
    {
        OnDispatcher(() =>
        {
            var gate = new TaskCompletionSource<CompileResult>();
            var surface = Build(CompileModes.AgenticAdvisory, (_, _) => gate.Task);
            surface.SetFieldText(surface.Fields[0].Id, 1, "Rename the helper in Money.cs.");

            // draft: no compile line (E5).
            Assert.Equal(PrepareState.Draft, surface.PrepareState);
            Assert.Null(surface.CompileLine);

            // The first gesture prepares.
            Assert.Null(surface.Send());
            Assert.Equal(PrepareState.Preparing, surface.PrepareState);
            Assert.Equal("Preparing…", surface.CompileLine);
            Assert.True(surface.CancelVisible);
            Assert.False(surface.PrepareAgainVisible);

            // A gesture while preparing is ignored with its reason (Ruling 77).
            Assert.Null(surface.Send());
            Assert.Equal(ComposerSendGate.PreparingReason, surface.Status);

            gate.SetResult(Answered(Proposal));
            Drain(surface);

            // prepared: the line in voice, the derived marks with their keep control, no Prepare again.
            Assert.Equal(PrepareState.Prepared, surface.PrepareState);
            Assert.StartsWith("Compiled on claude-opus-5[1m]", surface.CompileLine, StringComparison.Ordinal);
            Assert.False(surface.CancelVisible);
            Assert.False(surface.PrepareAgainVisible);
            Assert.Equal("? derived", surface.StructureMarks[GoalBlockFields.GoalKey]);
            Assert.Equal("prepared — press again to send", surface.Status);

            // stale: the draft changed after Prepare.
            surface.SetFieldText(surface.Fields[0].Id, 1, "Rename the helper in Money.cs and Ledger.cs.");
            Assert.Equal(PrepareState.Stale, surface.PrepareState);
            Assert.StartsWith("stale —", surface.CompileLine, StringComparison.Ordinal);
            Assert.True(surface.PrepareAgainVisible);

            // The next gesture re-prepares (the fake answers at once now) and the one after sends.
            Assert.Null(surface.Send());
            Drain(surface);
            Assert.Equal(PrepareState.Prepared, surface.PrepareState);

            var request = surface.Send();
            Assert.NotNull(request);
            Assert.Equal(1, surface.Gate.SendCount);
            // Advisory: nothing was kept, so the block is blank and the turn is a Message.
            Assert.Null(request!.Goal);
        });
    }

    /// <summary>A degraded call: the line reads <i>compiled mechanically — reason</i>, Prepare again is the control, the lines stay empty and editable, and the next gesture sends.</summary>
    [Fact]
    public void ADegradedCallShowsItsReasonAndPrepareAgainAndStillSends()
    {
        OnDispatcher(() =>
        {
            var surface = Build(CompileModes.Agentic, (_, _) => Task.FromResult(new CompileResult(CallOutcomes.Unavailable, "needs-login", null, null, Envelope.NotRecorded, null, 0, 0, null, false, 0, Envelope.NotRecorded, null, [])));
            surface.SetFieldText(surface.Fields[0].Id, 1, "Rename the helper.");

            Assert.Null(surface.Send());
            Drain(surface);

            Assert.Equal(PrepareState.Prepared, surface.PrepareState);
            Assert.Equal("compiled mechanically — needs-login", surface.CompileLine);
            Assert.True(surface.PrepareAgainVisible);
            Assert.Equal("— fill in", surface.StructureMarks[GoalBlockFields.GoalKey]);

            Assert.NotNull(surface.Send());
            Assert.Equal(1, surface.Gate.SendCount);
        });
    }

    /// <summary>Under advisory, keeping a line through its control marks it <i>kept</i> and the line reaches the run.</summary>
    [Fact]
    public void KeepingADerivedLineMarksItKeptAndItReachesTheRun()
    {
        OnDispatcher(() =>
        {
            var surface = Build(CompileModes.AgenticAdvisory, (_, _) => Task.FromResult(Answered(Proposal)));
            surface.SetFieldText(surface.Fields[0].Id, 1, "Rename the helper.");

            Assert.Null(surface.Send());
            Drain(surface);
            Assert.Equal("? derived", surface.StructureMarks[GoalBlockFields.GoalKey]);

            surface.KeepStructureLine(GoalBlockFields.GoalKey);
            surface.KeepStructureLine(GoalBlockFields.DoneWhenKey);
            surface.KeepStructureLine(GoalBlockFields.NotInScopeKey);
            Assert.Equal("✓ kept", surface.StructureMarks[GoalBlockFields.GoalKey]);

            var request = surface.Send();
            Assert.NotNull(request);
            Assert.Equal("Rename the helper.", request!.Goal!.Goal);
            Assert.Equal("It compiles.", request.Goal.DoneWhen);
        });
    }
}
