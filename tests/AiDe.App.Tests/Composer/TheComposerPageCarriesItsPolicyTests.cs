using AiDe.App.Workbench.Composer;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// Security <b>C12</b>'s static half (the shipped page's policy and its external module),
/// <b>C10</b>'s navigation policy as a pure decision, and the paste riders' clipboard rule.
/// </summary>
/// <remarks>
/// The other half of C12 — that the three control settings are really off and that the editor still
/// RENDERS under this policy — cannot be asserted here: it needs a real WebView2, which a
/// <c>dotnet test</c> host does not reliably provide (<b>DC-014</b>). It is asserted out of process
/// by <c>ComposerHostIntegrationTests</c>, which drives the real control.
/// </remarks>
public sealed class TheComposerPageCarriesItsPolicyTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }

    private static string WebFile(string name) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "AiDe.App", "Web", name));

    /// <summary>
    /// The same file with its comments removed, so an assertion is about what RUNS.
    /// </summary>
    /// <remarks>
    /// Both files document the rules they obey, and those paragraphs name the very tokens these
    /// assertions refuse — "no send, no run.start". A check that read the prose would fail on the
    /// explanation and pass on the violation, which is the wrong way round.
    /// </remarks>
    private static string Executable(string name)
    {
        var text = WebFile(name);
        text = System.Text.RegularExpressions.Regex.Replace(text, "<!--.*?-->", string.Empty,
            System.Text.RegularExpressions.RegexOptions.Singleline);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"/\*.*?\*/", string.Empty,
            System.Text.RegularExpressions.RegexOptions.Singleline);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?m)^\s*//.*$", string.Empty);
        return text;
    }

    [Fact]
    public void C12_TheShippedPageCarriesTheExactPolicyByteForByte()
    {
        var page = WebFile(ComposerPageContract.PageFile);

        Assert.Contains(
            $"<meta http-equiv=\"Content-Security-Policy\" content=\"{ComposerPageContract.ContentSecurityPolicy}\">",
            page,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("default-src 'none'")]
    [InlineData("script-src 'self'")]
    [InlineData("connect-src 'none'")]
    [InlineData("frame-src 'none'")]
    [InlineData("object-src 'none'")]
    [InlineData("base-uri 'none'")]
    [InlineData("form-action 'none'")]
    public void C12_EveryDirectiveIsPresentIndividually(string directive)
    {
        // Named one at a time so a rewrite that drops one fails on THAT line rather than on a diff of
        // the whole string.
        Assert.Contains(directive, ComposerPageContract.ContentSecurityPolicy, StringComparison.Ordinal);
    }

    [Fact]
    public void C12_TheModuleIsExternalAndThePageCarriesNoInlineScript()
    {
        var page = WebFile(ComposerPageContract.PageFile);

        Assert.Contains(
            $"<script type=\"module\" src=\"./{ComposerPageContract.ModuleFile}\"></script>",
            page,
            StringComparison.Ordinal);

        // An inline module would have forced `'unsafe-inline'` back into script-src, which would make
        // the strictest directive in the policy decorative.
        var scripts = Executable(ComposerPageContract.PageFile).Split("<script", StringSplitOptions.None).Skip(1);
        Assert.All(scripts, s => Assert.Contains("src=\"./", s[..Math.Min(s.Length, 80)], StringComparison.Ordinal));
        Assert.DoesNotContain("'unsafe-inline'", ComposerPageContract.ContentSecurityPolicy.Split(';')[1], StringComparison.Ordinal);
    }

    [Fact]
    public void ThePageAndItsModuleContainNoSendVerbAndNoHostObjectUse()
    {
        foreach (var name in new[] { ComposerPageContract.PageFile, ComposerPageContract.ModuleFile })
        {
            var text = Executable(name);

            Assert.DoesNotContain("\"send\"", text, StringComparison.Ordinal);
            Assert.DoesNotContain("run.start", text, StringComparison.Ordinal);
            Assert.DoesNotContain("send.requested", text, StringComparison.Ordinal);
            Assert.DoesNotContain("hostObjects", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AddHostObjectToScript", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ThePageNeverReportsTheAttachSettingItIsOnlyToldIt()
    {
        var module = WebFile(ComposerPageContract.ModuleFile);

        // Refusal (i). `attachEnabled` may be READ from a host push and rendered; it may never appear
        // inside a posted body.
        foreach (var posted in module.Split("post(").Skip(1))
        {
            var call = posted[..Math.Min(posted.Length, 160)];
            Assert.DoesNotContain("attachEnabled", call, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("https://example.invalid/")]
    [InlineData("file:///C:/x.html")]
    [InlineData("about:blank")]
    [InlineData("https://aide.assets.invalid.evil.test/")]
    [InlineData("https://aide.assets.invalid/other.html")]
    [InlineData("https://aide.assets.invalid/composer.html?x=1")]
    [InlineData(null)]
    public void C10_EveryNavigationThatIsNotTheComposerDocumentIsCancelled(string? uri)
    {
        Assert.True(ComposerPageContract.MustCancelNavigation(uri));
        Assert.False(ComposerPageContract.IsTheComposerDocument(uri));
    }

    [Fact]
    public void C10_TheComposersOwnDocumentIsTheOneThingNotCancelled()
    {
        Assert.False(ComposerPageContract.MustCancelNavigation(ComposerPageContract.Url));
        Assert.EndsWith("/composer.html", ComposerPageContract.Url, StringComparison.Ordinal);
        Assert.StartsWith("https://aide.assets.invalid/", ComposerPageContract.Url, StringComparison.Ordinal);
    }

    [Fact]
    public void TheComposerNeverReadsTheClipboardOutsideTheExplicitPasteGesture()
    {
        // Paste rider (2): no polling, no history, no paste-on-focus. The host-side half is that the
        // surface contains no clipboard call at all — read from the source rather than asserted.
        var surface = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "AiDe.App", "Workbench", "Composer", "ComposerSurface.cs"));

        Assert.DoesNotContain("Clipboard.", surface, StringComparison.Ordinal);
        Assert.DoesNotContain("GetClipboard", surface, StringComparison.Ordinal);

        var module = WebFile(ComposerPageContract.ModuleFile);
        Assert.DoesNotContain("navigator.clipboard", module, StringComparison.Ordinal);
        Assert.DoesNotContain("readText", module, StringComparison.Ordinal);

        // The one clipboard read in the page is inside the paste event's own data, which is the
        // gesture itself rather than an access to the clipboard.
        Assert.Contains("clipboardData", module, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePasteFenceCarriesNoProvenanceClaim()
    {
        var module = WebFile(ComposerPageContract.ModuleFile);

        // The app cannot verify where clipboard content came from, and a fabricated provenance is
        // worse than none — so the header says `pasted` and never a filename or a URL.
        Assert.Contains("\"text pasted\\n\"", module, StringComparison.Ordinal);
    }
}
