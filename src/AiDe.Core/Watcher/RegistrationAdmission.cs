using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AiDe.Core.Watcher;

/// <summary>An immutable historical admission, not a statement of human authority.</summary>
public sealed record RegistrationAdmission(
    string OperationId, string InputDigest, string ContextDigest, string DecisionDigest,
    SessionRecord Session, string RepositorySent, string RepositoryUsed,
    string CorrectionCode, long RecordedAtMilliseconds);

/// <summary>A replay carries its original receipt but never issues or renews a capability.</summary>
public sealed record RegistrationAdmissionResult(
    RegistrationAdmission Admission, SessionCapability? Capability, bool Replayed);

internal static class NativeAdmissionErrors
{
    internal const string Unavailable = "COORD_NATIVE_UNAVAILABLE";
    internal const string Uncertain = "COORD_NOTICE_UNCERTAIN";
    internal const string Busy = "COORD_NATIVE_BUSY";
    internal const string InvalidInput = "COORD_NATIVE_INPUT";
    internal const string ContextMismatch = "COORD_NATIVE_CONTEXT";
    internal const string OperationConflict = "COORD_NATIVE_OPERATION_CONFLICT";
    internal const string Capacity = "COORD_NOTICE_CAPACITY";
    internal const string Stale = "COORD_NATIVE_STALE";
    internal const string Owner = "COORD_NATIVE_OWNER";
    internal const string Integrity = "COORD_NATIVE_INTEGRITY";

    internal static WatcherException Error(string code) => new(code, code);
}

// No production source currently supplies this context. Internal enrollment is deliberately
// separate from the public, payload-facing API; a context is not parsed out of registration prose.
internal sealed record NativeRegistrationContext(
    WorktreeIdentity Worktree, TerminalIdentity Terminal, string PublicationRoot);

internal enum NativeAdmissionFaultPoint
{
    BeforeContextRead, AfterAdmission, AfterNotice, AfterSession,
    AfterEndClear, AfterHeartbeat, BeforeCommit, AfterCommit, BeforeLifecycleCheck,
}

internal enum NativeLifecycle { Heartbeat, End, Update }

internal sealed record BoundNativeRegistration(
    string OperationId, string InputDigest, string ContextDigest,
    string RepositorySent, string RepositoryUsed, string CorrectionCode,
    SessionBinding Binding, string PublicationRoot);

internal static class NativeAdmissionCodec
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static string Digest(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    internal static void Text(string? value, int maximum, bool empty = false)
    {
        if (value is null || value.Length > maximum || value.Contains('\0')
            || (!empty && string.IsNullOrWhiteSpace(value)))
            throw NativeAdmissionErrors.Error(NativeAdmissionErrors.InvalidInput);
        try { _ = StrictUtf8.GetByteCount(value); }
        catch (EncoderFallbackException) { throw NativeAdmissionErrors.Error(NativeAdmissionErrors.InvalidInput); }
    }

    internal static BoundNativeRegistration Bind(
        string operationId, HarnessRegistration registration, NativeRegistrationContext context)
    {
        Text(operationId, 128);
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(context);
        var attributes = new Dictionary<string, string?>(registration.Attributes, StringComparer.Ordinal);
        if (attributes.Count > 32) throw NativeAdmissionErrors.Error(NativeAdmissionErrors.InvalidInput);
        foreach (var (key, value) in attributes)
        {
            Text(key, 256);
            if (value is not null) Text(value, 4096, empty: true);
        }
        string Required(string key, int maximum, bool empty = false)
        {
            attributes.TryGetValue(key, out var value);
            Text(value, maximum, empty);
            return value!;
        }
        string? Optional(string key, int maximum)
        {
            attributes.TryGetValue(key, out var value);
            if (value is not null) Text(value, maximum);
            return value;
        }

        var sent = Required(OtelAttributes.RepoPath, 4096);
        _ = Required(OtelAttributes.RepoDisplay, 4096);
        var path = Required(OtelAttributes.WorktreePath, 4096);
        var branch = Required(OtelAttributes.WorktreeBranch, 512, empty: true);
        var terminal = Required(OtelAttributes.TerminalId, 512);
        var agent = Required(OtelAttributes.AgentName, 256);
        var harness = Optional(OtelAttributes.ServiceName, 256);
        var harnessVersion = Optional(OtelAttributes.ServiceVersion, 128);
        var model = Optional(OtelAttributes.GenAiModel, 256);
        var modelVersion = Optional(OtelAttributes.GenAiModelVersion, 128);
        if ((harness is null) != (harnessVersion is null) || (model is null) != (modelVersion is null))
            throw NativeAdmissionErrors.Error(NativeAdmissionErrors.InvalidInput);

        Text(context.Worktree.Repository.CanonicalPath, 4096);
        Text(context.Worktree.Repository.DisplayName, 4096);
        Text(context.Worktree.Branch, 512, empty: true);
        Text(context.Terminal.TerminalId, 512);
        ValidateLocalPath(context.Worktree.Path);
        ValidateLocalPath(context.PublicationRoot);
        if (RepositoryIdentity.Canonicalise(path) != RepositoryIdentity.Canonicalise(context.Worktree.Path)
            || branch != context.Worktree.Branch || terminal != context.Terminal.TerminalId)
            throw NativeAdmissionErrors.Error(NativeAdmissionErrors.ContextMismatch);
        var claimed = RepositoryIdentity.Canonicalise(sent);
        var repository = context.Worktree.Repository.CanonicalPath;
        var corrected = claimed == RepositoryIdentity.Canonicalise(context.Worktree.Path) && claimed != repository;
        if (!corrected && claimed != repository)
            throw NativeAdmissionErrors.Error(NativeAdmissionErrors.ContextMismatch);

        // Validate all input and both paths before the first filesystem call. Never follow .git.
        var used = corrected ? repository : sent;
        var frozenRepository = new RepositoryIdentity(used, used);
        var binding = new SessionBinding(frozenRepository,
            new WorktreeIdentity(frozenRepository, branch, context.Worktree.Path),
            context.Terminal, new AgentIdentity(agent),
            harness is null ? null : new HarnessIdentity(harness, harnessVersion!),
            model is null ? null : new ModelIdentity(model, modelVersion!), TrustClassification.Asserted);
        return new(operationId,
            Digest(JsonSerializer.SerializeToUtf8Bytes(attributes.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray())),
            Digest(JsonSerializer.SerializeToUtf8Bytes(context)),
            sent, used, corrected ? "LINKED_WORKTREE" : "NONE", binding, context.PublicationRoot);
    }

    internal static void ValidateContextFiles(BoundNativeRegistration input)
    {
        RejectReparseAncestors(input.Binding.Worktree.Path);
        RejectReparseAncestors(input.PublicationRoot);
    }

    private static void ValidateLocalPath(string path)
    {
        Text(path, 4096);
        if (!Path.IsPathFullyQualified(path) || path.StartsWith(@"\\", StringComparison.Ordinal)
            || path.StartsWith("//", StringComparison.Ordinal))
            throw NativeAdmissionErrors.Error(NativeAdmissionErrors.ContextMismatch);
        var segments = path.Replace('/', '\\').Split('\\');
        foreach (var segment in segments.Skip(1))
        {
            var stem = segment.Split('.')[0].ToUpperInvariant();
            if (segment is "." or ".." || segment.EndsWith('.') || segment.EndsWith(' ')
                || segment.Contains(':') || stem is "CON" or "PRN" or "AUX" or "NUL"
                || (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal)
                    || stem.StartsWith("LPT", StringComparison.Ordinal)) && char.IsAsciiDigit(stem[3])))
                throw NativeAdmissionErrors.Error(NativeAdmissionErrors.ContextMismatch);
        }
    }

    private static void RejectReparseAncestors(string path)
    {
        var ancestors = new Stack<string>();
        for (var directory = new DirectoryInfo(Path.GetFullPath(path)); directory is not null; directory = directory.Parent)
            ancestors.Push(directory.FullName);
        foreach (var ancestor in ancestors)
            if ((File.GetAttributes(ancestor) & FileAttributes.ReparsePoint) != 0)
                throw NativeAdmissionErrors.Error(NativeAdmissionErrors.ContextMismatch);
    }

    internal static string DecisionDigest(RegistrationAdmission admission) =>
        Digest(JsonSerializer.SerializeToUtf8Bytes(new
        {
            admission.OperationId, admission.InputDigest, admission.ContextDigest, admission.Session,
            admission.RepositorySent, admission.RepositoryUsed, admission.CorrectionCode, admission.RecordedAtMilliseconds,
        }));

    internal static byte[] NoticeBytes(RegistrationAdmission admission) =>
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            generatedBy = RegistrationPublisher.GeneratedBy,
            noticeId = admission.DecisionDigest[..32],
            sessionId = admission.Session.SessionId,
            repositorySent = admission.RepositorySent,
            repositoryUsed = admission.RepositoryUsed,
            reason = RepositoryCorrection.WorktreeResolvedReason,
        });

    internal static void RequireNotice(RegistrationAdmission admission, byte[] bytes, string digest)
    {
        if (!bytes.AsSpan().SequenceEqual(NoticeBytes(admission)) || Digest(bytes) != digest)
            throw NativeAdmissionErrors.Error(NativeAdmissionErrors.Integrity);
    }
}
