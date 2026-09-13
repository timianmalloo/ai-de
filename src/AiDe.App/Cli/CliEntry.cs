using System.IO;

namespace AiDe.App.Cli;

/// <summary>
/// The compile step's CLI verbs — <c>aide compile fold</c> and <c>aide session purge</c> — reached
/// from the same process the shell runs in, without a window (ADR-0033 rule 2's third
/// <c>Project()</c> site; ADR-0034 rule 6's deletion command).
/// </summary>
/// <remarks>
/// <para><b>Exit codes are the contract</b>, as the headless conductor's are: <b>0</b> done,
/// <b>3</b> refused or did not complete (the reason is in the output file), <b>64</b> the
/// arguments were wrong. <b>The output is a file, always</b>: a <c>WinExe</c> has no console to
/// print to, so every verb writes what it did to <c>--out</c> (a default under the temp directory
/// when none is given) and echoes it to the console when one happens to be attached.</para>
///
/// <para><b>The dispatch line in <c>App.OnStartup</c> is the Design lane's</b> (a seam request,
/// recorded in the Proof Pack): <c>if (Cli.CliEntry.IsRequested(args)) { … RunAsync … }</c> beside
/// the conductor's. Until it lands the verbs are reachable headlessly (the tests) and not from the
/// shell's process arguments.</para>
/// </remarks>
public static class CliEntry
{
    /// <summary>Whether these process arguments name one of the compile step's verbs.</summary>
    public static bool IsRequested(IReadOnlyList<string> args) =>
        args is { Count: >= 2 }
        && ((string.Equals(args[0], "compile", StringComparison.Ordinal) && string.Equals(args[1], "fold", StringComparison.Ordinal))
            || (string.Equals(args[0], "session", StringComparison.Ordinal) && string.Equals(args[1], "purge", StringComparison.Ordinal)));

    /// <summary>Runs the verb. Returns the exit code; the output file's path is written to <paramref name="console"/> when given.</summary>
    /// <param name="args">The process arguments, verb first.</param>
    /// <param name="console">Where to echo the output, or null.</param>
    /// <param name="confirm">The purge's confirmation prompt (the plan's text in, yes/no out); null means <c>--yes</c> is required.</param>
    public static Task<int> RunAsync(IReadOnlyList<string> args, TextWriter? console = null, Func<string, bool>? confirm = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (!IsRequested(args))
        {
            return Task.FromResult(64);
        }

        return string.Equals(args[0], "compile", StringComparison.Ordinal)
            ? Task.FromResult(CompileFold.Run(args, console))
            : Task.FromResult(PurgeHistoryVerb.Run(args, console, confirm));
    }

    /// <summary>The value after <paramref name="name"/>, or null.</summary>
    internal static string? Value(IReadOnlyList<string> args, string name)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.Ordinal))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    /// <summary>Whether a bare flag is present.</summary>
    internal static bool Flag(IReadOnlyList<string> args, string name) => args.Contains(name, StringComparer.Ordinal);

    /// <summary>Writes the verb's output to its file and echoes it.</summary>
    internal static void Emit(string outFile, string text, TextWriter? console)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outFile))!);
        File.WriteAllText(outFile, text);
        console?.WriteLine(text);
        console?.WriteLine($"written to {Path.GetFullPath(outFile)}");
    }
}
