using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Sessions;

namespace AiDe.App.Cli;

/// <summary>
/// <c>aide compile fold --workspace &lt;root&gt; --session &lt;id&gt; [--out &lt;file&gt;]</c>: recomputes
/// <c>Project(Fold(rows))</c> from the store's own rows and emits the fold and every envelope's
/// projection beside the <c>projection_sha</c> its <c>submitted</c> row recorded — P-D2's rebuild,
/// computed by the product's one <c>Project()</c> (its third named call site), never by a second
/// fold in Python (ADR-0033 "Alternatives considered").
/// </summary>
/// <remarks>
/// The reader opens the file only when no writer holds it (<c>FileShare.None</c>): a composer with
/// the session open is a visible refusal (exit 3), never a partial fold. The rebuild has no held
/// attachment bodies, so the projection's <c>prompt</c> is absent here — <c>text_sha256</c> is the
/// send's witness, <c>projection_sha</c> the rebuild's.
/// </remarks>
public static class CompileFold
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

    public static int Run(IReadOnlyList<string> args, TextWriter? console)
    {
        var workspace = CliEntry.Value(args, "--workspace");
        var sessionId = CliEntry.Value(args, "--session");
        if (string.IsNullOrWhiteSpace(workspace) || string.IsNullOrWhiteSpace(sessionId) || !SessionId.IsValid(sessionId))
        {
            console?.WriteLine("usage: aide compile fold --workspace <root> --session <id> [--out <file>]");
            return 64;
        }

        var outFile = CliEntry.Value(args, "--out") ?? Path.Combine(Path.GetTempPath(), $"aide-compile-fold-{sessionId}.json");
        var file = Path.Combine(SessionPaths.SessionDirectory(workspace, sessionId), EnvelopeStore.FileName);

        EnvelopeFold fold;
        try
        {
            fold = EnvelopeStore.ReadFile(file);
        }
        catch (EnvelopeStoreException error)
        {
            CliEntry.Emit(outFile, new JsonObject { ["session"] = sessionId, ["file"] = file, ["refused"] = error.Code, ["reason"] = error.Message }.ToJsonString(Pretty), console);
            return 3;
        }

        var envelopes = new JsonArray();
        var matched = 0;
        var compared = 0;
        foreach (var envelope in fold.Envelopes)
        {
            var row = new JsonObject
            {
                ["envelope_id"] = envelope.EnvelopeId,
                ["rows"] = envelope.Events.Count,
                ["opened_at"] = envelope.Opened?.At?.ToUniversalTime().ToString("O"),
                ["compile_mode"] = envelope.Opened?.CompileMode,
                ["effective_mode"] = envelope.EffectiveMode,
                ["abandoned"] = envelope.IsAbandoned,
                ["outcome"] = envelope.Outcome,
                ["supersedes"] = envelope.Opened?.Supersedes,
            };

            try
            {
                var projection = Projection.Project(envelope);
                row["projection"] = new JsonObject
                {
                    ["shape"] = projection.Shape,
                    ["tier"] = projection.Tier,
                    ["rationale"] = projection.Rationale,
                    ["rule"] = projection.Rule,
                    ["structure_source"] = projection.StructureSource,
                    ["fan_out_cap"] = projection.FanOutCap,
                    ["fan_out_ceiling"] = projection.FanOutCeiling,
                    ["budget"] = projection.Budget.IsSubscriptionBounded ? "bounded by the subscription" : "cap",
                    ["lease"] = projection.Lease is { } lease ? new JsonArray([.. lease.Exclusive.Select(p => JsonValue.Create(p))]) : null,
                    ["task_class"] = projection.TaskClass,
                    ["task_class_source"] = projection.TaskClassSource,
                    ["goal"] = projection.GoalBlock?.Goal,
                    ["done_when"] = projection.GoalBlock?.DoneWhen,
                    ["not_in_scope"] = projection.GoalBlock?.NotInScope,
                };
                row["rebuilt_projection_sha"] = projection.ProjectionSha;

                if (envelope.Submitted is { } submitted)
                {
                    compared++;
                    var match = string.Equals(submitted.ProjectionSha, projection.ProjectionSha, StringComparison.Ordinal);
                    if (match)
                    {
                        matched++;
                    }

                    row["submitted_projection_sha"] = submitted.ProjectionSha;
                    row["text_sha256"] = submitted.TextSha256;
                    row["projector_version"] = submitted.ProjectorVersion;
                    row["rebuild_matches"] = match;
                }
            }
            catch (EnvelopeStoreException error)
            {
                row["error"] = error.Code;
                row["reason"] = error.Message;
            }

            envelopes.Add(row);
        }

        var report = new JsonObject
        {
            ["session"] = sessionId,
            ["file"] = file,
            ["schema"] = EnvelopeStore.Schema,
            ["projector_version"] = Projection.Version,
            ["report"] = fold.Report,
            ["rows"] = fold.Rows,
            ["skipped_unknown_schema"] = fold.SkippedUnknownSchema,
            ["skipped_unknown_kind"] = fold.SkippedUnknownKind,
            ["skipped_torn"] = fold.SkippedTorn,
            ["broken_at"] = fold.BrokenAt,
            ["rebuild"] = new JsonObject { ["matched"] = matched, ["compared"] = compared },
            ["envelopes"] = envelopes,
        };

        CliEntry.Emit(outFile, report.ToJsonString(Pretty), console);
        return compared == matched ? 0 : 3;
    }
}
