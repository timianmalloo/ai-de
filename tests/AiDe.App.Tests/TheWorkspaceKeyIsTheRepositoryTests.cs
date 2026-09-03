using System.Diagnostics;
using AiDe.App.Workbench;
using AiDe.Core.Watcher;

namespace AiDe.App.Tests;

/// <summary>
/// Two worktrees of one repository are one workspace, and the scoring key says so.
/// </summary>
/// <remarks>
/// <para><b>The defect this prevents.</b> A Weave score is keyed to the workspace because how an
/// agent works is partly a product of the repository's directives, conventions and gates. Key on the
/// <i>checkout</i> instead and two worktrees of one repository become two cohorts - segmenting on the
/// one axis that carries no difference in what is being measured, since worktrees share every
/// directive.</para>
///
/// <para><b>And it would have failed quietly.</b> Splitting a cohort shrinks every leaderboard cell;
/// a cell under the minimum renders Not Comparable, which is the de-anonymisation guard. The privacy
/// protection would have fired correctly for a reason that was not privacy - the mechanism working
/// while the meaning was wrong, which is the shape nobody investigates because the surface looks
/// right.</para>
///
/// <para><b>Why a real repository and a real worktree.</b> The resolution being tested IS git's
/// answer to <c>--git-common-dir</c>; a fake would test the fake. This repository is itself developed
/// across several worktrees, so the case is the working condition rather than a hypothetical.</para>
/// </remarks>
public sealed class TheWorkspaceKeyIsTheRepositoryTests
{
    [Fact]
    public void TwoWorktreesOfOneRepositoryResolveToOneWorkspace()
    {
        var root = NewDirectory();
        var linked = root + "-linked";

        try
        {
            Git(root, "init", "-q", "-b", "main");
            Git(root, "config", "user.email", "test@example.invalid");
            Git(root, "config", "user.name", "test");
            File.WriteAllText(Path.Combine(root, "a.txt"), "a");
            Git(root, "add", "-A");
            Git(root, "commit", "-q", "-m", "init");
            Git(root, "worktree", "add", "-q", "-b", "linked", linked);

            var primary = WorkbenchShell.ResolveGitFacts(root);
            var worktree = WorkbenchShell.ResolveGitFacts(linked);

            // If git could not answer, this test proves nothing - say which, rather than failing on
            // the comparison below with a message about paths.
            Assert.True(primary.RepoResolved, "git did not resolve --git-common-dir for the primary checkout");
            Assert.True(worktree.RepoResolved, "git did not resolve --git-common-dir for the linked worktree");

            // Genuinely two checkouts...
            Assert.NotEqual(primary.WorktreePath, worktree.WorktreePath);
            Assert.NotEqual(primary.Branch, worktree.Branch);

            // ...and one repository, which is what the score is keyed to.
            Assert.Equal(primary.RepoPath, worktree.RepoPath);
            Assert.Equal(WorkspaceKey.From(primary.RepoPath), WorkspaceKey.From(worktree.RepoPath));
        }
        finally
        {
            Delete(linked);
            Delete(root);
        }
    }

    [Fact]
    public void ADirectoryThatIsNotARepositoryIsItsOwnWorkspace()
    {
        // The fallback that must NOT be treated as a failure: a plain folder genuinely is the
        // workspace, so it resolves. Only "git answered for the worktree but not for the common dir"
        // is unresolved - the two were one silent branch before, and a checkout standing in for a
        // repository there is the split above, reintroduced where nothing would notice.
        var root = NewDirectory();

        try
        {
            var facts = WorkbenchShell.ResolveGitFacts(root);

            Assert.True(facts.RepoResolved);
            Assert.Equal(facts.WorktreePath, facts.RepoPath);
            Assert.NotNull(WorkspaceKey.From(facts.RepoPath));
        }
        finally
        {
            Delete(root);
        }
    }

    [Fact]
    public void TwoSpellingsOfOnePathAreOneKey_AndTwoRepositoriesStayTwo()
    {
        var forward = WorkspaceKey.From("C:/repos/app");
        var backward = WorkspaceKey.From(@"C:\Repos\App\");
        var other = WorkspaceKey.From("C:/repos/other");

        Assert.Equal(forward, backward);
        Assert.NotEqual(forward, other);
        Assert.Null(WorkspaceKey.From((string?)null));
        Assert.Null(WorkspaceKey.From("   "));
    }

    private static string NewDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "aide-wt-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Delete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                // git marks objects read-only; a plain recursive delete refuses on Windows.
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // A leaked temp directory is not worth failing a passing assertion over.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void Git(string workingDirectory, params string[] arguments)
    {
        var info = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using var process = Process.Start(info)
            ?? throw new InvalidOperationException("git could not be started; this test needs a real repository.");

        process.WaitForExit(30_000);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"git {string.Join(' ', arguments)} failed ({process.ExitCode}): {process.StandardError.ReadToEnd()}");
        }
    }
}
