using System.Globalization;

namespace AiDe.App.Workbench.Checklist;

/// <summary>
/// The checklist's load-bearing copy (the WL-1b design, "UI and interaction design"; <c>DESIGN.md</c> rows CHK1–CHK12).
/// One home for every sentence the surface shows, so a test reads the same const the surface renders.
/// </summary>
/// <remarks>
/// The gap sentences ("Blocked — …") are the Core fold's (<c>AcceptanceFold</c>) and are shown as the fold wrote them,
/// never re-worded here: two definitions of one sentence is the shape DM7 names.
/// </remarks>
public static class ChecklistCopy
{
    public const string Empty = "No obligations yet. Add the conditions this work must meet.";

    public const string EvidenceMissing = "Evidence missing.";

    public const string Acknowledged = "Acknowledged — not satisfied. Acknowledging is not evidence.";

    public const string Deferred = "Deferred — acceptance stays blocked.";

    public const string NeedsDecision = "Needs a decision.";

    public const string NoDisposition = "Not reviewed.";

    public const string Stale = "The evidence belongs to an earlier draft. Link evidence from the current draft.";

    public const string NotRecorded = "Evidence currentness is not recorded, so this does not count as current.";

    public const string Ready = "Ready to accept.";

    public const string NotAWorkJourney = "This notebook has no work journey. It was made before work journeys existed. Obligations need a work journey.";

    /// <summary>The opener's refusal with no notebook to take the journey from, and the pane of an id that names no journey.</summary>
    public const string NoNotebook = "No notebook is focused. Open a notebook in Factory, then open its checklist.";

    /// <summary>A checklist whose own journey's notebook is not open: it names the journey and binds nothing else (seam req-01M5B8W7NJRW7A1X4G21J2QKPT).</summary>
    public static string NotOpen(string journeyId) =>
        $"This checklist belongs to work journey {journeyId}. Its notebook is not open. Reopen it from File → Recent sessions to review this checklist.";

    public const string NotConnected = "The workspace service is not connected, so obligations cannot be read or recorded.";

    public const string Loading = "Loading the checklist…";

    public const string Mandatory = "Mandatory";

    public const string AcceptDeliverable = "Accept deliverable";

    public const string AddObligation = "Add obligation";

    public const string NewObligationName = "New obligation";

    public const string Acknowledge = "Acknowledge";

    public const string LinkLatestRun = "Link the latest run";

    public const string Defer = "Defer";

    public const string MarkNeedsDecision = "Needs decision";

    /// <summary>The acceptance refusal's lead; the fold's gap sentences follow it (read out in the polite live region).</summary>
    public const string RefusedLead = "Not accepted.";

    /// <summary>The one sentence that may say "implemented" or "merged", and only to negate it (Ruling 179).</summary>
    public const string AcceptedNegation = "This records acceptance of the deliverable only. It does not say the work is implemented or merged.";

    public const string AcceptedEarlier = "Accepted at an earlier intent revision. The intent has changed since, so this acceptance does not cover the current revision.";

    /// <summary>"Satisfied by run {7-char key} (intent revision {n})."</summary>
    public static string Satisfied(string dispatchKey, int intentRevision) =>
        string.Create(CultureInfo.InvariantCulture, $"Satisfied by run {dispatchKey[..Math.Min(7, dispatchKey.Length)]} (intent revision {intentRevision}).");

    /// <summary>The Accepted line: it names the intent revision by number and claims nothing about implementation (Ruling 179).</summary>
    public static string Accepted(int intentRevision) =>
        string.Create(CultureInfo.InvariantCulture, $"Accepted: the declared deliverable at intent revision {intentRevision}. {AcceptedNegation}");

    /// <summary>"Accept intent (revision N)".</summary>
    public static string AcceptIntent(int intentRevision) =>
        string.Create(CultureInfo.InvariantCulture, $"Accept intent (revision {intentRevision})");

    public static string IntentAccepted(int intentRevision) =>
        string.Create(CultureInfo.InvariantCulture, $"Intent revision {intentRevision} is accepted.");
}
