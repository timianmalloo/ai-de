namespace AiDe.Core.Workbench.Records.Folds;

/// <summary>
/// The one home of the acceptance baseline (WL-1b, ADR-0040 section 5): the string
/// <c>intent:&lt;notebook_id&gt;@&lt;revision_no&gt;</c> over the intent notebook's latest saved revision.
/// </summary>
/// <remarks>
/// assume: DR-4 = A (the deliverable is the accepted intent revision). Confirm: the Owner's ruling on
/// req-01M5AD6EH3Y1VE7BGNGM5KHJHH. If false: only this value object and its test change.
/// HEAD, PR head and dirty-content hash are ADR-0040 baseline parts with no producer in WL-1b; the slice
/// that adds one extends this type, and an old baseline then compares unequal and reads stale.
/// </remarks>
public static class AcceptanceBaseline
{
    /// <summary>The baseline of a notebook with no saved revision. It equals no stored baseline.</summary>
    public const string NoRevision = "";

    public static string For(string notebookId, int? latestRevisionNo)
    {
        ArgumentException.ThrowIfNullOrEmpty(notebookId);
        return latestRevisionNo is { } revision ? $"intent:{notebookId}@{revision}" : NoRevision;
    }
}
