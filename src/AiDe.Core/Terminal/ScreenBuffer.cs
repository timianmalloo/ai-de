using System.Text;

namespace AiDe.Core.Terminal;

/// <summary>
/// A terminal screen: what the user would see, rather than the order the bytes arrived in.
/// </summary>
/// <remarks>
/// <para><b>Measured need, not speculative generality.</b> <c>spikes/agent-readiness</c> captured a
/// real agent CLI and found it draws with absolute cursor addressing — <c>ESC[3;2H</c>,
/// <c>ESC[14;2H</c>, <c>ESC[15;4H</c> — repainting regions in whatever order it likes. A pattern
/// matched against the byte stream is asking where the cursor went last, which for a line-oriented
/// shell is the same question as "what does the screen say" and for an agent is not. No cleverer
/// regex closes that gap: the information is not in the ordering of the bytes.</para>
///
/// <para><b>Deliberately small.</b> It interprets cursor movement, erasure and text — the sequences
/// that decide where a character lands. Colour, styling, scroll regions and alternate buffers are
/// consumed and discarded, because none of them change WHICH CELL a character occupies, and every
/// one of them would be code carrying no question anyone is asking yet. This is not a terminal
/// emulator and must not become one by accident; the renderer is Chromium's.</para>
///
/// <para><b>Unknown sequences are skipped, never printed.</b> A parser that fell through to "write
/// the bytes as text" would put escape codes into the screen it is supposed to be modelling, and the
/// readiness pattern would match text no human ever saw.</para>
/// </remarks>
public sealed class ScreenBuffer
{
    private readonly char[,] _cells;
    private readonly StringBuilder _parameters = new();

    private State _state = State.Ground;
    private int _row;
    private int _column;

    public ScreenBuffer(int rows = 30, int columns = 120)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);

        Rows = rows;
        Columns = columns;
        _cells = new char[rows, columns];
        Clear(0, (rows * columns) - 1);
    }

    private enum State { Ground, Escape, Csi, Osc, OscEscape }

    public int Rows { get; }

    public int Columns { get; }

    /// <summary>Feeds output through the parser.</summary>
    public void Feed(ReadOnlySpan<char> text)
    {
        foreach (var c in text)
        {
            switch (_state)
            {
                case State.Ground: Ground(c); break;
                case State.Escape: Escape(c); break;
                case State.Csi: Csi(c); break;
                case State.Osc: Osc(c); break;
                case State.OscEscape: OscEscape(c); break;
                default: break;
            }
        }
    }

    /// <summary>The screen as text, one line per row, trailing blanks removed.</summary>
    public string Render()
    {
        var builder = new StringBuilder(Rows * (Columns + 1));

        for (var row = 0; row < Rows; row++)
        {
            builder.Append(Line(row)).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>One row, trailing blanks removed.</summary>
    public string Line(int row)
    {
        if (row < 0 || row >= Rows) return string.Empty;

        var end = Columns - 1;
        while (end >= 0 && _cells[row, end] == ' ') end--;

        var builder = new StringBuilder(end + 1);
        for (var column = 0; column <= end; column++) builder.Append(_cells[row, column]);

        return builder.ToString();
    }

    /// <summary>
    /// The last row with anything on it.
    /// </summary>
    /// <remarks>
    /// The screen's analogue of "the tail". Where a shell's prompt is the last thing written, an
    /// agent's is the last thing DRAWN — and those are different rows in the same buffer.
    /// </remarks>
    public string LastNonEmptyLine()
    {
        for (var row = Rows - 1; row >= 0; row--)
        {
            var line = Line(row);
            if (line.Trim().Length > 0) return line;
        }

        return string.Empty;
    }

    private void Ground(char c)
    {
        switch (c)
        {
            case '\u001b': _state = State.Escape; _parameters.Clear(); break;
            case '\r': _column = 0; break;
            case '\n': NewLine(); break;
            case '\b': _column = Math.Max(0, _column - 1); break;
            case '\t': _column = Math.Min(Columns - 1, ((_column / 8) + 1) * 8); break;
            case '\a': break;
            default:
                if (char.IsControl(c)) break;
                Put(c);
                break;
        }
    }

    private void Escape(char c)
    {
        switch (c)
        {
            case '[': _state = State.Csi; break;
            case ']': _state = State.Osc; break;
            // ESC ( B and friends: a charset designation, one byte of payload.
            case '(' or ')' or '#' or '%': _state = State.Escape; break;
            default: _state = State.Ground; break;
        }
    }

    private void Csi(char c)
    {
        // Parameter and intermediate bytes accumulate; the first byte in @..~ ends the sequence.
        if (c is >= ' ' and <= '?')
        {
            _parameters.Append(c);
            return;
        }

        var parameters = _parameters.ToString();
        _parameters.Clear();
        _state = State.Ground;

        // Private sequences (ESC [ ? ...) are modes — bracketed paste, cursor visibility, alternate
        // screen. None of them move a character into a different cell.
        if (parameters.StartsWith('?')) return;

        var values = Values(parameters);

        switch (c)
        {
            case 'H' or 'f':
                _row = Clamp(Value(values, 0, 1) - 1, Rows);
                _column = Clamp(Value(values, 1, 1) - 1, Columns);
                break;
            case 'A': _row = Clamp(_row - Value(values, 0, 1), Rows); break;
            case 'B': _row = Clamp(_row + Value(values, 0, 1), Rows); break;
            case 'C': _column = Clamp(_column + Value(values, 0, 1), Columns); break;
            case 'D': _column = Clamp(_column - Value(values, 0, 1), Columns); break;
            case 'G': _column = Clamp(Value(values, 0, 1) - 1, Columns); break;
            case 'd': _row = Clamp(Value(values, 0, 1) - 1, Rows); break;
            case 'J': EraseDisplay(Value(values, 0, 0)); break;
            case 'K': EraseLine(Value(values, 0, 0)); break;
            default: break;
        }
    }

    private void Osc(char c)
    {
        // OSC runs until BEL or ST (ESC \). Its payload is a window title or a hyperlink — never
        // screen content — so it is consumed and dropped.
        if (c == '\a') { _state = State.Ground; return; }
        if (c == '\u001b') { _state = State.OscEscape; return; }
    }

    private void OscEscape(char c) => _state = c == '\\' ? State.Ground : State.Osc;

    private void Put(char c)
    {
        if (_column >= Columns)
        {
            _column = 0;
            NewLine();
        }

        _cells[_row, _column] = c;
        _column++;
    }

    private void NewLine()
    {
        if (_row < Rows - 1)
        {
            _row++;
            return;
        }

        // Scroll. The top row leaves the model entirely: this is a screen, not a scrollback, and
        // keeping history here would quietly rebuild the byte-stream problem one row at a time.
        for (var row = 1; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                _cells[row - 1, column] = _cells[row, column];
            }
        }

        for (var column = 0; column < Columns; column++) _cells[Rows - 1, column] = ' ';
    }

    private void EraseDisplay(int mode)
    {
        var cursor = (_row * Columns) + _column;

        switch (mode)
        {
            case 0: Clear(cursor, (Rows * Columns) - 1); break;
            case 1: Clear(0, cursor); break;
            default: Clear(0, (Rows * Columns) - 1); break;
        }
    }

    private void EraseLine(int mode)
    {
        var start = _row * Columns;

        switch (mode)
        {
            case 0: Clear(start + _column, start + Columns - 1); break;
            case 1: Clear(start, start + _column); break;
            default: Clear(start, start + Columns - 1); break;
        }
    }

    private void Clear(int from, int to)
    {
        for (var index = Math.Max(0, from); index <= Math.Min(to, (Rows * Columns) - 1); index++)
        {
            _cells[index / Columns, index % Columns] = ' ';
        }
    }

    private int Clamp(int value, int limit) => Math.Clamp(value, 0, limit - 1);

    private static int[] Values(string parameters) =>
        parameters.Length == 0
            ? []
            : [.. parameters.Split(';').Select(p => int.TryParse(p, out var v) ? v : 0)];

    private static int Value(int[] values, int index, int fallback) =>
        index < values.Length && values[index] > 0 ? values[index] : fallback;
}
