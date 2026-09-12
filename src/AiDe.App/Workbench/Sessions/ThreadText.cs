using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// A <see cref="TextBlock"/> whose UIA peer is a <b>Control-view</b> element (DS-1 U1; SC10):
/// the words, the reply, the decoration line and the event lines of a turn must be reachable by an
/// AT walking the Control view, and a <see cref="TextBlock"/> inside a <c>DataTemplate</c> is a
/// content element only by default.
/// </summary>
/// <remarks>
/// The reply and the event lines are model- and lane-authored: rendered as <see cref="TextBlock.Text"/>,
/// never as inlines or a hyperlink (Addendum D: no link activation from model-authored content;
/// DS-1 S1).
/// </remarks>
public sealed class ThreadText : TextBlock
{
    protected override AutomationPeer OnCreateAutomationPeer() => new ThreadTextAutomationPeer(this);

    private sealed class ThreadTextAutomationPeer(ThreadText owner) : TextBlockAutomationPeer(owner)
    {
        protected override bool IsControlElementCore() => true;
    }
}
