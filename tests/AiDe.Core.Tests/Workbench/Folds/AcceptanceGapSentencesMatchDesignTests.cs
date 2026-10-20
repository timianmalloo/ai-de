using AiDe.Core.Store.Records;
using AiDe.Core.Workbench.Records.Folds;

namespace AiDe.Core.Tests.Workbench.Folds;

/// <summary>
/// DC-NEW-wl-t26-copy, T35: every blocking-gap sentence the Core fold writes is pinned against the clause list of
/// DESIGN.md's CHK6 row. The expected text is read from DESIGN.md (Owner oracle rule); it is never copied from
/// <c>AcceptanceFold.cs</c>. The fold's own sentence is only the value under test.
/// </summary>
public sealed class AcceptanceGapSentencesMatchDesignTests
{
    private const string ObligationText = "Tests pass";
    private const int Revision = 3;
    private const string Baseline = "intent:n1@3";
    private const string ClauseListMarker = "as the Core fold writes it (";

    private static readonly ObligationRow O1 = new("o1", "j1", 1, ObligationText, true);
    private static readonly AttemptRow Attempt = new("dk-1", "w", 1, "n1", Revision, "s", 1, "sha-a");

    private static EvidenceLinkRow Link(string baseline = Baseline) =>
        new("l-o1", "obligation", "o1", 1, "attempt://dk-1", "claude", baseline, AcceptanceFold.ObservedStatus);

    private static AcceptanceInput Eligible() => new(
        "j1", "n1", Revision,
        [O1],
        new Dictionary<string, IReadOnlyList<ObligationRow>> { ["o1"] = [O1] },
        new Dictionary<string, IReadOnlyList<EvidenceLinkRow>> { ["o1"] = [Link()] },
        new Dictionary<string, IReadOnlyList<DispositionRow>>(),
        [new DispositionRow("intent", "j1", "intent-accepted", "shell:1", Baseline, NotebookId: "n1", RevisionNo: Revision)],
        new Dictionary<string, AttemptRow> { ["dk-1"] = Attempt },
        "sha-a");

    /// <summary>One input per gap code; the test fails if a code has no scenario here.</summary>
    private static readonly Dictionary<string, AcceptanceInput> Scenarios = new()
    {
        [AcceptanceGapCodes.ChecklistEmpty] = Eligible() with
        {
            CurrentObligations = [],
            ObligationHistory = new Dictionary<string, IReadOnlyList<ObligationRow>>(),
        },
        [AcceptanceGapCodes.IntentMissing] = Eligible() with { IntentLatestRevisionNo = null },
        [AcceptanceGapCodes.IntentNotAccepted] = Eligible() with { IntentDispositions = [] },
        [AcceptanceGapCodes.EvidenceMissing] = Eligible() with
        {
            Links = new Dictionary<string, IReadOnlyList<EvidenceLinkRow>>(),
        },
        [AcceptanceGapCodes.EvidenceStale] = Eligible() with
        {
            Links = new Dictionary<string, IReadOnlyList<EvidenceLinkRow>> { ["o1"] = [Link("intent:n1@2")] },
        },
        [AcceptanceGapCodes.EvidenceNotRecorded] = Eligible() with { LiveProjectionSha = null },
        [AcceptanceGapCodes.NeedsDecision] = Eligible() with
        {
            ObligationDispositions = new Dictionary<string, IReadOnlyList<DispositionRow>>
            {
                ["o1"] = [new DispositionRow("obligation", "o1", "needs-decision", "shell:1", Baseline) { IngressSeq = 1 }],
            },
        },
    };

    private static string[] AllGapCodes() =>
        typeof(AcceptanceGapCodes).GetFields()
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();

    [Fact]
    public void EveryGapCodeHasAScenarioHere()
    {
        Assert.Empty(AllGapCodes().Except(Scenarios.Keys));
        Assert.Empty(Scenarios.Keys.Except(AllGapCodes()));
    }

    [Fact]
    public void EveryGapSentenceTheFoldWrites_IsAClauseOfDesignChk6_AndEveryClauseIsASentence()
    {
        var clauses = Chk6Clauses().Select(Fill).ToList();
        var problems = new List<string>();

        foreach (var (code, input) in Scenarios)
        {
            var gap = Assert.Single(AcceptanceFold.Evaluate(input).Gaps, g => g.Code == code);
            if (!clauses.Contains(gap.Sentence, StringComparer.Ordinal))
            {
                problems.Add($"gap code '{code}' has no clause in DESIGN.md CHK6 (the fold writes: {gap.Sentence})");
            }
        }

        var sentences = Scenarios
            .Select(s => AcceptanceFold.Evaluate(s.Value).Gaps.Single(g => g.Code == s.Key).Sentence)
            .ToList();
        foreach (var clause in clauses.Where(c => !sentences.Contains(c, StringComparer.Ordinal)))
        {
            problems.Add($"DESIGN.md CHK6 clause matches no gap code: {clause}");
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>The CHK6 row's parenthesised clause list, split on the row's own " · " separator.</summary>
    private static IReadOnlyList<string> Chk6Clauses()
    {
        var row = File.ReadAllLines(Path.Combine(RepoRoot(), "DESIGN.md"))
            .Single(l => l.StartsWith("| CHK6 |", StringComparison.Ordinal));
        var start = row.IndexOf(ClauseListMarker, StringComparison.Ordinal) + ClauseListMarker.Length;
        var end = row.LastIndexOf(')');

        Assert.True(start >= ClauseListMarker.Length && end > start, "DESIGN.md CHK6 row has no clause list after '" + ClauseListMarker + "'");
        return row[start..end].Split(" · ", StringSplitOptions.TrimEntries);
    }

    private static string Fill(string clause) =>
        clause.Replace("{obligation}", ObligationText, StringComparison.Ordinal)
            .Replace("{n}", Revision.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AiDe.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("could not locate the repository root from the test output directory");
    }
}
