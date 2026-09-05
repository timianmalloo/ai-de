using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The read-only code viewer (ADR-0019 code-viewer-renderer) and its content seam (ADR-0018 node-content-reader-contract). AvalonEdit constructs on an
/// STA thread with no window (confirmed by the ADR-0019 code-viewer-renderer spike).
/// </summary>
public sealed class CodeViewerTests
{
    private static void OnSta(Action work) =>
        Sta.Run(work, 30);

    [Fact]
    public void Show_Code_AppliesHighlightingByLanguage_AndIsReadOnly()
    {
        OnSta(() =>
        {
            var v = new CodeViewerView();
            v.Show(new NodeContent("A", NodeContentKind.Code, "csharp", "class X {}"));

            Assert.Equal("C#", v.HighlightingName);
            Assert.False(v.IsFallback);
            Assert.Contains("class X", v.ShownText);
            Assert.Equal("A", v.NodeId);
        });
    }

    [Fact]
    public void Show_Code_UnknownLanguage_DegradesToPlain_NotError()
    {
        OnSta(() =>
        {
            var v = new CodeViewerView();
            v.Show(new NodeContent("B", NodeContentKind.Code, "bicep", "resource x 'y' = {}"));

            Assert.Null(v.HighlightingName);          // no built-in -> plain (US-ED2)
            Assert.False(v.IsFallback);               // still renders the content, not an error
            Assert.Contains("resource x", v.ShownText);
        });
    }

    [Fact]
    public void Show_None_ShowsFallback_NotCode()
    {
        OnSta(() =>
        {
            var v = new CodeViewerView();
            v.Show(new NodeContent("C", NodeContentKind.None, null, ""));

            Assert.True(v.IsFallback);
            Assert.Equal("", v.ShownText);
        });
    }

    [Fact]
    public void Show_Shortfall_IsReflected()
    {
        OnSta(() =>
        {
            var v = new CodeViewerView();
            v.Show(new NodeContent("D", NodeContentKind.Code, "python", "print(1)", Shortfall: "first 40 lines"));
            Assert.False(v.IsFallback);
            Assert.Equal("Python", v.HighlightingName);
        });
    }

    [Fact]
    public void Clear_ReturnsToFallback()
    {
        OnSta(() =>
        {
            var v = new CodeViewerView();
            v.Show(new NodeContent("E", NodeContentKind.Code, "csharp", "class Y {}"));
            Assert.False(v.IsFallback);

            v.Clear();
            Assert.True(v.IsFallback);
            Assert.Null(v.NodeId);
        });
    }

    [Fact]
    public async Task MockSource_ReturnsCodeContentForANode()
    {
        var content = await new MockNodeContentSource().GetAsync("Shop.Order");
        Assert.Equal(NodeContentKind.Code, content.RenderKind);
        Assert.Equal("csharp", content.Language);
        Assert.Contains("Shop.Order", content.Content);
    }
}
