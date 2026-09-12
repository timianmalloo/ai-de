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

    /// <summary>The suffix of the backup a refused file is kept under before its slot is rewritten (ADR-0032 rule 4).</summary>
    public const string RefusedBackupSuffix = ".bak";

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
        // admission — a bare (unrestricted) service is the grandfathered Coding slot.
        if (service is ZoneBackedLayoutService zbs)
        {
            _zoneService = zbs;
            _zoneStore = new ZoneLayoutStore(SlotPathFor(layoutFilePath, zbs.Admission.Perspective));
        }

        _debounce = new System.Timers.Timer(debounceMilliseconds) { AutoReset = false };
        _debounce.Elapsed += (_, _) => SaveNow();
    }

    /// <summary>
    /// The zone-envelope file of one host perspective's slot (ADR-0032 rule 1): Coding's is today's
    /// <c>&lt;layout&gt;.zones.json</c> — the empty suffix is the grandfathering decision, so a
    /// pre-Addendum-C file IS the Coding slot — and every other host perspective's is the sibling
    /// <c>&lt;layout&gt;.&lt;perspective&gt;.zones.json</c>. The map is not uniform and lives only here.
    /// A full-window perspective has no slot (Addendum C non-goal 5).
    /// </summary>
    public static string SlotPathFor(string layoutFilePath, Perspective perspective)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutFilePath);
        ArgumentNullException.ThrowIfNull(perspective);

        if (perspective.Body != PerspectiveBody.DockHost)
        {
            throw new ArgumentException($"'{perspective.Id}' is a full-window perspective and has no layout slot", nameof(perspective));
        }

        var dir = Path.GetDirectoryName(layoutFilePath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(layoutFilePath);
        var suffix = perspective == PerspectiveSet.Coding ? string.Empty : "." + perspective.Id;
        return Path.Combine(dir, name + suffix + ".zones.json");
    }

    /// <summary>The perspective whose slot this persists; <see cref="PerspectiveSet.Coding"/> for the legacy tree path.</summary>
    public Perspective Perspective => _zoneService?.Admission.Perspective ?? PerspectiveSet.Coding;

    /// <summary>
    /// What the last <see cref="Restore"/> dropped from this slot (ADR-0032 rule 2) — by surface,
    /// reason and the perspective that admits it. Empty when nothing was dropped or nothing restored.
    /// </summary>
    public IReadOnlyList<DroppedSurface> LastRestoreDropped { get; private set; } = [];

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
            var read = _zoneStore.Read(_availableSurfaces, _restorableKinds);
            if (read.Layout is { } zones)
            {
                // ADR-0032 rule 2: the host's service drops what this perspective does not admit and
                // reports each drop; the announcement names caption, kind and the admitting
                // perspective through the same result that reports a missing surface today.
                var report = _zoneService.RestoreZones(zones);
                LastRestoreDropped = report.Dropped;
                _backupDue = report.Dropped.Count > 0 ? (_zoneStore.FilePath + PrePerspectivesBackupSuffix, Once: true) : null;

                var result = new RestoreResult(
                    _zoneService.Current,
                    report.DefaultApplied,
                    report.Dropped.Count > 0 ? LayoutErrorCodes.PartialRestore : null,
                    [.. report.Dropped.Select(d => Caption(d))],
                    [],
                    DropAnnouncement(report));
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
                    _zoneStore.Save(_zoneService.Zones, backup); // zone-faithful: keeps collapsed content + extents
                    _backupDue = null;
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

    /// <summary>The surface as the report names it: its caption, with the kind's title when that differs.</summary>
    private static string Caption(DroppedSurface dropped)
    {
        var kindTitle = SurfaceContentFactory.Kinds.FirstOrDefault(k => string.Equals(k.Kind, dropped.Surface.Kind, StringComparison.Ordinal))?.Title
            ?? dropped.Surface.Kind;
        var caption = string.Equals(dropped.Surface.Title, kindTitle, StringComparison.Ordinal)
            ? dropped.Surface.Title
            : $"{dropped.Surface.Title} ({char.ToLowerInvariant(kindTitle[0]) + kindTitle[1..]})";

        return dropped.Reason == DropReason.DuplicateOneInstance
            ? $"a second {caption} (the first was kept)"
            : caption;
    }

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
            ? $"1 pane from your saved layout isn't available in {Perspective.Title} — {list}."
            : $"{report.Dropped.Count} panes from your saved layout aren't available in {Perspective.Title} — {list}.";

        var admitting = report.Dropped
            .Where(d => d.AdmittedBy is not null)
            .Select(d => d.AdmittedBy!.Title)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return admitting.Count == 0
            ? head
            : $"{head} {string.Join(" and ", admitting)} admits them; open them from its View menu.";
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
