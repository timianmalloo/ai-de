using System.Text;

namespace AiDe.Core.PromptCompilation;

/// <summary>
/// The versioned pair the agentic rung calls (ADR-0033 rule 1): <c>compile-prompt/1</c> in,
/// <c>compile-output/1</c> out — the contract's constants, its allow-list, its deny-list and the
/// two host-embedded texts. <b>Shipped inert in this slice</b>: no model call exists here (the
/// agentic rung is CV-3's, behind PD-5 and the eval gate); the validator and the assembler are
/// tested red-first over authored fixtures so the rung has a typed boundary to call.
/// </summary>
/// <remarks>
/// <para><b>The header and the template are host-compiled bytes</b> — never read from the workspace
/// or <c>.claude/</c> (§A8.3; P-D3's falsifier: a workspace file named like the template changes no
/// byte). <c>simplify:</c> they are string constants rather than <c>EmbeddedResource</c> files
/// because adding a resource glob is a <c>*.csproj</c> change the plan reserves to the conductor
/// (a seam request is filed in the Proof Pack); the upgrade trigger is that glob landing, at which
/// point the two texts move to <c>Compilation/Resources/</c> and <see cref="CompilePromptAssembler.PromptSha"/>
/// is unchanged as long as the bytes are.</para>
/// </remarks>
public static class CompileContract
{
    /// <summary>The prompt contract's version — <c>called.contract_version</c>; names the pair.</summary>
    public const string Version = "compile-prompt/1";

    /// <summary>The output contract the model must answer with.</summary>
    public const string OutputContract = "compile-output/1";

    /// <summary>A proposal's value bound (§A8.3): ≤ 2000 chars, no control characters.</summary>
    public const int MaxValueChars = 2000;

    /// <summary>The <c>notes</c> bound: ≤ 500 chars, shown as provenance, never applied.</summary>
    public const int MaxNotesChars = 500;

    /// <summary>The allow-list (v1): the three structure lines, and only the ones the prompt named as open. Case-sensitive.</summary>
    public static readonly IReadOnlyList<string> AllowList = DecorationNames.StructureLines;

    /// <summary>
    /// The deny-list — dropped and counted, never applied, never persisted (§A8.3). Named so a
    /// reader can see what the boundary refuses; <b>any unknown name</b> is refused the same way,
    /// so this list is documentation of intent, not the mechanism.
    /// </summary>
    public static readonly IReadOnlyList<string> DenyList =
        ["lease", "task_class", "fan_out_cap", "fan_out_ceiling", "fan_out_effective", "budget", "engine", "model", "account", "shape", "tier", "template", "history_window", "constitution", "compile_mode", "source"];

    /// <summary>
    /// The fixed host header — <b>the prompt's first bytes, always</b>: a prompt whose first bytes
    /// are <c>/…</c> is executed by the CLI as a local command (adapter 0.75.1 <c>acp-agent.js:6035</c>),
    /// so the compile prompt never begins with operator text.
    /// </summary>
    public const string HostHeader =
        "AI-DE compile step (compile-prompt/1). You are the compile stage of a prompt composer.\n"
        + "Read the fenced source text below and propose values for the OPEN structure lines only,\n"
        + "answering with exactly one compile-output/1 JSON object and nothing else. You may not\n"
        + "contradict the mechanical facts, propose a lease, a tier, a task class, a budget or any\n"
        + "name outside the open lines, and you have no tools: do not read, write, or run anything.\n";

    /// <summary>The <c>compile-prompt/1</c> template — the blocks in order, each named; <c>{{…}}</c> slots the assembler fills.</summary>
    public const string Template =
        "## source_text\n\n{{source_text}}\n\n"
        + "## mechanical_facts\n\n{{mechanical_facts}}\n\n"
        + "## open_lines\n\n{{open_lines}}\n\n"
        + "## family_profile\n\n{{family_profile}}\n\n"
        + "## history_window\n\n{{history_window}}\n\n"
        + "## constitution\n\n{{constitution}}\n\n"
        + "## output_contract\n\n"
        + "Answer with one JSON object: {\"contract\": \"compile-output/1\", \"decorations\": [{\"name\": \"<an open line>\", "
        + "\"value\": \"<a string, at most 2000 characters, no control characters>\", \"confidence\": <0.0-1.0>, "
        + "\"grounded_in\": [{\"input\": \"source_text\", \"span\": [start, end]}]}], \"notes\": \"<at most 500 characters>\"}\n";

    /// <summary>The header's and the template's bytes, as hashed and as sent.</summary>
    public static ReadOnlySpan<byte> TemplateBytes => Encoding.UTF8.GetBytes(HostHeader + Template);
}
