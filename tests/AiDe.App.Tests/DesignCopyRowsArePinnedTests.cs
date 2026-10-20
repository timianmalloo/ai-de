using System.Reflection;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Checklist;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Workbench.Records.Folds;

namespace AiDe.App.Tests;

/// <summary>
/// The class control for DC-NEW-wl-t26-copy: a copy oracle that reads the same constant the code renders can never go
/// red on a copy change. This test reads <b>DESIGN.md</b> (the Owner's words) and the <b>code</b> (what is rendered), and
/// fails when the two disagree, so a copy change cannot pass unseen.
/// </summary>
/// <remarks>
/// <para>Every DESIGN.md copy row that has an id (<c>| CHK3 | … |</c>) is either <b>pinned</b> to the literal the code
/// renders, or named on <see cref="UnmatchedRows"/> with a one-line reason. Every <c>*Copy</c> constant or builder in the
/// App and Core assemblies is either named by a pin, or named on <see cref="UnmatchedMembers"/>. Both lists may only
/// shrink: an entry that is now matched, or whose row or member is gone, fails as stale.</para>
/// <para>How a row is compared. The first rendered literal of a pin must be a prefix of the row's cell; any further
/// literal must appear in it. A prefix, not equality, because a cell may carry a trailing annotation (a ruling number, a
/// plural form). A builder with arguments is rendered for a sample argument that is unique in the output, and that
/// sample is then replaced back by the row's own placeholder (<c>{n}</c>, <c>{7-char key}</c>), so the comparison is
/// the text around the placeholder.</para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class DesignCopyRowsArePinnedTests
{
    private static readonly Regex IdRow = new(@"^\| (?<id>[A-Z]+[0-9]+[a-z]?) \| (?<cell>.*) \|$", RegexOptions.Compiled);

    /// <summary>A row the Owner has ruled stale opens its cell with this mark, then the reason, then <c>)* </c>, then the original sentence.</summary>
    private const string StaleMark = "*(stale: ";

    private const string SampleKey = "abcdefghij";
    private const string SampleKeyCut = "abcdefg";
    private const int SampleRevision = 4242;

    /// <summary>Rows that are layout or behaviour descriptions, or whose sentence no code renders. Shrink-only.</summary>
    private static readonly IReadOnlyDictionary<string, string> UnmatchedRows = new Dictionary<string, string>
    {
        ["SC7"] = "not copy: a layout and behaviour description (outcome line, reply, N events collapsed)",
        ["SC8"] = "not copy: a keyboard and focus-order rule",
        ["SC9"] = "not copy: an announcement rule (what is never announced)",
    };

    /// <summary>Copy constants and builders no DESIGN.md id row names. Shrink-only.</summary>
    private static readonly IReadOnlyDictionary<string, string> UnmatchedMembers = new Dictionary<string, string>
    {
    };

    private sealed record Pin(string RowId, string[] Rendered, string[] Members);

    private static string N(string s) => s.Replace(SampleKeyCut, "{7-char key}", StringComparison.Ordinal)
        .Replace(SampleRevision.ToString(System.Globalization.CultureInfo.InvariantCulture), "{n}", StringComparison.Ordinal);

    private static TurnView StopRequestedTurn() =>
        new(1, "env-0001", "words", [], TurnState.StopRequested, null, null, [], "b", ThreadFixtures.T0);

    private static string KeptPromptsSingular()
    {
        var method = typeof(LayoutPersistence).GetMethod("KeptPromptsReport", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.True(method is not null, "LayoutPersistence.KeptPromptsReport is the builder DOCK4's singular is rendered by; it moved, so repoint this pin.");
        return (string)method!.Invoke(null, [1, 1])!;
    }

    private static TurnView Turn(int ordinal, TurnState state, OutcomeView? outcome = null) =>
        new(ordinal, "env-000" + ordinal, "words", [], state, outcome, null, [], "b", ThreadFixtures.T0);

    private static ThreadSnapshot Snapshot(params TurnView[] turns) => new(turns, 1, true);

    private static OutcomeView Outcome(Spend? spend = null) => new("lane", null, 0, spend, null, 0);

    private static string Core(string typeName, string field)
    {
        var type = typeof(TurnCopy).Assembly.GetType(typeName);
        Assert.True(type is not null, typeName + " moved, so repoint this pin.");
        var f = type!.GetField(field, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        Assert.True(f is not null, typeName + "." + field + " moved, so repoint this pin.");
        return (string)f!.GetRawConstantValue()!;
    }

    private static string[] OutcomeWords() =>
        Enum.GetValues<TurnState>().OrderBy(st => st != TurnState.Running)
            .Select(st => TurnCopy.OutcomeWord(st, null))
            .Append(N(TurnCopy.OutcomeWord(TurnState.Failed, SampleRevision)))
            .ToArray();

    private static string SessionSpendSample(string text) => text.Replace("12,400", "{sum}").Replace("40,000", "{cap}").Replace("(1 not", "({k} not");

    private static string QueuedSample(string text) => text.Replace("b2", "{queued}").Replace("b1", "{behind}");

    private static string Spent(Spend? spend) => spend is null ? TurnCopy.SpendText(null) : TurnCopy.SpendText(spend).Replace("424", "{n}");

    private static IReadOnlyList<Pin> Pins() =>
    [
        new("NB2", [CurrentnessFold.StaleCopy, NotebookModel.StaleCopy], ["CurrentnessFold.StaleCopy", "NotebookModel.StaleCopy"]),
        new("RUN2", [TurnCopy.ReasonSentence(StopRequestedTurn())], ["TurnCopy.ReasonSentence"]),
        new(
            "RUN4a",
            [string.Format(System.Globalization.CultureInfo.InvariantCulture, TurnItem.LastEventReceived, "#{seq} at {HH:mm:ss}"), string.Format(System.Globalization.CultureInfo.InvariantCulture, TurnItem.LastEventReceived, "not recorded")],
            ["TurnItem.LastEventReceived"]),
        new("CHK1", [ChecklistCopy.Empty], ["ChecklistCopy.Empty"]),
        new(
            "CHK2",
            [ChecklistCopy.EvidenceMissing, ChecklistCopy.NoDisposition, ChecklistCopy.Acknowledged, ChecklistCopy.Deferred, ChecklistCopy.NeedsDecision],
            ["ChecklistCopy.EvidenceMissing", "ChecklistCopy.NoDisposition", "ChecklistCopy.Acknowledged", "ChecklistCopy.Deferred", "ChecklistCopy.NeedsDecision"]),
        new("CHK3", [N(ChecklistCopy.Satisfied(SampleKey, SampleRevision))], ["ChecklistCopy.Satisfied"]),
        new("CHK4", [ChecklistCopy.Stale], ["ChecklistCopy.Stale"]),
        new("CHK5", [ChecklistCopy.NotRecorded], ["ChecklistCopy.NotRecorded"]),
        new("CHK6", [ChecklistCopy.Ready], ["ChecklistCopy.Ready"]),
        new("CHK7", [N(ChecklistCopy.Accepted(SampleRevision))], ["ChecklistCopy.Accepted", "ChecklistCopy.AcceptedNegation"]),
        new("CHK8", [ChecklistCopy.RefusedLead], ["ChecklistCopy.RefusedLead"]),
        new("CHK9", [ChecklistCopy.NotAWorkJourney], ["ChecklistCopy.NotAWorkJourney"]),
        new("CHK10", [ChecklistCopy.NotOpen("{journey}")], ["ChecklistCopy.NotOpen"]),
        new("CHK11", [ChecklistCopy.NoNotebook], ["ChecklistCopy.NoNotebook"]),
        new(
            "CHK12",
            [N(ChecklistCopy.AcceptIntent(SampleRevision)), N(ChecklistCopy.IntentAccepted(SampleRevision)), ChecklistCopy.AcceptDeliverable],
            ["ChecklistCopy.AcceptIntent", "ChecklistCopy.IntentAccepted", "ChecklistCopy.AcceptDeliverable"]),
        new("CHK13", [ChecklistCopy.AcceptedEarlier], ["ChecklistCopy.AcceptedEarlier"]),
        new("CHK14", [ChecklistCopy.Acknowledge], ["ChecklistCopy.Acknowledge"]),
        new("CHK15", [ChecklistCopy.AddObligation], ["ChecklistCopy.AddObligation"]),
        new("CHK16", [ChecklistCopy.Defer], ["ChecklistCopy.Defer"]),
        new("CHK17", [ChecklistCopy.LinkLatestRun], ["ChecklistCopy.LinkLatestRun"]),
        new("CHK18", [ChecklistCopy.Loading], ["ChecklistCopy.Loading"]),
        new("CHK19", [ChecklistCopy.Mandatory], ["ChecklistCopy.Mandatory"]),
        new("CHK20", [ChecklistCopy.MarkNeedsDecision], ["ChecklistCopy.MarkNeedsDecision"]),
        new("CHK21", [ChecklistCopy.NewObligationName], ["ChecklistCopy.NewObligationName"]),
        new("CHK22", [ChecklistCopy.NotConnected], ["ChecklistCopy.NotConnected"]),
        new("NB3", [DaemonNotebookRevisions.OpenRefusedCopy], ["DaemonNotebookRevisions.OpenRefusedCopy"]),
        new("ST1", [Core("AiDe.Core.Projections.SolutionTreeProjection", "NotRecordedCopy")], ["SolutionTreeProjection.NotRecordedCopy"]),
        new("ST2", [Core("AiDe.Core.Projections.SolutionTreeProjection", "PythonTsCopy")], ["SolutionTreeProjection.PythonTsCopy"]),
        new("TURN1", OutcomeWords(), ["TurnCopy.OutcomeWord"]),
        new("TURN2", [TurnCopy.Name(StopRequestedTurn()).Replace("b1", "{ordinal}").Replace("words", "{words}")], ["TurnCopy.Name"]),
        new(
            "TURN3",
            [TurnCopy.DecorationLine([new("class", "free-form", "s", "r"), new("tier", "T0", "s", "r"), new("lease", "src/**", "s", "r"), new("shape", "goal block", "s", "r")])],
            ["TurnCopy.DecorationLine"]),
        new("TURN4", [N(TurnCopy.EditsText(SampleRevision)), TurnCopy.EditsText(1), TurnCopy.EditsText(null)], ["TurnCopy.EditsText"]),
        new("TURN5", [Spent(new Spend(400, 0, 24, 1)), Spent(null)], ["TurnCopy.SpendText"]),
        new("TURN6", [TurnCopy.EventsText(424).Replace("424", "{n}"), TurnCopy.EventsText(1)], ["TurnCopy.EventsText"]),
        new(
            "TURN7",
            [TurnCopy.DurationText(new TimeSpan(0, 4, 12)), TurnCopy.DurationText(TimeSpan.FromSeconds(41)), TurnCopy.DurationText(new TimeSpan(1, 2, 0))],
            ["TurnCopy.DurationText"]),
        new("TURN8", [TurnCopy.SpokenDuration(new TimeSpan(0, 4, 12)), TurnCopy.SpokenDuration(TimeSpan.FromSeconds(41))], ["TurnCopy.SpokenDuration"]),
        new(
            "TURN9",
            [
                SessionSpendSample(TurnCopy.SessionSpend([Turn(1, TurnState.Completed, Outcome(new Spend(12000, 0, 400, 1)))], null)),
                SessionSpendSample(TurnCopy.SessionSpend([], null)),
                SessionSpendSample(TurnCopy.SessionSpend([Turn(1, TurnState.Completed, Outcome(new Spend(12000, 0, 400, 1))), Turn(2, TurnState.Completed, Outcome())], null)),
                SessionSpendSample(TurnCopy.SessionSpend([Turn(1, TurnState.Completed, Outcome(new Spend(12000, 0, 400, 1)))], 40_000)),
            ],
            ["TurnCopy.SessionSpend"]),
        new(
            "TURN10",
            [
                QueuedSample(TurnCopy.QueuedSentence(Snapshot(Turn(2, TurnState.Queued)))),
                QueuedSample(TurnCopy.QueuedSentence(Snapshot(Turn(1, TurnState.Running), Turn(2, TurnState.Queued)))),
                QueuedSample(TurnCopy.QueuedSentence(Snapshot(Turn(1, TurnState.Interrupted, Outcome()), Turn(2, TurnState.Queued)))),
            ],
            ["TurnCopy.QueuedSentence"]),
        new("DOCK1", [SessionDocumentSurface.PaneHiddenCopy], ["SessionDocumentSurface.PaneHiddenCopy"]),
        new("DOCK4", [KeptPromptsSingular(), "Kept {N} prompt drafts as they were: prompt drafts do not become notebooks."], ["LayoutPersistence.KeptPromptsSentence"]),
    ];

    private static bool IsStale(string cell) => cell.StartsWith(StaleMark, StringComparison.Ordinal);

    /// <summary>The original sentence of a stale row: the text after the mark's reason, cut before any trailing annotation.</summary>
    private static string StaleSentence(string cell)
    {
        var end = cell.IndexOf(")* ", StringComparison.Ordinal);
        var sentence = end < 0 ? string.Empty : cell[(end + 3)..].Trim();
        var note = sentence.IndexOf(" (", StringComparison.Ordinal);
        return note < 0 ? sentence : sentence[..note];
    }

    private static Dictionary<string, string> DesignRows()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var rows = new Dictionary<string, string>();
        foreach (var line in File.ReadAllLines(Path.Combine(dir!.FullName, "DESIGN.md")))
        {
            var m = IdRow.Match(line);
            if (m.Success)
            {
                rows.Add(m.Groups["id"].Value, m.Groups["cell"].Value);
            }
        }

        return rows;
    }

    private static IEnumerable<(Type Type, FieldInfo Field)> CopyFields()
    {
        foreach (var assembly in new[] { typeof(ChecklistCopy).Assembly, typeof(TurnCopy).Assembly })
        {
            foreach (var type in assembly.GetTypes())
            {
                var isCopyType = type.Name.EndsWith("Copy", StringComparison.Ordinal);
                const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
                foreach (var f in type.GetFields(all).Where(f => f.IsLiteral && f.FieldType == typeof(string)))
                {
                    if (isCopyType || f.Name.EndsWith("Copy", StringComparison.Ordinal))
                    {
                        yield return (type, f);
                    }
                }
            }
        }
    }

    private static HashSet<string> CopyMembers()
    {
        var found = CopyFields().Select(x => x.Type.Name + "." + x.Field.Name).ToHashSet();
        foreach (var assembly in new[] { typeof(ChecklistCopy).Assembly, typeof(TurnCopy).Assembly })
        {
            foreach (var type in assembly.GetTypes().Where(t => t.Name.EndsWith("Copy", StringComparison.Ordinal)))
            {
                const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
                foreach (var mth in type.GetMethods(all).Where(mth => mth.ReturnType == typeof(string) && !mth.IsSpecialName))
                {
                    found.Add(type.Name + "." + mth.Name);
                }
            }
        }

        return found;
    }

    /// <summary>Every sentence the code is known to render: each pin's rendered text and each copy constant's value.</summary>
    private static IEnumerable<string> RenderedCorpus() =>
        Pins().SelectMany(p => p.Rendered).Concat(CopyFields().Select(x => (string)x.Field.GetRawConstantValue()!));

    [Fact]
    public void EveryPinnedRow_CarriesTheLiteralTheCodeRenders()
    {
        var rows = DesignRows();
        var failures = new List<string>();
        foreach (var pin in Pins())
        {
            if (!rows.TryGetValue(pin.RowId, out var cell))
            {
                failures.Add($"{pin.RowId}: pinned, but DESIGN.md has no such id row");
                continue;
            }

            if (IsStale(cell))
            {
                failures.Add($"{pin.RowId}: pinned, but the DESIGN.md row is marked stale; a stale row is not pinned (un-stale it, or drop the pin)");
                continue;
            }

            if (!cell.StartsWith(pin.Rendered[0], StringComparison.Ordinal))
            {
                failures.Add($"{pin.RowId}: DESIGN.md cell does not start with the rendered \"{pin.Rendered[0]}\" (cell: \"{cell}\")");
            }

            failures.AddRange(pin.Rendered.Skip(1).Where(r => !cell.Contains(r, StringComparison.Ordinal))
                .Select(r => $"{pin.RowId}: DESIGN.md cell does not contain the rendered \"{r}\""));
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void EveryDesignIdRow_IsPinnedOrNamedOnTheShrinkOnlyList()
    {
        var rows = DesignRows();
        var pinned = Pins().Select(p => p.RowId).ToHashSet();
        var failures = new List<string>();
        var stale = rows.Where(r => IsStale(r.Value)).Select(r => r.Key).ToHashSet();
        failures.AddRange(rows.Keys.Where(id => !stale.Contains(id) && !pinned.Contains(id) && !UnmatchedRows.ContainsKey(id))
            .Select(id => $"{id}: a DESIGN.md copy row no code literal is pinned to, and not on UnmatchedRows"));
        failures.AddRange(UnmatchedRows.Keys.Where(id => !rows.ContainsKey(id))
            .Select(id => $"{id}: stale UnmatchedRows entry, the DESIGN.md row is gone; delete it"));
        failures.AddRange(UnmatchedRows.Keys.Where(pinned.Contains)
            .Select(id => $"{id}: stale UnmatchedRows entry, the row is now pinned; delete it"));
        failures.AddRange(UnmatchedRows.Keys.Where(stale.Contains)
            .Select(id => $"{id}: on UnmatchedRows and marked stale in DESIGN.md; a stale row is neither pinned nor unmatched, delete the entry"));

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void EveryCopyConstant_IsPinnedOrNamedOnTheShrinkOnlyList()
    {
        var members = CopyMembers();
        var covered = Pins().SelectMany(p => p.Members).ToHashSet();
        var failures = new List<string>();
        failures.AddRange(members.Where(m => !covered.Contains(m) && !UnmatchedMembers.ContainsKey(m)).Order()
            .Select(m => $"{m}: a copy constant or builder no DESIGN.md id row pins, and not on UnmatchedMembers"));
        failures.AddRange(UnmatchedMembers.Keys.Where(m => !members.Contains(m))
            .Select(m => $"{m}: stale UnmatchedMembers entry, the member is gone or no longer a copy member; delete it"));
        failures.AddRange(UnmatchedMembers.Keys.Where(covered.Contains)
            .Select(m => $"{m}: stale UnmatchedMembers entry, the member is now pinned; delete it"));

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void AStaleRow_IsNotRenderedByAnyCode()
    {
        var corpus = RenderedCorpus().ToList();
        var failures = DesignRows().Where(r => IsStale(r.Value))
            .Where(r => StaleSentence(r.Value) is { Length: > 0 } sentence && corpus.Any(c => c.Contains(sentence, StringComparison.Ordinal)))
            .Select(r => $"{r.Key}: stale row is rendered; un-stale it (\"{StaleSentence(r.Value)}\")")
            .ToList();

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
