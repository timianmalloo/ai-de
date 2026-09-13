namespace AiDe.Core.Presentation.Sessions;

/// <summary>The status word of a tool item (DESIGN.md, the tool item row): <i>running · done · failed · interrupted</i>.</summary>
public enum ToolStatus
{
    /// <summary>No terminal status yet on a live turn — the ring.</summary>
    Running,

    /// <summary>The last result said <c>completed</c>.</summary>
    Done,

    /// <summary>The last result said <c>failed</c>.</summary>
    Failed,

    /// <summary>No terminal status on a turn that is no longer live: the lane ended before the tool answered — static, muted, never a ring.</summary>
    Interrupted,
}

/// <summary>
/// One item of a turn's conversation (Ruling 82), in event order: prose, reasoning, a tool call with
/// its results, or an event — a non-conversation row (<c>acp.*</c>, the conductor's lines,
/// stderr, a result whose call is not in this turn) that counts into <i>N events</i> and is never
/// dropped.
/// </summary>
/// <param name="Row">The <see cref="Coalesce"/> row the item renders — the message, the thought, the call, or the event.</param>
public abstract record ConversationItem(TurnRow Row)
{
    /// <summary>An <c>agent.msg</c> row: the lane's prose, rendered as the markdown subset with no link activation.</summary>
    public sealed record Prose(TurnRow Row) : ConversationItem(Row);

    /// <summary>An <c>agent.thought</c> row: reasoning — collapsed, muted, plain text, never announced (SC9).</summary>
    public sealed record Reasoning(TurnRow Row) : ConversationItem(Row);

    /// <summary>A <c>tool.call</c> row with its <c>tool.result</c> rows attached by id: <i>kind · title · status</i>, the detail on demand.</summary>
    /// <param name="Row">The call.</param>
    /// <param name="Results">Its results, in event order — empty when none arrived.</param>
    /// <param name="Status">The fold of the stated statuses against whether the turn is live.</param>
    /// <param name="Kind">The last stated kind, as a word; empty when no frame stated one.</param>
    /// <param name="Title">The last stated title, else the call row's text.</param>
    /// <param name="Input">The last stated input; empty when none.</param>
    /// <param name="Output">The results' outputs joined; empty when none.</param>
    public sealed record Tool(TurnRow Row, IReadOnlyList<TurnRow> Results, ToolStatus Status, string Kind, string Title, string Input, string Output) : ConversationItem(Row);

    /// <summary>A non-conversation row: folded into <i>N events</i>, rendered as an event line, never as an item.</summary>
    public sealed record Event(TurnRow Row) : ConversationItem(Row);
}

/// <summary>
/// The ONE projection from <c>Coalesce(turn.Events)</c> to the conversation's items (Ruling 82) —
/// pure, read by the thread's reply side; the Console split reads the rows beneath it (DM7: one
/// derivation, two readers). There is no grouping rule: each <c>tool.call</c> is one item (the
/// review's §7, the Simplifier's veto on D3's run-of-four grouping).
/// </summary>
public static class ConversationItems
{
    /// <summary>The mapper's <c>tool_call</c>.</summary>
    public const string CallKind = "tool.call";

    /// <summary>The mapper's <c>tool_call_update</c>.</summary>
    public const string ResultKind = "tool.result";

    /// <summary>The items of a turn's rows, in the rows' order. <paramref name="live"/>: the turn is running or waiting, so a call with no terminal status is <i>running</i>, not <i>interrupted</i>.</summary>
    public static IReadOnlyList<ConversationItem> Of(IReadOnlyList<TurnRow> rows, bool live)
    {
        ArgumentNullException.ThrowIfNull(rows);

        // A result belongs to the call with its id that PRECEDES it; a result with no such call is an
        // event row (never dropped, never a call's by position — attribution is a property of the row).
        var calls = new Dictionary<string, List<TurnRow>>(StringComparer.Ordinal);
        var items = new List<ConversationItem>(rows.Count);
        foreach (var row in rows)
        {
            switch (row.Kind)
            {
                case Coalesce.MessageKind:
                    items.Add(new ConversationItem.Prose(row));
                    break;
                case Coalesce.ThoughtKind:
                    items.Add(new ConversationItem.Reasoning(row));
                    break;
                case CallKind:
                    var results = new List<TurnRow>();
                    if (row.Tool is { } call)
                    {
                        calls[call.CallId] = results;
                    }

                    items.Add(new ConversationItem.Tool(row, results, ToolStatus.Interrupted, string.Empty, row.Text, string.Empty, string.Empty));
                    break;
                case ResultKind when row.Tool is { } result && calls.TryGetValue(result.CallId, out var attached):
                    attached.Add(row);
                    break;
                default:
                    items.Add(new ConversationItem.Event(row));
                    break;
            }
        }

        // The fold over each call and its results, once every result is attached.
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is ConversationItem.Tool tool)
            {
                items[i] = Fold(tool, live);
            }
        }

        return items;
    }

    /// <summary>The status word: <i>running · done · failed · interrupted</i>.</summary>
    public static string StatusWord(ToolStatus status) => status switch
    {
        ToolStatus.Running => "running",
        ToolStatus.Done => "done",
        ToolStatus.Failed => "failed",
        ToolStatus.Interrupted => "interrupted",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "unknown tool status"),
    };

    private static ConversationItem.Tool Fold(ConversationItem.Tool tool, bool live)
    {
        var frames = new List<ToolFacts>();
        if (tool.Row.Tool is { } call)
        {
            frames.Add(call);
        }

        frames.AddRange(tool.Results.Select(r => r.Tool).Where(f => f is not null)!);

        var status = frames.Select(f => f.Status).LastOrDefault(s => s is not null) switch
        {
            "completed" => ToolStatus.Done,
            "failed" => ToolStatus.Failed,
            _ => live ? ToolStatus.Running : ToolStatus.Interrupted,
        };

        return tool with
        {
            Status = status,
            Kind = frames.Select(f => f.Kind).LastOrDefault(k => k is not null) ?? string.Empty,
            Title = frames.Select(f => f.Title).LastOrDefault(t => t is not null) ?? tool.Row.Text,
            Input = frames.Select(f => f.Input).LastOrDefault(i => i is not null) ?? string.Empty,
            Output = string.Join('\n', tool.Results.Select(r => r.Tool?.Output).Where(o => o is not null)),
        };
    }
}
