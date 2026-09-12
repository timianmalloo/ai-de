using System.IO;
using System.Text.Json;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// The thread's four records (DS-1 §Telemetry) — <c>thread.layout</c>, <c>thread.announce</c>,
/// <c>thread.focus</c>, <c>thread.action</c> — and its three stable codes: <c>THR-0001</c> the
/// read model's apply failed, <c>THR-0002</c> a region refused focus, <c>THR-0003</c> a version gap.
/// Emitted on the normal path (IO1); no text, ever (O11).
/// </summary>
/// <remarks>
/// <b>Writes through <see cref="WorkbenchDiagnostics.Sink"/> when a test set one, else the same
/// log file the workbench writes.</b> <c>WorkbenchDiagnostics.Write</c> is private and its file is
/// the Shell lane's; a seam request asks for it to become internal so these records go through the
/// one writer — until then the file path is duplicated here and that is named debt (DM7).
/// </remarks>
public static class ThreadDiagnostics
{
    public const string ApplyFailed = "THR-0001";
    public const string FocusRefused = "THR-0002";
    public const string VersionGap = "THR-0003";

    private static readonly Lock Gate = new();

    public static void Layout(string surface, int turns, int realized, double? composerTop, double? viewport, double layoutMs, bool pinned, bool moved, long version) =>
        Write(new { ts = Now(), evt = "thread.layout", surface, turns, realized, composer_top = composerTop, viewport, layout_ms = layoutMs, pinned, moved, version });

    public static void Announce(string surface, int ordinal, string transition, string urgency, long version) =>
        Write(new { ts = Now(), evt = "thread.announce", surface, ordinal, transition, urgency, version });

    public static void Focus(string surface, string gesture, string from, string to, bool landed, string? errorCode = null) =>
        Write(new { ts = Now(), evt = "thread.focus", surface, gesture, from, to, landed, error_code = errorCode });

    public static void Action(string surface, int ordinal, string action, string? requestId) =>
        Write(new { ts = Now(), evt = "thread.action", surface, ordinal, action, request_id = requestId });

    public static void Error(string surface, string code, string exceptionType, string exceptionMessage, long? expected = null, long? received = null) =>
        Write(new { ts = Now(), evt = "thread.error", surface, error_code = code, exception = new { type = exceptionType, message = exceptionMessage }, expected, received });

    private static string Now() => DateTimeOffset.UtcNow.ToString("O");

    private static void Write(object record)
    {
        string line;
        try { line = JsonSerializer.Serialize(record); }
        catch { return; }

        var sink = WorkbenchDiagnostics.Sink;
        if (sink is not null)
        {
            try { sink(line); } catch { /* a test sink must not break the workbench either */ }
            return;
        }

        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AiDe", "logs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"workbench-{DateTime.UtcNow:yyyyMMdd}.log");
            lock (Gate) { File.AppendAllText(path, line + Environment.NewLine); }
        }
        catch
        {
            // Diagnostics are best-effort; never let a logging failure surface as a workbench failure.
        }
    }
}
