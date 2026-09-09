using System.Windows;
using System.Windows.Threading;
using AiDe.App.Workbench;

namespace AiDe.App;

/// <summary>
/// Interaction logic for App.xaml — and the one place an unhandled failure is recorded.
/// </summary>
/// <remarks>
/// <para><b>Why this class stopped being empty.</b> The shell crashed on "New Claude Code session"
/// and left no evidence anywhere: no Windows Error Reporting entry, no Application event-log record,
/// and nothing in the workbench log, which recorded only layout mutations. The user could report
/// only that the executable closed, and the investigation had to start from a screenshot of the
/// terminal.</para>
///
/// <para><b>The three routes a .NET UI app can die by</b>, all wired, because catching only the
/// first would leave two silent paths and a false sense that crashes are now recorded:</para>
/// <list type="bullet">
/// <item><description><b>Dispatcher</b> — an exception on the UI thread, which is where a click
/// handler runs.</description></item>
/// <item><description><b>AppDomain</b> — a background thread, which the dispatcher never
/// sees.</description></item>
/// <item><description><b>UnobservedTaskException</b> — a discarded <c>Task</c> whose fault nobody
/// awaited; it arrives at finalization, long after the gesture.</description></item>
/// </list>
///
/// <para><b>The process is still allowed to fail.</b> <c>e.Handled</c> stays false: surviving an
/// unhandled exception would leave the shell running in a state nothing designed for, and a tool
/// that keeps going after an invariant broke tells the user less than one that stops. This changes
/// what is KNOWN about a crash, not whether it happens.</para>
/// </remarks>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // HEADLESS CONDUCTOR MODE — the same App-layer composition root the deferred Conductor
        // Surface will call, reached without a window. Handled BEFORE base.OnStartup because
        // StartupUri is set in App.xaml: letting the base run would show MainWindow, attach a
        // workspace and start a terminal session, which is precisely what a governed run claims not
        // to do — and the claim is counted, so the shell doing it would show up as a non-zero count
        // rather than as a quiet contradiction.
        if (Conductor.ConductorEntry.IsRequested(e?.Args ?? []))
        {
            StartupUri = null;
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _ = RunHeadlessAsync(e!.Args);
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex) WorkbenchDiagnostics.Crash("appdomain", ex);
        };

        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            WorkbenchDiagnostics.Crash("task", args.Exception);

            // Observed so it does not also arrive as an AppDomain crash on a later GC, which would
            // record the same fault twice at two unrelated timestamps.
            args.SetObserved();
        };

        base.OnStartup(e);
    }

    /// <summary>
    /// Drives the headless run and ends the process with its verdict.
    /// </summary>
    /// <remarks>
    /// <b>The exit code is the report.</b> A shell with no console cannot say anything on the way
    /// out, so the run writes its result and transcript as files and the code says whether the
    /// evidence held. Shutting down inside a <c>finally</c> is what stops a failed run from leaving a
    /// windowless process alive under <c>OnExplicitShutdown</c>.
    /// </remarks>
    private async Task RunHeadlessAsync(string[] args)
    {
        var code = 3;
        try
        {
            code = await Conductor.ConductorEntry.RunAsync(args).ConfigureAwait(true);
        }
        catch (Exception error)
        {
            WorkbenchDiagnostics.Crash("conduct", error);
        }
        finally
        {
            Shutdown(code);
        }
    }

    private static void OnDispatcherUnhandledException(
        object sender, DispatcherUnhandledExceptionEventArgs e) =>
        WorkbenchDiagnostics.Crash("dispatcher", e.Exception);
}
