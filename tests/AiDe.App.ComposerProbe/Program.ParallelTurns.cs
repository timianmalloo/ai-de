using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;

namespace AiDe.App.ComposerProbe;

internal static partial class Program
{
    /// <summary>Runs Ruling 95 condition 1's measurement: two sessions on one workspace, one turn each, concurrently.</summary>
    private const string ParallelTurnsArgument = "--parallel-turns";

    // --parallel-turns only. Each is a different way "two sessions ran in parallel" fails to be
    // measured, so the proof doc can name which.
    private const int TheProviderFileIsAbsentOrRefuses = 40;
    private const int ASendWasRefused = 41;
    private const int ARunNeverConcluded = 42;

    /// <summary>
    /// <b>Ruling 95 condition 1 — measure first.</b> Two <see cref="SessionDocumentSurface"/>s on the
    /// same workspace, each composer bound to the machine's own provider file, each sent one
    /// read-only prompt within the same second through the product's own composition root
    /// (<c>SessionDocumentSurface.Launch</c> → <c>GovernedRunHost.RunAsync</c>). Records: both
    /// <c>lane.session-new</c> rows (from the diagnostics sink, with their timestamps), the
    /// <c>node</c> process census sampled every 250 ms while either run is open, both engine pids,
    /// and both outcome lines — so "the run host serialises them" is a reading, not a belief.
    /// </summary>
    /// <remarks>
    /// <b>Harness-wired, and said so.</b> This probe calls <c>Composer.Configure</c> itself with a
    /// context built from <c>~/.aide/providers.json</c> exactly as <c>SessionComposerBinder</c>
    /// builds one; the shell's binder is not on this path (the F5 pack's caveat). What is product-
    /// wired is everything from <c>Send()</c> onward.
    /// </remarks>
    private static class ParallelTurns
    {
        private const string Prompt = "reply with the single word ok";

        internal static int Run(string[] args)
        {
            var providers = ProviderConfiguration.ReadIfPresent(ProviderConfiguration.DefaultPath);
            if (providers is null)
            {
                Console.Out.WriteLine($"measure: no provider file at {ProviderConfiguration.DefaultPath}");
                return TheProviderFileIsAbsentOrRefuses;
            }

            var binding = providers.Bind("claude-code", out var refusal);
            if (binding is null)
            {
                Console.Out.WriteLine($"measure: provider binding refused — {refusal!.Field}: {refusal.Message}");
                return TheProviderFileIsAbsentOrRefuses;
            }

            var root = Path.Combine(Path.GetTempPath(), "aide-parallel-turns", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            GitInit(root);

            var window = new Window
            {
                Title = "AiDe parallel-turns measurement",
                Width = 1280,
                Height = 800,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                ShowActivated = false,
            };

            var result = Crashed;
            window.Loaded += async (_, _) =>
            {
                try
                {
                    result = await MeasureAsync(window, root, providers, binding);
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                    result = Crashed;
                }

                window.Close();
            };

            window.Show();

            var frame = new DispatcherFrame();
            window.Closed += (_, _) => frame.Continue = false;
            var guard = new DispatcherTimer(
                TimeSpan.FromSeconds(300), DispatcherPriority.Normal,
                (_, _) => { frame.Continue = false; }, Dispatcher.CurrentDispatcher);
            guard.Start();
            Dispatcher.PushFrame(frame);
            guard.Stop();
            return result;
        }

        private static async Task<int> MeasureAsync(Window window, string root, ProviderConfiguration providers, LaneBinding binding)
        {
            var panel = new StackPanel();
            window.Content = panel;

            var a = Open(panel, root, providers, binding, "parallel-a");
            var b = Open(panel, root, providers, binding, "parallel-b");
            var documents = new[] { a, b };

            var census = new List<(DateTimeOffset At, int[] NodePids)>();
            var baseline = NodePids();
            Console.Out.WriteLine($"measure: node baseline {baseline.Length} process(es) at {Now()}");

            // Both gestures within the same second, through the one public verb the button and the
            // accelerator call. A refusal here is the composer's own sentence.
            foreach (var document in documents)
            {
                document.Composer.Draft.SetFreeFormText(Prompt);
                var sentAt = Now();
                var request = document.Composer.Send();
                Console.Out.WriteLine($"measure: {document.Model.SessionId} send at {sentAt} → {(request is null ? "REFUSED: " + document.Composer.Status : "request built; prompt sha " + Sha(request.Prompt))}");
                if (request is null)
                {
                    return ASendWasRefused;
                }
            }

            // Sample the node census every 250 ms while either run is open, on the dispatcher, so
            // the lanes' Marshal(Dispatcher.Invoke) never waits on a blocked UI thread.
            var deadline = DateTimeOffset.UtcNow.AddSeconds(240);
            while (documents.Any(d => !d.LastLaunch.IsCompleted) && DateTimeOffset.UtcNow < deadline)
            {
                census.Add((DateTimeOffset.UtcNow, NodePids()));
                await Task.Delay(250);
            }

            if (documents.Any(d => !d.LastLaunch.IsCompleted))
            {
                Console.Out.WriteLine("measure: a run never concluded within 240 s");
                return ARunNeverConcluded;
            }

            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);

            var pids = new List<int>();
            foreach (var document in documents)
            {
                var turn = document.ReadModel.Current.Turns.Single();
                var row = document.Thread.Rows.Single();
                var run = document.LastRunResult;
                var pid = run?.EngineProcessId ?? -1;
                pids.Add(pid);
                Console.Out.WriteLine(
                    $"measure: {document.Model.SessionId} outcome: {row.OutcomeWord} · {row.Counts} · state {turn.State} · at {turn.At.ToUniversalTime():O} · concluded +{turn.Outcome?.Duration?.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture) ?? "not recorded"} s"
                    + $" · engine pid {pid} · exited {run?.EngineExited} · run {run?.RunId} · outcome {run?.Outcome} · failure {document.LastRunFailure ?? "none"}");
                foreach (var line in run?.Diagnostics ?? [])
                {
                    if (line.Contains("session/new", StringComparison.Ordinal) || line.Contains("engine pid", StringComparison.Ordinal) || line.Contains("stopReason", StringComparison.Ordinal) || line.Contains("prompt refused", StringComparison.Ordinal))
                    {
                        Console.Out.WriteLine($"measure:   {document.Model.SessionId} diag: {line}");
                    }
                }
            }

            var overlap = census.Where(s => pids.All(p => p > 0 && s.NodePids.Contains(p))).ToList();
            Console.Out.WriteLine($"measure: census samples {census.Count}; samples with BOTH engine pids alive {overlap.Count}"
                + (overlap.Count > 0 ? $" (first {overlap[0].At:O}, last {overlap[^1].At:O})" : string.Empty));
            var peak = census.Count == 0 ? 0 : census.Max(s => s.NodePids.Length);
            Console.Out.WriteLine($"measure: node peak {peak} process(es) (baseline {baseline.Length}, delta {peak - baseline.Length})");

            foreach (var document in documents)
            {
                document.Dispose();
            }

            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // git's read-only object files, or a lingering handle: the temp root is reaped later,
                // and a cleanup failure must not replace the measurement's exit code.
                Console.Out.WriteLine($"measure: temp root not deleted ({error.GetType().Name}); {root}");
            }
            return Ok;
        }

        private static SessionDocumentSurface Open(Panel panel, string root, ProviderConfiguration providers, LaneBinding binding, string name)
        {
            var config = new SessionConfigStore(root, SessionId.New(DateTimeOffset.UtcNow)).Create(name, root, ["claude-code"], DateTimeOffset.UtcNow);
            var model = new SessionDocumentViewModel(config.SessionId, config.Name, root, [CanvasModeCatalog.ConsoleModeId]);
            var document = new SessionDocumentSurface(model) { Height = 380 };
            panel.Children.Add(document);

            // The binder's context, field for field (SessionComposerBinder.Bind) — harness-wired.
            document.Composer.Configure(
                config,
                new ComposerSendContext(
                    RepositoryRoot: root,
                    DataDirectory: Path.Combine(root, ".aide"),
                    AdapterInstallRoot: providers.AdapterInstallRoot,
                    EngineId: binding.EngineId,
                    Model: binding.Model,
                    AccountLabel: binding.Account.Label,
                    TaskClass: config.DefaultTaskClass,
                    ProofPackArtifacts: [],
                    Providers: providers.Registry.Rows),
                ComposerFields.FreeForm(),
                new AttachmentGate(root, new AttachmentFileReader(), new NeverAffirm(), binding.Provider.ProviderId, binding.Account.Label));
            document.Composer.Draft.SwitchTo(ComposerShape.FreeForm);
            return document;
        }

        private sealed class NeverAffirm : IAttachmentAffirmation
        {
            public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
        }

        private static int[] NodePids()
        {
            var processes = Process.GetProcessesByName("node");
            try
            {
                return [.. processes.Select(p => p.Id)];
            }
            finally
            {
                foreach (var p in processes)
                {
                    p.Dispose();
                }
            }
        }

        private static void GitInit(string root)
        {
            foreach (var arguments in new[] { "init -q", "config user.email probe@aide.local", "config user.name probe", "commit -q --allow-empty -m baseline" })
            {
                using var git = Process.Start(new ProcessStartInfo("git", arguments) { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true });
                git?.WaitForExit();
            }
        }

        private static string Now() => DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        private static string Sha(string text) =>
            Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)))[..12];
    }
}
