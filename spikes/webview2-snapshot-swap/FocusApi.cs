using System.Reflection;
using Microsoft.Web.WebView2.Wpf;

namespace Webview2SnapshotSwapSpike;

/// <summary>
/// What focus API the WPF <c>WebView2</c> control actually exposes.
/// </summary>
/// <remarks>
/// Spike S4 measured that <c>Focus()</c> is refused and Tab never reaches the canvas, which makes
/// focus routing a design obligation. The obvious answer — "drive
/// <c>CoreWebView2Controller.MoveFocus</c>" — assumes the controller is reachable from the WPF
/// control, and the assembly's string table is not evidence of that: a name can appear because the
/// control uses it internally.
///
/// So the public surface is enumerated rather than recalled. This is the cheap version of the
/// standing rule that a dependency's shape is opened, not remembered.
/// </remarks>
internal static class FocusApi
{
    internal static void Report()
    {
        var type = typeof(WebView2);
        Console.WriteLine($"  type: {type.FullName}");
        Console.WriteLine($"  base: {type.BaseType?.FullName}");
        Console.WriteLine();

        var members = type
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name.Contains("Focus", StringComparison.OrdinalIgnoreCase)
                || m.Name.Contains("Controller", StringComparison.OrdinalIgnoreCase)
                || m.Name.Contains("Accelerator", StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.Name)
            .ToList();

        Console.WriteLine($"  PUBLIC focus/controller members declared on the WPF control: {members.Count}");
        foreach (var member in members)
        {
            Console.WriteLine($"    {member.MemberType,-8} {Describe(member)}");
        }

        // The controller specifically: if it is not public, MoveFocus cannot be called directly and
        // the design needs a different route into the canvas.
        var controller = type.GetProperty("CoreWebView2Controller",
            BindingFlags.Public | BindingFlags.Instance);
        var anyController = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.PropertyType.Name.Contains("Controller", StringComparison.Ordinal));

        Console.WriteLine();
        Console.WriteLine($"  public CoreWebView2Controller property: "
            + $"{(controller is not null ? "YES" : "no")}");
        Console.WriteLine($"  any public property of a Controller type: "
            + $"{(anyController is not null ? anyController.Name + " : " + anyController.PropertyType.Name : "no")}");

        var nonPublic = type
            .GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.PropertyType.Name.Contains("Controller", StringComparison.Ordinal))
            .Select(p => $"{p.Name} : {p.PropertyType.Name} ({(p.GetMethod?.IsAssembly == true ? "internal" : "private")})")
            .ToList();
        Console.WriteLine($"  non-public controller properties: "
            + (nonPublic.Count == 0 ? "none" : string.Join(", ", nonPublic)));
    }

    private static string Describe(MemberInfo member) => member switch
    {
        PropertyInfo p => $"{p.Name} : {p.PropertyType.Name}",
        EventInfo e => $"{e.Name} : {e.EventHandlerType?.Name}",
        MethodInfo m => $"{m.Name}({string.Join(", ", m.GetParameters().Select(x => x.ParameterType.Name))})"
            + $" : {m.ReturnType.Name}",
        _ => member.Name,
    };
}

/// <summary>
/// Whether the Win32 route into the hosted browser works, since the managed one does not exist.
/// </summary>
/// <remarks>
/// The WPF control exposes no controller, so <c>CoreWebView2Controller.MoveFocus</c> — the documented
/// way to hand focus to web content — is unreachable. What remains is that <c>WebView2</c> derives
/// from <c>HwndHost</c> and therefore has a real child window handle. Focusing it is a Win32 call.
///
/// This reads the result back with <c>GetFocus</c> rather than trusting the call's return value,
/// because <c>SetFocus</c> returns the PREVIOUSLY focused window and a null return is ambiguous
/// between "failed" and "nothing had focus" (E13/E14: an exit code is not a result).
/// </remarks>
internal static class Win32Focus
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetFocus();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    internal static void Probe(System.Windows.Interop.HwndHost host)
    {
        var handle = host.Handle;
        Console.WriteLine($"  HwndHost.Handle        : 0x{handle:X}");
        if (handle == IntPtr.Zero)
        {
            Console.WriteLine("  x   no child handle - the Win32 route is unavailable too.");
            return;
        }

        var before = GetFocus();
        SetFocus(handle);
        var after = GetFocus();

        Console.WriteLine($"  focus before SetFocus  : 0x{before:X}");
        Console.WriteLine($"  focus after SetFocus   : 0x{after:X}");
        Console.WriteLine($"  parent of focused hwnd : 0x{GetParent(after):X}");

        var landed = after == handle;
        var landedInside = !landed && after != IntPtr.Zero && IsDescendantOf(after, handle);

        if (landed)
        {
            Console.WriteLine("  OK  focus is on the host window itself.");
        }
        else if (landedInside)
        {
            Console.WriteLine("  OK  focus landed on a CHILD of the host - the browser's own input window.");
            Console.WriteLine("      That is the outcome that matters: keystrokes reach the page.");
        }
        else
        {
            Console.WriteLine("  x   SetFocus did not move focus into the hosted browser.");
        }
    }

    private static bool IsDescendantOf(IntPtr candidate, IntPtr ancestor)
    {
        for (var current = candidate; current != IntPtr.Zero; current = GetParent(current))
        {
            if (current == ancestor)
            {
                return true;
            }
        }

        return false;
    }
}
