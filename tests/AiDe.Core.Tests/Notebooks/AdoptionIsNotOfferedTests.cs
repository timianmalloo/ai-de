using AiDe.Core.Store;
using AiDe.Core.Store.Records;
using AiDe.Core.Tests.SchemaV2;

namespace AiDe.Core.Tests.Notebooks;

/// <summary>
/// Ruling 156(3): until the v3 <c>adopted_by</c> column lands in the first slice that offers adoption, the adoption
/// branch of <see cref="StoreWriter.AppendNotebook"/> refuses with one stable code, so no unattributed adoption is ever
/// written. Both legal transitions of Ruling 148/150 are refused; creating a notebook (version 1) is not.
/// </summary>
/// <remarks>The code is a literal, not the production constant, so a rename fails here.</remarks>
public sealed class AdoptionIsNotOfferedTests : IDisposable
{
    private const string NotOffered = "AIDE-STORE-ADOPTION-NOT-OFFERED";

    private readonly string _directory = SchemaV2Support.NewDirectory();

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void AdoptingALegacyNotebook_IsRefused_AndWritesNoVersionRow()
    {
        // A legacy row exists only from the migration backfill (Ruling 148 c), so start from a real v1 file.
        var path = Path.Combine(_directory, "legacy.db");
        SchemaV2Support.CreateV1Database(path);
        using var store = WorkspaceStore.Open(path);
        var legacy = LegacyNotebookOf(path);

        using (var writer = store.BeginWrite())
        {
            writer.AppendJourney(new JourneyRow("j-adopter", "work", "the adopting journey"));
            var refused = Assert.Throws<WorkspaceStoreException>(() =>
                writer.AppendNotebook(new NotebookRow(legacy, "j-adopter", Legacy: false)));
            Assert.Equal(NotOffered, refused.ErrorCode);
        }

        using var raw = SchemaV2Support.OpenRaw(path, readOnly: true);
        Assert.Equal(0, SchemaV2Support.Count(raw, $"SELECT count(*) FROM notebook_dim WHERE notebook_id = '{legacy}' AND version_no > 1"));
    }

    [Fact]
    public void ReOwningATerminalDraft_OutOfTheStandingJourney_IsRefused()
    {
        var path = Path.Combine(_directory, "terminal.db");
        using var store = WorkspaceStore.Open(path);
        using (var writer = store.BeginWrite())
        {
            // The PTY path's draft: owned by journey-terminal-drafts (Ruling 150).
            writer.SavePromptRevision("pty-draft", 1, "hello");
            writer.AppendJourney(new JourneyRow("j-real", "work", "a real journey"));
            writer.Commit();
        }

        using (var writer = store.BeginWrite())
        {
            var refused = Assert.Throws<WorkspaceStoreException>(() =>
                writer.AppendNotebook(new NotebookRow("pty-draft", "j-real", Legacy: false)));
            Assert.Equal(NotOffered, refused.ErrorCode);
        }

        using var raw = SchemaV2Support.OpenRaw(path, readOnly: true);
        Assert.Equal(0, SchemaV2Support.Count(raw, "SELECT count(*) FROM notebook_dim WHERE notebook_id = 'pty-draft' AND version_no > 1"));
        Assert.Equal(1, SchemaV2Support.Count(raw, "SELECT count(*) FROM prompt_revision_fact WHERE draft_id = 'pty-draft'"));
    }

    [Fact]
    public void CreatingANotebook_AtVersionOne_StillPasses()
    {
        var path = Path.Combine(_directory, "create.db");
        using var store = WorkspaceStore.Open(path);
        using (var writer = store.BeginWrite())
        {
            writer.AppendJourney(new JourneyRow("j-1", "work", "a journey"));
            writer.AppendNotebook(new NotebookRow("nb-1", "j-1", Legacy: false));
            writer.Commit();
        }

        using var raw = SchemaV2Support.OpenRaw(path, readOnly: true);
        Assert.Equal(1, SchemaV2Support.Count(raw,
            "SELECT count(*) FROM notebook_dim WHERE notebook_id = 'nb-1' AND version_no = 1 AND owner_journey_id = 'j-1' AND legacy = 0"));
    }

    /// <summary>A notebook the backfill marked legacy, with no owner.</summary>
    private static string LegacyNotebookOf(string path)
    {
        using var raw = SchemaV2Support.OpenRaw(path, readOnly: true);
        using var command = raw.CreateCommand();
        command.CommandText = "SELECT notebook_id FROM notebook_dim WHERE legacy = 1 AND owner_journey_id IS NULL LIMIT 1;";
        return command.ExecuteScalar() as string ?? throw new InvalidOperationException("the v1 fixture backfilled no legacy notebook");
    }
}
