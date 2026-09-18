using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Tests.AgentPlane;

namespace AiDe.Core.Tests;

/// <summary>
/// A native engine whose command is on PATH is never reported as "not configured".
/// </summary>
/// <remarks>
/// <para><b>INV-0013's red, and it could not be written until now.</b> The operator's clean-machine
/// build listed all five accounts as <i>"no account — Configure… · not configured (no adapter root —
/// no provider file)"</i> while <c>copilot</c> was installed and on PATH. Their words are the
/// requirement: <i>"copilot is installed and in path — I should not need to set up anything here — I
/// should just have to log in."</i></para>
///
/// <para>The cause is that the sheet answered <i>"can this engine launch on this machine?"</i> from
/// the product's own configuration state, short-circuiting <c>EngineCatalog.InstallRefusal</c> — the
/// single installed-reading DC-223 created — whenever <c>~/.aide/providers.json</c> is absent. On a
/// clean machine that is always, so the engine's command is never probed for any engine.</para>
///
/// <para><b>Why this file is the phase-0 receipt.</b> The investigation recorded that this test
/// "cannot even be written today — no locator seam — which is itself the finding": the PATH lookup
/// stopped at <c>EngineCatalog</c>, and every caller above it used the no-locator overload, so no
/// test above the catalog could control what PATH says. Phase 0 threaded the locator to
/// <see cref="NewSessionSheetViewModel.RowsOf"/>. This test is what that seam is for.</para>
/// </remarks>
public sealed class ANativeEngineOnPathIsNeverNotConfiguredTests
{
    [Fact]
    public void WithCopilotOnPathAndNoProviderFile_TheRowIsNotNotConfigured()
    {
        using var path = new EngineCatalogTests.FakePath();
        path.AddExecutable("copilot");

        // No provider file: exactly the clean machine the operator ran.
        var rows = NewSessionSheetViewModel.RowsOf(
            new ProviderRegistry([]), adapterInstallRoot: null, path.Locator);

        var copilot = rows.Single(r => string.Equals(r.EngineId, "copilot", StringComparison.Ordinal));

        Assert.NotEqual(AccountRowState.NotConfigured, copilot.State);
        Assert.DoesNotContain("no adapter root", copilot.StateReason, StringComparison.OrdinalIgnoreCase);
    }
}
