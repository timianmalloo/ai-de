using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.App.Conductor;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Sessions;
using AiDe.Core.Watcher;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// <b>The E7 consistency test across surfaces</b> (Addendum C §B7; D US-D1) — this slice closes
/// the chain: for one prompt, the decoration the composer shows, the row the store holds, the
/// bytes the lane receives and the leaderboard's class column agree. Through the real
/// composition root (a <see cref="SessionDocumentSurface"/> over a real session directory), never
/// a hand-wired seam. Plus ADR-0034 test 6: the store's lifetime is the document's.
/// </summary>
public sealed class TheDecorationTheRowTheBytesAndTheColumnAgreeTests
{
    private sealed class NeverAsked : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
    }

    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(20);

    private static (string Root, SessionConfig Config) Session()
    {
        var root = Path.Combine(Path.GetTempPath(), "aide-e7", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var config = new SessionConfigStore(root, SessionId.New(new DateTimeOffset(2026, 9, 12, 15, 0, 0, TimeSpan.Zero)))
            .Create("payments", "w-1", ["claude-code"], DateTimeOffset.UnixEpoch);
        return (root, config);
    }

    private static void Configure(SessionDocumentSurface document, string root, SessionConfig config) =>
        document.Composer.Configure(
            config,
            new ComposerSendContext(
                RepositoryRoot: root, DataDirectory: root, AdapterInstallRoot: root,
                EngineId: "no-such-engine",   // not in the catalog: the root is counted, the run refused, no engine spawned
                Model: "sonnet", AccountLabel: "max-personal", TaskClass: config.DefaultTaskClass,
                ProofPackArtifacts: [], Providers: []),
            ComposerFields.GoalBlock(),
            new AttachmentGate(root, new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max-personal"));

    [Fact]
    public void ForOnePromptTheDecorationTheRowTheBytesAndTheColumnAgree()
    {
        Sta.Run(() =>
        {
            var (root, config) = Session();
            try
            {
                var model = new SessionDocumentViewModel(config.SessionId, "payments", root, [CanvasModeCatalog.ConsoleModeId]);
                using var document = new SessionDocumentSurface(model);
                Configure(document, root, config);

                // The store opened with the document (ADR-0034 rule 2) and history is being recorded.
                Assert.NotNull(document.Envelopes);
                Assert.Null(document.Composer.HistoryState);

                // THE OPERATOR TYPES, and the mechanical pre-compile decorates: a goal block with two mentions.
                var composer = document.Composer;
                composer.Draft.SwitchTo(ComposerShape.GoalBlock);
                composer.Draft.SetFreeFormText("touch @src/A/ and @src/B/ and explain the store\n");
                composer.Draft.SetGoalValue(GoalBlockFields.GoalKey, "Explain the store");
                composer.Draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "the operator understands it");
                composer.Draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "the ADR");

                // PREPARE: the operator overrides the tier and chooses a class ON THE DECORATION LINE.
                composer.TierControl.SelectedItem = "T1";
                composer.ClassControl.SelectedItem = "defect";   // a vocabulary class: the control offers the session default, then TaskClassVocabulary.Offered

                // 1. THE DECORATION THE COMPOSER SHOWS — read from the rendered controls and rows.
                Render(document);
                var shown = composer.Decorations;
                Assert.Equal("T1", shown.Single(d => d.Name == "tier").Value);
                Assert.Equal("operator (rule said T2)", shown.Single(d => d.Name == "tier").Reason);
                Assert.Equal("defect", shown.Single(d => d.Name == "class").Value);
                Assert.Equal(DecorationSources.Operator, shown.Single(d => d.Name == "class").Source);
                Assert.Equal("src/A/** · src/B/**", shown.Single(d => d.Name == "lease").Value);
                Assert.Equal("goal block", shown.Single(d => d.Name == "shape").Value);
                Assert.Equal("T1", composer.TierControl.SelectedItem);
                Assert.Equal("defect", composer.ClassControl.SelectedItem);
                Assert.Contains("chosen for this prompt", Provenances(document));
                Assert.StartsWith("fan-out cap 2 (ceiling 2)", composer.SettingsLine, StringComparison.Ordinal);

                // The compiled disclosure shows the bytes that WILL be sent — Current, override included.
                var disclosed = composer.CompiledView;
                Assert.Contains("## tier\n\nT1\n", disclosed, StringComparison.Ordinal);

                // 2. THE BYTES THE LANE RECEIVES — the send, through the surface's own verb.
                var request = composer.Send();
                Assert.NotNull(request);
                Assert.Equal("T1", request!.Goal!.Tier);
                Assert.Equal(2, request.Goal.FanOutCap);
                Assert.Equal("defect", request.TaskClass);
                Assert.Equal(["src/A/**", "src/B/**"], request.Lease!.Exclusive);
                Assert.Contains("## tier\n\nT1\n", request.Prompt, StringComparison.Ordinal);
                Assert.Equal(disclosed, request.Prompt);   // the compiled disclosure WAS the sent bytes (the composer has moved to the next turn)

                // 3. THE ROW THE STORE HOLDS — the same projection, from the persisted fold.
                var submission = composer.Gate.LastSubmission!;
                Assert.True(submission.Recorded);
                var envelope = document.Envelopes!.Read().Find(submission.EnvelopeId)!;
                var stored = Projection.Project(envelope);
                Assert.Equal("T1", stored.Tier);
                Assert.Equal("operator (rule said T2)", stored.Rationale);
                Assert.Equal("defect", stored.TaskClass);
                Assert.Equal(DecorationSources.Operator, stored.TaskClassSource);
                Assert.Equal(request.Goal, stored.GoalBlock);
                Assert.Equal(request.Lease.Exclusive, stored.Lease!.Exclusive);
                Assert.Equal(submission.ProjectionSha, stored.ProjectionSha);
                Assert.Equal(submission.ProjectionSha, envelope.Submitted!.ProjectionSha);
                Assert.Equal(EnvelopeHash.Sha256Hex(request.Prompt), envelope.Submitted.TextSha256);
                Assert.Equal(
                    [DecorationNames.Ceilings, DecorationNames.TaskClass, DecorationNames.FamilyProfile, DecorationNames.TemplateApplied, DecorationNames.Attachments, DecorationNames.Goal, DecorationNames.DoneWhen, DecorationNames.NotInScope, DecorationNames.Tier],
                    envelope.Decorations.Select(d => d.Name));
                Assert.Equal(DecorationSources.Operator, envelope.Current(DecorationNames.Tier)!.Source);
                Assert.Equal(config.SessionId, envelope.Opened!.SessionId);

                // 4. THE COLUMN — the provenance the leaderboard stamps is the row's, and the same
                // string the composer's segment showed. (The stamp itself lands on a scored cell after
                // a real run; the refused run here scores nothing, so the value is asserted at its
                // source and the store's own test proves the UPDATE.)
                Assert.Equal(TaskClasses.Sources.Operator, submission.TaskClassSource);

                // The refused run concludes and the envelope is consumed — never an absent row.
                Assert.True(document.LastLaunch.Wait(Bound), "the refused run did not conclude");
                var consumed = document.Envelopes.Read().Find(submission.EnvelopeId)!.Consumed;
                Assert.NotNull(consumed);
                Assert.Equal("lane_exited", consumed!.Reason);
                Assert.Equal(Envelope.NotRecorded, consumed.Outcome);
            }
            finally
            {
                try { Directory.Delete(root, recursive: true); } catch (IOException) { }
            }
        });
    }

    /// <summary>The same prompt with nothing chosen: the session's default class with <c>source: session-default</c>, the rule's tier.</summary>
    [Fact]
    public void WithNothingChosenTheClassIsTheSessionsDefaultAndTheTierIsTheRules()
    {
        Sta.Run(() =>
        {
            var (root, config) = Session();
            try
            {
                using var document = new SessionDocumentSurface(new SessionDocumentViewModel(config.SessionId, "payments", root, [CanvasModeCatalog.ConsoleModeId]));
                Configure(document, root, config);
                var composer = document.Composer;
                composer.Draft.SwitchTo(ComposerShape.GoalBlock);
                composer.Draft.SetFreeFormText("touch @src/A/\n");
                composer.Draft.SetGoalValue(GoalBlockFields.GoalKey, "g");
                composer.Draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "d");
                composer.Draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "n");
                Render(document);

                Assert.Equal(ComposerSurface.RuleChoice, composer.TierControl.SelectedItem);
                Assert.Equal(TaskClasses.FreeForm, composer.ClassControl.SelectedItem);
                Assert.Contains("session default", Provenances(document));

                var request = composer.Send()!;
                Assert.Equal(TaskClasses.FreeForm, request.TaskClass);
                Assert.Equal("T1", request.Goal!.Tier);
                Assert.Equal(TaskClasses.Sources.SessionDefault, composer.Gate.LastSubmission!.TaskClassSource);
                Assert.Equal("R2", Projection.Project(document.Envelopes!.Read().Find(composer.Gate.LastSubmission.EnvelopeId)!).Rule);
                Assert.True(document.LastLaunch.Wait(Bound));
            }
            finally
            {
                try { Directory.Delete(root, recursive: true); } catch (IOException) { }
            }
        });
    }

    // ── ADR-0034 test 6: lifetime ──

    [Fact]
    public void OpeningADocumentOpensTheStoreAndClosingItReleasesTheHandle()
    {
        Sta.Run(() =>
        {
            var (root, config) = Session();
            try
            {
                var directory = SessionPaths.SessionDirectory(root, config.SessionId);
                var model = new SessionDocumentViewModel(config.SessionId, "payments", root, [CanvasModeCatalog.ConsoleModeId]);

                var document = new SessionDocumentSurface(model);
                Assert.NotNull(document.Envelopes);
                Assert.Throws<EnvelopeStoreException>(() => EnvelopeStore.Open(directory));   // held by the document

                document.Dispose();
                using var reopened = EnvelopeStore.Open(directory);                             // released at close
                Assert.NotNull(reopened);
            }
            finally
            {
                try { Directory.Delete(root, recursive: true); } catch (IOException) { }
            }
        });
    }

    [Fact]
    public void ALockedFileAtOpenDegradesPrepareWithTheReasonShownAndTheDocumentStillOpens()
    {
        Sta.Run(() =>
        {
            var (root, config) = Session();
            try
            {
                using var other = EnvelopeStore.Open(SessionPaths.SessionDirectory(root, config.SessionId));   // "another AI-DE"
                using var document = new SessionDocumentSurface(new SessionDocumentViewModel(config.SessionId, "payments", root, [CanvasModeCatalog.ConsoleModeId]));
                Configure(document, root, config);

                Assert.Null(document.Envelopes);
                Assert.Contains("another AI-DE has this session's compile history open", document.Composer.HistoryState, StringComparison.Ordinal);
                Render(document);
                Assert.Contains("compile history: [" + EnvelopeStoreErrorCodes.HeldByAnotherWriter + "] another AI-DE has this session's compile history open", document.Composer.SettingsLine, StringComparison.Ordinal);

                // And the send still proceeds, on the in-memory fold, with the same bytes.
                document.Composer.Draft.SetFreeFormText("explain the store\n");
                var request = document.Composer.Send();
                Assert.NotNull(request);
                Assert.False(document.Composer.Gate.LastSubmission!.Recorded);
                Assert.True(document.LastLaunch.Wait(Bound));
            }
            finally
            {
                try { Directory.Delete(root, recursive: true); } catch (IOException) { }
            }
        });
    }

    [Fact]
    public void ADocumentWithNoSessionDirectoryOpensWithHistoryNotRecordedAndTheReasonNamed()
    {
        Sta.Run(() =>
        {
            using var document = new SessionDocumentSurface(new SessionDocumentViewModel("20260912T150000Z-nodir00", "nodir", Path.GetTempPath(), [CanvasModeCatalog.ConsoleModeId]));
            Assert.Null(document.Envelopes);
            Assert.Contains("no session directory", document.Composer.HistoryState, StringComparison.Ordinal);
            Assert.False(Directory.Exists(SessionPaths.SessionDirectory(Path.GetTempPath(), "20260912T150000Z-nodir00")));   // never created
        });
    }

    [Fact]
    public void ClosingTheDocumentWithARunInFlightConsumesTheEnvelopeAsDocumentClosed()
    {
        Sta.Run(() =>
        {
            var (root, config) = Session();
            try
            {
                var directory = SessionPaths.SessionDirectory(root, config.SessionId);
                string envelopeId;
                using (var document = new SessionDocumentSurface(new SessionDocumentViewModel(config.SessionId, "payments", root, [CanvasModeCatalog.ConsoleModeId])))
                {
                    Configure(document, root, config);
                    document.Composer.Draft.SetFreeFormText("explain the store\n");
                    Assert.NotNull(document.Composer.Send());
                    envelopeId = document.Composer.Gate.LastSubmission!.EnvelopeId;

                    // Closed while the (refused) run may still be in flight: Dispose consumes what is left.
                }

                var envelope = EnvelopeStore.ReadFile(Path.Combine(directory, EnvelopeStore.FileName)).Find(envelopeId)!;
                var consumed = Assert.Single(envelope.Events.OfType<Consumed>());
                Assert.Contains(consumed.Reason, new[] { ConsumedReasons.DocumentClosed, "lane_exited" });
            }
            finally
            {
                try { Directory.Delete(root, recursive: true); } catch (IOException) { }
            }
        });
    }

    private static void Render(FrameworkElement root)
    {
        root.Measure(new Size(1200, 900));
        root.Arrange(new Rect(0, 0, 1200, 900));
        root.UpdateLayout();
    }

    private static IReadOnlyList<string> Provenances(DependencyObject root) =>
        [.. Descendants(root).OfType<TextBlock>().Select(t => t.Text).Distinct(StringComparer.Ordinal)];

    private static IEnumerable<DependencyObject> Descendants(DependencyObject node)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            var child = VisualTreeHelper.GetChild(node, i);
            yield return child;
            foreach (var inner in Descendants(child))
            {
                yield return inner;
            }
        }

        if (node is ContentControl { Content: DependencyObject content })
        {
            yield return content;
            foreach (var inner in Descendants(content))
            {
                yield return inner;
            }
        }
    }
}
