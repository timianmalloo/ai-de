using System.Windows;
using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The sequence-diagram scaffold (uml-sequence-diagram): the pure model projection and the surface's
/// participant/message rendering, ready to wire to Core ordered-call data when it lands.
/// </summary>
public sealed class SequenceDiagramTests
{
    private static void OnSta(Action work)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { work(); } catch (Exception ex) { failure = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "STA thread did not finish");
        if (failure is Xunit.Sdk.XunitException) throw failure;   // the message IS the finding (DC-078)

        if (failure is not null) { throw new InvalidOperationException("STA work failed", failure); }
    }

    // ---- model ---------------------------------------------------------------

    [Fact]
    public void Build_DerivesParticipants_InFirstSeenOrder_AndOrdersMessages()
    {
        var model = SequenceModel.Build(
        [
            ("Controller", "Service", "Handle()"),
            ("Service", "Repository", "Load()"),
            ("Service", "Service", "Validate()"),
        ]);

        Assert.Equal(["Controller", "Service", "Repository"], model.Participants.Select(p => p.Id));
        Assert.Equal(3, model.Messages.Count);
        Assert.Equal([0, 1, 2], model.Messages.Select(m => m.Order));
        Assert.Equal(SequenceMessageKind.Self, model.Messages[2].Kind); // Service -> Service
        Assert.Equal(SequenceMessageKind.Call, model.Messages[0].Kind);
    }

    [Fact]
    public void Build_WithNoCalls_IsEmpty()
    {
        Assert.True(SequenceModel.Build(null).IsEmpty);
        Assert.True(SequenceModel.Build([]).IsEmpty);
    }

    [Fact]
    public void Build_SimplifiesParticipantLabels_ToTheirLastSegment()
    {
        var model = SequenceModel.Build([("App.Web.OrderController", "App.Domain.OrderService", "Post()")]);
        Assert.Equal("OrderController", model.Participants[0].Label);
        Assert.Equal("OrderService", model.Participants[1].Label);
    }

    // ---- surface -------------------------------------------------------------

    [Fact]
    public void Surface_ShowsTheEmptyState_ForAnEmptyModel()
    {
        OnSta(() =>
        {
            var s = new SequenceDiagramSurface();
            s.Show(SequenceModel.Empty);
            Assert.True(s.IsEmpty);
            Assert.Equal(0, s.ParticipantCount);
        });
    }

    [Fact]
    public void Surface_DrawsLifelinesAndOrderedMessages()
    {
        OnSta(() =>
        {
            var s = new SequenceDiagramSurface();
            s.Show(SequenceModel.Build(
            [
                ("Controller", "Service", "Handle()"),
                ("Service", "Repository", "Load()"),
                ("Repository", "Service", "rows"),
            ]));

            Assert.False(s.IsEmpty);
            Assert.Equal(3, s.ParticipantCount);  // Controller, Service, Repository
            Assert.Equal(3, s.MessageCount);
        });
    }
}
