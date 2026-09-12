using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Workbench;
using Microsoft.Web.WebView2.Wpf;

namespace AiDe.App.ComposerProbe;

internal static partial class Program
{
    /// <summary>
    /// <b>INV-0009.</b> The operator's 22:33Z launch, replayed step by step in the product's own
    /// docking host, under the product's own <see cref="ShellModeController"/>, from the arrangement
    /// the operator's workbench log recorded — and then <c>File → New Session</c>'s choreography, with
    /// the one question the log could not answer asked of WPF directly: <b>did the new document's
    /// composer ever enter a rendered visual tree?</b>
    /// </summary>
    /// <remarks>
    /// <para><b>Why a replay and not a fresh shell.</b> Three times on 2026-09-11 a harness passed
    /// against a state the product was not in (DC-135). The operator's log carries the state: a
    /// workspace-open restore whose zone payload is quoted here verbatim, a session document opened
    /// before that restore and dropped by it, and — the line that names the mechanism — an
    /// <c>explorer-graph</c> surface initialising nine seconds before the New Session that showed
    /// nothing. Each step is optional so the run that omits one is the control for the one that
    /// includes it.</para>
    ///
    /// <para><b>Measured, never inferred.</b> WPF's own <c>Loaded</c> on the composer, the product's
    /// own handshake transitions on stdout, the mode controller's own state, and the page's own field
    /// count. The exit code is the first verdict that failed; every measurement is printed whether
    /// or not it failed.</para>
    /// </remarks>
    private static class SessionRender
    {
        /// <param name="PriorDocument">Replay 22:33:28Z — a session document opened with no workspace in the window (its composer refused, never configured), then dropped by the restore.</param>
        /// <param name="Explorer">Replay 22:34:00Z — enter Explorer mode through the product's controller before New Session.</param>
        /// <param name="ReturnToWorkbench">After New Session, leave Explorer mode and measure again (the necessity half).</param>
        /// <param name="Reopen">Run the reopen choreography (MainWindow.ReopenSessionAsync's one shell call) instead of New Session.</param>
        /// <param name="Sibling">In the same state, open a code viewer — a sibling dock document — and measure whether it loads.</param>
        /// <param name="BindOnRestore">Run the workspace-open choreography for restored session documents (MainWindow.AttachWorkspace's revive-and-bind) instead of New Session.</param>
        /// <param name="Chooser">Run File → New Session with NO workspace open — the chooser interposes — through the product's NewSessionFlow, then a second run with the chooser cancelled.</param>
        /// <param name="WindowHeight">The window's height; 720 is the operator's.</param>
        private sealed record Options(
            bool PriorDocument, bool Explorer, bool ReturnToWorkbench, bool Reopen, bool Sibling, bool BindOnRestore, bool Chooser, double WindowHeight);

        /// <summary>
        /// The <c>layout.mutation</c> line the operator's shell wrote at 2026-09-11T22:33:53.717Z
        /// (<c>docs/investigations/operator-launch-22-33Z.log.jsonl</c>, line 14), verbatim: the
        /// arrangement <c>workspace-open</c> restored. Two session documents from earlier in the day,
        /// the second of them the active tab; graph, domain, explore and sessions on the left; one
        /// terminal at the bottom; nothing on the right.
        /// </summary>
        private const string RestoredArrangementPayload =
            """{"ts":"2026-09-11T22:33:53.7177680+00:00","evt":"layout.mutation","operation":"workspace-open","placement":"restore-zones","surface":"layout","active":null,"stacks":[{"id":"zone-left","active":0,"surfaces":["graph:canvas","domain:view","explore:view","sessions:sessions"]},{"id":"zone-center","active":7,"surfaces":["ledger:ledger","leaderboard:leaderboard","board:board","session-document:20260911T125502Z-bd59855b:session-document","provenance:inspector","contexts:contexts","joins:joins","session-document:20260911T175821Z-1edfa710:session-document"]},{"id":"zone-bottom","active":0,"surfaces":["terminal#b356ea:terminal"]}]}""";

        /// <summary>The restored layout's active document — the one the reopen replay reopens.</summary>
        private const string RestoredActiveSessionId = "20260911T175821Z-1edfa710";

        private static readonly List<string> Lines = [];

        /// <summary>How many handshake lines for <paramref name="surfaceId"/> carry <paramref name="transition"/>.</summary>
        private static int Count(string surfaceId, string transition)
        {
            lock (Lines)
            {
                return Lines.Count(l =>
                    l.Contains("\"evt\":\"web-surface.handshake\"", StringComparison.Ordinal)
                    && l.Contains($"\"surface\":\"{surfaceId}\"", StringComparison.Ordinal)
                    && l.Contains($"\"transition\":\"{transition}\"", StringComparison.Ordinal));
            }
        }

        /// <summary>The trigger on the last <c>shell.mode</c> line, or <c>(none)</c> — the product's own record of why the body changed.</summary>
        private static string LastModeTrigger()
        {
            lock (Lines)
            {
                var last = Lines.LastOrDefault(l => l.Contains("\"evt\":\"shell.mode\"", StringComparison.Ordinal));
                if (last is null) { return "(none)"; }

                using var document = JsonDocument.Parse(last);
                return document.RootElement.GetProperty("trigger").GetString() ?? "(none)";
            }
        }

        /// <summary>How many <c>composer.layout</c> lines the surface wrote — one per measure pass.</summary>
        private static int LayoutLines(string surfaceId)
        {
            lock (Lines)
            {
                return Lines.Count(l =>
                    l.Contains("\"evt\":\"composer.layout\"", StringComparison.Ordinal)
                    && l.Contains($"\"surface\":\"{surfaceId}\"", StringComparison.Ordinal));
            }
        }

        internal static int Run(string[] args)
        {
            var options = new Options(
                PriorDocument: args.Contains("--prior-document", StringComparer.Ordinal),
                Explorer: args.Contains("--explorer", StringComparer.Ordinal),
                ReturnToWorkbench: args.Contains("--return-to-workbench", StringComparer.Ordinal),
                Reopen: args.Contains("--reopen", StringComparer.Ordinal),
                Sibling: args.Contains("--sibling", StringComparer.Ordinal),
                BindOnRestore: args.Contains("--bind-on-restore", StringComparer.Ordinal),
                Chooser: args.Contains("--chooser", StringComparer.Ordinal),
                WindowHeight: Height(args));

            var echo = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = line =>
            {
                lock (Lines) { Lines.Add(line); }
                echo?.Invoke(line);
            };

            var root = Path.Combine(Path.GetTempPath(), "aide-session-render", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            // THE PRODUCT'S COMPOSITION ROOT, from MainWindow's constructor: the shell; its docking
            // host as the body, held by a ContentControl; the dark dock theme; the key bindings; and
            // the mode controller over that same ContentControl, with the same Explorer factory.
            var shell = new WorkbenchShell(null);
            var body = new ContentControl();
            body.Content = shell.WorkbenchRoot;
            var window = new Window
            {
                Title = "AiDe session-render replay (INV-0009)",
                Content = body,
                Width = 1180,
                Height = options.WindowHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };
            shell.Manager.Theme = new AvalonDock.Themes.Vs2013DarkTheme();
            DockThemeAccents.Retokenise(shell.Manager);
            DockRoundedTabs.Apply(shell.Manager);
            shell.Bind(window);

            var mode = new ShellModeController(
                body,
                shell.WorkbenchRoot,
                () => new ExplorerSurface(shell.CreateExplorerGraph(), new NodeReaderView()));

            // MainWindow's one line for INV-0009's seam, verbatim: a document opens into a body
            // that is on screen. EveryOpeningCommandPassesThroughTheSeamTests asserts the product
            // and this replay carry the same wiring, so the replay cannot pass on a line the window
            // does not have (DC-135).
            shell.DocumentOpening += () => mode.Set(ShellViewMode.Workbench, "document-opening");

            var result = Crashed;
            window.Loaded += async (_, _) =>
            {
                try
                {
                    result = await ReplayAsync(window, shell, mode, root, options);
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                    result = Crashed;
                }

                window.Close();
            };

            window.Show();
            SetForegroundWindow(new WindowInteropHelper(window).Handle);

            var frame = new DispatcherFrame();
            window.Closed += (_, _) => frame.Continue = false;

            var guard = new DispatcherTimer(
                TimeSpan.FromSeconds(180), DispatcherPriority.Normal,
                (_, _) => { frame.Continue = false; }, Dispatcher.CurrentDispatcher);
            guard.Start();

            Dispatcher.PushFrame(frame);
            guard.Stop();
            shell.Dispose();
            try { Directory.Delete(root, recursive: true); } catch (IOException) { }
            return result;
        }

        private static double Height(string[] args)
        {
            var at = Array.IndexOf(args, "--height");
            return at >= 0 && at + 1 < args.Length
                && double.TryParse(args[at + 1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var height)
                ? height
                : 720;
        }

        private static async Task<int> ReplayAsync(
            Window window, WorkbenchShell shell, ShellModeController mode, string root, Options options)
        {
            Console.Out.WriteLine(
                $"options: priorDocument={options.PriorDocument} explorer={options.Explorer} returnToWorkbench={options.ReturnToWorkbench} "
                + $"reopen={options.Reopen} sibling={options.Sibling} bindOnRestore={options.BindOnRestore} chooser={options.Chooser} windowHeight={options.WindowHeight}");

            await Task.Delay(800);

            // ---- 22:33:28Z. File -> New Session with no workspace open in the window. ----
            // MainWindow.NewSession's `opened` callback ran OpenSessionDocument, then BindComposer
            // refused at its first guard ("this window has no open workspace") and Configure never
            // ran, then GiveTheNewSessionTheWholeTree maximized the document's zone.
            if (options.PriorDocument)
            {
                var now = DateTimeOffset.UtcNow;
                var prior = new SessionConfigStore(root, SessionId.New(now)).Create("prior session", root, ["claude-code"], now);
                var said = shell.OpenSessionDocument(prior);
                var priorComposer = shell.SessionComposer(prior.SessionId)
                    ?? throw new InvalidOperationException("the shell opened the prior document but holds no composer for it");
                priorComposer.ShowFieldRefusal(
                    "repositoryRoot", "this window has no open workspace, so a run has no checkout to cut a worktree from");
                said = NewSessionPlacement.GiveItTheWholeTree(shell.Service, prior.SessionId, said);
                shell.Adapter.Render();

                await WaitAsync(() => Count(priorComposer.SurfaceId, "page-ready") >= 1, TimeSpan.FromSeconds(30));
                await Task.Delay(500);
                Console.Out.WriteLine(
                    $"prior document (22:33:28Z replay): announced='{said}' initialising={Count(priorComposer.SurfaceId, "initialising")} "
                    + $"page-ready={Count(priorComposer.SurfaceId, "page-ready")} configured={Count(priorComposer.SurfaceId, "configured")} "
                    + $"init-pushed={Count(priorComposer.SurfaceId, "init-pushed")} layout-lines={LayoutLines(priorComposer.SurfaceId)} "
                    + $"status='{priorComposer.Status}' zones={Shape(shell)}");
            }

            // ---- 22:33:53Z. workspace-open restores the saved arrangement. ----
            // The product's own restore: ZoneLayoutStore writes the file, LayoutPersistence.Restore
            // reads it back over the shell's real service and RestoreZones replaces the arrangement.
            var arrangement = ArrangementFromTheOperatorsLog();
            var dataDirectory = Path.Combine(root, "shell-state");
            Directory.CreateDirectory(dataDirectory);
            var layoutPath = Path.Combine(dataDirectory, "layout.json");
            new ZoneLayoutStore(Path.Combine(dataDirectory, "layout.zones.json")).Save(arrangement);

            var available = shell.Service.Current.AllStacks()
                .SelectMany(s => s.Surfaces).Select(s => s.SurfaceId)
                .ToHashSet(StringComparer.Ordinal);
            var persistence = new LayoutPersistence(
                shell.Service, layoutPath, available,
                restorableKinds: SurfaceContentFactory.KnownKinds.ToHashSet(StringComparer.Ordinal));
            var restore = persistence.Restore();
            shell.Adapter.Render();
            await Task.Delay(1500);

            var restoredShape = Shape(shell);
            var expectedShape = Shape(arrangement);
            Console.Out.WriteLine(
                $"restore (22:33:53Z replay): applied-saved={persistence.LastRestoreAppliedASavedArrangement} announced='{restore.Announcement}' "
                + $"zones={restoredShape}");

            if (!persistence.LastRestoreAppliedASavedArrangement || !string.Equals(restoredShape, expectedShape, StringComparison.Ordinal))
            {
                Console.Error.WriteLine(
                    $"the replay did not reach the operator's arrangement: expected {expectedShape} but the shell holds {restoredShape}");
                persistence.Dispose();
                return TheReplayDidNotReachTheOperatorsState;
            }

            // What the restored layout's ACTIVE centre tab renders — a session-document surface with
            // no live session behind it. Printed because the operator read it before New Session.
            var restoredActiveId = SessionDocumentSurface.SurfaceIdFor(RestoredActiveSessionId);
            var restoredActiveText = FirstText(shell.Adapter.ContentFor(restoredActiveId));
            Console.Out.WriteLine(
                $"restored active document: surface={restoredActiveId} live-document={shell.Adapter.SurfaceContent<SessionDocumentSurface>(restoredActiveId) is not null} "
                + $"renders='{restoredActiveText}'");

            // ---- 22:34:00Z. explorer-graph initialising: Explorer mode entered. ----
            if (options.Explorer)
            {
                mode.Set(ShellViewMode.Explorer, "replay-22:34:00Z");
                await WaitAsync(() => Count("explorer-graph", "initialising") >= 1, TimeSpan.FromSeconds(20));
                await Task.Delay(500);
                Console.Out.WriteLine(
                    $"explorer (22:34:00Z replay): mode={mode.Mode} explorer-graph initialising={Count("explorer-graph", "initialising")} "
                    + $"workbench root loaded={shell.WorkbenchRoot.IsLoaded} visible={shell.WorkbenchRoot.IsVisible} parent={shell.WorkbenchRoot.Parent?.GetType().Name ?? "(none)"}");
            }

            var verdict = options.Reopen ? await ReopenAsync(shell, root)
                : options.BindOnRestore ? await BindOnRestoreAsync(shell, root)
                : options.Chooser ? await ChooserAsync(shell, root)
                : await NewSessionAsync(window, shell, mode, root, options);

            persistence.Dispose();
            return verdict;
        }

        /// <summary>
        /// 22:34:09Z. <c>MainWindow.NewSession</c>'s <c>opened</c> callback, in its order: the shell
        /// opens the document; <c>BindComposer</c> configures its composer; the pane takes the tree.
        /// </summary>
        private static async Task<int> NewSessionAsync(
            Window window, WorkbenchShell shell, ShellModeController mode, string root, Options options)
        {
            var now = DateTimeOffset.UtcNow;
            var config = new SessionConfigStore(root, SessionId.New(now)).Create("probe session", root, ["claude-code"], now);

            var graphReattachedBefore = Count("graph", "re-attached");
            var opened = shell.OpenSessionDocument(config);

            var composer = shell.SessionComposer(config.SessionId)
                ?? throw new InvalidOperationException("the shell opened the document but holds no composer for it");
            var loaded = 0;
            var unloaded = 0;
            composer.Loaded += (_, _) => loaded++;
            composer.Unloaded += (_, _) => unloaded++;

            composer.Draft.SwitchTo(ComposerShape.GoalBlock);
            composer.Configure(
                config,
                new ComposerSendContext(
                    RepositoryRoot: root, DataDirectory: root, AdapterInstallRoot: root,
                    EngineId: "claude-code", Model: "probe-model", AccountLabel: "probe-account",
                    TaskClass: "probe", ProofPackArtifacts: [], Providers: []),
                ComposerFields.GoalBlock(),
                new AttachmentGate(root, new AttachmentFileReader(), new NeverAffirms(), "anthropic", "probe-account"));

            var said = NewSessionPlacement.GiveItTheWholeTree(
                shell.Service, config.SessionId, opened + " Composer bound to claude-code · probe-model · probe-account.");
            shell.Adapter.Render();
            Console.Out.WriteLine($"new session (22:34:09Z replay): announced='{said}'");
            Console.Out.WriteLine($"new session: zones after the maximize={Shape(shell)}");

            window.UpdateLayout();
            await WaitAsync(() => Count(composer.SurfaceId, "init-pushed") >= 1, TimeSpan.FromSeconds(20));
            await Task.Delay(1000);

            var report = await Report("after New Session", shell, mode, composer, loaded, unloaded, graphReattachedBefore);
            Console.Out.WriteLine(report.Line);

            if (options.Sibling)
            {
                // THE CLASS, NOT THE INSTANCE: any dock document opened by a catalog command in the
                // same state. A code viewer is a plain WPF control — no browser — so its Loaded is
                // the whole question.
                var before = shell.Service.Current.AllStacks().SelectMany(s => s.Surfaces).Select(s => s.SurfaceId).ToHashSet(StringComparer.Ordinal);
                shell.Controller.Execute("workbench.newCodeViewer");
                var siblingId = shell.Service.Current.AllStacks().SelectMany(s => s.Surfaces)
                    .Select(s => s.SurfaceId).FirstOrDefault(id => !before.Contains(id));
                var sibling = siblingId is null ? null : shell.Adapter.ContentFor(siblingId);
                var siblingLoaded = 0;
                if (sibling is not null) { sibling.Loaded += (_, _) => siblingLoaded++; }
                window.UpdateLayout();
                await Task.Delay(1500);
                Console.Out.WriteLine(
                    $"sibling (code viewer, same state): surface={siblingId ?? "(none)"} in-layout={siblingId is not null} "
                    + $"content={sibling?.GetType().Name ?? "(none)"} wpf loaded={siblingLoaded} isLoaded={sibling?.IsLoaded ?? false} isVisible={sibling?.IsVisible ?? false}");
            }

            if (options.ReturnToWorkbench)
            {
                // THE NECESSITY HALF: remove the suspected cause and see whether the failure goes.
                mode.Set(ShellViewMode.Workbench, "replay-return-to-workbench");
                window.UpdateLayout();
                await WaitAsync(() => Count(composer.SurfaceId, "init-pushed") >= 1, TimeSpan.FromSeconds(30));
                await Task.Delay(1500);
                report = await Report("after returning to the workbench", shell, mode, composer, loaded, unloaded, graphReattachedBefore);
                Console.Out.WriteLine(report.Line);
            }

            if (report.Loaded == 0 || report.Initialising == 0)
            {
                Console.Error.WriteLine(
                    "the new session's composer never entered a rendered visual tree: WPF raised no Loaded on it and "
                    + "its browser host never initialised, so the operator saw nothing of the session the shell announced");
                return TheNewDocumentNeverLoaded;
            }

            if (report.InitPushed == 0 || report.Fields != "6")
            {
                Console.Error.WriteLine(
                    $"the new session's composer loaded but never reached init-pushed with six fields (init-pushed={report.InitPushed}, fields={report.Fields})");
                return TheNewDocumentNeverPushedInit;
            }

            Console.Out.WriteLine("the new session's composer entered the rendered tree, measured itself and mounted six fields");
            return Ok;
        }

        /// <summary>
        /// A provider file with one ready account for <c>claude-code</c>, as the operator's
        /// <c>~/.aide/providers.json</c> would carry it — written to the run's own directory, never
        /// read from the machine's.
        /// </summary>
        private const string ProviderFile = """
            {
              "adapterInstallRoot": "C:/adapters/probe",
              "providers": {
                "anthropic": { "auth": "subscription", "accounts": [
                  { "label": "probe-account", "health": "ready" } ] }
              },
              "engines": { "claude-code": { "model": "probe-model" } }
            }
            """;

        private static ProviderConfiguration WriteProviderFile(string root)
        {
            var path = Path.Combine(root, "providers.json");
            File.WriteAllText(path, ProviderFile);
            return ProviderConfiguration.Read(path);
        }

        /// <summary>
        /// <c>MainWindow.ReopenSessionAsync</c>'s shell half, as it stands after INV-0009 Phase 2:
        /// <c>session.json</c> read back through <c>SessionConfigStore.Load</c>, the one call
        /// <c>Shell.OpenSessionDocument(config)</c>, then the product's own binder over the config's
        /// enabled backends and a provider file. Before Phase 2 nothing bound the composer on this
        /// path and the restored pane kept its island; this measures what the path leaves on screen.
        /// </summary>
        private static async Task<int> ReopenAsync(WorkbenchShell shell, string root)
        {
            var now = DateTimeOffset.UtcNow;
            var store = new SessionConfigStore(root, RestoredActiveSessionId);
            store.Create("Terrace session", root, ["claude-code"], now);
            var config = store.Load();
            var providers = WriteProviderFile(root);

            var said = shell.OpenSessionDocument(config);
            var composer = shell.SessionComposer(config.SessionId)
                ?? throw new InvalidOperationException("the shell reopened the document but holds no composer for it");
            said += " " + SessionComposerBinder.Bind(
                shell, config,
                NewSessionSheetViewModel.RoutableAmong(config.EnabledBackends, providers.Registry),
                taskClass: null,
                repositoryRoot: root, dataDirectory: root,
                providers, new NeverAffirms());
            Console.Out.WriteLine($"reopen: announced='{said}' zones={Shape(shell)}");

            // What the pane renders now that a live document is registered for its surface: the
            // island the restore built, or the document the reopen registered.
            var documentId = SessionDocumentSurface.SurfaceIdFor(config.SessionId);
            var shown = shell.Adapter.SurfaceContent<SessionDocumentSurface>(documentId) is not null;
            Console.Out.WriteLine(
                $"reopen: pane content={shell.Adapter.ContentFor(documentId)?.GetType().Name ?? "(none)"} "
                + $"live-document-in-view={shown} "
                + $"renders='{FirstText(shell.Adapter.ContentFor(documentId))}' active in view={shell.Adapter.ActiveSurfaceId ?? "(none)"}");

            await WaitAsync(() => Count(composer.SurfaceId, "page-ready") >= 1, TimeSpan.FromSeconds(30));
            await Task.Delay(2000);

            var view = FindWebView(composer);
            var fields = view?.CoreWebView2 is null ? "(no page)" : await Eval(view, "String(document.querySelectorAll('#fields .field').length)");
            Console.Out.WriteLine(
                $"reopen: composer initialising={Count(composer.SurfaceId, "initialising")} page-ready={Count(composer.SurfaceId, "page-ready")} "
                + $"configured={Count(composer.SurfaceId, "configured")} init-pushed={Count(composer.SurfaceId, "init-pushed")} "
                + $"layout-lines={LayoutLines(composer.SurfaceId)} page fields={fields} host fields={composer.Fields.Count} status='{composer.Status}'");

            if (!shown || Count(composer.SurfaceId, "configured") == 0)
            {
                Console.Error.WriteLine(
                    $"the reopened session was not shown as a bound document: live document in the view={shown} "
                    + $"(the pane renders '{FirstText(shell.Adapter.ContentFor(documentId))}'), composer configured="
                    + $"{Count(composer.SurfaceId, "configured")} — nothing on the reopen path rebuilds a restored pane's content or binds its composer");
                return TheReopenedDocumentWasNeverConfigured;
            }

            if (Count(composer.SurfaceId, "init-pushed") == 0 || fields != "6")
            {
                Console.Error.WriteLine($"the reopened session's composer was configured but never reached init-pushed with six fields (fields={fields})");
                return TheNewDocumentNeverPushedInit;
            }

            Console.Out.WriteLine("the reopened session's composer was configured and mounted six fields");
            return Ok;
        }

        /// <summary>
        /// <c>MainWindow.AttachWorkspace</c>'s second half, as it stands after INV-0009 Phase 2b: the
        /// restored arrangement is on screen with its two session-document surfaces; one of them has a
        /// <c>session.json</c> in the workspace, the other does not. The shell revives what loads,
        /// the panes are rendered, and the product's binder binds each revived session. Measured: the
        /// restored active tab is a live, configured document, and the session that is gone keeps
        /// its island.
        /// </summary>
        private static async Task<int> BindOnRestoreAsync(WorkbenchShell shell, string root)
        {
            var now = DateTimeOffset.UtcNow;
            new SessionConfigStore(root, RestoredActiveSessionId).Create("Terrace session", root, ["claude-code"], now);
            var providers = WriteProviderFile(root);

            var revived = shell.ReviveRestoredSessionDocuments(root);
            shell.Adapter.Render();
            var bound = string.Join(" | ", revived.Select(config => SessionComposerBinder.Bind(
                shell, config,
                NewSessionSheetViewModel.RoutableAmong(config.EnabledBackends, providers.Registry),
                taskClass: null,
                repositoryRoot: root, dataDirectory: root,
                providers, new NeverAffirms())));
            Console.Out.WriteLine(
                $"bind-on-restore: revived=[{string.Join(",", revived.Select(c => c.SessionId))}] bound='{bound}' zones={Shape(shell)}");

            var activeId = SessionDocumentSurface.SurfaceIdFor(RestoredActiveSessionId);
            var goneId = SessionDocumentSurface.SurfaceIdFor("20260911T125502Z-bd59855b");
            var live = shell.Adapter.SurfaceContent<SessionDocumentSurface>(activeId) is not null;
            var goneLive = shell.Adapter.SurfaceContent<SessionDocumentSurface>(goneId) is not null;
            Console.Out.WriteLine(
                $"bind-on-restore: active document live={live} renders='{FirstText(shell.Adapter.ContentFor(activeId))}' "
                + $"active in view={shell.Adapter.ActiveSurfaceId ?? "(none)"}; gone document live={goneLive} renders='{FirstText(shell.Adapter.ContentFor(goneId))}'");

            var composer = shell.SessionComposer(RestoredActiveSessionId);
            if (composer is null)
            {
                Console.Error.WriteLine("the restored active session's document was not revived: the shell holds no composer for it");
                return TheRestoredDocumentWasNotRevived;
            }

            await WaitAsync(() => Count(composer.SurfaceId, "init-pushed") >= 1, TimeSpan.FromSeconds(30));
            await Task.Delay(1000);

            var view = FindWebView(composer);
            var fields = view?.CoreWebView2 is null ? "(no page)" : await Eval(view, "String(document.querySelectorAll('#fields .field').length)");
            Console.Out.WriteLine(
                $"bind-on-restore: composer initialising={Count(composer.SurfaceId, "initialising")} page-ready={Count(composer.SurfaceId, "page-ready")} "
                + $"configured={Count(composer.SurfaceId, "configured")} init-pushed={Count(composer.SurfaceId, "init-pushed")} "
                + $"layout-lines={LayoutLines(composer.SurfaceId)} page fields={fields} host fields={composer.Fields.Count} status='{composer.Status}'");

            if (!live || goneLive || Count(composer.SurfaceId, "configured") == 0 || Count(composer.SurfaceId, "init-pushed") == 0 || fields != "6")
            {
                Console.Error.WriteLine(
                    $"the restored session document was not revived as a bound document: live={live} gone-live={goneLive} "
                    + $"configured={Count(composer.SurfaceId, "configured")} init-pushed={Count(composer.SurfaceId, "init-pushed")} fields={fields}");
                return TheRestoredDocumentWasNotRevived;
            }

            Console.Out.WriteLine("the restored session document is live, its composer bound and mounted six fields; the session that is gone kept its island");
            return Ok;
        }

        /// <summary>
        /// <c>MainWindow.NewSessionAsync</c> with no workspace open, as it stands after INV-0009 Phase 3:
        /// the product's <see cref="NewSessionFlow"/>, whose chooser answers with a workspace, whose
        /// <c>openWorkspace</c> is the window's open path — stood in for by setting what the window
        /// reports, since the real path reaches a daemon — and whose <c>opened</c> callback is the
        /// window's: open the document, bind through the product's binder with the window's root,
        /// give it the tree. Then the same flow with the chooser cancelled. Measured: the composer
        /// binds to the CHOSEN root and reaches init-pushed; a cancelled chooser creates nothing.
        /// </summary>
        private static async Task<int> ChooserAsync(WorkbenchShell shell, string root)
        {
            var providers = WriteProviderFile(root);
            var chosen = Path.Combine(root, "chosen-workspace");
            Directory.CreateDirectory(chosen);
            string? windowRoot = null;
            var order = new List<string>();
            ComposerSurface? composer = null;

            NewSessionFlow Flow(Func<string?> choose) => new(
                activeWorkspaceRoot: () => windowRoot,
                chooseWorkspace: choose,
                openWorkspace: folder =>
                {
                    // The window reports the opened root in its own form — upper-cased here, the
                    // same directory — so a sheet bound to the chooser's string reads "other-root".
                    order.Add("open");
                    windowRoot = folder.ToUpperInvariant();
                    return Task.FromResult<string?>(null);
                },
                showSheet: sheet =>
                {
                    order.Add("sheet:" + (sheet.WorkspaceRoot == windowRoot ? "window-root" : "other-root"));
                    sheet.TaskClass = "probe";
                    return true;
                },
                registry: () => providers.Registry,
                workspaceId: folder => folder,
                opened: created =>
                {
                    order.Add("opened");
                    var said = shell.OpenSessionDocument(created.Config);
                    composer = shell.SessionComposer(created.Config.SessionId);
                    said = NewSessionPlacement.GiveItTheWholeTree(
                        shell.Service, created.Config.SessionId,
                        said + " " + SessionComposerBinder.Bind(
                            shell, created.Config, created.RoutableBackends, created.TaskClass,
                            repositoryRoot: windowRoot, dataDirectory: windowRoot,
                            providers, new NeverAffirms()));
                    shell.Adapter.Render();
                    Console.Out.WriteLine($"chooser: announced='{said}'");
                });

            var outcome = await Flow(() => { order.Add("choose"); return chosen; }).StartAsync();
            Console.Out.WriteLine(
                $"chooser: order=[{string.Join(",", order)}] created={outcome.Created is not null} outcome='{outcome.Announcement}'");

            if (composer is null)
            {
                Console.Error.WriteLine("the chooser flow created no document whose composer the shell holds");
                return TheChooserSessionWasNotBoundToTheChosenWorkspace;
            }

            await WaitAsync(() => Count(composer.SurfaceId, "init-pushed") >= 1, TimeSpan.FromSeconds(30));
            await Task.Delay(1000);

            var view = FindWebView(composer);
            var fields = view?.CoreWebView2 is null ? "(no page)" : await Eval(view, "String(document.querySelectorAll('#fields .field').length)");
            var boundTo = BoundRepositoryRoot(composer.SurfaceId);
            Console.Out.WriteLine(
                $"chooser: composer configured={Count(composer.SurfaceId, "configured")} init-pushed={Count(composer.SurfaceId, "init-pushed")} "
                + $"page fields={fields} status='{composer.Status}' "
                + $"bound-to-chosen-root={string.Equals(boundTo, chosen, StringComparison.OrdinalIgnoreCase)}");

            // The cancelled chooser, from a window with no workspace open again: nothing opened,
            // nothing created, no surface added.
            var surfacesBefore = shell.Service.Current.AllStacks().SelectMany(s => s.Surfaces).Count();
            var sessionsBefore = Directory.GetDirectories(SessionPaths.SessionsRoot(chosen)).Length;
            windowRoot = null;
            order.Clear();
            var cancelled = await Flow(() => { order.Add("choose"); return null; }).StartAsync();
            var surfacesAfter = shell.Service.Current.AllStacks().SelectMany(s => s.Surfaces).Count();
            var sessionsAfter = Directory.GetDirectories(SessionPaths.SessionsRoot(chosen)).Length;
            Console.Out.WriteLine(
                $"chooser cancelled: order=[{string.Join(",", order)}] created={cancelled.Created is not null} "
                + $"surfaces before={surfacesBefore} after={surfacesAfter} sessions before={sessionsBefore} after={sessionsAfter} "
                + $"outcome='{cancelled.Announcement}'");

            if (!string.Equals(boundTo, chosen, StringComparison.OrdinalIgnoreCase)
                || Count(composer.SurfaceId, "init-pushed") == 0 || fields != "6")
            {
                Console.Error.WriteLine(
                    $"the session created through the chooser was not bound to the chosen workspace: bound to '{boundTo ?? "(nothing)"}', "
                    + $"expected '{chosen}'; init-pushed={Count(composer.SurfaceId, "init-pushed")} fields={fields} status='{composer.Status}'");
                return TheChooserSessionWasNotBoundToTheChosenWorkspace;
            }

            if (cancelled.Created is not null || surfacesAfter != surfacesBefore || sessionsAfter != sessionsBefore || order.Contains("open"))
            {
                Console.Error.WriteLine("a cancelled chooser opened a workspace or created a session");
                return TheChooserSessionWasNotBoundToTheChosenWorkspace;
            }

            Console.Out.WriteLine(
                "the chooser opened the chosen workspace, the session was created and bound in it, and a cancelled chooser created nothing");
            return Ok;
        }

        /// <summary>The <c>repositoryRoot</c> on the <c>session-document.bound</c> line for the composer, or null when none was written.</summary>
        private static string? BoundRepositoryRoot(string composerSurfaceId)
        {
            lock (Lines)
            {
                var line = Lines.LastOrDefault(l =>
                    l.Contains("\"evt\":\"session-document.bound\"", StringComparison.Ordinal)
                    && l.Contains($"\"surface\":\"{composerSurfaceId}\"", StringComparison.Ordinal));
                if (line is null) { return null; }

                using var document = JsonDocument.Parse(line);
                return document.RootElement.GetProperty("repositoryRoot").GetString();
            }
        }

        private sealed record Measured(string Line, int Loaded, int Initialising, int InitPushed, string Fields);

        private static async Task<Measured> Report(
            string when, WorkbenchShell shell, ShellModeController mode, ComposerSurface composer,
            int loaded, int unloaded, int graphReattachedBefore)
        {
            var view = FindWebView(composer);
            var fields = view?.CoreWebView2 is null ? "(no page)" : await Eval(view, "String(document.querySelectorAll('#fields .field').length)");
            var initialising = Count(composer.SurfaceId, "initialising");
            var pushed = Count(composer.SurfaceId, "init-pushed");
            var line =
                $"{when}: mode={mode.Mode} last-mode-trigger={LastModeTrigger()} workbench root loaded={shell.WorkbenchRoot.IsLoaded} visible={shell.WorkbenchRoot.IsVisible}, "
                + $"composer wpf loaded={loaded} unloaded={unloaded} isLoaded={composer.IsLoaded} isVisible={composer.IsVisible} "
                + $"size={composer.ActualWidth:F0}x{composer.ActualHeight:F0}, "
                + $"transitions initialising={initialising} navigation-started={Count(composer.SurfaceId, "navigation-started")} "
                + $"page-ready={Count(composer.SurfaceId, "page-ready")} configured={Count(composer.SurfaceId, "configured")} init-pushed={pushed}, "
                + $"layout-lines={LayoutLines(composer.SurfaceId)}, page fields={fields}, "
                + $"graph re-attached by this render={Count("graph", "re-attached") - graphReattachedBefore}, "
                + $"active in view={shell.Adapter.ActiveSurfaceId ?? "(none)"}";
            return new Measured(line, loaded, initialising, pushed, fields);
        }

        /// <summary>The operator's restored arrangement, built from the quoted log line.</summary>
        private static WorkbenchLayout ArrangementFromTheOperatorsLog()
        {
            using var document = JsonDocument.Parse(RestoredArrangementPayload);
            var stacks = document.RootElement.GetProperty("stacks").EnumerateArray()
                .ToDictionary(
                    s => s.GetProperty("id").GetString()!,
                    s => new ZoneStack(
                        [.. s.GetProperty("surfaces").EnumerateArray().Select(v => SurfaceFrom(v.GetString()!))],
                        s.GetProperty("active").GetInt32()),
                    StringComparer.Ordinal);

            var zones = System.Collections.Immutable.ImmutableDictionary.CreateRange(new[]
            {
                KeyValuePair.Create(ZoneId.Left, new ZoneState(ZoneId.Left, stacks["zone-left"], ZoneState.DefaultExtent, Collapsed: false)),
                KeyValuePair.Create(ZoneId.Right, new ZoneState(ZoneId.Right, Content: null, ZoneState.DefaultExtent, Collapsed: false)),
                KeyValuePair.Create(ZoneId.Bottom, new ZoneState(ZoneId.Bottom, stacks["zone-bottom"], 0.30, Collapsed: false)),
                KeyValuePair.Create(ZoneId.Center, new ZoneState(ZoneId.Center, stacks["zone-center"], Extent: 1.0, Collapsed: false)),
            });

            return new WorkbenchLayout(zones, [], Maximized: null);
        }

        /// <summary>
        /// <c>"&lt;surfaceId&gt;:&lt;kind&gt;"</c> as <c>WorkbenchDiagnostics.LayoutMutation</c> writes it; a
        /// session-document id carries its own colon, so the kind is the LAST segment.
        /// </summary>
        private static Surface SurfaceFrom(string entry)
        {
            var at = entry.LastIndexOf(':');
            var id = entry[..at];
            var kind = entry[(at + 1)..];
            var title = kind == SessionDocumentSurface.Kind ? id[(id.IndexOf(':') + 1)..] : char.ToUpperInvariant(id[0]) + id[1..];
            return new Surface(id, kind, title);
        }

        /// <summary>The projected stacks as the mutation line prints them: id, active index, surfaces.</summary>
        private static string Shape(WorkbenchShell shell) => Shape(shell.Service.Current.AllStacks());

        private static string Shape(WorkbenchLayout layout) =>
            Shape(ZonesToTree.ToTree(layout).AllStacks());

        private static string Shape(IEnumerable<StackNode> stacks) =>
            string.Join(" | ", stacks.Select(s =>
                $"{s.Id}@{s.ActiveIndex}[{string.Join(",", s.Surfaces.Select(su => su.SurfaceId))}]"));

        private static string FirstText(DependencyObject? node)
        {
            if (node is null) { return "(no content)"; }
            if (node is TextBlock text) { return text.Text; }

            foreach (var child in LogicalTreeHelper.GetChildren(node))
            {
                if (child is DependencyObject dependency && FirstText(dependency) is { } found && found != "(no content)" && found.Length > 0)
                {
                    return found;
                }
            }

            return "(no content)";
        }

        private static async Task<bool> WaitAsync(Func<bool> settled, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (settled()) { return true; }
                await Task.Delay(100);
            }

            return false;
        }

        private static WebView2? FindWebView(DependencyObject node)
        {
            if (node is WebView2 found) { return found; }

            for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(node); i++)
            {
                if (FindWebView(System.Windows.Media.VisualTreeHelper.GetChild(node, i)) is { } child) { return child; }
            }

            if (node is ContentControl { Content: DependencyObject content }) { return FindWebView(content); }

            if (node is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is DependencyObject dependency && FindWebView(dependency) is { } nested) { return nested; }
                }
            }

            if (node is Decorator { Child: { } decorated }) { return FindWebView(decorated); }

            return null;
        }

        private sealed class NeverAffirms : IAttachmentAffirmation
        {
            public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
        }
    }
}
