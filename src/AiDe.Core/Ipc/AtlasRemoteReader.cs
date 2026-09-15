using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using AiDe.Core.Understanding;

namespace AiDe.Core.Ipc;

/// <summary>Owns an isolated persistent Atlas connection. It never owns the general WorkspaceClient pipe.</summary>
internal sealed class AtlasRemoteReader(string pipeName) : IAtlasWorkspaceReader
{
    private static readonly Meter Meter = new("AiDe.Core.AtlasRemoteReader");
    private static readonly Counter<long> Exchanges = Meter.CreateCounter<long>("atlas.remote.exchanges");
    private static readonly ActivitySource Activities = new("aide.Core.AtlasRemoteReader");
    private readonly SemaphoreSlim _exchange = new(1, 1);
    private readonly object _state = new();
    private readonly CancellationTokenSource _shutdown = new();
    private NamedPipeClientStream? _pipe;
    private string? _capability;
    private long _epoch;
    private bool _staticStructure;
    private ReaderLease? _lease;
    private Attempt? _attempt;
    private bool _terminal = true;
    private bool _disposed;
    private int _waiting;
    private Task? _disposal;

    public async ValueTask<IAtlasReaderLease> AdmitAsync(CancellationToken cancellationToken)
    {
        using var waiting = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
        await EnterAsync(waiting.Token).ConfigureAwait(false);
        try
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AtlasRemoteReader));
            if (!_terminal && _lease is { IsTerminal: false } existing) return existing;
            await ClosePipeAsync().ConfigureAwait(false);
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Atlas uses the Windows workspace pipe.");
            var pipe = IpcPipeFactory.CreateClient(pipeName);
            _pipe = pipe;
            using var connect = CancellationTokenSource.CreateLinkedTokenSource(waiting.Token);
            connect.CancelAfter(TimeSpan.FromSeconds(10));
            await pipe.ConnectAsync(connect.Token).ConfigureAwait(false);
            lock (_state) _terminal = false;
            var open = await ExchangeCoreAsync(
                new IpcMessage(IpcMessage.Open, new IpcRequest(IpcVersion.Current, "atlas.open", NewId(),
                    pipeName, 0, null, null)),
                element => IpcPayload.Read<IpcOpenResult>(element, WorkspaceOperations.Wire)
                    ?? throw new JsonException("Missing handshake."),
                result => { _capability = result.Capability; _epoch = result.Epoch; }, waiting.Token).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(open.Capability) || open.Epoch < 0) throw new JsonException("Invalid handshake.");
            _ = await ExchangeCoreAsync(Request(AtlasWorkspaceOperations.Capabilities, new AtlasCapabilitiesRequestDto([1])),
                element => AtlasReaderProjection.DeserializeCapabilities(Bytes(element)),
                value => _staticStructure = value.Features.Contains(AtlasReaderProjection.StaticStructureFeature, StringComparer.Ordinal),
                waiting.Token).ConfigureAwait(false);
            var admission = await ExchangeCoreAsync(Request(AtlasWorkspaceOperations.Admit, new AtlasAdmitRequestDto(1, _epoch)),
                element =>
                {
                    var dto = AtlasReaderProjection.DeserializeAdmit(Bytes(element));
                    if (dto.CoreEpoch != _epoch || dto.ExpiresAt <= DateTimeOffset.UtcNow
                        || dto.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5).AddSeconds(2))
                        throw new JsonException("Admission is not current.");
                    return dto;
                },
                dto => _lease = new ReaderLease(this, dto), waiting.Token).ConfigureAwait(false);
            _ = admission;
            cancellationToken.ThrowIfCancellationRequested();
            return _lease!;
        }
        catch
        {
            if (_lease is null || _terminal)
            {
                MarkTerminal();
                await ClosePipeAsync().ConfigureAwait(false);
            }
            throw;
        }
        finally { Exit(); }
    }

    private async Task EnterAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _waiting) > 17)
        {
            Interlocked.Decrement(ref _waiting);
            throw new AtlasReadException("Atlas.Busy", "The Atlas client queue is full.");
        }
        try { await _exchange.WaitAsync(cancellationToken).ConfigureAwait(false); }
        catch { Interlocked.Decrement(ref _waiting); throw; }
    }

    private void Exit()
    {
        _exchange.Release();
        Interlocked.Decrement(ref _waiting);
    }

    private IpcMessage Request<T>(string operation, T payload) => new(IpcMessage.Invoke,
        new IpcRequest(IpcVersion.Current, operation, NewId(), pipeName, _epoch, _capability,
            IpcPayload.From(payload, WorkspaceOperations.Wire)));

    private async ValueTask<T> ExchangeCoreAsync<T>(
        IpcMessage message, Func<JsonElement?, T> validate, Action<T> adopt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var text = JsonSerializer.Serialize(message, WorkspaceOperations.Wire);
        if (Encoding.UTF8.GetByteCount(text) > AtlasReaderProjection.MaxFrameBodyBytes)
            throw new AtlasReadException("Atlas.PayloadTooLarge", "The Atlas request exceeds one frame.");
        Attempt attempt;
        NamedPipeClientStream pipe;
        lock (_state)
        {
            if (_terminal || _pipe is null) throw Invalid();
            pipe = _pipe;
            attempt = new Attempt();
            _attempt = attempt;
        }
        using var registration = cancellationToken.Register(() =>
        {
            lock (_state)
                if (ReferenceEquals(_attempt, attempt) && attempt.MayHaveWritten)
                    MarkTerminalLocked();
        });
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_state)
            {
                if (_terminal) throw Invalid();
                attempt.MayHaveWritten = true;
            }
            using var activity = Activities.StartActivity("atlas.remote.exchange", ActivityKind.Client,
                default(ActivityContext), [new KeyValuePair<string, object?>("rpc.method", message.Request.Operation)]);
            await IpcFraming.WriteAsync(pipe, text, cancellationToken).ConfigureAwait(false);
            var raw = await IpcFraming.ReadAsync(pipe, cancellationToken).ConfigureAwait(false)
                ?? throw new EndOfStreamException("Atlas connection ended.");
            T result;
            lock (_state)
            {
                if (_terminal) throw Invalid();
                var response = AtlasReaderProjection.DeserializeIpcResponse(Encoding.UTF8.GetBytes(raw));
                if (!response.Ok)
                {
                    _attempt = null;
                    if (response.ErrorCode is not ("Atlas.DeadlineExceeded" or "Atlas.Busy" or "Atlas.Malformed"
                        or "Atlas.PayloadTooLarge" or "Atlas.ReceiptUnavailable" or "Atlas.MembershipUnavailable"))
                        MarkTerminalLocked();
                    throw new AtlasReadException(response.ErrorCode!, response.Reason!);
                }
                result = validate(response.Payload);
                adopt(result);
                _attempt = null;
            }
            Exchanges.Add(1);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
        catch (AtlasReadException) when (!ReferenceEquals(_attempt, attempt)) { throw; }
        catch
        {
            lock (_state)
                if (ReferenceEquals(_attempt, attempt))
                    MarkTerminalLocked();
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
        finally
        {
            lock (_state)
                if (ReferenceEquals(_attempt, attempt))
                    _attempt = null;
        }
    }

    private async ValueTask<T> QueryAsync<T>(ReaderLease lease, string operation, object request,
        Func<JsonElement?, T> validate, Action<T> adopt, CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lease.Invalidated, _shutdown.Token);
        await EnterAsync(linked.Token).ConfigureAwait(false);
        try
        {
            if (_terminal || !ReferenceEquals(_lease, lease) || lease.IsTerminal) throw Invalid();
            lock (_state) lease.ValidateQueuedRequest(request);
            return await ExchangeCoreAsync(Request(operation, request), validate, adopt, linked.Token).ConfigureAwait(false);
        }
        finally { Exit(); }
    }

    private void MarkTerminal()
    {
        lock (_state) MarkTerminalLocked();
    }

    private void MarkTerminalLocked()
    {
        _terminal = true;
        _lease?.Invalidate();
        _pipe?.Dispose();
        _capability = null;
    }

    private async ValueTask ClosePipeAsync()
    {
        _lease?.Invalidate();
        if (_pipe is { } pipe) await pipe.DisposeAsync().ConfigureAwait(false);
        _lease?.ReleaseCancellationOwnership();
        _pipe = null;
        _lease = null;
        _capability = null;
    }

    private async ValueTask ReleaseLeaseAsync(ReaderLease lease)
    {
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await EnterAsync(cleanup.Token).ConfigureAwait(false);
        try
        {
            if (!ReferenceEquals(_lease, lease)) { lease.Invalidate(); return; }
            try
            {
                if (!_terminal)
                    _ = await ExchangeCoreAsync(Request(AtlasWorkspaceOperations.Release,
                        new AtlasReleaseRequestDto(1, lease.ScopeToken, lease.CoreEpoch)),
                        element => AtlasReaderProjection.DeserializeReleased(Bytes(element)), _ => { }, cleanup.Token).ConfigureAwait(false);
            }
            finally
            {
                MarkTerminal();
                await ClosePipeAsync().ConfigureAwait(false);
            }
        }
        finally { Exit(); }
    }

    public ValueTask DisposeAsync()
    {
        lock (_state)
        {
            if (_disposal is null || _disposal.IsFaulted || _disposal.IsCanceled)
                _disposal = DisposeCoreAsync();
            return new ValueTask(_disposal);
        }
    }

    private async Task DisposeCoreAsync()
    {
        // Start outside the state lock held while publishing the owned disposal task.
        await Task.Yield();
        _disposed = true;
        MarkTerminal();
        await _shutdown.CancelAsync().ConfigureAwait(false);
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await EnterAsync(cleanup.Token).ConfigureAwait(false);
        try { await ClosePipeAsync().ConfigureAwait(false); }
        finally { Exit(); }
    }

    private static byte[] Bytes(JsonElement? element) => element is { ValueKind: JsonValueKind.Object } value
        ? Encoding.UTF8.GetBytes(value.GetRawText()) : throw new JsonException("Atlas response requires an object.");
    private static string NewId() => Guid.NewGuid().ToString("N");
    private static AtlasReadException Invalid() => new("Atlas.ScopeInvalid", "The Atlas lease is terminal; admit again.");
    private sealed class Attempt { internal bool MayHaveWritten; }

    private sealed class ReaderLease : IAtlasReaderLease, IAtlasReaderQueries
    {
        private readonly AtlasRemoteReader _owner;
        private readonly CancellationTokenSource _invalidated = new();
        private readonly CancellationToken _invalidatedToken;
        private readonly CancellationTokenRegistration _expiry;
        private readonly Dictionary<string, AtlasSelectRequestDto> _receipts = new(StringComparer.Ordinal);
        private string _manifest;
        private bool _inventoryRead;
        public string ScopeToken { get; }
        public string InitialManifestToken { get; }
        public long CoreEpoch { get; }
        public DateTimeOffset ExpiresAt { get; }
        public IAtlasReaderQueries Queries => this;
        public CancellationToken Invalidated => _invalidatedToken;
        public bool IsTerminal => _invalidated.IsCancellationRequested;

        internal ReaderLease(AtlasRemoteReader owner, AtlasAdmitDto admission)
        {
            _owner = owner;
            ScopeToken = admission.ScopeToken;
            InitialManifestToken = _manifest = admission.InitialManifestToken;
            CoreEpoch = admission.CoreEpoch;
            ExpiresAt = admission.ExpiresAt;
            _invalidatedToken = _invalidated.Token;
            _expiry = _invalidated.Token.Register(() =>
            {
                lock (owner._state)
                    if (ReferenceEquals(owner._lease, this) && !owner._terminal)
                        owner.MarkTerminalLocked();
            });
            _invalidated.CancelAfter(admission.ExpiresAt - DateTimeOffset.UtcNow);
        }

        internal void Invalidate()
        {
            if (!_invalidated.IsCancellationRequested) _invalidated.Cancel();
            _receipts.Clear();
        }

        internal void ReleaseCancellationOwnership()
        {
            _expiry.Dispose();
            _invalidated.Dispose();
        }

        internal void ValidateQueuedRequest(object request)
        {
            switch (request)
            {
                case AtlasInventoryRequestDto inventory:
                    Match(inventory.ScopeToken, inventory.ExpectedCoreEpoch);
                    if (inventory.ManifestToken != _manifest && inventory.ManifestToken != InitialManifestToken) throw Invalid();
                    break;
                case AtlasSelectRequestDto selection:
                    Match(selection.ScopeToken, selection.ExpectedCoreEpoch);
                    if (!_inventoryRead || selection.ManifestToken != _manifest) throw Invalid();
                    break;
                case AtlasRestoreRequestDto restore:
                    Match(restore.ScopeToken, restore.ExpectedCoreEpoch);
                    if (!_inventoryRead || !_receipts.ContainsKey(restore.ReceiptToken))
                        throw new AtlasReadException("Atlas.ReceiptUnavailable", "The receipt is no longer retained.");
                    break;
            }
        }

        private void Match(string scope, long epoch)
        {
            if (scope != ScopeToken || epoch != CoreEpoch || IsTerminal) throw Invalid();
        }

        public ValueTask<AtlasInventoryPageDto> InventoryAsync(AtlasInventoryRequestDto request, CancellationToken cancellationToken)
        {
            Match(request.ScopeToken, request.ExpectedCoreEpoch);
            if (request.ManifestToken != _manifest && request.ManifestToken != InitialManifestToken) throw Invalid();
            return _owner.QueryAsync(this, AtlasWorkspaceOperations.Inventory, request,
                element => AtlasReaderProjection.DeserializeInventory(Bytes(element), request),
                value => { _manifest = value.ManifestToken; _inventoryRead = true; }, cancellationToken);
        }

        public ValueTask<AtlasSelectionDto> SelectAsync(AtlasSelectRequestDto request, CancellationToken cancellationToken)
        {
            Match(request.ScopeToken, request.ExpectedCoreEpoch);
            if (!_inventoryRead || request.ManifestToken != _manifest) throw Invalid();
            request = request with { StaticStructure = _owner._staticStructure && request.StaticStructure is true ? true : null };
            return _owner.QueryAsync(this, AtlasWorkspaceOperations.Select, request,
                element => DecodeSelection(element, request),
                value => Adopt(value, request), cancellationToken);
        }

        public ValueTask<AtlasSelectionDto> RestoreAsync(AtlasRestoreRequestDto request, CancellationToken cancellationToken)
        {
            AtlasSelectRequestDto original;
            lock (_owner._state)
            {
                Match(request.ScopeToken, request.ExpectedCoreEpoch);
                if (!_inventoryRead || !_receipts.TryGetValue(request.ReceiptToken, out original!))
                    throw new AtlasReadException("Atlas.ReceiptUnavailable", "The receipt is not owned by this lease.");
            }
            return _owner.QueryAsync(this, AtlasWorkspaceOperations.Restore, request,
                element => DecodeSelection(element, original),
                value => Adopt(value, original), cancellationToken);
        }

        private static AtlasSelectionDto DecodeSelection(JsonElement? element, AtlasSelectRequestDto original)
        {
            var bytes = Bytes(element);
            using var document = JsonDocument.Parse(bytes);
            var issuedManifest = document.RootElement.GetProperty("manifestToken").GetString()
                ?? throw new JsonException("Missing issued manifest.");
            // The original manifest was checked before exchange. Native Q may issue a successor,
            // adopted only after this correlated, authenticated response validates in full.
            return AtlasReaderProjection.DeserializeSelection(bytes, original with { ManifestToken = issuedManifest });
        }

        private void Adopt(AtlasSelectionDto value, AtlasSelectRequestDto original)
        {
            _manifest = value.ManifestToken;
            if (value.ReceiptToken is not { } receipt) return;
            if (_receipts.Count >= 100) _receipts.Remove(_receipts.Keys.First());
            _receipts[receipt] = original with { ManifestToken = value.ManifestToken };
        }

        public async ValueTask DisposeAsync()
        {
            await _owner.ReleaseLeaseAsync(this).ConfigureAwait(false);
            _expiry.Dispose();
        }
    }
}
