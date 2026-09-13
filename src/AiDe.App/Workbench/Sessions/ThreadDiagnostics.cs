namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// The thread's four records (DS-1 §Telemetry) — <c>thread.layout</c>, <c>thread.announce</c>,
/// <c>thread.focus</c>, <c>thread.action</c> — and its three stable codes: <c>THR-0001</c> the
/// read model's apply failed, <c>THR-0002</c> a region refused focus, <c>THR-0003</c> a version gap.
/// Emitted on the normal path (IO1); no text, ever (O11).
/// </summary>
/// <remarks>
/// <b>One writer.</b> <see cref="WorkbenchDiagnostics.Write"/> is <c>internal</c> — these records go
/// through it rather than a second copy of the sink check and the log file path (DM7's seam
/// request, landed): <c>ThreadDiagnostics</c> composes its own event shape and hands the object
/// straight to the Shell lane's one writer.
/// </remarks>
public static class ThreadDiagnostics
{
    public const string ApplyFailed = "THR-0001";
    public const string FocusRefused = "THR-0002";
    public const string VersionGap = "THR-0003";

    public static void Layout(string surface, int turns, int realized, double? composerTop, double? viewport, double layoutMs, bool pinned, bool moved, long version) =>
        WorkbenchDiagnostics.Write(new { ts = Now(), evt = "thread.layout", surface, turns, realized, composer_top = composerTop, viewport, layout_ms = layoutMs, pinned, moved, version });

    public static void Announce(string surface, int ordinal, string transition, string urgency, long version) =>
        WorkbenchDiagnostics.Write(new { ts = Now(), evt = "thread.announce", surface, ordinal, transition, urgency, version });

    public static void Focus(string surface, string gesture, string from, string to, bool landed, string? errorCode = null) =>
        WorkbenchDiagnostics.Write(new { ts = Now(), evt = "thread.focus", surface, gesture, from, to, landed, error_code = errorCode });

    public static void Action(string surface, int ordinal, string action, string? requestId) =>
        WorkbenchDiagnostics.Write(new { ts = Now(), evt = "thread.action", surface, ordinal, action, request_id = requestId });

    public static void Error(string surface, string code, string exceptionType, string exceptionMessage, long? expected = null, long? received = null) =>
        WorkbenchDiagnostics.Write(new { ts = Now(), evt = "thread.error", surface, error_code = code, exception = new { type = exceptionType, message = exceptionMessage }, expected, received });

    private static string Now() => DateTimeOffset.UtcNow.ToString("O");
}
