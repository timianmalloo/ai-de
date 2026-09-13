using AiDe.Core.Presentation.Sessions;

// The folder is Presentation/Sessions (the oracle's path); the namespace is the thread tests' — a
// `Tests.Presentation` namespace would shadow `AiDe.Core.Presentation` for every sibling (DC-172).
namespace AiDe.Core.Tests.Sessions.Thread;

/// <summary>
/// <b>C1</b> (Ruling 81; CV-5.2). <c>Coalesce(turn.Events)</c> is the one pure fold from wire chunks
/// to message rows: consecutive <c>agent.msg</c> chunks are one row carrying the first chunk's
/// timestamp, the lane, the joined text and the chunk count; any other kind — or another lane —
/// breaks the run (condition 2: the boundary is the interleaving); <c>agent.thought</c> folds as
/// its own run; <c>tool.*</c>, <c>permission.request</c> and <c>acp.*</c> stay one row per event.
/// </summary>
public sealed class CoalesceTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 13, 16, 29, 56, TimeSpan.Zero);

    private static EventLine Line(int second, string kind, string text, string lane = "claude-code") =>
        new(T0.AddSeconds(second), lane, kind, text, "run");

    /// <summary>The operator's own Console (D3 screenshot 2): <i>mer</i> · <i>ges are</i> · <i>missing from the tracker and</i> — three rows for one message.</summary>
    private static readonly EventLine[] OperatorsThreeChunks =
    [
        Line(0, Coalesce.MessageKind, "mer"),
        Line(0, Coalesce.MessageKind, "ges are"),
        Line(1, Coalesce.MessageKind, " missing from the tracker and"),
    ];

    [Fact]
    public void ConsecutiveMessageChunks_FoldToOneRow_WithFirstTimestampLaneJoinedTextAndChunkCount()
    {
        var rows = Coalesce.Rows(OperatorsThreeChunks);

        var row = Assert.Single(rows);
        Assert.Equal(T0, row.At);
        Assert.Equal("claude-code", row.Lane);
        Assert.Equal(Coalesce.MessageKind, row.Kind);
        Assert.Equal("merges are missing from the tracker and", row.Text);
        Assert.Equal(3, row.Chunks);
    }

    /// <summary>Condition 2: a message a tool call interrupts is two rows — the boundary is the interleaving.</summary>
    [Fact]
    public void AToolCallBetweenChunks_BreaksTheMessageIntoTwoRows()
    {
        EventLine[] events =
        [
            Line(0, Coalesce.MessageKind, "Reading "),
            Line(0, Coalesce.MessageKind, "the tracker."),
            Line(1, "tool.call", "read docs/tracker.md"),
            Line(2, Coalesce.MessageKind, "Three merges "),
            Line(2, Coalesce.MessageKind, "are missing."),
        ];

        var rows = Coalesce.Rows(events);

        Assert.Equal(3, rows.Count);
        Assert.Equal(["Reading the tracker.", "read docs/tracker.md", "Three merges are missing."], rows.Select(r => r.Text));
        Assert.Equal([2, null, 2], rows.Select(r => r.Chunks));
        Assert.Equal(T0.AddSeconds(2), rows[2].At);
    }

    /// <summary>Ruling 82: reasoning folds as its own run — a thought beside a message is never one row.</summary>
    [Fact]
    public void ThoughtChunks_FoldSeparatelyFromMessageChunks()
    {
        EventLine[] events =
        [
            Line(0, Coalesce.ThoughtKind, "The tracker "),
            Line(0, Coalesce.ThoughtKind, "is stale."),
            Line(1, Coalesce.MessageKind, "Three "),
            Line(1, Coalesce.MessageKind, "merges are missing."),
            Line(2, Coalesce.ThoughtKind, "Name them."),
        ];

        var rows = Coalesce.Rows(events);

        Assert.Equal([Coalesce.ThoughtKind, Coalesce.MessageKind, Coalesce.ThoughtKind], rows.Select(r => r.Kind));
        Assert.Equal(["The tracker is stale.", "Three merges are missing.", "Name them."], rows.Select(r => r.Text));
        Assert.Equal([2, 2, 1], rows.Select(r => r.Chunks));
    }

    /// <summary>A row carries ONE lane (attribution is a property of the row, never a guess): another lane's chunk starts a new row.</summary>
    [Fact]
    public void AnotherLanesChunk_StartsANewRow()
    {
        EventLine[] events =
        [
            Line(0, Coalesce.MessageKind, "from claude ", lane: "claude-code"),
            Line(0, Coalesce.MessageKind, "from the conductor", lane: "conductor"),
        ];

        var rows = Coalesce.Rows(events);

        Assert.Equal(["claude-code", "conductor"], rows.Select(r => r.Lane));
        Assert.All(rows, r => Assert.Equal(1, r.Chunks));
    }

    [Theory]
    [InlineData("tool.call")]
    [InlineData("tool.result")]
    [InlineData("permission.request")]
    [InlineData("acp.session.update.usage_update")]
    [InlineData("acp.result")]
    [InlineData("stderr")]
    public void EveryOtherKind_IsOneRowPerEvent_WithNoChunkCount(string kind)
    {
        EventLine[] events = [Line(0, kind, "first"), Line(0, kind, "second"), Line(1, kind, "third")];

        var rows = Coalesce.Rows(events);

        Assert.Equal(3, rows.Count);
        Assert.Equal(["first", "second", "third"], rows.Select(r => r.Text));
        Assert.All(rows, r => Assert.Null(r.Chunks));
        Assert.All(rows, r => Assert.Equal(kind, r.Kind));
    }

    [Fact]
    public void NoEvents_NoRows()
    {
        Assert.Empty(Coalesce.Rows([]));
    }

    /// <summary>
    /// The vocabulary join, through the real mapper: an <c>agent_message_chunk</c> frame maps to the
    /// kind the fold folds, and two of them — their text read as the Console reads it — are one row.
    /// A renamed constant on either side leaves every other oracle green while production folds
    /// nothing; this is the one that goes red.
    /// </summary>
    [Fact]
    public void TheMappersMessageChunk_IsTheKindTheFoldFolds()
    {
        var mapper = new AiDe.Core.AgentPlane.AcpRunEventMapper("run-1", "claude-code");
        var first = mapper.Map("""{"jsonrpc":"2.0","method":"session/update","params":{"sessionId":"s","update":{"sessionUpdate":"agent_message_chunk","content":{"type":"text","text":"mer"}}}}""", T0);
        var second = mapper.Map("""{"jsonrpc":"2.0","method":"session/update","params":{"sessionId":"s","update":{"sessionUpdate":"agent_message_chunk","content":{"type":"text","text":"ges are"}}}}""", T0.AddSeconds(1));

        Assert.Equal(Coalesce.MessageKind, first.Kind);
        var rows = Coalesce.Rows([
            new EventLine(first.Ts, "claude-code", first.Kind, ConsoleStreamModel.TextOf(first), "run"),
            new EventLine(second.Ts, "claude-code", second.Kind, ConsoleStreamModel.TextOf(second), "run"),
        ]);

        var row = Assert.Single(rows);
        Assert.Equal("merges are", row.Text);
        Assert.Equal(2, row.Chunks);
    }

    /// <summary>
    /// <b>DC-nnn (CV-5-3 a).</b> A wire chunk whose text is only whitespace — the corpus has one,
    /// <c>thought.jsonl:27</c>, the <c>"\n\n"</c> between two paragraphs of a thought — is that
    /// whitespace, never the kind: with the reader's blank-means-absent fallback the fold joined the
    /// literal word <c>agent.thought</c> into the reasoning (and would join <c>agent.msg</c> into the
    /// prose on the same chunk boundary). Absent text still reads as the kind, so a frame with nothing
    /// in it is not blank — the positive control beside the fix.
    /// </summary>
    /// <remarks><b>Red observed</b>: <c>Expected: "\n\n" · Actual: "agent.thought"</c> (M1 failed on the same chunk).</remarks>
    [Fact]
    public void AWhitespaceOnlyChunk_IsItsWhitespace_NeverTheKind()
    {
        var mapper = new AiDe.Core.AgentPlane.AcpRunEventMapper("run-1", "claude-code");
        var blank = mapper.Map("""{"jsonrpc":"2.0","method":"session/update","params":{"sessionId":"s","update":{"sessionUpdate":"agent_thought_chunk","content":{"type":"text","text":"\n\n"}}}}""", T0);
        var empty = mapper.Map("""{"jsonrpc":"2.0","method":"session/update","params":{"sessionId":"s","update":{"sessionUpdate":"agent_message_chunk","content":{"type":"text","text":""}}}}""", T0);
        var absent = mapper.Map("""{"jsonrpc":"2.0","method":"session/update","params":{"sessionId":"s","update":{"sessionUpdate":"usage_update","used":1,"size":2}}}""", T0);

        Assert.Equal("\n\n", ConsoleStreamModel.TextOf(blank));
        Assert.Equal(string.Empty, ConsoleStreamModel.TextOf(empty));
        Assert.Equal("acp.session.update.usage_update", ConsoleStreamModel.TextOf(absent));

        var thought = Coalesce.Rows([
            new EventLine(T0, "claude-code", Coalesce.ThoughtKind, "one.", "run"),
            new EventLine(T0, "claude-code", Coalesce.ThoughtKind, ConsoleStreamModel.TextOf(blank), "run"),
            new EventLine(T0, "claude-code", Coalesce.ThoughtKind, "two.", "run"),
        ]);
        Assert.Equal("one.\n\ntwo.", Assert.Single(thought).Text);
    }

    /// <summary>
    /// D2, over generated interleavings (seeded — D0): the row count is the number of runs, a run
    /// being a maximal stretch of one foldable kind from one lane, or one event of any other kind;
    /// each row's text is its events' text joined; no text is lost or invented; every row's
    /// timestamp is its first event's.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(1981)]
    [InlineData(65_537)]
    public void OverGeneratedInterleavings_TheRowCountIsTheRunCount_AndTheTextRoundTrips(int seed)
    {
        var random = new Random(seed);
        string[] kinds = [Coalesce.MessageKind, Coalesce.MessageKind, Coalesce.MessageKind, Coalesce.ThoughtKind, "tool.call", "tool.result", "acp.result"];
        string[] lanes = ["claude-code", "claude-code", "claude-code", "conductor"];

        for (var trial = 0; trial < 200; trial++)
        {
            var events = Enumerable.Range(0, random.Next(0, 40))
                .Select(i => Line(i, kinds[random.Next(kinds.Length)], $"<{i}>", lanes[random.Next(lanes.Length)]))
                .ToList();

            var rows = Coalesce.Rows(events);

            Assert.Equal(Runs(events), rows.Count);
            Assert.Equal(string.Concat(events.Select(e => e.Text)), string.Concat(rows.Select(r => r.Text)));
            Assert.Equal(events.Count, rows.Sum(r => r.Chunks ?? 1));

            var cursor = 0;
            foreach (var row in rows)
            {
                Assert.Equal(events[cursor].At, row.At);
                Assert.Equal(events[cursor].Lane, row.Lane);
                Assert.Equal(events[cursor].Kind, row.Kind);
                cursor += row.Chunks ?? 1;
            }
        }
    }

    /// <summary>The oracle's own definition of a run, written independently of the fold — its own foldable set spelled out, not the fold's predicate: a change of kind or lane, or any non-folding kind, starts one.</summary>
    private static int Runs(IReadOnlyList<EventLine> events)
    {
        var runs = 0;
        for (var i = 0; i < events.Count; i++)
        {
            var continues = i > 0
                && events[i].Kind is "agent.msg" or "agent.thought"
                && events[i].Kind == events[i - 1].Kind
                && events[i].Lane == events[i - 1].Lane;
            if (!continues)
            {
                runs++;
            }
        }

        return runs;
    }
}
