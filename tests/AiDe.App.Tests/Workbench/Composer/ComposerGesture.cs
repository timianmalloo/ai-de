using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Tests.Records;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Dispatch;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Sessions;
using Microsoft.Data.Sqlite;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>The user's gesture on the composer (T28, Ruling 181).</b> The words arrive through the page's input verb
/// (<see cref="ComposerSurface.SetFieldText"/>), the send is <b>Ctrl+Enter</b> through
/// <see cref="ComposerSurface.OnAcceleratorKey"/>, and the <i>Compiled prompt</i> disclosure opens through its header's
/// toggle peer — never by setting the draft, calling <c>Send</c> or setting <c>IsExpanded</c>. <see cref="Drive"/> builds
/// the product's composition: a <see cref="SessionDocumentSurface"/> over a <see cref="NotebookModel"/> whose runs are
/// bound to a real <see cref="AttemptHost"/>, shown in a window.
/// </summary>
internal static class ComposerGesture
{
    private const uint Enter = 0x0D;

    /// <summary>The session scope every run of these tests is dispatched under.</summary>
    public static AttemptScope Scope(NotebookFixture fixture) => new("ws-t28", fixture.Store.CoreEpoch, SessionGeneration: 1);

    /// <summary>The attempt host's lock file inside the fixture's workspace.</summary>
    public static string LockFile(NotebookFixture fixture) => Path.Combine(fixture.Root, ".aide", "attempt-host.lock");

    /// <summary>
    /// The product's composition, bound to a host that is already open. With <paramref name="releaseRun"/> every stub
    /// run answers at once; without it a run stays in flight until the body calls <see cref="NotebookFixture.ReleaseRun"/>.
    /// </summary>
    public static void Drive(NotebookFixture fixture, Func<Window, SessionDocumentSurface, Task> body, bool releaseRun = true, double width = 1200, double height = 800)
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = width; window.Height = height; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(fixture.Root, NotebookFixture.SessionId));
                var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(fixture.Store), LockFile(fixture), CancellationToken.None);
                using (host)
                {
                    using var model = fixture.NewModel();
                    model.Runs.BindHost(host, Scope(fixture));
                    using var document = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                    window.Content = document;
                    ConfigureAsTheBinderDoes(fixture, document.Composer);
                    if (releaseRun)
                    {
                        fixture.ReleaseRun();
                    }

                    Settle(document);

                    await body(window, document);
                    fixture.ReleaseRun();
                    await document.LastLaunch.WaitAsync(NotebookFixture.Bound);
                    window.Content = null;
                }
            });
    }

    /// <summary>
    /// The stub lane of <see cref="NotebookFixture.ConfigureStub(ComposerSurface)"/> with the goal-block form
    /// <c>SessionComposerBinder</c> configures, and the default mechanical mode (one press sends).
    /// </summary>
    public static void ConfigureAsTheBinderDoes(NotebookFixture fixture, ComposerSurface composer)
    {
        var install = Path.Combine(fixture.Root, "install");
        Conductor.StubAcpAdapter.Install(install);
        composer.Configure(
            new SessionConfig(NotebookFixture.SessionId, "Binding", fixture.Root, DateTimeOffset.UnixEpoch, [new AccountRef("anthropic", "max")], new AccountRef("anthropic", "max")),
            new ComposerSendContext(
                RepositoryRoot: fixture.Root,
                DataDirectory: Path.Combine(fixture.Root, "data"),
                AdapterInstallRoot: install,
                EngineId: "claude-code",
                Model: "claude-sonnet-5",
                AccountLabel: "max",
                TaskClass: "implement",
                ProofPackArtifacts: [],
                Providers: [new ProviderRow("anthropic", ProviderAuth.Subscription, [new ProviderAccount("max", AccountHealth.Ready)])]),
            ComposerFields.GoalBlock(),
            new AttachmentGate(fixture.Root, new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max"));
        composer.Gate.Compiler = (_, _) => throw new OperationCanceledException();
    }

    /// <summary>The page's input verb: the words reach the draft the way a keystroke in the editor does.</summary>
    public static void Type(ComposerSurface composer, string words)
    {
        var editor = composer.Fields.First(f => f.Target == ComposerFieldTarget.FreeForm);
        composer.SetFieldText(editor.Id, 1, words);
    }

    /// <summary>Ctrl+Enter through the WebView2 accelerator handler.</summary>
    public static void CtrlEnter(ComposerSurface composer) =>
        Assert.True(composer.OnAcceleratorKey(Enter, controlHeld: true, isKeyDown: true), "Ctrl+Enter was not the send gesture");

    /// <summary>The disclosure named <i>Compiled prompt</i> in the composer's visual tree.</summary>
    public static Expander Disclosure(ComposerSurface composer) =>
        ThreadFixtures.Visuals<Expander>(composer).Single(e => AutomationProperties.GetName(e) == ComposerSurface.CompiledPromptHeading);

    /// <summary>The header's own click, through its automation peer (the toggle path, never the property).</summary>
    public static void Open(ComposerSurface composer)
    {
        var header = ThreadFixtures.Visuals<ToggleButton>(Disclosure(composer)).First();
        ((IToggleProvider)UIElementAutomationPeer.CreatePeerForElement(header).GetPattern(PatternInterface.Toggle)!).Toggle();
    }

    /// <summary>The compiled field — a logical child whether or not its disclosure is open.</summary>
    public static TextBox Field(ComposerSurface composer) =>
        Logical<TextBox>(composer).Single(b => b.IsReadOnly && b.MaxHeight <= ComposerSurface.CompiledPromptMaxHeight + 1);

    /// <summary>What the field says it is, on screen and in UIA: the header, the field's name, the disclosure's ItemStatus.</summary>
    public static (string Header, string Name, string ItemStatus) Heading(ComposerSurface composer)
    {
        var disclosure = Disclosure(composer);
        return ((string)disclosure.Header, AutomationProperties.GetName(Field(composer)), AutomationProperties.GetItemStatus(disclosure));
    }

    /// <summary>One layout pass and the thread's Background work.</summary>
    public static void Settle(FrameworkElement element)
    {
        element.UpdateLayout();
        element.Dispatcher.Invoke(DispatcherPriority.Background, () => { });
        element.UpdateLayout();
    }

    /// <summary>Pumps the window until <paramref name="condition"/> holds, or fails with <paramref name="what"/> after 20 s.</summary>
    public static async Task UntilAsync(Window window, Func<bool> condition, Func<string> what)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        }

        Assert.True(condition(), what());
    }

    /// <summary>The dispatch attempts the store holds — the host's write-ahead row, one per dispatch (ADR-0041).</summary>
    public static long AttemptRows(NotebookFixture fixture)
    {
        using var connection = new SqliteConnection($"Data Source={Path.Combine(fixture.Root, "workspace.db")};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var count = connection.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM dispatch_attempt_fact";
        return (long)count.ExecuteScalar()!;
    }

    public static IEnumerable<T> Logical<T>(DependencyObject node) where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(node))
        {
            if (child is not DependencyObject d)
            {
                continue;
            }

            if (d is T match)
            {
                yield return match;
            }

            foreach (var nested in Logical<T>(d))
            {
                yield return nested;
            }
        }
    }

    private sealed class NeverAsked : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
    }
}
