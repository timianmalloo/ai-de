using System.Diagnostics;
using AiDe.App.Workbench.Records;
using AiDe.Core.Store.Records;
using AiDe.Core.Workbench.Records.Folds;

namespace AiDe.App.Tests.Records;

/// <summary>
/// Ruling 178: currentness has ONE definition. The notebook's stale notice (<see cref="NotebookModel.CurrentnessOf"/>)
/// and the checklist's evidence state (<see cref="AcceptanceFold"/>) both read <see cref="CurrentnessFold"/>; the stale
/// sentence is one literal in <c>src/</c>. Model-level: no window is opened.
/// </summary>
public sealed class OneCurrentnessDefinitionTests : IDisposable
{
    private const string Sentence = "This output belongs to an earlier draft. Run again to refresh it.";

    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    /// <summary>The names of the folds that ran inside <paramref name="body"/>, from the pack's own fold signal.</summary>
    private static List<string> FoldsRunBy(Action body)
    {
        var seen = new List<string>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == FoldSignal.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => seen.Add((string)a.GetTagItem("fold")!),
        };
        ActivitySource.AddActivityListener(listener);
        body();
        return seen;
    }

    // ---- (A) the notebook's stale notice and the checklist's evidence state are one fold ----------

    [Fact]
    public async Task A_TheNotebookNoticeAndTheChecklistEvidence_ReadStale_FromTheSameFold()
    {
        using var model = _fixture.NewModel();
        var ran = (await model.PrepareRevisionAsync("Fix the door.", default))!;
        model.BindResult(1, ran);
        var later = (await model.PrepareRevisionAsync("Fix the back door.", default))!;

        var notebook = Currentness.NotRecorded;
        var checklist = EvidenceState.Missing;
        var folds = FoldsRunBy(() =>
        {
            notebook = model.CurrentnessOf(1, string.Empty) switch
            {
                ResultCurrentness.Stale => Currentness.Stale,
                ResultCurrentness.Current => Currentness.Current,
                _ => Currentness.NotRecorded,
            };
            checklist = AcceptanceFold.Evaluate(ChecklistOver(ran, later)).Obligations.Single().Evidence;
        });

        // One tuple, so a mutation of the fold shows both surfaces flipping at once.
        Assert.Equal((Currentness.Stale, EvidenceState.Stale), (notebook, checklist));
        // Each surface asked CurrentnessFold: one "currentness" signal from the notice, one from the checklist's judge.
        Assert.Equal(2, folds.Count(f => f == "currentness"));
    }

    /// <summary>A checklist whose one obligation cites the attempt that ran <paramref name="ran"/>, judged against <paramref name="latest"/>.</summary>
    private static AcceptanceInput ChecklistOver(NotebookRevision ran, NotebookRevision latest)
    {
        var notebookId = ran.Notebook.Value;
        var obligation = new ObligationRow("o1", "j1", 1, "Door fixed", true);
        var link = new EvidenceLinkRow(
            "l1", "obligation", "o1", 1, AttemptStateFold.AttemptArtifactRef("dk-1"), "claude",
            AcceptanceBaseline.For(notebookId, latest.RevisionNo), AcceptanceFold.ObservedStatus);
        var attempt = new AttemptRow("dk-1", "w", 1, notebookId, ran.RevisionNo, "s", 1, ran.BodySha);

        return new AcceptanceInput(
            "j1", notebookId, latest.RevisionNo,
            [obligation],
            new Dictionary<string, IReadOnlyList<ObligationRow>> { ["o1"] = [obligation] },
            new Dictionary<string, IReadOnlyList<EvidenceLinkRow>> { ["o1"] = [link] },
            new Dictionary<string, IReadOnlyList<DispositionRow>>(),
            [],
            new Dictionary<string, AttemptRow> { ["dk-1"] = attempt },
            latest.BodySha);
    }

    // ---- (B) the delegation keeps CurrentnessOf's behaviour, the empty-draft clause included -------

    [Fact]
    public async Task B_CurrentnessOf_KeepsItsVerdicts_AndAsksTheFoldForEachRecordedOne()
    {
        using var model = _fixture.NewModel();
        var ran = (await model.PrepareRevisionAsync("Fix the door.", default))!;
        model.BindResult(1, ran);

        var verdicts = new List<ResultCurrentness>();
        var folds = FoldsRunBy(() =>
        {
            verdicts.Add(model.CurrentnessOf(1, "Fix the door."));
            verdicts.Add(model.CurrentnessOf(1, string.Empty));
            verdicts.Add(model.CurrentnessOf(1, "Fix the back door."));
            verdicts.Add(model.CurrentnessOf(2, "Fix the door."));
        });

        Assert.Equal(
            [ResultCurrentness.Current, ResultCurrentness.Current, ResultCurrentness.Stale, ResultCurrentness.NotRecorded],
            verdicts);
        Assert.Equal(3, folds.Count(f => f == "currentness"));

        // A later saved revision outranks the empty-draft clause.
        await model.PrepareRevisionAsync("Fix the back door.", default);
        Assert.Equal(ResultCurrentness.Stale, model.CurrentnessOf(1, string.Empty));
    }

    // ---- (C) one stale sentence in src/ -------------------------------------------------------------

    [Fact]
    public void C_TheStaleSentenceIsOneLiteralInSrc_AndEverySurfaceNamesTheSameValue()
    {
        var src = Path.Combine(RepoRoot(), "src");
        var copies = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => File.ReadAllText(f).Contains(Sentence, StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(src, f))
            .ToList();

        Assert.Single(copies);
        Assert.Equal(Sentence, CurrentnessFold.StaleCopy);
        Assert.Equal(CurrentnessFold.StaleCopy, NotebookModel.StaleCopy);
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
