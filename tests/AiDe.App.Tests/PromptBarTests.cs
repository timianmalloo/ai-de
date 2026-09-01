using System.Windows.Input;
using AiDe.App.Workbench;
using AiDe.Core.Facts;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The prompt-dispatch surface: what the user is told about a side effect that cannot be undone.
/// </summary>
/// <remarks>
/// These assert the <b>reporting</b>, because that is what the UI is for. Whether the two-phase
/// receipt is correct is settled in <c>BoundaryDispatchTests</c>; what matters here is that a
/// <see cref="DispatchState.DeliveryUnknown"/> is never shown as a success, and that a refusal is
/// announced rather than swallowed.
/// </remarks>
public sealed class PromptBarTests
{
    private static DispatchReceipt Receipt(DispatchState state, string? errorCode = null) =>
        new("key", state, "session", 1, errorCode, DateTimeOffset.UtcNow);

    /// <summary>
    /// Every case here builds WPF elements, which require STA. Matches CommandPaletteTests rather
    /// than introducing a second convention for the same constraint.
    /// </summary>
    private static void OnSta(Action work)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { work(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)));
        // No XunitException guard here: this harness already rethrows unwrapped, so a guard would
        // be a line that can never fire. The bulk sweep added one because the FILE contained a
        // wrapper string — in a fixture that throws a literal to simulate an error, not in the
        // harness. Removed once the gate could tell the two apart.
        if (failure is not null) throw failure;
    }

    [Fact]
    public void ADeliveredPromptIsReportedAsDelivered()
    {
        var text = PromptBar.Describe(Receipt(DispatchState.PtyWriteAccepted));
        Assert.Contains("Delivered", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DeliveryUnknownIsNeverReportedAsSuccess_AndWarnsAboutResending()
    {
        // The state the whole protocol exists to produce honestly. A UI that rounded this to "sent"
        // would be inventing the half ADR-0010 deliberately refuses to guess.
        var text = PromptBar.Describe(Receipt(DispatchState.DeliveryUnknown));

        Assert.Contains("UNKNOWN", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("resend", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Delivered.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryDispatchStateGetsItsOwnSentence()
    {
        // A shared "something happened" message would collapse exactly the distinctions the user
        // needs to decide whether it is safe to resend.
        var sentences = Enum.GetValues<DispatchState>()
            .Select(s => PromptBar.Describe(Receipt(s)))
            .ToList();

        Assert.Equal(sentences.Count, sentences.Distinct(StringComparer.Ordinal).Count());
        Assert.All(sentences, s => Assert.False(string.IsNullOrWhiteSpace(s)));
    }

    [Fact]
    public void AnEmptyPromptIsRefusedAndAnnounced_RatherThanDispatched() => OnSta(() =>
    {
        var announcer = new RecordingAnnouncer();
        var bar = new PromptBar(announcer);
        var dispatched = false;
        bar.Dispatch = _ => { dispatched = true; return Task.FromResult(Receipt(DispatchState.PtyWriteAccepted)); };

        bar.Open();
        bar.Input.Text = "   ";
        bar.DispatchAsync().GetAwaiter().GetResult();

        Assert.False(dispatched);
        Assert.Contains("empty", announcer.Last, StringComparison.OrdinalIgnoreCase);
    });

    [Fact]
    public void WithNoWorkspaceAttached_TheBarRefusesAloud_RatherThanDoingNothing() => OnSta(() =>
    {
        // DC-011: a command that silently does nothing is indistinguishable from a broken key.
        var announcer = new RecordingAnnouncer();
        var bar = new PromptBar(announcer);

        bar.Open();
        bar.Input.Text = "run the tests";
        bar.DispatchAsync().GetAwaiter().GetResult();

        Assert.False(string.IsNullOrWhiteSpace(announcer.Last));
        Assert.Contains("no terminal", announcer.Last, StringComparison.OrdinalIgnoreCase);
    });

    [Fact]
    public void ADispatchThatThrows_DoesNotClaimThePromptWasNotSent() => OnSta(() =>
    {
        // The write-ahead attempt may already be durable, so "failed" would be a claim we cannot
        // support. The user is told to check the receipt instead.
        var announcer = new RecordingAnnouncer();
        var bar = new PromptBar(announcer);
        bar.Dispatch = _ => throw new InvalidOperationException("pipe closed");

        bar.Open();
        bar.Input.Text = "run the tests";
        bar.DispatchAsync().GetAwaiter().GetResult();

        Assert.Contains("did not complete", announcer.Last, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("check the receipt", announcer.Last, StringComparison.OrdinalIgnoreCase);
    });

    [Fact]
    public void EscapeClosesTheBar_AndEnterIsConsumedWhileItIsOpen() => OnSta(() =>
    {
        var bar = new PromptBar(new RecordingAnnouncer());

        Assert.False(bar.HandleKey(Key.Escape));   // closed: not ours to consume

        bar.Open();
        Assert.True(bar.IsOpen);
        Assert.True(bar.HandleKey(Key.Enter));     // open: Enter dispatches
        Assert.True(bar.HandleKey(Key.Escape));
        Assert.False(bar.IsOpen);
    });
}
