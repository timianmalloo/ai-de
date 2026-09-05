using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

namespace AiDe.App.Workbench;

/// <summary>
/// The read-only code viewer (spec-editor-surfaces US-ED1–ED4; ADR-0025 code-viewer-renderer). A native AvalonEdit
/// <see cref="TextEditor"/> in read-only mode with syntax highlighting picked from the content's
/// language tag — a pure WPF control, so none of ADR-0015's WebView2 airspace concerns. Renders a
/// <see cref="NodeContent"/>: code (highlighted), text (plain), a shortfall banner when the content was
/// bounded (US-ED3), and an honest fallback when there is no inline content (US-ED8).
/// </summary>
public sealed class CodeViewerView : ContentControl
{
    private readonly TextEditor _editor;
    private readonly TextBlock _shortfall;
    private readonly TextBlock _fallback;
    private readonly DockPanel _root;

    public CodeViewerView(string title = "Source")
    {
        AutomationProperties.SetName(this, title);
        SetResourceReference(BackgroundProperty, "SunkenBrush");

        _root = new DockPanel { LastChildFill = true };

        _shortfall = new TextBlock
        {
            Margin = new Thickness(12, 6, 12, 6),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Visibility = Visibility.Collapsed,
        };
        _shortfall.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        DockPanel.SetDock(_shortfall, Dock.Top);
        _root.Children.Add(_shortfall);

        _fallback = new TextBlock
        {
            Margin = new Thickness(16),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Visibility = Visibility.Collapsed,
        };
        _fallback.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        _editor = new TextEditor
        {
            IsReadOnly = true,               // US-ED1: no keystroke mutates the content
            ShowLineNumbers = true,
            FontFamily = new System.Windows.Media.FontFamily("Cascadia Mono, Consolas, monospace"),
            FontSize = 12.5,
            WordWrap = false,
        };
        AutomationProperties.SetName(_editor, title + " (read-only)");

        Content = _root;
        Clear();
    }

    /// <summary>The id of the node whose content is shown, or null when empty/cleared.</summary>
    public string? NodeId { get; private set; }

    /// <summary>The AvalonEdit highlighting currently applied, or null (plain). For tests.</summary>
    public string? HighlightingName => _editor.SyntaxHighlighting?.Name;

    /// <summary>Whether the viewer is showing the no-inline-content fallback.</summary>
    public bool IsFallback => _fallback.Visibility == Visibility.Visible;

    /// <summary>The read-only text currently shown (empty in the fallback state). For tests.</summary>
    public string ShownText => _editor.Text;

    /// <summary>Renders a node's content. RenderKind decides the branch (US-ED2/ED8).</summary>
    public void Show(NodeContent content)
    {
        NodeId = content.NodeId;

        if (content.RenderKind == NodeContentKind.None)
        {
            ShowFallback("No inline view for this node — see its metadata and edges.");
            return;
        }

        SetBody();
        _editor.SyntaxHighlighting = content.RenderKind == NodeContentKind.Code
            ? HighlightingFor(content.Language)   // unknown language -> null -> plain text (US-ED2)
            : null;
        _editor.Text = content.Content;

        if (!string.IsNullOrEmpty(content.Shortfall))
        {
            _shortfall.Text = content.Shortfall;
            _shortfall.Visibility = Visibility.Visible;
        }
        else
        {
            _shortfall.Visibility = Visibility.Collapsed;
        }
    }

    public void Clear()
    {
        NodeId = null;
        _editor.Text = "";
        _editor.SyntaxHighlighting = null;
        _shortfall.Visibility = Visibility.Collapsed;
        ShowFallback("Select a node to read its source.");
    }

    private void ShowFallback(string message)
    {
        _fallback.Text = message;
        _fallback.Visibility = Visibility.Visible;
        _shortfall.Visibility = Visibility.Collapsed;
        if (_root.Children.Contains(_editor)) { _root.Children.Remove(_editor); }
        if (!_root.Children.Contains(_fallback)) { _root.Children.Add(_fallback); }
    }

    private void SetBody()
    {
        if (_root.Children.Contains(_fallback)) { _root.Children.Remove(_fallback); }
        _fallback.Visibility = Visibility.Collapsed;
        if (!_root.Children.Contains(_editor)) { _root.Children.Add(_editor); }
    }

    /// <summary>
    /// Maps a language tag to an AvalonEdit built-in highlighting, or null (plain text) when there is
    /// none — confirmed by the ADR-0025 code-viewer-renderer spike: C#/Python/JavaScript/TSQL ship built-in; ts/bicep degrade.
    /// </summary>
    private static IHighlightingDefinition? HighlightingFor(string? language)
    {
        var name = (language ?? "").Trim().ToLowerInvariant() switch
        {
            "csharp" or "c#" or "cs" => "C#",
            "python" or "py" => "Python",
            "javascript" or "js" or "typescript" or "ts" => "JavaScript",
            "sql" or "tsql" => "TSQL",
            "json" => "Json",
            "xml" => "XML",
            "html" => "HTML",
            "css" => "CSS",
            _ => null,
        };

        return name is null ? null : HighlightingManager.Instance.GetDefinition(name);
    }
}
