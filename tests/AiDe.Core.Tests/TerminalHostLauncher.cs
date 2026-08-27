using System.Runtime.InteropServices;

namespace AiDe.Core.Tests;

/// <summary>
/// Launches <c>AiDe.Core.TerminalHost</c> in a console of its own and returns its verdict.
/// </summary>
/// <remarks>
/// <para>ConPTY attaches a child to the pseudo console only when the launching process owns a
/// <b>real console</b>, and a <c>dotnet test</c> host never does — its stdio is redirected
/// (<b>DC-014</b>). Any claim about the child round trip therefore has to be made from a process we
/// start ourselves with <c>CREATE_NEW_CONSOLE</c>, and <c>Process.Start</c> cannot request one, so
/// this is the one place a test needs interop of its own.</para>
///
/// <para>Shared rather than duplicated: two suites now need it, and a second hand-rolled copy of
/// <c>CreateProcessW</c> is the kind of thing that drifts silently.</para>
/// </remarks>
internal static class TerminalHostLauncher
{
    /// <summary>The helper executable, built alongside the tests and found relative to them.</summary>
    internal static string LocateHelper()
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "AiDe.Core.TerminalHost", "bin"));
        var configuration = Directory.Exists(Path.Combine(root, "Release")) ? "Release" : "Debug";
        var candidate = Path.Combine(root, configuration, "net10.0", "AiDe.Core.TerminalHost.exe");

        Assert.True(
            File.Exists(candidate),
            $"the terminal host helper was not built. Expected it at:\n  {candidate}\n"
            + "Build the solution rather than the test project alone.");

        return candidate;
    }

    /// <summary>
    /// Starts the helper with <c>CREATE_NEW_CONSOLE</c> and waits for its verdict.
    /// </summary>
    /// <remarks>
    /// <c>Process.Start</c> cannot request a new console, so this is the one place a test needs
    /// interop of its own.
    /// </remarks>
    internal static async Task<int> RunInNewConsoleAsync(
        string exe, string report, TimeSpan limit, string? mode = null)
    {
        const uint CREATE_NEW_CONSOLE = 0x00000010;

        var startup = new NativeStartupInfo { cb = Marshal.SizeOf<NativeStartupInfo>() };
        // The mode is the second argument, so the existing one-argument call keeps its
        // meaning and the capture probe stays working unchanged.
        var arguments = mode is null ? $"\"{report}\"" : $"\"{report}\" \"{mode}\"";
        var commandLine = $"\"{exe}\" {arguments}\0".ToCharArray();

        if (!CreateProcessW(
                null, ref commandLine[0], IntPtr.Zero, IntPtr.Zero, false, CREATE_NEW_CONSOLE,
                IntPtr.Zero, Path.GetDirectoryName(exe), ref startup, out var info))
        {
            Assert.Fail($"could not start the helper: Win32 error {Marshal.GetLastWin32Error()}");
        }

        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(info.dwProcessId);
            using var deadline = new CancellationTokenSource(limit);
            await process.WaitForExitAsync(deadline.Token);
            return process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            Assert.Fail("the terminal host helper did not exit within its deadline");
            return -1;
        }
        finally
        {
            CloseHandle(info.hThread);
            CloseHandle(info.hProcess);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeStartupInfo
    {
        public int cb;
        public IntPtr lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeProcessInformation
    {
        public IntPtr hProcess, hThread;
        public int dwProcessId, dwThreadId;
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateProcessW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessW(
        string? applicationName, ref char commandLine, IntPtr processAttributes,
        IntPtr threadAttributes, bool inheritHandles, uint creationFlags, IntPtr environment,
        string? currentDirectory, ref NativeStartupInfo startupInfo,
        out NativeProcessInformation information);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
