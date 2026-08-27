using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace ConPtyRuntimeProbe;

/// <summary>
/// Why the ConPTY child's output does not reach us — one run, every step reported.
/// </summary>
/// <remarks>
/// <para>The D7 conformance case <c>Output_DeliversTheChildProcessesOwnOutput</c> fails, and it
/// fails <b>differently</b> in two environments: in a sandboxed agent harness the channel carried
/// ConPTY's own 16 startup bytes and no child output; on a real console it carried nothing at all.
/// Both finished in under 100 ms, which means the output channel <i>completed</i> rather than timed
/// out — so the read side is ending early, before or instead of the child writing.</para>
///
/// <para>This is deliberately standalone and dependency-free: it re-implements the interop rather
/// than calling <c>AiDe.Core</c>, so a bug in the runtime's lifecycle cannot hide a bug in the
/// interop or vice versa. It reports what each call returned rather than asserting, because the
/// question is "what actually happens here", not "does it pass".</para>
/// </remarks>
internal static class Program
{
    private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    private static readonly IntPtr PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;
    private const int STILL_ACTIVE = 259;

    private static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        Header("Q0 — what kind of console does THIS process have?");
        Console.WriteLine($"  GetConsoleWindow()        : 0x{GetConsoleWindow():X}");
        Console.WriteLine($"  stdout file type          : {Describe(GetFileType(GetStdHandle(-11)))}");
        Console.WriteLine($"  stdin  file type          : {Describe(GetFileType(GetStdHandle(-10)))}");
        var list = new uint[16];
        Console.WriteLine($"  GetConsoleProcessList     : {GetConsoleProcessList(list, 16)}");
        Console.WriteLine();
        Console.WriteLine("  A real interactive console has a non-zero window and 'char' handles.");
        Console.WriteLine("  Pipes with a non-zero process list mean a headless/redirected console.");

        // Both handle-closing orders. The MS sample closes the console's pipe ends right after
        // CreatePseudoConsole; if ConPTY did NOT dup them, that would starve our reader of a writer
        // and produce exactly the immediate EOF both environments show.
        Run("A — close console pipe ends BEFORE CreateProcess", closeEarly: true);
        Run("B — close console pipe ends AFTER CreateProcess", closeEarly: false);

        // THE HYPOTHESIS. A host with no console of its own (which is every `dotnet test` host,
        // and this agent sandbox) may be why the child never attaches. AllocConsole gives this
        // process a real one; if C succeeds where A and B failed, the console is the variable and
        // the interop was never wrong.
        Header("C — same as A, but AllocConsole() FIRST");
        var had = GetConsoleWindow();
        var allocated = AllocConsole();
        Console.WriteLine($"  console before            : 0x{had:X}");
        Console.WriteLine($"  AllocConsole()            : {allocated}, "
            + $"lastError {Marshal.GetLastWin32Error()}");
        Console.WriteLine($"  console after             : 0x{GetConsoleWindow():X}");
        Run("C — with an allocated console", closeEarly: true);

        Header("What to send back");
        Console.WriteLine("  The whole output of this run. The lines that decide it are:");
        Console.WriteLine("   • 'first read returned' — 0 means EOF with no data (no writer on the pipe)");
        Console.WriteLine("   • 'child stdout seen'   — whether the pseudo console captured the child");
        Console.WriteLine("   • whether A and B differ — that isolates the handle-closing order");
        return 0;
    }

    private static void Run(string label, bool closeEarly)
    {
        Header(label);

        if (!CreatePipe(out var inputRead, out var inputWrite, IntPtr.Zero, 0)
            || !CreatePipe(out var outputRead, out var outputWrite, IntPtr.Zero, 0))
        {
            Console.WriteLine("  CreatePipe failed");
            return;
        }

        var hr = CreatePseudoConsole(
            new Coord { X = 80, Y = 25 }, inputRead, outputWrite, 0, out var console);
        Console.WriteLine($"  CreatePseudoConsole       : HRESULT 0x{hr:X8}, HPCON 0x{console:X}");
        if (hr != 0)
        {
            return;
        }

        if (closeEarly)
        {
            inputRead.Dispose();
            outputWrite.Dispose();
            Console.WriteLine("  closed console pipe ends  : yes (before CreateProcess)");
        }

        var size = IntPtr.Zero;
        InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);
        var attributes = Marshal.AllocHGlobal(size);
        var initialised = InitializeProcThreadAttributeList(attributes, 1, 0, ref size);
        var updated = UpdateProcThreadAttribute(
            attributes, 0, PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE, console, IntPtr.Size,
            IntPtr.Zero, IntPtr.Zero);
        Console.WriteLine($"  attribute list            : size {size}, init {initialised}, update {updated}");

        var startup = new StartupInfoEx
        {
            StartupInfo = new StartupInfo { cb = Marshal.SizeOf<StartupInfoEx>() },
            AttributeList = attributes,
        };

        var commandLine = "cmd.exe /c echo PROBE-CHILD-STDOUT\0".ToCharArray();
        var created = CreateProcess(
            null, ref commandLine[0], IntPtr.Zero, IntPtr.Zero, false,
            EXTENDED_STARTUPINFO_PRESENT | CREATE_UNICODE_ENVIRONMENT,
            IntPtr.Zero, null, ref startup, out var process);
        Console.WriteLine($"  CreateProcess             : {created}, pid {process.dwProcessId}, "
            + $"lastError {Marshal.GetLastWin32Error()}");

        if (!closeEarly)
        {
            inputRead.Dispose();
            outputWrite.Dispose();
            Console.WriteLine("  closed console pipe ends  : yes (after CreateProcess)");
        }

        // Synchronous reads on a dedicated thread, exactly as the runtime does it, reporting the
        // FIRST read's result — that single number separates "EOF immediately" from "blocked
        // waiting" from "data arrived".
        var seen = new StringBuilder();
        var firstRead = -2;
        var watch = Stopwatch.StartNew();
        var firstReadAt = TimeSpan.Zero;

        var reader = new Thread(() =>
        {
            try
            {
                using var stream = new FileStream(outputRead, FileAccess.Read, 4096, isAsync: false);
                var buffer = new byte[4096];
                while (true)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (firstRead == -2)
                    {
                        firstRead = read;
                        firstReadAt = watch.Elapsed;
                    }

                    if (read <= 0)
                    {
                        break;
                    }

                    lock (seen)
                    {
                        seen.Append(Encoding.UTF8.GetString(buffer, 0, read));
                    }
                }
            }
            catch (Exception ex)
            {
                lock (seen)
                {
                    seen.Append($"<<{ex.GetType().Name}: {ex.Message}>>");
                }

                if (firstRead == -2)
                {
                    firstRead = -3;
                }
            }
        })
        { IsBackground = true };
        reader.Start();

        Thread.Sleep(3000);

        GetExitCodeProcess(process.hProcess, out var exitCode);
        string text;
        lock (seen)
        {
            text = seen.ToString();
        }

        Console.WriteLine($"  first read returned       : {Explain(firstRead)} after {firstReadAt.TotalMilliseconds:F0} ms");
        Console.WriteLine($"  child exit code           : {(exitCode == STILL_ACTIVE ? "still running" : exitCode.ToString())}");
        Console.WriteLine($"  bytes received            : {Encoding.UTF8.GetByteCount(text)}");
        Console.WriteLine($"  child stdout seen         : "
            + (text.Contains("PROBE-CHILD-STDOUT", StringComparison.Ordinal) ? "YES" : "no"));
        Console.WriteLine($"  raw                       : {Visible(text)}");

        ClosePseudoConsole(console);
        DeleteProcThreadAttributeList(attributes);
        Marshal.FreeHGlobal(attributes);
        inputWrite.Dispose();
    }

    private static string Explain(int read) => read switch
    {
        -2 => "never returned (still blocked — the pipe is open and nothing was written)",
        -3 => "threw",
        0 => "0 = EOF IMMEDIATELY (no writer holds the pipe)",
        _ => $"{read} bytes",
    };

    private static string Visible(string text) => text.Length == 0
        ? "(empty)"
        : text.Replace("", "<ESC>").Replace("\r", "<CR>").Replace("\n", "<LF>");

    private static string Describe(uint fileType) => fileType switch
    {
        1 => "disk",
        2 => "char (a real console)",
        3 => "pipe (redirected)",
        _ => $"unknown ({fileType})",
    };

    private static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 72));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', 72));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Coord
    {
        public short X;
        public short Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfoEx
    {
        public StartupInfo StartupInfo;
        public IntPtr AttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfo
    {
        public int cb;
        public IntPtr lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr hProcess, hThread;
        public int dwProcessId, dwThreadId;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int CreatePseudoConsole(
        Coord size, SafeFileHandle input, SafeFileHandle output, uint flags, out IntPtr console);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern void ClosePseudoConsole(IntPtr console);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(
        out SafeFileHandle read, out SafeFileHandle write, IntPtr attributes, int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool InitializeProcThreadAttributeList(
        IntPtr list, int count, int flags, ref IntPtr size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateProcThreadAttribute(
        IntPtr list, uint flags, IntPtr attribute, IntPtr value, IntPtr size,
        IntPtr previous, IntPtr returnSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern void DeleteProcThreadAttributeList(IntPtr list);

    [DllImport("kernel32.dll", EntryPoint = "CreateProcessW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcess(
        string? applicationName, ref char commandLine, IntPtr processAttributes,
        IntPtr threadAttributes, bool inheritHandles, uint creationFlags, IntPtr environment,
        string? currentDirectory, ref StartupInfoEx startupInfo, out ProcessInformation info);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(IntPtr process, out int exitCode);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetStdHandle(int which);

    [DllImport("kernel32.dll")]
    private static extern uint GetFileType(IntPtr handle);

    [DllImport("kernel32.dll")]
    private static extern uint GetConsoleProcessList(uint[] list, uint count);
}
