using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>INV-0009 Phase 2 — a reopened session binds without a task class.</b> A session's config
/// carries no task class (the sheet's choice belongs to the run, not the container), so the
/// reopen and restore paths bind the composer with none. That is allowed at bind time and
/// <b>refused by name at send time</b>: a run with a guessed class ranks in the wrong cohort and
/// is indistinguishable from a chosen one afterwards (DC-110; Ruling 70's constraint that the
/// context's task class is the nullable session default).
/// </summary>
/// <remarks>
/// <b>Red first.</b> Before the change the context's task class could not be null at all, so a
/// reopened session could not be bound — the composer stayed configured=0 (INV-0009 §6, A1).
/// </remarks>
public sealed class ASendWithNoTaskClassIsRefusedByNameTests
{
    private static ComposerSendContext Context(string? taskClass) => new(
        RepositoryRoot: "C:/repo",
        DataDirectory: "C:/data",
        AdapterInstallRoot: "C:/adapters",
        EngineId: "claude-code",
        Model: "model",
        AccountLabel: "max-work",
        TaskClass: taskClass,
        ProofPackArtifacts: [],
        Providers: []);

    /// <summary>A complete goal block, so the send is refused for the task class or not at all.</summary>
    private static ComposerDraft CompleteGoalBlock()
    {
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);
        draft.SetGoalValue(GoalBlockFields.GoalKey, "Rename the helper in @src/Payments/Money.cs.");
        draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "@src/Payments/Money.cs compiles with the new name.");
        draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "Any file outside @src/Payments/Money.cs.");
        draft.SetGoalValue(GoalBlockFields.TierKey, "T1");
        draft.SetGoalValue(GoalBlockFields.FanOutCapKey, "0");
        draft.SetGoalValue(GoalBlockFields.BudgetKey, "10,1000");
        return draft;
    }

    [Fact]
    public void Send_WithNoTaskClass_RefusesNamingTheFieldAndSendsNothing()
    {
        var gate = new ComposerSendGate();

        var request = gate.Send(Context(taskClass: null), CompleteGoalBlock(), null, out var refusal);

        Assert.Null(request);
        Assert.NotNull(refusal);
        Assert.Contains("task class", refusal.Message, StringComparison.Ordinal);
        Assert.Equal(0, gate.SendCount);
    }

    [Fact]
    public void Send_WithABlankTaskClass_IsRefusedTheSameWay()
    {
        var gate = new ComposerSendGate();

        var request = gate.Send(Context(taskClass: "   "), CompleteGoalBlock(), null, out var refusal);

        Assert.Null(request);
        Assert.Contains("task class", refusal!.Message, StringComparison.Ordinal);
        Assert.Equal(0, gate.SendCount);
    }

    /// <summary>The companion: with a class the same block sends, so the refusal above is the class's doing.</summary>
    [Fact]
    public void Send_WithATaskClass_SendsAndTheRequestCarriesIt()
    {
        var gate = new ComposerSendGate();

        var request = gate.Send(Context(taskClass: "investigate"), CompleteGoalBlock(), null, out var refusal);

        Assert.Null(refusal);
        Assert.Equal("investigate", request!.TaskClass);
        Assert.Equal(1, gate.SendCount);
    }
}
