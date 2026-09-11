using AiDe.Core.AgentPlane;

namespace AiDe.Core.Presentation.Composer;

/// <summary>The three shapes one composer block can take (R19 bullet 3).</summary>
public enum ComposerShape
{
    /// <summary>Prose. The default, and the shape that works with no template anywhere in the path.</summary>
    FreeForm,

    /// <summary>The six goal-block fields, validated by the one mechanism (Ruling 26b).</summary>
    GoalBlock,

    /// <summary>A catalog template, rendered as a validated form.</summary>
    Template,
}

/// <summary>One attachment, as it will appear in the prompt and nowhere else.</summary>
/// <param name="DisplayPath">
/// What the fence header names: a repository-relative path for an inside-workspace file, the
/// absolute path for an outside-workspace one. The operator read this before affirming it.
/// </param>
/// <param name="ResolvedPath">The fully resolved real path, after symlinks and junctions (C14(e)(ii)).</param>
/// <param name="Bytes">The byte count. It is what makes the human's read informed (Ruling 43).</param>
/// <param name="Text">The decoded text, held literally — never a reference, never re-read at send.</param>
/// <param name="IsOutsideWorkspace">Computed from <paramref name="ResolvedPath"/>, never from the pick.</param>
/// <param name="Sha256">
/// The content hash. Channel B only — the committed channel may never carry it for an
/// outside-workspace file, because a hash of a cloned file is a confirmable fingerprint.
/// </param>
public sealed record ComposerAttachment(
    string DisplayPath,
    string ResolvedPath,
    int Bytes,
    string Text,
    bool IsOutsideWorkspace,
    string Sha256);

/// <summary>
/// One composer block's draft: its shape, the content of every shape it has held, and its
/// attachments.
/// </summary>
/// <remarks>
/// <para><b>Shape switching preserves content by per-shape draft retention, not by transformation</b>
/// (Ruling 26 cut iv). Each shape keeps its own state; switching moves a cursor. The transform is
/// R20 and is not built here, so no assist is reachable from this type — there is nothing to call.
/// </para>
///
/// <para><b>The one seeded direction, and why it is not a transform.</b> Going to free-form from a
/// goal block when free-form holds <i>nothing yet</i> seeds it with the goal block's compiled text
/// (R15 b2's non-assist residue). Going back returns the retained free-form draft byte-identical,
/// because retention outranks seeding: a seed is what an empty shape starts from, never what a
/// written one is overwritten with.</para>
/// </remarks>
public sealed class ComposerDraft
{
    private readonly Dictionary<string, string> _goalValues = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<string>> _templateValues = new(StringComparer.Ordinal);
    private readonly List<ComposerAttachment> _attachments = [];

    private string _freeForm = string.Empty;
    private bool _freeFormWritten;

    /// <summary>The active shape. Free-form is the default, and needs no template anywhere.</summary>
    public ComposerShape Shape { get; private set; } = ComposerShape.FreeForm;

    /// <summary>
    /// The editor's own held source text — what the operator typed, and nothing else (Ruling 66).
    /// </summary>
    /// <remarks>
    /// <para><b>Why this exists.</b> <see cref="ComposerCompiler.Compile"/>'s output additionally
    /// carries an attachment's file content and, for a template draft, the template's own fixed
    /// prose — neither of which the operator wrote. <see cref="LeaseDerivation"/> must read only what
    /// the operator authored, so it is derived from this, never from the compiled prompt.</para>
    ///
    /// <para><b>Shape-scoped, not shape-summed.</b> A draft retains every shape's content at once
    /// (see the class remarks), but only the <i>active</i> shape's content is what the operator is
    /// currently looking at and editing — a mention left behind in a shape the operator switched away
    /// from must not silently widen the lane's write scope.</para>
    /// </remarks>
    public string SourceText => Shape switch
    {
        ComposerShape.FreeForm => _freeForm,
        ComposerShape.GoalBlock => string.Join(
            '\n', GoalBlockFields.All.Select(name => _goalValues.TryGetValue(name, out var value) ? value : string.Empty)),
        ComposerShape.Template => string.Join('\n', _templateValues.Values.SelectMany(values => values)),
        _ => throw new ArgumentOutOfRangeException(nameof(Shape), Shape, "unknown composer shape"),
    };

    /// <summary>The retained free-form text.</summary>
    public string FreeFormText => _freeForm;

    /// <summary>The template this draft is bound to, when its shape is a template.</summary>
    public string? TemplateId { get; private set; }

    /// <summary>The goal-block field values, by wire name.</summary>
    public IReadOnlyDictionary<string, string> GoalValues => _goalValues;

    /// <summary>The template field values, by field name.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> TemplateValues => _templateValues;

    /// <summary>Everything attached to this send, in the order it was affirmed.</summary>
    public IReadOnlyList<ComposerAttachment> Attachments => _attachments;

    /// <summary>Replaces the free-form text.</summary>
    public void SetFreeFormText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _freeForm = text;
        _freeFormWritten = true;
    }

    /// <summary>Sets one goal-block field by its wire name.</summary>
    public void SetGoalValue(string fieldName, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        ArgumentNullException.ThrowIfNull(value);

        if (!GoalBlockFields.All.Contains(fieldName, StringComparer.Ordinal))
        {
            throw new ArgumentOutOfRangeException(
                nameof(fieldName), fieldName, "the goal block names no such field");
        }

        _goalValues[fieldName] = value;
    }

    /// <summary>Binds this draft to a catalog template and switches to the template shape.</summary>
    public void UseTemplate(string templateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
        TemplateId = templateId;
        Shape = ComposerShape.Template;
    }

    /// <summary>Sets one template field's values, in the caller's order — the order is the content.</summary>
    public void SetTemplateValue(string fieldName, IReadOnlyList<string> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        ArgumentNullException.ThrowIfNull(values);
        _templateValues[fieldName] = [.. values];
    }

    /// <summary>Adds an affirmed attachment.</summary>
    public void Add(ComposerAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        _attachments.Add(attachment);
    }

    /// <summary>Switches shape, retaining every shape's own content.</summary>
    /// <param name="shape">The shape to move to.</param>
    /// <param name="goalBlockText">
    /// The compiled goal-block text, supplied by the caller so this type never compiles anything
    /// itself. Used only to seed an <i>empty</i> free-form draft on the goal-block to free-form
    /// direction; ignored otherwise.
    /// </param>
    public void SwitchTo(ComposerShape shape, string? goalBlockText = null)
    {
        if (shape == ComposerShape.FreeForm
            && Shape == ComposerShape.GoalBlock
            && !_freeFormWritten
            && goalBlockText is not null)
        {
            _freeForm = goalBlockText;
            _freeFormWritten = true;
        }

        Shape = shape;
    }

    /// <summary>
    /// The goal block this draft declares, as a value the one validation mechanism can read.
    /// </summary>
    /// <remarks>
    /// Nullable throughout, exactly as <see cref="GoalBlock"/> is: a blank field must be expressible
    /// so the form engine can name it, rather than being defaulted into something that validates.
    /// </remarks>
    public GoalBlock ToGoalBlock()
    {
        return new GoalBlock(
            Text(GoalBlockFields.GoalKey),
            Text(GoalBlockFields.DoneWhenKey),
            Text(GoalBlockFields.NotInScopeKey),
            Text(GoalBlockFields.TierKey),
            int.TryParse(Text(GoalBlockFields.FanOutCapKey), out var cap) ? cap : null,
            ParseBudget(Text(GoalBlockFields.BudgetKey)));

        string? Text(string field) =>
            _goalValues.TryGetValue(field, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
    }

    /// <summary>
    /// Reads a budget written as <c>requests,tokens</c> — the two native number inputs, joined.
    /// </summary>
    /// <remarks>
    /// A pair that does not parse becomes <c>null</c> rather than a zero budget, because a zero
    /// budget is a spawn that can never do anything and the validator has a better message for the
    /// missing case than for the impossible one.
    /// </remarks>
    private static RunBudget? ParseBudget(string? raw)
    {
        if (raw is null)
        {
            return null;
        }

        var parts = raw.Split(',', StringSplitOptions.TrimEntries);
        return parts.Length == 2
            && int.TryParse(parts[0], out var requests)
            && long.TryParse(parts[1], out var tokens)
                ? new RunBudget(requests, tokens)
                : null;
    }
}
