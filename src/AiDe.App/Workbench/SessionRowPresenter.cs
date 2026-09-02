using System.Collections.Generic;
using System.Linq;
using AiDe.Core.Presentation;

namespace AiDe.App.Workbench;

/// <summary>
/// Pure presentation policy for the Sessions surface (smoke 9-1 #15). The read model
/// (<see cref="WatcherSessionRow"/>) is honest but its <c>DisplayLabel</c> is a flat, undifferentiated
/// line, so five sessions read as five identical blobs and the answer to "what is this and why is it
/// here" is buried. This turns a row into a legible two-line shape — a stable identity above muted
/// metadata — with a colour-plus-glyph liveness chip, and it states a telemetry gap the whole list
/// shares <b>once</b> rather than repeating "Not Recorded" down every row. No WPF, so it is verifiable
/// headless.
/// </summary>
public static class SessionRowPresenter
{
    /// <summary>
    /// The theme brush key that colours a liveness chip. Colour is the third signal only — the glyph
    /// and text carry the meaning (WCAG 2.2 AA, not colour alone), matching <see cref="LivenessBadge"/>.
    /// </summary>
    public static string ChipBrushKey(LivenessBadge liveness) => liveness.TokenName switch
    {
        "colors.verified" => "VerifiedBrush",   // Alive
        "colors.inferred" => "InferredBrush",   // Stale
        _ => "UnverifiedBrush",                 // Ended / unknown
    };

    /// <summary>The chip's text: the glyph and word together, e.g. "✓ Alive".</summary>
    public static string ChipText(LivenessBadge liveness) => $"{liveness.Glyph} {liveness.Text}";

    /// <summary>The primary line — who and where: the stable human identity a session is recognised by.</summary>
    public static string Identity(WatcherSessionRow row) => $"{row.Agent} · {row.Repository}/{row.Worktree}";

    /// <summary>The muted secondary line — harness, model, trust, spans: metadata, subordinate to identity.</summary>
    public static string Details(WatcherSessionRow row)
    {
        var parts = new List<string> { row.Harness, row.Model, $"trust {row.Trust}", $"{row.SpanCount} span(s)" };
        if (row.Disputed)
        {
            parts.Add(WatcherSessionRow.DisputedText);
        }

        return string.Join(" · ", parts);
    }

    /// <summary>
    /// One line stating a telemetry gap the whole list shares, so it is said once instead of repeated
    /// on every row (#15 — "an all-'Not Recorded' row is a telemetry gap the list should state, not
    /// repeat five times"). Null when the rows do not all share it, or there are fewer than two.
    /// </summary>
    public static string? SharedTelemetryNote(IReadOnlyList<WatcherSessionRow> rows)
    {
        if (rows.Count < 2)
        {
            return null;
        }

        var notRecorded = WatcherSessionText.NotRecorded;
        var allHarness = rows.All(r => r.Harness == notRecorded);
        var allModel = rows.All(r => r.Model == notRecorded);

        if (allHarness && allModel)
        {
            return $"All {rows.Count} sessions report no harness or model — the agent harness isn’t emitting telemetry.";
        }

        if (allHarness)
        {
            return $"All {rows.Count} sessions report no harness — the harness isn’t identifying itself.";
        }

        if (allModel)
        {
            return $"All {rows.Count} sessions report no model.";
        }

        return null;
    }
}
