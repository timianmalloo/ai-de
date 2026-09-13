using System.Reflection;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;

namespace AiDe.App.Tests;

/// <summary>
/// DM7's seam request, landed: <c>ThreadDiagnostics</c> used to duplicate <c>WorkbenchDiagnostics</c>'s
/// sink check and log-file path in a private copy rather than calling the one writer. This proves the
/// duplicate is gone, not merely that the logged behaviour still looks the same (which the sink-driven
/// tests elsewhere already cover and would not have caught a debt this shaped).
/// </summary>
public sealed class ThreadDiagnosticsHasOneWriterTests
{
    [Fact]
    public void WorkbenchDiagnosticsWrite_IsInternal_SoAnotherAppSideTypeCanCallItDirectly()
    {
        var write = typeof(WorkbenchDiagnostics).GetMethod(
            "Write", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

        Assert.NotNull(write);
        Assert.True(write!.IsAssembly, "WorkbenchDiagnostics.Write must be internal (DM7), not private");
    }

    [Fact]
    public void ThreadDiagnostics_CarriesNoPrivateWriterOfItsOwn()
    {
        var duplicateWrite = typeof(ThreadDiagnostics).GetMethod(
            "Write", BindingFlags.NonPublic | BindingFlags.Static);
        var duplicateGate = typeof(ThreadDiagnostics).GetField(
            "Gate", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.Null(duplicateWrite);
        Assert.Null(duplicateGate);
    }
}
