using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace AiDe.App.ContrastProbe;

/// <summary>
/// Boots the <b>shipped</b> shell — <see cref="AiDe.App.App"/>, its compiled App.xaml, its
/// <see cref="MainWindow"/> — and takes the contrast census of what it composes.
/// </summary>
/// <remarks>
/// <para><b>The product's own boot, not a rebuild of it.</b> <c>ContrastFloorTests</c> constructs the
/// controls it measures on a window it builds; an in-process census had to re-parse App.xaml and
/// MainWindow.xaml as loose XAML and restate the five composition lines. Both are mirrors, and the
/// day the real thing changes a mirror goes on measuring the other. This runs the generated entry
/// point's three lines — construct, <c>InitializeComponent</c>, <c>Run</c> — and reads the window
/// WPF opens from <c>StartupUri</c>. Nothing about the frame is restated here.</para>
///
/// <para><b>Nothing of the operator's is touched.</b> <c>AIDE_WORKSPACE_ROOT</c> is cleared in this
/// process, so the shell opens its first-run state and reaches no daemon; the session document is
/// opened under a temp directory that is deleted on exit; the window is moved off-screen the moment
/// it exists.</para>
///
/// <para><b>The exit code says whether the census was taken; the report says what it found.</b>
/// The test that launches this reads the JSON and asserts on the rows, so the failing sites land
/// in its assertion message rather than in a log nobody opens (DC-113).</para>
/// </remarks>
internal static class Program
{
    private const int Ok = 0;
    private const int CouldNotTake = 2;
    private const int Crashed = 10;

    private static int _exit = Crashed;

    [STAThread]
    private static int Main(string[] args)
    {
        var reportPath = ReportPath(args);

        try
        {
            // First-run, no daemon, no workspace of the operator's — see the remarks.
            Environment.SetEnvironmentVariable("AIDE_WORKSPACE_ROOT", null);

            // StartupUri is pack://application:,,,/MainWindow.xaml and resolves against the resource
            // assembly, which defaults to THIS exe. Point it at the product before Run.
            PointResourceAssemblyAtTheProduct();

            var app = new AiDe.App.App();
            app.InitializeComponent();
            app.Startup += (_, _) => app.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle, new Action(() => _ = StageAsync(app, reportPath)));

            return app.Run();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return Crashed;
        }
    }

    private static async Task StageAsync(AiDe.App.App app, string reportPath)
    {
        var workspace = Path.Combine(Path.GetTempPath(), "aide-contrast-census-" + Guid.NewGuid().ToString("N"));

        try
        {
            if (app.MainWindow is not MainWindow window)
            {
                throw new InvalidOperationException(
                    "the App opened no MainWindow, so there is no composed shell to walk (DC-016)");
            }

            // Off-screen, now that it exists. StartupUri shows it before any hook of ours can run.
            window.Left = -10000;
            window.Top = -10000;
            window.ShowInTaskbar = false;
            window.UpdateLayout();

            Directory.CreateDirectory(workspace);

            var version = typeof(AiDe.App.App).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? "(no informational version)";

            var report = await ShellContrastCensus.TakeAsync(window, workspace, version);

            File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

            var failing = report.Sites.Count(s => !s.Clears);
            Console.Out.WriteLine($"census taken: {report.Sites.Count} pairings, {failing} below floor, report at {reportPath}");
            _exit = Ok;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            _exit = CouldNotTake;
        }
        finally
        {
            try { Directory.Delete(workspace, recursive: true); } catch (IOException) { }
            app.Shutdown(_exit);
        }
    }

    /// <summary>
    /// Makes <c>pack://application:,,,/</c> resolve into <c>AiDe.App</c>, where MainWindow.xaml lives.
    /// </summary>
    /// <remarks>
    /// <b>Measured, not assumed:</b> by the time <c>Main</c> runs, <see cref="Application.ResourceAssembly"/>
    /// has already been read — WPF's own static initialisation touches it — and the public setter
    /// then refuses any other value ("cannot be changed after it has been set"). The backing field is
    /// the only remaining door. This is a probe, not the product; the product never needs this because
    /// its entry assembly IS the resource assembly.
    /// </remarks>
    private static void PointResourceAssemblyAtTheProduct()
    {
        var product = typeof(AiDe.App.App).Assembly;

        try
        {
            Application.ResourceAssembly = product;
            return;
        }
        catch (InvalidOperationException)
        {
            // Already read and pinned to this exe — fall through to the field.
        }

        var field = typeof(Application).GetField("_resourceAssembly", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Application has no _resourceAssembly field on this WPF build; the probe cannot boot the product's StartupUri");
        field.SetValue(null, product);

        // BaseUriHelper mirrors the value for pack: resolution; the public setter would have written both.
        var helper = typeof(Application).Assembly.GetType("MS.Internal.AppModel.BaseUriHelper", throwOnError: false)
            ?? typeof(System.Windows.Navigation.BaseUriHelper);
        helper.GetProperty("ResourceAssembly", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            ?.SetValue(null, product);

        if (!ReferenceEquals(Application.ResourceAssembly, product))
        {
            throw new InvalidOperationException("Application.ResourceAssembly still is not AiDe.App after the field write");
        }
    }

    private static string ReportPath(string[] args)
    {
        for (var i = 0; i + 1 < args.Length; i++)
        {
            if (string.Equals(args[i], "--report", StringComparison.Ordinal)) return args[i + 1];
        }

        return Path.Combine(Path.GetTempPath(), "aide-contrast-census.json");
    }
}
