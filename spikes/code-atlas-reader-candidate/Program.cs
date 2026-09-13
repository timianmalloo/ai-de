using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using AiDe.App.Workbench.Understanding;
using AiDe.Core.Understanding;
using Microsoft.Win32.SafeHandles;

namespace CodeAtlas.ReaderCandidate;

/// <summary>
/// Detached, proof-only host. --prove owns its synthetic repository; --approved requires a
/// Conductor-pinned external record and exact independent launch expectations. Neither mode
/// instantiates App or evaluates projects in the inspected repository.
/// </summary>
internal static class Program
{
    private const string GitExecutable = @"C:\Program Files\Git\cmd\git.exe";
    private const string GitVersion = "git version 2.55.0.windows.2";
    private const string ApprovedFutureRoot = @"C:\Projects\ai-de-atlas-live-reader-proof\src\AiDe.Core";
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    private static readonly string[] RequiredChecks =
    [
        "authorization-refusals", "git-version", "git-clean-revision", "actual-composition",
        "plain-startup", "window-visible", "controls-uia", "inventory-metadata",
        "first-file", "rendered-source", "unicode-bom", "member", "member-manifest",
        "different-file", "different-manifest", "back-receipt", "back-source",
        "back-focus", "no-fresh-back-select", "changed-clears", "unavailable-clears",
        "canceled", "pass-through", "rendered-capture", "membership-unavailable"
    ];

    [STAThread]
    public static int Main(string[] args)
    {
        Evidence? evidence = null;
        try
        {
            var options = Options.Parse(args);
            ValidateDirectory(options.Output);
            if (Directory.Exists(options.Output) || File.Exists(options.Output))
                throw new ProofFailure("OUTPUT-MUST-BE-NEW");
            Directory.CreateDirectory(options.Output);
            evidence = new Evidence(options.Output, options.Prove);
            using var watchdog = new System.Threading.Timer(_ =>
            {
                evidence.Event("timeout", new { code = "RUN-TIMEOUT" });
                evidence.Finish(2);
                Environment.Exit(2);
            }, null, TimeSpan.FromSeconds(100), Timeout.InfiniteTimeSpan);
            var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            application.Resources = ExistingBrushes();
            var window = new Window
            {
                Title = "Atlas detached reader — author proof",
                Width = 1100,
                Height = 760,
                ShowActivated = true
            };
            application.MainWindow = window;
            application.DispatcherUnhandledException += (_, e) =>
            {
                evidence.Event("dispatcher-failure", new { type = e.Exception.GetType().Name });
                e.Handled = true;
                evidence.Finish(1);
                application.Shutdown(1);
            };
            window.Loaded += async (_, _) =>
            {
                var exit = 1;
                try
                {
                    await Execute(options, evidence, application, window);
                    exit = evidence.AllRequiredPassed ? 0 : 1;
                }
                catch (Exception error)
                {
                    evidence.Event("failure", new
                    {
                        code = error is ProofFailure failure ? failure.Code : "UNEXPECTED",
                        type = error.GetType().Name
                    });
                    if (window.IsVisible && window.Content is DockPanel panel && panel.Children.OfType<AtlasReaderView>().Any())
                    {
                        try { Capture(window, evidence, "failure-rendered.png"); }
                        catch (Exception captureError) when (captureError is not OutOfMemoryException)
                        { evidence.Event("failure-capture-unavailable", new { type = captureError.GetType().Name }); }
                    }
                }
                finally
                {
                    evidence.Finish(exit);
                    application.Shutdown(exit);
                }
            };
            return application.Run(window);
        }
        catch (Exception error)
        {
            evidence?.Event("startup-refusal", new { type = error.GetType().Name });
            evidence?.Finish(1);
            Console.Error.WriteLine(error is ProofFailure failure ? failure.Code : "RUNNER-STARTUP-REFUSED");
            return 1;
        }
    }

    private static async Task Execute(Options options, Evidence evidence, Application application, Window window)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(85));
        var token = deadline.Token;
        var gate = options.Prove ? await SyntheticApproval(options, evidence, token) : ExternalApproval(options, evidence);
        if (!gate.Current(gate.Expected))
            throw new ProofFailure("APPROVAL-REFUSED");
        if (options.Prove)
            AuthorizationNegatives(gate, evidence);
        var membership = await Membership(gate.Expected.ApprovedAbsoluteRoot, options.ExpectedCommit, evidence, token);
        using var handle = await AtlasProofComposition.CreateForApprovedRootAsync(
            gate.Expected, gate.Current, membership.Paths, membership.Complete, membership.Reason, token)
            .WaitAsync(TimeSpan.FromSeconds(35), token);
        evidence.Check("actual-composition",
            handle.Queries.GetType().FullName == "AiDe.Core.Understanding.AtlasQueryService"
            && handle.Queries.GetType().Assembly == typeof(AtlasProofComposition).Assembly,
            new { factory = typeof(AtlasProofComposition).FullName, queryType = handle.Queries.GetType().FullName,
                module = handle.Queries.GetType().Module.ModuleVersionId, handle.InitialManifestToken });
        var trace = new TraceQueries(handle.Queries, evidence);
        var view = new AtlasReaderView(trace, handle.InitialManifestToken);
        Present(window, view, membership.Reason);
        await view.LoadAsync(token);
        await Paint();
        StartupObservation(application, window, evidence);
        evidence.Check("window-visible", window.IsVisible && window.IsLoaded && view.IsVisible
            && PresentationSource.FromVisual(view) is not null
            && window.Content is DockPanel panel && panel.Children.Contains(view));
        var inventory = trace.Calls.Select(c => c.Result).OfType<InventoryPage>().Last();
        var byteCounters = inventory.Bounds.GetType().GetProperties()
            .Where(p => p.Name.Contains("Byte", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.GetValue(inventory.Bounds)).OfType<long>().ToArray();
        evidence.Check("inventory-metadata", byteCounters.Length > 0 && byteCounters.All(n => n == 0),
            new { inventory.Bounds, inventory.NextOffset, inventory.Request });
        if (membership.Paths is null)
        {
            evidence.Event("membership-unavailable-visible", new { membership.Reason, view.StatusText, view.BoundsText });
            Capture(window, evidence, "membership-unavailable.png");
            throw new ProofFailure("MEMBERSHIP-UNAVAILABLE");
        }

        var containers = await FileContainers(view.FilesControl);
        if (containers.Count < 2)
            throw new ProofFailure("TWO-RENDERED-CSHARP-FILES-REQUIRED");
        var first = containers[0];
        var second = containers[1];
        var fileA = (AtlasFileNode)first.DataContext;
        var fileB = (AtlasFileNode)second.DataContext;
        var before = trace.Calls.Count;
        SelectTree(first, focus: true);
        var a = await Accepted(trace, view, before);
        evidence.Check("first-file", view.FilesControl.SelectedItem is AtlasFileNode selected
            && selected.FileValue == a.FileValue && a.FileValue == fileA.FileValue);
        if (options.OracleFault == "source")
            view.SourceControl.Text = "Synthetic oracle fault: control differs from the original query page.";
        CompareSource(view, a, evidence, "rendered-source");
        evidence.Check("unicode-bom", !options.Prove || (view.SourceText.Contains("\U0001f9ed", StringComparison.Ordinal)
            && !view.SourceText.StartsWith('\ufeff')));

        var declaration = a.Outline.Declarations.FirstOrDefault(d => d.Kind == AtlasDeclarationKind.Method)
            ?? throw new ProofFailure("REAL-METHOD-OBSERVATION-REQUIRED");
        var row = view.OutlineRows.FirstOrDefault(r => r.ObservationKey == declaration.ObservationKey)
            ?? throw new ProofFailure("RENDERED-OUTLINE-OBSERVATION-MISSING");
        view.OutlineControl.ScrollIntoView(row);
        view.OutlineControl.UpdateLayout();
        var memberContainer = view.OutlineControl.ItemContainerGenerator.ContainerFromItem(row) as ListBoxItem
            ?? throw new ProofFailure("OUTLINE-CONTAINER-MISSING");
        var memberPeer = OutlinePeer(view.OutlineControl, memberContainer);
        var memberSelection = memberPeer.GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider
            ?? throw new ProofFailure("OUTLINE-UIA-MISSING");
        memberSelection.Select();
        memberPeer.SetFocus();
        before = trace.Calls.Count;
        view.OutlineControl.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
            PresentationSource.FromVisual(view.OutlineControl)!, Environment.TickCount, Key.Enter)
            { RoutedEvent = Keyboard.KeyDownEvent });
        var member = await Accepted(trace, view, before);
        var memberCall = trace.Calls.Last(c => ReferenceEquals(c.Result, member));
        evidence.Check("member", memberCall.Request is SelectionRequest request
            && request.DeclarationObservationKey == declaration.ObservationKey
            && request.FileValue == a.FileValue && member.Source.Page!.Highlights.Length > 0,
            new { declaration.ObservationKey, declaration.Kind, declaration.Span });
        evidence.Check("member-manifest", member.ManifestToken == a.ManifestToken
            && ((SelectionRequest)memberCall.Request!).ManifestToken == a.ManifestToken);
        CompareSource(view, member, evidence, "member-source-correspondence");
        view.OutlineControl.Focus();
        var selectionStart = view.SourceControl.SelectionStart;
        var selectionLength = view.SourceControl.SelectionLength;
        var scroll = view.SourceControl.VerticalOffset;
        evidence.Event("pre-different-file-state", new
        {
            focusedType = Keyboard.FocusedElement?.GetType().FullName,
            outlineFocus = view.OutlineControl.IsKeyboardFocusWithin,
            selectedOutlineObservation = (view.OutlineControl.SelectedItem as OutlineRow)?.ObservationKey,
            memberSourceObservation = member.Source.ObservationKey,
            selectionStart, selectionLength, scroll
        });
        before = trace.Calls.Count;
        SelectTree(second, focus: false);
        var b = await Accepted(trace, view, before);
        evidence.Check("different-file", b.FileValue == fileB.FileValue && b.FileValue != member.FileValue
            && ((AtlasFileNode)view.FilesControl.SelectedItem).FileValue == b.FileValue);
        evidence.Check("different-manifest", b.ManifestToken != member.ManifestToken
            && ((SelectionRequest)trace.Calls.Last(c => ReferenceEquals(c.Result, b)).Request!).ManifestToken == member.ManifestToken);
        CompareSource(view, b, evidence, "different-source-correspondence");
        var selects = trace.Calls.Count(c => c.Kind == "select");
        before = trace.Calls.Count;
        InvokeBack(view);
        var restored = await Accepted(trace, view, before);
        var restoreCall = trace.Calls.Last(c => ReferenceEquals(c.Result, restored));
        evidence.Check("back-receipt", restoreCall.Kind == "restore" && restoreCall.Receipt == member.ReceiptToken,
            new { issued = member.ReceiptToken, used = restoreCall.Receipt });
        evidence.Check("back-source", restored.FileValue == member.FileValue
            && restored.ManifestToken == member.ManifestToken
            && restored.Source.ObservationKey == member.Source.ObservationKey
            && JsonSerializer.Serialize(restored.Source.ExpectedBinding, Json)
                == JsonSerializer.Serialize(member.Source.ExpectedBinding, Json));
        CompareSource(view, restored, evidence, "back-source-correspondence");
        await Paint();
        evidence.Check("back-focus", view.OutlineControl.IsKeyboardFocusWithin
            && view.OutlineControl.SelectedItem is OutlineRow restoredRow
            && restoredRow.ObservationKey == declaration.ObservationKey
            && view.SourceControl.SelectionStart == selectionStart
            && view.SourceControl.SelectionLength == selectionLength
            && Math.Abs(view.SourceControl.VerticalOffset - scroll) < 1,
            new { focusedType = Keyboard.FocusedElement?.GetType().FullName, selectionStart, selectionLength, scroll,
                actualOutlineFocus = view.OutlineControl.IsKeyboardFocusWithin,
                actualOutlineObservation = (view.OutlineControl.SelectedItem as OutlineRow)?.ObservationKey,
                actualSelectionStart = view.SourceControl.SelectionStart,
                actualSelectionLength = view.SourceControl.SelectionLength,
                actualScroll = view.SourceControl.VerticalOffset });
        evidence.Check("no-fresh-back-select", trace.Calls.Count(c => c.Kind == "select") == selects);
        evidence.Check("controls-uia", view.FilesControl.IsVisible && view.OutlineControl.IsVisible
            && view.BackButton.IsVisible
            && view.OutlineControl.ItemContainerGenerator.ContainerFromItem(view.OutlineControl.SelectedItem) is ListBoxItem currentMember
            && OutlinePeer(view.OutlineControl, currentMember).GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider currentSelection
            && currentSelection.IsSelected);
        evidence.Check("pass-through", trace.Calls.All(c => ReferenceEquals(c.OriginalTask, c.ForwardedTask))
            && trace.Calls.Where(c => c.Result is not null).All(c =>
                ReferenceEquals(c.Result, c.OriginalTask.GetType().GetProperty("Result")!.GetValue(c.OriginalTask))),
            new { requests = trace.Calls.Count, results = trace.Calls.Count(c => c.Result is not null) });
        StartupObservation(application, window, evidence);
        Capture(window, evidence, "journey-rendered.png");

        if (!options.Prove)
        {
            evidence.Event("real-root-not-mutated", new { authorProof = false, destructiveSyntheticCases = "NOT_APPLICABLE" });
            return;
        }
        var sourcePath = ChildPath(gate.Expected.ApprovedAbsoluteRoot, fileA.RelativePath);
        await File.AppendAllTextAsync(sourcePath, "\r\n// changed after the issued receipt\r\n", token);
        before = trace.Calls.Count;
        InvokeBack(view);
        var changed = await Published(trace, view, before);
        evidence.Check("changed-clears", changed.Source.State == SourceProjectionState.Changed
            && view.SourceText.Length == 0 && view.CurrentHighlights.Count == 0
            && changed.Source.Page is null, new { view.SourceStatusText });
        Capture(window, evidence, "changed-rendered.png");
        File.Delete(ChildPath(gate.Expected.ApprovedAbsoluteRoot, fileB.RelativePath));
        before = trace.Calls.Count;
        SelectTree(second, focus: false);
        var unavailable = await Published(trace, view, before);
        evidence.Check("unavailable-clears", unavailable.Source.State is SourceProjectionState.Unavailable
            && view.SourceText.Length == 0 && view.CurrentHighlights.Count == 0
            && unavailable.Source.Page is null, new { view.SourceStatusText });
        Capture(window, evidence, "unavailable-rendered.png");
        before = trace.Calls.Count;
        try { await view.LoadAsync(new CancellationToken(canceled: true)); }
        catch (OperationCanceledException) { }
        await Paint();
        var canceledPage = trace.Calls.Skip(before).Select(c => c.Result).OfType<InventoryPage>().SingleOrDefault();
        evidence.Check("canceled", canceledPage is not null && canceledPage.Files.IsEmpty
            && canceledPage.NextOffset is null && canceledPage.Bounds.OmissionReason == "atlas.query.canceled"
            && canceledPage.Bounds.TotalState == AtlasDenominatorState.Unknown && canceledPage.Bounds.TotalCount is null
            && view.SourceText.Length == 0 && view.CurrentHighlights.Count == 0
            && view.StatusText.Contains("cancel", StringComparison.OrdinalIgnoreCase),
            new { view.StatusText, canceledPage?.Bounds, canceledPage?.NextOffset });

        var noGitRoot = Path.Combine(Path.GetDirectoryName(options.Output)!, "non-git-scope");
        Directory.CreateDirectory(noGitRoot);
        var noGitRecord = gate.Expected with { ApprovedAbsoluteRoot = noGitRoot, RootToken = "synthetic-no-git-root" };
        var noGitGate = WriteApproval(noGitRecord, Path.Combine(Path.GetDirectoryName(options.Output)!, "non-git-approval.json"), evidence);
        var failedMembership = await Membership(noGitRoot, options.ExpectedCommit, evidence, token, expectedFailure: true);
        using var failedHandle = await AtlasProofComposition.CreateForApprovedRootAsync(noGitRecord, noGitGate.Current,
            failedMembership.Paths, failedMembership.Complete, failedMembership.Reason, token);
        var failedView = new AtlasReaderView(failedHandle.Queries, failedHandle.InitialManifestToken);
        Present(window, failedView, failedMembership.Reason);
        await failedView.LoadAsync(token);
        await Paint();
        evidence.Check("membership-unavailable", failedMembership.Paths is null && !failedMembership.Complete
            && failedView.FileRoots.Count == 0 && window.Content is DockPanel failurePanel
            && failurePanel.Children.OfType<TextBlock>().Single().Text.Contains(failedMembership.Reason, StringComparison.Ordinal),
            new { failedMembership.Reason, failedView.StatusText, failedView.BoundsText });
        Capture(window, evidence, "membership-unavailable-rendered.png");
    }

    private static void Present(Window window, AtlasReaderView view, string membershipReason)
    {
        var panel = new DockPanel();
        var reason = new TextBlock { Text = "Proof membership: " + membershipReason, TextWrapping = TextWrapping.Wrap };
        reason.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        panel.SetResourceReference(Panel.BackgroundProperty, "SurfaceBrush");
        DockPanel.SetDock(reason, Dock.Top);
        panel.Children.Add(reason);
        panel.Children.Add(view);
        window.Content = panel;
    }

    private static ResourceDictionary ExistingBrushes()
    {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("Proof.AppResources.xaml")
            ?? throw new ProofFailure("EXISTING-RESOURCES-MISSING");
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var document = XDocument.Load(reader);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        var definitions = document.Root!.Element(presentation + "Application.Resources")!;
        var resources = new ResourceDictionary();
        foreach (var key in new[] { "SurfaceBrush", "SurfaceSunkenBrush", "TextBrush", "TextMutedBrush" })
        {
            var definition = definitions.Elements(presentation + "SolidColorBrush")
                .Single(e => (string?)e.Attribute(xaml + "Key") == key);
            resources.Add(key, new BrushConverter().ConvertFromInvariantString((string)definition.Attribute("Color")!)!);
        }
        return resources;
    }

    private static async Task<List<TreeViewItem>> FileContainers(ItemsControl parent)
    {
        var found = new List<TreeViewItem>();
        var pending = new Queue<ItemsControl>();
        pending.Enqueue(parent);
        var visited = 0;
        while (pending.TryDequeue(out var control))
        {
            if (++visited > 256) throw new ProofFailure("TREE-PROOF-BOUND");
            control.UpdateLayout();
            for (var i = 0; i < control.Items.Count; i++)
            {
                var item = control.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                if (item is null) continue;
                if (control.Items[i] is not AtlasFileNode node) continue;
                if (node.IsFile && node.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    found.Add(item);
                else if (!node.IsFile)
                {
                    var peer = new TreeViewItemAutomationPeer(item);
                    (peer.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider
                        ?? throw new ProofFailure("TREE-EXPAND-UIA-MISSING")).Expand();
                    await Paint();
                    pending.Enqueue(item);
                }
            }
        }
        return found;
    }

    private static void SelectTree(TreeViewItem item, bool focus)
    {
        var peer = new TreeViewItemAutomationPeer(item);
        if (focus) peer.SetFocus();
        (peer.GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider
            ?? throw new ProofFailure("TREE-SELECTION-UIA-MISSING")).Select();
    }

    private static void InvokeBack(AtlasReaderView view) =>
        (new ButtonAutomationPeer(view.BackButton).GetPattern(PatternInterface.Invoke) as IInvokeProvider
            ?? throw new ProofFailure("BACK-UIA-MISSING")).Invoke();

    private static ListBoxItemAutomationPeer OutlinePeer(ListBox selector, ListBoxItem container)
    {
        var item = selector.ItemContainerGenerator.ItemFromContainer(container);
        if (item == DependencyProperty.UnsetValue || !ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(container), selector))
            throw new ProofFailure("OUTLINE-CONTAINER-OWNER-MISMATCH");
        var owner = UIElementAutomationPeer.CreatePeerForElement(selector) as SelectorAutomationPeer
            ?? throw new ProofFailure("OUTLINE-SELECTOR-UIA-MISSING");
        return new ListBoxItemAutomationPeer(item, owner);
    }

    private static async Task<SelectionProjection> Published(TraceQueries trace, AtlasReaderView view, int start)
    {
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(10))
        {
            await Paint();
            var completed = trace.Calls.Skip(start).LastOrDefault(c => c.Result is SelectionProjection);
            if (completed?.Result is SelectionProjection result && !view.StatusText.StartsWith("Loading", StringComparison.Ordinal)
                && !view.StatusText.StartsWith("Restoring", StringComparison.Ordinal))
                return result;
            if (trace.Calls.Skip(start).Any(c => c.OriginalTask.IsFaulted))
                throw new ProofFailure("QUERY-FAULTED");
            await Task.Delay(15);
        }
        throw new ProofFailure("UI-PUBLICATION-TIMEOUT");
    }

    private static async Task<SelectionProjection> Accepted(TraceQueries trace, AtlasReaderView view, int start)
    {
        var result = await Published(trace, view, start);
        if (result.Source.State != SourceProjectionState.IndexedMatch || result.Source.Page is null)
            throw new ProofFailure("INDEXED-MATCH-REQUIRED");
        return result;
    }

    private static async Task Paint()
    {
        await Dispatcher.Yield(DispatcherPriority.Background);
        await Dispatcher.Yield(DispatcherPriority.ContextIdle);
    }

    private static void CompareSource(AtlasReaderView view, SelectionProjection result, Evidence evidence, string check)
    {
        var page = result.Source.Page!;
        view.SourceControl.TextArea.TextView.EnsureVisualLines();
        evidence.Check(check, view.SourceText == page.Text && view.CurrentHighlights.SequenceEqual(page.Highlights)
            && view.SourceControl.TextArea.TextView.VisualLines.Count > 0
            && page.Highlights.All(h => h.Start >= page.PageSpan.Start && h.End <= page.PageSpan.End)
            && (string?)typeof(AtlasReaderView).GetField("_manifestToken", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)
                == result.ManifestToken,
            new { result.FileValue, result.ManifestToken, result.Source.ObservationKey, page.PageSpan,
                page.Highlights, displayedUtf16Length = view.SourceText.Length, renderedLines = view.SourceControl.TextArea.TextView.VisualLines.Count,
                displayedTextSha256 = Hash(Encoding.UTF8.GetBytes(view.SourceText)) });
    }

    private static void Capture(Window window, Evidence evidence, string name)
    {
        window.UpdateLayout();
        var width = checked((int)Math.Ceiling(window.ActualWidth));
        var height = checked((int)Math.Ceiling(window.ActualHeight));
        if (!window.IsVisible || width is < 1 or > 1600 || height is < 1 or > 1200)
            throw new ProofFailure("CAPTURE-WINDOW-BOUND");
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var pixels = new byte[checked(width * height * 4)];
        bitmap.CopyPixels(pixels, width * 4, 0);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var path = Path.Combine(evidence.Directory, name);
        using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            encoder.Save(output);
        using var input = File.OpenRead(path);
        var decoded = BitmapDecoder.Create(input, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        evidence.Check("rendered-capture", pixels.Any(b => b != 0) && pixels.Distinct().Take(3).Count() >= 3
            && decoded.Frames.Single().PixelWidth == width && decoded.Frames.Single().PixelHeight == height,
            new { kind = "proof-window-rendered-capture-not-desktop", file = name, width, height, sha256 = Hash(File.ReadAllBytes(path)) });
    }

    private static void StartupObservation(Application application, Window window, Evidence evidence)
    {
        var windows = application.Windows.Cast<Window>().ToArray();
        var children = ChildProcesses();
        evidence.Check("plain-startup", application.GetType() == typeof(Application) && application.StartupUri is null
            && ReferenceEquals(application.MainWindow, window) && windows.Length == 1 && ReferenceEquals(windows[0], window)
            && children.Count == 0,
            new { applicationType = application.GetType().FullName, productMainWindows = windows.Count(w => w.GetType().FullName == "AiDe.App.MainWindow"),
                unrelatedWindows = windows.Count(w => !ReferenceEquals(w, window)), childProcesses = children.Count,
                appStartupInstantiated = application.GetType().FullName == "AiDe.App.App" });
    }

    private static List<uint> ChildProcesses()
    {
        using var snapshot = CreateToolhelp32Snapshot(2, 0);
        if (snapshot.IsInvalid) throw new ProofFailure("PROCESS-OBSERVATION-UNAVAILABLE");
        var entry = new ProcessEntry { Size = (uint)Marshal.SizeOf<ProcessEntry>() };
        var children = new List<uint>();
        if (!Process32FirstW(snapshot, ref entry)) throw new ProofFailure("PROCESS-OBSERVATION-UNAVAILABLE");
        do { if (entry.ParentId == (uint)Environment.ProcessId) children.Add(entry.ProcessId); }
        while (Process32NextW(snapshot, ref entry));
        return children;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry
    {
        public uint Size, Usage, ProcessId;
        public nuint Heap;
        public uint ModuleId, Threads, ParentId;
        public int Priority;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string Executable;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeFileHandle CreateToolhelp32Snapshot(uint flags, uint processId);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32FirstW(SafeFileHandle snapshot, ref ProcessEntry entry);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32NextW(SafeFileHandle snapshot, ref ProcessEntry entry);

    private static async Task<ApprovalGate> SyntheticApproval(Options options, Evidence evidence, CancellationToken token)
    {
        var root = Path.Combine(Path.GetDirectoryName(options.Output)!, "synthetic-scope");
        if (Directory.Exists(root)) throw new ProofFailure("SYNTHETIC-SCOPE-MUST-BE-NEW");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "Alpha.cs"),
            "namespace Proof;\r\npublic class Alpha { public string M() => \"\U0001f9ed\"; }\r\n", new UTF8Encoding(true), token);
        await File.WriteAllTextAsync(Path.Combine(root, "Beta.cs"),
            "namespace Proof;\r\npublic class Beta { public int N() => 2; }\r\n", new UTF8Encoding(false), token);
        await Git(root, ["init", "--quiet"], evidence, token);
        await Git(root, ["add", "--", "Alpha.cs", "Beta.cs"], evidence, token);
        await Git(root, ["-c", "user.name=Atlas Synthetic Proof", "-c", "user.email=atlas-proof@example.invalid",
            "commit", "--quiet", "--no-gpg-sign", "-m", "Synthetic Atlas proof fixture"], evidence, token);
        options.ExpectedCommit = Encoding.UTF8.GetString(await Git(root, ["rev-parse", "--verify", "HEAD"], evidence, token)).Trim();
        var approval = new RecordedRootApproval("synthetic-proof-decision", "1", "synthetic-workspace", "synthetic-root",
            "synthetic-git-membership-only", "atlas-live-runner-astra", root, DateTimeOffset.UtcNow.AddMinutes(5));
        evidence.Event("synthetic-authority", new { approval.DecisionRecordToken, approval.GrantVersion,
            approval.WorkspaceToken, approval.RootToken, approval.PolicyToken, approval.SessionToken, approval.ExpiresAt });
        return WriteApproval(approval, Path.Combine(Path.GetDirectoryName(options.Output)!, "approval.json"), evidence);
    }

    private static ApprovalGate ExternalApproval(Options options, Evidence evidence)
    {
        var values = options.Values;
        var root = values["root"];
        if (!string.Equals(root, ApprovedFutureRoot, StringComparison.Ordinal))
            throw new ProofFailure("EXTERNAL-ROOT-NOT-ADMITTED");
        ValidateDirectory(root);
        if (IsInside(root, options.Output) || IsInside(root, values["record"]))
            throw new ProofFailure("PROOF-OUTPUT-OR-AUTHORITY-IN-SCOPE");
        if (options.ExpectedCommit.Length != 40 || options.ExpectedCommit.Any(c => !Uri.IsHexDigit(c)))
            throw new ProofFailure("EXACT-COMMIT-REQUIRED");
        var approval = new RecordedRootApproval(values["decision"], values["grant"], values["workspace"],
            values["root-token"], values["policy"], values["session"], root,
            DateTimeOffset.Parse(values["expires"], System.Globalization.CultureInfo.InvariantCulture));
        return new ApprovalGate(approval, values["record"], values["record-sha256"], evidence);
    }

    private static ApprovalGate WriteApproval(RecordedRootApproval approval, string path, Evidence evidence)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(ApprovalRecord.From(approval), Json);
        File.WriteAllBytes(path, bytes);
        return new ApprovalGate(approval, path, Hash(bytes), evidence);
    }

    private sealed record ApprovalRecord(int SchemaVersion, string DecisionRecordToken, string GrantVersion,
        string WorkspaceToken, string RootToken, string PolicyToken, string SessionToken,
        string ApprovedAbsoluteRoot, DateTimeOffset ExpiresAt, bool Revoked)
    {
        [JsonIgnore]
        public RecordedRootApproval Grant => new(DecisionRecordToken, GrantVersion, WorkspaceToken, RootToken,
            PolicyToken, SessionToken, ApprovedAbsoluteRoot, ExpiresAt);
        public static ApprovalRecord From(RecordedRootApproval grant) => new(1, grant.DecisionRecordToken,
            grant.GrantVersion, grant.WorkspaceToken, grant.RootToken, grant.PolicyToken, grant.SessionToken,
            grant.ApprovedAbsoluteRoot, grant.ExpiresAt, false);
    }

    private sealed class ApprovalGate(RecordedRootApproval expected, string path, string hash, Evidence evidence)
    {
        public RecordedRootApproval Expected { get; } = expected;
        public string Path { get; } = System.IO.Path.GetFullPath(path);
        public string HashValue { get; } = hash;
        public bool Current(RecordedRootApproval candidate)
        {
            var reason = "APPROVAL-UNREADABLE";
            try
            {
                if (candidate != Expected) return Refuse("APPROVAL-CANDIDATE-FIELDS");
                using var input = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (input.Length > 16 * 1024) return Refuse("APPROVAL-OVERSIZED");
                var bytes = new byte[16 * 1024 + 1];
                var count = 0;
                int read;
                while (count < bytes.Length && (read = input.Read(bytes, count, bytes.Length - count)) > 0)
                    count += read;
                if (count > 16 * 1024) return Refuse("APPROVAL-OVERSIZED");
                reason = "APPROVAL-MALFORMED";
                using var document = JsonDocument.Parse(bytes.AsMemory(0, count));
                var properties = document.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
                if (properties.Length != 10 || properties.Distinct(StringComparer.Ordinal).Count() != 10)
                    return Refuse("APPROVAL-SCHEMA");
                var record = JsonSerializer.Deserialize<ApprovalRecord>(bytes.AsSpan(0, count), Json);
                if (record is null || record.SchemaVersion != 1) return Refuse("APPROVAL-SCHEMA");
                if (record.Revoked) return Refuse("APPROVAL-REVOKED");
                if (record.ExpiresAt.Offset != TimeSpan.Zero || DateTimeOffset.UtcNow >= record.ExpiresAt)
                    return Refuse("APPROVAL-EXPIRED");
                if (record.Grant != Expected) return Refuse("APPROVAL-RECORD-FIELDS");
                if (!string.Equals(Hash(bytes.AsSpan(0, count)), HashValue, StringComparison.OrdinalIgnoreCase))
                    return Refuse("APPROVAL-HASH");
                return true;
            }
            catch (FileNotFoundException) { return Refuse("APPROVAL-MISSING"); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException
                or InvalidOperationException or ArgumentException or NotSupportedException)
            { return Refuse(reason); }
        }
        private bool Refuse(string code)
        {
            evidence.Event("approval-refusal", new { code, scope = Expected.RootToken });
            return false;
        }
    }

    private static void AuthorizationNegatives(ApprovalGate gate, Evidence evidence)
    {
        var original = File.ReadAllBytes(gate.Path);
        var record = ApprovalRecord.From(gate.Expected);
        try
        {
            File.Move(gate.Path, gate.Path + ".held");
            evidence.Check("approval-missing", !gate.Current(gate.Expected));
            File.Move(gate.Path + ".held", gate.Path);
            using (var locked = new FileStream(gate.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                evidence.Check("approval-unreadable", !gate.Current(gate.Expected));
            File.WriteAllBytes(gate.Path, new byte[16 * 1024 + 1]);
            evidence.Check("approval-oversized", !gate.Current(gate.Expected));
            File.WriteAllText(gate.Path, "{");
            evidence.Check("approval-malformed", !gate.Current(gate.Expected));
            foreach (var changed in new[]
            {
                record with { SchemaVersion = 2 }, record with { DecisionRecordToken = "other" },
                record with { GrantVersion = "other" }, record with { WorkspaceToken = "other" },
                record with { RootToken = "other" }, record with { PolicyToken = "other" },
                record with { SessionToken = "other" }, record with { ApprovedAbsoluteRoot = record.ApprovedAbsoluteRoot + "-other" },
                record with { ExpiresAt = record.ExpiresAt.AddSeconds(1) },
                record with { ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) }, record with { Revoked = true }
            })
            {
                File.WriteAllBytes(gate.Path, JsonSerializer.SerializeToUtf8Bytes(changed, Json));
                evidence.Check("approval-field-refusal-" + Guid.NewGuid().ToString("N"), !gate.Current(gate.Expected));
            }
            File.WriteAllBytes(gate.Path, original);
            evidence.Check("approval-hash", !new ApprovalGate(gate.Expected, gate.Path, new string('0', 64), evidence).Current(gate.Expected));
            evidence.Check("approval-candidate", !gate.Current(gate.Expected with { SessionToken = "other" }));
            evidence.Check("authorization-refusals", gate.Current(gate.Expected));
        }
        finally
        {
            File.WriteAllBytes(gate.Path, original);
            if (File.Exists(gate.Path + ".held")) File.Delete(gate.Path + ".held");
        }
    }

    private sealed record MembershipResult(ImmutableArray<string>? Paths, bool Complete, string Reason);

    private static async Task<MembershipResult> Membership(string root, string expectedCommit, Evidence evidence,
        CancellationToken token, bool expectedFailure = false)
    {
        try
        {
            var version = Encoding.UTF8.GetString(await Git(root, ["--version"], evidence, token)).Trim();
            evidence.Check("git-version", version == GitVersion, new { version, executable = GitExecutable });
            var revision = Encoding.UTF8.GetString(await Git(root, ["rev-parse", "--verify", "HEAD"], evidence, token)).Trim();
            var status = await Git(root, ["status", "--porcelain=v1", "-z", "--untracked-files=all", "--ignore-submodules=all", "--", "."], evidence, token);
            if (revision != expectedCommit || status.Length != 0) throw new ProofFailure("GIT-REVISION-OR-DIRTY-SCOPE");
            var bytes = await Git(root, ["ls-files", "--cached", "-z", "--", "."], evidence, token);
            if (bytes.Length > 0 && bytes[^1] != 0) throw new ProofFailure("GIT-NUL-TERMINATOR");
            var text = new UTF8Encoding(false, true).GetString(bytes);
            var paths = text.Split('\0', StringSplitOptions.RemoveEmptyEntries);
            foreach (var path in paths) _ = ChildPath(root, path);
            if (paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != paths.Length)
                throw new ProofFailure("GIT-DUPLICATE-MEMBERSHIP");
            if (!expectedFailure)
                evidence.Check("git-clean-revision", true, new { revision, dirtyBytes = status.Length, membershipCount = paths.Length });
            return new(paths.ToImmutableArray(), true, "GIT-MEMBERSHIP-VERIFIED");
        }
        catch (Exception error) when (error is ProofFailure or IOException or UnauthorizedAccessException
            or OperationCanceledException or System.ComponentModel.Win32Exception or DecoderFallbackException)
        {
            var code = error is ProofFailure failure ? failure.Code : "GIT-UNAVAILABLE";
            evidence.Event("membership-unavailable", new { code, fallback = false });
            return new(null, false, code);
        }
    }

    private static async Task<byte[]> Git(string root, string[] arguments, Evidence evidence, CancellationToken token)
    {
        ValidateDirectory(root);
        if (!File.Exists(GitExecutable) || (File.GetAttributes(GitExecutable) & FileAttributes.ReparsePoint) != 0)
            throw new ProofFailure("GIT-EXECUTABLE-REFUSED");
        var start = new ProcessStartInfo(GitExecutable)
        {
            WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var key in start.Environment.Keys.Where(k => k.StartsWith("GIT_", StringComparison.OrdinalIgnoreCase)).ToArray())
            start.Environment.Remove(key);
        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["GIT_CONFIG_GLOBAL"] = "NUL";
        start.Environment["GIT_CONFIG_SYSTEM"] = "NUL";
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        start.Environment["GIT_OPTIONAL_LOCKS"] = "0";
        start.Environment["GIT_ALLOW_PROTOCOL"] = "";
        foreach (var argument in new[]
        {
            "--no-pager", "--no-optional-locks", "-c", "core.hooksPath=NUL", "-c", "core.fsmonitor=false",
            "-c", "core.untrackedCache=false", "-c", "core.askPass=", "-c", "credential.helper=",
            "-c", "protocol.allow=never", "-c", "submodule.recurse=false", "-c", "diff.external=",
            "-c", "commit.gpgSign=false", "-c", "core.quotePath=false"
        }) start.ArgumentList.Add(argument);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        var watch = Stopwatch.StartNew();
        using var process = new Process { StartInfo = start };
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromSeconds(10));
        if (!process.Start()) throw new ProofFailure("GIT-START");
        try
        {
            var stdout = ReadBounded(process.StandardOutput.BaseStream, 2 * 1024 * 1024, budget.Token);
            var stderr = ReadBounded(process.StandardError.BaseStream, 16 * 1024, budget.Token);
            await Task.WhenAll(stdout, stderr, process.WaitForExitAsync(budget.Token)).WaitAsync(budget.Token);
            evidence.Event("git", new { operation = arguments[0], process.ExitCode, durationMs = watch.Elapsed.TotalMilliseconds,
                stdoutBytes = stdout.Result.Length, stderrBytes = stderr.Result.Length });
            if (process.ExitCode != 0) throw new ProofFailure("GIT-EXIT");
            return stdout.Result;
        }
        catch
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
    }

    private static async Task<byte[]> ReadBounded(Stream stream, int maximum, CancellationToken token)
    {
        using var output = new MemoryStream();
        var buffer = new byte[4096];
        int count;
        while ((count = await stream.ReadAsync(buffer, token)) != 0)
        {
            if (output.Length + count > maximum) throw new ProofFailure("GIT-STREAM-LIMIT");
            output.Write(buffer, 0, count);
        }
        return output.ToArray();
    }

    private static string ChildPath(string root, string relative)
    {
        if (Path.IsPathRooted(relative) || relative.Split(['/', '\\']).Any(s => s is "" or "." or ".."))
            throw new ProofFailure("MEMBERSHIP-PATH-REFUSED");
        var child = Path.GetFullPath(Path.Combine(root, relative));
        if (!IsInside(root, child)) throw new ProofFailure("MEMBERSHIP-PATH-ESCAPE");
        return child;
    }

    private static bool IsInside(string root, string path) => Path.GetFullPath(path).StartsWith(
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        || string.Equals(Path.GetFullPath(root), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase);

    private static void ValidateDirectory(string path)
    {
        for (var current = new DirectoryInfo(Path.GetFullPath(path)); current is not null; current = current.Parent)
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ProofFailure("REPARSE-DIRECTORY-REFUSED");
    }

    private static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private sealed class Options
    {
        public required bool Prove { get; init; }
        public required string Output { get; init; }
        public required Dictionary<string, string> Values { get; init; }
        public string ExpectedCommit { get; set; } = "";
        public string? OracleFault { get; init; }
        public static Options Parse(string[] args)
        {
            if (args.Length == 0 || args[0] is not ("--prove" or "--approved")) throw new ProofFailure("MODE-REQUIRED");
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 1; i < args.Length; i += 2)
            {
                if (i + 1 >= args.Length || !args[i].StartsWith("--", StringComparison.Ordinal)
                    || !values.TryAdd(args[i][2..], args[i + 1])) throw new ProofFailure("ARGUMENTS-REFUSED");
            }
            var prove = args[0] == "--prove";
            var allowed = prove ? new[] { "output", "oracle-fault" } :
                ["output", "root", "record", "record-sha256", "decision", "grant", "workspace", "root-token", "policy", "session", "expires", "expected-commit"];
            if (values.Keys.Except(allowed).Any() || (!prove && allowed.Any(k => !values.TryGetValue(k, out var value) || string.IsNullOrWhiteSpace(value))))
                throw new ProofFailure("EXACT-LAUNCH-EXPECTATIONS-REQUIRED");
            var output = Path.GetFullPath(values.GetValueOrDefault("output")
                ?? Path.Combine("artifacts", "atlas-reader", Guid.NewGuid().ToString("N"), "evidence"));
            var fault = values.GetValueOrDefault("oracle-fault");
            if (fault is not null && fault != "source") throw new ProofFailure("ORACLE-FAULT-UNKNOWN");
            if (!prove && (IsInside(values["root"], output) || IsInside(values["root"], values["record"])))
                throw new ProofFailure("OUTPUT-OR-AUTHORITY-IN-SCOPE");
            return new Options { Prove = prove, Output = output, Values = values,
                ExpectedCommit = values.GetValueOrDefault("expected-commit") ?? "", OracleFault = fault };
        }
    }

    private sealed class ObservedCall(string kind, object? request, string? receipt, Task original, Task forwarded)
    {
        public string Kind { get; } = kind;
        public object? Request { get; } = request;
        public string? Receipt { get; } = receipt;
        public Task OriginalTask { get; } = original;
        public Task ForwardedTask { get; } = forwarded;
        public object? Result { get; set; }
    }

    // Pattern: transparent decorator. Return the original Task, not a reconstructed result.
    private sealed class TraceQueries(IAtlasQueries inner, Evidence evidence) : IAtlasQueries
    {
        public List<ObservedCall> Calls { get; } = [];
        public Task<InventoryPage> InventoryAsync(PageRequest request, CancellationToken cancellationToken) =>
            Observe("inventory", request, null, inner.InventoryAsync(request, cancellationToken));
        public Task<SelectionProjection> SelectAsync(SelectionRequest request, CancellationToken cancellationToken) =>
            Observe("select", request, null, inner.SelectAsync(request, cancellationToken));
        public Task<SelectionProjection> RestoreAsync(string issuedReceiptToken, long requestSequence, CancellationToken cancellationToken) =>
            Observe("restore", new { requestSequence }, issuedReceiptToken, inner.RestoreAsync(issuedReceiptToken, requestSequence, cancellationToken));

        private Task<T> Observe<T>(string kind, object request, string? receipt, Task<T> original)
        {
            var call = new ObservedCall(kind, request, receipt, original, original);
            Calls.Add(call);
            evidence.Event("query-request", new { kind, request, receipt });
            _ = original.ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    call.Result = task.Result;
                    if (task.Result is SelectionProjection result)
                        evidence.Event("query-result", new { kind, result.ReceiptToken, result.ManifestToken,
                            result.FileValue, result.Generation, source = new { state = result.Source.State.ToString(),
                                result.Source.ObservationKey, result.Source.ExpectedBinding, result.Source.DecoderId,
                                pageSpan = result.Source.Page?.PageSpan, highlights = result.Source.Page?.Highlights,
                                textSha256 = result.Source.Page is { } page ? Hash(Encoding.UTF8.GetBytes(page.Text)) : null },
                            result.Outline, result.Bounds, result.Coverage });
                    else if (task.Result is InventoryPage page)
                        evidence.Event("inventory-result", new { page.Request, page.Bounds, page.NextOffset, files = page.Files.Length });
                }
                else evidence.Event("query-terminal", new { kind, canceled = task.IsCanceled, faulted = task.IsFaulted });
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return original;
        }
    }

    private sealed class Evidence
    {
        private readonly object gate = new();
        private readonly Stopwatch elapsed = Stopwatch.StartNew();
        private readonly Dictionary<string, string> checks;
        private bool finished;
        public string Directory { get; }
        public Evidence(string directory, bool prove)
        {
            Directory = directory;
            checks = RequiredChecks.ToDictionary(k => k, _ => "NOT_PROVEN", StringComparer.Ordinal);
            if (!prove)
                foreach (var key in new[] { "authorization-refusals", "unicode-bom", "changed-clears", "unavailable-clears", "canceled", "membership-unavailable" })
                    checks[key] = "NOT_APPLICABLE";
            Event("run-open", new { contract = "atlas-reader-author-proof/1", synthetic = prove, independentlyVerified = false });
        }
        public bool AllRequiredPassed { get { lock (gate) return checks.Values.All(v => v is "PASS" or "NOT_APPLICABLE"); } }
        public void Event(string kind, object value)
        {
            lock (gate)
                File.AppendAllText(Path.Combine(Directory, "events.jsonl"),
                    JsonSerializer.Serialize(new { kind, at = DateTimeOffset.UtcNow, elapsedMs = elapsed.Elapsed.TotalMilliseconds, value }, Json) + "\n");
        }
        public void Check(string name, bool passed, object? value = null)
        {
            lock (gate)
            {
                if (!checks.TryGetValue(name, out var prior) || prior != "FAIL")
                    checks[name] = passed ? "PASS" : "FAIL";
                Event("assertion", new { name, outcome = passed ? "PASS" : "FAIL", value });
            }
            if (!passed) throw new ProofFailure("ASSERT-" + name);
        }
        public void Finish(int exit)
        {
            lock (gate)
            {
                if (finished) return;
                finished = true;
                File.WriteAllText(Path.Combine(Directory, "summary.json"),
                    JsonSerializer.Serialize(new { exitCode = exit, durationMs = elapsed.Elapsed.TotalMilliseconds,
                        passed = checks.Count(p => p.Value == "PASS"), failed = checks.Count(p => p.Value == "FAIL"),
                        notProven = checks.Count(p => p.Value == "NOT_PROVEN"), checks }, Json));
                Console.WriteLine(JsonSerializer.Serialize(new { exitCode = exit, output = Directory,
                    passed = checks.Count(p => p.Value == "PASS"), failed = checks.Count(p => p.Value == "FAIL"),
                    notProven = checks.Count(p => p.Value == "NOT_PROVEN") }, Json));
            }
        }
    }

    private sealed class ProofFailure(string code) : Exception(code)
    {
        public string Code { get; } = code;
    }
}
