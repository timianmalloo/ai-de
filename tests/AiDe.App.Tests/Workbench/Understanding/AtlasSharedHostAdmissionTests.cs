using AiDe.App.Workbench;
using AiDe.App.Workbench.Understanding;
using AiDe.Core.Understanding;
using AiDe.Core.Workbench;
using System.Windows;
using System.Windows.Controls;

namespace AiDe.App.Tests;

public sealed class AtlasSharedHostAdmissionTests
{
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
        private int _receipt;

        public ValueTask<IAtlasReaderLease> AdmitAsync(CancellationToken cancellationToken)
        {
            ++Admissions;
            return Admission is null ? ValueTask.FromResult<IAtlasReaderLease>(Lease) : new(Admission.Task);
        }

        public ValueTask DisposeAsync()
        {
            ReaderDisposed = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask<AtlasInventoryPageDto> InventoryAsync(AtlasInventoryRequestDto request, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new AtlasInventoryPageDto(1, "scope", 7, "accepted-manifest", default,
                [new("dir-token", AtlasDirectoryEntryKind.Directory, null, "src", default, default,
                    new(AtlasDenominatorState.Unknown, null, "metadata only"), null),
                 new("file-token", AtlasDirectoryEntryKind.File, "dir-token", "src/file.cs", default, default,
                    new(AtlasDenominatorState.Known, 1, null), null)],
                Bounds(AtlasBoundsDimension.InventoryRows, 2), null, ["synthetic public-port fixture"]));

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
            public CancellationToken Invalidated => CancellationToken.None;
            public bool IsTerminal => Disposed;
            internal bool Disposed { get; private set; }
            public async ValueTask DisposeAsync()
            {
                if (owner.LeaseDisposal is { } pending) await pending.Task;
                Disposed = true;
            }
        }
    }
}
