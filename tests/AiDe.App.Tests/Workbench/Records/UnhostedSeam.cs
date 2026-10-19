using AiDe.App.Workbench;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;

namespace AiDe.App.Tests;

/// <summary>
/// Ruling 159(2)'s test seam, spelled once for the tests that send turns with no workspace: each call is only
/// <c>NotebookRuns.UnhostedForTests()</c> on the notebook the surface or shell holds. Product code never reaches it.
/// </summary>
internal static class UnhostedSeam
{
    /// <summary>The surface's turns run directly (unrecorded), as before Ruling 159(2).</summary>
    public static SessionDocumentSurface Unhosted(this SessionDocumentSurface surface)
    {
        surface.Notebook.Runs.UnhostedForTests();
        return surface;
    }

    /// <summary>The notebook's turns run directly, set before a surface binds (so nothing announces the refusal).</summary>
    public static NotebookModel Unhosted(this NotebookModel notebook)
    {
        notebook.Runs.UnhostedForTests();
        return notebook;
    }

    /// <summary>Opens the session through the shell, then its notebook's turns run directly.</summary>
    public static string OpenSessionDocumentUnhosted(this WorkbenchShell shell, AiDe.Core.Sessions.SessionConfig config)
    {
        var said = shell.OpenSessionDocument(config);
        shell.Notebook(NotebookId.ForSession(config.SessionId))!.Runs.UnhostedForTests();
        return said;
    }
}
