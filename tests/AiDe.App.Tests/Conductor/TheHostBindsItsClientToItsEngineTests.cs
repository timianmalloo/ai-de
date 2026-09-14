using System.Text.RegularExpressions;
using AiDe.App.Conductor;
using AiDe.Core.AgentPlane;
using AiDe.Core.PromptCompilation;

namespace AiDe.App.Tests.Conductor;

/// <summary>
/// Seam request <c>req-01M2GKC2RY667S51X22QVV7FGJ</c> (the engines lane, 2026-09-14): the run host and
/// the compile host constructed their <see cref="AcpLaneClient"/> without its <see cref="EngineRow"/>
/// and started the engine before any account was looked at, so a governed run on copilot would still
/// have sent the claude pin and a work-tenant account's host would never have reached the child.
/// </summary>
/// <remarks>
/// <para><b>Red-first.</b> Neither <c>EnvironmentFor</c> nor <c>ClientFor</c> existed: the first
/// observed red is CS0117 on both, and the source oracle below then read two client sites with no
/// <c>engine:</c> and two starts with no <c>environment:</c>.</para>
///
/// <para><b>Why a source-text oracle beside the unit rows.</b> <c>GovernedRunHost.RunAsync</c> starts
/// a real engine before it opens the session, so the frame it sends cannot be captured here without
/// the engine (the reason <c>TheGovernedLaneHasNoShellTests</c> gives for the same idiom); what is
/// left to prove is that <i>every</i> site in the App that constructs a client passes its row and
/// every site that starts an engine passes its environment — a control proven on one site protects
/// that site, not the boundary (DC-019).</para>
/// </remarks>
public sealed class TheHostBindsItsClientToItsEngineTests
{
    private static GovernedRunRequest Request(string engineId, IReadOnlyList<ProviderRow> providers, string account = "max") => new(
        RepositoryRoot: Path.GetTempPath(),
        DataDirectory: Path.GetTempPath(),
        AdapterInstallRoot: Path.GetTempPath(),
        EngineId: engineId,
        Model: "m",
        AccountLabel: account,
        TaskClass: "free-form",
        Goal: null,
        Lease: null,
        Prompt: "hello",
        ProofPackArtifacts: [],
        Providers: providers);

    private static ProviderRow Github(string? host) =>
        new("github", ProviderAuth.Subscription, [new ProviderAccount("work", AccountHealth.Ready, Host: host)]);

    /// <summary>A copilot run on an account that carries an enterprise host sets the engine's host variable on the child.</summary>
    [Fact]
    public void ACopilotRunOnAWorkTenantAccountPassesTheHostToTheChild()
    {
        var environment = GovernedRunHost.EnvironmentFor(
            Request("copilot", [Github("https://tenant.ghe.com")], account: "work"));

        Assert.Equal("https://tenant.ghe.com", Assert.Single(environment).Value);
        Assert.Equal(EngineCatalog.Find("copilot").Native!.HostVariable, Assert.Single(environment).Key);
    }

    /// <summary>An account with no host sets nothing — the claude-code lane's child environment is untouched.</summary>
    [Fact]
    public void AnAccountWithNoHostSetsNothing()
    {
        var anthropic = new ProviderRow("anthropic", ProviderAuth.Subscription, [new ProviderAccount("max", AccountHealth.Ready)]);

        Assert.Empty(GovernedRunHost.EnvironmentFor(Request("claude-code", [anthropic])));
    }

    /// <summary>
    /// An account the registry does not carry yields no environment here and is refused later by the
    /// spawn contract, as before — the lookup never invents a host and never pre-empts the refusal.
    /// </summary>
    [Fact]
    public void AnUnknownAccountYieldsNoEnvironmentAndLeavesTheRefusalToTheContract()
    {
        Assert.Empty(GovernedRunHost.EnvironmentFor(Request("copilot", [Github("https://tenant.ghe.com")], account: "nobody")));
    }

    /// <summary>A host on an account whose engine has no host variable is refused before any process starts.</summary>
    [Fact]
    public void AHostTheEngineCannotHonourIsRefusedBeforeTheSpawn()
    {
        var google = new ProviderRow("google", ProviderAuth.ApiKey, [new ProviderAccount("team", AccountHealth.Ready, Host: "https://x")]);

        var error = Assert.Throws<AgentPlaneException>(() => GovernedRunHost.EnvironmentFor(Request("gemini", [google], account: "team")));
        Assert.Equal(AgentPlaneErrorCodes.LaunchPathNotImplemented, error.Code);
    }

    /// <summary>The compile child keeps its output cap and gains the account's host — the same lookup as the run's.</summary>
    [Fact]
    public void TheCompileChildCarriesTheCapAndTheAccountsHost()
    {
        var request = new CompileRequest(
            Path.GetTempPath(), Path.GetTempPath(), "copilot", "m", "work",
            [Github("https://tenant.ghe.com")], CompileContract.HostHeader + "\n## source_text\n\n```text\nhello\n```\n", 1000, Path.GetTempPath());

        var environment = CompileCallHost.ChildEnvironmentFor(request);

        Assert.Equal("https://tenant.ghe.com", environment[EngineCatalog.Find("copilot").Native!.HostVariable!]);
        Assert.Equal(CompileCallHost.CompileChildEnvironment[AcpEngineProcess.MaxOutputTokensVariable], environment[AcpEngineProcess.MaxOutputTokensVariable]);
    }

    /// <summary>
    /// Every client the App constructs is told its engine, and every engine the two hosts start gets
    /// its environment — read from source, across the boundary, not from one site.
    /// </summary>
    [Fact]
    public void EveryClientSiteInTheAppPassesItsRowAndEveryHostStartPassesItsEnvironment()
    {
        var root = RepositoryRoot();
        var sources = Directory.EnumerateFiles(Path.Combine(root, "src", "AiDe.App"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();

        var clientSites = sources
            .SelectMany(p => CallArguments(File.ReadAllText(p), "new AcpLaneClient(")
                .Select(args => (File: Path.GetRelativePath(root, p), Args: args)))
            .ToList();
        Assert.NotEmpty(clientSites);
        Assert.All(clientSites, site => Assert.True(
            site.Args.Contains("engine:", StringComparison.Ordinal),
            $"{site.File} constructs an AcpLaneClient without its engine row: {site.Args.Trim()[..Math.Min(80, site.Args.Trim().Length)]}"));

        foreach (var host in new[] { "Conductor/GovernedRunHost.cs", "Conductor/CompileCallHost.cs" })
        {
            var starts = CallArguments(File.ReadAllText(Path.Combine(root, "src", "AiDe.App", host)), "AcpEngineProcess.Start(").ToList();
            Assert.NotEmpty(starts);
            Assert.All(starts, args => Assert.True(
                args.Contains("environment:", StringComparison.Ordinal),
                $"{host} starts an engine without its environment: {args.Trim()}"));
        }
    }

    /// <summary>
    /// The argument text of every call that starts with <paramref name="opener"/>, read to the paren
    /// that balances it — a lambda body's <c>;</c> inside the call is part of the call (the first
    /// version of this oracle stopped at that <c>;</c> and read the compile host's site as bare).
    /// </summary>
    private static IEnumerable<string> CallArguments(string text, string opener)
    {
        var at = 0;
        while ((at = text.IndexOf(opener, at, StringComparison.Ordinal)) >= 0)
        {
            var start = at + opener.Length;
            var depth = 1;
            var i = start;
            for (; i < text.Length && depth > 0; i++)
            {
                depth += text[i] switch { '(' => 1, ')' => -1, _ => 0 };
            }

            yield return text[start..(i - 1)];
            at = i;
        }
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AiDe.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("AiDe.sln not found above the test host");
    }
}
