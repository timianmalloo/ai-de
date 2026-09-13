using AiDe.App.Workbench;
using AiDe.App.Workbench.Understanding;
using AiDe.Core.Understanding;
using AiDe.Core.Workbench;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Threading;
using Xunit.Abstractions;

namespace AiDe.App.Tests;

public sealed class AtlasSharedHostAdmissionTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("factory")]
    [InlineData("lease")]
    [InlineData("reader")]
    public void Repair_ThrowOnceBoundary_NextAttachRecoversWithoutLosingOwnership(string boundary)
    {
        Pump(async _ =>
        {
            using var diagnostics = new HostDiagnostics(output);
            var owner = new AtlasWorkspaceOwner();
            var first = new PortFixture();
            var next = new PortFixture();
            if (boundary == "factory")
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    owner.AttachAsync(() => throw new InvalidOperationException("synthetic factory failure")));
            }
            else
            {
                await owner.AttachAsync(() => first);
                await owner.AdmitAsync(CancellationToken.None);
                first.LeaseFailuresRemaining = boundary == "lease" ? 1 : 0;
                first.ReaderFailuresRemaining = boundary == "reader" ? 1 : 0;
                await Assert.ThrowsAsync<InvalidOperationException>(() => owner.AttachAsync(() => next));
                Assert.Same(first, Field<IAtlasWorkspaceReader>(owner, "_reader"));
            }

            await owner.AttachAsync(() => next);
            Assert.Same(next.Lease, await owner.AdmitAsync(CancellationToken.None));
            if (boundary != "factory")
            {
                Assert.True(first.Lease.Disposed);
                Assert.True(first.ReaderDisposed);
                Assert.Equal(boundary == "lease" ? 2 : 1, first.LeaseDisposalAttempts);
                Assert.Equal(boundary == "reader" ? 2 : 1, first.ReaderDisposalAttempts);
            }
            await owner.DisposeAsync();
            Assert.True(next.Lease.Disposed);
            Assert.True(next.ReaderDisposed);
            Assert.Contains("ATLAS-HOST-TRANSITION", diagnostics.Text);
        });
    }

    [Theory]
    [InlineData("lease")]
    [InlineData("reader")]
    public void Repair_CloseFailure_RetainsResourceAndRetryFinishes(string boundary)
    {
        Pump(async _ =>
        {
            using var diagnostics = new HostDiagnostics(output);
            var reader = new PortFixture();
            var owner = new AtlasWorkspaceOwner();
            await owner.AttachAsync(() => reader);
            await owner.AdmitAsync(CancellationToken.None);
            reader.LeaseFailuresRemaining = boundary == "lease" ? 1 : 0;
            reader.ReaderFailuresRemaining = boundary == "reader" ? 1 : 0;

            await Assert.ThrowsAsync<InvalidOperationException>(() => owner.DisposeAsync().AsTask());
            Assert.Same(reader, Field<IAtlasWorkspaceReader>(owner, "_reader"));
            await owner.DisposeAsync();
            await owner.DisposeAsync();

            Assert.True(reader.ReaderDisposed);
            Assert.True(reader.Lease.Disposed);
            Assert.Equal(boundary == "lease" ? 2 : 1, reader.LeaseDisposalAttempts);
            Assert.Equal(boundary == "reader" ? 2 : 1, reader.ReaderDisposalAttempts);
            Assert.Contains("ATLAS-HOST-TRANSITION", diagnostics.Text);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Repair_ClearCallbackFailure_OtherCallbacksAndCleanupContinue(bool queuedInvalidation)
    {
        Pump(async _ =>
        {
            using var diagnostics = new HostDiagnostics(output);
            var reader = new PortFixture();
            var owner = new AtlasWorkspaceOwner();
            await owner.AttachAsync(() => reader);
            await owner.AdmitAsync(CancellationToken.None);
            var clears = 0;
            using var first = owner.Register(() => throw new InvalidOperationException("synthetic clear failure"));
            using var second = owner.Register(() => ++clears);

            if (queuedInvalidation)
            {
                reader.Invalidation.Cancel();
                await Dispatcher.Yield(DispatcherPriority.Background);
            }
            else
            {
                await owner.AttachAsync(() => new PortFixture());
            }
            Assert.Equal(1, clears);
            await owner.DisposeAsync();
            Assert.Equal(2, clears);
            Assert.True(reader.ReaderDisposed);
            Assert.Contains("ATLAS-HOST-INVALIDATE", diagnostics.Text);
        });
    }

    [Theory]
    [InlineData("lifetime")]
    [InlineData("admission")]
    public void Repair_FinalClose_DrainsBeforeDisposingPrimitivesExactlyOnce(string primitive)
    {
        Pump(async _ =>
        {
            using var diagnostics = new HostDiagnostics(output);
            var reader = new PortFixture();
            var owner = new AtlasWorkspaceOwner();
            await owner.AttachAsync(() => reader);
            await owner.AdmitAsync(CancellationToken.None);
            var lifetime = Field<CancellationTokenSource>(owner, "_lifetime");
            var admission = Field<SemaphoreSlim>(owner, "_admission");
            var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var tracked = owner.Track(pending.Task);
            var close = owner.DisposeAsync().AsTask();
            Assert.False(close.IsCompleted);
            Assert.True(lifetime.Token.IsCancellationRequested);
            Assert.Equal(1, admission.CurrentCount);
            pending.SetResult();
            await tracked;
            await close;

            if (primitive == "lifetime")
                Assert.Throws<ObjectDisposedException>(() => lifetime.Token);
            else
                Assert.Throws<ObjectDisposedException>(() => admission.Wait(0));
            Assert.Same(close, owner.DisposeAsync().AsTask());
            Assert.Equal(1, reader.LeaseDisposalAttempts);
            Assert.Equal(1, reader.ReaderDisposalAttempts);
            Assert.Equal(1, diagnostics.Text.Split("operation=atlas.close", StringSplitOptions.None).Length - 1);
        });
    }

    [Fact]
    public void Repair_CloseDuringAdmission_DrainsSemaphoreUserBeforePrimitiveDisposal()
    {
        Pump(async _ =>
        {
            var admission = new TaskCompletionSource<IAtlasReaderLease>(TaskCreationOptions.RunContinuationsAsynchronously);
            var reader = new PortFixture { Admission = admission };
            var owner = new AtlasWorkspaceOwner();
            await owner.AttachAsync(() => reader);
            var admitting = owner.AdmitAsync(CancellationToken.None);
            var close = owner.DisposeAsync().AsTask();
            Assert.False(close.IsCompleted);
            admission.SetResult(reader.Lease);
            try
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => admitting);
            }
            finally
            {
                await close;
            }

            Assert.Equal(1, reader.LeaseDisposalAttempts);
            Assert.Equal(1, reader.ReaderDisposalAttempts);
            Assert.True(reader.Lease.Disposed);
            Assert.True(reader.ReaderDisposed);
            Assert.Throws<ObjectDisposedException>(() => Field<SemaphoreSlim>(owner, "_admission").Wait(0));
        });
    }

    [Fact]
    public void Repair_AdmissionFailure_ReleasesOnlyHostActivationAndCanRetry()
    {
        Pump(async _ =>
        {
            using var diagnostics = new HostDiagnostics(output);
            var reader = new PortFixture { AdmissionFailuresRemaining = 1 };
            await using var owner = new AtlasWorkspaceOwner();
            await owner.AttachAsync(() => reader);
            var host = new AtlasLoadingHost(owner);
            await host.ActivateAsync();

            Assert.Contains("ATLAS-HOST-UNAVAILABLE", host.StatusText);
            Assert.Null(host.ReaderView);
            Assert.Null(Field<object?>(host, "_activation"));
            Assert.Null(Field<object?>(host, "_registration"));
            Assert.False(reader.ReaderDisposed);
            await host.ActivateAsync();
            Assert.NotNull(host.ReaderView);
            Assert.Equal(2, reader.Admissions);
            Assert.False(reader.Lease.Disposed);
            Assert.Contains("ATLAS-HOST-UNAVAILABLE", diagnostics.Text);
        });
    }

    [Fact]
    public void Repair_InventoryFailure_IsContainedByReaderNotMisreportedAsHostFailure()
    {
        Pump(async _ =>
        {
            var reader = new PortFixture { InventoryFailuresRemaining = 1 };
            await using var owner = new AtlasWorkspaceOwner();
            await owner.AttachAsync(() => reader);
            var host = new AtlasLoadingHost(owner);
            await host.ActivateAsync();

            var view = Assert.IsType<AtlasReaderView>(host.ReaderView);
            Assert.Contains("ATLAS-READER-INVENTORY", view.StatusText);
            Assert.Same(view, host.Content);
            Assert.False(reader.Lease.Disposed);
            await host.ActivateAsync();
            Assert.Single(Assert.IsType<AtlasReaderView>(host.ReaderView).FileRoots);
            Assert.Equal(1, reader.Admissions);
        });
    }

    private static T Field<T>(object value, string name) =>
        (T)value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;

    private sealed class HostDiagnostics : TraceListener
    {
        private readonly System.Text.StringBuilder _text = new();
        private readonly ITestOutputHelper _output;
        internal HostDiagnostics(ITestOutputHelper output)
        {
            _output = output;
            Trace.Listeners.Add(this);
        }
        internal string Text { get { lock (_text) return _text.ToString(); } }
        public override void Write(string? message)
        {
            lock (_text) _text.Append(message);
        }
        public override void WriteLine(string? message)
        {
            lock (_text) _text.AppendLine(message);
        }
        protected override void Dispose(bool disposing)
        {
            Trace.Listeners.Remove(this);
            _output.WriteLine(Text);
            base.Dispose(disposing);
        }
    }

    [Fact]
    public void Factory_AtlasKind_OnlyArchitectureWithDerivedNativeEntry()
    {
        var kind = Assert.Single(SurfaceContentFactory.Kinds, row => row.Kind == "code-atlas");
        Assert.Equal("Code Atlas", kind.Title);
        Assert.Equal([PerspectiveSet.Architecture], kind.Perspectives);
        Assert.IsType<SurfaceContentFactory.SurfaceEntry.Derived>(kind.Entry);
        Assert.False(kind.Windowed);
    }

    [Fact]
    public void Reader_ProductionPort_PreservesNativeProofConstructor()
    {
        Assert.NotNull(typeof(AtlasReaderView).GetConstructor([typeof(IAtlasQueries), typeof(string)]));
        Assert.NotNull(typeof(AtlasReaderView).GetConstructor([typeof(IAtlasReaderLease)]));
    }

    [Fact]
    public void Factory_NoWorkspace_ReturnsSynchronousExplicitHost()
    {
        Sta.Run(() =>
        {
            var kind = SurfaceContentFactory.Kinds.Single(row => row.Kind == "code-atlas");
            var host = Assert.IsType<AtlasLoadingHost>(kind.Build(new SurfaceContentFactory(null), null!));
            Assert.Contains("ATLAS-HOST-NO-WORKSPACE", host.StatusText);
            Assert.Null(host.ReaderView);
        });
    }

    [Fact]
    public void Owner_ConcurrentAdmission_ReusesOneHealthyLease()
    {
        Pump(async _ =>
        {
            var reader = new PortFixture();
            await using var owner = new AtlasWorkspaceOwner();
            await owner.AttachAsync(() => reader);
            var first = owner.AdmitAsync(CancellationToken.None);
            var second = owner.AdmitAsync(CancellationToken.None);

            Assert.Same(await first, await second);
            Assert.Equal(1, reader.Admissions);
        });
    }

    [Fact]
    public void Owner_Replacement_ClearsBeforeAwaitedLeaseAndReaderDisposal()
    {
        Pump(async _ =>
        {
            var reader = new PortFixture { LeaseDisposal = new(TaskCreationOptions.RunContinuationsAsynchronously) };
            await using var owner = new AtlasWorkspaceOwner();
            await owner.AttachAsync(() => reader);
            await owner.AdmitAsync(CancellationToken.None);
            var cleared = false;
            using var registration = owner.Register(() => cleared = true);
            var next = new PortFixture();
            var installed = false;

            var replacement = owner.AttachAsync(() => { installed = true; return next; });
            Assert.True(cleared);
            Assert.False(replacement.IsCompleted);
            Assert.False(installed);
            Assert.False(reader.ReaderDisposed);
            reader.LeaseDisposal.SetResult();
            await replacement;

            Assert.True(reader.ReaderDisposed);
            Assert.True(installed);
            Assert.Same(next.Lease, await owner.AdmitAsync(CancellationToken.None));
        });
    }

    [Fact]
    public void Owner_FinalClose_WaitsForTrackedOperationsAndDisposal()
    {
        Pump(async _ =>
        {
            var reader = new PortFixture();
            var owner = new AtlasWorkspaceOwner();
            await owner.AttachAsync(() => reader);
            await owner.AdmitAsync(CancellationToken.None);
            var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var tracked = owner.Track(pending.Task);

            owner.Dispose();
            var close = owner.DisposeAsync().AsTask();
            Assert.True(owner.Token.IsCancellationRequested);
            Assert.False(close.IsCompleted);
            Assert.False(reader.ReaderDisposed);
            pending.SetResult();
            await tracked;
            await close;

            Assert.True(reader.ReaderDisposed);
            Assert.True(reader.Lease.Disposed);
            await owner.DisposeAsync();
        });
    }

    [Fact]
    public void Host_Reparent_ClearsViewAndRetainsHealthyWorkspaceLease()
    {
        Pump(async root =>
        {
            var reader = new PortFixture();
            await using var owner = new AtlasWorkspaceOwner();
            await owner.AttachAsync(() => reader);
            var host = new AtlasLoadingHost(owner);
            root.Child = host;
            await host.ActivateAsync();
            var original = Assert.IsType<AtlasReaderView>(host.ReaderView);
            await original.SelectFileAsync(original.FileRoots.Single().Children.Single());
            Assert.Equal("alpha beta", original.SourceText);

            root.Child = null;
            host.Deactivate();
            Assert.Equal("", original.SourceText);
            Assert.False(reader.Lease.Disposed);
            root.Child = host;
            await host.ActivateAsync();

            Assert.Equal(1, reader.Admissions);
            Assert.NotSame(original, host.ReaderView);
            Assert.False(reader.ReaderDisposed);
            root.Child = null;
            host.Deactivate();
        });
    }

    [Fact]
    public void Host_LateAdmissionAfterUnload_NeverPublishesAbandonedView()
    {
        Pump(async _ =>
        {
            var admission = new TaskCompletionSource<IAtlasReaderLease>(TaskCreationOptions.RunContinuationsAsynchronously);
            var reader = new PortFixture { Admission = admission };
            await using var owner = new AtlasWorkspaceOwner();
            await owner.AttachAsync(() => reader);
            var host = new AtlasLoadingHost(owner);
            var activation = host.ActivateAsync();
            host.Deactivate();
            admission.SetResult(reader.Lease);
            await activation;

            Assert.Null(host.ReaderView);
            Assert.True(reader.Lease.Disposed);
            Assert.Contains("INACTIVE", host.StatusText);
        });
    }

    [Fact]
    public void Reader_RenderPort_PreservesTokensTextSpansBoundsAndSuccessorRestore()
    {
        Pump(async root =>
        {
            var port = new PortFixture();
            var view = new AtlasReaderView(port.Lease);
            root.Child = view;
            await view.LoadAsync();
            var folder = Assert.Single(view.FileRoots);
            var file = Assert.Single(folder.Children);
            Assert.False(folder.IsFile);
            Assert.Equal("dir-token", file.ParentToken);
            Assert.Equal("file-token", file.FileValue);
            await view.SelectFileAsync(file);
            Assert.Equal("alpha beta", view.SourceText);
            Assert.Equal(6, view.SourceControl.SelectionStart);
            Assert.Equal(4, view.SourceControl.SelectionLength);
            Assert.Equal(26, Assert.Single(view.CurrentHighlights).Start);
            Assert.Contains("content bytes 0", view.BoundsText);
            await view.SelectDeclarationAsync(Assert.Single(view.OutlineRows));
            Assert.True(view.CanGoBack);
            await view.GoBackAsync();
            Assert.Equal("receipt-1", port.RestoredReceipt);
            Assert.Equal("receipt-successor", port.LastReceipt);
            Assert.False(view.CanGoBack);
        });
    }

    [Theory]
    [InlineData(SourceProjectionState.Changed)]
    [InlineData(SourceProjectionState.Unavailable)]
    [InlineData(SourceProjectionState.Unverifiable)]
    [InlineData(SourceProjectionState.UnsupportedEncoding)]
    [InlineData(SourceProjectionState.TooLargeToVerify)]
    [InlineData(SourceProjectionState.ReadUnstable)]
    [InlineData(SourceProjectionState.Refused)]
    [InlineData(SourceProjectionState.Canceled)]
    public void Reader_NonIndexedState_ClearsSourceAndPreservesOutline(SourceProjectionState state)
    {
        Pump(async _ =>
        {
            var port = new PortFixture();
            var view = new AtlasReaderView(port.Lease);
            await view.LoadAsync();
            var file = view.FileRoots.Single().Children.Single();
            await view.SelectFileAsync(file);
            port.SourceState = state;
            await view.SelectFileAsync(file);

            Assert.Equal("", view.SourceText);
            Assert.Empty(view.CurrentHighlights);
            Assert.Single(view.OutlineRows);
            Assert.NotEqual("Source ready.", view.StatusText);
        });
    }

    [Fact]
    public void Reader_CanceledLateSelection_CannotAdoptOrPaint()
    {
        Pump(async _ =>
        {
            var port = new PortFixture();
            var view = new AtlasReaderView(port.Lease);
            await view.LoadAsync();
            var pending = new TaskCompletionSource<AtlasSelectionDto>(TaskCreationOptions.RunContinuationsAsynchronously);
            port.Selection = pending;
            using var cancellation = new CancellationTokenSource();
            var selection = view.SelectFileAsync(view.FileRoots.Single().Children.Single(), cancellation.Token);
            cancellation.Cancel();
            pending.SetResult(port.Result("late"));
            await selection;

            Assert.Equal("", view.SourceText);
            Assert.False(view.CanGoBack);
        });
    }

    private static void Pump(Func<Border, Task> body)
    {
        Border? root = null;
        Sta.Pump(() => root = new Border(), async (_, _) => await body(root!), timeoutSeconds: 30);
    }

    // Synthetic public-port fixture: exercises Shell publication/lifetime, not daemon verification.
    private sealed class PortFixture : IAtlasWorkspaceReader, IAtlasReaderQueries
    {
        internal PortFixture() => Lease = new LeaseFixture(this);
        internal LeaseFixture Lease { get; }
        internal int Admissions { get; private set; }
        internal bool ReaderDisposed { get; private set; }
        internal TaskCompletionSource? LeaseDisposal { get; init; }
        internal TaskCompletionSource<IAtlasReaderLease>? Admission { get; init; }
        internal TaskCompletionSource<AtlasSelectionDto>? Selection { get; set; }
        internal SourceProjectionState SourceState { get; set; } = SourceProjectionState.IndexedMatch;
        internal string? RestoredReceipt { get; private set; }
        internal string? LastReceipt { get; private set; }
        internal CancellationTokenSource Invalidation { get; } = new();
        internal int AdmissionFailuresRemaining { get; set; }
        internal int InventoryFailuresRemaining { get; set; }
        internal int LeaseFailuresRemaining { get; set; }
        internal int ReaderFailuresRemaining { get; set; }
        internal int LeaseDisposalAttempts { get; private set; }
        internal int ReaderDisposalAttempts { get; private set; }
        private int _receipt;

        public ValueTask<IAtlasReaderLease> AdmitAsync(CancellationToken cancellationToken)
        {
            ++Admissions;
            if (AdmissionFailuresRemaining-- > 0) throw new InvalidOperationException("synthetic admission failure");
            return Admission is null ? ValueTask.FromResult<IAtlasReaderLease>(Lease) : new(Admission.Task);
        }

        public ValueTask DisposeAsync()
        {
            ++ReaderDisposalAttempts;
            if (ReaderFailuresRemaining-- > 0) throw new InvalidOperationException("synthetic reader disposal failure");
            ReaderDisposed = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask<AtlasInventoryPageDto> InventoryAsync(AtlasInventoryRequestDto request, CancellationToken cancellationToken)
        {
            if (InventoryFailuresRemaining-- > 0) throw new InvalidOperationException("synthetic inventory failure");
            return ValueTask.FromResult(new AtlasInventoryPageDto(1, "scope", 7, "accepted-manifest", default,
                [new("dir-token", AtlasDirectoryEntryKind.Directory, null, "src", default, default,
                    new(AtlasDenominatorState.Unknown, null, "metadata only"), null),
                 new("file-token", AtlasDirectoryEntryKind.File, "dir-token", "src/file.cs", default, default,
                    new(AtlasDenominatorState.Known, 1, null), null)],
                Bounds(AtlasBoundsDimension.InventoryRows, 2), null, ["synthetic public-port fixture"]));
        }

        public ValueTask<AtlasSelectionDto> SelectAsync(AtlasSelectRequestDto request, CancellationToken cancellationToken)
        {
            Assert.Equal("accepted-manifest", request.ManifestToken);
            Assert.Equal("file-token", request.FileToken);
            return Selection is null ? ValueTask.FromResult(Result($"receipt-{++_receipt}")) : new(Selection.Task);
        }

        public ValueTask<AtlasSelectionDto> RestoreAsync(AtlasRestoreRequestDto request, CancellationToken cancellationToken)
        {
            RestoredReceipt = request.ReceiptToken;
            return ValueTask.FromResult(Result("receipt-successor"));
        }

        internal AtlasSelectionDto Result(string receipt)
        {
            LastReceipt = receipt;
            var indexed = SourceState is SourceProjectionState.IndexedMatch;
            return new(1, "scope", 7, "accepted-manifest", "file-token", null, receipt,
                new(SourceState, "observation", indexed ? "binding" : null, indexed ? "utf8" : null,
                    indexed ? "alpha beta" : null, indexed ? new(20, 10) : null,
                    indexed ? [new(26, 4)] : [], null, "fixture state"),
                Bounds(AtlasBoundsDimension.SourceUtf16CodeUnits, 1),
                AtlasOutlineState.Available, null, [new("decl-token", "beta", default, new(26, 4))],
                Bounds(AtlasBoundsDimension.OutlineRows, 1), null,
                new(AtlasDenominatorState.Unknown, null, "not recorded"), []);
        }

        private static AtlasBoundsDto Bounds(AtlasBoundsDimension dimension, int rows) =>
            new(dimension, 64, 64, rows, 0, AtlasDenominatorState.Unknown, null, "not recorded", null, null);

        internal sealed class LeaseFixture(PortFixture owner) : IAtlasReaderLease
        {
            public string ScopeToken => "scope";
            public string InitialManifestToken => "initial-manifest";
            public long CoreEpoch => 7;
            public DateTimeOffset ExpiresAt => DateTimeOffset.MaxValue;
            public IAtlasReaderQueries Queries => owner;
            public CancellationToken Invalidated => owner.Invalidation.Token;
            public bool IsTerminal => Disposed || owner.Invalidation.IsCancellationRequested;
            internal bool Disposed { get; private set; }
            public async ValueTask DisposeAsync()
            {
                ++owner.LeaseDisposalAttempts;
                if (owner.LeaseFailuresRemaining-- > 0) throw new InvalidOperationException("synthetic lease disposal failure");
                if (owner.LeaseDisposal is { } pending) await pending.Task;
                Disposed = true;
            }
        }
    }
}
