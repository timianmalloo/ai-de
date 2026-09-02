using AiDe.App.Workbench;
using Xunit;

namespace AiDe.App.Tests;

/// <summary>
/// Pointer events to the bytes a terminal expects under mouse tracking. Two encodings — SGR (the
/// modern, unbounded form) and legacy (offset-by-32, capped at 223) — each with an exact right answer.
/// </summary>
public sealed class TerminalMouseTests
{
    private static string Bytes(ReadOnlyMemory<byte> bytes) =>
        string.Concat(bytes.ToArray().Select(b => b < 0x20 || b == 0x7F ? $"<{b:X2}>" : ((char)b).ToString()));

    [Fact]
    public void Sgr_LeftPress_EncodesButtonAndOneBasedCell()
    {
        // ESC [ < 0 ; col+1 ; row+1 M — left button, column 5, row 2.
        Assert.Equal("<1B>[<0;6;3M",
            Bytes(TerminalMouse.Encode(TerminalMouseButton.Left, release: false, column: 5, row: 2, sgr: true)));
    }

    [Fact]
    public void Sgr_Release_UsesLowercaseM()
    {
        Assert.Equal("<1B>[<0;6;3m",
            Bytes(TerminalMouse.Encode(TerminalMouseButton.Left, release: true, column: 5, row: 2, sgr: true)));
    }

    [Fact]
    public void Sgr_WheelUp_UsesButtonCode64()
    {
        Assert.Equal("<1B>[<64;1;1M",
            Bytes(TerminalMouse.Encode(TerminalMouseButton.WheelUp, release: false, column: 0, row: 0, sgr: true)));
    }

    [Fact]
    public void Legacy_LeftPress_OffsetsEachByte32()
    {
        // ESC [ M (32+0) (32+0+1) (32+0+1) = ESC [ M <space> ! !
        Assert.Equal("<1B>[M !!",
            Bytes(TerminalMouse.Encode(TerminalMouseButton.Left, release: false, column: 0, row: 0, sgr: false)));
    }

    [Fact]
    public void Legacy_Release_UsesButtonCode3()
    {
        // 32 + 3 = 35 = '#'.
        Assert.Equal("<1B>[M#!!",
            Bytes(TerminalMouse.Encode(TerminalMouseButton.Left, release: true, column: 0, row: 0, sgr: false)));
    }

    [Fact]
    public void Legacy_BeyondColumn223_SendsNothing_RatherThanAWrongByte()
        => Assert.True(TerminalMouse.Encode(TerminalMouseButton.Left, false, column: 300, row: 0, sgr: false).IsEmpty);

    [Fact]
    public void OffGrid_SendsNothing()
        => Assert.True(TerminalMouse.Encode(TerminalMouseButton.Left, false, column: -1, row: 0, sgr: true).IsEmpty);
}
