using AiDe.Core.Watcher;

namespace AiDe.Core.Tests.Watcher;

/// <summary>
/// What the Daydream vertical produces against <b>this repository's own audit log</b>, not a fixture.
/// </summary>
/// <remarks>
/// <para><b>Why a test over the real corpus.</b> Every other test here builds the episodes it then
/// measures, so all of them pass in a world where the corpus is empty. The question this answers is
/// the one a fixture cannot: given what has actually been recorded here, can Daydream produce
/// anything at all?</para>
///
/// <para><b>Measured 2026-09-03 — 111 episodes scored, 7 clean, 103 carrying nothing to assess, ONE
/// observation recorded.</b> The vertical worked end to end. It had one row. And the recurrence
/// threshold is two distinct episodes, so one observation could never become a candidate: the output
/// over this repository's entire recorded history was zero, and would have been zero however good
/// the engine is.</para>
///
/// <para><b>Measured again 2026-09-05 — 120 scored, 15 clean, 103 still carrying nothing to assess,
/// TWO observations over two distinct episodes, ONE recurrence.</b> The threshold was crossed. The
/// engine did not change; capture did. Note what did <i>not</i> move: 103 episodes still carry
/// nothing to assess, so the instrumentation gap this file was written about is narrower, not
/// closed.</para>
///
/// <para><b>This test existed to expire, and it has.</b> A finding written only in prose is a memoir
/// (CI6), and a claim about what does NOT exist decays when someone else acts — with the author
/// absent and no reason to look (DC-094). So the claim was tied to something that failed the day it
/// stopped being true, and on 2026-09-05 it did. It was rewritten rather than deleted, because
/// deleting it would throw away the only place the transition is visible; the assertion now runs in
/// the opposite direction, and can only break if capture regresses.</para>
///
/// <para><b>It asserts a threshold relationship, never the measured numbers.</b> Pinning "103" would
/// go red on the next audit entry and be edited back without thought, which is a control that trains
/// people to ignore it.</para>
/// </remarks>
public sealed class WhatTheRealCorpusCanProduceTests
{
    /// <summary>Walks up for the repository root, so the test does not depend on the runner's cwd.</summary>
    private static string? FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "audit", "audit-log.jsonl")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    /// <remarks>
    /// Carries the observations themselves, not just their count. The count answered the question
    /// this file asked while the corpus was too thin to recur; now that it does recur, the only
    /// honest way to assert a candidate appears is to run the detector over what was actually
    /// recorded.
    /// </remarks>
    private sealed record Measurement(
        int Scored, DaydreamReach Reach, IReadOnlyList<DaydreamObservation> Observations);

    private static Measurement Measure()
    {
        var root = FindRepositoryRoot();

        // Loud, not skipped. A test that silently passes when it cannot find its subject is an
        // absence rendered as success (DC-025) — in the one test whose whole purpose is measuring
        // an absence.
        Assert.True(root is not null, "could not locate the repository root from " + AppContext.BaseDirectory);

        var scratch = Path.Combine(Path.GetTempPath(), "aide-corpus-" + Guid.NewGuid().ToString("n")[..8]);
        Directory.CreateDirectory(scratch);

        try
        {
            using var host = WatcherHost.Open(scratch, Path.Combine(scratch, "coord"));

            // The record goes in the SCRATCH directory, never in the repository. A test must not
            // write this repository's own docs/daydream/, or running the suite would author
            // committed content as a side effect.
            var record = DaydreamRepositoryRecord.For(scratch);

            var scored = host.ImportAndScoreEpisodesFromAuditLog(
                Path.Combine(root!, "docs", "audit", "audit-log.jsonl"),
                WorkspaceKey.From(root!),
                daydream: new DaydreamRecorder(record));

            return new Measurement(
                scored,
                new DaydreamReachProbe(host.Store, record).Probe(),
                record.Read().Observations);
        }
        finally
        {
            try { Directory.Delete(scratch, recursive: true); } catch (IOException) { /* best effort */ }
        }
    }

    /// <summary>
    /// The import path works against real data — episodes are read and scored.
    /// </summary>
    /// <remarks>
    /// The half worth confirming before drawing any conclusion from the rest: a zero here would mean
    /// the reader is broken, which is a completely different finding from "there is nothing to read".
    /// </remarks>
    [Fact]
    public void TheRealAuditLogYieldsScoredEpisodes()
    {
        var m = Measure();

        Assert.True(m.Scored > 0, "the real audit log produced no scored episodes at all");
        Assert.Equal(m.Scored, m.Reach.EpisodesScored);
    }

    /// <summary>
    /// The corpus can produce a candidate — the transition this test was written to catch.
    /// </summary>
    /// <remarks>
    /// <para><b>It expired on 2026-09-05, which was the point.</b> This assertion used to read
    /// <c>Observations &lt; 2</c> and carried an instruction: when it goes red, nothing is broken —
    /// enough turns have recorded their evidence for a second observable episode to exist, so
    /// rewrite it to assert that a candidate appears, and do not delete it. This is that
    /// rewrite.</para>
    ///
    /// <para><b>Measured at the transition — 120 episodes scored, 15 clean, 103 still carrying
    /// nothing to assess, TWO observations over two distinct episodes, ONE recurrence</b> (task
    /// class <c>audit-import</c>, verdict <c>Blocked</c>, correctness floor tripped). Daydream
    /// proposed something for the first time over this repository's real history.</para>
    ///
    /// <para><b>The direction of the claim reversed, and with it the direction of the risk.</b> The
    /// old assertion was doomed to expire, because observations only accumulate. This one can only
    /// break by <i>regression</i>: capture would have to stop, or the detector would have to stop
    /// grouping what it already groups. So it is no longer a tripwire waiting to fire — it is the
    /// floor under the capture obligation that <c>tools/verify-capture-instruction.py</c> enforces
    /// on the instruction side, asserted here against the corpus that obligation produces.</para>
    ///
    /// <para><b>Still a threshold relationship, never the measured numbers</b> (the rule the rest of
    /// this file follows). Pinning "1 recurrence" or "2 observations" would go red on the next
    /// episode that recurs and be edited back without thought.</para>
    /// </remarks>
    [Fact]
    public void TheRealCorpusProducesACandidate()
    {
        var m = Measure();

        // Named separately from the recurrence below, because the two failures mean opposite
        // things: too few observations is a capture regression, while observations that no longer
        // group is a detector regression, and the fix for those two is not the same.
        Assert.True(
            m.Observations.Count >= 2,
            $"only {m.Observations.Count} observation(s) were recorded — the corpus has fallen back "
            + "below the recurrence threshold, so Daydream can propose nothing again. Capture "
            + "regressed; the engine did not.");

        var recurring = new RecurrenceDetector().Recurring(m.Observations);

        Assert.True(
            recurring.Count > 0,
            $"{m.Observations.Count} observation(s) were recorded but none recur — every one is a "
            + "distinct signature, so nothing can be generalised yet.");

        // The two properties that make a candidate honest rather than merely present: it is
        // evidenced by genuinely distinct episodes (never one episode counted twice, which is the
        // cheapest way to manufacture confidence from a single event), and it describes something
        // that actually went wrong (a clean episode recurring is "work went well" — true,
        // recurrent, and useless as a lesson).
        Assert.All(recurring, r => Assert.True(
            r.DistinctEpisodes >= 2,
            $"recurrence {r.Signature} claims {r.DistinctEpisodes} distinct episode(s)"));

        Assert.All(recurring, r => Assert.False(
            r.Signature.IsUnremarkable,
            $"recurrence {r.Signature} is unremarkable and should never have been proposed"));
    }

    /// <summary>
    /// And the surface says WHY it is empty, rather than "no patterns observed yet".
    /// </summary>
    /// <remarks>
    /// The reason the probe exists. Against the real corpus the honest reading is that most turns
    /// recorded nothing assessable — not that the work went well — and the finding has to carry that
    /// or a permanently quiet Daydream reads as a healthy repository (DC-025).
    /// </remarks>
    [Fact]
    public void TheProbeExplainsTheRealCorpusRatherThanReportingSilence()
    {
        var m = Measure();

        Assert.True(m.Reach.NothingWasAssessed > 0, "expected unassessed episodes in the real corpus");
        Assert.NotNull(m.Reach.Finding);
        Assert.Contains("carried nothing to assess", m.Reach.Finding);
    }

    /// <summary>
    /// The gap is in capture, not in reading — checked rather than concluded.
    /// </summary>
    /// <remarks>
    /// <c>docs/proof/</c> exists and holds real Proof Packs, and some episodes ARE assessed. So the
    /// reader finds what is there; most turns simply never recorded evidence in their audit entry.
    /// Without this, "103 carried nothing to assess" would be equally consistent with a broken
    /// reader, and the fix for those two is opposite.
    /// </remarks>
    [Fact]
    public void SomeEpisodesAreAssessed_SoTheReaderIsNotTheProblem()
    {
        var m = Measure();

        Assert.True(
            m.Reach.NothingWentWrong + m.Reach.WouldRecord > 0,
            "no episode was assessed at all — that would point at the reader, not at capture");
    }
}
