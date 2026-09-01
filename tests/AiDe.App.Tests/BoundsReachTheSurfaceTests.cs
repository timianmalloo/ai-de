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
                "Core wired the skipped-file count as a trailing SearchResult of kind Other " +
                "(a footer row). The number IS visible, so this file's render assertions pass. " +
                "DESIGN.md §4a specifies count.lower-bound as a capped CHIP with a tooltip naming " +
                "the cap, not a row. Design closes it; remove this entry when the chip lands.",

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

            ["ContentSearchResult.Truncated/undetermined"] =
                "Sibling of FilesSkipped on the same record. Core's interim footer names the " +
                "skipped count; whether the Truncated flag itself is distinguishable is not " +
                "asserted. Needs a render assertion on SearchSurface with Truncated = true.",

            ["EvidenceRead.Shortfall/announced-only"] =
                "WorkbenchShell.cs:575 calls AnnounceShortfall — which §4a says reaches assistive " +
                "technology and nothing else. An announcement is not a rendered state. Needs a " +
                "render assertion on the surfaces that carry a bounded count. Design.",

            ["JoinResult.Disclosures/undetermined"] =
                "No render assertion. JoinSurface is Design's. Needs one.",

            ["KnowledgeNodeView.HealthFindings/undetermined"] =
                "§4a asks for these to be rendered (owner not recorded, orphan, review overdue — " +
                "460 review dates measured on this repo). No render assertion. Design.",

            ["PathResult.Truncated/undetermined"] =
                "A truncated path result that reads as a complete route is the §8.3a shape on the " +
                "impact/paths surface. No render assertion. Needs one.",

            ["WorkspaceGraph.Disclosures/undetermined"] =
                "The graph's own admission of what it could not establish. No render assertion.",

            ["WorkspaceOverview.Disclosures/undetermined"] =
                "As above, for the clustered overview.",
        };

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
            "HealthFindings", "Evidence", "ScopesReused", "IsDeclared",
        };

        var covered = new HashSet<string>(System.StringComparer.Ordinal)
        {
            // rendered and asserted above
            "FindMatch.Evidence",
            "ContentSearchResult.FilesSkipped",
            // rendered by surfaces outside this file's subject, verified by reading the render site
            "NodeContent.Shortfall",            // CodeViewerView.cs:96-98
            "ContextMapView.IsDeclared",        // ContextMapSurface.cs:77
        };

        var projections = typeof(AiDe.Core.Projections.FindMatch).Assembly
            .GetTypes()
            .Where(t => t.IsPublic && t.Namespace is not null
                        && t.Namespace.StartsWith("AiDe.Core.Projections", System.StringComparison.Ordinal));

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

    private static void OnSta(System.Action work)
    {
        System.Exception? failure = null;
        var thread = new Thread(() => { try { work(); } catch (System.Exception ex) { failure = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(System.TimeSpan.FromSeconds(30)), "STA thread did not finish");
        if (failure is not null) { throw new System.InvalidOperationException("STA work failed", failure); }
    }
}
