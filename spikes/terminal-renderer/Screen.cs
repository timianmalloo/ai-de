using System.Text;
using System.Windows.Media;

namespace TerminalRendererSpike;

/// <summary>A screenful of representative terminal content, and a stream that produced it.</summary>
/// <remarks>
/// Content is deliberately *build-log shaped* rather than random characters: mixed-length lines,
/// paths, timestamps, and a minority of coloured lines. Random uniform text would make every line
/// the same cost and the measurement smoother than reality — the whole point is that a real terminal
/// draws ragged, partly-styled lines.
/// </remarks>
internal sealed class Screen
{
    private readonly char[][] _lines;
    private readonly Brush[] _brushes;

    private Screen(char[][] lines, Brush[] brushes)
    {
        _lines = lines;
        _brushes = brushes;
        Rows = lines.Length;
        Columns = lines.Length == 0 ? 0 : lines[0].Length;
    }

    internal int Rows { get; }

    internal int Columns { get; }

    internal char[] Line(int row) => _lines[row];

    internal Brush Brush(int row) => _brushes[row];

    internal static Screen Sample(int columns, int rows)
    {
        // Fixed seed: the benchmark must compare draw paths, not two different screens.
        var random = new Random(20260826);
        var fragments = new[]
        {
            "Determining projects to restore...",
            "  AiDe.Core -> C:\\Projects\\ai-de\\src\\AiDe.Core\\bin\\Debug\\net10.0\\AiDe.Core.dll",
            "[19:41:51 INF] extraction scope=AiDe.Core generation=42 assertions=10314",
            "warning CS8618: Non-nullable field '_store' must contain a non-null value",
            "  Passed!  - Failed: 0, Passed: 134, Skipped: 0, Duration: 2 s",
            "$ git status --short",
            "        modified:   src/AiDe.Core/Store/StoreReader.cs",
        };

        var lines = new char[rows][];
        var brushes = new Brush[rows];

        for (var row = 0; row < rows; row++)
        {
            var text = fragments[random.Next(fragments.Length)];
            var buffer = new char[columns];
            for (var i = 0; i < columns; i++)
            {
                buffer[i] = i < text.Length ? text[i] : ' ';
            }

            lines[row] = buffer;

            // A minority of coloured lines, as a build log actually looks. Frozen because an unfrozen
            // brush costs dispatcher checks on every draw and would measure that instead.
            var brush = random.Next(6) switch
            {
                0 => new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71)),
                1 => new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)),
                2 => new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80)),
                _ => new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
            };

            brush.Freeze();
            brushes[row] = brush;
        }

        return new Screen(lines, brushes);
    }

    /// <summary>Roughly <paramref name="bytes"/> of VT output with the escape density of a real build.</summary>
    internal static string VtStream(int bytes)
    {
        var random = new Random(20260826);
        var builder = new StringBuilder(bytes + 256);
        var colours = new[] { "\u001b[31m", "\u001b[32m", "\u001b[33m", "\u001b[0m", "\u001b[1m" };

        while (builder.Length < bytes)
        {
            if (random.Next(4) == 0)
            {
                builder.Append(colours[random.Next(colours.Length)]);
            }

            builder.Append("[19:41:51 INF] scope=AiDe.Core generation=");
            builder.Append(random.Next(1000));
            builder.Append(" assertions=");
            builder.Append(random.Next(100000));
            builder.Append('\n');
        }

        return builder.ToString();
    }
}

/// <summary>
/// A minimal VT scanner — enough to establish whether *parsing* is anywhere near the constraint.
/// </summary>
/// <remarks>
/// This is not a terminal emulator and does not pretend to be: it classifies printable text, escape
/// sequences and newlines and counts them. That is the right scope for the question S3 asks, which
/// is whether the parse side is plausibly the bottleneck, and it is stated plainly so that nobody
/// later cites this number as "our VT parser is fast".
/// </remarks>
internal sealed class VtScanner
{
    internal long Printable { get; private set; }

    internal long Escapes { get; private set; }

    internal long Newlines { get; private set; }

    internal void Scan(string input)
    {
        Printable = 0;
        Escapes = 0;
        Newlines = 0;

        var i = 0;
        while (i < input.Length)
        {
            var c = input[i];
            if (c == '\u001b')
            {
                i++;
                if (i < input.Length && input[i] == '[')
                {
                    i++;
                    while (i < input.Length && !char.IsLetter(input[i]))
                    {
                        i++;
                    }
                }

                if (i < input.Length)
                {
                    i++;
                }

                Escapes++;
            }
            else if (c == '\n')
            {
                Newlines++;
                i++;
            }
            else
            {
                Printable++;
                i++;
            }
        }
    }
}
