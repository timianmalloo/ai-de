using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using AiDe.App.Tests.Shell;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using Microsoft.Web.WebView2.Wpf;

namespace AiDe.App.Tests.Kinds;

/// <summary>
/// The WL-S brief's check: <c>Ctrl+1/2/3</c> switch the perspective while keyboard focus is inside a WebView2 page —
/// the notebook's composer page and the Understand graph's canvas page — through the product's composition
/// (<c>MainWindow</c>'s: <see cref="PerspectiveShell"/> as the command router, the gestures bound on the window).
/// </summary>
/// <remarks>
/// The keys are real OS input (<c>SendInput</c>) into the browser's own window, so the path is the operator's:
/// Chromium, WebView2's accelerator event, the WPF wrapper's key events, the window's key binding. The test first
/// proves that this window holds the foreground and that OS focus is in the page's HWND; a machine where it cannot is
/// reported as such, never as a product failure.
/// </remarks>
public sealed class CtrlDigitSwitchesThePerspectiveFromInsideAPageTests : IDisposable
{
    private readonly ComposedCoding.Workspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    [CheckedFact]
    public void CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective()
    {
        var config = _workspace.Session("keys from a page");

        Sta.Pump<ContentControl>(
            create: () => new ContentControl(),
            configure: window =>
            {
                window.Left = 0;
                window.Top = 0;
                window.Width = 1440;
                window.Height = 900;
                window.ShowActivated = true;
            },
            timeoutSeconds: 150,
            body: async (window, host) =>
            {
                using var shell = new WorkbenchShell(queries: null);
                var mode = new PerspectiveShell(host, shell.Hosts, shell.Announcer);
                shell.CommandRouter = mode.Execute;
                shell.DocumentOpening += h => mode.OnDocumentOpening(h);
                shell.Bind(window);
                mode.Activate(PerspectiveSet.Factory, "test");

                shell.OpenSessionDocument(config);
                await Settle(window);
                Assert.Same(PerspectiveSet.Factory, mode.Active);

                // Ctrl+3 from inside the notebook's composer page.
                var composer = shell.SessionComposer(config.SessionId);
                Assert.NotNull(composer);
                var page = ComposedCoding.Visuals<WebView2>(composer!).FirstOrDefault();
                Assert.True(page is not null, "the notebook's composer renders no WebView2");
                await FocusInsideAsync(window, page!, () => Keyboard.Focus(page) is not null);
                await ChordAsync(window, 0x33);
                Assert.Same(PerspectiveSet.Understand, mode.Active);

                // Ctrl+2 from inside the Understand graph's canvas page.
                var canvas = await CanvasAsync(window, shell);
                await FocusInsideAsync(window, CanvasView(canvas), () => canvas.FocusTarget.TryFocus());
                await ChordAsync(window, 0x32);
                Assert.Same(PerspectiveSet.Review, mode.Active);

                // Ctrl+1 from inside the canvas page again.
                mode.Activate(PerspectiveSet.Understand, "test");
                await Settle(window);
                await FocusInsideAsync(window, CanvasView(canvas), () => canvas.FocusTarget.TryFocus());
                await ChordAsync(window, 0x31);
                Assert.Same(PerspectiveSet.Factory, mode.Active);
                window.Content = null;
            });
    }

    private static async Task<CanvasSurface> CanvasAsync(Window window, WorkbenchShell shell)
    {
        await Settle(window);
        var content = shell.Understand.Adapter.ContentFor("graph");
        Assert.NotNull(content);
        var canvas = ComposedCoding.Visuals<CanvasSurface>(content!).FirstOrDefault() ?? content as CanvasSurface;
        Assert.True(canvas is not null, "Understand's graph renders no CanvasSurface");
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!canvas!.Ready && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
            await Settle(window);
        }

        Assert.True(canvas.Ready, "the canvas page never finished loading");
        return canvas;
    }

    private static WebView2 CanvasView(CanvasSurface canvas) =>
        ComposedCoding.Visuals<WebView2>(canvas).FirstOrDefault() ?? throw new InvalidOperationException("the canvas renders no WebView2");

    /// <summary>Focuses the page and proves it: this window is the foreground, OS focus is a Chromium HWND, and the page has focus.</summary>
    private static async Task FocusInsideAsync(Window window, WebView2 page, Func<bool> focus)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (page.CoreWebView2 is null && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
            await Settle(window);
        }

        Assert.True(page.CoreWebView2 is not null, "the page's WebView2 never initialized");
        var hwnd = new WindowInteropHelper(window).Handle;
        var hasFocus = "false";
        var focusClass = "(none)";
        for (var attempt = 0; attempt < 20; attempt++)
        {
            window.Activate();
            SetForegroundWindow(hwnd);
            focus();
            await Task.Delay(150);
            await Settle(window);
            focusClass = ClassNameOf(GetFocus());
            hasFocus = await page.ExecuteScriptAsync("String(document.hasFocus())");
            if (GetForegroundWindow() == hwnd && focusClass.StartsWith("Chrome_", StringComparison.Ordinal) && hasFocus == "\"true\"")
            {
                return;
            }
        }

        // Read after the attempts above: a window that is not the foreground cannot take the chord (DC-NEW-wl-t33-input-state).
        InputPreconditions.RequireForeground(hwnd, "SendInput");
        throw new InvalidOperationException(
            $"environment: focus could not be put inside the page (foreground held={GetForegroundWindow() == hwnd}, os focus class='{focusClass}', page hasFocus={hasFocus})");
    }

    /// <summary>Ctrl + <paramref name="digit"/> as real OS input, then the dispatcher drained.</summary>
    private static async Task ChordAsync(Window window, ushort digit)
    {
        var inputs = new INPUT[4];
        inputs[0] = Key(0x11, up: false);
        inputs[1] = Key(digit, up: false);
        inputs[2] = Key(digit, up: true);
        inputs[3] = Key(0x11, up: true);
        Assert.Equal((uint)inputs.Length, SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>()));
        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(50);
            await Settle(window);
        }
    }

    private static INPUT Key(ushort vk, bool up)
    {
        var input = new INPUT { type = 1 };
        input.u.ki.wVk = vk;
        input.u.ki.dwFlags = up ? 0x0002u : 0u;
        return input;
    }

    private static async Task Settle(Window window)
    {
        window.UpdateLayout();
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    private static string ClassNameOf(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return "(none)";
        }

        var buffer = new StringBuilder(256);
        return GetClassName(hwnd, buffer, buffer.Capacity) > 0 ? buffer.ToString() : "(unknown)";
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetFocus();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
}
