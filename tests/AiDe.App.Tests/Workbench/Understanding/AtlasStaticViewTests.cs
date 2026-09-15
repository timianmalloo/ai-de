using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AiDe.App.Workbench.Understanding;
using AiDe.Core.Understanding;

namespace AiDe.App.Tests;

[Collection("Atlas E1 native UI")]
public sealed class AtlasStaticViewTests
{
    [Fact]
    public void Projection_DuplicateNamesRemainOccurrencesWithDirectParentOnly()
    {
        var rows = new[] { Class("c2", 10), Member("m2", "c2", 12), Class("c1", 0), Member("m1", "c1", 2) };
        var projection = AtlasStaticViewProjection.Create(Selection(rows), "c2");
        Assert.Equal(["c1", "m1", "c2", "m2"], projection.Occurrences.Select(row => row.Token));
        Assert.Equal(2, projection.Classifiers.Count);
        Assert.Equal("m2", Assert.Single(projection.Members).Token);
        Assert.True(projection.Classifier!.IsClass);
    }

    [Theory]
    [InlineData(AtlasClassifierFlavor.Class, true)]
    [InlineData(AtlasClassifierFlavor.RecordClass, true)]
    [InlineData(AtlasClassifierFlavor.Struct, false)]
    [InlineData(AtlasClassifierFlavor.RecordStruct, false)]
    [InlineData(AtlasClassifierFlavor.Interface, false)]
    [InlineData(AtlasClassifierFlavor.Enum, false)]
    public void Projection_OnlyAdmittedClassFlavorsBecomeUmlClasses(AtlasClassifierFlavor flavor, bool isClass)
    {
        var row = Class("c", 0) with { Structure = new(flavor, AtlasLexicalParentState.NotApplicable, null,
            AtlasStructureProvenance.Extracted, null) };
        Assert.Equal(isClass, AtlasStaticViewProjection.Create(Selection([row])).Classifier!.IsClass);
    }

    [Fact]
    public Task NativeHardStatesAndLargerText_KeepFullLabelsAndDisableStaleActions() => AtlasStaticTestHost.RunAsync(async () =>
    {
        var directory = Path.Combine(AtlasStaticCompositionTests.RepositoryRoot(), ".artifacts", "atlas-e1", $"states-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var evidence = new AtlasStaticCompositionTests.Evidence(directory);
        var view = new AtlasStaticView { FontSize = 15 };
        var window = new Window
        {
            Content = view, Resources = AtlasStaticCompositionTests.Resources(AtlasStaticCompositionTests.RepositoryRoot(), false),
            Width = 600, Height = 800, Left = 40, Top = 40, ShowInTaskbar = false,
        };
        try
        {
            window.Show();
            var rows = new[]
            {
                Class("long", 0) with { DisplayName = "LongClassifierWithGenericLikeParameters<TFirst, TSecond>" },
                Member("member", "long", 1) with { DisplayName = "LongMemberWithParameters(System.String argument, System.Int32 count)" },
            };
            view.Render(AtlasStaticViewProjection.Create(Selection(rows)));
            await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            var label = Assert.IsType<TextBlock>(Assert.Single(view.CompartmentButtons).Content);
            Assert.Equal(15, label.FontSize);
            AtlasStaticCompositionTests.CheckLabel(window, label, evidence, "local-text-size-15-not-OS-scaling");
            AtlasStaticCompositionTests.Capture(window, directory, "long-labels-15dip", evidence);
            foreach (var state in new[]
            {
                "Select a file to inspect its Class view.", "Loading Class view…",
                "Structure unavailable. This reader returned no admitted structural metadata.",
                "Source changed — refresh required", "Source access refused", "Source request canceled",
            })
            {
                view.ShowState(state);
                await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                Assert.Equal(state, view.StatusText);
                Assert.Null(view.Projection);
                Assert.Empty(view.CompartmentButtons);
                Assert.False(view.DeclarationList.IsEnabled);
                evidence.Mark("hard-state", new { state, Disabled = true });
            }
            AtlasStaticCompositionTests.Capture(window, directory, "canceled", evidence);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Projection_OffPageParentIsNotBorrowedFromPreviousPage()
    {
        var previous = AtlasStaticViewProjection.Create(Selection([Class("c", 0), Member("m", "c", 2)]));
        var outside = Member("far", "c", 40000) with
        {
            Structure = new(null, AtlasLexicalParentState.OutsidePage, null, AtlasStructureProvenance.Extracted, "parent outside page"),
        };
        var next = AtlasStaticViewProjection.Create(Selection([outside]), previous.Classifier!.Token);
        Assert.Null(next.Classifier);
        Assert.Empty(next.Members);
        Assert.Contains("No classifier on this page", next.Status);
        Assert.Contains("OutsidePage", Assert.Single(next.Occurrences).AccessibleName);
    }

    [Fact]
    public void Projection_LegacyTypeKindDoesNotInventStructure()
    {
        var projection = AtlasStaticViewProjection.Create(Selection([Class("legacy", 0) with { Structure = null }]));
        Assert.Empty(projection.Classifiers);
        Assert.Contains("Structure unavailable", projection.Status);
        Assert.Single(projection.Occurrences);
    }

    [Fact]
    public void Projection_CycleFailsInsteadOfLoopingOrDrawing()
    {
        var a = Class("a", 0) with { Structure = new(AtlasClassifierFlavor.Class, AtlasLexicalParentState.Present,
            "b", AtlasStructureProvenance.Extracted, null) };
        var b = Class("b", 10) with { Structure = new(AtlasClassifierFlavor.Class, AtlasLexicalParentState.Present,
            "a", AtlasStructureProvenance.Extracted, null) };
        Assert.Throws<ArgumentException>(() => AtlasStaticViewProjection.Create(Selection([a, b])));
    }

    [Theory]
    [InlineData(SourceProjectionState.Changed)]
    [InlineData(SourceProjectionState.Refused)]
    [InlineData(SourceProjectionState.Canceled)]
    [InlineData(SourceProjectionState.Unavailable)]
    [InlineData(SourceProjectionState.UnsupportedEncoding)]
    [InlineData(SourceProjectionState.Unverifiable)]
    [InlineData(SourceProjectionState.TooLargeToVerify)]
    [InlineData(SourceProjectionState.ReadUnstable)]
    public void Projection_NonCurrentSourceDisablesNavigation(SourceProjectionState state)
    {
        var selection = Selection([Class("c", 0)]);
        var projection = AtlasStaticViewProjection.Create(selection with
        {
            Source = selection.Source with { State = state, Text = null, BindingToken = null, PageSpan = null, Highlights = [] },
        });
        Assert.False(projection.CanNavigate);
        Assert.NotEqual("Class occurrence — extracted lexical declarations.", projection.Status);
    }

    [Fact]
    public Task ClassRequestAndBack_KeepPresentationAndOriginalReceipt() => AtlasStaticTestHost.RunAsync(async () =>
    {
        var port = new AtlasStaticPort();
        port.Select = request => AtlasStaticPort.Selection(request,
            request.StaticStructure == true ? [Class("c", 0), Member("m", "c", 1)] :
                [Class("c", 0) with { Structure = null }, Member("m", "c", 1) with { Structure = null }]);
        var reader = new AtlasReaderView(port);
        await reader.LoadAsync();
        await reader.SelectFileAsync(Assert.Single(reader.FileRoots));
        var sourceReceipt = reader.CurrentSelection!.ReceiptToken;
        await reader.SetPresentationAsync(AtlasPresentationMode.Class);
        Assert.True(port.LastRequest!.StaticStructure);
        Assert.Equal("c", reader.StaticView.Projection!.Classifier!.Token);
        await reader.GoBackAsync();
        Assert.Equal(AtlasPresentationMode.Source, reader.Presentation);
        Assert.Equal(sourceReceipt, reader.CurrentSelection!.ReceiptToken);
        Assert.Null(reader.CurrentSelection.Outline[0].Structure);
    });

    internal static AtlasOutlineRowDto Class(string token, int start) =>
        new(token, "Duplicate", AtlasDeclarationKind.Type, new(start, 3),
            new(AtlasClassifierFlavor.Class, AtlasLexicalParentState.NotApplicable, null, AtlasStructureProvenance.Extracted, null));
    internal static AtlasOutlineRowDto Member(string token, string parent, int start) =>
        new(token, "Member", AtlasDeclarationKind.Method, new(start, 3),
            new(null, AtlasLexicalParentState.Present, parent, AtlasStructureProvenance.Extracted, null));
    private static AtlasSelectionDto Selection(AtlasOutlineRowDto[] rows) =>
        AtlasStaticPort.Selection(new(1, "scope", 1, "manifest", "file", null, 0, 3, 0, 128, true), rows);

    [Fact]
    public Task ClassAction_IsPresentOnExistingReader() => AtlasStaticTestHost.RunAsync(() =>
    {
        var reader = new AtlasReaderView(new AtlasStaticPort());
        Assert.Single(LogicalButtons(reader), button => Equals(button.Content, "Class view"));
        return Task.CompletedTask;
    });

    [Fact]
    public Task PagingActions_ArePresentWithoutReplacingFileTree() => AtlasStaticTestHost.RunAsync(() =>
    {
        var reader = new AtlasReaderView(new AtlasStaticPort());
        Assert.Single(LogicalButtons(reader), button => Equals(button.Content, "Next declarations"));
        Assert.Single(LogicalButtons(reader), button => Equals(button.Content, "Next source page"));
        Assert.IsType<TreeView>(reader.FilesControl);
        return Task.CompletedTask;
    });

    [Fact]
    public Task FarMember_UsesIssuedGlobalUtf16Window() => AtlasStaticTestHost.RunAsync(async () =>
    {
        var port = new AtlasStaticPort();
        var reader = new AtlasReaderView(port);
        await reader.SelectDeclarationAsync(new OutlineRow("file", AtlasStaticPort.FarRow));

        Assert.NotNull(port.LastRequest);
        Assert.Equal(40000, port.LastRequest.SourceOffset);
        Assert.Equal(3, port.LastRequest.SourceLength);
        Assert.Null(port.LastRequest.StaticStructure);
    });

    [Fact]
    public Task Invocation_ProviderReturnedBeforeClick_RemainsPending() => AtlasStaticTestHost.RunAsync(async () =>
    {
        var button = new Button();
        var providerReturned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = AtlasStaticCompositionTests.InvokeAndObserveClickAsync(button, () =>
        {
            providerReturned.SetResult();
            return Task.CompletedTask;
        });
        await providerReturned.Task;

        try { Assert.False(operation.IsCompleted, "Provider return is not routed-click completion."); }
        finally { button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); }
        await operation;
    });

    [Fact]
    public Task RetainedActions_AfterInvalidation_DoNotRequestOrResurrectSource() => AtlasStaticTestHost.RunAsync(async () =>
    {
        var requests = 0;
        var port = new AtlasStaticPort
        {
            Select = request =>
            {
                requests++;
                return AtlasStaticPort.Selection(request, [Class("c", 0), Member("m", "c", 1)]);
            },
        };
        var reader = new AtlasReaderView(port);
        var window = new Window { Content = reader, Width = 1180, Height = 900, ShowInTaskbar = false };
        try
        {
            window.Show();
            await reader.LoadAsync();
            await reader.SelectFileAsync(Assert.Single(reader.FileRoots));
            await reader.SetPresentationAsync(AtlasPresentationMode.Class);
            var oldButton = Assert.Single(reader.StaticView.CompartmentButtons);
            var oldList = reader.StaticView.DeclarationList;
            var oldRow = reader.StaticView.Projection!.Occurrences.Single(row => row.Token == "m");
            reader.StaticView.Presentations.SelectedIndex = 1;
            oldList.SelectedItem = oldRow;
            await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            var inputSource = PresentationSource.FromVisual(oldList);
            var requestCount = requests;
            Assert.NotEmpty(reader.SourceText);
            Assert.NotNull(reader.CurrentBindingToken);
            Assert.NotEmpty(reader.CurrentHighlights);

            port.Lifetime.Cancel();
            window.Content = null;
            await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            // Retained routed actions deliberately bypass disabled-control input filtering.
            oldButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            oldList.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, inputSource, 0, Key.Enter)
                { RoutedEvent = Keyboard.KeyDownEvent });
            await reader.CurrentOperation;
            await Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            Assert.Equal(requestCount, requests);
            Assert.Empty(reader.SourceText);
            Assert.Null(reader.CurrentBindingToken);
            Assert.Empty(reader.CurrentHighlights);
            Assert.Null(reader.CurrentSelection);
        }
        finally { window.Close(); await port.DisposeAsync(); }
    });

    private static IEnumerable<Button> LogicalButtons(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if (child is Button button) yield return button;
            foreach (var nested in LogicalButtons(child)) yield return nested;
        }
    }
}

// Synthetic public-port request fixture, paired with the real-pipe composition tests.
internal sealed class AtlasStaticPort : IAtlasReaderLease, IAtlasReaderQueries
{
    internal static AtlasOutlineRowDto FarRow { get; } =
        new("far", "Far", AtlasDeclarationKind.Method, new(40000, 3));
    internal AtlasSelectRequestDto? LastRequest { get; private set; }
    internal Func<AtlasSelectRequestDto, AtlasSelectionDto>? Select { get; set; }
    internal Dictionary<string, AtlasSelectionDto> Receipts { get; } = new(StringComparer.Ordinal);
    internal CancellationTokenSource Lifetime { get; } = new();
    public string ScopeToken => "scope";
    public string InitialManifestToken => "manifest";
    public long CoreEpoch => 1;
    public DateTimeOffset ExpiresAt => DateTimeOffset.MaxValue;
    public IAtlasReaderQueries Queries => this;
    public CancellationToken Invalidated => Lifetime.Token;
    public bool IsTerminal => Lifetime.IsCancellationRequested;
    public ValueTask DisposeAsync()
    {
        Lifetime.Cancel();
        return ValueTask.CompletedTask;
    }

    public ValueTask<AtlasInventoryPageDto> InventoryAsync(AtlasInventoryRequestDto request, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new AtlasInventoryPageDto(1, ScopeToken, CoreEpoch, InitialManifestToken, default,
            [new("file", AtlasDirectoryEntryKind.File, null, "Widget.cs", default, default,
                new(AtlasDenominatorState.Unknown, null, "not established"), null)],
            Bounds(AtlasBoundsDimension.InventoryRows, 1), null, []));

    public ValueTask<AtlasSelectionDto> SelectAsync(AtlasSelectRequestDto request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        var result = Select?.Invoke(request) ?? Selection(request, [FarRow]);
        Receipts[result.ReceiptToken!] = result;
        return ValueTask.FromResult(result);
    }

    public ValueTask<AtlasSelectionDto> RestoreAsync(AtlasRestoreRequestDto request, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Receipts[request.ReceiptToken]);

    internal static AtlasSelectionDto Selection(AtlasSelectRequestDto request, AtlasOutlineRowDto[] rows,
        SourceProjectionState state = SourceProjectionState.IndexedMatch) =>
        new(1, "scope", 1, "manifest", "file", request.DeclarationToken,
            $"receipt-{request.DeclarationToken}-{request.OutlineOffset}-{request.StaticStructure}",
            new(state, "observation", state == SourceProjectionState.IndexedMatch ? "binding" : null, "utf8",
                state == SourceProjectionState.IndexedMatch ? "Far" : null,
                state == SourceProjectionState.IndexedMatch ? new(request.SourceOffset, 3) : null,
                state == SourceProjectionState.IndexedMatch ? [new(request.SourceOffset, 3)] : [], null, null),
            Bounds(AtlasBoundsDimension.SourceUtf16CodeUnits, 1), AtlasOutlineState.Available, null, rows,
            Bounds(AtlasBoundsDimension.OutlineRows, rows.Length), null,
            new(AtlasDenominatorState.Unknown, null, "not established"), []);

    internal static AtlasBoundsDto Bounds(AtlasBoundsDimension dimension, int rows) =>
        new(dimension, 128, 128, rows, dimension == AtlasBoundsDimension.SourceUtf16CodeUnits ? 3 : 0,
            AtlasDenominatorState.Unknown, null, "not established", "More declarations outside this page.", null);
}

internal static class AtlasStaticTestHost
{
    internal static async Task RunAsync(Func<Task> body)
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            Exception? failure = null;
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.InvokeAsync(async () =>
            {
                try { await body(); }
                catch (Exception exception) { failure = exception; }
                finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Send); }
            });
            Dispatcher.Run();
            if (failure is null) completed.SetResult();
            else completed.SetException(failure);
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }
}
