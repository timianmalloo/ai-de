using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace AiDe.App.Workbench;

/// <summary>Announces a completed layout change to assistive technology.</summary>
public interface IWorkbenchAnnouncer
{
    /// <summary>The last thing announced — the surface a test can read back.</summary>
    string Last { get; }

    void Announce(string message);
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

    public void Announce(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Last = message;
        _liveRegion.Text = message;

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
            peer.RaiseNotificationEvent(
                AutomationNotificationKind.ActionCompleted,
                AutomationNotificationProcessing.MostRecent,
                message,
                activityId: "aide.workbench.layout");
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
    private readonly List<string> _messages = [];

    public IReadOnlyList<string> Messages => _messages;

    public string Last => _messages.Count == 0 ? string.Empty : _messages[^1];

    public void Announce(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _messages.Add(message);
        }
    }
}
