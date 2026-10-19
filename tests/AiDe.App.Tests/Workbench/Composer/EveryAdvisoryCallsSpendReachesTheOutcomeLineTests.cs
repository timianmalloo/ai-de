using AiDe.App.Conductor;
using AiDe.App.Tests.Records;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>T30 n4 (A) — Ruling 180 (4): the cost is shown.</b> Under <c>agentic-advisory</c> every prepare that asks the model
/// spends the user's subscription, and Ruling 78 puts a compile's <c>called.cost</c> on its turn's outcome line. A turn
/// whose prompt was prepared, edited and prepared again made two calls; both are its spend. Every step is the user's
/// gesture on the product's composition (<see cref="ComposerGesture"/>), and no model is called: the compile host is a
/// stub that reports a different spend on each call, so a lost call is visible in the sum.
/// </summary>
public sealed class EveryAdvisoryCallsSpendReachesTheOutcomeLineTests(ITestOutputHelper output) : IDisposable
{
    private const string Words = "Rename the helper in the parser.";

    /// <summary>The stub lane's own run usage: 4 in, 2 out (<see cref="StubAcpAdapter.Mode"/>'s defaults).</summary>
    private const long RunTokens = 4 + 2;

    private static readonly RunEventCost FirstCall = new(1000, 200, 0, 1);
    private static readonly RunEventCost SecondCall = new(3000, 400, 0, 1);

    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    /// <summary>
    /// Type, Ctrl+Enter (prepare: call 1), edit the prompt, Ctrl+Enter (prepare again: call 2), Ctrl+Enter (send): the
    /// turn's outcome line counts both calls' tokens with the run's, and the session header's spend agrees.
    /// </summary>
    [Fact]
    public void APromptPreparedTwice_ItsTurnsOutcomeLine_CountsBothCompileCalls()
    {
        ComposerGesture.Drive(_fixture, async (window, document) =>
        {
            var composer = document.Composer;
            AKeptSuggestedLineIsKeptTests.UnderAdvisory(composer);
            var calls = 0;
            composer.Gate.Compiler = (_, _) =>
            {
                calls++;
                return Task.FromResult(new CompileResult(
                    CompileCallOutcomes.Answered, null, """{"contract":"compile-output/1","decorations":[]}""",
                    calls == 1 ? FirstCall : SecondCall, "claude-opus-5[1m]", 5, 0, 0, 1, false, 0, "account", null, []));
            };

            ComposerGesture.Type(composer, Words);
            ComposerGesture.Settle(document);
            ComposerGesture.CtrlEnter(composer);
            await composer.Preparing!.WaitAsync(NotebookFixture.Bound);
            ComposerGesture.Settle(document);

            ComposerGesture.Type(composer, Words + " Keep the public names.");
            ComposerGesture.Settle(document);
            ComposerGesture.CtrlEnter(composer);
            await composer.Preparing!.WaitAsync(NotebookFixture.Bound);
            ComposerGesture.Settle(document);
            Assert.Equal(2, calls);

            ComposerGesture.CtrlEnter(composer);
            await ComposerGesture.UntilAsync(
                window,
                () => document.ReadModel.Current.Turns is [{ Outcome: not null }],
                () => $"b1 never concluded; turns {document.ReadModel.Current.Turns.Count}; status \"{composer.Status}\"; failure {document.LastRunFailure}");
            ComposerGesture.Settle(document);

            var compile = FirstCall.TokensIn + FirstCall.TokensOut + SecondCall.TokensIn + SecondCall.TokensOut;
            var expected = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{compile + RunTokens:N0} tokens");
            var spend = document.ReadModel.Current.Turns[0].Outcome!.Spend;
            var counts = document.Thread.Rows[0].Counts;
            output.WriteLine($"compile calls {calls}; submission compile cost {composer.Gate.LastSubmission?.CompileCost}; turn spend {spend}; outcome line \"{counts}\"; header \"{TurnCopy.SessionSpend(document.ReadModel.Current.Turns, null)}\"");
            Assert.True(counts.Contains(expected, StringComparison.Ordinal), $"b1's outcome line reads \"{counts}\", not {expected} (spend {spend})");
            Assert.Equal(2 + 1, spend!.Requests);
        });
    }
}
