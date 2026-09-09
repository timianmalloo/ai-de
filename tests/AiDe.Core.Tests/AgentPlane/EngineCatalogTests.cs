using AiDe.Core.AgentPlane;

namespace AiDe.Core.Tests.AgentPlane;

/// <summary>
/// Engines are <b>data, not code paths</b> (spec §4.1) — and exactly one launch path exists.
/// </summary>
/// <remarks>
/// <para><b>Why the refusals are the important half.</b> Phase 1 launches <c>claude-code</c> through
/// its ACP adapter and nothing else. The deferral of the <c>codex</c> and <c>copilot</c> launch
/// paths to Phase 3+ is <i>enforced rather than remembered</i>: every refusal below goes red the
/// moment someone half-implements a second path, which is the model the programme's other
/// deferrals should imitate.</para>
///
/// <para><b>Every refusal names its reason.</b> A silent default is the failure this catalog exists
/// to prevent — it would launch the wrong engine, or the right engine the wrong way, and look like
/// success.</para>
///
/// <para><b>The pins come from the errata note, not from memory.</b> The spec names no adapter
/// package at all (verified: zero hits), so the package identity is catalog data pinned in
/// <c>docs/notes/conductor-spec-errata-policy.md</c>. Reaching for the superseded
/// <c>@zed-industries/claude-code-acp</c> name from memory silently yields a March build.</para>
/// </remarks>
public sealed class EngineCatalogTests
{
    private const string InstallRoot = @"C:\repo\spikes\acp-subscription-lane";

    /// <summary>
    /// The three rows load as data, with the pinned packages and declared ACP modes.
    /// </summary>
    /// <remarks>
    /// fixture-derivation: ok — these literals are the pinned catalog facts from
    /// docs/notes/conductor-spec-errata-policy.md and spec §14.2. Pinning them is the case's whole
    /// purpose: a version that drifts without a recorded re-pin is the drift this catches.
    /// </remarks>
    [Fact]
    public void ThreeEngineRowsLoadWithTheirPinnedPackages()
    {
        Assert.Equal(3, EngineCatalog.Rows.Count);

        var claude = EngineCatalog.Find("claude-code");
        Assert.Equal("anthropic", claude.Provider);
        Assert.Equal(AcpMode.Adapter, claude.Acp);
        Assert.Equal("@agentclientprotocol/claude-agent-acp", claude.AdapterPackage);
        Assert.Equal("0.75.1", claude.AdapterVersion);

        var codex = EngineCatalog.Find("codex");
        Assert.Equal("openai", codex.Provider);
        Assert.Equal(AcpMode.Adapter, codex.Acp);
        Assert.Equal("@agentclientprotocol/codex-acp", codex.AdapterPackage);
        Assert.Equal("1.10.0", codex.AdapterVersion);

        var copilot = EngineCatalog.Find("copilot");
        Assert.Equal("github", copilot.Provider);
        Assert.Equal(AcpMode.Native, copilot.Acp);
        Assert.Null(copilot.AdapterPackage);
    }

    /// <summary>Every adapter row pins both a package and a version — an unpinned adapter is drift.</summary>
    [Fact]
    public void EveryAdapterRowPinsBothAPackageAndAVersion()
    {
        foreach (var row in EngineCatalog.Rows.Where(r => r.Acp == AcpMode.Adapter))
        {
            Assert.False(string.IsNullOrWhiteSpace(row.AdapterPackage), row.Id + " pins no package");
            Assert.False(string.IsNullOrWhiteSpace(row.AdapterVersion), row.Id + " pins no version");
        }
    }

    /// <summary>The one launch path that is exercised: an adapter engine whose entry module was observed.</summary>
    [Fact]
    public void TheAdapterLaunchPathResolvesForClaudeCode()
    {
        var launch = EngineCatalog.ResolveLaunch("claude-code", InstallRoot);

        Assert.Equal("node", launch.FileName);
        var module = Assert.Single(launch.Arguments);
        Assert.Equal(
            Path.Combine(InstallRoot, "node_modules", "@agentclientprotocol/claude-agent-acp", "dist", "index.js"),
            module);
    }

    /// <summary>
    /// An engine whose ACP mode is not <c>adapter</c> is refused, and the reason names the engine
    /// and the mode.
    /// </summary>
    /// <remarks>
    /// This is the clause that keeps the <c>copilot</c> deferral enforced. Implementing a native
    /// launch turns this red, which is the intended alarm — the upgrade trigger firing, not a
    /// broken test.
    /// </remarks>
    [Fact]
    public void ANonAdapterModeIsRefusedWithANamedReason()
    {
        var error = Assert.Throws<AgentPlaneException>(
            () => EngineCatalog.ResolveLaunch("copilot", InstallRoot));

        Assert.Equal(AgentPlaneErrorCodes.LaunchPathNotImplemented, error.Code);
        Assert.Contains("copilot", error.Message, StringComparison.Ordinal);
        Assert.Contains("native", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("adapter", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// An adapter engine whose entry module has never been observed is refused too — the catalog
    /// does not guess a path it has not seen.
    /// </summary>
    /// <remarks>
    /// <c>codex</c> declares <c>acp: adapter</c> in spec §14.2, so the mode gate alone would let it
    /// through. Its entry module has never been observed on a real install, and inventing
    /// <c>dist/index.js</c> by analogy with the Claude adapter is a guess that would fail at spawn
    /// time with a file-not-found. Refusing here is what actually enforces the codex deferral.
    /// </remarks>
    [Fact]
    public void AnAdapterWithNoObservedEntryModuleIsRefusedRatherThanGuessed()
    {
        var error = Assert.Throws<AgentPlaneException>(
            () => EngineCatalog.ResolveLaunch("codex", InstallRoot));

        Assert.Equal(AgentPlaneErrorCodes.AdapterEntryModuleNotRecorded, error.Code);
        Assert.Contains("codex", error.Message, StringComparison.Ordinal);
    }

    /// <summary>An unknown engine id is refused, never defaulted, and the reason lists what is known.</summary>
    [Theory]
    [InlineData("grok-build")]
    [InlineData("antigravity")]
    [InlineData("Claude-Code")]
    [InlineData("")]
    public void AnUnknownEngineIdIsRefusedNeverDefaulted(string engineId)
    {
        var resolve = Assert.Throws<AgentPlaneException>(
            () => EngineCatalog.ResolveLaunch(engineId, InstallRoot));
        Assert.Equal(AgentPlaneErrorCodes.UnknownEngine, resolve.Code);
        Assert.Contains("claude-code", resolve.Message, StringComparison.Ordinal);

        var find = Assert.Throws<AgentPlaneException>(() => EngineCatalog.Find(engineId));
        Assert.Equal(AgentPlaneErrorCodes.UnknownEngine, find.Code);
    }
}
