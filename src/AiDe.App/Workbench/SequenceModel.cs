namespace AiDe.App.Workbench;

/// <summary>The kind of a sequence-diagram message, which fixes its UML arrow style.</summary>
public enum SequenceMessageKind
{
    /// <summary>A synchronous call — solid line, filled arrowhead.</summary>
    Call,

    /// <summary>A return — dashed line, open arrowhead.</summary>
    Return,

    /// <summary>A self-call — a message from a participant to itself (a loop back to its own lifeline).</summary>
    Self,
}

/// <summary>A participant (object/actor) in a sequence diagram — a header box atop a vertical lifeline.</summary>
public sealed record SequenceParticipant(string Id, string Label);

/// <summary>
/// One message between participants, in wire order. <see cref="Order"/> is the position in the
/// interaction (0-based), which is what a sequence diagram draws top-to-bottom.
/// </summary>
public sealed record SequenceMessage(int Order, string FromId, string ToId, string Label, SequenceMessageKind Kind);

/// <summary>
/// The sequence-diagram view model (UML interaction): the participants and the ordered messages
/// between them. A pure projection so it is verifiable headlessly, mirroring <see cref="ClassHierarchy"/>.
/// </summary>
/// <remarks>
/// <b>Data source (scaffold).</b> A faithful sequence diagram needs <i>ordered</i> call data — which
/// method calls which, in what order along a trace — which the graph does not yet emit (the Core ask
/// is <c>session-contracts §4k</c>). Until then <see cref="Build"/> projects from whatever ordered
/// call tuples it is handed (a test stub today; the Core feed when it lands), and the surface shows an
/// explicit empty state rather than implying an interaction that was not captured.
/// </remarks>
public sealed record SequenceModel(
    IReadOnlyList<SequenceParticipant> Participants,
    IReadOnlyList<SequenceMessage> Messages)
{
    public static readonly SequenceModel Empty = new([], []);

    public bool IsEmpty => Participants.Count == 0;

    /// <summary>
    /// Builds a sequence model from ordered call tuples <c>(from, to, label)</c>. Participants are
    /// derived from the calls in first-seen order (so the leftmost lifeline is the caller that starts
    /// the interaction); a call whose endpoints are equal is a self-message. Pure and deterministic.
    /// </summary>
    public static SequenceModel Build(IReadOnlyList<(string From, string To, string Label)>? calls)
    {
        calls ??= [];

        var participants = new List<SequenceParticipant>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Note(string id)
        {
            if (!string.IsNullOrEmpty(id) && seen.Add(id))
            {
                participants.Add(new SequenceParticipant(id, Simple(id)));
            }
        }

        var messages = new List<SequenceMessage>();
        var order = 0;
        foreach (var (from, to, label) in calls)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            {
                continue;
            }

            Note(from);
            Note(to);
            var kind = string.Equals(from, to, StringComparison.Ordinal)
                ? SequenceMessageKind.Self
                : SequenceMessageKind.Call;
            messages.Add(new SequenceMessage(order++, from, to, label ?? Simple(to), kind));
        }

        return new SequenceModel(participants, messages);
    }

    private static string Simple(string id)
    {
        var t = id;
        var paren = t.IndexOf('(');
        if (paren >= 0) { t = t[..paren]; }
        var dot = t.LastIndexOf('.');
        if (dot >= 0 && dot < t.Length - 1) { t = t[(dot + 1)..]; }
        return t.Trim();
    }
}
