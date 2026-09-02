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

    private static void OnDispatcherUnhandledException(
        object sender, DispatcherUnhandledExceptionEventArgs e) =>
        WorkbenchDiagnostics.Crash("dispatcher", e.Exception);
}
