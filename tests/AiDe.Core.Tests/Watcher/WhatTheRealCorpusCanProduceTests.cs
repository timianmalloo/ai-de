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
/// observation recorded.</b> The vertical works end to end. It has one row. And the recurrence
/// threshold is two distinct episodes, so one observation can never become a candidate: the output
/// over this repository's entire recorded history is zero, and would be zero however good the engine
/// is.</para>
///
/// <para><b>This test exists to expire.</b> A finding written only in prose is a memoir (CI6), and a
/// claim about what does NOT exist decays when someone else acts — with the author absent and no
/// reason to look (DC-094). So the claim is tied to something that fails the day it stops being
/// true. When capture improves enough for a second observation, this goes red, and that red is the
/// news: <b>Daydream can propose something for the first time.</b> Rewrite it then; do not
/// delete it.</para>
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

    private sealed record Measurement(int Scored, DaydreamReach Reach, int Observations);

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
                record.Read().Observations.Count);
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
    /// EXPECTED RED, EVENTUALLY: the corpus cannot yet produce a single candidate.
    /// </summary>
    /// <remarks>
    /// <para>Recurrence needs <c>RecurrenceDetector</c>'s minimum of two distinct episodes. Fewer
    /// observations than that means no pattern can ever be proposed, whatever the engine does.</para>
    ///
    /// <para><b>When this fails, nothing is broken.</b> It means enough turns have recorded their
    /// evidence for a second observable episode to exist, and Daydream can propose something for the
    /// first time. Rewrite the assertion to check that a candidate appears — that is what its own
    /// remark asked for, and deleting it would throw away the only place the transition is
    /// visible.</para>
    /// </remarks>
    [Fact]
    public void TheRealCorpusCannotYetProduceACandidate()
    {
        var m = Measure();

        Assert.True(
            m.Observations < 2,
            $"{m.Observations} observations were recorded — enough to recur. This test has expired, "
            + "which is good news: rewrite it to assert that a candidate appears.");

        Assert.Empty(new RecurrenceDetector().Recurring([]));
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
