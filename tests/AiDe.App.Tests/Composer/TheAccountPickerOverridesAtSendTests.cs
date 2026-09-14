using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.PromptCompilation;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// Ruling 105 condition 1, at the send gate: a Send with a picker override produces a
/// <c>GovernedRunRequest</c> whose <c>AccountLabel</c> is the override — with the engine and model
/// the override's provider binds to — and an <c>account</c> operator row on the envelope; a Send
/// with no override uses the session's default. A non-ready override is refused by name.
/// </summary>
public sealed class TheAccountPickerOverridesAtSendTests
{
    private static ComposerSendContext Context() => new(
        RepositoryRoot: @"C:\repo",
        DataDirectory: @"C:\data",
        AdapterInstallRoot: @"C:\adapter",
        EngineId: "claude-code",
        Model: "sonnet",
        AccountLabel: "max",
        TaskClass: "implement",
        ProofPackArtifacts: [],
        Providers: [],
        Accounts:
        [
            new AccountOption("anthropic", "max", "claude-code", "sonnet", Ready: true, "ready"),
            new AccountOption("openai", "chatgpt", "codex", "gpt-6-astra", Ready: true, "ready"),
            new AccountOption("anthropic", "work", "claude-code", "sonnet", Ready: false, "needs sign-in"),
        ]);

    private static ComposerDraft Draft()
    {
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);
        draft.SetGoalValue(GoalBlockFields.GoalKey, "g");
        draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "d");
        draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "n");
        return draft;
    }

    [Fact]
    public void AnOverrideCarriesTheOverridesAccountEngineAndModel_AndAnOperatorRow()
    {
        var gate = new ComposerSendGate();
        var draft = Draft();
        draft.ChooseAccount("chatgpt");
        gate.RenderView(draft);

        var request = gate.Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.Equal("chatgpt", request!.AccountLabel);
        Assert.Equal("codex", request.EngineId);
        Assert.Equal("gpt-6-astra", request.Model);
        Assert.Equal("chatgpt", gate.LastProjection!.AccountOverride);
    }

    [Fact]
    public void NoOverrideUsesTheSessionsDefault()
    {
        var gate = new ComposerSendGate();
        var draft = Draft();
        gate.RenderView(draft);

        var request = gate.Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.Equal("max", request!.AccountLabel);
        Assert.Equal("claude-code", request.EngineId);
        Assert.Equal("sonnet", request.Model);
        Assert.Null(gate.LastProjection!.AccountOverride);
    }

    [Fact]
    public void ANonReadyOrUnknownOverrideIsRefusedByName()
    {
        var gate = new ComposerSendGate();
        var draft = Draft();
        draft.ChooseAccount("work");
        gate.RenderView(draft);
        Assert.Null(gate.Send(Context(), draft, null, out var refusal));
        Assert.Contains("'work' is needs sign-in", refusal!.Message, StringComparison.Ordinal);
        Assert.Equal(0, gate.SendCount);

        draft.ChooseAccount("nobody");
        gate.RenderView(draft);
        Assert.Null(gate.Send(Context(), draft, null, out refusal));
        Assert.Contains("'nobody' is not one of this session's accounts", refusal!.Message, StringComparison.Ordinal);
    }
}
