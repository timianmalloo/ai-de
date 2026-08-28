using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AiDe.Core.Facts;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// Stages a prompt and dispatches it to the focused terminal, reporting the receipt.
/// </summary>
/// <remarks>
/// <para><b>The receipt is the point, not the send.</b> A prompt delivered to an agent session is a
/// side effect that cannot be undone by the product, so what the user is shown is the recorded
/// outcome — including <see cref="DispatchState.DeliveryUnknown"/>, which means the write happened
/// but nothing survived to say whether it landed. Reporting that honestly is the whole reason the
/// two-phase receipt exists (ADR-0010); a UI that said "sent" would be inventing the half the
/// protocol deliberately refuses to guess.</para>
///
/// <para><b>Enter dispatches, Escape cancels, and dispatch is disabled while one is in flight.</b> A
/// second Enter during the round trip would produce a second command id and therefore a second
/// prompt — the idempotency key protects a RETRY of the same command, not a user pressing twice.</para>
/// </remarks>
public sealed class PromptBar
{
    private readonly IWorkbenchAnnouncer _announcer;
    private bool _inFlight;

    public PromptBar(IWorkbenchAnnouncer announcer)
    {
        _announcer = announcer ?? throw new ArgumentNullException(nameof(announcer));

        Input = new TextBox { AcceptsReturn = false, MinWidth = 420 };
        AutomationProperties.SetName(Input, "Prompt to dispatch to the focused terminal");

        Status = new TextBlock { Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(Status, "Dispatch receipt");

        Root = new Border
        {
            Visibility = Visibility.Collapsed,
            Padding = new Thickness(12),
            Background = Brushes.White,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Dispatch prompt to focused terminal" },
                    Input,
                    Status,
                },
            },
        };

        AutomationProperties.SetName(Root, "Prompt dispatch");
    }

    public Border Root { get; }

    public TextBox Input { get; }

    public TextBlock Status { get; }

    public bool IsOpen => Root.Visibility == Visibility.Visible;

    /// <summary>
    /// Performs the dispatch. Null until a workspace attaches — the bar opens and refuses, rather
    /// than being hidden, so the chord never produces silence (<b>DC-011</b>).
    /// </summary>
    public Func<string, Task<DispatchReceipt>>? Dispatch { get; set; }

    public void Open()
    {
        Root.Visibility = Visibility.Visible;
        Input.Clear();
        Status.Text = string.Empty;
        Input.Focus();
        _announcer.Announce("Dispatch prompt. Type a prompt and press Enter, or Escape to cancel.");
    }

    public void Close()
    {
        Root.Visibility = Visibility.Collapsed;
        _announcer.Announce("Prompt dispatch closed.");
    }

    /// <summary>Handles a key while the bar is open. Returns true when the key was consumed.</summary>
    public bool HandleKey(Key key)
    {
        if (!IsOpen) return false;

        switch (key)
        {
            case Key.Escape:
                Close();
                return true;

            case Key.Enter:
                _ = DispatchAsync();
                return true;

            default:
                return false;
        }
    }

    internal async Task DispatchAsync()
    {
        if (_inFlight) return;

        var body = Input.Text?.Trim() ?? string.Empty;
        if (body.Length == 0)
        {
            Report("Nothing to dispatch — the prompt is empty.");
            return;
        }

        if (Dispatch is null)
        {
            Report("No terminal is available to dispatch to.");
            return;
        }

        _inFlight = true;
        Input.IsEnabled = false;
        try
        {
            Report(await Dispatch(body));
        }
        catch (Exception ex)
        {
            // A dispatch that threw is NOT a dispatch that did not happen: the write-ahead attempt
            // may already be durable. Saying "failed" would be a claim we cannot support.
            Report($"Dispatch did not complete: {ex.Message}. Check the receipt before resending.");
        }
        finally
        {
            _inFlight = false;
            Input.IsEnabled = true;
        }
    }

    private void Report(DispatchReceipt receipt) => Report(Describe(receipt));

    private void Report(string message)
    {
        Status.Text = message;
        _announcer.Announce(message);
    }

    /// <summary>
    /// Turns a receipt into a sentence. Every state gets its own, because the differences are what
    /// the user needs to act on.
    /// </summary>
    internal static string Describe(DispatchReceipt receipt) => receipt.State switch
    {
        DispatchState.PtyWriteAccepted =>
            "Delivered. The terminal accepted the prompt.",

        DispatchState.DeliveryUnknown =>
            "DELIVERY UNKNOWN. The prompt was written but nothing recorded whether it landed. " +
            "Check the session before resending — resending may duplicate it.",

        DispatchState.Rejected =>
            "Rejected: the session moved on before the prompt was written. Nothing was delivered.",

        DispatchState.TimedOut =>
            "Timed out while writing. The prompt may or may not have reached the session.",

        DispatchState.Failed =>
            $"Failed to write to the terminal ({receipt.ErrorCode ?? "no code"}). Nothing was delivered.",

        DispatchState.Pending =>
            "Recorded but not yet resolved. The outcome will appear once the write completes.",

        _ => $"Dispatch state: {receipt.State}.",
    };
}
