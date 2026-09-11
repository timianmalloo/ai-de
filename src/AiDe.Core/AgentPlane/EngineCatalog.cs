namespace AiDe.Core.AgentPlane;

/// <summary>
/// How an engine speaks ACP, per spec §14.2's provider schema — the file this repository reads is
/// <c>~/.aide/providers.json</c> (erratum: <c>docs/notes/conductor-spec-errata-providers-json.md</c>),
/// and its <c>acp:</c> key is accepted and never read, because this enum is the catalog's fact.
/// A closed set on purpose — the spec declares exactly these four, and unlike a run-event
/// <c>kind</c> they do not evolve additively: a fifth would be a new launch path, which is a code
/// change by definition.
/// </summary>
public enum AcpMode
{
    /// <summary>Spoken through an ACP adapter package. The only Phase-1 launch path.</summary>
    Adapter,

    /// <summary>Speaks ACP itself, with no adapter. Deferred to Phase 3+.</summary>
    Native,

    /// <summary>Not an ACP peer; watched through the terminal stack. Deferred to Phase 4.</summary>
    Observed,

    /// <summary>Capability unknown; enters as an observed lane whatever the spike finds.</summary>
    Deferred,
}

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
public sealed record EngineRow(
    string Id,
    string Provider,
    AcpMode Acp,
    string? AdapterPackage,
    string? AdapterVersion,
    string? AdapterEntryModule);

/// <summary>A resolved process launch: what to run, and with what arguments.</summary>
public sealed record EngineLaunch(string FileName, IReadOnlyList<string> Arguments);

/// <summary>
/// The engine catalog — three data rows, one launch path.
/// </summary>
/// <remarks>
/// <para><b>The pins are catalog facts, not spec text.</b> The spec names no adapter package at all
/// (verified: zero hits for <c>zed-industries</c>, <c>@zed</c>, <c>claude-code-acp</c>), so package
/// identity is pinned in <c>docs/notes/conductor-spec-errata-policy.md</c> — which is exactly where
/// §4.1 says such things live. The superseded <c>@zed-industries/claude-code-acp</c> last published
/// at v0.16.2, so reaching for that name from memory silently yields a March build.</para>
///
/// <para><b>Every refusal names its reason.</b> An unknown engine, a non-adapter mode, and an
/// adapter with no observed entry module each fail loudly and differently. A silent default here
/// would launch the wrong engine, or the right one the wrong way, and look like success.</para>
/// </remarks>
public static class EngineCatalog
{
    // simplify: adapter launch only; upgrade trigger = first native-ACP (copilot) spawn
    // Ceiling: the catalog carries three rows but resolves exactly one launch path, so the
    // codex/copilot deferral is enforced by a refusal rather than remembered by a reader. When the
    // trigger fires, EngineCatalogTests.ANonAdapterModeIsRefusedWithANamedReason goes red on
    // purpose — that is the alarm, not a broken test.
    private static readonly EngineRow[] CatalogRows =
    [
        new(
            "claude-code",
            "anthropic",
            AcpMode.Adapter,
            "@agentclientprotocol/claude-agent-acp",
            "0.75.1",
            // Observed: spikes/acp-subscription-lane/probe-read.js spawns exactly this module, and
            // the committed frame corpus is what came back out of it.
            "dist/index.js"),
        new(
            "codex",
            "openai",
            AcpMode.Adapter,
            "@agentclientprotocol/codex-acp",
            "1.10.0",
            // Never installed, never observed. Null rather than a copy of the Claude adapter's path.
            null),
        new(
            "copilot",
            "github",
            AcpMode.Native,
            // Native ACP: `copilot --acp`, preview since 2026-01-28. Recorded in the errata note
            // rather than modelled here, because Phase 1 refuses this path before it needs it.
            null,
            null,
            null),
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
    /// Resolves how to launch an engine. Exactly one path is implemented: an adapter package whose
    /// entry module has been observed on a real install.
    /// </summary>
    /// <param name="engineId">The catalog id.</param>
    /// <param name="adapterInstallRoot">The directory whose <c>node_modules</c> holds the adapter.</param>
    /// <exception cref="AgentPlaneException">
    /// <see cref="AgentPlaneErrorCodes.UnknownEngine"/> for an id the catalog does not carry;
    /// <see cref="AgentPlaneErrorCodes.LaunchPathNotImplemented"/> for any mode but
    /// <see cref="AcpMode.Adapter"/>;
    /// <see cref="AgentPlaneErrorCodes.AdapterEntryModuleNotRecorded"/> for an adapter whose entry
    /// module has never been observed.
    /// </exception>
    public static EngineLaunch ResolveLaunch(string engineId, string adapterInstallRoot)
    {
        var row = Find(engineId);

        if (row.Acp != AcpMode.Adapter)
        {
            throw new AgentPlaneException(
                AgentPlaneErrorCodes.LaunchPathNotImplemented,
                $"engine '{row.Id}' declares ACP mode '{row.Acp}'; Phase 1 implements the 'adapter' "
                + "launch path only, and the native / observed / deferred paths are deferred to Phase 3+");
        }

        if (row.AdapterPackage is null || row.AdapterEntryModule is null)
        {
            throw new AgentPlaneException(
                AgentPlaneErrorCodes.AdapterEntryModuleNotRecorded,
                $"engine '{row.Id}' declares ACP mode 'adapter' but no adapter entry module has been "
                + "observed on a real install; capture one before spawning rather than guessing a path");
        }

        string[] parts =
        [
            adapterInstallRoot,
            "node_modules",
            row.AdapterPackage,
            .. row.AdapterEntryModule.Split('/', StringSplitOptions.RemoveEmptyEntries),
        ];

        return new EngineLaunch("node", [Path.Combine(parts)]);
    }
}
