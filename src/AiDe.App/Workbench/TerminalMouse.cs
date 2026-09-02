using System.Text;

namespace AiDe.App.Workbench;

/// <summary>The pointer button a mouse report names.</summary>
public enum TerminalMouseButton
{
    Left = 0,
    Middle = 1,
    Right = 2,

    /// <summary>Wheel up — reported as a button press with code 64.</summary>
    WheelUp = 64,

    /// <summary>Wheel down — reported as a button press with code 65.</summary>
    WheelDown = 65,
}

/// <summary>
/// Encodes a pointer event into the bytes a terminal expects when the child has enabled mouse tracking.
/// </summary>
/// <remarks>
/// Kept separate from the view so the wire format is testable without a window — every case is a
/// lookup with an exact right answer, and a wrong byte is a click that lands on the wrong cell or does
/// nothing. Two encodings exist: <b>SGR</b> (<c>ESC [ &lt; b ; col ; row M/m</c>, xterm <c>?1006</c>),
/// which is unbounded and distinguishes press from release; and the <b>legacy</b> form
/// (<c>ESC [ M b col row</c>, each byte offset by 32), which cannot address past column/row 223.
/// </remarks>
public static class TerminalMouse
{
    private const byte Escape = 0x1B;

    /// <summary>
    /// Encodes a button press/release (or a wheel notch) at a 0-based cell. Returns empty for an
    /// off-grid coordinate, or one the legacy form cannot represent.
    /// </summary>
    public static ReadOnlyMemory<byte> Encode(
        TerminalMouseButton button, bool release, int column, int row, bool sgr)
    {
        if (column < 0 || row < 0)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        var code = (int)button;

        if (sgr)
        {
            // ESC [ < code ; col+1 ; row+1 (M for press/wheel, m for release). Coordinates are 1-based.
            var final = release ? 'm' : 'M';
            return Encoding.ASCII.GetBytes($"\u001b[<{code};{column + 1};{row + 1}{final}");
        }

        // Legacy: ESC [ M (32+cb) (32+col+1) (32+row+1). A release reports button code 3.
        var cb = release ? 3 : code;
        var cx = column + 1 + 32;
        var cy = row + 1 + 32;
        if (cb + 32 > 255 || cx > 255 || cy > 255)
        {
            return ReadOnlyMemory<byte>.Empty; // legacy encoding tops out at 223 — SGR has no such limit
        }

        return new byte[] { Escape, (byte)'[', (byte)'M', (byte)(cb + 32), (byte)cx, (byte)cy };
    }
}
