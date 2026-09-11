using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Sessions;

namespace AiDe.Core.Tests.Composer;

/// <summary>
/// R15 and R19's composition clauses: one goal-block validation mechanism (Ruling 26b), the field
/// widget inventory (Ruling 33), per-shape draft retention (Ruling 26 cut iv), R15 b2's re-based
/// round trip (Ruling 34), the S1 free-form guard, and the deterministic compile.
/// </summary>
public sealed class TheComposerIsOneValidationMechanismTests
{
    private static GoalBlock Block(
        string? goal = "g", string? doneWhen = "d", string? notInScope = "n", string? tier = "T1",
        int? cap = 0, RunBudget? budget = null) =>
        new(goal, doneWhen, notInScope, tier, cap, budget ?? new RunBudget(10, 1000));

    private static ComposerDraft GoalDraft(GoalBlock block)
    {
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);

        Set(GoalBlockFields.GoalKey, block.Goal);
        Set(GoalBlockFields.DoneWhenKey, block.DoneWhen);
        Set(GoalBlockFields.NotInScopeKey, block.NotInScope);
        Set(GoalBlockFields.TierKey, block.Tier);
        Set(GoalBlockFields.FanOutCapKey, block.FanOutCap?.ToString());
        Set(GoalBlockFields.BudgetKey, block.Budget is { } b ? $"{b.Requests},{b.Tokens}" : null);

        return draft;

        void Set(string field, string? value)
        {
            if (value is not null)
            {
                draft.SetGoalValue(field, value);
            }
        }
    }

    [Fact]
    public void Ruling26b_TheFormEngineAndTheSpawnContractNameOneFieldSetForEveryInput()
    {
        // Every combination of present/absent across the six fields, plus the two invalid-value
        // cases. If a second definition of goal-block validity existed anywhere, it would disagree on
        // one of these 66 inputs and nothing else would notice.
        var inputs = new List<GoalBlock>();

        for (var mask = 0; mask < 64; mask++)
        {
            inputs.Add(new GoalBlock(
                (mask & 1) != 0 ? "g" : null,
                (mask & 2) != 0 ? "d" : null,
                (mask & 4) != 0 ? "n" : null,
                (mask & 8) != 0 ? "T1" : null,
                (mask & 16) != 0 ? 0 : null,
                (mask & 32) != 0 ? new RunBudget(10, 1000) : null));
        }

        inputs.Add(Block(cap: -1));
        inputs.Add(Block(budget: new RunBudget(0, 0)));

        foreach (var block in inputs)
        {
            var contract = SpawnContract.Validate(block).Select(e => e.Field).Order(StringComparer.Ordinal);
            var form = ComposerFormEngine.Validate(GoalDraft(block)).Select(e => e.Field).Order(StringComparer.Ordinal);

            Assert.Equal(contract, form);
        }
    }

    [Fact]
    public void Ruling26b_TheSpawnContractsOwnFourTestsAreUntouchedAndTheSixNamesStillHold()
    {
        // The composer re-bases onto the contract; it does not renegotiate it. The six names and the
        // absence of a lease among them are what the lease clause turns on.
        Assert.Equal(6, GoalBlockFields.All.Count);
        Assert.Equal(
            ["goal", "done_when", "not_in_scope", "tier", "fan_out_cap", "budget"],
            GoalBlockFields.All);
        Assert.DoesNotContain(GoalBlockFields.All, f => f.Contains("lease", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Ruling26c_TheGoalBlocksHintsRenderAsDeclaredRatherThanEnforced()
    {
        var text = ComposerCompiler.RenderGoalBlock(Block());

        Assert.Contains("declared, not enforced in Phase 1", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("goal", ComposerFieldWidget.LongText)]
    [InlineData("done_when", ComposerFieldWidget.LongText)]
    [InlineData("not_in_scope", ComposerFieldWidget.LongText)]
    [InlineData("tier", ComposerFieldWidget.Enum)]
    [InlineData("fan_out_cap", ComposerFieldWidget.Text)]
    [InlineData("budget", ComposerFieldWidget.Budget)]
    public void Ruling33_TheGoalBlockFieldsTakeTheWidgetsTheInventoryNames(string field, ComposerFieldWidget expected)
    {
        Assert.Equal(expected, ComposerFieldWidgets.ForGoalBlockField(field));
    }

    [Fact]
    public void Ruling33_OnlyMentionsAndLongTextGetAnEditorInstance()
    {
        Assert.True(ComposerFieldWidgets.RendersInEditorView(ComposerFieldWidget.Mentions));
        Assert.True(ComposerFieldWidgets.RendersInEditorView(ComposerFieldWidget.LongText));

        foreach (var native in new[]
                 {
                     ComposerFieldWidget.Text, ComposerFieldWidget.List,
                     ComposerFieldWidget.Enum, ComposerFieldWidget.Budget,
                 })
        {
            Assert.False(
                ComposerFieldWidgets.RendersInEditorView(native),
                $"a per-field editor was created for the native widget {native}");
        }
    }

    [Fact]
    public void Ruling33_ATemplatesDeclaredFieldsMapOntoTheSameInventory()
    {
        Assert.Equal(
            ComposerFieldWidget.Mentions,
            ComposerFieldWidgets.ForTemplateField(new TemplateField("who", TemplateFieldType.Mentions, true, null, null)));
        Assert.Equal(
            ComposerFieldWidget.List,
            ComposerFieldWidgets.ForTemplateField(new TemplateField("steps", TemplateFieldType.List, true, null, 2)));
        Assert.Equal(
            ComposerFieldWidget.Text,
            ComposerFieldWidgets.ForTemplateField(new TemplateField("title", TemplateFieldType.Text, true, null, null)));
    }

    [Fact]
    public void S1_FreeFormSendsWithNoTemplateCodeAnywhereInThePath()
    {
        var draft = new ComposerDraft();
        draft.SetFreeFormText("just a prompt\n");

        // No template is bound, none is passed, and validation does not consult one.
        Assert.Empty(ComposerFormEngine.Validate(draft));
        Assert.Null(draft.TemplateId);
        Assert.Equal("just a prompt\n", ComposerCompiler.Compile(draft).Text);
    }

    [Fact]
    public void Ruling26cutIV_SwitchingShapePreservesEachShapesOwnContent()
    {
        var draft = new ComposerDraft();
        draft.SetFreeFormText("the original free-form draft\n");

        draft.SwitchTo(ComposerShape.GoalBlock);
        draft.SetGoalValue(GoalBlockFields.GoalKey, "move the aggregate");

        draft.SwitchTo(ComposerShape.FreeForm, ComposerCompiler.RenderGoalBlock(draft.ToGoalBlock()));

        // BYTE-IDENTICAL. Retention outranks seeding: a seed is what an empty shape starts from,
        // never what a written one is overwritten with.
        Assert.Equal("the original free-form draft\n", draft.FreeFormText);

        draft.SwitchTo(ComposerShape.GoalBlock);
        Assert.Equal("move the aggregate", draft.GoalValues[GoalBlockFields.GoalKey]);
    }

    [Fact]
    public void Ruling34_GoalBlockToFreeFormSeedsTheCompiledTextWhenFreeFormHoldsNothingYet()
    {
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);
        draft.SetGoalValue(GoalBlockFields.GoalKey, "move the aggregate");

        var compiled = ComposerCompiler.RenderGoalBlock(draft.ToGoalBlock());
        draft.SwitchTo(ComposerShape.FreeForm, compiled);

        Assert.Equal(compiled, draft.FreeFormText);
        Assert.Contains("move the aggregate", draft.FreeFormText, StringComparison.Ordinal);
    }

    [Fact]
    public void Ruling34_ARoundTripLosesNoFieldAndCallsNoAssist()
    {
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);

        foreach (var field in GoalBlockFields.All)
        {
            draft.SetGoalValue(field, field == GoalBlockFields.BudgetKey ? "10,1000" : $"value of {field}");
        }

        var before = draft.GoalValues.ToDictionary(StringComparer.Ordinal);

        draft.SwitchTo(ComposerShape.FreeForm, ComposerCompiler.RenderGoalBlock(draft.ToGoalBlock()));
        draft.SwitchTo(ComposerShape.Template);
        draft.SwitchTo(ComposerShape.GoalBlock);

        Assert.Equal(before, draft.GoalValues);
    }

    [Fact]
    public void TheCompileIsDeterministicAcrossRepeatedRuns()
    {
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);

        // Set in one order; the renderer walks the TEMPLATE's declared order, not the caller's.
        foreach (var field in GoalBlockFields.All.Reverse())
        {
            draft.SetGoalValue(field, field == GoalBlockFields.BudgetKey ? "10,1000" : $"v-{field}");
        }

        var first = ComposerCompiler.Compile(draft).Text;
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(first, ComposerCompiler.Compile(draft).Text);
        }

        Assert.StartsWith("## goal", first, StringComparison.Ordinal);
    }

    [Fact]
    public void ATemplateFormBlocksSendWithAFieldLevelErrorWhenARequiredFieldIsEmpty()
    {
        var template = new PromptTemplate(
            "launch", 1, "intent", "audience", "when", "why", "T1",
            [
                new TemplateField("title", TemplateFieldType.Text, Required: true, null, null),
                new TemplateField("steps", TemplateFieldType.List, Required: true, null, Min: 2),
            ],
            "# {{title}}\n{{#steps}}- {{.}}{{/steps}}\n",
            new Dictionary<string, string>());

        var draft = new ComposerDraft();
        draft.UseTemplate("launch");

        var errors = ComposerFormEngine.Validate(draft, template);
        Assert.Equal(["title", "steps"], errors.Select(e => e.Field).Order(StringComparer.Ordinal).Reverse());

        draft.SetTemplateValue("title", ["Ship it"]);
        draft.SetTemplateValue("steps", ["one"]);

        var minError = Assert.Single(ComposerFormEngine.Validate(draft, template));
        Assert.Equal("steps", minError.Field);
        Assert.Contains("minimum of 2", minError.Message, StringComparison.Ordinal);

        draft.SetTemplateValue("steps", ["one", "two"]);
        Assert.Empty(ComposerFormEngine.Validate(draft, template));
        Assert.Equal("# Ship it\n- one\n- two\n", ComposerCompiler.Compile(draft, template).Text);
    }

    [Fact]
    public void TheDraftSurvivesARestartThroughTheStoreRatherThanAReInstantiatedModel()
    {
        var root = Path.Combine(Path.GetTempPath(), "aide-draft", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var draft = new ComposerDraft();
            draft.SetFreeFormText("a draft that must outlive the process\n");
            draft.SwitchTo(ComposerShape.GoalBlock);
            draft.SetGoalValue(GoalBlockFields.GoalKey, "survive a restart");

            new ComposerDraftStore(root).Save("s-0001", draft);

            // A NEW store instance over the same bytes, which is what a restart is.
            var restored = new ComposerDraftStore(root).Load("s-0001");

            Assert.NotNull(restored);
            Assert.Equal("a draft that must outlive the process\n", restored!.FreeFormText);
            Assert.Equal("survive a restart", restored.GoalValues[GoalBlockFields.GoalKey]);
            Assert.Equal(ComposerShape.GoalBlock, restored.Shape);

            // ONE-WAY. Mutating what came back does not reach the stored bytes, and nothing exposes a
            // reverse path: the store's Load returns a fresh instance every time.
            restored.SetFreeFormText("mutated downstream\n");
            var again = new ComposerDraftStore(root).Load("s-0001");
            Assert.Equal("a draft that must outlive the process\n", again!.FreeFormText);
            Assert.NotSame(restored, again);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TheDraftStoreWritesInsideTheIgnoredSidecarAndHoldsNoAttachmentContent()
    {
        using var fixture = new AttachmentFixture();
        var draft = new ComposerDraft();
        draft.SetFreeFormText("prompt\n");

        fixture.Gate().Offer(
            draft, [fixture.WriteInside("secret-note.md", "ATTACHMENT-BODY-MARKER\n")], attachEnabled: true);

        var store = new ComposerDraftStore(fixture.WorkspaceRoot);
        store.Save("s-0001", draft);

        Assert.Equal(
            Path.Combine(fixture.WorkspaceRoot, ".aide", "composer-drafts.json"),
            store.File);

        var persisted = File.ReadAllText(store.File);
        Assert.DoesNotContain("ATTACHMENT-BODY-MARKER", persisted, StringComparison.Ordinal);
    }
}
