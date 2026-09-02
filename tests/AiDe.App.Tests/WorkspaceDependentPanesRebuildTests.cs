using System.Reflection;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using AiDe.Testing;

namespace AiDe.App.Tests;

/// <summary>
/// Every pane whose content needs a workspace is rebuilt when one arrives.
/// </summary>
/// <remarks>
/// <para><b>The defect, reported from a screenshot.</b> The default layout's "Domain" pane read
/// <i>"Domain is not available in this build"</i> against a fully indexed workspace. The factory
/// builds <c>view</c> and <c>inspector</c> only <c>when queries is not null</c>, so a pane realized
/// at construction — before <c>AttachWorkspace</c> — renders the unavailable placeholder and keeps
/// it until something marks it to rebuild. Nothing did.</para>
///
/// <para><b>It is the same defect as the watcher panes, swept too narrowly.</b> That fix marked
/// <c>sessions</c>, <c>board</c> and <c>leaderboard</c> — the three kinds in the report — and the
/// two kinds with the identical dependency were left out. A fix that stops at the reported instance
/// is not finished (CI1), and this is what that costs: a permanent "not available" on the pane the
/// default layout puts in front of every new user.</para>
///
/// <para><b>Why this test derives the set instead of listing it.</b> A hand-written list is what
/// failed. The rebuild set must be a superset of what the factory gates on <c>queries</c>, and the
/// gate lives in <c>SurfaceContentFactory</c> — so the test reads the factory's behaviour by
/// BUILDING each kind twice, once with queries and once without, and requires any kind that differs
/// to be in the rebuild set. A kind added tomorrow with the same dependency fails here without
/// anybody remembering this file exists.</para>
/// </remarks>
public sealed class WorkspaceDependentPanesRebuildTests
{
    /// <summary>A workspace that refuses everything — presence is what the factory gates on.</summary>
    private sealed class StubQueries : FakeWorkspaceQueries { }

    /// <summary>The rebuild set the shell actually uses, read from the field rather than restated.</summary>
    private static IReadOnlySet<string> RebuildSet()
    {
        var field = typeof(WorkbenchShell).GetField(
            "WorkspaceDependentPaneKinds", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(field);
        return (IReadOnlySet<string>)field!.GetValue(null)!;
    }

    /// <summary>What the factory produces for one kind, with and without a workspace.</summary>
    private static (string Without, string With) Build(string kind) => OnSta(() =>
    {
        var surface = new Surface($"probe-{kind}", kind, "Probe");

        var without = new SurfaceContentFactory(queries: null).Create(surface);
        var with = new SurfaceContentFactory(new StubQueries()).Create(surface);

        return (Describe(without), Describe(with));
    });

    /// <summary>
    /// The pane's real content type, past the island-chrome wrapper.
    /// </summary>
    /// <remarks>
    /// The facelift wraps each pane in a <c>Border</c>, so comparing the OUTER type says "Border"
    /// for every kind and this test passes by distinguishing nothing. Its own DC-016 guard caught
    /// that before it shipped — and it is the same wrapper that once hid the class-diagram surface
    /// from <c>ContentFor(id).OfType&lt;T&gt;()</c> and left it looking empty over a full workspace.
    /// </remarks>
    private static string Describe(object? content)
    {
        while (content is System.Windows.Controls.Border { Child: { } inner })
        {
            content = inner;
        }

        return content?.GetType().Name ?? "(null)";
    }

    private static T OnSta<T>(Func<T> body) =>
        Sta.Run<T>(body, 60);

    [Fact]
    public void EveryKindThatNeedsAWorkspaceIsInTheRebuildSet()
    {
        var rebuild = RebuildSet();
        var missing = new List<string>();
        var dependent = 0;

        foreach (var kind in SurfaceContentFactory.KnownKinds)
        {
            // A terminal hosts a live process; building one twice to compare would start two, and
            // rebuilding one kills a session (DC-029). Excluded by the same rule that excludes it
            // from the rebuild set.
            if (kind == "terminal" || kind == "canvas") continue;

            var (without, with) = Build(kind);

            if (string.Equals(without, with, StringComparison.Ordinal)) continue;

            dependent++;

            if (!rebuild.Contains(kind)) missing.Add($"{kind} ({without} -> {with})");
        }

        // The DC-016 guard. If the factory stopped gating anything on `queries`, every kind would
        // build identically, `missing` would be empty, and this test would pass by comparing a set
        // of nothing.
        Assert.True(dependent > 0,
            "no surface kind builds differently with and without a workspace, so this test is "
            + "checking an empty set — either the factory stopped gating on queries, or Build() has "
            + "stopped distinguishing the two cases");

        Assert.True(missing.Count == 0,
            "these kinds render one thing without a workspace and another with one, and are NOT "
            + "marked to rebuild when a workspace attaches — so a pane realized at startup keeps "
            + "its 'not available' placeholder forever: " + string.Join(", ", missing));
    }

    [Fact]
    public void TheDefaultLayoutsOwnPanesAreCovered()
    {
        // The pane that actually failed was in the DEFAULT layout, which is what a new user sees
        // before touching anything. Asserting the general rule above without this would let the
        // general rule pass while the shipped default was still broken.
        var rebuild = RebuildSet();

        var defaults = Layout.Default().AllStacks()
            .SelectMany(stack => stack.Surfaces)
            .Where(s => s.Kind is not ("terminal" or "canvas"))
            .ToList();

        Assert.NotEmpty(defaults);

        foreach (var surface in defaults)
        {
            var (without, with) = Build(surface.Kind);

            if (string.Equals(without, with, StringComparison.Ordinal)) continue;

            Assert.True(rebuild.Contains(surface.Kind),
                $"the default layout ships '{surface.Title}' (kind {surface.Kind}), which renders "
                + $"{without} with no workspace and {with} with one, and is never rebuilt — every "
                + "new user sees the placeholder permanently");
        }
    }
}
