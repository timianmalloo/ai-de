using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace AiDe.App.Workbench;

/// <summary>
/// The prompt-draft surface (spec-editor-surfaces US-ED5–ED8): a staged compose pane whose draft is
/// never sent by editing (US-ED5), and whose one explicit <b>Transfer</b> delivers it to a chosen
/// ready terminal session (US-ED6) one-way (US-ED7). The transfer rules live in
/// <see cref="PromptDraftViewModel"/>; this control renders them and persists the body across restart
/// via an injected save (US-ED5). The shell calls <see cref="Configure"/> after render to supply the
/// live ready-target list and the dispatch, exactly as it wires the canvas graph source.
/// </summary>
public sealed class PromptDraftSurface : ContentControl
{
    private readonly TextBox _text;
    private readonly ComboBox _target;
    private readonly Button _transfer;
    private readonly TextBlock _blocked;
    private readonly TextBlock _confirm;
    private readonly TextBlock _saved;
    private readonly Border _staged;

    private PromptDraftViewModel _vm;
    private Action<string>? _persist;
    private bool _suppress;

    public PromptDraftSurface(string surfaceId, string title)
    {
        SurfaceId = surfaceId;
        AutomationProperties.SetName(this, title);
        SetResourceReference(BackgroundProperty, "SurfaceBrush");

        // Until the shell wires it, there are no targets and dispatch is a no-op — the surface still
        // renders and stages text; transfer is simply blocked with a reason (US-ED6).
        _vm = new PromptDraftViewModel(() => [], (_, _) => Task.FromResult(false));

        var root = new DockPanel { LastChildFill = true };

        // Header: staged badge + saved note.
        var header = new DockPanel { Margin = new Thickness(12, 10, 12, 8) };
        _staged = Pill("staged — not sent", "VerifiedBrush");
        DockPanel.SetDock(_staged, Dock.Left);
        header.Children.Add(_staged);
        _saved = Muted("");
        _saved.HorizontalAlignment = HorizontalAlignment.Right;
        header.Children.Add(_saved);
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        // Footer bar: target picker + transfer + blocked reason.
        var bar = new DockPanel { Margin = new Thickness(12, 8, 12, 12) };
        DockPanel.SetDock(bar, Dock.Bottom);

        _transfer = new Button
        {
            Content = "Transfer \u2192",
            Padding = new Thickness(14, 6, 14, 6),
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        AutomationProperties.SetName(_transfer, "Transfer prompt to the selected session");
        _transfer.Click += async (_, _) => await TransferAsync();
        DockPanel.SetDock(_transfer, Dock.Right);
        bar.Children.Add(_transfer);

        _target = new ComboBox { MinWidth = 130, Margin = new Thickness(0, 0, 8, 0), DisplayMemberPath = nameof(PromptTarget.Title) };
        AutomationProperties.SetName(_target, "Target session");
        _target.SelectionChanged += (_, _) =>
        {
            _vm.SelectedTargetId = (_target.SelectedItem as PromptTarget)?.SessionId;
        };
        DockPanel.SetDock(_target, Dock.Left);
        var toLabel = Muted("to");
        toLabel.Margin = new Thickness(0, 0, 6, 0);
        toLabel.VerticalAlignment = VerticalAlignment.Center;
        DockPanel.SetDock(toLabel, Dock.Left);
        bar.Children.Add(toLabel);
        bar.Children.Add(_target);

        _blocked = Muted("");
        _blocked.VerticalAlignment = VerticalAlignment.Center;
        _blocked.TextWrapping = TextWrapping.Wrap;
        bar.Children.Add(_blocked);
        root.Children.Add(bar);

        // Confirmation strip (shown after transfer).
        _confirm = new TextBlock
        {
            Margin = new Thickness(12, 6, 12, 6),
            TextWrapping = TextWrapping.Wrap,
            Visibility = Visibility.Collapsed,
        };
        _confirm.SetResourceReference(TextBlock.ForegroundProperty, "VerifiedBrush");
        DockPanel.SetDock(_confirm, Dock.Bottom);
        root.Children.Add(_confirm);

        // The draft body.
        _text = new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(12, 10, 12, 10),
            FontSize = 13.5,
        };
        _text.SetResourceReference(BackgroundProperty, "SunkenBrush");
        AutomationProperties.SetName(_text, "Prompt draft");
        _text.TextChanged += (_, _) =>
        {
            if (_suppress) { return; }
            _vm.Body = _text.Text;
            _persist?.Invoke(_text.Text);
            _saved.Text = "saved with layout";
            Reflect();
        };
        root.Children.Add(_text);

        Content = root;
        Reflect();
    }

    public string SurfaceId { get; }

    /// <summary>The staged body (for tests / persistence).</summary>
    public string Body => _vm.Body;

    /// <summary>True once transferred (one-way).</summary>
    public bool Transferred => _vm.Transferred;

    /// <summary>
    /// Wires the surface to the shell: the live ready-target list, the dispatch, the initial body
    /// (restored from persistence), and the save callback. Rebuilds the view-model around them.
    /// </summary>
    public void Configure(
        Func<IReadOnlyList<PromptTarget>> readyTargets,
        Func<string, string, Task<bool>> dispatch,
        string? initialBody,
        Action<string>? persist)
    {
        _vm = new PromptDraftViewModel(readyTargets, dispatch);
        _persist = persist;

        if (!string.IsNullOrEmpty(initialBody))
        {
            _suppress = true;
            _text.Text = initialBody;
            _suppress = false;
            _vm.Body = initialBody;
        }

        RefreshTargets();
        Reflect();
    }

    /// <summary>Re-reads the live ready targets into the picker (call when sessions change).</summary>
    public void RefreshTargets()
    {
        var targets = _vm.Targets;
        var selected = (_target.SelectedItem as PromptTarget)?.SessionId ?? _vm.SelectedTargetId;
        _target.ItemsSource = targets;
        _target.SelectedItem = targets.FirstOrDefault(t => string.Equals(t.SessionId, selected, StringComparison.Ordinal))
            ?? targets.FirstOrDefault();
        Reflect();
    }

    private async Task TransferAsync()
    {
        if (!_vm.CanTransfer) { Reflect(); return; }
        var target = (_target.SelectedItem as PromptTarget)?.Title ?? "the session";
        var ok = await _vm.TransferAsync();
        if (ok)
        {
            _text.IsReadOnly = true;
            _text.Opacity = 0.5;
            _confirm.Text = $"Transferred to {target} — recorded as an audit prompt. The session owns it now.";
            _confirm.SetResourceReference(ForegroundProperty, "VerifiedBrush");
            _confirm.Visibility = Visibility.Visible;
        }
        else
        {
            // A rejected dispatch (the session was not ready / did not accept the write) is not silent —
            // the draft stays editable and transferable so the user can retry (US-ED6, no silent no-op).
            _confirm.Text = $"The transfer to {target} did not go through — the session may not be ready. The draft is unchanged; try again.";
            _confirm.SetResourceReference(ForegroundProperty, "TextMutedBrush");
            _confirm.Visibility = Visibility.Visible;
        }
        Reflect();
    }

    private void Reflect()
    {
        _transfer.IsEnabled = _vm.CanTransfer;
        _blocked.Text = _vm.CanTransfer ? "" : _vm.BlockedReason;
        _blocked.Visibility = string.IsNullOrEmpty(_blocked.Text) ? Visibility.Collapsed : Visibility.Visible;
        _staged.Visibility = _vm.Transferred ? Visibility.Collapsed : Visibility.Visible;
    }

    private static TextBlock Muted(string text)
    {
        var t = new TextBlock { Text = text, FontSize = 12 };
        t.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return t;
    }

    private static Border Pill(string text, string brushKey)
    {
        var t = new TextBlock { Text = text, FontSize = 11 };
        t.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
        var b = new Border
        {
            Child = t,
            Padding = new Thickness(8, 1, 8, 1),
            CornerRadius = new CornerRadius(999),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center,
        };
        b.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        return b;
    }
}
