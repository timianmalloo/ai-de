using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AiDe.Core.Understanding;

/// <summary>Recorded approval data, not authority. The trusted verifier must authenticate every field.</summary>
public sealed record RecordedRootApproval(string DecisionRecordToken, string GrantVersion, string WorkspaceToken,
    string RootToken, string PolicyToken, string SessionToken, string ApprovedAbsoluteRoot, DateTimeOffset ExpiresAt);

/// <summary>
/// Trusted, single-root proof composition. The verifier authenticates the recorded decision, exact root,
/// workspace, policy, session and expiry; a caller-supplied path or a callback returning true is not a
/// substitute for that authentication. Membership is supplied by the trusted runner, never discovered here.
/// </summary>
public static class AtlasProofComposition
{
    public static Task<AtlasProofHandle> CreateForApprovedRootAsync(RecordedRootApproval approval,
        Func<RecordedRootApproval, bool> isApprovalCurrent, ImmutableArray<string>? membershipPaths,
        bool membershipComplete, string membershipReason, CancellationToken cancellationToken, TimeProvider? clock = null) =>
        AtlasQueryService.CreateAsync(approval, isApprovalCurrent, membershipPaths, membershipComplete, membershipReason, cancellationToken, clock);
}

/// <summary>
/// Owns the detached query lifetime without exposing grants. InitialManifestToken bootstraps the first
/// selection; consumers must subsequently adopt the manifest token of an accepted selection generation.
/// </summary>
public sealed class AtlasProofHandle : IDisposable
{
    internal AtlasProofHandle(AtlasQueryService queries, string initialManifestToken) =>
        (Queries, InitialManifestToken) = (queries, initialManifestToken);
    public IAtlasQueries Queries { get; }
    public string InitialManifestToken { get; }
    public void Dispose() => ((IDisposable)Queries).Dispose();
}

internal sealed record AtlasQueryLimits(long ManifestBytes = 64 * 1024 * 1024, long ReceiptBytes = 2 * 1024 * 1024);

internal sealed class AtlasQueryService : IAtlasQueries, IDisposable
{
    private const int MaxManifests = 2;
    private const int MaxReceipts = 100;
    private const int MaxActive = 4;
    private const int MaxPending = 16;
    private static readonly ActivitySource Activities = new("AiDe.Core.Understanding.AtlasQueries");
    private static readonly Meter Metrics = new("AiDe.Core.Understanding.AtlasQueries");
    private static readonly Counter<long> Operations = Metrics.CreateCounter<long>("atlas.query.operations");
    private static readonly Counter<long> Failures = Metrics.CreateCounter<long>("atlas.query.failures");
    private static readonly Histogram<double> Duration = Metrics.CreateHistogram<double>("atlas.query.duration", "ms");
    private static readonly Histogram<long> Retention = Metrics.CreateHistogram<long>("atlas.query.retained", "By");
    private readonly object gate = new();
    private readonly AtlasRootGrant grant;
    private readonly Func<bool> isCurrent;
    private readonly AtlasSource source;
    private readonly AtlasQueryLimits limits;
    private readonly ImmutableArray<AtlasFileEntry> inventoryFiles;
    private readonly SemaphoreSlim slots = new(MaxActive);
    private readonly CancellationTokenSource shutdown = new();
    private readonly Dictionary<string, CachedManifest> manifests = new(StringComparer.Ordinal);
    private readonly LinkedList<string> manifestOrder = new();
    private readonly Dictionary<string, Receipt> receipts = new(StringComparer.Ordinal);
    private readonly LinkedList<string> receiptOrder = new();
    private string currentToken;
    private long manifestBytes;
    private long receiptBytes;
    private long latestSequence = -1;
    private int admitted;
    private int active;
    private bool disposed;
    private bool shutdownSignaled;
    private bool resourcesDisposed;

    private AtlasQueryService(AtlasRootGrant grant, Func<bool> isCurrent, AtlasSource source,
        AtlasQueryLimits limits, CachedManifest initial)
    {
        this.grant = grant;
        this.isCurrent = isCurrent;
        this.source = source;
        this.limits = limits;
        inventoryFiles = initial.Value.Files;
        currentToken = initial.Value.Token;
        manifests.Add(currentToken, initial);
        manifestOrder.AddLast(currentToken);
        manifestBytes = initial.Bytes;
    }

    internal Func<CancellationToken, Task>? BeforePublication { get; set; }
    internal (int Manifests, long ManifestBytes, int Receipts, long ReceiptBytes, int Active, int Pending) Retained
    {
        get
        {
            lock (gate)
                return (manifests.Count, manifestBytes, receipts.Count, receiptBytes, active, admitted - active);
        }
    }

    internal static async Task<AtlasProofHandle> CreateAsync(RecordedRootApproval approval,
        Func<RecordedRootApproval, bool> isApprovalCurrent, ImmutableArray<string>? membershipPaths,
        bool membershipComplete, string membershipReason, CancellationToken cancellationToken, TimeProvider? clock = null,
        AtlasQueryLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(approval);
        ArgumentNullException.ThrowIfNull(isApprovalCurrent);
        AtlasIdentityCodec.RequiredToken(approval.DecisionRecordToken, nameof(approval));
        AtlasIdentityCodec.RequiredToken(membershipReason, nameof(membershipReason));
        clock ??= TimeProvider.System;
        limits ??= new();
        if (limits.ManifestBytes is <= 0 or > 64 * 1024 * 1024 || limits.ReceiptBytes is <= 0 or > 2 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(limits));

        bool Current()
        {
            try
            {
                return clock.GetUtcNow() < approval.ExpiresAt && isApprovalCurrent(approval)
                    && clock.GetUtcNow() < approval.ExpiresAt;
            }
            catch (Exception) { return false; }
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!Current()) throw new UnauthorizedAccessException(Code.Approval);
        var source = new AtlasSource(clock);
        var observed = source.ObserveApprovedRootIdentity(approval.ApprovedAbsoluteRoot, Current, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!Current() || observed.Completion is not AtlasCompletionState.Complete || observed.Identity is null)
            throw new UnauthorizedAccessException(Code.Approval);
        var grant = AtlasRootGrant.Create(approval.GrantVersion, approval.WorkspaceToken, approval.RootToken,
            approval.PolicyToken, approval.SessionToken, approval.ApprovedAbsoluteRoot, observed.Identity, approval.ExpiresAt);
        var policy = membershipPaths is { } paths
            ? AtlasInventoryPolicy.KnownMembership(grant, paths, membershipComplete, Code.Membership)
            : AtlasInventoryPolicy.UnavailableMembership(Code.Membership);
        var enumerator = new AtlasDirectoryEnumerator(timeProvider: clock,
            isGrantCurrent: candidate => ReferenceEquals(candidate, grant) && Current());
        var manifest = await new AtlasInventory(enumerator)
            .BuildManifestAsync(grant, EnumerationLimits.CandidateDefault, policy, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!Current()) throw new UnauthorizedAccessException(Code.Approval);
        var cached = Cache(manifest, null, []);
        if (cached.Bytes > limits.ManifestBytes) throw new InvalidOperationException(Code.Retention);
        return new(new AtlasQueryService(grant, Current, source, limits, cached), manifest.Token);
    }

    public async Task<InventoryPage> InventoryAsync(PageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var activity = Activities.StartActivity("atlas.query.inventory");
        var started = Stopwatch.GetTimestamp();
        using var linked = Admit(null, cancellationToken, out _, out var refusal);
        if (linked is null)
        {
            Record("inventory", started);
            return EmptyInventory(request, refusal);
        }
        var acquired = false;
        try
        {
            await slots.WaitAsync(linked.Token).ConfigureAwait(false);
            acquired = true;
            lock (gate) active++;
            if (!Current()) return EmptyInventory(request, Code.Approval);
            linked.Token.ThrowIfCancellationRequested();
            lock (gate)
            {
                if (disposed || linked.IsCancellationRequested) return EmptyInventory(request, Code.Canceled);
                var manifest = manifests[currentToken].Value;
                return ProjectInventory(request, inventoryFiles, manifest.Files, manifest.Bounds);
            }
        }
        catch (OperationCanceledException) { return EmptyInventory(request, Code.Canceled); }
        finally { Release(acquired); Record("inventory", started); }
    }

    internal static InventoryPage ProjectInventory(PageRequest request, ImmutableArray<AtlasFileEntry> originalRows,
        ImmutableArray<AtlasFileEntry> currentRows, AtlasBounds inventoryBounds)
    {
        // An offset belongs to this retained ordered snapshot, never to a replacement rowset.
        if (!originalRows.SequenceEqual(currentRows))
            return EmptyInventory(request, Code.InventoryRestart,
                inventoryBounds.TotalState is AtlasDenominatorState.Withheld ? AtlasDenominatorState.Withheld : AtlasDenominatorState.Unknown);
        var files = currentRows.Skip(request.Offset).Take(request.Limit).ToImmutableArray();
        var known = inventoryBounds.TotalState is AtlasDenominatorState.Known;
        var omitted = request.Offset + (long)files.Length < currentRows.Length;
        // ReturnedBytes measures content payload; metadata retention charges are a different quantity.
        var bounds = new AtlasBounds(request.Limit, request.Limit, files.Length, 0,
            inventoryBounds.TotalCount, inventoryBounds.TotalState,
            inventoryBounds.OmissionReason ?? (!known ? Code.Membership : omitted ? Code.Page : null),
            inventoryBounds.LimitingDimension ?? (!known ? "membership" : omitted ? "page" : null));
        return new(request, bounds, files, omitted ? checked(request.Offset + files.Length) : null);
    }

    public Task<SelectionProjection> SelectAsync(SelectionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return RunSelection(request, null, request.RequestSequence, cancellationToken);
    }

    public Task<SelectionProjection> RestoreAsync(string issuedReceiptToken, long requestSequence, CancellationToken cancellationToken)
    {
        AtlasIdentityCodec.RequiredToken(issuedReceiptToken, nameof(issuedReceiptToken));
        AtlasBounds.NonNegative(requestSequence, nameof(requestSequence));
        return RunSelection(null, issuedReceiptToken, requestSequence, cancellationToken);
    }

    private async Task<SelectionProjection> RunSelection(SelectionRequest? request, string? receiptToken,
        long sequence, CancellationToken cancellationToken)
    {
        using var activity = Activities.StartActivity("atlas.query.selection");
        var started = Stopwatch.GetTimestamp();
        using var linked = Admit(sequence, cancellationToken, out var rejectedState, out var refusal);
        if (linked is null)
        {
            Record("selection", started);
            return Failure(rejectedState, sequence, refusal);
        }
        var acquired = false;
        try
        {
            await slots.WaitAsync(linked.Token).ConfigureAwait(false);
            acquired = true;
            lock (gate) active++;
            var invalid = Invalid(sequence, linked.Token);
            if (invalid is { } state) return Failure(state, sequence, StateCode(state));
            SelectionPlan? plan;
            lock (gate) plan = Plan(request, receiptToken);
            if (plan is null) return Failure(SourceProjectionState.Refused, sequence, Code.Selection);

            var prepared = await Task.Run(() => Prepare(plan, sequence, linked.Token), linked.Token).ConfigureAwait(false);
            if (BeforePublication is { } before) await before(linked.Token).ConfigureAwait(false);
            invalid = Invalid(sequence, linked.Token);
            if (invalid is { } finalState) return Failure(finalState, sequence, StateCode(finalState));
            lock (gate)
            {
                if (disposed) return Failure(SourceProjectionState.Refused, sequence, Code.Approval);
                if (linked.IsCancellationRequested || sequence != latestSequence)
                    return Failure(SourceProjectionState.Canceled, sequence, Code.Canceled);
                if (!manifests.ContainsKey(plan.Basis.Value.Token))
                    return Failure(SourceProjectionState.Refused, sequence, Code.Selection);
                if (prepared.Source.State is not SourceProjectionState.IndexedMatch)
                    return Failure(prepared.Source.State, sequence, Code.Source);
                return Publish(plan, prepared, sequence);
            }
        }
        catch (OperationCanceledException) { return Failure(SourceProjectionState.Canceled, sequence, Code.Canceled); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return Failure(SourceProjectionState.Unverifiable, sequence, Code.Source);
        }
        finally { Release(acquired); Record("selection", started); }
    }

    private SelectionPlan? Plan(SelectionRequest? request, string? receiptToken)
    {
        Receipt? receipt = null;
        if (receiptToken is not null && !receipts.TryGetValue(receiptToken, out receipt)) return null;
        var manifestToken = receipt?.ManifestToken ?? request!.ManifestToken;
        var fileValue = receipt?.FileValue ?? request!.FileValue;
        var declarationKey = receipt?.DeclarationKey ?? request?.DeclarationObservationKey;
        if (!manifests.TryGetValue(manifestToken, out var basis)) return null;
        var file = basis.Value.Files.FirstOrDefault(file => file.FileValue == fileValue);
        if (file is null || file.Kind is not AtlasDirectoryEntryKind.File || file.Availability is not AtlasFileAvailability.Available)
            return null;
        var observation = basis.Value.SourceObservations.FirstOrDefault(observation => observation.FileValue == fileValue);
        if (receipt is not null && !ReferenceEquals(observation, receipt.Observation)) return null;
        AtlasDeclaration? declaration = null;
        if (declarationKey is not null)
        {
            declaration = basis.Value.Declarations.FirstOrDefault(declaration => declaration.ObservationKey == declarationKey
                && declaration.SourceObservationKey == observation?.ObservationKey);
            if (declaration is null) return null;
        }
        var binding = receipt?.Binding ?? (observation is null ? null : Binding(observation));
        return new(basis, file, observation is null ? NewToken("manifest") : manifestToken, observation, binding, declaration);
    }

    private async Task<PreparedSelection> Prepare(SelectionPlan plan, long sequence, CancellationToken ct)
    {
        var request = new AtlasSourceReadRequest(grant, plan.File, plan.ManifestToken,
            plan.Observation?.ObservationKey ?? NewToken("source"),
            (candidate, token, file) => Visible(plan, candidate, token, file, sequence, ct));
        using var read = plan.Observation is null
            ? await source.ObserveAsync(request, ct).ConfigureAwait(false)
            : await source.ReadVerifiedAsync(request, plan.Observation, plan.Binding!, ct).ConfigureAwait(false);
        var observation = read.Observation;
        var binding = read.Binding;
        var buffer = read.Buffer;
        if (buffer is null || observation is null || binding is null)
            return new(plan.Basis, NonMatch(read.State), [], null);

        var cached = plan.Basis;
        if (plan.Observation is null)
        {
            CSharpDeclarationObservationResult? result = null;
            if (plan.File.Classification is AtlasFileClassification.CSharp)
            {
                var tree = CSharpSyntaxTree.ParseText(buffer.FullText, path: plan.File.RelativePath, cancellationToken: ct);
                var compilation = CSharpCompilation.Create("AtlasFile", [tree],
                    [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                using var input = CSharpDeclarationSourceInput.Create(tree, grant, plan.File, observation, binding,
                    read.RawSnapshot.Span, plan.File.RelativePath);
                result = CSharpDeclarationObservation.Observe(compilation,
                    AtlasCompilationScope.ForFileLimited(grant.WorkspaceToken, grant.RootToken, null), [input],
                    cancellationToken: ct);
                if (result.Completion is AtlasCompletionState.Canceled)
                    return new(plan.Basis, SourceProjection.Canceled(observation.ObservationKey), [], null);
            }
            var manifest = new AtlasManifest(plan.ManifestToken, grant, plan.Basis.Value.DirectoryObservationKey,
                plan.Basis.Value.Files, [observation], result?.Declarations ?? [], plan.Basis.Value.Completion, plan.Basis.Value.Bounds);
            cached = Cache(manifest, result?.Bounds, result?.Limitations ?? ["non-CSharp file"]);
        }
        var declarations = cached.Value.Declarations.Where(declaration => declaration.SourceObservationKey == observation.ObservationKey)
            .Take(PageRequest.MaxLimit).ToImmutableArray();
        var span = plan.Declaration?.DeclarationSpan ?? new AtlasTextSpan(0, buffer.FullText.Length);
        var page = source.ReadPage(read, span, plan.Declaration is null ? [] : [plan.Declaration.IdentifierSpan]);
        return new(cached, page, declarations, plan.Declaration?.ObservationKey);
    }

    private SelectionProjection Publish(SelectionPlan plan, PreparedSelection prepared, long sequence)
    {
        var manifest = prepared.Manifest;
        // Receipts keep the ORIGINAL observation. A later read must not turn old history into a fresh selection.
        var observation = plan.Observation ?? manifest.Value.SourceObservations.Single();
        var binding = plan.Binding ?? Binding(observation);
        var receipt = new Receipt(NewToken("receipt"), manifest.Value.Token, plan.File.FileValue, prepared.DeclarationKey,
            observation, binding, 512 + ChargeStrings(manifest.Value.Token, plan.File.FileValue, prepared.DeclarationKey,
                observation.ObservationKey, binding.ManifestIdentity, binding.ManifestFileIdentity, binding.PolicyIdentity,
                binding.ContentHash, binding.RootIdentity, binding.FileIdentity));
        if (manifest.Bytes > limits.ManifestBytes || receipt.Bytes > limits.ReceiptBytes)
            return Failure(SourceProjectionState.Refused, sequence, Code.Retention);
        if (!manifests.ContainsKey(manifest.Value.Token))
        {
            while (manifests.Count >= MaxManifests || manifestBytes + manifest.Bytes > limits.ManifestBytes)
                EvictManifest(manifestOrder.First!.Value);
            manifests.Add(manifest.Value.Token, manifest);
            manifestOrder.AddLast(manifest.Value.Token);
            manifestBytes += manifest.Bytes;
        }
        while (receipts.Count >= MaxReceipts || receiptBytes + receipt.Bytes > limits.ReceiptBytes)
            EvictReceipt(receiptOrder.First!.Value);
        receipts.Add(receipt.Token, receipt);
        receiptOrder.AddLast(receipt.Token);
        receiptBytes += receipt.Bytes;
        currentToken = manifest.Value.Token;
        Retention.Record(manifestBytes, new KeyValuePair<string, object?>("kind", "manifest"));
        Retention.Record(receiptBytes, new KeyValuePair<string, object?>("kind", "receipt"));
        var totalKnown = manifest.DeclarationBounds?.TotalState is not AtlasDenominatorState.Unknown and not AtlasDenominatorState.Withheld;
        var total = manifest.Value.Declarations.Length;
        var truncated = total > prepared.Declarations.Length;
        var bounds = new AtlasBounds(PageRequest.MaxLimit, PageRequest.MaxLimit, prepared.Declarations.Length,
            Encoding.UTF8.GetByteCount(prepared.Source.Page!.Text), totalKnown ? total : null,
            totalKnown ? AtlasDenominatorState.Known : AtlasDenominatorState.Unknown,
            !totalKnown ? "declaration population unknown" : truncated ? Code.Page : null,
            !totalKnown || truncated ? "declarations" : null);
        var limitations = manifest.Limitations.AddRange(new[] { "FileLimited", "project not established",
            "TFM not established", "observation-only symbol identity" });
        if (truncated) limitations = limitations.Add("outline truncated; no outline continuation port");
        if (prepared.Source.Page.PageSpan.End < observation.DecodedUtf16Length)
            limitations = limitations.Add("source page bounded; no arbitrary source continuation port");
        return new(receipt.Token, manifest.Value.Token, plan.File.FileValue, sequence,
            new SelectionOutline(prepared.Declarations.Select(declaration =>
                new OutlineDeclaration(declaration.ObservationKey, declaration.DisplaySignature, declaration.Kind, declaration.DeclarationSpan))),
            prepared.Source, bounds, SelectionCoverage.Unknown("file-limited observation"), limitations);
    }

    private bool Visible(SelectionPlan plan, AtlasRootGrant candidate, string token, AtlasFileEntry file, long sequence, CancellationToken ct)
    {
        if (!ReferenceEquals(candidate, grant) || token != plan.ManifestToken || !ReferenceEquals(file, plan.File)
            || Invalid(sequence, ct) is not null) return false;
        lock (gate) return manifests.TryGetValue(plan.Basis.Value.Token, out var basis) && ReferenceEquals(basis, plan.Basis);
    }

    private CancellationTokenSource? Admit(long? sequence, CancellationToken ct, out SourceProjectionState state, out string reason)
    {
        state = SourceProjectionState.Refused;
        reason = Code.Approval;
        if (!Current()) return null;
        lock (gate)
        {
            if (disposed) return null;
            if (ct.IsCancellationRequested || sequence is { } value && value <= latestSequence)
            {
                state = SourceProjectionState.Canceled;
                reason = Code.Canceled;
                return null;
            }
            if (admitted >= MaxActive + MaxPending) { reason = Code.Capacity; return null; }
            var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, shutdown.Token);
            admitted++;
            if (sequence is { } accepted) latestSequence = accepted;
            return linked;
        }
    }

    private bool Current() => !Volatile.Read(ref disposed) && isCurrent();
    private SourceProjectionState? Invalid(long sequence, CancellationToken ct) =>
        !Current() ? SourceProjectionState.Refused
        : ct.IsCancellationRequested || sequence != Interlocked.Read(ref latestSequence) ? SourceProjectionState.Canceled : null;

    private void Release(bool acquired)
    {
        lock (gate)
        {
            if (acquired) { active--; slots.Release(); }
            admitted--;
            DisposeResourcesWhenIdle();
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            manifests.Clear();
            receipts.Clear();
            manifestOrder.Clear();
            receiptOrder.Clear();
            manifestBytes = receiptBytes = 0;
        }
        shutdown.Cancel();
        lock (gate) { shutdownSignaled = true; DisposeResourcesWhenIdle(); }
    }

    private void DisposeResourcesWhenIdle()
    {
        if (!shutdownSignaled || admitted != 0 || resourcesDisposed) return;
        resourcesDisposed = true;
        slots.Dispose();
        shutdown.Dispose();
    }

    private void EvictManifest(string token)
    {
        manifestBytes -= manifests[token].Bytes;
        manifests.Remove(token);
        manifestOrder.Remove(token);
        foreach (var receipt in receipts.Values.Where(receipt => receipt.ManifestToken == token).ToArray())
            EvictReceipt(receipt.Token);
    }

    private void EvictReceipt(string token)
    {
        receiptBytes -= receipts[token].Bytes;
        receipts.Remove(token);
        receiptOrder.Remove(token);
    }

    private SelectionProjection Failure(SourceProjectionState state, long sequence, string reason)
    {
        Failures.Add(1, new KeyValuePair<string, object?>("reason", reason));
        return new(NewToken("unissued"), currentToken, "unavailable-file", sequence, new SelectionOutline([]),
            NonMatch(state), new AtlasBounds(128, 128, 0, 0, null, AtlasDenominatorState.Withheld, reason, "selection"),
            SelectionCoverage.Withheld(reason), [reason]);
    }

    private static InventoryPage EmptyInventory(PageRequest request, string reason,
        AtlasDenominatorState totalState = AtlasDenominatorState.Unknown) =>
        new(request, new AtlasBounds(request.Limit, request.Limit, 0, 0, null, totalState, reason, "query"), []);

    private static SourceProjection NonMatch(SourceProjectionState state) => state switch
    {
        SourceProjectionState.Changed => SourceProjection.Changed("source-not-published"),
        SourceProjectionState.Canceled => SourceProjection.Canceled("source-not-published"),
        SourceProjectionState.Refused => SourceProjection.Refused("source-not-published"),
        SourceProjectionState.Unverifiable => SourceProjection.Unverifiable("source-not-published"),
        SourceProjectionState.UnsupportedEncoding => SourceProjection.UnsupportedEncoding("source-not-published"),
        SourceProjectionState.TooLargeToVerify => SourceProjection.TooLargeToVerify("source-not-published"),
        SourceProjectionState.ReadUnstable => SourceProjection.ReadUnstable("source-not-published"),
        _ => SourceProjection.Unavailable("source-not-published"),
    };

    private static AtlasSourceBinding Binding(AtlasSourceObservation observation) =>
        AtlasSourceBinding.Create(observation.ManifestToken, observation.FileValue, observation.PolicyToken,
            AtlasIdentityCodec.ForNativeObject(observation.RootIdentity!), AtlasIdentityCodec.ForNativeObject(observation.FileIdentity!),
            observation.CanonicalSha256!);

    // Conservative retained-payload charges, not a measurement of CLR heap/RSS. Bodies and compiler objects never enter these caches.
    private static CachedManifest Cache(AtlasManifest manifest, AtlasBounds? declarationBounds, ImmutableArray<string> limitations) =>
        new(manifest, Charge(manifest) + ChargeStrings(limitations.ToArray())
            + ChargeStrings(declarationBounds?.OmissionReason, declarationBounds?.LimitingDimension),
            declarationBounds, limitations);
    private static long Charge(AtlasManifest manifest) =>
        1024 + ChargeStrings(manifest.Token, manifest.DirectoryObservationKey, manifest.RootGrant.ApprovedAbsoluteRoot,
            manifest.RootGrant.GrantVersion, manifest.RootGrant.WorkspaceToken, manifest.RootGrant.RootToken,
            manifest.RootGrant.PolicyToken, manifest.RootGrant.SessionToken,
            manifest.Bounds.OmissionReason, manifest.Bounds.LimitingDimension)
        + manifest.Files.Sum(ChargeFile)
        + manifest.SourceObservations.Sum(observation => 512 + ChargeStrings(observation.ObservationKey, observation.ManifestToken,
            observation.FileValue, observation.PolicyToken, observation.CanonicalSha256, observation.DecoderId))
        + manifest.Declarations.Sum(declaration => 512 + ChargeStrings(declaration.ObservationKey, declaration.LogicalSymbolValue,
            declaration.SourceObservationKey, declaration.ContextKey, declaration.DisplaySignature, declaration.Identifier,
            declaration.SourceBinding.ManifestIdentity, declaration.SourceBinding.ManifestFileIdentity,
            declaration.SourceBinding.PolicyIdentity, declaration.SourceBinding.RootIdentity, declaration.SourceBinding.FileIdentity,
            declaration.SourceBinding.ContentHash, declaration.UnresolvedReason));
    private static long ChargeFile(AtlasFileEntry file) =>
        512 + ChargeStrings(file.FileValue, file.RelativePath, file.ParentPathKey, file.Reason);
    private static long ChargeStrings(params string?[] values) => values.Sum(value => value is null ? 0L : 128 + 4L * value.Length);
    private static string NewToken(string kind) => $"atlas-{kind}:{Guid.NewGuid():N}";
    private static string StateCode(SourceProjectionState state) => state is SourceProjectionState.Canceled ? Code.Canceled : Code.Approval;
    private static void Record(string operation, long started)
    {
        var tag = new KeyValuePair<string, object?>("operation", operation);
        Operations.Add(1, tag);
        Duration.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds, tag);
    }

    private static class Code
    {
        internal const string Approval = "atlas.query.approval";
        internal const string Membership = "atlas.query.membership";
        internal const string Selection = "atlas.query.selection";
        internal const string Source = "atlas.query.source";
        internal const string Canceled = "atlas.query.canceled";
        internal const string Capacity = "atlas.query.capacity";
        internal const string Retention = "atlas.query.retention";
        internal const string Page = "atlas.query.page";
        internal const string InventoryRestart = "atlas.query.inventory-restart";
    }
    private sealed record CachedManifest(AtlasManifest Value, long Bytes, AtlasBounds? DeclarationBounds, ImmutableArray<string> Limitations);
    private sealed record Receipt(string Token, string ManifestToken, string FileValue, string? DeclarationKey,
        AtlasSourceObservation Observation, AtlasSourceBinding Binding, long Bytes);
    private sealed record SelectionPlan(CachedManifest Basis, AtlasFileEntry File, string ManifestToken,
        AtlasSourceObservation? Observation, AtlasSourceBinding? Binding, AtlasDeclaration? Declaration);
    private sealed record PreparedSelection(CachedManifest Manifest, SourceProjection Source,
        ImmutableArray<AtlasDeclaration> Declarations, string? DeclarationKey);
}
