using AiDe.Core.Watcher;

namespace AiDe.App.Tests;

/// <summary>
/// The workspace key the App-side scoring tests segment on.
/// </summary>
/// <remarks>
/// A deliberate twin of <c>AiDe.Core.Tests.Watcher.TestWorkspaces</c> rather than a shared assembly:
/// the two suites never build one cohort together, so they cannot drift into the split this constant
/// exists to prevent, and a test-only assembly reference between them would buy nothing.
/// </remarks>
internal static class TestWorkspaces
{
    /// <summary>The repository the scoring tests key on.</summary>
    public static readonly WorkspaceKey Repo = WorkspaceKey.From("C:/repos/app")!;
}
