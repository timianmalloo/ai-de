using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Webview2SnapshotSwapSpike;

/// <summary>
/// Two ways of photographing the same window, whose disagreement is the airspace measurement.
/// </summary>
/// <remarks>
/// <para><c>RenderTargetBitmap</c> renders <b>WPF's visual tree only</b>. A hosted HWND — which is
/// what the WPF WebView2 control is — never appears in it, because WPF does not draw that content;
/// Windows composites it separately.</para>
///
/// <para><c>PrintWindow</c> with <c>PW_RENDERFULLCONTENT</c> captures the window <b>as composited</b>,
/// child HWNDs included.</para>
///
/// <para>So: sample the same pixel in both. If the composited capture shows web content where the
/// WPF capture shows a WPF element that is supposed to be on top, the HWND is winning and airspace
/// is real. This is a measurement rather than an inference — the alternative is reasoning about
/// WPF's composition model, which is exactly what IO1 says not to do.</para>
/// </remarks>
internal static class Capture
{
    private const uint PW_RENDERFULLCONTENT = 0x00000002;

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hwnd, out Rect32 rect);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(
        IntPtr hdc, IntPtr bitmap, uint start, uint lines, byte[]? bits, ref BitmapInfo info, uint usage);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect32
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width, Height;
        public ushort Planes, BitCount;
        public uint Compression, SizeImage;
        public int XPelsPerMeter, YPelsPerMeter;
        public uint ClrUsed, ClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;
        public uint Colors;
    }

    /// <summary>A pixel grid with an origin, so samples can be taken in the same coordinates twice.</summary>
    internal sealed record Photo(byte[] Bgra, int Width, int Height)
    {
        public (byte R, byte G, byte B) At(int x, int y)
        {
            x = Math.Clamp(x, 0, Width - 1);
            y = Math.Clamp(y, 0, Height - 1);
            var i = ((y * Width) + x) * 4;
            return (Bgra[i + 2], Bgra[i + 1], Bgra[i]);
        }

        /// <summary>The most common colour in a small box — robust to antialiasing and text.</summary>
        public (byte R, byte G, byte B) Dominant(int centreX, int centreY, int radius = 6)
        {
            var counts = new Dictionary<(byte, byte, byte), int>();
            for (var y = centreY - radius; y <= centreY + radius; y++)
            {
                for (var x = centreX - radius; x <= centreX + radius; x++)
                {
                    var colour = At(x, y);
                    counts[colour] = counts.GetValueOrDefault(colour) + 1;
                }
            }

            return counts.OrderByDescending(kv => kv.Value).First().Key;
        }
    }

    /// <summary>WPF's own view of the window — the visual tree, and nothing hosted.</summary>
    internal static Photo WpfVisualTree(Window window)
    {
        var source = (HwndSource)PresentationSource.FromVisual(window)!;
        var dpi = VisualTreeHelper.GetDpi(window);
        var width = (int)(window.ActualWidth * dpi.DpiScaleX);
        var height = (int)(window.ActualHeight * dpi.DpiScaleY);

        var target = new RenderTargetBitmap(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY,
            PixelFormats.Pbgra32);
        target.Render(window);

        var stride = width * 4;
        var buffer = new byte[stride * height];
        target.CopyPixels(buffer, stride, 0);
        _ = source;
        return new Photo(buffer, width, height);
    }

    /// <summary>The window as Windows composites it, hosted child HWNDs included.</summary>
    internal static Photo? Composited(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (!GetClientRect(hwnd, out var rect))
        {
            return null;
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        var windowDc = GetWindowDC(hwnd);
        var memoryDc = CreateCompatibleDC(windowDc);
        var bitmap = CreateCompatibleBitmap(windowDc, width, height);
        var previous = SelectObject(memoryDc, bitmap);

        try
        {
            if (!PrintWindow(hwnd, memoryDc, PW_RENDERFULLCONTENT))
            {
                return null;
            }

            var info = new BitmapInfo
            {
                Header = new BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    Width = width,
                    // Negative height requests a top-down DIB, matching RenderTargetBitmap's origin.
                    Height = -height,
                    Planes = 1,
                    BitCount = 32,
                    Compression = 0,
                },
            };

            var buffer = new byte[width * height * 4];
            var copied = GetDIBits(memoryDc, bitmap, 0, (uint)height, buffer, ref info, 0);
            return copied == 0 ? null : new Photo(buffer, width, height);
        }
        finally
        {
            SelectObject(memoryDc, previous);
            DeleteObject(bitmap);
            DeleteDC(memoryDc);
            ReleaseDC(hwnd, windowDc);
        }
    }

    internal static string Hex((byte R, byte G, byte B) c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    /// <summary>
    /// Which of the known surfaces a sampled pixel belongs to — the NEAREST, not one within a fixed
    /// tolerance.
    /// </summary>
    /// <remarks>
    /// An absolute tolerance was the wrong instrument and produced a wrong reading on the first run.
    /// `PrintWindow` returned crimson `#E11D48` as `#CC3C4B` — the browser's colour management and
    /// DWM both touch it — which is 31 away on the green channel and so failed a ±24 test, and the
    /// spike reported "inconclusive" about a pixel that is obviously red and obviously not green.
    ///
    /// The question was never "is this pixel exactly the colour I asked for". It is "which of these
    /// three surfaces owns this pixel", and that is a nearest-neighbour question. Encoding the real
    /// question removes the arbitrary threshold entirely (DC-009: a proxy fails differently from the
    /// invariant).
    /// </remarks>
    internal static string Classify(
        (byte R, byte G, byte B) sample, params (string Name, (byte R, byte G, byte B) Colour)[] candidates)
    {
        static double Distance((byte R, byte G, byte B) a, (byte R, byte G, byte B) b)
        {
            double dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
            return Math.Sqrt((dr * dr) + (dg * dg) + (db * db));
        }

        return candidates.OrderBy(c => Distance(sample, c.Colour)).First().Name;
    }

    /// <summary>The distance to a named candidate, so a close call can be reported rather than hidden.</summary>
    internal static string Margins(
        (byte R, byte G, byte B) sample, params (string Name, (byte R, byte G, byte B) Colour)[] candidates)
    {
        static double Distance((byte R, byte G, byte B) a, (byte R, byte G, byte B) b)
        {
            double dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
            return Math.Sqrt((dr * dr) + (dg * dg) + (db * db));
        }

        return string.Join("  ", candidates
            .OrderBy(c => Distance(sample, c.Colour))
            .Select(c => $"{c.Name}={Distance(sample, c.Colour):F0}"));
    }
}
