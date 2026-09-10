using System.Text.Json.Nodes;
using AiDe.Core.AgentPlane;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// One line of the merged Console stream, carrying the lane it came from (R16 b1's "lane rail":
/// attribution is a property of the row, never a guess made at render time).
/// </summary>
/// <param name="Ordinal">The producing lane's own <see cref="RunEvent.Seq"/> — see the model's remarks.</param>
/// <param name="LaneId">Which lane produced it.</param>
/// <param name="LaneName">That lane's display name, resolved once at append.</param>
/// <param name="Kind">The <see cref="RunEvent.Kind"/>, verbatim.</param>
/// <param name="Text">What to show. Never null; an event with no text reads as its kind.</param>
public sealed record ConsoleRow(long Ordinal, string LaneId, string LaneName, string Kind, string Text);

/// <summary>One lane on the rail: who it is, how much it has said, and whether it is shown.</summary>
public sealed record ConsoleRail(string LaneId, string LaneName, int Rows, bool Visible);

/// <summary>
/// One node of the filter tree: a lane, or one event kind within a lane. Excluding a node hides
/// every row beneath it.
/// </summary>
/// <param name="LaneId">The lane this node belongs to.</param>
/// <param name="Kind">Null for the lane node itself; an event kind for a child.</param>
/// <param name="Label">What the tree shows.</param>
/// <param name="Rows">How many rows sit under it.</param>
/// <param name="Visible">Whether it is currently included.</param>
public sealed record ConsoleFilterNode(string LaneId, string? Kind, string Label, int Rows, bool Visible);

/// <summary>
/// The merged Console stream across every lane of one session (R16 b1) — rows, the lane rail, and
/// the filter tree over them.
/// </summary>
/// <remarks>
/// <para><b>The ordinal is the lane's own sequence, not a counter this type invents.</b>
/// <see cref="AcpRunEventMapper"/> assigns <see cref="RunEvent.Seq"/> at receipt, so a gap in what
/// the console holds is a gap in what the console <i>received</i> — which is exactly the question
/// the retain-never-rebuild clause asks. A second monotonic counter here would renumber whatever
/// arrived and make every rebuild look contiguous (DM7: two definitions of one quantity).</para>
///
/// <para><b>Filtering hides rows; it never drops them.</b> <see cref="Rows"/> is the record and
/// <see cref="VisibleRows"/> is the view, so excluding a lane cannot silently destroy history and
/// <see cref="OrdinalGaps"/> keeps answering about what arrived rather than about what is on
/// screen.</para>
/// </remarks>
public sealed class ConsoleStreamModel
{
    private readonly List<ConsoleRow> _rows = [];
    private readonly Dictionary<string, string> _laneNames = new(StringComparer.Ordinal);
    private readonly List<string> _laneOrder = [];
    private readonly HashSet<string> _hiddenLanes = new(StringComparer.Ordinal);
    private readonly HashSet<(string Lane, string Kind)> _hiddenKinds = [];

    /// <summary>
    /// Guards every read and write. <b>A merged stream is written by more than one lane by
    /// definition</b> — that is what "merged" means — and each lane drains its own queue on its own
    /// thread, so an unsynchronised <see cref="List{T}"/> here is not a theoretical race: two lanes
    /// appending at once corrupt it or throw, intermittently, which reads as a flaky test rather
    /// than as the defect it is (DC-078). Reads return snapshots for the same reason: a view
    /// enumerating while a lane appends would throw mid-render.
    /// </summary>
    private readonly Lock _gate = new();

    /// <summary>Every row that ever arrived, in receipt order.</summary>
    public IReadOnlyList<ConsoleRow> Rows
    {
        get
        {
            lock (_gate)
            {
                return [.. _rows];
            }
        }
    }

    /// <summary>The rows the filter currently includes.</summary>
    public IReadOnlyList<ConsoleRow> VisibleRows
    {
        get
        {
            lock (_gate)
            {
                return [.. _rows.Where(
                    r => !_hiddenLanes.Contains(r.LaneId) && !_hiddenKinds.Contains((r.LaneId, r.Kind)))];
            }
        }
    }

    /// <summary>Raised after any append or filter change, so a view can re-read.</summary>
    public event Action? Changed;

    /// <summary>The lane rail, in first-seen order.</summary>
    public IReadOnlyList<ConsoleRail> Rail
    {
        get
        {
            lock (_gate)
            {
                return
                [
                    .. _laneOrder.Select(id => new ConsoleRail(
                        id,
                        _laneNames[id],
                        _rows.Count(r => string.Equals(r.LaneId, id, StringComparison.Ordinal)),
                        !_hiddenLanes.Contains(id))),
                ];
            }
        }
    }

    /// <summary>The filter tree: one node per lane, one child per kind that lane has produced.</summary>
    public IReadOnlyList<ConsoleFilterNode> FilterTree
    {
        get
        {
            lock (_gate)
            {
                return FilterTreeUnsafe();
            }
        }
    }

    /// <summary>Builds the tree. The caller holds <see cref="_gate"/>.</summary>
    private List<ConsoleFilterNode> FilterTreeUnsafe()
    {
        var nodes = new List<ConsoleFilterNode>();

        foreach (var lane in _laneOrder)
        {
            var laneRows = _rows.Where(r => string.Equals(r.LaneId, lane, StringComparison.Ordinal)).ToList();
            nodes.Add(new ConsoleFilterNode(
                lane, null, _laneNames[lane], laneRows.Count, !_hiddenLanes.Contains(lane)));

            foreach (var kind in laneRows.Select(r => r.Kind).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
            {
                nodes.Add(new ConsoleFilterNode(
                    lane,
                    kind,
                    kind,
                    laneRows.Count(r => string.Equals(r.Kind, kind, StringComparison.Ordinal)),
                    !_hiddenLanes.Contains(lane) && !_hiddenKinds.Contains((lane, kind))));
            }
        }

        return nodes;
    }

    /// <summary>Appends one lane event to the merged stream.</summary>
    public void Append(string laneId, string laneName, RunEvent evt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(laneId);
        ArgumentNullException.ThrowIfNull(evt);

        lock (_gate)
        {
            if (!_laneNames.ContainsKey(laneId))
            {
                _laneNames[laneId] = string.IsNullOrWhiteSpace(laneName) ? laneId : laneName;
                _laneOrder.Add(laneId);
            }

            _rows.Add(new ConsoleRow(evt.Seq, laneId, _laneNames[laneId], evt.Kind, TextOf(evt)));
        }

        // Outside the lock: a subscriber marshals to the UI thread, and holding a lock across that
        // hand-off is how a render and an append deadlock against each other.
        Changed?.Invoke();
    }

    /// <summary>Includes or excludes a whole lane.</summary>
    public void SetLaneVisible(string laneId, bool visible)
    {
        lock (_gate)
        {
            if (visible)
            {
                _hiddenLanes.Remove(laneId);
            }
            else
            {
                _hiddenLanes.Add(laneId);
            }
        }

        Changed?.Invoke();
    }

    /// <summary>Includes or excludes one event kind within one lane.</summary>
    public void SetKindVisible(string laneId, string kind, bool visible)
    {
        lock (_gate)
        {
            if (visible)
            {
                _hiddenKinds.Remove((laneId, kind));
            }
            else
            {
                _hiddenKinds.Add((laneId, kind));
            }
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// Ordinals this lane never delivered, from 1 up to the highest it did — the positive oracle for
    /// "no event was lost".
    /// </summary>
    /// <remarks>
    /// Empty is the only passing answer. A console that was rebuilt starts its history at whatever
    /// arrived after the rebuild, so every ordinal before that reads here as missing — which is the
    /// difference <c>Assert.Same</c> cannot see.
    /// </remarks>
    public IReadOnlyList<long> OrdinalGaps(string laneId)
    {
        HashSet<long> seen;

        lock (_gate)
        {
            seen = [.. _rows
                .Where(r => string.Equals(r.LaneId, laneId, StringComparison.Ordinal))
                .Select(r => r.Ordinal)];
        }

        if (seen.Count == 0)
        {
            return [];
        }

        var highest = seen.Max();
        return [.. Enumerable.Range(1, (int)highest).Select(i => (long)i).Where(o => !seen.Contains(o))];
    }

    /// <summary>
    /// What a row shows: the event's own text when it carries one, else its kind.
    /// </summary>
    /// <remarks>
    /// <b>The two shapes are the mapper's, read rather than guessed.</b> An <c>agent.msg</c> body is
    /// the lifted <c>update</c>, whose text sits at <c>content.text</c>; a <c>permission.request</c>
    /// body is the lifted <c>params</c>, whose text sits at <c>title</c>. Falling back to the kind is
    /// deliberate: a blank row reads as an event with nothing in it rather than as one this
    /// projection did not recognise, and the kind is always true.
    /// </remarks>
    private static string TextOf(RunEvent evt) =>
        Text(evt.Body)
        ?? (evt.Body["content"] is JsonObject content ? Text(content) : null)
        ?? evt.Kind;

    private static string? Text(JsonObject body)
    {
        foreach (var field in (string[])["text", "message", "title"])
        {
            if (body.TryGetPropertyValue(field, out var node) && node is JsonValue value
                && value.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }
}
