using System.IO;
using AiDe.Core.PromptCompilation;

namespace AiDe.App.Cli;

/// <summary>
/// <c>aide session purge &lt;id&gt; --workspace &lt;root&gt; [--yes] [--out &lt;file&gt;]</c>: deletes the
/// session's <c>envelope-events.jsonl</c> and nothing else (US-D13; ADR-0034 rule 6). The
/// confirmation prints the session's name, id, workspace root, the resolved file path, the envelope
/// count and the newest <c>at</c> — an identity, never a count alone (DC-120).
/// </summary>
public static class SessionPurge
{
    public static int Run(IReadOnlyList<string> args, TextWriter? console, Func<string, bool>? confirm)
    {
        var sessionId = args.Count > 2 && !args[2].StartsWith("--", StringComparison.Ordinal) ? args[2] : null;
        var workspace = CliEntry.Value(args, "--workspace");
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(workspace))
        {
            console?.WriteLine("usage: aide session purge <id> --workspace <root> [--yes] [--out <file>]");
            return 64;
        }

        var outFile = CliEntry.Value(args, "--out") ?? Path.Combine(Path.GetTempPath(), $"aide-session-purge-{Sanitize(sessionId)}.txt");

        PurgePlan plan;
        try
        {
            plan = EnvelopePurge.Resolve(workspace, sessionId);
        }
        catch (EnvelopeStoreException error)
        {
            CliEntry.Emit(outFile, $"refused: {error.Message}\nnothing was touched", console);
            return 3;
        }

        var description = plan.Describe();
        if (!plan.FileExists)
        {
            CliEntry.Emit(outFile, description + "\nno compile history to purge", console);
            return 0;
        }

        var yes = CliEntry.Flag(args, "--yes") || (confirm is not null && confirm(description));
        if (!yes)
        {
            CliEntry.Emit(outFile, description + "\nnot confirmed; nothing was touched (pass --yes to purge without a prompt)", console);
            return 3;
        }

        try
        {
            EnvelopePurge.Execute(plan);
        }
        catch (EnvelopeStoreException error)
        {
            CliEntry.Emit(outFile, description + $"\nrefused: {error.Message}", console);
            return 3;
        }

        CliEntry.Emit(outFile, description + "\nhistory purged", console);
        return 0;
    }

    private static string Sanitize(string id) => string.Concat(id.Where(c => char.IsAsciiLetterOrDigit(c) || c == '-'));
}
