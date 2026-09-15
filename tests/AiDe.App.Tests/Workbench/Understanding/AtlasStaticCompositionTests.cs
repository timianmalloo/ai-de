using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
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

[Collection("Atlas E1 native UI")]
public sealed class AtlasStaticCompositionTests
{
    [Theory]
    [InlineData(1280, false, false)]
    [InlineData(1180, true, true)]
    [InlineData(1440, true, false)]
    public async Task RealPipe_ClassListFarMemberAndBackPreserveAuthority(int width, bool visualFirst, bool light)
    {
        var repository = RepositoryRoot();
        var evidence = Path.Combine(repository, ".artifacts", "atlas-e1",
            $"native-{width}-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(evidence);
        var log = new Evidence(evidence);
        var owned = Path.Combine(evidence, "owned");
        var root = Path.Combine(owned, "repository");
        Directory.CreateDirectory(Path.Combine(root, "src"));
        var text = "public class Widget {\r\n" + new string(' ', 33000) + "// 😀\r\n"
            + string.Join("\r\n", Enumerable.Range(0, 150).Select(index => $"public int M{index}() => {index};")) + "\r\n}";
        var sourcePath = Path.Combine(root, "src", "Widget.cs");
        await File.WriteAllTextAsync(sourcePath, text, new UTF8Encoding(false));
        await GitAsync(root, "init", "--quiet");
        await GitAsync(root, "add", "--", "src/Widget.cs");
        await GitAsync(root, "commit", "--quiet", "-m", "owned native structural fixture");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var binary = Path.Combine(repository, "src", "AiDe.Daemon", "bin", configuration, "net10.0-windows", "AiDe.Daemon.exe");
        Assert.True(File.Exists(binary), "Build this tree's daemon before the real-pipe native proof.");
        log.Mark("input", new
        {
            SourceHash = Hash(Encoding.UTF8.GetBytes(text)), Utf16Length = text.Length, Utf8Bytes = Encoding.UTF8.GetByteCount(text),
            CRLF = text.Contains("\r\n", StringComparison.Ordinal), NonBmpOffset = text.IndexOf("😀", StringComparison.Ordinal),
            AppHash = Hash(File.ReadAllBytes(typeof(AtlasReaderView).Assembly.Location)),
            CoreHash = Hash(File.ReadAllBytes(typeof(WorkspaceClient).Assembly.Location)),
            DaemonHash = Hash(File.ReadAllBytes(Path.ChangeExtension(binary, ".dll"))),
            SourcePin = (await GitAsync(repository, "rev-parse", "HEAD")).Trim(),
        });
        var config = Path.Combine(owned, "config");
        Directory.CreateDirectory(config);
        var start = new ProcessStartInfo(binary)
        {
            WorkingDirectory = Path.GetDirectoryName(binary)!, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
        };
        foreach (var argument in new[] { root, "--data", Path.Combine(owned, "data"), "--idle-seconds", "1", "--startup-seconds", "20" })
            start.ArgumentList.Add(argument);
        foreach (var variable in new[] { "HOME", "USERPROFILE", "LOCALAPPDATA", "APPDATA", "XDG_CONFIG_HOME" })
            start.Environment[variable] = config;
        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["GIT_CONFIG_GLOBAL"] = "NUL";
        using var daemon = Process.Start(start)!;
        var stdout = daemon.StandardOutput.ReadToEndAsync();
        var stderr = daemon.StandardError.ReadToEndAsync();
        WorkspaceClient? client = null;
        var failures = new List<Exception>();
        try
        {
            client = await WorkspaceClient.ConnectAsync(IpcPipeName.ForWorkspace(root), TimeSpan.FromSeconds(10), CancellationToken.None);
            log.Mark("daemon.connected", new { daemon.Id, client.Epoch });
            await AtlasStaticTestHost.RunAsync(async () =>
            {
                ObservedReader? observed = null;
                var model = new MainWindowViewModel(client, "display-is-not-authority", null,
                    commands: client, atlasReaderFactory: () => observed = new ObservedReader(client.CreateAtlasReader(), log));
                await model.RefreshAsync(CancellationToken.None);
                var window = new AiDe.App.MainWindow(() => Task.FromResult(model), Path.Combine(owned, "shell"), Resources(repository, light))
                {
                    Width = width, Height = 900, Left = 40, Top = 40,
                    WindowStartupLocation = WindowStartupLocation.Manual, ShowInTaskbar = false,
                };
                AtlasReaderView? reader = null;
                try
                {
                    window.Shell.Execute(PerspectiveSet.Architecture.CommandId);
                    var title = PerspectiveMenu.Opener(SurfaceContentFactory.Kinds.Single(kind => kind.Kind == "code-atlas")).Title;
                    var menu = Assert.IsType<Menu>(window.FindName("MainMenu"));
                    MenuItems(menu).Single(item => Equals(item.Header, title)).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                    window.Show();
                    await IdleAsync();
                    await window.WorkspaceReady;
                    Assert.NotNull(observed);
                    await observed.InventoryReady.Task;
                    await IdleAsync();
                    reader = Assert.IsType<AtlasReaderView>(Visuals<AtlasLoadingHost>(window).Single().ReaderView);
                    await reader.CurrentOperation;
                    await IdleAsync();
                    log.Mark("reader.inventory-published", new
                    {
                        reader.StatusText, Roots = reader.FileRoots.Count,
                        Files = reader.FileRoots.SelectMany(Flatten).Where(node => node.IsFile).Select(node => node.RelativePath).ToArray(),
                    });
                    var file = reader.FileRoots.SelectMany(Flatten).Single(node => node.IsFile && node.RelativePath.EndsWith("Widget.cs", StringComparison.Ordinal));
                    var hwnd = new WindowInteropHelper(window).Handle;
                    log.Mark("native.environment", new
                    {
                        Width = window.ActualWidth, Height = window.ActualHeight, light, visualFirst,
                        NativeDpi = GetDpiForWindow(hwnd), WpfDpi = VisualTreeHelper.GetDpi(window).PixelsPerInchX,
                        SystemParameters.HighContrast, SystemParameters.ClientAreaAnimation,
                        TextScale = "default inherited text size; OS text-scale setting not changed",
                    });
                    if (visualFirst) await InvokeAsync(hwnd, reader.ClassViewButton, log);
                    await reader.SelectFileAsync(file);
                    Assert.Equal(text[..reader.CurrentSelection!.Source.PageSpan!.Length], reader.SourceText);
                    if (!visualFirst)
                    {
                        Assert.True(reader.NextSourceButton.IsEnabled);
                        await reader.LoadNextSourcePageAsync();
                        var page = reader.CurrentSelection!.Source.PageSpan!;
                        Assert.Equal(text.Substring(page.Start, page.Length), reader.SourceText);
                        Assert.True(page.Start >= 32768);
                        await reader.GoBackAsync();
                        await InvokeAsync(hwnd, reader.ClassViewButton, log);
                    }
                    await reader.CurrentOperation;
                    await IdleAsync();
                    Assert.Equal(128, reader.OutlineRows.Count);
                    Assert.Equal(128, reader.CurrentSelection!.OutlineNextOffset);
                    Assert.Equal(AtlasPresentationMode.Class, reader.Presentation);
                    Assert.True(reader.StaticView.Projection!.Classifier!.IsClass);
                    ObserveSelectedTab(window, reader.StaticView.Presentations, log);
                    var firstButton = reader.StaticView.CompartmentButtons.First();
                    var member = Assert.IsType<AtlasStaticOccurrence>(firstButton.Tag);
                    Assert.True(member.Declaration.Span.Start > 32768);
                    CheckLabel(window, Assert.IsType<TextBlock>(firstButton.Content), log, "class.member");
                    Capture(window, evidence, "class", log);
                    await InvokeAsync(hwnd, firstButton, log);
                    await reader.CurrentOperation;
                    await IdleAsync();
                    Assert.Equal(AtlasPresentationMode.Source, reader.Presentation);
                    Assert.Equal(member.Token, reader.CurrentSelection!.DeclarationToken);
                    Assert.Equal(text.Substring(member.Declaration.Span.Start, member.Declaration.Span.Length), reader.SourceText);
                    var binding = reader.CurrentBindingToken;
                    CheckSource(window, reader, log, "compartment.source");
                    await InvokeAsync(hwnd, reader.BackButton, log);
                    await reader.CurrentOperation;
                    await IdleAsync();
                    ObserveBackFocus(window, reader, log, "back.compartment-focus");
                    Assert.Equal(AtlasPresentationMode.Class, reader.Presentation);
                    Assert.Equal(member.Token, reader.StaticView.SelectedToken);
                    Assert.Contains(reader.StaticView.CompartmentButtons, button => button.IsKeyboardFocusWithin);
                    await ActivateListAsync(hwnd, reader, member.Token, log);
                    Assert.Equal(binding, reader.CurrentBindingToken);
                    Assert.Equal(member.Token, reader.CurrentSelection!.DeclarationToken);
                    log.Mark("selection.parity", new { BindingHash = Hash(Encoding.UTF8.GetBytes(binding!)), SameCompartmentListSource = true });
                    await reader.GoBackAsync();
                    await reader.LoadNextOutlinePageAsync();
                    await IdleAsync();
                    Assert.Equal(23, reader.OutlineRows.Count);
                    Assert.Equal(128, reader.OutlinePageOffset);
                    Assert.Null(reader.StaticView.Projection!.Classifier);
                    Assert.All(reader.CurrentSelection!.Outline, row =>
                    {
                        Assert.Equal(AtlasLexicalParentState.OutsidePage, row.Structure!.ParentState);
                        Assert.Null(row.Structure.ParentDeclarationToken);
                    });
                    var far = reader.StaticView.Projection.Occurrences.Single(row => row.Declaration.DisplayName.Contains("M140(", StringComparison.Ordinal));
                    await ActivateListAsync(hwnd, reader, far.Token, log);
                    Assert.Equal(text.IndexOf("public int M140", StringComparison.Ordinal), reader.CurrentSelection!.Source.PageSpan!.Start);
                    Assert.Equal(text.Substring(far.Declaration.Span.Start, far.Declaration.Span.Length), reader.SourceText);
                    Assert.Single(reader.CurrentHighlights);
                    CheckSource(window, reader, log, "far.source");
                    Capture(window, evidence, "far-source", log);
                    await reader.GoBackAsync();
                    await IdleAsync();
                    ObserveBackFocus(window, reader, log, "back.list-focus");
                    Assert.Equal(AtlasPresentationMode.Class, reader.Presentation);
                    Assert.Equal(128, reader.OutlinePageOffset);
                    Assert.Equal(far.Token, reader.StaticView.SelectedToken);
                    Assert.True(reader.StaticView.DeclarationList.IsKeyboardFocusWithin);
                    await reader.SetPresentationAsync(AtlasPresentationMode.Source);
                    await reader.SelectDeclarationAsync(reader.OutlineRows.Single(row => row.ObservationKey == far.Token));
                    Assert.All(reader.CurrentSelection!.Outline, row => Assert.Null(row.Structure));
                    await reader.GoBackAsync();
                    Assert.Equal(AtlasPresentationMode.Source, reader.Presentation);
                    Assert.All(reader.CurrentSelection!.Outline, row => Assert.NotNull(row.Structure));
                    log.Mark("restore.original-preference", new { OutlinePage = reader.OutlinePageOffset, Presentation = reader.Presentation.ToString() });
                    await File.AppendAllTextAsync(sourcePath, "\r\n// changed owned source\r\n");
                    await reader.SelectFileAsync(file);
                    Assert.Equal("", reader.SourceText);
                    Assert.Null(reader.CurrentBindingToken);
                    Assert.DoesNotContain(reader.StaticView.CompartmentButtons, button => button.IsEnabled);
                    log.Mark("changed-source.inert", new { ReaderStatus = reader.StatusText, ClassStatus = reader.StaticView.StatusText });
                    await reader.GoBackAsync();
                    Assert.Equal("", reader.SourceText);
                    Assert.Null(reader.CurrentBindingToken);
                    log.Mark("changed-source.restore-refused", new { reader.StatusText });
                }
                finally
                {
                    window.Close();
                    await window.CloseOperation;
                    await IdleAsync();
                    Assert.False(window.IsVisible);
                    if (reader is not null) Assert.Null(reader.StaticView.Projection);
                    log.Mark("window.closed", new { BorrowedQueryUsable = (await client.FindAsync("", 1, CancellationToken.None)) is not null });
                }
            });
        }
        catch (Exception exception) { failures.Add(exception); log.Mark("primary", new { exception.Message, exception.StackTrace }); }
        finally
        {
            if (client is not null) await client.DisposeAsync();
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await daemon.WaitForExitAsync(timeout.Token);
                Assert.Equal(0, daemon.ExitCode);
                log.Mark("daemon.exited", new { daemon.Id, daemon.ExitCode, OutputHash = Hash(Encoding.UTF8.GetBytes(await stdout)), Error = await stderr });
                foreach (var path in Directory.EnumerateFiles(owned, "*", SearchOption.AllDirectories))
                    File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
                Directory.Delete(owned, recursive: true);
                log.Mark("fixture.deleted", new { Exists = Directory.Exists(owned) });
            }
            catch (Exception exception) { failures.Add(exception); log.Mark("cleanup-retained-debt", new { exception.Message, daemon.Id }); }
        }
        if (failures.Count > 0) throw new AggregateException("Native primary and cleanup failures.", failures);
    }

    private static async Task InvokeAsync(IntPtr hwnd, Button button, Evidence evidence)
    {
        await InvokeAndObserveClickAsync(button, () => InvokeProviderAsync(hwnd, button, evidence));
        evidence.Mark("uia.click-boundary", new { Name = AutomationProperties.GetName(button) });
    }

    internal static async Task InvokeAndObserveClickAsync(Button button, Func<Task> invoke)
    {
        var clicked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void ObserveClick(object sender, RoutedEventArgs args) => clicked.TrySetResult();
        button.Click += ObserveClick;
        try
        {
            await invoke();
            await clicked.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally { button.Click -= ObserveClick; }
    }

    private static async Task InvokeProviderAsync(IntPtr hwnd, Button button, Evidence evidence)
    {
        var name = AutomationProperties.GetName(button);
        var started = Stopwatch.GetTimestamp();
        var clicks = 0;
        void ObserveClick(object sender, RoutedEventArgs args)
        {
            Interlocked.Increment(ref clicks);
            evidence.Mark("uia.routed-click", new { Name = name, button.IsLoaded, button.IsVisible, button.IsEnabled });
        }
        button.Click += ObserveClick;
        evidence.Mark("uia.wpf-before", new
        {
            Name = name, button.IsLoaded, button.IsVisible, button.IsEnabled,
            button.ActualWidth, button.ActualHeight,
            ActualWindow = new WindowInteropHelper(Window.GetWindow(button)).Handle.ToInt64(),
            RequestedWindow = hwnd.ToInt64(),
        });
        try
        {
        var observation = await Task.Run(() =>
        {
            var window = AutomationElement.FromHandle(hwnd);
            var condition = new AndCondition(
                new PropertyCondition(AutomationElement.NameProperty, name),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
            var element = window.FindFirst(TreeScope.Descendants, condition);
            var buttons = window.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
                .Cast<AutomationElement>().Select(candidate => new
                {
                    candidate.Current.Name, candidate.Current.IsEnabled, candidate.Current.IsOffscreen,
                    Bounds = candidate.Current.BoundingRectangle.ToString(CultureInfo.InvariantCulture),
                }).ToArray();
            evidence.Mark("uia.lookup", new
            {
                Name = name, Found = element is not null, window.Current.ProcessId,
                NativeWindow = window.Current.NativeWindowHandle, Buttons = buttons,
                FoundAfterCensus = window.FindFirst(TreeScope.Descendants, condition) is not null,
            });
            Assert.NotNull(element);
            element.SetFocus();
            var role = element.Current.ControlType.ProgrammaticName;
            ((InvokePattern)element.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
            evidence.Mark("uia.provider-return", new { Name = name, ClicksObserved = Volatile.Read(ref clicks) });
            return new { Name = name, Role = role,
                Apartment = Thread.CurrentThread.GetApartmentState().ToString(), OwnedWindow = hwnd.ToInt64() };
        });
        evidence.Mark("uia.invoke", new { observation, DurationMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds });
        }
        finally
        {
            evidence.Mark("uia.after-invoke", new { Name = name, ClicksObserved = Volatile.Read(ref clicks) });
            button.Click -= ObserveClick;
        }
    }

    private static void ObserveBackFocus(Window window, AtlasReaderView reader, Evidence evidence, string stage)
    {
        var focused = Keyboard.FocusedElement as DependencyObject;
        var logical = FocusManager.GetFocusedElement(window) as DependencyObject;
        var focusWindow = focused is null ? null : Window.GetWindow(focused);
        evidence.Mark(stage, new
        {
            window.IsActive, window.IsKeyboardFocusWithin,
            ReaderFocus = reader.IsKeyboardFocusWithin,
            ListFocus = reader.StaticView.DeclarationList.IsKeyboardFocusWithin,
            KeyboardType = focused?.GetType().FullName,
            KeyboardName = focused is null ? null : AutomationProperties.GetName(focused),
            LogicalType = logical?.GetType().FullName,
            FocusWindow = focusWindow is null ? null : (long?)new WindowInteropHelper(focusWindow).Handle.ToInt64(),
            OwnedWindow = new WindowInteropHelper(window).Handle.ToInt64(),
            FocusInOwnedWindow = ReferenceEquals(focusWindow, window),
            OperationStatus = reader.CurrentOperation.Status.ToString(),
            Presentation = reader.Presentation.ToString(), reader.OutlinePageOffset,
            reader.StaticView.SelectedToken, reader.StatusText,
        });
    }

    private static void ObserveSelectedTab(Window window, TabControl tabs, Evidence evidence)
    {
        var tab = Assert.IsAssignableFrom<TabItem>(tabs.SelectedItem);
        var header = Visuals<TextBlock>(tab).Single(block => block.Text == tab.Header.ToString());
        var dpi = VisualTreeHelper.GetDpi(window);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(window.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var stride = bitmap.PixelWidth * 4;
        var pixels = new byte[stride * bitmap.PixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);
        Color Pixel(int x, int y)
        {
            var offset = y * stride + x * 4;
            return Color.FromArgb(pixels[offset + 3], pixels[offset + 2], pixels[offset + 1], pixels[offset]);
        }
        var tabBounds = tab.TransformToAncestor(window).TransformBounds(new Rect(tab.RenderSize));
        var headerBounds = header.TransformToAncestor(window).TransformBounds(new Rect(header.RenderSize));
        var backgroundX = (int)((tabBounds.Right - 4) * dpi.DpiScaleX);
        var backgroundY = (int)((tabBounds.Top + 4) * dpi.DpiScaleY);
        var background = Pixel(backgroundX, backgroundY);
        var foreground = Assert.IsType<SolidColorBrush>(header.Foreground).Color;
        Point? glyphPixel = null;
        for (var y = (int)Math.Ceiling(headerBounds.Top * dpi.DpiScaleY); y < headerBounds.Bottom * dpi.DpiScaleY; y++)
            for (var x = (int)Math.Ceiling(headerBounds.Left * dpi.DpiScaleX); x < headerBounds.Right * dpi.DpiScaleX; x++)
                if (Pixel(x, y) == foreground) glyphPixel ??= new Point(x, y);
        var ancestry = new List<object>();
        for (DependencyObject? current = header; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            var property = current is Border ? Border.BackgroundProperty
                : current is Panel ? Panel.BackgroundProperty : current is Control ? Control.BackgroundProperty : null;
            ancestry.Add(new
            {
                Type = current.GetType().Name,
                Opacity = (current as UIElement)?.Opacity,
                Background = property is null ? null : current.GetValue(property)?.ToString(),
                BackgroundSource = property is null ? null : DependencyPropertyHelper.GetValueSource(current, property).BaseValueSource.ToString(),
            });
            if (ReferenceEquals(current, tab)) break;
        }
        static double Luminance(Color color)
        {
            static double Linear(byte value) => value / 255d <= 0.04045 ? value / 255d / 12.92
                : Math.Pow((value / 255d + 0.055) / 1.055, 2.4);
            return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
        }
        var luminances = new[] { Luminance(foreground), Luminance(background) };
        evidence.Mark("selected-tab.paint", new
        {
            Foreground = foreground.ToString(), header.Opacity,
            ForegroundSource = DependencyPropertyHelper.GetValueSource(header, TextBlock.ForegroundProperty).BaseValueSource.ToString(),
            TemplateSource = DependencyPropertyHelper.GetValueSource(tab, Control.TemplateProperty).BaseValueSource.ToString(),
            Template = XamlWriter.Save(tab.Template),
            PaintedParts = Visuals<Border>(tab).Select(border => new
            {
                border.Name, border.IsVisible, Background = border.Background?.ToString(),
                Source = DependencyPropertyHelper.GetValueSource(border, Border.BackgroundProperty).BaseValueSource.ToString(),
            }).ToArray(),
            PaintedBackground = background.ToString(), BackgroundPixel = new { X = backgroundX, Y = backgroundY },
            GlyphInteriorPixel = glyphPixel?.ToString(CultureInfo.InvariantCulture),
            Contrast = glyphPixel.HasValue ? (double?)((luminances.Max() + 0.05) / (luminances.Min() + 0.05)) : null,
            Ancestry = ancestry,
        });
        Assert.True(glyphPixel.HasValue, "Selected header foreground needs an actual rendered glyph-interior pixel.");
        Assert.True((luminances.Max() + 0.05) / (luminances.Min() + 0.05) >= 4.5,
            "Selected header rendered foreground/background contrast must be at least 4.5:1.");
    }

    private static async Task ActivateListAsync(IntPtr hwnd, AtlasReaderView reader, string token, Evidence evidence)
    {
        reader.StaticView.Presentations.SelectedIndex = 1;
        var row = reader.StaticView.Projection!.Occurrences.Single(item => item.Token == token);
        reader.StaticView.DeclarationList.ScrollIntoView(row);
        await IdleAsync();
        await Task.Run(() =>
        {
            var window = AutomationElement.FromHandle(hwnd);
            var item = window.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.NameProperty, row.AccessibleName),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem)));
            Assert.NotNull(item);
            ((SelectionItemPattern)item.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
            item.SetFocus();
            Assert.True(((SelectionItemPattern)item.GetCurrentPattern(SelectionItemPattern.Pattern)).Current.IsSelected);
        });
        var list = reader.StaticView.DeclarationList;
        list.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(list), Environment.TickCount, Key.Enter)
            { RoutedEvent = Keyboard.KeyDownEvent });
        await reader.CurrentOperation;
        await IdleAsync();
        evidence.Mark("uia.list-and-routed-enter", new { Role = "ListItem", Selected = true, UiThread = Environment.CurrentManagedThreadId });
    }

    private static void CheckSource(Window window, AtlasReaderView reader, Evidence evidence, string stage)
    {
        var textView = reader.SourceControl.TextArea.TextView;
        textView.EnsureVisualLines();
        var viewport = Visible(textView, window);
        var page = reader.CurrentSelection!.Source.PageSpan!;
        Rect Bounds(int start, int length)
        {
            var document = reader.SourceControl.Document;
            var top = textView.GetVisualPosition(new(document.GetLocation(start)), ICSharpCode.AvalonEdit.Rendering.VisualYPosition.LineTop) - textView.ScrollOffset;
            var bottom = textView.GetVisualPosition(new(document.GetLocation(start + length)), ICSharpCode.AvalonEdit.Rendering.VisualYPosition.LineBottom) - textView.ScrollOffset;
            return textView.TransformToAncestor(window).TransformBounds(new Rect(top, bottom));
        }
        var source = Bounds(0, reader.SourceText.Length);
        var highlights = reader.CurrentHighlights.Select(span => Bounds(span.Start - page.Start, span.Length)).ToArray();
        evidence.Mark(stage, new { Viewport = viewport.ToString(CultureInfo.InvariantCulture), Source = source.ToString(CultureInfo.InvariantCulture),
            Highlights = highlights.Select(rect => rect.ToString(CultureInfo.InvariantCulture)), reader.SourceControl.FontSize, page.Start, page.Length });
        Assert.True(!viewport.IsEmpty && viewport.Contains(source));
        Assert.All(highlights, rect => Assert.True(viewport.Contains(rect)));
    }

    internal static void CheckLabel(Window window, TextBlock label, Evidence evidence, string stage)
    {
        var viewport = Visible(label, window, layout: false);
        var runs = Glyphs(VisualTreeHelper.GetDrawing(label), Matrix.Identity).ToArray();
        Assert.NotEmpty(runs);
        var rectangles = runs.Select(run => label.TransformToAncestor(window).TransformBounds(run.Bounds)).ToArray();
        evidence.Mark(stage, new { label.Text, label.FontSize, Viewport = viewport.ToString(CultureInfo.InvariantCulture),
            Glyphs = rectangles.Select(rect => rect.ToString(CultureInfo.InvariantCulture)) });
        Assert.All(rectangles, rect => Assert.True(viewport.Contains(rect)));
        Assert.Equal(new string(label.Text.Where(character => !char.IsWhiteSpace(character)).ToArray()),
            new string(string.Concat(runs.Select(run => run.Text)).Where(character => !char.IsWhiteSpace(character)).ToArray()));
    }

    private static IEnumerable<(Rect Bounds, string Text)> Glyphs(Drawing drawing, Matrix parent)
    {
        if (drawing is DrawingGroup group)
        {
            var transform = group.Transform?.Value ?? Matrix.Identity;
            transform.Append(parent);
            foreach (var child in group.Children)
                foreach (var run in Glyphs(child, transform)) yield return run;
        }
        else if (drawing is GlyphRunDrawing run)
            yield return (new MatrixTransform(parent).TransformBounds(run.Bounds), new string(run.GlyphRun.Characters.ToArray()));
    }

    private static Rect Visible(FrameworkElement element, Window window, bool layout = true)
    {
        var client = (FrameworkElement)window.Content;
        var viewport = client.TransformToAncestor(window).TransformBounds(new Rect(client.RenderSize));
        if (layout) viewport.Intersect(element.TransformToAncestor(window).TransformBounds(new Rect(element.RenderSize)));
        for (DependencyObject? current = element; current is not null && !ReferenceEquals(current, window); current = VisualTreeHelper.GetParent(current))
        {
            if (current is not UIElement visual) continue;
            if (!visual.IsVisible || visual.Opacity == 0) return Rect.Empty;
            if (visual.ClipToBounds) viewport.Intersect(visual.TransformToAncestor(window).TransformBounds(new Rect(visual.RenderSize)));
            if (VisualTreeHelper.GetClip(visual) is { } clip) viewport.Intersect(visual.TransformToAncestor(window).TransformBounds(clip.Bounds));
        }
        return viewport;
    }

    internal static void Capture(Window window, string directory, string name, Evidence evidence)
    {
        var dpi = VisualTreeHelper.GetDpi(window);
        var image = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(window.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        image.Render(window);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        var path = Path.Combine(directory, name + ".png");
        using (var file = File.Create(path)) encoder.Save(file);
        evidence.Mark("capture", new { Path = path, Sha256 = Hash(File.ReadAllBytes(path)), image.PixelWidth, image.PixelHeight });
    }

    private static IEnumerable<AtlasFileNode> Flatten(AtlasFileNode node) =>
        new[] { node }.Concat(node.Children.SelectMany(Flatten));
    private static Task IdleAsync() => Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    private static IEnumerable<T> Visuals<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); ++index)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) yield return match;
            foreach (var nested in Visuals<T>(child)) yield return nested;
        }
    }
    private static IEnumerable<MenuItem> MenuItems(ItemsControl root) =>
        root.Items.OfType<MenuItem>().SelectMany(item => new[] { item }.Concat(MenuItems(item)));
    internal static string RepositoryRoot()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AiDe.App.sln"))
            && !Directory.Exists(Path.Combine(root.FullName, ".git")) && !File.Exists(Path.Combine(root.FullName, ".git")))
            root = root.Parent;
        return root?.FullName ?? throw new InvalidOperationException("Executing repository not found.");
    }
    internal static ResourceDictionary Resources(string repository, bool light)
    {
        var root = XDocument.Load(Path.Combine(repository, "src", "AiDe.App", "App.xaml")).Root!;
        var ns = root.Name.Namespace;
        if (light)
        {
            var design = File.ReadAllText(Path.Combine(repository, "DESIGN.md"));
            XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
            foreach (var pair in new[] { ("SurfaceBrush", "light-surface"), ("SurfaceRaisedBrush", "light-surface-raised"),
                ("SurfaceSunkenBrush", "light-surface-sunken"), ("TextBrush", "light-text"),
                ("TextMutedBrush", "light-text-muted"), ("BorderBrush", "light-border") })
            {
                var line = design.Split('\n').Single(value => value.TrimStart().StartsWith(pair.Item2 + ":", StringComparison.Ordinal));
                root.Element(ns + "Application.Resources")!.Elements(ns + "SolidColorBrush")
                    .Single(element => (string?)element.Attribute(x + "Key") == pair.Item1)
                    .SetAttributeValue("Color", line.Split('"')[1]);
            }
        }
        return (ResourceDictionary)XamlReader.Parse(new XElement(ns + "ResourceDictionary",
            root.Attributes().Where(attribute => attribute.IsNamespaceDeclaration).Select(attribute =>
                new XAttribute(attribute.Name, attribute.Value == "clr-namespace:AiDe.App" ? "clr-namespace:AiDe.App;assembly=AiDe.App" : attribute.Value)),
            root.Element(ns + "Application.Resources")!.Elements()).ToString());
    }
    private static async Task<string> GitAsync(string directory, params string[] arguments)
    {
        var start = new ProcessStartInfo(@"C:\Program Files\Git\cmd\git.exe")
        {
            WorkingDirectory = directory, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
        };
        foreach (var argument in new[] { "-c", "user.name=Atlas Native Proof", "-c", "user.email=atlas-native@example.invalid" }.Concat(arguments))
            start.ArgumentList.Add(argument);
        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["GIT_CONFIG_GLOBAL"] = "NUL";
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await process.WaitForExitAsync(timeout.Token);
        Assert.True(process.ExitCode == 0, await error);
        return await output;
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);

    internal sealed class Evidence(string directory)
    {
        private readonly List<object> _events = [];
        private readonly long _started = Stopwatch.GetTimestamp();
        private readonly object _gate = new();
        internal void Mark(string stage, object attributes)
        {
            lock (_gate)
            {
                _events.Add(new { stage, milliseconds = Stopwatch.GetElapsedTime(_started).TotalMilliseconds, attributes });
                File.WriteAllText(Path.Combine(directory, "receipt.json"), JsonSerializer.Serialize(_events, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
    }

    private sealed class ObservedReader(IAtlasWorkspaceReader inner, Evidence evidence) : IAtlasWorkspaceReader
    {
        internal TaskCompletionSource InventoryReady { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async ValueTask<IAtlasReaderLease> AdmitAsync(CancellationToken cancellationToken) =>
            new ObservedLease(await inner.AdmitAsync(cancellationToken), this, evidence);
        public ValueTask DisposeAsync() => inner.DisposeAsync();

        private sealed class ObservedLease(IAtlasReaderLease lease, ObservedReader owner, Evidence evidence)
            : IAtlasReaderLease, IAtlasReaderQueries
        {
            public string ScopeToken => lease.ScopeToken;
            public string InitialManifestToken => lease.InitialManifestToken;
            public long CoreEpoch => lease.CoreEpoch;
            public DateTimeOffset ExpiresAt => lease.ExpiresAt;
            public IAtlasReaderQueries Queries => this;
            public CancellationToken Invalidated => lease.Invalidated;
            public bool IsTerminal => lease.IsTerminal;
            public ValueTask DisposeAsync() => lease.DisposeAsync();
            public async ValueTask<AtlasInventoryPageDto> InventoryAsync(AtlasInventoryRequestDto request, CancellationToken cancellationToken)
            {
                var result = await lease.Queries.InventoryAsync(request, cancellationToken);
                evidence.Mark("inventory.accepted", new
                {
                    Rows = result.Files.Length, result.NextOffset,
                    Entries = result.Files.Select(row => new { row.RelativePath, row.Kind, row.Availability }).ToArray(),
                });
                owner.InventoryReady.TrySetResult();
                return result;
            }
            public async ValueTask<AtlasSelectionDto> SelectAsync(AtlasSelectRequestDto request, CancellationToken cancellationToken)
            {
                var started = Stopwatch.GetTimestamp();
                var result = await lease.Queries.SelectAsync(request, cancellationToken);
                evidence.Mark("select.accepted", new
                {
                    request.SourceOffset, request.SourceLength, request.OutlineOffset, request.OutlineLimit, request.StaticStructure,
                    Rows = result.Outline.Length, result.OutlineNextOffset, State = result.Source.State.ToString(),
                    SourceBytes = result.SourceBounds.ReturnedContentBytes, MetadataContentBytes = result.OutlineBounds.ReturnedContentBytes,
                    DurationMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                });
                return result;
            }
            public async ValueTask<AtlasSelectionDto> RestoreAsync(AtlasRestoreRequestDto request, CancellationToken cancellationToken)
            {
                var result = await lease.Queries.RestoreAsync(request, cancellationToken);
                evidence.Mark("restore.accepted", new { Rows = result.Outline.Length, HasStructure = result.Outline.Any(row => row.Structure is not null) });
                return result;
            }
        }
    }
}
