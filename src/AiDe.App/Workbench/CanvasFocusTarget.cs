using System.Runtime.InteropServices;
using System.Windows.Interop;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// <see cref="ICanvasFocusTarget"/> over the WPF <c>WebView2</c>, which is an <see cref="HwndHost"/>.
/// </summary>
/// <remarks>
/// <para><b>Win32, because the managed route does not exist.</b> <c>CoreWebView2Controller.MoveFocus</c>
/// is the documented way to hand focus to web content, and the WPF control exposes no controller at
/// all — established by enumerating its public declared surface, which contains exactly two
/// focus-related members, both <c>FocusVisualStyle</c> (spike S4, finding 6). The API names DO appear
/// in the assembly's string table, so a grep would have confirmed a design that could not be built.</para>
///
/// <para><b>The read-back is the contract.</b> <c>SetFocus</c> returns the <i>previously</i> focused
/// window, and its null return is ambiguous between "failed" and "nothing had focus" — so it cannot
/// distinguish success from failure. <c>GetFocus</c> is asked afterwards, and focus landing on a
/// <i>descendant</i> counts, because the browser's own input window is a child of the host.</para>
/// </remarks>
public sealed class CanvasFocusTarget(HwndHost host, Func<bool> isObscured) : ICanvasFocusTarget
{
    private readonly HwndHost _host = host ?? throw new ArgumentNullException(nameof(host));
    private readonly Func<bool> _isObscured = isObscured ?? throw new ArgumentNullException(nameof(isObscured));

    [DllImport("user32.dll")] private static extern IntPtr SetFocus(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern IntPtr GetFocus();
    [DllImport("user32.dll")] private static extern IntPtr GetParent(IntPtr hWnd);

    public bool IsReady => SafeHandle != IntPtr.Zero;

    public bool IsObscured => _isObscured();

    public bool TryFocus()
    {
        var handle = SafeHandle;
        if (handle == IntPtr.Zero) return false;

        SetFocus(handle);

        var landed = GetFocus();
        return landed == handle || IsDescendantOf(landed, handle);
    }

    /// <summary>
    /// The child handle, or zero when the control has not been created. Reading <c>Handle</c> on an
    /// uncreated <see cref="HwndHost"/> throws rather than returning zero.
    /// </summary>
    private IntPtr SafeHandle
    {
        get
        {
            try { return _host.Handle; }
            catch (InvalidOperationException) { return IntPtr.Zero; }
        }
    }

    private static bool IsDescendantOf(IntPtr candidate, IntPtr ancestor)
    {
        if (candidate == IntPtr.Zero) return false;

        for (var current = candidate; current != IntPtr.Zero; current = GetParent(current))
        {
            if (current == ancestor) return true;
        }

        return false;
    }
}
