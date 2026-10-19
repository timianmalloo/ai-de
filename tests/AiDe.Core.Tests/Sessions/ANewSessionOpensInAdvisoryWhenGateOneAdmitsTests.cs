using AiDe.Core.AgentPlane;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Sessions;

namespace AiDe.Core.Tests.Sessions;

/// <summary>
/// <b>T30 (A) — Ruling 180 (1), amending Ruling 68's default only.</b> A new session's compile mode is
/// <c>agentic-advisory</c> when Gate 1 admits it, and <c>mechanical-only</c> otherwise. The gate's input is injected:
/// an admitting <see cref="CompileModeAvailability"/> built directly, and a refusing one evaluated by
/// <see cref="CompileModeGate.Evaluate(string, string)"/> over an empty temp proof directory. No test reads
/// <c>~/.aide/proof</c>. The <c>agentic</c> rung is untouched: it is never a default, whatever the gates say.
/// </summary>
public sealed class ANewSessionOpensInAdvisoryWhenGateOneAdmitsTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 19, 0, 0, TimeSpan.Zero);

    private static readonly AccountRef Max = new("anthropic", "max");

    private readonly string _root = Directory.CreateTempSubdirectory("aide-t30-default-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    /// <summary>Gate 1 admits, Gate 2 refuses: the ladder as this machine reads it today (Ruling 180's Owner check).</summary>
    private static CompileModeAvailability GateOneAdmits() => new(
        new Dictionary<string, CompileModeRefusal>(StringComparer.Ordinal)
        {
            [CompileModes.Agentic] = new("CE-0029", "Gate 2 has no admission report"),
        },
        Recount: null,
        ArtifactPath: "injected");

    /// <summary>Every rung admitted — the case where only the default rule keeps <c>agentic</c> out.</summary>
    private static CompileModeAvailability EveryRungAdmits() =>
        new(new Dictionary<string, CompileModeRefusal>(StringComparer.Ordinal), Recount: null, ArtifactPath: "injected");

    /// <summary>The gate's own refusal: no pin-spike artifact in an empty proof directory.</summary>
    private CompileModeAvailability GateOneRefuses()
    {
        var proof = Directory.CreateDirectory(Path.Combine(_root, "proof")).FullName;
        var install = Directory.CreateDirectory(Path.Combine(_root, "install")).FullName;
        return CompileModeGate.Evaluate(install, proof);
    }

    private SessionConfig Create(CompileModeAvailability? compileModes) =>
        new SessionConfigStore(_root, SessionId.New(Now)).Create("s", "w-1", [Max], Max, Now, compileModes: compileModes);

    [Fact]
    public void WhenGateOneAdmits_ANewSessionOpensInAgenticAdvisory_AndTheFileSaysSo()
    {
        var created = Create(GateOneAdmits());

        Assert.Equal(CompileModes.AgenticAdvisory, created.CompileMode);
        Assert.Equal(CompileModes.AgenticAdvisory, new SessionConfigStore(_root, created.SessionId).Load().CompileMode);
    }

    [Fact]
    public void WhenGateOneRefuses_ANewSessionOpensInMechanicalOnly_AndTheGateSaysWhy()
    {
        var availability = GateOneRefuses();
        var created = Create(availability);

        Assert.Equal(CompileModes.MechanicalOnly, created.CompileMode);
        var refusal = availability.RefusalFor(CompileModes.AgenticAdvisory);
        Assert.NotNull(refusal);
        Assert.Equal(EnvelopeStoreErrorCodes.PinArtifactMissing, refusal!.Code);
        Assert.Contains("compile-pin-spike artifact was not found", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenEveryRungIsAdmitted_TheDefaultIsStillAgenticAdvisory_NeverAgentic()
    {
        Assert.Equal(CompileModes.AgenticAdvisory, SessionConfig.DefaultCompileModeFor(EveryRungAdmits()));
        Assert.Equal(CompileModes.AgenticAdvisory, Create(EveryRungAdmits()).CompileMode);
    }

    [Fact]
    public void ACallerThatCannotEvaluateTheGate_OpensMechanicalOnly()
    {
        Assert.Equal(CompileModes.MechanicalOnly, SessionConfig.DefaultCompileModeFor(null));
        Assert.Equal(CompileModes.MechanicalOnly, Create(compileModes: null).CompileMode);
    }
}
