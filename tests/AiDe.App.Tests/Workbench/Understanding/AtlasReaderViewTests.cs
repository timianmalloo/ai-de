using System.Windows.Automation;
using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench.Understanding;
using AiDe.Core.Understanding;

namespace AiDe.App.Tests;

public sealed class AtlasReaderViewTests
{
    [Fact]
    public void NativeControlsExposeNamesFocusReadOnlyAndResourceBindings()
    {
        Sta.Run(() =>
        {
            var view = new AtlasReaderView(new FakeAtlasQueries(), "manifest:1");

            Assert.Equal("Back to restored Atlas receipt", AutomationProperties.GetName(view.BackButton));
            Assert.Equal("Atlas files", AutomationProperties.GetName(view.FilesControl));
            Assert.Equal("Atlas member outline", AutomationProperties.GetName(view.OutlineControl));
            Assert.Equal("Atlas source page read-only", AutomationProperties.GetName(view.SourceControl));
            Assert.True(view.SourceControl.Focusable);
            Assert.True(view.FilesControl.Focusable);
            Assert.True(view.OutlineControl.Focusable);
            Assert.True(view.IsSourceReadOnly);
            Assert.NotEqual(DependencyProperty.UnsetValue, view.SourceControl.ReadLocalValue(Control.BackgroundProperty));
            Assert.NotEqual(DependencyProperty.UnsetValue, view.SourceControl.ReadLocalValue(Control.ForegroundProperty));
        });
    }

    [Fact]
    public async Task LoadingEmptyPartialAndUnknownTotalsNeverInventZero()
    {
        await StaAsync(async () =>
        {
            var queries = new FakeAtlasQueries
            {
                Inventory = _ => Task.FromResult(new InventoryPage(new PageRequest(0, AtlasReaderView.PageSize), BoundsUnknown(0, "membership unavailable"), [])),
            };
            var empty = new AtlasReaderView(queries, "manifest:1");
            await empty.LoadAsync();
            Assert.Contains("No visible files", empty.StatusText);
            Assert.Contains("total not recorded", empty.BoundsText);
            Assert.DoesNotContain("of 0", empty.BoundsText);

            queries.Inventory = request => Task.FromResult(new InventoryPage(request, new AtlasBounds(request.Limit, request.Limit, 1, 100, null, AtlasDenominatorState.Unknown, "bounded walk", "page"), [File("src\\A.cs", "file:a")]));
            var partial = new AtlasReaderView(queries, "manifest:1");
            await partial.LoadAsync();
            Assert.True(partial.CanLoadMore);
            Assert.Contains("total not recorded", partial.BoundsText);
            Assert.Contains("src", partial.FileRoots[0].Name);
        });
    }

    [Fact]
    public async Task PageLoadedBoundsStayVisible()
    {
        await StaAsync(async () =>
        {
            var queries = new FakeAtlasQueries
            {
                Inventory = request => Task.FromResult(new InventoryPage(request, BoundsKnown(1, 3), [File("src\\LongFileNameThatMustRemainInspectable.cs", "file:a")])),
            };
            var view = new AtlasReaderView(queries, "manifest:1");

            await view.LoadAsync();

            Assert.Contains("Returned 1 of 3 rows", view.BoundsText);
            Assert.Contains("LongFileNameThatMustRemainInspectable.cs", view.FileRoots[0].Children[0].ToString());
            Assert.True(view.CanLoadMore);
        });
    }

    [Fact]
    public async Task SelectionShowsTypedOutlineSourceAndFileLimitedCopy()
    {
        await StaAsync(async () =>
        {
            var indexed = Indexed("file:a", "public class A { void M() {} }", [new AtlasTextSpan(7, 5)]);
            var queries = new FakeAtlasQueries { Select = _ => Task.FromResult(indexed) };
            var view = new AtlasReaderView(queries, "manifest:1");

            await view.SelectFileAsync(AtlasFileNode.File("A.cs", File("A.cs", "file:a")));

            Assert.Equal("public class A { void M() {} }", view.SourceText);
            Assert.Single(view.OutlineRows);
            Assert.Contains("Indexed source match", view.SourceStatusText);
            Assert.Single(view.CurrentHighlights);

            queries.Select = _ => Task.FromResult(Selection("file:a", SourceProjection.TooLargeToVerify("source:a"), "limited"));
            await view.SelectDeclarationAsync(view.OutlineRows[0]);

            Assert.Equal("", view.SourceText);
            Assert.Empty(view.CurrentHighlights);
            Assert.Contains("Project/TFM context not established", view.SourceStatusText);
        });
    }

    [Fact]
    public async Task EveryNonIndexedSourceStateClearsPriorTextAndHighlights()
    {
        await StaAsync(async () =>
        {
            var view = new AtlasReaderView(new FakeAtlasQueries(), "manifest:1");
            foreach (var state in NonIndexedStates())
            {
                var queries = new FakeAtlasQueries { Select = _ => Task.FromResult(Indexed("file:a", "class A {}", [new AtlasTextSpan(0, 5)])) };
                view = new AtlasReaderView(queries, "manifest:1");
                await view.SelectFileAsync(AtlasFileNode.File("A.cs", File("A.cs", "file:a")));
                queries.Select = _ => Task.FromResult(Selection("file:a", Projection(state, "source:a"), "reason"));

                await view.SelectFileAsync(AtlasFileNode.File("A.cs", File("A.cs", "file:a")));

                Assert.Equal("", view.SourceText);
                Assert.Empty(view.CurrentHighlights);
                Assert.DoesNotContain("class A", view.SourceStatusText);
            }
        });
    }

    [Fact]
    public async Task LateAResultCannotReplaceAcceptedBResult()
    {
        await StaAsync(async () =>
        {
            var first = new TaskCompletionSource<SelectionProjection>(TaskCreationOptions.RunContinuationsAsynchronously);
            var second = new TaskCompletionSource<SelectionProjection>(TaskCreationOptions.RunContinuationsAsynchronously);
            var calls = 0;
            var queries = new FakeAtlasQueries
            {
                Select = _ => ++calls == 1 ? first.Task : second.Task,
            };
            var view = new AtlasReaderView(queries, "manifest:1");
            var a = view.SelectFileAsync(AtlasFileNode.File("A.cs", File("A.cs", "file:a")));
            var b = view.SelectFileAsync(AtlasFileNode.File("B.cs", File("B.cs", "file:b")));

            second.SetResult(Indexed("file:b", "class B {}", []));
            await b;
            first.SetResult(Indexed("file:a", "class A {}", []));
            await a;

            Assert.Equal("class B {}", view.SourceText);
            Assert.Contains("file:b", view.StatusText);
        });
    }

    [Fact]
    public async Task BackUsesReceiptAndDoesNotSubstituteLatestWhenUnavailable()
    {
        await StaAsync(async () =>
        {
            var restored = "";
            var queries = new FakeAtlasQueries();
            queries.Select = request => Task.FromResult(Indexed(request.FileValue, request.FileValue == "file:a" ? "class A {}" : "class B {}", []));
            queries.Restore = (receipt, _) =>
            {
                restored = receipt;
                return Task.FromResult(Selection("file:a", SourceProjection.Unavailable("source:a"), "receipt expired"));
            };
            var view = new AtlasReaderView(queries, "manifest:1");
            await view.SelectFileAsync(AtlasFileNode.File("A.cs", File("A.cs", "file:a")));
            await view.SelectFileAsync(AtlasFileNode.File("B.cs", File("B.cs", "file:b")));

            await view.GoBackAsync();

            Assert.Equal("receipt:file:a", restored);
            Assert.Equal("", view.SourceText);
            Assert.Contains("receipt expired", view.SourceStatusText);
            Assert.DoesNotContain("class B", view.SourceText);
        });
    }

    private static Task StaAsync(Func<Task> body)
    {
        Sta.Pump(() => new Border(), async (_, _) => await body(), timeoutSeconds: 30);
        return Task.CompletedTask;
    }

    private static IEnumerable<SourceProjectionState> NonIndexedStates() =>
    [
        SourceProjectionState.Changed,
        SourceProjectionState.Unavailable,
        SourceProjectionState.Unverifiable,
        SourceProjectionState.UnsupportedEncoding,
        SourceProjectionState.TooLargeToVerify,
        SourceProjectionState.ReadUnstable,
        SourceProjectionState.Refused,
        SourceProjectionState.Canceled,
    ];

    private static SourceProjection Projection(SourceProjectionState state, string key) => state switch
    {
        SourceProjectionState.Changed => SourceProjection.Changed(key),
        SourceProjectionState.Unavailable => SourceProjection.Unavailable(key),
        SourceProjectionState.Unverifiable => SourceProjection.Unverifiable(key),
        SourceProjectionState.UnsupportedEncoding => SourceProjection.UnsupportedEncoding(key),
        SourceProjectionState.TooLargeToVerify => SourceProjection.TooLargeToVerify(key),
        SourceProjectionState.ReadUnstable => SourceProjection.ReadUnstable(key),
        SourceProjectionState.Refused => SourceProjection.Refused(key),
        SourceProjectionState.Canceled => SourceProjection.Canceled(key),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
    };

    private static SelectionProjection Indexed(string fileValue, string text, IReadOnlyList<AtlasTextSpan> highlights)
    {
        var root = new AtlasObjectIdentity("root", "1");
        var file = new AtlasObjectIdentity("file", "1");
        var hash = "sha256:" + new string('a', 64);
        var observation = AtlasSourceObservation.Verified("source:a", "manifest:1", fileValue, "policy:1", root, file, hash, text.Length, "utf-8", text.Length, BoundsKnown(1, 1));
        var binding = AtlasSourceBinding.Create("manifest:1", fileValue, "policy:1", AtlasIdentityCodec.ForNativeObject(root), AtlasIdentityCodec.ForNativeObject(file), hash);
        return Selection(
            fileValue,
            SourceProjection.IndexedMatch(observation, binding, "utf-8", new SourceTextPage(text, new AtlasTextSpan(0, text.Length), highlights)),
            "Project/TFM context not established");
    }

    private static SelectionProjection Selection(string fileValue, SourceProjection source, string limitation) =>
        new(
            "receipt:" + fileValue,
            "manifest:1",
            fileValue,
            1,
            new SelectionOutline([new OutlineDeclaration("decl:" + fileValue, "A.M()", AtlasDeclarationKind.Method, new AtlasTextSpan(7, 5))]),
            source,
            BoundsUnknown(1, "selection bounded"),
            SelectionCoverage.Unknown("coverage not recorded"),
            [limitation]);

    private static AtlasFileEntry File(string relativePath, string fileValue) =>
        new(fileValue, relativePath, ".", AtlasDirectoryEntryKind.File, AtlasFileClassification.CSharp, null, AtlasFileAvailability.Available, null);

    private static AtlasBounds BoundsKnown(int rows, long total) =>
        new(AtlasReaderView.PageSize, AtlasReaderView.PageSize, rows, rows * 100, total, AtlasDenominatorState.Known, null, "page");

    private static AtlasBounds BoundsUnknown(int rows, string reason) =>
        new(AtlasReaderView.PageSize, AtlasReaderView.PageSize, rows, rows * 100, null, AtlasDenominatorState.Unknown, reason, "page");

    private sealed class FakeAtlasQueries : IAtlasQueries
    {
        public Func<PageRequest, Task<InventoryPage>> Inventory { get; set; } =
            request => Task.FromResult(new InventoryPage(request, BoundsUnknown(0, "not loaded"), []));

        public Func<SelectionRequest, Task<SelectionProjection>> Select { get; set; } =
            request => Task.FromResult(Selection(request.FileValue, SourceProjection.Unavailable("source:missing"), "not loaded"));

        public Func<string, long, Task<SelectionProjection>> Restore { get; set; } =
            (_, _) => Task.FromResult(Selection("file:missing", SourceProjection.Unavailable("source:missing"), "receipt unavailable"));

        public Task<InventoryPage> InventoryAsync(PageRequest request, CancellationToken cancellationToken) =>
            Inventory(request);

        public Task<SelectionProjection> SelectAsync(SelectionRequest request, CancellationToken cancellationToken) =>
            Select(request);

        public Task<SelectionProjection> RestoreAsync(string issuedReceiptToken, long requestSequence, CancellationToken cancellationToken) =>
            Restore(issuedReceiptToken, requestSequence);
    }
}
