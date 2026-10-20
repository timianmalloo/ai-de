using Xunit.Abstractions;
using Xunit.Sdk;

namespace AiDe.App.Tests;

/// <summary>
/// The precondition for UI tests that depend on machine-wide input state (DC-NEW-wl-t33-input-state): the state is read, never set;
/// a non-default state ends the test NOT CHECKED (Skipped, with the reading as the reason), and the default lets the test's own
/// assertions run. The desktop is replaced by <see cref="IInputStateSource"/> here, so nothing in these tests touches the real input state.
/// </summary>
public sealed class InputStatePreconditionTests
{
    private static readonly IntPtr Window = new(0x1234);

    private sealed record Source(bool WpfDown = false, bool AsyncDown = false, IntPtr Foreground = default) : IInputStateSource
    {
        public bool WpfLeftButtonDown => WpfDown;

        public bool AsyncLeftButtonDown => AsyncDown;
    }

    [Fact]
    public void ALeftButtonThatWpfReadsDown_IsNotChecked_WithBothReadingsInTheReason()
    {
        var thrown = Assert.Throws<NotCheckedException>(() => InputPreconditions.RequireLeftButtonReleased(new Source(WpfDown: true, AsyncDown: true)));

        Assert.StartsWith("NOT CHECKED:", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("left mouse button", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("Mouse.PrimaryDevice.LeftButton=Pressed", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("GetAsyncKeyState(VK_LBUTTON)=down", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ALeftButtonThatWpfReadsReleased_IsChecked_EvenWhenTheAsyncReadingSaysDown()
    {
        InputPreconditions.RequireLeftButtonReleased(new Source(WpfDown: false, AsyncDown: true));
        InputPreconditions.RequireLeftButtonReleased(new Source());
    }

    [Fact]
    public void AForegroundThatIsNotTheTestsWindow_IsNotChecked_NamingBothWindows()
    {
        var thrown = Assert.Throws<NotCheckedException>(() => InputPreconditions.RequireForeground(Window, "SendInput", new Source(Foreground: new IntPtr(0x99))));

        Assert.StartsWith("NOT CHECKED:", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("foreground window", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("0x99", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("0x1234", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("SendInput", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AForegroundThatIsTheTestsWindow_IsChecked()
    {
        InputPreconditions.RequireForeground(Window, "SendInput", new Source(Foreground: Window));
    }

    [Fact]
    public void ANotCheckedFailure_IsRequeuedAsASkipCarryingItsReason_AndEveryOtherMessagePassesThrough()
    {
        var recorded = new List<IMessageSinkMessage>();
        using var bus = new NotCheckedBus(new RecordingBus(recorded));
        var test = RealTest();

        bus.QueueMessage(new TestFailed(test, 0m, string.Empty, new NotCheckedException("NOT CHECKED: reason")));
        bus.QueueMessage(new TestFailed(test, 0m, string.Empty, new XunitException("a real failure")));
        bus.QueueMessage(new TestPassed(test, 0m, string.Empty));

        Assert.Collection(
            recorded,
            first => Assert.Equal("NOT CHECKED: reason", Assert.IsAssignableFrom<ITestSkipped>(first).Reason),
            second => Assert.IsAssignableFrom<ITestFailed>(second),
            third => Assert.IsAssignableFrom<ITestPassed>(third));
    }

    private sealed class RecordingBus(List<IMessageSinkMessage> sink) : IMessageBus
    {
        public bool QueueMessage(IMessageSinkMessage message)
        {
            sink.Add(message);
            return true;
        }

        public void Dispose()
        {
        }
    }

    /// <summary>A real xUnit test of this very method, so the message types find the collection and method they read.</summary>
    private static XunitTest RealTest()
    {
        var type = typeof(InputStatePreconditionTests);
        var collection = new TestCollection(new TestAssembly(Reflector.Wrap(type.Assembly)), null, "input-state-precondition");
        var method = new TestMethod(new TestClass(collection, Reflector.Wrap(type)), Reflector.Wrap(type.GetMethod(nameof(AForegroundThatIsTheTestsWindow_IsChecked))!));
        var testCase = new XunitTestCase(new NullMessageSink(), TestMethodDisplay.ClassAndMethod, TestMethodDisplayOptions.None, method);
        return new XunitTest(testCase, "input-state-precondition");
    }
}
