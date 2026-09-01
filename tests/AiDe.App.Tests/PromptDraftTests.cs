using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The transfer rules for the prompt-draft surface (spec-editor-surfaces US-ED5–ED7). The view-model
/// is pure host-side logic — no terminal, no WebView2 — so these run without an STA thread: the
/// dispatch and the live target list are injected.
/// </summary>
public sealed class PromptDraftTests
{
    private static PromptDraftViewModel Vm(
        IReadOnlyList<PromptTarget> targets,
        List<(string id, string body)>? sent = null,
        bool accept = true)
    {
        return new PromptDraftViewModel(
            () => targets,
            (id, body) => { sent?.Add((id, body)); return Task.FromResult(accept); });
    }

    [Fact]
    public void CanTransfer_False_WhenNoReadyTarget()
    {
        var vm = Vm([]) ; vm.Body = "do the thing";
        Assert.False(vm.HasReadyTarget);
        Assert.False(vm.CanTransfer);
        Assert.Contains("ready", vm.BlockedReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanTransfer_False_WhenBodyEmpty()
    {
        var vm = Vm([new PromptTarget("s1", "copilot")]);
        vm.Body = "   ";
        Assert.True(vm.HasReadyTarget);
        Assert.False(vm.CanTransfer);
        Assert.Contains("Write a prompt", vm.BlockedReason);
    }

    [Fact]
    public void CanTransfer_True_WithTargetAndBody()
    {
        var vm = Vm([new PromptTarget("s1", "copilot")]);
        vm.Body = "refactor Total()";
        Assert.True(vm.CanTransfer);
        Assert.Equal("", vm.BlockedReason);
    }

    [Fact]
    public async Task Transfer_SendsBodyToSelectedTarget_AndIsOneWay()
    {
        var sent = new List<(string id, string body)>();
        var vm = Vm([new PromptTarget("s1", "copilot"), new PromptTarget("s2", "claude")], sent);
        vm.Body = "add a test first";
        vm.SelectedTargetId = "s2";

        Assert.True(await vm.TransferAsync());
        Assert.Single(sent);
        Assert.Equal(("s2", "add a test first"), sent[0]);
        Assert.True(vm.Transferred);
        Assert.False(vm.CanTransfer);                       // one-way

        // A second transfer does nothing (US-ED7: the session owns it now).
        Assert.False(await vm.TransferAsync());
        Assert.Single(sent);
    }

    [Fact]
    public async Task Transfer_DefaultsToFirstReady_WhenNoneSelected()
    {
        var sent = new List<(string id, string body)>();
        var vm = Vm([new PromptTarget("s1", "copilot"), new PromptTarget("s2", "claude")], sent);
        vm.Body = "hello";

        Assert.True(await vm.TransferAsync());
        Assert.Equal("s1", sent[0].id);
    }

    [Fact]
    public async Task Transfer_DefaultsToFirstReady_WhenSelectionIsStale()
    {
        var sent = new List<(string id, string body)>();
        var vm = Vm([new PromptTarget("s1", "copilot")], sent);
        vm.Body = "hello";
        vm.SelectedTargetId = "gone";                        // a session that is no longer ready

        Assert.True(await vm.TransferAsync());
        Assert.Equal("s1", sent[0].id);                      // falls back to a real ready target
    }

    [Fact]
    public async Task Transfer_RejectedByDispatch_LeavesDraftRetryable()
    {
        var sent = new List<(string id, string body)>();
        var vm = Vm([new PromptTarget("s1", "copilot")], sent, accept: false);
        vm.Body = "hello";

        Assert.False(await vm.TransferAsync());
        Assert.False(vm.Transferred);                        // not consumed
        Assert.True(vm.CanTransfer);                         // can retry
    }
}
