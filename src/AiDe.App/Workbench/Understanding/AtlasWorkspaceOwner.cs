using System.Diagnostics;
using System.Windows.Threading;
using AiDe.Core.Understanding;

namespace AiDe.App.Workbench.Understanding;

/// <summary>Owns workspace admission independently of the lifetime of any docked view.</summary>
internal sealed class AtlasWorkspaceOwner : IDisposable, IAsyncDisposable
{
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    private readonly SemaphoreSlim _admission = new(1, 1);
    private readonly HashSet<Task> _operations = [];
    private readonly HashSet<Action> _views = [];
    private CancellationTokenSource _lifetime = new();
    private IAtlasWorkspaceReader? _reader;
    private IAtlasReaderLease? _lease;
    private CancellationTokenRegistration _invalidation;
    private Task _transition = Task.CompletedTask;
    private Task? _close;
    private bool _disposed;

    internal long Generation { get; private set; }
    internal CancellationToken Token => _lifetime.Token;

    internal Task AttachAsync(Func<IAtlasWorkspaceReader>? factory)
    {
        _dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(_disposed, this);
        InvalidateViews();
        return _transition = ReplaceAsync(_transition, factory, Generation);
    }

    internal IDisposable Register(Action clear)
    {
        _dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(_disposed, this);
        _views.Add(clear);
        return new ViewRegistration(this, clear);
    }

    internal Task<IAtlasReaderLease> AdmitAsync(CancellationToken cancellationToken) =>
        Track(AdmitCoreAsync(Generation, cancellationToken));

    internal Task Track(Task operation)
    {
        lock (_operations) _operations.Add(operation);
        return ObserveAsync(operation);
    }

    private async Task<T> Track<T>(Task<T> operation)
    {
        await Track((Task)operation).ConfigureAwait(true);
        return await operation.ConfigureAwait(true);
    }

    private async Task ObserveAsync(Task operation)
    {
        try { await operation.ConfigureAwait(true); }
        finally { lock (_operations) _operations.Remove(operation); }
    }

    private async Task<IAtlasReaderLease> AdmitCoreAsync(long generation, CancellationToken token)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, Token);
        var started = Stopwatch.GetTimestamp();
        await _admission.WaitAsync(linked.Token).ConfigureAwait(true);
        try
        {
            linked.Token.ThrowIfCancellationRequested();
            if (generation != Generation || _disposed)
                throw new OperationCanceledException(linked.Token);
            if (_lease is not null)
            {
                if (_lease.IsTerminal || _lease.Invalidated.IsCancellationRequested)
                    throw new InvalidOperationException("ATLAS-HOST-TERMINAL");
                return _lease;
            }

            var reader = _reader ?? throw new InvalidOperationException("ATLAS-HOST-NO-WORKSPACE");
            var candidate = await reader.AdmitAsync(linked.Token).ConfigureAwait(true);
            if (generation != Generation || linked.IsCancellationRequested || candidate.IsTerminal
                || candidate.Invalidated.IsCancellationRequested)
            {
                await candidate.DisposeAsync().ConfigureAwait(true);
                throw new OperationCanceledException(linked.Token);
            }

            _lease = candidate;
            _invalidation = candidate.Invalidated.Register(() =>
            {
                var publication = _dispatcher.InvokeAsync(() =>
                {
                    if (generation == Generation && ReferenceEquals(_lease, candidate))
                        InvalidateViews();
                }).Task;
                _ = Track(publication);
            });
            return candidate;
        }
        finally
        {
            _admission.Release();
            Trace.TraceInformation("operation=atlas.admit duration_ms={0} scope.current={1}",
                Stopwatch.GetElapsedTime(started).TotalMilliseconds, generation == Generation);
        }
    }

    private void InvalidateViews()
    {
        ++Generation;
        _lifetime.Cancel();
        foreach (var clear in _views.ToArray()) clear();
    }

    private async Task ReplaceAsync(Task previous, Func<IAtlasWorkspaceReader>? factory, long generation)
    {
        await previous.ConfigureAwait(true);
        Task[] pending;
        lock (_operations) pending = _operations.ToArray();
        try { await Task.WhenAll(pending).ConfigureAwait(true); }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Trace.TraceInformation("code=ATLAS-HOST-DRAIN exception.type={0}", ex.GetType().FullName);
        }

        _invalidation.Dispose();
        var lease = _lease;
        var reader = _reader;
        _lease = null;
        _reader = null;
        try
        {
            if (lease is not null) await lease.DisposeAsync().ConfigureAwait(true);
        }
        finally
        {
            if (reader is not null) await reader.DisposeAsync().ConfigureAwait(true);
        }

        if (generation != Generation || _disposed) return;
        _lifetime.Dispose();
        _lifetime = new CancellationTokenSource();
        _reader = factory?.Invoke();
    }

    /// <summary>Compatibility close cancels synchronously; callers await DisposeAsync to drain.</summary>
    public void Dispose()
    {
        _dispatcher.VerifyAccess();
        if (_disposed) return;
        _disposed = true;
        InvalidateViews();
    }

    public ValueTask DisposeAsync()
    {
        _dispatcher.VerifyAccess();
        Dispose();
        return new ValueTask(_close ??= ReplaceAsync(_transition, null, Generation));
    }

    private sealed class ViewRegistration(AtlasWorkspaceOwner owner, Action clear) : IDisposable
    {
        public void Dispose() => owner._views.Remove(clear);
    }
}
