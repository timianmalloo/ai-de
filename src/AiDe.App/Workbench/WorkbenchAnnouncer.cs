using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Workbench;

/// <summary>Announces a completed layout change to assistive technology.</summary>
public interface IWorkbenchAnnouncer
{
    /// <summary>The last thing announced — the surface a test can read back.</summary>
    string Last { get; }

    void Announce(string message);

    /// <summary>
    /// Announces with an urgency and a kind (SC9; DS-1 P6): a status is queued after what is being
    /// read, an assertive announcement interrupts it. <c>Announce(string)</c> is the status form.
    /// </summary>
    /// <remarks>
    /// Additive by construction: an implementer that knows only the status form (the shell's test
    /// doubles predate this member — the merge of SH-2 met one) speaks the text as a status. The
    /// two real announcers override it: the workbench's raises the mapped notification, the
    /// recording one keeps the urgency for a test to read.
    /// </remarks>
    void Announce(Announcement announcement)
    {
        ArgumentNullException.ThrowIfNull(announcement);
        Announce(announcement.Text);
    }

    /// <summary>
    /// Empties the status line.
    /// </summary>
    /// <remarks>
    /// A status message has no natural end — it sits there until something else happens, and the
    /// last thing that happened is often the longest thing that happened. A reader needs to be able
    /// to put it away. Clearing is not the same as announcing an empty string: `Announce`
    /// deliberately ignores blank input, so a caller cannot wipe the line by accident.
    /// </remarks>
    void Clear();
}

/// <summary>
/// Speaks layout changes to assistive technology **without moving focus** (SC 4.1.3 Status Messages).
/// </summary>
/// <remarks>
/// Two mechanisms, deliberately together rather than either/or:
/// <list type="number">
/// <item><b>A UIA notification</b> (<see cref="AutomationPeer.RaiseNotificationEvent"/>) — the
/// modern, purpose-built channel for "tell the user something happened here", which screen readers
/// announce without changing the focus or the reading position.</item>
/// <item><b>A polite live region</b> — the older mechanism, kept because notification support varies
/// by screen reader and version. A layout change that reaches neither is a silent change, which is
/// the failure this class exists to prevent.</item>
/// </list>
/// Focus is never touched. That is the whole point: an operator who has just floated a pane with the
/// keyboard should hear that it happened and still be exactly where they were.
///
/// No exemplar documents doing this at all — see the spec's workbench exemplar evidence — so this is
/// the one place AI-DE is deliberately ahead of the category rather than matching it.
/// </remarks>
public sealed class WorkbenchAnnouncer : IWorkbenchAnnouncer
{
    private readonly TextBlock _liveRegion;

    public WorkbenchAnnouncer(TextBlock liveRegion)
    {
        _liveRegion = liveRegion;
        AutomationProperties.SetLiveSetting(_liveRegion, AutomationLiveSetting.Polite);
        AutomationProperties.SetName(_liveRegion, "Workbench status");
    }

    public string Last { get; private set; } = string.Empty;

    /// <summary>
    /// The raise seam (DS-1 A2): what the last notification was raised with —
    /// <c>(kind, processing, activityId)</c> — so a test can hold the announcer to the mapping it
    /// claims rather than trusting the call was made. Null until the first announcement.
    /// </summary>
    public (AutomationNotificationKind Kind, AutomationNotificationProcessing Processing, string ActivityId)? LastRaise { get; private set; }

    /// <inheritdoc/>
    public void Announce(Announcement announcement)
    {
        ArgumentNullException.ThrowIfNull(announcement);
        var (kind, processing) = NotificationMapping.For(announcement.Urgency, announcement.Kind);
        Announce(announcement.Text, kind, processing, NotificationMapping.ThreadActivityId);
    }

    public void Clear()
    {
        if (!_liveRegion.Dispatcher.CheckAccess())
        {
            _liveRegion.Dispatcher.InvokeAsync(Clear);
            return;
        }

        // `Last` is emptied too: it is what a test reads back, and a cleared line that still reports
        // its old text is a surface disagreeing with itself.
        Last = string.Empty;
        _liveRegion.Text = string.Empty;
    }

    public void Announce(string message) =>
        Announce(message, AutomationNotificationKind.ActionCompleted, AutomationNotificationProcessing.MostRecent, "aide.workbench.layout");

    private void Announce(string message, AutomationNotificationKind kind, AutomationNotificationProcessing processing, string activityId)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        // Marshalled here, not by every caller. This type owns the control and is therefore the only
        // one that knows a dispatcher is involved — and the moment an announcement can come from
        // background work (a re-index reporting its outcome), a caller that forgot would throw at
        // exactly the point it was trying to tell the user something.
        if (!_liveRegion.Dispatcher.CheckAccess())
        {
            _liveRegion.Dispatcher.InvokeAsync(() => Announce(message, kind, processing, activityId));
            return;
        }

        Last = message;
        _liveRegion.Text = message;
        // The visible strip is one line with an ellipsis (it must never grow and eat the window); the
        // full announcement — which can be a long re-index disclosure list — stays available on hover.
        _liveRegion.ToolTip = message.Length > 80 ? message : null;

        var peer = UIElementAutomationPeer.FromElement(_liveRegion)
            ?? UIElementAutomationPeer.CreatePeerForElement(_liveRegion);
        if (peer is null)
        {
            // No peer means no AT is listening; the visible strip still carries the message, so the
            // sighted path is unaffected. Degrading to "not announced" is correct here — inventing a
            // success would be worse than a quiet no-op.
            return;
        }

        peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);

        try
        {
            LastRaise = (kind, processing, activityId);
            peer.RaiseNotificationEvent(kind, processing, message, activityId);
        }
        catch (PlatformNotSupportedException)
        {
            // Older Windows builds have no notification API. The live region above already fired,
            // which is exactly why both mechanisms are used rather than one.
        }
    }
}

/// <summary>A headless announcer for tests and for any host without a live region yet.</summary>
public sealed class RecordingAnnouncer : IWorkbenchAnnouncer
{
    // Guarded because announcements now arrive from background work as well as from the UI thread:
    // a re-index reports its outcome when it finishes. An unsynchronised List would corrupt or throw
    // under exactly the case this records.
    private readonly System.Threading.Lock _gate = new();
    private readonly List<string> _messages = [];

    public IReadOnlyList<string> Messages
    {
        get
        {
            lock (_gate)
            {
                return [.. _messages];
            }
        }
    }

    public string Last
    {
        get
        {
            lock (_gate)
            {
                return _messages.Count == 0 ? string.Empty : _messages[^1];
            }
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _messages.Clear();
        }
    }

    public void Announce(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        lock (_gate)
        {
            _messages.Add(message);
        }
    }

    /// <summary>Every typed announcement, in order — the urgency and kind a test reads beside the text.</summary>
    public IReadOnlyList<Announcement> Announcements
    {
        get
        {
            lock (_gate)
            {
                return [.. _announcements];
            }
        }
    }

    private readonly List<Announcement> _announcements = [];

    /// <inheritdoc/>
    public void Announce(Announcement announcement)
    {
        ArgumentNullException.ThrowIfNull(announcement);

        lock (_gate)
        {
            _announcements.Add(announcement);
        }

        Announce(announcement.Text);
    }
}

/// <summary>
/// The pure mapping from an announcement's urgency and kind to the UIA notification API's two
/// enums (DS-1 A2) — on NVDA's real processing semantics: <c>event_UIA_notification</c> cancels
/// speech only for <c>MostRecent</c> / <c>ImportantMostRecent</c>, so a status is <c>All</c>
/// (queued) and an assertive announcement <c>ImportantMostRecent</c> (interrupts).
/// </summary>
public static class NotificationMapping
{
    /// <summary>The activity id every thread announcement is raised under.</summary>
    public const string ThreadActivityId = "aide.session.thread";

    public static (AutomationNotificationKind Kind, AutomationNotificationProcessing Processing) For(Urgency urgency, AnnouncementKind kind)
    {
        var processing = urgency switch
        {
            Urgency.Status => AutomationNotificationProcessing.All,
            Urgency.Assertive => AutomationNotificationProcessing.ImportantMostRecent,
            _ => throw new ArgumentOutOfRangeException(nameof(urgency), urgency, "unknown urgency"),
        };

        var notification = kind switch
        {
            AnnouncementKind.ItemAdded => AutomationNotificationKind.ItemAdded,
            AnnouncementKind.Completed => AutomationNotificationKind.ActionCompleted,
            AnnouncementKind.Aborted => AutomationNotificationKind.ActionAborted,
            AnnouncementKind.Other => AutomationNotificationKind.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "unknown announcement kind"),
        };

        return (notification, processing);
    }
}
