using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using AiDe.App.ViewModels;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Understanding;
using AiDe.Core.Ipc;
using AiDe.Core.Understanding;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

public sealed class AtlasDaemonMainWindowProofTests
{
    private static readonly string Tree = RepositoryRoot();
    private const string Git = @"C:\Program Files\Git\cmd\git.exe";
    private const string Source = "namespace ProofOwned;\npublic sealed class Widget\n{\n    public int Answer() => 42;\n}\n";

    [Fact]
    public async Task TransitionControl_Publication_NotificationDoesNotCompleteBeforeTail()
    {
        var receipt = ObserverReceipt();
        await RunDispatcherAsync(async () =>
        {
            await using var owner = new AtlasWorkspaceOwner();
            var port = new ObserverPort();
            await owner.AttachAsync(() => port);
            var host = new AtlasLoadingHost(owner);
            AtlasInventoryPageDto? page = await port.InventoryAsync(new(1,"control",7,"control",0,64),default);
            using var publication = new TransitionPublication(host, () => page, () => true);
            bool? completeInsideNotification = null;
            publication.FinalNotificationForControl = () => completeInsideNotification = publication.Ready.IsCompleted;
            await host.ActivateAsync();
            await publication.Ready;
            Assert.False(completeInsideNotification);
            Assert.Equal(1, publication.TailCount);
            host.Deactivate();
        }, receipt);
    }

    [Fact]
    public async Task TransitionControl_Escrow_FailedPrehandoffDisposalRetainsCandidate()
    {
        var lease = new TransitionControlLease { FailDisposal = true };
        var reader = new TransitionControlReader(lease);
        var escrow = new TransitionEscrow(reader);
        using var cancel = new CancellationTokenSource();
        var admission = escrow.AdmitAsync(cancel.Token).AsTask();
        await escrow.Held;
        cancel.Cancel();
        await Assert.ThrowsAsync<InvalidOperationException>(() => admission);
        Assert.Same(lease, escrow.OwnedCandidate);
        Assert.Equal(0, escrow.Handoffs);
        lease.FailDisposal = false;
        await escrow.DisposeAsync();
        Assert.Null(escrow.OwnedCandidate);
        Assert.Equal(2, lease.Disposals);
        Assert.Equal(1, reader.Disposals);
    }

    private sealed class TransitionPublication : IDisposable
    {
        private readonly AtlasLoadingHost _host;
        private readonly Func<AtlasInventoryPageDto?> _page;
        private readonly Func<bool> _surface;
        private readonly System.ComponentModel.DependencyPropertyDescriptor _content;
        private System.ComponentModel.DependencyPropertyDescriptor? _text;
        private AtlasReaderView? _view;
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _closed;
        private bool _scheduled;
        private Exception? _failure;
        private readonly Receipt? _receipt;
        private DispatcherOperation? _operation;
        internal int Replacements { get; private set; }
        internal int FinalNotifications { get; private set; }
        internal int LoadedEvents { get; private set; }
        internal int UnloadedEvents { get; private set; }
        internal AtlasReaderView? View => _view;
        internal Task Ready => _ready.Task;
        internal int TailCount { get; private set; }
        internal Action? FinalNotificationForControl { get; set; }
        internal TransitionPublication(AtlasLoadingHost host, Func<AtlasInventoryPageDto?> page, Func<bool> surface, Receipt? receipt=null)
        {
            _host=host;_page=page;_surface=surface;_receipt=receipt;
            if(host.ReaderView is not null)throw new InvalidOperationException("TRANSITION-LATE-SUBSCRIPTION");
            _content=System.ComponentModel.DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty,host.GetType())!;
            _content.AddValueChanged(host,ContentChanged);
        }
        private void ContentChanged(object? sender,EventArgs args)
        {
            if(_closed)return;
            if(_view is not null){Fail("TRANSITION-CONTENT-CHANGED");return;}
            if(_host.Content is not AtlasReaderView view)return;
            Replacements++;
            _view=view;
            _text=System.ComponentModel.DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty,typeof(TextBlock))!;
            _text.AddValueChanged(view.StatusControl,StatusChanged);
            view.Loaded+=Loaded;view.Unloaded+=Unloaded;
            Mark("content-reader");
        }
        private void StatusChanged(object? sender,EventArgs args)
        {
            if(_closed)return;
            Mark("status-notification");
            if(_view?.StatusText.StartsWith("ATLAS-READER-",StringComparison.Ordinal)==true)
            {Fail("TRANSITION-INVENTORY-FAILED");return;}
            if(_page() is not { } page || _view?.StatusText!=TransitionStatus(page))return;
            FinalNotifications++;
            if(_scheduled){Fail("TRANSITION-DUPLICATE-PUBLICATION");return;}
            _scheduled=true;
            _operation=_host.Dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(Tail));
            _operation.Aborted+=Aborted;
            Mark("tail-scheduled");
            FinalNotificationForControl?.Invoke();
        }
        private void Loaded(object sender,RoutedEventArgs args){if(_closed)return;LoadedEvents++;if(LoadedEvents>1)Fail("TRANSITION-EXTRA-LOADED");}
        private void Unloaded(object sender,RoutedEventArgs args){if(_closed)return;UnloadedEvents++;Fail("TRANSITION-UNLOADED");}
        private void Aborted(object? sender,EventArgs args){if(!_closed)Fail("TRANSITION-DISPATCH-ABORTED");}
        private void Tail()
        {
            if(_closed)return;
            TailCount++;Mark("tail-enter");
            try{AssertReady();_ready.TrySetResult();Mark("publication-ready");}
            catch(Exception error){_failure=error;_ready.TrySetException(error);Mark("publication-rejected");}
        }
        internal void AssertReady()
        {
            if(_closed)throw new InvalidOperationException("TRANSITION-CLOSED");
            if(_failure is not null)throw _failure;
            var page=_page()??throw new InvalidOperationException("TRANSITION-NO-PAGE");
            var view=_view??throw new InvalidOperationException("TRANSITION-NO-VIEW");
            var actual=Flatten(view.FileRoots).Select(n=>(n.EntryToken,n.ParentToken,n.RelativePath)).OrderBy(n=>n.EntryToken).ToArray();
            var expected=page.Files.Select(n=>(EntryToken:(string?)n.FileToken,n.ParentToken,n.RelativePath)).OrderBy(n=>n.EntryToken).ToArray();
            if(Replacements!=1 || FinalNotifications!=1 || TailCount!=1 || UnloadedEvents!=0
                || !ReferenceEquals(_host.Content,view) || !ReferenceEquals(_host.ReaderView,view)
                || !_surface() || !actual.SequenceEqual(expected) || view.StatusText!=TransitionStatus(page)
                || view.BoundsText!=TransitionBounds(page.Bounds)
                || view.LoadMoreButton.Visibility!=(page.NextOffset.HasValue?Visibility.Visible:Visibility.Collapsed)
                || view.LoadMoreButton.IsEnabled!=page.NextOffset.HasValue)
                throw new InvalidOperationException("TRANSITION-PUBLICATION-PREDICATE");
        }
        internal void Cancel(){if(_closed)return;Fail("TRANSITION-CANCELED");Dispose();}
        private void Fail(string code){_failure??=new InvalidOperationException(code);_ready.TrySetException(_failure);Mark(code);}
        private void Mark(string phase)=>_receipt?.Mark("transition.publication",new
        {Phase=phase,Tick=Stopwatch.GetTimestamp(),Utc=DateTimeOffset.UtcNow,ThreadId=Environment.CurrentManagedThreadId,
            Apartment=Thread.CurrentThread.GetApartmentState().ToString(),Replacements,FinalNotifications,TailCount,LoadedEvents,UnloadedEvents,
            ContentIsReader=ReferenceEquals(_host.Content,_view),Owner="not-recorded"});
        public void Dispose()
        {
            if(_closed)return;
            _closed=true;
            _content.RemoveValueChanged(_host,ContentChanged);
            if(_view is not null){_text?.RemoveValueChanged(_view.StatusControl,StatusChanged);_view.Loaded-=Loaded;_view.Unloaded-=Unloaded;}
            if(_operation is not null)_operation.Aborted-=Aborted;
            _ready.TrySetCanceled();_view=null;_text=null;_operation=null;FinalNotificationForControl=null;
        }
    }
    private static string TransitionStatus(AtlasInventoryPageDto page)=>
        $"{page.Completion}; showing {page.Files.Length} metadata rows. {string.Join(" ",page.Disclosures)}";
    private static string TransitionBounds(AtlasBoundsDto bounds)=>
        $"{bounds.Dimension}; request {bounds.RequestedLimit}; effective {bounds.EffectiveLimit}; rows {bounds.ReturnedRows}; "
        +$"content bytes {bounds.ReturnedContentBytes}; total {bounds.DenominatorState} {bounds.DenominatorValue} "
        +$"({bounds.DenominatorReason}); omission {bounds.OmissionDimension} {bounds.OmissionReason}.";

    [Theory]
    [InlineData("cancel")] [InlineData("dispose")] [InlineData("replace")] [InlineData("duplicate")]
    [InlineData("incomplete")] [InlineData("unload")] [InlineData("missing")] [InlineData("extra-loaded")]
    public async Task TransitionControl_Publication_InvalidOrLateSignalsNeverProduceReadiness(string phase)
    {
        await RunDispatcherAsync(async () =>
        {
            await using var owner=new AtlasWorkspaceOwner();var port=new ObserverPort();
            await owner.AttachAsync(()=>port);var host=new AtlasLoadingHost(owner);
            var page=await port.InventoryAsync(new(1,"control",7,"control",0,64),default);
            using var publication=new TransitionPublication(host,()=>phase=="missing"?null:page,()=>true);
            publication.FinalNotificationForControl=()=>
            {
                publication.FinalNotificationForControl=null;
                var view=publication.View!;
                switch(phase)
                {
                    case "cancel":publication.Cancel();break;
                    case "dispose":publication.Dispose();break;
                    case "replace":host.Content=new Border();break;
                    case "duplicate":view.StatusControl.Text="early";view.StatusControl.Text=TransitionStatus(page);break;
                    case "incomplete":host.Dispatcher.BeginInvoke(DispatcherPriority.Send,new Action(()=>view.LoadMoreButton.IsEnabled=true));break;
                    case "unload":view.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));break;
                    case "extra-loaded":view.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));view.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));break;
                }
            };
            await host.ActivateAsync();
            await IdleAsync();
            if(phase=="missing"){Assert.False(publication.Ready.IsCompleted);Assert.Equal(0,publication.TailCount);publication.Dispose();}
            Assert.NotNull(await Record.ExceptionAsync(()=>publication.Ready));
            Assert.InRange(publication.TailCount,0,1);
            var tails=publication.TailCount;
            publication.Dispose();
            host.Content=new Border();await IdleAsync();
            Assert.Equal(tails,publication.TailCount);
            host.Deactivate();
        },ObserverReceipt());
    }

    [Fact]
    public async Task TransitionControl_Publication_UnshownObjectsCannotEstablishShownPredicate()
    {
        await RunDispatcherAsync(async()=>
        {
            await using var owner=new AtlasWorkspaceOwner();var port=new ObserverPort();
            await owner.AttachAsync(()=>port);var host=new AtlasLoadingHost(owner);
            var page=await port.InventoryAsync(new(1,"control",7,"control",0,64),default);
            using var publication=new TransitionPublication(host,()=>page,()=>host.IsLoaded&&host.IsVisible);
            await host.ActivateAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(()=>publication.Ready);
            Assert.False(host.IsLoaded);Assert.Equal(1,publication.TailCount);
            publication.Dispose();host.Deactivate();
        },ObserverReceipt());
    }

    [Theory]
    [InlineData("before-acquire")] [InlineData("held")] [InlineData("after-handoff")]
    public async Task TransitionControl_Escrow_CancellationHasOneCustodian(string phase)
    {
        var lease=new TransitionControlLease();var acquired=new TaskCompletionSource<IAtlasReaderLease>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader=new TransitionControlReader(lease){Acquisition=phase=="before-acquire"?acquired.Task:null};
        var escrow=new TransitionEscrow(reader);using var cancel=new CancellationTokenSource();
        var admission=escrow.AdmitAsync(cancel.Token).AsTask();
        if(phase=="before-acquire"){cancel.Cancel();acquired.SetResult(lease);}
        await escrow.Held;
        if(phase=="after-handoff")
        {
            escrow.Release();Assert.Same(lease,await admission);cancel.Cancel();
            Assert.Null(escrow.OwnedCandidate);Assert.Equal(0,lease.Disposals);
            await lease.DisposeAsync();
        }
        else {cancel.Cancel();escrow.Release();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>admission);}
        await escrow.DisposeAsync();
        Assert.Equal(1,lease.Disposals);Assert.Equal(1,reader.Disposals);
        Assert.Equal(phase=="after-handoff"?1:0,escrow.Handoffs);
    }

    [Fact]
    public async Task TransitionControl_Escrow_ConcurrentCloseSharesDisposalTask()
    {
        var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lease=new TransitionControlLease{DisposalGate=release.Task};var reader=new TransitionControlReader(lease);
        var escrow=new TransitionEscrow(reader);using var cancel=new CancellationTokenSource();
        var admission=escrow.AdmitAsync(cancel.Token).AsTask();await escrow.Held;
        cancel.Cancel();var first=escrow.DisposeAsync().AsTask();var second=escrow.DisposeAsync().AsTask();
        await lease.DisposalStarted.Task;
        Assert.Same(first,second);Assert.Equal(1,lease.Disposals);Assert.False(first.IsCompleted);
        release.SetResult();await first;await second;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>admission);
        Assert.Equal(1,lease.Disposals);Assert.Equal(1,reader.Disposals);
    }

    [Fact]
    public async Task TransitionControl_Escrow_PostHandoffFailureStaysWithActualOwner()
    {
        await RunDispatcherAsync(async()=>
        {
            var owner=new AtlasWorkspaceOwner();var lease=new TransitionControlLease{FailDisposal=true};
            var reader=new TransitionControlReader(lease);var escrow=new TransitionEscrow(reader);
            await owner.AttachAsync(()=>escrow);
            var admission=owner.AdmitAsync(default);await escrow.Held;escrow.Release();
            Assert.Same(lease,await admission);Assert.Null(escrow.OwnedCandidate);
            await Assert.ThrowsAsync<InvalidOperationException>(()=>owner.DisposeAsync().AsTask());
            Assert.Equal(1,lease.Disposals);Assert.Equal(0,reader.Disposals);
            lease.FailDisposal=false;await owner.DisposeAsync();
            Assert.Equal(2,lease.Disposals);Assert.Equal(1,reader.Disposals);Assert.Equal(1,escrow.Handoffs);
        },ObserverReceipt());
    }

    [Fact]
    public void TransitionControl_Runtime_LoadedAssembliesHaveRecordedIdentity()
    {
        var identities=TransitionRuntimeIdentity();
        Assert.All(identities,item=>Assert.True(File.Exists(item.Path)));
        var directory=Path.Combine(Tree,"artifacts/atlas-transition-preparation");Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory,"runtime.json"),JsonSerializer.Serialize(new
        {Runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,Environment.Version,Assemblies=identities},new JsonSerializerOptions{WriteIndented=true}));
        Assert.Contains(identities,item=>item.Name=="PresentationFramework");
        Assert.Contains(identities,item=>item.Name=="WindowsBase");
    }
    private sealed record TransitionRuntime(string Name,string? Version,string Path,string Sha256,Guid ModuleId);
    private static TransitionRuntime[] TransitionRuntimeIdentity()=>new[]
    {typeof(object).Assembly,typeof(Window).Assembly,typeof(Dispatcher).Assembly,typeof(AtlasDaemonMainWindowProofTests).Assembly,
        typeof(MainWindowViewModel).Assembly,typeof(IAtlasWorkspaceReader).Assembly}.Distinct().Select(assembly=>new TransitionRuntime(
            assembly.GetName().Name!,assembly.GetName().Version?.ToString(),assembly.Location,Hash(File.ReadAllBytes(assembly.Location)),assembly.ManifestModule.ModuleVersionId)).ToArray();

    [Fact]
    public void TransitionControl_LoadedRegistry_IsScopedAndInertAfterDisposal()
    {
        Sta.Run(()=>
        {
            var host=new AtlasLoadingHost(null);var window=new Window{Content=host};
            var called=0;var instanceSawClass=false;
            host.Loaded+=(_,_)=>instanceSawClass=called==1;
            var scope=new TransitionLoadedScope(window,_=>called++);
            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Assert.Equal(1,called);Assert.True(instanceSawClass);Assert.Equal(1,scope.Count);
            scope.Dispose();host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Assert.Equal(1,called);Assert.Equal(0,TransitionLoadedScope.ActiveScopes);
            window.Close();
        });
    }

    private sealed class TransitionLoadedScope : IDisposable
    {
        private static readonly object Gate=new();
        private static readonly Dictionary<Window,TransitionLoadedScope> Scopes=[];
        private Window? _window;
        private Action<AtlasLoadingHost>? _observe;
        internal int Count {get;private set;}
        internal static int ActiveScopes {get{lock(Gate)return Scopes.Count;}}
        static TransitionLoadedScope()=>EventManager.RegisterClassHandler(typeof(AtlasLoadingHost),
            FrameworkElement.LoadedEvent,new RoutedEventHandler(OnLoaded));
        internal TransitionLoadedScope(Window window,Action<AtlasLoadingHost> observe)
        {_window=window;_observe=observe;lock(Gate)Scopes.Add(window,this);}
        private static void OnLoaded(object sender,RoutedEventArgs args)
        {
            if(sender is not AtlasLoadingHost host || Window.GetWindow(host) is not { } window)return;
            TransitionLoadedScope? scope;lock(Gate)Scopes.TryGetValue(window,out scope);
            if(scope?._observe is not { } observe)return;
            scope.Count++;observe(host);
        }
        public void Dispose()
        {lock(Gate){if(_window is not null)Scopes.Remove(_window);_window=null;_observe=null;}}
    }

    [Fact]
    public async Task TransitionControl_Escrow_CancelAfterHandoffBeforeOwnerContinuationRetainsFailedRelease()
    {
        await RunDispatcherAsync(async()=>
        {
            var owner=new AtlasWorkspaceOwner();var lease=new TransitionControlLease{FailDisposal=true};
            var reader=new TransitionControlReader(lease);var escrow=new TransitionEscrow(reader);
            await owner.AttachAsync(()=>escrow);using var cancel=new CancellationTokenSource();
            var queued=new TransitionControlContext();var prior=SynchronizationContext.Current;
            Task<IAtlasReaderLease> admission;
            try{SynchronizationContext.SetSynchronizationContext(queued);admission=owner.AdmitAsync(cancel.Token);}
            finally{SynchronizationContext.SetSynchronizationContext(prior);}
            await escrow.Held;escrow.Release();
            await queued.RunOneAsync(); // Escrow release continuation; owner continuation remains queued.
            Assert.Equal(1,escrow.Handoffs);Assert.False(admission.IsCompleted);Assert.Equal(0,lease.Disposals);
            cancel.Cancel();
            for(var remaining=8;!admission.IsCompleted;remaining--)
            {Assert.True(remaining>0,"Unexpected continuation graph.");await queued.RunOneAsync();}
            await Assert.ThrowsAsync<InvalidOperationException>(()=>admission);
            Assert.Equal(1,lease.Disposals);Assert.Equal(0,reader.Disposals);Assert.Null(escrow.OwnedCandidate);
            lease.FailDisposal=false;await owner.DisposeAsync();
            Assert.Equal(2,lease.Disposals);Assert.Equal(1,reader.Disposals);
        },ObserverReceipt());
    }
    private sealed class TransitionControlContext:SynchronizationContext
    {
        private readonly System.Threading.Channels.Channel<(SendOrPostCallback Callback,object? State)> _queue=
            System.Threading.Channels.Channel.CreateUnbounded<(SendOrPostCallback,object?)>();
        public override void Post(SendOrPostCallback callback,object? state)=>_queue.Writer.TryWrite((callback,state));
        internal async Task RunOneAsync(){var item=await _queue.Reader.ReadAsync();item.Callback(item.State);}
    }

    [Fact]
    public async Task TransitionControl_Publication_PartialCollectionAndLoadingAreNotReady()
    {
        await RunDispatcherAsync(async()=>
        {
            var empty=await new ObserverPort().InventoryAsync(new(1,"control",7,"control",0,64),default);
            var file=new AtlasFileDto("one",AtlasDirectoryEntryKind.File,null,"one.cs",AtlasFileClassification.CSharp,
                AtlasFileAvailability.Available,new(AtlasDenominatorState.Known,0,null),null);
            var page=empty with {Files=[file,file with {FileToken="two",RelativePath="two.cs"}]};
            var lease=new TransitionControlLease{Queries=new TransitionControlQueries(page)};
            await using var owner=new AtlasWorkspaceOwner();await owner.AttachAsync(()=>new TransitionControlReader(lease));
            var host=new AtlasLoadingHost(owner);
            using var publication=new TransitionPublication(host,()=>page,()=>true);
            var partial=0;var notReady=0;
            var descriptor=System.ComponentModel.DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty,host.GetType())!;
            System.Collections.Specialized.INotifyCollectionChanged? roots=null;
            System.Collections.Specialized.NotifyCollectionChangedEventHandler changed=(_,_)=>
            {partial++;if(!publication.Ready.IsCompleted)notReady++;};
            EventHandler content=(_,_)=>
            {
                if(host.Content is not AtlasReaderView view)return;
                roots=(System.Collections.Specialized.INotifyCollectionChanged)view.FileRoots;
                roots.CollectionChanged+=changed;
                view.StatusControl.Text="Loading control publication.";
                Assert.False(publication.Ready.IsCompleted);
            };
            descriptor.AddValueChanged(host,content);
            try
            {
                await host.ActivateAsync();await publication.Ready;
                Assert.True(partial>=2);Assert.Equal(partial,notReady);Assert.Equal(1,publication.TailCount);
            }
            finally
            {
                descriptor.RemoveValueChanged(host,content);
                if(roots is not null)roots.CollectionChanged-=changed;
                publication.Dispose();host.Deactivate();
            }
        },ObserverReceipt());
    }
    private sealed class TransitionControlQueries(AtlasInventoryPageDto page):IAtlasReaderQueries
    {
        public ValueTask<AtlasInventoryPageDto> InventoryAsync(AtlasInventoryRequestDto request,CancellationToken token)=>ValueTask.FromResult(page);
        public ValueTask<AtlasSelectionDto> SelectAsync(AtlasSelectRequestDto request,CancellationToken token)=>throw new NotSupportedException();
        public ValueTask<AtlasSelectionDto> RestoreAsync(AtlasRestoreRequestDto request,CancellationToken token)=>throw new NotSupportedException();
    }

    private sealed class TransitionCapturingReader(IAtlasWorkspaceReader inner,Action<AtlasInventoryPageDto> capture):IAtlasWorkspaceReader
    {
        public async ValueTask<IAtlasReaderLease> AdmitAsync(CancellationToken token)=>new TransitionCapturingLease(await inner.AdmitAsync(token),capture);
        public ValueTask DisposeAsync()=>inner.DisposeAsync();
    }
    private sealed class TransitionCapturingLease(IAtlasReaderLease inner,Action<AtlasInventoryPageDto> capture):IAtlasReaderLease,IAtlasReaderQueries
    {
        public string ScopeToken=>inner.ScopeToken;public string InitialManifestToken=>inner.InitialManifestToken;
        public long CoreEpoch=>inner.CoreEpoch;public DateTimeOffset ExpiresAt=>inner.ExpiresAt;
        public CancellationToken Invalidated=>inner.Invalidated;public bool IsTerminal=>inner.IsTerminal;
        public IAtlasReaderQueries Queries=>this;
        public async ValueTask<AtlasInventoryPageDto> InventoryAsync(AtlasInventoryRequestDto request,CancellationToken token)
        {var page=await inner.Queries.InventoryAsync(request,token);capture(page);return page;}
        public ValueTask<AtlasSelectionDto> SelectAsync(AtlasSelectRequestDto request,CancellationToken token)=>inner.Queries.SelectAsync(request,token);
        public ValueTask<AtlasSelectionDto> RestoreAsync(AtlasRestoreRequestDto request,CancellationToken token)=>inner.Queries.RestoreAsync(request,token);
        public ValueTask DisposeAsync()=>inner.DisposeAsync();
    }

    // Experiment-only: each Fact requires its own fresh process and unique ATLAS_PROOF_RUN.
    // These Facts are outside the preparation control filter and are not canonical-suite candidates.
    [Fact]
    public Task NativeTransition_A_NoLoadingTraversal()=>RunTransitionArmAsync(false);
    [Fact]
    public Task NativeTransition_B_LoadingTraversal()=>RunTransitionArmAsync(true);

    private static async Task RunTransitionArmAsync(bool loadingTraversal)
    {
        var run=Environment.GetEnvironmentVariable("ATLAS_PROOF_RUN")??throw new InvalidOperationException("ATLAS_PROOF_RUN required.");
        Assert.NotEmpty(run);Assert.True(run.All(c=>char.IsAsciiLetterOrDigit(c)||c=='-'));
        var evidence=Path.Combine(Tree,"artifacts","atlas-uia-transition",run);
        Assert.False(Directory.Exists(evidence));Directory.CreateDirectory(evidence);
        var receipt=new Receipt(evidence);Daemon? daemon=null;WorkspaceClient? client=null;
        var owned=Path.Combine(evidence,"owned");var repository=Path.Combine(owned,"repository");
        try
        {
            var configuration=new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            var binary=Path.Combine(Tree,"src","AiDe.Daemon","bin",configuration,"net10.0-windows","AiDe.Daemon.exe");
            Assert.True(File.Exists(binary));
            var gitRoot=Path.GetFullPath((await CommandAsync(Git,Tree,"rev-parse","--show-toplevel")).Trim());
            Assert.Equal(Tree,gitRoot,ignoreCase:true);
            receipt.Mark("transition.identity",new {Arm=loadingTraversal?"B":"A",Run=run,ProcessId=Environment.ProcessId,
                RepositoryRoot=Tree,SourceCommit=(await CommandAsync(Git,Tree,"rev-parse","HEAD")).Trim(),
                Runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,Assemblies=TransitionRuntimeIdentity(),
                Binary=binary,BinarySha256=Hash(File.ReadAllBytes(binary)),UiaWpfOwner="not-recorded",
                Claim="treatment association only",Activation="natural Loaded; no explicit ActivateAsync"});
            Directory.CreateDirectory(Path.Combine(repository,"src"));
            File.WriteAllText(Path.Combine(repository,"src","Widget.cs"),Source,new UTF8Encoding(false));
            await GitAsync(repository,"init","--quiet");await GitAsync(repository,"add","--","src/Widget.cs");
            await GitAsync(repository,"commit","--quiet","-m","owned synthetic source");
            receipt.Mark("transition.input",new {Sha256=Hash(File.ReadAllBytes(Path.Combine(repository,"src","Widget.cs"))),RelativePath="src/Widget.cs"});
            daemon=Daemon.Start(binary,repository,receipt,"transition");
            client=await WorkspaceClient.ConnectAsync(IpcPipeName.ForWorkspace(repository),TimeSpan.FromSeconds(10),CancellationToken.None);
            var connected=client;
            await RunDispatcherAsync(async()=>
            {
                TransitionEscrow? escrow=null;AtlasInventoryPageDto? page=null;var replies=0;
                TransitionPublication? publication=null;AtlasLoadingHost? host=null;
                var observed=new TaskCompletionSource<AtlasLoadingHost>(TaskCreationOptions.RunContinuationsAsynchronously);
                using var observer=new InstanceObserver(receipt);
                var vm=new MainWindowViewModel(connected,"transition-owned",null,commands:connected,atlasReaderFactory:()=>
                    escrow=new TransitionEscrow(new ObservedReader(new TransitionCapturingReader(connected.CreateAtlasReader(),reply=>
                    {if(++replies!=1)throw new InvalidOperationException("TRANSITION-EXTRA-INVENTORY");page=reply;}),receipt,"transition")));
                await vm.RefreshAsync(CancellationToken.None);
                var window=new AiDe.App.MainWindow(()=>Task.FromResult(vm),Path.Combine(owned,"shell-state"),Resources())
                {Width=1280,Height=900,Left=40,Top=40,WindowStartupLocation=WindowStartupLocation.Manual,ShowInTaskbar=false};
                TransitionLoadedScope? scope=null;
                scope=new TransitionLoadedScope(window,loaded=>
                {
                    receipt.Mark("transition.host-loaded",new {Count=scope!.Count,Tick=Stopwatch.GetTimestamp(),ThreadId=Environment.CurrentManagedThreadId});
                    if(host is not null){publication?.Cancel();return;}
                    host=loaded;
                    publication=new TransitionPublication(loaded,()=>page,()=>scope.Count==1&&escrow?.Admissions==1&&escrow.Handoffs==1
                        &&loaded.IsLoaded&&loaded.IsVisible&&Window.GetWindow(loaded)==window
                        &&publication?.LoadedEvents==1&&publication.View is {IsLoaded:true,IsVisible:true} view&&Window.GetWindow(view)==window,receipt);
                    observed.TrySetResult(loaded);
                });
                try
                {
                    window.Shell.Execute(PerspectiveSet.Architecture.CommandId);
                    var menu=Assert.IsType<Menu>(window.FindName("MainMenu"));
                    var title=PerspectiveMenu.Opener(SurfaceContentFactory.Kinds.Single(kind=>kind.Kind=="code-atlas")).Title;
                    Assert.Single(MenuItems(menu),item=>string.Equals(item.Header?.ToString(),title,StringComparison.Ordinal))
                        .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                    window.Show();await window.WorkspaceReady;
                    var current=await observed.Task;
                    Assert.NotNull(escrow);await escrow.Held;
                    Assert.Equal(1,scope.Count);Assert.Null(current.ReaderView);
                    var loading=Assert.IsType<TextBlock>(current.Content);Assert.Equal("Loading Code Atlas.",loading.Text);
                    Assert.True(current.IsLoaded&&current.IsVisible);Assert.Same(window,Window.GetWindow(current));
                    var hwnd=new WindowInteropHelper(window).Handle;
                    receipt.Mark("transition.held",new {OwnHwnd=hwnd.ToInt64(),scope.Count,escrow.Admissions,escrow.Handoffs,
                        ContentType=current.Content.GetType().Name,Text=loading.Text,Tick=Stopwatch.GetTimestamp()});
                    if(loadingTraversal)await TransitionCensusAsync(hwnd,receipt,"loading-treatment");
                    Assert.Same(loading,current.Content);Assert.Null(current.ReaderView);
                    receipt.Mark("transition.release",new {Tick=Stopwatch.GetTimestamp(),LoadingTraversal=loadingTraversal});
                    escrow.Release();await publication!.Ready;await IdleAsync();publication.AssertReady();
                    observer.Retain(current,publication.View!);
                    receipt.Mark("transition.pre-oracle",new {scope.Count,escrow.Admissions,escrow.Handoffs,publication.Replacements,
                        publication.LoadedEvents,publication.TailCount,Replies=replies,UiaWpfOwner="not-recorded"});
                    try{await ObserveWithInstancesAsync(window,hwnd,receipt,observer);}
                    finally
                    {
                        try{await TransitionCensusAsync(hwnd,receipt,"after-original-oracle");}
                        catch(Exception error){receipt.Failure("transition.post-oracle-observer",error);}
                    }
                    publication.AssertReady();
                }
                finally
                {
                    await TransitionCleanupSequenceAsync(receipt,
                        ("transition.observer-cleanup",()=>{publication?.Dispose();scope.Dispose();escrow?.Cancel();return Task.CompletedTask;}),
                        ("transition.window-cleanup",async()=>{window.Close();await window.CloseOperation;await IdleAsync();}),
                        ("transition.cleanup-record",()=>{receipt.Mark("transition.cleanup",new {RegistryCount=TransitionLoadedScope.ActiveScopes,
                            Custody=escrow?.Transitions,Handoffs=escrow?.Handoffs,Tick=Stopwatch.GetTimestamp()});return Task.CompletedTask;}));
                }
            },receipt);
            await client.DisposeAsync();client=null;
            await daemon.WaitForNormalExitAsync();receipt.Completed=true;
        }
        catch(Exception error){receipt.Failure("transition.primary",error);throw;}
        finally
        {
            await TransitionCleanupSequenceAsync(receipt,
                ("transition.client-cleanup",async()=>{if(client is not null)await client.DisposeAsync();}),
                ("transition.daemon-cleanup",async()=>{if(daemon is not null)await daemon.CleanupAsync();}));
            receipt.Save();
        }
        Assert.Equal(0,receipt.FailureCount);
    }

    private static async Task TransitionCleanupSequenceAsync(Receipt receipt,params (string Stage,Func<Task> Cleanup)[] actions)
    {
        foreach(var action in actions)
        {
            try{await action.Cleanup();}
            catch(Exception error){receipt.Failure(action.Stage,error);}
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TransitionControl_Cleanup_RecordsFailuresContinuesAndPreservesPrimary(bool originalFails)
    {
        var receipt=ObserverReceipt();var primary=new InvalidOperationException("original-query-control");
        var calls=new List<string>();
        var actual=await Record.ExceptionAsync(async()=>
        {
            try{if(originalFails)throw primary;}
            finally
            {
                await TransitionCleanupSequenceAsync(receipt,
                    ("control.window",()=>{calls.Add("window");throw new InvalidOperationException("window-close-control");}),
                    ("control.daemon",()=>{calls.Add("daemon");throw new InvalidOperationException("daemon-cleanup-control");}),
                    ("control.tail",()=>{calls.Add("tail");return Task.CompletedTask;}));
                receipt.Save();
            }
        });
        if(originalFails)Assert.Same(primary,actual);else Assert.Null(actual);
        Assert.Equal(new[]{"window","daemon","tail"},calls);Assert.Equal(2,receipt.FailureCount);
        using var saved=JsonDocument.Parse(File.ReadAllText(Path.Combine(receipt.DirectoryPath,"receipt.json")));
        Assert.False(saved.RootElement.GetProperty("Completed").GetBoolean());
        Assert.Equal(2,saved.RootElement.GetProperty("FailureCount").GetInt32());
    }

    private static Task TransitionCensusAsync(nint hwnd,Receipt receipt,string phase)=>Task.Run(()=>
    {
        var started=Stopwatch.GetTimestamp();
        var root=AutomationElement.FromHandle(hwnd);Assert.Equal(Environment.ProcessId,root.Current.ProcessId);
        Assert.Equal(hwnd.ToInt64(),(long)root.Current.NativeWindowHandle);
        var pending=new Queue<(AutomationElement Element,int Parent,int Depth)>();pending.Enqueue((root,-1,0));
        var nodes=new List<object>();var truncated=false;
        while(pending.Count>0)
        {
            if(nodes.Count>=128||Stopwatch.GetElapsedTime(started).TotalMilliseconds>=250){truncated=true;break;}
            var (element,parent,depth)=pending.Dequeue();var ordinal=nodes.Count;
            var current=element.Current;
            nodes.Add(new {Ordinal=ordinal,Parent=parent,Depth=depth,current.Name,current.AutomationId,
                ControlType=current.ControlType.ProgrammaticName,current.ProcessId,current.NativeWindowHandle,
                RuntimeId=element.GetRuntimeId(),UiaWpfOwner="not-recorded"});
            if(depth>=12){truncated=true;continue;}
            for(var child=TreeWalker.RawViewWalker.GetFirstChild(element);child is not null;child=TreeWalker.RawViewWalker.GetNextSibling(child))
            {
                if(nodes.Count+pending.Count>=128||Stopwatch.GetElapsedTime(started).TotalMilliseconds>=250){truncated=true;break;}
                pending.Enqueue((child,ordinal,depth+1));
            }
        }
        receipt.Mark("transition.uia-census",new {Phase=phase,OwnHwnd=hwnd.ToInt64(),ExpectedProcessId=Environment.ProcessId,
            StartTick=started,EndTick=Stopwatch.GetTimestamp(),Apartment=Thread.CurrentThread.GetApartmentState().ToString(),
            MaxNodes=128,MaxDepth=12,SoftBudgetMilliseconds=250,Truncated=truncated,Nodes=nodes,UiaWpfOwner="not-recorded"});
    });

    private sealed class TransitionEscrow(IAtlasWorkspaceReader inner) : IAtlasWorkspaceReader
    {
        private readonly TaskCompletionSource _held=new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release=new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _gate=new();
        private Task<IAtlasReaderLease>? _admission;
        private Task? _candidateDisposal;
        private Task? _readerDisposal;
        private IAtlasReaderLease? _candidate;
        private bool _cancel;
        private bool _handedOff;
        private bool _innerDisposed;
        private readonly List<string> _transitions=[];
        internal string[] Transitions {get{lock(_gate)return _transitions.ToArray();}}
        internal Task Held=>_held.Task;
        internal IAtlasReaderLease? OwnedCandidate {get{lock(_gate)return _handedOff?null:_candidate;}}
        internal int Handoffs { get; private set; }
        internal int Admissions { get; private set; }
        public ValueTask<IAtlasReaderLease> AdmitAsync(CancellationToken token)
        {
            TaskCompletionSource<IAtlasReaderLease> completion;
            lock(_gate)
            {
                if(_admission is not null)throw new InvalidOperationException("TRANSITION-EXTRA-ADMISSION");
                Admissions++;_transitions.Add("acquiring");
                completion=new(TaskCreationOptions.RunContinuationsAsynchronously);_admission=completion.Task;
            }
            _=CompleteAdmissionAsync(token,completion);return new(completion.Task);
        }
        private async Task CompleteAdmissionAsync(CancellationToken token,TaskCompletionSource<IAtlasReaderLease> completion)
        {
            try
            {
                using var registration=token.Register(Cancel);
                var candidate=await inner.AdmitAsync(token);
                lock(_gate){_candidate=candidate;_transitions.Add("held");}
                _held.TrySetResult();
                await _release.Task;
                lock(_gate)
                {
                    if(!_cancel && !token.IsCancellationRequested)
                    {
                        _handedOff=true;Handoffs++;_transitions.Add("handoff");
                        completion.TrySetResult(candidate);return;
                    }
                }
                await DisposeCandidateAsync(false);
                completion.TrySetCanceled(token.IsCancellationRequested?token:new CancellationToken(true));
            }
            catch(Exception error){_held.TrySetException(error);completion.TrySetException(error);}
        }
        internal void Release(){lock(_gate)_transitions.Add("release-request");_release.TrySetResult();}
        internal void Cancel(){lock(_gate){_cancel=true;_transitions.Add("cancel-request");}_release.TrySetResult();}
        private Task DisposeCandidateAsync(bool retry)
        {
            TaskCompletionSource completion;IAtlasReaderLease candidate;
            lock(_gate)
            {
                if(_handedOff || _candidate is null)return Task.CompletedTask;
                if(_candidateDisposal is not null && (!_candidateDisposal.IsCompleted || !retry))return _candidateDisposal;
                candidate=_candidate;completion=new(TaskCreationOptions.RunContinuationsAsynchronously);
                _candidateDisposal=completion.Task;_transitions.Add("disposing");
            }
            _=DisposeCandidateCoreAsync(candidate,completion);return completion.Task;
        }
        private async Task DisposeCandidateCoreAsync(IAtlasReaderLease candidate,TaskCompletionSource completion)
        {
            try
            {
                await candidate.DisposeAsync();
                lock(_gate){_candidate=null;_transitions.Add("candidate-disposed");}
                completion.TrySetResult();
            }
            catch(Exception error){lock(_gate)_transitions.Add("dispose-failed-retained");completion.TrySetException(error);}
        }
        public ValueTask DisposeAsync()
        {
            TaskCompletionSource completion;
            lock(_gate)
            {
                if(_innerDisposed)return ValueTask.CompletedTask;
                if(_readerDisposal is not null && !_readerDisposal.IsCompleted)return new(_readerDisposal);
                completion=new(TaskCreationOptions.RunContinuationsAsynchronously);_readerDisposal=completion.Task;
            }
            _=DisposeReaderCoreAsync(completion);return new(completion.Task);
        }
        private async Task DisposeReaderCoreAsync(TaskCompletionSource completion)
        {
            try
            {
                Cancel();
                if(_admission is not null)try{await _admission;}catch(Exception){/* Admission failure remains with its caller. */}
                await DisposeCandidateAsync(true);
                await inner.DisposeAsync();
                lock(_gate){_innerDisposed=true;_transitions.Add("reader-disposed");}
                completion.TrySetResult();
            }
            catch(Exception error){completion.TrySetException(error);}
        }
    }
    private sealed class TransitionControlReader(IAtlasReaderLease lease):IAtlasWorkspaceReader
    {
        internal int Disposals { get; private set; }
        internal Task<IAtlasReaderLease>? Acquisition {get;init;}
        public ValueTask<IAtlasReaderLease> AdmitAsync(CancellationToken token)=>Acquisition is null?ValueTask.FromResult(lease):new(Acquisition);
        public ValueTask DisposeAsync(){Disposals++;return ValueTask.CompletedTask;}
    }
    private sealed class TransitionControlLease:IAtlasReaderLease
    {
        internal bool FailDisposal { get; set; }
        internal int Disposals { get; private set; }
        internal Task? DisposalGate {get;init;}
        internal TaskCompletionSource DisposalStarted {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string ScopeToken=>"control";public string InitialManifestToken=>"control";
        public long CoreEpoch=>7;public DateTimeOffset ExpiresAt=>DateTimeOffset.MaxValue;
        public IAtlasReaderQueries Queries {get;init;}=new ObserverPort();
        public CancellationToken Invalidated=>CancellationToken.None;
        public bool IsTerminal { get; private set; }
        public async ValueTask DisposeAsync()
        {
            Disposals++;
            DisposalStarted.TrySetResult();
            if(DisposalGate is not null)await DisposalGate;
            if(FailDisposal)throw new InvalidOperationException("transition-control-disposal");
            IsTerminal=true;
        }
    }


    [Fact]
    public void NativeObserver_NonGui_IdentityIsReferenceBasedAndBounded()
    {
        var receipt=ObserverReceipt();
        using var observer=new InstanceObserver(receipt,maxReferences:2);
        var first=new EqualObservationObject(); var second=new EqualObservationObject();
        var id=observer.Identity(first);
        Assert.NotNull(id);
        Assert.Equal(id,observer.Identity(first));
        Assert.NotEqual(id,observer.Identity(second));
        Assert.Null(observer.Identity(new EqualObservationObject()));
        Assert.Equal(id,observer.Identity(first));
    }

    [Fact]
    public async Task NativeObserver_NonGui_ActualAdapterDistinguishesHostsChildrenAndRetainedViews()
    {
        var receipt=ObserverReceipt();
        int? retainedViewId=null;
        await RunDispatcherAsync(async () =>
        {
            using var observer=new InstanceObserver(receipt);
            await using var owner=new AtlasWorkspaceOwner();
            var port=new ObserverPort();
            var reader=new ObservedReader(port,receipt,"observer-control");
            await owner.AttachAsync(()=>reader);
            var first=new AtlasLoadingHost(owner); var second=new AtlasLoadingHost(null);
            var panel=new StackPanel(); panel.Children.Add(first); panel.Children.Add(second);
            var window=new ObserverControlWindow(panel);
            Assert.Equal(nint.Zero,new WindowInteropHelper(window).Handle);
            await first.ActivateAsync();
            var old=Assert.IsType<AtlasReaderView>(first.ReaderView);
            observer.Retain(first,second,old,reader);
            observer.Sample(window,"before",1);
            var before=Assert.Single(observer.Packets);
            Assert.Equal(2,before.Hosts.Length);
            using(var metadata=JsonDocument.Parse(JsonSerializer.Serialize(before)))
            {
                Assert.Equal(JsonValueKind.False,metadata.RootElement.GetProperty("WindowLoaded").ValueKind);
                Assert.Contains(metadata.RootElement.GetProperty("Hosts").EnumerateArray(),row=>row.GetProperty("ContentKind").GetString()=="AtlasReaderView");
            }
            var row=Assert.Single(before.Hosts,h=>h.Id==observer.Identity(first));
            Assert.Equal(observer.Identity(old),row.ContentId);
            Assert.Equal(row.ContentId,row.ReaderViewId);
            Assert.True(row.ContentIsReaderView);
            Assert.Equal("owned-window",row.Attachment);
            var view=Assert.Single(before.Views,v=>v.Id==observer.Identity(old));
            retainedViewId=view.Id;
            Assert.Equal(observer.Identity(old.FilesControl),view.FilesControlId);
            Assert.Equal(observer.Identity(old.SourceControl.Document),view.DocumentId);
            Assert.Equal(0,view.FileRoots);
            var observedReader=Assert.Single(before.Readers);
            Assert.Equal(observer.Identity(port),observedReader.InnerReaderId);
            Assert.Equal(observer.Identity(reader.Lease),observedReader.LeaseId);
            Assert.Equal("not-observed",view.BackingLeaseRelation);
            var replacementChild=new Border();first.Content=replacementChild;
            observer.Sample(window,"child-replaced",1);
            var changed=Assert.Single(observer.Packets.Last().Hosts,h=>h.Id==row.Id);
            Assert.Equal(observer.Identity(replacementChild),changed.ContentId);
            Assert.Equal(observer.Identity(old),changed.ReaderViewId);
            Assert.False(changed.ContentIsReaderView);
            first.Deactivate(); panel.Children.Remove(first);
            observer.Sample(window,"after",1);
            var after=observer.Packets.Last();
            var detached=Assert.Single(after.Hosts,h=>h.Id==row.Id);
            Assert.Equal("detached",detached.Attachment);
            Assert.False(detached.CensusMember);
            Assert.Null(detached.ReaderViewId);
            Assert.NotEqual(row.ContentId,detached.ContentId);
            Assert.Equal(observer.Identity(first.Content),detached.ContentId);
            Assert.Equal("TextBlock",detached.ContentKind);
            Assert.Contains(after.Views,v=>v.Id==view.Id && v.TestRetained);
            observer.Flush();
            Assert.Equal(nint.Zero,new WindowInteropHelper(window).Handle);
            window.Close();
        },receipt);
        var json=File.ReadAllText(receipt.DirectoryPath+"/receipt.json");
        Assert.DoesNotContain("DO-NOT-EMIT",json,StringComparison.Ordinal);
        using(var saved=JsonDocument.Parse(json))
        {
            var packet=Assert.Single(saved.RootElement.GetProperty("Events").EnumerateArray(),item=>
                item.GetProperty("Stage").GetString()=="observer.wpf" && item.GetProperty("Attributes").GetProperty("Boundary").GetString()=="before");
            var savedView=Assert.Single(packet.GetProperty("Attributes").GetProperty("Views").EnumerateArray(),item=>
                item.GetProperty("Id").GetInt32()==retainedViewId);
            Assert.False(savedView.GetProperty("IsLoaded").GetBoolean());
            Assert.False(savedView.GetProperty("IsVisible").GetBoolean());
        }
        Assert.Equal(0,receipt.FailureCount);
    }

    [Theory]
    [InlineData("nodes")] [InlineData("depth")] [InlineData("references")] [InlineData("packets")]
    public void NativeObserver_NonGui_ActualAdapterReportsBounds(string limit)
    {
        Sta.Run(()=>
        {
            var receipt=ObserverReceipt();
            using var observer=new InstanceObserver(receipt,maxNodes:limit=="nodes"?1:512,
                maxDepth:limit=="depth"?0:48,maxReferences:limit=="references"?1:256,maxPackets:limit=="packets"?1:16);
            var panel=new StackPanel();panel.Children.Add(new AtlasLoadingHost(null));
            var window=new ObserverControlWindow(panel);
            observer.Sample(window,"first",1);
            if(limit=="packets")observer.Sample(window,"second",1);
            Assert.True(observer.Truncated);
            Assert.NotEmpty(observer.Packets);
            Assert.InRange(observer.Packets.Count,1,16);
            observer.Flush();
            Assert.Equal(nint.Zero,new WindowInteropHelper(window).Handle);
            window.Close();
        });
    }

    [Fact]
    public void NativeObserver_NonGui_WrongDispatcherIsUnavailable()
    {
        var window=Sta.Run(()=>new Window());
        using var observer=new InstanceObserver(ObserverReceipt());
        observer.Sample(window,"wrong-thread",1);
        Assert.Contains("wrong-dispatcher",Assert.Single(observer.Packets).Unavailable);
    }

    [Theory]
    [InlineData("original")] [InlineData("sample")] [InlineData("sink")] [InlineData("null")] [InlineData("success")] [InlineData("format")]
    public async Task NativeObserver_NonGui_RealAwaitFinallyAndSinkPreserveOriginal(string fault)
    {
        var receipt=ObserverReceipt();
        await RunDispatcherAsync(async ()=>
        {
            var window=new ObserverControlWindow(new Border());
            var observerReceipt=fault=="sink"?new Receipt(Path.Combine(receipt.DirectoryPath,"absent"))
                :fault=="format"?new Receipt(receipt.DirectoryPath,new ThrowingPacketConverter()):receipt;
            using var observer=new InstanceObserver(observerReceipt);
            if(fault=="sample")observer.BeforeSampleForControl=()=>throw new InvalidOperationException("DO-NOT-EMIT-SAMPLE");
            var sentinel=new InvalidOperationException("DO-NOT-EMIT-ORIGINAL");var calls=0;
            var failure=await Record.ExceptionAsync(()=>ObserveWithInstancesAsync(window,nint.Zero,receipt,observer,async()=>
            {
                calls++;
                await Task.Yield();
                if(fault=="success")return;
                if(fault=="null")Assert.NotNull((object?)null);
                throw sentinel;
            }));
            Assert.Equal(1,calls);
            if(fault=="success")Assert.Null(failure);
            else if(fault=="null")Assert.IsType<Xunit.Sdk.NotNullException>(failure);
            else Assert.Same(sentinel,failure);
            Assert.Equal(new[]{"before-query-batch","after-query-batch"},observer.Packets.Select(p=>p.Boundary));
            Assert.All(observer.Packets,p=>Assert.Equal("STA",p.Apartment));
            Assert.True(observer.Packets[0].EndTick<=observer.Packets[1].StartTick);
            Assert.Equal(observer.Packets[0].BatchId,observer.Packets[1].BatchId);
            Assert.Equal(0,observerReceipt.FailureCount);
            if(fault is "sink" or "format")Assert.True(observer.SinkUnavailable);
            if(fault=="sample")Assert.All(observer.Packets,p=>Assert.Contains("InvalidOperationException",p.Unavailable));
            Assert.Equal(nint.Zero,new WindowInteropHelper(window).Handle);
            window.Close();
        },receipt);
    }


    [Theory]
    [InlineData("original")] [InlineData("null")] [InlineData("success")]
    public async Task NativeObserver_NonGui_SharedFormatterFailurePreservesLaterOriginalWrites(string outcome)
    {
        var directory=ObserverReceipt().DirectoryPath;
        var receipt=new Receipt(directory,new ThrowingPacketConverter());
        receipt.Mark("original.before",new { Original=true });
        await RunDispatcherAsync(async()=>
        {
            var window=new ObserverControlWindow(new Border());
            using var observer=new InstanceObserver(receipt);
            var sentinel=new InvalidOperationException("original-sentinel");
            var calls=0;
            var failure=await Record.ExceptionAsync(()=>ObserveWithInstancesAsync(window,nint.Zero,receipt,observer,async()=>
            {
                calls++;await Task.Yield();
                if(outcome=="original")throw sentinel;
                if(outcome=="null")Assert.NotNull((object?)null);
            }));
            Assert.Equal(1,calls);
            if(outcome=="original")Assert.Same(sentinel,failure);
            else if(outcome=="null")Assert.IsType<Xunit.Sdk.NotNullException>(failure);
            else Assert.Null(failure);
            Assert.True(observer.SinkUnavailable);
            receipt.Mark("original.after",new { Original=true });
            using var saved=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"receipt.json")));
            var stages=saved.RootElement.GetProperty("Events").EnumerateArray().Select(e=>e.GetProperty("Stage").GetString()).ToArray();
            Assert.Equal(new[]{"original.before","original.after"},stages);
            Assert.Equal(0,receipt.FailureCount);
            Assert.False(receipt.Completed);
            Assert.Equal(nint.Zero,new WindowInteropHelper(window).Handle);
            window.Close();
        },receipt);
    }

    [Fact]
    public void NativeObserver_NonGui_ObserverPublishesSerializedSnapshotOnce()
    {
        var converter=new SingleWritePacketConverter();
        var receipt=new Receipt(ObserverReceipt().DirectoryPath,converter);
        receipt.Mark("original.before",new { Original=true });
        using var observer=new InstanceObserver(receipt);
        observer.Sample(null!,"unavailable-control",0);
        observer.Flush();
        Assert.False(observer.SinkUnavailable);
        receipt.Mark("original.after",new { Original=true });
        Assert.Equal(1,converter.Writes);
        using var saved=JsonDocument.Parse(File.ReadAllText(Path.Combine(receipt.DirectoryPath,"receipt.json")));
        var packet=Assert.Single(saved.RootElement.GetProperty("Events").EnumerateArray(),e=>e.GetProperty("Stage").GetString()=="observer.wpf");
        Assert.Equal("frozen-packet",packet.GetProperty("Attributes").GetProperty("Snapshot").GetString());
    }

    private sealed class SingleWritePacketConverter : System.Text.Json.Serialization.JsonConverter<WpfObservation>
    {
        internal int Writes { get; private set; }
        public override WpfObservation Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>throw new NotSupportedException();
        public override void Write(Utf8JsonWriter writer,WpfObservation value,JsonSerializerOptions options)
        {
            if(++Writes!=1)throw new InvalidOperationException("packet-serialized-again");
            writer.WriteStartObject();writer.WriteString("Snapshot","frozen-packet");writer.WriteEndObject();
        }
    }

    [Fact]
    public async Task NativeObserver_NonGui_ReleaseIdentityComesFromActualEvent()
    {
        var receipt=ObserverReceipt();
        await RunDispatcherAsync(async()=>
        {
            using var observer=new InstanceObserver(receipt);
            var first=observer.TrackReader(new ObservedReader(new ObserverPort(),receipt,"same-role",observer));
            var second=observer.TrackReader(new ObservedReader(new ObserverPort(),receipt,"same-role",observer));
            var lease=await first.AdmitAsync(CancellationToken.None);await second.AdmitAsync(CancellationToken.None);
            var window=new ObserverControlWindow(new Border());
            observer.Sample(window,"before-release",0);
            Assert.Equal(2,observer.Packets.Last().Readers.Length);
            Assert.All(observer.Packets.Last().Readers,row=>Assert.Null(row.ReleaseStartEvent));
            await lease.DisposeAsync();
            observer.Sample(window,"after-release",0);observer.Flush();
            var released=Assert.Single(observer.Packets.Last().Readers,row=>row.WrapperId==observer.Identity(first));
            Assert.NotNull(released.ReleaseStartEvent);Assert.True(released.ReleaseCompleted);
            Assert.NotEqual(released.WrapperId,Assert.Single(observer.Packets.Last().Readers,row=>row.WrapperId==observer.Identity(second)).WrapperId);
            using var document=JsonDocument.Parse(File.ReadAllText(receipt.DirectoryPath+"/receipt.json"));
            var transition=Assert.Single(document.RootElement.GetProperty("Events").EnumerateArray(),row=>row.GetProperty("Stage").GetString()=="observer.lease-transition");
            Assert.Equal(released.ReleaseStartEvent,transition.GetProperty("Attributes").GetProperty("Sequence").GetInt64());
            Assert.Equal(observer.Identity(lease),transition.GetProperty("Attributes").GetProperty("LeaseId").GetInt32());
            Assert.Equal("release-start",transition.GetProperty("Attributes").GetProperty("Phase").GetString());
            window.Close();
        },receipt);
    }

    [Fact]
    public void NativeObserver_NonGui_ActualNativeSitesPreserveTheOriginalQueryContract()
    {
        var source=File.ReadAllText(Path.Combine(Tree,"tests/AiDe.App.Tests/Workbench/Understanding/AtlasDaemonMainWindowProofTests.cs"));
        var syntax=Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source).GetRoot();
        Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax Method(string name)=>syntax.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().Single(node=>node.Identifier.ValueText==name);
        var original=Method("ObserveAutomationAsync");
        var calls=original.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>().ToArray();
        Assert.Single(calls,call=>call.Expression.ToString()=="AutomationElement.FromHandle");
        var find=Assert.Single(calls,call=>call.Expression.ToString()=="root.FindFirst");
        Assert.Equal("TreeScope.Descendants",find.ArgumentList.Arguments[0].Expression.ToString());
        Assert.Equal("new PropertyCondition(AutomationElement.NameProperty, name)",find.ArgumentList.Arguments[1].Expression.ToString());
        Assert.Contains(calls,call=>call.ToString()=="Assert.NotNull(element)");
        Assert.Contains(calls,call=>call.ToString()=="Assert.Equal(Environment.ProcessId, root.Current.ProcessId)");
        Assert.Contains(calls,call=>call.Expression.ToString()=="Assert.False" && call.ArgumentList.Arguments[0].ToString()=="element.Current.IsOffscreen");
        Assert.Equal(new[]{"Atlas files","Atlas member outline","Atlas source page read-only","Atlas pagination and bounds","Back to restored Atlas receipt"},
            original.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ImplicitArrayCreationExpressionSyntax>().Single().Initializer!.Expressions
                .Cast<Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax>().Select(node=>node.Token.ValueText));
        var journey=Method("MainWindow_RealDaemonReplacement_AcknowledgesHealthyReleaseAndPreservesBorrowedClient");
        Assert.Equal(2,journey.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>().Count(call=>call.Expression.ToString()=="ObserveWithInstancesAsync"));
        Assert.Contains("WaitAsync(TimeSpan.FromSeconds(30))",Method("RunDispatcherAsync").ToString(),StringComparison.Ordinal);
        var observer=syntax.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().Single(node=>node.Identifier.ValueText=="InstanceObserver");
        foreach(var banned in new[]{"ActivateAsync(","UpdateLayout(",".Focus(",".Show(","AutomationPeer","FromHandle(","GetRuntimeId(","InvokeAsync(","Task.Delay(","_generation","._lease"})
            Assert.DoesNotContain(banned,observer.ToString(),StringComparison.Ordinal);
        Assert.Single(Method("Capture").DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>(),call=>call.Expression.ToString()=="bitmap.Render");
    }


    [Fact]
    public void NativeObserver_NonGui_NullContentIsNotAReaderInstance()
    {
        Sta.Run(()=>
        {
            using var observer=new InstanceObserver(ObserverReceipt());
            var host=new AtlasLoadingHost(null) { Content=null };
            var window=new ObserverControlWindow(host);observer.Sample(window,"null-content",0);
            var row=Assert.Single(Assert.Single(observer.Packets).Hosts);
            Assert.Null(row.ContentId);Assert.Null(row.ReaderViewId);Assert.False(row.ContentIsReaderView);
            window.Close();
        });
    }
    private sealed class ThrowingPacketConverter : System.Text.Json.Serialization.JsonConverter<WpfObservation>
    {
        public override WpfObservation Read(ref Utf8JsonReader reader,Type typeToConvert,JsonSerializerOptions options)=>throw new NotSupportedException();
        public override void Write(Utf8JsonWriter writer,WpfObservation value,JsonSerializerOptions options)=>throw new InvalidOperationException("DO-NOT-EMIT-FORMAT");
    }

    [Fact]
    public void NativeObserver_NonGui_PostQueryRuntimeIdentityIsBoundedWithoutMutatingInput()
    {
        var original=Enumerable.Range(0,40).ToArray();
        var bounded=BoundedRuntimeId(original,out var truncated);
        Assert.True(truncated);Assert.Equal(Enumerable.Range(0,32),bounded);Assert.Equal(40,original.Length);
        Assert.Null(BoundedRuntimeId(null,out var nullTruncated));Assert.False(nullTruncated);
    }

    private static Receipt ObserverReceipt()
    {
        var directory=Path.Combine(Tree,".artifacts/atlas-observer-controls",Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);return new Receipt(directory);
    }
    private sealed class EqualObservationObject
    {
        public override bool Equals(object? other)=>other is EqualObservationObject;
        public override int GetHashCode()=>1;
    }
    // An unshown WPF visual root: real AddVisualChild/parent relationships, no HWND/layout/peer.
    private sealed class ObserverControlWindow : Window
    {
        private readonly UIElement _child;
        internal ObserverControlWindow(UIElement child) { _child=child;AddVisualChild(child); }
        protected override int VisualChildrenCount=>1;
        protected override Visual GetVisualChild(int index)=>index==0?_child:throw new ArgumentOutOfRangeException(nameof(index));
    }
    private sealed class ObserverPort : IAtlasWorkspaceReader,IAtlasReaderQueries
    {
        public ValueTask<IAtlasReaderLease> AdmitAsync(CancellationToken cancellationToken)=>ValueTask.FromResult<IAtlasReaderLease>(new Lease(this));
        public ValueTask DisposeAsync()=>ValueTask.CompletedTask;
        public ValueTask<AtlasInventoryPageDto> InventoryAsync(AtlasInventoryRequestDto request,CancellationToken cancellationToken)=>
            ValueTask.FromResult(new AtlasInventoryPageDto(1,"DO-NOT-EMIT-SCOPE",7,"DO-NOT-EMIT-MANIFEST",AtlasCompletionState.Complete,[],
                new(AtlasBoundsDimension.InventoryRows,64,64,0,0,AtlasDenominatorState.Known,0,null,null,null),null,[]));
        public ValueTask<AtlasSelectionDto> SelectAsync(AtlasSelectRequestDto request,CancellationToken cancellationToken)=>throw new NotSupportedException();
        public ValueTask<AtlasSelectionDto> RestoreAsync(AtlasRestoreRequestDto request,CancellationToken cancellationToken)=>throw new NotSupportedException();
        private sealed class Lease(ObserverPort owner):IAtlasReaderLease
        {
            public string ScopeToken=>"DO-NOT-EMIT-SCOPE"; public string InitialManifestToken=>"DO-NOT-EMIT-MANIFEST";
            public long CoreEpoch=>7; public DateTimeOffset ExpiresAt=>DateTimeOffset.MaxValue;
            public IAtlasReaderQueries Queries=>owner; public CancellationToken Invalidated=>CancellationToken.None;
            public bool IsTerminal { get; private set; }
            public ValueTask DisposeAsync(){IsTerminal=true;return ValueTask.CompletedTask;}
        }
    }

    private sealed record HostObservation(int? Id,int? ContentId,int? ReaderViewId,bool? ContentIsReaderView,
        string State,string Attachment,int? ParentId,bool CensusMember,bool TestRetained,bool? IsLoaded,bool? IsVisible,string ContentKind,bool ObserverRetained=true);
    private sealed record ViewObservation(int? Id,int? FilesControlId,int? OutlineControlId,int? SourceControlId,int? DocumentId,
        int? FileRoots,int? OutlineRows,int? Highlights,bool? ReadOnly,bool? CanGoBack,bool? CanLoadMore,
        string Attachment,int? ParentId,bool CensusMember,bool TestRetained,bool? IsLoaded,bool? IsVisible,
        string BackingLeaseRelation="not-observed",bool ObserverRetained=true);
    private sealed record ReaderObservation(int? WrapperId,int? InnerReaderId,int? LeaseId,bool? Disposed,
        bool? Terminal,bool? Invalidated,long? ReleaseStartEvent,bool? ReleaseCompleted);
    private sealed record WpfObservation(long Sequence,long BatchId,string Boundary,DateTimeOffset StartedUtc,DateTimeOffset EndedUtc,
        long StartTick,long EndTick,int ThreadId,string Apartment,int? WindowId,int? VisitedNodes,int? QueuedNotVisited,
        bool Truncated,string[] Unavailable,HostObservation[] Hosts,ViewObservation[] Views,ReaderObservation[] Readers,
        bool? WindowLoaded,bool? WindowVisible,double? WindowWidth,double? WindowHeight);
    private sealed record LeaseTransition(long Sequence,int? LeaseId,string Phase,DateTimeOffset ObservedUtc,long Tick,
        int ThreadId,string Apartment,string SourceEvent);

    // Fixed test-local limits; this observer owns no product actions or automation peers.
    private sealed class InstanceObserver : IDisposable
    {
        private readonly Receipt _receipt;
        private readonly object _gate=new();
        private readonly List<object> _references=[];
        private readonly List<object> _held=[];
        private readonly List<WpfObservation> _packets=[];
        private readonly List<(ObservedLease Lease,long Sequence)> _releaseStarts=[];
        private readonly List<LeaseTransition> _transitions=[];
        private readonly int _maxNodes,_maxDepth,_maxReferences,_maxPackets;
        private long _sequence,_batch,_query;
        private int _flushed,_flushedTransitions;
        internal InstanceObserver(Receipt receipt,int maxNodes=512,int maxDepth=48,int maxReferences=256,int maxPackets=16)
        {
            _receipt=receipt;_maxNodes=Math.Clamp(maxNodes,1,512);_maxDepth=Math.Clamp(maxDepth,0,48);
            _maxReferences=Math.Clamp(maxReferences,1,256);_maxPackets=Math.Clamp(maxPackets,1,16);
        }
        internal Action? BeforeSampleForControl { get; set; }
        internal IReadOnlyList<WpfObservation> Packets=>_packets;
        internal bool Truncated { get; private set; }
        internal bool SinkUnavailable { get; private set; }
        internal int? Identity(object? value)
        {
            if(value is null)return null;
            lock(_gate)
            {
                for(var i=0;i<_references.Count;i++)if(ReferenceEquals(value,_references[i]))return i+1;
                if(_references.Count>=_maxReferences){Truncated=true;return null;}
                _references.Add(value);return _references.Count;
            }
        }
        internal long NextBatch(){lock(_gate)return ++_batch;}
        internal long NextQuery(){lock(_gate)return ++_query;}
        internal ObservedReader TrackReader(ObservedReader reader){Retain(reader);return reader;}
        internal void Retain(params object?[] values)
        {
            try
            {
                lock(_gate)foreach(var value in values)
                {
                    if(value is null || _held.Any(item=>ReferenceEquals(item,value)))continue;
                    if(_held.Count>=256){Truncated=true;break;}
                    if(Identity(value) is not null)_held.Add(value);
                }
            }
            catch(Exception error) when(error is not OutOfMemoryException){Truncated=true;}
        }
        internal void ReleaseStarted(ObservedLease lease)
        {
            try
            {
                lock(_gate)
                {
                    if(_releaseStarts.Count>=32){Truncated=true;return;}
                    var sequence=++_sequence;
                    _releaseStarts.Add((lease,sequence));
                    _transitions.Add(new(sequence,Identity(lease),"release-start",DateTimeOffset.UtcNow,Stopwatch.GetTimestamp(),
                        Environment.CurrentManagedThreadId,Thread.CurrentThread.GetApartmentState().ToString(),"ObservedLease.lease-release-start returned"));
                }
            }
            catch(Exception error) when(error is not OutOfMemoryException){Truncated=true;}
        }
        internal void Sample(Window window,string boundary,long batch)
        {
            // Include formatting and field access in the added diagnostic failure boundary.
            try { lock(_gate) SampleCore(window,boundary,batch); }
            catch(Exception error) when(error is not OutOfMemoryException){Truncated=true;}
        }
        private void SampleCore(Window window,string boundary,long batch)
        {
            if(_packets.Count>=_maxPackets){Truncated=true;return;}
            var started=Stopwatch.GetTimestamp();var utc=DateTimeOffset.UtcNow;
            var unavailable=new List<string>();var hosts=new List<HostObservation>();var views=new List<ViewObservation>();
            var readers=new List<ReaderObservation>();var seen=new List<DependencyObject>();
            var queue=new Queue<(DependencyObject Node,int Depth)>();int? visited=null,queued=null,windowId=null;
            bool? windowLoaded=null,windowVisible=null;double? windowWidth=null,windowHeight=null;
            bool Budget()=>Stopwatch.GetElapsedTime(started).TotalMilliseconds>=25;
            void Limited(string reason){Truncated=true;if(unavailable.Count<32 && !unavailable.Contains(reason))unavailable.Add(reason);}
            bool Held(object value)=>_held.Any(item=>ReferenceEquals(item,value));
            bool Seen(object value)=>seen.Any(item=>ReferenceEquals(item,value));
            (string State,int? Parent) Connection(DependencyObject node)
            {
                DependencyObject? current=node;int? parentId=null;
                for(var depth=0;depth<=48;depth++)
                {
                    if(Budget()){Limited("time-budget");return ("unavailable",parentId);}
                    if(ReferenceEquals(current,window))return ("owned-window",parentId);
                    var parent=VisualTreeHelper.GetParent(current!);
                    if(depth==0)parentId=Identity(parent);
                    if(parent is null)return ("detached",parentId);
                    current=parent;
                }
                Limited("parent-depth");return ("unavailable",parentId);
            }
            try
            {
                if(!window.Dispatcher.CheckAccess()){unavailable.Add("wrong-dispatcher");return;}
                BeforeSampleForControl?.Invoke();
                windowId=Identity(window);windowLoaded=window.IsLoaded;windowVisible=window.IsVisible;
                windowWidth=double.IsFinite(window.ActualWidth)?window.ActualWidth:null;
                windowHeight=double.IsFinite(window.ActualHeight)?window.ActualHeight:null;
                queue.Enqueue((window,0));visited=0;
                while(queue.Count!=0)
                {
                    if(seen.Count>=_maxNodes || Budget()){Limited(seen.Count>=_maxNodes?"node-limit":"time-budget");break;}
                    var (node,depth)=queue.Dequeue();seen.Add(node);visited=seen.Count;
                    var count=VisualTreeHelper.GetChildrenCount(node);
                    if(depth>=_maxDepth && count!=0){Limited("depth-limit");continue;}
                    for(var index=0;index<count;index++)
                    {
                        if(queue.Count>=512 || Budget()){Limited(queue.Count>=512?"queue-limit":"time-budget");break;}
                        queue.Enqueue((VisualTreeHelper.GetChild(node,index),depth+1));
                    }
                }
                queued=queue.Count;
                var candidates=new List<object>();
                foreach(var item in seen.Cast<object>().Concat(_held).Concat(_references.ToArray()))
                    if(!candidates.Any(value=>ReferenceEquals(value,item)))candidates.Add(item);
                foreach(var host in candidates.OfType<AtlasLoadingHost>().Take(17).ToArray())
                {
                    if(hosts.Count>=16 || Budget()){Limited(hosts.Count>=16?"host-limit":"time-budget");break;}
                    try
                    {
                        var child=host.Content;var view=host.ReaderView;
                        if(child is AtlasReaderView childView && !candidates.Any(value=>ReferenceEquals(value,childView)))candidates.Add(childView);
                        if(view is not null && !candidates.Any(value=>ReferenceEquals(value,view)))candidates.Add(view);
                        var connection=Connection(host);
                        var state=host.StatusText switch
                        {
                            "Loading Code Atlas."=>"loading",
                            "ATLAS-HOST-NO-WORKSPACE: open a workspace."=>"no-workspace",
                            "ATLAS-HOST-INACTIVE: source cleared."=>"inactive",
                            "ATLAS-HOST-CANCELED: activation canceled."=>"canceled",
                            "ATLAS-HOST-UNAVAILABLE: admission unavailable."=>"unavailable",
                            _=>"other",
                        };
                        hosts.Add(new(Identity(host),Identity(child),Identity(view),view is not null && ReferenceEquals(child,view),state,
                            connection.State,connection.Parent,Seen(host),Held(host),host.IsLoaded,host.IsVisible,
                            child switch { AtlasReaderView=>"AtlasReaderView",TextBlock=>"TextBlock",null=>"null",_=>"other" }));
                    }
                    catch(Exception error) when(error is not OutOfMemoryException){Limited("host:"+error.GetType().Name);}
                }
                foreach(var view in candidates.OfType<AtlasReaderView>().Take(33))
                {
                    if(views.Count>=32 || Budget()){Limited(views.Count>=32?"view-limit":"time-budget");break;}
                    bool? isLoaded=null,isVisible=null;
                    try { isLoaded=view.IsLoaded; }
                    catch(Exception error) when(error is not OutOfMemoryException){Limited("view-loaded:"+error.GetType().Name);}
                    try { isVisible=view.IsVisible; }
                    catch(Exception error) when(error is not OutOfMemoryException){Limited("view-visible:"+error.GetType().Name);}
                    try
                    {
                        var connection=Connection(view);
                        views.Add(new(Identity(view),Identity(view.FilesControl),Identity(view.OutlineControl),Identity(view.SourceControl),
                            Identity(view.SourceControl.Document),view.FileRoots.Count,view.OutlineRows.Count,view.CurrentHighlights.Count,
                            view.IsSourceReadOnly,view.CanGoBack,view.CanLoadMore,connection.State,connection.Parent,Seen(view),Held(view),isLoaded,isVisible));
                    }
                    catch(Exception error) when(error is not OutOfMemoryException){Limited("view:"+error.GetType().Name);}
                }
                foreach(var reader in _held.OfType<ObservedReader>().Take(33))
                {
                    if(readers.Count>=32 || Budget()){Limited(readers.Count>=32?"reader-limit":"time-budget");break;}
                    try
                    {
                        var lease=reader.Lease;
                        var release=_releaseStarts.LastOrDefault(item=>ReferenceEquals(item.Lease,lease));
                        readers.Add(new(Identity(reader),Identity(reader.InnerForObservation),Identity(lease),reader.Disposed,
                            lease?.IsTerminal,lease?.Invalidated.IsCancellationRequested,
                            release.Lease is null?null:release.Sequence,lease?.ReleaseCompleted));
                    }
                    catch(Exception error) when(error is not OutOfMemoryException){Limited("reader:"+error.GetType().Name);}
                }
                if(Truncated && unavailable.Count==0)unavailable.Add("reference-or-retention-limit");
            }
            catch(Exception error) when(error is not OutOfMemoryException){Limited(error.GetType().Name);}
            finally
            {
                _packets.Add(new(++_sequence,batch,boundary[..Math.Min(boundary.Length,128)],utc,DateTimeOffset.UtcNow,
                    started,Stopwatch.GetTimestamp(),Environment.CurrentManagedThreadId,Thread.CurrentThread.GetApartmentState().ToString(),
                    windowId,visited,queued,Truncated,unavailable.ToArray(),hosts.ToArray(),views.ToArray(),readers.ToArray(),
                    windowLoaded,windowVisible,windowWidth,windowHeight));
            }
        }
        internal void Flush()
        {
            try
            {
                lock(_gate)
                {
                    while(_flushed<_packets.Count)
                    {
                        var packet=_packets[_flushed++];
                        _receipt.MarkObservation("observer.wpf",packet);
                    }
                    while(_flushedTransitions<_transitions.Count)
                        _receipt.MarkObservation("observer.lease-transition",_transitions[_flushedTransitions++]);
                    _receipt.MarkObservation("observer.status",new { Truncated,SinkUnavailable,Packets=_packets.Count,References=_references.Count,
                        MaxNodes=_maxNodes,MaxDepth=_maxDepth,MaxReferences=_maxReferences,MaxPackets=_maxPackets,
                        MaxQueue=512,MaxHosts=16,MaxViews=32,MaxReaders=32,BetweenOperationsMilliseconds=25,
                        ObserverRetainsReferences=true,PrivateGeneration="not-observed",ViewBackingLease="not-observed" });
                }
            }
            catch(Exception error) when(error is not OutOfMemoryException){SinkUnavailable=true;}
        }
        public void Dispose(){Flush();lock(_gate){_references.Clear();_held.Clear();_releaseStarts.Clear();_transitions.Clear();}}
    }
    private static async Task ObserveWithInstancesAsync(Window window,nint hwnd,Receipt receipt,InstanceObserver observer,Func<Task>? originalForControl=null)
    {
        var batch=observer.NextBatch();
        observer.Sample(window,"before-query-batch",batch);
        try
        {
            if(originalForControl is not null)await originalForControl();
            else await ObserveAutomationAsync(hwnd,receipt,observer,batch);
        }
        finally
        {
            observer.Sample(window,"after-query-batch",batch);
            observer.Flush();
        }
    }


    [Fact]
    public async Task OwnedBlankWindowPreservesOriginalMissingNameFailureAndDiagnostics()
    {
        var directory = Path.Combine(Tree, "artifacts", "atlas-uia-diagnostic-control", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var receipt = new Receipt(directory);
        long hwnd = 0;
        await RunDispatcherAsync(async () =>
        {
            var window = new Window
            {
                Title = "Owned blank UIA diagnostic control",
                Content = new Border(), Width = 320, Height = 240,
                Left = 40, Top = 40, WindowStartupLocation = WindowStartupLocation.Manual,
                ShowInTaskbar = false, ShowActivated = false,
            };
            try
            {
                window.Show();
                await IdleAsync();
                hwnd = new WindowInteropHelper(window).Handle.ToInt64();
                receipt.Mark("control.owned-window", new { Hwnd = hwnd, ProcessId = Environment.ProcessId });
                var original = await Assert.ThrowsAsync<Xunit.Sdk.NotNullException>(
                    () => ObserveAutomationAsync(new nint(hwnd), receipt));
                receipt.Mark("control.original-not-null-failed", new { ExceptionType = original.GetType().FullName });
            }
            finally
            {
                window.Close();
                await IdleAsync();
            }
        }, receipt);

        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "receipt.json")));
        var events = document.RootElement.GetProperty("Events").EnumerateArray().ToArray();
        static string? Stage(JsonElement item) => item.GetProperty("Stage").GetString();
        var originalResult = Assert.Single(events, item => Stage(item) == "uia.find-first.original");
        var root = Assert.Single(events, item => Stage(item) == "uia.owned-root.after-original");
        var census = Assert.Single(events, item => Stage(item) == "uia.owned-root.census.after-original");
        var failure = Assert.Single(events, item => Stage(item) == "control.original-not-null-failed");
        Assert.True(Array.IndexOf(events, originalResult) < Array.IndexOf(events, root));
        Assert.True(Array.IndexOf(events, root) < Array.IndexOf(events, census));
        Assert.True(Array.IndexOf(events, census) < Array.IndexOf(events, failure));
        var result = originalResult.GetProperty("Attributes");
        Assert.Equal("Atlas files", result.GetProperty("ExpectedName").GetString());
        Assert.False(result.GetProperty("OriginalFound").GetBoolean());
        Assert.Equal(hwnd, result.GetProperty("OwnHwnd").GetInt64());
        Assert.Equal(Environment.ProcessId, result.GetProperty("ExpectedProcessId").GetInt32());
        var observedRoot = root.GetProperty("Attributes").GetProperty("Root");
        Assert.Equal(hwnd, observedRoot.GetProperty("NativeWindowHandle").GetInt64());
        Assert.Equal(Environment.ProcessId, observedRoot.GetProperty("ProcessId").GetInt32());
        var bounded = census.GetProperty("Attributes");
        Assert.Equal(hwnd, bounded.GetProperty("OwnHwnd").GetInt64());
        Assert.False(bounded.GetProperty("OriginalFound").GetBoolean());
        Assert.Equal(128, bounded.GetProperty("MaxNodes").GetInt32());
        Assert.Equal(12, bounded.GetProperty("MaxDepth").GetInt32());
        Assert.InRange(bounded.GetProperty("ObservedNodes").GetInt32(), 1, 128);
        var nodes = bounded.GetProperty("Nodes").EnumerateArray().ToArray();
        Assert.Equal(bounded.GetProperty("ObservedNodes").GetInt32(), nodes.Length);
        Assert.Equal(hwnd, nodes[0].GetProperty("NativeWindowHandle").GetInt64());
        Assert.All(nodes, node => Assert.InRange(node.GetProperty("Depth").GetInt32(), 0, 12));
        Assert.DoesNotContain(events, item => Stage(item) == "uia.own-hwnd");
        Assert.Equal(0, receipt.FailureCount);
        receipt.Completed = true;
        receipt.Mark("control.diagnostics-preserved", new { OriginalFailure = "NotNull", ExpectedMissingName = "Atlas files" });
    }

    [Fact]
    public async Task MainWindow_RealDaemonReplacement_AcknowledgesHealthyReleaseAndPreservesBorrowedClient()
    {
        var run = Environment.GetEnvironmentVariable("ATLAS_PROOF_RUN")
            ?? throw new InvalidOperationException("ATLAS_PROOF_RUN must name an owned receipt directory.");
        Assert.True(run.All(character => char.IsAsciiLetterOrDigit(character) || character == '-'));
        var evidence = Path.Combine(Tree, "artifacts", "atlas-real-daemon-window-proof", run);
        Assert.False(Directory.Exists(evidence), "A proof run must not overwrite an earlier receipt.");
        Directory.CreateDirectory(evidence);
        var receipt = new Receipt(evidence);
        var owned = Path.Combine(evidence, "owned");
        var daemons = new List<Daemon>();
        var clients = new List<WorkspaceClient>();
        try
        {
            var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            var binary = Path.Combine(Tree, "src", "AiDe.Daemon", "bin", configuration,
                "net10.0-windows", "AiDe.Daemon.exe");
            Assert.True(File.Exists(binary), "Build this tree's daemon explicitly before running this proof.");
            var gitRoot = Path.GetFullPath((await CommandAsync(Git, Tree, "rev-parse", "--show-toplevel")).Trim());
            Assert.True(string.Equals(Tree, gitRoot, StringComparison.OrdinalIgnoreCase),
                "The executing test assembly's repository must equal Git's actual current worktree root.");
            receipt.Mark("pins", new
            {
                RepositoryRoot = Tree, GitRoot = gitRoot,
                TestAssemblyPath = typeof(AtlasDaemonMainWindowProofTests).Assembly.Location,
                TestAssemblyRelativePath = Path.GetRelativePath(Tree, typeof(AtlasDaemonMainWindowProofTests).Assembly.Location),
                RootAuthority = "executing test assembly ancestors, repository project files, and actual Git top-level",
                SourceCommit = (await CommandAsync(Git, Tree, "rev-parse", "HEAD")).Trim(),
                Binary = binary, BinarySha256 = Hash(File.ReadAllBytes(binary)),
                DaemonAssemblySha256 = Hash(File.ReadAllBytes(Path.ChangeExtension(binary, ".dll"))),
                CoreAssemblySha256 = Hash(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(binary)!, "AiDe.Core.dll"))),
                AppAssemblySha256 = Hash(File.ReadAllBytes(typeof(MainWindowViewModel).Assembly.Location)),
                HarnessSha256 = Hash(File.ReadAllBytes(typeof(AtlasDaemonMainWindowProofTests).Assembly.Location)),
                TestSourceSha256 = Hash(File.ReadAllBytes(Path.Combine(Tree, "tests", "AiDe.App.Tests",
                    "Workbench", "Understanding", "AtlasDaemonMainWindowProofTests.cs"))),
                GitBinary = Git, GitSha256 = Hash(File.ReadAllBytes(Git)),
            });
            foreach (var name in new[] { "first", "replacement" })
            {
                var root = Path.Combine(owned, name, "repository");
                Directory.CreateDirectory(root);
                Directory.CreateDirectory(Path.Combine(root, "src"));
                File.WriteAllText(Path.Combine(root, "src", "Widget.cs"), Source, new UTF8Encoding(false));
                await GitAsync(root, "init", "--quiet");
                await GitAsync(root, "add", "--", "src/Widget.cs");
                await GitAsync(root, "commit", "--quiet", "-m", "owned synthetic source");
                var tracked = await GitAsync(root, "ls-files", "--", "src/Widget.cs");
                Assert.True(tracked.Trim() == "src/Widget.cs");
                receipt.Mark(name + ".input", new
                {
                    Root = root, File = "src/Widget.cs", Bytes = File.ReadAllBytes(Path.Combine(root, "src", "Widget.cs")).Length,
                    Sha256 = Hash(File.ReadAllBytes(Path.Combine(root, "src", "Widget.cs"))),
                    Commit = (await GitAsync(root, "rev-parse", "HEAD")).Trim(),
                });
                var daemon = Daemon.Start(binary, root, receipt, name);
                daemons.Add(daemon);
                var client = await WorkspaceClient.ConnectAsync(IpcPipeName.ForWorkspace(root),
                    TimeSpan.FromSeconds(10), CancellationToken.None);
                clients.Add(client);
                receipt.Mark(name + ".connected", new { daemon.Pid, client.Epoch });
            }

            await RunDispatcherAsync(async () =>
            {
                using var observer=new InstanceObserver(receipt);
                ObservedReader? firstReader = null;
                ObservedReader? replacementReader = null;
                var first = new MainWindowViewModel(clients[0], "display-first-not-pipe-authority", null,
                    commands: clients[0], atlasReaderFactory: () =>
                        observer.TrackReader(firstReader = new ObservedReader(clients[0].CreateAtlasReader(), receipt, "first", observer)));
                var replacement = new MainWindowViewModel(clients[1], "display-replacement-not-pipe-authority", null,
                    commands: clients[1], atlasReaderFactory: () =>
                        observer.TrackReader(replacementReader = new ObservedReader(clients[1].CreateAtlasReader(), receipt, "replacement", observer)));
                receipt.Mark("remote-vms.before-refresh", new
                {
                    FirstStatus = first.StatusMessage, ReplacementStatus = replacement.StatusMessage,
                });
                await first.RefreshAsync(CancellationToken.None);
                await replacement.RefreshAsync(CancellationToken.None);
                receipt.Mark("remote-vms.after-refresh", new
                {
                    FirstStatus = first.StatusMessage, ReplacementStatus = replacement.StatusMessage,
                    Initialization = "actual MainWindowViewModel.RefreshAsync over each owned real remote client",
                });
                var window = new AiDe.App.MainWindow(() => Task.FromResult(first),
                    Path.Combine(owned, "shell-state"), Resources())
                {
                    Width = 1280, Height = 900, Left = 40, Top = 40,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    ShowInTaskbar = false,
                };
                var firstSourcePath = Path.Combine(owned, "first", "repository", "src", "Widget.cs");
                var replacementSourcePath = Path.Combine(owned, "replacement", "repository", "src", "Widget.cs");
                try
                {
                    window.Shell.Execute(PerspectiveSet.Architecture.CommandId);
                    var menu = Assert.IsType<Menu>(window.FindName("MainMenu"));
                    var title = PerspectiveMenu.Opener(
                        SurfaceContentFactory.Kinds.Single(kind => kind.Kind == "code-atlas")).Title;
                    var opener = Assert.Single(MenuItems(menu),
                        item => string.Equals(item.Header?.ToString(), title, StringComparison.Ordinal));
                    opener.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                    window.Show();
                    await IdleAsync();
                    await window.WorkspaceReady;
                    window.UpdateLayout();
                    await IdleAsync();
                    var host = Assert.Single(Visuals<AtlasLoadingHost>(window));
                    await host.ActivateAsync();
                    var view = Assert.IsType<AtlasReaderView>(host.ReaderView);
                    observer.Retain(host,view);
                    var file = await FindOwnedFileAsync(view);
                    await view.SelectFileAsync(file);
                    var firstLease = Assert.IsType<ObservedLease>(firstReader?.Lease);
                    var fileSelection = Assert.IsType<AtlasSelectionDto>(firstLease.LastSelection);
                    VerifySource(view, fileSelection, receipt, "file", firstSourcePath);
                    var method = Assert.Single(fileSelection.Outline, row => row.Kind == AtlasDeclarationKind.Method);
                    var outline = Assert.Single(view.OutlineRows, row => row.ObservationKey == method.DeclarationToken);
                    var nativeBinding = fileSelection.Source.BindingToken;
                    Assert.False(string.IsNullOrEmpty(nativeBinding), "Verified source must carry its native binding.");
                    Assert.False(string.IsNullOrEmpty(fileSelection.ReceiptToken), "File selection must produce a receipt.");
                    await view.SelectDeclarationAsync(outline);
                    var memberSelection = Assert.IsType<AtlasSelectionDto>(firstLease.LastSelection);
                    VerifySource(view, memberSelection, receipt, "member", firstSourcePath);
                    Assert.True(memberSelection.DeclarationToken == method.DeclarationToken, "Selected member must match the native declaration.");
                    var actualSource = File.ReadAllText(firstSourcePath);
                    receipt.Mark("member.span-oracles", new
                    {
                        DeclarationSpan = method.Span, Highlights = memberSelection.Source.Highlights,
                        RenderedHighlightCount = view.CurrentHighlights.Count,
                        RejectedAssumption = "Declaration extent and source highlight extent must be identical",
                    });
                    Assert.True(memberSelection.Source.Highlights.Any(span =>
                        span.Start >= 0 && span.Length > 0 && span.Start + span.Length <= actualSource.Length
                        && actualSource.Substring(span.Start, span.Length).Contains("Answer", StringComparison.Ordinal)),
                        "A native highlight must identify Answer in the actual owned source bytes.");
                    Assert.Equal(memberSelection.Source.Highlights.Length, view.CurrentHighlights.Count);
                    Assert.True(File.ReadAllText(firstSourcePath).Substring(method.Span.Start, method.Span.Length).Contains("Answer()", StringComparison.Ordinal),
                        "The returned span must identify the method in the real owned bytes.");
                    var selectedOutline = Assert.Single(view.OutlineRows,
                        row => row.ObservationKey == method.DeclarationToken);
                    view.OutlineControl.SelectedItem = selectedOutline;
                    view.OutlineControl.ScrollIntoView(selectedOutline);
                    window.UpdateLayout();
                    await IdleAsync();
                    Assert.True(view.FilesControl.Focus(), "The rendered Atlas files control must accept keyboard focus.");
                    await IdleAsync();
                    var atlas = Assert.Single(window.Shell.Architecture.Service.Zones.AllSurfaces(),
                        surface => surface.Kind == "code-atlas");
                    var canonicalZone = window.Shell.Architecture.Service.Zones.FindZoneOf(atlas.SurfaceId);
                    receipt.Mark("normal-default.placement", new
                    {
                        atlas.SurfaceId, CanonicalZone = canonicalZone.ToString(),
                        AtlasWidth = view.ActualWidth, WindowWidth = window.ActualWidth,
                        view.IsVisible, view.IsKeyboardFocusWithin,
                        window.Shell.Architecture.Controller.FocusedSurfaceId,
                        window.Shell.Architecture.Controller.FocusedStackId,
                        PlacementAction = "existing Architecture opener; normal new Center default",
                        MaximizeInvoked = false,
                    });
                    try
                    {
                        _ = MeasureReading(window, view, actualSource, memberSelection.Source.Highlights,
                            receipt, "normal-default-observed", requireReadable: false);
                        VerifyFooter(window, first, receipt, "first");
                    }
                    finally { Capture(window, evidence, receipt, "mainwindow-member-normal-default.png",observer); }
                    Assert.True(canonicalZone == ZoneId.Center, "The real normal opener must place a new Atlas in canonical Center.");
                    Assert.True(view.IsVisible && view.IsKeyboardFocusWithin);
                    _ = MeasureReading(window, view, actualSource, memberSelection.Source.Highlights,
                        receipt, "normal-default-verified", requireReadable: true);
                    await ObserveWithInstancesAsync(window,new WindowInteropHelper(window).Handle,receipt,observer);
                    Assert.True(view.CanGoBack);
                    await view.GoBackAsync();
                    var restored = Assert.IsType<AtlasSelectionDto>(firstLease.LastSelection);
                    Assert.True(restored.FileToken == fileSelection.FileToken && restored.DeclarationToken == fileSelection.DeclarationToken,
                        "Back must restore the original file/declaration selection.");
                    Assert.True(restored.Source.BindingToken == nativeBinding, "Back must restore the original native-Q binding.");
                    VerifySource(view, restored, receipt, "back", firstSourcePath);
                    receipt.Mark("back.native-binding-restored", new { SameBinding = true, SameSelection = true });
                    window.UpdateLayout();
                    await IdleAsync();
                    var hwnd = new WindowInteropHelper(window).Handle;
                    await ObserveWithInstancesAsync(window,hwnd,receipt,observer);
                    Capture(window, evidence, receipt,observer:observer);

                    Assert.False(firstLease.IsTerminal, "Replacement must start from a healthy lease, not a terminal disposal shortcut.");
                    var apply = window.ApplyWorkspaceAsync(replacement);
                    Assert.True(view.SourceText.Length == 0, "Replacement must synchronously clear old source.");
                    await apply;
                    await IdleAsync();
                    window.UpdateLayout();
                    Assert.Same(replacement, window.DataContext);
                    Assert.True(firstLease.HealthyAtRelease && firstLease.ReleaseCompleted,
                        "The actual healthy lease DisposeAsync must return normally through acknowledged atlas.release.");
                    Assert.True(firstReader!.Disposed);
                    var oldFailure = await Record.ExceptionAsync(async () =>
                        await firstLease.Queries.InventoryAsync(new AtlasInventoryRequestDto(
                            1, firstLease.ScopeToken, firstLease.CoreEpoch, firstLease.InitialManifestToken, 0, 64),
                            CancellationToken.None));
                    Assert.NotNull(oldFailure);
                    Assert.True(oldFailure is OperationCanceledException or ObjectDisposedException
                        || oldFailure.GetType().Name == "AtlasReadException",
                        "The invalidated/disposed client lease must refuse reuse.");
                    receipt.Mark("old-scope.refused", new
                    {
                        Layer = "local invalidated/disposed AtlasRemoteReader guard; not a server response",
                        ExceptionType = oldFailure.GetType().FullName,
                    });
                    var nextHost = Assert.Single(Visuals<AtlasLoadingHost>(window));
                    await nextHost.ActivateAsync();
                    var nextView = Assert.IsType<AtlasReaderView>(nextHost.ReaderView);
                    observer.Retain(nextHost,nextView);
                    Assert.NotSame(view, nextView);
                    await nextView.SelectFileAsync(await FindOwnedFileAsync(nextView));
                    var nextLease = Assert.IsType<ObservedLease>(replacementReader?.Lease);
                    Assert.True(nextLease.ScopeToken != firstLease.ScopeToken, "Replacement must own a distinct actual scope.");
                    VerifySource(nextView, Assert.IsType<AtlasSelectionDto>(nextLease.LastSelection), receipt, "replacement", replacementSourcePath);
                    try { VerifyFooter(window, replacement, receipt, "replacement"); }
                    finally { Capture(window, evidence, receipt, "mainwindow-replacement-footer.png",observer); }
                    var receiptFailure = await Record.ExceptionAsync(async () =>
                        await nextLease.Queries.RestoreAsync(new AtlasRestoreRequestDto(
                            1, nextLease.ScopeToken, nextLease.CoreEpoch, fileSelection.ReceiptToken!), CancellationToken.None));
                    Assert.NotNull(receiptFailure);
                    Assert.True(receiptFailure.GetType().Name == "AtlasReadException",
                        "The real Atlas reader must reject the foreign receipt through its typed refusal.");
                    Assert.False(nextLease.IsTerminal, "Rejected foreign receipt must not terminalize the replacement scope.");
                    receipt.Mark("foreign-receipt.refused", new
                    {
                        Layer = "replacement client receipt admission guard; no new server authority",
                        ExceptionType = receiptFailure.GetType().FullName,
                    });
                    await IdleAsync();
                    Assert.True(view.SourceText.Length == 0, "Old source must stay cleared after replacement publication and dispatcher drain.");
                    receipt.Mark("replacement.old-source-cleared", new { BeforeAwait = true, AfterNewSelectionAndIdle = true });
                    await clients[0].FindAsync("", 1, CancellationToken.None);
                    await clients[1].FindAsync("", 1, CancellationToken.None);
                    window.Close();
                    await window.CloseOperation;
                    await IdleAsync();
                    Assert.False(window.IsVisible);
                    Assert.True(nextLease.HealthyAtRelease && nextLease.ReleaseCompleted && replacementReader!.Disposed);
                    await clients[0].FindAsync("", 1, CancellationToken.None);
                    await clients[1].FindAsync("", 1, CancellationToken.None);
                    receipt.Mark("window.closed-borrowed-clients-usable", new
                    {
                        WindowVisible = window.IsVisible, QueryRoundTrips = 2,
                        CommandAndQueryInterfacesShareSameBorrowedWorkspaceClient = true,
                    });
                }
                catch (Exception exception)
                {
                    receipt.Failure("ui.primary", exception);
                    throw;
                }
                finally
                {
                    try
                    {
                        if (window.IsVisible)
                        {
                            window.Close();
                            await window.CloseOperation;
                            await IdleAsync();
                        }
                    }
                    catch (Exception exception) { receipt.Failure("window.cleanup", exception); }
                }
            }, receipt);
            foreach (var client in clients) await client.DisposeAsync();
            clients.Clear();
            receipt.Mark("borrowed-clients.owner-disposed", new { Count = 2 });
            foreach (var daemon in daemons) await daemon.WaitForNormalExitAsync();
            receipt.Completed = true;
        }
        catch (Exception exception)
        {
            receipt.Failure("test.primary", exception);
            throw;
        }
        finally
        {
            foreach (var client in clients)
                try { await client.DisposeAsync(); }
                catch (Exception exception) { receipt.Failure("client.cleanup", exception); }
            foreach (var daemon in daemons) await daemon.CleanupAsync();
            try
            {
                if (Directory.Exists(owned))
                {
                    foreach (var file in Directory.EnumerateFiles(owned, "*", SearchOption.AllDirectories))
                        File.SetAttributes(file, FileAttributes.Normal);
                    Directory.Delete(owned, recursive: true);
                }
                receipt.Mark("fixtures.deleted", new { Exists = Directory.Exists(owned) });
            }
            catch (Exception exception) { receipt.Failure("fixture.cleanup-retained-debt", exception); }
            receipt.Save();
        }
        Assert.True(receipt.Completed && receipt.FailureCount == 0, "Proof and cleanup must both finish; inspect the receipt.");
    }

    private static async Task<AtlasFileNode> FindOwnedFileAsync(AtlasReaderView view)
    {
        for (var page = 0; page < 4 && view.CanLoadMore; ++page)
            await view.LoadMoreAsync();
        Assert.False(view.CanLoadMore, "Owned synthetic inventory must fit the four-page fixture budget.");
        return Assert.Single(Flatten(view.FileRoots), node => node.IsFile
            && (node.Name == "Widget.cs" || node.Name == "src/Widget.cs"));
    }

    private static void VerifySource(AtlasReaderView view, AtlasSelectionDto selection, Receipt receipt, string stage, string sourcePath)
    {
        Assert.Equal(SourceProjectionState.IndexedMatch, selection.Source.State);
        var span = Assert.IsType<AtlasSpanDto>(selection.Source.PageSpan);
        var actualBytes = File.ReadAllBytes(sourcePath);
        var actualSource = Encoding.UTF8.GetString(actualBytes);
        Assert.True(span.Start >= 0 && span.Length > 0 && span.Start + span.Length <= actualSource.Length);
        var expected = actualSource.Substring(span.Start, span.Length);
        Assert.True(selection.Source.Text == expected, "Source projection must match the span in the actual owned file.");
        Assert.True(view.SourceText == expected, "Rendered source must equal the real returned page.");
        Assert.True(view.IsSourceReadOnly);
        Assert.False(string.IsNullOrWhiteSpace(view.BoundsText));
        Assert.False(string.IsNullOrWhiteSpace(view.SourceStatusText));
        Assert.Equal(Encoding.UTF8.GetByteCount(expected), selection.SourceBounds.ReturnedContentBytes);
        receipt.Mark(stage + ".source", new
        {
            selection.Source.State, PageStart = span.Start, PageLength = span.Length,
            ActualFileSha256 = Hash(actualBytes),
            PageUtf8Sha256 = Hash(Encoding.UTF8.GetBytes(expected)),
            RenderedUtf8Sha256 = Hash(Encoding.UTF8.GetBytes(view.SourceText)),
            selection.SourceBounds, selection.OutlineBounds, selection.OutlineState,
            BoundsRendered = view.BoundsText, SourceStateRendered = view.SourceStatusText,
            Disclosures = selection.Disclosures.Length, ReadOnly = view.IsSourceReadOnly,
        });
    }

    private static async Task RunDispatcherAsync(Func<Task> body, Receipt receipt)
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            Exception? failure = null;
            Task? running = null;
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            async Task RunAsync()
            {
                try { await body(); }
                catch (Exception exception) { failure = exception; }
                finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Send); }
            }
            try
            {
                dispatcher.InvokeAsync(() => { running = RunAsync(); });
                Dispatcher.Run();
                Assert.True(running?.IsCompleted == true, "Dispatcher must outlive the owned body and its window cleanup.");
                receipt.Mark("dispatcher.drained", new { BodyCompleted = running.IsCompleted, Apartment = "STA" });
                if (failure is not null) completed.TrySetException(failure);
                else completed.TrySetResult();
            }
            catch (Exception exception) { completed.TrySetException(exception); }
        }) { IsBackground = true, Name = "Owner58-owned-window" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    private static Task IdleAsync() => Dispatcher.CurrentDispatcher.InvokeAsync(
        () => { }, DispatcherPriority.ApplicationIdle).Task;

    private static Task ObserveAutomationAsync(nint hwnd, Receipt receipt,InstanceObserver? observer=null,long batchId=0) => Task.Run(() =>
    {
        Assert.Equal(ApartmentState.MTA, Thread.CurrentThread.GetApartmentState());
        var root = AutomationElement.FromHandle(hwnd);
        Assert.Equal(Environment.ProcessId, root.Current.ProcessId);
        var names = new[]
        {
            "Atlas files", "Atlas member outline", "Atlas source page read-only",
            "Atlas pagination and bounds", "Back to restored Atlas receipt",
        };
        foreach (var name in names)
        {
            var queryId=observer?.NextQuery();
            var queryStartedUtc=DateTimeOffset.UtcNow;
            var queryStarted = Stopwatch.GetTimestamp();
            var element = root.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, name));
            var queryEnded=Stopwatch.GetTimestamp();
            var queryEndedUtc=DateTimeOffset.UtcNow;
            receipt.Mark("uia.find-first.original", new
            {
                ExpectedName = name, OwnHwnd = hwnd.ToInt64(), ExpectedProcessId = Environment.ProcessId,
                OriginalFound = element is not null, ObservedUtc = DateTimeOffset.UtcNow,
                QueryId=queryId,BatchId=batchId,QueryStartedUtc=queryStartedUtc,QueryEndedUtc=queryEndedUtc,
                QueryStartTick=queryStarted,QueryEndTick=queryEnded,
                QueryMilliseconds = Stopwatch.GetElapsedTime(queryStarted).TotalMilliseconds,
                RootBinding = "original AutomationElement.FromHandle; original process assertion passed",
            });
            ObserveOwnedAutomationAfterOriginal(root, hwnd, name, element is not null, receipt,queryId,batchId);
            Assert.NotNull(element);
            Assert.False(element.Current.IsOffscreen, "Named Atlas control must be visible in the proof-owned HWND.");
        }
        receipt.Mark("uia.own-hwnd", new { Hwnd = hwnd.ToInt64(), ProcessId = Environment.ProcessId, Apartment = "MTA", Names = names });
    });

    private static void ObserveOwnedAutomationAfterOriginal(
        AutomationElement root, nint hwnd, string expectedName, bool originalFound, Receipt receipt,long? queryId=null,long batchId=0)
    {
        try
        {
            var hasWindowPattern = root.TryGetCurrentPattern(WindowPattern.Pattern, out var pattern);
            var windowState = hasWindowPattern ? ((WindowPattern)pattern).Current.WindowVisualState.ToString() : null;
            receipt.Mark("uia.owned-root.after-original", new
            {
                ExpectedName = expectedName, OwnHwnd = hwnd.ToInt64(), ExpectedProcessId = Environment.ProcessId,
                OriginalFound = originalFound, WindowPatternRecorded = hasWindowPattern, WindowState = windowState,
                QueryId=queryId,BatchId=batchId,RuntimeIdTiming="after-original-query; same root reference",
                Root = AutomationDiagnosticNode(root, 0, 0, -1), ObservedUtc = DateTimeOffset.UtcNow,
            });
            if (!originalFound) ObserveBoundedOwnedAutomation(root, hwnd, expectedName, receipt);
        }
        catch (Exception error) when (error is ElementNotAvailableException
            or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            receipt.Mark("uia.diagnostic-unavailable", new
            {
                ExpectedName = expectedName, OwnHwnd = hwnd.ToInt64(),
                ExceptionType = error.GetType().FullName, MessageSha256 = Hash(Encoding.UTF8.GetBytes(error.Message)),
                OriginalFound = originalFound, OriginalAssertionStillApplies = true,
            });
        }
    }

    private static int[]? BoundedRuntimeId(int[]? runtimeId,out bool truncated)
    {
        truncated=runtimeId is { Length: >32 };
        return runtimeId is null?null:runtimeId[..Math.Min(runtimeId.Length,32)];
    }

    private static object AutomationDiagnosticNode(AutomationElement element, int ordinal, int depth, int parent)
    {
        const int maxNameCharacters = 160;
        var current = element.Current;
        var name = current.Name ?? "";
        var automationId = current.AutomationId ?? "";
        var rectangle = current.BoundingRectangle;
        var boundsRecorded = !rectangle.IsEmpty && double.IsFinite(rectangle.X) && double.IsFinite(rectangle.Y)
            && double.IsFinite(rectangle.Width) && double.IsFinite(rectangle.Height);
        return new
        {
            Ordinal = ordinal, ParentOrdinal = parent, Depth = depth,
            Name = name[..Math.Min(name.Length, maxNameCharacters)], NameLength = name.Length,
            NameTruncated = name.Length > maxNameCharacters,
            AutomationId = automationId[..Math.Min(automationId.Length, maxNameCharacters)],
            AutomationIdTruncated = automationId.Length > maxNameCharacters,
            current.ProcessId, current.NativeWindowHandle, current.IsOffscreen, current.IsEnabled,
            ControlType = current.ControlType?.ProgrammaticName, RuntimeId = BoundedRuntimeId(element.GetRuntimeId(),out var runtimeIdTruncated),
            RuntimeIdTruncated=runtimeIdTruncated,
            BoundsRecorded = boundsRecorded,
            Bounds = boundsRecorded ? new[] { rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height } : null,
        };
    }

    private static void ObserveBoundedOwnedAutomation(AutomationElement root, nint hwnd, string expectedName, Receipt receipt)
    {
        const int maxNodes = 128;
        const int maxDepth = 12;
        const double betweenCallBudgetMilliseconds = 250;
        var started = Stopwatch.GetTimestamp();
        var queue = new Queue<(AutomationElement Element, int Depth, int Parent)>();
        var nodes = new List<object>();
        queue.Enqueue((root, 0, -1));
        var truncated = false;
        string? unavailable = null;
        try
        {
            while (queue.Count != 0)
            {
                if (nodes.Count >= maxNodes || Stopwatch.GetElapsedTime(started).TotalMilliseconds >= betweenCallBudgetMilliseconds)
                {
                    truncated = true;
                    break;
                }
                var (element, depth, parent) = queue.Dequeue();
                var ordinal = nodes.Count;
                nodes.Add(AutomationDiagnosticNode(element, ordinal, depth, parent));
                if (depth >= maxDepth)
                {
                    truncated = true;
                    continue;
                }
                var child = TreeWalker.RawViewWalker.GetFirstChild(element);
                while (child is not null)
                {
                    if (nodes.Count + queue.Count >= maxNodes
                        || Stopwatch.GetElapsedTime(started).TotalMilliseconds >= betweenCallBudgetMilliseconds)
                    {
                        truncated = true;
                        break;
                    }
                    queue.Enqueue((child, depth + 1, ordinal));
                    child = TreeWalker.RawViewWalker.GetNextSibling(child);
                }
            }
        }
        catch (Exception error) when (error is ElementNotAvailableException
            or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            unavailable = error.GetType().FullName;
            truncated = true;
        }
        receipt.Mark("uia.owned-root.census.after-original", new
        {
            ExpectedName = expectedName, OwnHwnd = hwnd.ToInt64(), OriginalFound = false,
            View = "RawView rooted at the original proof-owned AutomationElement",
            MaxNodes = maxNodes, MaxDepth = maxDepth, BetweenCallBudgetMilliseconds = betweenCallBudgetMilliseconds,
            BudgetIsBetweenCallsNotAnIndividualProviderCallTimeout = true,
            ObservedNodes = nodes.Count, QueuedNotVisited = queue.Count, Truncated = truncated,
            Unavailable = unavailable, ElapsedMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            Nodes = nodes,
        });
    }

    private static double MeasureReading(Window window, AtlasReaderView view, string source,
        IReadOnlyList<AtlasSpanDto> highlights, Receipt receipt, string stage, bool requireReadable)
    {
        var client = Assert.IsAssignableFrom<FrameworkElement>(window.Content);
        var clientBounds = client.TransformToAncestor(window).TransformBounds(new Rect(client.RenderSize));
        var textView = view.SourceControl.TextArea.TextView;
        textView.EnsureVisualLines();
        var viewport = VisibleBounds(textView, window, clientBounds);
        Rect SourceRect(int start, int length)
        {
            var document = view.SourceControl.Document;
            var first = new ICSharpCode.AvalonEdit.TextViewPosition(document.GetLocation(start));
            var last = new ICSharpCode.AvalonEdit.TextViewPosition(document.GetLocation(start + length));
            var top = textView.GetVisualPosition(first, ICSharpCode.AvalonEdit.Rendering.VisualYPosition.LineTop)
                - textView.ScrollOffset;
            var bottom = textView.GetVisualPosition(last, ICSharpCode.AvalonEdit.Rendering.VisualYPosition.LineBottom)
                - textView.ScrollOffset;
            return textView.TransformToAncestor(window).TransformBounds(new Rect(top, bottom));
        }
        var lineBounds = new List<Rect>();
        var offset = 0;
        foreach (var line in source.Split('\n'))
        {
            if (line.Length > 0) lineBounds.Add(SourceRect(offset, line.Length));
            offset += line.Length + 1;
        }
        var highlightBounds = highlights.Select(span => SourceRect(span.Start, span.Length)).ToArray();
        var container = Assert.IsType<ListBoxItem>(
            view.OutlineControl.ItemContainerGenerator.ContainerFromItem(view.OutlineControl.SelectedItem));
        var label = Assert.Single(Visuals<TextBlock>(container),
            block => block.Text.Contains("Answer", StringComparison.Ordinal));
        var drawing = VisualTreeHelper.GetDrawing(label);
        Assert.NotNull(drawing);
        var runs = RenderedLabelRuns(drawing, Matrix.Identity).ToArray();
        var labelGlyphs = runs.Select(run => label.TransformToAncestor(window).TransformBounds(run.Bounds)).ToArray();
        var labelViewport = VisibleBounds(label, window, clientBounds, startAtLayoutBounds: false);
        var selected = Assert.IsType<OutlineRow>(view.OutlineControl.SelectedItem);
        var accessibleText = AutomationProperties.GetName(container);
        var expectedCharacters = new string(label.Text.Where(character => !char.IsWhiteSpace(character)).ToArray());
        var renderedCharacters = new string(string.Concat(runs.Select(run => run.Characters))
            .Where(character => !char.IsWhiteSpace(character)).ToArray());
        var completeCharacters = expectedCharacters == renderedCharacters;
        var completeLabel = selected.ToString() == label.Text && selected.AccessibleName == accessibleText;
        var textFits = !viewport.IsEmpty && lineBounds.Count > 0 && lineBounds.All(viewport.Contains);
        var highlightFits = !viewport.IsEmpty && highlightBounds.Length > 0 && highlightBounds.All(viewport.Contains);
        var labelFits = !labelViewport.IsEmpty && labelGlyphs.Length > 0 && labelGlyphs.All(labelViewport.Contains);
        var clientViewport = viewport;
        if (!clientViewport.IsEmpty) clientViewport.Offset(-clientBounds.X, -clientBounds.Y);
        receipt.Mark(stage + ".reading-geometry", new
        {
            CoordinateUnit = "WPF device-independent pixels", WindowClientBounds = Box(clientBounds),
            SourceViewportWindow = Box(viewport), SourceViewportClient = Box(clientViewport),
            SourceLineRectsWindow = lineBounds.Select(Box).ToArray(),
            HighlightRectsWindow = highlightBounds.Select(Box).ToArray(),
            SelectedLabelGlyphRunsWindow = labelGlyphs.Select(Box).ToArray(),
            SelectedLabelViewportWindow = Box(labelViewport), RenderedRunCount = runs.Length,
            SelectedLabelSha256 = Hash(Encoding.UTF8.GetBytes(label.Text)),
            FullAccessibleTextSha256 = Hash(Encoding.UTF8.GetBytes(accessibleText)),
            ExpectedCharactersSha256 = Hash(Encoding.UTF8.GetBytes(expectedCharacters)),
            RenderedCharactersSha256 = Hash(Encoding.UTF8.GetBytes(renderedCharacters)),
            CompleteGlyphCharacters = completeCharacters, CompleteLabelAndAccessibleText = completeLabel,
            label.TextWrapping, label.TextTrimming,
            label.FontSize, SourceFontSize = view.SourceControl.FontSize,
            SourceScrollX = textView.ScrollOffset.X, SourceScrollY = textView.ScrollOffset.Y,
            AllSourceLinesFit = textFits, HighlightFits = highlightFits, FullSelectedLabelFits = labelFits,
            AtlasWidth = view.ActualWidth, ParentPixelAcceptancePending = true,
        });
        if (requireReadable)
        {
            Assert.True(textFits, "Every source text line must fit the client- and ancestor-clipped source viewport.");
            Assert.True(highlightFits, "The selected identifier highlight must fully fit the visible source viewport.");
            Assert.True(labelFits, "The full selected method label must fit its client- and ancestor-clipped viewport.");
            Assert.True(completeCharacters && completeLabel,
                "All non-whitespace label characters must be drawn and full accessible text preserved.");
            Assert.Equal(TextWrapping.Wrap, label.TextWrapping);
            Assert.Equal(TextTrimming.None, label.TextTrimming);
            Assert.True(double.IsFinite(label.ActualWidth) && label.ActualWidth > 0
                && labelViewport.Width <= view.OutlineControl.ActualWidth,
                "Actual label clipping must stay bounded by the finite outline viewport.");
            Assert.True(labelGlyphs.Select(bounds => bounds.Top).Distinct().Count() > 1,
                "The complete real method label must render on multiple wrapped lines.");
            Assert.All(labelGlyphs, bounds => Assert.True(double.IsFinite(bounds.Width)
                && double.IsFinite(bounds.Height) && bounds.Width > 0 && bounds.Height > 0));
            Assert.True(view.SourceControl.FontSize >= 12 && label.FontSize >= 11,
                "Readability proof must use the real normal-size source and outline text.");
        }
        return view.ActualWidth;
    }

    private static IEnumerable<(Rect Bounds, string Characters, object Facts)> RenderedLabelRuns(Drawing drawing, Matrix parent)
    {
        if (drawing is DrawingGroup group)
        {
            Assert.True(group.Opacity > 0);
            var transform = group.Transform?.Value ?? Matrix.Identity;
            transform.Append(parent);
            foreach (var child in group.Children)
                foreach (var run in RenderedLabelRuns(child, transform))
                {
                    if (group.ClipGeometry is { } clip)
                        Assert.True(new MatrixTransform(transform).TransformBounds(clip.Bounds).Contains(run.Bounds),
                            "A drawing-level clip must not hide any rendered label glyph.");
                    yield return run;
                }
        }
        else if (drawing is GlyphRunDrawing glyph)
        {
            Assert.NotNull(glyph.GlyphRun.Characters);
            yield return (new MatrixTransform(parent).TransformBounds(glyph.Bounds),
                new string(glyph.GlyphRun.Characters.ToArray()),
                new
                {
                    CharacterCodePoints = glyph.GlyphRun.Characters.Select(character => (int)character).ToArray(),
                    ClusterMap = glyph.GlyphRun.ClusterMap?.ToArray(),
                    AdvanceWidths = glyph.GlyphRun.AdvanceWidths.ToArray(),
                    GlyphIndices = glyph.GlyphRun.GlyphIndices.ToArray(),
                    LocalInkBounds = Box(glyph.Bounds),
                    BaselineOrigin = new { glyph.GlyphRun.BaselineOrigin.X, glyph.GlyphRun.BaselineOrigin.Y },
                });
        }
    }

    private static Rect VisibleBounds(FrameworkElement visual, Window window, Rect clientBounds,
        bool startAtLayoutBounds = true)
    {
        var visible = startAtLayoutBounds
            ? visual.TransformToAncestor(window).TransformBounds(new Rect(visual.RenderSize))
            : clientBounds;
        visible.Intersect(clientBounds);
        for (DependencyObject? current = visual; current is not null && !ReferenceEquals(current, window);
             current = VisualTreeHelper.GetParent(current))
        {
            if (current is not UIElement element) continue;
            if (!element.IsVisible || element.Opacity == 0) return Rect.Empty;
            if (element.ClipToBounds)
                visible.Intersect(element.TransformToAncestor(window).TransformBounds(new Rect(element.RenderSize)));
            if (VisualTreeHelper.GetClip(element) is { } clip)
                visible.Intersect(element.TransformToAncestor(window).TransformBounds(clip.Bounds));
            if (visible.IsEmpty) return visible;
        }
        return visible;
    }

    private static object Box(Rect value) => new
    {
        Empty = value.IsEmpty, X = value.IsEmpty ? 0 : value.X, Y = value.IsEmpty ? 0 : value.Y,
        Width = value.IsEmpty ? 0 : value.Width, Height = value.IsEmpty ? 0 : value.Height,
    };

    private static void Capture(Window window, string evidence, Receipt receipt, string name = "mainwindow.png",InstanceObserver? observer=null)
    {
        var width = (int)Math.Ceiling(window.ActualWidth);
        var height = (int)Math.Ceiling(window.ActualHeight);
        Assert.True(width > 0 && height > 0 && window.IsVisible);
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        observer?.Sample(window,"capture-before:"+name,0);
        try { bitmap.Render(window); }
        finally { observer?.Sample(window,"capture-after:"+name,0); }
        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);
        Assert.True(pixels.Where((_, index) => index % 4 == 3).Any(alpha => alpha != 0),
            "The owned-window capture must not be a transparent blank.");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var path = Path.Combine(evidence, name);
        using (var file = File.Create(path)) encoder.Save(file);
        receipt.Mark("capture.window-only", new { Path = path, Width = width, Height = height, Sha256 = Hash(File.ReadAllBytes(path)) });
        observer?.Flush();
    }

    private static ResourceDictionary Resources()
    {
        var app = XDocument.Load(Path.Combine(Tree, "src", "AiDe.App", "App.xaml")).Root!;
        var ns = app.Name.Namespace;
        var dictionary = new XElement(ns + "ResourceDictionary",
            app.Attributes().Where(attribute => attribute.IsNamespaceDeclaration).Select(attribute =>
                new XAttribute(attribute.Name, attribute.Value == "clr-namespace:AiDe.App"
                    ? "clr-namespace:AiDe.App;assembly=AiDe.App" : attribute.Value)),
            app.Element(ns + "Application.Resources")!.Elements());
        return (ResourceDictionary)XamlReader.Parse(dictionary.ToString());
    }

    private static IEnumerable<MenuItem> MenuItems(ItemsControl parent)
    {
        foreach (var item in parent.Items.OfType<MenuItem>())
        {
            yield return item;
            foreach (var child in MenuItems(item)) yield return child;
        }
    }

    private static IEnumerable<T> Visuals<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); ++index)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) yield return match;
            foreach (var nested in Visuals<T>(child)) yield return nested;
        }
    }

    private static IEnumerable<AtlasFileNode> Flatten(IEnumerable<AtlasFileNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in Flatten(node.Children)) yield return child;
        }
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static string RepositoryRoot()
    {
        var assembly = typeof(AtlasDaemonMainWindowProofTests).Assembly.Location;
        for (var directory = new DirectoryInfo(Path.GetDirectoryName(assembly)!);
             directory is not null; directory = directory.Parent)
        {
            var root = directory.FullName;
            if (File.Exists(Path.Combine(root, "tests", "AiDe.App.Tests", "AiDe.App.Tests.csproj"))
                && File.Exists(Path.Combine(root, "src", "AiDe.Daemon", "AiDe.Daemon.csproj"))
                && (Directory.Exists(Path.Combine(root, ".git")) || File.Exists(Path.Combine(root, ".git"))))
                return root;
        }
        throw new InvalidOperationException("The executing proof assembly is not beneath a product repository.");
    }

    private static void VerifyFooter(Window window, MainWindowViewModel model, Receipt receipt, string name)
    {
        var client = Assert.IsAssignableFrom<FrameworkElement>(window.Content);
        var clientBounds = client.TransformToAncestor(window).TransformBounds(new Rect(client.RenderSize));
        var matches = Visuals<TextBlock>(window).Where(block => block.IsVisible
            && AutomationProperties.GetName(block) == "Workspace health"
            && System.Windows.Data.BindingOperations.GetBinding(block, TextBlock.TextProperty)?.Path?.Path
                == nameof(MainWindowViewModel.StatusMessage)
            && !VisibleBounds(block, window, clientBounds).IsEmpty).ToArray();
        receipt.Mark(name + ".footer-binding", new
        {
            ComputedStatus = model.StatusMessage, VisibleMatchingControls = matches.Length,
            RenderedTexts = matches.Select(block => block.Text).ToArray(),
            Selector = "owned MainWindow; automation name Workspace health; Text binding path StatusMessage",
        });
        Assert.False(string.IsNullOrWhiteSpace(model.StatusMessage)
            || model.StatusMessage.Contains("No workspace open", StringComparison.OrdinalIgnoreCase),
            "Legitimate remote Refresh must replace the unopened-workspace default.");
        var footer = Assert.Single(matches);
        var expression = System.Windows.Data.BindingOperations.GetBindingExpression(footer, TextBlock.TextProperty);
        var ownerMatches = ReferenceEquals(expression?.DataItem, model)
            && ReferenceEquals(window.DataContext, model);
        receipt.Mark(name + ".footer-owner", new
        {
            AutomationName = AutomationProperties.GetName(footer),
            BindingPath = expression?.ParentBinding.Path?.Path,
            BindingOwnerIsActiveVm = ownerMatches, DisplayEqualsRefreshedVm = footer.Text == model.StatusMessage,
        });
        Assert.True(ownerMatches, "The named footer binding must resolve to the active real remote VM.");
        Assert.True(footer.Text == model.StatusMessage, "The named footer must display its legitimately refreshed VM status.");
        var drawing = VisualTreeHelper.GetDrawing(footer);
        Assert.NotNull(drawing);
        var runs = RenderedLabelRuns(drawing, Matrix.Identity).ToArray();
        var bounds = runs.Select(run => footer.TransformToAncestor(window).TransformBounds(run.Bounds)).ToArray();
        var viewport = VisibleBounds(footer, window, clientBounds, startAtLayoutBounds: false);
        receipt.Mark(name + ".footer-glyph-characterization", new
        {
            EmptyInkPolicy = "Only nonempty U+0020-only runs; characterized before predicate change",
            Runs = runs.Select((run, index) => new
            {
                Index = index, run.Characters, run.Facts, WindowInkBounds = Box(bounds[index]),
            }).ToArray(),
        });
        var expected = new string(model.StatusMessage.Where(character => !char.IsWhiteSpace(character)).ToArray());
        var actual = new string(string.Concat(runs.Select(run => run.Characters))
            .Where(character => !char.IsWhiteSpace(character)).ToArray());
        var ink = runs.Select((run, index) => new InkRun(bounds[index], run.Characters)).ToArray();
        var fits = FooterInkFits(ink, viewport);
        receipt.Mark(name + ".footer-rendered", new
        {
            Text = footer.Text, CompleteGlyphCharacters = expected == actual, AllGlyphsFit = fits,
            ViewportWindow = Box(viewport), GlyphRunsWindow = bounds.Select(Box).ToArray(),
            AdmittedNoInkCodePoints = ink.Where(IsCharacterizedNoInk)
                .Select(run => run.Characters.Select(character => (int)character).ToArray()).ToArray(),
        });
        Assert.True(FooterAccepted(model.StatusMessage, ink, viewport),
            "Every required footer character and every real ink rectangle must survive actual clipping.");
        VerifyFooterNegativeControls(model.StatusMessage, ink, viewport, receipt, name);
    }

    private sealed record InkRun(Rect Bounds, string Characters);

    private static bool IsCharacterizedNoInk(InkRun run) =>
        run.Bounds.IsEmpty && run.Characters.Length > 0
        && run.Characters.All(character => character == ' ');

    private static bool FooterInkFits(IReadOnlyList<InkRun> runs, Rect viewport) =>
        !viewport.IsEmpty && runs.Any(run => !run.Bounds.IsEmpty)
        && runs.All(run => IsCharacterizedNoInk(run)
            || (!run.Bounds.IsEmpty && double.IsFinite(run.Bounds.X) && double.IsFinite(run.Bounds.Y)
                && double.IsFinite(run.Bounds.Width) && double.IsFinite(run.Bounds.Height)
                && run.Bounds.Width > 0 && run.Bounds.Height > 0 && viewport.Contains(run.Bounds)));

    private static string RequiredCharacters(string text) =>
        new(text.Where(character => !char.IsWhiteSpace(character)).ToArray());

    private static bool FooterAccepted(string text, IReadOnlyList<InkRun> runs, Rect viewport) =>
        RequiredCharacters(text) == RequiredCharacters(string.Concat(runs.Select(run => run.Characters)))
        && FooterInkFits(runs, viewport);

    private static void VerifyFooterNegativeControls(string text, InkRun[] measured, Rect viewport,
        Receipt receipt, string name)
    {
        var requiredRun = Array.FindIndex(measured, run => RequiredCharacters(run.Characters).Length > 0);
        Assert.True(requiredRun >= 0);
        var characterIndex = Array.FindIndex(measured[requiredRun].Characters.ToCharArray(),
            character => !char.IsWhiteSpace(character));
        var missing = measured.ToArray();
        missing[requiredRun] = missing[requiredRun] with
        {
            Characters = missing[requiredRun].Characters.Remove(characterIndex, 1),
        };
        var missingInkStillFits = FooterInkFits(missing, viewport);
        var missingRejected = !FooterAccepted(text, missing, viewport);

        var invisibleRequired = measured.ToArray();
        invisibleRequired[requiredRun] = invisibleRequired[requiredRun] with { Bounds = Rect.Empty };
        var invisibleRequiredRejected = !FooterAccepted(text, invisibleRequired, viewport);
        var otherInkStillFits = invisibleRequired.Where((_, index) => index != requiredRun)
            .All(run => IsCharacterizedNoInk(run) || viewport.Contains(run.Bounds));

        var rightmost = Enumerable.Range(0, measured.Length).Where(index => !measured[index].Bounds.IsEmpty)
            .OrderByDescending(index => measured[index].Bounds.Right).First();
        var last = measured[rightmost].Bounds;
        var clipped = new Rect(viewport.X, viewport.Y,
            last.Right - Math.Min(1, last.Width / 2) - viewport.Left, viewport.Height);
        var remainingInkFits = measured.Where((_, index) => index != rightmost)
            .All(run => IsCharacterizedNoInk(run) || clipped.Contains(run.Bounds));
        var clippingRejected = !FooterAccepted(text, measured, clipped);
        receipt.Mark(name + ".footer-negative-controls", new
        {
            InputKind = "copies of measured glyph evidence; no runtime, query, binding or model mutation",
            RemovedRequiredCodePoint = (int)measured[requiredRun].Characters[characterIndex],
            MissingCharacterRejected = missingRejected, MissingCharacterInkStillFits = missingInkStillFits,
            NonWhitespaceEmptyInkRejected = invisibleRequiredRejected, OtherInkStillFits = otherInkStillFits,
            RealInkClipRejected = clippingRejected, RemainingUnclippedInkFits = remainingInkFits,
            ClippedRunIndex = rightmost, OriginalRealInk = Box(last), NegativeViewport = Box(clipped),
        });
        Assert.True(missingInkStillFits && missingRejected,
            "Removing a required character must fail even when the unchanged ink rectangles fit.");
        Assert.True(otherInkStillFits && invisibleRequiredRejected,
            "An empty rectangle carrying required characters must fail; empty ink is not automatically whitespace.");
        Assert.True(remainingInkFits && clippingRejected,
            "Clipping real ink must fail even when every other ink rectangle fits and all characters remain.");
    }

    private static Task<string> GitAsync(string root, params string[] arguments) =>
        CommandAsync(Git, root, ["-c", "user.name=Atlas Proof", "-c", "user.email=atlas-proof@example.invalid", .. arguments]);

    private static async Task<string> CommandAsync(string binary, string directory, params string[] arguments)
    {
        var start = new ProcessStartInfo(binary)
        {
            WorkingDirectory = directory, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["GIT_CONFIG_GLOBAL"] = "NUL";
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await process.WaitForExitAsync(timeout.Token); }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
        await Task.WhenAll(stdout, stderr);
        Assert.True(process.ExitCode == 0, $"Owned command exit={process.ExitCode}; stderr SHA256={Hash(Encoding.UTF8.GetBytes(await stderr))}");
        return await stdout;
    }

    private sealed class ObservedReader(IAtlasWorkspaceReader inner, Receipt receipt, string name, InstanceObserver? observer=null) : IAtlasWorkspaceReader
    {
        internal IAtlasWorkspaceReader InnerForObservation => inner;
        internal ObservedLease? Lease { get; private set; }
        internal bool Disposed { get; private set; }
        public async ValueTask<IAtlasReaderLease> AdmitAsync(CancellationToken cancellationToken)
        {
            var actual = await inner.AdmitAsync(cancellationToken);
            Lease = new ObservedLease(actual, receipt, name, observer);
            observer?.Retain(this,Lease);
            receipt.Mark(name + ".real-admit", new { actual.CoreEpoch, actual.IsTerminal });
            return Lease;
        }
        public async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync();
            Disposed = true;
            receipt.Mark(name + ".reader-disposed", new { Completed = true });
        }
    }

    private sealed class ObservedLease(IAtlasReaderLease inner, Receipt receipt, string name, InstanceObserver? observer=null) : IAtlasReaderLease, IAtlasReaderQueries
    {
        public string ScopeToken => inner.ScopeToken;
        public string InitialManifestToken => inner.InitialManifestToken;
        public long CoreEpoch => inner.CoreEpoch;
        public DateTimeOffset ExpiresAt => inner.ExpiresAt;
        public IAtlasReaderQueries Queries => this;
        public CancellationToken Invalidated => inner.Invalidated;
        public bool IsTerminal => inner.IsTerminal;
        internal AtlasSelectionDto? LastSelection { get; private set; }
        internal bool HealthyAtRelease { get; private set; }
        internal bool ReleaseCompleted { get; private set; }
        public ValueTask<AtlasInventoryPageDto> InventoryAsync(AtlasInventoryRequestDto request, CancellationToken cancellationToken) =>
            inner.Queries.InventoryAsync(request, cancellationToken);
        public async ValueTask<AtlasSelectionDto> SelectAsync(AtlasSelectRequestDto request, CancellationToken cancellationToken)
        {
            var result = await inner.Queries.SelectAsync(request, cancellationToken);
            LastSelection = result;
            return result;
        }
        public async ValueTask<AtlasSelectionDto> RestoreAsync(AtlasRestoreRequestDto request, CancellationToken cancellationToken)
        {
            var result = await inner.Queries.RestoreAsync(request, cancellationToken);
            LastSelection = result;
            return result;
        }
        public async ValueTask DisposeAsync()
        {
            HealthyAtRelease = !inner.IsTerminal && !inner.Invalidated.IsCancellationRequested;
            receipt.Mark(name + ".lease-release-start", new { HealthyAtRelease });
            observer?.ReleaseStarted(this);
            await inner.DisposeAsync();
            ReleaseCompleted = true;
            receipt.Mark(name + ".lease-release-returned", new { HealthyAtRelease, NormalReturn = true, inner.IsTerminal });
        }
    }

    private sealed class Daemon(Process process, Task<string> stdout, Task<string> stderr, Receipt receipt, string name)
    {
        internal int Pid => process.Id;
        internal static Daemon Start(string binary, string repository, Receipt receipt, string name)
        {
            var directory = Path.GetDirectoryName(repository)!;
            var config = Path.Combine(directory, "config");
            Directory.CreateDirectory(config);
            var start = new ProcessStartInfo(binary)
            {
                WorkingDirectory = Path.GetDirectoryName(binary)!, UseShellExecute = false,
                CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
            };
            foreach (var argument in new[] { repository, "--data", Path.Combine(directory, "data"),
                "--idle-seconds", "1", "--startup-seconds", "20" })
                start.ArgumentList.Add(argument);
            foreach (var variable in new[] { "HOME", "USERPROFILE", "LOCALAPPDATA", "APPDATA", "XDG_CONFIG_HOME" })
                start.Environment[variable] = config;
            start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
            start.Environment["GIT_CONFIG_GLOBAL"] = "NUL";
            var process = Process.Start(start)!;
            receipt.Mark(name + ".daemon-start", new { process.Id, Binary = binary, OwnedRepository = repository });
            return new Daemon(process, process.StandardOutput.ReadToEndAsync(), process.StandardError.ReadToEndAsync(), receipt, name);
        }
        internal async Task WaitForNormalExitAsync()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await process.WaitForExitAsync(timeout.Token);
            Assert.Equal(0, process.ExitCode);
            Assert.True((await stdout).Contains("listening ", StringComparison.Ordinal));
            Assert.True(string.IsNullOrWhiteSpace(await stderr), "Inspect daemon stderr hash in the receipt.");
            receipt.Mark(name + ".daemon-normal-exit", new { process.Id, process.ExitCode, ListeningObserved = true });
        }
        internal async Task CleanupAsync()
        {
            try
            {
                var forced = !process.HasExited;
                if (forced)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync();
                    receipt.Failure(name + ".daemon-forced-cleanup", new InvalidOperationException("Daemon required owned-PID termination."));
                }
                var output = await stdout;
                var error = await stderr;
                receipt.Mark(name + ".daemon-reaped", new
                {
                    process.Id, process.ExitCode, Forced = forced,
                    ListeningObserved = output.Contains("listening ", StringComparison.Ordinal),
                    StdoutSha256 = Hash(Encoding.UTF8.GetBytes(output)),
                    StderrSha256 = Hash(Encoding.UTF8.GetBytes(error)),
                    StderrBytes = Encoding.UTF8.GetByteCount(error),
                });
                process.Dispose();
            }
            catch (Exception exception) { receipt.Failure(name + ".daemon-cleanup-retained-debt", exception); }
        }
    }

    private sealed class Receipt(string directory,System.Text.Json.Serialization.JsonConverter? observerConverterForControl=null)
    {
        internal string DirectoryPath => directory;
        private readonly List<object> _events = [];
        private readonly object _gate = new();
        private readonly long _started = Stopwatch.GetTimestamp();
        internal bool Completed { get; set; }
        internal int FailureCount { get; private set; }
        internal void Mark(string stage, object attributes)
        {
            lock (_gate)
            {
                _events.Add(new { Stage = stage, ElapsedMilliseconds = Stopwatch.GetElapsedTime(_started).TotalMilliseconds, Attributes = attributes });
                Save();
            }
        }
        internal void MarkObservation(string stage,object attributes)
        {
            // Freeze observer data before shared event mutation; later saves never revisit its formatter.
            var snapshot=JsonSerializer.SerializeToElement(attributes,SerializationOptions());
            Mark(stage,snapshot);
        }
        internal void Failure(string stage, Exception exception)
        {
            lock (_gate)
            {
                ++FailureCount;
                Mark(stage, new { ExceptionType = exception.GetType().FullName, exception.StackTrace,
                    MessageSha256 = Hash(Encoding.UTF8.GetBytes(exception.Message)) });
            }
        }
        private JsonSerializerOptions SerializationOptions()
        {
            var options=new JsonSerializerOptions { WriteIndented=true };
            if(observerConverterForControl is not null)options.Converters.Add(observerConverterForControl);
            return options;
        }
        internal void Save()
        {
            lock (_gate)
                File.WriteAllText(Path.Combine(directory, "receipt.json"), JsonSerializer.Serialize(new
                {
                    Completed = Completed && FailureCount == 0, FailureCount,
                    SameLiveScopeServerIdleBarrier = "NOT ESTABLISHED; in-process idle-Git evidence is separate",
                    DeliberatelyHeldLatePublicationRace = "NOT EXERCISED; drained replacement and persistent clearing asserted",
                    DefaultPlacement = "Reviewed normal Center placement; actual pixel acceptance belongs to parent",
                    Events = _events,
                }, SerializationOptions()));
        }
    }
}
