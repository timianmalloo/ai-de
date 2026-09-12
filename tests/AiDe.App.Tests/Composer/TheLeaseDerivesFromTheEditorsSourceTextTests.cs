using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// Ruling 66 (Addendum C). Lease derivation runs over the editor's own held source text — never an
/// attachment body, a template body, or the rendered goal-block heading/structure — at both call
/// sites (<c>ComposerSendGate.cs</c>'s <see cref="ComposerSendGate.Send"/> and the display caller,
/// <c>ComposerSurface.RenderCompiledView</c>).
/// </summary>
/// <remarks>
/// <para><b>Why this exists.</b> F-2: <see cref="LeaseDerivation"/> was fed <c>compiled.Text</c> — the
/// whole rendered prompt, attachment bodies and template prose included — so a mention the operator
/// never typed (it arrived inside a file they attached, or inside a catalog template's fixed prose)
/// silently widened the lane's write scope.</para>
///
/// <para><b>Red observed on <c>main</c> before this fix</b> (recorded in the Proof Pack): the
/// attachment and template-body cases below derived a pattern from content the operator never typed.
/// The goal-block heading case and the goal-field control were already green — headings carry no
/// <c>@</c>, and a goal field's own text was always part of the compiled text — so they are controls,
/// not regressions, and prove the fix does not over-narrow.</para>
///
/// <para><b>Re-homed on the write shape after Ruling 73</b> (CV-0): only a goal block with a derived
/// scope derives a lease at all — a free-form or template draft is a Message and runs read-only
/// with no lease (<c>TheReadOnlyTurnNeedsNoLeaseTests</c>). The source-of-derivation control is
/// therefore proven where a lease exists: the editor's mention sits in a goal-block field, the
/// mention nobody typed sits in an attachment body beside it.</para>
/// </remarks>
public sealed class TheLeaseDerivesFromTheEditorsSourceTextTests
{
    [Fact]
    public void AnAttachmentBodyMentionDerivesNoPatternButTheEditorTextStillDoes()
    {
        var draft = CompleteGoalBlockWithNoMentions();
        draft.SetGoalValue(GoalBlockFields.GoalKey, "Fix the bug in @src/AiDe.App/Foo.cs");
        draft.Add(new ComposerAttachment(
            DisplayPath: "notes.md",
            ResolvedPath: "C:/repo/notes.md",
            Bytes: 40,
            Text: "See @src/AiDe.Core/Bar.cs for context.",
            IsOutsideWorkspace: false,
            Sha256: "deadbeef"));

        var request = new ComposerSendGate().Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.NotNull(request);

        // What the operator typed is still there...
        Assert.Contains("src/AiDe.App/Foo.cs", request!.Lease!.Exclusive);

        // ...but the attachment's own body names a file the operator never referenced, and it must
        // never widen the lane's write scope, however it read before this fix.
        Assert.DoesNotContain("src/AiDe.Core/Bar.cs", request.Lease.Exclusive);
        Assert.Equal(["src/AiDe.App/Foo.cs"], request.Lease.Exclusive);
    }

    /// <summary>
    /// A template draft is a Message until the compile step reads its structure (CV-2): it runs
    /// read-only, so a mention in its fixed prose can widen nothing — there is no lease to widen.
    /// </summary>
    [Fact]
    public void ATemplateBodyMentionCannotWidenALeaseBecauseATemplateSendsReadOnly()
    {
        var template = new PromptTemplate(
            Id: "t1",
            Version: 1,
            Intent: "an intent",
            Audience: "an audience",
            WhenToUse: "when to use it",
            Why: "why it exists",
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
        Assert.True(request!.IsReadOnly);
        Assert.Null(request.Lease);
        Assert.Contains("docs/plan.md", request.Prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void TheRenderedGoalBlockHeadingsNeverYieldAPatternOnTheirOwn()
    {
        var draft = CompleteGoalBlockWithNoMentions();

        // Nothing names a path anywhere in the six fields, so — despite the compiled text carrying
        // six literal "## <name>" headings — nothing is derivable, and the turn runs read-only
        // (Ruling 73) rather than deriving a pattern from the rendered structure itself.
        var request = new ComposerSendGate().Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.True(request!.IsReadOnly);
        Assert.Null(request.Lease);
    }

    [Fact]
    public void TheSameMentionTypedInTheGoalFieldStillDerivesItsPattern()
    {
        var draft = CompleteGoalBlockWithNoMentions();
        draft.SetGoalValue(GoalBlockFields.GoalKey, "Rename the helper in @src/Payments/Money.cs.");

        var request = new ComposerSendGate().Send(Context(), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.Equal(["src/Payments/Money.cs"], request!.Lease!.Exclusive);
    }

    private static ComposerDraft CompleteGoalBlockWithNoMentions()
    {
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);
        draft.SetGoalValue(GoalBlockFields.GoalKey, "Rename the helper.");
        draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "It compiles under the new name.");
        draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "Nothing else changes.");
        draft.SetGoalValue(GoalBlockFields.TierKey, "T1");
        draft.SetGoalValue(GoalBlockFields.FanOutCapKey, "0");
        draft.SetGoalValue(GoalBlockFields.BudgetKey, "10,1000");
        return draft;
    }

    /// <summary>
    /// Condition (2): the surface's displayed lease (<c>ComposerSurface.cs:449</c>, the display
    /// caller) and the sent lease (<c>ComposerSendGate.cs:164</c>) come from the same symbol, so they
    /// can never disagree — for a draft that names an attachment mention the fix must exclude.
    /// </summary>
    [Fact]
    public void TheDisplayedLeaseAndTheSentLeaseAgreeForADraftWithAnAttachmentMention()
    {
        Sta.Run(() =>
        {
            var surface = new ComposerSurface("composer:s-0066", "s-0066 — composer");

            surface.Configure(
                new SessionConfig("s-0066", "first", "w-1", DateTimeOffset.UnixEpoch, ["claude-code"]),
                Context(),
                ComposerFields.GoalBlock(),
                new AttachmentGate(
                    @"C:\repo", new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max-personal"));

            // A write-shaped turn (Ruling 73): a complete goal block, its mention in the goal field.
            surface.Draft.SwitchTo(ComposerShape.GoalBlock);
            surface.Draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "It compiles.");
            surface.Draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "Nothing else.");
            surface.Draft.SetGoalValue(GoalBlockFields.TierKey, "T1");
            surface.Draft.SetGoalValue(GoalBlockFields.FanOutCapKey, "0");
            surface.Draft.SetGoalValue(GoalBlockFields.BudgetKey, "10,1000");

            // An attachment, added directly (no file system involved) so its mention is on the draft
            // exactly as an affirmed attach would leave it.
            surface.Draft.Add(new ComposerAttachment(
                DisplayPath: "notes.md",
                ResolvedPath: @"C:\repo\notes.md",
                Bytes: 40,
                Text: "See @src/AiDe.Core/Bar.cs for context.",
                IsOutsideWorkspace: false,
                Sha256: "deadbeef"));

            // A keystroke into the goal field — the display path's normal trigger.
            surface.SetFieldText(surface.Fields[0].Id, 1, "Fix the bug in @src/AiDe.App/Foo.cs");

            // ONLY THE LEASE LINE, not the whole rendered tree: the compiled-view TextBox is expected
            // to show the attachment body byte-for-byte (that is what will be sent) — it is the LEASE
            // specifically that must never widen from it.
            var leaseLineBeforeSend = LeaseLine(surface);
            Assert.Equal("Lease: src/AiDe.App/Foo.cs", leaseLineBeforeSend);

            var request = surface.Send();

            Assert.NotNull(request);
            Assert.Equal(["src/AiDe.App/Foo.cs"], request!.Lease!.Exclusive);

            // THE SAME PATTERNS THE DISPLAY SHOWED, not merely a non-conflicting set: the whole point
            // of the shared symbol is that the two can never quietly drift apart.
            Assert.Equal("Lease: " + string.Join(", ", request.Lease.Exclusive), leaseLineBeforeSend);
        });
    }

    /// <summary>
    /// The source-scan guard the register entry names: every <see cref="LeaseDerivation"/> call site
    /// in <c>src/</c> passes the editor's source-text symbol, never the compiled prompt.
    /// </summary>
    /// <remarks>
    /// <para><b>Root:</b> <c>src/</c>, every <c>*.cs</c> file. <b>Recursion:</b> all subdirectories,
    /// skipping generated <c>bin/</c> and <c>obj/</c> trees (the same skip every sibling source-scan
    /// guard in this suite uses). <b>Token set:</b> the two public entry points,
    /// <c>LeaseDerivation.Derive(</c> and <c>LeaseDerivation.Patterns(</c> — <see cref="LeaseDerivation"/>
    /// declares no other public member that takes a string (see
    /// <c>NoOperatorTypedLeaseEditorExistsAnywhereInTheComposer</c>), so these two calls are the
    /// derivation's only inputs anywhere in the product. <b>Allowlist:</b> none — a third call site
    /// would need this test updated by name, exactly as <c>C16_ExactlyTwoSitesInTheProductConstructAGovernedRunRequest</c>
    /// above is updated by name for its own edge.</para>
    ///
    /// <para><b>The oracle.</b> The argument text between the matching parentheses, trimmed, must end
    /// in <c>.SourceText</c> — <see cref="ComposerDraft.SourceText"/> is the one place in the product
    /// that holds the operator's own typed content and nothing else (never an attachment body, never
    /// a template's fixed prose, never the rendered goal-block heading text).</para>
    /// </remarks>
    [Fact]
    public void EveryLeaseDerivationCallSiteInSrcPassesTheSourceTextSymbol()
    {
        var sites = new List<(string File, string Argument)>();

        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var token in new[] { "LeaseDerivation.Derive(", "LeaseDerivation.Patterns(" })
            {
                for (var i = 0; (i = text.IndexOf(token, i, StringComparison.Ordinal)) >= 0; i += token.Length)
                {
                    var argStart = i + token.Length;
                    var argEnd = text.IndexOf(')', argStart);
                    Assert.True(argEnd > 0, $"{Path.GetFileName(file)}: unterminated {token} call");
                    sites.Add((Path.GetFileName(file), text[argStart..argEnd].Trim()));
                }
            }
        }

        // NAMED, not counted (as C16's guard above): today's three call sites, by file and argument
        // — the send gate reads the source text twice, once for the shape (`Patterns`, Ruling 73) and
        // once for the write-shaped lease (`Derive`), and the display site once.
        Assert.Equal(
            new[]
            {
                ("ComposerSendGate.cs", "draft.SourceText"),
                ("ComposerSendGate.cs", "draft.SourceText"),
                ("ComposerSurface.cs", "_draft.SourceText"),
            }
                .OrderBy(s => s.Item1, StringComparer.Ordinal),
            sites.OrderBy(s => s.File, StringComparer.Ordinal).Select(s => (s.File, s.Argument)));

        Assert.All(sites, s => Assert.EndsWith(".SourceText", s.Argument, StringComparison.Ordinal));
    }

    private sealed class NeverAsked : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
    }

    /// <summary>
    /// The line beginning "Lease: ", out of everything the surface rendered. The harvest below walks
    /// both the visual tree and, separately, a <c>ContentControl</c>'s logical content — which visits
    /// the same subtree twice once the template has applied — so every distinct line is deduplicated
    /// rather than asserted unique.
    /// </summary>
    private static string LeaseLine(FrameworkElement root) =>
        RenderedText(root)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .Single(line => line.StartsWith("Lease: ", StringComparison.Ordinal));

    private static string RenderedText(FrameworkElement root)
    {
        root.Measure(new Size(1200, 900));
        root.Arrange(new Rect(0, 0, 1200, 900));
        root.UpdateLayout();

        var text = new StringBuilder();
        Harvest(root, text);
        return text.ToString();
    }

    private static void Harvest(DependencyObject node, StringBuilder text)
    {
        switch (node)
        {
            case TextBlock block:
                text.Append(block.Text).Append('\n');
                break;
            case TextBox box:
                text.Append(box.Text).Append('\n');
                break;
            case ContentControl { Content: string content }:
                text.Append(content).Append('\n');
                break;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            Harvest(VisualTreeHelper.GetChild(node, i), text);
        }

        if (node is ContentControl { Content: DependencyObject child })
        {
            Harvest(child, text);
        }
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

    private static ComposerSendContext Context() => new(
        RepositoryRoot: @"C:\repo",
        DataDirectory: @"C:\data",
        AdapterInstallRoot: @"C:\adapter",
        EngineId: "claude-code",
        Model: "sonnet",
        AccountLabel: "max-personal",
        TaskClass: "implement",
        ProofPackArtifacts: [],
        Providers: []);
}
