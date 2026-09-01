using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The read-only code viewer (ADR-0019) and its content seam (ADR-0018). AvalonEdit constructs on an
/// STA thread with no window (confirmed by the ADR-0019 spike).
/// </summary>
public sealed class CodeViewerTests
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
