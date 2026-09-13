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
        internal bool Valid(int generation) =>
            !Revoked && Policy == generation && Stopwatch.GetTimestamp() < Expires;
    }

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
    internal string PipeName { get; } = Probe.PipeName();
    internal SyntheticQueue Queue { get; }
    internal int Active => Volatile.Read(ref _active);
    internal int Handshakes => Volatile.Read(ref _handshakes);
    internal int Revoked => Volatile.Read(ref _revoked);
    internal int Dropped => Volatile.Read(ref _dropped);
    internal int Deadlines => Volatile.Read(ref _deadlines);
    internal int LiveLeases => _leases.Count;
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
            if (open is null || open.Kind != IpcMessage.Open ||
                open.Request.WorkspaceId != Probe.Workspace || open.Request.WorkspaceEpoch != 1) return;
            var connection = Guid.NewGuid().ToString("N");
            lease = new Lease(IpcPipeFactory.PeerOf(pipe, connection), LeaseLifetime, _policy);
            _leases[connection] = lease;
            Interlocked.Increment(ref _handshakes);
            await WriteAsync(pipe, IpcResponse.Success(
                JsonSerializer.SerializeToElement(new IpcOpenResult(lease.Capability, lease.Epoch))), token);

            var request = await ReadAsync(pipe, token);
            if (request is null) return;
            if (request.Kind != IpcMessage.Invoke || request.Request.Capability != lease.Capability ||
                request.Request.WorkspaceId != lease.Workspace || request.Request.WorkspaceEpoch != lease.Epoch ||
                !lease.Valid(_policy)) return;

            using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
            budget.CancelAfter(WorkBudget);
            var work = _plans.GetOrAdd(request.Request.CommandId, marker =>
            {
                var immediate = new SyntheticWork(marker);
                immediate.Release.TrySetResult();
                return immediate;
            });
            work.Token = budget.Token;
            if (!Queue.Submit(work))
            {
                await WriteAsync(pipe, IpcResponse.Error("SPIKE.BUSY", "instance queue full"), token);
                return;
            }
            using var watchStop = CancellationTokenSource.CreateLinkedTokenSource(token);
            var ended = EndedAsync(pipe, watchStop.Token);
            try
            {
                if (await Task.WhenAny(ended, work.Completion.Task) == ended)
                {
                    Revoke(lease);
                    await budget.CancelAsync();
                }
                IpcResponse response;
                try { response = Probe.Reply(await work.Completion.Task); }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref _deadlines);
                    response = IpcResponse.Error("SPIKE.DEADLINE", "bounded work ended");
                }
                if (lease.Valid(_policy))
                    await WriteAsync(pipe, response, token);
                else
                    Interlocked.Increment(ref _dropped);
            }
            finally
            {
                await watchStop.CancelAsync();
                await ended;
            }
        }
        catch (IOException) { }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally
        {
            if (lease is not null)
            {
                Revoke(lease);
                _leases.TryRemove(lease.Peer.ConnectionId, out _);
            }
        }
    }

    private static async Task<bool> EndedAsync(Stream pipe, CancellationToken token)
    {
        try { return await pipe.ReadAsync(new byte[1], token) == 0; }
        catch (IOException) { return true; }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return false; }
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
        token.ThrowIfCancellationRequested();
        await using var pipe = IpcPipeFactory.CreateClient(pipeName);
        Interlocked.Increment(ref ClientPipes);
        try
        {
            await pipe.ConnectAsync(1000, token);
            // The callback owns exactly this operation's pipe. Registration is disposed before return.
            using var registration = token.Register(static state =>
            {
                if (state is NamedPipeClientStream owned) owned.Dispose();
            }, pipe);
            Interlocked.Increment(ref Registrations);
            try
            {
                var opened = await ExchangeAsync(pipe, new IpcMessage(IpcMessage.Open,
                    new IpcRequest(IpcVersion.Current, "open", "open", Probe.Workspace, 1, null, null)), token);
                var result = opened.Payload?.Deserialize<IpcOpenResult>(Probe.Wire)
                    ?? throw new InvalidDataException("SPIKE.HANDSHAKE");
                return await ExchangeAsync(pipe, new IpcMessage(IpcMessage.Invoke,
                    new IpcRequest(IpcVersion.Current, "atlas", marker, Probe.Workspace, result.Epoch,
                        result.Capability, null)), token);
            }
            catch (Exception exception) when (token.IsCancellationRequested &&
                exception is IOException or ObjectDisposedException)
            {
                throw new OperationCanceledException("owned connection abandoned", exception, token);
            }
            finally { Interlocked.Decrement(ref Registrations); }
        }
        finally { Interlocked.Decrement(ref ClientPipes); }
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
