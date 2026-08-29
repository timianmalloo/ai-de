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

    // ── A bounded read says what it did not see ───────────────────────────────────────────

    [Fact]
    public void ACompleteReadSaysNothing()
    {
        // Silence is the correct output when there is nothing to caveat. A banner on every refresh
        // is a banner the user stops reading, and then the one that mattered goes unread too.
        var read = new EvidenceRead([], NodesMatched: 12, NodesRead: 12, NeighbourLimit: 60,
            NodesAtNeighbourLimit: 0);

        Assert.True(read.IsComplete);
        Assert.Null(read.Shortfall);
    }

    [Fact]
    public void UnreadNodesAreNamedWithTheirCount()
    {
        var read = new EvidenceRead([], NodesMatched: 9000, NodesRead: 4000, NeighbourLimit: 60,
            NodesAtNeighbourLimit: 0);

        Assert.False(read.IsComplete);
        Assert.Contains("5,000 of 9,000", read.Shortfall!, StringComparison.Ordinal);
        Assert.Contains("lower bounds", read.Shortfall!, StringComparison.Ordinal);
    }

    [Fact]
    public void BothCausesAreReported_BecauseTheyHaveDifferentFixes()
    {
        // "The workspace is bigger than the search cap" and "these nodes are unusually connected"
        // are different problems. Collapsing them into one sentence leaves the reader guessing which
        // they have, and the fixes point in opposite directions.
        var read = new EvidenceRead([], NodesMatched: 9000, NodesRead: 4000, NeighbourLimit: 60,
            NodesAtNeighbourLimit: 17);

        Assert.Contains("were not read", read.Shortfall!, StringComparison.Ordinal);
        Assert.Contains("17 node(s) had more than 60", read.Shortfall!, StringComparison.Ordinal);
    }

    [Fact]
    public void ANodeExactlyAtTheLimitCountsAsTruncated()
    {
        // The read cannot tell "exactly 60 neighbours" from "60 of many", and guessing in the
        // flattering direction is how a cap becomes a quieter wrong number.
        var read = new EvidenceRead([], NodesMatched: 1, NodesRead: 1, NeighbourLimit: 60,
            NodesAtNeighbourLimit: 1);

        Assert.False(read.IsComplete);
        Assert.NotNull(read.Shortfall);
    }

    // ── One composition, and it routes where it says ──────────────────────────────────────

    [Fact]
    public void TheShippedCompositionRoutesEveryScopeKindToItsOwnExtractor()
    {
        // The router is four positional constructor parameters and getting their order wrong is
        // SILENT: a mis-ordered composite sent every bicep: scope to the schema extractor, both
        // failed, and the run reported a repository with no infrastructure in it. That happened, in
        // a spike, and it produced a confidently wrong write-up before anyone noticed.
        var composite = Assert.IsType<CompositeExtractor>(WorkspaceExtractors.Default());

        foreach (var (prefix, kind) in WorkspaceExtractors.RoutedKinds)
        {
            Assert.Equal(kind, composite.RouteFor(prefix + "anything").ScopeKind);
        }
    }

    [Fact]
    public void AnUnknownScopeKindFallsThroughRatherThanBeingMisrouted()
    {
        // The fallback is a real answer, not a hole: Phase 2's fixture evidence still renders beside
        // real extraction, and a scope kind this build does not know must not be quietly handed to
        // whichever extractor happens to be first.
        var composite = Assert.IsType<CompositeExtractor>(WorkspaceExtractors.Default());

        Assert.Equal("fixture", composite.RouteFor("timeline:whatever").ScopeKind);
    }

    // ── The screen, not the byte stream ───────────────────────────────────────────────────

    [Fact]
    public void TextLandsWhereTheCursorSaysItDoes_NotWhereItArrived()
    {
        // The measured shape. An agent draws with absolute addressing and repaints regions in
        // whatever order it likes, so the LAST bytes are not the BOTTOM line. Asserted through the
        // watcher rather than a screen of its own: a second model of one terminal disagrees with the
        // pane the first time either is fixed.
        var watcher = new AgentReadinessWatcher(readyPattern: "^middle$");
        watcher.Observe("\u001b[3;1Hmiddle" + "\u001b[1;1Htop");

        Assert.Contains("top", watcher.LastJudged, StringComparison.Ordinal);
        Assert.Contains("middle", watcher.LastJudged, StringComparison.Ordinal);

        // "middle" is on row 3 and "top" on row 1, so the last DRAWN line is middle — even though
        // "top" was the last thing written.
        Assert.True(watcher.IsReady);
    }

    [Fact]
    public void EscapeSequencesNeverBecomeScreenText()
    {
        // A parser that fell through to "write the bytes as text" would put escape codes into the
        // screen it models, and the readiness pattern would match text no human ever saw.
        var watcher = new AgentReadinessWatcher(readyPattern: "NEVER");
        watcher.Observe("\u001b[38;2;150;108;30mcoloured\u001b[m \u001b]0;title\adone");

        Assert.Contains("coloured done", watcher.LastJudged, StringComparison.Ordinal);
        Assert.DoesNotContain("38;2;150", watcher.LastJudged, StringComparison.Ordinal);
    }

    [Fact]
    public void TheRenderedScreenIsWhatTheUserWouldSee()
    {
        // Against the CAPTURED bytes. The dialog's text is spread across rows 3 to 17 by absolute
        // addressing, and only a screen model puts it back together.
        var watcher = new AgentReadinessWatcher(readyPattern: "NEVER");
        watcher.Observe(TrustGateOutput());

        Assert.Contains("Quick safety check", watcher.LastJudged, StringComparison.Ordinal);
        Assert.Contains("Yes, I trust this folder", watcher.LastJudged, StringComparison.Ordinal);
    }

    // ── The trust gate is a state, not a silent refusal ───────────────────────────────────

    [Fact]
    public void AnAgentWaitingOnAPersonSaysSo_AndIsNotReady()
    {
        // Measured: this gate is the NORMAL first screen, not an edge case. Reporting it as an
        // unexplained refusal is DC-011 — refusal indistinguishable from breakage.
        var watcher = AgentReadinessProfiles.BuiltIn.WatcherFor("claude")!;
        watcher.Observe(TrustGateOutput());

        Assert.True(watcher.NeedsAttention);
        Assert.Contains("trust", watcher.AttentionLine, StringComparison.OrdinalIgnoreCase);
        Assert.False(watcher.IsReady);
    }

    [Fact]
    public void AttentionOutranksAPromptLookingScreen()
    {
        // The dialog draws a chevron on its selected option. Even a marker that matches it must not
        // produce READY while a person is being asked a safety question.
        var watcher = new AgentReadinessWatcher(readyPattern: ".", attentionPattern: "trust");
        watcher.Observe("\u001b[1;1HDo you trust this folder?");

        Assert.True(watcher.NeedsAttention);
        Assert.False(watcher.IsReady);
    }

    [Fact]
    public void WithNoDialogOnScreen_ThePromptLineDecides()
    {
        var watcher = new AgentReadinessWatcher(readyPattern: "^>$", attentionPattern: "trust");
        watcher.Observe("thinking...\u001b[2;1H> ");

        Assert.False(watcher.NeedsAttention);
        Assert.True(watcher.IsReady);
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

    // ── A predicate is a name, and two extractors gave it two meanings ────────────────────

    private static EvidenceAssertion Say(string subject, string predicate, string obj) =>
        new("view", "rev-1", subject, predicate, obj, EvidenceOrigin.Static, VerificationStatus.Verified,
            new Provenance("test", null, "test", "1", DateTimeOffset.UnixEpoch));

    [Fact]
    public void ADependsOnFromCodeIsNotReportedAsAResourceDependency()
    {
        // MEASURED on a real repository: `depends_on` is the C# extractor's predicate for type
        // dependencies — 7,426 of them — and joining on the predicate alone attached the basis
        // "declared in the resource's dependsOn" to every one. A large number with a false sentence
        // beside it, which is the most convincing kind of wrong (DC-022).
        var result = new JoinProjection([
            Say("TheTerrace.Components.Display", "depends_on", "string"),
        ]).Compute();

        Assert.DoesNotContain(result.Edges, e => e.Kind == "depends_on");
    }

    [Fact]
    public void ADependsOnBetweenDeclaredResourcesIsStillJoined()
    {
        // The other half. Narrowing a join until it can no longer fire is not a fix.
        var result = new JoinProjection([
            Say("sqlServer", "resource_type", "Microsoft.Sql/servers"),
            Say("sqlServer", "depends_on", "vnet"),
        ]).Compute();

        var edge = Assert.Single(result.Edges, e => e.Kind == "depends_on");
        Assert.Equal("sqlServer", edge.From);
        Assert.Equal("vnet", edge.To);
        Assert.Equal(VerificationStatus.Verified, edge.Status);
    }

    [Fact]
    public void AHasTypeFromTheWrongProducerIsNotConsumedAsACodeType()
    {
        // has_type is emitted by ALL THREE extractors — measured over a real repository, not assumed
        // — and its object values partition by producer only by accident. This makes the partition
        // something the code enforces: a bicep-scoped subject claiming to be a class is not joined as
        // one, whatever the object value says (DC-022's residual).
        var result = new JoinProjection([
            Say("bicep:main#Order", "has_type", "class"),
            Say("table:Order", "has_type", "table"),
        ]).Compute();

        Assert.DoesNotContain(result.Edges, e => e.Kind == "maps_to");
    }

    [Fact]
    public void ACodeTypeIsStillJoinedToItsTable()
    {
        // The other half, every time: a qualifier that also blocks the real case is not a fix.
        var result = new JoinProjection([
            Say("Shop.Sales.Order", "has_type", "class"),
            Say("table:Order", "has_type", "table"),
        ]).Compute();

        var edge = Assert.Single(result.Edges, e => e.Kind == "maps_to");
        Assert.Equal("Shop.Sales.Order", edge.From);
        Assert.Equal(VerificationStatus.Inferred, edge.Status);
    }

    [Fact]
    public void ATableSubjectMustCarryTheTablePrefix()
    {
        // A code type that happened to be described as a "table" by another extractor must not
        // become a join target. Nothing emits this today; that is exactly when a qualifier is cheap.
        var result = new JoinProjection([
            Say("Shop.Sales.Order", "has_type", "class"),
            Say("Shop.Sales.Order", "has_type", "table"),
        ]).Compute();

        Assert.DoesNotContain(result.Edges, e => e.Kind == "maps_to");
    }

    [Fact]
    public void ACrossingDominatedByOneObjectSaysWhichOne()
    {
        // Found by eye once, so now it is computed. On TheTerrace, 57 of the 72 Football-to-
        // Operations edges were AppDbContext, which made a boundary that mostly holds look like one
        // that never did. A signal a person has to notice is a signal that gets noticed once.
        var edges = Enumerable.Range(0, 9).Select(i => Edge($"Ed.A{i}", "Fb.AppDbContext"))
            .Concat([Edge("Ed.B", "Fb.Other")])
            .ToList();

        var crossing = Assert.Single(new ContextProjection(Map(
            [.. edges.Select(e => e.Subject), .. edges.Select(e => e.Object)]), edges).Compute().Edges);

        Assert.Equal("Fb.AppDbContext", crossing.DominantTarget!.Object);
        Assert.Equal(9, crossing.DominantCount);
    }

    [Fact]
    public void AnEvenlySpreadCrossingNamesNothing()
    {
        // The half that stops this becoming noise. Ordinary coupling reaches many things, and a
        // signal that fires on every crossing tells the user nothing about any of them.
        var edges = Enumerable.Range(0, 8).Select(i => Edge($"Ed.A{i}", $"Fb.X{i}")).ToList();

        var crossing = Assert.Single(new ContextProjection(Map(
            [.. edges.Select(e => e.Subject), .. edges.Select(e => e.Object)]), edges).Compute().Edges);

        Assert.Null(crossing.DominantTarget);
        Assert.Equal(0, crossing.DominantCount);
    }

    [Fact]
    public void ExactlyHalfIsNotDomination()
    {
        // "Most of this crossing is one thing" is the claim. Half is not most, and a boundary rule
        // that fires ON the boundary is the kind of detail nobody checks until it misleads someone.
        var edges = Enumerable.Range(0, 3).Select(i => Edge($"Ed.A{i}", "Fb.Shared"))
            .Concat(Enumerable.Range(0, 3).Select(i => Edge($"Ed.B{i}", $"Fb.Other{i}")))
            .ToList();

        var crossing = Assert.Single(new ContextProjection(Map(
            [.. edges.Select(e => e.Subject), .. edges.Select(e => e.Object)]), edges).Compute().Edges);

        Assert.Null(crossing.DominantTarget);
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
