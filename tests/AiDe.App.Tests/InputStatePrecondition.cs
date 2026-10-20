using System.Runtime.InteropServices;
using System.Windows.Input;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AiDe.App.Tests;

/// <summary>The machine-wide input state a UI test's result can depend on, read through one seam so a test of the precondition can inject it.</summary>
public interface IInputStateSource
{
    /// <summary>WPF's own view of the left button — what <c>ButtonBase.OnKeyUp</c> reads. This decides.</summary>
    bool WpfLeftButtonDown { get; }

    /// <summary><c>GetAsyncKeyState(VK_LBUTTON)</c>: evidence beside the deciding reading, never the decision.</summary>
    bool AsyncLeftButtonDown { get; }

    /// <summary><c>GetForegroundWindow()</c>.</summary>
    IntPtr Foreground { get; }
}

/// <summary>The real desktop, read and never set.</summary>
public sealed class DesktopInputState : IInputStateSource
{
    public static readonly DesktopInputState Instance = new();

    public bool WpfLeftButtonDown => Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed;

    public bool AsyncLeftButtonDown => (GetAsyncKeyState(0x01) & 0x8000) != 0;

    public IntPtr Foreground => GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}

/// <summary>Thrown by a precondition that is not met. A <see cref="CheckedFactAttribute"/> test ends Skipped with this message as its reason.</summary>
public sealed class NotCheckedException(string message) : XunitException(message);

/// <summary>Preconditions on machine-wide input state: not default means NOT CHECKED (a skip), never a failure and never a pass.</summary>
public static class InputPreconditions
{
    public static void RequireLeftButtonReleased(IInputStateSource? source = null)
    {
    }

    public static void RequireForeground(IntPtr window, string what, IInputStateSource? source = null)
    {
    }
}

/// <summary>A <c>[Fact]</c> whose <see cref="NotCheckedException"/> ends the test Skipped at run time (xUnit 2.9.3 has no runtime skip of its own).</summary>
[XunitTestCaseDiscoverer("AiDe.App.Tests.CheckedFactDiscoverer", "AiDe.App.Tests")]
public sealed class CheckedFactAttribute : FactAttribute;

public sealed class CheckedFactDiscoverer(IMessageSink diagnosticMessageSink) : IXunitTestCaseDiscoverer
{
    public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        yield return new CheckedTestCase(
            diagnosticMessageSink,
            discoveryOptions.MethodDisplayOrDefault(),
            discoveryOptions.MethodDisplayOptionsOrDefault(),
            testMethod);
    }
}

public sealed class CheckedTestCase : XunitTestCase
{
    [Obsolete("Called by the de-serializer", error: true)]
    public CheckedTestCase()
    {
    }

    public CheckedTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod)
        : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod)
    {
    }

    public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource) =>
        base.RunAsync(diagnosticMessageSink, new NotCheckedBus(messageBus), constructorArguments, aggregator, cancellationTokenSource);
}

/// <summary>Re-queues a failure whose exception is <see cref="NotCheckedException"/> as a skip with its message as the reason; every other message passes through.</summary>
public sealed class NotCheckedBus(IMessageBus inner) : IMessageBus
{
    public bool QueueMessage(IMessageSinkMessage message) => inner.QueueMessage(message);

    public void Dispose() => inner.Dispose();
}
