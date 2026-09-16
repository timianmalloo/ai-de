using AiDe.App.Workbench;
using AiDe.App.Workbench.Understanding;
using AiDe.Core.Understanding;
using AiDe.Core.Workbench;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Threading;
using Xunit.Abstractions;
using AiDe.App.ViewModels;
using AiDe.Core;
using AiDe.Core.Projections;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml.Linq;

namespace AiDe.App.Tests;

public sealed class AtlasSharedHostAdmissionTests(ITestOutputHelper output)
{
    private const string ReadingSample =
        "namespace ProofOwned;\npublic sealed class Widget\n{\n    public int Answer() => 42;\n}\n";

    [Fact]
    public void Placement_NewAtlas_DefaultsToCenter()
    {
        Sta.Run(() =>
        {
            using var shell = new WorkbenchShell(null);
            var opener = PerspectiveMenu.Opener(SurfaceContentFactory.Kinds.Single(row => row.Kind == "code-atlas"));
            Assert.True(shell.Execute(opener.Id));
            var atlas = Assert.Single(shell.Architecture.Service.Zones.AllSurfaces(), surface => surface.Kind == "code-atlas");
            Assert.Equal(ZoneId.Center, shell.Architecture.Service.Zones.FindZoneOf(atlas.SurfaceId));
        });
    }

    [Fact]
    public void Placement_ShowExplicitlyPlacedAtlas_PreservesRightAndIdentity()
    {
        Sta.Run(() =>
        {
            using var shell = new WorkbenchShell(null);
            var explicitSurface = new Surface("explicit-atlas", "code-atlas", "Code Atlas");
            Assert.True(shell.Architecture.Service.Apply(
                new LayoutOperation.AddSurface(ZonesToTree.RightStackId, explicitSurface)).Applied);
            shell.Architecture.Adapter.Render();
            var opener = PerspectiveMenu.Opener(SurfaceContentFactory.Kinds.Single(row => row.Kind == "code-atlas"));
            Assert.True(shell.Execute(opener.Id));
            Assert.Equal("explicit-atlas",
                Assert.Single(shell.Architecture.Service.Zones.AllSurfaces(), surface => surface.Kind == "code-atlas").SurfaceId);
            Assert.Equal(ZoneId.Right, shell.Architecture.Service.Zones.FindZoneOf("explicit-atlas"));
        });
    }

    [Theory]
    [InlineData(1280, 900)]
    [InlineData(1440, 900)] // DESIGN.md:1050's declared startup target.
    public async Task Placement_ShownDefault_ExposesSourceHighlightAndCompleteMemberLabel(int width, int height)
    {
        var diagnostic = new StageDiagnostics();
        var directory = Path.Combine(Path.GetTempPath(), "aide-atlas-placement-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await RunOwnedDispatcherAsync(async () =>
            {
                var root = Path.Combine(directory, "workspace");
                Directory.CreateDirectory(root);
                using var core = new ObservedCore(
                    WorkspaceCore.Open("placement-fixture", root, Path.Combine(directory, "data")), diagnostic);
                var port = new PortFixture { ReadingText = ReadingSample };
                var model = new MainWindowViewModel(new LocalWorkspaceQueries(core.Value.Projections),
                    "placement-display", null, atlasReaderFactory: () => port);
                var window = new AiDe.App.MainWindow(() => Task.FromResult(model),
                    Path.Combine(directory, "shell-state"), MainWindowResources())
                {
                    Width = width, Height = height, Left = 40, Top = 40,
                    WindowStartupLocation = WindowStartupLocation.Manual, ShowInTaskbar = false, ShowActivated = false,
                };
                try
                {
                    Assert.Equal(84, System.Text.Encoding.UTF8.GetByteCount(ReadingSample));
                    window.Shell.Execute(PerspectiveSet.Architecture.CommandId);
                    var title = PerspectiveMenu.Opener(SurfaceContentFactory.Kinds.Single(row => row.Kind == "code-atlas")).Title;
                    var menu = Assert.IsType<Menu>(window.FindName("MainMenu"));
                    Assert.Single(MenuItems(menu), item => Equals(item.Header, title))
                        .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                    window.Show();
                    await diagnostic.IdleAsync("placement.shown");
                    await window.WorkspaceReady;
                    window.UpdateLayout();
                    await diagnostic.IdleAsync("placement.loaded");
                    var host = Assert.Single(Visuals<AtlasLoadingHost>(window));
                    var view = Assert.IsType<AtlasReaderView>(host.ReaderView);
                    await view.SelectFileAsync(view.FileRoots.Single().Children.Single());
                    await view.SelectDeclarationAsync(Assert.Single(view.OutlineRows));
                    await diagnostic.IdleAsync("placement.selected");
                    window.UpdateLayout();
                    var atlas = Assert.Single(window.Shell.Architecture.Service.Zones.AllSurfaces(),
                        surface => surface.Kind == "code-atlas");
                    diagnostic.Mark("placement", $"fixture=true window={width}x{height} "
                        + $"zone={window.Shell.Architecture.Service.Zones.FindZoneOf(atlas.SurfaceId)} "
                        + $"atlasWidth={view.ActualWidth:F3} sourceBytes=84");
                    AssertReadingGeometry(window, view, diagnostic);
                }
                finally
                {
                    window.Close();
                    await window.CloseOperation;
                    await diagnostic.IdleAsync("placement.closed");
                }
            }, diagnostic, () =>
            {
                Directory.Delete(directory, recursive: true);
                diagnostic.Mark("placement.fixture.deleted");
            });
        }
        catch (Exception exception) { diagnostic.Failure("placement.primary", exception); }
        finally { diagnostic.Save($"atlas-placement-{width}.log", output); }
        diagnostic.ThrowIfFailed();
    }

    private static void AssertReadingGeometry(Window window, AtlasReaderView view, StageDiagnostics diagnostic)
    {
        var client = Assert.IsAssignableFrom<FrameworkElement>(window.Content);
        var clientBounds = client.TransformToAncestor(window).TransformBounds(new Rect(client.RenderSize));
        var textView = view.SourceControl.TextArea.TextView;
        textView.EnsureVisualLines();
        var viewport = ClippedReadingBounds(textView, window, clientBounds);
        Rect GlyphBounds(int start, int length)
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
        var lines = new List<Rect>();
        var offset = 0;
        foreach (var line in ReadingSample.Split('\n'))
        {
            if (line.Length > 0) lines.Add(GlyphBounds(offset, line.Length));
            offset += line.Length + 1;
        }
        var highlights = view.CurrentHighlights.Select(span => GlyphBounds(span.Start, span.Length)).ToArray();
        var item = Assert.IsType<ListBoxItem>(
            view.OutlineControl.ItemContainerGenerator.ContainerFromItem(view.OutlineControl.SelectedItem));
        var label = Assert.Single(Visuals<TextBlock>(item), block => block.Text.Contains("Answer", StringComparison.Ordinal));
        var drawing = VisualTreeHelper.GetDrawing(label);
        Assert.NotNull(drawing);
        var runs = RenderedLabelRuns(drawing, Matrix.Identity).ToArray();
        Assert.NotEmpty(runs);
        var labelGlyphs = runs.Select(run => label.TransformToAncestor(window).TransformBounds(run.Bounds)).ToArray();
        var labelViewport = ClippedReadingBounds(label, window, clientBounds, startAtLayoutBounds: false);
        var textFits = !viewport.IsEmpty && lines.All(viewport.Contains);
        var highlightFits = !viewport.IsEmpty && highlights.Length > 0 && highlights.All(viewport.Contains);
        var labelFits = !labelViewport.IsEmpty && labelGlyphs.All(labelViewport.Contains);
        var selected = Assert.IsType<OutlineRow>(view.OutlineControl.SelectedItem);
        Assert.Equal(selected.ToString(), label.Text);
        Assert.Equal(selected.AccessibleName, System.Windows.Automation.AutomationProperties.GetName(item));
        Assert.Equal(TextWrapping.Wrap, label.TextWrapping);
        Assert.Equal(TextTrimming.None, label.TextTrimming);
        Assert.True(double.IsFinite(label.ActualWidth) && label.ActualWidth > 0);
        Assert.True(labelViewport.Width <= view.OutlineControl.ActualWidth,
            "The label's actual clip must remain bounded by the outline viewport.");
        Assert.All(labelGlyphs, bounds => Assert.True(
            double.IsFinite(bounds.Width) && double.IsFinite(bounds.Height) && bounds.Width > 0 && bounds.Height > 0));
        // Wrapping can consume separator whitespace; every non-whitespace character must still be drawn.
        Assert.Equal(new string(label.Text.Where(character => !char.IsWhiteSpace(character)).ToArray()),
            new string(string.Concat(runs.Select(run => run.Characters))
                .Where(character => !char.IsWhiteSpace(character)).ToArray()));
        Assert.True(labelGlyphs.Select(bounds => bounds.Top).Distinct().Count() > 1,
            "The full sample label must be rendered on multiple lines, not clipped as one line.");
        diagnostic.Mark("geometry", $"unit=DIP client={clientBounds} sourceViewport={viewport} "
            + $"labelVisible={labelViewport} renderedRuns={runs.Length} fullText={label.Text} "
            + $"sourceFont={view.SourceControl.FontSize} labelFont={label.FontSize} "
            + $"textFits={textFits} highlightFits={highlightFits} labelFits={labelFits}");
        foreach (var bounds in labelGlyphs) diagnostic.Mark("geometry.label-run", bounds.ToString());
        foreach (var bounds in lines) diagnostic.Mark("geometry.source-line", bounds.ToString());
        foreach (var bounds in highlights) diagnostic.Mark("geometry.highlight", bounds.ToString());
        Assert.True(textFits, "Every source line must fit the client- and ancestor-clipped source text viewport.");
        Assert.True(highlightFits, "The selected member highlight must fit the visible source text viewport.");
        Assert.True(labelFits, "The complete selected member label must fit its clipped viewport.");
    }

    // GetDrawing returns the visual's drawing content; inspect its glyph runs rather than reformatting the model text.
    private static IEnumerable<(Rect Bounds, string Characters)> RenderedLabelRuns(Drawing drawing, Matrix parent)
    {
        if (drawing is DrawingGroup group)
        {
            Assert.True(group.Opacity > 0);
            var transform = group.Transform?.Value ?? Matrix.Identity;
            transform.Append(parent);
            foreach (var child in group.Children)
            {
                foreach (var run in RenderedLabelRuns(child, transform))
                {
                    if (group.ClipGeometry is { } clip)
                        Assert.True(new MatrixTransform(transform).TransformBounds(clip.Bounds).Contains(run.Bounds),
                            "A drawing-level clip must not hide any rendered label glyphs.");
                    yield return run;
                }
            }
        }
        else if (drawing is GlyphRunDrawing glyph)
        {
            Assert.NotNull(glyph.GlyphRun.Characters);
            yield return (new MatrixTransform(parent).TransformBounds(glyph.Bounds),
                new string(glyph.GlyphRun.Characters.ToArray()));
        }
    }

    private static Rect ClippedReadingBounds(FrameworkElement visual, Window window, Rect clientBounds,
        bool startAtLayoutBounds = true)
    {
        // A TextBlock may paint glyph overhang outside its layout box; only actual clips hide that ink.
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

    [Fact]
    public void Handoff_WorkbenchFactory_OwnsWorkspaceLifetimeBeforeAnyPaneOpens()
    {
        Sta.Run(() =>
        {
            using var shell = new WorkbenchShell(null);
            Assert.NotNull(Field<SurfaceContentFactory>(shell, "_factory").AtlasOwner);
        });
    }

    /// <summary>Scans MainWindow.xaml.cs only, non-recursively, for the two exact composition
    /// tokens below; no allowlist. The shown-window test supplies the runtime half.</summary>
    [Fact]
    public void Handoff_MainWindow_SourceConsumesCommittedFactoryAndAwaitsClose()
    {
        var source = File.ReadAllText(RepositoryFile("src", "AiDe.App", "MainWindow.xaml.cs"));
        Assert.Contains("workspace.AtlasReaderFactory", source, StringComparison.Ordinal);
        Assert.Contains("await Shell.DisposeAsync()", source, StringComparison.Ordinal);
    }

    private static string RepositoryFile(params string[] parts)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AiDe.sln"))
            && !Directory.Exists(Path.Combine(root.FullName, ".git")) && !File.Exists(Path.Combine(root.FullName, ".git")))
            root = root.Parent;
        return Path.Combine([root?.FullName ?? throw new InvalidOperationException("Repository root unavailable."), .. parts]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handoff_ShownMainWindow_OpenerReplacementAndCloseUseOwnedLease(bool failFirstClose)
    {
        var diagnostic = new StageDiagnostics();
        var directory = Path.Combine(Path.GetTempPath(), "aide-atlas-window-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        diagnostic.Mark("fixture.created", directory);
        try
        {
            await RunOwnedDispatcherAsync(async () =>
            {
                diagnostic.Mark("body.enter");
                using var diagnostics = new HostDiagnostics(text => diagnostic.Mark("trace", text));
                var root = Path.Combine(directory, "workspace");
                Directory.CreateDirectory(root);
                diagnostic.Mark("core.open.before");
                using var core = new ObservedCore(
                    WorkspaceCore.Open("window-fixture", root, Path.Combine(directory, "data")), diagnostic);
                diagnostic.Mark("core.open.after");
                var borrowed = new LocalWorkspaceQueries(core.Value.Projections);
                var first = new PortFixture { LeaseDisposal = new(TaskCreationOptions.RunContinuationsAsynchronously) };
                var second = new PortFixture { LeaseDisposal = new(TaskCreationOptions.RunContinuationsAsynchronously) };
                diagnostic.Track("first.lease.gate", first.LeaseDisposal.Task);
                diagnostic.Track("second.lease.gate", second.LeaseDisposal.Task);
                var firstFactories = 0;
                var secondFactories = 0;
                var firstModel = new MainWindowViewModel(borrowed, "display-not-authority", null,
                    atlasReaderFactory: () => { ++firstFactories; return first; });
                var secondModel = new MainWindowViewModel(borrowed, "replacement-display", null,
                    atlasReaderFactory: () => { ++secondFactories; return second; });
                var window = new AiDe.App.MainWindow(() => Task.FromResult(firstModel),
                    Path.Combine(directory, "shell-state"), MainWindowResources())
                {
                    ShowInTaskbar = false,
                    ShowActivated = false,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                try
                {
                    diagnostic.Mark("architecture.before");
                    window.Shell.Execute(PerspectiveSet.Architecture.CommandId);
                    var menu = Assert.IsType<Menu>(window.FindName("MainMenu"));
                    var title = PerspectiveMenu.Opener(
                        SurfaceContentFactory.Kinds.Single(kind => kind.Kind == "code-atlas")).Title;
                    var entry = Assert.Single(MenuItems(menu), item =>
                        string.Equals(item.Header?.ToString(), title, StringComparison.Ordinal));
                    entry.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                    diagnostic.Mark("window.show.before");
                    window.Show();
                    diagnostic.Track("workspace.ready.at-show", window.WorkspaceReady);
                    diagnostic.Mark("window.show.after", $"generation={Field<long>(window, "_workspaceGeneration")}");
                    await diagnostic.IdleAsync("yield.after-show");
                    await diagnostic.AwaitAsync("workspace.ready", window.WorkspaceReady);
                    window.UpdateLayout();
                    await diagnostic.IdleAsync("yield.after-layout");
                    var host = Assert.Single(Visuals<AtlasLoadingHost>(window));
                    var view = Assert.IsType<AtlasReaderView>(host.ReaderView);
                    await diagnostic.AwaitAsync("select", view.SelectFileAsync(view.FileRoots.Single().Children.Single()));
                    Assert.Equal("alpha beta", view.SourceText);
                    Assert.Equal(1, firstFactories);
                    Assert.Same(firstModel, window.DataContext);

                    diagnostic.Mark("apply.before", $"generation={Field<long>(window, "_workspaceGeneration")}");
                    var replacement = window.ApplyWorkspaceAsync(secondModel);
                    diagnostic.Track("workspace.apply", replacement);
                    diagnostic.Mark("apply.started", $"generation={Field<long>(window, "_workspaceGeneration")}");
                    Assert.Equal("", view.SourceText);
                    Assert.False(replacement.IsCompleted);
                    Assert.Equal(0, secondFactories);
                    diagnostic.Mark("first.lease.release.before");
                    first.LeaseDisposal.SetResult();
                    diagnostic.Mark("first.lease.release.after");
                    await diagnostic.AwaitAsync("workspace.apply", replacement);
                    await diagnostic.IdleAsync("yield.after-apply");
                    window.UpdateLayout();
                    Assert.Same(secondModel, window.DataContext);
                    Assert.Equal(1, secondFactories);
                    Assert.True(first.ReaderDisposed);
                    var next = Assert.Single(Visuals<AtlasLoadingHost>(window));
                    Assert.NotSame(view, next.ReaderView);
                    Assert.NotNull(next.ReaderView);

                    second.ReaderFailuresRemaining = failFirstClose ? 1 : 0;
                    diagnostic.Mark("close.first.before");
                    window.Close();
                    var closing = window.CloseOperation;
                    diagnostic.Track("window.close.first", closing);
                    diagnostic.Mark("close.first.after", $"generation={Field<long>(window, "_workspaceGeneration")}");
                    Assert.False(closing.IsCompleted);
                    Assert.True(window.IsVisible);
                    window.Close();
                    diagnostic.Mark("close.reentrant.after");
                    Assert.Same(closing, window.CloseOperation);
                    second.LeaseDisposal.SetResult();
                    diagnostic.Mark("second.lease.release.after");
                    if (failFirstClose)
                    {
                        await Assert.ThrowsAsync<InvalidOperationException>(() => closing);
                        diagnostic.Mark("close.expected-failure");
                        await diagnostic.IdleAsync("yield.after-close-failure");
                        Assert.True(window.IsVisible);
                        Assert.Contains("ATLAS-WINDOW-CLOSE", diagnostics.Text);
                        window.Close();
                        diagnostic.Mark("close.retry.after");
                    }
                    await diagnostic.AwaitAsync("window.close.final", window.CloseOperation);
                    await diagnostic.IdleAsync("yield.after-close");
                    Assert.False(window.IsVisible);
                    Assert.Equal(1, second.LeaseDisposalAttempts);
                    Assert.Equal(failFirstClose ? 2 : 1, second.ReaderDisposalAttempts);
                    diagnostic.Mark("borrowed.find.before");
                    Assert.NotNull(await borrowed.FindAsync("", 1, CancellationToken.None));
                    diagnostic.Mark("borrowed.find.after");
                }
                catch (Exception exception)
                {
                    diagnostic.Failure("body.primary", exception);
                    throw;
                }
                finally
                {
                    diagnostic.Mark("window.finally.before",
                        $"generation={Field<long>(window, "_workspaceGeneration")}; firstDisposed={first.Lease.Disposed}; secondDisposed={second.Lease.Disposed}");
                    first.LeaseDisposal.TrySetResult();
                    second.LeaseDisposal.TrySetResult();
                    second.ReaderFailuresRemaining = 0;
                    try
                    {
                        if (window.IsVisible)
                        {
                            window.Close();
                            await diagnostic.AwaitAsync("window.finally.close", window.CloseOperation);
                            await diagnostic.IdleAsync("yield.finally");
                        }
                    }
                    catch (Exception exception) { diagnostic.Failure("window.cleanup", exception); }
                    diagnostic.Mark("window.finally.after");
                }
                diagnostic.Mark("body.before-core-disposal");
            }, diagnostic, () =>
            {
                diagnostic.Mark("fixture.delete.before");
                Directory.Delete(directory, recursive: true);
                diagnostic.Mark("fixture.delete.after");
            });
        }
        catch (Exception exception) { diagnostic.Failure("pump.primary", exception); }
        finally
        {
            diagnostic.Save($"atlas-mainwindow-local-pump-{failFirstClose.ToString().ToLowerInvariant()}.log", output);
        }
        diagnostic.ThrowIfFailed();
    }

    [Fact]
    public async Task Handoff_DiagnosticControl_OwnedDispatcherOutlivesWindowAndDisposesCore()
    {
        var diagnostic = new StageDiagnostics();
        var directory = Path.Combine(Path.GetTempPath(), "aide-atlas-control-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await RunOwnedDispatcherAsync(async () =>
            {
                diagnostic.Mark("control.body.before");
                var root = Path.Combine(directory, "workspace");
                Directory.CreateDirectory(root);
                using (var core = new ObservedCore(
                    WorkspaceCore.Open("control-fixture", root, Path.Combine(directory, "data")), diagnostic))
                {
                    var window = new Window
                    {
                        Content = new Border(), Width = 100, Height = 100, Left = -10000, Top = -10000,
                        ShowInTaskbar = false, ShowActivated = false,
                    };
                    try
                    {
                        window.Show();
                        await diagnostic.IdleAsync("control.idle.before-close");
                        window.Close();
                        diagnostic.Mark("control.window.closed");
                        await diagnostic.IdleAsync("control.idle.after-close");
                        var borrowed = new LocalWorkspaceQueries(core.Value.Projections);
                        Assert.NotNull(await borrowed.FindAsync("", 1, CancellationToken.None));
                        diagnostic.Mark("control.borrowed.find.after");
                    }
                    finally { if (window.IsVisible) window.Close(); }
                }
                diagnostic.Mark("control.body.after-core-disposal");
            }, diagnostic, () =>
            {
                Directory.Delete(directory, recursive: true);
                diagnostic.Mark("control.fixture.deleted");
            });
        }
        catch (Exception exception) { diagnostic.Failure("control.wait.primary-retained-debt", exception); }
        diagnostic.Save("atlas-mainwindow-local-pump-control.log", output);
        diagnostic.ThrowIfFailed();
    }

    [Fact]
    public void StageDiagnostics_FirstRetainedDirectAssertionKeepsIdentityAndStack()
    {
        var diagnostic = new StageDiagnostics();
        var infrastructure = CapturedDiagnosticFailure(new InvalidOperationException("first infrastructure"));
        var firstAssertion = CapturedDiagnosticFailure(new Xunit.Sdk.XunitException("first retained assertion"));
        var laterAssertion = CapturedDiagnosticFailure(new Xunit.Sdk.XunitException("later assertion"));
        var originalStack = firstAssertion.StackTrace;
        diagnostic.Failure("cleanup.recorded-first", infrastructure);
        diagnostic.Failure("first.assertion", firstAssertion);
        diagnostic.Failure("later.assertion", laterAssertion);
        var thrown = Assert.Throws<Xunit.Sdk.XunitException>(() => diagnostic.ThrowIfFailed());
        Assert.Same(firstAssertion, thrown);
        Assert.Equal("first retained assertion", thrown.Message);
        Assert.Contains(originalStack!, thrown.StackTrace!);
    }

    [Fact]
    public void StageDiagnostics_AggregationPreservesOriginalsWithoutFlattening()
    {
        var diagnostic = new StageDiagnostics();
        var nested = new AggregateException(new Xunit.Sdk.XunitException("nested is not direct"));
        var cleanup = new IOException("cleanup");
        diagnostic.Failure("nested", nested);
        diagnostic.Failure("cleanup", cleanup);
        var thrown = Assert.Throws<AggregateException>(() => diagnostic.ThrowIfFailed());
        Assert.Equal(2, thrown.InnerExceptions.Count);
        Assert.Same(nested, thrown.InnerExceptions[0]);
        Assert.Same(cleanup, thrown.InnerExceptions[1]);
    }

    [Fact]
    public void StageDiagnostics_PersistsEveryRetainedFailureBeyondStageAndDetailCaps()
    {
        var diagnostic = new StageDiagnostics();
        for (var index = 0; index < 170; ++index) diagnostic.Mark("fill." + index);
        var assertion = CapturedDiagnosticFailure(new Xunit.Sdk.XunitException(new string('a', 2000) + "ASSERTION_END"));
        var cleanup = CapturedDiagnosticFailure(new IOException(new string('c', 2000) + "CLEANUP_END"));
        diagnostic.Failure("first.assertion", assertion);
        diagnostic.Failure("duplicate.capture", assertion);
        diagnostic.Failure("retained.cleanup", cleanup);
        var name = "stage-diagnostics-cap-" + Guid.NewGuid().ToString("N") + ".log";
        diagnostic.Save(name, output);
        var text = File.ReadAllText(RepositoryFile("artifacts", "atlas-mainwindow", name));
        Assert.Contains("failureOrdinal=1 category=first.assertion", text);
        Assert.Contains("failureOrdinal=3 category=retained.cleanup", text);
        Assert.DoesNotContain("category=duplicate.capture", text);
        foreach (var exception in new[] { assertion, cleanup })
        {
            Assert.Contains(exception.GetType().FullName!, text);
            Assert.Contains(exception.Message, text);
            Assert.Contains(exception.StackTrace!, text);
        }
        Assert.Contains("omittedStages=", text);
    }

    [Fact]
    public async Task StageDiagnostics_OwnedDispatcherRetainsAssertionAndCleanup()
    {
        var diagnostic = new StageDiagnostics();
        var assertion = new Xunit.Sdk.XunitException("owned dispatcher assertion");
        var cleanup = new IOException("owned dispatcher cleanup");
        await RunOwnedDispatcherAsync(async () =>
        {
            await Task.Yield();
            throw assertion;
        }, diagnostic, () => throw cleanup);
        var name = "stage-diagnostics-owned-" + Guid.NewGuid().ToString("N") + ".log";
        diagnostic.Save(name, output);
        var thrown = Assert.Throws<Xunit.Sdk.XunitException>(() => diagnostic.ThrowIfFailed());
        Assert.Same(assertion, thrown);
        var text = File.ReadAllText(RepositoryFile("artifacts", "atlas-mainwindow", name));
        Assert.Contains("category=owned.body.primary", text);
        Assert.Contains("category=owned.cleanup-retained-debt", text);
        Assert.Contains(assertion.Message, text);
        Assert.Contains(cleanup.Message, text);
        Assert.Contains(assertion.StackTrace!, thrown.StackTrace!);
    }

    private static Exception CapturedDiagnosticFailure(Exception exception)
    {
        try { throw exception; }
        catch (Exception captured) { return captured; }
    }

    private static async Task RunOwnedDispatcherAsync(Func<Task> body, StageDiagnostics diagnostic, Action cleanup)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            Task? running = null;
            try
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
                async Task RunBodyAsync()
                {
                    try { await body(); }
                    catch (Exception exception) { diagnostic.Failure("owned.body.primary", exception); }
                    finally
                    {
                        try { cleanup(); }
                        catch (Exception exception) { diagnostic.Failure("owned.cleanup-retained-debt", exception); }
                        dispatcher.BeginInvokeShutdown(DispatcherPriority.Send);
                    }
                }
                dispatcher.InvokeAsync(() =>
                {
                    running = RunBodyAsync();
                    diagnostic.Track("owned.body", running);
                });
                Dispatcher.Run();
                diagnostic.Mark("owned.dispatcher.exited");
                if (running?.IsCompleted != true)
                    diagnostic.Failure("owned.body-undrained-retained-debt",
                        new InvalidOperationException("Dispatcher exited before owned body and cleanup completed."));
            }
            catch (Exception exception) { diagnostic.Failure("owned.thread.primary-retained-debt", exception); }
            finally { completion.TrySetResult(); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        diagnostic.Track("owned.thread", completion.Task);
        thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    private sealed class ObservedCore(WorkspaceCore value, StageDiagnostics diagnostic) : IDisposable
    {
        internal WorkspaceCore Value => value;
        public void Dispose()
        {
            diagnostic.Mark("core.dispose.before");
            try
            {
                value.Dispose();
                diagnostic.Mark("core.dispose.after");
            }
            catch (Exception exception) { diagnostic.Failure("core.cleanup", exception); }
        }
    }

    private sealed class StageDiagnostics
    {
        private readonly long _started = Stopwatch.GetTimestamp();
        private readonly List<string> _stages = [];
        private readonly Dictionary<string, Task> _tasks = [];
        private sealed record RetainedFailure(
            int Ordinal, string Category, Exception Exception, string Type, string Message, string? Stack);
        private readonly List<RetainedFailure> _failures = [];
        private readonly object _gate = new();
        private int _omitted;
        private int _failureCaptureOrdinal;

        internal void Mark(string stage, string detail = "")
        {
            var dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            var line = $"ms={Stopwatch.GetElapsedTime(_started).TotalMilliseconds:F3} stage={stage} "
                + $"thread={Environment.CurrentManagedThreadId} apartment={Thread.CurrentThread.GetApartmentState()} "
                + $"context={SynchronizationContext.Current?.GetType().Name ?? "none"} "
                + $"shutdownStarted={dispatcher?.HasShutdownStarted} shutdownFinished={dispatcher?.HasShutdownFinished} "
                + detail[..Math.Min(detail.Length, 1600)];
            lock (_gate)
            {
                if (_stages.Count < 160) _stages.Add(line);
                else ++_omitted;
            }
        }

        internal void Track(string name, Task task)
        {
            lock (_gate) _tasks[name] = task;
            Mark(name + ".tracked", $"status={task.Status}");
        }

        internal async Task AwaitAsync(string name, Task task)
        {
            Track(name, task);
            Mark(name + ".before", $"status={task.Status}");
            await task;
            Mark(name + ".after", $"status={task.Status}");
        }

        internal Task IdleAsync(string name)
        {
            var operation = Dispatcher.CurrentDispatcher.InvokeAsync(
                () => Mark(name + ".executed"), DispatcherPriority.ApplicationIdle);
            return AwaitAsync(name, operation.Task);
        }

        internal void Failure(string category, Exception exception)
        {
            lock (_gate)
            {
                var ordinal = ++_failureCaptureOrdinal;
                if (!_failures.Any(failure => ReferenceEquals(failure.Exception, exception)))
                    _failures.Add(new RetainedFailure(ordinal, category, exception,
                        exception.GetType().FullName ?? exception.GetType().Name,
                        exception.Message, exception.StackTrace));
            }
            Mark(category, exception.ToString());
        }

        internal void Save(string name, ITestOutputHelper output)
        {
            string text;
            lock (_gate)
            {
                text = string.Join(Environment.NewLine, _stages)
                    + Environment.NewLine + $"omittedStages={_omitted}"
                    + Environment.NewLine + string.Join(Environment.NewLine,
                        _tasks.Select(pair => $"task={pair.Key} finalStatus={pair.Value.Status}"))
                    + Environment.NewLine + $"retainedFailures={_failures.Count}"
                    + Environment.NewLine + string.Join(Environment.NewLine, _failures.Select(failure =>
                        $"failureOrdinal={failure.Ordinal} category={failure.Category} type={failure.Type}"
                        + Environment.NewLine + $"message={failure.Message}"
                        + Environment.NewLine + $"stack={failure.Stack ?? "not recorded"}"));
            }
            var path = RepositoryFile("artifacts", "atlas-mainwindow", name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
            output.WriteLine(text);
        }

        internal void ThrowIfFailed()
        {
            Exception[] exceptions;
            lock (_gate)
                exceptions = _failures.Select(failure => failure.Exception).ToArray();
            foreach (var failure in exceptions)
                if (failure is Xunit.Sdk.XunitException) ExceptionDispatchInfo.Capture(failure).Throw();
            if (exceptions.Length > 0)
                throw new AggregateException("Retained failures without a direct assertion.", exceptions);
        }
    }

    private static ResourceDictionary MainWindowResources()
    {
        var app = XDocument.Load(RepositoryFile("src", "AiDe.App", "App.xaml")).Root!;
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
        private readonly Action<string> _write;
        internal HostDiagnostics(ITestOutputHelper output) : this(output.WriteLine) { }
        internal HostDiagnostics(Action<string> write)
        {
            _write = write;
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
            _write(Text);
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
        internal string? ReadingText { get; init; }
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
                 new("file-token", AtlasDirectoryEntryKind.File, "dir-token",
                    ReadingText is null ? "src/file.cs" : "src/Widget.cs", default, default,
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
            var text = ReadingText ?? "alpha beta";
            var pageStart = ReadingText is null ? 20 : 0;
            var highlightStart = ReadingText is null ? 26 : text.IndexOf("Answer", StringComparison.Ordinal);
            var highlightLength = ReadingText is null ? 4 : 6;
            return new(1, "scope", 7, "accepted-manifest", "file-token", null, receipt,
                new(SourceState, "observation", indexed ? "binding" : null, indexed ? "utf8" : null,
                    indexed ? text : null, indexed ? new(pageStart, text.Length) : null,
                    indexed ? [new(highlightStart, highlightLength)] : [], null, "fixture state"),
                ReadingText is null ? Bounds(AtlasBoundsDimension.SourceUtf16CodeUnits, 1)
                    : Bounds(AtlasBoundsDimension.SourceUtf16CodeUnits, 1) with
                    {
                        RequestedLimit = 32768,
                        EffectiveLimit = 32768,
                        ReturnedContentBytes = indexed ? System.Text.Encoding.UTF8.GetByteCount(text) : 0,
                    },
                AtlasOutlineState.Available, null,
                [new("decl-token", ReadingText is null ? "beta" : "ProofOwned.Widget.Answer()",
                    ReadingText is null ? default : AtlasDeclarationKind.Method, new(highlightStart, highlightLength))],
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
