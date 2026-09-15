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
                ObservedReader? firstReader = null;
                ObservedReader? replacementReader = null;
                var first = new MainWindowViewModel(clients[0], "display-first-not-pipe-authority", null,
                    commands: clients[0], atlasReaderFactory: () =>
                        firstReader = new ObservedReader(clients[0].CreateAtlasReader(), receipt, "first"));
                var replacement = new MainWindowViewModel(clients[1], "display-replacement-not-pipe-authority", null,
                    commands: clients[1], atlasReaderFactory: () =>
                        replacementReader = new ObservedReader(clients[1].CreateAtlasReader(), receipt, "replacement"));
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
                    finally { Capture(window, evidence, receipt, "mainwindow-member-normal-default.png"); }
                    Assert.True(canonicalZone == ZoneId.Center, "The real normal opener must place a new Atlas in canonical Center.");
                    Assert.True(view.IsVisible && view.IsKeyboardFocusWithin);
                    _ = MeasureReading(window, view, actualSource, memberSelection.Source.Highlights,
                        receipt, "normal-default-verified", requireReadable: true);
                    await ObserveAutomationAsync(new WindowInteropHelper(window).Handle, receipt);
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
                    await ObserveAutomationAsync(hwnd, receipt);
                    Capture(window, evidence, receipt);

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
                    Assert.NotSame(view, nextView);
                    await nextView.SelectFileAsync(await FindOwnedFileAsync(nextView));
                    var nextLease = Assert.IsType<ObservedLease>(replacementReader?.Lease);
                    Assert.True(nextLease.ScopeToken != firstLease.ScopeToken, "Replacement must own a distinct actual scope.");
                    VerifySource(nextView, Assert.IsType<AtlasSelectionDto>(nextLease.LastSelection), receipt, "replacement", replacementSourcePath);
                    try { VerifyFooter(window, replacement, receipt, "replacement"); }
                    finally { Capture(window, evidence, receipt, "mainwindow-replacement-footer.png"); }
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

    private static Task ObserveAutomationAsync(nint hwnd, Receipt receipt) => Task.Run(() =>
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
            var element = root.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, name));
            Assert.NotNull(element);
            Assert.False(element.Current.IsOffscreen, "Named Atlas control must be visible in the proof-owned HWND.");
        }
        receipt.Mark("uia.own-hwnd", new { Hwnd = hwnd.ToInt64(), ProcessId = Environment.ProcessId, Apartment = "MTA", Names = names });
    });

    private static double MeasureReading(Window window, AtlasReaderView view, string source,
        IReadOnlyList<AtlasSpanDto> highlights, Receipt receipt, string stage, bool requireReadable)
    {
        var client = Assert.IsAssignableFrom<FrameworkElement>(window.Content);
        var clientBounds = client.TransformToAncestor(window).TransformBounds(new Rect(client.RenderSize));
        var textView = view.SourceControl.TextArea.TextView;
        textView.EnsureVisualLines();
        var viewport = VisibleBounds(textView, window, clientBounds);
        var selection = Assert.IsType<AtlasSelectionDto>(view.CurrentSelection);
        var page = Assert.IsType<AtlasSpanDto>(selection.Source.PageSpan);
        var pageText = source.Substring(page.Start, page.Length);
        Assert.Equal(pageText, selection.Source.Text);
        Assert.Equal(pageText, view.SourceText);
        Assert.Equal<AtlasSpanDto>(selection.Source.Highlights, highlights);
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
        foreach (var line in pageText.Split('\n'))
        {
            if (line.Length > 0) lineBounds.Add(SourceRect(offset, line.Length));
            offset += line.Length + 1;
        }
        var highlightBounds = highlights.Select(span => SourceRect(span.Start - page.Start, span.Length)).ToArray();
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
            SourcePageStart = page.Start, SourcePageLength = page.Length,
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

    private static void Capture(Window window, string evidence, Receipt receipt, string name = "mainwindow.png")
    {
        var width = (int)Math.Ceiling(window.ActualWidth);
        var height = (int)Math.Ceiling(window.ActualHeight);
        Assert.True(width > 0 && height > 0 && window.IsVisible);
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);
        Assert.True(pixels.Where((_, index) => index % 4 == 3).Any(alpha => alpha != 0),
            "The owned-window capture must not be a transparent blank.");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var path = Path.Combine(evidence, name);
        using (var file = File.Create(path)) encoder.Save(file);
        receipt.Mark("capture.window-only", new { Path = path, Width = width, Height = height, Sha256 = Hash(File.ReadAllBytes(path)) });
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

    private sealed class ObservedReader(IAtlasWorkspaceReader inner, Receipt receipt, string name) : IAtlasWorkspaceReader
    {
        internal ObservedLease? Lease { get; private set; }
        internal bool Disposed { get; private set; }
        public async ValueTask<IAtlasReaderLease> AdmitAsync(CancellationToken cancellationToken)
        {
            var actual = await inner.AdmitAsync(cancellationToken);
            Lease = new ObservedLease(actual, receipt, name);
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

    private sealed class ObservedLease(IAtlasReaderLease inner, Receipt receipt, string name) : IAtlasReaderLease, IAtlasReaderQueries
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

    private sealed class Receipt(string directory)
    {
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
        internal void Failure(string stage, Exception exception)
        {
            lock (_gate)
            {
                ++FailureCount;
                Mark(stage, new { ExceptionType = exception.GetType().FullName, exception.StackTrace,
                    MessageSha256 = Hash(Encoding.UTF8.GetBytes(exception.Message)) });
            }
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
                }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
