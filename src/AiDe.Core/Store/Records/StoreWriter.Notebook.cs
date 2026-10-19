using AiDe.Core.Store.Records;

namespace AiDe.Core.Store;

// The notebook operations' write half. Each method reads and writes inside the caller's writer
// transaction, which holds the store's single-writer gate, so a read-before-write cannot be split.
public sealed partial class StoreWriter
{
    /// <summary>
    /// Makes sure the notebook exists and returns its owner. The first call appends a work journey and
    /// the notebook it owns (legacy = 0) in this transaction; a notebook that already has a row is left
    /// untouched and its current owner (null for an unadopted legacy notebook) is returned.
    /// </summary>
    /// <remarks>
    /// assume: the journey id <c>journey-{notebookId}</c> is unused. Confirm: <c>ReadJourney</c> returns null
    /// before the append. If false: <see cref="AppendJourney"/> would add a version to someone else's journey.
    /// </remarks>
    public string? OpenNotebook(string notebookId, string journeyTitle)
    {
        var existing = ScalarRow("SELECT owner_journey_id FROM notebook_dim WHERE notebook_id = $id "
            + "ORDER BY version_no DESC LIMIT 1;", ("$id", notebookId));
        if (existing is not null)
        {
            return existing[0] as string;
        }

        var journeyId = $"journey-{notebookId}";
        AppendJourney(new JourneyRow(journeyId, "work", journeyTitle));
        AppendNotebook(new NotebookRow(notebookId, journeyId, Legacy: false));
        return journeyId;
    }

    /// <summary>
    /// Appends <paramref name="body"/> as the next revision unless it is byte-identical to the latest one, then
    /// returns the revision now at the head. The revision number is the stored maximum plus one.
    /// </summary>
    public (int RevisionNo, string BodySha, string SavedAt) SaveNotebookRevision(string notebookId, string body, int? parentRevisionNo)
    {
        var head = HeadRevision(notebookId);
        if (head is null || head.Value.BodySha != BodySha(body))
        {
            AppendNotebookRevision(new NotebookRevisionRow(notebookId, (head?.RevisionNo ?? 0) + 1, body, parentRevisionNo));
            head = HeadRevision(notebookId);
        }

        return head!.Value;
    }

    private (int RevisionNo, string BodySha, string SavedAt)? HeadRevision(string notebookId)
    {
        var row = ScalarRow("SELECT revision_no, body_sha, saved_at FROM prompt_revision_fact "
            + "WHERE draft_id = $id ORDER BY revision_no DESC LIMIT 1;", ("$id", notebookId));
        return row is null ? null : (Convert.ToInt32(row[0]), row[1] as string ?? "", (string)row[2]!);
    }
}
