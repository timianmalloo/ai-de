using System.Text.RegularExpressions;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// R21's control, now that the assist path is real (Ruling 180; T30 n6): every model call reachable from the
/// composer goes through the session's bound compile engine, under compile-mode gate 1, at <b>one sanctioned
/// site</b> — <c>src/AiDe.App/Workbench/Composer/ComposerSendGate.cs:148</c>, the <c>Compiler</c> default
/// (<c>CompileCallHost.CompileAsync</c>), invoked once at <c>:660</c> inside <c>PrepareAsync</c>.
/// </summary>
/// <remarks>
/// <para><b>Why App, not Core.</b> The host set is read by reflection over <c>AiDe.App</c> (every
/// <c>[CapabilityTier]</c> carrier), and the site's text is built from <c>nameof</c>, so a renamed host breaks the
/// build rather than leaving the scan's view; and the second fact drives the real surface. Core cannot see either.</para>
///
/// <para><b>The source set</b> is Core's <c>ThePrivacyResidualsAreCoveredTests.ComposerSources()</c>: the Core and App
/// composer folders, <c>composer.mjs</c> and <c>composer.html</c>. The site's caller chain
/// (<c>ComposerSurface.Send</c> → <c>PrepareTurnAsync</c> → <c>ComposerSendGate.PrepareAsync</c> → <c>Compiler</c>) adds no
/// file outside it. The send itself builds a <c>GovernedRunRequest</c> and raises <c>Sent</c>; the run executes
/// outside the composer, so it is not a composer model call.</para>
///
/// <para><b>Gate 1</b> reaches the site through the bound rung only: <c>ComposerSurface.Send</c> prepares only when
/// <c>IsAgenticRung</c>, and a rung is stored only through <c>SessionConfigStore.SetCompileMode</c>, which refuses a
/// mode <c>CompileModeGate</c> refuses (Core's <c>TheCompileModeLadderIsGatedTests</c>).</para>
///
/// <para><b>What this does not see:</b> a call made through an alias of the delegate (<c>var f = Compiler; f(…)</c>),
/// or through a type outside the reflected set and the named primitives. The line numbers are pinned on purpose: an
/// edit that moves the site turns this red and makes the editor re-read the control.</para>
/// </remarks>
public sealed class OnlyTheSanctionedSiteCallsAModelFromTheComposerTests
{
    private const string SanctionedFile = "src/AiDe.App/Workbench/Composer/ComposerSendGate.cs";
    private const int SanctionedLine = 148;
    private const int InvocationLine = 660;

    /// <summary>Engine and client primitives no composer line may name, beside the reflected <c>[CapabilityTier]</c> carriers.</summary>
    private static readonly string[] EnginePrimitives =
        ["AcpEngineProcess", "AcpLaneClient", "ICompileEngine", "GovernedRunHost", "AttemptHost", "HttpClient", @"Process\.Start"];

    /// <summary>A page-side network call would bypass the host entirely.</summary>
    private static readonly string[] PageNetwork = [@"fetch\s*\(", "XMLHttpRequest", "WebSocket", "EventSource", "sendBeacon"];

    [Fact]
    public void EveryModelCallInTheComposerIsTheOneSanctionedSite()
    {
        var hosts = typeof(CompileCallHost).Assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(CapabilityTierAttribute), inherit: false).Length > 0)
            .Select(t => Regex.Escape(t.Name))
            .ToList();
        Assert.Contains(nameof(CompileCallHost), hosts);

        var hostPattern = new Regex(@"\b(" + string.Join("|", hosts.Concat(EnginePrimitives).Concat(PageNetwork)) + @")", RegexOptions.CultureInvariant);
        var invocation = new Regex(@"\bCompiler\s*(\(|\.Invoke\s*\()", RegexOptions.CultureInvariant);

        var hostSites = new List<string>();
        var invocations = new List<string>();
        foreach (var (file, number, line) in CodeLines())
        {
            if (hostPattern.IsMatch(line))
            {
                hostSites.Add($"{file}:{number}");
            }

            if (invocation.IsMatch(line))
            {
                invocations.Add($"{file}:{number}");
            }
        }

        Assert.Equal([$"{SanctionedFile}:{SanctionedLine}"], hostSites);
        Assert.Equal([$"{SanctionedFile}:{InvocationLine}"], invocations);

        var lines = File.ReadAllLines(Path.Combine(RepoRoot(), SanctionedFile));
        Assert.Contains($"{nameof(CompileCallHost)}.{nameof(CompileCallHost.CompileAsync)}(", lines[SanctionedLine - 1], StringComparison.Ordinal);
        Assert.Contains($"{nameof(ComposerSendGate.Compiler)} {{ get; set; }}", lines[SanctionedLine - 1], StringComparison.Ordinal);
        Assert.Contains("await Compiler(request", lines[InvocationLine - 1], StringComparison.Ordinal);
    }

    [Fact]
    public void TheSanctionedSiteIsReachedOnlyUnderAnAgenticRung()
    {
        Sta.Run(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));

            var mechanical = Build(CompileModes.MechanicalOnly, out var mechanicalCalls);
            mechanical.SetFieldText(mechanical.Fields[0].Id, 1, "Rename the helper in Money.cs.");
            Assert.NotNull(mechanical.Send());
            Assert.Equal(0, mechanicalCalls());
            Assert.Equal(0, mechanical.Gate.CompilerCalls);

            // The counter is wired: the same gesture under an admitted agentic rung reaches the site once.
            var advisory = Build(CompileModes.AgenticAdvisory, out var advisoryCalls);
            advisory.SetFieldText(advisory.Fields[0].Id, 1, "Rename the helper in Money.cs.");
            Assert.Null(advisory.Send());
            Assert.Equal(1, advisoryCalls());
        });
    }

    private static ComposerSurface Build(string mode, out Func<int> calls)
    {
        var count = 0;
        calls = () => count;
        var surface = new ComposerSurface("composer:s-r21", "s-r21 — composer");
        surface.Configure(
            new SessionConfig("s-r21", "first", "w-1", DateTimeOffset.UnixEpoch, [new AccountRef("anthropic", "max")], new AccountRef("anthropic", "max")) { CompileMode = mode },
            new ComposerSendContext(
                RepositoryRoot: @"C:\repo", DataDirectory: @"C:\data", AdapterInstallRoot: @"C:\adapter",
                EngineId: "claude-code", Model: "claude-sonnet-5", AccountLabel: "max", TaskClass: "implement",
                ProofPackArtifacts: [], Providers: []),
            ComposerFields.FreeForm(),
            new AttachmentGate(@"C:\repo", new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max"));
        surface.Gate.Compiler = (_, _) =>
        {
            Interlocked.Increment(ref count);
            throw new OperationCanceledException();
        };
        return surface;
    }

    private sealed class NeverAsked : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
    }

    /// <summary>Every non-comment line of the composer sources, as (repo-relative file, 1-based line, text).</summary>
    private static IEnumerable<(string File, int Number, string Line)> CodeLines()
    {
        var root = RepoRoot();
        var sources = Directory.EnumerateFiles(Path.Combine(root, "src", "AiDe.Core", "Presentation", "Composer"), "*.cs")
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "src", "AiDe.App", "Workbench", "Composer"), "*.cs"))
            .Append(Path.Combine(root, "src", "AiDe.App", "Web", "composer.mjs"))
            .Append(Path.Combine(root, "src", "AiDe.App", "Web", "composer.html"));

        foreach (var path in sources)
        {
            var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            var lines = File.ReadAllLines(path);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith('*')
                    || trimmed.StartsWith("/*", StringComparison.Ordinal) || trimmed.StartsWith("<!--", StringComparison.Ordinal))
                {
                    continue;
                }

                yield return (relative, i + 1, lines[i]);
            }
        }
    }

    private static string RepoRoot()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AiDe.sln")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        return root.FullName;
    }
}
