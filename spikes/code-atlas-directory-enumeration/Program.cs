namespace CodeAtlas.DirectoryEnumerationProbe;

internal static class Program
{
    public static int Main()
    {
        var report = DirectoryEnumerationProbeCases.RunAll();
        Console.Write(report);
        return report.Contains("FAIL|", StringComparison.Ordinal) ? 1 : 0;
    }
}
