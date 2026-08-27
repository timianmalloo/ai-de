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
        var mode = args.Length > 1 ? args[1] : "capture";
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

        if (mode == "osc")
        {
            return await OscAsync(report, log);
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
    /// Drives OSC 133 through a real pseudo console, forged first and authenticated second.
    /// </summary>
    /// <remarks>
    /// <para><b>The question this answers is not "does the parser work".</b> That is settled by unit
    /// tests. It is whether an OSC sequence written by a child process <i>survives the round trip at
    /// all</i> — ConPTY is not a pipe, it is a terminal emulator that parses the child's output and
    /// re-emits its own VT stream, and a sequence it does not recognise could reasonably be dropped
    /// on the floor. If it were, every unit test above would still pass and the feature would be
    /// inert in production. That is a claim only a real console can settle.</para>
    ///
    /// <para>Forged is tried <b>first, deliberately</b>. An unauthenticated claim arrives while the
    /// session is still using the output heuristic, which is the state a real session is in when a
    /// hostile child would try it — and running it second, after a genuine claim had made OSC
    /// authoritative, would test an easier case than the real one.</para>
    ///
    /// <para>Exit codes: <b>0</b> both correct, <b>3</b> could not start, <b>5</b> the forged claim
    /// was honoured, <b>6</b> the authenticated claim was not honoured, <b>7</b> timed out.</para>
    /// </remarks>
    private static async Task<int> OscAsync(string? report, StringBuilder log)
    {
        ConPtyTerminalSession session;
        try
        {
            session = await ConPtyTerminalSession.StartAsync(
                new TerminalSessionRequest(
                    SessionId: "osc-probe",
                    Generation: 1,
                    // PowerShell rather than cmd because it can emit a raw ESC in one legible line.
                    CommandLine: "powershell.exe -NoProfile -NoLogo",
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
            var nonce = session.ShellIntegrationNonce;
            log.AppendLine($"nonce length: {nonce.Length}");

            // --- 1. forged: no nonce, sent while the heuristic is still in charge -------------
            await SendAsync(session, "[Console]::Write([char]27+']133;D;0'+[char]27+'\\'); 'FORGED-DONE'");

            if (!await WaitForAsync(session, "FORGED-DONE", log))
            {
                Write(report, log);
                return 7;
            }

            var afterForged = session.Activity;
            log.AppendLine($"activity after the forged claim: {afterForged}");

            if (afterForged == SessionActivity.Ready)
            {
                log.AppendLine("FAIL: an unauthenticated OSC 133;D was honoured");
                Write(report, log);
                return 5;
            }

            // --- 2. authenticated -------------------------------------------------------------
            await SendAsync(
                session, $"[Console]::Write([char]27+']133;D;0;nonce={nonce}'+[char]27+'\\'); 'REAL-DONE'");

            if (!await WaitForAsync(session, "REAL-DONE", log))
            {
                Write(report, log);
                return 7;
            }

            var afterReal = session.Activity;
            log.AppendLine($"activity after the authenticated claim: {afterReal}");

            if (afterReal != SessionActivity.Ready)
            {
                log.AppendLine(
                    "FAIL: an authenticated OSC 133;D did not reach the parser. Either ConPTY does "
                    + "not pass OSC through, or the read loop is not feeding it.");
                Write(report, log);
                return 6;
            }

            log.AppendLine("both cases correct");
            Write(report, log);
            return 0;
        }
    }

    private static async Task SendAsync(ConPtyTerminalSession session, string line) =>
        await session.WriteAsync(1, Encoding.UTF8.GetBytes(line + "\r"), CancellationToken.None);

    /// <summary>Drains output until <paramref name="marker"/> appears, or the deadline passes.</summary>
    /// <remarks>
    /// The marker is printed by the same command that emitted the sequence, so seeing it proves the
    /// read loop has already consumed the sequence — the two cannot be reordered, because they leave
    /// the child in that order down one pipe.
    /// </remarks>
    private static async Task<bool> WaitForAsync(
        ConPtyTerminalSession session, string marker, StringBuilder log)
    {
        var seen = new StringBuilder();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(25));

        try
        {
            while (await session.Output.WaitToReadAsync(deadline.Token))
            {
                while (session.Output.TryRead(out var chunk))
                {
                    seen.Append(Encoding.UTF8.GetString(chunk.Bytes.Span));

                    // The echo of the command we typed also contains the marker, so require the
                    // second occurrence: the first is the terminal echoing our own keystrokes back.
                    var first = seen.ToString().IndexOf(marker, StringComparison.Ordinal);
                    if (first >= 0
                        && seen.ToString().IndexOf(marker, first + 1, StringComparison.Ordinal) >= 0)
                    {
                        return true;
                    }
                }
            }

            log.AppendLine($"output completed before '{marker}' appeared");
            return false;
        }
        catch (OperationCanceledException)
        {
            log.AppendLine($"timed out waiting for '{marker}'");
            log.AppendLine("raw: " + seen.ToString().Replace("", "<ESC>"));
            return false;
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
