using System.Windows.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using AiDe.App.Workbench.Understanding;
using AiDe.Core.Understanding;
using Xunit.Abstractions;

namespace AiDe.App.Tests;

public sealed class AtlasReaderViewTests(ITestOutputHelper output)
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
            Assert.False(partial.CanLoadMore);
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
                Inventory = request => Task.FromResult(new InventoryPage(request, BoundsKnown(1, 3), [File("src\\LongFileNameThatMustRemainInspectable.cs", "file:a")], nextOffset: 1)),
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
            Assert.Contains("verification budget", view.SourceStatusText);
            Assert.DoesNotContain("Project/TFM", view.SourceStatusText);
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

    [Fact]
    public void NativeReader_NestedFile_RealizedContainerSelectsSourceAndExposesRoles()
    {
        var queries = new FakeAtlasQueries
        {
            Inventory = request => Task.FromResult(new InventoryPage(request, BoundsKnown(2, 3),
            [
                new AtlasFileEntry("directory:src", "src", ".", AtlasDirectoryEntryKind.Directory,
                    AtlasFileClassification.Unknown, null, AtlasFileAvailability.Available, null),
                File("src\\A.cs", "file:a"),
            ], nextOffset: 2)),
            Select = request => Task.FromResult(Indexed(request.FileValue, "class A {}", [])),
        };
        Shown(queries, async (window, view) =>
        {
            await view.LoadAsync();
            window.UpdateLayout();
            Assert.Single(view.FilesControl.Items);
            var directory = Assert.IsType<TreeViewItem>(view.FilesControl.ItemContainerGenerator.ContainerFromIndex(0));
            Assert.False(((AtlasFileNode)directory.Header).IsFile);
            directory.IsExpanded = true;
            window.UpdateLayout();
            var child = Assert.IsType<TreeViewItem>(directory.ItemContainerGenerator.ContainerFromIndex(0));
            var peer = UIElementAutomationPeer.CreatePeerForElement(child)!;
            Assert.Equal(AutomationControlType.TreeItem, peer.GetAutomationControlType());
            Assert.Contains("src\\A.cs", peer.GetName());
            ((ISelectionItemProvider)peer.GetPattern(PatternInterface.SelectionItem)!).Select();
            await Drain();
            Assert.Equal("class A {}", view.SourceText);
            window.UpdateLayout();
            var member = Assert.IsType<ListBoxItem>(view.OutlineControl.ItemContainerGenerator.ContainerFromIndex(0));
            var memberPeer = UIElementAutomationPeer.CreatePeerForElement(member)!;
            Assert.Equal(AutomationControlType.ListItem, memberPeer.GetAutomationControlType());
            Assert.Contains("A.M()", memberPeer.GetName());
            Assert.True(VirtualizingPanel.GetIsVirtualizing(view.FilesControl));
            Assert.Equal(13, view.SourceControl.FontSize);
        });
    }

    [Fact]
    public void NativeReader_Pagination_UsesReturnedOffsetAndDeduplicatesRepeatedRows()
    {
        var calls = 0;
        var queries = new FakeAtlasQueries
        {
            Inventory = request => Task.FromResult(++calls == 1
                ? new InventoryPage(request, BoundsKnown(1, 3), [File("src\\A.cs", "file:a")], nextOffset: 1)
                : new InventoryPage(new PageRequest(2, request.Limit), BoundsKnown(1, 3), [File("src\\A.cs", "file:a")])),
        };
        Shown(queries, async (window, view) =>
        {
            await view.LoadAsync();
            view.LoadMoreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Drain();
            Assert.False(view.CanLoadMore);
            Assert.Equal(2, calls);
            var directory = Assert.IsType<TreeViewItem>(view.FilesControl.ItemContainerGenerator.ContainerFromIndex(0));
            directory.IsExpanded = true;
            window.UpdateLayout();
            Assert.Single(directory.Items);
        });
    }

    [Theory]
    [InlineData(AtlasDenominatorState.Unknown)]
    [InlineData(AtlasDenominatorState.Withheld)]
    public void NativeReader_UnspecifiedContinuation_ShowsLimitationWithoutAction(AtlasDenominatorState total)
    {
        var queries = new FakeAtlasQueries
        {
            Inventory = request => Task.FromResult(new InventoryPage(request,
                new AtlasBounds(request.Limit, request.Limit, 1, 100, null, total, "bounded walk", "page"),
                [File("A.cs", "file:a")])),
        };
        Shown(queries, async (_, view) =>
        {
            await view.LoadAsync();
            Assert.False(view.CanLoadMore);
            Assert.Contains("No further retained page", view.BoundsText);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeReader_StaleFaultOrCancel_CannotChangeCurrentSurface(bool canceled)
    {
        var late = new TaskCompletionSource<SelectionProjection>();
        var queries = new FakeAtlasQueries { Select = _ => Task.FromResult(Indexed("file:a", "old text", [])) };
        Shown(queries, async (_, view) =>
        {
            await view.SelectFileAsync(Node("a"));
            queries.Select = _ => late.Task;
            var pending = view.SelectFileAsync(Node("b"));
            Assert.Equal("", view.SourceText);
            Assert.Empty(view.CurrentHighlights);
            queries.Select = _ => Task.FromResult(Indexed("file:c", "current text", []));
            await view.SelectFileAsync(Node("c"));
            view.SourceControl.TextArea.Focus();
            var focus = Keyboard.FocusedElement;
            var status = view.StatusText;
            if (canceled) late.SetCanceled(); else late.SetException(new InvalidOperationException("stale failure"));
            await pending;
            Assert.Equal("current text", view.SourceText);
            Assert.Equal(status, view.StatusText);
            Assert.Same(focus, Keyboard.FocusedElement);
        });
    }

    [Fact]
    public void NativeReader_SynchronousQueryThrow_IsObservedByNativeSelectionEvent()
    {
        var queries = new FakeAtlasQueries
        {
            Inventory = request => Task.FromResult(new InventoryPage(request, BoundsKnown(1, 1), [File("A.cs", "file:a")])),
            Select = _ => throw new InvalidOperationException("private query detail"),
        };
        Shown(queries, async (window, view) =>
        {
            await view.LoadAsync();
            window.UpdateLayout();
            var item = Assert.IsType<TreeViewItem>(view.FilesControl.ItemContainerGenerator.ContainerFromIndex(0));
            item.IsSelected = true;
            await Drain();
            Assert.Contains("ATLAS-READER-SELECTION", view.StatusText);
            Assert.DoesNotContain("private query detail", view.StatusText);
            Assert.Equal("", view.SourceText);
            Assert.False(view.CanGoBack);
        });
    }

    [Fact]
    public void NativeReader_StaleBack_DoesNotStealFocusOrConsumeHistory()
    {
        var restore = new TaskCompletionSource<SelectionProjection>();
        var queries = new FakeAtlasQueries
        {
            Select = request => Task.FromResult(Indexed(request.FileValue, request.FileValue, [])),
            Restore = (_, _) => restore.Task,
        };
        Shown(queries, async (_, view) =>
        {
            await view.SelectFileAsync(Node("a"));
            await view.SelectFileAsync(Node("b"));
            var pending = view.GoBackAsync();
            await view.SelectFileAsync(Node("c"));
            view.SourceControl.TextArea.Focus();
            var focus = Keyboard.FocusedElement;
            restore.SetResult(Indexed("file:a", "stale restored body", []));
            await pending;
            Assert.Equal("file:c", view.SourceText);
            Assert.Same(focus, Keyboard.FocusedElement);
            Assert.True(view.CanGoBack);
        });
    }

    [Fact]
    public void NativeReader_SupersededTransition_PreservesLastAcceptedReceipt()
    {
        var pending = new TaskCompletionSource<SelectionProjection>();
        var restored = "";
        var queries = new FakeAtlasQueries
        {
            Select = request => Task.FromResult(Indexed(request.FileValue, request.FileValue, [])),
            Restore = (receipt, _) =>
            {
                restored = receipt;
                return Task.FromResult(Indexed("file:a", "restored A", []));
            },
        };
        Shown(queries, async (_, view) =>
        {
            await view.SelectFileAsync(Node("a"));
            queries.Select = _ => pending.Task;
            var b = view.SelectFileAsync(Node("b"));
            queries.Select = _ => Task.FromResult(Indexed("file:c", "accepted C", []));
            await view.SelectFileAsync(Node("c"));
            pending.SetResult(Indexed("file:b", "stale B", []));
            await b;
            await view.GoBackAsync();
            Assert.Equal("receipt:file:a", restored);
            Assert.Equal("restored A", view.SourceText);
        });
    }

    [Fact]
    public void NativeReader_FailedTransition_DoesNotPushReceipt()
    {
        var queries = new FakeAtlasQueries { Select = request => Task.FromResult(Indexed(request.FileValue, "body", [])) };
        Shown(queries, async (_, view) =>
        {
            await view.SelectFileAsync(Node("a"));
            queries.Select = _ => Task.FromException<SelectionProjection>(new InvalidOperationException());
            await view.SelectFileAsync(Node("b"));
            Assert.False(view.CanGoBack);
            Assert.Equal("", view.SourceText);
        });
    }

    [Fact]
    public void NativeReader_BackHistory_IsBoundedAtFiftyReceipts()
    {
        var restores = 0;
        var queries = new FakeAtlasQueries
        {
            Select = request => Task.FromResult(Indexed(request.FileValue, "body", [])),
            Restore = (receipt, _) =>
            {
                restores++;
                return Task.FromResult(Indexed(receipt["receipt:".Length..], "restored", []));
            },
        };
        Shown(queries, async (_, view) =>
        {
            for (var i = 0; i < 55; i++) await view.SelectFileAsync(Node(i.ToString()));
            for (var i = 0; i < 50; i++) await view.GoBackAsync();
            Assert.False(view.CanGoBack);
            await view.GoBackAsync();
            Assert.Equal(50, restores);
            Assert.Contains("evicted", view.StatusText);
        });
    }

    [Fact]
    public void NativeReader_Unloaded_CancelsAndIgnoresOutstandingSelection()
    {
        var late = new TaskCompletionSource<SelectionProjection>();
        var queries = new FakeAtlasQueries { Select = _ => late.Task };
        Shown(queries, async (window, view) =>
        {
            var pending = view.SelectFileAsync(Node("a"));
            window.Content = null;
            await Drain();
            Assert.True(queries.LastSelectionToken.IsCancellationRequested);
            late.SetResult(Indexed("file:a", "late body", []));
            await pending;
            Assert.Equal("", view.SourceText);
        });
    }

    [Fact]
    public void NativeReader_KeyboardMemberThenBack_RestoresSourceSelectionScrollAndFocus()
    {
        var text = string.Join("\n", Enumerable.Range(0, 250).Select(i => $"line {i}"));
        var queries = new FakeAtlasQueries
        {
            Select = request => Task.FromResult(Indexed(request.FileValue, text, [])),
            Restore = (_, _) => Task.FromResult(Indexed("file:a", text, [])),
        };
        Shown(queries, async (window, view) =>
        {
            await view.SelectFileAsync(Node("a"));
            window.UpdateLayout();
            view.SourceControl.Select(15, 4);
            view.SourceControl.ScrollToVerticalOffset(200);
            view.OutlineControl.SelectedIndex = 0;
            view.OutlineControl.Focus();
            await Drain();
            var scroll = view.SourceControl.VerticalOffset;
            view.OutlineControl.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
                PresentationSource.FromVisual(view.OutlineControl), 0, Key.Enter) { RoutedEvent = Keyboard.KeyDownEvent });
            await Drain();
            Assert.True(view.CanGoBack);
            view.BackButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Drain();
            Assert.Equal(15, view.SourceControl.SelectionStart);
            Assert.Equal(4, view.SourceControl.SelectionLength);
            Assert.Equal(scroll, view.SourceControl.VerticalOffset);
            Assert.Equal(0, view.OutlineControl.SelectedIndex);
            Assert.True(view.OutlineControl.IsKeyboardFocusWithin);
        });
    }

    [Fact]
    public void NativeReader_AcceptedFileMemberFileAndBack_AdvanceOnlyReturnedManifestTokens()
    {
        var inputs = new List<string>();
        var expected = "manifest:1";
        var queries = new FakeAtlasQueries
        {
            Inventory = request => Task.FromResult(new InventoryPage(request, BoundsKnown(2, 2),
                [File("A.cs", "file:a"), File("B.cs", "file:b")])),
        };
        queries.Select = request =>
        {
            inputs.Add(request.ManifestToken);
            if (request.ManifestToken != expected)
                throw new InvalidOperationException("Fixture refuses stale manifest authority.");
            expected = "manifest:" + (inputs.Count + 1);
            return Task.FromResult(Indexed(request.FileValue, "accepted " + inputs.Count, [], expected));
        };
        queries.Restore = (_, _) =>
        {
            expected = "manifest:restored";
            return Task.FromResult(Indexed("file:a", "restored", [], expected));
        };
        Shown(queries, async (window, view) =>
        {
            await view.LoadAsync();
            window.UpdateLayout();
            var a = Assert.IsType<TreeViewItem>(view.FilesControl.ItemContainerGenerator.ContainerFromIndex(0));
            a.IsSelected = true;
            await Drain();
            Assert.Equal("accepted 1", view.SourceText);
            view.OutlineControl.SelectedIndex = 0;
            view.OutlineControl.Focus();
            view.OutlineControl.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
                PresentationSource.FromVisual(view.OutlineControl), 0, Key.Enter) { RoutedEvent = Keyboard.KeyDownEvent });
            await Drain();
            Assert.Equal("accepted 2", view.SourceText);
            var b = Assert.IsType<TreeViewItem>(view.FilesControl.ItemContainerGenerator.ContainerFromIndex(1));
            b.IsSelected = true;
            await Drain();
            Assert.Equal("accepted 3", view.SourceText);
            view.BackButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Drain();
            Assert.Equal("restored", view.SourceText);
            b.IsSelected = true;
            await Drain();
            Assert.Equal("accepted 4", view.SourceText);
            Assert.Equal(["manifest:1", "manifest:2", "manifest:3", "manifest:restored"], inputs);
        });
    }

    [Theory]
    [InlineData(SourceProjectionState.Refused)]
    [InlineData(SourceProjectionState.Unavailable)]
    [InlineData(SourceProjectionState.Canceled)]
    [InlineData(SourceProjectionState.Changed)]
    [InlineData(SourceProjectionState.Unverifiable)]
    [InlineData(SourceProjectionState.UnsupportedEncoding)]
    [InlineData(SourceProjectionState.TooLargeToVerify)]
    [InlineData(SourceProjectionState.ReadUnstable)]
    public void NativeReader_NonMatch_DoesNotAdvanceManifestOrHistory(SourceProjectionState state)
    {
        var queries = new FakeAtlasQueries { Select = _ => Task.FromResult(Indexed("file:a", "accepted", [], "manifest:accepted")) };
        Shown(queries, async (_, view) =>
        {
            await view.SelectFileAsync(Node("a"));
            queries.Select = _ => Task.FromResult(Selection("file:b", Projection(state, "source:b"), "not accepted", "manifest:rejected"));
            await view.SelectFileAsync(Node("b"));
            Assert.False(view.CanGoBack);
            Assert.Equal("", view.SourceText);
            queries.Select = request => request.ManifestToken == "manifest:accepted"
                ? Task.FromResult(Indexed("file:c", "accepted C", [], "manifest:c"))
                : Task.FromException<SelectionProjection>(new InvalidOperationException("wrong manifest"));
            await view.SelectFileAsync(Node("c"));
            Assert.Equal("accepted C", view.SourceText);
        });
    }

    [Theory]
    [InlineData("late")]
    [InlineData("fault")]
    [InlineData("cancel")]
    public void NativeReader_SupersededOrFailedRequest_CannotAdvanceManifest(string completion)
    {
        var late = new TaskCompletionSource<SelectionProjection>();
        var queries = new FakeAtlasQueries { Select = _ => Task.FromResult(Indexed("file:a", "A", [], "manifest:a")) };
        Shown(queries, async (_, view) =>
        {
            await view.SelectFileAsync(Node("a"));
            queries.Select = _ => late.Task;
            var pending = view.SelectFileAsync(Node("b"));
            if (completion == "late")
            {
                queries.Select = request => request.ManifestToken == "manifest:a"
                    ? Task.FromResult(Indexed("file:c", "C", [], "manifest:c"))
                    : Task.FromException<SelectionProjection>(new InvalidOperationException("wrong manifest"));
                await view.SelectFileAsync(Node("c"));
                Assert.Equal("C", view.SourceText);
                late.SetResult(Indexed("file:b", "stale B", [], "manifest:stale"));
            }
            else if (completion == "fault")
                late.SetException(new InvalidOperationException("fixture failure"));
            else
                late.SetCanceled();
            await pending;
            var expected = completion == "late" ? "manifest:c" : "manifest:a";
            queries.Select = request => request.ManifestToken == expected
                ? Task.FromResult(Indexed("file:d", "accepted D", [], "manifest:d"))
                : Task.FromException<SelectionProjection>(new InvalidOperationException("wrong manifest"));
            await view.SelectFileAsync(Node("d"));
            Assert.Equal("accepted D", view.SourceText);
        });
    }

    [Fact]
    public void NativeReader_UnavailableRestore_KeepsReceiptAndManifestAuthority()
    {
        var restores = new List<string>();
        var queries = new FakeAtlasQueries { Select = request => Task.FromResult(Indexed(request.FileValue, "body", [], "manifest:accepted")) };
        Shown(queries, async (_, view) =>
        {
            await view.SelectFileAsync(Node("a"));
            await view.SelectFileAsync(Node("b"));
            queries.Restore = (receipt, _) =>
            {
                restores.Add(receipt);
                return Task.FromResult(Selection("file:a", SourceProjection.Unavailable("source:a"), "receipt unavailable", "manifest:bad"));
            };
            await view.GoBackAsync();
            await view.GoBackAsync();
            Assert.Equal(["receipt:file:a", "receipt:file:a"], restores);
            queries.Select = request => request.ManifestToken == "manifest:accepted"
                ? Task.FromResult(Indexed("file:c", "C", [], "manifest:c"))
                : Task.FromException<SelectionProjection>(new InvalidOperationException("wrong manifest"));
            await view.SelectFileAsync(Node("c"));
            Assert.Equal("C", view.SourceText);
        });
    }

    [Theory]
    [InlineData(AtlasDenominatorState.Unknown)]
    [InlineData(AtlasDenominatorState.Withheld)]
    public void NativeReader_ExplicitContinuation_IsOnlyNextRequestOffset(AtlasDenominatorState state)
    {
        var offsets = new List<int>();
        var queries = new FakeAtlasQueries
        {
            Inventory = request =>
            {
                offsets.Add(request.Offset);
                var pageOffset = offsets.Count == 1 ? 4 : request.Offset;
                int? next = offsets.Count < 3 ? pageOffset + 1 : null;
                return Task.FromResult(new InventoryPage(new PageRequest(pageOffset, request.Limit),
                    new AtlasBounds(request.Limit, request.Limit, 1, 100, null, state, "bounded walk", "page"),
                    [File("File" + offsets.Count + ".cs", "file:" + offsets.Count)], next));
            },
        };
        Shown(queries, async (_, view) =>
        {
            await view.LoadAsync();
            Assert.True(view.CanLoadMore);
            view.LoadMoreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Drain();
            Assert.True(view.CanLoadMore);
            view.LoadMoreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Drain();
            Assert.False(view.CanLoadMore);
            Assert.Equal([0, 5, 6], offsets);
            Assert.Contains(state == AtlasDenominatorState.Unknown ? "total not recorded" : "total withheld", view.BoundsText);
            Assert.Contains("No further retained page", view.BoundsText);
        });
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(1, 3)]
    public void NativeReader_NullContinuation_NeverInfersAnotherPage(int rows, long total)
    {
        var queries = new FakeAtlasQueries
        {
            Inventory = request => Task.FromResult(new InventoryPage(request, BoundsKnown(rows, total),
                rows == 0 ? [] : [File("A.cs", "file:a")])),
        };
        Shown(queries, async (_, view) =>
        {
            await view.LoadAsync();
            Assert.False(view.CanLoadMore);
            Assert.Contains("No further retained page", view.BoundsText);
        });
    }

    [Fact]
    public void NativeReader_AcceptedMemberActivation_RetainsSelectedOutlineKeyAfterAwait()
    {
        var accepted = new TaskCompletionSource<SelectionProjection>(TaskCreationOptions.RunContinuationsAsynchronously);
        var queries = MemberActivationQueries(accepted.Task);
        Shown(queries, async (window, view) =>
        {
            await view.LoadAsync();
            SelectRenderedFile(window, view, 0);
            await Drain();
            var handle = new WindowInteropHelper(window).Handle;
            var processId = Environment.ProcessId;
            await AssertOwnedWindowSelectionAsync(window, handle, processId, null);
            ActivateRenderedMember(window, view);
            accepted.SetResult(Indexed("file:a", "accepted member body", []));
            await Drain();

            Assert.Equal("accepted member body", view.SourceText);
            var selected = Assert.IsType<OutlineRow>(view.OutlineControl.SelectedItem);
            Assert.Equal("decl:file:a", selected.ObservationKey);
            window.UpdateLayout();
            var item = Assert.IsType<ListBoxItem>(view.OutlineControl.ItemContainerGenerator.ContainerFromIndex(0));
            var receipt = $"phase=accepted-member-await; selectedKey={selected.ObservationKey}; realizedContentKey={(item.Content as OutlineRow)?.ObservationKey}; " +
                $"selectedIsContent={ReferenceEquals(selected, item.Content)}; selectedIsDataContext={ReferenceEquals(selected, item.DataContext)}; " +
                $"containerSelected={item.IsSelected}";
            output.WriteLine(receipt);

            Assert.Same(selected, item.Content);
            Assert.True(item.IsSelected, "Realized ListBoxItem.IsSelected must be true. " + receipt);
            await AssertOwnedWindowSelectionAsync(window, handle, processId, selected.AccessibleName);
        });
    }

    [Fact]
    public void NativeReader_AcceptedMemberThenDifferentFileBack_RestoresSelectedOutlineRowWithoutManualReselect()
    {
        var accepted = new TaskCompletionSource<SelectionProjection>(TaskCreationOptions.RunContinuationsAsynchronously);
        var queries = MemberActivationQueries(accepted.Task);
        queries.Restore = (_, _) => Task.FromResult(Indexed("file:a", "accepted member body", []));
        Shown(queries, async (window, view) =>
        {
            await view.LoadAsync();
            SelectRenderedFile(window, view, 0);
            await Drain();
            ActivateRenderedMember(window, view);
            accepted.SetResult(Indexed("file:a", "accepted member body", []));
            await Drain();

            Assert.Equal("decl:file:a", Assert.IsType<OutlineRow>(view.OutlineControl.SelectedItem).ObservationKey);
            var isolatedPeer = UIElementAutomationPeer.CreatePeerForElement(view.OutlineControl)!;
            var isolatedPattern = isolatedPeer.GetPattern(PatternInterface.Selection) as ISelectionProvider;
            var manual = isolatedPattern?.GetSelection();
            output.WriteLine($"phase=isolated-Back-window/manual-operands; patternPresent={isolatedPattern is not null}; " +
                $"slotCount={manual?.Length.ToString() ?? "<null>"}; " +
                $"firstSlotIsNull={(manual is { Length: > 0 } ? (manual[0] is null).ToString() : "<no-slot>")}; " +
                $"patternIdentifierIsNull={SelectionItemPatternIdentifiers.Pattern is null}");
            view.SourceControl.Select(14, 1);
            view.SourceControl.ScrollToVerticalOffset(0);
            SelectRenderedFile(window, view, 1);
            await Drain();
            Assert.Equal("body for file:b", view.SourceText);
            view.BackButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Drain();

            Assert.Equal("accepted member body", view.SourceText);
            Assert.Equal("decl:file:a", Assert.IsType<OutlineRow>(view.OutlineControl.SelectedItem).ObservationKey);
            Assert.Equal(14, view.SourceControl.SelectionStart);
            Assert.Equal(1, view.SourceControl.SelectionLength);
            Assert.Equal(0, view.SourceControl.VerticalOffset);
            Assert.True(view.OutlineControl.IsKeyboardFocusWithin);
        });
    }

    [Theory]
    [InlineData("missing-key")]
    [InlineData("wrong-file")]
    [InlineData("nonmatch")]
    public void NativeReader_UnmatchedMemberReply_DoesNotAcquireSelectionHistoryOrFocus(string reply)
    {
        var pending = new TaskCompletionSource<SelectionProjection>(TaskCreationOptions.RunContinuationsAsynchronously);
        var queries = MemberActivationQueries(pending.Task);
        Shown(queries, async (window, view) =>
        {
            await view.LoadAsync();
            SelectRenderedFile(window, view, 0);
            await Drain();
            ActivateRenderedMember(window, view);
            view.SourceControl.TextArea.Focus();
            var focus = Keyboard.FocusedElement;
            var projection = reply == "nonmatch"
                ? Selection("file:a", SourceProjection.Refused("source:a"), "refused", "manifest:rejected")
                : Indexed(reply == "wrong-file" ? "file:b" : "file:a", "unaccepted body", [], "manifest:rejected");
            pending.SetResult(new SelectionProjection(projection.ReceiptToken, projection.ManifestToken,
                projection.FileValue, projection.Generation,
                new SelectionOutline([new OutlineDeclaration(reply == "missing-key" ? "decl:other" : "decl:file:a",
                    "A.M()", AtlasDeclarationKind.Method, new AtlasTextSpan(7, 5))]),
                projection.Source, projection.Bounds, projection.Coverage, projection.Limitations));
            await Drain();

            Assert.Null(view.OutlineControl.SelectedItem);
            Assert.False(view.CanGoBack);
            Assert.Same(focus, Keyboard.FocusedElement);
            Assert.Equal("", view.SourceText);
            queries.Select = request => request.ManifestToken == "manifest:1"
                ? Task.FromResult(Indexed(request.FileValue, "authority preserved", []))
                : Task.FromException<SelectionProjection>(new InvalidOperationException("rejected reply acquired authority"));
            SelectRenderedFile(window, view, 1);
            await Drain();
            Assert.Equal("authority preserved", view.SourceText);
        });
    }

    [Fact]
    public void NativeReader_StaleMemberReply_CannotRebindCurrentOutline()
    {
        var pending = new TaskCompletionSource<SelectionProjection>(TaskCreationOptions.RunContinuationsAsynchronously);
        var queries = MemberActivationQueries(pending.Task);
        Shown(queries, async (window, view) =>
        {
            await view.LoadAsync();
            SelectRenderedFile(window, view, 0);
            await Drain();
            ActivateRenderedMember(window, view);
            SelectRenderedFile(window, view, 1);
            await Drain();
            view.SourceControl.TextArea.Focus();
            var focus = Keyboard.FocusedElement;
            var back = view.CanGoBack;
            pending.SetResult(Indexed("file:a", "stale member", [], "manifest:stale"));
            await Drain();

            Assert.Null(view.OutlineControl.SelectedItem);
            Assert.Equal("body for file:b", view.SourceText);
            Assert.Equal(back, view.CanGoBack);
            Assert.Same(focus, Keyboard.FocusedElement);
        });
    }

    [Fact]
    public void NativeReader_BackMissingMemberKey_DoesNotConsumeReceiptOrRestoreFocus()
    {
        var queries = MemberActivationQueries(Task.FromResult(Indexed("file:a", "accepted member body", [])));
        var restored = new List<string>();
        Shown(queries, async (window, view) =>
        {
            await view.LoadAsync();
            SelectRenderedFile(window, view, 0);
            await Drain();
            ActivateRenderedMember(window, view);
            await Drain();
            Assert.Equal("decl:file:a", Assert.IsType<OutlineRow>(view.OutlineControl.SelectedItem).ObservationKey);
            SelectRenderedFile(window, view, 1);
            await Drain();
            queries.Restore = (receipt, _) =>
            {
                restored.Add(receipt);
                var projection = Indexed("file:a", "unmatched restore", [], "manifest:unmatched");
                return Task.FromResult(new SelectionProjection(projection.ReceiptToken, projection.ManifestToken,
                    projection.FileValue, projection.Generation, new SelectionOutline([]), projection.Source,
                    projection.Bounds, projection.Coverage, projection.Limitations));
            };
            view.SourceControl.TextArea.Focus();
            var focus = Keyboard.FocusedElement;
            view.BackButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Drain();
            view.BackButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Drain();

            Assert.Equal(["receipt:file:a", "receipt:file:a"], restored);
            Assert.Null(view.OutlineControl.SelectedItem);
            Assert.Equal("", view.SourceText);
            Assert.Same(focus, Keyboard.FocusedElement);
        });
    }

    private async Task AssertOwnedWindowSelectionAsync(Window window, nint handle, int processId, string? expectedName)
    {
        var pending = Task.Run(() =>
        {
            Assert.Equal(ApartmentState.MTA, Thread.CurrentThread.GetApartmentState());
            var root = AutomationElement.FromHandle(handle);
            Assert.Equal(processId, root.Current.ProcessId);
            Assert.Equal(handle, new nint(root.Current.NativeWindowHandle));
            output.WriteLine($"phase=own-window-client; apartment=MTA; processId={root.Current.ProcessId}; hwnd={root.Current.NativeWindowHandle}");
            var outline = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.NameProperty, "Atlas member outline"),
                new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.List)));
            Assert.NotNull(outline);
            Assert.True(outline.TryGetCurrentPattern(SelectionPattern.Pattern, out var selectionObject),
                "The owned window's outline must publish SelectionPattern.");
            var selection = Assert.IsType<SelectionPattern>(selectionObject).Current.GetSelection();
            output.WriteLine($"phase={(expectedName is null ? "before-activation" : "accepted-member")}; clientSelectionCount={selection.Length}");
            if (expectedName is null)
            {
                Assert.Empty(selection);
                return;
            }

            var selected = Assert.Single(selection);
            Assert.True(selected.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var itemObject),
                "The current selected outline element must publish SelectionItemPattern.");
            var item = Assert.IsType<SelectionItemPattern>(itemObject).Current;
            var container = item.SelectionContainer;
            Assert.NotNull(container);
            var outlineId = outline.GetRuntimeId();
            var containerId = container.GetRuntimeId();
            output.WriteLine($"phase=current-client-element; name={selected.Current.Name}; isSelected={item.IsSelected}; " +
                $"selectionContainer={container.Current.Name}; outlineRuntimeId={string.Join(",", outlineId)}; " +
                $"containerRuntimeId={string.Join(",", containerId)}");
            Assert.Equal(expectedName, selected.Current.Name);
            Assert.True(item.IsSelected, "The UI Automation client's current selected item must be selected.");
            Assert.Equal(outlineId, containerId);
        });

        try
        {
            await pending.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            window.Close();
            try
            {
                await pending.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (TimeoutException)
            {
                _ = pending.ContinueWith(task => { _ = task.Exception; }, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                throw new Xunit.Sdk.XunitException("NOT_PROVEN: owned-window UIA timed out; worker did not stop within two seconds of closing its window.");
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                throw new Xunit.Sdk.XunitException($"NOT_PROVEN: owned-window UIA timed out; worker ended after window close with {ex.GetType().Name}.");
            }

            throw new Xunit.Sdk.XunitException("NOT_PROVEN: owned-window UIA exceeded ten seconds; worker stopped after closing its window.");
        }
        catch (Exception ex) when (ex is ElementNotAvailableException or System.Runtime.InteropServices.COMException or InvalidOperationException)
        {
            throw new Xunit.Sdk.XunitException($"NOT_PROVEN: owned-window UIA client unavailable: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static FakeAtlasQueries MemberActivationQueries(Task<SelectionProjection> member) => new()
    {
        Inventory = request => Task.FromResult(new InventoryPage(request, BoundsKnown(2, 2),
            [File("A.cs", "file:a"), File("B.cs", "file:b")])),
        Select = request => request.DeclarationObservationKey is null
            ? Task.FromResult(Indexed(request.FileValue, "body for " + request.FileValue, []))
            : member,
    };

    private static void SelectRenderedFile(Window window, AtlasReaderView view, int index)
    {
        window.UpdateLayout();
        var item = Assert.IsType<TreeViewItem>(view.FilesControl.ItemContainerGenerator.ContainerFromIndex(index));
        var peer = UIElementAutomationPeer.CreatePeerForElement(item)!;
        ((ISelectionItemProvider)peer.GetPattern(PatternInterface.SelectionItem)!).Select();
    }

    private static void ActivateRenderedMember(Window window, AtlasReaderView view)
    {
        window.UpdateLayout();
        Assert.IsType<ListBoxItem>(view.OutlineControl.ItemContainerGenerator.ContainerFromIndex(0));
        var peer = UIElementAutomationPeer.CreatePeerForElement(view.OutlineControl)!;
        var provider = Assert.IsAssignableFrom<ISelectionItemProvider>(
            Assert.Single(peer.GetChildren()).GetPattern(PatternInterface.SelectionItem));
        provider.Select();
        view.OutlineControl.Focus();
        view.OutlineControl.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
            PresentationSource.FromVisual(view.OutlineControl), 0, Key.Enter) { RoutedEvent = Keyboard.KeyDownEvent });
    }

    private static AtlasFileNode Node(string name) => AtlasFileNode.File(name + ".cs", File(name + ".cs", "file:" + name));

    private static void Shown(FakeAtlasQueries queries, Func<Window, AtlasReaderView, Task> body) =>
        Sta.Pump(() => new AtlasReaderView(queries, "manifest:1"), body,
            configure: window =>
            {
                window.Width = 1100;
                window.Height = 700;
                window.Left = 30;
                window.Top = 30;
                window.ShowActivated = true;
            }, timeoutSeconds: 30);

    private static async Task Drain() => await Dispatcher.Yield(DispatcherPriority.Background);

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

    private static SelectionProjection Indexed(string fileValue, string text, IReadOnlyList<AtlasTextSpan> highlights, string manifestToken = "manifest:1")
    {
        var root = new AtlasObjectIdentity("root", "1");
        var file = new AtlasObjectIdentity("file", "1");
        var hash = "sha256:" + new string('a', 64);
        var observation = AtlasSourceObservation.Verified("source:a", manifestToken, fileValue, "policy:1", root, file, hash, text.Length, "utf-8", text.Length, BoundsKnown(1, 1));
        var binding = AtlasSourceBinding.Create(manifestToken, fileValue, "policy:1", AtlasIdentityCodec.ForNativeObject(root), AtlasIdentityCodec.ForNativeObject(file), hash);
        return Selection(
            fileValue,
            SourceProjection.IndexedMatch(observation, binding, "utf-8", new SourceTextPage(text, new AtlasTextSpan(0, text.Length), highlights)),
            "Project/TFM context not established", manifestToken);
    }

    private static SelectionProjection Selection(string fileValue, SourceProjection source, string limitation, string manifestToken = "manifest:1") =>
        new(
            "receipt:" + fileValue,
            manifestToken,
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
        public CancellationToken LastSelectionToken { get; private set; }
        public Func<PageRequest, Task<InventoryPage>> Inventory { get; set; } =
            request => Task.FromResult(new InventoryPage(request, BoundsUnknown(0, "not loaded"), []));

        public Func<SelectionRequest, Task<SelectionProjection>> Select { get; set; } =
            request => Task.FromResult(Selection(request.FileValue, SourceProjection.Unavailable("source:missing"), "not loaded"));

        public Func<string, long, Task<SelectionProjection>> Restore { get; set; } =
            (_, _) => Task.FromResult(Selection("file:missing", SourceProjection.Unavailable("source:missing"), "receipt unavailable"));

        public Task<InventoryPage> InventoryAsync(PageRequest request, CancellationToken cancellationToken) =>
            Inventory(request);

        public Task<SelectionProjection> SelectAsync(SelectionRequest request, CancellationToken cancellationToken)
        {
            LastSelectionToken = cancellationToken;
            return Select(request);
        }

        public Task<SelectionProjection> RestoreAsync(string issuedReceiptToken, long requestSequence, CancellationToken cancellationToken) =>
            Restore(issuedReceiptToken, requestSequence);
    }
}
