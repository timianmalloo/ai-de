using System.Text.Json;
using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// A crash leaves a record naming what failed and where.
/// </summary>
/// <remarks>
/// <para><b>The gap this closes.</b> The shell crashed on "New Claude Code session" and left nothing
/// to read: no Windows Error Reporting entry, no Application event-log record, and nothing in the
/// workbench log — which recorded only layout mutations, and does not log agent-terminal creation at
/// all. The available evidence was "the .exe crashed", and the investigation had to proceed from a
/// screenshot of the terminal pane.</para>
///
/// <para><b>A crash is the moment the product knows the most and says the least.</b> Recording it
/// changes nothing about whether the process dies — <c>e.Handled</c> stays false deliberately — only
/// whether the next person has a stack trace or a shrug.</para>
///
/// <para><b>Why the inner exception is asserted separately.</b> A fault that arrives wrapped reports
/// the wrapper's type and message; reporting only those is how a real defect reads as an
/// infrastructure complaint, which is DC-078 pointed at a crash record. The one place it matters
/// most is the one place it was missing.</para>
/// </remarks>
public sealed class CrashesAreRecordedTests
{
    /// <summary>Captures what the diagnostics would have written, without touching the log file.</summary>
    private static List<JsonElement> Capture(Action body)
    {
        var lines = new List<string>();
        var previous = WorkbenchDiagnostics.Sink;

        WorkbenchDiagnostics.Sink = lines.Add;

        try { body(); }
        finally { WorkbenchDiagnostics.Sink = previous; }

        return [.. lines.Select(l => JsonDocument.Parse(l).RootElement)];
    }

    private static string? Text(JsonElement record, string property) =>
        record.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    [Fact]
    public void ACrashIsRecordedWithItsOriginAndType()
    {
        var records = Capture(() =>
            WorkbenchDiagnostics.Crash("dispatcher", new InvalidOperationException("a surface appears in more than one zone")));

        var crash = Assert.Single(records);

        Assert.Equal("crash", Text(crash, "evt"));
        Assert.Equal("dispatcher", Text(crash, "origin"));
        Assert.Equal(typeof(InvalidOperationException).FullName, Text(crash, "type"));
        Assert.Equal("a surface appears in more than one zone", Text(crash, "message"));
    }

    [Fact]
    public void TheStackTraceIsRecordedAndNotJustTheMessage()
    {
        // A message alone names what went wrong and never where. The whole reason this exists is to
        // turn "the .exe crashed" into a line number.
        var records = Capture(() =>
        {
            try
            {
                throw new InvalidOperationException("boom");
            }
            catch (InvalidOperationException ex)
            {
                WorkbenchDiagnostics.Crash("dispatcher", ex);
            }
        });

        var stack = Text(Assert.Single(records), "stack");

        Assert.NotNull(stack);
        Assert.Contains(nameof(TheStackTraceIsRecordedAndNotJustTheMessage), stack, StringComparison.Ordinal);
    }

    [Fact]
    public void AWrappedFaultReportsTheInnerExceptionToo()
    {
        // The DC-078 case: an outer wrapper describing the environment, carrying the real defect.
        // Recording only the outer type sends the reader to re-run rather than to investigate.
        var records = Capture(() => WorkbenchDiagnostics.Crash(
            "task",
            new AggregateException("one or more errors occurred", new KeyNotFoundException("zone 'Right' is missing"))));

        var crash = Assert.Single(records);

        Assert.Equal(typeof(KeyNotFoundException).FullName, Text(crash, "inner"));
        Assert.Equal("zone 'Right' is missing", Text(crash, "innerMessage"));
    }

    [Fact]
    public void AllThreeCrashRoutesAreWired()
    {
        // The DC-016 guard, and the reason it is worth having: wiring only the dispatcher would pass
        // every test above while leaving background threads and discarded tasks silent — two of the
        // three ways this app can actually die, and the two that produce no window to see.
        var source = System.IO.File.ReadAllText(AppXamlPath());

        Assert.Contains("DispatcherUnhandledException", source, StringComparison.Ordinal);
        Assert.Contains("AppDomain.CurrentDomain.UnhandledException", source, StringComparison.Ordinal);
        Assert.Contains("UnobservedTaskException", source, StringComparison.Ordinal);
    }

    private static string AppXamlPath()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);

        while (here is not null && !File.Exists(Path.Combine(here.FullName, "AiDe.sln")))
        {
            here = here.Parent;
        }

        Assert.NotNull(here);

        var path = Path.Combine(here!.FullName, "src", "AiDe.App", "App.xaml.cs");
        Assert.True(File.Exists(path), $"App.xaml.cs was not found at {path}");

        return path;
    }
}
