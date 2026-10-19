using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// Keeps the workbench arrangement on disk across restarts (US-9).
/// </summary>
/// <remarks>
/// Everything this needs already existed — the store, the envelope, the migration chain, the
/// partial-restore reporting — and none of it had a production caller, so the layout was never
/// actually saved or loaded by the running app. This is the wiring that makes "close the
/// application, reopen the workspace, my arrangement returns" true rather than merely tested.
///
/// Saving is **debounced**: a resize drag produces an operation per arrow press or per mouse-move,
/// and writing the file on each one would turn a smooth drag into a stutter of disk writes.
/// </remarks>
public sealed class LayoutPersistence : IDisposable
{
    /// <summary>The suffix of the one-time backup the first save after a dropping restore writes (ADR-0032 rule 3).</summary>
    public const string PrePerspectivesBackupSuffix = ".pre-perspectives.bak";

    /// <summary>The suffix of the one-time backup the first save after a slot migration writes (ADR-0045 §7).</summary>
    public const string PreNotebookBackupSuffix = ".pre-notebook.bak";

    /// <summary>The suffix of the backup a refused file is kept under before its slot is rewritten (ADR-0032 rule 4).</summary>
    public const string RefusedBackupSuffix = ".bak";

    /// <summary>
    /// The zone restore's report sentence when part of the saved file could not be read and was left out,
    /// and that part held no surface with a title (T21 node 4; confirmed by the Owner).
    /// </summary>
    internal const string PartlyReadSentence = "Part of your saved layout could not be read and was left out.";

    /// <summary>
    /// The same sentence when the left-out part held titled surfaces: this prefix, then their saved
    /// titles in saved order joined with ", ", then a period (T21 node 5, the Owner's refinement).
    /// </summary>
    internal const string PartlyReadNamingPrefix = "Part of your saved layout could not be read and was left out: ";

    /// <summary>
    /// DESIGN.md DOCK4's plural (Ruling 173 (3)): the restore that migrates a slot with more than one prompt draft says
    /// this, with <c>{N}</c> replaced by the number of drafts kept.
    /// </summary>
    internal const string KeptPromptsSentence = "Kept {N} prompt drafts as they were: prompt drafts do not become notebooks.";

    private readonly ILayoutService _service;
    private readonly LayoutStore _store;
    private readonly SurfaceAvailability _availability;
    private readonly Func<StackNode, bool> _displayIsConnected;
    private readonly System.Timers.Timer _debounce;
    private bool _disposed;
    private readonly ZoneLayoutStore? _zoneStore;
    private readonly ZoneBackedLayoutService? _zoneService;
    private readonly IReadOnlySet<string> _availableSurfaces;
    private readonly IReadOnlySet<string> _restorableKinds;

    // The backup the next save owes (ADR-0032 rules 3 and 4): the pre-perspective bytes after a
    // restore that dropped something, or the refused bytes after a restore that applied nothing.
    // Cleared by the save that wrote it; null when nothing is owed. `Once` is rule 3's "once,
    // guarded by the backup's absence" — true for the pre-perspective backup only: a refused file
    // is ALWAYS preserved before the slot is rewritten, so a `.bak` from an earlier refusal is
    // replaced by the latest refused bytes (the D&P reviewer's blocker: an absence guard on that
    // path destroyed the only copy while the announcement said "kept").
    private (string Path, bool Once)? _backupDue;
    private readonly object _saveGate = new();

    // The WL-S design's §7: the old slot this slot is renamed from, and the retired slot this one folds in (null when none).
    // `_renameDue` / `_foldDue` name the old file the next save moves to `<old>.bak`, after the restore that read it.
    private readonly string? _renamedFrom;
    private readonly string? _foldedFrom;
    private string? _renameDue;
    private string? _foldDue;

    /// <param name="restorableKinds">
    /// Surface kinds the shell can build content for. Surfaces CREATED at runtime — an agent
    /// terminal, for one — have ids that no fixed list can contain, so without this they were
    /// dropped on every restart and announced as no longer available.
    /// </param>
    public LayoutPersistence(
        ILayoutService service,
        string layoutFilePath,
        IReadOnlySet<string> availableSurfaces,
        Func<StackNode, bool>? displayIsConnected = null,
        double debounceMilliseconds = 750,
        IReadOnlySet<string>? restorableKinds = null)
    {
        _service = service;
        _store = new LayoutStore(layoutFilePath);
        _availableSurfaces = availableSurfaces;
        _restorableKinds = restorableKinds ?? new HashSet<string>(StringComparer.Ordinal);
        _availability = new SurfaceAvailability(availableSurfaces, _restorableKinds);
        _displayIsConnected = displayIsConnected ?? VirtualScreen.IsOnAConnectedDisplay;

        // ADR-0021 dz-persist: when the layout is zone-based, save/restore the ZONE model (which
        // preserves collapsed content and per-zone extents the projected tree cannot), to a sibling
        // file. The tree store stays wired for the legacy service and does no harm.
        // ADR-0032 rule 1: one zone-envelope file per HOST perspective, keyed by the service's own
        // admission — a bare (unrestricted) service is the grandfathered initial-host slot.
        if (service is ZoneBackedLayoutService zbs)
        {
            _zoneService = zbs;
            _zoneStore = new ZoneLayoutStore(SlotPathFor(layoutFilePath, zbs.Admission.Perspective));
            _renamedFrom = RenamedFromFor(layoutFilePath, zbs.Admission.Perspective);
            _foldedFrom = FoldedFromFor(layoutFilePath, zbs.Admission.Perspective);
        }

        _debounce = new System.Timers.Timer(debounceMilliseconds) { AutoReset = false };
        _debounce.Elapsed += (_, _) => SaveNow();
    }

    /// <summary>
    /// The zone-envelope file of one host perspective's slot (ADR-0032 rule 1): Factory's is today's
    /// <c>&lt;layout&gt;.zones.json</c> — the empty suffix is the grandfathering decision, so a
    /// pre-Addendum-C file IS the initial host's slot, permanently (ADR-0045 §7's "slot coding →
    /// factory" is met by identity) — and every other host perspective's is the sibling
    /// <c>&lt;layout&gt;.&lt;suffix&gt;.zones.json</c>. The map is not uniform and lives only here.
    /// </summary>
    public static string SlotPathFor(string layoutFilePath, Perspective perspective)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutFilePath);
        ArgumentNullException.ThrowIfNull(perspective);

        return Sibling(layoutFilePath, perspective == PerspectiveSet.Initial ? string.Empty : "." + perspective.Id);
    }

    private static string Sibling(string layoutFilePath, string suffix) =>
        Path.Combine(Path.GetDirectoryName(layoutFilePath) ?? string.Empty, Path.GetFileNameWithoutExtension(layoutFilePath) + suffix + ".zones.json");

    // simplify: the WL-S design's §7 slot migration reads two slots that no build writes any more: Understand falls back to
    // Architecture's `<layout>.architecture.zones.json` while its own file is absent (the rename), and Factory folds the retired
    // Coordination perspective's `<layout>.coordination.zones.json` into its Center (the fold). The first save after either
    // moves the old file to `<old>.bak`, so each runs once. Ceiling: these two paths. Upgrade trigger: when no supported build
    // can have left either file, delete both arms and the .bak handling in SaveNow.
    private static string? RenamedFromFor(string layoutFilePath, Perspective perspective) =>
        perspective == PerspectiveSet.Understand ? Sibling(layoutFilePath, ".architecture") : null;

    private static string? FoldedFromFor(string layoutFilePath, Perspective perspective) =>
        perspective == PerspectiveSet.Initial ? Sibling(layoutFilePath, ".coordination") : null;

    /// <summary>The perspective whose slot this persists; <see cref="PerspectiveSet.Initial"/> for the legacy tree path.</summary>
    public Perspective Perspective => _zoneService?.Admission.Perspective ?? PerspectiveSet.Initial;

    /// <summary>
    /// What the last <see cref="Restore"/> dropped from this slot (ADR-0032 rule 2) — by surface,
    /// reason and the perspective that admits it. Empty when nothing was dropped or nothing restored.
    /// </summary>
    public IReadOnlyList<DroppedSurface> LastRestoreDropped { get; private set; } = [];

    /// <summary>
    /// The notebooks the last <see cref="Restore"/> made from saved <c>session-document</c> placements (ADR-0045 §7), in
    /// zone order. Empty when nothing was migrated or nothing restored.
    /// </summary>
    public IReadOnlyList<Surface> LastRestoreMigrated { get; private set; } = [];

    /// <summary>Why the last <see cref="SaveNow"/> wrote nothing, or null when it succeeded (or never ran).</summary>
    public string? LastSaveFailure { get; private set; }

    /// <summary>Raised when a save could not be written, with the reason. May fire on the debounce timer's thread.</summary>
    public event Action<string>? SaveFailed;

    /// <summary>The last restore's outcome — what to announce, and what could not be honoured.</summary>
    public RestoreResult? LastRestore { get; private set; }

    /// <summary>
    /// Whether the last <see cref="Restore"/> applied a <b>saved</b> arrangement, as opposed to
    /// keeping the one already on screen.
    /// </summary>
    /// <remarks>
    /// A flag rather than a test on the returned object, because <see cref="Restore"/> returns a
    /// <see cref="RestoreResult"/> in BOTH cases and never null — so the caller's old
    /// <c>restore is null ? "keep-current" : "restore-zones"</c> logged "restore-zones" every time,
    /// including the times it restored nothing. Recovering the branch from the announcement text
    /// would be a second definition of one fact (DM7); this is the first one.
    /// </remarks>
    public bool LastRestoreAppliedASavedArrangement { get; private set; }

    /// <summary>Loads the saved arrangement, or the default when there is none or it cannot be honoured.</summary>
    public RestoreResult Restore()
    {
        if (_zoneService is not null && _zoneStore is not null)
        {
            LastRestoreDropped = [];
            LastRestoreMigrated = [];
            _renameDue = null;
            _foldDue = null;
            var read = _zoneStore.Read(_availableSurfaces, _restorableKinds);

            // §7's rename: read the new name, fall back to the old. An old file that cannot be read is left where it is.
            if (read.Refusal == ZoneLoadRefusal.NoFile && _renamedFrom is not null
                && new ZoneLayoutStore(_renamedFrom).Read(_availableSurfaces, _restorableKinds) is { Layout: not null } renamed)
            {
                read = renamed;
                _renameDue = _renamedFrom;
            }

            // §7's fold: every surface of the retired slot is appended to this slot's Center as tabs, never to its Left (the
            // landing rule), so the active tab stays what this slot saved. Admission below drops and reports what it refuses.
            var saved = read.Layout;
            if ((read.Refusal is ZoneLoadRefusal.None or ZoneLoadRefusal.NoFile) && _foldedFrom is not null
                && new ZoneLayoutStore(_foldedFrom).Read(_availableSurfaces, _restorableKinds).Layout is { } retired)
            {
                saved = Fold(saved ?? _zoneService.Zones, retired);
                _foldDue = _foldedFrom;
            }

            if (saved is not null)
            {
                // ADR-0045 §7: a saved session's pane is mapped to its notebook by stable id before admission.
                var (zones, migrated, keptPrompts) = Notebook.NotebookSlotMigration.Migrate(saved);
                LastRestoreMigrated = migrated;

                // ADR-0032 rule 2: the host's service drops what this perspective does not admit and
                // reports each drop; the announcement names caption, kind and the admitting
                // perspective through the same result that reports a missing surface today.
                // Ruling 174 (3), AC-D01b: a floating window whose saved bounds fall off every display
                // loses them, so the render opens it at the default placement near the workbench. The
                // same display test and the same words as the tree path (LayoutStore.Load).
                var rehomed = new List<string>();
                foreach (var floating in zones.Floating.Where(f => !_displayIsConnected(f)).ToList())
                {
                    rehomed.Add(floating.Active.Title);
                    zones = zones with { Floating = zones.Floating.Replace(floating, floating with { FloatingBounds = null }) };
                }

                var report = _zoneService.RestoreZones(zones);
                LastRestoreDropped = report.Dropped;

                // The first save after a migration keeps today's bytes once (ADR-0045 §7). That copy holds everything the
                // pre-perspective one would, so a restore that both migrates and drops owes only the migration's.
                // A renamed slot's old bytes are the whole original, so its `.bak` is the one owed.
                _backupDue = _renameDue is not null ? (_renameDue + RefusedBackupSuffix, Once: true)
                    : migrated.Count > 0 ? (_zoneStore.FilePath + PreNotebookBackupSuffix, Once: true)
                    : report.Dropped.Count > 0 ? (_zoneStore.FilePath + PrePerspectivesBackupSuffix, Once: true)
                    : null;

                var sentences = new List<string?> { DropAnnouncement(report), KeptPromptsReport(migrated.Count, keptPrompts.Count) };
                if (rehomed.Count > 0)
                {
                    sentences.Add($"{string.Join(", ", rehomed.Select(r => $"“{r}”"))} {ZoneLayoutStore.RehomedSentenceTail}.");
                }

                if (read.PartlyRead)
                {
                    sentences.Add(read.LeftOutTitles.Count > 0
                        ? $"{PartlyReadNamingPrefix}{string.Join(", ", read.LeftOutTitles)}."
                        : PartlyReadSentence);
                }

                var result = new RestoreResult(
                    _zoneService.Current,
                    report.DefaultApplied,
                    report.Dropped.Count > 0 || rehomed.Count > 0 || read.PartlyRead ? LayoutErrorCodes.PartialRestore : null,
                    [.. report.Dropped.Select(d => Caption(d))],
                    rehomed,
                    string.Join(" ", sentences.Where(part => !string.IsNullOrEmpty(part))));
                LastRestore = result;
                LastRestoreAppliedASavedArrangement = true;
                return result;
            }

            // No saved zone layout: keep the current arrangement rather than resetting. A REFUSED
            // file (newer schema, corrupt) is kept too — but reported with its reason, and its
            // bytes are owed a backup before this slot's first save rewrites it (ADR-0032 rule 4).
            var (code, why) = read.Refusal switch
            {
                ZoneLoadRefusal.NewerSchema => (LayoutErrorCodes.VersionUnsupported, "was written by a newer version of AI-DE"),
                ZoneLoadRefusal.Corrupt => (LayoutErrorCodes.Unreadable, "could not be read"),
                _ => ((string?)null, (string?)null),
            };
            _backupDue = code is null ? null : (_zoneStore.FilePath + RefusedBackupSuffix, Once: false);   // preserved every time

            var kept = new RestoreResult(_zoneService.Current, false, code, [], [],
                code is null
                    ? "Kept the current workbench arrangement."
                    : $"Your saved {Perspective.Title} layout {why} and was not applied; its bytes are copied to "
                      + $"{Path.GetFileName(_zoneStore.FilePath)}{RefusedBackupSuffix} (replacing an older copy) when the layout is next saved.");
            LastRestore = kept;
            LastRestoreAppliedASavedArrangement = false;
            return kept;
        }

        var treeResult = _store.Load(_availability, _displayIsConnected);
        LastRestore = treeResult;

        // The legacy tree path has no "kept" branch: it either loaded a saved layout or fell back to
        // the default, which WasDefaulted reports.
        LastRestoreAppliedASavedArrangement = !treeResult.WasDefaulted;
        _service.Restore(treeResult.Layout);
        return treeResult;
    }

    /// <summary>Schedules a save. Repeated calls within the debounce window collapse into one write.</summary>
    public void MarkDirty()
    {
        if (_disposed)
        {
            return;
        }

        _debounce.Stop();
        _debounce.Start();
    }

    /// <summary>Writes immediately — used on shutdown, where a pending debounce would be lost.</summary>
    public void SaveNow()
    {
        if (_disposed)
        {
            return;
        }

        // One save at a time: the debounce timer's thread and the UI thread's shutdown flush share
        // one temp path, and the loser of a race would report a sharing violation as "not saved".
        lock (_saveGate)
        {
            string? backup = null;
            try
            {
                if (_zoneService is not null && _zoneStore is not null)
                {
                    // ADR-0032 rules 3 and 4: the backup owed by the last restore rides the SAME
                    // replace that would otherwise lose the bytes — the pre-perspective copy once,
                    // guarded by its absence; the refused copy every time.
                    backup = _backupDue is { } due && !(due.Once && File.Exists(due.Path)) ? due.Path : null;

                    // §7's rename: the old file is moved onto the new name, so the replace below keeps its bytes as `<old>.bak`.
                    if (_renameDue is { } old && File.Exists(old) && !File.Exists(_zoneStore.FilePath))
                    {
                        File.Move(old, _zoneStore.FilePath);
                    }

                    _zoneStore.Save(_zoneService.Zones, backup); // zone-faithful: keeps collapsed content + extents
                    _backupDue = null;
                    _renameDue = null;

                    // §7's fold: once the folded tabs are written here, the retired slot is kept once as `<old>.bak`. An
                    // existing `.bak` is never replaced; the retired file then stays, and the next restore folds it again.
                    if (_foldDue is { } folded && File.Exists(folded) && !File.Exists(folded + RefusedBackupSuffix))
                    {
                        File.Move(folded, folded + RefusedBackupSuffix);
                    }

                    _foldDue = null;
                }
                else
                {
                    _store.Save(_service.Current);
                }

                LastSaveFailure = null;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // A layout that cannot be written is an annoyance, never a reason to fail the
                // operation the user actually asked for. It degrades to "not saved", not to a crash
                // on exit — and it is REPORTED, never swallowed: a refused replace leaves the only
                // original intact and says so (ADR-0032 rule 3's fail-safe half).
                var file = Path.GetFileName(_zoneStore?.FilePath ?? "layout.json");
                LastSaveFailure = backup is null
                    ? $"The {Perspective.Title} layout was not saved ({file}): {error.Message}"
                    : $"The {Perspective.Title} layout was not saved ({file}; its backup {Path.GetFileName(backup)} could not be written, so the original was left untouched): {error.Message}";
                SaveFailed?.Invoke(LastSaveFailure);
            }
        }
    }

    /// <summary>
    /// The surface as the report names it: its caption, with the kind's title when that differs —
    /// or, for a kind the product has retired, the caption and the retirement sentence (the ruling
    /// and where the content went), since no perspective can be named as its home.
    /// </summary>
    private static string Caption(DroppedSurface dropped)
    {
        if (SurfaceContentFactory.RetiredKinds.TryGetValue(dropped.Surface.Kind, out var retired))
        {
            return $"{dropped.Surface.Title} ({retired})";
        }

        var kindTitle = SurfaceContentFactory.Kinds.FirstOrDefault(k => string.Equals(k.Kind, dropped.Surface.Kind, StringComparison.Ordinal))?.Title
            ?? dropped.Surface.Kind;
        var caption = string.Equals(dropped.Surface.Title, kindTitle, StringComparison.Ordinal)
            ? dropped.Surface.Title
            : $"{dropped.Surface.Title} ({char.ToLowerInvariant(kindTitle[0]) + kindTitle[1..]})";

        return dropped.Reason == DropReason.DuplicateOneInstance
            ? $"a second {caption} (the first was kept)"
            : caption;
    }

    /// <summary>
    /// <paramref name="into"/> with every surface of <paramref name="retired"/> it does not already hold appended to its Center
    /// as tabs, in zone order and then floating (the WL-S design's §7). The Center's active tab is unchanged.
    /// </summary>
    private static WorkbenchLayout Fold(WorkbenchLayout into, WorkbenchLayout retired)
    {
        var taken = into.AllSurfaces().Select(s => s.SurfaceId).ToHashSet(StringComparer.Ordinal);
        var tabs = Enum.GetValues<ZoneId>().SelectMany(id => retired.Zone(id).Surfaces())
            .Concat(retired.Floating.SelectMany(f => f.Surfaces))
            .Where(s => taken.Add(s.SurfaceId))
            .ToImmutableList();
        if (tabs.IsEmpty)
        {
            return into;
        }

        var center = into.Zone(ZoneId.Center);
        return into.WithZone(center with { Content = Append(center.Content, tabs) });
    }

    // A split Center takes the tabs in its first stack.
    private static ZoneContent Append(ZoneContent? content, ImmutableList<Surface> tabs) => content switch
    {
        null => new ZoneStack(tabs),
        ZoneStack stack => stack with { Tabs = stack.Tabs.AddRange(tabs) },
        ZoneSplit split => split with { Children = split.Children.SetItem(0, Append(split.Children[0], tabs)) },
        _ => throw new InvalidOperationException($"zone content '{content.GetType().Name}' has no fold"),
    };

    /// <summary>
    /// Ruling 173 (3): the restore that migrates a slot says that its prompt drafts were kept as they were, because the
    /// notebook does not replace them yet. Null when this restore migrated nothing (the sentence is the migration's, said
    /// once) or the slot holds no prompt draft.
    /// </summary>
    private static string? KeptPromptsReport(int migrated, int kept) => (migrated, kept) switch
    {
        (0, _) or (_, 0) => null,
        (_, 1) => "Kept 1 prompt draft as it was: prompt drafts do not become notebooks.",
        _ => KeptPromptsSentence.Replace("{N}", kept.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal),
    };

    /// <summary>The drop-with-report sentence (spec §C4, plural forms as fixed there), or the plain restore sentence.</summary>
    private string DropAnnouncement(ZoneRestoreReport report)
    {
        if (report.DefaultApplied)
        {
            return $"Your saved layout had no panes {Perspective.Title} can show, so {Perspective.Title} opened with its default layout.";
        }

        if (report.Dropped.Count == 0)
        {
            return "Restored your saved workbench arrangement.";
        }

        var list = string.Join(", ", report.Dropped.Select(Caption));
        var head = report.Dropped.Count == 1
            ? $"1 pane from your saved layout isn't available in {Perspective.Title}"
            : $"{report.Dropped.Count} panes from your saved layout aren't available in {Perspective.Title}";

        // Where the dropped panes live, by the perspective that admits them, with its bound gesture
        // (DESIGN.md's drop-with-report form: an envelope read into Factory names Understand for
        // the graph kinds). One admitting
        // perspective is the common case and reads as spec §C4's sentence — the list, then where
        // they live; two or more list each pane ONCE under its perspective, gesture first, so the
        // operator learns which gesture reaches which pane without hearing every caption twice
        // (the UX & Accessibility lens's finding: the live region speaks the whole sentence and
        // the strip shows its head). A drop no perspective admits (a duplicate one-instance kind)
        // is in the flat list and in no group.
        var groups = report.Dropped
            .Where(d => d.AdmittedBy is not null)
            .GroupBy(d => d.AdmittedBy!)
            .ToList();

        static string Home(Perspective perspective) => $"{perspective.Title} ({perspective.Gesture})";

        return groups.Count switch
        {
            0 => $"{head} — {list}.",
            1 when groups[0].Count() == 1 => $"{head} — {list}. It lives in {Home(groups[0].Key)}; open it from its View menu.",
            1 => $"{head} — {list}. They live in {Home(groups[0].Key)}; open them from its View menu.",
            _ => $"{head} — "
                + string.Join("; ", groups.Select(g => $"{g.Count()} {(g.Count() == 1 ? "lives" : "live")} in {Home(g.Key)}: {string.Join(", ", g.Select(Caption))}"))
                + (report.Dropped.Count > groups.Sum(g => g.Count()) ? $"; also {string.Join(", ", report.Dropped.Where(d => d.AdmittedBy is null).Select(Caption))}" : string.Empty)
                + ". Open each from that perspective's View menu.",
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Flush before disposing: the most common moment to lose an arrangement is the one where the
        // user rearranged and immediately closed the app.
        _debounce.Stop();
        SaveNow();
        _disposed = true;
        _debounce.Dispose();
    }
}

/// <summary>Whether a floating pane's saved position still lands on a display that exists.</summary>
internal static class VirtualScreen
{
    /// <summary>
    /// True when the pane's saved rectangle meaningfully overlaps the virtual screen.
    /// </summary>
    /// <remarks>
    /// Uses the virtual screen rather than per-monitor enumeration because the question is only
    /// "can the user reach this window", and a pane spanning two displays is still reachable. A pane
    /// with no saved bounds counts as on-screen: the shell will place it, so there is nothing to fix.
    /// </remarks>
    internal static bool IsOnAConnectedDisplay(StackNode stack)
    {
        if (stack.FloatingBounds is not { } bounds)
        {
            return true;
        }

        var screen = new LayoutRect(
            System.Windows.SystemParameters.VirtualScreenLeft,
            System.Windows.SystemParameters.VirtualScreenTop,
            System.Windows.SystemParameters.VirtualScreenWidth,
            System.Windows.SystemParameters.VirtualScreenHeight);

        var overlapWidth = Math.Min(bounds.Right, screen.Right) - Math.Max(bounds.X, screen.X);
        var overlapHeight = Math.Min(bounds.Bottom, screen.Bottom) - Math.Max(bounds.Y, screen.Y);

        // A sliver of a title bar is not "reachable": require enough of the pane to be grabbable.
        const double MinimumVisible = 48;
        return overlapWidth >= MinimumVisible && overlapHeight >= MinimumVisible;
    }
}
