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
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>T28 — the Ruling 96 recurrence (WL-1a demo, 2026-10-19).</b> The user typed a prompt, pressed <b>Ctrl+Enter</b>
/// (demo step 5) and opened the <i>Compiled prompt</i> disclosure: it showed no compiled prompt. Ruling 96's tests open
/// the disclosure over a draft that was set and never sent. These tests take the user's gesture instead: the product's
/// composition (<see cref="SessionDocumentSurface"/> over a <see cref="NotebookModel"/> bound to a real
/// <see cref="AttemptHost"/>), the product's goal-block form (<c>SessionComposerBinder</c> configures
/// <see cref="ComposerFields.GoalBlock"/>), the words arriving through the page's own input verb
/// (<see cref="ComposerSurface.SetFieldText"/>) and the send through the accelerator (<see cref="ComposerSurface.OnAcceleratorKey"/>).
/// </summary>
public sealed class TheCompiledFieldAfterASubmitTests(ITestOutputHelper output) : IDisposable
{
    private const uint Enter = 0x0D;
    private const string Words = "why are we using grep? is graphify and obsidian vault not configured?";

    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    /// <summary>
    /// The user's order: type, Ctrl+Enter, then open the disclosure. The field must show the compiled prompt that was
    /// sent, laid out at its floor.
    /// </summary>
    [Fact]
    public void OpenedAfterACtrlEnterSubmit_TheCompiledFieldShowsTheSentPrompt()
    {
        Drive(async (window, document, composer) =>
        {
            Type(composer, Words);
            Settle(document);
            var sent = composer.Gate.RenderView(composer.Draft).Text;
            Record("before the submit", composer);

            // THE WRITER, OBSERVED: every re-render during the gesture names the product frames that ran it, so the
            // render that emptied the field is read off the stack, not inferred from the code.
            var renders = new List<string>();
            void OnRender() => renders.Add($"render → {composer.CompiledView.Length} chars via {ProductFrames()}");
            composer.DraftChanged += OnRender;
            Assert.True(composer.OnAcceleratorKey(Enter, controlHeld: true, isKeyDown: true), "Ctrl+Enter was not the send gesture");
            composer.DraftChanged -= OnRender;
            await document.LastBinding.WaitAsync(NotebookFixture.Bound);
            Settle(document);
            renders.ForEach(output.WriteLine);
            Record("after the submit, disclosure closed", composer);

            Open(composer);
            Settle(document);
            var box = Record("after the submit, disclosure opened", composer);
            var turn = Assert.Single(document.Notebook.Thread.Current.Turns);
            output.WriteLine($"sent prompt {sent.Length} chars · RenderView(next draft) {composer.Gate.RenderView(composer.Draft).Text.Length} chars · {turn.DisplayOrdinal}'s row SentBytes {turn.SentBytes.Length} chars, equal to the sent prompt: {turn.SentBytes == sent}");

            Assert.True(composer.CompiledPromptOpen, "the header click did not open the disclosure");
            Assert.False(string.IsNullOrEmpty(sent), "the one-line message compiled to nothing before the submit");
            Assert.True(box.Text.Length > 0, $"the opened compiled field is empty after the submit; the prompt sent was {sent.Length} chars");
            Assert.Equal(sent, box.Text);
            Assert.True(box.ActualHeight >= ComposerSurface.CompiledPromptMinHeight - 0.5, $"the compiled box was laid out at {box.ActualHeight:F2} px; its floor when open is {ComposerSurface.CompiledPromptMinHeight}");
        });
    }

    /// <summary>
    /// The sticky order (Ruling 96: opened once, it stays open across turns): open, type, Ctrl+Enter. The open field
    /// keeps showing the compiled prompt that was sent.
    /// </summary>
    [Fact]
    public void OpenWhileTyping_ThenCtrlEnter_TheCompiledFieldKeepsTheSentPrompt()
    {
        Drive(async (window, document, composer) =>
        {
            Open(composer);
            Type(composer, Words);
            Settle(document);
            var sent = composer.Gate.RenderView(composer.Draft).Text;
            var before = Record("open, typed, before the submit", composer);
            Assert.Equal(sent, before.Text);
            Assert.True(before.ActualHeight >= ComposerSurface.CompiledPromptMinHeight - 0.5, $"before the submit the open box was laid out at {before.ActualHeight:F2} px");

            Assert.True(composer.OnAcceleratorKey(Enter, controlHeld: true, isKeyDown: true), "Ctrl+Enter was not the send gesture");
            await document.LastBinding.WaitAsync(NotebookFixture.Bound);
            Settle(document);
            var after = Record("open, after the submit", composer);
            output.WriteLine($"sent prompt {sent.Length} chars · RenderView(next draft) {composer.Gate.RenderView(composer.Draft).Text.Length} chars · turns {document.Notebook.Thread.Current.Turns.Count}");

            Assert.True(composer.CompiledPromptOpen, "the submit closed the disclosure");
            Assert.True(after.Text.Length > 0, $"the open compiled field emptied on the submit; the prompt sent was {sent.Length} chars");
            Assert.Equal(sent, after.Text);
            Assert.True(after.ActualHeight >= ComposerSurface.CompiledPromptMinHeight - 0.5, $"the compiled box was laid out at {after.ActualHeight:F2} px; its floor when open is {ComposerSurface.CompiledPromptMinHeight}");
        });
    }

    private void Drive(Func<Window, SessionDocumentSurface, ComposerSurface, Task> body)
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
                var lockFile = Path.Combine(_fixture.Root, ".aide", "attempt-host.lock");
                var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), lockFile, CancellationToken.None);
                using (host)
                {
                    using var model = _fixture.NewModel();
                    model.Runs.BindHost(host, new AttemptScope("ws-t28", _fixture.Store.CoreEpoch, SessionGeneration: 1));
                    using var document = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                    window.Content = document;
                    ConfigureAsTheBinderDoes(document.Composer);
                    _fixture.ReleaseRun();
                    Settle(document);

                    await body(window, document, document.Composer);
                    await document.LastLaunch.WaitAsync(NotebookFixture.Bound);
                }
            });
    }

    /// <summary>The stub lane of <see cref="NotebookFixture.ConfigureStub(ComposerSurface)"/>, with the binder's goal-block form.</summary>
    private void ConfigureAsTheBinderDoes(ComposerSurface composer)
    {
        var install = Path.Combine(_fixture.Root, "install");
        Conductor.StubAcpAdapter.Install(install);
        composer.Configure(
            new SessionConfig(NotebookFixture.SessionId, "Binding", _fixture.Root, DateTimeOffset.UnixEpoch, [new AccountRef("anthropic", "max")], new AccountRef("anthropic", "max")),
            new ComposerSendContext(
                RepositoryRoot: _fixture.Root,
                DataDirectory: Path.Combine(_fixture.Root, "data"),
                AdapterInstallRoot: install,
                EngineId: "claude-code",
                Model: "claude-sonnet-5",
                AccountLabel: "max",
                TaskClass: "implement",
                ProofPackArtifacts: [],
                Providers: [new ProviderRow("anthropic", ProviderAuth.Subscription, [new ProviderAccount("max", AccountHealth.Ready)])]),
            ComposerFields.GoalBlock(),
            new AttachmentGate(_fixture.Root, new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max"));
        composer.Gate.Compiler = (_, _) => throw new OperationCanceledException();
    }

    /// <summary>The page's input verb: the words reach the draft the way a keystroke in the editor does.</summary>
    private static void Type(ComposerSurface composer, string words)
    {
        var message = composer.Fields.Single(f => f.Name == "message");
        composer.SetFieldText(message.Id, 1, words);
    }

    /// <summary>The header's own click, through its automation peer (the toggle path, never the property).</summary>
    private static void Open(ComposerSurface composer)
    {
        var disclosure = ThreadFixtures.Visuals<Expander>(composer).Single(e => AutomationProperties.GetName(e) == "Compiled prompt");
        var header = ThreadFixtures.Visuals<ToggleButton>(disclosure).First();
        ((IToggleProvider)UIElementAutomationPeer.CreatePeerForElement(header).GetPattern(PatternInterface.Toggle)!).Toggle();
    }

    private static void Settle(SessionDocumentSurface document)
    {
        document.UpdateLayout();
        document.Dispatcher.Invoke(DispatcherPriority.Background, () => { });
        document.UpdateLayout();
    }

    private TextBox Record(string moment, ComposerSurface composer)
    {
        var box = Logical<TextBox>(composer).Single(b => b.IsReadOnly && b.MaxHeight <= ComposerSurface.CompiledPromptMaxHeight + 1);
        output.WriteLine($"{moment}: IsExpanded {composer.CompiledPromptOpen} · _compiled.Text.Length {box.Text.Length} · _compiled.ActualHeight {box.ActualHeight:F2} · arrange-valid {box.IsArrangeValid} · draft {composer.Draft.SourceText.Length} chars");
        return box;
    }

    /// <summary>The product's own frames on the current stack, innermost first.</summary>
    private static string ProductFrames() =>
        string.Join(" ← ", new System.Diagnostics.StackTrace().GetFrames()
            .Select(f => f.GetMethod())
            .Where(m => m?.DeclaringType?.Namespace?.StartsWith("AiDe.App.Workbench", StringComparison.Ordinal) == true)
            .Select(m => $"{m!.DeclaringType!.Name}.{m.Name}"));

    private static IEnumerable<T> Logical<T>(DependencyObject node) where T : DependencyObject
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
