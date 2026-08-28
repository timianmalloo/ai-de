using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ExtractionContainmentSpike;

/// <summary>
/// <b>Option A — let the repository's code run, but run it somewhere it cannot do harm.</b>
/// <para>Two Win32 mechanisms, deliberately separated so the spike can show what each one buys:
/// a <b>job object</b> bounds lifetime and resources (the product already uses one for terminals),
/// and a <b>low integrity level</b> bounds what the process may write. Only the second one stops
/// the attack; the first stops it from lasting.</para>
/// </summary>
[SupportedOSPlatform("windows")]
internal static partial class Sandbox
{
    // ---------------------------------------------------------------- job object
    private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;
    private const uint JOB_OBJECT_LIMIT_ACTIVE_PROCESS = 0x0008;
    private const uint JOB_OBJECT_LIMIT_JOB_MEMORY = 0x0200;
    private const uint JOB_OBJECT_LIMIT_JOB_TIME = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS { public ulong R, W, O, RT, WT, OT; }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit, PerJobUserTimeLimit;
        public uint LimitFlags;
        public nuint MinimumWorkingSetSize, MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public nuint Affinity;
        public uint PriorityClass, SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public nuint ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed;
    }

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr CreateJobObjectW(IntPtr a, string? name);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetInformationJobObject(IntPtr job, int cls, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION info, int len);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr h);

    /// <summary>A job that kills everything inside it when the handle closes, and bounds the rest.</summary>
    internal static IntPtr CreateBoundedJob(int maxProcesses, long memoryBytes, TimeSpan cpu)
    {
        var job = CreateJobObjectW(IntPtr.Zero, null);
        if (job == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateJobObject");

        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        info.BasicLimitInformation.LimitFlags =
            JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE | JOB_OBJECT_LIMIT_ACTIVE_PROCESS |
            JOB_OBJECT_LIMIT_JOB_MEMORY | JOB_OBJECT_LIMIT_JOB_TIME;
        info.BasicLimitInformation.ActiveProcessLimit = (uint)maxProcesses;
        // 100ns units, and it is JOB time: every process in the job draws on one budget.
        info.BasicLimitInformation.PerJobUserTimeLimit = (long)(cpu.TotalMilliseconds * 10_000);
        info.JobMemoryLimit = (nuint)memoryBytes;

        if (!SetInformationJobObject(job, 9 /* ExtendedLimitInformation */, ref info, Marshal.SizeOf(info)))
        {
            var err = Marshal.GetLastWin32Error();
            CloseHandle(job);
            throw new Win32Exception(err, "SetInformationJobObject");
        }
        return job;
    }

    internal static void Assign(IntPtr job, IntPtr process)
    {
        if (!AssignProcessToJobObject(job, process))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "AssignProcessToJobObject");
    }

    internal static void Close(IntPtr h) => CloseHandle(h);

    // ---------------------------------------------------------------- integrity level
    /// <summary>
    /// Drops a directory to LOW integrity so a low-IL child can write there — and ONLY there.
    /// Uses icacls rather than hand-rolled ACL interop: it is the documented tool, and a spike that
    /// gets the ACL subtly wrong would measure the wrong thing.
    /// </summary>
    internal static bool MakeDirectoryLowIntegrityWritable(string path)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("icacls", $"\"{path}\" /setintegritylevel (OI)(CI)Low")
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
        };
        using var p = System.Diagnostics.Process.Start(psi)!;
        p.StandardOutput.ReadToEnd();
        p.StandardError.ReadToEnd();
        p.WaitForExit();
        return p.ExitCode == 0;
    }
}
