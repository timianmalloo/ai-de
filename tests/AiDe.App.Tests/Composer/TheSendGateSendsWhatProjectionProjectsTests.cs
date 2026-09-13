using System.Reflection;
using AiDe.App.Conductor;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// ADR-0033 rule 2 and US-D12, at the send gate: <c>GovernedRunRequest</c> is <b>byte-identical</b>
/// to the request the gate produced before the compile step existed (a literal golden, so the
/// oracle is independent of both implementations); <c>Projection.Project(</c> has exactly three
/// named call sites; every <c>LeaseDerivation</c> call passes the source-text symbol; the
/// <c>RunBudget</c> member reads are a named set; the three contract types' public signatures are
/// unchanged; a reopened session sends with the session's default class and <c>source:
/// session-default</c> (the successor of the retired refusal, Ruling 72).
/// </summary>
public sealed class TheSendGateSendsWhatProjectionProjectsTests
{
    private static ComposerSendContext Context(string taskClass = "implement") => new(
        RepositoryRoot: @"C:\repo",
        DataDirectory: @"C:\data",
        AdapterInstallRoot: @"C:\adapter",
        EngineId: "claude-code",
        Model: "sonnet",
        AccountLabel: "max-personal",
        TaskClass: taskClass,
        ProofPackArtifacts: ["docs/proof/pp-0001.md"],
        Providers: []);

    // ── byte-identical: three shapes, one literal golden each ──

    /// <summary>The goal block with one mention: the six sections in §14.3 order, the message, then the attachment fence.</summary>
    [Fact]
    public void AGoalBlockSendIsByteIdenticalToTheGolden()
    {
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);
        draft.SetFreeFormText("Rename the helper in @src/Payments/Money.cs.\n");
        draft.SetGoalValue(GoalBlockFields.GoalKey, "Rename the helper.");
        draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "It compiles under the new name.");
        draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "Nothing else changes.");
        draft.Add(new ComposerAttachment("notes.md", @"C:\repo\notes.md", 12, "see Money.cs", false, "deadbeef"));

        var request = new ComposerSendGate().Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.NotNull(request);

        // THE GOLDEN, FIELD BY FIELD: the literal values the gate produced before the compile step
        // existed — observed by running this test against the unchanged gate at `f3e394dc` (the
        // store commit; `ComposerSendGate.cs` there is byte-identical to `main` 5ce4b08e's — the
        // Proof Pack records the run). A record equality would compare the two lists by reference.
        Assert.Equal(@"C:\repo", request!.RepositoryRoot);
        Assert.Equal(@"C:\data", request.DataDirectory);
        Assert.Equal(@"C:\adapter", request.AdapterInstallRoot);
        Assert.Equal("claude-code", request.EngineId);
        Assert.Equal("sonnet", request.Model);
        Assert.Equal("max-personal", request.AccountLabel);
        Assert.Equal("implement", request.TaskClass);
        Assert.Equal(new GoalBlock("Rename the helper.", "It compiles under the new name.", "Nothing else changes.", "T1", 2, RunBudget.SubscriptionBounded), request.Goal);
        Assert.Equal(["src/Payments/Money.cs"], request.Lease!.Exclusive);
        Assert.Equal(
            "## goal\n\nRename the helper.\n\n"
            + "## done_when\n\nIt compiles under the new name.\n\n"
            + "## not_in_scope\n\nNothing else changes.\n\n"
            + "## tier\n\nT1\n\n"
            + "## fan_out_cap\n\n2\n\n"
            + "## budget\n\nbounded by your subscription — not measured here\n\n"
            + "## message\n\nRename the helper in @src/Payments/Money.cs.\n\n"
            + "\n```aide-attachment source=notes.md bytes=12 location=inside the workspace root\nsee Money.cs\n```\n",
            request.Prompt);
        Assert.Equal(["docs/proof/pp-0001.md"], request.ProofPackArtifacts);
        Assert.Empty(request.Providers);
        Assert.Equal("coord", request.CoordCommand);
        Assert.Null(request.PromptTimeout);
    }

    /// <summary>A Message: the message bytes alone, no block, no lease (Ruling 75; Ruling 73).</summary>
    [Fact]
    public void AMessageSendIsByteIdenticalToTheGolden()
    {
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);
        draft.SetFreeFormText("Explain the store in @src/AiDe.Core/Compilation/.\n");
        draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "the ADR");

        var request = new ComposerSendGate().Send(Context("free-form"), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.NotNull(request);
        AssertHostFields(request);
        Assert.Equal("free-form", request.TaskClass);
        Assert.Null(request.Goal);
        Assert.Null(request.Lease);
        Assert.True(request.IsReadOnly);
        Assert.Equal("Explain the store in @src/AiDe.Core/Compilation/.\n", request.Prompt);
    }

    /// <summary>The ten host-side fields every golden carries — literal, as the context supplied them (C16).</summary>
    private static void AssertHostFields(GovernedRunRequest request)
    {
        Assert.Equal(@"C:\repo", request.RepositoryRoot);
        Assert.Equal(@"C:\data", request.DataDirectory);
        Assert.Equal(@"C:\adapter", request.AdapterInstallRoot);
        Assert.Equal("claude-code", request.EngineId);
        Assert.Equal("sonnet", request.Model);
        Assert.Equal("max-personal", request.AccountLabel);
        Assert.Equal(["docs/proof/pp-0001.md"], request.ProofPackArtifacts);
        Assert.Empty(request.Providers);
        Assert.Equal("coord", request.CoordCommand);
        Assert.Null(request.PromptTimeout);
    }

    /// <summary>A template draft: the template's render, read-only.</summary>
    [Fact]
    public void ATemplateSendIsByteIdenticalToTheGolden()
    {
        var template = new PromptTemplate(
            Id: "t1", Version: 1, Intent: "an intent", Audience: "an audience", WhenToUse: "when", Why: "why",
            TierDefault: null,
            Fields: [new TemplateField("notes", TemplateFieldType.Text, Required: false, Hint: null, Min: null)],
            Body: "Read @docs/plan.md before you begin.\n\n{{notes}}\n",
            UnknownFrontmatter: new Dictionary<string, string>());
        var draft = new ComposerDraft();
        draft.UseTemplate("t1");
        draft.SetTemplateValue("notes", ["Also check @src/AiDe.App/Foo.cs"]);

        var request = new ComposerSendGate().Send(Context(), draft, template, out var refusal);

        Assert.Null(refusal);
        Assert.NotNull(request);
        AssertHostFields(request);
        Assert.Equal("implement", request.TaskClass);
        Assert.Null(request.Goal);
        Assert.Null(request.Lease);
        Assert.True(request.IsReadOnly);
        Assert.Equal("Read @docs/plan.md before you begin.\n\nAlso check @src/AiDe.App/Foo.cs\n", request.Prompt);
    }

    /// <summary>The rendered view and the sent prompt are the same bytes, and a stale view is refused, never sent with a lease from other bytes (US-D4).</summary>
    [Fact]
    public void AStaleRenderedViewIsRefusedRatherThanSentWithALeaseFromOtherBytes()
    {
        var gate = new ComposerSendGate();
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);
        draft.SetFreeFormText("touch @src/A/\n");
        draft.SetGoalValue(GoalBlockFields.GoalKey, "g");
        draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "d");
        draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "n");

        var view = gate.RenderView(draft);
        draft.SetFreeFormText("touch @src/B/\n");   // edited after the view was rendered, before Send

        var request = gate.Send(Context(), draft, null, out var refusal);

        Assert.Null(request);
        Assert.Equal("your draft changed since it was prepared — press again to prepare it", refusal!.Message);
        Assert.Equal(0, gate.SendCount);
        Assert.Contains("@src/A/", view.Text, StringComparison.Ordinal);

        // Re-rendered, the same draft sends, and what it sends is what it rendered.
        var fresh = gate.RenderView(draft);
        var sent = gate.Send(Context(), draft, null, out refusal);
        Assert.Null(refusal);
        Assert.Equal(fresh.Text, sent!.Prompt);
        Assert.Equal(["src/B/**"], sent.Lease!.Exclusive);
    }

    // ── Ruling 72: the class is never missing, so nothing refuses it ──

    [Fact]
    public void AReopenedSessionSendsWithTheSessionsDefaultClassAndSourceSessionDefault()
    {
        var gate = new ComposerSendGate();
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);
        draft.SetGoalValue(GoalBlockFields.GoalKey, "Rename the helper.");
        draft.SetFreeFormText("Rename the helper in @src/Payments/Money.cs.\n");
        draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "It compiles.");
        draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "Anything else.");

        var request = gate.Send(Context(AiDe.Core.Watcher.TaskClasses.FreeForm), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.Equal(AiDe.Core.Watcher.TaskClasses.FreeForm, request!.TaskClass);
        Assert.Equal(DecorationSources.SessionDefault, gate.LastSubmission!.TaskClassSource);

        // A per-prompt choice reaches THIS request and its source is operator.
        var second = new ComposerSendGate();
        draft.ChooseTaskClass("refactor");
        var chosen = second.Send(Context(AiDe.Core.Watcher.TaskClasses.FreeForm), draft, null, out _);
        Assert.Equal("refactor", chosen!.TaskClass);
        Assert.Equal(DecorationSources.Operator, second.LastSubmission!.TaskClassSource);

        // And the context's class is non-null by type: a null is a compile error, not a runtime refusal.
        Assert.Equal(typeof(string), typeof(ComposerSendContext).GetProperty(nameof(ComposerSendContext.TaskClass))!.PropertyType);
        Assert.False(new NullabilityInfoContext().Create(typeof(ComposerSendContext).GetProperty(nameof(ComposerSendContext.TaskClass))!).ReadState == NullabilityState.Nullable);
    }

    // ── the censuses (GO14a: root · recursion · token set · allowlist) ──

    /// <summary>
    /// <b>Root:</b> <c>src/</c>. <b>Recursion:</b> every <c>*.cs</c>, skipping <c>bin/</c> and
    /// <c>obj/</c>. <b>Token:</b> <c>Projection.Project(</c>. <b>Allowlist, exactly:</b>
    /// <c>Presentation/Composer/ComposerCompiler.cs</c>, <c>Workbench/Composer/ComposerSendGate.cs</c>,
    /// <c>Cli/CompileFold.cs</c> — the three named call sites by path (ADR-0033 rule 2; DM11 b).
    /// A fourth producer of the sent bytes reddens this by name.
    /// </summary>
    [Fact]
    public void ProjectionProjectHasExactlyThreeNamedCallSitesByPath()
    {
        var sites = Scan("Projection.Project(")
            .GroupBy(s => s.File, StringComparer.Ordinal)
            .Select(g => (File: g.Key, Calls: g.Count()))
            .OrderBy(s => s.File, StringComparer.Ordinal)
            .ToList();

        // THE THREE PATHS, AND HOW MANY CALLS EACH MAKES: the gate calls once for the view
        // (RenderView) and once for the send — the same function at both ends of C15's window, which
        // is what makes the view and the sent bytes one producer's output rather than two producers
        // checked for agreement (the Simplifier's finding).
        Assert.Equal(
            new[]
            {
                ("AiDe.App/Cli/CompileFold.cs", 1),
                ("AiDe.App/Workbench/Composer/ComposerSendGate.cs", 2),
                ("AiDe.Core/Presentation/Composer/ComposerCompiler.cs", 1),
            },
            sites);
    }

    /// <summary>
    /// <b>Root:</b> <c>src/</c>. <b>Recursion:</b> every <c>*.cs</c>, skipping <c>bin/</c> and
    /// <c>obj/</c>. <b>Tokens:</b> <c>LeaseDerivation.Derive(</c>, <c>LeaseDerivation.Patterns(</c>.
    /// <b>Allowlist, by (file, argument):</b> one <c>Derive(</c> in <c>Compilation/Projection.cs</c>
    /// over <c>opened.SourceText</c> (the one lease source in the product — ADR-0033 rule 2);
    /// <c>Patterns(</c> in <c>Projection.cs</c> (L), in <c>ComposerDraft.cs</c> (the block a render
    /// shows) and in <c>ComposerSurface.cs</c> (the display site) — every argument the source-text
    /// symbol. The plan's Seams row named <c>ComposerSendGate.cs</c> for the pre-D-1 world; after
    /// D-1 the gate calls <c>Project()</c> and derives nothing itself.
    /// </summary>
    [Fact]
    public void EveryLeaseDerivationCallSiteIsNamedAndPassesTheSourceTextSymbol()
    {
        var derive = Scan("LeaseDerivation.Derive(").Select(s => (s.File, s.Argument)).ToList();
        var patterns = Scan("LeaseDerivation.Patterns(").Select(s => (s.File, s.Argument)).OrderBy(s => s.File, StringComparer.Ordinal).ToList();

        Assert.Equal([("AiDe.Core/Compilation/Projection.cs", "opened.SourceText")], derive);
        Assert.Equal(
            new[]
            {
                ("AiDe.App/Workbench/Composer/ComposerSurface.cs", "_draft.SourceText"),
                ("AiDe.Core/Compilation/Projection.cs", "opened.SourceText"),
                ("AiDe.Core/Presentation/Composer/ComposerDraft.cs", "this.SourceText"),
            },
            patterns);
        Assert.All(derive.Concat(patterns), s => Assert.EndsWith(".SourceText", s.Argument, StringComparison.Ordinal));
    }

    /// <summary>
    /// §A13.3's census oracle, strengthened (Security C4; d′): <c>new Lease(</c> at exactly two sites
    /// by path — the derivation and the headless entry's request file — and zero hits for the three
    /// ways a <c>Lease</c> could be minted without the constructor's name. Same root, recursion and
    /// code-lines rule as the censuses above.
    /// </summary>
    [Fact]
    public void ALeaseIsMintedAtExactlyTwoNamedSitesAndByNoOtherPattern()
    {
        Assert.Equal(
            new[] { "AiDe.App/Conductor/ConductorEntry.cs", "AiDe.Core/Presentation/Composer/LeaseDerivation.cs" },
            Scan("new Lease(").Select(s => s.File).Order(StringComparer.Ordinal));

        foreach (var pattern in new[] { "with { Exclusive", "with { Lease", "Deserialize<Lease>" })
        {
            Assert.Empty(Scan(pattern).Select(s => s.File));
        }
    }

    /// <summary>
    /// The negative census the call-site censuses cannot see (the Test Architect's finding): a
    /// <c>using static</c> of the projection or the derivation, or a method-group assignment, would
    /// let a bare <c>Project(</c> / <c>Derive(</c> escape the token. Root <c>src/</c>, recursive,
    /// code lines only, tokens as listed, allowlist none.
    /// </summary>
    [Fact]
    public void NoFileImportsTheProjectionOrTheDerivationStaticallyOrTakesThemAsAMethodGroup()
    {
        foreach (var token in new[] { "using static AiDe.Core.PromptCompilation.Projection", "using static AiDe.Core.Presentation.Composer.LeaseDerivation", "= Projection.Project;", "= LeaseDerivation.Derive;", "= LeaseDerivation.Patterns;", "Projection.Project (" })
        {
            Assert.Empty(Scan(token).Select(s => s.File));
        }
    }

    /// <summary>
    /// The <c>RunBudget</c> named-member cap (ADR-0033 rule 3): every file in <c>src/</c> that reads
    /// <c>.Requests</c> / <c>.Tokens</c> on a budget (a file naming <c>RunBudget</c> or
    /// <c>BudgetCap</c>) is one of the named set, and every render site is guarded by
    /// <c>IsSubscriptionBounded</c> or reads the nullable cap. <b>Root:</b> <c>src/</c>;
    /// <b>recursion:</b> every <c>*.cs</c> bar <c>bin/</c>, <c>obj/</c>; <b>tokens:</b>
    /// <c>.Requests</c>, <c>.Tokens</c> in a file that names <c>RunBudget</c> or <c>BudgetCap</c>;
    /// <b>allowlist:</b> the seven files below with their guard. The ADR named four; S2 and CV-1
    /// added the sheet, the header and the snapshot — recorded, each guarded.
    /// </summary>
    [Fact]
    public void RunBudgetMemberReadsAreANamedSetWithNamedGuards()
    {
        var guards = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AiDe.App/Conductor/ConductorEntry.cs"] = "exempt: the field copy from a request file",
            ["AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs"] = "BudgetCap",       // the nullable cap: null is the absent state
            ["AiDe.Core/AgentPlane/GoalBlock.cs"] = "exempt: Validate's positivity check",
            ["AiDe.Core/Compilation/PreCompile.cs"] = "IsSubscriptionBounded",
            ["AiDe.Core/Compilation/Projection.cs"] = "IsSubscriptionBounded",
            ["AiDe.Core/Presentation/Composer/ComposerCompiler.cs"] = "IsSubscriptionBounded",
            ["AiDe.Core/Presentation/Sessions/NewSessionSheetViewModel.cs"] = "BudgetCap",
        };

        // THE PRECONDITION: a file reads a budget member when it names the type (`RunBudget`), the
        // setting (`BudgetCap`) or the block's field (`.Budget.` / `Budget!.` / `Budget?.` / `Budget is`)
        // — so a read through `var` off a goal block is seen. Files reading `.Requests`/`.Tokens` on
        // the cost and spend types name none of these and stay outside the cap by construction.
        var member = new System.Text.RegularExpressions.Regex(@"\.(Requests|Tokens)\b");
        var precondition = new System.Text.RegularExpressions.Regex(@"RunBudget|BudgetCap|\.Budget\b|Budget!\.|Budget\?\.|Budget is");
        var found = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        foreach (var file in SourceFiles())
        {
            var lines = File.ReadAllLines(file);
            if (!lines.Any(l => precondition.IsMatch(l)))
            {
                continue;
            }

            var hits = lines.Select((l, i) => (Line: l, Index: i))
                .Where(x => !x.Line.TrimStart().StartsWith("//", StringComparison.Ordinal) && member.IsMatch(x.Line))
                .Select(x => x.Index)
                .ToList();
            if (hits.Count > 0)
            {
                found[Relative(file)] = hits;
            }
        }

        Assert.Equal(guards.Keys.Order(StringComparer.Ordinal), found.Keys.Order(StringComparer.Ordinal));

        // THE GUARD AT EACH READ, not somewhere in the file: within three lines before or two after —
        // or the read is a COST or SPEND read (`RunEventCost` / `Spend` share the member names and
        // carry no sentinel), which is not a budget read at all.
        foreach (var (file, guard) in guards.Where(g => !g.Value.StartsWith("exempt", StringComparison.Ordinal)))
        {
            var lines = File.ReadAllLines(Path.Combine(RepoRoot(), "src", file));
            foreach (var hit in found[file])
            {
                var window = string.Join('\n', lines.Skip(Math.Max(0, hit - 3)).Take(6));
                var guarded = window.Contains(guard, StringComparison.Ordinal)
                    || lines[hit].Contains("Cost", StringComparison.Ordinal)
                    || lines[hit].Contains("Spend(", StringComparison.Ordinal);
                Assert.True(guarded, $"{file}:{hit + 1} reads a budget member with no `{guard}` within three lines before or two after");
            }
        }
    }

    /// <summary>
    /// US-D12: the public signatures of <c>SpawnContract</c>, <c>LeaseDerivation</c> and
    /// <c>TemplateCompiler</c> are the ones <c>main</c> carried before Addendum D, by reflection
    /// (a source hash would fail on a comment edit); <c>GovernedRunRequest</c> has fourteen parameters.
    /// </summary>
    [Fact]
    public void ThePublicSignaturesOfTheThreeContractTypesAreUnchangedAndTheRequestHasFourteenParameters()
    {
        static string TypeName(Type t) => t.IsGenericType
            ? t.Name[..t.Name.IndexOf('`')] + "<" + string.Join(", ", t.GetGenericArguments().Select(TypeName)) + ">"
            : t.Name;

        static IReadOnlyList<string> Signatures(Type type) =>
            type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Select(m => $"{TypeName(m.ReturnType)} {m.Name}({string.Join(", ", m.GetParameters().Select(p => TypeName(p.ParameterType) + " " + p.Name))})")
                .Order(StringComparer.Ordinal)
                .ToList();

        // AuthorizeBinding is ADR-0035 rule 1's factored identity half (CV-3) — an addition; the two
        // signatures US-D12 names, Validate and Authorize, are unchanged from main 5c132902.
        Assert.Equal(
            [
                "BoundIdentity AuthorizeBinding(String engineId, String model, String accountLabel, ObservedAuthStatus observed, ProviderRegistry registry)",
                "IReadOnlyList<GoalBlockError> Validate(GoalBlock block)",
                "Spawn Authorize(SpawnRequest request, ProviderRegistry registry)",
            ],
            Signatures(typeof(SpawnContract)));
        Assert.Equal(
            ["IReadOnlyList<String> Patterns(String compiledText)", "Lease Derive(String compiledText)"],
            Signatures(typeof(LeaseDerivation)));
        Assert.Equal(
            ["String Compile(PromptTemplate template, IReadOnlyDictionary<String, IReadOnlyList<String>> values)"],
            Signatures(typeof(TemplateCompiler)));

        // THE REQUEST'S FOURTEEN, BY NAME AND TYPE — a same-count type swap fails here.
        var ctor = Assert.Single(typeof(GovernedRunRequest).GetConstructors());
        Assert.Equal(
            new[]
            {
                "String RepositoryRoot", "String DataDirectory", "String AdapterInstallRoot", "String EngineId", "String Model", "String AccountLabel",
                "String TaskClass", "GoalBlock Goal", "Lease Lease", "String Prompt", "IReadOnlyList<String> ProofPackArtifacts",
                "IReadOnlyList<ProviderRow> Providers", "String CoordCommand", "Nullable<TimeSpan> PromptTimeout",
            },
            ctor.GetParameters().Select(p => TypeName(p.ParameterType) + " " + p.Name));
    }

    // ── the scan ──

    private static IEnumerable<string> SourceFiles() =>
        Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string Relative(string file) =>
        Path.GetRelativePath(Path.Combine(RepoRoot(), "src"), file).Replace('\\', '/');

    /// <summary>
    /// The named-call-site scan: code lines only — a line whose first non-blank characters are
    /// <c>//</c> is prose (an XML doc or a comment) and never a call site (register class CV-2 b:
    /// a census token spelled in a doc comment reddened the guard). The argument is the text up to
    /// the first <c>)</c> or <c>,</c>.
    /// </summary>
    private static List<(string File, string Argument)> Scan(string token)
    {
        var sites = new List<(string, string)>();
        foreach (var file in SourceFiles())
        {
            foreach (var line in File.ReadLines(file))
            {
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                for (var i = 0; (i = line.IndexOf(token, i, StringComparison.Ordinal)) >= 0; i += token.Length)
                {
                    var argStart = i + token.Length;
                    var argEnd = line.IndexOf(')', argStart);
                    Assert.True(argEnd > 0, $"{Relative(file)}: a {token} call whose argument does not end on its line");
                    var argument = line[argStart..argEnd].Trim();
                    if (argument.Contains(',', StringComparison.Ordinal))
                    {
                        argument = argument[..argument.IndexOf(',', StringComparison.Ordinal)].Trim();
                    }

                    sites.Add((Relative(file), argument));
                }
            }
        }

        return sites;
    }

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
}
