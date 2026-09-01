namespace AiDe.Core.Terminal;

/// <summary>How a cell's colour is specified.</summary>
public enum TerminalColorKind
{
    /// <summary>The renderer's own foreground or background. Not a palette entry.</summary>
    Default,

    /// <summary>An index into the 256-colour palette. 0–15 are the ANSI colours.</summary>
    Indexed,

    /// <summary>A direct 24-bit colour.</summary>
    Rgb,
}

/// <summary>
/// A cell colour, as the wire expresses it.
/// </summary>
/// <remarks>
/// Deliberately not a rendering type. Keeping the model in *terminal* terms — "palette index 4",
/// not "this shade of blue" — is what lets the theme decide what index 4 looks like, and what keeps
/// the whole screen model free of a UI framework. <see cref="Default"/> is a distinct case rather
/// than a magic index because "whatever the theme's foreground is" is not a colour.
/// </remarks>
public readonly record struct TerminalColor(TerminalColorKind Kind, int Index, byte R, byte G, byte B)
{
    public static TerminalColor Default => new(TerminalColorKind.Default, 0, 0, 0, 0);

    public static TerminalColor FromIndex(int index) =>
        new(TerminalColorKind.Indexed, Math.Clamp(index, 0, 255), 0, 0, 0);

    public static TerminalColor FromRgb(byte r, byte g, byte b) =>
        new(TerminalColorKind.Rgb, 0, r, g, b);
}

/// <summary>Non-colour styling carried by a cell.</summary>
[Flags]
public enum CellAttributes
{
    None = 0,
    Bold = 1 << 0,
    Underline = 1 << 1,

    /// <summary>Foreground and background swap at draw time. Used for selections and cursors.</summary>
    Inverse = 1 << 2,
}

/// <summary>One character cell.</summary>
public readonly record struct TerminalCell(
    char Character,
    TerminalColor Foreground,
    TerminalColor Background,
    CellAttributes Attributes)
{
    public static TerminalCell Blank => new(' ', TerminalColor.Default, TerminalColor.Default, CellAttributes.None);
}

/// <summary>The style subsequent writes are drawn in — the terminal's current pen.</summary>
public readonly record struct TerminalPen(
    TerminalColor Foreground,
    TerminalColor Background,
    CellAttributes Attributes)
{
    public static TerminalPen Default => new(TerminalColor.Default, TerminalColor.Default, CellAttributes.None);
}

/// <summary>How much of a line or screen an erase covers.</summary>
public enum EraseExtent
{
    /// <summary>From the cursor to the end, inclusive.</summary>
    ToEnd,

    /// <summary>From the start to the cursor, inclusive.</summary>
    ToStart,

    /// <summary>Everything.</summary>
    All,
}

/// <summary>
/// What a terminal is, once the bytes have been interpreted: a grid of styled cells and a cursor.
/// </summary>
/// <remarks>
/// <para><b>No WPF, by design.</b> Every rule here — wrapping, scrolling, what an erase covers,
/// where the cursor lands — is a data-structure question. Behind a rendering framework each one
/// would be testable only by drawing pixels and reading them back, and the rules would be verified
/// approximately or not at all. The renderer draws this; deciding what it contains is this type's
/// job.</para>
///
/// <para><b>Scrolling, not scrollback.</b> When the cursor passes the last row the grid shifts up
/// and the top row is discarded. History is a separate feature with its own memory budget, and
/// growing this buffer to provide it would put an unbounded allocation behind an innocuous property
/// — with the child process choosing how much.
/// simplify: viewport only; ceiling is one screen; upgrade trigger = scrollback becomes a
/// requirement, at which point it arrives as a bounded ring beside this, not inside it.</para>
///
/// <para><b>Nothing here throws on bad input.</b> Every coordinate is clamped, because the values
/// arrive in escape sequences written by an untrusted process and an exception reachable by
/// printing is a denial of service.</para>
///
/// <para>Not thread-safe: one screen belongs to one session's parser, and the renderer reads it on
/// the UI thread between writes.</para>
/// </remarks>
public sealed class TerminalScreen
{
    private const int TabStop = 8;

    private TerminalCell[] _cells;

    public TerminalScreen(int columns, int rows)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);

        Columns = columns;
        Rows = rows;
        _cells = NewGrid(columns, rows, TerminalCell.Blank);
    }

    public int Columns { get; private set; }

    public int Rows { get; private set; }

    public int CursorRow { get; private set; }

    public int CursorColumn { get; private set; }

    /// <summary>The style applied to subsequent writes and erases.</summary>
    public TerminalPen Pen { get; set; } = TerminalPen.Default;

    /// <summary>
    /// Has anything changed since <see cref="ClearDirty"/>?
    /// </summary>
    /// <remarks>
    /// The renderer presents on a timer to coalesce a fast producer into frames. Without this flag
    /// it would redraw a motionless screen at frame rate forever — the cost the coalescing policy
    /// exists to avoid, paid continuously instead of never.
    /// </remarks>
    public bool IsDirty { get; private set; } = true;

    public TerminalCell this[int row, int column] => _cells[(row * Columns) + column];

    /// <summary>
    /// The cell under the cursor, or <c>null</c> when the cursor is not on a real cell. The cursor
    /// legitimately sits off the grid at the <b>pending-wrap</b> column (<c>CursorColumn == Columns</c>,
    /// held after writing the last column until the next write wraps), and can be left outside the grid
    /// by other sequences. The renderer MUST read the character-under-cursor through this, never through
    /// the raw indexer: an out-of-bounds index in <c>OnRender</c> throws on the WPF UI thread, which is
    /// unhandled and terminates the whole application (DC-063).
    /// </summary>
    public TerminalCell? CellUnderCursor() =>
        CursorRow >= 0 && CursorRow < Rows && CursorColumn >= 0 && CursorColumn < Columns
            ? _cells[(CursorRow * Columns) + CursorColumn]
            : null;

    public void ClearDirty() => IsDirty = false;

    /// <summary>Writes text at the cursor, wrapping and scrolling as needed.</summary>
    public void Write(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        foreach (var c in text)
        {
            Write(c);
        }
    }

    /// <summary>Writes one character at the cursor.</summary>
    public void Write(char character)
    {
        if (CursorColumn >= Columns)
        {
            CursorColumn = 0;
            LineFeed();
        }

        _cells[(CursorRow * Columns) + CursorColumn] =
            new TerminalCell(character, Pen.Foreground, Pen.Background, Pen.Attributes);

        CursorColumn++;
        IsDirty = true;
    }

    public void CarriageReturn()
    {
        CursorColumn = 0;
        IsDirty = true;
    }

    /// <summary>
    /// Moves down one row, scrolling when already at the bottom. The column is unchanged.
    /// </summary>
    /// <remarks>
    /// Column-preserving is correct even though it looks like an omission: a shell that wants column
    /// zero sends CR with the LF, and a terminal that moved the column itself would break every
    /// program that relies on the distinction.
    /// </remarks>
    public void LineFeed()
    {
        if (CursorRow < Rows - 1)
        {
            CursorRow++;
        }
        else
        {
            ScrollUp();
        }

        IsDirty = true;
    }

    /// <summary>Moves left one cell without erasing.</summary>
    /// <remarks>
    /// Erasing here would delete twice: a shell removing a character sends BS, space, BS, and a
    /// destructive backspace would consume the character before the space arrived to do it.
    /// </remarks>
    public void Backspace()
    {
        if (CursorColumn > 0)
        {
            CursorColumn--;
            IsDirty = true;
        }
    }

    /// <summary>Moves to the next eight-column tab stop, stopping at the last column.</summary>
    public void Tab()
    {
        var next = ((CursorColumn / TabStop) + 1) * TabStop;
        CursorColumn = Math.Min(next, Columns - 1);
        IsDirty = true;
    }

    /// <summary>Places the cursor, clamped into the screen.</summary>
    public void MoveCursor(int row, int column)
    {
        CursorRow = Math.Clamp(row, 0, Rows - 1);
        CursorColumn = Math.Clamp(column, 0, Columns - 1);
        IsDirty = true;
    }

    public void EraseInLine(EraseExtent extent)
    {
        var (from, to) = extent switch
        {
            EraseExtent.ToEnd => (CursorColumn, Columns - 1),
            EraseExtent.ToStart => (0, CursorColumn),
            _ => (0, Columns - 1),
        };

        for (var column = from; column <= to; column++)
        {
            _cells[(CursorRow * Columns) + column] = Erased();
        }

        IsDirty = true;
    }

    public void EraseInDisplay(EraseExtent extent)
    {
        switch (extent)
        {
            case EraseExtent.ToEnd:
                EraseInLine(EraseExtent.ToEnd);
                FillRows(CursorRow + 1, Rows - 1);
                break;

            case EraseExtent.ToStart:
                EraseInLine(EraseExtent.ToStart);
                FillRows(0, CursorRow - 1);
                break;

            default:
                FillRows(0, Rows - 1);
                break;
        }

        IsDirty = true;
    }

    /// <summary>Resizes the grid, keeping the content that still fits.</summary>
    /// <remarks>
    /// Content is preserved by position rather than reflowed. Reflowing is what a user expects when
    /// narrowing a window over wrapped prose, and it needs a record of which line breaks were
    /// *wrapped* versus *written* — which this model does not keep, and inventing it here would be
    /// guessing at where text belongs.
    /// simplify: truncate rather than reflow; ceiling is a resize losing off-screen content;
    /// upgrade trigger = the model gains wrapped-line provenance.
    /// </remarks>
    public void Resize(int columns, int rows)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);

        if (columns == Columns && rows == Rows)
        {
            return;
        }

        var replacement = NewGrid(columns, rows, TerminalCell.Blank);

        for (var row = 0; row < Math.Min(rows, Rows); row++)
        {
            for (var column = 0; column < Math.Min(columns, Columns); column++)
            {
                replacement[(row * columns) + column] = _cells[(row * Columns) + column];
            }
        }

        _cells = replacement;
        Columns = columns;
        Rows = rows;

        CursorRow = Math.Clamp(CursorRow, 0, rows - 1);
        CursorColumn = Math.Clamp(CursorColumn, 0, columns - 1);
        IsDirty = true;
    }

    private void ScrollUp()
    {
        Array.Copy(_cells, Columns, _cells, 0, (Rows - 1) * Columns);

        for (var column = 0; column < Columns; column++)
        {
            _cells[((Rows - 1) * Columns) + column] = Erased();
        }
    }

    private void FillRows(int from, int to)
    {
        for (var row = Math.Max(from, 0); row <= Math.Min(to, Rows - 1); row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                _cells[(row * Columns) + column] = Erased();
            }
        }
    }

    /// <summary>
    /// A blank cell in the <i>current</i> background, which is how full-screen programs paint.
    /// </summary>
    /// <remarks>
    /// Erasing to the default background instead would make "set a background, clear the screen" —
    /// the first thing every full-screen tool does — paint nothing.
    /// </remarks>
    private TerminalCell Erased() =>
        new(' ', TerminalColor.Default, Pen.Background, CellAttributes.None);

    private static TerminalCell[] NewGrid(int columns, int rows, TerminalCell fill)
    {
        var cells = new TerminalCell[columns * rows];
        Array.Fill(cells, fill);
        return cells;
    }
}
