using System.Text;
using System.Windows;
using System.Windows.Automation;
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
/// with no lease (<c>TheReadOnlyTurnNeedsNoLeaseTests</c>). <b>And on the one editor after CV-1</b>:
/// the source text is the message the operator typed (Ruling 66; DESIGN.md SC1), so a mention in
/// the message derives its pattern and a mention in a structure line (Goal · Done when · Not in
/// scope) derives nothing — the structure is a decoration of the turn, never a second source of
/// write scope. The source-of-derivation control is therefore proven where a lease exists: the
/// editor's mention sits in the message, the mention nobody typed sits in an attachment body beside it.</para>
/// </remarks>
public sealed class TheLeaseDerivesFromTheEditorsSourceTextTests
{
    [Fact]
    public void AnAttachmentBodyMentionDerivesNoPatternButTheEditorTextStillDoes()
    {
        var draft = CompleteGoalBlockWithNoMentions();
        draft.SetFreeFormText("Fix the bug in @src/AiDe.App/Foo.cs\n");
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

    /// <summary>The message is the source text: the same mention in the message derives; in a structure line it does not (Ruling 66; CV-1).</summary>
    [Fact]
    public void TheSameMentionTypedInTheMessageDerivesItsPattern_AndInAStructureLineItDoesNot()
    {
        var inMessage = CompleteGoalBlockWithNoMentions();
        inMessage.SetFreeFormText("Rename the helper in @src/Payments/Money.cs.\n");

        var request = new ComposerSendGate().Send(Context(), inMessage, null, out var refusal);

        Assert.Null(refusal);
        Assert.Equal(["src/Payments/Money.cs"], request!.Lease!.Exclusive);

        var inGoal = CompleteGoalBlockWithNoMentions();
        inGoal.SetGoalValue(GoalBlockFields.GoalKey, "Rename the helper in @src/Payments/Money.cs.");

        var readOnly = new ComposerSendGate().Send(Context(), inGoal, null, out var refused);

        Assert.Null(refused);
        Assert.True(readOnly!.IsReadOnly);
        Assert.Null(readOnly.Lease);
    }

    private static ComposerDraft CompleteGoalBlockWithNoMentions()
    {
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);
        draft.SetGoalValue(GoalBlockFields.GoalKey, "Rename the helper.");
        draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "It compiles under the new name.");
        draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "Nothing else changes.");
        draft.SetFreeFormText("Rename the helper.\n");
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

            // A write-shaped turn (Ruling 73): a complete goal block, its mention in the message.
            surface.Draft.SwitchTo(ComposerShape.GoalBlock);
            surface.Draft.SetGoalValue(GoalBlockFields.GoalKey, "Rename the helper.");
            surface.Draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "It compiles.");
            surface.Draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "Nothing else.");

            // An attachment, added directly (no file system involved) so its mention is on the draft
            // exactly as an affirmed attach would leave it.
            surface.Draft.Add(new ComposerAttachment(
                DisplayPath: "notes.md",
                ResolvedPath: @"C:\repo\notes.md",
                Bytes: 40,
                Text: "See @src/AiDe.Core/Bar.cs for context.",
                IsOutsideWorkspace: false,
                Sha256: "deadbeef"));

            // A keystroke into the message — the display path's normal trigger.
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

    // The source-scan guard over every `LeaseDerivation` call site (root `src/`, recursive, tokens
    // `LeaseDerivation.Derive(` / `LeaseDerivation.Patterns(`, allowlist by (file, argument)) lives in
    // `TheSendGateSendsWhatProjectionProjectsTests.EveryLeaseDerivationCallSiteIsNamedAndPassesTheSourceTextSymbol`
    // since CV-2 moved the one `Derive(` call into `Projection.Project` (ADR-0033 rule 2) — one census,
    // one definition of the allowlist.

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
    private static string LeaseLine(FrameworkElement root)
    {
        root.Measure(new Size(1200, 900));
        root.Arrange(new Rect(0, 0, 1200, 900));
        root.UpdateLayout();

        // The decoration line's lease segment (SC2; CV-1): the value TextBlock is named "lease <value>",
        // so the rendered value is read from the element that shows it, never from the property.
        // The walk visits the same subtree twice (the visual tree and a ContentControl's logical
        // content), so the element is deduplicated by reference rather than asserted unique.
        var segment = Descendants(root)
            .OfType<TextBlock>()
            .Where(block => AutomationProperties.GetName(block).StartsWith("lease ", StringComparison.Ordinal))
            .Distinct()
            .Single();
        return "Lease: " + segment.Text.Replace(" · ", ", ", StringComparison.Ordinal);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject node)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            var child = VisualTreeHelper.GetChild(node, i);
            yield return child;
            foreach (var inner in Descendants(child))
            {
                yield return inner;
            }
        }

        if (node is ContentControl { Content: DependencyObject content })
        {
            yield return content;
            foreach (var inner in Descendants(content))
            {
                yield return inner;
            }
        }
    }

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
