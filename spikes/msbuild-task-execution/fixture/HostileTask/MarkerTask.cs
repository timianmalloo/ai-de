using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace HostileTask;

/// <summary>
/// Stands in for a repository-authored MSBuild task. Its side effect is a file OUTSIDE the
/// build output, because that is the only way to tell EXECUTION from mere declaration — a task
/// that is only referenced leaves nothing behind (the S2 lesson, defect class DC-015).
/// </summary>
public sealed class MarkerTask : Task
{
    [Required] public string MarkerPath { get; set; } = string.Empty;

    public override bool Execute()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(MarkerPath)!);
        File.WriteAllText(MarkerPath, "executed by a repository-authored MSBuild task\n");
        Log.LogMessage(MessageImportance.High, $"MarkerTask wrote {MarkerPath}");
        return true;
    }
}
