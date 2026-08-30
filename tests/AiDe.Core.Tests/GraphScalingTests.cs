using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using AiDe.Core.Ipc;
using AiDe.Core.Presentation;

namespace AiDe.Core.Tests;

/// <summary>
/// Controls for the graph-scaling fix (INV-0003 / DC-035): the daemon returns a legible
/// PayloadTooLarge instead of closing on an oversized response, and the default graph view stays
/// bounded rather than loading the whole graph.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class GraphScalingTests
{
    private static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web);

    // Defect B: an oversized response must not close the connection — it becomes a PayloadTooLarge
    // error the caller can act on. RED before the guard (the write threw ArgumentException and the
    // daemon dropped the pipe).
    [Fact]
    public void SerializeWithinBudget_ReturnsPayloadTooLarge_WhenTheResponseOverflowsTheFrame()
    {
        var huge = IpcResponse.Success(new string('x', IpcFraming.MaxFrameBytes + 1_000));

        var json = IpcServer.SerializeWithinBudget(huge);
        var response = JsonSerializer.Deserialize<IpcResponse>(json, Wire)!;

        Assert.False(response.Ok);
        Assert.Equal(IpcErrorCodes.PayloadTooLarge, response.ErrorCode);
        // The replacement itself fits, so the daemon can always write a valid frame.
        Assert.True(Encoding.UTF8.GetByteCount(json) <= IpcFraming.MaxFrameBytes);
    }

    [Fact]
    public void SerializeWithinBudget_PassesTheResponseThrough_WhenItFits()
    {
        var json = IpcServer.SerializeWithinBudget(IpcResponse.Success("small"));
        var response = JsonSerializer.Deserialize<IpcResponse>(json, Wire)!;

        Assert.True(response.Ok);
        Assert.Equal("small", response.Payload);
    }

    // DC-035 control: the default (no-focus) view must stay bounded rather than asking for the whole
    // graph. A whole-graph request overflowed the transport on a small repo and does not scale.
    [Fact]
    public void DefaultGraphView_AsksForABoundedOverview_NotTheWholeGraph()
    {
        // Bounded, and small enough to plausibly fit the transport — never the whole graph.
        Assert.InRange(CanvasGraphViewModel.WholeGraphNodeCap, 1, 1_000);
    }
}
