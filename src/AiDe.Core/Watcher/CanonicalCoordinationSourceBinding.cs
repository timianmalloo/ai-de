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
    // No storage adapter is enrolled by this unit, even when its old raw bound fits.
    internal bool IsStorable => false;
}

/// <summary>
/// Snapshot-only synthetic binding. Never opens request content, a database or an event-supplied path.
/// Reparses are refused; this is not a held-handle race-free live source contract.
/// </summary>
internal static class CanonicalCoordinationSourceBinding
{
    internal const string Origin = "canonical-requests-v1";
    private const int MaxStreams = 16;
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
            NativeAdmissionCodec.ValidateLocalPath(context.PrimaryPath);
            NativeAdmissionCodec.ValidateLocalPath(context.CheckoutPath);
            if (context.Binding.Repository.CanonicalPath != RepositoryIdentity.Canonicalise(context.PrimaryPath))
                return Result(CanonicalBindingStatus.Unbound, CoordinationBindingErrors.Mismatch);
            var primary = Path.GetFullPath(context.PrimaryPath);
            var checkout = Path.GetFullPath(context.CheckoutPath);
            Guard(primary);
            Guard(checkout);
            var common = Path.Combine(primary, ".git");
            Guard(common);
            if (!Directory.Exists(common)) return Result(CanonicalBindingStatus.Unavailable, CoordinationBindingErrors.Invalid);
            if (RepositoryIdentity.Canonicalise(checkout) != RepositoryIdentity.Canonicalise(primary))
            {
                var dotGit = Path.Combine(checkout, ".git");
                var pointer = ReadPointer(dotGit);
                if (!pointer.StartsWith("gitdir:", StringComparison.Ordinal))
                    return Result(CanonicalBindingStatus.Unavailable, CoordinationBindingErrors.Invalid);
                var gitdir = Path.GetFullPath(pointer[7..].Trim(), checkout);
                var worktrees = Path.Combine(common, "worktrees");
                if (RepositoryIdentity.Canonicalise(Path.GetDirectoryName(gitdir)!)
                    != RepositoryIdentity.Canonicalise(worktrees))
                    return Result(CanonicalBindingStatus.Unbound, CoordinationBindingErrors.Mismatch);
                Guard(gitdir);
                var commonPointer = Path.Combine(gitdir, "commondir");
                if (RepositoryIdentity.Canonicalise(Path.GetFullPath(ReadPointer(commonPointer), gitdir))
                    != RepositoryIdentity.Canonicalise(common))
                    return Result(CanonicalBindingStatus.Unbound, CoordinationBindingErrors.Mismatch);
                // A copied forward pointer is not checkout membership; Git's admin backlink must agree.
                var backlinkPath = Path.Combine(gitdir, "gitdir");
                var backlink = Path.GetFullPath(ReadPointer(backlinkPath), gitdir);
                if (!string.Equals(backlink, dotGit, PathComparison.ForThisFileSystem))
                    return Result(CanonicalBindingStatus.Unbound, CoordinationBindingErrors.Mismatch);
                Guard(dotGit);
            }
            var path = Path.Combine(primary, ".agents", "requests.jsonl");
            Guard(path);
            if (!File.Exists(path)) return Result(CanonicalBindingStatus.Unavailable, CoordinationBindingErrors.Invalid);
            var scope = CanonicalCoordinationCodec.Scope(context.Binding.Repository.CanonicalPath,
                Origin, RepositoryIdentity.Canonicalise(path), CanonicalCoordinationCodec.Version);
            return Result(record.Raw.Length > 65536 ? CanonicalBindingStatus.OriginBound : CanonicalBindingStatus.Bound,
                record.Raw.Length > 65536 ? "COORD_CANONICAL_ORIGIN_BOUND" : "COORD_CANONICAL_BOUND", path, scope);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
            or ArgumentException or NotSupportedException or WatcherException)
        {
            return Result(CanonicalBindingStatus.Unavailable, CoordinationBindingErrors.Invalid);
        }
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

    private static void Guard(string path)
    {
        // NativeAdmissionCodec's directory-only guard is private; this includes the final source file.
        var ancestors = new Stack<string>();
        for (var current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
            ancestors.Push(current);
        foreach (var ancestor in ancestors)
            if ((File.GetAttributes(ancestor) & FileAttributes.ReparsePoint) != 0)
                throw new IOException(CoordinationBindingErrors.Invalid);
    }
}
