using System.Collections.Generic;
using System.Linq;
using AiDe.Core.AgentPlane;
using AiDe.Core.Sessions;

namespace AiDe.Core.Tests.Sessions;

/// <summary>
/// FT clause 7 — the <c>goal-block</c> template's fields ARE <see cref="GoalBlockFields"/>' six
/// constants, and its hints do not read as enforced.
/// </summary>
/// <remarks>
/// <para><b>Nothing renames on the wire.</b> R18 re-bases Addendum A's goal-block shape as this
/// template with zero behaviour change; a template that spelled a field differently would give the
/// send gate and <see cref="SpawnContract.Validate"/> two vocabularies for one thing.</para>
///
/// <para><b>Validated, not enforced</b> — <c>GoalBlock.cs</c>'s own remark carries through
/// (Ruling 26 condition c). Phase 1 checks <c>fan_out_cap</c> and <c>budget</c> are present and
/// sane; nothing counts sub-lanes, requests or tokens against them.</para>
/// </remarks>
public sealed class GoalBlockTemplateTests
{
    private static PromptTemplate Template => TemplateCatalog.BuiltIn().Find("goal-block")!.Template!;

    [Fact]
    public void TheTemplatesFieldsAreTheSameSetAsGoalBlockFields()
    {
        Assert.Equal(
            GoalBlockFields.All.Order(StringComparer.Ordinal),
            Template.Fields.Select(f => f.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void TheTemplatesFieldsAreInTheOrderSpec143ListsThem()
    {
        Assert.Equal(GoalBlockFields.All, Template.Fields.Select(f => f.Name));
    }

    [Fact]
    public void TheFanOutCapAndBudgetHintsSayValidatedNotEnforced()
    {
        foreach (var name in new[] { GoalBlockFields.FanOutCapKey, GoalBlockFields.BudgetKey })
        {
            var hint = Template.Fields.Single(f => f.Name == name).Hint;

            Assert.NotNull(hint);
            Assert.Contains("not enforced", hint!, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void NoFieldHintClaimsAnythingIsEnforced()
    {
        foreach (var field in Template.Fields.Where(f => f.Hint is not null))
        {
            Assert.DoesNotContain("enforces", field.Hint!, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("enforced", field.Hint!.Replace("not enforced", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CompilingTheTemplateProducesTheSixWireNamesTheValidatorUses()
    {
        var values = GoalBlockFields.All.ToDictionary(
            name => name,
            name => (IReadOnlyList<string>)[$"a {name}"],
            StringComparer.Ordinal);

        var text = TemplateCompiler.Compile(Template, values);

        Assert.All(GoalBlockFields.All, name => Assert.Contains($"{name}: a {name}", text, StringComparison.Ordinal));
    }
}
