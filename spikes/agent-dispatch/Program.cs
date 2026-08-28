using System.Runtime.InteropServices;

// ---------------------------------------------------------------------------------------------
// ADR-0010's stated residual: dispatch has been proven against a live SHELL, not an agent CLI.
//
// An agent is a different animal. It buffers input, streams its answer, takes seconds rather than
// milliseconds, and may not echo what it was given. A receipt saying PtyWriteAccepted proves bytes
// left the process; only the agent's own reply proves the prompt arrived somewhere that could act.
//
// The dispatch itself runs inside AiDe.Core.TerminalHost, launched here with CREATE_NEW_CONSOLE
// because ConPTY does not attach a child to a console-less host (DC-014).
// ---------------------------------------------------------------------------------------------

var agent = args.Length > 0 ? args[0] : "claude";
var helper = Locate();

Console.WriteLine("Spike — prompt dispatch into a real agent CLI");
Console.WriteLine(new string('=', 96));
Console.WriteLine($"agent  : {agent}");
Console.WriteLine($"helper : {helper}");
Console.WriteLine(new string('=', 96));
Console.WriteLine();

if (!File.Exists(helper))
{
    Console.WriteLine("the terminal host is not built. Run: dotnet build tests/AiDe.Core.TerminalHost");
    return 2;
}

var report = Path.Combine(Path.GetTempPath(), $"aide-agent-{Guid.NewGuid():N}.log");

try
{
    var trusted = args.Length > 1 ? args[1] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    Console.WriteLine($"cwd    : {trusted}  (a TRUSTED folder — see RESULT.md)");
    Console.WriteLine();
    var exitCode = RunInNewConsole(helper, $"\"{report}\" dispatch-agent \"{agent}\" \"{trusted}\"", TimeSpan.FromMinutes(5));
    var log = File.Exists(report) ? File.ReadAllText(report) : "(no report written)";

    Console.WriteLine(log);
    Console.WriteLine(new string('=', 96));

    Console.WriteLine(exitCode switch
    {
        0 => "VERDICT: a prompt dispatched across a real daemon reached a real AGENT session and the\n"
             + "         agent acted on it. ADR-0010's residual is closed by evidence.",
        3 => "VERDICT: VOID — the agent CLI did not start. Nothing was measured.",
        6 => "VERDICT: the receipt recorded an accepted write and the agent never answered. Either the\n"
             + "         agent needs a different submit convention, or the prompt was written into a void.",
        8 => "VERDICT: REFUSED, and that is the fix working. Readiness could not be established, so\n"
             + "         the prompt was NOT written into whatever the agent is showing, and no durable\n"
             + "         attempt was recorded. Before the readiness contract this identical run reported\n"
             + "         PtyWriteAccepted for a prompt the trust gate ate.",
        _ => $"VERDICT: the probe exited {exitCode}. See the log above.",
    });

    return exitCode;
}
finally
{
    if (File.Exists(report)) File.Delete(report);
}

static string Locate() => Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..",
    "tests", "AiDe.Core.TerminalHost", "bin", "Debug", "net10.0", "AiDe.Core.TerminalHost.exe"));

static int RunInNewConsole(string exe, string arguments, TimeSpan limit)
{
    const uint CREATE_NEW_CONSOLE = 0x00000010;

    var startup = new STARTUPINFO { cb = Marshal.SizeOf<STARTUPINFO>() };
    if (!CreateProcess(exe, $"\"{exe}\" {arguments}", IntPtr.Zero, IntPtr.Zero, false,
            CREATE_NEW_CONSOLE, IntPtr.Zero, null, ref startup, out var info))
    {
        throw new InvalidOperationException($"CreateProcess failed: {Marshal.GetLastWin32Error()}");
    }

    try
    {
        // Waited on the HANDLE, not through Process.GetProcessById: a Process this object did not
        // start refuses to report an exit code, and the exit code IS the result here.
        if (WaitForSingleObject(info.hProcess, (uint)limit.TotalMilliseconds) != 0)
        {
            TerminateProcess(info.hProcess, 99);
            return 99;
        }

        return GetExitCodeProcess(info.hProcess, out var code) ? (int)code : -1;
    }
    finally
    {
        CloseHandle(info.hThread);
        CloseHandle(info.hProcess);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct PROCESS_INFORMATION
{
    public IntPtr hProcess, hThread;
    public uint dwProcessId, dwThreadId;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct STARTUPINFO
{
    public int cb;
    public string? lpReserved, lpDesktop, lpTitle;
    public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
    public short wShowWindow, cbReserved2;
    public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
}

internal static partial class Native
{
}

public partial class Program
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool CreateProcess(
        string? applicationName, string commandLine, IntPtr processAttributes, IntPtr threadAttributes,
        bool inheritHandles, uint creationFlags, IntPtr environment, string? currentDirectory,
        ref STARTUPINFO startupInfo, out PROCESS_INFORMATION processInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool GetExitCodeProcess(IntPtr handle, out uint exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool TerminateProcess(IntPtr handle, uint exitCode);
}
