using System.Runtime.Versioning;
using AiDe.Tests.Shared;
using Xunit.Sdk;

namespace AiDe.Core.Tests;

/// <summary>
/// The census retries only its read of the process table, explains a read that never succeeds, and
/// never softens what a good read shows (DC-NEW-wl-t12-census).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ConsoleHostCensusTests
{
    private static readonly TimeSpan NoWait = TimeSpan.Zero;

    private const string OneHeadlessHostOwnedBy42 =
        """[{"ProcessId":7,"ParentProcessId":42,"Name":"conhost.exe","Created":"","CommandLine":"conhost.exe --headless --width 80"}]""";

    private static ConsoleHostCensus.QueryResult Failed(string stderr = "boom\nsecond line") => new(1, "", stderr);

    private static ConsoleHostCensus.QueryResult Good(string json) => new(0, json, "");

    [Fact]
    public void ATransientReadFailure_IsAbsorbedByTheRetry()
    {
        var calls = 0;
        var snapshot = ConsoleHostCensus.Take(() => ++calls == 1 ? Failed() : Good(OneHeadlessHostOwnedBy42), ConsoleHostCensus.MaxAttempts, NoWait);

        Assert.True(snapshot.Readable, snapshot.Cause);
        Assert.Equal(2, calls);
        Assert.Single(snapshot.HeadlessHostsOwnedBy(42));
    }

    [Fact]
    public void AGoodReadShowingALeakedHost_IsReturnedAtOnce_AndStillFailsTheLeakAssertion()
    {
        var calls = 0;
        var snapshot = ConsoleHostCensus.Take(() => { calls++; return Good(OneHeadlessHostOwnedBy42); }, ConsoleHostCensus.MaxAttempts, NoWait);

        Assert.Equal(1, calls);
        Assert.True(snapshot.Readable);
        Assert.Throws<EmptyException>(() => Assert.Empty(snapshot.HeadlessHostsOwnedBy(42)));
    }

    [Fact]
    public void APersistentExitFailure_IsUnreadable_AndNamesExitCodeAndFirstStderrLine()
    {
        var calls = 0;
        var snapshot = ConsoleHostCensus.Take(() => { calls++; return Failed(); }, ConsoleHostCensus.MaxAttempts, NoWait);

        Assert.False(snapshot.Readable);
        Assert.Equal(ConsoleHostCensus.MaxAttempts, calls);
        Assert.Contains("exit 1: boom", snapshot.Cause);
        Assert.DoesNotContain("second line", snapshot.Cause);
        Assert.Contains("NOT CHECKED", snapshot.NotChecked("while the helper ran"));
        Assert.Contains("exit 1: boom", snapshot.NotChecked("while the helper ran"));
    }

    [Fact]
    public void APersistentParseFailure_IsUnreadable_AndNamesTheParseError()
    {
        var snapshot = ConsoleHostCensus.Take(() => Good("not json"), ConsoleHostCensus.MaxAttempts, NoWait);

        Assert.False(snapshot.Readable);
        Assert.Contains("JSON parse error", snapshot.Cause);
    }

    [Fact]
    public void AStartFailure_IsRetried_AndNamed()
    {
        var snapshot = ConsoleHostCensus.Take(
            () => throw new System.ComponentModel.Win32Exception("cannot start"), ConsoleHostCensus.MaxAttempts, NoWait);

        Assert.False(snapshot.Readable);
        Assert.Contains("did not start: cannot start", snapshot.Cause);
    }
}
