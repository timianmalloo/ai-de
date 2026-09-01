using System.Text.RegularExpressions;

namespace AiDe.App.Tests;

/// <summary>
/// Every caller of <c>CanvasSurface.EvaluateAsync</c> checks whether it could ask at all.
/// </summary>
/// <remarks>
/// <para><b>The hazard.</b> <c>EvaluateAsync</c> catches its own exception and returns
/// <c>"(evaluate failed: …)"</c> as an ordinary string, so failure and success have the same type
/// and the compiler cannot help. A caller that treats the result as a value measures a sentence:
/// <c>CanvasProbe</c>'s non-vacuity guard tested the node count for <c>"0"</c> or <c>""</c>, and a
/// page that never loaded reported <c>nodes rendered: (evaluate failed: …)</c>, passed the guard,
/// and let the probe carry on as though the canvas were full — the exact vacuity that guard exists
/// to prevent, arriving by a route it did not consider.</para>
///
/// <para><b>Why a control and not just the fix.</b> Fixing the caller fixes the instance; the class
/// survives, because the next caller can make the same mistake the same way. This is the
/// automated-control rung: a new unguarded caller fails here rather than at whatever a broken page
/// happens to report.</para>
///
/// <para><b>What would close it properly, and why it is not done here.</b> The real fix is at the
/// API — <c>Task&lt;string?&gt;</c> returning null, or letting the exception propagate — so that
/// ignoring failure is a null dereference or a crash rather than a plausible measurement. That is a
/// one-line change in <c>CanvasSurface.cs</c>, which the session contract assigns to Design (§2).
/// Proposed to them rather than taken. Until then this holds the line, and it should be deleted the
/// day the API stops handing back failure and success in one type.</para>
///
/// <para><b>A sentinel string is the defect family itself.</b> A failure that renders as a plausible
/// value is what the bound-dropping work has been about all day; this is that shape pointed at our
/// own tooling.</para>
/// </remarks>
public sealed class EvaluateAsyncCallersGuardTests
{
    private const string Sentinel = "(evaluate failed";

    private static DirectoryInfo RepoRoot()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);

        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AiDe.sln")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        return root!;
    }

    /// <summary>Files that call <c>EvaluateAsync</c>, other than the one that defines it.</summary>
    private static List<string> Callers(DirectoryInfo root)
    {
        var callers = new List<string>();

        foreach (var directory in new[] { "src", "tests", "spikes" })
        {
            var path = Path.Combine(root.FullName, directory);
            if (!Directory.Exists(path)) continue;

            foreach (var file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                var text = File.ReadAllText(file);

                // The declaration is not a call. Matching `.EvaluateAsync(` rather than the bare
                // name keeps CanvasSurface's own definition out.
                if (Regex.IsMatch(text, @"\.EvaluateAsync\s*\(")) callers.Add(file);
            }
        }

        return callers;
    }

    [Fact]
    public void EveryCallerChecksForTheFailureSentinel()
    {
        var root = RepoRoot();
        var unguarded = new List<string>();

        foreach (var caller in Callers(root))
        {
            if (!File.ReadAllText(caller).Contains(Sentinel, StringComparison.Ordinal))
            {
                unguarded.Add(Path.GetRelativePath(root.FullName, caller));
            }
        }

        Assert.True(unguarded.Count == 0,
            "these files call EvaluateAsync and never check for its failure sentinel, so a page "
            + "that never loaded reads as a measurement: " + string.Join(", ", unguarded)
            + ". Check the result starts with \"" + Sentinel + "\" before using it — failure and "
            + "success come back in the same type and the compiler cannot help.");
    }

    [Fact]
    public void TheSearchActuallyFindsCallers()
    {
        // The DC-016 guard. If the pattern stops matching — the method is renamed, the call is
        // wrapped — the assertion above passes by examining nothing, which is the shape this whole
        // file exists to prevent.
        var callers = Callers(RepoRoot());

        Assert.True(callers.Count > 0,
            "no caller of EvaluateAsync was found anywhere, so the guard above is checking an empty "
            + "set. Either the method was renamed and this control needs updating, or it has no "
            + "callers and this file should be deleted.");
    }

    [Fact]
    public void TheSentinelStillLooksLikeThis()
    {
        // The control matches a STRING the product produces. If CanvasSurface stops producing it —
        // ideally because the API started returning null or throwing — every caller trivially
        // "passes" by not containing text nothing emits. Pinned to the source that emits it.
        var surface = Path.Combine(
            RepoRoot().FullName, "src", "AiDe.App", "Workbench", "CanvasSurface.cs");

        Assert.True(File.Exists(surface), "CanvasSurface.cs has moved; this control cannot see it");

        Assert.Contains(Sentinel, File.ReadAllText(surface), StringComparison.Ordinal);
    }
}
