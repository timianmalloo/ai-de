using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AiDe.Core.Watcher;

internal static class OfficialCoordinationErrors
{
    internal const string Descriptor = "COORD_OFFICIAL_DESCRIPTOR";
    internal const string Rebind = "COORD_OFFICIAL_REBIND";
    internal const string KeyCollision = "COORD_OFFICIAL_KEY_COLLISION";
    internal const string Unavailable = "COORD_OFFICIAL_UNAVAILABLE";
    internal const string Ingested = "CANONICAL_INGESTED";
    internal const string Legacy = "CANONICAL_LEGACY";
    internal const string Ignored = "CANONICAL_IGNORED";
}

// Internal deterministic collision seam; never read from serialized input.
internal enum OfficialHashMode { Sha256, Collision }

internal sealed class OfficialDescriptorAcquisition
{
    internal const string Epoch = "official-capture/1";
    internal const int MaxDescriptorBytes = 65536;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly CoordinationSourceBinding _binding;
    private readonly string _primary;
    private readonly string _checkout;
    private readonly CanonicalStreamIdentity[] _streams;
    private readonly OfficialHashMode _hashMode;
    private readonly byte[] _sourceKey;
    private readonly byte[] _descriptor;

    private OfficialDescriptorAcquisition(CoordinationSourceBinding binding, string primary, string checkout,
        CanonicalStreamIdentity[] streams, string sourcePath, OfficialHashMode hashMode)
    {
        _binding = binding;
        _primary = primary;
        _checkout = checkout;
        _streams = streams;
        _hashMode = hashMode;
        SourcePath = sourcePath;
        _sourceKey = Encode("XHB1", ["descriptor/1", Repository, RepositoryIdentity.Canonicalise(primary),
            RepositoryIdentity.Canonicalise(sourcePath), CanonicalCoordinationSourceBinding.Origin]);
        _descriptor = BuildDescriptor();
        if (_descriptor.Length > MaxDescriptorBytes)
            throw new CoordinationSourceException(OfficialCoordinationErrors.Descriptor);
        SourceId = Hash(_descriptor, hashMode);
        Scope = "official:" + SourceId;
    }

    internal string Repository => _binding.Repository.CanonicalPath;
    internal string SourcePath { get; }
    internal string SourceId { get; }
    internal string Scope { get; }
    internal ReadOnlySpan<byte> Descriptor => _descriptor;
    internal ReadOnlySpan<byte> PhysicalSourceKey => _sourceKey;

    internal static OfficialDescriptorAcquisition Acquire(CoordinationSourceBinding binding,
        string primary, string checkout, IReadOnlyList<CanonicalStreamIdentity> streams,
        OfficialHashMode hashMode = OfficialHashMode.Sha256)
    {
        if (binding?.Repository is null || binding.Origin != CanonicalCoordinationSourceBinding.Origin
            || streams is null || streams.Count is < 1 or > CanonicalCoordinationSourceBinding.MaxStreams
            || !Enum.IsDefined(hashMode))
            throw new CoordinationSourceException(OfficialCoordinationErrors.Descriptor);
        try
        {
            var pairs = streams.Take(CanonicalCoordinationSourceBinding.MaxStreams + 1).ToArray();
            if (pairs.Length != streams.Count || pairs.Length > CanonicalCoordinationSourceBinding.MaxStreams
                || pairs.Any(s => s is null || !CanonicalCoordinationRecord.IsText(s.RepositoryId)
                    || !CanonicalCoordinationRecord.IsText(s.StreamId))
                || pairs.Distinct().Count() != pairs.Length
                || Utf8.GetByteCount(binding.Repository.CanonicalPath) is < 1 or > 4096)
                throw new CoordinationSourceException(OfficialCoordinationErrors.Descriptor);
            pairs = pairs.OrderBy(s => Convert.ToHexString(Utf8.GetBytes(s.RepositoryId)), StringComparer.Ordinal)
                .ThenBy(s => Convert.ToHexString(Utf8.GetBytes(s.StreamId)), StringComparer.Ordinal).ToArray();
            var path = CanonicalCoordinationSourceBinding.ResolvePhysicalPath(binding, primary, checkout);
            return new(binding, Path.GetFullPath(primary), Path.GetFullPath(checkout), pairs, path, hashMode);
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException
            or UnauthorizedAccessException or WatcherException)
        {
            throw new CoordinationSourceException(OfficialCoordinationErrors.Descriptor);
        }
        catch (IOException error) when (error is not CoordinationSourceException)
        {
            throw new CoordinationSourceException(OfficialCoordinationErrors.Descriptor);
        }
    }

    internal void Validate()
    {
        try
        {
            if (!_descriptor.AsSpan().SequenceEqual(BuildDescriptor())
                || SourceId != Hash(_descriptor, _hashMode) || Scope != "official:" + SourceId
                || SourcePath != CanonicalCoordinationSourceBinding.ResolvePhysicalPath(_binding, _primary, _checkout))
                throw new CoordinationSourceException(OfficialCoordinationErrors.Descriptor);
        }
        catch (Exception error) when (error is UnauthorizedAccessException
            || error is IOException and not CoordinationSourceException)
        {
            throw new CoordinationSourceException(OfficialCoordinationErrors.Descriptor);
        }
    }

    internal bool Allows(string repository, string stream) => _streams.Contains(new(repository, stream));

    private byte[] BuildDescriptor() => [.. _sourceKey, .. Encode("", [
        CanonicalCoordinationCodec.Version, "allowance/1",
        _streams.Length.ToString(CultureInfo.InvariantCulture),
        .. _streams.SelectMany(s => new[] { s.RepositoryId, s.StreamId })])];

    private static byte[] Encode(string tag, IEnumerable<string> fields)
    {
        using var buffer = new MemoryStream();
        buffer.Write(Encoding.ASCII.GetBytes(tag));
        using var writer = new BinaryWriter(buffer, Utf8, leaveOpen: true);
        foreach (var field in fields)
        {
            var bytes = Utf8.GetBytes(field);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
        return buffer.ToArray();
    }

    internal static string Hash(ReadOnlySpan<byte> bytes, OfficialHashMode mode) =>
        mode == OfficialHashMode.Collision ? new('0', 64) : CoordinationSourceCapture.Hash(bytes);
}

internal sealed record OfficialCheckpoint(string Scope, string Epoch, long Offset, string PrefixDigest)
{
    internal static OfficialCheckpoint Empty(OfficialDescriptorAcquisition descriptor) =>
        new(descriptor.Scope, OfficialDescriptorAcquisition.Epoch, 0, CoordinationSourceCapture.Hash([]));
}

internal sealed record OfficialCapture(string Status, int Bytes, IReadOnlyList<OfficialCapturedPage> Pages);

// Only Capture can construct a page. All pages share privately owned immutable snapshot bytes.
internal sealed class OfficialCapturedPage
{
    internal const int MaxCaptureBytes = 32 * 1024 * 1024;
    internal const int MaxPageBytes = 4 * 1024 * 1024;
    internal const int MaxPageRecords = 128;
    private readonly byte[] _snapshot;
    private readonly OfficialDescriptorAcquisition _descriptor;

    private OfficialCapturedPage(OfficialDescriptorAcquisition descriptor, byte[] snapshot, int offset, int end)
    {
        _descriptor = descriptor;
        _snapshot = snapshot;
        Offset = offset;
        End = end;
    }

    internal int Offset { get; }
    internal int End { get; }
    internal int SnapshotLength => _snapshot.Length;
    internal bool IsStorable => true;
    internal ReadOnlySpan<byte> Descriptor => _descriptor.Descriptor;
    internal string Scope => _descriptor.Scope;
    internal string PrefixDigest(int end) => CoordinationSourceCapture.Hash(_snapshot.AsSpan(0, end));
    internal ReadOnlySpan<byte> Raw(int offset, int end) => _snapshot.AsSpan(offset, end - offset);

    internal static OfficialCapture Capture(OfficialDescriptorAcquisition descriptor, OfficialCheckpoint? expected = null)
    {
        descriptor.Validate();
        ValidateExpected(descriptor, expected);
        byte[] bytes;
        try
        {
            CanonicalCoordinationSourceBinding.Guard(descriptor.SourcePath, missingAllowed: true);
            using var source = new FileStream(descriptor.SourcePath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            if (source.Length > MaxCaptureBytes)
                throw new CoordinationSourceException(CoordinationErrors.Bounds);
            bytes = new byte[checked((int)source.Length)];
            source.ReadExactly(bytes);
            descriptor.Validate();
        }
        catch (IOException error) when (error is FileNotFoundException or DirectoryNotFoundException)
        {
            if (expected is not null) throw new CoordinationSourceException(CoordinationErrors.SourceGap);
            return new("Unavailable", 0, []);
        }
        catch (EndOfStreamException)
        {
            throw new CoordinationSourceException(CoordinationErrors.SourceGap);
        }
        catch (Exception error) when (error is UnauthorizedAccessException
            || error is IOException and not CoordinationSourceException)
        {
            throw new CoordinationSourceException(OfficialCoordinationErrors.Unavailable);
        }
        if (expected is not null && (expected.Offset > bytes.Length
            || CoordinationSourceCapture.Hash(bytes.AsSpan(0, (int)expected.Offset)) != expected.PrefixDigest
            || expected.Offset > 0 && bytes[(int)expected.Offset - 1] != (byte)'\n'))
            throw new CoordinationSourceException(CoordinationErrors.SourceGap);
        var pages = new List<OfficialCapturedPage>();
        var start = checked((int)(expected?.Offset ?? 0));
        var frameStart = start;
        var count = 0;
        for (var i = start; i < bytes.Length; i++)
        {
            if (bytes[i] != (byte)'\n') continue;
            var content = i - frameStart - (i > frameStart && bytes[i - 1] == (byte)'\r' ? 1 : 0);
            if (content > CanonicalCoordinationCodec.MaxContentBytes)
                throw new CoordinationSourceException(CoordinationErrors.Bounds);
            if (count == MaxPageRecords || i + 1 - start > MaxPageBytes)
            {
                pages.Add(new(descriptor, bytes, start, frameStart));
                start = frameStart;
                count = 0;
            }
            count++;
            frameStart = i + 1;
        }
        if (bytes.Length - frameStart > CanonicalCoordinationCodec.MaxContentBytes + 1)
            throw new CoordinationSourceException(CoordinationErrors.Bounds);
        if (count > 0 || pages.Count == 0) pages.Add(new(descriptor, bytes, start, frameStart));
        return new(bytes.Length == 0 ? "Empty" : frameStart < bytes.Length ? "DeferredTail" : "Captured",
            bytes.Length, pages.AsReadOnly());
    }

    internal static void ValidateExpected(OfficialDescriptorAcquisition descriptor, OfficialCheckpoint? expected)
    {
        if (expected is not null && (expected.Scope != descriptor.Scope
            || expected.Epoch != OfficialDescriptorAcquisition.Epoch || expected.Offset is < 0 or > MaxCaptureBytes
            || expected.PrefixDigest is not { Length: 64 } || !expected.PrefixDigest.All(Uri.IsHexDigit)))
            throw new CoordinationSourceException(CoordinationErrors.StaleSnapshot);
    }

    internal IEnumerable<(int Offset, int End)> Frames() => FramesFrom(Offset);

    internal IEnumerable<(int Offset, int End)> PrefixFrames() => FramesFrom(0);

    private IEnumerable<(int Offset, int End)> FramesFrom(int start)
    {
        for (var i = start; i < End; i++)
            if (_snapshot[i] == (byte)'\n')
            {
                yield return (start, i + 1);
                start = i + 1;
            }
    }
}

internal sealed record OfficialAdmission(long Admission, long CurrentReceipt, string State,
    string InterpretationReason, long Offset, long End, string RawDigest,
    CoordinationOccurrenceKind? OccurrenceKind, bool Replayed);
internal sealed record OfficialPageResult(OfficialCheckpoint Checkpoint, IReadOnlyList<OfficialAdmission> Admissions);
internal sealed record OfficialRunResult(CoordinationPumpStats Stats, string SourceStatus,
    OfficialCheckpoint? Checkpoint, IReadOnlyList<OfficialAdmission> Admissions, CoordinationReadResult Read)
{
    internal string Authorization => "Denied";
    internal string ResponseActions => "OfficialApiUnavailable";
}

internal static class CanonicalCoordinationOfficialRuntime
{
    internal static OfficialRunResult RunOnce(OfficialDescriptorAcquisition descriptor,
        SqliteWatcherObservationStore store, SessionRecord reader)
    {
        using var activity = new Activity("coordination.official.project").Start();
        var started = Stopwatch.GetTimestamp();
        var admissions = new List<OfficialAdmission>();
        OfficialCheckpoint? checkpoint = null;
        var sourceStatus = "Unavailable";
        string? diagnostic = null;
        try
        {
            checkpoint = store.ReadOfficialCheckpoint(descriptor);
            var capture = OfficialCapturedPage.Capture(descriptor, checkpoint);
            sourceStatus = capture.Status;
            if (sourceStatus == "Unavailable") diagnostic = OfficialCoordinationErrors.Unavailable;
            foreach (var page in capture.Pages)
            {
                var result = store.ProjectOfficialPage(descriptor, page, checkpoint);
                checkpoint = result.Checkpoint;
                admissions.AddRange(result.Admissions);
            }
        }
        catch (CoordinationSourceException error)
        {
            diagnostic = error.Code;
            sourceStatus = error.Code;
        }
        var stats = new CoordinationPumpStats(admissions.Count, admissions.Sum(a => a.End - a.Offset),
            admissions.Count(a => a.Replayed), 0, admissions.Count(a => a.State == "refused"),
            Stopwatch.GetElapsedTime(started).TotalMilliseconds, diagnostic);
        activity.SetTag("coordination.project.records", stats.Records);
        activity.SetTag("coordination.project.bytes", stats.Bytes);
        activity.SetTag("coordination.project.replayed", stats.Replayed);
        activity.SetTag("coordination.project.refused", stats.Refused);
        activity.SetTag("coordination.project.elapsed_ms", stats.ElapsedMilliseconds);
        activity.SetTag("error.type", diagnostic);
        activity.SetStatus(diagnostic is null ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
        return new(stats, sourceStatus, checkpoint, admissions.AsReadOnly(),
            store.ReadCoordination(new(descriptor.SourceId, null, 200, reader)));
    }
}

internal sealed record OfficialPreparedFrame(byte[] Raw, byte[]? Canonical, byte[] Identity,
    string Key, string Digest, string Reason)
{
    internal string State => Canonical is null ? "refused" : "applied";

    internal static OfficialPreparedFrame Prepare(OfficialDescriptorAcquisition descriptor,
        OfficialCapturedPage page, int offset, int end, OfficialHashMode hashMode)
    {
        var raw = page.Raw(offset, end).ToArray();
        var content = raw.AsSpan(0, raw.Length - (raw.AsSpan().EndsWith("\r\n"u8) ? 2 : 1));
        var digest = CoordinationSourceCapture.Hash(raw);
        byte[]? canonical = null;
        var tag = "XHI1";
        var reason = OfficialCoordinationErrors.Ignored;
        string[] identity = [Convert.ToHexString(descriptor.PhysicalSourceKey),
            offset.ToString(CultureInfo.InvariantCulture), digest];
        try
        {
            if (!IsBlank(content))
            {
                using var document = CanonicalCoordinationCodec.Read(content);
                var body = document.RootElement;
                if (body.ValueKind != JsonValueKind.Object) throw new CanonicalInputException(CanonicalErrors.Schema);
                if (body.TryGetProperty("at", out var at) && (at.ValueKind != JsonValueKind.Number
                    || !at.TryGetDouble(out var n) || !double.IsFinite(n)))
                    throw new CanonicalInputException(CanonicalErrors.Field);
                if (body.TryGetProperty("id", out var id) && id.ValueKind != JsonValueKind.String)
                    throw new CanonicalInputException(CanonicalErrors.Schema);
                var kind = body.TryGetProperty("kind", out var kindValue) && kindValue.ValueKind == JsonValueKind.String
                    ? kindValue.GetString() : null;
                if (body.TryGetProperty("schemaVersion", out _) || kind?.StartsWith("coordination-v", StringComparison.Ordinal) == true)
                {
                    var parsed = CanonicalCoordinationRecord.Parse(content);
                    if (parsed.Record is not { IsLegacy: false } record) throw new CanonicalInputException(parsed.Code);
                    if (!descriptor.Allows(record.RepositoryId!, record.StreamId!))
                        throw new CanonicalInputException(CoordinationBindingErrors.Mismatch);
                    canonical = record.Canonical.ToArray();
                    tag = "XHE1";
                    identity = [Hex(record.RepositoryId!), Hex(record.StreamId!), Hex(record.EventId!)];
                    reason = OfficialCoordinationErrors.Ingested;
                }
                else if (kind is "request-add" or "request-resolve" && id.ValueKind == JsonValueKind.String
                    && id.GetString()!.Length > 0)
                {
                    canonical = content.ToArray();
                    tag = "XHL1";
                    reason = OfficialCoordinationErrors.Legacy;
                }
            }
        }
        catch (CanonicalInputException error)
        {
            reason = error.Code;
        }
        catch (Exception error) when (error is JsonException or DecoderFallbackException or EncoderFallbackException)
        {
            reason = CanonicalErrors.Schema;
        }
        var full = Encoding.ASCII.GetBytes(tag + "|" + Hex(descriptor.Repository) + "|"
            + Hex(CanonicalCoordinationSourceBinding.Origin) + "|" + string.Join("|", identity));
        if (full.Length > 65536 || canonical?.Length > 65536)
            throw new CoordinationSourceException(CoordinationErrors.Bounds);
        return new(raw, canonical, full, "official:" + OfficialDescriptorAcquisition.Hash(full, hashMode), digest, reason);
    }

    private static string Hex(string value) => Convert.ToHexString(new UTF8Encoding(false, true).GetBytes(value));
    private static bool IsBlank(ReadOnlySpan<byte> content)
    {
        foreach (var value in content)
            if (value is not (9 or 10 or 11 or 12 or 13 or 32)) return false;
        return true;
    }
}
