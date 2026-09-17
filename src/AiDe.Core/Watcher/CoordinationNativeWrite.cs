using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AiDe.Core.Watcher;

public enum CoordinationWriteStatus { Ready, Admitted, Refused, Unavailable, Uncertain }

/// <summary>Transport outcomes, not event-semantic acceptance by ingest.</summary>
public sealed record CoordinationWriteResult(
    CoordinationWriteStatus Status, string? Code = null,
    PreparedWrite? Prepared = null, CoordinationAdmission? Admission = null);

public sealed record CoordinationAdmission(string Session, int Sequence, long Start, int ByteCount);

/// <summary>Immutable exact bytes and source evidence; only the writer can create an attempt.</summary>
public sealed class PreparedWrite
{
    internal PreparedWrite(string root, string file, string session, int sequence,
        long start, byte[] prefix, byte[] bytes, bool existed)
    {
        Root = root;
        File = file;
        Prefix = prefix;
        Bytes = bytes;
        Existed = existed;
        Admission = new(session, sequence, start, bytes.Length);
    }

    public CoordinationAdmission Admission { get; }
    public byte[] CopyBytes() => (byte[])Bytes.Clone();
    internal string Root { get; }
    internal string File { get; }
    internal byte[] Prefix { get; }
    internal byte[] Bytes { get; }
    internal bool Existed { get; }
    internal bool Attempted { get; set; }
}

public sealed class CoordinationWriteException(string code, PreparedWrite? prepared = null)
    : IOException(code)
{
    public string Code { get; } = code;
    public PreparedWrite? Prepared { get; } = prepared;
}

public static class CoordinationWriteCodes
{
    public const string RecordBound = "COORD_RECORD_BOUND";
    public const string FileBound = "COORD_FILE_BOUND";
    public const string RootBound = "COORD_ROOT_BOUND";
    public const string SequenceConflict = "COORD_SEQUENCE_CONFLICT";
    public const string StalePreparation = "COORD_STALE_PREPARATION";
    public const string IdentityConflict = "COORD_IDENTITY_CONFLICT";
    public const string WriterBusy = "COORD_WRITER_BUSY";
    public const string SourceUnavailable = "COORD_SOURCE_UNAVAILABLE";
    public const string RootOverrun = "COORD_ROOT_OVERRUN";
    public const string WriteUncertain = "COORD_WRITE_UNCERTAIN";
}

public sealed partial class CoordContractWriter
{
    public const int MaximumLineBytes = 65_536;
    public const int MaximumFiles = 128;
    public const long MaximumRootBytes = 33_554_432;
    internal const string RootLockFile = ".coordlock";
    private static readonly object RootTableLock = new();
    private static readonly Dictionary<string, RootGate> RootGates = new(StringComparer.Ordinal);

    // Fault injection only: real FileStream writes/flushes remain the production boundary.
    internal Action<FileStream, ReadOnlyMemory<byte>>? WriteFault { get; init; }
    internal Action<FileStream>? FlushFault { get; init; }
    internal Action? DisposeFault { get; init; }

    /// <summary>Copies and bounds input before exclusion; never writes/reserves accepted log bytes.</summary>
    public CoordinationWriteResult Prepare(string kind, string session,
        IReadOnlyDictionary<string, string?>? attributes = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(kind);
        ArgumentException.ThrowIfNullOrEmpty(session);
        return Observe("prepare", null, () =>
        {
            var copied = CopyBounded(kind, session, attributes);
            // The caller's clock is outside exclusion: independent sessions can progress.
            var at = _time.GetUtcNow().ToUnixTimeMilliseconds() / 1000.0;
            using var guard = AcquireRoot();
            var file = Path.Combine(_logDir, FileNameFor(session));
            var quota = ReadQuota(file);
            using var target = quota.Exists
                ? new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
            var source = ReadSource(target);
            var sequence = NextSequence(source, session);
            var bytes = Encode(kind, session, copied, at, sequence);
            if (source.Length > 0 && source[^1] is not ((byte)'\n') and not ((byte)'\r'))
            {
                ValidateTail(source);
                bytes = [(byte)'\n', .. bytes];
            }
            CheckQuota(quota, bytes.Length);
            return new(CoordinationWriteStatus.Ready, Prepared: new PreparedWrite(
                CoordinationSourceCapture.RootKey(_logDir), file, session, sequence,
                source.Length, SHA256.HashData(source), bytes, quota.Exists));
        });
    }

    /// <summary>Admits the exact preparation or preserves it as uncertain. Never rebases an intent.</summary>
    public CoordinationWriteResult Append(PreparedWrite prepared)
    {
        ArgumentNullException.ThrowIfNull(prepared);
        return Observe("append", prepared, () =>
        {
            using var guard = AcquireRoot();
            if (prepared.Root != CoordinationSourceCapture.RootKey(_logDir))
            {
                throw new CoordinationWriteException(CoordinationWriteCodes.StalePreparation);
            }
            var quota = ReadQuota(prepared.File);
            if (!quota.Exists && (prepared.Existed || prepared.Attempted))
            {
                throw new CoordinationWriteException(CoordinationWriteCodes.StalePreparation);
            }
            // Bound admission before OpenOrCreate can introduce a new accepted-log file.
            if (!quota.Exists)
            {
                CheckQuota(quota, prepared.Bytes.Length);
            }
            using (var target = new FileStream(prepared.File, FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.Read))
            {
                var source = ReadSource(target);
                var offset = ReconcileOffset(prepared, source);
                CheckQuota(quota, prepared.Bytes.Length - offset);
                if (!prepared.Attempted && NextSequence(source, prepared.Admission.Session) != prepared.Admission.Sequence)
                {
                    throw new CoordinationWriteException(CoordinationWriteCodes.SequenceConflict);
                }
                target.Position = source.Length;
                prepared.Attempted = true;
                if (offset < prepared.Bytes.Length)
                {
                    var remaining = prepared.Bytes.AsMemory(offset);
                    if (WriteFault is { } write) write(target, remaining);
                    else target.Write(remaining.Span);
                }
                if (FlushFault is { } flush) flush(target);
                else target.Flush(flushToDisk: true);
            }
            DisposeFault?.Invoke();
            return new(CoordinationWriteStatus.Admitted, Admission: prepared.Admission);
        });
    }

    private static int ReconcileOffset(PreparedWrite prepared, byte[] source)
    {
        var start = checked((int)prepared.Admission.Start);
        if (source.Length < start ||
            !SHA256.HashData(source.AsSpan(0, start)).AsSpan().SequenceEqual(prepared.Prefix))
        {
            throw new CoordinationWriteException(CoordinationWriteCodes.StalePreparation);
        }
        var added = source.Length - start;
        if (!prepared.Attempted)
        {
            if (added != 0)
                throw new CoordinationWriteException(CoordinationWriteCodes.StalePreparation);
            return 0;
        }
        var compared = Math.Min(added, prepared.Bytes.Length);
        if (!source.AsSpan(start, compared).SequenceEqual(prepared.Bytes.AsSpan(0, compared)))
            throw new CoordinationWriteException(CoordinationWriteCodes.StalePreparation);
        return compared;
    }

    private static Dictionary<string, string?>? CopyBounded(string kind, string session,
        IReadOnlyDictionary<string, string?>? attributes)
    {
        long size = (long)kind.Length + session.Length;
        if (size > MaximumLineBytes) throw new CoordinationWriteException(CoordinationWriteCodes.RecordBound);
        if (attributes is null) return null;
        var copy = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in attributes)
        {
            size += (long)key.Length + (value?.Length ?? 4) + 4;
            if (size > MaximumLineBytes) throw new CoordinationWriteException(CoordinationWriteCodes.RecordBound);
            copy.Add(key, value);
        }
        return copy;
    }

    private static byte[] Encode(string kind, string session, Dictionary<string, string?>? attributes,
        double at, int sequence)
    {
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["kind"] = kind, ["contract"] = CoordContract.Version, ["session"] = session,
            ["at"] = at, ["seq"] = sequence,
        };
        if (attributes is not null) payload["attrs"] = attributes;
        // Stream counts actual output, not Utf8JsonWriter's conservative reservation size.
        using var output = new BoundedOutput();
        using (var json = new Utf8JsonWriter(output))
        {
            JsonSerializer.Serialize(json, payload);
        }
        output.WriteByte((byte)'\n');
        return output.ToArray();
    }

    private sealed class BoundedOutput : Stream
    {
        private readonly MemoryStream _output = new(MaximumLineBytes);
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _output.Length;
        public override long Position { get => _output.Position; set => throw new NotSupportedException(); }
        public byte[] ToArray() => _output.ToArray();
        public override void Flush() => _output.Flush();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (Length + buffer.Length > MaximumLineBytes)
                throw new CoordinationWriteException(CoordinationWriteCodes.RecordBound);
            _output.Write(buffer);
        }
        public override void WriteByte(byte value)
        {
            if (Length == MaximumLineBytes)
                throw new CoordinationWriteException(CoordinationWriteCodes.RecordBound);
            _output.WriteByte(value);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) _output.Dispose();
            base.Dispose(disposing);
        }
    }

    // Scan top-level *.jsonl, no recursion, at most 129 entries. No allowlist:
    // the non-JSONL .coordlock is excluded by the matching token itself.
    private (int Count, long Bytes, bool Exists) ReadQuota(string target)
    {
        var files = Directory.EnumerateFiles(_logDir, "*.jsonl").Take(MaximumFiles + 1).ToArray();
        long bytes = 0;
        var exists = false;
        var key = CoordinationSourceCapture.RootKey(target);
        foreach (var file in files)
        {
            bytes += new FileInfo(file).Length;
            exists |= key == CoordinationSourceCapture.RootKey(file);
        }
        if (files.Length > MaximumFiles || bytes > MaximumRootBytes)
            throw new CoordinationWriteException(CoordinationWriteCodes.RootOverrun);
        return (files.Length, bytes, exists);
    }

    private static void CheckQuota((int Count, long Bytes, bool Exists) quota, int added)
    {
        if (!quota.Exists && quota.Count >= MaximumFiles)
            throw new CoordinationWriteException(CoordinationWriteCodes.FileBound);
        if (quota.Bytes + added > MaximumRootBytes)
            throw new CoordinationWriteException(CoordinationWriteCodes.RootBound);
    }

    private static byte[] ReadSource(FileStream? stream)
    {
        if (stream is null) return [];
        if (stream.Length > MaximumRootBytes)
            throw new CoordinationWriteException(CoordinationWriteCodes.RootOverrun);
        stream.Position = 0;
        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        return bytes;
    }

    private static int NextSequence(byte[] source, string session)
    {
        var count = 0;
        var sequences = new HashSet<int>();
        var identified = false;
        var remaining = source.AsSpan();
        while (!remaining.IsEmpty)
        {
            var newline = remaining.IndexOfAny((byte)'\n', (byte)'\r');
            var length = newline < 0 ? remaining.Length : newline;
            if (length > MaximumLineBytes)
                throw new CoordinationWriteException(CoordinationWriteCodes.SourceUnavailable);
            var line = Encoding.UTF8.GetString(remaining[..length]).Trim();
            remaining = newline < 0 ? [] : remaining[(newline + 1)..];
            if (line.Length == 0) continue;
            count++;
            try
            {
                using var document = JsonDocument.Parse(line);
                if (document.RootElement.ValueKind != JsonValueKind.Object ||
                    !document.RootElement.TryGetProperty("session", out var identity)) continue;
                if (identity.ValueKind != JsonValueKind.String || identity.GetString() != session)
                    throw new CoordinationWriteException(CoordinationWriteCodes.IdentityConflict);
                identified = true;
                if (document.RootElement.TryGetProperty("seq", out var seq) &&
                    seq.ValueKind == JsonValueKind.Number && seq.TryGetInt32(out var value))
                    sequences.Add(value);
            }
            catch (JsonException) { /* Complete malformed legacy lines still count. */ }
        }
        if (count > 0 && !identified)
            throw new CoordinationWriteException(CoordinationWriteCodes.IdentityConflict);
        if (sequences.Contains(count + 1))
            throw new CoordinationWriteException(CoordinationWriteCodes.SequenceConflict);
        return count + 1;
    }

    private static void ValidateTail(byte[] source)
    {
        var start = source.AsSpan().LastIndexOfAny((byte)'\n', (byte)'\r') + 1;
        try { using var document = JsonDocument.Parse(source.AsMemory(start)); }
        catch (JsonException) { throw new CoordinationWriteException(CoordinationWriteCodes.SourceUnavailable); }
    }

    private CoordinationWriteResult Observe(string operation, PreparedWrite? prepared,
        Func<CoordinationWriteResult> action)
    {
        using var activity = new Activity("coordination.native." + operation).Start();
        var started = Stopwatch.GetTimestamp();
        CoordinationWriteResult result;
        try { result = action(); }
        catch (CoordinationWriteException error)
        {
            var unavailable = error.Code is CoordinationWriteCodes.WriterBusy or
                CoordinationWriteCodes.SourceUnavailable or CoordinationWriteCodes.RootOverrun;
            result = Failure(prepared, error.Code, unavailable);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            result = Failure(prepared, CoordinationWriteCodes.SourceUnavailable, true);
        }
        activity.SetTag("coordination.outcome", result.Status.ToString());
        activity.SetTag("coordination.bytes", result.Admission?.ByteCount ?? result.Prepared?.Admission.ByteCount ?? 0);
        activity.SetTag("coordination.duration_ms", Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        activity.SetTag("error.type", result.Code);
        activity.SetStatus(result.Code is null ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
        return result;
    }

    private static CoordinationWriteResult Failure(PreparedWrite? prepared, string code, bool unavailable) =>
        prepared?.Attempted == true
            ? new(CoordinationWriteStatus.Uncertain, CoordinationWriteCodes.WriteUncertain, prepared)
            : new(unavailable ? CoordinationWriteStatus.Unavailable : CoordinationWriteStatus.Refused, code);

    // Pattern: scoped keyed exclusion. References include acquisitions in flight and are
    // retired only after OS exclusion is released; no PID/TTL or manual lockfile deletion.
    private RootLease AcquireRoot()
    {
        var key = CoordinationSourceCapture.RootKey(_logDir);
        RootGate gate;
        lock (RootTableLock)
        {
            if (!RootGates.TryGetValue(key, out gate!))
                RootGates.Add(key, gate = new RootGate());
            gate.References++;
        }
        var lease = new RootLease(key, gate);
        try
        {
            if (!Monitor.TryEnter(gate.Sync))
                throw new CoordinationWriteException(CoordinationWriteCodes.WriterBusy);
            lease.Entered = true;
            Directory.CreateDirectory(_logDir);
            try
            {
                lease.File = new FileStream(Path.Combine(_logDir, RootLockFile),
                    FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException error) when ((error.HResult & 0xffff) is 32 or 33 or 11)
            {
                throw new CoordinationWriteException(CoordinationWriteCodes.WriterBusy);
            }
            return lease;
        }
        catch { lease.Dispose(); throw; }
    }

    private sealed class RootGate
    {
        public object Sync { get; } = new();
        public int References;
    }

    private sealed class RootLease(string key, RootGate gate) : IDisposable
    {
        public bool Entered { get; set; }
        public FileStream? File { get; set; }
        public void Dispose()
        {
            try { File?.Dispose(); }
            finally
            {
                if (Entered) Monitor.Exit(gate.Sync);
                lock (RootTableLock)
                {
                    if (--gate.References == 0) RootGates.Remove(key);
                }
            }
        }
    }
}
