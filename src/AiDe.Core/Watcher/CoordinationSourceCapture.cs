using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AiDe.Core.Watcher;

public sealed record CoordinationPumpStats(
    int Records, long Bytes, int Replayed, int Pending, int Refused, double ElapsedMilliseconds, string? Diagnostic);

internal static class CoordinationErrors
{
    internal const string SourceGap = "COORD_SOURCE_GAP";
    internal const string Bounds = "COORD_SOURCE_BOUND";
    internal const string DuplicateConflict = "COORD_DUPLICATE_CONFLICT";
    internal const string StaleSnapshot = "COORD_STALE_SNAPSHOT";
}

internal sealed class CoordinationSourceException(string code) : IOException(code)
{
    internal string Code { get; } = code;
}

internal sealed record CoordinationCheckpoint(long Offset, string Digest);
internal sealed record CoordinationRecord(
    long Offset, long End, byte[] Raw, string Digest, string Key,
    byte[] Canonical, CoordContractEvent? Event, SessionBinding? Binding, string? Refusal);
internal sealed record CoordinationPage(string Scope, IReadOnlyList<CoordinationRecord> Records, string PrefixDigest);
internal sealed record CoordinationCapture(
    long Bytes, int Recognized, CoordinationCheckpoint? Checkpoint, IReadOnlyList<CoordinationPage> Pages);

/// <summary>
/// Bounded immutable LF capture. The directory is supplied by trusted composition, never a payload.
/// Optimistic prefix validation does not promise filesystem exclusion or detect edit-and-restore ABA.
/// </summary>
internal static class CoordinationSourceCapture
{
    internal const string Epoch = "legacy-native-1";
    private const int MaxFiles = 128;
    private const int MaxCapture = 32 * 1024 * 1024;
    private const int MaxRecord = 64 * 1024;
    private const int MaxPageRecords = 128;
    private const int MaxPageBytes = 4 * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static string RootKey(string root) => Encode(Normalize(root)) + "|";
    private static string Normalize(string path) =>
        OperatingSystem.IsWindows() ? Path.GetFullPath(path).ToUpperInvariant() : Path.GetFullPath(path);
    private static string Encode(string value) => Convert.ToBase64String(StrictUtf8.GetBytes(value));
    internal static byte[] Canonical(CoordContractEvent value) => JsonSerializer.SerializeToUtf8Bytes(value, value.GetType());

    internal static IReadOnlyList<CoordinationCapture> Read(
        string root, SqliteWatcherObservationStore store, IngestHost host)
    {
        var prefix = RootKey(root);
        var checkpoints = store.CoordinationCheckpoints(prefix);
        var files = Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*.jsonl").Take(MaxFiles + 1).Order(StringComparer.Ordinal).ToArray()
            : [];
        if (files.Length > MaxFiles)
        {
            throw new CoordinationSourceException(CoordinationErrors.Bounds);
        }
        var scopes = files.ToDictionary(file => prefix + Encode(Normalize(file)), StringComparer.Ordinal);
        if (checkpoints.Keys.Any(scope => !scopes.ContainsKey(scope)))
        {
            throw new CoordinationSourceException(CoordinationErrors.SourceGap);
        }
        var captures = new List<CoordinationCapture>();
        var remaining = MaxCapture;
        foreach (var (scope, file) in scopes)
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (stream.Length > remaining)
            {
                throw new CoordinationSourceException(CoordinationErrors.Bounds);
            }
            var bytes = new byte[checked((int)stream.Length)];
            stream.ReadExactly(bytes);
            remaining -= bytes.Length;
            checkpoints.TryGetValue(scope, out var checkpoint);
            if (checkpoint is not null && (checkpoint.Offset > bytes.Length
                || Hash(bytes.AsSpan(0, checked((int)checkpoint.Offset))) != checkpoint.Digest))
            {
                throw new CoordinationSourceException(CoordinationErrors.SourceGap);
            }
            captures.Add(Capture(scope, bytes, checkpoint, host));
        }
        return captures;
    }

    private static CoordinationCapture Capture(
        string scope, byte[] bytes, CoordinationCheckpoint? checkpoint, IngestHost host)
    {
        var pages = new List<CoordinationPage>();
        var page = new List<CoordinationRecord>();
        var start = 0;
        var pageBytes = 0;
        var recognized = 0;
        using var prefixHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        for (var end = 0; end < bytes.Length; end++)
        {
            if (bytes[end] != (byte)'\n')
            {
                continue;
            }
            var length = end + 1 - start;
            if (length > MaxRecord)
            {
                throw new CoordinationSourceException(CoordinationErrors.Bounds);
            }
            if (page.Count > 0 && (page.Count == MaxPageRecords || pageBytes + length > MaxPageBytes))
            {
                pages.Add(new(scope, page.ToArray(), Convert.ToHexString(prefixHash.GetCurrentHash())));
                page.Clear();
                pageBytes = 0;
            }
            var raw = bytes.AsSpan(start, length).ToArray();
            var record = Prepare(start, end + 1, raw, host);
            recognized += record.Event is not null ? 1 : 0;
            page.Add(record);
            pageBytes += length;
            prefixHash.AppendData(raw);
            start = end + 1;
        }
        if (page.Count > 0)
        {
            pages.Add(new(scope, page.ToArray(), Convert.ToHexString(prefixHash.GetCurrentHash())));
        }
        return new(bytes.Length, recognized, checkpoint, pages);
    }

    private static CoordinationRecord Prepare(long offset, long end, byte[] raw, IngestHost host)
    {
        CoordContractEvent? value = null;
        SessionBinding? binding = null;
        var key = "position:" + offset.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string? refusal = null;
        try
        {
            var text = StrictUtf8.GetString(raw);
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || root.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count()
                    != root.EnumerateObject().Count()
                || !root.TryGetProperty("session", out var session) || session.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(session.GetString())
                || !root.TryGetProperty("seq", out var seq) || !seq.TryGetInt32(out var sequence) || sequence < 1)
            {
                throw new JsonException();
            }
            key = Encode(session.GetString()!) + ":" + sequence.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (Encoding.UTF8.GetByteCount(key) > 512)
            {
                throw new JsonException();
            }
            if (root.TryGetProperty("attrs", out var attrs)
                && (attrs.ValueKind != JsonValueKind.Object
                    || attrs.EnumerateObject().Any(property => property.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                    || attrs.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count()
                        != attrs.EnumerateObject().Count()))
            {
                throw new JsonException();
            }
            var parsed = CoordContractParser.Parse(text, out var stats);
            value = parsed.SingleOrDefault();
            refusal = stats.VersionRejected > 0 ? "UNSUPPORTED_VERSION" : value is null ? "UNSUPPORTED_RECORD" : null;
            if (value is ContractRegister registration)
            {
                binding = host.PrepareObservation(registration);
            }
        }
        catch (Exception error) when (error is JsonException or DecoderFallbackException
            or InvalidOperationException or FormatException or OverflowException or WatcherException)
        {
            value = null;
            refusal = "MALFORMED_RECORD";
            key = "position:" + offset.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        var canonical = value is null ? raw : Canonical(value);
        if (canonical.Length > MaxRecord)
        {
            canonical = raw;
            value = null;
            refusal = "CANONICAL_BOUND";
        }
        return new(offset, end, raw, Hash(raw), key, canonical, value, binding, refusal);
    }

    internal static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}
