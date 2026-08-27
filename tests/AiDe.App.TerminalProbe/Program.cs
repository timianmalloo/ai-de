using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using AiDe.Core.Facts;
using AiDe.Core.Terminal;

namespace AiDe.App.TerminalProbe;

/// <summary>
/// Does a ConPTY session deliver a child's output from a process with <b>no console at all</b>?
/// </summary>
/// <remarks>
/// <para><b>The question the product depends on.</b> <c>AiDe.App</c> is a GUI application: WinExe,
/// no console, never had one. <b>DC-014</b> measured a different arrangement — a console-less
/// <i>test</i> host whose stdio was redirected — and its control hands the probe a console with
/// <c>CREATE_NEW_CONSOLE</c>. Neither establishes what happens in the configuration that actually
/// ships, and "the terminal pane is empty in the real app" is not a defect any existing test could
/// report: every one of them would still be green.</para>
///
/// <para>Reports by exit code: <b>0</b> the child's output arrived, <b>2</b> it did not, <b>3</b>
/// the session could not start, <b>5</b> this host unexpectedly has a console, which would
/// invalidate the measurement rather than pass it.</para>
/// </remarks>
[SupportedOSPlatform("windows")]
internal static class Program
{
    private const string Marker = "gui-host-marker";

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    private static async Task<int> Main(string[] args)
    {
        var report = args.Length > 0 ? args[0] : null;
        var log = new StringBuilder();

        if (GetConsoleWindow() != IntPtr.Zero)
        {
            log.AppendLine("this host HAS a console, so it does not reproduce the product's case");
            Write(report, log);
            return 5;
        }

        log.AppendLine("host has no console (GUI subsystem) — the product's actual configuration");

        ConPtyTerminalSession session;
        try
        {
            session = await ConPtyTerminalSession.StartAsync(
                new TerminalSessionRequest(
                    SessionId: "gui-probe",
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
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(25));

            try
            {
                while (await session.Output.WaitToReadAsync(deadline.Token))
                {
                    while (session.Output.TryRead(out var chunk))
                    {
                        seen.Append(Encoding.UTF8.GetString(chunk.Bytes.Span));
                        if (seen.ToString().Contains(Marker, StringComparison.Ordinal))
                        {
                            log.AppendLine($"CAPTURED after {seen.Length} chars");
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
            log.AppendLine("raw: " + Readable(seen.ToString()));
            Write(report, log);
            return 2;
        }
    }

    /// <summary>Escape codes rendered visibly, so a report is legible in a text file.</summary>
    private static string Readable(string raw) =>
        raw.Replace(((char)0x1B).ToString(), "<ESC>", StringComparison.Ordinal);

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
    }
}
