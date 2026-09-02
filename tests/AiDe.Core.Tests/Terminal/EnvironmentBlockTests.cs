using System.Runtime.Versioning;
using AiDe.Core.Terminal;

namespace AiDe.Core.Tests.Terminal;

/// <summary>
/// The environment a terminal hands its child — the contract in
/// <c>docs/design/ux-agent-session-registration.md</c> §3.
/// </summary>
/// <remarks>
/// <para><b>These assert that there is NO total-size limit, which is the opposite of what an earlier
/// version asserted.</b> That version refused past 32,647 characters, from a bisection that had
/// measured <c>PATH</c> length and been written into the code as a block size — two different
/// quantities, one named with the other's units.</para>
///
/// <para><b>Re-measured through the hop that matters.</b> A <b>60,000-character non-PATH variable
/// passes a PowerShell-hosted launch intact</b>; a <b>33,000-character PATH breaks it</b>. So the
/// limit is on <c>PATH</c> — PowerShell resolves the command it was given through <c>PATH</c>, and
/// an oversized one stops it finding anything — and not on the block at all.</para>
///
/// <para><b>Why the guard was removed rather than corrected.</b> It would have refused launches that
/// work. A check that fires on correct behaviour gets switched off, and takes the real check with
/// it. Oversized <c>PATH</c> is already reported by <c>EnvironmentHealth</c> at a threshold far
/// below the one that breaks PowerShell, so the hazard that does exist is already covered.</para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class EnvironmentBlockTests
{
    [Fact]
    public void NoExtras_MeansInherit_SoTheCommonPathIsUnchanged()
    {
        Assert.Null(ConPtyInterop.BuildEnvironmentBlock(null));
        Assert.Null(ConPtyInterop.BuildEnvironmentBlock(new Dictionary<string, string>()));
    }

    [Fact]
    public void TheBlockCarriesTheExtras_AndIsDoubleNullTerminated()
    {
        var block = ConPtyInterop.BuildEnvironmentBlock(
            new Dictionary<string, string> { ["AIDE_SESSION"] = "surface-1" });

        Assert.NotNull(block);
        var text = new string(block!);

        Assert.Contains("AIDE_SESSION=surface-1\0", text, StringComparison.Ordinal);
        Assert.EndsWith("\0\0", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TheExtrasOverrideTheInheritedValue_RatherThanAppearingTwice()
    {
        const string name = "AIDE_BLOCK_TEST_OVERRIDE";
        Environment.SetEnvironmentVariable(name, "inherited");
        try
        {
            var block = ConPtyInterop.BuildEnvironmentBlock(
                new Dictionary<string, string> { [name] = "supplied" });

            var text = new string(block!);
            Assert.Contains($"{name}=supplied\0", text, StringComparison.Ordinal);
            Assert.DoesNotContain($"{name}=inherited\0", text, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    /// <summary>
    /// A large addition still builds, because the limit is not on the block.
    /// </summary>
    /// <remarks>
    /// The test this replaces asserted a refusal at 32,647. Keeping the inverted assertion is the
    /// point: it fails if a size guard is ever reintroduced without a measurement behind it.
    /// </remarks>
    [Fact]
    public void ALargeAdditionStillBuilds_BecauseTheLimitIsNotOnTheBlock()
    {
        var block = ConPtyInterop.BuildEnvironmentBlock(
            new Dictionary<string, string> { ["AIDE_BLOCK_TEST_BIG"] = new('x', 40_000) });

        Assert.NotNull(block);
        Assert.True(block!.Length > 40_000, "the large value was not carried");
    }

    /// <summary>
    /// The contract's own variables are negligible, which is why they are safe to add.
    /// </summary>
    /// <remarks>
    /// §3's rule "keep every value short" survives the correction with a different justification: it
    /// is no longer defence against a block limit, it is what keeps the addition irrelevant to any
    /// limit anyone later discovers. A serialised payload here would deserve a fresh measurement.
    /// </remarks>
    [Fact]
    public void TheContractsOwnVariablesAreSmall_WhichIsWhyTheyAreSafe()
    {
        var contract = new Dictionary<string, string>
        {
            ["AIDE_SESSION"] = "agent:claude#a1b2c3",
            ["AIDE_TERMINAL_ID"] = "agent:claude#a1b2c3",
            ["AIDE_HARNESS"] = "claude-code",
            ["AIDE_AGENT"] = "claude",
            ["AIDE_CONTRACT_VERSION"] = "loomkeeper/1",
        };

        var added = contract.Sum(kv => kv.Key.Length + kv.Value.Length + 2);
        Assert.True(added < 512, $"the contract adds {added} chars; it is meant to be negligible");
    }
}
