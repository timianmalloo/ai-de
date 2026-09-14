using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using AiDe.Core.Ipc;

namespace AiDe.Core.Understanding;

internal sealed record AtlasPrepared<T>(T Value, AtlasReadScopeIssuer.Operation Operation);

/// <summary>
/// Logical authority is connection-bound. Native pins belong only to an operation through its
/// publication gate; idle leases retain immutable observations and native Q receipts, not locks.
/// </summary>
internal sealed class AtlasReadScopeIssuer : IAsyncDisposable
{
    private static readonly Meter Meter = new("AiDe.Core.AtlasScopes");
    private static readonly Counter<long> Admissions = Meter.CreateCounter<long>("atlas.scope.admissions");
    private static readonly Counter<long> Operations = Meter.CreateCounter<long>("atlas.scope.operations");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("atlas.scope.operation.duration", "ms");
    private readonly AtlasWorkspaceReadPolicy _policy;
    private readonly AtlasGitMembership _membership;
    private readonly AtlasReadBudget _budget;
    private readonly ConcurrentDictionary<string, Connection> _connections = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task _expiry;
    private bool _disposed;

    internal AtlasReadScopeIssuer(AtlasWorkspaceReadPolicy policy, AtlasGitMembership membership, AtlasReadBudget budget)
    {
        _policy = policy;
        _membership = membership;
        _budget = budget;
        _expiry = ExpireAsync();
    }

    internal IpcResponse? Open(IpcRequest request, IpcPeer peer)
    {
        if (!request.Operation.StartsWith("atlas.", StringComparison.Ordinal)) return null;
        if (_disposed || _connections.ContainsKey(peer.ConnectionId))
            return IpcResponse.Error("Atlas.ScopeInvalid", "This Atlas connection cannot be reopened.");
        try
        {
            var connection = new Connection(peer, _budget.ReserveScope());
            if (!_connections.TryAdd(peer.ConnectionId, connection))
            {
                connection.Reservation.Dispose();
                return IpcResponse.Error("Atlas.ScopeInvalid", "This Atlas connection already exists.");
            }
            return null;
        }
        catch (AtlasReadException exception)
        {
            return IpcResponse.Error(exception.Code, exception.Message);
        }
    }

    private Connection RequireConnection(IpcPeer peer, string? capability)
    {
        if (_disposed || !_connections.TryGetValue(peer.ConnectionId, out var connection)
            || connection.Peer != peer || connection.Terminal || string.IsNullOrWhiteSpace(capability))
            throw Invalid();
        lock (connection.Gate)
        {
            if (connection.Capability is null) connection.Capability = capability;
            else if (!string.Equals(connection.Capability, capability, StringComparison.Ordinal)) throw Invalid();
        }
        return connection;
    }

    internal void RequireNegotiation(IpcPeer peer, string? capability) => _ = RequireConnection(peer, capability);

    internal async ValueTask<AtlasPrepared<AtlasAdmitDto>> AdmitAsync(
        AtlasAdmitRequestDto request, IpcPeer peer, string? capability, CancellationToken cancellationToken)
    {
        var connection = RequireConnection(peer, capability);
        Scope scope;
        lock (connection.Gate)
        {
            if (connection.Scope is not null || request.Version != 1 || !_policy.IsEpochCurrent(request.ExpectedCoreEpoch))
                throw Invalid();
            scope = new Scope(this, connection, request.ExpectedCoreEpoch);
            connection.Scope = scope;
            connection.ExpiresAt = scope.ExpiresAt;
        }
        Operation? operation = null;
        try
        {
            operation = await BeginAsync(scope, cancellationToken).ConfigureAwait(false);
            var grant = AtlasRootGrant.Create("atlas-runtime-v1", _policy.WorkspaceId, scope.RootToken,
                _policy.PolicyToken, peer.ConnectionId, _policy.RootPath, _policy.RootIdentity, scope.ExpiresAt);
            var created = await AtlasQueryService.CreateForScopeAsync(grant, scope.OperationCurrent, _budget, operation.Token).ConfigureAwait(false);
            scope.Queries = created.Queries;
            scope.InitialManifest = scope.Manifest(created.Manifest);
            foreach (var entry in created.Queries.ReaderFiles)
            {
                if (scope.Files.Count >= 25_000)
                    throw new AtlasReadException("Atlas.Busy", "The Atlas file handle limit was reached.");
                var token = NewToken();
                scope.Charge(token, entry.FileValue, entry.RelativePath, entry.ParentPathKey);
                scope.Files.Add(token, entry);
                scope.FileTokens.Add(entry.FileValue, token);
            }
            foreach (var entry in scope.Files.Values)
                if (entry.ParentPathKey != scope.RootToken && !scope.FileTokens.ContainsKey(entry.ParentPathKey))
                    throw new AtlasReadException("Atlas.AdmissionRefused", "The inventory cannot preserve its parent associations.");
            scope.CheckRetention();
            Admissions.Add(1);
            operation.CompleteWork();
            return new(new AtlasAdmitDto(1, scope.Token, scope.InitialManifest, scope.Epoch, scope.ExpiresAt), operation);
        }
        catch
        {
            scope.Invalidate();
            if (operation is not null)
            {
                operation.CompleteWork();
                await operation.DisposeAsync().ConfigureAwait(false);
            }
            throw;
        }
    }

    private Scope RequireScope(string token, long epoch, IpcPeer peer, string? capability)
    {
        var connection = RequireConnection(peer, capability);
        var scope = connection.Scope;
        if (scope is null || scope.Token != token || scope.Epoch != epoch || !scope.LogicalCurrent())
            throw Invalid();
        return scope;
    }

    internal async ValueTask<AtlasPrepared<AtlasInventoryPageDto>> InventoryAsync(
        AtlasInventoryRequestDto request, IpcPeer peer, string? capability, CancellationToken cancellationToken)
    {
        var scope = RequireScope(request.ScopeToken, request.ExpectedCoreEpoch, peer, capability);
        var operation = await BeginAsync(scope, cancellationToken).ConfigureAwait(false);
        try
        {
            _ = scope.ResolveManifest(request.ManifestToken);
            var page = await scope.Queries!.InventoryAsync(new PageRequest(request.Offset, request.Limit), operation.Token).ConfigureAwait(false);
            var result = AtlasReaderProjection.Inventory(page, request, scope.Epoch, scope.Queries.ReaderCompletion,
                entry => scope.FileTokens[entry.FileValue],
                parent => parent == scope.RootToken ? null : scope.FileTokens[parent],
                _ => new AtlasCountDto(AtlasDenominatorState.Unknown, null, "total not recorded"), []);
            operation.CompleteWork();
            return new(result, operation);
        }
        catch
        {
            operation.CompleteWork();
            await operation.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    internal async ValueTask<AtlasPrepared<AtlasSelectionDto>> SelectAsync(
        AtlasSelectRequestDto request, IpcPeer peer, string? capability, CancellationToken cancellationToken)
    {
        var scope = RequireScope(request.ScopeToken, request.ExpectedCoreEpoch, peer, capability);
        var operation = await BeginAsync(scope, cancellationToken).ConfigureAwait(false);
        try
        {
            var nativeManifest = scope.ResolveManifest(request.ManifestToken);
            if (!scope.Files.TryGetValue(request.FileToken, out var file) || !_policy.AllowsContent(file, operation.Membership))
                throw new AtlasReadException("Atlas.AdmissionRefused", "This entry is not eligible for source content.");
            operation.Membership.PrepareExclusiveSourceRead(_policy.RootPath);
            string? declaration = null;
            if (request.DeclarationToken is { } declarationToken)
            {
                if (!scope.Declarations.TryGetValue(declarationToken, out var item)
                    || item.File != file.FileValue || item.Manifest != nativeManifest)
                    throw Invalid();
                declaration = item.Native;
            }
            var native = await scope.Queries!.SelectForReaderAsync(
                new SelectionRequest(nativeManifest, file.FileValue, declaration, ++scope.Sequence),
                new AtlasTextSpan(request.SourceOffset, request.SourceLength), operation.Token).ConfigureAwait(false);
            var result = ProjectSelection(scope, operation, native, request);
            operation.CompleteWork();
            return new(result, operation);
        }
        catch
        {
            operation.CompleteWork();
            await operation.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    internal async ValueTask<AtlasPrepared<AtlasSelectionDto>> RestoreAsync(
        AtlasRestoreRequestDto request, IpcPeer peer, string? capability, CancellationToken cancellationToken)
    {
        var scope = RequireScope(request.ScopeToken, request.ExpectedCoreEpoch, peer, capability);
        var operation = await BeginAsync(scope, cancellationToken).ConfigureAwait(false);
        try
        {
            if (!scope.Receipts.TryGetValue(request.ReceiptToken, out var receipt) || !scope.Queries!.HasReaderReceipt(receipt.Native))
                throw new AtlasReadException("Atlas.ReceiptUnavailable", "The retained receipt is no longer available.");
            if (!scope.Files.TryGetValue(receipt.Request.FileToken, out var file) || !_policy.AllowsContent(file, operation.Membership))
                throw Invalid();
            operation.Membership.PrepareExclusiveSourceRead(_policy.RootPath);
            var native = await scope.Queries.RestoreForReaderAsync(receipt.Native, ++scope.Sequence,
                new AtlasTextSpan(receipt.Request.SourceOffset, receipt.Request.SourceLength), operation.Token).ConfigureAwait(false);
            var result = ProjectSelection(scope, operation, native, receipt.Request);
            operation.CompleteWork();
            return new(result, operation);
        }
        catch
        {
            operation.CompleteWork();
            await operation.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private AtlasSelectionDto ProjectSelection(Scope scope, Operation operation, SelectionProjection native, AtlasSelectRequestDto request)
    {
        operation.Token.ThrowIfCancellationRequested();
        var manifest = scope.Queries!.HasReaderManifest(native.ManifestToken)
            ? scope.Manifest(native.ManifestToken) : request.ManifestToken;
        var effectiveRequest = request with { ManifestToken = manifest };
        if (native.Source.State is not SourceProjectionState.IndexedMatch)
        {
            var source = AtlasReaderProjection.Source(native.Source, NewToken(), _ => throw Invalid(), null, native.Bounds.ReturnedBytes);
            var emptySource = new AtlasBoundsDto(AtlasBoundsDimension.SourceUtf16CodeUnits, request.SourceLength, 0, 0, 0,
                AtlasDenominatorState.Withheld, null, "total withheld", "not recorded", null);
            var emptyOutline = new AtlasBoundsDto(AtlasBoundsDimension.OutlineRows, request.OutlineLimit, 0, 0, 0,
                AtlasDenominatorState.Withheld, null, "total withheld", "not recorded", null);
            return new AtlasSelectionDto(1, scope.Token, scope.Epoch, manifest, request.FileToken, request.DeclarationToken,
                null, source, emptySource, AtlasOutlineState.Unavailable, "outline unavailable", [], emptyOutline, null,
                new AtlasCoverageDto(AtlasDenominatorState.Withheld, null, "total withheld"), []);
        }
        var total = scope.Queries.ReaderSourceTotal(native);
        var page = native.Source.Page!;
        var nextSource = total.Value is { } length && page.PageSpan.End < length && page.PageSpan.Length > 0
            ? page.PageSpan.End : (int?)null;
        var outlineState = scope.Queries.ReaderOutlineState(native);
        var nextOutline = outlineState is AtlasOutlineState.Available
            && request.OutlineOffset + request.OutlineLimit < native.Outline.Declarations.Length
            ? request.OutlineOffset + request.OutlineLimit : (int?)null;
        var receiptToken = NewToken();
        if (Encoding.UTF8.GetByteCount(System.Text.Json.JsonSerializer.Serialize(effectiveRequest)) > 2048)
            throw new AtlasReadException("Atlas.Busy", "The receipt request exceeds its retained allowance.");
        foreach (var old in scope.Receipts.Where(pair => !scope.Queries.HasReaderReceipt(pair.Value.Native)).Select(pair => pair.Key).ToArray())
            scope.Receipts.Remove(old);
        if (scope.Receipts.Count >= 100) scope.Receipts.Remove(scope.Receipts.Keys.First());
        scope.Receipts[receiptToken] = new Receipt(native.ReceiptToken, effectiveRequest);
        var phase = new AtlasSelectionPhaseContext(native.ManifestToken, native.FileValue, scope.Epoch,
            NewToken(), receiptToken, total, nextSource, nextSource is null ? null : "page omitted",
            nextSource is null ? null : AtlasBoundsDimension.SourceUtf16CodeUnits,
            outlineState, outlineState is AtlasOutlineState.Available ? null
                : outlineState is AtlasOutlineState.BudgetExceeded ? "outline budget exceeded" : "outline unsupported", nextOutline);
        var result = AtlasReaderProjection.Selection(native, effectiveRequest, phase,
            binding =>
            {
                if (!scope.Bindings.TryGetValue(binding, out var token))
                {
                    token = NewToken();
                    scope.Charge(token, binding.ContentHash, binding.ManifestIdentity, binding.ManifestFileIdentity,
                        binding.PolicyIdentity, binding.RootIdentity, binding.FileIdentity);
                    scope.Bindings.Add(binding, token);
                }
                return token;
            },
            declaration =>
            {
                var found = scope.Declarations.FirstOrDefault(pair => pair.Value.Native == declaration.ObservationKey
                    && pair.Value.Manifest == native.ManifestToken).Key;
                if (found is not null) return found;
                var token = NewToken();
                scope.Charge(token, declaration.ObservationKey, native.FileValue, native.ManifestToken);
                scope.Declarations[token] = new Declaration(declaration.ObservationKey, native.FileValue, native.ManifestToken);
                return token;
            });
        scope.CheckRetention();
        return result;
    }

    private async ValueTask<Operation> BeginAsync(Scope scope, CancellationToken caller)
    {
        var started = Stopwatch.GetTimestamp();
        var work = await _budget.EnterAsync(caller).ConfigureAwait(false);
        var entered = false;
        CancellationTokenSource? linked = null;
        Operation? operation = null;
        try
        {
            await scope.Serial.WaitAsync(caller).ConfigureAwait(false);
            entered = true;
            if (!scope.LogicalCurrent()) throw Invalid();
            linked = CancellationTokenSource.CreateLinkedTokenSource(caller, scope.Invalidated.Token, _shutdown.Token);
            operation = new Operation(scope, linked, work, started);
            scope.Active = operation;
            var membership = await _membership.CaptureAsync(_policy.RootPath, _policy.RootIdentity, linked.Token).ConfigureAwait(false);
            operation.SetMembership(membership);
            if (membership.State is not AtlasMembershipCaptureState.CandidateComplete)
                throw new AtlasReadException("Atlas.MembershipUnavailable", "A current membership observation is unavailable.");
            var fingerprint = $"{membership.AssociationIdentityStamp}|{membership.Head}|{membership.IndexDigest}";
            if (scope.Fingerprint is null) scope.Fingerprint = fingerprint;
            else if (!string.Equals(scope.Fingerprint, fingerprint, StringComparison.Ordinal))
            {
                scope.Invalidate();
                throw Invalid();
            }
            return operation;
        }
        catch
        {
            if (operation is not null)
            {
                operation.CompleteWork();
                await operation.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                linked?.Dispose();
                if (entered) scope.Serial.Release();
                work.Dispose();
            }
            throw;
        }
    }

    internal async ValueTask<AtlasReleasedDto> ReleaseAsync(AtlasReleaseRequestDto request, IpcPeer peer, string? capability, CancellationToken token)
    {
        var scope = RequireScope(request.ScopeToken, request.ExpectedCoreEpoch, peer, capability);
        scope.Invalidate();
        await scope.StopAsync(token).ConfigureAwait(false);
        return new AtlasReleasedDto(1, true);
    }

    internal async ValueTask EndConnectionAsync(IpcPeer peer, AtlasConnectionEndReason reason, CancellationToken cleanupToken)
    {
        if (!_connections.TryGetValue(peer.ConnectionId, out var connection) || connection.Peer != peer) return;
        connection.Terminal = true;
        connection.Scope?.Invalidate();
        if (connection.Scope is { } scope)
            await scope.StopAsync(cleanupToken).ConfigureAwait(false);
        if (_connections.TryRemove(peer.ConnectionId, out _))
            connection.Reservation.Dispose();
    }

    private async Task ExpireAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(_shutdown.Token).ConfigureAwait(false))
            {
                foreach (var connection in _connections.Values)
                {
                    if (!connection.Terminal && connection.Scope?.Terminal is not true
                        && DateTimeOffset.UtcNow < connection.ExpiresAt) continue;
                    connection.Terminal = true;
                    connection.Scope?.Invalidate();
                    using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    try { await EndConnectionAsync(connection.Peer, AtlasConnectionEndReason.Expired, cleanup.Token).ConfigureAwait(false); }
                    catch (OperationCanceledException) { }
                    catch (AtlasRetainedCleanupException) { }
                }
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested) { }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        foreach (var connection in _connections.Values)
        {
            connection.Terminal = true;
            connection.Scope?.Invalidate();
        }
        await _shutdown.CancelAsync().ConfigureAwait(false);
        await _expiry.ConfigureAwait(false);
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        foreach (var connection in _connections.Values)
            await EndConnectionAsync(connection.Peer, AtlasConnectionEndReason.Shutdown, cleanup.Token).ConfigureAwait(false);
        _shutdown.Dispose();
    }

    private static AtlasReadException Invalid() => new("Atlas.ScopeInvalid", "The Atlas scope is no longer available.");
    private static string NewToken() => Guid.NewGuid().ToString("N");

    internal sealed class Connection(IpcPeer peer, AtlasReadBudget.Reservation reservation)
    {
        internal object Gate { get; } = new();
        internal IpcPeer Peer { get; } = peer;
        internal AtlasReadBudget.Reservation Reservation { get; } = reservation;
        internal string? Capability;
        internal Scope? Scope;
        internal volatile bool Terminal;
        internal DateTimeOffset ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(30);
    }

    internal sealed record Declaration(string Native, string File, string Manifest);
    internal sealed record Receipt(string Native, AtlasSelectRequestDto Request);

    internal sealed class Scope(AtlasReadScopeIssuer owner, Connection connection, long epoch)
    {
        internal object Gate { get; } = new();
        internal string Token { get; } = NewToken();
        internal string RootToken { get; } = NewToken();
        internal long Epoch { get; } = epoch;
        internal DateTimeOffset ExpiresAt { get; } = DateTimeOffset.UtcNow.AddMinutes(5);
        internal SemaphoreSlim Serial { get; } = new(1, 1);
        internal CancellationTokenSource Invalidated { get; } = new();
        internal Dictionary<string, AtlasFileEntry> Files { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, string> FileTokens { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, string> Manifests { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, Declaration> Declarations { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, Receipt> Receipts { get; } = new(StringComparer.Ordinal);
        internal Dictionary<AtlasSourceBinding, string> Bindings { get; } = [];
        internal AtlasQueryService? Queries;
        internal Operation? Active;
        internal string? Fingerprint;
        internal string InitialManifest = "";
        internal long Sequence;
        private long _handleBytes;
        private bool _terminal;
        private bool _stopped;
        internal bool Terminal => _terminal || Invalidated.IsCancellationRequested;

        internal bool LogicalCurrent() => !_terminal && !Invalidated.IsCancellationRequested && !connection.Terminal && !owner._disposed
            && DateTimeOffset.UtcNow < ExpiresAt && owner._policy.IsEpochCurrent(Epoch);
        internal bool OperationCurrent() => LogicalCurrent() && Active is { HasMembership: true } active
            && !active.Token.IsCancellationRequested && active.Membership.IsCurrent();

        internal void Invalidate()
        {
            lock (Gate)
            {
                if (_terminal) return;
                _terminal = true;
                Invalidated.Cancel();
            }
        }

        internal string Manifest(string native)
        {
            foreach (var stale in Manifests.Where(pair => Queries is not null && !Queries.HasReaderManifest(pair.Value)).Select(pair => pair.Key).ToArray())
                Manifests.Remove(stale);
            var existing = Manifests.FirstOrDefault(pair => pair.Value == native).Key;
            if (existing is not null) return existing;
            var token = NewToken();
            Charge(token, native);
            Manifests[token] = native;
            return token;
        }

        internal string ResolveManifest(string token) => Manifests.TryGetValue(token, out var native)
            && Queries!.HasReaderManifest(native) ? native
            : throw new AtlasReadException("Atlas.StaleManifest", "The retained manifest is no longer available.");

        internal void Charge(params string[] values)
        {
            var bytes = values.Sum(value => (long)Encoding.UTF8.GetByteCount(value));
            // A reserved encoded allowance: 2 MiB Q manifests + 512 KiB Q receipts +
            // 1 MiB handles + bounded 100 x 2 KiB reader receipts remain below 4 MiB.
            if (_handleBytes + bytes > 1024 * 1024)
                throw new AtlasReadException("Atlas.Busy", "The encoded Atlas handle allowance is occupied.");
            _handleBytes += bytes;
        }
        internal void CheckRetention()
        {
            var retained = Queries!.Retained;
            if (_handleBytes + retained.ManifestBytes + retained.ReceiptBytes > AtlasReadBudget.ScopeRetainedBytes)
                throw new AtlasReadException("Atlas.Busy", "The Atlas retained-data budget was reached.");
        }

        internal async ValueTask StopAsync(CancellationToken cancellationToken)
        {
            Invalidate();
            if (Active is { } active)
            {
                await active.WorkFinished.WaitAsync(cancellationToken).ConfigureAwait(false);
                await active.DisposeAsync().ConfigureAwait(false);
            }
            await Serial.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_stopped) return;
                Queries?.Dispose();
                Files.Clear(); FileTokens.Clear(); Manifests.Clear(); Declarations.Clear(); Receipts.Clear(); Bindings.Clear();
                _stopped = true;
            }
            finally { Serial.Release(); }
        }
    }

    internal sealed class Operation : IAsyncDisposable
    {
        private readonly Scope _scope;
        private readonly CancellationTokenSource _cancellation;
        private readonly AtlasReadBudget.Reservation _work;
        private readonly long _started;
        private readonly TaskCompletionSource _workFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly SemaphoreSlim _disposeGate = new(1, 1);
        private AtlasMembershipSnapshot? _membership;
        private bool _disposed;
        internal AtlasMembershipSnapshot Membership => _membership ?? throw Invalid();
        internal bool HasMembership => _membership is not null;
        internal CancellationToken Token => _cancellation.Token;
        internal Task WorkFinished => _workFinished.Task;
        internal void CompleteWork() => _workFinished.TrySetResult();
        internal void SetMembership(AtlasMembershipSnapshot membership) => _membership = membership;

        internal Operation(Scope scope, CancellationTokenSource cancellation,
            AtlasReadBudget.Reservation work, long started)
        {
            _scope = scope; _cancellation = cancellation; _work = work; _started = started;
        }

        internal void Commit()
        {
            lock (_scope.Gate)
            {
                if (_disposed || !_scope.OperationCurrent()) throw Invalid();
                Token.ThrowIfCancellationRequested();
            }
        }

        public async ValueTask DisposeAsync()
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await _disposeGate.WaitAsync(cleanup.Token).ConfigureAwait(false);
            try
            {
                if (_disposed) return;
                if (_membership is not null) await _membership.DisposeAsync().ConfigureAwait(false);
                _disposed = true;
                _scope.Active = null;
                _scope.Serial.Release();
                _work.Dispose();
                _cancellation.Dispose();
                Operations.Add(1);
                Duration.Record(Stopwatch.GetElapsedTime(_started).TotalMilliseconds);
            }
            catch (IOException)
            {
                _scope.Invalidate();
                throw;
            }
            finally { _disposeGate.Release(); }
        }
    }
}
