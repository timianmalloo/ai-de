namespace AiDe.Core.AgentPlane;

/// <summary>
/// How an engine speaks ACP, per spec §14.2's provider schema — the file this repository reads is
/// <c>~/.aide/providers.json</c> (erratum: <c>docs/notes/conductor-spec-errata-providers-json.md</c>),
/// and its <c>acp:</c> key is accepted and never read, because this enum is the catalog's fact.
/// A closed set on purpose — the spec declares exactly these four, and unlike a run-event
/// <c>kind</c> they do not evolve additively: a fifth would be a new launch path, which is a code
/// change by definition (Rulings 97 and 105 both cut a fifth).
/// </summary>
public enum AcpMode
{
    /// <summary>Spoken through an ACP adapter package: <c>node &lt;root&gt;/node_modules/&lt;package&gt;/&lt;entry&gt;</c>.</summary>
    Adapter,

    /// <summary>
    /// Speaks ACP itself, with no adapter: the CLI's own command resolved from PATH and run without
    /// a shell. A launch path since the engine-backends spike of 2026-09-14 observed
    /// <c>copilot --acp</c> answer <c>initialize</c> (Ruling 97 (iii)).
    /// </summary>
    Native,

    /// <summary>Not an ACP peer; watched through the terminal stack. Deferred to Phase 4.</summary>
    Observed,

    /// <summary>Capability unknown; enters as an observed lane whatever the spike finds.</summary>
    Deferred,
}

/// <summary>
/// A native CLI's launch, <b>as the spike observed it</b>: the command as the operator types it, the
/// arguments that put it in ACP mode, and the install instruction copied from upstream.
/// </summary>
/// <remarks>
/// <b>An npm-delivered CLI records its package and entry module</b> for the same reason
/// <see cref="EngineRow.AdapterEntryModule"/> does: on Windows npm leaves a <c>.cmd</c> shim, and a
/// <c>.cmd</c> cannot be spawned without <c>cmd.exe</c> — which DC-027 measured dropping this
/// machine's 22,297-character PATH on the way through. So the shim is never run; the catalog runs
/// <c>node &lt;shim dir&gt;/node_modules/&lt;package&gt;/&lt;entry&gt;</c>, the line the shim itself
/// carries (read from <c>%APPDATA%\npm\gemini.cmd</c>, 2026-09-14). A CLI whose npm launch was
/// never observed leaves both null, and its shim is a refusal.
/// </remarks>
/// <param name="Command">The command name on PATH — <c>copilot</c>, <c>gemini</c>, <c>grok</c>.</param>
/// <param name="Arguments">The arguments that start ACP over stdio, exactly as observed.</param>
/// <param name="InstallInstruction">The upstream install command the spike copied, for the refusal when the CLI is absent.</param>
/// <param name="NpmPackage">The npm package that delivers the CLI, when its <c>node &lt;entry&gt;</c> launch was observed; else null.</param>
/// <param name="NpmEntryModule">The package's bin script as observed in its <c>package.json</c>; null when <paramref name="NpmPackage"/> is.</param>
/// <param name="HostVariable">The environment variable an account's enterprise <c>host</c> becomes, per the CLI's own help; null when the CLI has none.</param>
public sealed record NativeCommand(
    string Command,
    IReadOnlyList<string> Arguments,
    string InstallInstruction,
    string? NpmPackage = null,
    string? NpmEntryModule = null,
    string? HostVariable = null);

/// <summary>
/// One catalog row. <b>Engines are data, not code paths</b> (spec §4.1): a row declares how to
/// speak to an engine, and adding or repairing one is a data change plus at most an adapter shim.
/// </summary>
/// <param name="Id">The engine id used everywhere else in the plane.</param>
/// <param name="Provider">The account provider the engine authenticates against.</param>
/// <param name="Acp">How it speaks ACP.</param>
/// <param name="AdapterPackage">The pinned npm package, or null for a non-adapter engine.</param>
/// <param name="AdapterVersion">The pinned version. Pinning is §13's stated mitigation for adapter drift.</param>
/// <param name="AdapterEntryModule">
/// The module inside the installed package, <b>as observed on a real install</b>, or null when it
/// has never been observed. Null is a refusal, not a default: guessing this path by analogy is how
/// a spawn fails at runtime with a file-not-found instead of here.
/// </param>
/// <param name="Native">The native CLI's launch, for an <see cref="AcpMode.Native"/> row; null otherwise.</param>
public sealed record EngineRow(
    string Id,
    string Provider,
    AcpMode Acp,
    string? AdapterPackage,
    string? AdapterVersion,
    string? AdapterEntryModule,
    NativeCommand? Native = null)
{
    /// <summary>
    /// Whether this engine reads the claude-code adapter's <c>_meta.claudeCode.options</c> slot on
    /// <c>session/new</c>. Only the adapter whose <c>acp-agent.js</c> spreads that object into the
    /// SDK's options does (0.75.1, <c>:5964</c>); every other peer was observed accepting the
    /// ACP-standard frame and nothing says it reads this one.
    /// </summary>
    public bool ReadsClaudeCodeMeta =>
        Acp == AcpMode.Adapter
        && string.Equals(AdapterPackage, PromptCompilation.CompilePin.AdapterPackage, StringComparison.Ordinal);
}

/// <summary>A resolved process launch: what to run, and with what arguments. Exactly what is spawned.</summary>
public sealed record EngineLaunch(string FileName, IReadOnlyList<string> Arguments);

/// <summary>
/// Finds a native CLI's command on PATH <b>without a shell</b>, and says which shape it found.
/// </summary>
/// <remarks>
/// <para><b>Measured on this machine, 2026-09-14</b> (<c>where copilot</c> / <c>where gemini</c> /
/// <c>where grok</c>): <c>copilot</c> resolves first to the winget <c>copilot.exe</c> and second to
/// an npm <c>copilot.cmd</c>; <c>gemini</c> resolves only to npm's <c>gemini.cmd</c> (there is no
/// <c>gemini.exe</c> anywhere); <c>grok</c> is not on PATH at all. <c>CreateProcess</c> appends only
/// <c>.exe</c> when it searches PATH, so a <c>.cmd</c>-only CLI cannot be started by name, and a
/// <c>.cmd</c> started through <c>cmd.exe</c> is the DC-027 shape. Hence three passes, in this
/// order, and each returned launch is a file that exists:</para>
/// <list type="number">
/// <item>the executable — <c>&lt;dir&gt;/&lt;command&gt;.exe</c> on Windows, an executable file elsewhere — on any PATH entry;</item>
/// <item>npm's shim on any PATH entry, resolved to the script beside it: <c>node &lt;dir&gt;/node_modules/&lt;package&gt;/&lt;entry&gt;</c>;</item>
/// <item>the same script under the product's own install root (<c>npm install --prefix &lt;root&gt;</c>, Ruling 104's mechanism).</item>
/// </list>
/// <para>The executable wins over the shim wherever each sits on PATH: a direct binary is the launch
/// the spike observed for copilot, and a shim two entries earlier is not a reason to start
/// <c>node</c> on a package whose launch was never seen.</para>
/// </remarks>
public sealed class NativeCommandLocator
{
    private readonly IReadOnlyList<string> _pathEntries;
    private readonly bool _windows;

    /// <param name="pathEntries">The directories to search, in order.</param>
    /// <param name="windows">Whether to look for <c>.exe</c> and <c>.cmd</c> (true) or executable files (false).</param>
    public NativeCommandLocator(IReadOnlyList<string> pathEntries, bool windows)
    {
        ArgumentNullException.ThrowIfNull(pathEntries);
        _pathEntries = pathEntries;
        _windows = windows;
    }

    /// <summary>The process's own PATH, split on the platform's separator, empty entries dropped.</summary>
    public static NativeCommandLocator FromEnvironment()
        => new(
            (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            OperatingSystem.IsWindows());

    /// <summary>Pass 1: the executable on PATH, or null.</summary>
    public string? FindExecutable(string command)
    {
        foreach (var dir in _pathEntries)
        {
            var candidate = Path.Combine(dir, _windows ? command + ".exe" : command);
            if (File.Exists(candidate) && (_windows || IsExecutable(candidate)))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>Pass 2: npm's shim for the command on PATH (<c>&lt;command&gt;.cmd</c> on Windows, the sh shim elsewhere), or null.</summary>
    public string? FindNpmShim(string command)
    {
        foreach (var dir in _pathEntries)
        {
            var candidate = Path.Combine(dir, _windows ? command + ".cmd" : command);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool IsExecutable(string file)
        => !OperatingSystem.IsWindows()
            && (File.GetUnixFileMode(file) & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0;
}

/// <summary>
/// The engine catalog — data rows, two launch paths.
/// </summary>
/// <remarks>
/// <para><b>The pins are catalog facts, not spec text.</b> The spec names no adapter package at all
/// (verified: zero hits for <c>zed-industries</c>, <c>@zed</c>, <c>claude-code-acp</c>), so package
/// identity is pinned in <c>docs/notes/conductor-spec-errata-policy.md</c> — which is exactly where
/// §4.1 says such things live. The superseded <c>@zed-industries/claude-code-acp</c> last published
/// at v0.16.2, so reaching for that name from memory silently yields a March build.</para>
///
/// <para><b>Every launch line is one the engine-backends spike observed</b>
/// (<c>docs/spikes/engine-backends-2026-09-14.md</c>, "Catalog consequences"; the frame corpus under
/// <c>spikes/engine-backends/&lt;engine&gt;/</c>). The catalog's former <c>simplify:</c> — "adapter
/// launch only; upgrade trigger = first native-ACP (copilot) spawn" — was retired when that spike
/// observed <c>copilot --acp</c> answer <c>initialize</c> with <c>protocolVersion 1</c>; the test
/// it named as its alarm was re-pointed at the two modes that still have no path.</para>
///
/// <para><b>Every refusal names its reason.</b> An unknown engine, a mode with no launch path, an
/// adapter with no observed entry module, and a native CLI that is not on PATH each fail loudly and
/// differently. A silent default here would launch the wrong engine, or the right one the wrong
/// way, and look like success.</para>
/// </remarks>
public static class EngineCatalog
{
    private static readonly EngineRow[] CatalogRows =
    [
        new(
            "claude-code",
            "anthropic",
            AcpMode.Adapter,
            "@agentclientprotocol/claude-agent-acp",
            "0.75.1",
            // Observed: spikes/acp-subscription-lane/probe-read.js spawns exactly this module, and
            // the committed frame corpus is what came back out of it. Observed again under
            // `npm install --ignore-scripts` (spike §6, spikes/engine-backends/claude-code/frames.jsonl).
            "dist/index.js"),
        new(
            "codex",
            "openai",
            AcpMode.Adapter,
            "@agentclientprotocol/codex-acp",
            "1.10.0",
            // Observed 2026-09-14 (spike §2): `npm install --prefix %TEMP%\aide-engine-spikes\codex
            // --ignore-scripts @agentclientprotocol/codex-acp@1.10.0` → package.json
            // `"bin": {"codex-acp": "dist/index.js"}`, `"main": "dist/index.js"`; on disk
            // node_modules/@agentclientprotocol/codex-acp/dist/index.js (1,272,135 bytes); spawned as
            // `node <that>` it answered initialize in 834 ms with agentInfo.name
            // "@agentclientprotocol/codex-acp" (spikes/engine-backends/codex/frames.jsonl). It bundles
            // @openai/codex@0.153.4 and needs no `codex` on PATH (dist/index.js:35040, :22098-22105).
            "dist/index.js"),
        new(
            "copilot",
            "github",
            AcpMode.Native,
            null,
            null,
            null,
            new NativeCommand(
                // Observed: `copilot --acp` — GitHub Copilot CLI 1.0.84-5, the winget copilot.exe —
                // answered initialize in 408 ms with agentInfo.name "Copilot"
                // (spikes/engine-backends/copilot/fresh-home/frames.jsonl). stdio is the default
                // transport. NEVER `--no-auto-login`: measured to suppress the stored credential
                // itself, so session/new is refused -32000 even when the operator is signed in
                // (copilot/existing-login/ vs copilot/existing-login-autologin/).
                "copilot",
                ["--acp"],
                // Copied from https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli
                // (read 2026-09-14). The npm route (`npm install -g @github/copilot`) exists upstream
                // and on this machine, and its `node npm-loader.js --acp` launch was NOT observed —
                // so NpmPackage stays null and an npm shim is a refusal, not a guess.
                "winget install GitHub.Copilot  (or: npm install -g @github/copilot; then: copilot login, or copilot login --host https://<tenant>.ghe.com)",
                // COPILOT_GH_HOST: "GitHub hostname used only by Copilot CLI for authentication and
                // API requests, overriding GH_HOST when set" — `copilot help environment`,
                // spikes/engine-backends/copilot/copilot-help-environment.txt:51. Chosen over GH_HOST
                // because it reaches Copilot alone and leaves any `gh` in the child's tree untouched.
                // Whether the ACP server reads it at session time was not observable without a tenant.
                HostVariable: "COPILOT_GH_HOST")),
    ];

    /// <summary>The catalog, as data.</summary>
    public static IReadOnlyList<EngineRow> Rows => CatalogRows;

    /// <summary>Finds a row by id.</summary>
    /// <exception cref="AgentPlaneException">
    /// <see cref="AgentPlaneErrorCodes.UnknownEngine"/> — an id the catalog does not carry is
    /// refused, never defaulted to the one engine that happens to work today.
    /// </exception>
    public static EngineRow Find(string engineId)
        => CatalogRows.FirstOrDefault(r => string.Equals(r.Id, engineId, StringComparison.Ordinal))
            ?? throw new AgentPlaneException(
                AgentPlaneErrorCodes.UnknownEngine,
                $"unknown engine id '{engineId}'; the catalog carries: "
                + string.Join(", ", CatalogRows.Select(r => r.Id)));

    /// <summary>
    /// Resolves how to launch an engine: an adapter package whose entry module has been observed on
    /// a real install, or a native CLI found on this process's PATH.
    /// </summary>
    /// <param name="engineId">The catalog id.</param>
    /// <param name="adapterInstallRoot">The directory whose <c>node_modules</c> holds the adapter — or, for an npm-delivered native CLI that PATH lacks, the CLI.</param>
    /// <exception cref="AgentPlaneException">
    /// <see cref="AgentPlaneErrorCodes.UnknownEngine"/> for an id the catalog does not carry;
    /// <see cref="AgentPlaneErrorCodes.LaunchPathNotImplemented"/> for <see cref="AcpMode.Observed"/>
    /// and <see cref="AcpMode.Deferred"/>, and for a native row with no command;
    /// <see cref="AgentPlaneErrorCodes.AdapterEntryModuleNotRecorded"/> for an adapter whose entry
    /// module has never been observed;
    /// <see cref="AgentPlaneErrorCodes.EngineNotOnPath"/> for a native CLI PATH does not carry.
    /// </exception>
    public static EngineLaunch ResolveLaunch(string engineId, string adapterInstallRoot)
        => ResolveLaunch(engineId, adapterInstallRoot, NativeCommandLocator.FromEnvironment());

    /// <summary>
    /// <see cref="ResolveLaunch(string, string)"/> with the PATH lookup injected, so the native
    /// path's rules are asserted on a PATH the test built rather than the one the machine has.
    /// </summary>
    public static EngineLaunch ResolveLaunch(string engineId, string adapterInstallRoot, NativeCommandLocator locator)
        => ResolveLaunch(Find(engineId), adapterInstallRoot, locator);

    /// <summary>Resolves a row that need not be in the catalog — the refusals are tested on synthetic rows.</summary>
    internal static EngineLaunch ResolveLaunch(EngineRow row, string adapterInstallRoot, NativeCommandLocator locator)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(locator);

        return row.Acp switch
        {
            AcpMode.Adapter => ResolveAdapter(row, adapterInstallRoot),
            AcpMode.Native => ResolveNative(row, adapterInstallRoot, locator),
            _ => throw new AgentPlaneException(
                AgentPlaneErrorCodes.LaunchPathNotImplemented,
                $"engine '{row.Id}' declares ACP mode '{row.Acp}'; the catalog launches 'adapter' and 'native' "
                + "rows only, and an observed / deferred engine has no launch path until its spike records one"),
        };
    }

    private static EngineLaunch ResolveAdapter(EngineRow row, string adapterInstallRoot)
    {
        if (row.AdapterPackage is null || row.AdapterEntryModule is null)
        {
            throw new AgentPlaneException(
                AgentPlaneErrorCodes.AdapterEntryModuleNotRecorded,
                $"engine '{row.Id}' declares ACP mode 'adapter' but no adapter entry module has been "
                + "observed on a real install; capture one before spawning rather than guessing a path");
        }

        return new EngineLaunch("node", [NodeModule(adapterInstallRoot, row.AdapterPackage, row.AdapterEntryModule)]);
    }

    private static EngineLaunch ResolveNative(EngineRow row, string adapterInstallRoot, NativeCommandLocator locator)
    {
        if (row.Native is not { } native || string.IsNullOrWhiteSpace(native.Command))
        {
            throw new AgentPlaneException(
                AgentPlaneErrorCodes.LaunchPathNotImplemented,
                $"engine '{row.Id}' declares ACP mode 'native' and names no command; a native row without "
                + "an observed command line has no launch path");
        }

        // Pass 1 — the executable itself. CreateProcess would find this one by name; resolving it
        // here makes the launch line the host reports the file that is actually spawned.
        if (locator.FindExecutable(native.Command) is { } executable)
        {
            return new EngineLaunch(executable, native.Arguments);
        }

        // Pass 2 — npm's shim, never run: the script it would have handed to node is run instead.
        var shim = locator.FindNpmShim(native.Command);
        if (shim is not null)
        {
            if (native.NpmPackage is null || native.NpmEntryModule is null)
            {
                throw new AgentPlaneException(
                    AgentPlaneErrorCodes.EngineNotOnPath,
                    $"engine '{row.Id}': PATH carries '{shim}', an npm shim that needs a shell to run, and the "
                    + $"catalog records no observed npm launch for '{native.Command}'; install the observed "
                    + $"form instead: {native.InstallInstruction}");
            }

            var script = NodeModule(Path.GetDirectoryName(shim)!, native.NpmPackage, native.NpmEntryModule);
            if (!File.Exists(script))
            {
                throw new AgentPlaneException(
                    AgentPlaneErrorCodes.EngineNotOnPath,
                    $"engine '{row.Id}': PATH carries the npm shim '{shim}' but the script it runs is not beside it "
                    + $"('{script}' does not exist); reinstall: {native.InstallInstruction}");
            }

            return new EngineLaunch("node", [script, .. native.Arguments]);
        }

        // Pass 3 — the product's own install root, for an npm-delivered CLI PATH does not carry.
        if (native.NpmPackage is not null && native.NpmEntryModule is not null)
        {
            var script = NodeModule(adapterInstallRoot, native.NpmPackage, native.NpmEntryModule);
            if (File.Exists(script))
            {
                return new EngineLaunch("node", [script, .. native.Arguments]);
            }
        }

        throw new AgentPlaneException(
            AgentPlaneErrorCodes.EngineNotOnPath,
            $"engine '{row.Id}': '{native.Command}' is not on PATH"
            + (native.NpmPackage is null ? string.Empty : $" and '{native.NpmPackage}' is not installed under '{adapterInstallRoot}'")
            + $"; install it: {native.InstallInstruction}");
    }

    private static string NodeModule(string root, string package, string entryModule)
        => Path.Combine(
        [
            root,
            "node_modules",
            package,
            .. entryModule.Split('/', StringSplitOptions.RemoveEmptyEntries),
        ]);

    /// <summary>
    /// The environment an engine's child needs from the account it is bound to — today exactly one
    /// fact: an enterprise <c>host</c> becomes the CLI's own host variable (Ruling 97 condition 3;
    /// Ruling 105 (1): the host is on the account, never the engine row).
    /// </summary>
    /// <param name="row">The engine being launched.</param>
    /// <param name="account">The account the lane bills against.</param>
    /// <returns>Variables to set on the child; empty when the account carries no host.</returns>
    /// <exception cref="AgentPlaneException">
    /// <see cref="AgentPlaneErrorCodes.LaunchPathNotImplemented"/> when the account carries a host
    /// and the engine has no variable to honour it with — refused, because a launch that dropped the
    /// host would sign in to the wrong tenant and look like success.
    /// </exception>
    public static IReadOnlyDictionary<string, string> LaunchEnvironment(EngineRow row, ProviderAccount account)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(account);

        if (account.Host is not { } host)
        {
            return new Dictionary<string, string>();
        }

        if (row.Native?.HostVariable is not { } variable)
        {
            throw new AgentPlaneException(
                AgentPlaneErrorCodes.LaunchPathNotImplemented,
                $"account '{account.Label}' carries host '{host}' and engine '{row.Id}' has no enterprise-host "
                + "variable to pass it in; the launch is refused rather than made against the wrong tenant");
        }

        return new Dictionary<string, string> { [variable] = host };
    }
}
