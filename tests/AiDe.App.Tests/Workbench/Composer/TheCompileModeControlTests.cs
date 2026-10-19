using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using AiDe.App.Tests.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Sessions;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>T30 (A) and (B) — Ruling 180 (1) and (2), on the session settings.</b> The settings row states the session's
/// compile mode and why, with the gate's own reason text (C3). The mode is a control: a combo box named
/// <i>Compile mode</i>, changed by keyboard, through <see cref="SessionConfigStore.SetCompileMode"/> and the gate.
/// <c>mechanical-only</c> is always selectable; <c>agentic-advisory</c> only when Gate 1 admits it, otherwise disabled
/// with the reason. Every step is the user's gesture: the header's <i>Session settings</i> button through its invoke
/// peer, then arrow keys on the focused combo box. The gate's input is injected
/// (<see cref="SessionDocumentSurface.CompileModeLadder"/>): no test reads <c>~/.aide/proof</c>.
/// </summary>
public sealed class TheCompileModeControlTests(ITestOutputHelper output) : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    /// <summary>Gate 1 admits, Gate 2 refuses: the ladder as this machine reads it today (Ruling 180's Owner check).</summary>
    private static CompileModeAvailability GateOneAdmits() => new(
        new Dictionary<string, CompileModeRefusal>(StringComparer.Ordinal)
        {
            [CompileModes.Agentic] = new("CE-0029", "Gate 2 has no admission report"),
        },
        Recount: null,
        ArtifactPath: "injected");

    /// <summary>The gate's own refusal: no pin-spike artifact in an empty proof directory.</summary>
    private CompileModeAvailability GateOneRefuses() => CompileModeGate.Evaluate(
        Directory.CreateDirectory(Path.Combine(_fixture.Root, "no-install")).FullName,
        Directory.CreateDirectory(Path.Combine(_fixture.Root, "no-proof")).FullName);

    /// <summary>A new session written by the store as File → New Session writes it, then bound as the binder binds it.</summary>
    private SessionConfig NewSession(CompileModeAvailability ladder) =>
        new SessionConfigStore(_fixture.Root, NotebookFixture.SessionId).Create("Binding", _fixture.Root, [new AccountRef("anthropic", "max")], new AccountRef("anthropic", "max"), DateTimeOffset.UtcNow, compileModes: ladder);

    private string StoredMode() => new SessionConfigStore(_fixture.Root, NotebookFixture.SessionId).Load().CompileMode;

    [Fact]
    public void WhenGateOneAdmits_TheNewSessionsSettingsSayAgenticAdvisoryAndWhy_AndTheKeyboardCanChooseMechanicalOnly()
    {
        ComposerGesture.Drive(_fixture, (window, document) =>
        {
            var ladder = GateOneAdmits();
            var config = NewSession(ladder);
            document.CompileModeLadder = () => ladder;
            document.Composer.Gate.BindSession(config.SessionId, config.CompileMode, "claude-code", "implement");

            var picker = OpenSettings(document);
            output.WriteLine($"open: selected {Selected(picker)} · why \"{Why(picker)}\" · items {Items(picker)}");
            Assert.Equal(CompileModes.AgenticAdvisory, Selected(picker));
            Assert.Contains("Gate 1 admits it", Why(picker), StringComparison.Ordinal);
            Assert.Equal("Compile mode", AutomationProperties.GetName(picker));
            Assert.Equal(AutomationControlType.ComboBox, UIElementAutomationPeer.CreatePeerForElement(picker).GetAutomationControlType());
            Assert.True(picker.IsEnabled && picker.Focusable, "the compile-mode control cannot take keyboard focus");
            Assert.False(Item(picker, CompileModes.Agentic).IsEnabled, "agentic is offered although Gate 2 refuses it");

            Key(picker, System.Windows.Input.Key.Up);
            output.WriteLine($"after Up: selected {Selected(picker)} · stored {StoredMode()} · gate {document.Composer.Gate.CompileMode} · why \"{Why(picker)}\"");
            Assert.Equal(CompileModes.MechanicalOnly, Selected(picker));
            Assert.Equal(CompileModes.MechanicalOnly, StoredMode());
            Assert.Equal(CompileModes.MechanicalOnly, document.Composer.Gate.CompileMode);

            Key(picker, System.Windows.Input.Key.Down);
            Assert.Equal(CompileModes.AgenticAdvisory, StoredMode());
            Assert.Equal(CompileModes.AgenticAdvisory, document.Composer.Gate.CompileMode);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public void WhenGateOneRefuses_TheSettingsSayMechanicalOnlyWithTheGatesReason_AndAdvisoryIsDisabled()
    {
        ComposerGesture.Drive(_fixture, (window, document) =>
        {
            var ladder = GateOneRefuses();
            var config = NewSession(ladder);
            document.CompileModeLadder = () => ladder;
            document.Composer.Gate.BindSession(config.SessionId, config.CompileMode, "claude-code", "implement");

            var picker = OpenSettings(document);
            output.WriteLine($"open: selected {Selected(picker)} · why \"{Why(picker)}\" · items {Items(picker)}");
            Assert.Equal(CompileModes.MechanicalOnly, Selected(picker));
            Assert.Contains(ladder.RefusalFor(CompileModes.AgenticAdvisory)!.Reason, Why(picker), StringComparison.Ordinal);
            var advisory = Item(picker, CompileModes.AgenticAdvisory);
            Assert.False(advisory.IsEnabled, "agentic-advisory is offered although Gate 1 refuses it");
            Assert.Contains(ladder.RefusalFor(CompileModes.AgenticAdvisory)!.Reason, AutomationProperties.GetHelpText(advisory), StringComparison.Ordinal);

            Key(picker, System.Windows.Input.Key.Down);
            Assert.Equal(CompileModes.MechanicalOnly, Selected(picker));
            Assert.Equal(CompileModes.MechanicalOnly, StoredMode());
            Assert.Equal(CompileModes.MechanicalOnly, document.Composer.Gate.CompileMode);
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// T30 n2 (A), the real settings: with no ladder injected, the settings evaluate the gate over the bound composer's
    /// adapter install root. Only the gate's input is injected (<see cref="SessionDocumentSurface.CompileModeGateInput"/>),
    /// so the test sees which root the gate was asked about and no test reads <c>~/.aide/proof</c>.
    /// </summary>
    [Fact]
    public void WithNoLadderInjected_TheSettingsEvaluateTheGateOverTheBoundAdapter_AndOfferAgenticAdvisory()
    {
        ComposerGesture.Drive(_fixture, (window, document) =>
        {
            var asked = new List<string>();
            document.CompileModeGateInput = root =>
            {
                asked.Add(root);
                return GateOneAdmits();
            };
            var config = NewSession(GateOneAdmits());
            document.Composer.Gate.BindSession(config.SessionId, config.CompileMode, "claude-code", "implement");

            var picker = OpenSettings(document);
            output.WriteLine($"open: bound adapter {document.Composer.AdapterInstallRoot} · gate asked about [{string.Join(", ", asked)}] · selected {Selected(picker)} · why \"{Why(picker)}\" · items {Items(picker)}");
            Assert.Equal(Path.Combine(_fixture.Root, "install"), document.Composer.AdapterInstallRoot);
            Assert.Contains(document.Composer.AdapterInstallRoot, asked);
            Assert.True(Item(picker, CompileModes.AgenticAdvisory).IsEnabled, "agentic-advisory is disabled although the gate over the bound adapter admits it");
            Assert.Contains("Gate 1 admits it", Why(picker), StringComparison.Ordinal);
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// T30 n2 (C), DC-NEW-wl-t30-prefill: the settings query reads this test's own window only. Two concurrent windows are
    /// forced open first, each with an open popup holding a control named <i>Compile mode</i>: one on another STA thread
    /// (another test's, in the whole suite) and one on this thread. The control found must be this document's.
    /// </summary>
    [Fact]
    public void TheSettingsQuery_ReadsThisTestsOwnWindow_WithConcurrentWindowsOpen()
    {
        using var otherThread = ConcurrentWindow.OnAnotherThread();
        ComposerGesture.Drive(_fixture, (window, document) =>
        {
            var config = NewSession(GateOneAdmits());
            document.CompileModeLadder = GateOneAdmits;
            document.Composer.Gate.BindSession(config.SessionId, config.CompileMode, "claude-code", "implement");
            var sameThread = ConcurrentWindow.Open();
            try
            {
                var picker = OpenSettings(document);
                var owner = OwnerOf(picker);
                output.WriteLine($"found the \"Compile mode\" control of window \"{owner?.Title}\" (this test's: \"{window.Title}\")");
                Assert.True(ReferenceEquals(window, owner), $"the settings query read another window's control: \"{owner?.Title}\"");
            }
            finally
            {
                sameThread.Close();
            }

            return Task.CompletedTask;
        });
    }

    /// <summary>The window a control in an open popup belongs to: the popup's placement target's window.</summary>
    private static Window? OwnerOf(FrameworkElement element)
    {
        DependencyObject? node = element;
        while (node is not null and not System.Windows.Controls.Primitives.Popup)
        {
            node = LogicalTreeHelper.GetParent(node) ?? System.Windows.Media.VisualTreeHelper.GetParent(node);
        }

        return node is System.Windows.Controls.Primitives.Popup { PlacementTarget: { } target } ? Window.GetWindow(target) : null;
    }

    /// <summary>A window with an open popup that holds a combo box named <i>Compile mode</i> — another test's window, forced.</summary>
    private sealed class ConcurrentWindow : IDisposable
    {
        private readonly Thread? _thread;
        private readonly System.Windows.Threading.Dispatcher? _dispatcher;

        private ConcurrentWindow(Thread thread, System.Windows.Threading.Dispatcher dispatcher)
        {
            _thread = thread;
            _dispatcher = dispatcher;
        }

        /// <summary>Opens one on this thread and returns it; the caller closes it.</summary>
        public static Window Open()
        {
            var anchor = new Button { Content = "Session settings" };
            var window = new Window { Title = "a concurrent window", Content = anchor, Width = 300, Height = 200, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false };
            window.Show();
            var combo = new ComboBox();
            AutomationProperties.SetName(combo, "Compile mode");
            _ = new System.Windows.Controls.Primitives.Popup { PlacementTarget = anchor, Child = combo, StaysOpen = true, IsOpen = true };
            ComposerGesture.Settle(anchor);
            return window;
        }

        /// <summary>Opens one on its own STA thread, held open until disposed.</summary>
        public static ConcurrentWindow OnAnotherThread()
        {
            var ready = new ManualResetEventSlim();
            System.Windows.Threading.Dispatcher? dispatcher = null;
            var thread = new Thread(() =>
            {
                Open();
                dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                ready.Set();
                System.Windows.Threading.Dispatcher.Run();
            }) { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.True(ready.Wait(TimeSpan.FromSeconds(30)), "the concurrent window on another thread did not open");
            return new ConcurrentWindow(thread, dispatcher!);
        }

        public void Dispose()
        {
            _dispatcher?.InvokeShutdown();
            _thread?.Join(TimeSpan.FromSeconds(10));
        }
    }

    /// <summary>The header's <i>Session settings</i> button through its invoke peer, then the open popup's compile-mode control.</summary>
    private static ComboBox OpenSettings(SessionDocumentSurface document)
    {
        var button = ComposerGesture.Logical<Button>(document).Single(b => AutomationProperties.GetName(b) == "Session settings");
        ((IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)!).Invoke();
        ComposerGesture.Settle(document);
        // THIS THREAD'S SOURCES ONLY: CurrentSources is process-wide, and in the whole suite it holds other STA threads'
        // windows, whose RootVisual throws on this thread (DC-NEW-wl-t30-prefill).
        var popups = PresentationSource.CurrentSources.OfType<HwndSource>().Where(s => s.CheckAccess()).Select(s => s.RootVisual).OfType<FrameworkElement>().ToList();
        var picker = popups.SelectMany(ComposerGesture.Logical<ComboBox>).Concat(popups.SelectMany(ThreadFixtureVisuals))
            .FirstOrDefault(c => AutomationProperties.GetName(c) == "Compile mode");
        Assert.True(picker is not null, "the open session settings carry no control named \"Compile mode\"");
        picker!.Focus();
        return picker;
    }

    private static IEnumerable<ComboBox> ThreadFixtureVisuals(FrameworkElement root) =>
        AiDe.App.Tests.Sessions.Thread.ThreadFixtures.Visuals<ComboBox>(root);

    /// <summary>An arrow key on the control, routed as the keyboard routes it.</summary>
    private static void Key(ComboBox picker, Key key)
    {
        picker.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(picker), 0, key) { RoutedEvent = Keyboard.KeyDownEvent });
        ComposerGesture.Settle(picker);
    }

    private static string? Selected(ComboBox picker) => (picker.SelectedItem as ComboBoxItem)?.Tag as string;

    private static ComboBoxItem Item(ComboBox picker, string mode) =>
        picker.Items.OfType<ComboBoxItem>().Single(i => string.Equals(i.Tag as string, mode, StringComparison.Ordinal));

    private static string Items(ComboBox picker) =>
        string.Join(" | ", picker.Items.OfType<ComboBoxItem>().Select(i => $"{i.Content}{(i.IsEnabled ? string.Empty : " (disabled)")}"));

    /// <summary>The row's stated reason: the control's UIA help text, which is the visible line beneath it.</summary>
    private static string Why(ComboBox picker) => AutomationProperties.GetHelpText(picker);
}
