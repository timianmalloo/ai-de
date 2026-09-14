using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The pure half of the reader's HTML sandbox (Ruling 93): what counts as local, so a navigation or
/// a request that would leave the process is refused — and a destination that cannot be read is
/// refused too (fail closed). The live half — the settings read back from a real runtime, a page
/// with a script that does not run — is <see cref="HtmlSandboxIntegrationTests"/>.
/// </summary>
public sealed class HtmlSandboxPolicyTests
{
    [Theory]
    [InlineData("about:blank")]
    [InlineData("about:srcdoc")]
    [InlineData("data:text/html,%3Ch1%3Ehi%3C%2Fh1%3E")]
    [InlineData("DATA:image/png;base64,iVBORw0KGgo=")]
    public void ALocalDestinationIsNeitherCancelledNorBlocked(string uri)
    {
        Assert.True(HtmlSandboxPolicy.IsLocal(uri));
        Assert.False(HtmlSandboxPolicy.MustCancelNavigation(uri));
        Assert.False(HtmlSandboxPolicy.MustBlockRequest(uri));
    }

    [Theory]
    [InlineData("https://aide-sandbox-probe.invalid/leak.png")]
    [InlineData("http://localhost:8080/")]
    [InlineData("file:///C:/Windows/win.ini")]
    [InlineData("ftp://example.invalid/")]
    [InlineData("wss://example.invalid/socket")]
    [InlineData("blob:null/2f6c0b8e-0000-4000-8000-000000000000")]
    [InlineData("javascript:alert(1)")]
    [InlineData("mailto:someone@example.invalid")]
    [InlineData("")]
    [InlineData(null)]
    public void AnythingElseIsCancelledAndBlocked(string? uri)
    {
        Assert.False(HtmlSandboxPolicy.IsLocal(uri));
        Assert.True(HtmlSandboxPolicy.MustCancelNavigation(uri));
        Assert.True(HtmlSandboxPolicy.MustBlockRequest(uri));
    }
}
