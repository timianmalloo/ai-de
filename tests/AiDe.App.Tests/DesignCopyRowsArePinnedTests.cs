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
        ["NB1"] = "finding: no code renders \"Draft saved on this device.\"; the row is unbuilt or the sentence was removed",
        ["RUN1"] = "finding: no code renders \"No reasoning summary supplied.\"",
        ["RUN3"] = "finding: no code renders \"Response complete. Evidence still needs review.\"",
        ["RUN4"] = "finding: no code renders \"Connection lost. The run outcome is unknown.\"; TurnCopy says \"Delivery is unknown: ...\" for the nearest state",
        ["CHECK1"] = "finding: no code renders \"Acknowledged by you. Execution evidence is still missing.\"; ChecklistCopy.Acknowledged says a different sentence (CHK2)",
        ["AUTH1"] = "finding: no code renders \"Signed in does not mean this route is qualified.\"",
        ["DEMO1"] = "finding: no code renders \"Interactive prototype. Runs, accounts and evidence are simulated.\" (the sentence lives only in DESIGN.md)",
    };

    /// <summary>Copy constants and builders no DESIGN.md id row names. Shrink-only.</summary>
    private static readonly IReadOnlyDictionary<string, string> UnmatchedMembers = new Dictionary<string, string>
    {
        ["ChecklistCopy.AcceptedEarlier"] = "no row: the stale-acceptance sentence has no id row in DESIGN.md",
        ["ChecklistCopy.Acknowledge"] = "no row: button label, not in an id row",
        ["ChecklistCopy.AddObligation"] = "no row: button label, not in an id row",
        ["ChecklistCopy.Defer"] = "no row: button label, not in an id row",
        ["ChecklistCopy.LinkLatestRun"] = "no row: button label, not in an id row",
        ["ChecklistCopy.Loading"] = "no row: the loading sentence has no id row",
        ["ChecklistCopy.Mandatory"] = "no row: label, not in an id row",
        ["ChecklistCopy.MarkNeedsDecision"] = "no row: button label, not in an id row",
        ["ChecklistCopy.NewObligationName"] = "no row: default obligation name, not in an id row",
        ["ChecklistCopy.NotConnected"] = "no row: the not-connected sentence has no id row",
        ["DaemonNotebookRevisions.OpenRefusedCopy"] = "no row: a diagnostic reason, not in an id row",
        ["SolutionTreeProjection.NotRecordedCopy"] = "no row: solution-tree projection copy, outside the id rows",
        ["SolutionTreeProjection.PythonTsCopy"] = "no row: solution-tree projection copy, outside the id rows",
        ["TurnCopy.DecorationLine"] = "no row: SC-series derivation, the SC rows are layout",
        ["TurnCopy.DurationText"] = "no row: SC-series derivation, the SC rows are layout",
        ["TurnCopy.EditsText"] = "no row: SC-series derivation, the SC rows are layout",
        ["TurnCopy.EventsText"] = "no row: SC-series derivation, the SC rows are layout",
        ["TurnCopy.Name"] = "no row: SC10 UIA name derivation, no id row",
        ["TurnCopy.OutcomeWord"] = "no row: SC7 names the outcome line but its row is layout; the words have no id row",
        ["TurnCopy.QueuedSentence"] = "no row: the queued sentence has no id row",
        ["TurnCopy.SessionSpend"] = "no row: SC-series derivation, the SC rows are layout",
        ["TurnCopy.SpendText"] = "no row: SC-series derivation, the SC rows are layout",
        ["TurnCopy.SpokenDuration"] = "no row: SC-series derivation, the SC rows are layout",
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
