using System.Text;

namespace NotebookEditorE1Spike;

/// <summary>
/// Fixtures as C# literals, never as checked-in text files: git's line-ending conversion would
/// otherwise decide the bytes a whitespace round-trip is measured against.
/// </summary>
internal static class Fixtures
{
    /// <summary>
    /// Six fences — json, csharp, python (outside the bundle's languages), untagged, json nested in a
    /// list item, csharp nested in a blockquote — plus repeated blank lines, trailing spaces, tabs, a
    /// whitespace-only line and no final newline: the shapes UI-DOCUMENT-PROJECTION names.
    /// </summary>
    public const string NotebookLf =
        "# Notebook E-1 fixture\n" +
        "\n" +
        "Intro paragraph with trailing spaces   \n" +
        "\n" +
        "\n" +
        "\n" +
        "## Code\n" +
        "\n" +
        "```json\n" +
        "{\n" +
        "  \"a\": 1,\n" +
        "\n" +
        "\n" +
        "  \"b\": [true, false]\n" +
        "}\n" +
        "```\n" +
        "\n" +
        "```csharp\n" +
        "public class C\n" +
        "{\n" +
        "\tint x = 1;\t// tab inside\n" +
        "\n" +
        "}\n" +
        "```\n" +
        "\n" +
        "```python\n" +
        "def f():\n" +
        "    return 1\n" +
        "\n" +
        "\n" +
        "```\n" +
        "\n" +
        "```\n" +
        "plain fence, no language\n" +
        "```\n" +
        "\n" +
        "- list item\n" +
        "\n" +
        "  ```json\n" +
        "  {\"nested\": \"in list\"}\n" +
        "  ```\n" +
        "\n" +
        "> quoted\n" +
        "> ```csharp\n" +
        "> var q = 1;\n" +
        "> ```\n" +
        "\n" +
        "   \n" +
        "### Last heading\n" +
        "final line without newline";

    public static readonly string[] ExpectedFenceInfo = ["json", "csharp", "python", "", "json", "csharp"];

    public static string NotebookCrlf => NotebookLf.Replace("\n", "\r\n", StringComparison.Ordinal);

    /// <summary>One CRLF among LFs: no single separator can reproduce it, so it must be disclosed.</summary>
    public static string NotebookMixed => NotebookLf.Replace("## Code\n", "## Code\r\n", StringComparison.Ordinal);

    /// <summary>
    /// What a Windows app puts on the clipboard: CRLF, a ZWJ emoji sequence, a combining accent
    /// (e + U+0301), CJK, tabs and trailing spaces.
    /// </summary>
    public const string ClipboardText =
        "Pasted from Windows\r\n" +
        "\r\n" +
        "```json\r\n" +
        "{ \"k\": \"日本語 \U0001F469‍\U0001F4BB é\" }\r\n" +
        "```\r\n" +
        "\tindented\ttab   \r\n" +
        "trailing";

    public const string ClipboardHtml = "<b>RICH</b> <i>html</i> that must not be what is pasted";

    /// <summary>A Markdown notebook of at least 100,000 characters: headings, prose and fences.</summary>
    public static string Large(int minimum = 100_000)
    {
        var sb = new StringBuilder("# Large notebook\n\n");
        for (var i = 0; sb.Length < minimum; i++)
        {
            sb.Append("## Section ").Append(i).Append("\n\n");
            sb.Append("Paragraph ").Append(i)
              .Append(" carries enough prose to wrap across the editor width, with **bold**, *emphasis* and `code` spans in it so the inline parser has work to do on every line.\n\n");
            sb.Append("```json\n{\"i\": ").Append(i).Append(", \"v\": [1, 2, 3]}\n```\n\n");
            sb.Append("```csharp\nvar x").Append(i).Append(" = ").Append(i).Append(";\n```\n\n");
        }

        return sb.ToString();
    }
}
