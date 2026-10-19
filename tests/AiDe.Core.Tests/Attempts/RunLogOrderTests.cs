using System.Threading.Channels;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.Core.Tests.Attempts;

/// <summary>
/// The run log's order against the peer's publish (ADR-0041 §3: append and flush <b>before</b>
/// <c>Events.PublishAsync</c>), its failure path (§4: an unwritable line is not published and fails the prompt),
/// and the thread's Stop precedence (§4: <c>StopRequested</c> dominates <c>Waiting</c>).
/// </summary>
public sealed class RunLogOrderTests : IDisposable
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(30);
    private readonly string _root = Directory.CreateTempSubdirectory("aide-runlog-").FullName;

    [Fact]
    public async Task EveryEvent_IsInTheRunLog_BeforeThePeerPublishesIt()
    {
        var input = new LineReader();
        using var log = RunLogStore.Open(_root, "s-1", "key-1");
        var peer = new AcpPeer(input, new StringWriter(), new AcpRunEventMapper("key-1", "lane-1"), diagnostics: _ => { }, runLog: log);
        var pump = peer.RunAsync();

        for (var i = 0; i < 3; i++)
        {
            input.Send("""{"jsonrpc":"2.0","method":"session/update","params":{"sessionId":"s","update":{"sessionUpdate":"agent_message_chunk","content":{"type":"text","text":"x"}}}}""");
        }

        input.End();
        await foreach (var published in peer.Events.Reader.ReadAllAsync().WithCancellation(new CancellationTokenSource(Bound).Token))
        {
            Assert.Contains(RunLogStore.Read(log.Path), logged => logged.Seq == published.Event.Seq);
        }

        await pump.WaitAsync(Bound);
        Assert.Equal(3, RunLogStore.Read(log.Path).Count);
        Assert.Equal(3, log.Events);
    }

    [Fact]
    public async Task ALineThatCannotBeMadeDurable_IsNotPublished_AndFailsThePendingPromptWithRunLogUnwritable()
    {
        var input = new LineReader();
        using var log = new RunLogStore(new RefusingStream(), Path.Combine(_root, "refused.jsonl"));
        var peer = new AcpPeer(input, new StringWriter(), new AcpRunEventMapper("key-1", "lane-1"), diagnostics: _ => { }, runLog: log);
        var pump = peer.RunAsync();

        var prompt = peer.RequestAsync("session/prompt", null, Bound);
        input.Send("""{"jsonrpc":"2.0","method":"session/update","params":{"sessionId":"s","update":{"sessionUpdate":"agent_message_chunk","content":{"type":"text","text":"x"}}}}""");

        var refused = await Assert.ThrowsAsync<AgentPlaneException>(() => prompt.WaitAsync(Bound));
        Assert.Equal(RunLogStore.UnwritableCode, refused.Code);
        await pump.WaitAsync(Bound);
        Assert.Equal(0, peer.Events.Published);
    }

    [Fact]
    public void AStopRequestedTurn_StaysStopRequested_WhenAWaitOrAResumeArrives()
    {
        var thread = new RunChannelSessionThread();
        var ordinal = thread.Accept("go", [], "bytes", DateTimeOffset.UnixEpoch);
        thread.RequestStop(ordinal);

        thread.Wait(ordinal, new WaitingRequest("r-1", "permission", "write a file?", [TurnActionKind.Stop]));
        Assert.Equal(TurnState.StopRequested, thread.Current.Turns.Single(t => t.Ordinal == ordinal).State);

        thread.Resume(ordinal);
        Assert.Equal(TurnState.StopRequested, thread.Current.Turns.Single(t => t.Ordinal == ordinal).State);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { /* reaped later */ }
    }

    /// <summary>The engine's stdout, line by line, on cue.</summary>
    private sealed class LineReader : TextReader
    {
        private readonly Channel<string> _lines = Channel.CreateUnbounded<string>();
        private string _pending = "";

        public void Send(string frame) => _lines.Writer.TryWrite(frame + "\n");

        public void End() => _lines.Writer.TryComplete();

        public override async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            if (_pending.Length == 0)
            {
                if (!await _lines.Reader.WaitToReadAsync(cancellationToken) || !_lines.Reader.TryRead(out var next))
                {
                    return 0;
                }

                _pending = next;
            }

            var count = Math.Min(buffer.Length, _pending.Length);
            _pending.AsSpan(0, count).CopyTo(buffer.Span);
            _pending = _pending[count..];
            return count;
        }
    }

    /// <summary>A disk that refuses every write.</summary>
    private sealed class RefusingStream : MemoryStream
    {
        public override void Write(ReadOnlySpan<byte> buffer) => throw new IOException("simulated: the disk is full");
    }
}
