using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Tests.Shell;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// <b>Ruling 95 — Parallel.</b> <i>Start a parallel session</i> creates a derived sibling — the same
/// workspace, backends and config copied, <c>origin = parallel:&lt;parent id&gt;</c>, the name by the
/// Ruling 99 seam — opens it docked in the Left zone beside the parent (Ruling 83), binds its
/// composer, and sends the parent's draft (attachments following) as the sibling's first turn through
/// the sibling's own gate; the parent's draft is consumed only when that happened.
/// </summary>
/// <remarks>
/// <b>Red observed</b>: on a document with no shell the action reads <i>a parallel session needs
/// the shell; this composer has none</i> and the words stay (<see cref="WithoutAShell_TheActionRefuses_AndTheWordsStay"/>,
/// which is the same sentence the shell test read before <c>WorkbenchShell.ParallelSessionStarter</c>
/// existed — against the base shell that test does not compile: <c>CS1061 'WorkbenchShell' does not
/// contain a definition for 'ParallelSessionStarter'</c>).
/// </remarks>
public sealed class AParallelSessionIsADerivedSiblingTests : IDisposable
{
    private readonly ComposedCoding.Workspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    private sealed class NeverAsked : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
    }

    /// <summary>The binder's shape, harness-wired: a context whose engine the catalog refuses, so a send builds and launches and no adapter is spawned.</summary>
    private static string Bind(ComposerSurface composer, SessionConfig config, string root)
    {
        composer.Configure(
            config,
            new ComposerSendContext(
                RepositoryRoot: root, DataDirectory: Path.Combine(root, ".aide"), AdapterInstallRoot: root,
                EngineId: "no-such-engine", Model: "sonnet", AccountLabel: "max", TaskClass: config.DefaultTaskClass,
                ProofPackArtifacts: [], Providers: []),
            ComposerFields.FreeForm(),
            new AttachmentGate(root, new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max"));
        composer.Draft.SwitchTo(ComposerShape.FreeForm);
        return $"Composer bound to no-such-engine for {config.Name}.";
    }

    private static string? OriginOf(string root, string sessionId) =>
        new SessionConfigStore(root, sessionId).ReadEvents().Single(e => e.Kind == SessionEventKinds.Open).Body["origin"]?.GetValue<string>();

    private static Hyperlink? Link(ComposerSurface composer, string name)
    {
        var status = ThreadFixtures.Visuals<TextBlock>(composer).Single(t => AutomationProperties.GetName(t) == "Send status");
        return status.Inlines.OfType<Hyperlink>().FirstOrDefault(l => AutomationProperties.GetName(l) == name);
    }

    [Fact]
    public void TheFlow_CreatesADerivedSibling_AndSendsTheDraftThroughItsOwnGate()
    {
        Sta.Run(() =>
        {
            var root = _workspace.Root;
            var now = new DateTimeOffset(2026, 9, 14, 18, 30, 0, TimeSpan.Zero);
            var parent = new SessionConfigStore(root, SessionId.New(now)).Create(
                "payments", root, ["claude-code"], now, fanOutCeiling: 2, budgetCap: new RunBudget(10, 40_000), defaultTaskClass: "implement");

            var documents = new Dictionary<string, SessionDocumentSurface>(StringComparer.Ordinal);
            var remembered = new List<SessionConfig>();
            var flow = new ParallelSessionFlow(
                open: config =>
                {
                    documents[config.SessionId] = new SessionDocumentSurface(new SessionDocumentViewModel(config.SessionId, config.Name, root, [CanvasModeCatalog.ConsoleModeId]));
                    return $"Session “{config.Name}” opened.";
                },
                bind: config => Bind(documents[config.SessionId].Composer, config, root),
                composerOf: id => documents.GetValueOrDefault(id)?.Composer,
                remember: remembered.Add,
                uniqueName: name => name + " (7)",
                time: new FixedTime(now.AddMinutes(1)));

            var attachment = new ComposerAttachment("notes.md", Path.Combine(root, "notes.md"), 11, "the notes\n", false, "sha");
            var outcome = flow.Start(root, parent, new ParallelDraft("second question", [attachment]));

            // Created: the seam's name, the parent's config, the parallel origin.
            Assert.NotNull(outcome.Created);
            var sibling = outcome.Created!;
            Assert.Equal("payments (7)", sibling.Name);
            Assert.Equal(parent.WorkspaceId, sibling.WorkspaceId);
            Assert.Equal(parent.EnabledBackends, sibling.EnabledBackends);
            Assert.Equal(2, sibling.FanOutCeiling);
            Assert.Equal(new RunBudget(10, 40_000), sibling.BudgetCap);
            Assert.Equal("implement", sibling.DefaultTaskClass);
            Assert.NotEqual(parent.SessionId, sibling.SessionId);
            Assert.Equal("parallel:" + parent.SessionId, OriginOf(root, sibling.SessionId));
            Assert.Equal(SessionOrigins.Direct, OriginOf(root, parent.SessionId));
            Assert.Equal([sibling.SessionId], remembered.Select(c => c.SessionId));

            // Sent through the sibling's own gate: its thread has b1 with the words, the attachment in the bytes.
            var document = documents[sibling.SessionId];
            Assert.Equal(1, document.Composer.Gate.BlocksSent);
            var turn = Assert.Single(document.ReadModel.Current.Turns);
            Assert.Equal("second question", turn.SourceText);
            Assert.Contains("the notes", turn.SentBytes, StringComparison.Ordinal);
            Assert.Null(outcome.Refusal);
            Assert.StartsWith("Parallel session “payments (7)” created beside “payments”; its first turn is sent.", outcome.Announcement, StringComparison.Ordinal);

            document.LastLaunch.Wait(TimeSpan.FromSeconds(20));
            document.Dispose();
        });
    }

    [Fact]
    public void WhenTheSiblingsComposerIsNotBound_NothingIsSent_AndTheRefusalIsTheBinders()
    {
        Sta.Run(() =>
        {
            var root = _workspace.Root;
            var now = DateTimeOffset.UtcNow;
            var parent = new SessionConfigStore(root, SessionId.New(now)).Create("payments", root, ["claude-code"], now);
            var documents = new Dictionary<string, SessionDocumentSurface>(StringComparer.Ordinal);
            var flow = new ParallelSessionFlow(
                open: config =>
                {
                    documents[config.SessionId] = new SessionDocumentSurface(new SessionDocumentViewModel(config.SessionId, config.Name, root, [CanvasModeCatalog.ConsoleModeId]));
                    return $"Session “{config.Name}” opened.";
                },
                bind: _ => "The composer has no run binding — providers: there is no provider file.",
                composerOf: id => documents.GetValueOrDefault(id)?.Composer);

            var outcome = flow.Start(root, parent, new ParallelDraft("second", []));

            Assert.NotNull(outcome.Created);
            Assert.Equal("payments (2)", outcome.Created!.Name);
            Assert.Equal("The composer has no run binding — providers: there is no provider file.", outcome.Refusal);
            Assert.Equal(0, documents[outcome.Created.SessionId].Composer.Gate.BlocksSent);
            Assert.Contains("The prompt stays in this editor.", outcome.Announcement, StringComparison.Ordinal);
            documents[outcome.Created.SessionId].Dispose();
        });
    }

    /// <summary>The composer's own refusal, with no shell behind it — and the words stay.</summary>
    [Fact]
    public void WithoutAShell_TheActionRefuses_AndTheWordsStay()
    {
        Sta.Pump(
            create: () =>
            {
                var document = new SessionDocumentSurface(new SessionDocumentViewModel("20260914T183000Z-alone", "alone", _workspace.Root, [CanvasModeCatalog.ConsoleModeId]));
                Bind(document.Composer, new SessionConfig("20260914T183000Z-alone", "alone", _workspace.Root, DateTimeOffset.UnixEpoch, ["claude-code"]), _workspace.Root);
                document.ReadModel.Accept("first", [], "first", DateTimeOffset.Now);
                return document;
            },
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, document) =>
            {
                await window.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                document.Composer.Draft.SetFreeFormText("second");
                Assert.Null(document.Composer.Send());
                var parallel = Link(document.Composer, ComposerSurface.ParallelActionName);
                Assert.True(parallel is not null, $"the status line offers no Parallel action; it reads \"{document.Composer.Status}\"");

                parallel!.RaiseEvent(new RoutedEventArgs(Hyperlink.ClickEvent));

                Assert.Equal("a parallel session needs the shell; this composer has none", document.Composer.Status);
                Assert.Equal("second", document.Composer.Draft.SourceText);
                Assert.Single(document.ReadModel.Current.Turns);
            });
    }

    /// <summary>The shell's half: the sibling docks in Coding's Left zone beside the parent (Ruling 83), bound and sent; the parent's draft is consumed.</summary>
    [Fact]
    public void FromTheParentsStatusLine_TheSiblingDocksLeftBesideIt_AndTheParentsDraftIsConsumed()
    {
        var root = _workspace.Root;
        var parentConfig = new SessionConfigStore(root, SessionId.New(DateTimeOffset.UtcNow)).Create("payments", root, ["claude-code"], DateTimeOffset.UtcNow);

        Sta.Run(() =>
        {
            using var frame = ComposedCoding.Show(1440, 900);
            var shell = frame.Shell;
            var bound = new List<string>();

            shell.ParallelSessionStarter = (parent, request) => new ParallelSessionFlow(
                open: shell.OpenSessionDocument,
                bind: config => { bound.Add(config.Name); return Bind(shell.SessionComposer(config.SessionId)!, config, root); },
                composerOf: shell.SessionComposer)
                .Start(root, new SessionConfigStore(root, parent.Model.SessionId).Load(), request);

            shell.OpenSessionDocument(parentConfig);
            frame.Settle();
            var parentSurface = SessionDocumentSurface.SurfaceIdFor(parentConfig.SessionId);
            var parent = frame.Document(parentSurface);
            Bind(parent.Composer, parentConfig, root);
            parent.ReadModel.Accept("first", [], "first", DateTimeOffset.Now);
            frame.Settle();

            parent.Composer.Draft.SetFreeFormText("second");
            Assert.Null(parent.Composer.Send());
            var parallel = Link(parent.Composer, ComposerSurface.ParallelActionName);
            Assert.True(parallel is not null, $"the status line offers no Parallel action; it reads \"{parent.Composer.Status}\"");

            parallel!.RaiseEvent(new RoutedEventArgs(Hyperlink.ClickEvent));
            frame.Settle();

            // The sibling exists, docked in the Left zone beside the parent, bound, and sent.
            Assert.Equal(["payments (2)"], bound);
            var zones = shell.Coding.Service.Zones;
            var siblingSurface = zones.Zone(ZoneId.Left).Surfaces().Select(s => s.SurfaceId)
                .Single(id => id != parentSurface && SessionDocumentSurface.SessionIdOf(id) is not null);
            Assert.Equal(ZoneId.Left, zones.FindZoneOf(parentSurface));
            Assert.Null(zones.Maximized);
            var sibling = frame.Document(siblingSurface);
            Assert.Equal("payments (2)", sibling.Model.Title);
            Assert.Equal(1, sibling.Composer.Gate.BlocksSent);
            Assert.Equal("second", Assert.Single(sibling.ReadModel.Current.Turns).SourceText);
            Assert.Equal("parallel:" + parentConfig.SessionId, OriginOf(root, sibling.Model.SessionId));

            // The parent: its draft consumed, its status says so, its own thread untouched.
            Assert.Equal(string.Empty, parent.Composer.Draft.SourceText);
            Assert.Equal("sent as the first turn of a parallel session", parent.Composer.Status);
            Assert.Single(parent.ReadModel.Current.Turns);

            sibling.LastLaunch.Wait(TimeSpan.FromSeconds(20));
        });
    }

    /// <summary>Ruling 99's one rule, on the parent's name, over every name the workspace holds — the window supplies it, and this proves the seam carries it.</summary>
    [Fact]
    public void WithRuling99sRule_TheSiblingsNameCountsPastTheNamesTheWorkspaceHolds()
    {
        Sta.Run(() =>
        {
            var root = _workspace.Root;
            var now = DateTimeOffset.UtcNow;
            var parent = new SessionConfigStore(root, SessionId.New(now)).Create("payments", root, ["claude-code"], now);
            new SessionConfigStore(root, SessionId.New(now.AddSeconds(1))).Create("payments (2)", root, ["claude-code"], now.AddSeconds(1));

            var documents = new Dictionary<string, SessionDocumentSurface>(StringComparer.Ordinal);
            var flow = new ParallelSessionFlow(
                open: config =>
                {
                    documents[config.SessionId] = new SessionDocumentSurface(new SessionDocumentViewModel(config.SessionId, config.Name, root, [CanvasModeCatalog.ConsoleModeId]));
                    return "opened";
                },
                bind: _ => "not bound",
                composerOf: id => documents.GetValueOrDefault(id)?.Composer,
                uniqueName: name => SessionConfigStore.UniqueName(name, SessionConfigStore.ExistingNames(root)));

            var outcome = flow.Start(root, parent, new ParallelDraft("second", []));
            Assert.Equal("payments (3)", outcome.Created!.Name);
            documents[outcome.Created.SessionId].Dispose();
        });

        // And the window wires exactly that rule into the flow — never the first-counter default.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        var window = File.ReadAllText(Path.Combine(dir!.FullName, "src", "AiDe.App", "MainWindow.xaml.cs"));
        Assert.Contains("SessionConfigStore.UniqueName(name, AiDe.Core.Sessions.SessionConfigStore.ExistingNames(root))", window, StringComparison.Ordinal);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
