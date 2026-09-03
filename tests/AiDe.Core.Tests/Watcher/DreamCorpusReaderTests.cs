using AiDe.Core.Watcher;

namespace AiDe.Core.Tests.Watcher;

/// <summary>
/// D5 — the inbound half of the Dream seam: what the offline pass has already promoted, read back
/// so Daydream stops re-proposing it.
/// </summary>
/// <remarks>
/// <para><b>The spike that shaped this.</b> The seam design proposed emitting candidates into an
/// inbox <c>dream.py</c> would read, and labelled that <b>Inferred</b> pending a spike. The spike
/// read <c>load_corpus</c> and found five FIXED paths, no discovery, and no extension point on
/// <c>cmd_run</c>. The emit direction as designed would have written a file nothing reads — DC-089
/// built deliberately — so the design was revised and only this half survives.</para>
///
/// <para><b>Read-only and detected.</b> Nothing here invokes <c>dream.py</c>. Shelling out would
/// make Python and a vendored pack a runtime dependency of the product, which is the inversion the
/// design exists to refuse.</para>
/// </remarks>
public sealed class DreamCorpusReaderTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "aide-corpus-" + Guid.NewGuid().ToString("n")[..8]);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { /* best effort */ }
    }

    private void WriteRegister(string body)
    {
        var dir = Path.Combine(_root, "docs", "lessons");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "defect-classes.md"), body);
    }

    private void WriteMitigations(string body)
    {
        var dir = Path.Combine(_root, "docs", "lessons");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "mitigations.jsonl"), body);
    }

    private static DaydreamSignature Signature(string floors, string shortfalls = "") =>
        new("implement", ScoreSchema.Weave1Version, WeaveVerdict.Blocked, floors, shortfalls);

    /// <summary>
    /// A repository without the pack is an absence, not an empty corpus.
    /// </summary>
    /// <remarks>
    /// The normal case for most repositories. Reporting it as "found, and it contains nothing" would
    /// let a caller conclude that nothing has ever been promoted here — a claim about the repository
    /// rather than about what was looked for.
    /// </remarks>
    [Fact]
    public void ARepositoryWithoutThePackReportsAbsenceRatherThanAnEmptyCorpus()
    {
        Directory.CreateDirectory(_root);

        var corpus = DreamCorpusReader.Read(_root);

        Assert.False(corpus.Present);
        Assert.Contains("not recorded", corpus.Source);
        Assert.Empty(corpus.KnownLearnings);
    }

    [Fact]
    public void AMissingOrEmptyRootIsAbsentRatherThanThrowing()
    {
        Assert.False(DreamCorpusReader.Read(null).Present);
        Assert.False(DreamCorpusReader.Read("").Present);
        Assert.False(DreamCorpusReader.Read(Path.Combine(_root, "nope")).Present);
    }

    [Fact]
    public void TheRegisterIsReadAndNamedAsTheSource()
    {
        WriteRegister("""
            # Defect classes

            ### DC-042 — A launcher omits an identity and a guard degrades to advisory
            - **Shape:** …

            ### DC-043 — Something else entirely
            - **Shape:** …
            """);

        var corpus = DreamCorpusReader.Read(_root);

        Assert.True(corpus.Present);
        Assert.Equal(2, corpus.KnownLearnings.Count);
        Assert.Contains("defect-classes.md", corpus.Source);
    }

    [Fact]
    public void MitigationsAreReadAndBothSourcesAreNamed()
    {
        WriteRegister("### DC-001 — A registered class\n");
        WriteMitigations(
            """{"id":"mit-0001","class":"DC-001","summary":"a proven Correctness fix"}"""
            + "\n"
            + """{"id":"mit-0002","class":"DC-002","summary":"another"}"""
            + "\n");

        var corpus = DreamCorpusReader.Read(_root);

        Assert.True(corpus.Present);
        Assert.Equal(3, corpus.KnownLearnings.Count);
        Assert.Contains("defect-classes.md", corpus.Source);
        Assert.Contains("mitigations.jsonl", corpus.Source);
    }

    /// <summary>A hand edit in someone else's file must not stop the rest being read.</summary>
    [Fact]
    public void AMalformedMitigationLineIsSkippedNotFatal()
    {
        WriteMitigations(
            "not json at all\n"
            + """{"id":"mit-0001","class":"DC-001","summary":"a proven Correctness fix"}"""
            + "\n{ broken\n");

        var corpus = DreamCorpusReader.Read(_root);

        Assert.True(corpus.Present);
        Assert.Single(corpus.KnownLearnings);
    }

    // ---------------------------------------------------------------- suppression

    [Fact]
    public void AKnownLearningSuppressesItsCandidate()
    {
        WriteRegister("### DC-001 — Correctness floors trip on unrun verification\n");

        Assert.True(DreamCorpusReader.Read(_root).AlreadyKnown(Signature("Correctness")));
    }

    /// <summary>
    /// Every term must match, not any.
    /// </summary>
    /// <remarks>
    /// One shared word between a floor name and a paragraph of prose is a coincidence, and
    /// suppressing a candidate on a coincidence hides the thing being proposed — which is worse than
    /// proposing something twice, because the second is visible and the first is not.
    /// </remarks>
    [Fact]
    public void APartialMatchDoesNotSuppress()
    {
        WriteRegister("### DC-001 — Correctness floors trip on unrun verification\n");

        var corpus = DreamCorpusReader.Read(_root);

        Assert.False(corpus.AlreadyKnown(Signature("Correctness+Security")));
    }

    /// <summary>
    /// With no corpus, nothing is ever suppressed.
    /// </summary>
    /// <remarks>
    /// The guarantee the absent case has to keep. A reader that suppressed on an absent corpus would
    /// hide candidates because a file was missing, which is the worst possible reading of "not
    /// recorded".
    /// </remarks>
    [Fact]
    public void AnAbsentCorpusSuppressesNothing()
    {
        Directory.CreateDirectory(_root);

        Assert.False(DreamCorpusReader.Read(_root).AlreadyKnown(Signature("Correctness")));
        Assert.False(DreamCorpus.Absent.AlreadyKnown(Signature("Correctness")));
    }

    /// <summary>A signature with nothing to match on never suppresses.</summary>
    /// <remarks>
    /// An unremarkable pattern has no floors and no shortfalls, so there are no terms — and matching
    /// on an empty term list would suppress <i>everything</i> against any corpus at all.
    /// </remarks>
    [Fact]
    public void ASignatureWithNoTermsNeverSuppresses()
    {
        WriteRegister("### DC-001 — Anything at all\n");

        Assert.False(DreamCorpusReader.Read(_root).AlreadyKnown(Signature(string.Empty)));
    }
}
