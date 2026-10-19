using System.Windows.Automation;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;

namespace AiDe.App.Tests.Records;

/// <summary>
/// WL-1a's stale marker: edit the notebook after a run and the old result reads "This output belongs to an earlier
/// draft. Run again to refresh it." (spec §C3) — derived from the result's revision and the live draft, never stored.
/// </summary>
public sealed class TheOldResultIsMarkedStaleTests : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AnEditMarksTheResultStale_AndTheEmptyNextBlockDoesNot()
    {
        using var model = _fixture.NewModel();
        var first = await model.PrepareRevisionAsync("Fix the door.", default);
        model.BindResult(1, first);

        Assert.Equal(ResultCurrentness.Current, model.CurrentnessOf(1, "Fix the door."));
        Assert.Equal(ResultCurrentness.Current, model.CurrentnessOf(1, string.Empty));
        Assert.Equal(ResultCurrentness.Stale, model.CurrentnessOf(1, "Fix the back door."));

        // A later revision saved: the earlier result is stale whatever the editor holds now.
        await model.PrepareRevisionAsync("Fix the back door.", default);
        Assert.Equal(ResultCurrentness.Stale, model.CurrentnessOf(1, string.Empty));

        // A turn with no bound revision is not recorded — never a plausible "current".
        Assert.Equal(ResultCurrentness.NotRecorded, model.CurrentnessOf(2, string.Empty));
    }

    [Fact]
    public void TheSurfaceShowsTheStaleCopy_WhenTheDraftIsEditedAfterARun_AndClearsItWhenEditedBack()
    {
        var model = _fixture.NewModel();

        Sta.Pump(
            create: () => new SessionDocumentSurface(model),
            body: async (_, document) =>
            {
                _fixture.Configure(document);
                var field = document.Composer.Fields[0].Id;
                document.Composer.SetFieldText(field, 1, "Fix the door.");

                await NotebookFixture.SendAndSettleAsync(document);
                Assert.False(document.StaleNoticeVisible, "a fresh result was marked stale");

                document.Composer.SetFieldText(document.Composer.Fields[0].Id, 2, "Fix the back door.");
                Assert.True(document.StaleNoticeVisible, "an edit after the run left the old result unmarked");
                Assert.Equal(NotebookModel.StaleCopy, document.StaleNoticeText);
                Assert.Equal(NotebookModel.StaleCopy, AutomationProperties.GetName(document.StaleNotice));

                document.Composer.SetFieldText(document.Composer.Fields[0].Id, 3, "Fix the door.");
                Assert.False(document.StaleNoticeVisible, "the draft is the run's revision again, yet the mark stayed");

                document.Dispose();
                model.Dispose();
            });
    }
}
