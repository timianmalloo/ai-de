using System.Text;
using System.Text.Json;
using AiDe.Core.Sessions;

namespace AiDe.Core.AgentPlane;

/// <summary>
/// The durable run log of one execution attempt: one line per <see cref="RunEvent"/> at
/// <c>runs/&lt;dispatch_key&gt;.jsonl</c>, appended and flushed <b>before</b> the event is published
/// (ADR-0041 §3; docs/design/workbench-loops-wl-1a.md, "Run log, relay, redaction").
/// </summary>
/// <remarks>
/// <para><b>The line is T3's serialization</b>: <c>JsonSerializer.Serialize(runEvent)</c> with default
/// options, UTF-8, <c>\n</c>, no BOM — the format T6's fold reads.</para>
/// <para><b>A line that cannot be made durable is refused, not dropped.</b> <see cref="Append"/> throws
/// <see cref="AgentPlaneException"/> <see cref="UnwritableCode"/>; the peer then publishes nothing for the
/// frame and fails the pending prompt with that code, which the host maps to
/// <c>DeliveryUnknown{run_log_unwritable}</c>.</para>
/// <para><b>Append-only, one writer.</b> The peer's read loop is the only caller of <see cref="Append"/>;
/// the file is shared for read so a late subscriber can catch up while the run is live.</para>
/// </remarks>
public sealed class RunLogStore : IDisposable
{
    /// <summary>The stable code of a run-log line that could not be made durable.</summary>
    public const string UnwritableCode = "AIDE-RUN-LOG-UNWRITABLE";

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private readonly Stream _sink;
    private long _events;
    private long _bytes;

    /// <param name="sink">Where the lines go; flushed after every line.</param>
    /// <param name="path">The file the lines can be read back from, for a catch-up read.</param>
    public RunLogStore(Stream sink, string path)
    {
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentException.ThrowIfNullOrEmpty(path);
        _sink = sink;
        Path = path;
    }

    /// <summary>The run log's file.</summary>
    public string Path { get; }

    /// <summary>Lines made durable so far.</summary>
    public long Events => Interlocked.Read(ref _events);

    /// <summary>Bytes made durable so far.</summary>
    public long Bytes => Interlocked.Read(ref _bytes);

    /// <summary><c>&lt;ws&gt;/.aide/sessions/&lt;id&gt;/runs/&lt;dispatch_key&gt;.jsonl</c> — the one caller of the reserved path.</summary>
    public static string PathFor(string workspaceRoot, string sessionId, string dispatchKey) =>
        SessionPaths.RunLogFile(workspaceRoot, sessionId, dispatchKey);

    /// <summary>Opens (or continues) the attempt's run log for append.</summary>
    /// <exception cref="AgentPlaneException"><see cref="UnwritableCode"/> when the file cannot be opened.</exception>
    public static RunLogStore Open(string workspaceRoot, string sessionId, string dispatchKey)
    {
        var path = PathFor(workspaceRoot, sessionId, dispatchKey);
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            return new RunLogStore(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read), path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new AgentPlaneException(UnwritableCode, $"the run log '{path}' cannot be opened: {error.Message}");
        }
    }

    /// <summary>Appends one event's line and flushes it to disk.</summary>
    /// <exception cref="AgentPlaneException"><see cref="UnwritableCode"/> when the line could not be made durable.</exception>
    public void Append(RunEvent runEvent)
    {
        ArgumentNullException.ThrowIfNull(runEvent);

        var line = Utf8.GetBytes(JsonSerializer.Serialize(runEvent) + "\n");
        try
        {
            _sink.Write(line);
            if (_sink is FileStream file)
            {
                file.Flush(flushToDisk: true);
            }
            else
            {
                _sink.Flush();
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ObjectDisposedException or NotSupportedException)
        {
            throw new AgentPlaneException(UnwritableCode,
                $"run-log line seq {runEvent.Seq} could not be made durable in '{Path}': {error.Message}; it is not published");
        }

        Interlocked.Increment(ref _events);
        Interlocked.Add(ref _bytes, line.Length);
    }

    /// <summary>Every event in a run log, in order; empty when the file does not exist.</summary>
    public static IReadOnlyList<RunEvent> Read(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path))
        {
            return [];
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Utf8);
        var events = new List<RunEvent>();
        while (reader.ReadLine() is { } line)
        {
            if (line.Length > 0)
            {
                events.Add(JsonSerializer.Deserialize<RunEvent>(line)!);
            }
        }

        return events;
    }

    public void Dispose() => _sink.Dispose();
}
