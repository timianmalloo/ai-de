using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using AiDe.Core.Terminal;
using AiDe.Core.Tests.AgentPlane;

namespace AiDe.Core.Tests;

/// <summary>RED-FIRST PROBE. Deleted before the fix lands; here only to be observed passing.</summary>
[Trait("Platform", "Windows")]
[SupportedOSPlatform("windows")]
public sealed class JobContainmentTests
{
    [Fact]
    public void AnAssignThatFailsIsSilentToday()
    {
        using var child = Process.Start(new ProcessStartInfo(AcpProbeLauncher.Executable())
        {
            ArgumentList = { "--hang" },
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        })!;

        try
        {
            var handle = child.Handle;

            // A job whose last handle has closed: the object is gone, so the assign CANNOT succeed.
            var job = ConPtyInterop.CreateKillOnCloseJob();
            ConPtyInterop.CloseHandle(job);

            // EXACTLY what ConPtyTerminalSession.cs:263 and TerminalHostLauncher.cs:67 do today:
            // call it, discard the answer.
            ConPtyInterop.AssignProcessToJobObject(job, handle);

            Console.WriteLine($"RED: assign returned false, last error {Marshal.GetLastWin32Error()}, nothing was raised");
            Console.WriteLine($"RED: child {child.Id} HasExited={child.HasExited} - uncontained, and this test PASSES");

            Assert.False(child.HasExited);
        }
        finally
        {
            child.Kill(entireProcessTree: true);
            child.WaitForExit();
        }
    }
}
