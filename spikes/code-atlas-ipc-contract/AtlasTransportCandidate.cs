using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading.Channels;
using AiDe.Core.Ipc;

namespace CodeAtlas.IpcContractProbe;

internal sealed class SyntheticWork(string marker, bool ignoreCancellation = false)
{
    internal string Marker { get; } = marker;
    internal bool IgnoreCancellation { get; } = ignoreCancellation;
    internal TaskCompletionSource Started { get; } = Probe.Signal();
    internal TaskCompletionSource Release { get; } = Probe.Signal();
    internal TaskCompletionSource<string> Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal CancellationToken Token { get; set; }
}

// Experimental, instance-local Q4/16; this is not the production admission scheduler.
internal sealed class SyntheticQueue : IAsyncDisposable
{
    private readonly Channel<SyntheticWork> _channel = Channel.CreateBounded<SyntheticWork>(16);
    private readonly CancellationTokenSource _stop;
    private readonly Task[] _workers;
    private readonly object _gate = new();
    private int _active;
    private int _pending;
    internal int PeakActive { get; private set; }
    internal int PeakPending { get; private set; }
    internal int Refusals { get; private set; }
    internal int Active { get { lock (_gate) return _active; } }
    internal int Pending { get { lock (_gate) return _pending; } }

    internal SyntheticQueue(CancellationToken token)
    {
        _stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        _workers = Enumerable.Range(0, 4).Select(_ => WorkerAsync()).ToArray();
    }

    internal bool Submit(SyntheticWork work)
    {
        lock (_gate)
        {
            if (_pending == 16 || !_channel.Writer.TryWrite(work))
            {
                Refusals++;
                return false;
            }
            _pending++;
            PeakPending = Math.Max(PeakPending, _pending);
            return true;
        }
    }

    private async Task WorkerAsync()
    {
        try
        {
            await foreach (var work in _channel.Reader.ReadAllAsync(_stop.Token))
            {
                lock (_gate)
                {
                    _pending--;
                    _active++;
                    PeakActive = Math.Max(PeakActive, _active);
                }
                work.Started.TrySetResult();
                try
                {
                    using var bounded = CancellationTokenSource.CreateLinkedTokenSource(
                        work.IgnoreCancellation ? _stop.Token : work.Token, _stop.Token);
                    bounded.CancelAfter(TimeSpan.FromSeconds(2));
                    await work.Release.Task.WaitAsync(bounded.Token);
                    work.Completion.TrySetResult(work.Marker);
                }
                catch (OperationCanceledException)
                {
                    work.Completion.TrySetCanceled();
                }
                finally { lock (_gate) _active--; }
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        await _stop.CancelAsync();
        await Task.WhenAll(_workers).WaitAsync(TimeSpan.FromSeconds(5));
        while (_channel.Reader.TryRead(out var work))
        {
            lock (_gate) _pending--;
            work.Completion.TrySetCanceled();
        }
        _stop.Dispose();
    }
}

internal sealed class AtlasTransportCandidate : IAsyncDisposable
{
    private sealed class Lease(IpcPeer peer, TimeSpan lifetime, int policy)
    {
        internal IpcPeer Peer { get; } = peer;
        internal string Workspace { get; } = Probe.Workspace;
        internal long Epoch { get; } = 1;
        internal string NativeRoot { get; } = "synthetic-native-root";
        internal string Capability { get; } = Guid.NewGuid().ToString("N");
        internal int Policy { get; } = policy;
        internal long Expires { get; } = Stopwatch.GetTimestamp() +
            (long)(lifetime.TotalSeconds * Stopwatch.Frequency);
        internal bool Revoked;
        internal string? Manifest;
        internal string? Receipt;
        internal string? Selected;
        internal CancellationTokenSource End { get; } = new(lifetime);
        internal bool Valid(int generation) =>
            !Revoked && Policy == generation && Stopwatch.GetTimestamp() < Expires;
    }

    private static readonly IReadOnlyDictionary<string, string> Catalog =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["alpha"] = "Synthetic alpha member body.",
            ["beta"] = "Synthetic beta member body."
        };
    private readonly CancellationTokenSource _stop;
    private readonly Task[] _listeners;
    private readonly ConcurrentDictionary<string, SyntheticWork> _plans = new();
    private readonly ConcurrentDictionary<string, Lease> _leases = new();
    private int _active;
    private int _handshakes;
    private int _revoked;
    private int _dropped;
    private int _deadlines;
    private int _policy = 7;
    private int _requestsAccepted;
    internal string PipeName { get; } = Probe.PipeName();
    internal SyntheticQueue Queue { get; }
    internal int Active => Volatile.Read(ref _active);
    internal int Handshakes => Volatile.Read(ref _handshakes);
    internal int Revoked => Volatile.Read(ref _revoked);
    internal int Dropped => Volatile.Read(ref _dropped);
    internal int Deadlines => Volatile.Read(ref _deadlines);
    internal int LiveLeases => _leases.Count;
    internal int RequestsAccepted => Volatile.Read(ref _requestsAccepted);
    internal TimeSpan LeaseLifetime { get; set; } = TimeSpan.FromSeconds(5);
    internal TimeSpan WorkBudget { get; set; } = TimeSpan.FromMilliseconds(400);
    internal static int Registrations;
    internal static int ClientPipes;

    internal AtlasTransportCandidate(CancellationToken token)
    {
        _stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        Queue = new SyntheticQueue(_stop.Token);
        _listeners = Enumerable.Range(0, 8).Select(_ => ListenAsync()).ToArray();
    }

    internal SyntheticWork Plan(string marker, bool ignoreCancellation = false)
    {
        var work = new SyntheticWork(marker, ignoreCancellation);
        if (!_plans.TryAdd(marker, work)) throw new ArgumentException("duplicate synthetic marker", nameof(marker));
        return work;
    }

    internal void RevokePolicy()
    {
        Interlocked.Increment(ref _policy);
        foreach (var lease in _leases.Values) Revoke(lease);
    }

    private void Revoke(Lease lease)
    {
        lock (lease)
        {
            if (lease.Revoked) return;
            lease.Revoked = true;
            Interlocked.Increment(ref _revoked);
            lease.End.Cancel();
        }
    }

    private async Task ListenAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                await using var pipe = IpcPipeFactory.CreateServer(PipeName, 8);
                await pipe.WaitForConnectionAsync(_stop.Token);
                Interlocked.Increment(ref _active);
                try { await ServeAsync(pipe, _stop.Token); }
                finally { Interlocked.Decrement(ref _active); }
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
    }

    private async Task ServeAsync(NamedPipeServerStream pipe, CancellationToken token)
    {
        Lease? lease = null;
        try
        {
            var open = await ReadAsync(pipe, token);
            if (open?.Request is not { } openRequest || open.Kind != IpcMessage.Open ||
                !IpcVersion.IsSupported(openRequest.Version) ||
                openRequest.WorkspaceId != Probe.Workspace || openRequest.WorkspaceEpoch != 1) return;
            var connection = Guid.NewGuid().ToString("N");
            lease = new Lease(IpcPipeFactory.PeerOf(pipe, connection), LeaseLifetime, _policy);
            _leases[connection] = lease;
            Interlocked.Increment(ref _handshakes);
            await WriteAsync(pipe, IpcResponse.Success(
                JsonSerializer.SerializeToElement(new IpcOpenResult(lease.Capability, lease.Epoch))), token);

            using var sessionEnd = CancellationTokenSource.CreateLinkedTokenSource(token, lease.End.Token);
            while (lease.Valid(_policy))
            {
                var message = await ReadAsync(pipe, sessionEnd.Token);
                if (message?.Request is not { } request) return;
                if (message.Kind != IpcMessage.Invoke || !IpcVersion.IsSupported(request.Version) ||
                    request.Capability != lease.Capability || request.WorkspaceId != lease.Workspace ||
                    request.WorkspaceEpoch != lease.Epoch || !lease.Valid(_policy)) return;
                Interlocked.Increment(ref _requestsAccepted);
                if (!await ExecuteAsync(pipe, lease, request, token)) return;
            }
        }
        catch (IOException) { }
        catch (OperationCanceledException) when (token.IsCancellationRequested ||
            lease?.End.IsCancellationRequested == true) { }
        finally
        {
            if (lease is not null)
            {
                Revoke(lease);
                _leases.TryRemove(lease.Peer.ConnectionId, out _);
                lease.End.Dispose();
            }
        }
    }

    private async Task<bool> ExecuteAsync(
        NamedPipeServerStream pipe, Lease lease, IpcRequest request, CancellationToken token)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token, lease.End.Token);
        budget.CancelAfter(WorkBudget);
        var work = _plans.GetOrAdd(request.CommandId, marker =>
        {
            var immediate = new SyntheticWork(marker);
            immediate.Release.TrySetResult();
            return immediate;
        });
        work.Token = budget.Token;
        if (!Queue.Submit(work))
        {
            _plans.TryRemove(request.CommandId, out _);
            await WriteAsync(pipe, IpcResponse.Error("SPIKE.BUSY", "instance queue full"), token);
            return true;
        }

        using var watchStop = CancellationTokenSource.CreateLinkedTokenSource(token, lease.End.Token);
        var ended = EndedAsync(pipe, watchStop.Token);
        var deadlineEnded = false;
        try
        {
            if (await Task.WhenAny(ended, work.Completion.Task) == ended)
            {
                Revoke(lease);
                await budget.CancelAsync();
            }
            try { await work.Completion.Task; }
            catch (OperationCanceledException) { deadlineEnded = true; }
        }
        finally
        {
            // End the EOF-only read BEFORE publishing a reply, so it cannot steal the next frame.
            await watchStop.CancelAsync();
            if (await ended >= 0) Revoke(lease);
            _plans.TryRemove(request.CommandId, out _);
        }
        if (!lease.Valid(_policy))
        {
            Interlocked.Increment(ref _dropped);
            return false;
        }
        IpcResponse response;
        if (deadlineEnded || budget.IsCancellationRequested)
        {
            Interlocked.Increment(ref _deadlines);
            response = IpcResponse.Error("SPIKE.DEADLINE", "bounded work ended");
        }
        else
            response = Apply(request, lease);
        await WriteAsync(pipe, response, token);
        return true;
    }

    private static IpcResponse Apply(IpcRequest request, Lease lease)
    {
        string? Input(string name) => request.Payload is { ValueKind: JsonValueKind.Object } payload &&
            payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() : null;

        switch (request.Operation)
        {
            case "inventory":
                lease.Manifest ??= Guid.NewGuid().ToString("N");
                return Inventory(lease);
            case "select":
                if (lease.Manifest is null || Input("manifest") != lease.Manifest)
                    return IpcResponse.Error("SPIKE.MANIFEST", "manifest is not owned by this scope");
                var selected = Input("member");
                if (selected is null || !Catalog.ContainsKey(selected))
                    return IpcResponse.Error("SPIKE.MEMBER", "member is not in the owned manifest");
                lease.Selected = selected;
                lease.Receipt = Guid.NewGuid().ToString("N");
                return IpcResponse.Success(JsonSerializer.SerializeToElement(new
                    { receipt = lease.Receipt, selected = lease.Selected }));
            case "member":
                if (lease.Receipt is null || Input("receipt") != lease.Receipt || lease.Selected is null)
                    return IpcResponse.Error("SPIKE.RECEIPT", "receipt is not owned by this selection");
                return IpcResponse.Success(JsonSerializer.SerializeToElement(new
                    { member = lease.Selected, content = Catalog[lease.Selected] }));
            case "back":
                if (lease.Receipt is null || Input("receipt") != lease.Receipt)
                    return IpcResponse.Error("SPIKE.RECEIPT", "receipt is not owned by this selection");
                lease.Selected = null;
                lease.Receipt = null;
                return Inventory(lease);
            default:
                return IpcResponse.Error("SPIKE.OPERATION", "unknown synthetic operation");
        }
    }

    private static IpcResponse Inventory(Lease lease) =>
        IpcResponse.Success(JsonSerializer.SerializeToElement(new
        {
            scope = lease.Peer.ConnectionId, manifest = lease.Manifest,
            members = Catalog.Keys.ToArray(), selected = lease.Selected,
            root = lease.NativeRoot, epoch = lease.Epoch, policyGeneration = lease.Policy
        }));

    private static async Task<int> EndedAsync(Stream pipe, CancellationToken token)
    {
        try { return await pipe.ReadAsync(new byte[1], token); }
        catch (IOException) { return 0; }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return -1; }
    }

    private static async Task<IpcMessage?> ReadAsync(Stream pipe, CancellationToken token)
    {
        var raw = await IpcFraming.ReadAsync(pipe, token);
        return raw is null ? null : JsonSerializer.Deserialize<IpcMessage>(raw, Probe.Wire);
    }

    internal static Task WriteAsync(Stream pipe, IpcResponse response, CancellationToken token) =>
        IpcFraming.WriteAsync(pipe, JsonSerializer.Serialize(response, Probe.Wire), token);

    internal static async Task<IpcResponse> CallAsync(string pipeName, string marker, CancellationToken token)
    {
        // One-shot helper for the malformed-handshake fixture, not the stateful session policy.
        await using var session = await AtlasSession.ConnectAsync(pipeName, token);
        return await session.InvokeAsync("inventory", marker, null, token);
    }

    internal sealed class AtlasSession : IAsyncDisposable
    {
        private sealed class Attempt(AtlasSession owner)
        {
            private readonly object _gate = new();
            private bool _started;
            private bool _completed;

            internal void Begin(CancellationToken token)
            {
                lock (_gate)
                {
                    token.ThrowIfCancellationRequested();
                    _started = true;
                }
            }

            internal void Cancel()
            {
                lock (_gate)
                    if (_started && !_completed) owner.Abandon();
            }

            internal void Complete() { lock (_gate) _completed = true; }
        }

        private readonly NamedPipeClientStream _pipe;
        private readonly SemaphoreSlim _exchange = new(1, 1);
        private string? _capability;
        private long _epoch;
        private int _terminal;
        private int _disposed;
        internal bool IsTerminal => Volatile.Read(ref _terminal) != 0;

        private AtlasSession(string name)
        {
            _pipe = IpcPipeFactory.CreateClient(name);
            Interlocked.Increment(ref AtlasTransportCandidate.ClientPipes);
        }

        internal static async Task<AtlasSession> ConnectAsync(string name, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var session = new AtlasSession(name);
            try
            {
                await session._pipe.ConnectAsync(1000, token);
                var response = await session.OwnedExchangeAsync(new IpcMessage(IpcMessage.Open,
                    new IpcRequest(IpcVersion.Current, "open", "open", Probe.Workspace, 1, null, null)), token);
                var opened = response.Payload?.Deserialize<IpcOpenResult>(Probe.Wire)
                    ?? throw new InvalidDataException("SPIKE.HANDSHAKE");
                session._capability = opened.Capability;
                session._epoch = opened.Epoch;
                return session;
            }
            catch { await session.DisposeAsync(); throw; }
        }

        internal async Task<IpcResponse> InvokeAsync(
            string operation, string commandId, JsonElement? payload, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (IsTerminal) throw new InvalidOperationException("SPIKE.TERMINAL");
            await _exchange.WaitAsync(token);
            try
            {
                token.ThrowIfCancellationRequested();
                if (IsTerminal) throw new InvalidOperationException("SPIKE.TERMINAL");
                return await OwnedExchangeAsync(new IpcMessage(IpcMessage.Invoke,
                    new IpcRequest(IpcVersion.Current, operation, commandId,
                        Probe.Workspace, _epoch, _capability, payload)), token);
            }
            finally { _exchange.Release(); }
        }

        private async Task<IpcResponse> OwnedExchangeAsync(IpcMessage message, CancellationToken token)
        {
            var attempt = new Attempt(this);
            var registration = token.Register(attempt.Cancel);
            Interlocked.Increment(ref AtlasTransportCandidate.Registrations);
            try
            {
                attempt.Begin(token);
                try
                {
                    var response = await AtlasTransportCandidate.ExchangeAsync(_pipe, message, token);
                    attempt.Complete();
                    if (response.ErrorCode == "SPIKE.CLOSED") Abandon();
                    return response;
                }
                catch (Exception exception)
                {
                    Abandon();
                    if (token.IsCancellationRequested &&
                        exception is IOException or ObjectDisposedException)
                        throw new OperationCanceledException("attempted exchange abandoned", exception, token);
                    throw;
                }
            }
            finally
            {
                await registration.DisposeAsync();
                Interlocked.Decrement(ref AtlasTransportCandidate.Registrations);
            }
        }

        private void Abandon()
        {
            if (Interlocked.Exchange(ref _terminal, 1) == 0) _pipe.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            Abandon();
            Probe.Require(await _exchange.WaitAsync(TimeSpan.FromSeconds(5)), "session exchange ownership drained");
            _exchange.Dispose();
            await _pipe.DisposeAsync();
            _capability = null;
            Interlocked.Decrement(ref AtlasTransportCandidate.ClientPipes);
        }
    }

    internal static async Task<IpcResponse> ExchangeAsync(Stream pipe, IpcMessage message, CancellationToken token)
    {
        await IpcFraming.WriteAsync(pipe, JsonSerializer.Serialize(message, Probe.Wire), token);
        var raw = await IpcFraming.ReadAsync(pipe, token);
        return raw is null ? IpcResponse.Error("SPIKE.CLOSED", "connection ended") :
            JsonSerializer.Deserialize<IpcResponse>(raw, Probe.Wire) ??
            throw new InvalidDataException("SPIKE.RESPONSE");
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        await Task.WhenAll(_listeners).WaitAsync(TimeSpan.FromSeconds(5));
        await Queue.DisposeAsync();
        _stop.Dispose();
        Probe.Require(Active == 0 && LiveLeases == 0 && Queue.Active == 0 && Queue.Pending == 0,
            "candidate server drained all owned connections, leases and work");
        Probe.Emit("candidate.cleanup", new { Active, LiveLeases, queueActive = Queue.Active,
            queuePending = Queue.Pending, Handshakes, Revoked, Dropped, Deadlines,
            clientPipes = ClientPipes, registrations = Registrations });
    }
}
