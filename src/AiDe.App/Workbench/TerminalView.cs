using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Media;
using AiDe.Core.Terminal;

namespace AiDe.App.Workbench;

/// <summary>
/// Draws a <see cref="TerminalScreen"/> and turns key presses into input bytes.
/// </summary>
/// <remarks>
/// <para><b>The draw path is binding, not a preference.</b> Spike S3 measured three ways of putting
/// a 200×50 screen on the display: <c>GlyphRun</c> per line at 6.64 ms p95, <c>FormattedText</c> per
/// line at 12.28 ms, and <c>FormattedText</c> per <i>cell</i> at 142.80 ms — 21× slower and four
/// times over the frame budget, at 7 fps. The per-cell design is the one a competent implementer
/// writes first, because a terminal genuinely is a grid of independently styled cells, and nothing
/// about it looks wrong until it is measured. That is why it is recorded here rather than left to be
/// rediscovered.</para>
///
/// <para><b>Runs, not whole lines.</b> A line rarely has one style, so cells are grouped into runs
/// of identical style and each run becomes one <c>GlyphRun</c>. That is the same shape as the
/// measured path — a handful of draws per line rather than one per cell — and it is what real
/// terminals do.</para>
///
/// <para><b>Presenting is decoupled from parsing.</b> The architecture budgets 1 MiB/s of sustained
/// output, which is an <i>output</i> rate and not a <i>draw</i> rate: a terminal coalesces, so it
/// must consume a megabyte a second while only ever showing the final state at frame rate. The two
/// differ by three orders of magnitude, and conflating them is how a renderer gets blamed for a
/// parser's cost. Here that means the screen is written by the reader and drawn on the rendering
/// tick, and only when <see cref="TerminalScreen.IsDirty"/> says something changed.</para>
/// </remarks>
public sealed class TerminalView : FrameworkElement
{
    private TerminalPalette _palette = new();

    /// <summary>Swaps the colour scheme this view draws with and repaints. Per-session (DC-029 keeps
    /// the same view instance alive), so schemes do not leak between terminals.</summary>
    public void ApplyPalette(TerminalPalette palette)
    {
        _palette = palette;
        InvalidateVisual();
    }
    private readonly GlyphTypeface _typeface;
    private readonly double _emSize;

    private ushort[] _glyphs = new ushort[64];
    private double[] _advances = new double[64];

    private TerminalScreen _screen;
    private bool _rendering;

    public TerminalView(TerminalScreen screen, double fontSize = 13)
    {
        _screen = screen ?? throw new ArgumentNullException(nameof(screen));
        _emSize = fontSize;

        _typeface = LoadMonospace();
        CellWidth = _typeface.AdvanceWidths[_typeface.CharacterToGlyphMap['M']] * _emSize;
        CellHeight = Math.Ceiling(_typeface.Height * _emSize);
        Baseline = _typeface.Baseline * _emSize;

        Focusable = true;
        FocusVisualStyle = null; // The cursor and the focus ring below say it better than a dotted box.

        Loaded += (_, _) => StartRendering();
        Unloaded += (_, _) => StopRendering();
    }

    /// <summary>Raised when the user types. The surface forwards this to the session.</summary>
    public event EventHandler<ReadOnlyMemory<byte>>? Input;

    /// <summary>Raised when the drawable area changes size, in character cells.</summary>
    public event EventHandler<(int Columns, int Rows)>? GridResized;

    public double CellWidth { get; }

    public double CellHeight { get; }

    private double Baseline { get; }

    /// <summary>Points the view at a different screen — used when a session is replaced.</summary>
    public void Attach(TerminalScreen screen)
    {
        _screen = screen ?? throw new ArgumentNullException(nameof(screen));
        InvalidateVisual();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo info)
    {
        base.OnRenderSizeChanged(info);

        var columns = Math.Max(1, (int)(ActualWidth / CellWidth));
        var rows = Math.Max(1, (int)(ActualHeight / CellHeight));

        if (columns != _screen.Columns || rows != _screen.Rows)
        {
            GridResized?.Invoke(this, (columns, rows));
        }
    }

    protected override void OnRender(DrawingContext context)
    {
        // The whole area first: cells only paint their own background when it differs, so without
        // this the gap below the last full row would show whatever was behind the control.
        context.DrawRectangle(
            new SolidColorBrush(_palette.Background), null, new Rect(RenderSize));

        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        for (var row = 0; row < _screen.Rows; row++)
        {
            DrawRow(context, row, pixelsPerDip);
        }

        DrawCursor(context);
        _screen.ClearDirty();
    }

    private void DrawRow(DrawingContext context, int row, double pixelsPerDip)
    {
        var column = 0;

        while (column < _screen.Columns)
        {
            var start = column;
            var style = StyleOf(_screen[row, column]);

            // Extend the run while the style holds. One draw per run rather than per cell is the
            // whole performance story.
            while (column < _screen.Columns && StyleOf(_screen[row, column]).Equals(style))
            {
                column++;
            }

            DrawRun(context, row, start, column - start, style, pixelsPerDip);
        }
    }

    private void DrawRun(
        DrawingContext context, int row, int start, int length, RunStyle style, double pixelsPerDip)
    {
        var origin = new Point(start * CellWidth, row * CellHeight);

        // Inverse swaps at draw time rather than in the model: the model records what the program
        // said, and a screen that had already swapped could not tell inverse-on-default from an
        // explicit pair of colours.
        var foreground = style.Inverse ? style.Background : style.Foreground;
        var background = style.Inverse ? style.Foreground : style.Background;

        if (background != _palette.Background)
        {
            context.DrawRectangle(
                new SolidColorBrush(background), null,
                new Rect(origin.X, origin.Y, length * CellWidth, CellHeight));
        }

        if (!HasInk(row, start, length))
        {
            return; // All spaces: the background above is the entire content.
        }

        EnsureRunCapacity(length);

        for (var i = 0; i < length; i++)
        {
            var character = _screen[row, start + i].Character;

            // A character with no glyph in this face falls back to a space rather than throwing.
            // CharacterToGlyphMap raises on a miss, and a missing glyph is an ordinary consequence
            // of a program printing something the font does not cover — not an error condition.
            _glyphs[i] = _typeface.CharacterToGlyphMap.TryGetValue(character, out var glyph)
                ? glyph
                : _typeface.CharacterToGlyphMap[' '];

            // The advance is the CELL width, not the glyph's own. That is what makes the grid a
            // grid: a proportional advance here would let a run drift out of alignment with the
            // rows above and below it.
            _advances[i] = CellWidth;
        }

        var run = new GlyphRun(
            _typeface,
            bidiLevel: 0,
            isSideways: false,
            renderingEmSize: _emSize,
            pixelsPerDip: (float)pixelsPerDip,
            glyphIndices: _glyphs[..length],
            baselineOrigin: new Point(origin.X, origin.Y + Baseline),
            advanceWidths: _advances[..length],
            glyphOffsets: null,
            characters: null,
            deviceFontName: null,
            clusterMap: null,
            caretStops: null,
            language: null);

        context.DrawGlyphRun(new SolidColorBrush(foreground), run);

        if (style.Underline)
        {
            var y = origin.Y + Baseline + 1.5;
            context.DrawLine(
                new Pen(new SolidColorBrush(foreground), 1),
                new Point(origin.X, y),
                new Point(origin.X + (length * CellWidth), y));
        }
    }

    private void DrawCursor(DrawingContext context)
    {
        if (!IsKeyboardFocused)
        {
            return; // An unfocused terminal showing a live cursor is claiming input it will not get.
        }

        // Clamp the DRAWN column/row so a pending-wrap cursor (column == Columns, held after writing
        // the last column) shows on the last cell rather than in the right margin, and a cursor left
        // outside the grid still draws somewhere visible rather than far off-screen.
        var column = Math.Clamp(_screen.CursorColumn, 0, Math.Max(0, _screen.Columns - 1));
        var row = Math.Clamp(_screen.CursorRow, 0, Math.Max(0, _screen.Rows - 1));

        var rect = new Rect(column * CellWidth, row * CellHeight, CellWidth, CellHeight);

        context.DrawRectangle(new SolidColorBrush(_palette.Cursor), null, rect);

        // Redrawn over the block so the character under the cursor stays readable. Read through
        // CellUnderCursor, NOT the raw indexer: at the pending-wrap position (or any off-grid cursor)
        // there is no cell, and indexing it would throw IndexOutOfRangeException on the WPF UI thread
        // during OnRender - unhandled, terminating the whole application (DC-062). No cell => nothing
        // to redraw, which is correct: the wrap position holds no character.
        if (_screen.CellUnderCursor() is { } cell && cell.Character != ' ')
        {
            var text = new FormattedText(
                cell.Character.ToString(),
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Cascadia Mono, Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                _emSize,
                new SolidColorBrush(_palette.Background),
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            context.DrawText(text, new Point(rect.X, rect.Y));
        }
    }

    private bool HasInk(int row, int start, int length)
    {
        for (var i = 0; i < length; i++)
        {
            if (_screen[row, start + i].Character != ' ')
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureRunCapacity(int length)
    {
        if (_glyphs.Length >= length)
        {
            return;
        }

        // Grown, never per-run allocated: OnRender runs at frame rate and a fresh array per run
        // would make the garbage collector part of the draw budget.
        _glyphs = new ushort[length];
        _advances = new double[length];
    }

    private RunStyle StyleOf(TerminalCell cell) => new(
        _palette.Resolve(cell.Foreground, isBackground: false),
        _palette.Resolve(cell.Background, isBackground: true),
        (cell.Attributes & CellAttributes.Underline) != 0,
        (cell.Attributes & CellAttributes.Inverse) != 0);

    private void StartRendering()
    {
        if (_rendering)
        {
            return;
        }

        _rendering = true;
        CompositionTarget.Rendering += OnFrame;
    }

    private void StopRendering()
    {
        if (!_rendering)
        {
            return;
        }

        _rendering = false;
        CompositionTarget.Rendering -= OnFrame;
    }

    /// <summary>Presents at frame rate, and only when something changed.</summary>
    /// <remarks>
    /// This is the coalescing policy in one line. A producer emitting a megabyte a second updates
    /// the screen thousands of times between frames, and the user can only ever see the last of
    /// them; redrawing per write would spend the entire budget rendering states nobody observes.
    /// The dirty check is the other half — without it a motionless terminal would repaint sixty
    /// times a second forever.
    /// </remarks>
    private void OnFrame(object? sender, EventArgs e)
    {
        if (_screen.IsDirty)
        {
            InvalidateVisual();
        }
    }

    protected override void OnTextInput(TextCompositionEventArgs e)
    {
        // Text rather than key codes, so composed input, dead keys and IME sequences work. Mapping
        // characters from key codes would break every keyboard layout that is not US.
        var bytes = TerminalInput.ForText(e.Text);
        if (!bytes.IsEmpty)
        {
            Input?.Invoke(this, bytes);
            e.Handled = true;
        }

        base.OnTextInput(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var bytes = TerminalInput.ForKey(e.Key, Keyboard.Modifiers);
        if (!bytes.IsEmpty)
        {
            Input?.Invoke(this, bytes);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        Focus(); // Clicking a terminal is how everyone expects to start typing into it.
        base.OnMouseDown(e);
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        InvalidateVisual(); // The cursor only draws when focused, so focus changes are visual.
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        InvalidateVisual();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new TerminalViewAutomationPeer(this);

    private static GlyphTypeface LoadMonospace()
    {
        // DESIGN.md's mono stack, walked in order. A GlyphRun needs a GlyphTypeface — the concrete
        // face — so the fallback chain has to be resolved here rather than left to WPF's own text
        // stack, which only applies to the higher-level text APIs this path deliberately avoids.
        foreach (var family in (string[])["Cascadia Mono", "Consolas", "Courier New"])
        {
            var typeface = new Typeface(
                new FontFamily(family), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            if (typeface.TryGetGlyphTypeface(out var glyphTypeface)
                && glyphTypeface.CharacterToGlyphMap.ContainsKey('M'))
            {
                return glyphTypeface;
            }
        }

        throw new InvalidOperationException(
            "no monospace font from the design language's stack could be loaded");
    }

    /// <summary>Foreground, background and the attributes that change how a run is drawn.</summary>
    /// <remarks>
    /// Bold is absent on purpose: this face has one weight, and a bold run differs only in colour —
    /// which is already captured by the palette's bright variants. Including it would split runs
    /// that draw identically, costing draws to express nothing.
    /// </remarks>
    private readonly record struct RunStyle(Color Foreground, Color Background, bool Underline, bool Inverse);

    /// <summary>
    /// Names the control for assistive technology.
    /// </summary>
    /// <remarks>
    /// Under <see href="../../docs/adr/0014-accessibility-posture.md">ADR-0014</see> this carries no
    /// conformance obligation. It is here because a bare <see cref="FrameworkElement"/> surfaces as
    /// an unnamed pane, and a terminal is the surface a keyboard-first tool most needs to be able to
    /// find. What it does NOT do is expose the screen's text, which would need a live-region
    /// contract this build does not have.
    /// </remarks>
    private sealed class TerminalViewAutomationPeer(TerminalView owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(TerminalView);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

        protected override bool IsKeyboardFocusableCore() => true;
    }
}
