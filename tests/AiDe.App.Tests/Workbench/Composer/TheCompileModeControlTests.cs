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
