using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ExtractionContainmentSpike;

/// <summary>
/// Launches a child process at LOW integrity, by duplicating this process's own token and lowering
/// only its integrity label. No privilege is needed for that — the token is derived from the
/// caller's and never gains anything — which is what makes this viable inside a desktop app.
/// </summary>
/// <remarks>
/// Windows' mandatory integrity policy is NO_WRITE_UP: a low-integrity process may still READ
/// medium-integrity files (so it can read the repository it is analysing and the SDK it needs) but
/// may not WRITE them. That asymmetry is exactly the shape extraction wants.
/// </remarks>
[SupportedOSPlatform("windows")]
internal static class LowIntegrity
{
    private const uint TOKEN_ALL_ACCESS = 0xF01FF;
    private const uint SE_GROUP_INTEGRITY = 0x00000020;
    private const int TokenIntegrityLevel = 25;
    private const uint CREATE_SUSPENDED = 0x00000004;
    private const uint CREATE_NO_WINDOW = 0x08000000;
    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    /// <summary>S-1-16-4096 — the low integrity level.</summary>
    private const string LowIntegritySid = "S-1-16-4096";

    [StructLayout(LayoutKind.Sequential)] private struct SID_AND_ATTRIBUTES { public IntPtr Sid; public uint Attributes; }
    [StructLayout(LayoutKind.Sequential)] private struct TOKEN_MANDATORY_LABEL { public SID_AND_ATTRIBUTES Label; }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public uint dwProcessId, dwThreadId; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GetCurrentProcess();
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool OpenProcessToken(IntPtr p, uint access, out IntPtr token);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool DuplicateTokenEx(IntPtr existing, uint access, IntPtr attrs, int impersonation, int type, out IntPtr dup);
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern bool ConvertStringSidToSidW(string sid, out IntPtr psid);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool SetTokenInformation(IntPtr token, int cls, ref TOKEN_MANDATORY_LABEL info, int len);
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessAsUserW(
        IntPtr token, string? appName, string cmdLine, IntPtr procAttrs, IntPtr threadAttrs,
        bool inherit, uint flags, IntPtr env, string? cwd, ref STARTUPINFO si, out PROCESS_INFORMATION pi);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern uint ResumeThread(IntPtr thread);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(IntPtr h);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern uint WaitForSingleObject(IntPtr h, uint ms);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetExitCodeProcess(IntPtr h, out uint code);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr p);

    /// <summary>
    /// Starts <paramref name="commandLine"/> at low integrity, assigns it to <paramref name="job"/>
    /// BEFORE it runs a single instruction (hence CREATE_SUSPENDED), then waits.
    /// </summary>
    /// <param name="tempDirectory">
    /// Where the child may put its scratch. MSBuild writes to TMP/TEMP before it does anything
    /// else, and a low-integrity process inherits the parent's TEMP — which it cannot write. The
    /// first run of this spike failed for exactly that reason, and it looked like "low integrity
    /// breaks extraction" rather than "the child had nowhere to write".
    /// </param>
    internal static (bool Started, uint ExitCode, string? Error) RunContained(
        string commandLine, string workingDirectory, IntPtr job, TimeSpan timeout,
        string? tempDirectory = null)
    {
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ALL_ACCESS, out var self))
            return (false, 0, $"OpenProcessToken: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

        IntPtr dup = IntPtr.Zero, sid = IntPtr.Zero, env = IntPtr.Zero;
        try
        {
            if (!DuplicateTokenEx(self, TOKEN_ALL_ACCESS, IntPtr.Zero, 2 /*SecurityImpersonation*/, 1 /*TokenPrimary*/, out dup))
                return (false, 0, $"DuplicateTokenEx: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

            if (!ConvertStringSidToSidW(LowIntegritySid, out sid))
                return (false, 0, $"ConvertStringSidToSid: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

            var label = new TOKEN_MANDATORY_LABEL
            {
                Label = new SID_AND_ATTRIBUTES { Sid = sid, Attributes = SE_GROUP_INTEGRITY },
            };
            if (!SetTokenInformation(dup, TokenIntegrityLevel, ref label, Marshal.SizeOf(label) + GetLengthSid(sid)))
                return (false, 0, $"SetTokenInformation(IntegrityLevel): {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

            var si = new STARTUPINFO { cb = Marshal.SizeOf<STARTUPINFO>() };
            env = tempDirectory is null ? IntPtr.Zero : BuildEnvironment(tempDirectory);
            var flags = CREATE_SUSPENDED | CREATE_NO_WINDOW | (env == IntPtr.Zero ? 0 : CREATE_UNICODE_ENVIRONMENT);

            if (!CreateProcessAsUserW(dup, null, commandLine, IntPtr.Zero, IntPtr.Zero, false,
                    flags, env, workingDirectory, ref si, out var pi))
                return (false, 0, $"CreateProcessAsUser: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

            try
            {
                // Into the job before it is resumed: a process that runs first and is contained
                // second is not contained.
                Sandbox.Assign(job, pi.hProcess);
                ResumeThread(pi.hThread);

                var waited = WaitForSingleObject(pi.hProcess, (uint)timeout.TotalMilliseconds);
                if (waited != 0) return (true, uint.MaxValue, $"child did not exit within {timeout.TotalSeconds:F0}s");
                GetExitCodeProcess(pi.hProcess, out var code);
                return (true, code, null);
            }
            finally
            {
                CloseHandle(pi.hThread);
                CloseHandle(pi.hProcess);
            }
        }
        finally
        {
            if (env != IntPtr.Zero) Marshal.FreeHGlobal(env);
            if (sid != IntPtr.Zero) LocalFree(sid);
            if (dup != IntPtr.Zero) CloseHandle(dup);
            CloseHandle(self);
        }
    }

    /// <summary>
    /// The current environment with TMP/TEMP repointed at a directory the child can actually write.
    /// A Win32 environment block is UTF-16 "K=V\0K=V\0\0".
    /// </summary>
    private static IntPtr BuildEnvironment(string tempDirectory)
    {
        var vars = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
        {
            var key = (string)e.Key;
            if (key.Length == 0) continue;
            vars[key] = e.Value as string ?? string.Empty;
        }

        vars["TMP"] = tempDirectory;
        vars["TEMP"] = tempDirectory;
        // MSBuild node reuse leaves long-lived worker processes behind; a contained run wants none.
        vars["MSBUILDDISABLENODEREUSE"] = "1";

        var sb = new System.Text.StringBuilder();
        foreach (var kv in vars) sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\0');
        sb.Append('\0');
        return Marshal.StringToHGlobalUni(sb.ToString());
    }

    [DllImport("advapi32.dll")] private static extern int GetLengthSid(IntPtr sid);
}
