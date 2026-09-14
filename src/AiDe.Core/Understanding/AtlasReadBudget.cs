using System.Diagnostics.Metrics;

namespace AiDe.Core.Understanding;

internal sealed class AtlasReadException(string code, string reason) : Exception(reason)
{
    internal string Code { get; } = code;
}

/// <summary>Process-wide FIFO admission. Reservations remain charged until their owner actually drains.</summary>
internal sealed class AtlasReadBudget
{
    internal const int MaxScopes = 4;
    internal const int MaxActive = 4;
    internal const int MaxPending = 16;
    internal const long MaxOwnedBytes = 64L * 1024 * 1024;
    internal const long MaxRetainedBytes = 16L * 1024 * 1024;
    internal const long OperationBytes = 14L * 1024 * 1024;
    internal const long ConnectionBytes = 2L * 1024 * 1024;
    internal const long ScopeRetainedBytes = 4L * 1024 * 1024;
    internal static AtlasReadBudget ProcessWide { get; } = new();
    private static readonly Meter Meter = new("AiDe.Core.AtlasReadBudget");
    private static readonly Counter<long> Refusals = Meter.CreateCounter<long>("atlas.budget.refusals");
    private readonly object _gate = new();
    private readonly LinkedList<Waiter> _pending = [];
    private readonly SemaphoreSlim _compiler = new(1, 1);
    private int _scopes;
    private int _active;
    private long _owned;
    private long _retained;

    internal (int Scopes, int Active, int Pending, long Owned, long Retained) Read()
    {
        lock (_gate)
            return (_scopes, _active, _pending.Count, _owned, _retained);
    }

    internal Reservation ReserveScope()
    {
        lock (_gate)
        {
            if (_scopes >= MaxScopes || _owned + ConnectionBytes > MaxOwnedBytes
                || _retained + ScopeRetainedBytes > MaxRetainedBytes)
                throw Busy();
            _scopes++;
            _owned += ConnectionBytes;
            _retained += ScopeRetainedBytes;
        }
        return new Reservation(() =>
        {
            lock (_gate)
            {
                _scopes--;
                _owned -= ConnectionBytes;
                _retained -= ScopeRetainedBytes;
            }
        });
    }

    internal ValueTask<Reservation> EnterAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_active < MaxActive && _pending.Count == 0)
                return ValueTask.FromResult(Activate());
            if (_pending.Count >= MaxPending)
                throw Busy();
            var waiter = new Waiter(cancellationToken);
            waiter.Node = _pending.AddLast(waiter);
            return new ValueTask<Reservation>(WaitAsync(waiter));
        }
    }

    private async Task<Reservation> WaitAsync(Waiter waiter)
    {
        using var registration = waiter.Cancellation.Register(() =>
        {
            lock (_gate)
            {
                if (waiter.Node?.List is not null)
                {
                    _pending.Remove(waiter.Node);
                    waiter.Completion.TrySetCanceled(waiter.Cancellation);
                }
            }
        });
        return await waiter.Completion.Task.ConfigureAwait(false);
    }

    private Reservation Activate()
    {
        if (_owned + OperationBytes > MaxOwnedBytes)
            throw Busy();
        _active++;
        _owned += OperationBytes;
        return new Reservation(() =>
        {
            lock (_gate)
            {
                _active--;
                _owned -= OperationBytes;
                while (_pending.First is { } node)
                {
                    _pending.RemoveFirst();
                    if (node.Value.Cancellation.IsCancellationRequested)
                    {
                        node.Value.Completion.TrySetCanceled(node.Value.Cancellation);
                        continue;
                    }
                    node.Value.Completion.SetResult(Activate());
                    break;
                }
            }
        });
    }

    internal async ValueTask<Reservation> EnterCompilerAsync(CancellationToken cancellationToken)
    {
        await _compiler.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Reservation(() => _compiler.Release());
    }

    private static AtlasReadException Busy()
    {
        Refusals.Add(1);
        return new AtlasReadException("Atlas.Busy", "Atlas resource capacity is occupied.");
    }

    internal sealed class Reservation(Action release) : IDisposable
    {
        private Action? _release = release;
        public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
    }

    private sealed class Waiter(CancellationToken cancellation)
    {
        internal CancellationToken Cancellation { get; } = cancellation;
        internal TaskCompletionSource<Reservation> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal LinkedListNode<Waiter>? Node { get; set; }
    }
}
