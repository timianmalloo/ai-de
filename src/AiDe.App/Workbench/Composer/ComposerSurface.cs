using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.App.Conductor;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Workbench;
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
/// <para><b>The compiled prompt is a plain text box showing the whole prompt, on demand.</b> Not a
/// summary, not a preview, not a diff, and not virtualized (Ruling 57): the bytes that will be sent
/// are legible before the send, because one human read is the entire Phase-1 containment for every
/// non-edit tool call. It is collapsed at rest — the decoration line and the structure lines are
/// the reading surface (SC2), the bytes one disclosure away.</para>
///
/// <para><b>The composer is the current turn</b> (<c>DESIGN.md</c> SC1–SC6; CV-1): one message
/// editor (the page), then beneath it the structure lines (Goal · Done when · Not in scope — empty
/// and editable under <c>mechanical-only</c>), the decoration line in the thread's one grammar
/// (<i>This turn · class · tier · lease · shape [· template]</i>), the settings line the session's
/// ceilings derive, the compiled prompt, and the send row. No per-prompt tier, cap or budget field
/// exists (Rulings 56, 63, 72); the values are the session's and the compile step's.</para>
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
    /// <summary>
    /// How far the editor host's or the compiled view's height must move before another
    /// <c>composer.layout</c> line is written: below this a resize is a drag, not a change of state.
    /// </summary>
    private const double LayoutChangeThreshold = 24;

    private readonly WebSurfaceHost _host;
    private readonly WebView2 _view;
    private readonly TextBox _compiled;
    private readonly Expander _compiledDisclosure;
    private readonly Expander _structure;
    private readonly Dictionary<string, StructureLine> _structureLines = new(StringComparer.Ordinal);
    private readonly WrapPanel _decorationLine;
    private readonly TextBlock _settingsLine;
    private readonly DockPanel _compileRow;
    private readonly TextBlock _compileLine;
    private readonly Button _prepareAgain;
    private readonly Button _cancelPrepare;
    private readonly ComboBox _tierControl;
    private readonly ComboBox _classControl;
    private bool _renderingControls;
    private readonly TextBlock _status;
    private readonly IWorkbenchAnnouncer _announcer;
    private string _statusText = string.Empty;
    private double _beltHeight = double.PositiveInfinity;
    private double _editorRestHeight = double.PositiveInfinity;
    private readonly DockPanel _sendRow;
    private readonly StackPanel _lines;
    private readonly Button _send;
    private readonly Button _attach;
    private readonly Button _retry;
    private readonly ComboBox _templatePicker;
    private string _taskClass = AiDe.Core.Watcher.TaskClasses.FreeForm;
    private string _leaseLine = ComposerCompiler.LeaseLine(null);
    private TurnView? _inFlight;
    private TurnView? _queued;
    private string? _queuedSentence;
    private StatusKind _statusKind;
    private readonly ComposerSendGate _gate = new();
    private readonly ComposerDraft _draft = new();
    private readonly List<ComposerFieldDescriptor> _fields = [];
    private readonly string _instance = Guid.NewGuid().ToString("N");

    // BUILT WITH THE SURFACE, not with the session. The page posts `editor.ready` the moment it
    // mounts, which can be before the shell has called Configure — and a ready that arrives at a null
    // router is a mount the host never hears about, so nothing would ever push the first init.
    private readonly ComposerMessageRouter _router;

    private AttachmentGate? _attachments;
    private ComposerSendContext? _context;
    private TemplateCatalog? _catalog;
    private PromptTemplate? _template;
    private bool _attachEnabled;
    private bool _disposed;
    private bool _pageReady;
    private bool _configured;
    private int _blockedByAttachSetting;
    private int _navigations;
    private long _inputs;
    private bool _inputSinceInit;
    private bool _hasSentBefore;
    private double?[]? _lastHeights;
    private readonly HashSet<string> _dropsThisDocument = new(StringComparer.Ordinal);

    /// <param name="surfaceId">The surface's stable id, as every other surface carries one.</param>
    /// <param name="title">Its accessible name.</param>
    /// <param name="announcer">
    /// Where the status line is spoken (SC6 / SC9: a refusal is announced, never silent). The
    /// document passes its own so the page has one channel; null builds one over the status line
    /// itself — WPF raises no <c>LiveRegionChanged</c> on a text change, the app must.
    /// </param>
    public ComposerSurface(string surfaceId, string title, IWorkbenchAnnouncer? announcer = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(surfaceId);

        SurfaceId = surfaceId;
        AutomationProperties.SetName(this, title);
        SetResourceReference(BackgroundProperty, "SurfaceBrush");

        _compiled = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = CompiledPromptMaxHeight,
            // THE FLOOR IS ON THE BOX (Ruling 96): a floor that lives only in the parent's arithmetic
            // (MinimumHeight, the MaxHeight clamp) is not a floor the box desires — a one-line message
            // compiled to one line and the reader laid out at 29.89 px under its 48 (measured at the
            // operator's belt, 489.5 × 517.13). Collapsed, the box is never measured, so this costs
            // nothing at rest.
            MinHeight = CompiledPromptMinHeight,
            FontFamily = AiDe.App.Workbench.Sessions.ThreadFeed.Mono,
            FontSize = 12,
        };
        AutomationProperties.SetName(_compiled, "Compiled prompt — exactly what will be sent");

        _status = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
        _status.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        // ONE CHANNEL. With the document's announcer the status line is a visible mirror and the
        // document's live region speaks; without one the status line IS the live region (the
        // announcer sets its LiveSetting and raises the event). Never both.
        _announcer = announcer ?? new WorkbenchAnnouncer(_status);
        AutomationProperties.SetName(_status, "Send status");

        // Built after the status line it reports into: a browser that cannot start says so there.
        // The host's own contract is "not retried on the next attach" (the runtime is picked up when
        // the surface is next constructed) — Retry is the operator's recourse in the meantime. A
        // named method, not an inline lambda, so a test can drive the failure directly rather than
        // needing a real broken WebView2 runtime.
        _host = new WebSurfaceHost(surfaceId, this, InitialiseAsync, OnHostInitFailed);
        _view = _host.View;

        // THE EDITOR'S FLOOR (DESIGN.md:1092; DS-1 seam 5, spike Q14): in an Auto row the composer
        // arranges at its desired height, and a WebView2 desires nothing without a browser — the
        // editor host measured 0 px. The floor is what the composer DECLARES; the document belts it.
        _view.MinHeight = EditorFloor;
        AutomationProperties.SetName(_view, "Message");
        FocusTarget = new EditorFocusTarget(new CanvasFocusTarget(_view, static () => false), () => _pageReady);

        _send = new Button { Content = "Send", Padding = new Thickness(14, 6, 14, 6), MinHeight = 24, MinWidth = 24 };
        AutomationProperties.SetHelpText(_send, "Ctrl+Enter sends.");
        _send.Click += (_, _) => Send();

        _attach = new Button { Content = "Attach file…", Padding = new Thickness(10, 4, 10, 4), MinHeight = 24, MinWidth = 24, Margin = new Thickness(0, 0, 8, 0) };
        _attach.Click += (_, _) => OfferAttachment(PickFiles());
        ApplyAttachAffordance();

        // The mockup's editorerror Retry (session-conversation.html), placed in the send row
        // rather than replacing the editor the way the mockup draws it: the editor is a windowed
        // WebView2, and an overlay drawn where the mockup puts the error box would be an airspace
        // hazard the moment a retry succeeds and the browser paints there again (ADR-0015).
        // Shown only once the host reports init-failed, so it is a real recovery affordance rather
        // than a decoration nobody needs the rest of the time.
        _retry = new Button { Content = "Retry", Padding = new Thickness(10, 4, 10, 4), MinHeight = 24, MinWidth = 24, Margin = new Thickness(0, 0, 8, 0), Visibility = Visibility.Collapsed };
        _retry.Click += async (_, _) => await RetryEditorAsync();

        _templatePicker = BuildTemplatePicker();
        DockPanel.SetDock(_templatePicker, Dock.Top);

        // The send row: attach · the reason (a status) · Send (DESIGN.md's send row; Ruling 77's
        // refused gestures are announced here, never silent).
        _sendRow = new DockPanel { Margin = new Thickness(12, 4, 12, 10), LastChildFill = true };
        DockPanel.SetDock(_sendRow, Dock.Bottom);
        DockPanel.SetDock(_send, Dock.Right);
        DockPanel.SetDock(_attach, Dock.Left);
        _sendRow.Children.Add(_send);
        _sendRow.Children.Add(_attach);
        _sendRow.Children.Add(_retry);
        _sendRow.Children.Add(_status);

        // The tab order is the visual order — Attach · Retry (when shown) · the status (its link) ·
        // Send — not the children order the DockPanel's fill rule dictates (2.4.3).
        System.Windows.Input.KeyboardNavigation.SetTabIndex(_attach, 1);
        System.Windows.Input.KeyboardNavigation.SetTabIndex(_retry, 2);
        System.Windows.Input.KeyboardNavigation.SetTabIndex(_status, 3);
        System.Windows.Input.KeyboardNavigation.SetTabIndex(_send, 4);

        // The lines beneath the editor (DESIGN.md: 4 rows at rest, each 24 px): the structure,
        // this turn's decoration line, the settings line, the compiled prompt on demand.
        _structure = BuildStructure();
        _decorationLine = new WrapPanel { MinHeight = 24, Margin = new Thickness(0, 2, 0, 2) };
        AutomationProperties.SetName(_decorationLine, "This turn");
        _tierControl = BuildTierControl();
        _classControl = BuildClassControl();
        _settingsLine = new TextBlock { FontSize = 12, MinHeight = 24, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        _settingsLine.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        AutomationProperties.SetName(_settingsLine, "Settings");
        _compiledDisclosure = new Expander
        {
            Header = "Compiled prompt",
            IsExpanded = false,
            Content = _compiled,
            Margin = new Thickness(0, 2, 0, 0),
            Style = AiDe.App.Workbench.Sessions.ThreadFeed.DisclosureStyle(),
            Focusable = false,
            IsTabStop = false,
        };
        AutomationProperties.SetName(_compiledDisclosure, "Compiled prompt");
        AutomationProperties.SetHelpText(_compiledDisclosure, "exactly the bytes that will be sent; no diff, no comments");
        _compiledDisclosure.Expanded += (_, _) => CompiledPromptOpenChanged?.Invoke(true);
        _compiledDisclosure.Collapsed += (_, _) => CompiledPromptOpenChanged?.Invoke(false);

        // THE COMPILE LINE (§A10.2, §A11): absent under mechanical-only with nothing supplied (E5);
        // under an agentic rung it carries the `called` outcome's string in voice with its next-action
        // control — Cancel while preparing, Prepare again when the last call did not succeed or the
        // draft went stale.
        _compileLine = new TextBlock { FontSize = 12, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        _compileLine.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        AutomationProperties.SetName(_compileLine, "Compile");
        _prepareAgain = new Button { Content = "Prepare again", Padding = new Thickness(8, 2, 8, 2), MinHeight = 24, Margin = new Thickness(8, 0, 0, 0), Visibility = Visibility.Collapsed };
        AutomationProperties.SetHelpText(_prepareAgain, "Ask the model again for the open structure lines.");
        _prepareAgain.Click += (_, _) => Preparing = PrepareTurnAsync();
        _cancelPrepare = new Button { Content = "Cancel", Padding = new Thickness(8, 2, 8, 2), MinHeight = 24, Margin = new Thickness(8, 0, 0, 0), Visibility = Visibility.Collapsed };
        AutomationProperties.SetHelpText(_cancelPrepare, "Cancel — send without the model's lines.");
        _cancelPrepare.Click += (_, _) => _gate.CancelPrepare();
        _compileRow = new DockPanel { MinHeight = 24, LastChildFill = true, Visibility = Visibility.Collapsed };
        DockPanel.SetDock(_cancelPrepare, Dock.Right);
        DockPanel.SetDock(_prepareAgain, Dock.Right);
        _compileRow.Children.Add(_cancelPrepare);
        _compileRow.Children.Add(_prepareAgain);
        _compileRow.Children.Add(_compileLine);

        _lines = new StackPanel { Margin = new Thickness(12, 6, 12, 0) };
        _lines.Children.Add(_structure);
        _lines.Children.Add(_decorationLine);
        _lines.Children.Add(_compileRow);
        _lines.Children.Add(_settingsLine);
        _lines.Children.Add(_compiledDisclosure);
        DockPanel.SetDock(_lines, Dock.Bottom);

        var root = new DockPanel { LastChildFill = true };
        root.Children.Add(_templatePicker);
        root.Children.Add(_sendRow);
        root.Children.Add(_lines);
        root.Children.Add(_view);
        Content = root;

        // Rendered bounds are emitted on the normal path (IO1): the class survived as long as nothing
        // measured them.
        _view.SizeChanged += (_, _) => EmitLayout();
        _compiled.SizeChanged += (_, _) => EmitLayout();

        _router = new ComposerMessageRouter(ComposerPageContract.Url, _instance, [], this);

        _view.PreviewKeyDown += OnPreviewKey;
    }

    /// <summary>The editor host's floor (DESIGN.md:1092 ≥ 130 px) — what the composer declares under an infinite constraint (spike Q14).</summary>
    public const double EditorFloor = 130;

    /// <summary>The editor's rest height once the thread has turns (Ruling 80; DESIGN.md errata "Chat-like — editor height"): it scrolls only past this.</summary>
    public const double EditorRest = 280;

    /// <summary>The compiled prompt's ceiling when expanded (DESIGN.md:1109 ≤ 200 px, scrolls) — and it never takes the editor's floor (DC-137).</summary>
    public const double CompiledPromptMaxHeight = 200;

    /// <summary>
    /// The compiled prompt's floor when it is open: three lines of the mono face, so a reader the
    /// operator asked for is a reader (INV-0007's "the reader gets its share"). Under a constraint
    /// that cannot hold the floor, the editor's floor and this one both hold and the composer's
    /// minimum grows — the document's belt yields, the thread gets less; nothing is cut to 2 px.
    /// </summary>
    public const double CompiledPromptMinHeight = 48;

    /// <summary>A disclosure's header row (DESIGN.md: each line 24 px) — what the compiled prompt costs at rest.</summary>
    private const double DisclosureHeaderHeight = 24;

    /// <summary>The decoration source that earns the tilde and the inferred ink: a value the model proposed (CV-2's compile step). A rule's value is text.</summary>
    public const string ModelSource = "model";

    /// <summary>The editor as a focus region of the document's F6 cycle: SetFocus on the host HWND with a read-back (DS-1 seam 1).</summary>
    public ICanvasFocusTarget FocusTarget { get; }

    /// <summary>Raised once per page mount — after <see cref="MarkReady"/>; the document places focus in the editor on it (K7).</summary>
    public event Action? PageReady;

    /// <summary>Whether the compiled prompt disclosure is open — collapsed at rest (Ruling 57); the document owns the state across turns and a reopen (Ruling 96).</summary>
    public bool CompiledPromptOpen
    {
        get => _compiledDisclosure.IsExpanded;
        set => _compiledDisclosure.IsExpanded = value;
    }

    /// <summary>Raised when the disclosure opens or closes — by the operator's header or by <see cref="CompiledPromptOpen"/> — so the document can record the state it restores on reopen (Ruling 96).</summary>
    public event Action<bool>? CompiledPromptOpenChanged;

    /// <summary>The decoration rows this turn carries — the same projection the thread will show for it and the send gate will put on the wire (SC2; ADR-0033 rule 2).</summary>
    public IReadOnlyList<DecorationRow> Decorations =>
        ComposerCompiler.Decorations(_draft, _taskClass, _template, _context?.EngineId, _gate.SessionId, _gate.CompileMode);

    /// <summary>The tier control on the decoration line (E2): <i>rule</i>, T0, T1, T2 — an override is an <c>operator</c> row at Send (§A11).</summary>
    public ComboBox TierControl => _tierControl;

    /// <summary>The class control on the decoration line: the session's default or a class for this prompt (Ruling 70).</summary>
    public ComboBox ClassControl => _classControl;

    /// <summary>Why compile history is not being recorded, or null when it is — the reason Prepare shows (ADR-0034 rules 2–3).</summary>
    public string? HistoryState => _gate.HistoryState;

    /// <summary>The settings line as rendered: <i>fan-out cap 2 (ceiling 3) · budget: bounded by your subscription · from session settings</i>.</summary>
    public string SettingsLine => _settingsLine.Text;

    /// <summary>The structure lines' marks by wire name (<i>— fill in</i> · <i>edited</i>), for a test that reads the marks.</summary>
    public IReadOnlyDictionary<string, string> StructureMarks =>
        _structureLines.ToDictionary(pair => pair.Key, pair => pair.Value.Mark, StringComparer.Ordinal);

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
    public string Status => _statusText;

    /// <summary>
    /// Raised when the operator activates the in-flight turn's link in a refused-gesture reason
    /// (Ruling 77; SC8: the ordinal is a link to the turn) — the document focuses that turn's container.
    /// </summary>
    public event Action<int>? TurnRequested;

    /// <summary>
    /// The document's belt (DS-1 Q14): the height this composer may take before the compiled prompt
    /// yields — set by the document at its measure, its value <b>derived from the thread's need</b>
    /// (Ruling 80: the body less the empty caption at 0 turns, less the thread's minimum with turns;
    /// never a constant share). The composer's own minimum (its lines and the editor's floor) is
    /// never cut by it: a belt smaller than the minimum leaves the send row on screen and the thread
    /// shorter, not the send row clipped.
    /// </summary>
    public double BeltHeight
    {
        get => _beltHeight;
        set
        {
            if (!(value > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "the belt is a positive height");
            }

            if (_beltHeight != value)
            {
                _beltHeight = value;
                InvalidateMeasure();
            }
        }
    }

    /// <summary>
    /// The height the editor rests at when the belt allows (Ruling 80): <see cref="EditorRest"/>
    /// once the thread has turns — it scrolls only past that; unbounded, so the editor fills the
    /// belt, at 0 turns or with no document above it. Set by the document at its measure beside
    /// <see cref="BeltHeight"/>. The floor is never this: <see cref="EditorFloor"/> is what the
    /// editor keeps when the belt cannot give the rest.
    /// </summary>
    public double EditorRestHeight
    {
        get => _editorRestHeight;
        set
        {
            if (!(value > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "the rest is a positive height");
            }

            if (_editorRestHeight != value)
            {
                _editorRestHeight = value;
                InvalidateMeasure();
            }
        }
    }

    /// <summary>The composer's minimum height at its last measure: the lines, the picker, the send row and the editor's floor.</summary>
    public double MinimumHeight { get; private set; }

    /// <summary>The lease line: the read-only state, or the patterns (Ruling 73) — the decoration line's lease segment, prefixed.</summary>
    public string LeaseLine => _leaseLine;

    /// <summary>Whether <see cref="Configure"/> has run — a bound composer is not bound again (INV-0009 Phase 2).</summary>
    public bool IsConfigured => _configured;

    /// <summary>The router. Built with the surface, so a mount is heard before the session is wired.</summary>
    public ComposerMessageRouter Router => _router;

    /// <summary>Whether the page has reported that it mounted.</summary>
    public bool PageIsReady => _pageReady;

    /// <summary>How many times the browser was initialised. The contract is 1, across every re-parent.</summary>
    internal int InitialisationsStarted => _host.InitialisationsStarted;

    /// <summary>What the operator will read before sending: the whole compiled prompt.</summary>
    public string CompiledView => _compiled.Text;

    /// <summary>The fields the host has minted for the form on screen, in render order.</summary>
    public IReadOnlyList<ComposerFieldDescriptor> Fields => _fields;

    /// <summary>
    /// Wires the host-side sources: the session's config, the run context, and the attach path.
    /// </summary>
    /// <remarks>
    /// <para>Called by the shell after render, exactly as the canvas graph source is wired.
    /// Everything supplied here is host-owned; nothing in it can be influenced by the page.</para>
    ///
    /// <para><b>It pushes the first <c>host.init</c> — but only if the page has already mounted.</b>
    /// The two orders are both real: the shell configures a document it has just opened while the
    /// browser is still starting, and it can equally configure one whose page mounted first. Each
    /// half pushes only when the other has happened, so a mount yields <b>exactly one</b> init
    /// whichever way round they land.</para>
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

        // THE SESSION'S VALUES, ONE HOME (Rulings 56, 72): the ceilings the compiled block reads
        // and the class every prompt starts with — never a per-prompt field.
        _draft.UseSessionSettings(config);
        _taskClass = string.IsNullOrWhiteSpace(context.TaskClass) ? config.DefaultTaskClass : context.TaskClass;

        // THE ENVELOPE'S IDENTITY (ADR-0034 rule 1): every opened row names this session; the
        // compile mode is a provenance fact on it (E5) — mechanical-only until the ladder admits more.
        _gate.BindSession(config.SessionId, config.CompileMode, context.EngineId, _taskClass);

        _fields.Clear();
        _fields.AddRange(fields);

        _router.ReplaceFields([.. _fields.Select(f => f.Id)]);

        _templatePicker.ItemsSource = catalog is null ? null : ComposerTemplatePicker.Rows(catalog);
        _templatePicker.Visibility = catalog is null ? Visibility.Collapsed : Visibility.Visible;

        _configured = true;
        WorkbenchDiagnostics.WebSurfaceHandshake(SurfaceId, "configured", _navigations, _inputs, _router.Dropped, $"fields {_fields.Count}");

        ApplyAttachAffordance();
        RenderCompiledView();
        PushInitWhenBothHalvesHaveHappened();
    }

    /// <summary>
    /// Reports a host-side refusal on the surface, naming the field the operator must fix.
    /// </summary>
    /// <remarks>
    /// <b>Where the composer says why it has no run binding.</b> Nothing is wired — there is no
    /// context to wire — and the alternative is an empty surface, which is indistinguishable from a
    /// broken one. The field name is carried rather than folded into prose for the same reason
    /// <see cref="ComposerFieldError"/> carries one: the operator's next action is to edit one line.
    /// </remarks>
    /// <param name="field">The field, in the wire name the configuration file uses.</param>
    /// <param name="message">What is wrong, naming the file and the values found.</param>
    public void ShowFieldRefusal(string field, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        SetStatus($"{field}: {message}", Urgency.Assertive);
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
            SetStatus($"the template '{templateId}' is not one this catalog can offer", Urgency.Assertive);
            return;
        }

        _template = entry.Template;
        _draft.UseTemplate(templateId);

        _fields.Clear();
        _fields.AddRange(ComposerFields.ForTemplate(entry.Template!));
        _router.ReplaceFields([.. _fields.Select(f => f.Id)]);

        PushInit();
        RenderCompiledView();
    }

    /// <summary>
    /// The send gesture, host-owned. The button calls it; so does the accelerator handler.
    /// </summary>
    /// <returns>The request that was built, or null when the send was refused.</returns>
    public GovernedRunRequest? Send()
    {
        // EXACTLY ONE QUEUED TURN PER SESSION (Ruling 95): while one is queued a further Send is
        // refused naming it — cancel it or wait — before anything else is read.
        if (_queued is { } queued)
        {
            SetStatus($"{queued.DisplayOrdinal} is queued; cancel it or wait.", Urgency.Assertive);
            _statusKind = StatusKind.Refusal;
            return null;
        }

        if (_context is null)
        {
            SetStatus("the composer is not wired to a session yet", Urgency.Assertive);
            return null;
        }

        RenderCompiledView();

        // UNDER AN AGENTIC RUNG A PROMPT COSTS TWO GESTURES (Ruling 67; §A10.1): the first prepares —
        // opens the envelope, calls the bound model, enters Prepare — the second confirms. A gesture
        // while preparing is ignored with its reason (Ruling 77: no Send-now).
        if (_gate.IsAgenticRung && _gate.State != PrepareState.Prepared)
        {
            if (_gate.State == PrepareState.Preparing)
            {
                SetStatus(ComposerSendGate.PreparingReason, Urgency.Status);
                return null;
            }

            Preparing = PrepareTurnAsync();
            return null;
        }

        // A SEND WHILE A TURN RUNS OR WAITS IS A CHOICE, NOT A REFUSAL (Ruling 95, reversing
        // Ruling 77(b) as its own condition 2 foresaw): the status line offers Wait — the same gate,
        // compiled now, queued after the turn in flight — or a parallel session. Spoken, never
        // silent (SC6); no dialog, no modifier key. Offered here, behind the prepare gate, so under
        // an agentic rung the choice comes when a send would happen — never on the preparing gesture.
        if (_inFlight is { } inFlight)
        {
            OfferWaitOrParallel(inFlight);
            return null;
        }

        return SendThroughTheGate();
    }

    /// <summary>
    /// <i>Wait — send after b1</i> (Ruling 95): the same gate, now — the envelope compiled and
    /// submitted with its sha recorded, the request handed to the document, which queues it behind
    /// the turn in flight. Public so the status line's action and a test reach one verb.
    /// </summary>
    /// <returns>The request that was built and queued, or null when the gate refused.</returns>
    public GovernedRunRequest? SendAfterTheTurnInFlight()
    {
        if (_queued is { } queued)
        {
            SetStatus($"{queued.DisplayOrdinal} is queued; cancel it or wait.", Urgency.Assertive);
            _statusKind = StatusKind.Refusal;
            return null;
        }

        if (_context is null)
        {
            SetStatus("the composer is not wired to a session yet", Urgency.Assertive);
            return null;
        }

        RenderCompiledView();
        return SendThroughTheGate();
    }

    /// <summary>
    /// <i>Start a parallel session</i>'s seam (Ruling 95): the shell takes the draft's words and
    /// attachments, opens a derived sibling session and sends them as its first turn, and returns
    /// null — or the reason it did not, in which case the words stay here. Null when this composer
    /// has no shell (a document built alone), which is its own refusal.
    /// </summary>
    public Func<ParallelDraft, string?>? ParallelStarter { get; set; }

    /// <summary>
    /// Why <i>Start a parallel session</i> is refused before the shell is asked, or null when it is
    /// available. Set by the shell from what it measured or can do (Ruling 95 condition 1) — a
    /// composer never decides this from its own state.
    /// </summary>
    public string? ParallelRefusal { get; set; }

    private GovernedRunRequest? SendThroughTheGate()
    {
        var request = _gate.Send(_context!, _draft, _template, out var refusal);
        if (request is null)
        {
            SetStatus(
                refusal is { Errors.Count: > 0 }
                    ? string.Join("  ", refusal.Errors.Select(e => e.Message))
                    : refusal?.Message ?? "the send was refused",
                Urgency.Assertive);
            MarkInvalid(refusal);
            return null;
        }

        // THE SENT LEASE, AS SENT (Ruling 73): the request's own absent lease is the read-only state.
        _leaseLine = ComposerCompiler.LeaseLine(request.Lease?.Exclusive);

        // A QUEUED SEND KEEPS ITS OWN SENTENCE (Ruling 95): the document queued it during the gate's
        // Sent event and the thread's snapshot already put "b2 queued — sends after b1" here.
        if (_statusKind == StatusKind.Queued)
        {
            _hasSentBefore = true;
            return request;
        }

        // Ruling 75 condition (2): a goal-block form that compiled as a Message is said so, never
        // demoted silently. SC9's spoken announcement is CV-1's; the status line is the floor here.
        SetStatus(
            _draft.Shape == ComposerShape.GoalBlock && request.Goal is null
                ? "sent as a message — read-only"
                : "sent",
            Urgency.Status);
        _hasSentBefore = true;
        return request;
    }

    /// <summary>The compile line's text as rendered, or null when the line is absent.</summary>
    public string? CompileLine => _compileRow.Visibility == Visibility.Visible ? _compileLine.Text : null;

    /// <summary>The preparing gesture in flight, or the last one — what a test drains and what the document may await.</summary>
    public Task? Preparing { get; private set; }

    /// <summary>The operator keeps a derived line through its own control (the test's route to the same click).</summary>
    public void KeepStructureLine(string field)
    {
        if (_structureLines.TryGetValue(field, out var line))
        {
            line.Keep();
        }
    }

    /// <summary>The Prepare state, as the gate holds it.</summary>
    public PrepareState PrepareState => _gate.State;

    /// <summary>Whether the <i>Prepare again</i> control is on the screen.</summary>
    public bool PrepareAgainVisible => _prepareAgain.Visibility == Visibility.Visible;

    /// <summary>Whether the <i>Cancel</i> control is on the screen.</summary>
    public bool CancelVisible => _cancelPrepare.Visibility == Visibility.Visible;

    /// <summary>
    /// The preparing gesture: the envelope opened, the model called for the open lines, Prepare
    /// entered with the compile line and its next-action control — every state with a reason string
    /// in voice (§A11; US-D5).
    /// </summary>
    public async Task PrepareTurnAsync()
    {
        if (_context is null)
        {
            SetStatus("the composer is not wired to a session yet", Urgency.Assertive);
            return;
        }

        ShowCompileLine("Preparing…", preparing: true);
        SetStatus("Preparing… — the model is reading your draft", Urgency.Status);

        var result = await _gate.PrepareAsync(_context, _draft, _template);

        if (!result.Prepared)
        {
            ShowCompileLine(result.Message, preparing: false, degraded: true);
            SetStatus(result.Message, Urgency.Assertive);
            MarkInvalid(result.Errors is { Count: > 0 } errors ? new ComposerSendRefusal(errors, result.Message) : null);
            return;
        }

        // THE MODEL'S LINES, MARKED derived — and under agentic-advisory a keep is the act that lets
        // one project (§A11): the line's own control, never a page message.
        foreach (var (field, line) in _structureLines)
        {
            if (_gate.DerivedLines.TryGetValue(field, out var proposed))
            {
                line.ShowDerived(proposed, _gate.LastCallOutcome == CallOutcomes.Suspect);
            }
        }

        var degraded = _gate.LastCallOutcome is { } outcome && !CallOutcomes.Agentic.Contains(outcome, StringComparer.Ordinal);
        ShowCompileLine(_gate.CompileLineText ?? string.Empty, preparing: false, degraded: degraded);
        RenderCompiledView();
        SetStatus(
            degraded ? "prepared — " + (_gate.CompileLineText ?? string.Empty) + " — press again to send" : "prepared — press again to send",
            Urgency.Status);
    }

    /// <summary>Shows the compile line with the control its state needs: Cancel while preparing, Prepare again when degraded or stale.</summary>
    private void ShowCompileLine(string text, bool preparing, bool degraded = false)
    {
        _compileRow.Visibility = Visibility.Visible;
        _compileLine.Text = text;
        _cancelPrepare.Visibility = preparing ? Visibility.Visible : Visibility.Collapsed;
        _prepareAgain.Visibility = !preparing && (degraded || _gate.State == PrepareState.Stale) ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetItemStatus(_compileLine, _gate.State.ToString().ToLowerInvariant());
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
    /// <remarks>
    /// The page mounted. It pushes the first init <b>if the shell has already configured this
    /// surface</b>; if it has not, <see cref="Configure"/> pushes instead. See its remarks.
    /// </remarks>
    public void MarkReady()
    {
        _pageReady = true;
        WorkbenchDiagnostics.WebSurfaceHandshake(SurfaceId, "page-ready", _navigations, _inputs, _router.Dropped);
        PushInitWhenBothHalvesHaveHappened();
        PageReady?.Invoke();
    }

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

        _inputs++;
        if (!_inputSinceInit)
        {
            // Once per pushed init: the first keystroke that reached the draft on the page on screen.
            _inputSinceInit = true;
            WorkbenchDiagnostics.WebSurfaceHandshake(SurfaceId, "input-received", _navigations, _inputs, _router.Dropped);
        }

        RenderCompiledView();
    }

    /// <inheritdoc/>
    public void MoveFocus(bool backward)
    {
        if (backward)
        {
            // Shift+Tab from the editor's first stop: the document routes it to the thread's last
            // stop (DS-1 seam 3); with nothing subscribed, WPF's own previous stop.
            if (FocusLeftBackward is { } route)
            {
                route();
                return;
            }

            MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Previous));
            return;
        }

        MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
    }

    /// <summary>Raised when the page posts a backward <c>focus.leave</c>; the document routes it into the thread.</summary>
    public event Action? FocusLeftBackward;

    /// <summary>The composer's first WPF stop — the Goal line — for a document whose page is not up yet (F6 still has somewhere to land).</summary>
    public bool FocusFirstLine()
    {
        if (_structure.IsExpanded)
        {
            return _structureLines.TryGetValue(ComposerDraft.PerPromptGoalFields[0], out var line) && line.Editor.Focus();
        }

        // Collapsed: the structure's header is the composer's first stop — never a line the
        // operator cannot see.
        _structure.ApplyTemplate();
        return _structure.Template?.FindName("HeaderSite", _structure) is UIElement header && header.Focus();
    }

    /// <summary>Whether the structure lines are open — collapsed at rest (DESIGN.md:1088).</summary>
    public bool StructureOpen
    {
        get => _structure.IsExpanded;
        set => _structure.IsExpanded = value;
    }

    /// <summary>Whether Win32 focus is inside the editor's window — the page holds it and WPF's focused element reads null.</summary>
    public bool EditorHasFocus
    {
        get
        {
            try
            {
                return _pageReady && ((EditorFocusTarget)FocusTarget).HasFocus(_view.Handle);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// The in-flight turn, or null (Ruling 77): while one runs or waits, a Send gesture is refused
    /// with its ordinal named. Set by the document from the thread's snapshot; never inferred here.
    /// </summary>
    public void SetInFlight(TurnView? turn)
    {
        _inFlight = turn;
        if (turn is null && _statusKind == StatusKind.Offer)
        {
            SetStatus(string.Empty, Urgency.Status);
        }
    }

    /// <summary>
    /// The queued turn, or null (Ruling 95): set by the document from the thread's snapshot, with the
    /// one sentence the snapshot derives for it — <i>b2 queued — sends after b1</i> · <i>b1 stopped
    /// by you; b2 is waiting — Send it or cancel it</i>. The sentence is the status line while a turn
    /// is queued; spoken when it changes (assertively when it asks the operator for an act), and
    /// cleared when the queue empties.
    /// </summary>
    public void SetQueued(TurnView? queued, string? sentence, bool awaitsYou)
    {
        _queued = queued;

        if (queued is null)
        {
            if (_statusKind is StatusKind.Queued or StatusKind.Refusal)
            {
                SetStatus(string.Empty, Urgency.Status);
            }

            _queuedSentence = null;
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(sentence);
        if (string.Equals(_queuedSentence, sentence, StringComparison.Ordinal))
        {
            return;
        }

        _queuedSentence = sentence;
        SetStatus(sentence, awaitsYou ? Urgency.Assertive : Urgency.Status);
        _statusKind = StatusKind.Queued;
    }

    /// <summary>The offer's sentence (Ruling 95; DESIGN.md copy): <i>b1 is running. Wait — send after b1, or start a parallel session.</i></summary>
    public static string WaitOrParallelOffer(TurnView inFlight)
    {
        ArgumentNullException.ThrowIfNull(inFlight);
        return $"{inFlight.DisplayOrdinal} {InFlightState(inFlight)}. {WaitActionName(inFlight)}, or start a parallel session.";
    }

    /// <summary>The Wait action's name: <i>Wait — send after b1</i>.</summary>
    public static string WaitActionName(TurnView inFlight)
    {
        ArgumentNullException.ThrowIfNull(inFlight);
        return $"Wait — send after {inFlight.DisplayOrdinal}";
    }

    /// <summary>The Parallel action's name.</summary>
    public const string ParallelActionName = "Start a parallel session";

    private static string InFlightState(TurnView inFlight) =>
        inFlight.State == TurnState.Waiting ? "is waiting for you" : "is running";

    private enum StatusKind
    {
        Other,
        Offer,
        Queued,
        Refusal,
    }

    /// <summary>
    /// The status line: the sentence on screen and spoken (SC6 — never silent). A refusal, an
    /// error and a request are assertive (SC9); <i>sent</i> is a status. An empty text clears.
    /// </summary>
    private void SetStatus(string text, Urgency urgency)
    {
        _statusText = text;
        _statusKind = StatusKind.Other;
        if (text.Length == 0)
        {
            _status.Inlines.Clear();
            return;
        }

        // Spoken first: the announcer that owns the status line writes its Text, and the inlines
        // below replace it (the two agree — the announcer's text is this text).
        _announcer.Announce(new Announcement(text, urgency, AnnouncementKind.Other));
        _status.Inlines.Clear();
        _status.Inlines.Add(new System.Windows.Documents.Run(text));
    }

    /// <summary>
    /// Ruling 95's two-action line, spoken as one sentence and rendered as three links: the ordinal
    /// (SC8: to the turn's container, never an action — Ruling 77 condition 1's link kept), <i>Wait —
    /// send after b1</i>, and <i>Start a parallel session</i>. No dialog, no modifier key.
    /// </summary>
    private void OfferWaitOrParallel(TurnView inFlight)
    {
        SetStatus(WaitOrParallelOffer(inFlight), Urgency.Assertive);
        _statusKind = StatusKind.Offer;

        var ordinal = inFlight.Ordinal;
        var turn = StatusLink(inFlight.DisplayOrdinal, $"{inFlight.DisplayOrdinal}, {(inFlight.State == TurnState.Waiting ? "waiting for you" : "running")}");
        turn.Click += (_, _) => TurnRequested?.Invoke(ordinal);

        var wait = StatusLink(WaitActionName(inFlight), WaitActionName(inFlight));
        AutomationProperties.SetHelpText(wait, "compiles now and sends when the turn in flight ends");
        wait.Click += (_, _) => SendAfterTheTurnInFlight();

        var parallel = StatusLink(ParallelActionName, ParallelActionName);
        AutomationProperties.SetHelpText(parallel, ParallelRefusal ?? "a derived session beside this one; this prompt is its first turn");
        parallel.Click += (_, _) => StartParallel();

        _status.Inlines.Clear();
        _status.Inlines.Add(turn);
        _status.Inlines.Add(new System.Windows.Documents.Run($" {InFlightState(inFlight)}. "));
        _status.Inlines.Add(wait);
        _status.Inlines.Add(new System.Windows.Documents.Run(", or "));
        _status.Inlines.Add(parallel);
        _status.Inlines.Add(new System.Windows.Documents.Run("."));
    }

    private static System.Windows.Documents.Hyperlink StatusLink(string text, string name)
    {
        var link = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run(text));
        link.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "AccentBrush");
        AutomationProperties.SetName(link, name);
        return link;
    }

    /// <summary>
    /// <i>Start a parallel session</i> (Ruling 95): refused with the shell's reason when it has one
    /// (condition 1's measurement, or no shell to open a session); else the draft's words and
    /// attachments go to the shell and the draft is consumed once the handler took them.
    /// </summary>
    private void StartParallel()
    {
        if (ParallelRefusal is { } refusal)
        {
            SetStatus(refusal, Urgency.Assertive);
            return;
        }

        if (ParallelStarter is null)
        {
            SetStatus("a parallel session needs the shell; this composer has none", Urgency.Assertive);
            return;
        }

        var request = new ParallelDraft(_draft.SourceText, [.. _draft.Attachments]);
        if (ParallelStarter(request) is { } notStarted)
        {
            // The words stay here: nothing was sent on their behalf.
            SetStatus(notStarted, Urgency.Assertive);
            return;
        }

        // THE PARENT'S DRAFT IS CONSUMED (Ruling 95): its words now belong to the sibling's first turn.
        BeginNextTurn();
        SetStatus("sent as the first turn of a parallel session", Urgency.Status);
    }

    /// <summary>
    /// The turn was accepted: the composer starts the next one — the message and the structure
    /// lines empty, the gate on a new block, the page told (Feedback:+Confirmed — the turn now lives
    /// in the thread).
    /// </summary>
    public void BeginNextTurn()
    {
        _gate.NextBlock();
        _compileRow.Visibility = Visibility.Collapsed;
        _draft.SetFreeFormText(string.Empty);
        foreach (var field in ComposerDraft.PerPromptGoalFields)
        {
            _draft.SetGoalValue(field, string.Empty);
        }

        foreach (var line in _structureLines.Values)
        {
            line.Reset();
        }

        RenderCompiledView();
        if (_pageReady && _configured)
        {
            PushInit();
        }
    }

    /// <summary>A past turn's words become the next draft (<i>Use as the next draft</i> · <i>Send again as a new turn</i>) — a host→page push, never a second Configure.</summary>
    public void UseAsNextDraft(string sourceText)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        _draft.SetFreeFormText(sourceText);
        RenderCompiledView();
        if (_pageReady && _configured)
        {
            PushInit();
        }
    }

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
            SetStatus("the composer is not wired to a session yet", Urgency.Assertive);
            return new AttachOutcome([], [], 0);
        }

        var outcome = _attachments.Offer(_draft, filePaths, _attachEnabled);
        _blockedByAttachSetting += outcome.BlockedByAttachSetting;

        if (outcome.Refusals.Count > 0)
        {
            SetStatus(string.Join("  ", outcome.Refusals), Urgency.Assertive);
        }

        RenderCompiledView();
        return outcome;
    }

    /// <summary>The committed-channel record for this send: counts, and one boolean.</summary>
    public System.Text.Json.Nodes.JsonObject CommittedRecord() =>
        ComposerSendRecord.Committed(
            _attachEnabled,
            _gate.RenderedView ?? _gate.RenderView(_draft, _template),
            _blockedByAttachSetting);

    private void RenderCompiledView()
    {
        // STALE (§A11): the draft changed after Prepare — the live projections show, the compile
        // line carries the mark, the next gesture re-prepares; a draft edited back is fresh again.
        var before = _gate.State;
        _gate.RefreshState(_draft);
        if (_gate.State != before && _gate.State is PrepareState.Stale or PrepareState.Prepared)
        {
            ShowCompileLine(_gate.State == PrepareState.Stale ? "stale — your draft changed since it was prepared; press again to prepare it" : _gate.CompileLineText ?? string.Empty, preparing: false, degraded: _gate.State == PrepareState.Stale);
        }

        var compiled = _gate.RenderView(_draft, _template);
        _compiled.Text = compiled.Text;

        // THE DRAFT'S OWN SOURCE TEXT, NOT THE COMPILED PROMPT (Ruling 66) — the same symbol
        // `ComposerSendGate.Send` derives the sent lease from, and THE SAME TWO INPUTS it decides the
        // shape on (Ruling 73), so the displayed lease and the sent lease can never disagree: a
        // Message, or a goal block with no scope, reads read-only here and sends no lease there.
        var patterns = LeaseDerivation.Patterns(_draft.SourceText);
        _leaseLine = ComposerCompiler.LeaseLine(ComposerCompiler.IsReadOnly(_draft.TurnShape, patterns) ? null : patterns);

        RenderDecorationLine();
        foreach (var (field, line) in _structureLines)
        {
            // The draft is the source of truth: a value written to it (Use as the next draft, a test)
            // reaches the line; a value typed into the line reached the draft already.
            line.Sync(_draft.GoalValues.TryGetValue(field, out var value) ? value : string.Empty);
            line.RenderMark();
        }
    }

    /// <summary>
    /// The decoration line (SC2): <i>This turn · class free-form session default · tier T0 no goal
    /// block · lease … · message [· template …]</i>, every value a segment with its provenance
    /// beside it; the settings line beneath derives the effective cap (Ruling 64).
    /// </summary>
    private void RenderDecorationLine()
    {
        _decorationLine.Children.Clear();
        _decorationLine.Children.Add(SegmentLabel("This turn", strong: true));

        var decorations = Decorations;
        foreach (var row in decorations)
        {
            var segment = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 14, 0), MinHeight = 24 };
            if (row.Name != "shape")
            {
                segment.Children.Add(SegmentLabel(row.Name));
            }

            // THE EDITABLE DERIVED LINES (Prepare, §A11): the tier and the class carry a control
            // beside the value — each override is an operator row at Send; the value shown stays the
            // projection's, so what the control says and what the wire carries are one function's output.
            if (row.Name == "tier")
            {
                // The VALUE stays text (the rule's answer, or the override — never a tilde, U13); the
                // control beside it reads `rule` until the operator chooses.
                segment.Children.Add(DecorationValue(row));
                SyncControl(_tierControl, _draft.TierOverride ?? RuleChoice);
                segment.Children.Add(_tierControl);
            }
            else if (row.Name == "class")
            {
                // The control's selected item IS the class shown: the session's default, or this prompt's choice.
                SyncControl(_classControl, _draft.TaskClassChoice ?? _taskClass);
                segment.Children.Add(_classControl);
            }
            else
            {
                segment.Children.Add(DecorationValue(row));
            }

            // The provenance, inline: the current turn is confirmed at Send (SC2), so the source
            // and the reason are beside the value rather than a disclosure away.
            var provenance = new TextBlock
            {
                Text = row.Name == "class" ? (row.Source == DecorationSources.Operator ? "chosen for this prompt" : "session default") : row.Reason,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
            };
            provenance.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
            segment.Children.Add(provenance);

            _decorationLine.Children.Add(segment);
        }

        // The settings line derives from the decoration line's tier — one projection, two readers
        // (DM7) — and carries the one reason compile history is not being recorded, when there is one
        // (ADR-0034 rules 2–3: a locked or broken file degrades Prepare visibly, never silently).
        var tier = decorations.First(d => d.Name == "tier").Value;
        var settings = ComposerCompiler.SettingsLine(tier, _draft.Ceilings.FanOutCeiling, _draft.Ceilings.BudgetCap);
        _settingsLine.Text = _gate.HistoryState is { } history ? settings + " · compile history: " + history : settings;
    }

    /// <summary>The tier control's first choice: the rule's own value stands.</summary>
    public const string RuleChoice = "rule";

    private ComboBox BuildTierControl()
    {
        var control = new ComboBox { FontSize = 12, MinWidth = 64, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
        control.ItemsSource = new[] { RuleChoice, "T0", "T1", "T2" };
        AutomationProperties.SetName(control, "Tier override");
        AutomationProperties.SetHelpText(control, "The rule's tier stands unless you choose one here; a choice is recorded on this turn as yours.");
        control.SelectionChanged += (_, _) =>
        {
            if (_renderingControls || control.SelectedItem is not string choice)
            {
                return;
            }

            _draft.OverrideTier(choice == RuleChoice ? null : choice);
            RenderCompiledView();
        };
        return control;
    }

    private ComboBox BuildClassControl()
    {
        var control = new ComboBox { FontSize = 12, MinWidth = 96, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center, IsEditable = false };
        AutomationProperties.SetName(control, "Task class");
        AutomationProperties.SetHelpText(control, "The class this prompt is scored against. The session's default unless you choose one for this prompt; that never changes the default.");
        control.SelectionChanged += (_, _) =>
        {
            if (_renderingControls || control.SelectedItem is not string choice)
            {
                return;
            }

            _draft.ChooseTaskClass(string.Equals(choice, _taskClass, StringComparison.Ordinal) ? null : choice);
            RenderCompiledView();
        };
        return control;
    }

    /// <summary>Shows the draft's current choice on a control without re-entering its handler; the class control offers the session's default first, then the vocabulary.</summary>
    private void SyncControl(ComboBox control, string selected)
    {
        _renderingControls = true;
        try
        {
            if (ReferenceEquals(control, _classControl))
            {
                var offered = new List<string> { _taskClass };
                offered.AddRange(AiDe.Core.Presentation.Sessions.TaskClassVocabulary.Offered.Select(o => o.Id).Where(id => !string.Equals(id, _taskClass, StringComparison.Ordinal)));
                if (!offered.Contains(selected, StringComparer.Ordinal))
                {
                    offered.Add(selected);
                }

                if (control.ItemsSource is not IReadOnlyList<string> current || !current.SequenceEqual(offered, StringComparer.Ordinal))
                {
                    control.ItemsSource = offered;
                }
            }

            if (!Equals(control.SelectedItem, selected))
            {
                control.SelectedItem = selected;
            }

            if (control.Parent is Panel parent)
            {
                parent.Children.Remove(control);
            }
        }
        finally
        {
            _renderingControls = false;
        }
    }

    /// <summary>
    /// A decoration's value as rendered: the tilde and the inferred ink mark a MODEL-derived value
    /// (DESIGN.md SC4; §A11's marks) — a mechanical rule's value is text, a false uncertainty mark
    /// is a lie (U13). Internal so the rule is tested over a row of each source, not read from code.
    /// </summary>
    internal static TextBlock DecorationValue(DecorationRow row)
    {
        var inferred = string.Equals(row.Source, ModelSource, StringComparison.Ordinal);
        var value = new TextBlock
        {
            Text = inferred ? "~ " + row.Value : row.Value,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0),
        };
        value.SetResourceReference(TextBlock.ForegroundProperty, inferred ? "InferredBrush" : "TextBrush");
        if (row.Name == "lease")
        {
            value.FontFamily = AiDe.App.Workbench.Sessions.ThreadFeed.Mono;
        }

        AutomationProperties.SetName(value, row.Name + " " + row.Value);
        AutomationProperties.SetHelpText(value, row.Reason);
        return value;
    }

    private static TextBlock SegmentLabel(string text, bool strong = false)
    {
        var label = new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = strong ? FontWeights.SemiBold : FontWeights.Normal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, strong ? 12 : 4, 0),
        };
        label.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return label;
    }

    /// <summary>
    /// The structure: Goal · Done when · Not in scope as inline lines beneath the editor — empty
    /// and editable under <c>mechanical-only</c> (Addendum D §A11's <i>— fill in</i>), each with
    /// its mark; a refused Send marks the named line <i>invalid</i> with the one sentence (Ruling 75).
    /// </summary>
    /// <remarks>
    /// <b>D-5's first slice.</b> The lines exist so Prepare's regions are real controls; the deriver
    /// behind them is <see cref="StructureDeriver"/>, a fake returning three empty strings until the
    /// compile step (CV-2) proposes values — a <c>derived</c> mark is that slice's, not this one's.
    /// </remarks>
    private Expander BuildStructure()
    {
        var body = new StackPanel();
        foreach (var field in ComposerDraft.PerPromptGoalFields)
        {
            var line = new StructureLine(field, StructureDeriver.Fake(field), text =>
            {
                _draft.SetGoalValue(field, text);
                // On a prepared envelope the edit is an `operator` row — the derived row stays in the fold (§A11).
                _gate.EditLine(field, text);
                RenderCompiledView();
            }, () =>
            {
                _gate.KeepLine(field);
                RenderCompiledView();
            });
            _structureLines[field] = line;
            body.Children.Add(line.Root);
        }

        // COLLAPSED AT REST (DESIGN.md:1088: four rows beneath the editor, the structure collapsed;
        // +3 expanded). It opens when the operator opens it, when a refusal names one of its lines
        // (the error must be on the screen), and when a draft arrives with structure in it.
        var expander = new Expander
        {
            Header = "Goal · Done when · Not in scope",
            IsExpanded = false,
            Content = body,
            Style = AiDe.App.Workbench.Sessions.ThreadFeed.DisclosureStyle(),
            Focusable = false,
            IsTabStop = false,
        };
        AutomationProperties.SetName(expander, "Goal, Done when, Not in scope");
        AutomationProperties.SetHelpText(expander, "The structure of this turn. Written here, it makes the turn a goal block; a blank Goal or Done when makes a message.");
        return expander;
    }

    private void MarkInvalid(ComposerSendRefusal? refusal)
    {
        if (refusal is null)
        {
            return;
        }

        foreach (var error in refusal.Errors)
        {
            if (_structureLines.TryGetValue(error.Field, out var line))
            {
                line.MarkInvalid(error.Message);
                _structure.IsExpanded = true;   // the named line is on the screen (Ruling 75), never behind a fold
            }
        }
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
        _host.Dispose();
        WorkbenchDiagnostics.WebSurfaceHandshake(SurfaceId, "disposed", _navigations, _inputs, _router.Dropped);
    }

    private static IReadOnlyList<string> PickFiles()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Multiselect = true, CheckFileExists = true };
        return dialog.ShowDialog() == true ? dialog.FileNames : [];
    }

    /// <summary>
    /// Pushes <c>host.init</c> once the page has mounted <i>and</i> the shell has configured this
    /// surface — the two halves of the handshake, in whichever order they arrive.
    /// </summary>
    private void PushInitWhenBothHalvesHaveHappened()
    {
        if (_pageReady && _configured)
        {
            PushInit();
        }
    }

    private void PushInit()
    {
        _view.CoreWebView2?.PostWebMessageAsJson(InitPayloadJson());
        _inputSinceInit = false;
        WorkbenchDiagnostics.WebSurfaceHandshake(SurfaceId, "init-pushed", _navigations, _inputs, _router.Dropped, $"fields {_fields.Count}");
    }

    /// <summary>The <c>host.init</c> envelope as the page receives it — the one builder the push serialises (a test reads it without a browser).</summary>
    internal string InitPayloadJson()
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

                // THE DRAFT'S OWN VALUE, not a blank. The host is the source of the text and the page
                // mirrors it, so an init carrying `""` for a field the draft has content for does not
                // "start fresh" — it overwrites what the operator wrote with an empty form.
                value = CurrentValue(f),
            }),
            fileCandidates = Array.Empty<string>(),
            graphCandidates = Array.Empty<string>(),

            // The editor's placeholder and description (DESIGN.md copy; SC6, SC8): the page renders
            // them as aria-placeholder / aria-describedby on the one message editor.
            placeholder = _inFlight is not null || _hasSentBefore
                ? "Write the next message. Mention the files it may write as @path."
                : "What should this session do? Mention the files it may write as @path.",
            editorHelp = "Ctrl+Enter sends. Type @ to mention a file or folder. F6 moves to the thread; Ctrl+Home reaches the header.",

            // The shell's tokens as CSS custom properties, so the page draws with the one palette
            // (INV-0008, Fix C). Additive: a page that ignores it renders its fallbacks.
            theme = ComposerPageTheme.Current(),

            // THE ONE FLOOR (Ruling 80; DM-A): the page reads --editor-floor from this push and
            // keeps the same constant's declared value as its stylesheet fallback — it carries no
            // floor of its own (composer.html once said 110 while the host said 130, and the page
            // scrolled at the floor before a character was typed).
            editorFloor = EditorFloor,
        };

        return JsonSerializer.Serialize(payload);
    }

    /// <summary>What the draft currently holds for one field — the inverse of <see cref="SetFieldText"/>.</summary>
    private string CurrentValue(ComposerFieldDescriptor field) => field.Target switch
    {
        ComposerFieldTarget.FreeForm => _draft.FreeFormText,
        ComposerFieldTarget.GoalBlock =>
            _draft.GoalValues.TryGetValue(field.Name, out var goal) ? goal : string.Empty,
        ComposerFieldTarget.Template =>
            _draft.TemplateValues.TryGetValue(field.Name, out var values)
                ? string.Join("\n", values)
                : string.Empty,
        _ => string.Empty,
    };

    private static string WidgetName(ComposerFieldWidget widget) => widget switch
    {
        ComposerFieldWidget.LongText => "long-text",
        ComposerFieldWidget.Mentions => "mentions",
        ComposerFieldWidget.List => "list",
        ComposerFieldWidget.Enum => "enum",
        ComposerFieldWidget.Budget => "budget",
        _ => "text",
    };

    /// <summary>The host's <c>init-failed</c> callback: names the cause and shows Retry (the mockup's <c>editorerror</c> state).</summary>
    private void OnHostInitFailed(string failure)
    {
        SetStatus("the composer editor could not start: " + failure, Urgency.Assertive);
        _retry.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Runs the failed initialisation again (<see cref="WebSurfaceHost.Retry"/>) — the mockup's
    /// <c>editorerror</c> Retry. A second failure re-shows this same button and status; a success
    /// hides the button, and <see cref="PageReady"/> moves focus into the editor exactly as a first
    /// successful load does (unchanged path, no second wiring to keep in step).
    /// </summary>
    private async Task RetryEditorAsync()
    {
        _retry.Visibility = Visibility.Collapsed;
        SetStatus(string.Empty, Urgency.Status);
        await _host.Retry();
    }

    /// <summary>
    /// Configures the started browser and navigates it. Runs once per surface — the host guards
    /// the attach, so a re-parent by the docking host neither re-subscribes nor re-navigates.
    /// </summary>
    private async Task InitialiseAsync()
    {
        var core = _view.CoreWebView2!;

        ComposerPageContract.ApplySettingsFloor(core.Settings);
        WebAssetHost.Map(core);

        // THE HOST-MINTED INSTANCE, HANDED TO THE DOCUMENT BEFORE ANY SCRIPT RUNS. The page must
        // carry it in every envelope, including the first — `editor.ready` — so it cannot be
        // learned from `host.init`, which is what ready unblocks. Injected rather than posted:
        // a message needs an instance to be routed, which is the loop.
        //
        // IT GRANTS THE PAGE NOTHING. It is a string the host generated, that the host already
        // re-states on `host.init`, and that the router compares ordinally. It is not a host
        // object — those stay off — and there is no second value the page could have used.
        await core.AddScriptToExecuteOnDocumentCreatedAsync(
            "window.__aideComposerInstance = " + JsonSerializer.Serialize(_instance) + ";");

        core.NavigationStarting += OnNavigationStarting;
        core.FrameNavigationStarting += (_, e) => e.Cancel = ComposerPageContract.MustCancelNavigation(e.Uri);
        core.NewWindowRequested += (_, e) => e.Handled = true;
        core.LaunchingExternalUriScheme += (_, e) => e.Cancel = true;
        core.WebMessageReceived += OnWebMessage;

        core.Navigate(ComposerPageContract.Url);
    }

    /// <summary>
    /// <b>The writer is sized first (DC-137).</b> A DockPanel measures its docked children before the
    /// fill child, each with what remains of the constraint after the ones before it, so an uncapped
    /// compiled prompt would take its whole content height and the editor host the remainder. The
    /// compiled prompt's ceiling is therefore set here, before the content is measured: the smaller
    /// of its 200 px, what the chrome leaves once the editor has its floor, and half of what the
    /// chrome leaves (the writer is never smaller than the reader) — never under its own 48 px floor
    /// when it is open. The document sets <see cref="BeltHeight"/>, never <c>MaxHeight</c>; the
    /// content is measured within the belt — or within <see cref="MinimumHeight"/> when the belt is
    /// smaller, because a DesiredSize is clipped to what it was measured against and a belt passed
    /// straight through would clamp exactly as a <c>MaxHeight</c> did (L1's 600 px rows). Under an
    /// infinite constraint with no belt the composer declares its natural height — the floor is on
    /// the host (spike Q14). <b>The editor's height is derived, never its own desire (Ruling 80):</b>
    /// a WebView2 desires nothing of its own, so the host is given the belt's remainder after the
    /// chrome and the reader — capped at <see cref="EditorRestHeight"/>, floored by the host's
    /// <c>MinHeight</c> — and the composer desires exactly the belt while the belt can hold it.
    /// </summary>
    protected override Size MeasureOverride(Size constraint)
    {
        // The height the composer composes within: the document's belt, or the constraint when it
        // is tighter. Under it the compiled prompt yields first (DC-137) and the editor keeps its
        // floor; the composer's own minimum is what it desires when the belt is smaller than it.
        var height = Math.Min(constraint.Height, _beltHeight);

        var unbounded = new Size(constraint.Width, double.PositiveInfinity);
        var inner = new Size(Math.Max(0, constraint.Width - _lines.Margin.Left - _lines.Margin.Right), double.PositiveInfinity);
        _templatePicker.Measure(unbounded);
        _sendRow.Measure(unbounded);
        _structure.Measure(inner);
        _decorationLine.Measure(inner);
        _settingsLine.Measure(inner);

        // The lines at rest: everything but the compiled prompt's content — its header is one
        // 24 px row like the others (DESIGN.md: four rows at rest, each 24 px).
        var linesAtRest = _structure.DesiredSize.Height + _decorationLine.DesiredSize.Height + _settingsLine.DesiredSize.Height
            + DisclosureHeaderHeight + _compiledDisclosure.Margin.Top + _lines.Margin.Top + _lines.Margin.Bottom;

        var chrome = _templatePicker.DesiredSize.Height + _sendRow.DesiredSize.Height + linesAtRest;
        var compiledOpen = _compiledDisclosure.IsExpanded;
        MinimumHeight = chrome + EditorFloor + (compiledOpen ? CompiledPromptMinHeight : 0);
        // The reader's box: never over 200, never more than the writer (editor ≥ compiled —
        // INV-0007), never below the editor's floor's remainder, and never under its own floor.
        _compiled.MaxHeight = double.IsPositiveInfinity(height)
            ? CompiledPromptMaxHeight
            : Math.Max(
                CompiledPromptMinHeight,
                Math.Floor(Math.Min(CompiledPromptMaxHeight, Math.Min(height - chrome - EditorFloor, (height - chrome) / 2))));

        // THE EDITOR'S REST (Ruling 80). Under a finite belt the host's height is the belt's
        // remainder after the picker, the send row and the lines with the reader inside its ceiling
        // — capped at the rest the document asked for (EditorRest with turns; unbounded at 0 turns,
        // so the editor fills the belt), never under the floor (the host's MinHeight holds it there).
        // Under an infinite constraint with no belt the host declares only its floor (spike Q14).
        // Red observed without this: the DockPanel desired chrome + 130 whatever the belt, so the
        // editor sat at 130 under 429 px of empty thread (the operator's screenshot 1).
        if (double.IsPositiveInfinity(height))
        {
            _view.Height = double.NaN;
        }
        else
        {
            _lines.Measure(unbounded);
            var chromeWithReader = _templatePicker.DesiredSize.Height + _sendRow.DesiredSize.Height + _lines.DesiredSize.Height;
            var room = Math.Max(height, MinimumHeight) - chromeWithReader;
            _view.Height = Math.Max(EditorFloor, Math.Min(_editorRestHeight, room));
        }

        // The content is measured within the belt — or within the composer's own minimum when the
        // belt is smaller: a DesiredSize is clipped to what it was measured against, so a belt
        // passed straight through would clamp the composer exactly as a MaxHeight did and the
        // editor's floor would overlap the lines beneath it (L1's 600 px row, red by mutation).
        return base.MeasureOverride(new Size(constraint.Width, Math.Max(height, MinimumHeight)));
    }

    /// <summary>
    /// The one navigation gate. An allowed navigation replaces the document, so readiness — the
    /// router's and this surface's half — is re-keyed to the new one and its ready will push
    /// <c>host.init</c> with the draft as it stands (DC-138); a cancelled navigation replaces nothing.
    /// </summary>
    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        e.Cancel = ComposerPageContract.MustCancelNavigation(e.Uri);
        if (e.Cancel)
        {
            return;
        }

        _navigations++;
        _pageReady = false;
        _router.BeginNavigation();
        _dropsThisDocument.Clear();
        WorkbenchDiagnostics.WebSurfaceHandshake(SurfaceId, "navigation-started", _navigations, _inputs, _router.Dropped);
    }

    /// <summary>
    /// Writes the rendered bounds when they first exist and whenever the editor host's or the
    /// compiled view's height has moved more than <see cref="LayoutChangeThreshold"/> since the last
    /// line. A part whose arrange is not valid is sent as <c>null</c>, never as 0.
    /// </summary>
    internal void EmitLayout()
    {
        static double? Width(FrameworkElement e) => e.IsArrangeValid ? e.ActualWidth : null;
        static double? Height(FrameworkElement e) => e.IsArrangeValid ? e.ActualHeight : null;
        static bool Moved(double? a, double? b) =>
            a != b && (a is null || b is null || Math.Abs(a.Value - b.Value) > LayoutChangeThreshold);

        double?[] heights = [Height(_view), Height(_compiled)];
        if (_lastHeights is { } last && !heights.Zip(last).Any(pair => Moved(pair.First, pair.Second)))
        {
            return;
        }

        _lastHeights = heights;
        WorkbenchDiagnostics.ComposerLayout(
            SurfaceId,
            Width(this), Height(this),
            Width(_view), heights[0],
            Width(_compiled), heights[1],
            IsVisible, IsLoaded, _inputs);
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
        //
        // `AdditionalObjects` IS NULL FOR FOUR OF THE FIVE KINDS, and that is not a style note.
        // MEASURED by the composer probe, which prints it on every run: for a message posted with
        // plain `postMessage` the property reads `null`, so the obvious `foreach` over it threw a
        // NullReferenceException — INSIDE a multicast event invocation, which aborts the handler list,
        // and at a COM callback boundary, which swallowed it. The visible symptom was no symptom: no
        // exception, no crash, no drop counted, and every page message silently unreceived. Only
        // `attach.offered` — posted with `postMessageWithAdditionalObjects` — carries objects at all.
        var paths = new List<string>();
        try
        {
            if (e.AdditionalObjects is { } objects)
            {
                foreach (var item in objects)
                {
                    if (item is CoreWebView2File file)
                    {
                        paths.Add(file.Path);
                    }
                }
            }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // A message whose objects cannot be read carries no paths. It is still routed: the four
            // kinds that never carry one must not be lost because the fifth's accessor failed.
            paths.Clear();
        }

        var result = _router.Route(e.Source, e.WebMessageAsJson, paths);
        if (!result.Accepted && _dropsThisDocument.Add(result.Kind + "\n" + result.Reason))
        {
            // The silence DC-138 found — a mount the host refused to hear, a keystroke it refused —
            // is a line: once per kind and reason per document, so a page posting in a loop cannot
            // write the log full, and the first refusal of each shape is never lost.
            WorkbenchDiagnostics.WebSurfaceHandshake(
                SurfaceId, "message-dropped", _navigations, _inputs, _router.Dropped, $"{result.Kind}: {result.Reason}");
        }
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
    /// <summary>The single free-form field.</summary>
    public static IReadOnlyList<ComposerFieldDescriptor> FreeForm() =>
    [
        new(Mint("prompt"), "prompt", "Prompt", ComposerFieldWidget.LongText, ComposerFieldTarget.FreeForm, Required: false),
    ];

    /// <summary>
    /// The goal-block form's page half: <b>one message editor</b> (DESIGN.md SC1; Ruling 66). The
    /// three structure lines (Goal · Done when · Not in scope) are WPF controls beneath the editor,
    /// and tier, fan-out cap and budget are not fields at all (Rulings 56, 63, 72).
    /// </summary>
    public static IReadOnlyList<ComposerFieldDescriptor> GoalBlock() =>
    [
        new(Mint("message"), "message", "Message", ComposerFieldWidget.LongText, ComposerFieldTarget.FreeForm, Required: false),
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

/// <summary>
/// The editor as a focus target (DS-1 seam 1): ready only once the PAGE has mounted — a host HWND
/// with no page behind it would take focus and hold nothing to type into — and able to say whether
/// Win32 focus sits on it, which WPF's <c>Keyboard.FocusedElement</c> (null then) cannot.
/// </summary>
internal sealed class EditorFocusTarget(ICanvasFocusTarget host, Func<bool> pageReady) : ICanvasFocusTarget
{
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr GetFocus();
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr GetParent(IntPtr hWnd);

    public bool IsReady => pageReady() && host.IsReady;

    public bool IsObscured => host.IsObscured;

    public bool TryFocus() => IsReady && host.TryFocus();

    /// <summary>Whether Win32 focus is on the host's window or a descendant of it — the read a region test needs when WPF reports no focused element.</summary>
    public bool HasFocus(IntPtr hostHandle)
    {
        if (hostHandle == IntPtr.Zero)
        {
            return false;
        }

        for (var current = GetFocus(); current != IntPtr.Zero; current = GetParent(current))
        {
            if (current == hostHandle)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// One structure line beneath the editor: a label, an editable value with a watermark, and its
/// mark (Addendum D §A11's table: <i>— fill in</i> · <i>edited</i> · <i>invalid</i>).
/// </summary>
/// <remarks>
/// A plain <see cref="TextBox"/> — the line's name is constant (<i>Goal</i>), its mark is its
/// <c>ItemStatus</c> and a refusal reason its <c>HelpText</c> (SC10); an empty line's watermark is
/// a placeholder, never its value.
/// </remarks>
internal sealed class StructureLine
{
    private readonly TextBox _value;
    private readonly TextBlock _mark;
    private readonly TextBlock _why;
    private readonly TextBlock _watermark;
    private readonly Button _keep;
    private readonly Action? _onKeep;
    private string? _invalid;
    private string? _derived;
    private bool _suspect;
    private bool _kept;
    private bool _showing;

    public StructureLine(string field, string derived, Action<string> onChanged, Action? onKeep = null)
    {
        Field = field;
        _onKeep = onKeep;
        var label = new TextBlock { Text = LabelOf(field), FontSize = 12, Width = 96, VerticalAlignment = VerticalAlignment.Center };
        label.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        _value = new TextBox
        {
            Text = derived,
            AcceptsReturn = false,
            MinHeight = 24,
            FontSize = 13,
            VerticalContentAlignment = VerticalAlignment.Center,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(2, 1, 2, 1),
        };
        _value.SetResourceReference(Control.BackgroundProperty, "SurfaceRaisedBrush");
        AutomationProperties.SetName(_value, LabelOf(field));
        _value.TextChanged += (_, _) =>
        {
            if (_showing)
            {
                return;
            }

            _invalid = null;
            _kept = false;
            onChanged(_value.Text);
            RenderMark();
        };

        _keep = new Button { Content = "keep", Padding = new Thickness(6, 0, 6, 0), MinHeight = 22, Margin = new Thickness(6, 0, 0, 0), Visibility = Visibility.Collapsed };
        AutomationProperties.SetName(_keep, "Keep " + LabelOf(field));
        AutomationProperties.SetHelpText(_keep, "Keep the model's line as written.");
        _keep.Click += (_, _) => Keep();

        _watermark = new TextBlock { Text = "fill in", FontSize = 13, IsHitTestVisible = false, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) };
        _watermark.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        _mark = new TextBlock { FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        _mark.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        _why = new TextBlock { FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0), Visibility = Visibility.Collapsed };
        _why.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");

        var valueHost = new Grid();
        valueHost.Children.Add(_value);
        valueHost.Children.Add(_watermark);

        Root = new DockPanel { MinHeight = 28, LastChildFill = true };
        DockPanel.SetDock(label, Dock.Left);
        DockPanel.SetDock(_why, Dock.Right);
        DockPanel.SetDock(_keep, Dock.Right);
        DockPanel.SetDock(_mark, Dock.Right);
        Root.Children.Add(label);
        Root.Children.Add(_why);
        Root.Children.Add(_keep);
        Root.Children.Add(_mark);
        Root.Children.Add(valueHost);

        RenderMark();
    }

    public string Field { get; }

    public DockPanel Root { get; }

    public TextBox Editor => _value;

    /// <summary>The mark as rendered — one content, the line's <c>ItemStatus</c>.</summary>
    public string Mark => _mark.Text;

    public void MarkInvalid(string why)
    {
        _invalid = why;
        RenderMark();
    }

    public void Reset()
    {
        _invalid = null;
        _derived = null;
        _kept = false;
        _suspect = false;
        _value.Text = string.Empty;
        RenderMark();
    }

    /// <summary>The keep control's act: an operator row with the derived value, the mark <i>kept</i>.</summary>
    public void Keep()
    {
        if (_derived is null)
        {
            return;
        }

        _kept = true;
        _onKeep?.Invoke();
        RenderMark();
    }

    /// <summary>Shows the model's proposal with the <i>derived</i> mark (and <i>suspect</i> when the compile acted) — never an edit, never an operator row.</summary>
    public void ShowDerived(string value, bool suspect)
    {
        _showing = true;
        try
        {
            _derived = value;
            _suspect = suspect;
            _kept = false;
            _invalid = null;
            _value.Text = value;
        }
        finally
        {
            _showing = false;
        }

        RenderMark();
    }

    /// <summary>Shows the draft's value when it differs — the draft → line direction. A derived proposal on show with no draft value behind it stands (the draft holds the operator's lines, not the model's).</summary>
    public void Sync(string draftValue)
    {
        if (draftValue.Length == 0 && _derived is not null && string.Equals(_value.Text, _derived, StringComparison.Ordinal))
        {
            return;
        }

        if (!string.Equals(_value.Text, draftValue, StringComparison.Ordinal))
        {
            _value.Text = draftValue;
        }
    }

    public void RenderMark()
    {
        var empty = string.IsNullOrWhiteSpace(_value.Text);
        _watermark.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;

        if (_invalid is { } why)
        {
            _mark.Text = "! invalid";
            _why.Text = why;
            _why.Visibility = Visibility.Visible;
            AutomationProperties.SetHelpText(_value, why);
        }
        else
        {
            _mark.Text = empty ? "— fill in"
                : _kept ? "✓ kept"
                : _derived is not null && string.Equals(_value.Text, _derived, StringComparison.Ordinal) ? (_suspect ? "? derived · suspect" : "? derived")
                : "✓ edited";
            _why.Visibility = Visibility.Collapsed;
            AutomationProperties.SetHelpText(_value, string.Empty);
        }

        _keep.Visibility = _mark.Text.StartsWith("? derived", StringComparison.Ordinal) ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetItemStatus(_value, _mark.Text.TrimStart('—', '✓', '!', '?', ' '));
    }

    private static string LabelOf(string field) => field switch
    {
        GoalBlockFields.GoalKey => "Goal",
        GoalBlockFields.DoneWhenKey => "Done when",
        GoalBlockFields.NotInScopeKey => "Not in scope",
        _ => field,
    };
}

/// <summary>
/// The seam the compile step fills (Addendum D §A8; CV-2): what a structure line starts with.
/// </summary>
/// <remarks>
/// <b>A fake returning three empty strings — D-5's first slice, as the plan says.</b> Under
/// <c>mechanical-only</c> nothing derives a line (a <i>— fill in</i> mark is the truthful state);
/// the seam exists so Prepare's regions are real controls before the deriver is. It never invents a
/// plausible value.
/// </remarks>
public static class StructureDeriver
{
    public static string Fake(string field)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        return string.Empty;
    }
}

/// <summary>
/// What <i>Start a parallel session</i> hands the shell (Ruling 95): the parent draft's words and the
/// attachments that follow them — the derived sibling's first turn, sent through its own gate.
/// </summary>
/// <param name="SourceText">The draft's source text, verbatim.</param>
/// <param name="Attachments">The draft's attachments, already read through the parent's attach gate.</param>
public sealed record ParallelDraft(string SourceText, IReadOnlyList<ComposerAttachment> Attachments);
