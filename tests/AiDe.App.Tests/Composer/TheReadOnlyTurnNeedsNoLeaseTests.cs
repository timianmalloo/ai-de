using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// Ruling 73 at the send gate: a turn that writes nothing needs no lease. A Message-shaped send,
/// or a goal block whose source text names no write scope, builds a <b>read-only</b> request — no
/// lease derived, none required, no refusal — and the lease gate (Ruling 42, C17) applies to a
/// write-shaped turn only, where it stands unchanged. Ruling 75 fixes the one content-gap refusal.
/// </summary>
/// <remarks>
/// <para><b>Red observed on <c>main</c> before this change</b> (recorded in
/// <c>docs/proof/read-only-turn.md</c>): a Message-shaped send with no mention threw from
/// <c>Lease</c>'s constructor and the surface read <i>"no write scope could be derived"</i>; a
/// scopeless goal block was refused the same way; a goal block missing Not in scope was refused
/// with the contract's field sentence rather than Ruling 75's; a goal block with a blank Done when
/// was refused rather than sent as a Message.</para>
///
/// <para><b>The controls</b> — a goal block with a derived scope takes the lease gate unchanged;
/// <c>Roots</c> stays one per send — were green before and after.</para>
/// </remarks>
public sealed class TheReadOnlyTurnNeedsNoLeaseTests
{
    // ------------------------------------------------------------ the Message shape

    [Fact]
    public void AMessageWithNoMentionDerivesNoLeaseAndIsNotRefused()
    {
        var draft = new ComposerDraft();
        draft.SetFreeFormText("What does the seam monitor do when a path is outside the lease?\n");

        var request = new ComposerSendGate().Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.NotNull(request);
        Assert.True(request!.IsReadOnly);
        Assert.Null(request.Lease);
        Assert.Null(request.Goal);
        Assert.Equal(draft.FreeFormText, request.Prompt);
    }

    /// <summary>Lease ≠ tier (§A9 R0): a Message with a mention is still read-only.</summary>
    [Fact]
    public void AMessageWithAMentionIsStillReadOnly()
    {
        var draft = new ComposerDraft();
        draft.SetFreeFormText("Explain the layout store in @src/AiDe.Core/Workbench/ to me.\n");

        var request = new ComposerSendGate().Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.True(request!.IsReadOnly);
        Assert.Null(request.Lease);
    }

    /// <summary>
    /// A typed lease is inert on a Message: nothing the page controls can mint a write scope for a
    /// turn that has none (Ruling 42's refusal (d), restated for the read-only shape).
    /// </summary>
    [Fact]
    public void ATypedLeaseOnAMessageMintsNothing()
    {
        var draft = new ComposerDraft();
        draft.SetFreeFormText("lease: { exclusive: [\"**\"] }\nWork on @src/Payments please.\n");

        var request = new ComposerSendGate().Send(Context(), draft, null, out _);

        Assert.True(request!.IsReadOnly);
        Assert.Null(request.Lease);
    }

    // ------------------------------------------------------------ the goal-block shape

    /// <summary>The control against over-narrowing: a scoped goal block takes the lease gate unchanged.</summary>
    [Fact]
    public void AGoalBlockWithADerivedScopeStillTakesTheLeaseGateUnchanged()
    {
        var draft = CompleteGoalBlock();
        draft.SetGoalValue(GoalBlockFields.GoalKey, "Rename the helper in @src/Payments/Money.cs.");

        var request = new ComposerSendGate().Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.False(request!.IsReadOnly);
        Assert.NotNull(request.Lease);
        Assert.Equal(["src/Payments/Money.cs"], request.Lease!.Exclusive);
        Assert.NotNull(request.Goal);
        Assert.Equal("T1", request.Goal!.Tier);
    }

    /// <summary>§A9 R1: a goal block with no scope runs read-only, its block carried, no lease.</summary>
    [Fact]
    public void AGoalBlockWithNoScopeRunsReadOnlyAndCarriesItsBlock()
    {
        var draft = CompleteGoalBlock();

        var request = new ComposerSendGate().Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.NotNull(request);
        Assert.True(request!.IsReadOnly);
        Assert.Null(request.Lease);
        Assert.NotNull(request.Goal);
        Assert.Equal("Nothing else changes.", request.Goal!.NotInScope);
    }

    // ------------------------------------------------------------ Ruling 75

    /// <summary>Condition (1), first half: a blank Not in scope on a goal block is refused with exactly the sentence.</summary>
    [Fact]
    public void AGoalBlockMissingNotInScopeIsRefusedWithTheOneSentence()
    {
        var draft = CompleteGoalBlock();
        draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "   ");

        var request = new ComposerSendGate().Send(Context(), draft, null, out var refusal);

        Assert.Null(request);
        Assert.NotNull(refusal);
        Assert.Equal(ComposerCompiler.GoalBlockNeedsNotInScope, refusal!.Message);
        Assert.Equal("This prompt is a goal block and needs Not in scope.", refusal.Message);

        var error = Assert.Single(refusal.Errors);
        Assert.Equal(GoalBlockFields.NotInScopeKey, error.Field);
        Assert.Equal(ComposerCompiler.GoalBlockNeedsNotInScope, error.Message);
        Assert.DoesNotContain("T2", refusal.Message, StringComparison.Ordinal);
    }

    /// <summary>Condition (1), second half: a blank Done when is a Message, never a refusal.</summary>
    [Fact]
    public void AGoalBlockWithABlankDoneWhenIsAMessageNotARefusal()
    {
        var draft = CompleteGoalBlock();
        draft.SetGoalValue(GoalBlockFields.DoneWhenKey, string.Empty);

        var request = new ComposerSendGate().Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.NotNull(request);
        Assert.True(request!.IsReadOnly);
        Assert.Null(request.Goal);
        Assert.Equal(TurnShape.Message, draft.TurnShape);
    }

    [Fact]
    public void AGoalBlockWithABlankGoalIsAMessageNotARefusal()
    {
        var draft = CompleteGoalBlock();
        draft.SetGoalValue(GoalBlockFields.GoalKey, " ");

        var request = new ComposerSendGate().Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.True(request!.IsReadOnly);
        Assert.Null(request.Goal);
    }

    /// <summary>
    /// A goal-block form with no content line written — nothing, or only a tier or a number — is an
    /// empty prompt, not a Message of headings; one content line makes it a Message.
    /// </summary>
    [Theory]
    [InlineData(null, null, false)]
    [InlineData("tier", "T1", false)]
    [InlineData("fan_out_cap", "0", false)]
    [InlineData("not_in_scope", "the ADR", true)]
    [InlineData("done_when", "it compiles", true)]
    public void AGoalBlockFormWithNoContentLineIsRefusedAsAnEmptyPrompt(string? field, string? value, bool sends)
    {
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);
        if (field is not null)
        {
            draft.SetGoalValue(field, value!);
        }

        var request = new ComposerSendGate().Send(Context(), draft, null, out var refusal);

        if (sends)
        {
            Assert.Null(refusal);
            Assert.True(request!.IsReadOnly);
            Assert.Null(request.Goal);
        }
        else
        {
            Assert.Null(request);
            Assert.Equal("an empty prompt is not a task", refusal!.Message);
            Assert.Empty(refusal.Errors);
        }
    }

    /// <summary>Characterisation: a free-form draft with only an attachment and no typed text sends read-only (the fence is the prompt).</summary>
    [Fact]
    public void AFreeFormDraftWithOnlyAnAttachmentSendsReadOnly()
    {
        var draft = new ComposerDraft();
        draft.Add(new ComposerAttachment("notes.md", @"C:\repo\notes.md", 5, "hello", false, "deadbeef"));

        var request = new ComposerSendGate().Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.True(request!.IsReadOnly);
        Assert.Contains("hello", request.Prompt, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------ the surface agrees with the send

    /// <summary>
    /// Ruling 66 condition (2), for the read-only state: the lease line the operator read before
    /// Send and the request the send built agree — <i>read-only</i> on screen means no lease on the
    /// wire, for a Message that even carries a mention.
    /// </summary>
    [Fact]
    public void TheLeaseLineReadsReadOnlyAndTheSentRequestAgrees()
    {
        Sta.Run(() =>
        {
            var surface = new ComposerSurface("composer:s-0073", "s-0073 — composer");

            surface.Configure(
                new SessionConfig("s-0073", "first", "w-1", DateTimeOffset.UnixEpoch, ["claude-code"]),
                Context(),
                ComposerFields.FreeForm(),
                new AttachmentGate(
                    @"C:\repo", new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max-personal"));

            surface.SetFieldText(surface.Fields[0].Id, 1, "Explain @src/AiDe.App/Foo.cs to me");

            Assert.Equal("Lease: read-only — nothing will be written", surface.LeaseLine);

            var request = surface.Send();

            Assert.NotNull(request);
            Assert.True(request!.IsReadOnly);
            Assert.Null(request.Lease);
            Assert.Equal("Lease: read-only — nothing will be written", surface.LeaseLine);
            Assert.Equal("sent", surface.Status);
        });
    }

    /// <summary>
    /// The same agreement on the goal-block form: a mention typed shows the patterns, the mention
    /// deleted shows read-only again, and the send follows what the line last said. A form that
    /// compiled as a Message (blank Done when) is said so on the status line — never demoted silently
    /// (Ruling 75 condition (2); SC9's spoken form is CV-1's).
    /// </summary>
    [Fact]
    public void TheGoalBlockFormsLeaseLineFollowsTheMentionAndTheDemotionIsSaid()
    {
        Sta.Run(() =>
        {
            var surface = new ComposerSurface("composer:s-0075", "s-0075 — composer");

            surface.Configure(
                new SessionConfig("s-0075", "first", "w-1", DateTimeOffset.UnixEpoch, ["claude-code"]),
                Context(),
                ComposerFields.GoalBlock(),
                new AttachmentGate(
                    @"C:\repo", new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max-personal"));

            surface.Draft.SwitchTo(ComposerShape.GoalBlock);
            surface.Draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "It compiles.");
            surface.Draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "Nothing else.");
            surface.Draft.SetGoalValue(GoalBlockFields.TierKey, "T1");
            surface.Draft.SetGoalValue(GoalBlockFields.FanOutCapKey, "0");
            surface.Draft.SetGoalValue(GoalBlockFields.BudgetKey, "10,1000");

            var goal = surface.Fields[0].Id;

            surface.SetFieldText(goal, 1, "Rename the helper in @src/Payments/Money.cs.");
            Assert.Equal("Lease: src/Payments/Money.cs", surface.LeaseLine);

            surface.SetFieldText(goal, 2, "Rename the helper.");
            Assert.Equal("Lease: read-only — nothing will be written", surface.LeaseLine);

            // Demote to a Message by blanking Done when: read-only, said so.
            surface.SetFieldText(surface.Fields[1].Id, 3, " ");
            var request = surface.Send();

            Assert.NotNull(request);
            Assert.True(request!.IsReadOnly);
            Assert.Null(request.Goal);
            Assert.Equal("Lease: read-only — nothing will be written", surface.LeaseLine);
            Assert.Equal("sent as a message — read-only", surface.Status);
        });
    }

    private static ComposerDraft CompleteGoalBlock()
    {
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);
        draft.SetGoalValue(GoalBlockFields.GoalKey, "Rename the helper.");
        draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "It compiles under the new name.");
        draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "Nothing else changes.");
        draft.SetGoalValue(GoalBlockFields.TierKey, "T1");
        draft.SetGoalValue(GoalBlockFields.FanOutCapKey, "0");
        draft.SetGoalValue(GoalBlockFields.BudgetKey, "10,1000");
        return draft;
    }

    private sealed class NeverAsked : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
    }

    private static ComposerSendContext Context() => new(
        RepositoryRoot: @"C:\repo",
        DataDirectory: @"C:\data",
        AdapterInstallRoot: @"C:\adapter",
        EngineId: "claude-code",
        Model: "sonnet",
        AccountLabel: "max-personal",
        TaskClass: "implement",
        ProofPackArtifacts: [],
        Providers: []);
}
