using System.Globalization;
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
        var shift = (modifiers & ModifierKeys.Shift) != 0;

        if (control && key is >= Key.A and <= Key.Z)
        {
            // Ctrl+A is 1, Ctrl+C is 3, Ctrl+Z is 26 — the letter's position in the alphabet. This
            // is the definition of a control character, not a convention we chose.
            return new[] { (byte)(key - Key.A + 1) };
        }

        // Shift+Tab is back-tab (CBT, ESC [ Z), the standard "focus/selection backward" a TUI reads —
        // not a literal tab.
        if (key == Key.Tab && shift)
        {
            return Csi('Z');
        }

        // Cursor keys (arrows, Home, End). A MODIFIED cursor key is the CSI form with a modifier
        // parameter (ESC [ 1 ; mod X) regardless of DECCKM — this is how a TUI reads Ctrl+arrow for
        // word navigation and Shift+arrow for selection. An UNMODIFIED cursor key follows DECCKM: SS3
        // in application mode, CSI otherwise. Sending CSI unconditionally is what leaves the arrows
        // dead in a TUI that asked for application mode (smoke 9-2).
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
            var mod = ModifierParameter(modifiers);
            if (mod > 1)
            {
                return CsiModified(cursorFinal, mod);
            }

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

            // Function keys. F1–F4 are SS3 (ESC O P..S); F5–F12 are the tilde forms, whose numbers
            // (15,17–21,23,24) carry the historical xterm gaps that every TUI still matches on.
            Key.F1 => Ss3('P'),
            Key.F2 => Ss3('Q'),
            Key.F3 => Ss3('R'),
            Key.F4 => Ss3('S'),
            Key.F5 => CsiTilde(15),
            Key.F6 => CsiTilde(17),
            Key.F7 => CsiTilde(18),
            Key.F8 => CsiTilde(19),
            Key.F9 => CsiTilde(20),
            Key.F10 => CsiTilde(21),
            Key.F11 => CsiTilde(23),
            Key.F12 => CsiTilde(24),

            _ => ReadOnlyMemory<byte>.Empty,
        };
    }

    /// <summary>The bytes for composed text input.</summary>
    public static ReadOnlyMemory<byte> ForText(string text) =>
        string.IsNullOrEmpty(text) ? ReadOnlyMemory<byte>.Empty : Encoding.UTF8.GetBytes(text);

    /// <summary>
    /// The bytes for pasted text. When <paramref name="bracketed"/> (the child enabled bracketed paste),
    /// the text is wrapped in <c>ESC [ 200~ … ESC [ 201~</c> so the program treats it as one paste rather
    /// than running each line as it arrives. Carriage returns in the paste are normalized to CR so a
    /// multi-line paste lands as the child expects.
    /// </summary>
    public static ReadOnlyMemory<byte> ForPaste(string text, bool bracketed)
    {
        if (string.IsNullOrEmpty(text)) { return ReadOnlyMemory<byte>.Empty; }

        // Normalize Windows CRLF / lone LF to CR — a terminal submits lines on CR.
        var normalized = text.Replace("\r\n", "\r").Replace('\n', '\r');
        var body = Encoding.UTF8.GetBytes(normalized);
        if (!bracketed) { return body; }

        var start = "\u001b[200~"u8;
        var end = "\u001b[201~"u8;
        var buffer = new byte[start.Length + body.Length + end.Length];
        start.CopyTo(buffer);
        body.CopyTo(buffer.AsSpan(start.Length));
        end.CopyTo(buffer.AsSpan(start.Length + body.Length));
        return buffer;
    }

    // xterm modifier encoding: 1 + Shift(1) + Alt(2) + Ctrl(4). 1 means "no modifier".
    private static int ModifierParameter(ModifierKeys modifiers)
    {
        var code = 0;
        if ((modifiers & ModifierKeys.Shift) != 0) { code += 1; }
        if ((modifiers & ModifierKeys.Alt) != 0) { code += 2; }
        if ((modifiers & ModifierKeys.Control) != 0) { code += 4; }
        return code + 1;
    }

    private static byte[] Csi(char final) => [Escape, (byte)'[', (byte)final];

    private static byte[] Ss3(char final) => [Escape, (byte)'O', (byte)final];

    // ESC [ 1 ; <mod> <final> — the modified cursor-key form.
    private static byte[] CsiModified(char final, int mod)
    {
        var digits = mod.ToString(CultureInfo.InvariantCulture);
        var bytes = new byte[4 + digits.Length + 1];
        var i = 0;
        bytes[i++] = Escape;
        bytes[i++] = (byte)'[';
        bytes[i++] = (byte)'1';
        bytes[i++] = (byte)';';
        foreach (var d in digits) { bytes[i++] = (byte)d; }
        bytes[i] = (byte)final;
        return bytes;
    }

    // ESC [ <number> ~ — handles multi-digit numbers (F5 is 15, F12 is 24).
    private static byte[] CsiTilde(int number)
    {
        var digits = number.ToString(CultureInfo.InvariantCulture);
        var bytes = new byte[2 + digits.Length + 1];
        var i = 0;
        bytes[i++] = Escape;
        bytes[i++] = (byte)'[';
        foreach (var d in digits) { bytes[i++] = (byte)d; }
        bytes[i] = (byte)'~';
        return bytes;
    }
}
