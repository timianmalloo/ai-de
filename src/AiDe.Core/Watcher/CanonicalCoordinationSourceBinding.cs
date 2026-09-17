using System.Diagnostics;
using System.Text;

namespace AiDe.Core.Watcher;

internal sealed record CanonicalStreamIdentity(string RepositoryId, string StreamId);
internal sealed record CanonicalSourceContext(
    CoordinationSourceBinding Binding, string PrimaryPath, string CheckoutPath,
    string SourceId, IReadOnlyList<CanonicalStreamIdentity> Streams);
internal enum CanonicalBindingStatus { Bound, Invalid, Unsupported, Unbound, Unavailable, OriginBound }
internal sealed record CanonicalBindingResult(
    CanonicalBindingStatus Status, string Code, CanonicalParseResult Parsed,
    string? SourceId, string? SourcePath, string? Scope, double ElapsedMilliseconds)
{
    // Snapshot results are not capture capabilities; only OfficialCapturedPage is storable.
    internal bool IsStorable => false;
}

/// <summary>
/// Snapshot-only synthetic binding. Never opens request content, a database or an event-supplied path.
/// Reparses are refused; this is not a held-handle race-free live source contract.
/// </summary>
internal static class CanonicalCoordinationSourceBinding
{
    internal const string Origin = "canonical-requests-v1";
    internal const int MaxStreams = 16;
    private const int MaxPointer = 4096;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static CanonicalBindingResult Bind(ReadOnlySpan<byte> source, CanonicalSourceContext? context)
    {
        var started = Stopwatch.GetTimestamp();
        var parsed = CanonicalCoordinationRecord.Parse(source);
        CanonicalBindingResult Result(CanonicalBindingStatus status, string code,
            string? path = null, string? scope = null) =>
            new(status, code, parsed, path is null ? null : context!.SourceId,
                path, scope, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        if (parsed.Record is not { } record)
            return Result(parsed.Status == CanonicalRecordStatus.Unsupported
                ? CanonicalBindingStatus.Unsupported : CanonicalBindingStatus.Invalid, parsed.Code);
        if (context is null || context.Binding is null || context.Streams is null
            || context.Streams.Count is < 1 or > MaxStreams
            || !CanonicalCoordinationRecord.IsText(context.SourceId)
            || context.Binding.Origin != Origin)
            return Result(CanonicalBindingStatus.Unbound, CoordinationBindingErrors.Invalid);
        var streams = context.Streams.ToArray();
        if (streams.Any(s => s is null || !CanonicalCoordinationRecord.IsText(s.RepositoryId)
                || !CanonicalCoordinationRecord.IsText(s.StreamId))
            || streams.Distinct().Count() != streams.Length
            || (!record.IsLegacy && !streams.Contains(new(record.RepositoryId!, record.StreamId!))))
            return Result(CanonicalBindingStatus.Unbound, CoordinationBindingErrors.Mismatch);
        try
        {
            var path = ResolvePhysicalPath(context.Binding, context.PrimaryPath, context.CheckoutPath);
            Guard(path);
            if (!File.Exists(path)) return Result(CanonicalBindingStatus.Unavailable, CoordinationBindingErrors.Invalid);
            var scope = CanonicalCoordinationCodec.Scope(context.Binding.Repository.CanonicalPath,
                Origin, RepositoryIdentity.Canonicalise(path), CanonicalCoordinationCodec.Version);
            return Result(record.Raw.Length > 65536 ? CanonicalBindingStatus.OriginBound : CanonicalBindingStatus.Bound,
                record.Raw.Length > 65536 ? "COORD_CANONICAL_ORIGIN_BOUND" : "COORD_CANONICAL_BOUND", path, scope);
        }
        catch (CoordinationSourceException error) when (error.Code == CoordinationBindingErrors.Mismatch)
        {
            return Result(CanonicalBindingStatus.Unbound, error.Code);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
            or ArgumentException or NotSupportedException or WatcherException)
        {
            return Result(CanonicalBindingStatus.Unavailable, CoordinationBindingErrors.Invalid);
        }
    }

    internal static string ResolvePhysicalPath(CoordinationSourceBinding binding, string primaryPath, string checkoutPath)
    {
        NativeAdmissionCodec.ValidateLocalPath(primaryPath);
        NativeAdmissionCodec.ValidateLocalPath(checkoutPath);
        if (binding.Repository.CanonicalPath != RepositoryIdentity.Canonicalise(primaryPath))
            throw new CoordinationSourceException(CoordinationBindingErrors.Mismatch);
        var primary = Path.GetFullPath(primaryPath);
        var checkout = Path.GetFullPath(checkoutPath);
        Guard(primary);
        Guard(checkout);
        var common = Path.Combine(primary, ".git");
        Guard(common);
        if (!Directory.Exists(common)) throw new CoordinationSourceException(CoordinationBindingErrors.Invalid);
        if (RepositoryIdentity.Canonicalise(checkout) != RepositoryIdentity.Canonicalise(primary))
        {
            var dotGit = Path.Combine(checkout, ".git");
            var pointer = ReadPointer(dotGit);
            if (!pointer.StartsWith("gitdir:", StringComparison.Ordinal))
                throw new CoordinationSourceException(CoordinationBindingErrors.Invalid);
            var gitdir = Path.GetFullPath(pointer[7..].Trim(), checkout);
            if (RepositoryIdentity.Canonicalise(Path.GetDirectoryName(gitdir)!)
                != RepositoryIdentity.Canonicalise(Path.Combine(common, "worktrees")))
                throw new CoordinationSourceException(CoordinationBindingErrors.Mismatch);
            Guard(gitdir);
            if (RepositoryIdentity.Canonicalise(Path.GetFullPath(ReadPointer(Path.Combine(gitdir, "commondir")), gitdir))
                != RepositoryIdentity.Canonicalise(common))
                throw new CoordinationSourceException(CoordinationBindingErrors.Mismatch);
            var backlink = Path.GetFullPath(ReadPointer(Path.Combine(gitdir, "gitdir")), gitdir);
            if (!string.Equals(backlink, dotGit, PathComparison.ForThisFileSystem))
                throw new CoordinationSourceException(CoordinationBindingErrors.Mismatch);
            Guard(dotGit);
        }
        var path = Path.Combine(primary, ".agents", "requests.jsonl");
        Guard(path, missingAllowed: true);
        return path;
    }

    private static string ReadPointer(string path)
    {
        Guard(path);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        Span<byte> bytes = stackalloc byte[MaxPointer + 1];
        var count = 0;
        int read;
        while (count < bytes.Length && (read = stream.Read(bytes[count..])) > 0) count += read;
        if (count > MaxPointer) throw new IOException(CoordinationBindingErrors.Invalid);
        var pointer = StrictUtf8.GetString(bytes[..count]).Trim();
        if (pointer.Length == 0) throw new IOException(CoordinationBindingErrors.Invalid);
        return pointer;
    }

    internal static void Guard(string path, bool missingAllowed = false)
    {
        // NativeAdmissionCodec's directory-only guard is private; this includes the final source file.
        var ancestors = new Stack<string>();
        for (var current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
            ancestors.Push(current);
        foreach (var ancestor in ancestors)
        {
            try
            {
                if ((File.GetAttributes(ancestor) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException(CoordinationBindingErrors.Invalid);
            }
            // An absent source is classified by Capture, never created by the guard.
            catch (FileNotFoundException) when (missingAllowed) { }
            catch (DirectoryNotFoundException) when (missingAllowed) { }
        }
    }
}
