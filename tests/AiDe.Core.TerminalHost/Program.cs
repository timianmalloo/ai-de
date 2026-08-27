using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Terminal;

namespace AiDe.Core.TerminalHost;

/// <summary>
/// Drives a real <see cref="ConPtyTerminalSession"/> end to end and reports the verdict by exit code.
/// </summary>
/// <remarks>
/// <para><b>Why this exists.</b> ConPTY only attaches a child to the pseudo console when the host
/// process owns a <b>real console</b>. A <c>dotnet test</c> host never does — its stdio is
/// redirected — so the round trip cannot be verified in-process no matter how correct the runtime
/// is. Measured 2026-08-26: identical code captures the child's stdout under <c>dotnet run</c> from
/// a terminal (90 bytes, marker present) and captures nothing under a console-less host.</para>
///
/// <para>The conformance suite launches this executable with <c>CREATE_NEW_CONSOLE</c>, which gives
/// it a console of its own, and asserts on the exit code. That keeps the claim testable rather than
/// downgrading it to a comment, which is what a "known environment limitation" note would have
/// been.</para>
///
/// <para>Exit codes are the contract: <b>0</b> the child's output was captured, <b>2</b> it was not,
/// <b>3</b> the session could not start, <b>4</b> no console (the caller forgot the flag).</para>
/// </remarks>
[SupportedOSPlatform("windows")]
internal static class Program
{
    private const string Marker = "conpty-host-marker";

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    private static async Task<int> Main(string[] args)
    {
        var report = args.Length > 0 ? args[0] : null;
        var log = new StringBuilder();

        // Fail loudly rather than quietly reporting "not captured": a missing console is the
        // caller's mistake, and reporting it as a product failure would be a false negative that
        // looks exactly like a real one.
        if (GetConsoleWindow() == IntPtr.Zero)
        {
            log.AppendLine("no console window — launch with CREATE_NEW_CONSOLE");
            Write(report, log);
            return 4;
        }

        ConPtyTerminalSession session;
        try
        {
            session = await ConPtyTerminalSession.StartAsync(
                new TerminalSessionRequest(
                    SessionId: "host-probe",
                    Generation: 1,
                    CommandLine: "cmd.exe",
                    WorkingDirectory: Path.GetTempPath(),
                    Columns: 80,
                    Rows: 25,
                    ProcessingClass: SessionProcessingClass.LocalOnly),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            log.AppendLine($"StartAsync threw {ex.GetType().Name}: {ex.Message}");
            Write(report, log);
            return 3;
        }

        await using (session)
        {
            var written = await session.WriteAsync(
                1, Encoding.UTF8.GetBytes($"echo {Marker}\r"), CancellationToken.None);
            log.AppendLine($"write result: {written}");

            var seen = new StringBuilder();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            try
            {
                while (await session.Output.WaitToReadAsync(deadline.Token))
                {
                    while (session.Output.TryRead(out var chunk))
                    {
                        seen.Append(Encoding.UTF8.GetString(chunk.Bytes.Span));
                        if (seen.ToString().Contains(Marker, StringComparison.Ordinal))
                        {
                            log.AppendLine($"captured after {seen.Length} chars");
                            Write(report, log);
                            return 0;
                        }
                    }
                }

                log.AppendLine("output channel completed without the marker");
            }
            catch (OperationCanceledException)
            {
                log.AppendLine("timed out waiting for the marker");
            }

            log.AppendLine($"activity: {session.Activity}");
            log.AppendLine($"bytes seen: {seen.Length}");
            log.AppendLine("raw: " + seen.ToString().Replace("", "<ESC>"));
            Write(report, log);
            return 2;
        }
    }

    /// <summary>
    /// Writes the log where the caller can read it.
    /// </summary>
    /// <remarks>
    /// A new console closes when this process exits, taking its own stdout with it, so the detail
    /// has to leave through a file. Without it a failing run reports only an exit code, and "2"
    /// tells nobody why.
    /// </remarks>
    private static void Write(string? path, StringBuilder log)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            File.WriteAllText(path, log.ToString());
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
