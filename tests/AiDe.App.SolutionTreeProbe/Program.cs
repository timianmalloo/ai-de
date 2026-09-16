using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.Core.Projections;

namespace AiDe.App.SolutionTreeProbe;

/// <summary>Out-of-process Ctrl+Enter on the Solution tree. Exit 0 = Reveal in graph fired.</summary>
internal static class Program
{
    private const int Ok = 0;
    private const int ForegroundNotHeld = 2;
    private const int ActivateWasSource = 3;
    private const int NoActivate = 4;
    private const int FileNotSelected = 5;
    private const int Crashed = 6;
    private const int TimedOut = 7;

    private const ushort VkControl = 0x11;
    private const ushort VkReturn = 0x0D;
    private const uint KeyeventfKeyup = 0x0002;

    [STAThread]
    private static int Main()
    {
        try
        {
            return Run();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return Crashed;
        }
    }

    private static int Run()
    {
        var surface = new SolutionTreeSurface();
        surface.Show(new SolutionTreeResult(
            [
                new SolutionTreeNode("", SolutionTreeNodeKind.CensusFolder, CensusFolderCoverage.IndexedParent, null, null),
                new SolutionTreeNode("Program.cs", SolutionTreeNodeKind.FileArtifact, null, "Program", "class"),
            ],
            SkipListedDirectoriesOmitted: 0,
            OmittedByCap: 0,
            [],
            "rev-chord"));

        SolutionTreeActivate? seen = null;
        surface.ActivateRequested += (_, a) => seen = a;

        var window = new Window
        {
            Title = "AiDe solution-tree chord probe",
            Content = surface,
            Width = 480,
            Height = 360,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = 80,
            Top = 80,
            Topmost = true,
        };

        var result = TimedOut;
        window.Loaded += (_, _) =>
        {
            result = Probe(window, surface, () => seen);
            window.Close();
        };

        window.Show();
        var hwnd = new WindowInteropHelper(window).Handle;
        SetForegroundWindow(hwnd);

        var frame = new DispatcherFrame();
        window.Closed += (_, _) => frame.Continue = false;
        var guard = new DispatcherTimer(
            TimeSpan.FromSeconds(20),
            DispatcherPriority.Normal,
            (_, _) => { frame.Continue = false; },
            Dispatcher.CurrentDispatcher);
        guard.Start();
        Dispatcher.PushFrame(frame);
        guard.Stop();
        return result;
    }

    private static int Probe(Window window, SolutionTreeSurface surface, Func<SolutionTreeActivate?> seen)
    {
        window.Activate();
        var hwnd = new WindowInteropHelper(window).Handle;
        SetForegroundWindow(hwnd);
        surface.Measure(new Size(480, 360));
        surface.Arrange(new Rect(0, 0, 480, 360));
        surface.UpdateLayout();
        Expand(surface.Tree);

        var file = FindFile(surface.Tree);
        if (file is null)
        {
            Console.Error.WriteLine("file-artifact container was not generated");
            return FileNotSelected;
        }

        file.IsSelected = true;
        file.Focus();
        Keyboard.Focus(surface.Tree);
        window.UpdateLayout();
        Pump();

        if (GetForegroundWindow() != hwnd)
        {
            Console.Error.WriteLine("foreground not held; SendInput would prove another window");
            return ForegroundNotHeld;
        }

        SendChord();
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(3);
        while (DateTime.UtcNow < deadline && seen() is null)
        {
            Pump();
        }

        var activate = seen();
        if (activate is null)
        {
            Console.Error.WriteLine("no activate after Ctrl+Enter");
            return NoActivate;
        }

        if (activate.Kind != NodeViewKind.GraphNeighbourhood)
        {
            Console.Error.WriteLine($"activate was {activate.Kind}, not GraphNeighbourhood");
            return ActivateWasSource;
        }

        Console.Out.WriteLine("ctrl+enter reveal");
        return Ok;
    }

    private static void SendChord()
    {
        var inputs = new INPUT[4];
        inputs[0] = Key(VkControl, 0);
        inputs[1] = Key(VkReturn, 0);
        inputs[2] = Key(VkReturn, KeyeventfKeyup);
        inputs[3] = Key(VkControl, KeyeventfKeyup);
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT Key(ushort vk, uint flags) => new()
    {
        type = 1,
        u = new InputUnion { ki = new KEYBDINPUT { wVk = vk, dwFlags = flags } },
    };

    private static void Expand(ItemsControl parent)
    {
        parent.ApplyTemplate();
        parent.UpdateLayout();
        for (var i = 0; i < parent.Items.Count; i++)
        {
            if (parent.ItemContainerGenerator.ContainerFromIndex(i) is not TreeViewItem item)
            {
                continue;
            }

            item.IsExpanded = true;
            item.UpdateLayout();
            Expand(item);
        }
    }

    private static TreeViewItem? FindFile(ItemsControl parent)
    {
        parent.ApplyTemplate();
        parent.UpdateLayout();
        for (var i = 0; i < parent.Items.Count; i++)
        {
            if (parent.ItemContainerGenerator.ContainerFromIndex(i) is not TreeViewItem item)
            {
                continue;
            }

            if (item.DataContext is SolutionTreeNodeItem vm
                && vm.Node.Kind == SolutionTreeNodeKind.FileArtifact)
            {
                return item;
            }

            item.IsExpanded = true;
            item.UpdateLayout();
            var nested = FindFile(item);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static void Pump()
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        Thread.Sleep(20);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
