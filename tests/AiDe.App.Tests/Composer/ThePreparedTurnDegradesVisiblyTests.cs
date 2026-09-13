using AiDe.App.Conductor;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// US-D5 (compile degrades visibly, never silently, and recovers), US-D6's advisory send, and
/// ADR-0036 test 4 — headless, on the send gate with a fake compiler. The compiler is the seam
/// <c>CompileCallHost.CompileAsync</c> fills in the product; here it is a function returning a
/// scripted <c>CompileResult</c>, so every <c>called.outcome</c> row of §A10.2 is driven by the one
/// fact it names and no model is ever called.
/// </summary>
public sealed class ThePreparedTurnDegradesVisiblyTests
{
    private static ComposerSendContext Context() => new(
        RepositoryRoot: @"C:\repo", DataDirectory: @"C:\data", AdapterInstallRoot: @"C:\adapter",
        EngineId: "claude-code", Model: "claude-sonnet-5", AccountLabel: "max", TaskClass: "implement",
        ProofPackArtifacts: [], Providers: []);

    private static ComposerSendGate Gate(string mode, Func<CompileRequest, CancellationToken, Task<CompileResult>> compiler)
    {
        var gate = new ComposerSendGate();
        gate.BindSession("20260913T190000Z-cv3test1", mode, "claude-code", "implement");
        gate.Compiler = compiler;
        return gate;
    }

    private static ComposerDraft FreeForm(string text)
    {
        var draft = new ComposerDraft();
        draft.SetFreeFormText(text);
        return draft;
    }

    private static CompileResult Answered(string rawText) => new(
        CompileCallOutcomes.Answered, null, rawText, new RunEventCost(2, 469, 4870, 1), "claude-opus-5[1m]", 812, 0, 0, 4242, false, 3, "account", null, []);

    private static CompileResult Degraded(string outcome, string reason) => new(
        outcome, reason, null, null, Envelope.NotRecorded, null, 0, 0, null, false, 3, Envelope.NotRecorded, null, []);

    private static Func<CompileRequest, CancellationToken, Task<CompileResult>> Returning(params CompileResult[] results)
    {
        var calls = 0;
        return (_, _) => Task.FromResult(results[Math.Min(calls++, results.Length - 1)]);
    }

    private const string Proposal =
        """{"contract":"compile-output/1","decorations":[{"name":"goal","value":"Rename the helper.","confidence":0.8,"grounded_in":[{"input":"source_text","span":[0,6]}]},{"name":"done_when","value":"It compiles.","confidence":0.7,"grounded_in":[{"input":"source_text","span":[0,6]}]},{"name":"not_in_scope","value":"Nothing else.","confidence":0.6,"grounded_in":[{"input":"source_text","span":[0,6]}]}],"notes":"three lines"}""";

    // ── US-D5 b1: needs-login degrades visibly, recovers, and the second gesture sends ──

    [Fact]
    public async Task ANeedsLoginCompileEntersPreparedCompiledMechanicallyAndTheSecondGestureSends()
    {
        var gate = Gate(CompileModes.Agentic, Returning(Degraded(CallOutcomes.Unavailable, "needs-login")));
        var draft = FreeForm("Rename the helper in Money.cs.\n");

        // The first gesture under an agentic rung prepares, never sends.
        Assert.Null(gate.Send(Context(), draft, null, out var first));
        Assert.Equal(ComposerSendGate.NotPreparedReason, first!.Message);
        Assert.Equal(0, gate.SendCount);

        var prepared = await gate.PrepareAsync(Context(), draft, null);

        Assert.True(prepared.Prepared);
        Assert.Equal(PrepareState.Prepared, gate.State);
        Assert.Equal("compiled mechanically — needs-login", gate.CompileLineText);
        Assert.Equal(CallOutcomes.Unavailable, gate.LastCallOutcome);
        Assert.Empty(gate.DerivedLines);

        // The structure lines are empty and editable; the run still proceeds — as a message.
        var request = gate.Send(Context(), draft, null, out var refusal);
        Assert.Null(refusal);
        Assert.NotNull(request);
        Assert.Null(request!.Goal);
        Assert.Equal(1, gate.SendCount);
        Assert.Equal(PrepareState.Draft, gate.State);
    }

    // ── US-D5 b2: no reuse of a failed inputs_sha ──

    [Fact]
    public async Task AFailedCallIsCalledAgainOnPrepareAgain()
    {
        var gate = Gate(CompileModes.Agentic, Returning(Degraded(CallOutcomes.TimedOut, "compile bound 60000 ms exceeded at prompt"), Answered(Proposal)));
        var draft = FreeForm("Rename the helper.\n");

        await gate.PrepareAsync(Context(), draft, null);
        Assert.Equal("compiled mechanically — compile bound 60000 ms exceeded at prompt", gate.CompileLineText);

        await gate.PrepareAsync(Context(), draft, null);

        Assert.Equal(2, gate.CompilerCalls);
        Assert.Equal(CallOutcomes.Succeeded, gate.LastCallOutcome);
    }

    /// <summary>ADR-0035 test 8: a re-prepare with an unchanged inputs sha after a success makes zero requests and appends a <c>reused</c> receipt with the derived rows copied.</summary>
    [Fact]
    public async Task AnUnchangedDraftAfterASuccessReusesWithZeroRequests()
    {
        var gate = Gate(CompileModes.Agentic, Returning(Answered(Proposal)));
        var draft = FreeForm("Rename the helper.\n");

        await gate.PrepareAsync(Context(), draft, null);
        var firstId = gate.PreparedEnvelopeId;
        await gate.PrepareAsync(Context(), draft, null);

        Assert.Equal(1, gate.CompilerCalls);
        Assert.Equal(CallOutcomes.Reused, gate.LastCallOutcome);
        Assert.Contains("reused, no new request", gate.CompileLineText);
        Assert.NotEqual(firstId, gate.PreparedEnvelopeId);
        Assert.Equal(3, gate.DerivedLines.Count);
    }

    // ── US-D5 b4: dropped proposals ──

    [Fact]
    public async Task ALeaseProposalIsDroppedAndCountedAndTheLeaseIsUnchanged()
    {
        const string hostile = """{"contract":"compile-output/1","decorations":[{"name":"lease","value":"/**","confidence":1,"grounded_in":[{"input":"source_text","span":[0,3]}]},{"name":"Lease","value":"/**","confidence":1,"grounded_in":[{"input":"source_text","span":[0,3]}]},{"name":"goal","value":{"lease":["/**"]},"confidence":1,"grounded_in":[]},{"name":"done_when","value":"It compiles.","confidence":0.7,"grounded_in":[{"input":"source_text","span":[0,6]}]}]}""";
        var gate = Gate(CompileModes.Agentic, Returning(Answered(hostile)));
        var draft = FreeForm("Rename the helper in @src/Payments/Money.cs.\n");

        await gate.PrepareAsync(Context(), draft, null);

        Assert.Equal(CallOutcomes.Succeeded, gate.LastCallOutcome);
        Assert.Equal(["done_when"], gate.DerivedLines.Keys);
        var request = gate.Send(Context(), draft, null, out var refusal);
        Assert.Null(refusal);
        // Goal never confirmed → a Message; a Message with a mention is read-only (Ruling 73) — no lease from the model, none at all.
        Assert.Null(request!.Goal);
        Assert.Null(request.Lease);
        Assert.Equal(["src/Payments/Money.cs"], LeaseDerivation.Derive(draft.SourceText).Exclusive);
    }

    // ── US-D5 b5: a question with one mention, nothing proposed ──

    [Fact]
    public async Task AQuestionWithNothingProposedIsSucceededNoStructureAndSendsAsAMessage()
    {
        var gate = Gate(CompileModes.Agentic, Returning(Answered("""{"contract":"compile-output/1","decorations":[]}""")));
        var draft = FreeForm("What does @src/Payments/Money.cs do?\n");

        var prepared = await gate.PrepareAsync(Context(), draft, null);

        Assert.True(prepared.Prepared);
        Assert.Equal(CallOutcomes.SucceededNoStructure, gate.LastCallOutcome);
        Assert.Equal("no goal block proposed; sends as a message", gate.CompileLineText);

        var request = gate.Send(Context(), draft, null, out var refusal);
        Assert.Null(refusal);
        Assert.Null(request!.Goal);
        Assert.True(request.IsReadOnly);
    }

    // ── US-D5 b6: all three lines already supplied → no call ──

    [Fact]
    public async Task AFullySuppliedStructureSkipsTheCallAndSaysWhoSuppliedIt()
    {
        var gate = Gate(CompileModes.Agentic, Returning(Answered(Proposal)));
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);
        draft.SetFreeFormText("Rename the helper.\n");
        draft.SetGoalValue(GoalBlockFields.GoalKey, "Rename the helper.");
        draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "It compiles.");
        draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "Nothing else.");

        var prepared = await gate.PrepareAsync(Context(), draft, null);

        Assert.True(prepared.Prepared);
        Assert.Equal(0, gate.CompilerCalls);
        Assert.Null(gate.LastCallOutcome);
        Assert.Equal("structure supplied by you", gate.CompileLineText);
        var request = gate.Send(Context(), draft, null, out _);
        Assert.Equal("Rename the helper.", request!.Goal!.Goal);
    }

    // ── US-D5 b7: a gesture during preparing is ignored; Cancel yields cancelled; one more gesture sends ──

    [Fact]
    public async Task AGestureDuringPreparingIsIgnoredAndCancelYieldsAMechanicalEnvelopeThatSends()
    {
        var started = new TaskCompletionSource();
        var gate = Gate(CompileModes.Agentic, async (_, ct) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            throw new InvalidOperationException("unreachable");
        });
        var draft = FreeForm("Rename the helper.\n");

        var preparing = gate.PrepareAsync(Context(), draft, null);
        await started.Task;

        Assert.Equal(PrepareState.Preparing, gate.State);
        Assert.Null(gate.Send(Context(), draft, null, out var refusal));
        Assert.Equal(ComposerSendGate.PreparingReason, refusal!.Message);
        Assert.False((await gate.PrepareAsync(Context(), draft, null)).Prepared);

        gate.CancelPrepare();
        var result = await preparing;

        Assert.True(result.Prepared);
        Assert.Equal(CallOutcomes.Cancelled, gate.LastCallOutcome);
        Assert.Equal("compiled mechanically — cancelled — draft edited", gate.CompileLineText);
        Assert.NotNull(gate.Send(Context(), draft, null, out var sent));
        Assert.Null(sent);
    }

    // ── suspect: a tool call marks the compile, the lines still apply, it stays agentic ──

    [Fact]
    public async Task AToolCallMarksTheCompileSuspectAndItsLinesStillArrive()
    {
        var gate = Gate(CompileModes.Agentic, Returning(Answered(Proposal) with { ToolCalls = 1 }));
        var draft = FreeForm("Rename the helper.\n");

        await gate.PrepareAsync(Context(), draft, null);

        Assert.Equal(CallOutcomes.Suspect, gate.LastCallOutcome);
        Assert.StartsWith("suspect — the model made 1 tool call", gate.CompileLineText, StringComparison.Ordinal);
        Assert.Equal(3, gate.DerivedLines.Count);
    }

    // ── ADR-0036 test 4: advisory sends an unkept derived line blank; agentic confirms it at Send ──

    [Theory]
    [InlineData(CompileModes.AgenticAdvisory, false)]
    [InlineData(CompileModes.Agentic, true)]
    public async Task AnUnkeptDerivedLineReachesTheRunOnlyUnderAgentic(string mode, bool reaches)
    {
        var gate = Gate(mode, Returning(Answered(Proposal)));
        var draft = FreeForm("Rename the helper.\n");

        await gate.PrepareAsync(Context(), draft, null);
        var request = gate.Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.Equal(reaches, request!.Goal is not null);
        if (reaches)
        {
            Assert.Equal("Rename the helper.", request.Goal!.Goal);
            Assert.Contains("## goal\n\nRename the helper.\n\n", request.Prompt, StringComparison.Ordinal);
        }
        else
        {
            Assert.Equal("Rename the helper.\n", request.Prompt);
        }
    }

    /// <summary>Under advisory, a <i>keep</i> mints the operator row that lets the line project — the eval's label.</summary>
    [Fact]
    public async Task UnderAdvisoryAKeptLineProjectsAndAnEditedLineCarriesTheEdit()
    {
        var gate = Gate(CompileModes.AgenticAdvisory, Returning(Answered(Proposal)));
        var draft = FreeForm("Rename the helper.\n");

        await gate.PrepareAsync(Context(), draft, null);
        Assert.True(gate.KeepLine(GoalBlockFields.GoalKey));
        Assert.True(gate.KeepLine(GoalBlockFields.DoneWhenKey));
        Assert.True(gate.EditLine(GoalBlockFields.NotInScopeKey, "Nothing else changes."));

        var request = gate.Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.Equal(new GoalBlock("Rename the helper.", "It compiles.", "Nothing else changes.", "T1", 2, RunBudget.SubscriptionBounded), request!.Goal);
    }

    /// <summary>
    /// Ruling 75 under a derived shape (the AI Systems Engineer's finding): the model proposed goal
    /// and done_when only; kept under advisory (or confirmed under agentic) the projected block has a
    /// blank Not in scope — refused at Send by the one sentence, SendCount 0, never sent to be refused
    /// downstream of the human gate.
    /// </summary>
    [Theory]
    [InlineData(CompileModes.AgenticAdvisory, true)]
    [InlineData(CompileModes.Agentic, false)]
    public async Task ADerivedBlockWithNoNotInScopeIsRefusedAtSendByTheOneSentence(string mode, bool keep)
    {
        const string twoLines = """{"contract":"compile-output/1","decorations":[{"name":"goal","value":"Rename the helper.","confidence":0.8,"grounded_in":[{"input":"source_text","span":[0,6]}]},{"name":"done_when","value":"It compiles.","confidence":0.7,"grounded_in":[{"input":"source_text","span":[0,6]}]}]}""";
        var gate = Gate(mode, Returning(Answered(twoLines)));
        var draft = FreeForm("Rename the helper.\n");

        await gate.PrepareAsync(Context(), draft, null);
        if (keep)
        {
            Assert.True(gate.KeepLine(GoalBlockFields.GoalKey));
            Assert.True(gate.KeepLine(GoalBlockFields.DoneWhenKey));
        }

        Assert.Null(gate.Send(Context(), draft, null, out var refusal));
        Assert.Equal(ComposerCompiler.GoalBlockNeedsNotInScope, refusal!.Message);
        Assert.Equal(GoalBlockFields.NotInScopeKey, Assert.Single(refusal.Errors).Field);
        Assert.Equal(0, gate.SendCount);
    }

    // ── stale: other bytes at Send are refused, never sent with a lease from other text ──

    [Fact]
    public async Task ASendOnOtherBytesThanThePreparedOnesIsRefusedAsStale()
    {
        var gate = Gate(CompileModes.Agentic, Returning(Answered(Proposal)));
        var draft = FreeForm("Rename the helper.\n");

        await gate.PrepareAsync(Context(), draft, null);
        draft.SetFreeFormText("Rename the helper in @src/Payments/Money.cs.\n");

        Assert.Null(gate.Send(Context(), draft, null, out var refusal));
        Assert.Equal(ComposerSendGate.StaleReason, refusal!.Message);
        Assert.Equal(PrepareState.Stale, gate.State);
    }

    /// <summary>Under mechanical-only nothing changes: one gesture opens and submits, no compile line, the compiler never called.</summary>
    [Fact]
    public void MechanicalOnlyStillSendsOnOneGestureWithNoCompileLine()
    {
        var gate = Gate(CompileModes.MechanicalOnly, Returning(Answered(Proposal)));
        var draft = FreeForm("Rename the helper.\n");

        var request = gate.Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.NotNull(request);
        Assert.Equal(0, gate.CompilerCalls);
        Assert.Null(gate.CompileLineText);
    }
}
