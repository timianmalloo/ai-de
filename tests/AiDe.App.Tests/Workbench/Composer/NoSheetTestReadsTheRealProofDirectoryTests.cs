using System.IO;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>T30 n5 (C): no test builds the New session sheet over an adapter root with the real compile-mode gate.</b> The
/// sheet's default <c>CompileModeGateInput</c> is <c>CompileModeGate.Evaluate</c>, which reads
/// <c>%USERPROFILE%/.aide/proof/compile-pin-spike.json</c>. A test that gives the sheet an adapter root and no injected
/// gate therefore depends on this machine's proof directory: it reads <i>agentic-advisory</i> on the Owner's machine and
/// <i>mechanical-only</i> on a clean one. This guard reads every test source and refuses each such site.
/// </summary>
/// <remarks>
/// <b>What it reads.</b> Each <c>new NewSessionSheetViewModel(…)</c>, each target-typed <c>new(…)</c> assigned to or
/// returned as one, and each <c>new NewSessionFlow(…)</c>, with a named <c>adapterInstallRoot:</c> that is not
/// <c>null</c>: the site must set <c>CompileModeGateInput</c> in its object initializer (a sheet) or its arguments (a
/// flow's <c>showSheet</c>). A <c>Reload(registry, root)</c> with a root that is not <c>null</c> needs every sheet built
/// in that file to carry the injection. <b>Limit:</b> an adapter root passed by position is not seen.
/// </remarks>
public sealed class NoSheetTestReadsTheRealProofDirectoryTests(ITestOutputHelper output)
{
    private static readonly Regex SheetConstruction = new(
        @"new\s+NewSessionSheetViewModel\s*\(|NewSessionSheetViewModel\??\s+\w+\s*(?:\([^)]*\))?\s*(?:=>|=)\s*new\s*\(",
        RegexOptions.Compiled);

    private static readonly Regex FlowConstruction = new(@"new\s+NewSessionFlow\s*\(", RegexOptions.Compiled);

    private static readonly Regex RootArgument = new(@"adapterInstallRoot:\s*(?!null\b)\S", RegexOptions.Compiled);

    private static readonly Regex Reload = new(@"\.Reload\s*\(", RegexOptions.Compiled);

    [Fact]
    public void EverySheetBuiltOverAnAdapterRoot_InjectsTheCompileModeGate()
    {
        var root = RepoRoot();
        var violations = new List<string>();
        var sites = 0;
        foreach (var project in new[] { "tests/AiDe.App.Tests", "tests/AiDe.Core.Tests" })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, project), "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (relative.Contains("/bin/", StringComparison.Ordinal) || relative.Contains("/obj/", StringComparison.Ordinal) || file.EndsWith(nameof(NoSheetTestReadsTheRealProofDirectoryTests) + ".cs", StringComparison.Ordinal))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                if (!text.Contains("NewSessionSheetViewModel", StringComparison.Ordinal) && !text.Contains("NewSessionFlow", StringComparison.Ordinal))
                {
                    continue;
                }

                var sheets = new List<(int Line, bool Root, bool Injected)>();
                foreach (Match match in SheetConstruction.Matches(text))
                {
                    var open = match.Index + match.Length - 1;
                    var close = Balanced(text, open, '(', ')');
                    var arguments = text[open..close];
                    var rest = close + 1;
                    while (rest < text.Length && char.IsWhiteSpace(text[rest]))
                    {
                        rest++;
                    }

                    var initializer = rest < text.Length && text[rest] == '{' ? text[rest..Balanced(text, rest, '{', '}')] : string.Empty;
                    sheets.Add((LineOf(text, match.Index), RootArgument.IsMatch(arguments), initializer.Contains("CompileModeGateInput", StringComparison.Ordinal)));
                }

                foreach (var sheet in sheets.Where(s => s.Root))
                {
                    sites++;
                    if (!sheet.Injected)
                    {
                        violations.Add($"{relative}:{sheet.Line}: a sheet over an adapter root with the real gate");
                    }
                }

                foreach (Match match in FlowConstruction.Matches(text))
                {
                    var open = match.Index + match.Length - 1;
                    var arguments = text[open..Balanced(text, open, '(', ')')];
                    if (RootArgument.IsMatch(arguments))
                    {
                        sites++;
                        if (!arguments.Contains("CompileModeGateInput", StringComparison.Ordinal))
                        {
                            violations.Add($"{relative}:{LineOf(text, match.Index)}: a flow over an adapter root whose sheet keeps the real gate");
                        }
                    }
                }

                foreach (Match match in Reload.Matches(text))
                {
                    var open = match.Index + match.Length - 1;
                    var arguments = text[(open + 1)..Balanced(text, open, '(', ')')];
                    var last = arguments[(arguments.LastIndexOf(',') + 1)..].Trim();
                    if (sheets.Count == 0 || last == "null")
                    {
                        continue;
                    }

                    sites++;
                    if (sheets.Any(s => !s.Injected))
                    {
                        violations.Add($"{relative}:{LineOf(text, match.Index)}: a sheet reloaded over an adapter root with the real gate");
                    }
                }
            }
        }

        output.WriteLine($"{sites} sites over an adapter root; {violations.Count} without an injected gate");
        foreach (var violation in violations)
        {
            output.WriteLine(violation);
        }

        Assert.True(sites > 0, "the guard found no sheet built over an adapter root; its patterns no longer match the tests");
        Assert.Empty(violations);
    }

    /// <summary>The index of the bracket that closes the one at <paramref name="open"/>.</summary>
    private static int Balanced(string text, int open, char opening, char closing)
    {
        var depth = 0;
        for (var i = open; i < text.Length; i++)
        {
            if (text[i] == opening)
            {
                depth++;
            }
            else if (text[i] == closing && --depth == 0)
            {
                return i;
            }
        }

        throw new InvalidOperationException($"unbalanced '{opening}' at {open}");
    }

    private static int LineOf(string text, int index) => text.AsSpan(0, index).Count('\n') + 1;

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
