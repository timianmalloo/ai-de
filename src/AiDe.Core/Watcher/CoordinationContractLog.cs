using System.Text;
using System.Text.Json;

namespace AiDe.Core.Watcher;

/// <summary>
/// Writes injected-contract events for one or more non-AI-Forward sessions to a coord-core-shaped
/// append log (spike S4): one file per session (<c>&lt;dir&gt;/&lt;session&gt;.jsonl</c>), one JSON object
/// per line, <c>seq</c> auto-assigned, an atomic single-write append, and the <b>LOG-A</b> guard - a
/// leading newline when the file did not already end in one, so a fused line is impossible to express.
/// This is the session-side half of the contract; <see cref="InjectedContractIngest"/> is the ingest half.
/// </summary>
public sealed class CoordContractWriter
{
    private readonly string _logDir;
    private readonly TimeProvider _time;

    public CoordContractWriter(string logDir, TimeProvider? time = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(logDir);
        _logDir = logDir;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>Writes a registration with the same <see cref="OtelAttributes"/> keys as the OTLP path.</summary>
    public void WriteRegister(string externalSessionId, IReadOnlyDictionary<string, string?> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        Append(externalSessionId, "register", attributes);
    }

    public void WriteHeartbeat(string externalSessionId) => Append(externalSessionId, "heartbeat", null);

    public void WriteSessionEnd(string externalSessionId) => Append(externalSessionId, "session-end", null);

    private void Append(string session, string kind, IReadOnlyDictionary<string, string?>? attributes)
    {
        ArgumentException.ThrowIfNullOrEmpty(session);
        Directory.CreateDirectory(_logDir);
        var file = Path.Combine(_logDir, session + ".jsonl");

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["kind"] = kind,
            ["contract"] = CoordContract.Version,
            ["session"] = session,
            ["at"] = _time.GetUtcNow().ToUnixTimeMilliseconds() / 1000.0,
            ["seq"] = NextSeq(file),
        };
        if (attributes is not null)
        {
            payload["attrs"] = attributes.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
        }

        var line = JsonSerializer.Serialize(payload);
        // LOG-A: prepend a newline when the file does not already end in one, so a record left
        // unterminated by a crash or a hand edit cannot fuse with this one (control ladder rung 1).
        var text = (NeedsLeadingNewline(file) ? "\n" : "") + line + "\n";
        var bytes = Encoding.UTF8.GetBytes(text);

        // One Write under FileMode.Append (O_APPEND) is atomic: a concurrent writer cannot interleave
        // a partial line (mirrors the coord-core writer, spike S3/S4).
        using var stream = new FileStream(file, FileMode.Append, FileAccess.Write, FileShare.Read);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static bool NeedsLeadingNewline(string file)
    {
        if (!File.Exists(file))
        {
            return false;
        }

        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        if (stream.Length == 0)
        {
            return false;
        }

        stream.Seek(-1, SeekOrigin.End);
        var last = stream.ReadByte();
        return last is not ('\n' or '\r');
    }

    private static int NextSeq(string file)
    {
        if (!File.Exists(file))
        {
            return 1;
        }

        var count = 0;
        foreach (var line in File.ReadLines(file))
        {
            if (line.Trim().Length > 0)
            {
                count++;
            }
        }

        return count + 1;
    }
}

/// <summary>Reads a coord-core append log directory into ordered contract events (stdlib, tolerant).</summary>
public static class CoordContractLog
{
    /// <summary>
    /// Reads every <c>*.jsonl</c> in <paramref name="logDir"/> and parses them into one ordered event
    /// stream (<see cref="CoordContractParser"/> sorts by <c>(at, session, seq)</c>). A missing directory
    /// yields an empty list; a malformed line in any file is skipped and counted by the parser.
    /// </summary>
    public static IReadOnlyList<CoordContractEvent> ReadDirectory(string logDir)
    {
        ArgumentException.ThrowIfNullOrEmpty(logDir);
        if (!Directory.Exists(logDir))
        {
            return [];
        }

        var builder = new StringBuilder();
        foreach (var file in Directory.EnumerateFiles(logDir, "*.jsonl").OrderBy(f => f, StringComparer.Ordinal))
        {
            builder.Append(File.ReadAllText(file));
            builder.Append('\n'); // guarantee a separator between files (defence in depth for LOG-A)
        }

        return CoordContractParser.Parse(builder.ToString());
    }
}

/// <summary>
/// Reads a contract log directory and applies it to an <see cref="InjectedContractIngest"/>. Re-running
/// is safe: the adapter is idempotent (a duplicate register is ignored, a heartbeat merely refreshes
/// liveness), so a whole-directory re-read never double-registers - which is why a naive "read it all"
/// pump is correct here without tracking file offsets.
/// </summary>
public sealed class CoordContractLogPump(string logDir, InjectedContractIngest ingest)
{
    private readonly string _logDir = !string.IsNullOrEmpty(logDir) ? logDir : throw new ArgumentException("logDir is required", nameof(logDir));
    private readonly InjectedContractIngest _ingest = ingest ?? throw new ArgumentNullException(nameof(ingest));

    /// <summary>Reads the log directory once and applies every event; returns the count applied.</summary>
    public int PumpOnce()
    {
        var events = CoordContractLog.ReadDirectory(_logDir);
        _ingest.ApplyAll(events);
        return events.Count;
    }
}
