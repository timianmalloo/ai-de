using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AiDe.App.Workbench;

/// <summary>
/// The one place a top-level window is created, and the one place its caption is darkened.
/// </summary>
/// <remarks>
/// <para><b>A window's non-client area is not part of the app's theme (TC4).</b> The caption bar,
/// its title and its close button are drawn by DWM from the OS scheme, so a dark WPF window whose
/// caption was never opted in ships a light bar above a dark body. Measured on a real screen
/// capture of the New Session sheet: a <c>#F9F1EF</c> caption with <c>#000000</c> text and a
/// <c>#C75050</c> close button, on <c>#12151A</c>.</para>
///
/// <para><b>Why a factory rather than a rule.</b> <c>MainWindow</c> opted in and the two dialogs did
/// not, and nothing about <c>new Window</c> suggests that it is a decision at all. Making the
/// factory the only way to get a window turns "remember to opt the caption in" into "there is
/// nowhere else to get one" — and <c>DialogChromeTests</c> fails the build when a second
/// <c>new Window</c> appears in the product.</para>
///
/// <para><b>The corner preference is separate, and deliberately so.</b> Rounded corners belong to
/// the shell window; a fixed-size tool-window dialog takes the system's own decision. Both are
/// hints: pre-Windows-11 and unusual composition states ignore them, and a normal square window is
/// the correct outcome there rather than a failure.</para>
/// </remarks>
internal static class DarkCaption
{
    /// <summary>
    /// Creates a modal dialog window that is themed on both sides of the frame.
    /// </summary>
    /// <param name="title">The caption text.</param>
    /// <param name="owner">The window to centre on. Null centres on screen.</param>
    /// <param name="width">The dialog's fixed width; its height follows its content.</param>
    /// <param name="ground">
    /// The resource key for the window's ground. A key rather than a brush so the window cannot
    /// carry a colour the palette does not have (TC5).
    /// </param>
    internal static Window CreateDialog(
        string title, Window? owner, double width, string ground = "SurfaceRaisedBrush")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(ground);

        var window = new Window
        {
            Title = title,
            Width = width,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = owner is null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner,
            WindowStyle = WindowStyle.ToolWindow,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Owner = owner,
        };

        window.SetResourceReference(Window.BackgroundProperty, ground);
        Apply(window, round: false);
        return window;
    }

    /// <summary>Darkens a window's caption as soon as it has a handle.</summary>
    /// <remarks>
    /// Hooked on <see cref="Window.SourceInitialized"/> rather than called directly, because the
    /// attribute needs an HWND and a WPF window has none until then. A window that is never shown
    /// simply never runs this, which is the right answer for one.
    /// </remarks>
    internal static void Apply(Window window, bool round)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.SourceInitialized += (_, _) =>
            ApplyToHandle(new WindowInteropHelper(window).Handle, round);
    }

    /// <summary>Darkens the caption of an existing handle, and optionally rounds its corners.</summary>
    internal static void ApplyToHandle(IntPtr hwnd, bool round)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var dark = 1;
        _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));

        if (!round)
        {
            return;
        }

        var corner = DWMWCP_ROUND;
        _ = DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
    }

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    [DllImport("dwmapi.dll", SetLastError = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
