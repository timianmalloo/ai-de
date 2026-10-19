using System.Runtime.InteropServices;

namespace NotebookEditorE1Spike;

/// <summary>
/// Real Windows input: SendInput keystrokes and clicks, keyboard-layout switching for the focused
/// (browser-process) window, and the IMM control messages that open a Windows IME. Nothing here is
/// synthesized inside the page — every event enters through the OS input queue.
/// </summary>
internal static class Native
{
    public const ushort VkBack = 0x08;
    public const ushort VkReturn = 0x0D;
    public const ushort VkControl = 0x11;
    public const ushort VkEscape = 0x1B;
    public const ushort VkSpace = 0x20;
    public const ushort VkEnd = 0x23;
    public const ushort VkDown = 0x28;

    private const uint InputMouse = 0;
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint MouseLeftDown = 0x0002;
    private const uint MouseLeftUp = 0x0004;
    private const uint WmInputLangChangeRequest = 0x0050;
    private const uint WmImeControl = 0x0283;
    private const uint ImcSetConversionMode = 0x0002;
    private const uint ImcSetOpenStatus = 0x0006;
    private const uint ImcGetOpenStatus = 0x0005;
    private const uint KlfActivate = 0x00000001;

    public static IntPtr Foreground() => GetForegroundWindow();

    public static uint ClipboardSequence() => GetClipboardSequenceNumber();

    public static bool BringToFront(IntPtr hwnd)
    {
        SetForegroundWindow(hwnd);
        return GetForegroundWindow() == hwnd;
    }

    /// <summary>The window that holds keyboard focus on the foreground thread (the browser's own HWND).</summary>
    public static IntPtr FocusedWindow()
    {
        var fg = GetForegroundWindow();
        var thread = GetWindowThreadProcessId(fg, out _);
        var info = new GuiThreadInfo { cbSize = Marshal.SizeOf<GuiThreadInfo>() };
        return GetGUIThreadInfo(thread, ref info) ? info.hwndFocus : IntPtr.Zero;
    }

    public static (uint Pid, IntPtr Layout) LayoutOf(IntPtr hwnd)
    {
        var thread = GetWindowThreadProcessId(hwnd, out var pid);
        return (pid, GetKeyboardLayout(thread));
    }

    public static IntPtr[] LoadedLayouts()
    {
        var count = GetKeyboardLayoutList(0, null);
        var list = new IntPtr[count];
        GetKeyboardLayoutList(count, list);
        return list;
    }

    public static IntPtr LoadLayout(string klid) => LoadKeyboardLayoutW(klid, KlfActivate);

    public static bool UnloadLayout(IntPtr hkl) => UnloadKeyboardLayout(hkl);

    /// <summary>Ask the focused window's thread to switch layout — what the language bar does.</summary>
    public static void RequestLayout(IntPtr hwnd, IntPtr hkl) => PostMessageW(hwnd, WmInputLangChangeRequest, IntPtr.Zero, hkl);

    /// <summary>Open the IME and set hiragana conversion through the focused window's default IME window.</summary>
    public static (IntPtr ImeWindow, IntPtr OpenAfter) OpenIme(IntPtr hwnd, int conversionMode)
    {
        var ime = ImmGetDefaultIMEWnd(hwnd);
        if (ime == IntPtr.Zero)
        {
            return (IntPtr.Zero, IntPtr.Zero);
        }

        SendMessageW(ime, WmImeControl, (IntPtr)ImcSetOpenStatus, (IntPtr)1);
        SendMessageW(ime, WmImeControl, (IntPtr)ImcSetConversionMode, (IntPtr)conversionMode);
        return (ime, SendMessageW(ime, WmImeControl, (IntPtr)ImcGetOpenStatus, IntPtr.Zero));
    }

    public static void CloseIme(IntPtr imeWindow)
    {
        if (imeWindow != IntPtr.Zero)
        {
            SendMessageW(imeWindow, WmImeControl, (IntPtr)ImcSetOpenStatus, IntPtr.Zero);
        }
    }

    public static void Key(ushort vk) => Send(KeyInput(vk, false), KeyInput(vk, true));

    public static void Chord(ushort modifier, ushort vk) =>
        Send(KeyInput(modifier, false), KeyInput(vk, false), KeyInput(vk, true), KeyInput(modifier, true));

    public static void KeyDown(ushort vk) => Send(KeyInput(vk, false));

    public static void KeyUp(ushort vk) => Send(KeyInput(vk, true));

    /// <summary>A lower-case ASCII letter or digit as its virtual key — the path an IME sees.</summary>
    public static ushort VkFor(char c) => c switch
    {
        >= 'a' and <= 'z' => (ushort)char.ToUpperInvariant(c),
        >= '0' and <= '9' => c,
        _ => throw new ArgumentOutOfRangeException(nameof(c), c, "not a plain key"),
    };

    public static void TypeLetters(string text)
    {
        foreach (var c in text)
        {
            Key(VkFor(c));
        }
    }

    /// <summary>'@' on a US layout is Shift+2.</summary>
    public static void TypeAt() =>
        Send(KeyInput(0x10, false), KeyInput(0x32, false), KeyInput(0x32, true), KeyInput(0x10, true));

    public static void Click(int x, int y, bool withControl)
    {
        SetCursorPos(x, y);
        if (withControl)
        {
            Send(KeyInput(VkControl, false));
        }

        Send(MouseInput(MouseLeftDown), MouseInput(MouseLeftUp));
        if (withControl)
        {
            Send(KeyInput(VkControl, true));
        }
    }

    private static void Send(params Input[] inputs)
    {
        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (sent != inputs.Length)
        {
            throw new InvalidOperationException($"SendInput accepted {sent} of {inputs.Length} (error {Marshal.GetLastWin32Error()})");
        }
    }

    private static Input KeyInput(ushort vk, bool up) => new()
    {
        type = InputKeyboard,
        u = new InputUnion
        {
            ki = new KeybdInput
            {
                wVk = vk,
                wScan = (ushort)MapVirtualKeyW(vk, 0),
                dwFlags = up ? KeyEventKeyUp : 0,
            },
        },
    };

    private static Input MouseInput(uint flags) => new()
    {
        type = InputMouse,
        u = new InputUnion { mi = new MouseInputData { dwFlags = flags } },
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MouseInputData mi;
        [FieldOffset(0)] public KeybdInput ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInputData
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeybdInput
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public int cbSize;
        public uint flags;
        public IntPtr hwndActive;
        public IntPtr hwndFocus;
        public IntPtr hwndCapture;
        public IntPtr hwndMenuOwner;
        public IntPtr hwndMoveSize;
        public IntPtr hwndCaret;
        public Rect rcCaret;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int size);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKeyW(uint code, uint mapType);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);

    [DllImport("user32.dll")]
    private static extern bool GetGUIThreadInfo(uint thread, ref GuiThreadInfo info);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint thread);

    [DllImport("user32.dll")]
    private static extern int GetKeyboardLayoutList(int count, IntPtr[]? list);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadKeyboardLayoutW(string klid, uint flags);

    [DllImport("user32.dll")]
    private static extern bool UnloadKeyboardLayout(IntPtr hkl);

    [DllImport("user32.dll")]
    private static extern bool PostMessageW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessageW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();

    [DllImport("imm32.dll")]
    private static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hwnd);
}
