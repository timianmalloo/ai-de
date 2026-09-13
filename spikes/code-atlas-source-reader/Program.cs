namespace CodeAtlas.SourceReaderProbe;

internal static class Program
{
    public static int Main()
    {
        var report = SourceReaderProbeCases.RunAll();
        Console.Write(report);
        return report.Contains("FAIL|", StringComparison.Ordinal) ? 1 : 0;
    }
}
