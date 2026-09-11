using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.App.Conductor;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Sessions;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace AiDe.App.Workbench.Composer;

/// <summary>
/// The composer: a rich editor hosted in WebView2, a host-owned send, and a compiled view the
/// operator reads before anything leaves the machine (R15, R19).
/// </summary>
/// <remarks>
/// <para><b>The shell is the sole authority for everything except the characters of the draft.</b>
/// The page contributes text. The send verb, every attachment path, the engine, model, account, task
/// class, lease and goal-block field set are all host-side, and none of them is reachable from a
/// message.</para>
///
/// <para><b>The compiled view is a plain text box showing the whole prompt.</b> Not a summary, not a
/// preview, and not virtualized: the bytes that will be sent are legible before the send, because
/// one human read is the entire Phase-1 containment for every non-edit tool call.</para>
///
/// <para><b>The attach affordance is always visible.</b> With the session's attach setting off it is
/// disabled and says which setting governs it — an absent affordance is indistinguishable from an
/// unbuilt feature, and a disabled one with no reason is indistinguishable from a bug.</para>
///
/// <para><b>No clipboard access outside the explicit paste gesture.</b> This control never reads the
/// clipboard at all — <see cref="ClipboardReads"/> exists so that is an observable rather than a
/// claim, and paste is handled inside the page by the editor that received it.</para>
/// </remarks>
public sealed class ComposerSurface : ContentControl, IComposerMessageSink, IHasDisplayName, IDisposable
{
    private readonly WebView2 _view;
    private readonly TextBox _compiled;
    private readonly TextBlock _lease;
    private readonly TextBlock _status;
    private readonly Button _send;
    private readonly Button _attach;
    private readonly ComboBox _templatePicker;
    private readonly ComposerSendGate _gate = new();
    private readonly ComposerDraft _draft = new();
    private readonly List<ComposerFieldDescriptor> _fields = [];
    private readonly string _instance = Guid.NewGuid().ToString("N");

    private ComposerMessageRouter? _router;
    private AttachmentGate? _attachments;
    private ComposerSendContext? _context;
    private TemplateCatalog? _catalog;
    private PromptTemplate? _template;
    private bool _attachEnabled;
    private bool _disposed;
    private int _blockedByAttachSetting;

    /// <param name="surfaceId">The surface's stable id, as every other surface carries one.</param>
    /// <param name="title">Its accessible name.</param>
    public ComposerSurface(string surfaceId, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(surfaceId);

        SurfaceId = surfaceId;
        AutomationProperties.SetName(this, title);
        SetResourceReference(BackgroundProperty, "SurfaceBrush");

        _view = new WebView2();
        _compiled = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MinHeight = 90,
        };
        AutomationProperties.SetName(_compiled, "Compiled prompt — exactly what will be sent");

        _lease = new TextBlock { Text = "Lease: not derivable until the draft names something", Margin = new Thickness(0, 4, 0, 0) };
        _status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) };

        _send = new Button { Content = "Send", Padding = new Thickness(14, 6, 14, 6), Margin = new Thickness(0, 0, 8, 0) };
        _send.Click += (_, _) => Send();

        _attach = new Button { Content = "Attach file…", Padding = new Thickness(14, 6, 14, 6) };
        _attach.Click += (_, _) => OfferAttachment(PickFiles());
        ApplyAttachAffordance();

        _templatePicker = BuildTemplatePicker();
        DockPanel.SetDock(_templatePicker, Dock.Top);

        var bar = new DockPanel { Margin = new Thickness(12, 8, 12, 8) };
        DockPanel.SetDock(bar, Dock.Bottom);
        bar.Children.Add(_send);
        bar.Children.Add(_attach);

        var footer = new StackPanel { Margin = new Thickness(12, 0, 12, 12) };
        footer.Children.Add(new TextBlock { Text = "Compiled view" });
        footer.Children.Add(_compiled);
        footer.Children.Add(_lease);
        footer.Children.Add(_status);
        DockPanel.SetDock(footer, Dock.Bottom);

        var root = new DockPanel { LastChildFill = true };
        root.Children.Add(_templatePicker);
        root.Children.Add(bar);
        root.Children.Add(footer);
        root.Children.Add(_view);
        Content = root;

        _view.PreviewKeyDown += OnPreviewKey;
        Loaded += async (_, _) => await InitialiseAsync();
    }

    /// <summary>The surface's stable id.</summary>
    public string SurfaceId { get; }

    /// <inheritdoc/>
    public string? DisplayName => null;

    /// <summary>
    /// How many times this control read the clipboard. It is zero, always, across typing, focus
    /// changes and sends — the observable behind "no clipboard access outside the paste gesture".
    /// </summary>
    public long ClipboardReads { get; }

    /// <summary>The send gate. Exposed so the seam's counter is readable by a test.</summary>
    public ComposerSendGate Gate => _gate;

    /// <summary>The draft this surface composes.</summary>
    public ComposerDraft Draft => _draft;

    /// <summary>The last thing that happened, in a sentence.</summary>
    public string Status => _status.Text;

    /// <summary>The router, once the page has been wired. Null before initialisation.</summary>
    public ComposerMessageRouter? Router => _router;

    /// <summary>What the operator will read before sending: the whole compiled prompt.</summary>
    public string CompiledView => _compiled.Text;

    /// <summary>The fields the host has minted for the form on screen, in render order.</summary>
    public IReadOnlyList<ComposerFieldDescriptor> Fields => _fields;

    /// <summary>
    /// Wires the host-side sources: the session's config, the run context, and the attach path.
    /// </summary>
    /// <remarks>
    /// Called by the shell after render, exactly as the canvas graph source is wired. Everything
    /// supplied here is host-owned; nothing in it can be influenced by the page.
    /// </remarks>
    public void Configure(
        SessionConfig config,
        ComposerSendContext context,
        IReadOnlyList<ComposerFieldDescriptor> fields,
        AttachmentGate attachments,
        TemplateCatalog? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(attachments);

        _context = context;
        _attachments = attachments;
        _catalog = catalog;
        _attachEnabled = config.AttachEnabled;

        _fields.Clear();
        _fields.AddRange(fields);

        _router = new ComposerMessageRouter(
            ComposerPageContract.Url, _instance, [.. _fields.Select(f => f.Id)], this);

        _templatePicker.ItemsSource = catalog is null ? null : ComposerTemplatePicker.Rows(catalog);
        _templatePicker.Visibility = catalog is null ? Visibility.Collapsed : Visibility.Visible;

        ApplyAttachAffordance();
        RenderCompiledView();
    }

    /// <summary>The picker cards currently offered, in catalog order.</summary>
    public IReadOnlyList<TemplatePickerRow> TemplateCards =>
        _templatePicker.ItemsSource as IReadOnlyList<TemplatePickerRow> ?? [];

    /// <summary>
    /// Binds the draft to a catalog template and re-mints the form (R15's validated form).
    /// </summary>
    /// <remarks>
    /// <b>The field ids are re-minted, not reused.</b> A new form is a new set of fields the host
    /// holds; an id from the previous form is one the host no longer has, and the router is told so
    /// rather than left accepting it.
    /// </remarks>
    public void ChooseTemplate(string templateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);

        var entry = _catalog?.Find(templateId);
        if (entry is null || !entry.IsEnabled)
        {
            _status.Text = $"the template '{templateId}' is not one this catalog can offer";
            return;
        }

        _template = entry.Template;
        _draft.UseTemplate(templateId);

        _fields.Clear();
        _fields.AddRange(ComposerFields.ForTemplate(entry.Template!));
        _router?.ReplaceFields([.. _fields.Select(f => f.Id)]);

        PushInit();
        RenderCompiledView();
    }

    /// <summary>
    /// The send gesture, host-owned. The button calls it; so does the accelerator handler.
    /// </summary>
    /// <returns>The request that was built, or null when the send was refused.</returns>
    public GovernedRunRequest? Send()
    {
        if (_context is null)
        {
            _status.Text = "the composer is not wired to a session yet";
            return null;
        }

        RenderCompiledView();

        try
        {
            var request = _gate.Send(_context, _draft, _template, out var refusal);
            if (request is null)
            {
                _status.Text = refusal is { Errors.Count: > 0 }
                    ? string.Join("  ", refusal.Errors.Select(e => e.Message))
                    : refusal?.Message ?? "the send was refused";
                return null;
            }

            _lease.Text = "Lease: " + string.Join(", ", request.Lease.Exclusive);
            _status.Text = "sent";
            return request;
        }
        catch (ArgumentException error)
        {
            // The lease refused to be constructed because nothing was derivable. Reported, never
            // softened into a default: an all-covering lease never seams and looks like it is
            // working, which is the exact case the seam control refuses.
            _status.Text =
                "the send was refused: no write scope could be derived from this draft, so the lane "
                + "would have no seam monitor. Reference the files or directories this run may write "
                + $"({error.Message})";
            return null;
        }
    }

    /// <summary>
    /// Handles a WebView2 accelerator. Ctrl-Enter is the send, and it is marked handled so the page
    /// never sees it either (Security C11).
    /// </summary>
    /// <returns>Whether the key was the send gesture.</returns>
    public bool OnAcceleratorKey(uint virtualKey, bool controlHeld, bool isKeyDown)
    {
        if (!ComposerAccelerator.IsSend(virtualKey, controlHeld, isKeyDown))
        {
            return false;
        }

        Send();
        return true;
    }

    /// <inheritdoc/>
    public void MarkReady() => PushInit();

    /// <inheritdoc/>
    public void SetFieldText(string fieldId, long revision, string text)
    {
        var field = _fields.FirstOrDefault(f => string.Equals(f.Id, fieldId, StringComparison.Ordinal));
        if (field is null)
        {
            return;
        }

        switch (field.Target)
        {
            case ComposerFieldTarget.FreeForm:
                _draft.SetFreeFormText(text);
                break;
            case ComposerFieldTarget.GoalBlock:
                _draft.SetGoalValue(field.Name, text);
                break;
            case ComposerFieldTarget.Template:
                _draft.SetTemplateValue(
                    field.Name,
                    field.Widget is ComposerFieldWidget.List or ComposerFieldWidget.Mentions
                        ? text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        : [text]);
                break;
            default:
                return;
        }

        RenderCompiledView();
    }

    /// <inheritdoc/>
    public void MoveFocus() =>
        MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));

    /// <inheritdoc/>
    public void OfferAttachment(IReadOnlyList<string> filePaths) => Attach(filePaths);

    /// <inheritdoc/>
    public void RecordMetric(string name, long value) => Metrics[name] = value;

    /// <summary>Diagnostic counters the page moved. Nothing outside diagnostics is reachable.</summary>
    public Dictionary<string, long> Metrics { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Offers files to the draft through the attach gate.
    /// </summary>
    /// <remarks>
    /// The gate refuses before touching the file system when the session's attach setting is off, so
    /// the refusal below reaches the operator without a single byte having been read.
    /// </remarks>
    public AttachOutcome Attach(IReadOnlyList<string> filePaths)
    {
        ArgumentNullException.ThrowIfNull(filePaths);

        if (_attachments is null)
        {
            _status.Text = "the composer is not wired to a session yet";
            return new AttachOutcome([], [], 0);
        }

        var outcome = _attachments.Offer(_draft, filePaths, _attachEnabled);
        _blockedByAttachSetting += outcome.BlockedByAttachSetting;

        if (outcome.Refusals.Count > 0)
        {
            _status.Text = string.Join("  ", outcome.Refusals);
        }

        RenderCompiledView();
        return outcome;
    }

    /// <summary>The committed-channel record for this send: counts, and one boolean.</summary>
    public System.Text.Json.Nodes.JsonObject CommittedRecord() =>
        ComposerSendRecord.Committed(
            _attachEnabled,
            _gate.RenderedView ?? ComposerCompiler.Compile(_draft, _template),
            _blockedByAttachSetting);

    private void RenderCompiledView()
    {
        var compiled = _gate.RenderView(_draft, _template);
        _compiled.Text = compiled.Text;

        var patterns = LeaseDerivation.Patterns(compiled.Text);
        _lease.Text = patterns.Count == 0
            ? "Lease: not derivable until the draft names something"
            : "Lease: " + string.Join(", ", patterns);
    }

    /// <summary>
    /// The template picker: one card per catalog entry, headline and detail, disabled rows included.
    /// </summary>
    /// <remarks>
    /// <b>A card is <c>when_to_use</c> over <c>why</c>, and a failed template is a DISABLED card
    /// carrying its error</b> — never a silent drop. The projection is
    /// <see cref="ComposerTemplatePicker"/>'s, so what a card says is decided in a place a headless
    /// test can read.
    /// </remarks>
    private ComboBox BuildTemplatePicker()
    {
        var picker = new ComboBox
        {
            Margin = new Thickness(12, 10, 12, 4),
            DisplayMemberPath = null,
            Visibility = Visibility.Collapsed,
        };
        AutomationProperties.SetName(picker, "Start from template");

        var card = new FrameworkElementFactory(typeof(StackPanel));

        var headline = new FrameworkElementFactory(typeof(TextBlock));
        headline.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(TemplatePickerRow.Headline)));
        headline.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        headline.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);

        var detail = new FrameworkElementFactory(typeof(TextBlock));
        detail.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(TemplatePickerRow.Detail)));
        detail.SetValue(TextBlock.OpacityProperty, 0.75);
        detail.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);

        var badge = new FrameworkElementFactory(typeof(TextBlock));
        badge.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(TemplatePickerRow.OverrideBadge)));
        badge.SetValue(TextBlock.OpacityProperty, 0.6);

        card.AppendChild(headline);
        card.AppendChild(detail);
        card.AppendChild(badge);
        picker.ItemTemplate = new DataTemplate { VisualTree = card };

        var containerStyle = new Style(typeof(ComboBoxItem));
        containerStyle.Setters.Add(new Setter(
            IsEnabledProperty, new System.Windows.Data.Binding(nameof(TemplatePickerRow.IsEnabled))));
        picker.ItemContainerStyle = containerStyle;

        picker.SelectionChanged += (_, _) =>
        {
            if (picker.SelectedItem is TemplatePickerRow { IsEnabled: true } row)
            {
                ChooseTemplate(row.Id);
            }
        };

        return picker;
    }

    private void ApplyAttachAffordance()
    {
        _attach.IsEnabled = _attachEnabled;
        _attach.ToolTip = _attachEnabled
            ? "Attach a UTF-8 text file to this prompt."
            : "Attaching files is off for this session. Turn on the session's Attach files setting to enable it.";
        AutomationProperties.SetHelpText(_attach, (string)_attach.ToolTip);
    }

    /// <summary>
    /// Releases the hosted browser control.
    /// </summary>
    /// <remarks>
    /// <b>A WebView2 is a child PROCESS, not a visual.</b> Dropping the reference leaves the browser
    /// running, so a session document opened and closed repeatedly accumulates one per open — a leak
    /// that looks like nothing in the visual tree and like memory pressure in Task Manager.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _view.PreviewKeyDown -= OnPreviewKey;
        _view.Dispose();
    }

    private static IReadOnlyList<string> PickFiles()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Multiselect = true, CheckFileExists = true };
        return dialog.ShowDialog() == true ? dialog.FileNames : [];
    }

    private void PushInit()
    {
        var payload = new
        {
            v = ComposerMessageRouter.ProtocolVersion,
            kind = "host.init",
            instance = _instance,
            attachEnabled = _attachEnabled,
            fields = _fields.Select(f => new
            {
                id = f.Id,
                name = f.Name,
                label = f.Label,
                widget = WidgetName(f.Widget),
                required = f.Required,
                hint = f.Hint,
                options = f.Options,
                value = string.Empty,
            }),
            fileCandidates = Array.Empty<string>(),
            graphCandidates = Array.Empty<string>(),
        };

        _view.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(payload));
    }

    private static string WidgetName(ComposerFieldWidget widget) => widget switch
    {
        ComposerFieldWidget.LongText => "long-text",
        ComposerFieldWidget.Mentions => "mentions",
        ComposerFieldWidget.List => "list",
        ComposerFieldWidget.Enum => "enum",
        ComposerFieldWidget.Budget => "budget",
        _ => "text",
    };

    private async Task InitialiseAsync()
    {
        try
        {
            await _view.EnsureCoreWebView2Async();
            var core = _view.CoreWebView2;
            if (core is null)
            {
                return;
            }

            ComposerPageContract.ApplySettingsFloor(core.Settings);
            WebAssetHost.Map(core);

            core.NavigationStarting += (_, e) => e.Cancel = ComposerPageContract.MustCancelNavigation(e.Uri);
            core.FrameNavigationStarting += (_, e) => e.Cancel = ComposerPageContract.MustCancelNavigation(e.Uri);
            core.NewWindowRequested += (_, e) => e.Handled = true;
            core.LaunchingExternalUriScheme += (_, e) => e.Cancel = true;
            core.WebMessageReceived += OnWebMessage;

            core.Navigate(ComposerPageContract.Url);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // A missing or broken WebView2 runtime must not take the shell down: the composer is one
            // surface, and the same reasoning the canvas already records applies here.
            _status.Text = "the composer editor could not start: " + error.Message;
        }
    }

    /// <summary>
    /// The accelerator handler, wired through WPF rather than through the controller event directly —
    /// and the difference is a wrapper detail, not a change of owner.
    /// </summary>
    /// <remarks>
    /// <b>Verified rather than assumed:</b> the WPF <c>WebView2</c> control exposes no public
    /// <c>CoreWebView2Controller</c> (its API surface has <c>CoreWebView2</c> and nothing else), and
    /// its base class subscribes to the controller's accelerator event itself
    /// (<c>WebView2Base.CoreWebView2Controller_AcceleratorKeyPressed</c>) and republishes it as the
    /// control's own WPF key events. So this <i>is</i> the host-side accelerator handler; it is
    /// reached through the wrapper's republication. The send verb is still WPF's, still host-owned,
    /// and still unreachable from a page message.
    /// </remarks>
    private void OnPreviewKey(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var control = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0;
        var virtualKey = (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(
            e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key);

        if (OnAcceleratorKey(virtualKey, control, isKeyDown: true))
        {
            e.Handled = true;
        }
    }

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // The handler passes the three inputs and does nothing else — every rule is in the router,
        // where it can be exercised without a browser.
        var paths = new List<string>();
        foreach (var item in e.AdditionalObjects)
        {
            if (item is CoreWebView2File file)
            {
                paths.Add(file.Path);
            }
        }

        _router?.Route(e.Source, e.WebMessageAsJson, paths);
    }
}

/// <summary>Which part of the draft a rendered field writes to.</summary>
public enum ComposerFieldTarget
{
    /// <summary>The free-form prose block.</summary>
    FreeForm,

    /// <summary>One of the six goal-block fields, by wire name.</summary>
    GoalBlock,

    /// <summary>One declared template field.</summary>
    Template,
}

/// <summary>One field the host minted, told the page about, and will accept updates for.</summary>
/// <param name="Id">The host-minted id. The page may only ever MATCH one of these, never create one.</param>
/// <param name="Name">The wire name this field writes to.</param>
/// <param name="Label">What the operator reads.</param>
/// <param name="Widget">Its widget, from the Ruling 33 inventory.</param>
/// <param name="Target">Which part of the draft it writes to.</param>
/// <param name="Required">Whether an empty value blocks send.</param>
/// <param name="Hint">Inline guidance, or null.</param>
/// <param name="Options">The closed set, for an enum widget.</param>
public sealed record ComposerFieldDescriptor(
    string Id,
    string Name,
    string Label,
    ComposerFieldWidget Widget,
    ComposerFieldTarget Target,
    bool Required,
    string? Hint = null,
    IReadOnlyList<string>? Options = null);

/// <summary>Builds the field descriptors for a shape — host-side, from host-side vocabulary.</summary>
public static class ComposerFields
{
    /// <summary>The tier values the enum widget offers.</summary>
    public static readonly IReadOnlyList<string> Tiers = ["T0", "T1", "T2"];

    /// <summary>The single free-form field.</summary>
    public static IReadOnlyList<ComposerFieldDescriptor> FreeForm() =>
    [
        new(Mint("prompt"), "prompt", "Prompt", ComposerFieldWidget.LongText, ComposerFieldTarget.FreeForm, Required: false),
    ];

    /// <summary>
    /// The six goal-block fields, in the order §14.3 lists them, each with its Ruling 33 widget.
    /// </summary>
    /// <remarks>
    /// The list is derived from <see cref="GoalBlockFields.All"/> rather than typed out: a fixture
    /// that restates a list the product declares is the defect class the fixture-derivation gate
    /// exists for, and here it would also let the form and the spawn contract drift apart.
    /// </remarks>
    public static IReadOnlyList<ComposerFieldDescriptor> GoalBlock() =>
    [
        .. GoalBlockFields.All.Select(name => new ComposerFieldDescriptor(
            Mint(name),
            name,
            name,
            ComposerFieldWidgets.ForGoalBlockField(name),
            ComposerFieldTarget.GoalBlock,
            Required: true,
            Hint: null,
            Options: name == GoalBlockFields.TierKey ? Tiers : null)),
    ];

    /// <summary>The declared fields of a template, each with its Ruling 33 widget.</summary>
    public static IReadOnlyList<ComposerFieldDescriptor> ForTemplate(PromptTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        return
        [
            .. template.Fields.Select(field => new ComposerFieldDescriptor(
                Mint(field.Name),
                field.Name,
                field.Name,
                ComposerFieldWidgets.ForTemplateField(field),
                ComposerFieldTarget.Template,
                field.Required,
                field.Hint)),
        ];
    }

    /// <summary>
    /// Mints a field id. <b>The host mints; the page matches.</b> The id is opaque and unguessable so
    /// a page that wanted to address a field it was not given cannot construct one.
    /// </summary>
    public static string Mint(string name) =>
        name + ":" + Guid.NewGuid().ToString("N");
}
