using System.IO;
using System.Text.RegularExpressions;

namespace AiDe.App.Tests.Records;

/// <summary>
/// Ruling 172(4), the control against the test shape that hid WL-1a's read-back gap: T17's tests bound the host BEFORE
/// building the document, while the shell builds the document while the host still opens, so they passed and the product
/// never read back. A test-source census over <c>tests/</c>.
/// </summary>
/// <remarks>
/// The pattern, by name, <b>bind-first read-back</b>: in a file that observes the restart read-back (it names
/// <c>LastReadBack</c> or <c>Runs.ReadBack</c>), a call to <c>.BindHost(</c> with a <c>new SessionDocumentSurface(</c>
/// within the next <see cref="Window"/> lines. Each such site fails on its own, unless <see cref="NotAShellReopen"/> names
/// its member with the number of sites it holds and why. A reopen binds in the shell's order instead:
/// <c>BindHostWhenOpen</c> with an opener that completes after the document is built.
/// </remarks>
public sealed partial class ReadBackTestsCoverTheShellsOrderTests
{
    /// <summary>How many lines after a <c>BindHost</c> call a document construction counts as "bound first".</summary>
    private const int Window = 3;

    private const string EmptyStore =
        "a first open over an empty store: no attempt is recorded yet, so the read-back has nothing to read and the order cannot hide a restore gap";

    /// <summary>
    /// The bind-first sites that are not the shell's reopen: file, member, how many sites the member holds, and why.
    /// The product never binds first; its one bind is <c>BindHostWhenOpen</c> (WorkbenchShell.cs:3596). An entry whose
    /// member holds a different number of sites fails, so a new bind-first reopen cannot hide behind it; an entry that
    /// matches nothing fails too.
    /// </summary>
    internal static readonly (string File, string Method, int Sites, string Reason)[] NotAShellReopen =
    [
        ("TheAttemptHostBindingTests.cs", "ASentTurn_IsAnAttemptOfItsRevision_AndARestartReadsTheAttemptAndItsEventsBack", 1, EmptyStore),
        ("TheAttemptHostBindingTests.cs", "APaneStop_ReachesTheHostsStopAsync_AndTheAttemptEndsByItsStopRule", 1, EmptyStore),
        ("TheAttemptHostBindingTests.cs", "AQueuedTurn_IsTheHostsHeldSuccessor_AndEachAttemptsLiveLinesReachTheThread", 1, EmptyStore),
        ("TheRowsAttemptBindingSurvivesRestartAndSequenceTests.cs", "AfterARestart_TheStoreHoldsTheAttempt_AndTheThreadRestoresItsRow_WithTheLiveBindingLine", 1, EmptyStore),
        ("TheRowsAttemptBindingSurvivesRestartAndSequenceTests.cs", "ASendEditSendSequence_LeavesTurnAsRowOnItsRevisionAndRequest_AndTurnBOnItsOwn", 1, EmptyStore),
        ("TheRowsAttemptBindingSurvivesRestartAndSequenceTests.cs", "ASentTurnsRow_ShowsItsBinding_WhileTheRunIsStillHeld_ThroughTheSurface", 1, EmptyStore),
        ("TheReadBackRunsWhenTheHostBindsTests.cs", "RecordTwoAttemptsAsync", 1, EmptyStore),
        ("TheReadBackRunsWhenTheHostBindsTests.cs", "TheReadBackRunsOncePerBind_NotOncePerDocument_AndARebindReadsBackOnceMore", 1,
            "a reopen that binds before construction on purpose: Ruling 172(2) requires the read-back when the host is bound before "
            + "construction too, and this test proves that case and once per bind; the same file covers the shell's order"),
    ];

    [GeneratedRegex(@"\b(LastReadBack|Runs\.ReadBack)\b")]
    private static partial Regex ObservesTheReadBack();

    [GeneratedRegex(@"\.BindHost\(")]
    private static partial Regex BindsTheHost();

    [GeneratedRegex(@"new SessionDocumentSurface\(")]
    private static partial Regex BuildsTheDocument();

    [GeneratedRegex(@"^\s*(?:public|private|internal|protected)\b[^=;]*?\b(\w+)\(")]
    private static partial Regex DeclaresAMember();

    /// <summary>
    /// The bind-first sites of <paramref name="lines"/>: each 1-based line with the member it is in ("" before any), or
    /// none when the file does not observe the read-back.
    /// </summary>
    internal static IReadOnlyList<(int Line, string Method)> BindFirstSites(string[] lines)
    {
        if (!ObservesTheReadBack().IsMatch(string.Join('\n', lines)))
        {
            return [];
        }

        List<(int Line, string Method)> found = [];
        var method = "";
        for (var i = 0; i < lines.Length; i++)
        {
            if (DeclaresAMember().Match(lines[i]) is { Success: true } member)
            {
                method = member.Groups[1].Value;
            }

            if (BindsTheHost().IsMatch(lines[i]) && lines.Skip(i).Take(Window + 1).Any(l => BuildsTheDocument().IsMatch(l)))
            {
                found.Add((i + 1, method));
            }
        }

        return found;
    }

    /// <summary>
    /// The census's failures over <paramref name="files"/>: every member's sites that no entry of <paramref name="named"/>
    /// names with their exact count, and every entry that matches no site.
    /// </summary>
    internal static IReadOnlyList<string> Offenders(
        IEnumerable<(string File, string[] Lines)> files, IReadOnlyList<(string File, string Method, int Sites, string Reason)> named)
    {
        var sites = files.SelectMany(f => BindFirstSites(f.Lines).Select(s => (f.File, s.Method, s.Line))).ToList();
        List<string> offenders = [];
        foreach (var member in sites.GroupBy(s => (Name: Path.GetFileName(s.File), s.Method)))
        {
            var entry = named.FirstOrDefault(e => e.File == member.Key.Name && e.Method == member.Key.Method);
            if (entry.Sites != member.Count())
            {
                var why = entry.File is null ? "" : $"; named for {entry.Sites} site(s), holds {member.Count()}";
                offenders.Add($"{member.First().File}:{string.Join(",", member.Select(s => s.Line))} ({member.Key.Method}{why})");
            }
        }

        offenders.AddRange(named
            .Where(e => !sites.Any(s => Path.GetFileName(s.File) == e.File && s.Method == e.Method))
            .Select(e => $"{e.File} {e.Method}: named, but it holds no bind-first site; remove the entry"));
        return offenders;
    }

    [Fact]
    public void ThePattern_FlagsEachBindFirstSite_UnlessItsMemberIsNamedWithItsCount_AndFlagsANameThatMatchesNothing()
    {
        string[] bindFirst =
        [
            "    public void Reopens()",
            "model.Runs.BindHost(host, Scope);",
            "var surface = new SessionDocumentSurface(model, null, announcer);",
            "await surface.LastReadBack;",
        ];
        Assert.Equal([(2, "Reopens")], BindFirstSites(bindFirst));
        Assert.Equal([(2, "Reopens")], BindFirstSites([.. bindFirst, "other.Runs.BindHostWhenOpen(open, Scope);"]));
        Assert.Empty(BindFirstSites(["model.Runs.BindHostWhenOpen(() => opened.Task, Scope);", "var surface = new SessionDocumentSurface(model, null, announcer);", "await surface.LastReadBack;"]));
        Assert.Empty(BindFirstSites(["model.Runs.BindHost(host, Scope);", "var surface = new SessionDocumentSurface(model, null, announcer);"]));

        (string File, string[] Lines)[] file = [("tests/X.cs", bindFirst)];
        Assert.Single(Offenders(file, []));
        Assert.Empty(Offenders(file, [("X.cs", "Reopens", 1, "why")]));
        Assert.Single(Offenders(file, [("X.cs", "Reopens", 2, "why")]));
        Assert.Single(Offenders([], [("X.cs", "Reopens", 1, "why")]));
    }

    [Fact]
    public void NoReadBackTest_BindsTheHostBeforeTheDocument_UnlessTheSiteIsNamedAsNotTheShellsReopen()
    {
        var tests = Path.Combine(RepoRoot(), "tests");
        var files = Directory.EnumerateFiles(tests, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && Path.GetFileName(f) != $"{nameof(ReadBackTestsCoverTheShellsOrderTests)}.cs")
            .ToList();

        var readBackTests = files.Count(f => ObservesTheReadBack().IsMatch(File.ReadAllText(f)));
        Assert.True(readBackTests >= 3, $"the census found {readBackTests} read-back test files; it is not reading tests/");

        var offenders = Offenders(
            files.Select(f => (Path.GetRelativePath(RepoRoot(), f).Replace('\\', '/'), File.ReadAllLines(f))), NotAShellReopen);
        Assert.True(offenders.Count == 0,
            "bind-first read-back: reopen in the shell's order (BindHostWhenOpen with an opener that completes after the document "
            + "is built; Ruling 172), or name a site that is not a reopen in NotAShellReopen with its reason: " + string.Join("; ", offenders));
    }

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
