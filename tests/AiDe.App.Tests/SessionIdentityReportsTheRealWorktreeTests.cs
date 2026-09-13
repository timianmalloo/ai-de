using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The session identity a terminal registers with must describe the tree it is actually in.
/// </summary>
/// <remarks>
/// <para><b>What this replaces.</b> <c>IdentityFor</c> previously sent
/// <c>WorktreeBranch: "workspace"</c> — a literal, for every session in every repository — and
/// assigned the same variable to both <c>RepoPath</c> and <c>WorktreePath</c>, so a linked worktree
/// could not differ from its repository BY CONSTRUCTION. The watcher was therefore structurally
/// unable to show two worktrees of one repository as distinct sessions, which is the one thing that
/// field exists to do.</para>
///
/// <para><b>Why a literal was worse than an absence.</b> The attribute is required — it is always
/// emitted by <c>ToAttributes</c>, and a register with incomplete identity is QUARANTINED, so a
/// missing value would delete the session from the watcher rather than annotate it. Some string must
/// therefore be sent. <c>"workspace"</c> reads as a branch name; the replacement reads as an absence.
/// </para>
/// </remarks>
public sealed class SessionIdentityReportsTheRealWorktreeTests
{
    [Fact]
    public void AGitWorktree_ReportsItsRealBranch_NotAPlaceholder()
    {
        var facts = WorkbenchShell.ResolveGitFacts(Directory.GetCurrentDirectory());

        Assert.NotEqual("workspace", facts.Branch);
        Assert.False(string.IsNullOrWhiteSpace(facts.Branch));
    }

    [Fact]
    public void AGitWorktree_ReportsAWorktreePathAndARepoPath()
    {
        var facts = WorkbenchShell.ResolveGitFacts(Directory.GetCurrentDirectory());

        Assert.False(string.IsNullOrWhiteSpace(facts.WorktreePath));
        Assert.False(string.IsNullOrWhiteSpace(facts.RepoPath));

        // They are resolved by two different git primitives rather than one variable, so they CAN
        // differ. Asserting they are separately populated is the part that would have failed before;
        // whether they differ depends on whether the test runs in a linked worktree.
        Assert.False(string.IsNullOrWhiteSpace(facts.RepoDisplay));
    }

    [Fact]
    public void ANonRepository_ReportsAnUnknownBranch_NeverAGuess()
    {
        var temp = Path.Combine(Path.GetTempPath(), "aide-not-a-repo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var facts = WorkbenchShell.ResolveGitFacts(temp);

            Assert.Equal(WorkbenchShell.GitFacts.BranchUnknown, facts.Branch);
        }
        finally
        {
            try { Directory.Delete(temp, recursive: true); } catch { /* best effort */ }
        }
    }

    /// <summary>
    /// Ruling 85: the observed commit, not a fixture literal a revision label would double.
    /// Shape- and cross-checked, not merely non-empty (found in review: a non-empty/not-"rev-1"
    /// check alone would not catch <c>Head</c> and <c>Branch</c> swapping which <c>git rev-parse</c>
    /// call they read from — a branch name is also non-empty and also never equals "rev-1").
    /// </summary>
    [Fact]
    public void AGitWorktree_ReportsItsObservedHead_NotAFixtureLiteral()
    {
        var facts = WorkbenchShell.ResolveGitFacts(Directory.GetCurrentDirectory());

        Assert.False(string.IsNullOrWhiteSpace(facts.Head));
        Assert.NotEqual("rev-1", facts.Head);

        // A short commit hash's own shape — lowercase hex only — which a Head/Branch swap would
        // violate immediately against this worktree's branch (which contains "/"); also asserted
        // to differ from Branch directly.
        Assert.Matches("^[0-9a-f]{4,40}$", facts.Head!);
        Assert.NotEqual(facts.Branch, facts.Head);

        // Independently observed, not read back through the method under test: the same git
        // primitive, invoked directly here rather than via ResolveGitFacts, must agree.
        Assert.Equal(RunGitShortHead(Directory.GetCurrentDirectory()), facts.Head);
    }

    private static string RunGitShortHead(string workingDirectory)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", "rev-parse --short HEAD")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        })!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output.Trim();
    }

    /// <summary>No commit to observe: null, never a guessed or fixture value (Ruling 85).</summary>
    [Fact]
    public void ANonRepository_ReportsNoHead_NeverAGuess()
    {
        var temp = Path.Combine(Path.GetTempPath(), "aide-not-a-repo-head-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var facts = WorkbenchShell.ResolveGitFacts(temp);

            Assert.Null(facts.Head);
        }
        finally
        {
            try { Directory.Delete(temp, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void AMissingDirectory_DoesNotThrow_AndReportsUnknown()
    {
        var absent = Path.Combine(Path.GetTempPath(), "aide-absent-" + Guid.NewGuid().ToString("N"));

        var facts = WorkbenchShell.ResolveGitFacts(absent);

        Assert.Equal(WorkbenchShell.GitFacts.BranchUnknown, facts.Branch);
    }

}
