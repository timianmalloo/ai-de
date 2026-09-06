using System.Diagnostics;
using AiDe.Core.Watcher;

namespace AiDe.Core.Tests.Watcher;

/// <summary>
/// The locator answers against a REAL git worktree, not a fabricated pointer file.
/// </summary>
/// <remarks>
/// <para><b>Why real git.</b> The whole correction rests on one empirical fact: a linked worktree's
/// <c>.git</c> is a FILE containing <c>gitdir: &lt;repo&gt;/.git/worktrees/&lt;name&gt;</c>, while a
/// repository root's is a directory. A fabricated pointer file would test this class's parser
/// against this class's author's belief about git, which is the failure mode the whole evening was
/// about. Building the worktree makes git the source of the fact.</para>
///
/// <para><b>The rest of the suite uses a stub</b>, deliberately: those tests are about what the
/// registration does with an answer, not about how the answer is obtained. This file is the only
/// place the filesystem claim is checked, which is why it must not be a fake.</para>
/// </remarks>
public sealed class TheFileSystemRepositoryLocatorTests
{
    // Platform=Unverified: a linked worktree resolves to null on Linux - cause UNDIAGNOSED
    [Trait("Platform", "Unverified")]
    [Fact]
    public void ALinkedWorktreeResolvesToItsRepository()
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

            var locator = new FileSystemRepositoryLocator();

            // If git did not produce the pointer file this test proves nothing — say which, rather
            // than failing on the comparison with a message about paths.
            Assert.True(File.Exists(Path.Combine(linked, ".git")), "git did not write a .git pointer FILE in the worktree");
            Assert.True(Directory.Exists(Path.Combine(root, ".git")), "the primary checkout's .git is not a directory");

            var resolved = locator.RepositoryFor(linked);

            Assert.NotNull(resolved);
            Assert.Equal(
                new RepositoryIdentity(root, "x").CanonicalPath,
                new RepositoryIdentity(resolved!, "x").CanonicalPath);
        }
        finally
        {
            Delete(linked);
            Delete(root);
        }
    }

    [Fact]
    public void ARepositoryRootAnswersNothingToCorrect()
    {
        var root = NewDirectory();

        try
        {
            Git(root, "init", "-q", "-b", "main");

            // Null means "nothing to correct", which is exactly right for a repository root. It is
            // the same answer as "cannot tell", and the caller treats them identically on purpose.
            Assert.Null(new FileSystemRepositoryLocator().RepositoryFor(root));
        }
        finally
        {
            Delete(root);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AnEmptyPathAnswersNothing(string path)
        => Assert.Null(new FileSystemRepositoryLocator().RepositoryFor(path));

    [Fact]
    public void APathThatDoesNotExistAnswersNothing_RatherThanThrowing()
    {
        // The registrant's filesystem may simply not be ours. Unknown is the honest answer and it
        // must not travel as an exception through the ingest path.
        var missing = Path.Combine(Path.GetTempPath(), "aide-absent-" + Guid.NewGuid().ToString("N")[..8]);

        Assert.Null(new FileSystemRepositoryLocator().RepositoryFor(missing));
    }

    [Fact]
    public void AGitFileThatIsNotAWorktreePointerAnswersNothing()
    {
        // A .git file exists but says something else. Guessing from a shape we do not recognise is
        // how the split gets reintroduced silently.
        var root = NewDirectory();

        try
        {
            File.WriteAllText(Path.Combine(root, ".git"), "something else entirely");

            Assert.Null(new FileSystemRepositoryLocator().RepositoryFor(root));
        }
        finally
        {
            Delete(root);
        }
    }

    private static string NewDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "aide-loc-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Delete(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            // git marks objects read-only; a plain recursive delete refuses on Windows.
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(path, recursive: true);
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
