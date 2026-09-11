using System.Text;
using AiDe.Core.AgentPlane;
using AiDe.Core.Sessions;

namespace AiDe.Core.Presentation.Composer;

/// <summary>The text the conductor will receive, and the attachment totals that ride with it.</summary>
/// <param name="Text">Exactly the bytes the run host is handed. The view renders this, not a summary.</param>
/// <param name="AttachmentCount">How many attachments are inside <paramref name="Text"/>.</param>
/// <param name="AttachmentBytes">Their total source byte count.</param>
/// <param name="OutsideWorkspaceCount">How many came from outside the workspace root.</param>
public sealed record CompiledPrompt(
    string Text, int AttachmentCount, int AttachmentBytes, int OutsideWorkspaceCount)
{
    /// <summary>How many came from inside the workspace root.</summary>
    public int InsideWorkspaceCount => AttachmentCount - OutsideWorkspaceCount;
}

/// <summary>
/// Compiles a draft to the prompt text — and nothing else happens between here and the send.
/// </summary>
/// <remarks>
/// <para><b>This type is where Security C15 is enforceable, because it is total and pure.</b> It
/// reads the draft and the template it is given. It opens no file, queries no graph, expands no
/// mention, and makes no network call — so "the compiled view and the sent text differ by a byte"
/// says what it appears to say. Without that, the human approves a mention and the agent receives a
/// file, while the byte check stays green because both sides hold the unresolved text.</para>
///
/// <para><b>Mentions stay literal on purpose.</b> A mention is characters in the draft. It is never
/// resolved to a path or to a file's contents anywhere in this slice; expansion is R20, and
/// Ruling 44's acceptance is void the moment it exists.</para>
///
/// <para><b>Deterministic.</b> The goal block renders its six fields in the order §14.3 lists them,
/// a template renders through <see cref="TemplateCompiler"/>, attachments render in affirmation
/// order, and nothing reads a clock, a culture or the environment.</para>
/// </remarks>
public static class ComposerCompiler
{
    /// <summary>The fence info word an attachment block carries, so a reader can see what it is.</summary>
    public const string AttachmentFenceTag = "aide-attachment";

    /// <summary>Compiles the draft. <paramref name="template"/> is required only for a template draft.</summary>
    public static CompiledPrompt Compile(ComposerDraft draft, PromptTemplate? template = null)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var body = draft.Shape switch
        {
            ComposerShape.FreeForm => draft.FreeFormText,
            ComposerShape.GoalBlock => RenderGoalBlock(draft.ToGoalBlock()),
            ComposerShape.Template => RenderTemplate(draft, template),
            _ => throw new ArgumentOutOfRangeException(nameof(draft), draft.Shape, "unknown composer shape"),
        };

        var text = new StringBuilder(body);
        foreach (var attachment in draft.Attachments)
        {
            if (text.Length > 0 && text[^1] != '\n')
            {
                text.Append('\n');
            }

            text.Append('\n').Append(RenderAttachment(attachment));
        }

        return new CompiledPrompt(
            text.ToString(),
            draft.Attachments.Count,
            draft.Attachments.Sum(a => a.Bytes),
            draft.Attachments.Count(a => a.IsOutsideWorkspace));
    }

    /// <summary>
    /// The goal block as prompt text, in the order §14.3 lists the fields.
    /// </summary>
    /// <remarks>
    /// The headings are the <b>wire names</b>, so the field-level error a person sees and the prompt
    /// the engine reads use one vocabulary. <c>fan_out_cap</c> and <c>budget</c> are rendered as what
    /// they are — declarations — because Ruling 26c holds: Phase 1 validates them and enforces
    /// neither, and a prompt that reads as though they were enforced would be the first place that
    /// claim was made.
    /// </remarks>
    public static string RenderGoalBlock(GoalBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);

        var text = new StringBuilder();
        Section(GoalBlockFields.GoalKey, block.Goal);
        Section(GoalBlockFields.DoneWhenKey, block.DoneWhen);
        Section(GoalBlockFields.NotInScopeKey, block.NotInScope);
        Section(GoalBlockFields.TierKey, block.Tier);
        Section(GoalBlockFields.FanOutCapKey, block.FanOutCap?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Section(
            GoalBlockFields.BudgetKey,
            block.Budget is { } budget
                ? string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"requests: {budget.Requests}, tokens: {budget.Tokens} (declared, not enforced in Phase 1)")
                : null);

        return text.ToString();

        void Section(string name, string? value)
        {
            text.Append("## ").Append(name).Append('\n').Append('\n')
                .Append(value ?? string.Empty).Append('\n').Append('\n');
        }
    }

    private static string RenderTemplate(ComposerDraft draft, PromptTemplate? template)
    {
        if (template is null)
        {
            throw new ArgumentNullException(
                nameof(template),
                "a template draft cannot compile without the template it names; a compiler that "
                + "substituted a blank here would put an empty prompt on the wire");
        }

        return TemplateCompiler.Compile(template, draft.TemplateValues);
    }

    /// <summary>
    /// One attachment as a visible fenced block whose header names the source and its byte count.
    /// </summary>
    /// <remarks>
    /// <para><b>The fence is widened to clear the content</b> — a file containing a fence of its own
    /// would otherwise end the block early and leave the rest of the file rendering as prose, which
    /// is the same defect class as a truncation with a better disguise.</para>
    ///
    /// <para><b>The outside-workspace label is part of the header, and the header is the record of a
    /// decision already taken</b> — the control that bites is the affirmation at the pick
    /// (C14(e)(iii)). Both exist because the label alone is read only by the person who already
    /// decided.</para>
    /// </remarks>
    public static string RenderAttachment(ComposerAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        var fence = new string('`', LongestBacktickRun(attachment.Text) + 1);
        var location = attachment.IsOutsideWorkspace
            ? "OUTSIDE the workspace root"
            : "inside the workspace root";

        var header = string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{fence}{AttachmentFenceTag} source={attachment.DisplayPath} bytes={attachment.Bytes} location={location}");

        var body = attachment.Text.EndsWith('\n') ? attachment.Text : attachment.Text + "\n";
        return header + "\n" + body + fence + "\n";
    }

    private static int LongestBacktickRun(string text)
    {
        var longest = 2;
        var run = 0;
        foreach (var c in text)
        {
            run = c == '`' ? run + 1 : 0;
            if (run > longest)
            {
                longest = run;
            }
        }

        return longest;
    }
}
