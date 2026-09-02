using System.Text;
using System.Windows.Input;

namespace AiDe.App.Workbench;

/// <summary>
/// Translates key presses into the bytes a terminal expects on its input stream.
/// </summary>
/// <remarks>
/// <para><b>Kept separate from the view so it is testable without a window.</b> Every entry below is
/// a lookup table with an exact right answer, and the cost of getting one wrong is a key that
/// silently does nothing — the single most common way a terminal feels broken. Behind a control
/// these would be verified by pressing keys.</para>
///
/// <para><b>Text goes through <see cref="ForText"/>, not through this table.</b> Composed input,
/// dead keys and IME sequences all produce text rather than key presses, so mapping characters from
/// key codes would break every non-US keyboard.</para>
///
/// <para><b>Control characters are computed, not enumerated.</b> Ctrl+A through Ctrl+Z are the
/// letter minus 64 by definition, and writing out twenty-six cases would be twenty-six chances to
/// mistype one.</para>
/// </remarks>
public static class TerminalInput
{
    private const byte Escape = 0x1B;

    /// <summary>The bytes for a key press, or empty when the key sends nothing.</summary>
    /// <param name="applicationCursorKeys">
    /// When the child has enabled DECCKM (application cursor key mode), the cursor keys are encoded as
    /// SS3 (<c>ESC O A</c>) rather than CSI (<c>ESC [ A</c>) — which is what a full-screen TUI expects.
    /// </param>
    public static ReadOnlyMemory<byte> ForKey(Key key, ModifierKeys modifiers, bool applicationCursorKeys = false)
    {
        var control = (modifiers & ModifierKeys.Control) != 0;

        if (control && key is >= Key.A and <= Key.Z)
        {
            // Ctrl+A is 1, Ctrl+C is 3, Ctrl+Z is 26 — the letter's position in the alphabet. This
            // is the definition of a control character, not a convention we chose.
            return new[] { (byte)(key - Key.A + 1) };
        }

        // Cursor keys (arrows, Home, End) switch encoding with DECCKM: SS3 in application mode, CSI
        // in normal mode. Sending CSI unconditionally is what leaves the arrows dead in a TUI that
        // asked for application mode (smoke 9-2).
        var cursorFinal = key switch
        {
            Key.Up => 'A',
            Key.Down => 'B',
            Key.Right => 'C',
            Key.Left => 'D',
            Key.Home => 'H',
            Key.End => 'F',
            _ => '\0',
        };
        if (cursorFinal != '\0')
        {
            return applicationCursorKeys ? Ss3(cursorFinal) : Csi(cursorFinal);
        }

        return key switch
        {
            Key.Enter => "\r"u8.ToArray(),
            Key.Tab => "\t"u8.ToArray(),
            Key.Escape => new[] { Escape },

            // BS rather than DEL. Windows consoles expect 0x08, and sending DEL puts a `^?` on the
            // line in the shells this product launches.
            Key.Back => new byte[] { 0x08 },

            // The tilde forms, which is what these four actually are on the wire.
            Key.Insert => CsiTilde(2),
            Key.Delete => CsiTilde(3),
            Key.PageUp => CsiTilde(5),
            Key.PageDown => CsiTilde(6),

            _ => ReadOnlyMemory<byte>.Empty,
        };
    }

    /// <summary>The bytes for composed text input.</summary>
    public static ReadOnlyMemory<byte> ForText(string text) =>
        string.IsNullOrEmpty(text) ? ReadOnlyMemory<byte>.Empty : Encoding.UTF8.GetBytes(text);

    private static byte[] Csi(char final) => [Escape, (byte)'[', (byte)final];

    private static byte[] Ss3(char final) => [Escape, (byte)'O', (byte)final];

    private static byte[] CsiTilde(int number) =>
        [Escape, (byte)'[', (byte)('0' + number), (byte)'~'];
}
