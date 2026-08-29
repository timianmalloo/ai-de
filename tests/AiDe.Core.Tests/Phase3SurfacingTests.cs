using AiDe.Core.Extraction;
using AiDe.Core.Facts;
using AiDe.Core.Projections;
using AiDe.Core.Terminal;

namespace AiDe.Core.Tests;

/// <summary>
/// Making the Phase 3 evidence usable: a configurable readiness marker, a crossing that can be
/// opened, and uncovered symbols a user can act on.
/// </summary>
/// <remarks>
/// Every test here exists because a number was being shown that nobody could check, or a refusal
/// was being made that nobody could fix.
/// </remarks>
public sealed class Phase3SurfacingTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "aide-readiness", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    private string Write(string json)
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, AgentReadinessProfiles.FileName), json);
        return _dir;
    }

    // ── Readiness markers, per agent ──────────────────────────────────────────────────────

    [Fact]
    public void NoFile_LeavesTheBuiltInMarkersInForce()
    {
        var profiles = AgentReadinessProfiles.Load(_dir);

        Assert.Empty(profiles.Problems);
        Assert.Equal(AgentReadinessWatcher.KnownAgents["claude"], profiles.For("claude")!.Pattern);
        Assert.Equal("built-in", profiles.For("claude")!.Origin);
    }

    [Fact]
    public void AConfiguredMarkerReplacesTheBuiltInOne()
    {
        // The point of the whole file: a built-in marker that does not match a real agent's prompt
        // refused that agent forever, and the only way to change it was a rebuild.
        var profiles = AgentReadinessProfiles.Load(Write("""{ "claude": "READY>\\s*$" }"""));

        Assert.Empty(profiles.Problems);
        Assert.Equal(@"READY>\s*$", profiles.For("claude")!.Pattern);
        Assert.Equal(AgentReadinessProfiles.FileName, profiles.For("claude")!.Origin);

        var watcher = profiles.WatcherFor("claude")!;
        watcher.Observe("thinking...\nREADY>");
        Assert.True(watcher.IsReady);
    }

    [Fact]
    public void AnUnusablePatternIsReported_AndTheBuiltInMarkerStaysInForce()
    {
        // Never fails open. A pattern that does not compile must not become "assume ready" — the
        // one thing worse than refusing a ready agent is dispatching into an unready one.
        var profiles = AgentReadinessProfiles.Load(Write("""{ "claude": "([unclosed" }"""));

        Assert.Single(profiles.Problems);
        Assert.Contains("claude", profiles.Problems[0], StringComparison.Ordinal);
        Assert.Equal(AgentReadinessWatcher.KnownAgents["claude"], profiles.For("claude")!.Pattern);
    }

    [Fact]
    public void AnEmptyMarkerMeansThisAgentHasNone_AndDispatchCannotEstablishReadiness()
    {
        // A legitimate thing to say: it makes the refusal deliberate rather than the accident of a
        // pattern that happens never to match.
        var profiles = AgentReadinessProfiles.Load(Write("""{ "claude": "" }"""));

        Assert.Empty(profiles.Problems);
        Assert.Null(profiles.For("claude"));
        Assert.Null(profiles.WatcherFor("claude"));
    }

    [Fact]
    public void AnAgentTheBuildNeverHeardOfCanBeAdded()
    {
        var profiles = AgentReadinessProfiles.Load(Write("""{ "aider": "\\n>\\s*$" }"""));

        Assert.NotNull(profiles.WatcherFor("aider"));
        Assert.Equal(AgentReadinessProfiles.FileName, profiles.For("aider")!.Origin);
    }

    [Fact]
    public void AMalformedFileIsReported_NotSilentlyIgnored()
    {
        var profiles = AgentReadinessProfiles.Load(Write("{ not json"));

        Assert.Single(profiles.Problems);
        Assert.NotNull(profiles.For("claude"));
    }

    [Fact]
    public void TheTemplateIsNeverWrittenOverAUsersEdits()
    {
        // The file exists to hold a marker someone tuned. Regenerating it over their edit would
        // destroy the only copy of the thing this feature is for.
        var path = AgentReadinessProfiles.WriteTemplate(_dir);
        File.WriteAllText(path, """{ "claude": "MINE$" }""");

        AgentReadinessProfiles.WriteTemplate(_dir);

        Assert.Equal("""{ "claude": "MINE$" }""", File.ReadAllText(path));
    }

    [Fact]
    public void TheWatcherReportsTheTailItJudged()
    {
        // Tuning a marker by reasoning about what an agent probably prints is how a pattern that
        // never matches survives. This is what it actually printed.
        var watcher = new AgentReadinessWatcher(@"NEVERMATCHES$");
        watcher.Observe("╭─────╮\r\n│ > │\r\n╰─────╯");

        Assert.False(watcher.IsReady);
        Assert.Contains("│ > │", watcher.LastJudged, StringComparison.Ordinal);
        Assert.Equal("NEVERMATCHES$", watcher.Pattern);
    }

    [Fact]
    public void ARealTrustGateIsNotMistakenForAPrompt()
    {
        // MEASURED, not imagined. spikes/agent-readiness captured what Claude Code actually draws
        // when this shell starts it, and the bytes contain a chevron — at ESC[14;2H, as the SELECTION
        // CURSOR of the trust dialog, sitting on "No, exit".
        //
        // A looser marker is the obvious repair when a pattern does not match, and it would report
        // READY at the exact moment dispatch is most dangerous: the Enter that submits a prompt is
        // the Enter that confirms "No, exit". This is the negative control on that repair.
        var watcher = new AgentReadinessWatcher(AgentReadinessWatcher.KnownAgents["claude"]);
        watcher.Observe(TrustGateOutput());

        Assert.False(watcher.IsReady);
        Assert.Contains("❯", watcher.LastJudged, StringComparison.Ordinal);
    }

    /// <summary>The captured session output, control characters restored.</summary>
    /// <remarks>
    /// Stored escaped so the fixture is readable and diffable in exactly the whitespace a
    /// tail-anchored pattern turns on. Unescaped here so the watcher sees the real bytes.
    /// </remarks>
    private static string TrustGateOutput()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "claude-trust-gate.escaped.txt");
        Assert.True(File.Exists(path), $"the captured agent output is missing: {path}");

        return File.ReadAllText(path)
            .Replace("<ESC>", "\u001b", StringComparison.Ordinal)
            .Replace("<BEL>", "\a", StringComparison.Ordinal)
            .Replace("<TAB>", "\t", StringComparison.Ordinal)
            .Replace("<CR>", "\r", StringComparison.Ordinal)
            // The escaper prints a real newline after <LF> so the dump wraps; both go.
            .Replace("<LF>\n", "\n", StringComparison.Ordinal)
            .Replace("<LF>", "\n", StringComparison.Ordinal);
    }

    // ── Crossings can be opened ───────────────────────────────────────────────────────────

    private static EvidenceAssertion Edge(string subject, string obj) =>
        new("view", "rev-1", subject, "references", obj, EvidenceOrigin.Static, VerificationStatus.Verified,
            new Provenance("test", null, "test", "1", DateTimeOffset.UnixEpoch));

    private const string TwoContextYaml =
        """
        contexts:
          - name: Editorial
            includes:
              - Ed.*
          - name: Football
            includes:
              - Fb.*
        """;

    /// <summary>Written to disk because the reader validates a FILE against the symbols found.</summary>
    private BoundedContextMap Map(IReadOnlyCollection<string> symbols)
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "bounded-contexts.yaml");
        File.WriteAllText(path, TwoContextYaml);
        return BoundedContextReader.Load(path, symbols);
    }

    [Fact]
    public void ACrossingCarriesTheEdgesThatMakeIt()
    {
        // A count is not evidence. "Editorial → Football, 47 edges" is a claim about the user's code
        // that they cannot check, act on, or disagree with.
        var view = new ContextProjection(Map(["Ed.A", "Ed.B", "Fb.X", "Fb.Y"]),
            [Edge("Ed.A", "Fb.X"), Edge("Ed.B", "Fb.Y")]).Compute();

        var crossing = Assert.Single(view.Edges);
        Assert.Equal(2, crossing.Weight);
        Assert.Equal(2, crossing.Members.Count);
        Assert.Contains(crossing.Members, m => m.Subject == "Ed.A" && m.Object == "Fb.X");
        Assert.Equal(0, crossing.Undisclosed);
    }

    [Fact]
    public void TheMemberCapNeverBecomesAQuieterWrongNumber()
    {
        // The list is capped so a pane rendering thousands of rows does not stop responding. The
        // WEIGHT must stay the true total, and the difference must be stated — a cap that silently
        // truncated would turn a correct count into a confident wrong one.
        var edges = Enumerable.Range(0, ContextEdge.MemberCap + 25)
            .Select(i => Edge($"Ed.A{i}", $"Fb.X{i}"))
            .ToList();

        var map = Map([.. edges.Select(e => e.Subject), .. edges.Select(e => e.Object)]);

        var crossing = Assert.Single(new ContextProjection(map, edges).Compute().Edges);

        Assert.Equal(ContextEdge.MemberCap + 25, crossing.Weight);
        Assert.Equal(ContextEdge.MemberCap, crossing.Members.Count);
        Assert.Equal(25, crossing.Undisclosed);
    }

    // ── Uncovered symbols become a task ───────────────────────────────────────────────────

    [Fact]
    public void UncoveredSymbolsAreRankedByNamespace_LargestFirst()
    {
        var groups = ContextProjection.GroupUncovered(
            ["A.B.One", "A.B.Two", "A.B.Three", "C.D.Only", "Bare"]);

        Assert.Equal("A.B", groups[0].Namespace);
        Assert.Equal(3, groups[0].Symbols);
        Assert.Contains("A.B.One", groups[0].Examples);
    }

    [Fact]
    public void ASymbolWithNoNamespaceIsGrouped_NotDropped()
    {
        // Silently omitting the ones that do not fit the shape is how a coverage breakdown starts
        // disagreeing with the coverage number printed beside it.
        var groups = ContextProjection.GroupUncovered(["A.B.One", "Bare"]);

        Assert.Equal(2, groups.Sum(g => g.Symbols));
        Assert.Contains(groups, g => g.Namespace == "(no namespace)");
    }
}
