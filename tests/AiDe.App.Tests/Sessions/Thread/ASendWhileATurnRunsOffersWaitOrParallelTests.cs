using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Tests.Conductor;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Sessions.Thread;

/// <summary>
/// <b>Ruling 95 — the STA rows (§B5, amended): two-action line · queued · drained · stopped-with-queued ·
/// failed-with-queued · cancel · the queued turn's compile spend.</b> Every row is rendered: the
/// composer's status line with its links, the thread's row with its actions, and — where a run is
/// needed — the product's own composition root spawning <see cref="StubAcpAdapter"/> at the process
/// boundary, held and released on cue.
/// </summary>
/// <remarks>
/// <b>Red observed</b> (against the App at <c>c831113e</c>, the Core types present): every row
/// stops at the first assertion — <i>the status line offers no Wait action; it reads "b1 is
/// running; the next turn waits for it."</i> — because Ruling 77(b)'s refusal was the only
/// answer a Send while running had. Recorded in <c>docs/proof/send-while-running.md</c>.
/// </remarks>
public sealed class ASendWhileATurnRunsOffersWaitOrParallelTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(30);

    private sealed class NeverAsked : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
    }

    /// <summary>A temp workspace, a stub adapter install, and a document whose composer is bound to them.</summary>
    private sealed class Fixture : IDisposable
    {
        private readonly string _root = Directory.CreateTempSubdirectory("aide-r95-").FullName;
        private int _compilerCalls;

        public Fixture(StubAcpAdapter.Mode? mode = null, string compileMode = CompileModes.MechanicalOnly)
        {
            Repository = Path.Combine(_root, "repo");
            Directory.CreateDirectory(Repository);
            StubAcpAdapter.Install(Install, mode);

            var config = new SessionConfig("20260914T180000Z-r95", "r95", Repository, DateTimeOffset.UnixEpoch, ["claude-code"]) { CompileMode = compileMode };
            Document = new SessionDocumentSurface(new SessionDocumentViewModel(config.SessionId, config.Name, Repository, [CanvasModeCatalog.ConsoleModeId]), null, new RecordingAnnouncer());
            Document.Composer.Configure(
                config,
                new ComposerSendContext(
                    RepositoryRoot: Repository,
                    DataDirectory: Path.Combine(_root, "data"),
                    AdapterInstallRoot: Install,
                    EngineId: "claude-code",
                    Model: "claude-sonnet-5",
                    AccountLabel: "max",
                    TaskClass: config.DefaultTaskClass,
                    ProofPackArtifacts: [],
                    Providers: [new ProviderRow("anthropic", ProviderAuth.Subscription, [new ProviderAccount("max", AccountHealth.Ready)])]),
                ComposerFields.FreeForm(),
                new AttachmentGate(Repository, new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max"));
            Document.Composer.Draft.SwitchTo(ComposerShape.FreeForm);

            // The agentic rung's compiler: a fake whose cost differs per call, so a turn's compile
            // spend is attributable — 100/20 for the first envelope, 300/40 for the second.
            Document.Composer.Gate.Compiler = (_, _) =>
            {
                var call = Interlocked.Increment(ref _compilerCalls);
                return Task.FromResult(new CompileResult(
                    CompileCallOutcomes.Answered, null, """{"contract":"compile-output/1","decorations":[]}""",
                    call == 1 ? new RunEventCost(100, 20, 0, 1) : new RunEventCost(300, 40, 0, 1),
                    "claude-opus-5[1m]", 5, 0, 0, 1, false, 0, "account", null, []));
            };
        }

        public string Repository { get; }

        public string Install => Path.Combine(_root, "install");

        public SessionDocumentSurface Document { get; }

        public ComposerSurface Composer => Document.Composer;

        public RecordingAnnouncer Announcer =>
            (RecordingAnnouncer)typeof(SessionDocumentSurface).GetField("_announcer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(Document)!;

        public void Release() => StubAcpAdapter.Release(Repository);

        public void Dispose()
        {
            Document.Dispose();
            try { Directory.Delete(_root, recursive: true); } catch (IOException) { /* reaped later */ } catch (UnauthorizedAccessException) { /* reaped later */ }
        }
    }

    private static Hyperlink? Link(ComposerSurface composer, string name)
    {
        var status = ThreadFixtures.Visuals<TextBlock>(composer).Single(t => AutomationProperties.GetName(t) == "Send status");
        return status.Inlines.OfType<Hyperlink>().FirstOrDefault(l => AutomationProperties.GetName(l) == name);
    }

    private static void Click(Hyperlink link) => link.RaiseEvent(new RoutedEventArgs(Hyperlink.ClickEvent));

    private static Button? Action(SessionDocumentSurface document, int index, string word) =>
        ThreadFixtures.Visuals<Button>(ThreadFixtures.Container(document.Thread, index)).FirstOrDefault(b => b.Content is string s && s == word);

    private static async Task PumpAsync(Window window)
    {
        for (var i = 0; i < 3; i++)
        {
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
            window.UpdateLayout();
        }
    }

    private static async Task UntilAsync(Window window, Func<bool> condition, string what)
    {
        var deadline = DateTime.UtcNow + Bound;
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
            await PumpAsync(window);
        }

        Assert.True(condition(), what);

        // The read model moved; the rows follow on the dispatcher — pump so a reader of the rows
        // sees the snapshot the condition saw, never the one before it.
        await PumpAsync(window);
    }

    private static void Type(Fixture f, string text) => f.Composer.Draft.SetFreeFormText(text);

    /// <summary>The two-action line replaces Ruling 77(b)'s refusal: no dialog, no modifier, both actions named, spoken once.</summary>
    [Fact]
    public void ASendWhileATurnRuns_OffersWaitAndParallel_OnTheStatusLine_NeverARefusal()
    {
        Sta.Pump(
            create: () =>
            {
                var f = new Fixture();
                f.Document.ReadModel.Accept("first", [], "first", DateTimeOffset.Now);
                return f.Document;
            },
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, document) =>
            {
                await PumpAsync(window);
                var composer = document.Composer;
                var announcer = (RecordingAnnouncer)typeof(SessionDocumentSurface).GetField("_announcer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(document)!;
                var before = announcer.Announcements.Count;

                composer.Draft.SetFreeFormText("second");
                Assert.Null(composer.Send());

                Assert.True(Link(composer, "Wait — send after b1") is not null, $"the status line offers no Wait action; it reads \"{composer.Status}\"");
                Assert.True(Link(composer, "Start a parallel session") is not null, $"the status line offers no Parallel action; it reads \"{composer.Status}\"");
                Assert.NotNull(Link(composer, "b1, running"));
                Assert.Equal("b1 is running. Wait — send after b1, or start a parallel session.", composer.Status);

                // Spoken once, assertively (SC6): a choice the operator must make.
                var spoken = announcer.Announcements[^1];
                Assert.Equal(before + 1, announcer.Announcements.Count);
                Assert.Equal(composer.Status, spoken.Text);
                Assert.Equal(Urgency.Assertive, spoken.Urgency);

                // Nothing was compiled or queued by the offer itself: the draft stands, the thread has one turn.
                Assert.Equal("second", composer.Draft.SourceText);
                Assert.Single(document.ReadModel.Current.Turns);
                Assert.Equal(0, composer.Gate.BlocksSent);
            });
    }

    /// <summary>Wait compiles now (same gate, same bytes), queues exactly one turn, clears the editor; a second Send is refused naming it; Cancel returns the words.</summary>
    [Fact]
    public void Wait_QueuesOneCompiledTurn_ASecondSendIsRefused_AndCancelReturnsTheWords()
    {
        Sta.Pump(
            create: () =>
            {
                var f = new Fixture();
                f.Document.ReadModel.Accept("first", [], "first", DateTimeOffset.Now);
                return f.Document;
            },
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, document) =>
            {
                await PumpAsync(window);
                var composer = document.Composer;

                composer.Draft.SetFreeFormText("second");
                Assert.Null(composer.Send());
                var wait = Link(composer, "Wait — send after b1");
                Assert.True(wait is not null, $"the status line offers no Wait action; it reads \"{composer.Status}\"");
                var expectedBytes = composer.Gate.RenderView(composer.Draft).Text;

                Click(wait!);
                await PumpAsync(window);

                // QUEUED: b2 joined the thread with exactly the bytes the gate rendered, submitted (sha recorded), not running.
                Assert.Equal(2, document.ReadModel.Current.Turns.Count);
                var queued = document.ReadModel.Current.Turns[1];
                Assert.Equal(TurnState.Queued, queued.State);
                Assert.Equal(expectedBytes, queued.SentBytes);
                Assert.Equal("second", queued.SourceText);
                Assert.NotNull(composer.Gate.LastSubmission);
                Assert.Equal(1, composer.Gate.BlocksSent);
                Assert.Equal(1, document.ReadModel.Current.InFlight!.Ordinal);
                Assert.Equal(string.Empty, composer.Draft.SourceText);
                Assert.Equal("b2 queued — sends after b1", composer.Status);

                // The row: the word, the sentence, and Cancel as its only action — no Stop, no Send now.
                var row = document.Thread.Rows[1];
                Assert.Equal("queued", row.OutcomeWord);
                Assert.Equal("b2 queued — sends after b1", row.HelpText);
                Assert.Equal([TurnActionKind.Cancel], row.Actions);
                Assert.NotNull(Action(document, 1, "Cancel"));
                Assert.Null(Action(document, 1, "Send now"));
                Assert.Null(Action(document, 1, "Stop this turn"));

                // Exactly one: a further Send is refused, naming the queued turn.
                composer.Draft.SetFreeFormText("third");
                Assert.Null(composer.Send());
                Assert.Equal("b2 is queued; cancel it or wait.", composer.Status);
                Assert.Equal(2, document.ReadModel.Current.Turns.Count);

                // Cancel: the row stays (append-only), terminal and never sent; the words are back in the editor.
                Action(document, 1, "Cancel")!.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                await PumpAsync(window);
                Assert.Equal(TurnState.Cancelled, document.ReadModel.Current.Turns[1].State);
                Assert.Equal("cancelled by you", document.Thread.Rows[1].OutcomeWord);
                Assert.Equal("never sent", document.Thread.Rows[1].Counts);
                Assert.Empty(document.Thread.Rows[1].Actions);
                Assert.Equal("second", composer.Draft.SourceText);
                Assert.Null(document.ReadModel.Current.Queued);

                // And the next Send while b1 still runs offers the choice again — the queue is free.
                Assert.Null(composer.Send());
                Assert.NotNull(Link(composer, "Wait — send after b1"));
            });
    }

    /// <summary>The drain: when b1 ends Completed/Answered the queued turn is sent automatically, through the same root, as its own run.</summary>
    [Fact]
    public void TheQueuedTurn_IsSentWhenTheRunningTurnAnswers_ThroughTheProductsOwnRoot()
    {
        Fixture? fixture = null;
        try
        {
            Sta.Pump(
                create: () => (fixture = new Fixture()).Document,
                configure: window => { window.Width = 1200; window.Height = 800; },
                body: async (window, document) =>
                {
                    var f = fixture!;
                    using var ledger = CompositionRootLedger.Open();
                    await PumpAsync(window);

                    Type(f, "first");
                    Assert.NotNull(f.Composer.Send());
                    await UntilAsync(window, () => StubAcpAdapter.SessionOpened(f.Repository), "the first run never opened its ACP session (is node on PATH?)");
                    Assert.Equal(TurnState.Running, document.ReadModel.Current.Turns[0].State);

                    Type(f, "second");
                    Assert.Null(f.Composer.Send());
                    var wait = Link(f.Composer, "Wait — send after b1");
                    Assert.True(wait is not null, $"the status line offers no Wait action; it reads \"{f.Composer.Status}\"");
                    Click(wait!);
                    await PumpAsync(window);
                    Assert.Equal(TurnState.Queued, document.ReadModel.Current.Turns[1].State);
                    Assert.Equal(1, ledger.Roots);

                    f.Release();
                    await UntilAsync(window, () => document.ReadModel.Current.Turns[0].State == TurnState.Answered, "b1 never answered after the release");
                    await UntilAsync(window, () => document.ReadModel.Current.Turns[1].State == TurnState.Answered, $"b2 never drained; it reads {document.ReadModel.Current.Turns[1].State}");

                    Assert.Equal(2, ledger.Roots);
                    Assert.Equal("answered", document.Thread.Rows[1].OutcomeWord);
                    Assert.Contains("6 tokens", document.Thread.Rows[1].Counts);
                    Assert.Null(document.ReadModel.Current.Queued);
                    Assert.Equal(string.Empty, f.Composer.Status);
                });
        }
        finally
        {
            fixture?.Dispose();
        }
    }

    /// <summary>After Stop the queued turn stays queued, waits on the operator with Send now, and the drain never sends it; Send now does.</summary>
    [Fact]
    public void AfterStop_TheQueuedTurnWaitsWithSendNow_AndTheDrainNeverSendsIt()
    {
        Fixture? fixture = null;
        try
        {
            Sta.Pump(
                create: () => (fixture = new Fixture()).Document,
                configure: window => { window.Width = 1200; window.Height = 800; },
                body: async (window, document) =>
                {
                    var f = fixture!;
                    using var ledger = CompositionRootLedger.Open();
                    await PumpAsync(window);

                    Type(f, "first");
                    Assert.NotNull(f.Composer.Send());
                    await UntilAsync(window, () => StubAcpAdapter.SessionOpened(f.Repository), "the first run never opened its ACP session");

                    Type(f, "second");
                    Assert.Null(f.Composer.Send());
                    var wait = Link(f.Composer, "Wait — send after b1");
                    Assert.True(wait is not null, $"the status line offers no Wait action; it reads \"{f.Composer.Status}\"");
                    Click(wait!);
                    await PumpAsync(window);

                    Action(document, 0, "Stop this turn")!.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    await UntilAsync(window, () => document.ReadModel.Current.Turns[0].State == TurnState.Stopped, "b1 never stopped");
                    await Task.Delay(300);
                    await PumpAsync(window);

                    // Still queued; nothing drained; one root.
                    Assert.Equal(TurnState.Queued, document.ReadModel.Current.Turns[1].State);
                    Assert.Equal(1, ledger.Roots);
                    Assert.True(document.ReadModel.Current.QueuedAwaitsYou);
                    Assert.Equal("b1 stopped by you; b2 is waiting — Send it or cancel it", f.Composer.Status);
                    Assert.Equal([TurnActionKind.SendNow, TurnActionKind.Cancel], document.Thread.Rows[1].Actions);
                    var sendNow = Action(document, 1, "Send now");
                    Assert.NotNull(sendNow);

                    // Send now is the operator's: b2 runs as its own run.
                    sendNow!.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    await UntilAsync(window, () => document.ReadModel.Current.Turns[1].State == TurnState.Running, "Send now did not start b2");
                    Assert.Equal(2, ledger.Roots);
                    f.Release();
                    await UntilAsync(window, () => document.ReadModel.Current.Turns[1].State == TurnState.Answered, "b2 never answered after the release");
                    Assert.Null(document.ReadModel.Current.Queued);
                });
        }
        finally
        {
            fixture?.Dispose();
        }
    }

    /// <summary>After a failure the queued turn stays queued with Send now; the drain never sends it (Ruling 95 condition 3).</summary>
    [Fact]
    public void AfterAFailure_TheQueuedTurnWaits_AndTheDrainNeverSendsIt()
    {
        Fixture? fixture = null;
        try
        {
            Sta.Pump(
                create: () => (fixture = new Fixture(new StubAcpAdapter.Mode(FailAtInitialize: true))).Document,
                configure: window => { window.Width = 1200; window.Height = 800; },
                body: async (window, document) =>
                {
                    var f = fixture!;
                    using var ledger = CompositionRootLedger.Open();
                    await PumpAsync(window);

                    Type(f, "first");
                    Assert.NotNull(f.Composer.Send());
                    Assert.Equal(TurnState.Running, document.ReadModel.Current.Turns[0].State);

                    Type(f, "second");
                    Assert.Null(f.Composer.Send());
                    var wait = Link(f.Composer, "Wait — send after b1");
                    Assert.True(wait is not null, $"the status line offers no Wait action; it reads \"{f.Composer.Status}\"");
                    Click(wait!);
                    await PumpAsync(window);
                    Assert.Equal(TurnState.Queued, document.ReadModel.Current.Turns[1].State);

                    f.Release();
                    await UntilAsync(window, () => document.ReadModel.Current.Turns[0].State == TurnState.Failed, $"b1 never failed; it reads {document.ReadModel.Current.Turns[0].State}");
                    await Task.Delay(300);
                    await PumpAsync(window);

                    Assert.Equal(TurnState.Queued, document.ReadModel.Current.Turns[1].State);
                    Assert.Equal(1, ledger.Roots);
                    Assert.Equal("b1 failed; b2 is waiting — Send it or cancel it", f.Composer.Status);
                    Assert.Equal([TurnActionKind.SendNow, TurnActionKind.Cancel], document.Thread.Rows[1].Actions);
                    Assert.NotNull(Action(document, 1, "Send now"));
                });
        }
        finally
        {
            fixture?.Dispose();
        }
    }

    /// <summary>Ruling 95 condition 4 (Ruling 78): the queued turn's compile spend is on its own outcome line, never on the turn it waited behind.</summary>
    [Fact]
    public void TheQueuedTurnsCompileSpend_IsOnItsOwnOutcomeLine()
    {
        Fixture? fixture = null;
        try
        {
            Sta.Pump(
                create: () => (fixture = new Fixture(compileMode: CompileModes.AgenticAdvisory)).Document,
                configure: window => { window.Width = 1200; window.Height = 800; },
                body: async (window, document) =>
                {
                    var f = fixture!;
                    await PumpAsync(window);

                    // Under an agentic rung a prompt costs two gestures: prepare, then confirm.
                    Type(f, "first");
                    Assert.Null(f.Composer.Send());
                    await UntilAsync(window, () => f.Composer.PrepareState == PrepareState.Prepared, "b1 never prepared");
                    Assert.NotNull(f.Composer.Send());
                    await UntilAsync(window, () => StubAcpAdapter.SessionOpened(f.Repository), "the first run never opened its ACP session");

                    // The second: prepare, confirm → the offer → Wait (the same gate, the same two gestures, then the choice).
                    Type(f, "second");
                    Assert.Null(f.Composer.Send());
                    Assert.Null(Link(f.Composer, "Wait — send after b1"));   // the offer comes only when a send would happen
                    await UntilAsync(window, () => f.Composer.PrepareState == PrepareState.Prepared, "b2 never prepared");
                    Assert.Null(f.Composer.Send());
                    var wait = Link(f.Composer, "Wait — send after b1");
                    Assert.True(wait is not null, $"the status line offers no Wait action; it reads \"{f.Composer.Status}\"");
                    Click(wait!);
                    await PumpAsync(window);
                    Assert.Equal(TurnState.Queued, document.ReadModel.Current.Turns[1].State);

                    f.Release();
                    await UntilAsync(window, () => document.ReadModel.Current.Turns[0].State == TurnState.Answered, $"b1 never answered after the release; it reads {document.ReadModel.Current.Turns[0].State}; failure {document.LastRunFailure}");
                    await UntilAsync(window, () => document.ReadModel.Current.Turns[1].State == TurnState.Answered, $"b2 never drained; it reads {document.ReadModel.Current.Turns[1].State}; failure {document.LastRunFailure}; events [{string.Join(", ", document.ReadModel.Current.Turns[1].Events.Select(e => e.Kind))}]; stub traces:\n{StubAcpAdapter.Traces(f.Repository)}");

                    // b1: compile 100+20 + run 4+2 = 126; b2: compile 300+40 + run 6 = 346 — each on its own line.
                    Assert.True(document.Thread.Rows[0].Counts.Contains("126 tokens", StringComparison.Ordinal), $"b1's outcome line reads \"{document.Thread.Rows[0].Counts}\" (spend {document.ReadModel.Current.Turns[0].Outcome!.Spend})");
                    Assert.True(document.Thread.Rows[1].Counts.Contains("346 tokens", StringComparison.Ordinal), $"b2's outcome line reads \"{document.Thread.Rows[1].Counts}\" (spend {document.ReadModel.Current.Turns[1].Outcome!.Spend})");
                    Assert.Equal(new Spend(304, 0, 42, 2), document.ReadModel.Current.Turns[1].Outcome!.Spend);
                });
        }
        finally
        {
            fixture?.Dispose();
        }
    }
}
