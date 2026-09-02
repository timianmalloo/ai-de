using System.Text;
using System.Windows.Input;
using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// Key presses to input bytes.
/// </summary>
/// <remarks>
/// Every case is a lookup with an exact right answer, and the cost of one wrong entry is a key that
/// silently does nothing — the most common way a terminal feels broken while looking fine. Tested
/// away from the control because none of it needs a window, and behind one it would be verified by
/// pressing keys and watching.
/// </remarks>
public sealed class TerminalInputTests
{
    private static string Bytes(ReadOnlyMemory<byte> bytes) =>
        string.Concat(bytes.ToArray().Select(b => b < 0x20 || b == 0x7F ? $"<{b:X2}>" : ((char)b).ToString()));

    [Fact]
    public void Enter_SendsCarriageReturn_NotLineFeed()
    {
        // A shell reads CR as "run this". LF alone leaves the line unsubmitted, which presents as a
        // terminal that accepts typing and never does anything.
        Assert.Equal("<0D>", Bytes(TerminalInput.ForKey(Key.Enter, ModifierKeys.None)));
    }

    [Fact]
    public void Backspace_SendsBs_WhichIsWhatWindowsConsolesExpect()
    {
        Assert.Equal("<08>", Bytes(TerminalInput.ForKey(Key.Back, ModifierKeys.None)));
    }

    [Theory]
    [InlineData(Key.Up, "<1B>[A")]
    [InlineData(Key.Down, "<1B>[B")]
    [InlineData(Key.Right, "<1B>[C")]
    [InlineData(Key.Left, "<1B>[D")]
    [InlineData(Key.Home, "<1B>[H")]
    [InlineData(Key.End, "<1B>[F")]
    public void ArrowsAndHomeEnd_SendTheirCsiSequences(Key key, string expected)
    {
        Assert.Equal(expected, Bytes(TerminalInput.ForKey(key, ModifierKeys.None)));
    }

    [Theory]
    [InlineData(Key.Up, "<1B>OA")]
    [InlineData(Key.Down, "<1B>OB")]
    [InlineData(Key.Right, "<1B>OC")]
    [InlineData(Key.Left, "<1B>OD")]
    [InlineData(Key.Home, "<1B>OH")]
    [InlineData(Key.End, "<1B>OF")]
    public void InApplicationCursorMode_ArrowsAndHomeEnd_SendSs3_NotCsi(Key key, string expected)
    {
        // DECCKM (ESC [ ? 1 h): a full-screen TUI turns this on and then only recognises SS3 arrows.
        // Sending CSI here is what leaves the arrows dead in the Claude Code menu (smoke 9-2).
        Assert.Equal(expected, Bytes(TerminalInput.ForKey(key, ModifierKeys.None, applicationCursorKeys: true)));
    }

    [Theory]
    [InlineData(Key.Insert, "<1B>[2~")]
    [InlineData(Key.Delete, "<1B>[3~")]
    [InlineData(Key.PageUp, "<1B>[5~")]
    [InlineData(Key.PageDown, "<1B>[6~")]
    public void TheEditingKeys_SendTheirTildeForms(Key key, string expected)
    {
        Assert.Equal(expected, Bytes(TerminalInput.ForKey(key, ModifierKeys.None)));
    }

    [Fact]
    public void ControlC_SendsThreeSoARunawayCommandCanBeInterrupted()
    {
        // The single most important key in a terminal. Without it the only way to stop a command is
        // to close the pane and kill the process.
        Assert.Equal("<03>", Bytes(TerminalInput.ForKey(Key.C, ModifierKeys.Control)));
    }

    [Theory]
    [InlineData(Key.A, 1)]
    [InlineData(Key.D, 4)]
    [InlineData(Key.Z, 26)]
    public void ControlLetters_AreThePositionInTheAlphabet(Key key, byte expected)
    {
        Assert.Equal(expected, TerminalInput.ForKey(key, ModifierKeys.Control).ToArray().Single());
    }

    [Fact]
    public void AnUnmappedKey_SendsNothing()
    {
        // Empty rather than a guess: a key that sends a plausible-but-wrong byte is worse than one
        // that does nothing, because the shell acts on it.
        Assert.True(TerminalInput.ForKey(Key.F13, ModifierKeys.None).IsEmpty);
    }

    [Theory]
    [InlineData(Key.F1, "<1B>OP")]
    [InlineData(Key.F4, "<1B>OS")]
    [InlineData(Key.F5, "<1B>[15~")]
    [InlineData(Key.F10, "<1B>[21~")]
    [InlineData(Key.F12, "<1B>[24~")]
    public void FunctionKeys_SendTheirSs3OrTildeForms(Key key, string expected)
    {
        Assert.Equal(expected, Bytes(TerminalInput.ForKey(key, ModifierKeys.None)));
    }

    [Fact]
    public void ShiftTab_SendsBackTab_NotATab()
    {
        // CBT (ESC [ Z). A literal tab here would move forward, the opposite of what Shift+Tab means.
        Assert.Equal("<1B>[Z", Bytes(TerminalInput.ForKey(Key.Tab, ModifierKeys.Shift)));
    }

    [Theory]
    [InlineData(Key.Right, ModifierKeys.Control, "<1B>[1;5C")]   // Ctrl+Right — word forward
    [InlineData(Key.Left, ModifierKeys.Control, "<1B>[1;5D")]    // Ctrl+Left — word back
    [InlineData(Key.Up, ModifierKeys.Shift, "<1B>[1;2A")]       // Shift+Up — selection
    [InlineData(Key.End, ModifierKeys.Shift, "<1B>[1;2F")]      // Shift+End — select to end
    public void ModifiedCursorKeys_SendTheCsiModifierForm(Key key, ModifierKeys modifiers, string expected)
    {
        // Modified cursor keys are CSI-with-modifier regardless of DECCKM — a TUI reads these for word
        // navigation and selection.
        Assert.Equal(expected, Bytes(TerminalInput.ForKey(key, modifiers)));
        Assert.Equal(expected, Bytes(TerminalInput.ForKey(key, modifiers, applicationCursorKeys: true)));
    }

    [Fact]
    public void PlainLetters_AreNotMappedHere_BecauseTheyArriveAsText()
    {
        // Mapping characters from key codes is what breaks every non-US layout: the key that is
        // physically 'Q' is not 'Q' on an AZERTY keyboard, but the text event says what was typed.
        Assert.True(TerminalInput.ForKey(Key.A, ModifierKeys.None).IsEmpty);
    }

    [Fact]
    public void Text_IsSentAsUtf8()
    {
        Assert.Equal(
            Encoding.UTF8.GetBytes("héllo"),
            TerminalInput.ForText("héllo").ToArray());
    }

    [Fact]
    public void EmptyText_SendsNothing()
    {
        Assert.True(TerminalInput.ForText(string.Empty).IsEmpty);
    }
}
