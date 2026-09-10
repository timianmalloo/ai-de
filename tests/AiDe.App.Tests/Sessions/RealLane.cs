using System.Text.Json.Nodes;
using System.Threading.Channels;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// A <see cref="TextReader"/> a test pushes frames into, and closes to signal end of stream.
/// </summary>
/// <remarks>
/// <b>A second copy, deliberately, and this is why.</b> <c>AiDe.Core.Tests.AgentPlane.PushTextReader</c>
/// is the same idea and is <c>internal</c> to that assembly, so it cannot be reached from here.
/// Moving it into <c>tests/Shared</c> would edit another node's test tree at a serial point in the
/// slice for a thirty-line double, which is a larger change than the duplication it removes. If a
/// third assembly needs one, that is the moment it moves.
/// </remarks>
internal sealed class PushedOutputReader : TextReader
{
    private readonly Channel<string> _chunks = Channel.CreateUnbounded<string>();
    private string _current = string.Empty;
    private int _offset;

    /// <summary>Makes one NDJSON frame available to the next read, newline included.</summary>
    public void PushFrame(string json) => _chunks.Writer.TryWrite(json + "\n");

    /// <summary>End of stream — what a child process exiting looks like to its parent.</summary>
    public void EndOfStream() => _chunks.Writer.TryComplete();

    public override async ValueTask<int> ReadAsync(
        Memory<char> buffer, CancellationToken cancellationToken = default)
    {
        while (_offset >= _current.Length)
        {
            if (!await _chunks.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return 0;
            }

            if (_chunks.Reader.TryRead(out var next))
            {
                _current = next;
                _offset = 0;
            }
        }

        var count = Math.Min(buffer.Length, _current.Length - _offset);
        _current.AsSpan(_offset, count).CopyTo(buffer.Span);
        _offset += count;
        return count;
    }
}

/// <summary>
/// A real ACP lane, driven over a stream the test writes into.
/// </summary>
/// <remarks>
/// <para><b>Real, not a fake event source.</b> Everything from the wire inwards is the production
/// path: <see cref="AcpPeer"/> splits the frames, <see cref="AcpRunEventMapper"/> assigns
/// <see cref="RunEvent.Seq"/> and normalizes them, <see cref="AcpEventQueue"/> carries them, and
/// <see cref="SessionLane"/> drains that queue — the same four objects <c>GovernedRunHost</c>
/// composes. The only substitution is the child process's stdout, which is exactly what
/// <c>AcpPeerTests</c> substitutes and is the one part a unit test cannot own.</para>
///
/// <para><b>It matters that this is real.</b> The retain-never-rebuild oracle asks whether events
/// were lost across a mode switch. A hand-rolled emitter would answer about the emitter's own
/// bookkeeping; only the plane's own sequence can answer about the plane.</para>
/// </remarks>
internal sealed class RealLane : IDisposable
{
    private readonly PushedOutputReader _stdout = new();
    private readonly CancellationTokenSource _stopping = new();
    private readonly AcpPeer _peer;
    private readonly Task _pump;
    private int _pushed;

    public RealLane(string runId, string laneId)
    {
        LaneId = laneId;
        _peer = new AcpPeer(
            _stdout,
            TextWriter.Synchronized(new StringWriter()),
            new AcpRunEventMapper(runId, laneId),
            diagnostics: _ => { });

        _pump = _peer.RunAsync(_stopping.Token);
    }

    public string LaneId { get; }

    /// <summary>The plane's own queue for this lane — what a <see cref="SessionLane"/> drains.</summary>
    public ChannelReader<ObservedRunEvent> Events => _peer.Events.Reader;

    /// <summary>How many frames the test has pushed.</summary>
    public int Pushed => _pushed;

    /// <summary>Pushes one <c>agent_message_chunk</c> — the commonest frame a lane emits.</summary>
    public void Say(string text)
    {
        var frame = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = "session/update",
            ["params"] = new JsonObject
            {
                ["update"] = new JsonObject
                {
                    ["sessionUpdate"] = "agent_message_chunk",
                    ["content"] = new JsonObject { ["type"] = "text", ["text"] = text },
                },
            },
        };

        _stdout.PushFrame(frame.ToJsonString());
        _pushed++;
    }

    /// <summary>
    /// Pushes a real <c>session/request_permission</c> — an inbound request with an id, which the
    /// mapper projects onto the <c>permission.request</c> kind.
    /// </summary>
    /// <remarks>
    /// <c>"id": 0</c> on purpose: the captured corpus carries it, and a truthiness check reads it as
    /// a notification. The frame this test pushes is the frame the adapter really sends.
    /// </remarks>
    public void AskPermission(string title)
    {
        var frame = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 0,
            ["method"] = "session/request_permission",
            ["params"] = new JsonObject { ["title"] = title, ["options"] = new JsonArray() },
        };

        _stdout.PushFrame(frame.ToJsonString());
        _pushed++;
    }

    public void Dispose()
    {
        _stdout.EndOfStream();
        _stopping.Cancel();

        try
        {
            _pump.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // The pump was cancelled, which is how it is meant to end.
        }

        _stopping.Dispose();
    }
}
