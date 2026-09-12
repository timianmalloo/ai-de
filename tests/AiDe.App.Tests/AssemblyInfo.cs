// Test parallelization is disabled for the same reason as AiDe.Core.Tests, plus one specific to WPF.
//
// These tests create real WPF Windows on dedicated STA threads. Run in parallel, several classes
// show windows and tear down Dispatchers concurrently, and the test host crashes mid-run — which
// presents as tests silently DISAPPEARING from the count rather than as a failure, because the run
// aborts after the passes it already recorded. That is a success-shaped failure: "Passed! 27" with
// 21 tests never executed.
//
// Registered as defect class DC-008 (test-observable global state leaking between parallel classes);
// this is its second instance, which per CI4 means the first control was too narrow — it was applied
// to one test project when the cause was not project-specific.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

/// <summary>
/// The diagnostics sink's default for this assembly: the run's own terminal ledger, never the
/// operator's log. Without a default every fixture pane a test builds writes
/// <c>terminal.start</c>/<c>terminal.stop</c> into the OPERATOR's workbench log — INV-0010 counted
/// 4,115 such lines in one day, most of them fixtures (<c>terminal-1</c>, <c>terminal#…</c>,
/// <c>session-terminal:…deadbeef</c>) — so the operator's log was mostly not the operator's. A test
/// that asserts on the log installs its own sink and restores THIS one.
/// </summary>
/// <remarks>
/// <para><b>Why a file and not nowhere (INV-0011).</b> A hung run on 2026-09-12 kept 32 shells alive
/// for 30 minutes, born over 70 seconds from no test thread, and the default sink of the day —
/// <c>_ => { }</c> — had discarded every <c>terminal.start</c> that would have named the pane and
/// the moment. A measurement path degrades to "not recorded", never to unattributable: the two
/// terminal events, and nothing else, go to
/// <c>%TEMP%ide-tests	erminal-ledger-&lt;pid&gt;.log</c> (or <c>AIDE_TEST_TERMINAL_LEDGER</c>),
/// one line each, so the next report of shells nobody started can be read back to a surface id
/// and a timestamp. Everything else still goes nowhere.</para>
/// </remarks>
internal static class WorkbenchDiagnosticsSinkDefault
{
    private static readonly object Gate = new();
    private static string? _ledger;

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Install() => AiDe.App.Workbench.WorkbenchDiagnostics.Sink ??= Record;

    private static void Record(string line)
    {
        if (!line.Contains("\"evt\":\"terminal.", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            lock (Gate)
            {
                _ledger ??= Environment.GetEnvironmentVariable("AIDE_TEST_TERMINAL_LEDGER")
                    ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "aide-tests", $"terminal-ledger-{Environment.ProcessId}.log");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_ledger)!);
                System.IO.File.AppendAllText(_ledger, line + Environment.NewLine);
            }
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
        {
            // Not recorded is the honest degradation; a ledger must never fail a test.
        }
    }
}
