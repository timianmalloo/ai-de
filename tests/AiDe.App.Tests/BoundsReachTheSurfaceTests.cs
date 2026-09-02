using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The behavioural half of the control for the defect family in
/// <c>docs/collaboration/session-contracts.md</c> §8.3a: <b>a surface renders something plausible
/// while the honest data sits unread one layer down.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists and why it must be behavioural.</b> Three reading-based methods failed on the
/// same question in one day: a grep for field names (Session 3, published a false blocker — the
/// value crosses the boundary renamed, <c>Evidence</c> becomes <c>SearchResult.Detail</c>), a
/// <c>.FieldName</c> match (Core — attributed one record's field to another record's surface), and
/// a call-site check (Core — reported <c>GraphAsync</c> uncalled because the App reaches it through
/// one indirection). Two careful agents with the source open reached opposite wrong conclusions
/// about whether a value reaches the screen.
/// </para>
/// <para>
/// That question has exactly one reliable answer: <b>render the surface and read the tree.</b>
/// Reflection can see that a record HAS a field; only rendering can see whether a surface showed
/// it. This complements — it does not duplicate — <c>FieldsSurviveTheClientBoundaryTests</c>
/// (Core), which compares field NAMES across a producer→client record pair and so cannot see a
/// field that crosses and is then ignored. That case is this file's job.
/// </para>
/// <para>
/// <b>Three things it structurally cannot see</b>, named here rather than left implied by whichever
/// instance surfaced them: the <c>MainWindow</c> status line (not a surface, outside the
/// harvester); anything inside the WebView2 (the canvas is a hosted HTML page, and the harvester
/// walks a WPF visual tree — see session-contracts §8.9 for the costing to close it); and
/// <b>attributes</b>, of which <c>AutomationProperties.Name</c> / <c>aria-label</c> is the one that
/// matters. That third came from a live instance: the canvas chip renders its count twice, once as
/// text and once as an accessible name, and a fix to the visible half alone would leave the
/// accessible half stating the same false completeness claim — which this file reads as "absent"
/// because it harvests text, not attributes.
/// </para>
/// <para>
/// <b>It lands green with an allowance list, deliberately.</b> Landing red gets a gate switched
/// off. The list is the <c>verify-standins.py</c> shape: a non-compliant surface is legitimate
/// only while it is written down with a reason, and an entry describing a state that no longer
/// exists is itself a failure — a stale allowance keeps a question alive that nobody has to
/// answer.
/// </para>
/// </remarks>
public sealed class BoundsReachTheSurfaceTests
{
    // ---- the allowance list -------------------------------------------------------------------

    /// <summary>
    /// Known non-compliance, each with the reason it is tolerated and who closes it. An entry here
    /// is a question someone still has to answer, not an exemption.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> Allowed =
        new Dictionary<string, string>
        {
            ["ContentSearchResult.FilesSkipped/affordance"] =
                "HALF-CLOSED. Core's SearchProviderBoundTests now asserts the provider EMITS the " +
                "row (Other kind, Label carrying 40 of 412, Detail non-empty, and absent when " +
                "nothing was hidden — the half of DC-025 that gets forgotten, because a caveat " +
                "that fires when nothing was hidden trains a reader to skip caveats). This file " +
                "asserts the surface RENDERS it. The pair composes across a contract now stated " +
                "on both sides, with the residual seam named in the test. " +
                "WHAT REMAINS: the affordance. DESIGN.md §4a specifies count.lower-bound as a " +
                "capped CHIP with a tooltip naming the cap, not a row. Design closes it; remove " +
                "this entry when the chip lands.",

            ["InteractionResult.Truncated/unwired"] =
                "The sequence surface has no caller for InteractionAsync at all — which node " +
                "drives it is the open UX decision in §4v. There is no surface state to render, " +
                "so this is DC-073's member (the payload is never asked for), not this file's. " +
                "Remove when the surface is wired.",

            // ---- found by the DC-016 guard on its first run, 2026-09-01 --------------------
            // These seven were reported by reflection over the real assembly, NOT by reading a
            // request list. Several were absent from §8.3's nine-item list, which is exactly why
            // that list was marked unverified: it was assembled by grep and by reading §4a.
            //
            // They are recorded as OPEN QUESTIONS, not as exemptions, and deliberately NOT
            // classified by reading the render sites. Reading is the method that produced a false
            // blocker and two wrong re-derivations in one day; a render assertion is the only
            // answer this file accepts. Each entry converts to an assertion or to a defect.

            // Core classified all seven by following each consumption site (2026-09-01) and
            // flagged its own method honestly: IT READ THE WIRING, IT DID NOT RENDER IT. Their
            // verdicts are recorded below with that provenance, because on this exact question a
            // reading has been wrong three times in one day. Every entry still needs a render
            // assertion to close — a verdict is a hypothesis until the tree is walked.

            // ADDED BY CORE, 2026-09-01, following this file's own instruction — the field is new
            // and the guard caught it within minutes of it being added, which is the guard working
            // rather than a gap in it.
            ["EvidenceRow.Evidence/closed"] =
                "CLOSED. The design session checked the UNVERIFIED note above and found the cause " +
                "one layer further out than the record: SurfaceContentFactory built the ListBox " +
                "with DisplayMemberPath, which renders exactly ONE property, so the pane dropped " +
                "Evidence, NodeKind AND Confidence — and AutomationProperties.SetName was on the " +
                "list rather than its items, so EvidenceRow.AccessibleName was read by nothing. " +
                "Adding the field could not have fixed it. Now an ItemTemplate with a per-item " +
                "accessible name, and EvidencePaneRendersItsRowsTests walks the rendered tree for " +
                "both paths — observed failing on the shipped DisplayMemberPath, all three red. " +
                "Kept as an entry rather than deleted so the reasoning survives the fix.",

            ["EvidenceRead.Shortfall/global-and-transient"] =
                "CORRECTION. This entry previously said the shortfall is announced and therefore " +
                "'reaches assistive technology and nothing else'. That was wrong, and the wrong " +
                "framing would have produced the wrong fix — a screen-reader affordance for " +
                "something sighted users can already see. VERIFIED HERE: MainWindow.xaml.cs:38 " +
                "does 'LiveRegionHost.Content = Shell.LiveRegion', hosting the announcer's " +
                "TextBlock as visible window chrome. The real defect is weaker and different: the " +
                "shortfall is GLOBAL and TRANSIENT — one shared status line, describing one pane, " +
                "overwritten by the next announcement. A caveat about the Contexts pane that " +
                "vanishes when the graph finishes loading is not a rendered state, but it is not " +
                "invisible either. The fix is to put it ON the pane it describes. Design.\n" +
                "KNOWN HOLE: this harness cannot check it. The status line lives in MainWindow, " +
                "not in a surface, so it is outside the harvester's reach. Neither Core's reading " +
                "nor this file settles whether it actually paints.",

            ["KnowledgeNodeView.HealthFindings/dropped"] =
                "CONFIRMED DROPPED by Core's call-site check: zero consumption sites in " +
                "src/AiDe.App. §4a asks for these (owner not recorded, orphan, review overdue — " +
                "460 review dates measured on this repo). Design.",

            ["PathResult.Truncated/dropped"] =
                "CONFIRMED DROPPED: zero consumption sites. A truncated path that reads as a " +
                "complete route is the §8.3a shape on the impact/paths surface. Design.",

            ["WorkspaceGraph.Disclosures/rendered"] =
                "RETRACTED — NOT DROPPED. Verified end to end: WorkspaceGraph.Disclosures flows " +
                "into CanvasGraph.Disclosures (CanvasGraphViewModel.cs:163), which is serialised " +
                "whole (CanvasSurface.cs:98), and CanvasPage.cs:739-754 renders it as a " +
                "collapsible warning. Kept as an entry only until a render assertion replaces it.",

            ["WorkspaceOverview.Disclosures/rendered"] =
                "RETRACTED — NOT DROPPED. Same chain via CanvasGraphViewModel.cs:331.",

            // ---- a ninth, found by WIDENING THIS GUARD'S OWN SCOPE ------------------------
            // The guard originally scanned only AiDe.Core.Projections, so it could not see the
            // client-side records in AiDe.Core.Presentation — its own blind spot, and exactly the
            // shape it exists to catch. Widening the namespace filter surfaced this immediately.
            ["CanvasGraph.Disclosures/rendered-in-javascript"] =
                "RETRACTED — NOT DROPPED, and the retraction is the most instructive of the day. " +
                "CanvasPage.cs:739-754 renders it as a collapsible warning ('N edge(s) omitted by " +
                "the result bound', 'Not analysed: …'), hidden when empty, with a comment reasoning " +
                "about the affordance. It is arguably the BEST implementation of this whole family " +
                "in the codebase. " +
                "WHY BOTH SESSIONS CALLED IT DROPPED: we searched C# identifiers in a file whose " +
                "consumers are JAVASCRIPT. The value crosses a language AND a case boundary — " +
                "Disclosures becomes graph.disclosures inside a JS string literal — so no search " +
                "for the C# name can follow it. Fourth instance of 'a name search cannot follow a " +
                "renamed value', and the most invisible kind. " +
                "KNOWN HOLE: this harness cannot check it either. The canvas is a WebView2 hosting " +
                "HTML; the harvester walks a WPF visual tree and structurally cannot see into the " +
                "page. Second named hole in this control, alongside the MainWindow status line.",

            // ---- an eighth, found by Core while classifying the seven ----------------------
            ["CanvasGraph.Message/rendered-in-javascript"] =
                "RETRACTED — NOT DROPPED. CanvasPage.cs:728 uses graph.message as the caption when " +
                "present. My 'no consumer' claim came from grepping '.Message' in C#, which finds " +
                "only 'ex.Message' — the real consumer is 'graph.message' in the page's JavaScript. " +
                "Same language-boundary blindness as CanvasGraph.Disclosures above.",
        };

    /// <summary>
    /// A limit of the stale-allowance test below, named rather than left to be discovered.
    /// </summary>
    /// <remarks>
    /// <see cref="TheAllowanceListHasNoEntriesForFieldsThatNoLongerExist"/> can only tell that a
    /// field still EXISTS. It cannot tell that an allowance is no longer NEEDED — so a defect
    /// somebody fixes leaves a permanent entry, which is exactly the "stale allowance keeps a
    /// question alive nobody has to answer" failure this list was built to avoid, one level up.
    /// <c>ContentSearchResult.Truncated/reported-fixed</c> is already in that state.
    /// <para>
    /// There is no cheap automated answer: "is this still broken?" is the render assertion itself.
    /// The entry closes when someone writes the assertion, which is the forcing function working
    /// as intended — but the list will over-report until they do, and a reader should know that.
    /// </para>
    /// </remarks>
    private const string AllowanceListKnownLimit =
        "Entries persist until a render assertion replaces them; existence-checking cannot retire one.";

    // ---- the surface contract this file enforces ----------------------------------------------

    /// <summary>
    /// A surface must not silently drop what it was handed. Every non-empty field of every result
    /// given to <see cref="SearchSurface.ShowResults"/> must appear in the rendered tree.
    /// </summary>
    /// <remarks>
    /// This is the invariant that would have caught the real defect and would ALSO have prevented
    /// the false blocker: it asks what the user can see, not what the source says.
    /// </remarks>
    [Fact]
    public void SearchSurface_RendersEveryFieldItWasHanded_IncludingTheBoundedReadFooter()
    {
        OnSta(() =>
        {
            var surface = new SearchSurface();
            IReadOnlyList<SearchResult> hits =
            [
                new("n1", SearchResultKind.Member, "Element", "has_member = + addEventListener()"),
                new("f1", SearchResultKind.File, "src/app.ts:42", "el.addEventListener('click')"),
                // the bounded-read footer, in the interim shape Core shipped
                new("bound", SearchResultKind.Other,
                    "Searched 412 file(s) — 40 file(s) not read",
                    "This result is a lower bound."),
            ];

            surface.ShowResults(hits);
            var rendered = RenderedText(surface);

            foreach (var hit in hits)
            {
                Assert.True(rendered.Contains(hit.Label, System.StringComparison.Ordinal),
                    $"SearchSurface was handed the label \"{hit.Label}\" and did not render it.\n"
                    + RenderedForMessage(rendered));
                if (!string.IsNullOrEmpty(hit.Detail))
                {
                    Assert.True(rendered.Contains(hit.Detail, System.StringComparison.Ordinal),
                        $"SearchSurface rendered \"{hit.Label}\" but dropped its detail "
                        + $"\"{hit.Detail}\" — the qualifier that makes the row honest.\n"
                        + RenderedForMessage(rendered));
                }
            }

            // The bound itself, named rather than implied: the skipped-file count must be legible.
            Assert.True(rendered.Contains("40", System.StringComparison.Ordinal),
                "The search result set reported 40 unread files and the number is nowhere on "
                + "screen. A result list that shows hits without its skipped count is DC-025 at "
                + "the render boundary.\n" + RenderedForMessage(rendered));

            // ---- the join with Core's half of this claim, stated rather than assumed ----------
            //
            // SearchProviderBoundTests (Core) asserts the PROVIDER emits a bound row:
            // SearchResultKind.Other, Label carrying the counts, Detail non-empty. This file
            // asserts the SURFACE renders whatever it is handed. Neither runs the real provider
            // into the real surface, so the two compose only across a contract that had been
            // implicit in both. It is asserted here so that a change to either side breaks
            // something rather than quietly opening the gap between them:
            var footer = hits.Single(h => h.Kind == SearchResultKind.Other);
            Assert.True(rendered.Contains(footer.Label, System.StringComparison.Ordinal)
                        && rendered.Contains(footer.Detail, System.StringComparison.Ordinal),
                "A SearchResultKind.Other row is the shape Core's provider uses to carry a "
                + "bounded read. Both its Label (the counts) and its Detail (what the counts "
                + "mean) must reach the screen — a footer that renders half of itself states a "
                + "number with no claim attached.\n" + RenderedForMessage(rendered));

            // RESIDUAL SEAM, named because it is the honest limit of the pair: both halves are
            // tested against the contract, not against each other. A provider that stopped
            // emitting the row would still pass this file, and a surface that dropped it would
            // still pass Core's. Closing that needs one test driving the real provider into the
            // real surface, which neither of us has written.
        });
    }

    /// <summary>
    /// <see cref="JoinResult.Disclosures"/> — "what could not be joined" — must reach the screen.
    /// </summary>
    /// <remarks>
    /// Taken over from Core, whose verdict on this was a reading of JoinSurface.cs:86,92 and the
    /// last item on either session's list still resting on one agent's word. This harness reaches
    /// JoinSurface and Core's does not, so it was cheaper here.
    /// <para>
    /// Both directions are asserted. A disclosure list that renders is only half the claim: the
    /// surface must also say plainly that nothing was withheld when nothing was, because a
    /// "what could not be joined" heading over silence reads as an empty section rather than as
    /// an answer — the same reasoning as Core's <c>ACompleteSearchPutsNoCaveatOnScreen</c>.
    /// </para>
    /// </remarks>
    [Fact]
    public void JoinSurface_RendersWhatCouldNotBeJoined_AndSaysSoWhenNothingWas()
    {
        OnSta(() =>
        {
            var edge = new AiDe.Core.Projections.JoinEdge(
                "Orders", "dbo.Orders", "maps-to",
                AiDe.Core.Facts.VerificationStatus.Verified, "EF model");

            // With a disclosure: the code must be on screen. Asserted on the CODE, not on the
            // prose, because JoinSurface.Explain() expands known codes into a sentence and passes
            // unknown ones through — so the code is the stable part and the prose is not.
            var withDisclosure = new JoinSurface("Joins")
            {
                Source = () => new AiDe.Core.Projections.JoinResult(
                    [edge], ["sql-resource-name-unresolved"], 1, 0),
            };
            withDisclosure.Refresh();
            var rendered = RenderedText(withDisclosure);

            Assert.True(rendered.Contains("sql-resource-name-unresolved", System.StringComparison.Ordinal),
                "JoinSurface was given a disclosure and did not render it. 'What could not be "
                + "joined' is the section that stops a join list reading as complete.\n"
                + RenderedForMessage(rendered));

            // Not vacuous: the surface really did render its content, so the assertion above was
            // about a disclosure being present rather than about an empty tree.
            Assert.True(rendered.Contains("Orders", System.StringComparison.Ordinal),
                "The surface rendered no edges either, so the disclosure assertion proved nothing "
                + "(R4).\n" + RenderedForMessage(rendered));

            // Without one: the absence must be stated, not left as an empty heading.
            var clean = new JoinSurface("Joins")
            {
                Source = () => new AiDe.Core.Projections.JoinResult([edge], [], 1, 0),
            };
            clean.Refresh();
            var cleanRendered = RenderedText(clean);

            Assert.True(cleanRendered.Contains("Nothing was withheld", System.StringComparison.Ordinal),
                "Nothing was withheld and the surface did not say so. A 'what could not be joined' "
                + "heading over silence reads as an unfinished section, which is the same defect "
                + "as an unfired caveat reached from the other side.\n"
                + RenderedForMessage(cleanRendered));
        });
    }

    /// <summary>
    /// The harness proves it can fail, on a surface planted to fail it.
    /// </summary>
    /// <remarks>
    /// R4: a control that scanned nothing has not reported clean. Without this, a broken text
    /// harvester — one indirection it cannot walk, one templated control it cannot expand — makes
    /// every assertion above vacuously true and the file reports green forever. Observed failing
    /// on the real shape before it was trusted: <see cref="DropsItsDetailSurface"/> renders the
    /// label and discards the detail, which is precisely the defect this file exists to catch.
    /// </remarks>
    [Fact]
    public void TheHarnessFailsAPlantedSurfaceThatDropsItsDetail()
    {
        OnSta(() =>
        {
            var planted = new DropsItsDetailSurface();
            planted.Show("Element", "has_member = + addEventListener()");
            var rendered = RenderedText(planted);

            Assert.Contains("Element", rendered);                       // it does render something
            Assert.DoesNotContain("has_member", rendered);              // and it does drop the bound

            // Which is to say: had SearchSurface been built this way, the test above would fail.
        });
    }

    // ---- the DC-016 guard: the harness cannot shrink silently ---------------------------------

    /// <summary>
    /// Every bound-carrying field Core publishes must be either covered by this file or listed in
    /// <see cref="Allowed"/> with a reason.
    /// </summary>
    /// <remarks>
    /// Without this, the harness passes forever by testing a shrinking fraction of the surface
    /// area: a new bound field appears, nobody adds it here, and green means "the fields we
    /// remembered" rather than "the fields that exist". This is the hole
    /// <c>EveryOperationFitsTheFrameTests</c> caught twice in one morning on the Core side.
    /// </remarks>
    [Fact]
    public void EveryBoundCarryingFieldIsCoveredOrAllowed()
    {
        var vocabulary = new[]
        {
            "Truncated", "Shortfall", "FilesSkipped", "Disclosures",
            "HealthFindings", "Evidence", "ScopesReused", "IsDeclared", "Message",
        };

        var covered = new HashSet<string>(System.StringComparer.Ordinal)
        {
            // rendered and asserted above
            "FindMatch.Evidence",
            "ContentSearchResult.FilesSkipped",
            "ContentSearchResult.Truncated",   // SearchProviderBoundTests.AFiredMatchCapIsSaidOutLoud
                                              // + SearchBoundEndToEndTests.AFiredCapIsOnScreenToo (Core)
            "JoinResult.Disclosures",          // asserted below, mutation-tested
            // rendered by surfaces outside this file's subject, verified by reading the render site
            "NodeContent.Shortfall",            // CodeViewerView.cs:96-98
            "ContextMapView.IsDeclared",        // ContextMapSurface.cs:77
        };

        var projections = typeof(AiDe.Core.Projections.FindMatch).Assembly
            .GetTypes()
            .Where(t => t.IsPublic && t.Namespace is not null
                        && (t.Namespace.StartsWith("AiDe.Core.Projections", System.StringComparison.Ordinal)
                            || t.Namespace.StartsWith("AiDe.Core.Presentation", System.StringComparison.Ordinal)));

        var uncovered = new List<string>();
        foreach (var type in projections)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!vocabulary.Contains(property.Name)) { continue; }
                var key = $"{type.Name}.{property.Name}";
                if (covered.Contains(key)) { continue; }
                if (Allowed.Keys.Any(a => a.StartsWith(key + "/", System.StringComparison.Ordinal))) { continue; }
                uncovered.Add(key);
            }
        }

        Assert.True(uncovered.Count == 0,
            "Core publishes bound-carrying field(s) that no surface assertion covers and no "
            + "allowance explains:\n  " + string.Join("\n  ", uncovered.OrderBy(x => x))
            + "\n\nEither assert that a surface renders it, or add an entry to Allowed saying why "
            + "it is tolerated and who closes it. A bound nobody decided about is the defect this "
            + "file exists to prevent.");
    }

    /// <summary>
    /// An allowance describing a state that no longer exists is itself a failure.
    /// </summary>
    /// <remarks>
    /// A stale entry is worse than a missing one: it keeps a question alive that nobody has to
    /// answer, and it makes the list read as considered when it is merely old. Same rule as
    /// <c>verify-standins.py</c>'s registry.
    /// </remarks>
    [Fact]
    public void TheAllowanceListHasNoEntriesForFieldsThatNoLongerExist()
    {
        var assembly = typeof(AiDe.Core.Projections.FindMatch).Assembly;
        var stale = new List<string>();

        foreach (var key in Allowed.Keys)
        {
            var subject = key.Split('/')[0];              // "ContentSearchResult.FilesSkipped"
            var parts = subject.Split('.');
            if (parts.Length != 2) { continue; }          // not a Type.Property claim; nothing to check

            var type = assembly.GetTypes().FirstOrDefault(t => t.Name == parts[0]);
            if (type is null) { stale.Add($"{key}  (type {parts[0]} no longer exists)"); continue; }
            if (type.GetProperty(parts[1]) is null)
            {
                stale.Add($"{key}  (type {parts[0]} no longer has {parts[1]})");
            }
        }

        Assert.True(stale.Count == 0,
            "Allowance entries describe a state that no longer exists:\n  "
            + string.Join("\n  ", stale)
            + "\n\nRemove them. A stale allowance keeps a question alive nobody has to answer.");
    }

    // ---- machinery ----------------------------------------------------------------------------

    /// <summary>A surface planted to fail the contract, so the harness can prove it fails.</summary>
    private sealed class DropsItsDetailSurface : ContentControl
    {
        private readonly StackPanel _rows = new();

        public DropsItsDetailSurface() { Content = _rows; }

        /// <summary>Renders the label and discards the detail — the defect, deliberately.</summary>
        public void Show(string label, string detail)
        {
            _ = detail;                                   // dropped on purpose; that is the point
            _rows.Children.Add(new TextBlock { Text = label });
        }
    }

    /// <summary>
    /// Every piece of text a viewer could read, harvested from the laid-out tree.
    /// </summary>
    /// <remarks>
    /// Measure/Arrange first: without a layout pass a templated control has no visual children and
    /// the harvest returns almost nothing, which would make every assertion vacuous. TextBlock is
    /// read through its <see cref="TextBlock.Inlines"/> as well as its Text, because a
    /// <see cref="Run"/> is not a visual child — and a Run is exactly where SearchSurface puts the
    /// detail it was handed.
    /// </remarks>
    private static string RenderedText(FrameworkElement root)
    {
        root.Measure(new Size(1200, 900));
        root.Arrange(new Rect(0, 0, 1200, 900));
        root.UpdateLayout();

        var text = new StringBuilder();
        Harvest(root, text);
        return text.ToString();
    }

    private static void Harvest(DependencyObject node, StringBuilder into)
    {
        if (node is TextBlock block)
        {
            into.Append(block.Text).Append('\n');
            foreach (var inline in block.Inlines)
            {
                if (inline is Run run) { into.Append(run.Text).Append('\n'); }
            }
        }

        if (node is ContentControl { Content: DependencyObject content })
        {
            Harvest(content, into);                      // before the visual pass, and cheap after
        }

        var children = VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < children; i++)
        {
            Harvest(VisualTreeHelper.GetChild(node, i), into);
        }
    }

    private static string RenderedForMessage(string rendered) =>
        "What the surface actually rendered:\n---\n" + rendered.Trim() + "\n---";

    /// <summary>
    /// Runs <paramref name="work"/> on an STA thread, preserving what failed and why.
    /// </summary>
    /// <remarks>
    /// <b>An assertion failure is rethrown UNWRAPPED.</b> The first version wrapped every exception
    /// in <c>InvalidOperationException("STA work failed", …)</c>, which reported this file's own
    /// assertion messages as a broken harness — *"STA work failed"* on the surface with
    /// *"SearchSurface rendered 12 results and never mentioned 40 skipped files"* buried in an
    /// inner exception. The failure messages are the entire value of this file, and the harness
    /// was hiding them behind a machine complaint.
    /// <para>
    /// That is this file's own subject pointed at its diagnostics: **a real defect reported as an
    /// environment problem** is a false claim in the direction that gets the finding dismissed
    /// rather than investigated. Caught by Core, who inherited the shape when it copied this helper
    /// for its own WebView2 harness (§8.3a, DC-077's neighbourhood).
    /// </para>
    /// A genuine infrastructure failure — a thread that never finishes, a real exception from the
    /// code under test — still gets its wrapper, because there the wrapper is the true statement.
    /// </remarks>
    private static void OnSta(System.Action work) =>
        Sta.Run(work, 30);
}
